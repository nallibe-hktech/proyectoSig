# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: components-visual-test.spec.ts >> 6 Componentes Frontend - Prueba Visual Completa >> 2️⃣ Mi Aprobaciones — Filtrado por Rol
- Location: e2e\components-visual-test.spec.ts:74:7

# Error details

```
Error: expect(received).toBeTruthy()

Received: null
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
            - textbox "nombre@sigeurope.com" [ref=e22]
        - generic [ref=e23]:
          - generic [ref=e24]: Contraseña
          - generic [ref=e25]:
            - img [ref=e26]: lock
            - textbox "••••••••" [ref=e27]
            - button "Mostrar" [ref=e28] [cursor=pointer]:
              - img [ref=e29]: visibility
        - generic [ref=e30]:
          - generic [ref=e31] [cursor=pointer]:
            - checkbox "Recordarme" [ref=e32]
            - generic [ref=e33]: Recordarme
          - link "¿Olvidaste tu contraseña?" [ref=e34] [cursor=pointer]:
            - /url: "#"
        - button "Acceder al Sistema →" [disabled] [ref=e35]
        - generic [ref=e37]: o continua con
        - button "Microsoft / Azure AD SSO" [disabled] [ref=e38] [cursor=pointer]:
          - img [ref=e39]
          - text: Microsoft / Azure AD SSO
    - generic [ref=e44]:
      - generic [ref=e45]:
        - generic [ref=e46]: Credenciales demoSolo desarrollo
        - button "✕" [ref=e47] [cursor=pointer]
      - generic [ref=e48]:
        - generic [ref=e49]:
          - generic [ref=e50]: admin@sig.local
          - generic [ref=e51]: Admin SIG · Administrator
        - button "Usar" [ref=e52] [cursor=pointer]
      - generic [ref=e53]:
        - generic [ref=e54]:
          - generic [ref=e55]: direccion@sig.local
          - generic [ref=e56]: Carmen Ruiz · Direction
        - button "Usar" [ref=e57] [cursor=pointer]
      - generic [ref=e58]:
        - generic [ref=e59]:
          - generic [ref=e60]: fico@sig.local
          - generic [ref=e61]: Javier Lopez · Fico
        - button "Usar" [ref=e62] [cursor=pointer]
      - generic [ref=e63]:
        - generic [ref=e64]:
          - generic [ref=e65]: backoffice1@sig.local
          - generic [ref=e66]: Laura Sanchez · Backoffice
        - button "Usar" [ref=e67] [cursor=pointer]
      - generic [ref=e68]:
        - generic [ref=e69]:
          - generic [ref=e70]: pm.alpha@sig.local
          - generic [ref=e71]: Maria Garcia · ProjectManager
        - button "Usar" [ref=e72] [cursor=pointer]
      - generic [ref=e73]:
        - generic [ref=e74]:
          - generic [ref=e75]: auditor@sig.local
          - generic [ref=e76]: Ines Romero · Auditor
        - button "Usar" [ref=e77] [cursor=pointer]
      - generic [ref=e78]:
        - generic [ref=e79]:
          - generic [ref=e80]: reader@sig.local
          - generic [ref=e81]: Luis Vega · Reader
        - button "Usar" [ref=e82] [cursor=pointer]
    - contentinfo [ref=e83]: SIG-ES Plataforma Integral v1.0 · © 2026 Service Innovation Group · Excellence made in Europe
```

# Test source

