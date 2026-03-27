# StockSim — Master Roadmap

> Vollständiger Plan: Alle Modi, Features, Content, Optimierung, Education.
> Alles was gebaut werden muss, priorisiert und geschätzt.
> Stand: 2026-03-27. AI Event System integriert.

---

## A. SPIELMODI

### A1. Simulation Mode (STATUS: 95% fertig)

**Was fehlt:**
- [ ] Firmenprofile mit Tiefe — **ERSETZT durch C: AI Event System**
- [ ] Mehr News-Variation — **ERSETZT durch C: AI Event System**
- [ ] Performance-Optimierung — siehe Sektion E
- [ ] Playtesting + Balancing
- [ ] Company Logos (SVG generiert, Bible 11.3.6)
- [ ] Steam Achievements Integration
- [ ] Portfolio Screenshot Export (PNG mit Wasserzeichen)
- [ ] Autosave-Indicator im UI

### A2. History Mode (STATUS: 0% — verschmilzt mit AI Event System)

**Konzept:** Spiele echte Marktkrisen nach. Nutzt die Tier-4 Black Swan Arcs aus dem AI Event System als spielbare Szenarien.

**Szenarien (= Tier-4 Arcs, doppelte Verwertung):**
- [ ] **Black Monday 1987** — Markt fällt 22% an einem Tag
- [ ] **Dot-Com Bubble 2000** — Tech-Aktien bei 100x P/E
- [ ] **Financial Crisis 2008** — Banken kollabieren
- [ ] **Flash Crash 2010** — Markt fällt 9% in 5 Minuten
- [ ] **GameStop Squeeze 2021** — Retail vs Hedge Funds
- [ ] **COVID Crash 2020** — -34% in 23 Tagen, dann V-Recovery
- [ ] **Volcker Shock 1980** — Zinsen bei 20%, Recession
- [ ] **Oil Price War 2020** — Ölpreis wird negativ
- [ ] **AI Bubble** — Tech-Hype platzt oder wird real?

**Technisch:**
- Die 15 Tier-4 Arcs aus dem AI Event System sind gleichzeitig History-Mode-Szenarien
- Jeder Arc hat 2-4 Pfade mit je 3-5 Phasen → Replay-Value
- Braucht: Intro-Screen pro Szenario mit historischem Kontext
- Braucht: Ergebnis-Vergleich ("Du: -12%. S&P 500 damals: -37%")
- Wird als Teil von Phase C2 (Tier-4 Arcs) mitgebaut

**Aufwand: 1 Session** (Arcs kommen aus C2, nur Szenario-Shell + UI nötig)

### A3. Incremental/Tycoon Mode (STATUS: 0% — Design fertig)

**Konzept:** Starte mit $1.000 in einer Garage. Baue ein Finanzimperium durch Trading, Upgrades und Prestige-Resets.

**Progression:**

```
PHASE 1: Garage Trader ($1k - $50k)
├─ 5 Aktien verfügbar (nur Blue Chips)
├─ Nur Market Orders
├─ Kein Chart (nur Preisliste)
├─ Upgrade: "Gebrauchter Laptop" → Candlestick-Chart freischalten
├─ Upgrade: "Zeitungsabo" → News-Ticker freischalten
├─ Upgrade: "Online-Broker" → Limit Orders freischalten
└─ Meilenstein: $50k → Umzug ins Apartment

PHASE 2: Apartment Trader ($50k - $500k)
├─ 50 Aktien verfügbar
├─ Alle Basis-Ordertypen
├─ Upgrade: "Bloomberg-Terminal-Abo" → Indikatoren freischalten
├─ Upgrade: "Junior Analyst einstellen" → bekommt gelegentlich Tipps
├─ Upgrade: "Margin-Konto" → Margin Trading freischalten
├─ Upgrade: "Short-Selling-Berechtigung" → Shorts freischalten
└─ Meilenstein: $500k → Büro mieten

PHASE 3: Office Trader ($500k - $5M)
├─ 150 Aktien + ETFs
├─ Upgrade: "Quant-Analyst" → Algo-Signale
├─ Upgrade: "Nachrichtendienst" → Rumors kommen 1 Tag früher
├─ Upgrade: "Besserer Broker" → Niedrigere Kommissionen
├─ Upgrade: "Risk Management System" → Stop-Loss-Empfehlungen
└─ Meilenstein: $5M → Trading Floor

PHASE 4: Trading Floor ($5M - $100M)
├─ Alle 500+ Aktien
├─ Upgrade: "HFT-Infrastruktur" → Fills sind schneller/besser
├─ Upgrade: "Insider-Netzwerk" → Rumors 3x häufiger
├─ Upgrade: "PR-Abteilung" → Eigene Gerüchte streuen
├─ Upgrade: "Rechtsabteilung" → SMA-Strafen -50%
└─ Meilenstein: $100M → Hedge Fund gründen

PHASE 5: Hedge Fund ($100M - $1B)
├─ Neue Mechanik: Investoren-Geld verwalten (AUM)
├─ Performance Fee (2% + 20% der Gewinne)
├─ Investoren ziehen Geld ab wenn Performance schlecht
├─ Upgrade: "Lobbyisten" → Regulierung beeinflussen
├─ Upgrade: "Medien-Kontakte" → Markt bewegen
└─ Meilenstein: $1B → PRESTIGE verfügbar

PRESTIGE: "IPO — Geh an die Börse"
├─ Dein Fund wird als AI-Trader in den Markt injiziert
├─ Permanenter Bonus: +X% Startkapital, freigeschaltete Tools bleiben
├─ Neuer Run startet in der Garage, aber schneller
├─ Meta-Ziel: 10 Prestige-Runs → "Master of the Universe" Achievement
└─ Jeder Run dauert 2-4 Stunden (vs Simulation: endlos)
```

