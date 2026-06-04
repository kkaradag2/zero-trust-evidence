using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Zte.Backend.Application.SoftwareAttestation;
using Zte.Backend.Domain.Attestation;
using Zte.Backend.Application.Measurements;
using Zte.Backend.Domain.Measurements;

namespace Zte.Backend.Api.Controllers;

[ApiController]
[Route("api/attestation/software")]
public sealed class SoftwareAttestationController : ControllerBase
{
    private readonly ISoftwareAttestationService _softwareAttestationService;
    private readonly IMeasurementStore _measurementStore;

    public SoftwareAttestationController(ISoftwareAttestationService softwareAttestationService,
                                         IMeasurementStore measurementStore)
    {
        _softwareAttestationService = softwareAttestationService;
        _measurementStore = measurementStore;
    }

    [HttpPost("verify")]
    [ProducesResponseType(typeof(VerificationResult), StatusCodes.Status200OK)]
    public ActionResult<VerificationResult> Verify([FromBody] SoftwareAttestationRequest request)
    {
        var messageSizeBytes = JsonSerializer.SerializeToUtf8Bytes(request).Length;

        var result = _softwareAttestationService.Verify(request, messageSizeBytes);

        var measurement = new VerificationMeasurement(
            Id: Guid.NewGuid(),
            AttestationType: result.AttestationType,
            Accepted: result.Accepted,
            RiskLevel: result.RiskLevel,
            VerificationTimeMs: result.VerificationTimeMs,
            VerificationTimeMicroseconds: result.VerificationTimeMicroseconds,
            MessageSizeBytes: result.MessageSizeBytes,
            ProcessingStepCount: result.ProcessingStepCount,
            CreatedAtUtc: DateTime.UtcNow);

        _measurementStore.Add(measurement);

        return Ok(result);
    }

}