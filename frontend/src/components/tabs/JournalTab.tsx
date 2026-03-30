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