```ts
  3   | test.describe('6 Componentes Frontend - Prueba Visual Completa', () => {
  4   |   let page: Page;
  5   |   const baseUrl = 'http://localhost:4200';
  6   |   const apiUrl = 'http://localhost:5180';
  7   | 
  8   |   test.beforeEach(async ({ browser }) => {
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
> 103 |     expect(approvalsTable).toBeTruthy();
      |                            ^ Error: expect(received).toBeTruthy()
  104 |   });
  105 | 
  106 |   // ===== TEST 3: DETALLE CIERRES (Expandible) =====
  107 |   test('3️⃣ Detalle Cierres — Filas Expandibles', async () => {
  108 |     console.log('\n📋 Probando Detalle Cierres...');
  109 | 
  110 |     // Navega a un cierre específico (necesitas que exista en DB)
  111 |     await page.goto(`${baseUrl}/closures`);
  112 |     await page.waitForTimeout(2000);
  113 | 
  114 |     // Busca y haz click en el primer cierre
  115 |     const firstClosureLink = await page.$('a[routerLink*="/closures/"]');
  116 |     if (firstClosureLink) {
  117 |       await firstClosureLink.click();
  118 |       await page.waitForTimeout(2000);
  119 |     }
  120 | 
  121 |     // Buscar tabla de líneas
  122 |     const linesTable = await page.$('table.sig-lines-table');
  123 |     console.log('  ✓ Lines table visible:', !!linesTable);
  124 | 
  125 |     // Buscar botones expand
  126 |     const expandButtons = await page.locator('button.sig-expand-btn');
  127 |     const expandCount = await expandButtons.count();
  128 |     console.log('  ✓ Expand buttons encontrados:', expandCount);
  129 | 
  130 |     // Click en el primer expand button
  131 |     if (expandCount > 0) {
  132 |       await expandButtons.first().click();
  133 |       await page.waitForTimeout(500);
  134 | 
  135 |       // Verificar que se expandió
  136 |       const detailSection = await page.$('.sig-calc-detail');
  137 |       console.log('  ✓ Detail section visible tras expand:', !!detailSection);
  138 | 
  139 |       // Verificar subelementos
  140 |       const formula = await page.$('.sig-formula');
  141 |       const inputs = await page.$('.sig-inputs-grid');
  142 |       const resultado = await page.$('.sig-resultado');
  143 | 
  144 |       console.log('  ✓ Formula visible:', !!formula);
  145 |       console.log('  ✓ Inputs grid visible:', !!inputs);
  146 |       console.log('  ✓ Resultado visible:', !!resultado);
  147 |     }
  148 | 
  149 |     // Screenshot
  150 |     await page.screenshot({ path: 'test-results/03-closure-detail.png', fullPage: true });
  151 |     console.log('  📸 Screenshot guardado: test-results/03-closure-detail.png\n');
  152 | 
  153 |     expect(linesTable).toBeTruthy();
  154 |   });
  155 | 
  156 |   // ===== TEST 4: OVERRIDE EXCEPTION (Dialog) =====
  157 |   test('4️⃣ Override Excepciones — Modal Dialog', async () => {
  158 |     console.log('\n⚙️ Probando Override Dialog...');
  159 | 
  160 |     await page.goto(`${baseUrl}/closures`);
  161 |     await page.waitForTimeout(2000);
  162 | 
  163 |     // Navega a un cierre
  164 |     const firstClosureLink = await page.$('a[routerLink*="/closures/"]');
  165 |     if (firstClosureLink) {
  166 |       await firstClosureLink.click();
  167 |       await page.waitForTimeout(2000);
  168 |     }
  169 | 
  170 |     // Busca botones "Ajustar Manualmente"
  171 |     const overrideButtons = await page.locator('button:has-text("Ajustar Manualmente")');
  172 |     const overrideCount = await overrideButtons.count();
  173 |     console.log('  ✓ Override buttons encontrados:', overrideCount);
  174 | 
  175 |     // Click en el primero
  176 |     if (overrideCount > 0) {
  177 |       await overrideButtons.first().click();
  178 |       await page.waitForTimeout(500);
  179 | 
  180 |       // Verificar que el dialog abrió
  181 |       const dialogContent = await page.$('app-override-exception');
  182 |       console.log('  ✓ Override dialog visible:', !!dialogContent);
  183 | 
  184 |       // Verificar elementos del dialog
  185 |       const originalSection = await page.$('.sig-info-section');
  186 |       const newAmountInput = await page.$('input[type="number"]');
  187 |       const reasonTextarea = await page.$('textarea');
  188 |       const saveButton = await page.$('button[color="primary"]');
  189 | 
  190 |       console.log('  ✓ Original info section visible:', !!originalSection);
  191 |       console.log('  ✓ New amount input visible:', !!newAmountInput);
  192 |       console.log('  ✓ Reason textarea visible:', !!reasonTextarea);
  193 |       console.log('  ✓ Save button visible:', !!saveButton);
  194 | 
  195 |       // Prueba validación
  196 |       if (newAmountInput && reasonTextarea) {
  197 |         // Ingresa nuevo importe
  198 |         await newAmountInput.fill('1500');
  199 |         await page.waitForTimeout(300);
  200 | 
  201 |         // Ingresa razón corta (debe fallar validación)
  202 |         await reasonTextarea.fill('Fix');
  203 |         const saveButtonDisabled = await saveButton?.getAttribute('disabled');
```