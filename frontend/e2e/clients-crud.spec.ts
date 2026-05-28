import { test, expect } from '@playwright/test';

/**
 * E2E: CRUD básico sobre Clients post-login.
 * Requiere backend + frontend corriendo y seed cargado.
 */
test.describe('Clients CRUD', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.locator('[data-testid=input-email]').fill('admin@sig.local');
    await page.locator('input[type="password"]').first().fill('Demo#2026!');
    await page.locator('button[type=submit]').click();
    await expect(page).toHaveURL(/\/dashboard$/, { timeout: 10_000 });
  });

  test('listar clientes seed: Alpha, Beta, Gamma', async ({ page }) => {
    await page.goto('/clients');
    // Espera tabla renderizada
    await expect(page.locator('table')).toBeVisible({ timeout: 10_000 });
    await expect(page.getByText('Alpha Foods')).toBeVisible();
    await expect(page.getByText('Beta Cosmetics')).toBeVisible();
    await expect(page.getByText('Gamma Retail')).toBeVisible();
  });
});
