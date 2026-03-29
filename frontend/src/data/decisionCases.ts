/**
 * Decision Cases: Guided learning scenarios with decision points.
 * Each case sets up a situation, asks the player what to do, and explains the outcome.
 * Runs on top of the existing scenario system — no backend changes needed.
 */

export interface DecisionPoint {
  /** When to trigger: day number since scenario start */
  triggerDay: number;
  /** Title of the decision point */
  title: string;
  /** Situation description */
  situation: string;
  /** Available choices */
  choices: DecisionChoice[];
  /** Glossary terms relevant to this decision */
  glossaryTerms: string[];
}

export interface DecisionChoice {
  id: string;
  label: string;
  description: string;
  /** Is this the recommended action? */
  isRecommended: boolean;
  /** Explanation shown after choosing */
  explanation: string;
}

export interface DecisionCase {
  id: string;
  name: string;
  subtitle: string;
  description: string;
  difficulty: 'Beginner' | 'Intermediate' | 'Advanced';
  /** Color accent for the card */
  color: string;
  /** Estimated duration in minutes */
  durationMinutes: number;
  /** What the player will learn */
  learningGoals: string[];
  /** Scenario ID to start (from existing scenarios), or null for free play */
  scenarioId: string | null;
  /** Decision points that trigger during gameplay */
  decisions: DecisionPoint[];
  /** Summary shown at the end */
  summary: string;
}

