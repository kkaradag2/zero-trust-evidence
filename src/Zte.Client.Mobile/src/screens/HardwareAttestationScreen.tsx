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

import { runHardwareAttestationFlow } from '../api/hardwareAttestationFlow';
import type { HardwareAttestationFlowResult } from '../api/hardwareAttestationFlow';

const DEMO_DEVICE_ID = 'demo-android-device-001';

export function HardwareAttestationScreen() {
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<HardwareAttestationFlowResult | null>(null);

  async function handleRunHardwareAttestation() {
    try {
      setLoading(true);
      setResult(null);

      const flowResult = await runHardwareAttestationFlow(DEMO_DEVICE_ID);

      setResult(flowResult);
    } catch (error) {
      const message =
        error instanceof Error ? error.message : 'Hardware attestation failed.';

      Alert.alert('Hardware Attestation Error', message);
    } finally {
      setLoading(false);
    }
  }

  const enrollment = result?.enrollmentResult;
  const verification = result?.verificationResult;

  return (
    <ScrollView contentContainerStyle={styles.container}>
      <Text style={styles.title}>Hardware Attestation</Text>

      <Text style={styles.description}>
        This flow generates a hardware-backed Android Keystore key, enrolls the
        public key on the backend, signs a fresh challenge, and verifies the
        signature with the registered public key.
      </Text>

      <Button
        title="Run Hardware Attestation"
        onPress={handleRunHardwareAttestation}
        disabled={loading}
      />

      {loading ? (
        <View style={styles.loadingContainer}>
          <ActivityIndicator />
          <Text style={styles.loadingText}>Running hardware attestation...</Text>
        </View>
      ) : null}

      {enrollment ? (
        <View style={styles.card}>
          <Text style={styles.cardTitle}>Enrollment Result</Text>
          <Text>Accepted: {String(enrollment.accepted)}</Text>
          <Text>Risk Level: {enrollment.riskLevel}</Text>
          <Text>Attestation Type: {enrollment.attestationType}</Text>
          <Text>Verification Time: {enrollment.verificationTimeMs} ms</Text>
          <Text>Message Size: {enrollment.messageSizeBytes} bytes</Text>
          <Text>Processing Steps: {enrollment.processingStepCount}</Text>
          <Text>Reasons: {enrollment.reasons.join(', ') || '-'}</Text>
        </View>
      ) : null}

      {verification ? (
        <View style={styles.card}>
          <Text style={styles.cardTitle}>Verification Result</Text>
          <Text>Accepted: {String(verification.accepted)}</Text>
          <Text>Risk Level: {verification.riskLevel}</Text>
          <Text>Attestation Type: {verification.attestationType}</Text>
          <Text>Verification Time: {verification.verificationTimeMs} ms</Text>
          <Text>Message Size: {verification.messageSizeBytes} bytes</Text>
          <Text>Processing Steps: {verification.processingStepCount}</Text>
          <Text>Reasons: {verification.reasons.join(', ') || '-'}</Text>
        </View>
      ) : null}
    </ScrollView>
  );
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
});