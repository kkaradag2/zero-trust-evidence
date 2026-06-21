using System.Collections.Concurrent;
using Zte.Backend.Application.HardwareAttestation;

namespace Zte.Backend.Infrastructure.HardwareAttestation;

public sealed class InMemoryRegisteredDeviceKeyStore : IRegisteredDeviceKeyStore
{
    private readonly ConcurrentDictionary<string, RegisteredDeviceKey> _store = new();

    public void Save(RegisteredDeviceKey registeredDeviceKey)
    {
        ArgumentNullException.ThrowIfNull(registeredDeviceKey);

        var key = CreateKey(
            registeredDeviceKey.DeviceId,
            registeredDeviceKey.KeyAlias);

        _store[key] = registeredDeviceKey;
    }

    public RegisteredDeviceKey? Find(
        string deviceId,
        string keyAlias)
    {
        var key = CreateKey(deviceId, keyAlias);

        _store.TryGetValue(key, out var registeredDeviceKey);

        return registeredDeviceKey;
    }

    private static string CreateKey(string deviceId, string keyAlias)
    {
        return $"{deviceId.Trim().ToLowerInvariant()}::{keyAlias.Trim().ToLowerInvariant()}";
    }
}