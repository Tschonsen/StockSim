using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

public class CircuitBreakerTests
{
    private readonly CircuitBreaker _cb = new();
    private readonly DateTime _now = new(2027, 1, 5, 10, 0, 0);

    [Fact]
    public void ShouldHaltStock_WhenDropsOver10Percent()
    {
        // Mix of stocks so market avg doesn't trigger halt
        var stocks = new[]
        {
            MakeStock("AAPL", 100m, 85m),  // -15% → individual halt
            MakeStock("GOOG", 100m, 100m),  // flat
            MakeStock("MSFT", 100m, 100m),  // flat
        };

        _cb.Tick(stocks, _now);

        Assert.True(_cb.HaltedStocks.ContainsKey("AAPL"));
        Assert.False(_cb.HaltedStocks.ContainsKey("GOOG"));
    }

    [Fact]
    public void ShouldNotHalt_WhenDropUnder10Percent()
    {
        var stocks = new[]
        {
            MakeStock("AAPL", 100m, 92m),   // -8% → no individual halt
            MakeStock("GOOG", 100m, 102m),   // +2% → brings avg above -7%
        };

        _cb.Tick(stocks, _now);

        Assert.False(_cb.HaltedStocks.ContainsKey("AAPL"));
    }

    [Fact]
    public void HaltedStock_ShouldResumeAfter30Minutes()
    {
        var stocks = new[]
        {
            MakeStock("AAPL", 100m, 85m),
            MakeStock("GOOG", 100m, 100m),
            MakeStock("MSFT", 100m, 100m),
        };

        _cb.Tick(stocks, _now);
        Assert.True(_cb.HaltedStocks.ContainsKey("AAPL"));

        // 31 minutes later
        _cb.Tick(stocks, _now.AddMinutes(31));
        Assert.False(_cb.HaltedStocks.ContainsKey("AAPL"));
    }

    [Fact]
    public void MarketWideHalt_Level1_At7PercentDrop()
    {
        var stocks = Enumerable.Range(0, 10)
            .Select(i => MakeStock($"S{i}", 100m, 92m)) // all -8% → avg -8%
            .ToList();

        _cb.Tick(stocks, _now);

        Assert.True(_cb.IsMarketHalted);
    }

    [Fact]
    public void MarketWideHalt_ShouldExpire()
    {
        var stocks = Enumerable.Range(0, 10)
            .Select(i => MakeStock($"S{i}", 100m, 92m))
            .ToList();

        _cb.Tick(stocks, _now);
        Assert.True(_cb.IsMarketHalted);

        // 16 minutes later (Level 1 = 15 min)
        _cb.Tick(stocks, _now.AddMinutes(16));
        Assert.False(_cb.IsMarketHalted);
    }

    [Fact]
    public void NoHalt_WhenPricesStable()
    {
        var stocks = Enumerable.Range(0, 10)
            .Select(i => MakeStock($"S{i}", 100m, 101m)) // +1%
            .ToList();

        _cb.Tick(stocks, _now);

        Assert.False(_cb.IsMarketHalted);
        Assert.Empty(_cb.HaltedStocks);
    }

    private static Stock MakeStock(string symbol, decimal prevClose, decimal currentPrice)
    {
        return new Stock(symbol, $"{symbol} Corp", "Technology")
        {
            CurrentPrice = currentPrice,
            PreviousClose = prevClose,
            BidPrice = currentPrice - 0.10m,
            AskPrice = currentPrice + 0.10m,
            BaseVolatility = 0.02m,
            LiquidityScore = 7,
        };
    }
}
