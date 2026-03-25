# StockSim — Aktueller Zustand

> Kurz und knapp. Session-History siehe `design/SESSION_HISTORY.md`.

## Letztes Update: 2026-03-25, Session 9 Ende

## Status: ~80% der Bible implementiert | SMA System DONE

### Was in Session 9 gemacht wurde:
- **SMA System komplett implementiert (Bible Sektion 9)**
  - `SMAData.cs`: Model mit ViolationType, SMAState, SMAViolation, SMAInvestigation, SMAPenalty, TradingRestriction
  - `SMAEngine.cs`: 6 Detektionsalgorithmen (InsiderTrading, PumpAndDump, Spoofing, WashTrading, Cornering, BearRaid)
  - Suspicion Score (0-100), Score-Decay (-1/5 Tage), Investigation Lifecycle (30-60 Tage), Penalty System
  - Account Freeze bei 3+ Enforcement Actions (Bible 9.4.5)
  - GameLoop-Integration: tägliche Analyse bei Market Close, Order-Tracking, Cancellation-Tracking
  - WebSocket: `SMANotifications`, `SMAStatus`, `GetSMAStatus` Handler, `smaStatus` in MarketUpdate
  - SaveManager: SMAState wird mit Spielständen gespeichert/geladen
  - Frontend: TypeScript-Types, Store (smaStatus, smaData, smaNotifications), WS-Handler
  - TopBar: Shield-Icon mit Farbstatus (grau→gelb→rot pulsierend), klickbares SMA-Panel
  - Toast-Notifications für SMA-Warnungen/Untersuchungen/Strafen
  - 24 neue Tests (321 total, alle grün)

### Statistiken
- **~30k+ Zeilen**, **321 Tests** grün
- **14 Backend Engines**, **263+ Stocks**, **31 Achievements**, **10 Szenarien**

### Nächster Schritt: Phase 2.2
→ Rumors-System (Bible 4.8) — blockiert Insider-Trading-Gameplay
→ SSR/Uptick Rule (Bible 4.4.2) — Realismus bei Short Selling
→ AI-Trader Diversität (Bible 7) — 14 Typen geplant, 4 implementiert

### Bekannte Bugs (nach Phase 1 Fixes)
- ETFs ohne historische Preisdaten
- Scenario-Regeln nicht vollständig enforced
- `click()`/`notification()` Audio unbenutzt
- Frontend-Tests (vitest) haben Tooling-Problem mit rolldown
