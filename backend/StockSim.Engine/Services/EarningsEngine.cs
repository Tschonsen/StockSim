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
            stock.FairValue = stock.CurrentPrice; // Recalibrate fair value

            // Increase volatility for next few days
            stock.BaseVolatility *= 1.5m;

            report.PriceImpactPercent = Math.Round(priceImpact * 100, 2);
            ReleasedThisTick.Add(report);

            _log.Info("Earnings released", new
            {
                symbol = report.Symbol,
                expected = report.ExpectedEPS,
                actual = report.ActualEPS,
                beat,
                priceImpact = $"{priceImpact:P1}",
            });
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
