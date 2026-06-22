import { ArrowLeft, Download, RefreshCw } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';

import {
  getBenchmark,
  listBenchmarkMeasurements,
  type BenchmarkRun,
  type MeasurementPhase,
  type VerificationMeasurement,
} from '../api/benchmarks';
import { EmptyState } from '../components/EmptyState';
import { ErrorState } from '../components/ErrorState';
import { LoadingState } from '../components/LoadingState';
import { MetricCard } from '../components/MetricCard';
import { StatusBadge } from '../components/StatusBadge';
import { formatBytes, formatDateTime, formatDuration, formatNumber } from '../format';
import { benchmarkListPath } from '../routes';

const comparisonPhases: MeasurementPhase[] = [
  'SoftwareVerification',
  'HardwareEnrollment',
  'HardwareVerification',
];

type PhaseAggregate = {
  phase: MeasurementPhase;
  count: number;
  acceptedCount: number;
  successRate: number | null;
  averageTimeMs: number | null;
  minimumTimeMs: number | null;
  maximumTimeMs: number | null;
  standardDeviationTimeMs: number | null;
  averageTimeMicroseconds: number | null;
  averageMessageSizeBytes: number | null;
  averageProcessingStepCount: number | null;
};

type BenchmarkDetailPageProps = {
  benchmarkId: string;
  onNavigate: (path: string) => void;
};

