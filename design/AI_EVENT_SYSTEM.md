# StockSim — AI & Event System Masterplan

> Das Spiel muss durch Größe, Variation und Tiefe beeindrucken.
> Aufgeteilt in Teil 1 (Foundation) und Teil 2 (Spieler als Akteur).

---

## ARCHITEKTUR-ÜBERBLICK

```
         ┌──────────────────────────────────────┐
         │          MARKET DIRECTOR             │
         │    Orchestriert alles, bidirektional  │
         └──────────────┬───────────────────────┘
                        │
          ┌─────────────┼──────────────┬───────────────┐
          ▼             ▼              ▼               ▼
     PriceModel    EventEngine    NarrativeEng    CompanyEng
     (ONNX)       (Tier 1-4)    (Story-States)   (Dynamisch)
          │             │              │               │
          └─────────────┴──────────────┴───────────────┘
                        │
                 Bidirektionaler Loop:
                 Events → beeinflussen Preise
                 Preise → triggern Events
                 Narrative → verhindert Unsinn
                 Companies → erzeugen Events
```

---

## TEIL 1 — FOUNDATION

> Ziel: Massiv mehr Content, tiefere Events, smartere Preise.
> Der Spieler ist noch primär Beobachter/Reagierer, aber mit 100x mehr Substanz.

### 1.1 Erweitertes Event-Model

GameEvent bekommt volle Tiefe — jedes Event ist ein Mini-Bloomberg-Artikel:

```csharp
public class GameEvent  // ERWEITERT
{
    // --- Bestehend ---
    public long Id;
    public EventType Type;           // Macro, Sector, Company
    public EventSeverity Severity;   // Minor, Moderate, Major
    public float Sentiment;          // -1.0 bis +1.0
    public float PriceEffect;
    public float VolatilityMultiplier;
    public float VolumeMultiplier;
    public int DurationMinutes;
    public int RemainingMinutes;
    public DateTime TriggeredAt;
    public string Headline;
    public List<string> AffectedSymbols;
    public List<string> AffectedSectors;

    // --- NEU: Content-Tiefe ---
    public string Summary;               // 2-3 Sätze Kontext
    public string AnalystQuote;          // Fiktiver Experte mit Einschätzung
    public string AnalystName;           // "Sarah Chen, Atlantic Research"
    public string AnalystFirm;           // "Goldman Sachs"
    public string HistoricalParallel;    // "Similar to 1973 Oil Embargo..."
    public List<string> WhatToWatch;     // Bullet Points

    // --- NEU: Tier-System ---
    public int Tier;                     // 1-4
    public List<string> Tags;           // ["geopolitics", "energy", "supply-shock"]
    public string? Season;              // "Q1", "Q4", null
    public string? RequiresPhase;       // "bear", "bull", null
    public string? RequiresMarketCap;   // "large", "small", null

    // --- NEU: Differenzierte Impacts ---
    public Dictionary<string, SectorImpact> SectorImpacts;
    public List<AffectedCompany> DetailedImpacts;

    // --- NEU: Follow-Ups ---
    public List<FollowUpScenario> PossibleOutcomes;

    // --- NEU: Arc-Referenz ---
    public string? ArcId;               // Gehört zu welchem Arc?
    public int? ArcPhase;               // Welche Phase im Arc?
    public string? ArcPath;             // Welcher Pfad? (A/B/C)
}
```

### 1.2 Event Tier-System + Difficulty-Filter

4 Tiers, gesteuert durch Schwierigkeit:

```
TIER 1 — ALLTAG (alle Schwierigkeiten)
  Earnings, Analyst Up/Downgrades, CEO-Wechsel, Buybacks,
  Dividend Changes, Partnerships, Product Launches, Insider Filing
  → 3-6 pro Tag, ±1-5% Impact
  → ~1.500 Templates × 3-5 Headlines = ~6.000 Varianten

TIER 2 — MARKANT (ab Normal)
  FDA Decisions, Data Breach, Hostile Takeover, Accounting Fraud,
  Short Reports, Activist Investor, Major Contract, Credit Downgrade
  → 1-3 pro Woche, ±5-15% Impact
  → ~800 Templates × 3-5 Headlines = ~3.000 Varianten

TIER 3 — KRISEN (ab Hard)
  Sektor-Crash, Bank Run, Commodity Shock, Währungskrise,
  Trade War, Mega-Merger, Regulatory Crackdown
  → 1-3 pro Monat, ±10-30% Impact
  → ~50 Mini-Arcs mit 2 Pfaden, je 2-3 Phasen
  → ~300-500 Messages

TIER 4 — BLACK SWANS (ab Brutal + als Szenarien spielbar)
  Historische Mega-Events mit Multi-Wochen-Arcs
  → 0-2 pro Spieljahr, ±20-70% über Wochen
  → 15 große Arcs mit je 2-4 Pfaden, je 3-5 Phasen
  → ~540 Messages
```

