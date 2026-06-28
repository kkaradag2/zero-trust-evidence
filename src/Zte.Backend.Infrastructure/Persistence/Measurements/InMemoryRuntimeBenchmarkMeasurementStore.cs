using Zte.Backend.Application.Common.Interfaces;
using Zte.Backend.Domain.Measurements.Entities;

namespace Zte.Backend.Infrastructure.Persistence.Measurements;

public sealed class InMemoryRuntimeBenchmarkMeasurementStore : IRuntimeBenchmarkMeasurementStore
{
    private readonly List<RuntimeBenchmarkMeasurement> _measurements = [];
    private readonly object _lock = new();

    public void Add(RuntimeBenchmarkMeasurement measurement)
    {
        lock (_lock)
        {
            _measurements.Add(measurement);
        }
    }

    public void AddRange(IEnumerable<RuntimeBenchmarkMeasurement> measurements)
    {
        lock (_lock)
        {
            _measurements.AddRange(measurements);
        }
    }

    public IReadOnlyList<RuntimeBenchmarkMeasurement> GetByBenchmarkRunId(Guid benchmarkRunId)
    {
        lock (_lock)
        {
            return _measurements
                .Where(measurement => measurement.BenchmarkRunId == benchmarkRunId)
                .OrderBy(measurement => measurement.RunIndex)
                .ToList();
        }
    }
}
