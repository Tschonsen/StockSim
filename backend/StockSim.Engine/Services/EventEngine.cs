using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// Generates and manages market events that affect stock prices.
/// See Bible 8.1-8.4 for event system design.
///
/// Phase 1 MVP: 15 event templates (5 Macro, 5 Sector, 5 Company).
/// Events trigger randomly based on probability per tick.
/// Active events modify stock prices each tick until they expire.
/// </summary>
public class EventEngine
{
    private readonly Random _rng;
    private readonly Logger _log = new("EventEngine");
    private readonly List<GameEvent> _activeEvents = new();
    private readonly List<GameEvent> _eventHistory = new();

    /// <summary>All active events affecting the market.</summary>
    public IReadOnlyList<GameEvent> ActiveEvents => _activeEvents.AsReadOnly();

    /// <summary>Full event history for the session.</summary>
    public IReadOnlyList<GameEvent> EventHistory => _eventHistory.AsReadOnly();

    /// <summary>Events triggered this tick (for sending to frontend).</summary>
    public List<GameEvent> NewEventsThisTick { get; } = new();

    // Base probability of an event per tick (tuned for ~2-5 events per trading day)
    private const double MacroEventChance = 0.001;   // ~1 per 2-3 days
    private const double SectorEventChance = 0.002;   // ~2-3 per day
    private const double CompanyEventChance = 0.003;  // ~3-5 per day

    public EventEngine(int seed)
    {
        _rng = new Random(seed);
    }

    /// <summary>
    /// Called each tick. May generate new events and applies active event effects.
    /// </summary>
    public void Tick(IReadOnlyList<Stock> stocks, DateTime gameTime, bool isMarketOpen)
    {
        if (!isMarketOpen) return;

        NewEventsThisTick.Clear();

        // Try to generate new events
        TryGenerateMacroEvent(stocks, gameTime);
        TryGenerateSectorEvent(stocks, gameTime);
        TryGenerateCompanyEvent(stocks, gameTime);

        // Apply active events to stock prices
        ApplyActiveEvents(stocks);

        // Tick down durations
        foreach (var evt in _activeEvents)
        {
            evt.RemainingMinutes--;
        }

        // Remove expired events
        _activeEvents.RemoveAll(e => !e.IsActive);
    }

    /// <summary>
    /// Apply price/volatility effects from all active events to affected stocks.
    /// Effect is spread over the duration (per-tick fraction).
    /// </summary>
    private void ApplyActiveEvents(IReadOnlyList<Stock> stocks)
    {
        foreach (var evt in _activeEvents)
        {
            if (evt.DurationMinutes <= 0) continue;

            // Per-tick price effect (spread over duration)
            var tickPriceEffect = (decimal)(evt.PriceEffect / evt.DurationMinutes);

            foreach (var stock in stocks)
            {
                bool affected = false;

                if (evt.Type == EventType.Macro)
                {
                    affected = true;
                }
                else if (evt.Type == EventType.Sector && evt.AffectedSectors.Contains(stock.Sector))
                {
                    affected = true;
                }
                else if (evt.Type == EventType.Company && evt.AffectedSymbols.Contains(stock.Symbol))
                {
                    affected = true;
                }

                if (affected)
                {
                    // Apply gradual price effect
                    var priceChange = stock.CurrentPrice * tickPriceEffect;
                    stock.CurrentPrice = Math.Max(0.01m, Math.Round(stock.CurrentPrice + priceChange, 2));

                    // Update bid/ask around new price
                    var spread = stock.AskPrice - stock.BidPrice;
                    if (spread <= 0) spread = stock.CurrentPrice * 0.002m;
                    stock.BidPrice = Math.Round(stock.CurrentPrice - spread / 2, 2);
                    stock.AskPrice = Math.Round(stock.CurrentPrice + spread / 2, 2);
                }
            }
        }
    }

    // --- Event Generation ---

