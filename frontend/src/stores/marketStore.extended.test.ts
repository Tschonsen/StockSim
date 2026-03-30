import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { useMarketStore } from './marketStore';
import { GameSpeed, StockData, MarketUpdate } from '@/types/market';

// Provide window global for store actions that dispatch events
const mockWindow = { dispatchEvent: vi.fn() };
vi.stubGlobal('window', mockWindow);
vi.stubGlobal('Event', class Event { constructor(public type: string) {} });

// Reset store between tests
beforeEach(() => {
  mockWindow.dispatchEvent.mockClear();
  useMarketStore.setState({
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
    bigMoveSymbol: null,
    bigMoveDirection: null,
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
    beginnerMode: false,
    scenarioProgress: null,
    scenarioResult: null,
    isBankrupt: false,
    stockFundamentals: null,
    economicData: null,
    earningsCalendar: null,
    smaStatus: 'Clear',
    smaData: null,
    smaNotifications: [],
    shortSqueezeWarning: null,
    tenderOffer: null,
    activeArcs: [],
    vix: 18.0,
    fearGreed: 50,
    marketPhase: 'Neutral',
  });
  vi.useFakeTimers();
});

afterEach(() => {
  vi.useRealTimers();
});

const makeStock = (symbol: string, price: number): StockData => ({
  symbol,
  name: `${symbol} Corp`,
  sector: 'Technology',
  price,
  change: 0,
  changePercent: 0,
  bid: price - 0.1,
  ask: price + 0.1,
  volume: 500000,
  marketCap: price * 1_000_000,
  traits: [],
});

describe('MarketStore Extended', () => {
  describe('updatePrices', () => {
    it('should update stock data, priceFlash, gameTime, and tickCount', () => {
      const stock = makeStock('AAPL', 100);
      useMarketStore.getState().setStocks([stock]);

      const update: MarketUpdate = {
        prices: [{
          symbol: 'AAPL', price: 105, bid: 104.9, ask: 105.1,
          change: 5, changePercent: 5.0, volume: 800000,
          dayHigh: 106, dayLow: 99,
        }],
        gameTime: '2027-03-15T14:30:00',
        tick: 42,
        isMarketOpen: true,
      };

      useMarketStore.getState().updatePrices(update);
      const state = useMarketStore.getState();

      expect(state.stocks.get('AAPL')?.price).toBe(105);
      expect(state.stocks.get('AAPL')?.volume).toBe(800000);
      expect(state.gameTime).toBe('2027-03-15T14:30:00');
      expect(state.tickCount).toBe(42);
      expect(state.isMarketOpen).toBe(true);
    });

    it('should set priceFlash to "up" when price increases', () => {
      useMarketStore.getState().setStocks([makeStock('AAPL', 100)]);

      useMarketStore.getState().updatePrices({
        prices: [{ symbol: 'AAPL', price: 105, bid: 104.9, ask: 105.1, change: 5, changePercent: 5, volume: 1000, dayHigh: 105, dayLow: 100 }],
        gameTime: '', tick: 1, isMarketOpen: true,
      });

      expect(useMarketStore.getState().priceFlash.get('AAPL')).toBe('up');
    });

    it('should set priceFlash to "down" when price decreases', () => {
      useMarketStore.getState().setStocks([makeStock('AAPL', 100)]);

      useMarketStore.getState().updatePrices({
        prices: [{ symbol: 'AAPL', price: 95, bid: 94.9, ask: 95.1, change: -5, changePercent: -5, volume: 1000, dayHigh: 100, dayLow: 95 }],
        gameTime: '', tick: 1, isMarketOpen: true,
      });

      expect(useMarketStore.getState().priceFlash.get('AAPL')).toBe('down');
    });

    it('should not set priceFlash when price is unchanged', () => {
      useMarketStore.getState().setStocks([makeStock('AAPL', 100)]);

      useMarketStore.getState().updatePrices({
        prices: [{ symbol: 'AAPL', price: 100, bid: 99.9, ask: 100.1, change: 0, changePercent: 0, volume: 1000, dayHigh: 100, dayLow: 100 }],
        gameTime: '', tick: 1, isMarketOpen: true,
      });

      expect(useMarketStore.getState().priceFlash.has('AAPL')).toBe(false);
    });

    it('should update stockList array from the map', () => {
      useMarketStore.getState().setStocks([makeStock('AAPL', 100), makeStock('GOOG', 200)]);

      useMarketStore.getState().updatePrices({
        prices: [
          { symbol: 'AAPL', price: 110, bid: 109.9, ask: 110.1, change: 10, changePercent: 10, volume: 1000, dayHigh: 110, dayLow: 100 },
        ],
        gameTime: '', tick: 1, isMarketOpen: true,
      });

      const list = useMarketStore.getState().stockList;
      expect(list.length).toBe(2);
      const appl = list.find(s => s.symbol === 'AAPL');
      expect(appl?.price).toBe(110);
    });
  });

  describe('setPortfolio', () => {
    it('should set portfolio data correctly', () => {
      useMarketStore.getState().setPortfolio({
        cash: 30000,
        portfolioValue: 20000,
        totalEquity: 50000,
        realizedPnL: 500,
        totalCommissions: 29.70,
        tradeCount: 6,
        positions: [
          { symbol: 'AAPL', shares: 50, averageCost: 100, marketValue: 5500, unrealizedPnL: 500, unrealizedPnLPercent: 10 },
          { symbol: 'GOOG', shares: 20, averageCost: 200, marketValue: 4200, unrealizedPnL: 200, unrealizedPnLPercent: 5 },
        ],
      });

      const portfolio = useMarketStore.getState().portfolio;
      expect(portfolio).not.toBeNull();
      expect(portfolio!.cash).toBe(30000);
      expect(portfolio!.totalEquity).toBe(50000);
      expect(portfolio!.realizedPnL).toBe(500);
      expect(portfolio!.totalCommissions).toBe(29.70);
      expect(portfolio!.tradeCount).toBe(6);
      expect(portfolio!.positions).toHaveLength(2);
      expect(portfolio!.positions[0].symbol).toBe('AAPL');
      expect(portfolio!.positions[1].shares).toBe(20);
    });

    it('should overwrite previous portfolio data', () => {
      useMarketStore.getState().setPortfolio({
        cash: 50000, portfolioValue: 0, totalEquity: 50000,
        realizedPnL: 0, totalCommissions: 0, tradeCount: 0, positions: [],
      });
      useMarketStore.getState().setPortfolio({
        cash: 40000, portfolioValue: 10000, totalEquity: 50000,
        realizedPnL: 100, totalCommissions: 4.95, tradeCount: 1,
        positions: [{ symbol: 'AAPL', shares: 10, averageCost: 100, marketValue: 1000, unrealizedPnL: 0, unrealizedPnLPercent: 0 }],
      });

      const portfolio = useMarketStore.getState().portfolio;
      expect(portfolio!.cash).toBe(40000);
      expect(portfolio!.tradeCount).toBe(1);
      expect(portfolio!.positions).toHaveLength(1);
    });
  });

  describe('selectStock', () => {
    it('should set selectedSymbol and showStockDetail', () => {
      useMarketStore.getState().selectStock('GOOG');

      const state = useMarketStore.getState();
      expect(state.selectedSymbol).toBe('GOOG');
      expect(state.showStockDetail).toBe(true);
    });

    it('should clear selection with null', () => {
      useMarketStore.getState().selectStock('GOOG');
      useMarketStore.getState().selectStock(null);

      const state = useMarketStore.getState();
      expect(state.selectedSymbol).toBeNull();
      expect(state.showStockDetail).toBe(false);
    });

    it('should switch selection between stocks', () => {
      useMarketStore.getState().selectStock('AAPL');
      expect(useMarketStore.getState().selectedSymbol).toBe('AAPL');

      useMarketStore.getState().selectStock('GOOG');
      expect(useMarketStore.getState().selectedSymbol).toBe('GOOG');
      expect(useMarketStore.getState().showStockDetail).toBe(true);
    });
  });

  describe('resetGameState', () => {
    it('should reset all state to defaults', () => {
      // Set up some state first
      useMarketStore.getState().setStocks([makeStock('AAPL', 150)]);
      useMarketStore.getState().selectStock('AAPL');
      useMarketStore.getState().setPortfolio({
        cash: 40000, portfolioValue: 10000, totalEquity: 50000,
        realizedPnL: 100, totalCommissions: 9.90, tradeCount: 2,
        positions: [{ symbol: 'AAPL', shares: 10, averageCost: 150, marketValue: 1500, unrealizedPnL: 0, unrealizedPnLPercent: 0 }],
      });
      useMarketStore.getState().setBankrupt(true);

      // Now reset
      useMarketStore.getState().resetGameState();

      const state = useMarketStore.getState();
      expect(state.stocks.size).toBe(0);
      expect(state.stockList).toHaveLength(0);
      expect(state.portfolio).toBeNull();
      expect(state.orders).toHaveLength(0);
      expect(state.newsItems).toHaveLength(0);
      expect(state.selectedSymbol).toBeNull();
      expect(state.showStockDetail).toBe(false);
      expect(state.scenarioProgress).toBeNull();
      expect(state.scenarioResult).toBeNull();
      expect(state.isBankrupt).toBe(false);
      expect(state.analyticsData).toBeNull();
      expect(state.stockFundamentals).toBeNull();
      expect(state.economicData).toBeNull();
      expect(state.earningsCalendar).toBeNull();
      expect(state.tradeJournal).toHaveLength(0);
      expect(state.achievementPopup).toBeNull();
      expect(state.vix).toBe(18.0);
      expect(state.fearGreed).toBe(50);
      expect(state.marketPhase).toBe('Neutral');
    });

    it('should not reset connection or speed state', () => {
      useMarketStore.getState().setConnected(true);
      useMarketStore.getState().setSpeed(GameSpeed.Fast);

      useMarketStore.getState().resetGameState();

      // These should remain (resetGameState does not reset them)
      expect(useMarketStore.getState().isConnected).toBe(true);
      expect(useMarketStore.getState().speed).toBe(GameSpeed.Fast);
    });
  });

  describe('bigMoveSymbol', () => {
    it('should set bigMoveSymbol on >3% single-tick change for selected stock', () => {
      useMarketStore.getState().setStocks([makeStock('AAPL', 100)]);
      useMarketStore.getState().selectStock('AAPL');

      useMarketStore.getState().updatePrices({
        prices: [{ symbol: 'AAPL', price: 104, bid: 103.9, ask: 104.1, change: 4, changePercent: 4, volume: 1000, dayHigh: 104, dayLow: 100 }],
        gameTime: '', tick: 1, isMarketOpen: true,
      });

      expect(useMarketStore.getState().bigMoveSymbol).toBe('AAPL');
      expect(useMarketStore.getState().bigMoveDirection).toBe('up');
    });

    it('should detect big downward move', () => {
      useMarketStore.getState().setStocks([makeStock('AAPL', 100)]);
      useMarketStore.getState().selectStock('AAPL');

      useMarketStore.getState().updatePrices({
        prices: [{ symbol: 'AAPL', price: 96, bid: 95.9, ask: 96.1, change: -4, changePercent: -4, volume: 1000, dayHigh: 100, dayLow: 96 }],
        gameTime: '', tick: 1, isMarketOpen: true,
      });

      expect(useMarketStore.getState().bigMoveSymbol).toBe('AAPL');
      expect(useMarketStore.getState().bigMoveDirection).toBe('down');
    });

    it('should not trigger bigMove for non-selected stock', () => {
      useMarketStore.getState().setStocks([makeStock('AAPL', 100), makeStock('GOOG', 200)]);
      useMarketStore.getState().selectStock('GOOG');

      useMarketStore.getState().updatePrices({
        prices: [{ symbol: 'AAPL', price: 150, bid: 149.9, ask: 150.1, change: 50, changePercent: 50, volume: 1000, dayHigh: 150, dayLow: 100 }],
        gameTime: '', tick: 1, isMarketOpen: true,
      });

      expect(useMarketStore.getState().bigMoveSymbol).toBeNull();
    });

    it('should auto-clear bigMoveSymbol after 500ms', () => {
      useMarketStore.getState().setStocks([makeStock('AAPL', 100)]);
      useMarketStore.getState().selectStock('AAPL');

      useMarketStore.getState().updatePrices({
        prices: [{ symbol: 'AAPL', price: 104, bid: 103.9, ask: 104.1, change: 4, changePercent: 4, volume: 1000, dayHigh: 104, dayLow: 100 }],
        gameTime: '', tick: 1, isMarketOpen: true,
      });

      expect(useMarketStore.getState().bigMoveSymbol).toBe('AAPL');

      vi.advanceTimersByTime(500);

      expect(useMarketStore.getState().bigMoveSymbol).toBeNull();
      expect(useMarketStore.getState().bigMoveDirection).toBeNull();
    });

    it('should not set bigMove for small changes (<3%)', () => {
      useMarketStore.getState().setStocks([makeStock('AAPL', 100)]);
      useMarketStore.getState().selectStock('AAPL');

      useMarketStore.getState().updatePrices({
        prices: [{ symbol: 'AAPL', price: 102, bid: 101.9, ask: 102.1, change: 2, changePercent: 2, volume: 1000, dayHigh: 102, dayLow: 100 }],
        gameTime: '', tick: 1, isMarketOpen: true,
      });

      expect(useMarketStore.getState().bigMoveSymbol).toBeNull();
    });
  });
});
