using System.Text.Json;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// ONNX-based price prediction model. Trained on 151 real stocks (Yahoo Finance, 5 years).
/// Predicts expected daily return and volatility per stock.
///
/// Input (103 floats):
///   - 20 days × 5 features (return, volume_ratio, hl_range, gap, close_position) = 100
///   - 3 static features (sector bucket: growth/defensive/cyclical)
///
/// Output (2 floats):
///   - expected_return: predicted next-day return
///   - expected_volatility: predicted next-day absolute return (volatility proxy)
/// </summary>
public class PriceModel : IDisposable
{
    private readonly Logger _log = new("PriceModel");
    private InferenceSession? _session;
    private float[]? _seqMean, _seqScale, _statMean, _statScale;
    private bool _loaded;

    public const int LOOKBACK = 20;
    public const int FEATURES_PER_DAY = 5;
    public const int STATIC_FEATURES = 3;
    public const int TOTAL_INPUT = LOOKBACK * FEATURES_PER_DAY + STATIC_FEATURES; // 103

    /// <summary>Whether the ONNX model loaded successfully.</summary>
    public bool IsLoaded => _loaded;

    // Sector bucket encoding: growth / defensive / cyclical
    private static readonly Dictionary<string, float[]> SectorBuckets = new()
    {
        ["Technology"] = [1f, 0f, 0f],
        ["Healthcare"] = [0.5f, 0.5f, 0f],
        ["Financials"] = [0f, 0f, 1f],
        ["Consumer Goods"] = [0f, 1f, 0f],
        ["Energy"] = [0f, 0f, 1f],
        ["Industrials"] = [0f, 0.3f, 0.7f],
        ["Real Estate"] = [0f, 0.7f, 0.3f],
        ["Utilities"] = [0f, 1f, 0f],
        ["Materials"] = [0f, 0.3f, 0.7f],
        ["Telecommunications"] = [0f, 0.7f, 0.3f],
        ["Automotive"] = [0.7f, 0f, 0.3f],
        ["Aerospace"] = [0.3f, 0f, 0.7f],
    };

    /// <summary>
    /// Load ONNX model and scaler parameters from disk.
    /// </summary>
    /// <param name="modelPath">Path to price_model.onnx</param>
    /// <param name="scalerPath">Path to scaler_params.json</param>
    public bool Load(string modelPath, string scalerPath)
    {
        try
        {
            if (!File.Exists(modelPath))
            {
                _log.Warn("ONNX model not found, using GBM fallback", new { path = modelPath });
                return false;
            }
            if (!File.Exists(scalerPath))
            {
                _log.Warn("Scaler params not found, using GBM fallback", new { path = scalerPath });
                return false;
            }

            // Load scaler params
            var json = File.ReadAllText(scalerPath);
            var scalerData = JsonSerializer.Deserialize<Dictionary<string, float[]>>(json);
            if (scalerData == null) return false;

            _seqMean = scalerData["seq_mean"];
            _seqScale = scalerData["seq_scale"];
            _statMean = scalerData["stat_mean"];
            _statScale = scalerData["stat_scale"];

            // Load ONNX model
            var options = new SessionOptions();
            options.InterOpNumThreads = 1;
            options.IntraOpNumThreads = 1;
            _session = new InferenceSession(modelPath, options);

            _loaded = true;
            _log.Info("ONNX price model loaded", new { modelPath, seqFeatures = _seqMean.Length, statFeatures = _statMean.Length });
            return true;
        }
        catch (Exception ex)
        {
            _log.Error("Failed to load ONNX model", new { error = ex.Message });
            _loaded = false;
            return false;
        }
    }

