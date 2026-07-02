# Emergent Coupling — Treiber, Kanäle, Exposure-Modell

> **Design-Landkarte für die Verallgemeinerung von M0 (Öl→Airlines) auf die ganze Welt.**
> Status (Stand 2026-07-02): **Alle 4 Kanäle gebaut & TDD-bewiesen** (622/622 grün) — `DriverExposure`-Modell +
> Kanal-Anwendung in `GameLoop.DriftFundamentals` (InputCost/OutputPrice/Demand) und `RecalculateFairValues`
> (Valuation). M0 (Öl→Airlines) hineinmigriert; Schlagzeile *derselbe Treiber, gegensätzliche Wirkung* bewiesen.
> ⚠️ **Transitorisch:** die dekorierte `GetSectorMultipliers()` läuft noch **parallel** — für gewisse Sektoren
> also leichtes Doppel-Zählen. Nächster Schritt (§8): Sektoren einzeln vom dekorierten Nudge ablösen.
> Gehört zu `WORLD_SIM_VISION.md` (§S4/S5 emergentes Pricing, §1a Leitprinzip Real-Sim).

---

## 1. Zweck

M0 hat *einen* Treiber→Sektor-Kanal emergent gemacht (Öl → Treibstoffkosten → Marge → Kurs, siehe
`FundamentalDynamics.FuelCostMarginImpact`). Breite darf **nicht** durch 14 handgeschriebene Sektor-Formeln
entstehen — das wäre der Spezialfall-Haufen, vor dem die Überlebensregel warnt. Stattdessen: **ein generelles,
daten-getriebenes Exposure-Modell**, in dem ein neuer Sektor/Treiber ein *Datensatz* ist, kein Code.

---

## 2. Die Kern-Erkenntnis: 4 Wirk-Kanäle statt 1 Multiplikator

Die heutige `GetSectorMultipliers()` drückt alles zu **einem** Drift-Multiplikator (±3% Clamp) zusammen. Real
sind es aber **vier grundverschiedene Kanäle**, über die ein Makro-Treiber eine Firma trifft. Der emergente
Realismus entsteht daraus, jeden Treiber durch den *richtigen* Kanal zu routen:

| # | Kanal | Status | Wirkt auf | Beispiel | Fundamental-Pfad |
|---|-------|--------|-----------|----------|------------------|
| 1 | **InputCost** | ✅ gebaut | Marge | Öl → Airlines (Treibstoff) | Treiber↑ → Kosten↑ → Marge↓ → NetIncome↓ |
| 2 | **OutputPrice** | ✅ gebaut | Umsatz | Öl → Energy (verkaufen Öl!); Gold → Minen | Treiber↑ → Revenue↑ |
| 3 | **Demand** | ✅ gebaut | Umsatz / Growth | Konsumklima → Consumer/Luxury; PMI → Industrials; Zins → Real Estate | Treiber → Growth-Baseline |
| 4 | **Valuation** | ✅ gebaut | PE-Multiple (nicht Earnings!) | Zins → Tech (Growth härter abgezinst) | Treiber → PE in `RecalculateFairValues` |

**Zwei Mechaniken:**
- **Change-basiert** (InputCost/OutputPrice): reagieren auf die *Tagesänderung* eines Treibers, bewegen ein
  persistentes Level (Marge bzw. Umsatz) einmal pro Move. Elastizität ≥ 0, Vorzeichen vom Kanal fix.
  Funktionen: `FundamentalDynamics.InputCostMarginDelta` / `OutputPriceRevenueDelta`.
- **Level-basiert** (Demand/Valuation): reagieren auf die *Abweichung vom Normalwert* (`EconomicEngine.GetDriverDeviation`,
  gleiche Anker wie `GetSectorMultipliers`). Demand setzt die Growth-Baseline, zu der `GrowthTrajectory` zurückkehrt;
  Valuation justiert das PE-Multiple in `RecalculateFairValues`. **Signierte Elastizität** (codiert die Richtung,
  z.B. Zins→Housing negativ). Funktionen: `DemandGrowthBaseline` / `ValuationMultipleFactor`.

Gemeinsam: `Stock.DriverExposures` (Liste `DriverExposure(Driver, Channel, Elasticity)`); Anwendung in
`GameLoop.DriftFundamentals` (Kanäle 1-3) + `RecalculateFairValues` (Kanal 4).

