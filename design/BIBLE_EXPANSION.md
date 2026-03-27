# StockSim — Bible Expansion: Modi, Content, Education (ARCHIV)

> **VERALTET** — CompanyPersonality (Sektion 1) ist implementiert. Modi und Content-Pläne wurden in `MASTER_ROADMAP.md` migriert und durch das AI Event System ersetzt/erweitert.
> Dieses Dokument wird nur noch als historische Referenz aufbewahrt.
>
> Aktiver Plan: `design/MASTER_ROADMAP.md` + `design/AI_EVENT_SYSTEM.md`
>
> Ergänzung zur Game Design Bible. Gleicher Detailgrad. Alles hier ist verbindliche Spec.

---

## 1. FIRMEN-PERSÖNLICHKEIT & ZUGESCHNITTENE EVENTS

### 1.1 Design-Philosophie

Jede Firma muss sich **einzigartig anfühlen**. Der Spieler soll Firmen lieben und hassen lernen. Nicht "Stock #147 in Technology", sondern "VertexAI — die arrogante KI-Firma deren CEO ständig auf Twitter provoziert, deren Produkte aber brillant sind."

**Inspiration:**
- **Football Manager**: Jeder Spieler hat Persönlichkeit, History, Stärken/Schwächen
- **Crusader Kings**: Charaktere mit Traits die Events triggern
- **Rimworld**: Colonists mit Backstories die Gameplay beeinflussen

### 1.2 Firmen-Persönlichkeits-System

Jede Firma bekommt bei Generierung ein `CompanyPersonality` Objekt:

```
CompanyPersonality {
  // Identität
  FoundedYear: int (1970-2025, seed-basiert)
  FounderName: string (generiert)
  CEOName: string (generiert)
  CEOPersonality: enum (Visionary, Conservative, Aggressive, Scandal-Prone, Beloved, Eccentric)
  Headquarters: string ("San Francisco, CA" / "Austin, TX" / "New York, NY" etc.)
  EmployeeCount: int (abgeleitet von MarketCap)

  // Geschichte (3 Sätze, Template-basiert pro Sektor)
  BackstoryTemplate: int (1-10 pro Sektor = 120 Varianten)
  // Beispiel Tech: "Founded in {year} by {founder}, {company} started as a {origin}
  //   and grew into a {currentState} with {revenue} in annual revenue.
  //   Known for {reputation}, the company {recentEvent}."

  // Persönlichkeit (beeinflusst welche Events feuern)
  InnovationScore: float (0-1) — Wie oft Produkt-Events feuern
  StabilityScore: float (0-1) — Wie oft Management-Krisen passieren
  EthicsScore: float (0-1) — Wie oft Skandale/ESG-Events passieren
  AggressivenessScore: float (0-1) — Wie oft M&A/Expansion-Events feuern
  MediaPresence: float (0-1) — Wie oft die Firma in News erscheint

  // Beziehungen
  Rivals: string[] (2-3 Firmen im gleichen Sektor)
  Partners: string[] (1-2 Firmen, auch cross-sector)

  // Produkte (2-4 pro Firma)
  Products: ProductInfo[] {
    Name: string ("CloudSync Pro", "MediScan AI", "GreenCharge Battery")
    Category: string ("Enterprise SaaS", "Consumer Device", "API Platform")
    MarketShare: float (0-0.4)
    LaunchYear: int
  }
}
```

### 1.3 CEO-Persönlichkeitstypen

| Typ | Beschreibung | Event-Tendenz | Beispiel (real) |
|-----|-------------|---------------|-----------------|
| **Visionary** | Großspurige Versprechen, polarisiert, treibt Innovation | Produkt-Launches häufiger (+50%), aber auch mehr Enttäuschungen | Elon Musk |
| **Conservative** | Stabil, langweilig, zuverlässig | Weniger Events, dafür stetigeres Wachstum, selten Skandale | Warren Buffett |
| **Aggressive** | M&A-hungrig, wachstum um jeden Preis | M&A-Events 3x häufiger, hohe Verschuldungsrisiken | Carl Icahn |
| **Scandal-Prone** | Charismatisch aber ethisch fragwürdig | ESG/Legal-Events 3x häufiger, hohe Volatilität | Adam Neumann |
| **Beloved** | Mitarbeiter lieben ihn/sie, öffentliches Vertrauen | Crash-Resistenz (+20%), selten negative News | Satya Nadella |
| **Eccentric** | Unberechenbar, genial oder wahnsinnig | Extreme Events in beide Richtungen, Surprise-Faktor | John McAfee |

### 1.4 Zugeschnittene Event-Templates (Firmen-spezifisch)

**Pro Sektor 15-20 firmenspezifische Event-Templates.**
Events referenzieren CEO-Namen, Produkte, Rivalen, Firmennamen.

#### Technology Events (20 Templates):

