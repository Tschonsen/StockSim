# StockSim → World Sim — Vision & Architektur

> **Nordstern für den Pivot von „Börsen-Spiel" zu „lebende Welt, deren Anzeige die Börse ist".**
> Dieses Dokument ist die Single Source of Truth für die Richtung. Bei jeder Session, die am
> World-Sim-Umbau arbeitet, **zuerst hier + `CURRENT_STATE.md` lesen.**
> Angelegt: 2026-07-02. Status: **Vision fixiert, Umsetzung startet bei M0.**

---

## 1. Der Nordstern (in einem Absatz)

StockSim wird kein Börsen-Spiel mehr sein, sondern eine **simulierte Welt** — Länder, Ressourcen,
Sektoren, Firmen und Menschen mit fortlaufender Geschichte —, bei der der Aktienmarkt nur die
**Anzeige-Oberfläche** ist, durch die der Spieler diese Welt liest und in sie eingreift. „Bloomberg-Terminal
auf einem lebenden Planeten." Tausende Firmen mit echter, sich entwickelnder Historie; News, die kausal
durch die Welt propagieren und Kurse **verursachen** statt sie zu dekorieren. Das Ziel ist bewusst riesig
und über Jahre gedacht — deshalb wird es in **vertikalen Scheiben** gebaut, nie am Stück.

**Zweck:** ein **realistischer, risikofreier Trading-Sim** (Paper-Trading, kein Echtgeld, E7) in einer
**fiktiven Welt, die die reale so gut wie möglich nachbildet** (E8). Vorsimuliert **ab 1980**, spielbar
**ab dem 1. Januar 2026** (E9).

> ### ⚠️ ÜBERLEBENSREGEL (die #1-Projektgefahr)
> Der Scope ist riesig und es wert — aber der wahrscheinlichste Tod ist nicht ein technisches Problem,
> sondern **40 halbfertige Systeme und nie etwas Spielbares.** Zwei Disziplinen dagegen, beide nicht verhandelbar:
> 1. **Nie Breite bauen, bevor das Rückgrat an EINER vertikalen Scheibe bewiesen ist.** Jede Scheibe muss sich
>    *lebendig anfühlen*, bevor die nächste beginnt.
> 2. **Immer den Überblick behalten** — dieses Dokument + `CURRENT_STATE.md` aktuell halten; kein System
>    anfangen, das nicht in den Meilenstein-Pfad §10 passt. Verlorener Überblick = Scope-Tod.

---

## 1a. LEITPRINZIP (über allem) — Real-Sim, nicht Arcade

**Das Spiel ist eine Realsimulation, kein Arcade-Spiel. Jeder Einfluss hat realistischen Impact —
in Richtung UND Größenordnung.** Kurse, Event-Wirkungen, Trader-Flow, Makro-Effekte bewegen sich in
real-plausiblen Bandbreiten; keine gamey Übertreibung, keine aufgeblähten Zahlen für den „Wow"-Effekt.
Realismus schlägt Spektakel. Im Zweifel: an realen Marktproportionen kalibrieren.
Die **Welt** ist fiktiv (E8), ihr **Verhalten** aber an realen Daten kalibriert — reale Marktdynamik ist der
Maßstab (z.B. das vorhandene, auf 151 realen Tickern trainierte ML-Modell als Kalibrier-Referenz). „Realistisch"
darf nie Bauchgefühl sein, sondern gegen eine Referenz prüfbar.

**Konsequenz für den Umbau (Kalibrierung):** die bestehenden arcade-igen Tunings werden auf realistische
Magnituden zurückgeholt — u.a. der 3× erhöhte Base-Drift (~7,5 %/Jahr), Meme-Squeezes (+30–100 %),
symmetrische ±5 %-Bull/Bear-Phasen. Alle künftigen Effekte (emergentes Pricing §S5, KI-Trader-Flow §Z1,
Event-Impacts §S4) werden gegen dieses Prinzip geprüft.

---

