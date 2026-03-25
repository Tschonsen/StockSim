using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// StockSim Market Authority — regulatory engine that monitors player trading.
/// Bible 9.2-9.5: suspicion score, detection algorithms, investigations, penalties.
///
/// Design: the player CAN do illegal things, but risks getting caught.
/// Detection is probabilistic — skilled players can be subtle enough to avoid detection.
/// </summary>
public class SMAEngine
{
    private readonly Logger _log = new("SMAEngine");
    private readonly Random _rng;

    public SMAState State { get; set; } = new();

    /// <summary>Whether SMA enforcement is enabled. Can be toggled via Settings (Bible 16.3).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>New events this tick for frontend notifications.</summary>
    public List<SMANotification> NotificationsThisTick { get; } = new();

    // Detection tracking (not persisted — rebuilt from recent orders)
    private DateTime _lastDayProcessed;

    public SMAEngine(int seed)
    {
        _rng = new Random(seed);
    }

    /// <summary>
    /// Called once per game day (at market close) to run detection algorithms.
    /// Bible 9.2: the SMA analyzes trading patterns over time.
    /// </summary>
    public void TickDay(
        Portfolio portfolio,
        IReadOnlyList<Stock> stocks,
        Dictionary<string, Stock> stocksBySymbol,
        IReadOnlyList<GameEvent> recentEvents,
        DateTime gameTime)
    {
        NotificationsThisTick.Clear();

        if (!Enabled) return; // Settings: SMA enforcement disabled
        if (State.AccountFrozen) return;

        // Prune old tracking data (keep 10 game days)
        PruneOldRecords(gameTime);

        // Run detection algorithms
        DetectInsiderTrading(portfolio, stocksBySymbol, recentEvents, gameTime);
        DetectPumpAndDump(portfolio, stocksBySymbol, gameTime);
        DetectSpoofing(gameTime);
        DetectWashTrading(gameTime);
        DetectCornering(portfolio, stocksBySymbol, gameTime);
        DetectBearRaid(portfolio, stocksBySymbol, gameTime);

        // Process active investigations
        ProcessInvestigations(portfolio, stocksBySymbol, gameTime);

        // Expire trading restrictions
        ExpireRestrictions(gameTime);

        // Score decay: -1 per 5 clean days (Bible 9.2)
        ProcessScoreDecay(gameTime);

        // Generate status notifications based on score thresholds (Bible 9.2)
        GenerateStatusNotifications(gameTime);

        _lastDayProcessed = gameTime.Date;
    }

    /// <summary>
    /// Record a player order for SMA tracking. Called from OrderEngine.
    /// </summary>
    public void RecordOrder(string symbol, OrderSide side, decimal quantity, decimal price, DateTime time, bool filled)
    {
        State.RecentOrders.Add(new SMAOrderRecord
        {
            Symbol = symbol,
            Side = side,
            Quantity = quantity,
            Price = price,
            Time = time,
            IsFilled = filled,
        });
    }

    /// <summary>
    /// Record a cancelled order for spoofing detection. Called from OrderEngine.
    /// </summary>
    public void RecordCancellation(string symbol, decimal quantity, decimal price, DateTime placedAt, DateTime cancelledAt)
    {
        State.RecentCancellations.Add(new SMACancellationRecord
        {
            Symbol = symbol,
            Quantity = quantity,
            Price = price,
            PlacedAt = placedAt,
            CancelledAt = cancelledAt,
        });
    }

    // ========================
    // Detection Algorithms
    // ========================

