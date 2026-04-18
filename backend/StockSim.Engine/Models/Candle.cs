namespace StockSim.Engine.Models;

/// <summary>
/// A single OHLCV candlestick data point.
/// See Spec section 12.1 for chart data format.
/// </summary>
public record Candle(
    long Time,     // Unix timestamp (seconds)
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume
)
{
    public bool IsBullish => Close >= Open;
}

/// <summary>
/// Candlestick time intervals for chart display.
/// </summary>
public enum CandleInterval
{
    OneMinute = 60,
    FiveMinutes = 300,
    FifteenMinutes = 900,
    OneHour = 3600,
    FourHours = 14400,
    OneDay = 86400,
    OneWeek = 604800,
}

/// <summary>
/// Maintains rolling OHLCV candle history for a single stock.
/// Accumulates tick data into candles of the specified interval.
/// See Spec section 12.1 for TradingView data requirements.
/// </summary>
public class PriceHistory
{
    public string Symbol { get; }
    public CandleInterval Interval { get; }
    public List<Candle> Candles { get; } = new();

    private readonly int _maxCandles;
    private decimal _currentOpen;
    private decimal _currentHigh;
    private decimal _currentLow;
    private decimal _currentClose;
    private long _currentVolume;
    private long _currentBucketStart = -1;

    public PriceHistory(string symbol, CandleInterval interval, int maxCandles = 1000)
    {
        Symbol = symbol;
        Interval = interval;
        _maxCandles = maxCandles;
    }

    /// <summary>
    /// Feed a new price tick into the history.
    /// Creates new candles or updates the current one based on the time bucket.
    /// </summary>
    /// <param name="price">Current price</param>
    /// <param name="unixTime">Unix timestamp in seconds</param>
    /// <param name="volume">Tick volume to add</param>
    public void UpdateTick(decimal price, long unixTime, long volume = 0)
    {
        var bucketStart = GetBucketStart(unixTime);

        if (_currentBucketStart == -1)
        {
            // First tick ever
            _currentBucketStart = bucketStart;
            _currentOpen = price;
            _currentHigh = price;
            _currentLow = price;
            _currentClose = price;
            _currentVolume = volume;
            SyncCurrentCandle();
            return;
        }

        if (bucketStart != _currentBucketStart)
        {
            // New time bucket — finalize current candle and start new one
            _currentBucketStart = bucketStart;
            _currentOpen = price;
            _currentHigh = price;
            _currentLow = price;
            _currentClose = price;
            _currentVolume = volume;
            Candles.Add(new Candle(bucketStart, price, price, price, price, volume));

            // Limit history size
            while (Candles.Count > _maxCandles)
            {
                Candles.RemoveAt(0);
            }
        }
        else
        {
            // Same time bucket — update current candle
            if (price > _currentHigh) _currentHigh = price;
            if (price < _currentLow) _currentLow = price;
            _currentClose = price;
            _currentVolume += volume;
            SyncCurrentCandle();
        }
    }

    private void SyncCurrentCandle()
    {
        var updated = new Candle(
            _currentBucketStart,
            _currentOpen,
            _currentHigh,
            _currentLow,
            _currentClose,
            _currentVolume
        );

        if (Candles.Count > 0 && Candles[^1].Time == _currentBucketStart)
        {
            Candles[^1] = updated;
        }
        else
        {
            Candles.Add(updated);
            while (Candles.Count > _maxCandles)
            {
                Candles.RemoveAt(0);
            }
        }
    }

    private long GetBucketStart(long unixTime)
    {
        var interval = (long)Interval;
        return (unixTime / interval) * interval;
    }
}
