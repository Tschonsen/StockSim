# StockSim — Aktueller Zustand

> Kurz und knapp. Session-History siehe `design/SESSION_HISTORY.md`.

## Letztes Update: 2026-03-30, Session 28

## Status: v0.2.0 "Wall Street" — 584 Tests grün, Release-Ready

### Projekt-Kennzahlen
- **~40.000 Zeilen Code** (~18.000 Backend + 12.000 Frontend + 9.500 Tests + 2.800 Content + 400 ML)
- **~150 Dateien**
- **584 Tests** grün (496 Backend + 88 Frontend)
- **500 Event-Templates**, **80 Glossar-Einträge**, **16 Decision Cases**, **20 Szenarien**
- **57 Wiki-Artikel** in 8 Kategorien

### Session 28: v0.2.0 Release

**Playtest + Bugfixes:**
- 3 parallele Audits (Backend, Frontend, Electron)
- 2 Critical Bugs gefixt (Wash Sale auf Full Fills, resetGameState)
- 3 Medium Bugs gefixt (beginnerMode mutation, Ctrl+W Electron, commission override)
- 5 Electron-Export-Blocker gefixt (main process compile, preload, isDev, loadFile path, build script)
- Version auf 0.2.0 aktualisiert
- In-Game Changelog aktualisiert (TitleScreen Patch Notes)

### V1 Release-Roadmap — Stand:

| Phase | Status |
|-------|--------|
| Phase 1-4 | ✓ KOMPLETT |
| Grand Audit | ✓ KOMPLETT |
| Wiki + Szenarien + Tests | ✓ KOMPLETT |
| Interactive Events + Content | ✓ KOMPLETT |
| Packaging + Export | ✓ KOMPLETT |
| Playtest + Bugfix | ✓ KOMPLETT |
| **Version** | **v0.2.0 "Wall Street"** |
| **Steam Release** | **NICHT VOR AUGUST 2026** |

### Noch offen für Phase G (August):
- Steam Store Page, Screenshots, Trailer
- Steamworks SDK + Achievements API
- Steam Cloud Save
- App Icon (.ico)
- Final QA mit 3+ Testern