**Upgrade-Kategorien:**
- **Büro** (visuell + passiv): Garage → Apartment → Büro → Floor → Wolkenkratzer
- **Tools** (Trading-Features freischalten): Charts, Indikatoren, Order-Typen
- **Personal** (passive Boni): Analysten, Quants, Anwälte, PR
- **Infrastruktur** (Performance): Bessere Fills, schnellere News, niedrigere Kosten
- **Einfluss** (endgame): Gerüchte, Medien, Regulierung, Marktmanipulation

**Technisch:**
- Nutzt bestehende PriceEngine, EventEngine, AITraderEngine
- Braucht: Progression-System (Unlock-Logic, Upgrade-Trees)
- Braucht: Vereinfachtes Trading-UI (weniger Buttons, mehr visuelles Feedback)
- Braucht: Büro-Ansicht (einfache Illustration die sich mit Upgrades ändert)
- Braucht: Prestige-Logic + persistenter Meta-Fortschritt

**Aufwand: 4-5 Sessions**

### A4. Arcade Mode (STATUS: 0% — Design fertig)

**Konzept:** Schnelle 15-Minuten-Runden. Extremes Leverage. Meme-Stocks. Flash Crashes alle 2 Minuten. Highscore-Jagd.

**Spielablauf:**
1. Start: $10.000, 10x Leverage verfügbar
2. 20 Aktien (davon 5 "Meme Stocks" mit extremer Volatilität)
3. Events feuern alle 30 Sekunden (statt alle paar Minuten)
4. Flash Crashes, Short Squeezes, Insider-Tipps im Minutentakt
5. Nach 15 Minuten: Endabrechnung, Highscore
6. Daily Leaderboard (alle spielen das gleiche Seed)

**Modifiers (wählbar vor Rundenstart):**
- "YOLO": Nur All-In erlaubt (100% des Portfolios pro Trade)
- "Bear Market": Markt fällt konstant, nur Shorting profitabel
- "News Blackout": Keine News, nur Charts
- "Speed Demon": 30x Speed, 5-Minuten-Runde
- "Penny Stock Roulette": Nur Stocks unter $1

**Technisch:**
- Nutzt bestehende Engine mit Multipliern (Event-Frequenz 10x, Volatilität 3x)
- Braucht: Timer-UI, Highscore-System, Modifier-Auswahl
- Braucht: Meme-Stock-Generator (extreme Traits, wilde Namen)
- Braucht: Schnelleres Tick-System (1 Tick = 10 Sekunden statt 1 Minute)

**Aufwand: 2-3 Sessions**

---

## B. EDUCATION WIKI (In-Game Enzyklopädie)

**Konzept:** Eine vollständige, in-game Enzyklopädie über Börsenhandel — von den Anfängen bis heute. Bildung als Feature, nicht als Pflicht.

### B1. Struktur

