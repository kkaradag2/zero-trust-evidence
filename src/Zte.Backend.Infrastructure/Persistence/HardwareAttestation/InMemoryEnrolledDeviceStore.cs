using System.Collections.Concurrent;
using Zte.Backend.Application.Common.Interfaces;
using Zte.Backend.Application.Features.HardwareAttestation.Models;

namespace Zte.Backend.Infrastructure.Persistence.HardwareAttestation;

public sealed class InMemoryEnrolledDeviceStore : IEnrolledDeviceStore
{
    private readonly ConcurrentDictionary<string, EnrolledDevice> _store = new();

    public SaveEnrolledDeviceResult Save(EnrolledDevice enrolledDevice)
    {
        ArgumentNullException.ThrowIfNull(enrolledDevice);

        var key = CreateKey(
            enrolledDevice.DeviceId,
            enrolledDevice.KeyAlias);

        var created = true;
        var savedDevice = _store.AddOrUpdate(
            key,
            enrolledDevice,
            (_, existing) =>
            {
                created = false;
                return existing;
            });

        return new SaveEnrolledDeviceResult(savedDevice, created);
    }

    public EnrolledDevice? Find(
        string deviceId,
        string keyAlias)
    {
        var key = CreateKey(deviceId, keyAlias);

        _store.TryGetValue(key, out var enrolledDevice);

        return enrolledDevice;
    }

    private static string CreateKey(string deviceId, string keyAlias)
    {
        return $"{deviceId.Trim().ToLowerInvariant()}::{keyAlias.Trim().ToLowerInvariant()}";
    }
}
