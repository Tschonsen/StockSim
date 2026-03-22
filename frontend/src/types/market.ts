/**
 * Core market data types matching the backend models.
 * See Bible sections 11.3.1-11.3.6 for stock data specifications.
 */

export interface StockData {
  symbol: string;
  name: string;
  sector: string;
  price: number;
  previousClose?: number;
  change: number;
  changePercent: number;
  bid: number;
  ask: number;
  volume: number;
  marketCap: number;
  dayHigh?: number;
  dayLow?: number;
  traits: string[];
}

export interface MarketUpdate {
  prices: PriceUpdate[];
  gameTime: string;
  tick: number;
  isMarketOpen: boolean;
}

export interface PriceUpdate {
  Symbol: string;
  price: number;
  bid: number;
  ask: number;
  change: number;
  changePercent: number;
  volume: number;
}

export interface MarketSnapshot {
  stocks: StockData[];
  gameTime: string;
  speed: number;
  isMarketOpen: boolean;
}

export enum GameSpeed {
  Paused = 0,
  Normal = 1,
  Fast = 2,
  VeryFast = 5,
  Maximum = 10,
}

export type ActiveTab =
  | 'dashboard'
  | 'portfolio'
  | 'market'
  | 'orders'
  | 'news'
  | 'analytics';