```
1. PRODUKT-LAUNCH (InnovationScore > 0.6)
   "{Company} unveils {Product.Name} 2.0 at annual developer conference.
    CEO {CEO} calls it 'a paradigm shift in {Product.Category}.'
    Early reviews are {positive|mixed|negative}."
   → Positiv: +5-10%, Negativ: -3-5%

2. SECURITY BREACH (EthicsScore < 0.5)
   "{Company} discloses data breach affecting {1-50}M {Product.Name} users.
    CEO {CEO} apologizes, promises 'comprehensive security overhaul.'
    Regulatory investigation expected."
   → -4 bis -12%, SpreadMultiplier 3x

3. RIVAL POACHING (hat Rivals)
   "{Rival} hires {Company}'s head of {engineering|AI|product},
    raising concerns about talent drain at {Company}.
    CEO {CEO} reportedly 'furious' about the departure."
   → -2 bis -4%

4. KEYNOTE DISASTER (CEO = Visionary, chance 5%)
   "CEO {CEO}'s product demo at {Conference} fails live on stage.
    Social media erupts. '{Company} just pulled an Apple Maps moment.'"
   → -3 bis -8%, Recovery nach 2-3 Tagen

5. VIRAL PRODUCT (InnovationScore > 0.7, chance 3%)
   "{Product.Name} goes viral on social media. Downloads surge {200-500}%
    in 48 hours. Analysts scramble to upgrade {Symbol}."
   → +8 bis +20%, Volume 5x

6. ANTITRUST INVESTIGATION (MarketCap > $50B)
   "Department of Justice opens antitrust investigation into {Company}'s
    dominance in {Product.Category}. CEO {CEO} says company 'welcomes scrutiny.'"
   → -5 bis -15%, dauert 30-90 Tage

7. AI BREAKTHROUGH (Sektor = Technology)
   "{Company} publishes research paper claiming breakthrough in {GeneralAI|
    QuantumComputing|NeuralInterface}. Scientists debate significance."
   → +5 bis +15% für die Firma, +2-3% für Tech-Sektor

8. EARNINGS LEAK (MediaPresence > 0.7)
   "Unverified social media post suggests {Company} will {beat|miss}
    earnings expectations. Trading volume spikes ahead of official report."
   → Rumor-Event, +/-3-5% sofort, bestätigt sich in 80% der Fälle

9. CEO TWEET CONTROVERSY (CEO = Eccentric|Visionary)
   "CEO {CEO} tweets '{provocativeStatement}', sparking backlash.
    Board reportedly considering 'social media policy' for executives."
   → -2 bis -5%, Recovery nach 1 Tag

10. PATENT TROLL (hat Products mit hoher MarketShare)
    "Patent holding company sues {Company} for ${50-500}M over alleged
     infringement in {Product.Name}. Analysts expect settlement."
    → -1 bis -3%

11. STRATEGIC PARTNERSHIP (hat Partners)
    "{Company} announces strategic partnership with {Partner} to develop
     {jointProduct}. CEO {CEO}: 'This combines our strengths in {area}.'"
    → +3 bis +6% für beide Firmen

12. MASS LAYOFFS (StabilityScore < 0.4)
    "{Company} announces {5-25}% workforce reduction. {500-10000} employees
     affected. CEO {CEO} cites 'need to refocus on core business.'"
    → -3 bis -6% kurzfristig, +2-4% langfristig (Kostensenkung)

13. WHISTLEBLOWER (EthicsScore < 0.3)
    "Former {Company} employee files whistleblower complaint alleging
     {dataManipulation|fraudulentAccounting|safetyViolations}.
     Company denies allegations."
    → -8 bis -20%, Investigation Event folgt in 30 Tagen

14. ACTIVIST INVESTOR (MarketCap $5-50B)
    "Activist investor {ActivistName} acquires {5-9}% stake in {Company}.
     Demands board seats, strategic review, potential sale of {division}."
    → +5 bis +15% (Übernahme-Spekulation)

15. SERVER OUTAGE (hat SaaS/Cloud Product)
    "{Product.Name} experiences {2-12} hour global outage.
     Enterprise clients report significant business disruption.
     CEO {CEO} issues public apology."
    → -2 bis -5%, Competitor +1-2%

16. GOVERNMENT CONTRACT (AggressivenessScore > 0.6)
    "{Company} wins ${100M-5B} government contract for {projectType}.
     Shares surge as analysts raise revenue forecasts."
    → +5 bis +12%

17. STOCK SPLIT ANNOUNCEMENT
    "{Company} announces {2|3|4|5}-for-1 stock split effective {date}.
     CEO {CEO}: 'Making our shares accessible to more investors.'"
    → +3 bis +8% (Split-Euphorie)

18. INSIDER SELLING SPIKE (CEO personality egal)
    "SEC filings reveal {Company} CEO {CEO} sold ${1-50}M in shares
     last week. Company says sales were 'pre-planned under 10b5-1.'"
    → -2 bis -5%

19. SUBSCRIPTION GROWTH (hat recurring revenue Product)
    "{Company} reports {Product.Name} subscribers grew {15-40}% YoY
     to {X}M users. Average revenue per user up {5-15}%."
    → +6 bis +15%

20. COMPETITOR COLLAPSE (hat Rivals)
    "{Rival} files for bankruptcy. {Company} expected to capture
     significant market share. Analysts call it 'transformative.'"
    → +8 bis +20%, Rival -80 bis -95%
```

#### Healthcare Events (15 Templates):
```
1. FDA APPROVAL — "{Product.Name} receives FDA approval for {indication}"
2. CLINICAL TRIAL FAILURE — "Phase {2|3} trial for {Product.Name} fails primary endpoint"
3. DRUG RECALL — "FDA orders recall of {Product.Name} after {adverseEvents} reports"
4. PANDEMIC BENEFICIARY — "{Company} sees demand surge for {Product.Name}"
5. PATENT CLIFF — "{Product.Name} patent expires, generic competition expected"
6. PIPELINE BREAKTHROUGH — "Preclinical data for {newDrug} shows {promising} results"
7. PRICING SCANDAL — "Congressional hearing targets {Company}'s {Product.Name} pricing"
8. MERGER SYNERGY — "Post-merger integration of {Acquired} on track, CEO {CEO} says"
9. BIOTECH BUYOUT RUMOR — "Sources say {LargePharma} exploring acquisition of {Company}"
10. INSURANCE COVERAGE EXPANSION — "Major insurers add {Product.Name} to formulary"
11. MANUFACTURING ISSUE — "FDA warns of quality control issues at {Company}'s {City} facility"
12. CELEBRITY ENDORSEMENT — "Celebrity reveals using {Product.Name}, stock surges"
13. OPIOID SETTLEMENT — "{Company} agrees to ${X}B settlement in opioid litigation"
14. BREAKTHROUGH THERAPY — "FDA grants Breakthrough Therapy designation to {Product.Name}"
15. CEO HEALTH SCARE — "CEO {CEO} takes medical leave; interim leadership announced"
```

