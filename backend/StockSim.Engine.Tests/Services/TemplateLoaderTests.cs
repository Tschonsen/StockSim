using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

public class TemplateLoaderTests
{
    private static string FindDataPath()
    {
        // Navigate from test output to source data directory
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "StockSim.Engine", "data");
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        // Fallback: try build output
        return Path.Combine(AppContext.BaseDirectory, "data");
    }

    [Fact]
    public void LoadAll_ShouldLoadTemplatesFromDataDirectory()
    {
        var loader = new TemplateLoader(FindDataPath());
        loader.LoadAll();

        Assert.True(loader.TotalTemplates > 0, "Should load at least some templates");
    }

    [Fact]
    public void LoadAll_ShouldLoadTier1Templates()
    {
        var loader = new TemplateLoader(FindDataPath());
        loader.LoadAll();

        var tier1 = loader.GetTemplates(EventTier.Tier1);
        Assert.True(tier1.Count > 0, $"Should have Tier1 templates, got {tier1.Count}");
    }

    [Fact]
    public void LoadAll_ShouldLoadTier2Templates()
    {
        var loader = new TemplateLoader(FindDataPath());
        loader.LoadAll();

        var tier2 = loader.GetTemplates(EventTier.Tier2);
        Assert.True(tier2.Count > 0, $"Should have Tier2 templates, got {tier2.Count}");
    }

    [Fact]
    public void LoadAll_ShouldLoadAnalysts()
    {
        var loader = new TemplateLoader(FindDataPath());
        loader.LoadAll();

        Assert.True(loader.Analysts.Count >= 200, $"Should have 200+ analysts, got {loader.Analysts.Count}");
    }

    [Fact]
    public void Templates_ShouldHaveValidFields()
    {
        var loader = new TemplateLoader(FindDataPath());
        loader.LoadAll();

        foreach (var (tier, templates) in loader.TemplatesByTier)
        {
            foreach (var t in templates)
            {
                Assert.False(string.IsNullOrEmpty(t.Id), $"Template ID is empty in {tier}");
                Assert.True(t.Headlines.Count > 0, $"Template {t.Id} has no headlines");
                Assert.True(t.PriceEffect.Length == 2, $"Template {t.Id} needs [min, max] price effect");
                Assert.True(t.DurationMinutes.Length == 2, $"Template {t.Id} needs [min, max] duration");
            }
        }
    }

    [Fact]
    public void GetTemplates_WithCategory_ShouldFilterCorrectly()
    {
        var loader = new TemplateLoader(FindDataPath());
        loader.LoadAll();

        var earnings = loader.GetTemplates(EventTier.Tier1, "earnings");
        if (earnings.Count > 0)
        {
            Assert.All(earnings, t => Assert.Equal("earnings", t.Category));
        }
    }

    [Fact]
    public void GetRandomAnalyst_ShouldReturnAnalyst()
    {
        var loader = new TemplateLoader(FindDataPath());
        loader.LoadAll();

        var rng = new Random(42);
        var analyst = loader.GetRandomAnalyst(rng);

        Assert.NotNull(analyst);
        Assert.False(string.IsNullOrEmpty(analyst.Name));
        Assert.False(string.IsNullOrEmpty(analyst.Firm));
    }

    [Fact]
    public void GetRandomAnalyst_WithSector_ShouldPreferSpecialist()
    {
        var loader = new TemplateLoader(FindDataPath());
        loader.LoadAll();

        var rng = new Random(42);
        // Run multiple times to check if sector filtering works
        int sectorMatch = 0;
        for (int i = 0; i < 20; i++)
        {
            var analyst = loader.GetRandomAnalyst(rng, "Technology");
            if (analyst != null && (analyst.Specialty == "Technology" || analyst.Specialty == "Macro"))
                sectorMatch++;
        }

        Assert.True(sectorMatch > 10, $"Sector filter should bias toward specialists, got {sectorMatch}/20 matches");
    }

    [Fact]
    public void Analysts_ShouldHaveValidFields()
    {
        var loader = new TemplateLoader(FindDataPath());
        loader.LoadAll();

        foreach (var a in loader.Analysts)
        {
            Assert.False(string.IsNullOrEmpty(a.Name), "Analyst name is empty");
            Assert.False(string.IsNullOrEmpty(a.Firm), "Analyst firm is empty");
            Assert.False(string.IsNullOrEmpty(a.Specialty), "Analyst specialty is empty");
        }
    }

    [Fact]
    public void LoadAll_WithInvalidPath_ShouldNotThrow()
    {
        var loader = new TemplateLoader("/nonexistent/path");
        loader.LoadAll(); // Should not throw

        Assert.Equal(0, loader.TotalTemplates);
        Assert.Empty(loader.Analysts);
    }
}
