import React, { useState } from 'react';
import {
  ActivityIndicator,
  Alert,
  Button,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';

import {
  completeBenchmarkRun,
  createBenchmarkRun,
  failBenchmarkRun,
} from '../api/attestationApi';

import {
  enrollHardwareDevice,
  verifyHardwareDevice,
} from '../api/hardwareAttestationFlow';
import { runSoftwareAttestationFlow } from '../api/softwareAttestationFlow';
import { DEFAULT_BENCHMARK_ITERATION_COUNT } from '../config/benchmarkConfig';

import type { BenchmarkRun } from '../types/attestation';

import {
  getDeviceIdentity,
  type DeviceIdentity,
} from '../native/hardwareAttestationNative';

export function BenchmarkScreen() {
  const [loading, setLoading] = useState(false);
  const [benchmarkRun, setBenchmarkRun] = useState<BenchmarkRun | null>(null);
  const [startedAtLocal, setStartedAtLocal] = useState<string | null>(null);
  const [completedAtLocal, setCompletedAtLocal] = useState<string | null>(null);

  const [deviceIdentity, setDeviceIdentity] = useState<DeviceIdentity | null>(null);

  async function handleStartBenchmark() {
    let createdRun: BenchmarkRun | null = null;
    const iterationCount = DEFAULT_BENCHMARK_ITERATION_COUNT;

    try {
      setLoading(true);
      setBenchmarkRun(null);
      setStartedAtLocal(new Date().toISOString());
      setCompletedAtLocal(null);

      const identity = await getDeviceIdentity();

      setDeviceIdentity(identity);

      createdRun = await createBenchmarkRun(
        'Comparative',
        iterationCount,
        identity,
      );
      setBenchmarkRun(createdRun);

      for (let index = 0; index < iterationCount; index += 1) {
        await runSoftwareAttestationFlow(identity.deviceId, createdRun.id);
      }

      await enrollHardwareDevice(identity.deviceId, createdRun.id);

      for (let index = 0; index < iterationCount; index += 1) {
        await verifyHardwareDevice(identity.deviceId, createdRun.id);
      }

      const completedRun = await completeBenchmarkRun(createdRun.id);

      setBenchmarkRun(completedRun);
      setCompletedAtLocal(new Date().toISOString());
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
      <Text style={styles.title}>Benchmark</Text>

      <Text style={styles.description}>
        Starts a comparative benchmark run. The mobile client triggers software
        and hardware attestation flows. Detailed comparison and raw measurements
        are stored on the backend.
      </Text>

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
        <Text style={styles.cardTitle}>Benchmark Status</Text>

        <Text>Benchmark ID: {benchmarkRun?.code ?? '-'}</Text>
        <Text>Internal ID: {benchmarkRun?.id ?? '-'}</Text>
        <Text>Status: {benchmarkRun?.status ?? '-'}</Text>
        <Text>Type: {benchmarkRun?.type ?? '-'}</Text>
        <Text>Success: {benchmarkRun ? String(benchmarkRun.success) : '-'}</Text>
        <Text>
          Started At:{' '}
          {formatDateTime(benchmarkRun?.startedAtUtc ?? startedAtLocal)}
        </Text>
        <Text>
          Completed At:{' '}
          {formatDateTime(benchmarkRun?.completedAtUtc ?? completedAtLocal)}
        </Text>
        <Text>Duration: {benchmarkRun?.durationMs ?? '-'} ms</Text>
        <Text>Error: {benchmarkRun?.errorMessage ?? '-'}</Text>
        <Text>Device: {deviceIdentity?.deviceName ?? '-'}</Text>
        <Text>Device ID: {deviceIdentity?.deviceId ?? '-'}</Text>
        <Text>Android: {deviceIdentity?.androidVersion ?? '-'}</Text>
        <Text>SDK: {deviceIdentity?.sdkInt ?? '-'}</Text>
        <Text>CPU Cores: {deviceIdentity?.cpuCoreCount ?? '-'}</Text>
        <Text>
          RAM: {formatBytes(deviceIdentity?.totalMemoryBytes)}
        </Text>
        <Text>
          ABIs: {deviceIdentity?.supportedAbis.join(', ') ?? '-'}
        </Text>
        <Text>Iteration Count: {DEFAULT_BENCHMARK_ITERATION_COUNT}</Text>
        <Text>
          Expected Measurements: SoftwareVerification:{' '}
          {DEFAULT_BENCHMARK_ITERATION_COUNT}, HardwareEnrollment: 1,
          HardwareVerification: {DEFAULT_BENCHMARK_ITERATION_COUNT}
        </Text>
      </View>

      <Text style={styles.note}>
        Open the backend dashboard or API to inspect benchmark measurements.
      </Text>
    </ScrollView>
  );
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
  note: {
    fontSize: 13,
    lineHeight: 18,
  },
});
