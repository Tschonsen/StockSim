import { useEffect, useState, useRef } from 'react';
import { TopBar } from '@/components/layout/TopBar';
import { ScenarioBar } from '@/components/layout/ScenarioBar';
import { LeftSidebar } from '@/components/layout/LeftSidebar';
import { CentralArea } from '@/components/layout/CentralArea';
import { RightSidebar } from '@/components/layout/RightSidebar';
import { NewsTicker } from '@/components/layout/NewsTicker';
import { useMarketStore } from '@/stores/marketStore';
import { useKeyboardShortcuts } from '@/hooks/useKeyboardShortcuts';
import { WebSocketClient } from '@/services/websocket';
import { createLogger } from '@/services/logger';
import { MarketSnapshot, MarketUpdate, PortfolioData, OrderResultData, OrderData, NewsEvent, IndicatorData, OrderbookData, AnalyticsResponse, Achievement, ScenarioResultData, ScenarioProgress, StockFundamentals, EconomicDataResponse, EarningsCalendarResponse, SMAStatusResponse, SMANotification, ShortSqueezeWarning, TenderOffer } from '@/types/market';
import { SettingsModal, GameSettings, DEFAULT_SETTINGS } from '@/components/layout/SettingsModal';
import { audio } from '@/services/audio';
import { TutorialOverlay } from '@/components/layout/TutorialOverlay';
import { ShortcutsHelp } from '@/components/layout/ShortcutsHelp';
import { CommandBar } from '@/components/layout/CommandBar';
import { GlossaryModal } from '@/components/layout/GlossaryModal';
import { WikiModal } from '@/components/layout/WikiModal';
import { DecisionCaseModal } from '@/components/layout/DecisionCaseModal';
import { DECISION_CASES } from '@/data/decisionCases';
import type { DecisionPoint } from '@/data/decisionCases';
import { TitleScreen } from '@/components/screens/TitleScreen';
import { NewGameScreen, GameConfig } from '@/components/screens/NewGameScreen';
import { SaveDialog, LoadScreen } from '@/components/screens/SaveLoadScreen';
import '@/styles/globals.css';

const log = createLogger('App');

function getBackendPort(): number {
  const params = new URLSearchParams(window.location.search);
  return parseInt(params.get('backendPort') || '8765', 10);
}

const wsClient = new WebSocketClient(`ws://localhost:${getBackendPort()}`);

function AccountBar() {
  const portfolio = useMarketStore((s) => s.portfolio);
  const stockList = useMarketStore((s) => s.stockList);
  if (!portfolio) return null;

  const advancing = stockList.filter(s => s.changePercent > 0 && !s.traits?.includes('ETF')).length;
  const declining = stockList.filter(s => s.changePercent < 0 && !s.traits?.includes('ETF')).length;
  const total = advancing + declining || 1;

  const dayPLPct = portfolio.totalEquity > 0 ? ((portfolio as unknown as Record<string, number>).dayChangePercent ?? 0) : 0;
  const positions = portfolio.positions ? Object.keys(portfolio.positions).length : 0;
  const startingCash = portfolio.startingCash ?? 50000;
  const totalReturnPct = startingCash > 0 ? ((portfolio.totalEquity - startingCash) / startingCash * 100) : 0;

  return (
    <div style={{
      height: '24px', background: 'var(--bg-primary)', borderBottom: '1px solid var(--border)',
      display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '24px',
      fontSize: '11px', fontFamily: 'var(--font-mono)', flexShrink: 0,
    }}>
      <span>
        <span style={{ color: 'var(--text-disabled)' }}>Equity </span>
        <span style={{ color: 'var(--text-primary)', fontWeight: 600 }}>${portfolio.totalEquity.toFixed(0)}</span>
      </span>
      <span>
        <span style={{ color: 'var(--text-disabled)' }}>Cash </span>
        <span style={{ color: 'var(--text-primary)', fontWeight: 600 }}>${portfolio.cash.toFixed(0)}</span>
      </span>
      <span>
        <span style={{ color: 'var(--text-disabled)' }}>Return </span>
        <span style={{ color: totalReturnPct >= 0 ? 'var(--green-primary)' : 'var(--red-primary)', fontWeight: 600 }}>
          {totalReturnPct >= 0 ? '+' : ''}{totalReturnPct.toFixed(1)}%
        </span>
      </span>
      <span>
        <span style={{ color: 'var(--text-disabled)' }}>Day </span>
        <span style={{ color: dayPLPct >= 0 ? 'var(--green-primary)' : 'var(--red-primary)', fontWeight: 600 }}>
          {dayPLPct >= 0 ? '+' : ''}{dayPLPct.toFixed(2)}%
        </span>
      </span>
      <span>
        <span style={{ color: 'var(--text-disabled)' }}>Positions </span>
        <span style={{ color: 'var(--text-primary)', fontWeight: 600 }}>{positions}</span>
      </span>
      <span>
        <span style={{ color: 'var(--text-disabled)' }}>Trades </span>
        <span style={{ color: 'var(--text-primary)', fontWeight: 600 }}>{portfolio.tradeCount}</span>
      </span>
      <span style={{ display: 'flex', alignItems: 'center', gap: '4px' }}>
        <span style={{ color: 'var(--text-disabled)', fontSize: '10px' }}>A/D</span>
        <span className="mono" style={{ fontSize: '10px', color: 'var(--green-primary)', fontWeight: 600 }}>{advancing}</span>
        <div style={{ width: '40px', height: '4px', borderRadius: '2px', background: 'var(--bg-tertiary)', overflow: 'hidden', display: 'flex' }}>
          <div style={{ width: `${(advancing / total) * 100}%`, background: 'var(--green-primary)' }} />
          <div style={{ width: `${(declining / total) * 100}%`, background: 'var(--red-primary)' }} />
        </div>
        <span className="mono" style={{ fontSize: '10px', color: 'var(--red-primary)', fontWeight: 600 }}>{declining}</span>
      </span>
      {/* Seasonal indicator */}
      {(() => {
        const gt = useMarketStore.getState().gameTime;
        if (!gt) return null;
        const m = new Date(gt).getMonth();
        const seasonal = m === 0 ? 'Jan Effect' : m >= 4 && m <= 9 ? 'Sell in May' :
          m === 9 ? 'Oct Vol' : m >= 10 ? 'Holiday Rally' : null;
        if (!seasonal) return null;
        return (
          <span style={{ fontSize: '9px', color: 'var(--warning)', fontWeight: 600, opacity: 0.7 }}>
            {seasonal}
          </span>
        );
      })()}
      <span style={{ color: 'var(--text-disabled)', fontSize: '10px', opacity: 0.5 }}>
        Ctrl+K Search | Space Pause | Esc Menu
      </span>
    </div>
  );
}

