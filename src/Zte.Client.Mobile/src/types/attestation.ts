export type HardwareEnrollmentRequest = {
  benchmarkRunId?: string;
  challengeId: string;
  nonce: string;
  deviceId: string;
  appInstanceId?: string;
  keyAlias: string;
  publicKeyBase64: string;
  attestationEvidence?: string;
  certificateChainBase64: string[];
  clientTimestampUtc: string;
};

export type HardwareVerificationRequest = {
  benchmarkRunId?: string;
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

export type HardwareEnrollmentResult = {
  accepted: boolean;
  enrolledDeviceId: string | null;
  riskLevel: string;
  verificationSteps: string[];
  reasons: string[];
  warnings: string[];
  hardwareSecurityLevel: string | null;
  attestationSecurityLevel: string | null;
  processingStepCount: number;
  verificationTimeMs: number;
  verificationTimeMicroseconds: number;
  messageSizeBytes: number;
};

export type BenchmarkType = 'Hardware';

export type BenchmarkStatus = 'Running' | 'Completed' | 'Failed';

export type BenchmarkRun = {
  id: string;
  code: string;
  type: BenchmarkType;
  status: BenchmarkStatus;
  iterationCount: number;
  hardwareVerificationIterationCount: number;
  hardwareEnrollmentCount: number;
  startedAtUtc: string;
  completedAtUtc: string | null;
  errorMessage: string | null;
  durationMs: number | null;
  success: boolean;
  mobileDevice: BenchmarkDeviceInfo | null;
  backendSystem: BenchmarkBackendSystemInfo | null;
};

export type CreateBenchmarkRunRequest = {
  type: BenchmarkType;
  iterationCount?: number;
  mobileDevice?: BenchmarkDeviceInfo;
};

export type BenchmarkDeviceInfo = {
  deviceId?: string;
  deviceName?: string;
  manufacturer?: string;
  brand?: string;
  model?: string;
  device?: string;
  product?: string;
  hardware?: string;
  androidVersion?: string;
  sdkInt?: number;
  supportedAbis?: string[];
  isEmulator?: boolean;
  cpuCoreCount?: number;
  totalMemoryBytes?: number;
};

export type BenchmarkBackendSystemInfo = {
  machineName: string;
  osDescription: string;
  processArchitecture: string;
  processorCount: number;
  totalAvailableMemoryBytes: number | null;
};

export type FailBenchmarkRunRequest = {
  errorMessage: string;
};
