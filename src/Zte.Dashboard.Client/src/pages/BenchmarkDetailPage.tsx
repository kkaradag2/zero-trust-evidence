import { ArrowLeft, Download, RefreshCw } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';

import {
  getBenchmark,
  listBenchmarkRuntimeMeasurements,
  type BenchmarkRun,
  type RuntimeBenchmarkMeasurement,
} from '../api/benchmarks';
import { EmptyState } from '../components/EmptyState';
import { ErrorState } from '../components/ErrorState';
import { LoadingState } from '../components/LoadingState';
import { MetricCard } from '../components/MetricCard';
import {
  formatBytes,
  formatDateTime,
  formatDuration,
  formatNumber,
} from '../format';
import { benchmarkListPath } from '../routes';

type BenchmarkDetailPageProps = {
  benchmarkId: string;
  onNavigate: (path: string) => void;
};

type RuntimeSummary = {
  totalRequests: number | null;
  policyLabel: string;
  freshAttestationCount: number;
  savedRuntimeMeasurementCount: number;
  averageChallengeFetchMs: number | null;
  averageDeviceSigningMs: number | null;
  averageBackendVerificationMs: number | null;
  averageFreshProofCostMs: number | null;
};

export function BenchmarkDetailPage({
  benchmarkId,
  onNavigate,
}: BenchmarkDetailPageProps) {
  const [benchmark, setBenchmark] = useState<BenchmarkRun | null>(null);
  const [runtimeMeasurements, setRuntimeMeasurements] = useState<
    RuntimeBenchmarkMeasurement[]
  >([]);
  const [isLoading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadDetail = async () => {
    setError(null);
    setLoading(true);

    try {
      const [benchmarkData, runtimeMeasurementData] = await Promise.all([
        getBenchmark(benchmarkId),
        listBenchmarkRuntimeMeasurements(benchmarkId),
      ]);

      setBenchmark(benchmarkData);
      setRuntimeMeasurements(
        [...runtimeMeasurementData].sort(
          (left, right) => left.runIndex - right.runIndex,
        ),
      );
    } catch (ex) {
      setError(ex instanceof Error ? ex.message : 'Unknown request error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadDetail();
  }, [benchmarkId]);

  const runtimeSummary = useMemo(
    () => summarizeRuntimeMeasurements(benchmark, runtimeMeasurements),
    [benchmark, runtimeMeasurements],
  );

  if (isLoading) {
    return <LoadingState />;
  }

  if (error || !benchmark) {
    return <ErrorState message={error ?? 'Benchmark was not found.'} />;
  }

  return (
    <section className="page-stack">
      <div className="toolbar">
        <button
          type="button"
          className="icon-button"
          onClick={() => onNavigate(benchmarkListPath())}
        >
          <ArrowLeft size={16} aria-hidden="true" />
          Back
        </button>
        <button type="button" className="icon-button" onClick={loadDetail}>
          <RefreshCw size={16} aria-hidden="true" />
          Refresh
        </button>
      </div>

      <div className="metric-grid">
        <MetricCard
          label="Total Requests"
          value={formatNumber(runtimeSummary.totalRequests)}
        />
        <MetricCard label="Policy" value={runtimeSummary.policyLabel} />
        <MetricCard
          label="Fresh Attestations"
          value={formatNumber(runtimeSummary.freshAttestationCount)}
        />
        <MetricCard
          label="Saved Measurements"
          value={formatNumber(runtimeSummary.savedRuntimeMeasurementCount)}
        />
        <MetricCard
          label="Avg Challenge Fetch"
          value={formatMetricMs(runtimeSummary.averageChallengeFetchMs)}
        />
        <MetricCard
          label="Avg Device Signing"
          value={formatMetricMs(runtimeSummary.averageDeviceSigningMs)}
        />
        <MetricCard
          label="Avg Backend Verification"
          value={formatMetricMs(runtimeSummary.averageBackendVerificationMs)}
        />
        <MetricCard
          label="Avg Fresh Proof Cost"
          value={formatMetricMs(runtimeSummary.averageFreshProofCostMs)}
        />
        <MetricCard label="Duration" value={formatDuration(benchmark.durationMs)} />
        <MetricCard label="Success" value={String(benchmark.success)} />
      </div>

      <section className="table-section">
        <div className="section-heading">
          <div>
            <h2>Runtime Measurements</h2>
            <p>
              Per-request runtime measurements reported by the mobile benchmark
              runner.
            </p>
          </div>
          <button
            type="button"
            className="icon-button"
            onClick={() =>
              downloadRuntimeMeasurementsCsv(benchmark, runtimeMeasurements)
            }
            disabled={runtimeMeasurements.length === 0}
            title={
              runtimeMeasurements.length === 0
                ? 'No runtime measurements available for export.'
                : undefined
            }
          >
            <Download size={16} aria-hidden="true" />
            Download CSV
          </button>
        </div>

        {runtimeMeasurements.length === 0 ? (
          <EmptyState
            title="No runtime measurements have been saved for this benchmark run yet."
            message="Run the mobile benchmark again to populate runtime cost-freshness data."
          />
        ) : (
          <div className="table-card">
            <table>
              <thead>
                <tr>
                  <th>Benchmark Code</th>
                  <th>Total Requests</th>
                  <th>Challenge Fetch Ms</th>
                  <th>Device Signing Ms</th>
                  <th>Backend Verification Ms</th>
                  <th>Fresh Proof Cost Ms</th>
                  <th>Operation Total Ms</th>
                  <th>Run Index</th>
                  <th>Policy</th>
                  <th>Fresh</th>
                  <th>Success</th>
                  <th>Timestamp</th>
                </tr>
              </thead>
              <tbody>
                {runtimeMeasurements.map((measurement) => (
                  <tr key={measurement.id}>
                    <td>{benchmark.code}</td>
                    <td>{formatNumber(measurement.totalRequests)}</td>
                    <td>{formatPreciseMeasurement(measurement.challengeFetchMs)}</td>
                    <td>{formatPreciseMeasurement(measurement.deviceSigningMs)}</td>
                    <td>
                      {formatPreciseMeasurement(
                        measurement.backendVerificationMs,
                      )}
                    </td>
                    <td>{formatPreciseMeasurement(measurement.freshProofCostMs)}</td>
                    <td>{formatPreciseMeasurement(measurement.operationTotalMs)}</td>
                    <td>{formatNumber(measurement.runIndex)}</td>
                    <td>{measurement.policyLabel}</td>
                    <td>{String(measurement.attestationPerformed)}</td>
                    <td>{String(measurement.success)}</td>
                    <td>{formatDateTime(measurement.clientTimestampUtc)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <div className="info-grid">
        <section className="info-card">
          <span className="eyebrow">Mobile Device</span>
          <dl>
            <InfoRow label="Device" value={benchmark.mobileDevice?.deviceName} />
            <InfoRow label="Device ID" value={benchmark.mobileDevice?.deviceId} />
            <InfoRow label="Manufacturer" value={benchmark.mobileDevice?.manufacturer} />
            <InfoRow label="Brand" value={benchmark.mobileDevice?.brand} />
            <InfoRow label="Model" value={benchmark.mobileDevice?.model} />
            <InfoRow label="Hardware" value={benchmark.mobileDevice?.hardware} />
            <InfoRow label="Android" value={benchmark.mobileDevice?.androidVersion} />
            <InfoRow
              label="SDK"
              value={
                benchmark.mobileDevice?.sdkInt === undefined
                  ? undefined
                  : String(benchmark.mobileDevice.sdkInt)
              }
            />
            <InfoRow
              label="CPU Cores"
              value={
                benchmark.mobileDevice?.cpuCoreCount === undefined
                  ? undefined
                  : String(benchmark.mobileDevice.cpuCoreCount)
              }
            />
            <InfoRow
              label="RAM"
              value={formatBytes(benchmark.mobileDevice?.totalMemoryBytes)}
            />
            <InfoRow
              label="ABIs"
              value={benchmark.mobileDevice?.supportedAbis?.join(', ')}
            />
            <InfoRow
              label="Emulator"
              value={
                benchmark.mobileDevice?.isEmulator === undefined
                  ? undefined
                  : String(benchmark.mobileDevice.isEmulator)
              }
            />
          </dl>
        </section>

        <section className="info-card">
          <span className="eyebrow">Backend System</span>
          <dl>
            <InfoRow label="Machine" value={benchmark.backendSystem?.machineName} />
            <InfoRow label="OS" value={benchmark.backendSystem?.osDescription} />
            <InfoRow
              label="Architecture"
              value={benchmark.backendSystem?.processArchitecture}
            />
            <InfoRow
              label="CPU Cores"
              value={
                benchmark.backendSystem
                  ? String(benchmark.backendSystem.processorCount)
                  : undefined
              }
            />
            <InfoRow
              label="Available RAM"
              value={formatBytes(benchmark.backendSystem?.totalAvailableMemoryBytes)}
            />
          </dl>
        </section>
      </div>

    </section>
  );
}

function InfoRow({
  label,
  value,
}: {
  label: string;
  value: string | null | undefined;
}) {
  return (
    <>
      <dt>{label}</dt>
      <dd>{value || '-'}</dd>
    </>
  );
}

function summarizeRuntimeMeasurements(
  benchmark: BenchmarkRun | null,
  measurements: RuntimeBenchmarkMeasurement[],
): RuntimeSummary {
  const firstMeasurement = measurements[0];
  const freshMeasurements = measurements.filter(
    (measurement) => measurement.attestationPerformed,
  );

  return {
    totalRequests:
      firstMeasurement?.totalRequests ?? benchmark?.iterationCount ?? null,
    policyLabel: firstMeasurement?.policyLabel ?? '-',
    freshAttestationCount: freshMeasurements.length,
    savedRuntimeMeasurementCount: measurements.length,
    averageChallengeFetchMs: average(
      freshMeasurements.map((measurement) => measurement.challengeFetchMs),
    ),
    averageDeviceSigningMs: average(
      freshMeasurements.map((measurement) => measurement.deviceSigningMs),
    ),
    averageBackendVerificationMs: average(
      freshMeasurements.map((measurement) => measurement.backendVerificationMs),
    ),
    averageFreshProofCostMs: average(
      freshMeasurements.map((measurement) => measurement.freshProofCostMs),
    ),
  };
}

function downloadRuntimeMeasurementsCsv(
  benchmark: BenchmarkRun,
  measurements: RuntimeBenchmarkMeasurement[],
) {
  if (measurements.length === 0) {
    window.alert('No runtime measurements available for export.');
    return;
  }

  const headers = [
    'Benchmark Code',
    'Total Requests',
    'Challenge Fetch Ms',
    'Device Signing Ms',
    'Backend Verification Ms',
    'Fresh Proof Cost Ms',
    'Operation Total Ms',
    'Started At',
    'Completed At',
    'Duration Ms',
    'Success',
    'Mobile Manufacturer',
    'Mobile Brand',
    'Mobile Model',
    'Mobile Device',
    'Mobile Hardware',
    'Android Version',
    'Android SDK',
    'Mobile CPU Cores',
    'Mobile RAM',
    'Mobile ABIs',
    'Is Emulator',
    'Backend Machine',
    'Backend OS',
    'Backend Architecture',
    'Backend CPU Cores',
    'Backend Available RAM',
    'Policy K',
    'Policy Label',
    'Run Index',
    'Attestation Performed',
    'Measurement Success',
    'Error Message',
    'Timestamp',
  ];

  const rows = measurements.map((measurement) => [
    benchmark.code,
    String(measurement.totalRequests),
    formatPreciseMeasurement(measurement.challengeFetchMs),
    formatPreciseMeasurement(measurement.deviceSigningMs),
    formatPreciseMeasurement(measurement.backendVerificationMs),
    formatPreciseMeasurement(measurement.freshProofCostMs),
    formatPreciseMeasurement(measurement.operationTotalMs),
    benchmark.startedAtUtc,
    benchmark.completedAtUtc ?? '',
    valueOrEmpty(benchmark.durationMs),
    String(benchmark.success),
    benchmark.mobileDevice?.manufacturer ?? '',
    benchmark.mobileDevice?.brand ?? '',
    benchmark.mobileDevice?.model ?? '',
    benchmark.mobileDevice?.device ?? '',
    benchmark.mobileDevice?.hardware ?? '',
    benchmark.mobileDevice?.androidVersion ?? '',
    valueOrEmpty(benchmark.mobileDevice?.sdkInt),
    valueOrEmpty(benchmark.mobileDevice?.cpuCoreCount),
    valueOrEmpty(benchmark.mobileDevice?.totalMemoryBytes),
    benchmark.mobileDevice?.supportedAbis?.join('|') ?? '',
    valueOrEmpty(benchmark.mobileDevice?.isEmulator),
    benchmark.backendSystem?.machineName ?? '',
    benchmark.backendSystem?.osDescription ?? '',
    benchmark.backendSystem?.processArchitecture ?? '',
    valueOrEmpty(benchmark.backendSystem?.processorCount),
    valueOrEmpty(benchmark.backendSystem?.totalAvailableMemoryBytes),
    measurement.policyK,
    measurement.policyLabel,
    String(measurement.runIndex),
    String(measurement.attestationPerformed),
    String(measurement.success),
    measurement.errorMessage ?? '',
    measurement.clientTimestampUtc,
  ]);

  const csv = [headers, ...rows]
    .map((row) => row.map(escapeCsvValue).join(','))
    .join('\r\n');

  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');

  link.href = url;
  link.download = `${benchmark.code}-runtime-measurements.csv`;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

function escapeCsvValue(value: string): string {
  if (!/[",\r\n]/.test(value)) {
    return value;
  }

  return `"${value.replace(/"/g, '""')}"`;
}

function average(values: number[]): number | null {
  if (values.length === 0) {
    return null;
  }

  return values.reduce((total, value) => total + value, 0) / values.length;
}

function valueOrEmpty(value: boolean | number | null | undefined): string {
  if (value === null || value === undefined) {
    return '';
  }

  return String(value);
}

function formatMetricMs(value: number | null): string {
  if (value === null) {
    return '-';
  }

  return `${formatPreciseMeasurement(value)} ms`;
}

function formatPreciseMeasurement(value: number): string {
  if (value === 0) {
    return '0';
  }

  const absoluteValue = Math.abs(value);

  if (absoluteValue < 1) {
    return trimTrailingZeros(value.toFixed(9));
  }

  if (Number.isInteger(value)) {
    return String(value);
  }

  return trimTrailingZeros(value.toFixed(6));
}

function trimTrailingZeros(value: string): string {
  return value.replace(/(\.\d*?[1-9])0+$/, '$1').replace(/\.0+$/, '');
}
