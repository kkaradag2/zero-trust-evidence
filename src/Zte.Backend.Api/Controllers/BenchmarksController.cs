using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;
using Zte.Backend.Application.Features.Benchmarks.Contracts;
using Zte.Backend.Application.Common.Interfaces;
using Zte.Backend.Domain.Benchmarks.Entities;
using Zte.Backend.Domain.Benchmarks.Enums;
using Zte.Backend.Domain.Benchmarks.ValueObjects;
using Zte.Backend.Domain.Measurements.Entities;
using Zte.Backend.Domain.Measurements.Enums;

namespace Zte.Backend.Api.Controllers;

[ApiController]
[Route("api/benchmarks")]
public sealed class BenchmarksController : ControllerBase
{
    private const int DefaultBenchmarkIterationCount = 50;

    private readonly IBenchmarkRunStore _benchmarkRunStore;
    private readonly IMeasurementStore _measurementStore;
    private readonly IRuntimeBenchmarkMeasurementStore _runtimeBenchmarkMeasurementStore;

    public BenchmarksController(
        IBenchmarkRunStore benchmarkRunStore,
        IMeasurementStore measurementStore,
        IRuntimeBenchmarkMeasurementStore runtimeBenchmarkMeasurementStore)
    {
        _benchmarkRunStore = benchmarkRunStore;
        _measurementStore = measurementStore;
        _runtimeBenchmarkMeasurementStore = runtimeBenchmarkMeasurementStore;
    }

