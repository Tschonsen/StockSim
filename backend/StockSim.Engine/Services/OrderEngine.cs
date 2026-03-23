using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// Result of placing an order.
/// </summary>
public record OrderResult(bool Success, Order? Order, string? Error = null);

/// <summary>
/// Handles order placement, validation, execution, and lifecycle.
/// See Bible 4.1-4.3 for trading mechanics.
///
/// Responsibilities:
///   - Validate orders (cash, positions, quantity)
///   - Execute market orders immediately (at ask/bid + slippage)
///   - Queue limit orders and check them each tick
///   - Track commissions and P&L
///   - Expire day orders at market close
/// </summary>
public class OrderEngine
{
    private readonly Portfolio _portfolio;
    private readonly Logger _log = new("OrderEngine");

    /// <summary>Bible 4.1: $4.95 per trade (default).</summary>
    public const decimal DefaultCommission = 4.95m;

    public OrderEngine(Portfolio portfolio)
    {
        _portfolio = portfolio ?? throw new ArgumentNullException(nameof(portfolio));
    }

    /// <summary>
    /// Place a new order. Market orders execute immediately if market is open.
    /// Limit orders are queued for fill checking.
    /// </summary>
    public OrderResult PlaceOrder(
        string symbol,
        OrderSide side,
        OrderType type,
        decimal quantity,
        Stock stock,
        DateTime gameTime,
        bool isMarketOpen,
        decimal? limitPrice = null,
        TimeInForce timeInForce = TimeInForce.GTC,
        decimal? stopPrice = null,
        decimal? trailAmount = null)
    {
        // Validation
        if (quantity <= 0)
        {
            var order = CreateOrder(symbol, side, type, quantity, gameTime, limitPrice, timeInForce, stopPrice, trailAmount);
            order.Status = OrderStatus.Rejected;
            order.RejectReason = "Quantity must be positive.";
            _portfolio.Orders.Add(order);
            _log.Warn("Order rejected", new { reason = order.RejectReason, symbol, quantity });
            return new OrderResult(false, order, order.RejectReason);
        }

        if (type == OrderType.Limit && !limitPrice.HasValue)
        {
            var order = CreateOrder(symbol, side, type, quantity, gameTime, limitPrice, timeInForce, stopPrice, trailAmount);
            order.Status = OrderStatus.Rejected;
            order.RejectReason = "Limit price is required for limit orders.";
            _portfolio.Orders.Add(order);
            _log.Warn("Order rejected", new { reason = order.RejectReason, symbol });
            return new OrderResult(false, order, order.RejectReason);
        }

        if ((type == OrderType.Stop || type == OrderType.StopLimit) && !stopPrice.HasValue)
        {
            var order = CreateOrder(symbol, side, type, quantity, gameTime, limitPrice, timeInForce, stopPrice, trailAmount);
            order.Status = OrderStatus.Rejected;
            order.RejectReason = "Stop price is required for stop orders.";
            _portfolio.Orders.Add(order);
            _log.Warn("Order rejected", new { reason = order.RejectReason, symbol });
            return new OrderResult(false, order, order.RejectReason);
        }

        if (type == OrderType.StopLimit && !limitPrice.HasValue)
        {
            var order = CreateOrder(symbol, side, type, quantity, gameTime, limitPrice, timeInForce, stopPrice, trailAmount);
            order.Status = OrderStatus.Rejected;
            order.RejectReason = "Limit price is required for stop-limit orders.";
            _portfolio.Orders.Add(order);
            return new OrderResult(false, order, order.RejectReason);
        }

        if (type == OrderType.TrailingStop && (!trailAmount.HasValue || trailAmount <= 0))
        {
            var order = CreateOrder(symbol, side, type, quantity, gameTime, limitPrice, timeInForce, stopPrice, trailAmount);
            order.Status = OrderStatus.Rejected;
            order.RejectReason = "Trail amount is required for trailing stop orders.";
            _portfolio.Orders.Add(order);
            return new OrderResult(false, order, order.RejectReason);
        }

        // Side-specific validation
        if (side == OrderSide.Sell)
        {
            if (!_portfolio.Positions.TryGetValue(symbol, out var pos))
            {
                var order = CreateOrder(symbol, side, type, quantity, gameTime, limitPrice, timeInForce, stopPrice, trailAmount);
                order.Status = OrderStatus.Rejected;
                order.RejectReason = $"No position in {symbol}.";
                _portfolio.Orders.Add(order);
                _log.Warn("Order rejected", new { reason = order.RejectReason, symbol });
                return new OrderResult(false, order, order.RejectReason);
            }

            if (quantity > pos.Shares)
            {
                var order = CreateOrder(symbol, side, type, quantity, gameTime, limitPrice, timeInForce, stopPrice, trailAmount);
                order.Status = OrderStatus.Rejected;
                order.RejectReason = $"You only own {pos.Shares} shares of {symbol}.";
                _portfolio.Orders.Add(order);
                _log.Warn("Order rejected", new { reason = order.RejectReason, symbol, owned = pos.Shares, requested = quantity });
                return new OrderResult(false, order, order.RejectReason);
            }
        }

        if (side == OrderSide.Buy)
        {
            var estimatedCost = (stock.AskPrice * quantity) + DefaultCommission;
            if (estimatedCost > _portfolio.Cash)
            {
                var order = CreateOrder(symbol, side, type, quantity, gameTime, limitPrice, timeInForce, stopPrice, trailAmount);
                order.Status = OrderStatus.Rejected;
                order.RejectReason = $"Insufficient funds. You need ${estimatedCost:F2} but have ${_portfolio.Cash:F2}.";
                _portfolio.Orders.Add(order);
                _log.Warn("Order rejected", new { reason = "insufficient_funds", symbol, needed = estimatedCost, available = _portfolio.Cash });
                return new OrderResult(false, order, order.RejectReason);
            }
        }

        // Create the order
        var newOrder = CreateOrder(symbol, side, type, quantity, gameTime, limitPrice, timeInForce, stopPrice, trailAmount);
        _portfolio.Orders.Add(newOrder);

        // Market orders: execute if market open, otherwise pend
        if (type == OrderType.Market)
        {
            if (isMarketOpen)
            {
                ExecuteMarketOrder(newOrder, stock, gameTime);
            }
            else
            {
                newOrder.Status = OrderStatus.Pending;
                _log.Info("Market order queued (market closed)", new { id = newOrder.Id, symbol, side = side.ToString() });
            }
            return new OrderResult(true, newOrder);
        }

        // Limit orders: check if immediately fillable, otherwise queue
        if (type == OrderType.Limit)
        {
            if (isMarketOpen && CanFillLimitOrder(newOrder, stock))
            {
                ExecuteLimitOrder(newOrder, stock, gameTime);
            }
            else
            {
                newOrder.Status = isMarketOpen ? OrderStatus.Open : OrderStatus.Pending;
                _log.Info("Limit order placed", new { id = newOrder.Id, symbol, side = side.ToString(), limitPrice, status = newOrder.Status.ToString() });
            }
            return new OrderResult(true, newOrder);
        }

        // Stop orders: queue for trigger checking
        if (type == OrderType.Stop || type == OrderType.StopLimit)
        {
            newOrder.Status = isMarketOpen ? OrderStatus.Open : OrderStatus.Pending;
            _log.Info("Stop order placed", new { id = newOrder.Id, symbol, side = side.ToString(), stopPrice, status = newOrder.Status.ToString() });
            return new OrderResult(true, newOrder);
        }

        // Trailing stop: initialize high water mark and stop price
        if (type == OrderType.TrailingStop)
        {
            newOrder.HighWaterMark = stock.CurrentPrice;
            newOrder.StopPrice = stock.CurrentPrice - trailAmount!.Value;
            newOrder.Status = isMarketOpen ? OrderStatus.Open : OrderStatus.Pending;
            _log.Info("Trailing stop placed", new
            {
                id = newOrder.Id, symbol, trailAmount,
                stopPrice = newOrder.StopPrice, highWaterMark = newOrder.HighWaterMark,
            });
            return new OrderResult(true, newOrder);
        }

        return new OrderResult(true, newOrder);
    }

