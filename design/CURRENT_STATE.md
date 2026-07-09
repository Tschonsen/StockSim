# StockSim — Aktueller Zustand

> Kurz und knapp. Session-History siehe `design/SESSION_HISTORY.md`.

---

## ✅ STAND 2026-07-10 — M3 Slices 1-3: emergente Rohstoffe (Öl, Gas, Gold)

**Der Welt-Layer treibt jetzt alle drei Kern-Rohstoffe: Preise *entstehen* aus Angebot/Nachfrage statt Random Walk.**
Verallgemeinert auf `CommodityMarket` (Producer + Demand + Clearing-Preis + Event-Schock, pro-Markt-Elastizität),
Factories `CreateOil/Gas/GoldWorld`. Flag `EmergentCommodityPricing` (Default ON).

- **Öl:** Nachfrage prozyklisch (GDP+PMI), EIA-Event = transienter Demand-Schock. Schließt den Rohstoff-Zyklus
  (Boom→Öl→Inflation→Zins→kühlt) über die bestehende Kopplung.
- **Gas:** Nachfrage industriell (PMI-gewichtet) + Öl-Substitution (in Demand gefaltet, alter Ad-hoc-Nudge weg).
- **Gold:** *anderes* Modell — Safe-Haven/monetär: Nachfrage aus Fear (Konfidenz↓) + negativen Realzinsen
  (Inflation↑/Zins↓), NICHT Aktivität; statische Minen-Supply + hohe Elastizität. Beweist die Generalisierung.
- **Verifiziert:** je Rohstoff 1-Jahres-Multi-Seed-Playtest (real. Band, kein Clamp-Pinning) + direktionale Tests
  (Boom/Rezession, Safe-Haven). **Suite 650 grün, 0 Regression.** Commits `65dea8a`→`2c67d2a`, gepusht.
- **✅ Länder-Layer live (M3 Länder Slice 1):** `Country` (Stabilität [0-1] → `SupplyFactor` → Producer-Output,
  heilt zur Baseline) + `WorldState` (Länder besitzen alle 12 Producer über die Märkte; ein Petrostaat besitzt Öl
  UND Gas). `EconomicEngine.TickWorld()` jeden Tag: seltener **Geopolitik-Schock** (`CountryInstabilityChance` ~1/Jahr)
  destabilisiert ein Land → Förderung fällt → Rohstoff spikt über die Märkte → propagiert. **Krieg/Embargo = endogener
  Supply-Schock**, nicht injiziert. Verifiziert: Suite **658 grün**, direktionale Tests (Öl-Land destabilisiert → Öl
  spikt; Petrostaat → Öl+Gas). Commits `9af0e77` + `1c9afa2`, gepusht.
- **⏳ Verbleibt:** GUI-Feel-Kalibrierung (nur dein Auge). ⚠️ Die laufende App wurde VOR dem Länder-Layer gebaut →
  Neustart nötig, um Geopolitik-Schocks live zu sehen (Rohstoffe-emergent ist drin).
- **Nächste M3-Scheiben:** Länder treiben **BIP + Sektoren direkt** (nicht nur Supply) — der größere Schritt.
  Oder Breite: weitere Rohstoffe (Silber/Kupfer, schnell).

Historie (Öl-Detail, superseded von der Generalisierung oben): Slice 1 baute `OilMarket`, jetzt `CommodityMarket`.

- **Modell (`Models/OilMarket.cs`):** `OilProducer` (Land/Region: Baseline × Modifier) + `OilMarket`
  (Producers + globale Demand). Fundamentalpreis = `basePrice × (demand/supply)^elastizität` (inelastisch ~6 →
  kleine Ungleichgewichte bewegen viel, real, §1a). `CreateDefaultWorld()`: 4 fiktive Regionen, balanciert = $75.
- **Integration (`EconomicEngine`):** Öl-Nachfrage koppelt an die **Konjunktur** (prozyklisch, GDP+PMI-Deviation),
  Preis mean-revertet zum Fundamental + Mikro-Rauschen. Schließt einen echten Rohstoff-Zyklus über die bestehende
  azyklische Kopplung (Boom→Öl→Inflation→Zins→kühlt Wachstum). Producer-`ProductionModifier` = Supply-Schock-Hook.
- **`EmergentOilPricing` = Default ON (live).** Die einzige Öl-Schock-Injektion (wöchentliches EIA-Crude-Inventory-Event)
  ist migriert: setzt `OilPrice` nicht mehr direkt (würde unter Mean-Reversion zerfallen), sondern gibt einen
  **transienten, abklingenden Demand-Schock** auf den `OilMarket` → Preis bewegt sich mit realer Trägheit und
  rebalanciert. Öl-Bewegungen haben jetzt end-to-end eine echte Ursache (Konjunktur + Angebot/Nachfrage-Events).
- **Live-verifiziert:** 1-Jahres-Multi-Seed-Playtest → real. Bandbreite, kein Clamp-Pinning, Boom-Öl > Rezession-Öl;
  direktionaler Event-Schock-Test. **Suite 641 grün, 0 Regression.** ⏳ Verbleibt: GUI-Feel-Kalibrierung (nur dein Auge).
- **Git:** `65dea8a` (Modell) · `4687e50` (Integration) · `05042cf` (Default-Flip + Event-Migration), gepusht.
- **Nächste M3-Scheiben:** gleiches Muster auf **Gold/Gas/Metalle** (Producers + Nachfrage-Kopplung), dann **Länder**
  als Aggregat (BIP/Politik/Ressourcen-Endowment treiben die Sektoren) — der Welt-Layer wächst Rohstoff für Rohstoff.

---

## 🧭 STRATEGISCHER PIVOT (2026-07-02) — ZUERST LESEN

**StockSim wird von „Börsen-Spiel" zu „lebende Welt-Simulation, deren Anzeige die Börse ist" umgebaut.**
Vollständige Vision + Architektur + Meilensteine: **`design/WORLD_SIM_VISION.md`** (Nordstern, zuerst lesen).

Fixiert: **emergentes Pricing** (Welt→Firma→Fundamentaldaten→Kurs, GBM nur Mikro-Rauschen) · **Evolution im
Repo** (v0.3.0 als Tag einfrieren, Branch `worldsim`) · Stack bleibt C#/.NET + Electron/React · **Custom Charts**.
---

## ✅ STAND 2026-07-09 (Abend) — Design-Overhaul Schritt B: zwei wählbare Terminal-Themes

**Slate + Amber als auswählbare Farb-Themes gebaut, live, app-weit.** Auf der token-driven Chart-Engine (s.u.)
aufgesetzt: neue Paletten überschreiben nur Token-Werte, alles Token-gebundene folgt automatisch.

- **Themes:** `globals.css` — `.theme-slate` (neutrales Graphit + Teal-Cyan-Akzent) + `.theme-amber` (warmes
  Anthrazit + Phosphor-Amber, Bloomberg-Heritage). Platziert VOR den `.colorblind-*`-Blöcken → Colorblind gewinnt
  weiterhin für green/red. Wählbar via **Settings → Video → Color Theme** (`theme: 'default'|'slate'|'amber'` in
  `GameSettings`, persistiert; App-Effekt togglet `theme-*`-Klasse auf `<body>` neben der Colorblind-Logik).
- **Tokenization-Sweep (damit Themes app-weit greifen):** ~58 hardcodierte Farben in den Top-Offender-/Immer-
  sichtbaren Files (`App.tsx`, `TopBar.tsx`, `StockDetailView.tsx`, `OrderPanel.tsx`) → Tokens. Alpha-Varianten via
  `color-mix(in srgb, var(--token) N%, transparent)`; Glows via `--accent/green/red-glow`. `rgba(0,0,0/255…)`
  (Schatten/Overlays) bewusst gelassen. Nicht-getroffene Extended-Palette-Farben (`#A855F7`, `#14B8A6`, `#93c5fd`,
  RSI `#A78BFA`) gelassen — brauchen neue Tokens.
- **Verifiziert:** `tsc` grün · **Suite 90/90 grün** · Playwright gegen echten Backend (`pw-theme.mjs`, neu):
  Stock-Detail in slate/amber/default gescreenshottet, **ganze Trading-Fläche + Chart recoloren kohärent, 0
  Page-Errors**. Screenshots visuell geprüft (Chrome, Watchlist, Order-Panel, Chart folgen alle).
- **Sweep-Abdeckung (3 Runden, ~200 Konvertierungen, alles verifiziert):** ganze **primäre Oberfläche** token-driven —
  Chrome (`App`, `TopBar`, `LeftSidebar`, `RightSidebar`, `CentralArea`) + alle Haupt-Tabs (`StockDetailView`,
  `DashboardTab`, `NewsTab`, `PortfolioTab`, `MarketTab`, `AnalyticsTab`, `OrdersTab`, `OptionsChain`, `OrderPanel`,
  `NewsTicker`, `StockScreener`) + `NewGameScreen`. Slate/Dashboard/News in Slate visuell verifiziert (kohärent, 0 Errors).
- **Modals/Screens auch gesweept** (2. Runde): TitleScreen, SaveLoad, Wiki, DecisionCase, Glossary, CommandBar,
  Settings, HelpTip, ConfirmOrder, Orderbook. Orderbook: dynamische Per-Row-Alpha via `color-mix` mit berechnetem %.
- **Git: 4 saubere Commits auf `worldsim`** (nicht gepusht): `6b5cc57` Chart-Engine · `f9f71fd` Themes ·
  `540914d` Surface-Sweep · `3677b57` Modals-Sweep. `tsc` grün, 90/90 Tests durchgehend.
