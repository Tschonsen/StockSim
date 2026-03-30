import { useState, useEffect } from 'react';
import { useMarketStore } from '@/stores/marketStore';
import { StockChart, ChartType } from '@/components/charts/StockChart';
import { Orderbook } from '@/components/charts/Orderbook';
import { WebSocketClient } from '@/services/websocket';
import { ArrowLeft } from 'lucide-react';
import { HelpTip } from '@/components/ui/HelpTip';
import { styles } from '@/styles/centralStyles';
import { FUND_HELP } from '@/data/fundHelp';

interface StockDetailViewProps {
  wsClient: WebSocketClient;
}

export function StockDetailView({ wsClient }: StockDetailViewProps) {
  const selectStock = useMarketStore((s) => s.selectStock);
  const selectedSymbol = useMarketStore((s) => s.selectedSymbol);
  const stocks = useMarketStore((s) => s.stocks);
  const stockList = useMarketStore((s) => s.stockList);
  const ohlcvData = useMarketStore((s) => s.ohlcvData);
  const indicatorData = useMarketStore((s) => s.indicatorData);
  const orderbookData = useMarketStore((s) => s.orderbookData);
  const shortSqueezeWarning = useMarketStore((s) => s.shortSqueezeWarning);
  const setShortSqueezeWarning = useMarketStore((s) => s.setShortSqueezeWarning);
  const bigMoveSymbol = useMarketStore((s) => s.bigMoveSymbol);
  const bigMoveDirection = useMarketStore((s) => s.bigMoveDirection);
  const stockFundamentals = useMarketStore((s) => s.stockFundamentals);

  const [chartTimeframe, setChartTimeframe] = useState<string>('ALL');
  const [chartType, setChartType] = useState<ChartType>('candle');

  // Re-fetch OHLCV when timeframe changes
  useEffect(() => {
    if (selectedSymbol) {
      wsClient.send('GetOHLCV', { symbol: selectedSymbol, timeframe: chartTimeframe });
    }
  }, [chartTimeframe, selectedSymbol]);

  const stock = selectedSymbol ? stocks.get(selectedSymbol) : undefined;
  const chartData = selectedSymbol ? (ohlcvData.get(selectedSymbol) || []) : [];
  const indicators = selectedSymbol ? indicatorData.get(selectedSymbol) : undefined;

  if (!stock || !selectedSymbol) return null;

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
            <span style={{ fontWeight: 700, color: 'var(--warning)', fontSize: '13px' }}>
              ⚠ SHORT SQUEEZE WARNING
            </span>
            <span style={{ color: 'var(--text-secondary)', fontSize: '12px', marginLeft: '8px' }}>
              Short Interest {shortSqueezeWarning.shortInterestPercent.toFixed(1)}% | Price surged +{shortSqueezeWarning.priceChangePercent.toFixed(1)}% in last hour
            </span>
            {shortSqueezeWarning.playerHasShortPosition && (
              <div style={{ color: 'var(--red-primary)', fontSize: '12px', fontWeight: 600, marginTop: '4px' }}>
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
              background: 'rgba(245,158,11,0.15)', color: 'var(--warning)',
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
                color: chartType === ct ? 'var(--text-primary)' : 'var(--text-secondary)',
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
              color: chartTimeframe === tf ? 'var(--text-primary)' : 'var(--text-secondary)',
            }}>{tf}</button>
          ))}
          </div>
        </div>
        {indicators && (
          <div style={{ display: 'flex', gap: '12px', fontSize: '11px' }}>
            {indicators.sma20 && <span style={{ color: 'var(--warning)' }}>-- SMA 20</span>}
            {indicators.sma50 && <span style={{ color: 'var(--chart-purple)' }}>-- SMA 50</span>}
            {indicators.sma200 && <span style={{ color: 'var(--chart-pink)' }}>-- SMA 200</span>}
            {indicators.bollingerUpper && <span style={{ color: 'var(--chart-blue)' }}>-- Bollinger</span>}
          </div>
        )}
      </div>

      <div
        className={bigMoveSymbol === stock.symbol ? 'big-move-shake' : ''}
        key={bigMoveSymbol === stock.symbol ? `shake-${Date.now()}` : 'chart'}
        style={{
          borderLeft: bigMoveSymbol === stock.symbol
            ? `3px solid ${bigMoveDirection === 'up' ? 'var(--green-primary)' : 'var(--red-primary)'}`
            : '3px solid transparent',
          transition: 'border-color 0.3s',
        }}
      >
        <StockChart
          symbol={stock.symbol}
          data={chartData}
          indicators={indicators}
          chartType={chartType}
          height={450}
        />
      </div>

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
      {(() => {
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
                    <th style={{ ...styles.th, textAlign: 'right' }}>Mkt Cap <HelpTip term="Market Cap" size={10} /></th>
                    <th style={{ ...styles.th, textAlign: 'right' }}>P/E <HelpTip term="P/E Ratio" size={10} /></th>
                    <th style={{ ...styles.th, textAlign: 'right' }}>Div Yield <HelpTip term="Dividend Yield" size={10} /></th>
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
              ['Revenue', stockFundamentals.revenue >= 1e9 ? `$${(stockFundamentals.revenue / 1e9).toFixed(1)}B` : `$${(stockFundamentals.revenue / 1e6).toFixed(0)}M`],
              ['Net Income', stockFundamentals.netIncome >= 1e9 ? `$${(stockFundamentals.netIncome / 1e9).toFixed(1)}B` : stockFundamentals.netIncome <= -1e9 ? `-$${(Math.abs(stockFundamentals.netIncome) / 1e9).toFixed(1)}B` : `$${(stockFundamentals.netIncome / 1e6).toFixed(0)}M`, stockFundamentals.netIncome >= 0 ? 'var(--accent-green)' : 'var(--accent-red)'],
              ['Div Yield', stockFundamentals.dividendYield > 0 ? `${(stockFundamentals.dividendYield * 100).toFixed(2)}%` : '-'],
              ['Revenue Growth', `${(stockFundamentals.revenueGrowth * 100).toFixed(1)}%`],
              ['Debt/Equity', stockFundamentals.debtToEquity.toFixed(2)],
              ['Fair Value', `$${stockFundamentals.fairValue.toFixed(2)}`],
              ['Employees', stockFundamentals.employees >= 1000 ? `${(stockFundamentals.employees / 1000).toFixed(1)}K` : String(stockFundamentals.employees)],
              ['Avg Volume', stockFundamentals.averageVolume >= 1e6 ? `${(stockFundamentals.averageVolume / 1e6).toFixed(1)}M` : `${(stockFundamentals.averageVolume / 1e3).toFixed(0)}K`],
              ['Liquidity', `${stockFundamentals.liquidityScore}/10`],
              ...(stock.subsector ? [['Subsector', stock.subsector]] : []),
              ['Day Range', `$${stockFundamentals.dayLow.toFixed(2)} - $${stockFundamentals.dayHigh.toFixed(2)}`],
              ['Volatility', `${(stockFundamentals.baseVolatility * 100).toFixed(1)}%`],
              ['Insider Own', `${(stockFundamentals.insiderOwnership * 100).toFixed(1)}%`],
              ['Inst. Own', `${(stockFundamentals.institutionalOwnership * 100).toFixed(1)}%`],
              ['Analyst', stockFundamentals.analystConsensus],
              ['Target Price', `$${stockFundamentals.targetPrice.toFixed(2)}`],
              ['Rating', `${stockFundamentals.analystRating.toFixed(1)}/5.0`],
              ['Upside', `${(((stockFundamentals.targetPrice - stock.price) / stock.price) * 100).toFixed(1)}%`],
            ].map(([label, value, color]) => (
              <div key={label} style={{
                background: 'var(--bg-secondary)', border: '1px solid var(--border)',
                borderRadius: '4px', padding: '8px 10px',
              }}>
                <span style={{ fontSize: '10px', color: 'var(--text-disabled)', textTransform: 'uppercase', display: 'flex', alignItems: 'center', gap: 3 }}>
                  {label}
                  {FUND_HELP[label as string] && <HelpTip term={FUND_HELP[label as string]} size={10} />}
                </span>
                <span className="mono" style={{ fontSize: '13px', fontWeight: 600, color: color || 'var(--text-primary)' }}>{value}</span>
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
}
