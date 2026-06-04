namespace Zte.Backend.Domain.Attestation;

public sealed record VerificationResult(
    bool Accepted,
    AttestationType AttestationType,
    RiskLevel RiskLevel,
    int ProcessingStepCount,
    double VerificationTimeMs,
    long VerificationTimeMicroseconds,
    int MessageSizeBytes,
    IReadOnlyList<string> Reasons);