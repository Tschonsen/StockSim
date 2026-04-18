namespace StockSim.Engine.Models;

/// <summary>
/// Market conditions at game start. Determines the overall trend
/// of historical price data. See Spec 11.4.
/// </summary>
public enum MarketPhase
{
    /// <summary>Market has been rising over the past 6 months. 40% probability.</summary>
    Bull,

    /// <summary>Sideways movement. 40% probability.</summary>
    Neutral,

    /// <summary>Market has been falling. 20% probability.</summary>
    Bear,
}
