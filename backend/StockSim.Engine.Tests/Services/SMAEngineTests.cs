using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

public class SMAEngineTests
{
    private readonly DateTime _baseTime = new(2027, 3, 10, 16, 0, 0); // Market close
    private readonly SMAEngine _sma = new(42);

    private static Stock MakeStock(string symbol, decimal price, long avgVolume = 100_000, long sharesOutstanding = 10_000_000)
    {
        var s = new Stock(symbol, $"{symbol} Inc", "Technology")
        {
            CurrentPrice = price,
            PreviousClose = price,
            AverageVolume = avgVolume,
            SharesOutstanding = sharesOutstanding,
        };
        return s;
    }

    private static Portfolio MakePortfolio(decimal cash = 100_000m) => new(cash);

    private Dictionary<string, Stock> MakeStockDict(params Stock[] stocks)
    {
        var dict = new Dictionary<string, Stock>();
        foreach (var s in stocks) dict[s.Symbol] = s;
        return dict;
    }

    // ========================
    // Suspicion Score Basics
    // ========================

    [Fact]
    public void InitialScore_ShouldBeZero()
    {
        Assert.Equal(0, _sma.State.SuspicionScore);
        Assert.Equal(RegulatoryStatus.Clear, _sma.State.Status);
    }

    [Fact]
    public void Status_ShouldReflectScoreThresholds()
    {
        _sma.State.SuspicionScore = 0;
        Assert.Equal(RegulatoryStatus.Clear, _sma.State.Status);

        _sma.State.SuspicionScore = 10;
        Assert.Equal(RegulatoryStatus.Clear, _sma.State.Status);

        _sma.State.SuspicionScore = 11;
        Assert.Equal(RegulatoryStatus.UnderReview, _sma.State.Status);

        _sma.State.SuspicionScore = 30;
        Assert.Equal(RegulatoryStatus.UnderReview, _sma.State.Status);

        _sma.State.SuspicionScore = 31;
        Assert.Equal(RegulatoryStatus.UnderInvestigation, _sma.State.Status);

        _sma.State.SuspicionScore = 60;
        Assert.Equal(RegulatoryStatus.UnderInvestigation, _sma.State.Status);

        _sma.State.SuspicionScore = 61;
        Assert.Equal(RegulatoryStatus.EnforcementPending, _sma.State.Status);
    }

    // ========================
    // Score Decay (Bible 9.2)
    // ========================

    [Fact]
    public void ScoreDecay_ShouldDecrease1Per5CleanDays()
    {
        var sma = new SMAEngine(42);
        sma.State.SuspicionScore = 10;

        var stock = MakeStock("TEST", 100m);
        var dict = MakeStockDict(stock);
        var portfolio = MakePortfolio();

        // Tick 5 clean days
        for (int i = 0; i < 5; i++)
        {
            var time = _baseTime.AddDays(i);
            sma.TickDay(portfolio, new[] { stock }, dict, Array.Empty<GameEvent>(), time);
        }

        Assert.Equal(9, sma.State.SuspicionScore);
    }

    [Fact]
    public void ScoreDecay_ShouldNotGoNegative()
    {
        var sma = new SMAEngine(42);
        sma.State.SuspicionScore = 0;

        var stock = MakeStock("TEST", 100m);
        var dict = MakeStockDict(stock);
        var portfolio = MakePortfolio();

        for (int i = 0; i < 10; i++)
        {
            sma.TickDay(portfolio, new[] { stock }, dict, Array.Empty<GameEvent>(), _baseTime.AddDays(i));
        }

        Assert.Equal(0, sma.State.SuspicionScore);
    }

    // ========================
    // Trading Restrictions (Bible 9.4)
    // ========================

    [Fact]
    public void CanTrade_ShouldReturnFalse_WhenAccountFrozen()
    {
        _sma.State.AccountFrozen = true;
        Assert.False(_sma.State.CanTrade("AAPL", _baseTime));
    }

