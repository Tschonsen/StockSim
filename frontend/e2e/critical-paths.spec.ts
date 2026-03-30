import { test, expect } from '@playwright/test';

/**
 * Critical path tests — verify navigation through the main game flow.
 *
 * Prerequisites:
 *   - Vite dev server running on localhost:5173 (`npm run dev`)
 *   - Backend running on port 8765 (`cd backend && dotnet run`)
 *
 * These tests walk through: Title -> New Game -> Start Game -> In-Game UI.
 * If the backend is not running, the app may stay on "connecting" screen.
 */

test.describe('New Game Flow', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate with explicit backend port so WebSocket connects
    await page.goto('/?backendPort=8765');

    // Wait for title screen (requires backend connection)
    const logo = page.locator('h1:has-text("STOCKSIM")');
    await expect(logo).toBeVisible({ timeout: 30_000 });
  });

  test('clicking New Game navigates to game setup', async ({ page }) => {
    const newGameBtn = page.locator('button:has-text("New Game")');
    await newGameBtn.click();

    // NewGameScreen should show difficulty presets
    const sandboxTab = page.locator('text=Sandbox');
    await expect(sandboxTab).toBeVisible({ timeout: 10_000 });
  });

  test('can go back from New Game to Title', async ({ page }) => {
    // Go to New Game
    await page.locator('button:has-text("New Game")').click();
    await expect(page.locator('text=Sandbox')).toBeVisible({ timeout: 10_000 });

    // Click Back
    const backBtn = page.locator('button:has-text("Back")');
    await backBtn.click();

    // Should be back at title
    await expect(page.locator('h1:has-text("STOCKSIM")')).toBeVisible();
  });

  test('can start a new game and see in-game UI', async ({ page }) => {
    // Navigate to New Game
    await page.locator('button:has-text("New Game")').click();
    await expect(page.locator('text=Sandbox')).toBeVisible({ timeout: 10_000 });

    // Click Start Game (default settings)
    const startBtn = page.locator('button:has-text("Start")');
    await startBtn.click();

    // In-game UI should appear — look for TopBar or LeftSidebar elements
    // The watchlist or portfolio panel should be visible
    const gameUI = page.locator('text=Portfolio').first();
    await expect(gameUI).toBeVisible({ timeout: 30_000 });
  });
});

test.describe('In-Game UI', () => {
  test.beforeEach(async ({ page }) => {
    // Quick start: title -> new game -> start
    await page.goto('/?backendPort=8765');
    await expect(page.locator('h1:has-text("STOCKSIM")')).toBeVisible({ timeout: 30_000 });

    await page.locator('button:has-text("New Game")').click();
    await expect(page.locator('text=Sandbox')).toBeVisible({ timeout: 10_000 });

    await page.locator('button:has-text("Start")').click();
    // Wait for game to load
    await expect(page.locator('text=Portfolio').first()).toBeVisible({ timeout: 30_000 });
  });

  test('watchlist shows stock entries', async ({ page }) => {
    // Watchlist should have stock symbols after data loads
    // Look for typical stock list items (ticker symbols are uppercase 2-5 char strings)
    const stockEntries = page.locator('[class*="watchlist"] >> text=/^[A-Z]{2,5}$/').first();
    await expect(stockEntries).toBeVisible({ timeout: 15_000 });
  });

  test('top bar shows game date and cash', async ({ page }) => {
    // TopBar should show the starting cash amount (formatted with $ sign)
    const cashDisplay = page.locator('text=/\\$[\\d,]+/').first();
    await expect(cashDisplay).toBeVisible({ timeout: 10_000 });
  });
});
