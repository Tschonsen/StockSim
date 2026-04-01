#!/usr/bin/env node
/**
 * Automated Backend Playtest
 * Connects via WebSocket, starts a game, runs through key flows.
 * Catches: crashes, protocol errors, missing data, NaN values.
 * Cannot catch: UI rendering issues.
 */
import WebSocket from 'ws';
import { execSync, spawn } from 'child_process';

let PORT = 0;
let ws;
let backend;
const results = [];
let testCount = 0;
let passCount = 0;
let failCount = 0;

function log(msg) { console.log(`  ${msg}`); }
function pass(name) { testCount++; passCount++; results.push(`  ✅ ${name}`); }
function fail(name, reason) { testCount++; failCount++; results.push(`  ❌ ${name}: ${reason}`); }

function send(type, payload = {}) {
  ws.send(JSON.stringify({ type, payload }));
}

function waitFor(msgType, timeoutMs = 10000) {
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => reject(new Error(`Timeout waiting for ${msgType}`)), timeoutMs);
    const handler = (raw) => {
      try {
        const msg = JSON.parse(raw.toString());
        if (msg.type === msgType) {
          clearTimeout(timer);
          ws.removeListener('message', handler);
          resolve(msg.payload);
        }
      } catch {}
    };
    ws.on('message', handler);
  });
}

function collectMessages(durationMs) {
  return new Promise((resolve) => {
    const msgs = [];
    const handler = (raw) => {
      try { msgs.push(JSON.parse(raw.toString())); } catch {}
    };
    ws.on('message', handler);
    setTimeout(() => {
      ws.removeListener('message', handler);
      resolve(msgs);
    }, durationMs);
  });
}

function checkNoNaN(obj, path = '') {
  if (obj === null || obj === undefined) return [];
  if (typeof obj === 'number' && isNaN(obj)) return [path || 'root'];
  if (typeof obj === 'string' && obj === 'NaN') return [path || 'root'];
  if (Array.isArray(obj)) {
    const issues = [];
    obj.forEach((item, i) => issues.push(...checkNoNaN(item, `${path}[${i}]`)));
    return issues;
  }
  if (typeof obj === 'object') {
    const issues = [];
    for (const [k, v] of Object.entries(obj)) issues.push(...checkNoNaN(v, `${path}.${k}`));
    return issues;
  }
  return [];
}