    [Fact]
    public void CanTrade_ShouldReturnFalse_DuringTradingBan()
    {
        _sma.State.TradingBanUntil = _baseTime.AddDays(30);
        Assert.False(_sma.State.CanTrade("AAPL", _baseTime));
        Assert.True(_sma.State.CanTrade("AAPL", _baseTime.AddDays(31)));
    }

    [Fact]
    public void CanOpenPosition_ShouldReturnFalse_WhenCloseOnlyRestriction()
    {
        _sma.State.TradingRestrictions.Add(new TradingRestriction
        {
            Symbol = "AAPL",
            ExpiresAt = _baseTime.AddDays(90),
            CloseOnly = true,
        });

        Assert.False(_sma.State.CanOpenPosition("AAPL", _baseTime));
        Assert.True(_sma.State.CanOpenPosition("GOOG", _baseTime)); // Other symbols fine
        Assert.True(_sma.State.CanOpenPosition("AAPL", _baseTime.AddDays(91))); // After expiry
    }

    [Fact]
    public void IsShortSellingBanned_ShouldWork()
    {
        Assert.False(_sma.State.IsShortSellingBanned(_baseTime));
        _sma.State.ShortSellingBanUntil = _baseTime.AddDays(180);
        Assert.True(_sma.State.IsShortSellingBanned(_baseTime));
        Assert.False(_sma.State.IsShortSellingBanned(_baseTime.AddDays(181)));
    }

    [Fact]
    public void IsMarginBanned_ShouldWork()
    {
        Assert.False(_sma.State.IsMarginBanned(_baseTime));
        _sma.State.MarginBanUntil = _baseTime.AddDays(180);
        Assert.True(_sma.State.IsMarginBanned(_baseTime));
        Assert.False(_sma.State.IsMarginBanned(_baseTime.AddDays(181)));
    }

    // ========================
    // Wash Trading Detection (Bible 9.3.4)
    // ========================

    [Fact]
    public void WashTrading_ShouldDetect_WhenBuySellSameStockWithin5Min()
    {
        // Use a seed that produces detection (detection chance 40-70%)
        // We need to try multiple seeds to find one that detects
        bool detected = false;
        for (int seed = 0; seed < 50; seed++)
        {
            var sma = new SMAEngine(seed);
            var stock = MakeStock("TEST", 50m);
            var dict = MakeStockDict(stock);
            var portfolio = MakePortfolio();

            // Create 4 wash trades (buy+sell within 5 min, same day) — threshold is 3
            var baseTime = _baseTime;
            for (int i = 0; i < 4; i++)
            {
                var buyTime = baseTime.AddMinutes(-60 + i * 10);
                var sellTime = buyTime.AddMinutes(2);
                sma.RecordOrder("TEST", OrderSide.Buy, 100, 50m, buyTime, true);
                sma.RecordOrder("TEST", OrderSide.Sell, 100, 50.10m, sellTime, true);
            }

            sma.TickDay(portfolio, new[] { stock }, dict, Array.Empty<GameEvent>(), baseTime);

            if (sma.State.Violations.Any(v => v.Type == ViolationType.WashTrading))
            {
                detected = true;
                Assert.True(sma.State.SuspicionScore >= 8);
                break;
            }
        }

        Assert.True(detected, "Wash trading should be detected with at least one seed");
    }

    [Fact]
    public void WashTrading_ShouldNotDetect_WithFewerThan3Pairs()
    {
        var sma = new SMAEngine(42);
        var stock = MakeStock("TEST", 50m);
        var dict = MakeStockDict(stock);
        var portfolio = MakePortfolio();

        // Only 2 wash trades — below threshold
        for (int i = 0; i < 2; i++)
        {
            var buyTime = _baseTime.AddMinutes(-30 + i * 10);
            var sellTime = buyTime.AddMinutes(2);
            sma.RecordOrder("TEST", OrderSide.Buy, 100, 50m, buyTime, true);
            sma.RecordOrder("TEST", OrderSide.Sell, 100, 50m, sellTime, true);
        }

        sma.TickDay(portfolio, new[] { stock }, dict, Array.Empty<GameEvent>(), _baseTime);

        Assert.Empty(sma.State.Violations);
    }

