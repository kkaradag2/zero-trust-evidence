using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Zte.Backend.Application.Common.Interfaces;
using Zte.Backend.Domain.Challenges.Entities;

namespace Zte.Backend.Api.Controllers;

[ApiController]
[Route("api/attestation/challenge")]
public sealed class AttestationChallengeController : ControllerBase
{
    private readonly IChallengeStore _challengeStore;

    public AttestationChallengeController(IChallengeStore challengeStore)
    {
        _challengeStore = challengeStore;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AttestationChallenge), StatusCodes.Status200OK)]
    public ActionResult<AttestationChallenge> Create()
    {
        var createdAtUtc = DateTime.UtcNow;

        var challenge = new AttestationChallenge(
            ChallengeId: Guid.NewGuid(),
            Nonce: CreateNonce(),
            CreatedAtUtc: createdAtUtc,
            ExpiresAtUtc: createdAtUtc.AddMinutes(5));

        _challengeStore.Add(challenge);

        return Ok(challenge);
    }

    private static string CreateNonce()
    {
        var nonceBytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToBase64String(nonceBytes);
    }
}