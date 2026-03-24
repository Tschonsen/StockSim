import { useMemo, useState } from 'react';
import { useMarketStore } from '@/stores/marketStore';
import { X, Plus } from 'lucide-react';

export function LeftSidebar() {
  const watchlist = useMarketStore((s) => s.watchlist);
  const watchlists = useMarketStore((s) => s.watchlists);
  const activeWatchlistName = useMarketStore((s) => s.activeWatchlistName);
  const createWatchlist = useMarketStore((s) => s.createWatchlist);
  const deleteWatchlist = useMarketStore((s) => s.deleteWatchlist);
  const setActiveWatchlist = useMarketStore((s) => s.setActiveWatchlist);
  const stocks = useMarketStore((s) => s.stocks);
  const stockList = useMarketStore((s) => s.stockList);
  const selectedSymbol = useMarketStore((s) => s.selectedSymbol);
  const selectStock = useMarketStore((s) => s.selectStock);
  const removeFromWatchlist = useMarketStore((s) => s.removeFromWatchlist);
  const priceFlash = useMarketStore((s) => s.priceFlash);
  const [showNewListInput, setShowNewListInput] = useState(false);
  const [newListName, setNewListName] = useState('');
  const watchlistNames = Object.keys(watchlists);

  // Sector summary from stock data
  const sectorSummary = useMemo(() => {
    const sectors = new Map<string, { count: number; avgChange: number }>();
    stockList.forEach(s => {
      const existing = sectors.get(s.sector) || { count: 0, avgChange: 0 };
      existing.avgChange = (existing.avgChange * existing.count + s.changePercent) / (existing.count + 1);
      existing.count++;
      sectors.set(s.sector, existing);
    });
    return Array.from(sectors.entries())
      .sort((a, b) => b[1].avgChange - a[1].avgChange);
  }, [stockList]);

  return (
    <aside style={styles.sidebar}>
      {/* Watchlist */}
      <div style={styles.panel}>
        <div style={styles.panelHeader}>
          <span style={styles.panelTitle}>Watchlist ({watchlist.length})</span>
          {watchlistNames.length < 5 && (
            <button onClick={() => setShowNewListInput(true)} style={{
              background: 'transparent', border: 'none', color: 'var(--text-disabled)',
              cursor: 'pointer', padding: '2px', fontSize: '12px',
            }} title="New watchlist"><Plus size={12} /></button>
          )}
        </div>
        {/* Watchlist Tabs */}
        {watchlistNames.length > 1 && (
          <div style={{ display: 'flex', gap: '2px', padding: '0 8px 4px', flexWrap: 'wrap' }}>
            {watchlistNames.map(name => (
              <button key={name} onClick={() => setActiveWatchlist(name)} style={{
                padding: '2px 8px', border: 'none', borderRadius: '3px', cursor: 'pointer',
                fontSize: '10px', fontWeight: 600, fontFamily: 'var(--font-ui)',
                background: name === activeWatchlistName ? 'var(--text-accent)' : 'var(--bg-tertiary)',
                color: name === activeWatchlistName ? '#FFF' : 'var(--text-disabled)',
              }}>
                {name}
                {name !== 'Main' && name === activeWatchlistName && (
                  <span onClick={e => { e.stopPropagation(); deleteWatchlist(name); }}
                    style={{ marginLeft: '4px', opacity: 0.6 }}>x</span>
                )}
              </button>
            ))}
          </div>
        )}
        {showNewListInput && (
          <div style={{ display: 'flex', gap: '4px', padding: '0 8px 4px' }}>
            <input type="text" value={newListName} onChange={e => setNewListName(e.target.value)}
              placeholder="Name..." autoFocus
              onKeyDown={e => { if (e.key === 'Enter' && newListName.trim()) { createWatchlist(newListName.trim()); setNewListName(''); setShowNewListInput(false); } if (e.key === 'Escape') setShowNewListInput(false); }}
              style={{ flex: 1, height: '22px', background: 'var(--bg-input)', border: '1px solid var(--border)', borderRadius: '3px', color: 'var(--text-primary)', fontSize: '11px', padding: '0 6px', fontFamily: 'var(--font-ui)' }} />
          </div>
        )}
        <div style={styles.list}>
          {watchlist.length === 0 ? (
            <div style={styles.empty}>
              Your watchlist is empty.
              <div style={{ marginTop: '4px', fontSize: '11px' }}>
                Click + on any stock in the Market tab to add it.
              </div>
            </div>
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
                  onMouseEnter={(e) => { if (!isSelected) e.currentTarget.style.background = 'var(--bg-tertiary)'; }}
                  onMouseLeave={(e) => { if (!isSelected) e.currentTarget.style.background = 'transparent'; }}
                >
                  <div style={{ flex: 1, minWidth: 0 }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
                      <span className="mono" style={styles.symbol}>{stock.symbol}</span>
                      {stock.traits.includes('ETF') && (
                        <span style={{ fontSize: '8px', color: '#8B5CF6', fontWeight: 700, letterSpacing: '0.5px' }}>ETF</span>
                      )}
                    </div>
                    <span style={styles.name}>{stock.name}</span>
                    <span style={{ fontSize: '9px', color: 'var(--text-disabled)', display: 'block' }}>
                      {stock.sector} | Vol: {stock.volume >= 1e6 ? `${(stock.volume / 1e6).toFixed(1)}M` : `${(stock.volume / 1e3).toFixed(0)}K`}
                    </span>
                  </div>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                    <div style={styles.priceCol}>
                      <span
                        key={`${symbol}-${stock.price}`}
                        className={`mono ${priceFlash.get(symbol) === 'up' ? 'price-up' : priceFlash.get(symbol) === 'down' ? 'price-down' : ''}`}
                        style={styles.price}
                      >${stock.price.toFixed(2)}</span>
                      <span
                        className={`mono ${stock.changePercent >= 0 ? 'positive' : 'negative'}`}
                        style={styles.change}
                      >
                        {stock.changePercent >= 0 ? '+' : ''}{stock.changePercent.toFixed(2)}%
                      </span>
                    </div>
                    <button
                      onClick={(e) => { e.stopPropagation(); removeFromWatchlist(symbol); }}
                      style={styles.removeBtn}
                      title="Remove from watchlist"
                    >
                      <X size={12} />
                    </button>
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
          <span style={styles.panelTitle}>Sectors ({sectorSummary.length})</span>
        </div>
        <div style={styles.list}>
          {sectorSummary.length === 0 ? (
            <div style={styles.empty}>Loading sectors...</div>
          ) : (
            sectorSummary.map(([name, data]) => (
              <div key={name} style={styles.sectorRow}>
                <span style={styles.sectorName}>{name}</span>
                <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                  <span style={styles.sectorCount}>{data.count}</span>
                  <span className="mono" style={{
                    fontSize: '12px',
                    fontWeight: 600,
                    color: data.avgChange >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                  }}>
                    {data.avgChange >= 0 ? '+' : ''}{data.avgChange.toFixed(2)}%
                  </span>
                </div>
              </div>
            ))
          )}
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
  removeBtn: {
    background: 'transparent',
    border: 'none',
    color: 'var(--text-disabled)',
    cursor: 'pointer',
    padding: '2px',
    borderRadius: '2px',
    display: 'flex',
    alignItems: 'center',
    opacity: 0.5,
  },
  sectorRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: '6px 12px',
    fontSize: '12px',
  },
  sectorName: {
    color: 'var(--text-secondary)',
    fontSize: '12px',
    overflow: 'hidden',
    textOverflow: 'ellipsis',
    whiteSpace: 'nowrap',
    maxWidth: '140px',
  },
  sectorCount: {
    fontSize: '10px',
    color: 'var(--text-disabled)',
    background: 'var(--bg-tertiary)',
    padding: '1px 4px',
    borderRadius: '3px',
  },
};
