import { useMarketStore } from '@/stores/marketStore';

export function LeftSidebar() {
  const watchlist = useMarketStore((s) => s.watchlist);
  const stocks = useMarketStore((s) => s.stocks);
  const selectedSymbol = useMarketStore((s) => s.selectedSymbol);
  const selectStock = useMarketStore((s) => s.selectStock);

  return (
    <aside style={styles.sidebar}>
      {/* Watchlist */}
      <div style={styles.panel}>
        <div style={styles.panelHeader}>
          <span style={styles.panelTitle}>Watchlist ({watchlist.length})</span>
        </div>
        <div style={styles.list}>
          {watchlist.length === 0 ? (
            <div style={styles.empty}>Your watchlist is empty.</div>
          ) : (
            watchlist.map((symbol) => {
              const stock = stocks.get(symbol);
              if (!stock) return null;
              const isSelected = selectedSymbol === symbol;
              return (
                <div
                  key={symbol}
                  style={{
                    ...styles.stockRow,
                    ...(isSelected ? styles.stockRowSelected : {}),
                  }}
                  onClick={() => selectStock(symbol)}
                >
                  <div>
                    <span className="mono" style={styles.symbol}>{stock.symbol}</span>
                    <span style={styles.name}>{stock.name}</span>
                  </div>
                  <div style={styles.priceCol}>
                    <span className="mono" style={styles.price}>${stock.price.toFixed(2)}</span>
                    <span
                      className="mono"
                      style={{
                        ...styles.change,
                        color: stock.changePercent >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                      }}
                    >
                      {stock.changePercent >= 0 ? '+' : ''}{stock.changePercent.toFixed(2)}%
                    </span>
                  </div>
                </div>
              );
            })
          )}
        </div>
      </div>

      {/* Sectors */}
      <div style={styles.panel}>
        <div style={styles.panelHeader}>
          <span style={styles.panelTitle}>Sectors</span>
        </div>
        <div style={styles.sectorPlaceholder}>
          Sector overview loading...
        </div>
      </div>
    </aside>
  );
}

const styles: Record<string, React.CSSProperties> = {
  sidebar: {
    width: 'var(--sidebar-left-width)',
    background: 'var(--bg-secondary)',
    borderRight: '1px solid var(--border)',
    display: 'flex',
    flexDirection: 'column',
    flexShrink: 0,
    overflow: 'hidden',
  },
  panel: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    overflow: 'hidden',
  },
  panelHeader: {
    padding: '8px 12px',
    borderBottom: '1px solid var(--border)',
  },
  panelTitle: {
    fontWeight: 600,
    fontSize: '14px',
    color: 'var(--text-primary)',
  },
  list: {
    flex: 1,
    overflowY: 'auto',
  },
  empty: {
    padding: '20px 12px',
    color: 'var(--text-disabled)',
    fontSize: '13px',
    textAlign: 'center',
  },
  stockRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: '8px 12px',
    cursor: 'pointer',
    transition: 'background 150ms',
    borderLeft: '2px solid transparent',
  },
  stockRowSelected: {
    borderLeftColor: 'var(--text-accent)',
    background: 'var(--bg-tertiary)',
  },
  symbol: {
    fontWeight: 700,
    fontSize: '13px',
    color: 'var(--text-primary)',
    display: 'block',
  },
  name: {
    fontSize: '10px',
    color: 'var(--text-secondary)',
    display: 'block',
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
    maxWidth: '120px',
  },
  priceCol: {
    textAlign: 'right',
  },
  price: {
    fontSize: '13px',
    color: 'var(--text-primary)',
    display: 'block',
  },
  change: {
    fontSize: '12px',
    display: 'block',
  },
  sectorPlaceholder: {
    padding: '20px 12px',
    color: 'var(--text-disabled)',
    fontSize: '12px',
  },
};
