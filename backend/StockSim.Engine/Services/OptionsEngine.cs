using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// Manages option chains, pricing, and expiration for all stocks.
/// Generates strike ladders, prices via Black-Scholes each tick,
/// and handles expiration settlement.
/// </summary>
public class OptionsEngine
{
    private readonly Logger _log = new("OptionsEngine");
    private readonly Random _rng;

    /// <summary>All active option chains, keyed by underlying symbol.</summary>
    public Dictionary<string, OptionChain> Chains { get; } = new();

    /// <summary>Player's option positions.</summary>
    public List<OptionPosition> Positions { get; } = new();

    /// <summary>Contracts that expired this tick (for news/notifications).</summary>
    public List<OptionContract> ExpiredThisTick { get; } = new();

    /// <summary>Aggregate Gamma Exposure per stock. Positive = dealers long gamma (stabilizing), negative = short gamma (amplifying).</summary>
    public Dictionary<string, decimal> GammaExposure { get; } = new();

    /// <summary>GEX-driven price pressure per stock. Applied by GameLoop to PriceEngine.</summary>
    public Dictionary<string, decimal> GexPressure { get; } = new();

    /// <summary>GEX news events generated this tick.</summary>
    public List<string> GexNewsThisTick { get; } = new();

    /// <summary>Settlements that happened this tick.</summary>
    public List<OptionSettlement> SettlementsThisTick { get; } = new();

    /// <summary>Options news events generated this tick (IV Crush, Unusual Activity, Pin Risk).</summary>
    public List<OptionsNewsEvent> NewsThisTick { get; } = new();

    /// <summary>Risk-free rate (from EconomicEngine, annual).</summary>
    public double RiskFreeRate { get; set; } = 0.045;

    /// <summary>Track which symbols had IV crush applied today (prevent spam).</summary>
    private readonly HashSet<string> _ivCrushAppliedToday = new();

    /// <summary>Cooldown for options news per symbol (prevent spam). Key: "symbol:type", Value: days until next allowed.</summary>
    private readonly Dictionary<string, int> _newsThrottleDays = new();

    public OptionsEngine(int seed)
    {
        _rng = new Random(seed);
    }

    /// <summary>
    /// Generate option chains for eligible stocks. Called at game start and when new IPOs list.
    /// Only generates chains for stocks with MarketCap > $1B and LiquidityScore >= 5.
    /// </summary>
    public void GenerateChains(IReadOnlyList<Stock> stocks, DateTime gameTime)
    {
        var eligible = stocks.Where(s => !s.Traits.Contains("ETF")
            && s.MarketCap > 1_000_000_000m
            && s.LiquidityScore >= 5).ToList();

        foreach (var stock in eligible)
        {
            if (Chains.ContainsKey(stock.Symbol)) continue;

            var chain = new OptionChain { UnderlyingSymbol = stock.Symbol };
            var expirations = GenerateExpirations(gameTime);

            foreach (var expiry in expirations)
            {
                var strikes = GenerateStrikes(stock.CurrentPrice);
                var slice = new ExpirationSlice
                {
                    ExpirationDate = expiry,
                    DaysToExpiry = (expiry.Date - gameTime.Date).Days,
                    Strikes = strikes,
                };

                foreach (var strike in strikes)
                {
                    slice.Calls[strike] = CreateContract(stock.Symbol, OptionType.Call, strike, expiry);
                    slice.Puts[strike] = CreateContract(stock.Symbol, OptionType.Put, strike, expiry);
                }

                chain.Expirations.Add(expiry);
                chain.Slices[expiry] = slice;
            }

            Chains[stock.Symbol] = chain;
        }

        // Initial pricing for all generated chains
        foreach (var stock in eligible)
        {
            if (!Chains.TryGetValue(stock.Symbol, out var c)) continue;
            var s = (double)stock.CurrentPrice;
            var q = (double)stock.DividendYield;
            foreach (var slice in c.Slices.Values)
            {
                slice.DaysToExpiry = Math.Max(1, (slice.ExpirationDate.Date - gameTime.Date).Days);
                foreach (var contract in slice.Calls.Values.Concat(slice.Puts.Values))
                    PriceContract(contract, s, RiskFreeRate, q, gameTime);
            }
        }

        _log.Info("Option chains generated and priced", new { chains = Chains.Count, eligible = eligible.Count });
    }

