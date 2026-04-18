using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// Generates market rumors — vague hints about upcoming company events.
/// Spec 4.8: every 20-40 game days, player gets a tip.
/// 80% of rumors are true (event fires 1-5 days later), 20% are false.
/// Rumors appear as special news with 💬 icon and "Rumor" badge.
/// </summary>
public class RumorEngine
{
    private readonly Random _rng;
    private readonly Logger _log = new("RumorEngine");

    /// <summary>All active (not yet resolved) rumors.</summary>
    private readonly List<Rumor> _activeRumors = new();

    /// <summary>Full rumor history for the session.</summary>
    private readonly List<Rumor> _rumorHistory = new();

    /// <summary>Rumors generated this tick (for sending to frontend).</summary>
    public List<Rumor> NewRumorsThisTick { get; } = new();

    /// <summary>Events generated from true rumors this tick.</summary>
    public List<GameEvent> RumorEventsThisTick { get; } = new();

    /// <summary>All active rumors (for SMA cross-referencing).</summary>
    public IReadOnlyList<Rumor> ActiveRumors => _activeRumors.AsReadOnly();

    /// <summary>Full rumor history (for save/load).</summary>
    public IReadOnlyList<Rumor> RumorHistory => _rumorHistory.AsReadOnly();

    /// <summary>Game day when the last rumor was generated.</summary>
    public int LastRumorDay { get; set; }

    /// <summary>Days until next rumor (randomized 20-40).</summary>
    public int NextRumorInDays { get; set; }

    /// <summary>Multiplier for rumor frequency. "The Insider" scenario sets this to 3.0.</summary>
    public double FrequencyMultiplier { get; set; } = 1.0;

    // Rumor frequency: every 5-12 game days (increased from 20-40 for more engagement)
    private const int MinDaysBetween = 5;
    private const int MaxDaysBetween = 12;

    // Spec 4.8: 80% true, 20% false
    private const double TrueRumorChance = 0.80;

    // Days before the event fires (1-5)
    private const int MinLeadDays = 1;
    private const int MaxLeadDays = 5;

    public RumorEngine(int seed)
    {
        _rng = new Random(seed);
        NextRumorInDays = _rng.Next(2, 6); // First rumor comes quickly (days 2-5)
    }

    /// <summary>
    /// Called at market close each day. May generate a new rumor and/or fire events from true rumors.
    /// </summary>
    public void TickDay(IReadOnlyList<Stock> stocks, DateTime gameTime)
    {
        NewRumorsThisTick.Clear();
        RumorEventsThisTick.Clear();

        var currentDay = (int)(gameTime - new DateTime(2027, 1, 1)).TotalDays;

        // Check if any active true rumors should fire their event today
        FirePendingRumorEvents(stocks, gameTime);

        // Check if it's time for a new rumor
        var daysSinceLast = currentDay - LastRumorDay;
        var adjustedInterval = (int)(NextRumorInDays / Math.Max(0.1, FrequencyMultiplier));

        if (daysSinceLast >= adjustedInterval)
        {
            GenerateRumor(stocks, gameTime);
            LastRumorDay = currentDay;
            NextRumorInDays = _rng.Next(MinDaysBetween, MaxDaysBetween + 1);
        }

        // Supply chain whispers: if a stock's supplier/customer has upcoming earnings
        if (_rng.NextDouble() < 0.15 * FrequencyMultiplier) // 15% daily chance
            GenerateSupplyChainWhisper(stocks, gameTime);

        // Clean up old resolved rumors (keep last 30 days)
        _activeRumors.RemoveAll(r => r.EventFired || (gameTime - r.EventExpectedAt).TotalDays > 5);
    }

