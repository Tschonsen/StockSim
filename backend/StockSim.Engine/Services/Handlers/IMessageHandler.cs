using System.Text.Json;

namespace StockSim.Engine.Services.Handlers;

public interface IMessageHandler
{
    bool CanHandle(string messageType);
    Task HandleAsync(string messageType, string payload);
}
