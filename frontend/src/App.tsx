import { useEffect } from 'react';
import { TopBar } from '@/components/layout/TopBar';
import { LeftSidebar } from '@/components/layout/LeftSidebar';
import { CentralArea } from '@/components/layout/CentralArea';
import { RightSidebar } from '@/components/layout/RightSidebar';
import { NewsTicker } from '@/components/layout/NewsTicker';
import { useMarketStore } from '@/stores/marketStore';
import { useKeyboardShortcuts } from '@/hooks/useKeyboardShortcuts';
import { WebSocketClient } from '@/services/websocket';
import { createLogger } from '@/services/logger';
import { MarketSnapshot, MarketUpdate } from '@/types/market';
import '@/styles/globals.css';

const log = createLogger('App');

const wsClient = new WebSocketClient('ws://localhost:8765');

export function App() {
  const setStocks = useMarketStore((s) => s.setStocks);
  const updatePrices = useMarketStore((s) => s.updatePrices);
  const setConnected = useMarketStore((s) => s.setConnected);
  const setSpeed = useMarketStore((s) => s.setSpeed);

  // Keyboard shortcuts (Bible 18)
  useKeyboardShortcuts(wsClient);

  useEffect(() => {
    log.info('App mounting, connecting to backend');

    // Register message handlers
    wsClient.on('MarketSnapshot', (payload) => {
      const snapshot = payload as MarketSnapshot;
      setStocks(snapshot.stocks);
      setSpeed(snapshot.speed);
      log.info('Market snapshot received', { stocks: snapshot.stocks.length });
    });

    wsClient.on('MarketUpdate', (payload) => {
      updatePrices(payload as MarketUpdate);
    });

    wsClient.on('SpeedChanged', (payload) => {
      const data = payload as { speed: number };
      setSpeed(data.speed);
    });

    wsClient.on('welcome', () => {
      setConnected(true);
      log.info('Backend handshake complete');

      // Auto-start a new game for development
      wsClient.send('NewGame', { stockCount: 250 });
    });

    // Connect
    wsClient.connect();

    return () => {
      wsClient.disconnect();
    };
  }, []);

  return (
    <div className="app-container">
      <TopBar wsClient={wsClient} />
      <div className="main-layout">
        <LeftSidebar />
        <CentralArea />
        <RightSidebar />
      </div>
      <NewsTicker />
    </div>
  );
}