**Warum das zählt:** Öl↑ ist für Energy ein Segen (Kanal 3) und für Airlines ein Fluch (Kanal 1) — *derselbe
Treiber, entgegengesetzte Wirkung, verschiedene Kanäle.* Ein einzelner Sektor-Multiplikator kann das nicht
ausdrücken; deshalb fühlt er sich hohl an. `FuelCostMarginImpact` ist der erste Fall von Kanal 1.

---

## 3. Das generelle Exposure-Modell (data-driven)

Statt Formeln pro Sektor: **jede Firma trägt ein Set von Treiber-Exposures.**

```
DriverExposure {
    Driver:     string   // "Oil", "InterestRate", "Wages", ...  (Key in die Driver-Registry)
    Channel:    enum      // InputCost | Demand | OutputPrice | Valuation
    Elasticity: decimal   // Stärke; Vorzeichen ergibt sich aus Kanal + Treiberrichtung
}

// Beispiele:
Airline:        (Oil, InputCost, 0.30)
Energy-Prod.:   (Oil, OutputPrice, 0.50)
Tech-Growth:    (InterestRate, Valuation, 0.80)
REIT:           (InterestRate, Demand, 1.00)
Retail:         (Wages, InputCost, 0.15) + (ConsumerConfidence, Demand, 0.40)
```

**Tick-Anwendung (generisch, im täglichen Fundamentals-Update):**
- `InputCost`  → drückt/hebt die **Marge** (verallgemeinertes `FuelCostMarginImpact`).
- `Demand`     → biast **RevenueGrowth** (fließt via `GrowthTrajectory` in den Drift).
- `OutputPrice`→ hebt/senkt **Revenue** direkt.
- `Valuation`  → moduliert das **PE-Multiple** in `RecalculateFairValues` (trifft FairValue, nicht Earnings).

**Driver-Registry:** benannte Treiber mit aktuellem Wert + Baseline (heute in `EconomicData`). Ein neuer Treiber
(Löhne, Kupfer, …) = ein Registry-Eintrag. `FuelCostExposure` (M0) wird zum Spezialfall von `DriverExposure`
mit `Driver="Oil", Channel=InputCost`.

**Kalibrierung:** Elastizitäten an realen Sektor-Reaktionen geeicht (Leitprinzip §1a), nicht frei getunt.
Pass-Through/Clamp wie bei M0 (Fares/Hedges dämpfen kurzfristig; Ein-Schritt-Swing gedeckelt).

---

## 4. Ist-Zustand: die bestehende Kopplungs-Matrix (dekoriert)

Aus `EconomicEngine.GetSectorMultipliers()` — **das lösen wir ab.** Heute alles Kanal-agnostischer Drift-Mult,
±3% Clamp. Spalte „Kanal (soll)" = wohin es im emergenten Modell gehört.

| Sektor | Treiber (heute) | Richtung | Kanal (soll) |
|--------|-----------------|----------|--------------|
| Technology | Zins | −0,8 | Valuation |
| Financials | Zins | +0,5 | Demand (Nettozinsmarge) |
| Real Estate | Zins | −1,0 | Demand + InputCost (Zinsaufwand) |
| Utilities | Zins | −0,3 | Valuation/InputCost |
| Telecom | Zins | −0,3 | Valuation |
| Healthcare | Zins | −0,1 | Valuation (schwach) |
| Energy | Öl | +0,6 | **OutputPrice** |
| Transportation | Öl −0,4, PMI +0,3 | | **InputCost** (Öl) ✅M0 + Demand (PMI) |
| Consumer Goods | Konsumklima | +0,4 | Demand |
| Luxury Goods | Konsumklima +0,5, Arbeitslos. −0,4 | | Demand |
| Industrials | PMI | +0,5 | Demand |
| Materials | Inflation | +0,4 | OutputPrice (Preismacht) |
| Commodities | Gold +0,5, Öl +0,3 | | OutputPrice |

**Zusätzliche Overlays (existieren, berühren ebenfalls Sektoren):** `GetPolicyMultipliers` (Geldpolitik),
`GetDollarMultipliers` (DXY → Exporteure), `ElectionSectorShifts` (Politik), `EconomicCycleEngine` (Konjunktur-
phase → FairValue-Drift). Ziel: langfristig in dieselbe Exposure-Logik überführen (siehe §5).

---

