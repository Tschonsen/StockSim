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
                StartNewGame(config);
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
                var ohlcvReq = JsonSerializer.Deserialize<OHLCVRequest>(payload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (_gameLoop != null && ohlcvReq?.Symbol != null)
                {
                    await SendOHLCVData(ohlcvReq.Symbol);
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

            case "GetAnalytics":
                if (_gameLoop != null)
                {
                    Func<string, decimal> getPrice = sym =>
                        _gameLoop.Stocks.FirstOrDefault(s => s.Symbol == sym)?.CurrentPrice ?? 0m;
                    var analytics = AnalyticsCalculator.Calculate(_gameLoop.Portfolio, getPrice, 50_000m);
                    await _server!.SendAsync("AnalyticsData", analytics);
                }
                break;

            case "GetOrders":
                if (_gameLoop != null)
                {
                    await SendOrdersUpdate();
                }
                break;

            case "SaveGame":
                if (_gameLoop != null)
                {
                    var savePath = SaveManager.GetDefaultSavePath();
                    await SaveManager.SaveGameAsync(_gameLoop, savePath);
                    await _server!.SendAsync("GameSaved", new { success = true, path = savePath });
                }
                break;

            case "LoadGame":
                var loadPath = SaveManager.GetDefaultSavePath();
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

            case "shutdown":
                Log.Info("Shutdown requested by frontend");
                _running = false;
                break;

            default:
                Log.Warn("Unknown message type", new { type });
                break;
        }
    }

    private static void StartNewGame(NewGameConfig? config)
    {
        var seed = config?.Seed ?? new Random().Next();
        var stockCount = config?.StockCount ?? 250;
        var startingCash = config?.StartingCash ?? 50_000m;

        Log.Info("Starting new game", new { seed, stockCount, startingCash });
        _gameLoop = new GameLoop(seed, stockCount, startingCash);

        _ = SendMarketSnapshot();
    }

    private static async Task SendMarketSnapshot()
    {
        if (_gameLoop == null || _server == null) return;

        var snapshot = _gameLoop.Stocks.Select(s => new
        {
            symbol = s.Symbol,
            name = s.Name,
            sector = s.Sector,
            price = s.CurrentPrice,
            change = s.DayChange,
            changePercent = s.DayChangePercent,
            bid = s.BidPrice,
            ask = s.AskPrice,
            volume = s.DayVolume,
            marketCap = s.MarketCap,
            traits = s.Traits,
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

            if (_server!.IsClientConnected)
            {
                await SendPriceUpdate();

                // Send portfolio update every 5 ticks if player has positions
                if (_gameLoop.Portfolio.Positions.Count > 0 && _gameLoop.TickCount % 5 == 0)
                {
                    await SendPortfolioUpdate();
                }

                // Send new events to frontend for news ticker
                if (_gameLoop.EventEngine.NewEventsThisTick.Count > 0)
                {
                    await SendNewsEvents();
                }

                // Send dividend announcements as news
                foreach (var div in _gameLoop.DividendEngine.NewAnnouncementsThisTick)
                {
                    await _server.SendAsync("NewsEvents", new
                    {
                        events = new[]
                        {
                            new
                            {
                                id = 0,
                                type = "Company",
                                severity = "Moderate",
                                sentiment = 0.2f,
                                headline = $"{div.Symbol} declares quarterly dividend of ${div.DividendPerShare:F2}/share. Ex-date: {div.ExDividendDate:MMM dd}.",
                                affectedSymbols = new[] { div.Symbol },
                                affectedSectors = Array.Empty<string>(),
                                priceEffect = 0f,
                                timestamp = _gameLoop.GameTime.ToString("o"),
                            }
                        }
                    });
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

                // Autosave every 500 ticks (~8 game-hours at 1 tick/min)
                if (_gameLoop.TickCount > 0 && _gameLoop.TickCount % 500 == 0)
                {
                    var autosavePath = SaveManager.GetDefaultSavePath();
                    await SaveManager.SaveGameAsync(_gameLoop, autosavePath);
                    Log.Info("Autosaved", new { tick = _gameLoop.TickCount, path = autosavePath });
                }
            }

            var tickMs = (DateTime.UtcNow - tickStart).TotalMilliseconds;
            var targetMs = _gameLoop.Speed switch
            {
                GameSpeed.Normal => 1000,
                GameSpeed.Fast => 500,
                GameSpeed.VeryFast => 200,
                GameSpeed.Maximum => 100,
                _ => 1000,
            };

            var sleepMs = Math.Max(0, targetMs - (int)tickMs);
            if (sleepMs > 0) await Task.Delay(sleepMs);
        }
    }

    private static async Task SendPriceUpdate()
    {
        if (_gameLoop == null || _server == null) return;

        var updates = _gameLoop.Stocks.Select(s => new
        {
            s.Symbol,
            price = s.CurrentPrice,
            bid = s.BidPrice,
            ask = s.AskPrice,
            change = s.DayChange,
            changePercent = s.DayChangePercent,
            volume = s.DayVolume,
        }).ToList();

        await _server.SendAsync("MarketUpdate", new
        {
            prices = updates,
            gameTime = _gameLoop.GameTime.ToString("o"),
            tick = _gameLoop.TickCount,
            isMarketOpen = _gameLoop.IsMarketOpen(),
        });
    }

    private static async Task SendOHLCVData(string symbol)
    {
        if (_gameLoop == null || _server == null) return;

        // Combine historical daily candles with live 1-minute candles
        var allCandles = new List<object>();

        // 1. Historical daily candles (252 trading days before game start)
        if (_gameLoop.DailyHistory.TryGetValue(symbol, out var dailyCandles))
        {
            foreach (var c in dailyCandles)
            {
                allCandles.Add(new
                {
                    time = c.Time,
                    open = c.Open,
                    high = c.High,
                    low = c.Low,
                    close = c.Close,
                    volume = c.Volume,
                });
            }
        }

        // 2. Live 1-minute candles (from current session)
        if (_gameLoop.PriceHistories.TryGetValue(symbol, out var history))
        {
            foreach (var c in history.Candles)
            {
                allCandles.Add(new
                {
                    time = c.Time,
                    open = c.Open,
                    high = c.High,
                    low = c.Low,
                    close = c.Close,
                    volume = c.Volume,
                });
            }
        }

        await _server.SendAsync("OHLCVUpdate", new
        {
            symbol,
            candles = allCandles,
        });

        Log.Info("OHLCV data sent", new
        {
            symbol,
            dailyCandles = dailyCandles?.Count ?? 0,
            liveCandles = history?.Candles.Count ?? 0,
        });
    }

    private static async Task HandlePlaceOrder(PlaceOrderRequest req)
    {
        if (_gameLoop == null || _server == null) return;

        var stock = _gameLoop.Stocks.FirstOrDefault(s => s.Symbol == req.Symbol);
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
                fillPrice = result.Order.FillPrice,
                commission = result.Order.Commission,
                limitPrice = result.Order.LimitPrice,
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
            _gameLoop.Stocks.FirstOrDefault(s => s.Symbol == symbol)?.CurrentPrice ?? 0m;

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
            }
        }

        await _server.SendAsync("IndicatorData", new { symbol, indicators = result });
    }

    private record NewGameConfig(int? Seed, int? StockCount, decimal? StartingCash);
    private record SpeedConfig(int Speed);
    private record OHLCVRequest(string Symbol);
    private record PlaceOrderRequest(string Symbol, string Side, string Type, decimal Quantity, decimal? LimitPrice, string? TimeInForce, decimal? StopPrice, decimal? TrailAmount);
    private record CancelOrderRequest(long OrderId);
    private record IndicatorRequest(string Symbol, string[]? Indicators);
}
