using System.Text.Json;
using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services.Handlers;

/// <summary>
/// Handles data query messages: GetOHLCV, GetPortfolio, GetIndicators, GetAlerts, SetAlert, DeleteAlert,
/// GetOrderbook, GetAnalytics, GetAchievements, GetTradeJournal, GetStockFundamentals, GetTaxSummary,
/// GetEarningsCalendar, GetEconomicData, GetSMAStatus, GetOptionsChain, GetOrders, GetScenarios.
/// </summary>
public class DataQueryHandler : IMessageHandler
{
    private static readonly Logger Log = new("DataQueryHandler");
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private static readonly HashSet<string> MessageTypes = new()
    {
        "GetOHLCV", "GetPortfolio", "GetIndicators", "GetAlerts", "SetAlert", "DeleteAlert",
        "GetOrderbook", "GetAnalytics", "GetAchievements", "GetTradeJournal",
        "GetStockFundamentals", "GetTaxSummary", "GetEarningsCalendar", "GetEconomicData",
        "GetSMAStatus", "GetOptionsChain", "GetOrders", "GetScenarios"
    };

    private readonly GameContext _ctx;

    public DataQueryHandler(GameContext ctx)
    {
        _ctx = ctx;
    }

    public bool CanHandle(string messageType) => MessageTypes.Contains(messageType);

