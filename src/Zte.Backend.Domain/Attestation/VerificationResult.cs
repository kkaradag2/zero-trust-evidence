namespace Zte.Backend.Domain.Attestation;

public sealed record VerificationResult(
    bool Accepted,
    AttestationType AttestationType,
    RiskLevel RiskLevel,
    int ProcessingStepCount,
    long VerificationTimeMs,
    int MessageSizeBytes,
    IReadOnlyList<string> Reasons);