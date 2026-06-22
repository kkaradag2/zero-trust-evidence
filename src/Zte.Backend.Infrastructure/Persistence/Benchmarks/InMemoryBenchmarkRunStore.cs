using System.Collections.Concurrent;
using Zte.Backend.Application.Common.Interfaces;
using Zte.Backend.Domain.Benchmarks.Entities;
using Zte.Backend.Domain.Benchmarks.Enums;
using Zte.Backend.Domain.Benchmarks.ValueObjects;

namespace Zte.Backend.Infrastructure.Persistence.Benchmarks;

public sealed class InMemoryBenchmarkRunStore : IBenchmarkRunStore
{
    private readonly ConcurrentDictionary<Guid, BenchmarkRun> _runs = new();
    private int _nextRunNumber;

    public BenchmarkRun Create(
        BenchmarkType type,
        int iterationCount,
        BenchmarkDeviceInfo? mobileDevice,
        BenchmarkBackendSystemInfo? backendSystem)
    {
        var runNumber = Interlocked.Increment(ref _nextRunNumber);

        var run = new BenchmarkRun
        {
            Id = Guid.NewGuid(),
            Code = $"BCK-{runNumber:0000}",
            Type = type,
            Status = BenchmarkStatus.Running,
            IterationCount = iterationCount,
            SoftwareIterationCount = iterationCount,
            HardwareVerificationIterationCount = iterationCount,
            HardwareEnrollmentCount = 1,
            MobileDevice = mobileDevice,
            BackendSystem = backendSystem,
            StartedAtUtc = DateTimeOffset.UtcNow,
            CompletedAtUtc = null,
            ErrorMessage = null
        };

        _runs[run.Id] = run;

        return run;
    }

    public IReadOnlyList<BenchmarkRun> List()
    {
        return _runs.Values
            .OrderByDescending(run => run.StartedAtUtc)
            .ToList();
    }

    public BenchmarkRun? Find(Guid benchmarkRunId)
    {
        _runs.TryGetValue(benchmarkRunId, out var run);

        return run;
    }

    public BenchmarkRun? Complete(Guid benchmarkRunId)
    {
        if (!_runs.TryGetValue(benchmarkRunId, out var run))
        {
            return null;
        }

        run.Status = BenchmarkStatus.Completed;
        run.CompletedAtUtc = DateTimeOffset.UtcNow;
        run.ErrorMessage = null;

        return run;
    }

    public BenchmarkRun? Fail(Guid benchmarkRunId, string errorMessage)
    {
        if (!_runs.TryGetValue(benchmarkRunId, out var run))
        {
            return null;
        }

        run.Status = BenchmarkStatus.Failed;
        run.CompletedAtUtc = DateTimeOffset.UtcNow;
        run.ErrorMessage = errorMessage;

        return run;
    }
}
