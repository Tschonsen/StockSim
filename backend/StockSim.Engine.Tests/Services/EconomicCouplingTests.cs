using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

/// <summary>
/// Driver interdependence (design/EMERGENT_COUPLING.md §7): the economy's drivers are not independent —
/// one shock ripples through the causal chain oil → inflation → rates → growth → unemployment → confidence.
/// The chain is acyclic (no self-reinforcing loop), so it cascades but stays bounded.
/// </summary>
public class EconomicCouplingTests
{
    [Fact]
    public void OilShock_CascadesThroughEconomy()
    {
        var econ = new EconomicEngine(seed: 42);
        // Start every driver at its baseline, then impose a sustained oil shock.
        econ.Data.OilPrice = 140m;           // well above baseline 75
        econ.Data.InflationRate = 2m;
        econ.Data.InterestRate = 3m;
        econ.Data.GDPGrowth = 2m;
        econ.Data.UnemploymentRate = 4m;
        econ.Data.ConsumerConfidence = 90m;

        // A quarter+ of daily propagation (oil held elevated — no random walk here).
        for (int day = 0; day < 90; day++)
            econ.PropagateDriverCoupling();

        Assert.True(econ.Data.InflationRate > 2m, $"high oil should push inflation up, got {econ.Data.InflationRate}");
        Assert.True(econ.Data.InterestRate > 3m, $"rising inflation should push rates up, got {econ.Data.InterestRate}");
        Assert.True(econ.Data.GDPGrowth < 2m, $"higher rates should slow growth, got {econ.Data.GDPGrowth}");
        Assert.True(econ.Data.ConsumerConfidence < 90m, $"the stagflation cascade should sap confidence, got {econ.Data.ConsumerConfidence}");
    }

    [Fact]
    public void NoShock_EconomyStaysAtBaseline()
    {
        var econ = new EconomicEngine(seed: 7);
        econ.Data.OilPrice = 75m;   // all at baseline → zero deviation → no pressure
        econ.Data.InflationRate = 2m;
        econ.Data.InterestRate = 3m;
        econ.Data.GDPGrowth = 2m;
        econ.Data.UnemploymentRate = 4m;
        econ.Data.ConsumerConfidence = 90m;

        for (int day = 0; day < 90; day++)
            econ.PropagateDriverCoupling();

        // With nothing off-normal, the coupling adds no drift — the economy holds.
        Assert.Equal(2m, econ.Data.InflationRate);
        Assert.Equal(3m, econ.Data.InterestRate);
        Assert.Equal(2m, econ.Data.GDPGrowth);
    }
}
