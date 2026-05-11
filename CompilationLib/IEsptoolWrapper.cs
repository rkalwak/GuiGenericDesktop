namespace CompilationLib
{
    public interface IEsptoolWrapper
    {
        Task<EsptoolResult> ReadChipId(string comPort, CancellationToken cancellation = default);
        Task<EsptoolResult> ReadFlashId(string comPort, CancellationToken cancellation = default);
        Task<EsptoolResult> ReadFlush(string comPort, string chip, string backupFile, string flashSize = null, CancellationToken cancellation = default);
        Task<EsptoolResult> WriteFlush(string comPort, string chip, string binFile, CancellationToken cancellation = default);
        
        /// <summary>
        /// Merges partition.bin, bootloader.bin and firmware.bin into a single complete firmware file
        /// Addresses are parsed from the partition CSV file to ensure accuracy.
        /// </summary>
        /// <param name="buildOutputDirectory">Directory containing the bin files</param>
        /// <param name="outputFilePath">Path where the merged file should be saved</param>
        /// <param name="platform">Platform name (e.g., "GUI_Generic_ESP32")</param>
        /// <param name="flashSize">Flash size (e.g., "4MB", "8MB")</param>
        /// <param name="repositoryPath">Path to GUI-Generic repository (to find partition CSV)</param>
        /// <returns>Path to the merged file if successful, null otherwise</returns>
        Task<string> MergeFirmwareFiles(string buildOutputDirectory, string outputFilePath, string platform, string flashSize, string board, string repositoryPath = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads a specific region of the device flash memory to a file.
        /// </summary>
        /// <param name="comPort">COM port the device is connected to</param>
        /// <param name="offset">Flash offset to start reading from</param>
        /// <param name="size">Number of bytes to read</param>
        /// <param name="outputFile">Path to save the read data</param>
        Task<EsptoolResult> ReadFlashRegion(string comPort, long offset, long size, string outputFile, CancellationToken cancellation = default);

        /// <summary>
        /// Erases the entire flash memory of the device.
        /// </summary>
        Task<EsptoolResult> EraseFlash(string comPort, string chip, CancellationToken cancellation = default);

        /// <summary>
        /// Writes a binary file to the device flash at a specific byte offset.
        /// Used for OTA-only firmware images that must land in the app0 partition.
        /// </summary>
        Task<EsptoolResult> WriteFlashAtOffset(string comPort, string chip, long offset, string binFile, CancellationToken cancellation = default);
    }
}