```
STOCKSIM WIKI
├─ Geschichte der Börse
│  ├─ Amsterdamer Börse (1602) — Die erste Aktie
│  ├─ Wall Street — Vom Buttonwood Tree zur NYSE
│  ├─ Ticker Tape & Telegraph — Wie Trading technisch wurde
│  ├─ Die großen Crashes (1929, 1987, 2000, 2008)
│  ├─ Elektronischer Handel — Von Parketthandel zu HFT
│  ├─ Retail Revolution — Robinhood, Meme Stocks, Demokratisierung
│  └─ Heute — Algo Trading, Dark Pools, Krypto
│
├─ Trading Grundlagen
│  ├─ Was ist eine Aktie?
│  ├─ Bid, Ask, Spread — Wie Preise entstehen
│  ├─ Order-Typen erklärt (Market, Limit, Stop, etc.)
│  ├─ Long vs Short — Auf steigende und fallende Kurse setzen
│  ├─ Margin Trading — Hebel und Risiken
│  ├─ Dividenden — Passives Einkommen
│  ├─ IPOs — Wie Firmen an die Börse gehen
│  └─ ETFs — Was sie sind und warum sie dominieren
│
├─ Technische Analyse
│  ├─ Candlestick-Patterns (Doji, Hammer, Engulfing...)
│  ├─ Moving Averages (SMA, EMA) — Trend erkennen
│  ├─ RSI — Überkauft/Überverkauft
│  ├─ MACD — Momentum messen
│  ├─ Bollinger Bands — Volatilität lesen
│  ├─ Support & Resistance — Psychologische Levels
│  ├─ Volume — Was Volumen verrät
│  └─ VWAP — Institutionelles Benchmark
│
├─ Fundamentalanalyse
│  ├─ P/E Ratio — Was ist "teuer"?
│  ├─ Revenue, Earnings, Margins — Firmenbewertung
│  ├─ Debt-to-Equity — Verschuldung verstehen
│  ├─ Market Cap — Größenklassen
│  ├─ Dividendenrendite — Einkommen bewerten
│  └─ Fair Value — Wie man berechnet ob eine Aktie unter/überbewertet ist
│
├─ Marktstruktur
│  ├─ Wer handelt? (Market Maker, HFT, Hedge Funds, Retail)
│  ├─ Das Orderbook — Wie es funktioniert
│  ├─ Dark Pools — Versteckter Handel
│  ├─ Circuit Breaker — Warum Märkte gestoppt werden
│  ├─ Short Selling — Mechanik, Risiken, Short Squeezes
│  ├─ Regulierung (SEC/SMA) — Warum es Regeln gibt
│  └─ After-Hours Trading — Erweiterte Handelszeiten
│
├─ Strategie & Psychologie
│  ├─ Buy & Hold vs Active Trading
│  ├─ Value Investing (Warren Buffett)
│  ├─ Growth Investing (Cathie Wood)
│  ├─ Momentum Trading
│  ├─ Mean Reversion
│  ├─ Risk Management — Position Sizing, Stop Losses
│  ├─ Emotionen kontrollieren — Fear & Greed
│  ├─ Survivorship Bias — Warum Erfolgsgeschichten lügen
│  └─ Diversifikation — Warum nicht alles auf eine Karte
│
├─ Berühmte Personen & Events
│  ├─ Jesse Livermore — Der legendäre Spekulant
│  ├─ Warren Buffett — Das Orakel von Omaha
│  ├─ George Soros — Der Mann der die Bank of England brach
│  ├─ Michael Burry — Der Big Short
│  ├─ Keith Gill (DFV) — GameStop und die Retail-Revolution
│  ├─ Bernie Madoff — Der größte Ponzi Scheme
│  ├─ Enron — Wie ein Konzern durch Betrug fiel
│  └─ Long-Term Capital Management — Wenn Genies scheitern
│
└─ StockSim Guide
   ├─ Anfänger-Guide: Erste Schritte
   ├─ Intermediate: Order-Typen meistern
   ├─ Advanced: Short Selling & Margin
   ├─ Expert: Marktmanipulation erkennen (SMA)
   ├─ Szenarien-Guide: Tipps für jedes Szenario
   └─ Achievement-Guide: Alle 31 Achievements
```

### B2. Technisch

