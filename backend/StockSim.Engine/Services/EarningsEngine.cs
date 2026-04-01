using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// Manages quarterly earnings reports for all stocks.
/// Earnings announcements cause significant price movements (±5-20%).
/// Pre-earnings volatility increases as the date approaches.
/// </summary>
public class EarningsEngine
{
    private readonly Logger _log = new("EarningsEngine");
    private readonly Random _rng;

    /// <summary>All scheduled earnings with date and results.</summary>
    public List<EarningsReport> Schedule { get; } = new();

    /// <summary>Earnings released this tick (for frontend notification).</summary>
    public List<EarningsReport> ReleasedThisTick { get; } = new();

    /// <summary>Guidance events generated this tick (for news feed).</summary>
    public List<GuidanceEvent> GuidanceThisTick { get; } = new();

    /// <summary>Companies flagged for insolvency risk after earnings (for IPOEngine delisting).</summary>
    public List<InsolvencyWarning> InsolvencyWarnings { get; } = new();

    /// <summary>
    /// Post-Earnings Announcement Drift (PEAD): stocks continue drifting
    /// in the direction of the surprise for ~5 trading days after release.
    /// Key: symbol → (daily drift amount, days remaining).
    /// </summary>
    public Dictionary<string, (decimal DailyDrift, int DaysLeft)> PEADEffects { get; } = new();

    public EarningsEngine(int seed)
    {
        _rng = new Random(seed);
    }

    /// <summary>
    /// Generate earnings schedule for all stocks. Each stock gets quarterly earnings.
    /// Called at game start.
    /// </summary>
    public void GenerateSchedule(IReadOnlyList<Stock> stocks, DateTime gameStart)
    {
        Schedule.Clear();

        foreach (var stock in stocks)
        {
            // Skip ETFs
            if (stock.Traits.Contains("ETF")) continue;

            // Generate 4 quarterly earnings over the next year
            // Randomize the quarter start (companies report at different times)
            var baseOffset = _rng.Next(0, 63); // 0-63 trading days offset
            for (int q = 0; q < 4; q++)
            {
                var earningsDate = gameStart.AddDays(baseOffset + q * 63); // ~63 trading days per quarter
                // Skip weekends
                while (earningsDate.DayOfWeek == DayOfWeek.Saturday || earningsDate.DayOfWeek == DayOfWeek.Sunday)
                    earningsDate = earningsDate.AddDays(1);

                // Expected EPS based on company fundamentals
                var expectedEPS = stock.NetIncome > 0
                    ? Math.Round(stock.NetIncome / stock.SharesOutstanding / 4, 2) // Quarterly
                    : Math.Round((decimal)(_rng.NextDouble() * 2 - 0.5), 2);

                Schedule.Add(new EarningsReport
                {
                    Symbol = stock.Symbol,
                    ReportDate = new DateTime(earningsDate.Year, earningsDate.Month, earningsDate.Day, 16, 0, 0), // After market close
                    Quarter = q + 1,
                    ExpectedEPS = expectedEPS,
                    ExpectedRevenue = stock.Revenue / 4, // Quarterly
                });
            }
        }

        Schedule.Sort((a, b) => a.ReportDate.CompareTo(b.ReportDate));
        _log.Info("Earnings schedule generated", new { reports = Schedule.Count, stocks = stocks.Count });
    }

