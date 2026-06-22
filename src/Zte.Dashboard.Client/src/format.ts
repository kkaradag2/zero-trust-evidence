export function formatDateTime(value: string | null | undefined): string {
  if (!value) {
    return '-';
  }

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'medium',
  }).format(new Date(value));
}

export function formatDuration(value: number | null | undefined): string {
  if (value === null || value === undefined) {
    return '-';
  }

  if (value < 1000) {
    return `${Math.round(value)} ms`;
  }

  return `${(value / 1000).toFixed(2)} s`;
}

export function formatNumber(value: number | null | undefined): string {
  if (value === null || value === undefined) {
    return '-';
  }

  return new Intl.NumberFormat().format(value);
}

export function formatBytes(value: number | null | undefined): string {
  if (value === null || value === undefined) {
    return '-';
  }

  const gibibytes = value / 1024 / 1024 / 1024;

  if (gibibytes >= 1) {
    return `${gibibytes.toFixed(2)} GB`;
  }

  const mebibytes = value / 1024 / 1024;

  return `${mebibytes.toFixed(0)} MB`;
}
