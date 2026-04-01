import { useEffect } from 'react';
import { useMarketStore } from '@/stores/marketStore';
import { WebSocketClient } from '@/services/websocket';
import { styles } from '@/styles/centralStyles';

interface JournalTabProps {
  wsClient: WebSocketClient;
}

export function JournalTab({ wsClient }: JournalTabProps) {
  const tradeJournal = useMarketStore((s) => s.tradeJournal);
  const selectStock = useMarketStore((s) => s.selectStock);

  // Auto-fetch data when tab is mounted + periodic refresh
  useEffect(() => {
    wsClient.send('GetTradeJournal', {});
    const interval = setInterval(() => wsClient.send('GetTradeJournal', {}), 10_000);
    return () => clearInterval(interval);
  }, [wsClient]);

  return (
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

          {/* Cumulative P&L Curve + P&L Distribution */}
          {tradeJournal.length > 1 && (
            <div style={{ display: 'flex', gap: '12px', marginBottom: '16px' }}>
              {/* Cumulative P&L Curve */}
              <div style={{ flex: 2, background: 'var(--bg-secondary)', border: '1px solid var(--border)', borderRadius: '6px', padding: '10px 12px' }}>
                <span style={{ fontSize: '10px', color: 'var(--text-disabled)', letterSpacing: '1px', textTransform: 'uppercase' }}>Cumulative P&L</span>
                {(() => {
                  let cum = 0;
                  const points = tradeJournal.map(t => { cum += t.pnl; return cum; });
                  const min = Math.min(0, ...points);
                  const max = Math.max(0, ...points);
                  const range = max - min || 1;
                  const h = 80;
                  const w = 100;
                  const polyPoints = points.map((v, i) => `${(i / (points.length - 1)) * w},${h - ((v - min) / range) * h}`).join(' ');
                  const zeroY = h - ((0 - min) / range) * h;
                  const lastVal = points[points.length - 1];
                  const color = lastVal >= 0 ? '#10B981' : '#EF4444';
                  return (
                    <div style={{ position: 'relative', height: `${h}px`, marginTop: '4px' }}>
                      <svg viewBox={`0 0 ${w} ${h}`} style={{ width: '100%', height: '100%' }} preserveAspectRatio="none">
                        <line x1="0" y1={zeroY} x2={w} y2={zeroY} stroke="rgba(255,255,255,0.1)" strokeWidth="0.5" vectorEffect="non-scaling-stroke" />
                        <polyline points={polyPoints} fill="none" stroke={color} strokeWidth="1.5" vectorEffect="non-scaling-stroke" />
                      </svg>
                      <span className="mono" style={{ position: 'absolute', top: '2px', right: '4px', fontSize: '12px', fontWeight: 700, color }}>
                        {lastVal >= 0 ? '+' : ''}${lastVal.toFixed(0)}
                      </span>
                    </div>
                  );
                })()}
              </div>

              {/* P&L Distribution Histogram */}
              <div style={{ flex: 1, background: 'var(--bg-secondary)', border: '1px solid var(--border)', borderRadius: '6px', padding: '10px 12px' }}>
                <span style={{ fontSize: '10px', color: 'var(--text-disabled)', letterSpacing: '1px', textTransform: 'uppercase' }}>P&L Distribution</span>
                {(() => {
                  // Bucket trades into P&L ranges
                  const buckets = [
                    { label: '>+10%', min: 10, max: Infinity, count: 0 },
                    { label: '+5-10%', min: 5, max: 10, count: 0 },
                    { label: '+1-5%', min: 1, max: 5, count: 0 },
                    { label: '0-1%', min: 0, max: 1, count: 0 },
                    { label: '-1-0%', min: -1, max: 0, count: 0 },
                    { label: '-5--1%', min: -5, max: -1, count: 0 },
                    { label: '<-5%', min: -Infinity, max: -5, count: 0 },
                  ];
                  tradeJournal.forEach(t => {
                    for (const b of buckets) {
                      if (t.pnlPercent >= b.min && t.pnlPercent < b.max) { b.count++; break; }
                    }
                  });
                  const maxCount = Math.max(...buckets.map(b => b.count), 1);
                  return (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '2px', marginTop: '4px' }}>
                      {buckets.map(b => (
                        <div key={b.label} style={{ display: 'flex', alignItems: 'center', gap: '4px', fontSize: '10px' }}>
                          <span style={{ width: '50px', color: 'var(--text-disabled)', textAlign: 'right', flexShrink: 0 }}>{b.label}</span>
                          <div style={{ flex: 1, height: '8px', borderRadius: '2px', background: 'var(--bg-tertiary)' }}>
                            <div style={{
                              width: `${(b.count / maxCount) * 100}%`,
                              height: '100%', borderRadius: '2px',
                              background: b.min >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                              transition: 'width 0.3s',
                            }} />
                          </div>
                          <span className="mono" style={{ width: '16px', color: 'var(--text-disabled)', textAlign: 'right' }}>{b.count}</span>
                        </div>
                      ))}
                    </div>
                  );
                })()}
              </div>
            </div>
          )}

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
  );
}