    /// <summary>
    /// Bible 9.3.1: Insider Trading detection.
    /// Trigger: player buys/sells BEFORE event, profits > $1,000.
    /// Detection probability: 15-40% based on trade size.
    /// Score impact: +10 to +25.
    /// </summary>
    private void DetectInsiderTrading(
        Portfolio portfolio,
        Dictionary<string, Stock> stocksBySymbol,
        IReadOnlyList<GameEvent> recentEvents,
        DateTime gameTime)
    {
        // Look at events that happened in the last 3 days
        var recentCompanyEvents = recentEvents
            .Where(e => (gameTime - e.TriggeredAt).TotalDays <= 3 && e.PriceEffect != 0 && e.AffectedSymbols.Count > 0)
            .ToList();

        // Build symbol → events map
        var eventsBySymbol = new Dictionary<string, List<GameEvent>>();
        foreach (var evt in recentCompanyEvents)
        {
            foreach (var sym in evt.AffectedSymbols)
            {
                if (!eventsBySymbol.ContainsKey(sym))
                    eventsBySymbol[sym] = new();
                eventsBySymbol[sym].Add(evt);
            }
        }

        foreach (var (symbol, events) in eventsBySymbol)
        {
            if (string.IsNullOrEmpty(symbol)) continue;

            // Check if player had trades in this symbol BEFORE the event
            foreach (var evt in events)
            {
                var preEventTrades = State.RecentOrders
                    .Where(o => o.Symbol == symbol
                        && o.IsFilled
                        && o.Time < evt.TriggeredAt
                        && (evt.TriggeredAt - o.Time).TotalDays <= 3)
                    .ToList();

                if (preEventTrades.Count == 0) continue;

                // Check if the player profited
                if (!stocksBySymbol.TryGetValue(symbol, out var stock)) continue;

                // Estimate profit: did the player buy before a positive event or sell/short before negative?
                decimal estimatedProfit = 0;
                foreach (var trade in preEventTrades)
                {
                    if (evt.PriceEffect > 0 && (trade.Side == OrderSide.Buy || trade.Side == OrderSide.Cover))
                    {
                        // Bought before positive event
                        estimatedProfit += trade.Quantity * (stock.CurrentPrice - trade.Price);
                    }
                    else if (evt.PriceEffect < 0 && (trade.Side == OrderSide.Sell || trade.Side == OrderSide.Short))
                    {
                        // Sold/shorted before negative event
                        estimatedProfit += trade.Quantity * (trade.Price - stock.CurrentPrice);
                    }
                }

                if (estimatedProfit <= 1000) continue;

                // Detection probability: 15-40% based on trade size
                var totalQty = preEventTrades.Sum(t => t.Quantity);
                var detectionChance = 0.15 + Math.Min(0.25, (double)(totalQty / stock.AverageVolume) * 0.5);

                if (_rng.NextDouble() >= detectionChance) continue;

                // Caught!
                var scoreImpact = (int)Math.Clamp(10 + estimatedProfit / 2000, 10, 25);
                AddViolation(ViolationType.InsiderTrading, symbol, estimatedProfit, scoreImpact,
                    $"Pre-event trading in {symbol} yielded estimated profit of {estimatedProfit:C0}",
                    gameTime);
            }
        }
    }

