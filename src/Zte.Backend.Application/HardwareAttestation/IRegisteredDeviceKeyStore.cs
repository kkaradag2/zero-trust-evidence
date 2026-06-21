namespace Zte.Backend.Application.HardwareAttestation;

public interface IRegisteredDeviceKeyStore
{
    void Save(RegisteredDeviceKey registeredDeviceKey);

    RegisteredDeviceKey? Find(
        string deviceId,
        string keyAlias);
}