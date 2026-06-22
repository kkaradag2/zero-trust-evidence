namespace Zte.Backend.Application.Common.Contracts;

public sealed class CreateAttestationChallengeRequest
{
    public string? DeviceId { get; init; }

    public string? AppInstanceId { get; init; }

    public string? UserSessionId { get; init; }

    public string? Purpose { get; init; }
}
