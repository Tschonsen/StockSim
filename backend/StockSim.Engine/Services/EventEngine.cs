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

    // Base probability of an event per tick (tuned for 3-5 visible news per trading day)
    // 390 market-open ticks/day, so P/tick × 390 = expected/day
    // Session 25: doubled again (was 0.002/0.006/0.005, playtest showed only ~1/day)
    private const double MacroEventChance = 0.004;     // ~1.6/day
    private const double SectorEventChance = 0.012;    // ~4.7/day
    private const double CompanyEventChance = 0.010;   // ~3.9/day
    private const double FlashCrashChance = 0.00003;   // ~1 per 85 trading days

    /// <summary>Multiplier for event frequency. 0.5 = half, 2.0 = double. Set from NewGame config.</summary>
    public double FrequencyMultiplier { get; set; } = 1.0;

    // Daily event cap to prevent spam (max 20 events per trading day)
    private const int MaxEventsPerDay = 20;
    private int _eventsToday;
    private DateTime _lastEventDate;

    // Per-stock cooldown: max 1 company event per stock per 5 trading days
    private readonly Dictionary<string, DateTime> _lastCompanyEvent = new();

    // === EVENT CASCADE SYSTEM ===
    // Pending follow-up events that fire after a delay (realistic chain reactions)
    private readonly List<PendingFollowUp> _pendingFollowUps = new();

    // === TEMPLATE SYSTEM (Phase 1C) ===
    private readonly TemplateLoader? _templates;

    public EventEngine(int seed, TemplateLoader? templates = null)
    {
        _rng = new Random(seed);
        _templates = templates;
    }

    /// <summary>
    /// Generate 3-5 initial news events so the feed isn't empty at game start.
    /// Called once from GameLoop constructor after stocks are created.
    /// Events have no price effect (they represent "what happened before you started").
    /// </summary>
    public void GenerateInitialNews(IReadOnlyList<Stock> stocks, DateTime gameTime)
    {
        NewEventsThisTick.Clear();
        var sectors = stocks.Select(s => s.Sector).Distinct().ToList();
        var targetCount = 3 + _rng.Next(3); // 3-5 events

        // Generate a mix of event types using templates
        for (int i = 0; i < targetCount; i++)
        {
            GameEvent? evt = null;
            var minutesAgo = (targetCount - i) * 15 + _rng.Next(10); // Stagger timestamps
            var eventTime = gameTime.AddMinutes(-minutesAgo);

            var roll = _rng.NextDouble();
            if (roll < 0.3 && _templates != null)
            {
                // Macro event
                var macros = _templates.GetTemplates(EventTier.Tier1)
                    .Where(t => t.Type.Equals("Macro", StringComparison.OrdinalIgnoreCase)).ToList();
                if (macros.Count > 0)
                    evt = ResolveTemplate(macros[_rng.Next(macros.Count)], null, null, eventTime);
            }
            else if (roll < 0.6 && _templates != null)
            {
                // Sector event
                var sector = sectors[_rng.Next(sectors.Count)];
                var sectorTpls = _templates.GetTemplates(EventTier.Tier1)
                    .Where(t => t.Type.Equals("Sector", StringComparison.OrdinalIgnoreCase)).ToList();
                if (sectorTpls.Count > 0)
                    evt = ResolveTemplate(sectorTpls[_rng.Next(sectorTpls.Count)], null, sector, eventTime);
            }
            else if (_templates != null)
            {
                // Company event
                var stock = stocks.Where(s => !s.Traits.Contains("ETF")).ToList();
                if (stock.Count > 0)
                {
                    var s = stock[_rng.Next(stock.Count)];
                    var companyTpls = _templates.GetTemplates(EventTier.Tier1)
                        .Where(t => t.Type.Equals("Company", StringComparison.OrdinalIgnoreCase)).ToList();
                    if (companyTpls.Count > 0)
                        evt = ResolveTemplate(companyTpls[_rng.Next(companyTpls.Count)], s, s.Sector, eventTime);
                }
            }

            if (evt != null)
            {
                // Zero out price effects — these are "historical" news, not active
                evt.PriceEffect = 0;
                evt.VolatilityMultiplier = 1.0f;
                evt.DurationMinutes = 0;
                evt.RemainingMinutes = 0;
                evt.TriggeredAt = eventTime;
                _eventHistory.Add(evt);
                NewEventsThisTick.Add(evt);
                _log.Info("Initial news generated", new { headline = evt.Headline });
            }
        }
    }

    /// <summary>
    /// Called each tick. May generate new events and applies active event effects.
    /// </summary>
    public void Tick(IReadOnlyList<Stock> stocks, DateTime gameTime, bool isMarketOpen)
    {
        NewEventsThisTick.Clear();

        if (!isMarketOpen) return;

        // Reset daily event counter at day change
        if (gameTime.Date != _lastEventDate.Date)
        {
            _eventsToday = 0;
            _lastEventDate = gameTime;
        }

        // Fire pending follow-up events (cascades)
        FirePendingFollowUps(gameTime);

        // Try to generate new events (respecting daily cap)
        if (_eventsToday < MaxEventsPerDay)
        {
            TryGenerateFlashCrash(stocks, gameTime);
            TryGenerateMacroEvent(stocks, gameTime);
            TryGenerateSectorEvent(stocks, gameTime);
            TryGenerateCompanyEvent(stocks, gameTime);
        }

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
    /// Update analyst ratings and target prices when significant events fire.
    /// Called once per event at registration time (via first tick of the event).
    /// </summary>
    private readonly HashSet<long> _analystUpdatedEvents = new();
    private void UpdateAnalystSentiment(GameEvent evt, IReadOnlyList<Stock> stocks)
    {
        if (_analystUpdatedEvents.Contains(evt.Id)) return;
        if (evt.Severity < EventSeverity.Moderate) return;
        if (Math.Abs(evt.PriceEffect) < 0.02f) return;

        _analystUpdatedEvents.Add(evt.Id);
        var isPositive = evt.PriceEffect > 0;

        foreach (var stock in stocks)
        {
            bool affected = (evt.Type == EventType.Company && evt.AffectedSymbols.Contains(stock.Symbol))
                         || (evt.Type == EventType.Sector && evt.AffectedSectors.Contains(stock.Sector));
            if (!affected) continue;

            // Rating shift: Major events shift more
            var magnitude = evt.Severity >= EventSeverity.Major ? 0.3m : 0.15m;
            var shift = isPositive ? magnitude : -magnitude;
            // Add randomness
            shift += (decimal)(_rng.NextDouble() * 0.1 - 0.05);
            stock.AnalystRating = Math.Round(Math.Max(1.0m, Math.Min(5.0m, stock.AnalystRating + shift)), 1);

            // Target price adjustment
            var targetShift = 1m + (decimal)evt.PriceEffect * (evt.Type == EventType.Company ? 1.2m : 0.5m);
            stock.TargetPrice = Math.Round(stock.TargetPrice * targetShift, 2);

            _log.Debug("Analyst sentiment updated", new
            {
                symbol = stock.Symbol,
                rating = stock.AnalystRating,
                targetPrice = stock.TargetPrice,
                eventHeadline = evt.Headline[..Math.Min(50, evt.Headline.Length)],
            });
        }
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

            // Update analyst ratings on first encounter of this event
            UpdateAnalystSentiment(evt, stocks);

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

                // Sector contagion: company events ripple to sector peers at 30% strength
                bool peerContagion = false;
                if (!affected && evt.Type == EventType.Company
                    && evt.AffectedSectors.Contains(stock.Sector)
                    && !evt.AffectedSymbols.Contains(stock.Symbol)
                    && Math.Abs(evt.PriceEffect) > 0.03f) // Only for significant events (>3%)
                {
                    peerContagion = true;
                }

                if (affected || peerContagion)
                {
                    // Spike spreads during major events (3-5x normal)
                    if (affected && evt.Severity >= EventSeverity.Major && evt.RemainingMinutes > evt.DurationMinutes - 5)
                        stock.SpreadMultiplier = Math.Max(stock.SpreadMultiplier, 3.0m + (decimal)(Math.Abs(evt.PriceEffect) * 5));

                    // Apply gradual price effect (reduced for peer contagion)
                    var effectMultiplier = peerContagion ? 0.3m : 1.0m;
                    var priceChange = stock.CurrentPrice * tickPriceEffect * effectMultiplier;
                    var newPrice = Math.Max(0.01m, stock.CurrentPrice + priceChange);

                    // Daily clamp: severity-dependent (matches PriceEngine clamp)
                    if (stock.PreviousClose > 0)
                    {
                        var clampPct = evt.Severity switch
                        {
                            EventSeverity.Major => 0.05m,
                            EventSeverity.Moderate => 0.04m,
                            _ => 0.03m,
                        };
                        var maxPrice = stock.PreviousClose * (1m + clampPct);
                        var minPrice = stock.PreviousClose * (1m - clampPct);
                        newPrice = Math.Clamp(newPrice, minPrice, maxPrice);
                    }

                    stock.CurrentPrice = Math.Round(newPrice, 2);

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

    /// <summary>
    /// Bible 8.2.8: Flash Crash — rare, market drops 3-7% in minutes, partial recovery.
    /// </summary>
    private void TryGenerateFlashCrash(IReadOnlyList<Stock> stocks, DateTime gameTime)
    {
        if (_rng.NextDouble() > FlashCrashChance) return;

        var severity = 0.03f + (float)_rng.NextDouble() * 0.04f; // 3-7% drop
        var evt = new GameEvent
        {
            Type = EventType.Macro,
            Severity = EventSeverity.Major,
            Sentiment = -0.9f,
            Headline = $"FLASH CRASH: Market plunges {severity * 100:F1}% in minutes as algorithmic selling cascades",
            PriceEffect = -severity,
            VolatilityMultiplier = 3.0f,
            VolumeMultiplier = 5.0f,
            DurationMinutes = 5 + _rng.Next(5), // 5-10 minutes
            RemainingMinutes = 5 + _rng.Next(5),
            TriggeredAt = gameTime,
        };
        RegisterEvent(evt);

        // Partial recovery event (70-90% recovery after crash)
        var recovery = severity * (0.7f + (float)_rng.NextDouble() * 0.2f);
        var recoveryEvt = new GameEvent
        {
            Type = EventType.Macro,
            Severity = EventSeverity.Moderate,
            Sentiment = 0.4f,
            Headline = "Markets recovering from flash crash, buyers stepping in",
            PriceEffect = recovery,
            VolumeMultiplier = 3.0f,
            DurationMinutes = 30,
            RemainingMinutes = 30,
            TriggeredAt = gameTime.AddMinutes(10),
        };
        RegisterEvent(recoveryEvt);

        _log.Warn("FLASH CRASH triggered", new { drop = $"{severity:P1}", recovery = $"{recovery:P1}" });
    }

    private void TryGenerateMacroEvent(IReadOnlyList<Stock> stocks, DateTime gameTime)
    {
        if (_rng.NextDouble() > MacroEventChance * FrequencyMultiplier) return;

        // Try JSON templates first (Phase 1C)
        if (_templates != null)
        {
            var macroTemplates = _templates.GetTemplates(EventTier.Tier1)
                .Where(t => t.Type.Equals("Macro", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (macroTemplates.Count > 0)
            {
                var tpl = macroTemplates[_rng.Next(macroTemplates.Count)];
                var evt = ResolveTemplate(tpl, null, null, gameTime);
                if (evt != null) { RegisterEvent(evt); return; }
            }
        }

        // Fallback: hardcoded templates
        var templates = MacroTemplates;
        var template = templates[_rng.Next(templates.Length)];
        var evt2 = template(gameTime);
        RegisterEvent(evt2);
    }

    private void TryGenerateSectorEvent(IReadOnlyList<Stock> stocks, DateTime gameTime)
    {
        if (_rng.NextDouble() > SectorEventChance * FrequencyMultiplier) return;

        var sectors = stocks.Select(s => s.Sector).Distinct().ToList();
        var sector = sectors[_rng.Next(sectors.Count)];

        // Try JSON templates first (Phase 1C)
        if (_templates != null)
        {
            var sectorTemplates = _templates.GetTemplates(EventTier.Tier1)
                .Where(t => t.Type.Equals("Sector", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (sectorTemplates.Count > 0)
            {
                var tpl = sectorTemplates[_rng.Next(sectorTemplates.Count)];
                var evt = ResolveTemplate(tpl, null, sector, gameTime);
                if (evt != null) { RegisterEvent(evt); return; }
            }
        }

        // Fallback: hardcoded templates
        if (SectorSpecificTemplates.TryGetValue(sector, out var specific))
        {
            var template = specific[_rng.Next(specific.Length)];
            var evt = template(gameTime);
            RegisterEvent(evt);
        }
        else
        {
            var templates = GenericSectorTemplates;
            var template = templates[_rng.Next(templates.Length)];
            var evt = template(sector, gameTime);
            RegisterEvent(evt);
        }
    }

    private void TryGenerateCompanyEvent(IReadOnlyList<Stock> stocks, DateTime gameTime)
    {
        if (_rng.NextDouble() > CompanyEventChance * FrequencyMultiplier) return;

        var stock = stocks[_rng.Next(stocks.Count)];

        // Skip ETFs and stocks that had a recent event (5-day cooldown)
        if (stock.Traits.Contains("ETF")) return;
        if (_lastCompanyEvent.TryGetValue(stock.Symbol, out var lastEvt) && (gameTime - lastEvt).TotalDays < 5) return;

        // Try JSON templates first (Phase 1C)
        if (_templates != null && TryGenerateCompanyFromJson(stock, gameTime))
        {
            _lastCompanyEvent[stock.Symbol] = gameTime;
            return;
        }

        // Fallback: hardcoded templates
        var templates = CompanyTemplates;
        var templateIndex = _rng.Next(templates.Length);

        // Issue 18: CEO Archetype influences event selection
        var archetype = stock.Personality?.CEOArchetype ?? "";
        if (archetype is "Visionary" or "Disruptor")
        {
            if (_rng.NextDouble() < 0.6)
            {
                for (int i = 0; i < 5; i++)
                {
                    var candidate = templates[templateIndex](stock, gameTime);
                    if (candidate.PriceEffect > 0) break;
                    templateIndex = _rng.Next(templates.Length);
                }
            }
        }
        else if (archetype == "Cost-Cutter")
        {
            if (_rng.NextDouble() < 0.4)
                templateIndex = 17;
        }
        else if (archetype == "Founder-CEO")
        {
            if (templateIndex == 2)
                templateIndex = _rng.Next(templates.Length - 1);
            if (templateIndex >= 2) templateIndex++;
        }

        var template = templates[templateIndex];
        var evt = template(stock, gameTime);
        RegisterEvent(evt);
        _lastCompanyEvent[stock.Symbol] = gameTime;

        ScheduleCascades(stock, evt, gameTime);
    }

    /// <summary>Try to generate a company event from JSON templates. Returns true if successful.</summary>
    private bool TryGenerateCompanyFromJson(Stock stock, DateTime gameTime)
    {
        var companyTemplates = _templates!.GetTemplates(EventTier.Tier1)
            .Where(t => t.Type.Equals("Company", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Also include Tier2 company templates (less frequent)
        if (_rng.NextDouble() < 0.15) // 15% chance of Tier-2 event
        {
            companyTemplates = _templates.GetTemplates(EventTier.Tier2)
                .Where(t => t.Type.Equals("Company", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (companyTemplates.Count == 0) return false;

        var tpl = companyTemplates[_rng.Next(companyTemplates.Count)];

        // CEO Archetype bias: Visionary/Disruptor re-roll for positive
        var archetype = stock.Personality?.CEOArchetype ?? "";
        if (archetype is "Visionary" or "Disruptor" && _rng.NextDouble() < 0.6)
        {
            for (int i = 0; i < 5; i++)
            {
                if (tpl.Sentiment > 0) break;
                tpl = companyTemplates[_rng.Next(companyTemplates.Count)];
            }
        }

        var evt = ResolveTemplate(tpl, stock, null, gameTime);
        if (evt == null) return false;

        // Rivalry system — apply to resolved event
        if (!string.IsNullOrEmpty(stock.Personality?.RivalSymbol)
            && evt.Severity >= EventSeverity.Major
            && Math.Abs(evt.PriceEffect) > 0.03f)
        {
            var rivalEffect = -evt.PriceEffect * 0.3f;
            var rivalHeadline = rivalEffect < 0
                ? $"{stock.Personality!.RivalSymbol} faces competitive pressure as rival {stock.Name} gains ground"
                : $"{stock.Personality!.RivalSymbol} benefits as competitor {stock.Name} stumbles";
            var rivalEvt = new GameEvent
            {
                Type = EventType.Company, Severity = EventSeverity.Moderate,
                Sentiment = rivalEffect > 0 ? 0.3f : -0.3f,
                Headline = rivalHeadline,
                AffectedSymbols = new List<string> { stock.Personality!.RivalSymbol },
                AffectedSectors = new List<string> { stock.Sector },
                PriceEffect = rivalEffect,
                VolatilityMultiplier = 1.3f, VolumeMultiplier = 1.5f,
                DurationMinutes = evt.DurationMinutes, RemainingMinutes = evt.DurationMinutes, TriggeredAt = gameTime,
            };
            _activeEvents.Add(rivalEvt);
            _eventHistory.Add(rivalEvt);
            NewEventsThisTick.Add(rivalEvt);
        }

        RegisterEvent(evt);
        ScheduleCascades(stock, evt, gameTime);
        return true;
    }

    /// <summary>
    /// Inject an externally-created event (e.g. from RumorEngine) into the active event system.
    /// The event will be tracked, applied to prices, and sent to the frontend.
    /// </summary>
    public void InjectEvent(GameEvent evt)
    {
        RegisterEvent(evt);
    }

    private static readonly string[] BearishRefs = {
        "— worst since the 2008 financial crisis",
        "— reminiscent of the dot-com crash",
        "— echoing the March 2020 selloff",
        "— biggest decline since the European debt crisis",
        "— sharpest drop since Black Monday",
        "— not seen since the Great Recession",
    };
    private static readonly string[] BullishRefs = {
        "— strongest rally since the post-pandemic recovery",
        "— best performance since the 2013 bull run",
        "— reminiscent of the 1990s tech boom",
        "— biggest gain since quantitative easing began",
        "— not seen since the post-election rally",
    };
    private string EnrichWithHistoricalRef(string headline, float priceEffect)
    {
        var refs = priceEffect < 0 ? BearishRefs : BullishRefs;
        return headline + " " + refs[_rng.Next(refs.Length)];
    }

    private void RegisterEvent(GameEvent evt)
    {
        // Enrich headline with historical reference for major events
        if (evt.Severity >= EventSeverity.Major && _rng.NextDouble() < 0.3)
            evt.Headline = EnrichWithHistoricalRef(evt.Headline, evt.PriceEffect);

        _activeEvents.Add(evt);
        _eventHistory.Add(evt);
        // Cap history to prevent unbounded memory growth (keep last 500)
        if (_eventHistory.Count > 500)
            _eventHistory.RemoveRange(0, _eventHistory.Count - 500);
        NewEventsThisTick.Add(evt);
        _eventsToday++;

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

    // --- Generic Sector Templates (fallback) ---
    private Func<string, DateTime, GameEvent>[] GenericSectorTemplates => new Func<string, DateTime, GameEvent>[]
    {
        (s, t) => MakeSector(s, t, $"Government announces new regulatory framework for {s} industry", -0.020f, EventSeverity.Moderate, 90),
        (s, t) => MakeSector(s, t, $"Strong demand outlook boosts {s} sector confidence", 0.025f, EventSeverity.Moderate, 120),
        (s, t) => MakeSector(s, t, $"Supply chain disruptions hit {s} companies", -0.018f, EventSeverity.Moderate, 100),
        (s, t) => MakeSector(s, t, $"Analysts warn of slowing growth in {s} sector", -0.010f, EventSeverity.Minor, 60),
        (s, t) => MakeSector(s, t, $"Foreign investment surges into {s} sector", 0.015f, EventSeverity.Minor, 70),
    };

    // =====================================================
    // 65 SECTOR-SPECIFIC EVENT TEMPLATES (Bible 8.2.2)
    // ~5-6 per sector × 12 sectors
    // =====================================================
    private Dictionary<string, Func<DateTime, GameEvent>[]>? _sectorSpecificCache;
    private Dictionary<string, Func<DateTime, GameEvent>[]> SectorSpecificTemplates =>
        _sectorSpecificCache ??= new()
    {
        // --- TECHNOLOGY (5 templates) ---
        ["Technology"] = new Func<DateTime, GameEvent>[]
        {
            t => MakeSector("Technology", t, "Major tech breakthrough sends innovation stocks soaring, analysts raise price targets across the sector", 0.040f, EventSeverity.Major, 150),
            t => MakeSector("Technology", t, "New data privacy regulation announced, compliance costs expected to rise for tech companies", -0.025f, EventSeverity.Moderate, 90),
            t => MakeSector("Technology", t, "Global chip shortage worsens as demand outpaces supply, semiconductor stocks under pressure", -0.022f, EventSeverity.Moderate, 100),
            t => MakeSector("Technology", t, "Antitrust investigation launched into major tech companies, market dominance questioned", -0.030f, EventSeverity.Major, 120),
            t => MakeSector("Technology", t, "AI regulation framework announced by government, tech sector faces new compliance requirements", -0.018f, EventSeverity.Moderate, 90),
        },

        // --- ENERGY (5 templates) ---
        ["Energy"] = new Func<DateTime, GameEvent>[]
        {
            t => MakeSector("Energy", t, $"Oil prices surge {5+_rng.Next(16)}% on OPEC production cut announcement, energy stocks rally", 0.055f, EventSeverity.Major, 120),
            t => MakeSector("Energy", t, "Major oil pipeline explosion disrupts supply, energy prices spike on infrastructure concerns", 0.025f, EventSeverity.Moderate, 90),
            t => MakeSector("Energy", t, "OPEC announces surprise production cut, crude oil futures jump on tighter supply outlook", 0.040f, EventSeverity.Major, 120),
            t => MakeSector("Energy", t, "Government announces massive renewable energy subsidy package, solar and wind stocks surge", 0.030f, EventSeverity.Moderate, 100),
            t => MakeSector("Energy", t, "Major oil spill reported in coastal waters, environmental disaster hits energy sector sentiment", -0.035f, EventSeverity.Major, 120),
        },

        // --- FINANCIALS (5 templates) ---
        ["Financials"] = new Func<DateTime, GameEvent>[]
        {
            t => MakeSector("Financials", t, "Bank stress test results released: all major banks pass with strong capital ratios", 0.025f, EventSeverity.Moderate, 90),
            t => MakeSector("Financials", t, "Bank stress test results reveal capital shortfalls at several institutions, financial sector under pressure", -0.040f, EventSeverity.Major, 120),
            t => MakeSector("Financials", t, "New financial regulation tightens capital requirements for banks and insurers", -0.022f, EventSeverity.Moderate, 100),
            t => MakeSector("Financials", t, "Crypto market crashes 30%, contagion fears spread to traditional financial sector", -0.018f, EventSeverity.Moderate, 90),
            t => MakeSector("Financials", t, "Interest rate environment shifts favorably, bank net interest margins expected to expand", 0.030f, EventSeverity.Moderate, 100),
        },

        // --- HEALTHCARE (6 templates) ---
        ["Healthcare"] = new Func<DateTime, GameEvent>[]
        {
            t => MakeSector("Healthcare", t, "FDA approves blockbuster drug for widespread use, pharmaceutical stocks surge on massive market potential", 0.035f, EventSeverity.Major, 120),
            t => MakeSector("Healthcare", t, "FDA rejects major drug application citing safety concerns, pharma sector sentiment sours", -0.030f, EventSeverity.Major, 100),
            t => MakeSector("Healthcare", t, "WHO declares pandemic outbreak, healthcare and biotech stocks surge on treatment demand", 0.050f, EventSeverity.Major, 150),
            t => MakeSector("Healthcare", t, "Healthcare regulation tightened: new pricing controls hit pharma and insurance companies", -0.035f, EventSeverity.Major, 120),
            t => MakeSector("Healthcare", t, "Biotech breakthrough announced: revolutionary gene therapy shows promise in Phase 3 trials", 0.040f, EventSeverity.Major, 120),
            t => MakeSector("Healthcare", t, "Drug pricing scandal erupts as company raises prices 500%, congressional investigation launched", -0.025f, EventSeverity.Moderate, 90),
        },

        // --- CONSUMER GOODS (6 templates) ---
        ["Consumer Goods"] = new Func<DateTime, GameEvent>[]
        {
            t => MakeSector("Consumer Goods", t, "Consumer confidence surges to multi-year high, retail and consumer goods stocks benefit", 0.030f, EventSeverity.Moderate, 100),
            t => MakeSector("Consumer Goods", t, "Consumer confidence drops sharply, spending outlook dims for consumer goods sector", -0.028f, EventSeverity.Moderate, 90),
            t => MakeSector("Consumer Goods", t, "Major product recall over safety concerns sends shockwaves through consumer goods sector", -0.025f, EventSeverity.Moderate, 100),
            t => MakeSector("Consumer Goods", t, "Global supply chain disruption hits consumer goods manufacturers, delivery delays expected", -0.020f, EventSeverity.Moderate, 90),
            t => MakeSector("Consumer Goods", t, "E-commerce growth report shows online retail sales up 25%, digital-first brands rally", 0.022f, EventSeverity.Moderate, 80),
            t => MakeSector("Consumer Goods", t, $"Holiday season sales beat expectations by {5+_rng.Next(10)}%, consumer goods sector celebrates strong Q4", 0.030f, EventSeverity.Moderate, 90),
        },

        // --- INDUSTRIALS (6 templates) ---
        ["Industrials"] = new Func<DateTime, GameEvent>[]
        {
            t => MakeSector("Industrials", t, $"Infrastructure spending bill worth ${200+_rng.Next(300)}B signed into law, construction and industrial stocks surge", 0.050f, EventSeverity.Major, 150),
            t => MakeSector("Industrials", t, "Manufacturing PMI surges above 55, industrial production at strongest pace in years", 0.025f, EventSeverity.Moderate, 90),
            t => MakeSector("Industrials", t, "Manufacturing PMI drops below 45, signaling contraction in industrial activity", -0.025f, EventSeverity.Moderate, 100),
            t => MakeSector("Industrials", t, "Major factory accident raises safety concerns, industrial regulation expected to tighten", -0.020f, EventSeverity.Moderate, 90),
            t => MakeSector("Industrials", t, "Automation and robotics breakthrough boosts industrial efficiency outlook", 0.025f, EventSeverity.Moderate, 100),
            t => MakeSector("Industrials", t, "New trade war tariffs imposed on industrial imports, export-heavy companies hit hardest", -0.030f, EventSeverity.Major, 120),
        },

        // --- MATERIALS (5 templates) ---
        ["Materials"] = new Func<DateTime, GameEvent>[]
        {
            t => MakeSector("Materials", t, $"Commodity prices surge {8+_rng.Next(12)}% on supply squeeze, mining and metals stocks rally", 0.045f, EventSeverity.Major, 120),
            t => MakeSector("Materials", t, "Commodity prices crash on demand fears, materials sector faces heavy selling pressure", -0.040f, EventSeverity.Major, 120),
            t => MakeSector("Materials", t, "Mining accident and environmental disaster trigger regulatory crackdown on materials sector", -0.030f, EventSeverity.Major, 100),
            t => MakeSector("Materials", t, "China demand surge lifts materials sector, copper and steel prices hit multi-year highs", 0.040f, EventSeverity.Major, 120),
            t => MakeSector("Materials", t, "Rare earth supply disruption sends specialty materials prices soaring, tech supply chain at risk", 0.030f, EventSeverity.Moderate, 100),
        },

        // --- REAL ESTATE (6 templates) ---
        ["Real Estate"] = new Func<DateTime, GameEvent>[]
        {
            t => MakeSector("Real Estate", t, "Interest rate cut boosts real estate sector, REIT dividends look increasingly attractive", 0.050f, EventSeverity.Major, 120),
            t => MakeSector("Real Estate", t, "Interest rate hike hits real estate sector hard, mortgage costs surge to multi-year highs", -0.050f, EventSeverity.Major, 120),
            t => MakeSector("Real Estate", t, "Housing starts surge to highest level in years, residential developers and homebuilders rally", 0.030f, EventSeverity.Moderate, 90),
            t => MakeSector("Real Estate", t, "Commercial vacancy rates rise sharply as office demand weakens, commercial REITs under pressure", -0.030f, EventSeverity.Moderate, 100),
            t => MakeSector("Real Estate", t, "Work-from-home trend accelerates, office REITs face structural demand decline", -0.035f, EventSeverity.Major, 120),
            t => MakeSector("Real Estate", t, "Mortgage rates hit record lows, homebuilders and residential REITs surge on demand outlook", 0.040f, EventSeverity.Major, 120),
        },

        // --- TELECOMMUNICATIONS (5 templates) ---
        ["Telecommunications"] = new Func<DateTime, GameEvent>[]
        {
            t => MakeSector("Telecommunications", t, "Next-generation 6G network expansion announced, telecom infrastructure stocks surge", 0.028f, EventSeverity.Moderate, 100),
            t => MakeSector("Telecommunications", t, "Major network outage affects millions of customers, telecom sector sentiment drops", -0.025f, EventSeverity.Moderate, 90),
            t => MakeSector("Telecommunications", t, "Streaming subscriber growth beats all expectations, media and telecom stocks rally", 0.030f, EventSeverity.Moderate, 90),
            t => MakeSector("Telecommunications", t, "Cable cutting accelerates as consumers shift to streaming, traditional TV operators under pressure", -0.020f, EventSeverity.Moderate, 80),
            t => MakeSector("Telecommunications", t, "Major cybersecurity breach at telecom provider exposes customer data, sector trust shaken", -0.028f, EventSeverity.Moderate, 100),
        },

        // --- UTILITIES (5 templates) ---
        ["Utilities"] = new Func<DateTime, GameEvent>[]
        {
            t => MakeSector("Utilities", t, "Regulator approves rate increases for utility companies, sector margins set to improve", 0.022f, EventSeverity.Minor, 80),
            t => MakeSector("Utilities", t, "Extreme weather drives record power demand, utility stocks benefit from higher consumption", 0.018f, EventSeverity.Minor, 70),
            t => MakeSector("Utilities", t, "Power grid failure raises infrastructure concerns, utility regulation expected to tighten", -0.025f, EventSeverity.Moderate, 100),
            t => MakeSector("Utilities", t, "Renewable energy mandate expanded, clean energy utilities rally while fossil fuel plants face headwinds", 0.025f, EventSeverity.Moderate, 90),
            t => MakeSector("Utilities", t, "Carbon tax legislation proposed, coal and gas utilities face significant cost increases", -0.030f, EventSeverity.Moderate, 100),
        },

        // --- LUXURY GOODS (5 templates) ---
        ["Luxury Goods"] = new Func<DateTime, GameEvent>[]
        {
            t => MakeSector("Luxury Goods", t, "Luxury spending boom as wealthy consumers drive record sales, premium brands surge", 0.040f, EventSeverity.Major, 100),
            t => MakeSector("Luxury Goods", t, "Economic downturn hits luxury spending hard, discretionary sector faces steep decline", -0.045f, EventSeverity.Major, 120),
            t => MakeSector("Luxury Goods", t, "China luxury demand surges as consumers return to premium brands", 0.040f, EventSeverity.Major, 110),
            t => MakeSector("Luxury Goods", t, "China announces crackdown on conspicuous consumption, luxury brands face demand headwinds", -0.040f, EventSeverity.Major, 110),
            t => MakeSector("Luxury Goods", t, "ESG and sustainability controversy hits luxury brands, consumer boycott threats emerge", -0.025f, EventSeverity.Moderate, 90),
        },

        // --- TRANSPORTATION (6 templates) ---
        ["Transportation"] = new Func<DateTime, GameEvent>[]
        {
            t => MakeSector("Transportation", t, $"Oil prices surge {10+_rng.Next(15)}%, fuel cost spike hits airlines and trucking companies", -0.035f, EventSeverity.Major, 120),
            t => MakeSector("Transportation", t, "Oil prices drop sharply, transportation sector celebrates lower fuel costs", 0.030f, EventSeverity.Moderate, 100),
            t => MakeSector("Transportation", t, "Major airline accident shakes investor confidence, aviation safety review ordered", -0.035f, EventSeverity.Major, 120),
            t => MakeSector("Transportation", t, "Autonomous vehicle breakthrough announced, transportation tech stocks rally on disruption potential", 0.035f, EventSeverity.Major, 120),
            t => MakeSector("Transportation", t, "Port workers strike disrupts global shipping, supply chain delays expected for weeks", -0.028f, EventSeverity.Moderate, 100),
            t => MakeSector("Transportation", t, "Tourism boom drives record travel demand, airlines and hospitality stocks surge", 0.030f, EventSeverity.Moderate, 100),
        },
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

    // --- 20 Company Templates with Personality data (Bible 8.2.3-8.2.4) ---
    // Uses CEO names, products, and rivals when available for headline variety
    private Func<Stock, DateTime, GameEvent>[] CompanyTemplates => new Func<Stock, DateTime, GameEvent>[]
    {
        (s, t) => MakeCompany(s, t, $"{s.Name} ({s.Symbol}) beats earnings estimates, revenue up {10+_rng.Next(15)}%", 0.05f + (float)_rng.NextDouble()*0.05f, EventSeverity.Major, 60),
        (s, t) => MakeCompany(s, t, $"{s.Name} ({s.Symbol}) misses earnings expectations, {Ceo(s)} lowers full-year guidance", -0.07f - (float)_rng.NextDouble()*0.08f, EventSeverity.Major, 60),
        (s, t) => MakeCompany(s, t, $"{s.Name} CEO {Ceo(s)} steps down unexpectedly, board launches search for replacement", -0.05f - (float)_rng.NextDouble()*0.05f, EventSeverity.Moderate, 90),
        (s, t) => MakeCompany(s, t, $"{s.Name} launches {Product(s)} to strong initial reviews, analysts raise estimates", 0.04f + (float)_rng.NextDouble()*0.04f, EventSeverity.Moderate, 90),
        (s, t) => MakeCompany(s, t, $"{s.Name} ({s.Symbol}) faces regulatory investigation into {s.Sector.ToLower()} practices", -0.05f - (float)_rng.NextDouble()*0.05f, EventSeverity.Moderate, 120),
        (s, t) => MakeCompany(s, t, $"{Ceo(s)} announces ${50+_rng.Next(200)}M stock buyback program at {s.Name}", 0.02f + (float)_rng.NextDouble()*0.02f, EventSeverity.Minor, 60),
        (s, t) => MakeCompany(s, t, $"{s.Name} ({s.Symbol}) wins major government contract worth ${200+_rng.Next(500)}M", 0.06f + (float)_rng.NextDouble()*0.04f, EventSeverity.Major, 90),
        (s, t) => MakeCompany(s, t, $"{s.Name} announces strategic partnership to expand {Product(s)} into new markets", 0.03f + (float)_rng.NextDouble()*0.03f, EventSeverity.Moderate, 70),
        (s, t) => MakeCompany(s, t, $"{Ceo(s)} warns of headwinds: {s.Name} ({s.Symbol}) issues profit warning for next quarter", -0.08f - (float)_rng.NextDouble()*0.05f, EventSeverity.Major, 60),
        (s, t) => MakeCompany(s, t, $"{s.Name} reports data breach affecting {1+_rng.Next(5)}M customers, {Ceo(s)} pledges security overhaul", -0.04f - (float)_rng.NextDouble()*0.04f, EventSeverity.Moderate, 90),
        (s, t) => MakeCompany(s, t, $"Analyst upgrades {s.Symbol} to Strong Buy: \"{Product(s)} is a game-changer\" — PT ${(int)(s.CurrentPrice*1.3m)}", 0.03f + (float)_rng.NextDouble()*0.02f, EventSeverity.Minor, 60),
        (s, t) => MakeCompany(s, t, $"Analyst downgrades {s.Symbol} to Sell, cites weakening demand for {Product(s)}", -0.03f - (float)_rng.NextDouble()*0.02f, EventSeverity.Minor, 60),
        (s, t) => MakeCompany(s, t, $"{s.Name} wins patent lawsuit against competitor, awarded ${10+_rng.Next(50)}M in damages", 0.03f + (float)_rng.NextDouble()*0.05f, EventSeverity.Moderate, 90),
        (s, t) => MakeCompany(s, t, $"{s.Name} recalls {Product(s)} due to safety concerns, investigation launched", -0.04f - (float)_rng.NextDouble()*0.04f, EventSeverity.Moderate, 90),
        (s, t) => MakeCompany(s, t, $"{s.Name} ({s.Symbol}) CFO resigns amid accounting concerns, shares tumble", -0.06f - (float)_rng.NextDouble()*0.06f, EventSeverity.Major, 120),
        (s, t) => MakeCompany(s, t, $"Insider buying: {Ceo(s)} purchases ${1+_rng.Next(5)}M in {s.Symbol} shares on the open market", 0.02f + (float)_rng.NextDouble()*0.02f, EventSeverity.Minor, 60),
        (s, t) => MakeCompany(s, t, $"{s.Name} secures ${100+_rng.Next(400)}M in new funding to scale {Product(s)}", 0.04f + (float)_rng.NextDouble()*0.03f, EventSeverity.Moderate, 70),
        (s, t) => MakeCompany(s, t, $"{Ceo(s)} announces {5+_rng.Next(15)}% workforce reduction at {s.Name} to cut costs", -0.03f - (float)_rng.NextDouble()*0.03f, EventSeverity.Moderate, 80),
        (s, t) => MakeCompany(s, t, $"{s.Name} ({s.Symbol}) reports record quarterly revenue driven by strong {Product(s)} demand", 0.06f + (float)_rng.NextDouble()*0.04f, EventSeverity.Major, 60),
        (s, t) => MakeCompany(s, t, $"Shareholders file class-action lawsuit against {s.Name}, alleging {Ceo(s)} misled investors", -0.04f - (float)_rng.NextDouble()*0.06f, EventSeverity.Moderate, 120),
    };

    // Helper: get CEO name from personality or fallback
    private static string Ceo(Stock s) => s.Personality?.CEOName ?? "CEO";
    // Helper: get product name from personality or fallback
    private static string Product(Stock s) => s.Personality?.FlagshipProduct ?? "flagship product";

    // =====================================================
    // JSON TEMPLATE RESOLUTION (Phase 1C)
    // =====================================================

    /// <summary>
    /// Convert a JSON EventTemplate into a GameEvent with resolved placeholders.
    /// </summary>
    private GameEvent? ResolveTemplate(EventTemplate tpl, Stock? stock, string? sector, DateTime gameTime)
    {
        if (tpl.Headlines.Count == 0) return null;

        // Pick random headline
        var headline = tpl.Headlines[_rng.Next(tpl.Headlines.Count)];

        // Resolve placeholders
        headline = ResolvePlaceholders(headline, stock, sector, gameTime);
        var summary = tpl.Summary != null ? ResolvePlaceholders(tpl.Summary, stock, sector, gameTime) : null;

        // Randomize price effect from [min, max] range
        var priceEffect = tpl.PriceEffect.Length == 2
            ? tpl.PriceEffect[0] + (float)_rng.NextDouble() * (tpl.PriceEffect[1] - tpl.PriceEffect[0])
            : tpl.PriceEffect.Length == 1 ? tpl.PriceEffect[0] : 0f;

        // Randomize duration from [min, max] range
        var duration = tpl.DurationMinutes.Length == 2
            ? _rng.Next(tpl.DurationMinutes[0], tpl.DurationMinutes[1] + 1)
            : tpl.DurationMinutes.Length == 1 ? tpl.DurationMinutes[0] : 60;

        // Parse severity and type
        var severity = Enum.TryParse<EventSeverity>(tpl.Severity, true, out var sev) ? sev : EventSeverity.Minor;
        var type = Enum.TryParse<EventType>(tpl.Type, true, out var tp) ? tp : EventType.Company;

        // Parse tier from category path
        var tier = tpl.Category switch
        {
            _ when tpl.Tags?.Contains("tier4") == true => EventTier.Tier4,
            _ when tpl.Tags?.Contains("tier3") == true => EventTier.Tier3,
            _ when tpl.Tags?.Contains("tier2") == true => EventTier.Tier2,
            _ => EventTier.Tier1,
        };

        // Get analyst quote if available
        string? analystName = null, analystFirm = null, analystQuote = null;
        if (_templates != null && _rng.NextDouble() < 0.4) // 40% chance of analyst quote
        {
            var analyst = _templates.GetRandomAnalyst(_rng, sector ?? stock?.Sector);
            if (analyst != null)
            {
                analystName = analyst.Name;
                analystFirm = analyst.Firm;
                analystQuote = GenerateAnalystQuote(tpl, priceEffect, stock, sector);
            }
        }

        var evt = new GameEvent
        {
            Type = type,
            Severity = severity,
            Sentiment = tpl.Sentiment,
            Headline = headline,
            AffectedSymbols = stock != null ? new List<string> { stock.Symbol } : new List<string>(),
            AffectedSectors = sector != null ? new List<string> { sector } :
                              stock != null ? new List<string> { stock.Sector } : new List<string>(),
            PriceEffect = priceEffect,
            VolatilityMultiplier = tpl.VolatilityMultiplier,
            VolumeMultiplier = tpl.VolumeMultiplier,
            DurationMinutes = duration,
            RemainingMinutes = duration,
            TriggeredAt = gameTime,
            // New Phase 1A fields
            Summary = summary,
            Tier = tier,
            Tags = tpl.Tags != null ? new List<string>(tpl.Tags) : null,
            AnalystName = analystName,
            AnalystFirm = analystFirm,
            AnalystQuote = analystQuote,
        };

        // Schedule follow-ups from template
        if (tpl.FollowUps != null)
        {
            foreach (var fu in tpl.FollowUps.Where(f => !string.IsNullOrEmpty(f.Headline)))
            {
                var fuHeadline = ResolvePlaceholders(fu.Headline, stock, sector, gameTime);
                var fuDelay = _rng.Next(fu.DelayMinDays, fu.DelayMaxDays + 1);
                var followUpEvt = new GameEvent
                {
                    Type = type,
                    Severity = fu.Sentiment > 0 ? EventSeverity.Moderate : EventSeverity.Minor,
                    Sentiment = fu.Sentiment,
                    Headline = fuHeadline,
                    AffectedSymbols = evt.AffectedSymbols,
                    AffectedSectors = evt.AffectedSectors,
                    PriceEffect = fu.PriceEffect,
                    VolatilityMultiplier = 1.2f,
                    VolumeMultiplier = 1.5f,
                    DurationMinutes = 60,
                    RemainingMinutes = 60,
                    TriggeredAt = gameTime.AddDays(fuDelay),
                };
                ScheduleFollowUp(followUpEvt, gameTime.AddDays(fuDelay), fu.Probability);
            }
        }

        return evt;
    }

    private string ResolvePlaceholders(string text, Stock? stock, string? sector, DateTime gameTime)
    {
        if (stock != null)
        {
            text = text.Replace("{company}", stock.Name)
                       .Replace("{symbol}", stock.Symbol)
                       .Replace("{ceo}", Ceo(stock))
                       .Replace("{product}", Product(stock));
        }
        else
        {
            // Fallback for sector/macro events that use company placeholders
            text = text.Replace("{company}", sector ?? "the market")
                       .Replace("{symbol}", "")
                       .Replace("{ceo}", "management")
                       .Replace("{product}", "products");
        }
        if (sector != null)
            text = text.Replace("{sector}", sector);

        // Generic placeholders with random values
        var quarter = (gameTime.Month - 1) / 3 + 1;
        text = text.Replace("{quarter}", quarter.ToString())
                   .Replace("{beat_pct}", (_rng.Next(5, 30)).ToString())
                   .Replace("{rev_growth}", (_rng.Next(3, 25)).ToString())
                   .Replace("{eps}", (_rng.NextDouble() * 3 + 0.5).ToString("F2"))
                   .Replace("{expected}", (_rng.NextDouble() * 2 + 0.3).ToString("F2"))
                   .Replace("{amount}", FormatAmount(_rng.Next(10, 500)))
                   .Replace("{shares}", FormatShares(_rng.Next(5000, 200000)))
                   .Replace("{price}", (_rng.NextDouble() * 150 + 10).ToString("F2"))
                   .Replace("{pct}", (_rng.Next(2, 30)).ToString())
                   .Replace("{count}", (_rng.Next(2, 8)).ToString())
                   .Replace("{days}", (_rng.Next(3, 30)).ToString())
                   .Replace("{years}", (_rng.Next(2, 10)).ToString())
                   .Replace("{department}", new[] { "Engineering", "Sales", "Operations", "R&D", "Marketing" }[_rng.Next(5)])
                   .Replace("{ownership_pct}", (_rng.NextDouble() * 8 + 2).ToString("F1"))
                   .Replace("{target_price}", (_rng.Next(50, 500)).ToString())
                   .Replace("{fund_return}", (_rng.Next(-20, 40)).ToString())
                   .Replace("{volume_mult}", (_rng.Next(2, 10)).ToString())
                   .Replace("{spread}", (_rng.NextDouble() * 2 + 0.5).ToString("F1"))
                   .Replace("{score}", (_rng.Next(10, 100)).ToString())
                   .Replace("{aum}", FormatAmount(_rng.Next(500, 50000)))
                   .Replace("{allocation}", (_rng.Next(5, 40)).ToString())
                   .Replace("{increase_pct}", (_rng.Next(5, 25)).ToString())
                   .Replace("{expected_pct}", (_rng.Next(2, 15)).ToString())
                   .Replace("{project}", new[] { "infrastructure modernization", "defense systems", "cloud migration", "AI platform", "smart grid", "5G network" }[_rng.Next(6)])
                   .Replace("{efficacy}", (_rng.Next(60, 98)).ToString())
                   .Replace("{rate}", (_rng.NextDouble() * 3 + 0.5).ToString("F2"))
                   .Replace("{margin}", (_rng.Next(5, 35)).ToString())
                   .Replace("{growth}", (_rng.Next(5, 30)).ToString())
                   .Replace("{decline}", (_rng.Next(5, 25)).ToString())
                   .Replace("{revenue}", FormatAmount(_rng.Next(50, 5000)))
                   .Replace("{market_cap}", FormatAmount(_rng.Next(500, 50000)))
                   .Replace("{employees}", (_rng.Next(500, 50000)).ToString("N0"))
                   .Replace("{target}", (_rng.Next(50, 500)).ToString());
        // Catch-all: remove any remaining unresolved {placeholder} patterns
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\{[a-z_]+\}", m =>
            m.Value switch
            {
                _ => (_rng.Next(5, 50)).ToString() // Generic numeric fallback
            });
        return text;
    }

    private static string FormatAmount(int millions) => millions >= 1000 ? $"{millions / 1000.0:F1}B" : $"{millions}M";
    private static string FormatShares(int shares) => shares >= 100000 ? $"{shares / 1000}K" : shares.ToString("N0");

    private string GenerateAnalystQuote(EventTemplate tpl, float priceEffect, Stock? stock, string? sector)
    {
        var quotes = priceEffect > 0
            ? new[]
            {
                "This represents a significant positive catalyst for the stock.",
                "We see further upside potential from current levels.",
                "The fundamentals remain strong and this confirms our bullish thesis.",
                "Investors should view this as a buying opportunity.",
            }
            : new[]
            {
                "We advise caution and recommend reducing exposure.",
                "This development raises concerns about near-term outlook.",
                "The risk-reward profile has deteriorated significantly.",
                "We expect continued headwinds in the coming quarters.",
            };
        return quotes[_rng.Next(quotes.Length)];
    }

    private GameEvent MakeCompany(Stock stock, DateTime t, string headline, float effect, EventSeverity severity, int duration)
    {
        var evt = new GameEvent
        {
            Type = EventType.Company, Severity = severity,
            Sentiment = effect > 0 ? 0.5f : -0.5f, Headline = headline,
            AffectedSymbols = new List<string> { stock.Symbol },
            AffectedSectors = new List<string> { stock.Sector }, // For peer contagion
            PriceEffect = effect,
            VolatilityMultiplier = severity == EventSeverity.Major ? 2.0f : 1.3f,
            VolumeMultiplier = severity == EventSeverity.Major ? 3.0f : 1.5f,
            DurationMinutes = duration, RemainingMinutes = duration, TriggeredAt = t,
        };

        // Issue 17: Rivalry gameplay effect — major events create inverse pressure on rival
        if (!string.IsNullOrEmpty(stock.Personality?.RivalSymbol)
            && severity >= EventSeverity.Major
            && Math.Abs(effect) > 0.03f)
        {
            var rivalEffect = -effect * 0.3f; // ~30% inverse
            var rivalHeadline = rivalEffect < 0
                ? $"{stock.Personality!.RivalSymbol} faces competitive pressure as rival {stock.Name} gains ground"
                : $"{stock.Personality!.RivalSymbol} benefits as competitor {stock.Name} stumbles";
            var rivalEvt = new GameEvent
            {
                Type = EventType.Company, Severity = EventSeverity.Moderate,
                Sentiment = rivalEffect > 0 ? 0.3f : -0.3f,
                Headline = rivalHeadline,
                AffectedSymbols = new List<string> { stock.Personality!.RivalSymbol },
                AffectedSectors = new List<string> { stock.Sector },
                PriceEffect = rivalEffect,
                VolatilityMultiplier = 1.3f, VolumeMultiplier = 1.5f,
                DurationMinutes = duration, RemainingMinutes = duration, TriggeredAt = t,
            };
            // Register rival event directly (will be picked up by ApplyActiveEvents)
            _activeEvents.Add(rivalEvt);
            _eventHistory.Add(rivalEvt);
            NewEventsThisTick.Add(rivalEvt);
            _log.Info("Rivalry event triggered", new { original = stock.Symbol, rival = stock.Personality!.RivalSymbol, rivalEffect });
        }

        return evt;
    }

    // =====================================================
    // EVENT CASCADE SYSTEM (Follow-up chains)
    // =====================================================

    /// <summary>
    /// Schedule a follow-up event to fire after a delay.
    /// Creates realistic multi-day narrative chains.
    /// </summary>
    private void ScheduleFollowUp(GameEvent followUp, DateTime fireAt, float probability = 1.0f)
    {
        _pendingFollowUps.Add(new PendingFollowUp { Event = followUp, FireAt = fireAt, Probability = probability });
    }

    private void FirePendingFollowUps(DateTime gameTime)
    {
        var fired = new List<PendingFollowUp>();
        foreach (var pf in _pendingFollowUps)
        {
            if (gameTime >= pf.FireAt)
            {
                if (_rng.NextDouble() < pf.Probability)
                    RegisterEvent(pf.Event);
                fired.Add(pf);
            }
        }
        foreach (var f in fired) _pendingFollowUps.Remove(f);
    }

    /// <summary>
    /// Called after registering a company event. Schedules realistic follow-ups.
    /// </summary>
    private void ScheduleCascades(Stock stock, GameEvent evt, DateTime gameTime)
    {
        var h = evt.Headline.ToLower();
        var sym = stock.Symbol;
        var name = stock.Name;
        var ceo = Ceo(stock);

        // Earnings Miss → Analyst Downgrade (2-3 days later, 70% chance)
        if (h.Contains("misses earnings") || h.Contains("profit warning"))
        {
            ScheduleFollowUp(new GameEvent
            {
                Type = EventType.Company, Severity = EventSeverity.Minor,
                Sentiment = -0.3f,
                Headline = $"Multiple analysts downgrade {sym} following disappointing results, average PT cut 15%",
                AffectedSymbols = new List<string> { sym },
                AffectedSectors = new List<string> { stock.Sector },
                PriceEffect = -0.02f, DurationMinutes = 60, RemainingMinutes = 60, TriggeredAt = gameTime.AddDays(2),
            }, gameTime.AddDays(2 + _rng.Next(2)), 0.70f);

            // → Insider Selling (5-7 days later, 40% chance)
            ScheduleFollowUp(new GameEvent
            {
                Type = EventType.Company, Severity = EventSeverity.Minor,
                Sentiment = -0.2f,
                Headline = $"SEC filing: {ceo} sells ${2+_rng.Next(8)}M in {sym} shares following earnings miss",
                AffectedSymbols = new List<string> { sym },
                AffectedSectors = new List<string> { stock.Sector },
                PriceEffect = -0.015f, DurationMinutes = 60, RemainingMinutes = 60, TriggeredAt = gameTime.AddDays(5),
            }, gameTime.AddDays(5 + _rng.Next(3)), 0.40f);
        }

        // Earnings Beat → Analyst Upgrade (1-3 days later, 60% chance)
        if (h.Contains("beats earnings") || h.Contains("record quarterly revenue"))
        {
            ScheduleFollowUp(new GameEvent
            {
                Type = EventType.Company, Severity = EventSeverity.Minor,
                Sentiment = 0.3f,
                Headline = $"Wall Street raises {sym} price targets after strong quarter, consensus now at ${(int)(stock.CurrentPrice * 1.25m)}",
                AffectedSymbols = new List<string> { sym },
                AffectedSectors = new List<string> { stock.Sector },
                PriceEffect = 0.02f, DurationMinutes = 60, RemainingMinutes = 60, TriggeredAt = gameTime.AddDays(2),
            }, gameTime.AddDays(1 + _rng.Next(3)), 0.60f);
        }

        // CEO Resignation → New CEO Hired (15-30 days later, 80% chance)
        if (h.Contains("ceo") && h.Contains("steps down"))
        {
            ScheduleFollowUp(new GameEvent
            {
                Type = EventType.Company, Severity = EventSeverity.Moderate,
                Sentiment = 0.4f,
                Headline = $"{name} names new CEO from outside the company, board calls hire \"transformative\"",
                AffectedSymbols = new List<string> { sym },
                AffectedSectors = new List<string> { stock.Sector },
                PriceEffect = 0.035f, DurationMinutes = 90, RemainingMinutes = 90, TriggeredAt = gameTime.AddDays(20),
            }, gameTime.AddDays(15 + _rng.Next(16)), 0.80f);
        }

        // Regulatory Investigation → Fine/Settlement (20-40 days later, 60% chance)
        if (h.Contains("regulatory investigation"))
        {
            var fine = 50 + _rng.Next(450);
            ScheduleFollowUp(new GameEvent
            {
                Type = EventType.Company, Severity = EventSeverity.Moderate,
                Sentiment = -0.3f,
                Headline = $"{name} agrees to pay ${fine}M settlement to resolve regulatory probe",
                AffectedSymbols = new List<string> { sym },
                AffectedSectors = new List<string> { stock.Sector },
                PriceEffect = -0.03f, DurationMinutes = 60, RemainingMinutes = 60, TriggeredAt = gameTime.AddDays(25),
            }, gameTime.AddDays(20 + _rng.Next(21)), 0.60f);
        }

        // Data Breach → Class Action Lawsuit (10-20 days later, 50% chance)
        if (h.Contains("data breach"))
        {
            ScheduleFollowUp(new GameEvent
            {
                Type = EventType.Company, Severity = EventSeverity.Moderate,
                Sentiment = -0.4f,
                Headline = $"Class-action lawsuit filed against {name} over data breach, seeking ${100+_rng.Next(400)}M in damages",
                AffectedSymbols = new List<string> { sym },
                AffectedSectors = new List<string> { stock.Sector },
                PriceEffect = -0.03f, DurationMinutes = 90, RemainingMinutes = 90, TriggeredAt = gameTime.AddDays(12),
            }, gameTime.AddDays(10 + _rng.Next(11)), 0.50f);
        }

        // Product Launch Success → Revenue Guidance Raise (5-10 days, 50%)
        if (h.Contains("launches") && h.Contains("strong"))
        {
            ScheduleFollowUp(new GameEvent
            {
                Type = EventType.Company, Severity = EventSeverity.Moderate,
                Sentiment = 0.4f,
                Headline = $"{ceo} raises full-year revenue guidance citing strong demand for {Product(stock)}",
                AffectedSymbols = new List<string> { sym },
                AffectedSectors = new List<string> { stock.Sector },
                PriceEffect = 0.03f, DurationMinutes = 60, RemainingMinutes = 60, TriggeredAt = gameTime.AddDays(7),
            }, gameTime.AddDays(5 + _rng.Next(6)), 0.50f);
        }
    }

    // =====================================================
    // GEOPOLITICAL EVENTS (Bible 8.2.5)
    // =====================================================

    private static readonly string[] Regions = { "Eastern Europe", "the Middle East", "the South China Sea", "the Korean Peninsula", "Central Asia", "North Africa", "the Taiwan Strait" };
    private static readonly string[] Countries = { "a major oil-producing nation", "a key manufacturing hub", "a G20 member state", "a major emerging market", "a strategic trade partner" };

    /// <summary>
    /// Generate geopolitical events. Called daily from GameLoop.
    /// ~2% daily chance = ~5 per year.
    /// </summary>
    public void TryGenerateGeopoliticalEvent(IReadOnlyList<Stock> stocks, DateTime gameTime)
    {
        if (_rng.NextDouble() > 0.02 * FrequencyMultiplier) return;

        var templates = new Func<DateTime, GameEvent>[]
        {
            // Military conflict
            t => {
                var region = Regions[_rng.Next(Regions.Length)];
                var evt = new GameEvent {
                    Type = EventType.Macro, Severity = EventSeverity.Major, Sentiment = -0.6f,
                    Headline = $"BREAKING: Tensions escalate in {region} as military forces mobilize, global markets tumble",
                    PriceEffect = -0.04f - (float)_rng.NextDouble() * 0.04f,
                    VolatilityMultiplier = 2.5f, VolumeMultiplier = 3.0f,
                    DurationMinutes = 180, RemainingMinutes = 180, TriggeredAt = t,
                };
                // Follow-up: ceasefire/de-escalation 5-15 days later (60%)
                ScheduleFollowUp(new GameEvent {
                    Type = EventType.Macro, Severity = EventSeverity.Moderate, Sentiment = 0.5f,
                    Headline = $"Diplomatic breakthrough in {region}: ceasefire agreement reached, markets rally on relief",
                    PriceEffect = 0.03f, VolatilityMultiplier = 1.3f, VolumeMultiplier = 2.0f,
                    DurationMinutes = 120, RemainingMinutes = 120, TriggeredAt = t.AddDays(10),
                }, t.AddDays(5 + _rng.Next(11)), 0.60f);
                return evt;
            },
            // Economic sanctions
            t => {
                var country = Countries[_rng.Next(Countries.Length)];
                return new GameEvent {
                    Type = EventType.Macro, Severity = EventSeverity.Moderate, Sentiment = -0.3f,
                    Headline = $"International sanctions imposed on {country}, targeting energy and financial sectors",
                    AffectedSectors = new List<string> { "Energy", "Financials" },
                    PriceEffect = -0.025f, VolatilityMultiplier = 1.5f, VolumeMultiplier = 2.0f,
                    DurationMinutes = 150, RemainingMinutes = 150, TriggeredAt = t,
                };
            },
            // Debt ceiling crisis
            t => {
                var evt = new GameEvent {
                    Type = EventType.Macro, Severity = EventSeverity.Major, Sentiment = -0.5f,
                    Headline = "Debt ceiling deadline approaches with no deal in sight, markets brace for volatility",
                    PriceEffect = -0.035f, VolatilityMultiplier = 2.0f, VolumeMultiplier = 2.5f,
                    DurationMinutes = 200, RemainingMinutes = 200, TriggeredAt = t,
                };
                // Follow-up: resolved (80%)
                ScheduleFollowUp(new GameEvent {
                    Type = EventType.Macro, Severity = EventSeverity.Moderate, Sentiment = 0.4f,
                    Headline = "Congress reaches last-minute debt ceiling deal, markets surge on relief rally",
                    PriceEffect = 0.025f, VolatilityMultiplier = 1.3f, VolumeMultiplier = 2.0f,
                    DurationMinutes = 90, RemainingMinutes = 90, TriggeredAt = t.AddDays(5),
                }, t.AddDays(3 + _rng.Next(5)), 0.80f);
                return evt;
            },
            // Natural disaster: earthquake
            t => new GameEvent {
                Type = EventType.Macro, Severity = EventSeverity.Moderate, Sentiment = -0.3f,
                Headline = $"Magnitude {6.0 + _rng.NextDouble() * 2.5:F1} earthquake strikes major metropolitan area, insurance stocks tumble",
                AffectedSectors = new List<string> { "Financials", "Real Estate" },
                PriceEffect = -0.02f, VolatilityMultiplier = 1.5f, VolumeMultiplier = 2.0f,
                DurationMinutes = 120, RemainingMinutes = 120, TriggeredAt = t,
            },
            // Natural disaster: hurricane
            t => new GameEvent {
                Type = EventType.Macro, Severity = EventSeverity.Moderate, Sentiment = -0.3f,
                Headline = $"Category {3 + _rng.Next(3)} hurricane makes landfall, {2 + _rng.Next(8)} million without power",
                AffectedSectors = new List<string> { "Energy", "Financials", "Utilities" },
                PriceEffect = -0.02f, VolatilityMultiplier = 1.5f, VolumeMultiplier = 1.8f,
                DurationMinutes = 120, RemainingMinutes = 120, TriggeredAt = t,
            },
            // Government shutdown
            t => {
                var evt = new GameEvent {
                    Type = EventType.Macro, Severity = EventSeverity.Minor, Sentiment = -0.2f,
                    Headline = $"Government shutdown enters day {1 + _rng.Next(10)} as budget negotiations stall",
                    PriceEffect = -0.012f, VolatilityMultiplier = 1.3f, VolumeMultiplier = 1.5f,
                    DurationMinutes = 120, RemainingMinutes = 120, TriggeredAt = t,
                };
                // Follow-up: shutdown ends (90%)
                ScheduleFollowUp(new GameEvent {
                    Type = EventType.Macro, Severity = EventSeverity.Minor, Sentiment = 0.2f,
                    Headline = "Government shutdown ends as Congress passes bipartisan funding bill",
                    PriceEffect = 0.008f, DurationMinutes = 60, RemainingMinutes = 60, TriggeredAt = t.AddDays(7),
                }, t.AddDays(5 + _rng.Next(10)), 0.90f);
                return evt;
            },
            // Election result
            t => _rng.NextDouble() < 0.5
                ? new GameEvent {
                    Type = EventType.Macro, Severity = EventSeverity.Moderate, Sentiment = 0.3f,
                    Headline = "Markets rally as business-friendly candidate wins key election, tax cuts expected",
                    PriceEffect = 0.025f, VolatilityMultiplier = 1.5f, VolumeMultiplier = 2.0f,
                    DurationMinutes = 120, RemainingMinutes = 120, TriggeredAt = t,
                }
                : new GameEvent {
                    Type = EventType.Macro, Severity = EventSeverity.Moderate, Sentiment = -0.2f,
                    Headline = "Markets dip as regulatory-focused candidate wins election, tech and healthcare under pressure",
                    AffectedSectors = new List<string> { "Technology", "Healthcare" },
                    PriceEffect = -0.02f, VolatilityMultiplier = 1.5f, VolumeMultiplier = 2.0f,
                    DurationMinutes = 120, RemainingMinutes = 120, TriggeredAt = t,
                },
            // Labor strike
            t => {
                var sectors = new[] { "Transportation", "Industrials", "Consumer Goods" };
                var sector = sectors[_rng.Next(sectors.Length)];
                var stock = stocks.FirstOrDefault(s => s.Sector == sector && !s.Traits.Contains("ETF"));
                var name = stock?.Name ?? $"major {sector.ToLower()} company";
                var sym = stock?.Symbol ?? "";
                var evt = new GameEvent {
                    Type = EventType.Company, Severity = EventSeverity.Moderate, Sentiment = -0.4f,
                    Headline = $"{(int)(5 + _rng.NextDouble() * 25)}K workers at {name} begin strike over wage dispute",
                    AffectedSymbols = sym != "" ? new List<string> { sym } : new List<string>(),
                    AffectedSectors = new List<string> { sector },
                    PriceEffect = -0.06f, VolatilityMultiplier = 1.5f, VolumeMultiplier = 2.0f,
                    DurationMinutes = 150, RemainingMinutes = 150, TriggeredAt = t,
                };
                // Follow-up: strike resolved (75%, 5-15 days)
                ScheduleFollowUp(new GameEvent {
                    Type = EventType.Company, Severity = EventSeverity.Minor, Sentiment = 0.3f,
                    Headline = $"Strike at {name} resolved after union reaches deal on wages and benefits",
                    AffectedSymbols = sym != "" ? new List<string> { sym } : new List<string>(),
                    AffectedSectors = new List<string> { sector },
                    PriceEffect = 0.04f, DurationMinutes = 60, RemainingMinutes = 60, TriggeredAt = t.AddDays(10),
                }, t.AddDays(5 + _rng.Next(11)), 0.75f);
                return evt;
            },
        };

        var template = templates[_rng.Next(templates.Length)];
        var geoEvt = template(gameTime);
        RegisterEvent(geoEvt);
        _log.Info("Geopolitical event", new { headline = geoEvt.Headline });
    }

    // =====================================================
    // SECONDARY OFFERINGS (Bible 8.2.4)
    // =====================================================

    /// <summary>
    /// Try to generate secondary offerings. Called daily.
    /// ~0.5% daily = ~1-3 per quarter across all stocks.
    /// Targets high-debt or fast-growing companies.
    /// </summary>
    public void TryGenerateSecondaryOffering(IReadOnlyList<Stock> stocks, DateTime gameTime)
    {
        if (_rng.NextDouble() > 0.005 * FrequencyMultiplier) return;

        // Find candidates: high debt or fast growth, not ETF, not too small
        var candidates = stocks.Where(s =>
            !s.Traits.Contains("ETF") &&
            s.MarketCap > 500_000_000m &&
            (s.DebtToEquity > 1.5m || s.RevenueGrowth > 0.20m)
        ).ToList();

        if (candidates.Count == 0) return;

        var stock = candidates[_rng.Next(candidates.Count)];
        var discountPct = 3 + _rng.Next(6); // 3-8% discount
        var offerPrice = Math.Round(stock.CurrentPrice * (1m - discountPct / 100m), 2);
        var sharesOffered = (long)(stock.SharesOutstanding * (0.05 + _rng.NextDouble() * 0.10)); // 5-15% dilution
        var raisedM = Math.Round(offerPrice * sharesOffered / 1_000_000m, 0);

        var evt = new GameEvent
        {
            Type = EventType.Company, Severity = EventSeverity.Moderate,
            Sentiment = -0.3f,
            Headline = $"{stock.Name} prices secondary offering at ${offerPrice:F2} ({discountPct}% discount), raising ${raisedM}M",
            AffectedSymbols = new List<string> { stock.Symbol },
            AffectedSectors = new List<string> { stock.Sector },
            PriceEffect = -0.05f - (float)_rng.NextDouble() * 0.05f, // -5 to -10%
            VolatilityMultiplier = 1.5f, VolumeMultiplier = 2.5f,
            DurationMinutes = 90, RemainingMinutes = 90, TriggeredAt = gameTime,
        };
        RegisterEvent(evt);

        // Actually dilute: increase shares outstanding
        stock.SharesOutstanding += sharesOffered;

        // Follow-up: "oversubscribed" positive signal (40%, 5-10 days later)
        ScheduleFollowUp(new GameEvent
        {
            Type = EventType.Company, Severity = EventSeverity.Minor,
            Sentiment = 0.3f,
            Headline = $"{stock.Name} secondary offering oversubscribed 3x, strong institutional demand signals confidence",
            AffectedSymbols = new List<string> { stock.Symbol },
            AffectedSectors = new List<string> { stock.Sector },
            PriceEffect = 0.025f, DurationMinutes = 60, RemainingMinutes = 60, TriggeredAt = gameTime.AddDays(7),
        }, gameTime.AddDays(5 + _rng.Next(6)), 0.40f);

        _log.Info("Secondary offering", new { symbol = stock.Symbol, raisedM, discountPct, sharesOffered });
    }

    // =====================================================
    // M&A / TENDER OFFER EVENTS (Bible 8.2.7)
    // =====================================================

    /// <summary>M&A events generated this tick (for frontend tender offer popups).</summary>
    public List<MAndAEvent> MAndAEventsThisTick { get; } = new();

    /// <summary>
    /// Bible 8.2.7: Try to generate M&A events. Called daily at market close.
    /// ~4% daily chance = roughly every 25 trading days.
    /// </summary>
    public void TryGenerateMAndA(IReadOnlyList<Stock> stocks, DateTime gameTime)
    {
        MAndAEventsThisTick.Clear();

        if (_rng.NextDouble() > 0.04 * FrequencyMultiplier) return;

        // Find a valid target: mid/small cap, not ETF
        var targets = stocks.Where(s =>
            !s.Traits.Contains("ETF") &&
            s.MarketCap > 500_000_000m &&
            s.MarketCap < 50_000_000_000m
        ).ToList();

        if (targets.Count < 2) return;

        var target = targets[_rng.Next(targets.Count)];

        // Find an acquirer: larger company, same or adjacent sector
        var acquirers = stocks.Where(s =>
            !s.Traits.Contains("ETF") &&
            s.Symbol != target.Symbol &&
            s.MarketCap > target.MarketCap * 2m
        ).ToList();

        if (acquirers.Count == 0) return;

        var acquirer = acquirers[_rng.Next(acquirers.Count)];

        // Premium: 20-40% over current price
        var premiumPct = 20 + _rng.Next(21);
        var offerPrice = Math.Round(target.CurrentPrice * (1m + premiumPct / 100m), 2);
        var dealValueB = Math.Round(offerPrice * target.SharesOutstanding / 1_000_000_000m, 1);

        // Target event: price jumps to near deal price
        var targetEffect = premiumPct / 100f * 0.85f; // Doesn't quite reach deal price (deal risk discount)
        var targetEvt = new GameEvent
        {
            Type = EventType.Company, Severity = EventSeverity.Major,
            Sentiment = 0.7f,
            Headline = $"{acquirer.Name} to acquire {target.Name} ({target.Symbol}) for ${offerPrice:F2}/share ({premiumPct}% premium). Deal valued at ${dealValueB:F1}B.",
            AffectedSymbols = new List<string> { target.Symbol },
            PriceEffect = targetEffect,
            VolatilityMultiplier = 0.5f, // Volatility drops — price anchored to deal
            VolumeMultiplier = 4.0f,
            DurationMinutes = 60, RemainingMinutes = 60, TriggeredAt = gameTime,
        };
        RegisterEvent(targetEvt);

        // Acquirer event: slight dip (overpaying concern)
        var acquirerEvt = new GameEvent
        {
            Type = EventType.Company, Severity = EventSeverity.Moderate,
            Sentiment = -0.2f,
            Headline = $"{acquirer.Name} ({acquirer.Symbol}) shares slip on ${dealValueB:F1}B acquisition of {target.Name}",
            AffectedSymbols = new List<string> { acquirer.Symbol },
            PriceEffect = -0.03f - (float)_rng.NextDouble() * 0.02f,
            VolatilityMultiplier = 1.3f, VolumeMultiplier = 2.0f,
            DurationMinutes = 60, RemainingMinutes = 60, TriggeredAt = gameTime,
        };
        RegisterEvent(acquirerEvt);

        // Track for tender offer popup
        MAndAEventsThisTick.Add(new MAndAEvent
        {
            TargetSymbol = target.Symbol,
            TargetName = target.Name,
            AcquirerSymbol = acquirer.Symbol,
            AcquirerName = acquirer.Name,
            OfferPrice = offerPrice,
            PremiumPercent = premiumPct,
            DealValueBillions = dealValueB,
            AnnouncedAt = gameTime,
        });

        _log.Info("M&A event generated", new
        {
            acquirer = acquirer.Symbol,
            target = target.Symbol,
            offerPrice,
            premiumPct,
            dealValueB,
        });
    }
}

/// <summary>
/// M&A event data for tender offer popup and tracking.
/// Bible 8.2.7: Tender Offer lifecycle.
/// </summary>
public class MAndAEvent
{
    public string TargetSymbol { get; set; } = "";
    public string TargetName { get; set; } = "";
    public string AcquirerSymbol { get; set; } = "";
    public string AcquirerName { get; set; } = "";
    public decimal OfferPrice { get; set; }
    public int PremiumPercent { get; set; }
    public decimal DealValueBillions { get; set; }
    public DateTime AnnouncedAt { get; set; }
}

/// <summary>
/// A pending follow-up event scheduled to fire at a future game time.
/// Used by the cascade system for realistic multi-day event chains.
/// </summary>
internal class PendingFollowUp
{
    public GameEvent Event { get; set; } = null!;
    public DateTime FireAt { get; set; }
    public float Probability { get; set; } = 1.0f;
}