async function runTests() {
  console.log('\n🎮 StockSim Automated Playtest\n');

  // === TEST 1: Connect ===
  log('Connecting to backend...');
  try {
    ws = new WebSocket(`ws://127.0.0.1:${PORT}`);
    await new Promise((resolve, reject) => {
      ws.on('open', resolve);
      ws.on('error', reject);
      setTimeout(() => reject(new Error('Connection timeout')), 5000);
    });
    send('hello');
    const welcome = await waitFor('welcome', 5000);
    pass(`WebSocket connection + handshake (v${welcome.version})`);
  } catch (e) {
    fail('WebSocket connection', e.message);
    return;
  }

  // === TEST 2: List Saves ===
  try {
    send('ListSaves');
    const saveList = await waitFor('SaveList', 5000);
    pass(`ListSaves (${saveList.saves?.length ?? 0} saves found)`);
  } catch (e) {
    fail('ListSaves', e.message);
  }

  // === TEST 3: Start New Game ===
  let snapshot;
  try {
    send('NewGame', { StockCount: 30, StartingCash: 50000 });
    snapshot = await waitFor('MarketSnapshot', 15000);

    if (!snapshot.stocks || snapshot.stocks.length === 0) throw new Error('No stocks in snapshot');
    if (!snapshot.gameTime) throw new Error('No gameTime');

    const nanIssues = checkNoNaN(snapshot.stocks.slice(0, 5));
    if (nanIssues.length > 0) throw new Error(`NaN in stock data: ${nanIssues.join(', ')}`);

    const etfCount = snapshot.stocks.filter(s => s.traits?.includes('ETF')).length;
    const regularCount = snapshot.stocks.length - etfCount;
    pass(`NewGame → ${regularCount} stocks + ${etfCount} ETFs, gameTime=${snapshot.gameTime}`);

    // Check initial news
    if (snapshot.initialNews?.length > 0) {
      pass(`Initial news: ${snapshot.initialNews.length} events at game start`);
    } else {
      fail('Initial news', 'No initial news in MarketSnapshot');
    }
  } catch (e) {
    fail('NewGame', e.message);
    return;
  }

  // === TEST 4: Get Portfolio ===
  try {
    send('GetPortfolio');
    const portfolio = await waitFor('PortfolioUpdate', 5000);
    if (portfolio.cash !== 50000) throw new Error(`Expected $50000, got $${portfolio.cash}`);
    pass(`Portfolio: $${portfolio.cash} cash`);
  } catch (e) {
    fail('GetPortfolio', e.message);
  }

  // === TEST 5: Select a stock and get data ===
  const testStock = snapshot.stocks.find(s => !s.traits?.includes('ETF'));
  if (testStock) {
    try {
      send('GetOHLCV', { symbol: testStock.symbol });
      const ohlcv = await waitFor('OHLCVUpdate', 5000);
      if (!ohlcv.candles || ohlcv.candles.length === 0) throw new Error('No candle data');
      pass(`OHLCV for ${testStock.symbol}: ${ohlcv.candles.length} candles`);
    } catch (e) {
      fail('GetOHLCV', e.message);
    }

    // === TEST 6: Options Chain ===
    try {
      send('GetOptionsChain', { Symbol: testStock.symbol });
      const chain = await waitFor('OptionsChain', 5000);
      if (chain.noChain) {
        log(`  ⚠️  ${testStock.symbol} has no options chain (MCap too low)`);
        // Try a larger stock
        const bigStock = snapshot.stocks
          .filter(s => !s.traits?.includes('ETF'))
          .sort((a, b) => b.marketCap - a.marketCap)[0];
        if (bigStock) {
          send('GetOptionsChain', { Symbol: bigStock.symbol });
          const chain2 = await waitFor('OptionsChain', 5000);
          if (chain2.noChain) {
            fail('Options Chain', 'No stock has options');
          } else {
            const nanIssues = checkNoNaN(chain2.slices?.[0]?.calls?.slice(0, 3));
            if (nanIssues.length > 0) fail('Options Chain NaN', nanIssues.join(', '));
            else pass(`Options Chain: ${bigStock.symbol} — ${chain2.slices?.length} expirations, ${chain2.slices?.[0]?.strikes?.length} strikes`);
          }
        }
      } else {
        const nanIssues = checkNoNaN(chain.slices?.[0]?.calls?.slice(0, 3));
        if (nanIssues.length > 0) fail('Options Chain NaN', nanIssues.join(', '));
        else pass(`Options Chain: ${testStock.symbol} — ${chain.slices?.length} expirations`);
      }
    } catch (e) {
      fail('Options Chain', e.message);
    }

    // === TEST 7: Place a Buy Order ===
    try {
      send('PlaceOrder', {
        symbol: testStock.symbol,
        side: 'Buy',
        type: 'Market',
        quantity: 10,
      });
      const result = await waitFor('OrderResult', 5000);
      if (!result.success) throw new Error(result.message || 'Order rejected');
      pass(`Buy 10 ${testStock.symbol} @ $${result.order?.fillPrice ?? '?'}`);
    } catch (e) {
      fail('Buy Order', e.message);
    }

    // === TEST 8: Sell Order ===
    try {
      send('PlaceOrder', {
        symbol: testStock.symbol,
        side: 'Sell',
        type: 'Market',
        quantity: 5,
      });
      const result = await waitFor('OrderResult', 5000);
      if (!result.success) throw new Error(result.message || 'Order rejected');
      pass(`Sell 5 ${testStock.symbol}`);
    } catch (e) {
      fail('Sell Order', e.message);
    }
  }

  // === TEST 9: Run simulation for a few seconds (collect events) ===
  try {
    send('SetSpeed', { speed: 10 }); // Maximum speed
    const speedMsg = await waitFor('SpeedChanged', 3000);
    pass(`Speed set to ${speedMsg.speed}`);

    log('Running simulation for 8 seconds at max speed (waiting for market open)...');
    const msgs = await collectMessages(8000);

    const types = {};
    msgs.forEach(m => { types[m.type] = (types[m.type] || 0) + 1; });

    const newsCount = types['NewsEvents'] || 0;
    const priceUpdates = types['MarketUpdate'] || 0;
    const errors = msgs.filter(m => m.type === 'error');

    if (errors.length > 0) {
      fail('Simulation errors', errors.map(e => e.payload?.message).join('; '));
    } else {
      pass(`Simulation: ${priceUpdates} price updates, ${newsCount} news batches, ${Object.keys(types).length} msg types`);
    }

    // Check for NaN in price updates
    const priceMsg = msgs.find(m => m.type === 'MarketUpdate');
    if (priceMsg) {
      const nanIssues = checkNoNaN(priceMsg.payload);
      if (nanIssues.length > 0) fail('Price update NaN', nanIssues.slice(0, 5).join(', '));
      else pass('Price updates: no NaN values');
    }

    log(`  Message types: ${JSON.stringify(types)}`);

    // Analyze news content
    const allNews = msgs.filter(m => m.type === 'NewsEvents');
    const newsEvents = allNews.flatMap(m => m.payload?.events ?? []);
    const newsByType = {};
    newsEvents.forEach(e => { newsByType[e.type] = (newsByType[e.type] || 0) + 1; });
    log(`  News breakdown: ${JSON.stringify(newsByType)} (${newsEvents.length} total events)`);

    // At 10x speed, 8 seconds ≈ 80 game minutes ≈ first trading day
    // Multiple engines fire at market open, so 30-60 events is normal for day 1
    if (newsEvents.length > 100) {
      fail('News spam check', `${newsEvents.length} events in sim window is too many`);
    } else {
      pass(`News volume: ${newsEvents.length} events in sim window (acceptable)`);
    }
  } catch (e) {
    fail('Simulation run', e.message);
  }

  // === TEST 10: Economic Data ===
  try {
    send('GetEconomicData');
    const econ = await waitFor('EconomicData', 5000);
    if (!econ.indicators) throw new Error('No indicators');
    const ind = econ.indicators;
    if (ind.dollarIndex === undefined) throw new Error('Missing dollarIndex');
    if (ind.policyStance === undefined) throw new Error('Missing policyStance');
    pass(`Economic: Rate=${ind.interestRate}%, DXY=${ind.dollarIndex}, Policy=${ind.policyStance}, VIX=${econ.vix}`);
  } catch (e) {
    fail('Economic Data', e.message);
  }

  // === TEST 11: Save Game ===
  try {
    send('SaveGame', { SlotName: 'playtest_auto' });
    const saved = await waitFor('GameSaved', 5000);
    if (!saved.success) throw new Error(saved.error || 'Save failed');
    pass(`Game saved: ${saved.slot}`);
  } catch (e) {
    fail('Save Game', e.message);
  }

  // === TEST 12: Load Game ===
  try {
    send('LoadGame', { SlotName: 'playtest_auto' });
    // Should receive MarketSnapshot + PortfolioUpdate + GameLoaded
    const loadedSnapshot = await waitFor('MarketSnapshot', 10000);
    const loadResult = await waitFor('GameLoaded', 5000);
    if (!loadResult.success) throw new Error(loadResult.error || 'Load failed');
    if (!loadedSnapshot.stocks || loadedSnapshot.stocks.length === 0) throw new Error('No stocks after load');

    // Check initial news are NOT included in loaded snapshot (should be cleared)
    if (loadedSnapshot.initialNews?.length > 0) {
      fail('Load clears initial news', `Got ${loadedSnapshot.initialNews.length} stale initial news`);
    } else {
      pass('Load: no stale initial news in snapshot');
    }

    pass(`Game loaded: ${loadedSnapshot.stocks.length} stocks`);
  } catch (e) {
    fail('Load Game', e.message);
  }

  // === TEST 13: Earnings Calendar ===
  try {
    send('GetEarningsCalendar');
    const cal = await waitFor('EarningsCalendar', 5000);
    pass(`Earnings Calendar: ${cal.upcoming?.length ?? 0} upcoming, ${cal.recent?.length ?? 0} recent`);
  } catch (e) {
    fail('Earnings Calendar', e.message);
  }

  // === TEST 14: Stock Fundamentals ===
  if (testStock) {
    try {
      send('GetStockFundamentals', { symbol: testStock.symbol });
      const fund = await waitFor('StockFundamentals', 5000);
      if (!fund.symbol) throw new Error('No symbol in response');
      pass(`Fundamentals ${fund.symbol}: Revenue=$${fund.revenue}, P/E=${fund.peRatio}`);
    } catch (e) {
      fail('Stock Fundamentals', e.message);
    }
  }

  // === RESULTS ===
  send('SetSpeed', { speed: 0 }); // Pause
  ws.close();

  console.log('\n' + '='.repeat(50));
  console.log(`PLAYTEST RESULTS: ${passCount}/${testCount} passed, ${failCount} failed\n`);
  results.forEach(r => console.log(r));
  console.log('='.repeat(50) + '\n');

  process.exit(failCount > 0 ? 1 : 0);
}

