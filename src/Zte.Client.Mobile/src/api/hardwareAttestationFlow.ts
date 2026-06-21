import {
  enrollHardwareAttestation,
  getAttestationChallenge,
  verifyHardwareAttestation,
} from './attestationApi';

import {
  deleteHardwareKey,
  generateKeyWithAttestation,
  signChallenge,
} from '../native/hardwareAttestationNative';

import type { HardwareAttestationResult } from '../types/attestation';

const HARDWARE_KEY_ALIAS = 'zero_trust_hardware_attestation_key';

export type HardwareAttestationFlowResult = {
  enrollmentResult: HardwareAttestationResult;
  verificationResult: HardwareAttestationResult;
};

export async function runHardwareAttestationFlow(
  deviceId: string,
): Promise<HardwareAttestationFlowResult> {
  await deleteHardwareKey(HARDWARE_KEY_ALIAS);

  const enrollmentChallenge = await getAttestationChallenge();

  const keyMaterial = await generateKeyWithAttestation(
    HARDWARE_KEY_ALIAS,
    enrollmentChallenge.nonce,
  );

  const enrollmentResult = await enrollHardwareAttestation({
    challengeId: enrollmentChallenge.challengeId,
    nonce: enrollmentChallenge.nonce,
    deviceId,
    keyAlias: keyMaterial.alias,
    publicKeyBase64: keyMaterial.publicKeyBase64,
    certificateChainBase64: keyMaterial.certificateChainBase64,
    clientTimestampUtc: new Date().toISOString(),
  });

  const verificationChallenge = await getAttestationChallenge();

  const signature = await signChallenge(
    HARDWARE_KEY_ALIAS,
    verificationChallenge.nonce,
  );

  const verificationResult = await verifyHardwareAttestation({
    challengeId: verificationChallenge.challengeId,
    nonce: verificationChallenge.nonce,
    deviceId,
    keyAlias: signature.alias,
    signatureBase64: signature.signatureBase64,
    clientTimestampUtc: new Date().toISOString(),
  });

  return {
    enrollmentResult,
    verificationResult,
  };
}