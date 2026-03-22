export function NewsTicker() {
  return (
    <footer style={styles.ticker}>
      <span style={styles.badge}>NEWS</span>
      <div style={styles.scrollArea}>
        <span style={styles.tickerText}>
          Waiting for market data...
        </span>
      </div>
    </footer>
  );
}

const styles: Record<string, React.CSSProperties> = {
  ticker: {
    height: 'var(--ticker-height)',
    background: '#080C14',
    borderTop: '1px solid var(--border)',
    display: 'flex',
    alignItems: 'center',
    padding: '0 var(--space-3)',
    gap: 'var(--space-2)',
    flexShrink: 0,
  },
  badge: {
    background: 'var(--bg-tertiary)',
    color: 'var(--text-secondary)',
    fontSize: '10px',
    fontWeight: 600,
    padding: '2px 6px',
    borderRadius: '3px',
    flexShrink: 0,
  },
  scrollArea: {
    flex: 1,
    overflow: 'hidden',
  },
  tickerText: {
    fontFamily: 'var(--font-mono)',
    fontSize: '12px',
    color: 'var(--text-disabled)',
    whiteSpace: 'nowrap',
  },
};
