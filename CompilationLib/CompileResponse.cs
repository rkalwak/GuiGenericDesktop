public class CompileResponse
{
    public List<string> Progress { get; set; } = new List<string>();
    public double ElapsedTimeInSeconds { get; internal set; }
    public string OutputDirectory { get; internal set; }
    public string OutputFile { get; internal set; }
    public string Logs { get; internal set; }
    public bool IsSuccessful { get; set; }
    public string BackupFilePath { get; set; }

    public override string ToString()
    {
        return $"IsSuccessful: {IsSuccessful}, CompilationTime: {ElapsedTimeInSeconds}, Progress: {string.Join("\n", Progress)}";
    }
}