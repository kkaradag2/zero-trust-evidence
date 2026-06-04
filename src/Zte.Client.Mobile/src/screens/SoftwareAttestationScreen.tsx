import React, {useState} from 'react';
import {
  ActivityIndicator,
  Button,
  SafeAreaView,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';
import {runSoftwareAttestation} from '../api/attestationApi';
import {SoftwareAttestationClientResult} from '../types/attestation';

export function SoftwareAttestationScreen() {
  const [result, setResult] = useState<SoftwareAttestationClientResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isRunning, setIsRunning] = useState(false);

  async function handleRunSoftwareAttestation() {
    setIsRunning(true);
    setError(null);

    try {
      const attestationResult = await runSoftwareAttestation();
      setResult(attestationResult);
    } catch (err) {
      setResult(null);
      setError(err instanceof Error ? err.message : 'Unknown error occurred.');
    } finally {
      setIsRunning(false);
    }
  }

  return (
    <SafeAreaView style={styles.safeArea}>
      <ScrollView contentContainerStyle={styles.container}>
        <Text style={styles.title}>Zero Trust Evidence</Text>
        <Text style={styles.subtitle}>Software / Context-Aware Attestation</Text>

        <View style={styles.card}>
          <Text style={styles.cardTitle}>Experiment Goal</Text>
          <Text style={styles.bodyText}>
            This screen sends a software/context-aware client posture payload to
            the .NET backend and measures the client-side round-trip time.
          </Text>
        </View>

        <View style={styles.buttonContainer}>
          <Button
            title={isRunning ? 'Running...' : 'Run Software Attestation'}
            onPress={handleRunSoftwareAttestation}
            disabled={isRunning}
          />
        </View>

        {isRunning && (
          <View style={styles.loadingContainer}>
            <ActivityIndicator />
            <Text style={styles.bodyText}>Sending verification request...</Text>
          </View>
        )}

        {error && (
          <View style={[styles.card, styles.errorCard]}>
            <Text style={styles.cardTitle}>Error</Text>
            <Text style={styles.errorText}>{error}</Text>
          </View>
        )}

        {result && (
          <>
            <View style={styles.card}>
              <Text style={styles.cardTitle}>Client Measurement</Text>
              <Text style={styles.metricText}>
                Round-trip time: {result.clientRoundTripTimeMs.toFixed(3)} ms
              </Text>
            </View>

            <View style={styles.card}>
              <Text style={styles.cardTitle}>Backend Verification Result</Text>
              <Text style={styles.metricText}>
                Accepted: {String(result.response.accepted)}
              </Text>
              <Text style={styles.metricText}>
                Attestation Type: {result.response.attestationType}
              </Text>
              <Text style={styles.metricText}>
                Risk Level: {result.response.riskLevel}
              </Text>
              <Text style={styles.metricText}>
                Backend Time: {result.response.verificationTimeMs.toFixed(4)} ms
              </Text>
              <Text style={styles.metricText}>
                Backend Time: {result.response.verificationTimeMicroseconds} μs
              </Text>
              <Text style={styles.metricText}>
                Message Size: {result.response.messageSizeBytes} bytes
              </Text>
              <Text style={styles.metricText}>
                Processing Steps: {result.response.processingStepCount}
              </Text>
            </View>

            <View style={styles.card}>
              <Text style={styles.cardTitle}>Request Payload</Text>
              <Text style={styles.codeText}>
                {JSON.stringify(result.request, null, 2)}
              </Text>
            </View>

            <View style={styles.card}>
              <Text style={styles.cardTitle}>Raw Response</Text>
              <Text style={styles.codeText}>
                {JSON.stringify(result.response, null, 2)}
              </Text>
            </View>
          </>
        )}
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  container: {
    padding: 20,
    gap: 16,
  },
  title: {
    fontSize: 28,
    fontWeight: '700',
    color: '#111827',
  },
  subtitle: {
    fontSize: 16,
    color: '#4b5563',
    marginBottom: 8,
  },
  card: {
    backgroundColor: '#ffffff',
    borderRadius: 12,
    padding: 16,
    borderWidth: 1,
    borderColor: '#e5e7eb',
  },
  errorCard: {
    borderColor: '#ef4444',
    backgroundColor: '#fef2f2',
  },
  cardTitle: {
    fontSize: 16,
    fontWeight: '700',
    color: '#111827',
    marginBottom: 8,
  },
  bodyText: {
    fontSize: 14,
    color: '#374151',
    lineHeight: 20,
  },
  metricText: {
    fontSize: 14,
    color: '#111827',
    marginBottom: 4,
  },
  errorText: {
    fontSize: 14,
    color: '#991b1b',
  },
  codeText: {
    fontSize: 12,
    color: '#111827',
    fontFamily: 'monospace',
  },
  buttonContainer: {
    marginVertical: 4,
  },
  loadingContainer: {
    alignItems: 'center',
    gap: 8,
  },
});