    /// <summary>
    /// Check for earnings releases at market close (4 PM).
    /// Called daily from GameLoop.
    /// </summary>
    public void TickDay(IReadOnlyList<Stock> stocks, DateTime gameTime)
    {
        ReleasedThisTick.Clear();
        GuidanceThisTick.Clear();

        // Find earnings due today
        var dueToday = Schedule
            .Where(e => !e.Released && e.ReportDate.Date <= gameTime.Date)
            .ToList();

        foreach (var report in dueToday)
        {
            var stock = stocks.FirstOrDefault(s => s.Symbol == report.Symbol);
            if (stock == null) continue;

            // Generate actual results with surprise
            var surpriseRoll = _rng.NextDouble();
            decimal surpriseMagnitude;
            bool beat;

            if (surpriseRoll < 0.25) { beat = false; surpriseMagnitude = (decimal)(_rng.NextDouble() * 0.3 + 0.05); } // 25% miss
            else if (surpriseRoll < 0.45) { beat = false; surpriseMagnitude = (decimal)(_rng.NextDouble() * 0.1); }    // 20% slight miss
            else if (surpriseRoll < 0.70) { beat = true; surpriseMagnitude = (decimal)(_rng.NextDouble() * 0.1); }      // 25% slight beat
            else { beat = true; surpriseMagnitude = (decimal)(_rng.NextDouble() * 0.3 + 0.05); }                         // 30% solid beat

            var epsMultiplier = beat ? (1 + surpriseMagnitude) : (1 - surpriseMagnitude);
            report.ActualEPS = Math.Round(report.ExpectedEPS * epsMultiplier, 2);
            report.ActualRevenue = Math.Round(report.ExpectedRevenue * (decimal)(0.95 + _rng.NextDouble() * 0.1), 0);
            report.Released = true;
            report.Beat = beat;

            // Calculate price impact based on surprise magnitude
            var priceImpact = (report.ActualEPS - report.ExpectedEPS) / Math.Max(Math.Abs(report.ExpectedEPS), 0.01m);
            priceImpact = Math.Max(-0.20m, Math.Min(0.20m, priceImpact * 5)); // Cap at ±20%

            // Apply after-hours price gap (will show at next market open)
            var priceChange = stock.CurrentPrice * priceImpact;
            stock.CurrentPrice = Math.Max(0.01m, Math.Round(stock.CurrentPrice + priceChange, 2));

            // === REALISM: Update fundamentals after earnings ===
            // Revenue grows/shrinks based on actual results vs expected
            var revenueGrowth = report.ActualRevenue / Math.Max(report.ExpectedRevenue, 1m) - 1m;
            stock.Revenue = Math.Max(1000m, Math.Round(stock.Revenue * (1m + revenueGrowth), 0));

            // NetIncome shifts proportionally to EPS surprise
            var incomeMultiplier = report.ActualEPS / Math.Max(Math.Abs(report.ExpectedEPS), 0.01m);
            incomeMultiplier = Math.Max(0.5m, Math.Min(1.5m, incomeMultiplier)); // Cap at ±50% swing
            stock.NetIncome = Math.Round(stock.NetIncome * incomeMultiplier, 0);

            // Update RevenueGrowth tracking
            stock.RevenueGrowth = Math.Round(revenueGrowth, 4);

            // FairValue: derive from fundamentals (PE-based valuation)
            // Use sector-average PE (~18) applied to updated earnings
            if (stock.NetIncome > 0 && stock.SharesOutstanding > 0)
            {
                var eps = stock.NetIncome / stock.SharesOutstanding;
                stock.FairValue = Math.Max(0.50m, Math.Round(eps * 18m, 2)); // Simplified DCF: 18x earnings
            }
            else
            {
                stock.FairValue = stock.CurrentPrice; // Unprofitable → price is fair value
            }

            // Analyst Rating: shift based on earnings surprise
            // Beat → upgrade tendency, Miss → downgrade tendency
            var ratingShift = beat
                ? 0.1m + (decimal)(_rng.NextDouble() * 0.3) // +0.1 to +0.4
                : -(0.1m + (decimal)(_rng.NextDouble() * 0.3)); // -0.1 to -0.4
            stock.AnalystRating = Math.Round(Math.Max(1.0m, Math.Min(5.0m, stock.AnalystRating + ratingShift)), 1);

            // Target Price: analysts revise based on new fair value + sentiment
            var targetBias = beat ? 1.05m + (decimal)(_rng.NextDouble() * 0.15) // +5% to +20% above current
                                  : 0.85m + (decimal)(_rng.NextDouble() * 0.10); // -5% to -15% below current
            stock.TargetPrice = Math.Round(stock.CurrentPrice * targetBias, 2);

            // === REALISM BATCH 2: Dividend yield adjustments ===
            if (stock.DividendYield > 0)
            {
                if (!beat && stock.NetIncome < 0)
                {
                    // Earnings miss + negative income → cut dividend 10-50%
                    var cutFactor = 0.50m + (decimal)(_rng.NextDouble() * 0.40); // Keep 50-90%
                    stock.DividendYield = Math.Max(0m, Math.Round(stock.DividendYield * cutFactor, 4));
                    _log.Info("Dividend cut", new { symbol = stock.Symbol, newYield = stock.DividendYield });
                }
                else if (beat && stock.NetIncome > 0 && stock.DebtToEquity < 2.0m)
                {
                    // Strong beat + healthy balance sheet → raise dividend 2-8%
                    var raiseFactor = 1.02m + (decimal)(_rng.NextDouble() * 0.06);
                    stock.DividendYield = Math.Min(0.15m, Math.Round(stock.DividendYield * raiseFactor, 4));
                    _log.Info("Dividend raised", new { symbol = stock.Symbol, newYield = stock.DividendYield });
                }
            }

            // === REALISM BATCH 2: DebtToEquity adjustments ===
            if (beat && stock.NetIncome > 0)
            {
                // Beat → company pays down debt, D/E drops 2-8%
                var debtReduction = 0.92m + (decimal)(_rng.NextDouble() * 0.06); // 92-98% of previous
                stock.DebtToEquity = Math.Max(0m, Math.Round(stock.DebtToEquity * debtReduction, 2));
            }
            else if (!beat)
            {
                // Miss → company takes on debt, D/E rises 3-12%
                var debtIncrease = 1.03m + (decimal)(_rng.NextDouble() * 0.09); // 103-112% of previous
                stock.DebtToEquity = Math.Round(stock.DebtToEquity * debtIncrease, 2);
            }

            // === REALISM BATCH 2: Insolvency check ===
            if (stock.NetIncome < 0 && stock.DebtToEquity > 4.0m)
            {
                // Extreme debt + negative income → insolvency risk
                // Higher D/E and deeper losses → higher probability
                var insolvencyProb = Math.Min(0.5, (double)(stock.DebtToEquity - 4.0m) * 0.1
                    + Math.Abs((double)stock.NetIncome / Math.Max((double)stock.Revenue, 1.0)) * 0.3);
                if (_rng.NextDouble() < insolvencyProb)
                {
                    InsolvencyWarnings.Add(new InsolvencyWarning
                    {
                        Symbol = stock.Symbol,
                        Reason = $"Extreme financial distress: D/E={stock.DebtToEquity:F1}, NetIncome={stock.NetIncome:N0}",
                    });
                    _log.Warn("Insolvency risk flagged", new { symbol = stock.Symbol, de = stock.DebtToEquity, ni = stock.NetIncome });
                }
            }

            // Increase volatility for next few days
            stock.BaseVolatility *= 1.5m;

            report.PriceImpactPercent = Math.Round(priceImpact * 100, 2);
            ReleasedThisTick.Add(report);

            // PEAD: 5-day continuation drift (~30% of initial move spread over 5 days)
            var peadDrift = priceImpact * 0.06m; // 30% of impact / 5 days = 6% per day
            PEADEffects[stock.Symbol] = (peadDrift, 5);

            _log.Info("Earnings released", new
            {
                symbol = report.Symbol,
                expected = report.ExpectedEPS,
                actual = report.ActualEPS,
                beat,
                priceImpact = $"{priceImpact:P1}",
            });

            // === EARNINGS GUIDANCE: management updates forward expectations ===
            GenerateGuidance(report, stock, gameTime);
        }

        // Pre-earnings volatility: increase vol for stocks reporting in next 5 trading days
        var upcoming5Days = Schedule
            .Where(e => !e.Released && (e.ReportDate.Date - gameTime.Date).TotalDays <= 7 && (e.ReportDate.Date - gameTime.Date).TotalDays > 0)
            .ToList();

        foreach (var upcoming in upcoming5Days)
        {
            var stock = stocks.FirstOrDefault(s => s.Symbol == upcoming.Symbol);
            if (stock != null)
            {
                // Slight daily vol increase as earnings approach
                stock.BaseVolatility *= 1.01m;
            }
        }

        // Apply PEAD: continue drift for stocks with recent earnings
        var expired = new List<string>();
        foreach (var (sym, (drift, daysLeft)) in PEADEffects)
        {
            var peadStock = stocks.FirstOrDefault(s => s.Symbol == sym);
            if (peadStock != null)
            {
                var peadMove = peadStock.CurrentPrice * drift;
                peadStock.CurrentPrice = Math.Max(0.01m, Math.Round(peadStock.CurrentPrice + peadMove, 2));
            }
            PEADEffects[sym] = (drift, daysLeft - 1);
            if (daysLeft - 1 <= 0) expired.Add(sym);
        }
        foreach (var sym in expired) PEADEffects.Remove(sym);
    }

