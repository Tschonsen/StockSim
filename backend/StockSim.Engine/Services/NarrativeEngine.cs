using System.Text.Json;
using System.Text.Json.Serialization;
using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// Manages multi-phase event arcs that unfold over days/weeks.
/// Tier-3 arcs: sector crashes, commodity shocks, financial stress (2-3 phases, 2 paths).
/// Tier-4 arcs: black swans with 3-5 phases and 2-4 paths (future).
///
/// The NarrativeEngine:
/// 1. Loads arc templates from JSON
/// 2. Periodically activates new arcs based on conditions
/// 3. Advances active arcs through phases on daily ticks
/// 4. Resolves branch points based on market conditions
/// 5. Generates GameEvents for each phase
/// </summary>
public class NarrativeEngine
{
    private readonly Random _rng;
    private readonly Logger _log = new("NarrativeEngine");

    private readonly List<ArcTemplate> _arcTemplates = new();
    private readonly List<ActiveArc> _activeArcs = new();
    private readonly List<ActiveArc> _completedArcs = new();

    /// <summary>Events generated this tick by advancing arcs.</summary>
    public List<GameEvent> NewEventsThisTick { get; } = new();

    /// <summary>All currently active arcs.</summary>
    public IReadOnlyList<ActiveArc> ActiveArcs => _activeArcs.AsReadOnly();

    /// <summary>Number of loaded arc templates.</summary>
    public int TemplateCount => _arcTemplates.Count;

    private DateTime _lastArcCheck;

    public NarrativeEngine(int seed)
    {
        _rng = new Random(seed);
    }

    /// <summary>Load arc templates from JSON files in the tier3/tier4 directories.</summary>
    public void LoadArcs(string dataPath)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        foreach (var tier in new[] { "tier3", "tier4" })
        {
            var tierPath = Path.Combine(dataPath, "events", tier);
            if (!Directory.Exists(tierPath)) continue;

            foreach (var file in Directory.GetFiles(tierPath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var arcs = JsonSerializer.Deserialize<List<ArcTemplate>>(json, options);
                    if (arcs != null)
                    {
                        _arcTemplates.AddRange(arcs);
                        _log.Debug("Loaded arc file", new { file = Path.GetFileName(file), count = arcs.Count });
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn("Failed to load arc file", new { file = Path.GetFileName(file), error = ex.Message });
                }
            }
        }

        _log.Info("Arc templates loaded", new { count = _arcTemplates.Count });
    }

    /// <summary>
    /// Called daily (at market close). Checks for new arc activations and advances existing arcs.
    /// </summary>
    public void TickDay(IReadOnlyList<Stock> stocks, DateTime gameTime, MarketPhase phase)
    {
        NewEventsThisTick.Clear();

        // Advance existing arcs
        foreach (var arc in _activeArcs.ToList())
        {
            AdvanceArc(arc, stocks, gameTime, phase);
        }

        // Remove completed arcs
        _activeArcs.RemoveAll(a => a.Status == ArcStatus.Completed || a.Status == ArcStatus.Abandoned);

        // Try to activate new arcs (max 1 per check, ~5% daily chance)
        if (_activeArcs.Count < 2 && (gameTime - _lastArcCheck).TotalDays >= 1)
        {
            _lastArcCheck = gameTime;
            TryActivateArc(stocks, gameTime, phase);
        }
    }

    private void TryActivateArc(IReadOnlyList<Stock> stocks, DateTime gameTime, MarketPhase phase)
    {
        if (_arcTemplates.Count == 0) return;

        // ~5% daily chance of starting an arc (increased if no active arcs)
        var chance = _activeArcs.Count == 0 ? 0.08 : 0.03;
        if (_rng.NextDouble() > chance) return;

        // Min 30 days since last arc completion
        if (_completedArcs.Count > 0)
        {
            var lastCompleted = _completedArcs.Max(a => a.CompletedAt ?? DateTime.MinValue);
            if ((gameTime - lastCompleted).TotalDays < 20) return;
        }

        // Filter eligible arcs by conditions
        var eligible = _arcTemplates.Where(t =>
        {
            if (t.RequiresPhase != null && !MatchesPhase(t.RequiresPhase, phase)) return false;
            if (_activeArcs.Any(a => a.TemplateId == t.Id)) return false; // No duplicate arcs
            if (_completedArcs.Any(a => a.TemplateId == t.Id && (gameTime - (a.CompletedAt ?? DateTime.MinValue)).TotalDays < 60)) return false;
            return true;
        }).ToList();

        if (eligible.Count == 0) return;

        var template = eligible[_rng.Next(eligible.Count)];

        // Select target (sector or stock)
        string? targetSector = null;
        string? targetSymbol = null;

        if (template.TargetType == "sector")
        {
            var sectors = stocks.Select(s => s.Sector).Distinct().ToList();
            targetSector = sectors[_rng.Next(sectors.Count)];
        }
        else if (template.TargetType == "company")
        {
            var candidates = stocks.Where(s => !s.Traits.Contains("ETF")).ToList();
            if (candidates.Count > 0)
            {
                var stock = candidates[_rng.Next(candidates.Count)];
                targetSymbol = stock.Symbol;
                targetSector = stock.Sector;
            }
        }

        var activeArc = new ActiveArc
        {
            TemplateId = template.Id,
            Name = template.Name,
            Status = ArcStatus.Active,
            CurrentPhaseIndex = 0,
            StartedAt = gameTime,
            NextPhaseAt = gameTime, // Fire first phase immediately
            TargetSector = targetSector,
            TargetSymbol = targetSymbol,
        };

        _activeArcs.Add(activeArc);
        _log.Info("Arc activated", new { arc = template.Id, name = template.Name, sector = targetSector, symbol = targetSymbol });
    }

