// End-to-end smoke test: acts as an MCP client, spawns the bridge (which boots the
// backend), and drives a short game — proving the dual-channel contract works.
import path from "node:path";
import { fileURLToPath } from "node:url";
import { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { StdioClientTransport } from "@modelcontextprotocol/sdk/client/stdio.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const txt = (r) => r?.content?.map((c) => c.text).join("\n") ?? JSON.stringify(r);
const res = (r) => r?.contents?.map((c) => c.text).join("\n") ?? JSON.stringify(r);

const transport = new StdioClientTransport({
  command: process.platform === "win32" ? "node.exe" : "node",
  args: [path.resolve(__dirname, "..", "src", "server.js")],
});
const client = new Client({ name: "smoke", version: "0.1.0" }, { capabilities: {} });

await client.connect(transport);
console.log("connected.");

const tools = await client.listTools();
console.log("TOOLS:", tools.tools.map((t) => t.name).join(", "));
const resources = await client.listResources();
console.log("RESOURCES:", resources.resources.map((r) => r.uri).join(", "));

console.log("\n-- new_game (boots backend, may take ~30s) --");
console.log(txt(await client.callTool({ name: "new_game", arguments: { seed: 42, stockCount: 30 } })));

console.log("\n-- set_speed 5 --");
console.log(txt(await client.callTool({ name: "set_speed", arguments: { speed: 5 } })));

console.log("\n...letting the market run for 10s...");
await sleep(10_000);

console.log("\n-- resource stocksim://market (first 5) --");
const market = JSON.parse(res(await client.readResource({ uri: "stocksim://market" })));
console.log(`gameTime=${market.gameTime} open=${market.isMarketOpen} stocks=${market.stocks.length}`);
console.table(market.stocks.slice(0, 5));

const first = market.stocks[0];
console.log(`\n-- place_order Buy 10 ${first.symbol} --`);
console.log(txt(await client.callTool({ name: "place_order", arguments: { symbol: first.symbol, side: "Buy", quantity: 10 } })));

console.log("\n-- get_portfolio --");
console.log(txt(await client.callTool({ name: "get_portfolio", arguments: {} })));

console.log("\n-- resource stocksim://news (recent 5 headlines) --");
const news = JSON.parse(res(await client.readResource({ uri: "stocksim://news" })));
for (const n of news.recent.slice(-5)) {
  console.log(" •", n.headline);
  if (n.analystQuote) console.log("    Q:", n.analystQuote);
}

console.log("\nSMOKE OK");
await client.close();
process.exit(0);