    public async Task HandleAsync(string messageType, string payload)
    {
        switch (messageType)
        {
            case "GetOHLCV":
                var ohlcvReq = JsonSerializer.Deserialize<OHLCVRequestEx>(payload, JsonOpts);
                if (_ctx.GameLoop != null && ohlcvReq?.Symbol != null)
                    await SendOHLCVData(ohlcvReq.Symbol, ohlcvReq.Timeframe ?? "ALL");
                break;

            case "GetPortfolio":
                if (_ctx.GameLoop != null)
                    await SendHelper.SendPortfolioUpdate(_ctx);
                break;

            case "GetIndicators":
                var indReq = JsonSerializer.Deserialize<IndicatorRequest>(payload, JsonOpts);
                if (_ctx.GameLoop != null && indReq?.Symbol != null)
                    await SendIndicators(indReq.Symbol, indReq.Indicators ?? new[] { "SMA20", "SMA50", "RSI" });
                break;

            case "SetAlert":
                var alertReq = JsonSerializer.Deserialize<SetAlertRequest>(payload, JsonOpts);
                if (_ctx.GameLoop != null && alertReq != null && _ctx.GameLoop.Portfolio.PriceAlerts.Count < 20)
                {
                    var alert = new PriceAlert(alertReq.Symbol, alertReq.Condition, alertReq.TargetPrice);
                    _ctx.GameLoop.Portfolio.PriceAlerts.Add(alert);
                    await _ctx.Server.SendAsync("AlertSet", new { id = alert.Id, symbol = alert.Symbol, condition = alert.Condition, targetPrice = alert.TargetPrice });
                }
                break;

            case "DeleteAlert":
                var delReq = JsonSerializer.Deserialize<DeleteAlertRequest>(payload, JsonOpts);
                if (_ctx.GameLoop != null && delReq != null)
                {
                    _ctx.GameLoop.Portfolio.PriceAlerts.RemoveAll(a => a.Id == delReq.AlertId);
                    await _ctx.Server.SendAsync("AlertDeleted", new { id = delReq.AlertId });
                }
                break;

            case "GetAlerts":
                if (_ctx.GameLoop != null)
                {
                    var alerts = _ctx.GameLoop.Portfolio.PriceAlerts
                        .Where(a => a.Active)
                        .Select(a => new { id = a.Id, symbol = a.Symbol, condition = a.Condition, targetPrice = a.TargetPrice })
                        .ToList();
                    await _ctx.Server.SendAsync("AlertList", new { alerts });
                }
                break;

            case "GetOrderbook":
                var obReq = JsonSerializer.Deserialize<OHLCVRequest>(payload, JsonOpts);
                if (_ctx.GameLoop != null && obReq?.Symbol != null)
                {
                    var obStock = _ctx.GameLoop.StocksBySymbol.GetValueOrDefault(obReq.Symbol);
                    if (obStock != null)
                    {
                        var ob = OrderbookGenerator.Generate(obStock, new Random(obStock.Symbol.GetHashCode() + (int)_ctx.GameLoop.TickCount));
                        await _ctx.Server.SendAsync("OrderbookData", ob);
                    }
                }
                break;

            case "GetAnalytics":
                if (_ctx.GameLoop != null)
                {
                    Func<string, decimal> getPrice = sym =>
                        _ctx.GameLoop.StocksBySymbol.GetValueOrDefault(sym)?.CurrentPrice ?? 0m;
                    var analytics = AnalyticsCalculator.Calculate(
                        _ctx.GameLoop.Portfolio, getPrice, _ctx.GameLoop.StartingCash,
                        _ctx.GameLoop.AchievementEngine.Stats);
                    var equityHistory = _ctx.GameLoop.AchievementEngine.Stats.EquityHistory
                        .Select(s => new { time = s.Time, equity = s.Equity, cash = s.Cash, marketIndex = s.MarketIndex })
                        .ToList();
                    var sectorPnL = _ctx.GameLoop.AchievementEngine.Stats.SectorPnL
                        .Select(kv => new { sector = kv.Key, pnl = kv.Value })
                        .OrderByDescending(x => x.pnl)
                        .ToList();
                    await _ctx.Server.SendAsync("AnalyticsData", new
                    {
                        analytics,
                        equityHistory,
                        sectorPnL,
                    });
                }
                break;

            case "GetAchievements":
                if (_ctx.GameLoop != null)
                {
                    var achievements = _ctx.GameLoop.AchievementEngine.Achievements
                        .Select(a => new
                        {
                            id = a.Id,
                            name = a.Name,
                            description = a.Unlocked ? a.Description : "???",
                            category = a.Category.ToString(),
                            unlocked = a.Unlocked,
                            unlockedAt = a.UnlockedAt?.ToString("o"),
                        }).ToList();
                    await _ctx.Server.SendAsync("AchievementList", new { achievements });
                }
                break;

            case "GetTradeJournal":
                if (_ctx.GameLoop != null)
                {
                    var trades = _ctx.GameLoop.AchievementEngine.Stats.TradeHistory
                        .OrderByDescending(t => t.ExitTime)
                        .Take(200)
                        .Select(t => new
                        {
                            id = t.Id,
                            symbol = t.Symbol,
                            sector = t.Sector,
                            side = t.Side,
                            entryPrice = t.EntryPrice,
                            exitPrice = t.ExitPrice,
                            quantity = t.Quantity,
                            pnl = t.PnL,
                            pnlPercent = t.PnLPercent,
                            commission = t.Commission,
                            entryTime = t.EntryTime.ToString("o"),
                            exitTime = t.ExitTime.ToString("o"),
                            holdingDays = t.HoldingDays,
                        }).ToList();
                    await _ctx.Server.SendAsync("TradeJournal", new { trades });
                }
                break;

            case "GetStockFundamentals":
                var fundReq = JsonSerializer.Deserialize<OHLCVRequest>(payload, JsonOpts);
                if (_ctx.GameLoop != null && fundReq?.Symbol != null)
                {
                    var fundStock = _ctx.GameLoop.StocksBySymbol.GetValueOrDefault(fundReq.Symbol);
                    if (fundStock != null)
                    {
                        await _ctx.Server.SendAsync("StockFundamentals", new
                        {
                            symbol = fundStock.Symbol,
                            peRatio = fundStock.PERatio,
                            marketCap = fundStock.MarketCap,
                            revenue = fundStock.Revenue,
                            netIncome = fundStock.NetIncome,
                            dividendYield = fundStock.DividendYield,
                            debtToEquity = fundStock.DebtToEquity,
                            revenueGrowth = fundStock.RevenueGrowth,
                            employees = fundStock.Employees,
                            sharesOutstanding = fundStock.SharesOutstanding,
                            insiderOwnership = fundStock.InsiderOwnership,
                            institutionalOwnership = fundStock.InstitutionalOwnership,
                            shortInterest = fundStock.ShortInterest,
                            baseVolatility = fundStock.BaseVolatility,
                            liquidityScore = fundStock.LiquidityScore,
                            fairValue = fundStock.FairValue,
                            dayHigh = fundStock.DayHigh,
                            dayLow = fundStock.DayLow,
                            yearHigh = fundStock.YearHigh,
                            yearLow = fundStock.YearLow,
                            averageVolume = fundStock.AverageVolume,
                            @float = fundStock.Float,
                            floatPercentage = fundStock.FloatPercentage,
                            analystRating = fundStock.AnalystRating,
                            analystConsensus = fundStock.AnalystConsensus,
                            targetPrice = fundStock.TargetPrice,
                            personality = fundStock.Personality == null ? null : new
                            {
                                ceoName = fundStock.Personality.CEOName,
                                ceoArchetype = fundStock.Personality.CEOArchetype,
                                foundedYear = fundStock.Personality.FoundedYear,
                                headquarters = fundStock.Personality.Headquarters,
                                description = fundStock.Personality.Description,
                                flagshipProduct = fundStock.Personality.FlagshipProduct,
                                secondaryProduct = fundStock.Personality.SecondaryProduct,
                                rivalSymbol = fundStock.Personality.RivalSymbol,
                                foundingStory = fundStock.Personality.FoundingStory,
                                ceoQuote = fundStock.Personality.CEOQuote,
                                productDescription = fundStock.Personality.ProductDescription,
                                creditRating = fundStock.Personality.CreditRating,
                                keyMilestone = fundStock.Personality.KeyMilestone,
                            },
                        });
                    }
                }
                break;

            case "GetTaxSummary":
                if (_ctx.GameLoop != null)
                    await _ctx.Server.SendAsync("TaxSummary", _ctx.GameLoop.TaxEngine.GetSummary());
                break;

            case "GetEarningsCalendar":
                if (_ctx.GameLoop != null)
                {
                    var upcoming2 = _ctx.GameLoop.EarningsEngine.GetUpcoming(_ctx.GameLoop.GameTime, 60)
                        .Select(e => new
                        {
                            symbol = e.Symbol, reportDate = e.ReportDate.ToString("o"),
                            quarter = e.Quarter, expectedEPS = e.ExpectedEPS,
                        }).ToList();
                    var recent = _ctx.GameLoop.EarningsEngine.GetRecent(20)
                        .Select(e => new
                        {
                            symbol = e.Symbol, reportDate = e.ReportDate.ToString("o"),
                            quarter = e.Quarter, expectedEPS = e.ExpectedEPS,
                            actualEPS = e.ActualEPS, beat = e.Beat,
                            surprisePercent = e.EPSSurprisePercent,
                            priceImpact = e.PriceImpactPercent,
                        }).ToList();
                    await _ctx.Server.SendAsync("EarningsCalendar", new { upcoming = upcoming2, recent });
                }
                break;

            case "GetEconomicData":
                if (_ctx.GameLoop != null)
                {
                    var econ = _ctx.GameLoop.EconomicEngine;
                    var upcoming = econ.UpcomingEvents
                        .Where(e => !e.Released)
                        .Take(20)
                        .Select(e => new
                        {
                            id = e.Id, name = e.Name, indicator = e.Indicator,
                            scheduledDate = e.ScheduledDate.ToString("o"),
                            previousValue = e.PreviousValue, expectedValue = e.ExpectedValue,
                            impact = e.Impact,
                        }).ToList();
                    await _ctx.Server.SendAsync("EconomicData", new
                    {
                        indicators = new
                        {
                            interestRate = econ.Data.InterestRate,
                            inflationRate = econ.Data.InflationRate,
                            unemploymentRate = econ.Data.UnemploymentRate,
                            gdpGrowth = econ.Data.GDPGrowth,
                            consumerConfidence = econ.Data.ConsumerConfidence,
                            treasuryYield10Y = econ.Data.TreasuryYield10Y,
                            oilPrice = econ.Data.OilPrice,
                            goldPrice = econ.Data.GoldPrice,
                            manufacturingPMI = econ.Data.ManufacturingPMI,
                        },
                        fearGreedIndex = econ.GetFearGreedIndex(),
                        marketSentiment = econ.GetMarketSentiment(),
                        sectorMultipliers = econ.GetSectorMultipliers(),
                        upcomingEvents = upcoming,
                        vix = econ.MarketVolatilityIndex,
                        commodities = new
                        {
                            crudeOil = econ.Data.OilPrice,
                            gold = econ.Data.GoldPrice,
                            natGas = Math.Round(econ.Data.OilPrice * 0.04m, 2),
                            silver = Math.Round(econ.Data.GoldPrice * 0.035m, 2),
                            copper = Math.Round(3.5m + (econ.Data.ManufacturingPMI - 50m) * 0.02m, 2),
                            bitcoin = Math.Round(40000m + (econ.Data.ConsumerConfidence - 80m) * 200m, 0),
                        },
                    });
                }
                break;

            case "GetSMAStatus":
                if (_ctx.GameLoop != null)
                {
                    var smaState = _ctx.GameLoop.SMAEngine.State;
                    await _ctx.Server.SendAsync("SMAStatus", new
                    {
                        status = smaState.Status.ToString(),
                        violations = smaState.Violations.Select(v => new
                        {
                            id = v.Id,
                            type = v.Type.ToString(),
                            symbol = v.Symbol,
                            detectedAt = v.DetectedAt.ToString("o"),
                            estimatedProfit = v.EstimatedProfit,
                            description = v.Description,
                        }),
                        investigations = smaState.Investigations.Where(i => !i.IsResolved).Select(i => new
                        {
                            id = i.Id,
                            type = i.Type.ToString(),
                            symbol = i.Symbol,
                            startedAt = i.StartedAt.ToString("o"),
                            daysRemaining = i.DurationDays - i.DaysElapsed,
                        }),
                        penalties = smaState.Penalties.Select(p => new
                        {
                            id = p.Id,
                            type = p.Type.ToString(),
                            symbol = p.Symbol,
                            imposedAt = p.ImposedAt.ToString("o"),
                            fineAmount = p.FineAmount,
                            description = p.Description,
                        }),
                        tradingRestrictions = smaState.TradingRestrictions.Select(r => new
                        {
                            symbol = r.Symbol,
                            expiresAt = r.ExpiresAt.ToString("o"),
                            closeOnly = r.CloseOnly,
                        }),
                        tradingBanUntil = smaState.TradingBanUntil?.ToString("o"),
                        marginBanUntil = smaState.MarginBanUntil?.ToString("o"),
                        accountFrozen = smaState.AccountFrozen,
                        enforcementActionCount = smaState.EnforcementActionCount,
                    });
                }
                break;

            case "GetOptionsChain":
                var optReq = JsonSerializer.Deserialize<OHLCVRequest>(payload, JsonOpts);
                if (_ctx.GameLoop != null && optReq?.Symbol != null
                    && _ctx.GameLoop.OptionsEngine.Chains.TryGetValue(optReq.Symbol, out var optChain))
                {
                    var slices = optChain.Slices.OrderBy(s => s.Key).Select(kvp =>
                    {
                        var slice = kvp.Value;
                        return new
                        {
                            expirationDate = slice.ExpirationDate.ToString("o"),
                            daysToExpiry = slice.DaysToExpiry,
                            strikes = slice.Strikes,
                            calls = slice.Calls.OrderBy(c => c.Key).Select(c => MapContract(c.Value)),
                            puts = slice.Puts.OrderBy(p => p.Key).Select(p => MapContract(p.Value)),
                        };
                    }).ToList();

                    await _ctx.Server.SendAsync("OptionsChain", new
                    {
                        symbol = optReq.Symbol,
                        expirations = optChain.Expirations.Select(e => e.ToString("o")),
                        slices,
                        positions = _ctx.GameLoop.OptionsEngine.Positions
                            .Where(p => p.UnderlyingSymbol == optReq.Symbol)
                            .Select(p => new
                            {
                                contractId = p.ContractId,
                                type = p.Type.ToString(),
                                strike = p.StrikePrice,
                                expiry = p.ExpirationDate.ToString("o"),
                                quantity = p.Quantity,
                                avgCost = p.AvgCost,
                            }),
                    });
                }
                else if (_ctx.GameLoop != null)
                {
                    await _ctx.Server.SendAsync("OptionsChain", new
                    {
                        symbol = optReq?.Symbol ?? "",
                        expirations = Array.Empty<string>(),
                        slices = Array.Empty<object>(),
                        positions = Array.Empty<object>(),
                        noChain = true,
                    });
                }
                break;

            case "GetOrders":
                if (_ctx.GameLoop != null)
                    await SendHelper.SendOrdersUpdate(_ctx);
                break;

            case "GetScenarios":
                var scenarios = Scenario.GetAll().Select(s => new
                {
                    id = s.Id, name = s.Name, description = s.Description,
                    difficulty = s.Difficulty, startingCash = s.StartingCash,
                    timeLimitDays = s.TimeLimitDays,
                    targetValue = s.TargetPortfolioValue,
                }).ToList();
                await _ctx.Server.SendAsync("ScenarioList", new { scenarios });
                break;
        }
    }

