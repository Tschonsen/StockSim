using StockSim.Engine.Models;

namespace StockSim.Engine.Services;

/// <summary>
/// Generates unique CompanyPersonality data for each stock at game start.
/// CEO names, products, founding stories, HQ locations, and rivalries are
/// procedurally assembled from sector-specific pools.
/// </summary>
public static class CompanyPersonalityGenerator
{
    // --- CEO First Names (diverse pool) ---
    private static readonly string[] FirstNames =
    {
        "James", "Sarah", "Michael", "Emily", "David", "Jessica", "Robert", "Amanda",
        "William", "Jennifer", "Richard", "Stephanie", "Thomas", "Lauren", "Daniel", "Rachel",
        "Christopher", "Michelle", "Andrew", "Katherine", "Mark", "Angela", "Steven", "Olivia",
        "Kevin", "Maria", "Brian", "Samantha", "Jason", "Elizabeth", "Eric", "Victoria",
        "Alexander", "Natalie", "Marcus", "Diana", "Benjamin", "Caroline", "Nathan", "Helena",
        "Jonathan", "Priya", "Raj", "Wei", "Hiroshi", "Yuki", "Carlos", "Sofia",
        "Andreas", "Ingrid", "Liam", "Ava", "Noah", "Emma", "Ethan", "Mia",
        "Chen", "Akira", "Omar", "Fatima", "Ivan", "Olga", "Diego", "Lucia",
    };

    // --- CEO Last Names (diverse pool) ---
    private static readonly string[] LastNames =
    {
        "Chen", "Williams", "Johnson", "Brown", "Davis", "Miller", "Wilson", "Moore",
        "Anderson", "Taylor", "Thomas", "Jackson", "White", "Harris", "Martin", "Thompson",
        "Garcia", "Martinez", "Robinson", "Clark", "Rodriguez", "Lewis", "Lee", "Walker",
        "Hall", "Allen", "Young", "Hernandez", "King", "Wright", "Lopez", "Hill",
        "Schmidt", "Mueller", "Nakamura", "Tanaka", "Singh", "Patel", "Kim", "Park",
        "O'Brien", "Sullivan", "Johansson", "Larsen", "Novak", "Kowalski", "Petrov", "Costa",
        "Bergmann", "Fischer", "Weber", "Schneider", "Romano", "Rossi", "Santos", "Oliveira",
        "Andersson", "Eriksson", "Hayes", "Barrett", "Reeves", "Thornton", "Blackwell", "Ashford",
    };

    // --- CEO Archetypes ---
    private static readonly string[] CEOArchetypes =
    {
        "Visionary", "Cost-Cutter", "Empire Builder", "Turnaround Artist",
        "Founder-CEO", "Sales Machine", "Engineer-CEO", "Finance Veteran",
        "Industry Insider", "Disruptor", "Steady Hand", "Dealmaker",
    };

    // --- HQ Locations (60% US, 25% Europe, 10% Asia, 5% Rest) ---
    private static readonly string[] HQLocations =
    {
        // US (60%)
        "San Francisco, CA", "New York, NY", "Austin, TX", "Seattle, WA",
        "Boston, MA", "Chicago, IL", "Los Angeles, CA", "Denver, CO",
        "Miami, FL", "Dallas, TX", "Atlanta, GA", "San Jose, CA",
        "Houston, TX", "Phoenix, AZ", "Minneapolis, MN", "Philadelphia, PA",
        "Detroit, MI", "Portland, OR", "Charlotte, NC", "Nashville, TN",
        "Salt Lake City, UT", "San Diego, CA", "Pittsburgh, PA", "Raleigh, NC",
        "Washington, DC", "Tampa, FL", "Columbus, OH", "Indianapolis, IN",
        // Europe (25%)
        "London, UK", "Frankfurt, Germany", "Zurich, Switzerland", "Paris, France",
        "Amsterdam, Netherlands", "Stockholm, Sweden", "Dublin, Ireland",
        "Munich, Germany", "Milan, Italy", "Madrid, Spain", "Copenhagen, Denmark",
        "Edinburgh, UK",
        // Asia-Pacific (10%)
        "Tokyo, Japan", "Shanghai, China", "Seoul, South Korea", "Singapore",
        "Hong Kong", "Mumbai, India", "Sydney, Australia",
        // Rest of World (5%)
        "Toronto, Canada", "Dubai, UAE", "Sao Paulo, Brazil",
    };

