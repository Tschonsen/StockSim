using System.Text.Json;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services.Handlers;

/// <summary>
/// Handles save/load messages: SaveGame, LoadGame, ListSaves, DeleteSave.
/// Saves are grouped by game (seed) in subdirectories.
/// </summary>
public class PersistenceHandler : IMessageHandler
{
    private static readonly Logger Log = new("PersistenceHandler");
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private static readonly HashSet<string> MessageTypes = new()
    {
        "SaveGame", "LoadGame", "ListSaves", "DeleteSave"
    };

    private readonly GameContext _ctx;

    public PersistenceHandler(GameContext ctx)
    {
        _ctx = ctx;
    }

    public bool CanHandle(string messageType) => MessageTypes.Contains(messageType);

    public async Task HandleAsync(string messageType, string payload)
    {
        switch (messageType)
        {
            case "SaveGame":
                if (_ctx.GameLoop != null)
                {
                    // Iron Man scenario: no saving allowed
                    if (_ctx.GameLoop.ActiveScenario is { IsActive: true, NoSaveAllowed: true })
                    {
                        await _ctx.Server.SendAsync("GameSaved", new { success = false, error = "Saving is not allowed in this scenario (Iron Man mode)." });
                        break;
                    }

                    var saveReq = JsonSerializer.Deserialize<SaveGameRequest>(payload, JsonOpts);
                    var saveName = string.IsNullOrWhiteSpace(saveReq?.SaveName) ? "Quicksave" : saveReq.SaveName;
                    var seed = GetSeed(_ctx.GameLoop);

                    // Save to game-specific directory
                    var savePath = SaveManager.GetSavePath(seed, saveName);
                    try
                    {
                        await SaveManager.SaveGameAsync(_ctx.GameLoop, savePath, saveName);
                        await _ctx.Server.SendAsync("GameSaved", new
                        {
                            success = true,
                            saveName,
                            gameDate = _ctx.GameLoop.GameTime.ToString("o"),
                        });
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Save failed", new { error = ex.Message });
                        await _ctx.Server.SendAsync("GameSaved", new { success = false, error = $"Save failed: {ex.Message}" });
                    }
                }
                break;

            case "LoadGame":
                var loadReq = JsonSerializer.Deserialize<LoadGameRequest>(payload, JsonOpts);
                string loadPath;

                if (!string.IsNullOrEmpty(loadReq?.FilePath))
                {
                    // Direct file path (from save list UI)
                    loadPath = loadReq.FilePath;
                }
                else if (loadReq?.Seed > 0 && !string.IsNullOrEmpty(loadReq.SaveName))
                {
                    // Load by seed + name
                    loadPath = SaveManager.GetSavePath(loadReq.Seed.Value, loadReq.SaveName);
                }
                else
                {
                    // Legacy: load most recent save across all games
                    var allSaves = SaveManager.ListSaves();
                    loadPath = allSaves.FirstOrDefault()?.FilePath ?? SaveManager.GetDefaultSavePath();
                }

                var loaded = await SaveManager.LoadGameAsync(loadPath);
                if (loaded != null)
                {
                    // Populate NewEventsThisTick with recent history so the news feed isn't empty after load
                    loaded.EventEngine.NewEventsThisTick.Clear();
                    var recentEvents = loaded.EventEngine.EventHistory
                        .OrderByDescending(e => e.TriggeredAt)
                        .Take(10)
                        .Reverse()
                        .ToList();
                    loaded.EventEngine.NewEventsThisTick.AddRange(recentEvents);

                    _ctx.SetGameLoop(loaded);
                    await SendHelper.SendMarketSnapshot(_ctx);
                    await SendHelper.SendPortfolioUpdate(_ctx);
                    await _ctx.Server.SendAsync("GameLoaded", new { success = true });
                }
                else
                {
                    await _ctx.Server.SendAsync("GameLoaded", new { success = false, error = "No save file found" });
                }
                break;

            case "ListSaves":
                var games = SaveManager.ListSavesGrouped();
                await _ctx.Server.SendAsync("SaveList", new
                {
                    games,
                    saves = games.SelectMany(g => g.Saves).OrderByDescending(s => s.SaveDate).ToList(),
                });
                break;

            case "DeleteSave":
                var deleteReq = JsonSerializer.Deserialize<DeleteSaveRequest>(payload, JsonOpts);
                if (!string.IsNullOrEmpty(deleteReq?.FilePath))
                {
                    var deleted = SaveManager.DeleteSave(deleteReq.FilePath);
                    await _ctx.Server.SendAsync("SaveDeleted", new { success = deleted, filePath = deleteReq.FilePath });
                }
                break;
        }
    }

    private static int GetSeed(GameLoop gameLoop)
    {
        var field = typeof(GameLoop).GetField("_seed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? (int)field.GetValue(gameLoop)! : 0;
    }

    private record SaveGameRequest(string? SaveName, string? SlotName);
    private record LoadGameRequest(string? FilePath, string? SaveName, int? Seed, string? SlotName);
    private record DeleteSaveRequest(string? FilePath);
}
