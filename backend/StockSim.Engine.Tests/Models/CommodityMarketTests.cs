using StockSim.Engine.Models;

namespace StockSim.Engine.Tests.Models;

/// <summary>
/// World-layer commodity market (M3, design/WORLD_SIM_VISION.md §5): a commodity price EMERGES from
/// country supply and demand instead of a random walk. Pure, deterministic, calibrated to real
/// commodity behaviour (inelastic — small imbalances move price a lot, §1a). No live-loop wiring here.
/// </summary>
public class CommodityMarketTests
{
    private const decimal Base = 75m; // baseline $/barrel, matches EconomicData.OilPrice normal

    // ---- ClearingPrice kernel ----

    [Fact]
    public void ClearingPrice_Balanced_ReturnsBasePriceExactly()
    {
        Assert.Equal(Base, CommodityMarket.ClearingPrice(100m, 100m, Base, CommodityMarket.DefaultElasticity));
    }

    [Fact]
    public void ClearingPrice_SupplyDeficit_RaisesPrice()
    {
        var price = CommodityMarket.ClearingPrice(95m, 100m, Base, CommodityMarket.DefaultElasticity);
        Assert.True(price > Base, $"deficit should lift price, got {price}");
    }

    [Fact]
    public void ClearingPrice_Glut_LowersPrice()
    {
        var price = CommodityMarket.ClearingPrice(105m, 100m, Base, CommodityMarket.DefaultElasticity);
        Assert.True(price < Base, $"glut should depress price, got {price}");
    }

    [Fact]
    public void ClearingPrice_FivePercentSupplyLoss_IsRealisticShock()
    {
        // A ~5% supply loss (demand 100 vs supply 95) at inelastic ~6 lifts price ~+37% —
        // a big-but-historical shock, not an arcade 3x.
        var price = CommodityMarket.ClearingPrice(95m, 100m, Base, CommodityMarket.DefaultElasticity);
        Assert.InRange(price, Base * 1.30m, Base * 1.45m);
    }

    [Fact]
    public void ClearingPrice_DemandBoom_RaisesPrice_Symmetric()
    {
        var demandUp = CommodityMarket.ClearingPrice(100m, 105m, Base, CommodityMarket.DefaultElasticity);
        var supplyDown = CommodityMarket.ClearingPrice(100m / 1.05m, 100m, Base, CommodityMarket.DefaultElasticity);
        Assert.InRange(demandUp, Base * 1.30m, Base * 1.45m);
        Assert.InRange(demandUp - supplyDown, -0.01m, 0.01m);
    }

    [Fact]
    public void ClearingPrice_HigherElasticity_AmplifiesSameImbalance()
    {
        var soft = CommodityMarket.ClearingPrice(95m, 100m, Base, 3m);
        var hard = CommodityMarket.ClearingPrice(95m, 100m, Base, 9m);
        Assert.True(hard > soft, $"more inelastic → bigger move, got soft={soft}, hard={hard}");
    }

    [Fact]
    public void ClearingPrice_GuardsDegenerateInputs_NoCrash()
    {
        Assert.True(CommodityMarket.ClearingPrice(0m, 100m, Base, CommodityMarket.DefaultElasticity) > Base);
        Assert.True(CommodityMarket.ClearingPrice(100m, 0m, Base, CommodityMarket.DefaultElasticity) < Base);
    }

    // ---- CommodityProducer ----

    [Fact]
    public void Producer_ProductionIsBaselineTimesModifier()
    {
        var p = new CommodityProducer { Name = "Region A", BaselineProduction = 12m };
        Assert.Equal(12m, p.Production);
        p.ProductionModifier = 0.7m;
        Assert.Equal(8.4m, p.Production);
    }

    // ---- Market aggregation + worlds ----

    [Fact]
    public void Market_TotalSupply_SumsProducers_AndBalancedIsBasePrice()
    {
        var m = BalancedMarket();
        Assert.Equal(30m, m.TotalSupply);
        Assert.Equal(30m, m.EffectiveDemand);
        Assert.Equal(Base, m.FundamentalPrice(Base));
    }

    [Fact]
    public void Market_ProducerOutage_RaisesPrice_Endogenously()
    {
        var m = BalancedMarket();
        m.Producers.First(p => p.Name == "Big").ProductionModifier = 0.7m; // -30% on the largest → supply -12%
        var price = m.FundamentalPrice(Base);
        Assert.True(price > Base * 1.5m, $"a 12% supply loss should spike price, got {price}");
        Assert.True(price < Base * 3m, $"should stay in a plausible band, got {price}");
    }

    [Fact]
    public void Market_DemandDestruction_LowersPrice()
    {
        var m = BalancedMarket();
        m.DemandModifier = 0.9m;
        Assert.True(m.FundamentalPrice(Base) < Base, "demand destruction should soften price");
    }

    [Fact]
    public void CreateOilWorld_IsBalanced_AtBasePrice()
    {
        var m = CommodityMarket.CreateOilWorld();
        Assert.Equal(100m, m.TotalSupply);              // 34 + 26 + 24 + 16
        Assert.Equal(m.TotalSupply, m.EffectiveDemand);
        Assert.Equal(75m, m.FundamentalPrice(75m));
    }

    [Fact]
    public void CreateGasWorld_IsBalanced_AtBasePrice()
    {
        var m = CommodityMarket.CreateGasWorld();
        Assert.Equal(100m, m.TotalSupply);              // 38 + 30 + 20 + 12
        Assert.Equal(m.TotalSupply, m.EffectiveDemand);
        Assert.Equal(3.5m, m.FundamentalPrice(3.5m));
    }

    [Fact]
    public void CreateGoldWorld_IsBalanced_AtBasePrice()
    {
        var m = CommodityMarket.CreateGoldWorld();
        Assert.Equal(100m, m.TotalSupply);              // 30 + 28 + 24 + 18
        Assert.Equal(m.TotalSupply, m.EffectiveDemand);
        Assert.Equal(1900m, m.FundamentalPrice(1900m));
    }

    private static CommodityMarket BalancedMarket()
    {
        var m = new CommodityMarket { BaselineDemand = 30m };
        m.Producers.Add(new CommodityProducer { Name = "Big", BaselineProduction = 12m });
        m.Producers.Add(new CommodityProducer { Name = "Mid", BaselineProduction = 10m });
        m.Producers.Add(new CommodityProducer { Name = "Small", BaselineProduction = 8m });
        return m;
    }
}
