# StockSim — Aktueller Zustand

> Kurz und knapp. Session-History siehe `design/SESSION_HISTORY.md`.

## Letztes Update: 2026-03-27, Session 12

## Status: Event Cascades + Geopolitik + Realism Audit done

### Session 12 abgeschlossen:
- CompanyPersonality System + 65 Sektor-Events + Company Profile Panel
- 23 Playtest-Bugfixes (Split-Loop, Vola, ETF, Speed, Event-Spam, etc.)
- Event Cascades (6 Follow-Up-Ketten), 8 Geopolitik-Events, Secondary Offerings
- 10x Speed-Boost, Personality in Headlines, Rivalry-Gameplay

### Statistiken
- **~48k+ Zeilen**, **372 Tests** grün
- **500+ Stocks**, **13 ETFs**, **12 Sektoren**, **80+ Subsektoren**
- **130+ Event-Templates**, 6 Cascade-Chains, 8 Geopolitik-Events

### Realism Audit: 18 Issues identifiziert
Firmen sind "eingefroren" — Revenue, Earnings, Analyst Ratings, Target Prices, YearHigh/Low, Dividenden, Debt, Employees ändern sich nie. Details in Memory `project_realism_audit.md`.

### AI & Event System Masterplan erstellt (2026-03-27):
Neues Design-Dokument: `design/AI_EVENT_SYSTEM.md`
- **Teil 1:** Foundation — Tier-System, 2.300+ Templates, Event-Arcs mit Branching, ONNX Preismodell, MarketDirector, NarrativeEngine, Bloomberg-Style News
- **Teil 2:** Spieler als Akteur — Interaktive Events, Reputation, Supply Chains, Whisper Network, Politik, Firmen-Evolution

### Nächste Session (13):
→ Playwright Setup für Frontend Visual Testing
→ Realism Batch 1: Fundamentals evolve, YearHigh/Low, Analyst dynamic, Employees
→ ODER: AI Event System Teil 1 starten (Phase 1A: Models + Tier System)
