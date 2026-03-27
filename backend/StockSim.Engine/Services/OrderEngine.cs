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

    /// <summary>Override commission from game settings. If set, used instead of DefaultCommission.</summary>
    public static decimal? DefaultCommissionOverride { get; set; }

    /// <summary>Fired when a closing trade (sell/cover) fills. Used for trade journal.</summary>
    public event Action<TradeRecord>? OnTradeCompleted;

    /// <summary>Fired when an order is cancelled. Used for SMA spoofing detection.</summary>
    public event Action<Order>? OnOrderCancelled;

    /// <summary>Active scenario for rule enforcement (set from GameLoop).</summary>
    public Scenario? ActiveScenario { get; set; }

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
        // Scenario rule validation
        if (ActiveScenario != null && ActiveScenario.IsActive)
        {
            if (ActiveScenario.OnlyOneStock && (side == OrderSide.Buy || side == OrderSide.Short))
            {
                var existingPositions = _portfolio.Positions.Keys.Where(k => k != symbol).ToList();
                if (existingPositions.Count > 0)
                {
                    var order = CreateOrder(symbol, side, type, quantity, gameTime, limitPrice, timeInForce, stopPrice, trailAmount);
                    order.Status = OrderStatus.Rejected;
                    order.RejectReason = $"Scenario rule: You may only trade one stock. Already holding {existingPositions[0]}.";
                    _portfolio.Orders.Add(order);
                    return new OrderResult(false, order, order.RejectReason);
                }
            }
            if (ActiveScenario.OnlyPennyStocks && stock.CurrentPrice >= 5m && (side == OrderSide.Buy || side == OrderSide.Short))
            {
                var order = CreateOrder(symbol, side, type, quantity, gameTime, limitPrice, timeInForce, stopPrice, trailAmount);
                order.Status = OrderStatus.Rejected;
                order.RejectReason = $"Scenario rule: Only stocks under $5 allowed. {symbol} is ${stock.CurrentPrice:F2}.";
                _portfolio.Orders.Add(order);
                return new OrderResult(false, order, order.RejectReason);
            }
            if (ActiveScenario.OnlyDividendStocks && stock.DividendYield <= 0 && (side == OrderSide.Buy))
            {
                var order = CreateOrder(symbol, side, type, quantity, gameTime, limitPrice, timeInForce, stopPrice, trailAmount);
                order.Status = OrderStatus.Rejected;
                order.RejectReason = $"Scenario rule: Only dividend-paying stocks allowed. {symbol} has no dividend.";
                _portfolio.Orders.Add(order);
                return new OrderResult(false, order, order.RejectReason);
            }
        }

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
            if (!_portfolio.Positions.TryGetValue(symbol, out var pos) || pos.IsShort)
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

        if (side == OrderSide.Cover)
        {
            if (!_portfolio.Positions.TryGetValue(symbol, out var pos) || !pos.IsShort)
            {
                var order = CreateOrder(symbol, side, type, quantity, gameTime, limitPrice, timeInForce, stopPrice, trailAmount);
                order.Status = OrderStatus.Rejected;
                order.RejectReason = $"No short position in {symbol}.";
                _portfolio.Orders.Add(order);
                return new OrderResult(false, order, order.RejectReason);
            }

            if (quantity > Math.Abs(pos.Shares))
            {
                var order = CreateOrder(symbol, side, type, quantity, gameTime, limitPrice, timeInForce, stopPrice, trailAmount);
                order.Status = OrderStatus.Rejected;
                order.RejectReason = $"You only have {Math.Abs(pos.Shares)} shares shorted of {symbol}.";
                _portfolio.Orders.Add(order);
                return new OrderResult(false, order, order.RejectReason);
            }
        }

        if (side == OrderSide.Buy)
        {
            var commission = GetCommission();
            var estimatedCost = (stock.AskPrice * quantity) + commission;
            // If margin enabled, use buying power instead of just cash
            var availableFunds = _portfolio.MarginEnabled
                ? _portfolio.Cash + (_portfolio.Cash * (_portfolio.MaxLeverage - 1))
                : _portfolio.Cash;
            if (estimatedCost > availableFunds)
            {
                var order = CreateOrder(symbol, side, type, quantity, gameTime, limitPrice, timeInForce, stopPrice, trailAmount);
                order.Status = OrderStatus.Rejected;
                order.RejectReason = $"Insufficient funds. You need ${estimatedCost:F2} but have ${_portfolio.Cash:F2}.";
                _portfolio.Orders.Add(order);
                _log.Warn("Order rejected", new { reason = "insufficient_funds", symbol, needed = estimatedCost, available = _portfolio.Cash });
                return new OrderResult(false, order, order.RejectReason);
            }
        }

        // Issue 19: Minimum short position value to prevent penny short abuse
        if (side == OrderSide.Short)
        {
            var positionValue = quantity * stock.CurrentPrice;
            if (positionValue < 50m)
            {
                var order = CreateOrder(symbol, side, type, quantity, gameTime, limitPrice, timeInForce, stopPrice, trailAmount);
                order.Status = OrderStatus.Rejected;
                order.RejectReason = "Minimum short position value is $50.";
                _portfolio.Orders.Add(order);
                _log.Warn("Order rejected", new { reason = "penny_short", symbol, positionValue });
                return new OrderResult(false, order, order.RejectReason);
            }
        }

        // SSR enforcement (Bible 4.4.2): Alternative Uptick Rule
        // When SSR is active, short sales must be at Bid + $0.01 or higher
        if (side == OrderSide.Short && stock.IsSSR)
        {
            var uptickPrice = stock.BidPrice + 0.01m;

            if (type == OrderType.Market)
            {
                // Convert market short to limit short at Bid + $0.01
                type = OrderType.Limit;
                limitPrice = uptickPrice;
                _log.Info("SSR: market short converted to limit", new { symbol, uptickPrice });
            }
            else if (type == OrderType.Limit && limitPrice.HasValue && limitPrice.Value <= stock.BidPrice)
            {
                // Reject limit short if price is at or below bid
                var order = CreateOrder(symbol, side, type, quantity, gameTime, limitPrice, timeInForce, stopPrice, trailAmount);
                order.Status = OrderStatus.Rejected;
                order.RejectReason = $"SSR active: Short sale price must be above bid (${stock.BidPrice:F2}). Minimum: ${uptickPrice:F2}.";
                _portfolio.Orders.Add(order);
                _log.Warn("SSR: short order rejected", new { symbol, limitPrice, bid = stock.BidPrice });
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
        OnOrderCancelled?.Invoke(order);
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
        // Buy/Cover buy at ask, Sell/Short sell at bid
        var isBuying = order.Side == OrderSide.Buy || order.Side == OrderSide.Cover;
        var fillPrice = isBuying ? stock.AskPrice : stock.BidPrice;

        // Partial fills: orders >5% of daily volume get partially filled
        var remainingQty = order.Quantity - order.FilledQuantity;
        var maxFillPerTick = stock.AverageVolume > 0
            ? Math.Max(1m, stock.AverageVolume * 0.05m) // Max 5% of daily vol per fill
            : remainingQty;
        var fillQty = Math.Min(remainingQty, maxFillPerTick);

        // Slippage for large orders (Almgren-Chriss sqrt model)
        var slippage = CalculateSlippage(fillQty, stock);
        fillPrice *= isBuying ? (1m + slippage) : (1m - slippage);
        fillPrice = Math.Round(fillPrice, 2);

        if (fillQty < remainingQty)
        {
            // Partial fill: fill what we can, keep order open for remaining
            ApplyPartialFill(order, fillPrice, fillQty, gameTime);
            _log.Info("Partial fill", new { id = order.Id, filled = fillQty, remaining = remainingQty - fillQty });
        }
        else
        {
            ApplyFill(order, fillPrice, fillQty, gameTime);
        }
    }

    private void ExecuteLimitOrder(Order order, Stock stock, DateTime gameTime)
    {
        // Limit orders fill at market price if better than limit
        var fillPrice = order.Side == OrderSide.Buy
            ? Math.Min(stock.AskPrice, order.LimitPrice!.Value)
            : Math.Max(stock.BidPrice, order.LimitPrice!.Value);

        ApplyFill(order, fillPrice, order.Quantity, gameTime);
    }

    /// <summary>
    /// Partial fill: fills some quantity but keeps order open for remaining.
    /// No commission charged until full fill (charged once at completion).
    /// </summary>
    private void ApplyPartialFill(Order order, decimal fillPrice, decimal fillQuantity, DateTime gameTime)
    {
        // Track weighted average fill price
        var totalFilledBefore = order.FilledQuantity;
        var avgPriceBefore = order.FillPrice ?? fillPrice;
        order.FilledQuantity += fillQuantity;
        order.FillPrice = Math.Round(
            (avgPriceBefore * totalFilledBefore + fillPrice * fillQuantity) / order.FilledQuantity, 2);
        order.FilledAt = gameTime;
        order.Status = OrderStatus.Open; // Still open for remaining qty

        // Apply the partial fill to portfolio
        if (order.Side == OrderSide.Buy)
        {
            var cost = fillPrice * fillQuantity;
            _portfolio.Cash -= cost;
            if (_portfolio.Positions.TryGetValue(order.Symbol, out var pos))
                pos.AddShares(fillQuantity, fillPrice);
            else
                _portfolio.Positions[order.Symbol] = new Position(order.Symbol, fillQuantity, fillPrice);
        }
        else if (order.Side == OrderSide.Short)
        {
            var proceeds = fillPrice * fillQuantity;
            _portfolio.Cash += proceeds;
            if (_portfolio.Positions.TryGetValue(order.Symbol, out var pos))
                pos.AddShares(-fillQuantity, fillPrice);
            else
                _portfolio.Positions[order.Symbol] = new Position(order.Symbol, -fillQuantity, fillPrice);
        }

        // Check if now fully filled
        if (order.FilledQuantity >= order.Quantity)
        {
            var commission = GetCommission();
            order.Commission = commission;
            _portfolio.Cash -= commission;
            _portfolio.TotalCommissions += commission;
            _portfolio.TradeCount++;
            order.Status = OrderStatus.Filled;
        }
    }

    private decimal GetCommission() => DefaultCommissionOverride ?? DefaultCommission;

    private void ApplyFill(Order order, decimal fillPrice, decimal fillQuantity, DateTime gameTime)
    {
        var commission = GetCommission();
        order.FillPrice = fillPrice;
        order.FilledQuantity = fillQuantity;
        order.FilledAt = gameTime;
        order.Commission = commission;
        order.Status = OrderStatus.Filled;

        if (order.Side == OrderSide.Buy)
        {
            var totalCost = fillPrice * fillQuantity + commission;
            if (_portfolio.MarginEnabled && totalCost > _portfolio.Cash)
            {
                // Borrow on margin for the difference
                var borrowed = totalCost - _portfolio.Cash;
                _portfolio.MarginBalance += borrowed;
                _portfolio.Cash = 0;
            }
            else
            {
                _portfolio.Cash -= totalCost;
            }

            if (_portfolio.Positions.TryGetValue(order.Symbol, out var pos))
            {
                pos.AddShares(fillQuantity, fillPrice);
            }
            else
            {
                _portfolio.Positions[order.Symbol] = new Position(order.Symbol, fillQuantity, fillPrice);
            }
        }
        else if (order.Side == OrderSide.Sell)
        {
            var totalProceeds = fillPrice * fillQuantity - commission;
            // Repay margin first if outstanding
            if (_portfolio.MarginEnabled && _portfolio.MarginBalance > 0)
            {
                var repay = Math.Min(_portfolio.MarginBalance, totalProceeds);
                _portfolio.MarginBalance -= repay;
                totalProceeds -= repay;
            }
            _portfolio.Cash += totalProceeds;

            if (_portfolio.Positions.TryGetValue(order.Symbol, out var pos))
            {
                var realizedPnL = pos.RemoveShares(fillQuantity, fillPrice);
                _portfolio.RealizedPnL += realizedPnL;

                if (pos.Shares == 0)
                    _portfolio.Positions.Remove(order.Symbol);
            }
        }
        else if (order.Side == OrderSide.Short)
        {
            // Short: receive proceeds, create negative position
            var totalProceeds = fillPrice * fillQuantity - commission;
            _portfolio.Cash += totalProceeds;

            if (_portfolio.Positions.TryGetValue(order.Symbol, out var pos))
            {
                // Adding to existing short
                pos.AddShares(-fillQuantity, fillPrice);
            }
            else
            {
                _portfolio.Positions[order.Symbol] = new Position(order.Symbol, -fillQuantity, fillPrice);
            }
        }
        else if (order.Side == OrderSide.Cover)
        {
            // Cover: pay to buy back, close short position
            var totalCost = fillPrice * fillQuantity + commission;
            _portfolio.Cash -= totalCost;

            if (_portfolio.Positions.TryGetValue(order.Symbol, out var pos))
            {
                // Realized P&L for short: (short price - cover price) * shares
                var realizedPnL = fillQuantity * (pos.AverageCost - fillPrice);
                _portfolio.RealizedPnL += Math.Round(realizedPnL, 2);

                pos.Shares += fillQuantity; // Adding positive to negative
                if (pos.Shares == 0)
                    _portfolio.Positions.Remove(order.Symbol);
            }
        }

        _portfolio.TotalCommissions += commission;
        _portfolio.TradeCount++;

        _log.Info("Order filled", new
        {
            id = order.Id,
            symbol = order.Symbol,
            side = order.Side.ToString(),
            type = order.Type.ToString(),
            quantity = fillQuantity,
            fillPrice,
            commission,
            cash = _portfolio.Cash,
        });

        // Fire trade completed event for sell/cover (closing trades)
        if (order.Side == OrderSide.Sell || order.Side == OrderSide.Cover)
        {
            // Calculate P&L for this trade
            decimal entryPrice;
            if (order.Side == OrderSide.Sell)
            {
                // For sell: we need the avg cost before the sell
                entryPrice = _portfolio.Positions.TryGetValue(order.Symbol, out var remainingPos)
                    ? remainingPos.AverageCost
                    : fillPrice; // Position was fully closed, use order info
                // Actually, the position may be removed. Check orders for the buy side.
                var buyOrders = _portfolio.Orders
                    .Where(o => o.Symbol == order.Symbol && o.IsFilled && o.Side == OrderSide.Buy)
                    .OrderByDescending(o => o.FilledAt)
                    .FirstOrDefault();
                if (buyOrders != null) entryPrice = buyOrders.FillPrice ?? fillPrice;
                if (remainingPos != null) entryPrice = remainingPos.AverageCost;
            }
            else
            {
                // For cover: avg cost of the short position
                entryPrice = _portfolio.Positions.TryGetValue(order.Symbol, out var shortPos)
                    ? shortPos.AverageCost : fillPrice;
            }

            var pnl = order.Side == OrderSide.Sell
                ? (fillPrice - entryPrice) * fillQuantity - commission
                : (entryPrice - fillPrice) * fillQuantity - commission;
            var pnlPct = entryPrice > 0 ? Math.Round(pnl / (entryPrice * fillQuantity) * 100, 2) : 0;

            var holdingDays = 0;
            var buyTime = _portfolio.Orders
                .Where(o => o.Symbol == order.Symbol && o.IsFilled &&
                       (o.Side == OrderSide.Buy || o.Side == OrderSide.Short))
                .OrderByDescending(o => o.FilledAt)
                .FirstOrDefault()?.FilledAt;
            if (buyTime.HasValue)
                holdingDays = (gameTime - buyTime.Value).Days;

            OnTradeCompleted?.Invoke(new TradeRecord
            {
                Id = order.Id,
                Symbol = order.Symbol,
                Sector = "", // Will be filled by the subscriber
                Side = order.Side == OrderSide.Sell ? "Long" : "Short",
                EntryPrice = entryPrice,
                ExitPrice = fillPrice,
                Quantity = fillQuantity,
                PnL = Math.Round(pnl, 2),
                PnLPercent = pnlPct,
                Commission = commission,
                EntryTime = buyTime ?? gameTime,
                ExitTime = gameTime,
                HoldingDays = holdingDays,
            });
        }

        // OCO: Cancel paired order when this one fills
        if (order.OCOPairId.HasValue)
        {
            var pair = _portfolio.Orders.FirstOrDefault(o => o.Id == order.OCOPairId.Value && o.IsActive);
            if (pair != null)
            {
                pair.Status = OrderStatus.Cancelled;
                pair.RejectReason = "OCO pair filled";
                _log.Info("OCO pair cancelled", new { filledId = order.Id, cancelledId = pair.Id });
            }
        }
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
    /// <summary>
    /// Realistic market impact using square-root model.
    /// Real formula: slippage ≈ sqrt(orderSize / avgVolume) × spread × multiplier.
    /// Small orders: negligible. Large orders (>10% daily vol): significant (2-5%+).
    /// </summary>
    private decimal CalculateSlippage(decimal orderSize, Stock stock)
    {
        if (stock.AverageVolume <= 0) return 0m;

        var volumeRatio = orderSize / stock.AverageVolume;
        if (volumeRatio < 0.005m) return 0m; // Negligible for tiny orders (<0.5% daily vol)

        // Square-root market impact model (Almgren-Chriss inspired)
        var sqrtImpact = (decimal)Math.Sqrt((double)volumeRatio);

        var spreadFactor = stock.SpreadPercent / 100m;
        spreadFactor = Math.Max(spreadFactor, 0.001m);

        // Multiplier: 2x for realistic impact (real markets are harsh on large orders)
        var slippage = sqrtImpact * spreadFactor * 2.0m;

        // Cap at 10% (even mega orders can't move more than this in one fill)
        return Math.Min(slippage, 0.10m);
    }

    private static Order CreateOrder(
        string symbol, OrderSide side, OrderType type, decimal quantity,
        DateTime gameTime, decimal? limitPrice, TimeInForce timeInForce,
        decimal? stopPrice = null, decimal? trailAmount = null)
    {
        return new Order(symbol, side, type, quantity, gameTime, limitPrice, timeInForce, stopPrice, trailAmount);
    }
}
