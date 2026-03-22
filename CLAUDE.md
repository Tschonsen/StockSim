# StockSim — Claude Code Projektregeln

## OBERSTE DIREKTIVE: Zustandssicherung

**VOR jeder Arbeit:**
1. `design/CURRENT_STATE.md` lesen — verstehen wo wir stehen
2. `design/CURRENT_STATE.md` aktualisieren mit: was jetzt getan wird, welche Dateien betroffen sind

**NACH jeder Arbeit (oder bei natürlichem Ende eines Gesprächs):**
1. `design/CURRENT_STATE.md` aktualisieren mit: was wurde getan, was ist der aktuelle Stand, was sind die nächsten Schritte
2. Bei größeren Änderungen: relevante Memory-Dateien aktualisieren

**WARUM:** Bei Token-Limit, Internet-Ausfall oder Stromausfall muss das nächste Gespräch SOFORT wissen was los ist. Kein Kontext darf verloren gehen.

## REGEL 2: Regelmäßige Audits nach Umsetzung

**Nach jeder abgeschlossenen Feature-Implementierung:**
1. Code-Audit: Stimmt der Code mit der Game Design Bible überein?
2. Test-Audit: Sind alle Tests geschrieben und grün?
3. Logging-Audit: Werden alle wichtigen Aktionen geloggt?
4. Bible-Abgleich: Weicht die Implementierung von der Bible ab? Wenn ja: Bible ODER Code anpassen (nicht beides inkonsistent lassen)

**Nach jedem Meilenstein (z.B. "Order-System fertig", "AI-Trader laufen"):**
1. Vollständiger Audit: alle bisherigen Features gegen die Bible prüfen
2. Performance-Check: passt die Implementierung ins Performance-Budget?
3. Integrations-Test: funktionieren alle Systeme zusammen?
4. Ergebnis dokumentieren in `design/AUDIT_LOG.md`

**Audit-Rhythmus:**
- Mini-Audit: nach jedem Feature (5 Min, Checkliste)
- Großer Audit: nach jedem Meilenstein (vollständige Prüfung)
- Der Audit ist NICHT optional — er ist Teil der Fertigstellung jedes Features

## Coding-Richtlinien

- **Test-Driven Development (TDD):** Tests ZUERST schreiben, dann Code
- **Comprehensive Logging:** Structured Logging mit Levels (DEBUG, INFO, WARN, ERROR)
- **Fehler mit Kontext loggen:** WAS wurde versucht, MIT WELCHEN DATEN, WARUM ging es schief
- **Keine offenen Fragen:** Wenn etwas unklar ist, die sinnvollste Entscheidung selbst treffen

## Design-Richtlinien

- **Game Design Bible:** `design/GAME_DESIGN_BIBLE.md` ist die Single Source of Truth
- **Multiplayer-Vision:** `design/MULTIPLAYER_VISION.md` (Phase 2+, nicht jetzt implementieren)
- **Content-Menge ist das Differenzierungsmerkmal:** Immer Richtung "mehr Variation, mehr Tiefe"
- **Sprache:** Kommunikation auf Deutsch, Code und UI auf Englisch

## Tech Stack

- Frontend: Electron + React + TypeScript
- Backend: C# .NET
- Charts: TradingView Lightweight Charts (npm)
- Kommunikation: lokaler WebSocket
- State Management: Zustand
- Icons: Lucide Icons
- Fonts: JetBrains Mono (Zahlen), Inter (UI)
