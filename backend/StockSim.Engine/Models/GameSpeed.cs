namespace StockSim.Engine.Models;

/// <summary>
/// Game simulation speed settings.
/// See Spec section 10.2 for speed definitions.
/// </summary>
public enum GameSpeed
{
    /// <summary>Simulation frozen. Player can analyze but nothing changes.</summary>
    Paused = 0,

    /// <summary>1 tick = 1 game minute = 1 real second.</summary>
    Normal = 1,

    /// <summary>1 tick = 1 game minute = 0.5 real seconds.</summary>
    Fast = 2,

    /// <summary>1 tick = 1 game minute = 0.2 real seconds.</summary>
    VeryFast = 5,

    /// <summary>1 tick = 1 game minute = 0.1 real seconds.</summary>
    Maximum = 10,
}
