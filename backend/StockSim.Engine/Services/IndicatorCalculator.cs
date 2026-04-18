using StockSim.Engine.Models;

namespace StockSim.Engine.Services;

/// <summary>
/// Calculates technical indicators from OHLCV candle data.
/// Spec 12.2.4: SMA, EMA, RSI, MACD, Bollinger Bands.
/// All calculations are stateless — input candles, output indicator values.
/// </summary>
public static class IndicatorCalculator
{
    /// <summary>
    /// Simple Moving Average. Spec 12.2.4: periods 20, 50, 200.
    /// Returns array aligned with input candles (null where insufficient data).
    /// </summary>
    public static decimal?[] SMA(IReadOnlyList<Candle> candles, int period)
    {
        var result = new decimal?[candles.Count];
        if (candles.Count < period) return result;

        decimal sum = 0;
        for (int i = 0; i < period; i++)
            sum += candles[i].Close;

        result[period - 1] = Math.Round(sum / period, 2);

        for (int i = period; i < candles.Count; i++)
        {
            sum += candles[i].Close - candles[i - period].Close;
            result[i] = Math.Round(sum / period, 2);
        }

        return result;
    }

    /// <summary>
    /// Exponential Moving Average. Spec 12.2.4: default period 12.
    /// </summary>
    public static decimal?[] EMA(IReadOnlyList<Candle> candles, int period)
    {
        var result = new decimal?[candles.Count];
        if (candles.Count < period) return result;

        // Seed with SMA
        decimal sum = 0;
        for (int i = 0; i < period; i++)
            sum += candles[i].Close;

        var ema = sum / period;
        result[period - 1] = Math.Round(ema, 2);

        var multiplier = 2m / (period + 1);
        for (int i = period; i < candles.Count; i++)
        {
            ema = (candles[i].Close - ema) * multiplier + ema;
            result[i] = Math.Round(ema, 2);
        }

        return result;
    }

    /// <summary>
    /// Relative Strength Index. Spec 12.2.4: default period 14.
    /// Returns values 0-100.
    /// </summary>
    public static decimal?[] RSI(IReadOnlyList<Candle> candles, int period = 14)
    {
        var result = new decimal?[candles.Count];
        if (candles.Count < period + 1) return result;

        // Calculate initial average gain/loss
        decimal avgGain = 0, avgLoss = 0;
        for (int i = 1; i <= period; i++)
        {
            var change = candles[i].Close - candles[i - 1].Close;
            if (change > 0) avgGain += change;
            else avgLoss += Math.Abs(change);
        }

        avgGain /= period;
        avgLoss /= period;

        if (avgLoss == 0)
            result[period] = 100m;
        else
        {
            var rs = avgGain / avgLoss;
            result[period] = Math.Round(100m - (100m / (1m + rs)), 2);
        }

        // Smoothed RSI for remaining candles
        for (int i = period + 1; i < candles.Count; i++)
        {
            var change = candles[i].Close - candles[i - 1].Close;
            var gain = change > 0 ? change : 0;
            var loss = change < 0 ? Math.Abs(change) : 0;

            avgGain = (avgGain * (period - 1) + gain) / period;
            avgLoss = (avgLoss * (period - 1) + loss) / period;

            if (avgLoss == 0)
                result[i] = 100m;
            else
            {
                var rs = avgGain / avgLoss;
                result[i] = Math.Round(100m - (100m / (1m + rs)), 2);
            }
        }

        return result;
    }

