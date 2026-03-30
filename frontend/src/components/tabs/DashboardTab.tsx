import { useMemo, useEffect } from 'react';
import { useMarketStore } from '@/stores/marketStore';
import { WebSocketClient } from '@/services/websocket';
import { styles } from '@/styles/centralStyles';

interface DashboardTabProps {
  wsClient: WebSocketClient;
}

const getHeatColor = (pct: number) => {
  if (pct > 2) return 'var(--green-dark)';
  if (pct > 0.5) return 'var(--green-primary)';
  if (pct > 0) return 'rgba(16, 185, 129, 0.4)';
  if (pct > -0.5) return 'rgba(239, 68, 68, 0.4)';
  if (pct > -2) return 'var(--red-primary)';
  return 'var(--red-dark)';
};

export function DashboardTab({ wsClient }: DashboardTabProps) {
  const stockList = useMarketStore((s) => s.stockList);
  const stocks = useMarketStore((s) => s.stocks);
  const selectStock = useMarketStore((s) => s.selectStock);
  const setActiveTab = useMarketStore((s) => s.setActiveTab);
  const activeArcs = useMarketStore((s) => s.activeArcs);
  const economicData = useMarketStore((s) => s.economicData);
  const earningsCalendar = useMarketStore((s) => s.earningsCalendar);
  const vix = useMarketStore((s) => s.vix);
  const fearGreedLive = useMarketStore((s) => s.fearGreed);
  const marketPhase = useMarketStore((s) => s.marketPhase);
  const gameTime = useMarketStore((s) => s.gameTime);

  // Sector heatmap data
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
    })).sort((a, b) => b.avgChange - a.avgChange);
  }, [stockList]);

  // Top movers & market breadth
  const topMovers = useMemo(() => {
    const sorted = [...stockList].sort((a, b) => b.changePercent - a.changePercent);
    const byVolume = [...stockList].sort((a, b) => b.volume - a.volume);
    const tradable = stockList.filter(s => !s.traits.includes('ETF'));
    const abovePrev = tradable.filter(s => s.price > (s.previousClose || s.price) * 0.98).length;
    return {
      gainers: sorted.slice(0, 5),
      losers: sorted.slice(-5).reverse(),
      highVolume: byVolume.slice(0, 5),
      breadthPct: tradable.length > 0 ? (abovePrev / tradable.length * 100) : 50,
    };
  }, [stockList]);

  // Auto-fetch data when tab is selected
  useEffect(() => {
    wsClient.send('GetEconomicData', {});
    wsClient.send('GetEarningsCalendar', {});
  }, [wsClient]);

  // Navigate to market tab when sector is clicked
  const handleSectorClick = (_sectorName: string) => {
    // Original code used setFilterText + setActiveTab, but filterText is local to MarketTab now.
    // Just navigate to the market tab.
    setActiveTab('market');
  };

  return (
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
            onClick={() => handleSectorClick(sector.name)}
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
            <div style={{ fontSize: '11px', fontWeight: 700, color: 'var(--text-primary)', marginBottom: '2px' }}>
              {sector.name}
            </div>
            <div className="mono" style={{ fontSize: '14px', fontWeight: 700, color: 'var(--text-primary)' }}>
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
          {topMovers.gainers.map((s) => (
              <div key={s.symbol} style={{ ...styles.moverRow, cursor: 'pointer' }} onClick={() => selectStock(s.symbol)}>
                <span className="mono" style={styles.moverSymbol}>{s.symbol}</span>
                <span style={styles.moverName}>{s.name}</span>
                <span className="mono positive">{s.changePercent >= 0 ? '+' : ''}{s.changePercent.toFixed(2)}%</span>
              </div>
            ))}
        </div>
        <div>
          <h3 style={{ ...styles.moversTitle, color: 'var(--red-primary)' }}>Top Losers</h3>
          {topMovers.losers.map((s) => (
              <div key={s.symbol} style={{ ...styles.moverRow, cursor: 'pointer' }} onClick={() => selectStock(s.symbol)}>
                <span className="mono" style={styles.moverSymbol}>{s.symbol}</span>
                <span style={styles.moverName}>{s.name}</span>
                <span className="mono negative">{s.changePercent.toFixed(2)}%</span>
              </div>
            ))}
        </div>
        <div>
          <h3 style={styles.moversTitle}>Market Breadth</h3>
          <div style={{ background: 'var(--bg-secondary)', border: '1px solid var(--border)', borderRadius: '6px', padding: '8px 12px', marginBottom: '8px' }}>
            <span style={{ fontSize: '10px', color: 'var(--text-disabled)', display: 'block' }}>BREADTH</span>
            <div style={{ display: 'flex', height: '6px', borderRadius: '3px', overflow: 'hidden', margin: '4px 0', background: 'var(--bg-tertiary)' }}>
              <div style={{ width: `${topMovers.breadthPct}%`, background: 'var(--green-primary)', transition: 'width 300ms' }} />
            </div>
            <span className="mono" style={{ fontSize: '11px', color: 'var(--text-secondary)' }}>{topMovers.breadthPct.toFixed(0)}% positive</span>
          </div>
          <div style={{ fontSize: '11px', color: 'var(--text-disabled)', marginBottom: '4px' }}>HIGH VOLUME</div>
          {topMovers.highVolume.map(s => (
            <div key={s.symbol} style={{ ...styles.moverRow, cursor: 'pointer' }} onClick={() => selectStock(s.symbol)}>
              <span className="mono" style={styles.moverSymbol}>{s.symbol}</span>
              <span className="mono" style={{ fontSize: '11px', color: 'var(--text-secondary)' }}>
                {s.volume >= 1e6 ? `${(s.volume / 1e6).toFixed(1)}M` : `${(s.volume / 1e3).toFixed(0)}K`}
              </span>
            </div>
          ))}
        </div>
      </div>

      {/* Developing Stories (Active Arcs) */}
      {activeArcs.length > 0 && (
        <div style={{ marginBottom: '20px' }}>
          <h3 style={styles.moversTitle}>Developing Stories</h3>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(220px, 1fr))', gap: '8px' }}>
            {activeArcs.map((arc) => (
              <div
                key={arc.id}
                style={{
                  background: 'var(--bg-secondary)',
                  border: '1px solid var(--border)',
                  borderLeft: '3px solid var(--warning)',
                  borderRadius: '4px',
                  padding: '8px 10px',
                  cursor: arc.symbol ? 'pointer' : 'default',
                }}
                onClick={() => arc.symbol && selectStock(arc.symbol)}
              >
                <div style={{ fontSize: '12px', fontWeight: 600, color: 'var(--text-primary)', marginBottom: '4px' }}>
                  {arc.name}
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <span style={{ fontSize: '10px', color: 'var(--text-disabled)' }}>
                    {arc.sector ?? arc.symbol ?? ''}
                  </span>
                  <span style={{
                    fontSize: '9px', fontWeight: 700, padding: '1px 6px', borderRadius: '3px',
                    letterSpacing: '0.5px',
                    background: arc.phase === 1 ? 'rgba(245,158,11,0.15)' :
                               arc.phase === 2 ? 'rgba(245,158,11,0.25)' : 'rgba(239,68,68,0.2)',
                    color: arc.phase === 1 ? 'var(--warning)' :
                           arc.phase === 2 ? 'var(--chart-orange)' : 'var(--red-primary)',
                  }}>
                    PHASE {arc.phase}/3
                  </span>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Market Phase + VIX + Yield Curve */}
      {economicData && (
        <div style={{ display: 'flex', gap: '12px', marginBottom: '16px' }}>
          <div style={{
            flex: 1, background: 'var(--bg-secondary)', border: '1px solid var(--border)',
            borderRadius: '6px', padding: '12px', textAlign: 'center',
          }}>
            <span style={{ fontSize: '10px', color: 'var(--text-disabled)', letterSpacing: '1px', display: 'block' }}>MARKET PHASE</span>
            <span className="mono" style={{
              fontSize: '20px', fontWeight: 700, display: 'block', marginTop: '4px',
              color: marketPhase === 'Bull' ? 'var(--green-primary)' :
                     marketPhase === 'Bear' ? 'var(--red-primary)' : 'var(--warning)',
            }}>
              {marketPhase === 'Bull' ? '▲ BULL' : marketPhase === 'Bear' ? '▼ BEAR' : '◆ NEUTRAL'}
            </span>
            <span className="mono" style={{ fontSize: '11px', color: 'var(--text-disabled)' }}>
              Sentiment: {economicData.marketSentiment >= 0 ? '+' : ''}{economicData.marketSentiment.toFixed(2)}
            </span>
          </div>
          <div style={{
            flex: 1, background: 'var(--bg-secondary)', border: '1px solid var(--border)',
            borderRadius: '6px', padding: '12px', textAlign: 'center',
          }}>
            <span style={{ fontSize: '10px', color: 'var(--text-disabled)', letterSpacing: '1px', display: 'block' }}>VIX</span>
            <span className="mono" style={{
              fontSize: '24px', fontWeight: 700, display: 'block', marginTop: '4px',
              color: vix < 20 ? 'var(--green-primary)' :
                     vix < 30 ? 'var(--warning)' :
                     vix < 50 ? 'var(--chart-orange)' : 'var(--red-primary)',
            }}>
              {vix.toFixed(1)}
            </span>
            <span className="mono" style={{
              fontSize: '10px', fontWeight: 600,
              color: vix < 20 ? 'var(--green-primary)' :
                     vix < 30 ? 'var(--warning)' :
                     vix < 50 ? 'var(--chart-orange)' : 'var(--red-primary)',
            }}>
              {vix < 15 ? 'LOW VOL' : vix < 20 ? 'CALM' : vix < 30 ? 'ELEVATED' : vix < 50 ? 'HIGH' : 'EXTREME'}
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
                <span className="mono" style={{ display: 'block', fontSize: '14px', fontWeight: 700, color: 'var(--gold-primary)' }}>
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
            {(() => {
              const fg = fearGreedLive ?? economicData.fearGreedIndex;
              return (
                <div style={{
                  marginTop: '8px', background: 'var(--bg-secondary)', border: '1px solid var(--border)',
                  borderRadius: '6px', padding: '10px 12px', textAlign: 'center',
                }}>
                  <span style={{ fontSize: '10px', color: 'var(--text-disabled)', letterSpacing: '1px' }}>FEAR & GREED INDEX</span>
                  <div className="mono" style={{
                    fontSize: '32px', fontWeight: 700, marginTop: '4px',
                    color: fg > 60 ? 'var(--green-primary)' :
                           fg < 40 ? 'var(--red-primary)' : 'var(--warning)',
                  }}>{fg}</div>
                  <div style={{
                    fontSize: '11px', fontWeight: 600,
                    color: fg > 75 ? 'var(--green-primary)' :
                           fg > 60 ? '#34D399' :
                           fg > 40 ? 'var(--warning)' :
                           fg > 25 ? 'var(--chart-orange)' : 'var(--red-primary)',
                  }}>
                    {fg > 75 ? 'EXTREME GREED' :
                     fg > 60 ? 'GREED' :
                     fg > 40 ? 'NEUTRAL' :
                     fg > 25 ? 'FEAR' : 'EXTREME FEAR'}
                  </div>
                </div>
              );
            })()}
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
                      color: ev.impact === 'High' ? 'var(--red-primary)' : 'var(--warning)',
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
                  earningsCalendar.upcoming.slice(0, 8).map((e, i) => {
                    const stockInfo = stocks.get(e.symbol);
                    const reportDate = new Date(e.reportDate);
                    const now = gameTime ? new Date(gameTime) : new Date();
                    const diffDays = Math.round((reportDate.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));
                    const relDate = diffDays <= 0 ? 'Today' : diffDays === 1 ? 'Tomorrow' : `In ${diffDays} days`;
                    return (
                    <div key={`${e.symbol}-${e.reportDate}`} style={{
                      display: 'flex', justifyContent: 'space-between', alignItems: 'center',
                      padding: '5px 10px', fontSize: '11px', cursor: 'pointer',
                      borderBottom: i < 7 ? '1px solid rgba(31,41,55,0.3)' : 'none',
                    }} onClick={() => selectStock(e.symbol)}>
                      <div style={{ display: 'flex', flexDirection: 'column', minWidth: '100px' }}>
                        <span className="mono" style={{ fontWeight: 700, color: 'var(--text-primary)' }}>{e.symbol}</span>
                        {stockInfo && <span style={{ fontSize: '9px', color: 'var(--text-disabled)', lineHeight: 1.2 }}>{stockInfo.name}</span>}
                      </div>
                      <span style={{ color: 'var(--text-secondary)' }}>Q{e.quarter}</span>
                      <span className="mono" style={{ color: 'var(--text-secondary)' }}>
                        Est: ${e.expectedEPS.toFixed(2)}
                      </span>
                      <span style={{
                        color: diffDays <= 1 ? 'var(--warning)' : 'var(--text-disabled)',
                        fontWeight: diffDays <= 1 ? 600 : 400,
                      }}>
                        {relDate}
                      </span>
                    </div>
                    );
                  })
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
  );
}
