using Zte.Backend.Application.Common.Interfaces;
using Zte.Backend.Domain.Challenges.Entities;

namespace Zte.Backend.Infrastructure.Persistence.Challenges;

/// <summary>
/// Stores attestation challenges in memory for the lifetime of the backend process.
/// This implementation is intended for the experimental prototype and not for production use.
/// </summary>
public sealed class InMemoryChallengeStore : IChallengeStore
{
    /// <summary>
    /// Keeps generated challenges by their unique challenge identifier.
    /// The value also stores whether the challenge has already been used.
    /// </summary>
    private readonly Dictionary<Guid, StoredChallenge> _challenges = [];

    /// <summary>
    /// Protects the in-memory dictionary against concurrent access from multiple requests.
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    /// Adds a newly generated attestation challenge to the in-memory store.
    /// A newly created challenge is initially marked as unused.
    /// </summary>
    public void Add(AttestationChallenge challenge)
    {
        lock (_lock)
        {
            _challenges[challenge.ChallengeId] = new StoredChallenge(
                Challenge: challenge,
                IsUsed: false);
        }
    }

    /// <summary>
    /// Retrieves a previously generated challenge by its identifier.
    /// Returns null when the challenge does not exist in the current process memory.
    /// </summary>
    public AttestationChallenge? Get(Guid challengeId)
    {
        lock (_lock)
        {
            return _challenges.TryGetValue(challengeId, out var storedChallenge)
                ? storedChallenge.Challenge
                : null;
        }
    }

    /// <summary>
    /// Marks a challenge as used so that the same challenge cannot be reused later.
    /// This helps reduce replay risk in the attestation flow.
    /// </summary>
    /// <returns>
    /// True when the challenge exists and was unused before this call.
    /// False when the challenge does not exist or has already been used.
    /// </returns>
    public bool MarkAsUsed(Guid challengeId)
    {
        lock (_lock)
        {
            // If the challenge is not found, the verification flow should treat it as invalid.
            if (!_challenges.TryGetValue(challengeId, out var storedChallenge))
            {
                return false;
            }

            // If the challenge was already used, this may indicate a replay attempt.
            if (storedChallenge.IsUsed)
            {
                return false;
            }

            // Records are immutable, so we replace the stored value with a new copy.
            _challenges[challengeId] = storedChallenge with
            {
                IsUsed = true
            };

            return true;
        }
    }

    /// <summary>
    /// Internal representation of a challenge stored in memory.
    /// It combines the original challenge data with usage state.
    /// </summary>
    private sealed record StoredChallenge(
        AttestationChallenge Challenge,
        bool IsUsed);
}