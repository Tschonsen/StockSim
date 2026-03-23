using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

public class HistoryGeneratorTests
{
    private readonly HistoryGenerator _generator;

    public HistoryGeneratorTests()
    {
        _generator = new HistoryGenerator(seed: 42);
    }

    [Fact]
    public void Generate_ShouldReturn252Candles()
    {
        var stock = CreateTestStock(100m);

        var candles = _generator.GenerateDaily(stock, GameStartTime(), MarketPhase.Neutral);

        Assert.Equal(252, candles.Count);
    }

    [Fact]
    public void Generate_LastCandleClose_ShouldMatchCurrentPrice()
    {
        var stock = CreateTestStock(150m);

        var candles = _generator.GenerateDaily(stock, GameStartTime(), MarketPhase.Neutral);

        // Last candle close should be very close to stock's current price
        var lastClose = candles[^1].Close;
        var deviation = Math.Abs(lastClose - stock.CurrentPrice) / stock.CurrentPrice;
        Assert.True(deviation < 0.01m,
            $"Last close {lastClose} should be within 1% of {stock.CurrentPrice}, deviation={deviation:P2}");
    }

    [Fact]
    public void Generate_CandlesShouldBeChronological()
    {
        var stock = CreateTestStock(100m);

        var candles = _generator.GenerateDaily(stock, GameStartTime(), MarketPhase.Bull);

        for (int i = 1; i < candles.Count; i++)
        {
            Assert.True(candles[i].Time > candles[i - 1].Time,
                $"Candle {i} time {candles[i].Time} should be after candle {i - 1} time {candles[i - 1].Time}");
        }
    }

    [Fact]
    public void Generate_CandlesShouldHaveValidOHLC()
    {
        var stock = CreateTestStock(100m);

        var candles = _generator.GenerateDaily(stock, GameStartTime(), MarketPhase.Neutral);

        Assert.All(candles, c =>
        {
            Assert.True(c.High >= c.Low, $"High {c.High} should be >= Low {c.Low}");
            Assert.True(c.High >= c.Open, $"High {c.High} should be >= Open {c.Open}");
            Assert.True(c.High >= c.Close, $"High {c.High} should be >= Close {c.Close}");
            Assert.True(c.Low <= c.Open, $"Low {c.Low} should be <= Open {c.Open}");
            Assert.True(c.Low <= c.Close, $"Low {c.Low} should be <= Close {c.Close}");
            Assert.True(c.Open > 0, $"Open should be positive, got {c.Open}");
            Assert.True(c.Close > 0, $"Close should be positive, got {c.Close}");
            Assert.True(c.Volume > 0, $"Volume should be positive, got {c.Volume}");
        });
    }

    [Fact]
    public void Generate_BullMarket_ShouldShowUptrend()
    {
        var stock = CreateTestStock(100m);

        var candles = _generator.GenerateDaily(stock, GameStartTime(), MarketPhase.Bull);

        // In a bull market, early prices should generally be lower than late prices
        var firstQuarterAvg = candles.Take(63).Average(c => (double)c.Close);
        var lastQuarterAvg = candles.Skip(189).Average(c => (double)c.Close);

        Assert.True(lastQuarterAvg > firstQuarterAvg,
            $"Bull market: last quarter avg {lastQuarterAvg:F2} should be > first quarter avg {firstQuarterAvg:F2}");
    }

    [Fact]
    public void Generate_BearMarket_ShouldShowDowntrend()
    {
        var stock = CreateTestStock(100m);

        var candles = _generator.GenerateDaily(stock, GameStartTime(), MarketPhase.Bear);

        // In a bear market, early prices should generally be higher than late prices
        var firstQuarterAvg = candles.Take(63).Average(c => (double)c.Close);
        var lastQuarterAvg = candles.Skip(189).Average(c => (double)c.Close);

        Assert.True(lastQuarterAvg < firstQuarterAvg,
            $"Bear market: last quarter avg {lastQuarterAvg:F2} should be < first quarter avg {firstQuarterAvg:F2}");
    }