    private void AdvanceArc(ActiveArc arc, IReadOnlyList<Stock> stocks, DateTime gameTime, MarketPhase phase)
    {
        if (gameTime < arc.NextPhaseAt) return;

        var template = _arcTemplates.FirstOrDefault(t => t.Id == arc.TemplateId);
        if (template == null)
        {
            arc.Status = ArcStatus.Abandoned;
            return;
        }

        // Get phases for current path
        var applicablePhases = template.Phases
            .Where(p => p.Path == null || p.Path == arc.CurrentPath)
            .OrderBy(p => p.Index)
            .ToList();

        var currentPhase = applicablePhases.FirstOrDefault(p => p.Index == arc.CurrentPhaseIndex);
        if (currentPhase == null)
        {
            // No more phases → arc complete
            arc.Status = ArcStatus.Completed;
            arc.CompletedAt = gameTime;
            _completedArcs.Add(arc);
            _log.Info("Arc completed", new { arc = arc.TemplateId, path = arc.CurrentPath });
            return;
        }

        // Generate event for this phase
        var evt = CreatePhaseEvent(currentPhase, arc, template, stocks, gameTime);
        if (evt != null)
        {
            NewEventsThisTick.Add(evt);
        }

        // Check for branch point
        var branch = template.Branches.FirstOrDefault(b => b.AfterPhaseIndex == arc.CurrentPhaseIndex);
        if (branch != null && arc.CurrentPath == null)
        {
            arc.CurrentPath = ResolveBranch(branch, phase);
            _log.Info("Arc branched", new { arc = arc.TemplateId, path = arc.CurrentPath });
        }

        // Advance to next phase
        arc.CurrentPhaseIndex++;
        var nextPhase = applicablePhases.FirstOrDefault(p => p.Index == arc.CurrentPhaseIndex);
        if (nextPhase != null)
        {
            var delay = _rng.Next(nextPhase.DelayMinDays, nextPhase.DelayMaxDays + 1);
            arc.NextPhaseAt = gameTime.AddDays(delay);
        }
        else
        {
            // Check if there are path-specific phases
            var pathPhases = template.Phases
                .Where(p => p.Path == arc.CurrentPath && p.Index == arc.CurrentPhaseIndex)
                .ToList();
            if (pathPhases.Count > 0)
            {
                var delay = _rng.Next(pathPhases[0].DelayMinDays, pathPhases[0].DelayMaxDays + 1);
                arc.NextPhaseAt = gameTime.AddDays(delay);
            }
            else
            {
                arc.Status = ArcStatus.Completed;
                arc.CompletedAt = gameTime;
                _completedArcs.Add(arc);
                _log.Info("Arc completed", new { arc = arc.TemplateId, path = arc.CurrentPath });
            }
        }
    }

    private string ResolveBranch(ArcBranchTemplate branch, MarketPhase phase)
    {
        if (branch.Paths.Count == 0) return "A";

        // Calculate weighted probabilities
        var weights = branch.Paths.Select(p =>
        {
            var w = p.BaseWeight;
            if (p.FavoredInPhase != null && MatchesPhase(p.FavoredInPhase, phase))
                w *= (1 + p.PhaseBias);
            return (path: p.PathId, weight: w);
        }).ToList();

        var totalWeight = weights.Sum(w => w.weight);
        var roll = _rng.NextDouble() * totalWeight;

        float cumulative = 0;
        foreach (var (path, weight) in weights)
        {
            cumulative += weight;
            if (roll <= cumulative) return path;
        }

        return weights.Last().path;
    }

