using System.Runtime.CompilerServices;

// Expose internal test seams (deterministic scenario hooks) to the test project only.
[assembly: InternalsVisibleTo("StockSim.Engine.Tests")]