    /// <summary>
    /// Generate forward guidance after earnings release.
    /// 60% of companies issue guidance. Direction depends on earnings result + random factors.
    /// Adjusts next quarter's expected EPS and generates a news event.
    /// </summary>
    private void GenerateGuidance(EarningsReport report, Stock stock, DateTime gameTime)
    {
        // 60% of companies issue guidance
        if (_rng.NextDouble() > 0.60) return;

        // Find next quarter's report for this stock
        var nextReport = Schedule
            .FirstOrDefault(e => !e.Released && e.Symbol == report.Symbol && e.ReportDate > gameTime);
        if (nextReport == null) return;

        // Determine guidance direction based on current results + momentum
        var roll = _rng.NextDouble();
        GuidanceDirection direction;
        decimal epsAdjust;

        if (report.Beat)
        {
            // Beat → 55% raise, 35% maintain, 10% lower (sandbagging)
            if (roll < 0.55) { direction = GuidanceDirection.Raised; epsAdjust = (decimal)(_rng.NextDouble() * 0.15 + 0.03); }
            else if (roll < 0.90) { direction = GuidanceDirection.Maintained; epsAdjust = 0; }
            else { direction = GuidanceDirection.Lowered; epsAdjust = -(decimal)(_rng.NextDouble() * 0.08 + 0.02); }
        }
        else
        {
            // Miss → 15% raise (turnaround), 25% maintain, 50% lower, 10% withdrawn
            if (roll < 0.15) { direction = GuidanceDirection.Raised; epsAdjust = (decimal)(_rng.NextDouble() * 0.08 + 0.02); }
            else if (roll < 0.40) { direction = GuidanceDirection.Maintained; epsAdjust = 0; }
            else if (roll < 0.90) { direction = GuidanceDirection.Lowered; epsAdjust = -(decimal)(_rng.NextDouble() * 0.20 + 0.05); }
            else { direction = GuidanceDirection.Withdrawn; epsAdjust = -(decimal)(_rng.NextDouble() * 0.10 + 0.05); }
        }

        // Apply EPS adjustment to next quarter
        var oldExpected = nextReport.ExpectedEPS;
        nextReport.ExpectedEPS = Math.Round(nextReport.ExpectedEPS * (1m + epsAdjust), 2);

        // Generate news
        var sentiment = direction switch
        {
            GuidanceDirection.Raised => 0.4f,
            GuidanceDirection.Maintained => 0.0f,
            GuidanceDirection.Lowered => -0.4f,
            GuidanceDirection.Withdrawn => -0.6f,
            _ => 0f,
        };

        var headline = direction switch
        {
            GuidanceDirection.Raised => $"{stock.Name} raises Q{nextReport.Quarter} guidance: EPS now expected ${nextReport.ExpectedEPS:F2} (was ${oldExpected:F2})",
            GuidanceDirection.Maintained => $"{stock.Name} reaffirms Q{nextReport.Quarter} guidance at ${nextReport.ExpectedEPS:F2}",
            GuidanceDirection.Lowered => $"{stock.Name} lowers Q{nextReport.Quarter} outlook: EPS guidance cut to ${nextReport.ExpectedEPS:F2} from ${oldExpected:F2}",
            GuidanceDirection.Withdrawn => $"{stock.Name} withdraws forward guidance citing 'uncertain macro environment'",
            _ => "",
        };

        var summary = direction switch
        {
            GuidanceDirection.Raised => $"Management raised its outlook for Q{nextReport.Quarter}, pointing to strong demand trends and improving margins. The company now expects EPS of ${nextReport.ExpectedEPS:F2}, up from the previous target of ${oldExpected:F2}.",
            GuidanceDirection.Maintained => $"Management reiterated its existing outlook for Q{nextReport.Quarter}, maintaining EPS guidance at ${nextReport.ExpectedEPS:F2}. Analysts view the reaffirmation as a sign of steady execution.",
            GuidanceDirection.Lowered => $"Management cut its Q{nextReport.Quarter} forecast, citing headwinds from {(epsAdjust < -0.10m ? "weakening demand and margin pressure" : "cautious consumer spending")}. New EPS guidance of ${nextReport.ExpectedEPS:F2} is below the street's prior ${oldExpected:F2} estimate.",
            GuidanceDirection.Withdrawn => $"{stock.Name} withdrew all forward guidance, stating that current macro conditions make forecasting unreliable. The withdrawal is typically seen as a bearish signal and may trigger analyst downgrades.",
            _ => "",
        };

        // Small immediate price impact from guidance
        var guidanceImpact = direction switch
        {
            GuidanceDirection.Raised => (decimal)(_rng.NextDouble() * 0.02 + 0.005),
            GuidanceDirection.Lowered => -(decimal)(_rng.NextDouble() * 0.03 + 0.01),
            GuidanceDirection.Withdrawn => -(decimal)(_rng.NextDouble() * 0.04 + 0.02),
            _ => 0m,
        };
        stock.CurrentPrice = Math.Max(0.01m, Math.Round(stock.CurrentPrice * (1m + guidanceImpact), 2));

        GuidanceThisTick.Add(new GuidanceEvent
        {
            Symbol = stock.Symbol,
            Headline = headline,
            Summary = summary,
            Direction = direction,
            EPSAdjustment = epsAdjust,
            Sentiment = sentiment,
        });

        _log.Info("Earnings guidance issued", new { symbol = stock.Symbol, direction = direction.ToString(), oldEPS = oldExpected, newEPS = nextReport.ExpectedEPS });
    }

