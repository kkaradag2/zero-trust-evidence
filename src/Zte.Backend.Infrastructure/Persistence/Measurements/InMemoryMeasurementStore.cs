using Zte.Backend.Application.Common.Interfaces;
using Zte.Backend.Domain.Measurements.Entities;
using Zte.Backend.Domain.Measurements.Enums;

namespace Zte.Backend.Infrastructure.Persistence.Measurements;

public sealed class InMemoryMeasurementStore : IMeasurementStore
{
    private readonly List<VerificationMeasurement> _measurements = [];
    private readonly object _lock = new();

    public void Add(VerificationMeasurement measurement)
    {
        lock (_lock)
        {
            _measurements.Add(measurement);
        }
    }

    public IReadOnlyList<VerificationMeasurement> GetAll()
    {
        lock (_lock)
        {
            return _measurements
                .OrderByDescending(measurement => measurement.CreatedAtUtc)
                .ToList();
        }
    }


    public IReadOnlyList<VerificationMeasurement> ListByBenchmarkRunId(Guid benchmarkRunId)
    {
        return _measurements
            .Where(measurement => measurement.BenchmarkRunId == benchmarkRunId)
            .OrderBy(measurement => measurement.CreatedAtUtc)
            .ToList();
    }

}