#!/usr/bin/env node
/**
 * Simple trader playtest: start game, run at max speed, collect all data passively, trade at intervals.
 */
import WebSocket from 'ws';
import { spawn } from 'child_process';

let ws;
const news = [];
const dayData = [];
let dayCount = 0;
let portfolio = {};
let stocks = new Map();
let initialPrices = {};

function send(t, p = {}) { ws.send(JSON.stringify({ type: t, payload: p })); }
function waitFor(t, ms = 15000) {
  return new Promise((res, rej) => {
    const tm = setTimeout(() => rej(new Error('timeout:' + t)), ms);
    const h = (raw) => { try { const m = JSON.parse(raw.toString()); if (m.type === t) { clearTimeout(tm); ws.removeListener('message', h); res(m.payload); } } catch {} };
    ws.on('message', h);
  });
}

console.log('Starting backend...');
const backend = spawn('dotnet', ['run'], { cwd: 'backend/StockSim.Engine', stdio: ['pipe', 'pipe', 'pipe'] });
let port = 0;
await new Promise(r => {
  backend.stdout.on('data', d => { const m = d.toString().match(/free port (\d+)/); if (m) port = parseInt(m[1]); if (d.toString().includes('READY')) setTimeout(r, 500); });
  setTimeout(r, 8000);
});

ws = new WebSocket(`ws://127.0.0.1:${port}`);
await new Promise(r => ws.on('open', r));
send('hello'); await waitFor('welcome');

send('NewGame', { StockCount: 30, StartingCash: 100000, EnableShortSelling: true });
const snap = await waitFor('MarketSnapshot');
snap.stocks.forEach(s => { if (!s.traits?.includes('ETF')) { initialPrices[s.symbol] = s.price; stocks.set(s.symbol, s); } });
console.log(`Game: ${Object.keys(initialPrices).length} stocks, $100k\n`);

// Passive listener — collect everything
ws.on('message', raw => {
  try {
    const m = JSON.parse(raw.toString());
    if (m.type === 'DaySummary') {
      dayCount++;
      dayData.push({ day: dayCount, ...m.payload });
    }
    if (m.type === 'NewsEvents') {
      (m.payload?.events || []).forEach(e => news.push({ day: dayCount, ...e }));
    }
    if (m.type === 'MarketUpdate') {
      (m.payload?.updates || []).forEach(u => stocks.set(u.Symbol || u.symbol, u));
    }
    if (m.type === 'PortfolioUpdate') {
      portfolio = m.payload;
    }
  } catch {}
});

// === STRATEGY: Buy on day 2, trade every 10 days ===
send('SetSpeed', { speed: 10 });
console.log('Running at 10x speed...\n');

