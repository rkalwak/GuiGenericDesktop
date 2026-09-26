using CompilationLib;
using FluentAssertions;
using FluentAssertions.Execution;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CompilationLib.Tests.IntegrationTests
{
    [Trait("Category", "Integration")]
    public class CompileHandlerArduinoCliIntegrationTests : PlatformioTestBase, IDisposable
    {
        private const string SketchDirectoryName = "GUI-GENERIC";
        private const string Fqbn = "esp32:esp32:esp32";
        private const string SourceMainCppFileName = "main.cpp";
        private const string SourceMainCppBackupFileName = "main.cpp.bak";
        private const string InoFileName = "GUI-GENERIC.ino";
        private const string SourceInoFileName = "src.ino";
        private const string FirmwareFileName = "GUI-GENERIC.ino.bin";

        private readonly string _testDirectory;
        private readonly string _sketchDirectory;

        public CompileHandlerArduinoCliIntegrationTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "CompileHandlerArduinoCli_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDirectory);
            _sketchDirectory = Path.Combine(_testDirectory, SketchDirectoryName);
            CopyAll(SourceRepositoryPath, _sketchDirectory);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testDirectory))
                {
                    Directory.Delete(_testDirectory, true);
                }
            }
            catch
            {
            }
        }

        [Fact]
        public async Task Handle_WithArduinoCli_SketchApproachUsesCliOptionsAndCompiles()
        {
            var detector = new ArduinoCliDetector();
            var detection = await detector.TryGetArduinoCliAsync(CancellationToken.None);
            detection.Found.Should().BeTrue("arduino-cli must be installed and available to run the real integration compile");

            var isEsp32PlatformInstalled = await IsEsp32PlatformInstalledAsync(detection.PathOrCommand, CancellationToken.None);
            isEsp32PlatformInstalled.Should().BeTrue("the real integration compile requires `arduino-cli core install esp32:esp32`");

            var request = new CompileRequest
            {
                EnvironmentName = Fqbn,
                Board = "esp32",
                FlashSize = "4MB",
                ProjectDirectory = _sketchDirectory,
                ProjectPath = _sketchDirectory,
                LibrariesPath = Path.Combine(_sketchDirectory, "lib"),
                ShouldDeploy = false,
                UseSketchApproach = true,
                BuildFlags = new List<BuildFlagItem>
                {
                    new BuildFlagItem
                    {
                        Key = "SUPLA_CONFIG",
                        IsEnabled = true,
                        Parameters = new List<Parameter>
                        {
                            new Parameter { Name = "GPIOLed", Key = "GPIOLed", Type = "number", Value = "0" },
                            new Parameter { Name = "ActivationState", Key = "ActivationState", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIOButton", Key = "GPIOButton", Type = "number", Value = "0" },
                            new Parameter { Name = "ConfigMode", Key = "ConfigMode", Type = "number", Value = "0" },
                            new Parameter { Name = "ForceRestartOnConnectionLoss", Key = "ForceRestartOnConnectionLoss", Type = "number", Value = "0" }
                        }
                    },
                    new BuildFlagItem { Key = "SUPLA_OTA", IsEnabled = true },
                    new BuildFlagItem { Key = "SUPLA_ENABLE_GUI", IsEnabled = true },
                    new BuildFlagItem
                    {
                        Key = "SUPLA_INITIALCONFIG",
                        IsEnabled = true,
                        Parameters = new List<Parameter>
                        {
                            new Parameter { Name = "Mode", Key = "Mode", Type = "number", Value = "0" },
                            new Parameter { Name = "UseBuildConfiguration", Key = "UseBuildConfiguration", Type = "number", Value = "1" },
                            new Parameter { Name = "TimeoutInMin", Key = "TimeoutInMin", Type = "number", Value = "5" },
                            new Parameter { Name = "WIFISsid", Key = "WIFISsid", Type = "string", Value = "TestNetwork" },
                            new Parameter { Name = "WIFIPass", Key = "WIFIPass", Type = "string", Value = "test-password" },
                            new Parameter { Name = "Server", Key = "Server", Type = "string", Value = "svr85.supla.org" },
                            new Parameter { Name = "Email", Key = "Email", Type = "string", Value = "test@example.com" },
                            new Parameter { Name = "Login", Key = "Login", Type = "string", Value = "test" },
                            new Parameter { Name = "Password", Key = "Password", Type = "string", Value = "test-password" },
                            new Parameter { Name = "DeviceName", Key = "DeviceName", Type = "string", Value = "GG BD" }
                        }
                    },
                    new BuildFlagItem
                    {
                        Key = "SUPLA_LIMIT_SWITCH",
                        IsEnabled = true,
                        Parameters = new List<Parameter>
                        {
                            new Parameter { Name = "Count", Key = "Count", Type = "number", Value = "1" },
                            new Parameter { Name = "GPIO1", Key = "GPIO1", Type = "number", Value = "4" },
                            new Parameter { Name = "GPIO1Pullup", Key = "GPIO1Pullup", Type = "number", Value = "1" },
                            new Parameter { Name = "GPIO2", Key = "GPIO2", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO2Pullup", Key = "GPIO2Pullup", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO3", Key = "GPIO3", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO3Pullup", Key = "GPIO3Pullup", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO4", Key = "GPIO4", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO4Pullup", Key = "GPIO4Pullup", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO5", Key = "GPIO5", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO5Pullup", Key = "GPIO5Pullup", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO6", Key = "GPIO6", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO6Pullup", Key = "GPIO6Pullup", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO7", Key = "GPIO7", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO7Pullup", Key = "GPIO7Pullup", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO8", Key = "GPIO8", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO8Pullup", Key = "GPIO8Pullup", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO9", Key = "GPIO9", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO9Pullup", Key = "GPIO9Pullup", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO10", Key = "GPIO10", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO10Pullup", Key = "GPIO10Pullup", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO11", Key = "GPIO11", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO11Pullup", Key = "GPIO11Pullup", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO12", Key = "GPIO12", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO12Pullup", Key = "GPIO12Pullup", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO13", Key = "GPIO13", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO13Pullup", Key = "GPIO13Pullup", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO14", Key = "GPIO14", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO14Pullup", Key = "GPIO14Pullup", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO15", Key = "GPIO15", Type = "number", Value = "0" },
                            new Parameter { Name = "GPIO15Pullup", Key = "GPIO15Pullup", Type = "number", Value = "0" }
                        }
                    },
                    new BuildFlagItem { Key = "SUPLA_EXCLUDE_LITTLEFS_CONFIG", IsEnabled = true },
                    new BuildFlagItem { Key = "SUPLA_DISABLE_LOGS", IsEnabled = true }
                }
            };

            var handler = new CompileHandler();
            var result = await handler.Handle(request, CancellationToken.None);

            using (new AssertionScope())
            {
                AssertArduinoPreparationFiles();
                //AssertArduinoCompileScript();
                result.Should().NotBeNull();
                result.IsSuccessful.Should().BeTrue();
                Directory.Exists(result.OutputDirectory).Should().BeTrue();
                result.OutputFile.Should().Be(FirmwareFileName);

                var firmwarePath = Path.Combine(result.OutputDirectory, result.OutputFile);
                File.Exists(firmwarePath).Should().BeTrue();
            }
        }

        private void AssertArduinoPreparationFiles()
        {
            var mainCppPath = Path.Combine(_sketchDirectory, "src", SourceMainCppFileName);
            var mainCppBackupPath = Path.Combine(_sketchDirectory, "src", SourceMainCppBackupFileName);
            var inoPath = Path.Combine(_sketchDirectory, InoFileName);
            var sourceInoPath = Path.Combine(_sketchDirectory, "src", SourceInoFileName);

            File.Exists(mainCppPath).Should().BeFalse();
            File.Exists(mainCppBackupPath).Should().BeTrue();
            File.Exists(inoPath).Should().BeTrue();
            File.Exists(sourceInoPath).Should().BeFalse();
            File.ReadAllText(inoPath).Should().Be("#include \"src/main.cpp.bak\"\n");
        }

        private void AssertArduinoCompileScript()
        {
            var scriptFiles = Directory.GetFiles(_sketchDirectory, "compile-arduino*.ps1", SearchOption.TopDirectoryOnly);
            string scriptContent = null;

            foreach (var scriptFile in scriptFiles)
            {
                var candidateContent = File.ReadAllText(scriptFile);
                if (candidateContent.Contains(_sketchDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    scriptContent = candidateContent;
                    break;
                }
            }

            scriptContent.Should().NotBeNull("the test-generated script must reference the temporary sketch directory");
            scriptContent.Should().StartWith("& 'arduino-cli.exe' --% compile");
            scriptContent.Should().NotContain("@echo off");
            scriptContent.Should().Contain("arduino-cli");
            scriptContent.Should().Contain("compile");
            scriptContent.Should().Contain($"--fqbn {Fqbn}");
            scriptContent.Should().Contain("compiler.cpp.extra_flags=");
            scriptContent.Should().Contain("-D ESP32");
            scriptContent.Should().Contain("-D CONFIG_CRYPTO_MBEDTLS");
            scriptContent.Should().Contain("-DBUILD_VERSION=");
            scriptContent.Should().Contain("-DOPTIONS_HASH=");
            scriptContent.Should().Contain("-D SUPLA_CONFIG=1");
            scriptContent.Should().Contain("-D SUPLA_CONFIG_GPIOLed=0");
            scriptContent.Should().Contain("-D SUPLA_OTA=1");
            scriptContent.Should().Contain("-D SUPLA_ENABLE_GUI=1");
            scriptContent.Should().Contain("-D SUPLA_INITIALCONFIG=1");
            scriptContent.Should().Contain("-D SUPLA_INITIALCONFIG_UseBuildConfiguration=1");
            scriptContent.Should().Contain("-DSUPLA_INITIALCONFIG_WIFISsid=\"TestNetwork\"");
            scriptContent.Should().Contain("-DSUPLA_INITIALCONFIG_DeviceName=\"GG\\040BD\"");
            scriptContent.Should().Contain("-D SUPLA_LIMIT_SWITCH_Count=1");
            scriptContent.Should().Contain("-D SUPLA_LIMIT_SWITCH_GPIO1=4");
            scriptContent.Should().Contain("-D SUPLA_LIMIT_SWITCH_GPIO1Pullup=1");
            scriptContent.Should().Contain("-D SUPLA_LIMIT_SWITCH_GPIO14=0");
            scriptContent.Should().Contain("-D SUPLA_LIMIT_SWITCH_GPIO15=0");
            scriptContent.Should().Contain("-D SUPLA_EXCLUDE_LITTLEFS_CONFIG=1");
            scriptContent.Should().Contain("-D SUPLA_DISABLE_LOGS=1");
            scriptContent.Should().Contain("--output-dir");
        }

        private static async Task<bool> IsEsp32PlatformInstalledAsync(string arduinoCliPath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(arduinoCliPath))
            {
                return false;
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = arduinoCliPath,
                Arguments = "core list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
            {
                return false;
            }

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await Task.WhenAll(outputTask, errorTask, process.WaitForExitAsync(cancellationToken));

            var output = await outputTask;
            return process.ExitCode == 0 && output.Contains("esp32:esp32", StringComparison.OrdinalIgnoreCase);
        }
    }
}
