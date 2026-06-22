import { ArrowRight, RefreshCw } from 'lucide-react';
import { useEffect, useState } from 'react';

import { listBenchmarks, type BenchmarkRun } from '../api/benchmarks';
import { EmptyState } from '../components/EmptyState';
import { ErrorState } from '../components/ErrorState';
import { LoadingState } from '../components/LoadingState';
import { StatusBadge } from '../components/StatusBadge';
import { formatDateTime, formatDuration } from '../format';
import { benchmarkDetailPath } from '../routes';

type BenchmarkListPageProps = {
  onNavigate: (path: string) => void;
};

export function BenchmarkListPage({ onNavigate }: BenchmarkListPageProps) {
  const [benchmarks, setBenchmarks] = useState<BenchmarkRun[]>([]);
  const [isLoading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadBenchmarks = async () => {
    setError(null);
    setLoading(true);

    try {
      const data = await listBenchmarks();
      setBenchmarks(
        [...data].sort(
          (left, right) =>
            new Date(right.startedAtUtc).getTime() -
            new Date(left.startedAtUtc).getTime(),
        ),
      );
    } catch (ex) {
      setError(ex instanceof Error ? ex.message : 'Unknown request error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadBenchmarks();
  }, []);

  if (isLoading) {
    return <LoadingState />;
  }

  if (error) {
    return <ErrorState message={error} />;
  }

  return (
    <section className="page-stack">
      <div className="toolbar">
        <div>
          <h2>Benchmark history</h2>
          <p>Compare mobile attestation runs captured by the backend.</p>
        </div>
        <button type="button" className="icon-button" onClick={loadBenchmarks}>
          <RefreshCw size={16} aria-hidden="true" />
          Refresh
        </button>
      </div>

      {benchmarks.length === 0 ? (
        <EmptyState
          title="No benchmarks yet"
          message="Run a software or hardware benchmark from the mobile client to populate this dashboard."
        />
      ) : (
        <div className="table-card">
          <table>
            <thead>
              <tr>
                <th>Benchmark ID</th>
                <th>Type</th>
                <th>Status</th>
                <th>Started At</th>
                <th>Completed At</th>
                <th>Duration</th>
                <th>Success</th>
                <th>Detail</th>
              </tr>
            </thead>
            <tbody>
              {benchmarks.map((benchmark) => (
                <tr key={benchmark.id}>
                  <td>
                    <code>{benchmark.code}</code>
                  </td>
                  <td>{benchmark.type}</td>
                  <td>
                    <StatusBadge value={benchmark.status} />
                  </td>
                  <td>{formatDateTime(benchmark.startedAtUtc)}</td>
                  <td>{formatDateTime(benchmark.completedAtUtc)}</td>
                  <td>{formatDuration(benchmark.durationMs)}</td>
                  <td>
                    <StatusBadge value={String(benchmark.success)} />
                  </td>
                  <td>
                    <button
                      type="button"
                      className="table-action"
                      onClick={() => onNavigate(benchmarkDetailPath(benchmark.id))}
                    >
                      Open
                      <ArrowRight size={15} aria-hidden="true" />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
