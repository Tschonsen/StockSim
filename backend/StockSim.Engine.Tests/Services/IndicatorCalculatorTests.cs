using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

public class IndicatorCalculatorTests
{
    private static List<Candle> MakeCandles(params decimal[] closes)
    {
        return closes.Select((c, i) => new Candle(
            Time: i * 86400,
            Open: c - 1,
            High: c + 1,
            Low: c - 1.5m,
            Close: c,
            Volume: 100000
        )).ToList();
    }

    // --- SMA ---

    [Fact]
    public void SMA_ShouldCalculateCorrectAverage()
    {
        var candles = MakeCandles(10, 11, 12, 13, 14);

        var sma = IndicatorCalculator.SMA(candles, 3);

        Assert.Null(sma[0]);
        Assert.Null(sma[1]);
        Assert.Equal(11m, sma[2]); // (10+11+12)/3
        Assert.Equal(12m, sma[3]); // (11+12+13)/3
        Assert.Equal(13m, sma[4]); // (12+13+14)/3
    }

    [Fact]
    public void SMA_InsufficientData_ShouldReturnNulls()
    {
        var candles = MakeCandles(10, 11);

        var sma = IndicatorCalculator.SMA(candles, 5);

        Assert.All(sma, v => Assert.Null(v));
    }

    [Fact]
    public void SMA_Period1_ShouldEqualClose()
    {
        var candles = MakeCandles(10, 20, 30);

        var sma = IndicatorCalculator.SMA(candles, 1);

        Assert.Equal(10m, sma[0]);
        Assert.Equal(20m, sma[1]);
        Assert.Equal(30m, sma[2]);
    }

    // --- EMA ---

    [Fact]
    public void EMA_FirstValue_ShouldEqualSMA()
    {
        var candles = MakeCandles(10, 11, 12, 13, 14);

        var ema = IndicatorCalculator.EMA(candles, 3);

        Assert.Null(ema[0]);
        Assert.Null(ema[1]);
        Assert.Equal(11m, ema[2]); // SMA seed: (10+11+12)/3
    }

    [Fact]
    public void EMA_ShouldReactFasterThanSMA()
    {
        // Rising prices: EMA reacts faster to recent changes
        var candles = MakeCandles(10, 10, 10, 15, 20, 25, 30, 35);

        var ema = IndicatorCalculator.EMA(candles, 3);

        // EMA should have values after period
        Assert.NotNull(ema[2]);

        // Last EMA should be closer to 35 (last close) than the SMA seed
        Assert.True(ema[7]!.Value > 25m,
            $"EMA {ema[7]} should track rising prices closely");
    }

    [Fact]
    public void EMA_InsufficientData_ShouldReturnNulls()
    {
        var candles = MakeCandles(10, 11);

        var ema = IndicatorCalculator.EMA(candles, 5);

        Assert.All(ema, v => Assert.Null(v));
    }

    // --- RSI ---

    [Fact]
    public void RSI_ShouldBeInRange0To100()
    {
        var closes = Enumerable.Range(0, 50).Select(i => 100m + (decimal)Math.Sin(i * 0.5) * 10).ToArray();
        var candles = MakeCandles(closes);

        var rsi = IndicatorCalculator.RSI(candles, 14);

        foreach (var val in rsi.Where(v => v.HasValue))
        {
            Assert.True(val!.Value >= 0 && val.Value <= 100,
                $"RSI {val} should be between 0 and 100");
        }
    }

    [Fact]
    public void RSI_AllUp_ShouldBe100()
    {
        var candles = MakeCandles(10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25);

        var rsi = IndicatorCalculator.RSI(candles, 14);

        Assert.Equal(100m, rsi[14]);
    }

    [Fact]
    public void RSI_AllDown_ShouldBeNearZero()
    {
        var candles = MakeCandles(25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10);

        var rsi = IndicatorCalculator.RSI(candles, 14);

        Assert.True(rsi[14]!.Value < 1m, $"RSI {rsi[14]} should be near 0 for all-down");
    }

    [Fact]
    public void RSI_InsufficientData_ShouldReturnNulls()
    {
        var candles = MakeCandles(10, 11, 12);

        var rsi = IndicatorCalculator.RSI(candles, 14);

        Assert.All(rsi, v => Assert.Null(v));
    }

    // --- MACD ---

    [Fact]
    public void MACD_ShouldReturnThreeArrays()
    {
        var closes = Enumerable.Range(0, 50).Select(i => 100m + i * 0.5m).ToArray();
        var candles = MakeCandles(closes);

        var (macd, signal, histogram) = IndicatorCalculator.MACD(candles);

        Assert.Equal(candles.Count, macd.Length);
        Assert.Equal(candles.Count, signal.Length);
        Assert.Equal(candles.Count, histogram.Length);

        // At least some values should be non-null
        Assert.True(macd.Any(v => v.HasValue), "MACD should have values");
    }

    [Fact]
    public void MACD_Uptrend_ShouldBePositive()
    {
        var closes = Enumerable.Range(0, 50).Select(i => 100m + i * 1m).ToArray();
        var candles = MakeCandles(closes);

        var (macd, _, _) = IndicatorCalculator.MACD(candles);

        var lastMacd = macd.Last(v => v.HasValue);
        Assert.True(lastMacd > 0, $"MACD {lastMacd} should be positive in uptrend");
    }

    // --- Bollinger Bands ---

    [Fact]
    public void Bollinger_MiddleShouldEqualSMA()
    {
        var candles = MakeCandles(Enumerable.Range(0, 30).Select(i => 100m + i * 0.1m).ToArray());

        var (_, middle, _) = IndicatorCalculator.BollingerBands(candles, 20);
        var sma = IndicatorCalculator.SMA(candles, 20);

        for (int i = 0; i < candles.Count; i++)
        {
            Assert.Equal(sma[i], middle[i]);
        }
    }

    [Fact]
    public void Bollinger_UpperShouldBeAboveMiddle()
    {
        var closes = Enumerable.Range(0, 30).Select(i => 100m + (decimal)Math.Sin(i * 0.3) * 5).ToArray();
        var candles = MakeCandles(closes);

        var (upper, middle, lower) = IndicatorCalculator.BollingerBands(candles, 20);

        for (int i = 19; i < candles.Count; i++)
        {
            Assert.True(upper[i] > middle[i], $"Upper {upper[i]} should be > Middle {middle[i]} at {i}");
            Assert.True(lower[i] < middle[i], $"Lower {lower[i]} should be < Middle {middle[i]} at {i}");
        }
    }

    [Fact]
    public void Bollinger_FlatPrice_ShouldHaveNarrowBands()
    {
        var candles = MakeCandles(Enumerable.Repeat(100m, 30).ToArray());

        var (upper, middle, lower) = IndicatorCalculator.BollingerBands(candles, 20);

        // With constant price, bands should converge to the price
        Assert.Equal(100m, upper[29]);
        Assert.Equal(100m, middle[29]);
        Assert.Equal(100m, lower[29]);
    }
}