    /// <summary>
    /// Bible 9.3.2: Pump & Dump detection.
    /// Trigger: buy >10% daily volume, price rises >15%, sell within 5 days.
    /// Detection: 20-50%.
    /// Score: +15 to +30.
    /// </summary>
    private void DetectPumpAndDump(
        Portfolio portfolio,
        Dictionary<string, Stock> stocksBySymbol,
        DateTime gameTime)
    {
        // Group recent orders by symbol to find buy-then-sell patterns
        var ordersBySymbol = State.RecentOrders
            .Where(o => o.IsFilled && (gameTime - o.Time).TotalDays <= 5)
            .GroupBy(o => o.Symbol);

        foreach (var group in ordersBySymbol)
        {
            var symbol = group.Key;
            if (!stocksBySymbol.TryGetValue(symbol, out var stock)) continue;

            var buys = group.Where(o => o.Side == OrderSide.Buy).OrderBy(o => o.Time).ToList();
            var sells = group.Where(o => o.Side == OrderSide.Sell).OrderBy(o => o.Time).ToList();

            if (buys.Count == 0 || sells.Count == 0) continue;

            var totalBuyQty = buys.Sum(b => b.Quantity);
            var totalSellQty = sells.Sum(s => s.Quantity);

            // Must have bought AND sold significant amounts
            if (totalBuyQty < 100 || totalSellQty < totalBuyQty * 0.5m) continue;

            // Check if buys were >10% of daily volume
            if (stock.AverageVolume > 0 && totalBuyQty < stock.AverageVolume * 0.10m) continue;

            // Check if price rose >15% during the buy period
            var avgBuyPrice = buys.Sum(b => b.Price * b.Quantity) / totalBuyQty;
            var avgSellPrice = sells.Sum(s => s.Price * s.Quantity) / totalSellQty;
            var priceRise = avgBuyPrice > 0 ? (avgSellPrice - avgBuyPrice) / avgBuyPrice : 0;

            if (priceRise < 0.15m) continue;

            // Check sell happened within 5 days of last buy
            var lastBuy = buys.Last().Time;
            var firstSell = sells.First().Time;
            if ((firstSell - lastBuy).TotalDays > 5) continue;

            var profit = (avgSellPrice - avgBuyPrice) * Math.Min(totalBuyQty, totalSellQty);
            if (profit <= 0) continue;

            // Detection: 20-50% (small caps watched more)
            var isSmallCap = stock.MarketCap < 2_000_000_000m;
            var detectionChance = isSmallCap ? 0.35 : 0.20;
            detectionChance += Math.Min(0.15, (double)(totalBuyQty / Math.Max(1, stock.AverageVolume)) * 0.3);

            if (_rng.NextDouble() >= detectionChance) continue;

            var scoreImpact = (int)Math.Clamp(15 + profit / 3000, 15, 30);
            AddViolation(ViolationType.PumpAndDump, symbol, profit, scoreImpact,
                $"Suspected pump and dump in {symbol}: bought {totalBuyQty:N0} shares, price rose {priceRise:P0}, sold within {(firstSell - lastBuy).TotalDays:N0} days",
                gameTime);
        }
    }

    /// <summary>
    /// Bible 9.3.3: Spoofing detection.
    /// Trigger: cancel >80% of large orders within 10 min.
    /// Detection: 25-60%.
    /// Score: +10 per incident.
    /// </summary>
    private void DetectSpoofing(DateTime gameTime)
    {
        // Look at recent cancellations (last trading day)
        var todayCancels = State.RecentCancellations
            .Where(c => (gameTime - c.CancelledAt).TotalDays <= 1)
            .ToList();

        if (todayCancels.Count < 3) return;

        // Group by symbol
        var bySymbol = todayCancels.GroupBy(c => c.Symbol);

        foreach (var group in bySymbol)
        {
            var symbol = group.Key;
            var cancels = group.ToList();

            // Count total orders for this symbol today
            var todayOrders = State.RecentOrders
                .Where(o => o.Symbol == symbol && (gameTime - o.Time).TotalDays <= 1)
                .ToList();

            if (todayOrders.Count == 0) continue;

            // Avg based on filled orders only (to avoid skewing by the spoof orders themselves)
            var filledOrders = todayOrders.Where(o => o.IsFilled).ToList();
            var avgQty = filledOrders.Count > 0
                ? filledOrders.Average(o => (double)o.Quantity)
                : todayOrders.Average(o => (double)o.Quantity);

            // "Large" = >5x average filled order size
            var largeThreshold = avgQty * 5;
            var largeOrders = todayOrders.Count(o => (double)o.Quantity > largeThreshold);
            var largeCancels = cancels.Count(c =>
                (double)c.Quantity > largeThreshold
                && (c.CancelledAt - c.PlacedAt).TotalMinutes <= 10);

            if (largeOrders < 2 || largeCancels == 0) continue;

            var cancelRate = (double)largeCancels / Math.Max(1, largeOrders);
            if (cancelRate < 0.80) continue;

            // Detection: 25-60%
            var detectionChance = 0.25 + cancelRate * 0.35;
            if (_rng.NextDouble() >= detectionChance) continue;

            AddViolation(ViolationType.Spoofing, symbol, 0, 10,
                $"Suspected spoofing in {symbol}: {largeCancels} large orders cancelled within 10 minutes ({cancelRate:P0} cancel rate)",
                gameTime);
        }
    }

