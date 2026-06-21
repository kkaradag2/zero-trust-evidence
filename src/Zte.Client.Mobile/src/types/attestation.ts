
export type AttestationChallenge = {
  challengeId: string;
  nonce: string;
  createdAtUtc: string;
  expiresAtUtc: string;
};

export type SoftwareAttestationRequest = {
   challengeId: string;
  nonce: string;
  deviceId: string;
  platform: string;
  osVersion: string;
  appVersion: string;
  deviceBrand: string;
  deviceModel: string;
  isEmulator: boolean;
  isRooted: boolean;
  clientTimestampUtc: string;
};

export type VerificationResult = {
  accepted: boolean;
  attestationType: string;
  riskLevel: string;
  processingStepCount: number;
  verificationTimeMs: number;
  verificationTimeMicroseconds: number;
  messageSizeBytes: number;
  reasons: string[];
};

export type SoftwareAttestationClientResult = {
  request: SoftwareAttestationRequest;
  response: VerificationResult;
  clientRoundTripTimeMs: number;
};

export type HardwareEnrollmentRequest = {
  challengeId: string;
  nonce: string;
  deviceId: string;
  keyAlias: string;
  publicKeyBase64: string;
  certificateChainBase64: string[];
  clientTimestampUtc: string;
};

export type HardwareVerificationRequest = {
  challengeId: string;
  nonce: string;
  deviceId: string;
  keyAlias: string;
  signatureBase64: string;
  clientTimestampUtc: string;
};

export type HardwareAttestationResult = {
  accepted: boolean;
  attestationType: string;
  riskLevel: string;
  processingStepCount: number;
  verificationTimeMs: number;
  verificationTimeMicroseconds: number;
  messageSizeBytes: number;
  reasons: string[];
};