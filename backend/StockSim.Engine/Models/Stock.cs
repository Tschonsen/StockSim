namespace StockSim.Engine.Models;

/// <summary>
/// Represents a single tradeable stock with all its properties.
/// Core data model as defined in Game Design Bible sections 11.3.1-11.3.6.
/// </summary>
public class Stock
{
    // Identity
    public string Symbol { get; }
    public string Name { get; }
    public string Sector { get; }
    public string Subsector { get; set; } = "";

    // Price data
    public decimal CurrentPrice { get; set; }
    public decimal PreviousClose { get; set; }
    public decimal BidPrice { get; set; }
    public decimal AskPrice { get; set; }
    public decimal DayHigh { get; set; }
    public decimal DayLow { get; set; }
    public decimal YearHigh { get; set; }
    public decimal YearLow { get; set; }

    // Volume
    public long DayVolume { get; set; }
    public long AverageVolume { get; set; }

    // Share structure (Bible 5.8)
    public long SharesOutstanding { get; set; }
    public decimal InsiderOwnership { get; set; }
    public decimal InstitutionalOwnership { get; set; }
    public decimal ShortInterest { get; set; }

    // Fundamentals (Bible 11.3.2)
    public decimal Revenue { get; set; }
    public decimal NetIncome { get; set; }
    public decimal DividendYield { get; set; }
    public decimal DebtToEquity { get; set; }
    public decimal RevenueGrowth { get; set; }
    public int Employees { get; set; }

    // Trading parameters (Bible 11.3.3)
    public decimal BaseVolatility { get; set; }
    public int LiquidityScore { get; set; }
    public decimal ShortBorrowAvailability { get; set; }
    public decimal FairValue { get; set; }

    // Spread dynamics: multiplier that spikes during events and decays
    /// <summary>Spread multiplier (1.0 = normal, 3-5 during events). Decays toward 1.0.</summary>
    public decimal SpreadMultiplier { get; set; } = 1.0m;

    // Autocorrelation tracking
    /// <summary>5-day rolling return for momentum effect.</summary>
    public decimal Return5Day { get; set; }
    /// <summary>20-day rolling return for mean reversion.</summary>
    public decimal Return20Day { get; set; }

    // IPO tracking
    /// <summary>Date when this stock IPO'd (null for pre-existing stocks).</summary>
    public DateTime? IPODate { get; set; }
    /// <summary>Lock-up expiry date (180 days after IPO). Insider selling flood expected.</summary>
    public DateTime? LockUpExpiry { get; set; }
    /// <summary>Whether lock-up has expired (triggers insider selling pressure).</summary>
    public bool LockUpExpired { get; set; }

    // SSR — Alternative Uptick Rule (Bible 4.4.2)
    // Triggered when stock falls ≥10% from PreviousClose. Lasts rest of day + next trading day.
    public bool IsSSR { get; set; }
    public DateTime? SSRUntilDate { get; set; }

    // Traits (Bible 11.3.4)
    public List<string> Traits { get; } = new();

    // Company personality (Session 12: CEO, products, story, rivalries)
    public CompanyPersonality? Personality { get; set; }

    // Analyst Rating: 1=Strong Sell, 2=Sell, 3=Hold, 4=Buy, 5=Strong Buy
    public decimal AnalystRating { get; set; } = 3.0m;
    public decimal TargetPrice { get; set; }
    public string AnalystConsensus => AnalystRating switch
    {
        >= 4.5m => "Strong Buy",
        >= 3.5m => "Buy",
        >= 2.5m => "Hold",
        >= 1.5m => "Sell",
        _ => "Strong Sell",
    };

    // Computed properties
    public decimal DayChange => CurrentPrice - PreviousClose;

    public decimal DayChangePercent =>
        PreviousClose != 0 ? Math.Round((CurrentPrice - PreviousClose) / PreviousClose * 100, 2) : 0m;

    public decimal Spread => AskPrice - BidPrice;

    public decimal SpreadPercent =>
        BidPrice != 0 ? Math.Round(Spread / BidPrice * 100, 4) : 0m;

    public decimal MarketCap => CurrentPrice * SharesOutstanding;

    public decimal PERatio
    {
        get
        {
            if (CurrentPrice < 1.0m) return 0m; // Penny stocks show N/A
            if (NetIncome == 0) return 0m;
            var pe = Math.Round(MarketCap / NetIncome, 2);
            return pe > 999m ? 999m : pe < -999m ? -999m : pe;
        }
    }

    public long Float =>
        (long)(SharesOutstanding * (1m - InsiderOwnership * 0.6m));

    public decimal FloatPercentage =>
        SharesOutstanding != 0 ? (decimal)Float / SharesOutstanding : 0m;

    public decimal ShortInterestOfFloat =>
        Float != 0 ? ShortInterest / Float : 0m;

    /// <summary>
    /// Issue 20: Dividend yield trap flag. True when yield is suspiciously high (>8%)
    /// combined with deteriorating fundamentals (revenue declining >10% or debt/equity >2.5).
    /// </summary>
    public bool IsDividendTrap =>
        DividendYield > 0.08m && (RevenueGrowth < -0.10m || DebtToEquity > 2.5m);

    public Stock(string symbol, string name, string sector)
    {
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Sector = sector ?? throw new ArgumentNullException(nameof(sector));
    }

    public override string ToString() => $"{Symbol} ({Name}) @ {CurrentPrice:C}";
}

public class InsiderTradeEvent
{
    public string Symbol { get; set; } = "";
    public string Title { get; set; } = "";
    public bool IsBuy { get; set; }
    public int Shares { get; set; }
    public decimal Value { get; set; }
    public decimal Price { get; set; }
}

public class ShortSqueezeWarning
{
    public string Symbol { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public decimal PriceChangePercent { get; set; }
    public decimal ShortInterestPercent { get; set; }
    public bool PlayerHasShortPosition { get; set; }
}

public class StockSplitEvent
{
    public string Symbol { get; set; } = "";
    public string Ratio { get; set; } = "";
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
}
