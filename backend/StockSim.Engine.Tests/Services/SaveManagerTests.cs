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
        Assert.True(loaded!.Stocks.Count >= 10, $"Expected at least 10 stocks (+ ETFs), got {loaded.Stocks.Count}");
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
    public async Task SaveAndLoad_ShouldPreserveDynamicPersonality()
    {
        // CEO archetype, credit rating and performance streak evolve during play (Points 3/8) and
        // must survive a save/load, not reset to their seed-generated baseline.
        var gameLoop = new GameLoop(seed: 42, stockCount: 5);
        var stock = gameLoop.Stocks.First(s => s.Personality != null);
        var sym = stock.Symbol;
        stock.Personality!.CEOArchetype = "Disruptor";
        stock.Personality.CreditRating = "B";
        stock.Personality.PerformanceStreak = 47;

        await SaveManager.SaveGameAsync(gameLoop, _testSavePath);
        var loaded = await SaveManager.LoadGameAsync(_testSavePath);

        var reloaded = loaded!.Stocks.First(s => s.Symbol == sym);
        Assert.Equal(47, reloaded.Personality!.PerformanceStreak); // 0 on fresh regen → proves restore
        Assert.Equal("Disruptor", reloaded.Personality.CEOArchetype);
        Assert.Equal("B", reloaded.Personality.CreditRating);
    }

    [Fact]
    public async Task SaveAndLoad_ShouldPreserveUnlockedAchievements()
    {
        var gameLoop = new GameLoop(seed: 42, stockCount: 5);
        var ach = gameLoop.AchievementEngine.Achievements.First();
        ach.Unlocked = true;
        ach.UnlockedAt = new DateTime(2027, 3, 1);
        var id = ach.Id;

        await SaveManager.SaveGameAsync(gameLoop, _testSavePath);
        var loaded = await SaveManager.LoadGameAsync(_testSavePath);

        var reAch = loaded!.AchievementEngine.Achievements.First(a => a.Id == id);
        Assert.True(reAch.Unlocked, "unlocked achievement should survive load");
    }

    [Fact]
    public async Task SaveAndLoad_ShouldPreservePriceAlerts()
    {
        var gameLoop = new GameLoop(seed: 42, stockCount: 5);
        var sym = gameLoop.Stocks.First(s => !s.Traits.Contains("ETF")).Symbol;
        gameLoop.Portfolio.PriceAlerts.Add(new PriceAlert(sym, "Above", 123.45m));

        await SaveManager.SaveGameAsync(gameLoop, _testSavePath);
        var loaded = await SaveManager.LoadGameAsync(_testSavePath);

        var alert = Assert.Single(loaded!.Portfolio.PriceAlerts);
        Assert.Equal(sym, alert.Symbol);
        Assert.Equal(123.45m, alert.TargetPrice);
        Assert.Equal("Above", alert.Condition);
    }

    [Fact]
    public async Task SaveAndLoad_ShouldPreserveTaxState()
    {
        var gameLoop = new GameLoop(seed: 42, stockCount: 5);
        gameLoop.TaxEngine.ShortTermGains = 1234m;
        gameLoop.TaxEngine.TotalTaxPaid = 567m;
        gameLoop.TaxEngine.WashSaleDisallowed = 89m;

        await SaveManager.SaveGameAsync(gameLoop, _testSavePath);
        var loaded = await SaveManager.LoadGameAsync(_testSavePath);

        Assert.Equal(1234m, loaded!.TaxEngine.ShortTermGains);
        Assert.Equal(567m, loaded.TaxEngine.TotalTaxPaid);
        Assert.Equal(89m, loaded.TaxEngine.WashSaleDisallowed);
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
    public async Task SaveAndLoad_ShouldPreserveOrderIdCounter()
    {
        var gameLoop = new GameLoop(seed: 42, stockCount: 5);
        var stock = gameLoop.Stocks[0];

        // Place some orders to advance the ID counter
        gameLoop.OrderEngine.PlaceOrder(stock.Symbol, OrderSide.Buy, OrderType.Market, 5m, stock, gameLoop.GameTime, true);
        gameLoop.OrderEngine.PlaceOrder(stock.Symbol, OrderSide.Buy, OrderType.Market, 5m, stock, gameLoop.GameTime, true);

        await SaveManager.SaveGameAsync(gameLoop, _testSavePath);
        var loaded = await SaveManager.LoadGameAsync(_testSavePath);

        Assert.NotNull(loaded);

        // New orders after load should have IDs > the saved orders
        var newOrder = new Order("TEST", OrderSide.Buy, OrderType.Market, 1m, DateTime.Now);
        Assert.True(newOrder.Id > 2, $"New order ID {newOrder.Id} should be > 2 (saved max)");
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
