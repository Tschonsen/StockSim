using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

public class ETFEngineTests
{
    private (ETFEngine engine, List<Stock> stocks, Dictionary<string, Stock> bySymbol) Setup()
    {
        var stocks = new List<Stock>();
        for (int i = 0; i < 24; i++)
        {
            var sector = i < 12 ? "Technology" : "Energy";
            var stock = new Stock($"S{i:D3}", $"Stock {i}", sector)
            {
                CurrentPrice = 100m + i,
                SharesOutstanding = 1_000_000,
                AverageVolume = 500_000,
                DividendYield = 0.02m,
                LiquidityScore = 5,
                BaseVolatility = 0.02m,
            };
            stock.BidPrice = stock.CurrentPrice - 0.05m;
            stock.AskPrice = stock.CurrentPrice + 0.05m;
            stock.PreviousClose = stock.CurrentPrice;
            stock.DayHigh = stock.CurrentPrice;
            stock.DayLow = stock.CurrentPrice;
            stocks.Add(stock);
        }
        var bySymbol = stocks.ToDictionary(s => s.Symbol);
        var engine = new ETFEngine();
        return (engine, stocks, bySymbol);
    }

    [Fact]
    public void CreateETFs_ShouldCreate13ETFs()
    {
        var (engine, stocks, _) = Setup();
        var etfs = engine.CreateETFs(stocks);

        // 1 index + at least 2 sector ETFs (Tech + Energy)
        Assert.True(etfs.Count >= 3, $"Expected at least 3 ETFs, got {etfs.Count}");
        Assert.Contains(etfs, e => e.Symbol == "SIMX"); // Market index
    }

    [Fact]
    public void CreateETFs_ShouldHaveETFTrait()
    {
        var (engine, stocks, _) = Setup();
        var etfs = engine.CreateETFs(stocks);

        Assert.All(etfs, e => Assert.Contains("ETF", e.Traits));
    }

    [Fact]
    public void CreateETFs_SIMXShouldBeIndexFund()
    {
        var (engine, stocks, _) = Setup();
        var etfs = engine.CreateETFs(stocks);
        var simx = etfs.First(e => e.Symbol == "SIMX");

        Assert.Contains("Index Fund", simx.Traits);
    }

    [Fact]
    public void CreateETFs_ShouldHavePositivePrice()
    {
        var (engine, stocks, _) = Setup();
        var etfs = engine.CreateETFs(stocks);

        Assert.All(etfs, e => Assert.True(e.CurrentPrice > 0, $"{e.Symbol} price is {e.CurrentPrice}"));
    }

    [Fact]
    public void UpdatePrices_ShouldTrackConstituentChanges()
    {
        var (engine, stocks, bySymbol) = Setup();
        engine.CreateETFs(stocks);

        var simxBefore = engine.ETFs.First(e => e.Symbol == "SIMX").CurrentPrice;

        // Increase all stock prices by 10%
        foreach (var stock in stocks)
            stock.CurrentPrice *= 1.10m;

        engine.UpdatePrices(bySymbol);
        var simxAfter = engine.ETFs.First(e => e.Symbol == "SIMX").CurrentPrice;

        Assert.True(simxAfter > simxBefore, $"SIMX should increase: {simxBefore} -> {simxAfter}");
    }

    [Fact]
    public void UpdatePrices_ShouldTrackDayHighLow()
    {
        var (engine, stocks, bySymbol) = Setup();
        engine.CreateETFs(stocks);
        var simx = engine.ETFs.First(e => e.Symbol == "SIMX");

        var initialHigh = simx.DayHigh;

        foreach (var stock in stocks)
            stock.CurrentPrice *= 1.05m;
        engine.UpdatePrices(bySymbol);

        Assert.True(simx.DayHigh > initialHigh);
    }