    /// <summary>
    /// Check all open limit orders against current stock price.
    /// Called each tick from the game loop.
    /// </summary>
    public List<Order> CheckLimitOrders(Stock stock, DateTime gameTime, bool isMarketOpen)
    {
        if (!isMarketOpen) return new();

        var filled = new List<Order>();
        var activeOrders = _portfolio.Orders
            .Where(o => o.Symbol == stock.Symbol && o.IsActive &&
                   (o.Type == OrderType.Limit || (o.Type == OrderType.StopLimit && o.StopTriggered)))
            .ToList();

        foreach (var order in activeOrders)
        {
            if (CanFillLimitOrder(order, stock))
            {
                ExecuteLimitOrder(order, stock, gameTime);
                filled.Add(order);
            }
        }

        return filled;
    }

    /// <summary>
    /// Check stop and trailing stop orders against current price.
    /// Called each tick. Bible 4.2.5-4.2.7.
    /// </summary>
    public List<Order> CheckStopOrders(Stock stock, DateTime gameTime, bool isMarketOpen)
    {
        if (!isMarketOpen) return new();

        var triggered = new List<Order>();
        var activeStops = _portfolio.Orders
            .Where(o => o.Symbol == stock.Symbol && o.IsActive &&
                   (o.Type == OrderType.Stop || o.Type == OrderType.StopLimit || o.Type == OrderType.TrailingStop))
            .ToList();

        foreach (var order in activeStops)
        {
            // Update trailing stop high water mark
            if (order.Type == OrderType.TrailingStop && order.TrailAmount.HasValue)
            {
                if (order.Side == OrderSide.Sell && stock.CurrentPrice > order.HighWaterMark)
                {
                    order.HighWaterMark = stock.CurrentPrice;
                    order.StopPrice = stock.CurrentPrice - order.TrailAmount.Value;
                }
            }

            // Check if stop is triggered
            bool isTriggered = false;
            if (order.Side == OrderSide.Sell)
            {
                // Sell stop: triggers when price falls to or below stop
                isTriggered = stock.CurrentPrice <= order.StopPrice;
            }
            else
            {
                // Buy stop: triggers when price rises to or above stop
                isTriggered = stock.CurrentPrice >= order.StopPrice;
            }

            if (!isTriggered) continue;

            if (order.Type == OrderType.StopLimit)
            {
                // StopLimit: mark as triggered, becomes a limit order
                order.StopTriggered = true;
                // Check if limit can fill immediately
                if (CanFillLimitOrder(order, stock))
                {
                    ExecuteLimitOrder(order, stock, gameTime);
                    triggered.Add(order);
                }
                // Otherwise stays open as a limit order (will be checked by CheckLimitOrders)
            }
            else
            {
                // Stop and TrailingStop: execute as market order
                ExecuteMarketOrder(order, stock, gameTime);
                triggered.Add(order);
            }

            _log.Info("Stop triggered", new { id = order.Id, type = order.Type.ToString(), stopPrice = order.StopPrice, currentPrice = stock.CurrentPrice });
        }

        return triggered;
    }

