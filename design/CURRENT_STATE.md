# StockSim — Aktueller Zustand

> Kurz und knapp. Session-History siehe `design/SESSION_HISTORY.md`.

## Letztes Update: 2026-03-27, nach Session 12

## Status: Projekt-Audit abgeschlossen, Masterplan steht

### Projekt-Kennzahlen
- **~24.750 Zeilen Code** (11.865 Backend + 6.600 Frontend + 6.284 Tests)
- **90 Dateien** (40 Backend + 23 Frontend + 27 Tests)
- **372 Tests** grün
- **500+ Stocks**, **13 ETFs**, **12 Sektoren**, **80+ Subsektoren**
- **130+ Event-Templates**, 6 Cascade-Chains, 8 Geopolitik-Events

### Audit-Ergebnisse (2026-03-27):

**Realism:** 10 Fundamentalfelder ändern sich nie (Revenue, NetIncome, Employees, Debt, DividendYield, AnalystRating, TargetPrice, RevenueGrowth, YearHigh, YearLow). EarningsEngine erzeugt Reports aber schreibt keine Fundamentals. Fix: YearHigh/Low in Session 13, Rest in C2.6 (Session 34).

**SMA/Regulierung:** System funktioniert technisch (860 Zeilen, 6 Detektoren), aber Schwellenwerte so hoch dass normales Spielen nie etwas auslöst. Spieler bemerkt nichts. Fix: Schwellenwerte senken + mehr visuelles Feedback in Session 13.

**Events:** 130 Templates werden komplett durch neues JSON-Template-System ersetzt (nicht migriert). Siehe AI Event System.

### Nächste Session (13):
→ YearHigh/YearLow Tracking fixen
→ SMA Schwellenwerte ~50% senken, Feedback ab Score 10+ sichtbar machen
→ Autosave-Indicator
→ Dann: AI Event System Teil 1 (Session 14+)
