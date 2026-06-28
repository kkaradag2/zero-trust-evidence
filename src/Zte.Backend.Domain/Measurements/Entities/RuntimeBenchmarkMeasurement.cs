namespace Zte.Backend.Domain.Measurements.Entities;

public sealed record RuntimeBenchmarkMeasurement(
    Guid Id,
    Guid BenchmarkRunId,
    string PolicyK,
    string PolicyLabel,
    int RunIndex,
    int TotalRequests,
    bool AttestationPerformed,
    double ChallengeFetchMs,
    double DeviceSigningMs,
    double BackendVerificationMs,
    double FreshProofCostMs,
    double OperationTotalMs,
    int RequestPayloadBytes,
    int ResponsePayloadBytes,
    bool Success,
    string? ErrorMessage,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ClientTimestampUtc);
