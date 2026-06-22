namespace Zte.Backend.Domain.Benchmarks.ValueObjects;

public sealed class BenchmarkDeviceInfo
{
    public string? DeviceId { get; init; }

    public string? DeviceName { get; init; }

    public string? Manufacturer { get; init; }

    public string? Brand { get; init; }

    public string? Model { get; init; }

    public string? Device { get; init; }

    public string? Product { get; init; }

    public string? Hardware { get; init; }

    public string? AndroidVersion { get; init; }

    public int? SdkInt { get; init; }

    public IReadOnlyList<string> SupportedAbis { get; init; } = [];

    public bool? IsEmulator { get; init; }

    public int? CpuCoreCount { get; init; }

    public long? TotalMemoryBytes { get; init; }
}
