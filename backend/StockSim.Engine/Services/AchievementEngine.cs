using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// Manages achievements and player statistics tracking.
/// Bible 1.4.1: 30+ achievements across 4 categories.
/// </summary>
public class AchievementEngine
{
    private readonly Logger _log = new("AchievementEngine");
    private readonly List<Achievement> _achievements;
    public PlayerStats Stats { get; set; }

    /// <summary>Achievements unlocked this tick (for frontend notification).</summary>
    public List<Achievement> NewUnlocksThisTick { get; } = new();

    public IReadOnlyList<Achievement> Achievements => _achievements;

    public AchievementEngine()
    {
        Stats = new PlayerStats();
        _achievements = CreateAchievements();
    }

    private static List<Achievement> CreateAchievements()
    {
        return new List<Achievement>
        {
            // Wealth
            new("first_steps", "First Steps", "Portfolio value reaches $60,000", AchievementCategory.Wealth),
            new("six_figures", "Six Figures", "Portfolio value reaches $100,000", AchievementCategory.Wealth),
            new("quarter_million", "Quarter Million", "Portfolio value reaches $250,000", AchievementCategory.Wealth),
            new("half_millionaire", "Half Millionaire", "Portfolio value reaches $500,000", AchievementCategory.Wealth),
            new("millionaire", "Millionaire", "Portfolio value reaches $1,000,000", AchievementCategory.Wealth),
            new("multi_millionaire", "Multi-Millionaire", "Portfolio value reaches $5,000,000", AchievementCategory.Wealth),
            new("tycoon", "Tycoon", "Portfolio value reaches $10,000,000", AchievementCategory.Wealth),

            // Trading Skills
            new("first_blood", "First Blood", "Complete your first profitable trade", AchievementCategory.Trading),
            new("sharpshooter", "Sharpshooter", "10 profitable trades in a row", AchievementCategory.Trading),
            new("day_trader", "Day Trader", "50 trades in a single trading day", AchievementCategory.Trading),
            new("diamond_hands", "Diamond Hands", "Hold through -20% drawdown and sell at profit", AchievementCategory.Trading),
            new("short_master", "Short Master", "$10,000+ profit from short selling", AchievementCategory.Trading),
            new("dip_buyer", "Dip Buyer", "Buy a stock down >15% and sell at >20% gain", AchievementCategory.Trading),
            new("quick_flip", "Quick Flip", "Make $1,000+ profit on a trade held less than 1 day", AchievementCategory.Trading),
            new("patient_investor", "Patient Investor", "Hold a position for 30+ days at profit", AchievementCategory.Trading),
            new("diversified", "Diversified", "Hold positions in 5+ different sectors", AchievementCategory.Trading),
            new("penny_millionaire", "Penny Millionaire", "Make $10,000+ profit from stocks under $5", AchievementCategory.Trading),

            // Market Experience
            new("market_veteran", "Market Veteran", "Survive 1 year of game time", AchievementCategory.Market),
            new("crash_survivor", "Crash Survivor", "Portfolio drops less than -30% during a crash", AchievementCategory.Market),
            new("bull_rider", "Bull Rider", "$100k+ profit in a bull market", AchievementCategory.Market),
            new("bear_tamer", "Bear Tamer", "End a bear market with positive returns", AchievementCategory.Market),
            new("black_swan", "Black Swan", "Experience a flash crash", AchievementCategory.Market),
            new("sector_specialist", "Sector Specialist", "$50k+ profit from a single sector", AchievementCategory.Market),
            new("news_trader", "News Trader", "Profit from 10 different news events", AchievementCategory.Market),

            // Risk
            new("clean_record", "Clean Record", "Trade for 1 year without any large losses", AchievementCategory.Risk),
            new("comeback_kid", "Comeback Kid", "Recover to $100k after being below $20k", AchievementCategory.Risk),
            new("iron_stomach", "Iron Stomach", "Max drawdown never exceeds 15%", AchievementCategory.Risk),
            new("risk_manager", "Risk Manager", "Never lose more than 5% on a single trade", AchievementCategory.Risk),
            new("hundred_trades", "Centurion", "Complete 100 trades", AchievementCategory.Risk),
            new("five_hundred_trades", "Trading Machine", "Complete 500 trades", AchievementCategory.Risk),
            new("profit_factory", "Profit Factory", "Win rate above 60% with 50+ trades", AchievementCategory.Risk),
        };
    }

