// Screenshot converted tabs under the Slate theme to verify they follow the tokens.
import { chromium } from 'playwright';
const PORT = 5176;
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1500, height: 950 } });
const errs = []; page.on('pageerror', (e) => errs.push(e.message));

await page.goto(`http://127.0.0.1:${PORT}`, { waitUntil: 'domcontentloaded' });
await page.evaluate(() => localStorage.setItem('stocksim-settings', JSON.stringify({ theme: 'slate' })));
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

for (const tab of ['Dashboard', 'News', 'Portfolio']) {
  await page.getByRole('button', { name: new RegExp(`^${tab}$`, 'i') }).first().click().catch(() => {});
  await page.waitForTimeout(1400);
  await page.screenshot({ path: `D:/Dev/projects/StockSim/frontend/pw-tab-${tab.toLowerCase()}.png` });
  console.log(`tab=${tab} shot; errors=${errs.length}`);
}
await browser.close();
