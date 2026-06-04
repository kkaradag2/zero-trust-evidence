using Zte.Backend.Domain.Attestation;

namespace Zte.Backend.Domain.Measurements;

public sealed record VerificationMeasurement(
    Guid Id,
    AttestationType AttestationType,
    bool Accepted,
    RiskLevel RiskLevel,
    double VerificationTimeMs,
    long VerificationTimeMicroseconds,
    int MessageSizeBytes,
    int ProcessingStepCount,
    DateTime CreatedAtUtc);