    // ========================
    // Spoofing Detection (Bible 9.3.3)
    // ========================

    [Fact]
    public void Spoofing_ShouldDetect_WhenLargeOrdersCancelledQuickly()
    {
        bool detected = false;
        for (int seed = 0; seed < 50; seed++)
        {
            var sma = new SMAEngine(seed);
            var stock = MakeStock("TEST", 100m);
            var dict = MakeStockDict(stock);
            var portfolio = MakePortfolio();

            // Place some normal small orders for baseline (avg qty ~10)
            for (int i = 0; i < 5; i++)
            {
                sma.RecordOrder("TEST", OrderSide.Buy, 10, 100m, _baseTime.AddMinutes(-60 + i), true);
            }

            // Place 3 large orders (>5x average of 10 = >50 shares) and cancel them within 10 min
            for (int i = 0; i < 3; i++)
            {
                var placedAt = _baseTime.AddMinutes(-30 + i * 5);
                sma.RecordOrder("TEST", OrderSide.Buy, 200, 95m, placedAt, false);
                sma.RecordCancellation("TEST", 200, 95m, placedAt, placedAt.AddMinutes(5));
            }

            sma.TickDay(portfolio, new[] { stock }, dict, Array.Empty<GameEvent>(), _baseTime);

            if (sma.State.Violations.Any(v => v.Type == ViolationType.Spoofing))
            {
                detected = true;
                Assert.Equal(10, sma.State.SuspicionScore);
                break;
            }
        }

        Assert.True(detected, "Spoofing should be detected with at least one seed");
    }

    // ========================
    // Pump & Dump Detection (Bible 9.3.2)
    // ========================

    [Fact]
    public void PumpAndDump_ShouldDetect_WhenBuyLargeVolumeThenSellAfterRise()
    {
        bool detected = false;
        for (int seed = 0; seed < 100; seed++)
        {
            var sma = new SMAEngine(seed);
            var stock = MakeStock("MICR", 10m, avgVolume: 50_000, sharesOutstanding: 1_000_000);
            stock.CurrentPrice = 13m; // Price rose after buys
            var dict = MakeStockDict(stock);
            var portfolio = MakePortfolio();

            // Buy >10% of daily volume = >5000 shares
            sma.RecordOrder("MICR", OrderSide.Buy, 6000, 10m, _baseTime.AddDays(-3), true);

            // Sell within 5 days at higher price
            sma.RecordOrder("MICR", OrderSide.Sell, 6000, 13m, _baseTime.AddDays(-1), true);

            sma.TickDay(portfolio, new[] { stock }, dict, Array.Empty<GameEvent>(), _baseTime);

            if (sma.State.Violations.Any(v => v.Type == ViolationType.PumpAndDump))
            {
                detected = true;
                Assert.True(sma.State.SuspicionScore >= 15);
                break;
            }
        }

        Assert.True(detected, "Pump & dump should be detected with at least one seed");
    }

    [Fact]
    public void PumpAndDump_ShouldNotDetect_WhenPlayerHolds()
    {
        var sma = new SMAEngine(42);
        var stock = MakeStock("MICR", 13m, avgVolume: 50_000);
        var dict = MakeStockDict(stock);
        var portfolio = MakePortfolio();

        // Buy a lot but DON'T sell = not a pump & dump
        sma.RecordOrder("MICR", OrderSide.Buy, 6000, 10m, _baseTime.AddDays(-3), true);

        sma.TickDay(portfolio, new[] { stock }, dict, Array.Empty<GameEvent>(), _baseTime);

        Assert.Empty(sma.State.Violations);
    }

    // ========================
    // Bear Raid Detection (Bible 9.3.7)
    // ========================

