# StockSim — Aktueller Zustand

> Kurz und knapp. Session-History siehe `design/SESSION_HISTORY.md`.

## Letztes Update: 2026-04-01, Session 31

## Status: v0.2.1-dev — Content-Tiefe

### Projekt-Kennzahlen
- **~41.500 Zeilen Code** (~18.500 Backend + 12.200 Frontend + 9.500 Tests + 3.500 Content + 400 ML)
- **~150 Dateien**
- **496 Backend Tests** grün
- **900 Headlines** in Event-Templates (vorher ~480), **552 Templates**
- **64 Gründungsgeschichten** (vorher 15), **144 Company Descriptions** (vorher 48)
- **57 Wiki-Artikel** in 8 Kategorien

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

### TODO für nächste Session:
- [ ] **Commodities als handelbare Assets**: Gold, Silber, Öl als Ticker (GLD, SLV, USO)
- [ ] Performance-Profiling bei 250 Stocks + Maximum Speed (Long Playtest war langsam)
- [ ] Options Tab manuell verifizieren (Chain, Greeks, Buy/Sell)
- [ ] Code Signing für Installer (SmartScreen-Warnung entfernen)
- [ ] Steam Store Page vorbereiten (Screenshots, Description, Tags)
- [ ] Tier 3/4 Event-Templates expandieren (aktuell 21 + 15, Ziel: ~48 + ~30)
- [ ] Archetyp-spezifische Event-Headlines (CEO-Archetype beeinflusst News-Stil)

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
