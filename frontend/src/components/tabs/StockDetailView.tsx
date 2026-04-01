import { useState, useEffect } from 'react';
import { useMarketStore } from '@/stores/marketStore';
import { StockChart, ChartType } from '@/components/charts/StockChart';
import { Orderbook } from '@/components/charts/Orderbook';
import { WebSocketClient } from '@/services/websocket';
import { ArrowLeft, Star, StarOff, ChevronDown, ChevronRight, Bell, BellOff, X } from 'lucide-react';
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
  const earningsCalendar = useMarketStore((s) => s.earningsCalendar);
  const gameTime = useMarketStore((s) => s.gameTime);

  const watchlist = useMarketStore((s) => s.watchlist);
  const addToWatchlist = useMarketStore((s) => s.addToWatchlist);
  const removeFromWatchlist = useMarketStore((s) => s.removeFromWatchlist);

  const [chartTimeframe, setChartTimeframe] = useState<string>('ALL');
  const [chartType, setChartType] = useState<ChartType>('candle');
  const [collapsedSections, setCollapsedSections] = useState<Set<string>>(new Set());
  const [showAlertPanel, setShowAlertPanel] = useState(false);
  const [alertPrice, setAlertPrice] = useState('');
  const [alertCondition, setAlertCondition] = useState<'above' | 'below'>('above');
  const [compareSymbols, setCompareSymbols] = useState<string[]>([]);
  const [compareInput, setCompareInput] = useState('');
  const [alerts, setAlerts] = useState<{ id: string; symbol: string; condition: string; targetPrice: number }[]>([]);

  const toggleSection = (section: string) => {
    setCollapsedSections(prev => {
      const next = new Set(prev);
      next.has(section) ? next.delete(section) : next.add(section);
      return next;
    });
  };

  // Re-fetch OHLCV when timeframe changes
  useEffect(() => {
    if (selectedSymbol) {
      wsClient.send('GetOHLCV', { symbol: selectedSymbol, timeframe: chartTimeframe });
    }
  }, [chartTimeframe, selectedSymbol]);

  // Fetch alerts and listen for updates
  useEffect(() => {
    wsClient.send('GetAlerts', {});
    const unsub1 = wsClient.on('AlertList', (data: unknown) => {
      const d = data as { alerts: { id: string; symbol: string; condition: string; targetPrice: number }[] };
      setAlerts(d.alerts || []);
    });
    const unsub2 = wsClient.on('AlertSet', () => wsClient.send('GetAlerts', {}));
    const unsub3 = wsClient.on('AlertDeleted', () => wsClient.send('GetAlerts', {}));
    return () => { unsub1(); unsub2(); unsub3(); };
  }, [wsClient]);

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
          {(() => {
            const inWatchlist = watchlist.includes(stock.symbol);
            return (
              <button
                onClick={() => inWatchlist ? removeFromWatchlist(stock.symbol) : addToWatchlist(stock.symbol)}
                style={{
                  background: 'none', border: '1px solid var(--border)', borderRadius: '4px',
                  cursor: 'pointer', padding: '2px 8px', display: 'flex', alignItems: 'center', gap: '4px',
                  color: inWatchlist ? 'var(--warning)' : 'var(--text-disabled)',
                  fontSize: '10px', fontWeight: 600, fontFamily: 'var(--font-ui)',
                }}
                title={inWatchlist ? 'Remove from Watchlist' : 'Add to Watchlist'}
              >
                {inWatchlist ? <StarOff size={12} /> : <Star size={12} />}
                {inWatchlist ? 'Remove' : 'Watch'}
              </button>
            );
          })()}
          {/* Earnings Countdown Badge */}
          {earningsCalendar?.upcoming && (() => {
            const earnings = earningsCalendar.upcoming.find(e => e.symbol === stock.symbol);
            if (!earnings) return null;
            const now = gameTime ? new Date(gameTime) : new Date();
            const reportDate = new Date(earnings.reportDate);
            const diffDays = Math.round((reportDate.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
            if (diffDays < 0 || diffDays > 30) return null;
            return (
              <span style={{
                fontSize: '9px', fontWeight: 700, padding: '1px 6px', borderRadius: '3px',
                background: diffDays <= 3 ? 'rgba(239,68,68,0.15)' : 'rgba(245,158,11,0.12)',
                color: diffDays <= 3 ? 'var(--red-primary)' : 'var(--warning)',
                border: `1px solid ${diffDays <= 3 ? 'rgba(239,68,68,0.3)' : 'rgba(245,158,11,0.3)'}`,
                display: 'flex', alignItems: 'center', gap: '3px',
              }}>
                📊 Earnings {diffDays === 0 ? 'TODAY' : diffDays === 1 ? 'Tomorrow' : `in ${diffDays}d`}
                <span className="mono" style={{ fontSize: '8px', color: 'var(--text-disabled)' }}>
                  Est: ${earnings.expectedEPS.toFixed(2)}
                </span>
              </span>
            );
          })()}

          {/* Price Alert Button */}
          {(() => {
            const stockAlerts = alerts.filter(a => a.symbol === stock.symbol);
            return (
              <button
                onClick={() => { setShowAlertPanel(!showAlertPanel); setAlertPrice(stock.price.toFixed(2)); }}
                style={{
                  background: 'none', border: '1px solid var(--border)', borderRadius: '4px',
                  cursor: 'pointer', padding: '2px 8px', display: 'flex', alignItems: 'center', gap: '4px',
                  color: stockAlerts.length > 0 ? 'var(--warning)' : 'var(--text-disabled)',
                  fontSize: '10px', fontWeight: 600, fontFamily: 'var(--font-ui)',
                }}
                title="Set Price Alert"
              >
                {stockAlerts.length > 0 ? <Bell size={12} /> : <BellOff size={12} />}
                {stockAlerts.length > 0 ? `${stockAlerts.length} Alert${stockAlerts.length > 1 ? 's' : ''}` : 'Alert'}
              </button>
            );
          })()}

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
            {' | '}{stock.marketCap >= 200_000_000_000 ? 'Mega Cap' :
                    stock.marketCap >= 10_000_000_000 ? 'Large Cap' :
                    stock.marketCap >= 2_000_000_000 ? 'Mid Cap' :
                    stock.marketCap >= 300_000_000 ? 'Small Cap' : 'Micro Cap'}
          </span>
          {/* 52-Week Range */}
          {stockFundamentals?.symbol === selectedSymbol && stockFundamentals.yearHigh > 0 && (
            <div style={{ display: 'flex', alignItems: 'center', gap: '6px', marginTop: '4px' }}>
              <span className="mono" style={{ fontSize: '10px', color: 'var(--text-disabled)' }}>52W</span>
              <span className="mono" style={{ fontSize: '10px', color: 'var(--red-primary)' }}>${stockFundamentals.yearLow.toFixed(2)}</span>
              <div style={{ flex: 1, height: '4px', borderRadius: '2px', background: 'var(--bg-tertiary)', position: 'relative', maxWidth: '120px' }}>
                {stockFundamentals.yearHigh > stockFundamentals.yearLow && (
                  <div style={{
                    position: 'absolute', top: '-1px', bottom: '-1px',
                    left: `${Math.max(0, Math.min(100, ((stock.price - stockFundamentals.yearLow) / (stockFundamentals.yearHigh - stockFundamentals.yearLow)) * 100))}%`,
                    width: '4px', borderRadius: '2px', background: 'var(--text-accent)',
                  }} />
                )}
              </div>
              <span className="mono" style={{ fontSize: '10px', color: 'var(--green-primary)' }}>${stockFundamentals.yearHigh.toFixed(2)}</span>
            </div>
          )}
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

      {/* Price Alert Panel */}
      {showAlertPanel && (
        <div style={{
          background: 'var(--bg-secondary)', border: '1px solid var(--border)',
          borderRadius: '6px', padding: '10px 14px', marginBottom: '8px',
        }}>
          <div style={{ display: 'flex', gap: '8px', alignItems: 'center', marginBottom: '8px' }}>
            <select
              value={alertCondition}
              onChange={e => setAlertCondition(e.target.value as 'above' | 'below')}
              style={{
                height: '30px', background: 'var(--bg-input)', color: 'var(--text-primary)',
                border: '1px solid var(--border)', borderRadius: '4px', padding: '0 8px',
                fontSize: '12px', fontFamily: 'var(--font-ui)',
              }}
            >
              <option value="above">Price Above</option>
              <option value="below">Price Below</option>
            </select>
            <span style={{ fontSize: '14px', color: 'var(--text-secondary)' }}>$</span>
            <input
              type="number"
              value={alertPrice}
              onChange={e => setAlertPrice(e.target.value)}
              placeholder="0.00"
              step="0.01"
              style={{
                width: '100px', height: '30px', background: 'var(--bg-input)',
                color: 'var(--text-primary)', border: '1px solid var(--border)',
                borderRadius: '4px', padding: '0 8px', fontFamily: 'var(--font-mono)', fontSize: '13px',
              }}
            />
            <button
              onClick={() => {
                const price = parseFloat(alertPrice);
                if (price > 0 && selectedSymbol) {
                  wsClient.send('SetAlert', { symbol: selectedSymbol, condition: alertCondition, targetPrice: price });
                  setAlertPrice('');
                }
              }}
              style={{
                padding: '4px 12px', borderRadius: '4px', border: 'none', cursor: 'pointer',
                fontSize: '11px', fontWeight: 700, fontFamily: 'var(--font-ui)',
                background: 'var(--text-accent)', color: 'var(--text-primary)',
              }}
            >Set Alert</button>
          </div>
          {/* Active alerts for this stock */}
          {alerts.filter(a => a.symbol === stock.symbol).length > 0 && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
              {alerts.filter(a => a.symbol === stock.symbol).map(a => (
                <div key={a.id} style={{
                  display: 'flex', justifyContent: 'space-between', alignItems: 'center',
                  padding: '4px 8px', background: 'var(--bg-tertiary)', borderRadius: '4px', fontSize: '11px',
                }}>
                  <span>
                    <Bell size={10} style={{ color: 'var(--warning)', marginRight: '4px' }} />
                    <span style={{ color: 'var(--text-secondary)' }}>
                      {a.condition === 'above' ? 'Above' : 'Below'}
                    </span>
                    <span className="mono" style={{ color: 'var(--text-primary)', fontWeight: 600, marginLeft: '4px' }}>
                      ${a.targetPrice.toFixed(2)}
                    </span>
                  </span>
                  <button
                    onClick={() => wsClient.send('DeleteAlert', { alertId: a.id })}
                    style={{
                      background: 'none', border: 'none', cursor: 'pointer',
                      color: 'var(--text-disabled)', padding: '2px',
                    }}
                  ><X size={12} /></button>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

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
          {/* Compare Stocks */}
          <div style={{ display: 'flex', gap: '2px', alignItems: 'center' }}>
            <div style={{ width: '1px', height: '16px', background: 'var(--border)' }} />
            {compareSymbols.map((sym, i) => {
              const colors = ['#F97316', '#A855F7', '#14B8A6', '#F43F5E', '#84CC16'];
              return (
                <span key={sym} style={{
                  display: 'flex', alignItems: 'center', gap: '2px', padding: '2px 6px',
                  background: 'var(--bg-tertiary)', borderRadius: '3px', fontSize: '10px',
                }}>
                  <span style={{ color: colors[i % colors.length], fontWeight: 700 }}>{sym}</span>
                  <button onClick={() => setCompareSymbols(prev => prev.filter(s => s !== sym))} style={{
                    background: 'none', border: 'none', cursor: 'pointer', color: 'var(--text-disabled)',
                    fontSize: '12px', padding: '0 2px', lineHeight: 1,
                  }}>×</button>
                </span>
              );
            })}
            {compareSymbols.length < 3 && (
              <div style={{ position: 'relative' }}>
                <input
                  type="text"
                  value={compareInput}
                  onChange={e => setCompareInput(e.target.value.toUpperCase())}
                  onKeyDown={e => {
                    if (e.key === 'Enter' && compareInput) {
                      const sym = compareInput.trim();
                      if (sym && !compareSymbols.includes(sym) && sym !== selectedSymbol && stockList.some(s => s.symbol === sym)) {
                        setCompareSymbols(prev => [...prev, sym]);
                        wsClient.send('GetOHLCV', { symbol: sym, timeframe: chartTimeframe });
                      }
                      setCompareInput('');
                    }
                  }}
                  placeholder="+ Compare"
                  style={{
                    width: '80px', height: '22px', background: 'var(--bg-tertiary)',
                    border: '1px solid var(--border)', borderRadius: '3px',
                    color: 'var(--text-primary)', fontSize: '10px', padding: '0 6px',
                    fontFamily: 'var(--font-mono)',
                  }}
                />
              </div>
            )}
          </div>
        </div>
        {indicators && (
          <div style={{ display: 'flex', gap: '12px', fontSize: '11px' }}>
            {indicators.sma20 && <span style={{ color: 'var(--warning)' }}>-- SMA 20</span>}
            {indicators.sma50 && <span style={{ color: 'var(--chart-purple)' }}>-- SMA 50</span>}
            {indicators.sma200 && <span style={{ color: 'var(--chart-pink)' }}>-- SMA 200</span>}
            {indicators.bollingerUpper && <span style={{ color: 'var(--chart-blue)' }}>-- Bollinger</span>}
            <span style={{ color: '#A78BFA' }}>-- RSI 14</span>
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
          height={550}
          compareStocks={compareSymbols.map((sym, i) => ({
            symbol: sym,
            data: ohlcvData.get(sym) || [],
            color: ['#F97316', '#A855F7', '#14B8A6', '#F43F5E', '#84CC16'][i % 5],
          })).filter(cs => cs.data.length > 0)}
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

      {/* Technical Summary Card */}
      {indicators && chartData.length > 0 && (
        <div style={{ display: 'flex', gap: '8px', marginTop: '8px', marginBottom: '8px' }}>
          {/* Trend */}
          {(() => {
            const last = chartData[chartData.length - 1];
            const sma20 = indicators.sma20?.[indicators.sma20.length - 1]?.value;
            const sma50 = indicators.sma50?.[indicators.sma50.length - 1]?.value;
            const sma200 = indicators.sma200?.[indicators.sma200.length - 1]?.value;
            const aboveSMA20 = sma20 ? last.close > sma20 : undefined;
            const aboveSMA50 = sma50 ? last.close > sma50 : undefined;
            const aboveSMA200 = sma200 ? last.close > sma200 : undefined;
            const bullCount = [aboveSMA20, aboveSMA50, aboveSMA200].filter(v => v === true).length;
            const totalCount = [aboveSMA20, aboveSMA50, aboveSMA200].filter(v => v !== undefined).length;
            const trend = totalCount === 0 ? 'N/A' : bullCount >= 2 ? 'Bullish' : bullCount === 1 ? 'Neutral' : 'Bearish';
            const trendColor = trend === 'Bullish' ? 'var(--green-primary)' : trend === 'Bearish' ? 'var(--red-primary)' : 'var(--text-secondary)';

            // RSI approximation from recent closes
            const closes = chartData.slice(-15).map(d => d.close);
            let rsi = 50;
            if (closes.length >= 14) {
              let gains = 0, losses = 0;
              for (let i = 1; i < closes.length; i++) {
                const diff = closes[i] - closes[i - 1];
                if (diff > 0) gains += diff; else losses -= diff;
              }
              const avgGain = gains / 14;
              const avgLoss = losses / 14;
              const rs = avgLoss === 0 ? 100 : avgGain / avgLoss;
              rsi = 100 - 100 / (1 + rs);
            }
            const rsiColor = rsi > 70 ? 'var(--red-primary)' : rsi < 30 ? 'var(--green-primary)' : 'var(--text-primary)';
            const rsiLabel = rsi > 70 ? 'Overbought' : rsi < 30 ? 'Oversold' : 'Neutral';

            return (
              <>
                <div style={{
                  flex: 1, background: 'var(--bg-secondary)', border: '1px solid var(--border)',
                  borderRadius: '6px', padding: '8px 12px', display: 'flex', justifyContent: 'space-between', alignItems: 'center',
                }}>
                  <div>
                    <span style={{ fontSize: '10px', color: 'var(--text-disabled)', textTransform: 'uppercase', letterSpacing: '0.5px' }}>Trend</span>
                    <div style={{ fontWeight: 700, color: trendColor, fontSize: '13px' }}>{trend}</div>
                  </div>
                  <div style={{ fontSize: '10px', color: 'var(--text-disabled)', textAlign: 'right' }}>
                    {sma20 !== undefined && <div>SMA20: <span className="mono" style={{ color: aboveSMA20 ? 'var(--green-primary)' : 'var(--red-primary)' }}>{aboveSMA20 ? 'Above' : 'Below'}</span></div>}
                    {sma50 !== undefined && <div>SMA50: <span className="mono" style={{ color: aboveSMA50 ? 'var(--green-primary)' : 'var(--red-primary)' }}>{aboveSMA50 ? 'Above' : 'Below'}</span></div>}
                  </div>
                </div>
                <div style={{
                  flex: 1, background: 'var(--bg-secondary)', border: '1px solid var(--border)',
                  borderRadius: '6px', padding: '8px 12px', display: 'flex', justifyContent: 'space-between', alignItems: 'center',
                }}>
                  <div>
                    <span style={{ fontSize: '10px', color: 'var(--text-disabled)', textTransform: 'uppercase', letterSpacing: '0.5px' }}>RSI (14)</span>
                    <div className="mono" style={{ fontWeight: 700, color: rsiColor, fontSize: '15px' }}>{rsi.toFixed(0)}</div>
                  </div>
                  <div style={{ fontSize: '11px', color: rsiColor, fontWeight: 600 }}>{rsiLabel}</div>
                </div>
                <div style={{
                  flex: 1, background: 'var(--bg-secondary)', border: '1px solid var(--border)',
                  borderRadius: '6px', padding: '8px 12px',
                }}>
                  <span style={{ fontSize: '10px', color: 'var(--text-disabled)', textTransform: 'uppercase', letterSpacing: '0.5px' }}>Day Range</span>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '4px', marginTop: '2px' }}>
                    <span className="mono" style={{ fontSize: '10px', color: 'var(--text-disabled)' }}>${stock.dayLow?.toFixed(2) ?? '—'}</span>
                    <div style={{ flex: 1, height: '4px', borderRadius: '2px', background: 'var(--bg-tertiary)', position: 'relative', overflow: 'hidden' }}>
                      {stock.dayLow !== undefined && stock.dayHigh !== undefined && stock.dayHigh > stock.dayLow && (
                        <div style={{
                          position: 'absolute', top: 0, bottom: 0,
                          left: `${((stock.price - stock.dayLow) / (stock.dayHigh - stock.dayLow)) * 100}%`,
                          width: '3px', borderRadius: '2px', background: 'var(--text-accent)',
                        }} />
                      )}
                    </div>
                    <span className="mono" style={{ fontSize: '10px', color: 'var(--text-disabled)' }}>${stock.dayHigh?.toFixed(2) ?? '—'}</span>
                  </div>
                </div>
                {/* MACD */}
                {(() => {
                  const closes2 = chartData.map(d => d.close);
                  if (closes2.length < 26) return null;
                  // EMA helper
                  const ema = (data: number[], period: number) => {
                    const k = 2 / (period + 1);
                    const result = [data[0]];
                    for (let i = 1; i < data.length; i++) result.push(data[i] * k + result[i - 1] * (1 - k));
                    return result;
                  };
                  const ema12 = ema(closes2, 12);
                  const ema26 = ema(closes2, 26);
                  const macdLine = ema12.map((v, i) => v - ema26[i]);
                  const signal = ema(macdLine.slice(26), 9);
                  const macd = macdLine[macdLine.length - 1];
                  const sig = signal[signal.length - 1];
                  const hist = macd - sig;
                  const macdColor = hist >= 0 ? 'var(--green-primary)' : 'var(--red-primary)';
                  return (
                    <div style={{
                      flex: 1, background: 'var(--bg-secondary)', border: '1px solid var(--border)',
                      borderRadius: '6px', padding: '8px 12px', display: 'flex', justifyContent: 'space-between', alignItems: 'center',
                    }}>
                      <div>
                        <span style={{ fontSize: '10px', color: 'var(--text-disabled)', textTransform: 'uppercase', letterSpacing: '0.5px' }}>MACD</span>
                        <div className="mono" style={{ fontWeight: 700, color: macdColor, fontSize: '13px' }}>
                          {hist >= 0 ? '+' : ''}{hist.toFixed(2)}
                        </div>
                      </div>
                      <div style={{ fontSize: '10px', color: 'var(--text-disabled)', textAlign: 'right' }}>
                        <div>Line: <span className="mono" style={{ color: 'var(--text-primary)' }}>{macd.toFixed(2)}</span></div>
                        <div>Signal: <span className="mono" style={{ color: 'var(--chart-orange)' }}>{sig.toFixed(2)}</span></div>
                      </div>
                    </div>
                  );
                })()}
              </>
            );
          })()}
        </div>
      )}

      {/* Commodity ETF Info */}
      {stock.traits?.includes('Commodity ETF') && (
        <div style={{
          marginTop: '12px', padding: '10px 14px',
          background: 'rgba(245,158,11,0.06)', border: '1px solid rgba(245,158,11,0.15)',
          borderRadius: '6px', fontSize: '12px', color: 'var(--text-secondary)', lineHeight: 1.5,
        }}>
          <span style={{ fontWeight: 700, color: 'var(--warning)' }}>Commodity ETF</span> — This fund tracks {
            stock.symbol === 'GLD' ? 'the gold spot price. Gold is a safe-haven asset that typically rises during economic uncertainty.' :
            stock.symbol === 'SLV' ? 'the silver spot price. Silver moves with gold but is more volatile due to industrial demand.' :
            stock.symbol === 'USO' ? 'West Texas Intermediate crude oil. Oil prices are driven by OPEC decisions, geopolitics, and economic growth.' :
            'a commodity index.'
          } Price updates are driven by the Economic Engine, not constituent stocks.
        </div>
      )}

      {/* ETF Holdings */}
      {stockFundamentals?.etfConstituents && stockFundamentals.etfConstituents.length > 0 && (
        <div style={{ marginTop: '16px' }}>
          <h3 style={{ ...styles.heading, marginBottom: '8px' }}>Holdings ({stockFundamentals.etfConstituents.length} stocks)</h3>
          <div style={styles.tableContainer}>
            <table style={styles.table}>
              <thead>
                <tr>
                  <th style={styles.th}>Symbol</th>
                  <th style={styles.th}>Name</th>
                  <th style={styles.th}>Sector</th>
                  <th style={{ ...styles.th, textAlign: 'right' }}>Price</th>
                  <th style={{ ...styles.th, textAlign: 'right' }}>Change</th>
                  <th style={{ ...styles.th, textAlign: 'right' }}>Weight</th>
                </tr>
              </thead>
              <tbody>
                {stockFundamentals.etfConstituents.map((c: { symbol: string; name: string; sector: string; price: number; changePercent: number; weight: number }) => (
                  <tr key={c.symbol} style={styles.tr} onClick={() => selectStock(c.symbol)}
                    onMouseEnter={e => e.currentTarget.style.background = 'var(--bg-tertiary)'}
                    onMouseLeave={e => e.currentTarget.style.background = 'transparent'}>
                    <td className="mono" style={{ ...styles.td, fontWeight: 700, cursor: 'pointer', color: 'var(--text-accent)' }}>{c.symbol}</td>
                    <td style={{ ...styles.td, fontSize: '11px', color: 'var(--text-secondary)' }}>{c.name}</td>
                    <td style={{ ...styles.td, fontSize: '11px', color: 'var(--text-disabled)' }}>{c.sector}</td>
                    <td className="mono" style={{ ...styles.td, textAlign: 'right' }}>${c.price.toFixed(2)}</td>
                    <td className={`mono ${c.changePercent >= 0 ? 'positive' : 'negative'}`} style={{ ...styles.td, textAlign: 'right' }}>
                      {c.changePercent >= 0 ? '+' : ''}{c.changePercent.toFixed(2)}%
                    </td>
                    <td className="mono" style={{ ...styles.td, textAlign: 'right', color: 'var(--text-disabled)' }}>{c.weight.toFixed(1)}%</td>
                  </tr>
                ))}
              </tbody>
            </table>
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

      {/* Fundamentals Panel — grouped into collapsible sections */}
      {stockFundamentals && stockFundamentals.symbol === selectedSymbol && (() => {
        const f = stockFundamentals;
        const fmtM = (v: number) => v >= 1e9 ? `$${(v / 1e9).toFixed(1)}B` : v <= -1e9 ? `-$${(Math.abs(v) / 1e9).toFixed(1)}B` : `$${(v / 1e6).toFixed(0)}M`;
        const upside = ((f.targetPrice - stock.price) / stock.price * 100).toFixed(1);

        const sections: { title: string; key: string; items: [string, string, string?][] }[] = [
          { title: 'Core Metrics', key: 'core', items: [
            ['P/E Ratio', f.peRatio > 0 ? f.peRatio.toFixed(1) : 'N/A'],
            ['Market Cap', fmtM(f.marketCap)],
            ['Revenue', fmtM(f.revenue)],
            ['Net Income', fmtM(f.netIncome), f.netIncome >= 0 ? 'var(--green-primary)' : 'var(--red-primary)'],
            ['Rev Growth', `${(f.revenueGrowth * 100).toFixed(1)}%`, f.revenueGrowth >= 0 ? 'var(--green-primary)' : 'var(--red-primary)'],
            ['Div Yield', f.dividendYield > 0 ? `${(f.dividendYield * 100).toFixed(2)}%` : '-'],
          ]},
          { title: 'Valuation', key: 'valuation', items: [
            ['Fair Value', `$${f.fairValue.toFixed(2)}`],
            ['Debt/Equity', f.debtToEquity.toFixed(2), f.debtToEquity > 2 ? 'var(--red-primary)' : undefined],
          ]},
          { title: 'Analyst Coverage', key: 'analyst', items: [
            ['Consensus', f.analystConsensus, f.analystConsensus === 'Strong Buy' || f.analystConsensus === 'Buy' ? 'var(--green-primary)' : f.analystConsensus === 'Sell' || f.analystConsensus === 'Strong Sell' ? 'var(--red-primary)' : undefined],
            ['Rating', `${f.analystRating.toFixed(1)}/5.0`, f.analystRating >= 3.5 ? 'var(--green-primary)' : f.analystRating < 2.5 ? 'var(--red-primary)' : undefined],
            ['Target', `$${f.targetPrice.toFixed(2)}`, Number(upside) >= 0 ? 'var(--green-primary)' : 'var(--red-primary)'],
            ['Upside', `${upside}%`, Number(upside) >= 0 ? 'var(--green-primary)' : 'var(--red-primary)'],
          ]},
          { title: 'Ownership & Trading', key: 'ownership', items: [
            ['Insider Own', `${(f.insiderOwnership * 100).toFixed(1)}%`],
            ['Inst. Own', `${(f.institutionalOwnership * 100).toFixed(1)}%`],
            ['Avg Volume', f.averageVolume >= 1e6 ? `${(f.averageVolume / 1e6).toFixed(1)}M` : `${(f.averageVolume / 1e3).toFixed(0)}K`],
            ['Liquidity', `${f.liquidityScore}/10`],
            ['Employees', f.employees >= 1000 ? `${(f.employees / 1000).toFixed(1)}K` : String(f.employees)],
          ]},
          { title: 'Technical', key: 'technical', items: [
            ['Day Range', `$${f.dayLow.toFixed(2)} – $${f.dayHigh.toFixed(2)}`],
            ['Volatility', `${(f.baseVolatility * 100).toFixed(1)}%`],
            ...(stock.subsector ? [['Subsector', stock.subsector] as [string, string]] : []),
          ]},
        ];

        return (
          <div style={{ marginTop: '16px' }}>
            <h3 style={{ ...styles.heading, marginBottom: '8px' }}>Fundamentals</h3>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
              {sections.map(section => {
                const isCollapsed = collapsedSections.has(section.key);
                return (
                  <div key={section.key} style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border)', borderRadius: '6px', overflow: 'hidden' }}>
                    <button
                      onClick={() => toggleSection(section.key)}
                      style={{
                        width: '100%', display: 'flex', alignItems: 'center', gap: '6px',
                        padding: '8px 12px', background: 'none', border: 'none', cursor: 'pointer',
                        color: 'var(--text-secondary)', fontSize: '11px', fontWeight: 600,
                        fontFamily: 'var(--font-ui)', textTransform: 'uppercase', letterSpacing: '0.5px',
                      }}
                    >
                      {isCollapsed ? <ChevronRight size={14} /> : <ChevronDown size={14} />}
                      {section.title}
                      {!isCollapsed && (
                        <span style={{ marginLeft: 'auto', color: 'var(--text-disabled)', fontSize: '10px', fontWeight: 400, textTransform: 'none', letterSpacing: 0 }}>
                          {section.items.length} metrics
                        </span>
                      )}
                    </button>
                    {!isCollapsed && (
                      <div style={{ display: 'grid', gridTemplateColumns: `repeat(${Math.min(section.items.length, 3)}, 1fr)`, gap: '1px', padding: '0 8px 8px' }}>
                        {section.items.map(([label, value, color]) => (
                          <div key={label} style={{ padding: '6px 8px' }}>
                            <span style={{ fontSize: '10px', color: 'var(--text-disabled)', textTransform: 'uppercase', display: 'flex', alignItems: 'center', gap: 3 }}>
                              {label}
                              {FUND_HELP[label as string] && <HelpTip term={FUND_HELP[label as string]} size={10} />}
                            </span>
                            <span className="mono" style={{ fontSize: '13px', fontWeight: 600, color: color || 'var(--text-primary)' }}>{value}</span>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          </div>
        );
      })()}

      {/* Company Profile (Session 12) */}
      {stock.personality && (
        <div style={{ marginTop: '16px' }}>
          <h3 style={{ ...styles.heading, marginBottom: '8px' }}>Company Profile</h3>
          <div style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border)', borderRadius: '6px', padding: '14px' }}>
            <p style={{ fontSize: '13px', color: 'var(--text-secondary)', margin: '0 0 12px 0', lineHeight: '1.6' }}>
              {stock.personality.description}
            </p>

            {/* Product description */}
            {stock.personality.productDescription && (
              <p style={{ fontSize: '12px', color: 'var(--text-muted)', margin: '0 0 12px 0', lineHeight: '1.5' }}>
                {stock.personality.productDescription}
              </p>
            )}

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px', marginBottom: '12px' }}>
              {[
                ['CEO', `${stock.personality.ceoName} (${stock.personality.ceoArchetype})`],
                ['HQ', stock.personality.headquarters],
                ['Founded', String(stock.personality.foundedYear)],
                ['Flagship', stock.personality.flagshipProduct],
                ...(stock.personality.secondaryProduct ? [['Also Known For', stock.personality.secondaryProduct]] : []),
                ...(stock.personality.rivalSymbol ? [['Key Rival', stock.personality.rivalSymbol]] : []),
                ...(stock.personality.creditRating ? [['Credit Rating', stock.personality.creditRating]] : []),
                ...(stock.personality.keyMilestone ? [['Milestone', stock.personality.keyMilestone]] : []),
              ].map(([label, value]) => (
                <div key={label} style={{ display: 'flex', gap: '6px', fontSize: '12px' }}>
                  <span style={{ color: 'var(--text-disabled)', minWidth: '80px' }}>{label}</span>
                  <span style={{ color: 'var(--text-primary)' }}>{value}</span>
                </div>
              ))}
            </div>

            {/* Supply Chain */}
            {((stock.personality.suppliers?.length ?? 0) > 0 || (stock.personality.customers?.length ?? 0) > 0) && (
              <div style={{ marginBottom: '12px' }}>
                <div style={{ fontSize: '10px', color: 'var(--text-disabled)', textTransform: 'uppercase', letterSpacing: '0.5px', marginBottom: '4px' }}>Supply Chain</div>
                <div style={{ display: 'flex', gap: '16px' }}>
                  {stock.personality.suppliers && stock.personality.suppliers.length > 0 && (
                    <div>
                      <span style={{ fontSize: '10px', color: 'var(--text-disabled)' }}>Suppliers: </span>
                      {stock.personality.suppliers.map(s => (
                        <span key={s} className="mono" onClick={() => selectStock(s)} style={{
                          fontSize: '11px', fontWeight: 700, color: 'var(--text-accent)',
                          cursor: 'pointer', marginRight: '6px',
                          background: 'rgba(96,165,250,0.1)', padding: '1px 5px', borderRadius: '3px',
                        }}>{s}</span>
                      ))}
                    </div>
                  )}
                  {stock.personality.customers && stock.personality.customers.length > 0 && (
                    <div>
                      <span style={{ fontSize: '10px', color: 'var(--text-disabled)' }}>Customers: </span>
                      {stock.personality.customers.map(s => (
                        <span key={s} className="mono" onClick={() => selectStock(s)} style={{
                          fontSize: '11px', fontWeight: 700, color: 'var(--green-primary)',
                          cursor: 'pointer', marginRight: '6px',
                          background: 'rgba(16,185,129,0.1)', padding: '1px 5px', borderRadius: '3px',
                        }}>{s}</span>
                      ))}
                    </div>
                  )}
                </div>
              </div>
            )}

            {/* CEO Quote */}
            {stock.personality.ceoQuote && (
              <div style={{
                borderLeft: '2px solid var(--text-accent)', paddingLeft: '10px', marginBottom: '10px',
              }}>
                <p style={{ fontSize: '12px', color: 'var(--text-secondary)', fontStyle: 'italic', margin: '0 0 2px 0', lineHeight: '1.5' }}>
                  "{stock.personality.ceoQuote}"
                </p>
                <span style={{ fontSize: '10px', color: 'var(--text-disabled)' }}>
                  — {stock.personality.ceoName}, CEO
                </span>
              </div>
            )}

            <p style={{ fontSize: '11px', color: 'var(--text-disabled)', margin: '0', fontStyle: 'italic', lineHeight: '1.4' }}>
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
