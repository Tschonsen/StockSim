namespace StockSim.Engine.Models;

/// <summary>
/// Order side: Buy or Sell. See Bible 4.2.
/// </summary>
public enum OrderSide
{
    Buy,
    Sell,
}

/// <summary>
/// Order type. Phase 1 MVP: Market and Limit. See Bible 4.2.
/// </summary>
public enum OrderType
{
    Market,
    Limit,
}

/// <summary>
/// Order lifecycle status. See Bible 4.10.
/// </summary>
public enum OrderStatus
{
    Pending,           // Waiting for execution (market closed, or limit not yet triggered)
    Open,              // Active in the market (limit order waiting for fill)
    Filled,            // Fully executed
    PartiallyFilled,   // Limit order partially filled (Bible 4.3)
    Cancelled,         // Cancelled by player or system
    Rejected,          // Validation failed
    Expired,           // Day order expired at market close
}

/// <summary>
/// Time-in-force for limit orders. See Bible 4.2.3.
/// </summary>
public enum TimeInForce
{
    GTC,   // Good Till Cancelled
    Day,   // Expires at market close
}

/// <summary>
/// Represents a trading order placed by the player.
/// See Bible 4.1-4.2 for order mechanics.
/// </summary>
public class Order
{
    private static long _nextId = 1;

    public long Id { get; }
    public string Symbol { get; }
    public OrderSide Side { get; }
    public OrderType Type { get; }
    public OrderStatus Status { get; set; }
    public TimeInForce TimeInForce { get; }

    /// <summary>Requested quantity in shares.</summary>
    public decimal Quantity { get; }

    /// <summary>Limit price (only for Limit orders).</summary>
    public decimal? LimitPrice { get; }

    /// <summary>Quantity already filled (for partial fills).</summary>
    public decimal FilledQuantity { get; set; }

    /// <summary>Average fill price.</summary>
    public decimal? FillPrice { get; set; }

    /// <summary>Commission charged. Bible 4.1: $4.95 per trade.</summary>
    public decimal Commission { get; set; }

    /// <summary>Game time when the order was placed.</summary>
    public DateTime PlacedAt { get; }

    /// <summary>Game time when the order was filled (or last partial fill).</summary>
    public DateTime? FilledAt { get; set; }

    /// <summary>Rejection or cancellation reason.</summary>
    public string? RejectReason { get; set; }

    public bool IsFilled => Status == OrderStatus.Filled;
    public bool IsActive => Status == OrderStatus.Pending || Status == OrderStatus.Open || Status == OrderStatus.PartiallyFilled;
    public decimal RemainingQuantity => Quantity - FilledQuantity;

    public Order(
        string symbol,
        OrderSide side,
        OrderType type,
        decimal quantity,
        DateTime placedAt,
        decimal? limitPrice = null,
        TimeInForce timeInForce = TimeInForce.GTC)
    {
        Id = _nextId++;
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        Side = side;
        Type = type;
        Quantity = quantity;
        PlacedAt = placedAt;
        LimitPrice = limitPrice;
        TimeInForce = timeInForce;
        Status = OrderStatus.Pending;
    }

    /// <summary>Reset ID counter (for tests).</summary>
    public static void ResetIdCounter() => _nextId = 1;

    /// <summary>Set ID counter to a specific value (for save/load).</summary>
    public static void SetNextId(long nextId) => _nextId = nextId;

    public override string ToString() =>
        $"Order#{Id} {Side} {Type} {Quantity} {Symbol}" +
        (LimitPrice.HasValue ? $" @ ${LimitPrice}" : "") +
        $" [{Status}]";
}
