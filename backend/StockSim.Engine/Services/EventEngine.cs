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

    // --- 15 Macro Templates (Bible 8.2.1) ---
    private Func<DateTime, GameEvent>[] MacroTemplates => new Func<DateTime, GameEvent>[]
    {
        t => MakeMacro(t, _rng.NextDouble() < 0.5,
            "Federal Reserve raises interest rates by 25 basis points",
            "Federal Reserve cuts interest rates by 25 basis points",
            -0.015f, 0.015f, EventSeverity.Major, 120),
        t => MakeMacro(t, _rng.NextDouble() < 0.4,
            $"CPI inflation surges to {3.5 + _rng.NextDouble() * 2:F1}%, above expectations",
            $"Inflation cools to {1.8 + _rng.NextDouble() * 0.8:F1}%, below expectations",
            -0.012f, 0.010f, EventSeverity.Moderate, 90),
        t => MakeMacro(t, _rng.NextDouble() < 0.5,
            $"Economy adds {200 + _rng.Next(150)}K jobs, beating expectations",
            $"Jobs report disappoints: only {50 + _rng.Next(80)}K added",
            0.008f, -0.010f, EventSeverity.Moderate, 60),
        t => MakeMacro(t, _rng.NextDouble() < 0.5,
            $"GDP grows {2.0 + _rng.NextDouble() * 2:F1}% in Q{1 + _rng.Next(4)}, above forecast",
            $"GDP contracts {-0.5 - _rng.NextDouble():F1}%, raising recession fears",
            0.008f, -0.015f, EventSeverity.Moderate, 90),
        t => MakeMacro(t, _rng.NextDouble() < 0.5,
            "Trade tensions escalate as new tariffs announced on imports",
            "Trade deal breakthrough: tariffs to be reduced over 6 months",
            -0.020f, 0.015f, EventSeverity.Major, 150),
        t => MakeMacro(t, _rng.NextDouble() < 0.3,
            "Consumer confidence index drops to lowest level in 2 years",
            "Consumer confidence surges to 18-month high",
            -0.008f, 0.006f, EventSeverity.Minor, 60),
        t => MakeMacro(t, _rng.NextDouble() < 0.5,
            $"Oil prices surge {5 + _rng.Next(15)}% on supply concerns",
            $"Oil prices drop {5 + _rng.Next(10)}% as demand weakens",
            -0.005f, 0.003f, EventSeverity.Moderate, 90),
        t => MakeMacro(t, _rng.NextDouble() < 0.4,
            "Treasury yields spike to multi-year highs, pressuring equities",
            "Treasury yields fall sharply, boosting growth stocks",
            -0.012f, 0.010f, EventSeverity.Moderate, 100),
        t => MakeMacro(t, _rng.NextDouble() < 0.5,
            $"Retail sales decline {1 + _rng.NextDouble() * 2:F1}%, missing estimates",
            $"Retail sales jump {2 + _rng.NextDouble() * 3:F1}%, beating expectations",
            -0.006f, 0.005f, EventSeverity.Minor, 60),
        t => MakeMacro(t, _rng.NextDouble() < 0.3,
            "Housing market shows signs of cooling as mortgage rates rise",
            "Housing starts surge, signaling economic strength",
            -0.005f, 0.004f, EventSeverity.Minor, 60),
        t => MakeMacro(t, true,
            "Federal Reserve chair signals hawkish stance in congressional testimony",
            "", -0.010f, 0f, EventSeverity.Moderate, 90),
        t => MakeMacro(t, true,
            $"US dollar strengthens {1 + _rng.Next(3)}% against major currencies",
            "", -0.005f, 0f, EventSeverity.Minor, 60),
        t => MakeMacro(t, false, "",
            "Manufacturing PMI expands for third consecutive month",
            0f, 0.006f, EventSeverity.Minor, 60),
        t => MakeMacro(t, true,
            "Government shutdown looms as budget negotiations stall",
            "", -0.008f, 0f, EventSeverity.Moderate, 120),
        t => MakeMacro(t, false, "",
            "Infrastructure spending bill signed, boosting industrial outlook",
            0f, 0.010f, EventSeverity.Moderate, 120),
    };

    private GameEvent MakeMacro(DateTime t, bool isNegative, string negHeadline, string posHeadline,
        float negEffect, float posEffect, EventSeverity severity, int duration)
    {
        var headline = isNegative ? negHeadline : posHeadline;
        if (string.IsNullOrEmpty(headline)) headline = isNegative ? negHeadline : posHeadline;
        return new GameEvent
        {
            Type = EventType.Macro, Severity = severity,
            Sentiment = isNegative ? -0.3f : 0.3f,
            Headline = headline,
            PriceEffect = isNegative ? negEffect : posEffect,
            VolatilityMultiplier = severity == EventSeverity.Major ? 1.5f : 1.2f,
            VolumeMultiplier = severity == EventSeverity.Major ? 2.0f : 1.3f,
            DurationMinutes = duration, RemainingMinutes = duration, TriggeredAt = t,
        };
    }

    // --- 15 Sector Templates (Bible 8.2.2) ---
    private Func<string, DateTime, GameEvent>[] SectorTemplates => new Func<string, DateTime, GameEvent>[]
    {
        (s, t) => MakeSector(s, t, $"Government announces new regulatory framework for {s} industry", -0.020f, EventSeverity.Moderate, 90),
        (s, t) => MakeSector(s, t, $"Strong demand outlook boosts {s} sector confidence", 0.025f, EventSeverity.Moderate, 120),
        (s, t) => MakeSector(s, t, $"Supply chain disruptions hit {s} companies", -0.018f, EventSeverity.Moderate, 100),
        (s, t) => MakeSector(s, t, $"Major breakthrough reported in {s} sector, analysts upgrade outlook", 0.035f, EventSeverity.Major, 150),
        (s, t) => MakeSector(s, t, $"Analysts warn of slowing growth in {s} sector", -0.010f, EventSeverity.Minor, 60),
        (s, t) => MakeSector(s, t, $"New government subsidies announced for {s} sector", 0.020f, EventSeverity.Moderate, 90),
        (s, t) => MakeSector(s, t, $"{s} sector faces margin pressure as input costs rise", -0.015f, EventSeverity.Moderate, 80),
        (s, t) => MakeSector(s, t, $"Foreign investment surges into {s} sector", 0.015f, EventSeverity.Minor, 70),
        (s, t) => MakeSector(s, t, $"Labor shortage worsens in {s} industry", -0.012f, EventSeverity.Minor, 60),
        (s, t) => MakeSector(s, t, $"{s} companies report record Q{1+_rng.Next(4)} bookings", 0.030f, EventSeverity.Major, 120),
        (s, t) => MakeSector(s, t, $"Environmental concerns mount for {s} sector operations", -0.010f, EventSeverity.Minor, 60),
        (s, t) => MakeSector(s, t, $"Analyst initiates coverage on {s} sector with Overweight rating", 0.012f, EventSeverity.Minor, 60),
        (s, t) => MakeSector(s, t, $"Major {s} conference highlights strong innovation pipeline", 0.018f, EventSeverity.Moderate, 90),
        (s, t) => MakeSector(s, t, $"Import tariffs threaten {s} sector profitability", -0.020f, EventSeverity.Moderate, 100),
        (s, t) => MakeSector(s, t, $"Consolidation wave expected in {s} sector as M&A activity rises", 0.008f, EventSeverity.Minor, 60),
    };

    private GameEvent MakeSector(string sector, DateTime t, string headline, float effect, EventSeverity severity, int duration) => new()
    {
        Type = EventType.Sector, Severity = severity,
        Sentiment = effect > 0 ? 0.3f : -0.3f, Headline = headline,
        AffectedSectors = new List<string> { sector },
        PriceEffect = effect,
        VolatilityMultiplier = severity == EventSeverity.Major ? 1.5f : 1.2f,
        VolumeMultiplier = severity == EventSeverity.Major ? 2.0f : 1.3f,
        DurationMinutes = duration, RemainingMinutes = duration, TriggeredAt = t,
    };

    // --- 20 Company Templates (Bible 8.2.3-8.2.4) ---
    private Func<Stock, DateTime, GameEvent>[] CompanyTemplates => new Func<Stock, DateTime, GameEvent>[]
    {
        (s, t) => MakeCompany(s, t, $"{s.Name} ({s.Symbol}) beats earnings estimates, revenue up {10+_rng.Next(15)}%", 0.05f + (float)_rng.NextDouble()*0.05f, EventSeverity.Major, 60),
        (s, t) => MakeCompany(s, t, $"{s.Name} ({s.Symbol}) misses earnings expectations, guidance lowered", -0.07f - (float)_rng.NextDouble()*0.08f, EventSeverity.Major, 60),
        (s, t) => MakeCompany(s, t, $"{s.Name} ({s.Symbol}) CEO steps down unexpectedly", -0.05f - (float)_rng.NextDouble()*0.05f, EventSeverity.Moderate, 90),
        (s, t) => MakeCompany(s, t, $"{s.Name} ({s.Symbol}) launches new product to strong initial reviews", 0.04f + (float)_rng.NextDouble()*0.04f, EventSeverity.Moderate, 90),
        (s, t) => MakeCompany(s, t, $"{s.Name} ({s.Symbol}) faces regulatory investigation", -0.05f - (float)_rng.NextDouble()*0.05f, EventSeverity.Moderate, 120),
        (s, t) => MakeCompany(s, t, $"{s.Name} ({s.Symbol}) announces ${50+_rng.Next(200)}M stock buyback program", 0.02f + (float)_rng.NextDouble()*0.02f, EventSeverity.Minor, 60),
        (s, t) => MakeCompany(s, t, $"{s.Name} ({s.Symbol}) wins major government contract", 0.06f + (float)_rng.NextDouble()*0.04f, EventSeverity.Major, 90),
        (s, t) => MakeCompany(s, t, $"{s.Name} ({s.Symbol}) announces strategic partnership", 0.03f + (float)_rng.NextDouble()*0.03f, EventSeverity.Moderate, 70),
        (s, t) => MakeCompany(s, t, $"{s.Name} ({s.Symbol}) issues profit warning for next quarter", -0.08f - (float)_rng.NextDouble()*0.05f, EventSeverity.Major, 60),
        (s, t) => MakeCompany(s, t, $"{s.Name} ({s.Symbol}) reports data breach affecting {1+_rng.Next(5)}M customers", -0.04f - (float)_rng.NextDouble()*0.04f, EventSeverity.Moderate, 90),
        (s, t) => MakeCompany(s, t, $"Analyst upgrades {s.Symbol} to Strong Buy with ${(int)(s.CurrentPrice*1.3m)} price target", 0.03f + (float)_rng.NextDouble()*0.02f, EventSeverity.Minor, 60),
        (s, t) => MakeCompany(s, t, $"Analyst downgrades {s.Symbol} to Sell, cites weakening fundamentals", -0.03f - (float)_rng.NextDouble()*0.02f, EventSeverity.Minor, 60),
        (s, t) => MakeCompany(s, t, $"{s.Name} ({s.Symbol}) wins patent lawsuit, awarded ${10+_rng.Next(50)}M in damages", 0.03f + (float)_rng.NextDouble()*0.05f, EventSeverity.Moderate, 90),
        (s, t) => MakeCompany(s, t, $"{s.Name} ({s.Symbol}) recalls product due to safety concerns", -0.04f - (float)_rng.NextDouble()*0.04f, EventSeverity.Moderate, 90),
        (s, t) => MakeCompany(s, t, $"{s.Name} ({s.Symbol}) CFO resigns amid accounting concerns", -0.06f - (float)_rng.NextDouble()*0.06f, EventSeverity.Major, 120),
        (s, t) => MakeCompany(s, t, $"Insider buying detected: {s.Symbol} executives purchase ${1+_rng.Next(5)}M in shares", 0.02f + (float)_rng.NextDouble()*0.02f, EventSeverity.Minor, 60),
        (s, t) => MakeCompany(s, t, $"{s.Name} ({s.Symbol}) secures ${100+_rng.Next(400)}M in new funding", 0.04f + (float)_rng.NextDouble()*0.03f, EventSeverity.Moderate, 70),
        (s, t) => MakeCompany(s, t, $"{s.Name} ({s.Symbol}) announces {5+_rng.Next(15)}% workforce reduction", -0.03f - (float)_rng.NextDouble()*0.03f, EventSeverity.Moderate, 80),
        (s, t) => MakeCompany(s, t, $"{s.Name} ({s.Symbol}) reports record quarterly revenue of ${10+_rng.Next(90)}B", 0.06f + (float)_rng.NextDouble()*0.04f, EventSeverity.Major, 60),
        (s, t) => MakeCompany(s, t, $"{s.Name} ({s.Symbol}) faces class-action lawsuit from shareholders", -0.04f - (float)_rng.NextDouble()*0.06f, EventSeverity.Moderate, 120),
    };

    private GameEvent MakeCompany(Stock stock, DateTime t, string headline, float effect, EventSeverity severity, int duration) => new()
    {
        Type = EventType.Company, Severity = severity,
        Sentiment = effect > 0 ? 0.5f : -0.5f, Headline = headline,
        AffectedSymbols = new List<string> { stock.Symbol },
        PriceEffect = effect,
        VolatilityMultiplier = severity == EventSeverity.Major ? 2.0f : 1.3f,
        VolumeMultiplier = severity == EventSeverity.Major ? 3.0f : 1.5f,
        DurationMinutes = duration, RemainingMinutes = duration, TriggeredAt = t,
    };
}
