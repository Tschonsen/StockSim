#!/usr/bin/env node
// StockSim MCP bridge — exposes the game's WebSocket intent-contract as agent-usable
// MCP tools (actions) + resources (state). Any MCP client can drive/observe the game
// without pixels. IMPORTANT: stdout is the MCP transport — all logging goes to stderr.
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { z } from "zod";
import { StockSimClient } from "./stocksim-client.js";

const log = (...a) => console.error("[stocksim-mcp]", ...a);
const client = new StockSimClient({ log });

const json = (obj) => ({ content: [{ type: "text", text: JSON.stringify(obj, null, 2) }] });
const resourceJson = (uri, obj) => ({
  contents: [{ uri: uri.href, mimeType: "application/json", text: JSON.stringify(obj, null, 2) }],
});

const server = new McpServer({ name: "stocksim", version: "0.1.0" });

// ---- Tools (actions) ----

server.tool(
  "new_game",
  "Start a new StockSim game. Boots the backend on first call.",
  {
    seed: z.number().int().optional(),
    startingCash: z.number().optional(),
    stockCount: z.number().int().optional(),
    playerName: z.string().optional(),
  },
  async ({ seed = 42, startingCash = 100000, stockCount = 30, playerName = "Agent" }) => {
    await client.ensureReady();
    client.send("NewGame", { playerName, startingCash, stockCount, seed });
    const snap = await client.waitFor("MarketSnapshot", 5000);
    client.send("GetPortfolio");
    await client.waitFor("PortfolioUpdate", 1500);
    return json({ started: true, stocks: snap?.stocks?.length ?? client.stocks.size, seed });
  }
);

server.tool(
  "set_speed",
  "Set simulation speed (1=slow .. 5=maximum).",
  { speed: z.number().int().min(1).max(5) },
  async ({ speed }) => {
    await client.ensureReady();
    client.send("SetSpeed", { speed });
    return json({ speed });
  }
);

server.tool(
  "place_order",
  "Place a market order. side = Buy | Sell | Short | Cover.",
  {
    symbol: z.string(),
    side: z.enum(["Buy", "Sell", "Short", "Cover"]),
    quantity: z.number().positive(),
  },
  async ({ symbol, side, quantity }) => {
    await client.ensureReady();
    client.lastOrderResult = null;
    client.send("PlaceOrder", { Symbol: symbol, Side: side, Type: "Market", Quantity: quantity });
    const result = await client.waitFor("OrderResult", 2000);
    return json({ sent: { symbol, side, quantity }, result: result ?? client.lastOrderResult });
  }
);

server.tool(
  "get_portfolio",
  "Refresh and return the current portfolio (cash, positions, equity).",
  {},
  async () => {
    await client.ensureReady();
    client.send("GetPortfolio");
    const p = await client.waitFor("PortfolioUpdate", 1500);
    return json(p ?? client.portfolio ?? { note: "no portfolio yet" });
  }
);

// ---- Resources (state) ----

server.resource("market", "stocksim://market", async (uri) =>
  resourceJson(uri, client.marketSnapshot())
);

server.resource("portfolio", "stocksim://portfolio", async (uri) =>
  resourceJson(uri, client.portfolio ?? { note: "start a game first" })
);

server.resource("news", "stocksim://news", async (uri) =>
  resourceJson(uri, { count: client.news.length, recent: client.news.slice(-30) })
);

// ---- Boot ----

const shutdown = () => { client.close(); process.exit(0); };
process.on("SIGINT", shutdown);
process.on("SIGTERM", shutdown);

await server.connect(new StdioServerTransport());
log("StockSim MCP bridge ready (stdio)");
