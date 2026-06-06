using Zte.Backend.Domain.Challenges;

namespace Zte.Backend.Application.Challenges;

public interface IChallengeStore
{
    void Add(AttestationChallenge challenge);

    AttestationChallenge? Get(Guid challengeId);

    bool MarkAsUsed(Guid challengeId);
}