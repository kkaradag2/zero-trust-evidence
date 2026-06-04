using Zte.Backend.Application.Measurements;
using Zte.Backend.Domain.Measurements;

namespace Zte.Backend.Infrastructure.Measurements;

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
}