Difficulty steuert:
- Welche Tiers aktiv sind
- Frequenz-Multiplikatoren pro Tier
- Negativ-Bias (Easy: 50/50, Hard: 60% negativ, Brutal: 70%)
- Cascade-Wahrscheinlichkeit (Brutal: 1.5x)
- Recovery-Geschwindigkeit (Brutal: 0.5x)
- Max negative Severity (Easy: -5% cap)

### 1.3 Event-Arc-System mit Branching

Multi-Phasen-Arcs die sich über Tage/Wochen entfalten:

```
Phase 1 (Setup) → Phase 2 (Aufbau) → BRANCH POINT
                                          │
                            ┌─────────────┼─────────────┐
                            ▼             ▼             ▼
                        Path A        Path B        Path C
                       (Erfolg)     (Versandet)    (Eskalation)
                          │             │             │
                       Phase 3-5     Phase 3-4     Phase 3-6
```

Pfad-Wahrscheinlichkeiten sind DYNAMISCH basierend auf:
- Aktuelle Marktphase (Bull/Bear)
- Makro-Indikatoren (Zinsen, Inflation)
- Vorherige Events und aktive Arcs
- Sektor-Performance der letzten Wochen

15 Tier-4 Arcs:
1. Meme Stock Squeeze (3 Pfade: Squeeze/Fizzle/Trap)
2. Lehman Moment (3 Pfade: Bailout/Collapse/Slow Bleed)
3. Terror/Krieg Aftermath (3 Pfade: Quick Recovery/Prolonged/Escalation)
4. Pandemie (3 Pfade: V-Recovery/L-Shape/W-Shape)
5. Tech Bubble Burst (2 Pfade: Orderly/Crash)
6. Oil Shock (3 Pfade: Resolves/War/Demand Collapse)
7. Currency Crisis (3 Pfade: IMF Bailout/Default/Contagion)
8. AI Revolution (2 Pfade: Real Shift/Bubble)
9. Flash Crash 2.0 (2 Pfade: Recovery/Systemic Break)
10. Sovereign Default (3 Pfade: Restructuring/Cascade/Political Fix)
11. Corporate Mega-Fraud (2 Pfade: Isolated/Systemic)
12. Regulatory Earthquake (2 Pfade: Adapts/Destroyed)
13. Natural Catastrophe (2 Pfade: Recovery/Prolonged)
14. Trade War Escalation (3 Pfade: Deal/Cold War/Hot Conflict)
15. Bank Run / Liquidity Crisis (3 Pfade: Fed Saves/Controlled/Panic)

Alle 15 auch als spielbare Historical Scenarios im Hauptmenü.

### 1.4 ONNX Preismodell

Ersetzt/ergänzt Brownian Motion in PriceEngine:

```
Training (einmalig, Python):
  - Daten: Yahoo Finance, 500 Aktien, 5 Jahre
  - Features: Preis-History, Sektor, MarketCap, Vola, Event-Sentiment
  - Architektur: kleiner LSTM oder Transformer (~100K-1M Parameter)
  - Export: ONNX (~500KB)

Runtime (C#, OnnxRuntime):
  Input:  float[30] = [letzte 20 Returns, Sektor-Encoding, MarketCap,
                        Vola, Event-Sentiment, Event-Count,
                        Sector-Sentiment, Macro-Phase, Days-Since-Event]
  Output: float[2]  = [expected_return, expected_volatility]
  Speed:  <0.1ms pro Stock
```

Was es lernt:
- Tech-Small-Cap bewegt sich anders als Energy-Large-Cap
- Volatility-Clustering (hohe Vola → morgen auch hohe Vola)
- Earnings-Jumps, Momentum, Mean-Reversion
- Event-Reaktionsmuster (Gap Down → Slow Recovery)

### 1.5 MarketDirector (Orchestrator)

Neuer zentraler Service der alles koordiniert:

