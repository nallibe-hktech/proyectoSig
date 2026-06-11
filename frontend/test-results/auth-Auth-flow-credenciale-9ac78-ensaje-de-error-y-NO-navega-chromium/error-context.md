# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: auth.spec.ts >> Auth flow >> credenciales inválidas: muestra mensaje de error y NO navega
- Location: e2e\auth.spec.ts:38:7

# Error details

```
Error: expect(locator).toBeVisible() failed

Locator: getByText(/incorrect|credenciales|invalid/i)
Expected: visible
Error: strict mode violation: getByText(/incorrect|credenciales|invalid/i) resolved to 2 elements:
    1) <p class="sig-card-sub" _ngcontent-ng-c580294552="">Introduce tus credenciales para acceder a la plat…</p> aka getByText('Introduce tus credenciales')
    2) <span class="sig-demo-title" _ngcontent-ng-c580294552="">Credenciales demo</span> aka getByText('Credenciales demo')

Call log:
  - Expect "toBeVisible" with timeout 5000ms
  - waiting for getByText(/incorrect|credenciales|invalid/i)

```

# Page snapshot

```yaml
- generic [ref=e5]:
  - generic [ref=e6]:
    - generic [ref=e7]:
      - generic [ref=e8]: service
      - generic [ref=e9]: innovation
      - generic [ref=e10]:
        - text: group
        - generic [ref=e11]: ®
    - paragraph [ref=e12]: EXCELLENCE – MADE IN EUROPE
  - generic [ref=e13]:
    - generic [ref=e14]:
      - heading "¡Bienvenido de nuevo!" [level=2] [ref=e15]
      - paragraph [ref=e16]: Introduce tus credenciales para acceder a la plataforma.
      - generic [ref=e17]:
        - generic [ref=e18]:
          - generic [ref=e19]: Correo electrónico
          - generic [ref=e20]:
            - img [ref=e21]: mail
            - textbox "nombre@sigeurope.com" [ref=e22]: admin@sig.local
        - generic [ref=e23]:
          - generic [ref=e24]: Contraseña
          - generic [ref=e25]:
            - img [ref=e26]: lock
            - textbox "••••••••" [ref=e27]: WrongPassword!
            - button "Mostrar" [ref=e28] [cursor=pointer]:
              - img [ref=e29]: visibility
        - generic [ref=e30]:
          - generic [ref=e31] [cursor=pointer]:
            - checkbox "Recordarme" [ref=e32]
            - generic [ref=e33]: Recordarme
          - link "¿Olvidaste tu contraseña?" [ref=e34] [cursor=pointer]:
            - /url: "#"
        - generic [ref=e35]:
          - img [ref=e36]: error
          - text: Credenciales inválidas
        - button "Acceder al Sistema →" [ref=e37] [cursor=pointer]
        - generic [ref=e39]: o continua con
        - button "Microsoft / Azure AD SSO" [disabled] [ref=e40] [cursor=pointer]:
          - img [ref=e41]
          - text: Microsoft / Azure AD SSO
    - generic [ref=e46]:
      - generic [ref=e47]:
        - generic [ref=e48]: Credenciales demoSolo desarrollo
        - button "✕" [ref=e49] [cursor=pointer]
      - generic [ref=e50]:
        - generic [ref=e51]:
          - generic [ref=e52]: admin@sig.local
          - generic [ref=e53]: Admin SIG · Administrator
        - button "Usar" [ref=e54] [cursor=pointer]
      - generic [ref=e55]:
        - generic [ref=e56]:
          - generic [ref=e57]: direccion@sig.local
          - generic [ref=e58]: Carmen Ruiz · Direction
        - button "Usar" [ref=e59] [cursor=pointer]
      - generic [ref=e60]:
        - generic [ref=e61]:
          - generic [ref=e62]: fico@sig.local
          - generic [ref=e63]: Javier Lopez · Fico
        - button "Usar" [ref=e64] [cursor=pointer]
      - generic [ref=e65]:
        - generic [ref=e66]:
          - generic [ref=e67]: backoffice1@sig.local
          - generic [ref=e68]: Laura Sanchez · Backoffice
        - button "Usar" [ref=e69] [cursor=pointer]
      - generic [ref=e70]:
        - generic [ref=e71]:
          - generic [ref=e72]: pm.alpha@sig.local
          - generic [ref=e73]: Maria Garcia · ProjectManager
        - button "Usar" [ref=e74] [cursor=pointer]
      - generic [ref=e75]:
        - generic [ref=e76]:
          - generic [ref=e77]: auditor@sig.local
          - generic [ref=e78]: Ines Romero · Auditor
        - button "Usar" [ref=e79] [cursor=pointer]
      - generic [ref=e80]:
        - generic [ref=e81]:
          - generic [ref=e82]: reader@sig.local
          - generic [ref=e83]: Luis Vega · Reader
        - button "Usar" [ref=e84] [cursor=pointer]
    - contentinfo [ref=e85]: SIG-ES Plataforma Integral v1.0 · © 2026 Service Innovation Group · Excellence made in Europe
```

