# StockSim — Aktueller Zustand

> Kurz und knapp. Session-History siehe `design/SESSION_HISTORY.md`.

## Letztes Update: 2026-03-25

## Status: ~72% der Bible implementiert

### Statistiken
- **~29k Zeilen** (21k Code + 8k Docs)
- **280 Tests** alle grün
- **13 Backend Engines**
- **263+ Stocks** (250 + 13 ETFs + dynamische IPOs)
- **31 Achievements**, **10 Szenarien**, **42 Glossar-Einträge**
- **7 Tabs** + Command Bar + Glossary + P&L Heatmap

### Bekannte Bugs
- `previousClose` fehlt in MarketSnapshot Response → Frontend-Berechnungen falsch
- `OrderResult` fehlen Felder (filledQuantity, placedAt, filledAt, rejectReason)
- Bankruptcy-Check zu eng (nur Cash≤0 && Positions==0, nicht TotalEquity≤0)
- `playerName` + `showTutorial` nicht ans Backend gesendet
- `dayHigh`/`dayLow` fehlen in MarketUpdate Tick-Messages
- `EconomicDataResponse` Type-Mismatch (sectorMultipliers)
- `orderPlaced()` Audio nie getriggert

### Größte fehlende Features
1. **SMA Regulierung** (Bible Sektion 9, 0%) — Suspicion Score, illegale Aktionen, Strafen
2. **Rumors/Insider-Tips** (Bible 4.8) — Gerüchte vor Events
3. **SSR/Uptick Rule** (Bible 4.4.2) — Short Sale Restriction
4. **Short Squeeze Warnings** (Bible 4.4.5) — Detection + UI
5. **AI-Trader Diversität** (Bible 7) — 14 Typen geplant, 4 implementiert
6. **Tutorial Tiefe** (Bible 14) — Interaktive Führung fehlt

### Sonstige offene Punkte
- ETFs ohne historische Preisdaten
- Scenario-Regeln nicht vollständig enforced
- `NewGameDialog.tsx` obsolet (löschen)
- `click()`/`notification()` Audio unbenutzt
- Pattern Day Trader Rule, Stock Buybacks, M&A/Tender Offers fehlen

### Nächste Session: Was tun?
→ Bugs fixen ODER nächstes großes Feature (SMA?) — User fragen.