    /// <summary>
    /// Execute pending market orders when market opens.
    /// </summary>
    public List<Order> ExecutePendingOrders(Stock stock, DateTime gameTime, bool isMarketOpen)
    {
        if (!isMarketOpen) return new();

        var executed = new List<Order>();
        var pending = _portfolio.Orders
            .Where(o => o.Symbol == stock.Symbol && o.Status == OrderStatus.Pending && o.Type == OrderType.Market)
            .ToList();

        foreach (var order in pending)
        {
            // Re-validate
            if (order.Side == OrderSide.Buy)
            {
                var cost = stock.AskPrice * order.Quantity + DefaultCommission;
                if (cost > _portfolio.Cash)
                {
                    order.Status = OrderStatus.Rejected;
                    order.RejectReason = "Insufficient funds at market open.";
                    continue;
                }
            }

            ExecuteMarketOrder(order, stock, gameTime);
            executed.Add(order);
        }

        return executed;
    }

    /// <summary>
    /// Expire all Day orders. Called at market close.
    /// </summary>
    public List<Order> ExpireDayOrders()
    {
        var expired = new List<Order>();
        var dayOrders = _portfolio.Orders
            .Where(o => o.TimeInForce == TimeInForce.Day && o.IsActive)
            .ToList();

        foreach (var order in dayOrders)
        {
            order.Status = OrderStatus.Expired;
            expired.Add(order);
            _log.Info("Day order expired", new { id = order.Id, symbol = order.Symbol });
        }

        return expired;
    }

    /// <summary>
    /// Cancel an active order by ID.
    /// </summary>
    public bool CancelOrder(long orderId)
    {
        var order = _portfolio.Orders.FirstOrDefault(o => o.Id == orderId);
        if (order == null || !order.IsActive) return false;

        order.Status = OrderStatus.Cancelled;
        _log.Info("Order cancelled", new { id = orderId, symbol = order.Symbol });
        return true;
    }

