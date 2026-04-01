import { useMarketStore } from '@/stores/marketStore';
import { styles } from '@/styles/centralStyles';

export function PortfolioTab() {
  const portfolio = useMarketStore((s) => s.portfolio);
  const stocks = useMarketStore((s) => s.stocks);
  const selectStock = useMarketStore((s) => s.selectStock);

  return (
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

          {/* Portfolio Concentration Warning */}
          {(() => {
            if (!portfolio.positions.length || portfolio.totalEquity <= 0) return null;
            const concentrated = portfolio.positions.find(
              (p) => Math.abs(p.marketValue) / portfolio.totalEquity > 0.5
            );
            if (!concentrated) return null;
            const pct = ((Math.abs(concentrated.marketValue) / portfolio.totalEquity) * 100).toFixed(0);
            return (
              <div style={{
                background: 'rgba(245,158,11,0.08)',
                borderLeft: '3px solid var(--warning)',
                borderRadius: '4px',
                padding: '8px 12px',
                marginTop: '12px',
                fontSize: '12px',
                color: 'var(--warning)',
              }}>
                Concentrated Position: {concentrated.symbol} is {pct}% of your portfolio. Consider diversifying.
              </div>
            );
          })()}

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
                    <span style={{ fontSize: '9px', color: 'var(--text-primary)', fontWeight: 700 }}>Cash</span>
                  )}
                </div>
                {/* Position segments */}
                {portfolio.positions.map((p, i) => {
                  const pct = Math.abs(p.marketValue) / portfolio.totalEquity;
                  const colors = ['var(--green-primary)', 'var(--chart-purple)', 'var(--warning)', 'var(--chart-pink)', '#06B6D4', 'var(--red-primary)', 'var(--chart-blue)', '#14B8A6'];
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
                        <span style={{ fontSize: '9px', color: 'var(--text-primary)', fontWeight: 700 }}>{p.symbol}</span>
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
              Technology: 'var(--info)', Energy: 'var(--warning)', Financials: 'var(--green-primary)',
              Healthcare: 'var(--chart-pink)', 'Consumer Goods': 'var(--chart-purple)', Industrials: 'var(--text-disabled)',
              Materials: '#D97706', 'Real Estate': '#14B8A6', Telecommunications: '#06B6D4',
              Utilities: '#84CC16', 'Luxury Goods': '#F43F5E', Transportation: '#A78BFA',
            };
            let cumulativeAngle = 0;
            const slices = sectors.map(([name, value]) => {
              const pct = value / total;
              const startAngle = cumulativeAngle;
              cumulativeAngle += pct * 360;
              return { name, value, pct, startAngle, endAngle: cumulativeAngle, color: sectorColors[name] || 'var(--text-disabled)' };
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
                    const bg = pnlPct > 5 ? 'var(--green-dark)' :
                               pnlPct > 2 ? 'var(--green-primary)' :
                               pnlPct > 0 ? 'rgba(16,185,129,0.5)' :
                               pnlPct > -2 ? 'rgba(239,68,68,0.5)' :
                               pnlPct > -5 ? 'var(--red-primary)' : 'var(--red-dark)';
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
                            <span className="mono" style={{ fontSize: '11px', fontWeight: 700, color: 'var(--text-primary)' }}>{p.symbol}</span>
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
                      <div style={{ ...styles.summaryCard, borderLeft: '3px solid var(--chart-purple)' }}>
                        <span style={styles.summaryLabel}>Net Exposure</span>
                        <span className="mono" style={{ fontSize: '14px', fontWeight: 700, color: netExposure >= 0 ? 'var(--green-primary)' : 'var(--red-primary)' }}>
                          ${Math.abs(netExposure).toFixed(0)}
                        </span>
                        <span className="mono" style={{ fontSize: '10px', color: 'var(--text-disabled)' }}>
                          L:${longExposure.toFixed(0)} S:${shortExposure.toFixed(0)}
                        </span>
                      </div>
                      <div style={{ ...styles.summaryCard, borderLeft: '3px solid var(--warning)' }}>
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
                    <th style={styles.th}>Sector</th>
                    <th style={{ ...styles.th, textAlign: 'right' }}>Shares</th>
                    <th style={{ ...styles.th, textAlign: 'right' }}>Price</th>
                    <th style={{ ...styles.th, textAlign: 'right' }}>Avg Cost</th>
                    <th style={{ ...styles.th, textAlign: 'right' }}>Mkt Value</th>
                    <th style={{ ...styles.th, textAlign: 'right' }}>P&L</th>
                    <th style={{ ...styles.th, textAlign: 'right' }}>P&L %</th>
                    <th style={{ ...styles.th, width: '60px' }}></th>
                  </tr>
                </thead>
                <tbody>
                  {portfolio.positions.map((p) => {
                    const stockInfo = stocks.get(p.symbol);
                    const isShort = p.shares < 0;
                    return (
                    <tr
                      key={p.symbol}
                      style={styles.tr}
                      onClick={() => selectStock(p.symbol)}
                      onMouseEnter={(e) => (e.currentTarget.style.background = 'var(--bg-tertiary)')}
                      onMouseLeave={(e) => (e.currentTarget.style.background = 'transparent')}
                    >
                      <td className="mono" style={{ ...styles.td, fontWeight: 700 }}>
                        {p.symbol}
                        {isShort && <span style={{ fontSize: '9px', color: 'var(--warning)', marginLeft: '4px', fontWeight: 600 }}>SHORT</span>}
                      </td>
                      <td style={{ ...styles.td, fontSize: '10px', color: 'var(--text-disabled)' }}>{stockInfo?.sector ?? ''}</td>
                      <td className="mono" style={{ ...styles.td, textAlign: 'right' }}>{Math.abs(p.shares)}</td>
                      <td className="mono" style={{ ...styles.td, textAlign: 'right', color: 'var(--text-secondary)' }}>
                        ${stockInfo?.price.toFixed(2) ?? '—'}
                      </td>
                      <td className="mono" style={{ ...styles.td, textAlign: 'right' }}>${p.averageCost.toFixed(2)}</td>
                      <td className="mono" style={{ ...styles.td, textAlign: 'right' }}>${Math.abs(p.marketValue).toFixed(0)}</td>
                      <td className="mono" style={{
                        ...styles.td,
                        textAlign: 'right',
                        color: p.unrealizedPnL >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                        fontWeight: 600,
                      }}>
                        {p.unrealizedPnL >= 0 ? '+' : ''}${p.unrealizedPnL.toFixed(0)}
                      </td>
                      <td className="mono" style={{
                        ...styles.td,
                        textAlign: 'right',
                        color: p.unrealizedPnLPercent >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                      }}>
                        {p.unrealizedPnLPercent >= 0 ? '+' : ''}{p.unrealizedPnLPercent.toFixed(1)}%
                      </td>
                      <td style={{ ...styles.td, textAlign: 'center' }}>
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            selectStock(p.symbol);
                            // Dispatch trading shortcut to set order panel to Sell/Cover
                            setTimeout(() => {
                              window.dispatchEvent(new CustomEvent('tradingShortcut', { detail: isShort ? 'Cover' : 'Sell' }));
                            }, 100);
                          }}
                          style={{
                            background: 'transparent', border: `1px solid ${isShort ? 'var(--text-accent)' : 'var(--red-primary)'}`,
                            color: isShort ? 'var(--text-accent)' : 'var(--red-primary)',
                            borderRadius: '4px', padding: '2px 8px',
                            fontSize: '10px', cursor: 'pointer', fontWeight: 600,
                          }}
                          title={isShort ? 'Cover short position' : 'Sell position'}
                        >{isShort ? 'Cover' : 'Close'}</button>
                      </td>
                    </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          ) : (
            <div style={{ color: 'var(--text-disabled)', fontSize: '14px' }}>
              No open positions. Select a stock and place a buy order to get started.
            </div>
          )}

          {/* Tax-Loss Harvesting Suggestions (December only) */}
          {(() => {
            const gameTime = useMarketStore.getState().gameTime;
            const month = gameTime ? new Date(gameTime).getMonth() : -1;
            if (month !== 11) return null; // December only
            const losers = portfolio.positions
              .filter(p => p.unrealizedPnL < 0)
              .sort((a, b) => a.unrealizedPnL - b.unrealizedPnL);
            if (losers.length === 0) return null;
            const totalLoss = losers.reduce((s, p) => s + Math.abs(p.unrealizedPnL), 0);
            return (
              <div style={{ marginTop: '16px', marginBottom: '8px' }}>
                <h3 style={{ ...styles.heading, marginBottom: '8px', color: 'var(--warning)' }}>
                  Tax-Loss Harvesting Opportunity
                </h3>
                <div style={{
                  background: 'rgba(245,158,11,0.06)', border: '1px solid rgba(245,158,11,0.2)',
                  borderRadius: '6px', padding: '10px 14px', marginBottom: '8px', fontSize: '12px',
                  color: 'var(--text-secondary)', lineHeight: 1.5,
                }}>
                  It's December — consider selling losing positions to offset capital gains.
                  You have <span className="mono" style={{ color: 'var(--red-primary)', fontWeight: 700 }}>${totalLoss.toFixed(0)}</span> in unrealized losses across {losers.length} position{losers.length > 1 ? 's' : ''}.
                </div>
                {losers.slice(0, 5).map(p => (
                  <div key={p.symbol} onClick={() => selectStock(p.symbol)} style={{
                    display: 'flex', justifyContent: 'space-between', alignItems: 'center',
                    padding: '5px 10px', fontSize: '12px', cursor: 'pointer',
                    borderBottom: '1px solid rgba(31,41,55,0.2)',
                  }}>
                    <span className="mono" style={{ fontWeight: 700 }}>{p.symbol}</span>
                    <span className="mono" style={{ color: 'var(--red-primary)', fontWeight: 600 }}>
                      ${p.unrealizedPnL.toFixed(0)} ({p.unrealizedPnLPercent.toFixed(1)}%)
                    </span>
                  </div>
                ))}
              </div>
            );
          })()}

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
  );
}