// Start backend, wait for it, then run tests
async function main() {
  console.log('Starting backend...');
  backend = spawn('dotnet', ['run'], {
    cwd: 'backend/StockSim.Engine',
    stdio: ['pipe', 'pipe', 'pipe'],
  });

  // Wait for backend to be ready and discover port from stdout
  let discoveredPort = 0;
  await new Promise((resolve) => {
    const outputHandler = (data) => {
      const line = data.toString();
      process.stderr.write(line); // Echo backend output
      // Look for port in STOCKSIM_PORT=XXXXX or "listening on ws://127.0.0.1:XXXXX"
      const portMatch = line.match(/STOCKSIM_PORT=(\d+)/) || line.match(/listening on ws:\/\/127\.0\.0\.1:(\d+)/);
      if (portMatch) {
        discoveredPort = parseInt(portMatch[1]);
        console.log(`\nDiscovered backend port: ${discoveredPort}`);
      }
      if (line.includes('listening') || line.includes('STOCKSIM_PORT')) {
        setTimeout(resolve, 500); // Give it a moment after announcing ready
      }
    };
    backend.stdout.on('data', outputHandler);
    backend.stderr.on('data', outputHandler);
    setTimeout(() => { console.log('Backend startup timeout — proceeding'); resolve(); }, 10000);
  });

  if (!discoveredPort) {
    console.error('Could not discover backend port. Aborting.');
    backend.kill();
    process.exit(1);
  }

  PORT = discoveredPort;
  try {
    await runTests();
  } catch (e) {
    console.error('Playtest crashed:', e);
    process.exit(1);
  } finally {
    backend.kill();
  }
}

main();
