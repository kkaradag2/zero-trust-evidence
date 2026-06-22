namespace Zte.Backend.Domain.Benchmarks.ValueObjects;

public sealed class BenchmarkBackendSystemInfo
{
    public string MachineName { get; init; } = string.Empty;

    public string OsDescription { get; init; } = string.Empty;

    public string ProcessArchitecture { get; init; } = string.Empty;

    public int ProcessorCount { get; init; }

    public long? TotalAvailableMemoryBytes { get; init; }
}
