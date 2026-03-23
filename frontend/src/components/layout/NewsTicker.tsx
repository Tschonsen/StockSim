import { useMarketStore } from '@/stores/marketStore';

/**
 * Scrolling news ticker at the bottom of the screen.
 * Shows live market events. Bible 3.6 & 13.1.
 */
export function NewsTicker() {
  const newsItems = useMarketStore((s) => s.newsItems);
  const selectStock = useMarketStore((s) => s.selectStock);

  const hasNews = newsItems.length > 0;

  return (
    <footer style={styles.ticker}>
      <span style={styles.badge}>NEWS</span>
      <div style={styles.scrollArea}>
        {hasNews ? (
          <div style={styles.scrollContent}>
            {newsItems.slice(0, 20).map((item, i) => {
              const color = item.sentiment > 0.1
                ? 'var(--green-primary)'
                : item.sentiment < -0.1
                  ? 'var(--red-primary)'
                  : 'var(--text-secondary)';

              const symbol = item.affectedSymbols[0];

              return (
                <span key={`${item.id}-${i}`} style={{ ...styles.tickerItem }}>
                  <span
                    style={{
                      ...styles.severityDot,
                      background: item.severity === 'Major' ? 'var(--red-primary)' :
                                  item.severity === 'Moderate' ? '#F59E0B' : 'var(--text-disabled)',
                    }}
                  />
                  <span
                    className={item.severity === 'Major' ? 'pulse' : ''}
                    style={{
                      ...styles.headline, color, cursor: symbol ? 'pointer' : 'default',
                      textShadow: item.severity === 'Major'
                        ? `0 0 8px ${item.sentiment > 0 ? 'var(--green-glow)' : 'var(--red-glow)'}`
                        : 'none',
                    }}
                    onClick={() => symbol && selectStock(symbol)}
                  >
                    {item.headline}
                  </span>
                  <span style={styles.separator}>|</span>
                </span>
              );
            })}
          </div>
        ) : (
          <span style={styles.emptyText}>Waiting for market events...</span>
        )}
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
  scrollContent: {
    display: 'inline-flex',
    alignItems: 'center',
    gap: '0',
    whiteSpace: 'nowrap' as const,
    animation: 'tickerScroll 60s linear infinite',
  },
  tickerItem: {
    display: 'inline-flex',
    alignItems: 'center',
    gap: '6px',
    flexShrink: 0,
  },
  severityDot: {
    width: '6px',
    height: '6px',
    borderRadius: '50%',
    flexShrink: 0,
  },
  headline: {
    fontFamily: 'var(--font-mono)',
    fontSize: '12px',
    whiteSpace: 'nowrap' as const,
  },
  separator: {
    color: 'var(--text-disabled)',
    margin: '0 12px',
    fontSize: '12px',
  },
  emptyText: {
    fontFamily: 'var(--font-mono)',
    fontSize: '12px',
    color: 'var(--text-disabled)',
    whiteSpace: 'nowrap' as const,
  },
};
