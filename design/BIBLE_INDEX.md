# StockSim — Bible Quick Reference

> Kurzreferenz zur Game Design Bible. Nur die Bible selbst lesen wenn Details nötig sind.

| Sektion | Thema | Status | Zeilen | Kerninhalt |
|---------|-------|--------|--------|------------|
| 1 | Vision & Zielgruppe | DONE | 1-80 | "Bloomberg meets STONKS-9800", 3 Spielertypen, Bankruptcy-Regeln |
| 2 | Visual Style Guide | DONE | 80-200 | Farben (#0A0E17, #10B981, #EF4444), Fonts (Inter/JetBrains Mono), Dark Theme |
| 3 | Main UI Layout | 70% | 200-900 | TopBar, Sidebars, 7 Tabs, Stock Detail. **Fehlt: Detachable Panels (3.0.14)** |
| 4 | Trading Mechanik | 75% | 900-2400 | Orders, Slippage, Margin, Dividenden, Short Selling. **Fehlt: Rumors (4.8), SSR (4.4.2), Short Squeeze Warnings (4.4.5)** |
| 5 | Market Simulation | 85% | 2400-2900 | GBM-Preisformel, Volatilität, Sektoren, Korrelation, Float/Outstanding |
| 6 | Portfolio Management | 85% | 2900-3100 | Cash, Positionen, P&L, Margin. Solide implementiert |
| 7 | AI-Trader | 60% | 3100-3800 | **Bible: 14 diskrete Typen. Code: 4 aggregierte.** Fehlend: Activist, Buyback, Short Seller als eigene Typen |
| 8 | Events & Lifecycle | 80% | 3800-4450 | 50+ Templates, Flash Crash, Circuit Breaker, IPO, Earnings. **Fehlt: M&A/Tender Offers** |
| 9 | SMA Regulierung | **90%** | 4450-4800 | Suspicion Score, 6 Detektionen, Investigations, Penalties, UI. **Fehlt: Rumors (4.8) für vollständiges Insider-Trading** |
| 10 | Zeitsystem | 90% | 4800-5000 | Market Hours, Speed, Skip to Open. Solide |
| 11 | Stock-Generierung | 70% | 5000-5500 | 12 Sektoren, Traits, MarketCap. **Teilweise: ~25 Traits in Bible, unklar wie viele wirken** |
| 12 | Charts & Indikatoren | 75% | 5500-5800 | TradingView, SMA/EMA/RSI/MACD/BB. Solide Basis |
| 13 | News-System | 80% | 5800-6100 | Ticker, Severity, Sentiment, Filter. Gut |
| 14 | Tutorial | 40% | 6100-6300 | TutorialOverlay existiert. **Fehlt: Tiefe, interaktive Führung** |
| 15 | Save/Load | 90% | 6300-6400 | JSON, Multiple Slots, Autosave. Gut |
| 16 | Settings | 60% | 6400-6500 | 7/27+ funktional (Audio, Scale, Contrast). **Fehlt: viele Gameplay-Settings** |
| 17 | Audio | 50% | 6500-6600 | 10 Sounds definiert, 6 getriggert. **Fehlt: orderPlaced, click, notification, short_squeeze_alarm** |
| 18 | Hotkeys | 60% | 6600-6700 | Kern-Shortcuts da. **Fehlt: B=Buy-Fokus funktional, einige Spezial-Shortcuts** |
| 19 | Edge Cases | 80% | 6700-6800 | Bankruptcy, Restart. **Fehlt: Negative Cash Handling bei Margin** |
| 20 | Phasen-Roadmap | n/a | 6800-6900 | Phase 1 MVP + Phase 2 dokumentiert |
| 21 | Tech-Architektur | 90% | 6900-7100 | WebSocket-Protokoll, Electron, .NET. Solide |

## Größte Lücken (nach Impact)

1. ~~**SMA/Regulierung (Sektion 9)**~~ — ✅ 90% implementiert
2. **Rumors (4.8)** — Blockiert "The Insider" Szenario + Insider-Trading-Gameplay
3. **SSR/Uptick Rule (4.4.2)** — Realismus bei Short Selling
4. **AI-Trader Diversität (7)** — 14 Typen geplant, 4 implementiert
5. **Tutorial Tiefe (14)** — Kritisch für neue Spieler
