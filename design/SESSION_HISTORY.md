# StockSim — Session History (Archiv)

> Alte Session-Logs, ausgelagert aus CURRENT_STATE.md.
> Nur lesen wenn historischer Kontext nötig ist.

---

## Session 5 (2026-03-24): Achievement, ETF, Scenario, Bankruptcy, Fundamentals

- Achievement-System (31 in 4 Kategorien), PlayerStats, TradeRecord
- ETF-System (13 ETFs: SIMX + 12 Sektor)
- Enhanced Analytics (Sharpe, Drawdown, Profit Factor, Equity Curve)
- Trading Journal Tab
- Scenario Mode (10 Szenarien mit Win/Fail Conditions)
- Bankruptcy/Game Over + Restart
- Stock Fundamentals (12 Kennzahlen)
- Cancel Order Button
- Command Bar (Ctrl+K)
- Economic System (10 Indikatoren, Fear & Greed)
- Earnings System (Quarterly, Beat/Miss)
- Tax System (Short/Long-term, Tax Loss Harvesting)
- Bracket Orders / OCO
- Risk Dashboard (VaR, Exposure)
- Advanced Screener (16 Presets)
- NewGame-Optionen ans Backend durchgereicht
- Skip to Market Open
- Margin Trading komplett (2:1, Margin Call, Forced Liquidation)
- Stock Splits (Forward + Reverse)
- News Filters + Clickable Symbols
- Analyst Ratings
- Dividend Income Tracker
- Variable Commission
- Retirement/Career Summary (ab $1M)
- Market Phase Display (BULL/BEAR/NEUTRAL)
- P&L Heatmap (Treemap)
- Insider Trading (AI, passiv)
- Ticker Tape (NEWS/TAPE Toggle)

## Session 7 (2026-03-24): Polish & UI Features

- Multiple Watchlists (bis 5)
- Partial Close Quick-Buttons (25/50/75/100%)
- Market Hours Display (PRE/LIVE/AFTER/WEEKEND)
- Sector Allocation Pie Chart
- Tax Summary Panel
- Sector Detail Banner
- Settings funktional (Audio, UI Scale, Contrast, Ticker Speed)
- Glossary Modal (42 Begriffe)
- Benchmark Comparison (Portfolio vs SIMX)
- Day Summary Enhanced

## Session 8 (2026-03-25): Audit & Bugfixes

- PriceUpdate.Symbol casing Bug gefixt (uppercase → lowercase)
- Vollständiger Bible-Abgleich: ~72% implementiert
- SMA-System als größte Lücke identifiziert (0% von Bible Sektion 9)
- 26+ Issues dokumentiert
- ARCHITECTURE.md, BIBLE_INDEX.md, SESSION_HISTORY.md erstellt
- CURRENT_STATE.md und CLAUDE.md überarbeitet

## Session 9 (2026-03-25): SMA System (Bible Sektion 9)

- **SMA komplett implementiert** — größte Lücke geschlossen (0% → 90%)
- Backend: `SMAData.cs` Model, `SMAEngine.cs` (530 Zeilen, 6 Detektionsalgorithmen)
  - Insider Trading, Pump & Dump, Spoofing, Wash Trading, Cornering, Bear Raid
  - Probabilistische Erkennung (15-70% je nach Typ)
  - Suspicion Score (0-100), Decay (-1/5 Tage), Investigation Lifecycle (30-60d)
  - Penalty System: Geldstrafen, Trading Bans, Margin-Entzug, Account Freeze
- GameLoop-Integration: tägliche Analyse, Order+Cancellation-Tracking
- WebSocket: SMANotifications, SMAStatus, GetSMAStatus, smaStatus in MarketUpdate
- SaveManager: SMAState wird persistiert
- Frontend: Types, Store, WS-Handler, Shield-Icon (TopBar), SMA-Panel, Toast-Notifications
- 24 neue Tests (321 total, alle grün)
- Nächste Schritte: Rumors (4.8), SSR (4.4.2), AI-Trader Diversität (7)

## Session 10-11 (2026-03-26): Massive Feature Expansion + Performance

- Rumors, SSR, Short Squeeze, AI-Trader 14 Typen, Stock Splits, M&A/Tender Offers
- DEBUG-Logging entfernt, 500 Stocks Standard, 80+ Subsektoren
- Market-Tabelle paginiert, Price Update Throttling, WebSocket Delta Updates
- 363 Tests grün

## Session 12 (2026-03-27): CompanyPersonality + Sektorspezifische Events

