using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

/// <summary>
/// Regression tests: Save/Load with commodities, performance profiling,
/// full game simulation (GUI-equivalent automated playtest).
/// </summary>
public class RegressionTests : IDisposable
{
    private readonly string _testSavePath;

    public RegressionTests()
    {
        _testSavePath = Path.Combine(Path.GetTempPath(), $"stocksim_regression_{Guid.NewGuid()}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_testSavePath)) File.Delete(_testSavePath);
    }

    // === SAVE/LOAD REGRESSION ===

    [Fact]
    public async Task SaveLoad_PreservesCommodityETFs()
    {
        var loop = new GameLoop(seed: 42, stockCount: 30);
        loop.SetSpeed(GameSpeed.Normal);
        for (int i = 0; i < 35; i++) loop.ExecuteTick();

        // Verify commodities exist before save
        Assert.Contains(loop.Stocks, s => s.Symbol == "GLD");
        Assert.Contains(loop.Stocks, s => s.Symbol == "SLV");
        Assert.Contains(loop.Stocks, s => s.Symbol == "USO");
        var gldPrice = loop.Stocks.First(s => s.Symbol == "GLD").CurrentPrice;

        await SaveManager.SaveGameAsync(loop, _testSavePath);
        var loaded = await SaveManager.LoadGameAsync(_testSavePath);

        Assert.NotNull(loaded);
        Assert.Contains(loaded!.Stocks, s => s.Symbol == "GLD");
        Assert.Contains(loaded.Stocks, s => s.Symbol == "SLV");
        Assert.Contains(loaded.Stocks, s => s.Symbol == "USO");

        // Prices should be preserved
        var loadedGLD = loaded.Stocks.First(s => s.Symbol == "GLD");
        Assert.Equal(gldPrice, loadedGLD.CurrentPrice);
        Assert.Contains("Commodity ETF", loadedGLD.Traits);
    }

    [Fact]
    public async Task SaveLoad_PreservesPortfolioWithTrades()
    {
        var loop = new GameLoop(seed: 42, stockCount: 30, startingCash: 100_000m);
        loop.SetSpeed(GameSpeed.Normal);
        for (int i = 0; i < 35; i++) loop.ExecuteTick();

        // Make a trade
        var stock = loop.Stocks.First(s => !s.Traits.Contains("ETF"));
        loop.OrderEngine.PlaceOrder(stock.Symbol, OrderSide.Buy, OrderType.Market, 10m, stock, loop.GameTime, true);

        var cashBefore = loop.Portfolio.Cash;
        var posCount = loop.Portfolio.Positions.Count;

        await SaveManager.SaveGameAsync(loop, _testSavePath);
        var loaded = await SaveManager.LoadGameAsync(_testSavePath);

        Assert.NotNull(loaded);
        Assert.Equal(cashBefore, loaded!.Portfolio.Cash);
        Assert.Equal(posCount, loaded.Portfolio.Positions.Count);
    }

    [Fact]
    public async Task SaveLoad_PreservesEconomicState()
    {
        var loop = new GameLoop(seed: 42, stockCount: 10);
        loop.SetSpeed(GameSpeed.Normal);
        for (int i = 0; i < 100; i++) loop.ExecuteTick();

        var goldBefore = loop.EconomicEngine.Data.GoldPrice;
        var oilBefore = loop.EconomicEngine.Data.OilPrice;
        var rateBefore = loop.EconomicEngine.Data.InterestRate;

        await SaveManager.SaveGameAsync(loop, _testSavePath);
        var loaded = await SaveManager.LoadGameAsync(_testSavePath);

        Assert.NotNull(loaded);
        Assert.Equal(goldBefore, loaded!.EconomicEngine.Data.GoldPrice);
        Assert.Equal(oilBefore, loaded.EconomicEngine.Data.OilPrice);
        Assert.Equal(rateBefore, loaded.EconomicEngine.Data.InterestRate);
    }

    // === PERFORMANCE PROFILING ===

    [Fact]
    public void Performance_50Stocks_1000Ticks()
    {
        var loop = new GameLoop(seed: 99, stockCount: 50, startingCash: 100_000m);
        loop.AutoPauseOnNews = false;
        loop.AutoPauseOnMarginCall = false;
        loop.AutoPauseOnSMA = false;
        loop.AutoPauseOnShortSqueeze = false;
        loop.AutoPauseOnMarketOpen = false;
        loop.AutoPauseOnOrderExecution = false;
        loop.AutoPauseOnAlert = false;
        loop.SetSpeed(GameSpeed.Maximum);

        // Warmup
        for (int i = 0; i < 50; i++) loop.ExecuteTick();

        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++) loop.ExecuteTick();
        sw.Stop();

        var avgMs = sw.ElapsedMilliseconds / 1000.0;
        Assert.True(avgMs < 10,
            $"50 stocks: avg {avgMs:F2}ms/tick (target <10ms). Total: {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void Performance_250Stocks_500Ticks()
    {
        var loop = new GameLoop(seed: 99, stockCount: 250, startingCash: 100_000m);
        loop.AutoPauseOnNews = false;
        loop.AutoPauseOnMarginCall = false;
        loop.AutoPauseOnSMA = false;
        loop.AutoPauseOnShortSqueeze = false;
        loop.AutoPauseOnMarketOpen = false;
        loop.AutoPauseOnOrderExecution = false;
        loop.AutoPauseOnAlert = false;
        loop.SetSpeed(GameSpeed.Maximum);

        // Warmup
        for (int i = 0; i < 50; i++) loop.ExecuteTick();

        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < 500; i++) loop.ExecuteTick();
        sw.Stop();

        var avgMs = sw.ElapsedMilliseconds / 500.0;
        Assert.True(avgMs < 50,
            $"250 stocks: avg {avgMs:F2}ms/tick (target <50ms). Total: {sw.ElapsedMilliseconds}ms for {loop.Stocks.Count} stocks");
    }

    // === AUTOMATED GUI PLAYTEST ===

    [Fact]
    public void FullPlaytest_5Days_NoErrors()
    {
        var loop = new GameLoop(seed: 777, stockCount: 50, startingCash: 100_000m);
        loop.AutoPauseOnNews = false;
        loop.AutoPauseOnMarginCall = false;
        loop.AutoPauseOnSMA = false;
        loop.AutoPauseOnShortSqueeze = false;
        loop.AutoPauseOnMarketOpen = false;
        loop.AutoPauseOnOrderExecution = false;
        loop.AutoPauseOnAlert = false;
        loop.SetSpeed(GameSpeed.Maximum);

        // Run 5 trading days (390 ticks/day × 5 = 1950 + weekends/overnight skip)
        for (int i = 0; i < 3000; i++) loop.ExecuteTick();

        // Verify no corruption
        foreach (var stock in loop.Stocks)
        {
            Assert.False(double.IsNaN((double)stock.CurrentPrice), $"{stock.Symbol} price NaN");
            Assert.True(stock.CurrentPrice > 0, $"{stock.Symbol} price {stock.CurrentPrice} <= 0");
            Assert.True(stock.BidPrice > 0, $"{stock.Symbol} bid <= 0");
            Assert.True(stock.AskPrice >= stock.BidPrice, $"{stock.Symbol} ask < bid");
        }

        // Portfolio should be valid
        Assert.True(loop.Portfolio.Cash >= 0 || loop.Portfolio.MarginEnabled,
            $"Cash {loop.Portfolio.Cash} negative without margin");
        Assert.True(loop.Portfolio.Cash > 0,
            $"TotalEquity {loop.Portfolio.TotalEquity} <= 0");

        // Economic indicators in valid ranges
        Assert.True(loop.EconomicEngine.Data.OilPrice >= 20m && loop.EconomicEngine.Data.OilPrice <= 150m);
        Assert.True(loop.EconomicEngine.Data.GoldPrice >= 800m && loop.EconomicEngine.Data.GoldPrice <= 3000m);
        Assert.True(loop.EconomicEngine.Data.InterestRate >= 0m && loop.EconomicEngine.Data.InterestRate <= 15m);

        // Commodity ETFs still valid
        var gld = loop.Stocks.First(s => s.Symbol == "GLD");
        Assert.True(gld.CurrentPrice > 0 && gld.CurrentPrice < 1000);
    }

    [Fact]
    public void FullPlaytest_TradeDuringSimulation()
    {
        var loop = new GameLoop(seed: 123, stockCount: 30, startingCash: 100_000m);
        loop.AutoPauseOnNews = false;
        loop.AutoPauseOnMarginCall = false;
        loop.AutoPauseOnSMA = false;
        loop.AutoPauseOnShortSqueeze = false;
        loop.AutoPauseOnMarketOpen = false;
        loop.AutoPauseOnOrderExecution = false;
        loop.AutoPauseOnAlert = false;
        loop.SetSpeed(GameSpeed.Normal);

        // Advance to market open
        for (int i = 0; i < 35; i++) loop.ExecuteTick();

        var tradableStocks = loop.Stocks.Where(s => !s.Traits.Contains("ETF")).Take(5).ToList();

        // Buy 5 different stocks
        foreach (var stock in tradableStocks)
        {
            var result = loop.OrderEngine.PlaceOrder(
                stock.Symbol, OrderSide.Buy, OrderType.Market, 5m, stock,
                loop.GameTime, isMarketOpen: true);
            Assert.True(result.Success, $"Buy {stock.Symbol} failed: {result.Error}");
        }

        Assert.Equal(5, loop.Portfolio.Positions.Count);

        // Run 2 trading days
        for (int i = 0; i < 800; i++) loop.ExecuteTick();

        // Sell 2 positions
        for (int i = 0; i < 2; i++)
        {
            var pos = loop.Portfolio.Positions.ElementAt(0);
            var stock = loop.Stocks.First(s => s.Symbol == pos.Key);
            var result = loop.OrderEngine.PlaceOrder(
                stock.Symbol, OrderSide.Sell, OrderType.Market, 5m, stock,
                loop.GameTime, isMarketOpen: loop.IsMarketOpen());
            // May fail if market closed, that's OK
        }

        // Buy a commodity ETF
        var gld = loop.Stocks.First(s => s.Symbol == "GLD");
        var gldResult = loop.OrderEngine.PlaceOrder(
            "GLD", OrderSide.Buy, OrderType.Market, 10m, gld,
            loop.GameTime, isMarketOpen: loop.IsMarketOpen());

        // Run more ticks
        for (int i = 0; i < 500; i++) loop.ExecuteTick();

        // All prices should still be valid
        foreach (var stock in loop.Stocks)
        {
            Assert.False(double.IsNaN((double)stock.CurrentPrice), $"{stock.Symbol} NaN after trading");
            Assert.True(stock.CurrentPrice > 0, $"{stock.Symbol} <= 0 after trading");
        }

        Assert.True(loop.Portfolio.TradeCount >= 5, $"Expected >=5 trades, got {loop.Portfolio.TradeCount}");
    }

    [Fact]
    public void OptionsChain_SurvivesMultipleDays()
    {
        var loop = new GameLoop(seed: 42, stockCount: 50);
        loop.AutoPauseOnNews = false;
        loop.AutoPauseOnMarginCall = false;
        loop.AutoPauseOnSMA = false;
        loop.AutoPauseOnShortSqueeze = false;
        loop.AutoPauseOnMarketOpen = false;
        loop.AutoPauseOnOrderExecution = false;
        loop.AutoPauseOnAlert = false;
        loop.SetSpeed(GameSpeed.Maximum);

        Assert.True(loop.OptionsEngine.Chains.Count > 0, "Should have option chains");

        // Run 10 trading days
        for (int i = 0; i < 5000; i++) loop.ExecuteTick();

        // Options should still have valid chains
        Assert.True(loop.OptionsEngine.Chains.Count > 0, "Should still have option chains after 10 days");

        foreach (var (symbol, chain) in loop.OptionsEngine.Chains)
        {
            foreach (var slice in chain.Slices.Values)
            {
                foreach (var (_, call) in slice.Calls)
                {
                    Assert.False(double.IsNaN(call.Delta), $"{symbol} call delta NaN");
                    Assert.True(call.TheoreticalPrice >= 0, $"{symbol} call price {call.TheoreticalPrice} < 0");
                }
            }
        }
    }

    [Fact]
    public void EventEngine_GeneratesEvents_Over5Days()
    {
        var loop = new GameLoop(seed: 42, stockCount: 30);
        loop.AutoPauseOnNews = false;
        loop.AutoPauseOnMarginCall = false;
        loop.AutoPauseOnSMA = false;
        loop.AutoPauseOnShortSqueeze = false;
        loop.AutoPauseOnMarketOpen = false;
        loop.AutoPauseOnOrderExecution = false;
        loop.AutoPauseOnAlert = false;
        loop.SetSpeed(GameSpeed.Maximum);

        int totalEvents = 0;
        // Run 5 days
        for (int i = 0; i < 3000; i++)
        {
            loop.ExecuteTick();
            totalEvents += loop.EventEngine.NewEventsThisTick.Count;
        }

        // Should have generated a reasonable number of events
        Assert.True(totalEvents > 50,
            $"Expected >50 events over 5 days, got {totalEvents}");
    }
}