    // --- Sector-specific Products ---
    private static readonly Dictionary<string, string[]> FlagshipProducts = new()
    {
        ["Technology"] = new[] {
            "CloudSync Platform", "QuantumCore Processor", "NeuralNet SDK", "CyberShield Suite",
            "DataFlow Analytics", "PixelStream Engine", "VortexOS", "ByteForge IDE",
            "SynthAI Assistant", "ApexCloud Infrastructure", "LogicGate Security", "FluxDB Database",
            "TeraVision Display", "PhotonLink Network", "HelixCode Compiler", "NanoChip Architecture",
        },
        ["Energy"] = new[] {
            "SolarMax Panel Array", "PetroFlow Pipeline System", "VoltGrid Storage", "HydroTurbine X1",
            "WindHarvest Turbine", "GeoTherm Drill Platform", "FuelCell Pro", "TerraCore Reactor",
            "IonBattery Pack", "PlasmaGen System", "ArcWeld Extractor", "DynamoPower Station",
            "SurgeGuard Transformer", "InfernoDrill Rig", "RadiantCapture Solar Film", "FluxCapacitor Storage",
        },
        ["Financials"] = new[] {
            "CapitalOne Lending Platform", "TrustVault Digital Banking", "GlobalPay Network", "PremierWealth Advisory",
            "SterlingTrade Platform", "FortressRisk Analytics", "MeridianFunds Manager", "PinnacleCredit Engine",
            "VanguardIndex Tracker", "SovereignBond Platform", "LibertyLoan System", "CrestInsure Platform",
            "SummitPay Gateway", "HeritageWealth Engine", "EagleComply Regulatory Suite", "PatriotAudit System",
        },
        ["Healthcare"] = new[] {
            "BioGene Therapy", "NovaCure Treatment", "MediScan Diagnostic", "VitaPulse Monitor",
            "NeuraLink Implant", "CellRegen Platform", "GenomeMap Sequencer", "HelixDrug Discovery",
            "ImmunoShield Vaccine", "PharmaFlow Pipeline", "CardioSync Pacemaker", "SynapseNeuro Interface",
            "OxiPure Respirator", "ProtoCell Culture System", "GenoTherapy Suite", "CryoStore Preservation",
        },
        ["Consumer Goods"] = new[] {
            "FreshHarvest Organic Line", "PureCraft Premium Range", "BrightHome Essentials", "UrbanStyle Collection",
            "EverGreen Natural Products", "DailyBlend Nutrition", "GoldenShelf Gourmet", "NatureNest Wellness",
            "BloomBright Skincare", "ClearView Home Tech", "SwiftServe Delivery", "TrueValue Basics",
            "CraftBrew Artisan Series", "SimpleLife Organics", "NestComfort Home Line", "MapleGrove Heritage",
        },
        ["Industrials"] = new[] {
            "TitanPress Heavy Forge", "ApexCore CNC Machine", "GraniteBlock Builder", "BoltDrive Fastener System",
            "ArcWeld Pro Welder", "MatrixControl PLC", "OmegaLift Crane", "VanguardShield Defense System",
            "SummitReach Excavator", "RidgeLine Conveyor", "AnchorPoint Foundation", "AxisTurn Lathe",
            "ZenithFly Drone Platform", "DeltaForce Actuator", "IronClad Pipeline", "ForgeHammer Press",
        },
        ["Materials"] = new[] {
            "TerraCrystal Alloy", "GeoCore Drill System", "CarbonWeave Composite", "OreMax Extractor",
            "MineralPure Refinery", "AlloyForge Smelter", "StoneGuard Coating", "MetalMesh Filter",
            "PrismLens Optical Glass", "ElementPure Chemical", "CobaltCell Battery Grade", "SilicaShield Ceramic",
            "QuarryKing Crusher", "BedrockBase Foundation", "OnyxEdge Tool Steel", "ZincCoat Plating",
        },
        ["Real Estate"] = new[] {
            "CrownTower Luxury Residences", "HarborView Mixed-Use Complex", "SummitPark Office Campus", "UrbanNest Apartments",
            "MetroGate Shopping Center", "SkylinePlace Tower", "BeaconHill Development", "CrestView Estates",
            "HavenPark Community", "TowerBridge Office", "PinnaclePoint Resort", "GateWay Industrial Park",
            "StoneHaven Condos", "HeritageSquare Retail", "RiverWalk Promenade", "BayShore Marina Village",
        },
        ["Telecommunications"] = new[] {
            "SignalBoost 5G Network", "WaveLink Fiber System", "NetCore Router Platform", "BeamCast Satellite",
            "FiberOptic Express Line", "PulseNet Mesh Network", "EchoStream CDN", "RelayTower System",
            "OrbitSat Communication", "SpectrumWide Band", "GridConnect Hub", "FreqTune Antenna",
            "SyncLink Protocol", "DataPipe Backbone", "WireSpeed Switch", "OmniCast Broadcast",
        },
        ["Utilities"] = new[] {
            "PowerGrid Smart Meter", "GridVolt Transformer", "HydroFlow Dam System", "VoltSafe Circuit",
            "AmpCharge EV Station", "CurrentFlow Monitor", "FlowControl Valve System", "SourceClean Filter",
            "GreenGen Solar Farm", "CleanWater Treatment", "CivicLight Street Grid", "MetroPump Station",
            "NationalGrid Link", "CentralHeat System", "UnitedPower Substation", "PacificWave Tidal",
        },
        ["Luxury Goods"] = new[] {
            "Prestige Signature Collection", "Royal Heritage Timepiece", "Maison Luxe Handbag", "Elite Diamond Series",
            "Noble Cashmere Line", "Grand Atelier Couture", "Imperial Silk Collection", "Regal Crystal Decanter",
            "Opulent Gold Edition", "Crown Jewel Watch", "Sterling Silver Service", "Sovereign Leather Goods",
            "Platinum Reserve Fragrance", "Avant-Garde Design Chair", "Haute Cuisine Kitchenware", "Celeste Evening Gown",
        },
        ["Transportation"] = new[] {
            "SwiftCargo Tracking System", "GlobalRoute Logistics Platform", "RapidFleet Management", "CargoLink Hub",
            "ExpressRail High-Speed", "AeroNav Flight System", "MaritimeTrack Shipping", "VoyagePlan Route Optimizer",
            "TrackStar GPS Fleet", "VelocityDrive EV Truck", "FreightBridge Network", "HubConnect Terminal",
            "PacificLane Shipping Route", "AtlanticExpress Ferry", "CrossRoute Rail Link", "UnitedCargo Warehouse",
        },
    };

