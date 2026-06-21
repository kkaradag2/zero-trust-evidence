using System.Diagnostics;
using System.Security.Cryptography;
using Zte.Backend.Application.Challenges;
using Zte.Backend.Domain.Attestation;

namespace Zte.Backend.Application.HardwareAttestation;

public sealed class HardwareAttestationService : IHardwareAttestationService
{
    private readonly IChallengeStore _challengeStore;
    private readonly IRegisteredDeviceKeyStore _registeredDeviceKeyStore;

    public HardwareAttestationService(
        IChallengeStore challengeStore,
        IRegisteredDeviceKeyStore registeredDeviceKeyStore)
    {
        _challengeStore = challengeStore;
        _registeredDeviceKeyStore = registeredDeviceKeyStore;
    }

    public VerificationResult Enroll(
        HardwareEnrollmentRequest request,
        int messageSizeBytes)
    {
        var stopwatch = Stopwatch.StartNew();

        var reasons = new List<string>();
        var processingStepCount = 0;

        processingStepCount++;
        ValidateChallenge(request.ChallengeId, request.Nonce, reasons);

        processingStepCount++;
        ValidateEnrollmentRequest(request, reasons);

        processingStepCount++;
        if (!CanImportEcPublicKey(request.PublicKeyBase64))
        {
            reasons.Add("Public key format is invalid or unsupported.");
        }

        processingStepCount++;
        if (reasons.Count == 0)
        {
            var registeredDeviceKey = new RegisteredDeviceKey
            {
                DeviceId = request.DeviceId,
                KeyAlias = request.KeyAlias,
                PublicKeyBase64 = request.PublicKeyBase64,
                CertificateChainBase64 = request.CertificateChainBase64,
                RegisteredAtUtc = DateTimeOffset.UtcNow,
                LastVerifiedAtUtc = null
            };

            _registeredDeviceKeyStore.Save(registeredDeviceKey);
        }

        stopwatch.Stop();

        return CreateResult(
            accepted: reasons.Count == 0,
            reasons: reasons,
            processingStepCount: processingStepCount,
            verificationTimeMs: stopwatch.Elapsed.TotalMilliseconds,
            verificationTimeMicroseconds: ToMicroseconds(stopwatch.Elapsed),
            messageSizeBytes: messageSizeBytes);
    }

    public VerificationResult Verify(
        HardwareVerificationRequest request,
        int messageSizeBytes)
    {
        var stopwatch = Stopwatch.StartNew();

        var reasons = new List<string>();
        var processingStepCount = 0;

        processingStepCount++;
        ValidateChallenge(request.ChallengeId, request.Nonce, reasons);

        processingStepCount++;
        ValidateVerificationRequest(request, reasons);

        processingStepCount++;
        var registeredDeviceKey = _registeredDeviceKeyStore.Find(
            request.DeviceId,
            request.KeyAlias);

        if (registeredDeviceKey is null)
        {
            reasons.Add("Registered hardware key was not found for this device.");
        }

        processingStepCount++;
        if (registeredDeviceKey is not null)
        {
            var signatureValid = VerifySignature(
                registeredDeviceKey.PublicKeyBase64,
                request.Nonce,
                request.SignatureBase64);

            if (!signatureValid)
            {
                reasons.Add("Hardware key signature is invalid.");
            }
        }

        stopwatch.Stop();

        return CreateResult(
            accepted: reasons.Count == 0,
            reasons: reasons,
            processingStepCount: processingStepCount,
            verificationTimeMs: stopwatch.Elapsed.TotalMilliseconds,
            verificationTimeMicroseconds: ToMicroseconds(stopwatch.Elapsed),
            messageSizeBytes: messageSizeBytes);
    }

    private void ValidateChallenge(
        string challengeIdValue,
        string nonce,
        List<string> reasons)
    {
        if (!Guid.TryParse(challengeIdValue, out var challengeId))
        {
            reasons.Add("Attestation challenge id is invalid.");
            return;
        }

        var challenge = _challengeStore.Get(challengeId);

        if (challenge is null)
        {
            reasons.Add("Attestation challenge was not found.");
            return;
        }

        if (challenge.ExpiresAtUtc < DateTime.UtcNow)
        {
            reasons.Add("Attestation challenge has expired.");
        }

        if (!string.Equals(challenge.Nonce, nonce, StringComparison.Ordinal))
        {
            reasons.Add("Attestation nonce does not match the issued challenge.");
        }

        if (!_challengeStore.MarkAsUsed(challengeId))
        {
            reasons.Add("Attestation challenge has already been used.");
        }
    }

    private static void ValidateEnrollmentRequest(
        HardwareEnrollmentRequest request,
        List<string> reasons)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            reasons.Add("Device id is missing.");
        }

        if (string.IsNullOrWhiteSpace(request.KeyAlias))
        {
            reasons.Add("Key alias is missing.");
        }

        if (string.IsNullOrWhiteSpace(request.PublicKeyBase64))
        {
            reasons.Add("Public key is missing.");
        }

        if (request.CertificateChainBase64 is null || request.CertificateChainBase64.Count == 0)
        {
            reasons.Add("Attestation certificate chain is missing.");
        }
    }

    private static void ValidateVerificationRequest(
        HardwareVerificationRequest request,
        List<string> reasons)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            reasons.Add("Device id is missing.");
        }

        if (string.IsNullOrWhiteSpace(request.KeyAlias))
        {
            reasons.Add("Key alias is missing.");
        }

        if (string.IsNullOrWhiteSpace(request.SignatureBase64))
        {
            reasons.Add("Signature is missing.");
        }
    }

    private static VerificationResult CreateResult(
        bool accepted,
        IReadOnlyList<string> reasons,
        int processingStepCount,
        double verificationTimeMs,
        long verificationTimeMicroseconds,
        int messageSizeBytes)
    {
        return new VerificationResult(
            Accepted: accepted,
            AttestationType: AttestationType.Hardware,
            RiskLevel: accepted ? RiskLevel.Low : RiskLevel.High,
            ProcessingStepCount: processingStepCount,
            VerificationTimeMs: verificationTimeMs,
            VerificationTimeMicroseconds: verificationTimeMicroseconds,
            MessageSizeBytes: messageSizeBytes,
            Reasons: reasons);
    }

    private static bool CanImportEcPublicKey(string publicKeyBase64)
    {
        try
        {
            var publicKeyBytes = Convert.FromBase64String(publicKeyBase64);

            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool VerifySignature(
        string publicKeyBase64,
        string nonce,
        string signatureBase64)
    {
        try
        {
            var publicKeyBytes = Convert.FromBase64String(publicKeyBase64);
            var signatureBytes = Convert.FromBase64String(signatureBase64);
            var nonceBytes = Convert.FromBase64String(nonce);

            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

            return ecdsa.VerifyData(
                nonceBytes,
                signatureBytes,
                HashAlgorithmName.SHA256,
                DSASignatureFormat.Rfc3279DerSequence);
        }
        catch
        {
            return false;
        }
    }

    private static long ToMicroseconds(TimeSpan elapsed)
    {
        return elapsed.Ticks / 10;
    }
}