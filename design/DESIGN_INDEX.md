# StockSim — Design Spec Quick Reference

> Kurzreferenz zur Game Design Spec. Nur die Spec selbst lesen wenn Details nötig sind.

| Sektion | Thema | Status | Zeilen | Kerninhalt |
|---------|-------|--------|--------|------------|
| 1 | Vision & Zielgruppe | **DONE** | 1-80 | "Bloomberg meets STONKS-9800", 3 Spielertypen, Bankruptcy-Regeln |
| 2 | Visual Style Guide | **DONE** | 80-200 | Farben (#0A0E17, #10B981, #EF4444), Fonts (Inter/JetBrains Mono), Dark Theme |
| 3 | Main UI Layout | **95%** | 200-900 | TopBar, Sidebars, 7 Tabs, Stock Detail. ✅ DetachablePanelManager vorbereitet (Electron IPC). |
| 4 | Trading Mechanik | **95%** | 900-2400 | ✅ Orders, Slippage, Margin, Dividenden, Short Selling, Rumors, SSR, Short Squeeze. |
| 5 | Market Simulation | **95%** | 2400-2900 | ✅ GBM, Sektor-Korrelation (45% shared shocks), Volatilität, Float/Outstanding. |
| 6 | Portfolio Management | **95%** | 2900-3100 | ✅ Cash, Positionen, P&L, Margin, Detailed Analytics (12 Metriken). |
| 7 | AI-Trader | **95%** | 3100-3800 | ✅ 14 Trader-Typen. Daily: Window Dressing, Short Reports, Buybacks. HF Stress. |
| 8 | Events & Lifecycle | **95%** | 3800-4450 | ✅ 50+ Templates, Flash Crash, Circuit Breaker, IPO, Earnings, M&A/Tender Offers. |
| 9 | SMA Regulierung | **95%** | 4450-4800 | ✅ Suspicion Score, 6 Detektionen, Investigations, Penalties, UI, Rumors-Link. |
| 10 | Zeitsystem | **95%** | 4800-5000 | ✅ Market Hours, Speed, Skip to Open. Solide. |
| 11 | Stock-Generierung | **95%** | 5000-5500 | ✅ 12 Sektoren, 25/25 Traits zugewiesen, 22+ mit Gameplay-Effekten. |
| 12 | Charts & Indikatoren | **95%** | 5500-5800 | ✅ TradingView, SMA/EMA/RSI/MACD/BB/VWAP, Candle/Line/Area, Compare-Infrastruktur. |
| 13 | News-System | **90%** | 5800-6100 | ✅ Ticker, Filter, Expandable Detail Cards, Rumor Badge, Trade-Button. |
| 14 | Tutorial | **90%** | 6100-6300 | ✅ 8-Schritt Spotlight-Tutorial, Interactive Action Gating (stockSelected, orderPlaced, speedChanged). |
| 15 | Save/Load | **95%** | 6300-6400 | ✅ JSON, Multiple Slots, Autosave, Rumor/SMA State Round-Trip. |
| 16 | Settings | **95%** | 6400-6500 | ✅ 25/27 Settings. Gameplay + Simulation + Video + Audio + Controls + Accessibility. |
| 17 | Audio | **95%** | 6500-6600 | ✅ 13 SFX (alle verdrahtet), Background Music System (5 Moods, Crossfade), UI Sounds. |
| 18 | Hotkeys | **95%** | 6600-6700 | ✅ B/S/H/C Trading, Ctrl+K/N/L/S, F1/F11, Speed, Tabs, Escape, Enter. |
| 19 | Edge Cases | **95%** | 6700-6800 | ✅ Bankruptcy, Restart, Margin Cascade, SSR Enforcement, Priority Queue. |
| 20 | Phasen-Roadmap | n/a | 6800-6900 | Phase 1 MVP + Phase 2 dokumentiert |
| 21 | Tech-Architektur | **95%** | 6900-7100 | ✅ WebSocket-Protokoll, Electron, .NET, DetachablePanel IPC. |

## Gesamtstatus: ~95% Spec Coverage

Alle Sektionen sind bei mindestens 90%. Die verbleibenden 5% sind:
- Detachable Panels brauchen Electron-Runtime (IPC-Infrastruktur steht) → Stretch
- Background Music braucht echte Audio-Files (Synthese-Placeholder aktiv) → Session 36
- Einige Settings brauchen Backend-Wiring (Frontend-UI steht)

## AI Event System (Phase 1 — COMPLETE, Session 14-19)

| Komponente | Status | Details |
|------------|--------|---------|
| Models (EventTier, SectorImpact, EventArc, EventTierConfig) | **DONE** | Phase 1A |
| JSON Templates (500 total: 337 T1, 127 T2) | **DONE** | Phase 1B |
| 226 Analysten-Pool | **DONE** | Phase 1B |
| TemplateLoader Service | **DONE** | Phase 1B |
| EventEngine Integration (hybrid JSON+hardcoded) | **DONE** | Phase 1C |
| NarrativeEngine (multi-phase arcs) | **DONE** | Session 17 |
| 36 Story-Arcs (21 T3, 15 T4 Black Swans) | **DONE** | Session 17-18 |
| Frontend Bloomberg-Style News Detail | **DONE** | Phase 1E |
| ONNX Preismodell | **PLANNED** | Phase 1D |
| Named AI-Trader Entities (30-50) | **PLANNED** | Phase 2 |
| Supply Chain Network | **PLANNED** | Phase 2 |

Vollständiger Plan: `design/AI_EVENT_SYSTEM.md` + `design/MASTER_ROADMAP.md`
