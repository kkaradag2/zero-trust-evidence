using Zte.Backend.Domain.Attestation;

namespace Zte.Backend.Application.SoftwareAttestation;

public interface ISoftwareAttestationService
{
    VerificationResult Verify(SoftwareAttestationRequest request, int messageSizeBytes);
}