- In-Game als modales Fenster (wie Glossar, aber größer)
- Markdown-basiert (einfach zu schreiben und zu pflegen)
- Suchfunktion
- Verlinkt mit Gameplay: Klick auf "P/E Ratio" im Stock Detail → Wiki-Artikel öffnet sich
- Kontext-sensitiv: Wiki-Icon neben jedem komplexen UI-Element

### B3. Aufwand

- **Struktur + UI: 1 Session**
- **Content schreiben: 2-3 Sessions** (50+ Artikel à 300-500 Wörter)
- Alternativ: Content kann iterativ geschrieben werden

---

## C. AI EVENT SYSTEM — Tiefe, Variation, KI

> **Das Herzstück.** Ersetzt die alten Sektionen C1-C3 komplett.
> Vollständiges Design: `design/AI_EVENT_SYSTEM.md`

### C1. Teil 1 — Foundation (6-8 Sessions)

Massiv mehr Content, tiefere Events, smartere Preise. Der Spieler erlebt eine lebendige Welt.

#### C1.1 Event-Model + Tier-System (1-2 Sessions)

- [ ] **GameEvent erweitern:** Summary, AnalystQuote, AnalystName, HistoricalParallel, WhatToWatch, SectorImpacts, DetailedImpacts, FollowUpScenarios, Tags, Tier
- [ ] **EventTierConfig:** Difficulty-basierte Filterung (Easy→Brutal)
- [ ] **EventArc Model:** ArcPhase, ArcBranch, ArcPath mit dynamischen Wahrscheinlichkeiten
- [ ] **SectorImpact, AffectedCompany, FollowUpScenario** Models

```
Tier 1 (Alltag):     alle Schwierigkeiten, 3-6/Tag,  ±1-5%
Tier 2 (Markant):    ab Normal,           1-3/Woche, ±5-15%
Tier 3 (Krisen):     ab Hard,             1-3/Monat, ±10-30%
Tier 4 (Black Swan): ab Brutal + Szenarien, 0-2/Jahr, ±20-70%
```

#### C1.2 Content-Generierung (2-3 Sessions)

Offline via LLM generieren, als JSON-Daten ins Spiel einbauen:

- [ ] **Tier-1 Templates:** ~1.500 (Earnings, Analyst, Management, Products, Corporate, Insider, Dividends)
- [ ] **Tier-2 Templates:** ~800 (Regulatory, Fraud, M&A, Short/Activist, Crisis, Breakthrough, Legal)
- [ ] **Tier-3 Mini-Arcs:** ~50 (Sektor-Crashes, Commodity Shocks, Financial Stress) — je 2 Pfade, 2-3 Phasen
- [ ] **Tier-4 Mega-Arcs:** 15 Black Swans — je 2-4 Pfade, 3-5 Phasen (= History Mode Szenarien)
- [ ] **Analyst-Pool:** 200+ fiktive Analysten mit Firma + Titel
- [ ] **Headline-Varianten:** 3-5 pro Template = ~10.000-16.000 einzigartige Messages

Tier-4 Arcs:
1. Meme Stock Squeeze (3 Pfade)
2. Lehman Moment / Bank Collapse (3 Pfade)
3. Terror/Krieg Aftermath (3 Pfade)
4. Pandemie (3 Pfade: V/L/W-Recovery)
5. Tech Bubble Burst (2 Pfade)
6. Oil Shock (3 Pfade)
7. Currency Crisis (3 Pfade)
8. AI Revolution / Paradigm Shift (2 Pfade)
9. Flash Crash 2.0 (2 Pfade)
10. Sovereign Default (3 Pfade)
11. Corporate Mega-Fraud (2 Pfade)
12. Regulatory Earthquake (2 Pfade)
13. Natural Catastrophe (2 Pfade)
14. Trade War Escalation (3 Pfade)
15. Bank Run / Liquidity Crisis (3 Pfade)

#### C1.3 Engine-Integration (2 Sessions)

- [ ] **EventEngine refactorn:** JSON-Template-Loading, Tier-Filter, Difficulty-Anpassung
- [ ] **NarrativeEngine (NEU):** Story-States pro Firma, Arc-Management, Kohärenz-Check
- [ ] **MarketDirector (NEU):** Orchestrator — koordiniert Events, Preise, Narrative
- [ ] **Bidirektionaler Loop:** Events → Preise UND Preise → triggern Events (Stock -30% → Activist Event, Sektor +40% → Bubble Warning, ATH → Insider Selling)
- [ ] **Preis-Trigger-System:** CheckPriceTriggers() generiert reaktive Events

