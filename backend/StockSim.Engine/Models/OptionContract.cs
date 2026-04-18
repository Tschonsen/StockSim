namespace StockSim.Engine.Models;

/// <summary>Call or Put.</summary>
public enum OptionType { Call, Put }

/// <summary>
/// A single options contract. European-style, cash-settled, 100x multiplier.
/// Priced via Black-Scholes. See Spec Phase 3.
/// </summary>
public class OptionContract
{
    private static long _nextId;

    public long Id { get; set; } = Interlocked.Increment(ref _nextId);
    public string UnderlyingSymbol { get; set; } = "";
    public OptionType Type { get; set; }
    public decimal StrikePrice { get; set; }
    public DateTime ExpirationDate { get; set; }

    // Pricing
    public decimal TheoreticalPrice { get; set; }
    public decimal BidPrice { get; set; }
    public decimal AskPrice { get; set; }
    public decimal LastPrice { get; set; }
    public double ImpliedVolatility { get; set; }

    // Greeks
    public double Delta { get; set; }
    public double Gamma { get; set; }
    public double Theta { get; set; }   // Per calendar day
    public double Vega { get; set; }    // Per 1% vol change
    public double Rho { get; set; }     // Per 1% rate change

    // Volume / Open Interest
    public int Volume { get; set; }
    public int OpenInterest { get; set; }

    // Contract specs
    public const int Multiplier = 100;

    // Status
    public bool IsExpired { get; set; }

    /// <summary>In-the-money check.</summary>
    public bool IsITM(decimal stockPrice) => Type == OptionType.Call
        ? stockPrice > StrikePrice
        : stockPrice < StrikePrice;

    /// <summary>Intrinsic value (payoff if exercised now).</summary>
    public decimal IntrinsicValue(decimal stockPrice) => Type == OptionType.Call
        ? Math.Max(0, stockPrice - StrikePrice)
        : Math.Max(0, StrikePrice - stockPrice);

    /// <summary>Time value = theoretical price - intrinsic value.</summary>
    public decimal TimeValue(decimal stockPrice) =>
        Math.Max(0, TheoreticalPrice - IntrinsicValue(stockPrice));

    /// <summary>Cash settlement at expiry.</summary>
    public decimal SettlementValue(decimal stockPrice) =>
        IntrinsicValue(stockPrice) * Multiplier;

    /// <summary>OCC-style ticker: AAPL 250117C00150000 → AAPL Jan-17-2025 $150 Call</summary>
    public string DisplayName =>
        $"{UnderlyingSymbol} {ExpirationDate:MMM-dd} ${StrikePrice:F0} {Type}";

    /// <summary>Days to expiration from a given date.</summary>
    public int DaysToExpiry(DateTime fromDate) =>
        Math.Max(0, (ExpirationDate.Date - fromDate.Date).Days);

    /// <summary>Time to expiry in years (for Black-Scholes).</summary>
    public double TimeToExpiryYears(DateTime fromDate) =>
        Math.Max(0.0001, DaysToExpiry(fromDate) / 365.0);
}

/// <summary>
/// All options for one expiration date on one underlying.
/// </summary>
public class ExpirationSlice
{
    public DateTime ExpirationDate { get; set; }
    public int DaysToExpiry { get; set; }
    public List<decimal> Strikes { get; set; } = new();
    public Dictionary<decimal, OptionContract> Calls { get; set; } = new();
    public Dictionary<decimal, OptionContract> Puts { get; set; } = new();
}

/// <summary>
/// Full options chain for one underlying stock.
/// </summary>
public class OptionChain
{
    public string UnderlyingSymbol { get; set; } = "";
    public List<DateTime> Expirations { get; set; } = new();
    public Dictionary<DateTime, ExpirationSlice> Slices { get; set; } = new();

    /// <summary>Get all contracts across all expirations.</summary>
    public IEnumerable<OptionContract> AllContracts =>
        Slices.Values.SelectMany(s => s.Calls.Values.Concat(s.Puts.Values));
}

/// <summary>Player's option position.</summary>
public class OptionPosition
{
    public long ContractId { get; set; }
    public string UnderlyingSymbol { get; set; } = "";
    public OptionType Type { get; set; }
    public decimal StrikePrice { get; set; }
    public DateTime ExpirationDate { get; set; }
    public int Quantity { get; set; }       // Positive = long, Negative = short (written)
    public decimal AvgCost { get; set; }    // Price paid per contract
    public decimal MarketValue { get; set; }
    public decimal UnrealizedPnL { get; set; }

    public bool IsLong => Quantity > 0;
    public bool IsShort => Quantity < 0;
}
