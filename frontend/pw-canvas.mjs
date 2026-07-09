// Drive the running app headlessly against the in-house Canvas chart engine (M1):
// skip tutorial → Market → open a stock → max speed → watch the canvas render + tick.
import { chromium } from 'playwright';

const logs = [];
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1500, height: 950 } });
page.on('console', (m) => logs.push(`${m.text()}`));
page.on('pageerror', (e) => logs.push(`[pageerror] ${e.message}`));
const shot = (n) => page.screenshot({ path: `D:/Dev/projects/StockSim/frontend/pw-canvas-${n}.png` });

await page.goto('http://127.0.0.1:5173', { waitUntil: 'domcontentloaded' });
await page.evaluate(() => localStorage.setItem('useCanvasChart', '1'));
await page.reload({ waitUntil: 'domcontentloaded' });
await page.waitForTimeout(3000);

// Start (if at menu) then dismiss tutorial
await page.getByRole('button', { name: /new game/i }).first().click().catch(() => {});
await page.waitForTimeout(1500);
for (const re of [/start/i, /begin/i, /play/i, /create/i]) {
  const b = page.getByRole('button', { name: re }).first();
  if (await b.count()) { await b.click().catch(() => {}); break; }
}
await page.waitForTimeout(2500);
await page.getByRole('button', { name: /skip tutorial/i }).click().catch(() => {});
await page.waitForTimeout(800);

// Go to Market tab
await page.getByRole('button', { name: /^market$/i }).first().click().catch(() => {});
await page.waitForTimeout(1200);
await shot('a-market');

// Collect ticker-like candidates (excluding UI control words), then click until the chart canvas opens.
const CONTROLS = ['MAX','ALL','NEW','NEWS','OPEN','BUY','SELL','GTC','DAY','RSI','MACD','SMA','EMA','VWAP','SIMX','OHLC','PM','RT'];
const tickers = await page.evaluate((controls) => {
  const set = new Set();
  for (const e of document.querySelectorAll('*')) {
    if (e.children.length === 0) {
      const t = (e.textContent || '').trim();
      if (/^[A-Z]{3,5}$/.test(t) && !controls.includes(t)) set.add(t);
    }
  }
  return [...set].slice(0, 15);
}, CONTROLS);
console.log('ticker candidates:', JSON.stringify(tickers));
let opened = null;
for (const tk of tickers) {
  await page.getByText(tk, { exact: true }).first().click({ timeout: 2000 }).catch(() => {});
  await page.waitForTimeout(700);
  if ((await page.$$eval('canvas', (c) => c.length)) > 0) { opened = tk; break; }
}
console.log('detail opened via:', opened);
await page.waitForTimeout(1500);
await shot('b-selected');

// Max speed
await page.getByRole('button', { name: /^max$/i }).first().click().catch(() => {});
await page.waitForTimeout(1000);

logs.length = 0;
await page.waitForTimeout(3000);
await shot('live-1');
const cv = await page.$('canvas');
const box = cv && (await cv.boundingBox());
if (box) await page.mouse.move(box.x + box.width * 0.6, box.y + box.height * 0.4);
await page.waitForTimeout(6000);
await shot('live-2');

// --- Interaction probe: drag-pan then wheel-zoom, confirm the view responds (S3/S4 risk corner).
if (box) {
  const cx = box.x + box.width * 0.5;
  const cy = box.y + box.height * 0.35;
  await page.mouse.move(cx, cy);
  await page.mouse.down();
  await page.mouse.move(cx + 220, cy, { steps: 12 }); // drag right → scroll back in time
  await page.mouse.up();
  await page.waitForTimeout(500);
  await shot('pan');
  await page.mouse.move(cx, cy);
  await page.mouse.wheel(0, -400); // zoom in around cursor
  await page.waitForTimeout(300);
  await page.mouse.wheel(0, -400);
  await page.waitForTimeout(500);
  await shot('zoom');
}

// --- Prove token-following: switch to a colourblind theme; chart candles must recolour
//     (green/red → blue/orange) without any chart code change, driven purely by CSS tokens.
await page.evaluate(() => document.documentElement.classList.add('colorblind-deuteranopia'));
if (box) await page.mouse.move(box.x + box.width * 0.55, box.y + box.height * 0.45); // force a redraw
await page.waitForTimeout(600);
await shot('colorblind');

const canvasLogs = logs.filter((l) => l.includes('StockChartCanvas'));
console.log('=== [StockChartCanvas] first & last ===');
console.log(canvasLogs[0] || '(none)');
console.log(canvasLogs[canvasLogs.length - 1] || '(none)');
console.log('=== canvas element count ===');
console.log(await page.$$eval('canvas', (c) => c.length));
console.log('=== errors ===');
console.log(logs.filter((l) => /error|pageerror/i.test(l)).slice(-6).join('\n') || '(none)');

await browser.close();