    /// <summary>
    /// MACD (Moving Average Convergence Divergence).
    /// Spec 12.2.4: MACD line (EMA12 - EMA26), Signal line (EMA9 of MACD), Histogram.
    /// </summary>
    public static (decimal?[] macdLine, decimal?[] signalLine, decimal?[] histogram) MACD(
        IReadOnlyList<Candle> candles, int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9)
    {
        var count = candles.Count;
        var macdLine = new decimal?[count];
        var signalLine = new decimal?[count];
        var histogram = new decimal?[count];

        if (count < slowPeriod + signalPeriod) return (macdLine, signalLine, histogram);

        var fastEma = EMA(candles, fastPeriod);
        var slowEma = EMA(candles, slowPeriod);

        // MACD Line = Fast EMA - Slow EMA
        for (int i = 0; i < count; i++)
        {
            if (fastEma[i].HasValue && slowEma[i].HasValue)
                macdLine[i] = Math.Round(fastEma[i]!.Value - slowEma[i]!.Value, 4);
        }

        // Signal Line = EMA of MACD Line
        // Build pseudo-candles from MACD values for EMA calculation
        var macdValues = new List<int>();
        for (int i = 0; i < count; i++)
        {
            if (macdLine[i].HasValue) macdValues.Add(i);
        }

        if (macdValues.Count >= signalPeriod)
        {
            // Simple EMA over MACD values
            decimal sum = 0;
            for (int j = 0; j < signalPeriod; j++)
                sum += macdLine[macdValues[j]]!.Value;

            var signal = sum / signalPeriod;
            signalLine[macdValues[signalPeriod - 1]] = Math.Round(signal, 4);

            var mult = 2m / (signalPeriod + 1);
            for (int j = signalPeriod; j < macdValues.Count; j++)
            {
                signal = (macdLine[macdValues[j]]!.Value - signal) * mult + signal;
                signalLine[macdValues[j]] = Math.Round(signal, 4);
            }
        }

        // Histogram = MACD - Signal
        for (int i = 0; i < count; i++)
        {
            if (macdLine[i].HasValue && signalLine[i].HasValue)
                histogram[i] = Math.Round(macdLine[i]!.Value - signalLine[i]!.Value, 4);
        }

        return (macdLine, signalLine, histogram);
    }

    /// <summary>
    /// Bollinger Bands. Spec 12.2.4: period 20, 2 standard deviations.
    /// Returns (middle = SMA, upper = SMA + 2*StdDev, lower = SMA - 2*StdDev).
    /// </summary>
    public static (decimal?[] upper, decimal?[] middle, decimal?[] lower) BollingerBands(
        IReadOnlyList<Candle> candles, int period = 20, decimal stdDevMultiplier = 2m)
    {
        var count = candles.Count;
        var upper = new decimal?[count];
        var middle = SMA(candles, period);
        var lower = new decimal?[count];

        for (int i = period - 1; i < count; i++)
        {
            if (!middle[i].HasValue) continue;

            // Calculate standard deviation
            decimal sumSqDiff = 0;
            for (int j = i - period + 1; j <= i; j++)
            {
                var diff = candles[j].Close - middle[i]!.Value;
                sumSqDiff += diff * diff;
            }

            var stdDev = (decimal)Math.Sqrt((double)(sumSqDiff / period));
            var band = stdDev * stdDevMultiplier;

            upper[i] = Math.Round(middle[i]!.Value + band, 2);
            lower[i] = Math.Round(middle[i]!.Value - band, 2);
        }

        return (upper, middle, lower);
    }

    /// <summary>
    /// VWAP (Volume Weighted Average Price). Spec 12.2.4.
    /// Cumulative (price × volume) / cumulative volume. Resets at market open each day.
    /// </summary>
    public static decimal?[] VWAP(IReadOnlyList<Candle> candles)
    {
        var count = candles.Count;
        var result = new decimal?[count];
        if (count == 0) return result;

        decimal cumulativePV = 0;
        decimal cumulativeVol = 0;
        long lastDay = 0;

        for (int i = 0; i < count; i++)
        {
            var c = candles[i];
            // Detect day change — reset VWAP
            var dayKey = c.Time / 86400; // Unix day
            if (dayKey != lastDay)
            {
                cumulativePV = 0;
                cumulativeVol = 0;
                lastDay = dayKey;
            }

            var typicalPrice = (c.High + c.Low + c.Close) / 3m;
            cumulativePV += typicalPrice * c.Volume;
            cumulativeVol += c.Volume;

            result[i] = cumulativeVol > 0 ? Math.Round(cumulativePV / cumulativeVol, 2) : null;
        }

        return result;
    }
}
