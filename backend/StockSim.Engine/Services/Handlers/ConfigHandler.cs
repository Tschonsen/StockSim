using System.Text.Json;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services.Handlers;

/// <summary>
/// Handles settings messages: UpdateSettings.
/// </summary>
public class ConfigHandler : IMessageHandler
{
    private static readonly Logger Log = new("ConfigHandler");
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private static readonly HashSet<string> MessageTypes = new()
    {
        "UpdateSettings"
    };

    private readonly GameContext _ctx;

    public ConfigHandler(GameContext ctx)
    {
        _ctx = ctx;
    }

    public bool CanHandle(string messageType) => MessageTypes.Contains(messageType);

    public async Task HandleAsync(string messageType, string payload)
    {
        switch (messageType)
        {
            case "UpdateSettings":
                if (_ctx.GameLoop != null)
                {
                    var settingsReq = JsonSerializer.Deserialize<GameSettingsUpdate>(payload, JsonOpts);
                    if (settingsReq != null)
                    {
                        // Commission (Spec 16.3)
                        if (settingsReq.TradingCommission == false)
                            OrderEngine.DefaultCommissionOverride = 0m;
                        else if (settingsReq.CommissionAmount.HasValue)
                            OrderEngine.DefaultCommissionOverride = settingsReq.CommissionAmount.Value;

                        // Taxes (Spec 16.3)
                        if (settingsReq.EnableTaxes.HasValue)
                            _ctx.GameLoop.TaxEngine.Enabled = settingsReq.EnableTaxes.Value;

                        // SMA enforcement (Spec 16.3)
                        if (settingsReq.SmaEnforcement.HasValue)
                            _ctx.GameLoop.SMAEngine.Enabled = settingsReq.SmaEnforcement.Value;

                        // Skip weekends (Spec 16.2)
                        if (settingsReq.SkipWeekends.HasValue)
                            _ctx.GameLoop.SkipWeekends = settingsReq.SkipWeekends.Value;

                        // Auto-pause preferences
                        if (settingsReq.AutoPauseOnShortSqueeze.HasValue)
                            _ctx.GameLoop.AutoPauseOnShortSqueeze = settingsReq.AutoPauseOnShortSqueeze.Value;
                        if (settingsReq.AutoPauseOnSma.HasValue)
                            _ctx.GameLoop.AutoPauseOnSMA = settingsReq.AutoPauseOnSma.Value;
                        if (settingsReq.AutoPauseOnNews.HasValue)
                            _ctx.GameLoop.AutoPauseOnNews = settingsReq.AutoPauseOnNews.Value;
                        if (settingsReq.AutoPauseOnMarginCall.HasValue)
                            _ctx.GameLoop.AutoPauseOnMarginCall = settingsReq.AutoPauseOnMarginCall.Value;
                        if (settingsReq.AutoPauseOnMarketOpen.HasValue)
                            _ctx.GameLoop.AutoPauseOnMarketOpen = settingsReq.AutoPauseOnMarketOpen.Value;
                        if (settingsReq.AutoPauseOnOrderExecution.HasValue)
                            _ctx.GameLoop.AutoPauseOnOrderExecution = settingsReq.AutoPauseOnOrderExecution.Value;
                        if (settingsReq.AutoPauseOnAlert.HasValue)
                            _ctx.GameLoop.AutoPauseOnAlert = settingsReq.AutoPauseOnAlert.Value;

                        Log.Info("Settings updated", new
                        {
                            commission = OrderEngine.DefaultCommissionOverride,
                            taxes = _ctx.GameLoop.TaxEngine.Enabled,
                            sma = _ctx.GameLoop.SMAEngine.Enabled,
                            skipWeekends = _ctx.GameLoop.SkipWeekends,
                        });
                        await _ctx.Server.SendAsync("SettingsApplied", new { success = true });
                    }
                }
                break;
        }
    }

    private record GameSettingsUpdate(
        bool? TradingCommission,
        decimal? CommissionAmount,
        bool? EnableTaxes,
        bool? SmaEnforcement,
        bool? SkipWeekends,
        bool? AutoPauseOnShortSqueeze,
        bool? AutoPauseOnSma,
        bool? AutoPauseOnNews,
        bool? AutoPauseOnMarginCall,
        bool? AutoPauseOnMarketOpen,
        bool? AutoPauseOnOrderExecution,
        bool? AutoPauseOnAlert
    );
}
