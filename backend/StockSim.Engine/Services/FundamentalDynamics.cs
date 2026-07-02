using System.Globalization;

namespace StockSim.Engine.Services;

/// <summary>
/// Pure helpers that couple a company's fundamental trajectory to its daily drift.
///
/// Point 1b: once an event has shifted a stock's <c>RevenueGrowth</c> (its trajectory),
/// that trajectory must keep nudging the daily fundamental drift — otherwise the event's
/// mark evaporates after one tick. The trajectory itself mean-reverts toward a baseline so
/// a single beat/miss doesn't compound forever.
/// </summary>
public static class FundamentalDynamics
{
    /// <summary>Share of a stock's RevenueGrowth that biases its daily drift.</summary>
    private const decimal GrowthDriftWeight = 0.02m;

    /// <summary>Daily mean-reversion speed of RevenueGrowth toward the baseline.</summary>
    private const decimal GrowthReversionRate = 0.02m;

    /// <summary>
    /// Given a stock's current growth trajectory and a baseline it reverts toward,
    /// return the bias to add to today's drift and the (slightly mean-reverted) growth
    /// to store back on the stock.
    /// </summary>
    public static (decimal DriftBias, decimal NewGrowth) GrowthTrajectory(decimal revenueGrowth, decimal baseline)
    {
        var driftBias = revenueGrowth * GrowthDriftWeight;
        var newGrowth = revenueGrowth + (baseline - revenueGrowth) * GrowthReversionRate;
        return (driftBias, newGrowth);
    }

    // === Emergent driver coupling (design/EMERGENT_COUPLING.md) =========================
    // All four channels are LEVEL-based: they react to how far a driver sits from its baseline
    // (deviation), NOT to the day-over-day change. This is what makes a *sustained* high oil price a
    // *sustained* margin hit — a change-based version fires only on the day oil moves and misses a
    // constant level entirely. InputCost/OutputPrice move the margin (applied per day from a stable
    // base, not accumulated); Demand moves the growth baseline; Valuation moves the PE multiple.

    /// <summary>Share of an input-cost move offset by price resets and hedging, so only part of an
    /// elevated level lands on the margin.</summary>
    private const decimal InputCostPassThrough = 0.5m;

    /// <summary>Largest margin swing an input-cost or output-price coupling may impose — hedges and
    /// contracts cap the effect even for an outsized driver level.</summary>
    private const decimal MaxCouplingSwing = 0.25m;

    /// <summary>Share of an output-price level a producer actually realises (contracts/hedges dampen).</summary>
    private const decimal OutputPriceRealization = 0.6m;

    /// <summary>
    /// <b>InputCost channel (level-based).</b> How far a driver that is one of a company's input costs
    /// sits above/below normal moves its operating margin. Returns a margin level (fraction of revenue);
    /// driver above baseline → margin down. Applied per day from a stable base margin (see GameLoop) so a
    /// sustained level is a sustained hit, not an accumulating one. E.g. oil → airline fuel.
    ///
    /// <paramref name="driverDeviation"/> is the driver's normalised deviation from baseline (see
    /// EconomicEngine.GetDriverDeviation). <paramref name="exposure"/> is the input's share of revenue
    /// (airline fuel ~0.15–0.35). Pass-through cushions it; clamped to a plausible swing.
    /// </summary>
    public static decimal InputCostMarginLevel(decimal driverDeviation, decimal exposure)
    {
        if (exposure <= 0m) return 0m;
        var rawHit = -exposure * driverDeviation;             // oil 1 scale-unit high, 0.30 exposure → -0.30 raw
        var cushioned = rawHit * (1m - InputCostPassThrough); // resets + hedges offset ~half
        return Math.Clamp(cushioned, -MaxCouplingSwing, MaxCouplingSwing);
    }

    /// <summary>
    /// <b>OutputPrice channel (level-based).</b> How far a driver a producer <i>sells</i> sits above/below
    /// normal moves its margin — opposite sign to <see cref="InputCostMarginLevel"/> for the same driver
    /// (oil hurts airlines, helps oil producers): higher sell price on ~fixed costs widens the margin.
    /// Returns a margin level; clamped. Applied per day from a stable base margin, like InputCost.
    /// </summary>
    public static decimal OutputPriceMarginLevel(decimal driverDeviation, decimal exposure)
    {
        if (exposure <= 0m) return 0m;
        var realized = exposure * driverDeviation * OutputPriceRealization;
        return Math.Clamp(realized, -MaxCouplingSwing, MaxCouplingSwing);
    }

    // The Demand and Valuation channels are LEVEL-based: they respond to how far a driver sits from its
    // baseline (deviation), not to the day-over-day change. Their elasticity is SIGNED (it encodes the
    // driver's direction of effect — e.g. rates lower housing demand, so a negative elasticity).