# Test source

```ts
  1  | import { test, expect } from '@playwright/test';
  2  | 
  3  | /**
  4  |  * E2E: flujo de autenticación.
  5  |  *
  6  |  * NO se ejecuta automáticamente en la fase de tester. Para ejecutar:
  7  |  *   1) backend en http://localhost:5180 con seed cargado (admin@sig.local / Demo#2026!)
  8  |  *   2) frontend en http://localhost:4200 (ng serve)
  9  |  *   3) `npx playwright test e2e/auth.spec.ts` desde frontend/
  10 |  */
  11 | test.describe('Auth flow', () => {
  12 |   test('login -> dashboard -> logout -> redirige a /login', async ({ page }) => {
  13 |     await page.goto('/');
  14 |     // El authGuard debe redirigir a /login si no hay sesión
  15 |     await expect(page).toHaveURL(/\/login$/);
  16 | 
  17 |     // Rellenar credenciales
  18 |     await page.locator('[data-testid=input-email]').fill('admin@sig.local');
  19 |     // El input de password está en el form siguiente al de email
  20 |     await page.locator('input[type="password"]').first().fill('Demo#2026!');
  21 |     await page.locator('button[type=submit]').click();
  22 | 
  23 |     // Tras login válido debería navegar a /dashboard
  24 |     await expect(page).toHaveURL(/\/dashboard$/, { timeout: 10_000 });
  25 | 
  26 |     // sessionStorage tiene tokens
  27 |     const accessToken = await page.evaluate(() => window.sessionStorage.getItem('sig_access_token'));
  28 |     expect(accessToken).toBeTruthy();
  29 | 
  30 |     // Logout
  31 |     // El botón está en el AppBar, generalmente accesible vía avatar/menu
  32 |     // Cerramos sesión vía evaluación directa para no depender del DOM exacto
  33 |     await page.evaluate(() => window.sessionStorage.clear());
  34 |     await page.goto('/dashboard');
  35 |     await expect(page).toHaveURL(/\/login$/);
  36 |   });
  37 | 
  38 |   test('credenciales inválidas: muestra mensaje de error y NO navega', async ({ page }) => {
  39 |     await page.goto('/login');
  40 |     await page.locator('[data-testid=input-email]').fill('admin@sig.local');
  41 |     await page.locator('input[type="password"]').first().fill('WrongPassword!');
  42 |     await page.locator('button[type=submit]').click();
  43 | 
  44 |     // Sigue en /login
  45 |     await expect(page).toHaveURL(/\/login$/);
  46 |     // Mensaje de error visible (texto contiene "incorrect" o "credenciales")
> 47 |     await expect(page.getByText(/incorrect|credenciales|invalid/i)).toBeVisible({ timeout: 5_000 });
     |                                                                     ^ Error: expect(locator).toBeVisible() failed
  48 |   });
  49 | });
  50 | 
```