namespace Zte.Backend.Application.Features.HardwareAttestation.Contracts;

public sealed class HardwareAttestationResult
{
    public required bool IsAccepted { get; init; }

    public required string RiskLevel { get; init; }

    public required string Decision { get; init; }

    public required string Reason { get; init; }

    public required int ProcessingStepCount { get; init; }
}