    private static readonly Dictionary<string, string[]> SecondaryProducts = new()
    {
        ["Technology"] = new[] { "Cloud Backup Service", "Mobile SDK", "AI Chatbot", "Dev Analytics", "Edge Computing Module", "IoT Gateway", "Blockchain Ledger", "AR Toolkit" },
        ["Energy"] = new[] { "Grid Monitoring Service", "Carbon Credit Platform", "Smart Meter", "EV Charging Network", "Pipeline Inspection Drone", "Energy Trading Desk", "Micro-Grid Controller", "Hydrogen Fuel Cell" },
        ["Financials"] = new[] { "Mobile Banking App", "Fraud Detection AI", "Crypto Custody", "Robo-Advisor", "Insurance Marketplace", "Credit Scoring API", "Portfolio Optimizer", "Compliance Dashboard" },
        ["Healthcare"] = new[] { "Telehealth App", "Patient Portal", "Lab Automation", "Drug Delivery Patch", "Clinical Trial Platform", "Wearable Health Monitor", "AI Radiology", "Pharmacy Management" },
        ["Consumer Goods"] = new[] { "Subscription Box Service", "Loyalty App", "Direct-to-Consumer Platform", "Eco-Friendly Packaging Line", "Private Label Brand", "Meal Kit Service", "Smart Home Device", "Fitness Supplement" },
        ["Industrials"] = new[] { "Predictive Maintenance AI", "Fleet Tracking System", "Safety Compliance Suite", "3D-Printed Parts Service", "Robotic Arm Module", "Warehouse Automation", "Drone Inspection Service", "Digital Twin Platform" },
        ["Materials"] = new[] { "Recycled Materials Line", "Quality Testing Lab", "Custom Alloy Service", "Environmental Monitoring", "Mining Automation", "Chemical Analysis Kit", "Waste Treatment System", "Smart Packaging Film" },
        ["Real Estate"] = new[] { "Property Management App", "Virtual Tour Platform", "Smart Building System", "Tenant Portal", "Energy Management System", "Coworking Space Brand", "Parking Optimization", "Green Building Certification" },
        ["Telecommunications"] = new[] { "Streaming Service", "IoT Connectivity", "Enterprise VPN", "Cloud Phone System", "Cybersecurity Add-on", "Smart Home Bundle", "Business Wi-Fi", "Edge Computing Service" },
        ["Utilities"] = new[] { "Smart Thermostat Program", "Solar Lease Option", "Water Conservation Kit", "EV Charging Program", "Demand Response System", "Home Energy Audit", "Battery Storage Lease", "Green Energy Certificate" },
        ["Luxury Goods"] = new[] { "Travel & Hospitality Line", "Home Décor Collection", "Limited Artist Collab", "Private Members Club", "Concierge Service", "Heritage Archive", "Bespoke Tailoring", "Fine Dining Brand" },
        ["Transportation"] = new[] { "Last-Mile Delivery", "Freight Brokerage App", "Fleet Insurance", "Driver Training Academy", "Cold Chain Logistics", "Customs Clearance Service", "Warehouse Robotics", "Carbon Offset Program" },
    };

