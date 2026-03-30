namespace StockSim.Engine.Models;

/// <summary>
/// Tracks the player's reputation in the market.
/// Market Influence (0-100): How much the player's trades affect prices and attention.
/// SEC Scrutiny (0-100): How closely regulators are watching.
/// </summary>
public class PlayerReputation
{
    public decimal MarketInfluence { get; set; } = 0;  // 0-100
    public decimal SECScrutiny { get; set; } = 0;       // 0-100
    public string Title => GetTitle();

    // Influence effects
    public bool HasBetterMargin => MarketInfluence >= 20;      // Better margin rates
    public bool HasPriceImpact => MarketInfluence >= 40;       // Trades move prices more
    public bool GetsAnalystMentions => MarketInfluence >= 60;  // Appears in news
    public bool IsMarketMover => MarketInfluence >= 80;        // Significant impact

    // Scrutiny effects
    public bool HasTradeDelays => SECScrutiny >= 50;           // Orders take longer
    public bool UnderInvestigation => SECScrutiny >= 70;       // SMA investigations more likely
    public bool TradingRestricted => SECScrutiny >= 90;        // Reduced order sizes

    private string GetTitle() => MarketInfluence switch
    {
        >= 80 => "Market Mover",
        >= 60 => "Whale",
        >= 40 => "Notable Trader",
        >= 20 => "Known Trader",
        _ => "Anonymous"
    };

    /// <summary>Update reputation based on trading activity. Called daily.</summary>
    public void UpdateDaily(decimal portfolioValue, int tradesToday, int totalTrades, decimal totalPnL, bool hasSMAViolation)
    {
        // Market Influence grows with portfolio size and trading volume
        var targetInfluence = Math.Min(100m,
            (portfolioValue / 50_000m) * 10m +     // $500K = 100 influence from wealth
            (totalTrades / 10m) * 5m);              // 200 trades = 100 influence from activity
        MarketInfluence += (targetInfluence - MarketInfluence) * 0.02m; // Slow convergence
        MarketInfluence = Math.Clamp(MarketInfluence, 0, 100);

        // SEC Scrutiny rises with suspicious activity, decays slowly
        if (tradesToday > 10) SECScrutiny += 1m;           // Frequent trading
        if (hasSMAViolation) SECScrutiny += 5m;             // SMA violation
        SECScrutiny -= 0.5m;                                 // Natural decay
        SECScrutiny = Math.Clamp(SECScrutiny, 0, 100);
    }
}
