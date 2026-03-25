import { create } from 'zustand';
import { StockData, GameSpeed, ActiveTab, MarketUpdate, PortfolioData, OrderData, NewsEvent, IndicatorData, OrderbookData, AnalyticsResponse, Achievement, TradeJournalEntry, ScenarioResultData, StockFundamentals, EconomicDataResponse, EarningsCalendarResponse, RegulatoryStatus, SMAStatusResponse, SMANotification } from '@/types/market';
import { createLogger } from '@/services/logger';

const log = createLogger('MarketStore');

interface MarketState {
  // Stock data
  stocks: Map<string, StockData>;
  stockList: StockData[]; // sorted array for rendering

  // Game state
  gameTime: string;
  speed: GameSpeed;
  isMarketOpen: boolean;
  tickCount: number;

  // Chart data
  ohlcvData: Map<string, { time: number; open: number; high: number; low: number; close: number; volume: number }[]>;

  // UI state
  activeTab: ActiveTab;
  selectedSymbol: string | null;
  showStockDetail: boolean;
  watchlist: string[];
  watchlists: Record<string, string[]>;
  activeWatchlistName: string;

  // Connection
  isConnected: boolean;
  isGameActive: boolean;

  // Price flash tracking (which prices just changed direction)
  priceFlash: Map<string, 'up' | 'down'>;

  // Portfolio & Orders (Bible 4.1, 6.1)
  portfolio: PortfolioData | null;
  orders: OrderData[];
  lastOrderResult: { success: boolean; error: string | null; order: OrderData | null } | null;

  // News (Bible 8, 13)
  newsItems: NewsEvent[];

  // Indicators (Bible 12.2.4)
  indicatorData: Map<string, IndicatorData>;

  // Orderbook (Bible 12.3)
  orderbookData: OrderbookData | null;

  // Analytics (enhanced)
  analyticsData: AnalyticsResponse | null;

  // Achievements
  achievements: Achievement[];
  achievementPopup: Achievement | null;

  // Trade Journal
  tradeJournal: TradeJournalEntry[];

  // Scenario & Bankruptcy
  scenarioResult: ScenarioResultData | null;
  isBankrupt: boolean;
  stockFundamentals: StockFundamentals | null;

  // Economic & Earnings
  economicData: EconomicDataResponse | null;
  earningsCalendar: EarningsCalendarResponse | null;

  // SMA (StockSim Market Authority) - Bible 9
  smaStatus: RegulatoryStatus;
  smaData: SMAStatusResponse | null;
  smaNotifications: SMANotification[];

  // Actions
  setStocks: (stocks: StockData[]) => void;
  setOHLCVData: (symbol: string, candles: { time: number; open: number; high: number; low: number; close: number; volume: number }[]) => void;
  updatePrices: (update: MarketUpdate) => void;
  setSpeed: (speed: GameSpeed) => void;
  setActiveTab: (tab: ActiveTab) => void;
  selectStock: (symbol: string | null) => void;
  addToWatchlist: (symbol: string) => void;
  removeFromWatchlist: (symbol: string) => void;
  createWatchlist: (name: string) => void;
  deleteWatchlist: (name: string) => void;
  setActiveWatchlist: (name: string) => void;
  setConnected: (connected: boolean) => void;
  setGameActive: (active: boolean) => void;
  setPortfolio: (portfolio: PortfolioData) => void;
  setOrders: (orders: OrderData[]) => void;
  setOrderResult: (result: { success: boolean; error: string | null; order: OrderData | null } | null) => void;
  addNewsEvents: (events: NewsEvent[]) => void;
  setIndicatorData: (symbol: string, data: IndicatorData) => void;
  setOrderbookData: (data: OrderbookData) => void;
  setAnalyticsData: (data: AnalyticsResponse) => void;
  setAchievements: (achievements: Achievement[]) => void;
  showAchievementPopup: (achievement: Achievement) => void;
  dismissAchievementPopup: () => void;
  setTradeJournal: (trades: TradeJournalEntry[]) => void;
  setScenarioResult: (result: ScenarioResultData | null) => void;
  setBankrupt: (bankrupt: boolean) => void;
  setStockFundamentals: (data: StockFundamentals | null) => void;
  setEconomicData: (data: EconomicDataResponse) => void;
  setEarningsCalendar: (data: EarningsCalendarResponse) => void;
  setSMAStatus: (status: RegulatoryStatus) => void;
  setSMAData: (data: SMAStatusResponse) => void;
  addSMANotifications: (notifications: SMANotification[]) => void;
  dismissSMANotification: (index: number) => void;
}