#### Energy Events (15 Templates):
```
1. OIL DISCOVERY — "{Company} announces significant oil discovery in {Region}"
2. REFINERY EXPLOSION — "Explosion at {Company}'s {City} refinery, {X} workers injured"
3. OPEC DECISION — "OPEC+ production cut boosts outlook for {Company}"
4. PIPELINE CONTROVERSY — "{Company}'s {PipelineName} faces environmental protests"
5. RENEWABLE PIVOT — "CEO {CEO} announces ${X}B investment in renewable energy"
6. SPILL DISASTER — "{Company} faces ${X}B cleanup costs after {Region} oil spill"
7. LNG CONTRACT — "{Company} signs 20-year LNG supply deal with {AsianCountry}"
8. CARBON TAX IMPACT — "New carbon tax expected to cost {Company} ${X}M annually"
9. DRILLING BAN — "Government moratorium on {offshore|fracking} impacts {Company}"
10. ENERGY CRISIS — "European energy crisis drives record profits for {Company}"
11. SOLAR BREAKTHROUGH — "{Company}'s new solar panel achieves {X}% efficiency record"
12. GRID FAILURE — "{Company}'s grid failure affects {X}M customers in {Region}"
13. NUCLEAR RESTART — "{Company} receives approval to restart {PlantName} reactor"
14. EV CHARGING DEAL — "{Company} partners with {AutoMaker} for EV charging network"
15. DIVIDEND HIKE — "{Company} raises dividend {15-30}%, signals confidence"
```

#### Financials Events (15 Templates):
```
1. STRESS TEST PASS — "{Company} passes Federal Reserve stress test with flying colors"
2. STRESS TEST FAIL — "{Company} fails stress test, forced to halt buybacks and dividends"
3. TRADING DESK BLOWUP — "Rogue trader loses ${X}B at {Company}. CEO {CEO} orders review"
4. FINTECH DISRUPTION — "{FinTech} threatens {Company}'s {Product.Name} with zero-fee alternative"
5. RATE HIKE WINDFALL — "{Company} reports record net interest income from rate hikes"
6. MORTGAGE CRISIS — "{Company}'s mortgage portfolio shows {X}% delinquency rate"
7. CRYPTO EXPOSURE — "{Company} reveals ${X}B exposure to crypto assets, sparking concern"
8. MONEY LAUNDERING — "Regulators fine {Company} ${X}M for anti-money laundering failures"
9. WEALTH MANAGEMENT GROWTH — "{Company}'s wealth management division crosses ${X}T AUM"
10. BANK RUN FEARS — "Social media rumors trigger deposit outflows from {Company}"
11. IPO UNDERWRITING — "{Company} leads IPO of {TechUnicorn}, earning ${X}M in fees"
12. CREDIT DOWNGRADE — "Moody's downgrades {Company}'s credit rating to {Rating}"
13. SHAREHOLDER REVOLT — "Shareholders reject CEO {CEO}'s ${X}M compensation package"
14. BRANCH CLOSURES — "{Company} announces closure of {X} branches, shifting to digital"
15. ACQUISITION SPREE — "{Company} acquires {X}th company this year, raises integration concerns"
```

*[Consumer, Industrials, Materials, Real Estate, Telecom, Utilities, Luxury, Transportation: jeweils 15 Templates nach gleichem Schema — insgesamt 180 neue sektorspezifische Templates]*

### 1.5 Event-Ketten (Multi-Stage Events)

Events sind keine Einzelereignisse. Sie haben Konsequenzen.

**Kette 1: FDA Trial Lifecycle (Healthcare)**
```
Stage 1 (Tag 0): "Trial Announcement"
  "{Company} announces Phase {2|3} clinical trial for {Drug}
   targeting {Disease}. Results expected in {60-120} days."
  → Stock +3-8% (Hoffnung)
  → Volatilität steigt

Stage 2 (Tag 60-120): "Trial Results"
  WENN Erfolg (60% Chance):
    "{Drug} meets primary endpoint in Phase {2|3} trial.
     Efficacy rate of {65-92}%, safety profile 'favorable.'"
    → Stock +20-40%
    → WEITER zu Stage 3a

  WENN Misserfolg (40% Chance):
    "{Drug} fails to meet primary endpoint. {Company} says
     'disappointed but committed to alternative approaches.'"
    → Stock -25-50%
    → WEITER zu Stage 3b

Stage 3a (Tag +30): "FDA Submission"
  "{Company} submits {Drug} NDA to FDA. PDUFA date set for {date}."
  → Stock +2-5%
  → WEITER zu Stage 4

Stage 3b (Tag +60): "Aftermath"
  50%: "Pipeline Review" — Company shifts focus, slight recovery
  30%: "CEO Resigns" — Leadership change, uncertainty
  20%: "Acquisition Target" — Larger pharma approaches

Stage 4 (Tag +90): "FDA Decision"
  WENN Approval (70% nach erfolgreicher Trial):
    "FDA approves {Drug} for {Disease}. {Company} to begin
     commercialization Q{X}."
    → Stock +15-30%

  WENN Rejection (30%):
    "FDA issues Complete Response Letter for {Drug}, requesting
     additional data on {safetySignal}."
    → Stock -20-35%
```

**Kette 2: Corporate Scandal → Investigation → Resolution**
```
Stage 1 (Tag 0): "Whistleblower/Report"
  "Former employee alleges {Company} {fraudType}."
  → Stock -8-15%

Stage 2 (Tag 5-15): "Media Frenzy"
  "Multiple outlets confirm allegations. CEO {CEO} denies wrongdoing.
   Board hires independent investigators."
  → Stock -5-10% (additional)
  → Short Interest steigt

Stage 3 (Tag 30-60): "Investigation"
  "{SEC|DOJ} opens formal investigation into {Company}.
   Trading volume surges as shorts pile in."
  → Stock -3-5% (additional)
  → SMA Score des Spielers steigt wenn er vorher verkauft hat (Insider Trading)

Stage 4 (Tag 90-180): "Resolution"
  40%: Settlement — "${X}M fine, no admission of wrongdoing. Stock recovers 50%"
  30%: CEO Fired — "Board removes CEO {CEO}. New leadership. Slow recovery"
  20%: Cleared — "Investigation finds no wrongdoing. Short squeeze, +30-50%"
  10%: Criminal Charges — "CEO {CEO} indicted. Stock -30%, delisting possible"
```