## 2. Grundsatzentscheidungen (FIXIERT — nicht ohne expliziten Beschluss ändern)

| # | Entscheidung | Beschluss |
|---|---|---|
| E1 | **Emergentes Pricing (Weg B).** Der Kurs ist ein *Output* der Welt-Sim: Welt-Event → Firma → Fundamentaldaten → Kurs. GBM bleibt nur noch als Mikro-Rauschen auf dem emergenten Signal. | 2026-07-02 |
| E2 | **Evolution im StockSim-Repo**, kein neues Projekt. v0.3.0 wird als Git-Tag eingefroren, gebaut wird auf Branch `worldsim`. Markt-Layer + Content werden weiterverwendet, der GBM-Preiskern wird ersetzt. | 2026-07-02 |
| E3 | **Stack bleibt:** C# .NET (Backend) + Electron/React/TS (Frontend). Kein Sprachwechsel — der Markt-Layer steht in C#. | 2026-07-02 |
| E4 | **Custom Charts + eigenes Design** (eigene Canvas-2D-Engine) statt Chart-Bibliothek. WebGL nur als Notausgang bei Massendaten. Grund: volle Kontrolle, eigenes Design-Sprachbild, umgeht den React-Re-Render-Flaschenhals. **Die zuletzt eingebaute Lib (TradingView Lightweight) wird abgelöst — sie machte nur Probleme.** Kein weiterer Invest in die Lib. | 2026-07-02 |
| E5 | **Agent-spielbar & selbst-testbar by design.** Die Welt-Sim bekommt eine vollständige Maschinen-Schnittstelle (MCP / Agent-Bridge), über die ein KI-Agent (Claude) das Spiel *spielen und automatisiert playtesten* kann — nicht als Nachrüstung, sondern als Architektur-Prinzip. Baut auf dem `mcp-bridge`-Prototyp auf. Doppelnutzen: automatisierter Playtest-/Balance-Harness. | 2026-07-02 |
| E6 | **Nur EIN Spielmodus — der Hauptmodus** (eine persistente, lebende Welt, in die man einsteigt). **Szenarien werden komplett entfernt**, inkl. History Mode (geskriptete Arcs wie „Black Monday 1987" via `ForceActivateArc`). Widerspricht dem emergenten Real-Sim: Krisen *entstehen*, sie werden nicht kuratiert/geskriptet. | 2026-07-02 |
| E7 | **Ziel = realistischer, risikofreier Trading-Sim** (Paper-Trading, **kein Echtgeld**). Der Realismus selbst ist der Reiz — kein Score-/Arcade-Ziel, keine Win/Lose-Bedingung außer den realen Konsequenzen des Handelns (Bankrott etc.). Ein risikofreier Sandkasten, um echtes Trading zu erleben. | 2026-07-02 |
| E8 | **Fiktive Welt, die die reale so gut wie möglich simuliert.** Keine echten Firmen/Länder/Personen (Lizenzkosten + rechtliches Risiko) — fiktive Analoga, die sich real-plausibel verhalten. **Reales Marktverhalten ist die Kalibrier-Referenz** für das Leitprinzip §1a. | 2026-07-02 |
| E9 | **Zeit-Rahmen:** Vorsimulation läuft **ab 1980**, Spielbeginn **1. Januar 2026**. → 46 Jahre emergente Vorgeschichte bei Spielstart (siehe §6 + History-LOD). | 2026-07-02 |

---

## 3. Was schon existiert — NICHT neu bauen (Brückenkopf)

Der Pivot startet nicht bei null. Verwendbar / bereits Richtung Vision:

- **Emergenz-Brückenkopf (Session 37):** `FundamentalDynamics` (pure) + `EventEngine.ApplyFundamentalImpact()`
  bilden schon die Kette *Event → bleibende Fundamental-Änderung → Growth-Trajektorie → Drift → Kurs revertet
  nicht mehr voll*. **Das ist der Keim des emergenten Pricings.** Darauf wird aufgebaut, nicht daneben.
- **Content-Reichtum:** 500+ Firmen mit CEO/Archetyp/Gründungsstory/Rivalen, 14 Sektoren, 572 Event-Templates,
  226 Analysten, Supply-Chain-Beziehungen, Wirtschaftszyklus, Monetary Policy, 64 Gründungsgeschichten.
- **Markt-Layer (behalten & andocken):** OrderEngine, OptionsEngine (Black-Scholes), ETFEngine, DividendEngine,
  IndicatorCalculator, SMAEngine, Save/Load. Diese kriegen künftig ihre Preise aus der Welt statt aus GBM.
- **Menschen-Keim:** CEO-Archetypen, CEO-Firing, Persona-Evolution, benannte Entitäten (`EntityRegistry`).
- **Agent-Schnittstelle (E5):** `mcp-bridge/`-Prototyp exponiert den WS-Vertrag als MCP-Tools/Resources — ein
  Agent spielt StockSim nativ. Durch den Umbau **am Leben halten** (mit dem WS-Vertrag mitziehen), Ziel
  generisches `AgentBridge`-SDK. Basis für automatisierten Playtest/Balance-Harness.

**Cleanup-Kandidaten (sterben mit dem GBM-Kern):** `HistoryGenerator` (GBM-Fake-Historie → ersetzt durch
Vorsimulation §6), der GBM-Zufallskern in `PriceEngine.Tick` (→ emergente Preisfunktion).

---

## 4. Architektur-Rückgrat (die 5 Säulen — zuerst, alles hängt daran)

| Säule | Was | Warum |
|---|---|---|
| **S1 — Entity-Store (DOD)** | Flache Struct-Arrays / Entity-Component statt Objekt-Graph für alle Welt-Entities. | Zehntausende Entities cache-freundlich, multicore- & parallel-fähig. Macht CPU *und* GPU-Pfad erst möglich. |
| **S2 — Multi-Rate-Zeit** | Getrennte Takte: **Makro-Tick** (Länder, Karrieren, Sektoren — Tage/Jahre) + **Markt-Tick** (Kurse — Minuten). | Man simuliert nicht alles im selben Takt. |
| **S3 — LOD-Tiering** | Aktiv angeschaute Entities = volle Tiefe pro Tick; Hintergrund = grob/statistisch/seltener, lazy hochgeschaltet bei Fokus. | **Der** Skalierungshebel. Entkoppelt Sim-Kosten von der Entity-Anzahl (wie Flow-Fields die Kosten von der Agenten-Anzahl entkoppeln). |
| **S4 — Kausal-Event-Graph** | Events sind Knoten, die durch Welt→Land→Sektor→Firma→Kurs propagieren. Trennung: Event *simulieren* (Graph/CPU) vs. *erzählen* (ML). | So „wirken News wirklich", statt es nur zu behaupten. Baut auf Supply-Chain-Propagation aus. |
| **S5 — Emergent-Pricing-Engine** | Kurs = Funktion(Fundamentaldaten, Event-Impulse, Angebot/Nachfrage) **plus stochastische Textur-Schicht** (Fat Tails, Vol-Cluster). Ersetzt den GBM-*Kern*, behält aber Stochastik als Textur. | Kern von E1. ⚠️ Die Stochastik ist **nicht Alt-Ballast**, sondern trägt den Mikro-Realismus (§1a) — bottom-up allein sehen Kurse „falsch" aus (zu glatt/zu sprunghaft). Emergent = Richtung; Stochastik = real-plausible Textur. Eine der härtesten Ecken des Projekts. |

**Multicore-Notiz:** Die Sim läuft heute komplett single-threaded (kein `Parallel.ForEach`). Bei tausenden
Entities ist Parallelisierung **Pflicht, nicht Kür** — S1 (DOD) ist die Voraussetzung dafür. Blocker heute:
geteilte `Random`-Instanzen (per-Thread-RNG nötig) + statischer `GameEvent._nextId` (nicht thread-safe).

---

## 5. Die Welt-Layer (das große Neue, von grob nach fein)

- **Länder** — BIP, Zinsen, Politik, Ressourcen-Endowment, Stabilität. Treiben Sektoren.
- **Ressourcen** — Öl, Metalle, Energie, Nahrung. Angebot/Nachfrage → Preise → Firmenkosten.
- **Sektoren** — Bindeglied Land↔Firma; geteilte Schocks/Korrelation (Grundzüge existieren).
- **Firmen** — Fundamentaldaten (Umsatz, Marge, Schulden) die aus der Welt *entstehen*, plus tiefe, fortlaufende Historie.
- **Menschen/Agenten** — CEOs, Politiker, Analysten mit Karriere, Ruf, Alter, Skandalen; Geschichte läuft weiter.
- **KI-Trader als sichtbare Marktteilnehmer** — siehe Design-Ziel Z1.
- **Indikatoren-Layer** — „alle möglichen Indikatoren" als eigener, stateless, parallelisierbarer Rechen-Layer.

### Design-Ziel Z1 — KI-Trader relevant & sichtbar machen

**Problem heute:** Die 14 KI-Trader-Typen haben <1 % Preiseffekt/Tick und das Frontend zeigt **nichts** von
ihnen (siehe `PROJECT_STATUS.md` §2.4 „unsichtbare Features"). Sie sind mechanisch irrelevant und für den
Spieler unsichtbar.

**Ziel:** KI-Trader werden zu einer **spürbaren, lesbaren Kraft** für den Spieler:
- **Mechanisch relevant:** ihr Flow bewegt den Kurs merklich (nicht <1 %), im emergenten Modell über
  Angebot/Nachfrage statt kosmetischem Nudge. Reaktion auf Welt-Events → der Spieler handelt *gegen echte Gegner*.
- **Benannt & verfolgbar:** wenigstens ein Teil als benannte Entitäten (wie `EntityRegistry` für News),
  mit erkennbarem Stil, Positionen, Ruf, Geschichte — „Fonds X lädt seit Tagen Sektor Y auf".
- **Im UI sichtbar:** Aktivitäts-/Flow-Anzeige, wer kauft/verkauft, Sentiment-Shift, große Player-Moves.
- Gehört in den Menschen/Agenten-Layer; wird ab der Verbreiterung (M2+) ausgebaut.

---

## 6. Vorsimulation = die Welt-Engine headless (räumt `HistoryGenerator` weg)

Sobald das Pricing emergent ist, wird **keine GBM-Fake-Historie mehr generiert.** Stattdessen läuft die
**echte Welt-Engine im Schnelldurchlauf, bevor der Spieler einsteigt** (headless, auf Makro-LOD, schnell).
Ergebnis: die Historie ist *echt* — der CEO, der vor 3 Jahren gefeuert wurde, wurde in der Sim wirklich
gefeuert; der Chart zeigt reale Events. **Ein Mechanismus, zwei Gewinne:** ersetzt `HistoryGenerator`
(Cleanup) und macht die Welt tief.

**Konkreter Zeit-Rahmen (E9):** Vorsim **ab 1980**, Spielbeginn **1. Januar 2026** → **46 Jahre** emergente
Vorgeschichte bei Spielstart.

⚠️ **History-LOD (Pflicht, sonst platzt der Save):** 46 Jahre × tausende Entities × fortlaufende Historie
wächst ins Unendliche. LOD gilt **auch für die Vergangenheit**: je weiter zurück, desto gröber die Auflösung
(nah: Minuten/Tage → fern: Wochen/Jahre, nur Schlüssel-Events + Aggregate). „History fortschreiben" ist leicht;
„History verdichten" ist das eigentliche Problem. Vor M5 lösen.

---

## 7. Content-Generierung (hier — und nur hier — landet ML/GPU)

- **Firmen-Backstories, Menschen-Bios, dynamische News-Texte** via generativem Modell. Läuft zur
  **Generierungszeit** (Welt-Erstellung oder lazy bei Erstfokus), **nicht** im heißen Tick — kein Roundtrip-,
  kein Determinismus-Problem. GPU sinnvoll. Baut auf dem vorhandenen ONNX-Weg auf.
- **GPU für die Kern-Sim ist bewusst raus:** kleiner heißer Batch (dank LOD), `decimal`-Preise (GPU kann kein
  `decimal`), Tick-Roundtrip. Der echte Sim-Speedup kommt aus DOD + Multicore, nicht aus der GPU.
- **In-Game-ML ist ein erlaubtes Produkt-Feature:** das ONNX-Preismodell + künftige Content-Generierungs-Modelle
  dürfen ausgebaut / neue hinzugefügt werden, **wenn sie das Spiel verbessern**. (Nicht zu verwechseln mit
  Dev-Tooling: Brain-Files werden *nicht* an ein schwaches lokales Modell ausgelagert — siehe Projekt-`CLAUDE.md`.)

---

## 8. UI-Neubau

- **Eigene Chart-Engine (Canvas 2D):** Candlesticks, Volumen, Indikator-Overlays, Pan/Zoom, Echtzeit-Push.
- **Terminal-Layout:** Watchlist, Detail, News-Feed, plus **neue Welt-/Länder-/Sektor-Ansichten**.
- **Ernten aus der bestehenden UI:** Layout, Komponenten-Muster, WebSocket-Anbindung, Zustand-Store.
### UI-Design-Richtung

- **Terminal-*Gefühl*, aber eigenes Sprachbild.** Dichte, informations-getriebene Terminal-Anmutung — aber
  **eigene Farben, Formen, Schrift**, nicht der Look von Bloomberg oder einer Lib. Eigenständig, wiedererkennbar.
  (Basis: `design/UI_DESIGN_GUIDE.md`; Dark-Palette-Präferenz: neutrale Grautöne + gezielte Akzente, nicht
  durchgehend blau.)
- **Terminal UND Spiel zugleich.** Die Fläche muss zwei Naturen vereinen: das *Trading-Terminal* (Charts,
  Watchlist, News, Orderbuch, Welt-Ansichten) **und** die *Spiel-Ebene* — **Datum, Zeit-/Speed-Steuerung,
  Cash/Vermögen** brauchen dedizierten, immer sichtbaren Platz (Game-HUD). Nicht wegdesignen zugunsten reiner
  Terminal-Optik; beide Ebenen müssen atmen.
- **Customizing für den Spieler (Umfang TBD).** Der Spieler soll die Oberfläche nach eigenem Bedarf anpassen
  können. *Was genau, ist noch offen* — Kandidaten: verschiebbare/andockbare Panels (die bestehende
  `DetachablePanelManager`-Infrastruktur passt dazu), konfigurierbare Watchlists/Layouts, wählbare Kennzahlen,
  evtl. Theme-Akzente. → siehe offener Punkt O6.
- **Multi-Monitor-Support (Ziel).** Abgedockte Panels laufen als eigene Fenster auf beliebigen Monitoren —
  echter Trading-Terminal-Charakter. Basis: `DetachablePanelManager` (Electron IPC, existiert). **Architektur-
  Zwang von Tag eins:** (a) die Custom-Chart-Engine muss **fenster-agnostisch** rendern (eigenes Canvas +
  Render-Loop pro Fenster), (b) der Zustand-Store muss **cross-window synchronisieren** (IPC), damit jedes
  Fenster seinen eigenen Live-Feed hat. Volle Umsetzung darf später kommen, aber die Chart-/Layout-/State-
  Architektur muss es ab M1 *zulassen*.
- **Lib-Ablösung beschlossen (E4):** Sowohl ECharts als auch die zuletzt eingebaute TradingView Lightweight
  Charts fliegen raus — LW machte nur Probleme. Die Custom-Canvas-Engine ist das Ziel, kein weiterer Invest
  in die Lib. Bis die eigene Engine steht, wird LW nicht ausgebaut, nur geduldet.

---

## 9. Cleanup-Leitlinie (beim „aktuellen Code aufräumen")

Nicht alles polieren — nach **„trägt in die neue Welt" vs. „stirbt mit dem GBM-Kern"** sortieren:

- **Polieren / behalten:** Markt-Layer (Orders, Optionen, ETFs, Dividenden), Indikatoren, Save/Load, Content,
  Frontend-Anbindung, der Emergenz-Brückenkopf (`FundamentalDynamics`).
- **Ersetzen / entfernen (wenn emergent sie ablöst):** GBM-Preiskern in `PriceEngine.Tick`, `HistoryGenerator`.
- **Entfernen (E6 — nur ein Modus):** Szenarien-System inkl. History Mode — Szenario-Picker in `NewGameScreen`,
  Szenario-Definitionen, `ForceActivateArc`-Pfad. Neuer Einstieg: direkt in die persistente Welt (nach Vorsim §6).
- **Regel:** Code, der ersetzt wird, wird **nicht** vorher aufwändig poliert (Wegwerf-Arbeit vermeiden).

---

## 10. Meilenstein-Pfad (vertikale Scheiben)

Reihenfolge: kleinste Schritte mit sofort sichtbarem Effekt zuerst. Jede Scheibe muss sich *lebendig anfühlen*,
bevor die nächste beginnt.

- **M0 — Proof of Life (erste vertikale Scheibe). ▶ Engine-Ebene bewiesen (2026-07-02).** Umgesetzte Scheibe:
  **Ölschock → Airlines.** Öl bewegt Transportation-Kurse über die Fundamentaldaten (Öl → Treibstoffkosten →
  Marge → Earnings → FairValue → Kurs), firmenindividuell nach `Stock.FuelCostExposure` — Weg B statt dekoriertem
  Sektor-Nudge. Neu: `FundamentalDynamics.FuelCostMarginImpact` (pure, kalibriert), Öl-Kopplung in
  `GameLoop.DriftFundamentals`, subsektor-feine Exposure bei der Generierung (lebt im Spiel), `PriceEngine.DeterministicMode`
  („pure emergent"). Headless, TDD (610/610 grün, +9). Details: `CURRENT_STATE.md`.
  → **Erfolgskriterium erfüllt:** injizierter Ölschock bewegt Earnings betroffener Firmen deterministisch &
  proportional zur Exposure; mit Rausch AUS fällt der Kurs fundamental begründet (kein GBM nötig).
  ⏭ Offen: alten dekorierten `sectorMult`-Öl-Nudge später entfernen; Balance-Playtest über Agent-Bridge (E5).
- **M1 — Custom-Chart-Engine + Terminal-UI** für die M0-Scheibe.
- **M2 — In die Breite (generalisiert, NICHT sektorweise von Hand):** M0s spezifische Öl→Marge-Kopplung wird zum
  **generellen data-driven Exposure-Modell** (Treiber × 4 Kanäle: InputCost/Demand/OutputPrice/Valuation) —
  neuer Sektor = ein Datensatz, kein Code. Vollständige Design-Landkarte inkl. Kopplungs-Matrix + Abdeckungs-/
  Lücken-Liste: **`design/EMERGENT_COUPLING.md`**. Dazu mehr Firmen/Sektoren; LOD (S3) einführen und schärfen.
- **M3 — Länder & Ressourcen** als echte Treiber (Makro-Tick, S2).
- **M4 — Menschen-Layer** mit fortlaufender Geschichte (Karrieren, Politiker).
- **M5 — Vorsimulation** (§6): Welt-Engine headless N Jahre → echte Historie; `HistoryGenerator` raus.
- **M6+ — Skalierung** auf tausende Entities: DOD (S1) + Multicore ausreizen; Content-Generierung (§7) via ML.

*(Reihenfolge M1–M6 ist bewusst diskutierbar; M0 ist der fixe Startpunkt.)*

---

## 11. Offene Entscheidungen (vor der jeweiligen Scheibe klären)

- **O1 — Determinismus-Modell:** Vorsimulation + reproduzierbare Welten brauchen deterministische RNG. Bei
  späterer Parallelisierung: per-Thread counter-based RNG (Philox-artig). Fixieren spätestens vor M5/M6.
- **O2 — DOD-Migrationstiefe:** Wie weit wird der bestehende OOP-Objekt-Graph (Stock/CompanyPersonality) auf
  Struct-Arrays umgestellt — hart ab M2, oder schrittweise? Vor M2 entscheiden.
- ~~O3 — Chart-Umstieg~~ **ENTSCHIEDEN (2026-07-02):** Custom-Canvas + eigenes Design, Lib (LW) wird abgelöst (machte nur Probleme). Kein weiterer Lib-Invest.
- **O4 — Welche „ähnliche" UI ernten:** die aktuelle StockSim-UI (Annahme) oder ein anderes Projekt? Vom User bestätigen.
- **O5 — Orderbuch-getriebenes Pricing (Architektur-Chance):** Der real-sim-treueste Weg wäre ein echtes
  **Matching-Engine**, gefüttert von KI-Trader-Orders + Spieler-Orders + fundamental-getriebenem Agenten-Verhalten
  → Preis *ist* dann Angebot∩Nachfrage, nicht eine Formel. Vereint S5 + Z1 (Trader-Flow *ist* der Preis → KI-Trader
  automatisch relevant) **und** macht späteren Multiplayer natürlich (siehe `MULTIPLAYER_VISION.md`). Heute ist
  `OrderbookGenerator` nur UI-Fake. Großer Schritt — vor M2/M3 bewerten: echtes Matching vs. Formel-Pricing mit
  Trader-Flow-Term.
- **O6 — Umfang des UI-Customizings:** Spieler soll die Oberfläche anpassen können (§8) — *was genau* ist offen.
  Kandidaten: andockbare/verschiebbare Panels (`DetachablePanelManager` existiert), Layout-Presets, wählbare
  Kennzahlen/Spalten, Theme-Akzente. Vor M1 grob abstecken (welches Customizing die Chart-/Layout-Architektur
  von Anfang an ermöglichen muss).

---

## 12. Git / Branch-Plan (E2)

> ⚠️ **BLOCKIERT (Stand 2026-07-02):** Committen geht aktuell nicht. Der ganze Git-Plan (Commit → Tag → Branch)
> ist zurückgestellt, bis Git wieder läuft. Solange auf dem aktuellen Working-Tree weiterarbeiten; vorsichtig,
> da uncommitted. Sobald Git geht, in dieser Reihenfolge:

1. ⚠️ Zuerst: der große uncommittete Session-37-Block (siehe `CURRENT_STATE.md` Handoff) **committen**.
2. **v0.3.0 als Tag einfrieren** (`git tag v0.3.0`) — das released Spiel ist damit dauerhaft erreichbar.
3. **Branch `worldsim`** vom aktuellen Stand — hier läuft der Umbau.

---

## 13. Test-Strategie (TDD angepasst an emergente Systeme)

Klassisches `assert x == 5` bricht bei stochastischen/emergenten Effekten. Drei Ebenen statt einer:

1. **Deterministische Unit-Tests** — für pure Logik (Fundamental-Formeln, Event-Propagation-Regeln,
   Health-Scores). Bleibt TDD-Default wo möglich (fixe Seeds, `CurrentYear=0`-Pfade etc.).
2. **Statistische / property-based Tests** — für emergente Effekte: Liegt der Return-Median in realistischer
   Bandbreite? Zeigt die Verteilung Fat Tails? Bleiben Korrelationen im plausiblen Bereich? Über mehrere Seeds
   gemittelt. Basis existiert: `CompanyEventBiasDiagnostic` (Multi-Seed-Diagnose).
3. **Agent-Playtest-Harness (E5)** — Claude spielt via `mcp-bridge` automatisiert, prüft Balance/Realismus über
   lange Läufe. Wird zum **Haupt-Balance-Wächter**, nicht zur Ausnahme.

Kalibrierung gegen reale Referenz (E8/§1a) ist Teil von Ebene 2.
