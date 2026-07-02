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

    // --- Sector-specific description templates (12 per sector: 4 Scale, 4 Innovation, 4 Character) ---
    private static readonly Dictionary<string, string[]> DescriptionTemplates = new()
    {
        ["Technology"] = new[] {
            // Scale
            "Leading provider of {0} solutions for enterprise clients",
            "A multi-billion dollar {0} powerhouse with operations spanning 40 countries, established {1}",
            "One of the world's largest {0} companies, processing billions of transactions daily",
            "Dominant force in {0} with over 60% market share in its core segment",
            // Innovation
            "Pioneering next-generation {0} technology since {1}",
            "Disrupting the {0} space with AI-powered solutions",
            "Holder of 200+ patents in {0}, with an R&D budget larger than most competitors' revenue",
            "Redefining what's possible in {0} through a relentless focus on innovation and engineering talent",
            // Character
            "Transforming industries through innovative {0} platforms",
            "The scrappy {0} upstart that grew up — still run with startup intensity, now backed by enterprise scale",
            "Known industry-wide for its engineering culture, this {0} company has been a talent magnet since {1}",
            "A {0} company built on the belief that technology should simplify, not complicate. Customers agree",
        },
        ["Energy"] = new[] {
            "Major player in {0} production and distribution",
            "One of the top five {0} producers globally, with reserves spanning three continents",
            "A vertically integrated {0} giant controlling the full supply chain from exploration to retail",
            "Powering communities through advanced {0} infrastructure",
            "Committed to sustainable {0} solutions since {1}",
            "Industry leader in {0} technology and exploration",
            "Investing $2B annually in next-generation {0} research, betting big on the energy transition",
            "Pioneering carbon-neutral {0} operations with a target of net-zero by 2035",
            "Built to withstand $40 oil, this {0} company has the lowest cost basis in its peer group",
            "A century of {0} expertise, now channeling that knowledge into the fuels of tomorrow",
            "The {0} company that both environmentalists and shareholders can agree on — profitable and responsible since {1}",
            "When governments need a trusted {0} partner, this is the company they call. Operating in 25 countries since {1}",
        },
        ["Financials"] = new[] {
            "Trusted provider of {0} services to institutions and individuals",
            "Processing over $50 billion in daily transactions, a cornerstone of global {0}",
            "One of the world's most systemically important {0} institutions, built on trust since {1}",
            "Connecting capital markets through cutting-edge {0} platforms",
            "Delivering innovative {0} solutions since {1}",
            "Building financial security through {0} excellence",
            "Bringing Wall Street-grade {0} tools to Main Street investors through technology",
            "A fintech disruptor that's forced the entire {0} industry to lower fees and raise standards",
            "The {0} firm your grandmother trusted and your grandchildren will too. Stability personified since {1}",
            "Known for surviving every financial crisis since its founding in {1}, this {0} institution is built different",
            "Where quants meet relationship bankers — a {0} firm that excels at both the science and art of finance",
            "A new breed of {0} company that believes transparency, not fine print, builds customer loyalty",
        },
        ["Healthcare"] = new[] {
            "Advancing human health through breakthrough {0} research",
            "A top-10 global {0} company with a pipeline valued at over $30 billion",
            "Operating in 80 countries with a mission to make {0} accessible to all, since {1}",
            "Developing next-generation {0} therapies and diagnostics",
            "Pioneering {0} innovation to transform patient outcomes",
            "Leading {0} company committed to improving lives since {1}",
            "Holder of breakthrough patents that have fundamentally changed how doctors approach {0}",
            "Investing 25% of revenue back into {0} R&D — the highest ratio in the industry",
            "The {0} company that turned a rare disease treatment into a platform for a dozen blockbuster therapies",
            "Founded by physicians who were tired of waiting for someone else to fix {0}. So they built it themselves",
            "A {0} company where every employee can trace their work to a patient whose life improved",
            "From a single lab bench in {1} to a global {0} leader — this company's growth mirrors the industry itself",
        },
        ["Consumer Goods"] = new[] {
            "Beloved brand delivering quality {0} to millions of homes",
            "A household name in {0} with products in 90% of American pantries",
            "Managing a portfolio of 40+ {0} brands, each a market leader in its category",
            "Growing portfolio of premium {0} brands",
            "Building trusted {0} brands since {1}",
            "Meeting everyday needs with innovative {0} products",
            "Reinventing the {0} aisle with direct-to-consumer brands that bypass traditional retail",
            "A {0} company that turned sustainability from a marketing buzzword into a genuine competitive advantage",
            "The {0} brand that millennials grew up with — and now buy for their own kids",
            "Three generations of families have trusted this {0} company. That loyalty is the brand's real asset",
            "Once a scrappy {0} challenger brand, now the category leader — without ever losing its personality",
            "A {0} company that spends more on product quality than on advertising. The reviews speak for themselves",
        },
        ["Industrials"] = new[] {
            "Engineering excellence in {0} manufacturing since {1}",
            "A $15B {0} conglomerate supplying critical equipment to every major infrastructure project on the planet",
            "Building the infrastructure of tomorrow through {0}",
            "Global leader in {0} solutions for heavy industry",
            "Driving industrial innovation with advanced {0} systems",
            "Holding 500+ patents in {0} automation, with machines running on every continent except Antarctica",
            "The {0} company that both Boeing and Airbus depend on — because there is no substitute for precision",
            "Quietly essential: this {0} firm makes the components inside products you use every day without knowing it",
            "A {0} manufacturer that has never missed a delivery deadline in 30 years. Reliability as a brand promise",
            "Where old-school {0} craftsmanship meets Industry 4.0 automation. Founded {1}, reinvented for the digital age",
            "The {0} company that governments turn to when national infrastructure needs rebuilding",
            "Employee-owned since {1}, this {0} company's workforce has a personal stake in every product that ships",
        },
        ["Materials"] = new[] {
            "Extracting and processing high-grade {0} for global markets",
            "One of the world's top three {0} producers, with mines and processing plants across four continents",
            "Supplying critical {0} materials to industry since {1}",
            "Leading {0} producer with operations across multiple continents",
            "Innovating in {0} production with sustainable practices",
            "Holder of proprietary {0} processing technology that competitors have tried and failed to replicate",
            "Developing next-generation {0} materials for aerospace, defense, and semiconductor applications",
            "A {0} company betting big on recycled materials — because the cheapest mine is the one already above ground",
            "The {0} supplier that chipmakers, automakers, and defense contractors all rely on. Single-source, by choice",
            "Built on a deposit discovered in {1}, this {0} company turned geology into a global business",
            "A {0} company with a 150-year resource base at current extraction rates. Patient capital at its finest",
            "From raw ore to finished product, this vertically integrated {0} company controls every step of the value chain",
        },
        ["Real Estate"] = new[] {
            "Premier developer of {0} properties in top-tier markets",
            "Managing 200+ Class A {0} properties with a combined value exceeding $40 billion",
            "Creating vibrant {0} communities since {1}",
            "Transforming urban landscapes through innovative {0} development",
            "Managing a diversified portfolio of {0} assets",
            "Pioneering mixed-use {0} developments that blend living, working, and retail spaces",
            "A {0} REIT delivering consistent 6%+ yields through disciplined acquisition and best-in-class management",
            "Reimagining {0} for the post-pandemic era with flexible, technology-enabled spaces",
            "The {0} developer that city planners actually want to work with. Known for projects that improve neighborhoods",
            "A family-controlled {0} empire, still run by the grandchildren of the founder who broke ground in {1}",
            "Occupancy rates that defy market cycles — this {0} company's tenant retention is the envy of the industry",
            "From a single {0} property in {1} to a nationwide portfolio. Growth through patience and impeccable timing",
        },
        ["Telecommunications"] = new[] {
            "Connecting millions through advanced {0} networks",
            "Operating the nation's fastest-growing {0} network, now reaching 95% of the population",
            "Building the digital backbone with {0} infrastructure since {1}",
            "Expanding broadband access through next-gen {0} technology",
            "Leading provider of {0} connectivity solutions",
            "Investing $5B this year alone in {0} infrastructure to eliminate dead zones in rural America",
            "The {0} company that enterprises trust for 99.999% uptime and carrier-grade security",
            "A challenger brand in {0} that grew by offering radical transparency: no hidden fees, no contracts",
            "When the pandemic sent everyone home, this {0} company's network held. That earned them 3 million new subscribers",
            "A {0} company founded in {1} on the premise that connectivity is a right, not a luxury",
            "The only {0} provider with coast-to-coast fiber, 5G, and satellite coverage under one roof",
            "Known as the engineer's {0} company — technically superior networks, backed by the best uptime SLA in the business",
        },
        ["Utilities"] = new[] {
            "Providing reliable {0} services to communities since {1}",
            "Serving 5 million customers across 12 states with essential {0} infrastructure",
            "Delivering essential {0} infrastructure across the region",
            "Regulated provider of {0} serving millions of customers",
            "Transitioning to clean {0} generation for a sustainable future",
            "The first major {0} provider to commit to 100% carbon-free generation by 2030",
            "A {0} utility with the industry's lowest rates and highest customer satisfaction — a rare combination",
            "Investing in grid modernization to make {0} delivery more resilient against extreme weather events",
            "The {0} utility that other utilities benchmark against. Best-in-class reliability metrics for 10 consecutive years",
            "A community-focused {0} provider that has kept rate increases below inflation every year since {1}",
            "Building the smart grid of tomorrow while keeping the lights on today. Essential {0} services since {1}",
            "A regulated {0} monopoly that acts like it has competitors — because it believes ratepayers deserve better",
        },
        ["Luxury Goods"] = new[] {
            "Crafting exquisite {0} for discerning clients worldwide",
            "A $10B {0} house with flagship boutiques on every major luxury avenue in the world",
            "Heritage house of {0} excellence since {1}",
            "Iconic {0} brand synonymous with elegance and quality",
            "Defining luxury through artisanal {0} craftsmanship",
            "Where 200 years of {0} tradition meets contemporary design. Each piece tells a story",
            "The {0} brand with a three-year waitlist for its signature collection — you can't rush perfection",
            "Quietly preferred by royalty, heads of state, and those who know: the definitive name in {0}",
            "A {0} house that refuses to discount, ever. Scarcity is the strategy. Desirability is the result",
            "Founded in {1} by a master artisan, this {0} company still hand-finishes every piece in its original atelier",
            "The {0} brand that turned exclusivity into an art form — 500 pieces per year, no exceptions",
            "A modern {0} brand that earned its place alongside century-old houses in under two decades. Critics are believers now",
        },
        ["Transportation"] = new[] {
            "Moving goods efficiently through {0} networks since {1}",
            "Operating the largest privately held {0} fleet in North America, moving 500,000 tons daily",
            "Connecting global supply chains with {0} logistics",
            "Leading provider of {0} services across major trade routes",
            "Innovating in {0} to reduce costs and carbon footprint",
            "A {0} company that has reduced delivery times by 40% through AI-powered routing and predictive analytics",
            "The {0} backbone of e-commerce: if you ordered it online, there's a good chance this company moved it",
            "Pioneering autonomous {0} technology that will reshape how goods move across the country",
            "A {0} company built on one principle: every hour a package sits in a warehouse is an hour wasted",
            "From a single truck in {1} to a continent-spanning {0} empire. The original American logistics success story",
            "The {0} company that Fortune 500 supply chain managers have on speed dial — because reliability matters most",
            "A next-generation {0} company that treats logistics as a technology problem, not just a trucking problem",
        },
    };

    // --- Era-appropriate founding story templates ---
    private static readonly string[] PreWarStoryTemplates =
    {
        "Established in {0} as a family-owned {2} enterprise, growing across generations",
        "Incorporated in {0} by {1}, building a {2} empire from a single factory",
        "Founded in {0} during the industrial boom, becoming a pillar of the {2} industry",
        "One of the oldest names in {2}, tracing its roots to {0} when {1} opened its first office",
        "In {0}, {1} converted a bankrupt {2} mill into what would become one of the nation's most influential firms",
        "Born from a handshake deal in {0}, {1} laid the foundation for a {2} dynasty that would span four generations",
        "When the railroads opened the West, {1} was there — founding this {2} company in {0} to supply the frontier",
        "Surviving the Panic of 1893 and two world wars, this {2} firm was built by {1} in {0} on grit and iron",
        "Started as a {2} trading house in {0} by immigrant entrepreneur {1}, who arrived with nothing but ambition",
        "Chartered in {0} with backing from J.P. Morgan's circle, this {2} enterprise quickly became an industry giant",
        "In the depths of the Great Depression, {1} bought a failing {2} operation in {0} for pennies on the dollar — and rebuilt it into an empire",
        "{1} walked away from a university professorship in {0} to pursue a radical new approach to {2}, founding this company in a rented warehouse",
        "Three brothers pooled their savings in {0} to start a small {2} workshop. A century later, their name is synonymous with the industry",
        "Founded in {0} to supply {2} materials to the wartime effort, then pivoted to civilian markets with devastating efficiency",
        "What began as a modest {2} storefront in {0} under {1}'s management grew into a vertically integrated powerhouse by the 1940s",
        "The {1} family patriarch established this {2} concern in {0}, insisting that quality would always outweigh cost — a philosophy the company holds to this day",
        "Forged in the furnaces of the Industrial Revolution, this {2} company was incorporated in {0} when {1} secured the firm's first government contract",
        "In {0}, {1} defied convention by hiring women and minorities to run the factory floor — building both a {2} company and a legacy of inclusion",
        "After a devastating factory fire in {0} nearly destroyed everything, {1} rebuilt the {2} business stronger, incorporating fireproof designs that became industry standard",
        "Legend has it that {1} sketched the company's first {2} product on a napkin in {0}. The patent office accepted it three weeks later",
    };
    private static readonly string[] MidCenturyStoryTemplates =
    {
        "Founded in {0} by {1}, who recognized the post-war potential of the {2} sector",
        "Established in {0} when {1} spun off a division from a larger conglomerate to focus on {2}",
        "Started in {0} as a regional {2} company, expanding nationally through strategic acquisitions",
        "Created in {0} by {1}, a former executive who saw untapped demand in the {2} market",
        "Formed in {0} when three returning veterans pooled their GI Bill savings to build a {2} company that would define the industry",
        "In {0}, {1} traded a promising career at Bell Labs for the chance to revolutionize the {2} sector",
        "Born from a Cold War government contract in {0}, this {2} firm went public and never looked back",
        "When suburbia exploded in the 1960s, {1} was ready — having founded this {2} company in {0} to serve the growing middle class",
        "Started in {0} in a strip-mall office with two employees. By the time {1} retired, the {2} company had 10,000",
        "A chance meeting at a {2} trade show in {0} led {1} to cofound what would become a Fortune 500 company",
        "Launched in {0} as a joint venture between American and European investors, pioneering transatlantic {2} commerce",
        "{1} spent a decade at a competitor before starting this {2} company in {0} — and promptly stole their best clients",
        "In {0}, the merger of two family-owned {2} businesses created a combined entity that dominated the regional market for decades",
        "When NASA needed {2} solutions in the 1960s, they called {1}'s company — founded just years earlier in {0}",
        "The oil shocks of the 1970s nearly killed this {2} firm, founded in {0} by {1}. Instead, they adapted and thrived",
        "Founded in {0} on the principle that {2} should be accessible to ordinary Americans, not just corporations",
        "What started as {1}'s side project in {0} — a {2} newsletter — grew into a full-service industry leader",
        "In {0}, {1} mortgaged the family home to fund a {2} startup. The gamble paid off within eighteen months",
        "After the breakup of AT&T, {1} seized the opportunity in {0} to build an independent {2} company from the pieces",
        "Founded in {0} by {1}, a former Peace Corps volunteer who saw a way to apply {2} solutions to developing markets — then brought those innovations home",
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
        "What began as a {0} hackathon project by {1} became a {2} juggernaut valued at billions within a decade",
        "{1} was fired on a Friday, incorporated a {2} company by Monday, and landed their first customer by Thursday. That was {0}",
        "A Stanford dorm room in {0} birthed this {2} company when {1} and a roommate decided the incumbents were asleep at the wheel",
        "Pivoted from a failed social media app in {0} to become the fastest-growing {2} company in history",
        "In {0}, {1} wrote a viral blog post about everything wrong with the {2} industry — then raised $50M to fix it",
        "Founded in {0} during the dot-com bust, this {2} company survived by being profitable from day one. A radical concept at the time",
        "Three PhDs left a hedge fund in {0} to apply quantitative methods to {2} — and discovered a goldmine",
        "After the 2008 financial crisis, {1} saw an opening in {0} to rebuild {2} from the ground up, without legacy baggage",
        "Born remote-first in {0}, this {2} company has never had a headquarters. {1} runs it from a laptop",
        "In {0}, {1} took a struggling family {2} business and reinvented it with modern technology, turning loss into a 10x growth story",
        "Founded in {0} when {1} realized that AI could transform the entire {2} value chain. The market agreed",
        "What started as a Kickstarter campaign by {1} in {0} became a legitimate {2} company after raising $4M from 60,000 backers",
        "In {0}, climate legislation created a massive opportunity. {1} founded this {2} company to capture it",
        "A Y Combinator graduate from {0}, this {2} startup went from demo day to unicorn in under three years",
        "After {1}'s previous {2} company was acqui-hired in {0}, they used the proceeds to start something bigger and bolder",
        "Founded in {0} as an open-source project by {1}, then commercialized after developers worldwide adopted the {2} platform",
        "In {0}, {1} left a senior role at a FAANG company to chase an obsession: making {2} technology available to everyone, not just the top 1%",
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
            "Every great company was once called crazy. We embrace that.",
            "Our competitors are optimizing the present. We're inventing the future.",
            "I'd rather cannibalize our own revenue than let someone else do it.",
        },
        ["Cost-Cutter"] = new[] {
            "Every dollar saved goes straight to the bottom line.",
            "Efficiency is the foundation of sustainable growth.",
            "We've eliminated waste at every level of the organization.",
            "Margins matter more than revenue growth. Our investors understand that.",
            "I've cut 30% of overhead without touching a single customer-facing role.",
            "Lean operations aren't about doing less — they're about doing what matters.",
        },
        ["Empire Builder"] = new[] {
            "This acquisition positions us as the undisputed market leader.",
            "We see enormous opportunity for consolidation in this space.",
            "Scale is our competitive moat.",
            "We're building a platform, not a product. Every acquisition extends it.",
            "In five years, there will be two or three players left. We intend to be one.",
            "Organic growth is fine. But acquisitions let us skip ahead ten years.",
        },
        ["Turnaround Artist"] = new[] {
            "The restructuring is ahead of schedule and the results speak for themselves.",
            "We inherited challenges, but we're turning them into opportunities.",
            "This company has incredible potential that was being held back.",
            "The hardest part is already behind us. Now we execute.",
            "I've done this before at three other companies. I know what works.",
            "When I arrived, this company was months from disaster. Look at us now.",
        },
        ["Founder-CEO"] = new[] {
            "I started this company to solve a problem I personally experienced.",
            "We've never lost sight of our founding mission.",
            "This isn't just a business — it's my life's work.",
            "I still remember our first customer. That drive hasn't changed.",
            "Nobody will outwork us. This team has something money can't buy.",
            "The board keeps telling me to think like a CEO. I think like a founder.",
        },
        ["Sales Machine"] = new[] {
            "Revenue growth is our north star, everything else follows.",
            "We're winning customers at a pace our competitors can't match.",
            "Our pipeline has never been stronger.",
            "We added more new logos this quarter than in the entire previous year.",
            "The sales team is the engine of this company. I invest in them first.",
            "Customer acquisition cost is down 40%. That's how you scale profitably.",
        },
        ["Engineer-CEO"] = new[] {
            "The product speaks for itself. We let the technology do the talking.",
            "We invest in R&D because that's where the breakthroughs come from.",
            "Technical excellence isn't a goal — it's a requirement.",
            "Our engineering team filed 47 patents last year. That's our moat.",
            "I still review code on weekends. The details matter.",
            "We'd rather delay a launch than ship something that isn't ready.",
        },
        ["Finance Veteran"] = new[] {
            "Disciplined capital allocation is how you create long-term shareholder value.",
            "We're managing risk while maximizing returns — that's what we do.",
            "The balance sheet has never been stronger.",
            "ROIC is the metric I care about most. Everything else is noise.",
            "We returned $2B to shareholders last year. That's what capital discipline looks like.",
            "I've managed through three recessions. We're prepared for the next one.",
        },
        ["Industry Insider"] = new[] {
            "After 25 years in this industry, I know exactly where it's heading.",
            "Our relationships and expertise give us an unfair advantage.",
            "We understand this market better than anyone.",
            "Every regulator, every supplier, every competitor — I know them personally.",
            "When the industry zigs, we zag. Because we see what's actually happening.",
            "Experience compounds. And we have more of it than anyone else.",
        },
        ["Disruptor"] = new[] {
            "The incumbents should be worried. We're rewriting the rules.",
            "Why accept the status quo when you can change it?",
            "We move fast and break things — on purpose.",
            "Every industry has a reckoning coming. We are that reckoning.",
            "Traditional players spend 80% on maintenance and 20% on innovation. We flip that.",
            "Our biggest risk isn't failure — it's not being ambitious enough.",
        },
        ["Steady Hand"] = new[] {
            "Consistency and reliability — that's what our shareholders expect from us.",
            "We don't chase trends. We execute our strategy quarter after quarter.",
            "Boring is beautiful when it comes to generating returns.",
            "Our dividend has grown for 15 consecutive years. That's what steady means.",
            "Wall Street wants volatility. Our investors want predictability. We deliver.",
            "No surprises, no drama, just compound growth. That's our promise.",
        },
        ["Dealmaker"] = new[] {
            "This deal creates value that neither company could achieve alone.",
            "We see M&A as a core competency, not a one-time event.",
            "The synergies from this combination are substantial and achievable.",
            "I've closed 14 deals in 6 years. Each one made us stronger.",
            "The best deals are the ones where both sides walk away winners.",
            "Integration is where most acquirers fail. We've built a machine for it.",
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

        // Assign supply chains: connect sectors with natural buyer/supplier relationships
        AssignSupplyChains(stocks, rng);
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

    /// <summary>
    /// Assign supply chain relationships between sectors.
    /// Real-world flows: Materials → Industrials → Consumer Goods,
    /// Energy → Transportation, Technology → Financials, etc.
    /// Each stock gets 1-3 suppliers and 1-3 customers.
    /// </summary>
    private static void AssignSupplyChains(IList<Stock> stocks, Random rng)
    {
        // Natural sector supply chain relationships: Supplier → Customer
        var supplyChainMap = new Dictionary<string, string[]>
        {
            ["Materials"] = new[] { "Industrials", "Technology", "Consumer Goods" },
            ["Energy"] = new[] { "Transportation", "Industrials", "Utilities" },
            ["Technology"] = new[] { "Financials", "Healthcare", "Telecommunications" },
            ["Industrials"] = new[] { "Consumer Goods", "Real Estate", "Transportation" },
            ["Healthcare"] = new[] { "Consumer Goods" },
            ["Financials"] = new[] { "Real Estate", "Consumer Goods" },
            ["Telecommunications"] = new[] { "Technology", "Financials" },
            ["Utilities"] = new[] { "Real Estate", "Industrials" },
        };

        var bySector = stocks
            .Where(s => s.Personality != null && !s.Traits.Contains("ETF"))
            .GroupBy(s => s.Sector)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var stock in stocks)
        {
            if (stock.Personality == null || stock.Traits.Contains("ETF")) continue;

            // Find potential suppliers (sectors that supply to this stock's sector)
            foreach (var (supplierSector, customerSectors) in supplyChainMap)
            {
                if (!customerSectors.Contains(stock.Sector)) continue;
                if (!bySector.TryGetValue(supplierSector, out var potentialSuppliers)) continue;

                // 50% chance to connect with each matching supplier sector
                if (rng.NextDouble() > 0.5) continue;

                var supplier = potentialSuppliers[rng.Next(potentialSuppliers.Count)];
                if (supplier.Symbol == stock.Symbol) continue;
                if (stock.Personality.Suppliers.Contains(supplier.Symbol)) continue;
                if (stock.Personality.Suppliers.Count >= 3) break;

                // Keep the relationship bidirectional: only link if the supplier can also record this
                // company as its customer — otherwise the stock would list a supplier that doesn't list it back.
                if (supplier.Personality == null
                    || (supplier.Personality.Customers.Count >= 3 && !supplier.Personality.Customers.Contains(stock.Symbol)))
                    continue;

                stock.Personality.Suppliers.Add(supplier.Symbol);
                if (!supplier.Personality.Customers.Contains(stock.Symbol))
                    supplier.Personality.Customers.Add(stock.Symbol);
            }
        }

        var totalLinks = stocks.Sum(s => s.Personality?.Suppliers.Count ?? 0);
        // Log is static, can't use instance logger — silent assignment
    }
}
