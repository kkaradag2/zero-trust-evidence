namespace Zte.Backend.Application.Features.Benchmarks.Contracts;

public sealed class CompleteBenchmarkRunRequest
{
    public required Guid BenchmarkRunId { get; init; }
}