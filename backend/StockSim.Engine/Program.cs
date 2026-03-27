using System.Text.Json;
using StockSim.Engine.Models;
using StockSim.Engine.Services;
using StockSim.Engine.Utils;

namespace StockSim.Engine;

/// <summary>
/// Entry point for the StockSim backend engine.
/// Starts WebSocket server, initializes game loop, runs simulation.
/// See Bible 21.2 for backend lifecycle.
/// </summary>
public class Program
{
    private static readonly Logger Log = new("Main");
    private static GameLoop? _gameLoop;
    private static WebSocketServer? _server;
    private static bool _running = true;

    public static async Task Main(string[] args)
    {
        Log.Info("StockSim Engine starting", new { version = "0.1.0", pid = Environment.ProcessId });

        var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 8765;

        _server = new WebSocketServer(port);
        _server.OnMessage(HandleMessage);
        _server.OnClientConnected += () => Log.Info("Frontend connected");
        _server.OnClientDisconnected += () => Log.Info("Frontend disconnected");

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            _running = false;
            Log.Info("Shutdown requested");
        };

        var serverTask = _server.StartAsync();
        var tickTask = RunTickLoop();

        await Task.WhenAny(serverTask, tickTask);

        _server.Dispose();
        Log.Info("StockSim Engine stopped");
    }

    private static async Task HandleMessage(string type, string payload)
    {
        Log.Info("Message received", new { type });

        switch (type)
        {
            case "hello":
                await _server!.SendAsync("welcome", new
                {
                    version = "0.1.0",
                    status = _gameLoop != null ? "game_active" : "no_game"
                });
                break;

            case "NewGame":
                var config = JsonSerializer.Deserialize<NewGameConfig>(payload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                await StartNewGameAsync(config);
                break;

            case "SetSpeed":
                var speedData = JsonSerializer.Deserialize<SpeedConfig>(payload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (_gameLoop != null && speedData != null)
                {
                    _gameLoop.SetSpeed((GameSpeed)speedData.Speed);
                    await _server!.SendAsync("SpeedChanged", new { speed = (int)_gameLoop.Speed });
                }
                break;

            case "GetOHLCV":
                var ohlcvReq = JsonSerializer.Deserialize<OHLCVRequestEx>(payload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (_gameLoop != null && ohlcvReq?.Symbol != null)
                {
                    await SendOHLCVData(ohlcvReq.Symbol, ohlcvReq.Timeframe ?? "ALL");
                }
                break;

            case "PlaceOrder":
                var orderReq = JsonSerializer.Deserialize<PlaceOrderRequest>(payload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (_gameLoop != null && orderReq != null)
                {
                    await HandlePlaceOrder(orderReq);
                }
                break;

            case "CancelOrder":
                var cancelReq = JsonSerializer.Deserialize<CancelOrderRequest>(payload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (_gameLoop != null && cancelReq != null)
                {
                    var cancelled = _gameLoop.OrderEngine.CancelOrder(cancelReq.OrderId);
                    await _server!.SendAsync("OrderCancelled", new { orderId = cancelReq.OrderId, success = cancelled });
                }
                break;

            case "GetPortfolio":
                if (_gameLoop != null)
                {
                    await SendPortfolioUpdate();
                }
                break;

            case "GetIndicators":
                var indReq = JsonSerializer.Deserialize<IndicatorRequest>(payload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (_gameLoop != null && indReq?.Symbol != null)
                {
                    await SendIndicators(indReq.Symbol, indReq.Indicators ?? new[] { "SMA20", "SMA50", "RSI" });
                }
                break;

            case "SetAlert":
                var alertReq = JsonSerializer.Deserialize<SetAlertRequest>(payload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (_gameLoop != null && alertReq != null && _gameLoop.Portfolio.PriceAlerts.Count < 20)
                {
                    var alert = new PriceAlert(alertReq.Symbol, alertReq.Condition, alertReq.TargetPrice);
                    _gameLoop.Portfolio.PriceAlerts.Add(alert);
                    await _server!.SendAsync("AlertSet", new { id = alert.Id, symbol = alert.Symbol, condition = alert.Condition, targetPrice = alert.TargetPrice });
                }
                break;

            case "DeleteAlert":
                var delReq = JsonSerializer.Deserialize<DeleteAlertRequest>(payload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (_gameLoop != null && delReq != null)
                {
                    _gameLoop.Portfolio.PriceAlerts.RemoveAll(a => a.Id == delReq.AlertId);
                    await _server!.SendAsync("AlertDeleted", new { id = delReq.AlertId });
                }
                break;

            case "GetAlerts":
                if (_gameLoop != null)
                {
                    var alerts = _gameLoop.Portfolio.PriceAlerts
                        .Where(a => a.Active)
                        .Select(a => new { id = a.Id, symbol = a.Symbol, condition = a.Condition, targetPrice = a.TargetPrice })
                        .ToList();
                    await _server!.SendAsync("AlertList", new { alerts });
                }
                break;

            case "GetOrderbook":
                var obReq = JsonSerializer.Deserialize<OHLCVRequest>(payload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (_gameLoop != null && obReq?.Symbol != null)
                {
                    var obStock = _gameLoop.StocksBySymbol.GetValueOrDefault(obReq.Symbol);
                    if (obStock != null)
                    {
                        var ob = OrderbookGenerator.Generate(obStock, new Random(obStock.Symbol.GetHashCode() + (int)_gameLoop.TickCount));
                        await _server!.SendAsync("OrderbookData", ob);
                    }
                }
                break;

            case "GetAnalytics":
                if (_gameLoop != null)
                {
                    Func<string, decimal> getPrice = sym =>
                        _gameLoop.StocksBySymbol.GetValueOrDefault(sym)?.CurrentPrice ?? 0m;
                    var analytics = AnalyticsCalculator.Calculate(
                        _gameLoop.Portfolio, getPrice, _gameLoop.StartingCash,
                        _gameLoop.AchievementEngine.Stats);
                    var equityHistory = _gameLoop.AchievementEngine.Stats.EquityHistory
                        .Select(s => new { time = s.Time, equity = s.Equity, cash = s.Cash, marketIndex = s.MarketIndex })
                        .ToList();
                    var sectorPnL = _gameLoop.AchievementEngine.Stats.SectorPnL
                        .Select(kv => new { sector = kv.Key, pnl = kv.Value })
                        .OrderByDescending(x => x.pnl)
                        .ToList();
                    await _server!.SendAsync("AnalyticsData", new
                    {
                        analytics,
                        equityHistory,
                        sectorPnL,
                    });
                }
                break;

            case "GetAchievements":
                if (_gameLoop != null)
                {
                    var achievements = _gameLoop.AchievementEngine.Achievements
                        .Select(a => new
                        {
                            id = a.Id,
                            name = a.Name,
                            description = a.Unlocked ? a.Description : "???",
                            category = a.Category.ToString(),
                            unlocked = a.Unlocked,
                            unlockedAt = a.UnlockedAt?.ToString("o"),
                        }).ToList();
                    await _server!.SendAsync("AchievementList", new { achievements });
                }
                break;

            case "GetTradeJournal":
                if (_gameLoop != null)
                {
                    var trades = _gameLoop.AchievementEngine.Stats.TradeHistory
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
                    await _server!.SendAsync("TradeJournal", new { trades });
                }
                break;

            case "GetScenarios":
                var scenarios = Scenario.GetAll().Select(s => new
                {
                    id = s.Id, name = s.Name, description = s.Description,
                    difficulty = s.Difficulty, startingCash = s.StartingCash,
                    timeLimitDays = s.TimeLimitDays,
                    targetValue = s.TargetPortfolioValue,
                }).ToList();
                await _server!.SendAsync("ScenarioList", new { scenarios });
                break;

            case "StartScenario":
                var scenarioReq = JsonSerializer.Deserialize<StartScenarioRequest>(payload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (scenarioReq != null)
                {
                    var scenario = Scenario.GetAll().Find(s => s.Id == scenarioReq.ScenarioId);
                    if (scenario != null)
                    {
                        scenario.IsActive = true;
                        var scenSeed = new Random().Next();
                        _gameLoop = new GameLoop(scenSeed, 250, scenario.StartingCash);
                        _gameLoop.ActiveScenario = scenario;
                        await SendMarketSnapshot();
                        await _server!.SendAsync("ScenarioStarted", new
                        {
                            id = scenario.Id, name = scenario.Name,
                            description = scenario.Description,
                            startingCash = scenario.StartingCash,
                            timeLimitDays = scenario.TimeLimitDays,
                            targetValue = scenario.TargetPortfolioValue,
                        });
                    }
                }
                break;

            case "GetStockFundamentals":
                var fundReq = JsonSerializer.Deserialize<OHLCVRequest>(payload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (_gameLoop != null && fundReq?.Symbol != null)
                {
                    var fundStock = _gameLoop.StocksBySymbol.GetValueOrDefault(fundReq.Symbol);
                    if (fundStock != null)
                    {
                        await _server!.SendAsync("StockFundamentals", new
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
                            },
                        });
                    }
                }
                break;

            case "PlaceBracketOrder":
                var bracketReq = JsonSerializer.Deserialize<BracketOrderRequest>(payload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (_gameLoop != null && bracketReq != null)
                {
                    var bracketStock = _gameLoop.StocksBySymbol.GetValueOrDefault(bracketReq.Symbol);
                    if (bracketStock != null)
                    {
                        // Place take-profit (limit sell)
                        var tpResult = _gameLoop.OrderEngine.PlaceOrder(
                            bracketReq.Symbol, OrderSide.Sell, OrderType.Limit,
                            bracketReq.Quantity, bracketStock, _gameLoop.GameTime,
                            _gameLoop.IsMarketOpen(), bracketReq.TakeProfitPrice);

                        // Place stop-loss (stop sell)
                        var slResult = _gameLoop.OrderEngine.PlaceOrder(
                            bracketReq.Symbol, OrderSide.Sell, OrderType.Stop,
                            bracketReq.Quantity, bracketStock, _gameLoop.GameTime,
                            _gameLoop.IsMarketOpen(), stopPrice: bracketReq.StopLossPrice);

                        // Link them as OCO pair
                        if (tpResult.Order != null && slResult.Order != null)
                        {
                            tpResult.Order.OCOPairId = slResult.Order.Id;
                            slResult.Order.OCOPairId = tpResult.Order.Id;
                        }

                        await _server!.SendAsync("BracketOrderPlaced", new
                        {
                            success = true,
                            takeProfitId = tpResult.Order?.Id,
                            stopLossId = slResult.Order?.Id,
                        });
                        await SendOrdersUpdate();
                    }
                }
                break;

            case "GetTaxSummary":
                if (_gameLoop != null)
                {
                    await _server!.SendAsync("TaxSummary", _gameLoop.TaxEngine.GetSummary());
                }
                break;

            case "GetEarningsCalendar":
                if (_gameLoop != null)
                {
                    var upcoming2 = _gameLoop.EarningsEngine.GetUpcoming(_gameLoop.GameTime, 60)
                        .Select(e => new
                        {
                            symbol = e.Symbol, reportDate = e.ReportDate.ToString("o"),
                            quarter = e.Quarter, expectedEPS = e.ExpectedEPS,
                        }).ToList();
                    var recent = _gameLoop.EarningsEngine.GetRecent(20)
                        .Select(e => new
                        {
                            symbol = e.Symbol, reportDate = e.ReportDate.ToString("o"),
                            quarter = e.Quarter, expectedEPS = e.ExpectedEPS,
                            actualEPS = e.ActualEPS, beat = e.Beat,
                            surprisePercent = e.EPSSurprisePercent,
                            priceImpact = e.PriceImpactPercent,
                        }).ToList();
                    await _server!.SendAsync("EarningsCalendar", new { upcoming = upcoming2, recent });
                }
                break;

            case "GetEconomicData":
                if (_gameLoop != null)
                {
                    var econ = _gameLoop.EconomicEngine;
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
                    await _server!.SendAsync("EconomicData", new
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
                    });
                }
                break;

            case "SkipToOpen":
                if (_gameLoop != null && !_gameLoop.IsMarketOpen())
                {
                    // Fast-forward time to next market open (9:30 AM weekday)
                    var skipTime = _gameLoop.GameTime;
                    int safetyLimit = 5000; // Max ticks to prevent infinite loop
                    while (!_gameLoop.IsMarketOpen() && safetyLimit-- > 0)
                    {
                        skipTime = skipTime.AddMinutes(1);
                        // Skip weekends entirely
                        if (skipTime.DayOfWeek == DayOfWeek.Saturday)
                            skipTime = skipTime.AddDays(2).Date.AddHours(9).AddMinutes(30);
                        else if (skipTime.DayOfWeek == DayOfWeek.Sunday)
                            skipTime = skipTime.AddDays(1).Date.AddHours(9).AddMinutes(30);
                        // Jump to 9:30 if before market open
                        else if (skipTime.TimeOfDay < new TimeSpan(9, 30, 0))
                            skipTime = skipTime.Date.AddHours(9).AddMinutes(30);
                    }
                    _gameLoop.GameTime = skipTime;
                    await _server!.SendAsync("SpeedChanged", new { speed = (int)_gameLoop.Speed });
                    await SendPriceUpdate();
                    Log.Info("Skipped to market open", new { newTime = skipTime.ToString("o") });
                }
                break;

            case "GetSMAStatus":
                if (_gameLoop != null)
                {
                    var smaState = _gameLoop.SMAEngine.State;
                    await _server!.SendAsync("SMAStatus", new
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

            case "Retire":
                if (_gameLoop != null)
                {
                    Func<string, decimal> retirePrice = sym =>
                        _gameLoop.StocksBySymbol.GetValueOrDefault(sym)?.CurrentPrice ?? 0m;
                    var retireEquity = _gameLoop.Portfolio.TotalEquity(retirePrice);
                    var stats = _gameLoop.AchievementEngine.Stats;
                    var totalTrades2 = stats.WinningTradeCount + stats.LosingTradeCount;
                    var winRate2 = totalTrades2 > 0 ? Math.Round((decimal)stats.WinningTradeCount / totalTrades2 * 100, 1) : 0m;
                    var bestTrade = stats.TradeHistory.Count > 0 ? stats.TradeHistory.MaxBy(t => t.PnL) : null;
                    var worstTrade = stats.TradeHistory.Count > 0 ? stats.TradeHistory.MinBy(t => t.PnL) : null;
                    var achievementsUnlocked = _gameLoop.AchievementEngine.Achievements.Count(a => a.Unlocked);

                    await _server!.SendAsync("CareerSummary", new
                    {
                        finalEquity = retireEquity,
                        startingCash = _gameLoop.StartingCash,
                        totalReturn = retireEquity - _gameLoop.StartingCash,
                        totalReturnPercent = _gameLoop.StartingCash > 0
                            ? Math.Round((retireEquity - _gameLoop.StartingCash) / _gameLoop.StartingCash * 100, 2) : 0,
                        daysPlayed = stats.DaysPlayed,
                        totalTrades = totalTrades2,
                        winRate = winRate2,
                        bestTradePnL = bestTrade?.PnL ?? 0,
                        bestTradeSymbol = bestTrade?.Symbol ?? "",
                        worstTradePnL = worstTrade?.PnL ?? 0,
                        worstTradeSymbol = worstTrade?.Symbol ?? "",
                        totalCommissions = _gameLoop.Portfolio.TotalCommissions,
                        totalTaxPaid = _gameLoop.TaxEngine.TotalTaxPaid,
                        totalDividends = _gameLoop.TaxEngine.DividendTaxPaid / Math.Max(0.01m, _gameLoop.TaxEngine.LongTermRate), // Approximate gross dividends
                        maxDrawdown = stats.MaxDrawdownPercent,
                        achievementsUnlocked,
                        achievementsTotal = _gameLoop.AchievementEngine.Achievements.Count,
                        largestSingleGain = stats.LargestSingleGain,
                        largestSingleLoss = stats.LargestSingleLoss,
                    });
                    _gameLoop.SetSpeed(GameSpeed.Paused);
                }
                break;

            case "RestartBankrupt":
                if (_gameLoop != null && _gameLoop.IsBankrupt)
                {
                    // Bible 1.4: restart with $10k (normal), same market state
                    _gameLoop.Portfolio.Cash = 10_000m;
                    _gameLoop.Portfolio.Positions.Clear();
                    _gameLoop.Portfolio.RealizedPnL = 0;
                    _gameLoop.Portfolio.TotalCommissions = 0;
                    _gameLoop.Portfolio.TradeCount = 0;
                    _gameLoop.IsBankrupt = false;
                    await SendPortfolioUpdate();
                    await _server!.SendAsync("BankruptRestarted", new { cash = 10_000 });
                }
                break;

            case "GetOrders":
                if (_gameLoop != null)
                {
                    await SendOrdersUpdate();
                }
                break;

            case "AcceptTenderOffer":
                if (_gameLoop != null)
                {
                    var tenderReq = JsonSerializer.Deserialize<TenderOfferResponse>(payload,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (tenderReq != null && _gameLoop.Portfolio.Positions.ContainsKey(tenderReq.Symbol))
                    {
                        var pos = _gameLoop.Portfolio.Positions[tenderReq.Symbol];
                        var shares = Math.Abs(pos.Shares);
                        var proceeds = shares * tenderReq.OfferPrice;

                        // Remove position, add cash (guaranteed price, no slippage)
                        _gameLoop.Portfolio.Positions.Remove(tenderReq.Symbol);
                        _gameLoop.Portfolio.Cash += proceeds;
                        _gameLoop.Portfolio.RealizedPnL += proceeds - (shares * pos.AverageCost);
                        _gameLoop.Portfolio.TradeCount++;

                        await _server!.SendAsync("TenderOfferAccepted", new
                        {
                            symbol = tenderReq.Symbol,
                            shares,
                            proceeds,
                            offerPrice = tenderReq.OfferPrice,
                        });
                        await SendPortfolioUpdate();

                        Log.Info("Tender offer accepted", new { symbol = tenderReq.Symbol, shares, proceeds });
                    }
                }
                break;

            case "SaveGame":
                if (_gameLoop != null)
                {
                    // Iron Man scenario: no saving allowed
                    if (_gameLoop.ActiveScenario is { IsActive: true, NoSaveAllowed: true })
                    {
                        await _server!.SendAsync("GameSaved", new { success = false, error = "Saving is not allowed in this scenario (Iron Man mode)." });
                        break;
                    }

                    var saveReq = JsonSerializer.Deserialize<SaveGameRequest>(payload,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    var savePath = string.IsNullOrEmpty(saveReq?.SlotName)
                        ? SaveManager.GetDefaultSavePath()
                        : SaveManager.GetSlotPath(saveReq.SlotName);
                    await SaveManager.SaveGameAsync(_gameLoop, savePath);
                    await _server!.SendAsync("GameSaved", new { success = true, path = savePath, slot = saveReq?.SlotName ?? "quicksave" });
                }
                break;

            case "LoadGame":
                var loadReq = JsonSerializer.Deserialize<LoadGameRequest>(payload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var loadPath = string.IsNullOrEmpty(loadReq?.SlotName)
                    ? SaveManager.GetDefaultSavePath()
                    : SaveManager.GetSlotPath(loadReq.SlotName);
                var loaded = await SaveManager.LoadGameAsync(loadPath);
                if (loaded != null)
                {
                    _gameLoop = loaded;
                    await SendMarketSnapshot();
                    await SendPortfolioUpdate();
                    await _server!.SendAsync("GameLoaded", new { success = true });
                }
                else
                {
                    await _server!.SendAsync("GameLoaded", new { success = false, error = "No save file found" });
                }
                break;

            case "ListSaves":
                var saves = SaveManager.ListSaves();
                await _server!.SendAsync("SaveList", new { saves });
                break;

            case "shutdown":
                Log.Info("Shutdown requested by frontend");
                _running = false;
                break;

            case "UpdateSettings":
                if (_gameLoop != null)
                {
                    var settingsReq = JsonSerializer.Deserialize<GameSettingsUpdate>(payload,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (settingsReq != null)
                    {
                        // Commission (Bible 16.3)
                        if (settingsReq.TradingCommission == false)
                            OrderEngine.DefaultCommissionOverride = 0m;
                        else if (settingsReq.CommissionAmount.HasValue)
                            OrderEngine.DefaultCommissionOverride = settingsReq.CommissionAmount.Value;

                        // Taxes (Bible 16.3)
                        if (settingsReq.EnableTaxes.HasValue)
                            _gameLoop.TaxEngine.Enabled = settingsReq.EnableTaxes.Value;

                        // SMA enforcement (Bible 16.3)
                        if (settingsReq.SmaEnforcement.HasValue)
                            _gameLoop.SMAEngine.Enabled = settingsReq.SmaEnforcement.Value;

                        // Skip weekends (Bible 16.2)
                        if (settingsReq.SkipWeekends.HasValue)
                            _gameLoop.SkipWeekends = settingsReq.SkipWeekends.Value;

                        // Auto-pause preferences
                        if (settingsReq.AutoPauseOnShortSqueeze.HasValue)
                            _gameLoop.AutoPauseOnShortSqueeze = settingsReq.AutoPauseOnShortSqueeze.Value;
                        if (settingsReq.AutoPauseOnSma.HasValue)
                            _gameLoop.AutoPauseOnSMA = settingsReq.AutoPauseOnSma.Value;

                        Log.Info("Settings updated", new
                        {
                            commission = OrderEngine.DefaultCommissionOverride,
                            taxes = _gameLoop.TaxEngine.Enabled,
                            sma = _gameLoop.SMAEngine.Enabled,
                            skipWeekends = _gameLoop.SkipWeekends,
                        });
                        await _server!.SendAsync("SettingsApplied", new { success = true });
                    }
                }
                break;

            default:
                Log.Warn("Unknown message type", new { type });
                break;
        }
    }

    private static async Task StartNewGameAsync(NewGameConfig? config)
    {
        var seed = config?.Seed ?? new Random().Next();
        var stockCount = config?.StockCount ?? 250;
        var startingCash = config?.StartingCash ?? 50_000m;

        var playerName = config?.PlayerName ?? "Trader";
        Log.Info("Starting new game", new { seed, stockCount, startingCash, playerName });
        _gameLoop = new GameLoop(seed, stockCount, startingCash);
        _gameLoop.PlayerName = playerName;

        // Apply game settings from NewGameScreen
        if (config != null)
        {
            // Commission
            if (config.Commission.HasValue)
                StockSim.Engine.Services.OrderEngine.DefaultCommissionOverride = config.Commission.Value;

            // Volatility multiplier: adjust all stock base volatilities
            if (config.Volatility.HasValue && config.Volatility.Value != 1.0m)
            {
                foreach (var stock in _gameLoop.MutableStocks)
                    stock.BaseVolatility *= config.Volatility.Value;
            }

            // Event frequency: stored for EventEngine to use
            // AI aggression: stored for AITraderEngine to use
            // These are multipliers applied in the engines

            // Event frequency multiplier
            if (config.EventFrequency.HasValue)
                _gameLoop.EventEngine.FrequencyMultiplier = (double)config.EventFrequency.Value;

            // AI aggression multiplier
            if (config.AiAggression.HasValue)
                _gameLoop.AITraderEngine.AggressionMultiplier = (float)config.AiAggression.Value;

            // Tax system
            _gameLoop.TaxEngine.Enabled = true;

            // Margin trading
            if (config.EnableMargin == true)
                _gameLoop.Portfolio.MarginEnabled = true;
        }

        await SendMarketSnapshot();
    }

    private static async Task SendMarketSnapshot()
    {
        if (_gameLoop == null || _server == null) return;

        var snapshot = _gameLoop.Stocks.Select(s => new
        {
            symbol = s.Symbol,
            name = s.Name,
            sector = s.Sector,
            subsector = s.Subsector,
            price = s.CurrentPrice,
            change = s.DayChange,
            changePercent = s.DayChangePercent,
            bid = s.BidPrice,
            ask = s.AskPrice,
            volume = s.DayVolume,
            marketCap = s.MarketCap,
            traits = s.Traits,
            peRatio = s.PERatio,
            dividendYield = s.DividendYield,
            dayHigh = s.DayHigh,
            dayLow = s.DayLow,
            previousClose = s.PreviousClose,
            isSSR = s.IsSSR,
            personality = s.Personality == null ? null : new
            {
                ceoName = s.Personality.CEOName,
                ceoArchetype = s.Personality.CEOArchetype,
                foundedYear = s.Personality.FoundedYear,
                headquarters = s.Personality.Headquarters,
                description = s.Personality.Description,
                flagshipProduct = s.Personality.FlagshipProduct,
                secondaryProduct = s.Personality.SecondaryProduct,
                rivalSymbol = s.Personality.RivalSymbol,
                foundingStory = s.Personality.FoundingStory,
            },
        }).ToList();

        await _server.SendAsync("MarketSnapshot", new
        {
            stocks = snapshot,
            gameTime = _gameLoop.GameTime.ToString("o"),
            speed = (int)_gameLoop.Speed,
            isMarketOpen = _gameLoop.IsMarketOpen(),
            marketPhase = _gameLoop.Phase.ToString(),
        });
    }

    private static async Task RunTickLoop()
    {
        while (_running)
        {
            if (_gameLoop == null || _gameLoop.IsPaused)
            {
                await Task.Delay(50);
                continue;
            }

            var tickStart = DateTime.UtcNow;
            _gameLoop.ExecuteTick();

            // Throttle WebSocket sends at high speeds to avoid bottleneck
            var sendInterval = _gameLoop.Speed switch
            {
                GameSpeed.Maximum => 10,    // Send every 10th tick
                GameSpeed.VeryFast => 5,    // Send every 5th tick
                _ => 1,
            };
            var shouldSend = _gameLoop.TickCount % sendInterval == 0;

            if (_server!.IsClientConnected && shouldSend)
            {
                await SendPriceUpdate();

                // Send portfolio update every 5 sends if player has positions
                if (_gameLoop.Portfolio.Positions.Count > 0 && _gameLoop.TickCount % (5 * sendInterval) == 0)
                {
                    await SendPortfolioUpdate();
                }

                // Send new events to frontend for news ticker
                if (_gameLoop.EventEngine.NewEventsThisTick.Count > 0)
                {
                    await SendNewsEvents();
                }

                // Send dividend announcements as batched news
                if (_gameLoop.DividendEngine.NewAnnouncementsThisTick.Count > 0)
                {
                    var divEvents = _gameLoop.DividendEngine.NewAnnouncementsThisTick.Select(div => new
                    {
                        id = 0,
                        type = "Company",
                        severity = "Minor",
                        sentiment = 0.2f,
                        headline = $"{div.Symbol} declares quarterly dividend of ${div.DividendPerShare:F2}/share. Ex-date: {div.ExDividendDate:MMM dd}.",
                        affectedSymbols = new[] { div.Symbol },
                        affectedSectors = Array.Empty<string>(),
                        priceEffect = 0f,
                        timestamp = _gameLoop.GameTime.ToString("o"),
                    }).ToList();
                    await _server.SendAsync("NewsEvents", new { events = divEvents });
                }

                // Send dividend payment notifications
                foreach (var pay in _gameLoop.DividendEngine.PaymentsThisTick)
                {
                    await _server.SendAsync("DividendPaid", new
                    {
                        symbol = pay.Symbol,
                        shares = pay.Shares,
                        dividendPerShare = pay.DividendPerShare,
                        gross = pay.GrossDividend,
                        tax = pay.Tax,
                        net = pay.NetDividend,
                    });
                }

                // Send IPO/Delisting news
                if (_gameLoop.IPOEngine.NewsThisTick.Count > 0)
                {
                    var ipoEvents = _gameLoop.IPOEngine.NewsThisTick.Select(h => new
                    {
                        id = 0, type = "Company", severity = "Major",
                        sentiment = h.Contains("delisted") ? -0.8f : 0.5f,
                        headline = h,
                        affectedSymbols = Array.Empty<string>(),
                        affectedSectors = Array.Empty<string>(),
                        priceEffect = 0f,
                        timestamp = _gameLoop.GameTime.ToString("o"),
                    }).ToList();
                    await _server.SendAsync("NewsEvents", new { events = ipoEvents });

                    // If new stocks were added, send updated snapshot
                    if (_gameLoop.IPOEngine.NewIPOsThisTick.Count > 0)
                    {
                        await SendMarketSnapshot();
                    }
                }

                // Send SMA notifications (regulatory warnings, investigations, penalties)
                if (_gameLoop.SMAEngine.NotificationsThisTick.Count > 0)
                {
                    var smaNotifs = _gameLoop.SMAEngine.NotificationsThisTick.Select(n => new
                    {
                        type = n.Type.ToString(),
                        title = n.Title,
                        message = n.Message,
                        severity = n.Severity,
                        time = n.Time.ToString("o"),
                        pauseGame = n.PauseGame,
                    }).ToList();
                    await _server.SendAsync("SMANotifications", new { notifications = smaNotifs });

                    // Also inject SMA news into the news ticker
                    var smaNewsEvents = _gameLoop.SMAEngine.NotificationsThisTick
                        .Where(n => n.Type != Services.SMANotificationType.AmbientNews || true) // all of them
                        .Select(n => new
                        {
                            id = 0,
                            type = "Company",
                            severity = n.Severity == "critical" ? "Major" : "Moderate",
                            sentiment = n.Severity == "critical" ? -0.5f : -0.2f,
                            headline = $"{n.Title}: {n.Message}",
                            affectedSymbols = Array.Empty<string>(),
                            affectedSectors = Array.Empty<string>(),
                            priceEffect = 0f,
                            timestamp = _gameLoop.GameTime.ToString("o"),
                        }).ToList();
                    await _server.SendAsync("NewsEvents", new { events = smaNewsEvents });
                }

                // Check price alerts
                foreach (var alert in _gameLoop.Portfolio.PriceAlerts.Where(a => a.Active).ToList())
                {
                    var alertStock = _gameLoop.StocksBySymbol.GetValueOrDefault(alert.Symbol);
                    if (alertStock == null) continue;

                    var triggered = (alert.Condition == "above" && alertStock.CurrentPrice >= alert.TargetPrice)
                                 || (alert.Condition == "below" && alertStock.CurrentPrice <= alert.TargetPrice);

                    if (triggered)
                    {
                        alert.Active = false;
                        alert.Triggered = true;
                        await _server.SendAsync("AlertTriggered", new
                        {
                            id = alert.Id,
                            symbol = alert.Symbol,
                            condition = alert.Condition,
                            targetPrice = alert.TargetPrice,
                            currentPrice = alertStock.CurrentPrice,
                        });
                        // Pause game on alert (Bible 3.5.4)
                        _gameLoop.SetSpeed(GameSpeed.Paused);
                        await _server.SendAsync("SpeedChanged", new { speed = 0 });
                    }
                }

                // Rumors → send as special news events with "Rumor" type (Bible 4.8)
                if (_gameLoop.RumorEngine.NewRumorsThisTick.Count > 0)
                {
                    var rumorNews = _gameLoop.RumorEngine.NewRumorsThisTick.Select(r => new
                    {
                        id = r.Id,
                        type = "Rumor",
                        severity = "Moderate",
                        sentiment = 0f, // Neutral — rumors are uncertain
                        headline = r.Headline,
                        affectedSymbols = new[] { r.Symbol },
                        affectedSectors = Array.Empty<string>(),
                        priceEffect = 0f,
                        timestamp = r.CreatedAt.ToString("o"),
                    }).ToList();
                    await _server.SendAsync("NewsEvents", new { events = rumorNews });
                }

                // Short squeeze warnings (Bible 4.4.5)
                if (_gameLoop.ShortSqueezeWarningsThisTick.Count > 0)
                {
                    foreach (var sq in _gameLoop.ShortSqueezeWarningsThisTick)
                    {
                        // Breaking news event
                        await _server.SendAsync("NewsEvents", new
                        {
                            events = new[] { new
                            {
                                id = 0, type = "Company", severity = "Major",
                                sentiment = 0.8f,
                                headline = $"SHORT SQUEEZE: Short sellers scrambling to cover positions in {sq.Symbol} as stock surges {sq.PriceChangePercent:F1}%. Short interest at {sq.ShortInterestPercent:F1}%.",
                                affectedSymbols = new[] { sq.Symbol },
                                affectedSectors = Array.Empty<string>(),
                                priceEffect = 0.05f,
                                timestamp = _gameLoop.GameTime.ToString("o"),
                            }}
                        });

                        // Dedicated short squeeze notification
                        await _server.SendAsync("ShortSqueezeWarning", new
                        {
                            symbol = sq.Symbol,
                            companyName = sq.CompanyName,
                            priceChangePercent = sq.PriceChangePercent,
                            shortInterestPercent = sq.ShortInterestPercent,
                            playerHasShortPosition = sq.PlayerHasShortPosition,
                        });
                    }

                    // Auto-pause on short squeeze (conditional — Bible 16.2)
                    if (_gameLoop.AutoPauseOnShortSqueeze)
                    {
                        _gameLoop.SetSpeed(GameSpeed.Paused);
                        await _server.SendAsync("SpeedChanged", new { speed = 0 });
                    }
                }

                // Insider trades → generate news events
                if (_gameLoop.InsiderTradesThisTick.Count > 0)
                {
                    var insiderNews = _gameLoop.InsiderTradesThisTick.Select(it => new
                    {
                        id = 0, type = "Company", severity = it.Value > 500_000 ? "Moderate" : "Minor",
                        sentiment = it.IsBuy ? 0.15f : -0.1f,
                        headline = $"{it.Symbol} {it.Title} {(it.IsBuy ? "BUYS" : "SELLS")} {it.Shares:N0} shares (${it.Value:N0}) @ ${it.Price:F2}",
                        affectedSymbols = new[] { it.Symbol },
                        affectedSectors = Array.Empty<string>(),
                        priceEffect = 0f,
                        timestamp = _gameLoop.GameTime.ToString("o"),
                    }).ToList();
                    await _server.SendAsync("NewsEvents", new { events = insiderNews });
                }

                // Stock splits → generate news events
                if (_gameLoop.SplitsThisTick.Count > 0)
                {
                    var splitNews = _gameLoop.SplitsThisTick.Select(sp => new
                    {
                        id = 0, type = "Company", severity = "Moderate",
                        sentiment = sp.Ratio.StartsWith("1:") ? -0.2f : 0.2f,
                        headline = $"{sp.Symbol} announces {sp.Ratio} stock split. Price adjusted from ${sp.OldPrice:F2} to ${sp.NewPrice:F2}.",
                        affectedSymbols = new[] { sp.Symbol },
                        affectedSectors = Array.Empty<string>(),
                        priceEffect = 0f,
                        timestamp = _gameLoop.GameTime.ToString("o"),
                    }).ToList();
                    await _server.SendAsync("NewsEvents", new { events = splitNews });
                    await SendMarketSnapshot(); // Refresh all stock data
                }

                // Earnings releases → generate news events
                if (_gameLoop.EarningsEngine.ReleasedThisTick.Count > 0)
                {
                    var earningsNews = _gameLoop.EarningsEngine.ReleasedThisTick.Select(e =>
                    {
                        var beatMiss = e.Beat ? "BEATS" : "MISSES";
                        var sentiment = e.Beat ? 0.5f : -0.5f;
                        return new
                        {
                            id = 0, type = "Company", severity = "Major",
                            sentiment,
                            headline = $"{e.Symbol} Q{e.Quarter} Earnings: EPS ${e.ActualEPS:F2} {beatMiss} est. ${e.ExpectedEPS:F2} ({e.EPSSurprisePercent:+0.0;-0.0}%) | Stock {(e.PriceImpactPercent >= 0 ? "+" : "")}{e.PriceImpactPercent:F1}%",
                            affectedSymbols = new[] { e.Symbol },
                            affectedSectors = Array.Empty<string>(),
                            priceEffect = (float)(e.PriceImpactPercent / 100),
                            timestamp = _gameLoop.GameTime.ToString("o"),
                        };
                    }).ToList();
                    await _server.SendAsync("NewsEvents", new { events = earningsNews });
                }

                // Economic data releases → generate news events
                if (_gameLoop.EconomicEngine.ReleasedThisTick.Count > 0)
                {
                    var econEvents = _gameLoop.EconomicEngine.ReleasedThisTick.Select(e =>
                    {
                        var direction = e.Surprise > 0 ? "beats" : "misses";
                        var sentiment = e.Surprise > 0 ? 0.3f : -0.3f;
                        if (e.Indicator == "UnemploymentRate") sentiment = -sentiment; // Inverse
                        return new
                        {
                            id = 0, type = "Macro", severity = e.Impact == "High" ? "Major" : "Moderate",
                            sentiment,
                            headline = $"{e.Name}: {e.ActualValue:F2} ({direction} est. {e.ExpectedValue:F2})",
                            affectedSymbols = Array.Empty<string>(),
                            affectedSectors = Array.Empty<string>(),
                            priceEffect = (float)(e.Surprise * 0.01m),
                            timestamp = _gameLoop.GameTime.ToString("o"),
                        };
                    }).ToList();
                    await _server.SendAsync("NewsEvents", new { events = econEvents });
                }

                // M&A / Tender Offer notifications (Bible 8.2.7)
                if (_gameLoop.EventEngine.MAndAEventsThisTick.Count > 0)
                {
                    foreach (var mna in _gameLoop.EventEngine.MAndAEventsThisTick)
                    {
                        // Check if player holds target stock — send tender offer popup
                        if (_gameLoop.Portfolio.Positions.ContainsKey(mna.TargetSymbol))
                        {
                            var pos = _gameLoop.Portfolio.Positions[mna.TargetSymbol];
                            await _server.SendAsync("TenderOffer", new
                            {
                                targetSymbol = mna.TargetSymbol,
                                targetName = mna.TargetName,
                                acquirerName = mna.AcquirerName,
                                offerPrice = mna.OfferPrice,
                                premiumPercent = mna.PremiumPercent,
                                currentPrice = _gameLoop.StocksBySymbol.TryGetValue(mna.TargetSymbol, out var ts) ? ts.CurrentPrice : 0m,
                                playerShares = Math.Abs(pos.Shares),
                                totalPayout = Math.Abs(pos.Shares) * mna.OfferPrice,
                            });

                            // Auto-pause for player decision
                            _gameLoop.SetSpeed(GameSpeed.Paused);
                            await _server.SendAsync("SpeedChanged", new { speed = 0 });
                        }
                    }
                }

                // Margin call notification
                if (_gameLoop.MarginCallThisTick)
                {
                    await _server.SendAsync("MarginCall", new
                    {
                        message = "MARGIN CALL: Position force-liquidated to cover margin requirements.",
                        marginBalance = _gameLoop.Portfolio.MarginBalance,
                    });
                    await _server.SendAsync("NewsEvents", new { events = new[] { new {
                        id = 0, type = "Company", severity = "Major", sentiment = -0.8f,
                        headline = "MARGIN CALL: Forced liquidation triggered due to insufficient equity.",
                        affectedSymbols = Array.Empty<string>(), affectedSectors = Array.Empty<string>(),
                        priceEffect = 0f, timestamp = _gameLoop.GameTime.ToString("o"),
                    }}});
                }

                // Bankruptcy notification
                if (_gameLoop.IsBankrupt)
                {
                    await _server.SendAsync("Bankruptcy", new
                    {
                        message = "You've gone bankrupt.",
                        restartCash = 10_000,
                    });
                }

                // Scenario completion notification
                if (_gameLoop.ScenarioResult != null)
                {
                    var sr = _gameLoop.ScenarioResult;
                    await _server.SendAsync("ScenarioCompleted", new
                    {
                        scenarioId = sr.ScenarioId,
                        scenarioName = sr.ScenarioName,
                        won = sr.Won,
                        daysElapsed = sr.DaysElapsed,
                        finalPortfolioValue = sr.FinalPortfolioValue,
                        totalReturn = sr.TotalReturn,
                        totalReturnPercent = sr.TotalReturnPercent,
                        totalTrades = sr.TotalTrades,
                        winRate = sr.WinRate,
                        failReason = sr.FailReason,
                    });
                    // Clear after sending so we don't send repeatedly
                    // The result stays in the ScenarioResult property for reference
                    _gameLoop.ActiveScenario!.IsActive = false;
                }

                // Achievement unlock notifications
                if (_gameLoop.AchievementEngine.NewUnlocksThisTick.Count > 0)
                {
                    foreach (var ach in _gameLoop.AchievementEngine.NewUnlocksThisTick)
                    {
                        await _server.SendAsync("AchievementUnlocked", new
                        {
                            id = ach.Id,
                            name = ach.Name,
                            description = ach.Description,
                            category = ach.Category.ToString(),
                        });
                    }
                }

                // Day Summary at market close (4:00 PM)
                if (_gameLoop.GameTime.TimeOfDay == new TimeSpan(16, 0, 0))
                {
                    Func<string, decimal> gprice = sym =>
                        _gameLoop.StocksBySymbol.GetValueOrDefault(sym)?.CurrentPrice ?? 0m;

                    var topGainer = _gameLoop.Stocks.OrderByDescending(s => s.DayChangePercent).First();
                    var topLoser = _gameLoop.Stocks.OrderBy(s => s.DayChangePercent).First();
                    var avgChange = _gameLoop.Stocks.Average(s => (double)s.DayChangePercent);

                    // Sector performance
                    var sectorPerf = _gameLoop.Stocks
                        .Where(s => !s.Traits.Contains("ETF"))
                        .GroupBy(s => s.Sector)
                        .Select(g => new { sector = g.Key, change = Math.Round((double)g.Average(s => s.DayChangePercent), 2) })
                        .OrderByDescending(s => s.change)
                        .Take(5).ToList();

                    // Earnings released today
                    var todayEarnings = _gameLoop.EarningsEngine.GetRecent(10)
                        .Where(e => e.ReportDate.Date == _gameLoop.GameTime.Date)
                        .Select(e => new { symbol = e.Symbol, beat = e.Beat, surprise = e.EPSSurprisePercent })
                        .ToList();

                    // Economic events today
                    var todayEconEvents = _gameLoop.EconomicEngine.EventHistory
                        .Where(e => e.ScheduledDate.Date == _gameLoop.GameTime.Date)
                        .Select(e => new { name = e.Name, actual = e.ActualValue, expected = e.ExpectedValue })
                        .ToList();

                    var advancing = _gameLoop.Stocks.Count(s => s.DayChangePercent > 0);
                    var declining = _gameLoop.Stocks.Count(s => s.DayChangePercent < 0);

                    await _server.SendAsync("DaySummary", new
                    {
                        date = _gameLoop.GameTime.ToString("yyyy-MM-dd"),
                        marketChange = Math.Round(avgChange, 2),
                        topGainer = new { symbol = topGainer.Symbol, change = topGainer.DayChangePercent },
                        topLoser = new { symbol = topLoser.Symbol, change = topLoser.DayChangePercent },
                        portfolioValue = _gameLoop.Portfolio.TotalEquity(gprice),
                        dailyPnL = _gameLoop.Portfolio.TotalUnrealizedPnL(gprice) + _gameLoop.Portfolio.RealizedPnL,
                        tradesCount = _gameLoop.Portfolio.TradeCount,
                        eventsCount = _gameLoop.EventEngine.EventHistory.Count,
                        advancing,
                        declining,
                        sectorPerformance = sectorPerf,
                        earningsToday = todayEarnings,
                        economicEventsToday = todayEconEvents,
                    });

                }

                // Autosave every 500 ticks (~8 game-hours at 1 tick/min)
                if (_gameLoop.TickCount > 0 && _gameLoop.TickCount % 500 == 0)
                {
                    var autosavePath = SaveManager.GetDefaultSavePath();
                    await SaveManager.SaveGameAsync(_gameLoop, autosavePath);
                    Log.Info("Autosaved", new { tick = _gameLoop.TickCount, path = autosavePath });
                    await _server!.SendAsync("Autosaved", new { tick = _gameLoop.TickCount });
                }
            }

            var tickMs = (DateTime.UtcNow - tickStart).TotalMilliseconds;
            var targetMs = _gameLoop.Speed switch
            {
                GameSpeed.Normal => 1000,
                GameSpeed.Fast => 500,
                GameSpeed.VeryFast => 100,
                GameSpeed.Maximum => 10,  // As fast as possible
                _ => 1000,
            };

            var sleepMs = Math.Max(0, targetMs - (int)tickMs);
            if (sleepMs > 0) await Task.Delay(sleepMs);
        }
    }

    // Price cache for delta updates (only send changed stocks)
    private static readonly Dictionary<string, decimal> _lastSentPrices = new();

    private static async Task SendPriceUpdate()
    {
        if (_gameLoop == null || _server == null) return;

        // Delta updates: only send stocks whose price actually changed
        var updates = new List<object>();
        foreach (var s in _gameLoop.Stocks)
        {
            if (_lastSentPrices.TryGetValue(s.Symbol, out var lastPrice) && lastPrice == s.CurrentPrice)
                continue; // Price unchanged, skip

            _lastSentPrices[s.Symbol] = s.CurrentPrice;
            updates.Add(new
            {
                s.Symbol,
                price = s.CurrentPrice,
                bid = s.BidPrice,
                ask = s.AskPrice,
                change = s.DayChange,
                changePercent = s.DayChangePercent,
                volume = s.DayVolume,
                dayHigh = s.DayHigh,
                dayLow = s.DayLow,
                isSSR = s.IsSSR,
            });
        }

        // Always send at least game time + market state
        await _server.SendAsync("MarketUpdate", new
        {
            prices = updates,
            gameTime = _gameLoop.GameTime.ToString("o"),
            tick = _gameLoop.TickCount,
            isMarketOpen = _gameLoop.IsMarketOpen(),
            smaStatus = _gameLoop.SMAEngine.State.Status.ToString(),
        });
    }

    private static async Task SendOHLCVData(string symbol, string timeframe = "ALL")
    {
        if (_gameLoop == null || _server == null) return;

        var rawCandles = new List<Candle>();

        // 1. Historical daily candles (252 trading days before game start)
        if (_gameLoop.DailyHistory.TryGetValue(symbol, out var dailyCandles))
            rawCandles.AddRange(dailyCandles);

        // 2. Live 1-minute candles (from current session)
        if (_gameLoop.PriceHistories.TryGetValue(symbol, out var history))
            rawCandles.AddRange(history.Candles);

        // Filter by timeframe
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

        // Aggregate to appropriate resolution based on data range
        List<object> allCandles;
        var dataSpanDays = rawCandles.Count > 1
            ? (rawCandles[^1].Time - rawCandles[0].Time) / 86400.0
            : 0;

        if (dataSpanDays > 60) // >2 months: use daily candles (aggregate 1-min → daily)
        {
            allCandles = AggregateToDailyCandles(rawCandles);
        }
        else if (dataSpanDays > 5) // >5 days: use hourly candles
        {
            allCandles = AggregateToInterval(rawCandles, 3600);
        }
        else // Intraday: use raw 1-min or daily as-is
        {
            allCandles = rawCandles.Select(c => (object)new
            {
                time = c.Time, open = c.Open, high = c.High,
                low = c.Low, close = c.Close, volume = c.Volume,
            }).ToList();
        }

        await _server.SendAsync("OHLCVUpdate", new
        {
            symbol,
            candles = allCandles,
        });

        Log.Info("OHLCV data sent", new { symbol, timeframe, candles = allCandles.Count });
    }

    private static List<object> AggregateToDailyCandles(List<Candle> candles)
    {
        return candles
            .GroupBy(c => c.Time / 86400) // Group by day
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

    private static async Task HandlePlaceOrder(PlaceOrderRequest req)
    {
        if (_gameLoop == null || _server == null) return;

        var stock = _gameLoop.StocksBySymbol.GetValueOrDefault(req.Symbol);
        if (stock == null)
        {
            await _server.SendAsync("OrderResult", new { success = false, error = $"Unknown symbol: {req.Symbol}" });
            return;
        }

        var side = Enum.Parse<OrderSide>(req.Side, ignoreCase: true);
        var type = Enum.Parse<OrderType>(req.Type, ignoreCase: true);
        var tif = TimeInForce.GTC;
        if (!string.IsNullOrEmpty(req.TimeInForce))
            Enum.TryParse(req.TimeInForce, ignoreCase: true, out tif);

        var result = _gameLoop.OrderEngine.PlaceOrder(
            req.Symbol, side, type, req.Quantity, stock,
            _gameLoop.GameTime, _gameLoop.IsMarketOpen(),
            req.LimitPrice, tif, req.StopPrice, req.TrailAmount);

        await _server.SendAsync("OrderResult", new
        {
            success = result.Success,
            error = result.Error,
            order = result.Order == null ? null : new
            {
                id = result.Order.Id,
                symbol = result.Order.Symbol,
                side = result.Order.Side.ToString(),
                type = result.Order.Type.ToString(),
                status = result.Order.Status.ToString(),
                quantity = result.Order.Quantity,
                filledQuantity = result.Order.FilledQuantity,
                fillPrice = result.Order.FillPrice,
                commission = result.Order.Commission,
                limitPrice = result.Order.LimitPrice,
                placedAt = result.Order.PlacedAt.ToString("o"),
                filledAt = result.Order.FilledAt?.ToString("o"),
                rejectReason = result.Order.RejectReason,
            }
        });

        // Send updated portfolio and orders after order
        await SendPortfolioUpdate();
        await SendOrdersUpdate();
    }

    private static async Task SendPortfolioUpdate()
    {
        if (_gameLoop == null || _server == null) return;

        Func<string, decimal> getPrice = symbol =>
            _gameLoop.StocksBySymbol.GetValueOrDefault(symbol)?.CurrentPrice ?? 0m;

        var positions = _gameLoop.Portfolio.Positions.Values.Select(p =>
        {
            var price = getPrice(p.Symbol);
            return new
            {
                symbol = p.Symbol,
                shares = p.Shares,
                averageCost = p.AverageCost,
                marketValue = p.MarketValue(price),
                unrealizedPnL = p.UnrealizedPnL(price),
                unrealizedPnLPercent = p.UnrealizedPnLPercent(price),
            };
        }).ToList();

        await _server.SendAsync("PortfolioUpdate", new
        {
            cash = _gameLoop.Portfolio.Cash,
            portfolioValue = _gameLoop.Portfolio.PortfolioValue(getPrice),
            totalEquity = _gameLoop.Portfolio.TotalEquity(getPrice),
            realizedPnL = _gameLoop.Portfolio.RealizedPnL,
            totalCommissions = _gameLoop.Portfolio.TotalCommissions,
            tradeCount = _gameLoop.Portfolio.TradeCount,
            positions,
            // Margin data
            marginEnabled = _gameLoop.Portfolio.MarginEnabled,
            marginBalance = _gameLoop.Portfolio.MarginBalance,
            buyingPower = _gameLoop.Portfolio.BuyingPower(getPrice),
            marginUsedPercent = _gameLoop.Portfolio.MarginUsedPercent(getPrice),
        });
    }

    private static async Task SendOrdersUpdate()
    {
        if (_gameLoop == null || _server == null) return;

        var orders = _gameLoop.Portfolio.Orders.Select(o => new
        {
            id = o.Id,
            symbol = o.Symbol,
            side = o.Side.ToString(),
            type = o.Type.ToString(),
            status = o.Status.ToString(),
            quantity = o.Quantity,
            filledQuantity = o.FilledQuantity,
            limitPrice = o.LimitPrice,
            fillPrice = o.FillPrice,
            commission = o.Commission,
            placedAt = o.PlacedAt.ToString("o"),
            filledAt = o.FilledAt?.ToString("o"),
            rejectReason = o.RejectReason,
        }).ToList();

        await _server.SendAsync("OrdersUpdate", new { orders });
    }

    private static async Task SendNewsEvents()
    {
        if (_gameLoop == null || _server == null) return;

        var events = _gameLoop.EventEngine.NewEventsThisTick.Select(e => new
        {
            id = e.Id,
            type = e.Type.ToString(),
            severity = e.Severity.ToString(),
            sentiment = e.Sentiment,
            headline = e.Headline,
            affectedSymbols = e.AffectedSymbols,
            affectedSectors = e.AffectedSectors,
            priceEffect = e.PriceEffect,
            timestamp = e.TriggeredAt.ToString("o"),
            // Phase 1E: Rich event fields
            summary = e.Summary,
            analystQuote = e.AnalystQuote,
            analystName = e.AnalystName,
            analystFirm = e.AnalystFirm,
            tier = (int)e.Tier,
            tags = e.Tags,
        }).ToList();

        await _server.SendAsync("NewsEvents", new { events });
    }

    private static async Task SendIndicators(string symbol, string[] indicators)
    {
        if (_gameLoop == null || _server == null) return;

        // Get all candles (daily history + live)
        var allCandles = new List<Candle>();
        if (_gameLoop.DailyHistory.TryGetValue(symbol, out var daily))
            allCandles.AddRange(daily);
        if (_gameLoop.PriceHistories.TryGetValue(symbol, out var live))
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

        await _server.SendAsync("IndicatorData", new { symbol, indicators = result });
    }

    private record NewGameConfig(
        int? Seed, int? StockCount, decimal? StartingCash,
        decimal? Commission, decimal? Volatility, decimal? EventFrequency,
        decimal? AiAggression, bool? EnableMargin, bool? EnableBankruptcy,
        bool? EnableDividends, bool? EnableShortSelling, bool? EnableEvents,
        string? MarketHours, string? PlayerName, bool? ShowTutorial);
    private record SpeedConfig(int Speed);
    private record OHLCVRequest(string Symbol);
    private record PlaceOrderRequest(string Symbol, string Side, string Type, decimal Quantity, decimal? LimitPrice, string? TimeInForce, decimal? StopPrice, decimal? TrailAmount);
    private record CancelOrderRequest(long OrderId);
    private record SaveGameRequest(string? SlotName);
    private record LoadGameRequest(string? SlotName);
    private record TenderOfferResponse(string Symbol, decimal OfferPrice);
    private record GameSettingsUpdate(
        bool? TradingCommission,
        decimal? CommissionAmount,
        bool? EnableTaxes,
        bool? SmaEnforcement,
        bool? SkipWeekends,
        bool? AutoPauseOnShortSqueeze,
        bool? AutoPauseOnSma
    );
    private record SetAlertRequest(string Symbol, string Condition, decimal TargetPrice);
    private record DeleteAlertRequest(long AlertId);
    private record IndicatorRequest(string Symbol, string[]? Indicators);
    private record StartScenarioRequest(string ScenarioId);
    private record OHLCVRequestEx(string Symbol, string? Timeframe);
    private record BracketOrderRequest(string Symbol, decimal Quantity, decimal TakeProfitPrice, decimal StopLossPrice);
}
