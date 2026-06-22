using Zte.Backend.Application.Features.HardwareAttestation.Contracts;

namespace Zte.Backend.Application.Features.HardwareAttestation.Services;

public interface IHardwareAttestationEnrollmentService
{
    HardwareEnrollmentResult Enroll(
        HardwareEnrollmentRequest request,
        int messageSizeBytes);
}
