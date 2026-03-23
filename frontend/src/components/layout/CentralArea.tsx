import { useState, useMemo } from 'react';
import { useMarketStore } from '@/stores/marketStore';
import { StockChart } from '@/components/charts/StockChart';
import { StockData } from '@/types/market';
import { WebSocketClient } from '@/services/websocket';
import { Plus, ArrowLeft, ChevronUp, ChevronDown } from 'lucide-react';

interface CentralAreaProps {
  wsClient: WebSocketClient;
}

export function CentralArea({ wsClient }: CentralAreaProps) {
  const activeTab = useMarketStore((s) => s.activeTab);
  const stockList = useMarketStore((s) => s.stockList);
  const isGameActive = useMarketStore((s) => s.isGameActive);
  const speed = useMarketStore((s) => s.speed);
  const selectStock = useMarketStore((s) => s.selectStock);
  const addToWatchlist = useMarketStore((s) => s.addToWatchlist);
  const watchlist = useMarketStore((s) => s.watchlist);
  const selectedSymbol = useMarketStore((s) => s.selectedSymbol);
  const showStockDetail = useMarketStore((s) => s.showStockDetail);
  const stocks = useMarketStore((s) => s.stocks);
  const ohlcvData = useMarketStore((s) => s.ohlcvData);
  const [sortField, setSortField] = useState<keyof StockData>('symbol');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('asc');
  const [filterText, setFilterText] = useState('');

  const sortedStocks = useMemo(() => {
    let list = [...stockList];
    if (filterText) {
      const q = filterText.toLowerCase();
      list = list.filter(s =>
        s.symbol.toLowerCase().includes(q) ||
        s.name.toLowerCase().includes(q) ||
        s.sector.toLowerCase().includes(q)
      );
    }
    list.sort((a, b) => {
      const aVal = a[sortField];
      const bVal = b[sortField];
      if (typeof aVal === 'string' && typeof bVal === 'string') {
        return sortDir === 'asc' ? aVal.localeCompare(bVal) : bVal.localeCompare(aVal);
      }
      if (typeof aVal === 'number' && typeof bVal === 'number') {
        return sortDir === 'asc' ? aVal - bVal : bVal - aVal;
      }
      return 0;
    });
    return list;
  }, [stockList, sortField, sortDir, filterText]);

  const handleSort = (field: keyof StockData) => {
    if (sortField === field) {
      setSortDir(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      setSortField(field);
      setSortDir(field === 'symbol' || field === 'name' ? 'asc' : 'desc');
    }
  };

  const SortIcon = ({ field }: { field: keyof StockData }) => {
    if (sortField !== field) return null;
    return sortDir === 'asc'
      ? <ChevronUp size={12} style={{ marginLeft: '2px' }} />
      : <ChevronDown size={12} style={{ marginLeft: '2px' }} />;
  };

  const portfolio = useMarketStore((s) => s.portfolio);
  const orders = useMarketStore((s) => s.orders);
  const newsItems = useMarketStore((s) => s.newsItems);

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

      {/* Stock Detail View (Bible 3.4.2) */}
      {showStockDetail && selectedSymbol && (() => {
        const stock = stocks.get(selectedSymbol);
        const chartData = ohlcvData.get(selectedSymbol) || [];
        if (!stock) return null;
        return (
          <div style={styles.content}>
            <button
              style={styles.backBtn}
              onClick={() => selectStock(null)}
            >
              <ArrowLeft size={16} /> Back
            </button>

            {/* Stock Header (Bible 3.4.2) */}
            <div style={styles.stockHeader}>
              <div style={styles.stockHeaderLeft}>
                <span className="mono" style={styles.detailSymbol}>{stock.symbol}</span>
                <span style={styles.detailName}>{stock.name}</span>
                <span style={styles.sectorBadge}>{stock.sector}</span>
              </div>
              <div style={styles.stockHeaderRight}>
                <span className="mono" style={styles.detailPrice}>${stock.price.toFixed(2)}</span>
                <span className="mono" style={{
                  ...styles.detailChange,
                  color: stock.changePercent >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                }}>
                  {stock.changePercent >= 0 ? '+' : ''}{stock.change.toFixed(2)} ({stock.changePercent >= 0 ? '+' : ''}{stock.changePercent.toFixed(2)}%)
                  {stock.changePercent >= 0 ? ' ▲' : ' ▼'}
                </span>
              </div>
            </div>

            {/* Candlestick Chart */}
            <StockChart
              symbol={stock.symbol}
              data={chartData}
              height={450}
            />

            {chartData.length === 0 && (
              <div style={styles.chartPlaceholder}>
                Waiting for chart data... Start the simulation (press Space)
              </div>
            )}
          </div>
        );
      })()}

      {/* Dashboard (only when no stock detail) */}
      {!showStockDetail && activeTab === 'dashboard' && (
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

      {!showStockDetail && activeTab === 'market' && (
        <div style={styles.content}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 'var(--space-3)' }}>
            <h2 style={{ ...styles.heading, marginBottom: 0 }}>
              Market ({filterText ? `${sortedStocks.length} / ` : ''}{stockList.length} stocks)
            </h2>
            <input
              type="text"
              value={filterText}
              onChange={(e) => setFilterText(e.target.value)}
              placeholder="Filter by symbol, name, sector..."
              style={{
                width: '280px',
                height: '32px',
                background: 'var(--bg-input)',
                color: 'var(--text-primary)',
                border: '1px solid var(--border)',
                borderRadius: '4px',
                padding: '0 10px',
                fontFamily: 'var(--font-ui)',
                fontSize: '13px',
              }}
            />
          </div>
          <div style={styles.tableContainer}>
            <table style={styles.table}>
              <thead>
                <tr>
                  <th style={{ ...styles.th, ...styles.sortableTh }} onClick={() => handleSort('symbol')}>Symbol<SortIcon field="symbol" /></th>
                  <th style={{ ...styles.th, ...styles.sortableTh }} onClick={() => handleSort('name')}>Name<SortIcon field="name" /></th>
                  <th style={{ ...styles.th, ...styles.sortableTh }} onClick={() => handleSort('sector')}>Sector<SortIcon field="sector" /></th>
                  <th style={{ ...styles.th, ...styles.sortableTh, textAlign: 'right' }} onClick={() => handleSort('price')}>Price<SortIcon field="price" /></th>
                  <th style={{ ...styles.th, ...styles.sortableTh, textAlign: 'right' }} onClick={() => handleSort('changePercent')}>Change<SortIcon field="changePercent" /></th>
                  <th style={{ ...styles.th, ...styles.sortableTh, textAlign: 'right' }} onClick={() => handleSort('volume')}>Volume<SortIcon field="volume" /></th>
                  <th style={{ ...styles.th, textAlign: 'center', width: '40px' }}></th>
                </tr>
              </thead>
              <tbody>
                {sortedStocks.map((s) => (
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

      {/* Portfolio Tab (Bible 6.1) */}
      {!showStockDetail && activeTab === 'portfolio' && (
        <div style={styles.content}>
          <h2 style={styles.heading}>Portfolio</h2>
          {portfolio ? (
            <>
              {/* Summary Cards */}
              <div style={styles.portfolioSummary}>
                <div style={styles.summaryCard}>
                  <span style={styles.summaryLabel}>Total Equity</span>
                  <span className="mono" style={styles.summaryValue}>${portfolio.totalEquity.toFixed(2)}</span>
                </div>
                <div style={styles.summaryCard}>
                  <span style={styles.summaryLabel}>Cash</span>
                  <span className="mono" style={styles.summaryValue}>${portfolio.cash.toFixed(2)}</span>
                </div>
                <div style={styles.summaryCard}>
                  <span style={styles.summaryLabel}>Portfolio Value</span>
                  <span className="mono" style={styles.summaryValue}>${portfolio.portfolioValue.toFixed(2)}</span>
                </div>
                <div style={styles.summaryCard}>
                  <span style={styles.summaryLabel}>Realized P&L</span>
                  <span className="mono" style={{
                    ...styles.summaryValue,
                    color: portfolio.realizedPnL >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                  }}>
                    {portfolio.realizedPnL >= 0 ? '+' : ''}${portfolio.realizedPnL.toFixed(2)}
                  </span>
                </div>
                <div style={styles.summaryCard}>
                  <span style={styles.summaryLabel}>Trades</span>
                  <span className="mono" style={styles.summaryValue}>{portfolio.tradeCount}</span>
                </div>
                <div style={styles.summaryCard}>
                  <span style={styles.summaryLabel}>Commissions</span>
                  <span className="mono" style={styles.summaryValue}>${portfolio.totalCommissions.toFixed(2)}</span>
                </div>
              </div>

              {/* Positions Table */}
              <h3 style={{ ...styles.heading, marginTop: '24px' }}>
                Positions ({portfolio.positions.length})
              </h3>
              {portfolio.positions.length > 0 ? (
                <div style={styles.tableContainer}>
                  <table style={styles.table}>
                    <thead>
                      <tr>
                        <th style={styles.th}>Symbol</th>
                        <th style={{ ...styles.th, textAlign: 'right' }}>Shares</th>
                        <th style={{ ...styles.th, textAlign: 'right' }}>Avg Cost</th>
                        <th style={{ ...styles.th, textAlign: 'right' }}>Market Value</th>
                        <th style={{ ...styles.th, textAlign: 'right' }}>P&L</th>
                        <th style={{ ...styles.th, textAlign: 'right' }}>P&L %</th>
                      </tr>
                    </thead>
                    <tbody>
                      {portfolio.positions.map((p) => (
                        <tr
                          key={p.symbol}
                          style={styles.tr}
                          onClick={() => selectStock(p.symbol)}
                          onMouseEnter={(e) => (e.currentTarget.style.background = 'var(--bg-tertiary)')}
                          onMouseLeave={(e) => (e.currentTarget.style.background = 'transparent')}
                        >
                          <td className="mono" style={{ ...styles.td, fontWeight: 700 }}>{p.symbol}</td>
                          <td className="mono" style={{ ...styles.td, textAlign: 'right' }}>{p.shares}</td>
                          <td className="mono" style={{ ...styles.td, textAlign: 'right' }}>${p.averageCost.toFixed(2)}</td>
                          <td className="mono" style={{ ...styles.td, textAlign: 'right' }}>${p.marketValue.toFixed(2)}</td>
                          <td className="mono" style={{
                            ...styles.td,
                            textAlign: 'right',
                            color: p.unrealizedPnL >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                          }}>
                            {p.unrealizedPnL >= 0 ? '+' : ''}${p.unrealizedPnL.toFixed(2)}
                          </td>
                          <td className="mono" style={{
                            ...styles.td,
                            textAlign: 'right',
                            color: p.unrealizedPnLPercent >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                          }}>
                            {p.unrealizedPnLPercent >= 0 ? '+' : ''}{p.unrealizedPnLPercent.toFixed(2)}%
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <div style={{ color: 'var(--text-disabled)', fontSize: '14px' }}>
                  No open positions. Select a stock and place a buy order to get started.
                </div>
              )}
            </>
          ) : (
            <div style={{ color: 'var(--text-disabled)', fontSize: '14px' }}>Loading portfolio...</div>
          )}
        </div>
      )}

      {/* Orders Tab (Bible 4.10) */}
      {!showStockDetail && activeTab === 'orders' && (
        <div style={styles.content}>
          <h2 style={styles.heading}>Orders</h2>
          {orders.length > 0 ? (
            <div style={styles.tableContainer}>
              <table style={styles.table}>
                <thead>
                  <tr>
                    <th style={styles.th}>ID</th>
                    <th style={styles.th}>Symbol</th>
                    <th style={styles.th}>Side</th>
                    <th style={styles.th}>Type</th>
                    <th style={{ ...styles.th, textAlign: 'right' }}>Qty</th>
                    <th style={{ ...styles.th, textAlign: 'right' }}>Limit</th>
                    <th style={{ ...styles.th, textAlign: 'right' }}>Fill Price</th>
                    <th style={styles.th}>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {[...orders].reverse().map((o) => (
                    <tr key={o.id} style={{ ...styles.tr, cursor: 'default' }}>
                      <td className="mono" style={{ ...styles.td, color: 'var(--text-secondary)' }}>#{o.id}</td>
                      <td className="mono" style={{ ...styles.td, fontWeight: 700 }}>{o.symbol}</td>
                      <td style={{
                        ...styles.td,
                        color: o.side === 'Buy' ? 'var(--green-primary)' : 'var(--red-primary)',
                        fontWeight: 600,
                      }}>{o.side}</td>
                      <td style={styles.td}>{o.type}</td>
                      <td className="mono" style={{ ...styles.td, textAlign: 'right' }}>{o.quantity}</td>
                      <td className="mono" style={{ ...styles.td, textAlign: 'right', color: 'var(--text-secondary)' }}>
                        {o.limitPrice ? `$${o.limitPrice.toFixed(2)}` : '-'}
                      </td>
                      <td className="mono" style={{ ...styles.td, textAlign: 'right' }}>
                        {o.fillPrice ? `$${o.fillPrice.toFixed(2)}` : '-'}
                      </td>
                      <td style={{
                        ...styles.td,
                        color: o.status === 'Filled' ? 'var(--green-primary)' :
                               o.status === 'Rejected' ? 'var(--red-primary)' :
                               o.status === 'Cancelled' ? 'var(--text-disabled)' :
                               'var(--text-accent)',
                      }}>{o.status}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : (
            <div style={{ color: 'var(--text-disabled)', fontSize: '14px' }}>
              No orders yet. Select a stock and place an order to get started.
            </div>
          )}
          <button
            style={{ ...styles.backBtn, marginTop: '12px' }}
            onClick={() => wsClient.send('GetOrders', {})}
          >
            Refresh Orders
          </button>
        </div>
      )}

      {/* News Tab (Bible 13) */}
      {!showStockDetail && activeTab === 'news' && (
        <div style={styles.content}>
          <h2 style={styles.heading}>News Feed</h2>
          {newsItems.length > 0 ? (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
              {newsItems.map((item) => {
                const color = item.sentiment > 0.1 ? 'var(--green-primary)' :
                              item.sentiment < -0.1 ? 'var(--red-primary)' : 'var(--text-secondary)';
                return (
                  <div
                    key={item.id}
                    style={{
                      background: 'var(--bg-secondary)',
                      border: '1px solid var(--border)',
                      borderRadius: '6px',
                      padding: '12px 16px',
                      cursor: item.affectedSymbols[0] ? 'pointer' : 'default',
                    }}
                    onClick={() => item.affectedSymbols[0] && selectStock(item.affectedSymbols[0])}
                  >
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '4px' }}>
                      <span style={{
                        fontSize: '10px',
                        fontWeight: 600,
                        padding: '2px 6px',
                        borderRadius: '3px',
                        background: item.severity === 'Major' ? 'var(--red-dim)' :
                                    item.severity === 'Moderate' ? 'rgba(245, 158, 11, 0.2)' : 'var(--bg-tertiary)',
                        color: item.severity === 'Major' ? 'var(--red-primary)' :
                               item.severity === 'Moderate' ? '#F59E0B' : 'var(--text-secondary)',
                      }}>
                        {item.type.toUpperCase()} - {item.severity.toUpperCase()}
                      </span>
                      <span className="mono" style={{ fontSize: '11px', color: 'var(--text-disabled)' }}>
                        {item.timestamp ? new Date(item.timestamp).toLocaleTimeString() : ''}
                      </span>
                    </div>
                    <div style={{ fontSize: '14px', color, fontFamily: 'var(--font-ui)' }}>
                      {item.headline}
                    </div>
                    {(item.affectedSymbols.length > 0 || item.affectedSectors.length > 0) && (
                      <div style={{ marginTop: '4px', fontSize: '11px', color: 'var(--text-disabled)' }}>
                        {item.affectedSymbols.length > 0 && `Stocks: ${item.affectedSymbols.join(', ')}`}
                        {item.affectedSectors.length > 0 && `Sectors: ${item.affectedSectors.join(', ')}`}
                        {' | '}Price impact: {item.priceEffect >= 0 ? '+' : ''}{(item.priceEffect * 100).toFixed(1)}%
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          ) : (
            <div style={{ color: 'var(--text-disabled)', fontSize: '14px' }}>
              No news yet. Start the simulation to see market events.
            </div>
          )}
        </div>
      )}

      {!showStockDetail && !['dashboard', 'market', 'portfolio', 'orders', 'news'].includes(activeTab) && (
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
  backBtn: {
    background: 'transparent',
    border: 'none',
    color: 'var(--text-accent)',
    cursor: 'pointer',
    display: 'flex',
    alignItems: 'center',
    gap: '4px',
    fontSize: '14px',
    fontFamily: 'var(--font-ui)',
    padding: '4px 0',
    marginBottom: 'var(--space-3)',
  },
  stockHeader: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: 'var(--space-4)',
    paddingBottom: 'var(--space-3)',
    borderBottom: '1px solid var(--border)',
  },
  stockHeaderLeft: {
    display: 'flex',
    flexDirection: 'column' as const,
    gap: '4px',
  },
  stockHeaderRight: {
    textAlign: 'right' as const,
  },
  detailSymbol: {
    fontSize: '24px',
    fontWeight: 700,
    color: 'var(--text-primary)',
  },
  detailName: {
    fontSize: '14px',
    color: 'var(--text-secondary)',
  },
  sectorBadge: {
    fontSize: '12px',
    color: 'var(--text-secondary)',
    background: 'var(--bg-tertiary)',
    padding: '2px 8px',
    borderRadius: '4px',
    width: 'fit-content',
  },
  detailPrice: {
    fontSize: '28px',
    fontWeight: 700,
    color: 'var(--text-primary)',
    display: 'block',
  },
  detailChange: {
    fontSize: '16px',
    display: 'block',
  },
  chartPlaceholder: {
    textAlign: 'center' as const,
    color: 'var(--text-disabled)',
    padding: '40px',
    fontSize: '14px',
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
  sortableTh: {
    cursor: 'pointer',
    userSelect: 'none' as const,
    display: 'table-cell',
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
  portfolioSummary: {
    display: 'grid',
    gridTemplateColumns: 'repeat(3, 1fr)',
    gap: '12px',
  },
  summaryCard: {
    background: 'var(--bg-secondary)',
    border: '1px solid var(--border)',
    borderRadius: '6px',
    padding: '12px 16px',
    display: 'flex',
    flexDirection: 'column' as const,
    gap: '4px',
  },
  summaryLabel: {
    fontSize: '11px',
    color: 'var(--text-secondary)',
    textTransform: 'uppercase' as const,
  },
  summaryValue: {
    fontSize: '18px',
    fontWeight: 700,
    color: 'var(--text-primary)',
  },
};
