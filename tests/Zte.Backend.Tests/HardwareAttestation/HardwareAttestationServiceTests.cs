using System.Security.Cryptography;
using Zte.Backend.Application.Features.HardwareAttestation.Contracts;
using Zte.Backend.Application.Features.HardwareAttestation.Models;
using Zte.Backend.Application.Features.HardwareAttestation.Services;
using Zte.Backend.Domain.Attestation.Enums;
using Zte.Backend.Domain.Attestation.ValueObjects;
using Zte.Backend.Domain.Challenges.Entities;
using Zte.Backend.Infrastructure.Persistence.Challenges;
using Zte.Backend.Infrastructure.Persistence.HardwareAttestation;

namespace Zte.Backend.Tests.HardwareAttestation;

public sealed class HardwareAttestationServiceTests
{
    [Fact]
    public void Verify_WhenDeviceKeyIsNotRegistered_ReturnsHighRiskRejectedHardwareResult()
    {
        // Arrange
        var challengeStore = new InMemoryChallengeStore();
        var registeredDeviceKeyStore = new InMemoryRegisteredDeviceKeyStore();

        var createdAtUtc = DateTime.UtcNow;

        var challenge = new AttestationChallenge(
            ChallengeId: Guid.NewGuid(),
            Nonce: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            CreatedAtUtc: createdAtUtc,
            ExpiresAtUtc: createdAtUtc.AddMinutes(5));

        challengeStore.Add(challenge);

        var service = new HardwareAttestationService(
            challengeStore,
            registeredDeviceKeyStore);

        var request = new HardwareVerificationRequest
        {
            ChallengeId = challenge.ChallengeId.ToString(),
            Nonce = challenge.Nonce,
            DeviceId = "demo-device-001",
            KeyAlias = "zero_trust_attestation_key",
            SignatureBase64 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ClientTimestampUtc = DateTimeOffset.UtcNow
        };

        // Act
        var result = service.Verify(
            request,
            messageSizeBytes: 256);

        // Assert
        Assert.False(result.Accepted);
        Assert.Equal(AttestationType.Hardware, result.AttestationType);
        Assert.Equal(RiskLevel.High, result.RiskLevel);
        Assert.Contains(
            "Registered hardware key was not found",
            string.Join(" ", result.Reasons));
    }


    [Fact]
    public void Enroll_WhenRequestContainsValidChallengeAndPublicKey_ReturnsAcceptedHardwareResult()
    {
        // Arrange
        var challengeStore = new InMemoryChallengeStore();
        var registeredDeviceKeyStore = new InMemoryRegisteredDeviceKeyStore();

        var createdAtUtc = DateTime.UtcNow;

        var challenge = new AttestationChallenge(
            ChallengeId: Guid.NewGuid(),
            Nonce: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            CreatedAtUtc: createdAtUtc,
            ExpiresAtUtc: createdAtUtc.AddMinutes(5));

        challengeStore.Add(challenge);

        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        var publicKeyBase64 = Convert.ToBase64String(
            ecdsa.ExportSubjectPublicKeyInfo());

        var service = new HardwareAttestationService(
            challengeStore,
            registeredDeviceKeyStore);

        var request = new HardwareEnrollmentRequest
        {
            ChallengeId = challenge.ChallengeId.ToString(),
            Nonce = challenge.Nonce,
            DeviceId = "demo-device-001",
            KeyAlias = "zero_trust_attestation_key",
            PublicKeyBase64 = publicKeyBase64,
            CertificateChainBase64 =
            [
                Convert.ToBase64String(RandomNumberGenerator.GetBytes(512))
            ],
            ClientTimestampUtc = DateTimeOffset.UtcNow
        };

        // Act
        var result = service.Enroll(
            request,
            messageSizeBytes: 512);

        var registeredKey = registeredDeviceKeyStore.Find(
            request.DeviceId,
            request.KeyAlias);

        // Assert
        Assert.True(result.Accepted);
        Assert.Equal(AttestationType.Hardware, result.AttestationType);
        Assert.Equal(RiskLevel.Low, result.RiskLevel);

        Assert.NotNull(registeredKey);
        Assert.Equal(request.DeviceId, registeredKey.DeviceId);
        Assert.Equal(request.KeyAlias, registeredKey.KeyAlias);
        Assert.Equal(publicKeyBase64, registeredKey.PublicKeyBase64);
    }

    [Fact]
    public void Verify_WhenDeviceWasEnrolledAndSignatureIsValid_ReturnsAcceptedHardwareResult()
    {
        // Arrange
        var challengeStore = new InMemoryChallengeStore();
        var registeredDeviceKeyStore = new InMemoryRegisteredDeviceKeyStore();

        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        var publicKeyBase64 = Convert.ToBase64String(
            ecdsa.ExportSubjectPublicKeyInfo());

        var service = new HardwareAttestationService(
            challengeStore,
            registeredDeviceKeyStore);

        var deviceId = "demo-device-001";
        var keyAlias = "zero_trust_attestation_key";

        // Enrollment challenge
        var enrollmentCreatedAtUtc = DateTime.UtcNow;

        var enrollmentChallenge = new AttestationChallenge(
            ChallengeId: Guid.NewGuid(),
            Nonce: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            CreatedAtUtc: enrollmentCreatedAtUtc,
            ExpiresAtUtc: enrollmentCreatedAtUtc.AddMinutes(5));

        challengeStore.Add(enrollmentChallenge);

        var enrollmentRequest = new HardwareEnrollmentRequest
        {
            ChallengeId = enrollmentChallenge.ChallengeId.ToString(),
            Nonce = enrollmentChallenge.Nonce,
            DeviceId = deviceId,
            KeyAlias = keyAlias,
            PublicKeyBase64 = publicKeyBase64,
            CertificateChainBase64 =
            [
                Convert.ToBase64String(RandomNumberGenerator.GetBytes(512))
            ],
            ClientTimestampUtc = DateTimeOffset.UtcNow
        };

        var enrollmentResult = service.Enroll(
            enrollmentRequest,
            messageSizeBytes: 512);

        Assert.True(enrollmentResult.Accepted);

        // Verification challenge
        var verificationCreatedAtUtc = DateTime.UtcNow;

        var verificationChallenge = new AttestationChallenge(
            ChallengeId: Guid.NewGuid(),
            Nonce: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            CreatedAtUtc: verificationCreatedAtUtc,
            ExpiresAtUtc: verificationCreatedAtUtc.AddMinutes(5));

        challengeStore.Add(verificationChallenge);

        var nonceBytes = Convert.FromBase64String(verificationChallenge.Nonce);

        var signatureBytes = ecdsa.SignData(
            nonceBytes,
            HashAlgorithmName.SHA256,
            DSASignatureFormat.Rfc3279DerSequence);

        var verificationRequest = new HardwareVerificationRequest
        {
            ChallengeId = verificationChallenge.ChallengeId.ToString(),
            Nonce = verificationChallenge.Nonce,
            DeviceId = deviceId,
            KeyAlias = keyAlias,
            SignatureBase64 = Convert.ToBase64String(signatureBytes),
            ClientTimestampUtc = DateTimeOffset.UtcNow
        };

        // Act
        var verificationResult = service.Verify(
            verificationRequest,
            messageSizeBytes: 256);

        // Assert
        Assert.True(verificationResult.Accepted);
        Assert.Equal(AttestationType.Hardware, verificationResult.AttestationType);
        Assert.Equal(RiskLevel.Low, verificationResult.RiskLevel);
        Assert.Empty(verificationResult.Reasons);
    }

}