    /// <summary>Largest sustained growth shift the Demand channel may set as a trajectory baseline.</summary>
    private const decimal MaxDemandGrowthBaseline = 0.15m;

    /// <summary>
    /// <b>Demand channel.</b> How far off-baseline a demand driver sits shifts the growth rate a company
    /// sustains — the baseline its <see cref="GrowthTrajectory"/> mean-reverts toward (so it persists while
    /// the driver stays off-normal, then fades as it normalises). E.g. high consumer confidence lifts a
    /// retailer's sustainable growth; high rates cut a homebuilder's. Signed elasticity; clamped.
    /// </summary>
    public static decimal DemandGrowthBaseline(decimal driverDeviation, decimal signedElasticity)
        => Math.Clamp(driverDeviation * signedElasticity, -MaxDemandGrowthBaseline, MaxDemandGrowthBaseline);

    /// <summary>Largest fraction the Valuation channel may move a PE multiple in one direction.</summary>
    private const decimal MaxValuationSwing = 0.30m;

    /// <summary>
    /// <b>Valuation channel.</b> How far off-baseline a driver sits moves the valuation MULTIPLE (PE), not
    /// earnings — e.g. higher rates compress the multiple investors pay for growth. Returns a fractional
    /// adjustment to the PE (−0.20 = PE 20% lower). Signed elasticity; clamped. Applied in fair-value recalc.
    /// </summary>
    public static decimal ValuationMultipleFactor(decimal driverDeviation, decimal signedElasticity)
        => Math.Clamp(driverDeviation * signedElasticity, -MaxValuationSwing, MaxValuationSwing);

    /// <summary>
    /// Point 2: a single [-1, +1] health score from a company's fundamentals.
    /// +1 = thriving (profitable, growing, low debt, top-rated, rising), -1 = distressed.
    /// Five equally-weighted factors: profitability, growth, leverage, credit, momentum.
    /// </summary>
    public static decimal CompanyHealthScore(
        decimal revenueGrowth, decimal netIncome, decimal revenue,
        decimal debtToEquity, string creditRating, decimal recentReturn)
    {
        var margin = revenue > 0 ? netIncome / revenue : 0m;
        var profitability = Math.Clamp(margin * 4m, -1m, 1m);              // 25% margin → +1
        var growth = Math.Clamp(revenueGrowth * 4m, -1m, 1m);             // 25% growth → +1
        var leverage = Math.Clamp((1.5m - debtToEquity) / 1.5m, -1m, 1m); // D/E 0 → +1, D/E 3 → -1
        var credit = creditRating switch
        {
            "AAA" or "AA" => 1m,
            "A" => 0.5m,
            "BBB" => 0m,
            "BB" => -0.5m,
            "B" => -1m,
            _ => 0m,
        };
        var momentum = Math.Clamp(recentReturn * 5m, -1m, 1m);            // ±20% → ±1

        var score = (profitability + growth + leverage + credit + momentum) / 5m;
        return Math.Clamp(score, -1m, 1m);
    }

    /// <summary>
    /// Point 2: turn a health score into a news-selection weight. Neutral companies are the
    /// baseline (1.0); distress is the most newsworthy, strong results also draw coverage.
    /// </summary>
    public static double NewsWeight(decimal healthScore)
    {
        var h = (double)healthScore;
        return 1.0 + 1.5 * Math.Max(0.0, -h) + 0.8 * Math.Max(0.0, h);
    }

    /// <summary>Age (years) at which a company reaches its stable, mature profile.</summary>
    private const double MaturityReferenceAge = 60.0;

    /// <summary>
    /// Point 3 (FoundedYear): a company's age shapes its drift and volatility. Young firms
    /// compound faster and swing harder (multipliers above 1); they decay monotonically toward
    /// a stable floor (below 1) by the reference age. Age is clamped at both ends.
    /// </summary>
    public static (decimal DriftMultiplier, double VolMultiplier) MaturityModifiers(int ageYears)
    {
        var t = Math.Clamp(Math.Max(0, ageYears) / MaturityReferenceAge, 0.0, 1.0);
        var driftMult = (decimal)(1.4 - 0.55 * t); // 1.4 (newborn) → 0.85 (mature)
        var volMult = 1.3 - 0.45 * t;              // 1.3 (newborn) → 0.85 (mature)
        return (driftMult, volMult);
    }

