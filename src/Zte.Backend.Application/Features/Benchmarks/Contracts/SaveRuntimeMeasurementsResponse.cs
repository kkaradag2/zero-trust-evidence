namespace Zte.Backend.Application.Features.Benchmarks.Contracts;

public sealed record SaveRuntimeMeasurementsResponse(
    Guid BenchmarkRunId,
    int SavedCount);