```csharp
public class MarketDirector
{
    private readonly PriceModel _priceModel;
    private readonly EventEngine _eventEngine;
    private readonly NarrativeEngine _narrative;

    public void Tick(List<Stock> stocks, DateTime gameTime)
    {
        // 1. Marktkontext sammeln
        var context = BuildMarketContext(stocks, _eventEngine.ActiveEvents);

        // 2. Events generieren (tier-basiert, difficulty-gefiltert)
        var newEvents = _eventEngine.Generate(context);

        // 3. Arcs vorantreiben (Phasen, Branches evaluieren)
        _narrative.AdvanceArcs(context);

        // 4. Preise berechnen MIT Event-Kontext
        foreach (var stock in stocks)
        {
            var stockCtx = BuildStockContext(stock, newEvents, context);
            var (ret, vol) = _priceModel.Predict(stockCtx);
            stock.CurrentPrice *= (1 + ret);
            stock.Volatility = vol;
        }

        // 5. Preis-Trigger prüfen → reaktive Events
        var reactive = CheckPriceTriggers(stocks);
        // Stock -30% → "Activist Investor steigt ein"
        // Sektor +40% in 3mo → "Bubble Warning"
        // ATH → "Insider Selling gemeldet"
        _eventEngine.Inject(reactive);

        // 6. Narrative-Kohärenz prüfen
        _narrative.ValidateState(stocks);
    }
}
```

### 1.6 NarrativeEngine

Verhindert Unsinn und steuert Story-Kohärenz:
- Jede Firma hat einen Story-State (Normal, InCrisis, BeingAcquired, UnderInvestigation...)
- Story-States verhindern widersprüchliche Events
- Story-States beeinflussen welche Events möglich sind
- Arcs registrieren sich bei der NarrativeEngine

### 1.7 Content-Generierung (Offline)

LLM generiert einmalig alle Templates als JSON:

```
data/
  events/
    tier1/
      earnings.json        (~200 Templates)
      analyst_actions.json (~150 Templates)
      management.json      (~150 Templates)
      products.json        (~200 Templates)
      corporate.json       (~200 Templates)
      insider.json         (~100 Templates)
      sector_rotation.json (~100 Templates)
      dividends.json       (~100 Templates)
      misc.json            (~300 Templates)
    tier2/
      regulatory.json      (~100 Templates)
      fraud_scandal.json   (~80 Templates)
      ma_takeover.json     (~100 Templates)
      short_activist.json  (~80 Templates)
      crisis.json          (~100 Templates)
      breakthrough.json    (~100 Templates)
      legal.json           (~80 Templates)
      misc.json            (~160 Templates)
    tier3/
      sector_crashes.json  (~15 Arcs)
      commodity_shocks.json(~10 Arcs)
      financial_stress.json(~15 Arcs)
      regulatory.json      (~10 Arcs)
    tier4/
      meme_squeeze.json
      lehman_moment.json
      terror_aftermath.json
      pandemic.json
      tech_bubble.json
      oil_shock.json
      currency_crisis.json
      ai_revolution.json
      flash_crash.json
      sovereign_default.json
      mega_fraud.json
      regulatory_quake.json
      natural_catastrophe.json
      trade_war.json
      bank_run.json
  analysts/
    analysts.json          (200+ fiktive Analysten mit Firm + Titel)
  models/
    price_model.onnx       (~500KB trainiertes Modell)
```

### 1.8 Frontend: News wird Bloomberg-Terminal

News-Tab bekommt Detail-View:
- Headline-Liste (wie jetzt, aber mit Tier-Badges)
- Klick auf Event → Full Detail Panel:
  - Summary, Analyst Quote, Sector Impact Map
  - Affected Companies mit Begründung
  - Historical Parallel
  - What to Watch
  - Follow-Up Szenarien mit Wahrscheinlichkeiten

### 1.9 Umsetzungsreihenfolge Teil 1

