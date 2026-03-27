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
