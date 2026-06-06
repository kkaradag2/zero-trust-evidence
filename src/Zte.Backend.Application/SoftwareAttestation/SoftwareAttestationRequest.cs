namespace Zte.Backend.Application.SoftwareAttestation;

public sealed record SoftwareAttestationRequest(
    Guid ChallengeId,
    string Nonce,
    string DeviceId,
    string Platform,
    string OsVersion,
    string AppVersion,
    string DeviceBrand,
    string DeviceModel,
    bool IsEmulator,
    bool IsRooted,
    DateTime ClientTimestampUtc);