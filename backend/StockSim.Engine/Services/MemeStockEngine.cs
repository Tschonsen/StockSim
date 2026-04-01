using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// Emergent meme stock dynamics. Not scripted — triggered organically when
/// high short interest + low price + weak fundamentals + retail catalyst align.
///
/// Lifecycle (10-30 days):
///   1. Discovery (2-3 days): Social media buzz, +5-10%/day, volume 3x
///   2. FOMO (3-5 days): Exponential, +10-30%/day, volume 10x
///   3. Squeeze (1-3 days): Short covering, +30-100%, halts possible
///   4. Diamond Hands (2-5 days): Plateau, extreme volatility ±15%/day
///   5. Crash (3-7 days): Profit-taking, -50-80% from peak
///
/// Max 1-2 per game year. Interacts with GEX, margin cascade, circuit breakers.
/// </summary>
public class MemeStockEngine
{
    private readonly Logger _log = new("MemeStock");
    private readonly Random _rng;

    /// <summary>Currently active meme stock events.</summary>
    public List<MemeStockEvent> ActiveEvents { get; } = new();

    /// <summary>News events generated this tick.</summary>
    public List<MemeStockNews> NewsThisTick { get; } = new();

    /// <summary>Per-stock price pressure from meme dynamics. Key: symbol.</summary>
    public Dictionary<string, decimal> MemePressure { get; } = new();

    private int _daysSinceLastMeme;
    private int _memeCountThisYear;
    private const int MinDaysBetweenMemes = 60; // ~3 months cooldown
    private const int MaxMemesPerYear = 2;

    public MemeStockEngine(int seed)
    {
        _rng = new Random(seed);
        _daysSinceLastMeme = 30; // Allow first meme after ~30 days
    }

    /// <summary>
    /// Called daily. Scans for meme stock candidates and advances active events.
    /// </summary>
    public void TickDay(IReadOnlyList<Stock> stocks, DateTime gameTime, float retailSentiment)
    {
        NewsThisTick.Clear();
        MemePressure.Clear();
        _daysSinceLastMeme++;

        // Advance active meme events
        for (int i = ActiveEvents.Count - 1; i >= 0; i--)
        {
            var evt = ActiveEvents[i];
            evt.DaysInPhase++;
            AdvancePhase(evt, stocks, gameTime);

            if (evt.Phase == MemePhase.Completed)
            {
                _log.Info("Meme stock event completed", new { symbol = evt.Symbol, peakPrice = evt.PeakPrice, startPrice = evt.StartPrice });
                ActiveEvents.RemoveAt(i);
            }
            else
            {
                ApplyMemePressure(evt, stocks);
            }
        }

        // Check for new meme candidates (only if cooldown expired + not too many)
        if (_daysSinceLastMeme >= MinDaysBetweenMemes && _memeCountThisYear < MaxMemesPerYear && ActiveEvents.Count == 0)
        {
            TryScanForMemeCandidate(stocks, retailSentiment, gameTime);
        }

        // Reset yearly counter
        if (gameTime.DayOfYear == 1)
            _memeCountThisYear = 0;
    }

    private void TryScanForMemeCandidate(IReadOnlyList<Stock> stocks, float retailSentiment, DateTime gameTime)
    {
        // Need elevated retail sentiment as catalyst
        if (retailSentiment < 0.1f) return;

        // Base probability: 0.5% per day when conditions are right
        if (_rng.NextDouble() > 0.005 * (1 + retailSentiment)) return;

        // Find candidates: high short interest + low price + weak fundamentals
        var candidates = stocks
            .Where(s => !s.Traits.Contains("ETF")
                && s.ShortInterestOfFloat > 0.20m   // >20% short
                && s.CurrentPrice < 30m              // "affordable"
                && (s.NetIncome < 0 || s.DebtToEquity > 2.0m)) // weak fundamentals
            .OrderByDescending(s => s.ShortInterestOfFloat)
            .Take(5)
            .ToList();

        if (candidates.Count == 0) return;

        // Pick the stock with highest short interest (most squeeze potential)
        var target = candidates[_rng.Next(Math.Min(3, candidates.Count))];

        var evt = new MemeStockEvent
        {
            Symbol = target.Symbol,
            StockName = target.Name,
            Phase = MemePhase.Discovery,
            StartPrice = target.CurrentPrice,
            PeakPrice = target.CurrentPrice,
            ShortInterestAtStart = target.ShortInterestOfFloat,
            StartDate = gameTime,
            DaysInPhase = 0,
            // Squeeze strength depends on short interest: higher SI = bigger squeeze
            SqueezeIntensity = Math.Min(3.0m, target.ShortInterestOfFloat / 0.15m),
            // 30% chance squeeze fails (shorts hold through)
            WillSqueezeFail = _rng.NextDouble() < 0.30,
        };

        ActiveEvents.Add(evt);
        _daysSinceLastMeme = 0;
        _memeCountThisYear++;

        NewsThisTick.Add(new MemeStockNews
        {
            Symbol = target.Symbol,
            Headline = $"Social media buzz building around {target.Name} ({target.Symbol}) — retail traders flag {target.ShortInterestOfFloat:P0} short interest",
            Summary = $"Online trading communities have identified {target.Name} as a potential short squeeze candidate. With {target.ShortInterestOfFloat:P0} of the float sold short and shares trading at ${target.CurrentPrice:F2}, retail buying momentum is building.",
            Phase = MemePhase.Discovery,
            Severity = "Moderate",
        });

        _log.Info("Meme stock event triggered", new
        {
            symbol = target.Symbol,
            price = target.CurrentPrice,
            shortInterest = target.ShortInterestOfFloat,
            squeezeIntensity = evt.SqueezeIntensity,
            willFail = evt.WillSqueezeFail,
        });
    }

