# StockSim — UI Design Guide

> Basiert auf Bloomberg Terminal, TradingView, ThinkOrSwim Best Practices.
> Abgeglichen mit aktuellem Ist-Zustand (Session 26).

## 1. Design-Philosophie

**"Jeder Pixel muss seine Existenz rechtfertigen."**

- Keine dekorativen Elemente in daten-dichten UIs
- Keine Gradienten rein für Ästhetik
- Keine Icons die benachbarten Text duplizieren
- Wenn ein Element dem Nutzer nicht hilft eine Entscheidung zu treffen → entfernen

**Dichte = Wert / (Zeit + Platz)**
- Visuell dicht, aber semantisch gruppiert
- Kompakt heißt nicht "alles zusammengequetscht" — sondern "sinnvolle Info füllt sinnvollen Platz"

## 2. Farbsystem

### Hintergrund-Ebenen (4 Stufen Tiefe)
| Variable | Hex | Verwendung | Status |
|----------|-----|------------|--------|
| `--bg-primary` | `#0A0E17` | Haupt-Hintergrund (dunkelster) | ✅ Korrekt |
| `--bg-secondary` | `#111827` | Panels, Header, Karten | ✅ Korrekt |
| `--bg-tertiary` | `#1F2937` | Hover-States, Badges | ✅ Korrekt |
| `--bg-input` | `#161E2E` | Input-Felder | ✅ Korrekt |

**Gut:** Rich Dark Blues statt Pure Black. Bloomberg nutzt #000000, aber für ein Spiel ist unser Ansatz besser (weniger Augen-Ermüdung).

### Semantische Farben
| Variable | Hex | Verwendung | Status |
|----------|-----|------------|--------|
| `--green-primary` | `#10B981` | Gewinn, Positiv | ✅ |
| `--red-primary` | `#EF4444` | Verlust, Negativ | ✅ |
| `--text-accent` | `#60A5FA` | Aktiv, Fokus, Links | ✅ |
| `--warning` | `#F59E0B` | Warnungen, Amber | ✅ |

**Regel:** Farbe NUR für semantische Bedeutung. Grün/Rot ausschließlich für Gewinn/Verlust. Blau für interaktive Elemente.