#### C1.4 ONNX Preismodell (1-2 Sessions)

- [ ] **Python:** Trainingsdaten von Yahoo Finance (500 Aktien, 5 Jahre)
- [ ] **Modell trainieren:** LSTM/Transformer, ~100K-1M Parameter
- [ ] **ONNX Export:** ~500KB Datei
- [ ] **C# Integration:** OnnxRuntime, PriceModel.cs als Wrapper
- [ ] **Hybrid-Modus:** ONNX-Modell + Fallback auf bestehende GBM-PriceEngine

Das Modell lernt sektor-spezifische Muster: Volatility-Clustering, Momentum, Earnings-Jumps, Event-Reaktionen. Input inkl. Event-Sentiment → Preise reagieren realistisch auf Events.

#### C1.5 Frontend: Bloomberg-Style News (1 Session)

- [ ] **News Detail View:** Klick auf Event → Summary, Analyst Quote, Sector Impact Map, Affected Companies, Historical Parallel, What to Watch
- [ ] **Tier-Badges** in Headline-Liste (Breaking, Sector, Company)
- [ ] **Sector Impact Visualisierung** (Mini-Heatmap)
- [ ] **Historical Scenarios** in Szenario-Auswahl (= History Mode UI)

### C2. Teil 2 — Spieler als Akteur (8-10 Sessions)

Der Spieler ist nicht mehr Zuschauer sondern Teil der Welt. Seine Entscheidungen beeinflussen den Verlauf.

#### C2.1 Interaktive Events (2 Sessions)

- [ ] **Event-Choice System:** Bestimmte Events bieten Spieler-Entscheidungen (~10-15% aller Events)
- [ ] **Tender Offers:** [Tender] [Hold Out] [Buy More] [Short Acquirer]
- [ ] **Shareholder Votes:** Ab >5% Anteil — M&A, CEO-Wechsel, Buybacks
- [ ] **Crisis Response:** [Double Down] [Cut Losses] [Hedge] [Wait]
- [ ] **~100 interaktive Event-Templates** generieren
- [ ] **Frontend:** Choice-Dialoge in Event-Detail-View

#### C2.2 Reputation & Markt-Einfluss (1-2 Sessions)

- [ ] **PlayerReputation Model:** Market Influence (0-100) + SEC Scrutiny (0-100)
- [ ] **Influence-Effekte:**
  - 20+: Bessere Margin-Konditionen
  - 40+: Eigene Trades bewegen den Preis (Market Impact)
  - 60+: Analysten erwähnen Spieler
  - 80+: Shareholder Rights, Board Letters
  - 90+: AI-Trader kopieren Spieler (Front-Running)
- [ ] **Scrutiny-Effekte:**
  - 30+: SMA schaut genauer hin
  - 50+: Trade-Verzögerungen
  - 70+: Investigations bei verdächtigen Mustern
  - 90+: Trading-Sperre + Geldstrafe
- [ ] **Frontend:** Reputation Dashboard

#### C2.3 Supply Chain Netzwerk (2 Sessions)

- [ ] **SupplyChain Model:** Jede Firma hat Suppliers, Customers, Competitors
- [ ] **Zeitversetzte Event-Propagation:** Earthquake → Raw Materials -8% (Tag 1) → Manufacturer -4% (Tag 3) → Consumer -2% (Tag 5) → Rival +5% (Tag 7)
- [ ] **Supply Chain Daten:** ~1.000+ Verbindungen für 500 Firmen generieren
- [ ] **Frontend:** Supply Chain Visualisierung im Company Profile

#### C2.4 Whisper Network — Information hat Zeitdimensionen (1-2 Sessions)

- [ ] **Multi-Stage Info Delivery:**
  - Tag -5: SEC Filing (nur im Company Profile sichtbar)
  - Tag -3: Rumor (70/30 wahr/falsch, erweitert bestehende RumorEngine)
  - Tag -1: Whisper (Analyst senkt PT leise, kleine Notiz)
  - Tag 0: Breaking News (volle Headline, Sound, Ticker)
  - Tag +1: Deep Analysis (Bloomberg-Style Detail-Artikel)
