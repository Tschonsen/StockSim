using System.Linq;
using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

/// <summary>
/// Balance guard for the emergent driver coupling (Leitprinzip §1a: realistic magnitude, not gamey).
/// A sustained oil shock, run through the daily fundamentals + fair-value recalc over a quarter, must
/// move a fuel buyer's and an oil producer's fair value in OPPOSITE directions and stay BOUNDED — no
/// runaway, no collapse to zero. This is the multi-day, end-to-end counterpart to the one-step unit tests.
/// </summary>
public class EmergentBalanceTests
{
    [Fact]
    public void SustainedOilShock_MovesFairValue_OppositeDirections_Bounded()
    {
        var loop = new GameLoop(seed: 42, stockCount: 10);

        var airline = MakeCompany("AIR", "Transportation", fairValue: 150m);   // eps 10 × PE 15
        airline.DriverExposures.Add(new DriverExposure("OilPrice", ExposureChannel.InputCost, 0.30m));
        var producer = MakeCompany("OILP", "Energy", fairValue: 120m);          // eps 10 × PE 12
        producer.DriverExposures.Add(new DriverExposure("OilPrice", ExposureChannel.OutputPrice, 0.55m));
        loop.MutableStocks.Add(airline);
        loop.MutableStocks.Add(producer);

        // Baseline day records yesterday's oil; fair values start at equilibrium (eps × sector PE).
        loop.DriftFundamentals();
        var airStart = airline.FairValue;
        var prodStart = producer.FairValue;

        // Sustained +60% oil shock, then a quarter of daily fundamentals + fair-value adjustment.
        loop.Economy.Data.OilPrice *= 1.6m;
        for (int day = 0; day < 40; day++)
        {
            loop.DriftFundamentals();
            loop.RecalculateFairValues();
        }

        var airChange = (airline.FairValue - airStart) / airStart;
        var prodChange = (producer.FairValue - prodStart) / prodStart;

        // Opposite directions: the fuel buyer's fair value falls, the producer's rises.
        Assert.True(airChange < -0.03m, $"airline fair value should fall over the quarter, change {airChange:P1}");
        Assert.True(prodChange > 0.03m, $"producer fair value should rise over the quarter, change {prodChange:P1}");
        // Bounded — a severe but not absurd move; no runaway or collapse.
        Assert.True(airline.FairValue > airStart * 0.2m, $"airline fair value {airline.FairValue} collapsed too far");
        Assert.True(producer.FairValue < prodStart * 3m, $"producer fair value {producer.FairValue} ran away");
    }

    private static Stock MakeCompany(string symbol, string sector, decimal fairValue) => new(symbol, symbol, sector)
    {
        CurrentPrice = fairValue,
        PreviousClose = fairValue,
        FairValue = fairValue,
        SharesOutstanding = 10,
        Revenue = 1000m,
        NetIncome = 100m, // eps = 10; margin 10%
        RevenueGrowth = 0m,
    };
}
