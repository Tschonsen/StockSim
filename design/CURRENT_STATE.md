# StockSim — Aktueller Zustand

> Kurz und knapp. Session-History siehe `design/SESSION_HISTORY.md`.

## Letztes Update: 2026-03-27, Session 12

## Status: Event Cascades + Geopolitik + Corporate Actions

### Session 12 (3 Teile):

**Teil 1 — Features:**
- CompanyPersonality System, 65 sektorspezifische Event-Templates, Company Profile Panel

**Teil 2 — 23 Playtest-Bugfixes:**
- Split-Loop, Volatilität, ETF, Speed, Event-Spam, Headlines, M&A, Rumors, Dividenden, P/E, FoundedYear, HQs, Rivalries, Archetypes, Penny-Shorts

**Teil 3 — Realismus + Content:**
- **Event Cascade System** — Follow-Up-Ketten: Earnings Miss→Analyst Downgrade→Insider Selling, CEO Exit→New CEO Hired, Data Breach→Lawsuit, Product Launch→Guidance Raise, Investigation→Settlement
- **8 Geopolitische Events** — Military Conflicts (mit Ceasefire Follow-Up), Sanctions, Debt Ceiling (mit Resolution), Earthquakes, Hurricanes, Gov Shutdown, Elections, Labor Strikes (mit Resolution)
- **Secondary Offerings** — Kapitalerhöhungen bei High-Debt/Growth Firmen, SharesOutstanding-Dilution, Oversubscribed Follow-Up
- **Simulation 10x schneller** — Max Speed 10ms statt 100ms, WebSocket-Throttling bei High Speed, Overnight-Skip

### Statistiken
- **~48k+ Zeilen**, **372 Tests** grün
- **500+ Stocks**, **13 ETFs**, **12 Sektoren**, **80+ Subsektoren**
- **130+ Event-Templates**, 6 Cascade-Chains, 8 Geopolitik-Events
- **5 Tage/Minute** bei Max Speed (1 Spieljahr ≈ 50 Min)

### Nächste Session (13):
→ Erneuter Playtest um alle neuen Features zu verifizieren
→ Frontend: News-Priorisierung (Breaking vs Minor im Ticker)
→ Frontend: Company Profile in Sidebar / Detailansicht verbessern
