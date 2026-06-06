namespace Zte.Backend.Domain.Challenges;

public sealed record AttestationChallenge(
    Guid ChallengeId,
    string Nonce,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc);