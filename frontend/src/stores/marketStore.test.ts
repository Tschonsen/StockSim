import { describe, it, expect, beforeEach } from 'vitest';
import { useMarketStore } from './marketStore';
import { GameSpeed, StockData, NewsEvent } from '@/types/market';

// Reset store between tests
beforeEach(() => {
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
    isConnected: false,
    isGameActive: false,
    portfolio: null,
    orders: [],
    lastOrderResult: null,
    newsItems: [],
  });
});

const mockStock: StockData = {
  symbol: 'AAPL',
  name: 'Apple Corp',
  sector: 'Technology',
  price: 150,
  change: 2.5,
  changePercent: 1.69,
  bid: 149.90,
  ask: 150.10,
  volume: 1000000,
  marketCap: 15000000000,
  traits: ['Blue Chip'],
};

describe('MarketStore', () => {
  describe('setStocks', () => {
    it('should populate stocks map and list', () => {
      useMarketStore.getState().setStocks([mockStock]);

      const state = useMarketStore.getState();
      expect(state.stocks.size).toBe(1);
      expect(state.stocks.get('AAPL')).toBeDefined();
      expect(state.stockList).toHaveLength(1);
      expect(state.isGameActive).toBe(true);
    });
  });

  describe('updatePrices', () => {
    it('should update existing stock prices', () => {
      useMarketStore.getState().setStocks([mockStock]);

      useMarketStore.getState().updatePrices({
        prices: [{ Symbol: 'AAPL', price: 155, bid: 154.9, ask: 155.1, change: 5, changePercent: 3.33, volume: 2000000 }],
        gameTime: '2027-01-05T10:00:00',
        tick: 1,
        isMarketOpen: true,
      });

      const stock = useMarketStore.getState().stocks.get('AAPL');
      expect(stock?.price).toBe(155);
      expect(stock?.volume).toBe(2000000);
      expect(useMarketStore.getState().isMarketOpen).toBe(true);
    });
  });

  describe('setSpeed', () => {
    it('should update speed', () => {
      useMarketStore.getState().setSpeed(GameSpeed.Fast);

      expect(useMarketStore.getState().speed).toBe(GameSpeed.Fast);
    });
  });

  describe('selectStock', () => {
    it('should set selected symbol and show detail', () => {
      useMarketStore.getState().selectStock('AAPL');

      expect(useMarketStore.getState().selectedSymbol).toBe('AAPL');
      expect(useMarketStore.getState().showStockDetail).toBe(true);
    });

    it('should clear selection with null', () => {
      useMarketStore.getState().selectStock('AAPL');
      useMarketStore.getState().selectStock(null);

      expect(useMarketStore.getState().selectedSymbol).toBeNull();
      expect(useMarketStore.getState().showStockDetail).toBe(false);
    });
  });

  describe('watchlist', () => {
    it('should add to watchlist', () => {
      useMarketStore.getState().addToWatchlist('AAPL');

      expect(useMarketStore.getState().watchlist).toContain('AAPL');
    });

    it('should not add duplicates', () => {
      useMarketStore.getState().addToWatchlist('AAPL');
      useMarketStore.getState().addToWatchlist('AAPL');

      expect(useMarketStore.getState().watchlist).toHaveLength(1);
    });

    it('should remove from watchlist', () => {
      useMarketStore.getState().addToWatchlist('AAPL');
      useMarketStore.getState().addToWatchlist('GOOG');
      useMarketStore.getState().removeFromWatchlist('AAPL');

      expect(useMarketStore.getState().watchlist).not.toContain('AAPL');
      expect(useMarketStore.getState().watchlist).toContain('GOOG');
    });
  });

  describe('setActiveTab', () => {
    it('should switch tabs', () => {
      useMarketStore.getState().setActiveTab('portfolio');

      expect(useMarketStore.getState().activeTab).toBe('portfolio');
    });
  });

  describe('portfolio', () => {
    it('should store portfolio data', () => {
      useMarketStore.getState().setPortfolio({
        cash: 45000,
        portfolioValue: 5000,
        totalEquity: 50000,
        realizedPnL: 100,
        totalCommissions: 9.90,
        tradeCount: 2,
        positions: [{ symbol: 'AAPL', shares: 10, averageCost: 150, marketValue: 1550, unrealizedPnL: 50, unrealizedPnLPercent: 3.33 }],
      });

      const portfolio = useMarketStore.getState().portfolio;
      expect(portfolio).not.toBeNull();
      expect(portfolio!.cash).toBe(45000);
      expect(portfolio!.positions).toHaveLength(1);
    });
  });

  describe('orders', () => {
    it('should store orders', () => {
      useMarketStore.getState().setOrders([
        { id: 1, symbol: 'AAPL', side: 'Buy', type: 'Market', status: 'Filled', quantity: 10, filledQuantity: 10, limitPrice: null, fillPrice: 150.10, commission: 4.95, placedAt: '', filledAt: '', rejectReason: null },
      ]);

      expect(useMarketStore.getState().orders).toHaveLength(1);
      expect(useMarketStore.getState().orders[0].symbol).toBe('AAPL');
    });
  });

  describe('orderResult', () => {
    it('should store and clear order result', () => {
      useMarketStore.getState().setOrderResult({ success: true, error: null, order: null });

      expect(useMarketStore.getState().lastOrderResult).not.toBeNull();
      expect(useMarketStore.getState().lastOrderResult!.success).toBe(true);

      useMarketStore.getState().setOrderResult(null);
      expect(useMarketStore.getState().lastOrderResult).toBeNull();
    });
  });

  describe('news', () => {
    it('should add news events in newest-first order', () => {
      const event1: NewsEvent = { id: 1, type: 'Macro', severity: 'Major', sentiment: -0.3, headline: 'Fed raises rates', affectedSymbols: [], affectedSectors: [], priceEffect: -0.02, timestamp: '' };
      const event2: NewsEvent = { id: 2, type: 'Company', severity: 'Moderate', sentiment: 0.5, headline: 'AAPL beats earnings', affectedSymbols: ['AAPL'], affectedSectors: [], priceEffect: 0.05, timestamp: '' };

      useMarketStore.getState().addNewsEvents([event1]);
      useMarketStore.getState().addNewsEvents([event2]);

      const news = useMarketStore.getState().newsItems;
      expect(news).toHaveLength(2);
      expect(news[0].id).toBe(2); // Newest first
    });

    it('should limit to 100 items', () => {
      const events: NewsEvent[] = Array.from({ length: 110 }, (_, i) => ({
        id: i, type: 'Macro' as const, severity: 'Minor' as const, sentiment: 0, headline: `Event ${i}`, affectedSymbols: [], affectedSectors: [], priceEffect: 0, timestamp: '',
      }));

      useMarketStore.getState().addNewsEvents(events);

      expect(useMarketStore.getState().newsItems.length).toBeLessThanOrEqual(100);
    });
  });

  describe('OHLCV data', () => {
    it('should store candle data per symbol', () => {
      const candles = [{ time: 1000, open: 100, high: 105, low: 99, close: 103, volume: 500 }];
      useMarketStore.getState().setOHLCVData('AAPL', candles);

      const data = useMarketStore.getState().ohlcvData.get('AAPL');
      expect(data).toHaveLength(1);
      expect(data![0].close).toBe(103);
    });
  });

  describe('connection', () => {
    it('should track connection state', () => {
      useMarketStore.getState().setConnected(true);
      expect(useMarketStore.getState().isConnected).toBe(true);

      useMarketStore.getState().setConnected(false);
      expect(useMarketStore.getState().isConnected).toBe(false);
    });
  });
});
