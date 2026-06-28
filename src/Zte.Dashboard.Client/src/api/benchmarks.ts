export type BenchmarkRun = {
  id: string;
  code: string;
  type: string;
  status: string;
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

export type RuntimeBenchmarkMeasurement = {
  id: string;
  benchmarkRunId: string;
  policyK: string;
  policyLabel: string;
  runIndex: number;
  totalRequests: number;
  attestationPerformed: boolean;
  challengeFetchMs: number;
  deviceSigningMs: number;
  backendVerificationMs: number;
  freshProofCostMs: number;
  operationTotalMs: number;
  requestPayloadBytes: number;
  responsePayloadBytes: number;
  success: boolean;
  errorMessage: string | null;
  createdAtUtc: string;
  clientTimestampUtc: string;
};

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

export function listBenchmarkRuntimeMeasurements(
  id: string,
): Promise<RuntimeBenchmarkMeasurement[]> {
  return getJson<RuntimeBenchmarkMeasurement[]>(
    `/api/benchmarks/${encodeURIComponent(id)}/runtime-measurements`,
  );
}