export function BenchmarkDetailPage({
  benchmarkId,
  onNavigate,
}: BenchmarkDetailPageProps) {
  const [benchmark, setBenchmark] = useState<BenchmarkRun | null>(null);
  const [measurements, setMeasurements] = useState<VerificationMeasurement[]>([]);
  const [isLoading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadDetail = async () => {
    setError(null);
    setLoading(true);

    try {
      const [benchmarkData, measurementData] = await Promise.all([
        getBenchmark(benchmarkId),
        listBenchmarkMeasurements(benchmarkId),
      ]);

      setBenchmark(benchmarkData);
      setMeasurements(
        [...measurementData].sort(
          (left, right) =>
            new Date(left.createdAtUtc).getTime() -
            new Date(right.createdAtUtc).getTime(),
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

  const comparisonRows = useMemo(
    () => comparisonPhases.map((phase) => aggregatePhase(phase, measurements)),
    [measurements],
  );

  const softwareMeasurementCount = countMeasurementsByPhase(
    measurements,
    'SoftwareVerification',
  );
  const hardwareEnrollmentMeasurementCount = countMeasurementsByPhase(
    measurements,
    'HardwareEnrollment',
  );
  const hardwareVerificationMeasurementCount = countMeasurementsByPhase(
    measurements,
    'HardwareVerification',
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
          label="Runtime Iterations"
          value={formatNumber(benchmark.iterationCount)}
        />
        <MetricCard
          label="Software Measurements"
          value={formatNumber(softwareMeasurementCount)}
        />
        <MetricCard
          label="Hardware Enrollment Measurements"
          value={formatNumber(hardwareEnrollmentMeasurementCount)}
        />
        <MetricCard
          label="Hardware Verification Measurements"
          value={formatNumber(hardwareVerificationMeasurementCount)}
        />
        <MetricCard
          label="Total Measurements"
          value={formatNumber(measurements.length)}
        />
        <MetricCard label="Started At" value={formatDateTime(benchmark.startedAtUtc)} />
        <MetricCard
          label="Completed At"
          value={formatDateTime(benchmark.completedAtUtc)}
        />
        <MetricCard label="Duration" value={formatDuration(benchmark.durationMs)} />
      </div>

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

      <section className="table-section">
        <div className="section-heading">
          <h2>Runtime Comparison</h2>
          <p>
            SoftwareVerification and HardwareVerification are runtime phases.
            HardwareEnrollment is a separate setup cost.
          </p>
        </div>
        <div className="table-card">
          <table>
            <thead>
              <tr>
                <th>Phase</th>
                <th>Count</th>
                <th>Accepted Count</th>
                <th>Success Rate %</th>
                <th>Avg backend time ms</th>
                <th>Min backend time ms</th>
                <th>Max backend time ms</th>
                <th>Std dev backend time ms</th>
                <th>Avg backend time microseconds</th>
                <th>Avg message size bytes</th>
                <th>Avg processing step count</th>
              </tr>
            </thead>
            <tbody>
              {comparisonRows.map((row) => (
                <tr
                  key={row.phase}
                  className={
                    row.phase === 'HardwareEnrollment'
                      ? 'setup-cost-row'
                      : 'runtime-comparison-row'
                  }
                >
                  <td>{row.phase}</td>
                  <td>{formatNumber(row.count)}</td>
                  <td>{formatNumber(row.acceptedCount)}</td>
                  <td>{formatPercent(row.successRate)}</td>
                  <td>{formatMetric(row.averageTimeMs)}</td>
                  <td>{formatMetric(row.minimumTimeMs)}</td>
                  <td>{formatMetric(row.maximumTimeMs)}</td>
                  <td>{formatMetric(row.standardDeviationTimeMs)}</td>
                  <td>{formatMetric(row.averageTimeMicroseconds)}</td>
                  <td>{formatMetric(row.averageMessageSizeBytes)}</td>
                  <td>{formatMetric(row.averageProcessingStepCount)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <section className="table-section">
        <div className="section-heading">
          <div>
            <h2>Raw Measurements</h2>
            <p>All measurement records associated with this benchmark run.</p>
          </div>
          <button
            type="button"
            className="icon-button"
            onClick={() => downloadRawMeasurementsCsv(benchmark, measurements)}
            disabled={measurements.length === 0}
          >
            <Download size={16} aria-hidden="true" />
            Download CSV
          </button>
        </div>

        {measurements.length === 0 ? (
          <EmptyState title="No measurements recorded yet." />
        ) : (
          <div className="table-card">
            <table>
              <thead>
                <tr>
                  <th>Created At</th>
                  <th>Attestation Type</th>
                  <th>Accepted</th>
                  <th>Risk Level</th>
                  <th>Time ms</th>
                  <th>Time microseconds</th>
                  <th>Message bytes</th>
                  <th>Steps</th>
                </tr>
              </thead>
              <tbody>
                {measurements.map((measurement) => (
                  <tr key={measurement.id}>
                    <td>{formatDateTime(measurement.createdAtUtc)}</td>
                    <td>{measurement.attestationType}</td>
                    <td>
                      <StatusBadge value={String(measurement.accepted)} />
                    </td>
                    <td>
                      <StatusBadge value={measurement.riskLevel} />
                    </td>
                    <td>{formatNumber(measurement.verificationTimeMs)}</td>
                    <td>{formatNumber(measurement.verificationTimeMicroseconds)}</td>
                    <td>{formatNumber(measurement.messageSizeBytes)}</td>
                    <td>{formatNumber(measurement.processingStepCount)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
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

function downloadRawMeasurementsCsv(
  benchmark: BenchmarkRun,
  measurements: VerificationMeasurement[],
) {
  const headers = [
    'Benchmark Code',
    'Benchmark Id',
    'Created At',
    'Attestation Type',
    'Accepted',
    'Risk Level',
    'Verification Time Ms',
    'Verification Time Microseconds',
    'Message Size Bytes',
    'Processing Step Count',
  ];

  const rows = measurements.map((measurement) => [
    benchmark.code,
    benchmark.id,
    measurement.createdAtUtc,
    measurement.attestationType,
    String(measurement.accepted),
    measurement.riskLevel,
    String(measurement.verificationTimeMs),
    String(measurement.verificationTimeMicroseconds),
    String(measurement.messageSizeBytes),
    String(measurement.processingStepCount),
  ]);

  const csv = [headers, ...rows]
    .map((row) => row.map(escapeCsvValue).join(','))
    .join('\r\n');

  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');

  link.href = url;
  link.download = `${benchmark.code}-raw-measurements.csv`;
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

function aggregatePhase(
  phase: MeasurementPhase,
  measurements: VerificationMeasurement[],
): PhaseAggregate {
  const phaseMeasurements = measurements.filter((item) => item.phase === phase);
  const verificationTimes = phaseMeasurements.map(
    (item) => item.verificationTimeMs,
  );
  const averageTimeMs = average(verificationTimes);

  return {
    phase,
    count: phaseMeasurements.length,
    acceptedCount: phaseMeasurements.filter((item) => item.accepted).length,
    successRate:
      phaseMeasurements.length === 0
        ? null
        : phaseMeasurements.filter((item) => item.accepted).length /
          phaseMeasurements.length,
    averageTimeMs,
    minimumTimeMs:
      verificationTimes.length === 0 ? null : Math.min(...verificationTimes),
    maximumTimeMs:
      verificationTimes.length === 0 ? null : Math.max(...verificationTimes),
    standardDeviationTimeMs: standardDeviation(verificationTimes, averageTimeMs),
    averageTimeMicroseconds: average(
      phaseMeasurements.map((item) => item.verificationTimeMicroseconds),
    ),
    averageMessageSizeBytes: average(
      phaseMeasurements.map((item) => item.messageSizeBytes),
    ),
    averageProcessingStepCount: average(
      phaseMeasurements.map((item) => item.processingStepCount),
    ),
  };
}

function countMeasurementsByPhase(
  measurements: VerificationMeasurement[],
  phase: MeasurementPhase,
): number {
  return measurements.filter((item) => item.phase === phase).length;
}

function average(values: number[]): number | null {
  if (values.length === 0) {
    return null;
  }

  return values.reduce((total, value) => total + value, 0) / values.length;
}

function standardDeviation(
  values: number[],
  averageValue: number | null,
): number | null {
  if (values.length === 0 || averageValue === null) {
    return null;
  }

  const variance =
    values.reduce((total, value) => total + (value - averageValue) ** 2, 0) /
    values.length;

  return Math.sqrt(variance);
}

function formatMetric(value: number | null): string {
  if (value === null) {
    return '-';
  }

  return new Intl.NumberFormat(undefined, {
    maximumFractionDigits: 3,
  }).format(value);
}

function formatPercent(value: number | null): string {
  if (value === null) {
    return '-';
  }

  return new Intl.NumberFormat(undefined, {
    maximumFractionDigits: 1,
    style: 'percent',
  }).format(value);
}