    private void TryGenerateMacroEvent(IReadOnlyList<Stock> stocks, DateTime gameTime)
    {
        if (_rng.NextDouble() > MacroEventChance) return;

        var templates = MacroTemplates;
        var template = templates[_rng.Next(templates.Length)];
        var evt = template(gameTime);
        RegisterEvent(evt);
    }

    private void TryGenerateSectorEvent(IReadOnlyList<Stock> stocks, DateTime gameTime)
    {
        if (_rng.NextDouble() > SectorEventChance) return;

        var sectors = stocks.Select(s => s.Sector).Distinct().ToList();
        var sector = sectors[_rng.Next(sectors.Count)];

        var templates = SectorTemplates;
        var template = templates[_rng.Next(templates.Length)];
        var evt = template(sector, gameTime);
        RegisterEvent(evt);
    }

    private void TryGenerateCompanyEvent(IReadOnlyList<Stock> stocks, DateTime gameTime)
    {
        if (_rng.NextDouble() > CompanyEventChance) return;

        var stock = stocks[_rng.Next(stocks.Count)];

        var templates = CompanyTemplates;
        var template = templates[_rng.Next(templates.Length)];
        var evt = template(stock, gameTime);
        RegisterEvent(evt);
    }

    private void RegisterEvent(GameEvent evt)
    {
        _activeEvents.Add(evt);
        _eventHistory.Add(evt);
        NewEventsThisTick.Add(evt);

        _log.Info("Event triggered", new
        {
            id = evt.Id,
            type = evt.Type.ToString(),
            severity = evt.Severity.ToString(),
            headline = evt.Headline,
            priceEffect = evt.PriceEffect,
            duration = evt.DurationMinutes,
        });
    }

    // === EVENT TEMPLATES (Bible 8.2) ===

    // --- 5 Macro Templates ---
    private Func<DateTime, GameEvent>[] MacroTemplates => new Func<DateTime, GameEvent>[]
    {
        // 1. Fed Rate Decision
        (gameTime) =>
        {
            var isHike = _rng.NextDouble() < 0.5;
            return new GameEvent
            {
                Type = EventType.Macro,
                Severity = EventSeverity.Major,
                Sentiment = isHike ? -0.3f : 0.4f,
                Headline = isHike
                    ? "Federal Reserve raises interest rates by 25 basis points"
                    : "Federal Reserve cuts interest rates by 25 basis points",
                PriceEffect = isHike ? -0.015f : 0.015f,
                VolatilityMultiplier = 1.5f,
                VolumeMultiplier = 2.0f,
                DurationMinutes = 120,
                RemainingMinutes = 120,
                TriggeredAt = gameTime,
            };
        },
        // 2. Inflation Data
        (gameTime) =>
        {
            var isHot = _rng.NextDouble() < 0.4;
            return new GameEvent
            {
                Type = EventType.Macro,
                Severity = EventSeverity.Moderate,
                Sentiment = isHot ? -0.4f : 0.3f,
                Headline = isHot
                    ? $"CPI inflation comes in above expectations at {3.5 + _rng.NextDouble() * 2:F1}%"
                    : $"Inflation cools to {1.8 + _rng.NextDouble() * 0.8:F1}%, below expectations",
                PriceEffect = isHot ? -0.012f : 0.010f,
                VolatilityMultiplier = 1.3f,
                DurationMinutes = 90,
                RemainingMinutes = 90,
                TriggeredAt = gameTime,
            };
        },
        // 3. Jobs Report
        (gameTime) =>
        {
            var isStrong = _rng.NextDouble() < 0.5;
            return new GameEvent
            {
                Type = EventType.Macro,
                Severity = EventSeverity.Moderate,
                Sentiment = isStrong ? 0.3f : -0.3f,
                Headline = isStrong
                    ? $"Economy adds {200 + _rng.Next(150)}K jobs, beating expectations"
                    : $"Jobs report disappoints: only {50 + _rng.Next(80)}K added",
                PriceEffect = isStrong ? 0.008f : -0.010f,
                DurationMinutes = 60,
                RemainingMinutes = 60,
                TriggeredAt = gameTime,
            };
        },
        // 4. GDP Report
        (gameTime) =>
        {
            var isGood = _rng.NextDouble() < 0.5;
            return new GameEvent
            {
                Type = EventType.Macro,
                Severity = EventSeverity.Moderate,
                Sentiment = isGood ? 0.2f : -0.3f,
                Headline = isGood
                    ? $"GDP grows {2.0 + _rng.NextDouble() * 2:F1}% in Q{1 + _rng.Next(4)}, above forecast"
                    : $"GDP contracts {-0.5 - _rng.NextDouble():F1}%, raising recession fears",
                PriceEffect = isGood ? 0.008f : -0.015f,
                DurationMinutes = 90,
                RemainingMinutes = 90,
                TriggeredAt = gameTime,
            };
        },
        // 5. Trade War / Geopolitical
        (gameTime) =>
        {
            var isEscalation = _rng.NextDouble() < 0.5;
            return new GameEvent
            {
                Type = EventType.Macro,
                Severity = EventSeverity.Major,
                Sentiment = isEscalation ? -0.5f : 0.3f,
                Headline = isEscalation
                    ? "Trade tensions escalate as new tariffs announced on imports"
                    : "Trade deal breakthrough: tariffs to be reduced over 6 months",
                PriceEffect = isEscalation ? -0.020f : 0.015f,
                VolatilityMultiplier = 1.8f,
                VolumeMultiplier = 1.5f,
                DurationMinutes = 150,
                RemainingMinutes = 150,
                TriggeredAt = gameTime,
            };
        },
    };