    /// <summary>
    /// Bible 9.3.4: Wash Trading detection.
    /// Trigger: buy+sell same stock within 5 min, 3+ times/day.
    /// Detection: 40-70%.
    /// Score: +8 per incident.
    /// </summary>
    private void DetectWashTrading(DateTime gameTime)
    {
        var todayOrders = State.RecentOrders
            .Where(o => o.IsFilled && o.Time.Date == gameTime.Date)
            .GroupBy(o => o.Symbol);

        foreach (var group in todayOrders)
        {
            var symbol = group.Key;
            var orders = group.OrderBy(o => o.Time).ToList();

            // Find buy-sell pairs within 5 minutes
            int washCount = 0;
            for (int i = 0; i < orders.Count - 1; i++)
            {
                for (int j = i + 1; j < orders.Count; j++)
                {
                    var a = orders[i];
                    var b = orders[j];

                    if ((b.Time - a.Time).TotalMinutes > 5) break;

                    // Opposite sides?
                    bool isWash = (a.Side == OrderSide.Buy && b.Side == OrderSide.Sell)
                               || (a.Side == OrderSide.Sell && b.Side == OrderSide.Buy)
                               || (a.Side == OrderSide.Short && b.Side == OrderSide.Cover)
                               || (a.Side == OrderSide.Cover && b.Side == OrderSide.Short);

                    if (isWash && Math.Abs(a.Quantity - b.Quantity) < a.Quantity * 0.2m)
                    {
                        washCount++;
                    }
                }
            }

            if (washCount < 3) continue;

            // Detection: 40-70%
            var detectionChance = 0.40 + Math.Min(0.30, washCount * 0.05);
            if (_rng.NextDouble() >= detectionChance) continue;

            AddViolation(ViolationType.WashTrading, symbol, 0, 8,
                $"Suspected wash trading in {symbol}: {washCount} round-trip trades within 5 minutes",
                gameTime);
        }
    }

    /// <summary>
    /// Bible 9.3.5: Cornering the Market detection.
    /// Legal to hold large positions, illegal to manipulate price with them.
    /// Score: +20 only if manipulation detected.
    /// </summary>
    private void DetectCornering(
        Portfolio portfolio,
        Dictionary<string, Stock> stocksBySymbol,
        DateTime gameTime)
    {
        foreach (var (symbol, position) in portfolio.Positions)
        {
            if (position.Shares <= 0) continue;
            if (!stocksBySymbol.TryGetValue(symbol, out var stock)) continue;
            if (stock.Float <= 0) continue;

            var ownershipPct = (decimal)position.Shares / stock.Float;

            // >5% = public filing (legal, just a news event)
            // >20% = SMA attention if there's manipulation
            if (ownershipPct < 0.20m) continue;

            // Check for manipulation: did the player sell after building position?
            var recentSells = State.RecentOrders
                .Where(o => o.Symbol == symbol && o.IsFilled
                    && (o.Side == OrderSide.Sell)
                    && (gameTime - o.Time).TotalDays <= 5)
                .ToList();

            if (recentSells.Count == 0) continue; // Just holding = legal

            var sellQty = recentSells.Sum(s => s.Quantity);
            var avgSellPrice = recentSells.Sum(s => s.Price * s.Quantity) / sellQty;

            // If they sold significant amount after price rose, that's manipulation
            if (avgSellPrice <= position.AverageCost * 1.10m) continue;
            if (sellQty < position.Shares * 0.1m) continue;

            var profit = (avgSellPrice - position.AverageCost) * sellQty;

            if (_rng.NextDouble() >= 0.30) continue;

            AddViolation(ViolationType.Cornering, symbol, profit, 20,
                $"Market manipulation suspected: {ownershipPct:P1} ownership of {symbol} float with profitable selling pattern",
                gameTime);
        }
    }

