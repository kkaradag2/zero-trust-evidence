using Zte.Backend.Domain.Attestation;

namespace Zte.Backend.Application.HardwareAttestation;

public interface IHardwareAttestationService
{
    VerificationResult Enroll(
        HardwareEnrollmentRequest request,
        int messageSizeBytes);

    VerificationResult Verify(
        HardwareVerificationRequest request,
        int messageSizeBytes);
}