using System;
using System.Collections.Generic;
using System.Linq;

namespace StockSim.Engine.Models;

/// <summary>
/// A country/region that produces crude oil — the first entity of the World layer (M3, see
/// design/WORLD_SIM_VISION.md §5). Supply = Baseline × Modifier; a production shock (embargo,
/// outage, ramp-up) is just a change to <see cref="ProductionModifier"/>.
/// </summary>
public class OilProducer
{
    public string Name { get; set; } = "";

    /// <summary>Normal daily output in million barrels/day (mb/d).</summary>
    public decimal BaselineProduction { get; set; }

    /// <summary>Multiplier on baseline output: 1.0 = normal, &lt;1 = cut/outage, &gt;1 = surge.
    /// Production shocks move this; everything downstream reads <see cref="Production"/>.</summary>
    public decimal ProductionModifier { get; set; } = 1.0m;

    /// <summary>Current effective output (mb/d).</summary>
    public decimal Production => BaselineProduction * ProductionModifier;
}

/// <summary>
/// Emergent crude-oil market (World layer, M3 slice 1). The oil price is an OUTPUT of world
/// supply and demand rather than a random walk (design/WORLD_SIM_VISION.md E1/§5): producers
/// (countries) supply barrels, a global demand consumes them, and the fundamental price is the
/// base price scaled by the supply/demand imbalance under a low price-elasticity — oil moves a
/// lot on small imbalances, which is how the real market behaves (§1a). This feeds
/// EconomicData.OilPrice; the existing driver-coupling + exposure machinery does the rest, so a
/// producer's outage propagates to airlines/energy/inflation with no new downstream wiring.
///
/// Steps 1+2 (this file) are pure and isolated — the live-loop integration in EconomicEngine is a
/// separate step so the random-walk stays as a fallback until this is calibrated in a running game.
/// </summary>
public class OilMarket
{
    /// <summary>Price sensitivity to a supply/demand imbalance. Oil is famously inelastic — at ~6
    /// a 5% supply loss lifts the fundamental price ≈ +37%, in line with historical shocks.</summary>
    public const decimal DefaultElasticity = 6m;

    public List<OilProducer> Producers { get; } = new();

    /// <summary>Baseline global consumption in mb/d (the market is balanced when this equals total supply).</summary>
    public decimal BaselineDemand { get; set; }

    /// <summary>Multiplier on demand: 1.0 = normal, &gt;1 = boom, &lt;1 = recession / demand destruction.</summary>
    public decimal DemandModifier { get; set; } = 1.0m;

    public decimal TotalSupply => Producers.Sum(p => p.Production);

    public decimal EffectiveDemand => BaselineDemand * DemandModifier;

    /// <summary>Fundamental clearing price from the current producers and demand, at the default elasticity.</summary>
    public decimal FundamentalPrice(decimal basePrice)
        => ClearingPrice(TotalSupply, EffectiveDemand, basePrice, DefaultElasticity);

    /// <summary>Fundamental clearing price at a specified elasticity (for calibration/tests).</summary>
    public decimal FundamentalPrice(decimal basePrice, decimal elasticity)
        => ClearingPrice(TotalSupply, EffectiveDemand, basePrice, elasticity);

    /// <summary>
    /// Pure supply/demand price kernel: <c>basePrice × (demand/supply)^elasticity</c>.
    /// Balanced (supply == demand) → basePrice exactly. Deficit (demand &gt; supply) → higher;
    /// glut → lower. Guards degenerate supply/demand so the caller (which clamps to a sane band)
    /// never divides by zero or takes a fractional power of a non-positive number.
    /// </summary>
    public static decimal ClearingPrice(decimal supply, decimal demand, decimal basePrice, decimal elasticity)
    {
        if (supply <= 0m) return basePrice * 4m;    // supply collapse → spike (caller clamps to its ceiling)
        if (demand <= 0m) return basePrice * 0.25m; // demand collapse → floor
        var ratio = (double)(demand / supply);
        var factor = Math.Pow(ratio, (double)elasticity);
        return basePrice * (decimal)factor;
    }
}
