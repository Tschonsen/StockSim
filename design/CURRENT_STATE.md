# StockSim — Aktueller Zustand

> Kurz und knapp. Session-History siehe `design/SESSION_HISTORY.md`.

## Letztes Update: 2026-03-25, Session 8 Ende

## Status: ~72% der Bible implementiert | Phase 1 Bugfixes DONE

### Was in Session 8 gemacht wurde:
- PriceUpdate.Symbol casing Bug gefixt (uppercase → lowercase)
- Vollständiger Bible-Abgleich: 26+ Issues dokumentiert
- ARCHITECTURE.md, BIBLE_INDEX.md, SESSION_HISTORY.md, ROADMAP.md erstellt
- CLAUDE.md + CURRENT_STATE.md überarbeitet (Context-effizient)
- Phase 1 Bugfixes (8 Fixes): previousClose, OrderResult-Felder, dayHigh/dayLow, playerName, Bankruptcy-Check, orderPlaced Audio, EconomicData Type
- CI-Workflow eingerichtet (GitHub Actions: .NET Tests + TS Type Check)
- GitHub Repo erstellt: github.com/Tschonsen/StockSim (private)

### Statistiken
- **~29k Zeilen** (21k Code + 8k Docs), **297 Tests** grün
- **13 Backend Engines**, **263+ Stocks**, **31 Achievements**, **10 Szenarien**

### Nächster Schritt: Phase 2.1 — SMA Grundgerüst
→ Siehe `design/ROADMAP.md` für Details
→ Bible Sektion 9 (Zeilen ~4450-4800) lesen für Spezifikation
→ Neues `SMAEngine.cs` + `SMAData.cs` Model erstellen
→ Suspicion Score (0-100), Score-Decay, Basis-Detection

### Bekannte Bugs (nach Phase 1 Fixes)
- ETFs ohne historische Preisdaten
- Scenario-Regeln nicht vollständig enforced
- `click()`/`notification()` Audio unbenutzt
- Frontend-Tests (vitest) haben Tooling-Problem mit rolldown
