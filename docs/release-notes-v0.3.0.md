**StockSim v0.3.0 — First Public Release**

The first public snapshot of StockSim: a single-player stock market simulator with a living, systemic market. Bloomberg-grade UI, 18 interacting simulation engines, options trading with full Greeks, and a regulator that flags suspicious trading patterns.

### What's inside
- **Market simulation**: hybrid GBM + LSTM + GARCH price engine, 14 AI trader archetypes, monetary policy regime, supply-chain event propagation, meme stock lifecycle, options market with gamma exposure (GEX)
- **Content**: 572 event templates, 2,400+ headlines, 64 founding stories, 144 company descriptions, 226 fictional analysts, 39 scenarios, 53 achievements, 12-step tutorial
- **Engineering**: ~48,000 LOC, 530 backend tests, structured logging, versioned save/load

### Install
1. Download `StockSim.Setup.0.3.0.exe` below
2. Run the installer (Windows 10/11)
3. First launch: SmartScreen will warn — click "More info" → "Run anyway" (installer is unsigned; code signing is on the roadmap)

### Known limitations
- Windows only (macOS/Linux builds pending)
- Installer is unsigned → SmartScreen warning on first run
- Some settings are UI-only (see Settings Modal, will be wired in v0.4.0)

### What's next
- ECharts → TradingView Lightweight Charts migration
- Code signing for the installer
- Multiplayer (Phase 2, post-launch)

Full design specification: [`design/DESIGN_SPEC.md`](https://github.com/Tschonsen/StockSim/blob/master/design/DESIGN_SPEC.md)
Development workflow: [`CLAUDE.md`](https://github.com/Tschonsen/StockSim/blob/master/CLAUDE.md)
