using System.Diagnostics;
using Zte.Backend.Domain.Attestation;

namespace Zte.Backend.Application.SoftwareAttestation;

public sealed class SoftwareAttestationService : ISoftwareAttestationService
{
    public VerificationResult Verify(SoftwareAttestationRequest request, int messageSizeBytes)
    {
        var stopwatch = Stopwatch.StartNew();

        var reasons = new List<string>();
        var processingStepCount = 0;

        processingStepCount++;
        if (!string.Equals(request.Platform, "android", StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add("Only Android platform is supported in this experiment.");
        }

        processingStepCount++;
        if (string.IsNullOrWhiteSpace(request.AppVersion))
        {
            reasons.Add("Application version is missing.");
        }

        processingStepCount++;
        if (request.IsEmulator)
        {
            reasons.Add("Device appears to be an emulator.");
        }

        processingStepCount++;
        if (request.IsRooted)
        {
            reasons.Add("Device appears to be rooted.");
        }

        var riskLevel = reasons.Count switch
        {
            0 => RiskLevel.Low,
            1 => RiskLevel.Medium,
            _ => RiskLevel.High
        };

        var accepted = riskLevel != RiskLevel.High;

        stopwatch.Stop();

        return new VerificationResult(
            Accepted: accepted,
            AttestationType: AttestationType.Software,
            RiskLevel: riskLevel,
            ProcessingStepCount: processingStepCount,
            VerificationTimeMs: stopwatch.ElapsedMilliseconds,
            MessageSizeBytes: messageSizeBytes,
            Reasons: reasons);
    }
}