import { create } from 'zustand';
import { StockData, GameSpeed, ActiveTab, MarketUpdate, PortfolioData, OrderData, NewsEvent, IndicatorData, OrderbookData } from '@/types/market';
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

  // Connection
  isConnected: boolean;
  isGameActive: boolean;

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

  // Actions
  setStocks: (stocks: StockData[]) => void;
  setOHLCVData: (symbol: string, candles: { time: number; open: number; high: number; low: number; close: number; volume: number }[]) => void;
  updatePrices: (update: MarketUpdate) => void;
  setSpeed: (speed: GameSpeed) => void;
  setActiveTab: (tab: ActiveTab) => void;
  selectStock: (symbol: string | null) => void;
  addToWatchlist: (symbol: string) => void;
  removeFromWatchlist: (symbol: string) => void;
  setConnected: (connected: boolean) => void;
  setGameActive: (active: boolean) => void;
  setPortfolio: (portfolio: PortfolioData) => void;
  setOrders: (orders: OrderData[]) => void;
  setOrderResult: (result: { success: boolean; error: string | null; order: OrderData | null } | null) => void;
  addNewsEvents: (events: NewsEvent[]) => void;
  setIndicatorData: (symbol: string, data: IndicatorData) => void;
  setOrderbookData: (data: OrderbookData) => void;
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
  isConnected: false,
  isGameActive: false,
  portfolio: null,
  orders: [],
  lastOrderResult: null,
  newsItems: [],
  indicatorData: new Map(),
  orderbookData: null,

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

    for (const priceUpdate of update.prices) {
      const existing = updatedMap.get(priceUpdate.Symbol);
      if (existing) {
        updatedMap.set(priceUpdate.Symbol, {
          ...existing,
          price: priceUpdate.price,
          bid: priceUpdate.bid,
          ask: priceUpdate.ask,
          change: priceUpdate.change,
          changePercent: priceUpdate.changePercent,
          volume: priceUpdate.volume,
        });
      }
    }

    set({
      stocks: updatedMap,
      stockList: Array.from(updatedMap.values()),
      gameTime: update.gameTime,
      tickCount: update.tick,
      isMarketOpen: update.isMarketOpen,
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
    const { watchlist } = get();
    if (!watchlist.includes(symbol)) {
      log.info('Added to watchlist', { symbol });
      set({ watchlist: [...watchlist, symbol] });
    }
  },

  removeFromWatchlist: (symbol: string) => {
    const { watchlist } = get();
    log.info('Removed from watchlist', { symbol });
    set({ watchlist: watchlist.filter((s) => s !== symbol) });
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
}));
