using StockSim.Engine.Models;

namespace StockSim.Engine.Services.Handlers;

/// <summary>
/// Shared context that holds references to game state and server.
/// Passed to all message handlers so they can access the game loop and send messages.
/// </summary>
public class GameContext
{
    public GameLoop? GameLoop { get; set; }
    public WebSocketServer Server { get; }

    /// <summary>Called by GameControlHandler when a new game is started.</summary>
    public Action<GameLoop>? OnGameLoopChanged { get; set; }

    public GameContext(WebSocketServer server)
    {
        Server = server;
    }

    public void SetGameLoop(GameLoop? gameLoop)
    {
        GameLoop = gameLoop;
        OnGameLoopChanged?.Invoke(gameLoop!);
    }
}
