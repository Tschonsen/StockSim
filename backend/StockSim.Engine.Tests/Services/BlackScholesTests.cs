using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

public class BlackScholesTests
{
    // Reference values verified against standard BS calculators
    // S=100, K=100, T=1yr, r=5%, sigma=20%, q=0
    const double S = 100, K = 100, T = 1.0, R = 0.05, Sigma = 0.20, Q = 0;

    [Fact]
    public void NormCdf_StandardValues()
    {
        Assert.InRange(BlackScholes.NormCdf(0), 0.4999, 0.5001);
        Assert.InRange(BlackScholes.NormCdf(1), 0.8413, 0.8414);
        Assert.InRange(BlackScholes.NormCdf(-1), 0.1586, 0.1587);
        Assert.InRange(BlackScholes.NormCdf(2), 0.9772, 0.9773);
    }

    [Fact]
    public void CallPrice_ATM_StandardCase()
    {
        // S=100, K=100, T=1, r=5%, vol=20% → Call ≈ $10.45
        var call = BlackScholes.Price(S, K, T, R, Sigma, Q, OptionType.Call);
        Assert.InRange(call, 10.0, 11.0);
    }

    [Fact]
    public void PutPrice_ATM_StandardCase()
    {
        // S=100, K=100, T=1, r=5%, vol=20% → Put ≈ $5.57
        var put = BlackScholes.Price(S, K, T, R, Sigma, Q, OptionType.Put);
        Assert.InRange(put, 5.0, 6.5);
    }

    [Fact]
    public void PutCallParity_ShouldHold()
    {
        // C - P = S*e^(-qT) - K*e^(-rT)
        var call = BlackScholes.Price(S, K, T, R, Sigma, Q, OptionType.Call);
        var put = BlackScholes.Price(S, K, T, R, Sigma, Q, OptionType.Put);
        var parity = S * Math.Exp(-Q * T) - K * Math.Exp(-R * T);

        Assert.InRange(call - put, parity - 0.01, parity + 0.01);
    }

    [Fact]
    public void DeepITM_Call_ShouldApproachIntrinsic()
    {
        // S=150, K=100, very ITM call should be close to S-K*e^(-rT)
        var call = BlackScholes.Price(150, 100, T, R, Sigma, Q, OptionType.Call);
        Assert.True(call > 48, $"Deep ITM call should be > 48, got {call:F2}");
    }

    [Fact]
    public void DeepOTM_Put_ShouldBeNearZero()
    {
        // S=150, K=100, deep OTM put
        var put = BlackScholes.Price(150, 100, T, R, Sigma, Q, OptionType.Put);
        Assert.True(put < 2, $"Deep OTM put should be < 2, got {put:F2}");
    }

    [Fact]
    public void ExpiredOption_ShouldReturnIntrinsic()
    {
        var call = BlackScholes.Price(110, 100, 0, R, Sigma, Q, OptionType.Call);
        Assert.Equal(10, call, 2);

        var put = BlackScholes.Price(90, 100, 0, R, Sigma, Q, OptionType.Put);
        Assert.Equal(10, put, 2);

        var otmCall = BlackScholes.Price(90, 100, 0, R, Sigma, Q, OptionType.Call);
        Assert.Equal(0, otmCall, 2);
    }

    // === Greeks Tests ===

    [Fact]
    public void Delta_Call_ShouldBeBetween0And1()
    {
        var g = BlackScholes.CalcGreeks(S, K, T, R, Sigma, Q, OptionType.Call);
        Assert.InRange(g.Delta, 0.0, 1.0);
        // ATM call delta ≈ 0.55-0.65
        Assert.InRange(g.Delta, 0.50, 0.70);
    }

    [Fact]
    public void Delta_Put_ShouldBeBetweenMinus1And0()
    {
        var g = BlackScholes.CalcGreeks(S, K, T, R, Sigma, Q, OptionType.Put);
        Assert.InRange(g.Delta, -1.0, 0.0);
        // ATM put delta ≈ -0.45 to -0.35
        Assert.InRange(g.Delta, -0.50, -0.30);
    }

    [Fact]
    public void Gamma_ShouldBePositive()
    {
        var gc = BlackScholes.CalcGreeks(S, K, T, R, Sigma, Q, OptionType.Call);
        var gp = BlackScholes.CalcGreeks(S, K, T, R, Sigma, Q, OptionType.Put);
        Assert.True(gc.Gamma > 0);
        Assert.True(gp.Gamma > 0);
        // Gamma should be same for call and put
        Assert.Equal(gc.Gamma, gp.Gamma, 6);
    }

    [Fact]
    public void Theta_ShouldBeNegative_ForLongOptions()
    {
        var gc = BlackScholes.CalcGreeks(S, K, T, R, Sigma, Q, OptionType.Call);
        Assert.True(gc.Theta < 0, $"Call theta should be negative, got {gc.Theta}");

        var gp = BlackScholes.CalcGreeks(S, K, T, R, Sigma, Q, OptionType.Put);
        Assert.True(gp.Theta < 0, $"Put theta should be negative, got {gp.Theta}");
    }

