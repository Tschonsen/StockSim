# StockSim — Aktueller Zustand

> Kurz und knapp. Session-History siehe `design/SESSION_HISTORY.md`.

## Letztes Update: 2026-04-01, Session 32

## Status: v0.2.1-dev — Commodities + Realismus-Tests

### Projekt-Kennzahlen
- **~43.000 Zeilen Code** (~19.000 Backend + 12.200 Frontend + 10.000 Tests + 4.000 Content + 400 ML)
- **~155 Dateien**
- **523 Backend Tests** grün (vorher 496)
- **900 Headlines** in Event-Templates (vorher ~480), **572 Templates** (Tier1: 389, Tier2: 127, Tier3: 33, Tier4: 23)
- **64 Gründungsgeschichten** (vorher 15), **144 Company Descriptions** (vorher 48)
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

### TODO für nächste Session:
- [ ] Options Tab manuell verifizieren (Chain, Greeks, Buy/Sell) — GUI-Test
- [ ] Code Signing für Installer (SmartScreen-Warnung entfernen)
- [ ] Steam Store Page vorbereiten (Screenshots, Description, Tags)
- [ ] Frontend: Commodity-Sektor in Sidebar-Gruppierung testen
- [ ] Installer neu bauen mit Commodity-ETFs + allen Fixes

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
- [ ] Installer neu bauen nach allen Fixes
- [ ] Manueller Playtest (GUI)
- [ ] SmartScreen-Workaround dokumentieren (oder Code Signing)

### Offene Punkte:
- Code Signing für Installer (SmartScreen-Warnung)
- Auto-Updater
- Steam Integration (nicht vor August 2026)
