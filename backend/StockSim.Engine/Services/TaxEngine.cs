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

    /// <summary>
    /// Calculate tax on a closed trade and return the tax amount.
    /// Called when a sell/cover order fills.
    /// </summary>
    public decimal CalculateTradeTax(decimal pnl, int holdingDays)
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
}
