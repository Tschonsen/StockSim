using System.Text.Json;
using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services.Handlers;

/// <summary>
/// Handles game lifecycle messages: hello, NewGame, SetSpeed, StartScenario, SkipToOpen, Retire, RestartBankrupt, shutdown.
/// </summary>
public class GameControlHandler : IMessageHandler
{
    private static readonly Logger Log = new("GameControlHandler");
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private static readonly HashSet<string> MessageTypes = new()
    {
        "hello", "NewGame", "SetSpeed", "StartScenario", "SkipToOpen", "Retire", "RestartBankrupt", "shutdown"
    };

    private readonly GameContext _ctx;
    private readonly Action _requestShutdown;

    public GameControlHandler(GameContext ctx, Action requestShutdown)
    {
        _ctx = ctx;
        _requestShutdown = requestShutdown;
    }

    public bool CanHandle(string messageType) => MessageTypes.Contains(messageType);

    public async Task HandleAsync(string messageType, string payload)
    {
        switch (messageType)
        {
            case "hello":
                await _ctx.Server.SendAsync("welcome", new
                {
                    version = "0.2.0",
                    status = _ctx.GameLoop != null ? "game_active" : "no_game"
                });
                break;

            case "NewGame":
                var config = JsonSerializer.Deserialize<NewGameConfig>(payload, JsonOpts);
                await StartNewGameAsync(config);
                break;

            case "SetSpeed":
                var speedData = JsonSerializer.Deserialize<SpeedConfig>(payload, JsonOpts);
                if (_ctx.GameLoop != null && speedData != null)
                {
                    _ctx.GameLoop.SetSpeed((GameSpeed)speedData.Speed);
                    await _ctx.Server.SendAsync("SpeedChanged", new { speed = (int)_ctx.GameLoop.Speed });
                }
                break;

            case "StartScenario":
                var scenarioReq = JsonSerializer.Deserialize<StartScenarioRequest>(payload, JsonOpts);
                if (scenarioReq != null)
                {
                    var scenario = Scenario.GetAll().Find(s => s.Id == scenarioReq.ScenarioId);
                    if (scenario != null)
                    {
                        scenario.IsActive = true;
                        var scenSeed = new Random().Next();
                        var gameLoop = new GameLoop(scenSeed, 250, scenario.StartingCash);
                        gameLoop.ActiveScenario = scenario;
                        _ctx.SetGameLoop(gameLoop);
                        await SendHelper.SendMarketSnapshot(_ctx);
                        await _ctx.Server.SendAsync("ScenarioStarted", new
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

            case "SkipToOpen":
                if (_ctx.GameLoop != null && !_ctx.GameLoop.IsMarketOpen())
                {
                    var skipTime = _ctx.GameLoop.GameTime;
                    int safetyLimit = 5000;
                    while (!_ctx.GameLoop.IsMarketOpen() && safetyLimit-- > 0)
                    {
                        skipTime = skipTime.AddMinutes(1);
                        if (skipTime.DayOfWeek == DayOfWeek.Saturday)
                            skipTime = skipTime.AddDays(2).Date.AddHours(9).AddMinutes(30);
                        else if (skipTime.DayOfWeek == DayOfWeek.Sunday)
                            skipTime = skipTime.AddDays(1).Date.AddHours(9).AddMinutes(30);
                        else if (skipTime.TimeOfDay < new TimeSpan(9, 30, 0))
                            skipTime = skipTime.Date.AddHours(9).AddMinutes(30);
                    }
                    _ctx.GameLoop.GameTime = skipTime;
                    await _ctx.Server.SendAsync("SpeedChanged", new { speed = (int)_ctx.GameLoop.Speed });
                    // Note: SendPriceUpdate needs lastSentPrices cache from Program.cs tick loop context
                    Log.Info("Skipped to market open", new { newTime = skipTime.ToString("o") });
                }
                break;

            case "Retire":
                if (_ctx.GameLoop != null)
                {
                    Func<string, decimal> retirePrice = sym =>
                        _ctx.GameLoop.StocksBySymbol.GetValueOrDefault(sym)?.CurrentPrice ?? 0m;
                    var retireEquity = _ctx.GameLoop.Portfolio.TotalEquity(retirePrice);
                    var stats = _ctx.GameLoop.AchievementEngine.Stats;
                    var totalTrades2 = stats.WinningTradeCount + stats.LosingTradeCount;
                    var winRate2 = totalTrades2 > 0 ? Math.Round((decimal)stats.WinningTradeCount / totalTrades2 * 100, 1) : 0m;
                    var bestTrade = stats.TradeHistory.Count > 0 ? stats.TradeHistory.MaxBy(t => t.PnL) : null;
                    var worstTrade = stats.TradeHistory.Count > 0 ? stats.TradeHistory.MinBy(t => t.PnL) : null;
                    var achievementsUnlocked = _ctx.GameLoop.AchievementEngine.Achievements.Count(a => a.Unlocked);

                    await _ctx.Server.SendAsync("CareerSummary", new
                    {
                        finalEquity = retireEquity,
                        startingCash = _ctx.GameLoop.StartingCash,
                        totalReturn = retireEquity - _ctx.GameLoop.StartingCash,
                        totalReturnPercent = _ctx.GameLoop.StartingCash > 0
                            ? Math.Round((retireEquity - _ctx.GameLoop.StartingCash) / _ctx.GameLoop.StartingCash * 100, 2) : 0,
                        daysPlayed = stats.DaysPlayed,
                        totalTrades = totalTrades2,
                        winRate = winRate2,
                        bestTradePnL = bestTrade?.PnL ?? 0,
                        bestTradeSymbol = bestTrade?.Symbol ?? "",
                        worstTradePnL = worstTrade?.PnL ?? 0,
                        worstTradeSymbol = worstTrade?.Symbol ?? "",
                        totalCommissions = _ctx.GameLoop.Portfolio.TotalCommissions,
                        totalTaxPaid = _ctx.GameLoop.TaxEngine.TotalTaxPaid,
                        totalDividends = _ctx.GameLoop.TaxEngine.DividendTaxPaid / Math.Max(0.01m, _ctx.GameLoop.TaxEngine.LongTermRate),
                        maxDrawdown = stats.MaxDrawdownPercent,
                        achievementsUnlocked,
                        achievementsTotal = _ctx.GameLoop.AchievementEngine.Achievements.Count,
                        largestSingleGain = stats.LargestSingleGain,
                        largestSingleLoss = stats.LargestSingleLoss,
                    });
                    _ctx.GameLoop.SetSpeed(GameSpeed.Paused);
                }
                break;

            case "RestartBankrupt":
                if (_ctx.GameLoop != null && _ctx.GameLoop.IsBankrupt)
                {
                    _ctx.GameLoop.Portfolio.Cash = 10_000m;
                    _ctx.GameLoop.Portfolio.Positions.Clear();
                    _ctx.GameLoop.Portfolio.RealizedPnL = 0;
                    _ctx.GameLoop.Portfolio.TotalCommissions = 0;
                    _ctx.GameLoop.Portfolio.TradeCount = 0;
                    _ctx.GameLoop.IsBankrupt = false;
                    await SendHelper.SendPortfolioUpdate(_ctx);
                    await _ctx.Server.SendAsync("BankruptRestarted", new { cash = 10_000 });
                }
                break;

            case "shutdown":
                Log.Info("Shutdown requested by frontend");
                _requestShutdown();
                break;
        }
    }

    private async Task StartNewGameAsync(NewGameConfig? config)
    {
        var seed = config?.Seed ?? new Random().Next();
        var stockCount = config?.StockCount ?? 250;
        var startingCash = config?.StartingCash ?? 50_000m;

        var playerName = config?.PlayerName ?? "Trader";
        Log.Info("Starting new game", new { seed, stockCount, startingCash, playerName });
        var gameLoop = new GameLoop(seed, stockCount, startingCash);
        gameLoop.PlayerName = playerName;

        if (config != null)
        {
            if (config.Commission.HasValue)
                OrderEngine.DefaultCommissionOverride = config.Commission.Value;

            if (config.Volatility.HasValue && config.Volatility.Value != 1.0m)
            {
                foreach (var stock in gameLoop.MutableStocks)
                    stock.BaseVolatility *= config.Volatility.Value;
            }

            if (config.EventFrequency.HasValue)
                gameLoop.EventEngine.FrequencyMultiplier = (double)config.EventFrequency.Value;

            if (config.AiAggression.HasValue)
                gameLoop.AITraderEngine.AggressionMultiplier = (float)config.AiAggression.Value;

            gameLoop.TaxEngine.Enabled = true;

            if (config.EnableMargin == true)
                gameLoop.Portfolio.MarginEnabled = true;
        }

        _ctx.SetGameLoop(gameLoop);
        await SendHelper.SendMarketSnapshot(_ctx); // Initial news are bundled in the snapshot
    }

    private record NewGameConfig(
        int? Seed, int? StockCount, decimal? StartingCash,
        decimal? Commission, decimal? Volatility, decimal? EventFrequency,
        decimal? AiAggression, bool? EnableMargin, bool? EnableBankruptcy,
        bool? EnableDividends, bool? EnableShortSelling, bool? EnableEvents,
        string? MarketHours, string? PlayerName, bool? ShowTutorial);
    private record SpeedConfig(int Speed);
    private record StartScenarioRequest(string ScenarioId);
}