    /// <summary>
    /// Bible 9.3.7: Bear Raid detection.
    /// Trigger: large short >5% short interest + price drops >10% same day.
    /// Detection: 20-40%.
    /// Score: +12 to +20.
    /// </summary>
    private void DetectBearRaid(
        Portfolio portfolio,
        Dictionary<string, Stock> stocksBySymbol,
        DateTime gameTime)
    {
        foreach (var (symbol, position) in portfolio.Positions)
        {
            if (position.Shares >= 0) continue; // Only shorts
            if (!stocksBySymbol.TryGetValue(symbol, out var stock)) continue;

            // Check if player's short is >5% of short interest equiv
            var shortShares = Math.Abs(position.Shares);
            if (stock.Float <= 0) continue;
            var shortPct = (decimal)shortShares / stock.Float;
            if (shortPct < 0.05m) continue;

            // Check if stock dropped >10% today
            if (stock.DayChangePercent > -10) continue;

            // Were shorts opened today?
            var todayShorts = State.RecentOrders
                .Where(o => o.Symbol == symbol && o.IsFilled
                    && o.Side == OrderSide.Short
                    && o.Time.Date == gameTime.Date)
                .ToList();

            if (todayShorts.Count == 0) continue;

            var profit = todayShorts.Sum(s => s.Quantity * (s.Price - stock.CurrentPrice));
            if (profit <= 0) continue;

            var detectionChance = 0.20 + Math.Min(0.20, (double)shortPct * 2);
            if (_rng.NextDouble() >= detectionChance) continue;

            var scoreImpact = (int)Math.Clamp(12 + profit / 2000, 12, 20);
            AddViolation(ViolationType.BearRaid, symbol, profit, scoreImpact,
                $"Suspected bear raid on {symbol}: {shortPct:P1} short interest, stock down {stock.DayChangePercent:N1}% today",
                gameTime);
        }
    }

    // ========================
    // Investigation & Penalty System
    // ========================

    /// <summary>
    /// Process active investigations: advance days, resolve when complete.
    /// Bible 9.4.2: 30-60 day duration, 20% acquittal if score drops, 80% penalty.
    /// </summary>
    private void ProcessInvestigations(
        Portfolio portfolio,
        Dictionary<string, Stock> stocksBySymbol,
        DateTime gameTime)
    {
        foreach (var inv in State.Investigations.Where(i => !i.IsResolved))
        {
            inv.DaysElapsed++;

            if (inv.DaysElapsed < inv.DurationDays) continue;

            inv.IsResolved = true;

            // Acquittal chance: 20% base, higher if score has dropped
            var acquittalChance = State.SuspicionScore < 40 ? 0.40 : 0.20;
            if (_rng.NextDouble() < acquittalChance)
            {
                inv.Acquitted = true;
                State.SuspicionScore = Math.Max(0, State.SuspicionScore - 10);
                NotificationsThisTick.Add(new SMANotification
                {
                    Type = SMANotificationType.Acquittal,
                    Title = "SMA Investigation Closed",
                    Message = $"The SMA has concluded its investigation into your {inv.Type} activity in {inv.Symbol}. No further action will be taken.",
                    Severity = "info",
                    Time = gameTime,
                });
                _log.Info("Investigation acquitted", new { type = inv.Type, symbol = inv.Symbol });
                continue;
            }

            // Penalty!
            ImposePenalty(inv, portfolio, stocksBySymbol, gameTime);
        }

        // Start new investigations if score warrants it (Bible 9.4.2: score 60+)
        if (State.SuspicionScore >= 60)
        {
            var uninvestigated = State.Violations
                .Where(v => !State.Investigations.Any(i => i.Type == v.Type && i.Symbol == v.Symbol && !i.IsResolved))
                .OrderByDescending(v => v.DetectedAt)
                .FirstOrDefault();

            if (uninvestigated != null
                && !State.Investigations.Any(i => !i.IsResolved)) // One at a time
            {
                var investigation = new SMAInvestigation
                {
                    Id = State.NextInvestigationId++,
                    Type = uninvestigated.Type,
                    Symbol = uninvestigated.Symbol,
                    StartedAt = gameTime,
                    DurationDays = _rng.Next(30, 61),
                };
                State.Investigations.Add(investigation);

                // Trading restriction on investigated symbol (Bible 9.4.2)
                if (!State.TradingRestrictions.Any(r => r.Symbol == uninvestigated.Symbol && r.ExpiresAt > gameTime))
                {
                    State.TradingRestrictions.Add(new TradingRestriction
                    {
                        Symbol = uninvestigated.Symbol,
                        ExpiresAt = gameTime.AddDays(90),
                        CloseOnly = true,
                    });
                }

                NotificationsThisTick.Add(new SMANotification
                {
                    Type = SMANotificationType.Investigation,
                    Title = "SMA INVESTIGATION",
                    Message = $"The StockSim Market Authority has opened a formal investigation into your trading activity in {uninvestigated.Symbol} for suspected {FormatViolationType(uninvestigated.Type)}.",
                    Severity = "critical",
                    Time = gameTime,
                    PauseGame = true,
                });

                _log.Warn("Investigation opened", new { type = uninvestigated.Type, symbol = uninvestigated.Symbol, duration = investigation.DurationDays });
            }
        }
    }

