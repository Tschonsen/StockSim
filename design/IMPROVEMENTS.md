# StockSim — Verbesserungsplan

> Erstellt: 2026-03-30, Session 28. Alles was verbessert, anders gemacht, oder neu gebaut werden sollte.

## Priorität 1: VOR dem ersten Playtest (MUST)

### 1.1 CentralArea.tsx aufteilen (~2h)
**Problem:** 2300+ Zeilen, 7 Tabs in einer Datei, 19 Store-Subscriptions. Jeder Preis-Tick re-rendert ALLES.
**Lösung:** Aufteilen in separate Tab-Komponenten. CentralArea wird dünner Router.
**Dateien neu:** DashboardTab.tsx, PortfolioTab.tsx, MarketTab.tsx, OptionsTab.tsx, OrdersTab.tsx, NewsTab.tsx, AnalyticsTab.tsx, JournalTab.tsx
**Impact:** Größtes Performance-Upgrade. Jeder Tab subscribed nur auf seine Daten.

### 1.2 Save-Format Versionierung (~30min)
**Problem:** Saves sind raw JSON ohne Version. Jede Änderung am Datenmodell kann alte Saves brechen.
**Lösung:** `saveVersion: number` im JSON. Migration-System: `if (version < 2) migrateV1ToV2(data);`
**Datei:** SaveManager.cs

### 1.3 Program.cs aufteilen (~2h)
**Problem:** 1600 Zeilen God Object. WebSocket-Handler, Serialisierung, Game-Lifecycle alles zusammen.
**Lösung:** MessageRouter.cs, TradingHandler.cs, GameStateHandler.cs extrahieren.
**Impact:** Alle zukünftige Backend-Arbeit wird schneller und sicherer.

## Priorität 2: VOR Steam Release (SHOULD)

### 2.1 WebSocket Delta-Updates (~1h)
**Problem:** Jeder MarketUpdate sendet ALLE Stocks. Bei 50+ Stocks × 10 Felder = viel redundanter Traffic.
**Lösung:** Nur geänderte Felder senden. Kompaktes Array-Format statt JSON-Objekte.
**Impact:** ~70% weniger Traffic, bessere Performance bei Maximum Speed.

### 2.2 Chart-Bibliothek: ECharts → TradingView Lightweight Charts (~1 Tag)
**Problem:** ECharts ist ~800KB, Canvas-basiert (keine CSS Vars), Config wird bei jedem Update komplett neu gebaut.
**Lösung:** TradingView Lightweight Charts v5 — speziell für Finanz-Charts, kleiner, Echtzeit-optimiert.
**Watermark:** `attributionLogo: false` ist offiziell erlaubt (Apache 2.0). Bedingung: Link zu tradingview.com irgendwo in der App (z.B. Settings > About).
**Risiko:** ~1 Tag Arbeit, alle Chart-Features müssen re-implementiert werden. Indicator Overlays (SMA, Bollinger) müssen als Custom Series geplottet werden.
**Entscheidung:** Nur machen wenn ECharts-Performance ein echtes Problem wird im Playtest.

### 2.3 State-Sync Mechanismus (~1h)
**Problem:** Frontend/Backend können auseinanderlaufen wenn WebSocket-Messages verloren gehen.
**Lösung:** Alle 60s Hash-Vergleich. Bei Mismatch Full-Snapshot anfordern.

### 2.4 Game Loop Error Recovery (~30min)
**Problem:** Bei ExecuteTick Crash wird Spiel permanent pausiert. Kein Retry.
**Lösung:** Transiente Fehler → Tick überspringen, nächsten Tick normal. Erst bei 3+ Fehlern in Folge pausieren.

### 2.5 i18n vorbereiten (~1h Setup + ongoing)
**Problem:** Language-Setting in UI tut nichts. Alle Strings hardcoded.
**Lösung:** react-intl oder i18next aufsetzen. Strings als Keys. Entweder Language-Dropdown entfernen oder implementieren.
**Für V1:** Dropdown entfernen oder "English only" Label.

## Priorität 3: FÜR V2 (COULD)

### 3.1 CSS Modules statt Inline Styles
**Problem:** Hunderte Zeilen `style={{ }}` pro Komponente. Kein echtes :hover, :focus, Media Queries.
**Lösung:** Schrittweise Migration zu CSS Modules (.module.css). Oder Tailwind.
**Aufwand:** Groß (~1 Woche). Nur für V2.

### 3.2 Bessere Test-Philosophie
**Problem:** Tests prüfen interne State-Details statt beobachtbares Verhalten.
**Lösung:** Black-Box-Tests: Inputs rein, Outputs prüfen. Macht Tests resilient gegen Refactoring.
**Wann:** Schrittweise bei neuen Features.

### 3.3 Orders-Liste begrenzen
**Problem:** Portfolio.Orders wächst unbegrenzt. Bei langem Spiel Performance-Degradation.
**Lösung:** Abgeschlossene/Rejected Orders nach 100 Einträgen archivieren oder entfernen.

### 3.4 WebSocket Buffer Size
**Problem:** Fixed 8192-byte Empfangsbuffer. Große Messages werden abgeschnitten.
**Lösung:** EndOfMessage-Loop mit dynamischem Buffer.
