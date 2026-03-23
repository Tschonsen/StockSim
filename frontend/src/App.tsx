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
import { ShortcutsHelp } from '@/components/layout/ShortcutsHelp';
import { TitleScreen } from '@/components/screens/TitleScreen';
import { NewGameScreen, GameConfig } from '@/components/screens/NewGameScreen';
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

  // App screen state machine: title → newgame → ingame
  const [screen, setScreen] = useState<'connecting' | 'title' | 'newgame' | 'ingame'>('connecting');
  const [showSettings, setShowSettings] = useState(false);
  const [showTutorial, setShowTutorial] = useState(false);
  const [showShortcuts, setShowShortcuts] = useState(false);
  const [daySummary, setDaySummary] = useState<Record<string, unknown> | null>(null);
  const [gameSettings, setGameSettings] = useState<GameSettings>(DEFAULT_SETTINGS);

  // Keyboard shortcuts (Bible 18)
  useKeyboardShortcuts(wsClient);

  // Listen for shortcuts help toggle
  useEffect(() => {
    const handler = () => setShowShortcuts(v => !v);
    window.addEventListener('toggleShortcutsHelp', handler);
    return () => window.removeEventListener('toggleShortcutsHelp', handler);
  }, []);

  useEffect(() => {
    log.info('App mounting, connecting to backend');

    // Register message handlers (store unsub functions for cleanup)
    const unsubs: (() => void)[] = [];

    unsubs.push(wsClient.on('MarketSnapshot', (payload) => {
      const snapshot = payload as MarketSnapshot;
      setStocks(snapshot.stocks);
      setSpeed(snapshot.speed);
      log.info('Market snapshot received', { stocks: snapshot.stocks.length });
      setScreen('ingame');
      wsClient.send('GetPortfolio', {});
    }));

    unsubs.push(wsClient.on('MarketUpdate', (payload) => {
      updatePrices(payload as MarketUpdate);
    }));

    unsubs.push(wsClient.on('SpeedChanged', (payload) => {
      const data = payload as { speed: number };
      setSpeed(data.speed);
    }));

    unsubs.push(wsClient.on('OHLCVUpdate', (payload) => {
      const data = payload as { symbol: string; candles: { time: number; open: number; high: number; low: number; close: number; volume: number }[] };
      setOHLCVData(data.symbol, data.candles);
    }));

    unsubs.push(wsClient.on('OrderResult', (payload) => {
      const result = payload as OrderResultData;
      setOrderResult(result);
      if (result.success && result.order?.status === 'Filled') {
        if (result.order.side === 'Buy' || result.order.side === 'Cover') audio.orderFilledBuy();
        else audio.orderFilledSell();
      } else if (!result.success) {
        audio.orderRejected();
      }
    }));

    unsubs.push(wsClient.on('PortfolioUpdate', (payload) => {
      setPortfolio(payload as PortfolioData);
    }));

    unsubs.push(wsClient.on('OrdersUpdate', (payload) => {
      const data = payload as { orders: OrderData[] };
      setOrders(data.orders);
    }));

    unsubs.push(wsClient.on('NewsEvents', (payload) => {
      const data = payload as { events: NewsEvent[] };
      addNewsEvents(data.events);
      if (data.events.some(e => e.severity === 'Major')) audio.breakingNews();
      if (data.events.some(e => e.headline.includes('FLASH CRASH'))) audio.flashCrash();
    }));

    unsubs.push(wsClient.on('OrderbookData', (payload) => {
      setOrderbookData(payload as OrderbookData);
    }));

    unsubs.push(wsClient.on('DaySummary', (payload) => {
      setDaySummary(payload as Record<string, unknown>);
      audio.marketBell();
    }));

    unsubs.push(wsClient.on('AlertTriggered', () => {
      audio.priceAlert();
    }));

    unsubs.push(wsClient.on('IndicatorData', (payload) => {
      const data = payload as { symbol: string; indicators: IndicatorData };
      setIndicatorData(data.symbol, data.indicators);
    }));

    unsubs.push(wsClient.on('welcome', () => {
      setConnected(true);
      log.info('Backend handshake complete');
      setScreen('title');
    }));

    // Connect
    wsClient.connect();

    return () => {
      unsubs.forEach(fn => fn());
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

  // Title Screen
  if (screen === 'connecting') {
    return (
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100vh', background: 'var(--bg-primary)' }}>
        <div style={{ textAlign: 'center' }}>
          <h1 className="mono pulse" style={{ fontSize: '36px', color: 'var(--text-accent)', letterSpacing: '6px', textShadow: '0 0 20px rgba(96,165,250,0.2)' }}>STOCKSIM</h1>
          <p style={{ color: 'var(--text-disabled)', marginTop: '12px' }}>Connecting to engine...</p>
        </div>
      </div>
    );
  }

  if (screen === 'title') {
    return (
      <>
        <TitleScreen
          hasSaves={false}
          onNewGame={() => setScreen('newgame')}
          onContinue={() => {
            wsClient.send('LoadGame', {});
          }}
          onLoadGame={() => {
            wsClient.send('LoadGame', {});
          }}
          onSettings={() => setShowSettings(true)}
        />
        <SettingsModal isOpen={showSettings} onClose={() => setShowSettings(false)}
          settings={gameSettings} onSettingsChange={setGameSettings} />
      </>
    );
  }

  if (screen === 'newgame') {
    return (
      <NewGameScreen
        wsClient={wsClient}
        onBack={() => setScreen('title')}
        onStart={(config: GameConfig) => {
          if (config.showTutorial) setShowTutorial(true);
        }}
      />
    );
  }

  // InGame HUD
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
      <ShortcutsHelp isOpen={showShortcuts} onClose={() => setShowShortcuts(false)} />

      {/* Day Summary Modal */}
      {daySummary && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.5)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 4500 }}
          onClick={() => setDaySummary(null)}>
          <div style={{ width: '420px', background: 'var(--bg-secondary)', border: '1px solid var(--border)', borderRadius: '10px', padding: '24px', textAlign: 'center' }}
            onClick={e => e.stopPropagation()}>
            <h3 style={{ fontSize: '18px', fontWeight: 700, color: 'var(--text-primary)', marginBottom: '4px' }}>Market Closed</h3>
            <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>{String(daySummary.date)}</span>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '12px', margin: '16px 0' }}>
              <div style={{ background: 'var(--bg-tertiary)', borderRadius: '6px', padding: '10px' }}>
                <span style={{ fontSize: '10px', color: 'var(--text-disabled)', display: 'block' }}>MARKET</span>
                <span className="mono" style={{ fontSize: '16px', fontWeight: 700, color: Number(daySummary.marketChange) >= 0 ? 'var(--green-primary)' : 'var(--red-primary)' }}>
                  {Number(daySummary.marketChange) >= 0 ? '+' : ''}{Number(daySummary.marketChange).toFixed(2)}%
                </span>
              </div>
              <div style={{ background: 'var(--bg-tertiary)', borderRadius: '6px', padding: '10px' }}>
                <span style={{ fontSize: '10px', color: 'var(--text-disabled)', display: 'block' }}>PORTFOLIO</span>
                <span className="mono" style={{ fontSize: '16px', fontWeight: 700, color: 'var(--text-primary)' }}>
                  ${Number(daySummary.portfolioValue).toFixed(0)}
                </span>
              </div>
            </div>
            <button onClick={() => setDaySummary(null)} style={{
              padding: '8px 24px', borderRadius: '6px', background: 'var(--text-accent)',
              color: '#FFF', border: 'none', cursor: 'pointer', fontWeight: 700, fontSize: '14px',
            }}>Continue</button>
          </div>
        </div>
      )}

      <SettingsModal
        isOpen={showSettings}
        onClose={() => setShowSettings(false)}
        settings={gameSettings}
        onSettingsChange={setGameSettings}
      />
    </div>
  );
}
