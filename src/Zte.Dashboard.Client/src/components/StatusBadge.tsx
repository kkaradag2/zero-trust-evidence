type StatusBadgeProps = {
  value: string;
};

export function StatusBadge({ value }: StatusBadgeProps) {
  const tone = getTone(value);

  return <span className={`status-badge ${tone}`}>{value}</span>;
}

function getTone(value: string): string {
  switch (value.toLowerCase()) {
    case 'completed':
    case 'low':
    case 'true':
      return 'status-badge--success';
    case 'failed':
    case 'high':
    case 'false':
      return 'status-badge--danger';
    case 'medium':
      return 'status-badge--warning';
    default:
      return 'status-badge--neutral';
  }
}
