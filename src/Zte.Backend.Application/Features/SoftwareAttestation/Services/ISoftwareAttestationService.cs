using Zte.Backend.Application.Features.SoftwareAttestation.Contracts;
using Zte.Backend.Domain.Attestation.Enums;
using Zte.Backend.Domain.Attestation.ValueObjects;

namespace Zte.Backend.Application.Features.SoftwareAttestation.Services;

public interface ISoftwareAttestationService
{
    VerificationResult Verify(SoftwareAttestationRequest request, int messageSizeBytes);
}
