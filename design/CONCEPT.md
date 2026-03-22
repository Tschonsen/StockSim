# StockSim — Concept Notes

## Idea
Realistic stock market simulation. Single-player with AI traders, later real-time multiplayer.
"Bloomberg Terminal meets STONKS-9800" — looks like TV financial news, feels like Wall Street.

## Tech Stack (decided)
- **Frontend:** Electron + React + TypeScript (UI, Charts, Layout)
- **Backend:** C# .NET (Simulation, AI, Events, Savegames — all logic)
- **Charts:** TradingView Lightweight Charts (npm)
- **Communication:** Local WebSocket (React ↔ C#)
- **Steam:** Electron can export to Steam

## Core Features
- Hundreds to thousands of stocks, procedurally generated
- 12+ sectors with correlations
- Real-time with HOI4-style speed control (Pause, 1x, 2x, 5x, 10x)
- Candlestick charts, live updating
- Buy / Sell / Short / Limit Orders
- Portfolio with P&L tracking
- AI traders (Retail, Institutional, Bots) that move the market realistically

## Trading Mechanics
- Short Selling + Short Squeeze
- Diamond Hands (hold through volatility)
- Margin Calls
- Pump & Dump
- Insider Trading
- Options (Calls/Puts) — stretch goal
- As realistic as possible

## Events System
- Breaking News that explains WHY markets move
- Macro events (Fed decisions, interest rates, inflation)
- Sector events (oil spill, tech breakthrough, regulation)
- Company events (earnings, CEO scandal, bankruptcy, merger)
- Hundreds of unique events, not repetitive
- Cascading effects (one event triggers follow-ups)

## UI Style
- Dark theme, Bloomberg Terminal aesthetic
- Green/red numbers, glowing charts
- News ticker at bottom
- Professional but accessible
- Modern/clean, not retro

## Tutorial
- Introduction that teaches buying, selling, shorting, reading charts
- Guided first few minutes
- Tooltips for advanced concepts

## Phases
1. **Phase 1: Singleplayer MVP** — Simulation + AI + Events + Charts
2. **Phase 2: Multiplayer** — Real-time with many players, server infrastructure

## Platform
- Steam (desktop, Windows first)

## Architecture
```
React/TS = Display only (charts, buttons, layout)
C# .NET  = All logic (simulation, AI, events, prices, saves)
WebSocket = Local communication between the two
```