    // --- Sector-specific description templates ---
    private static readonly Dictionary<string, string[]> DescriptionTemplates = new()
    {
        ["Technology"] = new[] {
            "Leading provider of {0} solutions for enterprise clients",
            "Pioneering next-generation {0} technology since {1}",
            "Transforming industries through innovative {0} platforms",
            "Disrupting the {0} space with AI-powered solutions",
        },
        ["Energy"] = new[] {
            "Major player in {0} production and distribution",
            "Committed to sustainable {0} solutions since {1}",
            "Powering communities through advanced {0} infrastructure",
            "Industry leader in {0} technology and exploration",
        },
        ["Financials"] = new[] {
            "Trusted provider of {0} services to institutions and individuals",
            "Delivering innovative {0} solutions since {1}",
            "Connecting capital markets through cutting-edge {0} platforms",
            "Building financial security through {0} excellence",
        },
        ["Healthcare"] = new[] {
            "Advancing human health through breakthrough {0} research",
            "Pioneering {0} innovation to transform patient outcomes",
            "Leading {0} company committed to improving lives since {1}",
            "Developing next-generation {0} therapies and diagnostics",
        },
        ["Consumer Goods"] = new[] {
            "Beloved brand delivering quality {0} to millions of homes",
            "Building trusted {0} brands since {1}",
            "Meeting everyday needs with innovative {0} products",
            "Growing portfolio of premium {0} brands",
        },
        ["Industrials"] = new[] {
            "Engineering excellence in {0} manufacturing since {1}",
            "Building the infrastructure of tomorrow through {0}",
            "Global leader in {0} solutions for heavy industry",
            "Driving industrial innovation with advanced {0} systems",
        },
        ["Materials"] = new[] {
            "Extracting and processing high-grade {0} for global markets",
            "Supplying critical {0} materials to industry since {1}",
            "Innovating in {0} production with sustainable practices",
            "Leading {0} producer with operations across multiple continents",
        },
        ["Real Estate"] = new[] {
            "Premier developer of {0} properties in top-tier markets",
            "Creating vibrant {0} communities since {1}",
            "Managing a diversified portfolio of {0} assets",
            "Transforming urban landscapes through innovative {0} development",
        },
        ["Telecommunications"] = new[] {
            "Connecting millions through advanced {0} networks",
            "Building the digital backbone with {0} infrastructure since {1}",
            "Leading provider of {0} connectivity solutions",
            "Expanding broadband access through next-gen {0} technology",
        },
        ["Utilities"] = new[] {
            "Providing reliable {0} services to communities since {1}",
            "Delivering essential {0} infrastructure across the region",
            "Transitioning to clean {0} generation for a sustainable future",
            "Regulated provider of {0} serving millions of customers",
        },
        ["Luxury Goods"] = new[] {
            "Crafting exquisite {0} for discerning clients worldwide",
            "Heritage house of {0} excellence since {1}",
            "Defining luxury through artisanal {0} craftsmanship",
            "Iconic {0} brand synonymous with elegance and quality",
        },
        ["Transportation"] = new[] {
            "Moving goods efficiently through {0} networks since {1}",
            "Connecting global supply chains with {0} logistics",
            "Leading provider of {0} services across major trade routes",
            "Innovating in {0} to reduce costs and carbon footprint",
        },
    };

