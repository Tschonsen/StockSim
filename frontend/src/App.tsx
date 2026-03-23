import { useEffect, useState } from 'react';
import { TopBar } from '@/components/layout/TopBar';
import { LeftSidebar } from '@/components/layout/LeftSidebar';
import { CentralArea } from '@/components/layout/CentralArea';
import { RightSidebar } from '@/components/layout/RightSidebar';
import { NewsTicker } from '@/components/layout/NewsTicker';
import { useMarketStore } from '@/stores/marketStore';
import { useKeyboardShortcuts } from '@/hooks/useKeyboardShortcuts';
import { WebSocketClient } from '@/services/websocket';
import { createLogger } from '@/services/logger';
import { MarketSnapshot, MarketUpdate, PortfolioData, OrderResultData, OrderData, NewsEvent, IndicatorData, OrderbookData } from '@/types/market';
import { SettingsModal, GameSettings, DEFAULT_SETTINGS } from '@/components/layout/SettingsModal';
import { audio } from '@/services/audio';
import { TutorialOverlay } from '@/components/layout/TutorialOverlay';
import '@/styles/globals.css';

const log = createLogger('App');

const wsClient = new WebSocketClient('ws://localhost:8765');

export function App() {
  const setStocks = useMarketStore((s) => s.setStocks);
  const updatePrices = useMarketStore((s) => s.updatePrices);
  const setConnected = useMarketStore((s) => s.setConnected);
  const setSpeed = useMarketStore((s) => s.setSpeed);
  const setOHLCVData = useMarketStore((s) => s.setOHLCVData);
  const setPortfolio = useMarketStore((s) => s.setPortfolio);
  const setOrders = useMarketStore((s) => s.setOrders);
  const setOrderResult = useMarketStore((s) => s.setOrderResult);
  const addNewsEvents = useMarketStore((s) => s.addNewsEvents);
  const setIndicatorData = useMarketStore((s) => s.setIndicatorData);
  const setOrderbookData = useMarketStore((s) => s.setOrderbookData);
  const selectedSymbol = useMarketStore((s) => s.selectedSymbol);

  const [showSettings, setShowSettings] = useState(false);
  const [showTutorial, setShowTutorial] = useState(true);
  const [gameSettings, setGameSettings] = useState<GameSettings>(DEFAULT_SETTINGS);

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

      // Request initial portfolio data
      wsClient.send('GetPortfolio', {});
    });

    wsClient.on('MarketUpdate', (payload) => {
      updatePrices(payload as MarketUpdate);
    });

    wsClient.on('SpeedChanged', (payload) => {
      const data = payload as { speed: number };
      setSpeed(data.speed);
    });

    wsClient.on('OHLCVUpdate', (payload) => {
      const data = payload as { symbol: string; candles: { time: number; open: number; high: number; low: number; close: number; volume: number }[] };
      setOHLCVData(data.symbol, data.candles);
    });

    // Order & Portfolio handlers
    wsClient.on('OrderResult', (payload) => {
      const result = payload as OrderResultData;
      setOrderResult(result);
      // Audio feedback
      if (result.success && result.order?.status === 'Filled') {
        if (result.order.side === 'Buy' || result.order.side === 'Cover') audio.orderFilledBuy();
        else audio.orderFilledSell();
      } else if (!result.success) {
        audio.orderRejected();
      }
    });

    wsClient.on('PortfolioUpdate', (payload) => {
      setPortfolio(payload as PortfolioData);
    });

    wsClient.on('OrdersUpdate', (payload) => {
      const data = payload as { orders: OrderData[] };
      setOrders(data.orders);
    });

    wsClient.on('NewsEvents', (payload) => {
      const data = payload as { events: NewsEvent[] };
      addNewsEvents(data.events);
      // Audio for major events
      if (data.events.some(e => e.severity === 'Major')) audio.breakingNews();
      if (data.events.some(e => e.headline.includes('FLASH CRASH'))) audio.flashCrash();
    });

    wsClient.on('OrderbookData', (payload) => {
      setOrderbookData(payload as OrderbookData);
    });

    wsClient.on('AlertTriggered', () => {
      audio.priceAlert();
    });

    wsClient.on('IndicatorData', (payload) => {
      const data = payload as { symbol: string; indicators: IndicatorData };
      setIndicatorData(data.symbol, data.indicators);
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

  // Request OHLCV data and indicators when a stock is selected
  useEffect(() => {
    if (selectedSymbol) {
      wsClient.send('GetOHLCV', { symbol: selectedSymbol });
      wsClient.send('GetIndicators', { symbol: selectedSymbol, indicators: ['SMA20', 'SMA50', 'SMA200', 'RSI', 'BOLLINGER'] });
      wsClient.send('GetOrderbook', { symbol: selectedSymbol });
    }
  }, [selectedSymbol]);

  return (
    <div className="app-container">
      <TopBar wsClient={wsClient} onOpenSettings={() => setShowSettings(true)} />
      <div className="main-layout">
        <LeftSidebar />
        <CentralArea wsClient={wsClient} />
        <RightSidebar wsClient={wsClient} />
      </div>
      <NewsTicker />
      <TutorialOverlay isOpen={showTutorial} onClose={() => setShowTutorial(false)} />
      <SettingsModal
        isOpen={showSettings}
        onClose={() => setShowSettings(false)}
        settings={gameSettings}
        onSettingsChange={setGameSettings}
      />
    </div>
  );
}
