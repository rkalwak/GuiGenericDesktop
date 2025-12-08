using CompilationLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CompilationLib.Tests
{
    public abstract class PlatformioTestBase
    {
        protected readonly string SourceRepositoryPath = @"c:/repozytoria/platformio/GUI-Generic";
        protected readonly string TestRepositoryPath = @"c:/repozytoria/platformio/gg_test";

        protected string CreateTempRepoCopy()
        {
            string tempRepo = Path.Combine(TestRepositoryPath, "gui_generic_repo_" + Guid.NewGuid().ToString("N"));
            //Directory.CreateDirectory(tempRepo);
            CopyAll(SourceRepositoryPath, tempRepo);
            return tempRepo;
        }

        protected void CleanupTempRepo(string tempRepo)
        {
            try { if (Directory.Exists(tempRepo)) Directory.Delete(tempRepo, true); } catch { }
        }

        protected async Task<CompileResponse> RunHandlerAsync(string platform, List<BuildFlagItem> flags, string projectDir, string port = "COM3")
        {
            var request = new CompileRequest
            {
                BuildFlags = flags,
                Platform = platform,
                ProjectDirectory = projectDir,
                LibrariesPath = Path.Combine(projectDir, "lib"),
                ProjectPath = Path.Combine(projectDir, "platformio.ini"),
                ShouldDeploy = false,
                PortCom = port
            };

            var handler = new PlatformioCliHandler();
            return await handler.Handle(request, System.Threading.CancellationToken.None);
        }

        protected static void CopyAll(string sourceDir, string destinationDir)
        {
            if (!Directory.Exists(sourceDir))
                throw new DirectoryNotFoundException($"Source directory does not exist: {sourceDir}");
            if (sourceDir.Contains("git"))
                return;

            // Normalize paths to full absolute paths for reliable comparisons
            string sourceFull = Path.GetFullPath(sourceDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string destFull = Path.GetFullPath(destinationDir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            // Protect against recursive/nested copies: destination inside source or source inside destination
            if (string.Equals(sourceFull, destFull, StringComparison.OrdinalIgnoreCase) ||
                sourceFull.StartsWith(destFull, StringComparison.OrdinalIgnoreCase) ||
                destFull.StartsWith(sourceFull, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Refusing to copy because source ('{sourceFull}') and destination ('{destFull}') paths are nested or identical. This would cause recursive copying.");
            }

            Directory.CreateDirectory(destinationDir);

            foreach (var filePath in Directory.GetFiles(sourceDir, "*", SearchOption.TopDirectoryOnly))
            {
                var fileName = Path.GetFileName(filePath);
                var destFile = Path.Combine(destinationDir, fileName);
                File.Copy(filePath, destFile, overwrite: true);
            }

            foreach (var dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.TopDirectoryOnly))
            {
                var dirName = Path.GetFileName(dirPath);
                var destSubDir = Path.Combine(destinationDir, dirName);
                CopyAll(dirPath, destSubDir);
            }
        }
    }
}