export function App() {
  const selectStock = useMarketStore((s) => s.selectStock);
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
  const setShortSqueezeWarning = useMarketStore((s) => s.setShortSqueezeWarning);
  const tenderOffer = useMarketStore((s) => s.tenderOffer);
  const setTenderOffer = useMarketStore((s) => s.setTenderOffer);
  const smaNotifications = useMarketStore((s) => s.smaNotifications);
  const dismissSMANotification = useMarketStore((s) => s.dismissSMANotification);
  const selectedSymbol = useMarketStore((s) => s.selectedSymbol);

  // App screen state machine: connecting → title → newgame → ingame
  type ScreenName = 'connecting' | 'connectionFailed' | 'title' | 'newgame' | 'ingame';
  const [screen, setScreenRaw] = useState<ScreenName>('connecting');
  const [transitioning, setTransitioning] = useState(false);
  const [screenOpacity, setScreenOpacity] = useState(1);

  // Animated screen transition: fade out → switch → fade in (300ms total)
  const setScreen = (next: ScreenName) => {
    if (transitioning) return;
    setTransitioning(true);
    setScreenOpacity(0); // Fade out
    setTimeout(() => {
      setScreenRaw(next);
      // Small delay before fade-in to ensure new screen mounts
      requestAnimationFrame(() => {
        setScreenOpacity(1); // Fade in
        setTimeout(() => setTransitioning(false), 300);
      });
    }, 250); // Wait for fade-out to finish
  };

  // Instant screen switch (no animation — for backend-triggered transitions)
  const setScreenInstant = (next: ScreenName) => {
    setScreenRaw(next);
    setScreenOpacity(1);
  };

  // Transition wrapper style applied to all screen containers
  const screenTransitionStyle: React.CSSProperties = {
    opacity: screenOpacity,
    transition: 'opacity 250ms ease-in-out',
  };
  const [showSettings, setShowSettings] = useState(false);
  const [showTutorial, setShowTutorial] = useState(false);
  const [hasSaves, setHasSaves] = useState(false);
  const [showSaveDialog, setShowSaveDialog] = useState(false);
  const [showLoadScreen, setShowLoadScreen] = useState(false);
  const [showShortcuts, setShowShortcuts] = useState(false);
  const [alertToasts, setAlertToasts] = useState<{ id: number; symbol: string; condition: string; targetPrice: number; currentPrice: number }[]>([]);
  const [shareholderVote, setShareholderVote] = useState<{ symbol: string; companyName: string; proposal: string; voteType: string; ownershipPercent: number; priceImpact: number } | null>(null);

  // Auto-dismiss alert toasts after 5 seconds
  useEffect(() => {
    if (alertToasts.length === 0) return;
    const timer = setTimeout(() => {
      setAlertToasts(prev => prev.slice(1));
    }, 5000);
    return () => clearTimeout(timer);
  }, [alertToasts]);
  const [daySummary, setDaySummary] = useState<Record<string, unknown> | null>(null);
  const [gameSettings, setGameSettings] = useState<GameSettings>(() => {
    try {
      const saved = localStorage.getItem('stocksim-settings');
      return saved ? { ...DEFAULT_SETTINGS, ...JSON.parse(saved) } : DEFAULT_SETTINGS;
    } catch { return DEFAULT_SETTINGS; }
  });
  const [showCommandBar, setShowCommandBar] = useState(false);
  const [showGlossary, setShowGlossary] = useState(false);
  const [showWiki, setShowWiki] = useState(false);
  const [activeDecision, setActiveDecision] = useState<{ point: DecisionPoint; caseName: string } | null>(null);
  const [decisionCaseId, setDecisionCaseId] = useState<string | null>(null);
  const [completedDecisions, setCompletedDecisions] = useState<Set<string>>(new Set());
  const [careerSummary, setCareerSummary] = useState<Record<string, unknown> | null>(null);
  const [achievementFading, setAchievementFading] = useState(false);
  const achievementTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const settingsRef = useRef(gameSettings);
  useEffect(() => { settingsRef.current = gameSettings; }, [gameSettings]);

  // Auto-dismiss achievement popup after 5s with fade-out
  useEffect(() => {
    if (achievementPopup) {
      setAchievementFading(false);
      if (achievementTimerRef.current) clearTimeout(achievementTimerRef.current);
      achievementTimerRef.current = setTimeout(() => {
        setAchievementFading(true);
        setTimeout(() => dismissAchievementPopup(), 400);
      }, 5000);
    }
    return () => { if (achievementTimerRef.current) clearTimeout(achievementTimerRef.current); };
  }, [achievementPopup, dismissAchievementPopup]);

  // Keyboard shortcuts (Bible 18)
  useKeyboardShortcuts(wsClient);

  // Listen for shortcuts help toggle (? key opens Glossary now)
  useEffect(() => {
    const handler = () => setShowGlossary(v => !v);
    window.addEventListener('toggleShortcutsHelp', handler);
    return () => window.removeEventListener('toggleShortcutsHelp', handler);
  }, []);

  // Ctrl+W toggles wiki
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 'w') {
        e.preventDefault();
        setShowWiki(v => !v);
      }
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
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

    // Colorblind mode
    document.body.classList.remove('colorblind-deuteranopia', 'colorblind-protanopia', 'colorblind-tritanopia');
    if (gameSettings.colorblindMode && gameSettings.colorblindMode !== 'off') {
      document.body.classList.add(`colorblind-${gameSettings.colorblindMode}`);
    }

    // Ticker speed
    document.documentElement.style.setProperty('--ticker-speed', `${gameSettings.newsTickerSpeed}s`);

    // Music volume
    audio.setMusicVolume(gameSettings.musicVolume / 100);

    // Persist
    try { localStorage.setItem('stocksim-settings', JSON.stringify(gameSettings)); } catch {}
    window.dispatchEvent(new Event('settingsChanged'));

    // Sync simulation settings to backend
    wsClient.send('UpdateSettings', {
      TradingCommission: gameSettings.tradingCommission,
      CommissionAmount: gameSettings.commissionAmount,
      EnableTaxes: gameSettings.enableTaxes,
      SmaEnforcement: gameSettings.smaEnforcement,
      SkipWeekends: gameSettings.skipWeekends,
      AutoPauseOnShortSqueeze: gameSettings.autoPauseOnShortSqueeze,
      AutoPauseOnSma: gameSettings.smaEnforcement,
      AutoPauseOnNews: gameSettings.autoPauseOnNews,
      AutoPauseOnAlert: gameSettings.autoPauseOnAlert,
      AutoPauseOnMarketOpen: gameSettings.autoPauseOnMarketOpen,
      AutoPauseOnMarginCall: gameSettings.autoPauseOnMarginCall,
      AutoPauseOnOrderExecution: gameSettings.autoPauseOnOrderExecution,
    });
  }, [gameSettings]);

  // Ctrl+K for Command Bar + custom event from keyboard shortcuts
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        setShowCommandBar(v => !v);
      }
    };
    const closeAllModals = () => { setShowCommandBar(false); setShowGlossary(false); setShowSettings(false); };
    const customHandler = () => { closeAllModals(); setShowCommandBar(true); };
    const glossaryHandler = () => { setShowCommandBar(false); setShowSettings(false); setShowGlossary(v => !v); };
    const fullscreenHandler = () => {
      if (document.fullscreenElement) document.exitFullscreen?.();
      else document.documentElement.requestFullscreen?.();
    };
    const newGameHandler = () => setScreen('title');
    const loadGameHandler = () => setScreen('title');
    const confirmOrderHandler = () => window.dispatchEvent(new CustomEvent('confirmOrderAccept'));

    window.addEventListener('keydown', handler);
    window.addEventListener('openCommandBar', customHandler);
    window.addEventListener('toggleGlossary', glossaryHandler);
    window.addEventListener('toggleFullscreen', fullscreenHandler);
    window.addEventListener('newGame', newGameHandler);
    window.addEventListener('loadGame', loadGameHandler);
    window.addEventListener('confirmOrder', confirmOrderHandler);
    return () => {
      window.removeEventListener('keydown', handler);
      window.removeEventListener('openCommandBar', customHandler);
      window.removeEventListener('toggleGlossary', glossaryHandler);
      window.removeEventListener('toggleFullscreen', fullscreenHandler);
      window.removeEventListener('newGame', newGameHandler);
      window.removeEventListener('loadGame', loadGameHandler);
      window.removeEventListener('confirmOrder', confirmOrderHandler);
    };
  }, []);

  useEffect(() => {
    log.info('App mounting, connecting to backend');

    // Register message handlers (store unsub functions for cleanup)
    const unsubs: (() => void)[] = [];

    unsubs.push(wsClient.on('MarketSnapshot', (payload) => {
      const snapshot = payload as MarketSnapshot;
      useMarketStore.getState().resetGameState(); // Clear stale data from previous game
      setStocks(snapshot.stocks);
      setSpeed(snapshot.speed);
      if (snapshot.marketPhase) {
        useMarketStore.setState({ marketPhase: snapshot.marketPhase });
      }
      // Process initial news bundled with snapshot (avoids race condition)
      if (snapshot.initialNews?.length) {
        addNewsEvents(snapshot.initialNews);
        log.info('Initial news loaded', { count: snapshot.initialNews.length });
      }
      log.info('Market snapshot received', { stocks: snapshot.stocks.length });
      setScreenInstant('ingame');
      wsClient.send('GetPortfolio', {});
    }));

    // Throttle price updates via requestAnimationFrame for smooth rendering
    // At 10x speed, multiple updates may arrive per frame — only process the latest
    let pendingUpdate: MarketUpdate | null = null;
    let rafId: number | null = null;
    unsubs.push(wsClient.on('MarketUpdate', (payload) => {
      pendingUpdate = payload as MarketUpdate;
      if (rafId === null) {
        rafId = requestAnimationFrame(() => {
          if (pendingUpdate) updatePrices(pendingUpdate);
          pendingUpdate = null;
          rafId = null;
        });
      }
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
        if (settingsRef.current.tradeSound) {
          if (result.order.side === 'Buy' || result.order.side === 'Cover') audio.orderFilledBuy();
          else audio.orderFilledSell();
        }
      } else if (!result.success) {
        if (settingsRef.current.tradeSound) audio.orderRejected();
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
      if (settingsRef.current.newsAlertSound) {
        if (data.events.some(e => e.severity === 'Major')) audio.breakingNews();
        if (data.events.some(e => e.headline.includes('FLASH CRASH'))) audio.flashCrash();
      }
    }));

    unsubs.push(wsClient.on('OrderbookData', (payload) => {
      setOrderbookData(payload as OrderbookData);
    }));

    unsubs.push(wsClient.on('DaySummary', (payload) => {
      // Daily summary modal disabled — was annoying during gameplay
      // Data still received for decision case triggers + market bell
      if (settingsRef.current.marketBellSound) audio.marketBell();

      // Check decision case triggers
      const caseId = localStorage.getItem('activeDecisionCase');
      if (caseId) {
        const dc = DECISION_CASES.find(c => c.id === caseId);
        const dayNum = (payload as Record<string, unknown>).dayNumber as number ?? 0;
        if (dc) {
          const pending = dc.decisions.find(d =>
            d.triggerDay <= dayNum && !completedDecisions.has(`${caseId}-${d.triggerDay}`)
          );
          if (pending) {
            setActiveDecision({ point: pending, caseName: dc.name });
          }
        }
      }
    }));

    unsubs.push(wsClient.on('AlertTriggered', (payload) => {
      if (settingsRef.current.newsAlertSound) audio.priceAlert();
      const data = payload as { symbol: string; condition: string; targetPrice: number; currentPrice: number };
      setAlertToasts(prev => [...prev.slice(-4), {
        id: Date.now(),
        symbol: data.symbol,
        condition: data.condition,
        targetPrice: data.targetPrice,
        currentPrice: data.currentPrice,
      }]);
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

    unsubs.push(wsClient.on('ScenarioProgress', (payload) => {
      useMarketStore.getState().setScenarioProgress(payload as ScenarioProgress);
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
      if (settingsRef.current.newsAlertSound) audio.notification();
    }));

    unsubs.push(wsClient.on('ShortSqueezeWarning', (payload) => {
      const data = payload as ShortSqueezeWarning;
      setShortSqueezeWarning(data);
      if (settingsRef.current.newsAlertSound) audio.shortSqueezeAlarm();
    }));

    unsubs.push(wsClient.on('TenderOffer', (payload) => {
      setTenderOffer(payload as TenderOffer);
      if (settingsRef.current.newsAlertSound) audio.notification();
    }));

    unsubs.push(wsClient.on('ShareholderVote', (payload) => {
      const vote = payload as { symbol: string; companyName: string; proposal: string; voteType: string; ownershipPercent: number; priceImpact: number };
      setShareholderVote(vote);
      if (settingsRef.current.newsAlertSound) audio.notification();
    }));

    unsubs.push(wsClient.on('PDTWarning', () => {
      setAlertToasts(prev => [...prev.slice(-4), {
        id: Date.now(),
        symbol: 'PDT',
        condition: 'warning',
        targetPrice: 0,
        currentPrice: 0,
      }]);
    }));

    unsubs.push(wsClient.on('TaxSummary', (payload) => {
      // Store in marketStore or just dispatch event for CentralArea
      window.dispatchEvent(new CustomEvent('taxSummary', { detail: payload }));
    }));

    unsubs.push(wsClient.on('SaveList', (payload) => {
      const data = payload as { saves: { fileName: string }[] };
      setHasSaves(data.saves?.length > 0);
    }));

    unsubs.push(wsClient.on('GameSaved', (payload) => {
      const data = payload as { success: boolean; error?: string };
      if (data.success) {
        setHasSaves(true); // A save now exists
      } else {
        log.warn('Save failed', { error: data.error });
        window.dispatchEvent(new CustomEvent('gameSaveError', { detail: data.error }));
      }
    }));

    unsubs.push(wsClient.on('GameLoaded', (payload) => {
      const data = payload as { success: boolean; error?: string };
      if (!data.success) {
        log.warn('Load failed', { error: data.error });
        // Stay on title screen — user sees no transition
      } else {
        log.info('Game loaded successfully');
      }
    }));

    unsubs.push(wsClient.on('welcome', () => {
      setConnected(true);
      log.info('Backend handshake complete');
      wsClient.send('ListSaves', {}); // Check for available saves
      setScreenInstant('title');
    }));

    // Preload audio assets
    audio.preload();

    // Connect
    wsClient.connect();

    // Show error screen if backend doesn't connect within 15s
    const connectionTimeout = setTimeout(() => {
      if (wsClient.state !== 'connected') {
        setScreenInstant('connectionFailed');
      }
    }, 15000);

    return () => {
      unsubs.forEach(fn => fn());
      clearTimeout(connectionTimeout);
      if (rafId !== null) cancelAnimationFrame(rafId);
      wsClient.disconnect();
    };
  }, []);

  // Request OHLCV data and indicators when a stock is selected
  useEffect(() => {
    if (selectedSymbol) {
      wsClient.send('GetOHLCV', { symbol: selectedSymbol });
      wsClient.send('GetIndicators', { symbol: selectedSymbol, indicators: ['SMA20', 'SMA50', 'SMA200', 'RSI', 'BOLLINGER', 'VWAP'] });
      wsClient.send('GetOrderbook', { symbol: selectedSymbol });
      wsClient.send('GetStockFundamentals', { symbol: selectedSymbol });
    } else {
      setStockFundamentals(null);
    }
  }, [selectedSymbol]);

  // Title Screen
  if (screen === 'connecting') {
    return (
      <div style={{
        display: 'flex', alignItems: 'center', justifyContent: 'center', flexDirection: 'column',
        height: '100vh', background: 'var(--bg-primary)', gap: '24px',
        ...screenTransitionStyle,
      }}>
        <h1 className="mono pulse" style={{
          fontSize: '42px', fontWeight: 700, color: 'var(--text-accent)',
          letterSpacing: '8px', textShadow: '0 0 30px rgba(96,165,250,0.3)',
          margin: 0,
        }}>STOCKSIM</h1>
        <div style={{
          width: '200px', height: '3px', background: 'var(--bg-tertiary)',
          borderRadius: '2px', overflow: 'hidden',
        }}>
          <div style={{
            width: '40%', height: '100%', background: 'var(--text-accent)',
            borderRadius: '2px',
            animation: 'splashLoadingBar 1.5s ease-in-out infinite',
          }} />
        </div>
        <p style={{
          color: 'var(--text-disabled)', fontSize: '13px', margin: 0,
          letterSpacing: '1px',
        }}>Connecting to market...</p>
        <style>{`
          @keyframes splashLoadingBar {
            0% { transform: translateX(-200%); }
            100% { transform: translateX(400%); }
          }
        `}</style>
      </div>
    );
  }

  if (screen === 'connectionFailed') {
    return (
      <div style={{
        display: 'flex', alignItems: 'center', justifyContent: 'center', flexDirection: 'column',
        height: '100vh', background: 'var(--bg-primary)', gap: '20px', padding: '40px',
        ...screenTransitionStyle,
      }}>
        <h1 className="mono" style={{
          fontSize: '42px', fontWeight: 700, color: 'var(--text-accent)',
          letterSpacing: '8px', textShadow: '0 0 30px rgba(96,165,250,0.3)',
          margin: 0,
        }}>STOCKSIM</h1>
        <div style={{
          background: 'var(--bg-secondary)', border: '1px solid var(--border-color)',
          borderRadius: '8px', padding: '24px 32px', maxWidth: '480px', textAlign: 'center',
        }}>
          <p style={{ color: '#ef4444', fontSize: '16px', fontWeight: 600, margin: '0 0 12px' }}>
            Connection Failed
          </p>
          <p style={{ color: 'var(--text-secondary)', fontSize: '13px', lineHeight: '1.6', margin: '0 0 16px' }}>
            Could not connect to the game engine. This usually means:
          </p>
          <ul style={{
            color: 'var(--text-muted)', fontSize: '13px', lineHeight: '1.8',
            textAlign: 'left', margin: '0 0 20px', paddingLeft: '20px',
          }}>
            <li>Windows Firewall is blocking the connection</li>
            <li>Antivirus software is blocking StockSim.Engine.exe</li>
            <li>The game engine crashed on startup</li>
          </ul>
          <button
            onClick={() => { setScreenInstant('connecting'); wsClient.connect(); }}
            style={{
              background: 'var(--text-accent)', color: '#fff', border: 'none',
              borderRadius: '6px', padding: '10px 24px', fontSize: '14px',
              fontWeight: 600, cursor: 'pointer',
            }}
          >
            Retry Connection
          </button>
        </div>
      </div>
    );
  }

  if (screen === 'title') {
    return (
      <div style={screenTransitionStyle}>
        <TitleScreen
          hasSaves={hasSaves}
          onNewGame={() => setScreen('newgame')}
          onContinue={() => wsClient.send('LoadGame', {})}
          onLoadGame={() => setShowLoadScreen(true)}
          onSettings={() => setShowSettings(true)}
          onQuit={() => { wsClient.send('shutdown', {}); window.close(); }}
        />
        <LoadScreen
          isOpen={showLoadScreen}
          onClose={() => setShowLoadScreen(false)}
          onLoad={(filePath) => { setShowLoadScreen(false); wsClient.send('LoadGame', { FilePath: filePath }); }}
          wsClient={wsClient}
        />
        <SettingsModal isOpen={showSettings} onClose={() => setShowSettings(false)}
          settings={gameSettings} onSettingsChange={setGameSettings} />
      </div>
    );
  }

  if (screen === 'newgame') {
    return (
      <div style={screenTransitionStyle}>
      <NewGameScreen
        wsClient={wsClient}
        onBack={() => setScreen('title')}
        onStart={(config: GameConfig) => {
          if (config.showTutorial) setShowTutorial(true);
          useMarketStore.setState({ beginnerMode: config.difficulty === 'easy' });
          // Check for day-0 decision case trigger
          const caseId = localStorage.getItem('activeDecisionCase');
          if (caseId) {
            setDecisionCaseId(caseId);
            const dc = DECISION_CASES.find(c => c.id === caseId);
            const day0 = dc?.decisions.find(d => d.triggerDay === 0);
            if (dc && day0) {
              setTimeout(() => setActiveDecision({ point: day0, caseName: dc.name }), 2000);
            }
          }
        }}
      />
      </div>
    );
  }

  // InGame HUD
  return (
    <div className="app-container" style={screenTransitionStyle}>
      <TopBar wsClient={wsClient} onOpenSettings={() => { setShowCommandBar(false); setShowGlossary(false); setShowWiki(false); setShowSettings(true); }} onOpenCommandBar={() => { setShowSettings(false); setShowGlossary(false); setShowWiki(false); setShowCommandBar(true); }} onOpenWiki={() => { setShowSettings(false); setShowGlossary(false); setShowCommandBar(false); setShowWiki(true); }} onMainMenu={() => setScreen('title')} onSave={() => setShowSaveDialog(true)} />
      <ScenarioBar />
      <AccountBar />
      <div className="main-layout">
        <LeftSidebar />
        <CentralArea wsClient={wsClient} />
        <RightSidebar wsClient={wsClient} />
      </div>
      <NewsTicker />
      <SaveDialog isOpen={showSaveDialog} onClose={() => setShowSaveDialog(false)} wsClient={wsClient} />
      <TutorialOverlay isOpen={showTutorial} onClose={() => setShowTutorial(false)} />
      <ShortcutsHelp isOpen={showShortcuts} onClose={() => setShowShortcuts(false)} />
      <GlossaryModal isOpen={showGlossary} onClose={() => setShowGlossary(false)} />
      <WikiModal isOpen={showWiki} onClose={() => setShowWiki(false)} />
      {activeDecision && (
        <DecisionCaseModal
          decision={activeDecision.point}
          caseName={activeDecision.caseName}
          onClose={() => {
            const key = `${decisionCaseId}-${activeDecision.point.triggerDay}`;
            setCompletedDecisions(prev => new Set(prev).add(key));
            setActiveDecision(null);
          }}
        />
      )}

      {/* Tender Offer Popup (Bible 8.2.7) */}
      {tenderOffer && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.6)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 5000 }}>
          <div style={{ width: '440px', background: 'var(--bg-secondary)', border: '2px solid var(--warning)', borderRadius: '10px', padding: '24px' }}>
            <div style={{ fontSize: '11px', fontWeight: 700, color: 'var(--warning)', letterSpacing: '1.5px', marginBottom: '12px' }}>TENDER OFFER</div>
            <p style={{ fontSize: '14px', color: 'var(--text-secondary)', lineHeight: 1.6, margin: '0 0 16px' }}>
              <strong style={{ color: 'var(--text-primary)' }}>{tenderOffer.acquirerName}</strong> is offering{' '}
              <strong className="mono" style={{ color: 'var(--green-primary)' }}>${tenderOffer.offerPrice.toFixed(2)}</strong> per share
              for your <strong style={{ color: 'var(--text-primary)' }}>{tenderOffer.targetName}</strong> holdings.
            </p>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px', marginBottom: '16px', fontSize: '13px' }}>
              <div style={{ background: 'var(--bg-tertiary)', borderRadius: '6px', padding: '8px 12px' }}>
                <div style={{ color: 'var(--text-disabled)', fontSize: '10px' }}>Current Price</div>
                <div className="mono" style={{ color: 'var(--text-primary)', fontWeight: 700 }}>${tenderOffer.currentPrice.toFixed(2)}</div>
              </div>
              <div style={{ background: 'var(--bg-tertiary)', borderRadius: '6px', padding: '8px 12px' }}>
                <div style={{ color: 'var(--text-disabled)', fontSize: '10px' }}>Offer Price</div>
                <div className="mono" style={{ color: 'var(--green-primary)', fontWeight: 700 }}>${tenderOffer.offerPrice.toFixed(2)}</div>
              </div>
              <div style={{ background: 'var(--bg-tertiary)', borderRadius: '6px', padding: '8px 12px' }}>
                <div style={{ color: 'var(--text-disabled)', fontSize: '10px' }}>Premium</div>
                <div className="mono" style={{ color: 'var(--green-primary)', fontWeight: 700 }}>+{tenderOffer.premiumPercent}%</div>
              </div>
              <div style={{ background: 'var(--bg-tertiary)', borderRadius: '6px', padding: '8px 12px' }}>
                <div style={{ color: 'var(--text-disabled)', fontSize: '10px' }}>Your Shares</div>
                <div className="mono" style={{ color: 'var(--text-primary)', fontWeight: 700 }}>{tenderOffer.playerShares}</div>
              </div>
            </div>
            <div style={{ background: 'rgba(16,185,129,0.1)', borderRadius: '6px', padding: '10px 14px', marginBottom: '16px', textAlign: 'center' }}>
              <div style={{ color: 'var(--text-disabled)', fontSize: '10px' }}>Total Payout</div>
              <div className="mono" style={{ color: 'var(--green-primary)', fontSize: '20px', fontWeight: 700 }}>${tenderOffer.totalPayout.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</div>
            </div>
            <div style={{ display: 'flex', gap: '10px' }}>
              <button
                onClick={() => {
                  // Accept: sell shares at offer price via WebSocket
                  wsClient.send('AcceptTenderOffer', { symbol: tenderOffer.targetSymbol, offerPrice: tenderOffer.offerPrice });
                  setTenderOffer(null);
                }}
                style={{ flex: 1, padding: '10px', borderRadius: '6px', background: 'var(--green-primary)', color: 'var(--text-primary)', border: 'none', cursor: 'pointer', fontWeight: 700, fontSize: '14px', fontFamily: 'var(--font-ui)' }}
              >Accept Offer</button>
              <button
                onClick={() => setTenderOffer(null)}
                style={{ flex: 1, padding: '10px', borderRadius: '6px', background: 'var(--bg-tertiary)', color: 'var(--text-secondary)', border: '1px solid var(--border)', cursor: 'pointer', fontWeight: 600, fontSize: '14px', fontFamily: 'var(--font-ui)' }}
              >Decline / Hold</button>
            </div>
          </div>
        </div>
      )}

      {/* Shareholder Vote Modal */}
      {shareholderVote && (
        <div style={{ position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.6)', display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 5500 }}>
          <div style={{ width: '440px', background: 'var(--bg-secondary)', border: '2px solid var(--chart-purple)', borderRadius: '10px', padding: '24px' }}>
            <div style={{ textAlign: 'center', marginBottom: '16px' }}>
              <span style={{ fontSize: '10px', color: 'var(--chart-purple)', letterSpacing: '2px', fontWeight: 700 }}>SHAREHOLDER VOTE</span>
              <h3 style={{ fontSize: '18px', fontWeight: 700, color: 'var(--text-primary)', margin: '4px 0' }}>{shareholderVote.companyName}</h3>
              <span className="mono" style={{ fontSize: '12px', color: 'var(--text-disabled)' }}>
                You own {shareholderVote.ownershipPercent.toFixed(1)}% of outstanding shares
              </span>
            </div>
            <div style={{
              background: 'var(--bg-tertiary)', borderRadius: '6px', padding: '14px',
              marginBottom: '16px', fontSize: '14px', color: 'var(--text-primary)', lineHeight: 1.6,
            }}>
              {shareholderVote.proposal}
            </div>
            <div style={{ display: 'flex', gap: '10px' }}>
              <button onClick={() => {
                wsClient.send('ShareholderVoteResponse', { symbol: shareholderVote.symbol, voteType: shareholderVote.voteType, approved: true });
                setShareholderVote(null);
              }} style={{
                flex: 1, padding: '10px', borderRadius: '6px', background: 'var(--green-primary)',
                color: 'var(--text-primary)', border: 'none', cursor: 'pointer', fontWeight: 700, fontSize: '14px',
              }}>Vote YES</button>
              <button onClick={() => {
                wsClient.send('ShareholderVoteResponse', { symbol: shareholderVote.symbol, voteType: shareholderVote.voteType, approved: false });
                setShareholderVote(null);
              }} style={{
                flex: 1, padding: '10px', borderRadius: '6px', background: 'var(--red-primary)',
                color: 'var(--text-primary)', border: 'none', cursor: 'pointer', fontWeight: 700, fontSize: '14px',
              }}>Vote NO</button>
              <button onClick={() => setShareholderVote(null)} style={{
                padding: '10px 16px', borderRadius: '6px', background: 'var(--bg-tertiary)',
                color: 'var(--text-secondary)', border: '1px solid var(--border)', cursor: 'pointer', fontSize: '13px',
              }}>Abstain</button>
            </div>
          </div>
        </div>
      )}

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
              color: 'var(--text-primary)', border: 'none', cursor: 'pointer', fontWeight: 700, fontSize: '14px', marginTop: '8px',
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
            background: 'linear-gradient(180deg, var(--bg-secondary), var(--bg-primary))',
            border: '1px solid var(--gold-primary)', borderRadius: '12px', padding: '32px',
            boxShadow: '0 0 60px rgba(212,175,55,0.15)',
          }}>
            <div style={{ textAlign: 'center', marginBottom: '24px' }}>
              <div style={{ fontSize: '12px', color: 'var(--gold-primary)', letterSpacing: '4px', fontWeight: 600 }}>CAREER COMPLETE</div>
              <h2 style={{ fontSize: '32px', fontWeight: 900, color: 'var(--gold-light)', margin: '8px 0', letterSpacing: '2px' }}>RETIRED</h2>
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
                  <div style={{ fontSize: '10px', color: 'var(--gold-primary)', letterSpacing: '1px', marginBottom: '4px' }}>{String(label).toUpperCase()}</div>
                  <div className="mono" style={{ fontSize: '14px', fontWeight: 700, color: 'var(--text-primary)' }}>{String(value)}</div>
                </div>
              ))}
            </div>

            <div style={{ display: 'flex', gap: '12px', justifyContent: 'center' }}>
              <button onClick={() => { setCareerSummary(null); setScreen('newgame'); }} style={{
                padding: '10px 32px', borderRadius: '6px', fontSize: '14px', fontWeight: 700,
                background: 'linear-gradient(135deg, var(--gold-primary), var(--gold-dark))', color: 'var(--text-primary)',
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
                color: 'var(--text-primary)', border: 'none', cursor: 'pointer', fontWeight: 700, fontSize: '14px',
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
            border: `1px solid ${scenarioResult.won ? 'var(--gold-primary)' : 'var(--red-primary)'}`,
            borderRadius: '10px', padding: '32px', textAlign: 'center',
          }}>
            <h2 style={{
              fontSize: '24px', fontWeight: 900, marginBottom: '4px',
              color: scenarioResult.won ? 'var(--gold-primary)' : 'var(--red-primary)',
              letterSpacing: '2px',
            }}>
              {scenarioResult.won ? 'SCENARIO COMPLETE' : 'SCENARIO FAILED'}
            </h2>
            <p style={{ fontSize: '16px', color: 'var(--text-primary)', fontWeight: 600 }}>{scenarioResult.scenarioName}</p>
            {!scenarioResult.won && scenarioResult.failReason && (
              <p style={{ color: 'var(--red-primary)', fontSize: '13px', marginTop: '4px' }}>{scenarioResult.failReason}</p>
            )}

            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '8px', margin: '16px 0' }}>
              <div style={{ background: 'var(--bg-tertiary)', borderRadius: '6px', padding: '10px' }}>
                <span style={{ fontSize: '10px', color: 'var(--text-disabled)', display: 'block' }}>PORTFOLIO</span>
                <span className="mono" style={{ fontSize: '18px', fontWeight: 700, color: 'var(--text-primary)' }}>
                  ${scenarioResult.finalPortfolioValue.toFixed(0)}
                </span>
              </div>
              <div style={{ background: 'var(--bg-tertiary)', borderRadius: '6px', padding: '10px' }}>
                <span style={{ fontSize: '10px', color: 'var(--text-disabled)', display: 'block' }}>YOUR RETURN</span>
                <span className="mono" style={{
                  fontSize: '18px', fontWeight: 700,
                  color: scenarioResult.totalReturnPercent >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                }}>
                  {scenarioResult.totalReturnPercent >= 0 ? '+' : ''}{scenarioResult.totalReturnPercent.toFixed(1)}%
                </span>
              </div>
              <div style={{ background: 'var(--bg-tertiary)', borderRadius: '6px', padding: '10px' }}>
                <span style={{ fontSize: '10px', color: 'var(--text-disabled)', display: 'block' }}>DAYS PLAYED</span>
                <span className="mono" style={{ fontSize: '18px', fontWeight: 700, color: 'var(--text-primary)' }}>
                  {scenarioResult.daysElapsed}
                </span>
              </div>
            </div>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '8px', marginBottom: '16px' }}>
              <div style={{ background: 'var(--bg-tertiary)', borderRadius: '6px', padding: '8px' }}>
                <span style={{ fontSize: '10px', color: 'var(--text-disabled)', display: 'block' }}>TRADES</span>
                <span className="mono" style={{ fontSize: '14px', fontWeight: 700, color: 'var(--text-primary)' }}>
                  {scenarioResult.totalTrades}
                </span>
              </div>
              <div style={{ background: 'var(--bg-tertiary)', borderRadius: '6px', padding: '8px' }}>
                <span style={{ fontSize: '10px', color: 'var(--text-disabled)', display: 'block' }}>WIN RATE</span>
                <span className="mono" style={{
                  fontSize: '14px', fontWeight: 700,
                  color: scenarioResult.winRate >= 50 ? 'var(--green-primary)' : 'var(--red-primary)',
                }}>
                  {scenarioResult.winRate.toFixed(0)}%
                </span>
              </div>
              <div style={{ background: 'var(--bg-tertiary)', borderRadius: '6px', padding: '8px' }}>
                <span style={{ fontSize: '10px', color: 'var(--text-disabled)', display: 'block' }}>
                  {scenarioResult.won ? 'RESULT' : 'STATUS'}
                </span>
                <span style={{
                  fontSize: '14px', fontWeight: 700,
                  color: scenarioResult.won ? 'var(--gold-primary)' : 'var(--red-primary)',
                }}>
                  {scenarioResult.won ? 'Victory' : 'Defeated'}
                </span>
              </div>
            </div>

            {/* History Mode: comparison with actual market */}
            {scenarioResult.scenarioId.startsWith('history_') && (() => {
              const historyData: Record<string, { ret: number; date: string }> = {
                history_black_monday: { ret: -22.6, date: 'Oct 19, 1987' },
                history_dotcom: { ret: -49.1, date: 'Mar 2000' },
                history_2008: { ret: -38.5, date: 'Sep 2008' },
                history_flash_crash: { ret: -3.2, date: 'May 6, 2010' },
                history_covid: { ret: -33.9, date: 'Feb 2020' },
                history_gamestop: { ret: 0, date: 'Jan 2021' },
                history_volcker: { ret: -27.1, date: '1980' },
                history_oil_2020: { ret: -44.0, date: 'Apr 2020' },
              };
              const h = historyData[scenarioResult.scenarioId];
              if (!h) return null;
              const playerBeat = scenarioResult.totalReturnPercent > h.ret;
              return (
                <div style={{
                  background: playerBeat ? 'rgba(16,185,129,0.08)' : 'rgba(239,68,68,0.08)',
                  border: `1px solid ${playerBeat ? 'rgba(16,185,129,0.3)' : 'rgba(239,68,68,0.3)'}`,
                  borderRadius: '6px', padding: '12px', marginBottom: '16px',
                }}>
                  <div style={{ fontSize: '11px', color: 'var(--text-disabled)', marginBottom: '4px', textTransform: 'uppercase', letterSpacing: '1px' }}>
                    Historical Comparison — {h.date}
                  </div>
                  <div style={{ display: 'flex', justifyContent: 'center', gap: '24px', alignItems: 'baseline' }}>
                    <div style={{ textAlign: 'center' }}>
                      <div style={{ fontSize: '10px', color: 'var(--text-disabled)' }}>You</div>
                      <span className="mono" style={{
                        fontSize: '20px', fontWeight: 700,
                        color: scenarioResult.totalReturnPercent >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                      }}>{scenarioResult.totalReturnPercent >= 0 ? '+' : ''}{scenarioResult.totalReturnPercent.toFixed(1)}%</span>
                    </div>
                    <span style={{ fontSize: '16px', color: 'var(--text-disabled)' }}>vs</span>
                    <div style={{ textAlign: 'center' }}>
                      <div style={{ fontSize: '10px', color: 'var(--text-disabled)' }}>S&P 500 Actual</div>
                      <span className="mono" style={{
                        fontSize: '20px', fontWeight: 700,
                        color: h.ret >= 0 ? 'var(--green-primary)' : 'var(--red-primary)',
                      }}>{h.ret >= 0 ? '+' : ''}{h.ret.toFixed(1)}%</span>
                    </div>
                  </div>
                  <div className="mono" style={{
                    textAlign: 'center', marginTop: '8px', fontSize: '13px', fontWeight: 700,
                    color: playerBeat ? 'var(--green-primary)' : 'var(--red-primary)',
                  }}>
                    {playerBeat ? `You beat the market by ${(scenarioResult.totalReturnPercent - h.ret).toFixed(1)}%` : `Market beat you by ${(h.ret - scenarioResult.totalReturnPercent).toFixed(1)}%`}
                  </div>
                </div>
              );
            })()}

            <div style={{ display: 'flex', gap: '12px', justifyContent: 'center' }}>
              <button onClick={() => { setScenarioResult(null); setScreen('newgame'); }} style={{
                padding: '10px 24px', borderRadius: '6px', background: 'var(--text-accent)',
                color: 'var(--text-primary)', border: 'none', cursor: 'pointer', fontWeight: 700, fontSize: '14px',
              }}>New Game</button>
              <button onClick={() => {
                const sid = scenarioResult.scenarioId;
                setScenarioResult(null);
                setScreen('newgame');
                // Auto-select same scenario for retry
                setTimeout(() => wsClient.send('StartScenario', { scenarioId: sid }), 500);
              }} style={{
                padding: '10px 24px', borderRadius: '6px', background: 'var(--warning)',
                color: 'var(--bg-primary)', border: 'none', cursor: 'pointer', fontWeight: 700, fontSize: '14px',
              }}>Retry</button>
              <button onClick={() => setScenarioResult(null)} style={{
                padding: '10px 24px', borderRadius: '6px', background: 'var(--bg-tertiary)',
                color: 'var(--text-primary)', border: '1px solid var(--border)', cursor: 'pointer', fontWeight: 600, fontSize: '14px',
              }}>Continue</button>
            </div>
          </div>
        </div>
      )}

      {/* Price Alert Toasts */}
      {alertToasts.length > 0 && (
        <div style={{
          position: 'fixed', top: '80px', left: '24px', zIndex: 9997,
          display: 'flex', flexDirection: 'column', gap: '8px', maxWidth: '350px',
        }}>
          {alertToasts.slice(-3).map((toast) => (
            <div
              key={toast.id}
              onClick={() => { setAlertToasts(prev => prev.filter(t => t.id !== toast.id)); selectStock(toast.symbol); }}
              style={{
                background: 'rgba(245,158,11,0.15)', border: '1px solid var(--warning)',
                borderRadius: '8px', padding: '10px 14px', cursor: 'pointer',
                animation: 'slideDown 0.3s ease-out',
                boxShadow: '0 4px 20px rgba(0, 0, 0, 0.4)',
              }}
            >
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '12px' }}>
                <span style={{ fontSize: '16px' }}>🔔</span>
                <div>
                  <span style={{ fontWeight: 700, color: 'var(--warning)' }}>PRICE ALERT</span>
                  <span className="mono" style={{ color: 'var(--text-primary)', fontWeight: 700, marginLeft: '8px' }}>{toast.symbol}</span>
                </div>
              </div>
              <div className="mono" style={{ fontSize: '12px', color: 'var(--text-secondary)', marginTop: '4px' }}>
                {toast.condition === 'above' ? 'Crossed above' : 'Crossed below'} ${toast.targetPrice.toFixed(2)}
                <span style={{ color: 'var(--text-disabled)', marginLeft: '8px' }}>Now: ${toast.currentPrice.toFixed(2)}</span>
              </div>
            </div>
          ))}
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
                ? 'var(--warning)'
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
          className={`achievement-popup${achievementFading ? ' fade-out' : ''}`}
          onClick={() => { setAchievementFading(true); setTimeout(dismissAchievementPopup, 400); }}
          style={{
            position: 'fixed',
            top: '24px',
            left: '50%',
            zIndex: 9999,
            background: 'linear-gradient(135deg, rgba(212, 175, 55, 0.18), rgba(212, 175, 55, 0.06))',
            backdropFilter: 'blur(12px)',
            border: '1px solid var(--gold-primary)',
            borderRadius: '12px',
            padding: '16px 32px',
            display: 'flex',
            alignItems: 'center',
            gap: '16px',
            cursor: 'pointer',
          }}
        >
          <div style={{ fontSize: '36px', filter: 'drop-shadow(0 0 12px rgba(212,175,55,0.6))' }}>
            {achievementPopup.category === 'Wealth' ? '💰' :
             achievementPopup.category === 'Trading' ? '📈' :
             achievementPopup.category === 'Market' ? '🏛️' : '🏆'}
          </div>
          <div>
            <div style={{ fontSize: '11px', color: 'var(--gold-primary)', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '2px' }}>
              Achievement Unlocked
            </div>
            <div style={{ fontSize: '18px', fontWeight: 700, color: 'var(--gold-light)', marginTop: '4px' }}>
              {achievementPopup.name}
            </div>
            <div style={{ fontSize: '12px', color: 'rgba(245, 230, 184, 0.7)', marginTop: '4px' }}>
              {achievementPopup.description}
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
