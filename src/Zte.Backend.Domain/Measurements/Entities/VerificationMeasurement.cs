using Zte.Backend.Domain.Attestation.Enums;
using Zte.Backend.Domain.Measurements.Enums;

namespace Zte.Backend.Domain.Measurements.Entities;

public sealed record VerificationMeasurement(
    Guid Id,
    Guid? BenchmarkRunId,
    MeasurementPhase? Phase,
    AttestationType AttestationType,
    bool Accepted,
    RiskLevel RiskLevel,
    double VerificationTimeMs,
    long VerificationTimeMicroseconds,
    int MessageSizeBytes,
    int ProcessingStepCount,
    DateTime CreatedAtUtc);