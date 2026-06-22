namespace Zte.Backend.Application.Features.SoftwareAttestation.Contracts;

public sealed record SoftwareAttestationRequest(
    Guid? BenchmarkRunId,
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