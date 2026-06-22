import {
  getAttestationChallenge,
  verifySoftwareAttestation,
} from './attestationApi';

import type { HardwareAttestationResult } from '../types/attestation';

export type SoftwareAttestationFlowResult = {
  verificationResult: HardwareAttestationResult;
};

export async function runSoftwareAttestationFlow(
  deviceId: string,
  benchmarkRunId?: string,
): Promise<SoftwareAttestationFlowResult> {
  const challenge = await getAttestationChallenge();

  const verificationResult = await verifySoftwareAttestation({
    benchmarkRunId,
    challengeId: challenge.challengeId,
    nonce: challenge.nonce,
    deviceId,
    platform: 'Android',
    osVersion: '16',
    appVersion: '1.0.0',
    deviceBrand: 'Google',
    deviceModel: 'Android Emulator',
    isEmulator: true,
    isRooted: false,
    clientTimestampUtc: new Date().toISOString(),
  });

  return {
    verificationResult,
  };
}