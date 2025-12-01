namespace CompilationLib
{
    public interface ICompileHandler
    {
        Task<CompileResponse> Handle(CompileRequest a, CancellationToken none);
    }
}