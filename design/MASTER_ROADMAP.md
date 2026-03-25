# StockSim — Master Roadmap

> Vollständiger Plan: Alle Modi, Features, Content, Optimierung, Education.
> Alles was gebaut werden muss, priorisiert und geschätzt.

---

## A. SPIELMODI

### A1. Simulation Mode (STATUS: 95% fertig)

**Was fehlt:**
- [ ] Firmenprofile mit Tiefe (Geschichte, CEO, Produkte, Skandale) — siehe Sektion C
- [ ] Mehr News-Variation (200+ Templates statt 50+) — siehe Sektion C
- [ ] Performance-Optimierung — siehe Sektion D
- [ ] Playtesting + Balancing
- [ ] Company Logos (SVG generiert, Bible 11.3.6)
- [ ] Steam Achievements Integration
- [ ] Portfolio Screenshot Export (PNG mit Wasserzeichen)
- [ ] Autosave-Indicator im UI

### A2. History Mode (STATUS: 0% — Design fertig)

**Konzept:** Spiele echte Marktkrisen nach. Vorkonfigurierte Szenarien mit historisch korrekten Bedingungen.

**Szenarien:**
- [ ] **Black Monday 1987** — Markt fällt 22% an einem Tag. Startbedingung: Bull Market, plötzlicher Crash. Ziel: Überlebe mit >50% deines Portfolios.
- [ ] **Dot-Com Bubble 2000** — Tech-Aktien bei 100x P/E. Startbedingung: Extreme Tech-Bewertungen, steigende Zinsen. Ziel: Erkenne den Top und shorte rechtzeitig.
- [ ] **Financial Crisis 2008** — Banken kollabieren, Immobilien crashen. Startbedingung: Überbewertete Financials, hohe Verschuldung. Ziel: Überlebe den Crash, kaufe den Dip.
- [ ] **Flash Crash 2010** — Markt fällt 9% in 5 Minuten, erholt sich. Ziel: Reagiere schnell, kaufe den Flash-Dip.
- [ ] **GameStop Squeeze 2021** — Retail vs Hedge Funds. Hoher Short Interest, Social Media Hype. Ziel: Reite den Squeeze, steige rechtzeitig aus.
- [ ] **COVID Crash 2020** — Markt -34% in 23 Tagen, dann V-Recovery. Ziel: Überlebe den Crash, profitiere von der Erholung.
- [ ] **Volcker Shock 1980** — Zinsen bei 20%, Recession. Ziel: Navigiere Hochzins-Umfeld.

**Technisch:**
- Nutzt bestehende Engine zu 95%
- Braucht: vorkonfigurierte Stock-Sets, forcierte Event-Sequenzen, historische Zinsen/Inflation
- Braucht: Intro-Screen pro Szenario mit historischem Kontext (Text + Zeitstrahl)
- Braucht: Ergebnis-Vergleich ("Du: -12%. S&P 500 damals: -37%. Du hast den Markt geschlagen!")

