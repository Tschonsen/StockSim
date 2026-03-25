using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services;

/// <summary>
/// Manages dividend payments, ex-dates, and price adjustments.
/// Bible 11.3.2: Quarterly dividends for stocks with DividendYield > 0.
///
/// Timeline per quarter:
///   1. Announcement: 5 trading days before ex-date
///   2. Ex-Dividend Date: price drops by dividend amount at open
///   3. Record Date: 1 day after ex-date (who gets the dividend)
///   4. Payment Date: 10 days after record date (cash credited)
/// </summary>
public class DividendEngine
{
    private readonly Logger _log = new("DividendEngine");
    private readonly List<DividendEvent> _pendingPayments = new();
    private readonly List<DividendEvent> _paidDividends = new();

    public IReadOnlyList<DividendEvent> PendingPayments => _pendingPayments.AsReadOnly();
    public IReadOnlyList<DividendEvent> PaidDividends => _paidDividends.AsReadOnly();

    /// <summary>New dividend announcements this tick (for news).</summary>
    public List<DividendEvent> NewAnnouncementsThisTick { get; } = new();

    /// <summary>Payments made this tick (for notifications).</summary>
    public List<DividendPayment> PaymentsThisTick { get; } = new();

    /// <summary>Total dividends received by the player (gross, before tax).</summary>
    public decimal TotalDividendsReceived { get; set; }

    /// <summary>
    /// Called each tick. Handles announcements, ex-date price drops, and payments.
    /// </summary>
    public void Tick(IReadOnlyList<Stock> stocks, Portfolio portfolio, DateTime gameTime)
    {
        NewAnnouncementsThisTick.Clear();
        PaymentsThisTick.Clear();

        // Only process at market open (9:31)
        if (gameTime.TimeOfDay != new TimeSpan(9, 31, 0)) return;
        if (gameTime.DayOfWeek == DayOfWeek.Saturday || gameTime.DayOfWeek == DayOfWeek.Sunday) return;

        foreach (var stock in stocks)
        {
            if (stock.DividendYield <= 0) continue;

            // Check if it's time for a quarterly dividend announcement
            // Quarters: end of Mar, Jun, Sep, Dec — announce 20 trading days before quarter end
            if (ShouldAnnounceDividend(stock, gameTime))
            {
                var dividendPerShare = CalculateQuarterlyDividend(stock);
                var exDate = GetNextTradingDay(gameTime, 5);
                var paymentDate = GetNextTradingDay(exDate, 11);

                var evt = new DividendEvent
                {
                    Symbol = stock.Symbol,
                    DividendPerShare = dividendPerShare,
                    AnnouncementDate = gameTime,
                    ExDividendDate = exDate,
                    PaymentDate = paymentDate,
                };

                _pendingPayments.Add(evt);
                NewAnnouncementsThisTick.Add(evt);

                _log.Info("Dividend announced", new
                {
                    symbol = stock.Symbol,
                    dividend = dividendPerShare,
                    exDate = exDate.ToString("yyyy-MM-dd"),
                    paymentDate = paymentDate.ToString("yyyy-MM-dd"),
                });
            }
        }

        // Process ex-dates: drop price by dividend amount
        foreach (var evt in _pendingPayments.Where(e => e.ExDividendDate.Date == gameTime.Date && !e.ExDateProcessed))
        {
            var stock = stocks.FirstOrDefault(s => s.Symbol == evt.Symbol);
            if (stock != null)
            {
                stock.CurrentPrice = Math.Max(0.01m, Math.Round(stock.CurrentPrice - evt.DividendPerShare, 2));
                evt.ExDateProcessed = true;

                _log.Info("Ex-dividend price adjustment", new { symbol = stock.Symbol, drop = evt.DividendPerShare, newPrice = stock.CurrentPrice });
            }
        }

        // Process payments: credit cash to player
        var paymentsToProcess = _pendingPayments
            .Where(e => e.PaymentDate.Date <= gameTime.Date && !e.Paid)
            .ToList();

        foreach (var evt in paymentsToProcess)
        {
            if (portfolio.Positions.TryGetValue(evt.Symbol, out var position))
            {
                var totalDividend = Math.Round(position.Shares * evt.DividendPerShare, 2);
                // Short sellers pay the full dividend (no tax benefit); long holders pay 15% tax
                var tax = position.IsShort ? 0m : Math.Round(totalDividend * 0.15m, 2);
                var netDividend = position.IsShort ? -Math.Abs(totalDividend) : totalDividend - tax;

                portfolio.Cash += netDividend;
                evt.Paid = true;

                var payment = new DividendPayment
                {
                    Symbol = evt.Symbol,
                    Shares = position.Shares,
                    DividendPerShare = evt.DividendPerShare,
                    GrossDividend = totalDividend,
                    Tax = tax,
                    NetDividend = netDividend,
                };
                PaymentsThisTick.Add(payment);
                TotalDividendsReceived += totalDividend;

                _log.Info("Dividend paid", new
                {
                    symbol = evt.Symbol,
                    shares = position.Shares,
                    gross = totalDividend,
                    tax,
                    net = netDividend,
                });
            }
            else
            {
                evt.Paid = true; // No position, mark as done
            }
        }

        // Clean up old paid dividends
        var paid = _pendingPayments.Where(e => e.Paid).ToList();
        foreach (var p in paid)
        {
            _pendingPayments.Remove(p);
            _paidDividends.Add(p);
        }
    }

    private bool ShouldAnnounceDividend(Stock stock, DateTime gameTime)
    {
        // Announce on the 1st trading day of: Jan, Apr, Jul, Oct (quarterly)
        var month = gameTime.Month;
        var day = gameTime.Day;

        if (month != 1 && month != 4 && month != 7 && month != 10) return false;
        if (day > 5) return false; // Only first few days of the quarter

        // Check if already announced this quarter
        var quarterStart = new DateTime(gameTime.Year, month, 1);
        return !_pendingPayments.Any(e => e.Symbol == stock.Symbol && e.AnnouncementDate >= quarterStart)
            && !_paidDividends.Any(e => e.Symbol == stock.Symbol && e.AnnouncementDate >= quarterStart);
    }

    private static decimal CalculateQuarterlyDividend(Stock stock)
    {
        // Annual dividend = Price × Yield, quarterly = /4
        var annualDividend = stock.CurrentPrice * stock.DividendYield;
        return Math.Round(annualDividend / 4m, 2);
    }

    private static DateTime GetNextTradingDay(DateTime from, int tradingDays)
    {
        var date = from.Date;
        int counted = 0;
        while (counted < tradingDays)
        {
            date = date.AddDays(1);
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                counted++;
        }
        return date.Add(new TimeSpan(9, 31, 0));
    }
}

/// <summary>A pending or completed dividend event.</summary>
public class DividendEvent
{
    public string Symbol { get; set; } = "";
    public decimal DividendPerShare { get; set; }
    public DateTime AnnouncementDate { get; set; }
    public DateTime ExDividendDate { get; set; }
    public DateTime PaymentDate { get; set; }
    public bool ExDateProcessed { get; set; }
    public bool Paid { get; set; }
}

/// <summary>A dividend payment made to the player.</summary>
public class DividendPayment
{
    public string Symbol { get; set; } = "";
    public decimal Shares { get; set; }
    public decimal DividendPerShare { get; set; }
    public decimal GrossDividend { get; set; }
    public decimal Tax { get; set; }
    public decimal NetDividend { get; set; }
}
