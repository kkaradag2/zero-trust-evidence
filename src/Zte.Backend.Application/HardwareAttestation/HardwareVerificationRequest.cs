namespace Zte.Backend.Application.HardwareAttestation;

public sealed class HardwareVerificationRequest
{
    public required string ChallengeId { get; init; }

    public required string Nonce { get; init; }

    public required string DeviceId { get; init; }

    public required string KeyAlias { get; init; }

    public required string SignatureBase64 { get; init; }

    public required DateTimeOffset ClientTimestampUtc { get; init; }
}