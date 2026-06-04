using Zte.Backend.Domain.Measurements;

namespace Zte.Backend.Application.Measurements;

public interface IMeasurementStore
{
    void Add(VerificationMeasurement measurement);

    IReadOnlyList<VerificationMeasurement> GetAll();
}