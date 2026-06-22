using Zte.Backend.Domain.Benchmarks.Entities;
using Zte.Backend.Domain.Benchmarks.Enums;
using Zte.Backend.Domain.Benchmarks.ValueObjects;

namespace Zte.Backend.Application.Common.Interfaces;

public interface IBenchmarkRunStore
{
    BenchmarkRun Create(
        BenchmarkType type,
        int iterationCount,
        BenchmarkDeviceInfo? mobileDevice,
        BenchmarkBackendSystemInfo? backendSystem);

    IReadOnlyList<BenchmarkRun> List();

    BenchmarkRun? Find(Guid benchmarkRunId);

    BenchmarkRun? Complete(Guid benchmarkRunId);

    BenchmarkRun? Fail(Guid benchmarkRunId, string errorMessage);
}
