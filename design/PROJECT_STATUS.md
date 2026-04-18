# StockSim — Projekt-Dossier

> Vollständige Bestandsaufnahme. Was existiert, was funktioniert, was fehlt.
> Stand: 2026-03-27, nach Session 12.

---

## 1. PRODUKTÜBERSICHT

**StockSim** ist ein Echtzeit-Börsenhandelssimulator mit Bloomberg-Terminal-Ästhetik, 500+ fiktiven Aktien, 14 KI-Trader-Typen und einer vollständigen Marktregulierungsbehörde.

**Tech Stack:**
- Backend: C# .NET (Game Engine, WebSocket Server)
- Frontend: Electron + React 19 + TypeScript + Zustand
- Charts: Apache ECharts
- Kommunikation: lokaler WebSocket (Port 8765)
- Icons: Lucide React | Fonts: JetBrains Mono + Inter

**Codebasis:**
| Bereich | Dateien | Zeilen |
|---------|---------|--------|
| Backend Services | 23 | 8.829 |
| Backend Models | 15 | 1.338 |
| Backend Utils + Program.cs | 2 | 1.698 |
| **Backend Gesamt** | **40** | **11.865** |
| Frontend Komponenten | 17 | ~5.000 |
| Frontend Store + Types | 2 | 825 |
| Frontend Services + Hooks | 4 | 767 |
| **Frontend Gesamt** | **23** | **~6.600** |
| Tests (Backend) | 27 | 6.284 |
| **PROJEKT GESAMT** | **90** | **~24.750** |

---

## 2. FEATURE-INVENTAR — Was existiert und funktioniert

### 2.1 Markt-Simulation (Backend)

| Feature | Service | Zeilen | Status | Details |
|---------|---------|--------|--------|---------|
| **Preisberechnung** | PriceEngine.cs | 334 | Fertig | GBM + Fat Tails + Sektor-Korrelation (45%→85%) + GARCH-lite Vola-Clustering + Jump Diffusion |
| **Event-System** | EventEngine.cs | 1.081 | Fertig | 130+ Templates (Makro/Sektor/Company), Flash Crash, 6 Cascade-Chains, 8 Geopolitik-Events, Secondary Offerings, M&A/Tender |
| **Wirtschaftszyklus** | EconomicCycleEngine.cs | 147 | Fertig | 4-Phasen-Zyklus (Expansion/Peak/Contraction/Recovery) |
| **Makro-Daten** | EconomicEngine.cs | 274 | Fertig | 10 Indikatoren, Fear & Greed Index, Sektor-Multiplikatoren |
| **KI-Trader** | AITraderEngine.cs | 720 | Fertig | 14 Typen: Market Maker, HFT, Pension, Mutual, Index, HF L/S, HF Macro, SWF, DayTrader, Swing, Algo, Arb, Retail, Insider + Daily-Effekte (Window Dressing, Short Reports, Buybacks) |
| **Gerüchte** | RumorEngine.cs | 370 | Fertig | 8 Templates, 80/20 wahr/falsch, Event-Firing, SMA-Link |
| **Regulierung** | SMAEngine.cs | 860 | Fertig | 6 Detektions-Algorithmen (Insider, Pump&Dump, Spoofing, Wash, Cornering, Bear Raid), Investigations, Penalties, Trading-Restrictions |
| **Dividenden** | DividendEngine.cs | 210 | Fertig | Quartals-Dividenden, Ex-Dates, Zahlungen |
| **Earnings** | EarningsEngine.cs | 214 | Fertig | Quarterly Reports, Beat/Miss, Guidance, Kurssprung |
| **IPO/Delisting** | IPOEngine.cs | 225 | Fertig | Neue Aktien alle 30-60 Tage, Lock-up Period, Delistings |
| **ETFs** | ETFEngine.cs | 190 | Fertig | 13 ETFs (1 Market + 12 Sektor), NAV-Tracking |
| **Circuit Breaker** | CircuitBreaker.cs | 114 | Fertig | Trading-Halts bei -7%/-13%/-20% |
| **Firmen-Persönlichkeit** | CompanyPersonalityGenerator.cs | 405 | Fertig | CEO, Archetype, HQ, Products, Founding Story, Rivalries |