**Kette 3: M&A Saga (alle Sektoren)**
```
Stage 1: "Acquisition Rumor"
  (Rumor-Event, 80% wahr)
  → Target +5-10% auf Spekulation

Stage 2 (Tag 5-15): "Official Bid"
  "{Acquirer} offers ${Price}/share for {Target} ({Premium}% premium)"
  → Target springt auf nahe Offer-Preis
  → Acquirer -3-5%
  → Tender Offer Popup für Spieler

Stage 3 (Tag 15-45): "Regulatory Review"
  → Spieler muss entscheiden: Halten (auf Deal-Abschluss warten) oder Verkaufen

Stage 4 (Tag 45-90): "Resolution"
  50%: Deal Approved — Target zum Offer-Preis delisted
  25%: Higher Bid — Competing Offer, Target +10-15%
  15%: Deal Blocked — Target -20-30%, Acquirer +3-5%
  10%: Target Walks Away — "Poison Pill defense", Target -10%

Stage 5 (nur bei Higher Bid): "Bidding War"
  2-3 Runden höherer Gebote. Target kann +50-100% über Ursprungspreis gehen.
```

**Kette 4: Wirtschaftskrise (Macro → Sector → Company)**
```
Stage 1: "Warning Signs"
  "Yield curve inverts for first time since {year}. Recession fears mount."
  → Markt -1-2%, Financials -3-5%

Stage 2 (Tag 30-60): "Data Deterioration"
  "GDP contracts {-0.5 bis -2.0}%. Unemployment rises to {4.5-6.0}%."
  → Markt -5-10%, Cyclicals -15-20%

Stage 3 (Tag 60-120): "Crisis Event"
  "Major {bank|corporation} fails. Contagion fears spread."
  → Markt -10-20%, Financials -25-40%, Flight to Utilities/Healthcare

Stage 4 (Tag 120-240): "Policy Response"
  "Federal Reserve cuts rates by {50-100}bp. Government announces
   ${X}T stimulus package."
  → Markt stabilisiert, Tech +10-20%, Recovery beginnt

Stage 5 (Tag 240-360): "Recovery"
  "Markets enter recovery phase. 'Green shoots' visible in economic data."
  → Bull Market beginnt, stärkste Gains in most-beaten-down Sektoren
```

### 1.6 Firmen-Rivalitäten

Firmen im gleichen Sektor haben **Rivalitäten** die Events beeinflussen:

```
Wenn Rival ein positives Event hat:
  → Deine Firma -1-3% (Competitive Concern)
  → News: "Investors worry {Company} falling behind {Rival} in {area}"

Wenn Rival ein negatives Event hat:
  → Deine Firma +1-3% (Market Share Gewinne)
  → News: "{Company} seen as beneficiary of {Rival}'s troubles"

Wenn Rival übernommen wird:
  → Deine Firma +5-10% (weniger Wettbewerb)

Wenn Rival bankrott geht:
  → Deine Firma +10-20% (Markt-Dominanz)
```

### 1.7 Firma des Tages / Trending Companies

**Jeden Spieltag** wird 1-3 Firmen als "Trending" markiert:
- Höheres News-Volumen
- Stärkere Preisbewegungen
- Mehr AI-Trader-Aktivität
- Im UI: 🔥 Badge neben Symbol

---

## 2. INCREMENTAL/TYCOON MODE — Vollständiges Design

### 2.1 Design-Philosophie

**Inspiration:**
- **Cookie Clicker**: Einfacher Loop, exponentielles Wachstum, Prestige
- **Adventure Capitalist**: Business-Upgrades, Manager, Offline-Earnings
- **Idle Miner Tycoon**: Visuelles Upgrade-System (Büro wächst)
- **Melvor Idle**: Tiefes Skill-System hinter einfacher Oberfläche
- **Factorio**: "Just one more optimization" — endloser Verbesserungsdrang

**Kern-Loop:**
```
Trade → Profit → Upgrade → Trade effizienter → Mehr Profit → Bessere Upgrades
                    ↑                                              ↓
                    └──── PRESTIGE (Reset mit permanentem Bonus) ←──┘
```

**Spieldauer pro Run: 3-6 Stunden**
**Prestige-Runs: 5-10 für "Completion"**
**Gesamtspielzeit: 30-60 Stunden für 100%**

### 2.2 Phasen-System (5 Phasen)

#### Phase 1: Garage Trader ($1.000 → $50.000)

**Setting:** Dein Schlafzimmer. Ein alter Laptop. Ein Traum.

**Verfügbar:**
- 10 Aktien (handverlesen: 5 bekannte Blue Chips + 5 volatile Small Caps)
- Nur Market Orders
- Kein Chart (nur Preisliste mit grün/rot Pfeilen)
- Keine Indikatoren
- Keine News (nur Preisbewegungen sehen)
- 1x Speed only
- Keine Shortcuts

**Upgrades (kaufbar mit Profit):**

| Upgrade | Kosten | Effekt |
|---------|--------|--------|
| Gebrauchter Monitor | $500 | Line-Chart freischalten |
| Zeitungsabo | $1.000 | News-Ticker (nur Major Events) |
| Online-Broker-Upgrade | $2.000 | Limit Orders freischalten |
| WiFi-Upgrade | $3.000 | Preise updaten 2x schneller |
| Kaffeemaschine | $500 | 2x Speed freischalten |
| Trading for Dummies (Buch) | $200 | Tooltip-Hilfe freischalten |
| Zweiter Monitor | $5.000 | Candlestick-Chart freischalten |
| Reddit Premium | $1.000 | Gelegentliche "Hot Tips" (Rumors) |

