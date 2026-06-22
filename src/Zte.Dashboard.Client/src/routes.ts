export type DashboardRoute =
  | { page: 'benchmarks' }
  | { page: 'benchmark-detail'; benchmarkId: string };

const dashboardBasePath = '/dashboard';

export function resolveRoute(pathname: string): DashboardRoute {
  const relativePath = removeDashboardBase(pathname);
  const segments = relativePath
    .replace(/^\/+|\/+$/g, '')
    .split('/')
    .filter(Boolean);

  if (segments[0] === 'benchmarks' && segments[1]) {
    return {
      page: 'benchmark-detail',
      benchmarkId: decodeURIComponent(segments[1]),
    };
  }

  return { page: 'benchmarks' };
}

export function benchmarkListPath(): string {
  return `${dashboardBasePath}/benchmarks`;
}

export function benchmarkDetailPath(id: string): string {
  return `${dashboardBasePath}/benchmarks/${encodeURIComponent(id)}`;
}

function removeDashboardBase(pathname: string): string {
  if (pathname.toLowerCase() === dashboardBasePath) {
    return '';
  }

  if (pathname.toLowerCase().startsWith(`${dashboardBasePath}/`)) {
    return pathname.slice(dashboardBasePath.length);
  }

  return pathname;
}
