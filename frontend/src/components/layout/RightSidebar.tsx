import { useMemo, useState } from 'react';
import { useMarketStore } from '@/stores/marketStore';
import { OrderPanel } from '@/components/trading/OrderPanel';
import { WebSocketClient } from '@/services/websocket';
import { TrendingUp, TrendingDown, BarChart3, MousePointerClick, PanelRightClose, PanelRightOpen } from 'lucide-react';

interface RightSidebarProps {
  wsClient: WebSocketClient;
}

export function RightSidebar({ wsClient }: RightSidebarProps) {
  const selectedSymbol = useMarketStore((s) => s.selectedSymbol);
  const stocks = useMarketStore((s) => s.stocks);
  const stockList = useMarketStore((s) => s.stockList);
  const priceFlash = useMarketStore((s) => s.priceFlash);
  const selectStock = useMarketStore((s) => s.selectStock);
  const [collapsed, setCollapsed] = useState(false);

  const stock = selectedSymbol ? stocks.get(selectedSymbol) : null;

  // Quick picks for empty state
  const quickPicks = useMemo(() => {
    if (stockList.length === 0) return { gainers: [], losers: [], active: [] };
    const tradable = stockList.filter(s => !s.traits.includes('ETF'));
    const sorted = [...tradable].sort((a, b) => b.changePercent - a.changePercent);
    const byVolume = [...tradable].sort((a, b) => b.volume - a.volume);
    return {
      gainers: sorted.slice(0, 3),
      losers: sorted.slice(-3).reverse(),
      active: byVolume.slice(0, 3),
    };
  }, [stockList]);

  if (collapsed) {
    return (
      <aside style={{ ...styles.sidebar, width: '40px', minWidth: '40px', alignItems: 'center', padding: '8px 0' }}>
        <button onClick={() => setCollapsed(false)} style={{
          background: 'none', border: 'none', color: 'var(--text-disabled)', cursor: 'pointer', padding: '4px',
        }} title="Expand trading panel"><PanelRightOpen size={16} /></button>
        {stock && (
          <div style={{ writingMode: 'vertical-rl', textOrientation: 'mixed', marginTop: '12px', fontSize: '11px', fontFamily: 'var(--font-mono)' }}>
            <span style={{ fontWeight: 700, color: 'var(--text-primary)' }}>{stock.symbol}</span>
            <span style={{ color: stock.changePercent >= 0 ? 'var(--green-primary)' : 'var(--red-primary)', fontWeight: 600, marginTop: '4px' }}>
              {stock.changePercent >= 0 ? '+' : ''}{stock.changePercent.toFixed(1)}%
            </span>
          </div>
        )}
      </aside>
    );
  }

  return (
    <aside style={styles.sidebar}>
      {/* Collapse toggle */}
      <div style={{ display: 'flex', justifyContent: 'flex-end', padding: '4px 8px 0' }}>
        <button onClick={() => setCollapsed(true)} style={{
          background: 'none', border: 'none', color: 'var(--text-disabled)', cursor: 'pointer', padding: '2px',
        }} title="Collapse trading panel"><PanelRightClose size={14} /></button>
      </div>
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
          <div style={styles.emptyState}>
            <MousePointerClick size={24} style={{ color: 'var(--text-disabled)', marginBottom: '8px' }} />
            <div style={{ color: 'var(--text-disabled)', fontSize: '13px', marginBottom: '16px' }}>
              Select a stock to trade
            </div>
            {quickPicks.gainers.length > 0 && (
              <>
                <QuickPickSection icon={<TrendingUp size={12} />} title="Top Gainers" items={quickPicks.gainers} onSelect={selectStock} />
                <QuickPickSection icon={<TrendingDown size={12} />} title="Top Losers" items={quickPicks.losers} onSelect={selectStock} />
                <QuickPickSection icon={<BarChart3 size={12} />} title="Most Active" items={quickPicks.active} onSelect={selectStock} />
              </>
            )}
          </div>
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

function QuickPickSection({ icon, title, items, onSelect }: {
  icon: React.ReactNode; title: string;
  items: { symbol: string; price: number; changePercent: number }[];
  onSelect: (s: string) => void;
}) {
  return (
    <div style={{ marginBottom: '12px', width: '100%' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: '4px', color: 'var(--text-disabled)', fontSize: '10px', fontWeight: 600, letterSpacing: '0.5px', marginBottom: '4px', textTransform: 'uppercase' as const }}>
        {icon} {title}
      </div>
      {items.map(s => (
        <div key={s.symbol} onClick={() => onSelect(s.symbol)} style={{
          display: 'flex', justifyContent: 'space-between', alignItems: 'center',
          padding: '4px 8px', cursor: 'pointer', borderRadius: '4px',
          fontSize: '12px', transition: 'background 150ms',
        }}
          onMouseEnter={e => e.currentTarget.style.background = 'var(--bg-tertiary)'}
          onMouseLeave={e => e.currentTarget.style.background = 'transparent'}
        >
          <span className="mono" style={{ fontWeight: 600, color: 'var(--text-primary)' }}>{s.symbol}</span>
          <span className="mono" style={{
            fontWeight: 600, fontSize: '11px',
            color: s.changePercent >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
          }}>
            {s.changePercent >= 0 ? '+' : ''}{s.changePercent.toFixed(2)}%
          </span>
        </div>
      ))}
    </div>
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
  emptyState: {
    padding: '20px var(--space-4)',
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
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