    // --- 5 Sector Templates ---
    private Func<string, DateTime, GameEvent>[] SectorTemplates => new Func<string, DateTime, GameEvent>[]
    {
        // 1. Regulatory action
        (sector, gameTime) => new GameEvent
        {
            Type = EventType.Sector,
            Severity = EventSeverity.Moderate,
            Sentiment = -0.3f,
            Headline = $"Government announces new regulatory framework for {sector} industry",
            AffectedSectors = new List<string> { sector },
            PriceEffect = -0.020f,
            VolatilityMultiplier = 1.4f,
            DurationMinutes = 90,
            RemainingMinutes = 90,
            TriggeredAt = gameTime,
        },
        // 2. Positive sector news
        (sector, gameTime) => new GameEvent
        {
            Type = EventType.Sector,
            Severity = EventSeverity.Moderate,
            Sentiment = 0.4f,
            Headline = $"Strong demand outlook boosts {sector} sector confidence",
            AffectedSectors = new List<string> { sector },
            PriceEffect = 0.025f,
            VolumeMultiplier = 1.5f,
            DurationMinutes = 120,
            RemainingMinutes = 120,
            TriggeredAt = gameTime,
        },
        // 3. Supply chain disruption
        (sector, gameTime) => new GameEvent
        {
            Type = EventType.Sector,
            Severity = EventSeverity.Moderate,
            Sentiment = -0.4f,
            Headline = $"Supply chain disruptions hit {sector} companies",
            AffectedSectors = new List<string> { sector },
            PriceEffect = -0.018f,
            VolatilityMultiplier = 1.3f,
            DurationMinutes = 100,
            RemainingMinutes = 100,
            TriggeredAt = gameTime,
        },
        // 4. Industry breakthrough
        (sector, gameTime) => new GameEvent
        {
            Type = EventType.Sector,
            Severity = EventSeverity.Major,
            Sentiment = 0.5f,
            Headline = $"Major breakthrough reported in {sector} sector, analysts upgrade outlook",
            AffectedSectors = new List<string> { sector },
            PriceEffect = 0.035f,
            VolumeMultiplier = 2.0f,
            DurationMinutes = 150,
            RemainingMinutes = 150,
            TriggeredAt = gameTime,
        },
        // 5. Sector downturn warning
        (sector, gameTime) => new GameEvent
        {
            Type = EventType.Sector,
            Severity = EventSeverity.Minor,
            Sentiment = -0.2f,
            Headline = $"Analysts warn of slowing growth in {sector} sector",
            AffectedSectors = new List<string> { sector },
            PriceEffect = -0.010f,
            DurationMinutes = 60,
            RemainingMinutes = 60,
            TriggeredAt = gameTime,
        },
    };

