import { useMarketStore } from '@/stores/marketStore';
import { OrderPanel } from '@/components/trading/OrderPanel';
import { WebSocketClient } from '@/services/websocket';

interface RightSidebarProps {
  wsClient: WebSocketClient;
}

export function RightSidebar({ wsClient }: RightSidebarProps) {
  const selectedSymbol = useMarketStore((s) => s.selectedSymbol);
  const stocks = useMarketStore((s) => s.stocks);
  const priceFlash = useMarketStore((s) => s.priceFlash);

  const stock = selectedSymbol ? stocks.get(selectedSymbol) : null;

  return (
    <aside style={styles.sidebar}>
      {/* Selected Stock Info */}
      <div style={styles.panel}>
        {stock ? (
          <div style={styles.stockInfo}>
            <div style={styles.symbolRow}>
              <span className="mono" style={styles.symbol}>{stock.symbol}</span>
              <span style={styles.sector}>{stock.sector}</span>
            </div>
            <span style={styles.name}>{stock.name}</span>
            <div style={styles.priceRow}>
              <span
                key={`price-${stock.price}`}
                className={`mono ${priceFlash.get(selectedSymbol!) === 'up' ? 'price-up' : priceFlash.get(selectedSymbol!) === 'down' ? 'price-down' : ''}`}
                style={styles.bigPrice}
              >${stock.price.toFixed(2)}</span>
              <span className={`mono ${stock.changePercent >= 0 ? 'positive' : 'negative'}`} style={styles.bigChange}>
                {stock.changePercent >= 0 ? '+' : ''}{stock.changePercent.toFixed(2)}%
                {stock.changePercent >= 0 ? ' ▲' : ' ▼'}
              </span>
            </div>
          </div>
        ) : (
          <div style={styles.empty}>Select a stock to trade</div>
        )}
      </div>

      {/* Order Panel */}
      <div style={styles.orderSection}>
        <div style={styles.panelHeader}>
          <span style={styles.panelTitle}>Order</span>
        </div>
        {stock ? (
          <OrderPanel stock={stock} wsClient={wsClient} />
        ) : (
          <div style={styles.orderPlaceholder}>No stock selected</div>
        )}
      </div>
    </aside>
  );
}

const styles: Record<string, React.CSSProperties> = {
  sidebar: {
    width: 'var(--sidebar-right-width)',
    background: 'var(--bg-secondary)',
    borderLeft: '1px solid var(--border)',
    display: 'flex',
    flexDirection: 'column',
    flexShrink: 0,
    overflowY: 'auto',
    overflowX: 'hidden',
  },
  panel: {
    borderBottom: '1px solid var(--border)',
  },
  stockInfo: {
    padding: 'var(--space-4)',
  },
  symbolRow: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 'var(--space-1)',
  },
  symbol: {
    fontWeight: 700,
    fontSize: '18px',
    color: 'var(--text-primary)',
  },
  sector: {
    fontSize: '10px',
    color: 'var(--text-secondary)',
    background: 'var(--bg-tertiary)',
    padding: '2px 6px',
    borderRadius: '4px',
  },
  name: {
    fontSize: '12px',
    color: 'var(--text-secondary)',
    display: 'block',
    marginBottom: 'var(--space-2)',
  },
  priceRow: {
    display: 'flex',
    alignItems: 'baseline',
    gap: 'var(--space-2)',
  },
  bigPrice: {
    fontSize: '24px',
    fontWeight: 700,
    color: 'var(--text-primary)',
    textShadow: '0 0 12px rgba(249, 250, 251, 0.15)',
    letterSpacing: '0.5px',
  },
  bigChange: {
    fontSize: '14px',
  },
  empty: {
    padding: '40px var(--space-4)',
    color: 'var(--text-disabled)',
    fontSize: '14px',
    textAlign: 'center',
  },
  orderSection: {
    borderBottom: '1px solid var(--border)',
    flex: 1,
  },
  panelHeader: {
    padding: '8px 12px',
    borderBottom: '1px solid var(--border)',
  },
  panelTitle: {
    fontWeight: 600,
    fontSize: '14px',
    color: 'var(--text-primary)',
  },
  orderPlaceholder: {
    padding: '20px 12px',
    color: 'var(--text-disabled)',
    fontSize: '13px',
    textAlign: 'center',
  },
};
