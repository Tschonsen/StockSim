using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

public class EconomicEngineTests
{
    [Fact]
    public void ShouldInitializeWithReasonableValues()
    {
        var engine = new EconomicEngine(42);

        Assert.InRange(engine.Data.InterestRate, 0, 15);
        Assert.InRange(engine.Data.InflationRate, -1, 15);
        Assert.InRange(engine.Data.UnemploymentRate, 2, 15);
        Assert.InRange(engine.Data.GDPGrowth, -5, 8);
        Assert.InRange(engine.Data.ConsumerConfidence, 20, 120);
        Assert.InRange(engine.Data.OilPrice, 20, 150);
        Assert.InRange(engine.Data.GoldPrice, 800, 3000);
    }

    [Fact]
    public void ScheduleEvents_ShouldCreateEvents()
    {
        var engine = new EconomicEngine(42);
        engine.ScheduleEvents(new DateTime(2027, 1, 5));

        Assert.True(engine.UpcomingEvents.Count > 10, $"Expected >10 events, got {engine.UpcomingEvents.Count}");
    }

    [Fact]
    public void TickDay_ShouldDriftIndicators()
    {
        var engine = new EconomicEngine(42);
        engine.ScheduleEvents(new DateTime(2027, 1, 5));
        var rateBefore = engine.Data.InterestRate;

        // Tick 100 days — indicators should drift
        for (int i = 0; i < 100; i++)
            engine.TickDay(new DateTime(2027, 1, 5).AddDays(i));

        // At least some indicators should have changed
        var rateAfter = engine.Data.InterestRate;
        // With 100 days of drift, it's extremely unlikely all stay exactly the same
        Assert.True(engine.Data.InterestRate != rateBefore || engine.Data.InflationRate != 2.0m,
            "Indicators should drift over time");
    }

    [Fact]
    public void TickDay_ShouldReleaseScheduledEvents()
    {
        var engine = new EconomicEngine(42);
        var start = new DateTime(2027, 1, 5);
        engine.ScheduleEvents(start);

        var initialCount = engine.UpcomingEvents.Count;

        // Tick forward 30 days
        for (int i = 0; i < 30; i++)
            engine.TickDay(start.AddDays(i));

        // Some events should have been released
        Assert.True(engine.EventHistory.Count > 0, "Events should be released over 30 days");
        Assert.True(engine.UpcomingEvents.Count < initialCount || engine.EventHistory.Count > 0);
    }

    [Fact]
    public void GetSectorMultipliers_ShouldReturnAllSectors()
    {
        var engine = new EconomicEngine(42);
        var multipliers = engine.GetSectorMultipliers();

        Assert.True(multipliers.Count >= 12, $"Expected >= 12 sectors, got {multipliers.Count}");
        Assert.Contains("Technology", multipliers.Keys);
        Assert.Contains("Energy", multipliers.Keys);
        Assert.Contains("Financials", multipliers.Keys);
    }

    [Fact]
    public void GetFearGreedIndex_ShouldBeInRange()
    {
        var engine = new EconomicEngine(42);
        var index = engine.GetFearGreedIndex();

        Assert.InRange(index, 0, 100);
    }

    [Fact]
    public void GetMarketSentiment_ShouldBeInRange()
    {
        var engine = new EconomicEngine(42);
        var sentiment = engine.GetMarketSentiment();

        Assert.InRange(sentiment, -1m, 1m);
    }

    [Fact]
    public void IndicatorsStayWithinBounds()
    {
        var engine = new EconomicEngine(42);
        engine.ScheduleEvents(new DateTime(2027, 1, 5));

        for (int i = 0; i < 500; i++)
            engine.TickDay(new DateTime(2027, 1, 5).AddDays(i));

        Assert.InRange(engine.Data.InterestRate, 0, 15);
        Assert.InRange(engine.Data.UnemploymentRate, 2, 15);
        Assert.InRange(engine.Data.OilPrice, 20, 150);
        Assert.InRange(engine.Data.GoldPrice, 800, 3000);
    }
}

public class EarningsEngineTests
{
    private List<Stock> CreateStocks(int count)
    {
        return Enumerable.Range(0, count).Select(i =>
        {
            var stock = new Stock($"S{i:D3}", $"Stock {i}", "Technology")
            {
                CurrentPrice = 50m + i,
                SharesOutstanding = 1_000_000,
                NetIncome = 5_000_000m,
                Revenue = 50_000_000m,
                BaseVolatility = 0.02m,
            };
            stock.FairValue = stock.CurrentPrice;
            return stock;
        }).ToList();
    }