**Aufwand: 2 Sessions**

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
├─ Alle 263+ Aktien
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
├─ 📖 Geschichte der Börse
│  ├─ Amsterdamer Börse (1602) — Die erste Aktie
│  ├─ Wall Street — Vom Buttonwood Tree zur NYSE
│  ├─ Ticker Tape & Telegraph — Wie Trading technisch wurde
│  ├─ Die großen Crashes (1929, 1987, 2000, 2008)
│  ├─ Elektronischer Handel — Von Parketthandel zu HFT
│  ├─ Retail Revolution — Robinhood, Meme Stocks, Demokratisierung
│  └─ Heute — Algo Trading, Dark Pools, Krypto
│
├─ 📊 Trading Grundlagen
│  ├─ Was ist eine Aktie?
│  ├─ Bid, Ask, Spread — Wie Preise entstehen
│  ├─ Order-Typen erklärt (Market, Limit, Stop, etc.)
│  ├─ Long vs Short — Auf steigende und fallende Kurse setzen
│  ├─ Margin Trading — Hebel und Risiken
│  ├─ Dividenden — Passives Einkommen
│  ├─ IPOs — Wie Firmen an die Börse gehen
│  └─ ETFs — Was sie sind und warum sie dominieren
│
├─ 📈 Technische Analyse
│  ├─ Candlestick-Patterns (Doji, Hammer, Engulfing...)
│  ├─ Moving Averages (SMA, EMA) — Trend erkennen
│  ├─ RSI — Überkauft/Überverkauft
│  ├─ MACD — Momentum messen
│  ├─ Bollinger Bands — Volatilität lesen
│  ├─ Support & Resistance — Psychologische Levels
│  ├─ Volume — Was Volumen verrät
│  └─ VWAP — Institutionelles Benchmark
│
├─ 📋 Fundamentalanalyse
│  ├─ P/E Ratio — Was ist "teuer"?
│  ├─ Revenue, Earnings, Margins — Firmenbewertung
│  ├─ Debt-to-Equity — Verschuldung verstehen
│  ├─ Market Cap — Größenklassen
│  ├─ Dividendenrendite — Einkommen bewerten
│  └─ Fair Value — Wie man berechnet ob eine Aktie unter/überbewertet ist
│
├─ 🏦 Marktstruktur
│  ├─ Wer handelt? (Market Maker, HFT, Hedge Funds, Retail)
│  ├─ Das Orderbook — Wie es funktioniert
│  ├─ Dark Pools — Versteckter Handel
│  ├─ Circuit Breaker — Warum Märkte gestoppt werden
│  ├─ Short Selling — Mechanik, Risiken, Short Squeezes
│  ├─ Regulierung (SEC/SMA) — Warum es Regeln gibt
│  └─ After-Hours Trading — Erweiterte Handelszeiten
│
├─ 🧠 Strategie & Psychologie
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
├─ 🏛️ Berühmte Personen & Events
│  ├─ Jesse Livermore — Der legendäre Spekulant
│  ├─ Warren Buffett — Das Orakel von Omaha
│  ├─ George Soros — Der Mann der die Bank of England brach
│  ├─ Michael Burry — Der Big Short
│  ├─ Keith Gill (DFV) — GameStop und die Retail-Revolution
│  ├─ Bernie Madoff — Der größte Ponzi Scheme
│  ├─ Enron — Wie ein Konzern durch Betrug fiel
│  └─ Long-Term Capital Management — Wenn Genies scheitern
│
└─ 🎮 StockSim Guide
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

## C. STOCK CONTENT — Tiefe & Geschichte

### C1. Firmenprofil-System

Jede Firma braucht:
- [ ] **Geschichte** (2-3 Sätze): "Founded in 2019 by former Google engineers..."
- [ ] **CEO Name** (generiert): "Sarah Chen, CEO since 2022"
- [ ] **Produkte/Services** (2-3): "Cloud infrastructure, AI analytics, Enterprise SaaS"
- [ ] **Competitors** (2-3 aus gleichem Sektor)
- [ ] **Recent News History** (letzte 5 Events die diese Firma betroffen haben)
- [ ] **Key Metrics Dashboard** (P/E, Revenue Growth, Margin Trend)

**Technisch:**
- Generiert bei Spielstart aus Seed (wie Stock-Namen)
- Templates pro Sektor (Tech-Firmen haben andere Geschichten als Utilities)
- Braucht: `CompanyProfile` Datenstruktur + Generator
- Braucht: Frontend-Tab "Company" im Stock Detail

**Aufwand: 2 Sessions**

### C2. News-Erweiterung (50 → 200+ Templates)

Fehlende Kategorien:
- [ ] **Management-News**: CEO-Wechsel (detailliert), Board-Konflikte, Activist Investor
- [ ] **Produkt-News**: Launches, Recalls, FDA-Approvals, Patent-Siege
- [ ] **Legal-News**: Klagen, Settlements, Regulatory Fines
- [ ] **ESG-News**: Umweltskandale, Nachhaltigkeits-Ratings, Carbon Credits
- [ ] **Geopolitik**: Sanktionen, Handelskriege, Embargos
- [ ] **Naturkatastrophen**: Hurrikane, Erdbeben → Supply Chain Impact
- [ ] **Technologie-Disruption**: "AI threatens to displace X industry"
- [ ] **Arbeitsmarkt**: Streiks, Massenentlassungen, Hiring Booms
- [ ] **Krypto/FinTech Spillover**: Crypto-Crash beeinflusst Tech-Sentiment
- [ ] **Saisonale Events**: Black Friday (Retail+), Tax Season, Earnings Season

**Aufwand: 2 Sessions**

### C3. Event-Ketten (Multi-Stage Events)

