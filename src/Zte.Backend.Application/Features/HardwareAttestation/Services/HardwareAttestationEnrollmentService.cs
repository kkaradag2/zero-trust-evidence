using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Zte.Backend.Application.Common.Interfaces;
using Zte.Backend.Application.Features.HardwareAttestation.Contracts;
using Zte.Backend.Application.Features.HardwareAttestation.Models;
using Zte.Backend.Domain.Attestation.Enums;

namespace Zte.Backend.Application.Features.HardwareAttestation.Services;

public sealed class HardwareAttestationEnrollmentService : IHardwareAttestationEnrollmentService
{
    private const string AndroidKeyAttestationExtensionOid = "1.3.6.1.4.1.11129.2.1.17";

    private readonly IChallengeStore _challengeStore;
    private readonly IEnrolledDeviceStore _enrolledDeviceStore;
    private readonly IRegisteredDeviceKeyStore _registeredDeviceKeyStore;

    public HardwareAttestationEnrollmentService(
        IChallengeStore challengeStore,
        IEnrolledDeviceStore enrolledDeviceStore,
        IRegisteredDeviceKeyStore registeredDeviceKeyStore)
    {
        _challengeStore = challengeStore;
        _enrolledDeviceStore = enrolledDeviceStore;
        _registeredDeviceKeyStore = registeredDeviceKeyStore;
    }

