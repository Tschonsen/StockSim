# StockSim — Aktueller Zustand

> Kurz und knapp. Session-History siehe `design/SESSION_HISTORY.md`.

## Letztes Update: 2026-03-26, Session 11

## Status: Performance optimiert | 500 Stocks + Subsektoren

### Session 11:
- **DEBUG-Logging entfernt** — PriceEngine loggte 102k+ Einträge/Tag → 0
- **500 Stocks Standard** (statt 250) mit wählbarer Größe im NewGame
- **80+ Subsektoren** (z.B. Tech → Software, Semiconductors, Cloud, AI, Gaming...)
- **25+ Prefixes × 20 Suffixes pro Sektor** = 6.000+ Firmennamen
- **Market-Tabelle paginiert** — zeigt 50, "Show More" für Rest (statt 500 DOM-Nodes)
- **Price Update Throttling** — max 4 Updates/Sekunde ans Frontend (statt unbegrenzt)
- **WebSocket Delta Updates** — nur Stocks mit geändertem Preis werden gesendet

### Statistiken
- **~45k+ Zeilen**, **363 Tests** grün
- **500+ Stocks**, **13 ETFs**, **12 Sektoren**, **80+ Subsektoren**

### Nächste Session (12): Firmenprofile + Content-Tiefe
→ CompanyPersonality System (CEO, Produkte, Geschichte, Rivalitäten)
→ Sektorspezifische Event-Templates (erste 60 von 180)