```
Phase 1A — Model & Tier System
  ① GameEvent Model erweitern
  ② EventTierConfig + Difficulty-Filter
  ③ SectorImpact, AffectedCompany, FollowUpScenario Models
  ④ EventArc, ArcPhase, ArcBranch, ArcPath Models

Phase 1B — Content Generation
  ⑤ Tier-1 Templates generieren (~1.500)
  ⑥ Tier-2 Templates generieren (~800)
  ⑦ Analyst-Pool generieren (200+)
  ⑧ Tier-3 Mini-Arcs generieren (~50)
  ⑨ Tier-4 Mega-Arcs generieren (15)

Phase 1C — Engine Integration
  ⑩ EventEngine refactorn: Template-Loading, Tier-Filter
  ⑪ NarrativeEngine: Story-States, Arc-Management
  ⑫ MarketDirector: Orchestration, Preis-Trigger
  ⑬ Bidirektionaler Loop: Events ↔ Preise

Phase 1D — Price Model
  ⑭ Python: Trainingsdaten sammeln + aufbereiten
  ⑮ Modell trainieren + ONNX Export
  ⑯ C# OnnxRuntime Integration
  ⑰ PriceEngine Hybrid: ONNX + Fallback auf GBM

Phase 1E — Frontend
  ⑱ News Detail View (Bloomberg-Style)
  ⑲ Sector Impact Visualisierung
  ⑳ Historical Scenarios in Szenario-Auswahl
```

---

## TEIL 2 — SPIELER ALS AKTEUR

> Ziel: Der Spieler ist nicht mehr Zuschauer sondern Teil der Welt.
> Seine Entscheidungen beeinflussen den Verlauf. Information hat Zeitdimensionen.
> Das Spiel kennt den Spieler und reagiert auf ihn.

### 2.1 Interaktive Events — Spieler-Entscheidungen

Bestimmte Events bieten dem Spieler Wahlmöglichkeiten:

```
TENDER OFFER:
  Du hältst Aktien → [Tender] [Hold Out] [Buy More] [Short Acquirer]
  Jede Wahl hat unterschiedliche Outcomes + Wahrscheinlichkeiten

SHAREHOLDER VOTE:
  Du bist Major Shareholder (>5%) → Abstimmung über:
  → M&A Deals, CEO-Wechsel, Stock Splits, Buyback Programs
  → Deine Stimme hat Gewicht proportional zu deinem Anteil

SECONDARY OFFERING:
  Firma bietet dir Aktien zum Discount → [Participate] [Pass]

ACTIVIST DEFENSE:
  Du bist investiert, Activist greift an →
  [Side with Activist] [Support Management] [Sell and Run]

CRISIS RESPONSE:
  Deine Top-Holding crasht -30% →
  [Double Down] [Cut Losses] [Hedge with Options] [Wait and See]
  Nicht nur Trading-Buttons sondern kontextuelle Strategie-Wahl
```

Nicht JEDES Event ist interaktiv — nur ~10-15% bei denen der Spieler
direkt betroffen ist. Sonst wird es nervig.

### 2.2 Reputation & Markt-Einfluss

Der Spieler hat ein Profil in der Spielwelt:

```
MARKET INFLUENCE (0-100):
  Steigt durch: große Positionen, profitable Trades, Volumen
  Effekte:
    20+: Broker gibt bessere Margin-Konditionen
    40+: Deine großen Trades bewegen den Preis leicht (Market Impact)
    60+: Analysten erwähnen dich: "Notable: large position built"
    80+: Shareholder Votes, Board Letters, CEO reagiert auf dich
    90+: Front-Running durch AI-Trader (sie kopieren dich)

SEC SCRUTINY (0-100):
  Steigt durch: gut getimte Trades vor Events, häufiges Trading
                in Stocks die dann News haben, SMA-Violations
  Effekte:
    30+: SMA schaut genauer hin
    50+: Trades werden verzögert verarbeitet (Extra-Prüfung)
    70+: Investigation bei verdächtigen Mustern
    90+: Trading-Sperre möglich + Geldstrafe

ANALYST COVERAGE:
  Bei hohem Influence wirst du selbst zum "Smart Money Signal"
  → AI-Trader reagieren auf deine Trades
  → Preis bewegt sich wenn du kaufst (self-fulfilling)
```

### 2.3 Supply Chain Netzwerk

Firmen sind über Lieferketten verbunden:

```
Chip Manufacturer → Phone Maker → Retailer
                 → Car Maker   → Insurance
Raw Materials    → Manufacturer → Consumer

Events propagieren ZEITVERSETZT durch die Kette:
  Tag 1: Earthquake → Raw Materials -8%
  Tag 3: Manufacturer -4% (Lieferengpass)
  Tag 5: Consumer Brand -2% (Produktverzögerung)
  Tag 7: RIVAL Manufacturer +5% (übernimmt Aufträge)
```

Jede Firma hat:
- Suppliers (wer liefert mir zu)
- Customers (wem liefere ich)
- Competitors (wer profitiert wenn ich leide)

