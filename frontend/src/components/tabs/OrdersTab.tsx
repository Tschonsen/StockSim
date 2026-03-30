import { useMarketStore } from '@/stores/marketStore';
import { WebSocketClient } from '@/services/websocket';
import { styles } from '@/styles/centralStyles';

interface OrdersTabProps {
  wsClient: WebSocketClient;
}

export function OrdersTab({ wsClient }: OrdersTabProps) {
  const orders = useMarketStore((s) => s.orders);

  return (
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
  );
}