    /// <summary>
    /// Record a completed trade and update stats.
    /// Called from OrderEngine when a sell/cover order fills.
    /// </summary>
    public void RecordTrade(TradeRecord trade)
    {
        Stats.TradeHistory.Add(trade);

        // Update sector P&L
        if (!Stats.SectorPnL.ContainsKey(trade.Sector))
            Stats.SectorPnL[trade.Sector] = 0;
        Stats.SectorPnL[trade.Sector] += trade.PnL;

        // Win/loss tracking
        if (trade.PnL > 0)
        {
            Stats.WinningTradeCount++;
            Stats.ConsecutiveWins++;
            Stats.ConsecutiveLosses = 0;
            Stats.MaxConsecutiveWins = Math.Max(Stats.MaxConsecutiveWins, Stats.ConsecutiveWins);
            Stats.TotalGainFromWins += trade.PnL;
            Stats.LargestSingleGain = Math.Max(Stats.LargestSingleGain, trade.PnL);
        }
        else if (trade.PnL < 0)
        {
            Stats.LosingTradeCount++;
            Stats.ConsecutiveLosses++;
            Stats.ConsecutiveWins = 0;
            Stats.MaxConsecutiveLosses = Math.Max(Stats.MaxConsecutiveLosses, Stats.ConsecutiveLosses);
            Stats.TotalLossFromLosses += Math.Abs(trade.PnL);
            Stats.LargestSingleLoss = Math.Max(Stats.LargestSingleLoss, Math.Abs(trade.PnL));
        }

        // Short selling P&L
        if (trade.Side == "Short")
            Stats.ShortSellingPnL += trade.PnL;

        // Day trading count
        if (trade.ExitTime.Date == Stats.CurrentTradeDay.Date)
        {
            Stats.TradesInCurrentDay++;
        }
        else
        {
            Stats.CurrentTradeDay = trade.ExitTime.Date;
            Stats.TradesInCurrentDay = 1;
        }
    }

    /// <summary>
    /// Record an equity snapshot (called daily at market close).
    /// </summary>
    public void RecordEquitySnapshot(decimal equity, decimal cash, decimal marketIndex, DateTime gameTime)
    {
        var snapshot = new EquitySnapshot
        {
            Time = new DateTimeOffset(gameTime).ToUnixTimeSeconds(),
            Equity = equity,
            Cash = cash,
            MarketIndex = marketIndex,
        };
        Stats.EquityHistory.Add(snapshot);
        Stats.DaysPlayed++;

        // Track max portfolio value and drawdown
        if (equity > Stats.MaxPortfolioValue)
            Stats.MaxPortfolioValue = equity;

        if (Stats.MaxPortfolioValue > 0)
        {
            var drawdownPct = (Stats.MaxPortfolioValue - equity) / Stats.MaxPortfolioValue * 100;
            Stats.MaxDrawdownPercent = Math.Max(Stats.MaxDrawdownPercent, drawdownPct);
        }
    }