    private async Task SendOHLCVData(string symbol, string timeframe = "ALL")
    {
        var gameLoop = _ctx.GameLoop!;
        var server = _ctx.Server;

        var rawCandles = new List<Candle>();

        if (gameLoop.DailyHistory.TryGetValue(symbol, out var dailyCandles))
            rawCandles.AddRange(dailyCandles);

        if (gameLoop.PriceHistories.TryGetValue(symbol, out var history))
            rawCandles.AddRange(history.Candles);

        if (timeframe != "ALL" && rawCandles.Count > 0)
        {
            var latestTime = rawCandles[^1].Time;
            var cutoffSeconds = timeframe switch
            {
                "1D" => 86400L,
                "1W" => 7L * 86400,
                "1M" => 30L * 86400,
                "3M" => 90L * 86400,
                "1Y" => 365L * 86400,
                _ => long.MaxValue,
            };
            var cutoff = latestTime - cutoffSeconds;
            rawCandles = rawCandles.Where(c => c.Time >= cutoff).ToList();
        }

        List<object> allCandles;
        var dataSpanDays = rawCandles.Count > 1
            ? (rawCandles[^1].Time - rawCandles[0].Time) / 86400.0
            : 0;

        if (dataSpanDays > 60)
        {
            allCandles = AggregateToDailyCandles(rawCandles);
        }
        else if (dataSpanDays > 5)
        {
            allCandles = AggregateToInterval(rawCandles, 3600);
        }
        else
        {
            allCandles = rawCandles.Select(c => (object)new
            {
                time = c.Time, open = c.Open, high = c.High,
                low = c.Low, close = c.Close, volume = c.Volume,
            }).ToList();
        }

        await server.SendAsync("OHLCVUpdate", new
        {
            symbol,
            candles = allCandles,
        });

        Log.Info("OHLCV data sent", new { symbol, timeframe, candles = allCandles.Count });
    }

