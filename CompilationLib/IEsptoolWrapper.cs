namespace CompilationLib
{
    public interface IEsptoolWrapper
    {
        Task<EsptoolResult> ReadChipId(string comPort, CancellationToken cancellation = default);
        Task<EsptoolResult> ReadFlashId(string comPort, CancellationToken cancellation = default);
    }
}