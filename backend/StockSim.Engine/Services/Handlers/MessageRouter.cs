using StockSim.Engine.Utils;

namespace StockSim.Engine.Services.Handlers;

public class MessageRouter
{
    private static readonly Logger Log = new("MessageRouter");
    private readonly List<IMessageHandler> _handlers = new();

    public void Register(IMessageHandler handler) => _handlers.Add(handler);

    public async Task RouteAsync(string type, string payload)
    {
        foreach (var handler in _handlers)
        {
            if (handler.CanHandle(type))
            {
                await handler.HandleAsync(type, payload);
                return;
            }
        }
        Log.Warn("Unknown message type", new { type });
    }
}