- **CompanyPersonality System**: CEO (Name + Archetype), HQ, Gründungsjahr, Flagship & Secondary Product, Founding Story, Rivalries
- **CompanyPersonalityGenerator.cs** (~280 Zeilen): 64 First/Last Names, 12 Archetypes, 28 HQ-Locations, 16 Produkte pro Sektor, Description Templates, Founding Story Templates
- **Rivalry System**: ~60% der Firmen bekommen Rival aus demselben Subsector
- **65 sektorspezifische Event-Templates**: 5-6 pro Sektor × 12 Sektoren (Bible 8.2.2)
- **Company Profile Panel** im Frontend Stock Detail
- **Frontend-Typen**: CompanyPersonality Interface, StockData + StockFundamentals erweitert
- Save/Load kompatibel (Personality aus Seed deterministic regeneriert)
- 9 neue Tests (372 total, alle grün)

### Session 12b (2026-03-27): Playtest + 23 Bugfixes

Blind-Playtest via WebSocket ergab 23 Issues (5 kritisch, 7 hoch, 8 mittel, 3 niedrig). Alle gefixt:

**Kritisch:** Reverse-Split-Loop (60d Cooldown), Event-Spam (Daily Cap 8, Stock-Cooldown 5d), Volatilität (±3% Tick-Clamp, Jump 8→3x), ETF-Explosion (MCap-Weighted Index), Sim-Speed (Skip overnight bei Fast+)

**Hoch:** M&A 1%→4%, Rumors 20-40d→5-12d, Dividenden gestreut über 20 Tage, P/E Cap ±999 + Penny→N/A, Sector-Events gebootet

**Mittel:** FoundedYear~MCap, Ära-Stories, Intl HQs (40+ Städte), Personality in Headlines (CEO/Produkte), Rivalry-Gameplay (30% inverse), CEO-Archetype-Effekte, Penny-Short min $50, IsDividendTrap Flag

372 Tests grün

## Session 13 (2026-03-27): YearHigh/YearLow Fix, SMA Rebalance, Autosave Indicator

**YearHigh/YearLow Tracking (FIXED):**
- `GameLoop.GenerateHistoricalPrices()`: YearHigh/YearLow aus 252 historischen Candles initialisiert
- `PriceEngine.Tick()`: Live-Update bei jedem Tick (inkl. Init von 0)
- `GameLoop.ApplySplit()` / `ApplyReverseSplit()`: YearHigh/YearLow korrekt angepasst

**SMA Schwellenwerte Rebalance (BEWUSSTE BIBLE-ABWEICHUNG):**
- Bible 9.2 spezifiziert Score-Ranges 0-20/21-40/41-60/61-80/81-100
- Neue Ranges: 0-10 (Clear) / 11-30 (Review) / 31-60 (Investigation) / 61+ (Enforcement)
- Begründung: Playtest zeigte dass alte Werte zu hoch waren, Spieler merkte nie etwas
- Detektionsschwellenwerte ~50% gesenkt (Insider $1000→$500, P&D Vol 10%→5%, etc.)
- Feedback ab Score 10+ (subtile Ambient-News), ab 30+ direkte Warnungen
- Investigation ab Score 40 (statt 60)
- 4 verschiedene Ambient-News-Texte für Variation

**Autosave-Indicator (NEW):**
- Backend: `Program.cs` sendet "Autosaved" WebSocket-Event nach jedem Auto-Save (500 Ticks)
- Frontend: `TopBar.tsx` zeigt grünes "Autosaved" Label für 3s neben Save-Button

**Tests:** 3 neue Tests (375 total, alle grün)
- `Tick_ShouldUpdateYearHighLow`: Verifiziert YearHigh/YearLow-Update nach 500 Ticks
- `Tick_YearHighLow_ShouldInitializeFromZero`: Verifiziert Init von 0
- `GameLoop_ShouldInitializeYearHighLowFromHistory`: Verifiziert Init aus historischen Candles

## Session 14 (2026-03-27): AI Event System Phase 1A — Model Layer

**Neue Model-Dateien:**
- `SectorImpact.cs`: SectorImpact (differenzierter Sektor-Impact), AffectedCompany (gezielter Firmen-Impact), FollowUpScenario (datengetriebene Follow-Ups mit Conditions)
- `EventArc.cs`: EventArc (Multi-Phasen-Story), ArcPhase, ArcBranch, ArcPath, ArcStatus Enum
- `EventTierConfig.cs`: TierSettings (pro-Tier Config), EventTierConfig mit ForDifficulty() Factory für Easy/Normal/Hard/Brutal

