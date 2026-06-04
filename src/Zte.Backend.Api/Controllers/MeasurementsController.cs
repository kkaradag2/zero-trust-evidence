using Microsoft.AspNetCore.Mvc;
using Zte.Backend.Application.Measurements;
using Zte.Backend.Domain.Measurements;

namespace Zte.Backend.Api.Controllers;

[ApiController]
[Route("api/measurements")]
public sealed class MeasurementsController : ControllerBase
{
    private readonly IMeasurementStore _measurementStore;

    public MeasurementsController(IMeasurementStore measurementStore)
    {
        _measurementStore = measurementStore;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VerificationMeasurement>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<VerificationMeasurement>> GetAll()
    {
        return Ok(_measurementStore.GetAll());
    }
}