## 5. Abdeckungs-Karte: drin / fehlt / bewusst später

> Lebende Landkarte, damit Lücken **explizit und benannt** sind, nicht stille Auslassungen (Überblick-Disziplin).
> „Nicht alles drin" ist der *richtige* Zustand — Vollständigkeit ist ein Daten-Problem, kein Architektur-Problem,
> *wenn* die Struktur (Driver-Registry + Exposure-Modell) offen ist.

### ✅ Drin (Treiber)
Zins, Inflation, Arbeitslosigkeit, BIP, Konsumklima, 10Y-Rendite, Öl, Gold, Housing Starts, PMI, Dollar-Index,
Geldpolitik-Stance, VIX.

### ✅ Drin (Struktur)
Konjunkturzyklus, Monetary Policy (QE/Tightening), Saisonalität, Wahlen, Supply-Chain (firmen-individuell),
Events/News (inkl. Geopolitik/M&A/Fraud), SMA-Regulierung. Assets: Aktien, Sektor-/Commodity-ETFs, Optionen.

### ✅ Neu gebaut (2026-07-02)
- **4-Kanal-Exposure-Modell** (InputCost/OutputPrice/Demand/Valuation) — der Kern dieses Dokuments.
- **Treiber-Interdependenz (Kern-Kaskade)** — `PropagateDriverCoupling` (Öl→Inflation→Zins→Wachstum→…).

### ⚠️ Fehlt (real wichtig, sollte rein)
- **Löhne / Arbeitskosten** als eigener Treiber (InputCost für arbeitsintensive Sektoren) — inkl. Löhne↔Inflation-Kopplung.
- ✅ ~~Zinsaufwand~~ **gebaut (2026-07-03):** verschuldete Firmen (aus D/E abgeleitet) haben InterestRate-InputCost →
  Zinsen hoch = Marge runter, sektor-unabhängig. Erstes **firmen-individuelles** (nicht nur sektorweites) Coupling.
- **Eigenständige Rohstoffe:** Erdgas, Kupfer, Lithium, Agrar, Uran — heute nur *abgeleitete Anzeige* aus Öl/Gold.
- **Breitere Treiber-Verflechtung** (Demand-Pull-Inflation, DXY-Kopplungen) + Erwartungs-/Überraschungs-Pricing.

### ⏭ Bewusst später (größere Brocken)
- **Anleihen/Bonds** als handelbare Asset-Klasse (10Y existiert als Zahl, nicht handelbar).
- **Devisen/FX**, **Krypto** (Bitcoin nur Anzeige), **echte Rohstoff-Kontrakte** (nur ETFs).
- **Mehrere Länder / internationale Märkte** → `WORLD_SIM_VISION.md` M3.
- **Voll ausmodellierte Firmenbilanz** (Cashflow, Capex, Buybacks/Verwässerung).
- **Sektor-Input-Output-Matrix** (Output eines Sektors = Input eines anderen; heute nur lose Supply-Chain).

---

## 6. Validierung, bevor die Abstraktion festklopft

Nicht aus einem Beispiel überverallgemeinern. Drei Kanäle an konkreten Fällen prüfen:
- **Kanal 1 InputCost:** Öl → Airlines — ✅ bewiesen (M0).
- **Kanal 2/… Demand:** Zins → Real Estate — Gegenprobe.
- **Kanal 3 OutputPrice:** Öl → Energy — Gegenprobe (muss das *entgegengesetzte* Vorzeichen zu Airlines liefern).

Erst wenn dieselbe generische Mechanik alle drei korrekt (Richtung **und** real-plausible Magnitude) liefert,
wird `DriverExposure` als Modell festgeschrieben und die Generierung bekommt Exposure-Profile pro Sektor/Subsektor
(wie M0 die Fuel-Exposure). TDD: pure Kanal-Funktionen + statistische Magnituden-Checks (§13 der Vision).

---

## 7. Realitäts-Abgleich — warum dieses Modell realistisch ist (+ 2 Vereinfachungen)

**Wir stehen strukturell auf realem Grund:**
- Die **4 Kanäle spiegeln die reale Zweiteilung jeder Bewertung**: `Preis = erwartete Cashflows / Diskontsatz`.
  InputCost/OutputPrice/Demand bewegen den **Zähler** (Earnings), Valuation den **Nenner** (Multiple). Genau die
  DCF-Logik. (Bsp.: Nasdaq 2022 −33% war fast reine Multiple-Kompression durch Zinsen → Valuation-Kanal.)