    /// <summary>
    /// Whisper Network: generate supply chain rumors based on supplier/customer relationships.
    /// "Whispers from {supplier}'s supply chain suggest stronger/weaker quarter for {customer}"
    /// </summary>
    private void GenerateSupplyChainWhisper(IReadOnlyList<Stock> stocks, DateTime gameTime)
    {
        // Find stocks with supply chain relationships
        var candidates = stocks
            .Where(s => s.Personality?.Suppliers.Count > 0 && !s.Traits.Contains("ETF"))
            .ToList();
        if (candidates.Count == 0) return;

        var stock = candidates[_rng.Next(candidates.Count)];
        var supplierSym = stock.Personality!.Suppliers[_rng.Next(stock.Personality.Suppliers.Count)];
        var supplier = stocks.FirstOrDefault(s => s.Symbol == supplierSym);
        if (supplier == null) return;

        // Whisper based on supplier's recent performance
        var isPositive = supplier.PreviousClose > 0
            ? supplier.CurrentPrice > supplier.PreviousClose
            : _rng.NextDouble() < 0.5;

        var leadDays = _rng.Next(3, 8); // Longer lead than normal rumors
        var isTrue = _rng.NextDouble() < 0.7; // 70% accurate (less than regular 80%)

        var rumor = new Rumor
        {
            Symbol = stock.Symbol,
            CompanyName = stock.Name,
            Headline = isPositive
                ? $"\U0001F4AC Supply Chain Whisper: Sources close to {supplier.Name}'s operations hint at stronger-than-expected output — positive for customer {stock.Name}"
                : $"\U0001F4AC Supply Chain Whisper: Insiders report slowdowns at {supplier.Name}'s facilities — {stock.Name} may face supply constraints",
            CreatedAt = gameTime,
            EventExpectedAt = gameTime.AddDays(leadDays),
            IsTrue = isTrue,
            TemplateIndex = 1, // Reuse earnings template for event firing
            IsPositive = isPositive,
        };

        _activeRumors.Add(rumor);
        _rumorHistory.Add(rumor);
        NewRumorsThisTick.Add(rumor);

        _log.Info("Supply chain whisper generated", new { customer = stock.Symbol, supplier = supplierSym, isPositive, isTrue, leadDays });
    }

    /// <summary>
    /// Check if any pending true rumors should fire their associated event.
    /// </summary>
    private void FirePendingRumorEvents(IReadOnlyList<Stock> stocks, DateTime gameTime)
    {
        foreach (var rumor in _activeRumors.Where(r => r.IsTrue && !r.EventFired))
        {
            if (gameTime >= rumor.EventExpectedAt)
            {
                // Fire the real event
                var stock = stocks.FirstOrDefault(s => s.Symbol == rumor.Symbol);
                if (stock == null) continue;

                var evt = GenerateRumorEvent(rumor, stock, gameTime);
                RumorEventsThisTick.Add(evt);
                rumor.EventFired = true;

                _log.Info("Rumor event fired", new
                {
                    rumorId = rumor.Id,
                    symbol = rumor.Symbol,
                    headline = evt.Headline,
                    priceEffect = evt.PriceEffect,
                });
            }
        }

        // Mark false rumors as resolved when their expected date passes
        foreach (var rumor in _activeRumors.Where(r => !r.IsTrue && !r.EventFired))
        {
            if (gameTime >= rumor.EventExpectedAt)
            {
                rumor.EventFired = true; // No event fires, rumor was false
                _log.Info("False rumor expired", new { rumorId = rumor.Id, symbol = rumor.Symbol });
            }
        }
    }

    /// <summary>
    /// Generate a new rumor for a random stock.
    /// </summary>
    private void GenerateRumor(IReadOnlyList<Stock> stocks, DateTime gameTime)
    {
        if (stocks.Count == 0) return;

        // Pick a random non-ETF stock
        Stock? stock = null;
        for (var i = 0; i < 10; i++)
        {
            var candidate = stocks[_rng.Next(stocks.Count)];
            if (!candidate.Symbol.StartsWith("ETF_"))
            {
                stock = candidate;
                break;
            }
        }
        if (stock == null) return;

        var isTrue = _rng.NextDouble() < TrueRumorChance;
        var leadDays = _rng.Next(MinLeadDays, MaxLeadDays + 1);
        var templateIndex = _rng.Next(RumorTemplates.Length);
        var template = RumorTemplates[templateIndex];
        var isPositive = _rng.NextDouble() < 0.5;

        var rumor = new Rumor
        {
            Symbol = stock.Symbol,
            CompanyName = stock.Name,
            Headline = template.hint(stock, isPositive),
            CreatedAt = gameTime,
            EventExpectedAt = gameTime.AddDays(leadDays),
            IsTrue = isTrue,
            TemplateIndex = templateIndex,
            IsPositive = isPositive,
        };

        _activeRumors.Add(rumor);
        _rumorHistory.Add(rumor);
        NewRumorsThisTick.Add(rumor);

        _log.Info("Rumor generated", new
        {
            id = rumor.Id,
            symbol = stock.Symbol,
            isTrue,
            leadDays,
            headline = rumor.Headline,
        });
    }

    /// <summary>
    /// Generate the actual company event that matches a true rumor.
    /// </summary>
    private GameEvent GenerateRumorEvent(Rumor rumor, Stock stock, DateTime gameTime)
    {
        var template = RumorTemplates[rumor.TemplateIndex % RumorTemplates.Length];
        return template.eventFactory(stock, rumor.IsPositive, gameTime);
    }

