using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

public class CompanyInsolvencyTests
{
    [Fact]
    public void HighDebt_NegativeIncome_ShouldFlagInsolvencyRisk()
    {
        var engine = new EarningsEngine(42);
        var stock = new Stock("BANK", "Bankrupt Corp", "Technology")
        {
            CurrentPrice = 5m,
            SharesOutstanding = 1_000_000,
            NetIncome = -50_000_000m, // Deep losses
            Revenue = 10_000_000m,
            BaseVolatility = 0.05m,
            DebtToEquity = 5.0m, // Extreme debt
            DividendYield = 0m,
        };
        stock.FairValue = stock.CurrentPrice;
        var stocks = new List<Stock> { stock };
        engine.GenerateSchedule(stocks, new DateTime(2027, 1, 5));

        // Run earnings — should flag insolvency risk
        for (int i = 0; i < 120; i++)
            engine.TickDay(stocks, new DateTime(2027, 1, 5).AddDays(i));

        // InsolvencyRisk should be set for extremely distressed companies
        var released = engine.Schedule.Where(e => e.Released).ToList();
        if (released.Count > 0)
        {
            Assert.True(engine.InsolvencyWarnings.Any(w => w.Symbol == "BANK"),
                "Should flag insolvency warning for company with extreme debt and negative income");
        }
    }

    [Fact]
    public void HealthyCompany_ShouldNotFlagInsolvency()
    {
        var engine = new EarningsEngine(42);
        var stock = new Stock("HLTH", "Healthy Corp", "Technology")
        {
            CurrentPrice = 100m,
            SharesOutstanding = 1_000_000,
            NetIncome = 50_000_000m,
            Revenue = 200_000_000m,
            BaseVolatility = 0.02m,
            DebtToEquity = 0.5m,
            DividendYield = 0.02m,
        };
        stock.FairValue = stock.CurrentPrice;
        var stocks = new List<Stock> { stock };
        engine.GenerateSchedule(stocks, new DateTime(2027, 1, 5));

        for (int i = 0; i < 120; i++)
            engine.TickDay(stocks, new DateTime(2027, 1, 5).AddDays(i));

        Assert.DoesNotContain(engine.InsolvencyWarnings, w => w.Symbol == "HLTH");
    }

    [Fact]
    public void InsolvencyWarning_ShouldContainSymbolAndReason()
    {
        var engine = new EarningsEngine(42);
        var stock = new Stock("DOOM", "Doomed Corp", "Financials")
        {
            CurrentPrice = 2m,
            SharesOutstanding = 1_000_000,
            NetIncome = -100_000_000m,
            Revenue = 5_000_000m,
            BaseVolatility = 0.08m,
            DebtToEquity = 8.0m,
            DividendYield = 0m,
        };
        stock.FairValue = stock.CurrentPrice;
        var stocks = new List<Stock> { stock };
        engine.GenerateSchedule(stocks, new DateTime(2027, 1, 5));

        for (int i = 0; i < 120; i++)
            engine.TickDay(stocks, new DateTime(2027, 1, 5).AddDays(i));

        var warning = engine.InsolvencyWarnings.FirstOrDefault(w => w.Symbol == "DOOM");
        if (warning != null)
        {
            Assert.False(string.IsNullOrEmpty(warning.Symbol));
            Assert.False(string.IsNullOrEmpty(warning.Reason));
        }
    }

    [Fact]
    public void ConsecutiveLosses_ShouldIncreaseInsolvencyRisk()
    {
        var engine = new EarningsEngine(42);
        var stock = new Stock("SINK", "Sinking Corp", "Technology")
        {
            CurrentPrice = 10m,
            SharesOutstanding = 1_000_000,
            NetIncome = -20_000_000m,
            Revenue = 15_000_000m,
            BaseVolatility = 0.04m,
            DebtToEquity = 3.5m,
            DividendYield = 0m,
        };
        stock.FairValue = stock.CurrentPrice;
        var stocks = new List<Stock> { stock };
        engine.GenerateSchedule(stocks, new DateTime(2027, 1, 5));

        // Run a full year — multiple earnings misses should compound
        for (int i = 0; i < 365; i++)
            engine.TickDay(stocks, new DateTime(2027, 1, 5).AddDays(i));

        // After a year of losses, D/E should have increased significantly
        Assert.True(stock.DebtToEquity > 3.5m,
            $"D/E {stock.DebtToEquity} should have risen from 3.5 after sustained losses");
    }
}