Aktuell: Events sind einzeln. Realistischer: Events haben Follow-Ups.

Beispiele:
```
FDA Trial Announcement
  → 60 Tage später: Trial Results (positive/negative)
    → Bei positiv: Stock +30%, Analyst Upgrades
    → Bei negativ: Stock -40%, Klagen, CEO-Rücktritt möglich

M&A Announcement
  → 30 Tage: Regulatory Review
    → Approved: Deal closes, Target delisted
    → Blocked: Target -20%, Acquirer +5%
    → Competing Bid: Target +10%, Bidding War

Earnings Miss
  → 1 Tag: Analyst Downgrades
  → 5 Tage: PEAD (drift down)
  → 30 Tage: Restructuring Announcement oder Recovery
```

**Aufwand: 2 Sessions**

---

## D. PERFORMANCE-OPTIMIERUNG

### D1. Backend

- [ ] **Tick-Batching**: Alle Stocks in einem Durchgang statt einzeln
- [ ] **Lazy Evaluation**: Nur Stocks berechnen die sich ändern könnten
- [ ] **Indicator Caching**: SMA/RSI/MACD nicht jedes Mal neu berechnen
- [ ] **Reduce Logging**: DEBUG-Logs nur bei Bedarf (aktuell loggt jeder PriceEngine-Tick)
- [ ] **Memory Pool**: Stock-Objekte recyclen statt neu allokieren
- [ ] **Profiling**: Benchmarks für 500 Stocks bei Maximum Speed

### D2. Frontend

- [ ] **React.memo** für alle Layout-Komponenten
- [ ] **Virtualized Lists**: Market-Tabelle mit 263+ Rows → nur sichtbare rendern
- [ ] **Chart Optimization**: TradingView Updates throttlen (max 5/sec statt 60)
- [ ] **Store Selectors**: Granularere Zustand-Subscriptions (weniger Re-Renders)
- [ ] **Web Worker**: Indikator-Berechnungen in Worker Thread
- [ ] **Bundle Size**: Tree-Shaking, Code-Splitting per Tab

### D3. WebSocket

- [ ] **Delta Updates**: Nur geänderte Felder senden statt komplette Snapshots
- [ ] **Message Batching**: Mehrere Updates pro Frame zusammenfassen
- [ ] **Compression**: WebSocket-Nachrichten komprimieren (bei >100 Stocks relevant)

**Aufwand: 2-3 Sessions**

---

## E. GESAMTÜBERSICHT — ALLE SESSIONS

| Session | Was | Aufwand | Abhängigkeiten |
|---------|-----|---------|----------------|
| 11 | Performance-Optimierung (Backend + Frontend) | 1 Session | — |
| 12 | Firmenprofile + News-Erweiterung (100 neue Templates) | 2 Sessions | — |
| 13 | Event-Ketten (Multi-Stage Events) | 1 Session | News-Templates |
| 14 | Education Wiki (Struktur + UI + erste 20 Artikel) | 1 Session | — |
| 15 | Education Wiki (weitere 30+ Artikel) | 1 Session | Wiki-UI |
| 16 | History Mode (7 historische Szenarien) | 2 Sessions | — |
| 17 | Incremental/Tycoon Mode (Progression + Upgrades) | 2 Sessions | — |
| 18 | Incremental/Tycoon Mode (UI + Prestige + Polish) | 2 Sessions | Tycoon-Basis |
| 19 | Arcade Mode (Timer, Modifiers, Highscore) | 2 Sessions | — |
| 20 | Company Logos + Audio Polish + Visual Polish | 1 Session | — |
| 21 | Playtesting + Balancing + Bugfixes | 2 Sessions | Alles |
| 22 | Steam Integration + Store Page + Trailer | 1 Session | Alles |

**Gesamt: ~18 Sessions bis Full Release mit 4 Modi + Wiki + Deep Content**

---

## F. PRIORISIERUNG

### Must-Have für Launch (Sessions 11-16):
1. Performance
2. Firmenprofile + mehr News
3. Event-Ketten
4. Education Wiki
5. History Mode
6. Visual/Audio Polish

### Should-Have (Sessions 17-20):
7. Incremental/Tycoon Mode
8. Arcade Mode
9. Playtesting

### Nice-to-Have (Post-Launch):
10. Options/Derivatives DLC
11. Multiplayer
12. Twitch/Discord Integration
13. Mobile Companion
