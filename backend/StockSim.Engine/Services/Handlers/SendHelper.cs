using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services.Handlers;

/// <summary>
/// Shared helper methods for sending WebSocket updates to the frontend.
/// Used by multiple handlers and the tick loop in Program.cs.
/// </summary>
public static class SendHelper
{
    private static readonly Logger Log = new("SendHelper");

    public static async Task SendMarketSnapshot(GameContext ctx)
    {
        var gameLoop = ctx.GameLoop;
        var server = ctx.Server;
        if (gameLoop == null) return;

        var snapshot = gameLoop.Stocks.Select(s => new
        {
            symbol = s.Symbol,
            name = s.Name,
            sector = s.Sector,
            subsector = s.Subsector,
            price = s.CurrentPrice,
            change = s.DayChange,
            changePercent = s.DayChangePercent,
            bid = s.BidPrice,
            ask = s.AskPrice,
            volume = s.DayVolume,
            marketCap = s.MarketCap,
            traits = s.Traits,
            peRatio = s.PERatio,
            dividendYield = s.DividendYield,
            dayHigh = s.DayHigh,
            dayLow = s.DayLow,
            previousClose = s.PreviousClose,
            isSSR = s.IsSSR,
            personality = s.Personality == null ? null : new
            {
                ceoName = s.Personality.CEOName,
                ceoArchetype = s.Personality.CEOArchetype,
                foundedYear = s.Personality.FoundedYear,
                headquarters = s.Personality.Headquarters,
                description = s.Personality.Description,
                flagshipProduct = s.Personality.FlagshipProduct,
                secondaryProduct = s.Personality.SecondaryProduct,
                rivalSymbol = s.Personality.RivalSymbol,
                foundingStory = s.Personality.FoundingStory,
                ceoQuote = s.Personality.CEOQuote,
                productDescription = s.Personality.ProductDescription,
                creditRating = s.Personality.CreditRating,
                keyMilestone = s.Personality.KeyMilestone,
            },
        }).ToList();

        await server.SendAsync("MarketSnapshot", new
        {
            stocks = snapshot,
            gameTime = gameLoop.GameTime.ToString("o"),
            speed = (int)gameLoop.Speed,
            isMarketOpen = gameLoop.IsMarketOpen(),
            marketPhase = gameLoop.Phase.ToString(),
        });
    }

    public static async Task SendPortfolioUpdate(GameContext ctx)
    {
        var gameLoop = ctx.GameLoop;
        var server = ctx.Server;
        if (gameLoop == null) return;

        Func<string, decimal> getPrice = symbol =>
            gameLoop.StocksBySymbol.GetValueOrDefault(symbol)?.CurrentPrice ?? 0m;

        var positions = gameLoop.Portfolio.Positions.Values.Select(p =>
        {
            var price = getPrice(p.Symbol);
            return new
            {
                symbol = p.Symbol,
                shares = p.Shares,
                averageCost = p.AverageCost,
                marketValue = p.MarketValue(price),
                unrealizedPnL = p.UnrealizedPnL(price),
                unrealizedPnLPercent = p.UnrealizedPnLPercent(price),
            };
        }).ToList();

        await server.SendAsync("PortfolioUpdate", new
        {
            cash = gameLoop.Portfolio.Cash,
            portfolioValue = gameLoop.Portfolio.PortfolioValue(getPrice),
            totalEquity = gameLoop.Portfolio.TotalEquity(getPrice),
            realizedPnL = gameLoop.Portfolio.RealizedPnL,
            totalCommissions = gameLoop.Portfolio.TotalCommissions,
            tradeCount = gameLoop.Portfolio.TradeCount,
            positions,
            // Margin data
            marginEnabled = gameLoop.Portfolio.MarginEnabled,
            marginBalance = gameLoop.Portfolio.MarginBalance,
            buyingPower = gameLoop.Portfolio.BuyingPower(getPrice),
            marginUsedPercent = gameLoop.Portfolio.MarginUsedPercent(getPrice),
            // Player reputation
            reputation = new
            {
                marketInfluence = gameLoop.Reputation.MarketInfluence,
                secScrutiny = gameLoop.Reputation.SECScrutiny,
                title = gameLoop.Reputation.Title,
            },
        });
    }

