using StockSim.Engine.Models;

namespace StockSim.Engine.Services;

/// <summary>
/// Black-Scholes European option pricing with full Greeks and IV solver.
///
/// Formulas:
///   d1 = [ln(S/K) + (r - q + sigma²/2) * T] / (sigma * √T)
///   d2 = d1 - sigma * √T
///   Call = S * e^(-qT) * N(d1) - K * e^(-rT) * N(d2)
///   Put  = K * e^(-rT) * N(-d2) - S * e^(-qT) * N(-d1)
/// </summary>
public static class BlackScholes
{
    /// <summary>Price a European option.</summary>
    /// <param name="s">Current stock price</param>
    /// <param name="k">Strike price</param>
    /// <param name="t">Time to expiry in years</param>
    /// <param name="r">Risk-free rate (annual, e.g. 0.05 = 5%)</param>
    /// <param name="sigma">Volatility (annual, e.g. 0.30 = 30%)</param>
    /// <param name="q">Dividend yield (annual, e.g. 0.02 = 2%)</param>
    /// <param name="type">Call or Put</param>
    public static double Price(double s, double k, double t, double r, double sigma, double q, OptionType type)
    {
        if (s <= 0 || k <= 0) return 0;
        if (t <= 0) return type == OptionType.Call ? Math.Max(s - k, 0) : Math.Max(k - s, 0);
        if (sigma <= 0) sigma = 0.001;

        var (d1, d2) = D1D2(s, k, t, r, sigma, q);

        return type == OptionType.Call
            ? s * Math.Exp(-q * t) * NormCdf(d1) - k * Math.Exp(-r * t) * NormCdf(d2)
            : k * Math.Exp(-r * t) * NormCdf(-d2) - s * Math.Exp(-q * t) * NormCdf(-d1);
    }

    /// <summary>Calculate all Greeks for an option.</summary>
    public static Greeks CalcGreeks(double s, double k, double t, double r, double sigma, double q, OptionType type)
    {
        if (s <= 0 || k <= 0) return new Greeks();
        if (t <= 0) return new Greeks
        {
            Delta = type == OptionType.Call ? (s > k ? 1 : 0) : (s < k ? -1 : 0),
        };
        if (sigma <= 0) sigma = 0.001;

        var (d1, d2) = D1D2(s, k, t, r, sigma, q);
        var sqrtT = Math.Sqrt(t);
        var pdf1 = NormPdf(d1);
        var eqt = Math.Exp(-q * t);
        var ert = Math.Exp(-r * t);

        double delta, theta, rho;

        if (type == OptionType.Call)
        {
            delta = eqt * NormCdf(d1);
            theta = -(eqt * s * pdf1 * sigma) / (2 * sqrtT)
                    - r * k * ert * NormCdf(d2)
                    + q * s * eqt * NormCdf(d1);
            rho = k * t * ert * NormCdf(d2);
        }
        else
        {
            delta = -eqt * NormCdf(-d1);
            theta = -(eqt * s * pdf1 * sigma) / (2 * sqrtT)
                    + r * k * ert * NormCdf(-d2)
                    - q * s * eqt * NormCdf(-d1);
            rho = -k * t * ert * NormCdf(-d2);
        }

        // Gamma and Vega are the same for calls and puts
        var gamma = eqt * pdf1 / (s * sigma * sqrtT);
        var vega = s * eqt * pdf1 * sqrtT;

        return new Greeks
        {
            Delta = delta,
            Gamma = gamma,
            Theta = theta / 365.0,   // Convert to per-day
            Vega = vega / 100.0,     // Per 1% vol change
            Rho = rho / 100.0,       // Per 1% rate change
        };
    }

    /// <summary>
    /// Solve implied volatility via Newton-Raphson.
    /// Given a market price, find sigma such that BS(sigma) = marketPrice.
    /// </summary>
    public static double SolveIV(double marketPrice, double s, double k, double t, double r, double q, OptionType type, int maxIter = 100, double tol = 0.0001)
    {
        if (t <= 0 || marketPrice <= 0) return 0;

        var sigma = 0.20; // Initial guess

        for (int i = 0; i < maxIter; i++)
        {
            var price = Price(s, k, t, r, sigma, q, type);
            var vega = CalcVegaRaw(s, k, t, r, sigma, q);

            var diff = price - marketPrice;
            if (Math.Abs(diff) < tol) return sigma;
            if (vega < 1e-10) break; // Can't converge, vega too small

            sigma -= diff / vega;
            sigma = Math.Clamp(sigma, 0.001, 5.0);
        }

        // Fallback: bisection method
        return BisectionIV(marketPrice, s, k, t, r, q, type);
    }

    // === Internal helpers ===

    private static (double d1, double d2) D1D2(double s, double k, double t, double r, double sigma, double q)
    {
        var sqrtT = Math.Sqrt(t);
        var d1 = (Math.Log(s / k) + (r - q + sigma * sigma / 2) * t) / (sigma * sqrtT);
        var d2 = d1 - sigma * sqrtT;
        return (d1, d2);
    }

    /// <summary>Raw vega (not divided by 100) for Newton-Raphson.</summary>
    private static double CalcVegaRaw(double s, double k, double t, double r, double sigma, double q)
    {
        var (d1, _) = D1D2(s, k, t, r, sigma, q);
        return s * Math.Exp(-q * t) * NormPdf(d1) * Math.Sqrt(t);
    }

    private static double BisectionIV(double marketPrice, double s, double k, double t, double r, double q, OptionType type)
    {
        double lo = 0.001, hi = 5.0;
        for (int i = 0; i < 50; i++)
        {
            var mid = (lo + hi) / 2;
            var price = Price(s, k, t, r, mid, q, type);
            if (price > marketPrice) hi = mid;
            else lo = mid;
            if (hi - lo < 0.0001) break;
        }
        return (lo + hi) / 2;
    }

    /// <summary>Standard normal CDF (Abramowitz & Stegun approximation, error < 1e-7).</summary>
    public static double NormCdf(double x)
    {
        const double a1 = 0.31938153, a2 = -0.356563782, a3 = 1.781477937;
        const double a4 = -1.821255978, a5 = 1.330274429;

        var l = Math.Abs(x);
        var k = 1.0 / (1.0 + 0.2316419 * l);
        var result = 1.0 - (1.0 / Math.Sqrt(2 * Math.PI)) * Math.Exp(-l * l / 2)
            * (a1 * k + a2 * k * k + a3 * k * k * k + a4 * k * k * k * k + a5 * k * k * k * k * k);

        return x < 0 ? 1.0 - result : result;
    }

    /// <summary>Standard normal PDF.</summary>
    public static double NormPdf(double x) =>
        Math.Exp(-x * x / 2) / Math.Sqrt(2 * Math.PI);
}

/// <summary>Option Greeks bundle.</summary>
public record Greeks
{
    public double Delta { get; init; }
    public double Gamma { get; init; }
    public double Theta { get; init; }  // Per day
    public double Vega { get; init; }   // Per 1% vol change
    public double Rho { get; init; }    // Per 1% rate change
}
