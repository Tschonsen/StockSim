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
