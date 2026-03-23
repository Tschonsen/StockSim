using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

public class GameLoopTests
{
    [Fact]
    public void GameLoop_ShouldInitializeWithStocks()
    {
        var loop = new GameLoop(seed: 42, stockCount: 10);

        Assert.Equal(10, loop.Stocks.Count);
        Assert.All(loop.Stocks, s => Assert.True(s.CurrentPrice > 0));
    }

    [Fact]
    public void GameLoop_ShouldStartPaused()
    {
        var loop = new GameLoop(seed: 42, stockCount: 5);

        Assert.True(loop.IsPaused);
        Assert.Equal(GameSpeed.Paused, loop.Speed);
    }

    [Fact]
    public void GameLoop_ShouldAdvanceTimeOnTick()
    {
        var loop = new GameLoop(seed: 42, stockCount: 5);
        loop.SetSpeed(GameSpeed.Normal);
        var timeBefore = loop.GameTime;

        loop.ExecuteTick();

        Assert.True(loop.GameTime > timeBefore);
    }

    [Fact]
    public void GameLoop_ShouldNotTickWhenPaused()
    {
        var loop = new GameLoop(seed: 42, stockCount: 5);
        var timeBefore = loop.GameTime;

        loop.ExecuteTick();

        Assert.Equal(timeBefore, loop.GameTime);
    }

    [Fact]
    public void GameLoop_ShouldUpdatePricesOnTick()
    {
        var loop = new GameLoop(seed: 42, stockCount: 5);
        loop.SetSpeed(GameSpeed.Normal);

        // Advance past market open (9:00 → 9:31 = 31 ticks)
        for (int i = 0; i < 31; i++) loop.ExecuteTick();

        var pricesBefore = loop.Stocks.Select(s => s.CurrentPrice).ToList();

        // Run several ticks to ensure visible price changes
        for (int i = 0; i < 50; i++) loop.ExecuteTick();

        var pricesAfter = loop.Stocks.Select(s => s.CurrentPrice).ToList();
        var details = pricesBefore.Zip(pricesAfter).Select((p, i) =>
            $"{loop.Stocks[i].Symbol}: {p.First} -> {p.Second}");
        Assert.True(pricesBefore.Zip(pricesAfter).Any(p => p.First != p.Second),
            $"Prices should change. GameTime={loop.GameTime}, MarketOpen={loop.IsMarketOpen()}. " +
            string.Join(", ", details));
    }

    [Fact]
    public void GameLoop_ShouldTrackTickCount()
    {
        var loop = new GameLoop(seed: 42, stockCount: 5);
        loop.SetSpeed(GameSpeed.Normal);

        Assert.Equal(0, loop.TickCount);
        loop.ExecuteTick();
        Assert.Equal(1, loop.TickCount);
        loop.ExecuteTick();
        Assert.Equal(2, loop.TickCount);
    }

    [Fact]
    public void GameLoop_ShouldSupportSpeedChange()
    {
        var loop = new GameLoop(seed: 42, stockCount: 5);

        loop.SetSpeed(GameSpeed.Fast);
        Assert.Equal(GameSpeed.Fast, loop.Speed);
        Assert.False(loop.IsPaused);

        loop.SetSpeed(GameSpeed.Paused);
        Assert.True(loop.IsPaused);
    }

    [Fact]
    public void GameLoop_ShouldGenerateStocksAcrossSectors()
    {
        var loop = new GameLoop(seed: 42, stockCount: 100);

        var sectors = loop.Stocks.Select(s => s.Sector).Distinct().ToList();
        Assert.True(sectors.Count >= 4, $"Expected multiple sectors, got {sectors.Count}: {string.Join(", ", sectors)}");
    }

    [Fact]
    public void GameLoop_ShouldGenerateUniqueSymbols()
    {
        var loop = new GameLoop(seed: 42, stockCount: 100);

        var symbols = loop.Stocks.Select(s => s.Symbol).ToList();
        var uniqueSymbols = symbols.Distinct().ToList();

        Assert.Equal(symbols.Count, uniqueSymbols.Count);
    }

    [Fact]
    public void GameLoop_ShouldGenerateRealisticPriceRange()
    {
        var loop = new GameLoop(seed: 42, stockCount: 100);

        Assert.All(loop.Stocks, s =>
        {
            Assert.True(s.CurrentPrice >= 0.50m, $"{s.Symbol} price too low: {s.CurrentPrice}");
            Assert.True(s.CurrentPrice <= 5000m, $"{s.Symbol} price too high: {s.CurrentPrice}");
        });
    }

    [Fact]
    public void GameLoop_ShouldHaveMarketPhase()
    {
        var loop = new GameLoop(seed: 42, stockCount: 5);

        Assert.True(Enum.IsDefined(loop.Phase));
    }

    [Fact]
    public void GameLoop_ShouldGenerateDailyHistory()
    {
        var loop = new GameLoop(seed: 42, stockCount: 5);

        Assert.Equal(5, loop.DailyHistory.Count);
        Assert.All(loop.DailyHistory.Values, candles =>
        {
            Assert.Equal(252, candles.Count);
        });
    }

    [Fact]
    public void GameLoop_DailyHistory_LastCloseMatchesCurrentPrice()
    {
        var loop = new GameLoop(seed: 42, stockCount: 10);

        foreach (var stock in loop.Stocks)
        {
            var candles = loop.DailyHistory[stock.Symbol];
            var lastClose = candles[^1].Close;
            var deviation = Math.Abs(lastClose - stock.CurrentPrice) / stock.CurrentPrice;
            Assert.True(deviation < 0.01m,
                $"{stock.Symbol}: last candle close {lastClose} should match current price {stock.CurrentPrice}");
        }
    }
}
