namespace Zte.Backend.Domain.Challenges.Entities;

public sealed record AttestationChallenge(
    Guid ChallengeId,
    string Nonce,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc);