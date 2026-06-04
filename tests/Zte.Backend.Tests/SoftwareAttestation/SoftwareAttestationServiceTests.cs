using Zte.Backend.Application.SoftwareAttestation;
using Zte.Backend.Domain.Attestation;

namespace Zte.Backend.Tests.SoftwareAttestation;

public sealed class SoftwareAttestationServiceTests
{
    [Fact]
    public void Verify_WhenAndroidDeviceIsNotEmulatorAndNotRooted_ReturnsLowRiskAcceptedResult()
    {
        // Arrange
        // This test represents a normal Android device posture.
        // The device is Android, the application version is present,
        // and there are no emulator or root indicators.
        var service = new SoftwareAttestationService();

        var request = new SoftwareAttestationRequest(
            DeviceId: "test-device-001",
            Platform: "android",
            OsVersion: "14",
            AppVersion: "1.0.0",
            DeviceBrand: "Samsung",
            DeviceModel: "Galaxy S23",
            IsEmulator: false,
            IsRooted: false,
            ClientTimestampUtc: DateTime.UtcNow);

        // Act
        // The service verifies the software/context-aware posture signals.
        var result = service.Verify(request, messageSizeBytes: 220);

        // Assert
        // A clean software attestation payload should be accepted
        // and classified as low risk.
        Assert.True(result.Accepted);
        Assert.Equal(AttestationType.Software, result.AttestationType);
        Assert.Equal(RiskLevel.Low, result.RiskLevel);
        Assert.Equal(4, result.ProcessingStepCount);
        Assert.Equal(220, result.MessageSizeBytes);
        Assert.Empty(result.Reasons);

        // The verification time is expected to be measured,
        // even if the operation is very fast.
        Assert.True(result.VerificationTimeMicroseconds >= 0);
        Assert.True(result.VerificationTimeMs >= 0);
    }

    [Fact]
    public void Verify_WhenDeviceIsEmulator_ReturnsMediumRiskAcceptedResult()
    {
        // Arrange
        // This test represents a device posture where the client
        // appears to be running inside an emulator.
        // In this simplified rule set, a single suspicious signal
        // increases the risk level but does not immediately reject the request.
        var service = new SoftwareAttestationService();

        var request = new SoftwareAttestationRequest(
            DeviceId: "test-device-002",
            Platform: "android",
            OsVersion: "14",
            AppVersion: "1.0.0",
            DeviceBrand: "Google",
            DeviceModel: "Android Emulator",
            IsEmulator: true,
            IsRooted: false,
            ClientTimestampUtc: DateTime.UtcNow);

        // Act
        var result = service.Verify(request, messageSizeBytes: 225);

        // Assert
        // The request is accepted, but the emulator signal is recorded
        // as a reason and the risk level becomes medium.
        Assert.True(result.Accepted);
        Assert.Equal(AttestationType.Software, result.AttestationType);
        Assert.Equal(RiskLevel.Medium, result.RiskLevel);
        Assert.Equal(4, result.ProcessingStepCount);
        Assert.Equal(225, result.MessageSizeBytes);
        Assert.Contains("Device appears to be an emulator.", result.Reasons);
    }

    [Fact]
    public void Verify_WhenDeviceIsEmulatorAndRooted_ReturnsHighRiskRejectedResult()
    {
        // Arrange
        // This test represents a higher-risk client posture.
        // The client appears to be both an emulator and rooted.
        // In the simplified verification policy, multiple suspicious
        // software-level signals result in rejection.
        var service = new SoftwareAttestationService();

        var request = new SoftwareAttestationRequest(
            DeviceId: "test-device-003",
            Platform: "android",
            OsVersion: "14",
            AppVersion: "1.0.0",
            DeviceBrand: "Unknown",
            DeviceModel: "Unknown",
            IsEmulator: true,
            IsRooted: true,
            ClientTimestampUtc: DateTime.UtcNow);

        // Act
        var result = service.Verify(request, messageSizeBytes: 230);

        // Assert
        // Multiple suspicious signals should classify the request
        // as high risk and reject the verification result.
        Assert.False(result.Accepted);
        Assert.Equal(AttestationType.Software, result.AttestationType);
        Assert.Equal(RiskLevel.High, result.RiskLevel);
        Assert.Equal(4, result.ProcessingStepCount);
        Assert.Equal(230, result.MessageSizeBytes);
        Assert.Contains("Device appears to be an emulator.", result.Reasons);
        Assert.Contains("Device appears to be rooted.", result.Reasons);
    }
}