    [HttpPost]
    [ProducesResponseType(typeof(BenchmarkRun), StatusCodes.Status201Created)]
    public ActionResult<BenchmarkRun> Create([FromBody] CreateBenchmarkRunRequest request)
    {
        var iterationCount = request.IterationCount.GetValueOrDefault(DefaultBenchmarkIterationCount);

        if (iterationCount <= 0)
        {
            iterationCount = DefaultBenchmarkIterationCount;
        }

        var run = _benchmarkRunStore.Create(
            request.Type,
            iterationCount,
            request.MobileDevice,
            GetBackendSystemInfo());

        return CreatedAtAction(
            nameof(GetById),
            new { id = run.Id },
            run);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BenchmarkRun>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<BenchmarkRun>> List()
    {
        return Ok(_benchmarkRunStore.List());
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BenchmarkRun), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<BenchmarkRun> GetById(Guid id)
    {
        var run = _benchmarkRunStore.Find(id);

        if (run is null)
        {
            return NotFound();
        }

        return Ok(run);
    }

    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(BenchmarkRun), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<BenchmarkRun> Complete(Guid id)
    {
        var run = _benchmarkRunStore.Complete(id);

        if (run is null)
        {
            return NotFound();
        }

        return Ok(run);
    }

    [HttpPost("{id:guid}/fail")]
    [ProducesResponseType(typeof(BenchmarkRun), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<BenchmarkRun> Fail(
        Guid id,
        [FromBody] FailBenchmarkRunRequest request)
    {
        var run = _benchmarkRunStore.Fail(
            id,
            request.ErrorMessage);

        if (run is null)
        {
            return NotFound();
        }

        return Ok(run);
    }

    [HttpGet("{id:guid}/measurements")]
    [ProducesResponseType(typeof(IReadOnlyList<VerificationMeasurement>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IReadOnlyList<VerificationMeasurement>> GetMeasurements(Guid id)
    {
        var run = _benchmarkRunStore.Find(id);

        if (run is null)
        {
            return NotFound();
        }

        var measurements = _measurementStore.ListByBenchmarkRunId(id);

        return Ok(measurements);
    }

    [HttpPost("{id:guid}/runtime-measurements")]
    [ProducesResponseType(typeof(SaveRuntimeMeasurementsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<SaveRuntimeMeasurementsResponse> SaveRuntimeMeasurements(
        Guid id,
        [FromBody] IReadOnlyList<RuntimeMeasurementRequest> requests)
    {
        var run = _benchmarkRunStore.Find(id);

        if (run is null)
        {
            return NotFound();
        }

        if (requests is null)
        {
            return BadRequest("Runtime measurement request body is required.");
        }

        var validationErrors = ValidateRuntimeMeasurementRequests(requests);

        if (validationErrors.Count > 0)
        {
            return BadRequest(validationErrors);
        }

        var createdAtUtc = DateTimeOffset.UtcNow;
        var measurements = requests
            .Select(request => ToRuntimeBenchmarkMeasurement(id, request, createdAtUtc))
            .ToList();

        _runtimeBenchmarkMeasurementStore.AddRange(measurements);

        return Ok(new SaveRuntimeMeasurementsResponse(
            BenchmarkRunId: id,
            SavedCount: measurements.Count));
    }

    [HttpGet("{id:guid}/runtime-measurements")]
    [ProducesResponseType(typeof(IReadOnlyList<RuntimeBenchmarkMeasurement>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IReadOnlyList<RuntimeBenchmarkMeasurement>> GetRuntimeMeasurements(Guid id)
    {
        var run = _benchmarkRunStore.Find(id);

        if (run is null)
        {
            return NotFound();
        }

        var measurements = _runtimeBenchmarkMeasurementStore.GetByBenchmarkRunId(id);

        return Ok(measurements);
    }

    private static BenchmarkBackendSystemInfo GetBackendSystemInfo()
    {
        var memoryInfo = GC.GetGCMemoryInfo();

        return new BenchmarkBackendSystemInfo
        {
            MachineName = Environment.MachineName,
            OsDescription = RuntimeInformation.OSDescription,
            ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            ProcessorCount = Environment.ProcessorCount,
            TotalAvailableMemoryBytes = memoryInfo.TotalAvailableMemoryBytes > 0
                ? memoryInfo.TotalAvailableMemoryBytes
                : null
        };
    }

    private static IReadOnlyList<string> ValidateRuntimeMeasurementRequests(
        IReadOnlyList<RuntimeMeasurementRequest> requests)
    {
        var errors = new List<string>();

        for (var index = 0; index < requests.Count; index++)
        {
            var request = requests[index];
            var prefix = $"Measurement {index + 1}";

            if (request.RunIndex <= 0)
            {
                errors.Add($"{prefix}: runIndex must be greater than zero.");
            }

            if (request.TotalRequests <= 0)
            {
                errors.Add($"{prefix}: totalRequests must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(request.PolicyK))
            {
                errors.Add($"{prefix}: policyK is required.");
            }

            if (string.IsNullOrWhiteSpace(request.PolicyLabel))
            {
                errors.Add($"{prefix}: policyLabel is required.");
            }

            if (request.ChallengeFetchMs < 0)
            {
                errors.Add($"{prefix}: challengeFetchMs cannot be negative.");
            }

            if (request.DeviceSigningMs < 0)
            {
                errors.Add($"{prefix}: deviceSigningMs cannot be negative.");
            }

            if (request.BackendVerificationMs < 0)
            {
                errors.Add($"{prefix}: backendVerificationMs cannot be negative.");
            }

            if (request.FreshProofCostMs < 0)
            {
                errors.Add($"{prefix}: freshProofCostMs cannot be negative.");
            }

            if (request.OperationTotalMs < 0)
            {
                errors.Add($"{prefix}: operationTotalMs cannot be negative.");
            }
        }

        return errors;
    }

    private static RuntimeBenchmarkMeasurement ToRuntimeBenchmarkMeasurement(
        Guid benchmarkRunId,
        RuntimeMeasurementRequest request,
        DateTimeOffset createdAtUtc)
    {
        return new RuntimeBenchmarkMeasurement(
            Id: Guid.NewGuid(),
            BenchmarkRunId: benchmarkRunId,
            PolicyK: request.PolicyK!.Trim(),
            PolicyLabel: request.PolicyLabel!.Trim(),
            RunIndex: request.RunIndex,
            TotalRequests: request.TotalRequests,
            AttestationPerformed: request.AttestationPerformed,
            ChallengeFetchMs: request.ChallengeFetchMs,
            DeviceSigningMs: request.DeviceSigningMs,
            BackendVerificationMs: request.BackendVerificationMs,
            FreshProofCostMs: request.FreshProofCostMs,
            OperationTotalMs: request.OperationTotalMs,
            RequestPayloadBytes: request.RequestPayloadBytes,
            ResponsePayloadBytes: request.ResponsePayloadBytes,
            Success: request.Success,
            ErrorMessage: request.ErrorMessage,
            CreatedAtUtc: createdAtUtc,
            ClientTimestampUtc: request.Timestamp);
    }
}
