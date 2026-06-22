using Zte.Backend.Application.Features.HardwareAttestation.Models;

namespace Zte.Backend.Application.Common.Interfaces;

public interface IEnrolledDeviceStore
{
    SaveEnrolledDeviceResult Save(EnrolledDevice enrolledDevice);

    EnrolledDevice? Find(
        string deviceId,
        string keyAlias);
}

public sealed record SaveEnrolledDeviceResult(
    EnrolledDevice Device,
    bool Created);
