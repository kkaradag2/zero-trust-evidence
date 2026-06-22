using Zte.Backend.Application.Common.Interfaces;
using Zte.Backend.Application.Features.SoftwareAttestation.Contracts;
using Zte.Backend.Application.Features.SoftwareAttestation.Services;
using Zte.Backend.Domain.Attestation.Enums;
using Zte.Backend.Domain.Attestation.ValueObjects;
using Zte.Backend.Domain.Challenges.Entities;

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
        var challengeStore = new TestChallengeStore();

        var challenge = new AttestationChallenge(
            ChallengeId: Guid.NewGuid(),
            Nonce: "test-nonce",
            CreatedAtUtc: DateTime.UtcNow,
            ExpiresAtUtc: DateTime.UtcNow.AddMinutes(5));

        challengeStore.Add(challenge);

        var service = new SoftwareAttestationService(challengeStore);

        var request = new SoftwareAttestationRequest(
            BenchmarkRunId: null,
            ChallengeId: challenge.ChallengeId,
            Nonce: challenge.Nonce,
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
        Assert.Equal(5, result.ProcessingStepCount);
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
        var challengeStore = new TestChallengeStore();

        var challenge = new AttestationChallenge(
            ChallengeId: Guid.NewGuid(),
            Nonce: "test-nonce",
            CreatedAtUtc: DateTime.UtcNow,
            ExpiresAtUtc: DateTime.UtcNow.AddMinutes(5));

        challengeStore.Add(challenge);

        var service = new SoftwareAttestationService(challengeStore);

        var request = new SoftwareAttestationRequest(
            BenchmarkRunId: null,
            ChallengeId: challenge.ChallengeId,
            Nonce: challenge.Nonce,
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
        Assert.Equal(5, result.ProcessingStepCount);
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
        var challengeStore = new TestChallengeStore();

        var challenge = new AttestationChallenge(
            ChallengeId: Guid.NewGuid(),
            Nonce: "test-nonce",
            CreatedAtUtc: DateTime.UtcNow,
            ExpiresAtUtc: DateTime.UtcNow.AddMinutes(5));

        challengeStore.Add(challenge);

        var service = new SoftwareAttestationService(challengeStore);

        var request = new SoftwareAttestationRequest(
             BenchmarkRunId: null,
            DeviceId: "test-device-003",
            Platform: "android",
            OsVersion: "14",
            AppVersion: "1.0.0",
            DeviceBrand: "Unknown",
            DeviceModel: "Unknown",
            IsEmulator: true,
            IsRooted: true,
            ChallengeId: Guid.NewGuid(),
            Nonce: "test-nonce",
            ClientTimestampUtc: DateTime.UtcNow);

        // Act
        var result = service.Verify(request, messageSizeBytes: 230);

        // Assert
        // Multiple suspicious signals should classify the request
        // as high risk and reject the verification result.
        Assert.False(result.Accepted);
        Assert.Equal(AttestationType.Software, result.AttestationType);
        Assert.Equal(RiskLevel.High, result.RiskLevel);
        Assert.Equal(5, result.ProcessingStepCount);
        Assert.Equal(230, result.MessageSizeBytes);
        Assert.Contains("Device appears to be an emulator.", result.Reasons);
        Assert.Contains("Device appears to be rooted.", result.Reasons);
    }

    private sealed class TestChallengeStore : IChallengeStore
    {
        private readonly Dictionary<Guid, StoredChallenge> _challenges = [];

        public void Add(AttestationChallenge challenge)
        {
            _challenges[challenge.ChallengeId] = new StoredChallenge(
                Challenge: challenge,
                IsUsed: false);
        }

        public AttestationChallenge? Get(Guid challengeId)
        {
            return _challenges.TryGetValue(challengeId, out var storedChallenge)
                ? storedChallenge.Challenge
                : null;
        }

        public bool MarkAsUsed(Guid challengeId)
        {
            if (!_challenges.TryGetValue(challengeId, out var storedChallenge))
            {
                return false;
            }

            if (storedChallenge.IsUsed)
            {
                return false;
            }

            _challenges[challengeId] = storedChallenge with
            {
                IsUsed = true
            };

            return true;
        }

        private sealed record StoredChallenge(
            AttestationChallenge Challenge,
            bool IsUsed);
    }


}