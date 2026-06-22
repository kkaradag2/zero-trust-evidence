using Zte.Backend.Domain.Attestation.Enums;

namespace Zte.Backend.Application.Features.HardwareAttestation.Contracts;

public sealed record HardwareEnrollmentResult(
    bool Accepted,
    Guid? EnrolledDeviceId,
    RiskLevel RiskLevel,
    IReadOnlyList<string> VerificationSteps,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> Warnings,
    string? HardwareSecurityLevel,
    string? AttestationSecurityLevel,
    int ProcessingStepCount,
    double VerificationTimeMs,
    long VerificationTimeMicroseconds,
    int MessageSizeBytes);
