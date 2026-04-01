#!/usr/bin/env node
/**
 * Trader Playtest: Act like a real trader over 60+ game days.
 * Buy dips, sell rips, trade earnings, check fundamentals, monitor news.
 * Report on market realism, price behavior, news quality, economic cycles.
 */
import WebSocket from 'ws';
import { spawn } from 'child_process';

let ws, backend;
const portfolio = { cash: 0, equity: 0, positions: [] };
const tradeLog = [];
const newsLog = [];
const priceHistory = {};  // symbol → [{day, price}]
const dailySnapshots = [];
const economicHistory = [];
let gameDay = 0;
let gameDate = '';
let currentStocks = new Map();

function send(t, p = {}) { ws.send(JSON.stringify({ type: t, payload: p })); }
function waitFor(t, ms = 15000) {
  return new Promise((res, rej) => {
    const tm = setTimeout(() => rej(new Error('timeout:' + t)), ms);
    const h = (raw) => {
      try {
        const m = JSON.parse(raw.toString());
        if (m.type === t) { clearTimeout(tm); ws.removeListener('message', h); res(m.payload); }
      } catch {}
    };
    ws.on('message', h);
  });
}

function sleep(ms) { return new Promise(r => setTimeout(r, ms)); }

async function getPortfolio() {
  send('GetPortfolio');
  const p = await waitFor('PortfolioUpdate');
  portfolio.cash = p.cash;
  portfolio.equity = p.totalEquity;
  portfolio.positions = p.positions || [];
  return p;
}

async function getEconomic() {
  send('GetEconomicData');
  return await waitFor('EconomicData');
}

async function buy(symbol, qty) {
  send('PlaceOrder', { symbol, side: 'Buy', type: 'Market', quantity: qty });
  const r = await waitFor('OrderResult');
  if (r.success) {
    tradeLog.push({ day: gameDay, action: 'BUY', symbol, qty, price: r.order.fillPrice });
    return r.order.fillPrice;
  }
  return null;
}

async function sell(symbol, qty) {
  send('PlaceOrder', { symbol, side: 'Sell', type: 'Market', quantity: qty });
  const r = await waitFor('OrderResult');
  if (r.success) {
    tradeLog.push({ day: gameDay, action: 'SELL', symbol, qty, price: r.order.fillPrice });
    return r.order.fillPrice;
  }
  return null;
}

async function runDay() {
  // Collect messages for one game day
  return new Promise((resolve) => {
    const dayNews = [];
    const handler = (raw) => {
      try {
        const m = JSON.parse(raw.toString());
        if (m.type === 'DaySummary') {
          ws.removeListener('message', handler);
          gameDay++;
          resolve({ news: dayNews, summary: m.payload });
        }
        if (m.type === 'NewsEvents') {
          const events = m.payload?.events || [];
          events.forEach(e => {
            dayNews.push(e);
            newsLog.push({ day: gameDay, headline: e.headline, type: e.type, severity: e.severity });
          });
        }
        if (m.type === 'MarketUpdate') {
          const updates = m.payload?.updates || [];
          updates.forEach(u => {
            currentStocks.set(u.Symbol || u.symbol, u);
          });
        }
      } catch {}
    };
    ws.on('message', handler);
    // Timeout: at 10x speed with 30 stocks, a day takes ~20-25s
    setTimeout(() => { ws.removeListener('message', handler); resolve({ news: dayNews, timeout: true }); }, 60000);
  });
}