    // --- 5 Company Templates ---
    private Func<Stock, DateTime, GameEvent>[] CompanyTemplates => new Func<Stock, DateTime, GameEvent>[]
    {
        // 1. Earnings Beat
        (stock, gameTime) => new GameEvent
        {
            Type = EventType.Company,
            Severity = EventSeverity.Major,
            Sentiment = 0.6f,
            Headline = $"{stock.Name} ({stock.Symbol}) beats earnings estimates, revenue up {10 + _rng.Next(15)}%",
            AffectedSymbols = new List<string> { stock.Symbol },
            PriceEffect = 0.05f + (float)_rng.NextDouble() * 0.05f,
            VolumeMultiplier = 3.0f,
            DurationMinutes = 60,
            RemainingMinutes = 60,
            TriggeredAt = gameTime,
        },
        // 2. Earnings Miss
        (stock, gameTime) => new GameEvent
        {
            Type = EventType.Company,
            Severity = EventSeverity.Major,
            Sentiment = -0.6f,
            Headline = $"{stock.Name} ({stock.Symbol}) misses earnings expectations, guidance lowered",
            AffectedSymbols = new List<string> { stock.Symbol },
            PriceEffect = -0.07f - (float)_rng.NextDouble() * 0.08f,
            VolumeMultiplier = 3.0f,
            DurationMinutes = 60,
            RemainingMinutes = 60,
            TriggeredAt = gameTime,
        },
        // 3. CEO Resignation
        (stock, gameTime) => new GameEvent
        {
            Type = EventType.Company,
            Severity = EventSeverity.Moderate,
            Sentiment = -0.4f,
            Headline = $"{stock.Name} ({stock.Symbol}) CEO steps down unexpectedly",
            AffectedSymbols = new List<string> { stock.Symbol },
            PriceEffect = -0.05f - (float)_rng.NextDouble() * 0.05f,
            VolatilityMultiplier = 2.0f,
            VolumeMultiplier = 2.5f,
            DurationMinutes = 90,
            RemainingMinutes = 90,
            TriggeredAt = gameTime,
        },
        // 4. Product Launch
        (stock, gameTime) => new GameEvent
        {
            Type = EventType.Company,
            Severity = EventSeverity.Moderate,
            Sentiment = 0.5f,
            Headline = $"{stock.Name} ({stock.Symbol}) launches new product to strong initial reviews",
            AffectedSymbols = new List<string> { stock.Symbol },
            PriceEffect = 0.04f + (float)_rng.NextDouble() * 0.04f,
            VolumeMultiplier = 2.0f,
            DurationMinutes = 90,
            RemainingMinutes = 90,
            TriggeredAt = gameTime,
        },
        // 5. Regulatory Investigation
        (stock, gameTime) => new GameEvent
        {
            Type = EventType.Company,
            Severity = EventSeverity.Moderate,
            Sentiment = -0.3f,
            Headline = $"{stock.Name} ({stock.Symbol}) faces regulatory investigation",
            AffectedSymbols = new List<string> { stock.Symbol },
            PriceEffect = -0.05f - (float)_rng.NextDouble() * 0.05f,
            VolatilityMultiplier = 1.5f,
            VolumeMultiplier = 2.0f,
            DurationMinutes = 120,
            RemainingMinutes = 120,
            TriggeredAt = gameTime,
        },
    };
}
