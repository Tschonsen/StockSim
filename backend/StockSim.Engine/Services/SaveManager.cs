using System.Text.Json;
using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// Handles saving and loading game state to/from JSON files.
/// See Spec 15.1 for savegame data structure.
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
    public static async Task SaveGameAsync(GameLoop gameLoop, string filePath, string saveName = "Quicksave")
    {
        var saveData = new SaveData
        {
            Meta = new SaveMeta
            {
                SaveDate = DateTime.UtcNow.ToString("o"),
                GameDate = gameLoop.GameTime.ToString("o"),
                Version = "0.2.1",
                Seed = GetSeed(gameLoop),
                TickCount = gameLoop.TickCount,
                MarketPhase = gameLoop.Phase.ToString(),
                SaveName = saveName,
                PlayerName = gameLoop.PlayerName,
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
                Revenue = s.Revenue,
                NetIncome = s.NetIncome,
                DividendYield = s.DividendYield,
                DebtToEquity = s.DebtToEquity,
                RevenueGrowth = s.RevenueGrowth,
                ShortInterest = s.ShortInterest,
                AnalystRating = s.AnalystRating,
                TargetPrice = s.TargetPrice,
                Personality = s.Personality,
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
                PriceAlerts = gameLoop.Portfolio.PriceAlerts.Select(a => new AlertSave
                {
                    Symbol = a.Symbol, Condition = a.Condition, TargetPrice = a.TargetPrice,
                    Active = a.Active, Triggered = a.Triggered,
                }).ToList(),
            },
            OptionPositions = gameLoop.OptionsEngine.Positions.Select(p => new OptionPositionSave
            {
                ContractId = p.ContractId,
                UnderlyingSymbol = p.UnderlyingSymbol,
                Type = p.Type.ToString(),
                StrikePrice = p.StrikePrice,
                ExpirationDate = p.ExpirationDate.ToString("o"),
                Quantity = p.Quantity,
                AvgCost = p.AvgCost,
            }).ToList(),
            EconomicState = new EconomicSave
            {
                InterestRate = gameLoop.EconomicEngine.Data.InterestRate,
                InflationRate = gameLoop.EconomicEngine.Data.InflationRate,
                UnemploymentRate = gameLoop.EconomicEngine.Data.UnemploymentRate,
                GDPGrowth = gameLoop.EconomicEngine.Data.GDPGrowth,
                ConsumerConfidence = gameLoop.EconomicEngine.Data.ConsumerConfidence,
                TreasuryYield10Y = gameLoop.EconomicEngine.Data.TreasuryYield10Y,
                OilPrice = gameLoop.EconomicEngine.Data.OilPrice,
                GoldPrice = gameLoop.EconomicEngine.Data.GoldPrice,
                ManufacturingPMI = gameLoop.EconomicEngine.Data.ManufacturingPMI,
                DollarIndex = gameLoop.EconomicEngine.Data.DollarIndex,
                FedBalanceSheet = gameLoop.EconomicEngine.Data.FedBalanceSheet,
                PolicyStance = gameLoop.EconomicEngine.Data.PolicyStance.ToString(),
                MarketVolatilityIndex = gameLoop.EconomicEngine.MarketVolatilityIndex,
            },
            SMAState = gameLoop.SMAEngine.State,
            RumorState = new RumorSave
            {
                Rumors = gameLoop.RumorEngine.RumorHistory.ToList(),
                LastRumorDay = gameLoop.RumorEngine.LastRumorDay,
                NextRumorInDays = gameLoop.RumorEngine.NextRumorInDays,
            },
            Reputation = new ReputationSave
            {
                MarketInfluence = gameLoop.Reputation.MarketInfluence,
                SECScrutiny = gameLoop.Reputation.SECScrutiny,
            },
            Achievements = gameLoop.AchievementEngine.Achievements
                .Where(a => a.Unlocked)
                .Select(a => new AchievementSave { Id = a.Id, UnlockedAt = a.UnlockedAt })
                .ToList(),
            TaxState = new TaxSave
            {
                ShortTermGains = gameLoop.TaxEngine.ShortTermGains,
                ShortTermLosses = gameLoop.TaxEngine.ShortTermLosses,
                LongTermGains = gameLoop.TaxEngine.LongTermGains,
                LongTermLosses = gameLoop.TaxEngine.LongTermLosses,
                TotalTaxPaid = gameLoop.TaxEngine.TotalTaxPaid,
                DividendTaxPaid = gameLoop.TaxEngine.DividendTaxPaid,
                WashSaleDisallowed = gameLoop.TaxEngine.WashSaleDisallowed,
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

        MigrateSaveData(saveData);

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
                stock.Revenue = savedStock.Revenue > 0 ? savedStock.Revenue : stock.Revenue;
                stock.NetIncome = savedStock.NetIncome != 0 ? savedStock.NetIncome : stock.NetIncome;
                stock.DividendYield = savedStock.DividendYield;
                stock.DebtToEquity = savedStock.DebtToEquity > 0 ? savedStock.DebtToEquity : stock.DebtToEquity;
                stock.RevenueGrowth = savedStock.RevenueGrowth;
                stock.ShortInterest = savedStock.ShortInterest;
                stock.AnalystRating = savedStock.AnalystRating > 0 ? savedStock.AnalystRating : stock.AnalystRating;
                stock.TargetPrice = savedStock.TargetPrice > 0 ? savedStock.TargetPrice : stock.TargetPrice;
                // Restore evolved personality (null on legacy saves → keep the regenerated one).
                if (savedStock.Personality != null) stock.Personality = savedStock.Personality;
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

        // Restore price alerts (player config)
        foreach (var sa in saveData.Portfolio.PriceAlerts)
            gameLoop.Portfolio.PriceAlerts.Add(new PriceAlert(sa.Symbol, sa.Condition, sa.TargetPrice)
            {
                Active = sa.Active,
                Triggered = sa.Triggered,
            });

        // Restore unlocked achievements (progression)
        foreach (var sa in saveData.Achievements)
        {
            var a = gameLoop.AchievementEngine.Achievements.FirstOrDefault(x => x.Id == sa.Id);
            if (a != null) { a.Unlocked = true; a.UnlockedAt = sa.UnlockedAt; }
        }

        // Restore tax summary state (year-to-date gains/losses, taxes paid)
        if (saveData.TaxState != null)
        {
            var tax = gameLoop.TaxEngine;
            tax.ShortTermGains = saveData.TaxState.ShortTermGains;
            tax.ShortTermLosses = saveData.TaxState.ShortTermLosses;
            tax.LongTermGains = saveData.TaxState.LongTermGains;
            tax.LongTermLosses = saveData.TaxState.LongTermLosses;
            tax.TotalTaxPaid = saveData.TaxState.TotalTaxPaid;
            tax.DividendTaxPaid = saveData.TaxState.DividendTaxPaid;
            tax.WashSaleDisallowed = saveData.TaxState.WashSaleDisallowed;
        }

        // Restore SMA state
        if (saveData.SMAState != null)
        {
            gameLoop.SMAEngine.State = saveData.SMAState;
        }

        // Restore rumor state
        if (saveData.RumorState != null)
        {
            gameLoop.RumorEngine.LoadState(
                saveData.RumorState.Rumors,
                saveData.RumorState.LastRumorDay,
                saveData.RumorState.NextRumorInDays);
        }

        // Restore option positions
        if (saveData.OptionPositions != null)
        {
            foreach (var op in saveData.OptionPositions)
            {
                gameLoop.OptionsEngine.Positions.Add(new OptionPosition
                {
                    ContractId = op.ContractId,
                    UnderlyingSymbol = op.UnderlyingSymbol,
                    Type = Enum.TryParse<OptionType>(op.Type, out var t) ? t : OptionType.Call,
                    StrikePrice = op.StrikePrice,
                    ExpirationDate = DateTime.TryParse(op.ExpirationDate, out var d) ? d : DateTime.MaxValue,
                    Quantity = op.Quantity,
                    AvgCost = op.AvgCost,
                });
            }
            Log.Info("Option positions restored", new { count = saveData.OptionPositions.Count });
        }

        // Restore reputation
        if (saveData.Reputation != null)
        {
            gameLoop.Reputation.MarketInfluence = saveData.Reputation.MarketInfluence;
            gameLoop.Reputation.SECScrutiny = saveData.Reputation.SECScrutiny;
        }

        // Restore economic state
        if (saveData.EconomicState != null)
        {
            var econ = gameLoop.EconomicEngine.Data;
            econ.InterestRate = saveData.EconomicState.InterestRate;
            econ.InflationRate = saveData.EconomicState.InflationRate;
            econ.UnemploymentRate = saveData.EconomicState.UnemploymentRate;
            econ.GDPGrowth = saveData.EconomicState.GDPGrowth;
            econ.ConsumerConfidence = saveData.EconomicState.ConsumerConfidence;
            econ.TreasuryYield10Y = saveData.EconomicState.TreasuryYield10Y;
            econ.OilPrice = saveData.EconomicState.OilPrice;
            econ.GoldPrice = saveData.EconomicState.GoldPrice;
            econ.ManufacturingPMI = saveData.EconomicState.ManufacturingPMI;
            econ.DollarIndex = saveData.EconomicState.DollarIndex;
            econ.FedBalanceSheet = saveData.EconomicState.FedBalanceSheet;
            if (Enum.TryParse<MonetaryPolicyStance>(saveData.EconomicState.PolicyStance, out var stance))
                econ.PolicyStance = stance;
            gameLoop.EconomicEngine.MarketVolatilityIndex = saveData.EconomicState.MarketVolatilityIndex;
            Log.Info("Economic state restored", new { rate = econ.InterestRate, policy = econ.PolicyStance, dxy = econ.DollarIndex });
        }

        // Restore speed
        gameLoop.SetSpeed((GameSpeed)saveData.GameState.Speed);

        Log.Info("Game loaded", new { filePath, stocks = saveData.Stocks.Count, cash = gameLoop.Portfolio.Cash });

        return gameLoop;
    }

    /// <summary>Get save file path for a game seed + save name.</summary>
    public static string GetSavePath(int seed, string saveName)
    {
        var gameDir = GetGameDirectory(seed);
        var safeName = string.Join("_", saveName.Split(Path.GetInvalidFileNameChars()));
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "quicksave";
        return Path.Combine(gameDir, $"{safeName}.json");
    }

    /// <summary>Get default save file path (quicksave for current game).</summary>
    public static string GetDefaultSavePath()
    {
        var dir = GetSaveDirectory();
        return Path.Combine(dir, "quicksave.json");
    }

    /// <summary>Get save file path for a named slot (legacy compatibility).</summary>
    public static string GetSlotPath(string slotName)
    {
        var dir = GetSaveDirectory();
        var safeName = string.Join("_", slotName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(dir, $"{safeName}.json");
    }

    /// <summary>
    /// List all available saves, grouped by game (seed).
    /// Each game folder contains multiple named saves.
    /// Also scans root saves/ for legacy ungrouped saves.
    /// </summary>
    public static List<SaveGameInfo> ListSavesGrouped()
    {
        var result = new List<SaveGameInfo>();
        var rootDir = GetSaveDirectory();
        if (!Directory.Exists(rootDir)) return result;

        // Scan game subdirectories (new format: saves/game_{seed}/)
        foreach (var gameDir in Directory.GetDirectories(rootDir, "game_*"))
        {
            var saves = ScanDirectory(gameDir);
            if (saves.Count == 0) continue;

            var newest = saves.OrderByDescending(s => s.SaveDate).First();
            result.Add(new SaveGameInfo
            {
                GameId = Path.GetFileName(gameDir),
                Seed = int.TryParse(Path.GetFileName(gameDir).Replace("game_", ""), out var s) ? s : 0,
                PlayerName = newest.PlayerName,
                LastPlayed = newest.SaveDate,
                TotalSaves = saves.Count,
                Saves = saves.OrderByDescending(s => s.SaveDate).ToList(),
            });
        }

        // Scan root for legacy saves (ungrouped)
        var rootSaves = ScanDirectory(rootDir);
        if (rootSaves.Count > 0)
        {
            var newest = rootSaves.OrderByDescending(s => s.SaveDate).First();
            result.Add(new SaveGameInfo
            {
                GameId = "legacy",
                Seed = newest.Seed,
                PlayerName = newest.PlayerName,
                LastPlayed = newest.SaveDate,
                TotalSaves = rootSaves.Count,
                Saves = rootSaves.OrderByDescending(s => s.SaveDate).ToList(),
            });
        }

        return result.OrderByDescending(g => g.LastPlayed).ToList();
    }

    /// <summary>List all saves flat (for backward compatibility).</summary>
    public static List<SaveSlotInfo> ListSaves()
    {
        return ListSavesGrouped().SelectMany(g => g.Saves).OrderByDescending(s => s.SaveDate).ToList();
    }

    private static List<SaveSlotInfo> ScanDirectory(string dir)
    {
        return Directory.GetFiles(dir, "*.json")
            .Select(f =>
            {
                try
                {
                    var json = File.ReadAllText(f);
                    var data = JsonSerializer.Deserialize<SaveData>(json, JsonOptions);
                    if (data?.Meta == null) return null;
                    return new SaveSlotInfo
                    {
                        FileName = Path.GetFileNameWithoutExtension(f),
                        FilePath = f,
                        SaveName = data.Meta.SaveName ?? Path.GetFileNameWithoutExtension(f),
                        SaveDate = data.Meta.SaveDate ?? "",
                        GameDate = data.Meta.GameDate ?? "",
                        Cash = data.Portfolio?.Cash ?? 0,
                        PlayerName = data.Meta.PlayerName ?? "Trader",
                        Seed = data.Meta.Seed,
                        MarketPhase = data.Meta.MarketPhase ?? "",
                        TickCount = data.Meta.TickCount,
                    };
                }
                catch (Exception ex)
                {
                    Log.Error("Failed to read save", new { file = f, error = ex.Message });
                    return null;
                }
            })
            .Where(s => s != null)
            .Cast<SaveSlotInfo>()
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

    private static string GetGameDirectory(int seed)
    {
        var dir = Path.Combine(GetSaveDirectory(), $"game_{seed}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>
    /// Migrate save data from older versions to the current format.
    /// </summary>
    private static void MigrateSaveData(SaveData data)
    {
        var version = data.Meta?.Version ?? "0.1.0";

        // v0.1.0 → v0.2.0: Added Reputation, WashSale fields
        if (version == "0.1.0" || string.IsNullOrEmpty(version))
        {
            data.Reputation ??= new ReputationSave();
            data.Meta!.Version = "0.2.0";
            Log.Info("Migrated save from v0.1.0 to v0.2.0");
        }

        // v0.2.0 → v0.2.1: Added EconomicState, stock fundamentals
        if (version == "0.2.0")
        {
            data.EconomicState ??= new EconomicSave();
            data.Meta!.Version = "0.2.1";
            Log.Info("Migrated save from v0.2.0 to v0.2.1");
        }
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
        public List<OptionPositionSave>? OptionPositions { get; set; }
        public SMAState? SMAState { get; set; }
        public RumorSave? RumorState { get; set; }
        public ReputationSave? Reputation { get; set; }
        public EconomicSave? EconomicState { get; set; }
        public List<AchievementSave> Achievements { get; set; } = new();
        public TaxSave? TaxState { get; set; }
    }

    private class TaxSave
    {
        public decimal ShortTermGains { get; set; }
        public decimal ShortTermLosses { get; set; }
        public decimal LongTermGains { get; set; }
        public decimal LongTermLosses { get; set; }
        public decimal TotalTaxPaid { get; set; }
        public decimal DividendTaxPaid { get; set; }
        public decimal WashSaleDisallowed { get; set; }
    }

    private class AchievementSave
    {
        public string Id { get; set; } = "";
        public DateTime? UnlockedAt { get; set; }
    }

    private class AlertSave
    {
        public string Symbol { get; set; } = "";
        public string Condition { get; set; } = "";
        public decimal TargetPrice { get; set; }
        public bool Active { get; set; }
        public bool Triggered { get; set; }
    }

    private class EconomicSave
    {
        public decimal InterestRate { get; set; }
        public decimal InflationRate { get; set; }
        public decimal UnemploymentRate { get; set; }
        public decimal GDPGrowth { get; set; }
        public decimal ConsumerConfidence { get; set; }
        public decimal TreasuryYield10Y { get; set; }
        public decimal OilPrice { get; set; }
        public decimal GoldPrice { get; set; }
        public decimal ManufacturingPMI { get; set; }
        public decimal DollarIndex { get; set; }
        public decimal FedBalanceSheet { get; set; }
        public string PolicyStance { get; set; } = "Neutral";
        public double MarketVolatilityIndex { get; set; }
    }

    private class OptionPositionSave
    {
        public long ContractId { get; set; }
        public string UnderlyingSymbol { get; set; } = "";
        public string Type { get; set; } = "Call";
        public decimal StrikePrice { get; set; }
        public string ExpirationDate { get; set; } = "";
        public int Quantity { get; set; }
        public decimal AvgCost { get; set; }
    }

    private class RumorSave
    {
        public List<Rumor> Rumors { get; set; } = new();
        public int LastRumorDay { get; set; }
        public int NextRumorInDays { get; set; }
    }

    private class ReputationSave
    {
        public decimal MarketInfluence { get; set; }
        public decimal SECScrutiny { get; set; }
    }

    private class SaveMeta
    {
        public string SaveDate { get; set; } = "";
        public string GameDate { get; set; } = "";
        public string Version { get; set; } = "";
        public int Seed { get; set; }
        public long TickCount { get; set; }
        public string MarketPhase { get; set; } = "";
        public string SaveName { get; set; } = "";
        public string PlayerName { get; set; } = "";
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
        // Fundamentals (added v0.2.1)
        public decimal Revenue { get; set; }
        public decimal NetIncome { get; set; }
        public decimal DividendYield { get; set; }
        public decimal DebtToEquity { get; set; }
        public decimal RevenueGrowth { get; set; }
        public decimal ShortInterest { get; set; }
        public decimal AnalystRating { get; set; }
        public decimal TargetPrice { get; set; }
        // Persist the full personality: its CEO archetype, credit rating, performance streak and
        // consecutive-miss count evolve during play and would otherwise reset on load.
        public CompanyPersonality? Personality { get; set; }
    }

    private class PortfolioSave
    {
        public decimal Cash { get; set; }
        public decimal RealizedPnL { get; set; }
        public decimal TotalCommissions { get; set; }
        public int TradeCount { get; set; }
        public List<PositionSave> Positions { get; set; } = new();
        public List<OrderSave> Orders { get; set; } = new();
        public List<AlertSave> PriceAlerts { get; set; } = new();
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
    public string SaveName { get; set; } = "";
    public string SaveDate { get; set; } = "";
    public string GameDate { get; set; } = "";
    public decimal Cash { get; set; }
    public string PlayerName { get; set; } = "";
    public int Seed { get; set; }
    public string MarketPhase { get; set; } = "";
    public long TickCount { get; set; }
}

/// <summary>A game session with multiple saves.</summary>
public class SaveGameInfo
{
    public string GameId { get; set; } = "";
    public int Seed { get; set; }
    public string PlayerName { get; set; } = "";
    public string LastPlayed { get; set; } = "";
    public int TotalSaves { get; set; }
    public List<SaveSlotInfo> Saves { get; set; } = new();
}
