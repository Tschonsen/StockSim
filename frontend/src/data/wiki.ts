export interface WikiArticle {
  id: string;
  title: string;
  category: string;
  summary: string;
  content: string;
  relatedIds?: string[];
  glossaryTerms?: string[];
}

export const WIKI_CATEGORIES = [
  { id: 'basics', name: 'Trading Basics', icon: '\u{1F4CA}' },
  { id: 'technical', name: 'Technical Analysis', icon: '\u{1F4C8}' },
  { id: 'fundamental', name: 'Fundamental Analysis', icon: '\u{1F4B0}' },
  { id: 'structure', name: 'Market Structure', icon: '\u{1F3DB}\uFE0F' },
  { id: 'strategy', name: 'Strategy & Psychology', icon: '\u{1F9E0}' },
  { id: 'famous', name: 'Famous People & Events', icon: '\u2B50' },
  { id: 'guide', name: 'StockSim Guide', icon: '\u{1F3AE}' },
  { id: 'history', name: 'History of Markets', icon: '\u{1F4DC}' },
] as const;

export type WikiCategoryId = (typeof WIKI_CATEGORIES)[number]['id'];

export const WIKI_ARTICLES: WikiArticle[] = [
  // ═══════════════════════════════════════════════════════════════
  // TRADING BASICS
  // ═══════════════════════════════════════════════════════════════

  {
    id: 'stocks-101',
    title: 'What Are Stocks?',
    category: 'basics',
    summary:
      'Understanding stock ownership, shares, and why companies go public.',
    content: `## What Is a Stock?

A **stock** (also called a **share** or **equity**) represents partial ownership in a company. When you buy one share of a company, you become a part-owner — entitled to a fraction of its profits and assets.

## Why Do Companies Issue Stock?

Companies need money to grow. They can borrow (debt) or sell ownership stakes (equity). When a company "goes public" through an **IPO** (Initial Public Offering), it sells shares to the public for the first time. The money raised goes to the company to fund expansion, research, hiring, or paying off debt.

For example, if NVPH has 10 million shares outstanding and you own 1,000 shares, you own 0.01% of the company.

## What Makes Stock Prices Move?

Stock prices are driven by **supply and demand**. If more people want to buy a stock than sell it, the price goes up. If more people want to sell, the price goes down. Behind that simple mechanic, prices are influenced by:

- **Earnings reports** — Did the company make more or less money than expected?
- **Economic data** — Interest rates, inflation, employment numbers
- **News & sentiment** — Product launches, scandals, analyst upgrades
- **Sector trends** — If tech is hot, tech stocks tend to rise together

## Types of Stock

- **Common stock** — What most people buy. Comes with voting rights and potential dividends.
- **Preferred stock** — Pays a fixed dividend, gets priority in bankruptcy, but usually has no voting rights.

## How You Make (or Lose) Money

There are two ways to profit from stocks:

- **Capital gains** — Buy at $50, sell at $75, pocket $25 per share (minus fees)
- **Dividends** — Some companies pay out a portion of profits regularly

Of course, if the stock drops from $50 to $30, you lose $20 per share. Stocks can also go to $0 if the company goes bankrupt.

> In StockSim, you start with a cash balance and build your portfolio by buying and selling stocks on the simulated market. Watch how news events, earnings reports, and economic shifts affect your holdings in real time.`,
    relatedIds: ['order-types', 'ipos', 'dividends', 'market-cap'],
    glossaryTerms: ['Short Selling', 'Dividend Yield', 'Market Cap'],
  },

  {
    id: 'bid-ask-spread',
    title: 'Bid, Ask & The Spread',
    category: 'basics',
    summary:
      'How stock prices actually work and what the spread costs you on every trade.',
    content: `## The Two Prices

Every stock has two prices at any given moment:

- **Bid** — The highest price a buyer is currently willing to pay
- **Ask** (or **Offer**) — The lowest price a seller is currently willing to accept

If NVPH shows a bid of $148.50 and an ask of $148.75, that means the best buyer will pay $148.50 and the cheapest seller wants $148.75.

## What Is the Spread?

The **spread** is the difference between the bid and the ask. In our example: $148.75 - $148.50 = **$0.25 spread**.

The spread is a hidden cost of trading. When you place a **market order** to buy, you pay the ask price. When you sell, you receive the bid price. So the moment you buy a stock, you are already "down" by the spread amount.

## Why the Spread Matters

- **Liquid stocks** (heavily traded, like large-cap companies) have tight spreads — often just $0.01
- **Illiquid stocks** (thinly traded, small companies) can have wide spreads — $0.50 or more
- **Volatile moments** (earnings releases, market crashes) cause spreads to widen dramatically

If you trade a stock with a $0.50 spread 10 times a day, that is $5.00 per share eaten by spreads alone — before commissions.

## The Market Maker

Someone has to provide those bid and ask prices. **Market makers** are firms that continuously post buy and sell orders, profiting from the spread. They take the risk of holding inventory in exchange for the spread profit.

## Practical Tips

- Always check the spread before trading. A wide spread means higher costs.
- Use **limit orders** instead of market orders to control the price you pay.
- Avoid trading low-volume stocks during off-hours when spreads are widest.

> In StockSim, you can see the bid-ask spread on every stock. Notice how spreads widen during market-moving news events and tighten on calm trading days. Using limit orders helps you avoid paying the full spread.`,
    relatedIds: ['order-types', 'stocks-101', 'volume-analysis'],
    glossaryTerms: [
      'Bid/Ask Spread',
      'Market Order',
      'Limit Order',
      'Slippage',
    ],
  },

  {
    id: 'order-types',
    title: 'Order Types Explained',
    category: 'basics',
    summary:
      'Market, limit, stop, stop-limit, and trailing stop orders — when to use each one.',
    content: `## Market Order

A **market order** says: "Buy (or sell) right now at whatever the current price is." It guarantees execution but not price. On a liquid stock, you will get filled near the displayed price. On a volatile or illiquid stock, you might experience **slippage** — getting a worse price than expected.

**Use when:** You need to get in or out immediately and price precision does not matter.

## Limit Order

A **limit order** says: "Buy at $X or lower" (or "sell at $X or higher"). It guarantees your price but not execution — if the stock never reaches your limit price, the order sits unfilled.

Example: NVPH trades at $150. You place a limit buy at $145. Your order only fills if the price drops to $145 or below.

**Use when:** You want price control and are willing to wait.

## Stop Order (Stop-Loss)

A **stop order** becomes a market order when a trigger price is hit. A **stop-loss** at $140 means: "If the price drops to $140, sell at market."

**Warning:** In a fast-moving market, the actual fill price can be significantly below your stop price.

**Use when:** You want automatic downside protection.

## Stop-Limit Order

Combines a stop trigger with a limit order. A stop-limit with stop $140 and limit $138 means: "If price hits $140, place a limit sell at $138." This prevents selling at a terrible price during a flash crash, but the order might not fill at all if the price blows through both levels.

**Use when:** You want protection but refuse to sell below a certain price.

## Trailing Stop

A **trailing stop** follows the stock price upward by a set amount or percentage. If NVPH rises from $150 to $170 with a $10 trailing stop, your stop moves from $140 to $160. If the stock then drops to $160, you are sold out — locking in gains.

**Use when:** You want to ride a trend and lock in profits automatically.

## Order Duration

- **Day order** — Expires at market close if not filled
- **GTC (Good Till Cancelled)** — Stays active until filled or you cancel it

> In StockSim, you can practice all order types risk-free. Try placing a limit order below the current price and watch how long it takes to fill — or whether it fills at all.`,
    relatedIds: ['bid-ask-spread', 'long-short', 'margin-trading'],
    glossaryTerms: [
      'Market Order',
      'Limit Order',
      'Stop Order',
      'Stop-Limit Order',
      'Trailing Stop',
      'Slippage',
      'Day Order',
      'GTC (Good Till Cancelled)',
    ],
  },

  {
    id: 'long-short',
    title: 'Going Long vs Short Selling',
    category: 'basics',
    summary:
      'The mechanics of profiting from rising and falling stock prices.',
    content: `## Going Long

**Going long** simply means buying a stock with the expectation that its price will rise. You buy at a lower price and hope to sell at a higher one.

- Buy 100 shares of NVPH at $150 = $15,000 invested
- NVPH rises to $180 — sell for $18,000
- **Profit: $3,000** (20% return)

The maximum you can lose going long is 100% of your investment (if the stock goes to $0). Your upside is theoretically unlimited.

## Short Selling

**Short selling** is the opposite: you profit when a stock's price falls. Here is how it works:

1. You **borrow** shares from your broker (they lend from other clients' accounts)
2. You **sell** those borrowed shares at the current market price
3. Later, you **buy back** (cover) the shares at a (hopefully) lower price
4. You **return** the shares to the broker and keep the difference

Example:
- Short 100 shares of CRBL at $80 — you receive $8,000
- CRBL drops to $50 — you buy back for $5,000
- **Profit: $3,000**

## The Dangers of Shorting

Short selling has **asymmetric risk**:

- **Maximum profit** is capped — a stock can only drop to $0 (100% gain)
- **Maximum loss is unlimited** — a stock can rise to $200, $500, or $1,000+

If CRBL surges from $80 to $240, your loss is $16,000 on a $8,000 position — that is a 200% loss. This is why **short squeezes** are so devastating: when shorts are forced to buy back (cover), their buying pushes the price even higher, creating a vicious cycle.

## Margin Requirements

Short selling always requires a **margin account**. You must maintain enough equity as collateral. If the stock rises too much, you will face a **margin call** — the broker demands more cash or forcefully closes your position.

## Key Differences

- **Long:** Buy first, sell later. Risk limited to investment. No borrowing needed.
- **Short:** Sell first, buy later. Unlimited risk. Requires margin. Pay borrow fees.

> In StockSim, you can short sell any stock. Watch your unrealized P&L carefully — a position that moves against you can trigger a margin call and force-close your trade at the worst possible time.`,
    relatedIds: ['margin-trading', 'order-types', 'stocks-101'],
    glossaryTerms: ['Short Selling', 'Cover', 'Margin Call', 'Margin Trading'],
  },

  {
    id: 'margin-trading',
    title: 'Margin Trading',
    category: 'basics',
    summary:
      'How leverage works, margin calls, and why margin amplifies both gains and losses.',
    content: `## What Is Margin?

**Margin trading** means borrowing money from your broker to buy more stock than you could with cash alone. A **2:1 margin** means for every $1 of your own money, you can buy $2 worth of stock.

With $50,000 in cash and 2:1 margin, your **buying power** is $100,000.

## How It Amplifies Returns

If you buy $100,000 of NVPH at $150 using $50,000 cash + $50,000 margin:

- NVPH rises 10% to $165 — your position is worth $110,000. After repaying the $50,000 loan, you have $60,000. That is a **20% return** on your $50,000 cash.
- NVPH drops 10% to $135 — your position is worth $90,000. After the $50,000 loan, you have $40,000. That is a **20% loss** on your cash.

Leverage doubles both gains and losses.

## Initial vs Maintenance Margin

- **Initial margin** — The minimum equity you need to open a position (typically 50%)
- **Maintenance margin** — The minimum equity you must maintain (typically 25%)

## Margin Calls

When your equity drops below the maintenance requirement, the broker issues a **margin call**. You must either:

1. Deposit more cash
2. Sell positions to free up equity

If you do not act, the broker will **liquidate your positions** — often at the worst possible time, selling at the bottom of a drop.

## Example: Margin Call

- You have $50,000 cash, borrow $50,000, buy $100,000 of stock
- Stock drops 40% — position worth $60,000
- Your equity: $60,000 - $50,000 loan = $10,000
- Equity ratio: $10,000 / $60,000 = 16.7% — below the 25% maintenance
- **Margin call triggered** — broker force-sells your stock

## Interest on Margin

Borrowed money is not free. Brokers charge **interest** on your margin balance, typically 6-10% annually. This adds up, especially on long-held positions.

## Practical Rules

- Never use maximum margin — leave a buffer for volatility
- Set stop-losses to prevent margin calls
- Margin works best for short-term trades, not long-term holds (interest accumulates)

> In StockSim, margin is available once you have enough experience. Watch how a leveraged position behaves during a market downturn — it is a powerful lesson in risk management.`,
    relatedIds: ['long-short', 'order-types', 'debt-analysis'],
    glossaryTerms: [
      'Margin Trading',
      'Margin Call',
      'Buying Power',
      'Commission',
    ],
  },

  {
    id: 'dividends',
    title: 'Dividends',
    category: 'basics',
    summary:
      'How dividend payments work, key dates, yield calculations, and reinvestment strategies.',
    content: `## What Are Dividends?

A **dividend** is a payment a company makes to its shareholders from its profits. Not all companies pay dividends — fast-growing companies typically reinvest profits instead. Mature, profitable companies (utilities, banks, consumer staples) are the most reliable dividend payers.

## Key Dates

Four dates matter for every dividend:

- **Declaration date** — The company announces the dividend amount and dates
- **Ex-dividend date** — The cutoff. You must own the stock BEFORE this date to receive the dividend. If you buy on or after the ex-date, you get nothing.
- **Record date** — The company checks who owns shares (usually 1-2 days after ex-date)
- **Payment date** — Cash hits your account

**Important:** On the ex-dividend date, the stock price typically drops by approximately the dividend amount, since new buyers will not receive that payment.

## Dividend Yield

**Dividend yield** = Annual dividend / Stock price

If DENA pays $2.00 per year in dividends and trades at $50, the yield is 4%. This lets you compare income across stocks — but beware: an extremely high yield (8%+) can be a warning sign that the market expects a dividend cut.

## Qualified vs Ordinary Dividends

- **Qualified dividends** are taxed at the lower capital gains rate. You must hold the stock for at least 60 days around the ex-date.
- **Ordinary dividends** are taxed as regular income — a higher rate.

## DRIP: Dividend Reinvestment

A **DRIP** (Dividend Reinvestment Plan) automatically uses your dividend payments to buy more shares. Over decades, reinvesting dividends can dramatically boost returns through **compounding** — your dividends earn dividends.

Example: $10,000 invested at 7% annual return with dividends reinvested grows to ~$76,000 in 30 years. Without reinvestment, just ~$40,000.

## Dividend Aristocrats

Companies that have increased their dividend for 25+ consecutive years are called **Dividend Aristocrats**. These are considered among the most reliable income stocks.

> In StockSim, dividend-paying stocks distribute cash to your account automatically. Check the company details panel to see dividend yield and payment schedules. Building a portfolio of reliable dividend payers is one of many viable strategies.`,
    relatedIds: ['dividend-yield', 'stocks-101', 'revenue-earnings'],
    glossaryTerms: ['Dividend Yield'],
  },

  {
    id: 'ipos',
    title: 'IPOs & Delistings',
    category: 'basics',
    summary:
      'How companies enter and exit the stock market, and the risks of trading new listings.',
    content: `## What Is an IPO?

An **IPO** (Initial Public Offering) is when a private company sells shares to the public for the first time. It is the company's "debut" on the stock market.

## The IPO Process

1. **Hiring underwriters** — The company hires investment banks (Goldman Sachs, Morgan Stanley) to manage the offering
2. **SEC filing** — The company files an S-1 prospectus detailing its financials, risks, and plans
3. **Roadshow** — Executives pitch to institutional investors to gauge demand
4. **Pricing** — The underwriters set the IPO price based on demand
5. **Trading begins** — Shares start trading on an exchange (NYSE, NASDAQ)

## The Lock-Up Period

Company insiders (founders, employees, early investors) are usually restricted from selling their shares for **90-180 days** after the IPO. When the lock-up expires, a flood of insider selling can push the price down sharply.

## IPO Risks

- **Hype vs reality** — IPOs often open at inflated prices driven by excitement. Many drop 20-40% within the first year.
- **Limited history** — Newly public companies have short track records. Financial data is limited.
- **Volatility** — First-day trading can be wild, with swings of 30%+ in either direction.
- **Information asymmetry** — Insiders know far more about the company than you do.

## IPO "Pop" and Who Benefits

When an IPO opens 40% above its offering price, that "pop" mostly benefits institutional investors who got shares at the IPO price. Retail investors buying on the open market often pay inflated prices.

## Delistings

A stock gets **delisted** when it is removed from an exchange. This happens when:

- The company goes bankrupt
- Share price stays below $1 for an extended period
- The company fails to meet exchange listing requirements
- The company goes private (bought out)

Delisted stocks often become nearly worthless and extremely difficult to sell.

> In StockSim, new companies occasionally IPO onto the market. Pay attention to the initial pricing — buying on day one is risky, but waiting too long might mean missing genuine opportunities. Watch for the lock-up expiration date.`,
    relatedIds: ['stocks-101', 'market-cap', 'volume-analysis'],
    glossaryTerms: ['Market Cap'],
  },

  {
    id: 'etfs',
    title: 'ETFs',
    category: 'basics',
    summary:
      'How Exchange-Traded Funds work, their advantages, and common types.',
    content: `## What Is an ETF?

An **ETF** (Exchange-Traded Fund) is a basket of stocks (or other assets) bundled into a single tradable security. When you buy one share of an S&P 500 ETF, you effectively own a tiny piece of all 500 companies in the index.

ETFs trade on exchanges just like regular stocks — you can buy and sell them throughout the day at market prices.

## How ETFs Track an Index

Most ETFs are **passively managed**, meaning they simply mirror an index by holding the same stocks in the same proportions. If NVPH makes up 3% of the index, the ETF holds 3% of its assets in NVPH.

The ETF price stays close to the value of its underlying holdings through a mechanism called **creation and redemption** involving large institutional players called Authorized Participants.

## Common ETF Types

- **Broad market** — Track the entire market (S&P 500, Total Stock Market)
- **Sector** — Focus on one industry (Technology, Healthcare, Energy)
- **Bond** — Hold government or corporate bonds
- **International** — Track foreign markets (Europe, Emerging Markets)
- **Thematic** — Target trends (AI, Clean Energy, Cybersecurity)
- **Inverse/Leveraged** — Amplified or opposite returns (risky, for short-term trading only)

## ETFs vs Individual Stocks

**Advantages of ETFs:**
- **Diversification** — One purchase gives exposure to dozens or hundreds of companies
- **Lower risk** — If one company in the basket collapses, the damage is limited
- **Low fees** — Passive ETFs charge 0.03-0.20% annually
- **Simplicity** — No need to research individual companies

**Disadvantages:**
- **Capped upside** — You will never 10x an index fund in a year
- **Tracking error** — The ETF may slightly underperform its index
- **Sector drag** — In a sector ETF, weak companies pull down strong ones

## The 90% Rule

Studies consistently show that over long periods, roughly 90% of actively managed funds underperform a simple S&P 500 index ETF. This is why many legendary investors recommend index ETFs for most people.

> In StockSim, ETFs let you gain broad market exposure without picking individual winners. Try comparing the performance of a diversified ETF against your hand-picked stock portfolio over a full market cycle.`,
    relatedIds: ['stocks-101', 'market-cap', 'dividends'],
    glossaryTerms: ['Market Cap', 'Volume'],
  },

  // ═══════════════════════════════════════════════════════════════
  // TECHNICAL ANALYSIS
  // ═══════════════════════════════════════════════════════════════

  {
    id: 'candlesticks',
    title: 'Reading Candlestick Charts',
    category: 'technical',
    summary:
      'Understanding OHLC candles and the most common patterns traders watch for.',
    content: `## Anatomy of a Candlestick

Each candlestick shows four data points for a time period (1 minute, 1 hour, 1 day, etc.):

- **Open** — The price at the start of the period
- **High** — The highest price reached
- **Low** — The lowest price reached
- **Close** — The price at the end of the period

The thick part is the **body**. The thin lines above and below are **wicks** (or shadows).

- **Green/white candle** — Close > Open (price went up)
- **Red/black candle** — Close < Open (price went down)

## Key Single-Candle Patterns

**Doji** — Open and close are nearly identical, creating a cross shape. Signals indecision. After a strong trend, a doji suggests the trend may be losing momentum.

**Hammer** — Small body at the top, long lower wick. Appears after a downtrend and signals potential reversal upward. Buyers stepped in and pushed the price back up.

**Shooting Star** — Small body at the bottom, long upper wick. Appears after an uptrend. Sellers rejected the higher prices.

**Marubozu** — A candle with no wicks at all. A green marubozu means buyers dominated the entire period — strong bullish signal.

## Key Multi-Candle Patterns

**Engulfing** — A candle whose body completely engulfs the previous candle's body. A bullish engulfing (green engulfs red) after a downtrend signals reversal.

**Morning Star** — Three candles: large red, small-bodied (any color), large green. A powerful bottoming signal.

**Evening Star** — The opposite: large green, small-bodied, large red. Signals a top.

**Three White Soldiers** — Three consecutive green candles with higher closes. Strong bullish continuation.

## Reading Wicks

Long wicks tell a story:

- **Long upper wick** — Sellers pushed back against buyers. Bearish pressure.
- **Long lower wick** — Buyers defended against sellers. Bullish pressure.
- **Short wicks both sides** — Conviction in the direction of the body.

## Practical Tips

No single candle pattern is reliable on its own. Always confirm with:
- **Volume** — Patterns on high volume are more significant
- **Context** — Where the pattern appears matters (at support? resistance? after a trend?)
- **Other indicators** — RSI, MACD, moving averages

> In StockSim, the chart displays candlesticks by default. Zoom in on turning points and see if you can spot doji, hammer, or engulfing patterns at key support and resistance levels.`,
    relatedIds: ['support-resistance', 'volume-analysis', 'moving-averages'],
    glossaryTerms: [
      'Volume',
      'SMA (Simple Moving Average)',
      'RSI (Relative Strength Index)',
    ],
  },

  {
    id: 'moving-averages',
    title: 'Moving Averages',
    category: 'technical',
    summary:
      'SMA and EMA explained, plus the golden cross and death cross signals.',
    content: `## What Is a Moving Average?

A **moving average** smooths out price data by calculating the average price over a set number of periods. It filters out day-to-day noise and reveals the underlying trend.

## SMA: Simple Moving Average

The **SMA** gives equal weight to every price in the period.

**SMA(20)** = Sum of last 20 closing prices / 20

Common SMAs:
- **SMA 20** — Short-term trend (roughly one month of trading days)
- **SMA 50** — Medium-term trend
- **SMA 200** — Long-term trend (roughly one year)

If NVPH trades at $155 and its SMA 50 is $148, the stock is above its medium-term average — generally considered **bullish**.

## EMA: Exponential Moving Average

The **EMA** gives more weight to recent prices, making it faster to react to new information. Traders who want quicker signals prefer EMAs.

The EMA(12) and EMA(26) are the foundation of the MACD indicator.

## Trading Signals

**Price crossing a moving average:**
- Price crosses ABOVE the SMA 50 — bullish signal (potential uptrend beginning)
- Price crosses BELOW the SMA 50 — bearish signal (potential downtrend)

**Moving average crossovers:**
- **Golden Cross** — SMA 50 crosses ABOVE the SMA 200. This is one of the most watched bullish signals in all of technical analysis. Historically, golden crosses have preceded significant rallies.
- **Death Cross** — SMA 50 crosses BELOW the SMA 200. Bearish signal suggesting a long-term downtrend may be forming.

## Dynamic Support and Resistance

Moving averages often act as **dynamic support** in uptrends and **dynamic resistance** in downtrends. In a strong uptrend, the price tends to bounce off the SMA 50. If it breaks below the SMA 50 but holds the SMA 200, that is a deeper pullback but potentially still an intact trend.

## Limitations

- Moving averages are **lagging indicators** — they tell you what already happened, not what will happen
- In **sideways markets**, moving average signals produce many false crossovers (whipsaws)
- No single moving average length works for all stocks or all market conditions

> In StockSim, toggle the SMA 50 and SMA 200 overlays on your chart. Watch for golden cross and death cross events — they often align with major trend changes in the simulated market.`,
    relatedIds: ['macd', 'support-resistance', 'candlesticks'],
    glossaryTerms: ['SMA (Simple Moving Average)', 'MACD'],
  },

  {
    id: 'rsi',
    title: 'RSI - Relative Strength Index',
    category: 'technical',
    summary:
      'How to use RSI to identify overbought and oversold conditions, plus divergence signals.',
    content: `## What Is RSI?

The **Relative Strength Index (RSI)** is a momentum oscillator that measures the speed and magnitude of recent price changes. It ranges from **0 to 100**.

Developed by J. Welles Wilder in 1978, it remains one of the most widely used technical indicators.

## How RSI Is Calculated

RSI compares the average gain to the average loss over a period (typically 14 days):

- RSI = 100 - (100 / (1 + RS))
- RS = Average Gain / Average Loss

You do not need to calculate it manually — every charting platform does it for you.

## Reading RSI

- **Above 70** — The stock is considered **overbought**. It has risen quickly and may be due for a pullback.
- **Below 30** — The stock is considered **oversold**. It has fallen sharply and may be due for a bounce.
- **Between 30-70** — Neutral territory.

**Important:** Overbought does not mean "sell immediately." In a strong uptrend, RSI can stay above 70 for weeks. It is a warning sign, not a guaranteed reversal.

## RSI Divergence

**Divergence** occurs when price and RSI disagree — and it is often a powerful signal.

**Bullish divergence:** Price makes a new low, but RSI makes a higher low. The selling momentum is weakening even though price is still falling. Often precedes a reversal upward.

**Bearish divergence:** Price makes a new high, but RSI makes a lower high. The buying momentum is fading. Often precedes a reversal downward.

Example: CRBL drops from $80 to $60 (RSI at 25), then bounces to $70, then drops again to $55 (but RSI is at 30). Price made a lower low ($55 < $60), but RSI made a higher low (30 > 25). That is bullish divergence — the downtrend may be ending.

## RSI Strategies

- **Mean reversion** — Buy when RSI drops below 30, sell when it rises above 70
- **Trend confirmation** — In an uptrend, buy when RSI pulls back to 40-50 (not oversold, just resting)
- **Divergence trading** — Enter when price/RSI divergence appears, confirmed by other indicators

## Adjusting the Period

- **RSI(7)** — More sensitive, more signals, more false alarms
- **RSI(14)** — Standard, balanced
- **RSI(21)** — Smoother, fewer signals, more reliable

> In StockSim, RSI is available as a chart overlay. Watch how stocks behave after entering overbought or oversold territory. Pay special attention to divergences at major turning points — they are some of the most reliable signals in technical analysis.`,
    relatedIds: ['macd', 'bollinger-bands', 'candlesticks'],
    glossaryTerms: ['RSI (Relative Strength Index)'],
  },

  {
    id: 'macd',
    title: 'MACD',
    category: 'technical',
    summary:
      'Understanding the MACD line, signal line, and histogram for trend and momentum analysis.',
    content: `## What Is MACD?

**MACD** (Moving Average Convergence Divergence) is a trend-following momentum indicator that shows the relationship between two exponential moving averages.

Developed by Gerald Appel in the late 1970s, it is one of the most popular indicators for identifying trend changes and momentum shifts.

## Components

The MACD has three components:

- **MACD line** = EMA(12) - EMA(26). When the short-term average is above the long-term average, the MACD line is positive (bullish momentum).
- **Signal line** = EMA(9) of the MACD line. A smoothed version used to generate trade signals.
- **Histogram** = MACD line - Signal line. Visualizes the gap between the two lines. Tall bars = strong momentum. Shrinking bars = momentum fading.

## Signal Line Crossovers

The most common MACD signal:

- **Bullish crossover** — MACD line crosses ABOVE the signal line. Suggests upward momentum is building. Potential buy signal.
- **Bearish crossover** — MACD line crosses BELOW the signal line. Suggests momentum is shifting downward. Potential sell signal.

Example: NVPH has been falling. The MACD line turns upward and crosses above the signal line while both are below zero. This is a bullish crossover from oversold territory — often a strong buy signal.

## Zero Line Crossover

- MACD crossing above zero means the short-term EMA has crossed above the long-term EMA — confirming an uptrend
- MACD crossing below zero confirms a downtrend

## Histogram Analysis

The histogram gives early warnings:

- Histogram bars getting **taller** — momentum increasing in current direction
- Histogram bars getting **shorter** — momentum fading, possible reversal coming
- Histogram flipping from negative to positive (or vice versa) — the crossover just occurred

## MACD Divergence

Like RSI, MACD can show divergence:

- Price makes new highs but MACD makes lower highs — **bearish divergence** (momentum not confirming the move)
- Price makes new lows but MACD makes higher lows — **bullish divergence**

## Limitations

- MACD is a **lagging indicator** (based on moving averages)
- Produces many false signals in choppy, sideways markets
- Works best in trending markets with clear directional moves

> In StockSim, enable the MACD indicator below your price chart. Compare MACD crossovers with actual price reversals. You will notice they work well in trending markets but produce whipsaws during consolidation.`,
    relatedIds: ['moving-averages', 'rsi', 'volume-analysis'],
    glossaryTerms: ['MACD', 'SMA (Simple Moving Average)'],
  },

  {
    id: 'bollinger-bands',
    title: 'Bollinger Bands',
    category: 'technical',
    summary:
      'How Bollinger Bands measure volatility and signal potential breakouts.',
    content: `## What Are Bollinger Bands?

**Bollinger Bands** are a volatility indicator created by John Bollinger in the 1980s. They consist of three lines plotted on the price chart:

- **Middle band** — A 20-period simple moving average (SMA 20)
- **Upper band** — SMA 20 + (2 x standard deviation)
- **Lower band** — SMA 20 - (2 x standard deviation)

The bands automatically widen when volatility increases and narrow when volatility decreases.

## Reading the Bands

Statistically, about **95% of price action** falls within the bands (2 standard deviations). When price touches or exceeds a band, something notable is happening.

- **Price at upper band** — The stock is relatively expensive compared to recent history. Could mean overbought or could mean strong breakout.
- **Price at lower band** — The stock is relatively cheap. Could mean oversold or could mean breakdown.
- **Price riding the band** — In a strong trend, the price can "walk the band" — repeatedly touching the upper (or lower) band. This is a sign of strength, not necessarily a reversal signal.

## The Bollinger Squeeze

The most powerful Bollinger Band signal is the **squeeze**:

1. The bands narrow dramatically — volatility is low, the market is quiet
2. This calm is often the "eye before the storm"
3. A sharp move (breakout) typically follows

The squeeze does not tell you which direction the breakout will go — you need other indicators (volume, RSI, trend context) for that.

## Bollinger Band Strategies

**Mean reversion:** Buy when price touches the lower band and RSI is oversold. Sell when price returns to the middle band. Works best in ranging markets.

**Breakout trading:** Wait for a squeeze, then enter in the direction of the breakout with volume confirmation.

**Trend following:** In an uptrend, buy when price pulls back to the middle band (SMA 20) and bounces. The middle band acts as dynamic support.

## Combining with Other Indicators

Bollinger Bands are most effective when combined with:
- **RSI** — To confirm overbought/oversold at the bands
- **Volume** — High volume breakouts from a squeeze are more reliable
- **MACD** — To confirm the trend direction

> In StockSim, enable Bollinger Bands to see volatility in action. Watch for squeeze setups where the bands get very tight — then follow the breakout direction with a confirmed trade.`,
    relatedIds: ['rsi', 'moving-averages', 'support-resistance'],
    glossaryTerms: [
      'Bollinger Bands',
      'SMA (Simple Moving Average)',
      'Volume',
    ],
  },

  {
    id: 'support-resistance',
    title: 'Support & Resistance',
    category: 'technical',
    summary:
      'Identifying price levels where stocks tend to bounce or reverse, and how breakouts work.',
    content: `## What Is Support?

**Support** is a price level where a stock tends to stop falling and bounce upward. It acts as a "floor." At this level, buyers see value and step in, creating enough demand to halt the decline.

If NVPH drops to $140 three times over two months and bounces each time, $140 is a strong support level.

## What Is Resistance?

**Resistance** is a price level where a stock tends to stop rising and pull back. It acts as a "ceiling." At this level, sellers take profits or short sellers enter, creating enough supply to halt the advance.

If NVPH rallies to $165 repeatedly but fails to break above it, $165 is a strong resistance level.

## Why Do These Levels Work?

Support and resistance are rooted in human psychology:

- **Memory** — Traders remember buying at $140 and seeing it bounce. When it returns to $140, they buy again.
- **Anchoring** — Traders who missed the previous bounce at $140 are "anchored" to that price and plan to buy there next time.
- **Round numbers** — $100, $50, $200 act as natural support/resistance because humans gravitate toward round numbers.

## Breakouts

When price breaks through a support or resistance level with conviction (high volume), it often signals a significant move:

- **Breaking above resistance** — Bullish breakout. Previous sellers have been overcome. The stock may run significantly higher.
- **Breaking below support** — Bearish breakdown. Previous buyers have been overwhelmed. More downside likely.

## Role Reversal

One of the most important concepts: **when support breaks, it becomes resistance. When resistance breaks, it becomes support.**

If NVPH breaks below $140 support, that $140 level now becomes resistance. The stock may rally back to $140 but get rejected there by sellers who are relieved to "get out even."

## How to Identify Levels

- **Historical highs and lows** — Previous peaks and troughs
- **Moving averages** — SMA 50 and SMA 200 act as dynamic levels
- **Round numbers** — $100, $150, $200
- **Volume profile** — Prices where lots of trading occurred act as magnetic levels
- **Gap zones** — Price gaps often act as future support or resistance

## False Breakouts

Not every breakout is real. **False breakouts** (fakeouts) occur when price briefly breaches a level, traps traders, then reverses. To filter:

- Wait for a **close** beyond the level (not just intraday)
- Require high volume confirmation
- Look for a retest of the broken level before entering

> In StockSim, watch how prices react at previous highs and lows. Draw mental support and resistance lines and observe role reversal in action. These concepts form the foundation of almost every trading strategy.`,
    relatedIds: ['candlesticks', 'volume-analysis', 'bollinger-bands'],
    glossaryTerms: ['Volume', 'SMA (Simple Moving Average)'],
  },

  {
    id: 'volume-analysis',
    title: 'Volume Analysis',
    category: 'technical',
    summary:
      'How trading volume confirms or contradicts price moves, plus On-Balance Volume (OBV).',
    content: `## What Is Volume?

**Volume** is the number of shares traded during a given period. It measures participation and conviction. A stock that moves on heavy volume carries more significance than the same move on light volume.

## Volume Confirms Price

The core principle: **volume should confirm the trend.**

- Price rises + volume increases = **Healthy uptrend.** Many participants are buying.
- Price rises + volume decreases = **Weakening uptrend.** Fewer buyers pushing price up. Potential reversal ahead.
- Price falls + volume increases = **Strong selling pressure.** Panic or capitulation may be occurring.
- Price falls + volume decreases = **Selling interest fading.** The decline may be nearing its end.

## Volume Spikes

A sudden spike in volume (3-5x average) signals something important:

- **Earnings release** — The market digests new information
- **Breaking news** — Acquisitions, scandals, regulatory changes
- **Breakout** — Price breaks through support/resistance on volume, confirming the move
- **Climax** — Extreme volume at the end of a long trend often marks exhaustion (selling climax at a bottom, buying climax at a top)

## Volume Divergence

**Volume divergence** works like price divergence:

- Price makes a new high, but volume is lower than the previous high — **bearish divergence.** The rally is losing steam.
- Price makes a new low, but volume is lower than the previous low — **bullish divergence.** Selling pressure is weakening.

## On-Balance Volume (OBV)

**OBV** is a cumulative indicator:
- On up days, add the day's volume to a running total
- On down days, subtract it

The direction of OBV matters more than the number itself:
- Rising OBV with rising price — trend confirmed
- Rising OBV with flat price — accumulation (smart money buying). Price may soon break out upward.
- Falling OBV with flat price — distribution (smart money selling). Price may soon break down.

## Average Volume

A stock's **average daily volume** tells you about its liquidity:

- **High volume stocks** (>1M shares/day) — Easy to enter and exit, tight spreads
- **Low volume stocks** (<100K shares/day) — Harder to trade, wider spreads, more slippage

## Practical Rules

- Never trust a breakout without volume confirmation
- Watch for volume spikes as early warning of big moves
- Use OBV to detect accumulation before price moves up

> In StockSim, volume bars are displayed below every chart. Compare volume during breakouts versus false breakouts. You will quickly learn that volume is the "truth serum" of the market.`,
    relatedIds: ['support-resistance', 'candlesticks', 'vwap'],
    glossaryTerms: ['Volume', 'Bid/Ask Spread', 'Slippage'],
  },

  {
    id: 'vwap',
    title: 'VWAP',
    category: 'technical',
    summary:
      'Volume Weighted Average Price — the institutional benchmark for fair intraday pricing.',
    content: `## What Is VWAP?

**VWAP** (Volume Weighted Average Price) is the average price a stock has traded at throughout the day, weighted by volume. Unlike a simple average, VWAP gives more weight to prices where more shares changed hands.

VWAP = Cumulative (Price x Volume) / Cumulative Volume

It resets at the start of each trading day, making it an **intraday indicator only.**

## Why VWAP Matters

VWAP is the **benchmark institutional traders use** to evaluate their execution quality. A fund manager who needs to buy 500,000 shares of a stock wants to pay at or below VWAP. Paying above VWAP means they overpaid relative to the market.

Because large players use VWAP as their target, the price often respects VWAP as a level — making it useful for all traders.

## VWAP as Support and Resistance

- **Price above VWAP** — Buyers are in control. Most traders who traded today are profitable, which creates a bullish sentiment.
- **Price below VWAP** — Sellers are in control. Most traders are underwater, which creates selling pressure as they cut losses.
- **Price at VWAP** — A tug-of-war. The market is fairly priced relative to today's trading.

## Trading with VWAP

**Trend trading:**
- In an uptrend day, buy pullbacks to VWAP. The dip represents the average price — still a good entry if the trend is strong.
- In a downtrend day, sell (or short) rallies to VWAP.

**Mean reversion:**
- If price spikes far above VWAP on a news event, it often drifts back toward VWAP as the excitement fades.
- If price crashes far below VWAP, a bounce back toward VWAP is common.

## VWAP Bands

Some traders add standard deviation bands around VWAP (similar to Bollinger Bands). Prices at +2 standard deviations above VWAP are statistically extended and may revert. Prices at -2 standard deviations are statistically cheap.

## Limitations

- **Intraday only** — VWAP resets daily, so it is useless for multi-day analysis
- **Lagging** — Like all averages, it reflects where price has been, not where it is going
- **Self-fulfilling** — Because so many traders watch it, VWAP levels can become crowded trades

## Practical Tips

- Compare your fill price to VWAP. If you consistently buy above VWAP, your execution needs improvement.
- VWAP is most useful in the middle of the trading day. At the open, there is not enough data; at the close, it is too anchored.
- Combine VWAP with volume analysis for the strongest signals.

> In StockSim, VWAP is available as a chart overlay on intraday timeframes. Use it to evaluate whether you are getting good entry prices on your trades. Try the strategy of buying at VWAP in an uptrending stock and see how it compares to buying at random times.`,
    relatedIds: ['volume-analysis', 'support-resistance', 'moving-averages'],
    glossaryTerms: ['Volume'],
  },

  // ═══════════════════════════════════════════════════════════════
  // FUNDAMENTAL ANALYSIS
  // ═══════════════════════════════════════════════════════════════

  {
    id: 'pe-ratio',
    title: 'P/E Ratio',
    category: 'fundamental',
    summary:
      'The most common valuation metric — comparing stock price to earnings, including forward P/E and PEG ratio.',
    content: `## What Is the P/E Ratio?

The **Price-to-Earnings (P/E) ratio** is the most widely used valuation metric in investing. It tells you how much investors are paying for each dollar of a company's earnings.

**P/E = Stock Price / Earnings Per Share (EPS)**

If NVPH trades at $150 and earned $5.00 per share last year, its P/E is 30. Investors are paying $30 for every $1 of earnings.

## Trailing vs Forward P/E

- **Trailing P/E** uses the last 12 months of actual earnings. It is factual but backward-looking.
- **Forward P/E** uses analyst estimates for the next 12 months. It is forward-looking but relies on projections that may be wrong.

A company with trailing P/E of 40 but forward P/E of 25 is expected to grow earnings significantly. The market is pricing in that growth.

## What Is a "Good" P/E?

There is no universal answer. P/E depends heavily on:

- **Sector** — Tech stocks average P/E 25-35. Utilities average 15-20. Banks average 10-15.
- **Growth rate** — Fast-growing companies deserve higher P/Es because their earnings will be larger in the future.
- **Interest rates** — When rates are low, investors accept higher P/Es. When rates rise, P/Es compress.
- **Market conditions** — In a bull market, P/Es expand. In a bear market, they contract.

## The PEG Ratio

The **PEG ratio** adjusts the P/E for growth:

**PEG = P/E / Annual Earnings Growth Rate**

A stock with P/E of 30 growing earnings at 30% per year has a PEG of 1.0 — generally considered fairly valued. PEG below 1.0 suggests undervaluation relative to growth. PEG above 2.0 suggests the price may be getting ahead of the fundamentals.

## P/E Traps

- **Negative P/E** — The company is losing money. P/E is meaningless for unprofitable companies.
- **Artificially low P/E** — One-time gains (selling a division, tax benefits) can inflate earnings temporarily, making P/E look deceptively low.
- **Cyclical stocks** — Companies in cyclical industries (oil, mining, autos) often have their lowest P/E at the peak of the cycle and highest P/E at the bottom.

## Practical Use

- Compare P/E within the same sector, never across different sectors
- Use forward P/E for growth stocks, trailing P/E for stable companies
- Combine P/E with other metrics — no single number tells the whole story

> In StockSim, every company displays its P/E ratio in the details panel. Compare P/E ratios across similar companies in the same sector to identify which might be overvalued or undervalued relative to peers.`,
    relatedIds: ['revenue-earnings', 'fair-value', 'market-cap'],
    glossaryTerms: ['P/E Ratio'],
  },

  {
    id: 'revenue-earnings',
    title: 'Revenue & Earnings',
    category: 'fundamental',
    summary:
      'Understanding income statements, EPS, earnings beats and misses, and why they move stocks.',
    content: `## Revenue vs Earnings

- **Revenue** (also called sales or "top line") is the total money a company brings in from selling products and services.
- **Earnings** (also called profit, net income, or "bottom line") is what remains after all expenses, taxes, and costs are subtracted.

A company can have massive revenue but tiny earnings if costs are high. Conversely, a lean company can turn modest revenue into strong profits.

## The Income Statement

The income statement shows how revenue becomes earnings:

- **Revenue** — Total sales
- minus **Cost of Goods Sold (COGS)** = **Gross Profit**
- minus **Operating Expenses** (R&D, sales, admin) = **Operating Income**
- minus **Interest, Taxes, Other** = **Net Income (Earnings)**

## Earnings Per Share (EPS)

**EPS = Net Income / Shares Outstanding**

If a company earns $500 million and has 100 million shares, EPS = $5.00. This is the number used to calculate the P/E ratio and is the single most watched metric during earnings season.

## Earnings Season

Every quarter, public companies report their results. This is **earnings season** — typically starting in January, April, July, and October.

What matters most is not the absolute numbers, but how they compare to **analyst estimates (consensus)**:

- **Beat** — Earnings exceed expectations. Stock usually rises.
- **Miss** — Earnings fall short. Stock usually drops.
- **In-line** — Meets expectations. Reaction depends on guidance.

The magnitude of the beat or miss matters too. A $0.01 beat on a $2.00 EPS is negligible. A $0.50 beat is significant.

## Forward Guidance

Often more important than the actual results is what management says about the future — **forward guidance.** A company can beat earnings but still crash if it lowers guidance for next quarter.

"We beat by 10%, but next quarter will be weak" = stock drops.
"We missed by 5%, but we see accelerating growth ahead" = stock may rise.

## Revenue Growth

Revenue growth is critical, especially for younger companies:

- **Consistent 20%+ growth** — The market rewards this with high valuations
- **Slowing growth** — Even profitable companies get punished when growth decelerates (e.g., from 30% to 20%)
- **Negative growth** — Alarm bells. The company is shrinking.

## Practical Tips

- Check both revenue AND earnings trends over multiple quarters
- Pay attention to year-over-year comparisons, not just sequential quarters (seasonality matters)
- Read the earnings call summary for management tone — are they confident or cautious?

> In StockSim, companies release earnings reports quarterly. Watch how the stock reacts to beats and misses. The moment of an earnings release is one of the most volatile and educational experiences in the market.`,
    relatedIds: ['pe-ratio', 'fair-value', 'debt-analysis'],
    glossaryTerms: ['P/E Ratio'],
  },

  {
    id: 'debt-analysis',
    title: 'Debt Analysis',
    category: 'fundamental',
    summary:
      "How to evaluate a company's debt levels, including D/E ratio, interest coverage, and bankruptcy risk.",
    content: `## Why Debt Matters

Debt is a double-edged sword. Used wisely, it helps companies grow faster by funding expansion, acquisitions, and operations. Used recklessly, it can lead to **bankruptcy**.

As an investor, understanding a company's debt situation is critical — especially during economic downturns when revenue drops but debt payments remain fixed.

## Debt-to-Equity Ratio (D/E)

**D/E = Total Debt / Shareholders' Equity**

This measures how much a company relies on borrowed money versus owner investment.

- **D/E of 0.5** — For every $1 of equity, the company has $0.50 of debt. Conservative.
- **D/E of 1.0** — Equal debt and equity. Moderate.
- **D/E of 2.0+** — Heavily leveraged. Higher risk.

**Sector context matters:** Banks naturally operate with high D/E ratios (5-10x) because their business model is based on lending. Tech companies often have D/E below 0.5.

## Interest Coverage Ratio

**Interest Coverage = Operating Income / Interest Expense**

This tells you how easily a company can pay the interest on its debt.

- **Above 5** — Comfortable. Plenty of income to cover interest.
- **Between 2-5** — Adequate but watch for declining trends.
- **Below 2** — Danger zone. A bad quarter could mean missed payments.
- **Below 1** — The company cannot cover its interest from operations. It is burning cash or borrowing more.

## Net Debt

**Net Debt = Total Debt - Cash on Hand**

A company with $5 billion in debt but $8 billion in cash actually has **negative net debt** — it could pay off all debt immediately. This is a strong position. Many large tech companies sit on massive cash piles.

## Warning Signs

- **Rising D/E ratio** over multiple quarters — the company is borrowing more
- **Falling interest coverage** — earnings not keeping up with debt costs
- **Debt maturing soon** — If a company must refinance large amounts of debt in a high-interest-rate environment, costs can spike
- **Credit rating downgrade** — Rating agencies (Moody's, S&P) downgrade companies with deteriorating debt profiles
- **Negative free cash flow** — The company is spending more than it earns and may need to borrow to survive

## Bankruptcy Risk

When a company cannot meet its debt obligations, it enters **bankruptcy**:

- **Chapter 11** — Restructuring. The company continues operating while reorganizing debt. Equity holders often lose most or all value.
- **Chapter 7** — Liquidation. The company shuts down and sells assets. Stockholders typically get nothing.

## Practical Tips

- Compare D/E ratios within the same industry, not across sectors
- Watch the interest coverage trend over time — is it improving or deteriorating?
- Check when debt matures — upcoming maturities in a tough economy are a red flag

> In StockSim, companies with excessive debt are at risk of insolvency during economic downturns. Before buying a stock, check its debt levels in the company details panel. Avoiding overleveraged companies can save your portfolio during recessions.`,
    relatedIds: ['revenue-earnings', 'fair-value', 'pe-ratio'],
    glossaryTerms: ['P/E Ratio', 'Fair Value'],
  },

  {
    id: 'market-cap',
    title: 'Market Capitalization',
    category: 'fundamental',
    summary:
      'Understanding company size categories and why market cap matters for your portfolio.',
    content: `## What Is Market Cap?

**Market capitalization** (market cap) is the total market value of a company's outstanding shares.

**Market Cap = Stock Price x Shares Outstanding**

If NVPH trades at $150 and has 100 million shares outstanding, its market cap is $15 billion.

Market cap tells you the **size** of a company as valued by the market. It is more meaningful than stock price alone — a $500 stock with 10 million shares ($5B) is a smaller company than a $50 stock with 500 million shares ($25B).

## Size Categories

- **Mega cap** — $200B+ (the largest companies in the world)
- **Large cap** — $10B - $200B (established, stable, household names)
- **Mid cap** — $2B - $10B (growing companies, balance of growth and stability)
- **Small cap** — $300M - $2B (younger companies, higher growth potential, higher risk)
- **Micro cap** — Under $300M (very small, often volatile, limited analyst coverage)

## Why Market Cap Matters

**Risk profile:**
- Large caps are generally **safer** — diversified businesses, strong balance sheets, can weather recessions
- Small caps are generally **riskier** — less diversified, may depend on one product, more vulnerable to competition

**Return potential:**
- Large caps grow steadily but rarely double in a year
- Small caps can deliver explosive returns (or devastating losses)

**Liquidity:**
- Large caps trade millions of shares daily — easy to buy and sell
- Small caps may trade thinly — harder to enter and exit positions without moving the price

## Index Weighting

Most major indexes are **market-cap weighted**, meaning larger companies have more influence on the index:

- If a $2 trillion company drops 5%, it moves the index much more than a $50 billion company dropping 5%
- This means index performance is often dominated by the top 5-10 largest stocks

## Market Cap vs Enterprise Value

**Enterprise Value (EV)** provides a more complete picture:

**EV = Market Cap + Total Debt - Cash**

A company with $10B market cap, $5B debt, and $2B cash has an EV of $13B. EV represents what it would cost to acquire the entire company, including taking on its debt.

## Practical Tips

- Diversify across market cap sizes — large caps for stability, small caps for growth
- Do not assume a low stock price means a small company (check shares outstanding)
- Be cautious with micro caps — low liquidity and limited information create higher risk

> In StockSim, companies span all market cap categories. Building a portfolio across different sizes gives you a natural hedge — when large caps stagnate, small caps might surge, and vice versa. Check the market cap filter to explore different tiers.`,
    relatedIds: ['pe-ratio', 'stocks-101', 'etfs'],
    glossaryTerms: ['Market Cap', 'Volume'],
  },

  {
    id: 'dividend-yield',
    title: 'Dividend Yield & Payout',
    category: 'fundamental',
    summary:
      'How to evaluate dividend sustainability, avoid yield traps, and build an income portfolio.',
    content: `## Dividend Yield Explained

**Dividend Yield = Annual Dividend Per Share / Stock Price**

If DENA pays $3.00 per year in dividends and trades at $75, its yield is 4.0%. This means for every $100 invested, you receive $4 in annual income.

Yield changes as the stock price moves:
- Stock drops from $75 to $60 (dividend unchanged) — yield rises to 5.0%
- Stock rises from $75 to $100 — yield falls to 3.0%

## The Yield Trap

A very high yield (7%+) is often a **warning sign**, not a gift. When a company's stock drops sharply, the yield automatically increases. A stock that was yielding 3% at $100 now yields 6% at $50 — but the price dropped for a reason. If earnings are falling, a **dividend cut** may be imminent, destroying both income and capital.

**Red flags for yield traps:**
- Yield significantly above sector average
- Declining revenue and earnings
- Payout ratio above 80% (see below)
- Rising debt to fund dividend payments

## Payout Ratio

**Payout Ratio = Dividends Per Share / Earnings Per Share**

This tells you what percentage of earnings the company is distributing as dividends.

- **Below 50%** — Sustainable. Plenty of room to maintain and grow the dividend.
- **50-75%** — Moderate. Healthy for mature companies.
- **Above 80%** — Stretched. Little room for earnings decline before the dividend is at risk.
- **Above 100%** — The company is paying out more than it earns. Unsustainable without cutting the dividend or borrowing.

## Dividend Growth

The **dividend growth rate** is often more important than the current yield. A stock yielding 2% but growing its dividend 10% per year will yield 5.2% on your original purchase price after 10 years — plus your shares have likely appreciated significantly.

Look for companies with:
- 5-10+ years of consecutive dividend increases
- Earnings growth that supports continued increases
- Moderate payout ratio (room to keep raising)

## Building a Dividend Portfolio

A well-constructed dividend portfolio provides:
- Regular cash income
- Compounding growth through reinvestment (DRIP)
- Lower volatility than growth stocks (dividend payers tend to be stable)
- Some inflation protection (dividends tend to grow over time)

## Tax Considerations

Dividends are taxed. **Qualified dividends** (held 60+ days) receive preferential tax rates. **Non-qualified dividends** are taxed as ordinary income at higher rates.

> In StockSim, use the dividend yield data to compare income-generating stocks. Try building a portfolio of reliable dividend payers and track the passive income it generates over time. Compare the total return (capital gains + dividends) against a growth-only strategy.`,
    relatedIds: ['dividends', 'pe-ratio', 'revenue-earnings'],
    glossaryTerms: ['Dividend Yield'],
  },

  {
    id: 'fair-value',
    title: 'Fair Value',
    category: 'fundamental',
    summary:
      'Estimating what a stock is really worth using DCF analysis, PE multiples, and margin of safety.',
    content: `## What Is Fair Value?

**Fair value** is an estimate of what a stock is truly worth based on fundamentals — as opposed to its current market price, which is driven by supply, demand, and sentiment.

If you calculate that NVPH's fair value is $170 and it currently trades at $150, you might consider it **undervalued**. If it trades at $200, it might be **overvalued**.

## Method 1: DCF (Discounted Cash Flow)

The DCF model values a company based on the money it will generate in the future, discounted back to today's value.

The basic idea:
1. Estimate the company's **free cash flow** for the next 5-10 years
2. Estimate a **terminal value** (what the company is worth at the end of that period)
3. **Discount** all future cash flows back to present value using a discount rate (typically 8-12%)

The sum of all discounted cash flows equals the intrinsic value of the company. Divide by shares outstanding to get fair value per share.

**Pros:** Theoretically sound, based on actual cash generation
**Cons:** Extremely sensitive to assumptions. Small changes in growth rate or discount rate produce wildly different results.

## Method 2: PE Multiple

A simpler approach:

1. Estimate next year's earnings per share (e.g., $6.00)
2. Choose a fair P/E multiple based on the sector and growth rate (e.g., 25x for a moderately growing tech company)
3. Fair value = $6.00 x 25 = **$150**

This method is quicker but depends on choosing the "right" multiple, which is subjective.

## Method 3: Comparable Analysis

Compare the stock's valuation metrics to similar companies:

- If peers trade at P/E of 20-25 and your stock trades at P/E of 15 with similar growth, it may be undervalued
- If your stock trades at P/E of 35 while peers are at 20, it might be overvalued — or the market sees superior growth potential

## Margin of Safety

Coined by Benjamin Graham (Warren Buffett's mentor), the **margin of safety** means only buying when the stock trades significantly below your estimated fair value.

If you estimate fair value at $150, a 25% margin of safety means you only buy at $112.50 or below. This buffer protects you against:
- Errors in your analysis
- Unexpected negative events
- General market declines

The more uncertain the estimate, the larger the margin of safety should be.

## Why Fair Value Is Not Exact

Fair value is always an **estimate**, not a fact. Two skilled analysts can look at the same company and arrive at different fair values based on different assumptions about growth, margins, and discount rates.

Think of fair value as a range ($140-$170) rather than a single number ($155).

## Practical Approach

- Use multiple methods and see if they converge
- Focus on whether the stock is clearly cheap or clearly expensive rather than trying to find an exact number
- Apply a margin of safety — you want the odds in your favor

> In StockSim, each company has a calculated fair value based on its fundamentals. Compare the market price to fair value to identify potential bargains. Remember: the market can stay "wrong" for a long time, but eventually price tends to converge toward fair value.`,
    relatedIds: ['pe-ratio', 'revenue-earnings', 'debt-analysis'],
    glossaryTerms: ['Fair Value', 'P/E Ratio'],
  },

  // ═══════════════════════════════════════════════════════════════
  // MARKET STRUCTURE
  // ═══════════════════════════════════════════════════════════════

  {
    id: 'market-participants',
    title: 'Market Participants',
    category: 'structure',
    summary: 'Who trades in the stock market and what motivates them.',
    content: `## The Players in the Market

The stock market is an ecosystem of different participants, each with different goals, timeframes, and strategies.

## Retail Traders

Individual investors trading with their own money. Typically smaller accounts ($1K-$1M). They have the advantage of flexibility — no one tells them what to buy or when to sell. Their disadvantage is limited information and resources compared to institutions.

## Institutional Investors

- **Mutual Funds** manage money for many investors. They must follow their stated strategy (e.g., "large-cap growth")
- **Hedge Funds** use sophisticated strategies including short selling, leverage, and derivatives. They aim for absolute returns regardless of market direction
- **Pension Funds** invest retirement money with very long time horizons (decades). They favor stable, dividend-paying stocks
- **Insurance Companies** need to match their investment returns to future claim payouts

## Market Makers

Firms that provide liquidity by always offering to buy and sell. They profit from the bid-ask spread. Without market makers, you might not find anyone to trade with when you want to buy or sell.

## High-Frequency Traders (HFT)

Algorithmic traders that execute thousands of trades per second. They profit from tiny price discrepancies and provide significant market liquidity, but are controversial for potentially disadvantaging slower participants.

## How They Interact

When you buy a stock, you're likely buying from a market maker. Behind that market maker, institutional investors are making larger moves that drive the overall trend. Your edge as a retail trader is patience — institutions must deploy capital quickly, but you can wait for the perfect opportunity.

> In StockSim, AI traders simulate institutional behavior — momentum traders, value investors, arbitrageurs, and more. Watch the market to spot their patterns.`,
    relatedIds: ['order-types', 'bid-ask-spread'],
    glossaryTerms: ['Market Maker', 'Institutional Investor'],
  },

  {
    id: 'orderbook',
    title: 'The Order Book',
    category: 'structure',
    summary: 'How buy and sell orders are matched and executed.',
    content: `## What Is the Order Book?

The order book is the list of all pending buy and sell orders for a security, organized by price. It's the core mechanism that determines stock prices.

## Structure

**Bid Side (Buyers):** Orders sorted highest price first. The top bid is the most someone is willing to pay right now.

**Ask Side (Sellers):** Orders sorted lowest price first. The top ask is the least someone is willing to accept right now.

The difference between the best bid and best ask is the **spread**.

## How Orders Get Matched

When you place a **market buy order**, it matches against the lowest ask price. If you buy 100 shares and the ask shows 50 at $100.00 and 200 at $100.05, you get 50 at $100.00 and 50 at $100.05.

A **limit order** at $99.50 sits on the bid side until someone is willing to sell at that price or lower.

## Depth and Liquidity

**Depth** refers to how many shares are available at each price level. A "deep" order book means large orders can execute without moving the price much. A "thin" order book means even small orders cause significant price movement.

## Price Impact

Large orders consume multiple price levels, causing **slippage** — you end up paying more (buying) or receiving less (selling) than the quoted price. This is why institutional traders break large orders into smaller pieces.

## Hidden Orders

Some exchanges allow **hidden orders** (also called iceberg orders) that don't appear in the visible order book. Only a small portion is displayed; the rest fills silently.

> In StockSim, the order book is simulated with realistic depth. Large orders experience slippage based on the Almgren-Chriss market impact model. Check the order book panel when trading to see current depth.`,
    relatedIds: ['bid-ask-spread', 'order-types', 'market-participants'],
    glossaryTerms: ['Order Book', 'Spread', 'Liquidity'],
  },

  {
    id: 'circuit-breakers',
    title: 'Circuit Breakers & Halts',
    category: 'structure',
    summary: 'Safety mechanisms that pause trading during extreme volatility.',
    content: `## Why Markets Pause

Circuit breakers are automatic trading pauses designed to prevent panic selling and give investors time to process information. They were introduced after the 1987 crash.

## Market-Wide Circuit Breakers (S&P 500)

Based on the S&P 500's decline from the previous close:

- **Level 1 (-7%):** 15-minute trading halt. Markets reopen after the pause
- **Level 2 (-13%):** 1-hour trading halt. More serious — institutional traders reassess
- **Level 3 (-20%):** Trading halted for the rest of the day. Extremely rare

These only trigger during regular hours. Level 1 and 2 can only trigger once per day.

## Individual Stock Halts

**LULD (Limit Up-Limit Down):** Individual stocks are halted for 5 minutes if the price moves more than a specified percentage from a reference price. For large-cap stocks, the band is typically 5%. For smaller stocks, 10-20%.

## Short Sale Restriction (SSR)

When a stock drops **10% or more** from the previous close, the **Alternative Uptick Rule** activates. Short sellers can only sell at the bid price + $0.01 (the "uptick"). This prevents aggressive short selling from accelerating a decline. SSR remains active for the rest of the day and the following trading day.

## Real-World Examples

- **March 2020:** Circuit breakers triggered 4 times in 2 weeks during COVID panic
- **May 2010:** Flash Crash saw individual stock halts across hundreds of names
- **January 2021:** GameStop triggered LULD halts dozens of times in a single day

> In StockSim, all three circuit breaker levels are implemented with real SEC thresholds. SSR is enforced — look for the SSR indicator on stocks that have dropped 10%+. When SSR is active, your short sell orders are automatically converted to limit orders at bid + $0.01.`,
    relatedIds: ['long-short', 'market-participants'],
    glossaryTerms: ['Circuit Breaker', 'Short Sale Restriction'],
  },

  {
    id: 'short-selling',
    title: 'Short Selling Deep Dive',
    category: 'structure',
    summary: 'The mechanics, risks, and regulation of betting against stocks.',
    content: `## How Short Selling Works

1. **Borrow** shares from your broker (they lend from other clients' accounts)
2. **Sell** the borrowed shares at the current market price
3. **Wait** for the price to drop
4. **Buy back** ("cover") the shares at the lower price
5. **Return** the shares to the lender

Your profit is the difference between sell price and buy price, minus borrowing fees.

## The Risks

**Unlimited loss potential.** When you buy a stock, you can lose at most 100% (it goes to $0). When you short, your loss is theoretically unlimited because there's no ceiling on how high a stock can go.

**Short squeeze.** If a heavily-shorted stock starts rising, short sellers rush to cover (buy back), which pushes the price even higher, forcing more shorts to cover — a vicious cycle.

**Margin calls.** Since you're borrowing, you must maintain margin. If the stock rises enough, your broker demands more cash or forces you to cover at a loss.

**Borrow costs.** "Hard to borrow" stocks can cost 20-100%+ annualized in borrowing fees. Easy-to-borrow stocks might cost 0.5-3%.

## Short Interest

**Short interest** is the total number of shares currently sold short. **Short interest ratio** (days to cover) = short interest / average daily volume. A ratio above 5 means it would take 5 days of average volume for all shorts to cover — a squeeze risk.

## Regulation

- **Regulation SHO** requires brokers to "locate" shares before allowing a short sale
- **SSR (Alternative Uptick Rule)** restricts short selling on stocks down 10%+
- **Short interest** must be reported to exchanges twice monthly

> In StockSim, you can short sell any stock. Watch for SSR indicators, monitor short interest in the fundamentals panel, and be prepared for short squeezes. The game simulates borrow costs and margin requirements realistically.`,
    relatedIds: ['long-short', 'margin-trading', 'circuit-breakers'],
    glossaryTerms: ['Short Selling', 'Short Squeeze', 'Margin Call'],
  },

  {
    id: 'regulation',
    title: 'Market Regulation',
    category: 'structure',
    summary: 'The SEC, insider trading rules, and how markets are policed.',
    content: `## Who Regulates the Markets?

**SEC (Securities and Exchange Commission):** The primary regulator of US securities markets. Created after the 1929 crash to protect investors and maintain fair markets.

**FINRA (Financial Industry Regulatory Authority):** Self-regulatory organization that oversees broker-dealers.

## Key Rules

**Insider Trading:** It's illegal to trade based on material, non-public information. This includes:
- Corporate insiders (executives, board members) trading before announcements
- Tipping others with inside information
- Trading on overheard confidential information

Penalties: Up to $5 million fine and 20 years in prison.

**Pattern Day Trader Rule:** If you execute 4+ day trades within 5 business days in a margin account, you're classified as a pattern day trader and must maintain $25,000 in equity.

**Wash Sale Rule:** If you sell a security at a loss and repurchase the same security within 30 days, the loss is disallowed for tax purposes. The disallowed loss gets added to the cost basis of the new position.

**Regulation Fair Disclosure (Reg FD):** Companies must disclose material information to all investors simultaneously — no selective disclosure to favored analysts.

## Market Manipulation

Illegal activities include:
- **Spoofing:** Placing large orders you intend to cancel to mislead others
- **Pump and dump:** Artificially inflating a stock price then selling
- **Wash trading:** Trading with yourself to create false volume
- **Front-running:** Executing your own trade before filling a client's order

## The SMA (Securities Market Authority)

> In StockSim, the SMA monitors your trading behavior. Suspicious patterns like rapid buying and selling, spoofing-like order placement, or trading around news events may trigger an investigation. If your SEC Scrutiny level gets too high, you may face trading restrictions.`,
    relatedIds: ['short-selling', 'market-participants'],
    glossaryTerms: ['SEC', 'Insider Trading', 'Pattern Day Trader'],
  },

  {
    id: 'after-hours',
    title: 'Pre-Market & After-Hours Trading',
    category: 'structure',
    summary: 'Trading outside regular market hours and what it means.',
    content: `## Market Hours

**Regular Trading Hours:** 9:30 AM - 4:00 PM Eastern Time, Monday-Friday (excluding holidays).

**Pre-Market:** 4:00 AM - 9:30 AM ET. Lower volume, wider spreads.

**After-Hours:** 4:00 PM - 8:00 PM ET. Similar to pre-market conditions.

## Why Extended Hours Exist

Earnings reports are typically released before market open or after market close. Extended hours trading lets investors react to these announcements immediately rather than waiting for the next regular session.

## Key Differences from Regular Hours

- **Lower liquidity:** Fewer participants means wider bid-ask spreads
- **Higher volatility:** Individual trades have more price impact
- **Limit orders only:** Most brokers require limit orders in extended hours
- **Gaps:** Price can move significantly between sessions

## The Overnight Gap

When the market closes at 4:00 PM and reopens at 9:30 AM, 17.5 hours pass. News, global events, and overseas markets continue operating. This creates **gaps** — the opening price may be significantly different from the previous close.

**Gap up:** Opens higher than previous close (positive overnight news)
**Gap down:** Opens lower than previous close (negative overnight news)

Experienced traders watch for gap fills — the tendency for prices to "fill" the gap by returning to the previous close level.

> In StockSim, the market follows regular trading hours with overnight gaps based on after-hours events and news. Major earnings announcements and economic data releases can cause significant gaps at market open.`,
    relatedIds: ['circuit-breakers', 'revenue-earnings'],
    glossaryTerms: ['Pre-Market', 'After-Hours'],
  },

  {
    id: 'dark-pools',
    title: 'Dark Pools & Alternative Venues',
    category: 'structure',
    summary: 'Hidden trading venues where large orders execute away from public exchanges.',
    content: `## What Are Dark Pools?

Dark pools are private trading venues where buy and sell orders are not publicly visible before execution. They were created so institutional investors could trade large blocks of shares without revealing their intentions to the market.

## Why They Exist

Imagine a pension fund needs to sell 5 million shares of a stock. If they placed this order on a public exchange, other traders would see the massive sell order and front-run it — selling their own shares first, pushing the price down before the pension fund can execute.

In a dark pool, the order is hidden. The pension fund gets a better average price because the market doesn't react to their order.

## How They Work

Dark pools match buyers and sellers internally, typically at or near the midpoint of the public bid-ask spread. This means both sides get a slightly better price than they would on a public exchange.

## Controversy

- **Lack of transparency:** About 40% of US equity volume now trades in dark pools or via internalization
- **Price discovery:** If too much volume moves off public exchanges, the visible order book becomes less representative of true supply and demand
- **Fairness concerns:** Retail orders may be systematically routed to internalizers rather than public exchanges

## Types of Dark Venues

- **Independent dark pools** (e.g., Liquidnet, IEX)
- **Broker-dealer dark pools** (operated by banks)
- **Internalization** — brokers fill orders against their own inventory

> In StockSim, the market simulates institutional order flow including large block trades that don't appear in the visible order book. AI traders use strategies that mimic real institutional behavior, including breaking large orders into smaller pieces to minimize market impact.`,
    relatedIds: ['orderbook', 'market-participants'],
    glossaryTerms: ['Dark Pool', 'Liquidity'],
  },

  // ═══════════════════════════════════════════════════════════════
  // STRATEGY & PSYCHOLOGY
  // ═══════════════════════════════════════════════════════════════

  {
    id: 'buy-hold-vs-active',
    title: 'Buy & Hold vs Active Trading',
    category: 'strategy',
    summary: 'Two fundamentally different approaches to the market.',
    content: `## Buy & Hold

The simplest strategy: buy good companies (or index funds) and hold them for years or decades. Don't try to time the market.

**Advantages:**
- Historically beats most active traders
- Lower transaction costs and taxes
- Less time-consuming and stressful
- Benefits from compound growth

**Famous advocate:** Warren Buffett — "Our favorite holding period is forever."

## Active Trading

Attempting to profit from short-term price movements through frequent buying and selling.

**Advantages:**
- Potential for higher short-term returns
- Can profit in both rising and falling markets
- More engaging and intellectually stimulating

**Disadvantages:**
- Most active traders underperform buy & hold
- Higher transaction costs eat into returns
- Short-term gains taxed at higher rates
- Emotionally demanding

## The Data

Studies consistently show that 80-90% of active fund managers underperform their benchmark index over 10+ year periods. Individual active traders fare even worse.

## The Middle Ground

Many successful investors combine both approaches:
- Core portfolio: 70-80% in buy & hold positions
- Trading portfolio: 20-30% for active strategies
- Rebalance periodically rather than trading constantly

> In StockSim, you can practice both styles. Try running a scenario with pure buy & hold (buy diversified, set to maximum speed) and compare your returns to an active trading approach. The results might surprise you.`,
    relatedIds: ['risk-management', 'diversification'],
    glossaryTerms: ['Buy and Hold', 'Active Trading'],
  },

  {
    id: 'value-growth-momentum',
    title: 'Value, Growth & Momentum',
    category: 'strategy',
    summary: 'Three major investment styles and when each works best.',
    content: `## Value Investing

Buying stocks that trade below their intrinsic value. Value investors look for low P/E ratios, high dividend yields, and prices below book value.

**Key metrics:** P/E ratio, P/B ratio, dividend yield, free cash flow
**Risk:** Value traps — stocks that are cheap for a good reason
**Famous practitioners:** Warren Buffett, Benjamin Graham, Seth Klarman

## Growth Investing

Buying companies with above-average revenue and earnings growth, regardless of current valuation. Growth investors pay premium prices for companies that are growing fast.

**Key metrics:** Revenue growth rate, EPS growth, total addressable market
**Risk:** High valuations leave no margin for error — missing earnings estimates causes severe drops
**Famous practitioners:** Peter Lynch, Philip Fisher, Cathie Wood

## Momentum Investing

Buying stocks that have been going up and selling those that have been going down. Based on the empirical observation that trends persist.

**Key metrics:** Price relative to 50/200-day moving average, RSI, relative strength
**Risk:** Momentum crashes — sudden reversals can be devastating
**Famous practitioners:** William O'Neil (CANSLIM), trend followers

## Which Works Best?

All three styles go through cycles:
- **Value** outperforms during recoveries and inflationary periods
- **Growth** outperforms during economic expansions and low-rate environments
- **Momentum** works in trending markets but fails in choppy conditions

The best approach may be combining elements of all three.

> In StockSim, you can identify value stocks by comparing price to fair value, growth stocks by revenue growth in fundamentals, and momentum plays by watching the top gainers list and technical indicators.`,
    relatedIds: ['pe-ratio', 'moving-averages', 'fair-value'],
    glossaryTerms: ['Value Investing', 'Growth Stock'],
  },

  {
    id: 'risk-management',
    title: 'Risk Management',
    category: 'strategy',
    summary: 'How to protect your capital and survive bad trades.',
    content: `## The #1 Rule of Trading

"Don't lose money" sounds obvious, but it's the most important principle. A 50% loss requires a 100% gain just to break even. Protecting capital is more important than maximizing gains.

## Position Sizing

Never risk more than 1-2% of your total portfolio on a single trade. If you have $100,000:
- Maximum risk per trade: $1,000-$2,000
- If your stop loss is 5% below entry, position size = $20,000-$40,000

## Stop Losses

A stop loss order automatically sells when the price drops to your specified level. Types:
- **Fixed stop:** Set at a specific price (e.g., -5% from entry)
- **Trailing stop:** Moves up with the price, locks in profits
- **Mental stop:** A price level where you commit to selling (less reliable)

## Risk-Reward Ratio

Before entering a trade, calculate the potential reward vs risk. A 3:1 ratio means you expect to make $3 for every $1 you risk. With a 3:1 ratio, you only need to be right 25% of the time to break even.

## Correlation Risk

Owning 10 tech stocks isn't diversification — they'll all drop together in a tech selloff. True risk management means owning uncorrelated assets.

## The Kelly Criterion

A mathematical formula for optimal position sizing: bet size = (win probability × win size - loss probability × loss size) / win size. Most traders use a fraction (quarter or half Kelly) because the formula assumes perfect probability estimates.

> In StockSim, use stop loss orders to protect positions, watch the portfolio concentration warning if one stock exceeds 50% of your portfolio, and diversify across sectors. The game will show you exactly how drawdowns compound.`,
    relatedIds: ['order-types', 'diversification'],
    glossaryTerms: ['Stop Loss', 'Risk Management'],
  },

  {
    id: 'trading-psychology',
    title: 'Trading Psychology',
    category: 'strategy',
    summary: 'The emotional biases that cause traders to make bad decisions.',
    content: `## Your Brain vs the Market

The human brain evolved for survival, not for rational financial decision-making. Cognitive biases that kept our ancestors alive now cause us to buy high and sell low.

## Key Biases

**Loss aversion:** Losses feel roughly 2x more painful than equivalent gains feel good. This causes traders to hold losers too long (hoping they'll recover) and sell winners too quickly (locking in the good feeling).

**Confirmation bias:** We seek information that confirms our existing beliefs. If you're bullish on a stock, you'll unconsciously filter out negative news and amplify positive news.

**Anchoring:** We fixate on reference points. "I bought at $100, so I won't sell below $100" — even if the stock's fundamentals have changed and $100 is no longer rational.

**Recency bias:** We overweight recent events. After a market crash, we expect another crash. During a bull run, we expect gains to continue forever.

**FOMO (Fear of Missing Out):** Seeing others profit makes us chase trades we wouldn't normally take. This is how bubbles form — and how retail traders buy at the top.

**Overconfidence:** After a few winning trades, we increase position sizes and take on more risk, right before our luck runs out.

## How to Manage

1. **Have a plan before you trade.** Write down entry, exit, and stop loss BEFORE buying
2. **Keep a trading journal.** Review what you did right and wrong
3. **Size positions conservatively.** Emotional decisions hurt more with large positions
4. **Take breaks.** After a big win or loss, step away

> In StockSim, you can observe these biases in yourself in real-time. Notice when you hold a losing position too long or sell a winner too early. The game is a safe space to develop emotional discipline.`,
    relatedIds: ['risk-management', 'buy-hold-vs-active'],
    glossaryTerms: ['FOMO'],
  },

  {
    id: 'diversification',
    title: 'Diversification',
    category: 'strategy',
    summary: 'Why not putting all your eggs in one basket actually works.',
    content: `## The Only Free Lunch in Finance

Harry Markowitz called diversification "the only free lunch in investing." By combining assets that don't move in perfect lockstep, you can reduce risk without proportionally reducing expected return.

## How It Works

If you own one stock, you're exposed to both market risk and company-specific risk. By owning 20-30 stocks across different sectors, you eliminate most company-specific risk. One company going bankrupt hurts, but it's 3-5% of your portfolio, not 100%.

## What Real Diversification Looks Like

**Not diversified:** 10 tech stocks (they'll all drop together)
**Better:** Stocks across all sectors (tech, healthcare, energy, financials...)
**Best:** Stocks + bonds + commodities + international markets

## The Diminishing Returns

- 1 stock: Very high risk
- 5 stocks: Significant risk reduction
- 15 stocks: Most company-specific risk eliminated
- 30 stocks: Nearly all diversifiable risk eliminated
- 100+ stocks: Almost identical to owning an index fund

## Correlation Matters

During market stress, correlations increase — everything drops together. This is exactly when you need diversification most, and exactly when it works least. This is why some investors add truly uncorrelated assets like gold, treasury bonds, or cash.

## The Sector Approach

A simple diversification strategy: allocate roughly equally across major sectors. If one sector crashes (like energy in 2020), others may hold up or even benefit.

> In StockSim, you can track your portfolio allocation by sector in the Portfolio tab. If you see one sector dominating, consider rebalancing. The concentration warning will alert you if one position exceeds 50% of your portfolio.`,
    relatedIds: ['risk-management', 'etfs'],
    glossaryTerms: ['Diversification', 'Portfolio'],
  },

  {
    id: 'earnings-trading',
    title: 'Trading Around Earnings',
    category: 'strategy',
    summary: 'How to approach the most volatile events in a stock\'s life.',
    content: `## Earnings Season

Public companies report quarterly earnings (revenue, EPS, guidance). These reports are the most significant single-day events for individual stocks. Price moves of 5-20% in a single session are common.

## The Earnings Playbook

**Before earnings:**
- Implied volatility (IV) rises as traders buy options for protection or speculation
- The stock often drifts in the direction of consensus expectations
- Analyst estimates set the benchmark — the actual number matters less than the **surprise**

**After earnings:**
- The stock moves based on: actual vs expected, guidance, management tone
- IV crushes (drops sharply) — options lose value even if the stock moves
- Post-Earnings Announcement Drift (PEAD): stocks tend to continue moving in the direction of the initial reaction for days or weeks

## Strategies

**Earnings straddle:** Buy a call and put before earnings, profit from a large move in either direction. Risk: IV crush may offset the move.

**Post-earnings momentum:** Wait for the reaction, then trade in that direction for the drift.

**Sell the news:** If a stock runs up into earnings, consider selling regardless of the result — expectations may be priced in.

**Avoid entirely:** Many successful traders simply don't hold through earnings. The outcome is essentially a coin flip for directional traders.

## Key Metrics to Watch

- **EPS vs estimate:** Beat or miss?
- **Revenue vs estimate:** Top-line growth matters
- **Guidance:** Forward-looking statements often matter more than past results
- **Margins:** Are they expanding or contracting?

> In StockSim, check the Earnings Calendar on the Dashboard to see upcoming reports. Earnings cause realistic price reactions, IV crush in options, and post-announcement drift. Practice different earnings strategies to find what works for you.`,
    relatedIds: ['revenue-earnings', 'pe-ratio', 'bollinger-bands'],
    glossaryTerms: ['Earnings', 'Implied Volatility'],
  },

  {
    id: 'sector-rotation',
    title: 'Sector Rotation',
    category: 'strategy',
    summary: 'How money flows between sectors during different economic phases.',
    content: `## The Business Cycle

The economy moves through predictable phases, and different sectors outperform during each:

**Early Recovery:** Consumer Discretionary, Financials, Real Estate
**Mid Expansion:** Technology, Industrials, Materials
**Late Expansion:** Energy, Healthcare
**Recession:** Utilities, Consumer Staples, Healthcare

## Why It Happens

- **Interest rates** affect sectors differently. Low rates boost real estate and tech; high rates benefit banks
- **Consumer confidence** drives discretionary spending (luxury, travel, retail)
- **Inflation** benefits commodity producers (energy, materials) but hurts growth stocks
- **Defensive sectors** (utilities, staples) provide steady dividends regardless of economic conditions

## How to Spot Rotation

Watch for:
- Money flowing OUT of high-growth sectors INTO defensive ones (risk-off)
- Money flowing OUT of bonds INTO stocks (risk-on)
- Sector ETF relative performance diverging from the market index
- Economic indicators (PMI, consumer confidence, unemployment) shifting

## Practical Application

You don't need to predict the exact cycle phase. Instead:
1. Monitor which sectors are leading and lagging
2. Overweight leading sectors, underweight lagging ones
3. When leaders start to falter, the cycle may be shifting

> In StockSim, the economic engine drives realistic sector rotation. Watch the Market Phase indicator (Bull/Bear/Neutral), VIX gauge, and sector performance heatmap to identify rotation opportunities. Interest rate changes from the Fed directly impact sector performance.`,
    relatedIds: ['market-cap', 'etfs', 'diversification'],
    glossaryTerms: ['Sector Rotation', 'Business Cycle'],
  },

  {
    id: 'survivorship-bias',
    title: 'Survivorship Bias & Common Traps',
    category: 'strategy',
    summary: 'The hidden statistical tricks that make investing look easier than it is.',
    content: `## Survivorship Bias

When we study "successful companies" or "winning strategies," we only look at the survivors. We never see the thousands of companies that went bankrupt or strategies that failed.

**Example:** "If you had invested $10,000 in Amazon in 1997, you'd have $20 million today." True — but for every Amazon, there were hundreds of dot-com stocks that went to zero. You had no way to know Amazon would survive.

## Common Investing Traps

**Dividend trap:** A stock with 15% yield seems amazing until you realize the company is cutting its dividend because it can't afford it. The yield is high because the price has cratered.

**Value trap:** A stock with a P/E of 5 looks cheap until you realize earnings are declining rapidly. Next year's P/E might be 50.

**Past performance:** "This fund returned 25% annually for 5 years!" Funds that perform well attract attention; funds that perform poorly are quietly closed. The average fund return BEFORE survivorship bias is much lower.

**Backtesting bias:** Any strategy looks amazing when tested on historical data — you already know what happened. Real-time performance is always worse.

## How to Protect Yourself

- Ask "What could go wrong?" before every trade
- Study failures as much as successes
- Be skeptical of strategies that claim consistent outperformance
- Understand that past performance genuinely does not predict future results
- Keep position sizes small enough that any single failure is survivable

> In StockSim, companies can go bankrupt and get delisted. Dividend yields can be traps. This gives you a realistic experience of the risks that survivorship bias hides in historical analysis.`,
    relatedIds: ['risk-management', 'dividend-yield', 'trading-psychology'],
    glossaryTerms: ['Survivorship Bias'],
  },

  {
    id: 'options-strategies',
    title: 'Basic Options Strategies',
    category: 'strategy',
    summary: 'How to use calls and puts for speculation and hedging.',
    content: `## What Are Options?

Options give you the **right, but not the obligation** to buy (call) or sell (put) a stock at a specific price (strike) before a specific date (expiration).

## Buying Calls (Bullish)

You think NVPH will rise from $100 to $120. Instead of buying 100 shares ($10,000), you buy a $105 call for $3.00 ($300 total).

- **If right:** Stock goes to $120 → option worth $15 → $1,200 profit (400% return)
- **If wrong:** Stock stays below $105 → option expires worthless → lose $300

Leverage amplifies both gains and losses.

## Buying Puts (Bearish/Hedging)

You own NVPH at $100 and want protection. You buy a $95 put for $2.00.

- **Stock drops to $80:** Your shares lose $2,000, but your put gains $1,300 → net loss only $700
- **Stock stays above $95:** Put expires worthless, you lose $200 premium (insurance cost)

## Covered Calls (Income)

You own 100 shares at $100. You sell a $110 call for $2.00.

- **Stock stays below $110:** Keep the $200 premium as income
- **Stock goes above $110:** Shares get called away at $110 + you keep premium. You miss upside above $112

## The Greeks

- **Delta:** How much the option price moves per $1 stock move
- **Theta:** How much value the option loses per day (time decay)
- **Vega:** How much the option price changes with volatility changes
- **Gamma:** How fast delta changes

## Key Concepts

- **IV Crush:** Implied volatility drops after earnings → options lose value even if the stock moves
- **Time Decay:** Options lose value every day, accelerating near expiration
- **Intrinsic vs Extrinsic:** Intrinsic = real value (in-the-money amount), Extrinsic = time + volatility premium

> In StockSim, you can trade calls and puts with realistic Black-Scholes pricing, Greeks, and IV dynamics. Start with simple strategies (buying calls/puts) before moving to more complex ones.`,
    relatedIds: ['long-short', 'earnings-trading', 'risk-management'],
    glossaryTerms: ['Call Option', 'Put Option', 'Implied Volatility'],
  },

  {
    id: 'market-timing',
    title: 'Market Timing — Does It Work?',
    category: 'strategy',
    summary: 'The evidence for and against trying to predict market direction.',
    content: `## The Case Against

"Time in the market beats timing the market." Data supports this:

- Missing the 10 best days over 20 years cuts your returns by more than half
- The best days often happen right after the worst days (during peak panic)
- No one consistently predicts both when to sell AND when to buy back in

## The Case For

- Valuations matter: the S&P 500's 10-year return correlates with starting P/E ratio
- Technical indicators like the 200-day moving average have shown some predictive power
- Risk management sometimes requires reducing exposure

## The Compromise

Instead of binary "all in" or "all out," adjust your allocation:
- **Very expensive market:** Reduce equity allocation by 10-20%, hold more cash
- **Average valuation:** Normal allocation
- **Very cheap market (after crash):** Increase allocation, deploy cash

This "dynamic asset allocation" captures some timing benefit without the binary risk of being wrong.

## What Actually Works

- **Dollar cost averaging:** Invest the same amount regularly regardless of price. You automatically buy more shares when prices are low
- **Rebalancing:** When stocks outperform, sell some to buy bonds (or vice versa). This forces you to "sell high, buy low"
- **Staying invested:** The biggest risk isn't buying at the wrong time — it's not being invested at all

> In StockSim, you can test timing strategies by pausing at different market conditions. Compare your returns to a simple buy-and-hold approach over the same period. The results are educational.`,
    relatedIds: ['buy-hold-vs-active', 'moving-averages', 'risk-management'],
    glossaryTerms: ['Dollar Cost Averaging'],
  },

  // ═══════════════════════════════════════════════════════════════
  // FAMOUS PEOPLE & EVENTS
  // ═══════════════════════════════════════════════════════════════

  {
    id: 'jesse-livermore',
    title: 'Jesse Livermore',
    category: 'famous',
    summary: 'The "Boy Plunger" who made and lost fortunes in the early 1900s.',
    content: `## The Greatest Trader Who Ever Lived?

Jesse Livermore (1877-1940) started trading at 14 in bucket shops. By his 30s, he was one of the richest men in America. He made $100 million shorting the 1929 crash — equivalent to $1.5 billion today.

## Key Lessons

**"The market is never wrong — opinions often are."** Livermore believed in reading the tape (price action) rather than listening to tips or opinions.

**"It was never my thinking that made big money. It was always my sitting."** His biggest profits came from holding positions through major trends, not from frequent trading.

**"There is nothing new in Wall Street. There can't be because speculation is as old as the hills."** Markets are driven by human nature — fear and greed — which never changes.

## His Trading Rules

1. Trade with the trend, never against it
2. Wait for confirmation before entering
3. Cut losses quickly, let profits run
4. Don't average down on losing positions
5. Never act on tips — do your own analysis

## The Tragedy

Despite his brilliance, Livermore went bankrupt multiple times. He couldn't manage risk and was prone to overconfidence after big wins. He tragically took his own life in 1940. His story is a powerful reminder that market skill without risk management leads to ruin.

## Legacy

His book "Reminiscences of a Stock Operator" (1923, written by Edwin Lefevre) is considered the greatest trading book ever written. Every concept in modern technical analysis traces back to principles Livermore discovered through experience.`,
    relatedIds: ['trading-psychology', 'risk-management'],
  },

  {
    id: 'warren-buffett',
    title: 'Warren Buffett',
    category: 'famous',
    summary: 'The Oracle of Omaha — the most successful investor in history.',
    content: `## The Compounding Machine

Warren Buffett (b. 1930) turned a $10,000 investment in 1956 into over $100 billion. His company Berkshire Hathaway has compounded at ~20% annually for 60 years — the longest sustained outperformance in market history.

## Investment Philosophy

**"Be fearful when others are greedy, and greedy when others are fearful."**

Buffett is a value investor who buys wonderful companies at fair prices (evolved from his mentor Benjamin Graham's approach of buying mediocre companies at wonderful prices).

## Key Principles

1. **Circle of competence:** Only invest in businesses you understand
2. **Moat:** Look for durable competitive advantages (brand, switching costs, network effects)
3. **Management quality:** Invest with honest, competent leaders
4. **Margin of safety:** Buy below intrinsic value to protect against errors
5. **Long-term thinking:** "Our favorite holding period is forever"

## Famous Investments

- **Coca-Cola (1988):** $1.3B investment now worth $25B+ with dividends
- **Apple (2016):** Became Berkshire's largest holding, worth over $150B
- **GEICO, See's Candies, BNSF Railroad** — bought entire companies

## What Makes Him Different

Buffett's edge isn't superior intelligence — it's temperament. He's patient enough to wait years for the right opportunity and disciplined enough to pass on thousands of "good" deals to wait for great ones.

His annual shareholder letters are free, educational masterpieces on investing, business, and life.`,
    relatedIds: ['value-growth-momentum', 'fair-value', 'buy-hold-vs-active'],
  },

  {
    id: 'george-soros',
    title: 'George Soros',
    category: 'famous',
    summary: 'The man who broke the Bank of England.',
    content: `## The Quantum Fund

George Soros (b. 1930) founded Quantum Fund in 1969. Over 30 years, it averaged 30% annual returns, making Soros one of the most successful macro traders in history.

## Breaking the Bank of England (1992)

Britain was part of the European Exchange Rate Mechanism (ERM), which required keeping the pound within a narrow band against the German mark. Soros identified that Britain's economy was too weak to maintain this peg.

He built a massive $10 billion short position against the pound. On September 16, 1992 ("Black Wednesday"), the Bank of England was forced to withdraw from the ERM, and the pound collapsed. Soros made approximately $1 billion in a single day.

## Reflexivity Theory

Soros developed the theory of **reflexivity** — the idea that market participants' biases can actually change the fundamentals they're trying to predict. This creates feedback loops:

1. Investors believe a stock will rise → they buy → price rises → more investors believe → more buying
2. Eventually, the disconnect between perception and reality becomes unsustainable → crash

This explains bubbles and crashes better than efficient market theory.

## Trading Approach

- **Top-down macro:** Focus on big-picture economic imbalances
- **Asymmetric bets:** Structure trades with limited downside and massive upside
- **Conviction:** When you're right, go big. "It's not whether you're right or wrong, it's how much you make when you're right."

## Legacy

Soros demonstrated that individual traders can move markets, that macroeconomic analysis has real trading value, and that central banks are not invincible.`,
    relatedIds: ['trading-psychology', 'sector-rotation'],
  },

  {
    id: 'michael-burry',
    title: 'Michael Burry & The Big Short',
    category: 'famous',
    summary: 'The investor who saw the 2008 financial crisis coming.',
    content: `## Seeing What Nobody Else Saw

Dr. Michael Burry, a neurologist turned hedge fund manager, identified the US housing bubble in 2005 — two years before it collapsed. He bet against subprime mortgage bonds through credit default swaps, eventually earning $725 million for his fund and $100 million personally.

## How He Did It

1. **Deep research:** Burry actually read individual mortgage loan documents — thousands of them. He found that many loans were given to borrowers who couldn't possibly repay
2. **Created the instrument:** Credit default swaps on mortgage bonds didn't exist. He convinced Goldman Sachs and other banks to create them
3. **Endured years of pain:** From 2005-2007, his fund bled money paying premiums on the swaps. Investors demanded he close the positions. He refused
4. **Conviction:** When everyone told him he was wrong, including his own investors and the rating agencies, he held firm

## Key Lessons

**Independent thinking:** The biggest opportunities come from disagreeing with the consensus — but you need to be right AND patient.

**Asymmetric risk-reward:** His potential loss was limited to the premium payments. His potential gain was enormous if the housing market collapsed.

**The pain of being early:** Being right but early is functionally the same as being wrong — at least for a while. You need the capital and conviction to survive.

## After 2008

Burry continued his contrarian approach, making notable bets on water scarcity, GameStop (before the 2021 squeeze), and various macro positions. His story was immortalized in the book and movie "The Big Short."`,
    relatedIds: ['risk-management', 'debt-analysis'],
  },

  {
    id: 'crash-1929',
    title: 'The Crash of 1929',
    category: 'famous',
    summary: 'The crash that launched the Great Depression and modern market regulation.',
    content: `## The Roaring Twenties

Throughout the 1920s, the US stock market rose roughly 500%. Ordinary people borrowed heavily to invest (margin requirements were only 10% — you could buy $1,000 of stock with just $100). Everyone believed stocks only go up.

## The Crash

**Black Thursday (October 24, 1929):** Market dropped 11% at the open. Bankers pooled money to stabilize prices — it worked temporarily.

**Black Monday (October 28):** Market fell 13%. No rescue this time.

**Black Tuesday (October 29):** Market fell another 12%. Total panic. 16 million shares traded — a record that stood for decades.

## The Aftermath

The market didn't bottom until July 1932 — down **89%** from the peak. It took until **1954** (25 years!) for the market to recover to its 1929 high.

The crash triggered the Great Depression: banks failed, unemployment reached 25%, and millions lost their life savings.

## What Changed

The crash led to fundamental reforms:
- **SEC created (1934):** Federal regulation of securities markets
- **Glass-Steagall Act:** Separated commercial and investment banking
- **Margin requirements raised:** From 10% to 50% (Regulation T)
- **FDIC created:** Insured bank deposits
- **Securities Act of 1933:** Required companies to disclose financial information

## Lessons

The 1929 crash demonstrated that excessive leverage, speculation, and lack of regulation create systemic risk. Every subsequent bubble has echoed these same patterns — only the specific instruments change.`,
    relatedIds: ['regulation', 'margin-trading', 'circuit-breakers'],
  },

  {
    id: 'enron',
    title: 'Enron — The Fraud That Changed Everything',
    category: 'famous',
    summary: 'How America\'s 7th-largest company turned out to be a house of cards.',
    content: `## Rise

Enron was an energy company that transformed itself into a trading powerhouse in the 1990s. Named "America's Most Innovative Company" by Fortune magazine for six consecutive years. Its stock rose from $20 to $90 between 1997 and 2000.

## The Fraud

Enron used a web of **special purpose entities** (SPEs) to hide billions in debt and inflate profits. Key schemes:

- **Mark-to-market accounting:** Booked projected future profits as current revenue
- **Hidden debt:** Used off-balance-sheet entities to hide liabilities
- **Fake revenue:** Created "trades" between its own subsidiaries to inflate volume
- **Executive enrichment:** Insiders sold hundreds of millions in stock while telling employees to buy

## The Collapse

**October 2001:** Enron disclosed $618 million loss and $1.2 billion reduction in equity. Stock dropped from $33 to $1 in weeks.

**December 2001:** Filed for bankruptcy — then the largest in US history.

**The human cost:** 20,000 employees lost their jobs. Many had their entire retirement savings in Enron stock (which the company encouraged). Shareholders lost $74 billion.

## What Changed

- **Sarbanes-Oxley Act (2002):** Required CEO/CFO to personally certify financial statements, criminal penalties for fraud, independent audit committees
- **Arthur Andersen** (Enron's auditor, one of the "Big Five") was convicted of obstruction and ceased to exist
- The phrase "cooking the books" entered mainstream vocabulary

## Lesson for Traders

If something looks too good to be true, it probably is. Rapid revenue growth with opaque accounting, insider selling while promoting the stock, and aggressive mark-to-market accounting are red flags.`,
    relatedIds: ['revenue-earnings', 'regulation', 'survivorship-bias'],
  },

  {
    id: 'gamestop-2021',
    title: 'GameStop & The Meme Stock Revolution',
    category: 'famous',
    summary: 'How Reddit retail traders took on Wall Street hedge funds — and won.',
    content: `## The Setup

In late 2020, GameStop (GME) was a struggling brick-and-mortar game retailer trading around $4. Hedge funds like Melvin Capital had massive short positions — short interest exceeded 140% of the float (more shares sold short than actually existed).

Keith Gill ("Roaring Kitty" / "DeepF***ingValue") posted detailed analysis on Reddit's r/WallStreetBets showing GME was undervalued with massive short squeeze potential.

## The Squeeze

**January 2021:** Reddit retail traders coordinated buying, pushing GME from $17 to $483 in two weeks. Short sellers were forced to cover, buying shares and pushing the price even higher.

**Melvin Capital** lost 53% in January and eventually shut down entirely. **Citron Research** stopped publishing short reports.

## The Controversy

**Robinhood restricted buying** of GME on January 28, allowing only sells. The stock crashed. Congressional hearings followed.

The debate: Was this a legitimate short squeeze by informed retail traders, or a coordinated pump-and-dump? Was Robinhood protecting customers from risk, or protecting its own clearing obligations?

## Key Takeaways

1. **Short interest above 100% is extremely dangerous** for shorts — there literally aren't enough shares to cover
2. **Social media changed markets** — retail traders can now coordinate at scale
3. **Payment for order flow** became a mainstream concern (Robinhood sells its orders to market makers like Citadel)
4. **The little guy CAN win** — but most retail traders who bought GME late lost money

## Legacy

GME spawned "meme stocks" (AMC, BBBY, etc.) and brought millions of new retail investors into the market. It exposed structural issues in market plumbing and democratized financial knowledge — for better and worse.`,
    relatedIds: ['short-selling', 'market-participants', 'trading-psychology'],
  },

  {
    id: 'ltcm',
    title: 'LTCM — When Geniuses Failed',
    category: 'famous',
    summary: 'A Nobel Prize-winning hedge fund that nearly destroyed the financial system.',
    content: `## The Dream Team

Long-Term Capital Management was founded in 1994 by John Meriwether (legendary Salomon Brothers trader) with partners including **two Nobel Prize winners** in economics (Myron Scholes and Robert Merton — who helped create the Black-Scholes options pricing model).

The fund used sophisticated mathematical models to find small pricing discrepancies and used massive leverage (25:1 to 50:1) to amplify tiny profits.

## The Returns

1994-1997: Returns of 21%, 43%, 41%, and 17% after fees. The fund managed $4.7 billion with $125 billion in positions and over $1 trillion in derivatives exposure.

## The Blow-Up

**August 1998:** Russia defaulted on its debt. Global markets panicked. The "small" price discrepancies LTCM was betting on suddenly became enormous.

In one month, LTCM lost $4.6 billion — essentially all its capital. The problem: their models assumed that extreme events were incredibly rare. In reality, they happened.

## The Bailout

The Federal Reserve organized a bailout — not with taxpayer money, but by convincing 14 major banks to inject $3.6 billion to prevent LTCM's collapse from triggering a systemic crisis. An LTCM bankruptcy would have forced the liquidation of hundreds of billions in positions, crashing markets worldwide.

## Lessons

1. **Leverage kills.** Even with the best models, high leverage turns small losses into existential ones
2. **Models are not reality.** Mathematical models assume normal distributions; real markets have fat tails
3. **Liquidity vanishes when you need it most.** Everyone tries to sell at the same time
4. **"Too smart to fail" doesn't exist.** Two Nobel Prize winners couldn't prevent disaster
5. **Correlation increases in crises.** Assets that seem uncorrelated become highly correlated during panics

> In StockSim, the Black-Scholes model (the same one that helped create LTCM) powers options pricing. It's an excellent model — but remember its limitations. Markets can behave in ways no model anticipates.`,
    relatedIds: ['options-strategies', 'risk-management', 'diversification'],
  },

  // ═══════════════════════════════════════════════════════════════
  // STOCKSIM GUIDE
  // ═══════════════════════════════════════════════════════════════

  {
    id: 'guide-beginner',
    title: 'Getting Started — Beginner Guide',
    category: 'guide',
    summary: 'Your first steps in StockSim.',
    content: `## Welcome to StockSim

StockSim is a realistic stock market simulator. You start with $100,000 in cash and can trade stocks, options, and navigate market events just like in real financial markets.

## Your First Game

1. **Start a new game** on "Normal" difficulty
2. **Explore the Dashboard** — see market overview, top movers, developing stories
3. **Click a stock** in the watchlist to see its chart and details
4. **Place your first trade** — buy 10 shares of any stock using the Order Panel

## Key Concepts

**Your portfolio** shows everything you own: cash, positions, total equity, and P&L.

**The market moves on its own.** Stock prices change based on economic conditions, earnings, news events, and AI trader activity. You don't control the market — you react to it.

**Speed controls** let you fast-forward time. Start at 1x to learn, then increase to 2x or 5x once comfortable.

## What to Do First

1. Buy 2-3 stocks in different sectors (don't put everything in one stock)
2. Watch how prices move at 2x speed
3. Notice the news ticker — events affect stock prices
4. Check the Portfolio tab to see your P&L

## Common Mistakes

- Putting all your money in one stock
- Panic selling on small drops (-2% is normal daily movement)
- Trading too frequently (commissions add up)
- Ignoring stop losses (always have an exit plan)

> **Tip:** Use the Glossary (Ctrl+G) to look up any term you don't understand. Every concept in StockSim maps to a real-world market concept.`,
    relatedIds: ['guide-intermediate', 'stocks-101', 'order-types'],
  },

  {
    id: 'guide-intermediate',
    title: 'Intermediate Strategies',
    category: 'guide',
    summary: 'Level up your trading with technical analysis and risk management.',
    content: `## Beyond Basics

You've placed some trades and understand how the market works. Now it's time to develop a systematic approach.

## Technical Analysis in StockSim

Turn on indicators in the chart controls:
- **SMA 20/50:** Short and medium-term trend direction
- **RSI:** Overbought (>70) and oversold (<30) signals
- **Bollinger Bands:** Volatility and potential breakouts
- **VWAP:** Institutional price benchmark

## Building a Trading Plan

Every trade should have three elements BEFORE you enter:
1. **Entry criteria:** Why are you buying? (e.g., "RSI below 30 + price at support")
2. **Stop loss:** Where you'll exit if wrong (e.g., "-5% from entry")
3. **Profit target:** Where you'll take profits (e.g., "+15% or resistance level")

## Reading the Dashboard

- **Market Phase:** Bull markets favor buying dips; Bear markets favor short selling or staying defensive
- **VIX:** Low VIX (<20) = calm market, opportunities in selling options premium. High VIX (>30) = volatile, be cautious with position sizing
- **Fear & Greed:** Extreme fear = potential buying opportunity. Extreme greed = consider taking profits

## Sector Analysis

Don't just pick random stocks. Analyze sector performance:
- Which sectors are outperforming the market?
- Is money rotating from growth to value (or vice versa)?
- Are defensive sectors leading? (That's a warning sign for the broader market)

## Position Sizing

Never risk more than 5% of your portfolio on a single position. With $100,000:
- Maximum position size: $5,000
- If using a 5% stop loss, your maximum risk is $250 (0.25% of portfolio)

> **Challenge:** Try the "Market Crash" scenario. Start with $100,000, navigate a bear market, and try to end with more than you started.`,
    relatedIds: ['guide-beginner', 'guide-advanced', 'risk-management', 'rsi'],
  },

  {
    id: 'guide-advanced',
    title: 'Advanced Techniques',
    category: 'guide',
    summary: 'Options, short selling, and macro trading.',
    content: `## Advanced Trading

You're comfortable with basic trading and technical analysis. Time to explore the full toolkit.

## Options Trading

The Options tab (X) opens the options chain. Start with:
1. **Buying calls** on stocks you're bullish on (cheaper than buying shares, but riskier)
2. **Buying puts** to hedge existing positions (insurance)
3. **Covered calls** on stocks you own (generates income)

Key: Watch the Greeks. Delta tells you how much the option moves per $1 stock move. Theta is your daily time decay cost.

## Short Selling

The sell side isn't just "selling stocks you own." You can short sell to profit from declining prices:
1. Identify overvalued or deteriorating stocks (high P/E, declining revenue, negative news)
2. Short sell via the Order Panel (choose "Short" side)
3. Set a stop loss ABOVE your entry (shorts lose when prices rise)

**Warning:** Unlimited loss potential. Always use stop losses.

## Event-Driven Trading

Watch for:
- **Earnings beats/misses:** Trade the post-earnings drift
- **Fed rate decisions:** Impact all sectors, especially financials and real estate
- **Narrative arcs:** Developing stories create multi-day trends
- **Economic data releases:** GDP, unemployment, CPI affect market sentiment

## Macro Framework

1. Check Market Phase (Bull/Bear/Neutral)
2. Check VIX (calm/volatile)
3. Check Fear & Greed (extreme readings = contrarian signals)
4. Identify leading sectors
5. Find individual stocks within those sectors
6. Time entry using technical indicators

> **Challenge:** Try the "Expert" difficulty. Less starting capital, higher commissions, stricter margin. Can you grow $50,000 to $500,000?`,
    relatedIds: ['guide-intermediate', 'options-strategies', 'short-selling', 'earnings-trading'],
  },

  {
    id: 'guide-scenarios',
    title: 'Scenario Guide',
    category: 'guide',
    summary: 'Tips for completing each scenario successfully.',
    content: `## What Are Scenarios?

Scenarios are structured challenges with specific goals, time limits, and economic conditions. They test different skills and teach specific lessons.

## General Tips

1. **Read the briefing carefully.** Each scenario tells you the economic conditions and your objective
2. **Check the fail conditions.** Know what causes you to lose before you start trading
3. **Manage your time.** Scenarios have day limits — don't waste time at slow speeds
4. **Adapt to the environment.** A bear market scenario requires different strategies than a bull market

## Difficulty Matters

- **Easy:** Generous time limits, favorable conditions, lower fail thresholds
- **Normal:** Balanced challenge, realistic conditions
- **Hard:** Tight time limits, adverse conditions, strict requirements
- **Brutal:** Expert-only. Minimal margin for error

## Common Strategies by Scenario Type

**Growth scenarios** (grow portfolio to $X): Focus on high-beta stocks, momentum plays, and leverage. Speed matters.

**Survival scenarios** (don't lose more than X%): Defensive positioning, diversification, hedging with puts. Capital preservation over growth.

**Income scenarios** (generate $X in dividends): High-yield stocks, REITs, utilities. Patience and compound returns.

**Short-focused scenarios** (profit from decline): Find the weakest stocks, use proper position sizing, watch for short squeezes.

> **Pro tip:** You can retry any scenario. Your first attempt teaches you the conditions; subsequent attempts let you optimize your strategy.`,
    relatedIds: ['guide-beginner', 'risk-management', 'sector-rotation'],
  },

  {
    id: 'guide-achievements',
    title: 'Achievement Guide',
    category: 'guide',
    summary: 'How to unlock all achievements.',
    content: `## Achievement Categories

StockSim has achievements across four categories:

## Wealth Achievements

Track your portfolio growth milestones:
- **First Steps:** Reach $105,000 portfolio value
- **Six Figures:** Reach $200,000
- **Half a Million:** Reach $500,000
- **Millionaire:** Reach $1,000,000
- **Multi-Millionaire:** Reach $5,000,000

**Tips:** Compound growth is the key. Reinvest profits, diversify, and be patient. At 10x speed, you can accumulate wealth rapidly.

## Trading Achievements

Track your trading activity and skill:
- **First Trade:** Place your first order
- **Day Trader:** Execute 10+ trades in a single day
- **Diversified:** Own 10+ different stocks simultaneously
- **Short Seller:** Successfully profit from a short sale
- **Options Player:** Trade your first option

## Market Achievements

Track market events you've witnessed:
- **Crash Survivor:** Experience a circuit breaker halt
- **Bull Rider:** Hold positions through a 20%+ market rally
- **Earnings Season:** Hold a stock through its earnings report
- **Dividend Collector:** Receive your first dividend

## Hidden Achievements

Some achievements are hidden until unlocked. Experiment with different strategies and scenarios to discover them. Hint: some involve extreme outcomes...

> **Tip:** Achievements pop up with a gold animation when unlocked. They're permanently tracked across all your games. Check the Analytics tab for your achievement progress.`,
    relatedIds: ['guide-beginner', 'guide-intermediate'],
  },

  {
    id: 'guide-keyboard',
    title: 'Keyboard Shortcuts & Efficiency',
    category: 'guide',
    summary: 'Master the keyboard to trade faster.',
    content: `## Essential Shortcuts

**Navigation:**
- D — Dashboard
- P — Portfolio
- M — Market
- X — Options
- N — News
- A — Analytics
- J — Trade Journal

**Speed Control:**
- 1 — Normal (1x)
- 2 — Fast (2x)
- 3 — Very Fast (5x)
- 4 — Maximum (10x)
- Space — Pause/Resume

**Trading:**
- B — Focus Buy quantity input
- S — Focus Sell quantity input

**Tools:**
- Ctrl+K — Command Bar (search stocks, quick actions)
- Ctrl+W — Wiki
- Ctrl+G — Glossary
- Ctrl+S — Quick Save
- ? — Keyboard shortcuts help

## Command Bar Tips

The Command Bar (Ctrl+K) is the fastest way to navigate:
- Type a stock symbol to jump to it
- Type a company name to search
- Type "buy", "sell" for quick trading actions
- Type "save", "settings" for quick actions

## Custom Keybindings

You can rebind all keyboard shortcuts in Settings > Controls. Click the key display, press your desired key combination, and it's saved immediately.

> **Pro tip:** Learn the keyboard shortcuts and you'll trade much faster. The Command Bar alone can replace most mouse navigation.`,
    relatedIds: ['guide-beginner'],
  },

  // ═══════════════════════════════════════════════════════════════
  // HISTORY OF MARKETS
  // ═══════════════════════════════════════════════════════════════

  {
    id: 'history-amsterdam',
    title: 'Amsterdam 1602 — Birth of the Stock Market',
    category: 'history',
    summary: 'How the Dutch East India Company created the first stock exchange.',
    content: `## The First Corporation

In 1602, the Dutch East India Company (VOC — Vereenigde Oost-Indische Compagnie) needed massive capital to fund trading voyages to Asia. The solution: sell shares to the public.

This was revolutionary. Previously, investors funded individual voyages — if the ship sank, you lost everything. The VOC created **permanent capital** — shares that could be bought and sold independently of any single voyage.

## The Amsterdam Stock Exchange

The world's first official stock exchange was established to trade VOC shares. Features that still exist today:
- **Public trading:** Anyone could buy or sell shares
- **Price discovery:** Supply and demand determined the price
- **Speculation:** Traders began buying and selling for profit rather than investment
- **Short selling:** Dutch traders invented short selling in the 1600s

## Early Market Phenomena

The VOC's share price experienced the same patterns we see today:
- **Bubbles:** Tulip Mania (1637) — tulip bulb prices rose 5,900% before crashing
- **Manipulation:** Traders spread false rumors about ship arrivals to move prices
- **Insider trading:** Company directors traded on private information
- **Bear raids:** Coordinated short selling to drive prices down

## Legacy

The Amsterdam exchange established principles that every modern market follows: public price discovery, transferable ownership, and the separation of company operations from investor trading. The concept of publicly-traded companies changed the world — enabling ventures too large for any individual to fund alone.`,
    relatedIds: ['market-participants', 'short-selling'],
  },

  {
    id: 'history-wall-street',
    title: 'Wall Street — The Financial Capital',
    category: 'history',
    summary: 'How a street in lower Manhattan became the center of global finance.',
    content: `## The Buttonwood Agreement (1792)

Twenty-four stockbrokers signed the Buttonwood Agreement under a buttonwood tree on Wall Street, creating the New York Stock Exchange (NYSE). They agreed to trade securities only among themselves and charge minimum commissions.

## Growth of American Finance

**1800s:** The NYSE grew with America's industrialization. Railroad stocks were the first "tech boom." Cornelius Vanderbilt, Jay Gould, and J.P. Morgan became the original Wall Street titans — sometimes manipulating markets in ways that would be criminal today.

**1900-1920s:** Wall Street financed America's rise as a global power. The stock market became accessible to ordinary Americans for the first time.

**1929-1940s:** The crash and Great Depression nearly destroyed public trust in markets. The SEC was created to regulate the industry.

**1950s-1970s:** Post-war prosperity brought the "Nifty Fifty" era — 50 blue-chip stocks that "everyone" owned. The go-go years of the 1960s introduced performance-driven mutual funds.

## The Electronic Revolution

**1971:** NASDAQ launched as the first electronic stock exchange — no trading floor, pure computer-matching.

**1975:** Fixed commissions abolished — the birth of discount brokers.

**2000s:** Decimalization (penny pricing instead of fractions), high-frequency trading, and dark pools transformed market structure.

**2010s-2020s:** Zero-commission trading (Robinhood), meme stocks, cryptocurrency, and the democratization of finance.

## Wall Street Today

The physical trading floor is now largely symbolic. Most trading happens electronically across multiple venues. But "Wall Street" still represents the global financial system — the intersection of capital, risk, ambition, and innovation.`,
    relatedIds: ['history-amsterdam', 'crash-1929', 'regulation'],
  },

  {
    id: 'history-crashes',
    title: 'A History of Market Crashes',
    category: 'history',
    summary: 'From 1929 to 2020 — the patterns that repeat with every crash.',
    content: `## The Pattern

Every major crash follows the same emotional cycle: Optimism → Euphoria → "This time is different" → Denial → Panic → Capitulation → Depression → Hope → Recovery.

## Major Crashes

**1929 (The Great Crash):** -89%. Excessive leverage and speculation. Took 25 years to recover.

**1973-1974 (Oil Crisis):** -48%. OPEC oil embargo + Watergate + inflation. Stagflation made everything worse.

**1987 (Black Monday):** -22% in one day. Portfolio insurance (automatic selling) created a cascade. Led to circuit breakers.

**2000-2002 (Dot-Com Bust):** NASDAQ -78%. Internet companies with no revenue had billion-dollar valuations. "Profits don't matter" until they do.

**2008 (Global Financial Crisis):** -57%. Subprime mortgages, excessive leverage, credit default swaps. Nearly destroyed the global banking system.

**2020 (COVID Crash):** -34% in 23 days — the fastest drop in history. But also the fastest recovery: back to all-time highs in 5 months. Unprecedented government intervention.

## What They Have in Common

1. **Excessive leverage or speculation** preceding the crash
2. **A trigger event** that reveals the underlying fragility
3. **Contagion** — problems spread from one market to others
4. **Forced selling** — margin calls, fund redemptions, and panic create a vicious cycle
5. **Overreaction** — prices fall below fair value during panic
6. **Recovery** — markets always recover eventually (but "eventually" can be a long time)

## Lessons for Traders

- Crashes are normal. They happen roughly once a decade
- The time to prepare is during calm markets, not during the crash
- Cash is a position — having money available during a crash is an opportunity
- The recovery rewards those who didn't sell at the bottom

> In StockSim, you'll experience market downturns, circuit breakers, and flash crashes. These are the moments that define a trader — not the bull markets where everything goes up.`,
    relatedIds: ['crash-1929', 'circuit-breakers', 'risk-management'],
  },

  {
    id: 'history-electronic',
    title: 'The Electronic Trading Revolution',
    category: 'history',
    summary: 'How computers transformed markets from trading floors to server farms.',
    content: `## The Old Days

Before electronic trading, markets worked through **open outcry** — traders shouting and using hand signals on a physical trading floor. Orders were written on slips of paper. A single trade could take minutes.

## Key Milestones

**1971 — NASDAQ launches:** The first fully electronic stock exchange. No trading floor, just computers matching orders. Initially controversial ("you can't trust a computer to trade stocks").

**1975 — Fixed commissions abolished:** Previously, all brokers charged the same fee. Deregulation created competition and eventually led to discount brokers.

**1987 — Program trading:** Computerized portfolio insurance contributed to Black Monday. The crash revealed both the power and danger of automated trading.

**1998 — ECNs emerge:** Electronic Communication Networks (Instinet, Island) let traders bypass traditional exchanges. Competition drove spreads down.

**2001 — Decimalization:** Stocks switched from fractions (1/8, 1/16) to pennies. Spreads collapsed from 12.5 cents to 1 cent on many stocks.

**2005 — Reg NMS:** Required orders to be routed to the venue with the best price, regardless of which exchange. Created the modern multi-venue market structure.

**2010s — HFT era:** High-frequency traders now execute millions of trades per second, providing liquidity but also raising fairness concerns. The "Flash Crash" of 2010 showed the risks.

## Impact

- **Speed:** Trades that took minutes now take microseconds
- **Cost:** Commissions went from $100+ per trade to $0
- **Access:** Anyone with an internet connection can trade global markets
- **Complexity:** Modern market structure is incomprehensibly complex — dozens of exchanges, dark pools, and order types

## What's Next?

Blockchain-based settlement (T+0 instead of T+2), further democratization through mobile apps, and AI-driven trading strategies are the current frontier.`,
    relatedIds: ['history-wall-street', 'dark-pools', 'market-participants'],
  },

  {
    id: 'history-retail',
    title: 'The Retail Revolution',
    category: 'history',
    summary: 'From exclusive club to everyone with a smartphone.',
    content: `## The Old World

Until the 1970s, investing was for the wealthy. Minimum account sizes of $10,000+, high commissions ($100+ per trade), and limited information kept ordinary people out.

## Democratization Timeline

**1975:** Charles Schwab founded the first discount brokerage after commission deregulation. Trade costs dropped from $100+ to $30.

**1982:** 401(k) plans introduced. Millions of Americans became investors through employer retirement plans — often without realizing it.

**1994:** The internet arrives. Online brokers (E*Trade, Ameritrade) let people trade from home. Commissions dropped to $10-$15.

**2004:** Google Finance, Yahoo Finance made market data free. Previously, real-time quotes cost hundreds per month.

**2013:** Robinhood founded with a radical promise: $0 commissions. Funded by payment for order flow — selling order data to market makers.

**2020-2021:** The perfect storm:
- COVID lockdowns → people stuck at home with stimulus checks
- $0 commission trading widely available
- Social media (Reddit, TikTok, YouTube) created financial content communities
- GameStop and meme stocks brought millions of new traders

## The Current Landscape

- **500+ million** individual brokerage accounts in the US alone
- **Fractional shares** let you buy $1 of any stock
- **Options trading** accessible to retail for the first time at scale
- **Information parity** — retail has access to the same data as institutions (mostly)

## The Double Edge

More access is fundamentally good — but it also means more inexperienced traders taking on risk they don't understand. Options trading, in particular, has led to significant losses for retail traders who don't understand the Greeks.

## The Future

The trend is clear: markets will become more accessible, more global, and more 24/7. The question is whether education keeps pace with access.

> StockSim exists because of this gap. Access without education is dangerous. Our goal is to make you a better trader before you risk real money.`,
    relatedIds: ['history-electronic', 'gamestop-2021', 'market-participants'],
  },
];
