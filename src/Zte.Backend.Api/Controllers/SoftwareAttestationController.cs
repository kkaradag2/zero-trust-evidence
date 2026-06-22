using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Zte.Backend.Application.Features.SoftwareAttestation.Contracts;
using Zte.Backend.Application.Features.SoftwareAttestation.Services;
using Zte.Backend.Domain.Attestation.Enums;
using Zte.Backend.Domain.Attestation.ValueObjects;
using Zte.Backend.Application.Common.Interfaces;
using Zte.Backend.Domain.Measurements.Entities;
using Zte.Backend.Domain.Measurements.Enums;

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
           BenchmarkRunId: request.BenchmarkRunId,
           Phase: MeasurementPhase.SoftwareVerification,
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