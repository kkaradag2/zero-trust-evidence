export type SoftwareAttestationRequest = {
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