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

// --- Order & Portfolio Types (Bible 4.1-4.3) ---

export type OrderSide = 'Buy' | 'Sell' | 'Short' | 'Cover';
export type OrderType = 'Market' | 'Limit' | 'Stop' | 'StopLimit' | 'TrailingStop';
export type OrderStatus = 'Pending' | 'Open' | 'Filled' | 'PartiallyFilled' | 'Cancelled' | 'Rejected' | 'Expired';
export type TimeInForce = 'GTC' | 'Day';

export interface OrderData {
  id: number;
  symbol: string;
  side: OrderSide;
  type: OrderType;
  status: OrderStatus;
  quantity: number;
  filledQuantity: number;
  limitPrice: number | null;
  fillPrice: number | null;
  commission: number;
  placedAt: string;
  filledAt: string | null;
  rejectReason: string | null;
}

export interface PositionData {
  symbol: string;
  shares: number;
  averageCost: number;
  marketValue: number;
  unrealizedPnL: number;
  unrealizedPnLPercent: number;
}

export interface PortfolioData {
  cash: number;
  portfolioValue: number;
  totalEquity: number;
  realizedPnL: number;
  totalCommissions: number;
  tradeCount: number;
  positions: PositionData[];
}

export interface OrderResultData {
  success: boolean;
  error: string | null;
  order: OrderData | null;
}

// --- News/Events (Bible 8.1) ---

export interface IndicatorLine {
  time: number;
  value: number;
}

export interface IndicatorData {
  sma20?: IndicatorLine[];
  sma50?: IndicatorLine[];
  sma200?: IndicatorLine[];
  ema12?: IndicatorLine[];
  bollingerUpper?: IndicatorLine[];
  bollingerMiddle?: IndicatorLine[];
  bollingerLower?: IndicatorLine[];
  rsi?: IndicatorLine[];
  macdLine?: IndicatorLine[];
  macdSignal?: IndicatorLine[];
  macdHistogram?: IndicatorLine[];
}

export interface OrderbookLevel {
  price: number;
  quantity: number;
}

export interface OrderbookData {
  symbol: string;
  bids: OrderbookLevel[];
  asks: OrderbookLevel[];
  bestBid: number;
  bestAsk: number;
  spread: number;
  spreadPercent: number;
}

export interface NewsEvent {
  id: number;
  type: 'Macro' | 'Sector' | 'Company';
  severity: 'Minor' | 'Moderate' | 'Major';
  sentiment: number;
  headline: string;
  affectedSymbols: string[];
  affectedSectors: string[];
  priceEffect: number;
  timestamp: string;
}
