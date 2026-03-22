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

        Log.Info("Starting new game", new { seed, stockCount });
        _gameLoop = new GameLoop(seed, stockCount);

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

    private record NewGameConfig(int? Seed, int? StockCount);
    private record SpeedConfig(int Speed);
}
