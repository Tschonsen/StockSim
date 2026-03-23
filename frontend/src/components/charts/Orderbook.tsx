/**
 * Orderbook visualization component. Bible 12.3.
 * Shows 10 bid/ask levels with depth bars.
 */

interface OrderbookLevel {
  price: number;
  quantity: number;
}

interface OrderbookProps {
  bids: OrderbookLevel[];
  asks: OrderbookLevel[];
  bestBid: number;
  bestAsk: number;
  spread: number;
  spreadPercent: number;
}

export function Orderbook({ bids, asks, bestBid, bestAsk, spread, spreadPercent }: OrderbookProps) {
  const maxQty = Math.max(
    ...bids.map(b => b.quantity),
    ...asks.map(a => a.quantity),
    1
  );

  return (
    <div style={styles.container}>
      <div style={styles.header}>
        <span style={{ color: 'var(--green-primary)', fontWeight: 600 }}>BIDS (Buy)</span>
        <span style={{ color: 'var(--red-primary)', fontWeight: 600 }}>ASKS (Sell)</span>
      </div>

      <div style={styles.book}>
        {/* Bid side */}
        <div style={styles.side}>
          {bids.map((level, i) => (
            <div key={i} style={styles.row}>
              <div style={{
                ...styles.bar,
                background: 'var(--green-dim)',
                width: `${(level.quantity / maxQty) * 100}%`,
                right: 0,
                left: 'auto',
              }} />
              <span className="mono" style={styles.qty}>{level.quantity.toLocaleString()}</span>
              <span className="mono" style={{ ...styles.price, color: 'var(--green-primary)' }}>
                ${level.price.toFixed(2)}
              </span>
            </div>
          ))}
        </div>

        {/* Ask side */}
        <div style={styles.side}>
          {asks.map((level, i) => (
            <div key={i} style={styles.row}>
              <div style={{
                ...styles.bar,
                background: 'var(--red-dim)',
                width: `${(level.quantity / maxQty) * 100}%`,
                left: 0,
              }} />
              <span className="mono" style={{ ...styles.price, color: 'var(--red-primary)' }}>
                ${level.price.toFixed(2)}
              </span>
              <span className="mono" style={styles.qty}>{level.quantity.toLocaleString()}</span>
            </div>
          ))}
        </div>
      </div>

      {/* Spread */}
      <div style={styles.spreadRow}>
        <span className="mono" style={{ color: 'var(--green-primary)', fontSize: '13px' }}>
          Bid: ${bestBid.toFixed(2)}
        </span>
        <span className="mono" style={{ color: 'var(--text-secondary)', fontSize: '12px' }}>
          Spread: ${spread.toFixed(2)} ({spreadPercent.toFixed(2)}%)
        </span>
        <span className="mono" style={{ color: 'var(--red-primary)', fontSize: '13px' }}>
          Ask: ${bestAsk.toFixed(2)}
        </span>
      </div>
    </div>
  );
}

const styles: Record<string, React.CSSProperties> = {
  container: {
    background: 'var(--bg-primary)',
    border: '1px solid var(--border)',
    borderRadius: '6px',
    overflow: 'hidden',
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    padding: '8px 12px',
    borderBottom: '1px solid var(--border)',
    fontSize: '11px',
    textTransform: 'uppercase',
  },
  book: {
    display: 'flex',
    gap: '1px',
    background: 'var(--border)',
  },
  side: {
    flex: 1,
    display: 'flex',
    flexDirection: 'column',
    background: 'var(--bg-primary)',
  },
  row: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: '3px 8px',
    fontSize: '12px',
    position: 'relative',
    height: '24px',
  },
  bar: {
    position: 'absolute',
    top: 0,
    bottom: 0,
    opacity: 0.3,
  },
  price: {
    position: 'relative',
    zIndex: 1,
    fontWeight: 600,
  },
  qty: {
    position: 'relative',
    zIndex: 1,
    color: 'var(--text-secondary)',
    fontSize: '11px',
  },
  spreadRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: '8px 12px',
    borderTop: '1px solid var(--border)',
    background: 'var(--bg-secondary)',
  },
};
