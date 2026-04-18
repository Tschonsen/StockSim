namespace StockSim.Engine.Models;

/// <summary>
/// Predefined game scenario with specific start conditions and win/lose criteria.
/// Spec 1.4.2: Scenario Mode / Challenges.
/// </summary>
public class Scenario
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Difficulty { get; set; } = "Normal";

    // Start conditions
    public decimal StartingCash { get; set; } = 50_000;
    public MarketPhase? ForcePhase { get; set; }
    public decimal VolatilityMultiplier { get; set; } = 1.0m;
    public decimal EventFrequencyMultiplier { get; set; } = 1.0m;

    // Win/Lose conditions
    public decimal? TargetPortfolioValue { get; set; }
    public decimal? TargetDividendIncome { get; set; }
    public decimal? MaxLossPercent { get; set; }
    public int? TimeLimitDays { get; set; }
    public bool SurvivalMode { get; set; } // Just survive with positive portfolio

    // Special rules
    public bool OnlyOneStock { get; set; }
    public bool OnlyPennyStocks { get; set; }
    public bool OnlyDividendStocks { get; set; }
    public bool NoSaveAllowed { get; set; }

    // History Mode
    public string? HistoricalContext { get; set; }
    public string? HistoricalDate { get; set; }
    public decimal? ActualMarketReturn { get; set; }
    public string? ForceArcId { get; set; }

    // Runtime state
    public bool IsActive { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsWon { get; set; }
    public int DaysElapsed { get; set; }

    /// <summary>All predefined scenarios from the Spec.</summary>
    public static List<Scenario> GetAll()
    {
        return new List<Scenario>
        {
            new()
            {
                Id = "the_crash", Name = "The Crash",
                Description = "Market is crashing -40%. Survive with a positive portfolio.",
                Difficulty = "Hard",
                StartingCash = 50_000, ForcePhase = MarketPhase.Bear,
                VolatilityMultiplier = 2.0m, EventFrequencyMultiplier = 2.0m,
                SurvivalMode = true, TimeLimitDays = 126, // 6 months
            },
            new()
            {
                Id = "bull_run", Name = "Bull Run",
                Description = "Strong bull market. Turn $25k into $100k.",
                Difficulty = "Normal",
                StartingCash = 25_000, ForcePhase = MarketPhase.Bull,
                TargetPortfolioValue = 100_000, TimeLimitDays = 63, // 3 months
            },
            new()
            {
                Id = "short_squeeze", Name = "Short Squeeze",
                Description = "A stock has 60% short interest. Spot and profit from the squeeze. Target: $80k.",
                Difficulty = "Hard",
                StartingCash = 30_000,
                TargetPortfolioValue = 80_000, TimeLimitDays = 21, // 1 month
            },
            new()
            {
                Id = "one_stock", Name = "One Stock",
                Description = "You may only trade ONE stock. No diversification. Reach $150k.",
                Difficulty = "Hard",
                StartingCash = 50_000,
                OnlyOneStock = true, TargetPortfolioValue = 150_000, TimeLimitDays = 252, // 1 year
            },
            new()
            {
                Id = "recession", Name = "Recession",
                Description = "Economy in recession. High rates, falling markets. Survive with less than -10% loss.",
                Difficulty = "Hard",
                StartingCash = 100_000, ForcePhase = MarketPhase.Bear,
                VolatilityMultiplier = 1.5m,
                MaxLossPercent = 10, TimeLimitDays = 252,
            },
            new()
            {
                Id = "penny_stocks", Name = "Penny Stocks",
                Description = "Only trade stocks under $5. Turn $10k into $50k.",
                Difficulty = "Hard",
                StartingCash = 10_000,
                OnlyPennyStocks = true, TargetPortfolioValue = 50_000, TimeLimitDays = 126,
            },
            new()
            {
                Id = "dividend_king", Name = "Dividend King",
                Description = "Build a dividend portfolio generating $5k/quarter in passive income.",
                Difficulty = "Normal",
                StartingCash = 100_000,
                OnlyDividendStocks = true, TargetDividendIncome = 5_000, TimeLimitDays = 504, // 2 years
            },
            new()
            {
                Id = "speed_run", Name = "Speed Run",
                Description = "Normal market. Reach $1M as fast as possible. No time limit.",
                Difficulty = "Normal",
                StartingCash = 50_000,
                TargetPortfolioValue = 1_000_000,
            },
            new()
            {
                Id = "iron_man", Name = "Iron Man",
                Description = "No saving allowed. Survive 1 year with more than $50k.",
                Difficulty = "Brutal",
                StartingCash = 50_000,
                NoSaveAllowed = true, TargetPortfolioValue = 50_000, TimeLimitDays = 252,
            },
            new()
            {
                Id = "day_trader", Name = "Day Trader",
                Description = "Start with $25k. Make $50k through active day trading. High volatility.",
                Difficulty = "Hard",
                StartingCash = 25_000,
                VolatilityMultiplier = 1.8m, EventFrequencyMultiplier = 1.5m,
                TargetPortfolioValue = 75_000, TimeLimitDays = 63,
            },
            // --- New scenarios ---
            new()
            {
                Id = "the_big_short", Name = "The Big Short",
                Description = "Economy in late expansion with rising debt. Short overvalued stocks before the crash hits.",
                Difficulty = "Hard",
                StartingCash = 75_000, ForcePhase = MarketPhase.Bull,
                VolatilityMultiplier = 1.6m, EventFrequencyMultiplier = 1.8m,
                TargetPortfolioValue = 200_000, TimeLimitDays = 120,
            },
            new()
            {
                Id = "dot_com_bubble", Name = "Dot-Com Bubble",
                Description = "Tech sector is wildly overvalued. Survive with more than $80k after the bubble pops.",
                Difficulty = "Hard",
                StartingCash = 100_000, ForcePhase = MarketPhase.Bull,
                VolatilityMultiplier = 2.5m, EventFrequencyMultiplier = 2.0m,
                TargetPortfolioValue = 80_000, TimeLimitDays = 180,
                SurvivalMode = true,
            },
            new()
            {
                Id = "pandemic_panic", Name = "Pandemic Panic",
                Description = "Sudden 30% market crash followed by a V-shaped recovery. Profit from the chaos.",
                Difficulty = "Normal",
                StartingCash = 100_000, ForcePhase = MarketPhase.Bear,
                VolatilityMultiplier = 2.0m, EventFrequencyMultiplier = 1.5m,
                TargetPortfolioValue = 150_000, TimeLimitDays = 90,
            },
            new()
            {
                Id = "inflation_hedge", Name = "Inflation Hedge",
                Description = "Rising inflation erodes purchasing power. Your portfolio must grow more than 15% to beat it.",
                Difficulty = "Normal",
                StartingCash = 100_000, ForcePhase = MarketPhase.Neutral,
                VolatilityMultiplier = 1.3m, EventFrequencyMultiplier = 1.2m,
                TargetPortfolioValue = 115_000, TimeLimitDays = 252,
            },
            new()
            {
                Id = "the_squeeze", Name = "The Squeeze",
                Description = "Multiple heavily-shorted stocks are primed for a squeeze. Find them and ride the wave to $150k.",
                Difficulty = "Hard",
                StartingCash = 50_000,
                VolatilityMultiplier = 2.2m, EventFrequencyMultiplier = 1.5m,
                TargetPortfolioValue = 150_000, TimeLimitDays = 60,
            },
            new()
            {
                Id = "dividend_king_v2", Name = "Dividend King II",
                Description = "Build a $200k income portfolio. Collect $10k in total dividends. Pure income investing.",
                Difficulty = "Easy",
                StartingCash = 200_000, ForcePhase = MarketPhase.Neutral,
                OnlyDividendStocks = true, TargetDividendIncome = 10_000, TimeLimitDays = 504,
            },
            new()
            {
                Id = "day_trader_v2", Name = "Day Trader II",
                Description = "PDT minimum $25k. Grow to $50k through frequent trading. High commissions eat your profits.",
                Difficulty = "Hard",
                StartingCash = 25_000,
                VolatilityMultiplier = 2.0m, EventFrequencyMultiplier = 2.0m,
                TargetPortfolioValue = 50_000, TimeLimitDays = 60,
            },
            new()
            {
                Id = "bear_market_survivor", Name = "Bear Market Survivor",
                Description = "Persistent bear market with no relief. Don't lose more than 20% of your starting capital.",
                Difficulty = "Normal",
                StartingCash = 100_000, ForcePhase = MarketPhase.Bear,
                VolatilityMultiplier = 1.5m, EventFrequencyMultiplier = 1.3m,
                MaxLossPercent = 20, TimeLimitDays = 180,
            },
            new()
            {
                Id = "options_master", Name = "Options Master",
                Description = "Use options strategies to double your money. Calls, puts, spreads — master them all.",
                Difficulty = "Hard",
                StartingCash = 50_000, ForcePhase = MarketPhase.Neutral,
                VolatilityMultiplier = 1.4m,
                TargetPortfolioValue = 100_000, TimeLimitDays = 120,
            },
            new()
            {
                Id = "from_nothing", Name = "From Nothing",
                Description = "Start with just $10k. Reach $100k — a 10x return. The ultimate challenge with no time limit.",
                Difficulty = "Brutal",
                StartingCash = 10_000,
                VolatilityMultiplier = 1.0m,
                TargetPortfolioValue = 100_000,
            },
            // --- Scenarios 21-30 ---
            new()
            {
                Id = "commodity_king", Name = "Commodity King",
                Description = "Commodities are booming. Ride the wave with GLD, SLV, USO and turn $75k into $150k.",
                Difficulty = "Easy",
                StartingCash = 75_000, ForcePhase = MarketPhase.Bull,
                VolatilityMultiplier = 1.2m, EventFrequencyMultiplier = 1.0m,
                TargetPortfolioValue = 150_000, TimeLimitDays = 252,
            },
            new()
            {
                Id = "etf_only", Name = "ETF Only",
                Description = "Only ETFs allowed. No individual stocks. Prove passive investing can double your money.",
                Difficulty = "Normal",
                StartingCash = 50_000, ForcePhase = MarketPhase.Neutral,
                VolatilityMultiplier = 0.8m, EventFrequencyMultiplier = 1.0m,
                TargetPortfolioValue = 100_000, TimeLimitDays = 252,
            },
            new()
            {
                Id = "sector_rotation", Name = "Sector Rotation",
                Description = "The economy shifts through cycles. Rotate between sectors at the right time to reach $175k.",
                Difficulty = "Normal",
                StartingCash = 100_000, ForcePhase = MarketPhase.Neutral,
                VolatilityMultiplier = 1.2m, EventFrequencyMultiplier = 1.3m,
                TargetPortfolioValue = 175_000, TimeLimitDays = 504,
            },
            new()
            {
                Id = "flash_crash", Name = "Flash Crash",
                Description = "An engineered flash crash hammers the market. Survive 60 days without dropping below $70k.",
                Difficulty = "Hard",
                StartingCash = 100_000, ForcePhase = MarketPhase.Bear,
                VolatilityMultiplier = 3.0m, EventFrequencyMultiplier = 2.5m,
                TargetPortfolioValue = 70_000, TimeLimitDays = 60,
                SurvivalMode = true,
            },
            new()
            {
                Id = "earnings_season", Name = "Earnings Season",
                Description = "Earnings reports are dropping fast. Trade the reactions and grow $50k to $80k in one quarter.",
                Difficulty = "Normal",
                StartingCash = 50_000, ForcePhase = MarketPhase.Neutral,
                VolatilityMultiplier = 1.3m, EventFrequencyMultiplier = 1.5m,
                TargetPortfolioValue = 80_000, TimeLimitDays = 90,
            },
            new()
            {
                Id = "margin_call", Name = "Margin Call",
                Description = "Aggressive margin trading with $30k. High risk, high reward. Reach $100k or get wiped out.",
                Difficulty = "Hard",
                StartingCash = 30_000, ForcePhase = MarketPhase.Neutral,
                VolatilityMultiplier = 2.0m, EventFrequencyMultiplier = 1.5m,
                TargetPortfolioValue = 100_000, TimeLimitDays = 120,
            },
            new()
            {
                Id = "gold_rush", Name = "Gold Rush",
                Description = "Gold is surging on geopolitical fears. Ride the golden wave from $50k to $80k in 3 months.",
                Difficulty = "Easy",
                StartingCash = 50_000, ForcePhase = MarketPhase.Bull,
                VolatilityMultiplier = 1.0m, EventFrequencyMultiplier = 1.2m,
                TargetPortfolioValue = 80_000, TimeLimitDays = 63,
            },
            new()
            {
                Id = "black_monday", Name = "Black Monday",
                Description = "Markets crash 20% on day one. Start at $200k and claw your way back to even over one year.",
                Difficulty = "Brutal",
                StartingCash = 200_000, ForcePhase = MarketPhase.Bear,
                VolatilityMultiplier = 2.5m, EventFrequencyMultiplier = 2.0m,
                TargetPortfolioValue = 200_000, TimeLimitDays = 252,
            },
            new()
            {
                Id = "value_investor", Name = "Value Investor",
                Description = "Find undervalued stocks in a calm market. Patience pays — grow $100k to $150k over 2 years.",
                Difficulty = "Normal",
                StartingCash = 100_000, ForcePhase = MarketPhase.Neutral,
                VolatilityMultiplier = 0.7m, EventFrequencyMultiplier = 0.8m,
                TargetPortfolioValue = 150_000, TimeLimitDays = 504,
            },
            new()
            {
                Id = "the_apprentice", Name = "The Apprentice",
                Description = "Your first steps on Wall Street. A gentle bull market — just grow $25k to $30k. You got this.",
                Difficulty = "Easy",
                StartingCash = 25_000, ForcePhase = MarketPhase.Bull,
                VolatilityMultiplier = 0.6m, EventFrequencyMultiplier = 0.7m,
                TargetPortfolioValue = 30_000, TimeLimitDays = 63,
            },

            // === HISTORY MODE (9 scenarios) ===
            new()
            {
                Id = "history_black_monday", Name = "Black Monday 1987",
                Description = "October 19, 1987. The Dow crashes 22.6% in a single day — the worst one-day percentage decline in history. Can you survive?",
                Difficulty = "Hard",
                StartingCash = 100_000, ForcePhase = MarketPhase.Bear,
                VolatilityMultiplier = 2.5m, EventFrequencyMultiplier = 2.0m,
                SurvivalMode = true, MaxLossPercent = 50, TimeLimitDays = 60,
                HistoricalDate = "October 19, 1987", ActualMarketReturn = -22.6m,
                HistoricalContext = "Portfolio insurance selling triggered a cascade. Circuit breakers didn't exist yet. The Fed flooded markets with liquidity the next day.",
                ForceArcId = "lehman_moment",
            },
            new()
            {
                Id = "history_dotcom", Name = "Dot-Com Bubble 2000",
                Description = "March 2000. Tech stocks at insane valuations. The NASDAQ is about to lose 78% over the next 2.5 years. Get out in time — or profit from the crash.",
                Difficulty = "Hard",
                StartingCash = 100_000, ForcePhase = MarketPhase.Bull,
                VolatilityMultiplier = 2.0m, EventFrequencyMultiplier = 1.8m,
                TargetPortfolioValue = 150_000, TimeLimitDays = 252,
                HistoricalDate = "March 2000", ActualMarketReturn = -49.1m,
                HistoricalContext = "Companies with no revenue had $10B+ market caps. 'This time is different' was the mantra. It wasn't.",
                ForceArcId = "tech_bubble",
            },
            new()
            {
                Id = "history_2008", Name = "Financial Crisis 2008",
                Description = "September 2008. Lehman Brothers has collapsed. Banks are failing. Credit is frozen. The S&P 500 will fall 57% from peak. Navigate the worst crisis since the Great Depression.",
                Difficulty = "Brutal",
                StartingCash = 200_000, ForcePhase = MarketPhase.Bear,
                VolatilityMultiplier = 2.5m, EventFrequencyMultiplier = 2.0m,
                SurvivalMode = true, MaxLossPercent = 40, TimeLimitDays = 252,
                HistoricalDate = "September 15, 2008", ActualMarketReturn = -38.5m,
                HistoricalContext = "Subprime mortgages, CDOs, credit default swaps. 'Too big to fail' entered the lexicon. TARP bailout: $700 billion.",
                ForceArcId = "bank_run",
            },
            new()
            {
                Id = "history_flash_crash", Name = "Flash Crash 2010",
                Description = "May 6, 2010. The Dow plunges 1,000 points in minutes — then recovers most of the loss within 20 minutes. Trade the chaos.",
                Difficulty = "Hard",
                StartingCash = 50_000, ForcePhase = MarketPhase.Neutral,
                VolatilityMultiplier = 3.0m, EventFrequencyMultiplier = 2.5m,
                TargetPortfolioValue = 80_000, TimeLimitDays = 30,
                HistoricalDate = "May 6, 2010", ActualMarketReturn = -3.2m,
                HistoricalContext = "A single large sell order in E-mini S&P futures triggered a cascade. Some stocks briefly traded at $0.01. New circuit breakers were introduced.",
                ForceArcId = "flash_crash_systemic",
            },
            new()
            {
                Id = "history_covid", Name = "COVID Crash 2020",
                Description = "March 2020. Global pandemic. Markets crash 34% in 23 days — the fastest bear market in history. Then the Fed prints money and markets V-recover. Ride the wave.",
                Difficulty = "Normal",
                StartingCash = 100_000, ForcePhase = MarketPhase.Bear,
                VolatilityMultiplier = 2.0m, EventFrequencyMultiplier = 1.8m,
                TargetPortfolioValue = 150_000, TimeLimitDays = 180,
                HistoricalDate = "February 19, 2020", ActualMarketReturn = -33.9m,
                HistoricalContext = "Lockdowns, circuit breakers triggered 4 times in 10 days, oil went negative. Then: unlimited QE, stimulus checks, and the fastest recovery ever.",
                ForceArcId = "pandemic_shock",
            },
            new()
            {
                Id = "history_gamestop", Name = "GameStop Squeeze 2021",
                Description = "January 2021. Reddit's WallStreetBets takes on hedge funds. GameStop goes from $20 to $483 in days. Short sellers lose $13 billion. Join the revolution.",
                Difficulty = "Hard",
                StartingCash = 25_000, ForcePhase = MarketPhase.Bull,
                VolatilityMultiplier = 2.5m, EventFrequencyMultiplier = 2.0m,
                TargetPortfolioValue = 100_000, TimeLimitDays = 60,
                HistoricalDate = "January 2021", ActualMarketReturn = 0m,
                HistoricalContext = "Short interest exceeded 140% of float. Robinhood restricted buying. Congressional hearings followed. 'Diamond Hands' became a cultural meme.",
                ForceArcId = "meme_squeeze",
            },
            new()
            {
                Id = "history_volcker", Name = "Volcker Shock 1980",
                Description = "1980. Fed Chairman Paul Volcker raises interest rates to 20% to crush inflation. The economy enters a severe recession. Bond yields are astronomical. Adapt or perish.",
                Difficulty = "Hard",
                StartingCash = 100_000, ForcePhase = MarketPhase.Bear,
                VolatilityMultiplier = 1.8m, EventFrequencyMultiplier = 1.5m,
                SurvivalMode = true, MaxLossPercent = 30, TimeLimitDays = 252,
                HistoricalDate = "1980", ActualMarketReturn = -27.1m,
                HistoricalContext = "Inflation hit 14.8%. Mortgage rates reached 18%. Volcker's tough medicine worked — inflation fell to 3.2% by 1983, setting up the greatest bull market in history.",
                ForceArcId = "oil_shock",
            },
            new()
            {
                Id = "history_oil_2020", Name = "Oil Price War 2020",
                Description = "April 2020. Saudi-Russia price war + COVID demand collapse. Oil futures go NEGATIVE for the first time in history. Energy stocks are decimated.",
                Difficulty = "Normal",
                StartingCash = 75_000, ForcePhase = MarketPhase.Bear,
                VolatilityMultiplier = 2.0m, EventFrequencyMultiplier = 1.8m,
                TargetPortfolioValue = 120_000, TimeLimitDays = 180,
                HistoricalDate = "April 20, 2020", ActualMarketReturn = -44.0m,
                HistoricalContext = "WTI crude traded at -$37.63/barrel. Tankers became floating storage. Energy companies went bankrupt in waves. OPEC+ eventually agreed to historic production cuts.",
                ForceArcId = "oil_shock",
            },
            new()
            {
                Id = "history_ai_bubble", Name = "AI Bubble 202X",
                Description = "The AI hype is at fever pitch. Every company is an 'AI company' now. Tech valuations are stretched to breaking point. Is this time different? Or is this Dot-Com 2.0?",
                Difficulty = "Normal",
                StartingCash = 100_000, ForcePhase = MarketPhase.Bull,
                VolatilityMultiplier = 1.8m, EventFrequencyMultiplier = 1.5m,
                TargetPortfolioValue = 200_000, TimeLimitDays = 504,
                HistoricalDate = "202X", ActualMarketReturn = 0m,
                HistoricalContext = "Nvidia at $3T market cap. 'Generative AI will change everything.' But most companies can't show AI revenue yet. The outcome is uncertain.",
                ForceArcId = "ai_revolution",
            },
        };
    }
}

/// <summary>
/// Result of a completed scenario.
/// </summary>
public class ScenarioResult
{
    public string ScenarioId { get; set; } = "";
    public string ScenarioName { get; set; } = "";
    public bool Won { get; set; }
    public int DaysElapsed { get; set; }
    public decimal FinalPortfolioValue { get; set; }
    public decimal TotalReturn { get; set; }
    public decimal TotalReturnPercent { get; set; }
    public int TotalTrades { get; set; }
    public decimal WinRate { get; set; }
    public string FailReason { get; set; } = "";
}