Spieler die Supply Chains verstehen → handeln VOR der Kaskade.

### 2.4 Whisper Network — Information hat Zeitdimensionen

Nicht alle Information kommt gleichzeitig:

```
INFORMATIONS-STUFEN:

  TAG -5: [SEC FILING]   CEO verkauft Aktien
          → Steht im Company Profile, kein News-Event
          → Nur sichtbar wenn Spieler aktiv nachschaut

  TAG -3: [RUMOR]         "Might miss earnings"
          → RumorEngine (bereits vorhanden), 70/30 wahr/falsch
          → Im Rumor-Tab sichtbar

  TAG -1: [WHISPER]       Analyst senkt PT leise
          → Kleine Notiz im Company Detail
          → Kein Headline, kein Sound

  TAG  0: [BREAKING NEWS] "Company misses earnings by 30%"
          → Volle Headline, Sound, Ticker
          → Jetzt weiß es jeder → Crash

  TAG +1: [DEEP ANALYSIS] Detaillierter Artikel
          → Bloomberg-Style mit allen Details
          → What to Watch, Historical Parallel

Der aufmerksame Spieler handelt bei TAG -5.
Der Durchschnittsspieler reagiert bei TAG 0.
→ Skill-Expression durch Informationsvorsprung.
```

### 2.5 Politische Simulation

Die Welt außerhalb des Markts beeinflusst alles:

```
WAHLEN (alle 2 Spieljahre):
  → 2 Kandidaten mit unterschiedlichen Plattformen
  → Polls schwanken → Sektoren reagieren auf Umfragen
  → Wahlnacht: Sektoraler Shift je nach Ergebnis
  → Gesetzgebung in den Monaten danach

FED-MEETINGS (alle 6 Spielwochen):
  → Vorher: Spekulationen, Analyst-Quotes, Bond-Markt-Signale
  → Entscheidung: Hike / Hold / Cut + Statement-Nuancen
  → "Dovish Hold" vs "Hawkish Hold" macht den Unterschied
  → Dot Plot, Forward Guidance, Pressekonferenz-Zitate

SAISONALITÄT:
  → Januar-Effekt (Small Caps outperformen)
  → Sell in May (Sommer-Schwäche)
  → Q4 Holiday Shopping (Retail-Boost)
  → Earnings Seasons (Jan/Apr/Jul/Oct Volatilitätsspike)
  → Tax Loss Harvesting (Dezember Verkaufsdruck)
  → Window Dressing (Quartalsende Rebalancing)
  → Triple/Quadruple Witching Days (Options-Expiration Vola)
```

### 2.6 Dynamische Firmen-Evolution

Firmen die sich über die Spielzeit verändern:

```
CEO-DYNAMIK:
  CEO hat Archetype der Strategie bestimmt:
  → Visionary:     R&D hoch, Vola hoch, Wachstum hoch
  → Cost-Cutter:   Margins steigen, Innovation sinkt
  → Empire Builder: Acquisitions, Schulden steigen
  → Turnaround:    Restrukturierung, kurzfristig schmerzhaft

  CEO kann gefeuert werden (bei schlechter Performance)
  → Board sucht neuen CEO → anderer Archetype
  → Firma ändert Strategie → Kurs reagiert über Monate

PRODUCT LIFECYCLE:
  Jede Firma hat 3-5 Produkte mit Lebenszyklus:
  → R&D → Launch → Growth → Mature → Decline
  → Produkt-Erfolg/Misserfolg erzeugt Events
  → Neue Produkte werden dynamisch generiert
  → Pipeline sichtbar im Company Profile

FINANCIAL TRAJECTORY:
  Fundamentals ändern sich dynamisch:
  → Revenue, Earnings, Margins, Debt, Employees
  → Beeinflusst durch CEO-Typ, Events, Marktphase
  → Analyst-Ratings passen sich an
```

### 2.7 Illegale Handlungen & Marktmanipulation

> Querschnittsthema: Entsteht aus dem Zusammenspiel von Whisper Network + Reputation + Supply Chains + SMA.
> Der Spieler soll BEWUSST WÄHLEN können ob er legal oder illegal handelt — mit echtem Risiko.
> Design-Prinzip: "Crime Pays... Until It Doesn't"

#### Aktive Spieler-Manipulationen (Spieler tut es bewusst)

