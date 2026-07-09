using System;
using System.Collections.Generic;
using System.Linq;

namespace StockSim.Engine.Models;

/// <summary>
/// A country/region that produces a commodity — an entity of the World layer (M3, see
/// design/WORLD_SIM_VISION.md §5). Supply = Baseline × Modifier; a production shock (embargo,
/// outage, ramp-up) is just a change to <see cref="ProductionModifier"/>.
/// </summary>
public class CommodityProducer
{
    public string Name { get; set; } = "";

    /// <summary>Normal daily output in the commodity's supply units.</summary>
    public decimal BaselineProduction { get; set; }

    /// <summary>Multiplier on baseline output: 1.0 = normal, &lt;1 = cut/outage, &gt;1 = surge.
    /// Production shocks move this; everything downstream reads <see cref="Production"/>.</summary>
    public decimal ProductionModifier { get; set; } = 1.0m;

    /// <summary>Current effective output.</summary>
    public decimal Production => BaselineProduction * ProductionModifier;
}

/// <summary>
/// Emergent commodity market (World layer, M3). A commodity's price is an OUTPUT of world supply
/// and demand rather than a random walk (design/WORLD_SIM_VISION.md E1/§5): producers (countries)
/// supply units, a demand consumes them, and the fundamental price is the base price scaled by the
/// supply/demand imbalance under a low price-elasticity — commodities move a lot on small imbalances,
/// which is how the real market behaves (§1a). Feeds an EconomicData indicator; the existing driver-
/// coupling + exposure machinery does the rest, so a producer's outage propagates with no new wiring.
///
/// The market mechanics are commodity-agnostic; per-commodity worlds come from the factories below,
/// and per-commodity demand coupling + calibration live in EconomicEngine.
/// </summary>
public class CommodityMarket
{
    /// <summary>Default price sensitivity to a supply/demand imbalance. Oil/gas are famously inelastic —
    /// at ~6 a 5% supply loss lifts the fundamental price ≈ +37%, in line with historical shocks.</summary>
    public const decimal DefaultElasticity = 6m;

    public List<CommodityProducer> Producers { get; } = new();

    /// <summary>Baseline global consumption (the market is balanced when this equals total supply).</summary>
    public decimal BaselineDemand { get; set; }

    /// <summary>Multiplier on demand: 1.0 = normal, &gt;1 = boom, &lt;1 = recession / demand destruction.</summary>
    public decimal DemandModifier { get; set; } = 1.0m;

    /// <summary>Transient additive demand shock from recent events (inventory surprises, disruptions).
    /// Layered on top of the economy-driven demand each tick and decayed toward 0 (rebalancing/recovery),
    /// so an event moves the price with real inertia instead of magically setting it.</summary>
    public decimal EventDemandShock { get; set; } = 0m;

    /// <summary>Price sensitivity to imbalance for this market (see <see cref="DefaultElasticity"/>).</summary>
    public decimal Elasticity { get; set; } = DefaultElasticity;

    public decimal TotalSupply => Producers.Sum(p => p.Production);

    public decimal EffectiveDemand => BaselineDemand * DemandModifier;

    /// <summary>Fundamental clearing price from the current producers and demand, at this market's elasticity.</summary>
    public decimal FundamentalPrice(decimal basePrice)
        => ClearingPrice(TotalSupply, EffectiveDemand, basePrice, Elasticity);

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

    /// <summary>A balanced crude-oil world (fictional regions per E8, real-plausible proportions):
    /// total baseline supply equals baseline demand, so the fundamental starts at the oil base price.</summary>
    public static CommodityMarket CreateOilWorld()
    {
        var m = new CommodityMarket { BaselineDemand = 100m };
        m.Producers.Add(new CommodityProducer { Name = "Gulf States", BaselineProduction = 34m });
        m.Producers.Add(new CommodityProducer { Name = "North America", BaselineProduction = 26m });
        m.Producers.Add(new CommodityProducer { Name = "Eurasia", BaselineProduction = 24m });
        m.Producers.Add(new CommodityProducer { Name = "Offshore & Other", BaselineProduction = 16m });
        return m;
    }

    /// <summary>A balanced natural-gas world. Gas is more storage/seasonally constrained than oil, so it
    /// is a touch more inelastic (bigger price swings on the same imbalance).</summary>
    public static CommodityMarket CreateGasWorld()
    {
        var m = new CommodityMarket { BaselineDemand = 100m, Elasticity = 6.5m };
        m.Producers.Add(new CommodityProducer { Name = "Shale Basins", BaselineProduction = 38m });
        m.Producers.Add(new CommodityProducer { Name = "Northern Fields", BaselineProduction = 30m });
        m.Producers.Add(new CommodityProducer { Name = "Offshore Gas", BaselineProduction = 20m });
        m.Producers.Add(new CommodityProducer { Name = "LNG Imports", BaselineProduction = 12m });
        return m;
    }

    /// <summary>A balanced gold world. Unlike oil/gas, gold is a monetary/safe-haven asset: mine supply is
    /// near-static and very inelastic, so the price is driven almost entirely by investment demand — hence
    /// a high elasticity (small demand shifts move the price a lot). The demand coupling (fear, real rates,
    /// inflation) lives in EconomicEngine, not economic activity.</summary>
    public static CommodityMarket CreateGoldWorld()
    {
        var m = new CommodityMarket { BaselineDemand = 100m, Elasticity = 8m };
        m.Producers.Add(new CommodityProducer { Name = "African Reef", BaselineProduction = 30m });
        m.Producers.Add(new CommodityProducer { Name = "Andean Mines", BaselineProduction = 28m });
        m.Producers.Add(new CommodityProducer { Name = "Siberian Fields", BaselineProduction = 24m });
        m.Producers.Add(new CommodityProducer { Name = "Oceania & Other", BaselineProduction = 18m });
        return m;
    }
}
