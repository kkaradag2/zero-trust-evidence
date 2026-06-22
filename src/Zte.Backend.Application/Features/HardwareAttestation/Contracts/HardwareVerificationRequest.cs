namespace Zte.Backend.Application.Features.HardwareAttestation.Contracts;

public sealed class HardwareVerificationRequest
{
    public Guid? BenchmarkRunId { get; init; }

    public required string ChallengeId { get; init; }

    public required string Nonce { get; init; }

    public required string DeviceId { get; init; }

    public required string KeyAlias { get; init; }

    public required string SignatureBase64 { get; init; }

    public required DateTimeOffset ClientTimestampUtc { get; init; }
}