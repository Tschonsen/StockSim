namespace StockSim.Engine.Models;

/// <summary>
/// Macroeconomic indicators that drive market behavior.
/// These change over time and influence sector performance, volatility, and AI behavior.
/// </summary>
public class EconomicData
{
    /// <summary>Federal funds rate. Range: 0-15%. Affects all sectors, especially Real Estate and Financials.</summary>
    public decimal InterestRate { get; set; } = 2.5m;

    /// <summary>Annual inflation rate. Range: -1% to 15%. Affects consumer spending, commodity prices.</summary>
    public decimal InflationRate { get; set; } = 2.0m;

    /// <summary>Unemployment rate. Range: 2-15%. Inversely correlates with consumer spending.</summary>
    public decimal UnemploymentRate { get; set; } = 4.0m;

    /// <summary>Quarterly GDP growth rate. Range: -5% to 8%. Overall economic health.</summary>
    public decimal GDPGrowth { get; set; } = 2.5m;

    /// <summary>Consumer confidence index. Range: 20-120. Drives retail and consumer sectors.</summary>
    public decimal ConsumerConfidence { get; set; } = 95m;

    /// <summary>10-Year Treasury yield. Range: 0.5-10%. Benchmark for stock valuation.</summary>
    public decimal TreasuryYield10Y { get; set; } = 3.5m;

    /// <summary>Crude oil price per barrel. Range: $20-$150. Drives energy sector.</summary>
    public decimal OilPrice { get; set; } = 75m;

    /// <summary>Gold price per ounce. Range: $800-$3000. Safe haven indicator.</summary>
    public decimal GoldPrice { get; set; } = 1950m;

    /// <summary>Housing starts (thousands). Range: 500-2000. Real estate indicator.</summary>
    public decimal HousingStarts { get; set; } = 1400m;

    /// <summary>Manufacturing PMI. >50 = expansion, <50 = contraction.</summary>
    public decimal ManufacturingPMI { get; set; } = 52m;
}

/// <summary>
/// Scheduled economic event that will release data on a specific date.
/// </summary>
public class EconomicEvent
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Indicator { get; set; } = "";
    public DateTime ScheduledDate { get; set; }
    public decimal? PreviousValue { get; set; }
    public decimal? ExpectedValue { get; set; }
    public decimal? ActualValue { get; set; }
    public bool Released { get; set; }
    public string Impact { get; set; } = "Medium"; // Low, Medium, High

    /// <summary>Surprise: positive = better than expected, negative = worse.</summary>
    public decimal Surprise => (ActualValue ?? 0) - (ExpectedValue ?? 0);
}
