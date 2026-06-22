namespace Zte.Backend.Application.Features.HardwareAttestation.Models;

public sealed class RegisteredDeviceKey
{
    public required string DeviceId { get; init; }

    public required string KeyAlias { get; init; }

    public required string PublicKeyBase64 { get; init; }

    public required IReadOnlyList<string> CertificateChainBase64 { get; init; }

    public required DateTimeOffset RegisteredAtUtc { get; init; }

    public DateTimeOffset? LastVerifiedAtUtc { get; init; }
}