- [ ] **SEC Filing Scanner** im Company Profile
- [ ] **Skill-Expression:** Aufmerksame Spieler handeln bei Tag -5, Casual bei Tag 0

#### C2.5 Politische Simulation (1-2 Sessions)

- [ ] **Wahlen:** Alle 2 Spieljahre, 2 Kandidaten, Polls, Sektor-Shifts
- [ ] **Fed Meetings:** Alle 6 Spielwochen, Hike/Hold/Cut + Statement-Nuancen, Dot Plot
- [ ] **Saisonalität:** Januar-Effekt, Sell in May, Q4 Holiday, Earnings Seasons, Tax Loss Harvesting, Triple Witching
- [ ] **Frontend:** Political/Economic Calendar

#### C2.6 Dynamische Firmen-Evolution (1-2 Sessions)

- [ ] **CEO-Dynamik:** Archetypes (Visionary/Cost-Cutter/Empire Builder/Turnaround) beeinflussen Firmenstrategie, CEO kann gefeuert werden → neuer Typ → Kursreaktion
- [ ] **Product Lifecycle:** R&D → Launch → Growth → Mature → Decline, dynamisch generiert
- [ ] **Dynamic Fundamentals:** Revenue, Earnings, Margins, Debt, Employees ändern sich (löst Realism Audit Issues)
- [ ] **Frontend:** Enhanced Company Profile mit Pipeline + CEO-History

---

## D. POLISH & REMAINING FEATURES

### D1. Noch offene Features aus altem Roadmap

- [ ] Pattern Day Trader Rule
- [ ] Auto-Save (periodisch) + Indicator im UI
- [ ] Trade-Bestätigung bei großen Orders
- [ ] Stock Traits vollständig wirksam machen
- [ ] Company Logos (SVG generiert)
- [ ] Steam Achievements Integration
- [ ] Portfolio Screenshot Export

### D2. Audio & Visual

- [ ] Restliche Sounds + Squeeze-Alarm
- [ ] Background Music echte Audio-Files
- [ ] Tutorial vertiefen (interaktive Führung)

**Aufwand: 2-3 Sessions**

---

## E. PERFORMANCE-OPTIMIERUNG

### E1. Backend

- [ ] **Tick-Batching**: Alle Stocks in einem Durchgang statt einzeln
- [ ] **Lazy Evaluation**: Nur Stocks berechnen die sich ändern könnten
- [ ] **Indicator Caching**: SMA/RSI/MACD nicht jedes Mal neu berechnen
- [ ] **Reduce Logging**: DEBUG-Logs nur bei Bedarf
- [ ] **Memory Pool**: Stock-Objekte recyclen statt neu allokieren
- [ ] **Profiling**: Benchmarks für 500 Stocks bei Maximum Speed
- [ ] **ONNX Performance**: Batch-Inference für alle Stocks gleichzeitig

### E2. Frontend

- [ ] **React.memo** für alle Layout-Komponenten
- [ ] **Virtualized Lists**: Market-Tabelle mit 500+ Rows → nur sichtbare rendern
- [ ] **Chart Optimization**: ECharts Updates throttlen
- [ ] **Store Selectors**: Granularere Zustand-Subscriptions
- [ ] **Web Worker**: Indikator-Berechnungen in Worker Thread
- [ ] **Bundle Size**: Tree-Shaking, Code-Splitting per Tab

### E3. WebSocket

- [ ] **Delta Updates**: Nur geänderte Felder senden
- [ ] **Message Batching**: Mehrere Updates pro Frame zusammenfassen
- [ ] **Compression**: WebSocket-Nachrichten komprimieren

**Aufwand: 2-3 Sessions**

---

## F. ENTSCHIEDENE PUNKTE

### F1. Existierende 130 Events → Komplett ersetzt

Die 130+ hardcoded Templates in EventEngine.cs werden NICHT migriert sondern komplett durch das neue JSON-Template-System ersetzt. EventEngine.cs wird von Grund auf refactored:
- Alle Templates raus aus dem Code → JSON-Dateien
- Neue Tier-Logik, Difficulty-Filter, Arc-System
- Alte Cascades werden durch Tier-3 Arcs ersetzt

### F2. Realism Audit (Audit 2026-03-27)

**10 Felder die sich nach Spielstart NIE ändern, obwohl sie es müssen:**

