using StockSim.Engine.Models;
using StockSim.Engine.Services;
using StockSim.Engine.Services.Handlers;
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
    private static bool _running = true;
    private static GameContext? _ctx;

    // Price cache for delta updates (only send changed stocks)
    private static readonly Dictionary<string, decimal> _lastSentPrices = new();

    public static async Task Main(string[] args)
    {
        Log.Info("StockSim Engine starting", new { version = "0.2.0", pid = Environment.ProcessId });

        var server = WebSocketServer.CreateOnFreePort();
        _ctx = new GameContext(server);

        // Set up message router with all handlers
        var router = new MessageRouter();
        router.Register(new GameControlHandler(_ctx, () => _running = false));
        router.Register(new TradingHandler(_ctx));
        router.Register(new DataQueryHandler(_ctx));
        router.Register(new PersistenceHandler(_ctx));
        router.Register(new ConfigHandler(_ctx));

        server.OnMessage(async (type, payload) =>
        {
            Log.Info("Message received", new { type });
            await router.RouteAsync(type, payload);
        });
        server.OnClientConnected += () => Log.Info("Frontend connected");
        server.OnClientDisconnected += () => Log.Info("Frontend disconnected");

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            _running = false;
            Log.Info("Shutdown requested");
        };

        var serverTask = server.StartAsync();
        var tickTask = RunTickLoop();

        await Task.WhenAny(serverTask, tickTask);

        server.Dispose();
        Log.Info("StockSim Engine stopped");
    }

    private static async Task RunTickLoop()
    {
        while (_running)
        {
            var gameLoop = _ctx!.GameLoop;
            var server = _ctx.Server;

            if (gameLoop == null || gameLoop.IsPaused)
            {
                await Task.Delay(50);
                continue;
            }

            var tickStart = DateTime.UtcNow;
            var speedBeforeTick = gameLoop.Speed;
            try
            {
                gameLoop.ExecuteTick();
            }
            catch (Exception ex)
            {
                Log.Error("ExecuteTick failed, pausing game", new { error = ex.Message, stack = ex.StackTrace?[..Math.Min(200, ex.StackTrace?.Length ?? 0)] });
                gameLoop.SetSpeed(GameSpeed.Paused);
                if (server.IsClientConnected)
                    await server.SendAsync("error", new { message = $"Game error: {ex.Message}. Game paused." });
                continue;
            }

            // Notify frontend if auto-pause was triggered during tick
            if (gameLoop.Speed != speedBeforeTick && gameLoop.IsPaused && server.IsClientConnected)
            {
                await server.SendAsync("SpeedChanged", new { speed = 0 });
            }

            // Throttle WebSocket sends at high speeds to avoid bottleneck
            var sendInterval = gameLoop.Speed switch
            {
                GameSpeed.Maximum => 10,
                GameSpeed.VeryFast => 5,
                _ => 1,
            };
            var shouldSend = gameLoop.TickCount % sendInterval == 0;

            if (server.IsClientConnected && shouldSend)
            {
                await SendHelper.SendPriceUpdate(_ctx, _lastSentPrices);

                // Send portfolio update every 5 sends if player has positions
                if (gameLoop.Portfolio.Positions.Count > 0 && gameLoop.TickCount % (5 * sendInterval) == 0)
                {
                    await SendHelper.SendPortfolioUpdate(_ctx);
                }

                // Send scenario progress if active (every 10 ticks to reduce traffic)
                if (gameLoop.ActiveScenario is { IsActive: true } scen && gameLoop.TickCount % 10 == 0)
                {
                    var equity = gameLoop.Portfolio.TotalEquity(sym =>
                        gameLoop.StocksBySymbol.TryGetValue(sym, out var st) ? st.CurrentPrice : 0m);
                    var daysRemaining = scen.TimeLimitDays > 0 ? scen.TimeLimitDays - scen.DaysElapsed : (int?)null;
                    var winDesc = scen.TargetPortfolioValue.HasValue
                        ? $"Reach ${scen.TargetPortfolioValue.Value:N0}"
                        : scen.TargetDividendIncome.HasValue
                            ? $"Earn ${scen.TargetDividendIncome.Value:N0}/quarter in dividends"
                            : "Survive";
                    var loseDesc = scen.TimeLimitDays > 0 ? $"Time limit: {scen.TimeLimitDays} days" : "";
                    await server.SendAsync("ScenarioProgress", new
                    {
                        scenarioId = scen.Id,
                        scenarioName = scen.Name,
                        difficulty = scen.Difficulty.ToLower(),
                        targetValue = scen.TargetPortfolioValue ?? scen.StartingCash,
                        targetDescription = winDesc,
                        startingCash = scen.StartingCash,
                        currentEquity = equity,
                        daysElapsed = scen.DaysElapsed,
                        daysRemaining,
                        tradingDaysRemaining = daysRemaining.HasValue ? (int)(daysRemaining.Value * 5.0 / 7.0) : (int?)null,
                        winCondition = winDesc,
                        loseCondition = loseDesc,
                    });
                }

                // Send new events to frontend for news ticker
                if (gameLoop.EventEngine.NewEventsThisTick.Count > 0)
                {
                    await SendHelper.SendNewsEvents(_ctx);
                }

                // Send dividend announcements as batched news
                if (gameLoop.DividendEngine.NewAnnouncementsThisTick.Count > 0)
                {
                    var divEvents = gameLoop.DividendEngine.NewAnnouncementsThisTick.Select(div => new
                    {
                        id = 0,
                        type = "Company",
                        severity = "Minor",
                        sentiment = 0.2f,
                        headline = $"{div.Symbol} declares quarterly dividend of ${div.DividendPerShare:F2}/share. Ex-date: {div.ExDividendDate:MMM dd}.",
                        affectedSymbols = new[] { div.Symbol },
                        affectedSectors = Array.Empty<string>(),
                        priceEffect = 0f,
                        timestamp = gameLoop.GameTime.ToString("o"),
                    }).ToList();
                    await server.SendAsync("NewsEvents", new { events = divEvents });
                }

                // Send dividend payment notifications
                foreach (var pay in gameLoop.DividendEngine.PaymentsThisTick)
                {
                    await server.SendAsync("DividendPaid", new
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
                if (gameLoop.IPOEngine.NewsThisTick.Count > 0)
                {
                    var ipoEvents = gameLoop.IPOEngine.NewsThisTick.Select(h => new
                    {
                        id = 0, type = "Company", severity = "Major",
                        sentiment = h.Contains("delisted") ? -0.8f : 0.5f,
                        headline = h,
                        affectedSymbols = Array.Empty<string>(),
                        affectedSectors = Array.Empty<string>(),
                        priceEffect = 0f,
                        timestamp = gameLoop.GameTime.ToString("o"),
                    }).ToList();
                    await server.SendAsync("NewsEvents", new { events = ipoEvents });

                    // If new stocks were added, send updated snapshot
                    if (gameLoop.IPOEngine.NewIPOsThisTick.Count > 0)
                    {
                        await SendHelper.SendMarketSnapshot(_ctx);
                    }
                }

                // Send SMA notifications (regulatory warnings, investigations, penalties)
                if (gameLoop.SMAEngine.NotificationsThisTick.Count > 0)
                {
                    var smaNotifs = gameLoop.SMAEngine.NotificationsThisTick.Select(n => new
                    {
                        type = n.Type.ToString(),
                        title = n.Title,
                        message = n.Message,
                        severity = n.Severity,
                        time = n.Time.ToString("o"),
                        pauseGame = n.PauseGame,
                    }).ToList();
                    await server.SendAsync("SMANotifications", new { notifications = smaNotifs });

                    // Also inject SMA news into the news ticker
                    var smaNewsEvents = gameLoop.SMAEngine.NotificationsThisTick
                        .Where(n => n.Type != Services.SMANotificationType.AmbientNews || true)
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
                            timestamp = gameLoop.GameTime.ToString("o"),
                        }).ToList();
                    await server.SendAsync("NewsEvents", new { events = smaNewsEvents });
                }

                // Check price alerts
                foreach (var alert in gameLoop.Portfolio.PriceAlerts.Where(a => a.Active).ToList())
                {
                    var alertStock = gameLoop.StocksBySymbol.GetValueOrDefault(alert.Symbol);
                    if (alertStock == null) continue;

                    var triggered = (alert.Condition == "above" && alertStock.CurrentPrice >= alert.TargetPrice)
                                 || (alert.Condition == "below" && alertStock.CurrentPrice <= alert.TargetPrice);

                    if (triggered)
                    {
                        alert.Active = false;
                        alert.Triggered = true;
                        await server.SendAsync("AlertTriggered", new
                        {
                            id = alert.Id,
                            symbol = alert.Symbol,
                            condition = alert.Condition,
                            targetPrice = alert.TargetPrice,
                            currentPrice = alertStock.CurrentPrice,
                        });
                        if (gameLoop.AutoPauseOnAlert)
                        {
                            gameLoop.SetSpeed(GameSpeed.Paused);
                            await server.SendAsync("SpeedChanged", new { speed = 0 });
                        }
                    }
                }

                // Rumors
                if (gameLoop.RumorEngine.NewRumorsThisTick.Count > 0)
                {
                    var rumorNews = gameLoop.RumorEngine.NewRumorsThisTick.Select(r => new
                    {
                        id = r.Id,
                        type = "Rumor",
                        severity = "Moderate",
                        sentiment = 0f,
                        headline = r.Headline,
                        affectedSymbols = new[] { r.Symbol },
                        affectedSectors = Array.Empty<string>(),
                        priceEffect = 0f,
                        timestamp = r.CreatedAt.ToString("o"),
                    }).ToList();
                    await server.SendAsync("NewsEvents", new { events = rumorNews });
                }

                // Short squeeze warnings
                if (gameLoop.ShortSqueezeWarningsThisTick.Count > 0)
                {
                    foreach (var sq in gameLoop.ShortSqueezeWarningsThisTick)
                    {
                        await server.SendAsync("NewsEvents", new
                        {
                            events = new[] { new
                            {
                                id = 0, type = "Company", severity = "Major",
                                sentiment = 0.8f,
                                headline = $"SHORT SQUEEZE: Short sellers scrambling to cover positions in {sq.Symbol} as stock surges {sq.PriceChangePercent:F1}%. Short interest at {sq.ShortInterestPercent:F1}%.",
                                affectedSymbols = new[] { sq.Symbol },
                                affectedSectors = Array.Empty<string>(),
                                priceEffect = 0.05f,
                                timestamp = gameLoop.GameTime.ToString("o"),
                            }}
                        });

                        await server.SendAsync("ShortSqueezeWarning", new
                        {
                            symbol = sq.Symbol,
                            companyName = sq.CompanyName,
                            priceChangePercent = sq.PriceChangePercent,
                            shortInterestPercent = sq.ShortInterestPercent,
                            playerHasShortPosition = sq.PlayerHasShortPosition,
                        });
                    }

                    if (gameLoop.AutoPauseOnShortSqueeze)
                    {
                        gameLoop.SetSpeed(GameSpeed.Paused);
                        await server.SendAsync("SpeedChanged", new { speed = 0 });
                    }
                    gameLoop.ShortSqueezeWarningsThisTick.Clear();
                }

                // Insider trades
                if (gameLoop.InsiderTradesThisTick.Count > 0)
                {
                    var insiderNews = gameLoop.InsiderTradesThisTick.Select(it => new
                    {
                        id = 0, type = "Company", severity = it.Value > 500_000 ? "Moderate" : "Minor",
                        sentiment = it.IsBuy ? 0.15f : -0.1f,
                        headline = $"{it.Symbol} {it.Title} {(it.IsBuy ? "BUYS" : "SELLS")} {it.Shares:N0} shares (${it.Value:N0}) @ ${it.Price:F2}",
                        affectedSymbols = new[] { it.Symbol },
                        affectedSectors = Array.Empty<string>(),
                        priceEffect = 0f,
                        timestamp = gameLoop.GameTime.ToString("o"),
                    }).ToList();
                    await server.SendAsync("NewsEvents", new { events = insiderNews });
                    gameLoop.InsiderTradesThisTick.Clear();
                }

                // Stock splits
                if (gameLoop.SplitsThisTick.Count > 0)
                {
                    var splitNews = gameLoop.SplitsThisTick.Select(sp => new
                    {
                        id = 0, type = "Company", severity = "Moderate",
                        sentiment = sp.Ratio.StartsWith("1:") ? -0.2f : 0.2f,
                        headline = $"{sp.Symbol} announces {sp.Ratio} stock split. Price adjusted from ${sp.OldPrice:F2} to ${sp.NewPrice:F2}.",
                        affectedSymbols = new[] { sp.Symbol },
                        affectedSectors = Array.Empty<string>(),
                        priceEffect = 0f,
                        timestamp = gameLoop.GameTime.ToString("o"),
                    }).ToList();
                    await server.SendAsync("NewsEvents", new { events = splitNews });
                    gameLoop.SplitsThisTick.Clear();
                    await SendHelper.SendMarketSnapshot(_ctx);
                }

                // GEX (Gamma Exposure) alerts
                if (gameLoop.OptionsEngine.GexNewsThisTick.Count > 0)
                {
                    var gexNews = gameLoop.OptionsEngine.GexNewsThisTick.Select(headline => new
                    {
                        id = 0, type = "Company", severity = "Moderate",
                        sentiment = headline.Contains("negative") ? -0.3f : 0.1f,
                        headline,
                        affectedSymbols = Array.Empty<string>(),
                        affectedSectors = Array.Empty<string>(),
                        priceEffect = 0f,
                        timestamp = gameLoop.GameTime.ToString("o"),
                        tags = new[] { "options", "gamma", "gex", "dealer-hedging" },
                        tier = 2,
                    }).ToList();
                    await server.SendAsync("NewsEvents", new { events = gexNews });
                }

                // Options news events
                if (gameLoop.OptionsEngine.NewsThisTick.Count > 0)
                {
                    var optionsNews = gameLoop.OptionsEngine.NewsThisTick.Select(n => new
                    {
                        id = 0, type = "Company", severity = n.Severity,
                        sentiment = n.Type == OptionsNewsType.IVCrush ? -0.3f : 0f,
                        headline = n.Headline,
                        affectedSymbols = new[] { n.Symbol },
                        affectedSectors = Array.Empty<string>(),
                        priceEffect = 0f,
                        timestamp = gameLoop.GameTime.ToString("o"),
                    }).ToList();
                    await server.SendAsync("NewsEvents", new { events = optionsNews });
                }

                // Earnings releases
                if (gameLoop.EarningsEngine.ReleasedThisTick.Count > 0)
                {
                    var earningsNews = gameLoop.EarningsEngine.ReleasedThisTick.Select(e =>
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
                            timestamp = gameLoop.GameTime.ToString("o"),
                        };
                    }).ToList();
                    await server.SendAsync("NewsEvents", new { events = earningsNews });
                }

                // Earnings guidance
                if (gameLoop.EarningsEngine.GuidanceThisTick.Count > 0)
                {
                    var guidanceNews = gameLoop.EarningsEngine.GuidanceThisTick.Select(g => new
                    {
                        id = 0, type = "Company",
                        severity = g.Direction == GuidanceDirection.Withdrawn ? "Major" : "Moderate",
                        sentiment = g.Sentiment,
                        headline = g.Headline,
                        affectedSymbols = new[] { g.Symbol },
                        affectedSectors = Array.Empty<string>(),
                        priceEffect = 0f,
                        timestamp = gameLoop.GameTime.ToString("o"),
                        summary = g.Summary,
                        tags = new[] { "earnings", "guidance", g.Direction.ToString().ToLower() },
                        tier = 1,
                    }).ToList();
                    await server.SendAsync("NewsEvents", new { events = guidanceNews });
                }

                // Economic data releases
                if (gameLoop.EconomicEngine.ReleasedThisTick.Count > 0)
                {
                    var econEvents = gameLoop.EconomicEngine.ReleasedThisTick.Select(e =>
                    {
                        var direction = e.Surprise > 0 ? "beats" : "misses";
                        var sentiment = e.Surprise > 0 ? 0.3f : -0.3f;
                        if (e.Indicator == "UnemploymentRate") sentiment = -sentiment;
                        return new
                        {
                            id = 0, type = "Macro", severity = e.Impact == "High" ? "Major" : "Moderate",
                            sentiment,
                            headline = $"{e.Name}: {e.ActualValue:F2} ({direction} est. {e.ExpectedValue:F2})",
                            affectedSymbols = Array.Empty<string>(),
                            affectedSectors = Array.Empty<string>(),
                            priceEffect = (float)(e.Surprise * 0.01m),
                            timestamp = gameLoop.GameTime.ToString("o"),
                        };
                    }).ToList();
                    await server.SendAsync("NewsEvents", new { events = econEvents });
                }

                // Economic cycle phase transition
                if (gameLoop.EconomicCycle.PhaseChangeHeadline != null)
                {
                    await server.SendAsync("NewsEvents", new { events = new[] { new {
                        id = 0, type = "Macro", severity = "Major", sentiment = 0f,
                        headline = gameLoop.EconomicCycle.PhaseChangeHeadline,
                        affectedSymbols = Array.Empty<string>(), affectedSectors = Array.Empty<string>(),
                        priceEffect = 0f, timestamp = gameLoop.GameTime.ToString("o"),
                        tags = new[] { "economic-cycle", "sector-rotation", "macro" }, tier = 1,
                    }}});
                }

                // Monetary policy changes (Fed pivots)
                if (gameLoop.EconomicEngine.PolicyEventsThisTick.Count > 0)
                {
                    var policyNews = gameLoop.EconomicEngine.PolicyEventsThisTick.Select(headline => new
                    {
                        id = 0, type = "Macro", severity = "Major",
                        sentiment = gameLoop.EconomicEngine.Data.PolicyStance switch
                        {
                            MonetaryPolicyStance.QE => 0.6f,
                            MonetaryPolicyStance.Easing => 0.3f,
                            MonetaryPolicyStance.Tightening => -0.4f,
                            _ => 0f,
                        },
                        headline,
                        affectedSymbols = Array.Empty<string>(),
                        affectedSectors = Array.Empty<string>(),
                        priceEffect = 0f,
                        timestamp = gameLoop.GameTime.ToString("o"),
                        summary = gameLoop.EconomicEngine.Data.PolicyStance switch
                        {
                            MonetaryPolicyStance.QE => $"The Federal Reserve has initiated quantitative easing. Balance sheet: ${gameLoop.EconomicEngine.Data.FedBalanceSheet:F1}T. Growth stocks and real estate expected to benefit.",
                            MonetaryPolicyStance.Easing => $"The Fed has shifted to an easing stance with rate cuts expected. Current rate: {gameLoop.EconomicEngine.Data.InterestRate:F2}%.",
                            MonetaryPolicyStance.Tightening => $"The Fed has adopted a hawkish stance to combat inflation at {gameLoop.EconomicEngine.Data.InflationRate:F1}%. Rate hikes expected.",
                            _ => $"The Fed is pausing its monetary policy actions. Current rate: {gameLoop.EconomicEngine.Data.InterestRate:F2}%.",
                        },
                        tier = 1,
                        tags = new[] { "Fed", "Monetary Policy", "Interest Rates" },
                    }).ToList();
                    await server.SendAsync("NewsEvents", new { events = policyNews });
                }

                // M&A / Tender Offer notifications
                if (gameLoop.EventEngine.MAndAEventsThisTick.Count > 0)
                {
                    foreach (var mna in gameLoop.EventEngine.MAndAEventsThisTick)
                    {
                        if (gameLoop.Portfolio.Positions.ContainsKey(mna.TargetSymbol))
                        {
                            var pos = gameLoop.Portfolio.Positions[mna.TargetSymbol];
                            await server.SendAsync("TenderOffer", new
                            {
                                targetSymbol = mna.TargetSymbol,
                                targetName = mna.TargetName,
                                acquirerName = mna.AcquirerName,
                                offerPrice = mna.OfferPrice,
                                premiumPercent = mna.PremiumPercent,
                                currentPrice = gameLoop.StocksBySymbol.TryGetValue(mna.TargetSymbol, out var ts) ? ts.CurrentPrice : 0m,
                                playerShares = Math.Abs(pos.Shares),
                                totalPayout = Math.Abs(pos.Shares) * mna.OfferPrice,
                            });

                            gameLoop.SetSpeed(GameSpeed.Paused);
                            await server.SendAsync("SpeedChanged", new { speed = 0 });
                        }
                    }
                    gameLoop.EventEngine.MAndAEventsThisTick.Clear();
                }

                // Margin call notification
                if (gameLoop.MarginCallThisTick)
                {
                    await server.SendAsync("MarginCall", new
                    {
                        message = "MARGIN CALL: Position force-liquidated to cover margin requirements.",
                        marginBalance = gameLoop.Portfolio.MarginBalance,
                    });
                    await server.SendAsync("NewsEvents", new { events = new[] { new {
                        id = 0, type = "Company", severity = "Major", sentiment = -0.8f,
                        headline = "MARGIN CALL: Forced liquidation triggered due to insufficient equity.",
                        affectedSymbols = Array.Empty<string>(), affectedSectors = Array.Empty<string>(),
                        priceEffect = 0f, timestamp = gameLoop.GameTime.ToString("o"),
                    }}});
                }

                // Meme stock news
                if (gameLoop.MemeStockEngine.NewsThisTick.Count > 0)
                {
                    var memeNews = gameLoop.MemeStockEngine.NewsThisTick.Select(n => new
                    {
                        id = 0,
                        type = "Company",
                        severity = n.Severity,
                        sentiment = n.Phase switch
                        {
                            MemePhase.Discovery => 0.3f,
                            MemePhase.FOMO => 0.5f,
                            MemePhase.Squeeze => 0.7f,
                            MemePhase.DiamondHands => 0.1f,
                            MemePhase.Crash => -0.6f,
                            _ => 0f,
                        },
                        headline = n.Headline,
                        affectedSymbols = new[] { n.Symbol },
                        affectedSectors = Array.Empty<string>(),
                        priceEffect = 0f,
                        timestamp = gameLoop.GameTime.ToString("o"),
                        summary = n.Summary,
                        tags = new[] { "meme-stock", "retail", "short-squeeze", n.Phase.ToString().ToLower() },
                        tier = 1,
                    }).ToList();
                    await server.SendAsync("NewsEvents", new { events = memeNews });
                }

                // Index rebalancing news
                if (gameLoop.ETFEngine.RebalanceNewsThisTick.Count > 0)
                {
                    var rebalanceNews = gameLoop.ETFEngine.RebalanceNewsThisTick.Select(headline => new
                    {
                        id = 0, type = "Macro",
                        severity = headline.StartsWith("QUARTERLY") ? "Major" : "Moderate",
                        sentiment = 0f,
                        headline,
                        affectedSymbols = Array.Empty<string>(),
                        affectedSectors = Array.Empty<string>(),
                        priceEffect = 0f,
                        timestamp = gameLoop.GameTime.ToString("o"),
                        tags = new[] { "index", "rebalancing", "etf", "passive-flow" },
                        tier = 1,
                    }).ToList();
                    await server.SendAsync("NewsEvents", new { events = rebalanceNews });
                }

                // AI Margin Cascade news (market-wide forced liquidation)
                if (gameLoop.AITraderEngine.MarginCascadeNewsThisTick.Count > 0)
                {
                    var cascadeNews = gameLoop.AITraderEngine.MarginCascadeNewsThisTick.Select(headline => new
                    {
                        id = 0, type = "Macro", severity = "Major",
                        sentiment = -0.7f,
                        headline,
                        affectedSymbols = Array.Empty<string>(),
                        affectedSectors = Array.Empty<string>(),
                        priceEffect = 0f,
                        timestamp = gameLoop.GameTime.ToString("o"),
                        summary = $"Hedge fund stress at elevated levels. Forced deleveraging is creating cascading selling pressure across multiple sectors. VIX at {gameLoop.EconomicEngine.MarketVolatilityIndex:F1}.",
                        tags = new[] { "margin-call", "cascade", "deleveraging", "hedge-fund" },
                        tier = 1,
                    }).ToList();
                    await server.SendAsync("NewsEvents", new { events = cascadeNews });
                }

                // Bankruptcy notification
                if (gameLoop.IsBankrupt)
                {
                    await server.SendAsync("Bankruptcy", new
                    {
                        message = "You've gone bankrupt.",
                        restartCash = 10_000,
                    });
                }

                // Scenario completion notification
                if (gameLoop.ScenarioResult != null)
                {
                    var sr = gameLoop.ScenarioResult;
                    await server.SendAsync("ScenarioCompleted", new
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
                    gameLoop.ActiveScenario!.IsActive = false;
                }

                // Achievement unlock notifications
                if (gameLoop.AchievementEngine.NewUnlocksThisTick.Count > 0)
                {
                    foreach (var ach in gameLoop.AchievementEngine.NewUnlocksThisTick)
                    {
                        await server.SendAsync("AchievementUnlocked", new
                        {
                            id = ach.Id,
                            name = ach.Name,
                            description = ach.Description,
                            category = ach.Category.ToString(),
                        });
                    }
                }

                } // end shouldSend block

                // Day Summary at market close (4:00 PM) — sent every tick, not throttled
                if (server.IsClientConnected && gameLoop.GameTime.TimeOfDay == new TimeSpan(16, 0, 0))
                {
                    Func<string, decimal> gprice = sym =>
                        gameLoop.StocksBySymbol.GetValueOrDefault(sym)?.CurrentPrice ?? 0m;

                    if (gameLoop.Stocks.Count == 0) break;
                    var topGainer = gameLoop.Stocks.OrderByDescending(s => s.DayChangePercent).First();
                    var topLoser = gameLoop.Stocks.OrderBy(s => s.DayChangePercent).First();
                    var avgChange = gameLoop.Stocks.Average(s => (double)s.DayChangePercent);

                    var sectorPerf = gameLoop.Stocks
                        .Where(s => !s.Traits.Contains("ETF"))
                        .GroupBy(s => s.Sector)
                        .Select(g => new { sector = g.Key, change = Math.Round((double)g.Average(s => s.DayChangePercent), 2) })
                        .OrderByDescending(s => s.change)
                        .Take(5).ToList();

                    var todayEarnings = gameLoop.EarningsEngine.GetRecent(10)
                        .Where(e => e.ReportDate.Date == gameLoop.GameTime.Date)
                        .Select(e => new { symbol = e.Symbol, beat = e.Beat, surprise = e.EPSSurprisePercent })
                        .ToList();

                    var todayEconEvents = gameLoop.EconomicEngine.EventHistory
                        .Where(e => e.ScheduledDate.Date == gameLoop.GameTime.Date)
                        .Select(e => new { name = e.Name, actual = e.ActualValue, expected = e.ExpectedValue })
                        .ToList();

                    var advancing = gameLoop.Stocks.Count(s => s.DayChangePercent > 0);
                    var declining = gameLoop.Stocks.Count(s => s.DayChangePercent < 0);

                    await server.SendAsync("DaySummary", new
                    {
                        date = gameLoop.GameTime.ToString("yyyy-MM-dd"),
                        marketChange = Math.Round(avgChange, 2),
                        topGainer = new { symbol = topGainer.Symbol, change = topGainer.DayChangePercent },
                        topLoser = new { symbol = topLoser.Symbol, change = topLoser.DayChangePercent },
                        portfolioValue = gameLoop.Portfolio.TotalEquity(gprice),
                        dailyPnL = gameLoop.Portfolio.TotalUnrealizedPnL(gprice) + gameLoop.Portfolio.RealizedPnL,
                        tradesCount = gameLoop.Portfolio.TradeCount,
                        eventsCount = gameLoop.EventEngine.EventHistory.Count,
                        advancing,
                        declining,
                        sectorPerformance = sectorPerf,
                        earningsToday = todayEarnings,
                        economicEventsToday = todayEconEvents,
                    });
                }

                // Autosave every 500 ticks
                if (gameLoop.TickCount > 0 && gameLoop.TickCount % 500 == 0)
                {
                    var autosavePath = SaveManager.GetDefaultSavePath();
                    await SaveManager.SaveGameAsync(gameLoop, autosavePath);
                    Log.Info("Autosaved", new { tick = gameLoop.TickCount, path = autosavePath });
                    await server.SendAsync("Autosaved", new { tick = gameLoop.TickCount });
                }

            var tickMs = (DateTime.UtcNow - tickStart).TotalMilliseconds;
            var targetMs = gameLoop.Speed switch
            {
                GameSpeed.Normal => 1000,
                GameSpeed.Fast => 500,
                GameSpeed.VeryFast => 100,
                GameSpeed.Maximum => 10,
                _ => 1000,
            };

            var sleepMs = Math.Max(0, targetMs - (int)tickMs);
            if (sleepMs > 0) await Task.Delay(sleepMs);
        }
    }
}
