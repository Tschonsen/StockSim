using StockSim.Engine.Models;

namespace StockSim.Engine.Tests.Models;

/// <summary>
/// World-layer country (M3, design/WORLD_SIM_VISION.md §5): a country owns commodity production and its
/// stability drives that output, so instability becomes an endogenous supply shock. Pure, deterministic.
/// </summary>
public class CountryTests
{
    [Fact]
    public void SupplyFactor_MapsStabilityToOutput_Monotonic()
    {
        Assert.Equal(1.0m, new Country { Stability = 1.0m }.SupplyFactor);  // stable → full output
        Assert.Equal(0.7m, new Country { Stability = 0.5m }.SupplyFactor);  // half → 0.4 + 0.6*0.5
        Assert.Equal(0.4m, new Country { Stability = 0.0m }.SupplyFactor);  // collapse → floor, not zero
        Assert.True(new Country { Stability = 0.8m }.SupplyFactor > new Country { Stability = 0.3m }.SupplyFactor);
    }

    [Fact]
    public void SupplyFactor_ClampsOutOfRangeStability()
    {
        Assert.Equal(1.0m, new Country { Stability = 1.5m }.SupplyFactor);
        Assert.Equal(0.4m, new Country { Stability = -0.5m }.SupplyFactor);
    }

    [Fact]
    public void ApplyStabilityToProduction_SetsOwnedProducers()
    {
        var c = new Country { Name = "Petrostan", Stability = 0.5m };
        var p1 = new CommodityProducer { Name = "Field A", BaselineProduction = 20m };
        var p2 = new CommodityProducer { Name = "Field B", BaselineProduction = 10m };
        c.Producers.Add(p1);
        c.Producers.Add(p2);

        c.ApplyStabilityToProduction();

        Assert.Equal(0.7m, p1.ProductionModifier);
        Assert.Equal(0.7m, p2.ProductionModifier);
        Assert.Equal(14m, p1.Production); // 20 * 0.7
    }

    [Fact]
    public void Growth_SinksIntoRecessionWithInstability()
    {
        Assert.Equal(2.5m, new Country { Stability = 1.0m, BaselineGrowth = 2.5m, RecessionDepth = 8m }.Growth);
        Assert.Equal(-5.5m, new Country { Stability = 0.0m, BaselineGrowth = 2.5m, RecessionDepth = 8m }.Growth);
        var mild = new Country { Stability = 0.6m, BaselineGrowth = 2.5m, RecessionDepth = 8m }.Growth;
        Assert.InRange(mild, -1.0m, 0m); // 2.5 - 0.4*8 = -0.7
    }

    [Fact]
    public void RecoverStability_HealsTowardBaseline()
    {
        var c = new Country { Stability = 0.5m, BaselineStability = 1.0m };
        c.RecoverStability(0.5m);
        Assert.Equal(0.75m, c.Stability); // closed half the gap
        c.RecoverStability(0.5m);
        Assert.Equal(0.875m, c.Stability);
    }

    [Fact]
    public void CountryInstability_RaisesTheCommodityFundamental_EndToEnd()
    {
        // A country owns a slice of a balanced oil world; when it destabilises, its output falls and the
        // market's fundamental price rises — the geopolitical supply shock, purely from the model.
        var market = CommodityMarket.CreateOilWorld();
        var basePrice = market.FundamentalPrice(75m);
        Assert.Equal(75m, basePrice); // balanced to start

        // "Gulf States" (34% of supply) is a country that destabilises to 0.6.
        var gulf = new Country { Name = "Gulf States", Stability = 0.6m };
        gulf.Producers.Add(market.Producers.First(p => p.Name == "Gulf States"));
        gulf.ApplyStabilityToProduction();

        var shocked = market.FundamentalPrice(75m);
        Assert.True(shocked > basePrice * 1.15m, $"instability should spike oil, got {shocked}");
    }
}
