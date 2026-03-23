using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// Circuit breaker system that halts trading during extreme price movements.
/// Bible 8.2.8:
///   - Individual stock: halted 30min if drops >10% in 5 minutes
///   - Market-wide: Level 1 (-7%), Level 2 (-13%), Level 3 (-20%)
/// </summary>
public class CircuitBreaker
{
    private readonly Logger _log = new("CircuitBreaker");

    /// <summary>Stocks currently halted. Key=symbol, Value=resume time.</summary>
    public Dictionary<string, DateTime> HaltedStocks { get; } = new();

    /// <summary>Stocks already halted today (prevent re-trigger).</summary>
    private readonly HashSet<string> _haltedToday = new();
    private bool _marketHaltedToday;

    /// <summary>Market-wide halt resume time (null = not halted).</summary>
    public DateTime? MarketHaltUntil { get; private set; }

    /// <summary>New halts this tick (for notifications).</summary>
    public List<string> NewHaltsThisTick { get; } = new();

    /// <summary>Whether the entire market is halted.</summary>
    public bool IsMarketHalted => MarketHaltUntil.HasValue;

    /// <summary>Whether a specific stock is halted.</summary>
    public bool IsStockHalted(string symbol) =>
        HaltedStocks.ContainsKey(symbol) || IsMarketHalted;

    /// <summary>
    /// Check for circuit breaker triggers. Called each tick.
    /// </summary>
    public void Tick(IReadOnlyList<Stock> stocks, DateTime gameTime)
    {
        NewHaltsThisTick.Clear();

        // Remove expired halts
        var expiredStocks = HaltedStocks.Where(kv => kv.Value <= gameTime).Select(kv => kv.Key).ToList();
        foreach (var symbol in expiredStocks)
        {
            HaltedStocks.Remove(symbol);
            _log.Info("Trading resumed", new { symbol });
        }

        if (MarketHaltUntil.HasValue && MarketHaltUntil.Value <= gameTime)
        {
            MarketHaltUntil = null;
            _marketHaltedToday = true;
            _log.Info("Market-wide halt lifted");
        }

        // Check individual stock circuit breakers
        foreach (var stock in stocks)
        {
            if (HaltedStocks.ContainsKey(stock.Symbol)) continue;
            if (_haltedToday.Contains(stock.Symbol)) continue; // Already halted once today
            if (stock.PreviousClose <= 0) continue;

            var dayChangePct = (stock.CurrentPrice - stock.PreviousClose) / stock.PreviousClose;

            // Individual stock halt: >10% drop from previous close
            if (dayChangePct < -0.10m)
            {
                var resumeTime = gameTime.AddMinutes(30);
                HaltedStocks[stock.Symbol] = resumeTime;
                _haltedToday.Add(stock.Symbol);
                NewHaltsThisTick.Add(stock.Symbol);

                _log.Info("Circuit breaker triggered", new
                {
                    symbol = stock.Symbol,
                    drop = $"{dayChangePct:P1}",
                    resumeAt = resumeTime.ToString("HH:mm"),
                });
            }
        }

        // Market-wide circuit breaker (based on average market change)
        if (!IsMarketHalted && !_marketHaltedToday && stocks.Count > 0)
        {
            var avgChange = stocks
                .Where(s => s.PreviousClose > 0)
                .Average(s => (double)((s.CurrentPrice - s.PreviousClose) / s.PreviousClose));

            if (avgChange < -0.20)
            {
                // Level 3: halt for rest of day
                MarketHaltUntil = gameTime.Date.AddHours(16); // Until 4 PM
                _log.Info("LEVEL 3 CIRCUIT BREAKER: Market halted for rest of day", new { avgChange = $"{avgChange:P1}" });
                NewHaltsThisTick.Add("MARKET_L3");
            }
            else if (avgChange < -0.13)
            {
                // Level 2: halt for 1 hour
                MarketHaltUntil = gameTime.AddHours(1);
                _log.Info("LEVEL 2 CIRCUIT BREAKER: Market halted 1 hour", new { avgChange = $"{avgChange:P1}" });
                NewHaltsThisTick.Add("MARKET_L2");
            }
            else if (avgChange < -0.07)
            {
                // Level 1: halt for 15 minutes
                MarketHaltUntil = gameTime.AddMinutes(15);
                _log.Info("LEVEL 1 CIRCUIT BREAKER: Market halted 15 minutes", new { avgChange = $"{avgChange:P1}" });
                NewHaltsThisTick.Add("MARKET_L1");
            }
        }
    }
}
