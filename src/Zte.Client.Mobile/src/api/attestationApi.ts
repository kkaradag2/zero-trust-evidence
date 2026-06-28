import {
  HardwareEnrollmentRequest,
  HardwareVerificationRequest,
  HardwareAttestationResult,
  HardwareEnrollmentResult,
  BenchmarkRun,
  BenchmarkType,
  BenchmarkDeviceInfo,
  FailBenchmarkRunRequest,
  RuntimeMeasurementRequest,
  SaveRuntimeMeasurementsResponse,
} from '../types/attestation';

const API_BASE_URL = 'http://10.0.2.2:5145';

export async function enrollHardwareAttestation(
  request: HardwareEnrollmentRequest,
): Promise<HardwareEnrollmentResult> {
  const response = await fetch(
    `${API_BASE_URL}/api/hardware-attestation/enroll`,
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    },
  );

  if (!response.ok) {
    throw new Error(`Hardware enrollment failed with status ${response.status}`);
  }

  return response.json();
}

export async function verifyHardwareAttestation(
  request: HardwareVerificationRequest,
): Promise<HardwareAttestationResult> {
  const response = await fetch(
    `${API_BASE_URL}/api/attestation/hardware/verify`,
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    },
  );

  if (!response.ok) {
    throw new Error(`Hardware verification failed with status ${response.status}`);
  }

  return response.json();
}

export type AttestationChallengeResponse = {
  challengeId: string;
  nonce: string;
  expiresAtUtc: string;
};

export async function getAttestationChallenge(request?: {
  deviceId?: string;
  appInstanceId?: string;
  userSessionId?: string;
  purpose?: string;
}): Promise<AttestationChallengeResponse> {
  const response = request
    ? await fetch(`${API_BASE_URL}/api/attestation/challenge`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(request),
      })
    : await fetch(`${API_BASE_URL}/api/attestation/challenge`);

  if (!response.ok) {
    throw new Error(
      `Attestation challenge request failed with status ${response.status}`,
    );
  }

  return response.json();
}

export async function createBenchmarkRun(
  type: BenchmarkType = 'Hardware',
  iterationCount?: number,
  mobileDevice?: BenchmarkDeviceInfo,
): Promise<BenchmarkRun> {
  const request = {
    type,
    ...(iterationCount ? { iterationCount } : {}),
    ...(mobileDevice ? { mobileDevice } : {}),
  };

  const response = await fetch(`${API_BASE_URL}/api/benchmarks`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    throw new Error(
      `Create benchmark run failed with status ${response.status}`,
    );
  }

  return response.json();
}

export async function completeBenchmarkRun(
  benchmarkRunId: string,
): Promise<BenchmarkRun> {
  const response = await fetch(
    `${API_BASE_URL}/api/benchmarks/${benchmarkRunId}/complete`,
    {
      method: 'POST',
    },
  );

  if (!response.ok) {
    throw new Error(
      `Complete benchmark run failed with status ${response.status}`,
    );
  }

  return response.json();
}

export async function failBenchmarkRun(
  benchmarkRunId: string,
  errorMessage: string,
): Promise<BenchmarkRun> {
  const request: FailBenchmarkRunRequest = {
    errorMessage,
  };

  const response = await fetch(
    `${API_BASE_URL}/api/benchmarks/${benchmarkRunId}/fail`,
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    },
  );

  if (!response.ok) {
    throw new Error(`Fail benchmark run failed with status ${response.status}`);
  }

  return response.json();
}

export async function saveRuntimeMeasurements(
  benchmarkRunId: string,
  measurements: RuntimeMeasurementRequest[],
): Promise<SaveRuntimeMeasurementsResponse> {
  const response = await fetch(
    `${API_BASE_URL}/api/benchmarks/${benchmarkRunId}/runtime-measurements`,
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(measurements),
    },
  );

  if (!response.ok) {
    throw new Error(
      `Runtime measurement save failed with status ${response.status}`,
    );
  }

  return response.json();
}
