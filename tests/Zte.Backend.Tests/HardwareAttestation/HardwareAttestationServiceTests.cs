using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Zte.Backend.Application.Features.HardwareAttestation.Contracts;
using Zte.Backend.Application.Features.HardwareAttestation.Services;
using Zte.Backend.Domain.Attestation.Enums;
using Zte.Backend.Domain.Challenges.Entities;
using Zte.Backend.Infrastructure.Persistence.Challenges;
using Zte.Backend.Infrastructure.Persistence.HardwareAttestation;

namespace Zte.Backend.Tests.HardwareAttestation;

public sealed class HardwareAttestationServiceTests
{
    [Fact]
    public void Enroll_WhenChallengeAndPayloadAreValid_StoresEnrolledDevice()
    {
        var fixture = CreateEnrollmentFixture();

        var result = fixture.EnrollmentService.Enroll(
            fixture.Request,
            messageSizeBytes: 512);

        var enrolledDevice = fixture.EnrolledDeviceStore.Find(
            fixture.Request.DeviceId,
            fixture.Request.KeyAlias);

        Assert.True(result.Accepted);
        Assert.NotNull(result.EnrolledDeviceId);
        Assert.Equal(RiskLevel.Low, result.RiskLevel);
        Assert.Empty(result.Reasons);
        Assert.Contains("Attestation certificate challenge compared with backend nonce.", result.VerificationSteps);
        Assert.Equal("StrongBox", result.HardwareSecurityLevel);
        Assert.NotNull(enrolledDevice);
        Assert.Equal(result.EnrolledDeviceId, enrolledDevice.EnrolledDeviceId);
    }

    [Fact]
    public void Enroll_WhenChallengeIsExpired_RejectsEnrollment()
    {
        var fixture = CreateEnrollmentFixture(challengeExpiresAtUtc: DateTime.UtcNow.AddMinutes(-1));

        var result = fixture.EnrollmentService.Enroll(
            fixture.Request,
            messageSizeBytes: 512);

        Assert.False(result.Accepted);
        Assert.Equal(RiskLevel.High, result.RiskLevel);
        Assert.Contains("Attestation challenge has expired.", result.Reasons);
    }

    [Fact]
    public void Enroll_WhenNonceDoesNotMatch_RejectsEnrollment()
    {
        var fixture = CreateEnrollmentFixture(requestNonce: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));

        var result = fixture.EnrollmentService.Enroll(
            fixture.Request,
            messageSizeBytes: 512);

