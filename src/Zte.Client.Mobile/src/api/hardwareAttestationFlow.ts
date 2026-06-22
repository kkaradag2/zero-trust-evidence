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

import type {
  HardwareAttestationResult,
  HardwareEnrollmentResult,
} from '../types/attestation';

const HARDWARE_KEY_ALIAS = 'zero_trust_hardware_attestation_key';

export type HardwareAttestationFlowResult = {
  enrollmentResult: HardwareEnrollmentResult;
  verificationResult: HardwareAttestationResult;
};

export async function enrollHardwareDevice(
  deviceId: string,
  benchmarkRunId?: string,
): Promise<HardwareEnrollmentResult> {
  await deleteHardwareKey(HARDWARE_KEY_ALIAS);

  const enrollmentChallenge = await getAttestationChallenge({
    deviceId,
    appInstanceId: deviceId,
    purpose: 'hardware-enrollment',
  });

  const keyMaterial = await generateKeyWithAttestation(
    HARDWARE_KEY_ALIAS,
    enrollmentChallenge.nonce,
  );

  const enrollmentResult = await enrollHardwareAttestation({
    benchmarkRunId,
    challengeId: enrollmentChallenge.challengeId,
    nonce: enrollmentChallenge.nonce,
    deviceId,
    appInstanceId: deviceId,
    keyAlias: keyMaterial.alias,
    publicKeyBase64: keyMaterial.publicKeyBase64,
    attestationEvidence: keyMaterial.hardwareSecurityLevel,
    certificateChainBase64: keyMaterial.certificateChainBase64,
    clientTimestampUtc: new Date().toISOString(),
  });

  return enrollmentResult;
}

export async function verifyHardwareDevice(
  deviceId: string,
  benchmarkRunId?: string,
): Promise<HardwareAttestationResult> {
  const verificationChallenge = await getAttestationChallenge({
    deviceId,
    appInstanceId: deviceId,
    purpose: 'hardware-verification',
  });

  const signature = await signChallenge(
    HARDWARE_KEY_ALIAS,
    verificationChallenge.nonce,
  );

  const verificationResult = await verifyHardwareAttestation({
    benchmarkRunId,
    challengeId: verificationChallenge.challengeId,
    nonce: verificationChallenge.nonce,
    deviceId,
    keyAlias: signature.alias,
    signatureBase64: signature.signatureBase64,
    clientTimestampUtc: new Date().toISOString(),
  });

  return verificationResult;
}

export async function runHardwareAttestationFlow(
  deviceId: string,
  benchmarkRunId?: string,
): Promise<HardwareAttestationFlowResult> {
  const enrollmentResult = await enrollHardwareDevice(deviceId, benchmarkRunId);
  const verificationResult = await verifyHardwareDevice(deviceId, benchmarkRunId);

  return {
    enrollmentResult,
    verificationResult,
  };
}
