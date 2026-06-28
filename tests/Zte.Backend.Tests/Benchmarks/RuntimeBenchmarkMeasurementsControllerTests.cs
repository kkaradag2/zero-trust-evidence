using Microsoft.AspNetCore.Mvc;
using Zte.Backend.Api.Controllers;
using Zte.Backend.Application.Features.Benchmarks.Contracts;
using Zte.Backend.Domain.Benchmarks.Enums;
using Zte.Backend.Domain.Measurements.Entities;
using Zte.Backend.Infrastructure.Persistence.Benchmarks;
using Zte.Backend.Infrastructure.Persistence.Measurements;

namespace Zte.Backend.Tests.Benchmarks;

public sealed class RuntimeBenchmarkMeasurementsControllerTests
{
    [Fact]
    public void SaveRuntimeMeasurements_WhenBenchmarkExists_StoresMeasurements()
    {
        var fixture = CreateFixture();
        var request = CreateRuntimeMeasurementRequest(runIndex: 1);

        var result = fixture.Controller.SaveRuntimeMeasurements(
            fixture.BenchmarkRunId,
            [request]);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<SaveRuntimeMeasurementsResponse>(okResult.Value);
        var savedMeasurements = fixture.RuntimeMeasurementStore.GetByBenchmarkRunId(
            fixture.BenchmarkRunId);

        Assert.Equal(fixture.BenchmarkRunId, response.BenchmarkRunId);
        Assert.Equal(1, response.SavedCount);
        Assert.Single(savedMeasurements);
        Assert.Equal(1, savedMeasurements[0].RunIndex);
        Assert.Equal("5", savedMeasurements[0].PolicyK);
        Assert.True(savedMeasurements[0].AttestationPerformed);
    }

    [Fact]
    public void GetRuntimeMeasurements_WhenMeasurementsExist_ReturnsRowsOrderedByRunIndex()
    {
        var fixture = CreateFixture();

        fixture.Controller.SaveRuntimeMeasurements(
            fixture.BenchmarkRunId,
            [
                CreateRuntimeMeasurementRequest(runIndex: 2),
                CreateRuntimeMeasurementRequest(runIndex: 1)
            ]);

        var result = fixture.Controller.GetRuntimeMeasurements(fixture.BenchmarkRunId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var measurements = Assert.IsAssignableFrom<IReadOnlyList<RuntimeBenchmarkMeasurement>>(
            okResult.Value);

        Assert.Equal(2, measurements.Count);
        Assert.Equal(1, measurements[0].RunIndex);
        Assert.Equal(2, measurements[1].RunIndex);
    }

    [Fact]
    public void SaveRuntimeMeasurements_WhenBenchmarkDoesNotExist_ReturnsNotFound()
    {
        var fixture = CreateFixture();

        var result = fixture.Controller.SaveRuntimeMeasurements(
            Guid.NewGuid(),
            [CreateRuntimeMeasurementRequest(runIndex: 1)]);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public void SaveRuntimeMeasurements_WhenDurationIsNegative_ReturnsBadRequest()
    {
        var fixture = CreateFixture();

        var result = fixture.Controller.SaveRuntimeMeasurements(
            fixture.BenchmarkRunId,
            [CreateRuntimeMeasurementRequest(runIndex: 1, challengeFetchMs: -1)]);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var errors = Assert.IsAssignableFrom<IReadOnlyList<string>>(badRequest.Value);

        Assert.Contains(errors, error => error.Contains(
            "challengeFetchMs cannot be negative",
            StringComparison.Ordinal));
    }

    [Fact]
    public void SaveRuntimeMeasurements_WhenReuseRowIsSubmitted_StoresReuseMeasurement()
    {
        var fixture = CreateFixture();

        fixture.Controller.SaveRuntimeMeasurements(
            fixture.BenchmarkRunId,
            [CreateRuntimeMeasurementRequest(runIndex: 2, attestationPerformed: false)]);

        var savedMeasurements = fixture.RuntimeMeasurementStore.GetByBenchmarkRunId(
            fixture.BenchmarkRunId);

        var measurement = Assert.Single(savedMeasurements);
        Assert.False(measurement.AttestationPerformed);
        Assert.Equal(0, measurement.ChallengeFetchMs);
        Assert.Equal(0, measurement.DeviceSigningMs);
        Assert.Equal(0, measurement.BackendVerificationMs);
        Assert.Equal(0, measurement.FreshProofCostMs);
        Assert.True(measurement.Success);
    }

    private static RuntimeBenchmarkFixture CreateFixture()
    {
        var benchmarkRunStore = new InMemoryBenchmarkRunStore();
        var measurementStore = new InMemoryMeasurementStore();
        var runtimeMeasurementStore = new InMemoryRuntimeBenchmarkMeasurementStore();
        var benchmarkRun = benchmarkRunStore.Create(
            BenchmarkType.Hardware,
            iterationCount: 50,
            mobileDevice: null,
            backendSystem: null);
        var controller = new BenchmarksController(
            benchmarkRunStore,
            measurementStore,
            runtimeMeasurementStore);

        return new RuntimeBenchmarkFixture(
            Controller: controller,
            RuntimeMeasurementStore: runtimeMeasurementStore,
            BenchmarkRunId: benchmarkRun.Id);
    }

    private static RuntimeMeasurementRequest CreateRuntimeMeasurementRequest(
        int runIndex,
        bool attestationPerformed = true,
        double challengeFetchMs = 10)
    {
        return new RuntimeMeasurementRequest
        {
            PolicyK = "5",
            PolicyLabel = "Every 5 requests",
            RunIndex = runIndex,
            TotalRequests = 50,
            AttestationPerformed = attestationPerformed,
            ChallengeFetchMs = attestationPerformed ? challengeFetchMs : 0,
            DeviceSigningMs = attestationPerformed ? 20 : 0,
            BackendVerificationMs = attestationPerformed ? 30 : 0,
            FreshProofCostMs = attestationPerformed ? 60 : 0,
            OperationTotalMs = attestationPerformed ? 65 : 0,
            RequestPayloadBytes = attestationPerformed ? 256 : 0,
            ResponsePayloadBytes = attestationPerformed ? 512 : 0,
            Success = true,
            ErrorMessage = null,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private sealed record RuntimeBenchmarkFixture(
        BenchmarksController Controller,
        InMemoryRuntimeBenchmarkMeasurementStore RuntimeMeasurementStore,
        Guid BenchmarkRunId);
}
