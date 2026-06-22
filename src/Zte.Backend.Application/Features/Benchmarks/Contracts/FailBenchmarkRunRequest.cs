namespace Zte.Backend.Application.Features.Benchmarks.Contracts;

public sealed class FailBenchmarkRunRequest
{
    public required Guid BenchmarkRunId { get; init; }

    public required string ErrorMessage { get; init; }
}