- **„Jede Firma hat Exposures zu Treibern" ist wörtlich, wie Quant-Faktormodelle** (APT, BARRA-Stil) die Welt
  beschreiben: `Rendite = Σ (Faktor-Exposure × Faktor-Move) + idiosynkratisch`. Unser `DriverExposure` = ein
  Faktor-Loading. Wir haben die reale Struktur nachgebaut, nichts Künstliches.

**Zwei bewusste Vereinfachungen gegenüber der Realität (benannt, keine blinden Flecken):**
1. **Treiber-Interdependenz** — ✅ **Kern gebaut (2026-07-02).** `EconomicEngine.PropagateDriverCoupling()` (täglich):
   die azyklische Kaskade **Öl → Inflation → Zins → Wachstum → Arbeitslosigkeit → Konsumklima/PMI** (kleine,
   gelagerte, geklammerte Tages-Pushes; acyclic ⇒ stabil, kein Runaway). *Ein* Ölschock rollt jetzt durch die
   ganze Ökonomie und trifft via unserer Kanäle automatisch Airlines/Energy (Öl), Tech-Valuation & RealEstate-Demand
   (Zins) usw. ⏭ Offen: breitere/subtilere Verflechtungen (Demand-Pull, Löhne↔Inflation, DXY) + Kausal-Event-Graph
   (Vision §S4) für diskrete Schocks.
2. **Märkte preisen Erwartungen/Überraschungen und sind vorwärtsgerichtet.** Eine erwartete Zinserhöhung bewegt
   nichts; nur die Abweichung vom Konsens zählt, und der Markt bewegt sich *vorher*. Wir reagieren auf die
   tatsächliche Bewegung im Nachhinein. Schwerer zu simulieren; später.

**Zum Doppel-Zählen (§8):** In der Realität gibt es **einen** Preis, gebildet von **einem** Markt — keine
„dekorierte + emergente" Doppelschicht. Unser paralleler Betrieb ist rein ein Migrations-Artefakt; das Ziel
(eine Bewertungsfunktion) *ist* der realistische Zustand.

**Umsetzungs-Hinweis (2026-07-02):** Beim Balance-Check aufgefallen und gefixt — `DriftFundamentals` berechnete
die Marge *nach* dem Umsatz-Update, wodurch Umsatzwachstum **earnings-neutral** blieb (Demand/OutputPrice/Growth
erreichten den Kurs nicht). Fix: Marge aus dem Pre-Drift-Zustand erfassen → Umsatz wächst bei konstanter Marge →
Earnings wachsen proportional → FairValue. Erst dadurch wirken die umsatzseitigen Kanäle end-to-end.

### ⚠️ Live-Diagnose-Befunde (2026-07-02, `RealismDiagnostic`) — offene Kalibrier-Lücken

Ein A/B-Lauf über ein Quartal (gleicher Seed, mit/ohne anhaltenden Ölschock) hat zwei Magnituden-Probleme
aufgedeckt — die Kanäle stimmen in Isolation (Unit-Tests grün), aber im **System** sind die Magnituden unrealistisch:

1. **Befund A (behoben): Kosten-Kanäle waren change-basiert → feuerten bei anhaltendem Level nicht.**
   `InputCost/OutputPrice` reagierten auf die Öl-*Tagesänderung*; ein konstant hoher Ölpreis hat Änderung ≈ 0 →
   Hit feuerte nie. **✅ Fix umgesetzt:** beide Kanäle jetzt **level-basiert** (`InputCostMarginLevel`/
   `OutputPriceMarginLevel`, auf Öl-Abweichung), pro Tag aus stabiler Marge via Swap gesetzt (nicht akkumuliert).
   Damit sind **alle 4 Kanäle level-basiert**; die change-Maschinerie (`_previousDriverValues`) ist raus. OutputPrice
   wirkt jetzt auf die Marge (erreicht Earnings), nicht mehr auf reinen Umsatz. Unit-Tests grün.