    [Fact]
    public void Vega_ShouldBePositive_AndSameForCallPut()
    {
        var gc = BlackScholes.CalcGreeks(S, K, T, R, Sigma, Q, OptionType.Call);
        var gp = BlackScholes.CalcGreeks(S, K, T, R, Sigma, Q, OptionType.Put);
        Assert.True(gc.Vega > 0);
        Assert.Equal(gc.Vega, gp.Vega, 6);
    }

    // === IV Solver ===

    [Fact]
    public void SolveIV_ShouldRecoverKnownVolatility()
    {
        // Price with 30% vol, then solve IV — should get ~0.30 back
        var price = BlackScholes.Price(S, K, T, R, 0.30, Q, OptionType.Call);
        var iv = BlackScholes.SolveIV(price, S, K, T, R, Q, OptionType.Call);

        Assert.InRange(iv, 0.29, 0.31);
    }

    [Fact]
    public void SolveIV_DeepOTM_ShouldStillConverge()
    {
        var price = BlackScholes.Price(100, 150, 0.25, R, 0.40, Q, OptionType.Call);
        var iv = BlackScholes.SolveIV(price, 100, 150, 0.25, R, Q, OptionType.Call);

        Assert.InRange(iv, 0.30, 0.50);
    }

    // === OptionsEngine Tests ===

    [Fact]
    public void GenerateStrikes_ShouldCenterOnStockPrice()
    {
        var engine = new OptionsEngine(42);
        var stocks = new List<Stock>
        {
            new("OPTN", "Options Corp", "Technology")
            {
                CurrentPrice = 100m, SharesOutstanding = 500_000_000, LiquidityScore = 8, // ~$50B market cap
            }
        };

        engine.GenerateChains(stocks, new DateTime(2027, 1, 5));

        Assert.True(engine.Chains.ContainsKey("OPTN"));
        var chain = engine.Chains["OPTN"];
        Assert.True(chain.Expirations.Count >= 3, $"Expected >=3 expirations, got {chain.Expirations.Count}");

        var firstSlice = chain.Slices[chain.Expirations[0]];
        Assert.Contains(100m, firstSlice.Strikes); // ATM strike should exist
        Assert.True(firstSlice.Strikes.Count >= 15, $"Expected >=15 strikes, got {firstSlice.Strikes.Count}");
        Assert.True(firstSlice.Calls.Count > 0);
        Assert.True(firstSlice.Puts.Count > 0);
    }

    [Fact]
    public void TickDay_ShouldPriceAllContracts()
    {
        var engine = new OptionsEngine(42);
        var stocks = new List<Stock>
        {
            new("TEST", "Test Inc", "Technology")
            {
                CurrentPrice = 50m, SharesOutstanding = 200_000_000, LiquidityScore = 7, // ~$10B market cap
                DividendYield = 0.02m,
            }
        };

        engine.GenerateChains(stocks, new DateTime(2027, 1, 5));
        engine.TickDay(stocks, new DateTime(2027, 1, 6));

        var chain = engine.Chains["TEST"];
        var firstSlice = chain.Slices[chain.Expirations[0]];
        var atmCall = firstSlice.Calls[50m];

        Assert.True(atmCall.TheoreticalPrice > 0, $"ATM call price should be > 0, got {atmCall.TheoreticalPrice}");
        Assert.True(atmCall.BidPrice > 0);
        Assert.True(atmCall.AskPrice > atmCall.BidPrice);
        Assert.InRange(atmCall.Delta, 0.3, 0.8); // ATM call delta
        Assert.True(atmCall.Theta < 0); // Time decay
        Assert.True(atmCall.Vega > 0);
    }

    [Fact]
    public void OptionContract_ITM_OTM_Checks()
    {
        var call = new OptionContract { Type = OptionType.Call, StrikePrice = 100m };
        Assert.True(call.IsITM(110m));
        Assert.False(call.IsITM(90m));
        Assert.Equal(10m, call.IntrinsicValue(110m));
        Assert.Equal(0m, call.IntrinsicValue(90m));

        var put = new OptionContract { Type = OptionType.Put, StrikePrice = 100m };
        Assert.True(put.IsITM(90m));
        Assert.False(put.IsITM(110m));
        Assert.Equal(10m, put.IntrinsicValue(90m));
        Assert.Equal(0m, put.IntrinsicValue(110m));
    }

    [Fact]
    public void SettlementValue_ShouldIncludeMultiplier()
    {
        var call = new OptionContract { Type = OptionType.Call, StrikePrice = 100m };
        // Stock at $110, call strike $100 → intrinsic $10 × 100 multiplier = $1000
        Assert.Equal(1000m, call.SettlementValue(110m));
        Assert.Equal(0m, call.SettlementValue(90m)); // OTM
    }
}
