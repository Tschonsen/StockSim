using System.Collections.Generic;

namespace StockSim.Engine.Models;

/// <summary>
/// A country/region on the World layer (M3, design/WORLD_SIM_VISION.md §5). Slice 1: a country owns
/// commodity production (its resource endowment) and has a Stability attribute; instability disrupts its
/// output, which flows through the CommodityMarket to prices and — via the existing exposures/coupling —
/// to sectors. So a geopolitical shock ORIGINATES in the world instead of being an injected price move.
/// GDP contribution, politics and direct sector-driving come in later slices. Pure and isolated here.
/// </summary>
public class Country
{
    public string Name { get; set; } = "";

    /// <summary>Political/operational stability in [0,1]: 1 = stable, 0 = collapse. Low stability risks
    /// supply disruption. Events push it down; it recovers toward <see cref="BaselineStability"/> over time.</summary>
    public decimal Stability { get; set; } = 1.0m;

    /// <summary>The level Stability heals back toward (the country's "normal").</summary>
    public decimal BaselineStability { get; set; } = 1.0m;

    /// <summary>Commodity producers this country owns (its resource endowment), possibly across markets.</summary>
    public List<CommodityProducer> Producers { get; } = new();

    /// <summary>Production multiplier from stability: stable (1) → full output (1.0); collapse (0) → a floor
    /// (0.4), because instability disrupts but rarely zeroes a nation's output overnight. Monotonic, clamped.</summary>
    public decimal SupplyFactor => 0.4m + 0.6m * Clamp01(Stability);

    /// <summary>Drive each owned producer's output from current stability. Called each macro tick.</summary>
    public void ApplyStabilityToProduction()
    {
        var f = SupplyFactor;
        foreach (var p in Producers) p.ProductionModifier = f;
    }

    /// <summary>Heal stability toward baseline (disruptions recover). `rate` is the fraction closed per tick.</summary>
    public void RecoverStability(decimal rate)
    {
        Stability += (BaselineStability - Stability) * rate;
    }

    private static decimal Clamp01(decimal v) => v < 0m ? 0m : v > 1m ? 1m : v;
}
