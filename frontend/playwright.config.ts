import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright config para tests E2E de SIG · Plataforma de Cierres.
 *
 * Requisitos previos (NO se ejecutan en el pipeline del Tester de esta fase):
 *
 *   cd frontend
 *   npm install --save-dev @playwright/test
 *   npx playwright install chromium       # ~150MB, descarga 1-3 min
 *
 * Después:
 *
 *   # En una terminal:
 *   cd backend/SIG.API
 *   ASPNETCORE_ENVIRONMENT=Development dotnet run    # http://localhost:5180
 *
 *   # En otra:
 *   cd frontend
 *   ng serve --port 4200
 *
 *   # En una tercera:
 *   cd frontend
 *   npx playwright test
 */
export default defineConfig({
  testDir: './e2e',
  timeout: 30_000,
  retries: 1,
  reporter: 'line',
  use: {
    baseURL: 'http://localhost:4200',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
