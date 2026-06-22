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

    public BenchmarksController(IBenchmarkRunStore benchmarkRunStore, IMeasurementStore measurementStore)
    {
        _benchmarkRunStore = benchmarkRunStore;
        _measurementStore = measurementStore;
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
}