    [Fact]
    public void BearRaid_ShouldDetect_WhenAggressiveShortCausesPriceDrop()
    {
        bool detected = false;
        for (int seed = 0; seed < 100; seed++)
        {
            var sma = new SMAEngine(seed);
            var stock = MakeStock("WEAK", 40m, avgVolume: 100_000, sharesOutstanding: 5_000_000);
            stock.DayHigh = 50m;
            stock.DayLow = 40m;
            stock.PreviousClose = 50m; // -20% today
            stock.CurrentPrice = 40m;
            var dict = MakeStockDict(stock);
            var portfolio = MakePortfolio();

            // Add short position (>5% of float)
            var floatShares = stock.Float;
            var shortShares = (int)(floatShares * 0.06m);
            portfolio.Positions["WEAK"] = new Position("WEAK", -shortShares, 48m);

            // Record the short orders placed today
            sma.RecordOrder("WEAK", OrderSide.Short, shortShares, 48m, _baseTime.AddMinutes(-60), true);

            sma.TickDay(portfolio, new[] { stock }, dict, Array.Empty<GameEvent>(), _baseTime);

            if (sma.State.Violations.Any(v => v.Type == ViolationType.BearRaid))
            {
                detected = true;
                Assert.True(sma.State.SuspicionScore >= 12);
                break;
            }
        }

        Assert.True(detected, "Bear raid should be detected with at least one seed");
    }

    // ========================
    // Cornering Detection (Bible 9.3.5)
    // ========================

    [Fact]
    public void Cornering_JustHolding_ShouldNotTrigger()
    {
        var sma = new SMAEngine(42);
        var stock = MakeStock("TINY", 10m, sharesOutstanding: 1_000_000);
        var dict = MakeStockDict(stock);
        var portfolio = MakePortfolio();

        // Own >20% of float but don't sell
        var floatShares = stock.Float;
        portfolio.Positions["TINY"] = new Position("TINY", (int)(floatShares * 0.25m), 8m);

        sma.TickDay(portfolio, new[] { stock }, dict, Array.Empty<GameEvent>(), _baseTime);

        // No violation because just holding is legal
        Assert.Empty(sma.State.Violations);
    }

    // ========================
    // Front Running (Bible 9.3.6) — should NOT trigger
    // ========================

    [Fact]
    public void FrontRunning_ShouldBeConsideredLegal()
    {
        var sma = new SMAEngine(42);
        var stock = MakeStock("INST", 100m);
        var dict = MakeStockDict(stock);
        var portfolio = MakePortfolio();

        // Player buys before institutional volume — this is legal observation
        sma.RecordOrder("INST", OrderSide.Buy, 500, 100m, _baseTime.AddDays(-1), true);
        sma.RecordOrder("INST", OrderSide.Sell, 500, 110m, _baseTime, true);

        sma.TickDay(portfolio, new[] { stock }, dict, Array.Empty<GameEvent>(), _baseTime);

        // No front-running violation should exist (it's legal per Bible 9.3.6)
        Assert.DoesNotContain(sma.State.Violations, v => v.Description.Contains("front running", StringComparison.OrdinalIgnoreCase));
    }

    // ========================
    // Investigation Lifecycle (Bible 9.4.2)
    // ========================

    [Fact]
    public void Investigation_ShouldStartAtScore60()
    {
        var sma = new SMAEngine(42);
        sma.State.SuspicionScore = 65;

        // Add a violation to investigate
        sma.State.Violations.Add(new SMAViolation
        {
            Id = 1,
            Type = ViolationType.WashTrading,
            Symbol = "TEST",
            DetectedAt = _baseTime.AddDays(-1),
            ScoreImpact = 8,
            Description = "Test violation",
        });

        var stock = MakeStock("TEST", 50m);
        var dict = MakeStockDict(stock);
        var portfolio = MakePortfolio();

        sma.TickDay(portfolio, new[] { stock }, dict, Array.Empty<GameEvent>(), _baseTime);

        Assert.Single(sma.State.Investigations);
        Assert.False(sma.State.Investigations[0].IsResolved);
        Assert.True(sma.NotificationsThisTick.Any(n => n.Type == SMANotificationType.Investigation));
    }