    [Fact]
    public void Generate_ShouldBeWeekdaysOnly()
    {
        var stock = CreateTestStock(100m);

        var candles = _generator.GenerateDaily(stock, GameStartTime(), MarketPhase.Neutral);

        Assert.All(candles, c =>
        {
            var date = DateTimeOffset.FromUnixTimeSeconds(c.Time).DateTime;
            Assert.True(date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday,
                $"Candle at {date:yyyy-MM-dd} is on a weekend ({date.DayOfWeek})");
        });
    }

    [Fact]
    public void Generate_SameSeed_ShouldProduceSameResults()
    {
        var gen1 = new HistoryGenerator(seed: 123);
        var gen2 = new HistoryGenerator(seed: 123);
        var stock1 = CreateTestStock(100m);
        var stock2 = CreateTestStock(100m);

        var candles1 = gen1.GenerateDaily(stock1, GameStartTime(), MarketPhase.Bull);
        var candles2 = gen2.GenerateDaily(stock2, GameStartTime(), MarketPhase.Bull);

        Assert.Equal(candles1.Count, candles2.Count);
        for (int i = 0; i < candles1.Count; i++)
        {
            Assert.Equal(candles1[i].Close, candles2[i].Close);
        }
    }

    [Fact]
    public void Generate_AllPricesShouldBePositive()
    {
        // Test with a very cheap penny stock to make sure prices never go to zero
        var stock = CreateTestStock(1.50m, volatility: 0.04m);

        var candles = _generator.GenerateDaily(stock, GameStartTime(), MarketPhase.Bear);

        Assert.All(candles, c =>
        {
            Assert.True(c.Close > 0, $"Close should be positive, got {c.Close}");
            Assert.True(c.Open > 0, $"Open should be positive, got {c.Open}");
            Assert.True(c.Low > 0, $"Low should be positive, got {c.Low}");
        });
    }

    [Fact]
    public void Generate_VolumeShouldVaryButBeRealistic()
    {
        var stock = CreateTestStock(100m);
        stock.AverageVolume = 1_000_000;

        var candles = _generator.GenerateDaily(stock, GameStartTime(), MarketPhase.Neutral);

        var avgVolume = candles.Average(c => c.Volume);
        Assert.True(avgVolume > 500_000, $"Average volume {avgVolume} should be around the stock's average");
        Assert.True(avgVolume < 2_000_000, $"Average volume {avgVolume} should not be too far from average");
    }

    [Fact]
    public void Generate_LastCandleTime_ShouldBeBeforeGameStart()
    {
        var stock = CreateTestStock(100m);
        var gameStart = GameStartTime();

        var candles = _generator.GenerateDaily(stock, gameStart, MarketPhase.Neutral);

        var lastCandleTime = DateTimeOffset.FromUnixTimeSeconds(candles[^1].Time).DateTime;
        Assert.True(lastCandleTime < gameStart,
            $"Last candle {lastCandleTime} should be before game start {gameStart}");
    }

    [Fact]
    public void Generate_ExpensiveStock_ShouldWork()
    {
        var stock = CreateTestStock(3500m);

        var candles = _generator.GenerateDaily(stock, GameStartTime(), MarketPhase.Bull);

        Assert.Equal(252, candles.Count);
        Assert.All(candles, c => Assert.True(c.Close > 0));
    }

    // --- Helpers ---

    private static DateTime GameStartTime() => new(2027, 1, 4, 9, 0, 0);

    private static Stock CreateTestStock(decimal price, decimal volatility = 0.02m)
    {
        return new Stock("TEST", "Test Corp", "Technology")
        {
            CurrentPrice = price,
            PreviousClose = price,
            FairValue = price,
            BaseVolatility = volatility,
            LiquidityScore = 5,
            AverageVolume = 1_000_000,
            SharesOutstanding = 100_000_000,
        };
    }
}