    /// <summary>
    /// Impose penalty after failed investigation. Bible 9.4.3-9.4.5.
    /// </summary>
    private void ImposePenalty(
        SMAInvestigation investigation,
        Portfolio portfolio,
        Dictionary<string, Stock> stocksBySymbol,
        DateTime gameTime)
    {
        // Find the matching violation to estimate profit
        var violation = State.Violations
            .Where(v => v.Type == investigation.Type && v.Symbol == investigation.Symbol)
            .OrderByDescending(v => v.DetectedAt)
            .FirstOrDefault();

        var estimatedProfit = violation?.EstimatedProfit ?? 10000m;
        bool isSevere = State.SuspicionScore >= 80;

        // Fine calculation (Bible 9.4.3)
        decimal fineMultiplier = isSevere ? 3m : 2m;
        decimal minFine = isSevere ? 50000m : 10000m;

        Func<string, decimal> getPrice = sym =>
            stocksBySymbol.TryGetValue(sym, out var s) ? s.CurrentPrice : 0m;
        var portfolioValue = portfolio.TotalEquity(getPrice);
        decimal maxFine = Math.Max(500000m, portfolioValue * 0.20m);

        decimal fine = Math.Clamp(estimatedProfit * fineMultiplier, minFine, maxFine);

        int tradingBanDays = isSevere ? 30 : 0;
        int marginBanDays = isSevere ? 180 : 0;

        var penalty = new SMAPenalty
        {
            Id = State.NextPenaltyId++,
            Type = investigation.Type,
            Symbol = investigation.Symbol,
            ImposedAt = gameTime,
            FineAmount = fine,
            TradingBanDays = tradingBanDays,
            MarginBanDays = marginBanDays,
            Description = isSevere
                ? $"Severe enforcement action for {FormatViolationType(investigation.Type)}"
                : $"Fine for {FormatViolationType(investigation.Type)}",
        };
        State.Penalties.Add(penalty);

        // Deduct fine from cash
        portfolio.Cash -= fine;
        _log.Warn("SMA fine imposed", new { fine, type = investigation.Type, symbol = investigation.Symbol });

        // Force sell positions if cash goes negative (Bible 9.4.3)
        if (portfolio.Cash < 0)
        {
            ForceLiquidateForFine(portfolio, stocksBySymbol, gameTime);
        }

        // Trading restrictions
        if (!State.TradingRestrictions.Any(r => r.Symbol == investigation.Symbol && r.ExpiresAt > gameTime))
        {
            State.TradingRestrictions.Add(new TradingRestriction
            {
                Symbol = investigation.Symbol,
                ExpiresAt = gameTime.AddDays(90),
                CloseOnly = true,
            });
        }

        // Severe penalties (Bible 9.4.4)
        if (isSevere)
        {
            State.TradingBanUntil = gameTime.AddDays(tradingBanDays);
            State.MarginBanUntil = gameTime.AddDays(marginBanDays);
            State.EnforcementActionCount++;

            // Check for account freeze (Bible 9.4.5)
            if (State.EnforcementActionCount >= 3)
            {
                FreezeAccount(portfolio, stocksBySymbol, gameTime);
                return;
            }
        }

        var severity = isSevere ? "critical" : "warning";
        var title = isSevere ? "SMA ENFORCEMENT ACTION" : "SMA Fine";
        var msg = isSevere
            ? $"Severe penalties imposed for {FormatViolationType(investigation.Type)} in {investigation.Symbol}. Fine: {fine:C0}. Trading ban: {tradingBanDays} days. Margin revoked for {marginBanDays} days."
            : $"You have been fined {fine:C0} for {FormatViolationType(investigation.Type)} in {investigation.Symbol}.";

        NotificationsThisTick.Add(new SMANotification
        {
            Type = SMANotificationType.Penalty,
            Title = title,
            Message = msg,
            Severity = severity,
            Time = gameTime,
            PauseGame = true,
        });

        State.EnforcementActionCount++;
    }