    /// <summary>
    /// Tick: reprice all options, handle expirations. Called at market open daily.
    /// </summary>
    public void TickDay(IReadOnlyList<Stock> stocks, DateTime gameTime)
    {
        ExpiredThisTick.Clear();
        SettlementsThisTick.Clear();
        NewsThisTick.Clear();
        // NOTE: GexNewsThisTick is cleared per-tick by GameLoop, NOT here
        // (TickDay runs once/day but Program.cs reads GexNews every tick)
        GammaExposure.Clear();
        GexPressure.Clear();
        _ivCrushAppliedToday.Clear();

        // Decrement news throttle cooldowns
        foreach (var key in _newsThrottleDays.Keys.ToList())
        {
            _newsThrottleDays[key]--;
            if (_newsThrottleDays[key] <= 0) _newsThrottleDays.Remove(key);
        }

        foreach (var stock in stocks)
        {
            if (!Chains.TryGetValue(stock.Symbol, out var chain)) continue;

            var s = (double)stock.CurrentPrice;
            var q = (double)stock.DividendYield;
            var r = RiskFreeRate;

            // Check expirations first
            var expiredSlices = chain.Slices
                .Where(kvp => kvp.Key.Date <= gameTime.Date)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var expiry in expiredSlices)
            {
                SettleExpiration(chain, expiry, stock, gameTime);
                chain.Slices.Remove(expiry);
                chain.Expirations.Remove(expiry);
            }

            // Reprice remaining contracts
            foreach (var slice in chain.Slices.Values)
            {
                slice.DaysToExpiry = Math.Max(0, (slice.ExpirationDate.Date - gameTime.Date).Days);

                foreach (var contract in slice.Calls.Values.Concat(slice.Puts.Values))
                {
                    PriceContract(contract, s, r, q, gameTime);
                }
            }

            // Roll: if fewer than 3 expirations, add a new month
            if (chain.Expirations.Count < 3)
            {
                var lastExpiry = chain.Expirations.Count > 0
                    ? chain.Expirations.Max()
                    : gameTime;
                var newExpiry = GetThirdFriday(lastExpiry.AddMonths(1));

                var strikes = GenerateStrikes(stock.CurrentPrice);
                var slice = new ExpirationSlice
                {
                    ExpirationDate = newExpiry,
                    DaysToExpiry = (newExpiry.Date - gameTime.Date).Days,
                    Strikes = strikes,
                };
                foreach (var strike in strikes)
                {
                    slice.Calls[strike] = CreateContract(stock.Symbol, OptionType.Call, strike, newExpiry);
                    slice.Puts[strike] = CreateContract(stock.Symbol, OptionType.Put, strike, newExpiry);
                }
                chain.Expirations.Add(newExpiry);
                chain.Slices[newExpiry] = slice;
            }

            // === OPTIONS EVENTS ===
            CheckUnusualActivity(chain, stock, gameTime);
            CheckPinRisk(chain, stock, gameTime);

            // === GAMMA EXPOSURE (GEX) ===
            CalculateGEX(chain, stock);
        }

