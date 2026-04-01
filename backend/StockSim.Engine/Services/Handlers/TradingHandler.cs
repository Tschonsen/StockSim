using System.Text.Json;
using StockSim.Engine.Models;
using StockSim.Engine.Utils;

namespace StockSim.Engine.Services.Handlers;

/// <summary>
/// Handles trading messages: PlaceOrder, CancelOrder, PlaceBracketOrder, BuyOption, SellOption, AcceptTenderOffer.
/// </summary>
public class TradingHandler : IMessageHandler
{
    private static readonly Logger Log = new("TradingHandler");
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private static readonly HashSet<string> MessageTypes = new()
    {
        "PlaceOrder", "CancelOrder", "PlaceBracketOrder", "BuyOption", "SellOption", "AcceptTenderOffer"
    };

    private readonly GameContext _ctx;

    public TradingHandler(GameContext ctx)
    {
        _ctx = ctx;
    }

    public bool CanHandle(string messageType) => MessageTypes.Contains(messageType);

    public async Task HandleAsync(string messageType, string payload)
    {
        switch (messageType)
        {
            case "PlaceOrder":
                var orderReq = JsonSerializer.Deserialize<PlaceOrderRequest>(payload, JsonOpts);
                if (_ctx.GameLoop != null && orderReq != null)
                    await HandlePlaceOrder(orderReq);
                break;

            case "CancelOrder":
                var cancelReq = JsonSerializer.Deserialize<CancelOrderRequest>(payload, JsonOpts);
                if (_ctx.GameLoop != null && cancelReq != null)
                {
                    var cancelled = _ctx.GameLoop.OrderEngine.CancelOrder(cancelReq.OrderId);
                    await _ctx.Server.SendAsync("OrderCancelled", new { orderId = cancelReq.OrderId, success = cancelled });
                }
                break;

            case "PlaceBracketOrder":
                var bracketReq = JsonSerializer.Deserialize<BracketOrderRequest>(payload, JsonOpts);
                if (_ctx.GameLoop != null && bracketReq != null)
                {
                    var bracketStock = _ctx.GameLoop.StocksBySymbol.GetValueOrDefault(bracketReq.Symbol);
                    if (bracketStock != null)
                    {
                        var tpResult = _ctx.GameLoop.OrderEngine.PlaceOrder(
                            bracketReq.Symbol, OrderSide.Sell, OrderType.Limit,
                            bracketReq.Quantity, bracketStock, _ctx.GameLoop.GameTime,
                            _ctx.GameLoop.IsMarketOpen(), bracketReq.TakeProfitPrice);

                        var slResult = _ctx.GameLoop.OrderEngine.PlaceOrder(
                            bracketReq.Symbol, OrderSide.Sell, OrderType.Stop,
                            bracketReq.Quantity, bracketStock, _ctx.GameLoop.GameTime,
                            _ctx.GameLoop.IsMarketOpen(), stopPrice: bracketReq.StopLossPrice);

                        if (tpResult.Order != null && slResult.Order != null)
                        {
                            tpResult.Order.OCOPairId = slResult.Order.Id;
                            slResult.Order.OCOPairId = tpResult.Order.Id;
                        }

                        await _ctx.Server.SendAsync("BracketOrderPlaced", new
                        {
                            success = true,
                            takeProfitId = tpResult.Order?.Id,
                            stopLossId = slResult.Order?.Id,
                        });
                        await SendHelper.SendOrdersUpdate(_ctx);
                    }
                }
                break;

            case "BuyOption":
                var buyOptReq = JsonSerializer.Deserialize<OptionOrderRequest>(payload, JsonOpts);
                if (_ctx.GameLoop != null && buyOptReq != null)
                {
                    var result = ExecuteOptionOrder(buyOptReq, isBuy: true);
                    await _ctx.Server.SendAsync("OptionOrderResult", result);
                    await SendHelper.SendPortfolioUpdate(_ctx);
                }
                break;

            case "SellOption":
                var sellOptReq = JsonSerializer.Deserialize<OptionOrderRequest>(payload, JsonOpts);
                if (_ctx.GameLoop != null && sellOptReq != null)
                {
                    var result = ExecuteOptionOrder(sellOptReq, isBuy: false);
                    await _ctx.Server.SendAsync("OptionOrderResult", result);
                    await SendHelper.SendPortfolioUpdate(_ctx);
                }
                break;

            case "AcceptTenderOffer":
                if (_ctx.GameLoop != null)
                {
                    var tenderReq = JsonSerializer.Deserialize<TenderOfferResponse>(payload, JsonOpts);
                    if (tenderReq != null && _ctx.GameLoop.Portfolio.Positions.ContainsKey(tenderReq.Symbol))
                    {
                        var pos = _ctx.GameLoop.Portfolio.Positions[tenderReq.Symbol];
                        var shares = Math.Abs(pos.Shares);
                        var proceeds = shares * tenderReq.OfferPrice;

                        _ctx.GameLoop.Portfolio.Positions.Remove(tenderReq.Symbol);
                        _ctx.GameLoop.Portfolio.Cash += proceeds;
                        _ctx.GameLoop.Portfolio.RealizedPnL += proceeds - (shares * pos.AverageCost);
                        _ctx.GameLoop.Portfolio.TradeCount++;

                        await _ctx.Server.SendAsync("TenderOfferAccepted", new
                        {
                            symbol = tenderReq.Symbol,
                            shares,
                            proceeds,
                            offerPrice = tenderReq.OfferPrice,
                        });
                        await SendHelper.SendPortfolioUpdate(_ctx);

                        Log.Info("Tender offer accepted", new { symbol = tenderReq.Symbol, shares, proceeds });
                    }
                }
                break;
        }
    }