async function main() {
  // Start backend
  console.log('Starting backend...');
  backend = spawn('dotnet', ['run'], { cwd: 'backend/StockSim.Engine', stdio: ['pipe', 'pipe', 'pipe'] });
  let port = 0;
  await new Promise((resolve) => {
    backend.stdout.on('data', (d) => {
      const m = d.toString().match(/free port (\d+)/);
      if (m) port = parseInt(m[1]);
      if (d.toString().includes('READY')) setTimeout(resolve, 500);
    });
    setTimeout(resolve, 8000);
  });

  ws = new WebSocket(`ws://127.0.0.1:${port}`);
  await new Promise(r => ws.on('open', r));
  send('hello');
  await waitFor('welcome');

  // Start game: 30 stocks for faster sim
  send('NewGame', { StockCount: 30, StartingCash: 100000, EnableShortSelling: true, EnableMargin: true });
  const snap = await waitFor('MarketSnapshot');
  const stocks = snap.stocks.filter(s => !s.traits?.includes('ETF'));
  gameDate = snap.gameTime;
  console.log(`Game started: ${stocks.length} stocks, $100,000\n`);

  // Record initial prices
  stocks.forEach(s => {
    priceHistory[s.symbol] = [{ day: 0, price: s.price }];
    currentStocks.set(s.symbol, s);
  });

  // Get initial economic state
  const econ0 = await getEconomic();
  economicHistory.push({ day: 0, ...econ0.indicators, vix: econ0.vix, fearGreed: econ0.fearGreedIndex });

  // === TRADING STRATEGY ===
  // Phase 1 (Day 1-5): Buy diversified portfolio
  // Phase 2 (Day 6-30): Monitor, trade on news
  // Phase 3 (Day 31-60): Active trading, check earnings, rebalance

  send('SetSpeed', { speed: 10 }); // Max speed

  console.log('📈 PHASE 1: Building initial portfolio (Day 1-5)\n');

  // Pick 6 stocks across sectors for diversification
  const sectors = [...new Set(stocks.map(s => s.sector))];
  const picks = [];
  for (const sector of sectors.slice(0, 6)) {
    const sectorStocks = stocks.filter(s => s.sector === sector).sort((a, b) => b.marketCap - a.marketCap);
    if (sectorStocks.length > 0) picks.push(sectorStocks[0]);
  }

  // Run first few days, buy on day 1
  for (let d = 0; d < 5; d++) {
    const dayResult = await runDay();
    if (dayResult.timeout) { console.log('  (day timeout - market may be closed)'); continue; }

    // Record prices
    for (const [sym, s] of currentStocks) {
      if (!priceHistory[sym]) priceHistory[sym] = [];
      priceHistory[sym].push({ day: gameDay, price: s.price || s.Price });
    }

    if (d === 1) {
      // Buy initial positions (day 1, market should be open now)
      for (const stock of picks) {
        const qty = Math.floor(15000 / stock.price); // ~$15k per position
        if (qty > 0) {
          const price = await buy(stock.symbol, qty);
          if (price) console.log(`  Bought ${qty} ${stock.symbol} @ $${price.toFixed(2)} (${stock.sector})`);
        }
      }
      await getPortfolio();
      console.log(`  Portfolio: Cash $${portfolio.cash.toFixed(0)}, Equity $${portfolio.equity.toFixed(0)}, ${portfolio.positions.length} positions`);
    }

    if (dayResult.news.length > 0) {
      console.log(`  Day ${gameDay}: ${dayResult.news.length} news`);
    }
  }

  console.log('\n📊 PHASE 2: Monitor & React (Day 6-30)\n');

  for (let d = 0; d < 25; d++) {
    const dayResult = await runDay();
    if (dayResult.timeout) continue;

    // Record prices
    for (const [sym, s] of currentStocks) {
      if (!priceHistory[sym]) priceHistory[sym] = [];
      priceHistory[sym].push({ day: gameDay, price: s.price || s.Price });
    }

    // Check portfolio every 5 days
    if (gameDay % 5 === 0) {
      await getPortfolio();
      const econ = await getEconomic();
      economicHistory.push({ day: gameDay, ...econ.indicators, vix: econ.vix, fearGreed: econ.fearGreedIndex });

      const pnl = portfolio.equity - 100000;
      const pnlPct = (pnl / 100000 * 100).toFixed(2);
      console.log(`  Day ${gameDay}: Equity $${portfolio.equity.toFixed(0)} (${pnl >= 0 ? '+' : ''}$${pnl.toFixed(0)}, ${pnlPct}%) | VIX ${econ.vix?.toFixed(1)} | Rate ${econ.indicators?.interestRate?.toFixed(2)}% | DXY ${econ.indicators?.dollarIndex?.toFixed(1)}`);
    }

    // React to news: buy dips on major negative company events
    for (const news of dayResult.news) {
      if (news.severity === 'Major' && news.sentiment < -0.3 && news.affectedSymbols?.length > 0) {
        const sym = news.affectedSymbols[0];
        const stock = currentStocks.get(sym);
        if (stock && portfolio.cash > 5000) {
          const qty = Math.floor(5000 / (stock.price || stock.Price || 100));
          if (qty > 0) {
            const price = await buy(sym, qty);
            if (price) console.log(`  📰 Dip buy: ${qty} ${sym} @ $${price.toFixed(2)} on: "${news.headline.slice(0, 60)}..."`);
          }
        }
      }
    }

    // Sell winners (>15% gain)
    if (gameDay % 7 === 0) {
      await getPortfolio();
      for (const pos of portfolio.positions) {
        if (pos.unrealizedPnLPercent > 15) {
          const qty = Math.floor(Math.abs(pos.shares) / 2); // Sell half
          if (qty > 0) {
            const price = await sell(pos.symbol, qty);
            if (price) console.log(`  💰 Profit take: ${qty} ${pos.symbol} @ $${price.toFixed(2)} (${pos.unrealizedPnLPercent.toFixed(1)}% gain)`);
          }
        }
      }
    }
  }

  console.log('\n🔥 PHASE 3: Active Trading (Day 31-60)\n');

  for (let d = 0; d < 30; d++) {
    const dayResult = await runDay();
    if (dayResult.timeout) continue;

    // Record prices
    for (const [sym, s] of currentStocks) {
      if (!priceHistory[sym]) priceHistory[sym] = [];
      priceHistory[sym].push({ day: gameDay, price: s.price || s.Price });
    }

    if (gameDay % 10 === 0) {
      await getPortfolio();
      const econ = await getEconomic();
      economicHistory.push({ day: gameDay, ...econ.indicators, vix: econ.vix, fearGreed: econ.fearGreedIndex });

      const pnl = portfolio.equity - 100000;
      console.log(`  Day ${gameDay}: Equity $${portfolio.equity.toFixed(0)} (${pnl >= 0 ? '+' : ''}$${pnl.toFixed(0)}) | VIX ${econ.vix?.toFixed(1)} | Policy ${econ.indicators?.policyStance} | F&G ${econ.fearGreedIndex}`);
    }

    // Momentum strategy: buy stocks that are up >3% today
    if (gameDay % 3 === 0 && portfolio.cash > 3000) {
      for (const [sym, s] of currentStocks) {
        const changePct = s.changePercent || s.ChangePercent || 0;
        if (changePct > 3 && portfolio.cash > 3000) {
          const price = s.price || s.Price || 100;
          const qty = Math.floor(3000 / price);
          if (qty > 0) {
            const fill = await buy(sym, qty);
            if (fill) console.log(`  🚀 Momentum: ${qty} ${sym} @ $${fill.toFixed(2)} (up ${changePct.toFixed(1)}% today)`);
            break; // One momentum trade per cycle
          }
        }
      }
    }

    // Cut losers: sell positions down >10%
    if (gameDay % 10 === 0) {
      await getPortfolio();
      for (const pos of portfolio.positions) {
        if (pos.unrealizedPnLPercent < -10 && pos.shares > 0) {
          const price = await sell(pos.symbol, Math.abs(pos.shares));
          if (price) console.log(`  ✂️  Cut loss: ${pos.symbol} @ $${price.toFixed(2)} (${pos.unrealizedPnLPercent.toFixed(1)}% loss)`);
        }
      }
    }
  }

  // === FINAL STATE ===
  send('SetSpeed', { speed: 0 });
  await sleep(500);
  await getPortfolio();
  const finalEcon = await getEconomic();
  economicHistory.push({ day: gameDay, ...finalEcon.indicators, vix: finalEcon.vix, fearGreed: finalEcon.fearGreedIndex });

  // === ANALYSIS ===
  console.log('\n' + '='.repeat(70));
  console.log('📋 TRADER PLAYTEST REPORT');
  console.log('='.repeat(70));

  console.log(`\n⏱  Simulated: ${gameDay} trading days`);
  const finalPnl = portfolio.equity - 100000;
  const finalPnlPct = (finalPnl / 100000 * 100).toFixed(2);
  console.log(`💰 Final Equity: $${portfolio.equity.toFixed(0)} (${finalPnl >= 0 ? '+' : ''}$${finalPnl.toFixed(0)}, ${finalPnlPct}%)`);
  console.log(`💵 Cash: $${portfolio.cash.toFixed(0)}`);
  console.log(`📊 Positions: ${portfolio.positions.length}`);
  console.log(`📝 Total trades: ${tradeLog.length}`);

  // Price movement analysis
  console.log('\n📈 PRICE REALISM:');
  let gainers = 0, losers = 0, bigMovers = 0;
  const returns = [];
  for (const [sym, history] of Object.entries(priceHistory)) {
    if (history.length < 2) continue;
    const first = history[0].price;
    const last = history[history.length - 1].price;
    if (!first || !last) continue;
    const ret = (last - first) / first * 100;
    returns.push({ sym, ret, first, last });
    if (ret > 0) gainers++;
    else losers++;
    if (Math.abs(ret) > 20) bigMovers++;
  }
  returns.sort((a, b) => b.ret - a.ret);
  console.log(`  Gainers: ${gainers} | Losers: ${losers} | Big movers (>20%): ${bigMovers}`);
  console.log(`  Average return: ${(returns.reduce((s, r) => s + r.ret, 0) / returns.length).toFixed(2)}%`);
  console.log(`  Best:  ${returns[0]?.sym} ${returns[0]?.ret.toFixed(1)}% ($${returns[0]?.first.toFixed(2)} → $${returns[0]?.last.toFixed(2)})`);
  console.log(`  Worst: ${returns[returns.length - 1]?.sym} ${returns[returns.length - 1]?.ret.toFixed(1)}% ($${returns[returns.length - 1]?.first.toFixed(2)} → $${returns[returns.length - 1]?.last.toFixed(2)})`);

  // Distribution
  const buckets = { '<-10%': 0, '-10 to -5%': 0, '-5 to 0%': 0, '0 to 5%': 0, '5 to 10%': 0, '>10%': 0 };
  returns.forEach(r => {
    if (r.ret < -10) buckets['<-10%']++;
    else if (r.ret < -5) buckets['-10 to -5%']++;
    else if (r.ret < 0) buckets['-5 to 0%']++;
    else if (r.ret < 5) buckets['0 to 5%']++;
    else if (r.ret < 10) buckets['5 to 10%']++;
    else buckets['>10%']++;
  });
  console.log(`  Distribution: ${JSON.stringify(buckets)}`);

  // Economic evolution
  console.log('\n🏦 ECONOMIC EVOLUTION:');
  for (const e of economicHistory) {
    console.log(`  Day ${String(e.day).padStart(3)}: Rate=${e.interestRate?.toFixed(2)}% Infl=${e.inflationRate?.toFixed(2)}% GDP=${e.gdpGrowth?.toFixed(2)}% DXY=${e.dollarIndex?.toFixed(1)} VIX=${e.vix?.toFixed(1)} Policy=${e.policyStance} F&G=${e.fearGreed}`);
  }

  // News quality
  console.log('\n📰 NEWS QUALITY:');
  console.log(`  Total events: ${newsLog.length}`);
  const newsByType = {};
  newsLog.forEach(n => { newsByType[n.type] = (newsByType[n.type] || 0) + 1; });
  console.log(`  By type: ${JSON.stringify(newsByType)}`);
  console.log(`  Avg/day: ${(newsLog.length / Math.max(gameDay, 1)).toFixed(1)}`);

  // Sample headlines
  console.log('\n  Sample headlines:');
  const sampled = newsLog.filter((_, i) => i % Math.max(1, Math.floor(newsLog.length / 10)) === 0).slice(0, 12);
  sampled.forEach(n => console.log(`    [Day ${n.day}] [${n.type}] ${n.headline.slice(0, 100)}`));

  // Check for number-only nonsense
  const badHeadlines = newsLog.filter(n => /\b\d{1,2}\b [a-z]/.test(n.headline) && !n.headline.match(/Q\d|13[DF]|\d+%|\$\d|\d+x|\d+ month|\d+ day/));
  if (badHeadlines.length > 0) {
    console.log(`\n  ⚠️  Suspicious headlines (${badHeadlines.length}):`);
    badHeadlines.slice(0, 5).forEach(n => console.log(`    "${n.headline.slice(0, 100)}"`));
  }

  // Trade log
  console.log('\n💹 TRADE LOG:');
  tradeLog.forEach(t => console.log(`  Day ${String(t.day).padStart(3)}: ${t.action} ${t.qty} ${t.symbol} @ $${t.price.toFixed(2)}`));

  // Realism assessment
  console.log('\n' + '='.repeat(70));
  console.log('🎯 REALISM ASSESSMENT:');
  const avgRet = returns.reduce((s, r) => s + r.ret, 0) / returns.length;
  const stdDev = Math.sqrt(returns.reduce((s, r) => s + (r.ret - avgRet) ** 2, 0) / returns.length);

  const checks = [];
  if (avgRet > -5 && avgRet < 15) checks.push('✅ Average return reasonable (' + avgRet.toFixed(1) + '%)');
  else checks.push('⚠️  Average return extreme (' + avgRet.toFixed(1) + '%)');

  if (gainers > 0 && losers > 0) checks.push(`✅ Mix of gainers (${gainers}) and losers (${losers})`);
  else checks.push('⚠️  One-sided market');

  if (stdDev > 3 && stdDev < 30) checks.push('✅ Return dispersion realistic (σ=' + stdDev.toFixed(1) + '%)');
  else checks.push('⚠️  Return dispersion: σ=' + stdDev.toFixed(1) + '%');

  if (newsLog.length / Math.max(gameDay, 1) > 2 && newsLog.length / Math.max(gameDay, 1) < 20) checks.push('✅ News frequency OK (' + (newsLog.length / gameDay).toFixed(1) + '/day)');
  else checks.push('⚠️  News frequency: ' + (newsLog.length / Math.max(gameDay, 1)).toFixed(1) + '/day');

  const econChange = economicHistory.length > 1;
  if (econChange) checks.push('✅ Economic indicators evolving');
  else checks.push('⚠️  Economic indicators stale');

  checks.forEach(c => console.log('  ' + c));
  console.log('='.repeat(70) + '\n');

  ws.close();
  backend.kill();
  process.exit(0);
}

main().catch(e => { console.error('Crashed:', e.message); backend?.kill(); process.exit(1); });