    // --- Era-appropriate founding story templates ---
    private static readonly string[] PreWarStoryTemplates =
    {
        "Established in {0} as a family-owned {2} enterprise, growing across generations",
        "Incorporated in {0} by {1}, building a {2} empire from a single factory",
        "Founded in {0} during the industrial boom, becoming a pillar of the {2} industry",
        "One of the oldest names in {2}, tracing its roots to {0} when {1} opened its first office",
    };
    private static readonly string[] MidCenturyStoryTemplates =
    {
        "Founded in {0} by {1}, who recognized the post-war potential of the {2} sector",
        "Established in {0} when {1} spun off a division from a larger conglomerate to focus on {2}",
        "Started in {0} as a regional {2} company, expanding nationally through strategic acquisitions",
        "Created in {0} by {1}, a former executive who saw untapped demand in the {2} market",
    };
    private static readonly string[] ModernStoryTemplates =
    {
        "Founded in {0} by {1}, who spotted a gap in the {2} market",
        "Born out of {1}'s garage workshop in {0}, now a {2} powerhouse",
        "Launched in {0} with venture funding to disrupt the {2} industry",
        "Spun off from a university research lab in {0}, commercializing breakthrough {2} technology",
        "Founded in {0} after {1} left a tech giant to build a better {2} company",
        "Created in {0} by a team of {2} veterans led by {1}",
        "Launched in {0} with a mission to democratize access to {2}",
    };

    // --- Subsector shorthand for descriptions ---
    private static readonly Dictionary<string, string> SubsectorShorthand = new()
    {
        ["Software"] = "software", ["Semiconductors"] = "semiconductor", ["Cloud Computing"] = "cloud",
        ["Cybersecurity"] = "cybersecurity", ["AI & Machine Learning"] = "AI", ["Consumer Electronics"] = "electronics",
        ["Enterprise SaaS"] = "SaaS", ["Gaming"] = "gaming",
        ["Oil & Gas"] = "oil & gas", ["Renewable Energy"] = "renewable energy", ["Solar"] = "solar",
        ["Wind"] = "wind energy", ["Nuclear"] = "nuclear energy", ["Utilities Infrastructure"] = "utility",
        ["Energy Storage"] = "energy storage",
        ["Banks"] = "banking", ["Insurance"] = "insurance", ["Asset Management"] = "asset management",
        ["FinTech"] = "fintech", ["Payment Processing"] = "payments", ["Private Equity"] = "private equity",
        ["Mortgage & Lending"] = "lending",
        ["Pharmaceuticals"] = "pharmaceutical", ["Biotechnology"] = "biotech", ["Medical Devices"] = "medical device",
        ["Health Insurance"] = "health insurance", ["Telehealth"] = "telehealth", ["Diagnostics"] = "diagnostics",
        ["Hospital & Clinics"] = "healthcare services",
        ["Food & Beverage"] = "food & beverage", ["Retail"] = "retail", ["E-Commerce"] = "e-commerce",
        ["Apparel"] = "apparel", ["Home & Garden"] = "home goods", ["Personal Care"] = "personal care",
        ["Pet Industry"] = "pet care",
        ["Aerospace & Defense"] = "aerospace", ["Construction"] = "construction", ["Machinery"] = "machinery",
        ["Waste Management"] = "waste management", ["Engineering"] = "engineering", ["3D Printing"] = "3D printing",
        ["Robotics"] = "robotics",
        ["Mining"] = "mining", ["Chemicals"] = "chemical", ["Metals & Steel"] = "metals",
        ["Forest Products"] = "forestry", ["Specialty Materials"] = "specialty materials",
        ["Commercial"] = "commercial real estate", ["Residential"] = "residential", ["Industrial REIT"] = "industrial REIT",
        ["Healthcare REIT"] = "healthcare REIT", ["Data Centers"] = "data center REIT",
    };