2. 🔴 **Befund B — BESTÄTIGT (echter Bug): der 16:00-Tages-Block ist toter Code.** `GameTime` startet exakt
   9:00:00 und tickt +1 Min → trifft 16:00:00. Aber bei 16:00:00 ist `IsMarketOpen()` FALSE (`time < 16:00`) und
   `IsAfterHours()` TRUE (`time >= 16:00`), also feuert der After-Hours-Early-`return` (GameLoop ~402/419, Kommentar
   sagt selbst „no daily processing") **vor** dem Tages-Block (~667). Der enthält `DriftFundamentals` (Drift + alle
   4 Kanäle), `EarningsEngine`, `DividendEngine`, DailyHistory-Kerzen, Persona-Evolution, Reputation, Steuern …
   **→ die komplette tägliche Fundamentaldaten-Schicht läuft im Spiel NIE.** Bewiesen: Airline-Revenue byte-identisch
   mit Start über 60 Tage (Revenue wird nur dort verändert). NI ändert sich nur über Events (laufen in Marktzeit).

   **Einzeiler-Fix** (16:00:00 vom After-Hours-Block ausnehmen) aktiviert den Block — verifiziert: Revenue bewegt
   sich dann, der Ölschock trifft die Earnings. **ABER:** die Tages-Dynamik ist über viele Tage **grob instabil.**

   **Instabilitäts-Mechanismus (Tag-für-Tag-Trace, `DailyBlockInstability_Trace`):** die **FairValue macht große,
   ungedämpfte Sprünge** (Bsp. FLXH: FV 178→314 = +76% an *einem* Tag), dann jagt der Kurs ihr hinterher →
   Divergenz (je nach Firma nach oben weggelaufen *oder* auf ~0,01 kollabiert). Ein +76%-Tagessprung überschreitet
   den ±30%-Clamp von `RecalculateFairValues` bei weitem → es gibt **mehrere unkoordinierte FairValue-Schreiber**
   im Tages-Block (u.a. `EarningsEngine` setzt `FairValue = eps × 18` **ungeclampt**), die nie zusammen liefen und
   sich jetzt bekriegen. Die Tages-Schicht ist schlicht nie als laufendes System abgestimmt worden.

   **Stabilisierungs-Fortschritt (2026-07-02/03): 2 FairValue-Fixes umgesetzt → Block ist jetzt AKTIVIERBAR.**
   - **(i) FairValue-Pfad vereinheitlicht:** `EarningsEngine` hard-setzte `FairValue = eps×18` ungeclampt → entfernt.
     FairValue gehört jetzt allein `RecalculateFairValues` (geclampt ±30%/Tag, 5%/Tag-Blend). Trace danach: FV glatt
     (+1,5%/Tag statt +76%-Sprung), Kurs klebt an FV, **kein Kollaps/Runaway.** (Test `ReleasedEarnings_*` auf
     Fundamentaldaten umgestellt.)
   - **(ii) Unprofitable erodieren statt einzufrieren:** `RecalculateFairValues` fror bei Verlust die FairValue ein
     → ein vom Öl in die Verlustzone gedrückter Carrier behielt hohen Kurs (invertierter Effekt). Jetzt −1%/Tag-Erosion
     bei Verlust → **Öl-Effekt richtungsrichtig** (Käufer relativ schlechter als Verkäufer).
   - **Verifiziert:** Block temporär aktiviert → volle Suite nur **1 betroffener Test** (der Earnings-Test, umgestellt),
     Öl-A/B stabil + direktional korrekt. **Aktivierung selbst (Einzeiler) zurückgerollt** — bleibt Stufe-3-Entscheidung.

   **✅ SCHARFGESCHALTET (2026-07-03):** der Einzeiler (16:00:00 vom After-Hours ausnehmen) ist **permanent aktiv** —
   die ganze Tages-Schicht (DriftFundamentals + 4 Kanäle + Earnings + Dividenden + Chart-Kerzen) läuft jetzt im Spiel.
   Suite 625 grün, Live-Öl-A/B stabil + richtungsrichtig.

   **✅ Bärisch-Skew gefixt (2026-07-03):** 1-Jahres-Playtest zeigte erst Median −50%/Jahr. Instrumentierung
   (Price vs FairValue vs Earnings) → Earnings +5%, aber FairValue −51% → **Generierungs-Inkonsistenz**: generiertes
   KGV ~27 vs. Sektor-KGV ~18 → Massen-Repricing bei Block-Aktivierung. Fix: NetIncome bei Generierung aus
   `MarketCap / SectorPE()` (gemeinsamer Helper, ±30% Dispersion). Playtest danach: **Price median −2,9% / avg +3,9%,
   Earnings +3%, FairValue +3% — realistisch.** (Bonus: latenter Supply-Chain-Bidirektionalitäts-Bug mitgefixt.)

   **✅ Magnituden-Fine-Tune (2026-07-03):** Unprofitable-Erosion jetzt **proportional zur Verlust-Tiefe** (statt flach
   1%/Tag) — tiefe Verluste fallen hart, marginale driften. Öl-A/B: **Käufer −14%, Verkäufer +2%, Spread +16%**
   (realistisch); Playtest Median −1%, keine Kollapse. Beide Guards grün. **Offen:** nur noch echtes GUI-Feintuning.
   (frühere Notiz: klein vs. breitem
   Bärisch-Skew — Mean-Reversion/Event-Symmetrie). ⚠️ History-Frage bleibt: war der Block in v0.3.0 schon tot?
   Re-Messung: `RealismDiagnostic` (skipped) + temp-Aktivierung.

