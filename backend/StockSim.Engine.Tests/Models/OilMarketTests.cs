using StockSim.Engine.Models;

namespace StockSim.Engine.Tests.Models;

/// <summary>
/// World-layer oil market (M3 slice 1, design/WORLD_SIM_VISION.md §5): the oil price EMERGES from
/// country supply and global demand instead of a random walk. Pure, deterministic, calibrated to
/// real oil behaviour (inelastic — small imbalances move price a lot, §1a). No live-loop wiring yet.
/// </summary>
public class OilMarketTests
{
    private const decimal Base = 75m; // baseline $/barrel, matches EconomicData.OilPrice normal

    // ---- ClearingPrice kernel ----

    [Fact]
    public void ClearingPrice_Balanced_ReturnsBasePriceExactly()
    {
        Assert.Equal(Base, OilMarket.ClearingPrice(100m, 100m, Base, OilMarket.DefaultElasticity));
    }

    [Fact]
    public void ClearingPrice_SupplyDeficit_RaisesPrice()
    {
        var price = OilMarket.ClearingPrice(95m, 100m, Base, OilMarket.DefaultElasticity);
        Assert.True(price > Base, $"deficit should lift price, got {price}");
    }

    [Fact]
    public void ClearingPrice_Glut_LowersPrice()
    {
        var price = OilMarket.ClearingPrice(105m, 100m, Base, OilMarket.DefaultElasticity);
        Assert.True(price < Base, $"glut should depress price, got {price}");
    }

    [Fact]
    public void ClearingPrice_FivePercentSupplyLoss_IsRealisticShock()
    {
        // A ~5% supply loss (demand 100 vs supply 95) at inelastic ~6 lifts price ~+37% —
        // a big-but-historical oil shock, not an arcade 3x.
        var price = OilMarket.ClearingPrice(95m, 100m, Base, OilMarket.DefaultElasticity);
        Assert.InRange(price, Base * 1.30m, Base * 1.45m);
    }

    [Fact]
    public void ClearingPrice_DemandBoom_RaisesPrice_Symmetric()
    {
        // Demand +5% mirrors supply -5% (same ratio) → same price move.
        var demandUp = OilMarket.ClearingPrice(100m, 105m, Base, OilMarket.DefaultElasticity);
        var supplyDown = OilMarket.ClearingPrice(100m / 1.05m, 100m, Base, OilMarket.DefaultElasticity);
        Assert.InRange(demandUp, Base * 1.30m, Base * 1.45m);
        Assert.InRange(demandUp - supplyDown, -0.01m, 0.01m);
    }

    [Fact]
    public void ClearingPrice_HigherElasticity_AmplifiesSameImbalance()
    {
        var soft = OilMarket.ClearingPrice(95m, 100m, Base, 3m);
        var hard = OilMarket.ClearingPrice(95m, 100m, Base, 9m);
        Assert.True(hard > soft, $"more inelastic → bigger move, got soft={soft}, hard={hard}");
    }

    [Fact]
    public void ClearingPrice_GuardsDegenerateInputs_NoCrash()
    {
        Assert.True(OilMarket.ClearingPrice(0m, 100m, Base, OilMarket.DefaultElasticity) > Base); // supply collapse → high
        Assert.True(OilMarket.ClearingPrice(100m, 0m, Base, OilMarket.DefaultElasticity) < Base); // demand collapse → low
    }

    // ---- OilProducer ----

    [Fact]
    public void Producer_ProductionIsBaselineTimesModifier()
    {
        var p = new OilProducer { Name = "Region A", BaselineProduction = 12m };
        Assert.Equal(12m, p.Production);            // default modifier 1.0
        p.ProductionModifier = 0.7m;
        Assert.Equal(8.4m, p.Production);           // 30% outage
    }

    // ---- OilMarket aggregation ----

    [Fact]
    public void Market_TotalSupply_SumsProducers_AndBalancedIsBasePrice()
    {
        var m = BalancedMarket();
        Assert.Equal(30m, m.TotalSupply);           // 12 + 10 + 8
        Assert.Equal(30m, m.EffectiveDemand);
        Assert.Equal(Base, m.FundamentalPrice(Base));
    }

    [Fact]
    public void Market_ProducerOutage_RaisesPrice_Endogenously()
    {
        var m = BalancedMarket();
        // Largest producer cuts output 30% → total supply -12% → a real supply shock.
        m.Producers.First(p => p.Name == "Big").ProductionModifier = 0.7m;
        var price = m.FundamentalPrice(Base);
        Assert.True(price > Base * 1.5m, $"a 12% supply loss should spike oil, got {price}");
        // ...and it stays bounded (not a runaway) at this magnitude.
        Assert.True(price < Base * 3m, $"should stay in a plausible band, got {price}");
    }

    [Fact]
    public void Market_DemandDestruction_LowersPrice()
    {
        var m = BalancedMarket();
        m.DemandModifier = 0.9m; // -10% demand (recession)
        Assert.True(m.FundamentalPrice(Base) < Base, "demand destruction should soften oil");
    }

    /// <summary>Three producers whose baseline output exactly meets baseline demand (balanced world).</summary>
    private static OilMarket BalancedMarket()
    {
        var m = new OilMarket { BaselineDemand = 30m };
        m.Producers.Add(new OilProducer { Name = "Big", BaselineProduction = 12m });
        m.Producers.Add(new OilProducer { Name = "Mid", BaselineProduction = 10m });
        m.Producers.Add(new OilProducer { Name = "Small", BaselineProduction = 8m });
        return m;
    }
}
