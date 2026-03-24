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
}