**Meilenstein: $50.000** → Cutscene: "Du kündigst deinen Job. Zeit für Phase 2."

#### Phase 2: Apartment Trader ($50.000 → $500.000)

**Setting:** Ein aufgeräumtes Apartment. Schreibtisch mit zwei Monitoren. Kaffeetasse.

**Neu verfügbar:**
- 50 Aktien (alle Sektoren vertreten)
- Alle Basis-Ordertypen (Market, Limit, Stop)
- Candlestick-Charts mit Zeitrahmen
- News-Ticker (alle Severity-Level)
- Bis 5x Speed

**Upgrades:**

| Upgrade | Kosten | Effekt |
|---------|--------|--------|
| Bloomberg Terminal Lite | $15.000 | SMA-20 Indikator |
| Trading-Kurs | $5.000 | RSI-Indikator |
| Junior Analyst (Freelancer) | $20.000 | 1 Tipp pro Woche (Sektor-Trend) |
| Margin-Konto beantragen | $10.000 | Margin Trading (2x Leverage) |
| Short-Selling-Zulassung | $25.000 | Short Selling freischalten |
| Noise-Cancelling Kopfhörer | $2.000 | Konzentration: -10% Slippage |
| Dreifach-Monitor Setup | $8.000 | Orderbook-Ansicht freischalten |
| Finanz-Podcasts | $3.000 | Macro-Events 30 Min früher sehen |
| Premium Daten-Feed | $12.000 | 10x Speed freischalten |

**Meilenstein: $500.000** → "Du mietest ein Büro. Erste Angestellte."

#### Phase 3: Office Manager ($500.000 → $10.000.000)

**Setting:** Kleines Büro. 4-5 Schreibtische. Glaswand mit Blick auf die Skyline.

**Neu verfügbar:**
- 150 Aktien + ETFs
- OCO Orders, Trailing Stop
- Alle Indikatoren (MACD, Bollinger, VWAP)
- After-Hours Trading

**Upgrades:**

| Upgrade | Kosten | Effekt |
|---------|--------|--------|
| Senior Analyst | $100.000 | 3 Tipps/Woche + Sector Rotation Alerts |
| Quant Developer | $200.000 | Auto-Stop-Loss Empfehlungen |
| Risk Manager | $150.000 | Portfolio-Risiko-Bericht täglich |
| Compliance Officer | $80.000 | SMA-Strafen -30% |
| Bloomberg Terminal Pro | $50.000 | ALLE Indikatoren + Compare-Chart |
| Dedicated Server | $100.000 | Fills sind 20% besser (weniger Slippage) |
| Firmen-Rechtsanwalt | $120.000 | Tender Offer Tipps + Merger Arb Signale |
| Nachrichtendienst-Abo | $75.000 | Alle Rumors 2 Tage früher |
| AI Trading Bot v1.0 | $500.000 | Automatisiert 1 Strategie (z.B. "Buy Dips in Tech") |
| Hedge Fund Lizenz | $1.000.000 | Freischaltet Phase 4 ohne $10M |

**Meilenstein: $10.000.000** → "Du gründest einen Hedge Fund."

#### Phase 4: Hedge Fund ($10M → $1B)

**Setting:** Trading Floor. 20+ Bildschirme. Hektik. Bloomberg-Terminals überall.

**Neue Mechaniken:**
- **AUM (Assets Under Management)**: Investoren geben dir Geld
  - Start: $50M AUM (dein eigenes + Investoren)
  - Wächst wenn Performance gut, schrumpft bei Verlusten
  - **Management Fee**: 2% p.a. (passives Einkommen!)
  - **Performance Fee**: 20% der Gewinne über Benchmark
  - **Investor Patience**: Investoren ziehen nach 2 Quartalen Underperformance ab
- **Benchmark**: Du musst den S&P 500 (SIMX ETF) schlagen
- 263+ Aktien (alles verfügbar)

**Upgrades:**

| Upgrade | Kosten | Effekt |
|---------|--------|--------|
| Investor Relations Team | $2M | Investoren ziehen langsamer ab |
| Quantitative Research Abteilung | $5M | AI Bot v2.0 (3 Strategien parallel) |
| Proprietary Data Feed | $3M | Insider-Events 5 Tage früher |
| Prime Brokerage Deal | $1M | Margin 5x, Borrow Fees -50% |
| Dark Pool Zugang | $10M | Orders bewegen den Markt weniger (-50% Slippage) |
| Medien-Kontakte | $5M | Kannst Gerüchte über Firmen streuen (Vorsicht: SMA!) |
| Lobbyisten in DC | $20M | Regulierung beeinflussen (Sector-Boost-Events) |
| Satellitenbilder-Abo | $8M | Erkenne Supply Chain Issues 3 Tage früher |
| Expert Network | $4M | Insider-Tipps mit 90% Genauigkeit (aber SMA-Risiko!) |

**Meilenstein: $1.000.000.000** → PRESTIGE verfügbar

#### Phase 5 / Prestige: "IPO — Geh an die Börse"

**Was passiert:**
1. Dein Hedge Fund wird als Aktie im Markt gelistet
2. Er wird zu einem der 14 AI-Trader (deine Performance beeinflusst sein Verhalten)
3. Du startest NEU in der Garage
4. **Permanente Boni** aus diesem Run:
   - +$500 pro Prestige zum Startkapital ($1.000 → $1.500 → $2.000...)
   - 1 Upgrade bleibt freigeschaltet (du wählst)
   - 1 neuer Stock-Slot in Phase 1 (10 → 11 → 12...)
   - Titel: "Prestige I: Apprentice" → "Prestige V: Wolf" → "Prestige X: Master of the Universe"
