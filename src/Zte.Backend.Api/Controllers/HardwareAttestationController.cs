using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Zte.Backend.Application.Features.HardwareAttestation.Contracts;
using Zte.Backend.Application.Features.HardwareAttestation.Models;
using Zte.Backend.Application.Features.HardwareAttestation.Services;
using Zte.Backend.Application.Common.Interfaces;
using Zte.Backend.Domain.Attestation.Enums;
using Zte.Backend.Domain.Attestation.ValueObjects;
using Zte.Backend.Domain.Measurements.Entities;
using Zte.Backend.Domain.Measurements.Enums;

namespace Zte.Backend.Api.Controllers;

[ApiController]
[Route("api/attestation/hardware")]
public sealed class HardwareAttestationController : ControllerBase
{
    private readonly IHardwareAttestationService _hardwareAttestationService;
    private readonly IMeasurementStore _measurementStore;

    public HardwareAttestationController(
        IHardwareAttestationService hardwareAttestationService,
        IMeasurementStore measurementStore)
    {
        _hardwareAttestationService = hardwareAttestationService;
        _measurementStore = measurementStore;
    }

    [HttpPost("enroll")]
    [ProducesResponseType(typeof(VerificationResult), StatusCodes.Status200OK)]
    public ActionResult VerifyEnrollment([FromBody] HardwareEnrollmentRequest request)
    {
        var messageSizeBytes = JsonSerializer.SerializeToUtf8Bytes(request).Length;

        var result = _hardwareAttestationService.Enroll(
            request,
            messageSizeBytes);

        AddMeasurement(
    result,
    request.BenchmarkRunId,
    MeasurementPhase.HardwareEnrollment);

        return Ok(result);
    }

    [HttpPost("verify")]
    [ProducesResponseType(typeof(VerificationResult), StatusCodes.Status200OK)]
    public ActionResult Verify([FromBody] HardwareVerificationRequest request)
    {
        var messageSizeBytes = JsonSerializer.SerializeToUtf8Bytes(request).Length;

        var result = _hardwareAttestationService.Verify(
            request,
            messageSizeBytes);

        AddMeasurement(
     result,
     request.BenchmarkRunId,
     MeasurementPhase.HardwareVerification);


        return Ok(result);
    }

    private void AddMeasurement(
        VerificationResult result,
        Guid? benchmarkRunId,
        MeasurementPhase phase)
    {
        var measurement = new VerificationMeasurement(
            Id: Guid.NewGuid(),
            BenchmarkRunId: benchmarkRunId,
            Phase: phase,
            AttestationType: result.AttestationType,
            Accepted: result.Accepted,
            RiskLevel: result.RiskLevel,
            VerificationTimeMs: result.VerificationTimeMs,
            VerificationTimeMicroseconds: result.VerificationTimeMicroseconds,
            MessageSizeBytes: result.MessageSizeBytes,
            ProcessingStepCount: result.ProcessingStepCount,
            CreatedAtUtc: DateTime.UtcNow);

        _measurementStore.Add(measurement);
    }
}