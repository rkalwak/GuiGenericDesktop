public record EsptoolResult
{
    public bool Success { get; init; }
    public string? Command { get; init; }
    public int ExitCode { get; init; }
    public string? StdOut { get; init; }
    public string? StdErr { get; init; }
}