```
1. INSIDER TRADING (Whisper Network + SMA)
   Opportunity: Whisper Network liefert Tag -5 Info (SEC Filing, Rumor)
   Aktion:     Spieler handelt VOR öffentlicher News
   Profit:     +10-30% wenn er richtig liegt
   Risiko:     SMA erkennt Timing-Korrelation → Investigation
   Skill:      Alles auf einmal = verdächtig. Über 3 Tage verteilt = safer.
               Korrelierte Stocks/ETFs kaufen statt direkt = fast unerkennbar.
   Systeme:    C2.4 Whisper + SMAEngine + C2.2 Scrutiny

2. PUMP & DUMP (Reputation + SMA)
   Opportunity: Spieler hat Influence 60+ → Trades bewegen den Markt
   Aktion:     Massiv Small-Cap kaufen → AI-Trader kopieren → Preis steigt → Verkaufen
   Profit:     +20-40% auf Small-Caps
   Risiko:     SMA sieht Buy→Rise→Quick Sell Muster
   Skill:      Langsam akkumulieren vs. auffällig. Verkauf timing.
   Systeme:    C2.2 Reputation (Influence → Market Impact) + SMAEngine

3. FRONT-RUNNING (AI-Trader-Muster erkennen)
   Opportunity: Spieler erkennt: Pension Funds kaufen jeden Quartalsanfang
   Aktion:     2 Tage vorher kaufen, nach Pension-Pump verkaufen
   Profit:     +3-8% zuverlässig
   Risiko:     Grauzone — kein klares Verbot, aber SMA beobachtet Muster
   Skill:      Erkennung der 14 AI-Trader-Patterns (Window Dressing, Rebalancing)
   Systeme:    AITraderEngine (sichtbare Muster) + SMAEngine

4. SPOOFING (Orderbook-Manipulation)
   Opportunity: Spieler platziert große Limit-Orders weit vom Preis
   Aktion:     Fake Buy-Order bei $48 → Orderbook zeigt Demand → andere kaufen → Cancel
   Profit:     Indirekt durch Preisbewegung
   Risiko:     SMA: Hohe Cancel-Rate bei großen Orders
   Skill:      Order-Größe und Timing variieren
   Systeme:    OrderEngine + OrderbookGenerator + SMAEngine (bereits implementiert)

5. CORNERING THE MARKET (Supply Chain + Float-Kontrolle)
   Opportunity: Small-Cap mit niedrigem Float
   Aktion:     Langsam >20% des Floats kaufen → Angebot knapp → Preis steigt
   Profit:     Kontrolliert den Preis — kann Short Squeeze erzwingen
   Risiko:     SMA: Konzentrierte Position + ungewöhnliche Preisbewegung
   Skill:      Langsam akkumulieren über Wochen vs. auffällig
   Systeme:    C2.3 Supply Chain (Float-Kontrolle) + SMAEngine

6. BEAR RAID (Aggressives Shorten)
   Opportunity: Firma mit schwachen Fundamentals + hohem Debt
   Aktion:     Massiv shorten → Preis fällt → Stop-Losses anderer triggern → Kaskade
   Profit:     +30-60% auf Shorts wenn Kaskade klappt
   Risiko:     SMA: Große Short-Position + schneller Preisverfall
   Kombination: Mit Rumors (Teil 2) — Gerüchte streuen + shorten
   Systeme:    OrderEngine (Short) + SMAEngine + RumorEngine

7. MARKET MANIPULATION VIA INFLUENCE (Endgame, Influence 80+)
   Opportunity: Spieler ist "Smart Money" — AI-Trader folgen ihm
   Aktion:     Leise Position aufbauen → sichtbarer großer Kauf → News: "Notable investor..."
   Profit:     +10-20% durch Nachahmungseffekt
   Risiko:     Schwer erkennbar — ist es Manipulation oder legitim?
   Systeme:    C2.2 Reputation (Influence 80+) + AITraderEngine (Front-Running)
```

#### Passive Bedrohungen (Spieler als Opfer — Events/NPCs)

```
8. SHORT & DISTORT (AI Hedge Fund attackiert deine Position)
   → AI publiziert Short Report → deine Long-Position fällt 20%
   → Entscheidung: Halten? Verkaufen? Dagegenhalten?
   → Arc: Report → Investigation → Betrug oder berechtigt?
   → Systeme: Tier-2/3 Events + AITraderEngine

9. ACCOUNTING FRAUD (Enron-Style Tier-4 Arc)
   → Firma fälscht Bücher → plötzlich: SEC Investigation → -40%, Halt
   → Cascade: Auditor-Fallout, CEO Rücktritt, Delisting-Risiko
   → Systeme: Tier-4 Arc "Corporate Mega-Fraud"

10. PONZI SCHEME
    → Aktie mit zu guten Returns (konstant steigend, niedrige Vola)
    → Warnsignale im Whisper Network (Tag -5)
    → Plötzlich: "Assets frozen, fraud investigation" → -80%
    → Systeme: Tier-3/4 Events + Whisper Network

11. PAINT THE TAPE (AI Window Dressing)
    → Pension Funds kaufen am Quartalsende → Preise künstlich hoch
    → Tag danach: Rückfall
    → Spieler der aufpasst: kauft NICHT am Quartalsende
    → Systeme: AITraderEngine (bereits implementiert)
```

#### SMA-Gameplay: "Wanted Level"

```
SCORE 0-9:    CLEAN — Grünes Shield. Kein Feedback.
SCORE 10-24:  NOTED — Shield wird gelb. "Your activity has been noted."
              Kleine Notiz im SMA-Panel. Kein Gameplay-Effekt.
SCORE 25-39:  REVIEW — "SMA reviewing unusual trading in {stock}"
              Ambient News. AI-Trader werden vorsichtig bei deinen Stocks.
SCORE 40-59:  FLAGGED — Warnung im UI. Größere Trades werden sichtbar verzögert.
              "Your trading patterns have drawn regulatory attention."
SCORE 60-79:  INVESTIGATION — Formale Untersuchung (30-60 Tage).
              Trades in betroffenen Stocks überwacht. Outcomes:
              → 40%: Cleared (Score -20, Relief Rally auf deine Stocks)
              → 35%: Fine (Geldstrafe = % vom illegalen Profit)
              → 25%: Restrictions (30 Tage Trading-Limits)
SCORE 80-94:  ENFORCEMENT — Harte Strafen.
              → Geldstrafe + 30-Tage Trading Ban auf betroffene Stocks
              → 180-Tage Margin-Verbot
              → News: "{Player} fined by SMA for market manipulation"
SCORE 95-100: ACCOUNT FREEZE — Nuclear Option.
              → Kann X Tage NICHT handeln (nur Markt beobachten)
              → Portfolio verliert Wert während man zuschaut
              → Game Over Risiko bei hohem Leverage

Score-Decay: -1 pro 5 saubere Tage (kein Decay während Investigation).
Score-Amplifier: Wiederholungstäter bekommen 1.5x Score-Zuwachs.
```

#### Skill-Expression: Drei Spielertypen

```
DER LEGALE SPIELER:
  → Nutzt Whisper Network nur zum Recherchieren
  → Handelt erst NACH öffentlichen News
  → Kein SMA-Risiko, langsamer aber sicherer Profit
  → Score bleibt bei 0

DER GRAUZONEN-SPIELER:
  → Nutzt Info-Vorsprung subtil (kleine Positionen, über Tage verteilt)
  → Kauft ETFs statt Einzelaktien (schwerer zu erkennen)
  → Hält Score bei 10-30 — genug Risiko für Profit, zu wenig für Ärger
  → "Edge" ohne echte Gefahr

DER KRIMINELLE:
  → All-In vor Events, Pump & Dump auf Small-Caps, Cornering
  → Hohe Profits, aber Score steigt schnell auf 60+
  → Muss zwischen Investigations navigieren
  → Irgendwann: Frage wird nicht OB sondern WANN er erwischt wird
  → "Wie viel Geld schaffe ich bevor sie mich kriegen?"
```

#### Wo im Plan verankert

| Mechanik | Primäres System | Session |
|----------|----------------|---------|
| SMA-Schwellenwerte senken + Feedback | SMAEngine Rebalance | **13** |
| Insider Trading als bewusste Wahl | Whisper Network | **32** |
| Pump & Dump via Influence | Reputation System | **29** |
| Spoofing-Erkennung verbessern | SMAEngine (schon da) | **13** |
| Cornering + Float-Kontrolle | Supply Chains | **30-31** |
| Bear Raid + Short Reports | Events + AI-Trader | **18-19** |
| Fraud/Ponzi Opfer-Events | Tier-4 Arcs | **17** |
| Market Manipulation via Influence | Reputation 80+ | **29** |
| "Wanted Level" UI | Frontend SMA Panel | **22** |