    private async Task SendIndicators(string symbol, string[] indicators)
    {
        var gameLoop = _ctx.GameLoop!;
        var server = _ctx.Server;

        var allCandles = new List<Candle>();
        if (gameLoop.DailyHistory.TryGetValue(symbol, out var daily))
            allCandles.AddRange(daily);
        if (gameLoop.PriceHistories.TryGetValue(symbol, out var live))
            allCandles.AddRange(live.Candles);

        if (allCandles.Count == 0) return;

        var result = new Dictionary<string, object>();

        foreach (var ind in indicators)
        {
            switch (ind.ToUpper())
            {
                case "SMA20":
                    result["sma20"] = IndicatorCalculator.SMA(allCandles, 20)
                        .Select((v, i) => v.HasValue ? new { time = allCandles[i].Time, value = v.Value } : null)
                        .Where(x => x != null).ToList()!;
                    break;
                case "SMA50":
                    result["sma50"] = IndicatorCalculator.SMA(allCandles, 50)
                        .Select((v, i) => v.HasValue ? new { time = allCandles[i].Time, value = v.Value } : null)
                        .Where(x => x != null).ToList()!;
                    break;
                case "SMA200":
                    result["sma200"] = IndicatorCalculator.SMA(allCandles, 200)
                        .Select((v, i) => v.HasValue ? new { time = allCandles[i].Time, value = v.Value } : null)
                        .Where(x => x != null).ToList()!;
                    break;
                case "EMA12":
                    result["ema12"] = IndicatorCalculator.EMA(allCandles, 12)
                        .Select((v, i) => v.HasValue ? new { time = allCandles[i].Time, value = v.Value } : null)
                        .Where(x => x != null).ToList()!;
                    break;
                case "RSI":
                    result["rsi"] = IndicatorCalculator.RSI(allCandles, 14)
                        .Select((v, i) => v.HasValue ? new { time = allCandles[i].Time, value = v.Value } : null)
                        .Where(x => x != null).ToList()!;
                    break;
                case "BOLLINGER":
                    var (upper, middle, lower) = IndicatorCalculator.BollingerBands(allCandles, 20);
                    result["bollingerUpper"] = upper
                        .Select((v, i) => v.HasValue ? new { time = allCandles[i].Time, value = v.Value } : null)
                        .Where(x => x != null).ToList()!;
                    result["bollingerMiddle"] = middle
                        .Select((v, i) => v.HasValue ? new { time = allCandles[i].Time, value = v.Value } : null)
                        .Where(x => x != null).ToList()!;
                    result["bollingerLower"] = lower
                        .Select((v, i) => v.HasValue ? new { time = allCandles[i].Time, value = v.Value } : null)
                        .Where(x => x != null).ToList()!;
                    break;
                case "MACD":
                    var (macd, signal, hist) = IndicatorCalculator.MACD(allCandles);
                    result["macdLine"] = macd
                        .Select((v, i) => v.HasValue ? new { time = allCandles[i].Time, value = v.Value } : null)
                        .Where(x => x != null).ToList()!;
                    result["macdSignal"] = signal
                        .Select((v, i) => v.HasValue ? new { time = allCandles[i].Time, value = v.Value } : null)
                        .Where(x => x != null).ToList()!;
                    result["macdHistogram"] = hist
                        .Select((v, i) => v.HasValue ? new { time = allCandles[i].Time, value = v.Value } : null)
                        .Where(x => x != null).ToList()!;
                    break;
                case "VWAP":
                    var vwapData = IndicatorCalculator.VWAP(allCandles);
                    result["vwap"] = vwapData
                        .Select((v, i) => v.HasValue ? new { time = allCandles[i].Time, value = v.Value } : null)
                        .Where(x => x != null).ToList()!;
                    break;
            }
        }

        await server.SendAsync("IndicatorData", new { symbol, indicators = result });
    }

