namespace Zte.Backend.Application.HardwareAttestation;

public sealed class HardwareEnrollmentRequest
{
    public required string ChallengeId { get; init; }

    public required string Nonce { get; init; }

    public required string DeviceId { get; init; }

    public required string KeyAlias { get; init; }

    public required string PublicKeyBase64 { get; init; }

    public required IReadOnlyList<string> CertificateChainBase64 { get; init; }

    public required DateTimeOffset ClientTimestampUtc { get; init; }
}