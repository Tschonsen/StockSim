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
            var e = new EconomicEngine(42) { EmergentCommodityPricing = true, CountryInstabilityChance = 0 };
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
    public void EmergentOil_EventShock_LiftsPriceVsNoShock()
    {
        // Same seed → identical drift noise and scheduled releases; the only difference is the injected
        // bullish inventory shock, so its effect is isolated. A shock must move the price with inertia.
        decimal Run(bool withShock)
        {
            var e = new EconomicEngine(7) { EmergentCommodityPricing = true, CountryInstabilityChance = 0 };
            e.Data.OilPrice = 75m;
            for (int i = 0; i < 10; i++)
            {
                e.Data.GDPGrowth = 2m;               // neutral economy → shock is the only mover
                e.Data.ManufacturingPMI = 50m;
                if (i == 1 && withShock) e.OilMarket.EventDemandShock += 0.10m; // bullish draw
                e.TickDay(Start.AddDays(i));
            }
            return e.Data.OilPrice;
        }
        Assert.True(Run(true) > Run(false) + 2m, "a bullish inventory shock should lift oil vs no shock");
    }

    [Fact]
    public void EmergentOil_YearPlaytest_StaysInRealisticBand_NoClampPinning()
    {
        foreach (var seed in new[] { 1, 7, 42, 123, 999 })
        {
            var e = new EconomicEngine(seed) { EmergentCommodityPricing = true, CountryInstabilityChance = 0 };
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

    [Fact]
    public void EmergentGas_TracksIndustrialActivity_BoomHigherThanRecession()
    {
        decimal Run(decimal pmi, decimal gdp)
        {
            var e = new EconomicEngine(42) { EmergentCommodityPricing = true, CountryInstabilityChance = 0 };
            e.Data.NatGasPrice = 3.5m;
            for (int i = 0; i < 90; i++)
            {
                e.Data.ManufacturingPMI = pmi;   // gas demand is PMI-weighted (industrial/power)
                e.Data.GDPGrowth = gdp;
                e.TickDay(Start.AddDays(i));
            }
            return e.Data.NatGasPrice;
        }
        Assert.True(Run(58m, 4.5m) > Run(44m, 0.5m) + 0.2m, "gas should track industrial activity");
    }

    [Fact]
    public void EmergentGas_YearPlaytest_StaysInRealisticBand_NoClampPinning()
    {
        foreach (var seed in new[] { 1, 7, 42, 123, 999 })
        {
            var e = new EconomicEngine(seed) { EmergentCommodityPricing = true, CountryInstabilityChance = 0 };
            var prices = new List<decimal>();
            for (int i = 0; i < 252; i++)
            {
                e.TickDay(Start.AddDays(i));
                var p = e.Data.NatGasPrice;
                Assert.True(p > 1.5m && p < 15m, $"seed {seed} day {i}: gas pinned at a clamp ({p:F2})");
                prices.Add(p);
            }
            prices.Sort();
            Assert.InRange(prices[prices.Count / 2], 2.0m, 6.5m); // realistic band around ~$3.5
        }
    }

    [Fact]
    public void CountryInstability_SpikesOwnedCommodity_AndPropagates()
    {
        // Destabilising a major oil-producing country cuts its output → oil spikes, purely from the world.
        // Instability chance 0 so only the manual shock moves things; neutral economy isolates it further.
        decimal Run(bool destabilise)
        {
            var e = new EconomicEngine(42) { EmergentCommodityPricing = true, CountryInstabilityChance = 0 };
            e.Data.OilPrice = 75m;
            if (destabilise) e.World.Countries.First(c => c.Name == "Gulf States").Stability = 0.4m;
            for (int i = 0; i < 12; i++)
            {
                e.Data.GDPGrowth = 2m; e.Data.ManufacturingPMI = 50m; // neutral economy
                e.TickDay(Start.AddDays(i));
            }
            return e.Data.OilPrice;
        }
        Assert.True(Run(true) > Run(false) + 10m, "a destabilised major oil producer should spike oil");
    }

    [Fact]
    public void CountryInstability_DragsGlobalGDP()
    {
        decimal Run(bool destabilise)
        {
            var e = new EconomicEngine(42) { EmergentCommodityPricing = true, CountryInstabilityChance = 0 };
            e.Data.GDPGrowth = 2.5m;
            for (int i = 0; i < 60; i++)
            {
                // Hold a big economy destabilised (counter the daily heal) to isolate the drag on global GDP.
                if (destabilise) e.World.Countries.First(c => c.Name == "North America").Stability = 0.4m;
                e.TickDay(Start.AddDays(i));
            }
            return e.Data.GDPGrowth;
        }
        Assert.True(Run(true) < Run(false) - 0.5m, "sustained instability in a big economy should drag global GDP");
    }

    [Fact]
    public void CountryInstability_RecordsShockForNarration()
    {
        var e = new EconomicEngine(42) { EmergentCommodityPricing = true, CountryInstabilityChance = 1.0 };
        e.TickDay(Start);
        Assert.Single(e.GeopoliticalShocksThisTick);
        var shock = e.GeopoliticalShocksThisTick[0];
        Assert.False(string.IsNullOrEmpty(shock.Country));
        Assert.NotEmpty(shock.Commodities);
        Assert.InRange(shock.Severity, 0.2m, 0.6m);
    }

    [Fact]
    public void EmergentGold_SafeHaven_FearAndNegativeRealRatesLiftPrice()
    {
        decimal Run(decimal confidence, decimal inflation, decimal rate)
        {
            var e = new EconomicEngine(42) { EmergentCommodityPricing = true, CountryInstabilityChance = 0 };
            e.Data.GoldPrice = 1900m;
            for (int i = 0; i < 90; i++)
            {
                e.Data.ConsumerConfidence = confidence; // gold is driven by fear + real rates, not activity
                e.Data.InflationRate = inflation;
                e.Data.InterestRate = rate;
                e.TickDay(Start.AddDays(i));
            }
            return e.Data.GoldPrice;
        }
        var riskOff = Run(65m, 6m, 1m);   // fearful + high inflation + low nominal → deeply negative real rates
        var calm = Run(110m, 1m, 5m);     // confident + low inflation + high rates
        Assert.True(riskOff > calm + 20m, $"gold should be a safe haven (riskOff {riskOff:F0} vs calm {calm:F0})");
    }

    [Fact]
    public void EmergentGold_YearPlaytest_StaysInRealisticBand_NoClampPinning()
    {
        foreach (var seed in new[] { 1, 7, 42, 123, 999 })
        {
            var e = new EconomicEngine(seed) { EmergentCommodityPricing = true, CountryInstabilityChance = 0 };
            var prices = new List<decimal>();
            for (int i = 0; i < 252; i++)
            {
                e.TickDay(Start.AddDays(i));
                var p = e.Data.GoldPrice;
                Assert.True(p > 800m && p < 3000m, $"seed {seed} day {i}: gold pinned at a clamp ({p:F0})");
                prices.Add(p);
            }
            prices.Sort();
            Assert.InRange(prices[prices.Count / 2], 1400m, 2500m); // realistic band around ~$1900
        }
    }
}