    /// <summary>
    /// Get all active (open/pending) orders.
    /// </summary>
    public List<Order> GetActiveOrders()
    {
        return _portfolio.Orders.Where(o => o.IsActive).ToList();
    }

    // --- Private execution methods ---

    private void ExecuteMarketOrder(Order order, Stock stock, DateTime gameTime)
    {
        var fillPrice = order.Side == OrderSide.Buy ? stock.AskPrice : stock.BidPrice;

        // Slippage for large orders (Bible 4.2.1)
        var slippage = CalculateSlippage(order.Quantity, stock);
        if (order.Side == OrderSide.Buy)
            fillPrice *= (1m + slippage);
        else
            fillPrice *= (1m - slippage);
        fillPrice = Math.Round(fillPrice, 2);

        ApplyFill(order, fillPrice, order.Quantity, gameTime);
    }

    private void ExecuteLimitOrder(Order order, Stock stock, DateTime gameTime)
    {
        // Limit orders fill at market price if better than limit
        var fillPrice = order.Side == OrderSide.Buy
            ? Math.Min(stock.AskPrice, order.LimitPrice!.Value)
            : Math.Max(stock.BidPrice, order.LimitPrice!.Value);

        ApplyFill(order, fillPrice, order.Quantity, gameTime);
    }

    private void ApplyFill(Order order, decimal fillPrice, decimal fillQuantity, DateTime gameTime)
    {
        order.FillPrice = fillPrice;
        order.FilledQuantity = fillQuantity;
        order.FilledAt = gameTime;
        order.Commission = DefaultCommission;
        order.Status = OrderStatus.Filled;

        if (order.Side == OrderSide.Buy)
        {
            var totalCost = fillPrice * fillQuantity + DefaultCommission;
            _portfolio.Cash -= totalCost;

            if (_portfolio.Positions.TryGetValue(order.Symbol, out var pos))
            {
                pos.AddShares(fillQuantity, fillPrice);
            }
            else
            {
                _portfolio.Positions[order.Symbol] = new Position(order.Symbol, fillQuantity, fillPrice);
            }
        }
        else // Sell
        {
            var totalProceeds = fillPrice * fillQuantity - DefaultCommission;
            _portfolio.Cash += totalProceeds;

            if (_portfolio.Positions.TryGetValue(order.Symbol, out var pos))
            {
                var realizedPnL = pos.RemoveShares(fillQuantity, fillPrice);
                _portfolio.RealizedPnL += realizedPnL;

                if (pos.Shares == 0)
                    _portfolio.Positions.Remove(order.Symbol);
            }
        }

        _portfolio.TotalCommissions += DefaultCommission;
        _portfolio.TradeCount++;

        _log.Info("Order filled", new
        {
            id = order.Id,
            symbol = order.Symbol,
            side = order.Side.ToString(),
            type = order.Type.ToString(),
            quantity = fillQuantity,
            fillPrice,
            commission = DefaultCommission,
            cash = _portfolio.Cash,
        });
    }

    private bool CanFillLimitOrder(Order order, Stock stock)
    {
        if (order.Side == OrderSide.Buy)
        {
            // Buy limit: fill when ask ≤ limit price
            return stock.AskPrice <= order.LimitPrice!.Value;
        }
        else
        {
            // Sell limit: fill when bid ≥ limit price
            return stock.BidPrice >= order.LimitPrice!.Value;
        }
    }

    /// <summary>
    /// Bible 4.2.1: Slippage = (OrderSize / AvgVolume) × SpreadFactor × 0.5
    /// </summary>
    private decimal CalculateSlippage(decimal orderSize, Stock stock)
    {
        if (stock.AverageVolume <= 0) return 0m;

        var volumeRatio = orderSize / stock.AverageVolume;
        if (volumeRatio < 0.01m) return 0m; // Negligible for small orders

        var spreadFactor = stock.SpreadPercent / 100m;
        spreadFactor = Math.Max(spreadFactor, 0.001m); // Minimum spread factor

        var slippage = volumeRatio * spreadFactor * 0.5m;
        return Math.Min(slippage, 0.05m); // Cap at 5%
    }

    private static Order CreateOrder(
        string symbol, OrderSide side, OrderType type, decimal quantity,
        DateTime gameTime, decimal? limitPrice, TimeInForce timeInForce,
        decimal? stopPrice = null, decimal? trailAmount = null)
    {
        return new Order(symbol, side, type, quantity, gameTime, limitPrice, timeInForce, stopPrice, trailAmount);
    }
}
