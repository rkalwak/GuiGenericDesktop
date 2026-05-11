using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace CompilationLib.Tests
{
    /// <summary>
    /// End-to-end integration test for the Z2S firmware flash/upgrade/backup/restore flow.
    /// Firmware files are stored locally under TestFixtures\Z2SFirmware\ - run
    /// Download-Firmware.ps1 in that folder once to populate them.
    /// </summary>
    [Trait("Category", "Integration")]
    public class Z2SFlashUpgradeIntegrationTests
    {
        // ---- Device configuration -----------------------------------------------
        private const string ComPort = "COM7";
        private const string Chip    = "esp32c6";
        // -------------------------------------------------------------------------

        private const long ExpectedFlashSize = 16 * 1024 * 1024;
        private const double BackupSizeTolerance = 0.05;
        private const long MinBackupBytes = 4 * 1024 * 1024;

        private readonly string _backupDir;
        private readonly ITestOutputHelper _output;
        private readonly IEsptoolWrapper _esptool;

        public Z2SFlashUpgradeIntegrationTests(ITestOutputHelper output)
        {
            _output    = output;
            _backupDir = Path.Combine(Path.GetTempPath(), $"z2s_test_backup_{Guid.NewGuid():N}");
            _esptool   = new EsptoolWrapper();
        }

        [Fact]
        public async Task FlashOlderVersion_ThenUpgrade_ValidateVersions_BackupAndRestore()
        {
            // -- 1. Load local firmware fixtures -----------------------------------
            var fixtures = LoadFirmwareFixtures();
            fixtures.Should().HaveCountGreaterThanOrEqualTo(2,
                "need at least 2 firmware versions in TestFixtures\\Z2SFirmware\\manifest.json; run Download-Firmware.ps1");

            var olderFw = fixtures[fixtures.Count - 1];
            var newerFw = fixtures[0];

            Log($"Older fixture: {olderFw.Version} - {olderFw.LocalFile} ({olderFw.ExpectedSize:N0} bytes, offset {olderFw.FlashOffset})");
            Log($"Newer fixture: {newerFw.Version} - {newerFw.LocalFile} ({newerFw.ExpectedSize:N0} bytes, offset {newerFw.FlashOffset})");

            File.Exists(olderFw.BinPath).Should().BeTrue(
                $"older firmware binary not found at '{olderFw.BinPath}'; run Download-Firmware.ps1");
            File.Exists(newerFw.BinPath).Should().BeTrue(
                $"newer firmware binary not found at '{newerFw.BinPath}'; run Download-Firmware.ps1");

            // -- 2. Flash the OLDER release ----------------------------------------
            Log($"Flashing older release {olderFw.Version} to {ComPort} at offset {olderFw.FlashOffset}...");
            var flashOldResult = await _esptool.WriteFlashAtOffset(
                ComPort, Chip, olderFw.FlashOffsetLong, olderFw.BinPath, CancellationToken.None);
            flashOldResult.Success.Should().BeTrue(
                $"flashing older release {olderFw.Version} must succeed. Error: {flashOldResult.StdErr}");

            // -- 3. Read version from SPIFFS ----------------------------------------
            Log("Waiting for device to boot after older flash...");
            await Task.Delay(TimeSpan.FromSeconds(5));
            var deviceVersionAfterOld = await ReadDeviceVersionAsync(ComPort, CancellationToken.None);
            deviceVersionAfterOld.Should().NotBeNullOrWhiteSpace(
                "device must write version.dat to SPIFFS after booting the freshly flashed firmware");
            Log($"Device version after older flash: {deviceVersionAfterOld}");

            // -- 4. Create a full-flash backup -------------------------------------
            Directory.CreateDirectory(_backupDir);
            var backupFile = Path.Combine(_backupDir, $"backup_{olderFw.Version}.bin");

            Log($"Creating backup to {backupFile}...");
            var backupResult = await _esptool.ReadFlush(ComPort, Chip, backupFile, cancellation: CancellationToken.None);

            var backupSize = new FileInfo(backupFile).Length;
            Log($"Backup file size: {backupSize:N0} bytes ({backupSize / 1024.0 / 1024:F2} MB)");
            backupSize.Should().BeGreaterThanOrEqualTo(MinBackupBytes,
                $"backup must be at least {MinBackupBytes / 1024 / 1024} MB");
            backupSize.Should().BeCloseTo(ExpectedFlashSize, (long)(ExpectedFlashSize * BackupSizeTolerance),
                $"backup size should be close to the 8 MB flash size (+/-{BackupSizeTolerance * 100:F0}%)");

            // -- 5. Flash the NEWER release ----------------------------------------
            Log($"Flashing newer release {newerFw.Version} to {ComPort} at offset {newerFw.FlashOffset}...");
            var flashNewResult = await _esptool.WriteFlashAtOffset(
                ComPort, Chip, newerFw.FlashOffsetLong, newerFw.BinPath, CancellationToken.None);
            flashNewResult.Success.Should().BeTrue(
                $"flashing newer release {newerFw.Version} must succeed. Error: {flashNewResult.StdErr}");

            // -- 6. Read version after upgrade -------------------------------------
            Log("Waiting for device to boot after newer flash...");
            await Task.Delay(TimeSpan.FromSeconds(5));
            var deviceVersionAfterNew = await ReadDeviceVersionAsync(ComPort, CancellationToken.None);
            deviceVersionAfterNew.Should().NotBeNullOrWhiteSpace(
                "device must write version.dat to SPIFFS after booting the newer firmware");
            Log($"Device version after newer flash: {deviceVersionAfterNew}");
            deviceVersionAfterNew.Should().NotBe(deviceVersionAfterOld,
                "the version string must change after upgrading to the newer firmware");

            // -- 7. Restore from backup --------------------------------------------
            Log($"Restoring device from backup {backupFile}...");
            var restoreResult = await _esptool.WriteFlush(ComPort, Chip, backupFile, CancellationToken.None);
            restoreResult.Success.Should().BeTrue(
                $"restore from backup must succeed. Error: {restoreResult.StdErr}");

            // -- 8. Verify device is responsive ------------------------------------
            Log("Verifying device is responsive after restore...");
            await Task.Delay(TimeSpan.FromSeconds(5));
            var chipIdResult = await _esptool.ReadChipId(ComPort, CancellationToken.None);
            chipIdResult.Success.Should().BeTrue(
                $"device must respond to chip-id after restore. Error: {chipIdResult.StdErr}");

            // -- 9. Verify version was restored ------------------------------------
            Log("Reading version after restore...");
            var deviceVersionAfterRestore = await ReadDeviceVersionAsync(ComPort, CancellationToken.None);
            Log($"Device version after restore: {deviceVersionAfterRestore}");
            deviceVersionAfterRestore.Should().Be(deviceVersionAfterOld,
                "version after restoring the backup should match the version before the upgrade");

            Log("All steps completed successfully.");
            try { Directory.Delete(_backupDir, true); } catch { /* best-effort */ }
        }

        [Fact]
        public async Task Backup_FileContainsSameVersion_AsFlashedFirmware()
        {
            // -- 1. Load fixtures and pick the full image --------------------------
            var fixtures = LoadFirmwareFixtures();
            var fw23full = fixtures.Find(f => f.Version == "v1.5.23-full");
            fw23full.Should().NotBeNull("v1.5.23-full must be present in manifest.json; run Download-Firmware.ps1");
            File.Exists(fw23full!.BinPath).Should().BeTrue($"v1.5.23 full binary not found at '{fw23full.BinPath}'; run Download-Firmware.ps1");

            // -- 2. Flash the known full image -------------------------------------
            Log($"Flashing {fw23full.Version} to {ComPort} at offset {fw23full.FlashOffset}...");
            var flashResult = await _esptool.WriteFlashAtOffset(ComPort, Chip, fw23full.FlashOffsetLong, fw23full.BinPath, CancellationToken.None);
            flashResult.Success.Should().BeTrue($"flashing {fw23full.Version} must succeed. Error: {flashResult.StdErr}");

            // -- 3. Read version from the live device ------------------------------
            Log("Waiting for device to boot...");
            await Task.Delay(TimeSpan.FromSeconds(5));
            var deviceVersion = await ReadDeviceVersionWithRetryAsync(ComPort, cancellationToken: CancellationToken.None);
            deviceVersion.Should().NotBeNullOrWhiteSpace("device must report a version after booting the flashed firmware");
            Log($"Device version: {deviceVersion}");

            // -- 4. Create a backup of the full flash ------------------------------
            Directory.CreateDirectory(_backupDir);
            var backupFile = Path.Combine(_backupDir, $"backup_version_check.bin");

            Log($"Creating backup to {backupFile}...");
            var backupResult = await _esptool.ReadFlush(ComPort, Chip, backupFile, cancellation: CancellationToken.None);
            backupResult.Success.Should().BeTrue($"backup must succeed. Error: {backupResult.StdErr}");
            File.Exists(backupFile).Should().BeTrue("backup file must exist on disk");

            // -- 5. Parse version from backup file ---------------------------------
            Log("Parsing version from backup file...");
            const long defaultSpiffsOffset = 0x290000;
            const long defaultSpiffsSize   = 0x170000;
            const long spiffsScanSize      = 0x10000;

            var backupBytes = File.ReadAllBytes(backupFile);

            // Locate the SPIFFS partition from the partition table embedded in the backup
            // (partition table lives at 0x8000 in a full flash image, 0xC00 bytes long)
            const int ptableOffset = 0x8000;
            const int ptableSize   = 0xC00;
            backupBytes.Length.Should().BeGreaterThan(ptableOffset + ptableSize,
                "backup file must contain the partition table at 0x8000");

            var ptableBytes = new byte[ptableSize];
            Array.Copy(backupBytes, ptableOffset, ptableBytes, 0, ptableSize);
            var (foundOffset, foundSize) = ParsePartitionTable(ptableBytes);

            long spiffsOffset = foundOffset > 0 ? foundOffset : defaultSpiffsOffset;
            long spiffsSize   = foundOffset > 0 ? foundSize   : defaultSpiffsSize;
            Log($"SPIFFS offset from backup partition table: 0x{spiffsOffset:X}, size 0x{spiffsSize:X}");

            var scanSize = (int)Math.Min(spiffsSize, spiffsScanSize);
            backupBytes.Length.Should().BeGreaterThan((int)spiffsOffset + scanSize,
                "backup file must be large enough to contain the SPIFFS region");

            var spiffsChunk = new byte[scanSize];
            Array.Copy(backupBytes, spiffsOffset, spiffsChunk, 0, scanSize);
            var backupVersion = SpiffsVersionParser.FindVersion(spiffsChunk);

            Log($"Version parsed from backup file: {backupVersion}");

            // -- 6. Assert versions match ------------------------------------------
            backupVersion.Should().NotBeNullOrWhiteSpace("backup binary must contain a parseable version string");
            backupVersion.Should().Be(deviceVersion,
                "the version stored in the backup file must match the version read from the live device");

            Log("Version in backup matches live device version. Test passed.");
            try { Directory.Delete(_backupDir, true); } catch { /* best-effort */ }
        }

        [Fact]
        public async Task Flash_v1_5_23_Full_Then_v1_5_24_Then_v1_5_25_ValidateEachVersion()
        {
            // -- Load specific fixture versions ------------------------------------
            var all = LoadFirmwareFixtures();

            var fw23full = all.Find(f => f.Version == "v1.5.23-full");
            var fw23     = all.Find(f => f.Version == "v1.5.23");
            var fw24     = all.Find(f => f.Version == "v1.5.24");
            var fw25     = all.Find(f => f.Version == "v1.5.25");

            fw23full.Should().NotBeNull("v1.5.23-full must be present in manifest.json; run Download-Firmware.ps1");
            fw23.Should().NotBeNull("v1.5.23 must be present in manifest.json");
            fw24.Should().NotBeNull("v1.5.24 must be present in manifest.json");
            fw25.Should().NotBeNull("v1.5.25 must be present in manifest.json");

            File.Exists(fw23full!.BinPath).Should().BeTrue($"v1.5.23 full binary not found at '{fw23full.BinPath}'; run Download-Firmware.ps1");
            File.Exists(fw23!.BinPath).Should().BeTrue($"v1.5.23 binary not found at '{fw23.BinPath}'; run Download-Firmware.ps1");
            File.Exists(fw24!.BinPath).Should().BeTrue($"v1.5.24 binary not found at '{fw24.BinPath}'; run Download-Firmware.ps1");
            File.Exists(fw25!.BinPath).Should().BeTrue($"v1.5.25 binary not found at '{fw25.BinPath}'; run Download-Firmware.ps1");

            // -- Step 1: erase flash to ensure a clean known state ----------------
            Log($"Erasing flash on {ComPort} before initial full flash...");
            var eraseResult = await _esptool.EraseFlash(ComPort, Chip, CancellationToken.None);
            eraseResult.Success.Should().BeTrue($"erase flash must succeed. Error: {eraseResult.StdErr}");

            // -- Step 2: flash v1.5.23 full image at offset 0x0 -------------------
            Log($"Flashing {fw23full.Version} (full) to {ComPort} at offset {fw23full.FlashOffset}...");
            var flash23 = await _esptool.WriteFlashAtOffset(ComPort, Chip, fw23full.FlashOffsetLong, fw23full.BinPath, CancellationToken.None);
            flash23.Success.Should().BeTrue($"flashing {fw23full.Version} full image must succeed. Error: {flash23.StdErr}");

            Log($"Waiting for device to boot after {fw23full.Version}...");
            await Task.Delay(TimeSpan.FromSeconds(5));
            var version23 = await ReadDeviceVersionWithRetryAsync(ComPort, cancellationToken: CancellationToken.None);
            version23.Should().NotBeNullOrWhiteSpace($"device must report a version after flashing {fw23full.Version}");
            Log($"Device version after {fw23full.Version}: {version23}");

            // -- Step 3: backup after v1.5.23 -------------------------------------
            Directory.CreateDirectory(_backupDir);
            var backupFile = Path.Combine(_backupDir, "backup_v1.5.23.bin");

            Log($"Creating backup to {backupFile}...");
            var backupResult = await _esptool.ReadFlush(ComPort, Chip, backupFile, cancellation: CancellationToken.None);
            backupResult.Success.Should().BeTrue($"backup must succeed. Error: {backupResult.StdErr}");
            File.Exists(backupFile).Should().BeTrue("backup file must exist on disk");

            var backupSize = new FileInfo(backupFile).Length;
            Log($"Backup size: {backupSize:N0} bytes ({backupSize / 1024.0 / 1024:F2} MB)");
            backupSize.Should().BeGreaterThanOrEqualTo(MinBackupBytes, $"backup must be at least {MinBackupBytes / 1024 / 1024} MB");
            backupSize.Should().BeCloseTo(ExpectedFlashSize, (long)(ExpectedFlashSize * BackupSizeTolerance),
                $"backup size should be close to the 8 MB flash size (+/-{BackupSizeTolerance * 100:F0}%)");

            // -- Step 3: upgrade to v1.5.24 ---------------------------------------
            Log($"Upgrading from {fw23.Version} to {fw24.Version} at offset {fw24.FlashOffset}...");
            var flash24 = await _esptool.WriteFlashAtOffset(ComPort, Chip, fw24.FlashOffsetLong, fw24.BinPath, CancellationToken.None);
            flash24.Success.Should().BeTrue($"flashing {fw24.Version} must succeed. Error: {flash24.StdErr}");

            Log($"Waiting for device to boot after {fw24.Version}...");
            await Task.Delay(TimeSpan.FromSeconds(5));
            var version24 = await ReadDeviceVersionWithRetryAsync(ComPort, cancellationToken: CancellationToken.None);
            version24.Should().NotBeNullOrWhiteSpace($"device must report a version after flashing {fw24.Version}");
            Log($"Device version after {fw24.Version}: {version24}");
            version24.Should().NotBe(version23, $"version must change after upgrading from {fw23.Version} to {fw24.Version}");

            // -- Step 4: upgrade to v1.5.25 ---------------------------------------
            Log($"Upgrading from {fw24.Version} to {fw25.Version} at offset {fw25.FlashOffset}...");
            var flash25 = await _esptool.WriteFlashAtOffset(ComPort, Chip, fw25.FlashOffsetLong, fw25.BinPath, CancellationToken.None);
            flash25.Success.Should().BeTrue($"flashing {fw25.Version} must succeed. Error: {flash25.StdErr}");

            Log($"Waiting for device to boot after {fw25.Version}...");
            await Task.Delay(TimeSpan.FromSeconds(5));
            var version25 = await ReadDeviceVersionWithRetryAsync(ComPort, cancellationToken: CancellationToken.None);
            version25.Should().NotBeNullOrWhiteSpace($"device must report a version after flashing {fw25.Version}");
            Log($"Device version after {fw25.Version}: {version25}");
            version25.Should().NotBe(version24, $"version must change after upgrading from {fw24.Version} to {fw25.Version}");

            // -- Step 5: restore backup from v1.5.23 ------------------------------
            Log($"Restoring device from backup {backupFile}...");
            var restoreResult = await _esptool.WriteFlush(ComPort, Chip, backupFile, CancellationToken.None);
            restoreResult.Success.Should().BeTrue($"restore from backup must succeed. Error: {restoreResult.StdErr}");

            Log("Verifying device is responsive after restore...");
            await Task.Delay(TimeSpan.FromSeconds(5));
            var chipIdResult = await _esptool.ReadChipId(ComPort, CancellationToken.None);
            chipIdResult.Success.Should().BeTrue($"device must respond to chip-id after restore. Error: {chipIdResult.StdErr}");

            // -- Step 6: verify version reverted to v1.5.23 -----------------------
            Log("Reading version after restore...");
            var versionAfterRestore = await ReadDeviceVersionWithRetryAsync(ComPort, cancellationToken: CancellationToken.None);
            Log($"Device version after restore: {versionAfterRestore}");
            versionAfterRestore.Should().Be(version23,
                "version after restoring the v1.5.23 backup should match the version read when v1.5.23 was first flashed");

            Log("All steps completed successfully.");
            try { Directory.Delete(_backupDir, true); } catch { /* best-effort */ }
        }

        // -- Helpers ---------------------------------------------------------------

        private void Log(string message) =>
            _output.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");

        private static string FindFixtureDirectory()
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            while (dir is not null)
            {
                var candidate = Path.Combine(dir, "TestFixtures", "Z2SFirmware");
                if (Directory.Exists(candidate))
                    return candidate;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }

        private static List<FirmwareFixture> LoadFirmwareFixtures()
        {
            var fixtureDir = FindFixtureDirectory();
            fixtureDir.Should().NotBeNullOrWhiteSpace(
                "could not locate TestFixtures\\Z2SFirmware directory relative to the test assembly");

            var manifestPath = Path.Combine(fixtureDir!, "manifest.json");
            File.Exists(manifestPath).Should().BeTrue($"manifest.json not found at '{manifestPath}'");

            var manifest = JsonSerializer.Deserialize<FirmwareManifest>(
                File.ReadAllText(manifestPath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            manifest.Should().NotBeNull();
            manifest!.FirmwareFiles.Should().NotBeNullOrEmpty();

            var result = new List<FirmwareFixture>();
            foreach (var entry in manifest.FirmwareFiles)
            {
                result.Add(new FirmwareFixture
                {
                    Version         = entry.Version,
                    LocalFile       = entry.LocalFile,
                    FlashOffset     = entry.FlashOffset,
                    ExpectedSize    = entry.ExpectedSize,
                    BinPath         = Path.Combine(fixtureDir, entry.LocalFile),
                    FlashOffsetLong = Convert.ToInt64(entry.FlashOffset.Replace("0x", ""), 16)
                });
            }
            return result;
        }

        private async Task<string> ReadDeviceVersionWithRetryAsync(
            string comPort, int maxAttempts = 6, int delayBetweenSeconds = 5,
            CancellationToken cancellationToken = default)
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var version = await ReadDeviceVersionAsync(comPort, cancellationToken);
                if (!string.IsNullOrWhiteSpace(version))
                    return version;

                if (attempt < maxAttempts)
                {
                    Log($"Version not found yet (attempt {attempt}/{maxAttempts}), retrying in {delayBetweenSeconds}s...");
                    await Task.Delay(TimeSpan.FromSeconds(delayBetweenSeconds), cancellationToken);
                }
            }
            return null;
        }

        private async Task<string> ReadDeviceVersionAsync(string comPort, CancellationToken cancellationToken)
        {
            const long defaultSpiffsOffset = 0x290000;
            const long defaultSpiffsSize   = 0x170000;
            const long spiffsScanSize      = 0x10000;

            var tempDir = Path.Combine(Path.GetTempPath(), $"z2s_ver_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                long spiffsOffset = defaultSpiffsOffset;
                long spiffsSize   = defaultSpiffsSize;

                var ptableFile   = Path.Combine(tempDir, "ptable.bin");
                var ptableResult = await _esptool.ReadFlashRegion(comPort, 0x8000, 0xC00, ptableFile, cancellationToken);

                if (ptableResult.Success && File.Exists(ptableFile))
                {
                    var (foundOffset, foundSize) = ParsePartitionTable(File.ReadAllBytes(ptableFile));
                    if (foundOffset > 0)
                    {
                        spiffsOffset = foundOffset;
                        spiffsSize   = foundSize;
                        Log($"SPIFFS at 0x{spiffsOffset:X}, size 0x{spiffsSize:X}");
                    }
                }

                var scanSize     = Math.Min(spiffsSize, spiffsScanSize);
                var spiffsFile   = Path.Combine(tempDir, "spiffs_chunk.bin");
                var spiffsResult = await _esptool.ReadFlashRegion(comPort, spiffsOffset, scanSize, spiffsFile, cancellationToken);

                if (!spiffsResult.Success || !File.Exists(spiffsFile))
                    return null;

                return SpiffsVersionParser.FindVersion(File.ReadAllBytes(spiffsFile));
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { /* best-effort */ }
            }
        }

        private static (long offset, long size) ParsePartitionTable(byte[] bytes)
        {
            for (int i = 0; i + 32 <= bytes.Length; i += 32)
            {
                if (bytes[i] != 0xAA || bytes[i + 1] != 0x50) continue;
                if (bytes[i + 2] != 0x01) continue;

                var name     = Encoding.ASCII.GetString(bytes, i + 12, 16).TrimEnd('\0');
                byte subtype = bytes[i + 3];

                bool isSpiffs = name.Contains("spiff", StringComparison.OrdinalIgnoreCase)
                             || name.Contains("littlefs", StringComparison.OrdinalIgnoreCase)
                             || subtype == 0x82;
                if (!isSpiffs) continue;

                long offset = BitConverter.ToUInt32(bytes, i + 4);
                long size   = BitConverter.ToUInt32(bytes, i + 8);
                return (offset, size);
            }
            return (0, 0);
        }

        // -- DTOs ------------------------------------------------------------------

        private sealed class FirmwareManifest
        {
            public string        Description   { get; set; }
            public List<FwEntry> FirmwareFiles { get; set; } = [];
        }

        private sealed class FwEntry
        {
            public string Version      { get; set; }
            public string LocalFile    { get; set; }
            public string FlashOffset  { get; set; }
            public long   ExpectedSize { get; set; }
        }

        private sealed class FirmwareFixture
        {
            public string Version         { get; set; }
            public string LocalFile       { get; set; }
            public string FlashOffset     { get; set; }
            public long   ExpectedSize    { get; set; }
            public string BinPath         { get; set; }
            public long   FlashOffsetLong { get; set; }
        }
    }
}