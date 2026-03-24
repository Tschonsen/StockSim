using System.Text.Json;
using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// Handles saving and loading game state to/from JSON files.
/// See Bible 15.1 for savegame data structure.
/// Phase 1 MVP: single save slot, JSON format.
/// </summary>
public static class SaveManager
{
    private static readonly Logger Log = new("SaveManager");
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Save the current game state to a JSON file.
    /// </summary>
    public static async Task SaveGameAsync(GameLoop gameLoop, string filePath)
    {
        var saveData = new SaveData
        {
            Meta = new SaveMeta
            {
                SaveDate = DateTime.UtcNow.ToString("o"),
                GameDate = gameLoop.GameTime.ToString("o"),
                Version = "0.1.0",
                Seed = GetSeed(gameLoop),
                TickCount = gameLoop.TickCount,
                MarketPhase = gameLoop.Phase.ToString(),
            },
            GameState = new GameState
            {
                GameTime = gameLoop.GameTime.ToString("o"),
                Speed = (int)gameLoop.Speed,
            },
            Stocks = gameLoop.Stocks.Select(s => new StockSave
            {
                Symbol = s.Symbol,
                Name = s.Name,
                Sector = s.Sector,
                CurrentPrice = s.CurrentPrice,
                PreviousClose = s.PreviousClose,
                BidPrice = s.BidPrice,
                AskPrice = s.AskPrice,
                DayHigh = s.DayHigh,
                DayLow = s.DayLow,
                DayVolume = s.DayVolume,
                AverageVolume = s.AverageVolume,
                SharesOutstanding = s.SharesOutstanding,
                BaseVolatility = s.BaseVolatility,
                LiquidityScore = s.LiquidityScore,
                FairValue = s.FairValue,
                Traits = s.Traits.ToList(),
            }).ToList(),
            Portfolio = new PortfolioSave
            {
                Cash = gameLoop.Portfolio.Cash,
                RealizedPnL = gameLoop.Portfolio.RealizedPnL,
                TotalCommissions = gameLoop.Portfolio.TotalCommissions,
                TradeCount = gameLoop.Portfolio.TradeCount,
                Positions = gameLoop.Portfolio.Positions.Values.Select(p => new PositionSave
                {
                    Symbol = p.Symbol,
                    Shares = p.Shares,
                    AverageCost = p.AverageCost,
                }).ToList(),
                Orders = gameLoop.Portfolio.Orders.Select(o => new OrderSave
                {
                    Id = o.Id,
                    Symbol = o.Symbol,
                    Side = o.Side.ToString(),
                    Type = o.Type.ToString(),
                    Status = o.Status.ToString(),
                    Quantity = o.Quantity,
                    FilledQuantity = o.FilledQuantity,
                    LimitPrice = o.LimitPrice,
                    FillPrice = o.FillPrice,
                    Commission = o.Commission,
                }).ToList(),
            },
        };

        var json = JsonSerializer.Serialize(saveData, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);

        Log.Info("Game saved", new { filePath, stocks = saveData.Stocks.Count, cash = saveData.Portfolio.Cash });
    }

