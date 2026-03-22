import { create } from 'zustand';
import { StockData, GameSpeed, ActiveTab, MarketUpdate } from '@/types/market';
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

  // UI state
  activeTab: ActiveTab;
  selectedSymbol: string | null;
  watchlist: string[];

  // Connection
  isConnected: boolean;
  isGameActive: boolean;

  // Actions
  setStocks: (stocks: StockData[]) => void;
  updatePrices: (update: MarketUpdate) => void;
  setSpeed: (speed: GameSpeed) => void;
  setActiveTab: (tab: ActiveTab) => void;
  selectStock: (symbol: string | null) => void;
  addToWatchlist: (symbol: string) => void;
  removeFromWatchlist: (symbol: string) => void;
  setConnected: (connected: boolean) => void;
  setGameActive: (active: boolean) => void;
}

export const useMarketStore = create<MarketState>((set, get) => ({
  // Initial state
  stocks: new Map(),
  stockList: [],
  gameTime: '',
  speed: GameSpeed.Paused,
  isMarketOpen: false,
  tickCount: 0,
  activeTab: 'dashboard',
  selectedSymbol: null,
  watchlist: [],
  isConnected: false,
  isGameActive: false,

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
    set({ selectedSymbol: symbol });
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
}));
