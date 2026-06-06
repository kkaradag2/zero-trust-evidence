import {
  AttestationChallenge,
  SoftwareAttestationClientResult,
  SoftwareAttestationRequest,
  VerificationResult,
} from '../types/attestation';

const API_BASE_URL = 'http://10.0.2.2:5145';

export async function runSoftwareAttestation(): Promise<SoftwareAttestationClientResult> {
  const challengeResponse = await fetch(`${API_BASE_URL}/api/attestation/challenge`, {
    method: 'GET',
  });

  if (!challengeResponse.ok) {
    throw new Error(`Challenge request failed with status ${challengeResponse.status}`);
  }

  const challenge = (await challengeResponse.json()) as AttestationChallenge;

  const request: SoftwareAttestationRequest = {
    challengeId: challenge.challengeId,
    nonce: challenge.nonce,
    deviceId: 'android-test-device-001',
    platform: 'android',
    osVersion: '14',
    appVersion: '1.0.0',
    deviceBrand: 'Android',
    deviceModel: 'React Native Emulator',
    isEmulator: true,
    isRooted: false,
    clientTimestampUtc: new Date().toISOString(),
  };

  const startedAt = Date.now();

  const response = await fetch(`${API_BASE_URL}/api/attestation/software/verify`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(`Software attestation failed with status ${response.status}`);
  }

  const verificationResult = (await response.json()) as VerificationResult;

  const finishedAt = Date.now();

  return {
    request,
    response: verificationResult,
    clientRoundTripTimeMs: finishedAt - startedAt,
  };
}