5. **Dein alter Fund als AI-Gegner:**
   - Handelt nach deinem historischen Stil
   - Konkurriert um die gleichen Stocks
   - Kann sogar gegen dich traden

**Meta-Progression über alle Runs:**
- Lifetime Wealth (kumuliert): Zählt über alle Runs
- Hall of Fame: Deine besten Funds mit Performance-History
- Unlock-Track: Kosmetische Themes (Bloomberg Dark, WSB Theme, 80s Retro)

### 2.3 Visuelles Feedback

**Büro-Ansicht** (obere 30% des Bildschirms):
```
PHASE 1:        PHASE 2:         PHASE 3:           PHASE 4:
┌─────────┐    ┌──────────┐     ┌─────────────┐    ┌────────────────┐
│ 🛏️ 💻   │    │ 🖥️🖥️     │     │ 🖥️🖥️🖥️🖥️    │    │ 📊📊📊📊📊📊📊 │
│ Schlaf-  │    │ Apartment│     │ Büro        │    │ Trading Floor  │
│ zimmer   │    │ Desk     │     │ 4 Desks     │    │ 20+ Screens    │
│ 📱       │    │ ☕ 📰    │     │ 👥👥👥       │    │ 👥👥👥👥👥👥👥   │
└─────────┘    └──────────┘     │ 🌆 Skyline  │    │ 🌃 Penthouse   │
                                └─────────────┘    └────────────────┘
```

- Einfache 2D-Pixel-Art oder stilisierte Illustration
- Upgrades sind visuell sichtbar (neuer Monitor erscheint, Analyst sitzt am Desk)
- Kleine Animationen (Bildschirme flackern, Kaffee dampft)
- Bei Prestige: Büro "verwandelt" sich in die neue Phase

### 2.4 Balancing

| Phase | Dauer | Income/Tag | Upgrade-Kosten | Progression |
|-------|-------|-----------|----------------|-------------|
| 1 Garage | 30-60 min | $50-500 | $200-$5.000 | Schnell, lernend |
| 2 Apartment | 45-90 min | $500-5.000 | $2.000-$25.000 | Stetig |
| 3 Office | 60-120 min | $5.000-50.000 | $50.000-$1.000.000 | Herausfordernd |
| 4 Hedge Fund | 60-120 min | $50.000-500.000 | $1M-$20M | Komplex |
| Prestige | 5 min | — | — | Belohnend |
| **Total Run** | **3-6 Stunden** | | | |

---

## 3. ARCADE MODE — Vollständiges Design

### 3.1 Core Loop

```
Wähle Modifier → 15 Min Timer startet → Trade wie verrückt →
Events alle 30 Sek → Flash Crashes → Short Squeezes →
Timer endet → Score berechnet → Leaderboard
```

### 3.2 Scoring

```
Base Score = (Endportfolio - $10.000) × 100
Bonus: Trades > 50       → +500
Bonus: Max Drawdown < 10% → +1.000
Bonus: Short Squeeze überlebt → +2.000
Bonus: Flash Crash profitiert → +3.000
Malus: Bankrott          → Score = 0
Malus: SMA-Strafe        → -500 pro Strafe
```

### 3.3 Modifier (wählbar, stackbar)

| Modifier | Effekt | Score-Multiplier |
|----------|--------|-----------------|
| **YOLO** | Nur All-In erlaubt (100% pro Trade) | 2.0x |
| **Bear Market** | Markt fällt -0.1%/Tick konstant | 1.5x |
| **Blindfold** | Kein Chart, nur Preiszahlen | 1.8x |
| **Speed Demon** | 5-Minuten-Runde, 30x Speed | 1.5x |
| **Penny Stocks Only** | Nur Stocks unter $5 | 1.3x |
| **No Shorts** | Short Selling deaktiviert | 0.8x |
| **Insider** | 1 Rumor pro Minute (aber SMA aktiv!) | 1.2x |
| **Flash Crash Chaos** | Flash Crash alle 60 Sekunden | 2.0x |
| **Margin Madness** | 20x Leverage verfügbar | 1.5x |

### 3.4 Meme-Stocks (Arcade-exklusiv)

5 spezielle Stocks mit extremen Eigenschaften:
```
$MOON  — "MoonShot Inc" — Volatilität 10x, kein Fundamental-Wert
$YOLO  — "YOLO Capital" — Preis folgt Reddit-Sentiment
$DOGE  — "DogeCoin Corp" — Random Walks ohne jede Logik
$APE   — "Ape Together Inc" — Short Interest 80%, Squeeze-Garantie
$REKT  — "GetRekt Holdings" — Geht garantiert bankrott, Timing ist alles
```

### 3.5 Daily Challenge

- **Jeden Tag**: Neues Szenario mit festem Seed
- **Alle spielen das gleiche Setup**: Gleiche Stocks, gleiche Events, gleicher Zeitrahmen
- **Rangliste**: Top 100 werden angezeigt
- **Streak-Bonus**: 7 Tage in Folge gespielt → Kosmetischer Unlock

---

## 4. EDUCATION WIKI — Vollständiges Design

### 4.1 Design-Philosophie

**Inspiration:**
- **Civilization VI Civilopedia**: Umfassend, durchsuchbar, immer verfügbar
- **Paradox-Spiele**: In-Game Wiki verlinkt mit Gameplay
- **Investopedia**: Goldstandard für Finanz-Bildung online

**Prinzip:** Jeder Artikel ist **maximal 500 Wörter**. Kein Wall-of-Text. Jeder Artikel hat:
- **TL;DR** (1 Satz)
- **Erklärung** (2-3 Absätze)
- **Beispiel** (konkretes Zahlenbeispiel)
- **In StockSim** (wie es im Spiel funktioniert)
- **Fun Fact** (historische Anekdote)

### 4.2 Artikel-Templates

