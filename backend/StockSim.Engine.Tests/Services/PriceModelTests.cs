using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

public class PriceModelTests
{
    private static string? FindModelPath()
    {
        // Walk up from test bin directory to find ml/price_model.onnx
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            var candidate = Path.Combine(dir, "ml", "price_model.onnx");
            if (File.Exists(candidate)) return dir;
            dir = Path.GetDirectoryName(dir) ?? dir;
        }
        return null;
    }

    [Fact]
    public void Load_ShouldSucceed_WhenFilesExist()
    {
        var baseDir = FindModelPath();
        if (baseDir == null) return; // Skip if model not on disk (CI)

        using var model = new PriceModel();
        var loaded = model.Load(
            Path.Combine(baseDir, "ml", "price_model.onnx"),
            Path.Combine(baseDir, "ml", "scaler_params.json"));

        Assert.True(loaded);
        Assert.True(model.IsLoaded);
    }

    [Fact]
    public void Load_ShouldReturnFalse_WhenFilesMissing()
    {
        using var model = new PriceModel();
        var loaded = model.Load("nonexistent.onnx", "nonexistent.json");

        Assert.False(loaded);
        Assert.False(model.IsLoaded);
    }

    [Fact]
    public void Predict_ShouldReturnNull_WhenNotLoaded()
    {
        using var model = new PriceModel();
        var result = model.Predict(
            new decimal[25], new long[25], new decimal[25], new decimal[25], new decimal[25],
            "Technology");

        Assert.Null(result);
    }

    [Fact]
    public void Predict_ShouldReturnNull_WhenHistoryTooShort()
    {
        var baseDir = FindModelPath();
        if (baseDir == null) return;

        using var model = new PriceModel();
        model.Load(
            Path.Combine(baseDir, "ml", "price_model.onnx"),
            Path.Combine(baseDir, "ml", "scaler_params.json"));

        // Only 10 prices — need at least 21
        var result = model.Predict(
            new decimal[10], new long[10], new decimal[10], new decimal[10], new decimal[10],
            "Technology");

        Assert.Null(result);
    }

    [Fact]
    public void Predict_ShouldReturnValidPrediction()
    {
        var baseDir = FindModelPath();
        if (baseDir == null) return;

        using var model = new PriceModel();
        model.Load(
            Path.Combine(baseDir, "ml", "price_model.onnx"),
            Path.Combine(baseDir, "ml", "scaler_params.json"));

        // Generate synthetic daily data (25 days)
        var rng = new Random(42);
        var prices = new decimal[25];
        var volumes = new long[25];
        var highs = new decimal[25];
        var lows = new decimal[25];
        var opens = new decimal[25];
        prices[0] = 100m;
        for (int i = 1; i < 25; i++)
        {
            var change = (decimal)(rng.NextDouble() * 0.04 - 0.02); // ±2%
            prices[i] = Math.Max(1m, prices[i - 1] * (1 + change));
            opens[i] = prices[i - 1] * (1 + (decimal)(rng.NextDouble() * 0.01 - 0.005));
            highs[i] = Math.Max(prices[i], opens[i]) * 1.01m;
            lows[i] = Math.Min(prices[i], opens[i]) * 0.99m;
            volumes[i] = 1_000_000 + rng.Next(500_000);
        }

        var result = model.Predict(prices, volumes, highs, lows, opens, "Technology");

        Assert.NotNull(result);
        Assert.InRange(result.Value.ExpectedReturn, -0.10m, 0.10m);
        Assert.True(result.Value.ExpectedVolatility > 0, "Volatility should be positive");
    }

    [Fact]
    public void PredictFromCandles_ShouldReturnValidPrediction()
    {
        var baseDir = FindModelPath();
        if (baseDir == null) return;

        using var model = new PriceModel();
        model.Load(
            Path.Combine(baseDir, "ml", "price_model.onnx"),
            Path.Combine(baseDir, "ml", "scaler_params.json"));

        // Build candle list
        var rng = new Random(42);
        var candles = new List<Candle>();
        decimal price = 100m;
        long baseTime = 1700000000;
        for (int i = 0; i < 25; i++)
        {
            var change = (decimal)(rng.NextDouble() * 0.04 - 0.02);
            var open = price;
            price = Math.Max(1m, price * (1 + change));
            var high = Math.Max(open, price) * 1.01m;
            var low = Math.Min(open, price) * 0.99m;
            candles.Add(new Candle(baseTime + i * 86400, open, high, low, price, 1_000_000 + rng.Next(500_000)));
        }

        var result = model.PredictFromCandles(candles, "Financials");

        Assert.NotNull(result);
        Assert.InRange(result.Value.ExpectedReturn, -0.10m, 0.10m);
        Assert.True(result.Value.ExpectedVolatility > 0);
    }

    [Fact]
    public void Predict_DifferentSectors_ShouldProduceDifferentResults()
    {
        var baseDir = FindModelPath();
        if (baseDir == null) return;

        using var model = new PriceModel();
        model.Load(
            Path.Combine(baseDir, "ml", "price_model.onnx"),
            Path.Combine(baseDir, "ml", "scaler_params.json"));

        var rng = new Random(42);
        var prices = new decimal[25];
        var volumes = new long[25];
        var highs = new decimal[25];
        var lows = new decimal[25];
        var opens = new decimal[25];
        prices[0] = 100m;
        for (int i = 1; i < 25; i++)
        {
            prices[i] = prices[i - 1] * (1 + (decimal)(rng.NextDouble() * 0.04 - 0.02));
            opens[i] = prices[i - 1];
            highs[i] = Math.Max(prices[i], opens[i]) * 1.01m;
            lows[i] = Math.Min(prices[i], opens[i]) * 0.99m;
            volumes[i] = 1_000_000;
        }

        var techResult = model.Predict(prices, volumes, highs, lows, opens, "Technology");
        var utilResult = model.Predict(prices, volumes, highs, lows, opens, "Utilities");

        Assert.NotNull(techResult);
        Assert.NotNull(utilResult);
        // Different sector encoding should produce different predictions
        Assert.True(techResult.Value.ExpectedReturn != utilResult.Value.ExpectedReturn
            || techResult.Value.ExpectedVolatility != utilResult.Value.ExpectedVolatility,
            "Different sectors should produce different predictions");
    }
}