    public HardwareEnrollmentResult Enroll(
        HardwareEnrollmentRequest request,
        int messageSizeBytes)
    {
        var stopwatch = Stopwatch.StartNew();

        var reasons = new List<string>();
        var warnings = new List<string>();
        var steps = new List<string>();
        string? hardwareSecurityLevel = null;
        string? attestationSecurityLevel = null;

        ValidateRequestShape(request, reasons, steps);
        ValidateChallenge(request, reasons, steps);

        byte[]? publicKeyBytes = null;
        if (!string.IsNullOrWhiteSpace(request.PublicKeyBase64))
        {
            publicKeyBytes = ParsePublicKey(request.PublicKeyBase64, reasons, steps);
        }

        var certificates = ParseCertificateChain(request.CertificateChainBase64, reasons, steps);

        if (publicKeyBytes is not null && certificates.Count > 0)
        {
            ValidateLeafCertificatePublicKey(certificates[0], publicKeyBytes, reasons, steps);
        }

        if (certificates.Count > 0)
        {
            var attestation = TryReadAndroidAttestation(certificates[0], warnings, steps);
            hardwareSecurityLevel = attestation?.KeymasterSecurityLevel;
            attestationSecurityLevel = attestation?.AttestationSecurityLevel;

            if (attestation?.Challenge is not null)
            {
                ValidateAttestationChallenge(attestation.Challenge, request.Nonce, reasons, steps);
            }
            else
            {
                // TODO: Complete Android Key Attestation extension validation for production use.
                warnings.Add("Android attestation extension challenge was not available; enrollment is accepted only with structural certificate validation.");
                steps.Add("Android attestation challenge verification is partial.");
            }
        }

        Guid? enrolledDeviceId = null;

        if (reasons.Count == 0)
        {
            var enrolledDevice = new EnrolledDevice
            {
                EnrolledDeviceId = Guid.NewGuid(),
                DeviceId = request.DeviceId,
                AppInstanceId = request.AppInstanceId,
                KeyAlias = request.KeyAlias,
                PublicKeyBase64 = request.PublicKeyBase64,
                CertificateChainBase64 = request.CertificateChainBase64,
                VerificationSteps = steps,
                VerificationWarnings = warnings,
                HardwareSecurityLevel = hardwareSecurityLevel,
                AttestationSecurityLevel = attestationSecurityLevel,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var saveResult = _enrolledDeviceStore.Save(enrolledDevice);
            enrolledDeviceId = saveResult.Device.EnrolledDeviceId;

            if (!saveResult.Created)
            {
                warnings.Add("Device/key enrollment already exists; existing enrolled device record was returned.");
                steps.Add("Duplicate enrollment resolved deterministically.");
            }
            else
            {
                _registeredDeviceKeyStore.Save(new RegisteredDeviceKey
                {
                    DeviceId = request.DeviceId,
                    KeyAlias = request.KeyAlias,
                    PublicKeyBase64 = request.PublicKeyBase64,
                    CertificateChainBase64 = request.CertificateChainBase64,
                    RegisteredAtUtc = DateTimeOffset.UtcNow,
                    LastVerifiedAtUtc = null
                });

                steps.Add("Enrolled device record stored.");
            }
        }

        stopwatch.Stop();

        var accepted = reasons.Count == 0;

        return new HardwareEnrollmentResult(
            Accepted: accepted,
            EnrolledDeviceId: enrolledDeviceId,
            RiskLevel: accepted && warnings.Count == 0 ? RiskLevel.Low : accepted ? RiskLevel.Medium : RiskLevel.High,
            VerificationSteps: steps,
            Reasons: reasons,
            Warnings: warnings,
            HardwareSecurityLevel: hardwareSecurityLevel,
            AttestationSecurityLevel: attestationSecurityLevel,
            ProcessingStepCount: steps.Count,
            VerificationTimeMs: stopwatch.Elapsed.TotalMilliseconds,
            VerificationTimeMicroseconds: stopwatch.Elapsed.Ticks / 10,
            MessageSizeBytes: messageSizeBytes);
    }

    private static void ValidateRequestShape(
        HardwareEnrollmentRequest request,
        List<string> reasons,
        List<string> steps)
    {
        if (string.IsNullOrWhiteSpace(request.ChallengeId))
        {
            reasons.Add("Attestation challenge id is missing.");
        }

        if (string.IsNullOrWhiteSpace(request.Nonce))
        {
            reasons.Add("Attestation nonce is missing.");
        }

        if (string.IsNullOrWhiteSpace(request.DeviceId) && string.IsNullOrWhiteSpace(request.AppInstanceId))
        {
            reasons.Add("Device id or app instance id is required.");
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

        steps.Add("Enrollment request shape validated.");
    }

    private void ValidateChallenge(
        HardwareEnrollmentRequest request,
        List<string> reasons,
        List<string> steps)
    {
        if (!Guid.TryParse(request.ChallengeId, out var challengeId))
        {
            reasons.Add("Attestation challenge id is invalid.");
            steps.Add("Challenge id rejected.");
            return;
        }

        var challenge = _challengeStore.Get(challengeId);

        if (challenge is null)
        {
            reasons.Add("Attestation challenge was not found.");
            steps.Add("Challenge lookup failed.");
            return;
        }

        if (challenge.ExpiresAtUtc < DateTime.UtcNow)
        {
            reasons.Add("Attestation challenge has expired.");
        }

        if (!string.Equals(challenge.Nonce, request.Nonce, StringComparison.Ordinal))
        {
            reasons.Add("Attestation nonce does not match the issued challenge.");
        }

        if (!_challengeStore.MarkAsUsed(challengeId))
        {
            reasons.Add("Attestation challenge has already been used.");
        }

        steps.Add("Challenge id, nonce, expiration, and single-use state validated.");
    }

    private static byte[]? ParsePublicKey(
        string publicKeyBase64,
        List<string> reasons,
        List<string> steps)
    {
        try
        {
            var publicKeyBytes = Convert.FromBase64String(publicKeyBase64);

            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

            steps.Add("Submitted public key parsed as an EC SubjectPublicKeyInfo value.");
            return publicKeyBytes;
        }
        catch
        {
            reasons.Add("Public key format is invalid or unsupported.");
            steps.Add("Submitted public key parsing failed.");
            return null;
        }
    }

    private static IReadOnlyList<X509Certificate2> ParseCertificateChain(
        IReadOnlyList<string>? certificateChainBase64,
        List<string> reasons,
        List<string> steps)
    {
        var certificates = new List<X509Certificate2>();

        if (certificateChainBase64 is null || certificateChainBase64.Count == 0)
        {
            steps.Add("Certificate chain parsing skipped because no certificates were submitted.");
            return certificates;
        }

        for (var index = 0; index < certificateChainBase64.Count; index++)
        {
            try
            {
                var certificateBytes = Convert.FromBase64String(certificateChainBase64[index]);
                certificates.Add(X509CertificateLoader.LoadCertificate(certificateBytes));
            }
            catch
            {
                reasons.Add($"Certificate chain entry {index} is not a valid X.509 certificate.");
            }
        }

        if (certificates.Count == certificateChainBase64.Count)
        {
            steps.Add("Certificate chain entries parsed as X.509 certificates.");
        }
        else
        {
            steps.Add("Certificate chain structural parsing failed.");
        }

        return certificates;
    }

    private static void ValidateLeafCertificatePublicKey(
        X509Certificate2 leafCertificate,
        byte[] publicKeyBytes,
        List<string> reasons,
        List<string> steps)
    {
        try
        {
            var leafPublicKey = leafCertificate.PublicKey.ExportSubjectPublicKeyInfo();

            if (!CryptographicOperations.FixedTimeEquals(leafPublicKey, publicKeyBytes))
            {
                reasons.Add("Submitted public key does not match the leaf attestation certificate public key.");
            }

            steps.Add("Leaf certificate public key compared with submitted public key.");
        }
        catch
        {
            reasons.Add("Leaf attestation certificate public key could not be parsed.");
            steps.Add("Leaf certificate public key parsing failed.");
        }
    }

    private static AndroidAttestationInfo? TryReadAndroidAttestation(
        X509Certificate2 leafCertificate,
        List<string> warnings,
        List<string> steps)
    {
        var extension = leafCertificate.Extensions
            .OfType<X509Extension>()
            .FirstOrDefault(candidate => candidate.Oid?.Value == AndroidKeyAttestationExtensionOid);

        if (extension is null)
        {
            warnings.Add("Android key attestation certificate extension is missing.");
            steps.Add("Android attestation extension was not present.");
            return null;
        }

        try
        {
            var reader = new AsnReader(extension.RawData, AsnEncodingRules.DER);
            var sequence = reader.ReadSequence();

            sequence.ReadInteger();
            var attestationSecurityLevel = ReadSecurityLevel(sequence.ReadEnumeratedValue<AndroidSecurityLevel>());
            sequence.ReadInteger();
            var keymasterSecurityLevel = ReadSecurityLevel(sequence.ReadEnumeratedValue<AndroidSecurityLevel>());
            var challenge = sequence.ReadOctetString();

            steps.Add("Android attestation extension parsed.");

            return new AndroidAttestationInfo(
                Challenge: challenge,
                AttestationSecurityLevel: attestationSecurityLevel,
                KeymasterSecurityLevel: keymasterSecurityLevel);
        }
        catch
        {
            warnings.Add("Android attestation extension is present but could not be fully parsed.");
            steps.Add("Android attestation extension parsing failed.");
            return null;
        }
    }

    private static void ValidateAttestationChallenge(
        byte[] attestationChallenge,
        string nonce,
        List<string> reasons,
        List<string> steps)
    {
        try
        {
            var nonceBytes = Convert.FromBase64String(nonce);

            if (!CryptographicOperations.FixedTimeEquals(attestationChallenge, nonceBytes))
            {
                reasons.Add("Attestation certificate challenge does not match the issued nonce.");
            }

            steps.Add("Attestation certificate challenge compared with backend nonce.");
        }
        catch
        {
            reasons.Add("Backend nonce is not valid base64 and cannot be compared to attestation evidence.");
            steps.Add("Attestation challenge comparison failed.");
        }
    }

    private static string ReadSecurityLevel(AndroidSecurityLevel value)
    {
        return value switch
        {
            AndroidSecurityLevel.Software => "Software",
            AndroidSecurityLevel.TrustedEnvironment => "TrustedEnvironment",
            AndroidSecurityLevel.StrongBox => "StrongBox",
            _ => $"Unknown({value})"
        };
    }

    private enum AndroidSecurityLevel
    {
        Software = 0,
        TrustedEnvironment = 1,
        StrongBox = 2
    }

    private sealed record AndroidAttestationInfo(
        byte[] Challenge,
        string AttestationSecurityLevel,
        string KeymasterSecurityLevel);
}
