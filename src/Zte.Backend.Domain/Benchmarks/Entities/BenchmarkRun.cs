using Zte.Backend.Domain.Benchmarks.Enums;
using Zte.Backend.Domain.Benchmarks.ValueObjects;

namespace Zte.Backend.Domain.Benchmarks.Entities;

public sealed class BenchmarkRun
{
    public required Guid Id { get; init; }

    public required string Code { get; init; }

    public required BenchmarkType Type { get; init; }

    public required BenchmarkStatus Status { get; set; }

    public required int IterationCount { get; init; }

    public required int SoftwareIterationCount { get; init; }

    public required int HardwareVerificationIterationCount { get; init; }

    public required int HardwareEnrollmentCount { get; init; }

    public BenchmarkDeviceInfo? MobileDevice { get; init; }

    public BenchmarkBackendSystemInfo? BackendSystem { get; init; }

    public required DateTimeOffset StartedAtUtc { get; init; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public string? ErrorMessage { get; set; }

    public double? DurationMs
    {
        get
        {
            if (CompletedAtUtc is null)
            {
                return null;
            }

            return (CompletedAtUtc.Value - StartedAtUtc).TotalMilliseconds;
        }
    }

    public bool Success => Status == BenchmarkStatus.Completed;
}
