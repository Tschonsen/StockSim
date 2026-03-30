using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// Calculates and tracks capital gains taxes.
/// Short-term (<252 trading days / ~1 year): 35% tax rate.
/// Long-term (>=252 trading days): 15% tax rate.
/// Tax Loss Harvesting: losses offset gains.
/// </summary>
public class TaxEngine
{
    private readonly Logger _log = new("TaxEngine");

    public decimal ShortTermRate { get; set; } = 0.35m;  // 35%
    public decimal LongTermRate { get; set; } = 0.15m;   // 15%
    public bool Enabled { get; set; } = true;

    // Running totals for the current tax year
    public decimal ShortTermGains { get; set; }
    public decimal ShortTermLosses { get; set; }
    public decimal LongTermGains { get; set; }
    public decimal LongTermLosses { get; set; }
    public decimal TotalTaxPaid { get; set; }
    public decimal DividendTaxPaid { get; set; }
    public decimal WashSaleDisallowed { get; set; }

    /// <summary>
    /// Wash Sale Rule (IRS Rule): If you sell a security at a loss and repurchase
    /// the same security within 30 calendar days, the loss is disallowed for tax purposes.
    /// The disallowed loss is added to the cost basis of the new position.
    /// </summary>
    private readonly Dictionary<string, (DateTime sellDate, decimal loss)> _washSaleTracker = new();

    /// <summary>Track a loss sale for wash sale detection.</summary>
    public void RecordLossSale(string symbol, DateTime date, decimal loss)
    {
        if (loss >= 0) return;
        _washSaleTracker[symbol] = (date, Math.Abs(loss));
    }

    /// <summary>
    /// Check if buying this symbol triggers a wash sale.
    /// Returns the disallowed loss amount (to add to cost basis), or 0.
    /// </summary>
    public decimal CheckWashSale(string symbol, DateTime buyDate)
    {
        if (!Enabled) return 0;
        if (!_washSaleTracker.TryGetValue(symbol, out var entry)) return 0;
        var daysSinceSell = (buyDate - entry.sellDate).TotalDays;
        if (daysSinceSell > 30) { _washSaleTracker.Remove(symbol); return 0; }

        // Wash sale triggered: disallow the loss
        var disallowed = entry.loss;
        WashSaleDisallowed += disallowed;
        _washSaleTracker.Remove(symbol);
        _log.Info("Wash sale triggered", new { symbol, disallowed, daysSinceSell = (int)daysSinceSell });
        return disallowed;
    }

    /// <summary>Clean up expired wash sale entries (older than 30 days).</summary>
    public void CleanupExpiredWashSales(DateTime currentDate)
    {
        var expired = _washSaleTracker
            .Where(kv => (currentDate - kv.Value.sellDate).TotalDays > 30)
            .Select(kv => kv.Key).ToList();
        foreach (var key in expired) _washSaleTracker.Remove(key);
    }

    /// <summary>
    /// Calculate tax on a closed trade and return the tax amount.
    /// Called when a sell/cover order fills.
    /// </summary>
    public decimal CalculateTradeTax(decimal pnl, int holdingDays, string? symbol = null, DateTime? tradeDate = null)
    {
        if (!Enabled || pnl == 0) return 0;

        bool isLongTerm = holdingDays >= 252;

        if (pnl > 0)
        {
            // Gain
            if (isLongTerm)
            {
                LongTermGains += pnl;
                var tax = Math.Round(pnl * LongTermRate, 2);
                TotalTaxPaid += tax;
                return tax;
            }
            else
            {
                ShortTermGains += pnl;
                var tax = Math.Round(pnl * ShortTermRate, 2);
                TotalTaxPaid += tax;
                return tax;
            }
        }
        else
        {
            // Loss — recorded for offset, no immediate tax
            if (isLongTerm)
                LongTermLosses += Math.Abs(pnl);
            else
                ShortTermLosses += Math.Abs(pnl);

            // Track for wash sale rule
            if (symbol != null && tradeDate.HasValue)
                RecordLossSale(symbol, tradeDate.Value, pnl);

            return 0;
        }
    }

    /// <summary>
    /// Calculate tax on dividend income (qualified: 15%, ordinary: 35%).
    /// </summary>
    public decimal CalculateDividendTax(decimal dividendAmount)
    {
        if (!Enabled) return 0;
        // Assume qualified dividends (lower rate)
        var tax = Math.Round(dividendAmount * LongTermRate, 2);
        DividendTaxPaid += tax;
        TotalTaxPaid += tax;
        return tax;
    }

    /// <summary>Net gains after offsetting losses.</summary>
    public decimal NetShortTermGains => Math.Max(0, ShortTermGains - ShortTermLosses);
    public decimal NetLongTermGains => Math.Max(0, LongTermGains - LongTermLosses);
    public decimal TotalNetGains => NetShortTermGains + NetLongTermGains;

    /// <summary>Effective tax rate on total gains.</summary>
    public decimal EffectiveTaxRate =>
        (ShortTermGains + LongTermGains) > 0
            ? Math.Round(TotalTaxPaid / (ShortTermGains + LongTermGains) * 100, 2)
            : 0;

    /// <summary>Tax saved through loss harvesting.</summary>
    public decimal TaxSaved =>
        Math.Round(Math.Min(ShortTermLosses, ShortTermGains) * ShortTermRate +
                   Math.Min(LongTermLosses, LongTermGains) * LongTermRate, 2);

    /// <summary>Get full tax summary for frontend.</summary>
    public TaxSummary GetSummary()
    {
        return new TaxSummary
        {
            ShortTermGains = ShortTermGains,
            ShortTermLosses = ShortTermLosses,
            LongTermGains = LongTermGains,
            LongTermLosses = LongTermLosses,
            NetShortTermGains = NetShortTermGains,
            NetLongTermGains = NetLongTermGains,
            ShortTermTaxRate = ShortTermRate * 100,
            LongTermTaxRate = LongTermRate * 100,
            TotalTaxPaid = TotalTaxPaid,
            DividendTaxPaid = DividendTaxPaid,
            EffectiveTaxRate = EffectiveTaxRate,
            TaxSaved = TaxSaved,
            WashSaleDisallowed = WashSaleDisallowed,
        };
    }
}

public class TaxSummary
{
    public decimal ShortTermGains { get; set; }
    public decimal ShortTermLosses { get; set; }
    public decimal LongTermGains { get; set; }
    public decimal LongTermLosses { get; set; }
    public decimal NetShortTermGains { get; set; }
    public decimal NetLongTermGains { get; set; }
    public decimal ShortTermTaxRate { get; set; }
    public decimal LongTermTaxRate { get; set; }
    public decimal TotalTaxPaid { get; set; }
    public decimal DividendTaxPaid { get; set; }
    public decimal EffectiveTaxRate { get; set; }
    public decimal TaxSaved { get; set; }
    public decimal WashSaleDisallowed { get; set; }
}