    // --- CEO Quotes by Archetype ---
    private static readonly Dictionary<string, string[]> CEOQuotes = new()
    {
        ["Visionary"] = new[] {
            "The future belongs to those who see it before everyone else.",
            "We're not building for today — we're building for the next decade.",
            "Innovation isn't optional, it's survival.",
        },
        ["Cost-Cutter"] = new[] {
            "Every dollar saved goes straight to the bottom line.",
            "Efficiency is the foundation of sustainable growth.",
            "We've eliminated waste at every level of the organization.",
        },
        ["Empire Builder"] = new[] {
            "This acquisition positions us as the undisputed market leader.",
            "We see enormous opportunity for consolidation in this space.",
            "Scale is our competitive moat.",
        },
        ["Turnaround Artist"] = new[] {
            "The restructuring is ahead of schedule and the results speak for themselves.",
            "We inherited challenges, but we're turning them into opportunities.",
            "This company has incredible potential that was being held back.",
        },
        ["Founder-CEO"] = new[] {
            "I started this company to solve a problem I personally experienced.",
            "We've never lost sight of our founding mission.",
            "This isn't just a business — it's my life's work.",
        },
        ["Sales Machine"] = new[] {
            "Revenue growth is our north star, everything else follows.",
            "We're winning customers at a pace our competitors can't match.",
            "Our pipeline has never been stronger.",
        },
        ["Engineer-CEO"] = new[] {
            "The product speaks for itself. We let the technology do the talking.",
            "We invest in R&D because that's where the breakthroughs come from.",
            "Technical excellence isn't a goal — it's a requirement.",
        },
        ["Finance Veteran"] = new[] {
            "Disciplined capital allocation is how you create long-term shareholder value.",
            "We're managing risk while maximizing returns — that's what we do.",
            "The balance sheet has never been stronger.",
        },
        ["Industry Insider"] = new[] {
            "After 25 years in this industry, I know exactly where it's heading.",
            "Our relationships and expertise give us an unfair advantage.",
            "We understand this market better than anyone.",
        },
        ["Disruptor"] = new[] {
            "The incumbents should be worried. We're rewriting the rules.",
            "Why accept the status quo when you can change it?",
            "We move fast and break things — on purpose.",
        },
        ["Steady Hand"] = new[] {
            "Consistency and reliability — that's what our shareholders expect from us.",
            "We don't chase trends. We execute our strategy quarter after quarter.",
            "Boring is beautiful when it comes to generating returns.",
        },
        ["Dealmaker"] = new[] {
            "This deal creates value that neither company could achieve alone.",
            "We see M&A as a core competency, not a one-time event.",
            "The synergies from this combination are substantial and achievable.",
        },
    };

