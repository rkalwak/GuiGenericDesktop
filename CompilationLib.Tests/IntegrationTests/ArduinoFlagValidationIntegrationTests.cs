using CompilationLib;
using FluentAssertions;
using FluentAssertions.Execution;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CompilationLib.Tests.IntegrationTests
{
    [Trait("Category", "Integration")]
    public class ArduinoFlagValidationIntegrationTests : IDisposable
    {
        private const string ProjectRootName = "ArduinoFlagValidation";
        private const string Fqbn = "esp32:esp32:esp32";
        private const string Port = "COM3";

        private readonly string _testDirectory;
        private readonly string _projectDirectory;

        public ArduinoFlagValidationIntegrationTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), $"ArduinoFlagValidation_{Guid.NewGuid():N}");
            _projectDirectory = Path.Combine(_testDirectory, ProjectRootName);
            Directory.CreateDirectory(_projectDirectory);

            var sourceRoot = Path.Combine(AppContext.BaseDirectory, "TestFixtures", ProjectRootName);
            if (!Directory.Exists(sourceRoot))
            {
                throw new DirectoryNotFoundException($"Fixture project not found at '{sourceRoot}'.");
            }

            CopyDirectory(sourceRoot, _projectDirectory);
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
        public async Task Handle_WithArduinoCli_CompilesAndUploadsFlagValidationSketch()
        {
            var detector = new ArduinoCliDetector();
            var detection = await detector.TryGetArduinoCliAsync(CancellationToken.None);
            detection.Found.Should().BeTrue("arduino-cli must be installed to run the real integration compile");

            var isEsp32PlatformInstalled = await IsEsp32PlatformInstalledAsync(detection.PathOrCommand, CancellationToken.None);
            isEsp32PlatformInstalled.Should().BeTrue("the real integration compile requires the ESP32 Arduino core");
            SerialPort.GetPortNames().Should().Contain(Port, "an ESP32 must be connected on the configured integration-test port");

            var request = new CompileRequest
            {
                EnvironmentName = Fqbn,
                ProjectDirectory = _projectDirectory,
                ProjectPath = _projectDirectory,
                ShouldDeploy = true,
                ShouldBackup = false,
                ShouldEraseFlash = false,
                PortCom = Port,
                BuildFlags = new List<BuildFlagItem>
                {
                    new BuildFlagItem
                    {
                        Key = "FLAG_NUMERIC",
                        IsEnabled = true,
                        Parameters = new List<Parameter>
                        {
                            new Parameter { Key = "COUNT", Name = "COUNT", Type = "number", Value = "7" },
                            new Parameter { Key = "GPIO", Name = "GPIO", Type = "gpio", Value = "12" }
                        }
                    },
                    new BuildFlagItem
                    {
                        Key = "FLAG_STRING",
                        IsEnabled = true,
                        Parameters = new List<Parameter>
                        {
                            new Parameter { Key = "TEXT", Name = "TEXT", Type = "string", Value = "alpha beta" }
                        }
                    }
                }
            };

            var handler = new CompileHandler();
            var result = await handler.Handle(request, CancellationToken.None);
            using (new AssertionScope())
            {
                result.Should().NotBeNull();
                result.IsSuccessful.Should().BeTrue("the real compilation must succeed for the fixture project");
                Directory.Exists(result.OutputDirectory).Should().BeTrue();
                result.OutputFile.Should().Be("ArduinoFlagValidation.ino.bin");
                File.Exists(Path.Combine(result.OutputDirectory, result.OutputFile)).Should().BeTrue();

                var compileScripts = Directory.GetFiles(_projectDirectory, "compile-arduino*.ps1");
                compileScripts.Should().NotBeEmpty();
                var compileScriptContent = File.ReadAllText(compileScripts[0]);
                compileScriptContent.Should().Contain("alpha\\040beta");

                var serialOutput = await ReadSerialOutputAsync(Port, 20000);
                serialOutput.Should().Contain("FLAG_VALIDATION_BEGIN");
                serialOutput.Should().Contain("FLAG_STRING_ACTIVE=1");
                serialOutput.Should().Contain("FLAG_STRING_TEXT=alpha beta");
                serialOutput.Should().Contain("FLAG_NUMERIC_COUNT=7");
                serialOutput.Should().Contain("FLAG_NUMERIC_GPIO=12");
                serialOutput.Should().Contain("FLAG_VALIDATION_PASS");
            }
        }

        private static async Task<string> ReadSerialOutputAsync(string portName, int timeoutMs)
        {
            using var serialPort = new SerialPort(portName)
            {
                BaudRate = 115200,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                ReadTimeout = 1000,
                WriteTimeout = 1000,
            };

            serialPort.Open();
            serialPort.DiscardInBuffer();
            serialPort.DiscardOutBuffer();

            var buffer = new StringBuilder();
            var startedAt = DateTime.UtcNow;
            while ((DateTime.UtcNow - startedAt).TotalMilliseconds < timeoutMs)
            {
                if (serialPort.BytesToRead > 0)
                {
                    var chunk = new char[serialPort.BytesToRead];
                    var count = serialPort.Read(chunk, 0, chunk.Length);
                    buffer.Append(chunk, 0, count);
                }

                if (buffer.ToString().Contains("FLAG_VALIDATION_PASS", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                await Task.Delay(200);
            }

            return buffer.ToString();
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            foreach (var filePath in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(sourceDir, filePath);
                var targetPath = Path.Combine(destinationDir, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                File.Copy(filePath, targetPath, true);
            }
        }

        private static async Task<bool> IsEsp32PlatformInstalledAsync(string cliPath, CancellationToken cancellationToken)
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = cliPath,
                    Arguments = "core list",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            await process.WaitForExitAsync(cancellationToken);
            var output = await process.StandardOutput.ReadToEndAsync();
            return output.Contains("esp32:esp32", StringComparison.OrdinalIgnoreCase);
        }
    }
}