**GameEvent.cs erweitert:**
- EventTier Enum (Tier1-4) hinzugefügt
- ~20 nullable Properties: Content-Tiefe (Summary, Analyst, HistoricalParallel, WhatToWatch), Tier-System (Tags, Season, RequiresPhase/MarketCap), Differenzierte Impacts (SectorImpacts, DetailedImpacts), Follow-Ups (PossibleOutcomes), Arc-Referenzen (ArcId, ArcPhaseIndex, ArcPath)
- 100% abwärtskompatibel — bestehende Templates/Engine unverändert

**Tests:** 19 neue Tests (394 total, alle grün)
- Backward-Compatibility, neue Felder Round-Trip, EventTier Werte
- EventTierConfig für alle 4 Schwierigkeitsgrade, GetTier enabled/disabled
- SectorImpact/FollowUpScenario Defaults, EventArc Lifecycle, ArcPath Weights

### Session 14b (2026-03-27): AI Event System Phase 1B — Content + Infrastructure

**Infrastructure:**
- `data/` Directory-Struktur (events/tier1-4/, analysts/)
- `.csproj` Content-Copy für JSON-Dateien
- `EventTemplate.cs`: JSON-serialisierbares Template-Model mit FollowUpTemplate, SectorImpactTemplate, AnalystProfile
- `TemplateLoader.cs`: Service lädt JSON-Templates + Analysten, FindDataPath()-Logik, Tier-Filterung, Sektor-Analyst-Matching
- Custom `FollowUpListConverter`: Akzeptiert String-IDs und volle Objekte in followUps-Arrays

**Content generiert (370 Templates + 226 Analysten):**
- Tier-1 (243): earnings(51), analyst_actions(38), management(36), products(35), corporate(38), dividends(21), insider(24)
- Tier-2 (127): regulatory(30), fraud_scandal(25), short_activist(23), breakthrough(24), crisis(25)
- Analysten: 226 Profile mit 30 fiktiven Firmen, 14 Spezialisierungen

**Tests:** 10 neue Tests (404 total, alle grün)
- TemplateLoader: LoadAll, Tier1/Tier2 loading, Analyst loading, Field validation, Category filter, Random analyst + sector filter, Invalid path handling

## Session 15 (2026-03-27): AI Event System Phase 1C — EventEngine Integration

**EventEngine ↔ TemplateLoader Integration (Hybrid-Ansatz):**
- EventEngine Constructor akzeptiert optionalen `TemplateLoader?`
- `ResolveTemplate()`: Zentrale Methode JSON-Template → GameEvent
  - Placeholder-System: {company}, {ceo}, {product}, {sector}, {quarter}, {amount}, {shares}, {price}, {pct}, {count}, {days}, {department}
  - FormatAmount/FormatShares Helpers
  - Randomisiert PriceEffect + Duration aus [min, max] Ranges
  - 40% Chance auf Analyst-Quote mit Name+Firm aus Pool
  - Setzt neue Phase-1A-Felder: Summary, Tier, Tags, AnalystName/Firm/Quote
  - Follow-Ups aus Templates → PendingFollowUps gescheduled
- TryGenerateCompanyEvent: JSON zuerst → Fallback hardcoded
  - CEO-Archetype-Bias auch für JSON (Visionary re-roll für positive)
  - Rivalry-System für JSON-Events
  - 15% Chance auf Tier-2 Company Event
- TryGenerateMacroEvent: JSON zuerst → Fallback hardcoded
- TryGenerateSectorEvent: JSON zuerst → Fallback hardcoded
- GameLoop: TemplateLoader erstellt + LoadAll() + an EventEngine übergeben
- Custom FollowUpListConverter: Akzeptiert String-IDs + Objekte

**Tests:** 5 neue Tests (409 total, alle grün)
- EventEngine mit Templates generiert Events
- Rich Events haben Summary/Tags
- Placeholders korrekt aufgelöst (kein {company} in Headlines)
- Fallback ohne Templates funktioniert (Summary=null)
- Analyst-Quotes erscheinen probabilistisch

## Session 16 (2026-03-27): AI Event System Phase 1E — Frontend News Detail

**Backend:** Program.cs SendNewsEvents erweitert um summary, analystQuote/Name/Firm, tier, tags
**Frontend Types:** NewsEvent Interface um 6 optionale Felder erweitert
**News Detail Panel (Bloomberg-Style):**
- Expandierbare News-Karten im News Tab
- Summary-Text (2-3 Sätze Event-Kontext)
- Analyst-Quote Block (italic, blaue Sidebar, Name + Firm)
- Impact-Anzeige + Tier-Badge (Tier 2+ farbcodiert: orange/rot)
- Tags als Mono-Badges in var(--bg-tertiary)
- Trade-Button + Sektor-Info beibehalten

409 Tests grün, TypeScript fehlerfrei