    // --- Product Descriptions by Sector ---
    private static readonly Dictionary<string, string[]> ProductDescriptions = new()
    {
        ["Technology"] = new[] {
            "{0} — our flagship platform serving over 10 million users worldwide.",
            "{0} leverages cutting-edge AI to deliver unmatched performance.",
            "{0} has become the industry standard for enterprise-grade solutions.",
        },
        ["Energy"] = new[] {
            "{0} — next-generation energy solution with 40% better efficiency.",
            "{0} powers critical infrastructure across three continents.",
            "{0} represents a breakthrough in sustainable energy technology.",
        },
        ["Healthcare"] = new[] {
            "{0} has shown breakthrough results in Phase III clinical trials.",
            "{0} — our precision medicine platform transforming patient outcomes.",
            "{0} is FDA-approved and deployed in over 2,000 hospitals.",
        },
        ["Financials"] = new[] {
            "{0} processes over $50 billion in transactions annually.",
            "{0} — trusted by 500+ institutional clients for risk management.",
            "{0} has disrupted traditional financial services with zero-fee access.",
        },
        ["Consumer Goods"] = new[] {
            "{0} — beloved by consumers in 40+ countries.",
            "{0} has achieved cult-brand status among millennials and Gen Z.",
            "{0} generates $2B+ in annual recurring revenue.",
        },
        ["Industrials"] = new[] {
            "{0} sets the global standard for industrial automation.",
            "{0} — trusted by Fortune 500 manufacturers worldwide.",
            "{0} reduces operational costs by an average of 30%.",
        },
        ["Materials"] = new[] {
            "{0} — advanced materials enabling the next generation of technology.",
            "{0} is the preferred supplier for aerospace and defense applications.",
            "{0} has revolutionized supply chain efficiency in specialty materials.",
        },
        ["Real Estate"] = new[] {
            "{0} — premium properties in top-tier metropolitan markets.",
            "{0} portfolio includes 200+ Class A commercial properties.",
            "{0} delivers consistent 6%+ cap rates across all holdings.",
        },
        ["Telecommunications"] = new[] {
            "{0} — connecting 50 million subscribers with 99.99% uptime.",
            "{0} is the backbone of next-generation 5G infrastructure.",
            "{0} delivers fiber-optic speeds to underserved markets.",
        },
        ["Utilities"] = new[] {
            "{0} — providing reliable power to 3 million residential customers.",
            "{0} has achieved 100% renewable energy certification.",
            "{0} maintains the industry's lowest outage rate.",
        },
        ["Luxury Goods"] = new[] {
            "{0} — the world's most coveted luxury brand since its founding.",
            "{0} combines centuries of craftsmanship with modern design.",
            "{0} has a 3-year waitlist for its signature collection.",
        },
        ["Transportation"] = new[] {
            "{0} — moving 500,000 tons of freight daily across the continent.",
            "{0} operates the most fuel-efficient fleet in the industry.",
            "{0} has reduced delivery times by 40% through AI-powered routing.",
        },
    };

    // --- Key Milestone Templates ---
    private static readonly string[] KeyMilestoneTemplates =
    {
        "Reached $1B revenue milestone in {0}",
        "Expanded into international markets in {0}",
        "Completed landmark IPO in {0}",
        "Won industry innovation award for {1} in {0}",
        "Opened 100th facility in {0}",
        "Acquired key competitor in the {1} space in {0}",
        "Launched groundbreaking {1} product in {0}",
        "Named to Fortune 500 list in {0}",
        "Secured transformative partnership in {0}",
        "Hired 10,000th employee in {0}",
    };

    /// <summary>
    /// Generate personality data for all stocks. Call once after stock generation.
    /// Also assigns rivalries between companies in the same sector.
    /// </summary>
    public static void GenerateAll(IList<Stock> stocks, int seed)
    {
        var rng = new Random(seed + 7777); // Offset seed to avoid correlation with price generation

        foreach (var stock in stocks)
        {
            stock.Personality = Generate(rng, stock);
        }

        // Assign rivalries: pair up companies within the same subsector or sector
        AssignRivalries(stocks, rng);
    }

