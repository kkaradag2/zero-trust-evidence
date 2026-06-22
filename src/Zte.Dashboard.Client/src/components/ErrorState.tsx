type ErrorStateProps = {
  message: string;
};

export function ErrorState({ message }: ErrorStateProps) {
  return (
    <div className="state-panel state-panel--error">
      <strong>Unable to load dashboard data.</strong>
      <span>{message}</span>
    </div>
  );
}