    /// <summary>
    /// Point 5: a concrete, metric-grounded lead clause for an analyst quote. Picks the most
    /// relevant number (revenue growth, margin, or P/E) for the event direction and type, so the
    /// commentary is about THIS company instead of generic directional chatter.
    /// </summary>
    public static string AnalystMetricClause(
        string name, bool positive, decimal peRatio, decimal revenueGrowthPct, decimal marginPct, bool isEarnings)
    {
        static string N(decimal v) => v.ToString("F0", CultureInfo.InvariantCulture);

        if (positive)
        {
            if (isEarnings && revenueGrowthPct > 3m)
                return $"With revenue up {N(revenueGrowthPct)}%, {name}'s growth trajectory supports our bullish stance.";
            if (marginPct >= 12m)
                return $"At a {N(marginPct)}% net margin, {name} screens as best-in-class — we stay overweight.";
            if (peRatio > 0m && peRatio < 22m)
                return $"At {N(peRatio)}x earnings, {name} still looks attractively valued after this move.";
            return $"This is a clear positive for {name}; we're raising our estimates.";
        }

        if (isEarnings)
            return $"With revenue growth at {N(revenueGrowthPct)}%, {name}'s deceleration is a real concern.";
        if (peRatio >= 30m)
            return $"At {N(peRatio)}x earnings, {name} offers no margin of safety as the story deteriorates.";
        if (marginPct < 6m)
            return $"With net margins already thin at {N(marginPct)}%, {name} has little cushion to absorb this.";
        return $"This raises real questions for {name}; we're trimming our exposure.";
    }

    /// <summary>
    /// Point 4: a short clause describing the company's current trajectory, for weaving into
    /// news text. Distress dominates (it's the most newsworthy); returns empty when nothing is
    /// notable so generic events don't get a forced clause.
    /// </summary>
    public static string TrajectoryPhrase(
        decimal revenueGrowth, int performanceStreak, int consecutiveMisses, decimal recentReturn)
    {
        // Negative signals first.
        if (consecutiveMisses >= 2) return "extending a run of earnings disappointments";
        if (performanceStreak <= -45) return "mired in a prolonged rough stretch";
        if (revenueGrowth < -0.10m || recentReturn < -0.15m) return "struggling to halt a deepening slide";

        // Positive signals.
        if (performanceStreak >= 45) return "riding a sustained strong run";
        if (revenueGrowth > 0.10m || recentReturn > 0.15m) return "building on strong momentum";

        return string.Empty;
    }

    /// <summary>CEO archetypes ordered defensive → growth. Persona evolution steps along this.
    /// Turnaround Artist is intentionally absent — that transition is handled by CEO firing.</summary>
    private static readonly string[] ArchetypeSpectrum =
    {
        "Steady Hand", "Finance Veteran", "Cost-Cutter", "Industry Insider", "Engineer-CEO",
        "Dealmaker", "Sales Machine", "Empire Builder", "Founder-CEO", "Visionary", "Disruptor",
    };

    /// <summary>Sustained days of strength/weakness before a company's persona evolves.</summary>
    private const int PersonaEvolutionThreshold = 90;

    /// <summary>
    /// Point 3b: update a company's performance streak. Strong health (&gt;0.5) builds it up,
    /// weak health (&lt;-0.5) tears it down, and a neutral day decays it back toward zero.
    /// </summary>
    public static int UpdateStreak(int streak, decimal healthScore)
    {
        if (healthScore > 0.5m) return streak + 1;
        if (healthScore < -0.5m) return streak - 1;
        return streak > 0 ? streak - 1 : streak < 0 ? streak + 1 : 0;
    }

    /// <summary>+1 to evolve toward growth, -1 toward defensive, 0 if the streak is not yet sustained.</summary>
    public static int PersonaEvolutionDirection(int streak)
    {
        if (streak >= PersonaEvolutionThreshold) return 1;
        if (streak <= -PersonaEvolutionThreshold) return -1;
        return 0;
    }

    /// <summary>
    /// Move an archetype one step along the defensive↔growth spectrum. Clamped at both ends;
    /// archetypes not on the spectrum (e.g. Turnaround Artist) are returned unchanged.
    /// </summary>
    public static string EvolveArchetype(string current, int direction)
    {
        var idx = Array.IndexOf(ArchetypeSpectrum, current);
        if (idx < 0) return current;
        var newIdx = Math.Clamp(idx + Math.Sign(direction), 0, ArchetypeSpectrum.Length - 1);
        return ArchetypeSpectrum[newIdx];
    }

    /// <summary>
    /// Deterministic weighted sampling: given non-negative weights and a roll in [0, 1),
    /// return the chosen index. Falls back to the first index if all weights are zero.
    /// </summary>
    public static int WeightedPick(IReadOnlyList<double> weights, double roll01)
    {
        if (weights.Count == 0) return 0;

        double total = 0;
        foreach (var w in weights) total += Math.Max(0.0, w);
        if (total <= 0) return 0;

        var threshold = Math.Clamp(roll01, 0.0, 0.999999) * total;
        double cumulative = 0;
        for (int i = 0; i < weights.Count; i++)
        {
            cumulative += Math.Max(0.0, weights[i]);
            if (threshold < cumulative) return i;
        }
        return weights.Count - 1;
    }
}
