using CompilationLib;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

public class CompileHandler : ICompileHandler
{
    private const string DefaultOptionsHash = "e3bc65278b88a589d06c3f8316cf0e3b";
    private const string DefaultGuiGenericVersion = "R26.09.17";

    public CompileHandler()
    {
    }

    public event EventHandler<string> OutputLine;
    public event EventHandler<string> ErrorLine;

    public async Task<CompileResponse> Handle(CompileRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ProjectDirectory))
        {
            throw new ArgumentException("ProjectDirectory is required.", nameof(request));
        }

        var compileResponse = new CompileResponse();
        string projectPath = NormalizeProjectPath(request.ProjectPath, request.ProjectDirectory);
        string fqbn = NormalizeArduinoFqbn(request.EnvironmentName);
        string buildFlagsString = BuildFlagsStringForCompilation(request.BuildFlags);
        string buildExtraFlags = CombineBuildExtraFlags(
            GetTargetDefinesForFqbn(fqbn),
            GetProjectMetadataDefines(request.ProjectDirectory),
            buildFlagsString);
        var partitionCsvPath = GetArduinoPartitionCsvPath(request);
        var partitionBuildProperties = GetPartitionBuildProperties(request.FlashSize, partitionCsvPath);
        var buildDir = Path.Combine(request.ProjectDirectory, "build");

        if (request.ShouldDeploy && request.ShouldEraseFlash)
        {
            var chipType = request.Board.ToLower();
            var esptoolWrapper = new EsptoolWrapper();
            var eraseResult = await esptoolWrapper.EraseFlash(request.PortCom, chipType, cancellationToken);
            if (!eraseResult.Success)
            {
                throw new InvalidOperationException($"Flash erase failed before upload: {eraseResult.StdErr}");
            }
        }

        using var sketchPreparation = request.UseSketchApproach
            ? PrepareProjectForArduinoCli(projectPath)
            : null;

        var sketchDirectory = ResolveSketchDirectory(projectPath, request.ProjectDirectory);

        var arduinoCliExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "arduino-cli.exe"
            : "arduino-cli";

        var cliArguments = new List<string>
        {
            "compile",
            "--no-color",
            //"--quiet",
            "--clean",
            "-e",
            "--fqbn",
            fqbn
        };

        if (!string.IsNullOrWhiteSpace(request.LibrariesPath))
        {
            cliArguments.Add("--libraries");
            cliArguments.Add(request.LibrariesPath);
        }

        if (!string.IsNullOrWhiteSpace(buildExtraFlags))
        {
            cliArguments.Add("--build-property");
            cliArguments.Add($"compiler.cpp.extra_flags={buildExtraFlags}");
        }

        foreach (var property in partitionBuildProperties)
        {
            cliArguments.Add("--build-property");
            cliArguments.Add(property);
        }
        cliArguments.AddRange(GetUploadArguments(request));
        cliArguments.Add("--output-dir");
        cliArguments.Add(buildDir);
        cliArguments.Add(".");

        var processStartInfo = new ProcessStartInfo
        {
            FileName = arduinoCliExe,
            WorkingDirectory = sketchDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        SetEnglishLocale(processStartInfo);

        processStartInfo.Arguments = string.Join(" ", cliArguments.Select(QuoteArgument));

        var commandLineForLog = BuildCommandLineForLogging(arduinoCliExe, cliArguments);
        Console.WriteLine($"Compiling:{commandLineForLog}");
        Debug.WriteLine($"Compiling:{commandLineForLog}");

        // Keep the original command line visible in the same format users expect to see when debugging.
        Console.WriteLine($"Compiling command args: {string.Join(" ", cliArguments.Select(QuoteArgument))}");
        Debug.WriteLine($"Compiling command args: {string.Join(" ", cliArguments.Select(QuoteArgument))}");

        if (Directory.Exists(buildDir))
        {
            Directory.Delete(buildDir, recursive: true);
        }

        using var partitionCsvOverride = StagePartitionCsv(partitionCsvPath, sketchDirectory);
        using var process = new Process { StartInfo = processStartInfo };
        using var cancellationRegistration = cancellationToken.Register(() => TryKillProcess(process));

        process.EnableRaisingEvents = true;
        process.Exited += (sender, e) =>
        {
            Console.WriteLine($"Process exited with code: {process.ExitCode}");
            Debug.WriteLine($"Process exited with code: {process.ExitCode}");
        };
        process.OutputDataReceived += Process_OutputDataReceived;
        process.ErrorDataReceived += Process_ErrorDataReceived;

        Stopwatch stopwatch = Stopwatch.StartNew();
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);
        stopwatch.Stop();

        compileResponse.IsSuccessful = process.ExitCode == 0;
        compileResponse.ElapsedTimeInSeconds = stopwatch.Elapsed.TotalSeconds;
        compileResponse.OutputDirectory = buildDir;
        compileResponse.OutputFile = $"{Path.GetFileName(sketchDirectory)}.ino.bin";

        return compileResponse;
    }

    private static string GetArduinoPartitionCsvPath(CompileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FlashSize) ||
            request.FlashSize.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return PartitionManager.GetPartitionFilePath(
            request.EnvironmentName,
            request.FlashSize,
            request.ProjectDirectory,
            request.Board);
    }

    private static List<string> GetPartitionBuildProperties(string flashSize, string partitionCsvPath)
    {
        if (string.IsNullOrWhiteSpace(partitionCsvPath) || !File.Exists(partitionCsvPath))
        {
            return new List<string>();
        }

        var partitionLayout = PartitionGenerator.GetPartitionLayout(partitionCsvPath);
        if (partitionLayout.App0Size <= 0)
        {
            return new List<string>();
        }

        return new List<string>
        {
            $"build.flash_size={flashSize}",
            $"build.partitions={Path.GetFileNameWithoutExtension(partitionCsvPath)}",
            $"upload.maximum_size={partitionLayout.App0Size}"
        };
    }

    private static List<string> GetUploadArguments(CompileRequest request)
    {
        if (!request.ShouldDeploy)
        {
            return new List<string>();
        }

        if (string.IsNullOrWhiteSpace(request.PortCom))
        {
            throw new ArgumentException("PortCom is required when deployment is enabled.", nameof(request));
        }

        return new List<string>
        {
            "--upload",
            "--port",
            request.PortCom
        };
    }

    private static IDisposable StagePartitionCsv(string partitionCsvPath, string sketchDirectory)
    {
        if (string.IsNullOrWhiteSpace(partitionCsvPath) || !File.Exists(partitionCsvPath))
        {
            return null;
        }

        return new ArduinoPartitionCsvOverride(partitionCsvPath, Path.Combine(sketchDirectory, "partitions.csv"));
    }

    private static void SetEnglishLocale(ProcessStartInfo processStartInfo)
    {
        processStartInfo.Environment["LANG"] = "C";
        processStartInfo.Environment["LC_ALL"] = "C";
        processStartInfo.Environment["LC_MESSAGES"] = "C";
        processStartInfo.Environment["LANGUAGE"] = "en";
    }

    private sealed class ArduinoPartitionCsvOverride : IDisposable
    {
        private readonly string _targetPath;
        private readonly byte[] _originalContent;
        private readonly bool _targetExisted;

        public ArduinoPartitionCsvOverride(string sourcePath, string targetPath)
        {
            _targetPath = targetPath;
            _targetExisted = File.Exists(targetPath);
            _originalContent = _targetExisted ? File.ReadAllBytes(targetPath) : null;
            File.Copy(sourcePath, targetPath, overwrite: true);
        }

        public void Dispose()
        {
            if (_targetExisted)
            {
                File.WriteAllBytes(_targetPath, _originalContent);
            }
            else if (File.Exists(_targetPath))
            {
                File.Delete(_targetPath);
            }
        }
    }

    private static string NormalizeArduinoFqbn(string environmentName)
    {
        if (string.IsNullOrWhiteSpace(environmentName))
        {
            return "esp32:esp32:esp32";
        }

        var normalized = environmentName.Trim();
        if (normalized.Contains(':', StringComparison.Ordinal))
        {
            return normalized;
        }

        var lowered = normalized.Trim();
        if (lowered.StartsWith("GUI_Generic_", StringComparison.OrdinalIgnoreCase))
        {
            var model = lowered["GUI_Generic_".Length..];

            return model.ToLowerInvariant() switch
            {
                "esp8266" => "esp8266:esp8266:generic",
                "esp32" => "esp32:esp32:esp32",
                "esp32c3" => "esp32:esp32c3:esp32c3",
                "esp32c6" => "esp32:esp32c6:esp32c6",
                "esp32s2" => "esp32:esp32s2:esp32s2",
                "esp32s3" => "esp32:esp32s3:esp32s3",
                _ => "esp32:esp32:esp32"
            };
        }

        var simpleName = normalized.Replace("-", string.Empty, StringComparison.Ordinal).Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
        return simpleName switch
        {
            "esp8266" => "esp8266:esp8266:generic",
            "esp32" => "esp32:esp32:esp32",
            "esp32c3" => "esp32:esp32c3:esp32c3",
            "esp32c6" => "esp32:esp32c6:esp32c6",
            "esp32s2" => "esp32:esp32s2:esp32s2",
            "esp32s3" => "esp32:esp32s3:esp32s3",
            _ => normalized
        };
    }

    private static string GetTargetDefinesForFqbn(string fqbn)
    {
        if (string.IsNullOrWhiteSpace(fqbn))
        {
            return string.Empty;
        }

        if (fqbn.StartsWith("esp32:", StringComparison.OrdinalIgnoreCase))
        {
            return "-D ESP32 -D CONFIG_CRYPTO_MBEDTLS";
        }

        if (fqbn.StartsWith("esp8266:", StringComparison.OrdinalIgnoreCase))
        {
            return "-D ESP8266";
        }

        return string.Empty;
    }

    private static string GetProjectMetadataDefines(string projectDirectory)
    {
        var content = string.Empty;
        if (!string.IsNullOrWhiteSpace(projectDirectory))
        {
            var platformioIniPath = Path.Combine(projectDirectory, "platformio.ini");
            if (File.Exists(platformioIniPath))
            {
                content = File.ReadAllText(platformioIniPath);
            }
        }

        var defines = new List<string>();
        var guiGenericVersion = LibraryVersionExtractor.GetGuiGenericVersion(projectDirectory);
        if (string.IsNullOrWhiteSpace(guiGenericVersion))
        {
            guiGenericVersion = DefaultGuiGenericVersion;
        }

        defines.Add($"-DBUILD_VERSION={NormalizeMetadataDefineValue(guiGenericVersion)}");
        AddMetadataDefineIfPresent(content, defines, "OPTIONS_HASH");

        if (!defines.Any(define => define.StartsWith("-DOPTIONS_HASH=", StringComparison.OrdinalIgnoreCase)))
        {
            defines.Add($"-DOPTIONS_HASH={NormalizeMetadataDefineValue(DefaultOptionsHash)}");
        }

        return string.Join(" ", defines);
    }

    private static void AddMetadataDefineIfPresent(string content, List<string> defines, string defineName)
    {
        var match = Regex.Match(
            content,
            $@"^[ \t]*-D[ \t]+{Regex.Escape(defineName)}=(.+)$",
            RegexOptions.Multiline);

        if (!match.Success)
        {
            return;
        }

        var rawValue = match.Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return;
        }

        defines.Add($"-D{defineName}={NormalizeMetadataDefineValue(rawValue)}");
    }

    private static string NormalizeMetadataDefineValue(string rawValue)
    {
        var value = rawValue.Trim();

        if (value.Length >= 2 && value[0] == '\'' && value[^1] == '\'')
        {
            value = value[1..^1];
        }

        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
        {
            value = value[1..^1];
        }

        var escaped = EscapeBuildFlagStringValue(value);
        return $"\"{escaped}\"";
    }

    private static string EscapeBuildFlagStringValue(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }

    private static string BuildFlagsStringForCompilation(List<BuildFlagItem> buildFlags)
    {
        if (buildFlags == null)
        {
            return string.Empty;
        }

        var parts = new List<string>();

        foreach (var flag in buildFlags)
        {
            if (flag == null || string.IsNullOrWhiteSpace(flag.Key) || !flag.IsEnabled)
            {
                continue;
            }

            var flagKey = flag.Key.Trim();
            if (string.IsNullOrWhiteSpace(flagKey))
            {
                continue;
            }

            if (flagKey.StartsWith("SUPLA_", StringComparison.OrdinalIgnoreCase))
            {
                parts.Add($"-D {flagKey}=1");
            }
            else
            {
                parts.Add($"-D {flagKey}=1");
            }

            if (flag.Parameters == null)
            {
                continue;
            }

            foreach (var p in flag.Parameters)
            {
                if (p == null)
                {
                    continue;
                }

                var parameterKey = (p.Key ?? p.Name ?? string.Empty).Trim();
                parameterKey = SavedParameterKeyNormalizer.Normalize(parameterKey, flagKey);
                if (string.IsNullOrEmpty(parameterKey))
                {
                    continue;
                }

                var raw = (p.Value?.ToString() ?? string.Empty).Trim();
                var type = p.Type ?? string.Empty;

                string value;
                var isStringValue = false;
                if (string.Equals(type, "number", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(type, "gpio", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(type, "enum", StringComparison.OrdinalIgnoreCase) ||
                    IsNumericLike(raw))
                {
                    value = string.IsNullOrEmpty(raw) ? "0" : raw;
                }
                else if (string.Equals(type, "bool", StringComparison.OrdinalIgnoreCase))
                {
                    value = string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase) || raw == "1" ? "1" : "0";
                }
                else
                {
                    value = string.IsNullOrEmpty(raw)
                        ? "\"\""
                        : $"\"{EscapeBuildFlagStringValue(raw).Replace(" ", "\\040", StringComparison.Ordinal)}\"";
                    isStringValue = true;
                }

                var define = $"{Parameter.GetFullName(flagKey, parameterKey)}={value}";

                parts.Add(isStringValue ? $"-D{define}" : $"-D {define}");
            }
        }

        return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private static bool IsNumericLike(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _) ||
               decimal.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out _);
    }

    private static string CombineBuildExtraFlags(params string[] parts)
    {
        return string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string NormalizeProjectPath(string projectPath, string projectDirectory)
    {
        var normalizedPath = string.IsNullOrWhiteSpace(projectPath)
            ? projectDirectory
            : projectPath;

        if (Directory.Exists(normalizedPath))
        {
            // Arduino CLI expects the sketch root (directory containing the .ino file or the main.cpp entrypoint),
            // not a nested src folder created by some PlatformIO/UIs. If the supplied path is e.g. .../src,
            // normalize it back to the project root so the CLI looks for src.ino/main.cpp in the correct place.
            if (string.Equals(Path.GetFileName(normalizedPath), "src", StringComparison.OrdinalIgnoreCase))
            {
                var parentDirectory = Path.GetDirectoryName(normalizedPath);
                if (!string.IsNullOrWhiteSpace(parentDirectory) && Directory.Exists(parentDirectory))
                {
                    return parentDirectory;
                }
            }

            return normalizedPath;
        }

        if (File.Exists(normalizedPath))
        {
            return Path.GetDirectoryName(normalizedPath) ?? projectDirectory;
        }

        throw new DirectoryNotFoundException($"Project path does not exist: {normalizedPath}");
    }

    private static string ResolveSketchDirectory(string projectPath, string projectDirectory)
    {
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(projectPath))
        {
            candidates.Add(projectPath);
        }

        if (!string.IsNullOrWhiteSpace(projectDirectory))
        {
            candidates.Add(projectDirectory);
        }

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(candidate))
            {
                continue;
            }

            var rootIno = Directory.GetFiles(candidate, "*.ino", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(rootIno))
            {
                return Path.GetDirectoryName(rootIno) ?? candidate;
            }

            var sourceDirectory = Path.Combine(candidate, "src");
            var sourceSketchPath = Path.Combine(sourceDirectory, $"{Path.GetFileName(sourceDirectory)}.ino");
            if (Directory.Exists(sourceDirectory) && File.Exists(sourceSketchPath))
            {
                return sourceDirectory;
            }

            var nestedIno = Directory.GetFiles(candidate, "*.ino", SearchOption.AllDirectories).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(nestedIno))
            {
                return Path.GetDirectoryName(nestedIno) ?? candidate;
            }
        }

        return projectPath;
    }

    private static ArduinoSketchPreparation PrepareProjectForArduinoCli(string projectPath)
    {
        var preparation = new ArduinoSketchPreparation();

        if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
        {
            return preparation;
        }

        try
        {
            var projectRoot = NormalizeProjectPath(projectPath, projectPath);
            var rootMainCppPath = Path.Combine(projectRoot, "main.cpp");
            var srcDirectory = Path.Combine(projectRoot, "src");
            var srcMainCppPath = Path.Combine(srcDirectory, "main.cpp");
            var rootMainCppBackupPath = rootMainCppPath + ".h";
            var srcMainCppBackupPath = srcMainCppPath + ".h";
            var rootLegacyMainCppBackupPath = rootMainCppPath + ".bak";
            var srcLegacyMainCppBackupPath = srcMainCppPath + ".bak";

            var originalMainCppPath = File.Exists(rootMainCppPath)
                ? rootMainCppPath
                : File.Exists(srcMainCppPath)
                    ? srcMainCppPath
                    : null;

            var backupMainCppPath = File.Exists(rootMainCppBackupPath)
                ? rootMainCppBackupPath
                : File.Exists(srcMainCppBackupPath)
                    ? srcMainCppBackupPath
                    : File.Exists(rootLegacyMainCppBackupPath)
                        ? rootLegacyMainCppBackupPath
                        : File.Exists(srcLegacyMainCppBackupPath)
                            ? srcLegacyMainCppBackupPath
                            : null;

            if (originalMainCppPath is null && backupMainCppPath is null)
            {
                return preparation;
            }

            var sourceFilePath = originalMainCppPath ?? backupMainCppPath!;
            var sourceDirectory = Path.GetDirectoryName(sourceFilePath) ?? projectRoot;

            if (originalMainCppPath is not null)
            {
                backupMainCppPath = Path.Combine(sourceDirectory, Path.GetFileName(originalMainCppPath) + ".h");

                if (File.Exists(backupMainCppPath))
                {
                    preparation.MoveToTemporaryFile(backupMainCppPath);
                }

                var legacyBackupPath = Path.Combine(sourceDirectory, Path.GetFileName(originalMainCppPath) + ".bak");
                if (File.Exists(legacyBackupPath))
                {
                    preparation.MoveToTemporaryFile(legacyBackupPath);
                }

                preparation.MoveFile(originalMainCppPath, backupMainCppPath);
            }
            else if (backupMainCppPath.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
            {
                var legacyBackupPath = backupMainCppPath;
                backupMainCppPath = Path.Combine(sourceDirectory, Path.GetFileNameWithoutExtension(legacyBackupPath) + ".h");
                if (File.Exists(backupMainCppPath))
                {
                    preparation.MoveToTemporaryFile(backupMainCppPath);
                }

                preparation.MoveFile(legacyBackupPath, backupMainCppPath);
            }

            var includePath = Path.GetRelativePath(projectRoot, backupMainCppPath).Replace('\\', '/');
            var projectSketchFileName = $"{Path.GetFileName(projectRoot)}.ino";
            var projectInoPath = Path.Combine(projectRoot, projectSketchFileName);
            var staleSourceProjectInoPath = Path.Combine(sourceDirectory, projectSketchFileName);

            var existingProjectSketchPath = Directory.GetFiles(projectRoot, "*.ino", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (existingProjectSketchPath is not null &&
                !string.Equals(Path.GetFileName(existingProjectSketchPath), projectSketchFileName, StringComparison.Ordinal))
            {
                preparation.MoveFile(existingProjectSketchPath, projectInoPath);
            }
            else if (existingProjectSketchPath is null)
            {
                preparation.TrackCreatedFile(projectInoPath);
                File.WriteAllText(projectInoPath, $"#include \"{includePath}\"\n");
            }
            else
            {
                var existingSketchContent = File.ReadAllText(existingProjectSketchPath);
                var legacyIncludePath = includePath.EndsWith(".h", StringComparison.OrdinalIgnoreCase)
                    ? includePath[..^1] + "bak"
                    : null;
                var legacyGeneratedContent = legacyIncludePath is null
                    ? null
                    : $"#include \"{legacyIncludePath}\"\n";

                if (legacyGeneratedContent is not null && string.Equals(existingSketchContent, legacyGeneratedContent, StringComparison.Ordinal))
                {
                    preparation.TrackFileContent(existingProjectSketchPath, existingSketchContent);
                    File.WriteAllText(existingProjectSketchPath, $"#include \"{includePath}\"\n");
                }
            }

            if (!string.Equals(sourceDirectory, projectRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(staleSourceProjectInoPath))
            {
                preparation.MoveToTemporaryFile(staleSourceProjectInoPath);
            }

            var sourceSketchPath = Path.Combine(sourceDirectory, $"{Path.GetFileName(sourceDirectory)}.ino");
            // Only clean up a generated source-level wrapper when sketch files are under a nested source directory.
            if (!string.Equals(sourceDirectory, projectRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(sourceSketchPath))
            {
                var sourceSketchContent = File.ReadAllText(sourceSketchPath);
                var generatedSourceSketchContent = $"#include \"{Path.GetFileName(backupMainCppPath)}\"\n";
                var legacyGeneratedSourceSketchContent = $"#include \"{Path.GetFileNameWithoutExtension(backupMainCppPath)}.bak\"\n";
                if (string.Equals(sourceSketchContent, generatedSourceSketchContent, StringComparison.Ordinal) ||
                    string.Equals(sourceSketchContent, legacyGeneratedSourceSketchContent, StringComparison.Ordinal))
                {
                    preparation.MoveToTemporaryFile(sourceSketchPath);
                }
            }

            return preparation;
        }
        catch
        {
            preparation.Dispose();
            throw;
        }
    }

    private sealed class ArduinoSketchPreparation : IDisposable
    {
        private readonly List<Action> _restoreActions = new();
        private bool _disposed;

        public void MoveFile(string sourcePath, string destinationPath)
        {
            File.Move(sourcePath, destinationPath);
            _restoreActions.Add(() =>
            {
                if (File.Exists(destinationPath) && !File.Exists(sourcePath))
                {
                    File.Move(destinationPath, sourcePath);
                }
            });
        }

        public void TrackFileContent(string filePath, string originalContent)
        {
            _restoreActions.Add(() => File.WriteAllText(filePath, originalContent));
        }

        public void MoveToTemporaryFile(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
            var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(filePath)}.{Guid.NewGuid():N}.tmp");
            MoveFile(filePath, temporaryPath);
        }

        public void TrackCreatedFile(string filePath)
        {
            _restoreActions.Add(() =>
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            });
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            for (var i = _restoreActions.Count - 1; i >= 0; i--)
            {
                _restoreActions[i]();
            }
        }
    }

    private static void TryKillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (NotSupportedException)
        {
        }
    }

    private static string BuildCommandLineForLogging(string executablePath, IEnumerable<string> arguments)
    {
        var args = arguments.Select(QuoteArgument).ToList();
        var quotedExecutablePath = executablePath.Replace("'", "''");
        return $"& '{quotedExecutablePath}' --% {string.Join(" ", args)}";
    }

    private static string QuoteArgument(string argument)
    {
        if (string.IsNullOrEmpty(argument))
        {
            return "\"\"";
        }

        if (!argument.Any(char.IsWhiteSpace) && !argument.Contains('"'))
        {
            return argument;
        }

        var quotedArgument = new StringBuilder("\"");
        var backslashCount = 0;

        foreach (var character in argument)
        {
            if (character == '\\')
            {
                backslashCount++;
            }
            else if (character == '"')
            {
                quotedArgument.Append('\\', backslashCount * 2 + 1);
                quotedArgument.Append(character);
                backslashCount = 0;
            }
            else
            {
                quotedArgument.Append('\\', backslashCount);
                quotedArgument.Append(character);
                backslashCount = 0;
            }
        }

        quotedArgument.Append('\\', backslashCount * 2);
        quotedArgument.Append('"');
        return quotedArgument.ToString();
    }

    private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        var line = e.Data ?? string.Empty;
        Console.WriteLine(line);
        Debug.WriteLine(line);
        ErrorLine?.Invoke(this, line);
    }

    private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        var line = e.Data ?? string.Empty;
        Console.WriteLine(line);
        Debug.WriteLine(line);
        OutputLine?.Invoke(this, line);
    }
}
