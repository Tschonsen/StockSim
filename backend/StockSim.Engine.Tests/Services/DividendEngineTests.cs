using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

public class DividendEngineTests
{
    private readonly DividendEngine _engine = new();

    /// <summary>Helper: compute the staggered announcement day for a stock symbol in a quarter month.</summary>
    private static int AnnouncementDay(string symbol) => Math.Abs(symbol.GetHashCode()) % 20 + 1;

    [Fact]
    public void Tick_ShouldAnnounceDividend_OnQuarterStart()
    {
        var stock = CreateDividendStock("DIV", 100m, 0.04m); // 4% yield
        var stocks = new List<Stock> { stock };
        var portfolio = new Portfolio(50_000m);

        // Use the staggered announcement day for "DIV"
        var day = AnnouncementDay("DIV");
        var gameTime = new DateTime(2027, 1, day, 9, 31, 0);
        // If it falls on weekend, advance to Monday
        while (gameTime.DayOfWeek == DayOfWeek.Saturday || gameTime.DayOfWeek == DayOfWeek.Sunday)
            gameTime = gameTime.AddDays(1);

        _engine.Tick(stocks, portfolio, gameTime);

        Assert.Single(_engine.NewAnnouncementsThisTick);
        Assert.Equal("DIV", _engine.NewAnnouncementsThisTick[0].Symbol);
        Assert.Equal(1.00m, _engine.NewAnnouncementsThisTick[0].DividendPerShare); // $100 * 4% / 4
    }

    [Fact]
    public void Tick_ShouldNotAnnounce_ForNonDividendStocks()
    {
        var stock = CreateDividendStock("NODIV", 100m, 0m);
        var stocks = new List<Stock> { stock };
        var portfolio = new Portfolio(50_000m);

        var gameTime = new DateTime(2027, 1, 4, 9, 31, 0);
        _engine.Tick(stocks, portfolio, gameTime);

        Assert.Empty(_engine.NewAnnouncementsThisTick);
    }

    [Fact]
    public void Tick_ShouldNotAnnounce_MidQuarter()
    {
        var stock = CreateDividendStock("DIV", 100m, 0.04m);
        var stocks = new List<Stock> { stock };
        var portfolio = new Portfolio(50_000m);

        // Feb 15 — mid-quarter, no announcement
        var gameTime = new DateTime(2027, 2, 15, 9, 31, 0);
        _engine.Tick(stocks, portfolio, gameTime);

        Assert.Empty(_engine.NewAnnouncementsThisTick);
    }

    [Fact]
    public void ExDate_ShouldDropPrice()
    {
        var stock = CreateDividendStock("DIV", 100m, 0.04m);
        var stocks = new List<Stock> { stock };
        var portfolio = new Portfolio(50_000m);

        // Announce (use correct staggered day)
        _engine.Tick(stocks, portfolio, GetAnnouncementDate("DIV"));
        Assert.Single(_engine.PendingPayments);

        var exDate = _engine.PendingPayments[0].ExDividendDate;

        // Advance to ex-date
        _engine.Tick(stocks, portfolio, exDate);

        // Price should have dropped by dividend amount ($1.00)
        Assert.Equal(99m, stock.CurrentPrice);
    }

    [Fact]
    public void PaymentDate_ShouldCreditCashWithTax()
    {
        var stock = CreateDividendStock("DIV", 100m, 0.04m);
        var stocks = new List<Stock> { stock };
        var portfolio = new Portfolio(50_000m);
        portfolio.Positions["DIV"] = new Position("DIV", 50m, 100m); // 50 shares

        // Announce
        _engine.Tick(stocks, portfolio, GetAnnouncementDate("DIV"));
        var paymentDate = _engine.PendingPayments[0].PaymentDate;

        // Process ex-date
        var exDate = _engine.PendingPayments[0].ExDividendDate;
        _engine.Tick(stocks, portfolio, exDate);

        // Process payment
        var cashBefore = portfolio.Cash;
        _engine.Tick(stocks, portfolio, paymentDate);

        // 50 shares × $1.00 = $50 gross, 15% tax = $7.50, net = $42.50
        Assert.Single(_engine.PaymentsThisTick);
        Assert.Equal(50m, _engine.PaymentsThisTick[0].GrossDividend);
        Assert.Equal(7.50m, _engine.PaymentsThisTick[0].Tax);
        Assert.Equal(42.50m, _engine.PaymentsThisTick[0].NetDividend);
        Assert.Equal(cashBefore + 42.50m, portfolio.Cash);
    }

    [Fact]
    public void NoPosition_ShouldNotPayDividend()
    {
        var stock = CreateDividendStock("DIV", 100m, 0.04m);
        var stocks = new List<Stock> { stock };
        var portfolio = new Portfolio(50_000m); // No position in DIV

        _engine.Tick(stocks, portfolio, GetAnnouncementDate("DIV"));
        var paymentDate = _engine.PendingPayments[0].PaymentDate;
        var exDate = _engine.PendingPayments[0].ExDividendDate;

        _engine.Tick(stocks, portfolio, exDate);
        _engine.Tick(stocks, portfolio, paymentDate);

        Assert.Empty(_engine.PaymentsThisTick);
        Assert.Equal(50_000m, portfolio.Cash);
    }

    [Fact]
    public void ShouldNotAnnounce_TwiceInSameQuarter()
    {
        var stock = CreateDividendStock("DIV", 100m, 0.04m);
        var stocks = new List<Stock> { stock };
        var portfolio = new Portfolio(50_000m);

        var annDate = GetAnnouncementDate("DIV");
        _engine.Tick(stocks, portfolio, annDate);
        Assert.Single(_engine.PendingPayments);

        // Next day — same quarter, should not announce again
        _engine.Tick(stocks, portfolio, annDate.AddDays(1));
        Assert.Single(_engine.PendingPayments); // Still just one
    }

    [Fact]
    public void ExDate_ShouldBeWeekday()
    {
        var stock = CreateDividendStock("DIV", 100m, 0.04m);
        var stocks = new List<Stock> { stock };
        var portfolio = new Portfolio(50_000m);

        _engine.Tick(stocks, portfolio, GetAnnouncementDate("DIV"));

        var exDate = _engine.PendingPayments[0].ExDividendDate;
        Assert.True(exDate.DayOfWeek != DayOfWeek.Saturday && exDate.DayOfWeek != DayOfWeek.Sunday,
            $"Ex-date {exDate} should be a weekday");
    }

    /// <summary>Get the correct staggered announcement date for a symbol in Q1 2027.</summary>
    private static DateTime GetAnnouncementDate(string symbol)
    {
        var day = AnnouncementDay(symbol);
        var date = new DateTime(2027, 1, day, 9, 31, 0);
        while (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            date = date.AddDays(1);
        return date;
    }

    private static Stock CreateDividendStock(string symbol, decimal price, decimal yield)
    {
        return new Stock(symbol, $"{symbol} Corp", "Utilities")
        {
            CurrentPrice = price,
            PreviousClose = price,
            BidPrice = price - 0.10m,
            AskPrice = price + 0.10m,
            DayHigh = price,
            DayLow = price,
            BaseVolatility = 0.01m,
            LiquidityScore = 7,
            FairValue = price,
            AverageVolume = 500_000,
            SharesOutstanding = 50_000_000,
            DividendYield = yield,
        };
    }
}