// Wait for a few days to pass
const checkInterval = setInterval(async () => {
  process.stdout.write(`\r  Day ${dayCount}...`);

  // Buy on day 2
  if (dayCount === 2 && (portfolio.positions?.length || 0) === 0) {
    console.log('\n\n  Buying initial portfolio...');
    const symbols = Object.keys(initialPrices).slice(0, 6);
    for (const sym of symbols) {
      const price = initialPrices[sym];
      const qty = Math.floor(14000 / price);
      if (qty > 0) {
        send('PlaceOrder', { symbol: sym, side: 'Buy', type: 'Market', quantity: qty });
        console.log(`    BUY ${qty} ${sym}`);
      }
    }
    await new Promise(r => setTimeout(r, 2000));
    send('GetPortfolio');
  }

  // Trade on day 15, 25, 35, 45
  if ([15, 25, 35, 45].includes(dayCount) && portfolio.positions?.length > 0) {
    console.log(`\n\n  Day ${dayCount} rebalance:`);
    send('GetPortfolio');
    await new Promise(r => setTimeout(r, 500));

    for (const pos of portfolio.positions || []) {
      if (pos.unrealizedPnLPercent > 12) {
        const qty = Math.floor(Math.abs(pos.shares) / 2);
        if (qty > 0) {
          send('PlaceOrder', { symbol: pos.symbol, side: 'Sell', type: 'Market', quantity: qty });
          console.log(`    SELL ${qty} ${pos.symbol} (profit ${pos.unrealizedPnLPercent.toFixed(1)}%)`);
        }
      }
      if (pos.unrealizedPnLPercent < -12) {
        send('PlaceOrder', { symbol: pos.symbol, side: 'Sell', type: 'Market', quantity: Math.abs(pos.shares) });
        console.log(`    CUT ${pos.symbol} (loss ${pos.unrealizedPnLPercent.toFixed(1)}%)`);
      }
    }

    // Buy a new stock with free cash
    if (portfolio.cash > 10000) {
      const unbought = Object.keys(initialPrices).filter(s => !(portfolio.positions || []).find(p => p.symbol === s));
      if (unbought.length > 0) {
        const sym = unbought[Math.floor(Math.random() * unbought.length)];
        const price = stocks.get(sym)?.price || stocks.get(sym)?.Price || initialPrices[sym];
        const qty = Math.floor(10000 / price);
        if (qty > 0) {
          send('PlaceOrder', { symbol: sym, side: 'Buy', type: 'Market', quantity: qty });
          console.log(`    BUY ${qty} ${sym} (new position)`);
        }
      }
    }
  }

  // Stop after 60 days
  if (dayCount >= 60) {
    clearInterval(checkInterval);
    send('SetSpeed', { speed: 0 });
    await new Promise(r => setTimeout(r, 1000));
    send('GetPortfolio');
    await new Promise(r => setTimeout(r, 500));

    send('GetEconomicData');
    let econ;
    try { econ = await waitFor('EconomicData', 3000); } catch {}

    // === REPORT ===
    console.log('\n\n' + '='.repeat(70));
    console.log('📋 TRADER PLAYTEST REPORT — ' + dayCount + ' DAYS');
    console.log('='.repeat(70));

    const pnl = (portfolio.totalEquity || 100000) - 100000;
    console.log(`\n💰 Final: Equity $${(portfolio.totalEquity||0).toFixed(0)} | Cash $${(portfolio.cash||0).toFixed(0)} | P&L ${pnl >= 0 ? '+' : ''}$${pnl.toFixed(0)} (${(pnl/1000).toFixed(1)}%)`);
    console.log(`📊 Positions: ${(portfolio.positions||[]).length}`);
    (portfolio.positions||[]).forEach(p => console.log(`    ${p.symbol}: ${p.shares} shares, $${p.marketValue?.toFixed(0)} (${p.unrealizedPnLPercent?.toFixed(1)}%)`));

    console.log('\n📈 PRICE MOVEMENT (over ' + dayCount + ' days):');
    const returns = [];
    for (const [sym, init] of Object.entries(initialPrices)) {
      const current = stocks.get(sym);
      const curPrice = current?.price || current?.Price;
      if (curPrice) {
        const ret = (curPrice - init) / init * 100;
        returns.push({ sym, ret, from: init, to: curPrice });
      }
    }
    returns.sort((a, b) => b.ret - a.ret);
    const avg = returns.reduce((s, r) => s + r.ret, 0) / returns.length;
    console.log(`  Average: ${avg.toFixed(2)}%`);
    console.log(`  Gainers: ${returns.filter(r => r.ret > 0).length} | Losers: ${returns.filter(r => r.ret < 0).length}`);
    console.log(`  Top 5:`);
    returns.slice(0, 5).forEach(r => console.log(`    ${r.sym}: ${r.ret >= 0 ? '+' : ''}${r.ret.toFixed(1)}% ($${r.from.toFixed(2)} → $${r.to.toFixed(2)})`));
    console.log(`  Bottom 5:`);
    returns.slice(-5).forEach(r => console.log(`    ${r.sym}: ${r.ret >= 0 ? '+' : ''}${r.ret.toFixed(1)}% ($${r.from.toFixed(2)} → $${r.to.toFixed(2)})`));

    if (econ) {
      console.log(`\n🏦 Economy: Rate=${econ.indicators?.interestRate?.toFixed(2)}% Infl=${econ.indicators?.inflationRate?.toFixed(2)}% GDP=${econ.indicators?.gdpGrowth?.toFixed(2)}% DXY=${econ.indicators?.dollarIndex?.toFixed(1)} VIX=${econ.vix?.toFixed(1)} Policy=${econ.indicators?.policyStance}`);
    }

    console.log(`\n📰 News: ${news.length} total (${(news.length / dayCount).toFixed(1)}/day)`);
    const byType = {}; news.forEach(n => byType[n.type] = (byType[n.type]||0)+1);
    console.log(`  Types: ${JSON.stringify(byType)}`);
    console.log('  Sample:');
    news.filter((_,i) => i % Math.max(1, Math.floor(news.length/8)) === 0).slice(0,8).forEach(n =>
      console.log(`    [D${n.day}] ${n.headline?.slice(0,100)}`)
    );

    // Check for bad headlines
    const bad = news.filter(n => n.headline && (/\b\d{1,2}\s+[a-z]{3,}/.test(n.headline) && !n.headline.match(/Q\d|13[DF]|\d+%|\$\d|\d+ month|\d+ day|\d+x/)));
    if (bad.length > 0) console.log(`\n  ⚠️  ${bad.length} suspicious headlines found`);
    else console.log('\n  ✅ No suspicious headlines');

    console.log('\n' + '='.repeat(70));
    ws.close();
    backend.kill();
    process.exit(0);
  }
}, 5000);