    [Fact]
    public void Investigation_ShouldAddTradingRestriction()
    {
        var sma = new SMAEngine(42);
        sma.State.SuspicionScore = 65;
        sma.State.Violations.Add(new SMAViolation
        {
            Id = 1,
            Type = ViolationType.PumpAndDump,
            Symbol = "PUMP",
            DetectedAt = _baseTime.AddDays(-1),
            ScoreImpact = 20,
            EstimatedProfit = 50000m,
            Description = "Test",
        });

        var stock = MakeStock("PUMP", 50m);
        var dict = MakeStockDict(stock);
        var portfolio = MakePortfolio();

        sma.TickDay(portfolio, new[] { stock }, dict, Array.Empty<GameEvent>(), _baseTime);

        // Should have a close-only restriction on PUMP
        var restriction = sma.State.TradingRestrictions.Find(r => r.Symbol == "PUMP");
        Assert.NotNull(restriction);
        Assert.True(restriction!.CloseOnly);
    }

    // ========================
    // Notification System (Bible 9.2)
    // ========================

    [Fact]
    public void Notification_ShouldPauseGame_ForInvestigations()
    {
        var sma = new SMAEngine(42);
        sma.State.SuspicionScore = 65;
        sma.State.Violations.Add(new SMAViolation
        {
            Id = 1,
            Type = ViolationType.InsiderTrading,
            Symbol = "SEC",
            DetectedAt = _baseTime,
            ScoreImpact = 15,
            Description = "Test",
        });

        var stock = MakeStock("SEC", 100m);
        sma.TickDay(MakePortfolio(), new[] { stock }, MakeStockDict(stock), Array.Empty<GameEvent>(), _baseTime);

        var pauseNotifs = sma.NotificationsThisTick.Where(n => n.PauseGame).ToList();
        Assert.NotEmpty(pauseNotifs);
    }

    // ========================
    // SMAState Helpers
    // ========================

    [Fact]
    public void Restrictions_ShouldExpire()
    {
        var sma = new SMAEngine(42);
        sma.State.TradingRestrictions.Add(new TradingRestriction
        {
            Symbol = "OLD",
            ExpiresAt = _baseTime.AddDays(-1),
            CloseOnly = true,
        });
        sma.State.TradingBanUntil = _baseTime.AddDays(-1);
        sma.State.MarginBanUntil = _baseTime.AddDays(-1);

        var stock = MakeStock("OLD", 100m);
        sma.TickDay(MakePortfolio(), new[] { stock }, MakeStockDict(stock), Array.Empty<GameEvent>(), _baseTime);

        Assert.Empty(sma.State.TradingRestrictions);
        Assert.Null(sma.State.TradingBanUntil);
        Assert.Null(sma.State.MarginBanUntil);
    }

    [Fact]
    public void RecordOrder_ShouldStoreForDetection()
    {
        _sma.RecordOrder("AAPL", OrderSide.Buy, 100, 150m, _baseTime, true);

        Assert.Single(_sma.State.RecentOrders);
        Assert.Equal("AAPL", _sma.State.RecentOrders[0].Symbol);
        Assert.Equal(100m, _sma.State.RecentOrders[0].Quantity);
    }

    [Fact]
    public void RecordCancellation_ShouldStoreForDetection()
    {
        _sma.RecordCancellation("AAPL", 500, 150m, _baseTime.AddMinutes(-5), _baseTime);

        Assert.Single(_sma.State.RecentCancellations);
        Assert.Equal("AAPL", _sma.State.RecentCancellations[0].Symbol);
    }

    [Fact]
    public void OldRecords_ShouldBePruned()
    {
        var sma = new SMAEngine(42);
        // Add old order (>10 days ago)
        sma.RecordOrder("OLD", OrderSide.Buy, 100, 50m, _baseTime.AddDays(-15), true);
        // Add recent order
        sma.RecordOrder("NEW", OrderSide.Buy, 100, 50m, _baseTime.AddDays(-1), true);

        var stock = MakeStock("TEST", 100m);
        sma.TickDay(MakePortfolio(), new[] { stock }, MakeStockDict(stock), Array.Empty<GameEvent>(), _baseTime);

        Assert.Single(sma.State.RecentOrders);
        Assert.Equal("NEW", sma.State.RecentOrders[0].Symbol);
    }
}