export const useMarketStore = create<MarketState>((set, get) => ({
  // Initial state
  stocks: new Map(),
  stockList: [],
  gameTime: '',
  speed: GameSpeed.Paused,
  isMarketOpen: false,
  tickCount: 0,
  ohlcvData: new Map(),
  activeTab: 'dashboard',
  selectedSymbol: null,
  showStockDetail: false,
  watchlist: [],
  watchlists: { Main: [] },
  activeWatchlistName: 'Main',
  isConnected: false,
  isGameActive: false,
  priceFlash: new Map(),
  portfolio: null,
  orders: [],
  lastOrderResult: null,
  newsItems: [],
  indicatorData: new Map(),
  orderbookData: null,
  analyticsData: null,
  achievements: [],
  achievementPopup: null,
  tradeJournal: [],
  scenarioResult: null,
  isBankrupt: false,
  stockFundamentals: null,
  economicData: null,
  earningsCalendar: null,
  smaStatus: 'Clear',
  smaData: null,
  smaNotifications: [],

  setStocks: (stocks: StockData[]) => {
    const map = new Map<string, StockData>();
    stocks.forEach((s) => map.set(s.symbol, s));

    log.info('Market snapshot loaded', { count: stocks.length });

    set({
      stocks: map,
      stockList: stocks,
      isGameActive: true,
    });
  },

  updatePrices: (update: MarketUpdate) => {
    const { stocks } = get();
    const updatedMap = new Map(stocks);
    const flash = new Map<string, 'up' | 'down'>();

    for (const priceUpdate of update.prices) {
      const existing = updatedMap.get(priceUpdate.symbol);
      if (existing) {
        // Track price direction for flash animation
        if (priceUpdate.price > existing.price) flash.set(priceUpdate.symbol, 'up');
        else if (priceUpdate.price < existing.price) flash.set(priceUpdate.symbol, 'down');

        updatedMap.set(priceUpdate.symbol, {
          ...existing,
          price: priceUpdate.price,
          bid: priceUpdate.bid,
          ask: priceUpdate.ask,
          change: priceUpdate.change,
          changePercent: priceUpdate.changePercent,
          volume: priceUpdate.volume,
          dayHigh: priceUpdate.dayHigh,
          dayLow: priceUpdate.dayLow,
        });
      }
    }

    set({
      stocks: updatedMap,
      priceFlash: flash,
      stockList: Array.from(updatedMap.values()),
      gameTime: update.gameTime,
      tickCount: update.tick,
      isMarketOpen: update.isMarketOpen,
      ...(update.smaStatus ? { smaStatus: update.smaStatus as RegulatoryStatus } : {}),
    });
  },

  setSpeed: (speed: GameSpeed) => {
    log.info('Speed changed', { speed });
    set({ speed });
  },

  setActiveTab: (tab: ActiveTab) => {
    log.debug('Tab changed', { tab });
    set({ activeTab: tab });
  },

  selectStock: (symbol: string | null) => {
    log.debug('Stock selected', { symbol });
    set({ selectedSymbol: symbol, showStockDetail: symbol !== null });
  },

  setOHLCVData: (symbol, candles) => {
    const { ohlcvData } = get();
    const updated = new Map(ohlcvData);
    updated.set(symbol, candles);
    log.debug('OHLCV data received', { symbol, candles: candles.length });
    set({ ohlcvData: updated });
  },

  addToWatchlist: (symbol: string) => {
    const { watchlist, watchlists, activeWatchlistName } = get();
    if (!watchlist.includes(symbol)) {
      const newList = [...watchlist, symbol];
      const newLists = { ...watchlists, [activeWatchlistName]: newList };
      log.info('Added to watchlist', { symbol, list: activeWatchlistName });
      set({ watchlist: newList, watchlists: newLists });
    }
  },

  removeFromWatchlist: (symbol: string) => {
    const { watchlist, watchlists, activeWatchlistName } = get();
    const newList = watchlist.filter((s) => s !== symbol);
    const newLists = { ...watchlists, [activeWatchlistName]: newList };
    log.info('Removed from watchlist', { symbol });
    set({ watchlist: newList, watchlists: newLists });
  },

  createWatchlist: (name: string) => {
    const { watchlists } = get();
    if (Object.keys(watchlists).length >= 5) return; // Max 5
    if (watchlists[name]) return; // Already exists
    set({ watchlists: { ...watchlists, [name]: [] }, activeWatchlistName: name, watchlist: [] });
  },

  deleteWatchlist: (name: string) => {
    if (name === 'Main') return; // Can't delete Main
    const { watchlists } = get();
    const newLists = { ...watchlists };
    delete newLists[name];
    const firstList = Object.keys(newLists)[0] || 'Main';
    set({ watchlists: newLists, activeWatchlistName: firstList, watchlist: newLists[firstList] || [] });
  },

  setActiveWatchlist: (name: string) => {
    const { watchlists } = get();
    set({ activeWatchlistName: name, watchlist: watchlists[name] || [] });
  },

  setConnected: (connected: boolean) => {
    log.info('Connection state changed', { connected });
    set({ isConnected: connected });
  },

  setGameActive: (active: boolean) => {
    set({ isGameActive: active });
  },

  setPortfolio: (portfolio: PortfolioData) => {
    log.debug('Portfolio updated', { cash: portfolio.cash, equity: portfolio.totalEquity, positions: portfolio.positions.length });
    set({ portfolio });
  },

  setOrders: (orders: OrderData[]) => {
    log.debug('Orders updated', { count: orders.length });
    set({ orders });
  },

  setOrderbookData: (data: OrderbookData) => {
    set({ orderbookData: data });
  },

  setIndicatorData: (symbol: string, data: IndicatorData) => {
    const { indicatorData } = get();
    const updated = new Map(indicatorData);
    updated.set(symbol, data);
    log.debug('Indicator data received', { symbol, keys: Object.keys(data) });
    set({ indicatorData: updated });
  },

  addNewsEvents: (events: NewsEvent[]) => {
    const { newsItems } = get();
    const updated = [...events, ...newsItems].slice(0, 100); // Keep last 100
    set({ newsItems: updated });
  },

  setOrderResult: (result) => {
    if (result) {
      if (result.success) {
        log.info('Order executed', { order: result.order });
      } else {
        log.warn('Order failed', { error: result.error });
      }
    }
    set({ lastOrderResult: result });
  },

  setAnalyticsData: (data: AnalyticsResponse) => {
    log.debug('Analytics data received');
    set({ analyticsData: data });
  },

  setAchievements: (achievements: Achievement[]) => {
    log.debug('Achievements loaded', { count: achievements.length, unlocked: achievements.filter(a => a.unlocked).length });
    set({ achievements });
  },

  showAchievementPopup: (achievement: Achievement) => {
    log.info('Achievement unlocked!', { name: achievement.name });
    set({ achievementPopup: achievement });
    // Auto-dismiss after 5 seconds
    setTimeout(() => {
      set((state) => state.achievementPopup?.id === achievement.id ? { achievementPopup: null } : {});
    }, 5000);
  },

  dismissAchievementPopup: () => {
    set({ achievementPopup: null });
  },

  setTradeJournal: (trades: TradeJournalEntry[]) => {
    log.debug('Trade journal loaded', { count: trades.length });
    set({ tradeJournal: trades });
  },

  setScenarioResult: (result: ScenarioResultData | null) => {
    if (result) log.info('Scenario completed', { won: result.won, name: result.scenarioName });
    set({ scenarioResult: result });
  },

  setBankrupt: (bankrupt: boolean) => {
    if (bankrupt) log.warn('Player is bankrupt!');
    set({ isBankrupt: bankrupt });
  },

  setStockFundamentals: (data: StockFundamentals | null) => {
    set({ stockFundamentals: data });
  },

  setEconomicData: (data: EconomicDataResponse) => {
    set({ economicData: data });
  },

  setEarningsCalendar: (data: EarningsCalendarResponse) => {
    set({ earningsCalendar: data });
  },

  setSMAStatus: (status: RegulatoryStatus) => {
    set({ smaStatus: status });
  },

  setSMAData: (data: SMAStatusResponse) => {
    set({ smaData: data, smaStatus: data.status });
  },

  addSMANotifications: (notifications: SMANotification[]) => {
    set((state) => ({
      smaNotifications: [...state.smaNotifications, ...notifications],
    }));
  },

  dismissSMANotification: (index: number) => {
    set((state) => ({
      smaNotifications: state.smaNotifications.filter((_, i) => i !== index),
    }));
  },
}));