    public static async Task SendOrdersUpdate(GameContext ctx)
    {
        var gameLoop = ctx.GameLoop;
        var server = ctx.Server;
        if (gameLoop == null) return;

        var orders = gameLoop.Portfolio.Orders.Select(o => new
        {
            id = o.Id,
            symbol = o.Symbol,
            side = o.Side.ToString(),
            type = o.Type.ToString(),
            status = o.Status.ToString(),
            quantity = o.Quantity,
            filledQuantity = o.FilledQuantity,
            limitPrice = o.LimitPrice,
            fillPrice = o.FillPrice,
            commission = o.Commission,
            placedAt = o.PlacedAt.ToString("o"),
            filledAt = o.FilledAt?.ToString("o"),
            rejectReason = o.RejectReason,
        }).ToList();

        await server.SendAsync("OrdersUpdate", new { orders });
    }

    public static async Task SendNewsEvents(GameContext ctx)
    {
        var gameLoop = ctx.GameLoop;
        var server = ctx.Server;
        if (gameLoop == null) return;

        var events = gameLoop.EventEngine.NewEventsThisTick.Select(e => new
        {
            id = e.Id,
            type = e.Type.ToString(),
            severity = e.Severity.ToString(),
            sentiment = e.Sentiment,
            headline = e.Headline,
            affectedSymbols = e.AffectedSymbols,
            affectedSectors = e.AffectedSectors,
            priceEffect = e.PriceEffect,
            timestamp = e.TriggeredAt.ToString("o"),
            // Phase 1E: Rich event fields
            summary = e.Summary,
            analystQuote = e.AnalystQuote,
            analystName = e.AnalystName,
            analystFirm = e.AnalystFirm,
            tier = (int)e.Tier,
            tags = e.Tags,
        }).ToList();

        await server.SendAsync("NewsEvents", new { events });
    }

    public static async Task SendPriceUpdate(GameContext ctx, Dictionary<string, decimal> lastSentPrices)
    {
        var gameLoop = ctx.GameLoop;
        var server = ctx.Server;
        if (gameLoop == null) return;

        // Delta updates: only send stocks whose price actually changed
        var updates = new List<object>();
        foreach (var s in gameLoop.Stocks)
        {
            if (lastSentPrices.TryGetValue(s.Symbol, out var lastPrice) && lastPrice == s.CurrentPrice)
                continue;

            lastSentPrices[s.Symbol] = s.CurrentPrice;
            updates.Add(new
            {
                s.Symbol,
                price = s.CurrentPrice,
                bid = s.BidPrice,
                ask = s.AskPrice,
                change = s.DayChange,
                changePercent = s.DayChangePercent,
                volume = s.DayVolume,
                dayHigh = s.DayHigh,
                dayLow = s.DayLow,
                isSSR = s.IsSSR,
            });
        }

        var activeArcs = gameLoop.NarrativeEngine.ActiveArcs.Select(a => new
        {
            id = a.TemplateId,
            name = a.Name,
            phase = a.CurrentPhaseIndex,
            path = a.CurrentPath,
            sector = a.TargetSector,
            symbol = a.TargetSymbol,
        }).ToList();

        await server.SendAsync("MarketUpdate", new
        {
            prices = updates,
            gameTime = gameLoop.GameTime.ToString("o"),
            tick = gameLoop.TickCount,
            isMarketOpen = gameLoop.IsMarketOpen(),
            smaStatus = gameLoop.SMAEngine.State.Status.ToString(),
            activeArcs = activeArcs.Count > 0 ? activeArcs : null,
            vix = gameLoop.EconomicEngine.MarketVolatilityIndex,
            fearGreed = gameLoop.EconomicEngine.GetFearGreedIndex(),
        });
    }
}
