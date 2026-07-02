namespace StockSim.Engine.Models;

/// <summary>
/// The channel through which a macro driver reaches a company's fundamentals. The same driver can
/// hit different companies through different channels with opposite sign — e.g. oil is an
/// <see cref="InputCost"/> for airlines (fuel) but an <see cref="OutputPrice"/> for oil producers.
/// See design/EMERGENT_COUPLING.md.
/// </summary>
public enum ExposureChannel
{
    /// <summary>Driver is an input cost → moves operating margin (driver up → margin down).</summary>
    InputCost,

    /// <summary>Company sells the driver → moves revenue (driver up → revenue up). Producers.</summary>
    OutputPrice,

    /// <summary>Driver shifts demand → biases revenue growth. (Designed; not yet implemented.)</summary>
    Demand,

    /// <summary>Driver shifts the valuation multiple, not earnings (e.g. rates → growth PE).
    /// (Designed; not yet implemented.)</summary>
    Valuation,
}

/// <summary>
/// One company's sensitivity to one macro driver, through one channel. The data-driven building
/// block of emergent pricing: adding a sector/driver coupling is a datum, not code.
/// <paramref name="Elasticity"/>: for the change-based channels (<see cref="ExposureChannel.InputCost"/>,
/// <see cref="ExposureChannel.OutputPrice"/>) it is a non-negative strength and the channel fixes the sign.
/// For the level-based channels (<see cref="ExposureChannel.Demand"/>, <see cref="ExposureChannel.Valuation"/>)
/// it is SIGNED — it encodes the driver's direction of effect (e.g. rates → housing demand is negative).
/// </summary>
public sealed record DriverExposure(string Driver, ExposureChannel Channel, decimal Elasticity);
