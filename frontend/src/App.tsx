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
import { MarketSnapshot, MarketUpdate, PortfolioData, OrderResultData, OrderData, NewsEvent, IndicatorData, OrderbookData, AnalyticsResponse, Achievement, ScenarioResultData, StockFundamentals, EconomicDataResponse, EarningsCalendarResponse, SMAStatusResponse, SMANotification } from '@/types/market';
import { SettingsModal, GameSettings, DEFAULT_SETTINGS } from '@/components/layout/SettingsModal';
import { audio } from '@/services/audio';
import { TutorialOverlay } from '@/components/layout/TutorialOverlay';
import { ShortcutsHelp } from '@/components/layout/ShortcutsHelp';
import { CommandBar } from '@/components/layout/CommandBar';
import { GlossaryModal } from '@/components/layout/GlossaryModal';
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
  const setAnalyticsData = useMarketStore((s) => s.setAnalyticsData);
  const showAchievementPopup = useMarketStore((s) => s.showAchievementPopup);
  const setAchievements = useMarketStore((s) => s.setAchievements);
  const setTradeJournal = useMarketStore((s) => s.setTradeJournal);
  const achievementPopup = useMarketStore((s) => s.achievementPopup);
  const dismissAchievementPopup = useMarketStore((s) => s.dismissAchievementPopup);
  const setScenarioResult = useMarketStore((s) => s.setScenarioResult);
  const scenarioResult = useMarketStore((s) => s.scenarioResult);
  const setBankrupt = useMarketStore((s) => s.setBankrupt);
  const isBankrupt = useMarketStore((s) => s.isBankrupt);
  const setStockFundamentals = useMarketStore((s) => s.setStockFundamentals);
  const setEconomicData = useMarketStore((s) => s.setEconomicData);
  const setEarningsCalendar = useMarketStore((s) => s.setEarningsCalendar);
  const setSMAData = useMarketStore((s) => s.setSMAData);
  const addSMANotifications = useMarketStore((s) => s.addSMANotifications);
  const smaNotifications = useMarketStore((s) => s.smaNotifications);
  const dismissSMANotification = useMarketStore((s) => s.dismissSMANotification);
  const selectedSymbol = useMarketStore((s) => s.selectedSymbol);

  // App screen state machine: title → newgame → ingame
  const [screen, setScreen] = useState<'connecting' | 'title' | 'newgame' | 'ingame'>('connecting');
  const [showSettings, setShowSettings] = useState(false);
  const [showTutorial, setShowTutorial] = useState(false);
  const [showShortcuts, setShowShortcuts] = useState(false);
  const [daySummary, setDaySummary] = useState<Record<string, unknown> | null>(null);
  const [gameSettings, setGameSettings] = useState<GameSettings>(() => {
    try {
      const saved = localStorage.getItem('stocksim-settings');
      return saved ? { ...DEFAULT_SETTINGS, ...JSON.parse(saved) } : DEFAULT_SETTINGS;
    } catch { return DEFAULT_SETTINGS; }
  });
  const [showCommandBar, setShowCommandBar] = useState(false);
  const [showGlossary, setShowGlossary] = useState(false);
  const [careerSummary, setCareerSummary] = useState<Record<string, unknown> | null>(null);

  // Keyboard shortcuts (Bible 18)
  useKeyboardShortcuts(wsClient);

  // Listen for shortcuts help toggle (? key opens Glossary now)
  useEffect(() => {
    const handler = () => setShowGlossary(v => !v);
    window.addEventListener('toggleShortcutsHelp', handler);
    return () => window.removeEventListener('toggleShortcutsHelp', handler);
  }, []);

  // Apply settings changes to audio, UI, and persist to localStorage
  useEffect(() => {
    // Audio
    audio.setMasterVolume(gameSettings.masterVolume / 100);
    audio.setSfxVolume(gameSettings.sfxVolume / 100);
    audio.setEnabled(gameSettings.masterVolume > 0);

    // UI Scale
    document.documentElement.style.fontSize = `${gameSettings.uiScale}%`;

    // Large text
    document.body.classList.toggle('large-text', gameSettings.largeText);

    // High contrast
    document.body.classList.toggle('high-contrast', gameSettings.highContrast);

    // Reduced animations
    document.body.classList.toggle('reduced-motion', gameSettings.reducedAnimations);

    // Ticker speed
    document.documentElement.style.setProperty('--ticker-speed', `${gameSettings.newsTickerSpeed}s`);

    // Persist
    try { localStorage.setItem('stocksim-settings', JSON.stringify(gameSettings)); } catch {}
  }, [gameSettings]);

  // Ctrl+K for Command Bar + custom event from keyboard shortcuts
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        setShowCommandBar(v => !v);
      }
    };
    const customHandler = () => setShowCommandBar(true);
    window.addEventListener('keydown', handler);
    window.addEventListener('openCommandBar', customHandler);
    return () => {
      window.removeEventListener('keydown', handler);
      window.removeEventListener('openCommandBar', customHandler);
    };
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

    unsubs.push(wsClient.on('AnalyticsData', (payload) => {
      setAnalyticsData(payload as AnalyticsResponse);
    }));

    unsubs.push(wsClient.on('AchievementUnlocked', (payload) => {
      const ach = payload as Achievement;
      ach.unlocked = true;
      showAchievementPopup(ach);
      audio.achievement();
    }));

    unsubs.push(wsClient.on('AchievementList', (payload) => {
      const data = payload as { achievements: Achievement[] };
      setAchievements(data.achievements);
    }));

    unsubs.push(wsClient.on('TradeJournal', (payload) => {
      const data = payload as { trades: import('@/types/market').TradeJournalEntry[] };
      setTradeJournal(data.trades);
    }));

    unsubs.push(wsClient.on('CareerSummary', (payload) => {
      setCareerSummary(payload as Record<string, unknown>);
    }));

    unsubs.push(wsClient.on('ScenarioCompleted', (payload) => {
      setScenarioResult(payload as ScenarioResultData);
    }));

    unsubs.push(wsClient.on('Bankruptcy', () => {
      setBankrupt(true);
    }));

    unsubs.push(wsClient.on('BankruptRestarted', () => {
      setBankrupt(false);
    }));

    unsubs.push(wsClient.on('StockFundamentals', (payload) => {
      setStockFundamentals(payload as StockFundamentals);
    }));

    unsubs.push(wsClient.on('EconomicData', (payload) => {
      setEconomicData(payload as EconomicDataResponse);
    }));

    unsubs.push(wsClient.on('EarningsCalendar', (payload) => {
      setEarningsCalendar(payload as EarningsCalendarResponse);
    }));

    unsubs.push(wsClient.on('SMAStatus', (payload) => {
      setSMAData(payload as SMAStatusResponse);
    }));

    unsubs.push(wsClient.on('SMANotifications', (payload) => {
      const data = payload as { notifications: SMANotification[] };
      addSMANotifications(data.notifications);
    }));

    unsubs.push(wsClient.on('TaxSummary', (payload) => {
      // Store in marketStore or just dispatch event for CentralArea
      window.dispatchEvent(new CustomEvent('taxSummary', { detail: payload }));
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
      wsClient.send('GetStockFundamentals', { symbol: selectedSymbol });
    } else {
      setStockFundamentals(null);
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
          onContinue={() => wsClient.send('LoadGame', {})}
          onLoadGame={() => wsClient.send('LoadGame', {})}
          onSettings={() => setShowSettings(true)}
          onQuit={() => { wsClient.send('shutdown', {}); window.close(); }}
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
      <TopBar wsClient={wsClient} onOpenSettings={() => setShowSettings(true)} onOpenCommandBar={() => setShowCommandBar(true)} />
      <div className="main-layout">
        <LeftSidebar />
        <CentralArea wsClient={wsClient} />
        <RightSidebar wsClient={wsClient} />
      </div>
      <NewsTicker />
      <TutorialOverlay isOpen={showTutorial} onClose={() => setShowTutorial(false)} />
      <ShortcutsHelp isOpen={showShortcuts} onClose={() => setShowShortcuts(false)} />
      <GlossaryModal isOpen={showGlossary} onClose={() => setShowGlossary(false)} />

      {/* Day Summary Modal — Enhanced Bloomberg End-of-Day Report */}
      {daySummary && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.5)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 4500 }}
          onClick={() => setDaySummary(null)}>
          <div style={{ width: '500px', maxHeight: '80vh', overflowY: 'auto', background: 'var(--bg-secondary)', border: '1px solid var(--border)', borderRadius: '10px', padding: '24px' }}
            onClick={e => e.stopPropagation()}>
            <div style={{ textAlign: 'center', marginBottom: '16px' }}>
              <div style={{ fontSize: '10px', color: 'var(--text-disabled)', letterSpacing: '2px' }}>END OF DAY REPORT</div>
              <h3 style={{ fontSize: '18px', fontWeight: 700, color: 'var(--text-primary)', margin: '4px 0' }}>Market Closed</h3>
              <span style={{ fontSize: '13px', color: 'var(--text-secondary)' }}>{String(daySummary.date)}</span>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '8px', marginBottom: '12px' }}>
              <div style={{ background: 'var(--bg-tertiary)', borderRadius: '6px', padding: '10px', textAlign: 'center' }}>
                <span style={{ fontSize: '9px', color: 'var(--text-disabled)', display: 'block' }}>MARKET</span>
                <span className="mono" style={{ fontSize: '18px', fontWeight: 700, color: Number(daySummary.marketChange) >= 0 ? 'var(--green-primary)' : 'var(--red-primary)' }}>
                  {Number(daySummary.marketChange) >= 0 ? '+' : ''}{Number(daySummary.marketChange).toFixed(2)}%
                </span>
              </div>
              <div style={{ background: 'var(--bg-tertiary)', borderRadius: '6px', padding: '10px', textAlign: 'center' }}>
                <span style={{ fontSize: '9px', color: 'var(--text-disabled)', display: 'block' }}>PORTFOLIO</span>
                <span className="mono" style={{ fontSize: '18px', fontWeight: 700, color: 'var(--text-primary)' }}>
                  ${Number(daySummary.portfolioValue).toFixed(0)}
                </span>
              </div>
              <div style={{ background: 'var(--bg-tertiary)', borderRadius: '6px', padding: '10px', textAlign: 'center' }}>
                <span style={{ fontSize: '9px', color: 'var(--text-disabled)', display: 'block' }}>BREADTH</span>
                <span className="mono" style={{ fontSize: '14px' }}>
                  <span style={{ color: 'var(--green-primary)' }}>{Number(daySummary.advancing || 0)}</span>
                  {' / '}
                  <span style={{ color: 'var(--red-primary)' }}>{Number(daySummary.declining || 0)}</span>
                </span>
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px', marginBottom: '12px' }}>
              <div style={{ background: 'var(--bg-tertiary)', borderRadius: '6px', padding: '8px 12px' }}>
                <span style={{ fontSize: '9px', color: 'var(--text-disabled)' }}>TOP GAINER</span>
                <div className="mono" style={{ color: 'var(--green-primary)', fontSize: '13px', fontWeight: 700 }}>
                  {String((daySummary.topGainer as Record<string, unknown>)?.symbol)} +{Number((daySummary.topGainer as Record<string, unknown>)?.change).toFixed(2)}%
                </div>
              </div>
              <div style={{ background: 'var(--bg-tertiary)', borderRadius: '6px', padding: '8px 12px' }}>
                <span style={{ fontSize: '9px', color: 'var(--text-disabled)' }}>TOP LOSER</span>
                <div className="mono" style={{ color: 'var(--red-primary)', fontSize: '13px', fontWeight: 700 }}>
                  {String((daySummary.topLoser as Record<string, unknown>)?.symbol)} {Number((daySummary.topLoser as Record<string, unknown>)?.change).toFixed(2)}%
                </div>
              </div>
            </div>

            {/* Sector Performance */}
            {Array.isArray(daySummary.sectorPerformance) && (daySummary.sectorPerformance as Array<Record<string, unknown>>).length > 0 && (
              <div style={{ marginBottom: '12px' }}>
                <span style={{ fontSize: '9px', color: 'var(--text-disabled)', letterSpacing: '1px' }}>SECTOR PERFORMANCE</span>
                <div style={{ display: 'flex', gap: '4px', marginTop: '4px', flexWrap: 'wrap' }}>
                  {(daySummary.sectorPerformance as Array<Record<string, unknown>>).map((sp: Record<string, unknown>) => (
                    <span key={String(sp.sector)} className="mono" style={{
                      fontSize: '10px', padding: '2px 6px', borderRadius: '3px',
                      background: Number(sp.change) >= 0 ? 'rgba(16,185,129,0.15)' : 'rgba(239,68,68,0.15)',
                      color: Number(sp.change) >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                    }}>{String(sp.sector).slice(0, 4)} {Number(sp.change) >= 0 ? '+' : ''}{Number(sp.change).toFixed(1)}%</span>
                  ))}
                </div>
              </div>
            )}

            <button onClick={() => setDaySummary(null)} style={{
              width: '100%', padding: '10px', borderRadius: '6px', background: 'var(--text-accent)',
              color: '#FFF', border: 'none', cursor: 'pointer', fontWeight: 700, fontSize: '14px', marginTop: '8px',
            }}>Continue Trading</button>
          </div>
        </div>
      )}

      <SettingsModal
        isOpen={showSettings}
        onClose={() => setShowSettings(false)}
        settings={gameSettings}
        onSettingsChange={setGameSettings}
      />

      {/* Career Summary / Retirement Modal */}
      {careerSummary && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.8)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 9500 }}>
          <div style={{
            width: '560px', maxHeight: '85vh', overflowY: 'auto',
            background: 'linear-gradient(180deg, #111827, #0A0E17)',
            border: '1px solid #D4AF37', borderRadius: '12px', padding: '32px',
            boxShadow: '0 0 60px rgba(212,175,55,0.15)',
          }}>
            <div style={{ textAlign: 'center', marginBottom: '24px' }}>
              <div style={{ fontSize: '12px', color: '#D4AF37', letterSpacing: '4px', fontWeight: 600 }}>CAREER COMPLETE</div>
              <h2 style={{ fontSize: '32px', fontWeight: 900, color: '#F5E6B8', margin: '8px 0', letterSpacing: '2px' }}>RETIRED</h2>
              <div className="mono" style={{ fontSize: '28px', fontWeight: 700, color: Number(careerSummary.totalReturnPercent) >= 0 ? 'var(--green-primary)' : 'var(--red-primary)' }}>
                ${Number(careerSummary.finalEquity).toLocaleString(undefined, { maximumFractionDigits: 0 })}
              </div>
              <div className="mono" style={{ fontSize: '14px', color: Number(careerSummary.totalReturnPercent) >= 0 ? 'var(--green-primary)' : 'var(--red-primary)' }}>
                {Number(careerSummary.totalReturnPercent) >= 0 ? '+' : ''}{Number(careerSummary.totalReturnPercent).toFixed(1)}% Total Return
              </div>
            </div>

            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '8px', marginBottom: '20px' }}>
              {[
                ['Days Traded', careerSummary.daysPlayed],
                ['Total Trades', careerSummary.totalTrades],
                ['Win Rate', `${Number(careerSummary.winRate).toFixed(1)}%`],
                ['Best Trade', `+$${Number(careerSummary.bestTradePnL).toFixed(0)} (${careerSummary.bestTradeSymbol})`],
                ['Worst Trade', `-$${Number(careerSummary.worstTradePnL).toFixed(0)} (${careerSummary.worstTradeSymbol})`],
                ['Max Drawdown', `-${Number(careerSummary.maxDrawdown).toFixed(1)}%`],
                ['Commissions', `$${Number(careerSummary.totalCommissions).toFixed(0)}`],
                ['Tax Paid', `$${Number(careerSummary.totalTaxPaid).toFixed(0)}`],
                ['Achievements', `${careerSummary.achievementsUnlocked}/${careerSummary.achievementsTotal}`],
              ].map(([label, value]) => (
                <div key={String(label)} style={{
                  background: 'rgba(255,255,255,0.03)', borderRadius: '6px', padding: '10px',
                  textAlign: 'center', border: '1px solid rgba(212,175,55,0.1)',
                }}>
                  <div style={{ fontSize: '10px', color: '#D4AF37', letterSpacing: '1px', marginBottom: '4px' }}>{String(label).toUpperCase()}</div>
                  <div className="mono" style={{ fontSize: '14px', fontWeight: 700, color: 'var(--text-primary)' }}>{String(value)}</div>
                </div>
              ))}
            </div>

            <div style={{ display: 'flex', gap: '12px', justifyContent: 'center' }}>
              <button onClick={() => { setCareerSummary(null); setScreen('newgame'); }} style={{
                padding: '10px 32px', borderRadius: '6px', fontSize: '14px', fontWeight: 700,
                background: 'linear-gradient(135deg, #D4AF37, #B8860B)', color: '#FFF',
                border: 'none', cursor: 'pointer',
              }}>New Game</button>
              <button onClick={() => setCareerSummary(null)} style={{
                padding: '10px 24px', borderRadius: '6px', background: 'var(--bg-tertiary)',
                color: 'var(--text-primary)', border: '1px solid var(--border)', cursor: 'pointer', fontWeight: 600,
              }}>Continue Playing</button>
            </div>
          </div>
        </div>
      )}

      {/* Command Bar (Ctrl+K) */}
      <CommandBar wsClient={wsClient} isOpen={showCommandBar} onClose={() => setShowCommandBar(false)} />

      {/* Bankruptcy Modal */}
      {isBankrupt && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.7)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 9000 }}>
          <div style={{ width: '440px', background: 'var(--bg-secondary)', border: '1px solid var(--red-primary)', borderRadius: '10px', padding: '32px', textAlign: 'center' }}>
            <h2 style={{ fontSize: '28px', fontWeight: 900, color: 'var(--red-primary)', marginBottom: '8px', letterSpacing: '2px' }}>BANKRUPT</h2>
            <p style={{ color: 'var(--text-secondary)', fontSize: '14px', marginBottom: '24px' }}>
              You've lost everything. Your cash is zero and you have no positions left.
            </p>
            <div style={{ display: 'flex', gap: '12px', justifyContent: 'center' }}>
              <button onClick={() => { wsClient.send('RestartBankrupt', {}); }} style={{
                padding: '10px 24px', borderRadius: '6px', background: 'var(--green-primary)',
                color: '#FFF', border: 'none', cursor: 'pointer', fontWeight: 700, fontSize: '14px',
              }}>Restart with $10,000</button>
              <button onClick={() => { setBankrupt(false); setScreen('title'); }} style={{
                padding: '10px 24px', borderRadius: '6px', background: 'var(--bg-tertiary)',
                color: 'var(--text-primary)', border: '1px solid var(--border)', cursor: 'pointer', fontWeight: 600, fontSize: '14px',
              }}>New Game</button>
            </div>
          </div>
        </div>
      )}

      {/* Scenario Completed Modal */}
      {scenarioResult && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.7)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 9000 }}>
          <div style={{
            width: '480px', background: 'var(--bg-secondary)',
            border: `1px solid ${scenarioResult.won ? '#D4AF37' : 'var(--red-primary)'}`,
            borderRadius: '10px', padding: '32px', textAlign: 'center',
          }}>
            <h2 style={{
              fontSize: '24px', fontWeight: 900, marginBottom: '4px',
              color: scenarioResult.won ? '#D4AF37' : 'var(--red-primary)',
              letterSpacing: '2px',
            }}>
              {scenarioResult.won ? 'SCENARIO COMPLETE' : 'SCENARIO FAILED'}
            </h2>
            <p style={{ fontSize: '16px', color: 'var(--text-primary)', fontWeight: 600 }}>{scenarioResult.scenarioName}</p>
            {!scenarioResult.won && scenarioResult.failReason && (
              <p style={{ color: 'var(--red-primary)', fontSize: '13px', marginTop: '4px' }}>{scenarioResult.failReason}</p>
            )}

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '10px', margin: '20px 0' }}>
              <div style={{ background: 'var(--bg-tertiary)', borderRadius: '6px', padding: '10px' }}>
                <span style={{ fontSize: '10px', color: 'var(--text-disabled)', display: 'block' }}>PORTFOLIO</span>
                <span className="mono" style={{ fontSize: '16px', fontWeight: 700, color: 'var(--text-primary)' }}>
                  ${scenarioResult.finalPortfolioValue.toFixed(0)}
                </span>
              </div>
              <div style={{ background: 'var(--bg-tertiary)', borderRadius: '6px', padding: '10px' }}>
                <span style={{ fontSize: '10px', color: 'var(--text-disabled)', display: 'block' }}>RETURN</span>
                <span className="mono" style={{
                  fontSize: '16px', fontWeight: 700,
                  color: scenarioResult.totalReturnPercent >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                }}>
                  {scenarioResult.totalReturnPercent >= 0 ? '+' : ''}{scenarioResult.totalReturnPercent.toFixed(1)}%
                </span>
              </div>
              <div style={{ background: 'var(--bg-tertiary)', borderRadius: '6px', padding: '10px' }}>
                <span style={{ fontSize: '10px', color: 'var(--text-disabled)', display: 'block' }}>DAYS</span>
                <span className="mono" style={{ fontSize: '16px', fontWeight: 700, color: 'var(--text-primary)' }}>
                  {scenarioResult.daysElapsed}
                </span>
              </div>
            </div>

            <div style={{ display: 'flex', gap: '12px', justifyContent: 'center' }}>
              <button onClick={() => { setScenarioResult(null); setScreen('newgame'); }} style={{
                padding: '10px 24px', borderRadius: '6px', background: 'var(--text-accent)',
                color: '#FFF', border: 'none', cursor: 'pointer', fontWeight: 700, fontSize: '14px',
              }}>New Game</button>
              <button onClick={() => setScenarioResult(null)} style={{
                padding: '10px 24px', borderRadius: '6px', background: 'var(--bg-tertiary)',
                color: 'var(--text-primary)', border: '1px solid var(--border)', cursor: 'pointer', fontWeight: 600, fontSize: '14px',
              }}>Continue Playing</button>
            </div>
          </div>
        </div>
      )}

      {/* SMA Notification Toasts (Bible 9.2) */}
      {smaNotifications.length > 0 && (
        <div style={{
          position: 'fixed',
          top: '80px',
          right: '24px',
          zIndex: 9998,
          display: 'flex',
          flexDirection: 'column',
          gap: '8px',
          maxWidth: '420px',
        }}>
          {smaNotifications.slice(0, 3).map((notif, i) => {
            const bgColor = notif.severity === 'critical'
              ? 'rgba(239, 68, 68, 0.15)'
              : notif.severity === 'warning'
                ? 'rgba(245, 158, 11, 0.15)'
                : 'rgba(96, 165, 250, 0.1)';
            const borderColor = notif.severity === 'critical'
              ? 'var(--red-primary)'
              : notif.severity === 'warning'
                ? '#F59E0B'
                : 'var(--border)';
            const icon = notif.severity === 'critical' ? '🔴' : notif.severity === 'warning' ? '⚠' : 'ℹ';
            return (
              <div
                key={i}
                onClick={() => dismissSMANotification(i)}
                style={{
                  background: bgColor,
                  border: `1px solid ${borderColor}`,
                  borderRadius: '8px',
                  padding: '12px 16px',
                  cursor: 'pointer',
                  animation: 'slideDown 0.3s ease-out',
                  boxShadow: `0 4px 20px rgba(0, 0, 0, 0.4)`,
                }}
              >
                <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '4px' }}>
                  <span>{icon}</span>
                  <span style={{ fontWeight: 700, fontSize: '13px', color: borderColor }}>{notif.title}</span>
                </div>
                <div style={{ fontSize: '12px', color: 'var(--text-primary)', lineHeight: '1.4' }}>
                  {notif.message}
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Achievement Unlock Popup */}
      {achievementPopup && (
        <div
          onClick={dismissAchievementPopup}
          style={{
            position: 'fixed',
            top: '24px',
            left: '50%',
            transform: 'translateX(-50%)',
            zIndex: 9999,
            background: 'linear-gradient(135deg, rgba(212, 175, 55, 0.15), rgba(212, 175, 55, 0.05))',
            border: '1px solid #D4AF37',
            borderRadius: '10px',
            padding: '16px 32px',
            display: 'flex',
            alignItems: 'center',
            gap: '16px',
            boxShadow: '0 0 30px rgba(212, 175, 55, 0.3)',
            cursor: 'pointer',
            animation: 'slideDown 0.4s ease-out',
          }}
        >
          <div style={{ fontSize: '32px', filter: 'drop-shadow(0 0 8px rgba(212,175,55,0.5))' }}>
            {achievementPopup.category === 'Wealth' ? '$' :
             achievementPopup.category === 'Trading' ? '*' :
             achievementPopup.category === 'Market' ? '#' : '!'}
          </div>
          <div>
            <div style={{ fontSize: '11px', color: '#D4AF37', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '2px' }}>
              Achievement Unlocked
            </div>
            <div style={{ fontSize: '18px', fontWeight: 700, color: '#F5E6B8', marginTop: '2px' }}>
              {achievementPopup.name}
            </div>
            <div style={{ fontSize: '12px', color: 'rgba(245, 230, 184, 0.7)', marginTop: '2px' }}>
              {achievementPopup.description}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
