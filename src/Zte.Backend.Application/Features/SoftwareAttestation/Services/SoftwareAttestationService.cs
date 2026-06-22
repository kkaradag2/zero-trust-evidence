using System.Diagnostics;
using Zte.Backend.Application.Common.Interfaces;
using Zte.Backend.Application.Features.SoftwareAttestation.Contracts;
using Zte.Backend.Domain.Attestation.Enums;
using Zte.Backend.Domain.Attestation.ValueObjects;

namespace Zte.Backend.Application.Features.SoftwareAttestation.Services;

/// <summary>
/// Performs rule-based verification for software/context-aware attestation requests.
/// 
/// This service represents the software attestation baseline in the experiment.
/// It evaluates client-provided posture signals such as platform, app version,
/// emulator status, and root status.
/// 
/// Important:
/// These signals are still client-declared and should not be interpreted as
/// strong cryptographic proof of device integrity.
/// </summary>
public sealed class SoftwareAttestationService : ISoftwareAttestationService
{
    private readonly IChallengeStore _challengeStore;

    /// <summary>
    /// Creates a software attestation verification service.
    /// The challenge store is used to validate server-issued nonces and reduce replay risk.
    /// </summary>
    public SoftwareAttestationService(IChallengeStore challengeStore)
    {
        _challengeStore = challengeStore;
    }

    /// <summary>
    /// Verifies a software/context-aware attestation request and returns the verification result.
    /// 
    /// The method measures backend-side verification time, counts major processing steps,
    /// and produces a simplified risk level based on detected suspicious signals.
    /// </summary>
    public VerificationResult Verify(SoftwareAttestationRequest request, int messageSizeBytes)
    {
        // Measures only the backend-side verification time.
        // End-to-end client round-trip time is measured separately by the mobile client.
        var stopwatch = Stopwatch.StartNew();

        var reasons = new List<string>();
        var processingStepCount = 0;

        // Step 1:
        // Validate the server-issued challenge.
        // This does not make software attestation tamper-proof, but it binds the request
        // to a fresh backend-generated nonce and reduces simple replay attempts.
        processingStepCount++;
        var challenge = _challengeStore.Get(request.ChallengeId);

        if (challenge is null)
        {
            reasons.Add("Attestation challenge was not found.");
        }
        else
        {
            // Expired challenges should not be accepted because they may represent
            // delayed or replayed verification attempts.
            if (challenge.ExpiresAtUtc < DateTime.UtcNow)
            {
                reasons.Add("Attestation challenge has expired.");
            }

            // The nonce returned by the client must exactly match the nonce generated
            // by the backend for this challenge.
            if (!string.Equals(challenge.Nonce, request.Nonce, StringComparison.Ordinal))
            {
                reasons.Add("Attestation nonce does not match the issued challenge.");
            }

            // A valid challenge should be usable only once.
            // If MarkAsUsed returns false, the challenge is either missing or already used.
            if (!_challengeStore.MarkAsUsed(request.ChallengeId))
            {
                reasons.Add("Attestation challenge has already been used.");
            }
        }

        // Step 2:
        // Restrict the experimental scenario to Android only.
        // This matches the project scope and avoids mixing platforms in the evaluation.
        processingStepCount++;
        if (!string.Equals(request.Platform, "android", StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("Only Android platform is supported in this experiment.");
        }

        // Step 3:
        // Require application version as a basic software posture signal.
        // Missing version information makes it harder to reason about client state.
        processingStepCount++;
        if (string.IsNullOrWhiteSpace(request.AppVersion))
        {
            reasons.Add("Application version is missing.");
        }

        // Step 4:
        // Record emulator detection as a suspicious software-level signal.
        // In this simplified policy, one suspicious signal increases the risk level.
        processingStepCount++;
        if (request.IsEmulator)
        {
            reasons.Add("Device appears to be an emulator.");
        }

        // Step 5:
        // Record root detection as another suspicious software-level signal.
        // Multiple suspicious signals lead to a high-risk result in this prototype.
        processingStepCount++;
        if (request.IsRooted)
        {
            reasons.Add("Device appears to be rooted.");
        }

        // Convert the number of detected issues into a simplified risk level.
        // This is intentionally basic because the project compares verification flows,
        // not advanced risk scoring algorithms.
        var riskLevel = reasons.Count switch
        {
            0 => RiskLevel.Low,
            1 => RiskLevel.Medium,
            _ => RiskLevel.High
        };

        // In this prototype, high-risk requests are rejected.
        // Low and medium risk requests remain accepted so their measurements can still be observed.
        var accepted = riskLevel != RiskLevel.High;

        stopwatch.Stop();

        var verificationTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        var verificationTimeMicroseconds =
            stopwatch.ElapsedTicks * 1_000_000L / Stopwatch.Frequency;

        return new VerificationResult(
            Accepted: accepted,
            AttestationType: AttestationType.Software,
            RiskLevel: riskLevel,
            ProcessingStepCount: processingStepCount,
            VerificationTimeMs: verificationTimeMs,
            VerificationTimeMicroseconds: verificationTimeMicroseconds,
            MessageSizeBytes: messageSizeBytes,
            Reasons: reasons);
    }
}
