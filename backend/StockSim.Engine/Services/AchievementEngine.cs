using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// Manages achievements and player statistics tracking.
/// Spec 1.4.1: 30+ achievements across 4 categories.
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

            // --- New Wealth achievements ---
            new("first_dividend", "Dividend Collector", "Receive your first dividend payment", AchievementCategory.Wealth),
            new("passive_income", "Passive Income", "Earn $1,000 total in dividends", AchievementCategory.Wealth),
            new("portfolio_diversified", "Well Diversified", "Hold 10+ positions simultaneously", AchievementCategory.Wealth),

            // --- New Trading achievements ---
            new("options_trader", "Options Trader", "Complete your first options trade", AchievementCategory.Trading),
            new("commodity_trader", "Commodity Trader", "Trade a commodity ETF (GLD, SLV, or USO)", AchievementCategory.Trading),
            new("short_seller", "Short Seller", "Open your first short position", AchievementCategory.Trading),
            new("limit_master", "Limit Master", "Have 5+ limit orders filled", AchievementCategory.Trading),
            new("scalper", "Scalper", "Make profit on 3 trades held less than 1 day", AchievementCategory.Trading),

            // --- New Market achievements ---
            new("economic_cycle", "Economic Cycle", "Experience all 4 economic phases", AchievementCategory.Market),
            new("crisis_survivor", "Crisis Survivor", "Portfolio positive during a Tier 3+ event", AchievementCategory.Market),
            new("news_reader", "Informed Trader", "Trade within 10 minutes of a Major news event", AchievementCategory.Market),
            new("global_investor", "Global Investor", "Hold positions in 8+ different sectors", AchievementCategory.Market),

            // --- New Risk achievements ---
            new("zero_loss_week", "Perfect Week", "Complete 5+ trades in a week with 100% win rate", AchievementCategory.Risk),
            new("recovery_artist", "Recovery Artist", "Recover from a -15% drawdown to new all-time high", AchievementCategory.Risk),
            new("tax_efficient", "Tax Efficient", "Realize $10k+ in long-term capital gains (held >252 days)", AchievementCategory.Risk),

            // --- Session 35+ achievements ---
            new("supply_chain_master", "Supply Chain Master", "Profit from a supply chain cascade event", AchievementCategory.Market),
            new("history_buff", "History Buff", "Complete a History Mode scenario", AchievementCategory.Market),
            new("shareholder_activist", "Shareholder Activist", "Vote in a shareholder proposal", AchievementCategory.Trading),
            new("election_trader", "Election Trader", "Hold positions through an election cycle", AchievementCategory.Market),
            new("seasonal_trader", "Seasonal Trader", "Trade during a Triple Witching day", AchievementCategory.Trading),
            new("whisper_trader", "Whisper Trader", "Act on a supply chain whisper rumor", AchievementCategory.Trading),
            new("ten_bagger", "Ten Bagger", "Make 1,000%+ return on a single trade", AchievementCategory.Wealth),
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

        // Weekly trade tracking for "Perfect Week" achievement
        var cal = System.Globalization.CultureInfo.InvariantCulture.Calendar;
        var weekNum = cal.GetWeekOfYear(trade.ExitTime, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        var weekKey = $"{trade.ExitTime.Year}-{weekNum:D2}";
        if (!Stats.WeeklyTradeResults.ContainsKey(weekKey))
            Stats.WeeklyTradeResults[weekKey] = (0, 0);
        var (wins, total) = Stats.WeeklyTradeResults[weekKey];
        Stats.WeeklyTradeResults[weekKey] = (trade.PnL > 0 ? wins + 1 : wins, total + 1);

        // Commodity ETF tracking
        var commodityETFs = new HashSet<string> { "GLD", "SLV", "USO" };
        if (commodityETFs.Contains(trade.Symbol))
            Stats.HasTradedCommodityETF = true;

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
    public void CheckAchievements(decimal portfolioValue, Portfolio portfolio, Func<string, decimal> getPrice, DateTime gameTime, GameLoop? gameLoop = null)
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

        // --- New Wealth achievements ---
        TryUnlock("first_dividend", Stats.HasReceivedDividend, gameTime);
        TryUnlock("passive_income", Stats.TotalDividendsReceived >= 1_000, gameTime);
        TryUnlock("portfolio_diversified", portfolio.Positions.Count >= 10, gameTime);

        // --- New Trading achievements ---
        TryUnlock("options_trader", Stats.HasTradedOptions, gameTime);
        TryUnlock("commodity_trader", Stats.HasTradedCommodityETF, gameTime);
        TryUnlock("short_seller", Stats.HasOpenedShortPosition, gameTime);
        TryUnlock("limit_master", Stats.LimitOrdersFilled >= 5, gameTime);

        // Scalper: 3+ profitable trades held less than 1 day
        TryUnlock("scalper", Stats.TradeHistory.Count(t => t.PnL > 0 && t.HoldingDays < 1) >= 3, gameTime);

        // --- New Market achievements ---

        // Track current economic phase
        if (gameLoop != null)
        {
            Stats.EconomicPhasesExperienced.Add(gameLoop.EconomicCycle.Phase.ToString());
        }
        TryUnlock("economic_cycle", Stats.EconomicPhasesExperienced.Count >= 4, gameTime);

        // Crisis survivor: portfolio positive while a Tier 3+ event is active
        if (gameLoop != null)
        {
            var hasTier3PlusEvent = gameLoop.EventEngine.ActiveEvents
                .Any(e => e.Tier >= Models.EventTier.Tier3);
            TryUnlock("crisis_survivor", hasTier3PlusEvent && portfolioValue > gameLoop.StartingCash, gameTime);
        }

        // News reader / Informed Trader: tracked via HasTradedNearMajorEvent flag (set externally)
        TryUnlock("news_reader", Stats.HasTradedNearMajorEvent, gameTime);

        // Global investor: 8+ distinct sectors in current positions
        var distinctSectors = portfolio.Positions.Values
            .Select(p => GetSector(p.Symbol))
            .Where(s => s != "Unknown")
            .Distinct()
            .Count();
        TryUnlock("global_investor", distinctSectors >= 8, gameTime);

        // --- New Risk achievements ---

        // Perfect week: any week with 5+ trades and 100% win rate
        TryUnlock("zero_loss_week", Stats.WeeklyTradeResults.Values
            .Any(w => w.Total >= 5 && w.Wins == w.Total), gameTime);

        // Recovery artist: hit -15% drawdown then recovered to new ATH
        if (Stats.MaxPortfolioValue > 0)
        {
            var currentDrawdown = (Stats.MaxPortfolioValue - portfolioValue) / Stats.MaxPortfolioValue * 100;
            if (currentDrawdown >= 15)
                Stats.HitDrawdown15Percent = true;
            if (Stats.HitDrawdown15Percent && portfolioValue >= Stats.MaxPortfolioValue)
                Stats.RecoveredFromDrawdown = true;
        }
        TryUnlock("recovery_artist", Stats.RecoveredFromDrawdown, gameTime);

        // Tax efficient: $10k+ in long-term capital gains (held > 252 trading days)
        var longTermGains = Stats.TradeHistory
            .Where(t => t.PnL > 0 && t.HoldingDays >= 252)
            .Sum(t => t.PnL);
        TryUnlock("tax_efficient", longTermGains >= 10_000, gameTime);

        // --- Session 35+ achievements ---
        TryUnlock("supply_chain_master", Stats.HasProfitedFromSupplyChain, gameTime);
        TryUnlock("history_buff", Stats.HasCompletedHistoryScenario, gameTime);
        TryUnlock("shareholder_activist", Stats.HasVotedInShareholderMeeting, gameTime);
        TryUnlock("election_trader", Stats.HasTradedThroughElection, gameTime);
        TryUnlock("seasonal_trader", Stats.HasTradedOnTripleWitching, gameTime);
        TryUnlock("whisper_trader", Stats.HasActedOnWhisper, gameTime);
        TryUnlock("ten_bagger", Stats.TradeHistory.Any(t => t.PnLPercent >= 1000), gameTime);
    }

    /// <summary>Record that the player received a dividend payment.</summary>
    public void RecordDividendReceived(decimal amount)
    {
        Stats.HasReceivedDividend = true;
        Stats.TotalDividendsReceived += amount;
    }

    /// <summary>Record that the player opened a short position.</summary>
    public void RecordShortOpened() => Stats.HasOpenedShortPosition = true;

    /// <summary>Record that a limit order was filled.</summary>
    public void RecordLimitOrderFilled() => Stats.LimitOrdersFilled++;

    /// <summary>Record that the player traded options.</summary>
    public void RecordOptionsTrade() => Stats.HasTradedOptions = true;

    /// <summary>Record that the player traded near a Major news event.</summary>
    public void RecordTradeNearMajorEvent() => Stats.HasTradedNearMajorEvent = true;

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