    /// <summary>
    /// Load game state from a JSON file and create a new GameLoop.
    /// </summary>
    public static async Task<GameLoop?> LoadGameAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Log.Warn("Save file not found", new { filePath });
            return null;
        }

        var json = await File.ReadAllTextAsync(filePath);
        var saveData = JsonSerializer.Deserialize<SaveData>(json, JsonOptions);
        if (saveData == null) return null;

        // Recreate the GameLoop from seed (this regenerates stocks)
        // Subtract ETFs from count since they'll be auto-generated
        var seed = saveData.Meta.Seed;
        var etfCount = saveData.Stocks.Count(s => s.Traits.Contains("ETF"));
        var regularStockCount = saveData.Stocks.Count - etfCount;
        var gameLoop = new GameLoop(seed, regularStockCount, saveData.Portfolio.Cash);

        // Restore game time
        if (DateTime.TryParse(saveData.GameState.GameTime, out var gameTime))
        {
            // We need to set GameTime - use reflection or a setter
            SetGameTime(gameLoop, gameTime);
        }

        // Restore stock prices
        foreach (var savedStock in saveData.Stocks)
        {
            var stock = gameLoop.Stocks.FirstOrDefault(s => s.Symbol == savedStock.Symbol);
            if (stock != null)
            {
                stock.CurrentPrice = savedStock.CurrentPrice;
                stock.PreviousClose = savedStock.PreviousClose;
                stock.BidPrice = savedStock.BidPrice;
                stock.AskPrice = savedStock.AskPrice;
                stock.DayHigh = savedStock.DayHigh;
                stock.DayLow = savedStock.DayLow;
                stock.DayVolume = savedStock.DayVolume;
                stock.FairValue = savedStock.FairValue;
            }
        }

        // Restore portfolio
        gameLoop.Portfolio.Cash = saveData.Portfolio.Cash;
        gameLoop.Portfolio.RealizedPnL = saveData.Portfolio.RealizedPnL;
        gameLoop.Portfolio.TotalCommissions = saveData.Portfolio.TotalCommissions;
        gameLoop.Portfolio.TradeCount = saveData.Portfolio.TradeCount;

        foreach (var savedPos in saveData.Portfolio.Positions)
        {
            gameLoop.Portfolio.Positions[savedPos.Symbol] = new Position(
                savedPos.Symbol, savedPos.Shares, savedPos.AverageCost);
        }

        // Restore order ID counter to avoid collisions
        if (saveData.Portfolio.Orders.Count > 0)
        {
            var maxId = saveData.Portfolio.Orders.Max(o => o.Id);
            Order.SetNextId(maxId + 1);
        }

        // Restore speed
        gameLoop.SetSpeed((GameSpeed)saveData.GameState.Speed);

        Log.Info("Game loaded", new { filePath, stocks = saveData.Stocks.Count, cash = gameLoop.Portfolio.Cash });

        return gameLoop;
    }

    /// <summary>Get default save file path.</summary>
    public static string GetDefaultSavePath()
    {
        var dir = GetSaveDirectory();
        return Path.Combine(dir, "quicksave.json");
    }

    /// <summary>Get save file path for a named slot.</summary>
    public static string GetSlotPath(string slotName)
    {
        var dir = GetSaveDirectory();
        var safeName = string.Join("_", slotName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(dir, $"{safeName}.json");
    }

    /// <summary>List all available save files.</summary>
    public static List<SaveSlotInfo> ListSaves()
    {
        var dir = GetSaveDirectory();
        if (!Directory.Exists(dir)) return new();

        return Directory.GetFiles(dir, "*.json")
            .Select(f =>
            {
                try
                {
                    var json = File.ReadAllText(f);
                    var data = System.Text.Json.JsonSerializer.Deserialize<SaveData>(json, JsonOptions);
                    return new SaveSlotInfo
                    {
                        FileName = Path.GetFileNameWithoutExtension(f),
                        FilePath = f,
                        SaveDate = data?.Meta.SaveDate ?? "",
                        GameDate = data?.Meta.GameDate ?? "",
                        Cash = data?.Portfolio.Cash ?? 0,
                    };
                }
                catch { return null; }
            })
            .Where(s => s != null)
            .Cast<SaveSlotInfo>()
            .OrderByDescending(s => s.SaveDate)
            .ToList();
    }

    /// <summary>Delete a save file.</summary>
    public static bool DeleteSave(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            return true;
        }
        return false;
    }

    private static string GetSaveDirectory()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StockSim", "saves");
        Directory.CreateDirectory(dir);
        return dir;
    }

    // Helper: get seed from GameLoop (stored as private field)
    private static int GetSeed(GameLoop gameLoop)
    {
        var field = typeof(GameLoop).GetField("_seed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? (int)field.GetValue(gameLoop)! : 0;
    }

    // Helper: set GameTime (private setter)
    private static void SetGameTime(GameLoop gameLoop, DateTime time)
    {
        var prop = typeof(GameLoop).GetProperty("GameTime");
        prop?.SetValue(gameLoop, time);
    }

    // --- Save Data Structures ---

    private class SaveData
    {
        public SaveMeta Meta { get; set; } = new();
        public GameState GameState { get; set; } = new();
        public List<StockSave> Stocks { get; set; } = new();
        public PortfolioSave Portfolio { get; set; } = new();
    }

    private class SaveMeta
    {
        public string SaveDate { get; set; } = "";
        public string GameDate { get; set; } = "";
        public string Version { get; set; } = "";
        public int Seed { get; set; }
        public long TickCount { get; set; }
        public string MarketPhase { get; set; } = "";
    }

    private class GameState
    {
        public string GameTime { get; set; } = "";
        public int Speed { get; set; }
    }

    private class StockSave
    {
        public string Symbol { get; set; } = "";
        public string Name { get; set; } = "";
        public string Sector { get; set; } = "";
        public decimal CurrentPrice { get; set; }
        public decimal PreviousClose { get; set; }
        public decimal BidPrice { get; set; }
        public decimal AskPrice { get; set; }
        public decimal DayHigh { get; set; }
        public decimal DayLow { get; set; }
        public long DayVolume { get; set; }
        public long AverageVolume { get; set; }
        public long SharesOutstanding { get; set; }
        public decimal BaseVolatility { get; set; }
        public int LiquidityScore { get; set; }
        public decimal FairValue { get; set; }
        public List<string> Traits { get; set; } = new();
    }

    private class PortfolioSave
    {
        public decimal Cash { get; set; }
        public decimal RealizedPnL { get; set; }
        public decimal TotalCommissions { get; set; }
        public int TradeCount { get; set; }
        public List<PositionSave> Positions { get; set; } = new();
        public List<OrderSave> Orders { get; set; } = new();
    }

    private class OrderSave
    {
        public long Id { get; set; }
        public string Symbol { get; set; } = "";
        public string Side { get; set; } = "";
        public string Type { get; set; } = "";
        public string Status { get; set; } = "";
        public decimal Quantity { get; set; }
        public decimal FilledQuantity { get; set; }
        public decimal? LimitPrice { get; set; }
        public decimal? FillPrice { get; set; }
        public decimal Commission { get; set; }
    }

    private class PositionSave
    {
        public string Symbol { get; set; } = "";
        public decimal Shares { get; set; }
        public decimal AverageCost { get; set; }
    }
}

/// <summary>Info about an available save slot.</summary>
public class SaveSlotInfo
{
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string SaveDate { get; set; } = "";
    public string GameDate { get; set; } = "";
    public decimal Cash { get; set; }
}