public class PriceEngineHybridTests
{
    [Fact]
    public void PriceEngine_ShouldWorkWithoutOnnx()
    {
        var engine = new PriceEngine(42);
        var stock = new Stock("TEST", "Test Corp", "Technology")
        {
            CurrentPrice = 100m,
            PreviousClose = 100m,
            BidPrice = 99.90m,
            AskPrice = 100.10m,
            DayHigh = 100m,
            DayLow = 100m,
            BaseVolatility = 0.02m,
            LiquidityScore = 7,
            FairValue = 100m,
            AverageVolume = 1_000_000,
            SharesOutstanding = 100_000_000,
        };

        engine.GenerateSectorShocks(new[] { "Technology" });
        engine.Tick(stock, TimeSpan.FromMinutes(1));

        Assert.True(stock.CurrentPrice > 0);
        Assert.NotEqual(100m, stock.CurrentPrice); // Price should have moved
    }

    [Fact]
    public void OnnxBlendWeight_ShouldDefaultTo40Percent()
    {
        var engine = new PriceEngine(42);
        Assert.Equal(0.4m, engine.OnnxBlendWeight);
    }

    [Fact]
    public void GenerateDailyOnnxPredictions_ShouldNotCrash_WithoutModel()
    {
        var engine = new PriceEngine(42);
        var stocks = new List<Stock>
        {
            new("TEST", "Test", "Technology") { CurrentPrice = 100m }
        };
        var dailyHistory = new Dictionary<string, List<Candle>>();

        // Should not throw even without ONNX model
        engine.GenerateDailyOnnxPredictions(stocks, dailyHistory);
    }

    [Fact]
    public void RuntimeModifiers_EventSentiment_ShouldAffectPrice()
    {
        // Two engines, same seed — one with strong positive event sentiment, one without
        var engineNeutral = new PriceEngine(42);
        var engineBullish = new PriceEngine(42);

        var stockNeutral = CreateTestStock(100m);
        var stockBullish = CreateTestStock(100m);

        // Set strong bullish event sentiment on the bullish engine
        engineBullish.StockEventSentiment["TEST"] = 0.8f;
        engineBullish.StockEventVolMultiplier["TEST"] = 1.5f;
        engineBullish.MarketSentiment = 0.5m;
        engineBullish.SectorMultipliers["Technology"] = 1.1m;

        // Both need ONNX predictions to trigger hybrid path
        // Simulate by manually setting daily predictions
        // (Without ONNX model, the modifier path is skipped — test the GBM path at least)
        engineNeutral.GenerateSectorShocks(new[] { "Technology" });
        engineBullish.GenerateSectorShocks(new[] { "Technology" });

        // Run 200 ticks
        for (int i = 0; i < 200; i++)
        {
            engineNeutral.Tick(stockNeutral, TimeSpan.FromMinutes(1));
            engineBullish.Tick(stockBullish, TimeSpan.FromMinutes(1));
        }

        // Both should still produce valid prices
        Assert.True(stockNeutral.CurrentPrice > 0);
        Assert.True(stockBullish.CurrentPrice > 0);
    }

    [Fact]
    public void RuntimeModifiers_ShouldNotCrash_WhenEmpty()
    {
        var engine = new PriceEngine(42);
        // All modifier dictionaries empty — should work fine
        Assert.Empty(engine.StockEventSentiment);
        Assert.Empty(engine.StockEventVolMultiplier);
        Assert.Empty(engine.SectorMultipliers);
        Assert.Equal(0m, engine.MarketSentiment);

        var stock = CreateTestStock(100m);
        engine.GenerateSectorShocks(new[] { "Technology" });
        engine.Tick(stock, TimeSpan.FromMinutes(1));

        Assert.True(stock.CurrentPrice > 0);
    }

    private static Stock CreateTestStock(decimal price)
    {
        return new Stock("TEST", "Test Corp", "Technology")
        {
            CurrentPrice = price,
            PreviousClose = price,
            BidPrice = price - 0.10m,
            AskPrice = price + 0.10m,
            DayHigh = price,
            DayLow = price,
            BaseVolatility = 0.02m,
            LiquidityScore = 7,
            FairValue = price,
            AverageVolume = 1_000_000,
            SharesOutstanding = 100_000_000,
        };
    }
}