    private void AdvancePhase(MemeStockEvent evt, IReadOnlyList<Stock> stocks, DateTime gameTime)
    {
        var stock = stocks.FirstOrDefault(s => s.Symbol == evt.Symbol);
        if (stock == null) { evt.Phase = MemePhase.Completed; return; }

        // Track peak
        if (stock.CurrentPrice > evt.PeakPrice)
            evt.PeakPrice = stock.CurrentPrice;

        switch (evt.Phase)
        {
            case MemePhase.Discovery:
                if (evt.DaysInPhase >= 2 + _rng.Next(2)) // 2-3 days
                {
                    evt.Phase = MemePhase.FOMO;
                    evt.DaysInPhase = 0;
                    NewsThisTick.Add(new MemeStockNews
                    {
                        Symbol = evt.Symbol,
                        Headline = $"MEME RALLY: {evt.StockName} surges as retail buying intensifies — #{evt.Symbol} trending",
                        Summary = $"{evt.StockName} has gained {((stock.CurrentPrice / evt.StartPrice - 1) * 100):F0}% in days as retail traders pile in. Options volume has surged to 5x normal levels. Short sellers facing mounting pressure with {evt.ShortInterestAtStart:P0} of float still short.",
                        Phase = MemePhase.FOMO,
                        Severity = "Major",
                    });
                }
                break;

            case MemePhase.FOMO:
                if (evt.WillSqueezeFail && evt.DaysInPhase >= 2 + _rng.Next(2))
                {
                    // Failed squeeze → skip straight to crash
                    evt.Phase = MemePhase.Crash;
                    evt.DaysInPhase = 0;
                    NewsThisTick.Add(new MemeStockNews
                    {
                        Symbol = evt.Symbol,
                        Headline = $"MEME REVERSAL: {evt.StockName} rally collapses as short sellers hold firm",
                        Summary = $"The {evt.StockName} short squeeze attempt has failed. Short sellers maintained their positions and institutional selling has overwhelmed retail demand. The stock has begun to give back gains rapidly.",
                        Phase = MemePhase.Crash,
                        Severity = "Major",
                    });
                }
                else if (evt.DaysInPhase >= 3 + _rng.Next(3)) // 3-5 days
                {
                    evt.Phase = MemePhase.Squeeze;
                    evt.DaysInPhase = 0;
                    NewsThisTick.Add(new MemeStockNews
                    {
                        Symbol = evt.Symbol,
                        Headline = $"SHORT SQUEEZE: {evt.StockName} erupts as forced covering begins — trading halted multiple times",
                        Summary = $"Prime brokers have begun issuing margin calls to short sellers in {evt.StockName}, triggering a cascade of forced buy-to-cover orders. The stock has surged {((stock.CurrentPrice / evt.StartPrice - 1) * 100):F0}% from pre-rally levels. Circuit breakers have been triggered repeatedly.",
                        Phase = MemePhase.Squeeze,
                        Severity = "Major",
                    });
                }
                break;

            case MemePhase.Squeeze:
                if (evt.DaysInPhase >= 1 + _rng.Next(3)) // 1-3 days
                {
                    evt.Phase = MemePhase.DiamondHands;
                    evt.DaysInPhase = 0;
                    // Reduce short interest significantly (shorts covered)
                    stock.ShortInterest *= 0.3m; // 70% of shorts covered
                    NewsThisTick.Add(new MemeStockNews
                    {
                        Symbol = evt.Symbol,
                        Headline = $"{evt.StockName} enters volatile plateau — 'diamond hands' vs profit-takers",
                        Summary = $"After surging {((evt.PeakPrice / evt.StartPrice - 1) * 100):F0}% from pre-rally levels, {evt.StockName} has entered a volatile trading range. Online communities are urging holders to maintain positions while institutional analysts warn of imminent correction.",
                        Phase = MemePhase.DiamondHands,
                        Severity = "Moderate",
                    });
                }
                break;

            case MemePhase.DiamondHands:
                if (evt.DaysInPhase >= 2 + _rng.Next(4)) // 2-5 days
                {
                    evt.Phase = MemePhase.Crash;
                    evt.DaysInPhase = 0;
                    NewsThisTick.Add(new MemeStockNews
                    {
                        Symbol = evt.Symbol,
                        Headline = $"MEME CRASH: {evt.StockName} plunges as retail exodus accelerates",
                        Summary = $"{evt.StockName} is in freefall as early buyers take profits and momentum reverses. The stock peaked at ${evt.PeakPrice:F2} (up {((evt.PeakPrice / evt.StartPrice - 1) * 100):F0}%) but has begun a sharp correction. Analysts expect a return toward fundamental value.",
                        Phase = MemePhase.Crash,
                        Severity = "Major",
                    });
                }
                break;

            case MemePhase.Crash:
                // End when price has fallen 50-80% from peak or after 7 days
                var fromPeak = stock.CurrentPrice / Math.Max(evt.PeakPrice, 0.01m);
                if (fromPeak < 0.3m || evt.DaysInPhase >= 3 + _rng.Next(5))
                {
                    evt.Phase = MemePhase.Completed;
                    NewsThisTick.Add(new MemeStockNews
                    {
                        Symbol = evt.Symbol,
                        Headline = $"Meme stock mania fades: {evt.StockName} settles near ${stock.CurrentPrice:F2} after wild ride",
                        Summary = $"The {evt.StockName} saga appears to be over. From a starting price of ${evt.StartPrice:F2}, the stock peaked at ${evt.PeakPrice:F2} ({((evt.PeakPrice / evt.StartPrice - 1) * 100):F0}% gain) before crashing back to ${stock.CurrentPrice:F2}. Total short interest has dropped from {evt.ShortInterestAtStart:P0} to {stock.ShortInterestOfFloat:P0}.",
                        Phase = MemePhase.Completed,
                        Severity = "Moderate",
                    });
                }
                break;
        }
    }

