using System.Diagnostics;
using System.Text;

namespace CompilationLib
{
    public  class EsptoolWrapper : IEsptoolWrapper
    {
        private string _esptoolPath = "esptool.exe";

        public Task<EsptoolResult> ReadChipId(string comPort, CancellationToken cancellation = default)
            => RunEsptoolAsync($"--port {EscapeArgument(comPort)} chip-id", cancellation);


        public Task<EsptoolResult> ReadFlashId(string comPort, CancellationToken cancellation = default)
            => RunEsptoolAsync($"--port {EscapeArgument(comPort)} flash-id", cancellation);
        /// <summary>
        /// Runs esptool --chip esp32c6 --port {comPort} write-flash 0x000000 0x4000000 {binFile}
        /// </summary>
        public Task<EsptoolResult> WriteFlush(string comPort, string chip, string binFile, CancellationToken cancellation = default)
            => RunEsptoolAsync($"--chip {chip} --port {EscapeArgument(comPort)} --baud 921600 write-flash 0x000000 {EscapeArgument(binFile)}",
                                cancellation);

        /// <summary>
        /// Runs esptool --chip esp32c6 --port {comPort} read-flash 0x000000 0x4000000 {backupFile}
        /// </summary>
        public Task<EsptoolResult> ReadFlush(string comPort, string chip, string backupFile, CancellationToken cancellation = default)
            => RunEsptoolAsync($"--chip {chip} --port {EscapeArgument(comPort)} --baud 921600 read-flash 0x000000 0x400000 {EscapeArgument(backupFile)}",
                               cancellation);
        private static string EscapeArgument(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            if (value.Contains(' ') || value.Contains('\t') || value.Contains('"'))
                return "\"" + value.Replace("\"", "\\\"") + "\"";
            return value;
        }

        private async Task<EsptoolResult> RunEsptoolAsync(string arguments, CancellationToken cancellation)
        {

            var sbOut = new StringBuilder();
            var sbErr = new StringBuilder();
            Console.WriteLine(arguments);
            var psi = new ProcessStartInfo
            {
                FileName = _esptoolPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start esptool process.");
                var outTask = proc.StandardOutput.ReadToEndAsync();
                var errTask = proc.StandardError.ReadToEndAsync();
                await Task.WhenAll(outTask, errTask).ConfigureAwait(false);
                sbOut.AppendLine(outTask.Result);
                sbErr.AppendLine(errTask.Result);

                // Wait for exit with cancellation support
                using var reg = cancellation.Register(() =>
                {
                    try { if (!proc.HasExited) proc.Kill(true); } catch { }
                });

                await proc.WaitForExitAsync(cancellation).ConfigureAwait(false);

                var result = new EsptoolResult
                {
                    Command = $"{_esptoolPath} {arguments}",
                    ExitCode = proc.ExitCode,
                    StdOut = sbOut.ToString(),
                    StdErr = sbErr.ToString(),
                    Success = proc.ExitCode == 0
                };

                Console.WriteLine(result.StdOut);
                if (!string.IsNullOrEmpty(result.StdErr))
                    Console.Error.WriteLine(result.StdErr);

                return result;
            }
            catch (OperationCanceledException)
            {
                return new EsptoolResult { Command = $"{_esptoolPath} {arguments}", Success = false, ExitCode = -1, StdErr = "Operation canceled." };
            }
            catch (Exception ex)
            {
                return new EsptoolResult { Command = $"{_esptoolPath} {arguments}", Success = false, ExitCode = -1, StdErr = ex.ToString() };
            }
        }
    }

    public record EsptoolResult
    {
        public bool Success { get; init; }
        public string Command { get; init; }
        public int ExitCode { get; init; }
        public string StdOut { get; init; }
        public string StdErr { get; init; }

        public override string ToString()
        {
            return $"EsptoolResult: Success={Success}, ExitCode={ExitCode}, Command={Command}, Output={StdOut}, Error={StdErr}";
        }
    }
}