    private static CompanyPersonality Generate(Random rng, Stock stock)
    {
        var sector = stock.Sector;
        var subsector = string.IsNullOrEmpty(stock.Subsector) ? sector : stock.Subsector;
        var subsectorWord = SubsectorShorthand.GetValueOrDefault(subsector, sector.ToLower());

        var ceoFirst = FirstNames[rng.Next(FirstNames.Length)];
        var ceoLast = LastNames[rng.Next(LastNames.Length)];
        var ceoName = $"{ceoFirst} {ceoLast}";
        var archetype = CEOArchetypes[rng.Next(CEOArchetypes.Length)];

        // FoundedYear correlates with MarketCap: bigger = older
        var mcap = stock.MarketCap;
        int foundedYear;
        if (mcap > 100_000_000_000m)       foundedYear = rng.Next(1880, 1975);  // Mega-cap: old institutions
        else if (mcap > 10_000_000_000m)   foundedYear = rng.Next(1940, 2000);  // Large-cap
        else if (mcap > 1_000_000_000m)    foundedYear = rng.Next(1970, 2015);  // Mid-cap
        else                                foundedYear = rng.Next(1995, 2025);  // Small/micro-cap: young

        var hq = HQLocations[rng.Next(HQLocations.Length)];

        var flagship = "";
        if (FlagshipProducts.TryGetValue(sector, out var products))
            flagship = products[rng.Next(products.Length)];

        var secondary = "";
        if (rng.NextDouble() < 0.6 && SecondaryProducts.TryGetValue(sector, out var secProducts))
            secondary = secProducts[rng.Next(secProducts.Length)];

        // Description
        var desc = "";
        if (DescriptionTemplates.TryGetValue(sector, out var templates))
        {
            var tmpl = templates[rng.Next(templates.Length)];
            desc = string.Format(tmpl, subsectorWord, foundedYear);
        }

        // Founding story — era-appropriate templates
        var storyPool = foundedYear < 1950 ? PreWarStoryTemplates
                      : foundedYear < 1990 ? MidCenturyStoryTemplates
                      : ModernStoryTemplates;
        var storyTmpl = storyPool[rng.Next(storyPool.Length)];
        var founderName = $"{FirstNames[rng.Next(FirstNames.Length)]} {LastNames[rng.Next(LastNames.Length)]}";
        var story = string.Format(storyTmpl, foundedYear, founderName, subsectorWord);

        // CEO quote based on archetype
        var quote = CEOQuotes.TryGetValue(archetype, out var quotes)
            ? quotes[rng.Next(quotes.Length)]
            : "We remain focused on delivering value to our shareholders.";

        // Product description
        var productDesc = flagship != ""
            ? ProductDescriptions.TryGetValue(sector, out var descs)
                ? string.Format(descs[rng.Next(descs.Length)], flagship)
                : $"Industry-leading {subsectorWord} solution."
            : "";

        // Credit rating correlates with market cap + income
        var rating = mcap > 50_000_000_000m ? "AA" : mcap > 10_000_000_000m ? "A"
            : mcap > 2_000_000_000m ? "BBB" : mcap > 500_000_000m ? "BB" : "B";
        if (stock.NetIncome > 0 && rng.NextDouble() < 0.3) rating += "+";

        // Key milestone
        var milestoneYear = foundedYear + rng.Next(5, Math.Max(6, 2027 - foundedYear));
        var milestone = KeyMilestoneTemplates[rng.Next(KeyMilestoneTemplates.Length)];
        milestone = string.Format(milestone, milestoneYear, subsectorWord);

        return new CompanyPersonality
        {
            CEOName = ceoName,
            CEOArchetype = archetype,
            FoundedYear = foundedYear,
            Headquarters = hq,
            Description = desc,
            FlagshipProduct = flagship,
            SecondaryProduct = secondary,
            FoundingStory = story,
            CEOQuote = quote,
            ProductDescription = productDesc,
            CreditRating = rating,
            KeyMilestone = milestone,
        };
    }

    /// <summary>
    /// Pair companies into rivalries. Prefers same-subsector pairs.
    /// ~60% of companies get a rival.
    /// </summary>
    private static void AssignRivalries(IList<Stock> stocks, Random rng)
    {
        // Group by subsector first, then sector
        var bySubsector = stocks
            .Where(s => !s.Traits.Contains("ETF"))
            .GroupBy(s => string.IsNullOrEmpty(s.Subsector) ? s.Sector : $"{s.Sector}|{s.Subsector}")
            .Where(g => g.Count() >= 2)
            .ToList();

        foreach (var group in bySubsector)
        {
            var members = group.ToList();
            // Shuffle
            for (int i = members.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (members[i], members[j]) = (members[j], members[i]);
            }

            // Pair up (skip last if odd count)
            for (int i = 0; i + 1 < members.Count; i += 2)
            {
                if (rng.NextDouble() > 0.6) continue; // 60% chance of rivalry

                var a = members[i];
                var b = members[i + 1];

                if (a.Personality != null && string.IsNullOrEmpty(a.Personality.RivalSymbol))
                    a.Personality.RivalSymbol = b.Symbol;
                if (b.Personality != null && string.IsNullOrEmpty(b.Personality.RivalSymbol))
                    b.Personality.RivalSymbol = a.Symbol;
            }
        }
    }
}
