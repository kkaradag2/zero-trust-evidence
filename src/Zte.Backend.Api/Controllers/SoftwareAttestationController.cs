using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Zte.Backend.Application.SoftwareAttestation;
using Zte.Backend.Domain.Attestation;

namespace Zte.Backend.Api.Controllers;

[ApiController]
[Route("api/attestation/software")]
public sealed class SoftwareAttestationController : ControllerBase
{
    private readonly ISoftwareAttestationService _softwareAttestationService;

    public SoftwareAttestationController(ISoftwareAttestationService softwareAttestationService)
    {
        _softwareAttestationService = softwareAttestationService;
    }

    [HttpPost("verify")]
    [ProducesResponseType(typeof(VerificationResult), StatusCodes.Status200OK)]
    public ActionResult<VerificationResult> Verify([FromBody] SoftwareAttestationRequest request)
    {
        var messageSizeBytes = JsonSerializer.SerializeToUtf8Bytes(request).Length;

        var result = _softwareAttestationService.Verify(request, messageSizeBytes);

        return Ok(result);
    }
}