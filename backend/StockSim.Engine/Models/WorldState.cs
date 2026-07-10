using System.Collections.Generic;
using System.Linq;

namespace StockSim.Engine.Models;

/// <summary>
/// The World layer's country roster (M3, design/WORLD_SIM_VISION.md §5). Countries own the commodity
/// producers (their resource endowment, possibly across markets — a petrostate owns oil AND gas), and
/// each macro tick their stability drives that production and then heals. So a country's instability is
/// an endogenous supply shock that flows through the markets to prices and sectors.
///
/// Slice 1 covers supply only; GDP contribution, politics and direct sector-driving are later slices.
/// </summary>
public class WorldState
{
    public List<Country> Countries { get; } = new();

    /// <summary>Build the default world: countries owning the producers of the given markets. Every
    /// producer is owned by exactly one country, so with all countries stable the markets stay balanced.</summary>
    public static WorldState CreateDefault(CommodityMarket oil, CommodityMarket gas, CommodityMarket gold)
    {
        var w = new WorldState();
        CommodityProducer P(CommodityMarket m, string name) => m.Producers.First(p => p.Name == name);

        // Economic weights (GDP share) sum to 1.0; larger economies drag global growth more when unstable.
        w.Add("Gulf States", 0.10m, new[] { "oil", "gas" }, P(oil, "Gulf States"), P(gas, "Offshore Gas"));
        w.Add("North America", 0.28m, new[] { "oil", "gas" }, P(oil, "North America"), P(gas, "Shale Basins"));
        w.Add("Eurasia", 0.22m, new[] { "oil", "gas", "gold" }, P(oil, "Eurasia"), P(gas, "Northern Fields"), P(gold, "Siberian Fields"));
        w.Add("Offshore Bloc", 0.10m, new[] { "oil", "gas" }, P(oil, "Offshore & Other"), P(gas, "LNG Imports"));
        w.Add("African Union", 0.10m, new[] { "gold" }, P(gold, "African Reef"));
        w.Add("Andean States", 0.10m, new[] { "gold" }, P(gold, "Andean Mines"));
        w.Add("Oceania", 0.10m, new[] { "gold" }, P(gold, "Oceania & Other"));
        return w;
    }

    private void Add(string name, decimal weight, string[] commodities, params CommodityProducer[] producers)
    {
        var c = new Country { Name = name, EconomicWeight = weight };
        c.Producers.AddRange(producers);
        c.Commodities.AddRange(commodities);
        Countries.Add(c);
    }

    /// <summary>Macro tick: apply each country's stability to its production, then heal stability toward baseline.</summary>
    public void TickMacro(decimal recoveryRate)
    {
        foreach (var c in Countries)
        {
            c.ApplyStabilityToProduction();
            c.RecoverStability(recoveryRate);
        }
    }

    /// <summary>Global growth as the economic-weight-weighted average of country growth — so global GDP
    /// EMERGES from the countries, and a big country's instability drags the whole economy.</summary>
    public decimal AggregateGrowth()
    {
        decimal wsum = Countries.Sum(c => c.EconomicWeight);
        return wsum <= 0m ? 0m : Countries.Sum(c => c.EconomicWeight * c.Growth) / wsum;
    }
}

/// <summary>A geopolitical instability shock that fired this tick — for the news layer to narrate. The
/// price effect is already emergent (the country's supply drop moves the commodity market); this only
/// carries what's needed to tell the story.</summary>
public record GeopoliticalShock(string Country, decimal Severity, System.Collections.Generic.IReadOnlyList<string> Commodities);
