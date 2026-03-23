using StockSim.Engine.Models;

namespace StockSim.Engine.Services;

/// <summary>
/// Generates a simulated orderbook for display purposes.
/// Bible 5.3 + 12.3: 10 levels each side, AI Market Maker populated.
/// The orderbook is generated on-demand, not persisted.
/// </summary>
public static class OrderbookGenerator
{
    /// <summary>
    /// Generate 10 bid and 10 ask levels for a stock.
    /// </summary>
    public static OrderbookSnapshot Generate(Stock stock, Random rng)
    {
        var bids = new OrderbookLevel[10];
        var asks = new OrderbookLevel[10];

        var midPrice = stock.CurrentPrice;
        var halfSpread = (stock.AskPrice - stock.BidPrice) / 2;
        if (halfSpread <= 0) halfSpread = midPrice * 0.001m;

        // Base quantity depends on liquidity
        var baseQty = stock.LiquidityScore switch
        {
            >= 9 => 5000 + rng.Next(5000),
            >= 7 => 2000 + rng.Next(3000),
            >= 5 => 500 + rng.Next(1500),
            >= 3 => 100 + rng.Next(400),
            _ => 50 + rng.Next(150),
        };

        for (int i = 0; i < 10; i++)
        {
            // Bids: decreasing prices, levels get thinner further from mid
            var bidSpacing = halfSpread * (1 + i * 0.3m + (decimal)(rng.NextDouble() * 0.2));
            var bidPrice = Math.Round(stock.BidPrice - bidSpacing * i, 2);
            bidPrice = Math.Max(bidPrice, 0.01m);
            var bidQty = (int)(baseQty * (1.0 - i * 0.08) * (0.7 + rng.NextDouble() * 0.6));
            bids[i] = new OrderbookLevel { Price = bidPrice, Quantity = Math.Max(bidQty, 10) };

            // Asks: increasing prices
            var askSpacing = halfSpread * (1 + i * 0.3m + (decimal)(rng.NextDouble() * 0.2));
            var askPrice = Math.Round(stock.AskPrice + askSpacing * i, 2);
            var askQty = (int)(baseQty * (1.0 - i * 0.08) * (0.7 + rng.NextDouble() * 0.6));
            asks[i] = new OrderbookLevel { Price = askPrice, Quantity = Math.Max(askQty, 10) };
        }

        return new OrderbookSnapshot
        {
            Symbol = stock.Symbol,
            Bids = bids,
            Asks = asks,
            BestBid = stock.BidPrice,
            BestAsk = stock.AskPrice,
            Spread = stock.AskPrice - stock.BidPrice,
            SpreadPercent = stock.SpreadPercent,
        };
    }
}

public class OrderbookSnapshot
{
    public string Symbol { get; set; } = "";
    public OrderbookLevel[] Bids { get; set; } = Array.Empty<OrderbookLevel>();
    public OrderbookLevel[] Asks { get; set; } = Array.Empty<OrderbookLevel>();
    public decimal BestBid { get; set; }
    public decimal BestAsk { get; set; }
    public decimal Spread { get; set; }
    public decimal SpreadPercent { get; set; }
}

public class OrderbookLevel
{
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}
