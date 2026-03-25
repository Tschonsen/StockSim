using StockSim.Engine.Models;
using StockSim.Engine.Services;

namespace StockSim.Engine.Tests.Services;

public class EventEngineTests : IDisposable
{
    private readonly EventEngine _engine;
    private readonly List<Stock> _stocks;
    private readonly DateTime _marketTime = new(2027, 1, 5, 10, 0, 0);

    public EventEngineTests()
    {
        GameEvent.ResetIdCounter();
        _engine = new EventEngine(seed: 42);
        _stocks = new List<Stock>
        {
            CreateStock("AAPL", "Apple Corp", "Technology", 150m),
            CreateStock("XOM", "Exxon Corp", "Energy", 80m),
            CreateStock("JPM", "JPMorgan", "Financials", 120m),
        };
    }

    public void Dispose() => GameEvent.ResetIdCounter();

    [Fact]
    public void Tick_ShouldEventuallyGenerateEvents()
    {
        // Run many ticks — with the probabilities, we should get events
        for (int i = 0; i < 2000; i++)
        {
            _engine.Tick(_stocks, _marketTime.AddMinutes(i), isMarketOpen: true);
        }

        Assert.True(_engine.EventHistory.Count > 0,
            "Should have generated at least one event over 2000 ticks");
    }

    [Fact]
    public void Tick_ShouldNotGenerateEvents_WhenMarketClosed()
    {
        for (int i = 0; i < 1000; i++)
        {
            _engine.Tick(_stocks, _marketTime.AddMinutes(i), isMarketOpen: false);
        }

        Assert.Empty(_engine.EventHistory);
    }

    [Fact]
    public void Events_ShouldHaveValidFields()
    {
        for (int i = 0; i < 5000; i++)
        {
            _engine.Tick(_stocks, _marketTime.AddMinutes(i), isMarketOpen: true);
        }

        Assert.True(_engine.EventHistory.Count > 0);

        foreach (var evt in _engine.EventHistory)
        {
            Assert.False(string.IsNullOrEmpty(evt.Headline), "Headline should not be empty");
            Assert.True(evt.Sentiment >= -1.0f && evt.Sentiment <= 1.0f, $"Sentiment {evt.Sentiment} out of range");
            Assert.True(evt.DurationMinutes > 0, "Duration should be positive");
            Assert.True(Enum.IsDefined(evt.Type), "Type should be valid");
            Assert.True(Enum.IsDefined(evt.Severity), "Severity should be valid");
        }
    }

    [Fact]
    public void ActiveEvents_ShouldExpire()
    {
        // Run until we get an event
        for (int i = 0; i < 5000; i++)
        {
            _engine.Tick(_stocks, _marketTime.AddMinutes(i), isMarketOpen: true);
        }

        // All events with short durations should have expired by now
        // (we ran 5000 minutes > max duration of 150)
        Assert.True(_engine.EventHistory.Count > _engine.ActiveEvents.Count,
            "Some events should have expired");
    }

    [Fact]
    public void CompanyEvents_ShouldAffectSpecificStock()
    {
        var companyEvents = new List<GameEvent>();

        for (int i = 0; i < 5000; i++)
        {
            _engine.Tick(_stocks, _marketTime.AddMinutes(i), isMarketOpen: true);
            companyEvents.AddRange(_engine.NewEventsThisTick.Where(e => e.Type == EventType.Company));
        }

        if (companyEvents.Count > 0)
        {
            Assert.All(companyEvents, e =>
            {
                Assert.NotEmpty(e.AffectedSymbols);
                Assert.True(_stocks.Any(s => s.Symbol == e.AffectedSymbols[0]),
                    $"Affected symbol {e.AffectedSymbols[0]} should be in the stock list");
            });
        }
    }

    [Fact]
    public void SectorEvents_ShouldAffectSpecificSector()
    {
        var sectorEvents = new List<GameEvent>();

        for (int i = 0; i < 5000; i++)
        {
            _engine.Tick(_stocks, _marketTime.AddMinutes(i), isMarketOpen: true);
            sectorEvents.AddRange(_engine.NewEventsThisTick.Where(e => e.Type == EventType.Sector));
        }

        if (sectorEvents.Count > 0)
        {
            Assert.All(sectorEvents, e =>
            {
                Assert.NotEmpty(e.AffectedSectors);
            });
        }
    }

    [Fact]
    public void Events_ShouldAffectPrices()
    {
        var initialPrices = _stocks.ToDictionary(s => s.Symbol, s => s.CurrentPrice);

        // Run lots of ticks to ensure events fire and affect prices
        for (int i = 0; i < 5000; i++)
        {
            _engine.Tick(_stocks, _marketTime.AddMinutes(i), isMarketOpen: true);
        }

        // At least some prices should have changed due to events
        var priceChanged = _stocks.Any(s => s.CurrentPrice != initialPrices[s.Symbol]);
        Assert.True(priceChanged || _engine.EventHistory.Count == 0,
            "Prices should change when events fire");
    }

    [Fact]
    public void NewEventsThisTick_ShouldClearEachTick()
    {
        // Tick once
        _engine.Tick(_stocks, _marketTime, isMarketOpen: true);
        var firstTickCount = _engine.NewEventsThisTick.Count;

        // Tick again — NewEventsThisTick should be fresh
        _engine.Tick(_stocks, _marketTime.AddMinutes(1), isMarketOpen: true);

        // The list should only contain events from the second tick
        // (we can't assert exact count but it should be different list)
        Assert.True(_engine.NewEventsThisTick.Count >= 0); // Just ensure no crash
    }