    private GameEvent? CreatePhaseEvent(ArcPhaseTemplate phase, ActiveArc arc, ArcTemplate template, IReadOnlyList<Stock> stocks, DateTime gameTime)
    {
        var headline = phase.Headlines.Count > 0
            ? phase.Headlines[_rng.Next(phase.Headlines.Count)]
            : $"{template.Name}: Phase {phase.Index + 1}";

        // Resolve placeholders
        headline = headline.Replace("{sector}", arc.TargetSector ?? "the market");
        if (arc.TargetSymbol != null)
        {
            var stock = stocks.FirstOrDefault(s => s.Symbol == arc.TargetSymbol);
            if (stock != null)
            {
                headline = headline.Replace("{company}", stock.Name)
                                   .Replace("{symbol}", stock.Symbol);
            }
        }

        var severity = Enum.TryParse<EventSeverity>(phase.Severity, true, out var sev) ? sev : EventSeverity.Major;
        var type = arc.TargetSymbol != null ? EventType.Company :
                   arc.TargetSector != null ? EventType.Sector : EventType.Macro;

        return new GameEvent
        {
            Type = type,
            Severity = severity,
            Sentiment = phase.Sentiment,
            Headline = headline,
            AffectedSymbols = arc.TargetSymbol != null ? new List<string> { arc.TargetSymbol } : new List<string>(),
            AffectedSectors = arc.TargetSector != null ? new List<string> { arc.TargetSector } : new List<string>(),
            PriceEffect = phase.PriceEffect,
            VolatilityMultiplier = phase.VolatilityMultiplier,
            VolumeMultiplier = phase.VolumeMultiplier,
            DurationMinutes = phase.DurationMinutes > 0 ? phase.DurationMinutes : 120,
            RemainingMinutes = phase.DurationMinutes > 0 ? phase.DurationMinutes : 120,
            TriggeredAt = gameTime,
            // Rich fields
            Summary = phase.Summary,
            Tier = template.Tier == 4 ? EventTier.Tier4 : EventTier.Tier3,
            Tags = template.Tags != null ? new List<string>(template.Tags) : null,
            ArcId = arc.TemplateId,
            ArcPhaseIndex = phase.Index,
            ArcPath = arc.CurrentPath,
        };
    }

    private static bool MatchesPhase(string required, MarketPhase phase) => required.ToLower() switch
    {
        "bull" => phase == MarketPhase.Bull,
        "bear" => phase == MarketPhase.Bear,
        "neutral" => phase == MarketPhase.Neutral,
        _ => true,
    };
}

/// <summary>Runtime state for an active arc instance.</summary>
public class ActiveArc
{
    public string TemplateId { get; set; } = "";
    public string Name { get; set; } = "";
    public ArcStatus Status { get; set; } = ArcStatus.Active;
    public int CurrentPhaseIndex { get; set; }
    public string? CurrentPath { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime NextPhaseAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? TargetSector { get; set; }
    public string? TargetSymbol { get; set; }
}

// === JSON-serializable arc template models ===

public class ArcTemplate
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("tier")] public int Tier { get; set; } = 3;
    [JsonPropertyName("tags")] public List<string>? Tags { get; set; }
    [JsonPropertyName("targetType")] public string TargetType { get; set; } = "sector"; // "sector", "company", "macro"
    [JsonPropertyName("requiresPhase")] public string? RequiresPhase { get; set; }
    [JsonPropertyName("phases")] public List<ArcPhaseTemplate> Phases { get; set; } = new();
    [JsonPropertyName("branches")] public List<ArcBranchTemplate> Branches { get; set; } = new();
}

public class ArcPhaseTemplate
{
    [JsonPropertyName("index")] public int Index { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("path")] public string? Path { get; set; }
    [JsonPropertyName("headlines")] public List<string> Headlines { get; set; } = new();
    [JsonPropertyName("summary")] public string? Summary { get; set; }
    [JsonPropertyName("sentiment")] public float Sentiment { get; set; }
    [JsonPropertyName("priceEffect")] public float PriceEffect { get; set; }
    [JsonPropertyName("volatilityMultiplier")] public float VolatilityMultiplier { get; set; } = 1.5f;
    [JsonPropertyName("volumeMultiplier")] public float VolumeMultiplier { get; set; } = 2.0f;
    [JsonPropertyName("durationMinutes")] public int DurationMinutes { get; set; } = 120;
    [JsonPropertyName("severity")] public string Severity { get; set; } = "Major";
    [JsonPropertyName("delayMinDays")] public int DelayMinDays { get; set; } = 1;
    [JsonPropertyName("delayMaxDays")] public int DelayMaxDays { get; set; } = 5;
}

public class ArcBranchTemplate
{
    [JsonPropertyName("afterPhaseIndex")] public int AfterPhaseIndex { get; set; }
    [JsonPropertyName("paths")] public List<ArcPathTemplate> Paths { get; set; } = new();
}

public class ArcPathTemplate
{
    [JsonPropertyName("pathId")] public string PathId { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("baseWeight")] public float BaseWeight { get; set; } = 1.0f;
    [JsonPropertyName("favoredInPhase")] public string? FavoredInPhase { get; set; }
    [JsonPropertyName("phaseBias")] public float PhaseBias { get; set; } = 0.3f;
}