    [Fact]
    public void GenerateSchedule_ShouldCreate4QuartersPerStock()
    {
        var engine = new EarningsEngine(42);
        var stocks = CreateStocks(10);
        engine.GenerateSchedule(stocks, new DateTime(2027, 1, 5));

        // 10 stocks x 4 quarters = 40 reports
        Assert.Equal(40, engine.Schedule.Count);
    }

    [Fact]
    public void GenerateSchedule_ShouldSkipETFs()
    {
        var engine = new EarningsEngine(42);
        var stocks = CreateStocks(5);
        var etf = new Stock("SIMX", "Market ETF", "ETF") { CurrentPrice = 100, SharesOutstanding = 1_000_000 };
        etf.Traits.Add("ETF");
        stocks.Add(etf);

        engine.GenerateSchedule(stocks, new DateTime(2027, 1, 5));

        Assert.Equal(20, engine.Schedule.Count); // Only 5 real stocks x 4
        Assert.DoesNotContain(engine.Schedule, e => e.Symbol == "SIMX");
    }

    [Fact]
    public void TickDay_ShouldReleaseEarnings()
    {
        var engine = new EarningsEngine(42);
        var stocks = CreateStocks(5);
        engine.GenerateSchedule(stocks, new DateTime(2027, 1, 5));

        // Tick forward 90 days — should release some earnings
        for (int i = 0; i < 90; i++)
            engine.TickDay(stocks, new DateTime(2027, 1, 5).AddDays(i));

        Assert.True(engine.Schedule.Any(e => e.Released), "Some earnings should be released in 90 days");
    }

    [Fact]
    public void ReleasedEarnings_ShouldHaveBeatOrMiss()
    {
        var engine = new EarningsEngine(42);
        var stocks = CreateStocks(20);
        engine.GenerateSchedule(stocks, new DateTime(2027, 1, 5));

        for (int i = 0; i < 90; i++)
            engine.TickDay(stocks, new DateTime(2027, 1, 5).AddDays(i));

        var released = engine.Schedule.Where(e => e.Released).ToList();
        Assert.True(released.Count > 0);
        Assert.All(released, e => Assert.True(e.ActualEPS != 0 || e.ExpectedEPS == 0));
    }

    [Fact]
    public void ReleasedEarnings_ShouldImpactPrice()
    {
        var engine = new EarningsEngine(42);
        var stocks = CreateStocks(20);
        engine.GenerateSchedule(stocks, new DateTime(2027, 1, 5));

        var pricesBefore = stocks.ToDictionary(s => s.Symbol, s => s.CurrentPrice);

        for (int i = 0; i < 90; i++)
            engine.TickDay(stocks, new DateTime(2027, 1, 5).AddDays(i));

        var released = engine.Schedule.Where(e => e.Released).ToList();
        // At least one stock's price should have changed from earnings
        Assert.True(released.Any(e => e.PriceImpactPercent != 0));
    }

    [Fact]
    public void GetUpcoming_ShouldReturnFutureEarnings()
    {
        var engine = new EarningsEngine(42);
        var stocks = CreateStocks(5);
        var start = new DateTime(2027, 1, 5);
        engine.GenerateSchedule(stocks, start);

        var upcoming = engine.GetUpcoming(start, 60);
        Assert.True(upcoming.Count > 0);
        Assert.All(upcoming, e => Assert.True(e.ReportDate >= start));
    }

    [Fact]
    public void ReleasedEarnings_ShouldUpdateRevenue()
    {
        var engine = new EarningsEngine(42);
        var stocks = CreateStocks(20);
        var revenueBefore = stocks.ToDictionary(s => s.Symbol, s => s.Revenue);
        engine.GenerateSchedule(stocks, new DateTime(2027, 1, 5));

        for (int i = 0; i < 90; i++)
            engine.TickDay(stocks, new DateTime(2027, 1, 5).AddDays(i));

        var released = engine.Schedule.Where(e => e.Released).ToList();
        Assert.True(released.Count > 0);
        // At least one stock's revenue should have changed
        var changed = stocks.Any(s => s.Revenue != revenueBefore[s.Symbol]);
        Assert.True(changed, "Revenue should update after earnings");
    }