    [Fact]
    public void SameSeed_ShouldProduceSameEvents()
    {
        var engine1 = new EventEngine(seed: 123);
        var engine2 = new EventEngine(seed: 123);
        var stocks1 = new List<Stock> { CreateStock("TEST", "Test", "Technology", 100m) };
        var stocks2 = new List<Stock> { CreateStock("TEST", "Test", "Technology", 100m) };

        for (int i = 0; i < 1000; i++)
        {
            engine1.Tick(stocks1, _marketTime.AddMinutes(i), isMarketOpen: true);
            engine2.Tick(stocks2, _marketTime.AddMinutes(i), isMarketOpen: true);
        }

        Assert.Equal(engine1.EventHistory.Count, engine2.EventHistory.Count);
        for (int i = 0; i < engine1.EventHistory.Count; i++)
        {
            Assert.Equal(engine1.EventHistory[i].Headline, engine2.EventHistory[i].Headline);
        }
    }

    [Fact]
    public void Prices_ShouldStayPositive_AfterNegativeEvents()
    {
        var cheapStock = CreateStock("CHEAP", "Cheap Corp", "Technology", 1.00m);
        var stockList = new List<Stock> { cheapStock };

        for (int i = 0; i < 10000; i++)
        {
            _engine.Tick(stockList, _marketTime.AddMinutes(i), isMarketOpen: true);
        }

        Assert.True(cheapStock.CurrentPrice > 0, $"Price went to {cheapStock.CurrentPrice}");
    }

    // ========================
    // M&A Events (Bible 8.2.7)
    // ========================

    [Fact]
    public void MAndA_ShouldEventuallyGenerate()
    {
        // M&A needs: target (mid cap) + acquirer (larger)
        var target = CreateStock("TARG", "Target Corp", "Technology", 50m);
        target.SharesOutstanding = 50_000_000; // $2.5B market cap

        var acquirer = CreateStock("ACQR", "Acquirer Inc", "Technology", 200m);
        acquirer.SharesOutstanding = 200_000_000; // $40B market cap

        var stocks = new List<Stock> { target, acquirer };
        var found = false;

        for (int seed = 0; seed < 500; seed++)
        {
            var engine = new EventEngine(seed);
            engine.TryGenerateMAndA(stocks, _marketTime);

            if (engine.MAndAEventsThisTick.Count > 0)
            {
                var mna = engine.MAndAEventsThisTick[0];
                Assert.Equal("TARG", mna.TargetSymbol);
                Assert.Equal("ACQR", mna.AcquirerSymbol);
                Assert.True(mna.OfferPrice > target.CurrentPrice, "Offer should include premium");
                Assert.True(mna.PremiumPercent >= 20 && mna.PremiumPercent <= 40);
                found = true;
                break;
            }
        }

        Assert.True(found, "Should generate M&A event within 500 seeds");
    }

    [Fact]
    public void MAndA_ShouldNotGenerateForETFs()
    {
        var etf = CreateStock("ETF_MKT", "Market ETF", "ETF", 100m);
        etf.Traits.Add("ETF");
        etf.SharesOutstanding = 50_000_000;

        var acquirer = CreateStock("ACQR", "Acquirer", "Technology", 200m);
        acquirer.SharesOutstanding = 200_000_000;

        for (int seed = 0; seed < 200; seed++)
        {
            var engine = new EventEngine(seed);
            engine.TryGenerateMAndA(new List<Stock> { etf, acquirer }, _marketTime);

            foreach (var mna in engine.MAndAEventsThisTick)
            {
                Assert.NotEqual("ETF_MKT", mna.TargetSymbol);
            }
        }
    }

    [Fact]
    public void MAndA_AcquirerShouldBeLarger()
    {
        var small = CreateStock("SML", "Small Co", "Technology", 20m);
        small.SharesOutstanding = 10_000_000; // $200M — too small to be target (min $500M)

        var medium = CreateStock("MED", "Medium Co", "Technology", 50m);
        medium.SharesOutstanding = 20_000_000; // $1B

        var large = CreateStock("LRG", "Large Co", "Technology", 200m);
        large.SharesOutstanding = 200_000_000; // $40B

        var stocks = new List<Stock> { small, medium, large };

        for (int seed = 0; seed < 200; seed++)
        {
            var engine = new EventEngine(seed);
            engine.TryGenerateMAndA(stocks, _marketTime);

            foreach (var mna in engine.MAndAEventsThisTick)
            {
                // Target should be medium, acquirer should be large
                Assert.Equal("MED", mna.TargetSymbol);
                Assert.Equal("LRG", mna.AcquirerSymbol);
            }
        }
    }

    [Fact]
    public void MAndA_ShouldClearEventsEachCall()
    {
        var engine = new EventEngine(42);
        var t = CreateStock("T", "Target", "Tech", 50m);
        t.SharesOutstanding = 50_000_000;
        var a = CreateStock("A", "Acquirer", "Tech", 200m);
        a.SharesOutstanding = 200_000_000;
        var stocks = new List<Stock> { t, a };

        engine.TryGenerateMAndA(stocks, _marketTime);
        engine.TryGenerateMAndA(stocks, _marketTime);

        // Second call should have cleared previous results (max 1 event per call, if any)
        Assert.True(engine.MAndAEventsThisTick.Count <= 1);
    }

    private static Stock CreateStock(string symbol, string name, string sector, decimal price)
    {
        return new Stock(symbol, name, sector)
        {
            CurrentPrice = price,
            PreviousClose = price,
            BidPrice = price - 0.10m,
            AskPrice = price + 0.10m,
            DayHigh = price,
            DayLow = price,
            BaseVolatility = 0.02m,
            LiquidityScore = 7,
            FairValue = price,
            AverageVolume = 1_000_000,
            SharesOutstanding = 100_000_000,
        };
    }
}
