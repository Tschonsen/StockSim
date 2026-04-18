# StockSim — Roadmap (ARCHIV)

> **VERALTET** — Phasen 1-4 sind erledigt (Sessions 9-12). Restliche Items wurden in `MASTER_ROADMAP.md` migriert.
> Dieses Dokument wird nur noch als historische Referenz aufbewahrt.
>
> Aktiver Plan: `design/MASTER_ROADMAP.md`
>
> Priorisierte Aufgabenliste. Stand: 2026-03-25.

## Phase 1: Bugfixes (1 Session)

| # | Aufgabe | Aufwand | Dateien |
|---|---------|---------|---------|
| 1.1 | `previousClose` in MarketSnapshot senden | 5 min | Program.cs |
| 1.2 | `OrderResult` fehlende Felder ergänzen | 10 min | Program.cs |
| 1.3 | `dayHigh`/`dayLow` in MarketUpdate ergänzen | 5 min | Program.cs |
| 1.4 | `playerName` + `showTutorial` ans Backend senden | 15 min | NewGameScreen.tsx, Program.cs |
| 1.5 | `EconomicDataResponse` Type-Mismatch fixen | 5 min | types/market.ts |
| 1.6 | `NewGameDialog.tsx` löschen (obsolet) | 1 min | - |
| 1.7 | `orderPlaced()` Audio triggern in OrderPanel | 5 min | OrderPanel.tsx |
| 1.8 | Bankruptcy-Check erweitern (TotalEquity <= 0) | 15 min | GameLoop.cs |

## Phase 2: SMA Regulierungssystem (3-4 Sessions)

Spec Sektion 9, komplett neu. Größte fehlende Feature-Lücke.

| # | Aufgabe | Aufwand | Dateien |
|---|---------|---------|---------|
| 2.1 | `SMAEngine.cs` Grundgerüst: Suspicion Score (0-100), Score-Decay | 1 Session | Neues Model + Service |
| 2.2 | Detection-Algorithmen: Insider, Pump&Dump, Spoofing, Wash Trading, Cornering, Bear Raid | 1-2 Sessions | SMAEngine.cs, OrderEngine.cs |
| 2.3 | Strafen-System: Warnung, Untersuchung, Geldstrafe, Konto-Freeze | 1 Session | SMAEngine.cs, Program.cs |
| 2.4 | Frontend: Schild-Icon in TopBar, SMA-Panel, Banner/Warnungen | 1 Session | TopBar.tsx, CentralArea.tsx |

Abhängigkeiten: 2.1 → 2.2 → 2.3 → 2.4

## Phase 3: Rumors & Short-Mechaniken (2 Sessions)

| # | Aufgabe | Aufwand | Dateien |
|---|---------|---------|---------|
| 3.1 | `RumorEngine.cs` — Gerüchte vor Events, ~80% Trefferquote | 1 Session | Neues Service |
| 3.2 | Short Sale Restriction (SSR) — Uptick Rule bei >10% Fall | 0.5 Session | OrderEngine.cs, Stock.cs |
| 3.3 | Short Squeeze Detection + Warning Banner + Forced Cover | 0.5 Session | GameLoop.cs, CentralArea.tsx |

3.1 profitiert von SMA (Phase 2) für Insider-Trading-Erkennung.

## Phase 4: AI-Trader Diversität (2-3 Sessions)

| # | Aufgabe | Aufwand | Dateien |
|---|---------|---------|---------|
| 4.1 | AI-Trader von 4 aggregierten auf 14 diskrete Typen | 2 Sessions | AITraderEngine.cs |
| 4.2 | Stock Buyback Programme | 0.5 Session | GameLoop.cs |
| 4.3 | Activist Investor + Short Seller als eigene AI | 0.5 Session | AITraderEngine.cs |

## Phase 5: Polish & Content (2-3 Sessions)

| # | Aufgabe | Aufwand | Dateien |
|---|---------|---------|---------|
| 5.1 | Tutorial vertiefen (interaktive Führung) | 1 Session | TutorialOverlay.tsx |
| 5.2 | Restliche Sounds + Squeeze-Alarm | 0.5 Session | audio.ts, App.tsx |
| 5.3 | Pattern Day Trader Rule | 0.5 Session | OrderEngine.cs |
| 5.4 | Auto-Save (periodisch) | 0.5 Session | GameLoop.cs, Program.cs |
| 5.5 | Trade-Bestätigung bei großen Orders | 0.5 Session | OrderPanel.tsx |
| 5.6 | Stock Traits vollständig wirksam machen | 1 Session | PriceEngine.cs, Stock.cs |

## Phase 6: Stretch Goals (optional)

| # | Aufgabe | Aufwand |
|---|---------|---------|
| 6.1 | Rohstoffe/Crypto als neue Asset-Klassen | 2-3 Sessions |
| 6.2 | Backtesting-System | 2-3 Sessions |
| 6.3 | Multi-Window / Detachable Panels | 3-4 Sessions |
| 6.4 | Procedural Company Logos | 1-2 Sessions |

## Empfohlene Session-Planung

```
Session 9:  Phase 1 (alle Bugfixes)
Session 10: Phase 2.1 (SMA Grundgerüst)
Session 11: Phase 2.2 (Detection-Algorithmen)
Session 12: Phase 2.3 + 2.4 (Strafen + Frontend)
Session 13: Phase 3.1 (Rumors)
Session 14: Phase 3.2 + 3.3 (SSR + Short Squeeze)
Session 15: Phase 4 (AI-Trader)
Session 16+: Phase 5 (Polish)
```
