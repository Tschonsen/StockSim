using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

public class SaveManagerTests : IDisposable
{
    private readonly string _testSavePath;

    public SaveManagerTests()
    {
        _testSavePath = Path.Combine(Path.GetTempPath(), $"stocksim_test_{Guid.NewGuid()}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_testSavePath))
            File.Delete(_testSavePath);
    }

    [Fact]
    public async Task SaveAndLoad_ShouldPreserveGameTime()
    {
        var gameLoop = new GameLoop(seed: 42, stockCount: 5);
        gameLoop.SetSpeed(GameSpeed.Normal);
        for (int i = 0; i < 35; i++) gameLoop.ExecuteTick(); // Advance past market open

        await SaveManager.SaveGameAsync(gameLoop, _testSavePath);
        var loaded = await SaveManager.LoadGameAsync(_testSavePath);

        Assert.NotNull(loaded);
        Assert.Equal(gameLoop.GameTime, loaded!.GameTime);
    }

    [Fact]
    public async Task SaveAndLoad_ShouldPreserveStockCount()
    {
        var gameLoop = new GameLoop(seed: 42, stockCount: 10);

        await SaveManager.SaveGameAsync(gameLoop, _testSavePath);
        var loaded = await SaveManager.LoadGameAsync(_testSavePath);

        Assert.NotNull(loaded);
        Assert.Equal(10, loaded!.Stocks.Count);
    }

    [Fact]
    public async Task SaveAndLoad_ShouldPreserveCash()
    {
        var gameLoop = new GameLoop(seed: 42, stockCount: 5, startingCash: 75_000m);

        await SaveManager.SaveGameAsync(gameLoop, _testSavePath);
        var loaded = await SaveManager.LoadGameAsync(_testSavePath);

        Assert.NotNull(loaded);
        Assert.Equal(75_000m, loaded!.Portfolio.Cash);
    }

    [Fact]
    public async Task SaveAndLoad_ShouldPreservePositions()
    {
        var gameLoop = new GameLoop(seed: 42, stockCount: 5);
        var stock = gameLoop.Stocks[0];

        // Buy some shares
        gameLoop.OrderEngine.PlaceOrder(
            stock.Symbol, OrderSide.Buy, OrderType.Market, 10m, stock,
            gameLoop.GameTime, isMarketOpen: true);

        await SaveManager.SaveGameAsync(gameLoop, _testSavePath);
        var loaded = await SaveManager.LoadGameAsync(_testSavePath);

        Assert.NotNull(loaded);
        Assert.True(loaded!.Portfolio.Positions.ContainsKey(stock.Symbol),
            $"Should have position in {stock.Symbol}");
        Assert.Equal(10m, loaded.Portfolio.Positions[stock.Symbol].Shares);
    }

    [Fact]
    public async Task SaveAndLoad_ShouldPreserveRealizedPnL()
    {
        var gameLoop = new GameLoop(seed: 42, stockCount: 5);
        gameLoop.Portfolio.RealizedPnL = 1234.56m;
        gameLoop.Portfolio.TotalCommissions = 9.90m;
        gameLoop.Portfolio.TradeCount = 2;

        await SaveManager.SaveGameAsync(gameLoop, _testSavePath);
        var loaded = await SaveManager.LoadGameAsync(_testSavePath);

        Assert.NotNull(loaded);
        Assert.Equal(1234.56m, loaded!.Portfolio.RealizedPnL);
        Assert.Equal(9.90m, loaded.Portfolio.TotalCommissions);
        Assert.Equal(2, loaded.Portfolio.TradeCount);
    }

    [Fact]
    public async Task SaveAndLoad_ShouldPreserveStockPrices()
    {
        var gameLoop = new GameLoop(seed: 42, stockCount: 5);
        gameLoop.SetSpeed(GameSpeed.Normal);
        // Run some ticks to change prices
        for (int i = 0; i < 35; i++) gameLoop.ExecuteTick();

        var pricesBefore = gameLoop.Stocks.ToDictionary(s => s.Symbol, s => s.CurrentPrice);

        await SaveManager.SaveGameAsync(gameLoop, _testSavePath);
        var loaded = await SaveManager.LoadGameAsync(_testSavePath);

        Assert.NotNull(loaded);
        foreach (var stock in loaded!.Stocks)
        {
            Assert.Equal(pricesBefore[stock.Symbol], stock.CurrentPrice);
        }
    }

    [Fact]
    public async Task Load_NonexistentFile_ShouldReturnNull()
    {
        var result = await SaveManager.LoadGameAsync("/nonexistent/path/save.json");

        Assert.Null(result);
    }

    [Fact]
    public async Task Save_ShouldCreateFile()
    {
        var gameLoop = new GameLoop(seed: 42, stockCount: 5);

        await SaveManager.SaveGameAsync(gameLoop, _testSavePath);

        Assert.True(File.Exists(_testSavePath));
        var content = await File.ReadAllTextAsync(_testSavePath);
        Assert.Contains("\"seed\"", content);
        Assert.Contains("\"cash\"", content);
    }

    [Fact]
    public async Task SaveAndLoad_ShouldPreserveSpeed()
    {
        var gameLoop = new GameLoop(seed: 42, stockCount: 5);
        gameLoop.SetSpeed(GameSpeed.Fast);

        await SaveManager.SaveGameAsync(gameLoop, _testSavePath);
        var loaded = await SaveManager.LoadGameAsync(_testSavePath);

        Assert.NotNull(loaded);
        Assert.Equal(GameSpeed.Fast, loaded!.Speed);
    }
}