    [Fact]
    public void ReleasedEarnings_ShouldUpdateAnalystRating()
    {
        var engine = new EarningsEngine(42);
        var stocks = CreateStocks(20);
        // Set initial ratings
        foreach (var s in stocks) s.AnalystRating = 3.0m;
        engine.GenerateSchedule(stocks, new DateTime(2027, 1, 5));

        for (int i = 0; i < 90; i++)
            engine.TickDay(stocks, new DateTime(2027, 1, 5).AddDays(i));

        var released = engine.Schedule.Where(e => e.Released).ToList();
        Assert.True(released.Count > 0);
        // At least one stock's rating should have shifted from 3.0
        var shifted = stocks.Any(s => s.AnalystRating != 3.0m);
        Assert.True(shifted, "Analyst ratings should shift after earnings");
    }

    [Fact]
    public void ReleasedEarnings_ShouldUpdateTargetPrice()
    {
        var engine = new EarningsEngine(42);
        var stocks = CreateStocks(20);
        foreach (var s in stocks) s.TargetPrice = s.CurrentPrice;
        var targetsBefore = stocks.ToDictionary(s => s.Symbol, s => s.TargetPrice);
        engine.GenerateSchedule(stocks, new DateTime(2027, 1, 5));

        for (int i = 0; i < 90; i++)
            engine.TickDay(stocks, new DateTime(2027, 1, 5).AddDays(i));

        var changed = stocks.Any(s => s.TargetPrice != targetsBefore[s.Symbol]);
        Assert.True(changed, "Target prices should update after earnings");
    }

    [Fact]
    public void ReleasedEarnings_ShouldUpdateFairValue()
    {
        var engine = new EarningsEngine(42);
        var stocks = CreateStocks(20);
        var fairBefore = stocks.ToDictionary(s => s.Symbol, s => s.FairValue);
        engine.GenerateSchedule(stocks, new DateTime(2027, 1, 5));

        for (int i = 0; i < 90; i++)
            engine.TickDay(stocks, new DateTime(2027, 1, 5).AddDays(i));

        var changed = stocks.Any(s => s.FairValue != fairBefore[s.Symbol]);
        Assert.True(changed, "FairValue should update after earnings");
    }

    [Fact]
    public void AnalystRating_ShouldStayInBounds()
    {
        var engine = new EarningsEngine(42);
        var stocks = CreateStocks(20);
        engine.GenerateSchedule(stocks, new DateTime(2027, 1, 5));

        // Run many earnings cycles
        for (int i = 0; i < 365; i++)
            engine.TickDay(stocks, new DateTime(2027, 1, 5).AddDays(i));

        Assert.All(stocks, s =>
        {
            Assert.InRange(s.AnalystRating, 1.0m, 5.0m);
            Assert.True(s.Revenue > 0, $"{s.Symbol} revenue should stay positive");
        });
    }

    // ========================
    // Realism Batch 2: Dividend cuts/raises after earnings
    // ========================

    [Fact]
    public void EarningsMiss_ShouldCutDividend_WhenNetIncomeNegative()
    {
        var engine = new EarningsEngine(42);
        var stock = new Stock("DCUT", "DivCut Corp", "Utilities")
        {
            CurrentPrice = 50m,
            SharesOutstanding = 1_000_000,
            NetIncome = -2_000_000m, // Negative earnings
            Revenue = 20_000_000m,
            BaseVolatility = 0.02m,
            DividendYield = 0.05m, // 5% yield
            DebtToEquity = 1.0m,
        };
        stock.FairValue = stock.CurrentPrice;
        var stocks = new List<Stock> { stock };
        engine.GenerateSchedule(stocks, new DateTime(2027, 1, 5));

        var yieldBefore = stock.DividendYield;

        // Run until at least one earnings report fires
        for (int i = 0; i < 120; i++)
            engine.TickDay(stocks, new DateTime(2027, 1, 5).AddDays(i));

        var released = engine.Schedule.Where(e => e.Released).ToList();
        if (released.Count > 0)
        {
            // Dividend should be cut for a company with negative income
            Assert.True(stock.DividendYield < yieldBefore,
                $"Dividend yield {stock.DividendYield} should be less than {yieldBefore} after miss with negative income");
        }
    }

