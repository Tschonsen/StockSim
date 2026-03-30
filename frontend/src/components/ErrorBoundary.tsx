import React from 'react';

interface ErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
}

export class ErrorBoundary extends React.Component<
  { children: React.ReactNode },
  ErrorBoundaryState
> {
  constructor(props: { children: React.ReactNode }) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, info: React.ErrorInfo) {
    console.error('[ErrorBoundary] Uncaught error:', error, info.componentStack);
  }

  handleReload = () => {
    window.location.reload();
  };

  handleReport = () => {
    const subject = encodeURIComponent('StockSim Bug Report');
    const body = encodeURIComponent(
      `Error: ${this.state.error?.message}\n\nStack:\n${this.state.error?.stack}\n\nVersion: 0.2.0`
    );
    window.open(`mailto:support@stocksim.app?subject=${subject}&body=${body}`);
  };

  render() {
    if (this.state.hasError) {
      return (
        <div style={styles.container}>
          <div style={styles.card}>
            <div style={styles.icon}>!</div>
            <h1 style={styles.title}>SYSTEM FAILURE</h1>
            <p style={styles.subtitle}>
              An unexpected error crashed the application.
            </p>
            <div style={styles.errorBox}>
              <span style={styles.errorLabel}>ERROR</span>
              <code style={styles.errorText}>
                {this.state.error?.message || 'Unknown error'}
              </code>
            </div>
            <div style={styles.actions}>
              <button style={styles.reloadBtn} onClick={this.handleReload}>
                Reload Application
              </button>
              <button style={styles.reportBtn} onClick={this.handleReport}>
                Report Bug
              </button>
            </div>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}

const styles: Record<string, React.CSSProperties> = {
  container: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    height: '100vh',
    background: '#0a0e17',
    fontFamily: "'Inter', -apple-system, sans-serif",
  },
  card: {
    textAlign: 'center',
    maxWidth: '480px',
    padding: '48px 40px',
    background: '#111827',
    border: '1px solid #1f2937',
    borderRadius: '8px',
  },
  icon: {
    width: '48px',
    height: '48px',
    lineHeight: '48px',
    borderRadius: '50%',
    background: 'rgba(239, 68, 68, 0.15)',
    color: '#ef4444',
    fontSize: '24px',
    fontWeight: 700,
    margin: '0 auto 20px',
    border: '1px solid rgba(239, 68, 68, 0.3)',
  },
  title: {
    fontSize: '18px',
    fontWeight: 700,
    color: '#ef4444',
    letterSpacing: '3px',
    margin: '0 0 8px',
    fontFamily: "'JetBrains Mono', monospace",
  },
  subtitle: {
    fontSize: '14px',
    color: '#9ca3af',
    margin: '0 0 24px',
  },
  errorBox: {
    background: '#0d1117',
    border: '1px solid #1f2937',
    borderRadius: '4px',
    padding: '12px 16px',
    marginBottom: '28px',
    textAlign: 'left',
  },
  errorLabel: {
    display: 'block',
    fontSize: '10px',
    fontWeight: 700,
    letterSpacing: '1.5px',
    color: '#ef4444',
    marginBottom: '6px',
    fontFamily: "'JetBrains Mono', monospace",
  },
  errorText: {
    fontSize: '12px',
    color: '#d1d5db',
    wordBreak: 'break-word',
    fontFamily: "'JetBrains Mono', monospace",
    lineHeight: 1.5,
  },
  actions: {
    display: 'flex',
    gap: '12px',
    justifyContent: 'center',
  },
  reloadBtn: {
    padding: '10px 24px',
    fontSize: '13px',
    fontWeight: 600,
    color: '#fff',
    background: '#60a5fa',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
    fontFamily: "'Inter', sans-serif",
  },
  reportBtn: {
    padding: '10px 24px',
    fontSize: '13px',
    fontWeight: 600,
    color: '#9ca3af',
    background: 'transparent',
    border: '1px solid #374151',
    borderRadius: '4px',
    cursor: 'pointer',
    fontFamily: "'Inter', sans-serif",
  },
};