### 2.2 Trading-Mechanik (Backend)

| Feature | Service | Zeilen | Status | Details |
|---------|---------|--------|--------|---------|
| **Order-System** | OrderEngine.cs | 790 | Fertig | Market, Limit, Stop, StopLimit, Trailing Stop, Bracket Orders, OCO |
| **Steuern** | TaxEngine.cs | 131 | Fertig | 35% Short-term, 15% Long-term Capital Gains |
| **Portfolio-Analytik** | AnalyticsCalculator.cs | 170 | Fertig | Sharpe Ratio, Max Drawdown, Win Rate, Profit Factor, Sortino |
| **Indikatoren** | IndicatorCalculator.cs | 242 | Fertig | SMA, EMA, RSI, MACD, Bollinger Bands, VWAP |
| **Orderbook** | OrderbookGenerator.cs | 105 | Fertig | 10-Level Bid/Ask Simulation |
| **History** | HistoryGenerator.cs | 172 | Fertig | 252 Tage synthetische Candles per GBM |
| **Save/Load** | SaveManager.cs | 362 | Fertig | JSON, Multiple Slots, kompletter Spielstand |
| **Achievements** | AchievementEngine.cs | 219 | Fertig | 31 Achievements, Stats-Tracking (Streaks, Best/Worst) |

### 2.3 Frontend

| Feature | Komponente | Status | Details |
|---------|-----------|--------|---------|
| **7-Tab Layout** | CentralArea.tsx | Fertig | Dashboard, Portfolio, Market, Orders, News, Analytics, Journal |
| **Candlestick Charts** | StockChart.tsx | Fertig | ECharts, 7 Timeframes, 6 Indikatoren, Vergleichs-Modus |
| **Trading Panel** | OrderPanel.tsx | Fertig | Buy/Sell/Short/Cover, 5 Order-Typen, Margin-Support |
| **Watchlist** | LeftSidebar.tsx | Fertig | Multi-Watchlist (max 5), Preis-Flash-Animation |
| **News-Ticker** | NewsTicker.tsx | Fertig | Headlines + Preis-Tape, Auto-Scroll |
| **Command Bar** | CommandBar.tsx | Fertig | Ctrl+K Suchleiste, intelligentes Kommando-Parsing |
| **Stock Screener** | StockScreener.tsx | Fertig | 16 Presets (Gainers, Value, Dividend, Volatile...) |
| **Orderbook** | Orderbook.tsx | Fertig | 10-Level Bid/Ask mit Tiefe-Balken |
| **Tutorial** | TutorialOverlay.tsx | Fertig | 8 Schritte, Spotlight, Action Gating |
| **Glossar** | GlossaryModal.tsx | Fertig | 65+ Begriffe, searchable, kategorisiert |
| **Settings** | SettingsModal.tsx | Fertig | 6 Kategorien, 25+ Einstellungen |
| **Audio** | audio.ts | Fertig | 14 SFX, 5 Musik-Moods (Synthese), Crossfade |
| **Keyboard Shortcuts** | useKeyboardShortcuts.ts | Fertig | 25+ Shortcuts (Trading, Navigation, Speed, System) |
| **Szenarien** | NewGameScreen.tsx | Fertig | 10 Szenarien, 3 Presets, Custom-Config |
| **Title Screen** | TitleScreen.tsx | Fertig | Main Menu, Patch Notes, Continue/Load |

### 2.4 Markt-Content