    /// <summary>
    /// Bible 9.4.5: Account freeze after 3+ enforcement actions.
    /// </summary>
    private void FreezeAccount(
        Portfolio portfolio,
        Dictionary<string, Stock> stocksBySymbol,
        DateTime gameTime)
    {
        State.AccountFrozen = true;

        // Liquidate all positions
        // (Actual liquidation handled by GameLoop — we just flag it)

        // Set harsh restrictions for if the player continues
        State.MarginBanUntil = gameTime.AddDays(360);
        State.ShortSellingBanUntil = gameTime.AddDays(180);
        State.MaxPositionSizePercent = 5m;
        State.PositionSizeLimitUntil = gameTime.AddDays(180);

        NotificationsThisTick.Add(new SMANotification
        {
            Type = SMANotificationType.AccountFreeze,
            Title = "ACCOUNT FROZEN BY SMA",
            Message = "Your account has been frozen by the StockSim Market Authority due to repeated violations. All positions will be liquidated. You may continue with severe restrictions.",
            Severity = "critical",
            Time = gameTime,
            PauseGame = true,
        });

        _log.Error("Account frozen by SMA", new { enforcementActions = State.EnforcementActionCount });
    }

    /// <summary>
    /// Force-sell positions to cover a fine when cash is negative.
    /// </summary>
    private void ForceLiquidateForFine(
        Portfolio portfolio,
        Dictionary<string, Stock> stocksBySymbol,
        DateTime gameTime)
    {
        // Sell smallest/most liquid positions first
        var positionsToSell = portfolio.Positions.Values
            .Where(p => p.Shares > 0)
            .OrderBy(p =>
            {
                var price = stocksBySymbol.TryGetValue(p.Symbol, out var s) ? s.CurrentPrice : 0m;
                return Math.Abs(p.MarketValue(price));
            })
            .ToList();

        foreach (var pos in positionsToSell)
        {
            if (portfolio.Cash >= 0) break;
            if (!stocksBySymbol.TryGetValue(pos.Symbol, out var stock)) continue;

            var value = pos.Shares * stock.CurrentPrice;
            portfolio.Cash += value;
            portfolio.Positions.Remove(pos.Symbol);

            _log.Info("Force-sold position to cover SMA fine", new { symbol = pos.Symbol, shares = pos.Shares, value });
        }
    }

    // ========================
    // Score Management
    // ========================

    private void AddViolation(ViolationType type, string symbol, decimal profit, int scoreImpact, string description, DateTime gameTime)
    {
        var violation = new SMAViolation
        {
            Id = State.NextViolationId++,
            Type = type,
            Symbol = symbol,
            DetectedAt = gameTime,
            EstimatedProfit = profit,
            ScoreImpact = scoreImpact,
            Description = description,
        };
        State.Violations.Add(violation);

        State.SuspicionScore = Math.Min(100, State.SuspicionScore + scoreImpact);
        State.CleanDays = 0;

        _log.Warn("Violation detected", new { type, symbol, scoreImpact, newScore = State.SuspicionScore });

        // Generate notification based on score level (Bible 9.2)
        if (State.SuspicionScore >= 40)
        {
            NotificationsThisTick.Add(new SMANotification
            {
                Type = SMANotificationType.Warning,
                Title = "SMA Notice",
                Message = $"Your trading activity in {symbol} is being reviewed.",
                Severity = "warning",
                Time = gameTime,
            });
        }
    }

