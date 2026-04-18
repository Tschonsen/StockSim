import { useMarketStore } from '@/stores/marketStore';
import { WebSocketClient } from '@/services/websocket';
import { StockDetailView } from '@/components/tabs/StockDetailView';
import { DashboardTab } from '@/components/tabs/DashboardTab';
import { MarketTab } from '@/components/tabs/MarketTab';
import { PortfolioTab } from '@/components/tabs/PortfolioTab';
import { OptionsTab } from '@/components/tabs/OptionsTab';
import { OrdersTab } from '@/components/tabs/OrdersTab';
import { NewsTab } from '@/components/tabs/NewsTab';
import { AnalyticsTab } from '@/components/tabs/AnalyticsTab';
import { JournalTab } from '@/components/tabs/JournalTab';
import { styles } from '@/styles/centralStyles';

interface CentralAreaProps {
  wsClient: WebSocketClient;
}

export function CentralArea({ wsClient }: CentralAreaProps) {
  const activeTab = useMarketStore((s) => s.activeTab);
  const selectedSymbol = useMarketStore((s) => s.selectedSymbol);
  const showStockDetail = useMarketStore((s) => s.showStockDetail);
  const isGameActive = useMarketStore((s) => s.isGameActive);
  const speed = useMarketStore((s) => s.speed);

  if (!isGameActive) {
    return (
      <main style={styles.central}>
        <div style={styles.placeholder}>
          <span style={styles.logoLarge}>STOCKSIM</span>
          <span style={styles.subtitle}>Connecting to backend...</span>
        </div>
      </main>
    );
  }

  return (
    <main style={styles.central}>
      {/* PAUSED indicator — small, non-blocking */}
      {speed === 0 && (
        <div style={{
          position: 'absolute', top: '8px', left: '50%', transform: 'translateX(-50%)',
          zIndex: 10, pointerEvents: 'none',
          padding: '4px 16px', borderRadius: '4px',
          background: 'rgba(96, 165, 250, 0.15)', border: '1px solid rgba(96, 165, 250, 0.3)',
          fontSize: '12px', fontWeight: 700, color: 'var(--text-accent)',
          fontFamily: 'var(--font-mono)', letterSpacing: '2px',
        }}>PAUSED</div>
      )}

      {/* Stock Detail View (Spec 3.4.2) */}
      {showStockDetail && selectedSymbol ? (
        <StockDetailView wsClient={wsClient} />
      ) : (
        (() => {
          switch (activeTab) {
            case 'dashboard': return <DashboardTab wsClient={wsClient} />;
            case 'market': return <MarketTab wsClient={wsClient} />;
            case 'portfolio': return <PortfolioTab />;
            case 'options': return <OptionsTab wsClient={wsClient} />;
            case 'orders': return <OrdersTab wsClient={wsClient} />;
            case 'news': return <NewsTab />;
            case 'analytics': return <AnalyticsTab wsClient={wsClient} />;
            case 'journal': return <JournalTab wsClient={wsClient} />;
            default: return <DashboardTab wsClient={wsClient} />;
          }
        })()
      )}
    </main>
  );
}