export const DECISION_CASES: DecisionCase[] = [
  {
    id: 'first_trade',
    name: 'Your First Trade',
    subtitle: 'Learn the basics of buying and selling',
    description: 'You have $50,000 and need to make your first investment. Learn how to analyze a stock, place an order, and manage your position.',
    difficulty: 'Beginner',
    color: '#10B981',
    durationMinutes: 10,
    learningGoals: ['Place a market order', 'Understand bid/ask spread', 'Read a stock chart', 'Check your P&L'],
    scenarioId: null,
    decisions: [
      {
        triggerDay: 0,
        title: 'Choose Your First Stock',
        situation: 'You have $50,000 in cash. The market just opened. Look at the watchlist on the left — you need to pick a stock to buy. Consider: Which sectors do you understand? Which stocks have reasonable P/E ratios?',
        choices: [
          { id: 'tech', label: 'Buy a Technology stock', description: 'Tech stocks are volatile but can grow fast.', isRecommended: false, explanation: 'Tech stocks can be great, but they\'re volatile. For a first trade, a more stable stock might be less stressful.' },
          { id: 'diverse', label: 'Buy a large, stable company', description: 'Look for low volatility, positive dividends.', isRecommended: true, explanation: 'Great choice! Large, stable companies with dividends are perfect for learning. Lower risk while you get comfortable.' },
          { id: 'yolo', label: 'Put everything in one stock', description: 'Go big or go home!', isRecommended: false, explanation: 'Concentrating 100% in one stock is very risky. Most professionals never put more than 5-10% in a single position.' },
        ],
        glossaryTerms: ['Market Order', 'P/E Ratio', 'Dividend Yield'],
      },
      {
        triggerDay: 3,
        title: 'Your Stock Dropped 3%',
        situation: 'Your position is now showing a loss. This is normal — stocks move up and down daily. The question is: what do you do now?',
        choices: [
          { id: 'panic_sell', label: 'Sell immediately!', description: 'Cut your losses before it gets worse.', isRecommended: false, explanation: 'Panic selling locks in your loss. A 3% drop is normal daily volatility. The stock could recover tomorrow.' },
          { id: 'hold', label: 'Hold and wait', description: 'The fundamentals haven\'t changed.', isRecommended: true, explanation: 'Good instinct. If nothing fundamental changed (no bad news, no earnings miss), a 3% drop is just noise. Patience is key.' },
          { id: 'buy_more', label: 'Buy more at the lower price', description: 'Average down your cost basis.', isRecommended: false, explanation: 'Averaging down can work, but only if you\'re confident in the stock. For beginners, it\'s better to wait and observe.' },
        ],
        glossaryTerms: ['Unrealized P&L', 'Drawdown'],
      },
    ],
    summary: 'You learned how to pick a stock, place your first order, and handle a price drop. The key lesson: don\'t panic sell on normal volatility.',
  },
  {
    id: 'earnings_play',
    name: 'Earnings Season',
    subtitle: 'Navigate quarterly earnings reports',
    description: 'A company in your portfolio is about to report earnings. The stock could jump 5% or drop 10%. How do you prepare?',
    difficulty: 'Intermediate',
    color: '#F59E0B',
    durationMinutes: 15,
    learningGoals: ['Understand earnings impact', 'Use limit orders to manage risk', 'Learn about IV crush'],
    scenarioId: null,
    decisions: [
      {
        triggerDay: 0,
        title: 'Earnings in 2 Days',
        situation: 'One of the stocks in the market is reporting earnings in 2 days. The stock has been running up 8% in anticipation. Analysts expect strong results, but the stock is already priced for perfection.',
        choices: [
          { id: 'buy_before', label: 'Buy before earnings', description: 'Ride the momentum into the report.', isRecommended: false, explanation: 'Buying right before earnings is gambling. Even with a beat, the stock often sells off ("buy the rumor, sell the news"). The run-up already priced in good results.' },
          { id: 'wait', label: 'Wait for the report', description: 'See the actual numbers first.', isRecommended: true, explanation: 'Smart move. Waiting removes the binary risk. You can buy after earnings if the report confirms growth. You might pay a bit more, but with much less risk.' },
          { id: 'sell_existing', label: 'Sell if you own it', description: 'Take profits before the gamble.', isRecommended: false, explanation: 'Taking some profits before earnings isn\'t wrong — but selling your entire position out of fear means you miss potential upside. A partial sell (trim) would be ideal.' },
        ],
        glossaryTerms: ['P/E Ratio', 'Fair Value', 'Slippage'],
      },
      {
        triggerDay: 3,
        title: 'Earnings Beat, Stock Drops 4%',
        situation: 'The company beat earnings estimates by 5%, but the stock dropped 4% anyway. This happens when results are "priced in" — the market expected even better. This is called "buy the rumor, sell the news."',
        choices: [
          { id: 'buy_dip', label: 'Buy the dip', description: 'Good company, temporary selloff.', isRecommended: true, explanation: 'If the company beat estimates and guidance is strong, a post-earnings dip is often a buying opportunity. The market overreacts to short-term noise.' },
          { id: 'avoid', label: 'Stay away', description: 'If good news causes a drop, something\'s wrong.', isRecommended: false, explanation: 'Not necessarily wrong, but you\'d miss many great opportunities. Post-earnings dips on beats often reverse within 1-2 weeks.' },
          { id: 'short', label: 'Short the stock', description: 'The trend is down, ride it.', isRecommended: false, explanation: 'Shorting after an earnings beat is dangerous. The fundamental story is positive — you\'d be betting against improving business.' },
        ],
        glossaryTerms: ['Short Selling', 'Limit Order', 'Fair Value'],
      },
    ],
    summary: 'Earnings season is tricky. Key lessons: don\'t buy right before earnings (it\'s gambling), wait for the report, and know that "buy the rumor, sell the news" is real.',
  },
  {
    id: 'flash_crash',
    name: 'Flash Crash Survival',
    subtitle: 'What to do when markets panic',
    description: 'The market just dropped 5% in 10 minutes. Circuit breakers are firing. News is screaming. Everyone is selling. What do you do?',
    difficulty: 'Advanced',
    color: '#EF4444',
    durationMinutes: 10,
    learningGoals: ['Stay calm during crashes', 'Understand circuit breakers', 'Spot opportunities in panic'],
    scenarioId: 'the_crash',
    decisions: [
      {
        triggerDay: 1,
        title: 'Market Down 7% — Circuit Breaker!',
        situation: 'Level 1 circuit breaker triggered. Trading halted for 15 minutes. Your portfolio is down 12%. Social media is full of "the end is near" posts. The Fear & Greed Index is at 5 (Extreme Fear).',
        choices: [
          { id: 'sell_all', label: 'Sell everything', description: 'Get to cash before it gets worse.', isRecommended: false, explanation: 'Selling at the bottom of a panic is the worst thing you can do. Historically, markets recover from flash crashes within days or weeks. Selling locks in your losses permanently.' },
          { id: 'hold', label: 'Do nothing', description: 'Don\'t make emotional decisions.', isRecommended: true, explanation: 'This is the hardest but often the best choice. Flash crashes recover. Warren Buffett: "Be fearful when others are greedy, and greedy when others are fearful."' },
          { id: 'buy', label: 'Buy quality stocks on sale', description: 'Blood in the streets = opportunity.', isRecommended: false, explanation: 'Buying during a crash can be extremely profitable, but only if you buy quality companies. Don\'t try to catch the exact bottom — nobody can. But buying a great company at a 15% discount? That\'s how fortunes are made.' },
        ],
        glossaryTerms: ['Circuit Breaker', 'Flash Crash', 'Fear & Greed Index'],
      },
    ],
    summary: 'Flash crashes feel like the end of the world, but they\'re almost always temporary. The key: don\'t sell in panic. If anything, look for quality stocks at a discount.',
  },
  {
    id: 'sector_rotation',
    name: 'Sector Rotation',
    subtitle: 'Follow the money between sectors',
    description: 'Interest rates are rising. Some sectors benefit, others suffer. Learn to read macro signals and position accordingly.',
    difficulty: 'Intermediate',
    color: '#8B5CF6',
    durationMinutes: 15,
    learningGoals: ['Read economic indicators', 'Understand sector sensitivity', 'Rotate portfolio proactively'],
    scenarioId: null,
    decisions: [
      {
        triggerDay: 0,
        title: 'Fed Raises Interest Rates',
        situation: 'The Federal Reserve just raised interest rates by 0.5%. Technology stocks are dropping. Financial stocks are rising. What should you do with your portfolio?',
        choices: [
          { id: 'hold_tech', label: 'Hold Tech, it\'ll recover', description: 'Tech always comes back.', isRecommended: false, explanation: 'Tech does come back eventually, but rate hikes can pressure growth stocks for months. The math: higher rates = future earnings worth less today = lower PE multiples.' },
          { id: 'rotate', label: 'Sell some Tech, buy Financials', description: 'Banks profit from higher rates.', isRecommended: true, explanation: 'Correct! Banks earn more when rates rise (wider net interest margin). Rotating from rate-sensitive sectors (Tech, Real Estate) to rate-beneficiaries (Financials) is textbook sector rotation.' },
          { id: 'all_cash', label: 'Sell everything, wait it out', description: 'Cash is king in uncertainty.', isRecommended: false, explanation: 'Going to 100% cash means you pay taxes on gains AND miss any recovery. A better approach: reduce risk, don\'t eliminate it.' },
        ],
        glossaryTerms: ['Interest Rate (Fed Rate)', 'Sector Rotation', 'P/E Ratio'],
      },
    ],
    summary: 'Sector rotation is how professional fund managers adjust to changing economic conditions. Key: watch interest rates, inflation, and PMI to anticipate which sectors will lead.',
  },
  {
    id: 'stop_loss_mastery',
    name: 'Risk Management 101',
    subtitle: 'Protect your capital with stop losses',
    description: 'Learn the most important skill in trading: knowing when to cut your losses. Set stop losses, manage position sizes, and survive to trade another day.',
    difficulty: 'Beginner',
    color: '#60A5FA',
    durationMinutes: 10,
    learningGoals: ['Set a stop-loss order', 'Calculate position size', 'Understand risk/reward ratio'],
    scenarioId: null,
    decisions: [
      {
        triggerDay: 0,
        title: 'How Much to Risk?',
        situation: 'You found a stock you like at $50. You have $50,000. How much should you invest in this single position?',
        choices: [
          { id: 'all_in', label: '$50,000 (100%)', description: 'Maximum conviction!', isRecommended: false, explanation: 'Never put 100% in one stock. If it drops 20%, you lose $10,000. Professional traders rarely risk more than 2-5% of their portfolio on a single idea.' },
          { id: 'half', label: '$25,000 (50%)', description: 'A large but not insane position.', isRecommended: false, explanation: '50% is still very concentrated. If this stock has bad news, half your portfolio is at risk. Try 10-20% for a high-conviction bet.' },
          { id: 'proper', label: '$5,000 (10%)', description: '10% of portfolio per position.', isRecommended: true, explanation: 'This is solid risk management. A 10% position means even a 20% drop only costs you 2% of your total portfolio. You can afford to be wrong.' },
        ],
        glossaryTerms: ['Position Size', 'Diversification', 'Stop Order'],
      },
      {
        triggerDay: 2,
        title: 'Where to Set Your Stop Loss?',
        situation: 'You bought at $50. The stock is now at $51. You need to protect your gains and limit downside. Where do you place a stop-loss order?',
        choices: [
          { id: 'tight', label: '$49.50 (1% below entry)', description: 'Very tight stop.', isRecommended: false, explanation: 'A 1% stop is too tight — normal intraday volatility will stop you out. Most stocks move 1-3% daily. You\'d get "stopped out" on noise.' },
          { id: 'reasonable', label: '$47.50 (5% below entry)', description: 'Room to breathe.', isRecommended: true, explanation: 'A 5% stop gives the stock room for normal volatility while still protecting you from serious damage. With a 10% position, a 5% stop = 0.5% portfolio risk.' },
          { id: 'wide', label: '$40.00 (20% below entry)', description: 'Only for major crashes.', isRecommended: false, explanation: 'A 20% stop is basically no stop at all for short-term trading. By the time it triggers, you\'ve already lost 20%. Useful only for long-term holds.' },
        ],
        glossaryTerms: ['Stop Order', 'Trailing Stop', 'Slippage'],
      },
    ],
    summary: 'Risk management is the #1 skill that separates surviving traders from blown-up accounts. Rules: never risk more than 2-5% per trade, always use stop losses, and position size matters more than stock selection.',
  },
  {
    id: 'dividend_income',
    name: 'Building Passive Income',
    subtitle: 'Create a dividend portfolio',
    description: 'Learn how to build a portfolio that pays you every quarter. Find high-yield stocks, understand ex-dates, and calculate your passive income.',
    difficulty: 'Beginner',
    color: '#10B981',
    durationMinutes: 15,
    learningGoals: ['Find dividend-paying stocks', 'Understand ex-dividend dates', 'Calculate yield on cost'],
    scenarioId: 'dividend_king',
    decisions: [
      {
        triggerDay: 0,
        title: 'Picking Dividend Stocks',
        situation: 'You have $100,000 to build a dividend portfolio. You see stocks yielding 2% (stable blue chips) and stocks yielding 8% (risky small caps). Which do you choose?',
        choices: [
          { id: 'high_yield', label: 'Go for 8% yield', description: 'Maximum income!', isRecommended: false, explanation: 'Very high yields are often a trap ("yield trap"). The stock price may be falling (making the yield look high), or the company might cut the dividend. Yields above 5% deserve extra scrutiny.' },
          { id: 'balanced', label: 'Mix of 2-4% yielders', description: 'Stable, growing dividends.', isRecommended: true, explanation: 'Companies with 2-4% yield and a history of dividend growth are the sweet spot. They\'re profitable enough to keep paying AND grow the dividend over time.' },
          { id: 'growth', label: 'Ignore dividends, buy growth', description: 'Capital gains > dividends.', isRecommended: false, explanation: 'For this scenario you need dividend income, but the insight is valid: total return (price + dividends) matters more than yield alone.' },
        ],
        glossaryTerms: ['Dividend Yield', 'P/E Ratio', 'Diversification'],
      },
    ],
    summary: 'Dividend investing is about sustainable income, not maximum yield. Look for companies with moderate yields (2-4%), strong earnings, and a history of growing their dividend.',
  },
  {
    id: 'news_trading',
    name: 'Trading the News',
    subtitle: 'React to breaking market events',
    description: 'A major news event just dropped. The market is reacting. Learn how to separate signal from noise and trade on information, not emotion.',
    difficulty: 'Intermediate',
    color: '#F59E0B',
    durationMinutes: 10,
    learningGoals: ['Evaluate news impact', 'Avoid FOMO trading', 'Use limit orders around events'],
    scenarioId: null,
    decisions: [
      {
        triggerDay: 1,
        title: 'Breaking: Company Under Investigation',
        situation: 'A stock in your watchlist just dropped 8% on news of a regulatory investigation. The headlines are scary, but you notice: the stock\'s P/E is now very low and the investigation might result in just a fine.',
        choices: [
          { id: 'buy_fear', label: 'Buy the fear', description: 'Market is overreacting.', isRecommended: false, explanation: 'Could work, but regulatory investigations are unpredictable. It could be a fine, or it could be fraud. Wait for more details before committing capital.' },
          { id: 'wait_clarity', label: 'Wait for more details', description: 'First reports are often wrong.', isRecommended: true, explanation: 'Smart. Initial news reports are often incomplete or exaggerated. Within 1-2 days, the real scope of the investigation becomes clearer. Then you can make an informed decision.' },
          { id: 'short_it', label: 'Short it, more bad news coming', description: 'Where there\'s smoke, there\'s fire.', isRecommended: false, explanation: 'Shorting after an 8% drop is risky — much of the damage is already priced in. If the investigation turns out to be minor, the stock rebounds and you\'re caught in a short squeeze.' },
        ],
        glossaryTerms: ['Short Selling', 'Fair Value', 'Short Squeeze'],
      },
    ],
    summary: 'When news breaks: don\'t react in the first 30 minutes. Wait for facts, not headlines. The market overreacts to fear and underreacts to slow-moving positive changes.',
  },
  {
    id: 'portfolio_rebalance',
    name: 'Portfolio Rebalancing',
    subtitle: 'Keep your portfolio balanced',
    description: 'After a month of trading, your portfolio has drifted. One stock is now 40% of your portfolio. Time to rebalance.',
    difficulty: 'Beginner',
    color: '#60A5FA',
    durationMinutes: 10,
    learningGoals: ['Identify portfolio concentration', 'Trim winners', 'Rebalance across sectors'],
    scenarioId: null,
    decisions: [
      {
        triggerDay: 5,
        title: 'Your Winner is Now 40% of Portfolio',
        situation: 'Your best stock is up 60% and now makes up 40% of your portfolio. You love the company. But if it drops 20%, your entire portfolio drops 8%.',
        choices: [
          { id: 'let_ride', label: 'Let it ride', description: 'Winners keep winning!', isRecommended: false, explanation: 'Momentum is real, but concentration risk is dangerous. Even the best companies have bad quarters. Letting one stock dominate your portfolio is a ticking time bomb.' },
          { id: 'trim', label: 'Sell half, keep half', description: 'Take some profit, stay invested.', isRecommended: true, explanation: 'This is the professional move. You lock in profits, reduce risk, and still participate in future upside. Use the proceeds to diversify into other sectors.' },
          { id: 'sell_all', label: 'Sell the entire position', description: 'Book the profit.', isRecommended: false, explanation: 'Selling a winner completely means you might miss more upside. The stock is winning for a reason. Trimming (selling part) is better than all-or-nothing.' },
        ],
        glossaryTerms: ['Diversification', 'Position Size', 'Realized P&L'],
      },
    ],
    summary: 'Rebalancing is boring but essential. No single position should ever be more than 15-20% of your portfolio. Trim winners, cut losers, and stay diversified.',
  },
];