| Kategorie | Menge | Details |
|-----------|-------|---------|
| Aktien | 500+ | Über 12 Sektoren, 80+ Subsektoren |
| ETFs | 13 | 1 Market (SPY) + 12 Sektor-ETFs |
| Event-Templates | 130+ | Makro, Sektor, Company, Flash Crash, Geopolitik |
| Cascade-Chains | 6 | Follow-Up-Events mit Delay |
| AI-Trader-Typen | 14 | Von Market Maker bis Retail Trader |
| Szenarien | 10 | The Crash, Bull Run, Short Squeeze, Iron Man... |
| Achievements | 31 | Wealth, Trading, Market, Risk Kategorien |
| Glossar-Begriffe | 65+ | Finanz-Lexikon |
| Stock Traits | 22+ | Blue Chip, Penny Stock, ESG Leader, Biotech Gamble... |

### 2.5 Tests

| Bereich | Tests | Abdeckung |
|---------|-------|-----------|
| OrderEngine | 41 | Alle Order-Typen, Slippage, Margin, Edge Cases |
| SMAEngine | 24 | Alle 6 Detektionsalgorithmen |
| AITraderEngine | 19 | Trader-Verhalten, Sentiment |
| MarginOCOSplit | 24 | Margin Calls, Stock Splits, OCO |
| EconomicEarningsTax | 23 | Earnings, Taxes, Economic Data |
| Weitere 22 Dateien | 241 | Models, Indicators, ETF, IPO, CircuitBreaker, etc. |
| **Gesamt** | **372** | **Alle grün** |

**Lücken (keine dedizierten Tests):**
- EconomicEngine (Makro-Indikatoren)
- WebSocketServer
- OrderbookGenerator

### 2.6 WebSocket-Protokoll

37 Message-Typen implementiert:
```
Spiel:     NewGame, SetSpeed, SkipToOpen, SaveGame, LoadGame, ListSaves, Retire, RestartBankrupt, shutdown
Trading:   PlaceOrder, PlaceBracketOrder, CancelOrder, AcceptTenderOffer
Daten:     GetPortfolio, GetOHLCV, GetIndicators, GetOrderbook, GetOrders
Analyse:   GetAnalytics, GetAchievements, GetTradeJournal, GetStockFundamentals
Markt:     GetEarningsCalendar, GetEconomicData, GetTaxSummary, GetSMAStatus, GetScenarios
Settings:  UpdateSettings, SetAlert, DeleteAlert, GetAlerts
System:    hello (Handshake)
```

---

## 3. WAS FEHLT — Offene Punkte

### 3.1 Bugs (Session 13)

| # | Issue | Schwere | Details |
|---|-------|---------|---------|
| 1 | **Close-Only Restriction wird nicht geprüft** | Hoch | SMA verhängt Close-Only, aber OrderPanel validiert es nicht — Spieler kann trotzdem Positionen öffnen |
| 2 | **YearHigh/YearLow aktualisiert sich nie** | Hoch | Wird im Backend nie geschrieben nach Init |
| 3 | **Stock Screener "Apply" tut nichts** | Mittel | onApplyFilter Callback wird nie aufgerufen |
| 4 | **Order Reject Reason nicht angezeigt** | Mittel | rejectReason ist im Type definiert, wird nie im Toast/UI gerendert |

### 3.2 Unsichtbare Features (Backend arbeitet, Frontend zeigt nichts)

| Feature | Backend-Effekt | Frontend zeigt | Fix geplant |
|---------|---------------|----------------|-------------|
| **AI-Trader (14 Typen)** | <1% Preiseffekt/Tick | Nichts — keine Aktivitätsanzeige | Session 36 |
| **Wirtschaftszyklus (Bull/Bear)** | 5-15% Sektor-Drift | Nichts — keine Phasenanzeige | Session 36 |
| **Sektor-Korrelation (45%→85%)** | Synchronere Bewegungen in Krisen | Nichts — kein MarketStress-Level | Session 36 |
| **Slippage** | 0.01%-10% je nach Ordergröße | Nichts — kein Expected vs Actual | Session 22 |

