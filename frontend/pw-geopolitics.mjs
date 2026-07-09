// Self-test: drive the game at max speed and verify geopolitical instability produces BOTH a news
// headline AND a real commodity spike (end-to-end check of the M3 Länder news slice).
import { chromium } from 'playwright';

const PORT = 5173;
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1500, height: 950 } });
const errs = [];
page.on('pageerror', (e) => errs.push(e.message));
const shot = (n) => page.screenshot({ path: `D:/Dev/projects/StockSim/frontend/pw-geo-${n}.png` });

await page.goto(`http://127.0.0.1:${PORT}`, { waitUntil: 'domcontentloaded' });
await page.waitForTimeout(2500);
await page.getByRole('button', { name: /new game/i }).first().click().catch(() => {});
await page.waitForTimeout(1500);
for (const re of [/start/i, /begin/i, /play/i, /create/i]) {
  const b = page.getByRole('button', { name: re }).first();
  if (await b.count()) { await b.click().catch(() => {}); break; }
}
await page.waitForTimeout(2500);
await page.getByRole('button', { name: /skip tutorial/i }).click().catch(() => {});
await page.waitForTimeout(800);
await page.getByRole('button', { name: /^news$/i }).first().click().catch(() => {});
await page.waitForTimeout(600);
await page.getByRole('button', { name: /^max$/i }).first().click().catch(() => {});

const body = () => page.evaluate(() => document.body.innerText);
const readNum = async (label) => {
  const m = (await body()).match(new RegExp(label + '\\s*\\$?([0-9]+(?:\\.[0-9]+)?)'));
  return m ? parseFloat(m[1]) : null;
};
const readDate = async () => {
  const m = (await body()).match(/([A-Z][a-z]{2},\s*[A-Z][a-z]{2}\s*\d{1,2}\s*\d{4})/);
  return m ? m[1].replace(/\s+/g, ' ') : null;
};

let oilMin = Infinity, oilMax = 0, goldMin = Infinity, goldMax = 0;
const geoHeadlines = [];
let firstDate = null, lastDate = null, unpaused = false;

for (let i = 0; i < 70; i++) {
  await page.waitForTimeout(1000);
  const date = await readDate();
  if (date) { if (!firstDate) firstDate = date; lastDate = date; }
  // Insurance: if the clock hasn't moved in the first few reads, it's paused → unpause once.
  if (!unpaused && i === 4 && firstDate && firstDate === lastDate) {
    await page.keyboard.press('Space').catch(() => {});
    unpaused = true;
  }
  const oil = await readNum('OIL'), gold = await readNum('GOLD');
  if (oil) { oilMin = Math.min(oilMin, oil); oilMax = Math.max(oilMax, oil); }
  if (gold) { goldMin = Math.min(goldMin, gold); goldMax = Math.max(goldMax, gold); }

  const m = (await body()).match(/(GEOPOLITICS:[^\n]{0,90}|Unrest in [A-Za-z ]+ disrupts[^\n]{0,60})/i);
  if (m) {
    const h = m[1].trim();
    if (!geoHeadlines.includes(h)) {
      geoHeadlines.push(h);
      console.log(`[t=${i}s date=${lastDate}] "${h}" (oil=${oil}, gold=${gold})`);
      if (geoHeadlines.length === 1) await shot('headline');
    }
  }
}

await shot('final');
console.log('=== RESULT ===');
console.log(`sim date: ${firstDate}  →  ${lastDate}`);
console.log(`geopolitics headlines seen: ${geoHeadlines.length}`);
geoHeadlines.slice(0, 5).forEach(h => console.log('  • ' + h));
console.log(`oil range: ${isFinite(oilMin) ? oilMin.toFixed(2) : '?'} … ${oilMax.toFixed(2)} (spread ${isFinite(oilMin) ? (oilMax - oilMin).toFixed(2) : '?'})`);
console.log(`gold range: ${isFinite(goldMin) ? goldMin.toFixed(0) : '?'} … ${goldMax.toFixed(0)}`);
console.log('page errors:', errs.length);
await browser.close();
