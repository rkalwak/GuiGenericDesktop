namespace CompilationLib
{
    public interface IEsptoolWrapper
    {
        Task<EsptoolResult> ReadChipId(string comPort, CancellationToken cancellation = default);
        Task<EsptoolResult> ReadFlashId(string comPort, CancellationToken cancellation = default);
        Task<EsptoolResult> ReadFlush(string comPort, string chip, string backupFile, CancellationToken cancellation = default);
        Task<EsptoolResult> WriteFlush(string comPort, string chip, string binFile, CancellationToken cancellation = default);
        
        /// <summary>
        /// Merges partition.bin, bootloader.bin and firmware.bin into a single complete firmware file
        /// </summary>
        /// <param name="buildOutputDirectory">Directory containing the bin files</param>
        /// <param name="outputFilePath">Path where the merged file should be saved</param>
        /// <param name="platform">Platform name (e.g., "GUI_Generic_ESP32")</param>
        /// <returns>Path to the merged file if successful, null otherwise</returns>
        Task<string> MergeFirmwareFiles(string buildOutputDirectory, string outputFilePath, string platform);
    }
}