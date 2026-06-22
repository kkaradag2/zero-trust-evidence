using Zte.Backend.Application.Features.HardwareAttestation.Contracts;
using Zte.Backend.Domain.Attestation.Enums;
using Zte.Backend.Domain.Attestation.ValueObjects;

namespace Zte.Backend.Application.Features.HardwareAttestation.Services;

public interface IHardwareAttestationService
{
    VerificationResult Enroll(
        HardwareEnrollmentRequest request,
        int messageSizeBytes);

    VerificationResult Verify(
        HardwareVerificationRequest request,
        int messageSizeBytes);
}