| Feld | Wo gefixt |
|------|-----------|
| YearHigh / YearLow | Session 13 Quick-Fix (trivial, PriceEngine) |
| Revenue, NetIncome | C2.6a Dynamic Fundamentals (Session 34) |
| Employees, DebtToEquity | C2.6a Dynamic Fundamentals (Session 34) |
| DividendYield | C2.6a Dynamic Fundamentals (Session 34) |
| AnalystRating, TargetPrice | C2.6a Dynamic Fundamentals (Session 34) |
| RevenueGrowth | Abgeleitet von Revenue → automatisch |

**Zusätzliches Problem: EarningsEngine ändert KEINE Fundamentals.**
EarningsEngine generiert Beat/Miss Events + Preiseffekte, schreibt aber nie stock.Revenue oder stock.NetIncome. Das wird in C2.6a gefixt — EarningsEngine bekommt Fundamentals-Update-Logic.

### F3. SMA Regulierung — Existiert aber unsichtbar

**Audit-Ergebnis:** System funktioniert technisch, aber Schwellenwerte sind so hoch dass normales Spielen nie etwas auslöst. Spieler bemerkt SMA nicht.

**Probleme:**
- Score 0-39: Komplett unsichtbar, kein Feedback
- Insider Trading: >$1.000 Profit VOR Event nötig (fast unmöglich)
- Wash Trading: 3+ Buy-Sell-Paare am gleichen Tag in 5min
- Detection ist probabilistisch (15-70% Chance) → kann auch bei Match nicht feuern
- Erst ab Score 60 passiert etwas Spürbares (Investigation)

**Fix (Teil von Session 13 Quick-Fixes oder eigene Session):**
- Schwellenwerte ~50% senken
- Score 10+: Subtile UI-Hinweise (Shield-Farbe wechselt)
- Score 20+: Ambient News "Market authorities note unusual activity in {stock}"
- Score 30+: Deutliche Warnung statt erst bei 40
- Detection-Wahrscheinlichkeit auf 40-85% erhöhen
- Insider-Trading-Schwelle: >$500 Profit statt >$1.000
- Optional: "SMA Difficulty" Setting (Relaxed/Normal/Strict)

### F4. ONNX Training

Preis-Features (Returns, Vola, Sektor) trainiert auf echten Yahoo Finance Daten. Event-Features als Runtime-Modifier auf den ONNX-Output — nicht im Training, da historische Daten keine Game-Events haben.

### F5. Whisper Network

RumorEngine.cs bleibt als Basis für Tag -3 (Rumors). WhisperEngine.cs wird NEU gebaut für Tag -5 (SEC Filings) und Tag -1 (Whispers). Beide werden vom MarketDirector koordiniert. RumorEngine wird nicht refactored sondern eingebettet.

---

## G. SESSION-PLAN

Ehrliche Zahlen. Jede Zeile = 1 Session.