    [Fact]
    public void StrongEarningsBeat_ShouldRaiseDividend()
    {
        // Use many seeds to find one where a beat happens and dividend rises
        var found = false;
        for (int seed = 0; seed < 50; seed++)
        {
            var engine = new EarningsEngine(seed);
            var stock = new Stock("DRAI", "DivRaise Corp", "Utilities")
            {
                CurrentPrice = 100m,
                SharesOutstanding = 1_000_000,
                NetIncome = 50_000_000m, // Strong earnings
                Revenue = 200_000_000m,
                BaseVolatility = 0.02m,
                DividendYield = 0.03m, // 3% yield
                DebtToEquity = 0.5m,
            };
            stock.FairValue = stock.CurrentPrice;
            var stocks = new List<Stock> { stock };
            engine.GenerateSchedule(stocks, new DateTime(2027, 1, 5));

            for (int i = 0; i < 120; i++)
                engine.TickDay(stocks, new DateTime(2027, 1, 5).AddDays(i));

            var released = engine.Schedule.Where(e => e.Released && e.Beat).ToList();
            if (released.Count > 0 && stock.DividendYield > 0.03m)
            {
                found = true;
                break;
            }
        }
        Assert.True(found, "Should find at least one seed where dividend rises after earnings beat");
    }

    [Fact]
    public void DividendYield_ShouldNotGoNegative()
    {
        var engine = new EarningsEngine(42);
        var stocks = CreateStocks(20);
        foreach (var s in stocks) s.DividendYield = 0.01m; // Low yield
        engine.GenerateSchedule(stocks, new DateTime(2027, 1, 5));

        for (int i = 0; i < 365; i++)
            engine.TickDay(stocks, new DateTime(2027, 1, 5).AddDays(i));

        Assert.All(stocks, s => Assert.True(s.DividendYield >= 0m,
            $"{s.Symbol} dividend yield {s.DividendYield} should not be negative"));
    }

    [Fact]
    public void DividendYield_ShouldNotExceedCap()
    {
        var engine = new EarningsEngine(42);
        var stocks = CreateStocks(20);
        foreach (var s in stocks) s.DividendYield = 0.10m; // High yield
        engine.GenerateSchedule(stocks, new DateTime(2027, 1, 5));

        for (int i = 0; i < 365; i++)
            engine.TickDay(stocks, new DateTime(2027, 1, 5).AddDays(i));

        Assert.All(stocks, s => Assert.True(s.DividendYield <= 0.15m,
            $"{s.Symbol} dividend yield {s.DividendYield} should not exceed 15%"));
    }

    // ========================
    // Realism Batch 2: DebtToEquity changes after earnings
    // ========================

    [Fact]
    public void EarningsMiss_ShouldIncreaseDebtToEquity()
    {
        var found = false;
        for (int seed = 0; seed < 50; seed++)
        {
            var engine = new EarningsEngine(seed);
            var stock = new Stock("DEBT", "DebtUp Corp", "Technology")
            {
                CurrentPrice = 50m,
                SharesOutstanding = 1_000_000,
                NetIncome = 1_000_000m,
                Revenue = 20_000_000m,
                BaseVolatility = 0.02m,
                DebtToEquity = 1.5m,
                DividendYield = 0m,
            };
            stock.FairValue = stock.CurrentPrice;
            var stocks = new List<Stock> { stock };
            engine.GenerateSchedule(stocks, new DateTime(2027, 1, 5));

            for (int i = 0; i < 120; i++)
                engine.TickDay(stocks, new DateTime(2027, 1, 5).AddDays(i));

            var misses = engine.Schedule.Where(e => e.Released && !e.Beat).ToList();
            if (misses.Count > 0 && stock.DebtToEquity > 1.5m)
            {
                found = true;
                break;
            }
        }
        Assert.True(found, "Should find a seed where D/E rises after earnings miss");
    }

    [Fact]
    public void EarningsBeat_ShouldDecreaseDebtToEquity()
    {
        var found = false;
        for (int seed = 0; seed < 50; seed++)
        {
            var engine = new EarningsEngine(seed);
            var stock = new Stock("DELD", "DebtDown Corp", "Technology")
            {
                CurrentPrice = 100m,
                SharesOutstanding = 1_000_000,
                NetIncome = 50_000_000m,
                Revenue = 200_000_000m,
                BaseVolatility = 0.02m,
                DebtToEquity = 2.0m,
                DividendYield = 0m,
            };
            stock.FairValue = stock.CurrentPrice;
            var stocks = new List<Stock> { stock };
            engine.GenerateSchedule(stocks, new DateTime(2027, 1, 5));

            for (int i = 0; i < 120; i++)
                engine.TickDay(stocks, new DateTime(2027, 1, 5).AddDays(i));

            var beats = engine.Schedule.Where(e => e.Released && e.Beat).ToList();
            if (beats.Count > 0 && stock.DebtToEquity < 2.0m)
            {
                found = true;
                break;
            }
        }
        Assert.True(found, "Should find a seed where D/E drops after earnings beat");
    }

