import { Activity, BarChart3, ShieldCheck } from 'lucide-react';
import type { ReactNode } from 'react';

import { benchmarkListPath } from '../routes';

type DashboardLayoutProps = {
  title: string;
  subtitle: string;
  children: ReactNode;
  onNavigate: (path: string) => void;
};

export function DashboardLayout({
  title,
  subtitle,
  children,
  onNavigate,
}: DashboardLayoutProps) {
  return (
    <main className="dashboard-shell">
      <aside className="dashboard-sidebar">
        <button
          type="button"
          className="brand"
          aria-label="Go to benchmark runs"
          onClick={() => onNavigate(benchmarkListPath())}
        >
          <span className="brand__mark">
            <ShieldCheck size={21} strokeWidth={2.1} aria-hidden="true" />
          </span>
          <span>
            <strong>Zero Trust</strong>
            <small>Evidence Dashboard</small>
          </span>
        </button>

        <nav className="dashboard-nav" aria-label="Dashboard navigation">
          <button
            type="button"
            className="dashboard-nav__item dashboard-nav__item--active"
            onClick={() => onNavigate(benchmarkListPath())}
          >
            <BarChart3 size={18} aria-hidden="true" />
            Benchmark Runs
          </button>
        </nav>

        <div className="dashboard-sidebar__footer">
          <Activity size={16} aria-hidden="true" />
          In-memory benchmark telemetry
        </div>
      </aside>

      <section className="dashboard-main">
        <header className="dashboard-header">
          <div>
            <h1>{title}</h1>
            <p>{subtitle}</p>
          </div>
        </header>

        <div className="dashboard-content">{children}</div>
      </section>
    </main>
  );
}
