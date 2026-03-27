import { useState, useMemo, useEffect } from 'react';
import { useMarketStore } from '@/stores/marketStore';
import { StockChart, ChartType } from '@/components/charts/StockChart';
import { Orderbook } from '@/components/charts/Orderbook';
import { StockScreener } from '@/components/trading/StockScreener';
import { StockData } from '@/types/market';
import { WebSocketClient } from '@/services/websocket';
import { Plus, ArrowLeft, ChevronUp, ChevronDown, Trophy, TrendingUp, TrendingDown, Target, Shield } from 'lucide-react';

interface CentralAreaProps {
  wsClient: WebSocketClient;
}

export function CentralArea({ wsClient }: CentralAreaProps) {
  const activeTab = useMarketStore((s) => s.activeTab);
  const setActiveTab = useMarketStore((s) => s.setActiveTab);
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
  const indicatorData = useMarketStore((s) => s.indicatorData);
  const orderbookData = useMarketStore((s) => s.orderbookData);
  const priceFlash = useMarketStore((s) => s.priceFlash);
  const shortSqueezeWarning = useMarketStore((s) => s.shortSqueezeWarning);
  const setShortSqueezeWarning = useMarketStore((s) => s.setShortSqueezeWarning);

  const getHeatColor = (pct: number) => {
    if (pct > 2) return '#059669';
    if (pct > 0.5) return '#10B981';
    if (pct > 0) return 'rgba(16, 185, 129, 0.4)';
    if (pct > -0.5) return 'rgba(239, 68, 68, 0.4)';
    if (pct > -2) return '#EF4444';
    return '#DC2626';
  };

  // Sector heatmap data (must be at top level — Rules of Hooks)
  const sectorData = useMemo(() => {
    const sectors = new Map<string, { count: number; totalChange: number; totalCap: number }>();
    stockList.forEach(s => {
      const existing = sectors.get(s.sector) || { count: 0, totalChange: 0, totalCap: 0 };
      existing.count++;
      existing.totalChange += s.changePercent;
      existing.totalCap += s.marketCap;
      sectors.set(s.sector, existing);
    });
    return Array.from(sectors.entries()).map(([name, data]) => ({
      name,
      avgChange: data.count > 0 ? data.totalChange / data.count : 0,
      count: data.count,
      totalCap: data.totalCap,
    })).sort((a, b) => b.totalCap - a.totalCap);
  }, [stockList]);
  const [sortField, setSortField] = useState<keyof StockData>('symbol');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('asc');
  const [filterText, setFilterText] = useState('');
  const [newsFilter, setNewsFilter] = useState<string>('all');
  const [expandedNewsId, setExpandedNewsId] = useState<number | null>(null);
  const [marketRowsVisible, setMarketRowsVisible] = useState(50); // Virtualization: show 50, load more on scroll
  const [chartTimeframe, setChartTimeframe] = useState<string>('ALL');
  const [chartType, setChartType] = useState<ChartType>('candle');

  // Re-fetch OHLCV when timeframe changes
  useEffect(() => {
    if (selectedSymbol && showStockDetail) {
      wsClient.send('GetOHLCV', { symbol: selectedSymbol, timeframe: chartTimeframe });
    }
  }, [chartTimeframe, selectedSymbol]);

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
  const analyticsData = useMarketStore((s) => s.analyticsData);
  const achievements = useMarketStore((s) => s.achievements);
  const tradeJournal = useMarketStore((s) => s.tradeJournal);
  const stockFundamentals = useMarketStore((s) => s.stockFundamentals);
  const economicData = useMarketStore((s) => s.economicData);
  const earningsCalendar = useMarketStore((s) => s.earningsCalendar);

  const [taxSummary, setTaxSummary] = useState<Record<string, number> | null>(null);

  // Listen for tax summary data
  useEffect(() => {
    const handler = (e: Event) => setTaxSummary((e as CustomEvent).detail as Record<string, number>);
    window.addEventListener('taxSummary', handler);
    return () => window.removeEventListener('taxSummary', handler);
  }, []);

  // Auto-fetch data when tab is selected
  useEffect(() => {
    if (activeTab === 'analytics') {
      wsClient.send('GetAnalytics', {});
      wsClient.send('GetAchievements', {});
      wsClient.send('GetTaxSummary', {});
    }
    if (activeTab === 'journal') {
      wsClient.send('GetTradeJournal', {});
    }
    if (activeTab === 'dashboard') {
      wsClient.send('GetEconomicData', {});
      wsClient.send('GetEarningsCalendar', {});
    }
  }, [activeTab]);

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
        const indicators = indicatorData.get(selectedSymbol);
        if (!stock) return null;
        return (
          <div style={styles.content}>
            <button
              style={styles.backBtn}
              onClick={() => selectStock(null)}
            >
              <ArrowLeft size={16} /> Back
            </button>

            {/* Short Squeeze Warning Banner (Bible 4.4.5) */}
            {shortSqueezeWarning && shortSqueezeWarning.symbol === stock.symbol && (
              <div style={{
                background: 'rgba(245,158,11,0.15)', border: '1px solid rgba(245,158,11,0.4)',
                borderRadius: '6px', padding: '10px 14px', marginBottom: '8px',
                display: 'flex', justifyContent: 'space-between', alignItems: 'center',
              }}>
                <div>
                  <span style={{ fontWeight: 700, color: '#F59E0B', fontSize: '13px' }}>
                    ⚠ SHORT SQUEEZE WARNING
                  </span>
                  <span style={{ color: 'var(--text-secondary)', fontSize: '12px', marginLeft: '8px' }}>
                    Short Interest {shortSqueezeWarning.shortInterestPercent.toFixed(1)}% | Price surged +{shortSqueezeWarning.priceChangePercent.toFixed(1)}% in last hour
                  </span>
                  {shortSqueezeWarning.playerHasShortPosition && (
                    <div style={{ color: '#EF4444', fontSize: '12px', fontWeight: 600, marginTop: '4px' }}>
                      You have a short position in this stock. Consider covering.
                    </div>
                  )}
                </div>
                <button
                  onClick={() => setShortSqueezeWarning(null)}
                  style={{ background: 'none', border: 'none', color: 'var(--text-disabled)', cursor: 'pointer', fontSize: '16px' }}
                >×</button>
              </div>
            )}

            {/* Stock Header (Bible 3.4.2) */}
            <div style={styles.stockHeader}>
              <div style={styles.stockHeaderLeft}>
                <span className="mono" style={styles.detailSymbol}>{stock.symbol}</span>
                <span style={styles.detailName}>{stock.name}</span>
                <span style={styles.sectorBadge}>{stock.sector}</span>
                {stock.isSSR && (
                  <span style={{
                    fontSize: '9px', fontWeight: 700, padding: '1px 5px', borderRadius: '3px',
                    background: 'rgba(245,158,11,0.15)', color: '#F59E0B',
                    border: '1px solid rgba(245,158,11,0.3)',
                  }}>SSR</span>
                )}
                {stock.traits && stock.traits.length > 0 && (
                  <div style={{ display: 'flex', gap: '4px', flexWrap: 'wrap', marginTop: '4px' }}>
                    {stock.traits.map(t => (
                      <span key={t} style={{
                        fontSize: '10px', color: 'var(--text-accent)',
                        background: 'rgba(96, 165, 250, 0.1)',
                        padding: '1px 6px', borderRadius: '3px',
                      }}>{t}</span>
                    ))}
                  </div>
                )}
                <span className="mono" style={{ fontSize: '11px', color: 'var(--text-disabled)', marginTop: '2px' }}>
                  Vol: {stock.volume >= 1_000_000 ? `${(stock.volume / 1_000_000).toFixed(1)}M` :
                        stock.volume >= 1_000 ? `${(stock.volume / 1_000).toFixed(1)}K` : stock.volume}
                  {' | '}MCap: ${stock.marketCap >= 1_000_000_000 ? `${(stock.marketCap / 1_000_000_000).toFixed(1)}B` :
                               stock.marketCap >= 1_000_000 ? `${(stock.marketCap / 1_000_000).toFixed(0)}M` : stock.marketCap.toFixed(0)}
                </span>
              </div>
              <div style={styles.stockHeaderRight}>
                <span className="mono" style={styles.detailPrice}>${stock.price.toFixed(2)}</span>
                <span className={`mono ${stock.changePercent >= 0 ? 'glow-green' : 'glow-red'}`} style={{
                  ...styles.detailChange,
                  color: stock.changePercent >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                }}>
                  {stock.changePercent >= 0 ? '+' : ''}{stock.change.toFixed(2)} ({stock.changePercent >= 0 ? '+' : ''}{stock.changePercent.toFixed(2)}%)
                  {stock.changePercent >= 0 ? ' ▲' : ' ▼'}
                </span>
                <span className="mono" style={{ fontSize: '12px', color: 'var(--text-secondary)', display: 'block', marginTop: '4px' }}>
                  Bid: <span style={{ color: 'var(--green-primary)' }}>${stock.bid.toFixed(2)}</span>
                  {' '} Ask: <span style={{ color: 'var(--red-primary)' }}>${stock.ask.toFixed(2)}</span>
                  {' '} Spread: ${(stock.ask - stock.bid).toFixed(2)}
                </span>
              </div>
            </div>

            {/* Chart Toolbar: Type + Timeframe + Indicator Legend (Bible 12.2.2) */}
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
              <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                {/* Chart Type Buttons */}
                <div style={{ display: 'flex', gap: '2px' }}>
                  {(['candle', 'line', 'area'] as ChartType[]).map(ct => (
                    <button key={ct} onClick={() => setChartType(ct)} style={{
                      padding: '3px 8px', border: 'none', borderRadius: '3px', cursor: 'pointer',
                      fontSize: '10px', fontWeight: 600, fontFamily: 'var(--font-ui)',
                      background: chartType === ct ? 'var(--text-accent)' : 'var(--bg-tertiary)',
                      color: chartType === ct ? '#FFF' : 'var(--text-secondary)',
                    }}>{ct === 'candle' ? 'Candle' : ct === 'line' ? 'Line' : 'Area'}</button>
                  ))}
                </div>
                <div style={{ width: '1px', height: '16px', background: 'var(--border)' }} />
                {/* Timeframe Buttons */}
                <div style={{ display: 'flex', gap: '2px' }}>
                {['1D', '1W', '1M', '3M', '1Y', 'ALL'].map(tf => (
                  <button key={tf} onClick={() => setChartTimeframe(tf)} style={{
                    padding: '3px 10px', border: 'none', borderRadius: '3px', cursor: 'pointer',
                    fontSize: '11px', fontWeight: 600, fontFamily: 'var(--font-mono)',
                    background: chartTimeframe === tf ? 'var(--text-accent)' : 'var(--bg-tertiary)',
                    color: chartTimeframe === tf ? '#FFF' : 'var(--text-secondary)',
                  }}>{tf}</button>
                ))}
                </div>
              </div>
              {indicators && (
                <div style={{ display: 'flex', gap: '12px', fontSize: '11px' }}>
                  {indicators.sma20 && <span style={{ color: '#F59E0B' }}>-- SMA 20</span>}
                  {indicators.sma50 && <span style={{ color: '#8B5CF6' }}>-- SMA 50</span>}
                  {indicators.sma200 && <span style={{ color: '#EC4899' }}>-- SMA 200</span>}
                  {indicators.bollingerUpper && <span style={{ color: '#60A5FA' }}>-- Bollinger</span>}
                </div>
              )}
            </div>

            <StockChart
              symbol={stock.symbol}
              data={chartData}
              indicators={indicators}
              chartType={chartType}
              height={450}
            />

            {chartData.length === 0 && (
              <div style={{ height: '450px', display: 'flex', flexDirection: 'column', gap: '8px', padding: '20px' }}>
                <div className="skeleton" style={{ height: '20px', width: '40%' }} />
                <div className="skeleton" style={{ flex: 1, width: '100%' }} />
                <div className="skeleton" style={{ height: '16px', width: '60%' }} />
                <div style={{ textAlign: 'center', marginTop: '8px', color: 'var(--text-disabled)', fontSize: '12px' }}>
                  Waiting for chart data... Start the simulation (press Space)
                </div>
              </div>
            )}

            {/* Peer Comparison (same sector) */}
            {stock && (() => {
              const peers = stockList
                .filter(s => s.sector === stock.sector && s.symbol !== stock.symbol)
                .sort((a, b) => b.marketCap - a.marketCap)
                .slice(0, 5);
              if (peers.length === 0) return null;
              return (
                <div style={{ marginTop: '16px' }}>
                  <h3 style={{ ...styles.heading, marginBottom: '8px' }}>Peer Comparison ({stock.sector})</h3>
                  <div style={styles.tableContainer}>
                    <table style={styles.table}>
                      <thead>
                        <tr>
                          <th style={styles.th}>Symbol</th>
                          <th style={{ ...styles.th, textAlign: 'right' }}>Price</th>
                          <th style={{ ...styles.th, textAlign: 'right' }}>Change</th>
                          <th style={{ ...styles.th, textAlign: 'right' }}>Mkt Cap</th>
                          <th style={{ ...styles.th, textAlign: 'right' }}>P/E</th>
                          <th style={{ ...styles.th, textAlign: 'right' }}>Div Yield</th>
                        </tr>
                      </thead>
                      <tbody>
                        {/* Current stock row highlighted */}
                        <tr style={{ ...styles.tr, background: 'rgba(96,165,250,0.05)' }}>
                          <td className="mono" style={{ ...styles.td, fontWeight: 700, color: 'var(--text-accent)' }}>{stock.symbol}</td>
                          <td className="mono" style={{ ...styles.td, textAlign: 'right' }}>${stock.price.toFixed(2)}</td>
                          <td className={`mono ${stock.changePercent >= 0 ? 'positive' : 'negative'}`} style={{ ...styles.td, textAlign: 'right' }}>
                            {stock.changePercent >= 0 ? '+' : ''}{stock.changePercent.toFixed(2)}%
                          </td>
                          <td className="mono" style={{ ...styles.td, textAlign: 'right', color: 'var(--text-secondary)' }}>
                            {stock.marketCap >= 1e9 ? `$${(stock.marketCap / 1e9).toFixed(1)}B` : `$${(stock.marketCap / 1e6).toFixed(0)}M`}
                          </td>
                          <td className="mono" style={{ ...styles.td, textAlign: 'right' }}>{stock.peRatio && stock.peRatio > 0 ? stock.peRatio.toFixed(1) : 'N/A'}</td>
                          <td className="mono" style={{ ...styles.td, textAlign: 'right' }}>{stock.dividendYield && stock.dividendYield > 0 ? `${(stock.dividendYield * 100).toFixed(1)}%` : '-'}</td>
                        </tr>
                        {peers.map(p => (
                          <tr key={p.symbol} style={styles.tr} onClick={() => selectStock(p.symbol)}
                            onMouseEnter={e => e.currentTarget.style.background = 'var(--bg-tertiary)'}
                            onMouseLeave={e => e.currentTarget.style.background = 'transparent'}>
                            <td className="mono" style={{ ...styles.td, fontWeight: 700 }}>{p.symbol}</td>
                            <td className="mono" style={{ ...styles.td, textAlign: 'right' }}>${p.price.toFixed(2)}</td>
                            <td className={`mono ${p.changePercent >= 0 ? 'positive' : 'negative'}`} style={{ ...styles.td, textAlign: 'right' }}>
                              {p.changePercent >= 0 ? '+' : ''}{p.changePercent.toFixed(2)}%
                            </td>
                            <td className="mono" style={{ ...styles.td, textAlign: 'right', color: 'var(--text-secondary)' }}>
                              {p.marketCap >= 1e9 ? `$${(p.marketCap / 1e9).toFixed(1)}B` : `$${(p.marketCap / 1e6).toFixed(0)}M`}
                            </td>
                            <td className="mono" style={{ ...styles.td, textAlign: 'right' }}>{p.peRatio && p.peRatio > 0 ? p.peRatio.toFixed(1) : 'N/A'}</td>
                            <td className="mono" style={{ ...styles.td, textAlign: 'right' }}>{p.dividendYield && p.dividendYield > 0 ? `${(p.dividendYield * 100).toFixed(1)}%` : '-'}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              );
            })()}

            {/* Fundamentals Panel */}
            {stockFundamentals && stockFundamentals.symbol === selectedSymbol && (
              <div style={{ marginTop: '16px' }}>
                <h3 style={{ ...styles.heading, marginBottom: '8px' }}>Fundamentals</h3>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '8px' }}>
                  {[
                    ['P/E Ratio', stockFundamentals.peRatio > 0 ? stockFundamentals.peRatio.toFixed(1) : 'N/A'],
                    ['Market Cap', stockFundamentals.marketCap >= 1e9 ? `$${(stockFundamentals.marketCap / 1e9).toFixed(1)}B` : `$${(stockFundamentals.marketCap / 1e6).toFixed(0)}M`],
                    ['Div Yield', stockFundamentals.dividendYield > 0 ? `${(stockFundamentals.dividendYield * 100).toFixed(2)}%` : '-'],
                    ['Revenue Growth', `${(stockFundamentals.revenueGrowth * 100).toFixed(1)}%`],
                    ['Debt/Equity', stockFundamentals.debtToEquity.toFixed(2)],
                    ['Fair Value', `$${stockFundamentals.fairValue.toFixed(2)}`],
                    ['Avg Volume', stockFundamentals.averageVolume >= 1e6 ? `${(stockFundamentals.averageVolume / 1e6).toFixed(1)}M` : `${(stockFundamentals.averageVolume / 1e3).toFixed(0)}K`],
                    ['Liquidity', `${stockFundamentals.liquidityScore}/10`],
                    ['Day Range', `$${stockFundamentals.dayLow.toFixed(2)} - $${stockFundamentals.dayHigh.toFixed(2)}`],
                    ['Volatility', `${(stockFundamentals.baseVolatility * 100).toFixed(1)}%`],
                    ['Insider Own', `${(stockFundamentals.insiderOwnership * 100).toFixed(1)}%`],
                    ['Inst. Own', `${(stockFundamentals.institutionalOwnership * 100).toFixed(1)}%`],
                    ['Analyst', stockFundamentals.analystConsensus],
                    ['Target Price', `$${stockFundamentals.targetPrice.toFixed(2)}`],
                    ['Rating', `${stockFundamentals.analystRating.toFixed(1)}/5.0`],
                    ['Upside', `${(((stockFundamentals.targetPrice - stock.price) / stock.price) * 100).toFixed(1)}%`],
                  ].map(([label, value]) => (
                    <div key={label} style={{
                      background: 'var(--bg-secondary)', border: '1px solid var(--border)',
                      borderRadius: '4px', padding: '8px 10px',
                    }}>
                      <span style={{ fontSize: '10px', color: 'var(--text-disabled)', textTransform: 'uppercase', display: 'block' }}>{label}</span>
                      <span className="mono" style={{ fontSize: '13px', fontWeight: 600, color: 'var(--text-primary)' }}>{value}</span>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Company Profile (Session 12) */}
            {stock.personality && (
              <div style={{ marginTop: '16px' }}>
                <h3 style={{ ...styles.heading, marginBottom: '8px' }}>Company Profile</h3>
                <div style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border)', borderRadius: '6px', padding: '12px' }}>
                  <p style={{ fontSize: '13px', color: 'var(--text-secondary)', margin: '0 0 10px 0', lineHeight: '1.5' }}>
                    {stock.personality.description}
                  </p>
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px' }}>
                    {[
                      ['CEO', `${stock.personality.ceoName} (${stock.personality.ceoArchetype})`],
                      ['HQ', stock.personality.headquarters],
                      ['Founded', String(stock.personality.foundedYear)],
                      ['Flagship', stock.personality.flagshipProduct],
                      ...(stock.personality.secondaryProduct ? [['Also', stock.personality.secondaryProduct]] : []),
                      ...(stock.personality.rivalSymbol ? [['Rival', stock.personality.rivalSymbol]] : []),
                    ].map(([label, value]) => (
                      <div key={label} style={{ display: 'flex', gap: '6px', fontSize: '12px' }}>
                        <span style={{ color: 'var(--text-disabled)', minWidth: '60px' }}>{label}</span>
                        <span style={{ color: 'var(--text-primary)' }}>{value}</span>
                      </div>
                    ))}
                  </div>
                  <p style={{ fontSize: '11px', color: 'var(--text-disabled)', margin: '10px 0 0 0', fontStyle: 'italic', lineHeight: '1.4' }}>
                    {stock.personality.foundingStory}
                  </p>
                </div>
              </div>
            )}

            {/* Orderbook (Bible 12.3) */}
            {orderbookData && orderbookData.symbol === selectedSymbol && (
              <div style={{ marginTop: '16px' }}>
                <h3 style={{ ...styles.heading, marginBottom: '8px' }}>Order Book</h3>
                <Orderbook
                  bids={orderbookData.bids}
                  asks={orderbookData.asks}
                  bestBid={orderbookData.bestBid}
                  bestAsk={orderbookData.bestAsk}
                  spread={orderbookData.spread}
                  spreadPercent={orderbookData.spreadPercent}
                />
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

            {/* Market Overview Stats */}
            {stockList.length > 0 && (() => {
              const advancing = stockList.filter(s => s.changePercent > 0).length;
              const declining = stockList.filter(s => s.changePercent < 0).length;
              const unchanged = stockList.length - advancing - declining;
              const avgChange = stockList.reduce((a, s) => a + s.changePercent, 0) / stockList.length;
              const totalVolume = stockList.reduce((a, s) => a + s.volume, 0);

              return (
                <div style={{ display: 'flex', gap: '12px', marginBottom: '16px' }}>
                  <div style={{ ...styles.summaryCard, flex: 1 }}>
                    <span style={styles.summaryLabel}>Market Index</span>
                    <span className="mono" style={{
                      ...styles.summaryValue, fontSize: '16px',
                      color: avgChange >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                    }}>
                      {avgChange >= 0 ? '+' : ''}{avgChange.toFixed(2)}%
                    </span>
                  </div>
                  <div style={{ ...styles.summaryCard, flex: 1 }}>
                    <span style={styles.summaryLabel}>Advancing / Declining</span>
                    <span style={styles.summaryValue}>
                      <span style={{ color: 'var(--green-primary)' }}>{advancing}</span>
                      {' / '}
                      <span style={{ color: 'var(--red-primary)' }}>{declining}</span>
                      {unchanged > 0 && <span style={{ color: 'var(--text-disabled)' }}> ({unchanged})</span>}
                    </span>
                  </div>
                  <div style={{ ...styles.summaryCard, flex: 1 }}>
                    <span style={styles.summaryLabel}>Total Volume</span>
                    <span className="mono" style={styles.summaryValue}>
                      {totalVolume >= 1e9 ? `${(totalVolume / 1e9).toFixed(1)}B` :
                       totalVolume >= 1e6 ? `${(totalVolume / 1e6).toFixed(1)}M` :
                       totalVolume >= 1e3 ? `${(totalVolume / 1e3).toFixed(0)}K` : totalVolume}
                    </span>
                  </div>
                </div>
              );
            })()}

            {/* Sector Heatmap (Bible 12.4) */}
            <h3 style={styles.moversTitle}>Sector Heatmap</h3>
            <div style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(4, 1fr)',
              gap: '4px',
              marginBottom: '24px',
            }}>
              {sectorData.map(sector => (
                <div
                  key={sector.name}
                  onClick={() => { setFilterText(sector.name); setActiveTab('market'); }}
                  style={{
                    background: getHeatColor(sector.avgChange),
                    borderRadius: '4px',
                    padding: '12px 8px',
                    cursor: 'pointer',
                    textAlign: 'center',
                    minHeight: '60px',
                    display: 'flex',
                    flexDirection: 'column',
                    justifyContent: 'center',
                    transition: 'opacity 150ms',
                  }}
                  onMouseEnter={e => e.currentTarget.style.opacity = '0.8'}
                  onMouseLeave={e => e.currentTarget.style.opacity = '1'}
                >
                  <div style={{ fontSize: '11px', fontWeight: 700, color: '#FFF', marginBottom: '2px' }}>
                    {sector.name}
                  </div>
                  <div className="mono" style={{ fontSize: '14px', fontWeight: 700, color: '#FFF' }}>
                    {sector.avgChange >= 0 ? '+' : ''}{sector.avgChange.toFixed(2)}%
                  </div>
                  <div style={{ fontSize: '9px', color: 'rgba(255,255,255,0.6)' }}>
                    {sector.count} stocks
                  </div>
                </div>
              ))}
            </div>

            {/* Top Movers + Market Breadth side by side */}
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '16px', marginBottom: '20px' }}>
              <div>
                <h3 style={{ ...styles.moversTitle, color: 'var(--green-primary)' }}>Top Gainers</h3>
                {[...stockList]
                  .sort((a, b) => b.changePercent - a.changePercent)
                  .slice(0, 5)
                  .map((s) => (
                    <div key={s.symbol} style={{ ...styles.moverRow, cursor: 'pointer' }} onClick={() => selectStock(s.symbol)}>
                      <span className="mono" style={styles.moverSymbol}>{s.symbol}</span>
                      <span style={styles.moverName}>{s.name}</span>
                      <span className="mono positive">{s.changePercent >= 0 ? '+' : ''}{s.changePercent.toFixed(2)}%</span>
                    </div>
                  ))}
              </div>
              <div>
                <h3 style={{ ...styles.moversTitle, color: 'var(--red-primary)' }}>Top Losers</h3>
                {[...stockList]
                  .sort((a, b) => a.changePercent - b.changePercent)
                  .slice(0, 5)
                  .map((s) => (
                    <div key={s.symbol} style={{ ...styles.moverRow, cursor: 'pointer' }} onClick={() => selectStock(s.symbol)}>
                      <span className="mono" style={styles.moverSymbol}>{s.symbol}</span>
                      <span style={styles.moverName}>{s.name}</span>
                      <span className="mono negative">{s.changePercent.toFixed(2)}%</span>
                    </div>
                  ))}
              </div>
              <div>
                <h3 style={styles.moversTitle}>Market Breadth</h3>
                {(() => {
                  const aboveSMA = stockList.filter(s => s.price > (s.previousClose || s.price) * 0.98).length;
                  const breadth = stockList.length > 0 ? (aboveSMA / stockList.length * 100) : 50;
                  const highVol = [...stockList].sort((a, b) => b.volume - a.volume).slice(0, 5);
                  return (
                    <>
                      <div style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border)', borderRadius: '6px', padding: '8px 12px', marginBottom: '8px' }}>
                        <span style={{ fontSize: '10px', color: 'var(--text-disabled)', display: 'block' }}>BREADTH</span>
                        <div style={{ display: 'flex', height: '6px', borderRadius: '3px', overflow: 'hidden', margin: '4px 0', background: 'var(--bg-tertiary)' }}>
                          <div style={{ width: `${breadth}%`, background: 'var(--green-primary)', transition: 'width 300ms' }} />
                        </div>
                        <span className="mono" style={{ fontSize: '11px', color: 'var(--text-secondary)' }}>{breadth.toFixed(0)}% positive</span>
                      </div>
                      <div style={{ fontSize: '11px', color: 'var(--text-disabled)', marginBottom: '4px' }}>HIGH VOLUME</div>
                      {highVol.map(s => (
                        <div key={s.symbol} style={{ ...styles.moverRow, cursor: 'pointer' }} onClick={() => selectStock(s.symbol)}>
                          <span className="mono" style={styles.moverSymbol}>{s.symbol}</span>
                          <span className="mono" style={{ fontSize: '11px', color: 'var(--text-secondary)' }}>
                            {s.volume >= 1e6 ? `${(s.volume / 1e6).toFixed(1)}M` : `${(s.volume / 1e3).toFixed(0)}K`}
                          </span>
                        </div>
                      ))}
                    </>
                  );
                })()}
              </div>
            </div>

            {/* Market Phase + Yield Curve */}
            {economicData && (
              <div style={{ display: 'flex', gap: '12px', marginBottom: '16px' }}>
                <div style={{
                  flex: 1, background: 'var(--bg-secondary)', border: '1px solid var(--border)',
                  borderRadius: '6px', padding: '12px', textAlign: 'center',
                }}>
                  <span style={{ fontSize: '10px', color: 'var(--text-disabled)', letterSpacing: '1px', display: 'block' }}>MARKET PHASE</span>
                  <span className="mono" style={{
                    fontSize: '20px', fontWeight: 700, display: 'block', marginTop: '4px',
                    color: economicData.marketSentiment > 0.2 ? 'var(--green-primary)' :
                           economicData.marketSentiment < -0.2 ? 'var(--red-primary)' : '#F59E0B',
                  }}>
                    {economicData.marketSentiment > 0.3 ? 'BULL MARKET' :
                     economicData.marketSentiment > 0.1 ? 'BULLISH' :
                     economicData.marketSentiment > -0.1 ? 'NEUTRAL' :
                     economicData.marketSentiment > -0.3 ? 'BEARISH' : 'BEAR MARKET'}
                  </span>
                  <span className="mono" style={{ fontSize: '11px', color: 'var(--text-disabled)' }}>
                    Sentiment: {economicData.marketSentiment >= 0 ? '+' : ''}{economicData.marketSentiment.toFixed(2)}
                  </span>
                </div>
                <div style={{
                  flex: 1, background: 'var(--bg-secondary)', border: '1px solid var(--border)',
                  borderRadius: '6px', padding: '12px', textAlign: 'center',
                }}>
                  <span style={{ fontSize: '10px', color: 'var(--text-disabled)', letterSpacing: '1px', display: 'block' }}>YIELD CURVE</span>
                  {(() => {
                    const y2 = economicData.indicators.interestRate;
                    const y10 = economicData.indicators.treasuryYield10Y;
                    const spread = y10 - y2;
                    const inverted = spread < 0;
                    return (
                      <>
                        <div style={{ display: 'flex', justifyContent: 'center', gap: '20px', margin: '8px 0' }}>
                          <div>
                            <span style={{ fontSize: '9px', color: 'var(--text-disabled)' }}>2Y</span>
                            <span className="mono" style={{ display: 'block', fontSize: '16px', fontWeight: 700, color: 'var(--text-primary)' }}>
                              {y2.toFixed(2)}%
                            </span>
                          </div>
                          <div style={{ display: 'flex', alignItems: 'center', color: inverted ? 'var(--red-primary)' : 'var(--green-primary)' }}>
                            <span style={{ fontSize: '18px' }}>{inverted ? '!' : '-'}</span>
                          </div>
                          <div>
                            <span style={{ fontSize: '9px', color: 'var(--text-disabled)' }}>10Y</span>
                            <span className="mono" style={{ display: 'block', fontSize: '16px', fontWeight: 700, color: 'var(--text-primary)' }}>
                              {y10.toFixed(2)}%
                            </span>
                          </div>
                        </div>
                        <span className="mono" style={{
                          fontSize: '11px', fontWeight: 600,
                          color: inverted ? 'var(--red-primary)' : 'var(--green-primary)',
                        }}>
                          Spread: {spread >= 0 ? '+' : ''}{spread.toFixed(2)}%
                          {inverted && ' INVERTED'}
                        </span>
                      </>
                    );
                  })()}
                </div>
                <div style={{
                  flex: 1, background: 'var(--bg-secondary)', border: '1px solid var(--border)',
                  borderRadius: '6px', padding: '12px', textAlign: 'center',
                }}>
                  <span style={{ fontSize: '10px', color: 'var(--text-disabled)', letterSpacing: '1px', display: 'block' }}>COMMODITIES</span>
                  <div style={{ display: 'flex', justifyContent: 'center', gap: '16px', marginTop: '8px' }}>
                    <div>
                      <span style={{ fontSize: '9px', color: 'var(--text-disabled)' }}>OIL</span>
                      <span className="mono" style={{ display: 'block', fontSize: '14px', fontWeight: 700, color: 'var(--text-primary)' }}>
                        ${economicData.indicators.oilPrice.toFixed(0)}
                      </span>
                    </div>
                    <div>
                      <span style={{ fontSize: '9px', color: 'var(--text-disabled)' }}>GOLD</span>
                      <span className="mono" style={{ display: 'block', fontSize: '14px', fontWeight: 700, color: '#D4AF37' }}>
                        ${economicData.indicators.goldPrice.toFixed(0)}
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            )}

            {/* Economic Indicators + Fear & Greed + Earnings Calendar */}
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px' }}>
              {/* Economic Indicators */}
              {economicData && (
                <div>
                  <h3 style={styles.moversTitle}>Economic Indicators</h3>
                  <div style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border)', borderRadius: '6px', overflow: 'hidden' }}>
                    {[
                      ['Fed Rate', `${economicData.indicators.interestRate.toFixed(2)}%`],
                      ['Inflation', `${economicData.indicators.inflationRate.toFixed(2)}%`],
                      ['Unemployment', `${economicData.indicators.unemploymentRate.toFixed(1)}%`],
                      ['GDP Growth', `${economicData.indicators.gdpGrowth.toFixed(2)}%`],
                      ['Consumer Conf.', economicData.indicators.consumerConfidence.toFixed(1)],
                      ['10Y Treasury', `${economicData.indicators.treasuryYield10Y.toFixed(2)}%`],
                      ['Oil (WTI)', `$${economicData.indicators.oilPrice.toFixed(2)}`],
                      ['Gold', `$${economicData.indicators.goldPrice.toFixed(0)}`],
                      ['Mfg PMI', economicData.indicators.manufacturingPMI.toFixed(1)],
                    ].map(([label, value], i) => (
                      <div key={label} style={{
                        display: 'flex', justifyContent: 'space-between', padding: '5px 10px',
                        borderBottom: i < 8 ? '1px solid rgba(31,41,55,0.3)' : 'none',
                        fontSize: '12px',
                      }}>
                        <span style={{ color: 'var(--text-secondary)' }}>{label}</span>
                        <span className="mono" style={{ fontWeight: 600, color: 'var(--text-primary)' }}>{value}</span>
                      </div>
                    ))}
                  </div>

                  {/* Fear & Greed */}
                  <div style={{
                    marginTop: '8px', background: 'var(--bg-secondary)', border: '1px solid var(--border)',
                    borderRadius: '6px', padding: '10px 12px', textAlign: 'center',
                  }}>
                    <span style={{ fontSize: '10px', color: 'var(--text-disabled)', letterSpacing: '1px' }}>FEAR & GREED INDEX</span>
                    <div className="mono" style={{
                      fontSize: '32px', fontWeight: 700, marginTop: '4px',
                      color: economicData.fearGreedIndex > 60 ? 'var(--green-primary)' :
                             economicData.fearGreedIndex < 40 ? 'var(--red-primary)' : '#F59E0B',
                    }}>{economicData.fearGreedIndex}</div>
                    <div style={{
                      fontSize: '11px', fontWeight: 600,
                      color: economicData.fearGreedIndex > 75 ? 'var(--green-primary)' :
                             economicData.fearGreedIndex > 60 ? '#34D399' :
                             economicData.fearGreedIndex > 40 ? '#F59E0B' :
                             economicData.fearGreedIndex > 25 ? '#F97316' : 'var(--red-primary)',
                    }}>
                      {economicData.fearGreedIndex > 75 ? 'EXTREME GREED' :
                       economicData.fearGreedIndex > 60 ? 'GREED' :
                       economicData.fearGreedIndex > 40 ? 'NEUTRAL' :
                       economicData.fearGreedIndex > 25 ? 'FEAR' : 'EXTREME FEAR'}
                    </div>
                  </div>
                </div>
              )}

              {/* Upcoming Events + Earnings Calendar */}
              <div>
                {/* Economic Events */}
                {economicData && economicData.upcomingEvents.length > 0 && (
                  <div style={{ marginBottom: '12px' }}>
                    <h3 style={styles.moversTitle}>Economic Calendar</h3>
                    <div style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border)', borderRadius: '6px', overflow: 'hidden' }}>
                      {economicData.upcomingEvents.slice(0, 6).map((ev, i) => (
                        <div key={ev.id} style={{
                          display: 'flex', justifyContent: 'space-between', alignItems: 'center',
                          padding: '6px 10px', fontSize: '11px',
                          borderBottom: i < 5 ? '1px solid rgba(31,41,55,0.3)' : 'none',
                        }}>
                          <div>
                            <span style={{ fontWeight: 600, color: 'var(--text-primary)' }}>{ev.name}</span>
                            <span style={{ color: 'var(--text-disabled)', marginLeft: '6px' }}>
                              {new Date(ev.scheduledDate).toLocaleDateString()}
                            </span>
                          </div>
                          <span style={{
                            fontSize: '9px', fontWeight: 600, padding: '1px 6px', borderRadius: '3px',
                            background: ev.impact === 'High' ? 'rgba(239,68,68,0.2)' : 'rgba(245,158,11,0.2)',
                            color: ev.impact === 'High' ? '#EF4444' : '#F59E0B',
                          }}>{ev.impact}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {/* Earnings Calendar */}
                {earningsCalendar && (
                  <div>
                    <h3 style={styles.moversTitle}>Earnings Calendar</h3>
                    <div style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border)', borderRadius: '6px', overflow: 'hidden' }}>
                      {earningsCalendar.upcoming.length > 0 ? (
                        earningsCalendar.upcoming.slice(0, 8).map((e, i) => (
                          <div key={`${e.symbol}-${e.reportDate}`} style={{
                            display: 'flex', justifyContent: 'space-between', alignItems: 'center',
                            padding: '5px 10px', fontSize: '11px', cursor: 'pointer',
                            borderBottom: i < 7 ? '1px solid rgba(31,41,55,0.3)' : 'none',
                          }} onClick={() => selectStock(e.symbol)}>
                            <span className="mono" style={{ fontWeight: 700, color: 'var(--text-primary)', width: '50px' }}>{e.symbol}</span>
                            <span style={{ color: 'var(--text-secondary)' }}>Q{e.quarter}</span>
                            <span className="mono" style={{ color: 'var(--text-secondary)' }}>
                              Est: ${e.expectedEPS.toFixed(2)}
                            </span>
                            <span style={{ color: 'var(--text-disabled)' }}>
                              {new Date(e.reportDate).toLocaleDateString()}
                            </span>
                          </div>
                        ))
                      ) : (
                        <div style={{ padding: '12px', textAlign: 'center', color: 'var(--text-disabled)', fontSize: '12px' }}>
                          No upcoming earnings
                        </div>
                      )}
                    </div>
                    {earningsCalendar.recent.length > 0 && (
                      <div style={{ marginTop: '8px' }}>
                        <span style={{ fontSize: '10px', color: 'var(--text-disabled)', letterSpacing: '1px' }}>RECENT RESULTS</span>
                        <div style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border)', borderRadius: '6px', overflow: 'hidden', marginTop: '4px' }}>
                          {earningsCalendar.recent.slice(0, 4).map((e, i) => (
                            <div key={`${e.symbol}-recent`} style={{
                              display: 'flex', justifyContent: 'space-between', alignItems: 'center',
                              padding: '5px 10px', fontSize: '11px', cursor: 'pointer',
                              borderBottom: i < 3 ? '1px solid rgba(31,41,55,0.3)' : 'none',
                            }} onClick={() => selectStock(e.symbol)}>
                              <span className="mono" style={{ fontWeight: 700, width: '50px' }}>{e.symbol}</span>
                              <span className="mono" style={{
                                color: e.beat ? 'var(--green-primary)' : 'var(--red-primary)', fontWeight: 600,
                              }}>{e.beat ? 'BEAT' : 'MISS'} {e.surprisePercent >= 0 ? '+' : ''}{e.surprisePercent.toFixed(1)}%</span>
                              <span className="mono" style={{
                                color: e.priceImpact >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                              }}>{e.priceImpact >= 0 ? '+' : ''}{e.priceImpact.toFixed(1)}%</span>
                            </div>
                          ))}
                        </div>
                      </div>
                    )}
                  </div>
                )}
              </div>
            </div>
          </div>
      )}

      {!showStockDetail && activeTab === 'market' && (
        <div style={styles.content}>
          {/* Stock Screener */}
          <StockScreener
            stocks={stockList}
            onApplyFilter={setFilterText}
            onSelectStock={selectStock}
          />

          {/* Sector Detail Banner (when filtered by sector) */}
          {filterText && sectorData.find(s => s.name.toLowerCase() === filterText.toLowerCase()) && (() => {
            const sector = sectorData.find(s => s.name.toLowerCase() === filterText.toLowerCase())!;
            const sectorStocks = stockList.filter(s => s.sector.toLowerCase() === filterText.toLowerCase());
            const bestStock = [...sectorStocks].sort((a, b) => b.changePercent - a.changePercent)[0];
            const worstStock = [...sectorStocks].sort((a, b) => a.changePercent - b.changePercent)[0];
            const avgPE = sectorStocks.filter(s => (s.peRatio ?? 0) > 0).reduce((s, st) => s + (st.peRatio ?? 0), 0) / Math.max(1, sectorStocks.filter(s => (s.peRatio ?? 0) > 0).length);
            const totalMcap = sectorStocks.reduce((s, st) => s + st.marketCap, 0);
            // Find sector ETF
            const etfMap: Record<string, string> = { Technology: 'STEC', Energy: 'SENG', Financials: 'SFIN', Healthcare: 'SHLT', 'Consumer Goods': 'SCON', Industrials: 'SIND', Materials: 'SMAT', 'Real Estate': 'SREL', Telecommunications: 'STEL', Utilities: 'SUTL', 'Luxury Goods': 'SLUX', Transportation: 'STRN' };
            const etfSymbol = etfMap[sector.name];
            return (
              <div style={{
                background: 'var(--bg-secondary)', border: '1px solid var(--border)',
                borderRadius: '8px', padding: '12px 16px', marginBottom: '12px',
              }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
                  <h3 style={{ fontSize: '16px', fontWeight: 700, color: 'var(--text-primary)', margin: 0 }}>{sector.name}</h3>
                  <div style={{ display: 'flex', gap: '8px' }}>
                    <span className="mono" style={{
                      fontSize: '14px', fontWeight: 700,
                      color: sector.avgChange >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                    }}>{sector.avgChange >= 0 ? '+' : ''}{sector.avgChange.toFixed(2)}%</span>
                    {etfSymbol && (
                      <button onClick={() => selectStock(etfSymbol)} style={{
                        background: 'rgba(96,165,250,0.1)', border: '1px solid rgba(96,165,250,0.3)',
                        borderRadius: '4px', padding: '2px 8px', cursor: 'pointer',
                        fontSize: '11px', fontWeight: 700, color: 'var(--text-accent)', fontFamily: 'var(--font-mono)',
                      }}>{etfSymbol}</button>
                    )}
                  </div>
                </div>
                <div style={{ display: 'flex', gap: '16px', fontSize: '11px', color: 'var(--text-secondary)' }}>
                  <span>{sector.count} stocks</span>
                  <span>Mkt Cap: ${totalMcap >= 1e12 ? `${(totalMcap / 1e12).toFixed(1)}T` : `${(totalMcap / 1e9).toFixed(0)}B`}</span>
                  <span>Avg P/E: {avgPE > 0 ? avgPE.toFixed(1) : 'N/A'}</span>
                  {bestStock && <span>Best: <span className="mono" style={{ color: 'var(--green-primary)' }}>{bestStock.symbol} +{bestStock.changePercent.toFixed(1)}%</span></span>}
                  {worstStock && <span>Worst: <span className="mono" style={{ color: 'var(--red-primary)' }}>{worstStock.symbol} {worstStock.changePercent.toFixed(1)}%</span></span>}
                </div>
              </div>
            );
          })()}

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
                {sortedStocks.length === 0 && (
                  <tr><td colSpan={7} style={{ padding: '20px', textAlign: 'center', color: 'var(--text-disabled)' }}>
                    No stocks match your filter.
                  </td></tr>
                )}
                {sortedStocks.slice(0, marketRowsVisible).map((s) => (
                  <tr
                    key={s.symbol}
                    style={styles.tr}
                    onClick={() => selectStock(s.symbol)}
                    onMouseEnter={(e) => (e.currentTarget.style.background = 'var(--bg-tertiary)')}
                    onMouseLeave={(e) => (e.currentTarget.style.background = 'transparent')}
                  >
                    <td className="mono" style={{ ...styles.td, fontWeight: 700 }}>
                      {s.symbol}
                      {s.isSSR && <span style={{
                        fontSize: '8px', fontWeight: 700, marginLeft: '4px', padding: '0 3px',
                        borderRadius: '2px', background: 'rgba(245,158,11,0.15)', color: '#F59E0B',
                        verticalAlign: 'super',
                      }}>SSR</span>}
                    </td>
                    <td style={styles.td}>{s.name}</td>
                    <td style={{ ...styles.td, color: 'var(--text-secondary)', fontSize: '12px' }}>{s.sector}</td>
                    <td key={`${s.symbol}-${s.price}`}
                      className={`mono ${priceFlash.get(s.symbol) === 'up' ? 'price-up' : priceFlash.get(s.symbol) === 'down' ? 'price-down' : ''}`}
                      style={{ ...styles.td, textAlign: 'right' }}>${s.price.toFixed(2)}</td>
                    <td className={`mono ${s.changePercent >= 0 ? 'positive' : 'negative'}`} style={{
                      ...styles.td, textAlign: 'right',
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
            {sortedStocks.length > marketRowsVisible && (
              <div style={{ textAlign: 'center', padding: '12px' }}>
                <button
                  onClick={() => setMarketRowsVisible(v => Math.min(v + 50, sortedStocks.length))}
                  style={{
                    padding: '8px 24px', border: '1px solid var(--border)', borderRadius: '4px',
                    background: 'var(--bg-tertiary)', color: 'var(--text-secondary)',
                    cursor: 'pointer', fontSize: '12px', fontFamily: 'var(--font-ui)',
                  }}
                >
                  Show More ({sortedStocks.length - marketRowsVisible} remaining)
                </button>
              </div>
            )}
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
                {portfolio.marginEnabled && (
                  <>
                    <div style={{ ...styles.summaryCard, borderLeft: `3px solid ${(portfolio.marginUsedPercent ?? 0) > 75 ? 'var(--red-primary)' : 'var(--text-accent)'}` }}>
                      <span style={styles.summaryLabel}>Margin Used</span>
                      <span className="mono" style={{
                        ...styles.summaryValue, fontSize: '16px',
                        color: (portfolio.marginUsedPercent ?? 0) > 75 ? 'var(--red-primary)' : 'var(--text-primary)',
                      }}>{(portfolio.marginUsedPercent ?? 0).toFixed(1)}%</span>
                    </div>
                    <div style={styles.summaryCard}>
                      <span style={styles.summaryLabel}>Margin Balance</span>
                      <span className="mono" style={{ ...styles.summaryValue, fontSize: '16px', color: (portfolio.marginBalance ?? 0) > 0 ? 'var(--red-primary)' : 'var(--text-primary)' }}>
                        ${(portfolio.marginBalance ?? 0).toFixed(0)}
                      </span>
                    </div>
                    <div style={styles.summaryCard}>
                      <span style={styles.summaryLabel}>Buying Power</span>
                      <span className="mono" style={{ ...styles.summaryValue, fontSize: '16px', color: 'var(--green-primary)' }}>
                        ${(portfolio.buyingPower ?? portfolio.cash).toFixed(0)}
                      </span>
                    </div>
                  </>
                )}
              </div>

              {/* Positions Table */}
              {/* Portfolio Allocation Visual */}
              {portfolio.positions.length > 0 && (
                <div style={{ marginTop: '20px', marginBottom: '8px' }}>
                  <h3 style={{ ...styles.heading, marginBottom: '8px' }}>Allocation</h3>
                  <div style={{ display: 'flex', height: '24px', borderRadius: '6px', overflow: 'hidden', gap: '2px' }}>
                    {/* Cash segment */}
                    <div
                      style={{
                        flex: portfolio.cash / portfolio.totalEquity,
                        background: 'var(--text-accent)',
                        display: 'flex', alignItems: 'center', justifyContent: 'center',
                      }}
                      title={`Cash: $${portfolio.cash.toFixed(0)} (${(portfolio.cash / portfolio.totalEquity * 100).toFixed(0)}%)`}
                    >
                      {portfolio.cash / portfolio.totalEquity > 0.08 && (
                        <span style={{ fontSize: '9px', color: '#FFF', fontWeight: 700 }}>Cash</span>
                      )}
                    </div>
                    {/* Position segments */}
                    {portfolio.positions.map((p, i) => {
                      const pct = Math.abs(p.marketValue) / portfolio.totalEquity;
                      const colors = ['#10B981', '#8B5CF6', '#F59E0B', '#EC4899', '#06B6D4', '#EF4444', '#60A5FA', '#14B8A6'];
                      return (
                        <div
                          key={p.symbol}
                          style={{
                            flex: pct,
                            background: colors[i % colors.length],
                            display: 'flex', alignItems: 'center', justifyContent: 'center',
                          }}
                          title={`${p.symbol}: $${Math.abs(p.marketValue).toFixed(0)} (${(pct * 100).toFixed(0)}%)`}
                        >
                          {pct > 0.06 && (
                            <span style={{ fontSize: '9px', color: '#FFF', fontWeight: 700 }}>{p.symbol}</span>
                          )}
                        </div>
                      );
                    })}
                  </div>
                </div>
              )}

              {/* Sector Allocation Pie Chart */}
              {portfolio.positions.length > 1 && (() => {
                const sectorMap = new Map<string, number>();
                portfolio.positions.forEach(p => {
                  const s = stocks.get(p.symbol);
                  const sector = s?.sector || 'Other';
                  sectorMap.set(sector, (sectorMap.get(sector) || 0) + Math.abs(p.marketValue));
                });
                const total = Array.from(sectorMap.values()).reduce((a, b) => a + b, 0);
                if (total === 0) return null;
                const sectors = Array.from(sectorMap.entries()).sort((a, b) => b[1] - a[1]);
                const sectorColors: Record<string, string> = {
                  Technology: '#3B82F6', Energy: '#F59E0B', Financials: '#10B981',
                  Healthcare: '#EC4899', 'Consumer Goods': '#8B5CF6', Industrials: '#6B7280',
                  Materials: '#D97706', 'Real Estate': '#14B8A6', Telecommunications: '#06B6D4',
                  Utilities: '#84CC16', 'Luxury Goods': '#F43F5E', Transportation: '#A78BFA',
                };
                let cumulativeAngle = 0;
                const slices = sectors.map(([name, value]) => {
                  const pct = value / total;
                  const startAngle = cumulativeAngle;
                  cumulativeAngle += pct * 360;
                  return { name, value, pct, startAngle, endAngle: cumulativeAngle, color: sectorColors[name] || '#6B7280' };
                });
                const describeArc = (startAngle: number, endAngle: number, r: number) => {
                  const start = polarToCartesian(50, 50, r, endAngle);
                  const end = polarToCartesian(50, 50, r, startAngle);
                  const largeArc = endAngle - startAngle > 180 ? 1 : 0;
                  return `M ${start.x} ${start.y} A ${r} ${r} 0 ${largeArc} 0 ${end.x} ${end.y}`;
                };
                const polarToCartesian = (cx: number, cy: number, r: number, angleDeg: number) => {
                  const rad = (angleDeg - 90) * Math.PI / 180;
                  return { x: cx + r * Math.cos(rad), y: cy + r * Math.sin(rad) };
                };
                return (
                  <div style={{ display: 'flex', gap: '16px', marginBottom: '16px', alignItems: 'center' }}>
                    <div style={{ width: '120px', height: '120px', flexShrink: 0 }}>
                      <svg viewBox="0 0 100 100">
                        {slices.map((sl) => (
                          sl.pct > 0.005 && (
                            <path key={sl.name} d={describeArc(sl.startAngle, sl.endAngle - 0.5, 40)}
                              fill="none" stroke={sl.color} strokeWidth="18" />
                          )
                        ))}
                        <text x="50" y="48" textAnchor="middle" fill="var(--text-primary)" fontSize="12" fontWeight="700" fontFamily="var(--font-mono)">
                          {sectors.length}
                        </text>
                        <text x="50" y="58" textAnchor="middle" fill="var(--text-disabled)" fontSize="7">
                          sectors
                        </text>
                      </svg>
                    </div>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '3px', flex: 1 }}>
                      {slices.slice(0, 6).map(sl => (
                        <div key={sl.name} style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '11px' }}>
                          <div style={{ width: '8px', height: '8px', borderRadius: '2px', background: sl.color, flexShrink: 0 }} />
                          <span style={{ color: 'var(--text-secondary)', flex: 1 }}>{sl.name}</span>
                          <span className="mono" style={{ color: 'var(--text-primary)', fontWeight: 600 }}>{(sl.pct * 100).toFixed(0)}%</span>
                        </div>
                      ))}
                      {slices.length > 6 && (
                        <span style={{ fontSize: '10px', color: 'var(--text-disabled)' }}>+{slices.length - 6} more</span>
                      )}
                    </div>
                  </div>
                );
              })()}

              {/* Positions P&L Heatmap (Treemap-style) */}
              {portfolio.positions.length > 0 && (
                <div style={{ marginBottom: '16px' }}>
                  <h3 style={{ ...styles.heading, marginBottom: '8px' }}>P&L Heatmap</h3>
                  <div style={{
                    display: 'flex', flexWrap: 'wrap', gap: '2px', height: '100px',
                    borderRadius: '6px', overflow: 'hidden',
                  }}>
                    {[...portfolio.positions]
                      .sort((a, b) => Math.abs(b.marketValue) - Math.abs(a.marketValue))
                      .map(p => {
                        const totalVal = portfolio.positions.reduce((s, pos) => s + Math.abs(pos.marketValue), 0);
                        const pct = totalVal > 0 ? Math.abs(p.marketValue) / totalVal : 0;
                        const pnlPct = p.unrealizedPnLPercent;
                        const bg = pnlPct > 5 ? '#059669' :
                                   pnlPct > 2 ? '#10B981' :
                                   pnlPct > 0 ? 'rgba(16,185,129,0.5)' :
                                   pnlPct > -2 ? 'rgba(239,68,68,0.5)' :
                                   pnlPct > -5 ? '#EF4444' : '#DC2626';
                        return (
                          <div key={p.symbol} onClick={() => selectStock(p.symbol)} style={{
                            flex: `${pct * 100} 0 0`, minWidth: '40px', height: '100%',
                            background: bg, cursor: 'pointer',
                            display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center',
                            transition: 'opacity 150ms',
                          }}
                            onMouseEnter={e => e.currentTarget.style.opacity = '0.8'}
                            onMouseLeave={e => e.currentTarget.style.opacity = '1'}
                            title={`${p.symbol}: ${pnlPct >= 0 ? '+' : ''}${pnlPct.toFixed(1)}% ($${p.unrealizedPnL.toFixed(0)})`}
                          >
                            {pct > 0.06 && (
                              <>
                                <span className="mono" style={{ fontSize: '11px', fontWeight: 700, color: '#FFF' }}>{p.symbol}</span>
                                <span className="mono" style={{ fontSize: '10px', color: 'rgba(255,255,255,0.8)' }}>
                                  {pnlPct >= 0 ? '+' : ''}{pnlPct.toFixed(1)}%
                                </span>
                              </>
                            )}
                          </div>
                        );
                      })}
                  </div>
                </div>
              )}

              {/* Risk Metrics */}
              {portfolio.positions.length > 0 && (
                <div style={{ marginTop: '16px', marginBottom: '16px' }}>
                  <h3 style={{ ...styles.heading, marginBottom: '8px' }}>Risk Analysis</h3>
                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '8px' }}>
                    {(() => {
                      const totalValue = portfolio.totalEquity;
                      const posValue = portfolio.portfolioValue;
                      const cashPct = totalValue > 0 ? (portfolio.cash / totalValue * 100) : 100;
                      const investedPct = totalValue > 0 ? (posValue / totalValue * 100) : 0;
                      const longExposure = portfolio.positions.filter(p => p.shares > 0).reduce((s, p) => s + Math.abs(p.marketValue), 0);
                      const shortExposure = portfolio.positions.filter(p => p.shares < 0).reduce((s, p) => s + Math.abs(p.marketValue), 0);
                      const netExposure = longExposure - shortExposure;
                      const largestPos = portfolio.positions.reduce((max, p) => Math.abs(p.marketValue) > Math.abs(max.marketValue) ? p : max, portfolio.positions[0]);
                      const concentration = totalValue > 0 ? (Math.abs(largestPos.marketValue) / totalValue * 100) : 0;
                      const sectors = new Set(portfolio.positions.map(p => {
                        const stock = stocks.get(p.symbol);
                        return stock?.sector || 'Unknown';
                      }));
                      // Approximate 1-day VaR (95%) assuming 2% daily volatility
                      const var95 = posValue * 0.02 * 1.65; // 1.65 = 95% z-score

                      return (
                        <>
                          <div style={{ ...styles.summaryCard, borderLeft: '3px solid var(--text-accent)' }}>
                            <span style={styles.summaryLabel}>Invested</span>
                            <span className="mono" style={{ fontSize: '14px', fontWeight: 700, color: 'var(--text-primary)' }}>{investedPct.toFixed(0)}%</span>
                            <span className="mono" style={{ fontSize: '10px', color: 'var(--text-disabled)' }}>Cash: {cashPct.toFixed(0)}%</span>
                          </div>
                          <div style={{ ...styles.summaryCard, borderLeft: '3px solid #8B5CF6' }}>
                            <span style={styles.summaryLabel}>Net Exposure</span>
                            <span className="mono" style={{ fontSize: '14px', fontWeight: 700, color: netExposure >= 0 ? 'var(--green-primary)' : 'var(--red-primary)' }}>
                              ${Math.abs(netExposure).toFixed(0)}
                            </span>
                            <span className="mono" style={{ fontSize: '10px', color: 'var(--text-disabled)' }}>
                              L:${longExposure.toFixed(0)} S:${shortExposure.toFixed(0)}
                            </span>
                          </div>
                          <div style={{ ...styles.summaryCard, borderLeft: '3px solid #F59E0B' }}>
                            <span style={styles.summaryLabel}>VaR (95% 1d)</span>
                            <span className="mono" style={{ fontSize: '14px', fontWeight: 700, color: 'var(--red-primary)' }}>
                              -${var95.toFixed(0)}
                            </span>
                            <span className="mono" style={{ fontSize: '10px', color: 'var(--text-disabled)' }}>
                              {(var95 / totalValue * 100).toFixed(1)}% of equity
                            </span>
                          </div>
                          <div style={{ ...styles.summaryCard, borderLeft: concentration > 30 ? '3px solid var(--red-primary)' : '3px solid var(--green-primary)' }}>
                            <span style={styles.summaryLabel}>Concentration</span>
                            <span className="mono" style={{ fontSize: '14px', fontWeight: 700, color: concentration > 30 ? 'var(--red-primary)' : 'var(--text-primary)' }}>
                              {concentration.toFixed(0)}%
                            </span>
                            <span className="mono" style={{ fontSize: '10px', color: 'var(--text-disabled)' }}>
                              {largestPos.symbol} | {sectors.size} sectors
                            </span>
                          </div>
                        </>
                      );
                    })()}
                  </div>
                </div>
              )}

              <h3 style={{ ...styles.heading, marginTop: '16px' }}>
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

              {/* Dividend Income Section */}
              {portfolio.positions.length > 0 && (() => {
                const divPositions = portfolio.positions
                  .map(p => {
                    const s = stocks.get(p.symbol);
                    const divYield = s?.dividendYield ?? 0;
                    const annualIncome = Math.abs(p.marketValue) * divYield;
                    const quarterlyIncome = annualIncome / 4;
                    return { ...p, divYield, annualIncome, quarterlyIncome, stock: s };
                  })
                  .filter(p => p.divYield > 0)
                  .sort((a, b) => b.annualIncome - a.annualIncome);

                if (divPositions.length === 0) return null;
                const totalAnnual = divPositions.reduce((s, p) => s + p.annualIncome, 0);
                const totalQuarterly = totalAnnual / 4;

                return (
                  <div style={{ marginTop: '20px' }}>
                    <h3 style={{ ...styles.heading, marginBottom: '8px' }}>
                      Dividend Income
                    </h3>
                    <div style={{ display: 'flex', gap: '10px', marginBottom: '10px' }}>
                      <div style={{ ...styles.summaryCard, flex: 1 }}>
                        <span style={styles.summaryLabel}>Annual Income</span>
                        <span className="mono" style={{ fontSize: '16px', fontWeight: 700, color: 'var(--green-primary)' }}>
                          ${totalAnnual.toFixed(0)}
                        </span>
                      </div>
                      <div style={{ ...styles.summaryCard, flex: 1 }}>
                        <span style={styles.summaryLabel}>Quarterly</span>
                        <span className="mono" style={{ fontSize: '16px', fontWeight: 700, color: 'var(--green-primary)' }}>
                          ${totalQuarterly.toFixed(0)}
                        </span>
                      </div>
                      <div style={{ ...styles.summaryCard, flex: 1 }}>
                        <span style={styles.summaryLabel}>Yield on Portfolio</span>
                        <span className="mono" style={{ fontSize: '16px', fontWeight: 700, color: 'var(--text-primary)' }}>
                          {portfolio.totalEquity > 0 ? (totalAnnual / portfolio.totalEquity * 100).toFixed(2) : '0'}%
                        </span>
                      </div>
                    </div>
                    <div style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border)', borderRadius: '6px', overflow: 'hidden' }}>
                      {divPositions.map((p, i) => (
                        <div key={p.symbol} style={{
                          display: 'flex', justifyContent: 'space-between', alignItems: 'center',
                          padding: '6px 12px', fontSize: '12px', cursor: 'pointer',
                          borderBottom: i < divPositions.length - 1 ? '1px solid rgba(31,41,55,0.3)' : 'none',
                        }} onClick={() => selectStock(p.symbol)}>
                          <span className="mono" style={{ fontWeight: 700, width: '60px' }}>{p.symbol}</span>
                          <span className="mono" style={{ color: 'var(--text-secondary)' }}>
                            Yield: {(p.divYield * 100).toFixed(2)}%
                          </span>
                          <span className="mono" style={{ color: 'var(--green-primary)', fontWeight: 600 }}>
                            ${p.quarterlyIncome.toFixed(0)}/qtr
                          </span>
                          <span className="mono" style={{ color: 'var(--green-primary)' }}>
                            ${p.annualIncome.toFixed(0)}/yr
                          </span>
                        </div>
                      ))}
                    </div>
                  </div>
                );
              })()}
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
                    <th style={{ ...styles.th, width: '60px' }}></th>
                  </tr>
                </thead>
                <tbody>
                  {[...orders].reverse().map((o) => (
                    <tr key={o.id} style={{ ...styles.tr, cursor: 'default' }}>
                      <td className="mono" style={{ ...styles.td, color: 'var(--text-secondary)' }}>#{o.id}</td>
                      <td className="mono" style={{ ...styles.td, fontWeight: 700 }}>{o.symbol}</td>
                      <td style={{
                        ...styles.td,
                        color: o.side === 'Buy' || o.side === 'Cover' ? 'var(--green-primary)' : 'var(--red-primary)',
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
                               o.status === 'Expired' ? 'var(--text-disabled)' :
                               'var(--text-accent)',
                      }}>{o.status}</td>
                      <td style={{ ...styles.td, textAlign: 'center' }}>
                        {(o.status === 'Pending' || o.status === 'Open') && (
                          <button
                            onClick={() => { wsClient.send('CancelOrder', { orderId: o.id }); setTimeout(() => wsClient.send('GetOrders', {}), 200); }}
                            style={{
                              background: 'transparent', border: '1px solid var(--red-primary)',
                              color: 'var(--red-primary)', borderRadius: '4px', padding: '2px 8px',
                              fontSize: '10px', cursor: 'pointer', fontWeight: 600,
                            }}
                          >Cancel</button>
                        )}
                      </td>
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
          <div style={{ display: 'flex', gap: '8px', marginTop: '12px' }}>
            <button style={styles.backBtn} onClick={() => wsClient.send('GetOrders', {})}>
              Refresh Orders
            </button>
          </div>
        </div>
      )}

      {/* News Tab (Bible 13) */}
      {!showStockDetail && activeTab === 'news' && (
        <div style={styles.content}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '12px' }}>
            <h2 style={{ ...styles.heading, marginBottom: 0 }}>News Feed ({newsItems.length})</h2>
            <div style={{ display: 'flex', gap: '4px' }}>
              {['all', 'Macro', 'Sector', 'Company', 'Rumor'].map(f => (
                <button key={f} onClick={() => setNewsFilter(f)} style={{
                  padding: '4px 10px', border: 'none', borderRadius: '4px', cursor: 'pointer',
                  fontSize: '11px', fontWeight: 600, fontFamily: 'var(--font-ui)',
                  background: newsFilter === f ? 'var(--text-accent)' : 'var(--bg-tertiary)',
                  color: newsFilter === f ? '#FFF' : 'var(--text-secondary)',
                }}>{f === 'all' ? 'All' : f}</button>
              ))}
              <button onClick={() => setNewsFilter(newsFilter === 'bullish' ? 'all' : 'bullish')} style={{
                padding: '4px 10px', border: 'none', borderRadius: '4px', cursor: 'pointer',
                fontSize: '11px', fontWeight: 600,
                background: newsFilter === 'bullish' ? 'var(--green-primary)' : 'var(--bg-tertiary)',
                color: newsFilter === 'bullish' ? '#FFF' : 'var(--green-primary)',
              }}>Bullish</button>
              <button onClick={() => setNewsFilter(newsFilter === 'bearish' ? 'all' : 'bearish')} style={{
                padding: '4px 10px', border: 'none', borderRadius: '4px', cursor: 'pointer',
                fontSize: '11px', fontWeight: 600,
                background: newsFilter === 'bearish' ? 'var(--red-primary)' : 'var(--bg-tertiary)',
                color: newsFilter === 'bearish' ? '#FFF' : 'var(--red-primary)',
              }}>Bearish</button>
            </div>
          </div>
          {(() => {
            const filtered = newsItems.filter(item => {
              if (newsFilter === 'all') return true;
              if (newsFilter === 'bullish') return item.sentiment > 0.1;
              if (newsFilter === 'bearish') return item.sentiment < -0.1;
              return item.type === newsFilter;
            });
            return filtered.length > 0 ? (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
                {filtered.map((item) => {
                  const isRumor = item.type === 'Rumor';
                  const color = isRumor ? 'var(--text-accent)' :
                                item.sentiment > 0.1 ? 'var(--green-primary)' :
                                item.sentiment < -0.1 ? 'var(--red-primary)' : 'var(--text-secondary)';
                  return (
                    <div key={item.id} style={{
                      background: isRumor ? 'rgba(96,165,250,0.05)' : 'var(--bg-secondary)',
                      border: `1px solid ${isRumor ? 'rgba(96,165,250,0.2)' : 'var(--border)'}`,
                      borderRadius: '6px', padding: '10px 14px',
                      cursor: item.affectedSymbols[0] ? 'pointer' : 'default',
                      borderLeft: `3px solid ${color}`,
                    }} onClick={() => setExpandedNewsId(expandedNewsId === item.id ? null : item.id)}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '3px' }}>
                        <div style={{ display: 'flex', gap: '6px', alignItems: 'center' }}>
                          <span style={{
                            fontSize: '9px', fontWeight: 600, padding: '1px 5px', borderRadius: '3px',
                            background: isRumor ? 'rgba(96,165,250,0.15)' :
                                        item.severity === 'Major' ? 'var(--red-dim)' : item.severity === 'Moderate' ? 'rgba(245,158,11,0.2)' : 'var(--bg-tertiary)',
                            color: isRumor ? 'var(--text-accent)' :
                                   item.severity === 'Major' ? 'var(--red-primary)' : item.severity === 'Moderate' ? '#F59E0B' : 'var(--text-disabled)',
                            border: isRumor ? '1px solid rgba(96,165,250,0.3)' : 'none',
                          }}>{isRumor ? '💬 Rumor' : item.type}</span>
                          {item.affectedSymbols.length > 0 && item.affectedSymbols.map(sym => (
                            <span key={sym} className="mono" style={{
                              fontSize: '10px', fontWeight: 700, color: 'var(--text-accent)',
                              background: 'rgba(96,165,250,0.1)', padding: '1px 5px', borderRadius: '3px',
                              cursor: 'pointer',
                            }} onClick={e => { e.stopPropagation(); selectStock(sym); }}>{sym}</span>
                          ))}
                        </div>
                        <span className="mono" style={{ fontSize: '10px', color: 'var(--text-disabled)' }}>
                          {item.timestamp ? new Date(item.timestamp).toLocaleTimeString() : ''}
                        </span>
                      </div>
                      <div style={{ fontSize: '13px', color, fontFamily: 'var(--font-ui)', lineHeight: 1.4 }}>
                        {item.headline}
                      </div>
                      {/* Expandable detail section (Bible 13.3) */}
                      {expandedNewsId === item.id && (
                        <div style={{ marginTop: '8px', paddingTop: '8px', borderTop: '1px solid var(--border)', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                          <div style={{ fontSize: '11px', color: 'var(--text-secondary)' }}>
                            <span>Impact: </span>
                            <span className="mono" style={{ color: item.priceEffect >= 0 ? 'var(--green-primary)' : 'var(--red-primary)', fontWeight: 700 }}>
                              {item.priceEffect >= 0 ? '+' : ''}{(item.priceEffect * 100).toFixed(1)}%
                            </span>
                            {item.affectedSectors.length > 0 && (
                              <span style={{ marginLeft: '12px' }}>Sectors: {item.affectedSectors.join(', ')}</span>
                            )}
                          </div>
                          {item.affectedSymbols[0] && (
                            <button onClick={e => { e.stopPropagation(); selectStock(item.affectedSymbols[0]); }} style={{
                              padding: '4px 12px', borderRadius: '4px', border: 'none', cursor: 'pointer',
                              fontSize: '11px', fontWeight: 700, fontFamily: 'var(--font-ui)',
                              background: 'var(--text-accent)', color: '#FFF',
                            }}>Trade {item.affectedSymbols[0]}</button>
                          )}
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            ) : (
              <div style={{ color: 'var(--text-disabled)', fontSize: '14px' }}>
                {newsItems.length > 0 ? `No ${newsFilter} news.` : 'No news yet. Start the simulation to see market events.'}
              </div>
            );
          })()}
        </div>
      )}

      {/* Analytics Tab — Comprehensive Stats Dashboard */}
      {!showStockDetail && activeTab === 'analytics' && (
        <div style={styles.content}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
            <h2 style={{ ...styles.heading, marginBottom: 0 }}>Performance Analytics</h2>
            <button style={styles.backBtn} onClick={() => wsClient.send('GetAnalytics', {})}>
              Refresh
            </button>
          </div>

          {analyticsData ? (() => {
            const a = analyticsData.analytics;
            return (
              <>
                {/* Top Row: Key Performance Numbers */}
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '10px', marginBottom: '20px' }}>
                  <div style={{ ...styles.summaryCard, borderLeft: `3px solid ${a.totalReturnPercent >= 0 ? 'var(--green-primary)' : 'var(--red-primary)'}` }}>
                    <span style={styles.summaryLabel}>Total Return</span>
                    <span className="mono" style={{
                      ...styles.summaryValue,
                      color: a.totalReturnPercent >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                    }}>
                      {a.totalReturnPercent >= 0 ? '+' : ''}{a.totalReturnPercent.toFixed(1)}%
                    </span>
                    <span className="mono" style={{ fontSize: '11px', color: 'var(--text-secondary)' }}>
                      {a.totalReturn >= 0 ? '+' : ''}${a.totalReturn.toFixed(0)}
                    </span>
                  </div>
                  <div style={{ ...styles.summaryCard, borderLeft: '3px solid var(--text-accent)' }}>
                    <span style={styles.summaryLabel}>Total Equity</span>
                    <span className="mono" style={styles.summaryValue}>${a.totalEquity.toFixed(0)}</span>
                    <span className="mono" style={{ fontSize: '11px', color: 'var(--text-secondary)' }}>
                      Cash: ${a.cash.toFixed(0)}
                    </span>
                  </div>
                  <div style={{ ...styles.summaryCard, borderLeft: '3px solid #F59E0B' }}>
                    <span style={styles.summaryLabel}>Win Rate</span>
                    <span className="mono" style={{ ...styles.summaryValue, color: a.winRate >= 50 ? 'var(--green-primary)' : 'var(--red-primary)' }}>
                      {a.winRate.toFixed(1)}%
                    </span>
                    <span className="mono" style={{ fontSize: '11px', color: 'var(--text-secondary)' }}>
                      {a.winningTrades}W / {a.losingTrades}L
                    </span>
                  </div>
                  <div style={{ ...styles.summaryCard, borderLeft: '3px solid #8B5CF6' }}>
                    <span style={styles.summaryLabel}>Max Drawdown</span>
                    <span className="mono" style={{ ...styles.summaryValue, color: 'var(--red-primary)' }}>
                      -{a.maxDrawdownPercent.toFixed(1)}%
                    </span>
                    <span className="mono" style={{ fontSize: '11px', color: 'var(--text-secondary)' }}>
                      Sharpe: {a.sharpeRatio.toFixed(2)}
                    </span>
                  </div>
                </div>

                {/* Trade Statistics Detail (Bible 6.3) */}
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '10px', marginBottom: '20px' }}>
                  <div style={styles.summaryCard}>
                    <span style={styles.summaryLabel}>Avg Win</span>
                    <span className="mono" style={{ ...styles.summaryValue, color: 'var(--green-primary)', fontSize: '16px' }}>
                      +${a.avgWin.toFixed(0)}
                    </span>
                  </div>
                  <div style={styles.summaryCard}>
                    <span style={styles.summaryLabel}>Avg Loss</span>
                    <span className="mono" style={{ ...styles.summaryValue, color: 'var(--red-primary)', fontSize: '16px' }}>
                      -${Math.abs(a.avgLoss).toFixed(0)}
                    </span>
                  </div>
                  <div style={styles.summaryCard}>
                    <span style={styles.summaryLabel}>Best Trade</span>
                    <span className="mono" style={{ ...styles.summaryValue, color: 'var(--green-primary)', fontSize: '16px' }}>
                      +${a.bestTradePnL.toFixed(0)}
                    </span>
                    <span className="mono" style={{ fontSize: '10px', color: 'var(--text-disabled)' }}>{a.bestTradeSymbol}</span>
                  </div>
                  <div style={styles.summaryCard}>
                    <span style={styles.summaryLabel}>Worst Trade</span>
                    <span className="mono" style={{ ...styles.summaryValue, color: 'var(--red-primary)', fontSize: '16px' }}>
                      ${a.worstTradePnL.toFixed(0)}
                    </span>
                    <span className="mono" style={{ fontSize: '10px', color: 'var(--text-disabled)' }}>{a.worstTradeSymbol}</span>
                  </div>
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '10px', marginBottom: '20px' }}>
                  <div style={styles.summaryCard}>
                    <span style={styles.summaryLabel}>Profit Factor</span>
                    <span className="mono" style={{ ...styles.summaryValue, fontSize: '16px', color: a.profitFactor >= 1 ? 'var(--green-primary)' : 'var(--red-primary)' }}>
                      {a.profitFactor >= 999 ? '∞' : a.profitFactor.toFixed(2)}
                    </span>
                  </div>
                  <div style={styles.summaryCard}>
                    <span style={styles.summaryLabel}>Win Streak</span>
                    <span className="mono" style={{ ...styles.summaryValue, fontSize: '16px', color: 'var(--green-primary)' }}>
                      {a.maxConsecutiveWins}
                    </span>
                  </div>
                  <div style={styles.summaryCard}>
                    <span style={styles.summaryLabel}>Loss Streak</span>
                    <span className="mono" style={{ ...styles.summaryValue, fontSize: '16px', color: 'var(--red-primary)' }}>
                      {a.maxConsecutiveLosses}
                    </span>
                  </div>
                  <div style={styles.summaryCard}>
                    <span style={styles.summaryLabel}>Commissions</span>
                    <span className="mono" style={{ ...styles.summaryValue, fontSize: '16px', color: 'var(--text-secondary)' }}>
                      ${a.totalCommissions.toFixed(0)}
                    </span>
                  </div>
                </div>

                {/* Equity Curve + Benchmark */}
                {analyticsData.equityHistory.length > 1 && (
                  <div style={{ marginBottom: '20px' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '8px' }}>
                      <h3 style={{ ...styles.heading, marginBottom: 0, fontSize: '14px' }}>Portfolio vs Market</h3>
                      <div style={{ display: 'flex', gap: '12px', fontSize: '11px' }}>
                        <span style={{ color: 'var(--green-primary)' }}>-- Portfolio</span>
                        <span style={{ color: '#F59E0B' }}>-- Market (SIMX)</span>
                      </div>
                    </div>
                    <div style={{
                      height: '180px',
                      background: 'var(--bg-secondary)',
                      border: '1px solid var(--border)',
                      borderRadius: '6px',
                      position: 'relative',
                      padding: '8px 12px',
                      overflow: 'hidden',
                    }}>
                      {(() => {
                        const data = analyticsData.equityHistory;
                        const firstEquity = Number(data[0].equity);

                        // Build normalized series (% return from start)
                        const portfolioReturns = data.map(d => ((Number(d.equity) - firstEquity) / firstEquity) * 100);
                        // Build cumulative market returns from daily changes
                        let cumMarket = 0;
                        const marketReturns = data.map(d => {
                          cumMarket += Number(d.marketIndex);
                          return cumMarket;
                        });

                        const allValues = [...portfolioReturns, ...marketReturns];
                        const min = Math.min(...allValues);
                        const max = Math.max(...allValues);
                        const range = max - min || 1;
                        const width = 100;
                        const height = 164;

                        const toPoints = (vals: number[]) => vals.map((v, i) => {
                          const x = (i / (vals.length - 1)) * width;
                          const y = height - ((v - min) / range) * height;
                          return `${x},${y}`;
                        }).join(' ');

                        const portfolioPoints = toPoints(portfolioReturns);
                        const marketPoints = toPoints(marketReturns);
                        const lastPortfolio = portfolioReturns[portfolioReturns.length - 1];
                        const lastMarket = marketReturns[marketReturns.length - 1];
                        const outperforming = lastPortfolio > lastMarket;
                        const color = lastPortfolio >= 0 ? 'var(--green-primary)' : 'var(--red-primary)';

                        return (
                          <>
                            <svg viewBox={`0 0 ${width} ${height}`} style={{ width: '100%', height: '100%' }} preserveAspectRatio="none">
                              {/* Zero line */}
                              {min < 0 && max > 0 && (
                                <line x1="0" y1={height - ((0 - min) / range) * height}
                                  x2={width} y2={height - ((0 - min) / range) * height}
                                  stroke="rgba(255,255,255,0.1)" strokeWidth="0.5" vectorEffect="non-scaling-stroke" />
                              )}
                              {/* Market benchmark */}
                              <polyline points={marketPoints} fill="none" stroke="#F59E0B" strokeWidth="1" strokeOpacity="0.6" vectorEffect="non-scaling-stroke" />
                              {/* Portfolio */}
                              <polyline points={portfolioPoints} fill="none" stroke={color} strokeWidth="1.5" vectorEffect="non-scaling-stroke" />
                            </svg>
                            <div style={{ position: 'absolute', top: '8px', right: '12px', textAlign: 'right' }}>
                              <span className="mono" style={{ fontSize: '12px', color, fontWeight: 700 }}>
                                You: {lastPortfolio >= 0 ? '+' : ''}{lastPortfolio.toFixed(1)}%
                              </span>
                              <br />
                              <span className="mono" style={{ fontSize: '11px', color: '#F59E0B' }}>
                                Mkt: {lastMarket >= 0 ? '+' : ''}{lastMarket.toFixed(1)}%
                              </span>
                            </div>
                            <div style={{ position: 'absolute', bottom: '8px', left: '12px' }}>
                              <span className="mono" style={{
                                fontSize: '11px', fontWeight: 600,
                                color: outperforming ? 'var(--green-primary)' : 'var(--red-primary)',
                              }}>
                                {outperforming ? 'Beating' : 'Trailing'} market by {Math.abs(lastPortfolio - lastMarket).toFixed(1)}%
                              </span>
                              <span className="mono" style={{ fontSize: '10px', color: 'var(--text-disabled)', marginLeft: '8px' }}>
                                {data.length}d
                              </span>
                            </div>
                          </>
                        );
                      })()}
                    </div>
                  </div>
                )}

                {/* Trade Statistics Grid */}
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '16px', marginBottom: '20px' }}>
                  {/* Left: Trade Metrics */}
                  <div>
                    <h3 style={{ ...styles.heading, marginBottom: '8px', fontSize: '14px' }}>Trade Statistics</h3>
                    <div style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border)', borderRadius: '6px', overflow: 'hidden' }}>
                      {[
                        ['Total Trades', a.totalTrades.toString()],
                        ['Avg Win', `+$${a.avgWin.toFixed(0)}`],
                        ['Avg Loss', `-$${a.avgLoss.toFixed(0)}`],
                        ['Profit Factor', a.profitFactor > 100 ? 'INF' : a.profitFactor.toFixed(2)],
                        ['Best Trade', `+$${a.bestTradePnL.toFixed(0)} (${a.bestTradeSymbol})`],
                        ['Worst Trade', `-$${a.worstTradePnL.toFixed(0)} (${a.worstTradeSymbol})`],
                        ['Max Win Streak', a.maxConsecutiveWins.toString()],
                        ['Max Loss Streak', a.maxConsecutiveLosses.toString()],
                        ['Commissions', `$${a.totalCommissions.toFixed(0)}`],
                      ].map(([label, value], i) => (
                        <div key={label} style={{
                          display: 'flex', justifyContent: 'space-between', padding: '8px 12px',
                          borderBottom: i < 8 ? '1px solid rgba(31,41,55,0.3)' : 'none',
                        }}>
                          <span style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>{label}</span>
                          <span className="mono" style={{ fontSize: '12px', fontWeight: 600, color: 'var(--text-primary)' }}>{value}</span>
                        </div>
                      ))}
                    </div>
                  </div>

                  {/* Right: P&L Breakdown + Sector P&L */}
                  <div>
                    <h3 style={{ ...styles.heading, marginBottom: '8px', fontSize: '14px' }}>P&L Breakdown</h3>
                    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px', marginBottom: '16px' }}>
                      <div style={styles.summaryCard}>
                        <span style={styles.summaryLabel}>Realized</span>
                        <span className="mono" style={{
                          fontSize: '14px', fontWeight: 700,
                          color: a.realizedPnL >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                        }}>
                          {a.realizedPnL >= 0 ? '+' : ''}${a.realizedPnL.toFixed(0)}
                        </span>
                      </div>
                      <div style={styles.summaryCard}>
                        <span style={styles.summaryLabel}>Unrealized</span>
                        <span className="mono" style={{
                          fontSize: '14px', fontWeight: 700,
                          color: a.unrealizedPnL >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                        }}>
                          {a.unrealizedPnL >= 0 ? '+' : ''}${a.unrealizedPnL.toFixed(0)}
                        </span>
                      </div>
                    </div>

                    {analyticsData.sectorPnL.length > 0 && (
                      <>
                        <h3 style={{ ...styles.heading, marginBottom: '8px', fontSize: '14px' }}>Sector P&L</h3>
                        <div style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border)', borderRadius: '6px', overflow: 'hidden' }}>
                          {analyticsData.sectorPnL.map((sp, i) => (
                            <div key={sp.sector} style={{
                              display: 'flex', justifyContent: 'space-between', padding: '6px 12px',
                              borderBottom: i < analyticsData.sectorPnL.length - 1 ? '1px solid rgba(31,41,55,0.3)' : 'none',
                            }}>
                              <span style={{ fontSize: '12px', color: 'var(--text-secondary)' }}>{sp.sector}</span>
                              <span className="mono" style={{
                                fontSize: '12px', fontWeight: 600,
                                color: sp.pnl >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                              }}>
                                {sp.pnl >= 0 ? '+' : ''}${sp.pnl.toFixed(0)}
                              </span>
                            </div>
                          ))}
                        </div>
                      </>
                    )}
                  </div>
                </div>

                {/* Tax Summary */}
                {taxSummary && (taxSummary.totalTaxPaid > 0 || taxSummary.shortTermGains > 0) && (
                  <div style={{ marginBottom: '20px' }}>
                    <h3 style={{ ...styles.heading, marginBottom: '8px', fontSize: '14px' }}>Tax Summary</h3>
                    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '8px' }}>
                      <div style={styles.summaryCard}>
                        <span style={styles.summaryLabel}>Total Tax Paid</span>
                        <span className="mono" style={{ fontSize: '14px', fontWeight: 700, color: 'var(--red-primary)' }}>
                          ${taxSummary.totalTaxPaid?.toFixed(0) ?? 0}
                        </span>
                      </div>
                      <div style={styles.summaryCard}>
                        <span style={styles.summaryLabel}>Effective Rate</span>
                        <span className="mono" style={{ fontSize: '14px', fontWeight: 700, color: 'var(--text-primary)' }}>
                          {taxSummary.effectiveTaxRate?.toFixed(1) ?? 0}%
                        </span>
                      </div>
                      <div style={styles.summaryCard}>
                        <span style={styles.summaryLabel}>Tax Saved</span>
                        <span className="mono" style={{ fontSize: '14px', fontWeight: 700, color: 'var(--green-primary)' }}>
                          ${taxSummary.taxSaved?.toFixed(0) ?? 0}
                        </span>
                        <span style={{ fontSize: '9px', color: 'var(--text-disabled)' }}>via loss harvesting</span>
                      </div>
                      <div style={styles.summaryCard}>
                        <span style={styles.summaryLabel}>Div Tax</span>
                        <span className="mono" style={{ fontSize: '14px', fontWeight: 700, color: 'var(--text-primary)' }}>
                          ${taxSummary.dividendTaxPaid?.toFixed(0) ?? 0}
                        </span>
                      </div>
                    </div>
                    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px', marginTop: '8px' }}>
                      <div style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border)', borderRadius: '6px', padding: '8px 12px' }}>
                        <span style={{ fontSize: '10px', color: 'var(--text-disabled)' }}>SHORT-TERM ({taxSummary.shortTermTaxRate}%)</span>
                        <div className="mono" style={{ fontSize: '12px', marginTop: '2px' }}>
                          <span style={{ color: 'var(--green-primary)' }}>+${(taxSummary.shortTermGains || 0).toFixed(0)}</span>
                          {' / '}
                          <span style={{ color: 'var(--red-primary)' }}>-${(taxSummary.shortTermLosses || 0).toFixed(0)}</span>
                        </div>
                      </div>
                      <div style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border)', borderRadius: '6px', padding: '8px 12px' }}>
                        <span style={{ fontSize: '10px', color: 'var(--text-disabled)' }}>LONG-TERM ({taxSummary.longTermTaxRate}%)</span>
                        <div className="mono" style={{ fontSize: '12px', marginTop: '2px' }}>
                          <span style={{ color: 'var(--green-primary)' }}>+${(taxSummary.longTermGains || 0).toFixed(0)}</span>
                          {' / '}
                          <span style={{ color: 'var(--red-primary)' }}>-${(taxSummary.longTermLosses || 0).toFixed(0)}</span>
                        </div>
                      </div>
                    </div>
                  </div>
                )}

                {/* Trade Analysis — Visual Stats */}
                {analyticsData.analytics.totalTrades > 0 && (
                  <div style={{ marginBottom: '20px' }}>
                    <h3 style={{ ...styles.heading, marginBottom: '8px', fontSize: '14px' }}>Trade Analysis</h3>
                    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px' }}>
                      {/* Win/Loss Distribution Bar */}
                      <div style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border)', borderRadius: '6px', padding: '12px' }}>
                        <span style={{ fontSize: '11px', color: 'var(--text-disabled)', letterSpacing: '1px' }}>WIN / LOSS RATIO</span>
                        <div style={{ display: 'flex', height: '24px', borderRadius: '4px', overflow: 'hidden', margin: '8px 0', gap: '2px' }}>
                          <div style={{
                            flex: analyticsData.analytics.winningTrades,
                            background: 'var(--green-primary)', display: 'flex', alignItems: 'center', justifyContent: 'center',
                          }}>
                            <span className="mono" style={{ fontSize: '10px', color: '#FFF', fontWeight: 700 }}>
                              {analyticsData.analytics.winningTrades}W
                            </span>
                          </div>
                          <div style={{
                            flex: analyticsData.analytics.losingTrades,
                            background: 'var(--red-primary)', display: 'flex', alignItems: 'center', justifyContent: 'center',
                          }}>
                            <span className="mono" style={{ fontSize: '10px', color: '#FFF', fontWeight: 700 }}>
                              {analyticsData.analytics.losingTrades}L
                            </span>
                          </div>
                        </div>
                        <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '11px' }}>
                          <span className="mono" style={{ color: 'var(--green-primary)' }}>Avg +${a.avgWin.toFixed(0)}</span>
                          <span className="mono" style={{ color: 'var(--red-primary)' }}>Avg -${a.avgLoss.toFixed(0)}</span>
                        </div>
                      </div>

                      {/* Risk/Reward Visual */}
                      <div style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border)', borderRadius: '6px', padding: '12px' }}>
                        <span style={{ fontSize: '11px', color: 'var(--text-disabled)', letterSpacing: '1px' }}>RISK / REWARD</span>
                        <div style={{ display: 'flex', gap: '16px', marginTop: '8px', justifyContent: 'center' }}>
                          <div style={{ textAlign: 'center' }}>
                            <span style={{ fontSize: '9px', color: 'var(--text-disabled)' }}>PROFIT FACTOR</span>
                            <span className="mono" style={{
                              display: 'block', fontSize: '22px', fontWeight: 700, marginTop: '4px',
                              color: a.profitFactor >= 1.5 ? 'var(--green-primary)' : a.profitFactor >= 1 ? '#F59E0B' : 'var(--red-primary)',
                            }}>
                              {a.profitFactor > 100 ? 'INF' : a.profitFactor.toFixed(2)}
                            </span>
                          </div>
                          <div style={{ width: '1px', background: 'var(--border)' }} />
                          <div style={{ textAlign: 'center' }}>
                            <span style={{ fontSize: '9px', color: 'var(--text-disabled)' }}>SHARPE</span>
                            <span className="mono" style={{
                              display: 'block', fontSize: '22px', fontWeight: 700, marginTop: '4px',
                              color: a.sharpeRatio >= 1 ? 'var(--green-primary)' : a.sharpeRatio >= 0 ? '#F59E0B' : 'var(--red-primary)',
                            }}>
                              {a.sharpeRatio.toFixed(2)}
                            </span>
                          </div>
                          <div style={{ width: '1px', background: 'var(--border)' }} />
                          <div style={{ textAlign: 'center' }}>
                            <span style={{ fontSize: '9px', color: 'var(--text-disabled)' }}>MAX DD</span>
                            <span className="mono" style={{
                              display: 'block', fontSize: '22px', fontWeight: 700, marginTop: '4px',
                              color: 'var(--red-primary)',
                            }}>
                              -{a.maxDrawdownPercent.toFixed(1)}%
                            </span>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                )}

                {/* Achievements Section */}
                {achievements.length > 0 && (
                  <div>
                    <h3 style={{ ...styles.heading, marginBottom: '8px', fontSize: '14px' }}>
                      Achievements ({achievements.filter(a => a.unlocked).length}/{achievements.length})
                    </h3>
                    <div style={{
                      display: 'grid',
                      gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
                      gap: '8px',
                    }}>
                      {achievements.map(ach => {
                        const categoryIcon = ach.category === 'Wealth' ? <TrendingUp size={14} /> :
                          ach.category === 'Trading' ? <Target size={14} /> :
                          ach.category === 'Market' ? <TrendingDown size={14} /> :
                          <Shield size={14} />;
                        return (
                          <div key={ach.id} style={{
                            background: ach.unlocked ? 'rgba(212, 175, 55, 0.08)' : 'var(--bg-secondary)',
                            border: `1px solid ${ach.unlocked ? 'rgba(212, 175, 55, 0.3)' : 'var(--border)'}`,
                            borderRadius: '6px',
                            padding: '10px 12px',
                            opacity: ach.unlocked ? 1 : 0.5,
                          }}>
                            <div style={{ display: 'flex', alignItems: 'center', gap: '6px', marginBottom: '4px' }}>
                              <span style={{ color: ach.unlocked ? '#D4AF37' : 'var(--text-disabled)' }}>{categoryIcon}</span>
                              <span style={{
                                fontSize: '12px', fontWeight: 700,
                                color: ach.unlocked ? '#D4AF37' : 'var(--text-disabled)',
                              }}>
                                {ach.name}
                              </span>
                              {ach.unlocked && <Trophy size={12} style={{ color: '#D4AF37', marginLeft: 'auto' }} />}
                            </div>
                            <div style={{ fontSize: '11px', color: ach.unlocked ? 'var(--text-secondary)' : 'var(--text-disabled)' }}>
                              {ach.description}
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  </div>
                )}
              </>
            );
          })() : (
            <div style={{ color: 'var(--text-disabled)', fontSize: '14px' }}>Start trading to see analytics.</div>
          )}
        </div>
      )}

      {/* Trading Journal Tab */}
      {!showStockDetail && activeTab === 'journal' && (
        <div style={styles.content}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '16px' }}>
            <h2 style={{ ...styles.heading, marginBottom: 0 }}>Trading Journal</h2>
            <button style={styles.backBtn} onClick={() => wsClient.send('GetTradeJournal', {})}>
              Refresh
            </button>
          </div>

          {tradeJournal.length > 0 ? (
            <>
              {/* Summary Stats */}
              <div style={{ display: 'flex', gap: '10px', marginBottom: '16px' }}>
                {(() => {
                  const totalPnL = tradeJournal.reduce((sum, t) => sum + t.pnl, 0);
                  const wins = tradeJournal.filter(t => t.pnl > 0);
                  const losses = tradeJournal.filter(t => t.pnl < 0);
                  const avgHold = tradeJournal.reduce((sum, t) => sum + t.holdingDays, 0) / tradeJournal.length;
                  return (
                    <>
                      <div style={{ ...styles.summaryCard, flex: 1 }}>
                        <span style={styles.summaryLabel}>Net P&L</span>
                        <span className="mono" style={{
                          fontSize: '16px', fontWeight: 700,
                          color: totalPnL >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                        }}>
                          {totalPnL >= 0 ? '+' : ''}${totalPnL.toFixed(0)}
                        </span>
                      </div>
                      <div style={{ ...styles.summaryCard, flex: 1 }}>
                        <span style={styles.summaryLabel}>Trades</span>
                        <span className="mono" style={{ fontSize: '16px', fontWeight: 700, color: 'var(--text-primary)' }}>
                          {tradeJournal.length}
                        </span>
                      </div>
                      <div style={{ ...styles.summaryCard, flex: 1 }}>
                        <span style={styles.summaryLabel}>Win / Loss</span>
                        <span className="mono" style={{ fontSize: '16px', fontWeight: 700 }}>
                          <span style={{ color: 'var(--green-primary)' }}>{wins.length}</span>
                          {' / '}
                          <span style={{ color: 'var(--red-primary)' }}>{losses.length}</span>
                        </span>
                      </div>
                      <div style={{ ...styles.summaryCard, flex: 1 }}>
                        <span style={styles.summaryLabel}>Avg Hold</span>
                        <span className="mono" style={{ fontSize: '16px', fontWeight: 700, color: 'var(--text-primary)' }}>
                          {avgHold.toFixed(1)}d
                        </span>
                      </div>
                    </>
                  );
                })()}
              </div>

              {/* Trade History Table */}
              <div style={styles.tableContainer}>
                <table style={styles.table}>
                  <thead>
                    <tr>
                      <th style={styles.th}>Symbol</th>
                      <th style={styles.th}>Side</th>
                      <th style={styles.th}>Sector</th>
                      <th style={{ ...styles.th, textAlign: 'right' }}>Entry</th>
                      <th style={{ ...styles.th, textAlign: 'right' }}>Exit</th>
                      <th style={{ ...styles.th, textAlign: 'right' }}>Qty</th>
                      <th style={{ ...styles.th, textAlign: 'right' }}>P&L</th>
                      <th style={{ ...styles.th, textAlign: 'right' }}>P&L %</th>
                      <th style={{ ...styles.th, textAlign: 'right' }}>Hold</th>
                      <th style={styles.th}>Date</th>
                    </tr>
                  </thead>
                  <tbody>
                    {tradeJournal.map((t) => (
                      <tr key={t.id} style={{ ...styles.tr, cursor: 'pointer' }} onClick={() => selectStock(t.symbol)}>
                        <td className="mono" style={{ ...styles.td, fontWeight: 700 }}>{t.symbol}</td>
                        <td style={{
                          ...styles.td,
                          color: t.side === 'Long' ? 'var(--green-primary)' : 'var(--red-primary)',
                          fontWeight: 600,
                        }}>{t.side}</td>
                        <td style={{ ...styles.td, fontSize: '11px', color: 'var(--text-secondary)' }}>{t.sector}</td>
                        <td className="mono" style={{ ...styles.td, textAlign: 'right' }}>${t.entryPrice.toFixed(2)}</td>
                        <td className="mono" style={{ ...styles.td, textAlign: 'right' }}>${t.exitPrice.toFixed(2)}</td>
                        <td className="mono" style={{ ...styles.td, textAlign: 'right' }}>{t.quantity}</td>
                        <td className="mono" style={{
                          ...styles.td, textAlign: 'right', fontWeight: 600,
                          color: t.pnl >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                        }}>
                          {t.pnl >= 0 ? '+' : ''}${t.pnl.toFixed(2)}
                        </td>
                        <td className="mono" style={{
                          ...styles.td, textAlign: 'right',
                          color: t.pnlPercent >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                        }}>
                          {t.pnlPercent >= 0 ? '+' : ''}{t.pnlPercent.toFixed(1)}%
                        </td>
                        <td className="mono" style={{ ...styles.td, textAlign: 'right', color: 'var(--text-secondary)' }}>
                          {t.holdingDays}d
                        </td>
                        <td className="mono" style={{ ...styles.td, fontSize: '11px', color: 'var(--text-disabled)' }}>
                          {new Date(t.exitTime).toLocaleDateString()}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          ) : (
            <div style={{ color: 'var(--text-disabled)', fontSize: '14px' }}>
              No completed trades yet. Close a position to see your trading journal.
            </div>
          )}
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
    inset: 0,
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    background: 'rgba(10, 14, 23, 0.65)',
    backdropFilter: 'blur(2px)',
    fontSize: '64px',
    fontWeight: 900,
    color: 'rgba(96, 165, 250, 0.7)',
    textShadow: '0 0 30px rgba(96, 165, 250, 0.4), 0 0 60px rgba(96, 165, 250, 0.15)',
    pointerEvents: 'none',
    zIndex: 100,
    fontFamily: 'var(--font-mono)',
    letterSpacing: '16px',
    textTransform: 'uppercase' as const,
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
    fontSize: '32px',
    fontWeight: 700,
    color: 'var(--text-primary)',
    display: 'block',
    textShadow: '0 0 15px rgba(249, 250, 251, 0.15)',
    letterSpacing: '1px',
  },
  detailChange: {
    fontSize: '16px',
    display: 'block',
    fontWeight: 600,
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