3. **Sekundär (nach B):** selbst mit laufendem Block Fundamentaldaten→Preis-Dämpfung (FairValue-Blend 5%/Tag +
   schwache Mean-Reversion) + Bärisch-Skew + eben die Instabilität. Alles Teil des Stabilisierungs-Passes.
2. **Breiter Bärisch-Skew.** In einem zufälligen 60-Tage-Fenster waren fast alle Sektoren −10% bis −28%
   (Transportation −25,6%, Energy −28,2%). Ein zufälliges Quartal sollte kein breiter Crash sein — deckt sich mit
   dem bekannten Magnituden-Skew aus Session 37 (negative Templates heftiger).

**Konsequenz:** Bevor mehr Kanäle/Treiber draufkommen, braucht es einen **Transmissions-/Balance-Kalibrier-Schritt**
(Mean-Reversion-Stärke, FairValue-Anpassungstempo, Event-Magnituden-Symmetrie). Das berührt Kern-Preisdynamik +
den Feel des released Spiels → Stufe-2-Entscheidung, mit dem User abzustimmen. `RealismDiagnostic` bleibt als Guard.

## 8. Migrationspfad (dekoriert → emergent)

1. ✅ `DriverExposure` + `Channel`-Enum + Driver-Registry/Normalisierung (`EconomicEngine.GetDriverValue`/`GetDriverDeviation`).
2. ✅ Generische Kanal-Anwendung (alle 4 Kanäle) in `DriftFundamentals` + `RecalculateFairValues`.
3. ✅ Exposure-Profile bei der Generierung — **fast alle Sektoren abgedeckt (2026-07-03):** Transportation (Öl-InputCost),
   Energy/Oil&Gas (Öl-OutputPrice), Consumer/Luxury (Konsumklima-Demand), Industrials (PMI-Demand), Real Estate (Zins-Demand),
   Technology/Utilities/Telecom (Zins-Valuation), Financials (Zins-Demand/NIM), Materials (Inflation-OutputPrice + Mining→Gold).
   Playtest bestätigt: Markt bleibt realistisch (Median ~flat, keine Kollapse). Nur Healthcare bewusst offen (real fast zins-immun).
4. ✅ **ERLEDIGT (2026-07-03): dekorierte `GetSectorMultipliers` aus dem Pricing entfernt.** Kein Doppel-Zählen mehr —
   Sektor×Treiber-Effekte sind jetzt **allein emergent** (DriverExposures). Konkret raus: der `mult`-Faktor in
   `PriceEngine.SectorMultipliers` (ExecuteTick) + der Sektor-Nudge in `DriftFundamentals`. `GetSectorMultipliers` selbst
   bleibt nur noch für die UI-Anzeige (`DataQueryHandler`). Messlatten grün: Öl-Effekt sogar sauberer (Spread +23%),
   Markt realistisch (Median ~−5%, keine Kollapse). **Ein Bewertungsmodell statt zwei — wie in §7 (Realität).**
5. ⏭ Overlays (Policy/DXY/Election/Zyklus) **laufen noch dekorativ** (Policy/DXY-Mults auf ONNX-Drift + Zyklus auf
   FairValue-Drift). Zuletzt in dieselbe Logik ziehen — oder als bewusst dekorative Schicht
   behalten, falls emergent nicht lohnt (pro Overlay entscheiden).