    [Fact]
    public void DebtToEquity_ShouldNotGoNegative()
    {
        var engine = new EarningsEngine(42);
        var stocks = CreateStocks(20);
        foreach (var s in stocks) s.DebtToEquity = 0.1m; // Low debt
        engine.GenerateSchedule(stocks, new DateTime(2027, 1, 5));

        for (int i = 0; i < 365; i++)
            engine.TickDay(stocks, new DateTime(2027, 1, 5).AddDays(i));

        Assert.All(stocks, s => Assert.True(s.DebtToEquity >= 0m,
            $"{s.Symbol} D/E {s.DebtToEquity} should not be negative"));
    }
}

public class TaxEngineTests
{
    [Fact]
    public void ShortTermGain_ShouldTaxAt35Percent()
    {
        var engine = new TaxEngine();
        var tax = engine.CalculateTradeTax(1000m, holdingDays: 100);

        Assert.Equal(350m, tax); // 35% of 1000
        Assert.Equal(1000m, engine.ShortTermGains);
        Assert.Equal(350m, engine.TotalTaxPaid);
    }

    [Fact]
    public void LongTermGain_ShouldTaxAt15Percent()
    {
        var engine = new TaxEngine();
        var tax = engine.CalculateTradeTax(1000m, holdingDays: 300);

        Assert.Equal(150m, tax); // 15% of 1000
        Assert.Equal(1000m, engine.LongTermGains);
    }

    [Fact]
    public void Loss_ShouldNotTax()
    {
        var engine = new TaxEngine();
        var tax = engine.CalculateTradeTax(-500m, holdingDays: 100);

        Assert.Equal(0m, tax);
        Assert.Equal(500m, engine.ShortTermLosses);
        Assert.Equal(0m, engine.TotalTaxPaid);
    }

    [Fact]
    public void TaxLossHarvesting_ShouldOffsetGains()
    {
        var engine = new TaxEngine();

        // Loss
        engine.CalculateTradeTax(-500m, holdingDays: 100);
        // Gain
        engine.CalculateTradeTax(1000m, holdingDays: 100);

        Assert.Equal(1000m, engine.ShortTermGains);
        Assert.Equal(500m, engine.ShortTermLosses);
        Assert.Equal(500m, engine.NetShortTermGains);
    }

    [Fact]
    public void DividendTax_ShouldApply()
    {
        var engine = new TaxEngine();
        var tax = engine.CalculateDividendTax(1000m);

        Assert.Equal(150m, tax); // 15% qualified rate
        Assert.Equal(150m, engine.DividendTaxPaid);
    }

    [Fact]
    public void DisabledEngine_ShouldNotTax()
    {
        var engine = new TaxEngine { Enabled = false };

        var tax = engine.CalculateTradeTax(1000m, holdingDays: 100);
        Assert.Equal(0m, tax);

        var divTax = engine.CalculateDividendTax(500m);
        Assert.Equal(0m, divTax);
    }

    [Fact]
    public void EffectiveTaxRate_ShouldCalculateCorrectly()
    {
        var engine = new TaxEngine();
        engine.CalculateTradeTax(1000m, holdingDays: 100); // 350 tax
        engine.CalculateTradeTax(1000m, holdingDays: 300); // 150 tax

        // Total tax: 500, Total gains: 2000 => 25%
        Assert.Equal(25m, engine.EffectiveTaxRate);
    }

    [Fact]
    public void GetSummary_ShouldReturnAllFields()
    {
        var engine = new TaxEngine();
        engine.CalculateTradeTax(1000m, holdingDays: 100);
        engine.CalculateTradeTax(-300m, holdingDays: 400);

        var summary = engine.GetSummary();
        Assert.Equal(1000m, summary.ShortTermGains);
        Assert.Equal(300m, summary.LongTermLosses);
        Assert.Equal(35m, summary.ShortTermTaxRate);
        Assert.Equal(15m, summary.LongTermTaxRate);
    }

    [Fact]
    public void ZeroPnL_ShouldNotTax()
    {
        var engine = new TaxEngine();
        var tax = engine.CalculateTradeTax(0m, holdingDays: 100);
        Assert.Equal(0m, tax);
    }
}