    /// <summary>
    /// Check if a symbol has an active rumor (for SMA insider trading cross-reference).
    /// Returns the rumor if found, null otherwise.
    /// </summary>
    public Rumor? GetActiveRumorForSymbol(string symbol)
    {
        return _activeRumors.FirstOrDefault(r => r.Symbol == symbol && !r.EventFired);
    }

    /// <summary>
    /// Load state from save data.
    /// </summary>
    public void LoadState(List<Rumor> rumors, int lastRumorDay, int nextRumorInDays)
    {
        _activeRumors.Clear();
        _rumorHistory.Clear();

        foreach (var r in rumors)
        {
            _rumorHistory.Add(r);
            if (!r.EventFired)
                _activeRumors.Add(r);
        }

        LastRumorDay = lastRumorDay;
        NextRumorInDays = nextRumorInDays;

        if (rumors.Count > 0)
            Rumor.SetIdCounter(rumors.Max(r => r.Id) + 1);
    }

    // === RUMOR TEMPLATES ===
    // Each template has a vague hint (shown to player) and an event factory (fires if true).
    // Spec 4.8: hints are intentionally vague — "a major announcement" not "iPhone launch beats expectations".

    private static readonly (Func<Stock, bool, string> hint, Func<Stock, bool, DateTime, GameEvent> eventFactory)[] RumorTemplates =
    {
        // 0: Product announcement
        (
            (s, pos) => pos
                ? $"💬 Market Rumor: Sources suggest {s.Name} may be preparing a major product announcement."
                : $"💬 Market Rumor: Sources suggest {s.Name} may be facing issues with an upcoming product launch.",
            (s, pos, t) => new GameEvent
            {
                Type = EventType.Company, Severity = EventSeverity.Major,
                Sentiment = pos ? 0.6f : -0.6f,
                Headline = pos
                    ? $"{s.Name} ({s.Symbol}) unveils groundbreaking new product to strong market reception"
                    : $"{s.Name} ({s.Symbol}) delays key product launch, citing development setbacks",
                AffectedSymbols = new List<string> { s.Symbol },
                PriceEffect = pos ? 0.06f : -0.06f,
                VolatilityMultiplier = 2.0f, VolumeMultiplier = 3.0f,
                DurationMinutes = 90, RemainingMinutes = 90, TriggeredAt = t,
            }
        ),
        // 1: Earnings surprise
        (
            (s, pos) => pos
                ? $"💬 Market Rumor: Whispers from {s.Name}'s supply chain suggest stronger-than-expected quarter."
                : $"💬 Market Rumor: Industry contacts hint that {s.Name} may report disappointing numbers.",
            (s, pos, t) => new GameEvent
            {
                Type = EventType.Company, Severity = EventSeverity.Major,
                Sentiment = pos ? 0.7f : -0.7f,
                Headline = pos
                    ? $"{s.Name} ({s.Symbol}) reports blowout earnings, revenue surges past estimates"
                    : $"{s.Name} ({s.Symbol}) misses earnings badly, issues downward guidance revision",
                AffectedSymbols = new List<string> { s.Symbol },
                PriceEffect = pos ? 0.08f : -0.09f,
                VolatilityMultiplier = 2.0f, VolumeMultiplier = 3.5f,
                DurationMinutes = 60, RemainingMinutes = 60, TriggeredAt = t,
            }
        ),
        // 2: Partnership / contract
        (
            (s, pos) => pos
                ? $"💬 Market Rumor: {s.Name} is reportedly in advanced talks for a significant strategic deal."
                : $"💬 Market Rumor: A major partnership involving {s.Name} may be falling apart.",
            (s, pos, t) => new GameEvent
            {
                Type = EventType.Company, Severity = EventSeverity.Moderate,
                Sentiment = pos ? 0.5f : -0.5f,
                Headline = pos
                    ? $"{s.Name} ({s.Symbol}) announces landmark strategic partnership"
                    : $"{s.Name} ({s.Symbol}) loses key partnership, revenue outlook uncertain",
                AffectedSymbols = new List<string> { s.Symbol },
                PriceEffect = pos ? 0.04f : -0.05f,
                VolatilityMultiplier = 1.5f, VolumeMultiplier = 2.0f,
                DurationMinutes = 70, RemainingMinutes = 70, TriggeredAt = t,
            }
        ),
        // 3: Regulatory / legal
        (
            (s, pos) => pos
                ? $"💬 Market Rumor: {s.Name} may receive favorable regulatory clearance soon."
                : $"💬 Market Rumor: There are whispers of a regulatory probe targeting {s.Name}.",
            (s, pos, t) => new GameEvent
            {
                Type = EventType.Company, Severity = EventSeverity.Major,
                Sentiment = pos ? 0.5f : -0.7f,
                Headline = pos
                    ? $"{s.Name} ({s.Symbol}) receives key regulatory approval, clearing major hurdle"
                    : $"{s.Name} ({s.Symbol}) faces formal regulatory investigation, shares slide",
                AffectedSymbols = new List<string> { s.Symbol },
                PriceEffect = pos ? 0.05f : -0.07f,
                VolatilityMultiplier = 1.8f, VolumeMultiplier = 2.5f,
                DurationMinutes = 120, RemainingMinutes = 120, TriggeredAt = t,
            }
        ),
        // 4: Leadership change
        (
            (s, pos) => pos
                ? $"💬 Market Rumor: {s.Name} is said to be recruiting a high-profile industry leader."
                : $"💬 Market Rumor: Sources close to {s.Name} suggest senior leadership may be departing.",
            (s, pos, t) => new GameEvent
            {
                Type = EventType.Company, Severity = EventSeverity.Moderate,
                Sentiment = pos ? 0.3f : -0.5f,
                Headline = pos
                    ? $"{s.Name} ({s.Symbol}) appoints acclaimed industry veteran as new CEO"
                    : $"{s.Name} ({s.Symbol}) CEO announces surprise resignation",
                AffectedSymbols = new List<string> { s.Symbol },
                PriceEffect = pos ? 0.03f : -0.06f,
                VolatilityMultiplier = 1.5f, VolumeMultiplier = 2.0f,
                DurationMinutes = 90, RemainingMinutes = 90, TriggeredAt = t,
            }
        ),
        // 5: M&A / acquisition
        (
            (s, pos) => pos
                ? $"💬 Market Rumor: {s.Name} is rumored to be an acquisition target."
                : $"💬 Market Rumor: An attempted acquisition of {s.Name} may be called off.",
            (s, pos, t) => new GameEvent
            {
                Type = EventType.Company, Severity = EventSeverity.Major,
                Sentiment = pos ? 0.8f : -0.4f,
                Headline = pos
                    ? $"{s.Name} ({s.Symbol}) receives premium buyout offer, shares surge"
                    : $"Acquisition talks for {s.Name} ({s.Symbol}) collapse, shares retreat",
                AffectedSymbols = new List<string> { s.Symbol },
                PriceEffect = pos ? 0.12f : -0.05f,
                VolatilityMultiplier = 2.5f, VolumeMultiplier = 4.0f,
                DurationMinutes = 60, RemainingMinutes = 60, TriggeredAt = t,
            }
        ),
        // 6: Clinical trial / R&D (great for pharma/biotech but works for any)
        (
            (s, pos) => pos
                ? $"💬 Market Rumor: Insiders hint at a breakthrough development at {s.Name}."
                : $"💬 Market Rumor: There are concerns about a key project failure at {s.Name}.",
            (s, pos, t) => new GameEvent
            {
                Type = EventType.Company, Severity = EventSeverity.Major,
                Sentiment = pos ? 0.7f : -0.7f,
                Headline = pos
                    ? $"{s.Name} ({s.Symbol}) announces major R&D breakthrough, analysts raise targets"
                    : $"{s.Name} ({s.Symbol}) scraps key development project after costly failure",
                AffectedSymbols = new List<string> { s.Symbol },
                PriceEffect = pos ? 0.07f : -0.08f,
                VolatilityMultiplier = 2.0f, VolumeMultiplier = 3.0f,
                DurationMinutes = 90, RemainingMinutes = 90, TriggeredAt = t,
            }
        ),
        // 7: Financial / accounting
        (
            (s, pos) => pos
                ? $"💬 Market Rumor: {s.Name} may announce a significant share buyback program."
                : $"💬 Market Rumor: Questions are being raised about {s.Name}'s accounting practices.",
            (s, pos, t) => new GameEvent
            {
                Type = EventType.Company, Severity = pos ? EventSeverity.Moderate : EventSeverity.Major,
                Sentiment = pos ? 0.4f : -0.8f,
                Headline = pos
                    ? $"{s.Name} ({s.Symbol}) announces massive stock buyback program"
                    : $"{s.Name} ({s.Symbol}) discloses accounting irregularities, CFO suspended",
                AffectedSymbols = new List<string> { s.Symbol },
                PriceEffect = pos ? 0.03f : -0.10f,
                VolatilityMultiplier = pos ? 1.3f : 2.5f, VolumeMultiplier = pos ? 1.5f : 4.0f,
                DurationMinutes = pos ? 60 : 120, RemainingMinutes = pos ? 60 : 120, TriggeredAt = t,
            }
        ),
    };
}