### 3.3 Daten vorhanden aber nicht angezeigt

Felder in StockFundamentals die definiert + vom Backend gesendet, aber im UI nie gerendert werden:
- YearHigh, YearLow, AnalystConsensus
- InsiderOwnership, InstitutionalOwnership, ShortInterest
- BaseVolatility, LiquidityScore, FairValue
- Float, FloatPercentage, Subsector

Fix: Session 22 (Frontend Bloomberg-News + Stock Detail Ausbau)

### 3.4 Tote Settings (~20 Stück, 0% implementiert)

**Video:** WindowMode, Resolution, VSync, FpsLimit, ShowFps
**Accessibility:** ColorblindMode (3 Varianten, kein CSS-Filter), Language (kein i18n)
**Audio:** MarketBellSound, TradeSound, NewsAlertSound (Toggles existieren, werden nicht abgefragt)
**Autosave:** Autosave, AutosaveInterval, ShowAutosaveNotification (kein Interval implementiert)
**Simulation (nicht ans Backend gesendet):** MarginInterest, ShortBorrowFees, SmaStrictness, TaxRateMode

Entscheidung Session 13: Entweder implementieren oder ehrlich entfernen/ausgrauen.

### 3.5 Noch offene Features

| Feature | Aufwand | Geplant |
|---------|---------|---------|
| Pattern Day Trader Rule | 30 min | Session 36 |
| Trade-Bestätigung bei großen Orders | 30 min | Session 36 |
| Background Music echte Audio-Files | 1+ Session | Session 36 |
| Company Logos (SVG) | 1-2 Sessions | Session 36 |
| Steam Achievements Integration | 1 Session | Session 44 |
| Portfolio Screenshot Export | 30 min | Session 36 |

### 3.6 Geplante Systeme (Sessions 14+)

Siehe `design/MASTER_ROADMAP.md` Sektion C (AI Event System) + A-H.

---

## 4. DESIGN-DOKUMENTE — Status

| Dokument | Zeilen | Status | Aktion |
|----------|--------|--------|--------|
| `CURRENT_STATE.md` | ~30 | **AKTIV** | Wird nach jeder Session aktualisiert |
| `ARCHITECTURE.md` | ~155 | **AKTIV** | Datei-Index, bei Änderungen updaten |
| `MASTER_ROADMAP.md` | ~380 | **AKTIV** | Haupt-Roadmap, Sessions 13-33 |
| `AI_EVENT_SYSTEM.md` | ~280 | **AKTIV** | Detail-Spec für AI Event System |
| `DESIGN_SPEC.md` | ~7100 | **AKTIV** | Single Source of Truth für Game Design |
| `DESIGN_INDEX.md` | ~35 | **AKTIV** | Quick Reference zur Spec |
| `SESSION_HISTORY.md` | ~200 | **AKTIV (Archiv)** | Wird nach Sessions ergänzt |
| `AUDIT_LOG.md` | ~150 | **AKTIV** | Wird bei Audits ergänzt |
| `CONCEPT.md` | ~50 | **AKTIV** | Ursprüngliche Vision, immer noch gültig |
| `MULTIPLAYER_VISION.md` | ~100 | **AKTIV (Zukunft)** | Post-Launch Phase 2 |
| `ROADMAP.md` | ~80 | **VERALTET** | Phasen 1-4 erledigt, Rest in MASTER |
| `DESIGN_CHANGES.md` | ~500 | **VERALTET** | Inhalte in MASTER_ROADMAP migriert |

---

## 5. ABHÄNGIGKEITEN

### Runtime (Produktion)
**Backend:** .NET (C#), keine externen NuGet-Packages
**Frontend:** React 19, Zustand 5, ECharts 6, Lucide Icons, echarts-for-react

### Development
Electron 41, Vite 5, TypeScript 5.9, Vitest, Testing Library, electron-builder
