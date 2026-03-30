import { defineConfig } from '@playwright/test';

/**
 * Playwright E2E config for StockSim frontend.
 *
 * Tests run against a live Vite dev server (localhost:5173).
 * The backend must also be running for full integration tests.
 *
 * Usage:
 *   1. Start backend:  cd backend && dotnet run
 *   2. Start frontend: cd frontend && npm run dev
 *   3. Run tests:      npm run test:e2e
 */
export default defineConfig({
  testDir: './e2e',
  timeout: 60_000,
  expect: { timeout: 10_000 },
  retries: 0,
  reporter: 'list',
  use: {
    baseURL: 'http://localhost:5173',
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { browserName: 'chromium' },
    },
  ],
});
