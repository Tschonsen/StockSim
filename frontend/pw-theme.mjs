// Screenshot the app under each color theme to verify the tokenized overhaul.
import { chromium } from 'playwright';

const PORT = 5176;
const THEMES = ['slate', 'amber', 'default'];
const browser = await chromium.launch();

for (const theme of THEMES) {
  const page = await browser.newPage({ viewport: { width: 1500, height: 950 } });
  const errs = [];
  page.on('pageerror', (e) => errs.push(e.message));

  await page.goto(`http://127.0.0.1:${PORT}`, { waitUntil: 'domcontentloaded' });
  await page.evaluate((t) => localStorage.setItem('stocksim-settings', JSON.stringify({ theme: t })), theme);
  await page.reload({ waitUntil: 'domcontentloaded' });
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
  await page.getByRole('button', { name: /^market$/i }).first().click().catch(() => {});
  await page.waitForTimeout(1200);

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
  let opened = null;
  for (const tk of tickers) {
    await page.getByText(tk, { exact: true }).first().click({ timeout: 2000 }).catch(() => {});
    await page.waitForTimeout(600);
    if ((await page.$$eval('canvas', (c) => c.length)) > 0) { opened = tk; break; }
  }
  await page.waitForTimeout(1500);
  await page.screenshot({ path: `D:/Dev/projects/StockSim/frontend/pw-theme-${theme}.png` });
  console.log(`theme=${theme} opened=${opened} errors=${errs.length}${errs.length ? ' :: ' + errs.slice(0,2).join(' | ') : ''}`);
  await page.close();
}
await browser.close();
