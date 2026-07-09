using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

/// <summary>
/// Reference-checked calibration of the emergent commodities (WORLD_SIM_VISION.md §1a: realism must be
/// checkable against a reference, not gut feeling). Runs multi-seed multi-year playtests, measures the
/// statistical signature (annualised volatility, price band, driver correlations) and compares to
/// real-world targets. Writes a human-readable report AND asserts realistic bands so it doubles as a guard.
///
/// Real-world references (annualised vol / behaviour):
///   Oil  — WTI ~30-50% vol, procyclical (positive corr with growth).
///   Gas  — Henry Hub ~50-100% vol (storage/seasonal — most volatile).
///   Gold — ~13-18% vol, safe-haven (negative corr with confidence).
/// </summary>
public class CommodityCalibrationDiagnostic
{
    private static readonly DateTime Start = new(2026, 1, 1);
    private const int Years = 3;
    private const int Days = Years * 252;
    private static readonly int[] Seeds = { 1, 7, 42, 123, 999 };

    [Fact]
    public void EmergentCommodities_MatchRealWorldStatistics()
    {
        var oilVol = new List<double>();
        var gasVol = new List<double>();
        var goldVol = new List<double>();
        var oilGdpCorr = new List<double>();
        var goldConfCorr = new List<double>();
        var oilMedian = new List<decimal>();
        var gasMedian = new List<decimal>();
        var goldMedian = new List<decimal>();

        foreach (var seed in Seeds)
        {
            // Baseline market signature (no geopolitical tail shocks) — isolate the "normal" dynamics.
            var e = new EconomicEngine(seed) { EmergentCommodityPricing = true, CountryInstabilityChance = 0 };
            var oil = new List<decimal>();
            var gas = new List<decimal>();
            var gold = new List<decimal>();
            var gdp = new List<decimal>();
            var conf = new List<decimal>();
            for (int i = 0; i < Days; i++)
            {
                e.TickDay(Start.AddDays(i));
                oil.Add(e.Data.OilPrice);
                gas.Add(e.Data.NatGasPrice);
                gold.Add(e.Data.GoldPrice);
                gdp.Add(e.Data.GDPGrowth);
                conf.Add(e.Data.ConsumerConfidence);
            }
            oilVol.Add(AnnualisedVol(oil));
            gasVol.Add(AnnualisedVol(gas));
            goldVol.Add(AnnualisedVol(gold));
            // Vol is a daily property; the macro relationship is measured on ~monthly-smoothed series
            // (real commodity↔macro correlation is noise-dominated daily, clear only at lower frequency).
            oilGdpCorr.Add(Pearson(Smooth(oil, 21), Smooth(gdp, 21)));
            goldConfCorr.Add(Pearson(Smooth(gold, 21), Smooth(conf, 21)));
            oilMedian.Add(Median(oil));
            gasMedian.Add(Median(gas));
            goldMedian.Add(Median(gold));
        }

        double OilV = oilVol.Average(), GasV = gasVol.Average(), GoldV = goldVol.Average();
        double OilC = oilGdpCorr.Average(), GoldC = goldConfCorr.Average();

        var report =
            "=== Emergent Commodity Calibration (5 seeds x 3y, no geopolitics) ===\n" +
            $"Oil   vol {OilV,6:P1}  median ${oilMedian.Average(),7:F2}   corr(oil, GDP)      {OilC,6:F2}  [real: ~30-50% vol, procyclical +]\n" +
            $"Gas   vol {GasV,6:P1}  median ${gasMedian.Average(),7:F2}   (storage/seasonal)          [real: ~50-100% vol, most volatile]\n" +
            $"Gold  vol {GoldV,6:P1}  median ${goldMedian.Average(),7:F2}   corr(gold, confidence) {GoldC,6:F2}  [real: ~13-18% vol, safe-haven -]\n" +
            $"Ordering check: gold {GoldV:P1} < oil {OilV:P1} < gas {GasV:P1}  (expected gold < oil < gas)\n";
        try { File.WriteAllText(Path.Combine(Path.GetTempPath(), "stocksim-calibration.txt"), report); }
        catch { /* report file is a convenience; the assertions below are the real guard */ }

        // --- Assertions against realistic bands (the guard) ---
        Assert.InRange(OilV, 0.12, 0.60);   // oil realistically volatile
        Assert.InRange(GasV, 0.20, 1.10);   // gas the most volatile
        Assert.InRange(GoldV, 0.05, 0.30);  // gold the calmest
        Assert.True(GoldV < OilV, $"gold ({GoldV:P1}) should be calmer than oil ({OilV:P1})");
        Assert.True(OilV < GasV, $"oil ({OilV:P1}) should be calmer than gas ({GasV:P1})");
        Assert.True(OilC > 0.10, $"oil should be procyclical (corr with GDP {OilC:F2})");
        Assert.True(GoldC < -0.10, $"gold should be a safe haven (corr with confidence {GoldC:F2})");
    }

    private static double AnnualisedVol(List<decimal> prices)
    {
        var rets = new List<double>();
        for (int i = 1; i < prices.Count; i++)
        {
            var prev = (double)prices[i - 1];
            if (prev != 0) rets.Add(((double)prices[i] - prev) / prev);
        }
        var mean = rets.Average();
        var variance = rets.Sum(r => (r - mean) * (r - mean)) / rets.Count;
        return Math.Sqrt(variance) * Math.Sqrt(252);
    }

    private static double Pearson(List<decimal> a, List<decimal> b)
    {
        var xs = a.Select(v => (double)v).ToArray();
        var ys = b.Select(v => (double)v).ToArray();
        double mx = xs.Average(), my = ys.Average();
        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < xs.Length; i++)
        {
            var dx = xs[i] - mx; var dy = ys[i] - my;
            sxy += dx * dy; sxx += dx * dx; syy += dy * dy;
        }
        return (sxx == 0 || syy == 0) ? 0 : sxy / Math.Sqrt(sxx * syy);
    }

    /// <summary>Centred-ish moving average (window days) — reveals the low-frequency relationship under noise.</summary>
    private static List<decimal> Smooth(List<decimal> xs, int window)
    {
        var outp = new List<decimal>();
        for (int i = window - 1; i < xs.Count; i++)
        {
            decimal s = 0;
            for (int j = i - window + 1; j <= i; j++) s += xs[j];
            outp.Add(s / window);
        }
        return outp;
    }

    private static decimal Median(List<decimal> xs)
    {
        var s = xs.OrderBy(v => v).ToList();
        return s[s.Count / 2];
    }
}
