namespace StockSim.Engine.Services;

/// <summary>
/// Sector-aware content pools for news placeholders (Point 6). Keeps generated headlines plausible:
/// a healthcare miss blames trial delays, not "rising input costs"; a bank's product is a payments
/// network, not a "blockchain solution". Unknown sectors fall back to a neutral pool.
/// </summary>
public static class SectorContent
{
    /// <summary>Plausible miss / weakness reasons for a sector's earnings and guidance news.</summary>
    public static string[] Reasons(string? sector) => sector switch
    {
        "Technology" => new[] { "slowing enterprise software spend", "cloud price competition", "elongating sales cycles", "saturation in core markets", "AI infrastructure cost overruns" },
        "Healthcare" => new[] { "clinical trial delays", "pricing pressure from payers", "regulatory review setbacks", "patent cliff erosion", "slower drug adoption" },
        "Energy" => new[] { "commodity price swings", "production outages", "weak refining margins", "tightening emissions rules", "project cost overruns" },
        "Financials" => new[] { "rising credit losses", "net interest margin compression", "deposit outflows", "weaker capital-markets activity", "higher loan-loss provisions" },
        "Consumer Goods" => new[] { "soft consumer spending", "inventory gluts", "input cost inflation", "private-label competition", "shifting demand" },
        "Industrials" => new[] { "weak order backlogs", "supply chain disruptions", "rising input costs", "soft industrial demand", "project delays" },
        "Materials" => new[] { "commodity price declines", "weak construction demand", "energy cost spikes", "oversupply", "mine disruptions" },
        "Real Estate" => new[] { "rising vacancy rates", "higher financing costs", "soft leasing demand", "cap-rate expansion", "refinancing pressure" },
        "Telecommunications" => new[] { "subscriber churn", "price competition", "heavy network capex", "spectrum cost pressure", "cord-cutting" },
        "Utilities" => new[] { "adverse rate-case rulings", "higher fuel costs", "storm-related outages", "rising financing costs", "regulatory pushback" },
        "Luxury Goods" => new[] { "softening discretionary demand", "weak tourism flows", "currency headwinds", "inventory destocking", "a China demand slowdown" },
        "Transportation" => new[] { "fuel cost spikes", "soft freight demand", "labor disruptions", "capacity gluts", "weak shipping rates" },
        _ => new[] { "weakening demand", "rising costs", "competitive pressure", "macro headwinds", "operational setbacks" },
    };

    /// <summary>Plausible product / technology descriptors for a sector (fallback when no real
    /// company product is available, e.g. sector-level news).</summary>
    public static string[] Technologies(string? sector) => sector switch
    {
        "Technology" => new[] { "AI platform", "cloud infrastructure", "cybersecurity suite", "data analytics platform", "developer toolkit" },
        "Healthcare" => new[] { "drug candidate", "clinical platform", "diagnostic device", "gene therapy", "medical device" },
        "Energy" => new[] { "grid-storage system", "solar array", "carbon-capture unit", "smart-grid platform", "offshore wind project" },
        "Financials" => new[] { "payments network", "trading platform", "risk-management engine", "digital banking app", "fraud-detection system" },
        "Consumer Goods" => new[] { "product line", "flagship brand", "direct-to-consumer platform", "packaging innovation", "loyalty program" },
        "Industrials" => new[] { "automation system", "robotics platform", "industrial sensor suite", "factory-automation line", "predictive-maintenance system" },
        "Materials" => new[] { "advanced alloy", "composite material", "specialty chemical", "recycling process", "extraction technology" },
        "Real Estate" => new[] { "mixed-use development", "smart-building platform", "property-management system", "logistics hub", "residential complex" },
        "Telecommunications" => new[] { "5G network", "fiber rollout", "streaming platform", "network-virtualization stack", "edge-computing layer" },
        "Utilities" => new[] { "smart-grid platform", "grid-modernization program", "renewable generation project", "demand-response system", "battery-storage facility" },
        "Luxury Goods" => new[] { "flagship collection", "boutique concept", "heritage line", "bespoke service", "limited-edition release" },
        "Transportation" => new[] { "logistics platform", "fleet-management system", "autonomous-driving stack", "route-optimization engine", "electric fleet" },
        _ => new[] { "flagship product", "core platform", "new service offering", "next-generation product line", "key initiative" },
    };

    /// <summary>Sector-specific KPIs that earnings beats/misses turn on (e.g. "net interest margin"
    /// for banks, "comparable sales" for retail), so headlines read like the right industry.</summary>
    public static string[] EarningsMetrics(string? sector) => sector switch
    {
        "Technology" => new[] { "cloud revenue growth", "enterprise bookings", "AI-driven demand" },
        "Healthcare" => new[] { "pipeline progress", "drug sales", "trial readouts" },
        "Energy" => new[] { "production volumes", "refining margins", "upstream output" },
        "Financials" => new[] { "net interest margin", "loan growth", "trading revenue" },
        "Consumer Goods" => new[] { "comparable sales", "volume growth", "pricing gains" },
        "Industrials" => new[] { "order intake", "backlog growth", "equipment demand" },
        "Materials" => new[] { "shipment volumes", "realized prices", "production output" },
        "Real Estate" => new[] { "occupancy rates", "rental income", "leasing activity" },
        "Telecommunications" => new[] { "subscriber additions", "ARPU growth", "broadband net adds" },
        "Utilities" => new[] { "rate-base growth", "regulated earnings", "load growth" },
        "Luxury Goods" => new[] { "comparable sales", "brand momentum", "flagship demand" },
        "Transportation" => new[] { "freight volumes", "load factors", "yield growth" },
        _ => new[] { "revenue", "core earnings", "operating results" },
    };
}
