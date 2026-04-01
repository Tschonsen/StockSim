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

  const totalBidQty = bids.reduce((sum, b) => sum + b.quantity, 0);
  const totalAskQty = asks.reduce((sum, a) => sum + a.quantity, 0);
  const totalQty = totalBidQty + totalAskQty || 1;
  const bidPct = Math.round(totalBidQty / totalQty * 100);
  const askPct = 100 - bidPct;

  return (
    <div style={styles.container}>
      <div style={styles.header}>
        <span style={{ color: 'var(--green-primary)', fontWeight: 600 }}>BIDS (Buy)</span>
        {/* Imbalance bar */}
        <div style={{ flex: 1, display: 'flex', alignItems: 'center', gap: '4px', margin: '0 12px' }}>
          <span className="mono" style={{ fontSize: '10px', color: 'var(--green-primary)', fontWeight: 600 }}>{bidPct}%</span>
          <div style={{ flex: 1, height: '4px', borderRadius: '2px', background: 'var(--bg-tertiary)', overflow: 'hidden', display: 'flex' }}>
            <div style={{ width: `${bidPct}%`, background: 'var(--green-primary)', transition: 'width 0.3s' }} />
            <div style={{ width: `${askPct}%`, background: 'var(--red-primary)', transition: 'width 0.3s' }} />
          </div>
          <span className="mono" style={{ fontSize: '10px', color: 'var(--red-primary)', fontWeight: 600 }}>{askPct}%</span>
        </div>
        <span style={{ color: 'var(--red-primary)', fontWeight: 600 }}>ASKS (Sell)</span>
      </div>

      <div style={styles.book}>
        {/* Bid side */}
        <div style={styles.side}>
          {bids.map((level, i) => {
            const pct = level.quantity / maxQty;
            return (
              <div key={i} style={styles.row}>
                <div style={{
                  ...styles.bar,
                  background: `rgba(16,185,129,${0.1 + pct * 0.35})`,
                  width: `${pct * 100}%`,
                  right: 0,
                  left: 'auto',
                }} />
                <span className="mono" style={{ ...styles.qty, fontWeight: pct > 0.7 ? 700 : 400, color: pct > 0.7 ? 'var(--text-primary)' : 'var(--text-secondary)' }}>{level.quantity.toLocaleString()}</span>
                <span className="mono" style={{ ...styles.price, color: i === 0 ? 'var(--green-primary)' : 'var(--text-secondary)' }}>
                  ${level.price.toFixed(2)}
                </span>
              </div>
            );
          })}
        </div>

        {/* Ask side */}
        <div style={styles.side}>
          {asks.map((level, i) => {
            const pct = level.quantity / maxQty;
            return (
              <div key={i} style={styles.row}>
                <div style={{
                  ...styles.bar,
                  background: `rgba(239,68,68,${0.1 + pct * 0.35})`,
                  width: `${pct * 100}%`,
                  left: 0,
                }} />
                <span className="mono" style={{ ...styles.price, color: i === 0 ? 'var(--red-primary)' : 'var(--text-secondary)' }}>
                  ${level.price.toFixed(2)}
                </span>
                <span className="mono" style={{ ...styles.qty, fontWeight: pct > 0.7 ? 700 : 400, color: pct > 0.7 ? 'var(--text-primary)' : 'var(--text-secondary)' }}>{level.quantity.toLocaleString()}</span>
              </div>
            );
          })}
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