**Template: Konzept-Artikel**
```markdown
# {Titel}

**TL;DR:** {Ein Satz der das Konzept erklärt.}

## Was ist {Konzept}?

{2-3 Absätze Erklärung in einfacher Sprache.}

## Beispiel

{Konkretes Zahlenbeispiel mit $-Beträgen.}

## In StockSim

{Wie der Spieler das Konzept im Spiel antrifft/nutzt.}

## Fun Fact

{Historische Anekdote oder überraschende Tatsache.}

---
*Verwandte Artikel: {Link1}, {Link2}, {Link3}*
```

**Template: Personen-Artikel**
```markdown
# {Name}

**{Titel/Beruf}** | {Lebensjahre}

**Bekannt für:** {1 Satz}

## Wer war {Name}?

{2 Absätze Biografie.}

## Die berühmteste Trade/Entscheidung

{Die eine Story die jeder kennen sollte.}

## Was können wir lernen?

{2-3 Lektionen für den Spieler.}

## In StockSim

{Wie dieses Wissen im Spiel hilft.}
```

**Template: Historisches-Event-Artikel**
```markdown
# {Event-Name} ({Jahr})

**Was passierte:** {1 Satz}
**Markt-Impact:** {S&P 500 Performance}
**Dauer:** {X Tage/Monate/Jahre}

## Vorgeschichte

{Was zum Event führte.}

## Der Tag/Die Woche

{Dramatische Beschreibung.}

## Nachwirkungen

{Was danach passierte, Regulierungsänderungen.}

## Lektionen

{Was Trader daraus gelernt haben.}

## In StockSim

{Verweis auf History Mode Szenario.}
```

### 4.3 Vollständige Artikel-Liste (70+ Artikel)

**Börsengeschichte (12 Artikel):**
1. Die Geburt der Aktie — Amsterdam 1602
2. Wall Street — Vom Buttonwood Tree zur NYSE
3. Der Ticker Tape — Wie Information die Märkte veränderte
4. Der Große Crash von 1929
5. Die SEC — Warum Regulierung entstand
6. Von Parkett zu Bildschirm — Elektronischer Handel
7. Black Monday 1987 — Der schlimmste Tag
8. Die Dot-Com Bubble — Gier und Wahnsinn
9. Die Finanzkrise 2008 — Wie Banken die Welt beinahe zerstörten
10. Flash Crash 2010 — Wenn Algorithmen durchdrehen
11. GameStop 2021 — Retail vs Wall Street
12. Die Zukunft — AI, Dark Pools, Dezentralisierung

**Trading-Grundlagen (15 Artikel):**
13. Was ist eine Aktie?
14. Bid, Ask und Spread
15. Market Orders — Sofort kaufen/verkaufen
16. Limit Orders — Dein Preis oder nichts
17. Stop Orders — Verluste begrenzen
18. Trailing Stop — Gewinne absichern
19. Long gehen — Auf steigende Kurse setzen
20. Short Selling — Auf fallende Kurse setzen
21. Margin Trading — Mit geliehenem Geld handeln
22. Dividenden — Passives Einkommen
23. IPOs — Wenn Firmen an die Börse gehen
24. ETFs — Ein Korb voller Aktien
25. Slippage — Warum du nicht immer deinen Preis bekommst
26. After-Hours Trading — Handel nach Börsenschluss
27. Circuit Breaker — Warum Märkte gestoppt werden

**Technische Analyse (10 Artikel):**
28. Candlestick Charts lesen
29. Moving Averages (SMA & EMA)
30. RSI — Relative Strength Index
31. MACD — Moving Average Convergence/Divergence
32. Bollinger Bands
33. VWAP — Volume Weighted Average Price
34. Support & Resistance
35. Volume — Was Handelsvolumen verrät
36. Trend Lines — Trends erkennen
37. Chart Patterns — Kopf-Schulter, Doppelboden & Co

**Fundamentalanalyse (8 Artikel):**
38. P/E Ratio — Was ist "teuer"?
39. Revenue & Earnings — Die Einnahmen verstehen
40. Profit Margins — Wie profitabel ist eine Firma?
41. Debt-to-Equity — Verschuldung bewerten
42. Market Cap — Von Micro bis Mega
43. Dividendenrendite — Einkommen bewerten
44. Fair Value — Ist eine Aktie über/unterbewertet?
45. Earnings Reports — Quartalszahlen lesen

**Marktstruktur (8 Artikel):**
46. Wer handelt? — Market Maker bis Retail
47. Das Orderbook — Angebot und Nachfrage sehen
48. High-Frequency Trading — Millisekunden-Handel
49. Dark Pools — Versteckter Handel
50. Short Squeezes — Wenn Shorts brennen
51. Regulierung — SEC, FINRA und die Regeln
52. Insider Trading — Was erlaubt ist und was nicht
53. Market Manipulation — Pump & Dump und andere Tricks

**Strategie & Psychologie (10 Artikel):**
54. Buy & Hold vs Active Trading
55. Value Investing — Der Warren-Buffett-Weg
56. Growth Investing — Auf Wachstum setzen
57. Momentum Trading — Der Trend ist dein Freund
58. Mean Reversion — Übertreibungen nutzen
59. Position Sizing — Wie viel pro Trade?
60. Stop Losses richtig setzen
61. Emotionen kontrollieren — Fear & Greed
62. Diversifikation — Nicht alles auf eine Karte
63. Risk/Reward Ratio — Ist der Trade es wert?

**Berühmte Personen (8 Artikel):**
64. Jesse Livermore — Der legendäre Spekulant
65. Warren Buffett — Das Orakel von Omaha
66. George Soros — "Der Mann der die Bank of England brach"
67. Michael Burry — The Big Short
68. Keith Gill (DFV/Roaring Kitty) — GameStop Revolution
69. Bernie Madoff — Der größte Betrug der Geschichte
70. Jim Simons — Renaissance Technologies und die Quants
71. Cathie Wood — Innovation und Disruption

