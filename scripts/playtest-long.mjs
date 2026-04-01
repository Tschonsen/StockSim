#!/usr/bin/env node
/**
 * Long Playtest: Simulates ~1 game year at max speed.
 * Monitors: crashes, NaN, news quality, economic transitions, earnings, options, meme stocks.
 * Performs trades throughout the session.
 */
import WebSocket from 'ws';

const PORT = parseInt(process.argv[2] || '63944');
let ws;
const stats = {
  tickUpdates: 0,
  newsBatches: 0,
  newsEvents: 0,
  newsByType: {},
  newsHeadlines: [],
  priceUpdates: 0,
  nanDetected: 0,
  errors: [],
  trades: { buys: 0, sells: 0, rejected: 0 },
  economicSnapshots: [],
  earnings: 0,
  guidance: 0,
  dividends: 0,
  policyChanges: [],
  optionsNews: 0,
  memeNews: 0,
  rebalanceNews: 0,
  cascadeNews: 0,
  gexNews: 0,
  daySummaries: 0,
  maxNewsPerDay: 0,
  currentDayNews: 0,
  lastGameDate: '',
};

function send(type, payload = {}) {
  ws.send(JSON.stringify({ type, payload }));
}

function waitFor(msgType, timeoutMs = 15000) {
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => reject(new Error(`Timeout: ${msgType}`)), timeoutMs);
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

function checkNaN(obj, path = '') {
  if (obj === null || obj === undefined) return 0;
  if (typeof obj === 'number' && isNaN(obj)) { stats.nanDetected++; return 1; }
  if (typeof obj === 'string' && obj === 'NaN') { stats.nanDetected++; return 1; }
  if (Array.isArray(obj)) return obj.reduce((c, item, i) => c + checkNaN(item, `${path}[${i}]`), 0);
  if (typeof obj === 'object') return Object.entries(obj).reduce((c, [k, v]) => c + checkNaN(v, `${path}.${k}`), 0);
  return 0;
}

