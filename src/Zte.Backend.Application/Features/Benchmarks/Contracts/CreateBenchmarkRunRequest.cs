using Zte.Backend.Domain.Benchmarks.Entities;
using Zte.Backend.Domain.Benchmarks.Enums;
using Zte.Backend.Domain.Benchmarks.ValueObjects;

namespace Zte.Backend.Application.Features.Benchmarks.Contracts;

public sealed class CreateBenchmarkRunRequest
{
    public BenchmarkType Type { get; init; } = BenchmarkType.Comparative;

    public int? IterationCount { get; init; }

    public BenchmarkDeviceInfo? MobileDevice { get; init; }
}
