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

        w.Add("Gulf States", P(oil, "Gulf States"), P(gas, "Offshore Gas"));
        w.Add("North America", P(oil, "North America"), P(gas, "Shale Basins"));
        w.Add("Eurasia", P(oil, "Eurasia"), P(gas, "Northern Fields"), P(gold, "Siberian Fields"));
        w.Add("Offshore Bloc", P(oil, "Offshore & Other"), P(gas, "LNG Imports"));
        w.Add("African Union", P(gold, "African Reef"));
        w.Add("Andean States", P(gold, "Andean Mines"));
        w.Add("Oceania", P(gold, "Oceania & Other"));
        return w;
    }

    private void Add(string name, params CommodityProducer[] producers)
    {
        var c = new Country { Name = name };
        c.Producers.AddRange(producers);
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
}