### Kontrast-Check
| Kombination | Ratio | Mindestens | Status |
|-------------|-------|------------|--------|
| `--text-primary` (#F9FAFB) auf `--bg-primary` (#0A0E17) | ~18:1 | 4.5:1 | ✅ |
| `--text-secondary` (#9CA3AF) auf `--bg-primary` | ~8:1 | 4.5:1 | ✅ |
| `--text-disabled` (#4B5563) auf `--bg-primary` | ~3.5:1 | 4.5:1 | ⚠️ Grenzwertig |
| `--text-disabled` (#4B5563) auf `--bg-secondary` (#111827) | ~2.8:1 | 4.5:1 | ❌ Zu niedrig |

**Fix nötig:** `--text-disabled` auf `#6B7280` erhöhen (Ratio ~5.5:1).

### Farb-Inkonsistenzen (gefunden)
| Stelle | Problem | Fix |
|--------|---------|-----|
| NewsTicker Hintergrund | Hardcoded `#080C14` | → `var(--bg-primary)` oder neue Variable |
| ScenarioBar Gradient | `#0d1321 → #111827` | → CSS Variable oder konsistenter Wert |
| CentralArea Heatmap | Hardcoded `#059669`, `#DC2626` | → Eigene Heatmap-Variablen |
| Difficulty Badges | Hardcoded in 3+ Komponenten | → Shared DIFF_COLORS Konstante |

## 3. Typografie

### Font-Stack
| Zweck | Font | Fallback | Status |
|-------|------|----------|--------|
| UI-Text | Inter | -apple-system, sans-serif | ✅ |
| Zahlen/Daten | JetBrains Mono | Fira Code, monospace | ✅ |

### Größen-System
| Element | Größe | Gewicht | Status |
|---------|-------|---------|--------|
| Headings | 16px | 600 | ✅ |
| Body/Tables | 13px | 400 | ✅ |
| Labels/Captions | 11px | 600, uppercase | ✅ |
| Small/Badges | 9-10px | 700 | ✅ |
| Min. lesbare Größe | 12px | — | ⚠️ Einige Labels bei 9px |

### Kritische Regel: Tabular Numbers
```css
.mono, [class*="mono"] {
  font-family: var(--font-mono);
  font-variant-numeric: tabular-nums lining-nums;
}
```
**Status:** ✅ In globals.css definiert. Wird konsistent für Preise/Prozente genutzt.

### Typografie-Inkonsistenzen
| Stelle | Problem | Fix |
|--------|---------|-----|
| Einige Inline-Styles | `fontFamily: "'JetBrains Mono'"` statt Variable | → `var(--font-mono)` |
| ScenarioBar | Eigene Font-Deklarationen | → CSS-Klassen nutzen |
| OrderPanel Labels | Teilweise 11px, teilweise 10px | → Einheitlich 11px |

## 4. Spacing-System

### 4px Basis-Grid
| Variable | Wert | Verwendung | Status |
|----------|------|------------|--------|
| `--space-1` | 4px | Minimaler Abstand | ✅ |
| `--space-2` | 6px | Enge Gruppierung | ✅ |
| `--space-3` | 10px | Standard-Innen | ✅ |
| `--space-4` | 14px | Standard-Außen | ✅ |
| `--space-5` | 18px | Sektion-Trennung | ✅ |

### Spacing-Inkonsistenzen
| Stelle | Problem |
|--------|---------|
| CentralArea | Mix aus `var(--space-X)` und hardcoded `12px`, `8px` |
| ScenarioBar | Alle Werte hardcoded (kein `var()`) |
| OrderPanel | Durchgehend hardcoded `12px` |
| Watchlist Rows | `padding: 8px 12px` hardcoded |

**Empfehlung:** Nicht sofort alles refactoren, aber neue Komponenten MÜSSEN CSS-Variablen nutzen.

## 5. Layout-Architektur

### Aktuelle Struktur
```
┌─────────────────────────────────────────────┐
│ TopBar (44px)                               │
├─────────────────────────────────────────────┤
│ ScenarioBar (32px, optional)                │
├──────┬──────────────────────┬───────────────┤
│ Left │                      │ Right         │
│ Side │    CentralArea       │ Sidebar       │
│ bar  │    (flex: 1)         │ (300px)       │
│(260px)│                      │               │
├──────┴──────────────────────┴───────────────┤
│ NewsTicker (32px)                           │
└─────────────────────────────────────────────┘
```

**Bewertung:**
- ✅ Klassisches Trading-Layout (Bloomberg, TradingView)
- ✅ Feste Seitenleisten, flexibler Hauptbereich
- ✅ Ticker am unteren Rand (Bloomberg-Standard)
- ⚠️ Nicht konfigurierbar (Best Practice: Panels verschiebbar/resizable)
- ⚠️ Keine Multi-Monitor-Unterstützung

### Best Practice Vergleich
| Feature | Bloomberg | TradingView | StockSim | Status |
|---------|-----------|-------------|----------|--------|
| Panel-Layout | Frei konfigurierbar | Frei konfigurierbar | Fest | ⚠️ V2 |
| Keyboard-First | Ja (alles) | Teilweise | 15+ Shortcuts | ✅ Gut |
| Tab-System | Multi-Window | Tabs + Popouts | 7 Tabs | ✅ |
| Watchlist | Multiple | Multiple | Multiple (5) | ✅ |
| Chart-Overlay | Indikatoren + Zeichnen | Vollständig | 6 Indikatoren | ✅ |
| Order Entry | Inline | Sidebar | Sidebar | ✅ |

## 6. Interaktions-Patterns

### Hover-States
| Element | Aktuell | Best Practice | Status |
|---------|---------|---------------|--------|
| Buttons | `brightness(1.15)` | Border-Farbe + Background | ⚠️ Subtil |
| Tab-Buttons | Color transition 150ms | Background + Border | ✅ |
| Tabellen-Zeilen | Background → tertiary | Background + Left-Border | ✅ |
| Stock-Rows | Manual onMouseEnter | CSS `:hover` | ⚠️ Inkonsistent |
| Dropdowns | Keine Custom-Styles | Custom Dropdown | ⚠️ System-Default |

### Animationen
| Animation | Dauer | Verwendung | Best Practice | Status |
|-----------|-------|------------|---------------|--------|
| Preis-Flash | 0.8s | Preisänderung Grün/Rot | 0.5-1s | ✅ |
| News-Pulse | 1.5s infinite | Breaking News | Sparsam nutzen | ✅ |
| Ticker-Scroll | 30-60s | News-Ticker | Geschwindigkeit einstellbar | ✅ (Settings) |
| Fortschrittsbalken | 0.8s ease | ScenarioBar | Smooth | ✅ |

**Fehlende Animationen:**
- Seiten-Wechsel (Tab-Transition) — kein Fade/Slide
- Preisänderungen in Tabellen — nur Flash, kein Zähler-Animation
- Neue News — erscheinen abrupt, kein Slide-In
- Achievement-Popup — kein Entry-Animation definiert

### Timing-Richtlinien
| Verzögerung | Strategie |
|-------------|-----------|
| <100ms | Keine Animation nötig |
| 100ms-1s | Subtile Transition |
| 1-10s | Indeterminate Loader |
| >10s | Progress Bar |

## 7. Gamification-Elemente

### Vorhanden
| Element | Status | Bewertung |
|---------|--------|-----------|
| Achievements (31) | ✅ | Gut, fehlt: Animation bei Unlock |
| Karriere-Titel | ❌ | Geplant Phase 4 |
| Leaderboard | ❌ | V2 (Multiplayer) |
| Portfolio-Milestones | ✅ (via Achievements) | Könnte sichtbarer sein |
| Szenario-Progress | ✅ (ScenarioBar) | Bloomberg-Style, funktional |
| Decision Cases | ✅ (8 Cases) | Gutes Lern-Tool |

### Fehlend (Priorität)
1. **Karriere-Progression sichtbar** — Junior Trader → Fund Manager → Legend in TopBar
2. **Achievement-Popup mit Animation** — Aktuell nur Text, kein Confetti/Shine
3. **Portfolio-Meilenstein-Marker** — $100K, $500K, $1M mit visueller Feier
4. **Trading-Streak-Anzeige** — "5 Profitable Trades in a Row!"

**Anti-Pattern vermeiden:** Kein Confetti bei jedem Trade (Robinhood-Kritik). Feiern nur bei echten Meilensteinen.

## 8. Accessibility

### Aktueller Stand
| Feature | Status | Details |
|---------|--------|---------|
| Dark Theme | ✅ | Durchgehend |
| High Contrast Mode | ✅ | In Settings |
| Colorblind Modes | ✅ | 3 Modi (Deuteranopie, Protanopie, Tritanopie) |
| Reduced Motion | ✅ | In Settings |
| Large Text | ✅ | In Settings |
| Keyboard Shortcuts | ✅ | 15+ definiert |
| Focus-Visible | ✅ | In globals.css |
| Screen Reader | ⚠️ | Fehlende aria-labels |
| Focus Trap (Modals) | ❌ | Nicht implementiert |

### Farbblindheit
**Kritisch:** Grün/Rot für Gewinn/Verlust → 8% der Männer betroffen.
**Aktuelle Lösung:** Colorblind-Modi + Pfeilsymbole (▲▼) bei Preisänderungen.
**Verbesserung:** Immer +/- Vorzeichen zeigen (bereits der Fall bei Prozenten ✅).

## 9. Abgleich: Soll vs. Ist

### ✅ Richtig gemacht
1. **Farbsystem** — 4 Hintergrund-Ebenen, semantische Farben, konsistente Variablen
2. **Typografie** — JetBrains Mono für Zahlen, Inter für UI, tabular-nums aktiv
3. **Layout** — Klassisches Trading-Layout, Bloomberg-inspiriert
4. **Daten-Dichte** — Hoch aber lesbar, Watchlist + Chart + Orders gleichzeitig
5. **Keyboard-Shortcuts** — 15+ Shortcuts, Ctrl+K Command Bar
6. **Ticker** — Bloomberg TV Style mit News/Tape-Modi
7. **Accessibility** — Colorblind, High Contrast, Reduced Motion, Large Text
8. **7-Tab-System** — Gut organisiert, Shortcuts D/P/M/O/N/A/J

### ⚠️ Verbesserungswürdig
1. **Hardcoded Farben** — ~20 Stellen mit Hex statt CSS-Variable
2. **Spacing Inkonsistenz** — Mix aus var() und hardcoded px
3. **Hover-States** — Stock-Rows nutzen JS statt CSS :hover
4. **Dropdown-Styling** — System-Default statt Custom
5. **Tab-Transition** — Kein Übergangs-Effekt zwischen Tabs
6. **`--text-disabled` Kontrast** — Zu niedrig auf `--bg-secondary`

### ❌ Fehlt noch
1. **Panel-Konfiguration** — Panels nicht verschiebbar/resizable (V2)
2. **Karriere-Progression** — Kein sichtbarer Rang in TopBar (Phase 4)
3. **Achievement-Animation** — Kein Entry-Effekt bei Popup
4. **Focus Trap in Modals** — Modals fangen Tab-Navigation nicht ein
5. **Aria-Labels** — Icon-Buttons brauchen aria-label Attribute
6. **Zähler-Animationen** — Preise springen statt zu zählen

### Prioritäten für nächste Sessions
| Prio | Item | Aufwand | Phase |
|------|------|---------|-------|
| 1 | `--text-disabled` Kontrast fixen | 1 Zeile | Jetzt |
| 2 | Hardcoded Farben → CSS Vars | 1h | Jetzt |
| 3 | Achievement-Popup Animation | 30min | Phase 4 |
| 4 | Karriere-Progression TopBar | 2h | Phase 4 |
| 5 | Panel-Konfiguration | 2 Tage | V2 |
| 6 | Focus Trap für Modals | 1h | Phase 4 |