### 2.8 Umsetzungsreihenfolge Teil 2

```
Phase 2A — Player Interaction
  ① Event-Choice System (Tender, Vote, Crisis Response)
  ② Frontend: Choice-Dialoge in Events
  ③ Choice-Outcomes in EventEngine integrieren
  ④ ~100 interaktive Event-Templates generieren

Phase 2B — Reputation System
  ⑤ PlayerReputation Model (Influence, Scrutiny)
  ⑥ Influence-Effekte (Market Impact, Shareholder Rights)
  ⑦ Scrutiny-Effekte (SMA-Integration erweitern)
  ⑧ Frontend: Reputation Dashboard

Phase 2C — Supply Chains
  ⑨ SupplyChain Model (Suppliers, Customers, Competitors)
  ⑩ Zeitversetzte Event-Propagation
  ⑪ Supply Chain Visualisierung im Frontend
  ⑫ Content: Lieferketten für 500 Firmen generieren

Phase 2D — Information Timing
  ⑬ Multi-Stage Information Delivery
  ⑭ SEC Filing Scanner im Company Profile
  ⑮ Whisper-System (pre-event signals)
  ⑯ Erweiterte Rumor-Integration

Phase 2E — World Simulation
  ⑰ Election System (Kandidaten, Polls, Outcomes)
  ⑱ Fed Meeting Simulation (Decisions, Statements)
  ⑲ Saisonalität in EventEngine + PriceModel
  ⑳ Frontend: Political/Economic Calendar

Phase 2F — Company Evolution
  ㉑ CEO-Dynamik (Archetypes, Wechsel, Strategie-Shift)
  ㉒ Product Lifecycle Engine
  ㉓ Dynamic Fundamentals (Revenue, Earnings evolve)
  ㉔ Frontend: Enhanced Company Profile
```

---

## MENGEN-ÜBERSICHT

```
EVENTS GESAMT:
  Tier 1 Templates:           ~1.500
  Tier 2 Templates:           ~800
  Tier 3 Mini-Arcs:           ~50 (× 2 Pfade × 3 Phasen = ~300 Messages)
  Tier 4 Mega-Arcs:           15 (× 3 Pfade × 4 Phasen = ~540 Messages)
  Headline-Varianten:         3-5 pro Template
  Interaktive Events (Teil 2): ~100
  ─────────────────────────────────────
  Einzigartige Messages:      ~10.000-16.000
  Gefühlte Varianten:         ~30.000+ (durch Kombination)

CONTENT-POOLS:
  Analyst-Namen + Firmen:     200+
  Historical Parallels:       150-200
  Company-Reasons:            500+
  Supply Chain Links:         1.000+ Verbindungen

MODELLE:
  Price Model (ONNX):         ~500KB, <0.1ms/Stock

FIRMEN:
  Stocks:                     500+
  Jede mit: CEO, Products, Supply Chain, Story-State
```

---

## TECHNISCHE DATEIEN (NEU)

```
Backend (C#):
  Services/
    MarketDirector.cs         — Orchestrator
    PriceModel.cs             — ONNX Wrapper
    NarrativeEngine.cs        — Story-States, Arc-Management
    CompanyEvolutionEngine.cs — CEO-Dynamik, Product Lifecycle
    SupplyChainEngine.cs      — Lieferketten-Propagation (Teil 2)
    PoliticalEngine.cs        — Wahlen, Fed, Saisonalität (Teil 2)
    PlayerReputationEngine.cs — Influence, Scrutiny (Teil 2)
    WhisperEngine.cs          — Multi-Stage Info Delivery (Teil 2)
  Models/
    GameEvent.cs              — Erweitert (siehe oben)
    EventArc.cs               — Arc, Phase, Branch, Path
    EventTierConfig.cs        — Difficulty-Filter
    SectorImpact.cs           — Differenzierte Sektor-Auswirkungen
    SupplyChain.cs            — Supplier/Customer/Competitor (Teil 2)
    PlayerReputation.cs       — Influence + Scrutiny (Teil 2)
    EventChoice.cs            — Spieler-Entscheidungen (Teil 2)

Data (JSON):
  data/events/tier1-4/        — Alle Event-Templates
  data/analysts/              — Analysten-Pool
  data/supply_chains/         — Lieferketten (Teil 2)
  data/elections/             — Politische Szenarien (Teil 2)
  models/price_model.onnx     — Preismodell
```
