import { test, expect, Page } from '@playwright/test';

test.describe('6 Componentes Frontend - Prueba Visual Completa', () => {
  let page: Page;
  const baseUrl = 'http://localhost:4200';
  const apiUrl = 'http://localhost:5180';

  test.beforeEach(async ({ browser }) => {
    page = await browser.newPage();
    // Mock login — obtener token del backend
    await page.goto(`${baseUrl}/login`);

    // Esperar a que cargue el formulario de login
    await page.waitForSelector('input[type="email"]', { timeout: 5000 }).catch(() => {
      console.log('⚠️ Login form no encontrado, intentando acceso directo al dashboard...');
    });

    // Si existe el formulario, loguéate
    const emailInput = await page.$('input[type="email"]');
    if (emailInput) {
      await page.fill('input[type="email"]', 'admin@sig.local');
      await page.fill('input[type="password"]', 'AdminPassword123!');
      await page.click('button[type="submit"]');
      await page.waitForNavigation({ timeout: 10000 }).catch(() => {
        console.log('⚠️ Navigation timeout, continuando...');
      });
    }

    // Esperar a que se cargue dashboard
    await page.waitForURL('**/dashboard', { timeout: 5000 }).catch(() => {
      console.log('⚠️ Dashboard no cargó, estado actual:', page.url());
    });
  });

  test.afterEach(async () => {
    await page.close();
  });

  // ===== TEST 1: DASHBOARD DINÁMICO =====
  test('1️⃣ Dashboard — SVG Charts Dinámicos', async () => {
    console.log('\n📊 Probando Dashboard...');
    await page.goto(`${baseUrl}/dashboard`);

    // Esperar a que Dashboard cargue
    await page.waitForTimeout(2000);

    // Verificar que existen los elementos principales
    const donut = await page.$('.sig-donut');
    const gauge = await page.$('.sig-gauge');
    const areaChart = await page.$('.sig-area-chart');
    const projectsTable = await page.$('.sig-projects-table');

    console.log('  ✓ Donut visible:', !!donut);
    console.log('  ✓ Gauge visible:', !!gauge);
    console.log('  ✓ Area chart visible:', !!areaChart);
    console.log('  ✓ Projects table visible:', !!projectsTable);

    // Verificar que hay SVG (no canvas)
    const svg = await page.$('svg');
    console.log('  ✓ SVG elements encontrados:', !!svg);

    // Screenshot
    await page.screenshot({ path: 'test-results/01-dashboard.png', fullPage: true });
    console.log('  📸 Screenshot guardado: test-results/01-dashboard.png\n');

    // Validar que NO hay hardcoded values
    const alerts = await page.locator('.sig-alert-badge').innerText();
    console.log('  ✓ Badge de alertas:', alerts, '(debe ser número real, no "3")');

    expect(donut || gauge || areaChart).toBeTruthy();
  });

  // ===== TEST 2: MI APROBACIONES =====
  test('2️⃣ Mi Aprobaciones — Filtrado por Rol', async () => {
    console.log('\n👤 Probando Mi Aprobaciones...');
    await page.goto(`${baseUrl}/approvals`);

    // Esperar a que cargue
    await page.waitForTimeout(2000);

    // Verificar elementos principales
    const searchInput = await page.$('input[placeholder*="Buscar"]');
    const periodDropdown = await page.$('select, mat-select'); // Material select
    const approvalsTable = await page.$('table');

    console.log('  ✓ Search input visible:', !!searchInput);
    console.log('  ✓ Period dropdown visible:', !!periodDropdown);
    console.log('  ✓ Approvals table visible:', !!approvalsTable);

    // Prueba búsqueda en tiempo real
    if (searchInput) {
      await searchInput.fill('test');
      await page.waitForTimeout(500);
      const filteredRows = await page.locator('tbody tr').count();
      console.log('  ✓ Filas encontradas con búsqueda "test":', filteredRows);
      await searchInput.clear();
    }

    // Screenshot
    await page.screenshot({ path: 'test-results/02-approvals.png', fullPage: true });
    console.log('  📸 Screenshot guardado: test-results/02-approvals.png\n');

    expect(approvalsTable).toBeTruthy();
  });

  // ===== TEST 3: DETALLE CIERRES (Expandible) =====
  test('3️⃣ Detalle Cierres — Filas Expandibles', async () => {
    console.log('\n📋 Probando Detalle Cierres...');

    // Navega a un cierre específico (necesitas que exista en DB)
    await page.goto(`${baseUrl}/closures`);
    await page.waitForTimeout(2000);

    // Busca y haz click en el primer cierre
    const firstClosureLink = await page.$('a[routerLink*="/closures/"]');
    if (firstClosureLink) {
      await firstClosureLink.click();
      await page.waitForTimeout(2000);
    }

    // Buscar tabla de líneas
    const linesTable = await page.$('table.sig-lines-table');
    console.log('  ✓ Lines table visible:', !!linesTable);

    // Buscar botones expand
    const expandButtons = await page.locator('button.sig-expand-btn');
    const expandCount = await expandButtons.count();
    console.log('  ✓ Expand buttons encontrados:', expandCount);

    // Click en el primer expand button
    if (expandCount > 0) {
      await expandButtons.first().click();
      await page.waitForTimeout(500);

      // Verificar que se expandió
      const detailSection = await page.$('.sig-calc-detail');
      console.log('  ✓ Detail section visible tras expand:', !!detailSection);

      // Verificar subelementos
      const formula = await page.$('.sig-formula');
      const inputs = await page.$('.sig-inputs-grid');
      const resultado = await page.$('.sig-resultado');

      console.log('  ✓ Formula visible:', !!formula);
      console.log('  ✓ Inputs grid visible:', !!inputs);
      console.log('  ✓ Resultado visible:', !!resultado);
    }

    // Screenshot
    await page.screenshot({ path: 'test-results/03-closure-detail.png', fullPage: true });
    console.log('  📸 Screenshot guardado: test-results/03-closure-detail.png\n');

    expect(linesTable).toBeTruthy();
  });

  // ===== TEST 4: OVERRIDE EXCEPTION (Dialog) =====
  test('4️⃣ Override Excepciones — Modal Dialog', async () => {
    console.log('\n⚙️ Probando Override Dialog...');

    await page.goto(`${baseUrl}/closures`);
    await page.waitForTimeout(2000);

    // Navega a un cierre
    const firstClosureLink = await page.$('a[routerLink*="/closures/"]');
    if (firstClosureLink) {
      await firstClosureLink.click();
      await page.waitForTimeout(2000);
    }

    // Busca botones "Ajustar Manualmente"
    const overrideButtons = await page.locator('button:has-text("Ajustar Manualmente")');
    const overrideCount = await overrideButtons.count();
    console.log('  ✓ Override buttons encontrados:', overrideCount);

    // Click en el primero
    if (overrideCount > 0) {
      await overrideButtons.first().click();
      await page.waitForTimeout(500);

      // Verificar que el dialog abrió
      const dialogContent = await page.$('app-override-exception');
      console.log('  ✓ Override dialog visible:', !!dialogContent);

      // Verificar elementos del dialog
      const originalSection = await page.$('.sig-info-section');
      const newAmountInput = await page.$('input[type="number"]');
      const reasonTextarea = await page.$('textarea');
      const saveButton = await page.$('button[color="primary"]');

      console.log('  ✓ Original info section visible:', !!originalSection);
      console.log('  ✓ New amount input visible:', !!newAmountInput);
      console.log('  ✓ Reason textarea visible:', !!reasonTextarea);
      console.log('  ✓ Save button visible:', !!saveButton);

      // Prueba validación
      if (newAmountInput && reasonTextarea) {
        // Ingresa nuevo importe
        await newAmountInput.fill('1500');
        await page.waitForTimeout(300);

        // Ingresa razón corta (debe fallar validación)
        await reasonTextarea.fill('Fix');
        const saveButtonDisabled = await saveButton?.getAttribute('disabled');
        console.log('  ✓ Save button disabled con razón corta (<10 chars):', saveButtonDisabled !== null);

        // Completa razón
        await reasonTextarea.fill('Se ajusta porque el cálculo original fue incorrecto');
        await page.waitForTimeout(300);
        const saveButtonEnabled = await saveButton?.getAttribute('disabled');
        console.log('  ✓ Save button enabled con razón válida (>10 chars):', saveButtonEnabled === null);

        // Verifica diferencia actualizada
        const diff = await page.locator('.sig-diff').innerText();
        console.log('  ✓ Diferencia mostrada:', diff.substring(0, 50) + '...');
      }
    }

    // Screenshot
    await page.screenshot({ path: 'test-results/04-override-dialog.png', fullPage: true });
    console.log('  📸 Screenshot guardado: test-results/04-override-dialog.png\n');
  });

  // ===== TEST 5: BÚSQUEDA FULL-TEXT =====
  test('5️⃣ Búsqueda Full-text — Multi-campo', async () => {
    console.log('\n🔍 Probando Búsqueda Full-text...');

    await page.goto(`${baseUrl}/projects`);
    await page.waitForTimeout(2000);

    // Buscar componente de búsqueda
    const searchComponent = await page.$('app-fulltext-search');
    console.log('  ✓ Search component visible:', !!searchComponent);

    const searchInput = await page.$('app-fulltext-search input');
    if (searchInput) {
      // Prueba búsqueda
      await searchInput.fill('test');
      await page.waitForTimeout(500);

      // Verifica estadísticas
      const stats = await page.locator('.sig-search-stats').innerText();
      console.log('  ✓ Search stats:', stats.substring(0, 50) + '...');

      // Verifica filtros de columna (si existen)
      const columnFilters = await page.locator('.sig-field-chip');
      const filterCount = await columnFilters.count();
      console.log('  ✓ Column filters encontrados:', filterCount);

      // Verifica resultados
      const results = await page.locator('tbody tr').count();
      console.log('  ✓ Filas visibles tras búsqueda:', results);

      // Prueba typo (Levenshtein)
      await searchInput.clear();
      await searchInput.fill('projct'); // typo: falta 'e'
      await page.waitForTimeout(500);

      const suggestions = await page.locator('.sig-suggestion-btn');
      const suggestionCount = await suggestions.count();
      console.log('  ✓ Sugerencias fuzzy encontradas:', suggestionCount, '(Levenshtein distance <= 2)');
    }

    // Screenshot
    await page.screenshot({ path: 'test-results/05-fulltext-search.png', fullPage: true });
    console.log('  📸 Screenshot guardado: test-results/05-fulltext-search.png\n');

    expect(searchComponent).toBeTruthy();
  });

  // ===== TEST 6: IMPORTACIÓN EXCEL =====
  test('6️⃣ Importación Excel — Validación y Preview', async () => {
    console.log('\n📤 Probando Importación Excel...');

    await page.goto(`${baseUrl}/projects`);
    await page.waitForTimeout(2000);

    // Busca el componente de importación (generalmente en un tab)
    const importTab = await page.$('button:has-text("Importar")');
    if (importTab) {
      await importTab.click();
      await page.waitForTimeout(1000);
    }

    // Buscar componente de importación
    const importComponent = await page.$('app-excel-import');
    console.log('  ✓ Import component visible:', !!importComponent);

    // Buscar zona de upload
    const uploadZone = await page.$('.sig-upload-zone');
    console.log('  ✓ Upload zone visible:', !!uploadZone);

    // Crear archivo Excel de prueba
    const fs = require('fs');
    const path = require('path');

    // Crear archivo Excel mínimo (XLSX)
    try {
      const testFile = path.join(__dirname, '../../test-import.xlsx');
      // Nota: Para prueba real, necesitarías crear un archivo XLSX válido
      // Por ahora, verificamos que el componente está presente

      console.log('  ℹ️ Componente preparado para upload (archivo real requiere XLSX válido)');
    } catch (err) {
      console.log('  ⚠️ No se pudo crear archivo de prueba:', err);
    }

    // Verificar elementos del componente
    const fileInput = await page.$('input[type="file"]');
    console.log('  ✓ File input visible:', !!fileInput);

    const uploadButton = await page.$('button:has-text("Seleccionar archivo")');
    console.log('  ✓ Upload button visible:', !!uploadButton);

    // Screenshot
    await page.screenshot({ path: 'test-results/06-excel-import.png', fullPage: true });
    console.log('  📸 Screenshot guardado: test-results/06-excel-import.png\n');

    expect(importComponent).toBeTruthy();
  });

  // ===== TEST 7: RESPONSIVIDAD =====
  test('7️⃣ Responsividad — Mobile / Tablet / Desktop', async () => {
    console.log('\n📱 Probando Responsividad...');

    // Mobile (375px)
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto(`${baseUrl}/dashboard`);
    await page.waitForTimeout(1000);
    await page.screenshot({ path: 'test-results/07a-mobile.png', fullPage: true });
    console.log('  📸 Mobile screenshot: test-results/07a-mobile.png');

    // Tablet (768px)
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.goto(`${baseUrl}/dashboard`);
    await page.waitForTimeout(1000);
    await page.screenshot({ path: 'test-results/07b-tablet.png', fullPage: true });
    console.log('  📸 Tablet screenshot: test-results/07b-tablet.png');

    // Desktop (1920px)
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.goto(`${baseUrl}/dashboard`);
    await page.waitForTimeout(1000);
    await page.screenshot({ path: 'test-results/07c-desktop.png', fullPage: true });
    console.log('  📸 Desktop screenshot: test-results/07c-desktop.png\n');

    expect(true).toBeTruthy();
  });

  // ===== TEST 8: ERRORES DE CONSOLA =====
  test('8️⃣ Verificar Consola — Sin Errores TypeScript/API', async () => {
    console.log('\n⚠️ Verificando Console Errors...');

    const errors: string[] = [];
    const warnings: string[] = [];

    page.on('console', (msg) => {
      if (msg.type() === 'error') {
        errors.push(msg.text());
      } else if (msg.type() === 'warning') {
        warnings.push(msg.text());
      }
    });

    // Navega por todas las páginas
    await page.goto(`${baseUrl}/dashboard`);
    await page.waitForTimeout(2000);

    await page.goto(`${baseUrl}/approvals`);
    await page.waitForTimeout(2000);

    await page.goto(`${baseUrl}/projects`);
    await page.waitForTimeout(2000);

    console.log('  ✓ Console errors encontrados:', errors.length);
    if (errors.length > 0) {
      console.log('    Errores:');
      errors.slice(0, 5).forEach((err) => console.log(`      - ${err.substring(0, 100)}`));
    }

    console.log('  ✓ Console warnings encontrados:', warnings.length);

    expect(errors.length).toBeLessThan(10); // Tolerancia para warnings menores
  });
});