- **⏳ Rest = Entscheidungen / bewusste Skips (kein blinder Fleiß mehr):**
  1. **Extended-Palette-Farben ohne Token** (kategoriale Viz: Sektor-Allocation-Swatches, Compare-Overlays,
     RSI `#A78BFA`) — **Design-Entscheid:** neue Tokens ODER theme-fix lassen. Empfehlung: **fix lassen** (kategoriale
     Paletten bleiben konstant, wie `--chart-*`).
  2. **Nicht anfassen (§9):** `ScenarioBar`, `decisionCases.ts` + Szenario-Zeug → per **E6 entfernt**, nicht polieren.
     `ErrorBoundary` (Standalone-Crash-Screen, bewusst self-contained). `NewGameScreen`-Daten-Array-Farben (`${c}0A`).
     `JournalTab`/Screener-Mini-Charts: SVG/Canvas-Attribut-Farben (var() greift dort nicht).
  3. **Daten-Files** (`careerTitles.ts`, `decisionCases.ts`): Farben als Hex-Alpha-Konkat → brauchen Mapping-Layer.
  4. **E4-Abschluss offen:** LW/ECharts-Lib + `StockChartLW`/`StockChart` + Deps noch drin (als Fallback hinter Flag).
     Endgültig entfernen, wenn Canvas-Engine im echten Gebrauch bewährt (Vision §8: „bis eigene Engine steht, geduldet").
- **Design-Board:** interaktives Richtungs-Board als Artifact gebaut (Palette-Switcher auf Live-Terminal-Mockup) —
  diente der Richtungswahl; User-Entscheid: **beide (Slate + Amber) als Auswahl**, statt einer.

---

## ✅ STAND 2026-07-09 — M1 Custom-Chart-Engine gebaut (Lib-Ablösung, E4)

**Die eigene Canvas-2D-Chart-Engine steht und ist Default.** Damit ist die in E4/§8 beschlossene Ablösung
von TradingView Lightweight Charts umgesetzt — volle Parität mit dem alten `StockChartLW`, alles selbst
gezeichnet, kein Lib-Invest mehr.

- **Neu:** `frontend/src/components/charts/engine/CanvasChartEngine.ts` — **framework-agnostische** Pure-TS-Engine
  (owns `<canvas>` + Render-Loop + Input; kein React drin → kann später abgedockte Multi-Monitor-Panels treiben,
  wie §8 verlangt). `+ .brain`. Dazu die dünne React-Hülle `StockChartCanvas.tsx` (identischer Props-Vertrag wie
  StockChartLW → **Drop-in**). `+ .brain`.
- **Features (Parität):** Candle/Line/Area, Volume-Overlay (Pane-Boden), Indikator-Overlays (SMA20/50/200, EMA12,
  Bollinger×3, VWAP), RSI-Sub-Pane (0–100 + 70/30-Guides), Auto-Y-Scale über sichtbares Fenster, Zeit-/Preisachse,
  Grid, **Drag-Pan + Wheel-Zoom + Crosshair** (mit Achsen-Labels). Live-Tail-Update erhält Zoom/Scroll; View
  recentert nur bei Symbol-/Timeframe-Wechsel (Right-Edge-Follow).
- **Verdrahtung:** `StockDetailView` — Canvas ist **Default**; `localStorage useLWChart=1` → alte LW-Variante,
  `useECharts=1` → Legacy-ECharts (beide als reversible Fallbacks behalten). Lib noch nicht deinstalliert.
- **Verifiziert:** `npx tsc --noEmit` grün · **Frontend-Suite 90/90 grün** · Playwright-Screenshots gegen echten
  Backend (`frontend/pw-canvas.mjs`, neu): Candles/Volume/Overlays/RSI/Crosshair rendern, Live-Tick bewegt letzte
  Kerze ohne View-Sprung, Pan+Zoom verschieben das sichtbare Fenster sauber, null Console-Errors.
- **Token-driven (2026-07-09, Schritt A vom Design-Overhaul-Prep):** Die Engine hardcodet keine Farben mehr —
  sie liest sie zur Draw-Zeit aus den CSS-Design-Tokens (`globals.css :root`: `--bg-primary`, `--green-primary`,
  `--red-primary`, `--chart-purple/pink/cyan/blue`, `--warning`, `--text-accent`, `--border`, …) via
  `getComputedStyle` (live → folgt Theme-Wechseln), Alpha-Varianten in JS abgeleitet, Fallbacks auf die alten Hex.
  Kein `globals.css`-Change nötig, null Duplikation. **Fixt nebenbei einen A11y-Bug:** die Colorblind-Themes
  (`.colorblind-*` remappen green/red → blau/orange bzw. teal/pink) greifen jetzt auch im Chart (Kerzen folgen).
  Damit ist der Chart **overhaul-ready**: künftiger Frontend-Redesign ändert nur Tokens, Chart zieht automatisch mit.
  `tsc` grün, Suite 90/90 grün. ⏳ Automatischer Colorblind-Screenshot-Beweis stand aus (Harness kollidierte mit
  der parallel laufenden Electron-Instanz am selben Backend) — im laufenden Spiel per Colorblind-Toggle live prüfbar.
- **⚠️ Alles uncommitted** (Branch `worldsim`). Committen wenn gewünscht.
- **Offen (bewusst v2, war auch in LW nicht drin):** Compare-Overlays (%-normalisiert), OHLC-Info-Label oben links,
  MACD-Sub-Pane, Tooltip-Box, Bollinger-Band-Fill. Danach: LW-Dep entfernen. Größeres M1-Reststück: die eigentliche
  **Terminal-UI** (Welt-/Länder-/Sektor-Ansichten, Game-HUD-Feinschliff) — separat von der Chart-Engine.

---

## ✅ STAND 2026-07-03 (Tagesabschluss) — MORGEN HIER WEITER

**Emergentes Pricing ist KOMPLETT gebaut, live, kalibriert — und committed + gepusht.** Der ganze Bogen ist durch:
M0 → 4 level-basierte Faktor-Kanäle (InputCost/OutputPrice/Demand/Valuation) → Treiber-Interdependenz-Kaskade (inkl.
Lohn-Preis-Spirale) → **kritischer Dead-Code-Bug gefixt** (der 16:00-Tages-Block mit `DriftFundamentals`/Earnings/
Chart-Kerzen lief im Spiel NIE) → scharfgeschaltet → realistisch kalibriert (Generierungs-KGV-Fix, Verlust-Erosion) →
dekorierte `GetSectorMultipliers` aus dem Pricing abgelöst (**ein** Bewertungsmodell) → volle Sektor- + firmen-
individuelle Abdeckung → Treiber: Öl, Gas, Löhne, Zins, Inflation, Gold, PMI, Konsumklima. **626 Tests grün**,
GUI visuell verifiziert (Playwright-Screenshots). Mechanismus-Details unten + in `EMERGENT_COUPLING.md`.

**Git:** Branch **`worldsim`** (4 Commits, alle nach `origin/worldsim` gepusht), `master` = Release mit Tag `v0.3.0`,
Working Tree sauber. Heavy Live-Guards sind `[Skip]` in `RealismDiagnostic` (Öl-A/B + 1-Jahres-Playtest, un-skipbar).

**MORGEN — frische Optionen (kein Zwang, Reihenfolge diskutierbar):**
1. **M3-Sprung** (der nächste *echte* Vision-Meilenstein): Länder & Ressourcen als Layer — die Treiber aus einer
   Welt *entstehen* lassen statt Random-Walk. Verdient eine fokussierte Session (siehe `WORLD_SIM_VISION.md` §5/§10).
2. **Breite:** weitere Rohstoff-Treiber (Kupfer→Industrials, Lithium→EV) — dasselbe Muster, schnell.
3. **Overlays emergent** (Policy/DXY/Zyklus) oder **Custom-Chart-Engine** (M1, Lib-Ablösung steht noch aus).
4. **Feintuning** am GUI-Playtest (Feel — nur dein Auge kann's beurteilen).

⚠️ Charts: **Custom-Canvas + eigenes Design beschlossen**, Lightweight wird noch abgelöst (noch nicht angefasst).

---

## Emergente Abdeckung vervollständigt + GUI-Playtest gemacht (2026-07-03)

- **Fast alle Sektoren jetzt emergent getrieben:** Financials (Zins→NIM/Demand), Materials (Inflation→OutputPrice + Mining→Gold),
  Utilities/Telecom (Zins→Valuation) ergänzt (feste Elastizitäten, kein RNG-Shift). Playtest bleibt realistisch (Median −3,7%,
  keine Kollapse). Nur Healthcare bewusst offen (real fast zins-immun). Suite 625 grün.
- **GUI-Playtest visuell verifiziert** (Playwright-Screenshot-Harness `frontend/pw-run.mjs` + Screenshots gelesen): App läuft
  stabil, null Errors, realistischer Markt, Kurs tickt live, **neue Tages-Kerzen werden angehängt** (= scharfgeschalteter Block
  wirkt sichtbar). App startbar via `npm run electron:dev` (electron-main ggf. erst `npx tsc -p tsconfig.electron.json`).
- **✅ Migration abgeschlossen (§8):** die dekorierte `GetSectorMultipliers` ist **aus dem Pricing entfernt** — kein
  Doppel-Zählen mehr, Sektor×Treiber ist jetzt allein emergent. (Funktion bleibt nur für UI-Anzeige.) Öl-Effekt danach
  sauberer (Spread +23%), Markt realistisch (Median ~−5%). **Ein Bewertungsmodell.** Suite 625 grün.
- **Noch dekorativ (bewusst):** Policy/DXY/Election-Overlays + Konjunkturzyklus-FairValue-Drift — später evtl. emergent,
  oder bewusst als Deko behalten. Und: Healthcare hat bewusst keine Zins-Kopplung (real fast immun).

---

## ✅✅ Tages-Block SCHARFGESCHALTET — emergente Schicht läuft jetzt live (2026-07-03)

**Der Bogen ist geschlossen.** Der kritische Bug (Tages-Block toter Code) ist gefixt und der Block **permanent aktiv**:
DriftFundamentals + alle 4 emergenten Kanäle + Earnings + Dividenden + Chart-Kerzen + Persona-Evolution laufen jetzt
zum ersten Mal im Spiel. Suite **625 grün, 2 Diagnosen skipped, 0 Fehler**. Live-Öl-A/B: Käufer +5,2% < Verkäufer +11,3%
(richtungsrichtig), keine Kollapse. **Der `RealismDiagnostic` (skipped) ist jetzt ein valider Live-Guard** (Block aktiv).

**Wie es aktivierbar wurde (2 FairValue-Fixes):**
- **(i)** `EarningsEngine` ungeclamptes `FairValue=eps×18` entfernt → FairValue allein von `RecalculateFairValues`
  (geclampt+geglättet) → FV glatt statt +76%-Sprung, kein Kollaps. (`ReleasedEarnings_ShouldUpdateFundamentals` umgestellt.)
- **(ii)** Unprofitable erodieren FairValue −1%/Tag (statt einfrieren) → Öl-Effekt richtungsrichtig.
- **Aktivierung:** `GameLoop.ExecuteTick` — 16:00:00 vom After-Hours-Early-return ausgenommen (Zeile ~402).

**Headless-Playtest → Bärisch-Skew gefunden UND gefixt (2026-07-03):**
- Playtest (1 Jahr) zeigte erst: stabil, aber **Median −50%/Jahr** (objektiv falsch). Instrumentierung (Price vs FairValue
  vs Earnings) lokalisierte die Wurzel: **Earnings stiegen (+5%), aber FairValue kollabierte (−51%)** → ein
  **Generierungs-Inkonsistenz-Bug**: generiertes KGV ~27 (Revenue=MarketCap×10-40%×Marge) vs. Sektor-KGV ~18 in
  `RecalculateFairValues` → bei Block-Aktivierung repricet der ganze Markt einmalig nach unten.
- **✅ Fix:** NetIncome bei Generierung aus `MarketCap / Sektor-KGV` abgeleitet (±30% Dispersion), Sektor-KGV in
  gemeinsamen Helper `SectorPE()` extrahiert (Generierung ⇄ FairValue-Modell konsistent). Playtest danach:
  **Price median −2,9%, avg +3,9%, Earnings +3%, FairValue +3% — realistischer Markt** (best +92% / worst −46%, kein Kollaps).
- **Bonus:** Die Generierungs-Änderung legte einen latenten **Supply-Chain-Bidirektionalitäts-Bug** frei
  (`CompanyPersonalityGenerator`: einseitiger Supplier-Link wenn Supplier customer-voll) — **gefixt.**

**✅ Öl-Effekt-Fine-Tune (2026-07-03):** Öl-A/B war invertiert (geschockte Airline machte Verlust, fiel aber *langsamer*
als profitable Peers — Unprofitable-Erosion war flach 1%/Tag). Fix: **Erosion proportional zur Verlust-Tiefe** (Verlust/Umsatz
× 0,25 + 0,005, gecappt 0,5–3%/Tag). Ergebnis: **Öl-Käufer −14%, Verkäufer +2%, Spread +16%** (realistisch), Markt bleibt
gesund (Playtest Median −1%, keine Kollapse). Beide Guards grün, Suite 625 grün.

**→ Offen:** nur noch echtes „fühlt sich richtig an"-Feintuning (GUI-Playtest). Harness: `RealismDiagnostic` +
`Playtest_OneYear_Readout` (skipped, un-skipbar). Details: `EMERGENT_COUPLING.md §7`. ⚠️ **Alles uncommitted** (Git blockiert).

---

## Stabilisierung: 2 FairValue-Fixes → Tages-Block AKTIVIERBAR (2026-07-03) ✅ (→ oben: jetzt scharf)

Auf den kritischen Befund (Tages-Block tot, Aktivierung → Instabilität) folgt der Stabilisierungs-Fortschritt:
- **(i) FairValue-Pfad vereinheitlicht** — `EarningsEngine` hard-setzte `FairValue = eps×18` **ungeclampt** → entfernt.
  FairValue gehört jetzt allein `RecalculateFairValues` (geclampt+geglättet). Trace: FV glatt (+1,5%/Tag statt
  +76%-Sprung), Kurs klebt dran, **kein Kollaps.** (`ReleasedEarnings_ShouldUpdateFundamentals`-Test umgestellt.)
- **(ii) Unprofitable erodieren** (`RecalculateFairValues` else-Zweig, −1%/Tag) statt einzufrieren → **Öl-Effekt
  richtungsrichtig** (Käufer relativ schlechter als Verkäufer).
- **Verifiziert:** Block temp-aktiviert → nur **1 betroffener Test** (der Earnings-Test), Öl-A/B stabil + direktional korrekt.

**Beide Fixes behalten** (sicher: Suite **625 grün, 2 Diagnosen skipped**). **Block-Aktivierung (Einzeiler) zurückgerollt**
— bleibt **Stufe-3-Entscheidung mit User** (formt released-Feel). 

**→ Offen:** (a) **Block scharfschalten** (16:00:00 vom After-Hours ausnehmen); (b) **Magnituden-Kalibrierung**
(Öl-Effekt da, aber klein vs. Bärisch-Skew). Details: `EMERGENT_COUPLING.md §7`. Re-Messung: skipped `RealismDiagnostic`
+ temp-Aktivierung.

---

## 🔴🔴 KRITISCHER BUG BESTÄTIGT (2026-07-02): Tages-Block ist toter Code (→ oben: Fixes fertig)

**Der wichtigste Befund der ganzen Session.** Der 16:00-Tages-Block in `GameLoop.ExecuteTick` (~Zeile 667) —
enthält `DriftFundamentals` (Fundamentaldaten-Drift + ALLE 4 emergenten Kanäle), `EarningsEngine`, `DividendEngine`,
DailyHistory-Kerzen, Persona-Evolution, Reputation, Steuern — **läuft im Spiel NIE.** Weil bei exakt 16:00:00
`IsMarketOpen()` FALSE + `IsAfterHours()` TRUE ist → der After-Hours-Early-`return` (~Zeile 402/419) schattet den
Tages-Block. Bewiesen: Airline-Revenue byte-identisch mit Start über 60 Sim-Tage.

**Einzeiler-Fix verifiziert** (16:00:00 vom After-Hours ausnehmen) → Block läuft, Revenue bewegt sich, Ölschock
trifft Earnings. **ABER die Tages-Dynamik ist grob instabil** (Preise → ~0,01, NI ±Mio, schon im Baseline). Nie in
einem laufenden Spiel stabil-getestet. **Fix zurückgerollt** (Spiel stabil halten).

**→ NÄCHSTER SCHRITT (Top-Priorität, Stufe-3, mit User): Bug-Fix + Stabilisierungs-Pass** der Tages-Schicht. Erst
danach ist das ganze emergente Pricing überhaupt live wirksam. **Offene Frage an User/History:** war der Block in
v0.3.0 schon tot (großer latenter Bug) oder hat ein späterer After-Hours-Refactor ihn geschattet? Details:
`EMERGENT_COUPLING.md §7`. Diagnose-Instrumentierung im (skipped) `RealismDiagnostic`.

**Lehre der Session:** Unit-Tests grün ≠ Feature läuft. Erst der Live-A/B-Diagnose-Lauf hat aufgedeckt, dass die
ganze emergente Schicht (40+ grüne Tests) im echten Loop dormant ist. **Live verifizieren.**

---

## M2 — Kosten-Kanäle level-basiert + TIEFERER Befund (2026-07-02) 🔴 emergente Schicht läuft im Loop nicht

**Zwei Dinge in dieser Runde:**

**(A) ✅ Kosten-Kanäle auf level-basiert umgebaut.** Vorher change-basiert (reagierten auf Öl-*Tagesänderung*) →
konstant hohes Öl feuerte nie. Jetzt `FundamentalDynamics.InputCostMarginLevel`/`OutputPriceMarginLevel` (auf
Abweichung), pro Tag aus stabiler Marge via **Swap** gesetzt (`_appliedDriverMargin`, kein Akkumulieren). **Alle 4
Kanäle jetzt level-basiert**, change-Maschinerie (`_previousDriverValues`) raus. OutputPrice wirkt jetzt auf Marge
(erreicht Earnings). Tests umgeschrieben, Suite **625 grün + 1 Diagnose skipped**.

**(B) 🔴 Diagnose-Instrumentierung deckte einen VIEL tieferen Bug auf:** die emergente Fundamentaldaten-Schicht
läuft im Live-Tick-Loop **gar nicht.** Probe: Airline-**Revenue byte-identisch mit Startwert** über 60 Tage, NI in
Baseline & Shocked identisch (trotz Öl 140 vs 98). `DriftFundamentals` (täglicher Drift + alle 4 Kanäle) wird im
Roh-`ExecuteTick` nie erreicht — der Tages-Block hängt an `GameTime == exakt 16:00:00`, davor ein After-Hours-
Early-`return` (GameLoop ~402/419). **⚠️ Harness-Artefakt vs. echter Gameplay-Bug — muss gegen den `NewGame`-Pfad
verifiziert werden.**

**→ NÄCHSTER SCHRITT (vor allem anderen): Befund B verifizieren/fixen** — läuft `DriftFundamentals` im echten Spiel?
Wenn nicht, ist die ganze emergente Arbeit dormant. Details: `EMERGENT_COUPLING.md §7`. Diagnose-Instrumentierung
bleibt im (skipped) `RealismDiagnostic` für die Untersuchung.

---

## M2 — Live-Diagnose: Realismus-Befund (2026-07-02) ⚠️ Kalibrier-Lücke aufgedeckt (durch B überholt)

**Rausgezoomt + instrumentiert** (`RealismDiagnostic`: A/B über ein Quartal, gleicher Seed, mit/ohne anhaltenden
Ölschock; jetzt **[Skip]** = manueller schwerer Test, perturbierte sonst den Suite-State). Die Instrumentierung
lieferte die **präzise Wurzel**:
1. 🔴 **Kosten-Kanäle feuern bei anhaltendem Level nicht.** Airline-NetIncome in Baseline & Shocked **byte-identisch**
   → Ölschock ändert Earnings NULL. Weil `InputCost/OutputPrice` **change-basiert** sind (reagieren auf Öl-Tagesänderung);
   konstant hohes Öl = Änderung 0 → kein Hit; initialer Sprung vom `prev=current`-Init verschluckt. **Fix: Kosten-Kanäle
   LEVEL-basieren** (wie Demand/Valuation) — Marge = f(Öl-Abweichung), pro Tag aus stabiler Basis-Marge gesetzt (nicht akkumuliert).
2. **Sekundär:** selbst bei korrekter Earnings-Bewegung ist Fundamentaldaten→Preis gedämpft (FairValue-Blend 5%/Tag +
   schwache Mean-Reversion vs. GBM). Plus breiter Bärisch-Skew (Session-37-bekannt).

**→ Nächster Schritt: Kosten-Kanäle level-basieren** (kehrt meine change-basierte Design-Entscheidung um; braucht
`Stock.BaseMargin` + Umbau in `DriftFundamentals` + Kanal-Tests). Reversal einer Entscheidung → **mit User abgestimmt.**
Danach evtl. Transmissions-Dämpfung (Mean-Reversion/FairValue-Tempo, Kern-Preisdynamik → Stufe-2). Zahlen: `EMERGENT_COUPLING.md §7`.
Suite **625 grün + 1 Diagnose skipped**.

---

## M2 — Treiber-Interdependenz, Kern-Kaskade (2026-07-02) ✅ der größte Realismus-Sprung

**Ein Shock rollt jetzt durch die ganze Ökonomie.** `EconomicEngine.PropagateDriverCoupling()` (täglich, live im
Loop nach `TickDay`): azyklische Kaskade **Öl→Inflation→Zins→Wachstum→Arbeitslosigkeit→Konsumklima/PMI** — kleine,
gelagerte, geklammerte Tages-Pushes proportional zur Treiber-Abweichung. Acyclic ⇒ stabilisierend, kein Runaway.
Composed mit den 4 Kanälen: *ein* Ölschock trifft nun automatisch Airlines/Energy (Öl) **und** Tech-Valuation +
RealEstate-Demand (Zins, weil Öl→Inflation→Zins). Schließt die #1-Vereinfachung aus dem Realitäts-Abgleich (§7) teilweise.

**Umgesetzt (TDD, 625/625 grün, +2):** `PropagateDriverCoupling` (kuratierte Makro-Links, snapshot-basiert für
1-Tag-Lag) + Verdrahtung in GameLoop-Tagessequenz. `EconomicCouplingTests` (Kaskade + Baseline-Stabilität).
Live-Ripple minimal, da Tests kurz-horizontig + Kopplung klein/gelagert.
- ⚠️ **Brain-File offen:** `EconomicEngine` (großes File, nur Teile gelesen) — Coupling-Methoden dokumentiert in EMERGENT_COUPLING, volles Brain deferred (Qualität > Vollständigkeit).
- **Offen:** breitere Verflechtung (Demand-Pull, Löhne↔Inflation, DXY), Erwartungs-Pricing, Kausal-Event-Graph (§S4).

---

## M2 — Alle 4 Coupling-Kanäle gebaut (2026-07-02) ✅ Demand + Valuation ergänzt

Die zwei **level-basierten** Kanäle (reagieren auf Treiber-Abweichung vom Normalwert, nicht Tagesänderung):
- **Demand** → setzt die Growth-Baseline, zu der `GrowthTrajectory` zurückkehrt (Konsumklima↑ → Retailer-Growth↑; Zins↑ → Homebuilder-Growth↓).
- **Valuation** → justiert das PE-Multiple in `RecalculateFairValues` (Zins↑ → Tech-FairValue↓, **ohne** Earnings zu ändern).

**Umgesetzt (TDD, 622/622 grün, +9):**
- `EconomicEngine.GetDriverDeviation` (Normalisierung: (Wert−Baseline)/Scale, gleiche Anker wie `GetSectorMultipliers`).
- `FundamentalDynamics.DemandGrowthBaseline` + `ValuationMultipleFactor` (pure, **signierte** Elastizität, geclampt).
- `DriftFundamentals`: Demand-Baseline an `GrowthTrajectory`. `RecalculateFairValues` (jetzt `internal`): Valuation-Faktor aufs PE.
- Generierung: Consumer/Luxury→Konsumklima-Demand, Industrials→PMI-Demand, Real Estate→Zins-Demand, Technology→Zins-Valuation (feste Elastizitäten, kein rng).
- Tests: `DriverCouplingTests` (+7 pure), `DemandValuationTests` (+2 Integration).
- 🐛 **End-to-End-Fix (Balance-Check):** `DriftFundamentals` berechnete die Marge *nach* dem Umsatz-Update → Umsatzwachstum war **earnings-neutral**, d.h. Demand/OutputPrice/Growth erreichten den Kurs nie. Fix: Marge aus Pre-Drift-Zustand erfassen → Earnings wachsen mit Umsatz → FairValue. **Erst dadurch wirken die umsatzseitigen Kanäle wirklich.** (622/622 grün, breite Änderung ohne Regression.)
- `EmergentBalanceTests` (Mehrtages-Guard): Ölschock über ein Quartal → Airline-FairValue runter, Produzent hoch, begrenzt (kein Runaway).
- `EMERGENT_COUPLING.md §7` = **Realitäts-Abgleich** (warum das Modell realistisch ist: DCF + Faktormodelle; 2 benannte Vereinfachungen: Treiber-Unabhängigkeit + Erwartungs-Pricing).
- ⚠️ **Transitorisch:** dekorierte `GetSectorMultipliers` läuft noch **parallel** → leichtes Doppel-Zählen. **Nächster Schritt:** Sektoren einzeln ablösen (Migrationspfad §8) + restliche Matrix-Kopplungen als Exposures.

---

## M2-Kern — Generelles Driver-Coupling-Modell (2026-07-02) ✅ InputCost + OutputPrice gebaut

**Statt sektorweise von Hand: ein data-driven Exposure-Modell.** M0s Öl→Marge ist hineinmigriert; die
Schlagzeile ist bewiesen — *derselbe Treiber, gegensätzliche Wirkung*: Öl↑ schadet Airlines (InputCost→Marge)
und nützt Öl-Produzenten (OutputPrice→Umsatz), im selben Lauf. Design-Landkarte: `design/EMERGENT_COUPLING.md`.

**Umgesetzt (TDD, 613/613 grün):**
- `Models/DriverExposure.cs`: `ExposureChannel` (InputCost/OutputPrice gebaut; Demand/Valuation reserviert) + `record DriverExposure(Driver, Channel, Elasticity)`.
- `Stock.DriverExposures` (ersetzt das M0-`FuelCostExposure`-Feld — ein Modell, kein Legacy daneben).
- `FundamentalDynamics.InputCostMarginDelta` (war `FuelCostMarginImpact`) + neu `OutputPriceRevenueDelta` (pure, change-basiert, Pass-Through/Realization + Clamp).
- `GameLoop.DriftFundamentals`: iteriert Exposures, routet je Kanal; `_previousDriverValues`-Dict (generalisiert das `_previousOilPrice`-Tracking).
- `EconomicEngine.GetDriverValue(string)`.
- Generierung: Transportation → Öl-InputCost (subsektor-fein), Energy „Oil & Gas" → Öl-OutputPrice (0.55; Renewables 0). **Lebt im Spiel.**
- Tests: `DriverCouplingTests` (10, inkl. Schlagzeile), `OilAirlineEmergenceTests` (2, Airlines runter + Produzenten hoch im selben Lauf).
- ⚠️ Hinweis: Energy-Exposure bewusst **ohne** `rng`-Draw (feste Subsektor-Werte) — vermeidet Generierungs-Stream-Shift, der sonst einen bestehenden Options-Theta-Grenzfall (`RealismTests.OptionsGreeks`) triggerte. Kein fremder Test aufgeweicht.

**Nächster Schritt:** Kanal 3+4 (**Demand** level-basiert: z.B. Zins→RealEstate; **Valuation**: Zins→Tech-PE) als eigener Increment — anderer Mechanismus als die change-basierten Kanäle. Dann Sektoren schrittweise von `GetSectorMultipliers` auf Exposures umstellen.

---

## M0 — Emergentes Pricing, erste vertikale Scheibe (2026-07-02) ✅ Engine-Ebene bewiesen

**Scheibe:** Ölschock → Airlines. Öl bewegt Transportation-Kurse jetzt über die *Fundamentaldaten*
(Öl → Treibstoffkosten → Marge → Earnings → FairValue → Kurs), firmenindividuell nach Exposure — Weg B
(emergent) statt Weg A (dekorierter Sektor-Nudge). Baut auf `FundamentalDynamics` + `EconomicEngine.OilPrice`.

**Umgesetzt (TDD, 610/610 grün, +9 neu):**
- `FundamentalDynamics.FuelCostMarginImpact(oilChangePct, exposure)` (pure, kalibriert: Treibstoff ~15–35%
  Umsatz, Pass-Through 0.5, Clamp ±0.25). Brain-File geschrieben.
- `Stock.FuelCostExposure` (neues Feld; statisch, generierungs-deterministisch → kein Save-DTO nötig).
- `GameLoop.DriftFundamentals`: Öl-Tagesänderung → Marge/NetIncome proportional zur Exposure (`_previousOilPrice`-Tracking).
- `GameLoop.GenerateStocks`: Transportation-Firmen kriegen subsektor-feine Exposure (Airlines ~28–35%, EV 0) → **lebt im Spiel**.
- `PriceEngine.DeterministicMode` (echtes Feature: „pure emergent", Rausch AUS) → beweist im Test, dass der Move fundamental ist, nicht Zufall.
- Test-Seams: `InternalsVisibleTo` + `GameLoop.DriftFundamentals` internal + `GameLoop.Economy`.
- Tests: `FuelCostImpactTests` (7), `OilAirlineEmergenceTests` (2 — Öl→Earnings proportional + FairValue↓→Kurs↓ deterministisch).

**Nächste Schritte (M0-Abschluss / M1):**
1. Der alte dekorierte `sectorMult`-Öl-Nudge in `DriftFundamentals` (Zeile ~1777) kann später raus (jetzt vom Fundamental-Pfad abgelöst) — noch drin, kein Big-Bang.
2. Optional: kurzer Balance-/Playtest über die Agent-Bridge (E5), ob die Magnitude im echten Spiel real-plausibel bleibt.
3. Danach M1 (Custom-Chart-Engine) oder M2-Breite. **Breite ist als Design fertig:** `design/EMERGENT_COUPLING.md`
   (generelles Exposure-Modell mit 4 Kanälen InputCost/Demand/OutputPrice/Valuation + volle Kopplungs-Matrix +
   Abdeckungs-/Lücken-Liste). Nächster Bau-Schritt dort: `DriverExposure` generalisieren + an Zins→RealEstate
   (Demand) und Öl→Energy (OutputPrice) gegenprüfen, bevor die Abstraktion festklopft. **Nicht sektorweise von Hand.**
- ⚠️ Alles **uncommitted** (Git blockiert) — mischt sich mit Session-37-Block; zusammen committen wenn Git geht.

---

## Letztes Update: 2026-06-23, Session 37 (Tagesabschluss)

## ⏯️ HANDOFF — hier morgen (2026-06-24) weitermachen
**Diese Session erledigt (alles uncommitted, ⚠️ als Erstes committen wenn möglich):**
- News↔Firma-Initiative Punkt 1–8 KOMPLETT (Events→Fundamentals, health-gewichtete Auswahl, FoundedYear-Reife,
  Persona-Evolution, Trajektorie-Text, konkrete Analyst-Quotes, sektor-bewusste Platzhalter+Headlines, Rendering-Cleanup)
- Save/Load-Audit + 4 Fixes (Personality, Achievements, Alerts, Tax) + Punkt-1-Feintuning
- MCP-Bridge-Prototyp (`mcp-bridge/`)
- Backend: 601 Tests grün. Frontend: 90 Tests grün.
- **Chart-Migration ECharts→Lightweight v5: LW ist jetzt DEFAULT, live-tickend, visuell verifiziert.**
- electron:dev-Fixes (Backend-Pfad, Port-Arg) + Logger-Env-Var + happy-dom Test-DOM.

**Nächste Schritte (offen):**
1. **Committen** (großer Block oder pro Thema) — sobald Git wieder geht.
2. Chart: **Compare-%-Overlays + OHLC-Label** in `StockChartLW` portieren → volle Parität → dann ECharts (`StockChart.tsx` + echarts-Deps) entfernen.
3. Optional: Pro-Tick-Re-Render von StockDetailView dämpfen (Header-Flash etc.), Punkt 6 Teil 2 weitere Templates.
- App starten: `cd frontend && npm run electron:dev` (Electron-Main bei Bedarf `npx tsc -p tsconfig.electron.json`). Headless testen/screenshotten: `frontend/pw-run.mjs`.

---

## Status: v0.3.0-dev — News↔Firma-Tiefe, Punkt 1 von 7 (Events → Fundamentals)

### Session 37: News/Firma-Verzahnung — Punkt 1 (2026-06-23)

**Diagnose (3 Explore-Agents):** Content ist reich, aber von der Mechanik abgekoppelt.
8 von 16 Personality-Feldern sind totes Holz; News-Text ist Fill-in-the-blank; Event-Auswahl
ist uniform random; News→Fundamentals-Feedback fehlt → Preise reverten, weil keine echten
Zahlen sie stützen. 7-Punkte-Plan (Phase A mechanisch 1-3, Phase B Text 4-7).

**Punkt 1 umgesetzt — Events → Fundamentals-Feedback (TDD, 9 neue Tests, 539/539 grün):**
- `EventEngine.ApplyFundamentalImpact()` (neu): 1× pro Event, Moderate+, ~35% der Preisreaktion
  wird zu bleibender Fundamental-Änderung. Tag-gesteuert: earnings→NetIncome+Growth,
  product→Revenue+Growth, fraud→NetIncome-Hit + sofortiges Credit-Downgrade, sonst Growth-Nudge.
  Aufgerufen neben `UpdateAnalystSentiment` in `ApplyActiveEvents`, gleich gegated.
- `FundamentalDynamics.GrowthTrajectory()` (neue Datei, pure): RevenueGrowth biased den
  täglichen Drift und mean-revertet zur Baseline (0).
- `GameLoop.DriftFundamentals()` ruft GrowthTrajectory → Trajektorie persistiert, läuft nicht davon.
- Loop schließt sich: Beat → Growth↑ → Drift↑ → Revenue wächst echt → Kurs revertet nicht mehr voll.

**Punkt 2 umgesetzt — Event-Auswahl nach Firmen-Zustand gewichtet (TDD, 7 neue Tests, 546/546 grün):**
- `FundamentalDynamics.CompanyHealthScore()` (pure): 5 Faktoren (Profitabilität, Growth, Leverage,
  Credit, Momentum) → [-1,+1]. `NewsWeight()`: kranke > gesunde > neutral (1.0). `WeightedPick()`:
  deterministisches gewichtetes Sampling.
- `EventEngine.PickWeightedStock()` ersetzt den uniform-random Pick in `TryGenerateCompanyEvent`.
  ETFs niedriges Gewicht (0.4). News ist jetzt kausal: distressed Firmen kommen häufiger vor.

**Punkt 3 abgeschlossen — tote Personality-Felder aktivieren:**
- ✅ **FoundedYear → Reife-Kurve** (4 Tests): `FundamentalDynamics.MaturityModifiers(age)` (pure) →
  junge Firmen mehr Drift+Vol (×1.4/×1.3), alte stabiler (×0.85), monoton, geclampt. PriceEngine
  bekommt `CurrentYear` (von GameLoop), wendet Reife auf Vol + Drift an. Unit-Tests (CurrentYear=0) unberührt.
- ✅ **Persona-Evolution** (6 Tests, 556/556): Archetyp driftet entlang Defensive↔Wachstum-Spektrum
  nach dauerhaft gutem/schlechtem Lauf. `UpdateStreak`/`PersonaEvolutionDirection`/`EvolveArchetype`
  (pure) + `GameLoop.EvolvePersonas()` (täglich), Schwelle 90 Tage, emittiert "strategy_shift"-News.
  Neues Feld `CompanyPersonality.PerformanceStreak`. Streak resettet bei CEO-Firing.
- ⏭️ SecondaryProduct → Umsatzstrom & Headquarters: bewusst geskippt (zu dünn, kein echter Hebel).

**Phase B — Text-Tiefe:**
- ✅ **Punkt 4 — News-Text spiegelt Trajektorie** (6 Tests, 562/562): `FundamentalDynamics.TrajectoryPhrase()`
  (pure) → Klausel aus RevenueGrowth/PerformanceStreak/ConsecutiveMisses/Return20Day (Distress dominiert,
  sonst leer). In `ResolveTemplate` an die Summary angehängt: „The move comes as {Name} is {phrase}."

- ✅ **Punkt 5 — Analyst-Quotes konkret** (7 Tests, 569/569): `FundamentalDynamics.AnalystMetricClause()`
  (pure) → kennzahlen-gegründeter Leitsatz aus P/E, Revenue-Growth, Margin + Event-Typ (earnings),
  je nach Richtung. In `GenerateAnalystQuote` dem generischen Satz vorangestellt (nur wenn stock != null).
- 🔧 **Test-Infra-Fix:** Test-Parallelität assembly-weit aus (`ParallelizationConfig.cs`). Grund: statischer
  `GameEvent._nextId` + `ResetIdCounter()` in 6 Klassen → Race kollidierte Event-IDs → mein ID-gekoppeltes
  Gating übersprang Events. Produktcode unberührt; Suite jetzt deterministisch (~23s).

- ✅ **Punkt 6 Teil 1 — Platzhalter-Pools sektor-bewusst** (3 Tests, 572/572): neue pure Klasse
  `SectorContent` (Reasons/Technologies pro Sektor + neutraler Fallback). `{technology}` nutzt jetzt das
  **echte Firmen-Produkt** (FlagshipProduct/SecondaryProduct — aktiviert totes Feld), sonst Sektor-Pool;
  `{reason}` sektor-bewusst. Globale Unsinn-Arrays (Healthcare→„blockchain") entfernt. `{division}` bleibt generisch.

- ✅ **Punkt 7 — Benannte Entitäten persistent** (4 Tests, 576/576): neue testbare `EntityRegistry`
  (pro `(symbol, role)` klebt die Entität). In `ResolvePlaceholders` sind `{executive}`/`{activist}`/
  `{investor}`/`{person}` für Firmen-Kontext jetzt sticky (Elliott jagt weiter dieselbe Firma);
  Sektor/Macro bleibt random.

- ✅ **Punkt 6 Teil 2 — Headlines sektor-spezifisch** (2 Tests, 578/578): `SectorContent.EarningsMetrics(sector)`
  (pure) + `{sector_metric}`-Platzhalter. In die vier Kern-Earnings-Entries (Strong/Moderate Beat,
  Severe/Moderate Miss in `earnings.json`) eingewoben → Pharma „beats on pipeline progress", Bank
  „miss on softer net interest margin". Andere Spezial-Entries hatten schon Domain-Flavor.

**electron:dev-Fixes (2026-06-23):** Beim App-Start zwei Bugs gefunden, die den Electron-Dev-Modus komplett
brachen: (1) Backend-Pfad in `src/main/main.ts` um eine Ebene falsch (`../../` → `../../../backend/StockSim.Engine`);
(2) Backend (`Program.cs`) ignorierte den Port-Arg → lief auf zufälligem Port, Renderer hardcoded auf 8765.
Fix: Backend honoriert jetzt optionalen Port-Arg (8765); MCP-Bridge ohne Arg → weiter dynamisch. App startet
+ verbindet sich sauber (electron-main muss via `tsc -p tsconfig.electron.json` kompiliert sein, dist/main fehlte).

**Chart-Migration ECharts→Lightweight v5 — M1 (2026-06-23):** `lightweight-charts@5.2.0` installiert.
Parallele `StockChartLW.tsx` (ECharts-Version unangetastet → null Risiko): Candle/Line/Area + Volume-Overlay
+ SMA/EMA/BB/VWAP-Overlays + RSI-Subpane (v5-Panes), Dark-Theme, gleiche Props wie StockChart.
**Verifiziert headless:** type-check 0 Fehler, voller Vite-Build OK, Runtime-Wiring-Test (`StockChartLW.test.tsx`,
happy-dom + LW gemockt) grün, Frontend-Suite 90/90. happy-dom als Test-DOM ergänzt (jsdom in dieser Env durch
ESM-Bug kaputt). Umschalter in StockDetailView via `localStorage.setItem('useLWChart','1')`.
**LW ist jetzt DEFAULT-Chart** (ECharts nur noch via `localStorage useECharts=1` als Fallback). Live-Update
**visuell selbst verifiziert** (Playwright-Harness `frontend/pw-run.mjs`: zwei Screenshots 6s Abstand, Kurs
61.99→60.01, Candle wandert). Fixes nach User-Feedback: `rightOffset:0` (kein „Zukunfts"-Gap), Recent-Window
(letzte ~90 Bars statt fitContent auf alles → Candles sichtbar groß, Live-Bewegung wahrnehmbar). Das gemeldete
„Hover-Flackern" war der ECharts-Tooltip — LW hat kein Info-Overlay (nur Crosshair). News-Banner-Reset gefixt
(`NewsTicker` snapshottet alle 25s).
**Live-Update:** inkrementell via `series.update()` nur auf dem geänderten Tail (letzter Balken + angehängte);
`fitContent` nur bei Erstladung/Symbolwechsel → Zoom/Scroll bleibt beim Tick erhalten. Full-setData-Fallback
im try/catch. Wiring-Test deckt's mit ab. **Wichtig:** `chartData` (historische OHLCV) tickte nicht — Fix in
StockDetailView: `liveChartData`-Memo überlagert `stock.price` auf den letzten Candle pro Tick (close/high/low),
sodass der Chart real-time läuft (gilt nur für die LW-Variante).
⏳ Offen: **visuelle GUI-Abnahme durch User** (Pixel kann ich nicht sehen), dann Compare-%-Overlays + OHLC-Label
portieren, danach ECharts ersetzen + entfernen.

**Balance-Playtest + Bias-Diagnose (2026-06-23):** 40-Tage-Playtest (seed 123): **0 Errors/NaN/Runaways/Crashes**
— Punkt 1 lässt Preise NICHT explodieren. ABER Returns stark bärisch (median −30.8%). Mit schnellem
Multi-Seed-Diagnose-Harness (`CompanyEventBiasDiagnostic`, 6 Seeds) disambiguiert: **kein Count-Skew aus
Punkt 2** (pos:neg = 35:27, ratio 1.30), hohe Seed-Varianz (+6.4% bis −17.4%) → das −30% war **Seed-123-Pech
(Bear-Phase)**, keine systematische Regression. Real ist nur ein *vorbestehender* Magnituden-Skew (negative
Templates heftiger als positive), den Punkt 1 leicht verstärkt. **Kein dringender Fix.** ✅ Feintuning umgesetzt:
Punkt-1-Faktor für negative Events 0.35→0.30 (`NegativeFundamentalFactor`, mit Asymmetrie-Test).
Diagnose-Harness bleibt als Balance-Guard.

**Save/Load-Audit (2026-06-23, 5 Tests, 599/599):** systematisch geprüft, welche dynamischen Zustände
den Load überleben. Gefixte Lücken (alle mit Regressions-Test in SaveManagerTests):
- **Personality** (CEOArchetype-Evolution, dynam. CreditRating, neues `PerformanceStreak`) — in StockSave-DTO.
- **Unlocked Achievements** (Progression) — neue AchievementSave-Liste in SaveData.
- **Price Alerts** (liegen auf Portfolio) — neue AlertSave-Liste in PortfolioSave.
- **Tax-Summary** (YTD Gains/Losses, TotalTaxPaid, WashSaleDisallowed) — neue TaxSave.
- Alle null-safe/legacy-kompatibel.
- **Bewusst NICHT persistiert (transient/akzeptabler Verlust):** aktive Meme-Phasen, geplante Event-Cascades
  (PendingFollowUps), 30-Tage-WashSale-Tracker (privat, kurzlebig), EntityRegistry-Kontinuität (kosmetisch).
  Bereits abgedeckt (vorher): GameTime, Stocks-Fundamentals, Portfolio, Options, Economic, SMA, Rumor, Reputation.

**Punkt 8 — News-Rendering-Cleanup (2026-06-23, ~17 Tests, 595/595):** Bugs aus dem Playtest gefixt.
- **Locale/Komma:** InvariantCulture global in `Program.Main` (DefaultThreadCurrentCulture) + `EventEngine.Tick`
  (Core-Wrapper, deckt template + hardcoded Generatoren) + `ResolvePlaceholders`-Wrapper → „$0,15" → „$0.15".
- **Catch-all-Garbage:** neue pure `NewsText.FillLeftovers` — unaufgelöste Platzhalter werden nach Name
  (numerisch?) bzw. Kontext ($-davor/%-danach) geroutet → Zahl statt „$the companyM"; „the the company" vermieden.
- **Doppel-Suffix:** `{revenue}/{market_cap}` als Milliarden-Zahl + `NewsText.FixDoubleUnits` („$162MB" → „$162M").
- **Doppelartikel:** Analyst-Quote `sectorName` Fallback auf stock.Sector statt „the market".
- Regressions-Test `NewsRenderingTests` rendert echte Events unter de-DE und prüft alle Muster.

**MCP-Bridge-Prototyp (`mcp-bridge/`, 2026-06-23):** „Dual-Channel-Frontend" — exponiert StockSims
WS-Vertrag als MCP-Tools (new_game/set_speed/place_order/get_portfolio) + Resources
(market/portfolio/news). Node ESM + offizielles MCP-SDK, spawnt Backend selbst. Smoke-Test grün:
Agent spielt StockSim nativ über MCP (Buy gefüllt, Portfolio/News gelesen). README mit Claude-Config.
Nächster Schritt Richtung generisches `AgentBridge`-SDK (+ Web `window.agent`-Transport).

**Initiative News↔Firma KOMPLETT (Punkt 1-7, alle Teile durch).** 7 neue Source-Files
(FundamentalDynamics, SectorContent, EntityRegistry + 4 Test-Files + ParallelizationConfig), ~39 neue
Tests, **578 grün**. **⚠️ Alles uncommitted** — committen wenn möglich.

---

## Status: v0.3.0-dev — Portfolio Cleanup: Bible → Spec Rename + README

### Session 36: Portfolio Cleanup (2026-04-18)

**Ziel:** Repo public-ready machen — "Bible"-Lingo eliminieren, professionelle README ergänzen.

**Umbenennung Design-Docs:**
- `GAME_DESIGN_BIBLE.md` → `DESIGN_SPEC.md`
- `BIBLE_INDEX.md` → `DESIGN_INDEX.md`
- `BIBLE_EXPANSION.md` → `DESIGN_CHANGES.md`
- Git-History via `git mv` erhalten.

**Terminologie-Sweep:**
- `Bible` → `Spec` in 60 Code-Files (264 Kommentar-Referenzen wie "Bible 4.1" → "Spec 4.1")
- Alle Design-Docs aktualisiert: CLAUDE.md, AUDIT_LOG, SESSION_HISTORY, MASTER_ROADMAP, ROADMAP, PROJECT_STATUS
- Self-Referenzen in den drei umbenannten Files gefixt ("Game Design Bible" → "Game Design Spec")
- Nur `.claude/settings.local.json` behält 2 alte Referenzen (User-Allowlist-Historie, irrelevant)

**README.md hinzugefügt:**
- Pitch, Feature-Highlights, Tech-Stack-Tabelle, Architektur-Diagramm (ASCII)
- Build-/Dev-Instruktionen, Projekt-Struktur, Roadmap-Link
- Shields.io-Badges (Status, Version, Stack, Tests, License)
- Screenshot-Slots mit TODO (Screenshots in `docs/screenshots/` ablegen)

**Verifikation:**
- `dotnet build`: 0 Warnings, 0 Errors
- `dotnet test`: **530/530 Tests grün**, 7s Dauer
- `grep -iE "bible"` über Repo: nur .claude/settings.local.json (gewollt)

**Branch:** `cleanup/professional-naming` — noch nicht gemerged, User muss reviewen.

**Offen (bewusst):**
- Screenshots machen + `docs/screenshots/` füllen
- Optional: GitHub Actions CI-Workflow für grünes Test-Badge

---

## Vorherige Sessions (31-35, Stand 2026-04-01)

### Status vorher: v0.3.0-dev — Supply Chain + History Mode + 18 neue Systeme

### Projekt-Kennzahlen
- **~48.000 Zeilen Code** (~21.000 Backend + 13.500 Frontend + 10.500 Tests + 5.500 Content + 400 ML)
- **~165 Dateien**
- **530 Backend Tests** grün (vorher 496)
- **2.400+ Headlines** in Event-Templates, **572 Templates** (Tier1: 389, Tier2: 127, Tier3: 33, Tier4: 23)
- **64 Gründungsgeschichten**, **144 Company Descriptions**
- **39 Szenarien** (inkl. 9 History Mode), **53 Achievements**, **12-Step Tutorial**
- **14 Sektoren** (inkl. Commodities), **3 Commodity ETFs** (GLD/SLV/USO)
- **63+ Frontend-Realism-Fixes**, **18 neue Backend-Systeme**
- **57 Wiki-Artikel** in 8 Kategorien

### Session 32: Commodities + Realismus-Tests

**Commodity ETFs (GLD, SLV, USO):**
- 3 neue Commodity-ETFs als spezielle ETFs die EconomicEngine-Indikatoren tracken
- GLD → GoldPrice/10, SLV → GoldPrice*0.035/3, USO → OilPrice*0.7
- Trait: "Commodity ETF", Sektor: "Commodities" (neuer 14. Sektor)
- Tick-Noise ±0.1% für natürliche Preisbewegung
- Commodity-spezifischer Sektor-Multiplier in EconomicEngine
- Voll handelbar (Buy/Sell/Short), keine Options (wie alle ETFs)
- 252 Tage History generiert, Save/Load kompatibel

**Frontend Realismus-Fixes (Batch 1):**
- Market-Index-Ticker in TopBar: SIMX (mit ±%), Fear&Greed, Gold, Oil — live aktualisiert
- Speed Controls: "1x/2x/5x/10x" → "▶/▶▶/▶▶▶/MAX" — weniger gamey
- Career Badge: Aus TopBar entfernt → nur noch im Game Menu (Esc) sichtbar
- Lock-Emojis: "SHORT 🔒" → "SHORT (Locked)" — professioneller

**Frontend Realismus-Fixes (Batch 2):**
- Fundamentals gruppiert: 5 collapsible Sections (Core, Valuation, Analyst, Ownership, Technical)
- Farbcodierung: Net Income grün/rot, Upside grün/rot, D/E > 2 rot
- "Add to Watchlist" Button im Stock-Detail-Header (Star-Icon, toggle)
- Watchlist-Suche: Filter-Input erscheint bei > 5 Einträgen (Symbol + Name)

**Frontend Realismus-Fixes (Batch 3):**
- Order Confirmation: Cash Before/After, Portfolio %, Trade Size %, Margin-Warnung
- Tab-Gruppierung: Views (D/P/M) | Trading (O/X) | Analysis (N/A/J) — visuelle Divider
- Side-Farbe im Confirm-Dialog (Buy=grün, Sell=rot, Short=gelb, Cover=blau)

**Frontend Realismus-Fixes (Batch 4):**
- Watchlist Sort: All/▲/▼ Buttons — Gainers/Losers filtern und sortieren
- Sector Performance-Bars: Hintergrund-Balken proportional zur Avg-Change
- Orderbook Imbalance: Bid/Ask-Verhältnis als Fortschrittsbalken + Prozent
- Orderbook Depth: Intensität der Balken proportional zur Size, große Orders fett
- Best Bid/Ask visuell hervorgehoben (grün/rot), restliche Levels gedämpft

**Frontend Realismus-Fixes (Batch 5):**
- Chart OHLC-Label: O/H/L/C + Change oben links im Chart (wie TradingView)
- Persistent Account Bar: Equity | Cash | Day P&L% | Positions | Trades — immer sichtbar
- Account Bar unter TopBar, über main-layout — kein Scrollen nötig

**Frontend Realismus-Fixes (Batch 6):**
- Time-in-Force: GTC/Day Buttons bei Limit/Stop Orders (visuell, Backend unterstützt GTC)
- Volume-Bars 50% kräftiger (0.3 → 0.5 Opacity) für bessere Lesbarkeit
- Input Focus States: Blauer Glow-Ring + Shadow bei Focus, Placeholder-Styling, Disabled-States
- Right Sidebar collapsible: 40px-Streifen mit Symbol+Change vertikal, Toggle-Button
- Collapsed = 260px mehr Platz für Chart/Content, wie Bloomberg Sidebar-Toggle

**Frontend Realismus-Fixes (Batch 7):**
- Technical Summary Cards: Trend (Bullish/Neutral/Bearish via SMA), RSI (14), Day Range Bar
- Short Borrow Indicator: Short Interest % angezeigt bei Short/Cover Orders, High SI Warning
- News Ticker TAPE: Commodity-Preise (Gold, Oil, Rate, F&G) am Anfang des Scrollbands
- Alle 3 Cards unter dem Chart, kompakt, Bloomberg-Style mit Farbcodierung

**Frontend Realismus-Fixes (Batch 8):**
- 52-Week Range Bar im Stock-Detail-Header (Yahoo Finance Style, mit Preis-Marker)
- Market Cap Category Label: Mega/Large/Mid/Small/Micro Cap
- Keyboard Shortcut Hints in Account Bar (Ctrl+K, Space, Esc)
- 36 Frontend-Fixes total

**Frontend Feature-Adds (Batch 9):**
- RSI(14) Sub-Chart unter dem Preis-Chart (eigenes Grid, 70/30 Referenzlinien, lila)
- Grid-Layout: Price 52% | Volume 8% | RSI 14% — professionelles 3-Panel-Layout
- RSI wird aus Close-Preisen berechnet (Wilder Smoothing, 14 Perioden)
- DataZoom synchronisiert über alle 3 Panels
- RSI Label in Indikator-Legende
- Journal: Cumulative P&L Curve (SVG, grün/rot je nach Gewinn/Verlust)
- Journal: P&L Distribution Histogram (7 Buckets von >+10% bis <-5%)
- 40 Frontend-Fixes total

**Frontend Feature-Adds (Batch 10):**
- Orders Tab: Status-Filter (All/Open/Filled/Rejected), Open Orders Summary mit Cancel All
- Chart-Höhe 450→550px für bessere RSI-Darstellung im 3-Panel-Layout
- 43 Frontend-Fixes total

**Frontend Feature-Adds (Batch 11):**
- Price Alerts UI: Set Above/Below Alerts, View aktive Alerts, Delete per Stock
- Backend-Integration: SetAlert/DeleteAlert/GetAlerts WebSocket-Messages
- MACD Technical Card: Line, Signal, Histogram mit Farbcodierung (grün/rot)
- 4 Technical Cards total: Trend | RSI | Day Range | MACD
- 48 Frontend-Fixes total

**Frontend Feature-Adds (Batch 12):**
- Portfolio: Close/Cover Buttons pro Position + Current Price + Sector Spalte
- Short-Positionen mit "SHORT" Badge markiert, Cover statt Close Button
- Alert Toast Notifications: Pop-up links oben wenn Price Alert triggert (5s Auto-Dismiss)
- Klick auf Toast → navigiert zum Stock
- 52 Frontend-Fixes total

**Frontend Feature-Adds (Batch 13):**
- Stock Compare UI: Typ Symbol in Input → Overlay auf Chart (bis 3 Stocks, farbkodiert)
- Vergleichs-Tags mit Remove-Button, nutzt bestehende compareStocks Chart-Prop
- Earnings Countdown Badge: "Earnings in 5d | Est: $1.42" im Stock-Header
- Farbkodierung: rot wenn ≤3 Tage, gelb wenn ≤30 Tage
- 55 Frontend-Fixes total

**Frontend Feature-Adds (Batch 14):**
- Quick Size Buttons: 25%/50%/75%/MAX im OrderPanel + Position Size als % of Equity
- Watchlist Quick-Trade: B/S Buttons erscheinen bei Hover (dispatcht tradingShortcut)
- Dashboard Treemap: Sektoren proportional zu Market Cap statt gleichmäßiges Grid
- Treemap zeigt Market Cap ($B) bei großen Zellen, Tooltip mit Details
- 59 Frontend-Fixes total

**Frontend Feature-Adds (Batch 15):**
- Notification Center: Bell-Icon in TopBar mit Dropdown der Major/Crisis/Black Swan Events
- Roter Dot-Indikator wenn Major-Events vorhanden, klickbar → navigiert zum Stock
- Account Bar: Total Return % (seit Spielstart), Advance/Decline Ratio mit Mini-Bar
- 63 Frontend-Fixes total

**Session 33: QA, Scenarios, Achievements, Tutorial**

**Regression Tests (9 neue Tests, 523 total):**
- Save/Load: Commodity-ETFs, Portfolio mit Trades, Economic State
- Performance: 50 Stocks × 1000 Ticks (< 10ms), 250 Stocks × 500 Ticks (< 50ms)
- Full Playtest: 5 Tage ohne Errors, Trading während Simulation
- Options Chains: 10 Tage NaN-frei
- Event Engine: 50+ Events über 5 Tage

**Szenarien 20 → 30:**
- commodity_king, etf_only, sector_rotation, flash_crash, earnings_season
- margin_call, gold_rush, black_monday, value_investor, the_apprentice

**Achievements 31 → 46:**
- Wealth: Dividend Collector, Passive Income, Well Diversified
- Trading: Options Trader, Commodity Trader, Short Seller, Limit Master, Scalper
- Market: Economic Cycle, Crisis Survivor, Informed Trader, Global Investor
- Risk: Perfect Week, Recovery Artist, Tax Efficient

**Tutorial 8 → 12 Steps:**
- News & Events: Filter, Severity, Portfolio-Filter
- Price Alerts & Comparison: Alert-System, Stock Overlay, RSI
- Commodities & ETFs: GLD/SLV/USO, Sektor-ETFs
- Analytics & Journal: Equity Curve, Sharpe, P&L Distribution

**Realismus-Tests (18 neue Tests in RealismTests.cs):**
- CommodityETFs: Existenz, Traits, Preis-Tracking, History, Handelbarkeit, Options-Ausschluss
- Options: Eligibility-Kriterien, Greeks-Ranges (Delta/IV/Theta), OTM-Expiry
- Ökonomie: Sektor-Multiplier, Indikator-Drift, Commodity-Sektor
- Archetypes: Alle Stocks haben Personality mit CEO/Story/Rating
- Performance: 250 Stocks × 100 Ticks < 50ms/Tick
- Meme Stock Engine: Crash-frei nach 100 Ticks

### Session 31: Content-Tiefe

**"My Portfolio" News Filter:**
- Neuer Filter-Button im News Feed zeigt nur News für eigene Positionen
- Filtert über `affectedSymbols` × Player Portfolio Positions
- Deaktiviert (opacity 0.4) wenn keine Positionen

**Founding Stories 15 → 64:**
- PreWar (< 1950): 4 → 20 — Industrial Revolution, Dynastien, Depression, Wartime
- MidCentury (1950-1989): 4 → 20 — GI Bill, Space Race, Conglomerate Era, Early Computing
- Modern (≥ 1990): 7 → 24 — Dot-com, Pivots, Climate, Dorm Rooms, Y Combinator

**Company Descriptions 48 → 144:**
- 12 Sektoren × 12 Templates (vorher 4), 3 Tiers pro Sektor:
  - Scale/Authority (4): Marktposition, Revenue, globale Reichweite
  - Innovation/Mission (4): R&D, Patente, Vision
  - Character/Story (4): Kultur, Geschichte, Ruf
- Viele multi-sentence Descriptions mit mehr Tiefe

**Event Template Headlines 3x expandiert:**
- earnings.json: 51 Templates × 6 Headlines = 306 (vorher ~153)
- corporate.json: 38 Templates × 6 Headlines = 228 (vorher ~114)
- management.json: 36 Templates × 5-6 Headlines = 181 (vorher ~108)
- products.json: 35 Templates × 5-6 Headlines = 185 (vorher ~105)
- Summaries enriched mit Lore-Platzhaltern ({ceo}, {headquarters}, {product})
- Bloomberg/Reuters Wire-Service Style: "FLASH:", "BREAKING:", Ticker-Prefix

**Tier 3 Templates 20 → 33 (+13 neue Arcs):**
- commodity_shocks: +3 (lithium_shortage, natural_gas_crisis, steel_dumping)
- financial_stress: +3 (commercial_real_estate_crisis, crypto_contagion, insurance_catastrophe)
- regulatory: +3 (ai_regulation_wave, pharma_pricing_crackdown, financial_deregulation)
- sector_crashes: +3 (ev_bubble_burst, biotech_winter, telecom_debt_crisis)

**Tier 4 Templates 16 → 23 (+7 neue Black Swan Arcs):**
- market_crashes: +2 (etf_liquidity_crisis, quant_meltdown)
- industry_shocks: +2 (energy_transition_shock, cybersecurity_catastrophe)
- systemic: +2 (dollar_crisis, derivatives_blowup)
- geopolitical: +2 (taiwan_crisis, sanctions_cascade) — war 1 extra da trade_war hier

**Archetyp-spezifische Headlines:**
- `ApplyArchetypeHeadlineFlavor()` in EventEngine.cs
- 25% Chance: CEO-Archetype beeinflusst Headline-Text
- Positive Events: Leadership-Prefix ("Sarah Chen's vision pays off: ...")
- Negative Events: CEO-Reaction-Suffix ("...vows: 'We will come back stronger'")
- 12 Archetypes × 2 Varianten (positiv + negativ) = 24 Flavor-Texte

**Named Entity Pools 2x erweitert:**
- Executives: 8 → 16 (GC, CRO, CPO, CIO, CHRO, VP Engineering, CDO)
- Activists: 8 → 16 (ValueAct, Cevian, Sachem Head, Greenlight, Corvex)
- Investors: 8 → 16 (JPMorgan AM, PIMCO, Bridgewater, GIC, Norges Bank, CalPERS)
- Locations: 10 → 20, Countries: 10 → 20, Technologies: 8 → 16
- Divisions: 8 → 16, Reasons: 8 → 16, Partners: 8 → 16
- Analyst Firms: 10 → 20 (Cowen, Piper Sandler, Raymond James, Bernstein, RBC)

### Session 30: Bugfixes + Realismus Phase 1

**Fix: Keine News zum Spielstart:**
- Initial-News atomar im `MarketSnapshot` mitgesendet (`initialNews`-Feld)
- 5-8 Events, garantiert 1 Macro + 1 Sector + 1 Company, Tier2 eingemischt

**Fix: News-Inhalt zu dünn:**
- Analyst-Quotes 4→20 pro Richtung (40 total), kontextbezogen mit Aktien-/Sektornamen

**Fix: Save/Load Frontend-Handler:**
- `GameSaved`/`GameLoaded`-Handler: Fehler-Feedback in TopBar + App.tsx

**Realismus Phase 1: Monetary Policy + Dollar Index:**
- `MonetaryPolicyStance` Enum: Tightening/Neutral/Easing/QE
- Automatische Übergänge basierend auf Zinsen, Inflation, GDP, Unemployment
- Fed Balance Sheet ($2-12T) driftet je nach Stance
- Policy-Änderungen generieren Major-News ("FOMC signals hawkish pivot...")
- Sektor-Multipliers: QE → Growth+/Financials-, Tightening → umgekehrt
- Dollar Index (DXY, 80-120): getrieben von Zinsdifferential + Policy + Wirtschaftsstärke
- DXY-Sektor-Effekte: starker USD hurt Exporteure (Tech/Industrials/Materials/Energy)
- Beide neue Indikatoren im Frontend-EconomicData verfügbar

**Realismus Phase 2: Earnings Guidance + Margin Cascade:**
- Earnings Guidance: 60% der Firmen geben nach Earnings Forward Guidance
- 4 Richtungen: Raised/Maintained/Lowered/Withdrawn mit realistischen Wahrscheinlichkeiten
- Guidance passt nächstes Quartal Expected EPS an + generiert News + kleiner Kursimpact
- Margin Call Cascade: PolicyStressFactor (Tightening=0.4, QE=-0.2) fließt in Hedge Fund Stress
- Kaskaden-Multiplier: Stress >0.7 verstärkt sich selbst (positive Feedback-Loop)
- Sektor-Leverage: Real Estate 1.8x, Financials 1.5x, Tech 1.3x härter getroffen
- High-Beta Stocks leiden überproportional in Kaskaden
- Cascade News ("MARGIN ALERT: Leveraged funds facing margin calls...")

**Realismus Phase 3: ETF Flows + GEX + Index Rebalancing:**
- Index Rebalancing: Quartalsweise Neu-Zusammensetzung der Sektor-ETFs
- ETF Flow Effects: Additions → Kaufdruck (+0.5-1.5%), Removals → Verkaufsdruck, 5-Tage Decay
- Rebalancing-News im Feed ("QUARTERLY INDEX REBALANCING", "Stock added/removed from ETF")
- GEX (Gamma Exposure): Aggregiertes Gamma pro Aktie über alle Options-Kontrakte
- Negative GEX → Dealer-Hedging ampliziert Moves (Volatilität steigt)
- Positive GEX → Dealer-Hedging dämpft Moves (Gamma-Wall, Stabilisierung)
- GEX-Alerts bei extremen Werten im News-Feed
- GEX-Pressure wird per Tick auf Preise angewandt

**Realismus Phase 4: Meme Stock Dynamics:**
- MemeStockEngine: 5-Phasen-Lebenszyklus (Discovery→FOMO→Squeeze→DiamondHands→Crash)
- Emergentes Verhalten: Trigger bei High Short Interest >20% + Low Price <$30 + Weak Fundamentals
- 30% Chance auf Failed Squeeze (Shorts halten durch → direkter Crash)
- Squeeze-Intensität skaliert mit Short Interest
- Max 1-2 pro Spieljahr, 60-Tage-Cooldown
- Phasen-spezifische Preispressure: Discovery +5-10%, FOMO +10-30%, Squeeze +30-100%, Crash -8-20%
- Volume-Multiplier bis 20x in Squeeze-Phase
- Short Interest wird während Squeeze reduziert (Zwangs-Covering)
- News pro Phase: "Social media buzz building", "SHORT SQUEEZE", "Meme stock mania fades"

### Alle 8 Realismus-Features abgeschlossen ✅

| Phase | Feature | Status |
|-------|---------|--------|
| 1 | Monetary Policy (QE/Tapering) | ✅ DONE |
| 1 | Dollar Strength Index | ✅ DONE |
| 2 | Earnings Guidance | ✅ DONE |
| 2 | Margin Call Cascade Enhancement | ✅ DONE |
| 3 | ETF Flow Effects | ✅ DONE |
| 3 | Options Gamma Exposure (GEX) | ✅ DONE |
| 3 | Index Rebalancing | ✅ DONE |
| 4 | Meme Stock Dynamics | ✅ DONE |

Plan-Datei: `.claude/plans/structured-meandering-map.md`

**Save/Load Fix:**
- Stale Initial-News beim Laden werden jetzt gecleart (keine falschen News nach Load)
- Save speichert jetzt: Stock-Fundamentals (Revenue, NetIncome, D/E, ShortInterest, AnalystRating, etc.)
- Save speichert jetzt: Economic State (Zinsen, Inflation, DXY, Policy Stance, VIX, Balance Sheet)
- Save-Version 0.2.1, Rückwärtskompatible Migration von 0.2.0
- Frontend-Handler für Fehler-Feedback waren schon implementiert

**News Template-Platzhalter Fix:**
- `{eps}` → echtes Quarterly EPS aus NetIncome/SharesOutstanding
- `{price}`, `{target_price}`, `{target}` → echte Stock/Target Preise
- `{revenue}`, `{market_cap}` → echte Fundamentals
- `{margin}` → echte Nettomargenberechnung (NetIncome/Revenue)
- `{rev_growth}`, `{growth}` → echte RevenueGrowth
- `{ownership_pct}` → echte InsiderOwnership
- `{spread}` → echter Bid-Ask-Spread
- `{employees}` → echte Mitarbeiterzahl
- `{shares}`, `{amount}` → skaliert proportional zu MarketCap
- `{beat_pct}`, `{pct}` → berücksichtigt Stock-Volatilität

**Screen Transitions + Tutorial Check:**
- Animierte Screen-Transitions: Fade-Out (250ms) → Screen-Wechsel → Fade-In (250ms)
- User-getriggerte Wechsel (Menü-Buttons) animiert, Backend-getriggerte instant
- Tutorial (8-Step Spotlight Overlay) existiert und ist funktional
- Tutorial-Events (stockSelected, orderPlaced, speedChanged) werden korrekt gefeuert

**Playtest-Bugfixes:**
- Options Chain: NaN-Schutz für alle Greeks/Preise, ITM-Feld korrekt berechnet (war fehlend)
- Options Chain: Alle Werte als `double` serialisiert (war `decimal` → potentielle JSON-Probleme)
- Load Game: `hasSaves` dynamisch via `ListSaves` Backend-Query (war hardcoded `false`)
- Load Game: "Continue"/"Load Game" Buttons jetzt aktiv wenn Saves existieren
- Options News Spam: 5-Tage-Cooldown für Unusual Activity, 3-Tage für Pin Risk (war: jeden Tag)

**Save-System Overhaul:**
- Saves gruppiert nach Spiel (Ordner `game_{seed}/`)
- Benannte Saves: Dialog mit Texteingabe statt nur Quicksave
- SaveMeta: SaveName + PlayerName gespeichert
- Load Screen: Spiele als aufklappbare Gruppen, jeder Save mit Game-Datum, Day, Cash, Market Phase
- "Continue" lädt neuesten Save (über alle Spiele)
- "Load Game" öffnet gruppierte Save-Liste
- Backend: ListSavesGrouped(), DeleteSave, FilePath-basiertes Laden
- Rückwärtskompatibel: Legacy-Saves im Root werden als "legacy" Gruppe angezeigt

**Options UI Overhaul:**
- Stock Picker: Grid aller optionsfähigen Aktien mit Preis, Change%, MarketCap
- Back-Button → zurück zum Picker von Chain-View
- Header: Symbol + Live-Preis + Trend + Quantity-Input
- Buy/Sell Buttons mit Labels statt "B"/"S", Tooltip mit Preis
- Open Interest + Volume Spalten, ATM/ITM Highlighting
- Positions-Bar: zeigt offene Options-Positionen mit Live P&L
- PortfolioUpdate wird nach Options-Trade gesendet (Cash + Equity aktualisiert)
- Options-Wert fließt in totalEquity + portfolioValue ein

**Price Model Rebalancing:**
- Base-Drift 3x erhöht: 0.00001→0.00003 pro Tick (~7.5%/Jahr statt 2.5%)
- Alle Stock-Traits proportional angepasst (Growth ~12.5%, Turnaround ~15%, Value ~6%)
- Bull/Bear Phase symmetrisch: beide ±5%/Jahr (war asymmetrisch +3.75/-5%)
- Defensive Bear-Offset verstärkt (0.00001→0.000015)
- HistoryGenerator Returns an Live-Drift angeglichen (Neutral: -5% bis +15% statt -10% bis +10%)
- Contraction Sector-Multipliers milder (z.B. Tech 0.5→0.6, Real Estate 0.4→0.5)
- Chart-Übergang: ECharts dispose Error gefixt (try/catch + leerer Platzhalter bei empty option)

**ETF Holdings sichtbar:**
- Klick auf einen ETF → StockDetailView zeigt "Holdings (N stocks)" Tabelle
- Jeder Constituent: Symbol (klickbar), Name, Sector, Price, Change%, Weight%
- Sortiert nach Gewichtung (Market-Cap-Weighted)
- Backend: ETFEngine.GetConstituents() + DataQueryHandler liefert constituents mit StockFundamentals

**Bankruptcy-Check: Options-Value inkludiert** (war: Spieler mit $445k in Options ging bankrott weil Equity-Check Options ignorierte)

**News Platzhalter komplett überarbeitet:**
- 20+ fehlende Platzhalter hinzugefügt: {executive}, {activist}, {investor}, {location}, {country}, {technology}, {division}, {reason}, {competitor}, {partner}, {market}, {analyst_firm}, {new_ceo}, {person}, {number}, {num_shares}, {percentage}, {duration}, {stake_pct}
- Jeder Platzhalter hat Pool aus realistischen Namen (z.B. "Elliott Management", "Goldman Sachs", "CFO Sarah Chen")
- Catch-All Fallback: "the company" statt random Zahlen
- Vorher: "loses sales chief 25" → Jetzt: "loses sales chief CFO Sarah Chen"

**Short P&L% Vorzeichen gefixt** (Math.Abs auf TotalCost)

**DaySummary Throttle-Bug gefixt:**
- DaySummary war innerhalb des `shouldSend` Blocks (nur jeder 10. Tick bei Max Speed)
- Bei Maximum Speed konnte der 16:00-Tick übersprungen werden → kein DaySummary
- Fix: DaySummary + Autosave aus dem shouldSend Block rausgezogen → wird immer gesendet

**Realism Improvements (9/10 aus Plan umgesetzt):**
- Event Volume Spikes: VolumeMultiplier wird jetzt angewendet (war nie benutzt obwohl auf 500+ Events gesetzt)
- Dynamic Spreads: Market Maker widenen bei Open/Close (1.5x/1.4x) + bei Events (Minor 1.2x bis Major 2.5x)
- Crash Correlation: Konvexe Kurve (√stress), Mega-Caps fallen 30% weniger, Small-Caps 25% mehr
- Stop-Loss Cascades: AI simuliert Stop-Clustering an runden Zahlen ($5/$10/$25/$50/$100), Druckwellen
- Sector Rotation News: "ECONOMIC SHIFT: Analysts see transition from X to Y" mit Gewinner-/Verlierer-Sektoren
- Opening Gap from Events: Gap proportional zu Overnight-Events statt rein zufällig
- Flight to Quality: Defensive/Utilities driften aufwärts bei hohem Stress, Speculative abwärts
- DaySummary Throttle-Fix: wird jetzt immer gesendet (war: nur wenn shouldSend=true)
- Pre-Market übersprungen (After-Hours existiert schon, Pre-Market verkompliziert unnötig)

**News-Volume auf Bloomberg-Level erhöht:**
- EventEngine: ~38 Events/Tag (Macro 3 + Sector 10 + Company 25) + andere Engines ~15 = ~50-60/Tag total
- Daily Cap: 50 (war 12)
- Initial News: 12-17 beim Start (war 5-8), Heavy Company-Bias (3 garantiert + 60% Chance)
- Spieler muss Noise von Signal trennen — echtes Trading-Skill
- Fehlende Dividend-Platzhalter ({cut_pct}, {new_div}, etc.) hinzugefügt

**Lore → Bewertung (Content-Tiefe fließt in Aktienkurse ein):**
- CEO Archetype → Drift: Visionary +0.3%/Jahr, Disruptor +0.4%, Cost-Cutter -0.05%, Steady Hand +0.05%
- CEO Archetype → Volatilität: Disruptor 1.2x, Turnaround 1.25x, Steady Hand 0.8x, Finance Vet 0.85x
- Credit Rating → Drift: AAA/AA +0.07%/Jahr Premium, BB -0.07%, B -0.2% (Junk Penalty)
- Credit Rating → Volatilität: AAA 0.85x (stabil), B 1.35x (distressed = sehr volatil)
- Credit Rating → Event-Impact: B-rated Firmen erleiden 30% stärkere negative Events, AAA nur 70%
- Alle 12 Archetypes beeinflussen Event-Sentiment (Visionary 55% positive Re-Roll, Cost-Cutter 15% negative)
- Mehr Platzhalter: {secondary_product}, {headquarters}, {credit_rating}, {ceo_archetype}, {rival}, etc.
- CEO Quotes von 3→6 pro Archetype erweitert (72 total)
- Dividend-Platzhalter ({cut_pct}, {new_div}, {yield}, {streak}, {purpose}) alle aufgelöst

### Bekannte Bugs:
- Keine bekannten Bugs

### Session 34: History Mode + Saisonalität + PDT

**SeasonalityEngine:**
- Kalender-basierte Markt-Effekte: Januar-Effekt, Sell in May, Oktober-Volatilität
- Q4 Holiday Rally (Consumer/Luxury +1.5%), Sommer-Lull (Volume -30%)
- Earnings Season (Jan/Apr/Jul/Oct → Event-Freq +50%)
- Triple Witching (3. Freitag Mar/Jun/Sep/Dec → Volume +50%, Vol +15%)
- Dividend Quarter-End, Tax-Loss Harvesting (December)
- Seasonal Headlines als News injiziert

**History Mode (9 Szenarien, 39 total):**
- Black Monday 1987, Dot-Com 2000, Financial Crisis 2008, Flash Crash 2010
- COVID 2020, GameStop 2021, Volcker Shock 1980, Oil Price War 2020, AI Bubble 202X
- Jedes mit historischem Kontext, echter Marktrendite, Force-Arc-ID
- NarrativeEngine.ForceActivateArc() aktiviert passenden Tier-4 Arc sofort

**Pattern Day Trader Rule:**
- Day-Trade Tracking: Buy+Sell am selben Tag = 1 Day-Trade
- PDT Warning wenn < $25k Equity und ≥ 4 Day-Trades in 5 Tagen
- Warning als Notification, kein Hard-Block (wie echte FINRA-Regel)

### Session 35: Supply Chain, Whisper Network, Dynamic Fundamentals

**Supply Chain System:**
- CompanyPersonality: Suppliers/Customers Listen (1-3 pro Stock)
- Sektorbasierte Zuordnung: Materials→Industrials→Consumer, Energy→Transportation, etc.
- EventEngine: Events propagieren durch Supply Chain (Customers 40%, Suppliers 25%, 1-5 Tage Delay)
- PriceEngine: Supplier-Performance beeinflusst Stock-Drift (15% Durchfluss bei >3% Moves)
- GameLoop DriftFundamentals: Supply Chain Impact auf Revenue-Drift

**Whisper Network:**
- RumorEngine: Supply Chain Whispers basierend auf Supplier/Customer-Beziehungen
- "Supply Chain Whisper: Sources at {supplier} hint at stronger output — positive for {customer}"
- 15% tägliche Chance, 3-7 Tage Lead Time, 70% Trefferquote
- Längere Lead Time als normale Rumors → Informationsvorteil für aufmerksame Spieler

**Dynamic Fundamentals Enhancement:**
- CEO-Archetype beeinflusst Revenue-Drift: Visionary +0.05%/Tag, Cost-Cutter -0.01% aber Margin+
- Turnaround Artist: beschleunigte Erholung bei negativem NetIncome
- Empire Builder: schnelleres Mitarbeiterwachstum
- Dynamische Credit Ratings: D/E > 3 → Downgrade-Risiko, positive Income → Upgrade-Chance

**Rivalry in PriceEngine:**
- RivalSymbol (existierte, war unbenutzt) → jetzt aktiv: 5% inverse Drift
- Rival hat guten Tag → leichter Headwind, Rival hat schlechten Tag → leichter Tailwind

**CEO Firing:**
- Nach 3 konsekutiven Earnings-Misses (>10% unter Erwartung) wird CEO gefeuert
- Neuer CEO-Archetype aus Turnaround/Cost-Cutter/Finance Vet/Insider/Steady Hand
- Major News Event + positiver Kursimpact (neue Hoffnung)

**FOMC Meetings:**
- Alle 30 Handelstage (≈6 Wochen) scheduled FOMC Decision Event
- Rate Hike/Cut/Hold basierend auf aktueller Policy Stance
- Statement-Nuancen: "inflation remains elevated", "downside risks have increased", etc.
- Automatische Zinsanpassung (+/- 25bp)

**Frontend:**
- Supply Chain Anzeige im Company Profile: Suppliers (blau, klickbar) + Customers (grün, klickbar)

### TODO für nächste Session (Chart Migration + Polish):
- [ ] **ECharts → TradingView Lightweight Charts** Migration (StockChart.tsx komplett neu)
  - Candlestick + Volume + SMA/EMA/Bollinger Overlays
  - RSI Sub-Chart als separates Chart-Panel
  - Bessere Performance, keine dispose/yAxis Bugs mehr
  - `lightweight-charts` ist bereits in package.json
- [ ] Shareholder Vote Response im Frontend testen (Vote YES → News Event)
- [ ] Save/Load Regression mit Supply Chain Daten verifizieren
- [ ] Long Playtest: 30+ Tage, alle neuen Features (Supply Chain, Seasonality, Elections)
- [ ] Frontend: Election Event als spezielles Banner im Dashboard

### Archiv-TODO:
- [ ] Options Tab manuell verifizieren (Chain, Greeks, Buy/Sell) — GUI-Test
- [ ] Code Signing für Installer (SmartScreen-Warnung entfernen)
- [ ] Steam Store Page vorbereiten (Screenshots, Description, Tags)
- [ ] Frontend: Commodity-Sektor in Sidebar-Gruppierung testen
- [x] Installer v0.3.0 gebaut

### Release-Checkliste:
- [x] Installer v0.2.1 gebaut (248 MB)
- [x] Automated Playtest: 18/19 bestanden (1 = erwarteter Market-Closed Reject)
- [x] Long Playtest: 7 Tage simuliert, 0 NaN, 0 Errors, Trading/Save/Load/Options OK
- [x] News-Frequenz rebalanciert (EventEngine: Macro 0.3%, Sector 0.6%, Company 0.5%, Cap 12/Tag)
- [x] GEX Spam gefixt (wurde jeden Tick gesendet statt einmal pro Tag)
- [x] Margin Cascade Spam gefixt (Cooldown 390 Ticks = 1 Spieltag)
- [x] News nach Load: letzte 10 Events aus History als initialNews mitgeschickt
- [x] Event-Frequenz rebalanciert (Macro 0.4%, Sector 0.8%, Company 0.8%)
- [x] Active UI Playtest: 16 Screenshots, News funktioniert, Dashboard/Chart/Trading OK
- [x] Installer v0.3.0 neu gebaut
- [ ] Manueller Playtest (GUI)
- [ ] SmartScreen-Workaround dokumentieren (oder Code Signing)

### Offene Punkte:
- Code Signing für Installer (SmartScreen-Warnung)
- Auto-Updater
- Steam Integration (nicht vor August 2026)
