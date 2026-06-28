using Zte.Backend.Domain.Measurements.Entities;

namespace Zte.Backend.Application.Common.Interfaces;

public interface IRuntimeBenchmarkMeasurementStore
{
    void Add(RuntimeBenchmarkMeasurement measurement);

    void AddRange(IEnumerable<RuntimeBenchmarkMeasurement> measurements);

    IReadOnlyList<RuntimeBenchmarkMeasurement> GetByBenchmarkRunId(Guid benchmarkRunId);
}
