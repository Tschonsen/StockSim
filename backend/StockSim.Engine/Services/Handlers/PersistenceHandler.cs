using System.Text.Json;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services.Handlers;

/// <summary>
/// Handles save/load messages: SaveGame, LoadGame, ListSaves.
/// </summary>
public class PersistenceHandler : IMessageHandler
{
    private static readonly Logger Log = new("PersistenceHandler");
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private static readonly HashSet<string> MessageTypes = new()
    {
        "SaveGame", "LoadGame", "ListSaves"
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
                    var savePath = string.IsNullOrEmpty(saveReq?.SlotName)
                        ? SaveManager.GetDefaultSavePath()
                        : SaveManager.GetSlotPath(saveReq.SlotName);
                    try
                    {
                        await SaveManager.SaveGameAsync(_ctx.GameLoop, savePath);
                        await _ctx.Server.SendAsync("GameSaved", new { success = true, path = savePath, slot = saveReq?.SlotName ?? "quicksave" });
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
                var loadPath = string.IsNullOrEmpty(loadReq?.SlotName)
                    ? SaveManager.GetDefaultSavePath()
                    : SaveManager.GetSlotPath(loadReq.SlotName);
                var loaded = await SaveManager.LoadGameAsync(loadPath);
                if (loaded != null)
                {
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
                var saves = SaveManager.ListSaves();
                await _ctx.Server.SendAsync("SaveList", new { saves });
                break;
        }
    }

    private record SaveGameRequest(string? SlotName);
    private record LoadGameRequest(string? SlotName);
}
