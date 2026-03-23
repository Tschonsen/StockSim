using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// Economic cycle system. Bible 5.9.
/// Four phases: Expansion → Peak → Contraction → Recovery → Expansion...
/// Each phase applies sector-specific drift adjustments.
/// </summary>
public class EconomicCycleEngine
{
    private readonly Random _rng;
    private readonly Logger _log = new("EconomicCycle");
    private int _daysInCurrentPhase;
    private int _phaseDuration;

    public EconomicPhase Phase { get; private set; }

    // Sector rotation per phase (Bible 5.9)
    private static readonly Dictionary<EconomicPhase, Dictionary<string, float>> SectorMultipliers = new()
    {
        [EconomicPhase.Expansion] = new()
        {
            ["Technology"] = 1.5f, ["Consumer Goods"] = 1.3f, ["Financials"] = 1.4f,
            ["Industrials"] = 1.2f, ["Energy"] = 1.0f, ["Healthcare"] = 0.8f,
            ["Utilities"] = 0.7f, ["Materials"] = 1.1f, ["Real Estate"] = 1.2f,
            ["Telecommunications"] = 1.0f, ["Luxury Goods"] = 1.4f, ["Transportation"] = 1.2f,
        },
        [EconomicPhase.Peak] = new()
        {
            ["Technology"] = 0.8f, ["Consumer Goods"] = 0.9f, ["Financials"] = 0.7f,
            ["Industrials"] = 1.1f, ["Energy"] = 1.5f, ["Healthcare"] = 1.0f,
            ["Utilities"] = 1.0f, ["Materials"] = 1.3f, ["Real Estate"] = 0.6f,
            ["Telecommunications"] = 0.9f, ["Luxury Goods"] = 0.8f, ["Transportation"] = 1.0f,
        },
        [EconomicPhase.Contraction] = new()
        {
            ["Technology"] = 0.5f, ["Consumer Goods"] = 1.2f, ["Financials"] = 0.5f,
            ["Industrials"] = 0.6f, ["Energy"] = 0.7f, ["Healthcare"] = 1.3f,
            ["Utilities"] = 1.5f, ["Materials"] = 0.6f, ["Real Estate"] = 0.4f,
            ["Telecommunications"] = 1.0f, ["Luxury Goods"] = 0.4f, ["Transportation"] = 0.7f,
        },
        [EconomicPhase.Recovery] = new()
        {
            ["Technology"] = 1.1f, ["Consumer Goods"] = 1.0f, ["Financials"] = 1.5f,
            ["Industrials"] = 1.3f, ["Energy"] = 0.8f, ["Healthcare"] = 1.0f,
            ["Utilities"] = 0.8f, ["Materials"] = 1.1f, ["Real Estate"] = 1.4f,
            ["Telecommunications"] = 1.0f, ["Luxury Goods"] = 1.0f, ["Transportation"] = 1.2f,
        },
    };

    public EconomicCycleEngine(int seed)
    {
        _rng = new Random(seed);

        // Start phase based on seed (Bible 5.9)
        var roll = _rng.NextDouble();
        Phase = roll switch
        {
            < 0.40 => EconomicPhase.Expansion,
            < 0.65 => EconomicPhase.Recovery,
            < 0.85 => EconomicPhase.Peak,
            _ => EconomicPhase.Contraction,
        };

        _phaseDuration = GetPhaseDuration(Phase);
        _daysInCurrentPhase = _rng.Next(_phaseDuration / 3); // Start partway through

        _log.Info("Economic cycle initialized", new { phase = Phase.ToString(), daysIn = _daysInCurrentPhase, duration = _phaseDuration });
    }

    /// <summary>
    /// Called once per trading day (at market open).
    /// Advances the cycle and applies sector drift.
    /// </summary>
    public void TickDay(IReadOnlyList<Stock> stocks)
    {
        _daysInCurrentPhase++;

        // Check for phase transition
        if (_daysInCurrentPhase >= _phaseDuration)
        {
            var oldPhase = Phase;
            Phase = GetNextPhase(Phase);
            _phaseDuration = GetPhaseDuration(Phase);
            _daysInCurrentPhase = 0;

            _log.Info("Economic phase changed", new
            {
                from = oldPhase.ToString(),
                to = Phase.ToString(),
                newDuration = _phaseDuration,
            });
        }

        // Apply sector-specific drift based on current phase
        var multipliers = SectorMultipliers.GetValueOrDefault(Phase);
        if (multipliers == null) return;

        foreach (var stock in stocks)
        {
            if (multipliers.TryGetValue(stock.Sector, out var mult))
            {
                // Adjust fair value drift: strong sectors drift up, weak sectors drift down
                var driftAdjustment = (mult - 1.0f) * 0.0002f; // Small daily adjustment
                stock.FairValue = Math.Max(0.01m, Math.Round(stock.FairValue * (1m + (decimal)driftAdjustment), 2));
            }
        }
    }

    /// <summary>Get the sector multiplier for current phase.</summary>
    public float GetSectorMultiplier(string sector)
    {
        if (SectorMultipliers.TryGetValue(Phase, out var multipliers))
            if (multipliers.TryGetValue(sector, out var mult))
                return mult;
        return 1.0f;
    }

    private int GetPhaseDuration(EconomicPhase phase) => phase switch
    {
        EconomicPhase.Expansion => 90 + _rng.Next(210),    // 90-300 days
        EconomicPhase.Peak => 20 + _rng.Next(40),          // 20-60 days
        EconomicPhase.Contraction => 60 + _rng.Next(120),  // 60-180 days
        EconomicPhase.Recovery => 60 + _rng.Next(60),      // 60-120 days
        _ => 100,
    };

    private static EconomicPhase GetNextPhase(EconomicPhase current) => current switch
    {
        EconomicPhase.Expansion => EconomicPhase.Peak,
        EconomicPhase.Peak => EconomicPhase.Contraction,
        EconomicPhase.Contraction => EconomicPhase.Recovery,
        EconomicPhase.Recovery => EconomicPhase.Expansion,
        _ => EconomicPhase.Expansion,
    };
}

/// <summary>Economic cycle phases. Bible 5.9.</summary>
public enum EconomicPhase
{
    Expansion,
    Peak,
    Contraction,
    Recovery,
}
