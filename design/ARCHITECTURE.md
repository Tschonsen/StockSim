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
| `GameEvent.cs` | ~74 | Market-Event: Type, Severity, Sentiment, PriceEffect |
| `PriceAlert.cs` | ~35 | Spieler-Preisalarm (max 20) |
| `Scenario.cs` | ~146 | 10 Szenarien mit Win/Lose-Bedingungen |
| `Achievement.cs` | ~99 | 31 Achievements + PlayerStats (Streaks, Best/Worst Trade) |
| `EconomicData.cs` | ~57 | Makro-Indikatoren (Zinsen, Inflation, BIP, etc.) |
| `GameSpeed.cs` | ~23 | Enum: Paused, Normal, Fast, VeryFast, Maximum |
| `MarketPhase.cs` | ~17 | Enum: Bull, Neutral, Bear |

## Backend Services (`backend/StockSim.Engine/Services/`)

### Kern-Loop
| Datei | Zeilen | Zweck |
|-------|--------|-------|
| `GameLoop.cs` | ~874 | **Orchestrator**: Tick-Schleife, ruft alle Engines, koordiniert alles |
| `PriceEngine.cs` | ~205 | Brownian Motion Preisberechnung (Drift + Vola + MeanReversion) |

### Trading & Portfolio
| Datei | Zeilen | Zweck |
|-------|--------|-------|
| `OrderEngine.cs` | ~672 | Order-Validierung, Execution, Kommission, Slippage, P&L |
| `AnalyticsCalculator.cs` | ~170 | Sharpe Ratio, Drawdown, Win Rate, Profit Factor |
| `TaxEngine.cs` | ~131 | Capital Gains Tax: 35% short-term, 15% long-term |

### Markt-Features
| Datei | Zeilen | Zweck |
|-------|--------|-------|
| `EventEngine.cs` | ~359 | 50+ Event-Templates, Macro/Sector/Company Events |
| `CircuitBreaker.cs` | ~114 | Trading-Halts bei -10% in 5min |
| `DividendEngine.cs` | ~198 | Quartals-Dividenden, Ex-Dates, Zahlungen |
| `EarningsEngine.cs` | ~188 | Quarterly Earnings, Beat/Miss, Kurssprung |
| `IPOEngine.cs` | ~206 | Neue Aktien alle 30-60 Tage, Delistings |
| `ETFEngine.cs` | ~183 | 13 ETFs (1 Market + 12 Sektor) |

### Wirtschaft & KI
| Datei | Zeilen | Zweck |
|-------|--------|-------|
| `EconomicEngine.cs` | ~272 | 10 Makro-Indikatoren, Fear & Greed Index |
| `EconomicCycleEngine.cs` | ~147 | 4-Phasen-Zyklus (Expansion/Peak/Contraction/Recovery) |
| `AITraderEngine.cs` | ~245 | 4 aggregierte AI-Typen: MarketMaker, Retail, Institutional, Algo |
| `AchievementEngine.cs` | ~219 | 31 Achievements, Stats-Tracking |
| `SMAEngine.cs` | ~530 | StockSim Market Authority: 6 Detektionsalgorithmen, Investigations, Penalties |

### Regulierung (Bible 9)
| Datei | Zeilen | Zweck |
|-------|--------|-------|
| `SMAData.cs` (Models) | ~180 | SMAState, ViolationType, SMAViolation, SMAInvestigation, SMAPenalty, TradingRestriction |

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
| `Program.cs` | ~1303 | Bootstrap, WebSocket-Handler, alle Message-Typen |
| `Utils/Logger.cs` | ~99 | Structured Logging (DEBUG/INFO/WARN/ERROR) |

## Frontend (`frontend/src/`)

### Screens
| Datei | Zeilen | Zweck |
|-------|--------|-------|
| `components/screens/TitleScreen.tsx` | ~179 | Hauptmenü: Start, Load |
| `components/screens/NewGameScreen.tsx` | ~637 | Spielerstellung: Difficulty, Cash, Szenarien |

### Layout
| Datei | Zeilen | Zweck |
|-------|--------|-------|
| `App.tsx` | ~595 | Root-Komponente, State Machine, 24 WS-Handler |
| `components/layout/TopBar.tsx` | ~334 | Tabs, Speed, Zeit, Save, Settings |
| `components/layout/CentralArea.tsx` | ~2200 | **Größte Datei**: 7 Tabs (Dashboard, Portfolio, Market, Orders, News, Analytics, Journal) |
| `components/layout/LeftSidebar.tsx` | ~279 | Watchlist, Sektor-Übersicht |
| `components/layout/NewsTicker.tsx` | ~168 | Scrollende News/Preise |
| `components/layout/CommandBar.tsx` | ~300 | Ctrl+K Suchleiste |
| `components/layout/GlossaryModal.tsx` | ~193 | 42 Begriffe, searchable |
| `components/layout/SettingsModal.tsx` | ~354 | Einstellungen |
| `components/layout/TutorialOverlay.tsx` | ~145 | Tutorial-Schritte |
| `components/layout/ShortcutsHelp.tsx` | ~92 | Tastaturkürzel-Hilfe |

### Trading & Charts
| Datei | Zeilen | Zweck |
|-------|--------|-------|
| `components/trading/OrderPanel.tsx` | ~462 | Order-Formular (Buy/Sell/Short/Cover) |
| `components/trading/StockScreener.tsx` | ~164 | Aktien-Filter/Sortierung |
| `components/trading/ConfirmOrderDialog.tsx` | ~111 | Bestätigungsdialog |
| `components/charts/StockChart.tsx` | ~186 | TradingView Chart-Integration |
| `components/charts/Orderbook.tsx` | ~149 | Bid/Ask Orderbook |

### State & Services
| Datei | Zeilen | Zweck |
|-------|--------|-------|
| `stores/marketStore.ts` | ~341 | Zustand Store: gesamter UI-State |
| `services/websocket.ts` | ~180 | WebSocket-Client, Auto-Reconnect |
| `services/audio.ts` | ~142 | 10 Sound-Effekte (Web Audio API) |
| `services/logger.ts` | ~108 | Frontend-Logger |
| `types/market.ts` | ~326 | TypeScript-Interfaces (Backend-Matching) |
| `hooks/useKeyboardShortcuts.ts` | ~161 | Alle Tastaturkürzel |

## Abhängigkeitsgraph

```
Program.cs (Bootstrap)
  ├─ GameLoop (Orchestrator)
  │  ├─ PriceEngine
  │  ├─ OrderEngine ← TaxEngine
  │  ├─ EventEngine
  │  ├─ DividendEngine
  │  ├─ CircuitBreaker
  │  ├─ EconomicCycleEngine
  │  ├─ EconomicEngine
  │  ├─ EarningsEngine
  │  ├─ IPOEngine
  │  ├─ ETFEngine
  │  ├─ AITraderEngine
  │  ├─ AchievementEngine
  │  └─ SMAEngine
  ├─ WebSocketServer → Frontend
  ├─ SaveManager
  └─ HistoryGenerator, IndicatorCalculator, OrderbookGenerator

App.tsx (Frontend Root)
  ├─ WebSocketClient → marketStore
  ├─ TopBar, CentralArea, LeftSidebar, NewsTicker
  ├─ Modals (Settings, Glossary, Tutorial, CommandBar)
  └─ Charts, OrderPanel, StockScreener
```
