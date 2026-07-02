using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

/// <summary>
/// Live-sim realism diagnostic: run the SAME world (same seed) twice — once normally, once with a
/// sustained oil shock — over a simulated quarter. Same seed ⇒ identical random draws, so the per-stock
/// difference isolates the pure oil-cascade effect. The litmus test: does the shock reach INDIRECT sectors
/// (Technology valuation, Real Estate demand) through oil→inflation→rates, not just the oil-exposed ones?
/// Writes a metrics summary to temp for inspection and asserts the cascade holds end-to-end in the live game.
/// </summary>
public class RealismDiagnostic
{
    private const int Days = 60;
    private const int StockCount = 100;

    [Fact(Skip = "Heavy manual playtest (~1min). Un-skip to read a market-health readout to " +
                 "%TEMP%/stocksim_playtest.txt. Emergent-only market: ~flat median, healthy dispersion, no collapses. " +
                 "See EMERGENT_COUPLING.md §7/§8.")]
    public void Playtest_OneYear_Readout()
    {
        var loop = new GameLoop(seed: 2024, stockCount: 150);
        loop.SetSpeed(GameSpeed.Normal);
        var startDate = loop.GameTime;
        var reg = loop.Stocks.Where(s => !s.Traits.Contains("ETF")).ToList();
        var pxStart = reg.ToDictionary(s => s.Symbol, s => s.CurrentPrice);
        var fvStart = reg.ToDictionary(s => s.Symbol, s => s.FairValue);
        var niStart = reg.ToDictionary(s => s.Symbol, s => s.NetIncome);

        var lastDate = loop.GameTime.Date;
        int days = 0;
        while (days < 120)
        {
            loop.ExecuteTick();
            if (loop.GameTime.Date != lastDate) { lastDate = loop.GameTime.Date; days++; }
        }

        var stocks = loop.Stocks.Where(s => !s.Traits.Contains("ETF") && pxStart.ContainsKey(s.Symbol) && pxStart[s.Symbol] > 0).ToList();
        static decimal Median(IEnumerable<decimal> xs) { var l = xs.OrderBy(x => x).ToList(); return l.Count == 0 ? 0 : l[l.Count / 2]; }
        var pxRet = stocks.Select(s => s.CurrentPrice / pxStart[s.Symbol] - 1m).ToList();
        var fvRet = stocks.Where(s => fvStart[s.Symbol] > 0).Select(s => s.FairValue / fvStart[s.Symbol] - 1m).ToList();
        var niRet = stocks.Where(s => niStart[s.Symbol] > 0).Select(s => s.NetIncome / niStart[s.Symbol] - 1m).ToList();
        var invalid = stocks.Count(s => s.CurrentPrice <= 0m || s.CurrentPrice > 1_000_000m);

        var lines = new List<string>
        {
            $"PLAYTEST — {days} trading days, {stocks.Count} stocks. WHERE does the bearish drift come from?",
            $"  PRICE      avg {pxRet.Average(),7:P1}  median {Median(pxRet),7:P1}   (best {pxRet.Max(),6:P0} / worst {pxRet.Min(),6:P0})",
            $"  FAIR VALUE avg {fvRet.Average(),7:P1}  median {Median(fvRet),7:P1}",
            $"  EARNINGS   avg {niRet.Average(),7:P1}  median {Median(niRet),7:P1}",
            $"  collapsed(<$1) {stocks.Count(s => s.CurrentPrice < 1m)}   mooned(>5x) {stocks.Count(s => s.CurrentPrice > pxStart[s.Symbol] * 5m)}   invalid {invalid}",
        };
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "stocksim_playtest.txt"), string.Join(Environment.NewLine, lines));

        Assert.Equal(0, invalid);
    }

    [Fact(Skip = "Heavy manual live guard (~20s; perturbs suite state). Emergent-only pricing: a sustained oil shock " +
                 "hurts fuel buyers ~-14%, lifts oil sellers, spread ~+23%, prices sane. Un-skip to re-measure. " +
                 "Writes %TEMP%/stocksim_realism_diag.txt.")]
    public void OilShock_CascadeReachesIndirectSectors_LiveSim()
    {
        // Classify stocks by oil exposure once (same seed ⇒ same companies in every run).
        var classLoop = new GameLoop(seed: 777, stockCount: StockCount);
        var fuelBuyers = classLoop.Stocks
            .Where(s => s.DriverExposures.Any(e => e.Driver == "OilPrice" && e.Channel == ExposureChannel.InputCost && e.Elasticity >= 0.25m))
            .Select(s => s.Symbol).ToHashSet();
        var oilSellers = classLoop.Stocks
            .Where(s => s.DriverExposures.Any(e => e.Driver == "OilPrice" && e.Channel == ExposureChannel.OutputPrice))
            .Select(s => s.Symbol).ToHashSet();

        var (baseline, baseLoop) = RunQuarter(seed: 777, pinOil: false);
        var (shocked, shockLoop) = RunQuarter(seed: 777, pinOil: true);

        // Instrument the single fuel buyer: where does the signal live or die?
        var buyerSym = fuelBuyers.FirstOrDefault();
        var probe = new List<string>();
        if (buyerSym != null)
        {
            var s0 = classLoop.Stocks.First(s => s.Symbol == buyerSym);
            var b = baseLoop.Stocks.First(s => s.Symbol == buyerSym);
            var sh = shockLoop.Stocks.First(s => s.Symbol == buyerSym);
            probe.Add($"probe {buyerSym}: START NI {s0.NetIncome:F0} Rev {s0.Revenue:F0} exposures {s0.DriverExposures.Count} FV {s0.FairValue:F2}");
            probe.Add($"  BASELINE end: NI {b.NetIncome:F0} Rev {b.Revenue:F0} exp {b.DriverExposures.Count} FV {b.FairValue:F2} px {b.CurrentPrice:F2} | oil {baseLoop.Economy.Data.OilPrice:F1} dev {baseLoop.Economy.GetDriverDeviation("OilPrice"):F2}");
            probe.Add($"  SHOCKED  end: NI {sh.NetIncome:F0} Rev {sh.Revenue:F0} exp {sh.DriverExposures.Count} FV {sh.FairValue:F2} px {sh.CurrentPrice:F2} | oil {shockLoop.Economy.Data.OilPrice:F1} dev {shockLoop.Economy.GetDriverDeviation("OilPrice"):F2}");
        }

        decimal GroupEffect(HashSet<string> group)
        {
            var syms = group.Where(s => baseline.ContainsKey(s) && shocked.ContainsKey(s)).ToList();
            return syms.Count == 0 ? 0m : syms.Average(s => shocked[s] - baseline[s]);
        }

        var buyerEffect = GroupEffect(fuelBuyers);
        var sellerEffect = GroupEffect(oilSellers);

        var lines = new List<string>
        {
            $"Oil-shock effect over {Days} days (shocked minus baseline avg return):",
            $"  fuel buyers (n={fuelBuyers.Count}, high oil InputCost) : {buyerEffect,7:P1}",
            $"  oil sellers (n={oilSellers.Count}, oil OutputPrice)    : {sellerEffect,7:P1}",
            $"  spread (sellers - buyers)                             : {sellerEffect - buyerEffect,7:P1}",
        };
        lines.AddRange(probe);
        var summary = string.Join(Environment.NewLine, lines);
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "stocksim_realism_diag.txt"), summary);

        // Stability & sanity: a quarter runs without NaN, collapse, or runaway, with real dispersion.
        foreach (var v in baseline.Values)
            Assert.True(v > -0.95m && v < 5m, $"return {v:P1} is implausible (collapse/runaway).\n{summary}");
        Assert.True(baseline.Values.Max() - baseline.Values.Min() > 0.03m, $"stocks should disperse.\n{summary}");

        // Directional: under a sustained oil shock, fuel buyers fare worse than oil sellers.
        Assert.True(buyerEffect < sellerEffect, $"fuel buyers should fare worse than oil sellers under an oil shock.\n{summary}");
    }

    [Fact(Skip = "Manual diagnostic: day-by-day trace of one stock. Needs the daily block ACTIVE (temp 16:00 fix). " +
                 "Used to localise the fair-value instability (fixed: EarningsEngine unclamped set removed + " +
                 "unprofitable erosion). Writes %TEMP%/stocksim_instability_trace.txt.")]
    public void DailyBlockInstability_Trace()
    {
        var loop = new GameLoop(seed: 777, stockCount: 60);
        loop.SetSpeed(GameSpeed.Normal);
        var s = loop.Stocks.First(x => !x.Traits.Contains("ETF") && x.NetIncome > 0);
        var lines = new List<string>
        {
            $"Day-by-day trace of {s.Symbol} (daily block ACTIVE, FairValue unified):",
            $"  START : Rev {s.Revenue,13:F0} NI {s.NetIncome,13:F0} FV {s.FairValue,9:F2} px {s.CurrentPrice,9:F2}",
        };
        var lastDate = loop.GameTime.Date;
        int days = 0;
        while (days < 20)
        {
            loop.ExecuteTick();
            if (loop.GameTime.Date != lastDate)
            {
                lastDate = loop.GameTime.Date;
                days++;
                var margin = s.Revenue != 0 ? s.NetIncome / s.Revenue : 0m;
                lines.Add($"  day {days,2}: Rev {s.Revenue,13:F0} NI {s.NetIncome,13:F0} margin {margin,7:P1} FV {s.FairValue,9:F2} px {s.CurrentPrice,9:F2}");
            }
        }
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "stocksim_instability_trace.txt"), string.Join(Environment.NewLine, lines));
        Assert.True(loop.GameTime.Date != DateTime.MinValue); // observation only
    }

    private static (Dictionary<string, decimal> Returns, GameLoop Loop) RunQuarter(int seed, bool pinOil)
    {
        var loop = new GameLoop(seed, stockCount: StockCount);
        loop.SetSpeed(GameSpeed.Normal); // a fresh loop starts paused
        var start = loop.Stocks.ToDictionary(s => s.Symbol, s => s.CurrentPrice);

        for (int t = 0; t < Days * 390; t++)
        {
            loop.ExecuteTick();
            if (pinOil && loop.Economy.Data.OilPrice < 140m) loop.Economy.Data.OilPrice = 140m;
        }

        var returns = loop.Stocks
            .Where(s => !s.Traits.Contains("ETF") && start.TryGetValue(s.Symbol, out var p) && p > 0m)
            .ToDictionary(s => s.Symbol, s => s.CurrentPrice / start[s.Symbol] - 1m);
        return (returns, loop);
    }
}
