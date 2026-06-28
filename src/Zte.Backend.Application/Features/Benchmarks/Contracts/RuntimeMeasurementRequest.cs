namespace Zte.Backend.Application.Features.Benchmarks.Contracts;

public sealed class RuntimeMeasurementRequest
{
    public string? PolicyK { get; init; }

    public string? PolicyLabel { get; init; }

    public int RunIndex { get; init; }

    public int TotalRequests { get; init; }

    public bool AttestationPerformed { get; init; }

    public double ChallengeFetchMs { get; init; }

    public double DeviceSigningMs { get; init; }

    public double BackendVerificationMs { get; init; }

    public double FreshProofCostMs { get; init; }

    public double OperationTotalMs { get; init; }

    public int RequestPayloadBytes { get; init; }

    public int ResponsePayloadBytes { get; init; }

    public bool Success { get; init; }

    public string? ErrorMessage { get; init; }

    public DateTimeOffset Timestamp { get; init; }
}
