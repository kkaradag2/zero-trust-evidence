namespace Zte.Backend.Application.Features.HardwareAttestation.Models;

public sealed class EnrolledDevice
{
    public required Guid EnrolledDeviceId { get; init; }

    public required string DeviceId { get; init; }

    public string? AppInstanceId { get; init; }

    public required string KeyAlias { get; init; }

    public required string PublicKeyBase64 { get; init; }

    public required IReadOnlyList<string> CertificateChainBase64 { get; init; }

    public required IReadOnlyList<string> VerificationSteps { get; init; }

    public required IReadOnlyList<string> VerificationWarnings { get; init; }

    public string? HardwareSecurityLevel { get; init; }

    public string? AttestationSecurityLevel { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
