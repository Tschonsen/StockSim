# StockSim — Architecture Map

> Schnellreferenz: welche Datei macht was. Vor jeder Aufgabe hier nachschlagen.

## Backend Models (`backend/StockSim.Engine/Models/`)

| Datei | Zeilen | Zweck |
|-------|--------|-------|
| `Stock.cs` | ~114 | Aktie: Preis, Volume, Fundamentals, Traits, Insider/Institutional Ownership |
| `Portfolio.cs` | ~81 | Spieler: Cash, Positions, Orders, Margin-Status |
| `Position.cs` | ~74 | Einzelposition: Shares, AvgCost, P&L-Berechnung (Long + Short) |
| `Order.cs` | ~141 | Order mit Side/Type/Status Enums, OCO-Pair, Lifecycle |
| `Candle.cs` | ~141 | OHLCV-Candlestick + PriceHistory Rolling Window |
| `GameEvent.cs` | ~130 | Market-Event: Type, Severity, Sentiment, PriceEffect + Phase 1A: Summary, Analyst, Tier, Tags, SectorImpacts, ArcRef |
| `EventArc.cs` | ~120 | Multi-Phase Story Arc: ArcStatus, ArcPhase, ArcBranch, ArcPath |
| `EventTierConfig.cs` | ~110 | Difficulty-Presets: TierSettings pro Tier, ForDifficulty() Factory |
| `EventTemplate.cs` | ~130 | JSON-serialisierbares Event-Template + FollowUpTemplate, SectorImpactTemplate, AnalystProfile, FollowUpListConverter |
| `SectorImpact.cs` | ~85 | SectorImpact, AffectedCompany, FollowUpScenario |
| `PriceAlert.cs` | ~35 | Spieler-Preisalarm (max 20) |
| `Scenario.cs` | ~146 | 10 Szenarien mit Win/Lose-Bedingungen |
| `Achievement.cs` | ~99 | 31 Achievements + PlayerStats (Streaks, Best/Worst Trade) |
| `EconomicData.cs` | ~57 | Makro-Indikatoren (Zinsen, Inflation, BIP, etc.) |
| `GameSpeed.cs` | ~23 | Enum: Paused, Normal, Fast, VeryFast, Maximum |
| `MarketPhase.cs` | ~17 | Enum: Bull, Neutral, Bear |
| `SMAData.cs` | ~180 | SMAState, ViolationType, SMAViolation, SMAInvestigation, SMAPenalty |
| `Rumor.cs` | ~50 | Rumor: Symbol, Headline, IsTrue, EventExpectedAt |
| `CompanyPersonality.cs` | ~40 | CEO, Archetype, HQ, Products, FoundingStory, RivalSymbol |
| `OptionContract.cs` | ~100 | Options: Contract, Chain, ExpirationSlice, Position, Settlement |

## Backend Services (`backend/StockSim.Engine/Services/`)

### Kern-Loop
| Datei | Zeilen | Zweck |
|-------|--------|-------|
| `GameLoop.cs` | ~1320 | **Orchestrator**: Tick-Schleife, ruft alle Engines, koordiniert alles |
| `PriceEngine.cs` | ~370 | GBM + ONNX Hybrid: Brownian Motion + LSTM-Predictions + GARCH + Fat Tails |
| `PriceModel.cs` | ~220 | ONNX-Wrapper: lädt price_model.onnx, normalisiert Features, Inferenz |

### AI Event System (Phase 1 — COMPLETE)
| Datei | Zeilen | Zweck |
|-------|--------|-------|
| `TemplateLoader.cs` | ~165 | Lädt JSON-Templates + Analysten aus data/, Tier-Filter, Sektor-Matching |
| `NarrativeEngine.cs` | ~320 | Multi-Phase Story-Arcs: Aktivierung, Phasen-Advance, Branching, Event-Generation |
| `EventEngine.cs` | ~1360 | Hybrid: JSON-Templates first + hardcoded Fallback. ResolveTemplate(), Placeholder-System, Cascades, Rivalry |

### Trading & Portfolio
| Datei | Zeilen | Zweck |
|-------|--------|-------|
| `OrderEngine.cs` | ~672 | Order-Validierung, Execution, Kommission, Slippage, P&L |
| `AnalyticsCalculator.cs` | ~170 | Sharpe Ratio, Drawdown, Win Rate, Profit Factor |
| `TaxEngine.cs` | ~131 | Capital Gains Tax: 35% short-term, 15% long-term |

### Markt-Features
| Datei | Zeilen | Zweck |
|-------|--------|-------|
| `CircuitBreaker.cs` | ~114 | Trading-Halts bei -10% in 5min |
| `DividendEngine.cs` | ~198 | Quartals-Dividenden, Ex-Dates, Zahlungen |
| `EarningsEngine.cs` | ~188 | Quarterly Earnings, Beat/Miss, Kurssprung |
| `IPOEngine.cs` | ~206 | Neue Aktien alle 30-60 Tage, Delistings |
| `ETFEngine.cs` | ~183 | 13 ETFs (1 Market + 12 Sektor) |

