
public class CompileResponse
{
    public List<string> Progress { get; set; } = new List<string>();
    public int ExitCode { get; internal set; }
    public double ElapsedTimeInSeconds { get; internal set; }
    public string OutputDirectory { get; internal set; }
    public string OutputFile { get; internal set; }

    public override string ToString()
    {
        return $"ExitCode: {ExitCode}, CompilationTime: {ElapsedTimeInSeconds}, Progress: {string.Join("\n", Progress)}";
    }
}