    /// <summary>
    /// Predict expected return and volatility for a stock.
    /// </summary>
    /// <param name="priceHistory">Recent daily close prices (newest last, at least LOOKBACK+1 entries)</param>
    /// <param name="volumeHistory">Recent daily volumes (newest last, at least LOOKBACK entries)</param>
    /// <param name="highHistory">Recent daily highs</param>
    /// <param name="lowHistory">Recent daily lows</param>
    /// <param name="openHistory">Recent daily opens</param>
    /// <param name="sector">Stock sector name</param>
    /// <returns>(expectedReturn, expectedVolatility) or null if prediction fails</returns>
    public (decimal ExpectedReturn, decimal ExpectedVolatility)? Predict(
        decimal[] priceHistory,
        long[] volumeHistory,
        decimal[] highHistory,
        decimal[] lowHistory,
        decimal[] openHistory,
        string sector)
    {
        if (!_loaded || _session == null) return null;
        if (priceHistory.Length < LOOKBACK + 1) return null;

        try
        {
            var input = BuildInput(priceHistory, volumeHistory, highHistory, lowHistory, openHistory, sector);
            if (input == null) return null;

            var tensor = new DenseTensor<float>(input, new[] { 1, TOTAL_INPUT });
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("input", tensor) };

            using var results = _session.Run(inputs);
            var output = results.First().AsTensor<float>();

            var expectedReturn = (decimal)Math.Clamp(output[0, 0], -0.10f, 0.10f);
            var expectedVol = (decimal)Math.Max(output[0, 1], 0.001f);

            return (expectedReturn, expectedVol);
        }
        catch (Exception ex)
        {
            _log.Debug("ONNX prediction failed, falling back to GBM", new { error = ex.Message });
            return null;
        }
    }

    /// <summary>
    /// Build normalized input array from raw stock data.
    /// </summary>
    private float[]? BuildInput(
        decimal[] prices, long[] volumes, decimal[] highs, decimal[] lows, decimal[] opens,
        string sector)
    {
        if (_seqMean == null || _seqScale == null || _statMean == null || _statScale == null) return null;

        var input = new float[TOTAL_INPUT];
        var n = prices.Length;

        // Calculate 20-day average volume for volume ratio
        long avgVol = 0;
        for (int i = Math.Max(0, n - LOOKBACK - 1); i < n; i++)
            avgVol += volumes[Math.Min(i, volumes.Length - 1)];
        avgVol = Math.Max(avgVol / LOOKBACK, 1);

        // Build sequence features (20 days)
        for (int day = 0; day < LOOKBACK; day++)
        {
            int idx = n - LOOKBACK - 1 + day; // -1 because we need prev close for return
            int idxPrev = idx - 1;
            if (idx < 0 || idxPrev < 0 || idx >= n) continue;

            var close = (float)prices[idx];
            var prevClose = (float)prices[idxPrev];
            var high = (float)highs[Math.Min(idx, highs.Length - 1)];
            var low = (float)lows[Math.Min(idx, lows.Length - 1)];
            var open_ = (float)opens[Math.Min(idx, opens.Length - 1)];
            var vol = volumes[Math.Min(idx, volumes.Length - 1)];

            var dailyReturn = prevClose > 0 ? (close - prevClose) / prevClose : 0f;
            var volumeRatio = avgVol > 0 ? (float)vol / avgVol : 1f;
            var hlRange = close > 0 ? (high - low) / close : 0f;
            var gap = prevClose > 0 ? (open_ - prevClose) / prevClose : 0f;
            var dayRange = high - low;
            var closePos = dayRange > 0 ? (close - low) / dayRange : 0.5f;

            int baseIdx = day * FEATURES_PER_DAY;
            input[baseIdx + 0] = dailyReturn;
            input[baseIdx + 1] = volumeRatio;
            input[baseIdx + 2] = hlRange;
            input[baseIdx + 3] = gap;
            input[baseIdx + 4] = closePos;
        }

        // Static features: sector bucket
        var bucket = SectorBuckets.GetValueOrDefault(sector, new[] { 0.33f, 0.33f, 0.33f });
        int statBase = LOOKBACK * FEATURES_PER_DAY;
        input[statBase + 0] = bucket[0];
        input[statBase + 1] = bucket[1];
        input[statBase + 2] = bucket[2];

        // Normalize using scaler params
        for (int i = 0; i < _seqMean.Length && i < statBase; i++)
        {
            var scale = _seqScale[i] > 0 ? _seqScale[i] : 1f;
            input[i] = (input[i] - _seqMean[i]) / scale;
        }
        for (int i = 0; i < _statMean.Length; i++)
        {
            var scale = _statScale[i] > 0 ? _statScale[i] : 1f;
            input[statBase + i] = (input[statBase + i] - _statMean[i]) / scale;
        }

        return input;
    }

    /// <summary>
    /// Convenience: predict from daily candle history.
    /// </summary>
    public (decimal ExpectedReturn, decimal ExpectedVolatility)? PredictFromCandles(
        IReadOnlyList<Candle> dailyCandles, string sector)
    {
        if (dailyCandles.Count < LOOKBACK + 1) return null;

        var prices = dailyCandles.Select(c => c.Close).ToArray();
        var volumes = dailyCandles.Select(c => c.Volume).ToArray();
        var highs = dailyCandles.Select(c => c.High).ToArray();
        var lows = dailyCandles.Select(c => c.Low).ToArray();
        var opens = dailyCandles.Select(c => c.Open).ToArray();

        return Predict(prices, volumes, highs, lows, opens, sector);
    }

    public void Dispose()
    {
        _session?.Dispose();
        _session = null;
        _loaded = false;
    }
}
