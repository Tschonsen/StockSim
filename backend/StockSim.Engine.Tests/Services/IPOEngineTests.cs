using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

public class IPOEngineTests
{
    private readonly IPOEngine _engine = new(seed: 42);

    [Fact]
    public void ShouldAnnounceIPO_AfterEnoughDays()
    {
        var stocks = new List<Stock>();
        var portfolio = new Portfolio(50_000m);

        // Run enough days to trigger an IPO announcement (30-60 days)
        for (int i = 0; i < 70; i++)
        {
            var date = new DateTime(2027, 1, 6, 9, 31, 0).AddDays(i);
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) continue;
            _engine.TickDay(stocks, portfolio, date);
        }

        Assert.True(_engine.PendingIPOs.Count > 0 || stocks.Count > 0,
            "Should have announced or listed at least one IPO");
    }

    [Fact]
    public void IPO_ShouldCreateNewStock()
    {
        var stocks = new List<Stock>();
        var portfolio = new Portfolio(50_000m);
        var baseDate = new DateTime(2027, 1, 6, 9, 31, 0);

        // Run until we get a listed IPO
        for (int i = 0; i < 100; i++)
        {
            var date = baseDate.AddDays(i);
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) continue;
            _engine.TickDay(stocks, portfolio, date);
        }

        if (stocks.Count > 0)
        {
            var newStock = stocks[0];
            Assert.True(newStock.CurrentPrice > 0, "IPO stock should have positive price");
            Assert.False(string.IsNullOrEmpty(newStock.Symbol), "IPO stock should have symbol");
            Assert.False(string.IsNullOrEmpty(newStock.Name), "IPO stock should have name");
        }
    }

    [Fact]
    public void IPO_ShouldHaveOpeningSpike()
    {
        var stocks = new List<Stock>();
        var portfolio = new Portfolio(50_000m);
        var baseDate = new DateTime(2027, 1, 6, 9, 31, 0);

        for (int i = 0; i < 100; i++)
        {
            var date = baseDate.AddDays(i);
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) continue;
            _engine.TickDay(stocks, portfolio, date);
        }

        // IPO stocks should open above expected price (10-50% spike)
        foreach (var s in stocks)
        {
            Assert.True(s.CurrentPrice >= s.FairValue,
                $"{s.Symbol} IPO price {s.CurrentPrice} should be >= expected {s.FairValue}");
        }
    }

    [Fact]
    public void NewsThisTick_ShouldContainIPOAnnouncements()
    {
        var stocks = new List<Stock>();
        var portfolio = new Portfolio(50_000m);
        var allNews = new List<string>();

        for (int i = 0; i < 100; i++)
        {
            var date = new DateTime(2027, 1, 6, 9, 31, 0).AddDays(i);
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) continue;
            _engine.TickDay(stocks, portfolio, date);
            allNews.AddRange(_engine.NewsThisTick);
        }

        Assert.True(allNews.Any(n => n.Contains("IPO") || n.Contains("files for")),
            "Should have IPO-related news");
    }

    [Fact]
    public void SameSeed_ShouldProduceSameIPOs()
    {
        var e1 = new IPOEngine(seed: 99);
        var e2 = new IPOEngine(seed: 99);
        var s1 = new List<Stock>();
        var s2 = new List<Stock>();
        var p1 = new Portfolio(50_000m);
        var p2 = new Portfolio(50_000m);

        for (int i = 0; i < 80; i++)
        {
            var date = new DateTime(2027, 1, 6, 9, 31, 0).AddDays(i);
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) continue;
            e1.TickDay(s1, p1, date);
            e2.TickDay(s2, p2, date);
        }

        Assert.Equal(s1.Count, s2.Count);
        for (int i = 0; i < s1.Count; i++)
            Assert.Equal(s1[i].Symbol, s2[i].Symbol);
    }
}
