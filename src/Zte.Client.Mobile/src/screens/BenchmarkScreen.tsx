import React, { useState } from 'react';
import {
  ActivityIndicator,
  Alert,
  Button,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';

import {
  completeBenchmarkRun,
  createBenchmarkRun,
  failBenchmarkRun,
  getAttestationChallenge,
  saveRuntimeMeasurements,
  verifyHardwareAttestation,
} from '../api/attestationApi';

import {enrollHardwareDevice} from '../api/hardwareAttestationFlow';
import { DEFAULT_BENCHMARK_ITERATION_COUNT } from '../config/benchmarkConfig';

import type { BenchmarkRun } from '../types/attestation';
import type { RuntimeMeasurementRequest } from '../types/attestation';

import {
  getDeviceIdentity,
  signChallenge,
  type DeviceIdentity,
} from '../native/hardwareAttestationNative';

type RefreshPolicyValue = 1 | 3 | 5 | 10 | 'session-only';

type RefreshPolicyOption = {
  label: string;
  value: RefreshPolicyValue;
};

type BenchmarkIterationMeasurement = {
  runIndex: number;
  policy: RefreshPolicyValue;
  totalRequests: number;
  attestationPerformed: boolean;
  challengeFetchMs: number | null;
  deviceSigningMs: number | null;
  backendVerificationMs: number | null;
  freshProofCostMs: number | null;
  operationTotalMs: number;
  success: boolean;
  errorMessage: string | null;
  timestamp: string;
};

const refreshPolicies: RefreshPolicyOption[] = [
  {label: 'Every request', value: 1},
  {label: 'Every 3 requests', value: 3},
  {label: 'Every 5 requests', value: 5},
  {label: 'Every 10 requests', value: 10},
  {label: 'Session only', value: 'session-only'},
];

const iterationOptions = [50, 100, 150, 200];
const HARDWARE_KEY_ALIAS = 'zero_trust_hardware_attestation_key';

export function BenchmarkScreen() {
  const [loading, setLoading] = useState(false);
  const [benchmarkRun, setBenchmarkRun] = useState<BenchmarkRun | null>(null);
  const [startedAtLocal, setStartedAtLocal] = useState<string | null>(null);
  const [completedAtLocal, setCompletedAtLocal] = useState<string | null>(null);
  const [selectedPolicy, setSelectedPolicy] =
    useState<RefreshPolicyValue>(1);
  const [iterationCount, setIterationCount] = useState(
    DEFAULT_BENCHMARK_ITERATION_COUNT,
  );
  const [deviceIdentity, setDeviceIdentity] = useState<DeviceIdentity | null>(null);
  const [measurements, setMeasurements] = useState<
    BenchmarkIterationMeasurement[]
  >([]);
  const [savedMeasurementCount, setSavedMeasurementCount] = useState<
    number | null
  >(null);
  const [runtimeMeasurementSaveError, setRuntimeMeasurementSaveError] =
    useState<string | null>(null);

  const selectedPolicyLabel = getPolicyLabel(selectedPolicy);
  const freshAttestationCount = calculateFreshAttestationCount(
    selectedPolicy,
    iterationCount,
  );
  const freshMeasurements = measurements.filter(
    (measurement) => measurement.attestationPerformed,
  );
  const averageChallengeFetchMs = averageMeasurement(
    freshMeasurements,
    'challengeFetchMs',
  );
  const averageDeviceSigningMs = averageMeasurement(
    freshMeasurements,
    'deviceSigningMs',
  );
  const averageBackendVerificationMs = averageMeasurement(
    freshMeasurements,
    'backendVerificationMs',
  );
  const averageFreshProofCostMs = averageMeasurement(
    freshMeasurements,
    'freshProofCostMs',
  );

  async function handleStartBenchmark() {
    let createdRun: BenchmarkRun | null = null;
    const nextMeasurements: BenchmarkIterationMeasurement[] = [];

    try {
      setLoading(true);
      setBenchmarkRun(null);
      setMeasurements([]);
      setSavedMeasurementCount(null);
      setRuntimeMeasurementSaveError(null);
      setStartedAtLocal(new Date().toISOString());
      setCompletedAtLocal(null);

      const identity = await getDeviceIdentity();

      setDeviceIdentity(identity);

      createdRun = await createBenchmarkRun(
        'Hardware',
        iterationCount,
        identity,
      );
      setBenchmarkRun(createdRun);

      await enrollHardwareDevice(identity.deviceId, createdRun.id);

      for (let index = 0; index < iterationCount; index += 1) {
        let measurement: BenchmarkIterationMeasurement;

        if (shouldPerformFreshAttestation(index, selectedPolicy)) {
          measurement = await runMeasuredFreshAttestation({
            runIndex: index + 1,
            policy: selectedPolicy,
            totalRequests: iterationCount,
            deviceId: identity.deviceId,
            benchmarkRunId: createdRun.id,
          });
        } else {
          measurement = createReuseMeasurement({
            runIndex: index + 1,
            policy: selectedPolicy,
            totalRequests: iterationCount,
          });
        }

        nextMeasurements.push(measurement);
        setMeasurements([...nextMeasurements]);

        if (!measurement.success) {
          throw new Error(
            measurement.errorMessage ?? 'Hardware attestation failed.',
          );
        }
      }

      const completedRun = await completeBenchmarkRun(createdRun.id);

      setBenchmarkRun(completedRun);
      setCompletedAtLocal(new Date().toISOString());

      try {
        if (nextMeasurements.length !== iterationCount) {
          throw new Error(
            `Expected ${iterationCount} runtime measurements, got ${nextMeasurements.length}.`,
          );
        }

        const saveResult = await saveRuntimeMeasurements(
          createdRun.id,
          toRuntimeMeasurementRequests(nextMeasurements),
        );
        setSavedMeasurementCount(saveResult.savedCount);
      } catch (saveError) {
        const saveMessage =
          saveError instanceof Error
            ? saveError.message
            : 'Runtime measurement save failed.';
        setRuntimeMeasurementSaveError(saveMessage);
      }
    } catch (error) {
      const message =
        error instanceof Error ? error.message : 'Benchmark failed.';

      if (createdRun) {
        try {
          const failedRun = await failBenchmarkRun(createdRun.id, message);
          setBenchmarkRun(failedRun);
        } catch {
          // Ignore failure reporting errors on the mobile side.
        }
      }

      setCompletedAtLocal(new Date().toISOString());
      Alert.alert('Benchmark Error', message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <ScrollView contentContainerStyle={styles.container}>
      <Text style={styles.title}>Runtime Hardware Attestation Benchmark</Text>

      <Text style={styles.description}>
        Measures the runtime cost of renewing Android hardware-backed
        attestation evidence under different refresh policies.
      </Text>

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Policy</Text>
        <View style={styles.optionGroup}>
          {refreshPolicies.map((policy) => (
            <Pressable
              key={policy.label}
              style={[
                styles.optionButton,
                selectedPolicy === policy.value && styles.optionButtonSelected,
              ]}
              onPress={() => setSelectedPolicy(policy.value)}
              disabled={loading}
            >
              <Text
                style={[
                  styles.optionText,
                  selectedPolicy === policy.value && styles.optionTextSelected,
                ]}
              >
                {policy.label}
              </Text>
            </Pressable>
          ))}
        </View>
      </View>

      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Iterations</Text>
        <View style={styles.optionGroup}>
          {iterationOptions.map((option) => (
            <Pressable
              key={option}
              style={[
                styles.iterationButton,
                iterationCount === option && styles.optionButtonSelected,
              ]}
              onPress={() => setIterationCount(option)}
              disabled={loading}
            >
              <Text
                style={[
                  styles.optionText,
                  iterationCount === option && styles.optionTextSelected,
                ]}
              >
                {option}
              </Text>
            </Pressable>
          ))}
        </View>
      </View>

      <Button
        title="Start Benchmark"
        onPress={handleStartBenchmark}
        disabled={loading}
      />

      {loading ? (
        <View style={styles.loadingContainer}>
          <ActivityIndicator />
          <Text style={styles.loadingText}>Benchmark is running...</Text>
        </View>
      ) : null}

      <View style={styles.card}>
        <Text style={styles.cardTitle}>Summary</Text>
        <SummaryRow label="Status" value={benchmarkRun?.status ?? '-'} />
        <SummaryRow label="Policy" value={selectedPolicyLabel} />
        <SummaryRow label="Total Requests" value={String(iterationCount)} />
        <SummaryRow
          label="Fresh Attestations"
          value={String(freshAttestationCount)}
        />
        <SummaryRow
          label="Avg Challenge Fetch"
          value={formatMetricMs(averageChallengeFetchMs)}
        />
        <SummaryRow
          label="Avg Device Signing"
          value={formatMetricMs(averageDeviceSigningMs)}
        />
        <SummaryRow
          label="Avg Backend Verification"
          value={formatMetricMs(averageBackendVerificationMs)}
        />
        <SummaryRow
          label="Avg Fresh Proof Cost"
          value={formatMetricMs(averageFreshProofCostMs)}
        />
        <SummaryRow
          label="Success"
          value={benchmarkRun ? String(benchmarkRun.success) : '-'}
        />
        <SummaryRow
          label="Saved Measurements"
          value={formatSavedMeasurementCount(
            savedMeasurementCount,
            runtimeMeasurementSaveError,
          )}
        />
        <SummaryRow
          label="Duration"
          value={formatDuration(benchmarkRun?.durationMs)}
        />
        <SummaryRow label="Error" value={benchmarkRun?.errorMessage ?? '-'} />
        {runtimeMeasurementSaveError ? (
          <SummaryRow
            label="Measurement Save Error"
            value={`Runtime measurement save failed: ${runtimeMeasurementSaveError}`}
          />
        ) : null}
      </View>

      <View style={styles.card}>
        <Text style={styles.cardTitle}>Details</Text>
        <ScrollView style={styles.detailsScroll} nestedScrollEnabled>
          <SummaryRow label="Benchmark ID" value={benchmarkRun?.code ?? '-'} />
          <SummaryRow
            label="Started At"
            value={formatDateTime(benchmarkRun?.startedAtUtc ?? startedAtLocal)}
          />
          <SummaryRow
            label="Completed At"
            value={formatDateTime(
              benchmarkRun?.completedAtUtc ?? completedAtLocal,
            )}
          />
          <SummaryRow label="Device" value={deviceIdentity?.deviceName ?? '-'} />
          <SummaryRow
            label="Device ID"
            value={deviceIdentity?.deviceId ?? '-'}
          />
          <SummaryRow
            label="Android"
            value={deviceIdentity?.androidVersion ?? '-'}
          />
          <SummaryRow
            label="SDK"
            value={
              deviceIdentity?.sdkInt === undefined
                ? '-'
                : String(deviceIdentity.sdkInt)
            }
          />
          <SummaryRow
            label="CPU Cores"
            value={
              deviceIdentity?.cpuCoreCount === undefined
                ? '-'
                : String(deviceIdentity.cpuCoreCount)
            }
          />
          <SummaryRow
            label="RAM"
            value={formatBytes(deviceIdentity?.totalMemoryBytes)}
          />
          <SummaryRow
            label="ABIs"
            value={deviceIdentity?.supportedAbis.join(', ') ?? '-'}
          />
          <SummaryRow
            label="Measured Iterations"
            value={String(measurements.length)}
          />
        </ScrollView>
      </View>

      <Text style={styles.note}>
        Open the backend dashboard or API to inspect benchmark measurements.
      </Text>
    </ScrollView>
  );
}

async function runMeasuredFreshAttestation({
  runIndex,
  policy,
  totalRequests,
  deviceId,
  benchmarkRunId,
}: {
  runIndex: number;
  policy: RefreshPolicyValue;
  totalRequests: number;
  deviceId: string;
  benchmarkRunId: string;
}): Promise<BenchmarkIterationMeasurement> {
  const operationStartedAt = Date.now();
  let challengeFetchMs: number | null = null;
  let deviceSigningMs: number | null = null;
  let backendVerificationMs: number | null = null;

  try {
    const challengeStartedAt = Date.now();
    const verificationChallenge = await getAttestationChallenge({
      deviceId,
      appInstanceId: deviceId,
      purpose: 'hardware-verification',
    });
    challengeFetchMs = Date.now() - challengeStartedAt;

    const signingStartedAt = Date.now();
    const signature = await signChallenge(
      HARDWARE_KEY_ALIAS,
      verificationChallenge.nonce,
    );
    deviceSigningMs = Date.now() - signingStartedAt;

    const backendVerificationStartedAt = Date.now();
    await verifyHardwareAttestation({
      benchmarkRunId,
      challengeId: verificationChallenge.challengeId,
      nonce: verificationChallenge.nonce,
      deviceId,
      keyAlias: signature.alias,
      signatureBase64: signature.signatureBase64,
      clientTimestampUtc: new Date().toISOString(),
    });
    backendVerificationMs = Date.now() - backendVerificationStartedAt;

    return {
      runIndex,
      policy,
      totalRequests,
      attestationPerformed: true,
      challengeFetchMs,
      deviceSigningMs,
      backendVerificationMs,
      freshProofCostMs:
        challengeFetchMs + deviceSigningMs + backendVerificationMs,
      operationTotalMs: Date.now() - operationStartedAt,
      success: true,
      errorMessage: null,
      timestamp: new Date().toISOString(),
    };
  } catch (error) {
    return {
      runIndex,
      policy,
      totalRequests,
      attestationPerformed: true,
      challengeFetchMs,
      deviceSigningMs,
      backendVerificationMs,
      freshProofCostMs: null,
      operationTotalMs: Date.now() - operationStartedAt,
      success: false,
      errorMessage:
        error instanceof Error ? error.message : 'Hardware attestation failed.',
      timestamp: new Date().toISOString(),
    };
  }
}

function createReuseMeasurement({
  runIndex,
  policy,
  totalRequests,
}: {
  runIndex: number;
  policy: RefreshPolicyValue;
  totalRequests: number;
}): BenchmarkIterationMeasurement {
  return {
    runIndex,
    policy,
    totalRequests,
    attestationPerformed: false,
    challengeFetchMs: 0,
    deviceSigningMs: 0,
    backendVerificationMs: 0,
    freshProofCostMs: 0,
    operationTotalMs: 0,
    success: true,
    errorMessage: null,
    timestamp: new Date().toISOString(),
  };
}

function toRuntimeMeasurementRequests(
  measurements: BenchmarkIterationMeasurement[],
): RuntimeMeasurementRequest[] {
  return measurements.map((measurement) => ({
    policyK: String(measurement.policy),
    policyLabel: getPolicyLabel(measurement.policy),
    runIndex: measurement.runIndex,
    totalRequests: measurement.totalRequests,
    attestationPerformed: measurement.attestationPerformed,
    challengeFetchMs: measurement.challengeFetchMs ?? 0,
    deviceSigningMs: measurement.deviceSigningMs ?? 0,
    backendVerificationMs: measurement.backendVerificationMs ?? 0,
    freshProofCostMs:
      measurement.freshProofCostMs ??
      (measurement.challengeFetchMs ?? 0) +
        (measurement.deviceSigningMs ?? 0) +
        (measurement.backendVerificationMs ?? 0),
    operationTotalMs: measurement.operationTotalMs,
    requestPayloadBytes: 0,
    responsePayloadBytes: 0,
    success: measurement.success,
    errorMessage: measurement.errorMessage ?? '',
    timestamp: measurement.timestamp,
  }));
}

function SummaryRow({label, value}: {label: string; value: string}) {
  return (
    <View style={styles.summaryRow}>
      <Text style={styles.summaryLabel}>{label}</Text>
      <Text style={styles.summaryValue}>{value}</Text>
    </View>
  );
}

function shouldPerformFreshAttestation(
  iterationIndex: number,
  policy: RefreshPolicyValue,
): boolean {
  if (policy === 'session-only') {
    return iterationIndex === 0;
  }

  return iterationIndex % policy === 0;
}

function calculateFreshAttestationCount(
  policy: RefreshPolicyValue,
  iterations: number,
): number {
  if (iterations <= 0) {
    return 0;
  }

  if (policy === 'session-only') {
    return 1;
  }

  return Math.ceil(iterations / policy);
}

function getPolicyLabel(policy: RefreshPolicyValue): string {
  return refreshPolicies.find((option) => option.value === policy)?.label ?? '-';
}

function averageMeasurement(
  measurements: BenchmarkIterationMeasurement[],
  key:
    | 'challengeFetchMs'
    | 'deviceSigningMs'
    | 'backendVerificationMs'
    | 'freshProofCostMs',
): number | null {
  const values = measurements
    .map((measurement) => measurement[key])
    .filter((value): value is number => value !== null);

  if (values.length === 0) {
    return null;
  }

  return values.reduce((total, value) => total + value, 0) / values.length;
}

function formatDateTime(value: string | null | undefined): string {
  if (!value) {
    return '-';
  }

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'medium',
  }).format(new Date(value));
}

function formatDuration(value: number | null | undefined): string {
  if (value === null || value === undefined) {
    return '-';
  }

  return `${Math.round(value)} ms`;
}

function formatMetricMs(value: number | null): string {
  if (value === null) {
    return '-';
  }

  return `${Math.round(value)} ms`;
}

function formatSavedMeasurementCount(
  savedMeasurementCount: number | null,
  runtimeMeasurementSaveError: string | null,
): string {
  if (runtimeMeasurementSaveError) {
    return 'failed';
  }

  if (savedMeasurementCount === null) {
    return '-';
  }

  return String(savedMeasurementCount);
}

function formatBytes(value: number | null | undefined): string {
  if (!value) {
    return '-';
  }

  const gibibytes = value / 1024 / 1024 / 1024;

  return `${gibibytes.toFixed(2)} GB`;
}

const styles = StyleSheet.create({
  container: {
    padding: 20,
    gap: 16,
  },
  title: {
    fontSize: 24,
    fontWeight: '700',
  },
  description: {
    fontSize: 14,
    lineHeight: 20,
  },
  section: {
    gap: 10,
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: '700',
  },
  optionGroup: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
  },
  optionButton: {
    borderWidth: 1,
    borderColor: '#9ca3af',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 9,
  },
  iterationButton: {
    minWidth: 64,
    alignItems: 'center',
    borderWidth: 1,
    borderColor: '#9ca3af',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 9,
  },
  optionButtonSelected: {
    backgroundColor: '#111827',
    borderColor: '#111827',
  },
  optionText: {
    color: '#111827',
    fontSize: 14,
    fontWeight: '600',
  },
  optionTextSelected: {
    color: '#ffffff',
  },
  loadingContainer: {
    marginTop: 16,
    gap: 8,
  },
  loadingText: {
    fontSize: 14,
  },
  card: {
    marginTop: 16,
    padding: 16,
    borderWidth: 1,
    borderRadius: 8,
    gap: 6,
  },
  cardTitle: {
    fontSize: 18,
    fontWeight: '700',
    marginBottom: 8,
  },
  summaryRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    gap: 16,
  },
  summaryLabel: {
    flex: 1,
    color: '#4b5563',
    fontSize: 14,
  },
  summaryValue: {
    flex: 1,
    color: '#111827',
    fontSize: 14,
    fontWeight: '600',
    textAlign: 'right',
  },
  detailsScroll: {
    maxHeight: 220,
  },
  note: {
    fontSize: 13,
    lineHeight: 18,
  },
});
