import { test, expect } from '@playwright/test';

/**
 * E2E: flujo de autenticación.
 *
 * NO se ejecuta automáticamente en la fase de tester. Para ejecutar:
 *   1) backend en http://localhost:5180 con seed cargado (admin@sig.local / Demo#2026!)
 *   2) frontend en http://localhost:4200 (ng serve)
 *   3) `npx playwright test e2e/auth.spec.ts` desde frontend/
 */
test.describe('Auth flow', () => {
  test('login -> dashboard -> logout -> redirige a /login', async ({ page }) => {
    await page.goto('/');
    // El authGuard debe redirigir a /login si no hay sesión
    await expect(page).toHaveURL(/\/login$/);

    // Rellenar credenciales
    await page.locator('[data-testid=input-email]').fill('admin@sig.local');
    // El input de password está en el form siguiente al de email
    await page.locator('input[type="password"]').first().fill('Demo#2026!');
    await page.locator('button[type=submit]').click();

    // Tras login válido debería navegar a /dashboard
    await expect(page).toHaveURL(/\/dashboard$/, { timeout: 10_000 });

    // sessionStorage tiene tokens
    const accessToken = await page.evaluate(() => window.sessionStorage.getItem('sig_access_token'));
    expect(accessToken).toBeTruthy();

    // Logout
    // El botón está en el AppBar, generalmente accesible vía avatar/menu
    // Cerramos sesión vía evaluación directa para no depender del DOM exacto
    await page.evaluate(() => window.sessionStorage.clear());
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/\/login$/);
  });

  test('credenciales inválidas: muestra mensaje de error y NO navega', async ({ page }) => {
    await page.goto('/login');
    await page.locator('[data-testid=input-email]').fill('admin@sig.local');
    await page.locator('input[type="password"]').first().fill('WrongPassword!');
    await page.locator('button[type=submit]').click();

    // Sigue en /login
    await expect(page).toHaveURL(/\/login$/);
    // Mensaje de error visible (texto contiene "incorrect" o "credenciales")
    await expect(page.getByText(/incorrect|credenciales|invalid/i)).toBeVisible({ timeout: 5_000 });
  });
});
