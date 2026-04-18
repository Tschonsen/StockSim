# StockSim — Game Design Spec

**Version:** 1.0
**Datum:** 22. März 2026
**Status:** In Arbeit

> Dieses Dokument ist auf Deutsch verfasst. Alle UI-Texte und In-Game-Beschriftungen sind auf Englisch, da das Spiel in englischer Sprache erscheint.

---

## Inhaltsverzeichnis

1. [Vision & Überblick](#1-vision--überblick)
2. [Visual Style Guide](#2-visual-style-guide)
3. [Haupt-UI-Layout](#3-haupt-ui-layout)
4. [Trading Mechanics](#4-trading-mechanics)
5. [Marktsimulation](#5-marktsimulation)
6. [Portfolio Management](#6-portfolio-management)
7. [AI-Trader](#7-ai-trader)
8. [Events System](#8-events-system)
9. [Regulierung, Illegale Handlungen & Konsequenzen](#9-regulierung)
10. [Zeitsystem](#10-zeitsystem)
11. [Aktien- und Sektor-Generierung](#11-aktien--und-sektor-generierung)
12. [Charts & Datenvisualisierung](#12-charts--datenvisualisierung)
13. [News Ticker & News System](#13-news-ticker--news-system)
14. [Tutorial & Onboarding](#14-tutorial--onboarding)
15. [Speicher- und Ladesystem](#15-speicher--und-ladesystem)
16. [Settings & Konfiguration](#16-settings--konfiguration)
17. [Audio Design](#17-audio-design)
18. [Tastenkürzel](#18-tastenkürzel)
19. [Edge Cases & Fehlerzustände](#19-edge-cases--fehlerzustände)
20. [Phasenplanung](#20-phasenplanung)
21. [Technische Architektur](#21-technische-architektur)
22. [Anhang](#22-anhang)

---

## 1. Vision & Überblick

### 1.1 Elevator Pitch

StockSim ist eine realistische Börsensimulation, in der der Spieler mit prozedural generierten Aktien handelt, während KI-gesteuerte Marktteilnehmer und ein dynamisches Event-System für lebendige, unvorhersehbare Märkte sorgen. Das Spiel kombiniert die Informationsdichte eines Bloomberg-Terminals mit dem Spielgefühl eines Wall-Street-Thrillers.

**Referenz-Satz:** "Bloomberg Terminal meets STONKS-9800."

### 1.2 Design-Säulen

**Realismus**
Die Simulation bildet echte Marktmechaniken so genau wie möglich ab. Preise bewegen sich nicht zufällig — sie reagieren auf Angebot, Nachfrage, Nachrichten und die Handlungen hunderter KI-Trader. Short Squeezes, Margin Calls und Flash Crashes entstehen organisch aus dem System, nicht aus Skripten.

**Zugänglichkeit**
Ein Börsenneuling soll das Spiel starten, das Tutorial durchlaufen und innerhalb von 10 Minuten seinen ersten Trade verstehen können. Gleichzeitig findet ein erfahrener Trader genug Tiefe, um Strategien zu testen, die er im echten Markt nicht riskieren würde. Die UI zeigt grundlegende Informationen prominent und versteckt fortgeschrittene Features hinter einem weiteren Klick — nie andersherum.

**Immersion**
Jeder Bildschirm soll sich anfühlen wie ein professionelles Trading-Terminal. Die Ästhetik (dunkle Farben, leuchtende Zahlen, News-Ticker), die Sounds (Börsen-Glocke, Trade-Bestätigungston) und die Events (Breaking News mit Eilmeldungs-Jingle) erzeugen zusammen das Gefühl, auf einem echten Trading Floor zu sitzen.

**Emergenz**
Die spannendsten Momente entstehen nicht aus vorgefertigten Szenarien, sondern aus dem Zusammenspiel der Systeme. Ein Zinsanstieg der Zentralbank drückt Tech-Aktien, was einen überhebelmten KI-Fonds in einen Margin Call treibt, dessen erzwungene Verkäufe eine Kettenreaktion auslösen — und plötzlich steckt der Spieler mitten in einem Mini-Crash, den niemand geplant hat.

### 1.3 Zielgruppen-Profile

**Profil A: Der Börsenneuling**
Hat wenig bis keine Erfahrung mit Aktienhandel. Kommt zum Spiel, weil er verstehen will, wie die Börse funktioniert, ohne echtes Geld zu riskieren. Braucht klare Erklärungen, ein geführtes Tutorial und Tooltips. Wird frustriert durch zu viele Zahlen auf einmal, unverständliche Fachbegriffe ohne Erklärung und Verluste, die er nicht nachvollziehen kann. Wird gehalten durch sichtbare Lernerfolge ("Ah, DESHALB fällt die Aktie nach der Nachricht"), langsame Steigerung der Komplexität und das befriedigende Gefühl, eine Marktbewegung vorhergesagt zu haben.

**Profil B: Der erfahrene Trader**
Kennt Ordertypen, technische Analyse und Marktmechaniken. Sucht eine Sandbox, um Strategien zu testen (Short Selling, Margin Trading, Event-basiertes Trading), ohne echtes Risiko. Wird frustriert durch unrealistische Preisbewegungen, fehlende Ordertypen und eine zu einfache Simulation. Wird gehalten durch realistische Marktdynamiken, genug Tiefe für komplexe Strategien, seltene Events (Short Squeeze, Flash Crash), die selbst für Profis spannend sind.

**Profil C: Der Strategiespieler**
Spielt Paradox-Spiele, Factorio oder andere Simulationen. Kommt zum Spiel, weil er ein neues System zum Optimieren sucht. Kennt vielleicht keine Börsenkonzepte, lernt sie aber schnell. Wird frustriert durch fehlende Geschwindigkeitskontrolle (will vorspulen können) und mangelnde Tiefe im Langzeitspiel. Wird gehalten durch die HOI4-artige Zeitsteuerung, emergente Ereignisse, die Langzeit-Planung belohnen und Analytics, die seinen Fortschritt messbar machen.

### 1.4 Spielziel, Endbedingungen & Meilensteine

**Ist StockSim ein Sandbox-Spiel?**
Ja — aber mit Struktur. Es gibt kein "Game Over: You Win"-Screen. Stattdessen gibt es ein Meilenstein-System, das dem Spieler Ziele gibt, ohne ihn einzuschränken.

**Primäres Spielziel:** Vermögen aufbauen. Der Spieler startet mit $25k-$100k und versucht, durch cleveres Trading so viel wie möglich zu verdienen. Es gibt keine obere Grenze — das Ziel ist selbstgesteckt.

**Endbedingungen:**

| Bedingung | Was passiert |
|---|---|
| **Bankrott (Cash ≤ $0 + keine Positionen)** | Popup: `You've gone bankrupt.` Optionen: `[Restart with $10,000]` (Neustart mit Handicap im gleichen Markt, varies by difficulty: Easy $25,000, Normal $10,000, Hard $5,000) oder `[New Game]` oder `[Load Save]`. Der Markt läuft weiter — der Spieler kann mit wenig Kapital versuchen, sich zu erholen. |
| **SMA Account Freeze** | Siehe Kapitel 9.4.5 — schwere Strafe, aber kein echtes Game Over. Spieler kann weiterspielen mit Handicap. |
| **Freiwilliger Ruhestand** | Im Menü: `Retire` Button (ab $1M Portfolio). Zeigt einen "Career Summary" Screen mit allen Statistiken. Der Spieler kann danach ein neues Spiel starten oder weiterspielen. |

**Bankrott-Mechanik im Detail:**
- Cash = $0 ist noch KEIN Bankrott (Spieler hat Positionen die er verkaufen kann)
- Bankrott = Cash ≤ $0 UND Gesamtportfolio-Wert ≤ $0 (alle Positionen + Cash)
- Kann passieren durch: Margin-Liquidation mit Restschuld, Short Squeeze mit unbegrenztem Verlust, SMA-Strafen
- Bei Bankrott wird das Spiel automatisch pausiert
- Der "Restart with $10,000 (varies by difficulty: Easy $25,000, Normal $10,000, Hard $5,000)" Modus behält den aktuellen Markt (inkl. alle Aktien, Events, AI-Zustand) — nur das Spielerkonto wird zurückgesetzt

#### 1.4.1 Achievements / Meilensteine

Achievements geben dem Spieler Kurzzeit- und Langzeit-Ziele und belohnen verschiedene Spielstile.

**Achievement-Kategorien:**

**Vermögen (Wealth):**
| Achievement | Bedingung | Icon |
|---|---|---|
| First Steps | Portfolio-Wert erreicht $60,000 (bei $50k Start) | 🏃 |
| Six Figures | Portfolio-Wert erreicht $100,000 | 💯 |
| Quarter Million | Portfolio-Wert erreicht $250,000 | 📈 |
| Half Millionaire | Portfolio-Wert erreicht $500,000 | 💰 |
| Millionaire | Portfolio-Wert erreicht $1,000,000 | 🏆 |
| Multi-Millionaire | Portfolio-Wert erreicht $5,000,000 | 👑 |
| Tycoon | Portfolio-Wert erreicht $10,000,000 | 🏛️ |

**Trading-Skills:**
| Achievement | Bedingung |
|---|---|
| First Blood | Erster profitabler Trade |
| Sharpshooter | 10 profitable Trades in Folge |
| Day Trader | 50 Trades an einem Handelstag |
| Diamond Hands | Position halten durch -20% Drawdown und danach mit Gewinn verkaufen |
| Short Master | $10,000+ Gewinn durch Short Selling |
| Squeeze Survivor | Short Squeeze überleben ohne Liquidation |
| Dip Buyer | Aktie kaufen die >15% gefallen ist und mit >20% Gewinn verkaufen |
| Earnings Whisperer | 5× vor Earnings die richtige Richtung vorhersagen |

**Markt-Erfahrung:**
| Achievement | Bedingung |
|---|---|
| Market Veteran | 1 Jahr Spielzeit überleben |
| Crash Survivor | Portfolio-Wert sinkt nie unter -30% während eines Market-Crashes |
| Bull Rider | $100k+ Gewinn in einem Bull Market |
| Bear Tamer | Positiv durch einen Bear Market kommen |
| Black Swan | Einen Flash Crash erleben |
| Sector Specialist | >$50k Gewinn ausschließlich in einem Sektor |

**Risiko & Regulierung:**
| Achievement | Bedingung |
|---|---|
| Clean Record | 1 Jahr Spielzeit ohne SMA-Warnung |
| Tax Optimizer | Effektive Steuerrate unter 18% durch Tax Loss Harvesting |
| Close Call | SMA-Score erreicht 50+ und dann wieder auf 0 bringen |
| Wolf of Wall Street | $100k+ Gewinn durch illegale Handlungen (auch wenn bestraft) |
| Comeback Kid | Nach Bankrott auf $100k Portfolio-Wert kommen |

**UI-Anzeige:**
- Achievement-Popup: wenn erreicht, erscheint ein Banner oben im Bildschirm (Gold-Farbe `#D4AF37`, 3 Sekunden, mit Sound)
- Achievement-Galerie: zugänglich über Settings → `Achievements` — zeigt alle Achievements (erreicht und unerreicht)
- Unerreichte Achievements: ausgegraut mit Bedingung verborgen (nur Titel sichtbar, Bedingung als `???`)
- Steam-Integration: Achievements werden mit Steam Achievements synchronisiert

#### 1.4.2 Scenario Mode / Herausforderungen

Neben dem Sandbox-Modus gibt es vordefinierte Szenarien mit spezifischen Startbedingungen und Zielen.

**Zugang:** Hauptmenü → `New Game` → Tab `Scenarios` (neben `Sandbox`)

**Szenarien:**

| Szenario | Startbedingung | Ziel | Zeitlimit |
|---|---|---|---|
| **The Crash** | Markt befindet sich am Beginn eines -40% Crashes. $50k Start. | Überlebe mit positivem Portfolio-Wert | 6 Monate (Spielzeit) |
| **Bull Run** | Markt befindet sich im starken Aufschwung. $25k Start. | Erreiche $100k | 3 Monate |
| **Short Squeeze** | Eine Aktie hat 60% Short Interest. $30k Start. | Erkenne und profitiere vom Squeeze. Ziel: $80k | 1 Monat |
| **The Insider** | Spieler bekommt häufiger Rumors (3× so oft). $50k Start. | Erreiche $200k ohne SMA-Strafe | 6 Monate |
| **One Stock** | Spieler darf nur EINE Aktie handeln (keine Diversifikation). $50k Start. | Erreiche $150k | 1 Jahr |
| **Recession** | Wirtschaft in Rezession, Arbeitslosigkeit steigt, Zinsen hoch. $100k Start. | Überlebe das Jahr mit weniger als -10% Verlust | 1 Jahr |
| **Penny Stocks** | Nur Aktien unter $5 handeln. $10k Start. | Erreiche $50k | 6 Monate |
| **Dividend King** | Nur Dividend-Aktien. $100k Start. | Erreiche $5k/Quartal passives Dividendeneinkommen | 2 Jahre |
| **Speed Run** | Normaler Markt. $50k Start. | Erreiche $1M so schnell wie möglich | Kein Limit |
| **Iron Man** | Kein Speichern erlaubt (Autosave only, kein Laden). $50k Start. | Überlebe 1 Jahr mit >$50k | 1 Jahr |

**Szenario-Completion:**
- Bei Erfolg: Achievement + Career Summary Screen mit Statistiken
- Bei Scheitern: `Scenario Failed` Screen mit Analyse (was ging schief, wo hätte man anders handeln können)
- Bestenliste: persönliche Bestzeiten / Bestwerte pro Szenario (lokal gespeichert)

### 1.5 Kern-Spielschleife (Core Loop)

Der Gameplay-Loop besteht aus fünf Phasen, die sich zyklisch wiederholen:

```
┌──────────────────────────────────────────────────┐
│                                                  │
│   ① BEOBACHTEN                                   │
│   Märkte scannen, Charts lesen, News prüfen      │
│                    │                              │
│                    ▼                              │
│   ② ANALYSIEREN                                  │
│   Trends erkennen, Events bewerten,              │
│   Sektoren vergleichen                           │
│                    │                              │
│                    ▼                              │
│   ③ HANDELN                                      │
│   Orders platzieren (Buy, Sell, Short,           │
│   Limit, Stop)                                   │
│                    │                              │
│                    ▼                              │
│   ④ BEOBACHTEN                                   │
│   Ausführung verfolgen, P&L beobachten,          │
│   auf Events reagieren                           │
│                    │                              │
│                    ▼                              │
│   ⑤ ANPASSEN                                     │
│   Portfolio rebalancen, Gewinne mitnehmen,       │
│   Verluste begrenzen, neue Chancen suchen        │
│                    │                              │
│                    └──── zurück zu ① ─────────────│
│                                                  │
└──────────────────────────────────────────────────┘
```

**Frühe Spielphase (erste 30 Minuten):** Der Spieler besitzt nur Cash, beobachtet den Markt, tätigt vorsichtige erste Käufe. Der Loop dreht sich schnell: kaufen, beobachten, verkaufen, lernen. Zeitsteuerung steht meist auf 1x oder 2x.

**Mittlere Spielphase (1-5 Stunden):** Der Spieler hat ein diversifiziertes Portfolio, nutzt Limit Orders und Stop Losses, reagiert auf Events. Der Loop wird breiter: mehr Analyse, weniger impulsive Trades. Zeitsteuerung wechselt häufig zwischen Pause (für Analyse) und 5x (zum Vorspulen).

**Späte Spielphase (5+ Stunden):** Der Spieler nutzt Short Selling, Margin Trading, beobachtet Sektor-Rotationen, spekuliert auf Events. Der Loop wird strategischer: langfristige Positionen, Portfolio-Optimierung, Risikomanagement. Zeitsteuerung auf 10x mit häufigem Pausieren bei Events.

### 1.6 Technologie-Stack

```
┌─────────────────────────────────────────────────┐
│                   FRONTEND                       │
│         Electron + React + TypeScript            │
│                                                  │
│  ┌─────────────┐  ┌──────────────────────────┐  │
│  │ React UI    │  │ TradingView Lightweight   │  │
│  │ Components  │  │ Charts (npm)              │  │
│  └─────────────┘  └──────────────────────────┘  │
│                                                  │
│  Aufgabe: Reine Anzeige und Benutzerinteraktion  │
│  Keine Spiellogik, keine Berechnungen            │
└──────────────────────┬──────────────────────────┘
                       │
                 WebSocket (lokal)
                 JSON-Messages
                       │
┌──────────────────────┴──────────────────────────┐
│                   BACKEND                        │
│                  C# .NET                         │
│                                                  │
│  ┌───────────┐ ┌────────────┐ ┌──────────────┐  │
│  │Simulation │ │ AI-Trader  │ │ Event-Engine │  │
│  │ Engine    │ │ System     │ │              │  │
│  └───────────┘ └────────────┘ └──────────────┘  │
│  ┌───────────┐ ┌────────────┐ ┌──────────────┐  │
│  │ Trading   │ │ Save/Load  │ │ WebSocket    │  │
│  │ Engine    │ │ System     │ │ Server       │  │
│  └───────────┘ └────────────┘ └──────────────┘  │
│                                                  │
│  Aufgabe: Gesamte Spiellogik, Preisberechnung,   │
│  KI, Events, Savegames                           │
└─────────────────────────────────────────────────┘
```

**Datenfluss:**
1. Backend berechnet pro Tick: neue Preise, KI-Entscheidungen, Event-Prüfungen, Order-Matching
2. Backend sendet gebatchte Updates über WebSocket ans Frontend (Preise, Portfolio-Status, Events)
3. Frontend rendert die Daten (Charts, Tabellen, Ticker)
4. Spieler interagiert mit Frontend (Order platzieren, Speed ändern, navigieren)
5. Frontend sendet Spieler-Aktionen über WebSocket ans Backend
6. Backend verarbeitet Spieler-Aktionen im nächsten Tick

### 1.7 Plattform & Distribution

- **Plattform:** Steam (Windows, Desktop)
- **Electron-zu-Steam:** Electron-Apps lassen sich als Steam-Titel vertreiben. Steam Overlay und Steamworks SDK werden über Native Node-Module eingebunden.
- **Geplante Steam-Features (Post-Launch):** Achievements, Cloud Saves, ggf. Steam Workshop für Custom Events
- **Minimale Systemanforderungen (Ziel):** Windows 10, 8 GB RAM, keine dedizierte GPU nötig (keine 3D-Grafik)

---

## 2. Visual Style Guide

### 2.1 Ästhetische Leitlinie

Das visuelle Design orientiert sich am Bloomberg Terminal: dunkel, informationsdicht, professionell. Jeder Pixel dient der Informationsübermittlung. Dekorative Elemente existieren nur, wenn sie die Immersion stärken (z.B. ein subtiler Glow auf aktiven Chart-Linien).

**Grundregeln:**
- Modern und clean, NICHT retro oder pixelig
- Dunkel mit hohem Kontrast für Zahlen
- Farbe hat Bedeutung: Grün = positiv/Gewinn, Rot = negativ/Verlust, immer und überall konsistent
- Informationsdichte wie Bloomberg, aber mit klarer visueller Hierarchie — das Wichtigste ist am größten und hellsten
- Kein visuelles Rauschen: keine Texturen, keine Gradienten auf Flächen, keine unnötigen Borders

### 2.2 Farbpalette

#### Hintergrundfarben
| Bezeichnung | Hex | Verwendung |
|---|---|---|
| `bg-primary` | `#0A0E17` | Haupt-Hintergrund des gesamten Fensters |
| `bg-secondary` | `#111827` | Panel-Hintergründe (Sidebar, Watchlist, News) |
| `bg-tertiary` | `#1F2937` | Karten, erhöhte Elemente, Hover-Hintergründe |
| `bg-input` | `#161E2E` | Eingabefelder, Dropdowns |
| `bg-modal` | `#111827` | Modal-Hintergrund |
| `bg-overlay` | `rgba(0, 0, 0, 0.7)` | Hintergrund-Dim hinter Modals |

#### Akzentfarben (Positiv / Negativ)
| Bezeichnung | Hex | Verwendung |
|---|---|---|
| `green-primary` | `#10B981` | Positive Zahlen, Kursgewinne, Buy-Buttons |
| `green-dim` | `#065F46` | Grüne Hintergründe (z.B. positive Zeile in Tabelle) |
| `green-glow` | `rgba(16, 185, 129, 0.3)` | Glow-Effekt auf positiven Chart-Bereichen |
| `red-primary` | `#EF4444` | Negative Zahlen, Kursverluste, Sell-Buttons, Warnungen |
| `red-dim` | `#7F1D1D` | Rote Hintergründe (z.B. Margin-Warnung) |
| `red-glow` | `rgba(239, 68, 68, 0.3)` | Glow-Effekt auf negativen Chart-Bereichen |

#### Textfarben
| Bezeichnung | Hex | Verwendung |
|---|---|---|
| `text-primary` | `#F9FAFB` | Haupttext, wichtige Zahlen, Überschriften |
| `text-secondary` | `#9CA3AF` | Labels, Spaltenüberschriften, weniger wichtige Info |
| `text-disabled` | `#4B5563` | Deaktivierte Elemente, Platzhaltertext |
| `text-accent` | `#60A5FA` | Links, aktive Tabs, ausgewählte Elemente |

#### Systemfarben
| Bezeichnung | Hex | Verwendung |
|---|---|---|
| `warning` | `#F59E0B` | Warnungen (Margin Warning, ungewöhnliches Volumen) |
| `info` | `#3B82F6` | Info-Tooltips, neutrale Hinweise |
| `border` | `#1F2937` | Panel-Ränder, Trennlinien |
| `border-hover` | `#374151` | Ränder bei Hover |
| `border-focus` | `#3B82F6` | Ränder bei Fokus (Eingabefelder) |

#### Chart-spezifische Farben
| Bezeichnung | Hex | Verwendung |
|---|---|---|
| `candle-up-body` | `#10B981` | Körper einer steigenden Kerze |
| `candle-up-wick` | `#10B981` | Docht einer steigenden Kerze |
| `candle-down-body` | `#EF4444` | Körper einer fallenden Kerze |
| `candle-down-wick` | `#EF4444` | Docht einer fallenden Kerze |
| `volume-up` | `rgba(16, 185, 129, 0.3)` | Volumen-Bar bei steigender Kerze |
| `volume-down` | `rgba(239, 68, 68, 0.3)` | Volumen-Bar bei fallender Kerze |
| `crosshair` | `#6B7280` | Fadenkreuz-Linien im Chart |
| `grid` | `rgba(31, 41, 55, 0.5)` | Chart-Gitterlinien |
| `sma-20` | `#F59E0B` | SMA 20 Linie (Amber) |
| `sma-50` | `#8B5CF6` | SMA 50 Linie (Lila) |
| `sma-200` | `#EC4899` | SMA 200 Linie (Pink) |
| `bollinger` | `rgba(96, 165, 250, 0.2)` | Bollinger Band Füllung |
| `bollinger-line` | `#60A5FA` | Bollinger Band Linien |

### 2.3 Typografie

**Primärschrift (Zahlen & Daten):** `JetBrains Mono`
Monospace-Schrift, damit Zahlen in Tabellen exakt untereinander stehen. Alle Preise, Prozentwerte, Volumen-Zahlen und Zeitstempel verwenden diese Schrift.

**Sekundärschrift (UI-Labels & Text):** `Inter`
Klare Sans-Serif für Labels, Menüs, Buttons, Fließtext und News-Headlines.

#### Schriftgrößen-Skala
| Stufe | Größe | Zeilenhöhe | Verwendung |
|---|---|---|---|
| `xs` | 10px | 14px | Kleinste Beschriftungen, Chart-Achsen |
| `sm` | 12px | 16px | Tabellen-Inhalt, Watchlist-Einträge, Timestamps |
| `md` | 14px | 20px | Standard-Text, Labels, Buttons, News-Text |
| `lg` | 16px | 24px | Panel-Überschriften, Navigations-Tabs |
| `xl` | 20px | 28px | Aktien-Symbol in Detail-Ansicht, Sektions-Titel |
| `xxl` | 28px | 36px | Aktueller Preis in Aktien-Detail, Portfolio-Gesamtwert |
| `hero` | 36px | 44px | Breaking News Headline, Dashboard-Kennzahl |

#### Zahlenformatierung
- Tausender-Trenner: Komma (englische Notation, da Spielsprache Englisch): `1,234,567.89`
- Preise: immer 2 Dezimalstellen: `$142.58`
- Prozent: 2 Dezimalstellen mit Vorzeichen: `+2.34%` oder `-1.07%`
- Volumen: Abgekürzt ab Tausend: `1.2K`, `3.5M`, `1.1B`
- Währung: USD-Symbol vor dem Betrag: `$50,000.00`
- Negative Beträge in Rot mit Minus: `-$1,234.56`
- Positive Beträge in Grün mit Plus (wo Veränderung dargestellt wird): `+$567.89`

### 2.4 Iconografie

- **Stil:** Outline-Icons, Strichstärke 1.5px, abgerundete Ecken
- **Icon-Library:** Lucide Icons (Open Source, konsistenter Stil, React-kompatibel)
- **Größen:** S = 16px, M = 20px, L = 24px

#### Kern-Icons und ihre Zuordnung
| Funktion | Icon | Größe |
|---|---|---|
| Kaufen (Buy) | `arrow-up-circle` | M |
| Verkaufen (Sell) | `arrow-down-circle` | M |
| Short | `trending-down` | M |
| Chart (Candle) | `candlestick-chart` | M |
| Chart (Line) | `line-chart` | M |
| Suche | `search` | M |
| Filter | `filter` | M |
| Sortierung auf | `chevron-up` | S |
| Sortierung ab | `chevron-down` | S |
| Settings | `settings` | M |
| Speichern | `save` | M |
| Pause | `pause` | M |
| Play | `play` | M |
| Schneller | `fast-forward` | M |
| News | `newspaper` | M |
| Warnung | `alert-triangle` | M |
| Info | `info` | S |
| Schließen | `x` | M |
| Portfolio | `briefcase` | M |
| Dashboard | `layout-dashboard` | M |
| Orders | `list-ordered` | M |
| Analytics | `bar-chart-3` | M |
| Watchlist hinzufügen | `plus` | S |
| Watchlist entfernen | `x` | S |
| Positiv/Steigend | `triangle` (gefüllt, nach oben) | S |
| Negativ/Fallend | `triangle` (gefüllt, nach unten) | S |

#### Sektor-Icons
| Sektor | Icon |
|---|---|
| Technology | `cpu` |
| Energy | `zap` |
| Financials | `landmark` |
| Healthcare | `heart-pulse` |
| Consumer Goods | `shopping-cart` |
| Industrials | `factory` |
| Materials | `gem` |
| Real Estate | `building-2` |
| Telecommunications | `radio` |
| Utilities | `plug` |
| Luxury | `diamond` |
| Transportation | `truck` |

### 2.5 Barrierefreiheit & Farbenblind-Modus

**Problem:** Grün/Rot ist die Kernunterscheidung im gesamten Spiel (Gewinn/Verlust, steigend/fallend). Etwa 8% aller Männer haben eine Rot-Grün-Farbschwäche. Ohne Alternative ist das Spiel für sie kaum spielbar.

**Lösung: Drei Farbmodi (konfigurierbar in Settings → Display):**

**Modus 1: Standard (Default)**
- Positiv: `#10B981` (Grün)
- Negativ: `#EF4444` (Rot)

**Modus 2: Deuteranopie / Protanopie (Rot-Grün-Schwäche)**
- Positiv: `#3B82F6` (Blau)
- Negativ: `#F59E0B` (Orange/Amber)
- Alle Stellen im Spiel, die Grün/Rot verwenden, werden auf Blau/Orange umgestellt
- Charts: blaue und orange Kerzen statt grüne und rote
- Zusätzlich: Muster-Unterscheidung (positive Kerzen haben einen dünnen diagonalen Strich, negative sind solid — als Backup)

**Modus 3: Tritanopie (Blau-Gelb-Schwäche, selten)**
- Positiv: `#10B981` (Grün)
- Negativ: `#EC4899` (Pink/Magenta)

**Zusätzliche Maßnahmen (in ALLEN Modi aktiv):**
- Dreiecke (▲/▼) begleiten IMMER Farben — auch ohne Farbe erkennbar
- Vorzeichen (+/-) begleiten IMMER Zahlenwerte
- Kerzen: steigende Kerzen haben einen hohlen Körper, fallende einen gefüllten (klassische Darstellungsform)
- In Tabellen: kleine Icons neben farbigen Zahlen

**Setting:** `Color Mode` — Dropdown: `Standard`, `Colorblind (Red-Green)`, `Colorblind (Blue-Yellow)`

### 2.6 Spacing- und Layout-System

**Basis-Einheit:** 4px. Alle Abstände sind Vielfache von 4px.

| Token | Wert | Verwendung |
|---|---|---|
| `space-1` | 4px | Minimaler Abstand, Icon-zu-Text innerhalb eines Labels |
| `space-2` | 8px | Abstand zwischen eng verwandten Elementen (z.B. Preis und Prozent) |
| `space-3` | 12px | Padding innerhalb kleiner Komponenten (Buttons, Badges) |
| `space-4` | 16px | Standard-Padding innerhalb von Panels und Karten |
| `space-5` | 20px | Abstand zwischen Sektionen innerhalb eines Panels |
| `space-6` | 24px | Abstand zwischen Panels |
| `space-8` | 32px | Großer Abstand, z.B. über/unter Hauptbereichen |

**Panel-Styling:**
- Eckenradius: 8px
- Border: 1px solid `border` (`#1F2937`)
- Kein Box-Shadow (zu hell für dunkles Theme). Stattdessen: subtile Border-Unterscheidung
- Padding innen: `space-4` (16px)

### 2.7 Animationen & Transitions

**Globale Timing-Werte:**
- Schnelle Transition (Hover, Farbwechsel): 150ms, `ease-out`
- Standard-Transition (Panel-Wechsel, Tabs): 200ms, `ease-in-out`
- Langsame Transition (Modal ein/aus, Overlays): 300ms, `ease-in-out`

**Zahlen-Tick-Animation:**
Wenn sich ein Preis ändert, wird die Zahl für 800ms farblich hervorgehoben:
- Preis gestiegen: Hintergrund flasht kurz `green-dim`, Text wird `green-primary`, dann zurück zu `text-primary`
- Preis gefallen: Hintergrund flasht kurz `red-dim`, Text wird `red-primary`, dann zurück zu `text-primary`
- Der neue Wert wird nicht sofort angezeigt, sondern "tickt" hoch/runter (animierter Zahlenwechsel über 300ms)

**Chart-Update-Animation:**
Neue Kerzen erscheinen ohne Animation (sofort), aber die letzte Kerze (aktuelle) wächst/schrumpft live mit dem Preis. Kein Spring- oder Bounce-Effekt — das wirkt unseriös.

**Button-Feedback:**
- Hover: Hintergrundfarbe wird 10% heller, Transition 150ms
- Active/Pressed: Hintergrundfarbe wird 5% dunkler als Hover, Transition 50ms
- Disabled: Opacity 0.4, kein Hover-Effekt, Cursor `not-allowed`

**Glow-Pulse:**
Bei wichtigen Events (Breaking News, Margin Call) pulsiert der relevante UI-Bereich 3x mit einem subtilen Glow:
- Breaking News: roter oder grüner Glow (je nach Sentiment) auf dem News-Ticker, 1s pro Puls
- Margin Call: roter Glow auf dem Portfolio-Panel, 1s pro Puls

**News-Ticker-Scroll:**
- Geschwindigkeit: 60px/Sekunde (Standard), konfigurierbar von 30-120px/s in den Settings
- Pausiert bei Hover (Spieler will lesen)
- Pausiert wenn Spielzeit pausiert ist
- Bei Speed 5x/10x: Geschwindigkeit bleibt gleich, aber neue Einträge kommen häufiger

**Modal-Ein/Ausblenden:**
- Einblenden: Fade-In (Opacity 0→1) + leichtes Scale (0.95→1.0), 300ms
- Ausblenden: Fade-Out + Scale (1.0→0.95), 200ms
- Overlay dahinter: Fade-In/Out separat, 200ms

### 2.8 Responsivität & Fenstergrößen

- **Minimale Fenstergröße:** 1280 × 720px — darunter wird das Fenster nicht verkleinert
- **Empfohlene Fenstergröße:** 1920 × 1080px — hierfür wird das Layout optimiert
- **Maximale Fenstergröße:** Unbegrenzt (Fullscreen auf beliebigem Monitor)

**Skalierungsverhalten:**
- Die **linke Sidebar** (Watchlist + Sektoren) hat eine feste Breite von 280px. Bei Fenstern unter 1400px Breite kann sie über einen Toggle-Button ein-/ausgeklappt werden.
- Die **rechte Sidebar** (Trading Panel) hat eine feste Breite von 320px. Ebenfalls ein-/ausklappbar.
- Der **zentrale Bereich** nimmt den gesamten verbleibenden Platz ein und skaliert mit.
- Die **Top Bar** hat eine feste Höhe von 48px.
- Der **News Ticker** hat eine feste Höhe von 36px.
- Alle internen Inhalte skalieren NICHT mit der Fenstergröße — nur der verfügbare Platz ändert sich. Text bleibt gleich groß.
- **UI-Skalierung** (80%, 100%, 120%, 150%) ist in den Settings konfigurierbar und skaliert alles gleichmäßig (Schrift, Spacing, Icons).

---

## 3. Haupt-UI-Layout (Main Screen)

### 3.0 Menüführung & Navigations-Architektur

Das gesamte Menüsystem von StockSim besteht aus drei Ebenen: dem Hauptmenü (Title Screen), dem In-Game-HUD mit Escape/Pause-Menü, und einem System aus modalen Dialogen. Jede Ebene ist klar getrennt.

#### 3.0.1 Navigationsfluss-Diagramm

```
APP-START
  │
  ▼
[Splash Screen (1-3s)] ──▶ [HAUPTMENÜ / TITLE SCREEN]
                              │
              ┌───────────────┼───────────────┐
              │               │               │
          [New Game]     [Continue]       [Load Game]
              │               │               │
              ▼               │               ▼
    [Erfahrungslevel]*        │         [Lade-Dialog]
    (* nur beim 1. Mal)       │               │
              │               │               ▼
              ▼               │        [Ladebildschirm]
    [Sandbox / Szenario]      │               │
              │               ▼               │
              ▼          [Ladebildschirm]      │
       [Ladebildschirm] ─────┴───────────────┘
              │
              ▼
       [IN-GAME HUD]
              │
    ┌─────────┼─────────┐
    │         │         │
[Tab-Nav] [Escape]  [Game Events]
    │         │         │
  D,P,M,  [PAUSE    [Event-Modals]
  O,N,A    MENÜ]     (priorisierte
  Tabs       │        Warteschlange)
  + Stock    │
  Detail  ┌──┼──┐
          │  │  │
       Save Load Settings
       Achievements Help
       Hauptmenü Beenden
              │
    [Daily Summary] ──▶ [Nächster Tag]
    [Bankrott]      ──▶ [Neustart/Laden/Neu]
    [Ruhestand]     ──▶ [Career Summary]
    [Szenario-Ende] ──▶ [Ergebnis-Screen]
```

#### 3.0.2 Splash Screen (App-Start)

Erscheint beim Starten der Anwendung während Electron und C#-Backend initialisieren.

- Hintergrund: `bg-primary` (`#0A0E17`), komplett dunkel
- Zentriert: "STOCKSIM" in `Inter Black`, 48px, `text-primary`, mit subtiler Glow-Animation (text-shadow pulsiert zwischen 0 und `rgba(96, 165, 250, 0.15)` über 3 Sekunden)
- Darunter: kleiner Lade-Spinner (rotierender Ring, 24px, `text-accent`)
- Dauer: 1-3 Sekunden (abhängig von System-Performance)
- Kein Fortschrittsbalken (Startup-Dauer nicht vorhersehbar)
- Kein Sound, keine Musik
- Übergang zum Hauptmenü: Fade (300ms)

#### 3.0.3 Hauptmenü (Title Screen)

Wird nach dem Splash Screen angezeigt. Vollbild, kein HUD, kein Sidebar — nur das Menü.

**Hintergrund-Animation:**
- `bg-primary` als Basis
- Darüber: eine Matrix aus scrollenden Aktien-Tickern und Preisen, gerendert in `text-disabled` (#4B5563) bei ~15% Opacity
- Die Preise ticken zufällig hoch (kurzer grüner Flash) und runter (kurzer roter Flash) in langsamem Tempo
- Scrollrichtung: langsam von rechts nach links und von unten nach oben (diagonaler Drift)
- Dies sind KEINE echten Spieldaten — rein dekorativ, mit Fake-Symbolen
- Erzeugt das Gefühl eines "lebenden Terminals"

**Logo:**
- Position: horizontal zentriert, ~30% vom oberen Rand
- Text: "STOCKSIM" in `Inter Black`, 48px, `text-primary`
- Glow-Animation: text-shadow pulsiert zwischen 0 und `rgba(96, 165, 250, 0.15)` über 3s Zyklus
- Untertitel: "Trade. Speculate. Dominate." in `Inter Regular`, 16px, `text-secondary`, 8px unter dem Logo

**Menü-Buttons:**
Vertikal gestapelt, zentriert unter dem Logo, 40px Abstand zum Untertitel.

| # | Button | Sichtbar | Aktion |
|---|---|---|---|
| 1 | `Continue` | Nur wenn Savegames existieren | Lädt den neuesten Save (manuell oder Autosave, was neuer ist) |
| 2 | `New Game` | Immer | Öffnet den New-Game-Dialog |
| 3 | `Load Game` | Immer (disabled wenn keine Saves) | Öffnet den Lade-Dialog |
| 4 | `Settings` | Immer | Öffnet den Settings-Dialog |
| 5 | `Achievements` | Immer | Öffnet die Achievement-Galerie |
| 6 | `Quit` | Immer | Beendet die Anwendung (ohne Bestätigung — kein Spiel läuft) |

**Button-Styling:**
- Breite: 300px, Höhe: 48px, Border-Radius: 8px
- Hintergrund: `bg-tertiary` (#1F2937)
- Schrift: `Inter SemiBold`, 16px, `text-primary`
- Hover: Hintergrund 10% heller, subtile Scale-Animation (1.02×, 150ms)
- Disabled: Opacity 0.4, Cursor `not-allowed`, Tooltip bei Hover: "No saves found"
- Abstand zwischen Buttons: 8px
- `Continue`-Button hat einen leichten blauen linken Rand (2px `text-accent`) um ihn hervorzuheben

**Erster Start vs. Wiederkehrender Spieler:**
- **Erster Start (keine Saves):** `Continue` fehlt, `Load Game` ist disabled. Nur `New Game`, `Settings`, `Achievements`, `Quit`.
- **Wiederkehrender Spieler:** Alle Buttons sichtbar. `Continue` ist der erste Button (oben).
- **Nach Spiel-Abschluss:** Unter dem Untertitel erscheint eine kleine Info-Zeile: "Last session: Retired with $2.4M" oder "Last scenario: Bull Run — Completed" in `text-disabled`, 12px.

**Version und System-Info:**
- Unten rechts: "v1.0" in `text-disabled`, 11px
- Unten links (Zukunft): Steam-Benutzername

**Musik:** Keine Musik auf dem Title Screen. Musik startet erst wenn ein Spiel geladen/gestartet wird.

#### 3.0.4 Escape / Pause-Menü

**Das zentrale In-Game-Menü.** Wird angezeigt wenn der Spieler Escape drückt und kein anderes Modal offen ist.

**Trigger:** Escape-Taste (wenn kein Modal, Overlay, Suchfeld oder Kontextmenü offen — siehe 3.0.8 für die vollständige Escape-Hierarchie)

**Effekt:** Das Spiel pausiert sofort. Die Simulation stoppt.

**Layout:**

```
┌────────────────────────────────────────┐
│                                        │
│           S T O C K S I M              │
│       Trade. Speculate. Dominate.      │
│                                        │
│         [    Resume Game    ]           │
│         [    Quick Save     ]           │
│         [    Save Game      ]           │
│         [    Load Game      ]           │
│         [    Settings       ]           │
│         [    Achievements   ]           │
│         [    Help / Glossary]           │
│         [    Main Menu      ]           │
│         [    Quit to Desktop]           │
│                                        │
│     Tue, Mar 15 2027 — 2:32 PM         │
│     Portfolio: $62,456 (+24.9%)        │
│     Play Time: 4h 32m                  │
│                                        │
└────────────────────────────────────────┘
```

- Modal: 400 × 520px, zentriert
- Hintergrund: `bg-modal` (#111827), dahinter `bg-overlay`
- Die Spielwelt bleibt sichtbar, aber gedimmt (Charts, Zahlen als eingefrorener Snapshot)
- Logo: "STOCKSIM" in `Inter Bold`, 24px, `text-primary`. Untertitel in 12px, `text-secondary`
- Buttons: identischer Stil wie Title-Screen-Buttons, aber 280px breit, 44px hoch
- Game-Info unten: aktuelles Spieldatum, Portfolio-Wert (farbig), echte Spielzeit — alles in `text-disabled`, 12px

**Button-Aktionen:**

| Button | Aktion |
|---|---|
| Resume Game | Schließt Menü, setzt Spiel fort. Auch: Escape erneut drücken. |
| Quick Save | Speichert sofort in den "Quick Save"-Slot (überschreibt). Button-Text flasht kurz "Saved!" in `green-primary` (1s). Menü bleibt offen. |
| Save Game | Öffnet den Speichern-Dialog (als nested Modal über dem Pause-Menü). |
| Load Game | Zeigt Warnung bei ungespeichertem Fortschritt, dann Lade-Dialog. |
| Settings | Öffnet Settings-Modal über dem Pause-Menü. |
| Achievements | Öffnet Achievement-Galerie als Modal. |
| Help / Glossary | Öffnet Hilfe-Modal. |
| Main Menu | Bestätigungsdialog: "Return to Main Menu?" — `[Cancel]`, `[Save & Return]`, `[Return Without Saving]` |
| Quit to Desktop | Bestätigungsdialog: "Quit to Desktop?" — `[Cancel]`, `[Save & Quit]`, `[Quit Without Saving]` |

**`Retire`-Button (bedingt):**
Erscheint zwischen `Achievements` und `Main Menu`, nur wenn Portfolio-Wert ≥ $1,000,000. Text: `Retire` mit einem kleinen `🏆`-Badge. Klick öffnet den Career Summary Screen.

#### 3.0.5 Ladebildschirme

**Markt-Generierung (New Game):**

```
┌──────────────────────────────────────────────────┐
│                                                  │
│                S T O C K S I M                   │
│                                                  │
│           Generating market...                   │
│           [═══════════►          ] 45%            │
│                                                  │
│  Creating 500 companies across 12 sectors...     │
│                                                  │
│  💡 Press Space to pause the simulation at       │
│     any time.                                    │
│                                                  │
└──────────────────────────────────────────────────┘
```

- Hintergrund: `bg-primary`, keine Animation (clean, schnell rendernd)
- Fortschrittsbalken: 400px breit, 4px hoch, Track `bg-tertiary`, Füllung `text-accent`, animiert
- Status-Text unter dem Balken beschreibt den aktuellen Schritt:
  1. "Creating companies..." (0-20%)
  2. "Generating price histories..." (20-50%)
  3. "Initializing AI traders..." (50-70%)
  4. "Scheduling events..." (70-85%)
  5. "Preparing your trading desk..." (85-100%)
- Tipp-Text: rotiert alle 3 Sekunden. Pool von 15+ Tipps:
  - `Press Space to pause the simulation at any time.`
  - `Watch the news ticker for market-moving events.`
  - `Short selling profits from falling prices — but losses are unlimited.`
  - `Use Ctrl+F to quickly search for any stock.`
  - `The Daily Summary shows after-hours events. Don't skip it.`
  - `Set Price Alerts to get notified when a stock hits your target.`
  - `Limit Orders let you buy at a price you choose. Patience pays.`
  - `Check Analytics to see if you're beating the market.`
  - `Green means profit. Red means... opportunity.`
  - `Diversification reduces risk. Don't put all eggs in one basket.`
  - `The SMA is watching. Trade smart, not shady.`
  - `Short Squeezes can happen when short interest exceeds 30% of float.`
  - `Use Stop Losses to protect your positions while you're away.`
  - `Earnings reports are the single biggest price mover. Check the calendar.`
  - `Extended hours trading is available before and after regular market hours.`

**Savegame laden:**
- Gleicher Bildschirm, aber Titel "Loading save..."
- Unter dem Balken: Save-Metadata (Name, Spieldatum, Portfolio-Wert)
- Keine Tipps (Laden ist schnell genug)

#### 3.0.6 Achievement-Galerie

Zugänglich über: Hauptmenü → Achievements, Pause-Menü → Achievements.

**Layout:**
Modal: 800 × 600px, zentriert.

```
┌──────────────────────────────────────────────────────────────┐
│  ACHIEVEMENTS                                   12 / 31     │
│──────────────────────────────────────────────────────────────│
│  [ All ] [ Wealth ] [ Trading ] [ Market ] [ Risk ]         │
│──────────────────────────────────────────────────────────────│
│                                                              │
│  WEALTH                                          4/7         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ 🏃            │  │ 💯            │  │ 🔒 LOCKED    │      │
│  │ First Steps   │  │ Six Figures   │  │ Quarter M.   │      │
│  │ ✓ Unlocked    │  │ ✓ Unlocked    │  │ ???          │      │
│  │ Mar 5, 2027   │  │ Mar 12, 2027  │  │              │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│                                                              │
│  TRADING SKILLS                                  3/8         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ ...           │  │ ...           │  │ ...           │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│  ...                                                         │
└──────────────────────────────────────────────────────────────┘
```

- Header: "ACHIEVEMENTS" in `Inter Bold`, 20px. Rechts: "12 / 31" in `text-secondary`
- Kategorie-Tabs: horizontale Toggle-Buttons (Filter)
- Achievement-Karten: Grid, 3 pro Zeile, je ~240 × 100px
  - **Freigeschaltet:** `bg-secondary`, Gold-Rand (`#D4AF37`), Icon farbig, Name `Inter SemiBold` 14px, Datum `text-disabled` 11px. Hover: subtiler Gold-Glow.
  - **Gesperrt:** `bg-tertiary`, Icon grau, Opacity 0.6, Bedingung als "???" in `text-disabled`
  - **Fortschritt:** kleiner Fortschrittsbalken unter dem Namen ("30 / 50")
- Scrollbar bei vielen Achievements

#### 3.0.7 Career Summary / Ruhestand

Erscheint bei: Klick auf "Retire" im Pause-Menü (ab $1M), oder bei Szenario-Abschluss/-Scheitern.

**Layout:**
Großes Modal: 900 × 700px (Feier-Moment, nicht eng).

```
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│                     CAREER SUMMARY                           │
│              You retired from Wall Street.                   │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐    │
│  │  FINAL PORTFOLIO VALUE           $2,456,789.00       │    │
│  │  Total Return              +$2,406,789 (+4,813%)     │    │
│  │  Starting Capital                    $50,000         │    │
│  │  Play Time                          12h 45m          │    │
│  │  Game Time                       2y 3m 14d           │    │
│  └──────────────────────────────────────────────────────┘    │
│                                                              │
│  TRADING STATS                 MARKET STATS                  │
│  Total Trades: 847             Bull Markets: 3               │
│  Win Rate: 68.2%               Bear Markets: 2              │
│  Best Trade: +$45k (NVPH)      Crashes Survived: 1          │
│  Worst Trade: -$12k (STCG)     Events Witnessed: 234        │
│  Profit Factor: 3.12                                        │
│  Avg Hold Time: 4.2 days                                    │
│                                                              │
│  ACHIEVEMENTS EARNED: 18 / 31                                │
│  [🏃] [💯] [📈] [💰] [🏆] ...                              │
│                                                              │
│  [Portfolio Performance Chart — Miniatur-Linienchart]        │
│                                                              │
│  [New Game]    [Main Menu]    [Keep Playing]                  │
└──────────────────────────────────────────────────────────────┘
```

- Titel: `Inter Bold`, 28px, `text-primary`
- Portfolio-Wert: `JetBrains Mono Bold`, 36px, `green-primary`
- Subtile Animation: langsames "Konfetti" aus kleinen grünen $-Zeichen und ▲-Pfeilen, ~10% Opacity, schwebt nach oben
- Performance-Chart: vereinfachter Area-Chart, 400 × 80px
- `Keep Playing`: schließt den Screen, Spieler kann weitermachen (Retirement ist nur ein Meilenstein)

**Szenario-Variante:**
- Erfolg: "SCENARIO COMPLETE" Titel, Ziel vs. Ergebnis, Zeitvergleich, Persönliche Bestleistung
- Scheitern: "SCENARIO FAILED" Titel, kein Konfetti, roter Gradient oben, "What went wrong?" Analyse
- Buttons: `[Retry Scenario]`, `[Choose Another]`, `[Main Menu]`

**Bankrott-Variante:**
Separater Screen (kein Career Summary, sondern Bankrott-Modal, siehe 3.0.9).

#### 3.0.8 Escape-Taste: Vollständige Verhaltens-Hierarchie

Die Escape-Taste hat je nach Zustand eine andere Funktion. Die Hierarchie wird von oben nach unten geprüft — der ERSTE Treffer wird ausgeführt:

```
Escape gedrückt → Prüfe von oben nach unten:

1. Eingabefeld ist fokussiert
   → Fokus entfernen (Blur). Nichts weiter.

2. Such-Overlay ist offen (Ctrl+F / globale Suche)
   → Such-Overlay schließen.

3. Kontextmenü ist offen
   → Kontextmenü schließen.

4. Bestätigungsdialog ist offen (z.B. "Save löschen?" über Lade-Dialog)
   → Bestätigungsdialog schließen (= Cancel). Eltern-Modal bleibt.

5. Game-Event-Modal ist offen:
   → Breaking News / Short Squeeze Warning: Dismiss
   → Daily Summary: KEIN EFFEKT (Spieler muss "Continue" klicken)
   → Margin Call: KEIN EFFEKT (Spieler muss reagieren)
   → Bankrott: KEIN EFFEKT (Spieler muss Option wählen)
   → Tender Offer: Decline
   → Circuit Breaker: KEIN EFFEKT (warten bis Handel fortgesetzt)

6. Spieler-Modal ist offen (Settings, Save, Load, Help, Achievements)
   → Modal schließen. Falls über Pause-Menü geöffnet: zurück zum Pause-Menü.

7. Aktien-Detail-Ansicht ist offen
   → Zurück zur vorherigen Tab-Ansicht (← Back).

8. Tutorial-Overlay ist aktiv
   → Kein Effekt (Tutorial hat eigenen Skip-Button).

9. Pause-Menü ist offen
   → Pause-Menü schließen, Spiel fortsetzen.

10. Nichts ist offen (normaler Spielzustand)
    → Pause-Menü öffnen (Spiel pausiert).
```

#### 3.0.9 Bankrott-Screen

Erscheint wenn Cash ≤ $0 UND Gesamtportfolio-Wert ≤ $0. Spiel pausiert automatisch.

**Layout:**
Modal: 500 × 400px. Roter oberer Rand (4px `red-primary`).

```
┌────────────────────────────────────────────┐
│  ═══ BANKRUPTCY ═══                        │
│                                            │
│  Your portfolio has been wiped out.        │
│                                            │
│  Total Losses: -$50,000.00                │
│  Final Cash: -$2,340.00                   │
│  Game Time Survived: 47 days              │
│                                            │
│  What happened:                            │
│  Short squeeze in NVPH forced liquidation  │
│  at $234.50, resulting in -$38,000 loss.  │
│                                            │
│  [  Restart with $X  ]                     │
│  [  Load Save             ]                │
│  [  New Game              ]                │
│  [  Main Menu             ]                │
└────────────────────────────────────────────┘
```

- "BANKRUPTCY": `Inter Bold`, 24px, `red-primary`
- Verlust: `JetBrains Mono Bold`, 20px, `red-primary`
- "What happened": automatisch generierte Erklärung des größten Verlust-Events, `Inter` 13px, `text-secondary`
- `$X` im Button-Text wird dynamisch ersetzt (Betrag abhängig von Difficulty: Easy $25,000 / Normal $10,000 / Hard $5,000)
- Restart: Markt läuft weiter, nur Spielerkonto wird zurückgesetzt

#### 3.0.10 Vollständiger Katalog aller Bestätigungsdialoge

| Auslöser | Dialog-Text | Buttons |
|---|---|---|
| Order platzieren (wenn Bestätigung aktiv) | Vollständige Order-Details + Warnung | `[Cancel]` + `[Confirm]` |
| Order stornieren | "Cancel this order?" + Order-Details | `[Keep]` + `[Cancel Order]` |
| Save löschen | "Delete save '[Name]'? Cannot be undone." | `[Cancel]` + `[Delete]` (rot) |
| ALLE Saves löschen | "Delete ALL saves?" + zweite Bestätigung: "Type DELETE" | `[Cancel]` + Eingabe + `[Delete All]` (rot) |
| Save überschreiben | "Overwrite save '[Name]'?" | `[Cancel]` + `[Overwrite]` |
| Settings-Sektion zurücksetzen | "Reset [Section] to defaults?" | `[Cancel]` + `[Reset]` |
| ALLE Settings zurücksetzen | "Reset all settings to defaults?" | `[Cancel]` + `[Reset All]` |
| Neues Spiel (ungespeichert) | "Unsaved progress. Save first?" | `[Save]` + `[Don't Save]` + `[Cancel]` |
| Spiel laden (ungespeichert) | "Unsaved progress will be lost." | `[Cancel]` + `[Load]` |
| Hauptmenü (aus Pause) | "Return to Main Menu?" | `[Cancel]` + `[Save & Return]` + `[Return Without Saving]` |
| Beenden (aus Pause) | "Quit to Desktop?" | `[Cancel]` + `[Save & Quit]` + `[Quit Without Saving]` |
| Watchlist löschen | "Delete watchlist '[Name]'?" | `[Cancel]` + `[Delete]` |
| Aktie aus Watchlist entfernen | KEINE Bestätigung — sofort, mit Undo-Toast (5s): "Removed AAPL. [Undo]" | — |
| Ruhestand | "Retire? You can keep playing after viewing your Career Summary." | `[Cancel]` + `[Retire]` |
| Tender Offer akzeptieren | "Accept offer for [QTY] shares at $[PRICE]? Irreversible." | `[Decline]` + `[Accept]` |
| Alles verkaufen / covern | "Sell all [QTY] shares of [SYMBOL] at market price?" | `[Cancel]` + `[Sell All]` |

**Bestätigungsdialog-Styling:**
- 350-400px breit, zentriert, `bg-modal`, Border-Radius: 12px
- Warnungs-Icon (⚠ amber Dreieck) bei destruktiven Aktionen
- Destruktiver Button in `red-primary`, Cancel in neutral
- Enter bestätigt, Escape cancelt

#### 3.0.11 Szenario-Auswahl

Zugänglich über: New Game → Tab "Scenarios".

**Layout im New-Game-Dialog:**

```
┌──────────────────────────────────────────────────────────────┐
│  NEW GAME                                                    │
│  [  Sandbox  ]  [  Scenarios  ]                              │
│──────────────────────────────────────────────────────────────│
│                                                              │
│  Filter: [ All ] [ Easy ] [ Medium ] [ Hard ]                │
│                                                              │
│  ┌──────────────────────────┐  ┌──────────────────────────┐  │
│  │ THE CRASH        [HARD]  │  │ BULL RUN      [MEDIUM]   │  │
│  │                          │  │                          │  │
│  │ Survive a -40% crash     │  │ Turn $25k into $100k    │  │
│  │ with $50k.               │  │ in a bull market.       │  │
│  │                          │  │                          │  │
│  │ ⏱ 6 months               │  │ ⏱ 3 months              │  │
│  │ 🎯 Stay positive          │  │ 🎯 Reach $100k          │  │
│  │                          │  │                          │  │
│  │ Best: $68,420 (Mar '26)  │  │ Best: 14 days           │  │
│  └──────────────────────────┘  └──────────────────────────┘  │
│  ... (weitere Karten, scrollbar)                             │
│                                                              │
│  [          START SCENARIO          ]                         │
└──────────────────────────────────────────────────────────────┘
```

- Szenario-Karten: Grid, 2 pro Zeile, je ~360 × 180px
- Titel: `Inter Bold`, 16px
- Difficulty-Badge: farbig (Grün Easy, Amber Medium, Rot Hard)
- Beschreibung: `Inter`, 13px, `text-secondary`
- Zeitlimit und Ziel: `JetBrains Mono`, 12px
- Persönliche Bestleistung: `text-accent`, 12px (oder "Not attempted" in `text-disabled`)
- Ausgewählte Karte: blauer Rand (`border-focus`)
- Start-Button: volle Breite, `green-primary`, disabled bis eine Karte gewählt ist

**Szenario-Kontext im Spiel:**
Während eines Szenarios erscheint in der Top Bar ein persistenter Badge neben der Spielzeit:
`[SCENARIO: The Crash | Goal: Stay Positive | Day 47/180]`
- Klein, `text-disabled`, 10px, mit Mini-Fortschrittsbalken
- Klick: Popup mit Details zum Szenario-Fortschritt

#### 3.0.12 Tastatur-Navigation

**Tab-Navigation:**
Alle interaktiven Elemente sind via Tab-Taste erreichbar. Reihenfolge folgt dem visuellen Layout: Top Bar → Linke Sidebar → Zentralbereich → Rechte Sidebar.

**Focus-Ring:**
- 2px Outline, `border-focus` (#3B82F6), Offset 2px
- Nur sichtbar bei Tastatur-Navigation (`:focus-visible`), nicht bei Maus-Klick

**Focus-Trapping:**
Wenn ein Modal offen ist, bleibt der Tab-Fokus innerhalb des Modals gefangen. Der Spieler kann nicht zu Elementen hinter dem Overlay tabben. Beim Schließen springt der Fokus zurück zum auslösenden Element.

**Skip-Navigation:**
Erstes fokussierbares Element auf der Seite: versteckter "Skip to main content" Link. Tab einmal → Link erscheint → Enter → Fokus springt zum Zentralbereich.

#### 3.0.13 Breadcrumbs / Navigationskontext

**Aktiver Tab:** Der aktive Tab in der Top Bar hat blauen Text + 2px Unterstreichung — immer sichtbar.

**Aktien-Detail-Breadcrumb:**
Wenn eine Aktien-Detail-Ansicht offen ist, erscheint ein Breadcrumb oben im Zentralbereich:
```
Dashboard > AAPL — Apple Inc.
```
- Erster Teil (Tab-Name): klickbar (`text-accent`, Hover-Unterstreichung), kehrt zum Tab zurück
- `>` Separator: `text-disabled`
- Aktienname: `text-primary` (aktuell, nicht klickbar)

**Szenario-Badge:** Siehe 3.0.11.

### 3.0.14 Multi-Window-System (Detachable Panels)

StockSim unterstützt mehrere Fenster, die auf verschiedene Monitore verteilt werden können. Wie bei einem echten Bloomberg-Terminal kann der Spieler Panels aus dem Hauptfenster auskoppeln und frei positionieren.

#### Auskoppelbare Panels

| Panel | Auskoppelbar | Standard-Größe (detached) | Inhalt im eigenen Fenster |
|---|---|---|---|
| **Chart** | Ja (mehrfach!) | 800 × 600px | Vollständiger Chart mit Toolbar, Indikatoren, Zeitrahmen. Jedes Fenster zeigt eine andere Aktie. |
| **News Feed** | Ja | 500 × 700px | Kompletter News-Feed mit Filtern, Upcoming Events Sidebar |
| **Portfolio** | Ja | 700 × 500px | Positionen-Tabelle, P&L, Allokation |
| **Orderbook** | Ja | 400 × 500px | Bid/Ask-Tabelle + Depth-Visualisierung für eine Aktie |
| **Time & Sales** | Ja | 350 × 500px | Scrollende Trade-Liste für eine Aktie |
| **Watchlist** | Ja | 300 × 600px | Watchlist mit Sparklines |
| **Analytics** | Ja | 700 × 500px | Performance-Charts, Risiko-Metriken |
| **Trading Journal** | Ja | 500 × 400px | Notizen-Liste |
| **News Ticker** | Nein | — | Bleibt immer im Hauptfenster |
| **Top Bar** | Nein | — | Bleibt immer im Hauptfenster |
| **Order Panel** | Nein | — | Bleibt immer im Hauptfenster (Trading muss zentral sein) |
| **Pause Menu** | Nein | — | Nur im Hauptfenster |

#### Auskoppeln — UI-Interaktion

**Methode 1: Drag & Drop**
- Jedes auskoppelbare Panel hat ein kleines `external-link`-Icon (16px, `text-disabled`) in der oberen rechten Ecke des Panel-Headers
- Der Spieler kann den Panel-Header greifen und aus dem Hauptfenster HERAUSZIEHEN
- Wenn der Drag über den Rand des Hauptfensters hinausgeht: Panel löst sich und wird ein eigenes Fenster
- Animation: das Panel "reißt" sich los (kurzer Scale-Down auf 0.9, dann normaler Fenster-Aufbau)

**Methode 2: Button-Klick**
- Klick auf das `external-link`-Icon: Panel wird sofort als neues Fenster geöffnet
- Das Panel im Hauptfenster wird durch einen Platzhalter ersetzt: `[Panel Name] — Detached ↗` (klickbar, bringt Fokus auf das detached Fenster)
- Oder: der frei gewordene Platz wird vom Zentralbereich eingenommen (mehr Platz für den Chart)

**Methode 3: Rechtsklick**
- Rechtsklick auf Panel-Header → Kontextmenü: `Detach to Window`, `Detach to New Monitor` (positioniert auf dem zweiten Monitor wenn vorhanden)

#### Zurückkoppeln (Re-Dock)

- Im detached Fenster: Schließen-Button (`×`) schließt das Fenster UND koppelt das Panel zurück ins Hauptfenster
- Alternativ: Drag des detached Fensters zurück über das Hauptfenster → Panel dockt wieder an
- Tastenkürzel: `Ctrl+W` schließt das aktive detached Fenster und dockt es zurück

#### Mehrere Charts gleichzeitig

Die Killer-Funktion: der Spieler kann MEHRERE Chart-Fenster öffnen, jedes für eine andere Aktie.

**Öffnen:**
- Rechtsklick auf Aktie in Watchlist/Tabelle → `Open Chart in New Window`
- Oder: im Chart-Fenster: `Ctrl+N` → neues Chart-Fenster, Spieler wählt Aktie
- Maximum: 8 gleichzeitige Chart-Fenster (Performance-Limit)

**Synchronisation:**
- Alle Fenster empfangen Live-Preis-Updates über den gleichen WebSocket
- Zeitsteuerung (Pause/Speed) gilt GLOBAL — alle Fenster pausieren/beschleunigen gleichzeitig
- Wenn der Spieler in einem Chart-Fenster eine Aktie anklickt: das Order-Panel im Hauptfenster wechselt zu dieser Aktie (automatische Synchronisation)

**Layout-Beispiel (2 Monitore):**

```
MONITOR 1 (Hauptfenster)              MONITOR 2 (Detached Panels)
┌─────────────────────────────┐      ┌──────────────┬──────────────┐
│ [Top Bar + Speed + Cash]    │      │ AAPL Chart   │ TSLA Chart   │
│                             │      │ (Candlestick)│ (Candlestick)│
│ [Watch] [Market Tab] [Trade]│      │              │              │
│         Aktien-Tabelle      │      │ 15m Zeitrah. │ 1D Zeitrah.  │
│         Screener            │      │              │              │
│         Heatmap             │      ├──────────────┼──────────────┤
│                             │      │ Portfolio    │ News Feed    │
│ [News Ticker]               │      │ Positionen   │ Live-Events  │
└─────────────────────────────┘      │ P&L          │ Filter       │
                                     └──────────────┴──────────────┘
```

#### Detached-Fenster-Styling

- Jedes detached Fenster hat denselben Dark-Theme-Hintergrund (`bg-primary`)
- Eigene Titelleiste: `STOCKSIM — [Panel Name]` (z.B. `STOCKSIM — AAPL Chart`)
- Gleiche Schriften, Farben, Animationen wie im Hauptfenster
- Minimierbar, maximierbar, frei skalierbar
- Fensterposition und -größe werden gespeichert und beim nächsten Start wiederhergestellt

#### Layout-Speicherung

**Fenster-Layout wird automatisch gespeichert:**
- Welche Panels sind detached
- Position und Größe jedes Fensters
- Welche Aktie ist in welchem Chart-Fenster
- Auf welchem Monitor ist welches Fenster

**Gespeichert in:** `%AppData%/StockSim/window-layout.json` (global, nicht pro Savegame — das Layout ist eine Benutzer-Präferenz)

**Layout-Reset:** Settings → Advanced → `Reset Window Layout` — alle Fenster zurück ins Hauptfenster

#### Technische Implementierung

**Electron Multi-Window:**
- Hauptfenster: `BrowserWindow` (main)
- Detached Panels: je ein eigener `BrowserWindow` (child, Referenz auf main)
- Kommunikation zwischen Fenstern: Electron IPC (`ipcMain` / `ipcRenderer`)
- WebSocket-Daten: nur das Hauptfenster hat die WebSocket-Verbindung. Es leitet relevante Updates per IPC an die detached Fenster weiter.

**Performance:**
- Jedes detached Fenster ist ein eigener Renderer-Prozess → eigenes RAM-Budget (~50-100 MB pro Fenster)
- Maximum 8 detached Fenster (bei mehr: Warnung 'Maximum number of windows reached')
- Chart-Fenster sind am teuersten (TradingView Library pro Fenster)
- Bei Performance-Problemen: Settings → Advanced → `Disable Multi-Window` deaktiviert die Funktion komplett

**Graceful Degradation:**
- Wenn der Spieler nur einen Monitor hat: Funktion ist trotzdem nutzbar (Fenster überlagern sich)
- Auf schwachen PCs: `Disable Multi-Window` in Settings, alles bleibt im Hauptfenster
- Wenn ein detached Fenster auf einem Monitor ist der abgesteckt wird: Fenster springt auf den Hauptmonitor zurück

### 3.1 Gesamtlayout-Architektur

Das Hauptfenster ist in fünf feste Zonen unterteilt. Es gibt keine frei verschiebbaren Fenster — die Anordnung ist fix, um Konsistenz und Wiedererkennung zu gewährleisten.

```
┌──────────────────────────────────────────────────────────────┐
│                        TOP BAR (48px)                        │
│  [Logo] [Dashboard|Portfolio|Market|Orders|News|Analytics]   │
│         [Markt-Ticker]          [Datum/Zeit] [Speed] [$Cash] │
├────────────┬─────────────────────────────┬───────────────────┤
│            │                             │                   │
│  LINKE     │      ZENTRALER BEREICH      │   RECHTE          │
│  SIDEBAR   │                             │   SIDEBAR         │
│  (280px)   │    (flexibel, füllt Rest)   │   (320px)         │
│            │                             │                   │
│ ┌────────┐ │  Inhalt wechselt je nach    │ ┌─────────────┐  │
│ │Watchlst│ │  aktivem Tab:               │ │ Ausgewählte │  │
│ │        │ │                             │ │ Aktie       │  │
│ │AAPL  ▲ │ │  Dashboard: Heatmap,        │ │ AAPL        │  │
│ │MSFT  ▼ │ │    Top Movers, Index-Chart  │ │ $142.58     │  │
│ │TSLA  ▲ │ │                             │ │ +2.34%      │  │
│ │...     │ │  Portfolio: Positionen,      │ │             │  │
│ │        │ │    P&L, Chart               │ │ ┌─────────┐ │  │
│ └────────┘ │                             │ │ │ ORDER   │ │  │
│            │  Market: Aktientabelle       │ │ │ PANEL   │ │  │
│ ┌────────┐ │                             │ │ │         │ │  │
│ │Sectors │ │  Orders: Offene/Historie    │ │ │Buy|Sell │ │  │
│ │        │ │                             │ │ │Short    │ │  │
│ │Tech ▲  │ │  News: News-Feed           │ │ │         │ │  │
│ │Enrg ▼  │ │                             │ │ │Qty [ ] │ │  │
│ │Fin  ▲  │ │  Analytics: Performance-    │ │ │Price[ ]│ │  │
│ │...     │ │    Charts                   │ │ │         │ │  │
│ └────────┘ │                             │ │ │[PLACE  ]│ │  │
│            │                             │ │ │[ORDER  ]│ │  │
│            │                             │ │ └─────────┘ │  │
│            │                             │ │             │  │
│            │                             │ │ ┌─────────┐ │  │
│            │                             │ │ │Position │ │  │
│            │                             │ │ │Quick    │ │  │
│            │                             │ │ │View     │ │  │
│            │                             │ │ └─────────┘ │  │
├────────────┴─────────────────────────────┴───────────────────┤
│                     NEWS TICKER (36px)                        │
│  [14:32] AAPL — Apple beats earnings ▲ | [14:28] OIL — ...   │
└──────────────────────────────────────────────────────────────┘
```

**Pixel-Aufteilung bei 1920×1080:**
- Top Bar: 1920 × 48px (oben, volle Breite)
- Linke Sidebar: 280 × 996px (links, zwischen Top Bar und Ticker)
- Zentraler Bereich: 1320 × 996px (Mitte, flexibel)
- Rechte Sidebar: 320 × 996px (rechts)
- News Ticker: 1920 × 36px (unten, volle Breite)
- Verbleibende Höhe für Inhalt: 1080 - 48 - 36 = 996px

### 3.2 Top Bar

Die Top Bar ist ein durchgängiger, dunkler Streifen am oberen Fensterrand. Hintergrund: `bg-secondary` (`#111827`). Untere Border: 1px solid `border` (`#1F2937`). Höhe: 48px. Innen vertikal zentriert.

#### 3.2.1 Linker Bereich: Logo & Navigation (Links, ca. 500px)

**Logo:**
- Position: ganz links, Padding-Left 16px
- Darstellung: Text "STOCKSIM" in `Inter Bold`, 18px, Farbe `text-primary`
- Kein Bild-Logo im MVP — nur Typografie
- Rechts vom Logo: 24px Abstand, dann vertikale Trennlinie (1px, `border`, Höhe 24px)

**Navigations-Tabs:**
Direkt rechts neben der Trennlinie, 16px Abstand. Sechs Tabs nebeneinander als Text-Buttons:

| Tab | Label | Shortcut |
|---|---|---|
| 1 | `Dashboard` | D |
| 2 | `Portfolio` | P |
| 3 | `Market` | M |
| 4 | `Orders` | O |
| 5 | `News` | N |
| 6 | `Analytics` | A |

**Tab-Styling:**
- Schrift: `Inter Medium`, 14px
- Inaktiv: Farbe `text-secondary` (`#9CA3AF`), kein Hintergrund
- Hover: Farbe `text-primary` (`#F9FAFB`), Hintergrund `bg-tertiary` (`#1F2937`), Border-Radius 6px
- Aktiv: Farbe `text-accent` (`#60A5FA`), untere Border 2px solid `text-accent`, kein Hintergrund
- Padding pro Tab: 8px horizontal, 12px vertikal
- Abstand zwischen Tabs: 4px
- Transition bei Hover/Aktiv: 150ms

**Tab-Wechsel-Verhalten:**
Kein Seitenübergang, kein Fade. Der zentrale Bereich wechselt seinen Inhalt sofort. Kein Flackern — der neue Inhalt ist bereits im DOM, nur ausgeblendet (CSS `display: none` wird zu `display: block`).

#### 3.2.2 Mittlerer Bereich: Markt-Zusammenfassung (Mitte, flexibel)

Ein horizontaler Lauftext, der die wichtigsten Indizes und Top-Mover zeigt. Zentriert in der verbleibenden Breite.

**Format eines Eintrags:**
`INDEX-NAME ▲ +1.24%` oder `INDEX-NAME ▼ -0.87%`

**Angezeigte Daten (in Reihenfolge):**
1. Gesamtmarkt-Index (z.B. `MARKET ▲ +0.42%`)
2. Top 3 Sektor-Indizes nach absoluter Tagesperformance
3. Top 2 Einzelaktien-Mover (größte %-Veränderung)

**Styling:**
- Schrift: `JetBrains Mono`, 12px
- Trennzeichen zwischen Einträgen: `  |  ` (Pipe mit Spaces), Farbe `text-disabled`
- Positive Werte: `green-primary`
- Negative Werte: `red-primary`
- Dreieck-Symbol: ausgefülltes Dreieck, gleiche Farbe wie Zahl
- Kein Scrollen — die Daten passen in eine Zeile. Bei Platzmangel werden weniger Einträge gezeigt (erst die Einzelaktien, dann Sektoren weglassen)

**Aktualisierungsfrequenz:** Bei jedem Preis-Tick (Echtzeit).

#### 3.2.3 Rechter Bereich: Spielzeit, Speed & Cash (Rechts, ca. 450px)

Von links nach rechts:

**Spielzeit-Anzeige:**
- Format: `Mon, Mar 15 2027 — 2:32 PM`
- Schrift: `JetBrains Mono`, 13px, Farbe `text-primary`
- Datum und Uhrzeit ticken in Echtzeit mit (synchron zur Simulation)
- Zusatz-Indikator direkt darunter oder daneben: `Market Open` (in `green-primary`, 10px) oder `Market Closed` (in `red-primary`, 10px) oder `Weekend` (in `text-disabled`, 10px)

**Speed-Control (rechts neben Spielzeit, 16px Abstand):**
Fünf Buttons in einer Button-Group (zusammenhängendes Element mit gemeinsamem Border):

| Button | Label | Bedeutung |
|---|---|---|
| 1 | `▐▐` (Pause-Symbol) | Pause |
| 2 | `▶` | 1x Geschwindigkeit |
| 3 | `▶▶` | 2x Geschwindigkeit |
| 4 | `▶▶▶` | 5x Geschwindigkeit |
| 5 | `▶▶▶▶` | 10x Geschwindigkeit |

**Speed-Button-Styling:**
- Button-Group-Hintergrund: `bg-tertiary`
- Einzelner Button: 32 × 32px, kein individueller Border
- Inaktiv: Icon-Farbe `text-secondary`
- Hover: Hintergrund wird leicht heller
- Aktiv (aktuell gewählte Geschwindigkeit): Hintergrund `bg-primary`, Icon-Farbe `text-accent`, subtile Border-Markierung
- Bei Pause aktiv: Pause-Button bekommt zusätzlich einen pulsierenden Glow-Ring in `text-accent` (langsam, 2s Zyklus), damit der Spieler nicht vergisst, dass das Spiel pausiert ist

**Pause-Overlay-Indikator:**
Wenn pausiert, erscheint zusätzlich ein semi-transparenter Text `PAUSED` in der oberen Mitte des zentralen Bereichs. Schrift: `Inter Bold`, 48px, Farbe `rgba(249, 250, 251, 0.15)`. Der Text liegt über dem Inhalt, blockiert aber keine Interaktion (pointer-events: none).

**Cash-Anzeige (rechts neben Speed, 16px Abstand):**
- Label: `Cash` in `text-secondary`, 10px, darüber
- Betrag: `$XX,XXX.XX` in `JetBrains Mono`, 16px, `text-primary`
- Wenn Cash knapp wird (unter 10% des Startkapitals): Farbe wechselt zu `warning`
- Padding-Right: 16px (Abstand zum Fensterrand)

**Settings-Icon (ganz rechts, vor Padding):**
- Icon: `settings` (Zahnrad), 20px, Farbe `text-secondary`
- Hover: Farbe `text-primary`, leichte Rotation (15°, 300ms)
- Klick: öffnet Settings-Modal (siehe Kapitel 16)

**Save-Icon (links neben Settings, 8px Abstand):**
- Icon: `save`, 20px, Farbe `text-secondary`
- Hover: Farbe `text-primary`
- Klick: sofortiges Speichern, kurze Bestätigung (Toast "Game saved" für 2s)
- Nach dem Speichern: Icon flasht kurz `green-primary` (500ms)

### 3.3 Linke Sidebar: Watchlist & Sektor-Navigation

Hintergrund: `bg-secondary`. Rechte Border: 1px solid `border`. Breite: 280px. Vertikal scrollbar wenn Inhalt zu lang.

Die Sidebar enthält zwei Panels übereinander:
1. **Watchlist** (obere 60% der Sidebar-Höhe, min 300px)
2. **Sectors** (untere 40%, min 200px)

Zwischen den Panels: ein draggbarer Resize-Handle (4px Höhe, `border`-Farbe, Cursor `row-resize`). Der Spieler kann die Aufteilung zwischen Watchlist und Sectors anpassen.

#### 3.3.1 Watchlist-Panel

**Panel-Header:**
- Titel: `Watchlist` in `Inter SemiBold`, 14px, `text-primary`
- Rechts daneben: Zahl in Klammern `(12)` (Anzahl der Einträge), `text-secondary`, 12px
- Ganz rechts: Plus-Icon (`plus`, 16px, `text-secondary`, Hover: `text-primary`)
- Klick auf Plus: öffnet die globale Aktiensuche (Overlay), Ergebnis wird zur Watchlist hinzugefügt
- Padding: 12px horizontal, 8px vertikal

**Suchfeld:**
- Direkt unter dem Header
- Volle Breite minus 24px Padding (links/rechts je 12px)
- Höhe: 32px
- Hintergrund: `bg-input`
- Border: 1px solid `border`, bei Fokus `border-focus`
- Border-Radius: 6px
- Placeholder-Text: `Search stocks...` in `text-disabled`, 12px
- Such-Icon (`search`, 14px) links im Feld, 8px Innen-Padding
- Die Suche filtert die Watchlist-Einträge in Echtzeit während der Eingabe. Kein separater Such-Button.

**Watchlist-Einträge (Liste):**
Jeder Eintrag ist eine Zeile, volle Breite, Höhe 44px, Padding 8px horizontal.

```
┌──────────────────────────────────────┐
│ AAPL     $142.58   +2.34%  ╱╲╱╲╱╲  │
│ Apple Inc.                  ~~~~~~~~ │
└──────────────────────────────────────┘
```

Layout pro Zeile:
- **Links:** Symbol (`JetBrains Mono Bold`, 13px, `text-primary`) und Firmenname darunter (`Inter`, 10px, `text-secondary`, truncated mit Ellipsis wenn zu lang)
- **Mitte:** Aktueller Preis (`JetBrains Mono`, 13px, `text-primary`)
- **Rechts oben:** Prozent-Veränderung (`JetBrains Mono`, 12px, `green-primary` oder `red-primary`, mit Vorzeichen)
- **Rechts unten:** Mini-Sparkline (40px breit, 16px hoch, Linienchart der letzten 24h, Farbe passend zu Veränderung)

**Zeilen-Interaktion:**
- Hover: Hintergrund `bg-tertiary`, Transition 150ms
- Klick: Wählt die Aktie aus (siehe rechte Sidebar), öffnet Aktien-Detail im Zentralbereich
- Ausgewählte Zeile: linker Rand 2px solid `text-accent`, Hintergrund leicht `bg-tertiary`
- Rechtsklick: Kontextmenü mit:
  - `View Details` — öffnet Aktien-Detail
  - `Trade` — fokussiert Order-Panel
  - `Remove from Watchlist` — entfernt Eintrag (mit kurzer Fade-Out-Animation, 200ms)
- Drag & Drop: Spieler kann Einträge umsortieren. Drag-Handle erscheint links bei Hover (6 Punkte, `grip-vertical`-Icon, 12px, `text-disabled`)

**Leerzustand (keine Stocks auf Watchlist):**
Zentrierter Text: `Your watchlist is empty.` (`Inter`, 13px, `text-disabled`). Darunter: `Click + to add stocks` (`text-accent`, 12px, klickbar — öffnet Suche).

**Scrollverhalten:**
Wenn mehr als ~12 Einträge (abhängig von Panelhöhe): vertikaler Scrollbalken rechts, dünn (6px), Farbe `bg-tertiary`, erscheint nur bei Hover über das Panel.

**Mehrere Watchlists:**
- Über dem Suchfeld: Dropdown zur Watchlist-Auswahl
- Standard-Watchlists: `Main` (Standard, nicht löschbar)
- Spieler kann neue Watchlists erstellen: `+ New List` am Ende des Dropdowns
- Klick → Modal: Name eingeben (z.B. "Short Candidates", "Dividend Picks", "Tech Watchlist")
- Maximal 10 Watchlists
- Jede Watchlist ist unabhängig (Aktie kann in mehreren sein)
- Rechtsklick auf Watchlist-Name im Dropdown: `Rename` / `Delete`
- Aktien können per Rechtsklick → `Move to List...` oder `Copy to List...` verschoben/kopiert werden

#### 3.3.2 Sektor-Übersicht-Panel

**Panel-Header:**
- Titel: `Sectors` in `Inter SemiBold`, 14px, `text-primary`
- Kein weiterer Button im Header

**Sektor-Einträge (Liste):**
12 Einträge, einer pro Sektor. Jeder Eintrag: Höhe 36px, Padding 8px horizontal.

```
┌──────────────────────────────────────┐
│ ⚡ Energy                    -1.24%  │
└──────────────────────────────────────┘
```

Layout pro Zeile:
- **Links:** Sektor-Icon (16px, `text-secondary`) + 8px Abstand + Sektorname (`Inter`, 13px, `text-primary`)
- **Rechts:** Tagesperformance (`JetBrains Mono`, 13px, farbig: `green-primary` oder `red-primary`, mit Vorzeichen)

**Zeilen-Interaktion:**
- Hover: Hintergrund `bg-tertiary`, Tooltip erscheint nach 500ms Verzögerung
- Tooltip-Inhalt: `X stocks | Market Cap: $XXB | Vol: XXM`
- Klick: Wechselt zum Market-Tab im Zentralbereich und setzt den Sektor-Filter auf diesen Sektor
- Aktiver Filter: Zeile bekommt linken Rand 2px solid `text-accent`

**Sortierung:** Standardmäßig nach absoluter Performance (größte Veränderung oben). Nicht vom Spieler änderbar.

#### 3.3.3 Globale Aktiensuche (Search Overlay)

**Globale Aktiensuche (Search Overlay):**
Wird ausgelöst durch: `Ctrl+F`, `/`-Taste, oder Klick auf `+` in der Watchlist.

Layout: Ein zentriertes Overlay (600px × auto, max 500px Höhe) mit dunklem Hintergrund-Dim.

```
┌────────────────────────────────────────────────┐
│  🔍 [Search stocks by name or symbol...    ]   │
│                                                │
│  AAPL   Apple Inc.              Technology  ▲  │
│  ACLS   Apex Cloud Solutions    Technology  ▼  │
│  AMZN   Amazon Corp.           Consumer    ▲  │
│  ...                                           │
│                                                │
│  Showing 15 of 500 results                     │
└────────────────────────────────────────────────┘
```

- Suchfeld oben: Auto-Fokus, Echtzeit-Filterung während der Eingabe
- Ergebnisliste: Symbol (Bold, 14px) + Name + Sektor-Badge + Tagesperformance
- Jede Zeile: Hover = bg-tertiary, Klick = Aktie auswählen + Overlay schließen
- Rechtsklick auf Ergebnis: Kontextmenü (Add to Watchlist, Trade, View Details)
- Escape oder Klick außerhalb: schließt Overlay
- Max 15 Ergebnisse sichtbar, scrollbar
- Bei leerem Suchfeld: zeigt die 15 meistgehandelten Aktien des Tages

### 3.4 Zentraler Bereich

Der zentrale Bereich nimmt den gesamten Platz zwischen linker Sidebar, rechter Sidebar, Top Bar und News Ticker ein. Sein Inhalt wechselt je nach aktivem Tab in der Top Bar.

Hintergrund: `bg-primary` (`#0A0E17`). Padding: 16px auf allen Seiten.

#### 3.4.1 Dashboard-Tab (Standard nach Spielstart)

Das Dashboard zeigt eine Gesamtübersicht des Marktes. Es ist die Startseite und bietet einen schnellen Überblick.

**Layout (von oben nach unten):**

**1. Markt-Index-Chart (obere 30% des zentralen Bereichs)**
- Ein Linien-Chart des Gesamtmarkt-Index über den aktuellen Handelstag
- TradingView Lightweight Charts, Area-Typ (gefüllte Fläche unter der Linie)
- Linie: `text-accent` wenn positiv, `red-primary` wenn negativ (gemessen ab Tageseröffnung)
- Füllung: entsprechende Glow-Farbe (leicht transparent)
- Links oben im Chart: `Market Index` Label + aktueller Wert + Tagesveränderung
- Zeitrahmen: fest auf Intraday (1 Handelstag). Keine Zeitrahmen-Auswahl — dafür gibt es die Aktien-Detail-Ansicht

**2. Sektor-Heatmap (mittlere 40%)**
- Treemap-Layout: Rechtecke, deren Größe proportional zur Marktkapitalisierung des Sektors ist
- Farbe: Intensität zeigt Tagesperformance
  - Stark positiv (>3%): gesättigtes `green-primary`
  - Leicht positiv (0-3%): abgedunkeltes Grün
  - Neutral (±0.1%): `bg-tertiary` (grau)
  - Leicht negativ (0 bis -3%): abgedunkeltes Rot
  - Stark negativ (<-3%): gesättigtes `red-primary`
- In jedem Rechteck: Sektor-Name (`Inter SemiBold`, 14px, weiß) und Performance-Zahl (`JetBrains Mono`, 18px, weiß)
- Hover: Rechteck bekommt hellen Border, Tooltip mit Details
- Klick: wechselt zu Market-Tab mit Sektor-Filter

**3. Top Movers (untere 30%, zwei Spalten nebeneinander)**

**Linke Spalte: Top Gainers**
- Titel: `Top Gainers ▲` in `green-primary`, `Inter SemiBold`, 14px
- Liste der 5 Aktien mit größtem prozentualen Tagesgewinn
- Pro Eintrag (Höhe 36px): Rang (`#1`-`#5`, `text-disabled`, 12px) | Symbol (`JetBrains Mono Bold`, 13px) | Name (truncated, `text-secondary`, 12px) | Preis (`JetBrains Mono`, 13px) | Veränderung (`green-primary`, `JetBrains Mono`, 13px)
- Hover auf Zeile: Hintergrund `bg-tertiary`
- Klick: öffnet Aktien-Detail

**Rechte Spalte: Top Losers**
- Titel: `Top Losers ▼` in `red-primary`, `Inter SemiBold`, 14px
- Identisches Layout, aber mit den 5 Aktien mit größtem Verlust
- Veränderung in `red-primary`

#### 3.4.2 Aktien-Detail-Ansicht

Wird angezeigt, wenn der Spieler eine Aktie anklickt (aus Watchlist, Tabelle, Heatmap oder anderswo). Ersetzt den aktuellen Tab-Inhalt. Ein `← Back`-Button (oben links, `text-accent`, 14px) kehrt zur vorherigen Ansicht zurück. Escape-Taste tut dasselbe.

**Layout:**
Der gesamte zentrale Bereich wird für die Aktien-Detail-Ansicht genutzt.

**Aktien-Header (obere 80px):**
```
┌─────────────────────────────────────────────────────────────┐
│ ← Back                                                      │
│                                                              │
│ AAPL          $142.58    +$3.26 (+2.34%)  ▲                 │
│ Apple Inc.    Technology                                     │
│                                                              │
│ Day Range: $138.20 ━━━━━━━━━━━●━━━ $143.10                  │
│ 52W Range: $98.50 ━━●━━━━━━━━━━━━━ $167.30                  │
└─────────────────────────────────────────────────────────────┘
```

- Symbol: `JetBrains Mono Bold`, 24px, `text-primary`
- Firmenname: `Inter`, 14px, `text-secondary`
- Sektor-Badge: Hintergrund `bg-tertiary`, Padding 4px 8px, Border-Radius 4px, Sektor-Icon + Name in 12px
- Preis: `JetBrains Mono Bold`, 28px, `text-primary`
- Veränderung: `JetBrains Mono`, 16px, `green-primary` oder `red-primary`, mit +/- und ▲/▼
- Day Range: horizontaler Balken, 200px breit, Hintergrund `bg-tertiary`, gefüllter Bereich `text-secondary`, Punkt bei aktuellem Preis. Links: Tages-Low, rechts: Tages-High (`JetBrains Mono`, 11px)
- 52W Range: gleicher Stil, aber für 52-Wochen-Bereich

**Zweite Zeile im Header (zusätzliche Daten):**
```
Bid: $142.45 (2,500)    Ask: $142.63 (1,800)    Spread: $0.18 (0.13%)
Volume: 2.3M    Avg Volume: 4.1M    Short Interest: 8.2%
Analyst: BUY (4B 2H 1S)    Target: $165.00 (+15.7%)
Dividend: $1.50/qtr (2.4%)    Ex-Date: Apr 12    Next Earnings: May 5
```
- Bid/Ask mit Größe in Klammern (Anzahl Aktien am besten Level): `JetBrains Mono`, 12px
- Volume / Avg Volume: aktuelles Tagesvolumen vs. 20-Tage-Durchschnitt
- Short Interest: in `warning`-Farbe wenn >20%
- Analyst Rating: kompakte Darstellung mit Balken (siehe 4.7)
- Dividend-Info: nur bei Dividend-Aktien, sonst weggelassen
- Next Earnings: Datum des nächsten Earnings-Reports (wichtig für Planung)

**Chart-Bereich (unter dem Header, Höhe: restlicher Platz minus 80px Header):**
Vollständige Beschreibung in Kapitel 12 (Charts & Datenvisualisierung).

#### 3.4.3 Portfolio-Tab

Wird angezeigt wenn der Spieler den `Portfolio`-Tab wählt. Vollständige Beschreibung in Kapitel 6 (Portfolio Management).

#### 3.4.4 Market-Tab

**Aktientabelle — die vollständige Liste aller handelbaren Aktien.**

**Filterleiste (oben, Höhe 48px):**
- Suchfeld: Breite 240px, gleicher Stil wie Watchlist-Suche, Placeholder `Search by name or symbol...`
- Sektor-Dropdown: Breite 180px, Höhe 32px, Hintergrund `bg-input`, Border `border`. Ausgewählt: Sektorname. Default: `All Sectors`. Dropdown-Liste zeigt alle 12 Sektoren mit Icons. Klick auf Sektor filtert Tabelle sofort.
- Price Range: zwei kleine Eingabefelder (je 80px): `Min $` und `Max $`, `JetBrains Mono`, 12px
- Performance-Filter: Dropdown mit Optionen: `All`, `Gainers only`, `Losers only`, `>5%`, `<-5%`
- Ergebnis-Zähler: rechts, `Showing 847 of 1,000 stocks` in `text-secondary`, 12px

**Tabelle:**

| Spalte | Breite | Schrift | Ausrichtung | Sortierbar |
|---|---|---|---|---|
| Symbol | 80px | `JetBrains Mono Bold`, 13px, `text-primary` | links | ja |
| Name | 180px (flex) | `Inter`, 13px, `text-secondary` | links | ja |
| Sector | 120px | `Inter`, 12px, `text-secondary`, Badge-Stil | links | ja |
| Price | 100px | `JetBrains Mono`, 13px, `text-primary` | rechts | ja |
| Change | 90px | `JetBrains Mono`, 13px, farbig | rechts | ja |
| Change % | 80px | `JetBrains Mono`, 13px, farbig | rechts | ja |
| Volume | 90px | `JetBrains Mono`, 12px, `text-secondary` | rechts | ja |
| Market Cap | 100px | `JetBrains Mono`, 12px, `text-secondary` | rechts | ja |

**Tabellen-Header:**
- Hintergrund: `bg-secondary`, Position: sticky (bleibt oben beim Scrollen)
- Schrift: `Inter SemiBold`, 11px, `text-secondary`, Uppercase
- Klick auf Header: sortiert Tabelle nach dieser Spalte. Erster Klick: absteigend. Zweiter Klick: aufsteigend. Dritter Klick: Standard-Sortierung (alphabetisch nach Symbol).
- Sortier-Indikator: kleines Dreieck rechts neben dem Spaltennamen (▲ oder ▼), `text-accent`

**Tabellen-Zeilen:**
- Höhe: 40px, Padding 8px horizontal
- Hover: Hintergrund `bg-tertiary`
- Klick: öffnet Aktien-Detail-Ansicht
- Rechtsklick: Kontextmenü (`View Details`, `Add to Watchlist`, `Trade`)
- Zebra-Striping: jede zweite Zeile hat Hintergrund `rgba(31, 41, 55, 0.3)` (kaum sichtbar, aber hilft bei der Lesbarkeit)
- Preis-Zellen blinken bei Änderung (Zahlen-Tick-Animation, siehe 2.7)

**Virtualisiertes Scrollen:**
Bei 1000+ Aktien werden nur die sichtbaren Zeilen gerendert (React-Virtualized oder ähnlich). Scrollbalken rechts, dünn (6px). Scrollgeschwindigkeit: schnell, kein Momentum-Scroll (da präzise Auswahl nötig).

**Erweiterter Aktien-Screener:**
Zusätzlich zur einfachen Filterleiste gibt es einen erweiterten Screener, zugänglich über den Button `Advanced Screener` rechts in der Filterleiste. Öffnet ein Panel über der Tabelle (200px Höhe, einklappbar).

**Screener-Kriterien (kombinierbar mit UND-Logik):**

| Kategorie | Kriterium | Eingabe |
|---|---|---|
| **Preis** | Price Range | Min $ — Max $ |
| | Day Change % | Min % — Max % |
| | 52-Week Position | Near High / Near Low / Middle |
| **Fundamentals** | P/E Ratio | Min — Max |
| | Market Cap | Micro / Small / Mid / Large / Mega (Checkboxen) |
| | Dividend Yield | Min % — Max % |
| | Revenue Growth | Min % — Max % |
| **Technicals** | RSI (14) | Min — Max (z.B. <30 für überverkauft) |
| | Above/Below SMA 50 | Above / Below |
| | Above/Below SMA 200 | Above / Below |
| | Volume vs Average | Min % (z.B. >200% für ungewöhnliches Volumen) |
| **Trading** | Short Interest | Min % — Max % |
| | Analyst Rating | Strong Buy / Buy / Hold / Sell / Strong Sell |
| | Sector | Dropdown (mehrfachauswahl) |
| | Liquidity Score | Min — Max (1-10) |

**Screener-Presets (schnelle Auswahl):**
- `Undervalued` — P/E <15, Dividend >2%, RSI <40
- `Growth Stocks` — Revenue Growth >20%, P/E >30, Above SMA 50
- `Dividend Champions` — Dividend >3%, Large/Mega Cap
- `Short Squeeze Candidates` — Short Interest >25%, Volume >200% avg
- `Oversold Bounce` — RSI <30, Day Change <-5%
- `Unusual Volume` — Volume >300% average, any direction
- `Custom` — Spieler kann eigene Preset speichern (maximal 5)

**UI:** Jedes Kriterium als Zeile mit Label, Eingabefeldern und einem `✕`-Button zum Entfernen. `+ Add Criteria` Button. `[Apply]` Button. `[Reset]` Link. Ergebnisse aktualisieren sich sofort in der Tabelle darunter.

#### 3.4.5 Orders-Tab

Vollständige Beschreibung in Kapitel 4.10 (Order-Management-UI).

#### 3.4.6 News-Tab

Vollständige Beschreibung in Kapitel 13 (News Ticker & News System).

#### 3.4.7 Analytics-Tab

**Sub-Tabs im Analytics-Tab:** `[Performance]  [Journal]`
Der Performance-Sub-Tab enthält alle unten beschriebenen Elemente (Charts, Metriken). Der Journal-Sub-Tab zeigt das Trading Journal (siehe 6.4).

**Performance-Dashboard für das eigene Portfolio.**

**Obere Reihe (3 Karten nebeneinander, je ⅓ Breite, Höhe 100px):**

Karte 1: `Total Return`
- Große Zahl: `+$12,456.78 (+24.9%)` in `green-primary` oder `red-primary`, `JetBrains Mono Bold`, 24px
- Subtitle: `Since inception` in `text-secondary`, 12px

Karte 2: `Win Rate`
- Große Zahl: `67.3%` in `text-primary`, `JetBrains Mono Bold`, 24px
- Subtitle: `142 of 211 trades profitable` in `text-secondary`, 12px

Karte 3: `Best / Worst Trade`
- Zwei Zeilen: `Best: +$2,340 (AAPL)` in `green-primary`, 14px
- `Worst: -$890 (TSLA)` in `red-primary`, 14px

**Mittlerer Bereich (60% Höhe): Portfolio-Performance-Chart**
- Linien-Chart: Gesamtportfolio-Wert über Zeit
- Zweite Linie (gestrichelt): Markt-Index zum Vergleich
- Legende oben rechts im Chart: `Portfolio` (solid, `text-accent`) und `Market Index` (dashed, `text-secondary`)
- Zeitraum-Buttons: `1W`, `1M`, `3M`, `6M`, `1Y`, `All` — identischer Stil wie Chart-Zeitrahmen

**Unterer Bereich (40% Höhe, zwei Spalten):**

Linke Spalte: `Sector Allocation`
- Donut-Chart (Kreisdiagramm mit Loch)
- Jeder Sektor hat eine eigene Farbe (aus einer vordefinierten Palette, nicht die Rot/Grün-Farben)
- Legende rechts neben dem Donut: Sektorname + Prozent + Betrag
- Zentrums-Text: Gesamtinvestierter Betrag

Rechte Spalte: `Monthly P&L`
- Bar-Chart: ein Balken pro Monat (oder Woche wenn weniger als 3 Monate gespielt)
- Positive Monate: `green-primary`
- Negative Monate: `red-primary`
- X-Achse: Monat/Jahr, Y-Achse: P&L in $

### 3.5 Rechte Sidebar: Trading Panel & Order Entry

Hintergrund: `bg-secondary`. Linke Border: 1px solid `border`. Breite: 320px. Vertikal scrollbar.

Die Sidebar enthält drei Bereiche übereinander:
1. **Ausgewählte Aktie** (Kompakt-Info, ~100px)
2. **Order-Eingabe-Panel** (~350px)
3. **Offene Positionen Quick View** (Rest)

#### 3.5.1 Ausgewählte Aktie (Kompakt-Info)

Zeigt die aktuell in der Watchlist oder Tabelle angeklickte Aktie. Wenn keine Aktie ausgewählt: Platzhalter `Select a stock to trade` in `text-disabled`, 14px, zentriert.

**Layout bei ausgewählter Aktie:**
```
┌──────────────────────────────┐
│ AAPL          Technology  ⚡ │
│ Apple Inc.                   │
│                              │
│ $142.58      +2.34%  ▲      │
│                              │
│ ────── Mini-Chart (24h) ──── │
└──────────────────────────────┘
```

- Symbol: `JetBrains Mono Bold`, 18px, `text-primary`
- Sektor-Badge: klein, rechts, 10px, `text-secondary` mit Icon
- Firmenname: `Inter`, 12px, `text-secondary`
- Preis: `JetBrains Mono Bold`, 22px, `text-primary`
- Veränderung: `JetBrains Mono`, 14px, farbig, mit Vorzeichen und Dreieck
- Mini-Chart: 280px × 40px, Area-Chart (TradingView Lightweight), zeigt die letzten 24h, keine Achsenbeschriftung, Farbe passend zu Tagesperformance
- Padding: 16px

#### 3.5.2 Order-Eingabe-Panel

**Tab-Auswahl (Order-Seite):**
Drei Tabs in einer Tab-Group, volle Breite:

| Tab | Label | Farbe (aktiv) |
|---|---|---|
| Buy | `BUY` | `green-primary` auf `green-dim` Hintergrund |
| Sell | `SELL` | `red-primary` auf `red-dim` Hintergrund |
| Short | `SHORT` | `warning` auf dunklem Amber-Hintergrund |

- Schrift: `Inter Bold`, 13px, Uppercase
- Inaktive Tabs: `text-secondary` auf `bg-tertiary`
- Tab-Höhe: 36px
- Kein Abstand zwischen Tabs (durchgängige Button-Group)

**Order-Typ-Dropdown (unter den Tabs, volle Breite):**
- Label darüber: `Order Type` in `text-secondary`, 11px
- Dropdown: Höhe 36px, Hintergrund `bg-input`, Border `border`
- Optionen: `Market`, `Limit`, `Stop`, `Stop-Limit`, `Trailing Stop`
- Ausgewählt: `Market` (Standard)
- Der Inhalt darunter ändert sich je nach gewähltem Order-Typ

**Felder für Market Order:**
```
┌──────────────────────────────┐
│ Order Type                   │
│ [Market                  ▼] │
│                              │
│ Quantity                     │
│ [         ] shares           │
│                              │
│ Estimated Cost               │
│ $1,425.80                    │
│                              │
│ Available Cash: $48,574.20   │
│                              │
│ [     PLACE BUY ORDER      ] │
└──────────────────────────────┘
```

- **Quantity-Feld:** Breite 100%, Höhe 36px, `JetBrains Mono`, 14px, `bg-input`, rechts daneben `shares` Label in `text-secondary`. Dezimalstellen erlaubt (min. 0.001 für Fractional Shares, siehe 4.1). Toggle `Shares` / `Amount ($)` rechts neben dem Feld. Bei Short-Orders: nur Ganzzahlen. Bei Eingabe: Estimated Cost aktualisiert sich live.
- **Estimated Cost:** Berechnet: Quantity × aktueller Ask-Preis (bei Buy) oder Bid-Preis (bei Sell). `JetBrains Mono`, 16px, `text-primary`. Aktualisiert sich in Echtzeit mit dem Preis.
- **Available Cash:** `JetBrains Mono`, 12px, `text-secondary`. Zeigt verfügbares Cash. Wird rot wenn Estimated Cost > Available Cash.
- **Place Order Button:** Volle Breite, Höhe 44px, Border-Radius 6px.
  - Buy: Hintergrund `green-primary`, Text `#FFFFFF`, `Inter Bold`, 14px, Uppercase
  - Sell: Hintergrund `red-primary`, Text `#FFFFFF`
  - Short: Hintergrund `warning`, Text `#000000`
  - Hover: 10% heller
  - Disabled (z.B. nicht genug Cash): Opacity 0.4, Cursor `not-allowed`
  - Klick: öffnet Bestätigungsdialog (siehe 3.7)

**Zusätzliche Felder für Limit Order:**
- **Limit Price:** Eingabefeld, gleicher Stil wie Quantity. Label: `Limit Price`. Prefix `$` im Feld. Vorausgefüllt mit aktuellem Preis.
- **Time in Force:** Dropdown mit Optionen: `GTC (Good Till Cancelled)`, `Day Order`, `GTD (Good Till Date)`. Bei GTD: zusätzliches Datumsfeld.

**Zusätzliche Felder für Stop Order:**
- **Stop Price:** Eingabefeld. Label: `Stop Price (trigger)`.
- Hinweistext darunter: `When price reaches $XXX, a market order will be placed.` in `text-disabled`, 11px.

**Zusätzliche Felder für Stop-Limit Order:**
- **Stop Price:** Label: `Stop Price (trigger)`
- **Limit Price:** Label: `Limit Price (execution)`
- Hinweistext: `When price reaches stop, a limit order at $XXX will be placed.` in `text-disabled`, 11px.

**Zusätzliche Felder für Trailing Stop:**
- **Trail Amount:** Eingabefeld + Toggle-Button (`$` oder `%`)
  - Dollar-Modus: `Trail by $5.00`
  - Prozent-Modus: `Trail by 3.5%`
- Hinweistext: `Stop price adjusts automatically as the market moves in your favor.` in `text-disabled`, 11px.

#### 3.5.3 Offene Positionen (Quick View)

**Titel:** `Your Position` in `Inter SemiBold`, 13px, `text-secondary`

Wenn der Spieler eine Position in der aktuell ausgewählten Aktie hat:
```
┌──────────────────────────────┐
│ Your Position                │
│                              │
│ 50 shares @ $138.20          │
│ Value: $7,129.00             │
│ P&L: +$219.00 (+3.17%)      │
│                              │
│ [SELL ALL]  [SELL PARTIAL]   │
└──────────────────────────────┘
```

- Shares und Durchschnittspreis: `JetBrains Mono`, 13px, `text-primary`
- Value: `JetBrains Mono`, 13px, `text-secondary`
- P&L: `JetBrains Mono`, 14px, farbig (`green-primary` / `red-primary`)
- `Sell All` Button: `red-primary` Hintergrund, 50% Breite
- `Sell Partial` Button: `bg-tertiary` Hintergrund, `text-primary`, 50% Breite
- Klick auf `Sell Partial`: Fokussiert das Order-Panel (rechte Sidebar), wechselt zum SELL-Tab, Symbol ist vorausgefüllt, Stückzahl-Feld ist leer und fokussiert (Spieler gibt gewünschte Verkaufsmenge ein).

Wenn Short-Position:
- Text zeigt: `Short: 30 shares @ $155.00`
- Buttons: `COVER ALL`, `COVER PARTIAL`

Wenn keine Position: `No position in AAPL` in `text-disabled`, 12px.

#### 3.5.4 Price Alerts

**Position:** Unter der Position Quick View (oder in einem Tab zusammen mit der Quick View).

**Funktion:** Der Spieler kann Preisalarme setzen: "Benachrichtige mich, wenn AAPL $150 erreicht."

**UI:**
```
┌──────────────────────────────┐
│ Price Alerts           [+ ]  │
│                              │
│ AAPL ≥ $150.00    [🔔] [✕]  │
│ AAPL ≤ $130.00    [🔔] [✕]  │
│ TSLA ≥ $200.00    [🔔] [✕]  │
│                              │
│ No alert for selected stock. │
│ [+ Set Alert]                │
└──────────────────────────────┘
```

- **Neuen Alert erstellen:** Klick auf `+` oder `Set Alert` Button
  - Dropdown: `When price is` → `≥ (above)` oder `≤ (below)`
  - Eingabefeld: Preis
  - `[Create Alert]` Button
- **Alert-Eintrag:** Symbol + Bedingung + Preis. `🔔`-Icon (aktiv = `text-accent`, stumm = `text-disabled`). `✕` zum Löschen.
- **Wenn Alert triggert:**
  - Spiel pausiert automatisch (konfigurierbar)
  - Toast mit Sound: `Price Alert: AAPL has reached $150.00!` (info)
  - Alert wird automatisch deaktiviert (einmalig, nicht wiederkehrend)
- **Maximale Alerts:** 20 gleichzeitig aktive Alerts
- **Tastenkürzel:** Kein dedizierter Shortcut — über UI

### 3.6 Unterer Bereich: News Ticker

Vollständige Beschreibung in Kapitel 13.1 (News Ticker).

Kurzfassung für das Layout: Ein durchgängiger Streifen am unteren Rand. Hintergrund: `#080C14` (noch dunkler als `bg-primary`). Obere Border: 1px solid `border`. Höhe: 36px. Text scrollt von rechts nach links. Farbige Headlines. Klickbar.

### 3.7 Modale Dialoge & Overlays

#### Order-Bestätigungsdialog

Erscheint nach Klick auf "Place Order". Zentriert, Breite 400px, Höhe auto.

```
┌────────────────────────────────────────┐
│          Confirm Buy Order             │
│                                        │
│  Stock:        AAPL                    │
│  Order Type:   Market                  │
│  Quantity:     50 shares               │
│  Est. Price:   $142.58                 │
│  Est. Total:   $7,129.00              │
│  Commission:   $X.XX (per settings)    │
│                ─────────               │
│  Total Cost:   $X,XXX.XX              │
│                                        │
│  ⚠ Market orders execute at the best  │
│    available price. Actual price may   │
│    differ slightly.                    │
│                                        │
│  [  CANCEL  ]        [  CONFIRM  ]     │
└────────────────────────────────────────┘
```

- Titel: `Inter SemiBold`, 16px, `text-primary`
- Labels links: `Inter`, 13px, `text-secondary`
- Werte rechts: `JetBrains Mono`, 13px, `text-primary`
- Total Cost: `JetBrains Mono Bold`, 16px, `text-primary`
- Warnhinweis: `warning`-Farbe, `Inter`, 11px, mit ⚠-Icon
- Cancel-Button: `bg-tertiary`, `text-primary`, 48% Breite
- Confirm-Button: `green-primary` (Buy), `red-primary` (Sell), `warning` (Short), weiße Schrift, 48% Breite
- Enter-Taste: triggert Confirm. Escape-Taste: triggert Cancel.
- Overlay-Hintergrund: `bg-overlay`

**Diese Bestätigung kann in den Settings deaktiviert werden** — dann wird die Order sofort platziert.

#### Event-Popup (Breaking News)

Erscheint bei Major/Catastrophic Events. Zentriert, Breite 600px.

```
┌────────────────────────────────────────────────────┐
│ ▰▰ BREAKING NEWS ▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰▰ │
│                                                    │
│ Federal Reserve Raises Interest Rates by 50bps     │
│                                                    │
│ The Federal Reserve announced a surprise 50 basis  │
│ point rate hike, citing persistent inflation       │
│ concerns. Markets are expected to react            │
│ significantly, particularly in rate-sensitive      │
│ sectors such as Real Estate and Technology.        │
│                                                    │
│ Affected: MARKET-WIDE                              │
│ Expected Impact: ▼ Negative (Major)                │
│                                                    │
│ [    DISMISS    ]                 Auto-close: 15s  │
└────────────────────────────────────────────────────┘
```

- Banner: Hintergrund `red-primary` (negative Events) oder `green-primary` (positive), volle Breite, Höhe 36px, Text `BREAKING NEWS` in Weiß, `Inter Black`, 14px, Uppercase, zentriert
- Headline: `Inter Bold`, 20px, `text-primary`
- Fließtext: `Inter`, 14px, `text-secondary`, Zeilenhöhe 22px
- Affected: Aktien-Symbole als klickbare Badges
- Expected Impact: farbig, mit Dreieck
- Dismiss-Button: Volle Breite unten, `bg-tertiary`, `text-primary`, 44px Höhe
- Auto-close Timer: rechts unten, Countdown in Sekunden, `text-disabled`
- Spiel pausiert automatisch wenn Popup erscheint (konfigurierbar)
- Overlay dahinter: `bg-overlay`
- Erscheint mit Sound (Breaking News Jingle, siehe Kapitel 17)

#### Toast-Notifications

Kleine Benachrichtigungen oben rechts im Fenster (unterhalb der Top Bar, 8px Abstand, 8px vom rechten Rand).

- Breite: 320px, Höhe: auto (min 48px)
- Hintergrund: `bg-secondary`, Border: 1px solid `border`, Border-Radius: 8px
- Aufbau: Icon links (20px) + Text rechts
- Schrift: `Inter`, 13px, `text-primary`
- Sub-Text: `Inter`, 11px, `text-secondary`
- Maximale Dauer: 5 Sekunden, dann Fade-Out (500ms)
- Hover: Timer pausiert
- Klick: navigiert zum relevanten Bereich (z.B. Klick auf Trade-Toast öffnet Orders)
- Stapeln: maximal 3 Toasts gleichzeitig sichtbar, neue schieben alte nach unten

**Toast-Typen:**
| Typ | Icon | Border-Links-Farbe | Beispiel |
|---|---|---|---|
| Trade Executed | `check-circle` in `green-primary` | `green-primary` | `Bought 50 AAPL @ $142.58` |
| Order Cancelled | `x-circle` in `text-secondary` | `text-secondary` | `Limit order for MSFT cancelled` |
| Order Rejected | `alert-circle` in `red-primary` | `red-primary` | `Insufficient funds for order` |
| News Alert | `newspaper` in `info` | `info` | `AAPL: Earnings beat expectations` |
| Margin Warning | `alert-triangle` in `warning` | `warning` | `Margin utilization at 85%` |
| Game Saved | `save` in `green-primary` | `green-primary` | `Game saved successfully` |

### 3.8 Kontextmenüs

Alle Kontextmenüs haben einheitliches Styling:
- Hintergrund: `bg-secondary`
- Border: 1px solid `border`
- Border-Radius: 8px
- Box-Shadow: `0 4px 16px rgba(0, 0, 0, 0.4)`
- Padding: 4px vertikal
- Maximale Breite: 240px

**Einzelner Menüeintrag:**
- Höhe: 32px, Padding: 8px 12px
- Icon links (16px, `text-secondary`) + 8px + Text (`Inter`, 13px, `text-primary`) + Shortcut rechts (`text-disabled`, 11px)
- Hover: Hintergrund `bg-tertiary`
- Trennlinie zwischen Gruppen: 1px solid `border`, Margin 4px vertikal

**Kontextmenü: Aktie (Watchlist, Tabelle, Chart)**
| Eintrag | Icon | Shortcut |
|---|---|---|
| View Details | `eye` | Enter |
| Buy | `arrow-up-circle` (grün) | B |
| Sell | `arrow-down-circle` (rot) | S |
| Short | `trending-down` (amber) | H |
| Add to Watchlist | `plus` | W |
| Remove from Watchlist | `x` | — |
| — (Trennlinie) | | |
| View in Market Tab | `layout-list` | M |

**Kontextmenü: Order (in Orders-Tab)**
| Eintrag | Icon |
|---|---|
| Modify Order | `edit` |
| Cancel Order | `x-circle` (rot) |
| View Stock | `eye` |

**Kontextmenü: Position (in Portfolio)**
| Eintrag | Icon |
|---|---|
| Sell All | `arrow-down-circle` (rot) |
| Sell Partial | `minus-circle` |
| View Details | `eye` |
| Set Stop Loss | `shield` |

---

## 4. Trading Mechanics

### 4.1 Übersicht des Handelssystems

Das gesamte Handelssystem läuft im C#-Backend. Das Frontend sendet Order-Anfragen per WebSocket, das Backend validiert, matched und sendet Ergebnisse zurück.

**Kontomodell:**
- **Cash Balance:** Verfügbares Bargeld. Startet mit dem gewählten Startkapital ($25k / $50k / $100k).
- **Margin Balance:** Geliehenes Kapital für Margin-Trades. Startet bei $0. Maximale Margin = 2× Cash (d.h. bei $50k Cash: bis zu $100k Buying Power).
- **Buying Power:** Cash + verfügbare Margin. Reduziert sich bei jedem Kauf.
- **Portfolio Value:** Marktwert aller offenen Positionen (Long + Short).
- **Total Equity:** Cash + Portfolio Value - Margin Schulden.

**Zinsen auf Cash-Guthaben:** Nicht investiertes Cash verdient Zinsen in Höhe von Leitzins - 1% p.a. (mindestens 0%). Bei einem Leitzins von 4% = 3% p.a. auf Cash. Tägliche Gutschrift, automatisch. Anzeige im Portfolio: `Cash interest earned today: +$X.XX` als kleine Zeile unter dem Cash-Betrag.

**Handelsgebühren (Kommissionen):**
- Standard: $4.95 pro Trade (nicht pro Aktie), abhängig von Difficulty
- Konfigurierbar in Settings: kann auf $0 gesetzt oder erhöht werden
- Wird bei Order-Bestätigung angezeigt und von Cash abgezogen bei Ausführung

**Fractional Shares (Bruchstücke):**
- Der Spieler kann Aktien in Bruchstücken kaufen (mindestens 0.001 Aktien)
- Ermöglicht Dollar-basiertes Kaufen: "Kaufe für $500 AAPL" → ergibt z.B. 3.512 Aktien
- UI: im Order-Panel ein Toggle `Shares` / `Amount ($)`:
  - `Shares`-Modus (Standard): Spieler gibt Stückzahl ein (erlaubt Dezimalstellen: 0.001)
  - `Amount`-Modus: Spieler gibt Dollar-Betrag ein, System berechnet Stückzahl
- Anzeige im Portfolio: Stückzahl mit 3 Dezimalstellen wenn Bruchteil (`50.000` vs. `3.512`)
- Verkauf: Fractional Shares können nur als Market Order verkauft werden (wie bei echten Brokern)
- Kein Fractional Short Selling (nur ganze Aktien shortbar)

**Settlement (T+1):**
- Trades settlen 1 Handelstag nach Ausführung (T+1)
- **Auswirkung auf Cash:** Verkaufserlös ist sofort als "Unsettled Cash" verfügbar, wird nach 1 Tag zu "Settled Cash"
- **Unsettled Cash:** Kann für neue Käufe verwendet werden (wie bei echten Brokern mit Margin-Konten)
- **Auswirkung auf Short:** Geliehene Aktien müssen T+1 geliefert werden (bei Naked Short → Forced Buy-In nach T+3)
- **UI:** Im Portfolio: `Cash: $48,000 ($3,500 settling)` — der Settling-Betrag in `text-disabled`
- **Vereinfachung:** Für den Spieler ändert sich im Alltag wenig (Unsettled Cash ist nutzbar). Die Mechanik wird nur relevant bei:
  - Bankrott-Berechnung: nur Settled Cash zählt
  - SMA-Prüfung: Wash-Trading-Erkennung basiert auf Settlement-Zeiten
  - Forced Buy-In bei fehlgeschlagener Aktienlieferung (Short)

**Handelszeiten:**
- Market Open: 9:30 AM (Spielzeit)
- Market Close: 4:00 PM (Spielzeit)
- Außerhalb dieser Zeiten können keine Orders ausgeführt werden
- Orders können jederzeit platziert werden — sie werden bei Market Open ausgeführt (bei Market Orders) oder warten auf ihren Trigger (bei Limit/Stop)

### 4.2 Order-Typen im Detail

#### 4.2.1 Market Order (Buy)

**Definition:** Sofortiger Kauf zum besten verfügbaren Ask-Preis.

**Ausführungslogik:**
1. Spieler gibt Stückzahl ein und bestätigt
2. Backend prüft: genug Cash/Buying Power? Markt offen? Aktie nicht ausgesetzt?
3. Order wird zum aktuellen Ask-Preis ausgeführt
4. Slippage-Berechnung: bei Orders, die >5% des Tagesvolumens ausmachen, wird der Preis um einen Aufschlag erhöht (Slippage-Faktor = Order-Größe / Tagesvolumen × Spread × 2)
5. Cash wird reduziert, Position wird eröffnet oder aufgestockt
6. Bestätigungsmeldung an Frontend

**Slippage-Modell (für alle Market Orders):**
```
Effektiver Preis = Ask-Preis × (1 + Slippage)
Slippage = (OrderGröße / DurchschnittsVolumen) × SpreadFaktor × 0.5
```
- Bei kleinen Orders (<1% des Volumens): Slippage ≈ 0 (vernachlässigbar)
- Bei mittleren Orders (1-5%): merklicher Aufschlag (0.1-0.5%)
- Bei großen Orders (>5%): signifikanter Aufschlag (0.5-2%)
- Bei illiquiden Aktien: Slippage ist höher (SpreadFaktor ist größer)

**Fehlerfälle:**
- Nicht genug Cash: Toast `Insufficient funds. You need $X more.` (rot)
- Markt geschlossen: Toast `Market is closed. Order will execute at market open.` (info) — Order wird als "pending market open" gespeichert
- Aktie ausgesetzt (Circuit Breaker): Toast `Trading halted for AAPL. Try again later.` (warning)
- Stückzahl = 0 oder negativ: Validierung im Frontend, Button bleibt disabled

#### 4.2.2 Market Order (Sell)

Analog zu Market Buy, aber:
- Ausführung zum Bid-Preis (nicht Ask)
- Slippage drückt den Preis nach unten (Spieler bekommt weniger)
- Kann nur ausgeführt werden, wenn der Spieler eine Long-Position in der Aktie hat
- Teilverkauf möglich: Spieler kann weniger Aktien verkaufen als er besitzt
- Wenn Stückzahl > besessene Aktien: Validierungsfehler `You only own X shares of AAPL.`
- Bei Sell All: Cash wird erhöht, Position wird geschlossen

#### 4.2.3 Limit Order (Buy)

**Definition:** Kauf nur wenn der Preis den festgelegten Limit-Preis erreicht oder unterschreitet.

**Mechanik:**
1. Spieler gibt Stückzahl und Limit-Preis ein
2. Order wird nicht sofort ausgeführt, sondern ins System eingestellt
3. Bei jedem Tick prüft das Backend: Ist der Ask-Preis ≤ Limit-Preis?
4. Wenn ja: Ausführung zum Limit-Preis (oder besser, falls der Marktpreis darunter liegt)
5. Wenn nein: Order bleibt offen

**Time-in-Force (Gültigkeitsdauer):**
- **GTC (Good Till Cancelled):** Order bleibt aktiv, bis sie ausgeführt oder manuell gecancelt wird. Kein Zeitlimit.
- **Day Order:** Order wird am Ende des aktuellen Handelstages (4:00 PM) automatisch gecancelt, wenn nicht ausgeführt. Toast-Benachrichtigung: `Day order for AAPL expired.`
- **GTD (Good Till Date):** Order bleibt aktiv bis zu einem vom Spieler gewählten Datum. Danach automatisch gecancelt.

**Teilausführungen (Partial Fills):**
Limit Orders können teilweise ausgeführt werden, wenn nicht genug Volumen am gewünschten Preis verfügbar ist. Vollständige Beschreibung in Sektion 4.3.

**UI-Besonderheiten:**
- Limit-Preis-Feld wird vorausgefüllt mit dem aktuellen Preis
- Wenn der Spieler einen Limit-Preis über dem aktuellen Ask eingibt (bei Buy): Hinweis `Your limit price is above the current ask. This will execute immediately like a market order.` in `warning`, 11px

#### 4.2.4 Limit Order (Sell)

Analog zu Limit Buy, aber:
- Ausführung wenn Bid-Preis ≥ Limit-Preis
- Warnung wenn Limit-Preis unter aktuellem Bid: `Your limit price is below the current bid. This will execute immediately.`

#### 4.2.5 Stop Order (Stop Loss)

**Definition:** Wenn der Marktpreis einen bestimmten Trigger-Preis erreicht, wird automatisch eine Market Order ausgelöst.

**Mechanik (für Sell Stop / Stop Loss auf Long-Position):**
1. Spieler setzt Stop-Preis unterhalb des aktuellen Preises
2. Bei jedem Tick: Ist der letzte Handelspreis ≤ Stop-Preis?
3. Wenn ja: eine Market Sell Order wird automatisch ausgelöst
4. Ausführung zum dann aktuellen Bid-Preis (mit Slippage)

**Mechanik (für Buy Stop auf Short-Position):**
1. Spieler setzt Stop-Preis oberhalb des aktuellen Preises
2. Bei jedem Tick: Ist der letzte Preis ≥ Stop-Preis?
3. Wenn ja: Market Buy (Cover) wird ausgelöst

**Wichtig:** Der Ausführungspreis kann vom Stop-Preis abweichen (Gap, Slippage). Der Stop-Preis ist nur der Trigger, keine Preisgarantie.

**UI-Hinweis unter dem Feld:**
`When the price reaches $XXX.XX, a market order will be placed. Actual execution price may differ.` — `text-disabled`, 11px.

#### 4.2.6 Stop-Limit Order

**Definition:** Wie Stop Order, aber statt einer Market Order wird eine Limit Order ausgelöst.

**Zwei Preisfelder:**
1. **Stop-Preis (Trigger):** Wenn dieser Preis erreicht wird, wird die Limit Order aktiviert
2. **Limit-Preis (Ausführung):** Die aktivierte Limit Order wird nur zu diesem Preis oder besser ausgeführt

**Risiko:** Wenn der Preis durch den Stop fällt und sofort unter den Limit-Preis weiterrutscht, wird die Order möglicherweise nie ausgeführt. Der Spieler muss das verstehen.

**UI-Hinweis:**
`When price hits stop ($XXX), a limit order at $YYY will be placed. Warning: If the price moves past your limit, the order may not fill.` — `warning`, 11px.

#### 4.2.7 Trailing Stop

**Definition:** Ein Stop-Preis, der sich automatisch nachzieht, wenn der Marktpreis sich zugunsten des Spielers bewegt.

**Mechanik (für Long-Position):**
1. Spieler setzt Trail-Abstand (z.B. $5.00 oder 3%)
2. Initialer Stop-Preis = aktueller Preis - Trail-Abstand
3. Bei jedem Tick: wenn der Preis steigt, steigt auch der Stop (Preis - Trail-Abstand). Der Stop bewegt sich nur nach oben, nie nach unten.
4. Wenn der Preis fällt und den Stop erreicht: Market Sell wird ausgelöst

**Beispiel:**
- Preis $100, Trail $5 → Stop bei $95
- Preis steigt auf $110 → Stop steigt auf $105
- Preis fällt auf $108 → Stop bleibt bei $105
- Preis fällt auf $105 → TRIGGER → Market Sell

**Mechanik (für Short-Position):**
Umgekehrt: Stop bewegt sich nach unten wenn der Preis fällt.

**UI:**
- Trail-Amount-Feld mit Toggle-Button `$` / `%`
- Live-Anzeige: `Current stop: $105.00 (trailing $5.00 from high of $110.00)` in `text-secondary`, 12px, aktualisiert sich in Echtzeit

#### 4.2.8 Fill-or-Kill (FOK)

**Definition:** Order muss sofort und VOLLSTÄNDIG ausgeführt werden, sonst wird sie komplett gecancelt.

**UI:** Im Time-in-Force-Dropdown: `FOK (Fill or Kill)`. Hinweis: `This order must fill completely and immediately, or it will be cancelled entirely.`

#### 4.2.9 Immediate-or-Cancel (IOC)

**Definition:** Order wird sofort ausgeführt, so viel wie möglich. Der nicht ausgeführte Rest wird gecancelt.

**UI:** Im Time-in-Force-Dropdown: `IOC (Immediate or Cancel)`.

#### 4.2.10 Market-on-Open (MOO) / Market-on-Close (MOC)

**Market-on-Open:** Order wird zum Eröffnungskurs ausgeführt. Kann jederzeit platziert werden. Besonders nützlich: im Daily Summary eine MOO-Order für den nächsten Tag platzieren.

**Market-on-Close:** Order wird zum Schlusskurs ausgeführt. Muss vor 3:50 PM platziert werden.

**UI:** Im Order-Type-Dropdown: `Market on Open`, `Market on Close`.

### 4.3 Partial Fills (Teilausführungen)

Limit Orders können teilweise ausgeführt werden, wenn nicht genug Volumen am gewünschten Preis verfügbar ist.

**Mechanik:**
1. Spieler platziert Limit Buy für 1,000 Aktien @ $50
2. Im Orderbook sind nur 400 Aktien @ ≤$50 verfügbar
3. 400 werden sofort ausgeführt, 600 bleiben als offene Limit Order
4. Toast: `Partial fill: 400 of 1,000 AAPL filled at $50.00. 600 remaining.`
5. Status im Orders-Tab: `Partially Filled (400/1,000)` mit Fortschrittsbalken

**Regeln:**
- Market Orders: IMMER vollständig (Slippage statt Partial Fill)
- Limit Orders: Partial Fills sind Standard
- FOK: keine Partial Fills (alles oder nichts)
- IOC: Partial Fill erlaubt, Rest gecancelt
- Stop Orders → werden zu Market Orders → kein Partial Fill

### 4.4 Short Selling

#### 4.4.1 Grundmechanik

Short Selling erlaubt dem Spieler, von fallenden Kursen zu profitieren. Er "leiht" sich Aktien (virtuell vom Broker-System), verkauft sie sofort, und muss sie später zurückkaufen ("covern").

**Gewinn/Verlust:**
- Gewinn = Verkaufspreis - Rückkaufpreis (× Stückzahl, minus Gebühren und Leihgebühren)
- Verlust ist theoretisch unbegrenzt (Preis kann unendlich steigen)

**Leihgebühr (Borrow Fee):**
- Pro Tag berechnet, vom Cash abgezogen
- Basis: 0.5% p.a. für leicht verfügbare Aktien (Large Cap, niedriges Short Interest)
- Erhöht sich bei hohem Short Interest:
  - Short Interest < 10%: 0.5% p.a.
  - Short Interest 10-20%: 2% p.a.
  - Short Interest 20-40%: 8% p.a.
  - Short Interest > 40%: 15-30% p.a. (schwer zu leihen)
- Anzeige im Order-Panel bei Short: `Borrow fee: ~$X.XX/day` in `warning`, 12px

**Short Interest:**
- Für jede Aktie wird getrackt, wie viel von allen Marktteilnehmern (Spieler + AI) geshortet ist
- Short Interest = Gesamt-Short-Volumen / Gesamtaktien
- Anzeige in Aktien-Detail: `Short Interest: 15.3%` — bei >20% in `warning`-Farbe

#### 4.4.2 Alternative Uptick Rule (Short Sale Price Restriction)

Short Sales unterliegen der Alternative Uptick Rule: Wenn eine Aktie an einem Tag >10% gefallen ist, dürfen Short Sales am Rest des Tages und am folgenden Tag nur auf einem **Uptick** ausgeführt werden (der Short-Verkaufspreis muss über dem aktuellen Best Bid liegen).

**Mechanik:**
- Trigger: Aktie fällt ≥10% vom Vortages-Schlusskurs
- Effekt: Short Sales nur zum Bid + $0.01 oder höher (nicht zum Bid selbst)
- Dauer: Rest des Tages + nächster Handelstag
- UI: Badge `SSR` (Short Sale Restriction) in der Aktien-Tabelle und im Aktien-Detail, Farbe `warning`
- Spieler-Impact: Short-Order wird zu einem Limit-Sell (nicht Market), automatisch auf Bid + $0.01 gesetzt
- AI-Impact: Short Seller AI reduziert ihre Aggressivität bei SSR-Aktien

**Warum:** Verhindert aggressive Bear Raids auf bereits fallende Aktien.

**Abgrenzung zum Circuit Breaker:** Die Alternative Uptick Rule (SSR) und der Circuit Breaker sind separate Mechanismen. SSR: triggert bei -10% vom Vortagesschluss, schränkt nur Short-Selling ein, Handel geht normal weiter. Circuit Breaker: triggert bei -10% innerhalb von 5 Minuten (Einzelaktie) oder -7%/-13%/-20% Tagesfall (Marktindex), setzt den gesamten Handel aus.

#### 4.4.3 Short-Position eröffnen

**UI:** Der `SHORT`-Tab im Order-Panel (amber/gelb).

**Margin-Anforderung:**
- Initial Margin: 150% des Positionswerts muss als Sicherheit vorhanden sein
- Beispiel: Short 100 Aktien @ $50 = $5,000 Positionswert → $7,500 müssen als Buying Power vorhanden sein
- Anzeige im Order-Panel: `Required margin: $7,500` und `Available: $48,000`

**Maximale Short-Menge:**
Begrenzt durch:
1. Verfügbare Margin (Buying Power / 1.5 / Preis)
2. Borrow-Verfügbarkeit (bei sehr hohem Short Interest kann es sein, dass nicht genug Aktien zum Leihen vorhanden sind)
3. Anzeige: `Max shortable: 640 shares` in `text-secondary`, 12px

#### 4.4.4 Short-Position schließen (Cover)

**UI:** Wenn eine Short-Position besteht und der Spieler den `SELL`-Tab wählt, wechselt dieser automatisch zu `COVER` (gleicher Button, anderer Text).

Alternativ: in der Position Quick View (rechte Sidebar) die `COVER ALL` / `COVER PARTIAL` Buttons.

**Cover-Mechanik:**
1. Spieler gibt Stückzahl ein (maximal = Short-Positionsgröße)
2. Market Cover: Kauf zum Ask-Preis (mit Slippage)
3. P&L wird realisiert: (Short-Eröffnungspreis - Cover-Preis) × Stückzahl - Gebühren - aufgelaufene Leihgebühren
4. Margin wird freigegeben

#### 4.4.5 Short Squeeze

Ein Short Squeeze entsteht organisch aus dem Zusammenspiel der Systeme — er wird nicht geskriptet.

**Bedingungen für einen Short Squeeze:**
1. Hohes Short Interest (>30%)
2. Positiver Katalysator (Event oder starke Kaufwelle)
3. Preis steigt schnell (>10% in kurzer Zeit)
4. Geshorte Positionen (AI und Spieler) geraten in Margin-Probleme
5. Erzwungene Eindeckung (Cover) treibt den Preis weiter hoch
6. Kaskade: noch mehr Shorts werden gezwungen einzudecken

**UI bei Short Squeeze:**
- Warnung im Aktien-Detail: Banner `⚠ SHORT SQUEEZE WARNING` in `warning`-Hintergrund, wenn Short Interest >30% UND Preis >10% gestiegen in letzter Stunde
- Breaking News Event: `Short sellers are scrambling to cover their positions in [SYMBOL] as the stock surges XX%.`
- Im Chart: ungewöhnlich hohe grüne Volumen-Bars

**Auswirkung auf den Spieler:**
Wenn der Spieler eine Short-Position in der betroffenen Aktie hat, bekommt er:
1. Toast-Warning: `Your short position in AAPL is at risk. Consider covering.`
2. Margin-Warnungen (siehe 4.4.6)
3. Eventuell erzwungene Liquidation

#### 4.4.6 Margin Call (Short)

**Berechnung:**
- Maintenance Margin: 125% des Positionswerts muss als Sicherheit vorhanden sein
- Margin Ratio = (Cash + hinterlegte Sicherheiten) / Short-Positionsmarktwert
- Warnstufen:

| Stufe | Ratio | Aktion |
|---|---|---|
| Normal | >150% | Keine Warnung |
| Warning | 130-150% | Gelbes Banner im Portfolio, Toast-Warnung |
| Margin Call | 110-130% | Rotes Banner, Popup mit Countdown (24h Spielzeit), Spiel pausiert automatisch |
| Forced Liquidation | <110% | Position wird automatisch zum Marktpreis gecovert |

**Margin Call UI:**
- Rotes Banner oben im Portfolio-Tab: `MARGIN CALL: You must reduce your short position in AAPL or deposit more funds within 24 hours (game time).`
- Countdown-Timer im Banner: `Time remaining: 18h 45m`
- Pulsierender roter Glow auf dem Portfolio-Tab in der Navigation
- Spiel pausiert automatisch bei erstmaligem Margin Call (damit der Spieler reagieren kann)

**Erzwungene Liquidation:**
- Wenn Countdown abläuft ODER Ratio unter 110% fällt (was auch immer zuerst eintritt)
- Automatischer Market Cover der gesamten Short-Position
- Toast (rot): `Forced liquidation: Your short position in AAPL was covered at $XXX.XX. Loss: -$X,XXX.XX`
- Breaking News: `[SYMBOL] shorts are being forcefully liquidated due to margin violations.` (trägt zum Squeeze bei)

### 4.5 Margin Trading (Long)

#### 4.5.1 Margin-Konto-System

Margin-Trading erlaubt dem Spieler, mit geliehenem Geld zu handeln (Hebel).

**Regeln:**
- Maximum Leverage: 2× (bei $50k Cash kann für bis zu $100k gekauft werden)
- Initial Margin Requirement: 50% (d.h. mindestens die Hälfte des Kaufpreises muss eigenes Cash sein)
- Maintenance Margin: 25% des Positionswerts
- Margin-Zinsen: **Zentralbank-Leitzins + 4%** p.a. auf den geliehenen Betrag, täglich berechnet und von Cash abgezogen. Bei einem Leitzins von 4% = 8% Margin-Zinsen. Steigt der Leitzins auf 6% = 10% Margin-Zinsen. Dies koppelt Margin-Kosten direkt an Zentralbank-Events und macht Zinsentscheidungen für gehebelte Spieler spürbar.

**Anzeige im Order-Panel (bei Buy, wenn Margin nötig):**
- `Using margin: $3,500 borrowed` in `warning`, 12px
- `Daily interest: ~$0.77` in `text-disabled`, 11px

**Buying Power Berechnung:**
```
Buying Power = Cash × 2 - Wert bestehender Margin-Positionen
```

#### 4.5.2 Margin Call (Long)

Wenn der Wert der auf Margin gekauften Positionen fällt, kann die Sicherheit unter das Minimum fallen.

**Berechnung:**
```
Equity Ratio = (Portfolio Value - Margin Schuld) / Portfolio Value
```

| Stufe | Equity Ratio | Aktion |
|---|---|---|
| Normal | >50% | Keine Warnung |
| Warning | 30-50% | Gelbes Banner |
| Margin Call | 25-30% | Rotes Banner, Countdown (24h Spielzeit), Auto-Pause |
| Forced Liquidation | <25% | Automatischer Verkauf von Positionen bis Ratio >30% |

**Erzwungene Liquidation (Long):**
- Das System verkauft die größte Margin-Position zuerst
- Wiederholt, bis die Margin-Anforderung erfüllt ist
- Toast pro Verkauf: `Margin liquidation: Sold XX shares of AAPL at $XXX.XX`

### 4.6 Pump & Dump Mechanik

Pump & Dump wird von AI-Tradern initiiert (siehe Kapitel 7) und ergibt sich aus dem System:

**Phase 1 — Akkumulation (subtil):**
- AI-Insider kauft still große Mengen einer Small-Cap-Aktie
- Volumen steigt leicht, Preis bewegt sich minimal
- Dauer: 1-3 Spieltage

**Phase 2 — Pump (laut):**
- Plötzliche Kaufwelle (AI Retail FOMO)
- Preis steigt 20-100% in kurzer Zeit
- Hohes Volumen
- Möglicherweise begleitet von einem positiven (aber irreführenden) Event
- UI-Indikator: `⚠ Unusual Activity` Badge in der Aktien-Tabelle (orange, blinkt)

**Phase 3 — Dump (schmerzhaft):**
- AI-Insider verkauft seine Position
- Preis stürzt ab
- Spieler, die spät eingestiegen sind, machen Verlust

**Warnsignale für den Spieler:**
- `Unusual Activity`-Badge (erscheint bei ungewöhnlichem Volumen-Spike + Preis-Spike bei Small Caps)
- News: `Analysts warn of unusual trading activity in [SYMBOL].`
- Hoher RSI-Wert im Chart (wenn Indikatoren aktiv)

### 4.7 Analyst Ratings & Price Targets

Analysten bewerten Aktien und setzen Kursziele. Diese Ratings beeinflussen AI-Trader-Verhalten und sind für den Spieler wichtige Informationsquellen.

**Rating-Stufen:**
| Rating | Bedeutung | Farbe |
|---|---|---|
| Strong Buy | Analysten sind überzeugt, die Aktie steigt deutlich | `green-primary` |
| Buy | Empfehlung zum Kauf | `green-primary` (heller) |
| Hold | Neutral, kein Handlungsbedarf | `text-secondary` |
| Sell | Empfehlung zum Verkauf | `red-primary` (heller) |
| Strong Sell | Analysten warnen vor deutlichem Verfall | `red-primary` |

**Price Targets:**
- Jede Aktie hat ein Konsens-Kursziel (Durchschnitt aller Analysten)
- Wird bei Generierung gesetzt (basierend auf Fair Value + 10-30% Aufschlag)
- Ändert sich nach Events (Earnings, Übernahmen, Skandale)
- Anzeige in Aktien-Detail: `Analyst Consensus: BUY | Target: $165.00 (+15.7%)` in `text-secondary`, 13px

**Rating-Änderungen als Events:**
| Event | Sentiment | Preis-Effekt |
|---|---|---|
| Upgrade (z.B. Hold → Buy) | +0.2 | +2-4% |
| Downgrade (z.B. Buy → Sell) | -0.3 | -3-6% |
| Price Target Raised | +0.1 | +1-2% |
| Price Target Cut | -0.2 | -2-4% |
| Initiierung mit Strong Buy | +0.3 | +3-5% |

**Beispiel-Headlines:**
- `Goldman Sachs upgrades [SYMBOL] to Buy, raises target to $XXX.`
- `Morgan Stanley downgrades [SYMBOL] to Sell, cuts price target to $XXX.`
- `Analyst initiates coverage on [SYMBOL] with Strong Buy rating.`

**UI in Aktien-Detail:**
Unter dem Aktien-Header, neben Bid/Ask:
```
Analyst Rating: ████████░░ BUY (4 Buy, 2 Hold, 1 Sell)
Price Target: $165.00 (+15.7% upside)
```
- Rating-Balken: farbig (Grün = Buy-Anteil, Grau = Hold, Rot = Sell)
- Hover auf Balken: Tooltip mit Aufschlüsselung der Ratings

**Frequenz:** 1-3 Rating-Änderungen pro Spieltag (über alle Aktien verteilt).

### 4.8 Insider Trading (Spielmechanik)

Gelegentlich bekommt der Spieler einen "Tip" — eine vage Vorschau auf ein kommendes Event:

**Mechanik:**
- Zufällig (ca. alle 20-40 Spieltage) erscheint eine spezielle News:
  `💬 Market Rumor: Sources suggest [COMPANY] may announce [vague hint] soon.`
- Der Hinweis ist absichtlich ungenau (z.B. "a major product announcement" statt "iPhone launch beats expectations")
- Der Spieler kann entscheiden, ob er darauf handelt
- Keine Gameplay-Konsequenzen für "Insider Trading" — es ist ein Feature, kein Vergehen
- Manchmal sind Rumors falsch (ca. 20% der Zeit) — das Event tritt nicht ein oder fällt anders aus

**UI:**
- Rumors erscheinen als eigene Kategorie im News-Feed mit `💬`-Icon und `Rumor`-Badge
- Farbe: `text-accent` (blau) statt grün/rot — neutral, weil ungewiss

### 4.9 Order-Ausführungs-Engine

**Matching-Logik:**
Das Backend unterhält ein simuliertes Orderbook pro Aktie (generiert durch AI-Trader). Spieler-Orders werden gegen dieses Orderbook gematched:

1. **Market Order:** Wird sofort zum besten verfügbaren Preis ausgeführt (Ask für Buy, Bid für Sell)
2. **Limit Order:** Wird ins interne "Spieler-Orderbuch" eingestellt und bei jedem Tick gegen den Marktpreis geprüft
3. **Stop/Trailing Stop:** Trigger wird bei jedem Tick geprüft, bei Auslösung wird Market/Limit Order generiert

**Preis-Priorität:** Spieler-Orders haben keine Priorität gegenüber AI-Orders. Der Marktpreis wird zuerst durch AI-Aktivität bestimmt, und der Spieler handelt zu diesem Preis (mit Slippage bei großen Orders).

**Ausführungsbestätigung (WebSocket Message von Backend an Frontend):**
```json
{
  "type": "OrderFilled",
  "orderId": "ORD-12345",
  "symbol": "AAPL",
  "side": "BUY",
  "quantity": 50,
  "filledPrice": 142.63,
  "totalCost": 7131.50,
  "commission": 4.95,
  "timestamp": "2027-03-15T14:32:15"
}
```

**Latenz-Simulation:** Keine. Orders werden im gleichen Tick ausgeführt, in dem sie empfangen werden (Spielzeit). Latenz-Simulation (als Realismus-Feature) ist ein Stretch Goal.

### 4.10 Order-Management-UI (Orders-Tab)

Der Orders-Tab im Zentralbereich zeigt zwei Untertabs:

**Sub-Tabs (oben, kleiner als Haupt-Tabs):**
- `Open Orders` — alle aktiven, noch nicht ausgeführten Orders
- `Order History` — alle abgeschlossenen und gecancelten Orders

#### 4.10.1 Open Orders

**Tabelle:**

| Spalte | Breite | Inhalt |
|---|---|---|
| Order ID | 100px | `ORD-12345`, `JetBrains Mono`, 12px, `text-disabled` |
| Type | 80px | `Market`, `Limit`, etc. — Badge-Stil, farbig |
| Symbol | 80px | `AAPL`, `JetBrains Mono Bold`, 13px, klickbar |
| Side | 60px | `BUY` (grün), `SELL` (rot), `SHORT` (amber), `COVER` (blau) |
| Quantity | 80px | `50`, rechts ausgerichtet |
| Price | 100px | Limit-/Stop-Preis oder `Market` in kursiv |
| Status | 100px | `Pending`, `Partially Filled`, `Triggered` — mit farbigem Dot |
| Created | 120px | `Mar 15, 2:32 PM`, `text-secondary` |
| Actions | 80px | `Cancel`-Button (klein, rot), `Edit`-Icon |

**Cancel-Button:**
- Klein, Text `Cancel`, `Inter`, 11px, `red-primary`, kein Hintergrund, Border 1px `red-primary`
- Hover: Hintergrund `red-dim`
- Klick: sofortige Cancellation (keine Bestätigung nötig), Zeile verschwindet mit Fade-Out

**Edit (Modify Order):**
- Icon `edit`, 16px, `text-secondary`
- Klick: öffnet ein kleines Inline-Formular in der Zeile, in dem Preis und Menge geändert werden können
- `Save` und `Cancel` Buttons im Inline-Formular

**Leerzustand:** `You have no open orders.` zentriert, `text-disabled`.

#### 4.10.2 Order-Historie

Gleiche Tabelle wie Open Orders, aber:
- Zusätzliche Spalte: `Filled Price` (tatsächlicher Ausführungspreis)
- Zusätzliche Spalte: `P&L` (bei geschlossenen Roundtrips, sonst leer)
- Status-Spalte zeigt: `Filled` (grüner Dot), `Cancelled` (grauer Dot), `Expired` (gelber Dot), `Rejected` (roter Dot)
- Keine Actions-Spalte
- Sortierung: neueste oben
- Filter oben: Zeitraum-Dropdown (`Today`, `This Week`, `This Month`, `All Time`), Symbol-Suche, Side-Filter

---

## 5. Marktsimulation

### 5.1 Preis-Engine Übersicht

Alle Preise werden im C#-Backend berechnet. Das Frontend zeigt nur an, was das Backend liefert — es findet keine Preislogik im Frontend statt.

**Tick-basiertes System:**
Die Simulation arbeitet in diskreten Ticks. Pro Tick wird für jede Aktie ein neuer Preis berechnet. Die Tick-Frequenz hängt von der Spielgeschwindigkeit ab (siehe Kapitel 10).

**Ein Tick umfasst (in dieser Reihenfolge):**
1. Spielzeit voranschreiten
2. Event-Queue prüfen (neue Events auslösen)
3. AI-Trader-Entscheidungen treffen (neue AI-Orders generieren)
4. Alle Orders matchen (AI + Spieler)
5. Neue Preise berechnen (basierend auf ausgeführten Orders + Modell)
6. Margin-Checks durchführen
7. Update-Paket an Frontend senden

### 5.2 Preisberechnung

#### 5.2.1 Basis-Preismodell

Jede Aktie hat eine Preisbewegung pro Tick, die sich aus mehreren Komponenten zusammensetzt:

```
NeuerPreis = AlterPreis × (1 + Drift + Zufall + OrderImpact + EventImpact + SektorKorrelation)
```

**Komponenten:**

**Drift (Trend):**
- Langfristiger Trend der Aktie (z.B. +0.001% pro Tick für Wachstumsaktien)
- Definiert durch die Aktien-Persönlichkeit (siehe 11.3.4)
- Blue Chips: leicht positiver Drift
- Spekulative Aktien: kein oder negativer Drift
- Wird beeinflusst durch globale Marktphase (Bull/Bear)

**Zufall (Random Walk):**
- Basis: Geometric Brownian Motion
- `Zufall = Volatilität × NormalverteilterZufall × √TickDauer`
- Volatilität ist pro Aktie definiert und kann sich dynamisch ändern (z.B. nach Events)
- Der Zufall ist der Haupttreiber kurzfristiger Preisbewegungen

**Order-Impact:** (siehe 5.2.2)
**Event-Impact:** (siehe Kapitel 8)
**Sektor-Korrelation:** (siehe 5.6)

**Mean Reversion:**
- Aktien tendieren dazu, zu einem "fairen Wert" zurückzukehren
- Fairer Wert wird aus Fundamentaldaten berechnet (KGV, Gewinnwachstum)
- Wenn der Preis >20% über dem fairen Wert liegt: leichter negativer Drift-Bonus
- Wenn der Preis >20% unter dem fairen Wert liegt: leichter positiver Drift-Bonus
- Stärke der Mean Reversion: schwach (0.001-0.01% pro Tick) — schließt extreme Abweichungen nicht schnell, aber verhindert dauerhaftes Abdriften

#### 5.2.2 Order-Impact

Kauf- und Verkaufsorders beeinflussen den Preis:

```
OrderImpact = Vorzeichen × (OrderVolumen / DurchschnittsTickVolumen) × ImpactFaktor
```

- Vorzeichen: +1 für Käufe, -1 für Verkäufe
- ImpactFaktor: 0.01 (1% Preisveränderung bei einem Order, das dem gesamten Durchschnittsvolumen eines Ticks entspricht)
- Kleine Orders (<0.1% des Volumens): vernachlässigbarer Impact
- Große Orders: spürbarer Impact, der den Preis bewegt

**Temporärer vs. permanenter Impact:**
- 70% des Impacts ist temporär und klingt über die nächsten 10 Ticks ab
- 30% ist permanent (informationsbasiert — der Markt "lernt" vom Trade)

#### 5.2.3 Bid-Ask-Spread

Jede Aktie hat einen Bid- und einen Ask-Preis. Der Spread ist die Differenz.

**Spread-Berechnung:**
```
Spread = BasisSpread × VolatilitätsFaktor × (1 / LiquiditätsFaktor)
```

- **BasisSpread:** 0.05% des Preises für Large Caps, 0.2% für Mid Caps, 0.5-2% für Small Caps
- **VolatilitätsFaktor:** steigt bei hoher Volatilität (1.0 normal, bis zu 3.0 in Krisen)
- **LiquiditätsFaktor:** fällt bei niedrigem Volumen (1.0 normal, bis zu 0.2 bei extrem niedriger Liquidität)

**Spread-Anzeige in der UI (Aktien-Detail):**
```
Bid: $142.45    Ask: $142.63    Spread: $0.18 (0.13%)
```
- Bid in `green-primary`, Ask in `red-primary`, Spread in `text-secondary`
- Unter dem Preis im Aktien-Header, `JetBrains Mono`, 12px

#### 5.2.4 Volatilität

**Historische Volatilität:**
- Berechnet aus den letzten 20 Handelstagen (Standard-Deviation der täglichen Returns, annualisiert)
- Anzeige in Aktien-Detail: `Volatility (20d): 32.4%`
- Niedrig (<15%): stabile Blue-Chip-Aktie
- Mittel (15-40%): normale Aktie
- Hoch (>40%): spekulative Aktie oder Krisenzeit

**Volatilitäts-Regimes (globaler Markt):**
| Regime | VIX-Äquivalent | Beschreibung | Auswirkung |
|---|---|---|---|
| Calm | <15 | Ruhiger Markt | Kleine Preisbewegungen, enge Spreads |
| Normal | 15-25 | Normaler Markt | Standard-Preisbewegungen |
| Elevated | 25-35 | Erhöhte Nervosität | Größere Schwankungen, breitere Spreads |
| Crisis | >35 | Panik / Crash | Extreme Bewegungen, sehr breite Spreads, AI verkauft aggressiv |

Das Regime ändert sich durch Events (z.B. Zentralbank-Entscheidung kann von Normal zu Elevated wechseln) und durch Preis-Momentum (wenn der Index 5%+ an einem Tag fällt → Crisis).

### 5.3 Simuliertes Orderbook

Jede Aktie hat ein simuliertes Orderbook, das von AI Market Makers generiert wird.

**Tiefe:** 10 Levels auf jeder Seite (10 Bid-Levels, 10 Ask-Levels).

**Generierung:**
- Market Maker AI stellt Bid- und Ask-Orders ein (siehe Kapitel 7.2.1)
- Jedes Level: Preis + aggregierte Menge
- Bid-Level 1 = höchster Bid (nah am Midpoint), Level 10 = niedrigster Bid
- Ask-Level 1 = niedrigster Ask (nah am Midpoint), Level 10 = höchster Ask
- Die Abstände zwischen Levels sind nicht gleichmäßig — enger am Midpoint, weiter außen

**Update-Frequenz:** Bei jedem Tick wird das Orderbook neu berechnet. Bei hoher Spielgeschwindigkeit wird nur Level 1 (Best Bid/Ask) aktualisiert, um Performance zu sparen — die tiefen Levels ändern sich seltener.

**Orderbook-UI:** Siehe Kapitel 12.3.

### 5.4 Volumen-Simulation

**Tagesvolumen-Profil:**
Das Handelsvolumen folgt einer U-Kurve über den Tag:
- 9:30-10:30 AM: hohes Volumen (30% des Tagesvolumens) — Eröffnungstrades
- 10:30 AM - 2:00 PM: niedriges Volumen (25%) — Mittagsflaute
- 2:00-3:00 PM: steigendes Volumen (15%)
- 3:00-4:00 PM: hohes Volumen (30%) — Schlusskurse

**Volumen-Variation:**
- Basis-Tagesvolumen ist pro Aktie definiert (abhängig von Marktkapitalisierung)
- Tagesvariation: ±30% zufällig
- Event-Tage: 2-5× normales Volumen
- Earnings-Tage: 3-8× normales Volumen

### 5.5 Markt-Öffnungszeiten

**Handelszeiten:** 9:30 AM - 4:00 PM (Spielzeit), Montag bis Freitag.

**Extended Hours Trading (Pre-Market & After-Hours):**

| Session | Uhrzeit (Spielzeit) | Beschreibung |
|---|---|---|
| Pre-Market | 7:00 - 9:30 AM | Eingeschränkter Handel vor Börseneröffnung |
| Regular Hours | 9:30 AM - 4:00 PM | Normaler Handel |
| After-Hours | 4:00 - 8:00 PM | Eingeschränkter Handel nach Börsenschluss |
| Closed | 8:00 PM - 7:00 AM | Kein Handel |

**Extended-Hours-Einschränkungen:**
- Nur **Limit Orders** erlaubt (keine Market Orders — zu riskant bei dünner Liquidität)
- Volumen: ~5-10% des regulären Tagesvolumens (sehr dünn)
- Spreads: 3-5× breiter als während Regular Hours
- Slippage: deutlich höher
- Nur die liquidesten Aktien handelbar (Liquidity Score ≥ 5, ca. Top 30% der Aktien)
- Illiquide Aktien: `Extended hours trading not available for [SYMBOL].`

**Warum ist das wichtig?**
Earnings werden typischerweise nach Market Close (4:00 PM) oder vor Market Open (7:00 AM) veröffentlicht. Ohne Extended Hours kann der Spieler NICHT auf Earnings reagieren, bevor der Preis am nächsten Tag mit einem Gap öffnet. Extended Hours geben dem Spieler die Chance, sofort zu handeln — aber zu schlechteren Konditionen (breiter Spread, dünne Liquidität).

**UI-Indikator:** In der Top Bar neben der Uhrzeit:
- `Pre-Market` (blauer Badge) — während 7:00-9:30
- `Market Open` (grüner Badge) — während 9:30-16:00
- `After-Hours` (orangener Badge) — während 16:00-20:00
- `Market Closed` (grauer Badge) — während 20:00-7:00

**Events außerhalb der Handelszeiten:**
- Events können jederzeit auftreten (auch nachts, Wochenende)
- Wenn ein Event während Extended Hours auftritt: Spieler kann sofort handeln (mit Einschränkungen)
- Wenn ein Event während Closed auftritt: Preis reagiert erst in Pre-Market oder bei Market Open (Gap)

**Market Open:**
- Eröffnungspreis = Schlusskurs vom Vortag ± Overnight-Veränderung
- Overnight-Veränderung: basiert auf Events, die während der geschlossenen Zeit aufgetreten sind
- Gap Up/Gap Down: wenn ein starkes Event über Nacht auftritt, öffnet die Aktie deutlich höher/niedriger als der Schlusskurs
- UI: kurze Animation des Preis-Sprungs beim Open

**Market Close:**
- Letzter Tick des Tages bei 4:00 PM
- Closing Auction: leicht erhöhtes Volumen in den letzten 5 Minuten
- Schlusskurs wird als "Previous Close" für den nächsten Tag gespeichert
- UI: `Market Closed`-Indikator in Top Bar

**Wochenenden:**
- Samstag und Sonntag — kein Handel
- Spielzeit kann vorgedrückt werden (Speed 10x) — es passiert wenig, aber Events können auftreten
- Optional: automatisches Überspringen von Wochenenden (konfigurierbar in Settings)
- Wenn nicht übersprungen: "Weekend Summary" News am Sonntag Abend mit Vorschau auf die kommende Woche

**Feiertage:**
Im MVP werden keine Feiertage simuliert (zu komplex, geringer Mehrwert). Stretch Goal für Phase 2.

### 5.6 Korrelationen

**Sektor-Korrelationen:**
Aktien innerhalb eines Sektors bewegen sich ähnlich. Wenn eine Tech-Aktie steigt, steigen andere Tech-Aktien tendenziell auch (nicht garantiert, aber wahrscheinlicher).

**Korrelationsmatrix (Beispiel-Werte, 0 = keine Korrelation, 1 = perfekte Korrelation):**

| | Tech | Energy | Finance | Health | Consumer |
|---|---|---|---|---|---|
| Tech | 1.0 | 0.1 | 0.3 | 0.2 | 0.3 |
| Energy | 0.1 | 1.0 | 0.2 | 0.0 | 0.1 |
| Finance | 0.3 | 0.2 | 1.0 | 0.1 | 0.3 |
| Health | 0.2 | 0.0 | 0.1 | 1.0 | 0.2 |
| Consumer | 0.3 | 0.1 | 0.3 | 0.2 | 1.0 |

**Vollständige 12×12 Sektor-Korrelationsmatrix:**
(Werte: 0.0 = keine Korrelation, 1.0 = perfekte Korrelation. Werte gelten im Normal-Regime. Im Crisis-Regime steigen alle Werte Richtung 0.8-1.0.)

|  | Tech | Energy | Fin | Health | Cons | Ind | Mat | Real Est | Telco | Util | Luxury | Trans |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| **Tech** | 1.0 | 0.10 | 0.30 | 0.20 | 0.25 | 0.20 | 0.10 | 0.15 | 0.40 | 0.05 | 0.25 | 0.15 |
| **Energy** | 0.10 | 1.0 | 0.20 | 0.05 | 0.10 | 0.35 | 0.45 | 0.10 | 0.05 | 0.30 | 0.10 | 0.40 |
| **Fin** | 0.30 | 0.20 | 1.0 | 0.10 | 0.30 | 0.25 | 0.15 | 0.50 | 0.15 | 0.20 | 0.30 | 0.20 |
| **Health** | 0.20 | 0.05 | 0.10 | 1.0 | 0.20 | 0.10 | 0.10 | 0.05 | 0.10 | 0.15 | 0.15 | 0.10 |
| **Cons** | 0.25 | 0.10 | 0.30 | 0.20 | 1.0 | 0.20 | 0.15 | 0.25 | 0.20 | 0.15 | 0.55 | 0.20 |
| **Ind** | 0.20 | 0.35 | 0.25 | 0.10 | 0.20 | 1.0 | 0.50 | 0.20 | 0.10 | 0.15 | 0.15 | 0.45 |
| **Mat** | 0.10 | 0.45 | 0.15 | 0.10 | 0.15 | 0.50 | 1.0 | 0.15 | 0.05 | 0.10 | 0.10 | 0.30 |
| **Real Est** | 0.15 | 0.10 | 0.50 | 0.05 | 0.25 | 0.20 | 0.15 | 1.0 | 0.10 | 0.25 | 0.20 | 0.10 |
| **Telco** | 0.40 | 0.05 | 0.15 | 0.10 | 0.20 | 0.10 | 0.05 | 0.10 | 1.0 | 0.20 | 0.15 | 0.10 |
| **Util** | 0.05 | 0.30 | 0.20 | 0.15 | 0.15 | 0.15 | 0.10 | 0.25 | 0.20 | 1.0 | 0.05 | 0.15 |
| **Luxury** | 0.25 | 0.10 | 0.30 | 0.15 | 0.55 | 0.15 | 0.10 | 0.20 | 0.15 | 0.05 | 1.0 | 0.15 |
| **Trans** | 0.15 | 0.40 | 0.20 | 0.10 | 0.20 | 0.45 | 0.30 | 0.10 | 0.10 | 0.15 | 0.15 | 1.0 |

**Wichtige Korrelationspaare:**
- Höchste: Consumer ↔ Luxury (0.55), Industrials ↔ Materials (0.50), Financials ↔ Real Estate (0.50)
- Niedrigste: Tech ↔ Utilities (0.05), Energy ↔ Healthcare (0.05), Materials ↔ Telco (0.05)
- Diese Werte spiegeln die reale Wirtschaft: Luxusgüter hängen am Konsum, Industrie braucht Materialien, Immobilien sind zinsabhängig (Finanzen).

**Implementierung:**
Bei jedem Tick wird ein "Sektor-Return" berechnet. Einzelne Aktien im Sektor bekommen einen Korrelationsanteil dieses Returns:
```
AktienReturn += SektorReturn × IntraSektorKorrelation × ZufallsFaktor
AktienReturn += AndereSektorReturns × InterSektorKorrelation × ZufallsFaktor
```

**Krisenkorrelation:**
In Krisen (Volatilitäts-Regime "Crisis") steigen alle Korrelationen Richtung 1.0 — alles fällt zusammen. Dies simuliert den realen Effekt, dass in Panik Diversifikation weniger hilft.

### 5.7 Marktindizes

**Gesamtmarkt-Index:**
- Name: `MARKET` (oder ein generierter Name wie "StockSim Composite")
- Berechnung: marktkapitalisierungsgewichteter Durchschnitt aller Aktien
- Startwert: 10,000 Punkte
- Anzeige: Top Bar (Markt-Zusammenfassung), Dashboard (Index-Chart)

**Sektor-Indizes:**
- Einer pro Sektor (z.B. `TECH-IDX`, `ENRG-IDX`)
- Berechnung: marktkapitalisierungsgewichteter Durchschnitt aller Aktien im Sektor
- Startwert: 1,000 Punkte
- Anzeige: Sektor-Panel in linker Sidebar, Top Bar

**Index-Nutzung:**
- Performance-Benchmark (Analytics: "Your portfolio vs. Market Index")
- Volatilitäts-Regime wird vom Markt-Index abgeleitet
- AI-Trader-Sentiment basiert teilweise auf Index-Trend

### 5.8 Float, Shares Outstanding & Ownership

**Shares Outstanding vs. Float — kritische Unterscheidung:**

| Begriff | Definition |
|---|---|
| **Shares Outstanding** | Gesamtanzahl aller ausgegebenen Aktien |
| **Float** | Aktien die frei am Markt handelbar sind (Outstanding - gesperrte Aktien) |
| **Locked Shares** | Aktien die NICHT handelbar sind: Insider-Besitz, Institutionelle Langzeithalter, Lock-Up-Perioden |

**Warum ist das kritisch?**
- Short Interest wird als % des FLOATS berechnet, nicht der Outstanding Shares
- Wenn eine Aktie 100M Outstanding hat aber nur 30M Float, und 15M sind geshortet: Short Interest = 50% des Floats (nicht 15%)
- Das macht Short Squeezes realistischer — weniger verfügbare Aktien = schnellere Squeeze-Dynamik
- Der Spieler kann den Float beeinflussen: wenn er >5% hält, reduziert sich der effektive Float

**Generierung bei Spielstart:**
```
ShareStructure {
  SharesOutstanding: int — 10M bis 5B (abhängig von MarketCap)
  InsiderOwnership: float — 5-30% (Gründer, Management)
  InstitutionalOwnership: float — 20-70% (Fonds, Pensionskassen)
  Float: int — Outstanding × (1 - InsiderOwnership × LockupFactor)
  FloatPercentage: float — Float / Outstanding (typisch 40-85%)
}
```

**Anzeige in Aktien-Detail:**
```
Shares Outstanding: 150.2M    Float: 98.5M (65.6%)
Insider Ownership: 18.3%     Institutional: 52.1%
Short Interest: 15.3M (15.5% of float)
```

**Dynamische Änderungen:**
- Nach IPO: Float ist anfangs niedrig (Lock-Up), steigt nach Lock-Up-Expiration
- Nach Secondary Offering: Outstanding steigt, Float steigt
- Nach Buyback: Outstanding sinkt, Float sinkt
- Spieler kauft >5%: effektiver Float sinkt (Spieler hält langfristig = quasi locked)

### 5.9 Wirtschaftszyklus-System (Economic Cycles)

Der Markt durchläuft realistische Wirtschaftszyklen. Nicht zufällig, sondern als System mit Ursache und Wirkung.

**Vier Phasen des Zyklus:**

```
         ┌─── EXPANSION ───┐
        ╱                    ╲
   RECOVERY                  PEAK
        ╲                    ╱
         └── CONTRACTION ──┘
```

| Phase | Dauer (Spielzeit) | Merkmale |
|---|---|---|
| **Expansion** | 90-300 Tage | Steigende Unternehmensgewinne, niedrige Arbeitslosigkeit, moderate Inflation, Marktindex steigt stetig |
| **Peak** | 20-60 Tage | Überhitzung, hohe Bewertungen (KGVs steigen), Inflation steigt, Zentralbank erhöht Zinsen, Euphorie |
| **Contraction** | 60-180 Tage | Sinkende Gewinne, steigende Arbeitslosigkeit, fallende Preise, Panik, hohe Volatilität |
| **Recovery** | 60-120 Tage | Zinssenkungen, Stabilisierung, vorsichtiger Optimismus, Bodenbildung |

**Zykluslänge:** Ein vollständiger Zyklus dauert 230-660 Spieltage (ca. 1-3 Spieljahre). Die Länge ist variabel (Zufall + Events).

**Mechanik — was den Zyklus treibt:**

**Expansion → Peak:**
- Trigger: Inflation steigt über 4%, Zentralbank beginnt Zinsen zu erhöhen
- Effekte: Bewertungen (KGV) werden gestreckt, Volatilität steigt langsam, Spekulation nimmt zu
- AI: Hedge Funds erhöhen Leverage, Retail-FOMO auf Maximum

**Peak → Contraction:**
- Trigger: Kombination aus: Zinserhöhung + negatives Macro-Event + übertriebene Bewertungen
- Kann auch durch einen Black-Swan-Event ausgelöst werden (Crash, Pandemie, Bankenkrise)
- Effekte: schneller Preisverfall, Volatilitäts-Regime wechselt zu "Crisis", Liquidität sinkt
- AI: Hedge Funds de-leveragen, Retail verkauft in Panik, Pensionsfonds kaufen den Dip

**Contraction → Recovery:**
- Trigger: Zentralbank senkt Zinsen aggressiv + Preise fallen unter fairen Wert + Volatilität sinkt
- Effekte: langsame Stabilisierung, Volumen sinkt (Apathie), einzelne Sektoren erholen sich zuerst
- AI: Value-Investoren und Sovereign Wealth kaufen, Retail bleibt fern

**Recovery → Expansion:**
- Trigger: positive Earnings-Überraschungen + sinkende Arbeitslosigkeit + steigende Konsumausgaben
- Effekte: breite Markterholung, Optimismus kehrt zurück, Zuflüsse in Aktienfonds
- AI: alle Typen beginnen wieder zu kaufen, Volumen steigt

**Sektor-Rotation innerhalb des Zyklus:**
| Phase | Starke Sektoren | Schwache Sektoren |
|---|---|---|
| Frühe Expansion | Technology, Consumer, Financials | Utilities, Healthcare |
| Späte Expansion | Energy, Materials, Industrials | Consumer, Technology |
| Contraction | Utilities, Healthcare, Consumer Staples | Technology, Financials, Real Estate |
| Recovery | Financials, Real Estate, Industrials | Energy, Utilities |

**Anzeige für den Spieler:**
- Kein explizites "Du bist in Phase X" Label — der Spieler muss die Signale selbst erkennen
- Indirekte Hinweise: Zinsentscheidungen (News), Inflationsdaten (News), Marktindex-Trend (Chart), Sektor-Performance (Heatmap)
- Im Analytics-Tab: `Market Cycle Indicator` — ein einfacher Indikator basierend auf Index-Momentum + Volatilität + Zinsniveau:
  - Grün-Pfeil-Hoch: "Expansion"
  - Gelb-Warnung: "Overheating" (Peak)
  - Rot-Pfeil-Runter: "Contraction"
  - Blau-Waagerecht: "Recovery"
  - Dies ist ein HINWEIS, keine Garantie — wie reale Wirtschaftsindikatoren kann er falsch liegen

**Startphase:**
Bei Spielbeginn befindet sich der Markt in einer zufälligen Phase (durch Seed bestimmt):
- 40% Expansion (am häufigsten, da am längsten)
- 25% Recovery
- 20% Peak
- 15% Contraction

### 5.10 Liquidität und Markttiefe

**Liquiditäts-Score pro Aktie (1-10):**

| Score | Typ | Durchschnittsvolumen | Spread | Slippage |
|---|---|---|---|---|
| 9-10 | Mega Cap | >10M Aktien/Tag | 0.01-0.05% | Vernachlässigbar |
| 7-8 | Large Cap | 1-10M | 0.05-0.1% | Gering |
| 5-6 | Mid Cap | 100K-1M | 0.1-0.3% | Merkbar |
| 3-4 | Small Cap | 10K-100K | 0.3-1% | Signifikant |
| 1-2 | Micro Cap | <10K | 1-5% | Sehr hoch |

**Auswirkungen niedriger Liquidität:**
- Breiterer Spread (teurer zu handeln)
- Höhere Slippage bei Market Orders
- Dünneres Orderbook (weniger Depth)
- Market Orders könnten nicht vollständig ausgeführt werden bei sehr kleinen Aktien
- Preis-Impact ist größer (eine mittlere Order kann den Preis deutlich bewegen)

### 5.11 Abwesenheit von Derivaten (Options)

Im MVP und bis Phase 3 existieren keine Options (Calls/Puts) im Spiel. In der Realität beeinflusst der Optionsmarkt die Aktienkurse erheblich (Gamma Squeezes, Delta Hedging, Implied Volatility). StockSim kompensiert dieses Fehlen durch:
- Erhöhte Basis-Volatilität (simuliert den Effekt von Options-Market-Makern)
- AI-Algo-Trader die Mean-Reversion-Strategien fahren (simuliert Delta-Hedging-Effekte)
- Das Volatilitäts-Regime-System (simuliert VIX-ähnliche Dynamiken ohne echten VIX)

Options sind als Phase 4 Stretch Goal geplant.

---

## 6. Portfolio Management

### 6.1 Portfolio-Dashboard-UI (Portfolio-Tab)

Das Portfolio-Dashboard füllt den gesamten Zentralbereich wenn der `Portfolio`-Tab aktiv ist.

**Layout (von oben nach unten):**

#### 6.1.1 Gesamtübersicht-Karte (obere 100px)

Eine breite Karte über die volle Breite mit den wichtigsten Kennzahlen:

```
┌──────────────────────────────────────────────────────────────┐
│  Total Value          Day P&L           Total P&L            │
│  $62,456.78           +$1,234.56        +$12,456.78          │
│                       (+2.01%)          (+24.91%)            │
│                                                              │
│  Cash: $15,230.44    Invested: $47,226.34    Margin: $0.00   │
│  Buying Power: $30,460.88                                    │
└──────────────────────────────────────────────────────────────┘
```

- **Total Value:** `JetBrains Mono Bold`, 28px, `text-primary`. = Cash + Marktwert aller Positionen - Margin-Schulden
- **Day P&L:** `JetBrains Mono Bold`, 20px, farbig. Veränderung des Gesamtwerts seit Market Open heute.
- **Total P&L:** `JetBrains Mono Bold`, 20px, farbig. Gesamtgewinn/-verlust seit Spielbeginn.
- Untere Zeile: `JetBrains Mono`, 13px, `text-secondary`
  - Cash: verfügbares Bargeld
  - Invested: Marktwert aller Positionen
  - Margin: aktuell geliehener Betrag (0 wenn kein Margin genutzt)
  - Buying Power: maximaler Betrag, der für Käufe verfügbar ist

Hintergrund der Karte: `bg-secondary`. Border: 1px solid `border`. Border-Radius: 8px. Padding: 20px.

Wenn Day P&L positiv: ein subtiler grüner Glow am oberen Rand der Karte. Wenn negativ: roter Glow.

#### 6.1.2 Positionen-Tabelle (mittlere 50%)

**Titel:** `Open Positions` in `Inter SemiBold`, 16px, mit Anzahl `(8)` in `text-secondary`

**Tabelle:**

| Spalte | Breite | Inhalt | Ausrichtung |
|---|---|---|---|
| Symbol | 80px | `AAPL`, `JetBrains Mono Bold`, 13px, klickbar (`text-accent` bei Hover) | links |
| Name | 150px (flex) | `Apple Inc.`, truncated | links |
| Type | 60px | `LONG` (grün Badge) oder `SHORT` (amber Badge), 11px | links |
| Qty | 70px | `50`, `JetBrains Mono` | rechts |
| Avg Price | 100px | `$138.20`, `JetBrains Mono` | rechts |
| Current | 100px | `$142.58`, blinkt bei Änderung | rechts |
| Market Value | 110px | `$7,129.00` | rechts |
| P&L ($) | 100px | `+$219.00`, farbig | rechts |
| P&L (%) | 80px | `+3.17%`, farbig | rechts |
| Weight | 60px | `15.1%`, `text-secondary` | rechts |
| Actions | 70px | Sell/Cover Button (klein) | center |

**Zeilen-Details:**
- Hover: Hintergrund `bg-tertiary`
- Klick auf Symbol: öffnet Aktien-Detail
- Klick auf Sell/Cover-Button: fokussiert Order-Panel mit vorausgefüllten Daten
- Sortierung: Standard nach P&L(%) absteigend. Alle Spalten sortierbar.
- Positive P&L-Zeilen: kein spezieller Hintergrund (nur farbige Zahlen)
- Negative P&L-Zeilen: kein spezieller Hintergrund (nur farbige Zahlen)

**Leerzustand:** `You have no open positions. Start trading to build your portfolio.` zentriert, `text-disabled`, 14px. Darunter ein Button `Go to Market` in `text-accent`.

#### 6.1.3 Portfolio-Chart (untere 25%, links)

- Linienchart des Gesamtportfolio-Werts über die Zeit
- X-Achse: Zeit, Y-Achse: Wert in $
- Zeitrahmen-Buttons: `1D`, `1W`, `1M`, `3M`, `6M`, `1Y`, `All`
- Linie: `text-accent` wenn Gesamtperformance positiv, `red-primary` wenn negativ
- Füllung unter der Linie: entsprechende Glow-Farbe
- Startpunkt (Spielbeginn): horizontale gestrichelte Linie als Referenz
- Hover: Crosshair mit Tooltip (Datum + Wert)

#### 6.1.4 Allokations-Übersicht (untere 25%, rechts)

**Donut-Chart:**
- Jeder Sektor eine eigene Farbe (vordefinierte Palette, NICHT grün/rot):
  - Technology: `#3B82F6` (Blau)
  - Energy: `#F59E0B` (Amber)
  - Financials: `#8B5CF6` (Lila)
  - Healthcare: `#EC4899` (Pink)
  - Consumer: `#10B981` (Smaragd)
  - Industrials: `#6B7280` (Grau)
  - Materials: `#F97316` (Orange)
  - Real Estate: `#14B8A6` (Teal)
  - Telecom: `#6366F1` (Indigo)
  - Utilities: `#84CC16` (Lime)
  - Luxury: `#D946EF` (Fuchsia)
  - Transport: `#0EA5E9` (Sky)
- Loch in der Mitte: zeigt `Cash: 24.4%` (`JetBrains Mono`, 14px)
- Cash-Anteil als eigenes Segment (Farbe: `#374151`, Grau)
- Legende rechts neben dem Donut: Sektorname + Farb-Dot + Prozent + Betrag
- Hover auf Segment: Segment wird leicht herausgehoben, Tooltip mit Details

### 6.2 P&L-Berechnung

**Unrealized P&L (offene Positionen):**
- Long: (Aktueller Preis - Durchschnittskaufpreis) × Stückzahl
- Short: (Short-Eröffnungspreis - Aktueller Preis) × Stückzahl
- Beides abzüglich aufgelaufener Gebühren (Margin-Zinsen, Borrow Fees)
- Aktualisiert sich in Echtzeit mit jedem Preis-Tick

**Realized P&L (geschlossene Trades):**
- Berechnet bei Positionsschließung (Sell, Cover)
- Long: (Verkaufspreis - Kaufpreis) × Stückzahl - Kommissionen (Kauf + Verkauf)
- Short: (Short-Preis - Cover-Preis) × Stückzahl - Kommissionen - Leihgebühren
- Wird kumuliert gespeichert

**Day P&L:**
- Summe der Veränderungen aller Positionen seit Market Open
- = Σ (Aktueller Preis - Preis bei Market Open) × Stückzahl (für alle Long-Positionen)
- + Σ (Preis bei Market Open - Aktueller Preis) × Stückzahl (für alle Short-Positionen)
- + Realized P&L von heute

### 6.3 Trade-Historie

Zugänglich über den Orders-Tab → Order History Sub-Tab (siehe 4.10.2) oder über den Analytics-Tab.

**Zusätzliche Zusammenfassungs-Statistiken (oben in der Trade-Historie):**
- Total Trades: `211`
- Winning Trades: `142 (67.3%)`
- Losing Trades: `69 (32.7%)`
- Average Win: `+$164.50`
- Average Loss: `-$98.20`
- Largest Win: `+$2,340 (AAPL)`
- Largest Loss: `-$890 (TSLA)`
- Profit Factor: `2.68` (Gesamt-Gewinne / Gesamt-Verluste)

### 6.4 Trading Journal / Notizen

**Funktion:** Der Spieler kann Notizen zu Aktien, Trades und Strategien festhalten.

**Zugang:** Im Analytics-Tab als Sub-Tab `Journal`, oder über das Kontextmenü einer Aktie → `Add Note`.

**Layout:**
```
┌──────────────────────────────────────────────────────┐
│  TRADING JOURNAL                        [+ New Note] │
│                                                      │
│  ┌──────────────────────────────────────────────┐    │
│  │ Mar 15, 2:30 PM          AAPL   📌 Pinned    │    │
│  │                                              │    │
│  │ Bought 50 shares before earnings. Expecting  │    │
│  │ a beat based on supply chain data. Stop loss │    │
│  │ at $135.                                     │    │
│  │                                     [Edit]   │    │
│  └──────────────────────────────────────────────┘    │
│                                                      │
│  ┌──────────────────────────────────────────────┐    │
│  │ Mar 14, 10:15 AM         MARKET               │    │
│  │                                              │    │
│  │ Fed meeting next week. Considering reducing  │    │
│  │ tech exposure.                               │    │
│  └──────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────┘
```

**Neue Notiz erstellen:**
- Klick auf `+ New Note` oder Rechtsklick auf Aktie → `Add Note`
- Modal: Textfeld (mehrzeilig, max 500 Zeichen), optionales Symbol-Tag (Dropdown), Pin-Toggle
- `[Save]` und `[Cancel]`

**Notiz-Features:**
- **Symbol-Tag:** Optional. Verknüpft die Notiz mit einer Aktie. Wird in der Aktien-Detail-Ansicht angezeigt.
- **Pinned:** Gepinnte Notizen erscheinen immer oben.
- **Zeitstempel:** Automatisch (Spielzeit).
- **Filter:** Nach Symbol, Zeitraum, nur Pinned.
- **Maximale Notizen:** 100 (älteste können gelöscht werden).
- **Gespeichert im Savegame.**

**Anzeige in Aktien-Detail:**
Wenn Notizen für die aktuell angezeigte Aktie existieren: kleines `📝 2 notes`-Badge im Header, klickbar → zeigt die Notizen in einem kleinen Popup.

### 6.5 Risiko-Metriken

Angezeigt im Analytics-Tab (siehe 3.4.7):

- **Portfolio Beta:** Korrelation zum Marktindex. 1.0 = bewegt sich wie der Markt. >1 = volatiler. <1 = defensiver.
- **Diversifikations-Score:** 1-10. Basiert auf Anzahl Sektoren und Gleichmäßigkeit der Verteilung. 1 Sektor = 1, alle Sektoren gleich gewichtet = 10.
- **Sector Exposure:** Balkendiagramm der Sektorgewichtung vs. Marktgewichtung
- **Max Drawdown:** Größter Wertverlust vom Höchststand zum Tiefststand, in % und $.
- **Sharpe Ratio:** (Portfolio-Return - Risk-Free-Rate) / Portfolio-Volatilität. Risikobereinigter Return.

---

## 7. AI-Trader

### 7.1 Übersicht & Philosophie

AI-Trader sind die unsichtbare Hand des Marktes. Sie erzeugen Volumen, Liquidität und realistische Preisbewegungen. Ohne sie wäre der Markt tot — Preise würden sich nur durch Spieler-Orders bewegen.

**Grundregeln:**
- AI-Trader sind keine Gegner. Sie versuchen nicht, den Spieler zu schlagen.
- AI-Trader sind nicht sichtbar als individuelle Akteure. Der Spieler sieht ihre aggregierte Wirkung: Volumen, Preisbewegungen, Orderbook-Tiefe.
- Ausnahme: bestimmte AI-Aktivitäten werden indirekt sichtbar (z.B. "Institutional buying detected" als News, ungewöhnliches Volumen, Analyst Ratings).
- AI-Trader handeln nach ihren eigenen Regeln — sie können Geld verlieren und irrationale Entscheidungen treffen.
- Die Marktteilnehmer bilden ein Ökosystem. Jeder Typ erfüllt eine Funktion, die den Markt realistischer macht.

### 7.2 AI-Trader-Typen (16 Typen)

Die 16 Typen sind in 4 Kategorien eingeteilt:

**A) Liquiditäts-Provider (halten den Markt am Laufen)**
1. Market Maker
2. High-Frequency Trader (HFT)

**B) Institutionelle Akteure (große, langsame Kapitalströme)**
3. Pensionsfonds
4. Mutual Fund Manager (Aktive Fonds)
5. Index/ETF Fund (Passive Fonds)
6. Hedge Fund — Long/Short
7. Hedge Fund — Macro
8. Sovereign Wealth / Staatsfonds

**C) Professionelle Einzelhändler (schnell, strategisch)**
9. Day Trader
10. Swing Trader
11. Algorithmic / Quant Trader
12. Arbitrageur

**D) Marktverzerrer & Sonderfälle**
13. Retail Trader (Kleinanleger)
14. Insider Trader
15. Activist Short Seller
16. Corporate Buyback (kein Trader im eigentlichen Sinn, aber erzeugt Kaufdruck)

---

#### 7.2.1 Market Maker

**Kategorie:** Liquiditäts-Provider
**Funktion:** Stellt kontinuierlich Bid- und Ask-Orders, sorgt für Liquidität und ein funktionierendes Orderbook.

**Verhalten:**
- Stellt auf beiden Seiten (Bid und Ask) Orders ein
- Spread wird basierend auf Volatilität und Risiko berechnet
- Profitiert von der Bid-Ask-Differenz
- Meidet direktionales Risiko (hält keine großen Netto-Positionen)
- Zieht sich bei hoher Volatilität teilweise zurück (breitere Spreads, weniger Tiefe)
- Verschiedene Market Maker konkurrieren um den engsten Spread

**Parameter:**
- `spreadWidth`: 0.05-0.5% (je nach Aktien-Liquidität)
- `orderSize`: 100-10,000 Aktien pro Level
- `levels`: 5-10 Levels tief auf jeder Seite
- `riskTolerance`: wie weit die Netto-Position vom Null abweichen darf
- `volatilityResponse`: wie stark der Spread bei Volatilität erweitert wird
- `competitiveness`: wie aggressiv der Spread verengt wird (Konkurrenz)

**Auswirkung:** Enge Spreads bei liquiden Aktien, weite bei Krisen. Immer eine Gegenpartei.

**Population:** 2-5 pro Aktie (je nach MarketCap). ~1500 bei 500 Aktien.

#### 7.2.2 High-Frequency Trader (HFT)

**Kategorie:** Liquiditäts-Provider
**Funktion:** Ultrasschnelle Trades, die minimale Preisdifferenzen ausnutzen. Sorgen für Preiseffizienz.

**Verhalten:**
- Reagieren innerhalb von 1 Tick auf Preisänderungen
- Handeln in großem Volumen, halten Positionen nur für Sekunden/Minuten
- Market-Making-ähnlich, aber opportunistischer
- Erkennen und nutzen kurzfristige Orderflow-Muster
- Können bei Stress plötzlich verschwinden (Liquidity-Vacuum → Flash Crash Risiko)

**Parameter:**
- `reactionSpeed`: 1 Tick (sofort)
- `orderSize`: 1,000-50,000 Aktien (aber nur für Sekunden gehalten)
- `profitTarget`: 0.01-0.05% pro Trade
- `riskCut`: sofortiger Stop bei -0.1%
- `withdrawalThreshold`: bei welcher Volatilität der HFT den Markt verlässt

**Auswirkung:** Sorgen für sehr enge Spreads bei Large Caps. Können Flash Crashes verursachen, wenn sie gleichzeitig abschalten.

**Sichtbarkeit:** Indirekt — ultrahohes Volumen ohne Preisbewegung, plötzliches Liquiditäts-Vakuum bei Crashes.

**Population:** 5-15 (global, bevorzugen Large Caps).

#### 7.2.3 Pensionsfonds

**Kategorie:** Institutionell
**Funktion:** Extrem langfristig, konservativ, riesige Positionen. Repräsentieren Altersvorsorge-Kapital.

**Verhalten:**
- Kaufen ausschließlich Blue Chips und Dividend Aristocrats
- Halten Positionen für Monate bis Jahre (Spielzeit)
- Rebalancen quartalsweise (alle ~60 Spieltage)
- Reagieren kaum auf kurzfristige News oder Preisschwankungen
- Verkaufen nur bei fundamentaler Verschlechterung (Dividende gestrichen, Insolvenzrisiko)
- Kaufen Dips bei Blue Chips (automatischer Stabilisator)

**Parameter:**
- `targetAllocation`: pro Sektor definiert (z.B. 20% Tech, 15% Healthcare, 15% Financials...)
- `rebalanceInterval`: 60 Spieltage
- `positionSize`: 50,000-2,000,000 Aktien (massiv)
- `minDividendYield`: 1.5% (kauft keine Nicht-Dividend-Aktien)
- `maxBeta`: 1.2 (meidet zu volatile Aktien)
- `panicThreshold`: -40% (extrem hoch — verkauft fast nie in Panik)

**Auswirkung:** Stabilisieren den Markt. Rebalancing-Tage erzeugen vorhersehbare Volumen-Spikes. Ihr Kaufen bei Dips bremst Crashes.

**Sichtbarkeit:** News bei großen Block-Trades: `Pension fund activity detected: $XXM block trade in [SYMBOL].`

**Population:** 5-10 (global, jeder mit 20-50 Positionen).

#### 7.2.4 Mutual Fund Manager (Aktive Fonds)

**Kategorie:** Institutionell
**Funktion:** Professionell verwaltete Fonds, die versuchen, den Markt zu schlagen. Müssen sich an ein Mandat halten (z.B. "Growth Fund", "Value Fund", "Healthcare Fund").

**Verhalten:**
- Jeder hat ein Mandat (Sektor-Fokus oder Stil-Fokus)
- Growth-Fonds: kaufen Aktien mit hohem Umsatzwachstum, akzeptieren hohe KGVs
- Value-Fonds: kaufen unterbewertete Aktien mit niedrigem KGV
- Sektor-Fonds: kaufen nur innerhalb ihres Sektors
- Halten Positionen 20-90 Spieltage
- Reagieren auf Earnings, Analyst-Ratings und Fundamentaldaten
- Window Dressing: am Quartalsende kaufen sie Gewinner-Aktien und verkaufen Verlierer (damit das Portfolio gut aussieht)

**Parameter:**
- `mandate`: "growth", "value", "sector_tech", "sector_health", "dividend", "small_cap"
- `benchmarkIndex`: gegen welchen Index sie sich messen
- `trackingError`: wie weit sie vom Benchmark abweichen dürfen
- `positionSize`: 10,000-200,000 Aktien
- `holdingPeriod`: 20-90 Spieltage
- `windowDressingDay`: 3-5 Tage vor Quartalsende

**Auswirkung:** Erzeugen Sektor-Rotationen. Window-Dressing am Quartalsende kann zu vorhersehbaren Patterns führen.

**Sichtbarkeit:** News: `Fund flows: Growth funds see $XXB in inflows this quarter.`

**Population:** 15-30 (global, verteilt über verschiedene Mandate).

#### 7.2.5 Index/ETF Fund (Passive Fonds)

**Kategorie:** Institutionell
**Funktion:** Bilden den Marktindex nach. Kaufen und verkaufen strikt nach Index-Gewichtung. Keinerlei eigene Meinung.

**Verhalten:**
- Kaufen alle Aktien im Index proportional zur Marktkapitalisierung
- Verkaufen NIEMALS basierend auf Meinung — nur bei Rebalancing oder Mittelzu-/abflüssen
- Bei Mittelzuflüssen (neues Geld kommt in den Fonds): kaufen alle Aktien proportional
- Bei Mittelabflüssen (Anleger ziehen Geld ab): verkaufen alle Aktien proportional
- Rebalancen bei IPOs (neue Aktie wird aufgenommen) und Delistings

**Parameter:**
- `fundSize`: $1B-$50B (repräsentiertes Kapital)
- `dailyFlow`: -0.5% bis +0.5% des Fondsvermögens (zufällig, mit Bull/Bear-Bias)
- `rebalanceOnIPO`: true
- `rebalanceOnDelisting`: true

**Auswirkung:** Erzeugen stetigen Kaufdruck im Bull-Markt (Zuflüsse) und Verkaufsdruck im Bear-Markt (Abflüsse). Verstärken dadurch Trends. Bei IPOs: die neue Aktie bekommt einen "Index-Inclusion-Bump" weil alle passiven Fonds kaufen müssen.

**Sichtbarkeit:** News bei großen Flows: `Index funds see record inflows of $XXB this month.`

**Population:** 3-8 (wenige, aber riesig).

#### 7.2.6 Hedge Fund — Long/Short Equity

**Kategorie:** Institutionell
**Funktion:** Aggressiv, gehebelt, nutzt sowohl Long- als auch Short-Positionen. Versucht marktneutrale Rendite.

**Verhalten:**
- Kauft unterbewertete Aktien (Long) und shortet überbewertete (Short) gleichzeitig
- Nutzt Leverage (2-5×)
- Mittlere Haltedauer: 10-60 Spieltage
- Reagiert schnell auf Events und Fundamentaldaten
- Kann in Margin Calls geraten bei extremen Bewegungen → erzwungenes Liquidieren aller Positionen (de-leveraging)
- De-leveraging-Events können Kaskaden auslösen (wie 2008 oder Archegos 2021)

**Parameter:**
- `leverage`: 2-5×
- `longShortRatio`: 60% Long / 40% Short (variiert)
- `positionSize`: 5,000-100,000 Aktien
- `holdingPeriod`: 10-60 Spieltage
- `marginCallThreshold`: Portfolio -15% bis -25%
- `deleverageSpeed`: wie schnell bei Margin Call liquidiert wird

**Auswirkung:** Können durch Deleverage-Events massive Verkaufswellen auslösen. Ihre Shorts erzeugen Short Interest. Aggressives Shorten kann Short Squeezes provozieren.

**Sichtbarkeit:**
- News: `Hedge fund liquidation suspected as multiple large-cap stocks see unusual selling pressure.`
- News: `Sources report a major hedge fund faces margin call after [EVENT].`

**Population:** 8-15 (global).

#### 7.2.7 Hedge Fund — Macro / Global

**Kategorie:** Institutionell
**Funktion:** Handelt basierend auf makroökonomischen Trends. Setzt auf ganze Sektoren oder den Gesamtmarkt, nicht einzelne Aktien.

**Verhalten:**
- Analysiert Makro-Events (Zinsen, Inflation, GDP) und positioniert sich entsprechend
- Geht "All-In" auf Sektor-Wetten (z.B. Short Real Estate vor Zinserhöhung)
- Nutzt starken Leverage (3-10×)
- Hält Positionen wochen- bis monatelang
- Ändert Positionen selten, aber wenn, dann massiv

**Parameter:**
- `leverage`: 3-10×
- `macroView`: "hawkish" (erwartet Zinserhöhungen), "dovish" (erwartet Zinssenkungen), "recession_fear", "growth_optimism"
- `sectorBets`: welche Sektoren long/short
- `convictionLevel`: wie konzentriert die Wetten sind (1-5 Sektoren vs. breit)
- `positionSize`: 100,000-1,000,000 Aktien (in Sektor-Körben)

**Auswirkung:** Können ganze Sektoren bewegen. Ihre Positionsänderungen nach Makro-Events erzeugen große Sektor-Rotationen.

**Sichtbarkeit:** News: `Macro funds rotating out of [SECTOR] into [SECTOR], sources say.`

**Population:** 3-8 (wenige, aber einflussreich).

#### 7.2.8 Sovereign Wealth / Staatsfonds

**Kategorie:** Institutionell
**Funktion:** Riesige, staatsnahe Investmentvehikel. Extrem langfristig, konservativ, politisch beeinflusst.

**Verhalten:**
- Kaufen Blue Chips und Large Caps über alle Sektoren
- Extrem langsame Entscheidungsprozesse (Wochen bis Monate)
- Kaufen bevorzugt in Krisen ("Buy the dip" mit schier endlosem Kapital)
- Verkaufen selten — höchstens bei geopolitischen Gründen
- Ihre Käufe sind so groß, dass sie den Markt stützen können

**Parameter:**
- `fundSize`: $50B-$500B
- `buyFrequency`: alle 30-90 Tage
- `positionSize`: 200,000-5,000,000 Aktien (gigantisch)
- `dipBuyingThreshold`: kauft verstärkt wenn Markt -10% oder mehr gefallen ist
- `holdingPeriod`: 200+ Spieltage (quasi unbegrenzt)

**Auswirkung:** Marktstabilisator. Ihre Käufe in Krisen setzen einen "Floor" unter den Markt. Wenn selbst Staatsfonds verkaufen → das Signal ist apokalyptisch.

**Sichtbarkeit:** News: `Sovereign wealth fund reportedly accumulating positions in [SECTOR] stocks.`

**Population:** 2-5 (global).

#### 7.2.9 Day Trader

**Kategorie:** Professionell
**Funktion:** Kurzfristiger Händler, der am Tagesende keine Position hält. Schnell, technisch, diszipliniert.

**Verhalten:**
- Kauft und verkauft innerhalb eines Handelstages
- Nutzt technische Analyse: Support/Resistance-Levels, Breakouts, Candlestick-Patterns
- Sucht Aktien mit hohem Intraday-Momentum (>2% Tagesbewegung)
- Striktes Risikomanagement: Stop Loss bei -1 bis -2%
- Handelt nur die ersten 90 Minuten und letzten 60 Minuten (höchstes Volumen)
- Schließt ALLE Positionen vor Market Close

**Parameter:**
- `strategy`: "breakout", "momentum", "reversal"
- `entryThreshold`: Mindest-Momentum für Entry (z.B. 1% Bewegung in 15 Minuten)
- `stopLoss`: -1% bis -2%
- `takeProfit`: +2% bis +5%
- `orderSize`: 500-5,000 Aktien
- `tradingWindow`: nur 9:30-11:00 und 14:30-16:00

**Auswirkung:** Verstärken Intraday-Trends. Erzeugen hohes Volumen in den ersten/letzten Stunden. Ihre Stop-Losses können Kaskaden auslösen bei schnellen Drops.

**Population:** 50-200 (viele kleine Day Trader).

#### 7.2.10 Swing Trader

**Kategorie:** Professionell
**Funktion:** Hält Positionen 2-15 Tage. Sucht mittelfristige Trends und Patterns.

**Verhalten:**
- Kauft bei technischen Signalen: Golden Cross (SMA 50 kreuzt SMA 200), RSI-Reversal, Breakout aus Consolidation
- Hält 2-15 Spieltage
- Setzt Stop-Losses bei -5 bis -8%
- Nimmt Gewinne bei +10 bis +20%
- Analysiert Sektor-Momentum (bevorzugt Aktien in starken Sektoren)
- Vermeidet Aktien kurz vor Earnings (zu riskant)

**Parameter:**
- `strategy`: "trend_following", "breakout", "sector_momentum"
- `holdingPeriod`: 2-15 Spieltage
- `stopLoss`: -5% bis -8%
- `takeProfit`: +10% bis +20%
- `orderSize`: 1,000-10,000 Aktien
- `avoidEarnings`: true (keine neuen Positionen 5 Tage vor Earnings)

**Auswirkung:** Erzeugen mehrtägige Trends. Ihre Einstiege bei Breakouts verstärken Ausbrüche. Kollektive Gewinnmitnahmen können Pullbacks erzeugen.

**Population:** 30-80 (global).

#### 7.2.11 Algorithmic / Quant Trader

**Kategorie:** Professionell
**Funktion:** Vollautomatische Strategien basierend auf statistischen Modellen und technischen Indikatoren.

**Verhalten:**
- **Momentum-Algos:** Kaufen Aktien mit starkem Momentum (letzte 20-60 Tage), verkaufen bei Momentum-Umkehr
- **Mean-Reversion-Algos:** Kaufen bei starkem Abverkauf (RSI < 30, >2 Standardabweichungen unter MA), verkaufen bei Erholung
- **Pairs-Trading-Algos:** Identifizieren korrelierte Aktienpaare im gleichen Sektor. Wenn die Korrelation bricht (eine steigt, andere nicht): Long die zurückgebliebene, Short die vorgelaufene
- **Seasonality-Algos:** Handeln basierend auf historischen saisonalen Mustern
- Alle komplett emotionslos, regelbasiert

**Parameter:**
- `strategy`: "momentum", "mean_reversion", "pairs_trading", "seasonality"
- `signalThreshold`: Schwellenwert für Signal-Stärke
- `speed`: Reaktionszeit 1-3 Ticks
- `orderSize`: 500-10,000 Aktien
- `maxPositions`: 5-20 gleichzeitige Positionen
- `maxDrawdown`: bei welchem Portfolio-Drawdown alle Positionen geschlossen werden

**Auswirkung:** Sorgen für Preiseffizienz. Mean-Reversion-Algos bremsen Übertreibungen. Momentum-Algos verstärken Trends. Flash-Crash-Potenzial wenn mehrere gleichzeitig verkaufen.

**Population:** 15-40 (global).

#### 7.2.12 Arbitrageur

**Kategorie:** Professionell
**Funktion:** Nutzt Preisdifferenzen zwischen korrelierten Aktien oder zwischen Aktienpreis und fairem Wert aus.

**Verhalten:**
- Beobachtet Sektor-Korrelationen: wenn eine Tech-Aktie steigt und eine ähnliche nicht mitzieht → kauft die zurückgebliebene
- Bei Übernahmen: kauft Target-Aktie (handelt unter Deal-Preis) und shortet Acquirer (Merger Arbitrage)
- Handelt die Differenz zwischen Aktienpreis und berechnetem Fair Value
- Schnell, risikoarm, kleine Gewinne pro Trade

**Parameter:**
- `strategy`: "correlation_arb", "merger_arb", "value_arb"
- `priceDivergenceThreshold`: ab welcher Abweichung gehandelt wird
- `speed`: 1-2 Ticks
- `orderSize`: 2,000-20,000 Aktien
- `profitTarget`: 0.5-2% pro Trade (klein)

**Auswirkung:** Halten Korrelationen intakt. Sorgen dafür, dass Aktien im gleichen Sektor sich ähnlich bewegen. Halten den Markt "effizient".

**Population:** 10-20 (global).

#### 7.2.13 Retail Trader (Kleinanleger)

**Kategorie:** Marktverzerrer
**Funktion:** Repräsentieren Millionen von Kleinanlegern. Handeln emotional, trend-basiert, leicht beeinflussbar.

**Subtypen (innerhalb der Retail-Kategorie):**

**a) FOMO-Buyer:**
- Kauft wenn der Preis steigt (Momentum-Chasing)
- Reagiert stark auf positive News und Social-Media-Trends
- Hält 1-10 Tage, verkauft in Panik bei -10 bis -20%
- Kauft bevorzugt bekannte/medienpräsente Aktien

**b) Dividend-Collector:**
- Kauft Dividend-Aktien und hält langfristig
- Reinvestiert Dividenden
- Verkauft selten, außer bei Dividendenkürzung
- Stabilisierender Effekt auf Dividend-Aktien

**c) Dip-Buyer:**
- Kauft nach starken Kursrückgängen ("Buy the dip")
- Wartet auf -10% bis -20% Drop
- Kann mehrfach den gleichen Dip kaufen (Average Down)
- Hält 5-30 Tage

**d) Trend-Follower:**
- Folgt dem was in "Finanz-News" (Events) empfohlen wird
- Kauft Analyst-Upgrades, verkauft Downgrades
- Reagiert auf Insider-Buying-News
- Hält 3-15 Tage

**e) Panic-Seller:**
- Hält existierende Positionen, aber verkauft bei jeder schlechten Nachricht sofort
- Extrem niedriger Schmerzthreshold (-5 bis -10%)
- Kauft selten nach — einmal raus, bleibt raus (für diese Aktie)
- Erzeugt Überreaktionen bei negativen Events

**Parameter (global für alle Retail-Subtypen):**
- `fomoBias`: 0.1-0.9
- `panicBias`: 0.1-0.9
- `newsReactivity`: 0.3-1.0
- `herdFactor`: 0.2-0.8
- `orderSize`: 10-500 Aktien (klein)
- `holdingPeriod`: 1-30 Spieltage
- `subtype`: "fomo", "dividend", "dip_buyer", "trend_follower", "panic_seller"

**Auswirkung:** Verstärken ALLE Trends, erzeugen Übertreibungen, treiben Pump-Dynamiken, verursachen Panikverkäufe. Sind der emotionalste und unberechenbarste Marktteilnehmer.

**Population:** 1000-5000 Einheiten (jede repräsentiert ~1000 Kleinanleger), verteilt über die 5 Subtypen:
- FOMO-Buyer: 30%
- Dividend-Collector: 15%
- Dip-Buyer: 20%
- Trend-Follower: 20%
- Panic-Seller: 15%

#### 7.2.14 Insider Trader (AI)

**Kategorie:** Marktverzerrer
**Funktion:** Handelt VOR einem Event, basierend auf "Vorwissen."

**Verhalten:**
- Weiß 1-5 Spieltage vor einem Event, was passieren wird
- Baut langsam eine Position auf (um nicht aufzufallen)
- Variiert die Geschwindigkeit: manchmal über 5 Tage verteilt, manchmal in 1-2 Tagen (auffälliger)
- Verkauft/covert nach dem Event (typischerweise 1-3 Tage nach Event)
- Hat nicht immer Zugang zu jedem Event — nur zu 30-50% aller Company-Events

**Parameter:**
- `leadTime`: 1-5 Tage vor Event
- `positionBuildRate`: langsam (5 Tage) bis schnell (1 Tag)
- `accuracy`: 100% (weiß die Richtung)
- `accessProbability`: 0.3-0.5 (nicht jedes Event wird "geleaked")
- `orderSize`: 1,000-20,000 Aktien

**Auswirkung:** Ungewöhnliches Volumen VOR Events — Signal für aufmerksame Spieler. Generiert "Market Rumors" (siehe 4.8).

**Sichtbarkeit:** Indirekt über Volumen-Anomalien, Rumors.

**Population:** 10-25 (global, jeweils zukünftigen Events zugeordnet).

#### 7.2.15 Activist Short Seller

**Kategorie:** Marktverzerrer
**Funktion:** Recherchiert und shortet gezielt Unternehmen, die überbewertet sind oder Betrug begehen. Veröffentlicht Reports.

**Verhalten:**
1. **Research-Phase (10-20 Tage):** Identifiziert überbewertete Aktien (hohes KGV, sinkende Gewinne, hohe Verschuldung, verdächtige Buchhaltung)
2. **Position-Building (5-10 Tage):** Baut still Short-Position auf
3. **Report-Veröffentlichung:** Publiziert "Short Report" als News-Event → Preis fällt
4. **Profit-Taking:** Covert über die nächsten 5-15 Tage

**Parameter:**
- `targetCriteria`: überbewertete Aktien (KGV > 50, oder Schulden > 3× Eigenkapital)
- `positionSize`: 10,000-100,000 Aktien
- `reportImpact`: wie überzeugend der Short Report ist (beeinflusst andere Trader)
- `coveringPeriod`: 5-15 Tage nach Report

**Auswirkung:** Short Reports können Preise um 10-30% drücken. Erzeugen hohen Short Interest. Wenn der Report falsch liegt: Short Squeeze Gefahr.

**Sichtbarkeit:**
- Short-Report-News: `Activist short seller publishes critical report on [SYMBOL], alleging [ISSUE].`
- Steigende Short Interest Meldungen
- Gegenreaktion-News: `[COMPANY] denies allegations in short seller report.`

**Population:** 3-8 (global, selten aber impactful).

#### 7.2.16 Corporate Buyback Program

**Kategorie:** Sonderfall (kein echter "Trader", sondern ein Firmen-Mechanismus)
**Funktion:** Unternehmen kaufen eigene Aktien zurück, um den Preis zu stützen und Shareholder Value zu steigern.

**Verhalten:**
- Wird als Event ausgelöst: `[COMPANY] announces $[X]B share buyback program.`
- Danach: stetige Käufe über 30-90 Spieltage
- Kauft bevorzugt bei Dips (Rule 10b-18: nicht mehr als 25% des Tagesvolumens)
- Reduziert die umlaufenden Aktien → EPS steigt → KGV-Effekt

**Parameter:**
- `totalBuybackAmount`: $500M - $10B (abh. von Firmengröße)
- `duration`: 30-90 Spieltage
- `dailyLimit`: 25% des durchschnittlichen Tagesvolumens
- `dipBuyingBias`: kauft mehr bei Kursrückgängen

**Auswirkung:** Stetiger Kaufdruck über Wochen/Monate. Stützt den Preis. Besonders wichtig bei Blue Chips.

**Sichtbarkeit:**
- Ankündigungs-News (Event)
- Quartalsberichte: `[COMPANY] repurchased $XXM in shares during Q[Q].`

**Population:** Nicht "Population" im klassischen Sinn — wird pro Aktie aktiviert wenn das Event ausgelöst wird. Typisch: 5-15 gleichzeitig aktive Buyback-Programme.

### 7.3 AI-Trader-Population, Volumen-Kalibrierung & Skalierung

#### 7.3.1 Grundprinzip: Volumen-Zielwert zuerst

Die AI-Population wird NICHT von der Anzahl der Trader her gedacht, sondern vom **Ziel-Tagesvolumen pro Aktie**. Jede Aktie hat ein definiertes Basis-Tagesvolumen (siehe 11.3.3). Die AI-Trader müssen dieses Volumen erzeugen. Die Populationsgröße ergibt sich daraus.

**Ziel-Tagesvolumen nach Aktientyp:**
| Typ | Basis-Tagesvolumen | In Trades/Tag |
|---|---|---|
| Mega Cap | 10-50M Aktien | ~50,000-200,000 Trades |
| Large Cap | 2-10M Aktien | ~10,000-50,000 Trades |
| Mid Cap | 500K-2M Aktien | ~2,000-10,000 Trades |
| Small Cap | 50K-500K Aktien | ~500-2,000 Trades |
| Micro Cap | 5K-50K Aktien | ~50-500 Trades |

Das Backend erzeugt dieses Volumen nicht durch individuelle Trader-Simulationen, sondern durch ein **zweistufiges System:**

**Stufe 1: Aggregierter Volumen-Generator (Performance-effizient)**
- Pro Aktie wird pro Tick ein "Hintergrund-Volumen" generiert
- Dieses Volumen folgt der U-Kurve (hoch am Open/Close, niedrig mittags)
- Es repräsentiert die Summe aller kleinen, unbedeutenden Trades (Retail, passive Fonds, HFT)
- Hat keinen Einfluss auf die Preisrichtung — es ist neutrales Handelsrauschen
- Erzeugt 70-85% des Gesamtvolumens

**Stufe 2: Individuelle AI-Trader (Entscheidungsträger)**
- Die tatsächlich simulierten AI-Trader-Instanzen treffen Kauf-/Verkaufsentscheidungen
- Ihre Trades haben direktionalen Einfluss auf den Preis
- Erzeugen 15-30% des Gesamtvolumens
- DIESE Trades bewegen den Markt

**Warum dieses Zwei-Stufen-System?**
Wenn wir 200.000 Trades/Tag für eine Mega-Cap individuell simulieren wollten, bräuchten wir zehntausende AI-Instanzen pro Aktie. Bei 500 Aktien wäre das Performance-technisch unmöglich. Stattdessen generieren wir das "Grundrauschen" statistisch und simulieren nur die Trades, die den Preis tatsächlich bewegen.

#### 7.3.2 Individuelle AI-Trader-Population

Diese Trader werden einzeln simuliert und treffen eigenständige Kauf-/Verkaufsentscheidungen:

**Gesamtpopulation (bei 500 Aktien, Standard-Markt):**

| # | Typ | Instanzen | Ø Orders/Tag | Ø Stückzahl/Order | Haltedauer |
|---|---|---|---|---|---|
| 1 | Market Maker | ~1,500 | ~1,170 je | 500-5,000 | Sekunden |
| 2 | HFT | 20 | ~500 je | 1,000-10,000 | Sekunden |
| 3 | Pensionsfonds | 8 | ~0.5 je | 50,000-500,000 | Monate+ |
| 4 | Mutual Fund Manager | 30 | ~2 je | 10,000-100,000 | Wochen-Monate |
| 5 | Index/ETF Fund | 8 | ~1 je | 20,000-200,000 | Permanent |
| 6 | Hedge Fund L/S | 15 | ~5 je | 5,000-50,000 | Wochen |
| 7 | Hedge Fund Macro | 8 | ~1 je | 50,000-500,000 | Wochen-Monate |
| 8 | Sovereign Wealth | 5 | ~0.2 je | 100,000-2,000,000 | Monate+ |
| 9 | Day Trader | 200 | ~15 je | 500-5,000 | Minuten-Stunden |
| 10 | Swing Trader | 100 | ~1 je | 1,000-10,000 | Tage-Wochen |
| 11 | Algo/Quant | 40 | ~30 je | 500-5,000 | Minuten-Tage |
| 12 | Arbitrageur | 20 | ~20 je | 1,000-10,000 | Minuten-Stunden |
| 13a | Retail FOMO | 500 | ~2 je | 10-200 | Tage |
| 13b | Retail Dividend | 200 | ~0.3 je | 50-500 | Wochen-Monate |
| 13c | Retail Dip-Buyer | 300 | ~0.5 je | 20-300 | Tage-Wochen |
| 13d | Retail Trend-Follower | 300 | ~1 je | 20-200 | Tage |
| 13e | Retail Panic-Seller | 200 | ~0.5 je | 10-500 | — (verkauft nur) |
| 14 | Insider | 20 | ~2 je | 1,000-10,000 | Tage |
| 15 | Activist Short Seller | 8 | ~1 je | 5,000-50,000 | Wochen |
| 16 | Corporate Buyback | ~12 aktiv | ~5 je | 5,000-50,000 | Wochen-Monate |
| | **GESAMT** | **~3,494** | | | |

**Hinweis:** Market Maker sind mit ~1,500 die größte Gruppe, aber sie sind extrem simpel (Bid/Ask stellen) und performance-günstig. Die eigentliche CPU-Last kommt von den ~2,000 entscheidungstragenden Tradern.

#### 7.3.3 Volumen-Zusammensetzung einer Large-Cap-Aktie (Beispieltag)

```
Tagesvolumen: 5,000,000 Aktien

Aufschlüsselung:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Aggregiertes Hintergrund-Volumen    3,750,000   75%
  (statistisch generiert, preis-neutral)

Market Maker Orderflow                500,000   10%
  (Bid/Ask, preis-stabilisierend)

HFT Orderflow                        250,000    5%
  (schnelle Trades, kurzfristig preis-neutral)

Institutionelle Trades                200,000    4%
  (große Blöcke, selten aber impactful)

Algo/Quant + Arbitrage                150,000    3%
  (schnelle direktionale Trades)

Day/Swing Trader                       80,000    1.6%
  (mittlere Trades)

Retail (alle Subtypen)                 50,000    1%
  (viele kleine Trades)

Insider + Short Seller + Buyback       20,000    0.4%
  (selten, gezielt)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

**Was bewegt den Preis?**
Nicht das Gesamtvolumen, sondern das **Netto-Orderflow-Ungleichgewicht**:
- Wenn 55% der direktionalen Orders Käufe sind und 45% Verkäufe → Netto-Kaufdruck → Preis steigt
- Das Hintergrundvolumen ist per Definition neutral (50/50)
- Nur die ~25% "echten" AI-Trades haben direktionale Bias
- Ein Netto-Ungleichgewicht von 5% des direktionalen Volumens (~60,000 Aktien) kann den Preis einer Large Cap um ~0.5-1% bewegen
- Bei Small Caps reicht ein Netto-Ungleichgewicht von ~5,000 Aktien für 1-2% Bewegung

#### 7.3.4 Skalierung mit Marktgröße

| Markt | Aktien | Individuelle AI | Market Maker | Hintergrund-Volumen |
|---|---|---|---|---|
| Small (100) | 100 | ~1,200 | ~300 | Skaliert pro Aktie |
| Standard (500) | 500 | ~3,500 | ~1,500 | Skaliert pro Aktie |
| Large (1000) | 1000 | ~5,000 | ~3,000 | Skaliert pro Aktie |

- Market Maker skalieren linear (2-5 pro Aktie)
- Hintergrund-Volumen skaliert linear (definiert pro Aktie)
- Entscheidungstragende Trader skalieren **sublinear** — bei 1000 Aktien gibt es nicht doppelt so viele Hedge Funds wie bei 500, sondern die gleichen Fonds verteilen sich auf mehr Aktien → weniger Liquidität pro Aktie bei Small Caps = realistisch

#### 7.3.5 Verteilung nach Kapitalstärke

Die Verteilung des investierten Kapitals spiegelt die Realität wider:

```
Sovereign Wealth + Pensionsfonds ████████████████████████████████  45%
Index/ETF Funds (Passiv)         ██████████████████                20%
Mutual Funds (Aktiv)             ██████████████                    15%
Hedge Funds (L/S + Macro)        ████████                           8%
Algo/Quant + Arbitrage           ████                               4%
Day/Swing Trader                 ███                                3%
HFT                              ██                                 2%
Retail (alle Subtypen)           ██                                 2%
Insider/Short/Buyback            ▏                                 <1%
```

**Das bedeutet:** Wenn ein Pensionsfonds oder Sovereign Wealth Fund eine Position ändert, hat das einen ungleich größeren Preiseffekt als tausende Retail-Trades zusammen. Aber die Retail-Masse bestimmt das kurzfristige Sentiment und die Volatilität.

**AI-Startkapital (bei Standard-Markt, 500 Aktien):**

Das Gesamtkapital aller AI-Trader repräsentiert die 'Marktkapitalisierung' des simulierten Marktes.

| AI-Typ | Kapital pro Instanz | Gesamt (alle Instanzen) |
|---|---|---|
| Sovereign Wealth | $20-100B je | ~$200B |
| Pensionsfonds | $5-30B je | ~$100B |
| Index/ETF Fund | $5-50B je | ~$100B |
| Mutual Fund | $500M-5B je | ~$30B |
| Hedge Fund L/S | $500M-5B je | ~$20B |
| Hedge Fund Macro | $1-10B je | ~$20B |
| Algo/Quant | $50M-500M je | ~$5B |
| Arbitrageur | $50M-500M je | ~$3B |
| Day Trader | $50K-500K je | ~$20M |
| Swing Trader | $100K-1M je | ~$30M |
| Retail (alle) | $5K-100K je | ~$100M |
| Insider | $1M-10M je | ~$50M |
| Activist Short | $100M-1B je | ~$2B |
| **GESAMT** | | **~$480B** |

Bei 500 Aktien mit einer durchschnittlichen Marktkapitalisierung von ~$5B ergibt das eine Gesamt-Marktkapitalisierung von ~$2.5T. Die AI-Trader halten zusammen ~$480B = ~19% des Marktes (der Rest ist 'impliziertes' Kapital das nicht aktiv simuliert wird, aber in der Preisberechnung berücksichtigt wird).

**Skalierung:** Bei Small Market (100 Aktien): ~$100B AI-Kapital. Bei Large Market (1000 Aktien): ~$1T AI-Kapital.

#### 7.3.6 Performance-Budget der AI-Simulation

**Berechnung pro Tick bei 10x Speed (härtester Fall):**
- Tick-Budget: 100ms
- Market Maker: ~1,500 Instanzen × einfache Spread-Berechnung = ~5ms
- Hintergrund-Volumen: 500 Aktien × statistische Generierung = ~2ms
- Entscheidungstragende AI: ~2,000 Instanzen, davon ~200-400 entscheiden in diesem Tick = ~20ms
- Order-Matching: ~500 Orders pro Tick = ~10ms
- Preisberechnung: 500 Aktien = ~5ms
- Rest (Events, Margin, etc.): ~10ms
- **Gesamt: ~52ms** — passt in 100ms Budget

**Optimierung bei Performance-Problemen:**
1. AI-Entscheidungsfrequenz reduzieren (Retail entscheidet alle 30 statt 10 Ticks)
2. Market Maker nur alle 2 Ticks aktualisieren bei 10x
3. Hintergrundvolumen batchen (alle 5 Ticks statt jeden Tick)
4. Nur Aktien mit Spieler-Aktivität (Watchlist, offene Orders) in voller Auflösung simulieren, Rest vereinfacht

### 7.4 AI-Entscheidungslogik

**Tick-basierte Entscheidungsfindung:**
Nicht jeder AI-Trader entscheidet in jedem Tick. Entscheidungsfrequenz nach Typ:

| Typ | Ticks zwischen Entscheidungen | Begründung |
|---|---|---|
| Market Maker | 1 (jeder Tick) | Muss Orderbook aktuell halten |
| HFT | 1 (jeder Tick) | Geschwindigkeit ist ihr Vorteil |
| Day Trader | 1-5 | Beobachtet Intraday-Patterns aktiv |
| Algo/Quant | 1-3 | Automatisiert, reagiert schnell |
| Arbitrageur | 1-3 | Muss Preisdifferenzen sofort nutzen |
| Swing Trader | 10-30 | Entscheidet einmal pro Stunde (Spielzeit) |
| Retail (FOMO/Panic) | 5-20 | Menschliche Reaktionszeit |
| Retail (Dividend/Dip) | 30-100 | Weniger aktiv, geduldig |
| Hedge Fund L/S | 20-60 | Analysiert, dann handelt |
| Mutual Fund | 50-200 | Langsame Entscheidungsprozesse |
| Hedge Fund Macro | 100-400 | Ändert Positionen selten |
| Insider | 10-50 | Baut Position schrittweise auf |
| Activist Short | 30-100 | Methodisch, nicht hastig |
| Pensionsfonds | 200-500 | Extrem langsam |
| Sovereign Wealth | 300-1000 | Glacial |
| Corporate Buyback | 10-30 | Stetige tägliche Käufe |
| Index/ETF Fund | 50-200 | Reagiert auf Zuflüsse/Abflüsse |

**Entscheidungs-Inputs (nach Typ):**
| Input | MM | HFT | Day | Swing | Algo | Retail | Inst | Hedge |
|---|---|---|---|---|---|---|---|---|
| Aktueller Preis | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Preis-Trend (kurzfristig) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | | |
| Preis-Trend (langfristig) | | | | ✓ | ✓ | | ✓ | ✓ |
| Volumen | ✓ | ✓ | ✓ | | ✓ | | | |
| Orderbook-Tiefe | ✓ | ✓ | | | | | | |
| Technische Indikatoren | | | ✓ | ✓ | ✓ | | | |
| Fundamentaldaten | | | | | ✓ | | ✓ | ✓ |
| News/Events | | | | ✓ | | ✓ | ✓ | ✓ |
| Analyst Ratings | | | | | | ✓ | ✓ | |
| Sektor-Momentum | | | | ✓ | ✓ | | ✓ | ✓ |
| Eigenes Portfolio | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Markt-Sentiment | | | | | | ✓ | | ✓ |
| Insider-Signale | | | | | | | | |
| Makro-Daten | | | | | | | ✓ | ✓ |

**Zufallskomponente:**
- Jede Entscheidung hat einen Zufallsfaktor (verhindert gleichförmiges Verhalten)
- Stärke: 5% (Algos, HFT) bis 30% (Retail)
- Zwei identische AI treffen unterschiedliche Entscheidungen durch Zufall

### 7.5 AI-Interaktionsdynamiken

Das Zusammenspiel der AI-Typen erzeugt emergente Marktphänomene:

**Bull Market Dynamik:**
```
Index/ETF Zuflüsse → stetiger Kaufdruck
  → Preise steigen → Momentum-Algos kaufen
    → Retail FOMO kaufen → Preise steigen schneller
      → Swing Trader steigen ein → Trend verstärkt sich
        → Hedge Funds gehen Long mit Leverage
```

**Bear Market / Crash Dynamik:**
```
Negatives Makro-Event → Hedge Fund Macro verkauft Sektor
  → Preise fallen → Algo Stop-Losses triggern
    → Retail Panic-Seller verkaufen → Preise fallen schneller
      → Hedge Fund L/S Margin Call → erzwungene Liquidation
        → Day Trader Stop-Losses → Kaskade
          → HFT ziehen sich zurück → Liquiditäts-Vakuum
            → Market Maker erhöhen Spreads → Slippage steigt
              → Pensionsfonds/Sovereign kaufen → Boden bildet sich
```

**Short Squeeze Dynamik:**
```
Activist Short Seller veröffentlicht Report → Preis fällt
  → Mehr Retail/Algos shorten → Short Interest steigt >40%
    → Positive Überraschung (Earnings Beat) → Preis steigt
      → Short Seller müssen covern → Kaufdruck
        → Retail FOMO kaufen → Preis steigt schneller
          → Mehr Shorts covern (erzwungen) → Kaskade
            → HFT handeln die Momentum → extremer Spike
              → Schließlich: Mean-Reversion-Algos verkaufen → Preis normalisiert
```

**Flash Crash Dynamik:**
```
Algo-Signal: Verkauf (z.B. MA-Crossover bei mehreren Algos gleichzeitig)
  → Schneller Preisfall → HFT erkennen Trend, verstärken
    → HFT ziehen Liquidität ab → Orderbook wird dünn
      → Market Maker erhöhen Spreads drastisch
        → Day Trader Stop-Losses → weitere Verkäufe ins Vakuum
          → Circuit Breaker triggert → Handel gestoppt
            → Nach Wiedereröffnung: Dip-Buyer + Pensionsfonds kaufen → Erholung
```

### 7.6 AI-Impact auf Spieler-Erlebnis

**Balance-Grundregel:** Der Spieler soll das Gefühl haben, in einem lebendigen Markt zu handeln, aber seine Entscheidungen sollen Auswirkungen haben.

**Spieler-Impact vs. AI-Impact:**
- Bei Mega/Large Caps: Spieler-Orders haben minimalen Preiseinfluss (realistisch — ein Einzelner bewegt Apple nicht)
- Bei Mid Caps: Spieler-Orders haben leichten Einfluss bei großen Orders
- Bei Small/Micro Caps: Spieler-Orders können den Preis merklich bewegen (auch realistisch)
- Empfehlung: Spieler wird organisch zu Small Caps tendieren, wenn er "den Markt schlagen" will

**AI-Fehler:**
AI-Trader sind nicht perfekt:
- Retail-AI kauft oft zu hoch und verkauft zu tief (FOMO + Panik)
- Hedge Funds gehen manchmal pleite (Deleverage-Events)
- Algos erzeugen gelegentlich Flash Crashes (Fehler im System)
- Mutual Funds underperformen den Index (realistisch — 85% der aktiven Fonds verlieren gegen den Index)
- Insider-Trades sind nicht immer profitabel (Event kann anders ausfallen als erwartet, 30% Fehlerrate)

Das schafft Opportunities für den Spieler, der diese Muster erkennt.

---

## 8. Events System

### 8.1 Event-Architektur

Events sind der narrative Motor des Spiels. Sie erklären, WARUM sich Preise bewegen, und geben dem Spieler Kontext für Entscheidungen.

**Event-Pipeline:**
```
Generierung → Scheduling → Auslösung → Effekt-Anwendung → News-Anzeige
```

1. **Generierung:** Das System generiert Events basierend auf Templates + Randomisierung
2. **Scheduling:** Manche Events werden zeitlich geplant (Earnings, Zentralbank), andere sind zufällig
3. **Auslösung:** Zum geplanten Zeitpunkt oder wenn Bedingungen erfüllt sind
4. **Effekt-Anwendung:** Preis-, Volatilitäts-, Volumen- und Sentiment-Effekte werden berechnet und angewendet
5. **News-Anzeige:** Event wird als News-Meldung im Frontend dargestellt

**Event-Datenstruktur (C#):**
```
Event {
  Id: string
  Type: EventType (Macro, Sector, Company, Market)
  Severity: EventSeverity (Minor, Moderate, Major, Catastrophic)
  Sentiment: float (-1.0 bis +1.0, negativ = schlecht)
  Title: string
  Description: string
  AffectedSymbols: string[] (leer bei Macro-Events)
  AffectedSectors: string[] (leer bei Company-Events)
  PriceEffect: float (prozentuale Preisänderung, z.B. -0.05 = -5%)
  VolatilityEffect: float (Multiplikator, z.B. 2.0 = doppelte Vola)
  VolumeEffect: float (Multiplikator, z.B. 3.0 = dreifaches Volumen)
  Duration: int (Spieltage, wie lange der Effekt anhält)
  FollowUpEvents: EventTemplate[] (mögliche Folge-Events)
  FollowUpProbability: float (0-1)
  Timestamp: DateTime
}
```

### 8.2 Event-Kategorien

#### 8.2.1 Makroökonomische Events

Diese Events betreffen den gesamten Markt.

**Zentralbank-Zinsentscheidung:**
| Variante | Sentiment | Preis-Effekt (Markt) | Besonders betroffen |
|---|---|---|---|
| Rate Hike (+25bps) | -0.3 | -1% bis -2% | Real Estate (-3%), Tech (-2%), Financials (+1%) |
| Rate Hike (+50bps, surprise) | -0.7 | -3% bis -5% | Real Estate (-6%), Tech (-4%) |
| Rate Hold (expected) | 0.0 | ±0.2% | Minimal |
| Rate Cut (-25bps) | +0.4 | +1% bis +2% | Real Estate (+3%), Tech (+2%) |
| Rate Cut (-50bps, emergency) | +0.3 / -0.3 | Mixed | Kann bullish oder bearish sein (signalisiert Krise) |

Beispiel-Headlines:
- `Federal Reserve raises interest rates by 25 basis points to 5.50%`
- `Fed holds rates steady, citing mixed economic signals`
- `Federal Reserve cuts rates in surprise emergency move`

**Inflationsdaten:**
| Variante | Sentiment | Effekt |
|---|---|---|
| Inflation lower than expected | +0.4 | Markt +1-2%, Tech/Growth besonders stark |
| Inflation as expected | 0.0 | Minimal |
| Inflation higher than expected | -0.4 | Markt -1-3%, Angst vor weiteren Zinserhöhungen |
| Inflation much higher (shock) | -0.7 | Markt -3-5%, Volatilität steigt stark |

Beispiel-Headlines:
- `CPI comes in at 2.1%, below expectations of 2.4%`
- `Inflation surges to 6.8%, highest in 18 months`

**Weitere Makro-Events (gleiche Detailtiefe im Anhang):**
- GDP Report (higher/lower/as expected)
- Jobs Report (strong/weak/as expected)
- Sovereign Debt Crisis (ein Land, Ansteckungseffekte)
- Recession Start/End Declaration
- Trade War Escalation/De-Escalation
- Currency Crisis

#### 8.2.2 Sektor-Events

Betreffen alle Aktien innerhalb eines Sektors.

**Technology:**
| Event | Sentiment | Effekt auf Sektor |
|---|---|---|
| Major tech breakthrough | +0.5 | +3-5%, dann +1%/Tag für 3 Tage |
| Data privacy regulation announced | -0.4 | -2-4% |
| Global chip shortage worsens | -0.3 | -2-3%, besonders Halbleiter |
| Tech antitrust investigation | -0.3 | -2-4% auf Top-Aktien |
| AI regulation framework announced | -0.2 | -1-3% |

**Energy:**
| Event | Sentiment | Effekt auf Sektor |
|---|---|---|
| Oil price spike (+20%) | +0.5 | +5-8% |
| Oil pipeline explosion | +0.3 (Preis) / -0.5 (Betreiber) | Mixed |
| OPEC production cut | +0.4 | +3-5% |
| Renewable energy subsidy announced | +0.3 (Renewables) | Solar/Wind +5%, Oil -2% |
| Major oil spill / environmental disaster | -0.6 | Betreiber -10-20%, Sektor -2-3% |

**Financials:**
| Event | Sentiment | Effekt |
|---|---|---|
| Bank stress test results (pass) | +0.3 | +2-3% |
| Bank stress test results (fail) | -0.6 | Betroffene Bank -10%, Sektor -3-5% |
| Financial regulation tightened | -0.3 | -2-3% |
| Crypto market crash | -0.2 | -1-2% (ansteckend, obwohl kein Crypto im Spiel) |
| Interest rate sensitivity shift | varies | Depends on direction |

**Healthcare:**
| Event | Sentiment | Effekt auf Sektor |
|---|---|---|
| FDA approves blockbuster drug | +0.5 | Pharma-Firma +15-30%, Sektor +2-3% |
| FDA rejects drug application | -0.5 | Betroffene Firma -20-40%, Sektor -1-2% |
| Pandemic outbreak declared | +0.6 (Healthcare) | Pharma/Biotech +10-15%, Medical Devices +5% |
| Healthcare regulation tightened | -0.3 | Sektor -3-5%, besonders Versicherungen |
| Drug pricing scandal | -0.4 | Betroffene Firma -10-20%, Sektor -2% |
| Successful clinical trial (Phase 3) | +0.5 | Firma +20-40%, Konkurrenten -2-5% |
| Failed clinical trial | -0.6 | Firma -30-50%, Sektor -1% |
| Major hospital chain merger | +0.2 | Healthcare Services +3-5% |
| Patent expiration on blockbuster drug | -0.3 | Firma -8-15%, Generic-Hersteller +5-10% |
| Biotech breakthrough announced | +0.4 | Biotech-Subsector +5-10%, Pharma +2% |
| Medical device recall | -0.4 | Firma -10-15%, Sektor -1% |
| Opioid settlement announced | -0.3 | Beteiligte Firmen -5-10%, Sektor -1-2% |

**Consumer Goods:**
| Event | Sentiment | Effekt auf Sektor |
|---|---|---|
| Consumer confidence surges | +0.3 | Sektor +2-4% |
| Consumer confidence drops | -0.3 | Sektor -2-4%, Luxury -3-5% |
| Major product recall (food/safety) | -0.4 | Firma -8-15%, Konkurrenten +1-2% |
| Supply chain disruption | -0.3 | Sektor -2-4%, betrifft alle Hersteller |
| Viral product / social media trend | +0.3 | Firma +5-15%, kurzfristig (1-5 Tage) |
| E-commerce growth report | +0.2 | Online-Retailers +3-5%, Traditional -1-2% |
| Raw material price spike | -0.2 | Sektor -1-3% (Margen unter Druck) |
| Holiday season sales beat expectations | +0.3 | Sektor +2-4% (Q4 Event) |
| Holiday season sales disappoint | -0.3 | Sektor -2-4% |
| Major brand acquisition | +0.2 | Target +10-20%, Acquirer -2-5% |
| Health trend shifts demand | +0.2/-0.2 | Gesunde Produkte +3-5%, Junk Food -2-3% |

**Industrials:**
| Event | Sentiment | Effekt auf Sektor |
|---|---|---|
| Infrastructure spending bill passed | +0.5 | Sektor +4-8%, Construction +6-10% |
| Manufacturing PMI above 55 | +0.3 | Sektor +2-3% |
| Manufacturing PMI below 45 | -0.3 | Sektor -2-4% |
| Major factory accident | -0.4 | Firma -8-15%, Safety-Regulierung droht |
| Automation/robotics breakthrough | +0.3 | Automation-Firmen +5-8%, Traditional Labor -2% |
| Trade war tariffs imposed | -0.4 | Export-Firmen -5-10%, Domestic +1-2% |
| Trade war tariffs removed | +0.3 | Export-Firmen +3-5% |
| Defense contract awarded | +0.3 | Firma +5-10%, Konkurrenten -1-2% |
| Supply chain normalization | +0.2 | Sektor +1-3% |
| Construction permits decline | -0.2 | Construction -3-5% |
| Major industrial merger | +0.2 | Target +15-25%, Sektor +1% |

**Materials:**
| Event | Sentiment | Effekt auf Sektor |
|---|---|---|
| Commodity price surge (copper/steel) | +0.4 | Mining +5-10%, Chemical -2% (input costs) |
| Commodity price crash | -0.4 | Mining -5-10%, Chemical +1-2% |
| Mining accident / environmental disaster | -0.5 | Firma -10-20%, Sektor -2-3% |
| New mineral deposit discovered | +0.3 | Firma +8-15% |
| Environmental regulation tightened | -0.3 | Sektor -3-5%, Clean Materials +2% |
| China demand surge | +0.4 | Sektor +3-6% (China = größter Abnehmer) |
| China demand slowdown | -0.4 | Sektor -3-6% |
| Rare earth supply disruption | +0.3 | Rare Earth Miners +10-15%, Tech -1-2% |
| Recycling technology breakthrough | -0.2 | Virgin Materials -3-5%, Recycling +5% |
| Steel tariffs imposed | +0.2/-0.3 | Domestic Steel +3-5%, Importers -5% |
| Chemical plant explosion | -0.4 | Firma -10-15%, Chemical Sektor -2% |

**Real Estate:**
| Event | Sentiment | Effekt auf Sektor |
|---|---|---|
| Interest rate cut | +0.5 | Sektor +4-8% (sehr zinssensitiv) |
| Interest rate hike | -0.5 | Sektor -4-8% |
| Housing starts surge | +0.3 | Residential +3-5% |
| Housing starts decline | -0.3 | Residential -3-5% |
| Commercial vacancy rates rise | -0.3 | Commercial REITs -4-6% |
| Work-from-home trend accelerates | -0.3 | Office REITs -5-10%, Residential +2-3% |
| Foreign investment in property market | +0.2 | Sektor +2-4% |
| Property bubble fears | -0.4 | Sektor -3-6%, Financials -2% |
| Rent control legislation proposed | -0.3 | Residential REITs -5-8% |
| Mortgage rate hits new low | +0.4 | Sektor +3-6%, Homebuilders +5-8% |
| Major REIT dividend cut | -0.4 | Betroffener REIT -10-15%, REIT-Sektor -2% |

**Telecommunications:**
| Event | Sentiment | Effekt auf Sektor |
|---|---|---|
| 5G/6G network expansion announced | +0.3 | Sektor +2-4%, Equipment +5% |
| Spectrum auction results | +0.2/-0.2 | Gewinner +3-5%, Verlierer -2-3% |
| Major network outage | -0.4 | Betroffene Firma -5-10% |
| Streaming subscriber growth beats | +0.3 | Media/Streaming +5-8% |
| Streaming subscriber loss | -0.4 | Firma -8-15% |
| Telecom merger approved | +0.3 | Target +10-20%, Konkurrenten -2% |
| Telecom merger blocked | -0.2 | Target -10-15%, Konkurrenten +2% |
| Data privacy regulation | -0.2 | Sektor -2-3% |
| Rural broadband subsidy announced | +0.2 | Broadband-Provider +3-5% |
| Cable cutting accelerates | -0.3 | Traditional TV -5-8%, Streaming +2-3% |
| Cybersecurity breach at telecom | -0.3 | Firma -5-8%, Cybersecurity-Firmen +3% |

**Utilities:**
| Event | Sentiment | Effekt auf Sektor |
|---|---|---|
| Rate increase approved by regulator | +0.2 | Firma +3-5% (höhere Einnahmen) |
| Rate increase denied | -0.3 | Firma -5-8% |
| Extreme weather (heat wave/cold snap) | +0.2 | Sektor +2-3% (höherer Verbrauch) |
| Power grid failure | -0.4 | Firma -5-10%, Regulierung droht |
| Renewable energy mandate expanded | +0.3 | Renewable Utilities +5-8%, Coal/Gas -3% |
| Nuclear plant safety concern | -0.4 | Nuclear -8-12%, andere Utilities +1% |
| Water shortage / drought | -0.2 | Water Utilities -3-5% (Regulierung) |
| Utility merger approved | +0.2 | Target +8-12% |
| Carbon tax legislation proposed | -0.3 | Coal/Gas Utilities -5-8%, Renewables +3% |
| Green energy subsidy expanded | +0.3 | Renewable +5-8%, Traditional -1% |

**Luxury Goods:**
| Event | Sentiment | Effekt auf Sektor |
|---|---|---|
| Luxury spending boom (wealthy consumers) | +0.4 | Sektor +3-6% |
| Economic downturn hits luxury spending | -0.4 | Sektor -4-8% (stark zyklisch) |
| Celebrity endorsement / viral trend | +0.3 | Firma +5-10% (kurzfristig) |
| Counterfeiting scandal | -0.3 | Firma -5-10% (Marken-Image) |
| China luxury demand surge | +0.4 | Sektor +4-7% (China = größter Luxusmarkt) |
| China luxury crackdown | -0.4 | Sektor -4-7% |
| Iconic brand heritage event | +0.2 | Firma +2-5% (Tradition/Prestige) |
| Supply chain issue (rare materials) | -0.2 | Firma -3-5% |
| New flagship store / expansion | +0.1 | Firma +1-3% |
| Luxury conglomerate acquires brand | +0.3 | Target +15-25%, Conglomerate -2% |
| ESG/sustainability controversy | -0.3 | Firma -5-10% |

**Transportation:**
| Event | Sentiment | Effekt auf Sektor |
|---|---|---|
| Oil price spike (fuel costs up) | -0.4 | Airlines -5-10%, Trucking -3-5%, Rail +1% |
| Oil price drop (fuel costs down) | +0.3 | Airlines +3-5%, Trucking +2-3% |
| Major airline accident | -0.5 | Firma -10-20%, Sektor -2-3% |
| Airport expansion approved | +0.2 | Airlines +2-3%, Airport operators +5% |
| Shipping container shortage | -0.3 | Shipping -3-5%, Exporters -1-2% |
| Autonomous vehicle breakthrough | +0.4 | AV-Firmen +8-15%, Traditional Transport -2% |
| Port workers strike | -0.3 | Shipping -5-8%, Retailers -1-2% |
| Rail accident / derailment | -0.3 | Rail -5-8%, Trucking +2% |
| Electric vehicle adoption milestone | +0.3 | EV Transport +5-8%, Traditional -1% |
| Tourism boom / travel demand surge | +0.3 | Airlines +4-6%, Hotels +3-5% |
| Travel ban / border restrictions | -0.4 | Airlines -8-15%, Shipping -2% |

Jeder Sektor hat 10-12 eigene Event-Templates. Zusammen mit den Makro-Events (8.2.1), Unternehmens-Events (8.2.3), Kapitalmaßnahmen (8.2.4), Geopolitischen Events (8.2.5), Rechtlichen Events (8.2.6), Unternehmensstruktur-Events (8.2.7) und Marktstruktur-Events (8.2.8) ergeben sich **über 200 verschiedene Event-Templates**. Durch Platzhalter-Variation, Stärke-Abstufungen und Kaskadierung sind tausende einzigartige Event-Kombinationen möglich.

#### 8.2.3 Unternehmens-Events

Betreffen eine einzelne Aktie.

**Earnings Report:**
| Variante | Sentiment | Preis-Effekt |
|---|---|---|
| Beats expectations significantly | +0.7 | +5-15% (Gap Up am nächsten Tag) |
| Beats expectations slightly | +0.3 | +2-5% |
| In line with expectations | 0.0 | ±1% (oft leicht negativ: "sell the news") |
| Misses slightly | -0.3 | -3-7% |
| Misses significantly | -0.7 | -10-25% (Gap Down) |
| Misses + lowers guidance | -0.9 | -15-30% |

Beispiel-Headlines:
- `[COMPANY] reports Q3 earnings: EPS $2.45 vs $2.10 expected, revenue up 18%`
- `[COMPANY] misses earnings estimates, lowers full-year guidance`

**Weitere Unternehmens-Events:**
| Event | Sentiment | Preis-Effekt |
|---|---|---|
| CEO resignation (unexpected) | -0.4 | -5-10% |
| CEO hired (well-regarded) | +0.3 | +3-5% |
| Fraud/Accounting scandal | -0.9 | -20-50% |
| Successful product launch | +0.5 | +5-10% |
| Product recall / major bug | -0.4 | -5-10% |
| Acquisition announced (acquirer) | -0.1 | -2-5% (overpaying fear) |
| Acquisition announced (target) | +0.7 | +15-30% (premium) |
| Stock buyback program | +0.2 | +2-3% |
| Dividend increase | +0.2 | +1-3% |
| Dividend cut | -0.4 | -5-10% |
| Bankruptcy warning | -0.8 | -30-60% |
| Bankruptcy filing | -0.95 | -80-99% |
| Regulatory investigation | -0.3 | -5-15% |
| Patent victory | +0.3 | +3-8% |
| Patent loss | -0.3 | -3-8% |

#### 8.2.4 Kapitalmaßnahmen (Corporate Actions)

Events die die Aktienstruktur eines Unternehmens verändern.

**Stock Split:**
| Variante | Effekt |
|---|---|
| Forward Split (z.B. 4:1) | Preis ÷4, Stückzahl ×4, MarketCap bleibt gleich |
| Forward Split (z.B. 10:1) | Preis ÷10, Stückzahl ×10, MarketCap bleibt gleich |
| Reverse Split (z.B. 1:10) | Preis ×10, Stückzahl ÷10, MarketCap bleibt gleich |

- **Forward Split Trigger:** Aktien mit Preis >$500 haben eine Chance (5% pro Quartal), einen Split anzukündigen
- **Reverse Split Trigger:** Aktien mit Preis <$1 haben eine Chance (20% pro Quartal), einen Reverse Split durchzuführen (um Delisting zu vermeiden)
- **Ankündigung:** 10 Spieltage vor dem Split: `[COMPANY] announces [X]-for-1 stock split, effective [DATE].`
- **Sentiment:** Forward Split = leicht positiv (+0.2, signalisiert Stärke), Reverse Split = negativ (-0.3, signalisiert Schwäche)
- **Preis-Effekt:** Forward Split: Preis steigt oft +5-10% bis zum Split-Datum (Retail-Begeisterung, psychologisch "billiger"). Reverse Split: Preis fällt oft -5-15% (negatives Signal)
- **Am Split-Tag:** Preis wird mechanisch angepasst. Charts zeigen split-adjustierte historische Preise (wie im echten Markt).
- **Spieler-Position:** Wird automatisch angepasst. 50 Aktien @ $200 → 200 Aktien @ $50 (bei 4:1 Split). Toast: `Stock split: Your 50 shares of AAPL are now 200 shares.`

**Auswirkung auf offene Orders:** Alle offenen Limit-, Stop- und Trailing-Stop-Orders werden automatisch split-adjustiert. Bei einem 4:1 Split: Limit-Preis $200 → $50, Stückzahl 100 → 400. Toast: `Your open orders for [SYMBOL] have been adjusted for the stock split.`

**Auswirkung auf Short-Positionen:** Short-Positionen werden identisch adjustiert. Bei einem 4:1 Split: Short 100 Aktien @ $200 → Short 400 Aktien @ $50. Die ökonomische Exposure bleibt identisch. Toast: `Your short position in [SYMBOL] has been adjusted for the stock split.`

Beispiel-Headlines:
- `[COMPANY] announces 4-for-1 stock split to make shares "more accessible"`
- `[COMPANY] executes 1-for-10 reverse stock split to maintain listing requirements`

**Secondary Offering (Kapitalerhöhung):**
- Ein Unternehmen gibt neue Aktien aus, um Kapital zu beschaffen
- **Effekt:** Verwässerung → EPS sinkt → Preis fällt typischerweise -5-15%
- **Trigger:** Unternehmen mit hoher Verschuldung oder schnellem Wachstum (brauchen Kapital)
- **Frequenz:** Selten — 1-3 pro Quartal im gesamten Markt
- **Ankündigung:** `[COMPANY] announces secondary offering of [X] million shares at $[PRICE].`
- **Pricing:** Neue Aktien werden typischerweise 3-8% unter dem aktuellen Kurs angeboten (Discount)
- **Umlaufende Aktien:** Erhöhen sich → MarketCap-Berechnung ändert sich
- **Follow-up:** Manchmal positives Signal 1-2 Wochen später (`Secondary offering oversubscribed, signaling strong institutional demand`)

Beispiel-Headlines:
- `[COMPANY] prices secondary offering at $XX, a 5% discount to market`
- `[COMPANY] raises $XXM through share issuance to fund expansion`

#### 8.2.5 Geopolitische & Makro-Events (erweitert)

**Geopolitische Konflikte / Kriege:**
| Variante | Sentiment | Effekt |
|---|---|---|
| Military conflict escalation | -0.6 | Markt -3-8%, Defense/Aerospace +5-10%, Energy +3-5% (Ölpreis), Transport -5% |
| Ceasefire / Peace agreement | +0.5 | Markt +2-5%, Defense -3%, Energy -2% |
| Economic sanctions imposed | -0.3 | Betroffene Sektoren -3-8%, Energy +2-5% (Angebotsverknappung) |
| Sanctions lifted | +0.3 | Betroffene Sektoren +3-5% |

Beispiel-Headlines:
- `Tensions escalate in [REGION] as military forces mobilize along border.`
- `International sanctions imposed on [COUNTRY], targeting energy and financial sectors.`

**Wahlen & Politische Events:**
| Variante | Sentiment | Effekt |
|---|---|---|
| Election: Business-friendly candidate wins | +0.3 | Markt +2-4%, Financials +3%, Regulierte Sektoren +2% |
| Election: Regulatory candidate wins | -0.2 | Markt -1-3%, Tech -3%, Healthcare -4%, Energy -2% |
| Government shutdown threat | -0.2 | Markt -1-2%, Defense/Gov-Contractors -3-5% |
| Government shutdown begins | -0.3 | Markt -2-3%, Vola steigt |
| Debt ceiling crisis | -0.5 | Markt -3-5%, Financials -5-8%, Vola 2× |
| Debt ceiling resolved | +0.3 | Markt +2-3% (Erleichterung) |

Beispiel-Headlines:
- `[CANDIDATE] wins presidential election, markets react to policy implications.`
- `Government shutdown enters day [X] as negotiations stall.`

**Naturkatastrophen:**
| Variante | Sentiment | Effekt |
|---|---|---|
| Major earthquake | -0.3 | Versicherungen -5-10%, Infrastruktur -3%, Bauunternehmen +3-5% (Wiederaufbau) |
| Hurricane / Typhoon | -0.3 | Versicherungen -5-10%, Energy (Offshore) -3-5%, Bauunternehmen +2-4% |
| Wildfire / Drought | -0.2 | Landwirtschaft -3-5%, Utilities -2-3% |
| Flooding / Tsunami | -0.4 | Versicherungen -8-12%, Transport -3%, Real Estate -2-5% |

Beispiel-Headlines:
- `Magnitude [X] earthquake strikes [REGION], widespread damage reported.`
- `Category [X] hurricane makes landfall, [NUMBER] million without power.`

**Arbeitskämpfe / Streiks:**
| Variante | Sentiment | Effekt |
|---|---|---|
| Major labor strike at [COMPANY] | -0.4 | Betroffene Firma -5-15%, Konkurrenten +2-3% (Marktanteil-Gewinn) |
| Industry-wide strike threat | -0.3 | Gesamter Sektor -3-5% |
| Strike resolved | +0.2 | Firma +3-5% (Erleichterung), Sektor +1-2% |

Beispiel-Headlines:
- `[NUMBER] workers at [COMPANY] begin strike over wage dispute.`
- `[INDUSTRY] union threatens nationwide walkout starting [DATE].`

**Weitere Makro-Daten (erweitert):**
| Event | Frequenz | Effekt |
|---|---|---|
| Consumer Confidence Report | Monatlich | Positiv: Consumer +1-2%, Negativ: Consumer -1-2% |
| Manufacturing PMI | Monatlich | Über 50: Industrials +1%, Unter 50: Industrials -1-2% |
| Housing Market Data | Monatlich | Stark: Real Estate +2-3%, Schwach: Real Estate -2-3% |
| Credit Rating Change (Country) | Selten | Downgrade: Markt -3-5%, Financials -5%. Upgrade: +2-3% |
| Commodity Price Shock (Gold/Copper/Lithium) | Zufällig | Materials +/-5-10%, abhängig von Rohstoff |

**Earnings Guidance (Forward Guidance):**
Separate von den Earnings-Zahlen selbst. Eine Firma kann gute Earnings haben aber schlechte Guidance — oder umgekehrt.
| Variante | Sentiment | Effekt |
|---|---|---|
| Guidance raised above expectations | +0.4 | Aktie +5-10% (zusätzlich zu Earnings-Effekt) |
| Guidance in line | 0.0 | Kein zusätzlicher Effekt |
| Guidance lowered | -0.5 | Aktie -8-15% (AUCH wenn Earnings gut waren!) |
| Guidance withdrawn (uncertainty) | -0.3 | Aktie -5-8%, Vola 2× |

Beispiel: `[COMPANY] beats Q3 earnings but lowers full-year guidance, citing macroeconomic headwinds.` → Earnings +3% aber Guidance -8% = Netto -5%.

**Credit Rating Changes (Company-Level):**
| Variante | Sentiment | Effekt |
|---|---|---|
| Upgrade (z.B. BBB → A) | +0.2 | Aktie +2-4%, niedrigere Schuldzinsen |
| Downgrade (z.B. A → BBB) | -0.3 | Aktie -3-6%, höhere Schuldzinsen |
| Downgrade to Junk (BBB → BB) | -0.6 | Aktie -10-20%, Institutionelle MÜSSEN verkaufen (Mandate verbieten Junk), Vola 2.5× |
| Rating Watch Negative | -0.2 | Aktie -2-4%, Warnsignal |

Beispiel-Headlines:
- `Moody's downgrades [COMPANY] to junk status, citing deteriorating cash flow.`
- `S&P upgrades [COMPANY] to A-rating after strong deleveraging.`

**Activist Investor Kampagnen:**
- Ein AI-Investor nimmt öffentlich eine große Position und fordert Änderungen
- `Activist investor [NAME] discloses [X]% stake in [COMPANY], demands [CHANGE].`
- Typische Forderungen: Board-Sitze, CEO-Wechsel, Spin-Off, Kostensenkung, Buyback
- Effekt: Aktie +5-15% (Markt erwartet positive Veränderung)
- Follow-ups: Company Resists (-3%), Company Agrees (+5%), Proxy Fight (-2% Unsicherheit)

**Special Dividends:**
- Einmalige Extra-Dividende (nicht die reguläre quartalsweise)
- Trigger: Firma hat ungewöhnlich viel Cash, oder verkauft einen Geschäftsbereich
- `[COMPANY] announces special dividend of $[AMOUNT] per share.`
- Effekt: Aktie +2-5% bei Ankündigung, dann Ex-Dividend-Abschlag am Ex-Date
- Größe: $5-$50 pro Aktie (deutlich mehr als reguläre Dividende)

**IPO Lock-Up Expiration:**
- 90-180 Tage nach einem IPO dürfen Insider ihre Aktien verkaufen
- 10 Tage vorher: `[SYMBOL] IPO lock-up expires on [DATE]. Insiders may sell [X]M shares.`
- Am Expiration-Tag: häufig Verkaufsdruck (-5-15%) durch Insider-Verkäufe
- Effekt: Preis -5-15%, Volumen 3-5× normal, erhöhte Vola für 3-5 Tage
- Nicht jedes Mal schlimm — manchmal verkaufen Insider gar nicht (positives Signal)

**Index-Inclusion / Index-Exclusion:**
- Wenn eine Aktie in den Hauptindex aufgenommen wird, MÜSSEN alle passiven Fonds (Index/ETF AI) kaufen
- Ankündigung (10 Tage vorher): `[SYMBOL] to be added to Market Index, effective [DATE].`
- Effekt bis zum Inclusion-Tag: Preis +5-15% (Vorfreude + passive Fonds kaufen)
- Am Tag selbst: hoher Volumen-Spike, danach Normalisierung
- Umgekehrt bei Exclusion: Preis -5-10% (passive Fonds müssen verkaufen)
- Trigger: Aktie wächst in die Top-Marktkapitalisierungen (automatisch) oder schrumpft raus

#### 8.2.6 Rechtliche & Regulatorische Events

**Class Action Lawsuit:**
- Aktionäre verklagen ein Unternehmen (z.B. nach Betrug, irreführenden Aussagen)
- Ankündigung: `Class action lawsuit filed against [COMPANY] alleging [REASON].`
- Effekt: Aktie -5-15%, Vola 2×, 5-10 Tage
- Follow-up (30-90 Tage später): Settlement (`[COMPANY] agrees to $XXM settlement`) oder Dismissal (`Court dismisses class action against [COMPANY]`)
- Settlement: leicht negativ (-2-3%, Kosten), aber Unsicherheit endet → Vola sinkt
- Dismissal: positiv (+5-8%, Erleichterung)

**Antitrust / Kartellverfahren:**
- Regulierer untersucht Marktmachtmissbrauch
- `Regulators launch antitrust probe into [COMPANY]'s [MARKET] dominance.`
- Effekt: Aktie -8-15%, Sektor -1-2%, dauert lange (30-90 Tage Unsicherheit)
- Follow-up: Fine (Geldstrafe), Structural Remedy (Unternehmensteile abstoßen), oder Clearance (freigesprochen)

#### 8.2.7 Unternehmensstruktur-Events

**Spin-Off:**
- Ein Unternehmen spaltet einen Geschäftsbereich als eigenständige Aktie ab
- Ankündigung (20 Tage vorher): `[COMPANY] announces spin-off of [DIVISION] as new publicly traded company [NEW_COMPANY].`
- Am Spin-Off-Tag:
  - Eine neue Aktie erscheint im Markt (wie ein IPO, aber bestehende Aktionäre bekommen Anteile)
  - Der Preis der Muttergesellschaft fällt um den Wert des abgespaltenen Teils (mechanisch, wie ein Dividend-Abschlag)
  - Spieler die Aktien der Muttergesellschaft halten: bekommen automatisch Aktien der neuen Firma (anteilig)
- Effekt: Oft langfristig positiv für beide Teile ("pure play" Bewertungen sind höher)
- Frequenz: Selten, 1-2 pro Jahr (Spielzeit)
- **Auswirkung auf Short-Positionen bei Spin-Off:** Spieler mit Short-Positionen in der Muttergesellschaft schulden dem Verleiher die neuen Spin-Off-Aktien. Die Short-Position wird adjustiert: der Spieler erhält eine zusätzliche Short-Position in der neuen Firma (proportional). Toast: `Spin-off: You now have a short position in [NEW_SYMBOL] due to your short in [PARENT_SYMBOL].`

**Tender Offer (Übernahmeangebot):**
- Ein Unternehmen (oder Investor) bietet allen Aktionären an, ihre Aktien zu einem festen Preis zu kaufen (üblicherweise mit Premium)
- `[ACQUIRER] launches tender offer for [TARGET] at $[PRICE] per share ([PREMIUM]% premium).`
- **Spieler-Entscheidung:** Wenn der Spieler Aktien des Targets besitzt, bekommt er ein Popup:
  ```
  ┌────────────────────────────────────────────┐
  │         TENDER OFFER                        │
  │                                            │
  │  [ACQUIRER] is offering $[PRICE] per       │
  │  share for your [TARGET] holdings.         │
  │                                            │
  │  Current market price: $[MARKET_PRICE]     │
  │  Offer price: $[OFFER_PRICE]               │
  │  Premium: +[PREMIUM]%                      │
  │                                            │
  │  Your shares: [QTY]                        │
  │  Total payout: $[TOTAL]                    │
  │                                            │
  │  Offer expires: [DATE]                     │
  │                                            │
  │  [ACCEPT OFFER]     [DECLINE / HOLD]       │
  └────────────────────────────────────────────┘
  ```
- **Accept:** Aktien werden zum Angebotspreis verkauft (garantiert, kein Slippage)
- **Decline:** Spieler behält die Aktien. Riskant: wenn genug Aktionäre akzeptieren, wird die Firma übernommen und die Restaktien werden erzwungen zum gleichen Preis eingetauscht. Aber: manchmal scheitert das Angebot → Preis fällt zurück.
- Follow-ups: Competing Offer (höheres Gegenangebot), Regulatory Block, Deal Completion, Deal Failure

**Chapter 11 vs. Chapter 7 Bankruptcy:**

Bisher nur "Bankruptcy" als Event — jetzt differenziert:

**Chapter 11 (Reorganisation):**
- Unternehmen ist in Schwierigkeiten, meldet Insolvenz an, versucht sich zu restrukturieren
- `[COMPANY] files for Chapter 11 bankruptcy protection.`
- Effekt: Aktie -50-80%, aber wird NICHT sofort delisted
- Handel geht weiter (oft als "Penny Stock" unter $1)
- Dauer: 60-180 Spieltage
- Follow-ups:
  - Successful Restructuring (20% Chance): Unternehmen kommt zurück, Preis erholt sich auf 30-50% des Vorkrisenniveaus
  - Conversion to Chapter 7 (40% Chance): Liquidation → Delisting
  - Acquisition out of Bankruptcy (40% Chance): ein anderes Unternehmen kauft → Aktionäre bekommen minimalen Payout

**Chapter 7 (Liquidation):**
- Unternehmen wird aufgelöst, Vermögen verteilt an Gläubiger
- `[COMPANY] files for Chapter 7 bankruptcy. Liquidation proceedings begin.`
- Effekt: Aktie -90-99%, Handel für 10 Tage möglich (Spieler kann noch verkaufen), dann Delisting
- Aktionäre bekommen typischerweise NICHTS (Gläubiger zuerst)
- Spieler-Position wird wertlos → Toast: `Your position in [SYMBOL] has been written off. Loss: -$X,XXX`

#### 8.2.8 Marktstruktur-Events

**Flash Crash:**
- Auslöser: mehrere Algo-Trader verkaufen gleichzeitig, Liquidität verschwindet kurzfristig
- Dauer: 2-10 Minuten (Spielzeit)
- Effekt: Marktindex fällt 3-7% in Minuten, erholt sich dann zu 70-90%
- UI: `FLASH CRASH` Banner rot blinkend, Spiel pausiert automatisch
- Chance: ca. einmal alle 200-500 Spieltage (selten!)

**Circuit Breaker:**
- Auslöser: eine Aktie fällt >10% innerhalb von 5 Minuten, oder der Marktindex fällt >7%
- Effekt (Einzelaktie): Handel in dieser Aktie wird für 30 Minuten (Spielzeit) ausgesetzt
- Effekt (Marktindex): ALLE Aktien werden für 15 Minuten ausgesetzt (Level 1: -7%), für 1 Stunde (Level 2: -13%), oder für den Rest des Tages (Level 3: -20%)
- UI: `TRADING HALTED` Badge bei betroffener Aktie (rot, blinkend), Orders werden rejected
- News: `Trading halted in [SYMBOL] after 12% drop triggers circuit breaker.` oder `Market-wide circuit breaker triggered. All trading suspended for 15 minutes.`

**IPO (Initial Public Offering):**
- Neue Aktie wird dem Markt hinzugefügt
- Frequenz: alle 30-60 Spieltage
- Ankündigung 5 Tage vorher: `[NEW COMPANY] files for IPO, expected to list at $XX-$XX.`
- Am IPO-Tag: Aktie erscheint in der Aktienliste, oft mit starkem Opening-Day-Spike (+10-50%)
- Der Spieler kann am IPO-Tag zum Marktpreis kaufen (kein Pre-IPO-Zugang)

**Delisting:**
- Eine Aktie wird vom Markt genommen (nach Bankrott oder regulatorischer Entscheidung)
- 10 Handelstage Vorwarnung: `[SYMBOL] faces delisting. Trading will be suspended on [DATE].`
- Preis fällt typischerweise auf <$1
- Spieler mit Positionen werden gezwungen zu verkaufen (zum letzten Preis) oder verlieren die Position
- Aktie verschwindet aus allen Listen

### 8.3 Event-Effekte (Detail)

Jeder Event hat einen definierten Effektsatz:

**Sofortige Effekte (bei Auslösung):**
- **Price Gap:** Der Preis springt sofort (beim nächsten Tick). Wird als Gap im Chart sichtbar, besonders wenn der Markt geschlossen war.
- **Volatilitäts-Spike:** Volatilitäts-Multiplikator wird sofort angewendet.
- **Volumen-Spike:** Tagesvolumen-Multiplikator.

**Graduelle Effekte (über Tage):**
- **Drift-Änderung:** Der langfristige Trend ändert sich für X Tage. Z.B. nach Earnings Beat: positiver Drift für 5 Tage, der langsam abklingt.
- **Sentiment-Shift:** AI-Trader-Stimmung ändert sich (bullish/bearish).
- **Abklingfunktion:** Effekte schwächen sich exponentiell ab. Formel: `Effekt(Tag) = InitialEffekt × e^(-DecayRate × Tag)`

**Stärke-Skala:**
| Stufe | Preis-Impact | Volatilitäts-Mult. | Dauer | UI-Behandlung |
|---|---|---|---|---|
| Minor | ±0.5-2% | 1.2× | 1-2 Tage | Toast-Notification |
| Moderate | ±2-5% | 1.5× | 3-5 Tage | Toast + News-Ticker |
| Major | ±5-15% | 2.0× | 5-10 Tage | Breaking News Popup |
| Catastrophic | ±15-50%+ | 3.0× | 10-30 Tage | Breaking News Popup + Auto-Pause |

### 8.4 Event-Kaskadierung

Events können Folge-Events auslösen:

**Beispiel-Kaskade:**
1. `Company X: Accounting fraud discovered` (Catastrophic, -40%)
2. → 80% Chance: `Company X: CEO resigns amid scandal` (Major, -10%)
3. → 60% Chance: `Sector: Regulatory investigation into accounting practices sector-wide` (Moderate, Sektor -3%)
4. → 30% Chance: `Macro: SEC announces stricter reporting requirements` (Minor, Markt -1%)

**Implementierung:**
- Jeder Event-Template hat ein Array von `FollowUpTemplates` mit Wahrscheinlichkeiten
- Folge-Events werden 1-5 Tage nach dem Auslöser-Event gescheduled
- Maximale Kaskadierungstiefe: 4 (verhindert Endlos-Ketten)
- Maximal 2 Follow-ups pro Event (verhindert exponentielle Explosion)

### 8.5 Event-Scheduling

**Geplante Events (vorhersehbar):**
- **Earnings Season:** Alle 63 Spieltage (quartalsweise, ~63 Handelstage pro Quartal) gibt es eine "Earnings Season", in der 30-50% aller Unternehmen Quartalszahlen berichten. Über 2-3 Wochen (Spielzeit) verteilt. Der Spieler kann im Kalender sehen, wann welches Unternehmen berichtet.
- **Zentralbank-Meetings:** Alle 45-60 Spieltage. Ankündigung 7 Tage vorher.
- **Wirtschaftsdaten:** Jobs Report (monatlich), CPI (monatlich), GDP (quartalsweise). Feste Intervalle, ±2 Tage Variation.

**Zufällige Events:**
- Skandale, Unfälle, Produktlaunches, Übernahmen, IPOs
- Frequenz: 2-5 zufällige Events pro Spieltag (bei 500 Aktien)
- Gewichtet: Minor-Events häufiger, Catastrophic-Events selten (1 alle 50-100 Tage)

**Event-Dichte-Steuerung:**
- Mindestens 1 Event pro Spieltag (kein "toter" Tag)
- Maximal 10 Events pro Spieltag (keine Überflutung)
- Wenn die Queue leer ist und 2 Tage kein Event stattfand: Force-Generate ein Minor/Moderate Event
- Wenn 5+ Events an einem Tag: verschiebe Minor-Events auf den nächsten Tag

### 8.6 Event-UI

Siehe Kapitel 3.7 (Modale Dialoge) für Breaking News Popup und Toast-Notifications.

Zusätzlich:

**Event-Kalender (im News-Tab):**
- Sidebar rechts im News-Tab: `Upcoming Events`
- Zeigt geplante Events der nächsten 14 Tage
- Format: `Mar 18: AAPL Earnings Report`, `Mar 22: Fed Interest Rate Decision`
- Der Spieler kann sich vorbereiten (Positionen aufbauen oder reduzieren)

---

## 9. Regulierung, Illegale Handlungen & Konsequenzen

### 9.1 Übersicht & Philosophie

In der realen Welt überwacht die SEC (Securities and Exchange Commission) den Markt. Illegale Handlungen wie Insiderhandel, Marktmanipulation und Spoofing werden verfolgt und bestraft. In StockSim gibt es eine simulierte Regulierungsbehörde — die **SMA (StockSim Market Authority)** — die das Spielerverhalten überwacht.

**Design-Philosophie:**
- Der Spieler KANN illegale Handlungen begehen — das Spiel verhindert sie nicht
- Aber es gibt ein Risiko-/Belohnungs-System: illegale Strategien sind profitabel, aber der Spieler kann erwischt werden
- Konsequenzen reichen von Geldstrafen über Handelsbeschränkungen bis zum "Game Over" (Konto eingefroren)
- Das System ist KEIN moralischer Zeigefinger — es ist eine Gameplay-Mechanik
- Der Spieler lernt, wie reale Marktregulierung funktioniert
- Manche illegalen Taktiken sind subtil genug, dass die SMA sie nur schwer erkennt — das belohnt geschicktes Vorgehen

### 9.2 Die SMA (StockSim Market Authority)

**Hinweis:** Die Abkürzung 'SMA' steht in diesem Kapitel für die StockSim Market Authority (Regulierungsbehörde). Im Kontext von Charts und technischer Analyse (Kapitel 12) bezeichnet SMA den Simple Moving Average — einen technischen Indikator. Die Bedeutung ergibt sich aus dem Kontext.

Die SMA ist ein Backend-System, das das Handelsverhalten des Spielers kontinuierlich analysiert.

**Suspicion Score (Verdachts-Punktzahl):**
Jeder Spieler hat einen unsichtbaren Suspicion Score (0-100):
- 0-20: Unauffällig (normal)
- 21-40: Beobachtung (SMA wird aufmerksam)
- 41-60: Warnung (SMA leitet Voruntersuchung ein)
- 61-80: Untersuchung (formelle Untersuchung, Handelsbeschränkungen)
- 81-100: Anklage (schwere Strafen, mögliches "Game Over")

**Der Score ist dem Spieler NICHT direkt sichtbar.** Er sieht nur indirekte Signale:
- Bei 20+: gelegentliche News: `SMA reports increased surveillance of unusual trading activity.` (generisch, nicht auf den Spieler bezogen)
- Bei 40+: direkte Warnung: `⚠ SMA Notice: Your trading activity in [SYMBOL] is being reviewed.` — gelbe Warnung im Portfolio
- Bei 60+: formelle Benachrichtigung: `🔴 SMA Investigation: You are under formal investigation for suspected [VIOLATION].` — rotes Banner, Handelsbeschränkungen
- Bei 80+: Anklage: `SMA Enforcement: You have been charged with [VIOLATION].` — Strafe wird verhängt

**Score-Abbau:** Der Score sinkt langsam über Zeit wenn der Spieler "sauber" handelt: -1 Punkt pro 5 Spieltage ohne verdächtige Aktivität. Bei aktiver Untersuchung: kein Abbau bis die Untersuchung abgeschlossen ist.

**UI-Anzeige (subtil):**
- Kein numerischer Score sichtbar (das wäre zu gamey)
- Stattdessen: ein kleines Schild-Icon (`shield`) in der Top Bar, normalerweise `text-disabled` (grau, unsichtbar fast)
- Bei Score 20-40: Icon wird `text-secondary` (etwas sichtbarer)
- Bei Score 40-60: Icon wird `warning` (gelb)
- Bei Score 60+: Icon wird `red-primary` (rot), pulsierend
- Hover auf Icon: Tooltip `Regulatory Status: Clear` / `Under Review` / `Under Investigation` / `Enforcement Action Pending`
- Klick auf Icon: öffnet SMA-Panel mit Details (aktive Warnungen, vergangene Strafen)

### 9.3 Illegale Handlungen im Detail

#### 9.3.1 Insider Trading (aktiv)

**Was der Spieler tun kann:**
Der Spieler erhält gelegentlich "Rumors" (siehe 4.8). Wenn er basierend auf einem Rumor handelt und dieser Rumor sich als wahr herausstellt, hat er effektiv Insider-gehandelt.

**Erkennung durch SMA:**
- Trigger: Spieler kauft/verkauft BEVOR ein Event öffentlich wird UND profitiert davon
- Bedingungen: Trade < 3 Tage vor Event + Position wurde nach Event geschlossen + Gewinn > $1,000
- Erkennungswahrscheinlichkeit: 15-40% (abhängig von Trade-Größe — große Trades fallen mehr auf)
- Score-Zuschlag: +10 bis +25 (je nach Gewinn)

**Beispiel-Ablauf:**
1. Spieler sieht Rumor: `Sources suggest NovaPharma may announce positive trial results.`
2. Spieler kauft 1,000 Aktien @ $20
3. 2 Tage später: Event `NovaPharma announces successful Phase 3 trial results.` → Preis steigt auf $32
4. Spieler verkauft → Gewinn $12,000
5. SMA-Algorithmus erkennt: Pre-Event-Kauf, großer Gewinn, Timing verdächtig
6. 30% Chance: `SMA Notice: Your trades in NVPH prior to the clinical trial announcement are being reviewed.` → +15 Score

#### 9.3.2 Market Manipulation (Pump & Dump durch den Spieler)

**Was der Spieler tun kann:**
Bei Micro/Small Caps kann der Spieler genug Kapital haben, um den Preis signifikant zu bewegen:
1. Kauft aggressiv eine Micro Cap (Preis steigt durch eigenen Order-Impact)
2. Retail-AI sieht steigenden Preis → FOMO kauft mit → Preis steigt weiter
3. Spieler verkauft am Top → Gewinn

**Erkennung durch SMA:**
- Trigger: Spieler kauft >10% des Tagesvolumens einer Aktie, Preis steigt >15%, Spieler verkauft innerhalb von 5 Tagen
- Erkennungswahrscheinlichkeit: 20-50% (Small Caps werden stärker überwacht)
- Score-Zuschlag: +15 bis +30

**Variation — Pump ohne Dump:**
Wenn der Spieler kauft und HÄLT (nicht verkauft), ist es kein Pump & Dump, sondern eine "große Position". Das ist legal. Die SMA erkennt nur den Dump (schneller Verkauf nach starkem Anstieg).

#### 9.3.3 Spoofing

**Was der Spieler tun kann:**
Große Orders platzieren, die das Orderbook beeinflussen (andere AI sehen die Order und reagieren), und dann die Order canceln bevor sie ausgeführt wird.

**Mechanik:**
1. Spieler platziert große Limit Buy Order weit unter dem aktuellen Preis (z.B. 10,000 Aktien @ $95 wenn der Preis bei $100 ist)
2. Die Order erscheint im Orderbook → AI-Trader sehen starken "Support" bei $95
3. Einige AI kaufen, weil sie den Support als bullish interpretieren
4. Preis stabilisiert sich oder steigt leicht
5. Spieler cancelt die Order → der Support verschwindet
6. Alternativ: große Sell-Order oberhalb des Preises ("Fake Wall") um den Preis zu drücken

**Erkennung durch SMA:**
- Trigger: Spieler platziert Order und cancelt >80% seiner großen Orders (>5× Durchschnittsvolumen) innerhalb von 10 Minuten
- Erkennungswahrscheinlichkeit: 25-60% (Spoofing wird in der Realität aggressiv verfolgt)
- Score-Zuschlag: +10 pro Spoofing-Vorfall

#### 9.3.4 Wash Trading

**Was der Spieler tun kann:**
Kaufen und sofort wieder verkaufen (oder umgekehrt), um künstliches Volumen zu erzeugen und den Eindruck von Aktivität zu erwecken.

**Mechanik:**
1. Spieler kauft 500 Aktien von AAPL
2. Spieler verkauft sofort 500 Aktien von AAPL
3. Volumen wird erhöht, aber der Spieler hat keinen direkten Gewinn
4. Nutzen: kann AI-Trader triggern, die auf Volumen-Spikes reagieren (Algo-Trader, Retail)

**Erkennung durch SMA:**
- Trigger: Spieler kauft und verkauft die gleiche Aktie innerhalb von 5 Minuten ohne signifikante Preisänderung, wiederholt (>3× am Tag)
- Erkennungswahrscheinlichkeit: 40-70% (einfach zu erkennen)
- Score-Zuschlag: +8 pro Vorfall

#### 9.3.5 Cornering the Market

**Was der Spieler tun kann:**
So viele Aktien einer kleinen Firma kaufen, dass er einen erheblichen Teil des Free Float kontrolliert.

**Mechanik:**
1. Spieler kauft kontinuierlich eine Micro/Small Cap
2. Bei >5% der umlaufenden Aktien: regulatorische Meldepflicht (News: `Filing: [PLAYER NAME] reports 5.1% stake in [SYMBOL].`) — das ist LEGAL
3. Bei >10%: erhöhte Aufmerksamkeit, breiterer Spread (Market Maker werden vorsichtig)
4. Bei >20%: SMA-Warnung, Short-Seller könnten sich zurückziehen (Short Squeeze Risiko), Preis steigt wegen verringertem Free Float
5. Bei >30%: der Spieler kontrolliert effektiv den Markt für diese Aktie

**Erkennung:**
- Die Position >5% ist automatisch öffentlich (wie SEC 13D Filing in der Realität)
- Das Aufbauen der Position ist legal — problematisch wird es erst, wenn der Spieler seine Marktmacht missbraucht (z.B. den Preis drückt und dann billig nachkauft)
- Score-Zuschlag: 0 für das bloße Halten, +20 bei Nachweis von Manipulation

**Gameplay-Effekt:** Der Spieler wird de facto zum "Activist Investor" — er kann den Preis mit seinen Trades signifikant beeinflussen. Gefährlich: wenn der Preis fällt, sitzt er auf einer riesigen Position und kann nicht schnell genug verkaufen (illiquide).

#### 9.3.6 Front Running

**Was der Spieler tun kann:**
Muster in institutionellem Orderflow erkennen und vorher handeln.

**Mechanik:**
- Der Spieler beobachtet ungewöhnliches Volumen (Insider-AI oder Institutionelle bauen eine Position auf)
- Er kauft bevor die Position vollständig aufgebaut ist
- Der Preis steigt weiter durch die verbleibenden institutionellen Käufe
- Der Spieler verkauft mit Gewinn

**Erkennung:**
- Front Running ist in der Realität nur illegal für Broker/Berater die Kundenorders sehen — nicht für Retail-Trader die öffentliche Informationen nutzen
- Im Spiel: KEINE SMA-Strafe. Front Running basierend auf Marktbeobachtung ist **legale Alpha-Generierung**
- Das ist ein Skill — der Spieler wird belohnt, wenn er Volumen-Anomalien erkennt

#### 9.3.7 Bear Raid

**Was der Spieler tun kann:**
Aggressives Shorten einer Small Cap um den Preis zu drücken, besonders wenn die Aktie bereits angeschlagen ist.

**Mechanik:**
1. Spieler identifiziert eine schwache Aktie (nach negativem Event, hohe Verschuldung)
2. Shortet aggressiv → Preis fällt durch Order-Impact
3. Retail-Panic-Seller sehen den Fall → verkaufen auch → Preis fällt weiter
4. Spieler covert am Tief → Gewinn

**Erkennung:**
- Trigger: Spieler baut große Short-Position auf (>5% Short Interest allein) UND Preis fällt >10% am gleichen Tag
- Erkennungswahrscheinlichkeit: 20-40%
- Score-Zuschlag: +12 bis +20
- Zusätzlich: das betroffene Unternehmen kann eine "Market Manipulation Complaint" als Event auslösen

#### 9.3.8 Naked Short Selling

**Was der Spieler tun kann:**
Normalerweise muss der Spieler Aktien leihen um zu shorten (Borrow). Naked Shorting = Shorten ohne Borrow.

**Mechanik:**
- Im Standard-Modus: nicht möglich. Spieler muss Borrow-Verfügbarkeit haben.
- Bei bestimmten Micro Caps mit sehr niedrigem Liquiditäts-Score (<3): das System erlaubt "versehentlich" einen Naked Short, wenn die Borrow-Verfügbarkeit auf 0 fällt NACHDEM der Spieler die Position eröffnet hat (Borrow wird zurückgerufen)
- **Forced Buy-In:** Wenn das Borrow zurückgerufen wird, hat der Spieler 3 Spieltage um die Position zu covern. Danach: erzwungenes Cover zum Marktpreis.
- Naked Shorting ist keine aktive Spieler-Wahl, sondern ein Risiko-Event

### 9.4 Konsequenzen-System

#### 9.4.1 Warnung (Score 40-60)

**SMA Warning Letter:**
- Toast + Eintrag im SMA-Panel: `The SMA has noted unusual trading patterns in your recent activity regarding [SYMBOL]. This is a formal notice. No action is required at this time, but continued unusual activity may result in further review.`
- **Gameplay-Effekt:** Keine Einschränkungen. Nur ein Signal an den Spieler, dass er beobachtet wird.
- **Spieler kann reagieren:** Aufhören mit verdächtigem Verhalten → Score sinkt wieder

#### 9.4.2 Formelle Untersuchung (Score 60-80)

**SMA Investigation Notice:**
- Breaking-News-Popup: `🔴 SMA INVESTIGATION: The StockSim Market Authority has opened a formal investigation into your trading activity in [SYMBOL(S)] for suspected [VIOLATION].`
- Spiel pausiert automatisch
- **Gameplay-Effekte:**
  - **Handels-Einschränkung:** Für die betroffene(n) Aktie(n): keine neuen Positionen eröffnen, nur bestehende schließen (90 Spieltage)
  - **Erhöhte Margin-Anforderung:** +50% auf alle Positionen (als "Risiko-Aufschlag")
  - **Öffentliche Bekanntmachung:** News: `SMA investigating trader for suspected [VIOLATION] in [SYMBOL].` — dies kann den Aktienkurs beeinflussen (negativ bei Manipulation, AI-Trader werden vorsichtiger)
  - Untersuchung dauert 30-60 Spieltage. Danach: Freispruch (20% Chance wenn Score sinkt) oder Strafe (80%)

#### 9.4.3 Geldstrafe (Score 60-80, nach Untersuchung)

**SMA Fine:**
- `SMA Enforcement: You have been fined $[AMOUNT] for [VIOLATION].`
- **Strafberechnung:**
  - Basis: 2× der geschätzten illegalen Gewinne
  - Minimum: $10,000
  - Maximum: $500,000 (oder 20% des Gesamtportfolio-Werts, was höher ist)
  - Wird direkt vom Cash abgezogen
  - Wenn nicht genug Cash: Positionen werden erzwungen verkauft bis die Strafe gedeckt ist
- **Zusätzlich:** Handelsverbot für die betroffene Aktie für 90 Spieltage

**Beispiele:**
- Insider Trading, Gewinn $12,000 → Strafe $24,000
- Pump & Dump, Gewinn $50,000 → Strafe $100,000
- Spoofing (kein direkter Gewinn messbar) → Pauschal $25,000

#### 9.4.4 Schwere Strafe (Score 80-100)

**SMA Enforcement Action:**
- Breaking-News-Popup: `🔴 SMA ENFORCEMENT: Severe penalties imposed for [VIOLATION].`
- **Gameplay-Effekte:**
  - **Geldstrafe:** 3× der illegalen Gewinne (Minimum $50,000)
  - **Trading Ban:** Kein Handel für 30 Spieltage. Der Spieler kann nur zuschauen wie seine Positionen sich bewegen (Stop-Losses und Limit-Orders bleiben aktiv, aber keine neuen Orders)
  - **Margin-Entzug:** Kein Margin-Trading mehr für 180 Spieltage (muss alle Margin-Schulden sofort zurückzahlen)
  - **Öffentliche Bekanntmachung:** Große News, kann den Markt beeinflussen

#### 9.4.5 "Game Over" Szenario (Score 100)

**SMA Account Freeze (extremster Fall):**
- Tritt nur ein bei wiederholten schweren Verstößen (3+ Enforcement Actions)
- `YOUR ACCOUNT HAS BEEN FROZEN BY THE SMA.`
- Alle Positionen werden zum Marktpreis liquidiert
- Gesamte Strafe wird berechnet
- Spieler bekommt den Rest als "Final Cash Balance"
- **Spielstand wird NICHT gelöscht** — der Spieler kann von diesem Punkt aus weiterspielen mit dem verbliebenen Cash, aber:
  - Kein Margin für 360 Spieltage
  - Kein Short Selling für 180 Spieltage
  - Maximale Positionsgröße auf 5% des Portfolios pro Aktie begrenzt für 180 Spieltage
  - Das ist der "Neuanfang" mit Handicap

### 9.5 Legale Graubereiche

Nicht alles, was verdächtig aussieht, ist illegal. Das System muss unterscheiden:

| Handlung | Legal? | SMA-Reaktion |
|---|---|---|
| Große Position aufbauen (>5% Filing) | Ja | Meldepflicht (automatisch), kein Score |
| Auf Rumors handeln (ohne Insider-Wissen) | Ja | Kein Score — Rumor war öffentlich |
| Volumen-Muster erkennen und vorher kaufen | Ja | Kein Score — das ist Marktbeobachtung |
| Short Selling (normal) | Ja | Kein Score |
| Aggressiv kaufen und Preis bewegen | Grauzone | Score nur wenn gefolgt von schnellem Verkauf |
| Day-Trading (viele Trades/Tag) | Ja | Kein Score, solange kein Muster von Manipulation |
| Auf Earnings-Gerüchte spekulieren | Ja | Kein Score — Gerüchte sind öffentlich |
| Große Order platzieren und stehen lassen | Ja | Kein Score — nur Cancellation-Muster ist verdächtig |

### 9.6 Steuern (Capital Gains Tax)

**Grundmechanik:**
Der Spieler zahlt Steuern auf realisierte Gewinne. Steuern sind ein Gameplay-Element, das langfristiges Halten belohnt.

**Steuersätze:**
| Haltedauer | Steuersatz | Label |
|---|---|---|
| < 30 Spieltage | 35% | Short-Term Capital Gains (hohes Steuerniveau) |
| 30-180 Spieltage | 25% | Mid-Term |
| > 180 Spieltage | 15% | Long-Term Capital Gains (vergünstigter Satz) |

**Berechnung:**
- Steuern fallen NUR auf realisierte Gewinne an (beim Verkauf/Cover)
- Verluste werden gegengerechnet (Tax Loss Harvesting möglich)
- Netto-Verlust: keine Steuer, Verlust wird vorgetragen
- Steuer wird am Ende jedes Quartals (90 Spieltage) automatisch vom Cash abgezogen
- 10 Spieltage vorher: Warnung `Tax payment of $X,XXX due in 10 days.`

**Anzeige:**
- Im Portfolio-Tab: `Unrealized Tax Liability: $X,XXX` (geschätzter Steuerbetrag wenn alle Positionen jetzt geschlossen würden)
- Im Analytics-Tab: `Tax Paid (YTD): $X,XXX` und `Tax Rate (Effective): XX%`
- Bei jeder Trade-Bestätigung: `Estimated tax impact: $XXX (short-term rate)`

**Tax Loss Harvesting (Strategie):**
Der Spieler kann Verlust-Positionen verkaufen um den Steuer-Score zu senken:
- Verlust realisieren → reduziert die Steuerlast
- Dann ggf. die gleiche Aktie zurückkaufen (siehe Wash-Sale-Regel unten)

**Wash-Sale-Regel (vereinfacht):** Wenn der Spieler eine Aktie mit Verlust verkauft und die gleiche Aktie innerhalb von 5 Spieltagen zurückkauft, wird der Verlust steuerlich NICHT anerkannt. Der Verlust wird stattdessen auf die Kostenbasis der neuen Position aufgeschlagen. Dies verhindert triviales Tax-Loss-Harvesting-Exploiting. UI: Bei Rückkauf innerhalb von 5 Tagen: Warnung `Wash sale detected: Tax loss of $XXX disallowed and added to cost basis.`

**Dividenden-Besteuerung:** Dividenden werden ebenfalls besteuert. Steuersatz: pauschal 15% auf alle Dividenden-Einnahmen (unabhängig von Haltedauer). Abzug erfolgt automatisch bei Dividendenausschüttung. Anzeige: `Dividend received: $150.00 (after 15% tax: $127.50)`.

**Bei unzureichendem Cash für Steuerzahlung:** Wenn der Spieler am Steuertag nicht genug Cash hat: (1) Warnung 10 Tage vorher: `Upcoming tax payment: $X,XXX. You currently have insufficient cash.` (2) Am Steuertag: automatischer Verkauf der kleinsten/liquidesten Position(en) bis die Steuerschuld gedeckt ist. (3) Toast: `Tax payment: Sold XX shares of [SYMBOL] to cover $X,XXX tax liability.` (4) Wenn selbst nach Verkauf aller Positionen die Schuld nicht gedeckt ist: Restschuld wird als negativer Cash-Stand geführt (quasi Steuer-Margin) — muss innerhalb von 30 Tagen beglichen werden, sonst SMA-Strafe.

**Setting:** `Enable Taxes` — Toggle, Standard AUS bei Easy, AN bei Normal/Hard. Bei AUS: keine Steuern (einfacherer Modus).

### 9.7 Regulatorische Events

Die SMA und regulatorische Entwicklungen können auch den Markt beeinflussen (unabhängig vom Spieler):

**Neue Event-Typen:**

| Event | Sentiment | Effekt |
|---|---|---|
| SMA announces crackdown on short selling | -0.2 (Short Seller) | Short Interest sinkt, Short-Aktien steigen |
| New regulations tighten margin requirements | -0.3 | Margin-Handel wird eingeschränkt, gehebelte Positionen unter Druck |
| SMA investigates [AI-COMPANY] for fraud | -0.5 | Betroffene Aktie -15-30%, Sektor -2-3% |
| Class action lawsuit filed against [COMPANY] | -0.4 | Aktie -8-15%, Vola 2× |
| SMA clears [COMPANY] of all charges | +0.3 | Aktie +5-10% (Erleichterungsrally) |
| New insider trading penalties announced | 0.0 | Kein Preiseffekt, aber Score-Sensitivität steigt |
| Regulatory approval for [PRODUCT/MERGER] | +0.5 | Aktie +10-20% |
| Antitrust investigation launched | -0.3 | Aktie -5-10% |

---

## 10. Zeitsystem

### 10.1 HOI4-Referenz und Adaption

Das Zeitsystem orientiert sich an Hearts of Iron 4: Der Spieler kontrolliert den Zeitfluss vollständig. Er kann jederzeit pausieren, die Simulation analysieren und dann die Zeit mit verschiedenen Geschwindigkeiten laufen lassen.

**Unterschied zu HOI4:** In HOI4 vergeht die Zeit in Stunden/Tagen. In StockSim vergeht die Zeit in Minuten der Börsenzeit. Ein "Tick" bei 1x Geschwindigkeit entspricht 1 Spielminute. Der Handelstag hat 390 Minuten (9:30 AM - 4:00 PM = 6.5 Stunden).

### 10.2 Geschwindigkeitsstufen

#### 10.2.1 Pause

- Die Simulation stoppt vollständig. Kein Preis ändert sich, keine AI handelt, keine Zeit vergeht.
- Der Spieler kann: Charts analysieren, News lesen, Watchlist bearbeiten, Portfolio studieren, Orders vorbereiten.
- Orders, die während Pause platziert werden, werden bei Fortsetzen im nächsten Tick ausgeführt.
- Visuelles Feedback: `PAUSED` Text über dem Zentralbereich (siehe 3.2.3). Pause-Button pulsiert.

#### 10.2.2 1x Geschwindigkeit (Standard)

- 1 Tick = 1 Spielminute = 1 echte Sekunde
- Der Spieler erlebt den Handelstag in Echtzeit-ähnlichem Tempo
- 390 Ticks pro Handelstag = 6.5 Minuten Echtzeit pro Handelstag
- Geeignet für: aktives Day-Trading, Beobachtung einzelner Aktien, erstes Kennenlernen

#### 10.2.3 2x Geschwindigkeit

- 1 Tick = 1 Spielminute = 0.5 echte Sekunden
- 390 Ticks pro Handelstag = ~3.25 Minuten Echtzeit
- Geeignet für: schnelleres Day-Trading, Warten auf Limit Order Ausführung

#### 10.2.4 5x Geschwindigkeit

- 1 Tick = 1 Spielminute = 0.2 echte Sekunden
- 390 Ticks pro Handelstag = ~1.3 Minuten Echtzeit
- Geeignet für: Swing-Trading (Positionen über mehrere Tage), Warten auf Events
- Bei dieser Geschwindigkeit werden Chart-Updates gebatcht (nicht jeder Tick → neues Chart-Update, sondern alle 5 Ticks)

#### 10.2.5 10x Geschwindigkeit

- 1 Tick = 1 Spielminute = 0.1 echte Sekunden
- 390 Ticks pro Handelstag = ~39 Sekunden Echtzeit
- Geeignet für: Langfrist-Strategien, Warten auf Earnings Season, schnelles Vorspulen
- Chart-Updates alle 10 Ticks, Watchlist-Updates alle 5 Ticks
- Performance-kritisch: Backend muss 10 vollständige Ticks pro Sekunde schaffen

### 10.3 Zeitsteuerungs-UI

Siehe Kapitel 3.2.3 für das detaillierte Layout.

**Tastenkürzel:**
- `Leertaste`: Pause / Fortsetzen (Toggle)
- `1`: Geschwindigkeit 1x
- `2`: Geschwindigkeit 2x
- `3`: Geschwindigkeit 5x
- `4`: Geschwindigkeit 10x
- `+` oder `Pfeil-Rechts`: eine Stufe schneller
- `-` oder `Pfeil-Links`: eine Stufe langsamer

**Wichtig:** Bei Geschwindigkeitswechsel ändert sich die Tick-Rate sofort. Es gibt keinen Übergang.

### 10.4 Spielzeit-Darstellung

**Format:** `Tue, Mar 15 2027 — 2:32 PM`

**Tickende Uhr:** Die Uhrzeit tickt sichtbar mit. Bei 1x: jede Sekunde eine neue Minute. Bei 10x: rasante Uhränderung.

**Markt-Status-Indikator:**
- `Pre-Market` (blau) — 7:00-9:30 AM (Extended Hours Trading: nur Limit Orders, siehe 5.5)
- `Market Open` (grün, 10px) — 9:30 AM - 4:00 PM
- `After-Hours` (orange) — 4:00-8:00 PM (Extended Hours Trading: nur Limit Orders, siehe 5.5)
- `Market Closed` (rot) — 8:00 PM - 7:00 AM
- `Weekend` (grau) — Samstag/Sonntag

**Startdatum:** Das Spiel beginnt an einem zufälligen Montag in einem zufälligen Jahr (z.B. "Mon, Jan 6 2025"). Das genaue Datum ist irrelevant — es gibt keine realen Events.

### 10.5 Technische Tick-Architektur

**Was passiert in einem Tick (Reihenfolge):**
1. `AdvanceTime()` — Spielzeit um 1 Minute erhöhen
2. `CheckMarketStatus()` — Ist der Markt offen? Wenn nein: nur Events prüfen, keine Trades
3. `ProcessEvents()` — Event-Queue prüfen, fällige Events auslösen
4. `AIDecisions()` — Alle AI-Trader entscheiden (wenn ihr Entscheidungstimer fällig ist)
5. `MatchOrders()` — Alle neuen Orders (AI + Spieler) gegen Orderbook matchen
6. `CalculatePrices()` — Neue Preise aus Basis-Modell + Order-Impact berechnen
7. `UpdateOrderbook()` — Bid/Ask aktualisieren
8. `CheckMargins()` — Margin-Anforderungen prüfen für Spieler und AI
9. `BuildUpdatePacket()` — Daten für Frontend zusammenstellen
10. `SendUpdate()` — WebSocket-Nachricht ans Frontend

**Performance-Budget pro Tick:**

| Geschwindigkeit | Max. Tick-Dauer | Budget |
|---|---|---|
| 1x | 1000ms (1s) | Großzügig |
| 2x | 500ms | Komfortabel |
| 5x | 200ms | Muss effizient sein |
| 10x | 100ms | Kritisch — Performance-Optimierung nötig |

**Wenn Tick-Berechnung zu lange dauert (>Budget):**
- Das Spiel verlangsamt sich automatisch (Frame-Dropping, nicht Tick-Skipping)
- Ticks werden NIE übersprungen — das würde Orders verpassen
- Bei anhaltenden Performance-Problemen: Toast-Warnung `Simulation running slower than target speed.`

**WebSocket-Message-Batching:**
| Geschwindigkeit | Update-Frequenz an Frontend |
|---|---|
| 1x | Jeder Tick (1/s) |
| 2x | Jeder Tick (2/s) |
| 5x | Alle 2 Ticks (2.5/s) |
| 10x | Alle 5 Ticks (2/s) |

Bei höherer Geschwindigkeit werden weniger Updates gesendet, dafür enthält jedes Update die akkumulierten Daten. Preise zeigen nur den letzten Wert, Charts bekommen die OHLCV-Daten für den gesamten Zeitraum seit dem letzten Update.

### 10.6 Tagesrhythmus-System (Off-Hours-Handling)

Außerhalb der Handelszeiten passiert nichts Handelbares. Statt den Spieler durch Leerlauf sitzen zu lassen, wird die Zeit intelligent überbrückt.

**Der Handelstag als Gameplay-Einheit:**

```
┌─────────────────────────────────────────────────┐
│  ① PRE-MARKET PHASE (kurz, ~10 Min Spielzeit)   │
│  → Overnight-Events anzeigen (als Karten)       │
│  → Gaps sichtbar in Charts                      │
│  → Extended-Hours-Trading möglich (Limit only)  │
│  → MOO-Orders können platziert werden           │
├─────────────────────────────────────────────────┤
│  ② MARKET OPEN  🔔                              │
│  → Voller Handel, volle Speed-Kontrolle         │
│  → 6.5 Stunden Spielzeit (9:30-16:00)           │
├─────────────────────────────────────────────────┤
│  ③ AFTER-HOURS PHASE (kurz, ~10 Min Spielzeit)  │
│  → After-Hours-Events (Earnings!)               │
│  → Extended-Hours-Trading möglich               │
│  → MOC-Ergebnisse sichtbar                      │
├─────────────────────────────────────────────────┤
│  ④ DAILY SUMMARY (automatisch, Spiel pausiert)  │
│  → Tages-Zusammenfassung                        │
│  → Overnight-Events (falls vorhanden)           │
│  → Vorschau auf morgen                          │
│  → [CONTINUE TO NEXT DAY]                       │
│  → Skip zur nächsten Pre-Market-Phase            │
└─────────────────────────────────────────────────┘
```

**Kein Leerlauf:** Die Nacht (20:00-7:00) wird NICHT simuliert. Nach dem Daily Summary springt die Zeit direkt zur nächsten Pre-Market-Phase. Der Spieler sitzt nie vor einer tickenden Uhr ohne Handelsmöglichkeit.

**Wochenende:** Freitag-Daily-Summary enthält einen `Weekend Events` Abschnitt. Dann springt die Zeit direkt zu Montag Pre-Market. Kein Warten.

#### 10.6.1 Daily Summary Screen

Erscheint automatisch nach der After-Hours-Phase. Spiel pausiert.

**Layout:**

```
┌────────────────────────────────────────────────────┐
│           📊 DAILY SUMMARY — Tue, Mar 15           │
│                                                    │
│  YOUR PERFORMANCE            MARKET                │
│  ───────────────             ──────                │
│  Day P&L: +$1,234 (+2.1%)   Index: +0.8%          │
│  Total Value: $63,456        Vol: 4.2B             │
│  Trades: 8 (6 wins, 2 loss) Volatility: Normal    │
│  ✓ You outperformed the market today               │
│                                                    │
│  TOP MOVERS                                        │
│  ▲ NVPH  +12.3%  Earnings beat expectations        │
│  ▼ PTVE   -8.1%  Oil pipeline incident             │
│  ▲ VTXD   +5.4%  Analyst upgrade                   │
│  ▼ STCG   -3.2%  Sector rotation                   │
│                                                    │
│  ⚡ AFTER-HOURS / OVERNIGHT EVENTS                 │
│  🔴 ACLS: Earnings miss — EPS $1.20 vs $1.55 exp. │
│     After-hours: -11.3%. Expected gap down.        │
│  🟢 Fed minutes suggest possible rate pause        │
│     Futures indicate +0.3% at open.                │
│                                                    │
│  📅 TOMORROW                                       │
│  📊 VTXD Earnings (before market open)             │
│  📊 SWAI Earnings (after close)                    │
│  🏛️ Weekly Jobs Report (8:30 AM)                  │
│                                                    │
│  [CONTINUE TO NEXT DAY]    [REVIEW PORTFOLIO]      │
└────────────────────────────────────────────────────┘
```

**Interaktion:**
- `Continue to Next Day`: springt direkt zur Pre-Market-Phase des nächsten Handelstages
- `Review Portfolio`: wechselt zum Portfolio-Tab (Spiel bleibt pausiert), von dort `Continue` Button
- Der Spieler kann im Daily Summary auch Orders für den nächsten Tag platzieren (MOO-Orders)
- Escape schließt den Summary nicht — der Spieler MUSS "Continue" klicken (damit er die Events nicht verpasst)

**Styling:**
- Modal: 700 × 550px, zentriert, `bg-secondary`
- Überschrift: `Inter Bold`, 20px
- Performance-Zahlen: `JetBrains Mono Bold`, farbig
- Outperformance-Indikator: `✓ You outperformed the market` in `green-primary` oder `✗ Market outperformed you` in `red-primary`
- After-Hours Events: farbcodiert (🔴/🟢), klickbar → navigiert zur Aktie

#### 10.6.2 Weekend Summary

Am Freitag ist der Daily Summary erweitert:

```
┌────────────────────────────────────────────────────┐
│         📊 WEEKLY SUMMARY — Fri, Mar 19            │
│                                                    │
│  YOUR WEEK                   MARKET                │
│  Week P&L: +$3,456 (+5.8%)  Index: +1.2%          │
│  Best Day: Tuesday (+$1,800) Worst: Thursday (-$200)│
│  Trades: 34 (Win Rate: 68%)                        │
│                                                    │
│  ⚡ WEEKEND EVENTS                                  │
│  🔴 [COUNTRY] announces trade sanctions on [SECTOR]│
│  🟢 Analyst: "Markets look healthy going into Q2"  │
│                                                    │
│  📅 WEEK AHEAD                                     │
│  Mon: AAPL Earnings, Consumer Confidence Report    │
│  Tue: Fed Interest Rate Decision (2:00 PM)         │
│  Wed: CPI Inflation Data                           │
│  Thu: 3 Earnings Reports                           │
│  Fri: Jobs Report                                  │
│                                                    │
│  [CONTINUE TO MONDAY]       [REVIEW PORTFOLIO]     │
└────────────────────────────────────────────────────┘
```

### 10.7 Automatische Pause-Trigger

Das Spiel pausiert automatisch bei bestimmten Ereignissen (alle konfigurierbar in Settings):

| Trigger | Standard | Setting |
|---|---|---|
| Breaking News (Major/Catastrophic) | AN | `Auto-pause on breaking news` |
| Margin Call | AN | `Auto-pause on margin call` |
| Short Squeeze Warning | AN | `Auto-pause on short squeeze` |
| Order ausgeführt | AUS | `Auto-pause on order execution` |
| Market Open | AUS | `Auto-pause on market open` |
| Market Close | AUS | `Auto-pause on market close` |
| Tutorial-Schritte | AN (im Tutorial) | Nicht konfigurierbar |

Bei Auto-Pause: das Spiel hält an, der Auslöser wird angezeigt (Popup/Toast), der Spieler setzt manuell fort.

---

## 11. Aktien- und Sektor-Generierung

### 11.1 Prozedurale Generierung Übersicht

Beim Starten eines neuen Spiels generiert das C#-Backend den gesamten Markt: Sektoren, Aktien, Fundamentaldaten, Startpreise und historische Preisdaten.

**Seed-System:**
- Jedes neue Spiel hat einen zufälligen Seed (32-bit Integer)
- Der Spieler kann optional einen Seed manuell eingeben (für reproduzierbare Märkte)
- Gleicher Seed = identische Aktien, Namen, Startpreise
- Die laufende Simulation ist NICHT deterministisch (wegen AI-Zufall), nur die Startbedingungen

**Marktgrößen-Voreinstellungen:**

| Preset | Aktien | ETFs | REITs | Sektoren | Empfohlen für | RAM |
|---|---|---|---|---|---|---|
| Small | 250 | 9 | 5 | 8 | Schwache PCs, schnelles Spiel | ~200 MB |
| Standard | 1.000 | 13 | 15 | 12 | Empfohlen | ~400 MB |
| Large | 2.500 | 13 | 30 | 12 | Starke PCs, maximale Vielfalt | ~700 MB |
| Massive | 5.000 | 13 | 50 | 12 | Enthusiasten, Tiered Simulation aktiv | ~1.2 GB |
| Custom | 100-10.000 | auto | auto | 8-12 | Slider mit Performance-Warnung | variabel |

**Small Market: Ausgeschlossene Sektoren:** Bei 8 Sektoren werden die 4 kleinsten entfernt: Telecommunications, Utilities, Luxury Goods und Transportation. Diese haben die geringste Marktkapitalisierung und am wenigsten einzigartige Gameplay-Mechaniken.

### 11.2 Sektoren

#### 11.2.1 Sektor-Liste (12 Sektoren)

| # | Sektor | Englischer Name | Typische Volatilität | Wachstum |
|---|---|---|---|---|
| 1 | Technologie | Technology | Hoch | Hoch |
| 2 | Energie | Energy | Hoch | Mittel |
| 3 | Finanzen | Financials | Mittel | Mittel |
| 4 | Gesundheit | Healthcare | Mittel-Hoch | Hoch |
| 5 | Konsumgüter | Consumer Goods | Niedrig-Mittel | Mittel |
| 6 | Industrie | Industrials | Mittel | Mittel |
| 7 | Materialien | Materials | Mittel-Hoch | Niedrig |
| 8 | Immobilien | Real Estate | Mittel | Niedrig |
| 9 | Telekommunikation | Telecommunications | Niedrig | Niedrig |
| 10 | Versorger | Utilities | Niedrig | Niedrig |
| 11 | Luxusgüter | Luxury Goods | Mittel | Mittel |
| 12 | Transport | Transportation | Mittel | Mittel |

#### 11.2.2 Sektor-Eigenschaften

Jeder Sektor hat statische Eigenschaften, die die Generierung und Simulation beeinflussen:

```
Sector {
  Name: string
  BaseVolatility: float (0.1-0.5) — Basis für alle Aktien im Sektor
  GrowthBias: float (-0.001 bis +0.003) — langfristiger Trend pro Tick
  InterestSensitivity: float (-1 bis +1) — wie stark Zinsänderungen wirken
  SeasonalEffect: float[] — 12 Werte für jeden Monat (z.B. Retail boomt in Q4)
  TypicalMarketCapRange: [min, max] — Milliarden $
  IntraSectorCorrelation: float (0.3-0.7) — wie ähnlich sich Aktien im Sektor bewegen
}
```

### 11.3 Aktien-Generierung

#### 11.3.1 Name und Symbol

**Namensgenerierung:**
Firmennamen werden aus Bausteinen zusammengesetzt, die sektorspezifisch sind:

**Technology:**
- Prefixes: Quantum, Vertex, Nova, Cyber, Nexus, Apex, Synth, Pixel, Cloud, Data, Neural, Helix, Vortex, Cipher, Logic, Nano, Byte, Flux, Tera, Photon
- Suffixes: Dynamics, Systems, Technologies, Labs, Solutions, AI, Logic, Ware, Networks, Soft, Tech, Digital, Computing, Robotics, Intelligence
- Patterns: `[Prefix][Suffix]` oder `[Prefix] [Suffix]` oder `[Prefix][Suffix] Inc.`
- Beispiele: "VertexDynamics", "NovaSoft Inc.", "Quantum Logic Systems", "CipherByte Technologies"

**Energy:**
- Prefixes: Petro, Solar, Volt, Hydro, Geo, Wind, Fuel, Terra, Ion, Atom, Flux, Ember, Radiant, Neon, Thermal, Plasma, Arc, Dynamo, Surge, Inferno
- Suffixes: Energy, Power, Resources, Oil, Gas, Corp, Renewables, Fuels, Petroleum, Electric, Solar, Dynamics, Generation, Holdings, Services
- Beispiele: "PetroVolt Energy", "SolarWind Corp", "TerraPower Resources", "PlasmaArc Generation"

**Financials:**
- Prefixes: Capital, First, Global, Premier, Trust, Crown, Sterling, Pacific, Atlantic, Meridian, Pinnacle, Vanguard, Sovereign, Liberty, Patriot, Crest, Summit, Heritage, Eagle, Fortress
- Suffixes: Bank, Financial, Holdings, Capital, Group, Trust, Securities, Advisors, Investments, Wealth, Asset Management, Partners, Bancorp, Finance, Corp
- Beispiele: "FirstTrust Holdings", "Sterling Capital Group", "Pacific Securities", "MeridianCrest Wealth"

**Healthcare:**
- Prefixes: Bio, Nova, Medi, Vita, Pulse, Neura, Cell, Genome, Helix, Immuno, Pharma, Cardio, Synapse, Tera, Oxi, Proto, Geno, Astra, Zen, Cryo
- Suffixes: Pharma, Therapeutics, Sciences, Biotech, Labs, Medical, Diagnostics, Health, Genomics, Life Sciences, BioSciences, Cure, Medics, Rx
- Beispiele: "BioGenix Labs", "NovaPharma Inc.", "HelixCure Sciences", "PulsePoint Medical"

**Consumer Goods:**
- Prefixes: Bright, Prime, Fresh, Urban, Ever, Home, Pure, Daily, Golden, Nature, Harvest, Bloom, Clear, Swift, True, Craft, Simple, Nest, Grove, Maple
- Suffixes: Brands, Products, Foods, Consumer, Essentials, Goods, Co., Corp, Industries, Lifestyle, Home, Market, Living, Direct, Supply
- Beispiele: "BrightLeaf Brands", "FreshHarvest Foods", "PureCraft Consumer", "GoldenShelf Brands"

**Industrials:**
- Prefixes: Iron, Steel, Forge, Titan, Atlas, Apex, Core, Prime, Granite, Bolt, Arc, Matrix, Omega, Vanguard, Summit, Ridge, Anchor, Axis, Zenith, Delta
- Suffixes: Industries, Manufacturing, Engineering, Heavy Industries, Works, Fabrication, Machinery, Industrial, Solutions, Systems, Corp, Construction, Dynamics, Automation
- Beispiele: "TitanForge Industries", "ApexCore Manufacturing", "GraniteBolt Engineering", "VanguardAxis Systems"

**Materials:**
- Prefixes: Terra, Geo, Crystal, Ore, Mineral, Carbon, Alloy, Stone, Metal, Prism, Element, Cobalt, Nickel, Silica, Lumen, Quarry, Bedrock, Onyx, Zinc, Copper
- Suffixes: Materials, Mining, Resources, Metals, Minerals, Chemical, Composites, Industries, Extraction, Corp, Elements, Raw Materials, Alloys, Processing
- Beispiele: "TerraCrystal Mining", "CarbonAlloy Materials", "PrismElement Resources", "BedrockOnyx Mining"

**Real Estate:**
- Prefixes: Crown, Harbor, Summit, Urban, Metro, Skyline, Park, Beacon, Crest, Estate, Haven, Tower, Pinnacle, Gate, Stone, Heritage, River, Bay, Lakeview, Grand
- Suffixes: Realty, Properties, Real Estate, Holdings, Development, Estates, Trust, Capital, Group, REIT, Land, Property, Residential, Commercial
- Beispiele: "CrownHarbor Realty", "SkylineCrest Properties", "MetroPark Development", "BeaconTower REIT"

**Telecommunications:**
- Prefixes: Signal, Wave, Link, Net, Tele, Beam, Fiber, Pulse, Echo, Relay, Orbit, Spectrum, Grid, Freq, Band, Sync, Data, Wire, Core, Omni
- Suffixes: Communications, Telecom, Networks, Wireless, Connect, Broadband, Media, Signal, Systems, Corp, Mobile, Digital, Comm, Tech
- Beispiele: "SignalWave Communications", "FiberLink Telecom", "SpectrumGrid Networks", "OmniBeam Wireless"

**Utilities:**
- Prefixes: Power, Grid, Hydro, Volt, Amp, Current, Flow, Source, Green, Clean, Civic, Metro, National, Central, United, Pacific, Northern, Southern, Western, Eastern
- Suffixes: Utilities, Power, Electric, Energy, Water, Gas, Services, Utility, Corp, Holdings, Light, Generation, Distribution, Municipal
- Beispiele: "GridVolt Utilities", "CleanSource Power", "MetroFlow Electric", "PacificAmp Energy"

**Luxury Goods:**
- Prefixes: Prestige, Royal, Maison, Luxe, Elite, Noble, Grand, Imperial, Regal, Opulent, Crown, Sterling, Sovereign, Platinum, Premier, Avant, Haute, Bel, Celeste, Artisan
- Suffixes: Luxury, Group, Brands, Collection, Maison, House, Atelier, Design, International, Corp, Lifestyle, Premium, Couture, Holdings
- Beispiele: "PrestigeRoyal Group", "MaisonLuxe Brands", "EliteNoble Collection", "SterlingCrown Luxury"

**Transportation:**
- Prefixes: Trans, Global, Swift, Rapid, Fleet, Cargo, Express, Rail, Aero, Maritime, Voyage, Route, Track, Velocity, Freight, Hub, Pacific, Atlantic, Cross, United
- Suffixes: Transport, Logistics, Shipping, Freight, Airlines, Rail, Corp, Transit, Carriers, Lines, Express, Mobility, Haulage, Distribution
- Beispiele: "SwiftGlobal Logistics", "RapidFleet Transport", "AeroVoyage Airlines", "CrossRoute Freight"

**Namensgenerator-Kapazität:**
Mit ~20 Prefixes × ~15 Suffixes × 3 Namenspatterns pro Sektor = ~900 mögliche Namen pro Sektor. Bei 12 Sektoren: **~10.800 mögliche einzigartige Firmennamen**. Das reicht für den Large Market (1.000 Aktien) mit enormer Variation und ohne Wiederholungen.

**Namenspatterns:**
1. `[Prefix][Suffix]` — z.B. 'VertexDynamics'
2. `[Prefix] [Suffix]` — z.B. 'Nova Logic Systems'
3. `[Prefix][Suffix] Inc.` / `[Prefix][Suffix] Corp.` — z.B. 'NovaSoft Inc.'
4. `[Prefix] [Prefix] [Suffix]` — z.B. 'Quantum Logic Systems' (zwei Prefixes + Suffix, seltener)

**Symbol-Generierung:**
Symbole werden aus 3-5 Buchstaben abgeleitet:
1. Anfangsbuchstaben aller Wörter: 'Nova Logic Systems' → 'NLS'
2. Anfang + Konsonanten: 'VertexDynamics' → 'VTXD'
3. Gekürzt: 'BioGenix Labs' → 'BGXL'
4. Bei Duplikaten: Buchstabe anhängen oder variieren
5. Seltene Buchstaben (X, Z, Q) werden bevorzugt für Tech/Biotech-Firmen

#### 11.3.2 Fundamentaldaten

Jede Aktie bekommt bei Generierung Fundamentaldaten zugewiesen:

```
StockFundamentals {
  MarketCap: float — in Milliarden $
  Revenue: float — Jahresumsatz
  NetIncome: float — Jahresgewinn
  PERatio: float — Preis/Gewinn-Verhältnis (berechnet, nicht gesetzt)
  DividendYield: float — 0% bis 6%
  DebtToEquity: float — 0 bis 3.0
  RevenueGrowth: float — -20% bis +50% p.a.
  Employees: int — 100 bis 500,000
}
```

**Verteilung der Marktkapitalisierung:**
Die Verteilung folgt einem Potenzgesetz (wenige große, viele kleine):
- Mega Cap (>$100B): ~2% der Aktien
- Large Cap ($10-100B): ~8%
- Mid Cap ($2-10B): ~20%
- Small Cap ($300M-2B): ~40%
- Micro Cap (<$300M): ~30%

**KGV (P/E Ratio):**
- Berechnet: Marktkapitalisierung / Nettogewinn
- Typische Range: 5-50 (Growth-Aktien höher, Value-Aktien niedriger)
- Einige Aktien sind unprofitabel (negatives KGV, angezeigt als `N/A`)

**Dividendenrendite:**
- 60% der Aktien zahlen keine Dividende (besonders Growth und Small Caps)
- 30% zahlen 0.5-3% (solide Unternehmen)
- 10% zahlen 3-6% ("Dividend Aristocrats", Utilities, Real Estate)
- Dividenden werden quartalsweise ausgezahlt und dem Cash gutgeschrieben

**Ex-Dividend-Date Mechanik:**
- Jede Dividend-Aktie hat ein festes Dividendenzahlungs-Intervall (quartalsweise)
- 5 Tage vor Auszahlung: Ankündigung `[COMPANY] declares quarterly dividend of $X.XX per share. Ex-date: [DATE].`
- **Ex-Dividend Date:** An diesem Tag fällt der Aktienpreis automatisch um den Dividendenbetrag
  - Beispiel: Aktie $100, Dividende $1.50 → öffnet bei ~$98.50 am Ex-Date
  - Dies ist ein mechanischer Preisabfall, KEIN Event — es ist die Standard-Marktmechanik
- **Record Date:** 1 Tag nach Ex-Date. Wer am Record Date die Aktie besitzt, bekommt die Dividende
- **Payment Date:** 10-20 Tage nach Record Date. Dividende wird dem Cash gutgeschrieben
- **Spieler-UI:** In Aktien-Detail: `Next Dividend: $1.50/share | Ex-Date: Mar 25 | Yield: 2.4%`
- **Spieler kauft VOR Ex-Date:** bekommt Dividende, aber Preis fällt → netto neutral (wie in der Realität)
- **Spieler kauft NACH Ex-Date:** keine Dividende für dieses Quartal, aber kauft zum günstigeren Preis
- **Short-Seller:** Muss am Ex-Date die Dividende an den Verleiher ZAHLEN (aus Cash abgezogen). Toast: `Dividend payment due on short position: -$XXX.XX`
- **Anzeige im Earnings-Kalender:** Ex-Dates werden im Upcoming Events Kalender angezeigt

**Dividend Reinvestment (DRIP):** Optional per Aktie aktivierbar. Wenn aktiv, wird die Dividenden-Ausschüttung automatisch zum Marktpreis in zusätzliche Aktien (Fractional Shares) der gleichen Firma reinvestiert. Aktivierung: Rechtsklick auf Position → `Enable DRIP` / `Disable DRIP`. UI: im Portfolio bei DRIP-Aktien: kleines `DRIP`-Badge. Setting: `Default DRIP for new positions` Toggle in Settings (Standard: AUS).

#### 11.3.3 Handels-Parameter

```
StockTradingParams {
  StartPrice: float — $1 bis $5,000 (abgeleitet von MarketCap)
  BaseVolatility: float — 0.005 bis 0.05 (tägliche %-Schwankung)
  BaseDailyVolume: int — 1,000 bis 50,000,000
  LiquidityScore: int — 1 bis 10
  ShortBorrowAvailability: float — 0.5 bis 1.0 (wie leicht shortbar)
  FairValue: float — initialer "fairer Wert" für Mean Reversion
  SharesOutstanding: int — 10M bis 5B
  InsiderOwnership: float — 0.05 bis 0.30 (5-30%)
  InstitutionalOwnership: float — 0.20 bis 0.70 (20-70%)
  Float: int — berechnet aus Outstanding × (1 - InsiderLockup)
  FloatPercentage: float — Float / Outstanding (40-85%)
}
```

**Startpreis-Logik:**
- Mega Caps: $100-$5,000
- Large Caps: $30-$300
- Mid Caps: $10-$80
- Small Caps: $2-$30
- Micro Caps: $0.50-$10 (Penny Stocks unter $5 haben besondere Eigenschaften: höhere Vola, breitere Spreads)

#### 11.3.4 Aktien-"Persönlichkeit" (Traits)

Jede Aktie bekommt 1-3 Traits zugewiesen, die ihr Verhalten prägen. Bei 1.000 Aktien sorgen 25 verschiedene Traits für ausreichend Variation.

| # | Trait | Effekt | Typisch für |
|---|---|---|---|
| 1 | Blue Chip | Niedrige Vola, stabiler positiver Drift, hohe Liquidität, zuverlässige Dividende | Mega/Large Caps |
| 2 | Growth Stock | Höhere Vola, starker positiver Drift, kein Dividend, hohes KGV | Tech, Healthcare |
| 3 | Value Stock | Niedrige Vola, niedriges KGV, moderate Dividende, unterschätzt vom Markt | Financials, Industrials |
| 4 | Dividend Aristocrat | Stabile quartalsweise Dividende (>3%), sehr niedrige Vola, langweilig aber zuverlässig | Utilities, Consumer, Real Estate |
| 5 | Speculative | Hohe Vola, kein klarer Trend, anfällig für Pump&Dump und Gerüchte | Small/Micro Caps |
| 6 | Volatile | Überdurchschnittliche Vola in beide Richtungen, große Intraday-Swings | Energy, Biotech, Micro Caps |
| 7 | Penny Stock | Preis unter $5, sehr hohe Vola, illiquide, breiter Spread, Delisting-Risiko | Micro Caps |
| 8 | Market Leader | Höchste MarketCap im Sektor, stärkster Einfluss auf Sektor-Index | Eine pro Sektor |
| 9 | Momentum Stock | Trends halten länger an (schwächere Mean Reversion), gut für Swing-Trading | Growth, Tech |
| 10 | Defensive | Bewegt sich weniger in Krisen (niedriges Beta <0.7), stabil in Bear Markets | Utilities, Healthcare, Consumer Staples |
| 11 | Cyclical | Stark abhängig vom Wirtschaftszyklus, steigt in Expansion, fällt in Contraction | Industrials, Materials, Luxury |
| 12 | Turnaround | Firma war in Schwierigkeiten, erholt sich langsam. Hohes Risiko, hohes Potenzial. | Jeder Sektor |
| 13 | Cash Cow | Hoher Free Cashflow, stabile Einnahmen, oft Buyback-Programme | Large Caps, Consumer |
| 14 | Debt Heavy | Hohe Verschuldung (Debt/Equity >2.0), sehr zinssensitiv, Insolvenz-Risiko | Jeder Sektor |
| 15 | Fast Grower | Umsatzwachstum >30% p.a., oft unprofitabel, extremes Potenzial oder Totalverlust | Tech, Healthcare |
| 16 | Slow Grower | Umsatzwachstum <5%, stabil, langweilig, geringe Vola, vorhersehbar | Utilities, Telco |
| 17 | Acquisition Target | Firma ist ein wahrscheinliches Übernahmeziel (kleine MarketCap + wertvolle Assets/Technologie) | Small/Mid Caps |
| 18 | Serial Acquirer | Firma kauft regelmäßig andere Firmen (Events: Übernahmen). Preis schwankt um Deal-Ankündigungen. | Large Caps, Tech, Healthcare |
| 19 | ESG Leader | Hohe Umwelt-/Sozial-/Governance-Wertung, zieht ESG-Fonds an, resilient gegen Skandal-Events | Jeder Sektor |
| 20 | Controversy Magnet | Anfällig für Skandale, Klagen, regulatorische Probleme. Höheres Event-Risiko. | Tech, Energy, Financials |
| 21 | IPO Fresh | Kürzlich per IPO gelistet (<180 Tage). Lock-Up-Expiration steht bevor. Hohe Vola. | Jeder Sektor |
| 22 | Insider Favorite | Hoher Insider-Ownership (>20%). Insider-Trading-Events wahrscheinlicher. | Small/Mid Caps |
| 23 | Short Target | Hohes Short Interest (>15%), fundamentale Schwächen, Activist Short Seller interessiert | Jeder Sektor |
| 24 | Seasonal | Starke saisonale Muster (z.B. Retail boomt in Q4, Reisen in Q2/Q3) | Consumer, Luxury, Transport |
| 25 | Compounder | Kontinuierliches Gewinnwachstum 10-20% p.a. über Jahre, steigt stetig ohne große Ausschläge | Jeder Sektor |

**Trait-Zuweisung:**
- Mega/Large Caps: 2-3 Traits (komplexere Persönlichkeiten)
- Mid Caps: 1-2 Traits
- Small/Micro Caps: 1-2 Traits
- Einige Traits schließen sich gegenseitig aus (z.B. Blue Chip + Penny Stock, Growth + Slow Grower)
- Traits können sich im Laufe des Spiels ÄNDERN: ein Fast Grower kann zum Slow Grower werden wenn das Wachstum nachlässt. Ein Turnaround kann zum Growth Stock werden. Dies passiert durch Events und fundamentale Veränderungen (alle 60-180 Spieltage wird für jede Aktie geprüft ob Traits noch passen).

#### 11.3.5 Weitere Wertpapierarten

Neben normalen Aktien (Common Stock) existieren drei weitere handelbare Instrumente:

**A) ETFs (Exchange Traded Funds)**

ETFs bilden einen Index oder Sektor ab und sind wie Aktien handelbar.

**Generierung:** Pro Sektor wird 1 ETF erstellt + 1 Gesamtmarkt-ETF = 13 ETFs.

| ETF | Bildet ab | Beispielname | Symbol |
|---|---|---|---|
| Market ETF | Gesamtmarkt-Index | StockSim Total Market ETF | SMKT |
| Tech ETF | Technology-Sektor-Index | Technology Select ETF | XTEC |
| Energy ETF | Energy-Sektor-Index | Energy Select ETF | XNRG |
| (etc. für jeden Sektor) | | | |

**Mechanik:**
- Preis wird berechnet aus dem gewichteten Durchschnitt aller Aktien im Index/Sektor
- Kein eigenes Preismodell — der ETF-Preis folgt dem Index exakt (kein Tracking Error im MVP)
- Kaufen/Verkaufen wie eine normale Aktie (Market, Limit, etc.)
- NICHT shortbar (zu komplex für MVP)
- Keine Dividende (thesaurierend — Dividenden der enthaltenen Aktien werden reinvestiert)
- Spread: sehr eng (0.01-0.05%), hohe Liquidität
- Ideal für Anfänger die einen ganzen Sektor kaufen wollen statt einzelne Aktien zu analysieren
- Ideal für Diversifikation mit einem Trade

**UI:** ETFs erscheinen in der Market-Tabelle mit einem speziellen Badge `ETF` neben dem Symbol. Im Screener filterbar: `Type: Stock / ETF / All`.

**B) REITs (Real Estate Investment Trusts)**

REITs sind Immobilien-Unternehmen mit besonderen Regeln.

**Generierung:** 10-20% der Real-Estate-Sektor-Aktien werden als REITs generiert (bei 500 Aktien: ~4-8 REITs).

**Besondere Mechanik:**
- **Pflichtdividende:** REITs MÜSSEN mindestens 90% ihrer Gewinne als Dividende ausschütten → sehr hohe Dividendenrendite (4-8%)
- **Zinssensitivität:** REITs sind EXTREM zinssensitiv. Zinserhöhung = REIT-Preise fallen stark. Zinssenkung = starker Anstieg.
- **Niedrige Volatilität** im Normalfall, aber hohe Vola um Zinsentscheidungen herum
- **Trait:** Automatisch `Dividend Aristocrat` + `Cyclical`

**UI:** REITs haben ein `REIT` Badge im Symbol. Dividendenrendite wird prominent angezeigt.

**C) SPACs (Special Purpose Acquisition Companies)**

SPACs sind leere Firmenhüllen die an die Börse gehen, um eine private Firma zu übernehmen.

**Generierung:** Alle 60-120 Spieltage wird 1 SPAC per IPO gelistet (selten).

**Lifecycle eines SPACs:**
1. **IPO:** SPAC wird gelistet bei $10.00 (immer). Name: generisch (z.B. 'Apex Acquisition Corp', 'Nova Capital Holdings')
2. **Suchphase (60-180 Tage):** Preis schwankt minimal um $10 (±5%). Gerüchte können den Preis bewegen.
3. **Target-Ankündigung:** SPAC kündigt an, welche Firma übernommen wird. → Preis springt +20-50% (oder fällt -10% wenn der Markt das Target nicht mag).
4. **Merger-Abschluss (30-60 Tage später):** SPAC verschwindet, wird zur normalen Aktie mit neuem Namen und Symbol. Preis basiert auf dem Wert der übernommenen Firma.
5. **Alternativ: Kein Target gefunden (20% Chance):** SPAC wird nach 365 Tagen liquidiert. Aktionäre bekommen $10 pro Aktie zurück (verlieren nur Opportunitätskosten).

**Spieler-Strategie:**
- Kaufen bei $10, hoffen auf gutes Target → +20-50% Gewinn
- Risiko begrenzt: $10 Rückzahlung bei Scheitern (max Verlust = Kaufpreis - $10 wenn über $10 gekauft)
- Spekulativ: auf Gerüchte handeln welches Target übernommen wird

**UI:** SPACs haben ein `SPAC` Badge. Im Aktien-Detail: Lifecycle-Status angezeigt ('Searching for target', 'Target announced: [NAME]', 'Merger pending').

#### 11.3.6 Firmen-Identität & Profil-Generierung

Jede generierte Firma bekommt ein vollständiges Profil, das sie einzigartig und lebendig macht. Alle Daten werden prozedural generiert.

**Profil-Datenstruktur:**
```
CompanyProfile {
  // Basis (bereits definiert in 11.3.1-11.3.3)
  Name, Symbol, Sector, MarketCap, Fundamentals, Traits

  // NEU: Identität
  Description: string — 2-3 Sätze, was die Firma macht
  FoundedYear: int — 1950-2025 (ältere Firmen = stabiler, jüngere = volatiler)
  Headquarters: string — generierte Stadt + Region
  CEO: string — generierter Name
  CFO: string — generierter Name
  CTO: string — generierter Name (nur bei Tech/Healthcare)
  Employees: int — 50 bis 500,000 (korreliert mit MarketCap)
  Products: string[] — 2-4 generierte Produktnamen
  Competitors: string[] — 3-5 andere Firmen im gleichen Sektor mit ähnlicher MarketCap
  RiskFactors: string[] — 2-4 generierte Risikofaktoren
  Narrative: string — 2-3 Sätze über aktuelle Situation/Trend
}
```

**Beschreibungs-Generierung (Description):**
Jeder Sektor hat eigene Beschreibungs-Templates:

**Technology:**
- `[NAME] develops [PRODUCT_TYPE] for [CUSTOMER_TYPE]. The company's flagship product, [PRODUCT_1], [PRODUCT_CLAIM].`
- PRODUCT_TYPE: 'cloud-based enterprise software', 'AI-powered analytics tools', 'cybersecurity solutions', 'semiconductor chips', 'mobile applications', 'SaaS platforms', 'IoT infrastructure', 'blockchain solutions', 'data management systems', 'developer tools'
- CUSTOMER_TYPE: 'enterprise clients worldwide', 'Fortune 500 companies', 'small and medium businesses', 'government agencies', 'healthcare providers', 'financial institutions'
- PRODUCT_CLAIM: 'is used by over [X] clients', 'has seen [X]% adoption growth', 'leads the market in [NICHE]', 'was recently recognized as an industry leader'

**Energy:**
- `[NAME] operates [OPERATION_TYPE] across [REGION]. The company [ACTIVITY] and serves [CUSTOMER_BASE].`
- OPERATION_TYPE: 'oil and gas exploration assets', 'renewable energy installations', 'natural gas pipelines', 'solar farm networks', 'offshore drilling platforms', 'wind farm portfolios', 'nuclear power facilities', 'hydrogen production plants'
- REGION: 'North America', 'the Gulf Coast', 'Western Europe', 'the Asia-Pacific region', 'Latin America', 'global markets'

**Financials:**
- `[NAME] provides [SERVICE_TYPE] to [CLIENT_TYPE]. With $[AUM]B in assets under management, the firm [POSITION].`
- SERVICE_TYPE: 'commercial banking services', 'investment management', 'insurance products', 'wealth advisory', 'mortgage lending', 'payment processing', 'private equity', 'consumer credit services'

**Healthcare:**
- `[NAME] focuses on [FOCUS_AREA]. The company's pipeline includes [PIPELINE] and its lead product [PRODUCT_1] [STATUS].`
- FOCUS_AREA: 'oncology therapeutics', 'rare disease treatments', 'medical device innovation', 'diagnostic imaging', 'gene therapy', 'immunotherapy', 'cardiovascular treatments', 'neuroscience research'

**(Für alle 12 Sektoren existieren ähnliche Template-Sets mit je 8-12 Variablen-Pools.)**

**Produktnamen-Generierung:**
Produkte werden aus sektorspezifischen Bausteinen zusammengesetzt:

| Sektor | Prefix-Pool | Suffix-Pool | Beispiele |
|---|---|---|---|
| Tech | Cloud, Edge, Data, Neural, Quantum, Shield, Smart, Core, Flex, Omni | Sync, Vault, Guard, Net, Flow, Hub, Link, OS, Suite, Platform | CloudSync, ShieldNet, DataVault, EdgeGuard |
| Healthcare | Vita, Neuro, Cardio, Onco, Immuno, Gene, Bio, Cell, Pharma, Medi | Cure, Shield, Guard, Boost, Clear, Max, Plus, Pro, Rx, Therapy | VitaCure, OncoShield, GeneMax, ImmunoPlus |
| Energy | Solar, Wind, Petro, Hydro, Volt, Grid, Power, Fuel, Therm, Eco | Flow, Stream, Force, Core, Line, Star, Wave, Tech, Grid, Pro | SolarFlow, VoltCore, WindForce, GridStar |
| Finance | Capital, Trust, Shield, Secure, Prime, Gold, Eagle, Summit, Core, Safe | Pay, Guard, Plus, Pro, Direct, Connect, One, Max, Wise, Net | CapitalGuard, TrustPay, SecurePlus, PrimeDirect |
| (etc.) | | | |

**CEO/CFO/CTO-Namensgenerierung:**
Aus Pools von Vor- und Nachnamen:
- Vornamen (diverse): Sarah, James, Priya, Wei, Carlos, Maria, David, Aisha, Michael, Yuki, Robert, Elena, Thomas, Kenji, Jennifer, Ahmed, Lisa, Henrik, Fatima, Alexander (40+ Namen)
- Nachnamen (diverse): Chen, Torres, Patel, Johnson, Kim, Mueller, Santos, Williams, Nakamura, Okafor, Brown, Petrov, Garcia, Anderson, Singh, Thompson, Lee, Martinez, Wright, Johansson (40+ Namen)
- Kombination: Zufall, aber konsistent pro Firma (gleicher Seed = gleicher CEO)

**Headquarters-Generierung:**
Pool von 30+ fiktiven oder generischen Städtenamen:
- US: San Francisco, Austin, Boston, New York, Seattle, Denver, Chicago, Atlanta, Miami, Phoenix
- International: London, Berlin, Tokyo, Singapore, Toronto, Sydney, Dubai, Stockholm, Seoul, Mumbai
- Zuweisung basierend auf Sektor (Tech → San Francisco/Austin/Seattle, Finanzen → New York/London, Energy → Houston/Dubai)

**Competitors:**
Automatisch bestimmt: die 3-5 nächsten Firmen im gleichen Sektor mit ähnlicher MarketCap (±50%). Diese werden in der UI verlinkt — Klick auf Competitor → navigiert zu deren Profil.

**Risikofaktoren-Pool (pro Sektor):**

| Sektor | Risikofaktoren |
|---|---|
| Tech | 'Rapid technological change', 'High customer concentration', 'Cybersecurity threats', 'Regulatory scrutiny', 'Key talent retention', 'Open-source competition', 'Data privacy concerns', 'Market saturation' |
| Energy | 'Commodity price volatility', 'Environmental regulations', 'Geopolitical instability', 'Transition to renewables', 'Aging infrastructure', 'Weather dependency', 'Regulatory changes' |
| Financials | 'Interest rate sensitivity', 'Credit risk exposure', 'Regulatory capital requirements', 'Cybersecurity risks', 'Market volatility impact', 'Competition from fintech', 'Compliance costs' |
| Healthcare | 'Clinical trial failure risk', 'Patent expiration', 'FDA regulatory risk', 'Drug pricing pressure', 'Competition from generics', 'R&D cost escalation', 'Reimbursement changes' |
| (etc.) | (jeweils 6-8 sektorspezifische Risiken) |

Jeder Firma werden 2-4 Risikofaktoren aus dem Pool ihres Sektors zugewiesen.

**Narrative-Generierung:**
Die 'Narrative' beschreibt die aktuelle Situation und wird aus der Kombination von Traits, jüngstem Preistrend und Events generiert:

Templates:
- Growth + steigender Preis: '[NAME] has been on a strong growth trajectory since [EVENT]. Analysts expect continued momentum driven by [PRODUCT] adoption.'
- Value + fallender Preis: '[NAME] has seen its shares decline amid sector-wide headwinds, but fundamentals remain solid with a P/E of [PE]. Value investors are watching closely.'
- Turnaround: 'After a challenging period marked by [NEGATIVE_EVENT], [NAME] is showing signs of recovery under new CEO [CEO_NAME].'
- Controversy: '[NAME] faces ongoing scrutiny over [RISK_FACTOR], which has weighed on investor sentiment despite strong operational performance.'
- IPO Fresh: 'Since going public [X] months ago, [NAME] has [TREND]. The upcoming lock-up expiration on [DATE] is being closely watched.'

Die Narrative wird alle 30 Spieltage automatisch neu generiert, basierend auf Events die in der Zwischenzeit passiert sind. So 'erzählt' jede Firma ihre eigene Geschichte.

**UI — Firmen-Profil in der Aktien-Detail-Ansicht:**
Ein neuer Tab neben `[Chart] [Order Book] [Time & Sales]`: `[Profile]`

```
┌──────────────────────────────────────────────────────────────┐
│ [Chart]  [Order Book]  [Time & Sales]  [Profile]             │
│──────────────────────────────────────────────────────────────│
│                                                              │
│ ABOUT                                                        │
│ Vertex Dynamics develops cloud-based enterprise security     │
│ solutions for Fortune 500 companies. The company's flagship  │
│ product, ShieldNet, is used by over 2,000 clients worldwide. │
│ Founded in 2019, Vertex has grown through organic growth     │
│ and strategic acquisitions.                                  │
│                                                              │
│ KEY PRODUCTS          LEADERSHIP         HEADQUARTERS        │
│ • ShieldNet Platform  CEO: Sarah Chen     San Francisco, CA  │
│ • CloudVault Pro      CFO: Mark Torres    Founded: 2019      │
│ • EdgeGuard Suite     CTO: Priya Patel    Employees: 4,200   │
│                                                              │
│ FUNDAMENTALS                                                 │
│ Market Cap: $8.2B     Revenue: $1.4B      Net Income: $180M  │
│ P/E: 28.4             Div Yield: —        Debt/Equity: 0.8   │
│ Revenue Growth: 24%   EPS: $3.12          Float: 72%         │
│ Short Interest: 8.2%  Insider Own: 12%    Inst. Own: 58%     │
│                                                              │
│ COMPETITORS                                                  │
│ CNXT CyberNexus Tech  $6.8B  +1.2%  ▲                      │
│ DHLN DataHelix Net.   $5.1B  -0.8%  ▼                      │
│ NRPS NeuralPath Sys.  $4.2B  +2.4%  ▲                      │
│                                                              │
│ RISK FACTORS                                                 │
│ ⚠ High customer concentration                               │
│ ⚠ Competitive cybersecurity market                          │
│ ⚠ Key person dependency (CEO)                               │
│                                                              │
│ NARRATIVE                                                    │
│ Vertex has been on a strong growth trajectory since the       │
│ launch of ShieldNet 2.0 last quarter. However, increasing    │
│ competition and regulatory scrutiny of cloud security        │
│ practices pose headwinds for the coming quarters.            │
│                                                              │
│ ANALYST CONSENSUS                                            │
│ Rating: ████████░░ BUY (4 Buy, 2 Hold, 1 Sell)             │
│ Price Target: $165.00 (+15.7% upside)                       │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

Styling:
- Section-Titel: `Inter SemiBold`, 12px, `text-secondary`, Uppercase
- About-Text: `Inter`, 13px, `text-primary`, Zeilenhöhe 20px
- Key Products: Bullet-Liste, `Inter`, 13px
- Leadership: `Inter`, 13px, Name in `text-primary`, Rolle in `text-secondary`
- Competitors: klickbar (`text-accent`), mit Preis und Tagesperformance
- Risk Factors: ⚠ Icon in `warning`, Text in `text-secondary`
- Narrative: `Inter`, 13px, `text-secondary`, kursiv
- Fundamentals: `JetBrains Mono`, 13px, Grid-Layout wie dargestellt

**Prozedural generierte Firmenlogos:**

Jede Firma bekommt ein einzigartiges Logo, das aus Bausteinen zusammengesetzt wird. Keine externen Assets — alles wird als SVG im Code generiert.

**Logo-Aufbau (3 Schichten):**

```
┌─────────────────────────────────────────┐
│                                         │
│   Schicht 1: Hintergrundform            │
│   (Kreis, Quadrat, Hexagon, Raute,      │
│    Schild, abgerundetes Rechteck)       │
│                                         │
│   Schicht 2: Geometrisches Akzent-Muster│
│   (Diagonale, Halbkreis, Dreieck,       │
│    Streifen, Punkt, Welle, Stern)       │
│                                         │
│   Schicht 3: Initialen (1-2 Buchstaben) │
│   (aus dem Firmen-Symbol abgeleitet)    │
│                                         │
└─────────────────────────────────────────┘
```

**Beispiele:**

```
  ╭──────╮    ◇──────◇    ⬡──────⬡    ┌──────┐
  │ ╱╲   │    │      │    │  ●   │    │▓▓    │
  │╱  ╲  │    │  NP  │    │  SC  │    │▓▓ TF │
  │ VD  │    │      │    │      │    │      │
  │╲  ╱  │    │      │    │      │    │      │
  │ ╲╱   │    ◇──────◇    ⬡──────⬡    └──────┘
  ╰──────╯
  Kreis+       Raute+      Hexagon+    Quadrat+
  Diagonale    Clean       Punkt       Streifen
  Blau         Grün        Lila        Orange
```

**Generierungs-Parameter (seed-basiert):**

```
LogoConfig {
  shape: enum — Circle, RoundedSquare, Hexagon, Diamond, Shield, Pill
  accentPattern: enum — None, Diagonal, HalfCircle, Triangle, Stripes, Dot, Wave, Corner, Cross
  primaryColor: hex — aus Sektor-Farbpalette
  accentColor: hex — heller/dunkler als primaryColor (automatisch berechnet)
  initials: string — 1-2 Buchstaben aus dem Symbol
  initialsFontWeight: Bold | ExtraBold
}
```

**Sektor-Farbpaletten für Logos:**

| Sektor | Primärfarbe | Beschreibung |
|---|---|---|
| Technology | `#3B82F6` Pool: `#2563EB`, `#3B82F6`, `#60A5FA`, `#1D4ED8` | Blautöne |
| Energy | `#F59E0B` Pool: `#D97706`, `#F59E0B`, `#FBBF24`, `#B45309` | Amber/Orange |
| Financials | `#8B5CF6` Pool: `#7C3AED`, `#8B5CF6`, `#A78BFA`, `#6D28D9` | Lilatöne |
| Healthcare | `#EC4899` Pool: `#DB2777`, `#EC4899`, `#F472B6`, `#BE185D` | Pink/Rose |
| Consumer | `#10B981` Pool: `#059669`, `#10B981`, `#34D399`, `#047857` | Smaragd/Grün |
| Industrials | `#6B7280` Pool: `#4B5563`, `#6B7280`, `#9CA3AF`, `#374151` | Grau/Stahl |
| Materials | `#F97316` Pool: `#EA580C`, `#F97316`, `#FB923C`, `#C2410C` | Orange/Rost |
| Real Estate | `#14B8A6` Pool: `#0D9488`, `#14B8A6`, `#2DD4BF`, `#0F766E` | Teal |
| Telecom | `#6366F1` Pool: `#4F46E5`, `#6366F1`, `#818CF8`, `#4338CA` | Indigo |
| Utilities | `#84CC16` Pool: `#65A30D`, `#84CC16`, `#A3E635`, `#4D7C0F` | Lime/Grün |
| Luxury | `#D946EF` Pool: `#C026D3`, `#D946EF`, `#E879F9`, `#A21CAF` | Fuchsia |
| Transport | `#0EA5E9` Pool: `#0284C7`, `#0EA5E9`, `#38BDF8`, `#0369A1` | Sky/Blau |

Jede Firma wählt eine Farbe aus dem Pool ihres Sektors (seed-basiert). So haben Firmen im gleichen Sektor ähnliche, aber nicht identische Farben.

**Logo-Größen:**

| Kontext | Größe | Detail |
|---|---|---|
| Watchlist-Zeile | 24 × 24px | Nur Form + Initialen, kein Akzent |
| Aktien-Tabelle (Market Tab) | 24 × 24px | Nur Form + Initialen |
| Aktien-Detail-Header | 40 × 40px | Form + Akzent + Initialen |
| Firmen-Profil-Tab | 64 × 64px | Voller Detail |
| News-Karten | 20 × 20px | Nur Form + Initiale (1 Buchstabe) |
| Daily Summary | 24 × 24px | Form + Initialen |

**Einzigartigkeit:**
Mit 6 Formen × 9 Akzent-Muster × 4 Farben pro Sektor × 12 Sektoren = **2.592 visuelle Kombinationen** (plus die Initialen, die immer einzigartig sind durch das Symbol). Bei 5.000 Aktien gibt es vereinzelt ähnliche Logos in verschiedenen Sektoren, aber innerhalb eines Sektors sind alle unterscheidbar.

**Rendering:**
- SVG-basiert (keine Bild-Dateien, kein Asset-Management)
- Generiert beim Spielstart und gecacht
- React-Komponente: `<CompanyLogo symbol='VTXD' size={24} />`
- Skaliert perfekt auf jede Größe (Vektor)
- Farbmodus-kompatibel: im Colorblind-Mode werden die Sektorfarben durch die alternativen Farben ersetzt

**ETF/REIT/SPAC-Logos:**
- ETFs: einheitliche Form (Quadrat mit abgerundeten Ecken), Sektorfarbe, Text 'ETF' statt Initialen
- REITs: Schild-Form, Teal-Farbe, Initialen
- SPACs: gestrichelte Umrandung (signalisiert 'noch keine echte Firma'), Text 'SPAC' bis zur Merger-Ankündigung, danach normales Logo der übernommenen Firma

#### 11.3.7 Event-Personalisierung

Events referenzieren die generierten Firmendaten, wodurch jedes Event einzigartig wirkt.

**Statt generisch:**
`[COMPANY] CEO resigns amid scandal.`

**Personalisiert:**
`Vertex Dynamics CEO Sarah Chen resigns amid cybersecurity data breach allegations. The company's board has appointed CFO Mark Torres as interim CEO while conducting a search.`

**Personalisierungs-Variablen:**
Events nutzen folgende Firmen-Daten als Platzhalter:
- `[CEO_NAME]` → generierter CEO-Name
- `[CFO_NAME]` → generierter CFO-Name
- `[PRODUCT_1]` → erstes Produkt der Firma
- `[PRODUCT_2]` → zweites Produkt
- `[COMPETITOR_1]` → Name des ersten Competitors
- `[HEADQUARTERS]` → Standort
- `[EMPLOYEE_COUNT]` → Mitarbeiteranzahl
- `[RISK_FACTOR]` → einer der zugewiesenen Risikofaktoren
- `[FOUNDED_YEAR]` → Gründungsjahr
- `[SECTOR_PRODUCT_TYPE]` → sektorspezifischer Produkttyp

**Beispiele personalisierter Events:**

| Generisch | Personalisiert |
|---|---|
| `[COMPANY] launches new product to strong demand.` | `Vertex Dynamics launches EdgeGuard Suite to strong demand, with 500 enterprise clients signing up in the first week.` |
| `[COMPANY] CEO steps down.` | `Vertex Dynamics CEO Sarah Chen steps down after 6 years. CFO Mark Torres named interim CEO.` |
| `[COMPANY] faces regulatory investigation.` | `Vertex Dynamics faces SMA investigation into cybersecurity data handling practices at its San Francisco headquarters.` |
| `Short seller targets [COMPANY].` | `Activist short seller releases report on Vertex Dynamics, alleging ShieldNet Platform has critical security flaws.` |
| `[COMPANY] acquires competitor.` | `Vertex Dynamics announces $2.1B acquisition of NeuralPath Systems, expanding its AI security portfolio.` |

**Narrative-Update nach Events:**
Wenn ein Event eine Firma betrifft, wird deren Narrative automatisch aktualisiert:
- Vor Event: 'Vertex has been growing rapidly...'
- Nach Earnings Beat: 'Following a strong Q3 earnings beat, Vertex has seen renewed investor interest. ShieldNet adoption grew 24% year-over-year.'
- Nach CEO-Skandal: 'Vertex faces uncertainty after CEO Sarah Chen's departure amid allegations. Interim CEO Mark Torres has pledged stability.'
- Die Narrative speichert die letzten 3 Events als Kontext und generiert einen neuen Text daraus.

### 11.4 Marktstruktur bei Spielbeginn

**Initiale Preis-Historie:**
Bei Generierung wird automatisch 1 Jahr (252 Handelstage) an historischen OHLCV-Daten rückwirkend generiert. Der Spieler sieht vom ersten Moment an Charts mit Geschichte — nicht leere Charts.

**Generierung der Historie:**
- Rückwärts vom Startzeitpunkt simuliert (vereinfachtes Modell, nur Basis-Preismodell + Zufall, keine Events)
- Erstellt realistische Kerzen mit Open, High, Low, Close, Volume
- Kann Trends enthalten (z.B. eine Aktie, die die letzten 3 Monate gestiegen ist)

**Marktphase bei Start (zufällig):**
| Phase | Wahrscheinlichkeit | Effekt |
|---|---|---|
| Bull Market | 40% | Marktindex ist in den letzten 6 Monaten gestiegen, positives Sentiment |
| Neutral | 40% | Seitwärtsbewegung |
| Bear Market | 20% | Marktindex ist gefallen, negatives Sentiment |

Der Spieler kann die Marktphase NICHT wählen (sie wird durch den Seed bestimmt).

**Startbedingungen der Wirtschaft:**

Die Wirtschaftsparameter beim Spielstart hängen von der zufälligen Marktphase ab:

| Parameter | Bull Market | Neutral | Bear Market |
|---|---|---|---|
| Leitzins (Fed Funds Rate) | 2.0-3.5% | 3.0-4.5% | 4.5-6.0% |
| Inflationsrate | 1.5-2.5% | 2.0-3.5% | 3.5-5.5% |
| Arbeitslosenquote | 3.0-4.5% | 4.0-5.5% | 5.5-8.0% |
| Markt-Volatilitätsregime | Calm/Normal | Normal | Elevated |
| Wirtschaftszyklus-Phase | Expansion | Recovery/Expansion | Contraction/Peak |

Diese Werte werden innerhalb der Ranges zufällig generiert (seed-basiert). Sie beeinflussen:
- Margin-Zinsen: Leitzins + Difficulty-Aufschlag (z.B. 3% Leitzins + 4% = 7% Margin-Zinsen bei Normal)
- AI-Verhalten: Institutionelle kaufen aggressiver bei niedrigen Zinsen
- Event-Gewichtung: mehr negative Makro-Events bei hoher Inflation
- Sektor-Rotation: siehe Wirtschaftszyklus-System (5.9)

Der Spieler sieht diese Werte NICHT direkt als Zahlen — sie manifestieren sich durch News-Events ('Current unemployment rate stands at 4.2%'), Zentralbank-Entscheidungen und Marktbewegungen.

---

## 12. Charts & Datenvisualisierung

### 12.1 TradingView Lightweight Charts Integration

**Library:** TradingView Lightweight Charts v4 (npm Package `lightweight-charts`)
**Lizenz:** Apache 2.0 (kostenlos, auch kommerziell)

**React-Wrapper:**
Eine eigene React-Komponente `<StockChart>` die die Library wrappet:
```
Props:
  symbol: string
  data: OHLCV[] (historische Daten)
  livePrice: number (aktueller Tick-Preis)
  timeframe: "1m" | "5m" | "15m" | "1h" | "4h" | "1D" | "1W" | "1M"
  chartType: "candle" | "line" | "area"
  indicators: IndicatorConfig[]
  height: number
  width: number
```

**OHLCV-Datenformat:**
```json
{
  "time": 1678886400,
  "open": 141.20,
  "high": 143.50,
  "low": 140.80,
  "close": 142.58,
  "volume": 2345000
}
```

**Live-Updates:**
Bei jedem Frontend-Update vom Backend wird die letzte Kerze aktualisiert (Close, High, Low werden angepasst, Volume addiert). Wenn ein neuer Zeitrahmen beginnt (z.B. neue Minute bei 1m-Chart), wird eine neue Kerze erstellt.

### 12.2 Aktien-Detail-Ansicht (Chart-Screen)

#### 12.2.1 Layout

Siehe Kapitel 3.4.2 für den Aktien-Header.

Der Chart-Bereich nimmt den gesamten Platz unter dem Header ein.

#### 12.2.2 Chart-Toolbar (über dem Chart, Höhe 36px)

Von links nach rechts:

**Chart-Typ-Buttons:**
- `Candle` (Standard, aktiv), `Line`, `Area`
- Button-Group-Stil wie Speed-Control
- Icon + kurzes Label, 12px

**Zeitrahmen-Buttons:**
- `1m`, `5m`, `15m`, `1h`, `4h`, `1D`, `1W`, `1M`
- Button-Group, identischer Stil
- Standard: `1D`
- Verfügbarkeit hängt von vorhandener Daten-Historie ab (bei Spielstart: 1m/5m nur für den aktuellen Tag, 1D für 1 Jahr)

**Indikator-Button:**
- `📊 Indicators` (Text-Button, rechts)
- Klick: öffnet Indikator-Dropdown/Panel

#### 12.2.3 Chart-Bereich

**Hauptchart (Kerzen/Linie/Area):**
- Hintergrund: `bg-primary`
- Gitterlinien: `grid`-Farbe (kaum sichtbar)
- X-Achse: Zeitstempel, automatisch formatiert (bei 1m: "14:32", bei 1D: "Mar 15", bei 1M: "Jan 2027")
- Y-Achse: Preis, rechts, `JetBrains Mono`, 11px, `text-secondary`
- Preis-Skala passt sich automatisch an die sichtbaren Daten an (Auto-Scale)

**Kerzen-Darstellung:**
- Steigende Kerze (Close > Open): Körper `candle-up-body`, Docht `candle-up-wick`
- Fallende Kerze (Close < Open): Körper `candle-down-body`, Docht `candle-down-wick`
- Körperbreite: automatisch, abhängig von Zoom-Level und Zeitrahmen
- Docht: 1px Linie

**Volumen-Bars:**
- Unter dem Hauptchart, in einem separaten Bereich (20% der Chart-Höhe)
- Steigende Kerze: `volume-up`-Farbe
- Fallende Kerze: `volume-down`-Farbe
- Höhe proportional zum Volumen (höchstes Volumen = volle Höhe)

**Crosshair:**
- Erscheint bei Maus-Hover über den Chart
- Vertikale Linie (gestrichelt, `crosshair`-Farbe, 1px)
- Horizontale Linie (gestrichelt, `crosshair`-Farbe, 1px)
- An der Y-Achse: Preis-Label mit Hintergrund (`bg-tertiary`, `text-primary`, 11px)
- An der X-Achse: Zeit-Label mit Hintergrund
- Daten-Box oben links im Chart: `O: 141.20  H: 143.50  L: 140.80  C: 142.58  Vol: 2.3M` — Werte der Kerze, auf die der Crosshair zeigt

**Event-Marker auf dem Chart:**
- Events, die eine Aktie betroffen haben, werden als kleine vertikale Marker auf dem Chart zum Zeitpunkt ihres Auftretens angezeigt
- Marker-Darstellung: kleines Dreieck/Fähnchen oberhalb der Kerze, bei der das Event stattfand
- Farbe: grünes Dreieck für positive Events, rot für negative, blau für neutrale
- Hover auf Marker: Tooltip zeigt Event-Headline und Preisänderung
- Klick auf Marker: öffnet die News-Detailansicht für dieses Event
- Dies verbindet visuell die Frage "WARUM hat sich der Preis bewegt?" direkt mit dem Chart
- Nur Events, die direkt für DIESE Aktie relevant sind, werden angezeigt (Company Events + Sektor-Events, die diesen Sektor betreffen)
- Kann in der Chart-Toolbar ein-/ausgeschaltet werden: "Show Events" Toggle

**Navigation:**
- Mausrad: Zoom in/out (horizontal, Zeitachse)
- Drag (linke Maustaste): Zeitachse verschieben
- Doppelklick: Chart auf Standard-Zoom und aktuelle Position zurücksetzen
- Der Chart scrollt automatisch mit (neueste Kerze immer sichtbar), bis der Spieler manuell in die Vergangenheit scrollt

#### 12.2.4 Technische Indikatoren

**Overlay-Indikatoren (im Hauptchart gezeichnet):**

| Indikator | Parameter | Farbe |
|---|---|---|
| SMA (Simple Moving Average) | Periode (Standard: 20) | `sma-20` (#F59E0B) |
| SMA 50 | Periode (Standard: 50) | `sma-50` (#8B5CF6) |
| SMA 200 | Periode (Standard: 200) | `sma-200` (#EC4899) |
| EMA (Exponential MA) | Periode (Standard: 12) | `#06B6D4` (Cyan) |
| Bollinger Bands | Periode (20), StdDev (2) | Linien: `bollinger-line`, Füllung: `bollinger` |

**Unter-Chart-Indikatoren (in separatem Panel unter dem Hauptchart):**

| Indikator | Panel-Höhe | Beschreibung |
|---|---|---|
| RSI (Relative Strength Index) | 100px | Linie, 0-100 Skala. Überkauft (>70): rote Zone. Überverkauft (<30): grüne Zone. |
| MACD | 120px | MACD-Linie (blau), Signal-Linie (orange), Histogramm (grün/rot Bars) |
| Stochastic | 100px | %K und %D Linien, 0-100 Skala |
| Volume Profile | 100px | Horizontale Bars am rechten Rand des Charts, zeigen Volumen pro Preis-Level |

**Spezialindikator: VWAP (Volume Weighted Average Price):**
- Overlay-Indikator (im Hauptchart)
- Zeigt den durchschnittlichen Preis gewichtet nach Volumen über den Tag
- Farbe: `#06B6D4` (Cyan), gestrichelte Linie
- Wird jeden Tag bei Market Open zurückgesetzt
- Wichtig für institutionelles Trading: Preis über VWAP = bullish, unter VWAP = bearish
- AI-Trader (Institutionelle, Algos) verwenden VWAP als Referenz für ihre Entscheidungen

**Indikator-Panel (Dropdown bei Klick auf "Indicators"):**
- Liste aller verfügbaren Indikatoren mit Checkbox
- Angehakte Indikatoren werden angezeigt
- Pro Indikator: klickbares Settings-Icon um Parameter zu ändern (Periode etc.)
- Maximal 3 Overlay-Indikatoren + 2 Unter-Chart-Indikatoren gleichzeitig (Performance und Lesbarkeit)
- `Remove All` Button unten

#### 12.2.5 Chart-Vergleich (Compare)

**Funktion:** Eine oder mehrere Aktien als Overlay über den Hauptchart legen, um relative Performance zu vergleichen.

**UI:**
- In der Chart-Toolbar: `Compare` Button (rechts neben Indicators)
- Klick: öffnet kleines Suchfeld (`Add symbol to compare...`)
- Spieler tippt Symbol ein, wählt aus Dropdown
- Die Vergleichsaktie wird als farbige Linie über den Hauptchart gelegt
- Y-Achse wechselt zu **prozentuale Veränderung** (nicht absolute Preise), damit Aktien mit unterschiedlichen Preisen vergleichbar sind
- Startpunkt (0%) = linker Rand des sichtbaren Chart-Bereichs

**Darstellung:**
- Hauptaktie: Standard-Farbe (`text-accent`)
- Vergleichsaktie 1: `#F59E0B` (Amber)
- Vergleichsaktie 2: `#8B5CF6` (Lila)
- Vergleichsaktie 3: `#EC4899` (Pink)
- Maximal 3 Vergleichsaktien gleichzeitig
- Legende oben rechts im Chart: Symbol + Farbe + aktuelle %-Veränderung
- Klick auf Legende-Eintrag: entfernt die Vergleichslinie

**Vergleich mit Index:**
- Statt einer Aktie kann auch ein Sektor-Index oder der Markt-Index als Vergleich gewählt werden
- Vordefinierte Optionen: `Market Index`, `Sector Index [auto]` (automatisch der Sektor der Hauptaktie)

#### 12.2.6 Time & Sales (Tape)

**Funktion:** Scrollende Liste der letzten ausgeführten Trades für die ausgewählte Aktie. Zeigt Echtzeit-Handelsaktivität.

**Position:** Als umschaltbarer Tab in der Aktien-Detail-Ansicht, neben "Order Book":
`[Chart] [Order Book] [Time & Sales]`

**Layout:**
```
┌─────────────────────────────────┐
│ TIME & SALES         AAPL       │
├──────────┬────────┬─────────────┤
│ TIME     │ PRICE  │  SIZE       │
├──────────┼────────┼─────────────┤
│ 14:32:15 │ 142.63 │     500  ▲  │
│ 14:32:12 │ 142.58 │   1,200  ▼  │
│ 14:32:08 │ 142.60 │     300  ▲  │
│ 14:32:05 │ 142.55 │   2,800  ▼  │
│ 14:32:01 │ 142.58 │     150  ▲  │
│ ...      │        │             │
└──────────┴────────┴─────────────┘
```

- **Time:** `JetBrains Mono`, 11px, `text-disabled`
- **Price:** `JetBrains Mono`, 12px. Farbe: `green-primary` wenn ≥ vorheriger Preis, `red-primary` wenn darunter
- **Size:** `JetBrains Mono`, 12px, `text-secondary`. ▲ (Trade at Ask = Buyer, grün) oder ▼ (Trade at Bid = Seller, rot)
- Große Trades (>10× Durchschnitt) werden hervorgehoben: fetter Text, leichter Hintergrund-Flash
- Scrollt automatisch (neueste oben), pausiert bei Hover
- Zeigt die letzten 200 Trades

### 12.3 Orderbook-Visualisierung

**Position:** In der Aktien-Detail-Ansicht, als umschaltbarer Tab unter dem Chart (neben "Chart" Tab ein "Order Book" Tab).

**Layout:**

```
┌───────────────────────────────────────┐
│     BIDS (Buy)    │  ASKS (Sell)      │
├───────────────────┼───────────────────┤
│  ████████  2,500  │  $142.63  1,800   │ ← Level 1 (Best)
│  ██████    1,800  │  $142.65  2,200   │
│  █████     1,200  │  $142.68  3,100   │
│  ████        900  │  $142.72  1,500   │
│  ███         600  │  $142.78  2,800   │
│  ██          400  │  $142.85  4,200   │
│  ██          350  │  $142.90  1,900   │
│  █           200  │  $142.98  1,100   │
│  █           150  │  $143.05  800     │
│  ▏           100  │  $143.15  500     │
├───────────────────┼───────────────────┤
│  Bid: $142.58     │  Ask: $142.63     │
│      Spread: $0.05 (0.04%)            │
└───────────────────────────────────────┘
```

- **Bids (links):** Menge links, Preis rechts. Balken wachsen nach links (größte Menge = voller Balken). Farbe: `green-dim` für Balken, `green-primary` für Preis.
- **Asks (rechts):** Preis links, Menge rechts. Balken wachsen nach rechts. Farbe: `red-dim` für Balken, `red-primary` für Preis.
- **Spread-Anzeige:** Unten, zentriert. `JetBrains Mono`, 13px.
- **10 Levels** auf jeder Seite.
- **Aktualisierung:** Bei jedem Frontend-Update (gebatcht bei hoher Geschwindigkeit).
- **Hover auf Level:** Tooltip zeigt kumulatives Volumen bis zu diesem Level.

### 12.4 Heatmap

Auf dem Dashboard (siehe 3.4.1). Implementierung als Treemap mit der Library `d3-hierarchy` oder einer React-Treemap-Bibliothek.

**Interaktion:**
- Hover: Rechteck bekommt weißen Border (2px), Tooltip: `[Sector]: [Performance]% | [X stocks] | Market Cap: $XXB`
- Klick: wechselt zum Market-Tab mit Sektor-Filter aktiv

### 12.5 Portfolio-Charts

Siehe Kapitel 6.1.3 und 6.1.4 für Details.

### 12.6 Mini-Charts / Sparklines

Kleine Inline-Charts in Watchlist-Einträgen und Tabellen-Zellen.

**Spezifikation:**
- Breite: 40-60px, Höhe: 16-20px
- Typ: einfache Linie (keine Kerzen, keine Achsen, keine Labels)
- Datenpunkte: 20-30 (verteilt über den gewählten Zeitraum, z.B. letzten 24h)
- Farbe: `green-primary` wenn Linie steigt (erster Punkt < letzter Punkt), `red-primary` wenn sie fällt
- Keine Interaktion (kein Hover, kein Klick — die Zeile selbst ist klickbar)
- Gerendert mit `<canvas>` oder SVG für Performance (kein TradingView für Sparklines)

---

## 13. News Ticker & News System

### 13.1 News Ticker (Bottom Bar)

#### 13.1.1 Layout

- Position: fixiert am unteren Fensterrand, volle Breite
- Höhe: 36px
- Hintergrund: `#080C14` (dunkler als `bg-primary`)
- Obere Border: 1px solid `border`
- Links: Label `NEWS` in einem Badge (Hintergrund `bg-tertiary`, `text-secondary`, 10px, Padding 2px 6px), gefolgt von einer vertikalen Trennlinie
- Rechts davon: scrollender Text-Bereich (nimmt den Rest der Breite ein)

#### 13.1.2 Ticker-Einträge

**Format eines Eintrags:**
```
[14:32] AAPL ▲ +2.3% — Apple beats earnings expectations, stock surges
```

- Zeitstempel: `JetBrains Mono`, 11px, `text-disabled`, in eckigen Klammern
- Symbol: `JetBrains Mono Bold`, 12px, `text-primary`
- Dreieck: ▲ in `green-primary` oder ▼ in `red-primary`
- Veränderung: `JetBrains Mono`, 12px, farbig
- Separator: ` — ` (Gedankenstrich)
- Headline: `Inter`, 12px, `text-secondary`
- Trenner zwischen Einträgen: `  |  ` in `text-disabled`

**Farbcodierung:**
- Positives Event: Symbol + Veränderung in `green-primary`
- Negatives Event: Symbol + Veränderung in `red-primary`
- Neutrales Event: alles in `text-secondary`
- Macro-Events (kein einzelnes Symbol): `MARKET` oder Sektor-Name statt Symbol

#### 13.1.3 Ticker-Verhalten

- **Scrollrichtung:** Rechts nach links, kontinuierlich
- **Scrollgeschwindigkeit:** 60px/Sekunde (Standard), konfigurierbar 30-120px/s
- **Bei Hover:** Scroll pausiert, Text steht still. Der Spieler kann lesen.
- **Bei Spiel-Pause:** Ticker pausiert ebenfalls
- **Bei Speed 5x/10x:** Scrollgeschwindigkeit bleibt gleich, aber es kommen mehr Einträge (da mehr Events pro Echtzeitsekunde)
- **Neue Einträge:** werden rechts (außerhalb des sichtbaren Bereichs) angefügt und scrollen rein
- **Buffer:** Die letzten 50 Einträge werden im Ticker gehalten, ältere verschwinden
- **Klick auf Eintrag:** Spiel pausiert, Aktien-Detail-Ansicht der betroffenen Aktie öffnet sich

### 13.2 News-Feed (News-Tab)

Der News-Tab im Zentralbereich zeigt alle Events als scrollbaren Feed.

**Layout:**

```
┌─────────────────────────────────────────────┬──────────────┐
│              NEWS FEED                       │  UPCOMING    │
│                                              │  EVENTS      │
│ ┌──────────────────────────────────────────┐ │              │
│ │ 🔴 2:32 PM  COMPANY                     │ │  Mar 18      │
│ │                                          │ │  AAPL        │
│ │ Apple misses earnings expectations       │ │  Earnings    │
│ │                                          │ │              │
│ │ Apple Inc. reported Q3 earnings of       │ │  Mar 22      │
│ │ $1.85 per share, missing analyst         │ │  Fed Rate    │
│ │ estimates of $2.10. Revenue came in...   │ │  Decision    │
│ │                                          │ │              │
│ │ AAPL -8.4%  |  TECH-IDX -1.2%          │ │  Mar 25      │
│ │                                          │ │  Jobs Report │
│ │ [View Stock]  [Trade]                    │ │              │
│ └──────────────────────────────────────────┘ │  ...         │
│                                              │              │
│ ┌──────────────────────────────────────────┐ │              │
│ │ 🟢 1:45 PM  SECTOR                      │ │              │
│ │                                          │ │              │
│ │ Energy sector rallies on OPEC cut...     │ │              │
│ └──────────────────────────────────────────┘ │              │
│                                              │              │
│ ...                                          │              │
└─────────────────────────────────────────────┴──────────────┘
```

**Linker Bereich: News Feed (75% Breite)**

**Filter-Bar (oben, 40px):**
- Kategorie-Filter: `All`, `Macro`, `Sector`, `Company`, `Market` — als Toggle-Buttons
- Sektor-Dropdown: `All Sectors` + alle 12 Sektoren
- Watchlist-Toggle: `Watchlist Only` (zeigt nur News zu Watchlist-Aktien)
- Suchfeld: 200px, `Search news...`

**News-Karten:**
Jede News ist eine Karte (volle Breite, Padding 16px, Border-Bottom 1px solid `border`):

- **Ungelesen:** Linker Rand 3px solid `text-accent`, Titel in `Inter SemiBold`
- **Gelesen:** Kein linker Rand, Titel in `Inter Regular`

**Karten-Aufbau:**
- Zeile 1: Sentiment-Dot (🔴/🟢/⚪, 8px) + Zeitstempel (`JetBrains Mono`, 11px, `text-disabled`) + Kategorie-Badge (`COMPANY` / `SECTOR` / `MACRO` / `MARKET`, Hintergrund `bg-tertiary`, 10px)
- Zeile 2: Headline (`Inter SemiBold` oder `Regular`, 15px, `text-primary`)
- Zeile 3-5: Fließtext (2-3 Zeilen, `Inter`, 13px, `text-secondary`, Line-Height 20px)
- Zeile 6: Betroffene Symbole als klickbare Badges + deren aktuelle Performance seit dem Event
- Zeile 7: Action-Buttons: `View Stock` (outline, `text-accent`) und `Trade` (outline, `green-primary`)
- Hover auf Karte: Hintergrund leicht `bg-tertiary`

**Rechter Bereich: Upcoming Events (25% Breite, 200px)**

- Titel: `Upcoming Events` (`Inter SemiBold`, 14px)
- Liste geplanter Events der nächsten 14 Tage
- Pro Eintrag: Datum (`JetBrains Mono`, 12px, `text-primary`) + Symbol oder Typ (`text-secondary`, 12px) + Event-Art (`text-disabled`, 11px)
- Hover: Hintergrund `bg-tertiary`
- Klick: scrollt zum Event im Feed (wenn es schon stattgefunden hat) oder zeigt Tooltip mit Vorschau

### 13.3 News-Detail-Ansicht

Klick auf eine News-Karte expandiert sie (Accordion-Stil, kein neuer Screen):

- Voller Fließtext (3-8 Sätze)
- Betroffene Aktie(n) mit Mini-Chart (zeigt Kursverlauf seit dem Event, 100px × 30px)
- Performance seit Event: `AAPL: $155.20 → $142.58 (-8.13% since event)`
- `Trade`-Button: vorausgefüllt mit Symbol im Order-Panel
- `Related Events`: Liste von 0-3 verwandten Events (Kaskadierungen oder ähnliche Events)

### 13.4 News-Generierung

**Template-System:**
Jeder Event-Typ hat 5-10 verschiedene Text-Templates, die mit Platzhaltern gefüllt werden:

**Beispiel-Templates für "Earnings Beat":**
1. `[COMPANY] reports Q[Q] earnings: EPS $[EPS] vs $[EXPECTED] expected, revenue up [GROWTH]%`
2. `[COMPANY] crushes earnings estimates, posting [GROWTH]% revenue growth in Q[Q]`
3. `Strong quarter for [COMPANY]: earnings per share of $[EPS] beat consensus by [BEAT]%`
4. `[COMPANY] delivers earnings surprise, shares surge [CHANGE]% in after-hours trading`
5. `Wall Street cheers as [COMPANY] tops Q[Q] expectations on both revenue and earnings`

**Platzhalter werden zur Laufzeit gefüllt:**
- `[COMPANY]` → generierter Firmenname
- `[Q]` → aktuelles Quartal (1-4)
- `[EPS]` → generierter EPS-Wert (basierend auf Fundamentaldaten ± Variation)
- `[EXPECTED]` → etwas unter dem tatsächlichen EPS (da "Beat")
- `[GROWTH]` → generierte Wachstumszahl
- `[CHANGE]` → tatsächliche Preisveränderung nach Event

**Stil:** Professionell, wie Bloomberg/Reuters/CNBC-Schlagzeilen. Kein Slang, keine Memes, keine Emojis (außer bei Rumors: 💬).

**Variation:** Das System wählt zufällig eines der Templates. Der gleiche Event-Typ klingt nie zweimal gleich hintereinander. Das letzte verwendete Template wird getrackt, um Wiederholungen zu vermeiden.

---

## 14. Tutorial & Onboarding

### 14.1 Erster Start (First-Time User Experience)

Beim allerersten Start (kein Savegame vorhanden) wird der Spieler durch einen Einrichtungsablauf geführt.

**Screen 1: Willkommen**
- Zentrierter Text auf dunklem Hintergrund:
  - `Welcome to StockSim` (`Inter Bold`, 36px, `text-primary`)
  - `Your journey to Wall Street starts here.` (`Inter`, 16px, `text-secondary`)
  - `[START]` Button (`text-accent` Border, 48px Höhe, 200px Breite)
- Subtile Animation: Zahlen und Ticker-Fragmente scrollen langsam im Hintergrund (rein dekorativ)

**Screen 2: Erfahrungslevel**
- `How familiar are you with stock trading?` (`Inter SemiBold`, 20px)
- Drei Karten nebeneinander (je ~300px breit):

| Karte | Label | Beschreibung | Effekt |
|---|---|---|---|
| 1 | `Beginner` | `I'm new to this. Teach me everything.` | Tutorial startet automatisch, alle Tooltips aktiviert |
| 2 | `Intermediate` | `I know the basics. Skip the intro.` | Kurzes Tutorial (nur UI-Tour, kein Handels-Tutorial), Tooltips an |
| 3 | `Expert` | `I've been trading for years.` | Kein Tutorial, Tooltips aus, sofort ins Spiel |

- Hover auf Karte: Border wechselt zu `text-accent`, leichte Scale-Animation (1.02×)
- Klick wählt aus

**Screen 3: Neues Spiel**
- `Create Your Portfolio` (`Inter SemiBold`, 20px)
- **Portfolio Name:** Eingabefeld (Standard: "My Portfolio"), max 30 Zeichen
- **Starting Capital:** drei Optionen als Karten:
  - `$100,000` — `Easy` Badge (grün)
  - `$50,000` — `Normal` Badge (gelb) — vorausgewählt
  - `$25,000` — `Hard` Badge (rot)
- **Difficulty:** Die Wahl des Startkapitals bestimmt die Difficulty-Stufe (Easy/Normal/Hard). Alle Difficulty-abhängigen Einstellungen (siehe Kapitel 16.3) werden automatisch gesetzt. Der Spieler kann einzelne Einstellungen danach in den Settings manuell überschreiben.
- **Market Size:** Dropdown: `Small (100 stocks)`, `Standard (500 stocks)` (vorausgewählt), `Large (1000 stocks)`
- **Seed (Advanced):** eingeklapptes Feld, "Custom Seed" Link. Klick: Eingabefeld für Seed (Integer). Standardmäßig leer (= zufällig).
- `[BEGIN TRADING]` Button (volle Breite, `green-primary` Hintergrund, weiße Schrift, 48px)

**Ladebildschirm:**
Während der Markt generiert wird (kann 2-5 Sekunden dauern):
- `Generating market...` mit Fortschrittsbalken
- Unter dem Balken: wechselnde Tipps:
  - `Tip: Green means profit. Red means... opportunity.`
  - `Tip: Press Space to pause the simulation at any time.`
  - `Tip: Watch the news ticker for market-moving events.`

### 14.2 Geführtes Tutorial

Das Tutorial wird als Overlay über dem tatsächlichen Spiel angezeigt. Das Spiel läuft (pausiert), und der Spieler interagiert mit echten UI-Elementen.

**Spotlight-Effekt:** Der relevante UI-Bereich wird hell beleuchtet, der Rest des Bildschirms wird gedimmt (Overlay `rgba(0, 0, 0, 0.6)` mit Ausschnitt für den relevanten Bereich).

**Tutorial-Box:** Eine Sprechblase/Karte neben dem beleuchteten Bereich:
- Hintergrund: `bg-secondary`, Border: 2px solid `text-accent`, Border-Radius: 12px
- Breite: 360px, Padding: 20px
- Titel: `Inter SemiBold`, 16px, `text-primary`
- Text: `Inter`, 14px, `text-secondary`, Zeilenhöhe 22px
- Schritt-Indikator: `Step 3 of 7` in `text-disabled`, 11px, oben rechts
- Buttons unten: `[Skip Tutorial]` (links, `text-disabled`) + `[Next →]` (rechts, `text-accent`)
- Bei Schritten mit Spieler-Aktion: `[Next →]` ist disabled bis die Aktion ausgeführt wurde

#### 14.2.1 Schritt 1: UI-Orientierung

**Spotlight:** Gesamter Bildschirm (kein Dimming, aber Nummern/Labels auf den Hauptbereichen)
- Nummerierte Callouts auf: ①Watchlist, ②Chart Area, ③Trading Panel, ④News Ticker, ⑤Time Controls
- Text: `This is your trading desk. Let's get familiar with the layout.`
- `[Next →]`

#### 14.2.2 Schritt 2: Watchlist erkunden

**Spotlight:** Linke Sidebar (Watchlist)
- Text: `Your Watchlist shows stocks you're tracking. Click on any stock to see its details.`
- **Spieler-Aktion:** Muss eine Aktie in der Watchlist anklicken
- Watchlist ist vorausgefüllt mit 5 Aktien (eine pro Sektor-Kategorie: eine Tech, eine Energy, eine Finance, etc.)

#### 14.2.3 Schritt 3: Chart lesen

**Spotlight:** Zentraler Bereich (Chart der angeklickten Aktie)
- Text: `This is a candlestick chart. Each candle shows the price movement over a period. Green candles mean the price went up. Red candles mean it went down. The thin lines (wicks) show the highest and lowest prices.`
- Sekundärer Hinweis: `Try changing the timeframe using the buttons above the chart.`
- **Spieler-Aktion:** Optional — Zeitrahmen wechseln (oder Skip)

#### 14.2.4 Schritt 4: Erster Kauf

**Spotlight:** Rechte Sidebar (Order Panel)
- Text: `Let's make your first trade. We'll buy some shares.`
- Tutorial stellt sicher, dass der BUY-Tab aktiv ist
- Anleitung: `Enter a quantity (try 10 shares) and click "PLACE BUY ORDER".`
- **Spieler-Aktion:** Quantity eingeben, Order platzieren und bestätigen
- Nach Ausführung: `Congratulations! You just bought 10 shares of [SYMBOL].`

#### 14.2.5 Schritt 5: Portfolio prüfen

**Spotlight:** Portfolio-Tab (automatisch gewechselt)
- Text: `Here's your portfolio. You can see your position, the current price, and your profit or loss (P&L).`
- Tutorial lässt den Preis leicht steigen (1-2% — subtile Manipulation für ein positives erstes Erlebnis)
- `Your position is already up! Nice timing.`

#### 14.2.6 Schritt 6: Zeitsteuerung

**Spotlight:** Speed-Controls in der Top Bar
- Text: `You control time. Press Space to pause. Use 1-4 keys or click the buttons to change speed. Try it now.`
- **Spieler-Aktion:** Mindestens einmal Speed ändern oder pausieren/fortsetzen

#### 14.2.7 Schritt 7: Erster Verkauf

- Tutorial lässt die Aktie 3-5% steigen
- Text: `Your stock went up! Let's sell for a profit. Go to the SELL tab and sell your shares.`
- **Spieler-Aktion:** Sell-Order platzieren
- `You just made $XX profit! That's how it works.`

#### 14.2.8 Abschluss

- Kein Spotlight — Box zentriert:
  - `Tutorial Complete!`
  - `You now know the basics. Here are a few tips:`
  - `• Watch the news ticker for market-moving events`
  - `• Use Limit Orders to buy at a specific price`
  - `• Press Ctrl+S to save your game anytime`
  - `• Hover over any ? icon for explanations`
  - `[Start Trading]` Button
- Tutorial kann jederzeit aus Settings → `Restart Tutorial` wiederholt werden

### 14.3 Tooltips & Kontexthilfe

**Fragezeichen-Icons:**
- Erscheinen neben komplexen Konzepten (Order-Typen, Short Selling, Margin, Spread, etc.)
- Icon: `info` (ℹ), 14px, `text-disabled`
- Hover (nach 300ms Delay): Tooltip erscheint

**Tooltip-Styling:**
- Hintergrund: `bg-tertiary`, Border: 1px solid `border`, Border-Radius: 8px
- Box-Shadow: `0 4px 12px rgba(0, 0, 0, 0.3)`
- Maximalbreite: 280px, Padding: 12px
- Text: `Inter`, 13px, `text-primary`
- Pfeil zeigt zum ℹ-Icon

**Beispiel-Tooltips:**
| Element | Tooltip-Text |
|---|---|
| Limit Order | `A Limit Order lets you set the maximum price you're willing to pay (buy) or minimum price you'll accept (sell). The order only executes when the market reaches your price.` |
| Short Selling | `Short selling means borrowing shares and selling them, hoping to buy them back cheaper later. Your profit is the difference. Warning: losses are theoretically unlimited if the price rises.` |
| Spread | `The spread is the difference between the Bid (highest buy offer) and Ask (lowest sell offer). A tighter spread means cheaper trading.` |
| P&L | `Profit & Loss. Shows how much money you've made or lost on a position. Unrealized P&L is for open positions; Realized P&L is for closed trades.` |
| Margin | `Margin is borrowed money used to buy more stocks than your cash allows. It amplifies both gains AND losses. You pay interest on borrowed funds.` |

**Einstellung:** `Show tooltips` kann in Settings deaktiviert werden (Standard: AN für Beginner/Intermediate, AUS für Expert).

### 14.4 Glossar / Help-Screen

Zugänglich über: Settings → `Help & Glossary` oder Tastenkürzel `F1`.

**Layout:** Modal, 800 × 600px, zwei Spalten:
- Links: alphabetische Liste aller Begriffe (scrollbar, mit Buchstaben-Navigation A-Z)
- Rechts: Erklärung des ausgewählten Begriffs

**Begriffe (Auswahl):**
Ask, Bid, Bollinger Bands, Bull Market, Bear Market, Candlestick, Circuit Breaker, Cover, Day Order, Dividend, EMA, EPS, Flash Crash, FOMO, Gap, GTC, Limit Order, Liquidation, Margin, Margin Call, Market Cap, Market Order, Mean Reversion, Moving Average, OHLCV, Order Book, P&L, P/E Ratio, Position, RSI, Short Interest, Short Selling, Short Squeeze, Slippage, SMA, Spread, Stop Loss, Stop Order, Trailing Stop, Volatility, Volume

**Suchfeld:** Oben im Modal, filtert die Liste in Echtzeit.

---

## 15. Speicher- und Ladesystem

### 15.1 Savegame-Datenstruktur

**Gespeicherte Daten:**
```
SaveGame {
  Meta {
    SaveName: string
    SaveDate: DateTime (Echtzeit)
    GameDate: DateTime (Spielzeit)
    Version: string (Spiel-Version)
    Seed: int
    PlayTime: TimeSpan (Echtzeit-Spielzeit)
    PortfolioValue: float (für Anzeige in Save-Liste)
  }

  GameState {
    CurrentDate: DateTime
    CurrentSpeed: int (0-4)
    MarketPhase: string
    VolatilityRegime: string
  }

  Stocks[] {
    Symbol, Name, Sector, Traits[]
    Fundamentals { MarketCap, Revenue, ... }
    TradingParams { CurrentPrice, Volatility, ... }
    PriceHistory[] { Date, O, H, L, C, V } — gesamte Historie
    ShortInterest: float
  }

  Player {
    Cash: float
    MarginDebt: float
    Positions[] { Symbol, Quantity, AvgPrice, OpenDate }
    OpenOrders[] { OrderId, Type, Symbol, Side, Qty, Price, TIF, Created }
    TradeHistory[] { ... }
    TotalRealizedPnL: float
  }

  AIState {
    AITraders[] { Type, Portfolio, Parameters, LastDecision }
  }

  EventState {
    EventHistory[] { Event }
    ScheduledEvents[] { Event, TriggerDate }
    ActiveEffects[] { Effect, RemainingDuration }
    LastEventTemplates[] { used templates for variation }
  }

  Settings { ... (alle Spieler-Einstellungen) }
  Watchlists[] { Name, Symbols[] }
  Achievements[] { AchievementId, UnlockedDate, Progress }
  PriceAlerts[] { Symbol, Condition, TargetPrice, Active }
  TradingJournal[] { Id, Timestamp, Symbol, Text, Pinned }
  SMAState { SuspicionScore, ActiveInvestigations[], PastPenalties[], TradingRestrictions[] }
  TaxRecords { UnrealizedLiability, QuarterlyPayments[], RealizedGainsThisQuarter }
  EconomicCyclePhase: string
  ActiveBuybackPrograms[] { Symbol, RemainingAmount, DailyLimit }
}
```

**Format:** JSON (lesbar, leicht debugbar). Komprimiert mit gzip bei großen Märkten (>1000 Aktien, Preis-Historien).

**Geschätzte Savegame-Größe:**
- 100 Aktien, 1 Monat gespielt: ~5 MB
- 500 Aktien, 6 Monate gespielt: ~50 MB
- 1000 Aktien, 1 Jahr gespielt: ~150 MB (komprimiert: ~30 MB)

### 15.2 Speichern

**Manuelles Speichern:**
- Tastenkürzel: `Ctrl+S`
- Save-Button in Top Bar (siehe 3.2.3)
- Öffnet den Speichern-Dialog (wenn noch kein Name vergeben) oder speichert sofort in den aktuellen Slot (wenn Name vorhanden)

**Speichern-Dialog:**
- Modal, 500 × 400px
- Eingabefeld: `Save Name` (vorausgefüllt mit letztem Namen oder "Save 1")
- Liste bestehender Saves darunter (kann überschrieben werden, mit Warnung)
- `[SAVE]` und `[CANCEL]` Buttons

**Autosave:**
- Standard: alle 10 Minuten (Echtzeit)
- Konfigurierbar: 1, 5, 10, 15, 30 Minuten oder AUS
- Autosave-Slot: separater Slot, überschreibt sich selbst, zählt nicht zu den manuellen Saves
- Toast bei Autosave: `Auto-saved` (dezent, 2s, nur wenn Setting `Show autosave notification` AN)

**Speicher-Feedback:**
- Während des Speicherns: Save-Icon in Top Bar rotiert (Lade-Animation, 500ms)
- Danach: Toast `Game saved` (grün, 2s)
- Speichern blockiert NICHT das Spiel — die Simulation läuft weiter

**Save-Slots:** Unbegrenzt (nur durch Festplattenspeicher limitiert). Savegames werden in `%AppData%/StockSim/saves/` gespeichert.

### 15.3 Laden

**Laden-Dialog:**
- Tastenkürzel: `Ctrl+L`
- Oder: Main Menu → `Load Game`
- Modal, 600 × 500px

**Save-Liste:**
Tabelle mit allen Saves:

| Spalte | Inhalt |
|---|---|
| Name | Save-Name |
| Game Date | `Mar 15, 2027` (Spielzeit) |
| Portfolio Value | `$62,456` |
| Play Time | `4h 32m` |
| Saved | `Mar 22, 2026 14:30` (Echtzeit) |

- Sortierung: neueste oben
- Hover: Zeile hervorgehoben
- Klick: wählt Save aus, `[LOAD]` Button wird aktiv
- Doppelklick: lädt sofort
- Rechtsklick: `Delete Save` (mit Bestätigung)

**Bestätigung:** `Unsaved progress will be lost. Continue?` — `[Cancel]` + `[Load]`

**Ladebalken:** Bei großen Saves (>20 MB): Fortschrittsbalken mit `Loading save...`

### 15.4 Neues Spiel

- Tastenkürzel: `Ctrl+N`
- Warnung wenn aktuelles Spiel nicht gespeichert: `You have unsaved progress. Save before starting a new game?` — `[Save]` + `[Don't Save]` + `[Cancel]`
- Führt zum Neues-Spiel-Dialog (Screen 3 aus Kapitel 14.1, ohne die Erfahrungslevel-Abfrage)

### 15.5 Datenintegrität

**Validierung beim Laden:**
- Version-Check: wenn Save von einer älteren Spielversion stammt → Migration (automatisch) oder Warnung
- Checksum: einfache Prüfsumme im Save-Header, um Korruption zu erkennen
- Bei korruptem Save: `This save file appears to be corrupted and cannot be loaded.` → letztes Autosave anbieten

---

## 16. Settings & Konfiguration

### 16.1 Settings-Modal Layout

- Tastenkürzel: `Escape` → Pause-Menü (wenn kein anderes Modal offen, siehe 3.0.4), oder Klick auf Settings-Zahnrad
- Modal, 700 × 550px, zentriert
- Links: vertikale Tab-Leiste (160px Breite) mit Kategorien
- Rechts: Einstellungen der gewählten Kategorie (scrollbar)
- Unten: `[Reset to Defaults]` (links, `text-disabled`) + `[Close]` (rechts)
- Alle Änderungen werden sofort angewendet (kein "Apply"-Button nötig)

### 16.2 General

| Setting | Typ | Standard | Beschreibung |
|---|---|---|---|
| Autosave | Toggle | AN | Automatisches Speichern aktivieren |
| Autosave Interval | Dropdown | 10 min | 1, 5, 10, 15, 30 Minuten |
| Show Autosave Notification | Toggle | AN | Toast bei Autosave anzeigen |
| Show Tooltips | Toggle | AN | Hilfe-Tooltips bei ℹ-Icons |
| Confirm Orders | Toggle | AN | Bestätigungsdialog vor Order-Platzierung |
| Auto-Pause on Breaking News | Toggle | AN | Spiel pausiert bei Major/Catastrophic Events |
| Auto-Pause on Margin Call | Toggle | AN | Spiel pausiert bei Margin Call |
| Auto-Pause on Short Squeeze | Toggle | AN | Spiel pausiert bei Short Squeeze Warning |
| Auto-Pause on Price Alert | Toggle | AN | Spiel pausiert wenn Price Alert triggert |
| Auto-Pause on Order Execution | Toggle | AUS | Spiel pausiert wenn Order ausgeführt wird |
| Auto-Pause on Market Open | Toggle | AUS | Spiel pausiert bei Market Open |
| Skip Weekends | Toggle | AUS | Wochenenden automatisch vorspulen |
| Restart Tutorial | Button | — | Tutorial von vorne starten |

### 16.3 Simulation

| Setting | Typ | Standard | Beschreibung |
|---|---|---|---|
| Trading Commission | Toggle + Input | AN, $4.95 | Handelsgebühren ein/aus, Betrag anpassbar |
| Margin Interest | Toggle | AN | Margin-Zinsen aktivieren |
| Short Borrow Fees | Toggle | AN | Leihgebühren für Short Selling |
| Enable Taxes | Toggle | AUS (Easy), AN (Normal/Hard) | Capital Gains Tax auf realisierte Gewinne |
| Tax Rate Mode | Dropdown | Realistic | Realistic (15-35%), Flat (20%), Off |
| SMA Enforcement | Toggle | AN | Regulierungsbehörde überwacht Spieler-Handeln |
| SMA Strictness | Dropdown | Normal | Lenient (selten Strafen), Normal, Strict (aggressive Überwachung) |
| Difficulty | Dropdown | Normal | Siehe Difficulty-Tabelle unten |

**Difficulty-spezifische Mechaniken (was sich GENAU ändert):**

| Mechanik | Easy | Normal | Hard |
|---|---|---|---|
| **Startkapital** | $100,000 | $50,000 | $25,000 |
| **Kommissionen** | $0 (kostenlos) | $4.95/Trade | $9.95/Trade |
| **Margin-Zinsen** | Leitzins + 0% p.a. | Leitzins + 4% p.a. | Leitzins + 8% p.a. |
| **Short Borrow Fee** | 50% der normalen Rate | Normal | 150% der normalen Rate |
| **Steuern** | Aus | An (15-35%) | An (20-40%) |
| **SMA-Strenge** | Lenient (halbe Score-Zuschläge) | Normal | Strict (doppelte Score-Zuschläge) |
| **AI-Aggressivität** | Retail-FOMO gedämpft, weniger Panic | Normal | Hedge Funds aggressiver, mehr Flash-Crash-Risiko |
| **Event-Häufigkeit (Catastrophic)** | 1 alle 200 Tage | 1 alle 100 Tage | 1 alle 50 Tage |
| **Slippage** | 50% des normalen Werts | Normal | 150% des normalen Werts |
| **Spread** | 80% der normalen Breite | Normal | 120% der normalen Breite |
| **Wirtschaftszyklus** | Expansionen dauern länger | Normal | Kontraktionen dauern länger |
| **Tutorial** | Automatisch aktiviert | Optional | Deaktiviert |
| **Tooltips** | Immer an | Standard an | Standard aus |
| **Margin-Call-Frist** | 48h Spielzeit | 24h Spielzeit | 12h Spielzeit |
| **Bankrott-Neustart** | $25,000 | $10,000 | $5,000 |

### 16.4 Audio

| Setting | Typ | Standard |
|---|---|---|
| Master Volume | Slider 0-100 | 80 |
| Music Volume | Slider 0-100 | 50 |
| SFX Volume | Slider 0-100 | 70 |
| News Alert Sound | Toggle | AN |
| Trade Execution Sound | Toggle | AN |
| Market Bell Sound | Toggle | AN |

### 16.5 Display

| Setting | Typ | Standard |
|---|---|---|
| Window Mode | Dropdown | Windowed |
| Resolution | Dropdown | Native (nur im Fullscreen) |
| UI Scale | Dropdown | 100% (80%, 100%, 120%, 150%) |
| News Ticker Speed | Slider 30-120 | 60 px/s |
| Default Chart Timeframe | Dropdown | 1D |
| Default Chart Type | Dropdown | Candlestick |
| Number Format | Dropdown | 1,234.56 (US) |
| Show Sparklines in Watchlist | Toggle | AN |
| Color Mode | Dropdown | Standard | Standard, Colorblind (Red-Green), Colorblind (Blue-Yellow) |
| Reduced Animations | Toggle | AUS |

### 16.6 Controls

**Tastenkürzel-Übersicht** (nur Anzeige, keine Anpassung im MVP):
Zeigt alle Shortcuts in einer zweispaltigen Tabelle (Kategorie links, Shortcut rechts). Siehe Kapitel 18 für die vollständige Liste.

### 16.7 Advanced

| Setting | Typ | Standard |
|---|---|---|
| Show Performance Stats | Toggle | AUS | FPS, Tick-Rate, WebSocket-Latenz anzeigen |
| Performance Mode | Toggle | AUS | Reduziert Animationen und Chart-Updates |
| Open Save Folder | Button | — | Öffnet den Savegame-Ordner im Explorer |
| Clear Cache | Button | — | Löscht temporäre Dateien |
| Reset All Settings | Button | — | Setzt alle Settings auf Standard (mit Bestätigung) |
| Delete All Saves | Button | — | Löscht alle Savegames (mit doppelter Bestätigung!) |

---

## 17. Audio Design

### 17.1 Audio-Philosophie

Audio in StockSim ist dezent und professionell. Es verstärkt die Immersion, ohne abzulenken. Der Spieler soll sich fühlen wie in einer Nachrichtenredaktion oder einem ruhigen Trading Floor — nicht wie in einem Arcade-Spiel.

### 17.2 Hintergrundmusik

**Stil:** Ambient, leicht elektronisch, Low-Fi. Denke an: Bloomberg TV Hintergrundmusik, Lo-Fi Beats, Ambient-Drone mit subtilen melodischen Elementen.

**Tracks:** Mindestens 5 verschiedene Tracks (je 3-5 Minuten Loop):
1. `calm_market` — Ruhige Phase, sanftes Piano + Pad
2. `steady_trading` — Normaler Handel, leichter Beat, pulsierend
3. `rising_momentum` — Bull-Phase, optimistischer Unterton, etwas schneller
4. `tension_building` — Erhöhte Volatilität, dunklere Töne, Spannung
5. `crisis_mode` — Crash/Panik, tiefe Drones, schnellere Pads, dringend

**Dynamische Musik-Auswahl:**
- Calm Market → Volatilitäts-Regime "Calm"
- Steady Trading → Regime "Normal"
- Rising Momentum → Regime "Normal" + Marktindex steigt >1% heute
- Tension Building → Regime "Elevated"
- Crisis Mode → Regime "Crisis"

**Übergänge:** Crossfade über 5 Sekunden. Keine harten Schnitte.

**Lizenz:** Royalty-free oder eigenkomponiert. KEIN Urheberrecht-Problem für Steam-Release.

### 17.3 Sound Effects (SFX)

#### 17.3.1 UI-Sounds

| Sound | Beschreibung | Trigger |
|---|---|---|
| `click_soft` | Weiches, kurzes Klicken | Button-Klick, Tab-Wechsel |
| `click_toggle` | Leises Schaltergeräusch | Toggle-Switch, Checkbox |
| `modal_open` | Sanftes Whoosh (aufsteigend, 200ms) | Modal öffnet sich |
| `modal_close` | Sanftes Whoosh (absteigend, 150ms) | Modal schließt sich |
| `notification` | Dezentes Ping (hell, kurz) | Toast erscheint |

#### 17.3.2 Trading-Sounds

| Sound | Beschreibung | Trigger |
|---|---|---|
| `order_placed` | Bestätigendes "Blip" (aufsteigend, befriedigend) | Order platziert |
| `order_filled_buy` | Kurzes Kassen-Kling (höher) | Buy-Order ausgeführt |
| `order_filled_sell` | Kurzes Kassen-Kling (tiefer) | Sell-Order ausgeführt |
| `order_cancelled` | Leises Flattern/Cancel-Ton | Order gecancelt |
| `order_rejected` | Dumpfer Error-Ton (kurz, nicht harsch) | Order abgelehnt |
| `margin_warning` | Alarmierende, aber nicht erschreckende Warnung (2 aufsteigende Töne) | Margin Warning |
| `margin_call` | Dringenderer Alarm (3 schnelle Töne, tiefer) | Margin Call |

#### 17.3.3 Event-Sounds

| Sound | Beschreibung | Trigger |
|---|---|---|
| `breaking_news` | TV-Eilmeldungs-Jingle (2-3 Sekunden, dramatisch aber kurz) | Breaking News Popup |
| `news_ping` | Dezentes Nachrichtenping | Standard-News im Ticker |
| `market_bell_open` | Börsenglocke (realistisch, 2 Sekunden) | Market Open (9:30 AM) |
| `market_bell_close` | Börsenglocke (etwas gedämpfter) | Market Close (4:00 PM) |
| `short_squeeze_alarm` | Sirenenartig, aber gedämpft (1.5 Sekunden) | Short Squeeze Warning |
| `flash_crash` | Dramatischer Absturz-Sound (fallender Ton, 1 Sekunde) | Flash Crash |

### 17.4 Audio-Implementierung

**Library:** Howler.js (npm Package, gut mit Electron kompatibel)

**Preloading:** Alle SFX werden beim Start geladen (klein, <1 MB gesamt). Musik-Tracks werden on-demand geladen (je ~3-5 MB).

**Lautstärke-Kaskade:**
- Endlautstärke = Master × Kategorie × Sound-spezifisch
- Beispiel: Master 80% × SFX 70% × `click_soft` standardmäßig 50% = effektiv 28%

### 17.5 Audio-Beschaffung (Kostenlos / Royalty-Free)

Alle Audio-Assets müssen kostenlos und kommerziell nutzbar sein (Steam-Release). Keine Lizenzgebühren.

**Musik — Empfohlene Quellen:**
- **Pixabay Music** (pixabay.com/music) — Komplett kostenlos, keine Attribution nötig, kommerzielle Nutzung erlaubt. Große Auswahl an Ambient/Lo-Fi/Electronic.
- **Freesound.org** — Creative Commons Lizenzen, viele CC0 (Public Domain). Gut für Ambient-Loops.
- **Incompetech (Kevin MacLeod)** — CC BY 3.0 (Attribution im Credits-Screen). Große Bibliothek professioneller Musik.
- **Mixkit** (mixkit.co) — Kostenlose Musik und SFX, keine Attribution nötig.
- **Eigene Generierung:** Tools wie Suno AI, Udio oder ähnliche AI-Musik-Generatoren können lizenzfreie Tracks erzeugen. Vorsicht: Lizenzbedingungen des jeweiligen Tools prüfen (einige erlauben kommerzielle Nutzung, andere nicht).

**Suchbegriffe für passende Tracks:**
- Calm Market: 'ambient corporate', 'lo-fi background', 'minimal electronic'
- Steady Trading: 'light electronic pulse', 'corporate ambient beat'
- Rising Momentum: 'uplifting ambient', 'positive corporate'
- Tension Building: 'dark ambient', 'suspense electronic', 'tension building'
- Crisis Mode: 'intense ambient', 'dark drone', 'urgent electronic'

**SFX — Empfohlene Quellen:**
- **Freesound.org** — Größte kostenlose SFX-Bibliothek. Viele CC0-Sounds.
- **Pixabay Sound Effects** — Kostenlos, keine Attribution.
- **Mixkit** — Kostenlose UI-Sounds und Benachrichtigungstöne.
- **Zapsplat** (zapsplat.com) — Kostenloser Account mit großer Auswahl, Attribution nötig.

**Spezifische SFX-Suche:**
| Sound | Suchbegriffe |
|---|---|
| Button Click | 'ui click', 'soft tap', 'interface click' |
| Order Placed | 'confirmation beep', 'positive blip', 'cash register soft' |
| Order Filled | 'cha-ching soft', 'coin drop', 'success chime' |
| Breaking News | 'news jingle', 'breaking news intro', 'alert fanfare' |
| Market Bell | 'bell ring', 'trading bell', 'stock exchange bell' |
| Margin Warning | 'warning alarm soft', 'alert tone double' |
| Error | 'error buzzer soft', 'negative beep' |

**Lizenz-Checkliste vor Verwendung:**
- [ ] Kommerziell nutzbar (für Steam-Verkauf)?
- [ ] Keine Lizenzgebühren (Royalty-Free)?
- [ ] Attribution nötig? Wenn ja: im Credits-Screen aufnehmen
- [ ] Exklusiv? (Darf nicht exklusiv sein — andere Spiele dürfen den gleichen Sound nutzen)
- [ ] AI-generiert? Wenn ja: Plattform-TOS prüfen für kommerzielle Nutzung

**Credits-Screen:**
Im Hauptmenü unter dem Version-Text (oder in Settings → About): Link zu einem Credits-Overlay der alle verwendeten Assets mit Lizenz und Attribution auflistet.

---

## 18. Tastenkürzel (Keyboard Shortcuts)

### 18.1 Zeitsteuerung

| Shortcut | Aktion |
|---|---|
| `Space` | Pause / Fortsetzen (Toggle) |
| `1` | Geschwindigkeit 1x |
| `2` | Geschwindigkeit 2x |
| `3` | Geschwindigkeit 5x |
| `4` | Geschwindigkeit 10x |
| `+` oder `→` | Eine Stufe schneller |
| `-` oder `←` | Eine Stufe langsamer |

### 18.2 Navigation

| Shortcut | Aktion |
|---|---|
| `D` | Dashboard-Tab |
| `P` | Portfolio-Tab |
| `M` | Market-Tab |
| `O` | Orders-Tab |
| `N` | News-Tab |
| `A` | Analytics-Tab |
| `Escape` | Modal schließen / Zurück / Settings |

### 18.3 Trading

| Shortcut | Aktion |
|---|---|
| `B` | Buy-Tab im Order-Panel aktivieren |
| `S` | Sell-Tab im Order-Panel aktivieren |
| `H` | Short-Tab im Order-Panel aktivieren |
| `Enter` | Order bestätigen (wenn Bestätigungsdialog offen: Confirm) |

### 18.4 Suche & Allgemein

| Shortcut | Aktion |
|---|---|
| `Ctrl+F` oder `/` | Globale Aktiensuche fokussieren |
| `Ctrl+S` | Speichern |
| `Ctrl+L` | Laden-Dialog |
| `Ctrl+N` | Neues Spiel |
| `F1` | Hilfe / Glossar |
| `F11` | Vollbild Toggle |
| `Ctrl+Q` | Spiel beenden |
| `Ctrl+J` | Trading Journal öffnen |
| `Ctrl+K` | Price Alert setzen (für ausgewählte Aktie) |
| `Ctrl+Shift+D` | Aktuelles Panel als Fenster auskoppeln |

### 18.5 Chart

| Shortcut | Aktion |
|---|---|
| Mausrad | Zoom in/out |
| Drag | Zeitachse verschieben |
| Doppelklick | Chart zurücksetzen |

**Wichtig:** Shortcuts funktionieren NICHT wenn ein Eingabefeld fokussiert ist (damit der Spieler Zahlen/Text eingeben kann ohne versehentlich Aktionen auszulösen).

---

## 19. Edge Cases & Fehlerzustände

### 19.1 Handels-Fehler

| Situation | Fehlermeldung (Toast, rot) | Verhalten |
|---|---|---|
| Nicht genug Cash für Kauf | `Insufficient funds. You need $X,XXX more.` | Order wird abgelehnt |
| Nicht genug Aktien für Verkauf | `You only own X shares of AAPL.` | Order wird abgelehnt |
| Stückzahl = 0 | (Frontend-Validierung, Button disabled) | Button bleibt disabled |
| Stückzahl negativ | (Frontend-Validierung) | Input akzeptiert keine negativen Zahlen |
| Stückzahl mit Dezimalen (Long Buy/Sell) | Erlaubt, mindestens 0.001 (Fractional Shares). | Short: nur Ganzzahlen erlaubt, Validierungsfehler bei Dezimalen. |
| Limit-Preis ≤ 0 | `Price must be greater than zero.` | Order wird abgelehnt |
| Markt geschlossen (Market Order) | `Market is closed. Your order will execute at market open.` (info, nicht rot) | Order wird als "pending open" gespeichert |
| Aktie ausgesetzt (Circuit Breaker) | `Trading in AAPL is currently halted.` | Order wird abgelehnt |
| Aktie wird delisted | `AAPL is being delisted and cannot be traded.` | Order wird abgelehnt |
| Nicht genug Margin für Short | `Insufficient margin. Required: $X,XXX. Available: $Y,YYY.` | Order wird abgelehnt |

### 19.2 Margin-Fehler

| Situation | Verhalten |
|---|---|
| Margin Call während Pause | Wird sofort bei Fortsetzen verarbeitet |
| Erzwungene Liquidation bei illiquider Aktie | Verkauf zum Best Available Preis (auch mit hoher Slippage) |
| Spieler ignoriert Margin Call (Countdown läuft ab) | Erzwungene Liquidation (kein Entrinnen) |
| Margin Call + gleichzeitig Breaking News | Beide Overlays erscheinen, Margin Call hat Priorität (wird zuerst angezeigt) |
| **Spieler-Bankrott (Cash ≤ $0, keine Positionen)** | Spiel pausiert, Bankrott-Popup mit Optionen: `Restart with $X` (Difficulty-abhängig), `New Game`, `Load Save`. Siehe 1.4 für Details. |
| Spieler kann sich nicht erholen (kein Cash, nur wertlose Positionen) | Wenn alle Positionen <$1 wert: quasi-Bankrott, gleiche Behandlung |
| Restschuld nach Margin-Liquidation | Wenn Margin-Liquidation nicht alle Schulden deckt: negativer Cash-Stand. Spieler ist bankrott. |

### 19.3 Gleichzeitige Krisen (Priority Queue)

Wenn mehrere Events gleichzeitig um die Aufmerksamkeit des Spielers konkurrieren, gilt folgende Prioritätsreihenfolge:

| Priorität | Event | Begründung |
|---|---|---|
| 1 (höchste) | Circuit Breaker (Market Halt) | Alles stoppt |
| 2 | Spieler-Bankrott | Spielbeendend, muss adressiert werden |
| 3 | Margin Call (erzwungene Liquidation imminent) | Zeitkritisch |
| 4 | Margin Warning | Dringend, aber nicht unmittelbar |
| 5 | Breaking News (Catastrophic) | Wichtig, aber keine Spieleraktion nötig |
| 6 | Breaking News (Major) | Informativ |
| 7 | Short Squeeze Warning | Warnung |
| 8 | Price Alert ausgelöst | Benachrichtigung |
| 9 | Order Executed Benachrichtigung | Routine |
| 10 (niedrigste) | Standard News | Informativ |

**Regeln:**
- Es wird immer nur EIN Modal gleichzeitig angezeigt. Weitere werden in eine Queue gestellt und erscheinen nach dem Schließen des aktuellen Modals.
- Toasts können gestapelt werden (maximal 3 gleichzeitig sichtbar).
- Wenn eine erzwungene Liquidation während eines Circuit Breaker Halts stattfindet: Die Liquidation wird zum Wiedereröffnungspreis ausgeführt.
- Wenn Bankrott und Breaking News gleichzeitig auftreten: Bankrott-Popup hat Priorität, News erscheint danach.

### 19.4 Markt-Extremsituationen

| Situation | Verhalten |
|---|---|
| Aktienpreis erreicht $0 | Delisting-Event wird automatisch ausgelöst, Position wird wertlos |
| Aktienpreis steigt >1000% in kurzer Zeit | Kein Eingriff — das kann bei Micro Caps passieren (realistisch) |
| Kein Volumen (komplett illiquide) | Market Order: `No liquidity available for AAPL. Try again later.` |
| Markt-Crash-Spirale (Index -20%+) | Circuit Breaker Level 3: Handel wird für den Rest des Tages ausgesetzt |
| Alle AI-Trader verkaufen gleichzeitig | Dampening: AI-Entscheidungen werden zeitlich gespreizt (nicht alle im gleichen Tick) |

### 19.5 System-Fehler

| Situation | Verhalten |
|---|---|
| WebSocket-Verbindung verloren | Retry alle 2 Sekunden, Spiel wird pausiert, Banner `Connection lost. Reconnecting...` |
| Backend-Crash | Modal: `Simulation has stopped unexpectedly. Your game was auto-saved X minutes ago. [Load Autosave] [Quit]` |
| Out of Memory | Warnung bei Neues-Spiel-Dialog wenn >1000 Aktien: `Large markets require more memory. 8GB+ recommended.` |
| Savegame von neuer Version in alter Version | `This save was created with a newer version of StockSim and cannot be loaded.` |

### 19.6 UI-Edge-Cases

| Situation | Lösung |
|---|---|
| Extrem langer Firmenname | Truncation mit Ellipsis (`...`). Tooltip zeigt vollen Namen. Max-Breite beachten. |
| Preis >$10,000 | Darstellung als `$12,345.67` (normales Format, kein Abkürzen) |
| Preis <$0.01 | Darstellung mit 4 Dezimalstellen: `$0.0034` |
| Volumen >1 Milliarde | Darstellung als `1.2B` |
| Leere Watchlist | Platzhalter (siehe 3.3.1) |
| Keine Positionen | Platzhalter (siehe 6.1.2) |
| Keine offenen Orders | Platzhalter |
| Keine News (sollte nicht vorkommen) | Platzhalter: `No news yet. Events will appear as the market moves.` |
| Fenster zu klein | Unter 1280×720: Scrollbar erscheint, aber UI bricht nicht |

---

## 20. Phasenplanung (Roadmap)

### 20.1 Phase 1: MVP (Minimum Viable Product)

**Ziel:** Ein spielbares, funktionierendes Produkt mit den Kern-Mechaniken.

**Must-Have Features:**
- [ ] Electron + React Frontend lauffähig
- [ ] C# .NET Backend lauffähig
- [ ] WebSocket-Kommunikation stabil
- [ ] Prozedurale Aktien-Generierung (100-200 Aktien, 8 Sektoren)
- [ ] Basis-Preissimulation (Brownian Motion + Drift + Zufall)
- [ ] Market Orders (Buy / Sell)
- [ ] Limit Orders (Buy / Sell)
- [ ] Candlestick-Charts (TradingView Lightweight Charts)
- [ ] Zeitsteuerung (Pause, 1x, 2x, 5x)
- [ ] Portfolio-Anzeige (Positionen, P&L)
- [ ] Watchlist
- [ ] Markt-Tabelle (alle Aktien, sortierbar, filterbar)
- [ ] AI Market Maker (Orderbook-Generierung)
- [ ] AI Retail Trader (grundlegendes FOMO/Panik-Verhalten)
- [ ] 10-15 Event-Templates (5 Macro, 5 Company, 5 Sector)
- [ ] News-Ticker
- [ ] Grundlegende Top Bar, Sidebars, Zentralbereich
- [ ] Speichern / Laden (1 Slot + Autosave)
- [ ] Tastenkürzel (Zeitsteuerung + Navigation)
- [ ] Dark Theme (Basisfarben, keine Glow-Effekte)
- [ ] Performance: 200 Aktien bei 5x ohne Ruckeln

### 20.2 Phase 2: Feature-Complete Singleplayer

**Ziel:** Alle geplanten Singleplayer-Features implementiert.

- [ ] Short Selling + Short Squeeze + Margin Call
- [ ] Margin Trading (Long)
- [ ] Stop Orders, Stop-Limit, Trailing Stop
- [ ] 12 Sektoren, 500+ Aktien
- [ ] Alle AI-Trader-Typen (Institutionelle, Algo, Insider, Short Seller)
- [ ] Vollständiges Event-System (50+ Templates, Kaskadierung, Kalender)
- [ ] Tutorial (7 Schritte + Tooltips)
- [ ] Orderbook-Visualisierung
- [ ] Technische Indikatoren (SMA, EMA, RSI, MACD, Bollinger)
- [ ] Analytics-Tab (Performance, Risiko-Metriken)
- [ ] Dashboard Heatmap
- [ ] 10x Geschwindigkeit
- [ ] Mehrere Save-Slots
- [ ] Vollständige Settings
- [ ] Audio (5 Musik-Tracks + alle SFX)
- [ ] Trade-Historie + Statistiken
- [ ] Pump & Dump Dynamik
- [ ] Insider-Rumors
- [ ] Glossar / Help-System
- [ ] IPO und Delisting Events
- [ ] Flash Crash und Circuit Breaker
- [ ] Dividenden + Ex-Dividend-Date Mechanik
- [ ] Gap Up / Gap Down bei Market Open
- [ ] Earnings Calendar + Upcoming Events Sidebar
- [ ] Analyst Ratings & Price Targets (Events + UI)
- [ ] Stock Splits + Reverse Splits (Events + Chart-Adjustment)
- [ ] Secondary Offerings (Events)
- [ ] Price Alerts System
- [ ] Mehrere Watchlists
- [ ] Chart-Vergleich (Compare/Overlay)
- [ ] Time & Sales (Tape)
- [ ] Erweiterter Aktien-Screener mit Presets
- [ ] Trading Journal / Notizen
- [ ] Corporate Buyback Mechanik (AI)
- [ ] SMA Regulierungssystem (Suspicion Score, Warnungen, Strafen)
- [ ] Illegale Handlungen (Spoofing, Wash Trading, Pump&Dump Erkennung)
- [ ] Steuersystem (Capital Gains Tax, Tax Loss Harvesting)
- [ ] Spin-Off Events + Spieler-Aktienverteilung
- [ ] Tender Offers + Spieler-Entscheidungspopup
- [ ] Chapter 11 vs Chapter 7 Bankruptcy (differenziert)
- [ ] Class Action Lawsuits (Events + Follow-ups)
- [ ] Antitrust-Verfahren (Events)
- [ ] Regulatorische Events (SMA Crackdowns, neue Regeln)
- [ ] Wirtschaftszyklus-System (4 Phasen, Sektor-Rotation)
- [ ] Float / Shares Outstanding / Ownership-Tracking
- [ ] Geopolitische Events (Kriege, Sanktionen, Wahlen)
- [ ] Naturkatastrophen-Events
- [ ] Arbeitskämpfe / Streiks
- [ ] Earnings Forward Guidance (separat von Earnings)
- [ ] Credit Rating Changes (Company + Country)
- [ ] Activist Investor Campaigns
- [ ] IPO Lock-Up Expiration Events
- [ ] Index Inclusion / Exclusion Events
- [ ] Special Dividends
- [ ] VWAP Indikator
- [ ] Farbenblind-Modus (3 Modi)
- [ ] Achievement-System (Steam-kompatibel)
- [ ] Scenario Mode (10+ vordefinierte Szenarien)
- [ ] Spieler-Bankrott-Mechanik + Neustart
- [ ] Difficulty-spezifische Mechaniken (Easy/Normal/Hard detailliert)
- [ ] Multi-Window-System (Detachable Panels, Multi-Monitor)

### 20.3 Phase 3: Polish & Steam Release

**Ziel:** Spielqualität auf Steam-Release-Level heben.

- [ ] Steam-Integration (Achievements, Cloud Saves)
- [ ] Performance-Optimierung (1000+ Aktien bei 10x)
- [ ] UI-Polish: alle Animationen, Glow-Effekte, Transitions
- [ ] Sound-Polish: Musik-Übergänge, SFX-Balance
- [ ] Balance-Tuning: AI-Verhalten, Event-Häufigkeit, Schwierigkeitsgrade
- [ ] Bug-Fixing und Edge-Case-Behandlung
- [ ] Lokalisierungs-Infrastruktur (i18n-ready, auch wenn zunächst nur Englisch)
- [ ] Steam Store Page, Screenshots, Trailer
- [ ] Beta-Testing
- [ ] Zeichenwerkzeuge im Chart (Trendlinien, horizontale Linien)
- [ ] Erweitertes Tutorial (für Intermediate/Expert-Spieler)

### 20.4 Phase 4: Post-Launch & Stretch Goals

- [ ] Options-Handel (Calls / Puts)
- [ ] IPO-Teilnahme als Spieler (vor Börsenlisting kaufen)
- [ ] Weitere Event-Templates (100+)
- [ ] Sektor-ETFs / Index-Fonds als handelbare Instrumente
- [ ] Portfolio-Export (CSV)
- [ ] Erweiterte Statistiken
- [ ] Fibonacci Retracement und weitere Zeichentools
- [ ] Multiplayer (separates Designdokument)
- [ ] Steam Workshop (Custom Events, Custom Aktien-Packs)
- [ ] Weitere Sprachen

---

## 21. Technische Architektur (Anhang)

### 21.1 Frontend-Architektur (React/TypeScript)

**Component-Hierarchie (Überblick):**
```
<App>
  <TopBar>
    <Logo />
    <NavigationTabs />
    <MarketSummaryTicker />
    <GameTime />
    <SpeedControls />
    <CashDisplay />
    <SaveButton />
    <SettingsButton />
  </TopBar>

  <MainLayout>
    <LeftSidebar>
      <WatchlistPanel />
      <SectorsPanel />
    </LeftSidebar>

    <CentralArea>
      {/* Conditional rendering based on active tab */}
      <DashboardView />
      <PortfolioView />
      <MarketView />
      <OrdersView />
      <NewsView />
      <AnalyticsView />
      <StockDetailView /> {/* Overlay on any tab */}
    </CentralArea>

    <RightSidebar>
      <SelectedStockInfo />
      <OrderEntryPanel />
      <PositionQuickView />
    </RightSidebar>
  </MainLayout>

  <NewsTicker />

  {/* Modals & Overlays */}
  <OrderConfirmModal />
  <BreakingNewsPopup />
  <SettingsModal />
  <SaveLoadModal />
  <TutorialOverlay />
  <ToastContainer />
</App>
```

**State Management:** Zustand (leichtgewichtig, besser als Redux für dieses Projekt).

**Stores:**
- `marketStore`: alle Aktiendaten, Preise, Indizes
- `portfolioStore`: Positionen, Cash, P&L
- `orderStore`: offene und historische Orders
- `newsStore`: News-Feed, Ticker-Einträge
- `gameStore`: Spielzeit, Geschwindigkeit, Markt-Status
- `uiStore`: aktiver Tab, ausgewählte Aktie, Modal-Zustand
- `settingsStore`: alle Einstellungen

**WebSocket-Client:**
- Singleton-Service `WebSocketService`
- Automatischer Reconnect bei Verbindungsverlust (Exponential Backoff: 1s, 2s, 4s, 8s, max 30s)
- Message-Parser: empfängt JSON, dispatcht an die richtigen Stores
- Outgoing Queue: bei Verbindungsverlust werden ausgehende Messages gequeued und bei Reconnect gesendet

### 21.2 Backend-Architektur (C# .NET)

**Service-Architektur:**
```
Program.cs (Entry Point)
  └── GameEngine (Haupt-Game-Loop)
       ├── SimulationService (Tick-basierte Preisberechnung)
       ├── TradingService (Order-Matching, Ausführung)
       ├── AIService (AI-Trader-Entscheidungen)
       ├── EventService (Event-Generierung, Scheduling, Auslösung)
       ├── MarketDataService (Orderbook, Indizes, Statistiken)
       ├── PortfolioService (Spieler-Portfolio, Margin-Checks)
       ├── SaveService (Speichern/Laden)
       └── WebSocketServer (Kommunikation mit Frontend)
```

**Game Loop:**
```csharp
while (running) {
    if (isPaused) { Thread.Sleep(50); continue; }

    var tickStart = DateTime.Now;

    simulationService.AdvanceTime();
    eventService.ProcessEvents();
    aiService.MakeDecisions();
    tradingService.MatchOrders();
    simulationService.CalculatePrices();
    marketDataService.UpdateOrderbooks();
    portfolioService.CheckMargins();

    var update = marketDataService.BuildUpdatePacket();
    webSocketServer.SendUpdate(update);

    var tickDuration = DateTime.Now - tickStart;
    var targetDuration = GetTargetTickDuration(currentSpeed);
    if (tickDuration < targetDuration) {
        Thread.Sleep(targetDuration - tickDuration);
    }
}
```

**Threading-Modell:**
- **Game Loop Thread:** Hauptthread für die Simulation (single-threaded für Determinismus)
- **WebSocket Thread:** separater Thread für Netzwerk-I/O
- **Save Thread:** Speichern läuft asynchron auf eigenem Thread (blockiert nicht den Game Loop)

**Backend-Start und Lifecycle:**

Das C#-Backend wird als Child-Process von Electron gestartet:

1. Electron-App startet → Splash Screen wird angezeigt
2. Electron spawnt den C#-Prozess (`dotnet StockSim.Backend.dll`)
3. C#-Backend startet WebSocket-Server auf `localhost:8765` (konfigurierbarer Port)
4. C#-Backend sendet 'ready' Message über stdout an Electron
5. Electron-Frontend verbindet sich zum WebSocket
6. Handshake: Frontend sendet `{type: 'hello', version: '1.0'}`, Backend antwortet `{type: 'welcome', version: '1.0'}`
7. Splash Screen schließt → Hauptmenü wird angezeigt

**Healthcheck:**
- Frontend sendet alle 5 Sekunden ein `{type: 'ping'}` → Backend antwortet `{type: 'pong'}`
- Wenn 3 Pings ohne Pong: 'Connection lost' Banner (siehe 19.4)
- Backend prüft ob Electron noch läuft (Parent-Process-ID). Wenn nicht: Backend beendet sich selbst.

**Shutdown:**
- Spieler beendet das Spiel → Frontend sendet `{type: 'shutdown'}` → Backend speichert Zustand und beendet sich
- Bei Crash: Electron erkennt Child-Process-Exit → Fehlerdialog (siehe 19.4)

### 21.3 WebSocket-Protokoll

**Message-Typen (Frontend → Backend):**

| Type | Payload | Beschreibung |
|---|---|---|
| `PlaceOrder` | `{symbol, side, type, qty, price?, stopPrice?, trailAmount?, tif}` | Order platzieren |
| `CancelOrder` | `{orderId}` | Order stornieren |
| `ModifyOrder` | `{orderId, newQty?, newPrice?}` | Order ändern |
| `SetSpeed` | `{speed: 0-4}` | Geschwindigkeit ändern |
| `Save` | `{saveName}` | Spiel speichern |
| `Load` | `{saveName}` | Spiel laden |
| `NewGame` | `{config}` | Neues Spiel starten |
| `UpdateSettings` | `{settings}` | Einstellungen ändern |
| `AddToWatchlist` | `{symbol}` | Aktie zur Watchlist |
| `RemoveFromWatchlist` | `{symbol}` | Aktie von Watchlist entfernen |

**Message-Typen (Backend → Frontend):**

| Type | Payload | Frequenz |
|---|---|---|
| `MarketUpdate` | `{prices: [{symbol, bid, ask, last, change, volume}...], indices: [...], gameTime}` | Jeder Update-Zyklus |
| `OrderUpdate` | `{orderId, status, filledPrice?, filledQty?, commission?}` | Bei Änderung |
| `PortfolioUpdate` | `{cash, positions: [...], totalValue, dayPnL, totalPnL, marginDebt, buyingPower}` | Jeder Update-Zyklus |
| `NewsItem` | `{id, type, severity, sentiment, title, description, symbols, sectors, timestamp}` | Bei Event |
| `BreakingNews` | `{...NewsItem, autoPause: bool}` | Bei Major/Catastrophic Event |
| `MarketStatus` | `{isOpen, nextOpen?, nextClose?}` | Bei Statuswechsel |
| `SaveConfirmation` | `{success, saveName}` | Nach Speichern |
| `Error` | `{code, message}` | Bei Fehlern |
| `OHLCVUpdate` | `{symbol, timeframe, candles: [{time, o, h, l, c, v}...]}` | Bei Chart-Anfrage |
| `OrderbookUpdate` | `{symbol, bids: [{price, qty}...], asks: [{price, qty}...]}` | Bei Chart-Ansicht aktiv |

**Batching:** `MarketUpdate` enthält nur Aktien, deren Preis sich seit dem letzten Update geändert hat (Delta-Updates). Bei 500 Aktien und 1x Speed: typischerweise 50-100 Aktualisierungen pro Tick.

### 21.4 Performance-Budgets

| Metrik | Ziel |
|---|---|
| Frontend Frame Rate | 60 FPS |
| Frontend Frame Budget | <16ms pro Frame |
| Backend Tick (1x) | <500ms (Budget: 1000ms) |
| Backend Tick (10x) | <80ms (Budget: 100ms) |
| WebSocket Message Size | <50 KB pro Update |
| WebSocket Messages/sec | ≤5 |
| Electron RAM | <500 MB |
| C# Backend RAM | <300 MB (500 Aktien), <600 MB (1000 Aktien) |
| Startup Time | <5 Sekunden bis spielbereit |
| Save Time | <3 Sekunden |
| Load Time | <5 Sekunden |

### 21.5 Balancing & Tuning-Prozess

Die Simulation hat hunderte einstellbare Parameter. Ein systematischer Tuning-Prozess ist nötig.

**Balancing-Parameter-Kategorien:**
1. **Preis-Engine:** Drift, Volatilität, Mean-Reversion-Stärke, Order-Impact-Faktor
2. **AI-Verhalten:** FOMO-Bias, Panik-Bias, Entscheidungsfrequenz, Positionsgröße
3. **Events:** Häufigkeit, Stärke, Kaskadierungs-Wahrscheinlichkeit
4. **Wirtschaftszyklus:** Phasendauer, Übergangstrigger, Sektor-Rotation-Stärke
5. **Regulierung:** SMA-Erkennungsraten, Score-Zuschläge, Strafbeträge

**Tuning-Methode: Headless Simulation**
Das C#-Backend kann OHNE Frontend als Headless-Simulation laufen. Dies ermöglicht:
- 1000× Speed (keine UI-Updates, keine WebSocket-Latenz)
- Automatische Simulation von 10 Jahren Spielzeit in Minuten
- Statistische Auswertung: Markt-Returns, Volatilitäts-Verteilung, Event-Häufigkeit, AI-Performance
- A/B-Testing: Parameter-Set A vs. B, vergleiche Ergebnisse

**Realismus-Kriterien (Was 'gut' bedeutet):**
| Metrik | Zielwert | Realer Vergleich |
|---|---|---|
| Jährliche Markt-Rendite | 5-12% (Expansion), -10 bis -30% (Contraction) | S&P 500 historisch ~10% p.a. |
| Tägliche Volatilität (Index) | 0.5-1.5% (Normal), 2-5% (Crisis) | VIX 15-25 = 1-1.5%/Tag |
| Max Drawdown pro Zyklus | -20 bis -45% | 2008: -57%, 2020: -34% |
| Earnings-Beat-Reaktion | +3-8% für Beats, -5-15% für Misses | Reale Durchschnittswerte |
| Short Squeeze Häufigkeit | 1-3 pro Jahr | Historisch ~2-5 pro Jahr |
| Flash Crash Häufigkeit | 0-1 pro Jahr | ~1 alle 2-3 Jahre real |
| Spieler kann Markt schlagen | ~60% der Spieler bei Normal-Difficulty | Realistisch: ~15% schlagen den Index |

**Tuning-Workflow:**
1. Parameter definieren (in Config-Datei, nicht hardcoded)
2. Headless-Simulation laufen lassen (100 Durchläufe à 5 Spieljahre)
3. Statistiken auswerten gegen Realismus-Kriterien
4. Parameter anpassen
5. Wiederholen bis Kriterien erfüllt
6. Playtest mit echten Spielern (Spaß-Faktor: subjektiv, aber nicht vernachlässigbar)

### 21.6 Tiered Simulation (Skalierung auf 5.000+ Aktien)

Um mehrere tausend Aktien performant zu simulieren, verwendet das Backend ein Stufen-System.

**Vier Simulations-Stufen:**

| Tier | Beschreibung | Simulation | CPU-Kosten | Aktien |
|---|---|---|---|---|
| **Tier 1: Full** | Volle Simulation | Individuelles Orderbook, alle AI-Typen, vollständige Events, Partial Fills | Hoch (~2ms/Aktie) | 20-50 |
| **Tier 2: Standard** | Normal-Simulation | Vereinfachtes Orderbook (5 statt 10 Levels), Standard-AI, Events | Mittel (~0.5ms/Aktie) | 100-500 |
| **Tier 3: Light** | Leicht-Simulation | Kein Orderbook, nur Hintergrund-Volumen + Preis-Modell, Basis-AI | Niedrig (~0.1ms/Aktie) | 500-2.000 |
| **Tier 4: Dormant** | Minimal-Simulation | Nur Preisupdate (Drift + Zufall + Sektor-Korrelation), kein Volumen, kein AI | Minimal (~0.01ms/Aktie) | Rest |

**Tier-Zuweisung (dynamisch):**

Eine Aktie wird Tier 1 zugewiesen wenn:
- Sie in der Watchlist des Spielers ist
- Der Spieler eine offene Position hat
- Der Spieler eine offene Order hat
- Der Spieler sie im Aktien-Detail betrachtet

Tier 2 wenn:
- Sie im gleichen Sektor wie eine Tier-1-Aktie ist
- Sie eine Large/Mega Cap ist
- Sie in den letzten 30 Tagen ein Event hatte

Tier 3 wenn:
- Sie eine Mid/Small Cap ist ohne Spieler-Interaktion

Tier 4 wenn:
- Sie eine Micro Cap ist ohne Spieler-Interaktion
- Sie in den letzten 60 Tagen kein Event hatte

**Tier-Wechsel:**
- Upgrade (z.B. Tier 3 → Tier 1): sofort wenn der Spieler die Aktie anklickt. Orderbook wird in ~100ms generiert.
- Downgrade (z.B. Tier 1 → Tier 3): nach 5 Minuten ohne Spieler-Interaktion
- Event an einer Tier-3/4-Aktie: temporäres Upgrade auf Tier 2 für die Dauer des Event-Effekts

**Performance-Berechnung bei 5.000 Aktien:**

```
Tier 1:    50 Aktien  × 2.0 ms = 100 ms ← zu viel!
Tier 2:   300 Aktien  × 0.5 ms =  15 ms
Tier 3: 1.500 Aktien  × 0.1 ms =  15 ms
Tier 4: 3.150 Aktien  × 0.01ms =   3 ms
─────────────────────────────────────────
Summe:                          ~133 ms ← zu viel für 10x Speed
```

**Optimierung:** Tier 1 wird auf max 30 Aktien begrenzt. Rest:

```
Tier 1:    30 Aktien  × 2.0 ms =  60 ms
Tier 2:   200 Aktien  × 0.5 ms =  10 ms
Tier 3: 1.500 Aktien  × 0.1 ms =  15 ms
Tier 4: 3.270 Aktien  × 0.01ms =   3 ms
─────────────────────────────────────────
Summe:                           ~88 ms ← passt in 100ms Budget!
```

**Erweiterte Marktgrößen-Presets:**

| Preset | Aktien | ETFs | REITs | Sektoren | Empfohlen für | RAM |
|---|---|---|---|---|---|---|
| Small | 250 | 9 | 5 | 8 | Schwache PCs, schnelles Spiel | ~200 MB |
| Standard | 1.000 | 13 | 15 | 12 | Empfohlen | ~400 MB |
| Large | 2.500 | 13 | 30 | 12 | Starke PCs, maximale Vielfalt | ~700 MB |
| Massive | 5.000 | 13 | 50 | 12 | Enthusiasten, Tiered Simulation aktiv | ~1.2 GB |
| Custom | 100-10.000 | auto | auto | 8-12 | Slider mit Performance-Warnung | variabel |

Warnung bei Massive/Custom >3.000: 'Large markets require more memory (16GB+ recommended) and may reduce maximum simulation speed.'

Warnung bei Custom >5.000: 'Very large markets use Tiered Simulation. Stocks you are not actively watching will have simplified price models.'

---

## 22. Anhang

### 22.1 Beispiel-Event-Templates (33 ausgearbeitete Beispiele)

**1. Earnings Beat (Company, Major, +0.6)**
- Title: `[COMPANY] Smashes Q[Q] Earnings Expectations`
- Body: `[COMPANY] reported earnings per share of $[EPS], significantly beating analyst estimates of $[EXPECTED]. Revenue grew [GROWTH]% year-over-year to $[REVENUE]B, driven by strong performance in [SEGMENT].`
- Effekt: Aktie +8-12%, Sektor +1-2%, Vola 1.5×, 3 Tage

**2. Earnings Miss (Company, Major, -0.6)**
- Title: `[COMPANY] Misses Earnings, Cuts Guidance`
- Body: `[COMPANY] fell short of Wall Street expectations, posting EPS of $[EPS] versus the $[EXPECTED] consensus. Management lowered full-year guidance, citing [REASON].`
- Effekt: Aktie -10-18%, Sektor -1-2%, Vola 2×, 5 Tage

**3. Fed Rate Hike (Macro, Major, -0.5)**
- Title: `Federal Reserve Raises Rates by [BPS] Basis Points`
- Body: `The Federal Reserve increased the federal funds rate to [RATE]%, citing [REASON]. Fed Chair noted that further adjustments will depend on incoming economic data.`
- Effekt: Markt -2-3%, Real Estate -5%, Tech -3%, Financials +1%, Vola 1.5×, 5 Tage

**4. CEO Scandal (Company, Major, -0.7)**
- Title: `[COMPANY] CEO Resigns Amid [SCANDAL_TYPE] Allegations`
- Body: `The chief executive of [COMPANY] has stepped down following allegations of [SCANDAL_TYPE]. The board has appointed [NAME] as interim CEO while conducting a search for a permanent replacement.`
- Effekt: Aktie -12-20%, Vola 2.5×, 7 Tage. Follow-up: 60% Chance "Board announces permanent CEO" (+5%)

**5. Oil Spike (Sector, Moderate, +0.4)**
- Title: `Oil Prices Surge [CHANGE]% on Supply Concerns`
- Body: `Crude oil prices jumped [CHANGE]% to $[PRICE] per barrel after [REASON], raising concerns about energy costs for consumers and businesses.`
- Effekt: Energy +4-6%, Transport -2-3%, Consumer -1%, Vola 1.3×, 3 Tage

**6. Tech Regulation (Sector, Moderate, -0.3)**
- Title: `Lawmakers Propose Sweeping Tech Regulation Bill`
- Body: `A bipartisan group of legislators introduced a bill that would impose new restrictions on [AREA], potentially impacting major technology companies' business models.`
- Effekt: Tech -3-5%, Vola 1.5×, 5 Tage

**7. Acquisition Announced (Company, Major, +0.7 Target / -0.2 Acquirer)**
- Title: `[ACQUIRER] to Acquire [TARGET] for $[PRICE]B`
- Body: `[ACQUIRER] announced a definitive agreement to acquire [TARGET] at $[SHARE_PRICE] per share, representing a [PREMIUM]% premium to yesterday's closing price. The deal is expected to close in [MONTHS] months.`
- Effekt: Target +20-35% (to near deal price), Acquirer -3-5%, Vola target 0.5× (price anchored to deal)

**8. Flash Crash (Market, Catastrophic, -0.8)**
- Title: `Markets Plunge in Sudden Flash Crash`
- Body: `Major indices dropped [DROP]% within minutes in what appears to be an algorithmic trading malfunction. Circuit breakers were triggered as panic selling cascaded across multiple sectors.`
- Effekt: Markt -5-8% sofort, Recovery +3-5% über nächste Stunde, Vola 3×, 1 Tag

**9. Dividend Cut (Company, Moderate, -0.4)**
- Title: `[COMPANY] Slashes Dividend by [CUT]%`
- Body: `[COMPANY] announced a [CUT]% reduction in its quarterly dividend, from $[OLD] to $[NEW] per share, as management prioritizes [REASON].`
- Effekt: Aktie -6-10%, besonders bei Dividend-Aktien, Vola 1.5×, 3 Tage

**10. Pandemic Scare (Macro, Catastrophic, -0.9)**
- Title: `WHO Declares Health Emergency as [DISEASE] Spreads`
- Body: `The World Health Organization declared a public health emergency as [DISEASE] cases surge across [REGIONS]. Governments are implementing travel restrictions and containment measures.`
- Effekt: Markt -8-15%, Healthcare +5-10%, Transport -10-15%, Consumer -5-8%, Vola 3×, 20 Tage

**11. Product Launch Success (Company, Moderate, +0.4)**
- Title: `[COMPANY] Launches [PRODUCT] to Strong Demand`
- Body: `[COMPANY]'s new [PRODUCT] has exceeded initial sales projections, with [NUMBER] units sold in the first [PERIOD]. Analysts are raising price targets.`
- Effekt: Aktie +5-8%, Vola 1.3×, 3 Tage

**12. Bank Crisis (Sector, Catastrophic, -0.8)**
- Title: `[BANK] Faces Liquidity Crisis, Shares Halted`
- Body: `Trading in [BANK] shares was halted after the stock plunged [DROP]% amid reports of a liquidity crunch. Regulators are monitoring the situation closely as contagion fears spread.`
- Effekt: Bank -40-60%, Financials -8-12%, Markt -3-5%, Vola 3×, 10 Tage

**13. Short Report (Company, Moderate, -0.4)**
- Title: `Short Seller Targets [COMPANY], Calls It "Overvalued"`
- Body: `Prominent short seller [AI_NAME] released a report alleging that [COMPANY] has overstated its [METRIC] by as much as [PERCENT]%. The company has denied the allegations.`
- Effekt: Aktie -8-15%, Short Interest steigt, Vola 2×, 5 Tage

**14. Merger Blocked (Company, Moderate, -0.3)**
- Title: `Regulators Block [ACQUIRER]-[TARGET] Merger`
- Body: `Antitrust regulators have blocked the proposed $[PRICE]B merger between [ACQUIRER] and [TARGET], citing concerns about reduced competition in [MARKET].`
- Effekt: Target -15-25% (Premium verschwindet), Acquirer +3-5%, Vola 1.5×, 3 Tage

**15. Jobs Report Strong (Macro, Minor, +0.2)**
- Title: `Economy Adds [NUMBER]K Jobs, Beating Expectations`
- Body: `The latest employment report showed [NUMBER],000 new jobs, above the [EXPECTED],000 forecast. The unemployment rate held steady at [RATE]%.`
- Effekt: Markt +0.5-1%, Vola 1.1×, 1 Tag

**16. IPO Announcement (Market, Moderate, +0.3)**
- Title: `[COMPANY] Files for IPO, Plans to List at $[PRICE]-$[PRICE_HIGH]`
- Body: `[COMPANY], a [SECTOR] company known for [DESCRIPTION], has filed for an initial public offering. The company aims to raise $[AMOUNT]M at a valuation of $[VALUATION]B.`
- Effekt: Kein sofortiger Preiseffekt (neue Aktie erscheint in 5 Tagen)

**17. Insider Buying (Company, Minor, +0.2)**
- Title: `[COMPANY] Insiders Purchase $[AMOUNT]M in Stock`
- Body: `SEC filings reveal that multiple [COMPANY] insiders have purchased shares totaling $[AMOUNT]M over the past week, the largest insider buying activity in [PERIOD].`
- Effekt: Aktie +2-4%, Vola 1.1×, 2 Tage

**18. Currency Crisis (Macro, Major, -0.5)**
- Title: `[CURRENCY] Plunges [DROP]% Amid Economic Turmoil`
- Body: `The [CURRENCY] fell sharply against major currencies as [COUNTRY] faces mounting economic challenges including [REASON].`
- Effekt: Markt -2-4%, Exporters -3-5%, Vola 2×, 7 Tage

**19. Renewable Energy Push (Sector, Moderate, +0.3)**
- Title: `Government Announces $[AMOUNT]B Renewable Energy Package`
- Body: `The government unveiled a $[AMOUNT]B investment package for renewable energy, including subsidies for solar and wind projects, and tax incentives for clean energy adoption.`
- Effekt: Renewables +6-10%, Traditional Energy -2-3%, Vola 1.3×, 5 Tage

**20. Circuit Breaker Triggered (Market, Catastrophic, -0.7)**
- Title: `Market-Wide Circuit Breaker Triggered After [DROP]% Drop`
- Body: `All trading has been suspended for [DURATION] after the market index fell [DROP]% from yesterday's close, triggering Level [LEVEL] circuit breaker protections.`
- Effekt: Handel gestoppt für X Minuten, nach Wiedereröffnung: hohe Vola, Erholung oder weitere Verluste

**21. Stock Split (Company, Moderate, +0.2)**
- Title: `[COMPANY] Announces [X]-for-1 Stock Split`
- Body: `[COMPANY]'s board approved a [X]-for-1 stock split, making shares more accessible to retail investors. The split will take effect on [DATE]. Current shareholders will receive [X-1] additional shares for each share held.`
- Effekt: Aktie +3-8% bis zum Split-Datum (Retail-Begeisterung), am Split-Tag mechanische Preisanpassung

**22. Reverse Split (Company, Minor, -0.3)**
- Title: `[COMPANY] Executes 1-for-[X] Reverse Stock Split`
- Body: `[COMPANY] completed a 1-for-[X] reverse stock split to bring its share price above $[MIN_PRICE] and maintain its exchange listing. Shareholders now hold [1/X] as many shares at [X]x the price.`
- Effekt: Aktie -5-15%, negatives Signal, erhöhte Vola

**23. Secondary Offering (Company, Moderate, -0.3)**
- Title: `[COMPANY] Prices Secondary Offering at $[PRICE]`
- Body: `[COMPANY] priced a secondary offering of [SHARES]M shares at $[PRICE], a [DISCOUNT]% discount to the last closing price, raising $[AMOUNT]M to fund [PURPOSE].`
- Effekt: Aktie -5-12% (Verwässerung), Vola 1.5×, 3 Tage

**24. Analyst Upgrade (Company, Minor, +0.2)**
- Title: `[ANALYST] Upgrades [SYMBOL] to Buy, Raises Target to $[TARGET]`
- Body: `Analysts at [ANALYST] upgraded [COMPANY] from [OLD_RATING] to Buy, citing [REASON]. The new price target of $[TARGET] implies [UPSIDE]% upside from current levels.`
- Effekt: Aktie +2-5%, Vola 1.2×, 1-2 Tage

**25. Analyst Downgrade (Company, Minor, -0.2)**
- Title: `[ANALYST] Downgrades [SYMBOL] to Sell, Cuts Target to $[TARGET]`
- Body: `[ANALYST] downgraded [COMPANY] to Sell from [OLD_RATING], lowering the price target to $[TARGET] and citing concerns about [REASON].`
- Effekt: Aktie -3-6%, Vola 1.3×, 1-2 Tage

**26. Buyback Announcement (Company, Minor, +0.2)**
- Title: `[COMPANY] Announces $[AMOUNT]B Share Buyback Program`
- Body: `[COMPANY]'s board authorized a $[AMOUNT]B share repurchase program, signaling management's confidence in the company's long-term outlook. The program is expected to be executed over [MONTHS] months.`
- Effekt: Aktie +2-4%, dann stetiger Kaufdruck über Wochen (Corporate Buyback AI wird aktiviert)

**27. Insider Selling (Company, Minor, -0.15)**
- Title: `[COMPANY] Insiders Sell $[AMOUNT]M in Stock`
- Body: `SEC filings show [COMPANY] executives sold [SHARES] shares worth $[AMOUNT]M over the past week. While insider selling can occur for personal reasons, the scale has drawn attention.`
- Effekt: Aktie -1-3%, Vola 1.1×, 1 Tag (leicht negatives Signal)

**28. Spin-Off Announcement (Company, Moderate, +0.2)**
- Title: `[COMPANY] to Spin Off [DIVISION] as Independent Company`
- Body: `[COMPANY] announced plans to separate its [DIVISION] unit into an independently traded public company, [NEW_COMPANY]. Current shareholders will receive [RATIO] shares of [NEW_SYMBOL] for each share of [SYMBOL] held on the record date.`
- Effekt: Muttergesellschaft ±5%, positives Langzeitsignal, neue Aktie wird generiert

**29. Tender Offer (Company, Major, +0.6 Target)**
- Title: `[ACQUIRER] Launches $[PRICE]B Hostile Bid for [TARGET]`
- Body: `[ACQUIRER] has launched an unsolicited tender offer to acquire [TARGET] at $[SHARE_PRICE] per share, a [PREMIUM]% premium to yesterday's close. [TARGET]'s board is expected to meet to evaluate the offer.`
- Effekt: Target +15-30%, Acquirer -3-8%, Spieler-Entscheidungspopup

**30. Class Action Filed (Company, Moderate, -0.4)**
- Title: `Class Action Lawsuit Filed Against [COMPANY]`
- Body: `Shareholders have filed a class action lawsuit against [COMPANY] alleging [REASON]. The suit seeks damages of $[AMOUNT]M and names [CEO] and other executives as defendants.`
- Effekt: Aktie -5-12%, Vola 2×, Unsicherheit für 30-90 Tage

**31. Chapter 11 Bankruptcy (Company, Catastrophic, -0.9)**
- Title: `[COMPANY] Files for Chapter 11 Bankruptcy Protection`
- Body: `[COMPANY] has filed for Chapter 11 bankruptcy, citing $[DEBT]B in outstanding debt and declining revenues. The company will attempt to restructure under court supervision while maintaining operations.`
- Effekt: Aktie -50-80%, Handel geht weiter, Follow-ups über 60-180 Tage

**32. Regulatory Approval (Company, Major, +0.5)**
- Title: `[AGENCY] Grants Approval for [COMPANY]'s [PRODUCT/DEAL]`
- Body: `The [AGENCY] has approved [COMPANY]'s application for [DETAIL], removing a key uncertainty that had weighed on the stock. Analysts are raising price targets.`
- Effekt: Aktie +8-20%, Vola sinkt, positiver Drift für 5 Tage

**33. SMA Investigation of Company (Company, Major, -0.5)**
- Title: `SMA Opens Investigation into [COMPANY] Trading Practices`
- Body: `The StockSim Market Authority has launched an investigation into potential securities violations at [COMPANY], including allegations of [VIOLATION]. The company's shares have been placed on a regulatory watch list.`
- Effekt: Aktie -10-20%, Vola 2.5×, 10 Tage

### 22.2 Beispiel-Firmennamen und -Symbole (pro Sektor 10 Beispiele)

**Technology:**
| Name | Symbol |
|---|---|
| Vertex Dynamics | VTXD |
| NovaSoft Inc. | NVST |
| Quantum Logic Systems | QLGS |
| CyberNexus Technologies | CNXT |
| Apex Cloud Solutions | ACLS |
| SynthWave AI | SWAI |
| PixelForge Labs | PXFL |
| DataHelix Networks | DHLN |
| NeuralPath Systems | NRPS |
| CloudPeak Software | CPKS |

**Energy:**
| Name | Symbol |
|---|---|
| PetroVolt Energy | PTVE |
| SolarWind Corp | SLWC |
| TerraPower Resources | TPWR |
| HydroGeo Industries | HGEO |
| VoltStream Energy | VSTE |
| GreenFuel Dynamics | GFDY |
| AtomCore Power | ATCP |
| WindRidge Renewables | WRDG |
| GeoThermal Solutions | GTHS |
| IonFlux Energy | IFLX |

**Financials:**
| Name | Symbol |
|---|---|
| FirstTrust Holdings | FTHL |
| Sterling Capital Group | STCG |
| Pacific Securities | PCSC |
| CrownBridge Financial | CBFN |
| GlobalPremier Bank | GPBK |
| Atlas Trust Corp | ATSC |
| Meridian Advisors | MRAD |
| Sovereign Wealth Holdings | SWHL |
| Pinnacle Financial | PNFN |
| Vanguard Capital Partners | VGCP |

**Healthcare:**
| Name | Symbol |
|---|---|
| NovaPharma Inc. | NVPH |
| BioGenix Labs | BGXL |
| MedCore Therapeutics | MDCT |
| HelixCure Sciences | HXCS |
| VitaPath Diagnostics | VTPD |
| CellForge Biotech | CLFB |
| PulsePoint Medical | PPMD |
| GenomicVista Health | GVSH |
| NeuraStar Pharma | NRSP |
| ImmunoCore Labs | IMCL |

**Consumer Goods:**
| Name | Symbol |
|---|---|
| BrightLeaf Brands | BLFB |
| PrimePantry Corp | PPCY |
| FreshHarvest Foods | FHFD |
| UrbanTaste Inc. | UBTI |
| EverGlow Products | EGLP |
| HomeNest Essentials | HNES |
| PureCraft Consumer | PCCN |
| DailyBlend Co. | DBCO |
| GoldenShelf Brands | GSBD |
| NaturePath Goods | NTPG |

**(Weitere Sektoren — Industrials, Materials, Real Estate, Telecom, Utilities, Luxury, Transport — folgen demselben Schema. Die Generierung verwendet die Prefix/Suffix-Listen aus Kapitel 11.3.1.)**

### 22.3 Farbpalette Quick Reference

```
Hintergründe:     #0A0E17  #111827  #1F2937  #161E2E  #080C14
Grün:             #10B981  #065F46  rgba(16,185,129,0.3)
Rot:              #EF4444  #7F1D1D  rgba(239,68,68,0.3)
Text:             #F9FAFB  #9CA3AF  #4B5563  #60A5FA
System:           #F59E0B  #3B82F6
Border:           #1F2937  #374151  #3B82F6
Chart-Indikatoren: #F59E0B  #8B5CF6  #EC4899  #06B6D4  #60A5FA
Sektoren:         #3B82F6  #F59E0B  #8B5CF6  #EC4899  #10B981
                  #6B7280  #F97316  #14B8A6  #6366F1  #84CC16
                  #D946EF  #0EA5E9
```

### 22.4 Keyboard Shortcuts Quick Reference

```
ZEITSTEUERUNG          NAVIGATION             TRADING
Space   Pause/Play     D  Dashboard           B  Buy
1       Speed 1x       P  Portfolio            S  Sell
2       Speed 2x       M  Market              H  Short
3       Speed 5x       O  Orders              Enter  Confirm
4       Speed 10x      N  News
+/→     Schneller      A  Analytics            SYSTEM
-/←     Langsamer      Esc  Zurück/Close       Ctrl+S  Save
                                               Ctrl+L  Load
CHART                  Ctrl+N  New Game
Scroll  Zoom           Ctrl+F  Search
Drag    Pan            Ctrl+J  Journal
DblClk  Reset          Ctrl+K  Price Alert
                       F1      Help
                       F11     Fullscreen
                       Ctrl+Q  Quit
```

### 22.5 WebSocket Message Reference

Siehe Kapitel 21.3 für die vollständige Tabelle aller Messages mit Payloads.

### 22.6 Glossar der Börsen-Fachbegriffe (für Entwickler)

| Begriff | Erklärung |
|---|---|
| Ask | Niedrigster Preis, zu dem jemand verkaufen will |
| Bid | Höchster Preis, zu dem jemand kaufen will |
| Candlestick | Chart-Darstellung mit Open/High/Low/Close pro Zeiteinheit |
| Circuit Breaker | Automatische Handelsaussetzung bei extremen Preisbewegungen |
| Cover | Schließen einer Short-Position durch Rückkauf |
| EPS | Earnings Per Share — Gewinn pro Aktie |
| Flash Crash | Extrem schneller, vorübergehender Kurseinbruch |
| FOMO | Fear Of Missing Out — Angst, eine Chance zu verpassen |
| Gap | Preissprung zwischen Schlusskurs und nächstem Eröffnungskurs |
| GTC | Good Till Cancelled — Order bleibt aktiv bis gecancelt |
| Liquidation | Erzwungener Verkauf/Cover einer Position |
| Margin | Geliehenes Kapital für gehebelte Trades |
| Margin Call | Aufforderung, Sicherheiten nachzuschießen |
| Market Cap | Marktkapitalisierung = Preis × Anzahl Aktien |
| Mean Reversion | Tendenz von Preisen, zum Mittelwert zurückzukehren |
| OHLCV | Open, High, Low, Close, Volume — Standardformat für Kursdaten |
| P/E Ratio | Price/Earnings — Preis im Verhältnis zum Gewinn |
| P&L | Profit and Loss — Gewinn und Verlust |
| Short Interest | Anteil der leerverkauften Aktien an der Gesamtmenge |
| Short Squeeze | Erzwungene Eindeckung von Short-Positionen treibt Preis nach oben |
| Slippage | Differenz zwischen erwartetem und tatsächlichem Ausführungspreis |
| Spread | Differenz zwischen Bid und Ask |
| VIX | Volatility Index — Maß für erwartete Marktschwankungen |

---

*Ende der Game Design Spec — Version 1.0*
