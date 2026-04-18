# StockSim

**A realistic stock market simulator with a living, systemic market — Bloomberg-grade UI meets simulation depth.**

![Status](https://img.shields.io/badge/status-in_development-yellow)
![Version](https://img.shields.io/badge/version-0.3.0--dev-blue)
![Backend](https://img.shields.io/badge/backend-C%23_.NET_8-512BD4)
![Frontend](https://img.shields.io/badge/frontend-Electron_%2B_React_19-61DAFB)
![Tests](https://img.shields.io/badge/tests-530_passing-brightgreen)
![License](https://img.shields.io/badge/license-Proprietary-red)

---

## About

StockSim is a single-player market simulation where players trade a living, procedurally generated stock universe. Prices move through a hybrid engine (Geometric Brownian Motion + LSTM predictions + GARCH + fat tails), events cascade through supply chains, and 14 AI trader archetypes create emergent market behavior. A regulator — the StockSim Market Authority — quietly tracks your trading patterns and flags suspicious activity.

The goal is not day-trader optimization: it is a deep, systemic simulation where every mechanic feeds into every other mechanic. A Fed rate hike strengthens the Dollar Index, hurting exporter sectors; high short interest plus weak fundamentals can trigger a meme-stock squeeze cycle; supply-chain whispers leak into analyst reports three days before earnings.

---

## Screenshots

> Screenshots are pending. Place final captures in `docs/screenshots/` and update this section before Steam submission.

- **Main trading view** — candlestick chart, RSI panel, orderbook, news feed
- **Dashboard** — sector heatmap, market breadth, macro indicators
- **Options chain** — Black-Scholes pricing, Greeks, IV crush
- **Analytics** — Sharpe ratio, equity curve, P&L distribution

---

## Key Features

### Market Simulation (realism focus)
- **Hybrid price engine**: Geometric Brownian Motion + LSTM neural network predictions (ONNX) + GARCH volatility clustering + fat-tailed jump diffusion
- **14 AI trader archetypes** (market maker, HFT, institutional, activist, hedge fund, etc.) with distinct behavior, creating emergent flow
- **Monetary policy regime** (QE → Neutral → Tightening) drives sector rotation automatically
- **Supply chain propagation**: events flow from suppliers to customers with realistic delays
- **Meme stock engine**: 5-phase lifecycle (discovery → FOMO → squeeze → diamond hands → crash) with dynamic short covering
- **Options market** with full Black-Scholes pricing, all five Greeks, IV crush around earnings, and gamma exposure (GEX) dealer hedging effects
- **Index rebalancing**, ETF flow effects, dividend system, circuit breakers, IPOs, M&A, tender offers

### Content Depth
- **572 event templates** across four tiers (routine → rare → mini-arcs → black swan) with **2,400+ unique headlines**
- **64 founding stories**, **144 company descriptions** across 12 sectors, generated per-stock
- **226 fictional analysts** from 30 firms with individual specializations and bias profiles
- **39 scenarios** (career challenges + 9 historical events like Black Monday 1987, COVID 2020, GameStop 2021)
- **53 achievements**, **12-step interactive tutorial**, **42 glossary entries**

### Regulator Simulation
The **StockSim Market Authority (SMA)** runs six detection algorithms in the background: insider trading, pump & dump, spoofing, wash trading, cornering, bear raids. Score accumulates, investigations open, and penalties (fines, trading bans, account freezes) follow.

### Engineering
- **~48,000 lines of code** across ~165 files
- **530 backend tests** covering engines, integration, regression, realism
- **Structured logging** with leveled output, context-rich error messages
- **Save/load** with versioned schema and backward-compatible migration

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | C# / .NET 8 (self-contained) |
| Frontend | Electron + React 19 + TypeScript |
| State | Zustand |
| Charts | ECharts (migrating to TradingView Lightweight Charts) |
| Transport | Local WebSocket (port 8765) |
| ML | Python (training), ONNX Runtime (inference) |
| Build | electron-builder (Windows NSIS installer) |
| Testing | xUnit (backend), Vitest + Playwright (frontend) |

---

## Architecture

StockSim is a local two-process application:

```
┌─────────────────────┐     WebSocket      ┌─────────────────────┐
│   Electron + React  │◄──── port 8765 ───►│  C# Game Engine     │
│   (Frontend UI)     │                     │  (Simulation Core)  │
│   ~13.5k LOC        │                     │  ~21k LOC           │
└─────────────────────┘                     └──────────┬──────────┘
                                                       │
                                         ┌─────────────┴───────────────┐
                                         │                             │
                                         ▼                             ▼
                                  18+ Simulation Engines        JSON Content Pack
                                  (Price, Orders, Events,       (500+ event templates,
                                   AI Traders, Economy,          analysts, scenarios)
                                   Options, SMA, Memes, …)
```

For a detailed file map see [`design/ARCHITECTURE.md`](design/ARCHITECTURE.md).
For the full game design specification see [`design/DESIGN_SPEC.md`](design/DESIGN_SPEC.md) (or the [`DESIGN_INDEX.md`](design/DESIGN_INDEX.md) for a section-by-section overview).

---

## Getting Started

### Prerequisites

- **.NET 8 SDK**
- **Node.js 20+** and **npm**
- **Git**
- Windows 10/11 (primary target; macOS/Linux support pending)

### Development

```bash
git clone https://github.com/Tschonsen/StockSim.git
cd StockSim

# Install frontend dependencies
cd frontend && npm install && cd ..

# Start backend + frontend dev servers
./dev.sh
```

Open `http://localhost:5173` in your browser. The backend runs headless on port 8765.

### Run Tests

```bash
# Backend (530 tests)
cd backend/StockSim.Engine.Tests
dotnet test

# Frontend
cd frontend
npm test
```

### Build a Release Installer

```bash
./build.sh
# → frontend/release/StockSim Setup X.X.X.exe
```

---

## Project Structure

```
StockSim/
├── backend/
│   ├── StockSim.Engine/              # C# simulation core (18+ engines)
│   │   ├── Models/                   # Domain types (Stock, Order, Event, ...)
│   │   ├── Services/                 # GameLoop + engines
│   │   ├── data/                     # JSON content pack (events, analysts)
│   │   └── Utils/                    # Logger, helpers
│   └── StockSim.Engine.Tests/        # 530 xUnit tests
├── frontend/
│   └── src/
│       ├── components/               # React components (screens, layout, trading, charts)
│       ├── stores/                   # Zustand state
│       ├── services/                 # WebSocket client, audio, logger
│       ├── hooks/                    # Keyboard shortcuts, etc.
│       └── main/                     # Electron main process
├── ml/                               # Python training pipeline (LSTM → ONNX)
├── design/                           # Living design documentation
│   ├── DESIGN_SPEC.md                # Full game design specification
│   ├── DESIGN_INDEX.md               # Spec section index
│   ├── ARCHITECTURE.md               # File-level dependency map
│   ├── CURRENT_STATE.md              # Latest session status
│   ├── MASTER_ROADMAP.md             # Feature roadmap
│   └── SESSION_HISTORY.md            # Dev session archive
├── scripts/                          # Build and automation helpers
├── build.sh                          # Full release build
├── dev.sh                            # Local dev launcher
└── CLAUDE.md                         # AI-pair-programming project rules
```

---

## Development Workflow

StockSim is built with an AI-pair-programming workflow. Every session follows the process defined in [`CLAUDE.md`](CLAUDE.md):

1. Read `design/CURRENT_STATE.md` and `design/ARCHITECTURE.md` first.
2. Touch only the files relevant to the task.
3. Test-first (TDD); all new behavior is covered by tests.
4. After each feature, update `CURRENT_STATE.md` and run a mini-audit against the spec.
5. Milestone audits are logged in `design/AUDIT_LOG.md`.

This discipline is why the project can sustain ~48k LOC with 530 green tests at a single-developer pace.

---

## Roadmap

- **Chart migration**: ECharts → TradingView Lightweight Charts (eliminates known dispose/yAxis bugs)
- **Code signing** for Windows installer (SmartScreen warning removal)
- **Steam integration**: store page, achievements, cloud saves — targeted August 2026
- **Multiplayer (Phase 2)**: see [`design/MULTIPLAYER_VISION.md`](design/MULTIPLAYER_VISION.md) — post-launch

Full roadmap: [`design/MASTER_ROADMAP.md`](design/MASTER_ROADMAP.md).

---

## License

Proprietary — see [`LICENSE`](LICENSE). All rights reserved.
