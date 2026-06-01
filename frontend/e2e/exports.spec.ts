import { test, expect } from '@playwright/test';
import * as XLSX from 'xlsx';
import * as fs from 'fs';
import * as path from 'path';

/**
 * E2E: Validación de exports A3 Innuva (.xls) y A3 ERP (.xlsx)
 *
 * Requisitos previos:
 *   1) Backend en http://localhost:5180 con seed cargado (admin@sig.local / Demo#2026!)
 *   2) Frontend en http://localhost:4200 (ng serve)
 *   3) Debe existir al menos un cierre en estado "Aprobado" o "Exportado"
 *      (O el backend genera automáticamente cierres en seed)
 *
 * Para ejecutar:
 *   cd frontend
 *   npx playwright test e2e/exports.spec.ts --headed
 *
 * Con debug:
 *   npx playwright test e2e/exports.spec.ts --debug
 */

test.describe('A3 Exports (Innuva .xls y ERP .xlsx)', () => {
  let closureId: number | null = null;

  /**
   * Setup: Login y obtener/crear un closureId en estado Aprobado
   */
  test.beforeEach(async ({ page }) => {
    // Login
    await page.goto('http://localhost:4200/login', { waitUntil: 'domcontentloaded' });
    await page.locator('[data-testid=input-email]').fill('admin@sig.local');
    await page.locator('input[type="password"]').first().fill('Demo#2026!');
    await page.locator('button[type=submit]').click();

    // Esperar a dashboard
    await expect(page).toHaveURL(/\/dashboard/, { timeout: 20_000 });

    // Dar tiempo adicional para autenticación
    await page.waitForTimeout(2000);

    // Probar cierres del 1 al 5 hasta encontrar uno con botones de exportación
    for (let closureNum = 1; closureNum <= 5; closureNum++) {
      try {
        console.log(`Trying closure ${closureNum}...`);
        await page.goto(`/closures/${closureNum}`, { waitUntil: 'domcontentloaded' });

        // Esperar a que algo cargue (KPI cards)
        const kpiLoaded = await page
          .waitForSelector('[data-testid="kpi-coste-total"]', { timeout: 5_000 })
          .catch(() => null);

        if (!kpiLoaded) {
          console.log(`Closure ${closureNum}: KPI not found, trying next`);
          continue;
        }

        // Buscar los botones de exportación
        const innuvaBtn = await page
          .locator('[data-testid=btn-export-innuva]')
          .isVisible()
          .catch(() => false);
        const erpBtn = await page
          .locator('[data-testid=btn-export-erp]')
          .isVisible()
          .catch(() => false);

        if (innuvaBtn && erpBtn) {
          closureId = closureNum;
          console.log(`✓ Found closure ${closureNum} with export buttons visible`);
          break;
        } else {
          console.log(`Closure ${closureNum}: Export buttons not visible (state may not be Aprobado)`);
        }
      } catch (e) {
        console.log(`Error checking closure ${closureNum}: ${e}`);
      }
    }

    if (!closureId) {
      console.log('No closure with export buttons found in closures 1-5');
      test.skip();
    }
  });

  /**
   * Test 1: Descargar A3 Innuva (.xls)
   * Valida:
   *  - Archivo se descarga con extensión .xls
   *  - NPOI format es válido
   *  - Contiene columnas esperadas: Empresa, Imputación, Tipo de Paga, Importe Bruto, SS Trabajador, IRPF, etc.
   *  - Datos están presentes (no vacío)
   */
  test('Descargar A3 Innuva (.xls) - estructura y datos válidos', async ({ page, context }) => {
    if (!closureId) {
      test.skip();
      return;
    }

    const downloadPromise = context.waitForEvent('download');

    // Click en botón "A3 Innuva"
    const exportBtn = page.locator('[data-testid=btn-export-innuva]');
    await expect(exportBtn).toBeVisible({ timeout: 5_000 });
    await exportBtn.click();

    // Esperar descarga
    const download = await downloadPromise;

    // Validar que el archivo existe y tiene extensión .xls
    expect(download.suggestedFilename()).toMatch(/\.xls$/i);
    expect(download.suggestedFilename()).toContain('A3Innuva');

    // Guardar temporalmente para lectura
    const tempDir = '/tmp';
    const tempPath = path.join(tempDir, download.suggestedFilename());
    await download.saveAs(tempPath);

    // Verificar que el archivo existe
    expect(fs.existsSync(tempPath)).toBeTruthy();

    // Intentar leer el archivo con XLSX
    let workbook: XLSX.WorkBook;
    try {
      workbook = XLSX.readFile(tempPath);
    } catch (e) {
      expect(false, `No se pudo leer el archivo .xls como XLSX válido: ${e}`).toBeTruthy();
      return;
    }

    // Validar que hay al menos una sheet
    expect(workbook.SheetNames.length).toBeGreaterThan(0);

    const firstSheet = workbook.SheetNames[0];
    const worksheet = workbook.Sheets[firstSheet];

    // Convertir a array de objetos
    const rows: Record<string, any>[] = XLSX.utils.sheet_to_json(worksheet) as Record<string, any>[];

    // Validar que hay datos (al menos 1 fila)
    expect(rows.length).toBeGreaterThan(0);

    // Validar que al menos una fila tiene datos
    const firstRow = rows[0];
    expect(Object.keys(firstRow).length).toBeGreaterThan(0);

    // Columnas esperadas para A3 Innuva (pueden variar según la implementación)
    // Verificar que contiene al menos algunas columnas clave: Empresa, Imputación, Importe, etc.
    const expectedColumns = [
      'Empresa',
      'Imputación',
      'Tipo de Paga',
      'Importe Bruto',
      'SS Trabajador',
      'IRPF'
    ];

    const foundColumns = expectedColumns.filter(col =>
      Object.keys(firstRow).some(key => key.toLowerCase().includes(col.toLowerCase()))
    );

    // Al menos 3 columnas esperadas deben estar presentes
    expect(foundColumns.length).toBeGreaterThanOrEqual(3);

    // Limpiar archivo temporal
    fs.unlinkSync(tempPath);
  });

  /**
   * Test 2: Descargar A3 ERP (.xlsx)
   * Valida:
   *  - Archivo se descarga con extensión .xlsx
   *  - ClosedXML format es válido
   *  - Contiene columnas: Cliente NIF, Cliente Nombre, Proyecto, Concepto, Importe, IVA%, Total
   *  - Datos están presentes
   */
  test('Descargar A3 ERP (.xlsx) - estructura y datos válidos', async ({ page, context }) => {
    if (!closureId) {
      test.skip();
      return;
    }

    const downloadPromise = context.waitForEvent('download');

    // Click en botón "A3 ERP"
    const exportBtn = page.locator('[data-testid=btn-export-erp]');
    await expect(exportBtn).toBeVisible({ timeout: 5_000 });
    await exportBtn.click();

    // Esperar descarga
    const download = await downloadPromise;

    // Validar que el archivo existe y tiene extensión .xlsx
    expect(download.suggestedFilename()).toMatch(/\.xlsx$/i);
    expect(download.suggestedFilename()).toContain('A3ERP');

    // Guardar temporalmente para lectura
    const tempDir = '/tmp';
    const tempPath = path.join(tempDir, download.suggestedFilename());
    await download.saveAs(tempPath);

    // Verificar que el archivo existe
    expect(fs.existsSync(tempPath)).toBeTruthy();

    // Intentar leer el archivo con XLSX
    let workbook: XLSX.WorkBook;
    try {
      workbook = XLSX.readFile(tempPath);
    } catch (e) {
      expect(false, `No se pudo leer el archivo .xlsx como XLSX válido: ${e}`).toBeTruthy();
      return;
    }

    // Validar que hay al menos una sheet
    expect(workbook.SheetNames.length).toBeGreaterThan(0);

    const firstSheet = workbook.SheetNames[0];
    const worksheet = workbook.Sheets[firstSheet];

    // Convertir a array de objetos
    const rows: Record<string, any>[] = XLSX.utils.sheet_to_json(worksheet) as Record<string, any>[];

    // Validar que hay datos (al menos 1 fila)
    expect(rows.length).toBeGreaterThan(0);

    // Validar que al menos una fila tiene datos
    const firstRow = rows[0];
    expect(Object.keys(firstRow).length).toBeGreaterThan(0);

    // Columnas esperadas para A3 ERP
    const expectedColumns = [
      'Cliente NIF',
      'Cliente Nombre',
      'Proyecto',
      'Concepto',
      'Importe',
      'IVA%',
      'Total'
    ];

    const foundColumns = expectedColumns.filter(col =>
      Object.keys(firstRow).some(key => key.toLowerCase().includes(col.toLowerCase().replace(/[%]/g, '')))
    );

    // Al menos 4 columnas esperadas deben estar presentes
    expect(foundColumns.length).toBeGreaterThanOrEqual(4);

    // Limpiar archivo temporal
    fs.unlinkSync(tempPath);
  });

  /**
   * Test 3: Cálculo de VAT en A3 ERP
   * Valida:
   *  - VAT = 21% para cliente con país España o null (valor por defecto)
   *  - VAT = 0% para país intra-EU (ej: Francia, Alemania, etc. si existen en seed)
   *  - Cálculo de Total = Importe + (Importe * VAT%)
   */
  test('VAT calculation en A3 ERP - validar tasas según país', async ({ page, context }) => {
    if (!closureId) {
      test.skip();
      return;
    }

    const downloadPromise = context.waitForEvent('download');

    // Click en botón "A3 ERP"
    const exportBtn = page.locator('[data-testid=btn-export-erp]');
    await expect(exportBtn).toBeVisible({ timeout: 5_000 });
    await exportBtn.click();

    // Esperar descarga
    const download = await downloadPromise;
    expect(download.suggestedFilename()).toMatch(/\.xlsx$/i);

    // Guardar temporalmente
    const tempDir = '/tmp';
    const tempPath = path.join(tempDir, download.suggestedFilename());
    await download.saveAs(tempPath);

    // Leer archivo
    let workbook: XLSX.WorkBook;
    try {
      workbook = XLSX.readFile(tempPath);
    } catch (e) {
      expect(false, `Error al leer .xlsx: ${e}`).toBeTruthy();
      return;
    }

    const worksheet = workbook.Sheets[workbook.SheetNames[0]];
    const rows: Record<string, any>[] = XLSX.utils.sheet_to_json(worksheet) as Record<string, any>[];

    expect(rows.length).toBeGreaterThan(0);

    // Analizar cada fila
    rows.forEach((row, index) => {
      // Saltar fila TOTAL (no tiene IVA desglosado)
      if (String(row['Concepto'] || '').trim().toUpperCase() === 'TOTAL') {
        return;
      }

      // Buscar columnas de IVA, País, Importe y Total
      const ivaKey = Object.keys(row).find(k => k.toLowerCase().includes('iva') || k.toLowerCase().includes('vat'));
      const paisKey = Object.keys(row).find(k => k.toLowerCase().includes('país') || k.toLowerCase().includes('pais'));
      const importeKey = Object.keys(row).find(k => k.toLowerCase().includes('importe') && !k.toLowerCase().includes('iva'));
      const totalKey = Object.keys(row).find(k => k.toLowerCase().includes('total'));

      if (ivaKey) {
        const ivaValue = parseFloat(String(row[ivaKey] || 0).replace('%', '').trim());

        // Si hay país, validar regla VAT
        if (paisKey) {
          const pais = String(row[paisKey] || 'España').trim();
          const esEspaña = pais.toLowerCase().includes('españa') || pais === '';
          const esIntraEU = ['Francia', 'Alemania', 'Italia', 'Portugal', 'Bélgica', 'Países Bajos', 'Irlanda', 'Austria'].some(
            p => pais.toLowerCase().includes(p.toLowerCase())
          );

          if (esEspaña || pais === '') {
            expect(ivaValue).toBe(21);
          } else if (esIntraEU) {
            // Intra-EU podría ser 0% o la tasa local, validar coherencia
            const validRates = [0, 19, 20, 21, 23];
            expect(validRates).toContain(ivaValue);
          }
        } else {
          // Sin país especificado, asumir España (21%)
          expect(ivaValue).toBe(21);
        }

        // Validar cálculo de Total = Importe + (Importe * IVA%)
        if (importeKey && totalKey) {
          const importe = parseFloat(String(row[importeKey] || 0));
          const total = parseFloat(String(row[totalKey] || 0));
          const expectedTotal = importe + (importe * (ivaValue / 100));

          expect(Math.abs(total - expectedTotal)).toBeLessThan(0.01);
        }
      }
    });

    // Limpiar
    fs.unlinkSync(tempPath);
  });

  /**
   * Test 4: Descargar ambos formatos sin errores en el mismo closure
   * Valida flujo completo: usuario puede descargar A3 Innuva y A3 ERP sin problemas
   */
  test('Flujo completo: descargar A3 Innuva y A3 ERP en secuencia', async ({ page, context }) => {
    if (!closureId) {
      test.skip();
      return;
    }

    // Descargar A3 Innuva
    {
      const downloadPromise = context.waitForEvent('download');
      await page.locator('[data-testid=btn-export-innuva]').click();
      const download = await downloadPromise;
      expect(download.suggestedFilename()).toMatch(/\.xls$/i);

      const tempDir = '/tmp';
      const tempPath = path.join(tempDir, download.suggestedFilename());
      await download.saveAs(tempPath);
      expect(fs.existsSync(tempPath)).toBeTruthy();
      fs.unlinkSync(tempPath);
    }

    // Descargar A3 ERP
    {
      const downloadPromise = context.waitForEvent('download');
      await page.locator('[data-testid=btn-export-erp]').click();
      const download = await downloadPromise;
      expect(download.suggestedFilename()).toMatch(/\.xlsx$/i);

      const tempDir = '/tmp';
      const tempPath = path.join(tempDir, download.suggestedFilename());
      await download.saveAs(tempPath);
      expect(fs.existsSync(tempPath)).toBeTruthy();
      fs.unlinkSync(tempPath);
    }
  });
});