### Options (Phase 3)
| Datei | Zeilen | Zweck |
|-------|--------|-------|
| `BlackScholes.cs` | ~160 | BS-Pricing, 5 Greeks, IV Solver (Newton-Raphson + Bisection) |
| `OptionsEngine.cs` | ~400 | Chain-Gen, Strike-Ladders, Repricing, Expiry-Settlement, IV Crush, Unusual Activity, Pin Risk |

### Wirtschaft & KI
| Datei | Zeilen | Zweck |
|-------|--------|-------|
| `EconomicEngine.cs` | ~450 | 10 Makro-Indikatoren, Fear & Greed, FOMC Meetings, Elections, Monetary Policy |
| `EconomicCycleEngine.cs` | ~147 | 4-Phasen-Zyklus (Expansion/Peak/Contraction/Recovery) |
| `SeasonalityEngine.cs` | ~140 | 8 Kalender-Effekte (Januar, Sell in May, Oktober, Triple Witching, etc.) |
| `AITraderEngine.cs` | ~530 | 14 AI-Trader-Typen + Daily (WindowDressing, ShortReports, Buybacks) |
| `AchievementEngine.cs` | ~350 | 53 Achievements, Stats-Tracking, neue System-Achievements |
| `SMAEngine.cs` | ~860 | StockSim Market Authority: 6 Detektionsalgorithmen, Investigations, Penalties |
| `RumorEngine.cs` | ~370 | Market Rumors + Supply Chain Whispers (Whisper Network) |
| `MemeStockEngine.cs` | ~150 | Meme Stock 5-Phasen-Lebenszyklus, Short Squeeze |

### Content & Personality
| Datei | Zeilen | Zweck |
|-------|--------|-------|
| `CompanyPersonalityGenerator.cs` | ~800 | CEO, Produkte, HQ, Story, Rivalries, Supply Chain für jede Aktie |

### Utilities & Daten
| Datei | Zeilen | Zweck |
|-------|--------|-------|
| `HistoryGenerator.cs` | ~172 | 252 Tage historische Candles per GBM |
| `IndicatorCalculator.cs` | ~206 | SMA, EMA, RSI, MACD, Bollinger Bands |
| `OrderbookGenerator.cs` | ~78 | 10-Level Bid/Ask Orderbook |
| `SaveManager.cs` | ~331 | Save/Load als JSON |
| `WebSocketServer.cs` | ~197 | WebSocket auf Port 8765 |

### Entry Point
| Datei | Zeilen | Zweck |
|-------|--------|-------|
| `Program.cs` | ~1620 | Bootstrap, WebSocket-Handler, alle Message-Typen |
| `Utils/Logger.cs` | ~99 | Structured Logging (DEBUG/INFO/WARN/ERROR) |

## ML Directory (`ml/`)

| Datei | Zweck |
|-------|-------|
| `train_price_model.py` | Training-Script: Yahoo Finance Download → Feature Engineering → LSTM → ONNX Export |
| `price_model.onnx` | Trainiertes ONNX-Modell (216 KB, 53.698 Parameter, Input: 103 Floats → Output: 2 Floats) |
| `scaler_params.json` | StandardScaler-Parameter für Feature-Normalisierung (mean + scale) |
| `data_cache.csv` | Gecachte Yahoo Finance Daten (151 Tickers, 5 Jahre, ~190K Rows) |

## Data Directory (`backend/StockSim.Engine/data/`)

| Pfad | Dateien | Inhalt |
|------|---------|--------|
| `events/tier1/` | 12 JSON | 337 Tier-1 Templates (earnings, analyst, management, products, corporate, dividends, insider, institutional, hedge_fund, market_structure, retail_sentiment, sector_rotation) |
| `events/tier2/` | 5 JSON | 127 Tier-2 Templates (regulatory, fraud, short_activist, breakthrough, crisis) |
| `events/tier3/` | 4 JSON | 21 Tier-3 Mini-Arcs (sector_crashes, commodity_shocks, financial_stress, regulatory) |
| `events/tier4/` | 4 JSON | 15 Tier-4 Black Swan Arcs (market_crashes, geopolitical, industry_shocks, systemic) |
| `analysts/` | 1 JSON | 226 fiktive Analysten (30 Firmen, 14 Spezialisierungen) |

## Frontend (`frontend/src/`)

### Screens
| Datei | Zeilen | Zweck |
|-------|--------|-------|
| `components/screens/TitleScreen.tsx` | ~179 | Hauptmenü: Start, Load |
| `components/screens/NewGameScreen.tsx` | ~637 | Spielerstellung: Difficulty, Cash, Szenarien |

