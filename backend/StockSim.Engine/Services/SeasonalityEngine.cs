using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// Calendar-based market effects: January Effect, Sell in May, October volatility,
/// Q4 Holiday Rally, Earnings Seasons, Triple Witching, Summer Lull.
/// All effects are real, documented market phenomena.
/// </summary>
public class SeasonalityEngine
{
    private readonly Logger _log = new("Seasonality");

    /// <summary>Sector drift multiplier for current month (applied to PriceEngine drift).</summary>
    public Dictionary<string, decimal> SectorMultipliers { get; } = new();

    /// <summary>Overall volatility multiplier for current month.</summary>
    public decimal VolatilityMultiplier { get; private set; } = 1m;

    /// <summary>Event frequency multiplier (higher during earnings seasons).</summary>
    public decimal EventFrequencyMultiplier { get; private set; } = 1m;

    /// <summary>Volume multiplier for current period.</summary>
    public decimal VolumeMultiplier { get; private set; } = 1m;

    /// <summary>True if today is a Triple Witching day (3rd Friday of Mar/Jun/Sep/Dec).</summary>
    public bool IsTripleWitching { get; private set; }

    /// <summary>News headline for seasonal events (cleared each day).</summary>
    public string? SeasonalHeadline { get; private set; }

    private int _lastProcessedDay;

    /// <summary>
    /// Called once per trading day from GameLoop. Updates all seasonal multipliers.
    /// </summary>
    public void TickDay(DateTime gameTime)
    {
        var dayOfYear = gameTime.DayOfYear;
        if (dayOfYear == _lastProcessedDay) return;
        _lastProcessedDay = dayOfYear;

        var month = gameTime.Month;
        var dayOfWeek = gameTime.DayOfWeek;
        var day = gameTime.Day;

        SectorMultipliers.Clear();
        SeasonalHeadline = null;
        IsTripleWitching = false;
        VolatilityMultiplier = 1m;
        EventFrequencyMultiplier = 1m;
        VolumeMultiplier = 1m;

        // === JANUARY EFFECT ===
        // Small caps outperform in January due to tax-loss selling reversal
        if (month == 1)
        {
            SectorMultipliers["Technology"] = 1.008m;    // Growth/small cap boost
            SectorMultipliers["Consumer Goods"] = 1.005m;
            SectorMultipliers["Materials"] = 1.006m;
            EventFrequencyMultiplier = 1.3m; // Q4 earnings season starts late Jan
            if (day == 2 || day == 3)
                SeasonalHeadline = "SEASONAL: January Effect — historically strong month for small caps as tax-loss selling reverses";
        }

        // === EARNINGS SEASONS (Jan, Apr, Jul, Oct — weeks 2-5) ===
        if ((month == 1 || month == 4 || month == 7 || month == 10) && day >= 10)
        {
            EventFrequencyMultiplier = 1.5m; // 50% more company events
            VolatilityMultiplier = 1.1m;     // Slightly higher vol during earnings
            VolumeMultiplier = 1.2m;
        }

        // === SELL IN MAY ===
        // May-October historically underperforms November-April
        if (month >= 5 && month <= 10)
        {
            var dampening = -0.003m; // Small negative drift all sectors
            foreach (var sector in AllSectors)
                SectorMultipliers[sector] = SectorMultipliers.GetValueOrDefault(sector, 1m) + dampening;

            if (month == 5 && day <= 3)
                SeasonalHeadline = "SEASONAL: 'Sell in May and go away' — historically weaker May-October period begins";
        }

        // === SUMMER LULL (Jul-Aug) ===
        if (month == 7 || month == 8)
        {
            VolumeMultiplier = 0.7m;         // 30% less volume
            VolatilityMultiplier *= 0.9m;    // Slightly calmer (but can snap)
        }

        // === OCTOBER EFFECT ===
        // Historically most volatile month (1929, 1987, 2008 all had October crashes)
        if (month == 10)
        {
            VolatilityMultiplier = 1.2m;
            if (day == 1)
                SeasonalHeadline = "SEASONAL: October — historically the most volatile month. Major crashes have occurred in October (1929, 1987, 2008)";
        }

        // === Q4 HOLIDAY RALLY (Nov-Dec) ===
        // "Santa Claus Rally" — markets tend to rally late year
        if (month == 11 || month == 12)
        {
            SectorMultipliers["Consumer Goods"] = SectorMultipliers.GetValueOrDefault("Consumer Goods", 1m) + 0.008m;
            SectorMultipliers["Luxury Goods"] = SectorMultipliers.GetValueOrDefault("Luxury Goods", 1m) + 0.006m;
            SectorMultipliers["Transportation"] = SectorMultipliers.GetValueOrDefault("Transportation", 1m) + 0.004m;
            SectorMultipliers["Technology"] = SectorMultipliers.GetValueOrDefault("Technology", 1m) + 0.005m;

            if (month == 12 && day >= 20)
            {
                // Santa Claus Rally — last 5 trading days + first 2 of January
                foreach (var sector in AllSectors)
                    SectorMultipliers[sector] = SectorMultipliers.GetValueOrDefault(sector, 1m) + 0.004m;
                VolumeMultiplier = 0.8m; // Thin holiday volume
            }

            if (month == 11 && day <= 3)
                SeasonalHeadline = "SEASONAL: Q4 Holiday Rally — consumer spending and year-end window dressing historically boost markets";
        }

        // === TAX-LOSS HARVESTING (December) ===
        if (month == 12 && day >= 10 && day <= 28)
        {
            // Losers get sold harder in December (tax optimization)
            VolatilityMultiplier *= 1.05m;
        }

        // === TRIPLE WITCHING ===
        // 3rd Friday of March, June, September, December
        // Options + futures + index futures all expire simultaneously
        if ((month == 3 || month == 6 || month == 9 || month == 12)
            && dayOfWeek == DayOfWeek.Friday && day >= 15 && day <= 21)
        {
            IsTripleWitching = true;
            VolumeMultiplier = 1.5m;          // Massive volume spike
            VolatilityMultiplier *= 1.15m;    // Higher intraday vol
            SeasonalHeadline = "TRIPLE WITCHING: Options, futures, and index futures expire simultaneously — expect elevated volume and volatility";
        }

        // === DIVIDEND QUARTER-END ===
        // Stocks go ex-dividend around quarter-end → slight drift up before, drop after
        if ((month == 3 || month == 6 || month == 9 || month == 12) && day >= 25)
        {
            SectorMultipliers["Utilities"] = SectorMultipliers.GetValueOrDefault("Utilities", 1m) + 0.003m;
            SectorMultipliers["Financials"] = SectorMultipliers.GetValueOrDefault("Financials", 1m) + 0.002m;
            SectorMultipliers["Real Estate"] = SectorMultipliers.GetValueOrDefault("Real Estate", 1m) + 0.003m;
        }

        if (SeasonalHeadline != null)
            _log.Info("Seasonal event", new { month, day, headline = SeasonalHeadline });
    }

    private static readonly string[] AllSectors =
    {
        "Technology", "Energy", "Financials", "Healthcare", "Consumer Goods",
        "Industrials", "Materials", "Real Estate", "Telecommunications",
        "Utilities", "Luxury Goods", "Transportation", "Commodities",
    };
}
