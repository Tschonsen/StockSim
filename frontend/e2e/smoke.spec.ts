import { test, expect } from '@playwright/test';

/**
 * Smoke tests — verify the app loads and renders core screens.
 *
 * Prerequisites:
 *   - Vite dev server running on localhost:5173 (`npm run dev`)
 *   - Backend running for full integration (optional for title screen)
 */

test.describe('Title Screen', () => {
  test('renders the STOCKSIM logo and tagline', async ({ page }) => {
    await page.goto('/');

    // The title screen should show the logo
    const logo = page.locator('h1:has-text("STOCKSIM")');
    await expect(logo).toBeVisible({ timeout: 15_000 });

    // Tagline
    const tagline = page.locator('text=Trade. Speculate. Dominate.');
    await expect(tagline).toBeVisible();
  });

  test('shows New Game button', async ({ page }) => {
    await page.goto('/');

    const newGameBtn = page.locator('button:has-text("New Game")');
    await expect(newGameBtn).toBeVisible({ timeout: 15_000 });
  });

  test('shows patch notes panel', async ({ page }) => {
    await page.goto('/');

    const patchNotes = page.locator('text=PATCH NOTES');
    await expect(patchNotes).toBeVisible({ timeout: 15_000 });
  });

  test('shows version string', async ({ page }) => {
    await page.goto('/');

    const version = page.locator('text=v0.1.0');
    await expect(version).toBeVisible({ timeout: 15_000 });
  });
});