        Assert.False(result.Accepted);
        Assert.Contains("Attestation nonce does not match the issued challenge.", result.Reasons);
    }

    [Fact]
    public void Enroll_WhenCertificateChainIsMissing_RejectsEnrollment()
    {
        var fixture = CreateEnrollmentFixture(certificateChainBase64: []);

        var result = fixture.EnrollmentService.Enroll(
            fixture.Request,
            messageSizeBytes: 512);

        Assert.False(result.Accepted);
        Assert.Contains("Attestation certificate chain is missing.", result.Reasons);
    }

    [Fact]
    public void Enroll_WhenPublicKeyIsInvalid_RejectsEnrollment()
    {
        var fixture = CreateEnrollmentFixture(publicKeyBase64: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));

        var result = fixture.EnrollmentService.Enroll(
            fixture.Request,
            messageSizeBytes: 512);

        Assert.False(result.Accepted);
        Assert.Contains("Public key format is invalid or unsupported.", result.Reasons);
    }

    [Fact]
    public void Enroll_WhenCertificateChainIsMalformed_RejectsEnrollment()
    {
        var fixture = CreateEnrollmentFixture(certificateChainBase64:
        [
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(512))
        ]);

        var result = fixture.EnrollmentService.Enroll(
            fixture.Request,
            messageSizeBytes: 512);

        Assert.False(result.Accepted);
        Assert.Contains("Certificate chain entry 0 is not a valid X.509 certificate.", result.Reasons);
    }

    [Fact]
    public void Enroll_WhenDeviceAndKeyAlreadyExist_ReturnsExistingEnrollment()
    {
        var fixture = CreateEnrollmentFixture();

        var first = fixture.EnrollmentService.Enroll(
            fixture.Request,
            messageSizeBytes: 512);

        var secondFixture = CreateEnrollmentFixture(
            challengeStore: fixture.ChallengeStore,
            enrolledDeviceStore: fixture.EnrolledDeviceStore,
            registeredDeviceKeyStore: fixture.RegisteredDeviceKeyStore,
            deviceId: fixture.Request.DeviceId,
            keyAlias: fixture.Request.KeyAlias,
            ecdsa: fixture.Ecdsa);

        var second = secondFixture.EnrollmentService.Enroll(
            secondFixture.Request,
            messageSizeBytes: 512);

        Assert.True(first.Accepted);
        Assert.True(second.Accepted);
        Assert.Equal(first.EnrolledDeviceId, second.EnrolledDeviceId);
        Assert.Contains("Device/key enrollment already exists; existing enrolled device record was returned.", second.Warnings);
        Assert.Equal(RiskLevel.Medium, second.RiskLevel);
    }

    [Fact]
    public void Verify_WhenDeviceKeyIsNotRegistered_ReturnsHighRiskRejectedHardwareResult()
    {
        var challengeStore = new InMemoryChallengeStore();
        var registeredDeviceKeyStore = new InMemoryRegisteredDeviceKeyStore();
        var challenge = AddChallenge(challengeStore);
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

        var result = service.Verify(
            request,
            messageSizeBytes: 256);

        Assert.False(result.Accepted);
        Assert.Equal(AttestationType.Hardware, result.AttestationType);
        Assert.Equal(RiskLevel.High, result.RiskLevel);
        Assert.Contains("Registered hardware key was not found", string.Join(" ", result.Reasons));
    }

    [Fact]
    public void Verify_WhenDeviceWasEnrolledAndSignatureIsValid_ReturnsAcceptedHardwareResult()
    {
        var fixture = CreateEnrollmentFixture();
        var enrollmentResult = fixture.EnrollmentService.Enroll(
            fixture.Request,
            messageSizeBytes: 512);

        Assert.True(enrollmentResult.Accepted);

        var verificationChallenge = AddChallenge(fixture.ChallengeStore);
        var nonceBytes = Convert.FromBase64String(verificationChallenge.Nonce);
        var signatureBytes = fixture.Ecdsa.SignData(
            nonceBytes,
            HashAlgorithmName.SHA256,
            DSASignatureFormat.Rfc3279DerSequence);

        var verificationRequest = new HardwareVerificationRequest
        {
            ChallengeId = verificationChallenge.ChallengeId.ToString(),
            Nonce = verificationChallenge.Nonce,
            DeviceId = fixture.Request.DeviceId,
            KeyAlias = fixture.Request.KeyAlias,
            SignatureBase64 = Convert.ToBase64String(signatureBytes),
            ClientTimestampUtc = DateTimeOffset.UtcNow
        };

        var verificationService = new HardwareAttestationService(
            fixture.ChallengeStore,
            fixture.RegisteredDeviceKeyStore);

        var verificationResult = verificationService.Verify(
            verificationRequest,
            messageSizeBytes: 256);

        Assert.True(verificationResult.Accepted);
        Assert.Equal(AttestationType.Hardware, verificationResult.AttestationType);
        Assert.Equal(RiskLevel.Low, verificationResult.RiskLevel);
        Assert.Empty(verificationResult.Reasons);
    }

    private static EnrollmentFixture CreateEnrollmentFixture(
        InMemoryChallengeStore? challengeStore = null,
        InMemoryEnrolledDeviceStore? enrolledDeviceStore = null,
        InMemoryRegisteredDeviceKeyStore? registeredDeviceKeyStore = null,
        string? requestNonce = null,
        DateTime? challengeExpiresAtUtc = null,
        string? publicKeyBase64 = null,
        IReadOnlyList<string>? certificateChainBase64 = null,
        string deviceId = "demo-device-001",
        string keyAlias = "zero_trust_attestation_key",
        ECDsa? ecdsa = null)
    {
        challengeStore ??= new InMemoryChallengeStore();
        enrolledDeviceStore ??= new InMemoryEnrolledDeviceStore();
        registeredDeviceKeyStore ??= new InMemoryRegisteredDeviceKeyStore();
        ecdsa ??= ECDsa.Create(ECCurve.NamedCurves.nistP256);

        var challenge = AddChallenge(challengeStore, expiresAtUtc: challengeExpiresAtUtc);
        var nonce = requestNonce ?? challenge.Nonce;
        var actualPublicKeyBase64 = publicKeyBase64 ?? Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());
        var actualCertificateChainBase64 = certificateChainBase64 ?? [CreateCertificateBase64(ecdsa, Convert.FromBase64String(nonce))];

        var enrollmentService = new HardwareAttestationEnrollmentService(
            challengeStore,
            enrolledDeviceStore,
            registeredDeviceKeyStore);

        var request = new HardwareEnrollmentRequest
        {
            ChallengeId = challenge.ChallengeId.ToString(),
            Nonce = nonce,
            DeviceId = deviceId,
            AppInstanceId = "app-instance-001",
            KeyAlias = keyAlias,
            PublicKeyBase64 = actualPublicKeyBase64,
            AttestationEvidence = "android-key-attestation",
            CertificateChainBase64 = actualCertificateChainBase64,
            ClientTimestampUtc = DateTimeOffset.UtcNow
        };

        return new EnrollmentFixture(
            ChallengeStore: challengeStore,
            EnrolledDeviceStore: enrolledDeviceStore,
            RegisteredDeviceKeyStore: registeredDeviceKeyStore,
            EnrollmentService: enrollmentService,
            Request: request,
            Ecdsa: ecdsa);
    }

    private static AttestationChallenge AddChallenge(
        InMemoryChallengeStore challengeStore,
        DateTime? expiresAtUtc = null)
    {
        var createdAtUtc = DateTime.UtcNow;

        var challenge = new AttestationChallenge(
            ChallengeId: Guid.NewGuid(),
            Nonce: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            CreatedAtUtc: createdAtUtc,
            ExpiresAtUtc: expiresAtUtc ?? createdAtUtc.AddMinutes(5));

        challengeStore.Add(challenge);

        return challenge;
    }

    private static string CreateCertificateBase64(
        ECDsa ecdsa,
        byte[] attestationChallenge)
    {
        var request = new CertificateRequest(
            "CN=Zero Trust Evidence Test Attestation",
            ecdsa,
            HashAlgorithmName.SHA256);

        request.CertificateExtensions.Add(new X509Extension(
            "1.3.6.1.4.1.11129.2.1.17",
            CreateAndroidAttestationExtension(attestationChallenge),
            critical: false));

        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddMinutes(5));

        return Convert.ToBase64String(certificate.Export(X509ContentType.Cert));
    }

    private static byte[] CreateAndroidAttestationExtension(byte[] challenge)
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);

        writer.PushSequence();
        writer.WriteInteger(3);
        writer.WriteEnumeratedValue(AndroidSecurityLevel.TrustedEnvironment);
        writer.WriteInteger(4);
        writer.WriteEnumeratedValue(AndroidSecurityLevel.StrongBox);
        writer.WriteOctetString(challenge);
        writer.WriteOctetString([]);
        writer.PushSequence();
        writer.PopSequence();
        writer.PushSequence();
        writer.PopSequence();
        writer.PopSequence();

        return writer.Encode();
    }

    private enum AndroidSecurityLevel
    {
        Software = 0,
        TrustedEnvironment = 1,
        StrongBox = 2
    }

    private sealed record EnrollmentFixture(
        InMemoryChallengeStore ChallengeStore,
        InMemoryEnrolledDeviceStore EnrolledDeviceStore,
        InMemoryRegisteredDeviceKeyStore RegisteredDeviceKeyStore,
        HardwareAttestationEnrollmentService EnrollmentService,
        HardwareEnrollmentRequest Request,
        ECDsa Ecdsa);
}
