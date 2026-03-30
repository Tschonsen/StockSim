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
  {
    id: 'flash_crash_advanced',
    name: 'Flash Crash',
    subtitle: 'Survive a 7% market drop in minutes',
    description: 'The market just plunged 7% in under 10 minutes. Circuit breakers have halted trading. Panic is everywhere. Your portfolio is deep red. Every second feels like an eternity.',
    difficulty: 'Advanced',
    color: '#DC2626',
    durationMinutes: 15,
    learningGoals: ['Understand circuit breaker levels', 'Control emotions during extreme volatility', 'Identify contrarian opportunities', 'Recognize flash crash recovery patterns'],
    scenarioId: 'the_crash',
    decisions: [
      {
        triggerDay: 0,
        title: 'The Drop Begins — Down 4% in 3 Minutes',
        situation: 'Markets are in freefall. Your portfolio is down 6% and falling. The VIX has spiked to 45. Order books are thin — bid/ask spreads have widened to 5x normal. You can see sell orders stacking up.',
        choices: [
          { id: 'panic_sell', label: 'Sell everything at market price', description: 'Get out before it hits zero.', isRecommended: false, explanation: 'Selling into a flash crash means you get the WORST possible prices. Wide spreads and thin order books mean your market orders execute far below the displayed price. This is exactly when slippage destroys accounts.' },
          { id: 'hold', label: 'Turn off the screen and wait', description: 'Refuse to make emotional decisions.', isRecommended: true, explanation: 'The best decision in a flash crash is often NO decision. Historical data shows flash crashes recover 60-80% of the drop within hours to days. By not acting, you avoid selling at the absolute bottom.' },
          { id: 'buy_dip', label: 'Place limit buy orders on quality stocks', description: 'Set orders 10-15% below current prices.', isRecommended: false, explanation: 'This is the advanced contrarian play. Placing LIMIT orders (not market orders!) at deep discounts can score incredible entries. The key is limit orders only — market orders in a crash get terrible fills.' },
          { id: 'short', label: 'Short the market to hedge', description: 'Profit from further downside.', isRecommended: false, explanation: 'Shorting during a crash feels clever but is extremely dangerous. Circuit breakers can halt trading, and the snap-back rally can be violent. Many short sellers have been wiped out by flash crash recoveries.' },
        ],
        glossaryTerms: ['Circuit Breaker', 'Flash Crash', 'Slippage'],
      },
      {
        triggerDay: 1,
        title: 'Circuit Breaker Triggered — Trading Halted',
        situation: 'Level 1 circuit breaker (7% drop) has halted all trading for 15 minutes. The news is full of doomsday headlines. Social media is screaming "SELL!" Your phone is blowing up with panicked messages.',
        choices: [
          { id: 'queue_sell', label: 'Queue sell orders for when trading resumes', description: 'Be first out the door.', isRecommended: false, explanation: 'Queuing sells during a halt means your orders execute into the reopening chaos. Historically, the 15-minute halt calms markets — prices often stabilize or bounce when trading resumes.' },
          { id: 'review', label: 'Use the halt to review fundamentals', description: 'Check: has anything actually changed?', isRecommended: true, explanation: 'Excellent. The 15-minute halt exists precisely to give you time to THINK. Ask: is this a systemic crisis or a technical glitch? Has any real economic data changed? Usually the answer is no — and that means recovery is likely.' },
          { id: 'add_cash', label: 'Transfer more cash to buy the dip', description: 'Prepare for bargain hunting.', isRecommended: false, explanation: 'Being ready to buy after a panic is smart, but transferring money takes time. Focus on what you can control right now: your existing positions and your emotional state.' },
        ],
        glossaryTerms: ['Circuit Breaker', 'Fear & Greed Index', 'Drawdown'],
      },
      {
        triggerDay: 3,
        title: 'The Recovery — Markets Bounce 5%',
        situation: 'Two days after the crash, markets have recovered 5 of the 7 percentage points lost. Your portfolio is almost back to even. But the fear lingers — could another crash happen tomorrow?',
        choices: [
          { id: 'sell_recovery', label: 'Sell on the bounce — get out while you can', description: 'Take the chance to exit at better prices.', isRecommended: false, explanation: 'Selling after the recovery locks in a small loss instead of a big one, but it also means you miss the full recovery. Flash crash rebounds often continue for days as panic sellers buy back in.' },
          { id: 'hold_course', label: 'Stay the course — the thesis hasn\'t changed', description: 'Continue with your original plan.', isRecommended: true, explanation: 'If nothing fundamental changed, your original investment thesis is still valid. The crash was noise, the recovery proves it. Stay disciplined and don\'t let one scary day change your strategy.' },
          { id: 'hedge', label: 'Add hedges for future crashes', description: 'Buy protective puts or reduce leverage.', isRecommended: false, explanation: 'Adding hedges AFTER a crash is like buying insurance after the fire. The cost of puts is highest right after a crash (elevated IV). Better to have hedges in place before you need them.' },
        ],
        glossaryTerms: ['Drawdown', 'Unrealized P&L', 'Fear & Greed Index'],
      },
    ],
    summary: 'Flash crashes test your nerves, not your analysis. The lesson: circuit breakers exist to help you think, most flash crashes recover quickly, and the worst thing you can do is sell at market price into panic. If you do nothing, you usually come out fine.',
  },
  {
    id: 'short_squeeze',
    name: 'The Short Squeeze',
    subtitle: 'When shorts get trapped and prices explode',
    description: 'A stock with 40% short interest is starting to climb. Short sellers are getting nervous. If enough of them buy to cover, the price could skyrocket. Do you join the squeeze or stay away?',
    difficulty: 'Advanced',
    color: '#F43F5E',
    durationMinutes: 20,
    learningGoals: ['Understand short interest and days-to-cover', 'Recognize squeeze mechanics', 'Manage extreme risk scenarios', 'Know when to take profits'],
    scenarioId: null,
    decisions: [
      {
        triggerDay: 0,
        title: 'The Setup: 40% Short Interest',
        situation: 'You notice a stock trading at $20 with 40% short interest and only 2 days to cover. The company just reported surprisingly good earnings. Short sellers expected the company to fail — but it didn\'t. The stock is up 15% pre-market.',
        choices: [
          { id: 'short_more', label: 'Short the stock — it\'s overvalued', description: 'The fundamentals don\'t justify this price.', isRecommended: false, explanation: 'Shorting into a potential squeeze is one of the most dangerous trades possible. With 40% short interest, even a small rally forces massive buying pressure as shorts cover. Your losses are theoretically unlimited.' },
          { id: 'buy_shares', label: 'Buy shares to ride the squeeze', description: 'Join the momentum before it accelerates.', isRecommended: true, explanation: 'When short interest is this high and a catalyst appears, buying early in the squeeze can be very profitable. The key: use a SMALL position size (this is high risk) and have a clear exit plan.' },
          { id: 'buy_calls', label: 'Buy call options for leveraged exposure', description: 'Maximum profit potential with defined risk.', isRecommended: false, explanation: 'Options give you leverage, and your risk is limited to the premium paid. But IV (implied volatility) is already sky-high on a squeeze candidate, making options expensive. You\'re paying a huge premium for that leverage.' },
        ],
        glossaryTerms: ['Short Selling', 'Short Squeeze', 'Short Interest'],
      },
      {
        triggerDay: 2,
        title: 'The Squeeze Accelerates — Up 80%',
        situation: 'The stock has gone from $20 to $36 in two days. Short sellers are panicking and buying to cover, pushing the price even higher. Social media is on fire. Everyone is talking about "diamond hands." Your position is up 60%.',
        choices: [
          { id: 'hold_diamond', label: 'Diamond hands — hold for $100', description: 'The squeeze isn\'t over yet!', isRecommended: false, explanation: 'Greed is dangerous in a squeeze. They end as violently as they begin. When the last short covers, buying pressure evaporates and the price collapses. Nobody rings a bell at the top.' },
          { id: 'sell_half', label: 'Sell half, let the rest ride', description: 'Lock in profit, keep upside exposure.', isRecommended: true, explanation: 'The "sell half" strategy is perfect for squeezes. You lock in guaranteed profit (your cost basis is now effectively zero) while still participating if it goes higher. You can\'t go wrong with house money.' },
          { id: 'sell_all', label: 'Sell everything — take the 60% gain', description: 'Don\'t be greedy.', isRecommended: false, explanation: 'Taking 60% profit is never wrong. But selling everything means you miss out if the squeeze doubles from here. The half-and-half approach gives you the best of both worlds.' },
        ],
        glossaryTerms: ['Short Squeeze', 'Realized P&L', 'Unrealized P&L'],
      },
      {
        triggerDay: 4,
        title: 'The Top — Stock Hits $60, Then Drops to $40',
        situation: 'The stock peaked at $60 (200% from the start) and has now fallen to $40. It happened in one hour. Some people made a fortune. Others bought at $55 and are already down 27%. The squeeze is losing steam.',
        choices: [
          { id: 'buy_dip_again', label: 'Buy the dip — squeeze isn\'t over', description: 'It could go back to $60.', isRecommended: false, explanation: 'Buying dips during a deflating squeeze is extremely dangerous. Once the short covering is done, there\'s no more forced buying. The stock often falls back close to fundamental value, which could be $20.' },
          { id: 'sell_remaining', label: 'Sell remaining position', description: 'The party is over — take what\'s left.', isRecommended: true, explanation: 'Recognizing when a squeeze is ending is crucial. Signs: declining volume, short interest dropping, price making lower highs. When these appear, take your profits and walk away. You don\'t need to catch every dollar.' },
          { id: 'short_top', label: 'Short it — ride it back down', description: 'What goes up must come down.', isRecommended: false, explanation: 'Shorting after a squeeze feels obvious in hindsight, but timing the top is nearly impossible. The stock could spike to $80 before finally collapsing. Many "smart" shorts got destroyed trying to time the top of famous squeezes.' },
        ],
        glossaryTerms: ['Short Squeeze', 'Short Interest', 'Drawdown'],
      },
    ],
    summary: 'Short squeezes create explosive moves, but they always end. The strategy: get in early if you spot the setup, sell in tranches on the way up, and NEVER buy near the top. Greed kills more accounts in squeezes than anything else.',
  },
  {
    id: 'dividend_strategy',
    name: 'Dividend Strategy',
    subtitle: 'Build a reliable income portfolio',
    description: 'You want your money working for you. Build a dividend portfolio that pays consistent quarterly income. But beware: not all dividends are created equal.',
    difficulty: 'Beginner',
    color: '#059669',
    durationMinutes: 10,
    learningGoals: ['Distinguish yield traps from quality dividends', 'Understand DRIP (dividend reinvestment)', 'Compare REITs vs blue chips for income', 'Calculate compound growth of reinvested dividends'],
    scenarioId: 'dividend_king',
    decisions: [
      {
        triggerDay: 0,
        title: 'High Yield vs. Dividend Growth',
        situation: 'You have $80,000 to invest for income. Stock A yields 8% ($6,400/year) but has flat earnings and a 95% payout ratio. Stock B yields 2.5% ($2,000/year) but grows its dividend 10% annually and has a 40% payout ratio. Which is better for long-term income?',
        choices: [
          { id: 'high_yield', label: 'Stock A — $6,400/year right now', description: 'More income today is better.', isRecommended: false, explanation: 'A 95% payout ratio means the company pays out almost ALL its earnings. There\'s no room for error. One bad quarter and the dividend gets cut. This is a classic yield trap — high yield that\'s unsustainable.' },
          { id: 'growth', label: 'Stock B — starts lower but grows', description: '$2,000 now, but growing 10% per year.', isRecommended: true, explanation: 'In 12 years, Stock B\'s dividend income surpasses Stock A\'s (thanks to compounding). In 20 years, Stock B pays $13,500/year vs Stock A\'s still-$6,400. Plus, Stock B\'s share price likely grew too. Dividend GROWTH beats high yield.' },
          { id: 'split', label: 'Split 50/50 between both', description: 'Diversify your income sources.', isRecommended: false, explanation: 'Diversification is usually smart, but here you\'re diluting a great pick with a risky one. Better to diversify across multiple quality dividend growers than mix quality with yield traps.' },
        ],
        glossaryTerms: ['Dividend Yield', 'Diversification', 'P/E Ratio'],
      },
      {
        triggerDay: 3,
        title: 'REITs vs. Blue Chip Dividends',
        situation: 'You\'re deciding between a REIT (Real Estate Investment Trust) yielding 5% and a blue chip consumer staples company yielding 3%. REITs must pay 90% of income as dividends, making them high yielders. But they\'re sensitive to interest rates.',
        choices: [
          { id: 'reit', label: 'Go all-in on the REIT', description: '5% yield is hard to beat.', isRecommended: false, explanation: 'REITs are great for income, but they\'re essentially a bet on real estate and interest rates. When rates rise, REIT prices fall. Concentrating in one sector type defeats the purpose of building a resilient income stream.' },
          { id: 'blue_chip', label: 'Stick with blue chip dividends', description: 'Safer, more predictable income.', isRecommended: false, explanation: 'Blue chips are more stable, but limiting yourself to one type means you miss diversification benefits. REITs often move independently of regular stocks, which actually reduces portfolio risk.' },
          { id: 'mix', label: 'Blend REITs and blue chips', description: 'Different income sources for resilience.', isRecommended: true, explanation: 'A mix of REITs (real estate income), blue chip dividends (corporate earnings), and perhaps some utilities (regulated returns) creates a resilient income stream that holds up across different economic conditions.' },
        ],
        glossaryTerms: ['Dividend Yield', 'Interest Rate (Fed Rate)', 'Diversification'],
      },
      {
        triggerDay: 7,
        title: 'Reinvest or Take the Cash?',
        situation: 'Your first dividend payment arrives: $500. You can reinvest it automatically (DRIP) to buy more shares, or take the cash. You don\'t need the money right now.',
        choices: [
          { id: 'cash', label: 'Take the cash', description: 'Enjoy the passive income.', isRecommended: false, explanation: 'Taking cash feels rewarding, but you\'re leaving compound growth on the table. If you don\'t need the income now, reinvesting creates a snowball effect that dramatically increases future income.' },
          { id: 'reinvest', label: 'Reinvest via DRIP', description: 'Let compounding do its magic.', isRecommended: true, explanation: 'Reinvesting dividends is the single most powerful wealth-building strategy. A $100,000 portfolio yielding 3% with reinvested dividends grows to $242,000 in 20 years — without adding a single dollar. That\'s the magic of compounding.' },
          { id: 'save_cash', label: 'Save cash for buying dips', description: 'Deploy dividends when stocks are cheap.', isRecommended: false, explanation: 'Timing the market with dividend cash sounds clever, but studies show DRIP outperforms because it buys consistently — sometimes at highs, sometimes at lows, averaging out over time. Consistency beats timing.' },
        ],
        glossaryTerms: ['Dividend Yield', 'Fair Value', 'Realized P&L'],
      },
    ],
    summary: 'Dividend investing rewards patience. Key lessons: avoid yield traps (high yield with unsustainable payouts), prefer dividend GROWTH over current yield, diversify income sources across sectors, and reinvest dividends to harness the power of compounding.',
  },
  {
    id: 'options_101',
    name: 'Options 101',
    subtitle: 'Your first options trade before earnings',
    description: 'A company is reporting earnings next week. You believe the stock will make a big move — but you\'re not sure which direction. Welcome to the world of options.',
    difficulty: 'Intermediate',
    color: '#7C3AED',
    durationMinutes: 15,
    learningGoals: ['Understand calls vs puts', 'Learn about implied volatility and IV crush', 'Grasp the basics of the Greeks', 'Recognize time decay (theta)'],
    scenarioId: null,
    decisions: [
      {
        triggerDay: 0,
        title: 'Choosing Your Options Strategy',
        situation: 'TechCorp reports earnings in 5 days. The stock is at $100. Implied volatility is 60% (very high). You think the stock will move big but aren\'t sure of the direction. A $100 call costs $8 and a $100 put costs $7.',
        choices: [
          { id: 'buy_call', label: 'Buy a call option ($8 premium)', description: 'Bet on the stock going up after earnings.', isRecommended: false, explanation: 'A call is a directional bet. If TechCorp beats and rises to $115, your $8 call is worth $15 (87% gain). But if it drops, you lose the entire $8 premium. You\'re also paying a high IV premium that will evaporate after earnings (IV crush).' },
          { id: 'buy_put', label: 'Buy a put option ($7 premium)', description: 'Bet on the stock going down.', isRecommended: false, explanation: 'Same logic as the call but in reverse. You profit if the stock drops below $93 (strike minus premium). But you need a big move to overcome the IV crush — the inflated premium you paid will deflate after earnings regardless of direction.' },
          { id: 'straddle', label: 'Buy a straddle (call + put = $15)', description: 'Profit from a big move in either direction.', isRecommended: false, explanation: 'A straddle profits from a big move in EITHER direction. But at $15 combined cost, the stock needs to move more than 15% to profit. With IV at 60%, the market is already pricing in a big move. You\'re betting the move is even BIGGER than expected.' },
          { id: 'skip', label: 'Skip — options before earnings are overpriced', description: 'IV is too high, the edge isn\'t there.', isRecommended: true, explanation: 'This is the disciplined answer. When IV is at 60%, options are expensive because everyone expects a big move. After earnings, IV collapses (IV crush), destroying option value regardless of direction. Buying options before earnings is often a losing game.' },
        ],
        glossaryTerms: ['Options', 'Implied Volatility (IV)', 'Time Decay (Theta)'],
      },
      {
        triggerDay: 2,
        title: 'Understanding the Greeks',
        situation: 'You\'re studying options and see: Delta 0.50, Gamma 0.05, Theta -0.15, Vega 0.30. The stock hasn\'t moved, but your option lost $15 overnight. What happened?',
        choices: [
          { id: 'confused', label: 'Something\'s broken — the stock didn\'t move', description: 'If the stock is flat, options shouldn\'t lose value.', isRecommended: false, explanation: 'Options lose value every day even if the stock doesn\'t move! This is Theta (time decay). Your Theta of -0.15 means you lose $15/day per contract. Options are a wasting asset — time is always working against buyers.' },
          { id: 'theta', label: 'Theta decay — time is the enemy', description: 'Each day costs $15 in time value.', isRecommended: true, explanation: 'Exactly right. Theta is the daily cost of holding an option. At -0.15 ($15/day), you need the stock to move enough each day just to break even. This is why option buying has a low win rate — you\'re fighting the clock.' },
          { id: 'vega', label: 'IV dropped slightly', description: 'Vega measures sensitivity to volatility changes.', isRecommended: false, explanation: 'Good thinking! Vega IS a factor — if IV dropped, your option loses value via Vega. But with 5 days to earnings, IV usually rises. The primary culprit here is Theta decay, which accelerates as expiration approaches.' },
        ],
        glossaryTerms: ['Options', 'Time Decay (Theta)', 'Implied Volatility (IV)'],
      },
      {
        triggerDay: 5,
        title: 'Post-Earnings IV Crush',
        situation: 'Earnings came out — TechCorp beat estimates and the stock rose 5%. But the call option you were watching only went from $8 to $6. The stock went UP but the option went DOWN. This is IV crush in action.',
        choices: [
          { id: 'buy_after', label: 'Buy options AFTER earnings when IV is low', description: 'Cheaper options, less IV crush risk.', isRecommended: true, explanation: 'Buying after earnings means you pay for a directional move without the inflated IV premium. The option is cheaper, and if the stock continues trending, you profit without fighting IV crush. This is the pro move.' },
          { id: 'sell_options', label: 'Sell options BEFORE earnings to collect IV premium', description: 'Be the house, not the gambler.', isRecommended: false, explanation: 'Selling options before earnings means you COLLECT the inflated premium and profit from IV crush. This is how market makers make money. However, it requires significant capital and carries large risk if the stock moves more than expected.' },
          { id: 'avoid_earnings', label: 'Avoid options around earnings entirely', description: 'Too unpredictable for beginners.', isRecommended: false, explanation: 'Not a bad instinct for beginners. Earnings options are complex because you\'re simultaneously betting on direction, magnitude, AND volatility. Master regular options first before adding earnings complexity.' },
        ],
        glossaryTerms: ['Implied Volatility (IV)', 'Options', 'Time Decay (Theta)'],
      },
    ],
    summary: 'Options are powerful but complex. Key lessons: IV crush destroys option value after earnings, Theta eats your position every day, and buying expensive options before binary events is usually a losing strategy. Consider buying AFTER earnings when IV is low.',
  },
  {
    id: 'stop_loss_discipline',
    name: 'Stop Loss Discipline',
    subtitle: 'When to cut your losses and walk away',
    description: 'Your stock is dropping. Your gut says hold. Your plan says sell. This case teaches the hardest lesson in trading: discipline beats hope.',
    difficulty: 'Beginner',
    color: '#0EA5E9',
    durationMinutes: 10,
    learningGoals: ['Set and honor stop-loss levels', 'Understand the psychology of loss aversion', 'Learn position sizing based on risk', 'Use trailing stops to lock in gains'],
    scenarioId: null,
    decisions: [
      {
        triggerDay: 0,
        title: 'The Position Goes Against You',
        situation: 'You bought a stock at $50 based on strong fundamentals. It\'s now $46 — down 8%. No news, no earnings miss, just steady selling pressure. You set a mental stop at $47.50 (5% loss) but didn\'t place the actual order. What now?',
        choices: [
          { id: 'hold_hope', label: 'Hold and hope — it\'ll come back', description: 'No news means nothing has changed.', isRecommended: false, explanation: 'Hope is not a strategy. You already broke your own rule by not selling at $47.50. Each dollar below your stop makes it psychologically harder to sell. This is how small losses become big losses. Studies show "hold and hope" traders lose 3x more on average.' },
          { id: 'average_down', label: 'Buy more at $46 to lower your average', description: 'Better average cost means faster recovery.', isRecommended: false, explanation: 'Averaging down on a losing position is adding to a mistake. You\'re doubling your exposure to something that\'s already going wrong. Professional traders say: "Your first loss is your best loss." Averaging down is how $500 losses become $5,000 losses.' },
          { id: 'cut_loss', label: 'Sell now and accept the 8% loss', description: 'Discipline means following your rules.', isRecommended: true, explanation: 'This hurts, but it\'s the right call. You already violated your stop at $47.50 — don\'t make it worse. An 8% loss on a 10% position is only 0.8% of your portfolio. That\'s recoverable. A 30% loss isn\'t. Cut it and move on.' },
          { id: 'trailing_stop', label: 'Set a trailing stop at $44', description: 'Give it a bit more room but with a hard floor.', isRecommended: false, explanation: 'Setting a wider stop after the stock already dropped past your original stop is moving the goalposts. It\'s a compromise with your emotions. However, if you genuinely reassess and believe $44 is the right technical level, a hard stop there is better than no stop at all.' },
        ],
        glossaryTerms: ['Stop Order', 'Trailing Stop', 'Drawdown'],
      },
      {
        triggerDay: 3,
        title: 'Revenge Trading',
        situation: 'After selling at a loss, you spot another stock that looks similar to the one you just sold. You feel the urge to "make back" your loss quickly. The setup is decent but not great.',
        choices: [
          { id: 'revenge', label: 'Buy it — need to recover that loss fast', description: 'The faster you recover, the better.', isRecommended: false, explanation: 'This is "revenge trading" — one of the most destructive patterns in trading. The desire to recover a loss quickly leads to taking subpar setups with oversized positions. Each trade should be evaluated independently. Your previous loss is irrelevant to this new trade.' },
          { id: 'walk_away', label: 'Take a break — no trading today', description: 'Clear your head first.', isRecommended: true, explanation: 'Taking a break after a loss is a sign of emotional maturity. Professional traders often have a "loss limit" rule: after X losses in a day, they stop trading. Your judgment is impaired after a loss. Come back tomorrow with fresh eyes and a clear plan.' },
          { id: 'paper_trade', label: 'Paper trade it — test without risking money', description: 'See if your read is right without the risk.', isRecommended: false, explanation: 'Paper trading to validate your thesis is actually very smart. If the setup works on paper, you can take a similar setup with real money later when your emotions are in check. Not the best answer here, but far better than revenge trading.' },
        ],
        glossaryTerms: ['Position Size', 'Unrealized P&L', 'Realized P&L'],
      },
      {
        triggerDay: 5,
        title: 'Building a Trailing Stop System',
        situation: 'Your new position is working — up 12%. You want to protect your gains without capping your upside. A trailing stop follows the price up but triggers a sell if the price drops by a set amount.',
        choices: [
          { id: 'tight_trail', label: '3% trailing stop', description: 'Lock in most of the gain.', isRecommended: false, explanation: 'A 3% trailing stop protects gains well but gets triggered by normal volatility. Intraday swings of 2-3% are common — you might get stopped out on a dip that recovers the same day.' },
          { id: 'moderate_trail', label: '7% trailing stop', description: 'Balance between protection and room.', isRecommended: true, explanation: 'A 7% trailing stop is the sweet spot for most stocks. It gives enough room for normal volatility while still protecting against a real breakdown. As the stock rises, your stop rises too — you lock in gains automatically.' },
          { id: 'no_stop', label: 'No stop — just monitor manually', description: 'Check the price a few times per day.', isRecommended: false, explanation: 'Manual monitoring fails because of emotions. When the price drops, you\'ll find excuses not to sell. A trailing stop enforces your discipline mechanically — it sells when your rules say to, regardless of how you feel in the moment.' },
        ],
        glossaryTerms: ['Trailing Stop', 'Stop Order', 'Unrealized P&L'],
      },
    ],
    summary: 'Stop loss discipline separates profitable traders from the rest. The rules: always set stops BEFORE entering a trade, never move a stop further away, accept small losses gracefully, and use trailing stops to protect winners. Your first loss is your best loss.',
  },
  {
    id: 'sector_rotation_advanced',
    name: 'Sector Rotation',
    subtitle: 'Rotate your portfolio as the economy shifts',
    description: 'The Fed just raised interest rates by 0.75%. Markets are repricing everything. Some sectors will thrive, others will suffer. Can you position your portfolio ahead of the crowd?',
    difficulty: 'Intermediate',
    color: '#8B5CF6',
    durationMinutes: 15,
    learningGoals: ['Map sectors to economic cycles', 'Understand interest rate impact on different industries', 'Recognize sector correlation and decorrelation', 'Time rotations based on macro signals'],
    scenarioId: null,
    decisions: [
      {
        triggerDay: 0,
        title: 'The Rate Hike Announcement',
        situation: 'The Federal Reserve raised rates by 0.75% — the largest hike in 20 years. Your portfolio is 40% tech, 20% financials, 20% healthcare, 20% consumer staples. Tech futures are down 3% after hours.',
        choices: [
          { id: 'buy_financials', label: 'Sell tech, buy more financials', description: 'Banks profit from higher rates.', isRecommended: true, explanation: 'Banks earn the spread between what they pay depositors and what they charge borrowers. Higher rates widen this spread. Financial stocks (banks, insurance) historically outperform in rising rate environments. Reducing tech exposure makes sense because higher rates reduce the present value of future earnings — and tech is priced on future growth.' },
          { id: 'sell_tech', label: 'Sell all tech and go to cash', description: 'Tech will crash in a rate hike cycle.', isRecommended: false, explanation: 'Selling ALL tech is an overreaction. Quality tech companies with strong current earnings and low debt can weather rate hikes. The ones to sell are unprofitable "growth at any cost" companies that need low rates to survive. Be selective, not binary.' },
          { id: 'buy_utilities', label: 'Buy utilities for safety', description: 'Defensive stocks protect in uncertainty.', isRecommended: false, explanation: 'Utilities are generally defensive, but they actually suffer in rising rate environments. They carry lots of debt (borrowing costs rise) and their stable dividends become less attractive vs. rising bond yields. Utilities are a FALLING rate play, not a rising rate play.' },
          { id: 'do_nothing', label: 'Do nothing — one hike doesn\'t change everything', description: 'Market overreacts to Fed announcements.', isRecommended: false, explanation: 'Doing nothing is rarely the worst option, but a 0.75% hike is significant. It signals the Fed is serious about fighting inflation. Ignoring a major policy shift leaves you exposed. You don\'t need to panic-trade, but gradual repositioning is wise.' },
        ],
        glossaryTerms: ['Interest Rate (Fed Rate)', 'Sector Rotation', 'P/E Ratio'],
      },
      {
        triggerDay: 5,
        title: 'Second-Order Effects',
        situation: 'One week after the hike: mortgage rates jumped from 5% to 6.5%. Housing starts are dropping. Real estate stocks are down 10%. Consumer confidence is falling. But surprisingly, energy stocks are rising because the strong dollar hasn\'t slowed oil demand.',
        choices: [
          { id: 'sell_realestate', label: 'Short real estate stocks', description: 'Housing is clearly in trouble.', isRecommended: false, explanation: 'The 10% drop may have already priced in the bad news. Shorting after a major move is risky — you might be right on direction but wrong on timing. Real estate stocks could bounce before continuing lower. Selling existing real estate holdings is safer than shorting.' },
          { id: 'buy_energy', label: 'Add energy exposure', description: 'Energy benefits from inflation and strong demand.', isRecommended: true, explanation: 'Good macro thinking! In inflationary environments with rate hikes, energy often outperforms because oil prices stay elevated while other sectors struggle. Energy companies generate massive cash flow that translates to dividends and buybacks.' },
          { id: 'buy_healthcare', label: 'Increase healthcare allocation', description: 'Healthcare is defensive and rate-insensitive.', isRecommended: false, explanation: 'Healthcare IS relatively rate-insensitive, which makes it a decent safe haven. But it\'s not the optimal rotation in this environment. It won\'t lose much, but it won\'t capture the upside that financials and energy offer in a rising rate cycle.' },
        ],
        glossaryTerms: ['Sector Rotation', 'Interest Rate (Fed Rate)', 'Dividend Yield'],
      },
      {
        triggerDay: 10,
        title: 'Signs of a Pivot',
        situation: 'Three months in: unemployment is rising, retail sales are slowing, and inflation is finally coming down. The market is starting to price in rate CUTS next year. Which sectors will lead when rates eventually fall?',
        choices: [
          { id: 'back_to_tech', label: 'Rotate back into tech and growth', description: 'Lower rates boost growth stock valuations.', isRecommended: true, explanation: 'When rate cuts are anticipated, growth stocks (especially tech) rally first because lower rates increase the present value of their future earnings. Rotating from value/financials back to growth ahead of the pivot is the classic professional move.' },
          { id: 'stay_financials', label: 'Stay in financials', description: 'They\'ve been working, why change?', isRecommended: false, explanation: 'Financials led during the rate HIKE cycle, but they underperform when rates start falling (narrower margins). By the time the Fed actually cuts, financials have usually already given back gains. Rotate before the crowd.' },
          { id: 'bonds', label: 'Buy bonds before rate cuts', description: 'Bond prices rise when rates fall.', isRecommended: false, explanation: 'Buying bonds before rate cuts is actually brilliant for a fixed income strategy — but this is a stock simulation! The insight transfers though: sectors sensitive to rates (tech, real estate, utilities) act like "equity bonds" and rally when rate cuts approach.' },
        ],
        glossaryTerms: ['Sector Rotation', 'Interest Rate (Fed Rate)', 'Fair Value'],
      },
    ],
    summary: 'Sector rotation is the art of being in the right sectors at the right time. The cycle: rate hikes favor financials and energy, rate peaks favor healthcare and staples, rate cuts favor tech and real estate. Always rotate BEFORE the consensus catches on.',
  },
  {
    id: 'ipo_hype',
    name: 'The IPO Hype',
    subtitle: 'Navigate the frenzy of a hot new listing',
    description: 'The most anticipated IPO of the year is here. The company priced at $40, opened at $80, and is now trading at $120. Everyone is talking about it. FOMO is real. What do you do?',
    difficulty: 'Intermediate',
    color: '#F59E0B',
    durationMinutes: 15,
    learningGoals: ['Understand IPO mechanics and pricing', 'Learn about lock-up periods and insider selling', 'Recognize hype cycles and mean reversion', 'Avoid FOMO-driven decisions'],
    scenarioId: null,
    decisions: [
      {
        triggerDay: 0,
        title: 'IPO Day: 200% Pop',
        situation: 'TechUnicorn Inc. just IPO\'d. Priced at $40, it opened at $80 (institutions got the $40 price, not you), and in the first hour it hit $120. The company has $500M revenue but is not yet profitable. The S-1 filing shows $2B in losses last year. Everyone on social media is posting their gains.',
        choices: [
          { id: 'buy_open', label: 'Buy now at $120 — this is the next Amazon', description: 'Get in before it goes to $200.', isRecommended: false, explanation: 'Buying a 200% first-day pop is almost always a losing trade. Studies show that hot IPOs underperform the market by 20% over the first year. You\'re buying at peak euphoria — insiders who got in at $40 are the ones selling to you at $120.' },
          { id: 'wait_pullback', label: 'Wait for a pullback to $80-90', description: 'Let the hype cool off first.', isRecommended: true, explanation: 'Smart patience. Most hot IPOs pull back 30-50% from their first-day high within the first 3-6 months. Setting a buy target at $80-90 (the opening price area) gives you a much better risk/reward if you truly believe in the company.' },
          { id: 'short_hype', label: 'Short it — this is pure hype', description: 'Gravity always wins.', isRecommended: false, explanation: 'While the pullback is likely, TIMING a short on a hyped IPO is extremely dangerous. Short squeezes, limited share availability, and manic buying can push the price to irrational levels before gravity kicks in. Many short sellers have been destroyed by IPO hype.' },
          { id: 'skip', label: 'Skip entirely — plenty of other opportunities', description: 'Don\'t play a game you can\'t win.', isRecommended: false, explanation: 'Skipping isn\'t wrong — missing a trade never cost anyone money. But if the company is genuinely great, waiting for a better entry price and then buying is the optimal strategy. Don\'t let fear of hype prevent you from ever owning a good company.' },
        ],
        glossaryTerms: ['IPO', 'Fair Value', 'Short Selling'],
      },
      {
        triggerDay: 5,
        title: 'The Lock-Up Expiry Approaches',
        situation: 'It\'s been 5 months since the IPO. The stock settled at $85 after pulling back from $120. Now, the 180-day lock-up period is about to expire. This means insiders and early investors (who bought at $5-10 per share) can finally sell their shares. That\'s 400 million shares that could hit the market.',
        choices: [
          { id: 'sell_before', label: 'Sell before the lock-up expires', description: 'Avoid the flood of insider selling.', isRecommended: true, explanation: 'Lock-up expiry is one of the most reliable predictable events in stock markets. On average, stocks drop 3-5% around lock-up expiry. With 400M shares potentially hitting the market, the selling pressure could be significant. Selling before and buying back after is the smart play.' },
          { id: 'hold_through', label: 'Hold through — it\'s already priced in', description: 'Everyone knows about the lock-up.', isRecommended: false, explanation: '"Priced in" is the most dangerous phrase in investing. While the market IS aware of lock-up dates, the actual magnitude of selling is unknown until it happens. Some IPOs see 10-15% drops at lock-up. The risk/reward of holding through is unfavorable.' },
          { id: 'buy_lockup', label: 'Buy more — lock-up fear creates opportunity', description: 'Insiders won\'t all sell at once.', isRecommended: false, explanation: 'It\'s true that not all insiders sell immediately, and some lock-up drops create buying opportunities. But buying BEFORE the lock-up is premature. Wait for the lock-up to actually expire, see how much selling occurs, and then buy the dip if the company is still strong.' },
        ],
        glossaryTerms: ['IPO', 'Short Selling', 'Fair Value'],
      },
      {
        triggerDay: 10,
        title: 'One Year Later: Reality Check',
        situation: 'The IPO hype is long gone. The stock is at $55 — still above the $40 IPO price but way below the $120 peak. The company is growing revenue but still burning cash. Early buyers at $120 are down 54%. Was the company ever worth $120?',
        choices: [
          { id: 'buy_value', label: 'Buy now — the company is finally fairly valued', description: 'Revenue growth + lower price = opportunity.', isRecommended: true, explanation: 'After a year, you can evaluate the company on actual performance rather than hype. If revenue is growing and the path to profitability is clear, $55 might be a great entry. The key difference: you\'re making a decision based on data, not FOMO. This is how successful investors operate.' },
          { id: 'avoid', label: 'Still too risky — wait for profitability', description: 'Unprofitable companies are binary bets.', isRecommended: false, explanation: 'Waiting for profitability is conservative but valid. The downside: by the time a growth company turns profitable, the stock has often already doubled. The market prices in future profitability 12-18 months before it arrives. You might miss the run.' },
          { id: 'lesson_learned', label: 'Never buy IPOs — lesson learned', description: 'The game is rigged for insiders.', isRecommended: false, explanation: 'The first-day IPO game IS rigged — institutions get the best price. But the "never buy IPOs" rule would have kept you out of Amazon, Google, and many other great companies. The lesson isn\'t "avoid IPOs" — it\'s "avoid IPO DAY pricing." Wait 6-12 months, then evaluate.' },
        ],
        glossaryTerms: ['IPO', 'Fair Value', 'P/E Ratio'],
      },
    ],
    summary: 'IPO hype is one of the strongest FOMO triggers in markets. The rules: never buy on IPO day (you\'re buying at peak euphoria), watch for the lock-up expiry drop, and wait 6-12 months before evaluating. The best IPO strategy is patience.',
  },
  {
    id: 'black_swan',
    name: 'Black Swan',
    subtitle: 'When the unthinkable happens',
    description: 'An unexpected global event has just crashed markets 20% in a week. Nobody saw it coming. Your portfolio is devastated. Panic is everywhere. But history shows: the greatest fortunes are made in the darkest hours.',
    difficulty: 'Advanced',
    color: '#1F2937',
    durationMinutes: 20,
    learningGoals: ['Understand tail risk and black swan events', 'Learn hedging strategies for catastrophic scenarios', 'Find opportunity in crisis', 'Build an anti-fragile portfolio'],
    scenarioId: 'the_crash',
    decisions: [
      {
        triggerDay: 0,
        title: 'Day 1: Markets Down 12%',
        situation: 'An unexpected event (think: pandemic, financial crisis, geopolitical shock) has just rocked global markets. The S&P 500 is down 12% in a single day — the worst since 2008. Trading volumes are 5x normal. The VIX is at 65. Your portfolio is down 15% because you held growth stocks.',
        choices: [
          { id: 'sell_everything', label: 'Sell everything — preserve what\'s left', description: 'Cut losses before it gets worse.', isRecommended: false, explanation: 'Selling after a 12% crash means you\'re selling into maximum fear with maximum slippage. The bid/ask spreads are enormous. You\'ll get terrible fills. And historically, selling on the worst day of a crisis locks in losses right near the bottom. The 2020 crash recovered in 5 months.' },
          { id: 'hedge_puts', label: 'Buy protective puts on your positions', description: 'Limit further downside with insurance.', isRecommended: false, explanation: 'Protective puts AFTER a crash are painfully expensive. The VIX at 65 means options premiums are 3-4x their normal price. You\'re buying insurance after the house is on fire. The time to buy puts was BEFORE the crisis. Still, if you can\'t stomach more downside, expensive insurance beats panic selling.' },
          { id: 'buy_blue_chips', label: 'Start buying quality blue chips slowly', description: 'Decade-defining opportunity in great companies.', isRecommended: true, explanation: 'Warren Buffett\'s famous quote: "Be fearful when others are greedy, and greedy when others are fearful." The VIX at 65 means MAXIMUM FEAR. Great companies are on sale at prices you may never see again. The key word is SLOWLY — don\'t deploy all cash at once. Buy in tranches over days/weeks.' },
          { id: 'go_cash', label: 'Go to cash and wait for stability', description: 'Cash is king in a crisis.', isRecommended: false, explanation: 'Going to cash feels safe but is a form of selling the bottom. You\'ll feel "safe" while missing the recovery. The problem: when do you get back in? Most investors who sell during crashes wait too long and buy back higher than where they sold. The emotional damage makes it hard to re-enter.' },
        ],
        glossaryTerms: ['Circuit Breaker', 'Flash Crash', 'Fear & Greed Index'],
      },
      {
        triggerDay: 5,
        title: 'Week 2: Markets Down 20% Total',
        situation: 'The crisis deepens. Markets are now down 20% from pre-crisis levels. Companies are issuing profit warnings. Unemployment claims are spiking. The news is relentlessly negative. But you notice: some companies (cloud, delivery, healthcare) are actually seeing increased demand.',
        choices: [
          { id: 'crisis_winners', label: 'Buy companies that benefit from the crisis', description: 'Some businesses thrive in chaos.', isRecommended: true, explanation: 'Every crisis creates winners. In 2020: Zoom, Amazon, Moderna. In 2008: dollar stores, bankruptcy lawyers, gold miners. Identifying crisis beneficiaries early is one of the most profitable strategies. The key: look for companies whose products become MORE needed during the crisis.' },
          { id: 'beaten_down', label: 'Buy the most beaten-down stocks', description: 'The bigger the drop, the bigger the rebound.', isRecommended: false, explanation: 'The most beaten-down stocks are often beaten down for a reason — they may not survive the crisis. Airlines, cruise lines, and restaurants dropped 80%+ in 2020. Some recovered. Some went bankrupt. Buying the deepest dips is a coin flip, not a strategy.' },
          { id: 'stay_put', label: 'Make no changes — ride it out', description: 'Don\'t try to be clever in a crisis.', isRecommended: false, explanation: 'Doing nothing is better than panic selling, but a crisis also presents rare opportunities to upgrade your portfolio. You can swap weaker holdings for stronger ones at similar prices. Inaction is safe but not optimal.' },
        ],
        glossaryTerms: ['Drawdown', 'Diversification', 'Fair Value'],
      },
      {
        triggerDay: 15,
        title: 'Month 2: The False Recovery',
        situation: 'Markets bounced 10% from the lows. Headlines read "The worst is over." But you know: bear market rallies are common and often trap buyers. The VIX is still at 35 (high). Unemployment is still rising. Is this the real recovery or a dead cat bounce?',
        choices: [
          { id: 'all_in', label: 'Go all-in — the bottom is in', description: 'The market is forward-looking, buy now.', isRecommended: false, explanation: 'Going all-in on a 10% bounce is premature. Bear markets typically have multiple 5-15% rallies before the real bottom. The 2008 crisis had 6 "false bottoms" before the actual low. Deploy capital gradually, not all at once.' },
          { id: 'gradual', label: 'Continue buying in small tranches', description: 'Add 5-10% of cash reserves each week.', isRecommended: true, explanation: 'Dollar-cost averaging during a crisis is the optimal strategy. You might not catch the exact bottom, but you don\'t need to. Buying consistently at depressed levels means your average cost will be excellent. This is how patient investors build generational wealth.' },
          { id: 'wait_confirmation', label: 'Wait for clear confirmation of recovery', description: 'Need to see improving economic data first.', isRecommended: false, explanation: 'Waiting for economic confirmation sounds prudent, but markets lead the economy by 6-9 months. By the time unemployment falls and GDP recovers, stocks have already rallied 40-60% from the lows. The market recovers BEFORE the news gets better.' },
        ],
        glossaryTerms: ['Drawdown', 'Fear & Greed Index', 'Fair Value'],
      },
      {
        triggerDay: 25,
        title: 'Month 6: Recovery Confirmed',
        situation: 'Markets have recovered 80% of the crash losses. Your gradual buying strategy means you\'re now in profit. But the scars remain — how do you build a portfolio that\'s better prepared for the next black swan?',
        choices: [
          { id: 'barbell', label: 'Barbell strategy: 80% safe, 20% aggressive', description: 'Protect the core, swing for fences with the rest.', isRecommended: true, explanation: 'The barbell strategy (coined by Nassim Taleb) puts most of your money in ultra-safe assets and a small portion in high-risk/high-reward positions. You survive any crash (safe portion) while having asymmetric upside (aggressive portion). This is the anti-fragile approach.' },
          { id: 'permanent_hedge', label: 'Always hold 10% in puts/gold', description: 'Permanent insurance against crashes.', isRecommended: false, explanation: 'Permanent hedges protect against crashes but cost 2-3% annually in premiums and opportunity cost. Over 10 years without a major crash, that\'s 20-30% of returns sacrificed for insurance. Better to use the barbell approach — it\'s cheaper and equally protective.' },
          { id: 'same_portfolio', label: 'Go back to the same portfolio — crashes are rare', description: 'Don\'t over-prepare for unlikely events.', isRecommended: false, explanation: 'Crashes are rare but devastating. Doing nothing to prepare means you\'ll be in the same panic next time. The goal isn\'t to predict crashes — it\'s to build a portfolio that survives them AND benefits from them. That requires structural changes, not just hope.' },
        ],
        glossaryTerms: ['Diversification', 'Drawdown', 'Position Size'],
      },
    ],
    summary: 'Black swans are by definition unpredictable, but your response doesn\'t have to be. The playbook: don\'t sell into panic, buy quality in tranches during the crisis, identify crisis beneficiaries, and build an anti-fragile portfolio for next time. The biggest fortunes in history were built by buying when others were terrified.',
  },
];