    /// <summary>
    /// Bible 9.2: -1 per 5 clean game days without suspicious activity.
    /// No decay during active investigations.
    /// </summary>
    private void ProcessScoreDecay(DateTime gameTime)
    {
        if (State.SuspicionScore <= 0) return;
        if (State.Investigations.Any(i => !i.IsResolved)) return; // No decay during investigation

        State.CleanDays++;
        if (State.CleanDays >= 5)
        {
            State.SuspicionScore = Math.Max(0, State.SuspicionScore - 1);
            State.CleanDays = 0;
            _log.Debug("Score decay", new { newScore = State.SuspicionScore });
        }
    }

    /// <summary>
    /// Generate ambient notifications based on score thresholds. Bible 9.2.
    /// </summary>
    private void GenerateStatusNotifications(DateTime gameTime)
    {
        // Only generate periodically (every 5 game days)
        if ((gameTime - _lastDayProcessed).TotalDays < 5) return;

        if (State.SuspicionScore >= 20 && State.SuspicionScore < 40)
        {
            // Generic surveillance news (not targeted at player)
            if (_rng.NextDouble() < 0.3)
            {
                NotificationsThisTick.Add(new SMANotification
                {
                    Type = SMANotificationType.AmbientNews,
                    Title = "Market News",
                    Message = "SMA reports increased surveillance of unusual trading activity.",
                    Severity = "info",
                    Time = gameTime,
                });
            }
        }
    }

    private void PruneOldRecords(DateTime gameTime)
    {
        var cutoff = gameTime.AddDays(-10);
        State.RecentOrders.RemoveAll(o => o.Time < cutoff);
        State.RecentCancellations.RemoveAll(c => c.CancelledAt < cutoff);
    }

    private void ExpireRestrictions(DateTime gameTime)
    {
        State.TradingRestrictions.RemoveAll(r => r.ExpiresAt <= gameTime);

        if (State.TradingBanUntil.HasValue && gameTime >= State.TradingBanUntil.Value)
            State.TradingBanUntil = null;

        if (State.MarginBanUntil.HasValue && gameTime >= State.MarginBanUntil.Value)
            State.MarginBanUntil = null;

        if (State.ShortSellingBanUntil.HasValue && gameTime >= State.ShortSellingBanUntil.Value)
            State.ShortSellingBanUntil = null;

        if (State.PositionSizeLimitUntil.HasValue && gameTime >= State.PositionSizeLimitUntil.Value)
        {
            State.MaxPositionSizePercent = 0;
            State.PositionSizeLimitUntil = null;
        }
    }

    private static string FormatViolationType(ViolationType type) => type switch
    {
        ViolationType.InsiderTrading => "insider trading",
        ViolationType.PumpAndDump => "market manipulation (pump and dump)",
        ViolationType.Spoofing => "spoofing",
        ViolationType.WashTrading => "wash trading",
        ViolationType.Cornering => "market cornering",
        ViolationType.BearRaid => "bear raid",
        _ => type.ToString(),
    };
}

/// <summary>
/// Notification types for the frontend.
/// </summary>
public enum SMANotificationType
{
    AmbientNews,     // Generic market surveillance news
    Warning,         // Direct warning to player (score 40+)
    Investigation,   // Formal investigation opened (score 60+)
    Penalty,         // Fine or enforcement action
    Acquittal,       // Investigation closed, no penalty
    AccountFreeze,   // Game over scenario
}

/// <summary>
/// A notification to display in the frontend.
/// </summary>
public class SMANotification
{
    public SMANotificationType Type { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Severity { get; set; } = "info"; // info, warning, critical
    public DateTime Time { get; set; }
    public bool PauseGame { get; set; }
}