**StockSim Guide (5 Artikel):**
72. Anfänger-Guide
73. Intermediate: Order-Typen meistern
74. Advanced: Short Selling & Margin
75. Expert: Marktmanipulation erkennen
76. Szenarien-Guide

### 4.4 Kontext-Integration

**Jedes UI-Element** das ein Wiki-Konzept referenziert bekommt ein ℹ️-Icon:
- Klick → Wiki-Artikel öffnet sich als Overlay
- Artikel lädt im selben Modal wie das Glossar (aber größer: 700×500px)

**Beispiele:**
- P/E Ratio im Stock Detail → ℹ️ → Artikel "P/E Ratio"
- RSI im Chart → ℹ️ → Artikel "RSI"
- Short-Button im Order Panel → ℹ️ → Artikel "Short Selling"
- SMA-Shield in TopBar → ℹ️ → Artikel "Regulierung"

---

## 5. HISTORY MODE — Szenario-Details

### 5.1 Szenario-Struktur

Jedes Szenario hat:
- **Intro** (historischer Kontext, 3-4 Bildschirme mit Text + Zeitstrahl)
- **Vorkonfigurierte Marktbedingungen** (Zinsen, Inflation, Sektorbewertungen)
- **Geskriptete Event-Sequenz** (historisch akkurat)
- **Spieler-Startbedingungen** (Kapital, verfügbare Tools)
- **Ziel** (Überleben, Profit-Target, Beat-the-Market)
- **Ergebnis-Vergleich** ("Du: +12%. Warren Buffett damals: -8%. Du hast gewonnen!")

### 5.2 Szenario: Financial Crisis 2008

```
INTRO:
  Bildschirm 1: "Es ist 2006. Immobilienpreise steigen seit 10 Jahren.
                  Banken vergeben Kredite an jeden. Niemand sieht Gefahr."
  Bildschirm 2: "CDOs, Subprime Mortgages, Leverage — die Zeitbombe tickt.
                  Einige wenige sehen es kommen."
  Bildschirm 3: "Du bist einer von ihnen. $200.000 Portfolio. 18 Monate
                  bis zur Katastrophe. Was machst du?"

MARKTBEDINGUNGEN:
  Phase: Late-Cycle Bull (überhitzt)
  Zinsen: 5.25% (hoch, Fed hat gerade gestoppt)
  Immobilien: Überbewertet (+200% seit 2000)
  Financials P/E: 12x (scheinbar billig, aber toxische Assets)
  VIX: 12 (trügerische Ruhe)

EVENT-SEQUENZ:
  Monat 1-6: Erste Risse
    - "Subprime lender New Century Financial warns of rising defaults"
    - "Bear Stearns hedge funds report significant losses on CDO positions"
    - Financials -5-10%

  Monat 7-12: Krise baut sich auf
    - "BNP Paribas freezes three investment funds, citing 'evaporation of liquidity'"
    - "Northern Rock bank experiences first UK bank run in 150 years"
    - "Fed cuts rates to 4.75% in emergency move"
    - Financials -20-30%, Markt -10%

  Monat 13-15: Kollaps
    - "Bear Stearns collapses, sold to JPMorgan for $2/share"
    - "Lehman Brothers files for bankruptcy — largest in US history"
    - "AIG receives $85B government bailout"
    - "TARP: Congress passes $700B bank bailout"
    - Financials -60-80%, Markt -40%, VIX > 80

  Monat 16-18: Boden + erste Erholung
    - "Fed cuts rates to near zero"
    - "Obama announces $787B stimulus package"
    - "Markets hit bottom in March 2009"
    - Markt +20% vom Tief

SPIELER-ZIEL:
  "Beende die 18 Monate mit einem positiven Portfolio.
   Der S&P 500 hat -57% verloren. Kannst du es besser?"

ERGEBNIS:
  S&P 500: -57%
  Warren Buffett (Berkshire): -32%
  Michael Burry (Scion): +489% (shorted subprime)

  DEIN ERGEBNIS: {PlayerReturn}%
  Rating:
    > +100%: "The Big Short" ⭐⭐⭐⭐⭐
    > +20%:  "Hedge Fund Manager" ⭐⭐⭐⭐
    > 0%:    "Survivor" ⭐⭐⭐
    > -20%:  "Retail Investor" ⭐⭐
    > -40%:  "Lehman Employee" ⭐
    < -57%:  "Worse than doing nothing" 💀
```

*[Gleicher Detailgrad für alle 7 Szenarien: 1929, 1987, 2000, 2010 Flash Crash, 2020 COVID, 2021 GameStop]*

---

## 6. PERFORMANCE-ANFORDERUNGEN

### 6.1 Ziel-Metriken

| Metrik | Ziel | Aktuell (geschätzt) |
|--------|------|-------------------|
| Tick-Berechnung (263 Stocks) | < 5ms | ~10-20ms |
| Frontend Re-Render | < 16ms (60fps) | ~20-30ms |
| WebSocket Latenz | < 50ms | ~10-20ms |
| Memory (Backend) | < 200MB | ~100MB |
| Memory (Frontend) | < 300MB | ~150MB |
| Startup Time | < 5 Sekunden | ~3-5 Sek |
| Save/Load | < 2 Sekunden | ~1 Sek |

### 6.2 Kritische Optimierungen

1. **PriceEngine Logging abschalten** — Aktuell loggt JEDER Tick für JEDEN Stock (263 × 390 = 102.570 Log-Einträge pro Tag)
2. **Virtualized Stock Table** — 263 Rows rendern nur die ~20 sichtbaren
3. **Chart Update Throttle** — Max 2 Updates/Sekunde statt 60
4. **Delta WebSocket Updates** — Nur geänderte Felder senden

---

*Dieses Dokument wird fortlaufend erweitert. Jede Sektion ist die verbindliche Spezifikation für die Implementierung.*
