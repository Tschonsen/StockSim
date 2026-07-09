import { useMemo, useState } from 'react';
import { useMarketStore } from '@/stores/marketStore';
import { X, Plus, PanelLeftClose, PanelLeftOpen, Search } from 'lucide-react';

export function LeftSidebar() {
  const [collapsed, setCollapsed] = useState(false);
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
  const [watchlistFilter, setWatchlistFilter] = useState('');
  const [watchlistSort, setWatchlistSort] = useState<'default' | 'gainers' | 'losers'>('default');
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

  if (collapsed) {
    return (
      <aside style={{ ...styles.sidebar, width: '36px', minWidth: '36px', alignItems: 'center', padding: '8px 0' }}>
        <button onClick={() => setCollapsed(false)} style={styles.collapseBtn} title="Expand sidebar" aria-label="Expand sidebar">
          <PanelLeftOpen size={16} />
        </button>
      </aside>
    );
  }

  return (
    <aside style={styles.sidebar}>
      {/* Watchlist */}
      <div style={styles.panel}>
        <div style={styles.panelHeader}>
          <span style={styles.panelTitle}>Watchlist ({watchlist.length})</span>
          <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
            {watchlistNames.length < 5 && (
              <button onClick={() => setShowNewListInput(true)} style={{
                background: 'transparent', border: 'none', color: 'var(--text-disabled)',
                cursor: 'pointer', padding: '2px', fontSize: '12px',
              }} title="New watchlist" aria-label="Create new watchlist"><Plus size={12} /></button>
            )}
            <button onClick={() => setCollapsed(true)} style={styles.collapseBtn} title="Collapse sidebar" aria-label="Collapse sidebar">
              <PanelLeftClose size={14} />
            </button>
          </div>
        </div>
        {/* Watchlist Tabs */}
        {watchlistNames.length > 1 && (
          <div style={{ display: 'flex', gap: '2px', padding: '0 8px 4px', flexWrap: 'wrap' }}>
            {watchlistNames.map(name => (
              <button key={name} onClick={() => setActiveWatchlist(name)} style={{
                padding: '2px 8px', border: 'none', borderRadius: '3px', cursor: 'pointer',
                fontSize: '10px', fontWeight: 600, fontFamily: 'var(--font-ui)',
                background: name === activeWatchlistName ? 'var(--text-accent)' : 'var(--bg-tertiary)',
                color: name === activeWatchlistName ? 'var(--text-primary)' : 'var(--text-disabled)',
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
        {watchlist.length > 3 && (
          <div style={{ padding: '0 8px 4px', display: 'flex', gap: '4px', alignItems: 'center' }}>
            {watchlist.length > 5 && (
              <div style={{ flex: 1, position: 'relative' }}>
                <Search size={12} style={{ position: 'absolute', left: '8px', top: '6px', color: 'var(--text-disabled)', pointerEvents: 'none' }} />
                <input
                  type="text"
                  value={watchlistFilter}
                  onChange={e => setWatchlistFilter(e.target.value)}
                  placeholder="Filter..."
                  style={{
                    width: '100%', height: '24px', background: 'var(--bg-input)',
                    border: '1px solid var(--border)', borderRadius: '3px',
                    color: 'var(--text-primary)', fontSize: '11px', padding: '0 6px 0 24px',
                    fontFamily: 'var(--font-mono)', boxSizing: 'border-box',
                  }}
                />
              </div>
            )}
            <div style={{ display: 'flex', gap: '1px', background: 'var(--bg-tertiary)', borderRadius: '3px', overflow: 'hidden', flexShrink: 0 }}>
              {([['default', 'All'], ['gainers', '▲'], ['losers', '▼']] as const).map(([key, label]) => (
                <button key={key} onClick={() => setWatchlistSort(key)} style={{
                  padding: '3px 6px', border: 'none', cursor: 'pointer', fontSize: '10px', fontWeight: 600,
                  fontFamily: 'var(--font-mono)',
                  background: watchlistSort === key ? 'var(--bg-primary)' : 'transparent',
                  color: watchlistSort === key ? (key === 'gainers' ? 'var(--green-primary)' : key === 'losers' ? 'var(--red-primary)' : 'var(--text-accent)') : 'var(--text-disabled)',
                }}>{label}</button>
              ))}
            </div>
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
            watchlist.filter(symbol => {
              const s = stocks.get(symbol);
              if (!s) return false;
              if (watchlistSort === 'gainers' && s.changePercent < 0) return false;
              if (watchlistSort === 'losers' && s.changePercent >= 0) return false;
              if (!watchlistFilter) return true;
              const q = watchlistFilter.toLowerCase();
              return symbol.toLowerCase().includes(q) || (s.name?.toLowerCase().includes(q) ?? false);
            }).sort((a, b) => {
              if (watchlistSort === 'default') return 0;
              const sa = stocks.get(a), sb = stocks.get(b);
              if (!sa || !sb) return 0;
              return watchlistSort === 'gainers' ? sb.changePercent - sa.changePercent : sa.changePercent - sb.changePercent;
            }).map((symbol) => {
              const stock = stocks.get(symbol);
              if (!stock) return null;
              const isSelected = selectedSymbol === symbol;
              return (
                <div
                  key={symbol}
                  className="stock-row"
                  data-selected={isSelected ? "true" : undefined}
                  style={{
                    ...styles.stockRow,
                    ...(isSelected ? styles.stockRowSelected : {}),
                  }}
                  onClick={() => selectStock(symbol)}
                >
                  <div style={{ flex: 1, minWidth: 0 }}>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
                      <span className="mono" style={styles.symbol}>{stock.symbol}</span>
                      {stock.traits.includes('ETF') && (
                        <span style={{ fontSize: '8px', color: 'var(--chart-purple)', fontWeight: 700, letterSpacing: '0.5px' }}>ETF</span>
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
                    <div className="watchlist-actions" style={{ display: 'flex', gap: '2px', opacity: 0 }}>
                      <button
                        onClick={(e) => { e.stopPropagation(); selectStock(symbol); setTimeout(() => window.dispatchEvent(new CustomEvent('tradingShortcut', { detail: 'Buy' })), 50); }}
                        style={{ background: 'none', border: '1px solid var(--green-primary)', borderRadius: '2px', color: 'var(--green-primary)', cursor: 'pointer', padding: '1px 4px', fontSize: '9px', fontWeight: 700, lineHeight: 1 }}
                        title="Quick Buy"
                      >B</button>
                      <button
                        onClick={(e) => { e.stopPropagation(); selectStock(symbol); setTimeout(() => window.dispatchEvent(new CustomEvent('tradingShortcut', { detail: 'Sell' })), 50); }}
                        style={{ background: 'none', border: '1px solid var(--red-primary)', borderRadius: '2px', color: 'var(--red-primary)', cursor: 'pointer', padding: '1px 4px', fontSize: '9px', fontWeight: 700, lineHeight: 1 }}
                        title="Quick Sell"
                      >S</button>
                    </div>
                    <button
                      onClick={(e) => { e.stopPropagation(); removeFromWatchlist(symbol); }}
                      style={styles.removeBtn}
                      title="Remove from watchlist"
                      aria-label={`Remove ${symbol} from watchlist`}
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
            sectorSummary.map(([name, data]) => {
              const maxChange = Math.max(...sectorSummary.map(([, d]) => Math.abs(d.avgChange)), 0.01);
              const barWidth = Math.min(Math.abs(data.avgChange) / maxChange * 100, 100);
              const barColor = data.avgChange >= 0 ? 'color-mix(in srgb, var(--green-primary) 15%, transparent)' : 'color-mix(in srgb, var(--red-primary) 15%, transparent)';
              return (
                <div key={name} style={{ ...styles.sectorRow, position: 'relative', overflow: 'hidden' }}>
                  <div style={{
                    position: 'absolute', top: 0, bottom: 0,
                    [data.avgChange >= 0 ? 'right' : 'left']: 0,
                    width: `${barWidth}%`, background: barColor, transition: 'width 0.5s',
                  }} />
                  <span style={{ ...styles.sectorName, position: 'relative', zIndex: 1 }}>{name}</span>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '6px', position: 'relative', zIndex: 1 }}>
                    <span style={styles.sectorCount}>{data.count}</span>
                    <span className="mono" style={{
                      fontSize: '12px', fontWeight: 600, minWidth: '52px', textAlign: 'right',
                      color: data.avgChange >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                    }}>
                      {data.avgChange >= 0 ? '+' : ''}{data.avgChange.toFixed(2)}%
                    </span>
                  </div>
                </div>
              );
            })
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
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  collapseBtn: {
    background: 'transparent',
    border: 'none',
    color: 'var(--text-disabled)',
    cursor: 'pointer',
    padding: '2px',
    borderRadius: '3px',
    display: 'flex',
    alignItems: 'center',
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