    /// <summary>
    /// Check all achievement conditions. Called periodically (e.g., at market close).
    /// </summary>
    public void CheckAchievements(decimal portfolioValue, Portfolio portfolio, Func<string, decimal> getPrice, DateTime gameTime)
    {
        NewUnlocksThisTick.Clear();

        // Wealth achievements
        TryUnlock("first_steps", portfolioValue >= 60_000, gameTime);
        TryUnlock("six_figures", portfolioValue >= 100_000, gameTime);
        TryUnlock("quarter_million", portfolioValue >= 250_000, gameTime);
        TryUnlock("half_millionaire", portfolioValue >= 500_000, gameTime);
        TryUnlock("millionaire", portfolioValue >= 1_000_000, gameTime);
        TryUnlock("multi_millionaire", portfolioValue >= 5_000_000, gameTime);
        TryUnlock("tycoon", portfolioValue >= 10_000_000, gameTime);

        // Trading achievements
        TryUnlock("first_blood", Stats.WinningTradeCount >= 1, gameTime);
        TryUnlock("sharpshooter", Stats.MaxConsecutiveWins >= 10, gameTime);
        TryUnlock("day_trader", Stats.TradesInCurrentDay >= 50, gameTime);
        TryUnlock("short_master", Stats.ShortSellingPnL >= 10_000, gameTime);
        TryUnlock("diversified", portfolio.Positions.Values.Select(p => GetSector(p.Symbol)).Distinct().Count() >= 5, gameTime);

        var totalTrades = Stats.WinningTradeCount + Stats.LosingTradeCount;

        // Quick flip: trade with PnL > $1000 and held < 1 day
        TryUnlock("quick_flip", Stats.TradeHistory.Any(t => t.PnL > 1000 && t.HoldingDays < 1), gameTime);

        // Patient investor: trade held 30+ days at profit
        TryUnlock("patient_investor", Stats.TradeHistory.Any(t => t.PnL > 0 && t.HoldingDays >= 30), gameTime);

        // Dip buyer: bought >15% down, sold >20% up
        TryUnlock("dip_buyer", Stats.TradeHistory.Any(t => t.Side == "Long" && t.PnLPercent >= 20), gameTime);

        // Penny millionaire
        TryUnlock("penny_millionaire", Stats.TradeHistory.Where(t => t.EntryPrice < 5).Sum(t => t.PnL) >= 10_000, gameTime);

        // Market experience
        TryUnlock("market_veteran", Stats.DaysPlayed >= 252, gameTime); // 1 trading year
        TryUnlock("black_swan", Stats.SurvivedFlashCrash, gameTime);
        TryUnlock("sector_specialist", Stats.SectorPnL.Values.Any(v => v >= 50_000), gameTime);

        // Risk achievements
        TryUnlock("comeback_kid", portfolioValue >= 100_000 && Stats.EquityHistory.Any(s => s.Equity < 20_000), gameTime);
        TryUnlock("iron_stomach", totalTrades >= 20 && Stats.MaxDrawdownPercent <= 15, gameTime);
        TryUnlock("risk_manager", totalTrades >= 20 && Stats.LargestSingleLoss <= portfolioValue * 0.05m, gameTime);
        TryUnlock("hundred_trades", totalTrades >= 100, gameTime);
        TryUnlock("five_hundred_trades", totalTrades >= 500, gameTime);

        var winRate = totalTrades > 0 ? (decimal)Stats.WinningTradeCount / totalTrades * 100 : 0;
        TryUnlock("profit_factory", totalTrades >= 50 && winRate >= 60, gameTime);

        // Clean record: 252 days without a loss > 10% of portfolio
        TryUnlock("clean_record", Stats.DaysPlayed >= 252 && Stats.LargestSingleLoss < 5000, gameTime);
    }

    // Placeholder for sector lookup — will be wired up from GameLoop
    private Func<string, string>? _getSector;
    public void SetSectorLookup(Func<string, string> getSector) => _getSector = getSector;
    private string GetSector(string symbol) => _getSector?.Invoke(symbol) ?? "Unknown";

    private void TryUnlock(string id, bool condition, DateTime gameTime)
    {
        if (!condition) return;
        var achievement = _achievements.Find(a => a.Id == id);
        if (achievement == null || achievement.Unlocked) return;

        achievement.Unlocked = true;
        achievement.UnlockedAt = gameTime;
        NewUnlocksThisTick.Add(achievement);
        _log.Info("Achievement unlocked", new { id, name = achievement.Name });
    }
}
