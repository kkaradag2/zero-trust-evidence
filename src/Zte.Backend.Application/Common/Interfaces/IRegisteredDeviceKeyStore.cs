using Zte.Backend.Application.Features.HardwareAttestation.Models;

namespace Zte.Backend.Application.Common.Interfaces;

public interface IRegisteredDeviceKeyStore
{
    void Save(RegisteredDeviceKey registeredDeviceKey);

    RegisteredDeviceKey? Find(
        string deviceId,
        string keyAlias);
}
