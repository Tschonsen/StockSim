using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// Generates historical OHLCV price data for stocks at game start.
/// Creates 252 trading days (1 year) of daily candles backwards from the game start time.
/// Uses a simplified price model (GBM + drift, no events) per Spec 11.4.
///
/// Algorithm:
///   1. Calculate a starting price 252 days ago based on market phase
///   2. Simulate forward 252 days with daily GBM
///   3. Scale the series so the final close matches stock.CurrentPrice
///   4. Generate realistic OHLC wicks and volume for each day
/// </summary>
public class HistoryGenerator
{
    private readonly Random _rng;
    private readonly Logger _log = new("HistoryGenerator");
    private double? _spareNormal;

    public HistoryGenerator(int seed)
    {
        _rng = new Random(seed);
    }

    /// <summary>
    /// Generate 252 daily OHLCV candles ending just before game start.
    /// </summary>
    public List<Candle> GenerateDaily(Stock stock, DateTime gameStartTime, MarketPhase phase, int tradingDays = 252)
    {
        // 1. Build list of trading day timestamps (weekdays only, going backwards)
        var tradingDates = GetTradingDates(gameStartTime, tradingDays);

        // 2. Determine annual return based on market phase
        // Aligned with live PriceEngine drift (~7.5% base + phase modifier)
        // This ensures the historical chart transitions smoothly into live simulation
        var annualReturn = phase switch
        {
            MarketPhase.Bull => 0.08 + _rng.NextDouble() * 0.20,    // +8% to +28%
            MarketPhase.Neutral => -0.05 + _rng.NextDouble() * 0.20, // -5% to +15% (avg ~5%)
            MarketPhase.Bear => -0.20 + _rng.NextDouble() * 0.15,   // -20% to -5%
            _ => 0.0,
        };

        // 3. Generate daily returns via simplified GBM
        var dailyVolatility = (double)stock.BaseVolatility;
        var dailyDrift = annualReturn / tradingDays;
        var closes = new double[tradingDays];

        // Start from an arbitrary price (we'll scale later)
        closes[0] = 100.0;
        for (int i = 1; i < tradingDays; i++)
        {
            var randomReturn = NextNormal() * dailyVolatility;
            var dailyReturn = dailyDrift + randomReturn;
            closes[i] = closes[i - 1] * (1.0 + dailyReturn);
            closes[i] = Math.Max(closes[i], 0.001); // Floor
        }

        // 4. Scale entire series so last close = stock.CurrentPrice
        var scaleFactor = (double)stock.CurrentPrice / closes[^1];
        for (int i = 0; i < tradingDays; i++)
        {
            closes[i] *= scaleFactor;
        }

        // 5. Build OHLCV candles
        var candles = new List<Candle>(tradingDays);
        var avgVolume = stock.AverageVolume > 0 ? stock.AverageVolume : 100_000;

        for (int i = 0; i < tradingDays; i++)
        {
            var close = Math.Round((decimal)closes[i], 2);
            var prevClose = i > 0 ? Math.Round((decimal)closes[i - 1], 2) : close;

            // Open: small gap from previous close (±0.5%)
            var gapPercent = (decimal)(NextNormal() * 0.005);
            var open = Math.Round(prevClose * (1m + gapPercent), 2);
            open = Math.Max(open, 0.01m);

            // Wicks: extend beyond open/close
            var wickSize = (decimal)(Math.Abs(NextNormal()) * (double)stock.BaseVolatility * 0.5);
            var high = Math.Max(open, close) * (1m + wickSize);
            var low = Math.Min(open, close) * (1m - wickSize);
            high = Math.Round(high, 2);
            low = Math.Round(Math.Max(low, 0.01m), 2);

            // Ensure OHLC consistency
            high = Math.Max(high, Math.Max(open, close));
            low = Math.Min(low, Math.Min(open, close));

            // Volume: random around average with ±50% variation
            var volVariation = 0.5 + _rng.NextDouble();  // 0.5x to 1.5x
            var volume = (long)(avgVolume * volVariation);
            volume = Math.Max(volume, 1);

            var unixTime = new DateTimeOffset(tradingDates[i], TimeSpan.Zero).ToUnixTimeSeconds();
            candles.Add(new Candle(unixTime, open, high, low, close, volume));
        }

        _log.Info("Historical prices generated", new
        {
            symbol = stock.Symbol,
            phase = phase.ToString(),
            days = candles.Count,
            startPrice = candles[0].Open,
            endPrice = candles[^1].Close,
            annualReturn = $"{annualReturn:P1}",
        });

        return candles;
    }

    /// <summary>
    /// Determine market phase from seed (Spec 11.4: Bull 40%, Neutral 40%, Bear 20%).
    /// </summary>
    public static MarketPhase DeterminePhase(int seed)
    {
        var rng = new Random(seed);
        var roll = rng.NextDouble();
        return roll switch
        {
            < 0.40 => MarketPhase.Bull,
            < 0.80 => MarketPhase.Neutral,
            _ => MarketPhase.Bear,
        };
    }

    private List<DateTime> GetTradingDates(DateTime gameStartTime, int count)
    {
        var dates = new List<DateTime>(count);
        // Go backwards from the last trading day before game start
        var date = gameStartTime.Date.AddDays(-1);

        // Find trading days going backwards
        var backwardDates = new List<DateTime>();
        while (backwardDates.Count < count)
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                backwardDates.Add(date);
            }
            date = date.AddDays(-1);
        }

        // Reverse to chronological order
        backwardDates.Reverse();
        return backwardDates;
    }

    private double NextNormal()
    {
        if (_spareNormal.HasValue)
        {
            var spare = _spareNormal.Value;
            _spareNormal = null;
            return spare;
        }

        double u, v, s;
        do
        {
            u = _rng.NextDouble() * 2.0 - 1.0;
            v = _rng.NextDouble() * 2.0 - 1.0;
            s = u * u + v * v;
        } while (s >= 1.0 || s == 0.0);

        s = Math.Sqrt(-2.0 * Math.Log(s) / s);
        _spareNormal = v * s;
        return u * s;
    }
}