| # | Was | Abh. |
|---|-----|------|
| **AI EVENT SYSTEM TEIL 1 — Foundation** | | |
| 13 | Quick-Fixes: YearHigh/Low tracking, **SMA Rebalance** (Schwellenwerte senken, Feedback ab Score 10+), Close-Only Restriction Bug, Reject Reason in Toasts, tote Settings aufräumen | — |
| 14 | C1.1: GameEvent Model erweitern + Tier-System + Arc-Models | — |
| 15 | C1.2a: Content-Gen Tier 1 (~1.500 Templates) | C1.1 |
| 16 | C1.2b: Content-Gen Tier 2 (~800 Templates) + Analyst-Pool | C1.1 |
| 17 | C1.2c: Content-Gen Tier 3+4 (50 Mini-Arcs + 15 Mega-Arcs) | C1.1 |
| 18 | C1.3a: EventEngine Refactor — JSON-Loading, Tier-Filter, Migration bestehender 130 Templates | C1.1+C1.2 |
| 19 | C1.3b: NarrativeEngine + MarketDirector + Bidirektionaler Loop | C1.3a |
| 20 | C1.4a: Python — Trainingsdaten sammeln, Modell trainieren, ONNX Export | — |
| 21 | C1.4b: C# OnnxRuntime Integration + PriceEngine Hybrid-Modus | C1.4a |
| 22 | C1.5: Frontend Bloomberg-News Detail View + **"Wanted Level" SMA-Panel** + History Mode UI + **fehlende Fundamental-Felder anzeigen** (ShortInterest, Float, FairValue, Ownership, Slippage-Transparenz) | C1.3 |
| | | |
| **PERFORMANCE + WIKI** | | |
| 23 | Performance-Optimierung Backend (Tick-Batching, Caching, Profiling) | — |
| 24 | Performance-Optimierung Frontend (Virtualization, Memo, Workers) | — |
| 25 | Education Wiki Struktur + UI + erste 25 Artikel | — |
| 26 | Education Wiki weitere 25+ Artikel | 25 |
| | | |
| **AI EVENT SYSTEM TEIL 2 — Spieler als Akteur** | | |
| 27 | C2.1a: Event-Choice System Backend (Tender, Votes, Crisis Response) | C1.3 |
| 28 | C2.1b: ~100 interaktive Templates + Frontend Choice-Dialoge | C2.1a |
| 29 | C2.2: Reputation System (Influence + Scrutiny + SMA-Integration + **Pump&Dump/Manipulation via Influence**) | C2.1 |
| 30 | C2.3a: Supply Chain Model + Daten generieren (1.000+ Verbindungen) | C1.3 |
| 31 | C2.3b: Zeitversetzte Propagation + Frontend Visualisierung | C2.3a |
| 32 | C2.4: Whisper Network (Multi-Stage Info, Filing Scanner, RumorEngine-Erweiterung, **Insider Trading als bewusste Spieler-Wahl**) | C1.3 |
| 33 | C2.5: Politik (Wahlen, Fed Meetings, Saisonalität, Calendar-UI) | C1.3 |
| 34 | C2.6a: Dynamic Fundamentals (Revenue, Earnings, Margins, Debt evolve) | C1.3 |
| 35 | C2.6b: CEO-Dynamik + Product Lifecycle + Enhanced Company Profile | C2.6a |
| | | |
| **POLISH + MODI + LAUNCH** | | |
| 36 | Polish: PDT Rule, Trade Confirmation, Logos, Screenshots, Audio, **AI-Trader-Aktivität + Wirtschaftszyklus + MarketStress sichtbar machen** | — |
| 37 | Incremental/Tycoon Mode: Progression, Upgrades, Büro-Phasen | — |
| 38 | Incremental/Tycoon Mode: UI, Prestige, Meta-Fortschritt | 37 |
| 39 | Incremental/Tycoon Mode: Balancing + Polish | 38 |
| 40 | Arcade Mode: Timer, Modifiers, Highscore, Meme-Stocks | — |
| 41 | Arcade Mode: Leaderboard + Polish | 40 |
| 42 | Playtesting + Balancing Session 1 | Alles |
| 43 | Playtesting + Balancing Session 2 + Bugfixes | 42 |
| 44 | Steam Integration + Store Page + Trailer | Alles |

**Zusammenfassung:**
```
Sessions 13-22:  AI Event System Teil 1          10 Sessions
Sessions 23-26:  Performance + Wiki               4 Sessions
Sessions 27-35:  AI Event System Teil 2           9 Sessions
Sessions 36-44:  Polish + Modi + Launch           9 Sessions
                                            ──────────────
                                            ~32 Sessions
```

---

## H. PRIORISIERUNG

### Must-Have für Launch:
1. **AI Event System Teil 1** (Tiers, 2.300+ Templates, Arcs, MarketDirector, Bloomberg-News)
2. **ONNX Preismodell** (realistischere Kurse)
3. Performance-Optimierung
4. History Mode (kommt gratis mit Tier-4 Arcs)
5. Education Wiki
6. Polish (PDT, Logos, Audio)

### Should-Have:
7. **AI Event System Teil 2** (Interaktive Events, Reputation, Supply Chains, Whisper, Politik, Firmen-Evolution)

### Stretch / Post-Launch:
8. Incremental/Tycoon Mode
9. Arcade Mode
10. Detachable Panels (Multi-Window)
11. Backtesting-System
12. Rohstoffe/Crypto als Asset-Klassen
13. Options/Derivatives DLC
14. Multiplayer (siehe `design/MULTIPLAYER_VISION.md`)
15. Twitch/Discord Integration

### Nicht mehr aktiv geplant (aus altem Roadmap):
- ~~Detachable Panels~~ → Stretch, Infrastruktur steht (IPC ready)
- ~~Backtesting~~ → Stretch
- ~~Rohstoffe/Crypto~~ → Post-Launch DLC
