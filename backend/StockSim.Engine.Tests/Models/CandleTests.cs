using StockSim.Engine.Models;

namespace StockSim.Engine.Tests.Models;

public class CandleTests
{
    [Fact]
    public void Candle_ShouldStoreOHLCV()
    {
        var candle = new Candle(1000, 100m, 105m, 98m, 103m, 50000);

        Assert.Equal(1000L, candle.Time);
        Assert.Equal(100m, candle.Open);
        Assert.Equal(105m, candle.High);
        Assert.Equal(98m, candle.Low);
        Assert.Equal(103m, candle.Close);
        Assert.Equal(50000L, candle.Volume);
    }

    [Fact]
    public void Candle_ShouldIdentifyBullish()
    {
        var candle = new Candle(1000, 100m, 105m, 99m, 104m, 50000);
        Assert.True(candle.IsBullish);
    }

    [Fact]
    public void Candle_ShouldIdentifyBearish()
    {
        var candle = new Candle(1000, 104m, 106m, 99m, 100m, 50000);
        Assert.False(candle.IsBullish);
    }

    [Fact]
    public void PriceHistory_ShouldAccumulateCandles()
    {
        var history = new PriceHistory("VTXD", CandleInterval.OneMinute);

        history.UpdateTick(100m, 1000);

        Assert.Single(history.Candles);
        Assert.Equal(100m, history.Candles[0].Open);
        Assert.Equal(100m, history.Candles[0].Close);
    }

    [Fact]
    public void PriceHistory_ShouldUpdateCurrentCandle()
    {
        var history = new PriceHistory("VTXD", CandleInterval.OneMinute);

        history.UpdateTick(100m, 1000);
        history.UpdateTick(105m, 1000); // Same time bucket
        history.UpdateTick(98m, 1000);
        history.UpdateTick(102m, 1000);

        Assert.Single(history.Candles);
        var c = history.Candles[0];
        Assert.Equal(100m, c.Open);   // First tick
        Assert.Equal(105m, c.High);   // Highest
        Assert.Equal(98m, c.Low);     // Lowest
        Assert.Equal(102m, c.Close);  // Last tick
    }

    [Fact]
    public void PriceHistory_ShouldCreateNewCandleOnNewTimeBucket()
    {
        var history = new PriceHistory("VTXD", CandleInterval.OneMinute);

        history.UpdateTick(100m, 1000); // Minute 1
        history.UpdateTick(102m, 1060); // Minute 2 (60 seconds later)

        Assert.Equal(2, history.Candles.Count);
        Assert.Equal(100m, history.Candles[0].Close);
        Assert.Equal(102m, history.Candles[1].Open);
    }

    [Fact]
    public void PriceHistory_ShouldAccumulateVolume()
    {
        var history = new PriceHistory("VTXD", CandleInterval.OneMinute);

        history.UpdateTick(100m, 1000, volume: 500);
        history.UpdateTick(101m, 1000, volume: 300);

        Assert.Equal(800, history.Candles[0].Volume);
    }

    [Fact]
    public void PriceHistory_ShouldLimitCandleCount()
    {
        var history = new PriceHistory("VTXD", CandleInterval.OneMinute, maxCandles: 100);

        for (int i = 0; i < 200; i++)
        {
            history.UpdateTick(100m + i * 0.1m, i * 60); // New candle every minute
        }

        Assert.True(history.Candles.Count <= 100);
    }

    [Fact]
    public void PriceHistory_ShouldSupportDailyCandles()
    {
        var history = new PriceHistory("VTXD", CandleInterval.OneDay);

        // Day 1
        history.UpdateTick(100m, 0);
        history.UpdateTick(105m, 100);
        // Day 2 (86400 seconds later)
        history.UpdateTick(106m, 86400);

        Assert.Equal(2, history.Candles.Count);
    }
}
