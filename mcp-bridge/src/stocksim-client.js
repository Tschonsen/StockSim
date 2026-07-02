// Thin client over StockSim's WebSocket protocol: spawns (or connects to) the backend,
// tracks the latest semantic state, and exposes send + waitFor helpers. This is the
// "intent contract" the MCP server maps onto agent tools/resources.
import { spawn } from "node:child_process";
import path from "node:path";
import { fileURLToPath } from "node:url";
import WebSocket from "ws";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const BACKEND_DIR = path.resolve(__dirname, "..", "..", "backend", "StockSim.Engine");

export class StockSimClient {
  constructor({ log = () => {} } = {}) {
    this.log = log;
    this.ws = null;
    this.proc = null;
    this.port = process.env.STOCKSIM_PORT ? Number(process.env.STOCKSIM_PORT) : null;
    this._ready = null;

    // Latest semantic state
    this.stocks = new Map(); // symbol -> { symbol, name, sector, price, change }
    this.portfolio = null;
    this.news = []; // ring buffer of recent event objects
    this.gameTime = null;
    this.tick = 0;
    this.isMarketOpen = false;
    this.lastOrderResult = null;
    this._waiters = []; // { type, resolve, timer }
  }

  /** Ensure backend is running and the WS is open. Idempotent. */
  ensureReady() {
    if (!this._ready) this._ready = this._boot();
    return this._ready;
  }

  async _boot() {
    if (!this.port) this.port = await this._spawnBackend();
    await this._connect(this.port);
  }

  _spawnBackend() {
    return new Promise((resolve, reject) => {
      this.log(`spawning backend in ${BACKEND_DIR}`);
      const cmd = process.platform === "win32" ? "dotnet.exe" : "dotnet";
      this.proc = spawn(cmd, ["run", "-c", "Release"], { cwd: BACKEND_DIR });
      let buf = "";
      const onData = (d) => {
        buf += d.toString();
        const m = buf.match(/READY:(\d+)/);
        if (m) {
          this.proc.stdout.off("data", onData);
          resolve(Number(m[1]));
        }
      };
      this.proc.stdout.on("data", onData);
      this.proc.stderr.on("data", (d) => this.log(`[backend stderr] ${d}`));
      this.proc.on("exit", (code) => this.log(`backend exited ${code}`));
      setTimeout(() => reject(new Error("backend did not signal READY within 90s")), 90_000);
    });
  }

  _connect(port) {
    return new Promise((resolve, reject) => {
      const url = `ws://127.0.0.1:${port}`;
      this.log(`connecting ${url}`);
      this.ws = new WebSocket(url, { maxPayload: 0 });
      this.ws.on("open", () => { this.log("ws open"); resolve(); });
      this.ws.on("error", reject);
      this.ws.on("message", (raw) => this._onMessage(raw));
    });
  }

  _onMessage(raw) {
    let msg;
    try { msg = JSON.parse(raw.toString()); } catch { return; }
    const { type, payload = {} } = msg;

    switch (type) {
      case "MarketSnapshot":
        for (const s of payload.stocks ?? [])
          this.stocks.set(s.symbol, { symbol: s.symbol, name: s.name, sector: s.sector, price: s.price, change: s.change ?? 0 });
        for (const e of payload.initialNews ?? []) this._pushNews(e);
        break;
      case "MarketUpdate":
        this.gameTime = payload.gameTime ?? this.gameTime;
        this.tick = payload.tick ?? this.tick;
        this.isMarketOpen = payload.isMarketOpen ?? this.isMarketOpen;
        for (const u of payload.prices ?? []) {
          const sym = u.Symbol ?? u.symbol;
          const cur = this.stocks.get(sym);
          if (cur) { cur.price = u.price ?? cur.price; cur.change = u.change ?? cur.change; }
        }
        break;
      case "NewsEvents":
        for (const e of payload.events ?? []) this._pushNews(e);
        break;
      case "PortfolioUpdate":
        this.portfolio = payload;
        break;
      case "OrderResult":
        this.lastOrderResult = payload;
        break;
    }
    this._resolveWaiters(type, payload);
  }

  _pushNews(e) {
    this.news.push({
      headline: e.headline,
      summary: e.summary,
      analystQuote: e.analystQuote,
      tags: e.tags,
      affectedSymbols: e.affectedSymbols,
    });
    if (this.news.length > 80) this.news.splice(0, this.news.length - 80);
  }

  send(type, payload = {}) {
    if (!this.ws || this.ws.readyState !== WebSocket.OPEN) throw new Error("not connected");
    this.ws.send(JSON.stringify({ type, payload }));
  }

  /** Resolve on the next inbound message of `type`, or after `ms` (whichever first). */
  waitFor(type, ms = 1500) {
    return new Promise((resolve) => {
      const timer = setTimeout(() => {
        this._waiters = this._waiters.filter((w) => w.timer !== timer);
        resolve(null);
      }, ms);
      this._waiters.push({ type, resolve, timer });
    });
  }

  _resolveWaiters(type, payload) {
    const still = [];
    for (const w of this._waiters) {
      if (w.type === type) { clearTimeout(w.timer); w.resolve(payload); }
      else still.push(w);
    }
    this._waiters = still;
  }

  marketSnapshot() {
    return {
      gameTime: this.gameTime, tick: this.tick, isMarketOpen: this.isMarketOpen,
      stocks: [...this.stocks.values()].sort((a, b) => a.symbol.localeCompare(b.symbol)),
    };
  }

  close() {
    try { this.ws?.close(); } catch {}
    try { this.proc?.kill(); } catch {}
  }
}