    [Fact]
    public void ResetDailyValues_ShouldResetHighLow()
    {
        var (engine, stocks, bySymbol) = Setup();
        engine.CreateETFs(stocks);

        foreach (var stock in stocks)
            stock.CurrentPrice *= 1.10m;
        engine.UpdatePrices(bySymbol);

        engine.ResetDailyValues();
        var simx = engine.ETFs.First(e => e.Symbol == "SIMX");

        Assert.Equal(simx.CurrentPrice, simx.DayHigh);
        Assert.Equal(simx.CurrentPrice, simx.DayLow);
        Assert.Equal(0, simx.DayVolume);
    }

    [Fact]
    public void ETFsShouldHaveHighLiquidity()
    {
        var (engine, stocks, _) = Setup();
        engine.CreateETFs(stocks);

        Assert.All(engine.ETFs, e => Assert.Equal(10, e.LiquidityScore));
    }

    [Fact]
    public void SectorETF_ShouldMatchSector()
    {
        var (engine, stocks, _) = Setup();
        engine.CreateETFs(stocks);

        var techETF = engine.ETFs.FirstOrDefault(e => e.Symbol == "STEC");
        Assert.NotNull(techETF);
        Assert.Equal("Technology", techETF.Sector);
    }

    [Fact]
    public void UpdatePrices_ShouldDeriveVolumeFromConstituents()
    {
        var (engine, stocks, bySymbol) = Setup();
        engine.CreateETFs(stocks);

        // Set volume on constituent stocks
        foreach (var s in stocks)
            s.DayVolume = 100_000;

        engine.UpdatePrices(bySymbol);

        var simx = engine.ETFs.First(e => e.Symbol == "SIMX");
        Assert.True(simx.DayVolume > 0, $"ETF volume should be derived from constituents, got {simx.DayVolume}");
    }

    [Fact]
    public void CreateETFs_ShouldCreateAllSectorETFs_WhenAllSectorsPresent()
    {
        var sectors = new[]
        {
            "Technology", "Energy", "Financials", "Healthcare",
            "Consumer Goods", "Industrials", "Materials", "Real Estate",
            "Telecommunications", "Utilities", "Luxury Goods", "Transportation"
        };

        var stocks = new List<Stock>();
        int idx = 0;
        foreach (var sector in sectors)
        {
            for (int i = 0; i < 2; i++)
            {
                stocks.Add(new Stock($"X{idx:D3}", $"Stock {idx}", sector)
                {
                    CurrentPrice = 50m + idx,
                    SharesOutstanding = 1_000_000,
                    DividendYield = 0.02m,
                    BaseVolatility = 0.02m,
                });
                idx++;
            }
        }

        var engine = new ETFEngine();
        var etfs = engine.CreateETFs(stocks);

        // 1 market-wide + 12 sector ETFs = 13
        Assert.Equal(13, etfs.Count);
    }

    [Fact]
    public void CreateETFs_SkipsSectorsWithNoStocks()
    {
        // Only Technology stocks
        var stocks = new List<Stock>
        {
            new Stock("T001", "Tech1", "Technology") { CurrentPrice = 100m, SharesOutstanding = 1_000_000, DividendYield = 0.01m },
            new Stock("T002", "Tech2", "Technology") { CurrentPrice = 150m, SharesOutstanding = 1_000_000, DividendYield = 0.02m },
        };

        var engine = new ETFEngine();
        var etfs = engine.CreateETFs(stocks);

        // 1 market ETF + 1 sector ETF (Technology only)
        Assert.Equal(2, etfs.Count);
        Assert.Contains(engine.ETFs, e => e.Symbol == "SIMX");
        Assert.Contains(engine.ETFs, e => e.Symbol == "STEC");
    }

    [Fact]
    public void ResetDailyValues_ShouldSetPreviousCloseToCurrentPrice()
    {
        var (engine, stocks, bySymbol) = Setup();
        engine.CreateETFs(stocks);

        // Change price via update
        foreach (var s in stocks)
            s.CurrentPrice *= 1.10m;
        engine.UpdatePrices(bySymbol);

        var simx = engine.ETFs.First(e => e.Symbol == "SIMX");
        var priceBeforeReset = simx.CurrentPrice;

        engine.ResetDailyValues();

        Assert.Equal(priceBeforeReset, simx.PreviousClose);
    }
}
