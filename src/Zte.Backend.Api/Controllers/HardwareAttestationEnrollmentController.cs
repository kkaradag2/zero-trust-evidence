using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Zte.Backend.Application.Common.Interfaces;
using Zte.Backend.Application.Features.HardwareAttestation.Contracts;
using Zte.Backend.Application.Features.HardwareAttestation.Services;
using Zte.Backend.Domain.Attestation.Enums;
using Zte.Backend.Domain.Measurements.Entities;
using Zte.Backend.Domain.Measurements.Enums;

namespace Zte.Backend.Api.Controllers;

[ApiController]
[Route("api/hardware-attestation")]
public sealed class HardwareAttestationEnrollmentController : ControllerBase
{
    private readonly IHardwareAttestationEnrollmentService _hardwareAttestationEnrollmentService;
    private readonly IMeasurementStore _measurementStore;

    public HardwareAttestationEnrollmentController(
        IHardwareAttestationEnrollmentService hardwareAttestationEnrollmentService,
        IMeasurementStore measurementStore)
    {
        _hardwareAttestationEnrollmentService = hardwareAttestationEnrollmentService;
        _measurementStore = measurementStore;
    }

    [HttpPost("enroll")]
    [ProducesResponseType(typeof(HardwareEnrollmentResult), StatusCodes.Status200OK)]
    public ActionResult<HardwareEnrollmentResult> Enroll([FromBody] HardwareEnrollmentRequest request)
    {
        var messageSizeBytes = JsonSerializer.SerializeToUtf8Bytes(request).Length;

        var result = _hardwareAttestationEnrollmentService.Enroll(
            request,
            messageSizeBytes);

        var measurement = new VerificationMeasurement(
            Id: Guid.NewGuid(),
            BenchmarkRunId: request.BenchmarkRunId,
            Phase: MeasurementPhase.HardwareEnrollment,
            AttestationType: AttestationType.Hardware,
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