async function main() {
  console.log(`\n🎮 StockSim LONG PLAYTEST (port ${PORT})\n`);
  console.log('Connecting...');

  ws = new WebSocket(`ws://127.0.0.1:${PORT}`);
  await new Promise((resolve, reject) => {
    ws.on('open', resolve);
    ws.on('error', reject);
    setTimeout(() => reject(new Error('Connect timeout')), 5000);
  });

  send('hello');
  await waitFor('welcome', 5000);
  console.log('✅ Connected\n');

  // Start game
  send('NewGame', { StockCount: 50, StartingCash: 100000, EnableMargin: true, EnableEvents: true });
  const snapshot = await waitFor('MarketSnapshot', 20000);
  const stocks = snapshot.stocks.filter(s => !s.traits?.includes('ETF'));
  console.log(`✅ Game started: ${snapshot.stocks.length} stocks (${stocks.length} regular)\n`);

  // Collect portfolio
  send('GetPortfolio');
  const portfolio = await waitFor('PortfolioUpdate', 5000);
  console.log(`💰 Starting cash: $${portfolio.cash}\n`);

  // Set up continuous message handler
  let tradeQueue = [];
  let gameDay = 0;

  ws.on('message', (raw) => {
    try {
      const msg = JSON.parse(raw.toString());
      checkNaN(msg.payload);

      switch (msg.type) {
        case 'MarketUpdate': {
          stats.priceUpdates++;
          const updates = msg.payload?.updates ?? msg.payload?.stocks ?? [];
          if (Array.isArray(updates)) updates.forEach(u => checkNaN(u));
          break;
        }
        case 'NewsEvents': {
          stats.newsBatches++;
          const events = msg.payload?.events ?? [];
          stats.newsEvents += events.length;
          stats.currentDayNews += events.length;
          events.forEach(e => {
            stats.newsByType[e.type] = (stats.newsByType[e.type] || 0) + 1;
            // Track special news
            if (e.headline?.includes('guidance') || e.headline?.includes('Guidance') || e.headline?.includes('raises Q') || e.headline?.includes('lowers Q') || e.headline?.includes('withdraws forward')) stats.guidance++;
            if (e.headline?.includes('MARGIN ALERT') || e.headline?.includes('margin calls')) stats.cascadeNews++;
            if (e.headline?.includes('MEME') || e.headline?.includes('meme') || e.headline?.includes('SHORT SQUEEZE') || e.headline?.includes('diamond hands')) stats.memeNews++;
            if (e.headline?.includes('REBALANCING') || e.headline?.includes('added to') || e.headline?.includes('removed from')) stats.rebalanceNews++;
            if (e.headline?.includes('Gamma') || e.headline?.includes('GEX') || e.headline?.includes('gamma')) stats.gexNews++;
            if (e.headline?.includes('Options') || e.headline?.includes('IV Crush') || e.headline?.includes('Pin Risk') || e.headline?.includes('Unusual Options')) stats.optionsNews++;
            if (e.headline?.includes('FOMC') || e.headline?.includes('Fed ') || e.headline?.includes('QE') || e.headline?.includes('tapering')) stats.policyChanges.push(e.headline);
            // Keep last 10 headlines for inspection
            if (stats.newsHeadlines.length < 200) stats.newsHeadlines.push(e.headline);
          });
          break;
        }
        case 'DaySummary': {
          stats.daySummaries++;
          gameDay++;
          // Track max news per day
          if (stats.currentDayNews > stats.maxNewsPerDay) stats.maxNewsPerDay = stats.currentDayNews;
          stats.currentDayNews = 0;

          // Log progress every 10 days
          if (gameDay % 10 === 0) {
            process.stdout.write(`  Day ${gameDay}: ${stats.newsEvents} news, ${stats.priceUpdates} prices, ${stats.trades.buys}B/${stats.trades.sells}S trades\r`);
          }

          // Make trades periodically
          if (gameDay % 3 === 0 && tradeQueue.length > 0) {
            const stock = tradeQueue[gameDay % tradeQueue.length];
            send('PlaceOrder', { symbol: stock, side: 'Buy', type: 'Market', quantity: 5 + Math.floor(Math.random() * 20) });
          }
          if (gameDay % 7 === 0 && tradeQueue.length > 0) {
            const stock = tradeQueue[(gameDay + 3) % tradeQueue.length];
            send('PlaceOrder', { symbol: stock, side: 'Sell', type: 'Market', quantity: 5 });
          }
          break;
        }
        case 'OrderResult': {
          const r = msg.payload;
          if (r.success) {
            if (r.order?.side === 'Buy' || r.order?.side === 'Cover') stats.trades.buys++;
            else stats.trades.sells++;
          } else {
            stats.trades.rejected++;
          }
          break;
        }
        case 'error': {
          stats.errors.push(msg.payload?.message || 'Unknown error');
          break;
        }
      }
    } catch {}
  });

  // Pick 8 stocks to trade
  tradeQueue = stocks.slice(0, 8).map(s => s.symbol);
  console.log(`📊 Trading: ${tradeQueue.join(', ')}\n`);

  // Initial buys
  for (const sym of tradeQueue.slice(0, 4)) {
    send('PlaceOrder', { symbol: sym, side: 'Buy', type: 'Market', quantity: 10 });
  }

  // Set max speed
  send('SetSpeed', { speed: 10 });
  console.log('⏩ Speed set to 10x — simulating ~1 game year...\n');

  // Run for 120 seconds at max speed
  // At 10x: ~1 tick/100ms = ~10 ticks/s → 390 ticks/day → ~39s/day → ~3 days in 120s
  const SIM_DURATION_MS = 120000;
  await new Promise(resolve => setTimeout(resolve, SIM_DURATION_MS));

  // Pause and collect final state
  send('SetSpeed', { speed: 0 });
  await new Promise(resolve => setTimeout(resolve, 500));

  // Get final economic data
  send('GetEconomicData');
  try {
    const econ = await waitFor('EconomicData', 5000);
    stats.economicSnapshots.push({
      rate: econ.indicators?.interestRate,
      inflation: econ.indicators?.inflationRate,
      dxy: econ.indicators?.dollarIndex,
      policy: econ.indicators?.policyStance,
      vix: econ.vix,
      gdp: econ.indicators?.gdpGrowth,
    });
  } catch {}

  // Get final portfolio
  send('GetPortfolio');
  let finalPortfolio;
  try {
    finalPortfolio = await waitFor('PortfolioUpdate', 5000);
  } catch {}

  // Save game
  send('SaveGame', { SaveName: `Playtest Day ${gameDay}` });
  try { await waitFor('GameSaved', 5000); } catch {}

  ws.close();

  // === REPORT ===
  console.log('\n\n' + '='.repeat(60));
  console.log('📋 LONG PLAYTEST REPORT');
  console.log('='.repeat(60));

  console.log(`\n⏱  Duration: ${SIM_DURATION_MS / 1000}s real → ${gameDay} trading days simulated`);
  console.log(`📊 Price updates: ${stats.priceUpdates}`);
  console.log(`📰 News: ${stats.newsEvents} events in ${stats.newsBatches} batches`);
  console.log(`   By type: ${JSON.stringify(stats.newsByType)}`);
  console.log(`   Max news/day: ${stats.maxNewsPerDay}`);
  console.log(`   Avg news/day: ${gameDay > 0 ? (stats.newsEvents / gameDay).toFixed(1) : '?'}`);

  console.log(`\n📰 Special events:`);
  console.log(`   Earnings Guidance: ${stats.guidance}`);
  console.log(`   Options news: ${stats.optionsNews}`);
  console.log(`   GEX alerts: ${stats.gexNews}`);
  console.log(`   Margin cascades: ${stats.cascadeNews}`);
  console.log(`   Meme stock: ${stats.memeNews}`);
  console.log(`   Index rebalancing: ${stats.rebalanceNews}`);
  console.log(`   Fed policy: ${stats.policyChanges.length}`);
  if (stats.policyChanges.length > 0) {
    stats.policyChanges.forEach(h => console.log(`     → ${h}`));
  }

  console.log(`\n💹 Trades: ${stats.trades.buys} buys, ${stats.trades.sells} sells, ${stats.trades.rejected} rejected`);
  if (finalPortfolio) {
    console.log(`💰 Final: Cash=$${finalPortfolio.cash?.toFixed(0)}, Equity=$${finalPortfolio.totalEquity?.toFixed(0)}, P&L=$${finalPortfolio.realizedPnL?.toFixed(0)}`);
  }

  if (stats.economicSnapshots.length > 0) {
    const e = stats.economicSnapshots[0];
    console.log(`\n🏦 Economy: Rate=${e.rate?.toFixed(2)}%, Inflation=${e.inflation?.toFixed(2)}%, GDP=${e.gdp?.toFixed(2)}%`);
    console.log(`   DXY=${e.dxy?.toFixed(1)}, Policy=${e.policy}, VIX=${e.vix?.toFixed(1)}`);
  }

  console.log(`\n⚠️  NaN detected: ${stats.nanDetected}`);
  console.log(`❌ Errors: ${stats.errors.length}`);
  stats.errors.forEach(e => console.log(`   → ${e}`));

  // Sample headlines
  console.log(`\n📝 Sample headlines (first 15):`);
  stats.newsHeadlines.slice(0, 15).forEach(h => console.log(`   • ${h}`));

  console.log('\n' + '='.repeat(60));
  const issues = [];
  if (stats.nanDetected > 0) issues.push(`${stats.nanDetected} NaN values`);
  if (stats.errors.length > 0) issues.push(`${stats.errors.length} errors`);
  if (gameDay < 50) issues.push(`Only ${gameDay} days simulated (expected 200+)`);
  if (stats.maxNewsPerDay > 30) issues.push(`Max ${stats.maxNewsPerDay} news/day (high)`);

  if (issues.length === 0) {
    console.log('✅ PLAYTEST PASSED — no issues detected');
  } else {
    console.log(`⚠️  ISSUES: ${issues.join(', ')}`);
  }
  console.log('='.repeat(60) + '\n');

  process.exit(issues.length > 0 ? 1 : 0);
}

main().catch(e => { console.error('Playtest crashed:', e); process.exit(1); });
