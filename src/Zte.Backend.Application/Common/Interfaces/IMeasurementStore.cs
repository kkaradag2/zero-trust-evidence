using Zte.Backend.Domain.Measurements.Entities;
using Zte.Backend.Domain.Measurements.Enums;

namespace Zte.Backend.Application.Common.Interfaces;

public interface IMeasurementStore
{
    void Add(VerificationMeasurement measurement);

    IReadOnlyList<VerificationMeasurement> GetAll();

    IReadOnlyList<VerificationMeasurement> ListByBenchmarkRunId(Guid benchmarkRunId);
}