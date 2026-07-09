using StockSim.Engine.Models;

namespace StockSim.Engine.Tests.Models;

/// <summary>
/// The World layer's country roster (M3, §5): countries own commodity producers and their stability
/// drives supply. Pure, deterministic — verifies ownership completeness and that instability cuts output.
/// </summary>
public class WorldStateTests
{
    [Fact]
    public void CreateDefault_OwnsEveryProducer_MarketsStayBalancedWhenStable()
    {
        var oil = CommodityMarket.CreateOilWorld();
        var gas = CommodityMarket.CreateGasWorld();
        var gold = CommodityMarket.CreateGoldWorld();
        var w = WorldState.CreateDefault(oil, gas, gold);

        Assert.Equal(12, w.Countries.Sum(c => c.Producers.Count)); // 4 oil + 4 gas + 4 gold, each owned once

        w.TickMacro(0.02m); // all stable → full output → markets balanced
        Assert.Equal(75m, oil.FundamentalPrice(75m));
        Assert.Equal(3.5m, gas.FundamentalPrice(3.5m));
        Assert.Equal(1900m, gold.FundamentalPrice(1900m));
    }

    [Fact]
    public void TickMacro_UnstableCountry_CutsItsProducersOutputAcrossMarkets()
    {
        var oil = CommodityMarket.CreateOilWorld();
        var gas = CommodityMarket.CreateGasWorld();
        var gold = CommodityMarket.CreateGoldWorld();
        var w = WorldState.CreateDefault(oil, gas, gold);

        // Gulf States owns oil "Gulf States" (34) AND gas "Offshore Gas" (20) — a petrostate.
        var gulf = w.Countries.First(c => c.Name == "Gulf States");
        gulf.Stability = 0.5m;
        w.TickMacro(0m); // apply stability, no recovery

        Assert.Equal(0.7m, oil.Producers.First(p => p.Name == "Gulf States").ProductionModifier);
        Assert.Equal(0.7m, gas.Producers.First(p => p.Name == "Offshore Gas").ProductionModifier);
        Assert.True(oil.FundamentalPrice(75m) > 75m, "oil supply cut lifts oil");
        Assert.True(gas.FundamentalPrice(3.5m) > 3.5m, "the same country's gas cut lifts gas");
    }
}