    /// <summary>Get upcoming earnings for the next N trading days.</summary>
    public List<EarningsReport> GetUpcoming(DateTime gameTime, int days = 30)
    {
        var cutoff = gameTime.AddDays(days);
        return Schedule
            .Where(e => !e.Released && e.ReportDate >= gameTime && e.ReportDate <= cutoff)
            .OrderBy(e => e.ReportDate)
            .ToList();
    }

    /// <summary>Get recent earnings results.</summary>
    public List<EarningsReport> GetRecent(int count = 20)
    {
        return Schedule
            .Where(e => e.Released)
            .OrderByDescending(e => e.ReportDate)
            .Take(count)
            .ToList();
    }
}

/// <summary>
/// A single earnings report for a stock.
/// </summary>
public class EarningsReport
{
    public string Symbol { get; set; } = "";
    public DateTime ReportDate { get; set; }
    public int Quarter { get; set; }

    // Estimates
    public decimal ExpectedEPS { get; set; }
    public decimal ExpectedRevenue { get; set; }

    // Actuals (filled on release)
    public decimal ActualEPS { get; set; }
    public decimal ActualRevenue { get; set; }
    public bool Beat { get; set; }
    public decimal PriceImpactPercent { get; set; }
    public bool Released { get; set; }

    public decimal EPSSurprise => ActualEPS - ExpectedEPS;
    public decimal EPSSurprisePercent => ExpectedEPS != 0 ? Math.Round(EPSSurprise / Math.Abs(ExpectedEPS) * 100, 2) : 0;
}

/// <summary>
/// Flags a company at risk of insolvency (extreme debt + persistent losses).
/// Used by GameLoop to trigger delisting via IPOEngine.
/// </summary>
public class InsolvencyWarning
{
    public string Symbol { get; set; } = "";
    public string Reason { get; set; } = "";
}

/// <summary>
/// Forward guidance issued by management after earnings.
/// Raised guidance = bullish, lowered = bearish. Affects next quarter's expected EPS.
/// </summary>
public class GuidanceEvent
{
    public string Symbol { get; set; } = "";
    public string Headline { get; set; } = "";
    public string Summary { get; set; } = "";
    public GuidanceDirection Direction { get; set; }
    /// <summary>How much the next quarter's expected EPS was adjusted (absolute).</summary>
    public decimal EPSAdjustment { get; set; }
    public float Sentiment { get; set; }
}

public enum GuidanceDirection { Raised, Maintained, Lowered, Withdrawn }
