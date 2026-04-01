import { useState, useMemo } from 'react';
import { useMarketStore } from '@/stores/marketStore';
import { WebSocketClient } from '@/services/websocket';
import { styles } from '@/styles/centralStyles';

interface OrdersTabProps {
  wsClient: WebSocketClient;
}

export function OrdersTab({ wsClient }: OrdersTabProps) {
  const orders = useMarketStore((s) => s.orders);
  const [statusFilter, setStatusFilter] = useState<string>('all');

  const filteredOrders = useMemo(() => {
    const reversed = [...orders].reverse();
    if (statusFilter === 'all') return reversed;
    return reversed.filter(o => o.status === statusFilter);
  }, [orders, statusFilter]);

  const stats = useMemo(() => {
    const open = orders.filter(o => o.status === 'Pending' || o.status === 'Open').length;
    const filled = orders.filter(o => o.status === 'Filled').length;
    const rejected = orders.filter(o => o.status === 'Rejected').length;
    const cancelled = orders.filter(o => o.status === 'Cancelled' || o.status === 'Expired').length;
    return { open, filled, rejected, cancelled };
  }, [orders]);

  return (
    <div style={styles.content}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '12px' }}>
        <h2 style={{ ...styles.heading, marginBottom: 0 }}>Orders ({orders.length})</h2>
        <div style={{ display: 'flex', gap: '4px' }}>
          {[
            { key: 'all', label: 'All', count: orders.length },
            { key: 'Open', label: 'Open', count: stats.open, color: 'var(--text-accent)' },
            { key: 'Filled', label: 'Filled', count: stats.filled, color: 'var(--green-primary)' },
            { key: 'Rejected', label: 'Rejected', count: stats.rejected, color: 'var(--red-primary)' },
          ].map(f => (
            <button key={f.key} onClick={() => setStatusFilter(f.key)} style={{
              padding: '4px 10px', border: 'none', borderRadius: '4px', cursor: 'pointer',
              fontSize: '11px', fontWeight: 600, fontFamily: 'var(--font-ui)',
              background: statusFilter === f.key ? 'var(--bg-primary)' : 'var(--bg-tertiary)',
              color: statusFilter === f.key ? (f.color || 'var(--text-accent)') : 'var(--text-disabled)',
            }}>{f.label} {f.count > 0 ? `(${f.count})` : ''}</button>
          ))}
        </div>
      </div>

      {/* Open Orders Summary */}
      {stats.open > 0 && (
        <div style={{
          background: 'rgba(96,165,250,0.06)', border: '1px solid rgba(96,165,250,0.15)',
          borderRadius: '6px', padding: '8px 12px', marginBottom: '12px',
          display: 'flex', justifyContent: 'space-between', alignItems: 'center',
          fontSize: '12px', color: 'var(--text-accent)',
        }}>
          <span><span style={{ fontWeight: 700 }}>{stats.open}</span> open order{stats.open > 1 ? 's' : ''} pending execution</span>
          <button onClick={() => {
            orders.filter(o => o.status === 'Pending' || o.status === 'Open').forEach(o => {
              wsClient.send('CancelOrder', { orderId: o.id });
            });
            setTimeout(() => wsClient.send('GetOrders', {}), 300);
          }} style={{
            background: 'transparent', border: '1px solid var(--red-primary)',
            color: 'var(--red-primary)', borderRadius: '4px', padding: '3px 10px',
            fontSize: '10px', cursor: 'pointer', fontWeight: 600,
          }}>Cancel All</button>
        </div>
      )}

      {filteredOrders.length > 0 ? (
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
              {filteredOrders.map((o) => (
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
  );
}
