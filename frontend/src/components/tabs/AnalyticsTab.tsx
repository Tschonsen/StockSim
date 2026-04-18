import { useState, useEffect } from 'react';
import { useMarketStore } from '@/stores/marketStore';
import { WebSocketClient } from '@/services/websocket';
import { Trophy, TrendingUp, TrendingDown, Target, Shield } from 'lucide-react';
import { HelpTip } from '@/components/ui/HelpTip';
import { styles } from '@/styles/centralStyles';

interface AnalyticsTabProps {
  wsClient: WebSocketClient;
}

export function AnalyticsTab({ wsClient }: AnalyticsTabProps) {
  const analyticsData = useMarketStore((s) => s.analyticsData);
  const achievements = useMarketStore((s) => s.achievements);

  const [taxSummary, setTaxSummary] = useState<Record<string, number> | null>(null);

  // Listen for tax summary data
  useEffect(() => {
    const handler = (e: Event) => setTaxSummary((e as CustomEvent).detail as Record<string, number>);
    window.addEventListener('taxSummary', handler);
    return () => window.removeEventListener('taxSummary', handler);
  }, []);

  // Auto-fetch data when tab is mounted + periodic refresh
  useEffect(() => {
    wsClient.send('GetAnalytics', {});
    wsClient.send('GetAchievements', {});
    wsClient.send('GetTaxSummary', {});
    const interval = setInterval(() => {
      wsClient.send('GetAnalytics', {});
      wsClient.send('GetTaxSummary', {});
    }, 10_000);
    return () => clearInterval(interval);
  }, [wsClient]);

  return (
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
              <div style={{ ...styles.summaryCard, borderLeft: '3px solid var(--warning)' }}>
                <span style={styles.summaryLabel}>Win Rate <HelpTip term="Win Rate" size={10} /></span>
                <span className="mono" style={{ ...styles.summaryValue, color: a.winRate >= 50 ? 'var(--green-primary)' : 'var(--red-primary)' }}>
                  {a.winRate.toFixed(1)}%
                </span>
                <span className="mono" style={{ fontSize: '11px', color: 'var(--text-secondary)' }}>
                  {a.winningTrades}W / {a.losingTrades}L
                </span>
              </div>
              <div style={{ ...styles.summaryCard, borderLeft: '3px solid var(--chart-purple)' }}>
                <span style={styles.summaryLabel}>Max Drawdown <HelpTip term="Drawdown" size={10} /></span>
                <span className="mono" style={{ ...styles.summaryValue, color: 'var(--red-primary)' }}>
                  -{a.maxDrawdownPercent.toFixed(1)}%
                </span>
                <span className="mono" style={{ fontSize: '11px', color: 'var(--text-secondary)' }}>
                  Sharpe: {a.sharpeRatio.toFixed(2)}
                </span>
              </div>
            </div>

            {/* Trade Statistics Detail (Spec 6.3) */}
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
                  {a.profitFactor >= 999 ? '\u221E' : a.profitFactor.toFixed(2)}
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
                    <span style={{ color: 'var(--warning)' }}>-- Market (SIMX)</span>
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
                          <polyline points={marketPoints} fill="none" stroke="var(--warning)" strokeWidth="1" strokeOpacity="0.6" vectorEffect="non-scaling-stroke" />
                          {/* Portfolio */}
                          <polyline points={portfolioPoints} fill="none" stroke={color} strokeWidth="1.5" vectorEffect="non-scaling-stroke" />
                        </svg>
                        <div style={{ position: 'absolute', top: '8px', right: '12px', textAlign: 'right' }}>
                          <span className="mono" style={{ fontSize: '12px', color, fontWeight: 700 }}>
                            You: {lastPortfolio >= 0 ? '+' : ''}{lastPortfolio.toFixed(1)}%
                          </span>
                          <br />
                          <span className="mono" style={{ fontSize: '11px', color: 'var(--warning)' }}>
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
                        <span className="mono" style={{ fontSize: '10px', color: 'var(--text-primary)', fontWeight: 700 }}>
                          {analyticsData.analytics.winningTrades}W
                        </span>
                      </div>
                      <div style={{
                        flex: analyticsData.analytics.losingTrades,
                        background: 'var(--red-primary)', display: 'flex', alignItems: 'center', justifyContent: 'center',
                      }}>
                        <span className="mono" style={{ fontSize: '10px', color: 'var(--text-primary)', fontWeight: 700 }}>
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
                          color: a.profitFactor >= 1.5 ? 'var(--green-primary)' : a.profitFactor >= 1 ? 'var(--warning)' : 'var(--red-primary)',
                        }}>
                          {a.profitFactor > 100 ? 'INF' : a.profitFactor.toFixed(2)}
                        </span>
                      </div>
                      <div style={{ width: '1px', background: 'var(--border)' }} />
                      <div style={{ textAlign: 'center' }}>
                        <span style={{ fontSize: '9px', color: 'var(--text-disabled)' }}>SHARPE</span>
                        <span className="mono" style={{
                          display: 'block', fontSize: '22px', fontWeight: 700, marginTop: '4px',
                          color: a.sharpeRatio >= 1 ? 'var(--green-primary)' : a.sharpeRatio >= 0 ? 'var(--warning)' : 'var(--red-primary)',
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
                          <span style={{ color: ach.unlocked ? 'var(--gold-primary)' : 'var(--text-disabled)' }}>{categoryIcon}</span>
                          <span style={{
                            fontSize: '12px', fontWeight: 700,
                            color: ach.unlocked ? 'var(--gold-primary)' : 'var(--text-disabled)',
                          }}>
                            {ach.name}
                          </span>
                          {ach.unlocked && <Trophy size={12} style={{ color: 'var(--gold-primary)', marginLeft: 'auto' }} />}
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
  );
}
