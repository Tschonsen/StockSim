export interface GlossaryEntry {
  term: string;
  category: string;
  definition: string;
}

export const GLOSSARY: GlossaryEntry[] = [
  // Orders
  { term: 'Market Order', category: 'Orders', definition: 'An order to buy or sell immediately at the current market price. Guaranteed execution, but price may vary slightly (slippage).' },
  { term: 'Limit Order', category: 'Orders', definition: 'An order to buy or sell at a specific price or better. Only executes if the market reaches your price. May never fill.' },
  { term: 'Stop Order', category: 'Orders', definition: 'An order that triggers a market order when the price reaches a specified level. Used to limit losses (stop-loss) or enter positions.' },
  { term: 'Stop-Limit Order', category: 'Orders', definition: 'Combines a stop and a limit order. When the stop price is hit, a limit order is placed instead of a market order.' },
  { term: 'Trailing Stop', category: 'Orders', definition: 'A stop order that automatically adjusts as the price moves in your favor. Locks in profits while giving room for growth.' },
  { term: 'Bracket Order (OCO)', category: 'Orders', definition: 'A take-profit and stop-loss pair. One Cancels Other \u2014 when one fills, the other is automatically cancelled.' },
  { term: 'GTC (Good Till Cancelled)', category: 'Orders', definition: 'Order stays active until you cancel it or it fills. No expiration date.' },
  { term: 'Day Order', category: 'Orders', definition: 'Order expires at market close if not filled. Automatically cancelled at 4:00 PM.' },
  { term: 'Slippage', category: 'Orders', definition: 'The difference between the expected price and the actual fill price. More common with large orders or low-liquidity stocks.' },

  // Trading
  { term: 'Short Selling', category: 'Trading', definition: 'Selling borrowed shares to profit from a price decline. You sell first, buy back later. Unlimited loss potential.' },
  { term: 'Cover', category: 'Trading', definition: 'Buying back shares to close a short position. This is how you exit a short trade.' },
  { term: 'Margin Trading', category: 'Trading', definition: 'Borrowing money from the broker to trade. 2:1 leverage means you can buy $2 of stock for every $1 of cash.' },
  { term: 'Margin Call', category: 'Trading', definition: 'When your equity falls below the maintenance requirement (25%), the broker forcefully sells your positions to recover the loan.' },
  { term: 'Commission', category: 'Trading', definition: 'A fee charged per trade. Reduces your profit on every buy and sell.' },
  { term: 'Buying Power', category: 'Trading', definition: 'Total amount you can spend. With margin: Cash + available margin. Without margin: just Cash.' },

  // Analysis
  { term: 'P/E Ratio', category: 'Analysis', definition: 'Price to Earnings ratio. Stock price divided by annual earnings per share. Lower = potentially undervalued. Negative = company losing money.' },
  { term: 'Market Cap', category: 'Analysis', definition: 'Total market value of a company. Price x Shares Outstanding. Large cap >$10B, Mid cap $2-10B, Small cap <$2B.' },
  { term: 'Dividend Yield', category: 'Analysis', definition: 'Annual dividend payment as a percentage of stock price. 3% yield means $3 per year for every $100 invested.' },
  { term: 'SMA (Simple Moving Average)', category: 'Analysis', definition: 'Average closing price over N periods. SMA50 = 50-day average. Price above SMA = bullish, below = bearish.' },
  { term: 'RSI (Relative Strength Index)', category: 'Analysis', definition: 'Momentum indicator (0-100). Above 70 = overbought (may fall). Below 30 = oversold (may rise).' },
  { term: 'MACD', category: 'Analysis', definition: 'Trend-following momentum indicator. Signal line crossovers indicate buy/sell opportunities.' },
  { term: 'Bollinger Bands', category: 'Analysis', definition: 'Volatility bands around a moving average. Price touching upper band = overbought, lower band = oversold.' },
  { term: 'Fair Value', category: 'Analysis', definition: 'Estimated "true" price of a stock based on fundamentals. If price < fair value, stock may be undervalued.' },
  { term: 'Volume', category: 'Analysis', definition: 'Number of shares traded. High volume confirms price moves. Low volume moves are less reliable.' },
  { term: 'Bid/Ask Spread', category: 'Analysis', definition: 'Difference between highest buy price (bid) and lowest sell price (ask). Tighter spread = more liquid stock.' },
  { term: 'Beta', category: 'Analysis', definition: 'Measures volatility relative to the market. Beta 1.5 = 50% more volatile than the market. Beta 0.5 = half as volatile.' },

  // Market
  { term: 'Bull Market', category: 'Market', definition: 'A market condition where prices are rising or expected to rise. Characterized by optimism and strong buying.' },
  { term: 'Bear Market', category: 'Market', definition: 'A market condition where prices are falling 20%+ from recent highs. Characterized by pessimism and selling.' },
  { term: 'Flash Crash', category: 'Market', definition: 'An extremely rapid market decline, usually caused by cascading sell orders. Can drop 5-10% in minutes.' },
  { term: 'Circuit Breaker', category: 'Market', definition: 'Automatic trading halt triggered by extreme market moves. Level 1: -7%, Level 2: -13%, Level 3: -20% (market closes).' },
  { term: 'IPO (Initial Public Offering)', category: 'Market', definition: 'When a private company first sells shares to the public. Often volatile in early trading.' },
  { term: 'Stock Split', category: 'Market', definition: 'Dividing existing shares into more shares at a lower price. 2:1 split = 2x shares at half the price. Total value unchanged.' },
  { term: 'Yield Curve', category: 'Market', definition: 'Graph of bond yields by maturity. Normal = upward slope. Inverted = short-term rates above long-term (recession signal).' },
  { term: 'Fear & Greed Index', category: 'Market', definition: 'Sentiment indicator (0-100). Below 25 = Extreme Fear (potential buying opportunity). Above 75 = Extreme Greed (potential sell signal).' },
  { term: 'After-Hours Trading', category: 'Market', definition: 'Trading outside regular market hours (4-8 PM). Lower volume, wider spreads, but allows reacting to news after close.' },
  { term: 'Market Maker', category: 'Market', definition: 'A firm that provides liquidity by always offering to buy and sell. They profit from the bid-ask spread.' },
  { term: 'Short Squeeze', category: 'Market', definition: 'When a heavily-shorted stock rises, forcing short sellers to buy back shares, driving the price even higher.' },

  // Portfolio
  { term: 'Unrealized P&L', category: 'Portfolio', definition: 'Profit or loss on positions you still hold. Not "real" until you sell \u2014 the price could change.' },
  { term: 'Realized P&L', category: 'Portfolio', definition: 'Profit or loss on closed trades. This is your actual locked-in gain or loss.' },
  { term: 'Drawdown', category: 'Portfolio', definition: 'Peak-to-trough decline in portfolio value. Max drawdown measures your worst losing streak.' },
  { term: 'Sharpe Ratio', category: 'Portfolio', definition: 'Risk-adjusted return. Higher = better. Above 1.0 = good, above 2.0 = excellent. Measures return per unit of risk.' },
  { term: 'Win Rate', category: 'Portfolio', definition: 'Percentage of trades that were profitable. 50%+ is decent, but depends on average win vs average loss size.' },
  { term: 'Profit Factor', category: 'Portfolio', definition: 'Total gains divided by total losses. Above 1.0 = profitable. Above 1.5 = good. Above 2.0 = excellent.' },
  { term: 'VaR (Value at Risk)', category: 'Portfolio', definition: 'Estimated maximum loss over a time period at a confidence level. "95% VaR of $5,000" means 95% chance you won\'t lose more than $5,000 today.' },
  { term: 'Tax Loss Harvesting', category: 'Portfolio', definition: 'Selling losing positions to offset capital gains taxes. Short-term losses offset short-term gains first.' },
  { term: 'Diversification', category: 'Portfolio', definition: 'Spreading investments across different stocks/sectors to reduce risk. Don\'t put all your eggs in one basket.' },
  { term: 'Position Size', category: 'Portfolio', definition: 'How much of your portfolio to allocate to a single trade. Rule of thumb: never risk more than 2-5% on one trade.' },

  // Economy
  { term: 'Interest Rate (Fed Rate)', category: 'Economy', definition: 'The rate set by the central bank. Higher rates = borrowing more expensive, hurts growth stocks and real estate.' },
  { term: 'Inflation', category: 'Economy', definition: 'Rate at which prices increase. Moderate inflation (2%) is healthy. High inflation erodes purchasing power and increases rates.' },
  { term: 'GDP Growth', category: 'Economy', definition: 'Gross Domestic Product growth rate. Positive = economy expanding. Negative for 2 quarters = recession.' },
  { term: 'PMI (Purchasing Managers Index)', category: 'Economy', definition: 'Manufacturing activity indicator. Above 50 = expansion. Below 50 = contraction.' },
  { term: 'Earnings Per Share (EPS)', category: 'Economy', definition: 'Company profit divided by shares outstanding. Higher EPS = more profitable. Key metric for valuation.' },
  { term: 'Sector Rotation', category: 'Economy', definition: 'Money flowing from one sector to another based on economic cycle. E.g., tech leads in growth, utilities lead in recession.' },
  { term: 'Quantitative Easing (QE)', category: 'Economy', definition: 'Central bank buying bonds to inject money into the economy. Lowers rates, boosts asset prices.' },

  // Regulatory
  { term: 'SSR (Short Sale Restriction)', category: 'Regulatory', definition: 'Rule that restricts short selling when a stock drops 10%+ from previous close. Shorts can only sell on upticks.' },
  { term: 'Insider Trading', category: 'Regulatory', definition: 'Trading based on material, non-public information. Illegal and monitored by the SMA (StockSim Market Authority).' },
  { term: 'Market Manipulation', category: 'Regulatory', definition: 'Artificially influencing stock prices through deceptive practices. Spoofing, wash trading, and pump-and-dump are examples.' },
];
