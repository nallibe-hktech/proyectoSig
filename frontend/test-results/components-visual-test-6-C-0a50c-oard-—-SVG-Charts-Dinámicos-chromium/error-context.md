# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: components-visual-test.spec.ts >> 6 Componentes Frontend - Prueba Visual Completa >> 1️⃣ Dashboard — SVG Charts Dinámicos
- Location: e2e\components-visual-test.spec.ts:40:7

# Error details

```
Test timeout of 30000ms exceeded while running "beforeEach" hook.
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
            - textbox "••••••••" [ref=e27]: AdminPassword123!
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
  1   | import { test, expect, Page } from '@playwright/test';
  2   | 
  3   | test.describe('6 Componentes Frontend - Prueba Visual Completa', () => {
  4   |   let page: Page;
  5   |   const baseUrl = 'http://localhost:4200';
  6   |   const apiUrl = 'http://localhost:5180';
  7   | 
> 8   |   test.beforeEach(async ({ browser }) => {
      |        ^ Test timeout of 30000ms exceeded while running "beforeEach" hook.
  9   |     page = await browser.newPage();
  10  |     // Mock login — obtener token del backend
  11  |     await page.goto(`${baseUrl}/login`);
  12  | 
  13  |     // Esperar a que cargue el formulario de login
  14  |     await page.waitForSelector('input[type="email"]', { timeout: 5000 }).catch(() => {
  15  |       console.log('⚠️ Login form no encontrado, intentando acceso directo al dashboard...');
  16  |     });
  17  | 
  18  |     // Si existe el formulario, loguéate
  19  |     const emailInput = await page.$('input[type="email"]');
  20  |     if (emailInput) {
  21  |       await page.fill('input[type="email"]', 'admin@sig.local');
  22  |       await page.fill('input[type="password"]', 'AdminPassword123!');
  23  |       await page.click('button[type="submit"]');
  24  |       await page.waitForNavigation({ timeout: 10000 }).catch(() => {
  25  |         console.log('⚠️ Navigation timeout, continuando...');
  26  |       });
  27  |     }
  28  | 
  29  |     // Esperar a que se cargue dashboard
  30  |     await page.waitForURL('**/dashboard', { timeout: 5000 }).catch(() => {
  31  |       console.log('⚠️ Dashboard no cargó, estado actual:', page.url());
  32  |     });
  33  |   });
  34  | 
  35  |   test.afterEach(async () => {
  36  |     await page.close();
  37  |   });
  38  | 
  39  |   // ===== TEST 1: DASHBOARD DINÁMICO =====
  40  |   test('1️⃣ Dashboard — SVG Charts Dinámicos', async () => {
  41  |     console.log('\n📊 Probando Dashboard...');
  42  |     await page.goto(`${baseUrl}/dashboard`);
  43  | 
  44  |     // Esperar a que Dashboard cargue
  45  |     await page.waitForTimeout(2000);
  46  | 
  47  |     // Verificar que existen los elementos principales
  48  |     const donut = await page.$('.sig-donut');
  49  |     const gauge = await page.$('.sig-gauge');
  50  |     const areaChart = await page.$('.sig-area-chart');
  51  |     const projectsTable = await page.$('.sig-projects-table');
  52  | 
  53  |     console.log('  ✓ Donut visible:', !!donut);
  54  |     console.log('  ✓ Gauge visible:', !!gauge);
  55  |     console.log('  ✓ Area chart visible:', !!areaChart);
  56  |     console.log('  ✓ Projects table visible:', !!projectsTable);
  57  | 
  58  |     // Verificar que hay SVG (no canvas)
  59  |     const svg = await page.$('svg');
  60  |     console.log('  ✓ SVG elements encontrados:', !!svg);
  61  | 
  62  |     // Screenshot
  63  |     await page.screenshot({ path: 'test-results/01-dashboard.png', fullPage: true });
  64  |     console.log('  📸 Screenshot guardado: test-results/01-dashboard.png\n');
  65  | 
  66  |     // Validar que NO hay hardcoded values
  67  |     const alerts = await page.locator('.sig-alert-badge').innerText();
  68  |     console.log('  ✓ Badge de alertas:', alerts, '(debe ser número real, no "3")');
  69  | 
  70  |     expect(donut || gauge || areaChart).toBeTruthy();
  71  |   });
  72  | 
  73  |   // ===== TEST 2: MI APROBACIONES =====
  74  |   test('2️⃣ Mi Aprobaciones — Filtrado por Rol', async () => {
  75  |     console.log('\n👤 Probando Mi Aprobaciones...');
  76  |     await page.goto(`${baseUrl}/approvals`);
  77  | 
  78  |     // Esperar a que cargue
  79  |     await page.waitForTimeout(2000);
  80  | 
  81  |     // Verificar elementos principales
  82  |     const searchInput = await page.$('input[placeholder*="Buscar"]');
  83  |     const periodDropdown = await page.$('select, mat-select'); // Material select
  84  |     const approvalsTable = await page.$('table');
  85  | 
  86  |     console.log('  ✓ Search input visible:', !!searchInput);
  87  |     console.log('  ✓ Period dropdown visible:', !!periodDropdown);
  88  |     console.log('  ✓ Approvals table visible:', !!approvalsTable);
  89  | 
  90  |     // Prueba búsqueda en tiempo real
  91  |     if (searchInput) {
  92  |       await searchInput.fill('test');
  93  |       await page.waitForTimeout(500);
  94  |       const filteredRows = await page.locator('tbody tr').count();
  95  |       console.log('  ✓ Filas encontradas con búsqueda "test":', filteredRows);
  96  |       await searchInput.clear();
  97  |     }
  98  | 
  99  |     // Screenshot
  100 |     await page.screenshot({ path: 'test-results/02-approvals.png', fullPage: true });
  101 |     console.log('  📸 Screenshot guardado: test-results/02-approvals.png\n');
  102 | 
  103 |     expect(approvalsTable).toBeTruthy();
  104 |   });
  105 | 
  106 |   // ===== TEST 3: DETALLE CIERRES (Expandible) =====
  107 |   test('3️⃣ Detalle Cierres — Filas Expandibles', async () => {
  108 |     console.log('\n📋 Probando Detalle Cierres...');
```