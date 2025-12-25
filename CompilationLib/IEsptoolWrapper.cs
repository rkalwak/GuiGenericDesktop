namespace CompilationLib
{
    public interface IEsptoolWrapper
    {
        Task<EsptoolResult> ReadChipId(string comPort, CancellationToken cancellation = default);
        Task<EsptoolResult> ReadFlashId(string comPort, CancellationToken cancellation = default);
        Task<EsptoolResult> ReadFlush(string comPort, string chip, string backupFile, CancellationToken cancellation = default);
        Task<EsptoolResult> WriteFlush(string comPort, string chip, string binFile, CancellationToken cancellation = default);
    }
}