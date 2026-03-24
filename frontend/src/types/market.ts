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
  peRatio?: number;
  dividendYield?: number;
  traits: string[];
}

export interface MarketUpdate {
  prices: PriceUpdate[];
  gameTime: string;
  tick: number;
  isMarketOpen: boolean;
}

export interface PriceUpdate {
  symbol: string;
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
  | 'analytics'
  | 'journal';

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
  // Margin
  marginEnabled?: boolean;
  marginBalance?: number;
  buyingPower?: number;
  marginUsedPercent?: number;
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

// --- Analytics Types ---

export interface PortfolioAnalytics {
  totalEquity: number;
  cash: number;
  portfolioValue: number;
  totalReturn: number;
  totalReturnPercent: number;
  realizedPnL: number;
  unrealizedPnL: number;
  totalCommissions: number;
  totalTrades: number;
  openPositions: number;
  winRate: number;
  winningTrades: number;
  losingTrades: number;
  avgTradeSize: number;
  bestTradePnL: number;
  bestTradeSymbol: string;
  worstTradePnL: number;
  worstTradeSymbol: string;
  avgWin: number;
  avgLoss: number;
  maxDrawdownPercent: number;
  profitFactor: number;
  sharpeRatio: number;
  maxConsecutiveWins: number;
  maxConsecutiveLosses: number;
}

export interface EquitySnapshot {
  time: number;
  equity: number;
  cash: number;
  marketIndex: number;
}

export interface SectorPnL {
  sector: string;
  pnl: number;
}

export interface AnalyticsResponse {
  analytics: PortfolioAnalytics;
  equityHistory: EquitySnapshot[];
  sectorPnL: SectorPnL[];
}

// --- Achievement Types ---

export interface Achievement {
  id: string;
  name: string;
  description: string;
  category: 'Wealth' | 'Trading' | 'Market' | 'Risk';
  unlocked: boolean;
  unlockedAt: string | null;
}

// --- Trade Journal ---

// --- Scenario Types ---

export interface ScenarioInfo {
  id: string;
  name: string;
  description: string;
  difficulty: string;
  startingCash: number;
  timeLimitDays: number | null;
  targetValue: number | null;
}

export interface ScenarioResultData {
  scenarioId: string;
  scenarioName: string;
  won: boolean;
  daysElapsed: number;
  finalPortfolioValue: number;
  totalReturn: number;
  totalReturnPercent: number;
  totalTrades: number;
  winRate: number;
  failReason: string;
}

// --- Stock Fundamentals ---

export interface StockFundamentals {
  symbol: string;
  peRatio: number;
  marketCap: number;
  revenue: number;
  netIncome: number;
  dividendYield: number;
  debtToEquity: number;
  revenueGrowth: number;
  employees: number;
  sharesOutstanding: number;
  insiderOwnership: number;
  institutionalOwnership: number;
  shortInterest: number;
  baseVolatility: number;
  liquidityScore: number;
  fairValue: number;
  dayHigh: number;
  dayLow: number;
  yearHigh: number;
  yearLow: number;
  averageVolume: number;
  float: number;
  floatPercentage: number;
  analystRating: number;
  analystConsensus: string;
  targetPrice: number;
}

// --- Economic Data ---

export interface EconomicIndicators {
  interestRate: number;
  inflationRate: number;
  unemploymentRate: number;
  gdpGrowth: number;
  consumerConfidence: number;
  treasuryYield10Y: number;
  oilPrice: number;
  goldPrice: number;
  manufacturingPMI: number;
}

export interface EconomicDataResponse {
  indicators: EconomicIndicators;
  fearGreedIndex: number;
  marketSentiment: number;
  upcomingEvents: { id: string; name: string; indicator: string; scheduledDate: string; impact: string }[];
}

// --- Earnings Calendar ---

export interface EarningsCalendarResponse {
  upcoming: { symbol: string; reportDate: string; quarter: number; expectedEPS: number }[];
  recent: { symbol: string; reportDate: string; quarter: number; expectedEPS: number; actualEPS: number; beat: boolean; surprisePercent: number; priceImpact: number }[];
}

export interface TradeJournalEntry {
  id: number;
  symbol: string;
  sector: string;
  side: 'Long' | 'Short';
  entryPrice: number;
  exitPrice: number;
  quantity: number;
  pnl: number;
  pnlPercent: number;
  commission: number;
  entryTime: string;
  exitTime: string;
  holdingDays: number;
}
