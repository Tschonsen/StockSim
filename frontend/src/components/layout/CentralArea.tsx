import { useMarketStore } from '@/stores/marketStore';
import { Plus } from 'lucide-react';

export function CentralArea() {
  const activeTab = useMarketStore((s) => s.activeTab);
  const stockList = useMarketStore((s) => s.stockList);
  const isGameActive = useMarketStore((s) => s.isGameActive);
  const speed = useMarketStore((s) => s.speed);
  const selectStock = useMarketStore((s) => s.selectStock);
  const addToWatchlist = useMarketStore((s) => s.addToWatchlist);
  const watchlist = useMarketStore((s) => s.watchlist);

  if (!isGameActive) {
    return (
      <main style={styles.central}>
        <div style={styles.placeholder}>
          <span style={styles.logoLarge}>STOCKSIM</span>
          <span style={styles.subtitle}>Connecting to backend...</span>
        </div>
      </main>
    );
  }

  return (
    <main style={styles.central}>
      {/* PAUSED overlay */}
      {speed === 0 && (
        <div style={styles.pausedOverlay}>PAUSED</div>
      )}

      {activeTab === 'dashboard' && (
        <div style={styles.content}>
          <h2 style={styles.heading}>Dashboard</h2>
          <p style={styles.info}>{stockList.length} stocks loaded</p>
          {/* Top Movers */}
          <div style={styles.moversRow}>
            <div style={styles.moversCol}>
              <h3 style={{ ...styles.moversTitle, color: 'var(--green-primary)' }}>Top Gainers ▲</h3>
              {[...stockList]
                .sort((a, b) => b.changePercent - a.changePercent)
                .slice(0, 5)
                .map((s) => (
                  <div key={s.symbol} style={styles.moverRow}>
                    <span className="mono" style={styles.moverSymbol}>{s.symbol}</span>
                    <span style={styles.moverName}>{s.name}</span>
                    <span className="mono positive">{s.changePercent >= 0 ? '+' : ''}{s.changePercent.toFixed(2)}%</span>
                  </div>
                ))}
            </div>
            <div style={styles.moversCol}>
              <h3 style={{ ...styles.moversTitle, color: 'var(--red-primary)' }}>Top Losers ▼</h3>
              {[...stockList]
                .sort((a, b) => a.changePercent - b.changePercent)
                .slice(0, 5)
                .map((s) => (
                  <div key={s.symbol} style={styles.moverRow}>
                    <span className="mono" style={styles.moverSymbol}>{s.symbol}</span>
                    <span style={styles.moverName}>{s.name}</span>
                    <span className="mono negative">{s.changePercent.toFixed(2)}%</span>
                  </div>
                ))}
            </div>
          </div>
        </div>
      )}

      {activeTab === 'market' && (
        <div style={styles.content}>
          <h2 style={styles.heading}>Market ({stockList.length} stocks)</h2>
          <div style={styles.tableContainer}>
            <table style={styles.table}>
              <thead>
                <tr>
                  <th style={styles.th}>Symbol</th>
                  <th style={styles.th}>Name</th>
                  <th style={styles.th}>Sector</th>
                  <th style={{ ...styles.th, textAlign: 'right' }}>Price</th>
                  <th style={{ ...styles.th, textAlign: 'right' }}>Change</th>
                  <th style={{ ...styles.th, textAlign: 'right' }}>Volume</th>
                  <th style={{ ...styles.th, textAlign: 'center', width: '40px' }}></th>
                </tr>
              </thead>
              <tbody>
                {stockList.slice(0, 50).map((s) => (
                  <tr
                    key={s.symbol}
                    style={styles.tr}
                    onClick={() => selectStock(s.symbol)}
                    onMouseEnter={(e) => (e.currentTarget.style.background = 'var(--bg-tertiary)')}
                    onMouseLeave={(e) => (e.currentTarget.style.background = 'transparent')}
                  >
                    <td className="mono" style={{ ...styles.td, fontWeight: 700 }}>{s.symbol}</td>
                    <td style={styles.td}>{s.name}</td>
                    <td style={{ ...styles.td, color: 'var(--text-secondary)', fontSize: '12px' }}>{s.sector}</td>
                    <td className="mono" style={{ ...styles.td, textAlign: 'right' }}>${s.price.toFixed(2)}</td>
                    <td className="mono" style={{
                      ...styles.td,
                      textAlign: 'right',
                      color: s.changePercent >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                    }}>
                      {s.changePercent >= 0 ? '+' : ''}{s.changePercent.toFixed(2)}%
                    </td>
                    <td className="mono" style={{ ...styles.td, textAlign: 'right', color: 'var(--text-secondary)' }}>
                      {s.volume >= 1_000_000 ? `${(s.volume / 1_000_000).toFixed(1)}M` :
                       s.volume >= 1_000 ? `${(s.volume / 1_000).toFixed(1)}K` :
                       s.volume.toString()}
                    </td>
                    <td style={{ ...styles.td, textAlign: 'center' }}>
                      {!watchlist.includes(s.symbol) && (
                        <button
                          style={styles.addBtn}
                          onClick={(e) => { e.stopPropagation(); addToWatchlist(s.symbol); }}
                          title="Add to Watchlist"
                        >
                          <Plus size={14} />
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {activeTab !== 'dashboard' && activeTab !== 'market' && (
        <div style={styles.content}>
          <div style={styles.tabPlaceholder}>
            {activeTab.charAt(0).toUpperCase() + activeTab.slice(1)} — Coming soon
          </div>
        </div>
      )}
    </main>
  );
}

const styles: Record<string, React.CSSProperties> = {
  central: {
    flex: 1,
    background: 'var(--bg-primary)',
    overflow: 'hidden',
    position: 'relative',
    display: 'flex',
    flexDirection: 'column',
  },
  content: {
    flex: 1,
    padding: 'var(--space-4)',
    overflowY: 'auto',
  },
  placeholder: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    height: '100%',
    gap: 'var(--space-2)',
  },
  logoLarge: {
    fontFamily: 'var(--font-ui)',
    fontWeight: 900,
    fontSize: '48px',
    color: 'var(--text-primary)',
    opacity: 0.3,
  },
  subtitle: {
    color: 'var(--text-disabled)',
    fontSize: '16px',
  },
  pausedOverlay: {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    fontSize: '48px',
    fontWeight: 800,
    color: 'rgba(249, 250, 251, 0.15)',
    pointerEvents: 'none',
    zIndex: 10,
    fontFamily: 'var(--font-ui)',
  },
  heading: {
    fontSize: '16px',
    fontWeight: 600,
    marginBottom: 'var(--space-4)',
    color: 'var(--text-primary)',
  },
  info: {
    color: 'var(--text-secondary)',
    fontSize: '13px',
    marginBottom: 'var(--space-4)',
  },
  moversRow: {
    display: 'flex',
    gap: 'var(--space-6)',
  },
  moversCol: {
    flex: 1,
  },
  moversTitle: {
    fontSize: '14px',
    fontWeight: 600,
    marginBottom: 'var(--space-2)',
  },
  moverRow: {
    display: 'flex',
    alignItems: 'center',
    gap: 'var(--space-2)',
    padding: '6px 0',
    fontSize: '13px',
  },
  moverSymbol: {
    fontWeight: 700,
    width: '60px',
    color: 'var(--text-primary)',
  },
  moverName: {
    flex: 1,
    color: 'var(--text-secondary)',
    fontSize: '12px',
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
  },
  tableContainer: {
    overflowX: 'auto',
  },
  table: {
    width: '100%',
    borderCollapse: 'collapse',
    fontSize: '13px',
  },
  th: {
    textAlign: 'left',
    padding: '8px',
    fontWeight: 600,
    fontSize: '11px',
    color: 'var(--text-secondary)',
    textTransform: 'uppercase',
    borderBottom: '1px solid var(--border)',
    background: 'var(--bg-secondary)',
    position: 'sticky',
    top: 0,
  },
  tr: {
    borderBottom: '1px solid rgba(31, 41, 55, 0.3)',
    cursor: 'pointer',
    transition: 'background 150ms',
  },
  td: {
    padding: '8px',
    color: 'var(--text-primary)',
  },
  addBtn: {
    background: 'transparent',
    border: '1px solid var(--border)',
    borderRadius: '4px',
    color: 'var(--text-secondary)',
    cursor: 'pointer',
    padding: '2px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    transition: 'color 150ms, border-color 150ms',
  },
  tabPlaceholder: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    height: '100%',
    color: 'var(--text-disabled)',
    fontSize: '20px',
    fontWeight: 600,
  },
};
