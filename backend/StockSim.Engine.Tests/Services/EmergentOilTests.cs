using System;
using System.Collections.Generic;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

/// <summary>
/// Live verification of emergent oil pricing (M3 slice 1). Per the world-sim test strategy
/// (WORLD_SIM_VISION.md §13), emergent effects are checked statistically over a run, not with a
/// single deterministic assert: oil must stay in a realistic band across seeds and track the
/// economy directionally (procyclical demand). Exercises the real EconomicEngine.TickDay loop.
/// </summary>
public class EmergentOilTests
{
    private static readonly DateTime Start = new(2026, 1, 1);

    [Fact]
    public void EmergentOil_TracksEconomy_BoomHigherThanRecession()
    {
        decimal RunPinnedEconomy(decimal gdp, decimal pmi)
        {
            var e = new EconomicEngine(42) { EmergentOilPricing = true };
            e.Data.OilPrice = 75m;
            for (int i = 0; i < 90; i++)
            {
                e.Data.GDPGrowth = gdp;          // pin the economy so the demand signal is steady
                e.Data.ManufacturingPMI = pmi;
                e.TickDay(Start.AddDays(i));
            }
            return e.Data.OilPrice;
        }

        var boom = RunPinnedEconomy(5.0m, 58m);
        var recession = RunPinnedEconomy(0.0m, 44m);
        Assert.True(boom > recession + 5m,
            $"procyclical: boom oil ({boom:F1}) should clearly exceed recession oil ({recession:F1})");
    }

    [Fact]
    public void EmergentOil_YearPlaytest_StaysInRealisticBand_NoClampPinning()
    {
        foreach (var seed in new[] { 1, 7, 42, 123, 999 })
        {
            var e = new EconomicEngine(seed) { EmergentOilPricing = true };
            var prices = new List<decimal>();
            for (int i = 0; i < 252; i++)
            {
                e.TickDay(Start.AddDays(i));
                var p = e.Data.OilPrice;
                Assert.False(double.IsNaN((double)p), $"seed {seed} day {i}: oil is NaN");
                Assert.True(p > 20m && p < 150m, $"seed {seed} day {i}: oil pinned at a clamp ({p:F1})");
                prices.Add(p);
            }
            prices.Sort();
            var median = prices[prices.Count / 2];
            Assert.InRange(median, 45m, 110m); // realistic central band over a natural year
        }
    }
}