    private static object MapContract(OptionContract c)
    {
        return new
        {
            id = c.Id,
            type = c.Type.ToString(),
            strike = c.StrikePrice,
            expiry = c.ExpirationDate.ToString("o"),
            theo = c.TheoreticalPrice,
            bid = c.BidPrice,
            ask = c.AskPrice,
            last = c.LastPrice,
            iv = Math.Round(c.ImpliedVolatility * 100, 1),
            delta = c.Delta,
            gamma = c.Gamma,
            theta = c.Theta,
            vega = c.Vega,
            rho = c.Rho,
            volume = c.Volume,
            openInterest = c.OpenInterest,
        };
    }

    private static List<object> AggregateToDailyCandles(List<Candle> candles)
    {
        return candles
            .GroupBy(c => c.Time / 86400)
            .Select(g =>
            {
                var sorted = g.OrderBy(c => c.Time).ToList();
                return (object)new
                {
                    time = sorted[0].Time,
                    open = sorted[0].Open,
                    high = sorted.Max(c => c.High),
                    low = sorted.Min(c => c.Low),
                    close = sorted[^1].Close,
                    volume = sorted.Sum(c => (long)c.Volume),
                };
            }).ToList();
    }

    private static List<object> AggregateToInterval(List<Candle> candles, long intervalSeconds)
    {
        return candles
            .GroupBy(c => c.Time / intervalSeconds)
            .Select(g =>
            {
                var sorted = g.OrderBy(c => c.Time).ToList();
                return (object)new
                {
                    time = sorted[0].Time,
                    open = sorted[0].Open,
                    high = sorted.Max(c => c.High),
                    low = sorted.Min(c => c.Low),
                    close = sorted[^1].Close,
                    volume = sorted.Sum(c => (long)c.Volume),
                };
            }).ToList();
    }

    private record OHLCVRequest(string Symbol);
    private record OHLCVRequestEx(string Symbol, string? Timeframe);
    private record IndicatorRequest(string Symbol, string[]? Indicators);
    private record SetAlertRequest(string Symbol, string Condition, decimal TargetPrice);
    private record DeleteAlertRequest(long AlertId);
}
