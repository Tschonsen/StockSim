# StockSim — Claude Code Projektregeln

## REGEL 1: Orientierung vor Arbeit

**Jedes neue Gespräch startet so:**
1. `design/CURRENT_STATE.md` lesen — aktueller Stand, Bugs, offene Punkte (~50 Zeilen)
2. `design/WORLD_SIM_VISION.md` lesen — **Nordstern des laufenden World-Sim-Pivots** (Richtung, Architektur, Meilensteine)
3. `design/ARCHITECTURE.md` lesen — welche Datei macht was, Abhängigkeiten
4. Nur die für die aktuelle Aufgabe relevanten Dateien lesen

**NIE die ganze Spec lesen.** Stattdessen:
- `design/DESIGN_INDEX.md` für Sektions-Übersicht + Implementierungsstatus
- Nur die relevante Spec-Sektion lesen wenn Details nötig sind

**Session-History** (`design/SESSION_HISTORY.md`) nur lesen wenn historischer Kontext nötig ist.

## REGEL 2: Zustandssicherung

**NACH jeder Arbeit (oder bei natürlichem Ende eines Gesprächs):**
1. `design/CURRENT_STATE.md` aktualisieren: was wurde getan, neuer Stand, nächste Schritte
2. `design/SESSION_HISTORY.md` ergänzen wenn eine Session abgeschlossen ist
3. Bei Architektur-Änderungen: `design/ARCHITECTURE.md` aktualisieren
4. Bei größeren Änderungen: relevante Memory-Dateien aktualisieren

**CURRENT_STATE.md kurz halten!** Max ~60 Zeilen. Keine Session-Logs dort — die gehören in SESSION_HISTORY.md.

**WARUM:** Bei Token-Limit, Internet-Ausfall oder Stromausfall muss das nächste Gespräch SOFORT wissen was los ist.

## REGEL 3: Audits nach Umsetzung

**Nach jeder Feature-Implementierung (Mini-Audit):**
1. Code stimmt mit Spec überein?
2. Tests geschrieben und grün?
3. Logging vorhanden?
4. CURRENT_STATE.md aktualisiert?

**Nach jedem Meilenstein (Großer Audit):**
1. Alle Mini-Audit-Punkte
2. Performance-Check
3. Integrations-Test
4. Ergebnis in `design/AUDIT_LOG.md`

## REGEL 4: Context Window schonen

- **Kleine, fokussierte Aufgaben** pro Gespräch
- **Nicht alles auf einmal lesen** — nur was für die aktuelle Aufgabe nötig ist
- **Agents nutzen** für parallele Recherche statt alles im Hauptkontext zu laden
- **CentralArea.tsx (~2200 Zeilen)** nie komplett lesen — nur den relevanten Tab-Abschnitt

## Coding-Richtlinien

- **Test-Driven Development (TDD):** Tests ZUERST schreiben, dann Code
- **Comprehensive Logging:** Structured Logging mit Levels (DEBUG, INFO, WARN, ERROR)
- **Fehler mit Kontext loggen:** WAS wurde versucht, MIT WELCHEN DATEN, WARUM ging es schief
- **Keine offenen Fragen:** Wenn etwas unklar ist, die sinnvollste Entscheidung selbst treffen

## Brain-Files & Kontext-Hygiene (wichtig — Projekt wächst)

- **Brain-Files sind Pflicht und ausführlich** (§1.6 global). Je größer das Projekt, desto mehr sparen gute
  Brain-Files Kontext-Tokens — für jede erstellte/substantiell editierte Source-Datei eine gründliche
  `<file>.brain`-Summary (Purpose, Key exports, Collaborators, Gotchas, Conventions).
- **Von Claude schreiben, NICHT auslagern:** Brain-Files werden vom Hauptmodell (Claude) erstellt, **nicht**
  an das lokale CodeBrain-14B-Modell delegiert — dafür ist es zu schwach (Qualität > Token-Ersparnis).
  Das überschreibt bewusst die „nutze CodeBrain-MCP wenn vorhanden"-Empfehlung aus §1.6 für dieses Projekt.
- **Abgrenzung:** Das gilt nur für **Dev-Tooling** (Brain-Files/Doku). Das **KI-Modell IM Spiel** (Preismodell,
  Content-Generierung) ist ein Produkt-Feature und darf ausgebaut werden, wenn es das Spiel verbessert (siehe
  `WORLD_SIM_VISION.md` §7).

## Design-Richtlinien

- **Design Spec:** `design/DESIGN_SPEC.md` ist die Single Source of Truth
- **Spec-Index:** `design/DESIGN_INDEX.md` für schnellen Überblick
- **Multiplayer-Vision:** `design/MULTIPLAYER_VISION.md` (Phase 2+, nicht jetzt implementieren)
- **Content-Menge ist das Differenzierungsmerkmal:** Immer Richtung "mehr Variation, mehr Tiefe"
- **Sprache:** Kommunikation auf Deutsch, Code und UI auf Englisch

## Tech Stack

- Frontend: Electron + React + TypeScript
- Backend: C# .NET
- Charts: TradingView Lightweight Charts (npm)
- Kommunikation: lokaler WebSocket (Port 8765)
- State Management: Zustand
- Icons: Lucide Icons
- Fonts: JetBrains Mono (Zahlen), Inter (UI)

## Dateien-Übersicht

| Datei | Zweck | Wann lesen? |
|-------|-------|-------------|
| `design/CURRENT_STATE.md` | Aktueller Stand, Bugs, nächste Schritte | **Immer zuerst** |
| `design/WORLD_SIM_VISION.md` | Nordstern des World-Sim-Pivots (Vision, Architektur, Meilensteine) | **Immer als zweites** (solange Pivot läuft) |
| `design/ARCHITECTURE.md` | Datei-Index mit Beschreibungen | **Immer als drittes** |
| `design/DESIGN_INDEX.md` | Spec-Sektionen + Implementierungsstatus | Bei Feature-Arbeit |
| `design/DESIGN_SPEC.md` | Vollständige Design-Spezifikation | Nur relevante Sektion |
| `design/SESSION_HISTORY.md` | Archiv alter Session-Logs | Nur bei Bedarf |
| `design/AUDIT_LOG.md` | Audit-Ergebnisse | Nach Meilensteinen |