    private void ApplyMemePressure(MemeStockEvent evt, IReadOnlyList<Stock> stocks)
    {
        var stock = stocks.FirstOrDefault(s => s.Symbol == evt.Symbol);
        if (stock == null) return;

        var intensity = evt.SqueezeIntensity;
        decimal dailyPressure;
        decimal volumeMultiplier;

        switch (evt.Phase)
        {
            case MemePhase.Discovery:
                dailyPressure = (0.05m + (decimal)_rng.NextDouble() * 0.05m) * intensity;   // +5-10%
                volumeMultiplier = 3m;
                break;
            case MemePhase.FOMO:
                dailyPressure = (0.10m + (decimal)_rng.NextDouble() * 0.20m) * intensity;   // +10-30%
                volumeMultiplier = 10m;
                break;
            case MemePhase.Squeeze:
                dailyPressure = (0.30m + (decimal)_rng.NextDouble() * 0.70m) * intensity;   // +30-100%
                volumeMultiplier = 20m;
                // Reduce short interest during squeeze
                stock.ShortInterest = Math.Max(0, stock.ShortInterest * 0.9m);
                break;
            case MemePhase.DiamondHands:
                // Wild swings both directions
                dailyPressure = ((decimal)_rng.NextDouble() - 0.45m) * 0.30m * intensity;   // ±15%
                volumeMultiplier = 8m;
                break;
            case MemePhase.Crash:
                dailyPressure = -(0.08m + (decimal)_rng.NextDouble() * 0.12m) * intensity;  // -8-20%
                volumeMultiplier = 5m;
                break;
            default:
                return;
        }

        MemePressure[evt.Symbol] = dailyPressure;

        // Volume spike
        stock.DayVolume = Math.Max(stock.DayVolume, (long)(stock.AverageVolume * (double)volumeMultiplier));

        // Increase volatility during meme event
        stock.BaseVolatility = Math.Min(0.15m, stock.BaseVolatility * 1.05m);
    }
}

public class MemeStockEvent
{
    public string Symbol { get; set; } = "";
    public string StockName { get; set; } = "";
    public MemePhase Phase { get; set; }
    public decimal StartPrice { get; set; }
    public decimal PeakPrice { get; set; }
    public decimal ShortInterestAtStart { get; set; }
    public DateTime StartDate { get; set; }
    public int DaysInPhase { get; set; }
    public decimal SqueezeIntensity { get; set; } = 1.0m;
    public bool WillSqueezeFail { get; set; }
}

public class MemeStockNews
{
    public string Symbol { get; set; } = "";
    public string Headline { get; set; } = "";
    public string Summary { get; set; } = "";
    public MemePhase Phase { get; set; }
    public string Severity { get; set; } = "Moderate";
}

public enum MemePhase
{
    Discovery,
    FOMO,
    Squeeze,
    DiamondHands,
    Crash,
    Completed,
}