    private async Task HandlePlaceOrder(PlaceOrderRequest req)
    {
        var gameLoop = _ctx.GameLoop!;
        var server = _ctx.Server;

        var stock = gameLoop.StocksBySymbol.GetValueOrDefault(req.Symbol);
        if (stock == null)
        {
            await server.SendAsync("OrderResult", new { success = false, error = $"Unknown symbol: {req.Symbol}" });
            return;
        }

        if (!Enum.TryParse<OrderSide>(req.Side, ignoreCase: true, out var side))
        {
            await server.SendAsync("OrderResult", new { success = false, error = $"Invalid order side: {req.Side}" });
            return;
        }
        if (!Enum.TryParse<OrderType>(req.Type, ignoreCase: true, out var type))
        {
            await server.SendAsync("OrderResult", new { success = false, error = $"Invalid order type: {req.Type}" });
            return;
        }
        var tif = TimeInForce.GTC;
        if (!string.IsNullOrEmpty(req.TimeInForce))
            Enum.TryParse(req.TimeInForce, ignoreCase: true, out tif);

        var result = gameLoop.OrderEngine.PlaceOrder(
            req.Symbol, side, type, req.Quantity, stock,
            gameLoop.GameTime, gameLoop.IsMarketOpen(),
            req.LimitPrice, tif, req.StopPrice, req.TrailAmount);

        await server.SendAsync("OrderResult", new
        {
            success = result.Success,
            error = result.Error,
            order = result.Order == null ? null : new
            {
                id = result.Order.Id,
                symbol = result.Order.Symbol,
                side = result.Order.Side.ToString(),
                type = result.Order.Type.ToString(),
                status = result.Order.Status.ToString(),
                quantity = result.Order.Quantity,
                filledQuantity = result.Order.FilledQuantity,
                fillPrice = result.Order.FillPrice,
                commission = result.Order.Commission,
                limitPrice = result.Order.LimitPrice,
                placedAt = result.Order.PlacedAt.ToString("o"),
                filledAt = result.Order.FilledAt?.ToString("o"),
                rejectReason = result.Order.RejectReason,
            }
        });

        await SendHelper.SendPortfolioUpdate(_ctx);
        await SendHelper.SendOrdersUpdate(_ctx);
    }

    private object ExecuteOptionOrder(OptionOrderRequest req, bool isBuy)
    {
        var gameLoop = _ctx.GameLoop!;
        var engine = gameLoop.OptionsEngine;

        OptionContract? contract = null;
        foreach (var chain in engine.Chains.Values)
        {
            contract = chain.AllContracts.FirstOrDefault(c => c.Id == req.ContractId);
            if (contract != null) break;
        }

        if (contract == null)
            return new { success = false, message = "Contract not found" };
        if (contract.IsExpired)
            return new { success = false, message = "Contract has expired" };

        var price = isBuy ? contract.AskPrice : contract.BidPrice;
        var totalCost = price * OptionContract.Multiplier * req.Quantity;
        var commission = 0.65m * req.Quantity;

        if (isBuy)
        {
            var totalDebit = totalCost + commission;
            if (gameLoop.Portfolio.Cash < totalDebit)
                return new { success = false, message = $"Insufficient cash. Need ${totalDebit:N2}, have ${gameLoop.Portfolio.Cash:N2}" };

            gameLoop.Portfolio.Cash -= totalDebit;

            var existing = engine.Positions.FirstOrDefault(p => p.ContractId == contract.Id);
            if (existing != null)
            {
                var totalQty = existing.Quantity + req.Quantity;
                existing.AvgCost = (existing.AvgCost * Math.Abs(existing.Quantity) + price * req.Quantity) / Math.Abs(totalQty);
                existing.Quantity = totalQty;
            }
            else
            {
                engine.Positions.Add(new OptionPosition
                {
                    ContractId = contract.Id,
                    UnderlyingSymbol = contract.UnderlyingSymbol,
                    Type = contract.Type,
                    StrikePrice = contract.StrikePrice,
                    ExpirationDate = contract.ExpirationDate,
                    Quantity = req.Quantity,
                    AvgCost = price,
                });
            }

            Log.Info("Option bought", new { symbol = contract.UnderlyingSymbol, contract = contract.DisplayName, qty = req.Quantity, price, total = totalDebit });
        }
        else
        {
            var existing = engine.Positions.FirstOrDefault(p => p.ContractId == contract.Id);
            if (existing == null || existing.Quantity < req.Quantity)
                return new { success = false, message = "Insufficient position to sell" };

            var totalCredit = totalCost - commission;
            gameLoop.Portfolio.Cash += totalCredit;
            existing.Quantity -= req.Quantity;

            var pnl = (price - existing.AvgCost) * OptionContract.Multiplier * req.Quantity;

            if (existing.Quantity == 0)
                engine.Positions.Remove(existing);

            Log.Info("Option sold", new { symbol = contract.UnderlyingSymbol, contract = contract.DisplayName, qty = req.Quantity, price, pnl });
        }

        return new { success = true, message = isBuy ? "Option purchased" : "Option sold", price, quantity = req.Quantity };
    }

    private record PlaceOrderRequest(string Symbol, string Side, string Type, decimal Quantity, decimal? LimitPrice, string? TimeInForce, decimal? StopPrice, decimal? TrailAmount);
    private record CancelOrderRequest(long OrderId);
    private record BracketOrderRequest(string Symbol, decimal Quantity, decimal TakeProfitPrice, decimal StopLossPrice);
    private record OptionOrderRequest(long ContractId, string Symbol, int Quantity);
    private record TenderOfferResponse(string Symbol, decimal OfferPrice);
}
