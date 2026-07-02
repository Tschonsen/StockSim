# StockSim MCP Bridge

A **dual-channel frontend** prototype: it exposes StockSim's existing WebSocket
intent-contract as Model Context Protocol (MCP) **tools** (actions) and **resources**
(state), so any MCP-capable agent can play and observe the game natively — no pixels,
no screen-scraping.

The idea generalizes: the bridge maps an app's semantic protocol onto MCP. StockSim is
the first adapter because its typed WS messages (`NewGame`, `PlaceOrder`, `MarketSnapshot`,
`NewsEvents`, …) are already ~80% of an agent contract.

## What it exposes

**Tools (actions)**
- `new_game({ seed?, startingCash?, stockCount?, playerName? })` — boots the backend on first call
- `set_speed({ speed: 1..5 })`
- `place_order({ symbol, side: Buy|Sell|Short|Cover, quantity })`
- `get_portfolio()` — cash, positions, equity

**Resources (state)**
- `stocksim://market` — game time, market-open flag, all stocks (symbol/name/sector/price/change)
- `stocksim://portfolio` — current portfolio
- `stocksim://news` — recent news (headline, summary, analyst quote)

## Run

```bash
cd mcp-bridge
npm install
npm run smoke   # end-to-end test: boots backend, plays a short game over MCP
```

The bridge spawns the backend itself (`dotnet run -c Release` in `../backend/StockSim.Engine`).
To attach to an already-running backend instead, set `STOCKSIM_PORT` (the port from the
backend's `READY:<port>` line).

## Register with Claude (Desktop or Claude Code)

Add to your MCP config (e.g. `claude_desktop_config.json` or a project `.mcp.json`):

```json
{
  "mcpServers": {
    "stocksim": {
      "command": "node",
      "args": ["D:/Dev/projects/StockSim/mcp-bridge/src/server.js"]
    }
  }
}
```

Then in a fresh session: `new_game`, read `stocksim://market`, `place_order`, etc. —
the agent plays StockSim directly.

## Notes / next steps
- Requires the .NET backend to build (`dotnet` on PATH).
- v1 supports market orders; limit/stop, options, alerts can be added by mapping the
  remaining WS message types the same way.
- Security: the action channel is unscoped in this prototype — fine for local single-user
  play, but a shared deployment would need per-tool permissions.
- Generalization target: extract the "annotate state + actions → emit MCP" layer into a
  reusable SDK (`AgentBridge`) with a web in-page `window.agent` transport alongside stdio.
