export type BenchmarkRun = {
  id: string;
  code: string;
  type: string;
  status: string;
  iterationCount: number;
  softwareIterationCount: number;
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

export type VerificationMeasurement = {
  id: string;
  benchmarkRunId: string | null;
  phase: MeasurementPhase | null;
  attestationType: string;
  accepted: boolean;
  riskLevel: string;
  verificationTimeMs: number;
  verificationTimeMicroseconds: number;
  messageSizeBytes: number;
  processingStepCount: number;
  createdAtUtc: string;
};

export type MeasurementPhase =
  | 'SoftwareVerification'
  | 'HardwareEnrollment'
  | 'HardwareVerification'
  | string;

async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(path, {
    headers: {
      Accept: 'application/json',
    },
  });

  if (!response.ok) {
    throw new Error(`Request failed with ${response.status}`);
  }

  return response.json() as Promise<T>;
}

export function listBenchmarks(): Promise<BenchmarkRun[]> {
  return getJson<BenchmarkRun[]>('/api/benchmarks');
}

export function getBenchmark(id: string): Promise<BenchmarkRun> {
  return getJson<BenchmarkRun>(`/api/benchmarks/${encodeURIComponent(id)}`);
}

export function listBenchmarkMeasurements(
  id: string,
): Promise<VerificationMeasurement[]> {
  return getJson<VerificationMeasurement[]>(
    `/api/benchmarks/${encodeURIComponent(id)}/measurements`,
  );
}