        // Generate GEX news for extreme values
        foreach (var (sym, gex) in GammaExposure)
        {
            if (gex < -50_000m && _rng.NextDouble() < 0.1) // Extreme negative GEX
                GexNewsThisTick.Add($"OPTIONS ALERT: Extreme negative gamma exposure on {sym} — dealer hedging may amplify moves");
            else if (gex > 100_000m && _rng.NextDouble() < 0.05) // Extreme positive GEX
                GexNewsThisTick.Add($"OPTIONS: High gamma wall at {sym} — dealer hedging expected to dampen volatility");
        }
    }

    /// <summary>
    /// Calculate aggregate Gamma Exposure (GEX) for a stock's options chain.
    /// GEX = Sum(Gamma × OpenInterest × 100 × StockPrice) across all contracts.
    /// Dealers are assumed net short options → their gamma is opposite to OI.
    /// Positive GEX = dealers buy dips/sell rallies (stabilizing).
    /// Negative GEX = dealers sell dips/buy rallies (amplifying).
    /// </summary>
    private void CalculateGEX(OptionChain chain, Stock stock)
    {
        decimal totalGex = 0;
        decimal netDelta = 0;

        foreach (var slice in chain.Slices.Values)
        {
            foreach (var call in slice.Calls.Values)
            {
                // Dealers short calls → long gamma from calls (stabilizing)
                totalGex += (decimal)call.Gamma * call.OpenInterest * 100m * stock.CurrentPrice;
                netDelta += (decimal)call.Delta * call.OpenInterest * 100m;
            }
            foreach (var put in slice.Puts.Values)
            {
                // Dealers short puts → short gamma from puts (amplifying in selloffs)
                totalGex -= (decimal)put.Gamma * put.OpenInterest * 100m * stock.CurrentPrice;
                netDelta += (decimal)put.Delta * put.OpenInterest * 100m;
            }
        }

        GammaExposure[stock.Symbol] = totalGex;

        // Calculate GEX-driven price pressure:
        // Negative GEX → amplify moves (increase volatility)
        // Positive GEX → dampen moves (decrease volatility)
        // Dealer hedging: when stock moves, dealers must rehedge delta
        var dayChange = stock.DayChangePercent;
        decimal pressure = 0;

        if (totalGex < -10_000m && Math.Abs(dayChange) > 0.5m)
        {
            // Negative gamma: dealers chase the move → amplify
            pressure = (decimal)dayChange * 0.0001m * Math.Min(1m, Math.Abs(totalGex) / 100_000m);
        }
        else if (totalGex > 10_000m && Math.Abs(dayChange) > 0.3m)
        {
            // Positive gamma: dealers counteract the move → dampen
            pressure = -(decimal)dayChange * 0.00005m * Math.Min(1m, totalGex / 200_000m);
        }

        if (pressure != 0)
            GexPressure[stock.Symbol] = pressure;
    }

    /// <summary>
    /// Apply IV Crush: after earnings, slash IV by 30-50% on the stock's options.
    /// Called from GameLoop when EarningsEngine releases results.
    /// </summary>
    public void ApplyIVCrush(string symbol, DateTime gameTime)
    {
        if (_ivCrushAppliedToday.Contains(symbol)) return;
        if (!Chains.TryGetValue(symbol, out var chain)) return;

        _ivCrushAppliedToday.Add(symbol);
        var crushFactor = 0.5 + _rng.NextDouble() * 0.2; // Retain 50-70% of IV (crush 30-50%)

        foreach (var slice in chain.Slices.Values)
        {
            foreach (var c in slice.Calls.Values.Concat(slice.Puts.Values))
            {
                c.ImpliedVolatility *= crushFactor;
            }
        }

        NewsThisTick.Add(new OptionsNewsEvent
        {
            Symbol = symbol,
            Type = OptionsNewsType.IVCrush,
            Headline = $"Options Alert: {symbol} implied volatility crushed {(int)((1 - crushFactor) * 100)}% after earnings release",
            Severity = "Moderate",
        });

        _log.Info("IV Crush applied", new { symbol, crushFactor = $"{crushFactor:P0}" });
    }

    /// <summary>Detect unusual options activity (volume spike).</summary>
    private void CheckUnusualActivity(OptionChain chain, Stock stock, DateTime gameTime)
    {
        var key = $"{stock.Symbol}:unusual";
        if (_newsThrottleDays.TryGetValue(key, out var cd) && cd > 0) return;

        // Check if any contract has volume > 3x its open interest
        var hotContracts = chain.AllContracts
            .Where(c => !c.IsExpired && c.OpenInterest > 100 && c.Volume > c.OpenInterest * 3)
            .OrderByDescending(c => c.Volume)
            .Take(1)
            .ToList();

        foreach (var c in hotContracts)
        {
            var direction = c.Type == OptionType.Call ? "bullish" : "bearish";
            NewsThisTick.Add(new OptionsNewsEvent
            {
                Symbol = stock.Symbol,
                Type = OptionsNewsType.UnusualActivity,
                Headline = $"Unusual Options Activity: {stock.Symbol} {c.DisplayName} — {c.Volume:N0} contracts traded ({direction} signal)",
                Severity = "Minor",
            });
            _newsThrottleDays[key] = 5; // 5-day cooldown per stock
        }
    }

    /// <summary>Detect pin risk: stock price near a strike with high OI close to expiry.</summary>
    private void CheckPinRisk(OptionChain chain, Stock stock, DateTime gameTime)
    {
        var key = $"{stock.Symbol}:pin";
        if (_newsThrottleDays.TryGetValue(key, out var cd) && cd > 0) return;

        var nearestExpiry = chain.Slices.Values
            .Where(s => s.DaysToExpiry > 0 && s.DaysToExpiry <= 3)
            .FirstOrDefault();
        if (nearestExpiry == null) return;

        // Find strike closest to current price
        var price = stock.CurrentPrice;
        if (price <= 0 || nearestExpiry.Strikes.Count == 0) return;
        var closestStrike = nearestExpiry.Strikes
            .OrderBy(s => Math.Abs(s - price))
            .First();

        var distance = Math.Abs(price - closestStrike) / price;
        if (distance > 0.01m) return; // Only if within 1% of strike

        // Check if significant OI at this strike
        var callOI = nearestExpiry.Calls.GetValueOrDefault(closestStrike)?.OpenInterest ?? 0;
        var putOI = nearestExpiry.Puts.GetValueOrDefault(closestStrike)?.OpenInterest ?? 0;
        var totalOI = callOI + putOI;
        if (totalOI < 500) return;

        NewsThisTick.Add(new OptionsNewsEvent
        {
            Symbol = stock.Symbol,
            Type = OptionsNewsType.PinRisk,
            Headline = $"Pin Risk: {stock.Symbol} trading within 1% of ${closestStrike:F0} strike with {totalOI:N0} open interest — expiry in {nearestExpiry.DaysToExpiry}d",
            Severity = "Minor",
        });
        _newsThrottleDays[key] = 3; // 3-day cooldown
    }

    /// <summary>Price a single contract via Black-Scholes + generate realistic bid/ask.</summary>
    private void PriceContract(OptionContract c, double s, double r, double q, DateTime gameTime)
    {
        if (s <= 0) return; // Guard against zero/negative stock price
        var k = (double)c.StrikePrice;
        var t = c.TimeToExpiryYears(gameTime);
        var sigma = GetVolatility(c, s, k, t);

        // Theoretical price
        var theo = BlackScholes.Price(s, k, t, r, sigma, q, c.Type);
        c.TheoreticalPrice = Math.Round((decimal)Math.Max(theo, 0.01), 2);
        c.ImpliedVolatility = sigma;

        // Greeks
        var greeks = BlackScholes.CalcGreeks(s, k, t, r, sigma, q, c.Type);
        c.Delta = Math.Round(greeks.Delta, 4);
        c.Gamma = Math.Round(greeks.Gamma, 6);
        c.Theta = Math.Round(greeks.Theta, 4);
        c.Vega = Math.Round(greeks.Vega, 4);
        c.Rho = Math.Round(greeks.Rho, 4);

        // Bid/Ask spread based on liquidity (ATM tighter, OTM wider)
        var moneyness = s > 0 ? Math.Abs(s - k) / s : 1.0;
        var spreadPct = moneyness < 0.05 ? 0.05 : moneyness < 0.15 ? 0.10 : 0.20; // 5-20% spread
        var halfSpread = (decimal)(theo * spreadPct / 2);
        halfSpread = Math.Max(halfSpread, 0.05m); // Min $0.05 half-spread

        c.BidPrice = Math.Max(0.01m, Math.Round(c.TheoreticalPrice - halfSpread, 2));
        c.AskPrice = Math.Round(c.TheoreticalPrice + halfSpread, 2);
        c.LastPrice = c.TheoreticalPrice;

        // Simulated volume and OI
        var baseOI = moneyness < 0.05 ? 5000 : moneyness < 0.15 ? 1000 : 200;
        c.OpenInterest = baseOI + _rng.Next(baseOI);
        c.Volume = _rng.Next(c.OpenInterest / 5 + 1);
    }

    /// <summary>Get volatility for pricing — use stock's base vol + skew.</summary>
    private double GetVolatility(OptionContract c, double s, double k, double t)
    {
        // Volatility smile/skew: OTM puts have higher IV, OTM calls lower
        // This is realistic: put protection demand drives put IV up
        var moneyness = s > 0 && k > 0 ? Math.Log(k / s) : 0;
        var baseVol = 0.25 + _rng.NextDouble() * 0.05; // 25-30% base

        // Skew: -0.1 per 10% OTM for puts, +0.05 per 10% OTM for calls
        double skew;
        if (c.Type == OptionType.Put && moneyness < 0)
            skew = moneyness * 0.5; // Puts get higher vol as they go OTM (negative moneyness = OTM put)
        else if (c.Type == OptionType.Call && moneyness > 0)
            skew = -moneyness * 0.3; // Calls get slightly lower vol as they go OTM
        else
            skew = 0;

        // Term structure: longer dated = slightly higher vol
        var termAdj = Math.Sqrt(t) * 0.02;

        return Math.Max(0.10, baseVol + skew + termAdj);
    }

    /// <summary>Handle expiration: settle ITM contracts, expire OTM worthless.</summary>
    private void SettleExpiration(OptionChain chain, DateTime expiry, Stock stock, DateTime gameTime)
    {
        var slice = chain.Slices[expiry];
        var price = stock.CurrentPrice;

        foreach (var contract in slice.Calls.Values.Concat(slice.Puts.Values))
        {
            contract.IsExpired = true;
            ExpiredThisTick.Add(contract);

            // Check if player has a position in this contract
            var pos = Positions.FirstOrDefault(p => p.ContractId == contract.Id);
            if (pos == null) continue;

            var settlement = contract.SettlementValue(price);
            var netSettlement = pos.IsLong ? settlement * pos.Quantity : -settlement * Math.Abs(pos.Quantity);

            SettlementsThisTick.Add(new OptionSettlement
            {
                Contract = contract,
                Position = pos,
                SettlementAmount = netSettlement,
                StockPriceAtExpiry = price,
                WasITM = contract.IsITM(price),
            });

            _log.Info("Option settled", new
            {
                symbol = contract.UnderlyingSymbol,
                type = contract.Type.ToString(),
                strike = contract.StrikePrice,
                stockPrice = price,
                itm = contract.IsITM(price),
                settlement = netSettlement,
            });
        }

        // Remove expired positions
        Positions.RemoveAll(p => p.ExpirationDate.Date <= gameTime.Date);
    }

    // === Helpers ===

    private static List<decimal> GenerateStrikes(decimal stockPrice)
    {
        decimal interval = stockPrice switch
        {
            < 25m => 2.50m,
            < 50m => 2.50m,
            < 200m => 5.00m,
            _ => 10.00m,
        };

        var atm = Math.Round(stockPrice / interval) * interval;
        var strikes = new List<decimal>();

        for (int i = -10; i <= 10; i++)
        {
            var strike = atm + i * interval;
            if (strike > 0) strikes.Add(strike);
        }

        return strikes;
    }

    private static List<DateTime> GenerateExpirations(DateTime fromDate)
    {
        // Monthly expirations: 3rd Friday of next 4 months
        var expirations = new List<DateTime>();
        for (int m = 0; m < 4; m++)
        {
            var expiry = GetThirdFriday(fromDate.AddMonths(m + 1));
            if (expiry > fromDate) expirations.Add(expiry);
        }
        return expirations;
    }

    private static DateTime GetThirdFriday(DateTime monthDate)
    {
        var firstDay = new DateTime(monthDate.Year, monthDate.Month, 1);
        var dayOfWeek = firstDay.DayOfWeek;
        var firstFriday = dayOfWeek <= DayOfWeek.Friday
            ? firstDay.AddDays(DayOfWeek.Friday - dayOfWeek)
            : firstDay.AddDays(7 - (dayOfWeek - DayOfWeek.Friday));
        return firstFriday.AddDays(14).Add(new TimeSpan(16, 0, 0)); // 3rd Friday, 4 PM close
    }

    private static OptionContract CreateContract(string symbol, OptionType type, decimal strike, DateTime expiry) => new()
    {
        UnderlyingSymbol = symbol,
        Type = type,
        StrikePrice = strike,
        ExpirationDate = expiry,
    };
}

/// <summary>Result of an option expiration settlement.</summary>
public class OptionSettlement
{
    public OptionContract Contract { get; set; } = null!;
    public OptionPosition Position { get; set; } = null!;
    public decimal SettlementAmount { get; set; }
    public decimal StockPriceAtExpiry { get; set; }
    public bool WasITM { get; set; }
}

public enum OptionsNewsType { IVCrush, UnusualActivity, PinRisk }

public class OptionsNewsEvent
{
    public string Symbol { get; set; } = "";
    public OptionsNewsType Type { get; set; }
    public string Headline { get; set; } = "";
    public string Severity { get; set; } = "Minor";
}