### Layout
| Datei | Zeilen | Zweck |
|-------|--------|-------|
| `App.tsx` | ~600 | Root-Komponente, State Machine, 24+ WS-Handler |
| `components/layout/TopBar.tsx` | ~360 | Tabs, Speed, Zeit, Save, Autosave-Indicator, SMA-Shield, Settings |
| `components/layout/CentralArea.tsx` | ~2300 | **Größte Datei**: 7 Tabs + Bloomberg-Style News Detail + Arc Banner |
| `components/layout/LeftSidebar.tsx` | ~279 | Watchlist, Sektor-Übersicht |
| `components/layout/NewsTicker.tsx` | ~190 | Scrollende News/Preise + CRISIS/BLACK SWAN Badges |
| `components/layout/CommandBar.tsx` | ~300 | Ctrl+K Suchleiste |
| `components/layout/GlossaryModal.tsx` | ~193 | 42 Begriffe, searchable |
| `components/layout/SettingsModal.tsx` | ~354 | Einstellungen |
| `components/layout/TutorialOverlay.tsx` | ~145 | Tutorial-Schritte |
| `components/layout/ShortcutsHelp.tsx` | ~92 | Tastaturkürzel-Hilfe |

### Trading & Charts
| Datei | Zeilen | Zweck |
|-------|--------|-------|
| `components/trading/OrderPanel.tsx` | ~500 | Order-Formular (Buy/Sell/Short/Cover) + Beginner Lock |
| `components/trading/OptionsChain.tsx` | ~250 | Options Chain UI (Calls/Puts, Greeks, B/S Buttons) |
| `components/trading/StockScreener.tsx` | ~164 | Aktien-Filter/Sortierung |
| `components/trading/ConfirmOrderDialog.tsx` | ~130 | Bestätigungsdialog (Double-Submit-Schutz) |
| `components/charts/StockChart.tsx` | ~440 | ECharts-Integration (Candlestick + Volume) |
| `components/charts/Orderbook.tsx` | ~149 | Bid/Ask Orderbook |

### State & Services
| Datei | Zeilen | Zweck |
|-------|--------|-------|
| `stores/marketStore.ts` | ~380 | Zustand Store: gesamter UI-State + ActiveArcs |
| `services/websocket.ts` | ~180 | WebSocket-Client, Auto-Reconnect |
| `services/audio.ts` | ~142 | 10 Sound-Effekte (Web Audio API) |
| `services/logger.ts` | ~108 | Frontend-Logger |
| `types/market.ts` | ~350 | TypeScript-Interfaces + ActiveArc, NewsEvent (rich fields) |
| `hooks/useKeyboardShortcuts.ts` | ~161 | Alle Tastaturkürzel |

## Abhängigkeitsgraph

```
Program.cs (Bootstrap)
  ├─ GameLoop (Orchestrator)
  │  ├─ PriceEngine
  │  ├─ OrderEngine ← TaxEngine
  │  ├─ TemplateLoader → EventEngine (JSON templates)
  │  ├─ EventEngine (hybrid: JSON + hardcoded)
  │  ├─ NarrativeEngine → EventEngine (arc events via InjectEvent)
  │  ├─ DividendEngine
  │  ├─ CircuitBreaker
  │  ├─ EconomicCycleEngine
  │  ├─ EconomicEngine
  │  ├─ EarningsEngine
  │  ├─ IPOEngine
  │  ├─ ETFEngine
  │  ├─ AITraderEngine
  │  ├─ AchievementEngine
  │  ├─ RumorEngine → EventEngine (injects rumor events)
  │  └─ SMAEngine
  ├─ WebSocketServer → Frontend
  ├─ SaveManager
  └─ HistoryGenerator, IndicatorCalculator, OrderbookGenerator

data/ (JSON Content)
  ├─ events/tier1/ (337 templates, 12 files)
  ├─ events/tier2/ (127 templates, 5 files)
  ├─ events/tier3/ (21 arcs, 4 files)
  ├─ events/tier4/ (15 arcs, 4 files)
  └─ analysts/     (226 profiles)

App.tsx (Frontend Root)
  ├─ WebSocketClient → marketStore
  ├─ TopBar, CentralArea, LeftSidebar, NewsTicker
  ├─ Modals (Settings, Glossary, Tutorial, CommandBar)
  ├─ Charts, OrderPanel, OptionsChain, StockScreener
  ├─ ScenarioBar, DecisionCaseModal, HelpTip
  └─ data/glossary.ts (80 Einträge), data/decisionCases.ts (8 Cases)
```

Vollständiges Event-System Design: `design/AI_EVENT_SYSTEM.md`
