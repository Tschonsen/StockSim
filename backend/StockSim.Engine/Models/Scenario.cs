namespace StockSim.Engine.Models;

/// <summary>
/// Predefined game scenario with specific start conditions and win/lose criteria.
/// Bible 1.4.2: Scenario Mode / Challenges.
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

    // Runtime state
    public bool IsActive { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsWon { get; set; }
    public int DaysElapsed { get; set; }

    /// <summary>All predefined scenarios from the Bible.</summary>
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
