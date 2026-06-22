import { useEffect, useMemo, useState } from 'react';

import { DashboardLayout } from './layouts/DashboardLayout';
import { BenchmarkDetailPage } from './pages/BenchmarkDetailPage';
import { BenchmarkListPage } from './pages/BenchmarkListPage';
import { benchmarkListPath, resolveRoute, type DashboardRoute } from './routes';

function App() {
  const [route, setRoute] = useState<DashboardRoute>(() =>
    resolveRoute(window.location.pathname),
  );

  useEffect(() => {
    if (window.location.pathname === '/dashboard') {
      window.history.replaceState({}, '', benchmarkListPath());
      setRoute({ page: 'benchmarks' });
    }

    const handlePopState = () => {
      setRoute(resolveRoute(window.location.pathname));
    };

    window.addEventListener('popstate', handlePopState);

    return () => {
      window.removeEventListener('popstate', handlePopState);
    };
  }, []);

  const title = useMemo(() => {
    if (route.page === 'benchmark-detail') {
      return 'Benchmark Detail';
    }

    return 'Benchmark Runs';
  }, [route]);

  const subtitle = useMemo(() => {
    if (route.page === 'benchmark-detail') {
      return 'Review verification phases and raw measurements.';
    }

    return 'Inspect attestation benchmark runs captured by the backend.';
  }, [route]);

  const navigateTo = (path: string) => {
    window.history.pushState({}, '', path);
    setRoute(resolveRoute(path));
  };

  return (
    <DashboardLayout title={title} subtitle={subtitle} onNavigate={navigateTo}>
      {route.page === 'benchmark-detail' ? (
        <BenchmarkDetailPage
          benchmarkId={route.benchmarkId}
          onNavigate={navigateTo}
        />
      ) : (
        <BenchmarkListPage onNavigate={navigateTo} />
      )}
    </DashboardLayout>
  );
}

export default App;
