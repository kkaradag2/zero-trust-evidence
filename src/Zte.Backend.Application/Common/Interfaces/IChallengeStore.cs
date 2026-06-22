using Zte.Backend.Domain.Challenges.Entities;

namespace Zte.Backend.Application.Common.Interfaces;

public interface IChallengeStore
{
    void Add(AttestationChallenge challenge);

    AttestationChallenge? Get(Guid challengeId);

    bool MarkAsUsed(Guid challengeId);
}