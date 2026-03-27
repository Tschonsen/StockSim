using System.Text.Json;
using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// Loads event templates and analyst profiles from JSON files in the data/ directory.
/// Templates are loaded once at startup and cached in memory.
/// </summary>
public class TemplateLoader
{
    private static readonly Logger _log = new("TemplateLoader");
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string _dataPath;

    /// <summary>All loaded event templates, keyed by tier then category.</summary>
    public Dictionary<EventTier, List<EventTemplate>> TemplatesByTier { get; } = new();

    /// <summary>All loaded analyst profiles.</summary>
    public List<AnalystProfile> Analysts { get; } = new();

    /// <summary>Total number of loaded templates across all tiers.</summary>
    public int TotalTemplates => TemplatesByTier.Values.Sum(l => l.Count);

    public TemplateLoader(string? dataPath = null)
    {
        _dataPath = dataPath ?? FindDataPath();
    }

    /// <summary>
    /// Load all templates and analysts from the data/ directory.
    /// </summary>
    public void LoadAll()
    {
        LoadTier(EventTier.Tier1, "tier1");
        LoadTier(EventTier.Tier2, "tier2");
        LoadTier(EventTier.Tier3, "tier3");
        LoadTier(EventTier.Tier4, "tier4");
        LoadAnalysts();

        _log.Info("Templates loaded", new
        {
            tier1 = TemplatesByTier.GetValueOrDefault(EventTier.Tier1)?.Count ?? 0,
            tier2 = TemplatesByTier.GetValueOrDefault(EventTier.Tier2)?.Count ?? 0,
            tier3 = TemplatesByTier.GetValueOrDefault(EventTier.Tier3)?.Count ?? 0,
            tier4 = TemplatesByTier.GetValueOrDefault(EventTier.Tier4)?.Count ?? 0,
            analysts = Analysts.Count,
            totalTemplates = TotalTemplates,
        });
    }

    /// <summary>
    /// Get templates for a specific tier, optionally filtered by category.
    /// </summary>
    public List<EventTemplate> GetTemplates(EventTier tier, string? category = null)
    {
        if (!TemplatesByTier.TryGetValue(tier, out var templates))
            return new List<EventTemplate>();

        if (category == null)
            return templates;

        return templates.Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>
    /// Get a random analyst, optionally filtered by sector specialty.
    /// </summary>
    public AnalystProfile? GetRandomAnalyst(Random rng, string? sector = null)
    {
        if (Analysts.Count == 0) return null;

        if (sector != null)
        {
            var sectorAnalysts = Analysts.Where(a =>
                a.Specialty.Equals(sector, StringComparison.OrdinalIgnoreCase) ||
                a.Specialty.Equals("Macro", StringComparison.OrdinalIgnoreCase)).ToList();

            if (sectorAnalysts.Count > 0)
                return sectorAnalysts[rng.Next(sectorAnalysts.Count)];
        }

        return Analysts[rng.Next(Analysts.Count)];
    }

    private void LoadTier(EventTier tier, string folderName)
    {
        var tierPath = Path.Combine(_dataPath, "events", folderName);
        if (!Directory.Exists(tierPath))
        {
            _log.Debug("Tier directory not found", new { tier = tier.ToString(), path = tierPath });
            return;
        }

        var templates = new List<EventTemplate>();
        foreach (var file in Directory.GetFiles(tierPath, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var fileTemplates = JsonSerializer.Deserialize<List<EventTemplate>>(json, _jsonOptions);
                if (fileTemplates != null)
                {
                    templates.AddRange(fileTemplates);
                    _log.Debug("Loaded template file", new { file = Path.GetFileName(file), count = fileTemplates.Count });
                }
            }
            catch (Exception ex)
            {
                _log.Warn("Failed to load template file", new { file = Path.GetFileName(file), error = ex.Message });
            }
        }

        TemplatesByTier[tier] = templates;
    }

    private void LoadAnalysts()
    {
        var analystsPath = Path.Combine(_dataPath, "analysts", "analysts.json");
        if (!File.Exists(analystsPath))
        {
            _log.Debug("Analysts file not found", new { path = analystsPath });
            return;
        }

        try
        {
            var json = File.ReadAllText(analystsPath);
            var analysts = JsonSerializer.Deserialize<List<AnalystProfile>>(json, _jsonOptions);
            if (analysts != null)
            {
                Analysts.AddRange(analysts);
            }
        }
        catch (Exception ex)
        {
            _log.Warn("Failed to load analysts", new { error = ex.Message });
        }
    }

    private static string FindDataPath()
    {
        // Try relative to executable first
        var exeDir = AppContext.BaseDirectory;
        var dataPath = Path.Combine(exeDir, "data");
        if (Directory.Exists(dataPath)) return dataPath;

        // Try relative to working directory
        dataPath = Path.Combine(Directory.GetCurrentDirectory(), "data");
        if (Directory.Exists(dataPath)) return dataPath;

        // Try navigating up from exe (development builds)
        var dir = new DirectoryInfo(exeDir);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "data");
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }

        _log.Warn("Data directory not found, using exe directory", new { exeDir });
        return Path.Combine(exeDir, "data");
    }
}
