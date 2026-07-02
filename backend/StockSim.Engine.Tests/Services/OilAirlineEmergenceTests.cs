using System;
using System.Linq;
using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

/// <summary>
/// Emergent pricing, end-to-end through the general driver-coupling model (design/EMERGENT_COUPLING.md).
/// An oil shock moves fundamentals through the right channel per company — InputCost for fuel buyers
/// (margin down), OutputPrice for oil producers (revenue up) — and, with noise off, a lower fair value
/// drags the price down. Same driver, opposite effect, no GBM needed for the move.
/// </summary>
public class OilAirlineEmergenceTests
{
    [Fact]
    public void SustainedHighOil_HurtsFuelBuyers_HelpsOilSellers_ProportionalToExposure()
    {
        var loop = new GameLoop(seed: 42, stockCount: 30);

        var picks = loop.Stocks.Where(s => !s.Traits.Contains("ETF")).Take(4).ToList();
        Assert.Equal(4, picks.Count);
        var heavyBuyer = picks[0];
        var lightBuyer = picks[1];
        var oilSeller = picks[2];
        var unexposed = picks[3];
        foreach (var s in picks) { s.Revenue = 1000m; s.NetIncome = 100m; s.RevenueGrowth = 0m; s.DriverExposures.Clear(); }
        heavyBuyer.DriverExposures.Add(new DriverExposure("OilPrice", ExposureChannel.InputCost, 0.35m));
        lightBuyer.DriverExposures.Add(new DriverExposure("OilPrice", ExposureChannel.InputCost, 0.15m));
        oilSeller.DriverExposures.Add(new DriverExposure("OilPrice", ExposureChannel.OutputPrice, 0.50m));
        // unexposed: no driver exposures

        // Oil at its baseline → zero deviation → no coupling.
        loop.Economy.Data.OilPrice = 75m;
        loop.DriftFundamentals();
        var unexposedNi = unexposed.NetIncome;

        // Oil sits sustained-high. Level-based: the LEVEL (not a day-over-day change) drives the effect,
        // so a constant high oil price is a constant margin shift.
        loop.Economy.Data.OilPrice = 140m;
        loop.DriftFundamentals();

        Assert.True(heavyBuyer.NetIncome < lightBuyer.NetIncome, $"more fuel exposure → lower earnings ({heavyBuyer.NetIncome} vs {lightBuyer.NetIncome})");
        Assert.True(lightBuyer.NetIncome < unexposed.NetIncome, $"a fuel buyer earns less than an unexposed peer under high oil ({lightBuyer.NetIncome} vs {unexposed.NetIncome})");
        Assert.True(oilSeller.NetIncome > unexposed.NetIncome, $"an oil producer earns more under high oil ({oilSeller.NetIncome} vs {unexposed.NetIncome})");
        Assert.InRange(unexposed.NetIncome, unexposedNi * 0.97m, unexposedNi * 1.03m); // no exposure → ~unchanged
    }

    [Fact]
    public void DeterministicMode_PriceFollowsFundamentalFairValue_NotNoise()
    {
        var engine = new PriceEngine(seed: 42) { DeterministicMode = true };
        var fair = MakeStock("FAIR", fairValue: 150m);        // priced at fair value
        var impaired = MakeStock("IMPAIRED", fairValue: 120m); // worse fundamentals → fair value below price

        for (int i = 0; i < 30; i++)
        {
            engine.Tick(fair, TimeSpan.FromMinutes(1));
            engine.Tick(impaired, TimeSpan.FromMinutes(1));
        }

        // With all randomness stripped, the impaired stock drifts DOWN toward its lower fair value;
        // the fairly-priced one does not fall the same way. The move is purely fundamental.
        Assert.True(impaired.CurrentPrice < 150m, $"impaired price {impaired.CurrentPrice} should fall toward fair value");
        Assert.True(impaired.CurrentPrice < fair.CurrentPrice, "impaired stock should trade below the fairly-valued one");
    }

    private static Stock MakeStock(string symbol, decimal fairValue) => new(symbol, symbol, "Transportation")
    {
        CurrentPrice = 150m,
        PreviousClose = 150m,
        BidPrice = 149.90m,
        AskPrice = 150.10m,
        DayHigh = 150m,
        DayLow = 150m,
        FairValue = fairValue,
        AverageVolume = 1_000_000,
        SharesOutstanding = 100_000_000,
        Revenue = 1000m,
        NetIncome = 100m,
        RevenueGrowth = 0m,
        DebtToEquity = 1.0m,
        BaseVolatility = 0.02m,
        Personality = new CompanyPersonality
        {
            CEOName = "Test CEO",
            CEOArchetype = "Steady Hand",
            CreditRating = "A",
        },
    };
}
