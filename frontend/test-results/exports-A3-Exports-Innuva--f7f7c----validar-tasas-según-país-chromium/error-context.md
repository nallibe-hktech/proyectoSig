# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: exports.spec.ts >> A3 Exports (Innuva .xls y ERP .xlsx) >> VAT calculation en A3 ERP - validar tasas según país
- Location: e2e\exports.spec.ts:259:7

# Error details

```
Error: expect(received).toBe(expected) // Object.is equality

Expected: 21
Received: 0
```

# Page snapshot

```yaml
- generic [ref=e1]:
  - generic [ref=e4]:
    - generic "Navegación principal" [ref=e6]:
      - generic [ref=e7]:
        - link "SIG Plataforma de Cierres" [ref=e9] [cursor=pointer]:
          - /url: /dashboard
          - generic [ref=e10]: SIG
          - generic [ref=e11]: Plataforma de Cierres
        - separator [ref=e12]
        - generic [ref=e13]: Operativo
        - navigation [ref=e14]:
          - link "Dashboard" [ref=e15] [cursor=pointer]:
            - /url: /dashboard
            - img [ref=e16]: dashboard
            - generic [ref=e17]: Dashboard
          - link "Clients" [ref=e18] [cursor=pointer]:
            - /url: /clients
            - img [ref=e19]: groups
            - generic [ref=e20]: Clients
          - link "Projects" [ref=e21] [cursor=pointer]:
            - /url: /projects
            - img [ref=e22]: folder_open
            - generic [ref=e23]: Projects
          - link "Actions" [ref=e24] [cursor=pointer]:
            - /url: /actions
            - img [ref=e25]: task_alt
            - generic [ref=e26]: Actions
          - link "Concepts" [ref=e27] [cursor=pointer]:
            - /url: /concepts
            - img [ref=e28]: calculate
            - generic [ref=e29]: Concepts
          - link "Variables" [ref=e30] [cursor=pointer]:
            - /url: /variables
            - img [ref=e31]: data_object
            - generic [ref=e32]: Variables
          - link "Periods" [ref=e33] [cursor=pointer]:
            - /url: /periods
            - img [ref=e34]: calendar_month
            - generic [ref=e35]: Periods
          - link "Approvals" [ref=e36] [cursor=pointer]:
            - /url: /approvals
            - img [ref=e37]: approval
            - generic [ref=e38]: Approvals
          - link "Closures" [ref=e39] [cursor=pointer]:
            - /url: /closures
            - img [ref=e40]: lock_clock
            - generic [ref=e41]: Closures
          - link "Reports" [ref=e42] [cursor=pointer]:
            - /url: /reports
            - img [ref=e43]: bar_chart
            - generic [ref=e44]: Reports
        - separator [ref=e45]
        - generic [ref=e46]: Administración
        - navigation [ref=e47]:
          - link "Cost Centers" [ref=e48] [cursor=pointer]:
            - /url: /cost-centers
            - img [ref=e49]: account_balance
            - generic [ref=e50]: Cost Centers
          - link "Departments" [ref=e51] [cursor=pointer]:
            - /url: /departments
            - img [ref=e52]: corporate_fare
            - generic [ref=e53]: Departments
          - link "Roles" [ref=e54] [cursor=pointer]:
            - /url: /roles
            - img [ref=e55]: verified_user
            - generic [ref=e56]: Roles
          - link "Users" [ref=e57] [cursor=pointer]:
            - /url: /users
            - img [ref=e58]: manage_accounts
            - generic [ref=e59]: Users
          - link "Audit Log" [ref=e60] [cursor=pointer]:
            - /url: /audit
            - img [ref=e61]: history
            - generic [ref=e62]: Audit Log
          - link "Sync" [ref=e63] [cursor=pointer]:
            - /url: /sync
            - img [ref=e64]: refresh
            - generic [ref=e65]: Sync
          - link "Celero Visitas" [ref=e66] [cursor=pointer]:
            - /url: /celero-visitas
            - img [ref=e67]: location_on
            - generic [ref=e68]: Celero Visitas
    - generic [ref=e70]:
      - banner [ref=e71]:
        - button "Abrir o cerrar menú de navegación" [ref=e72] [cursor=pointer]:
          - img [ref=e73]: menu_open
        - link "SIG Plataforma de Cierres" [ref=e76] [cursor=pointer]:
          - /url: /dashboard
          - generic [ref=e77]: SIG
          - generic [ref=e78]: Plataforma de Cierres
        - combobox "Seleccionar período activo" [ref=e79]:
          - generic [ref=e80] [cursor=pointer]:
            - generic [ref=e82]: Marzo 2026
            - img [ref=e85]
        - generic [ref=e87]: Admin SIG
        - button "Menú de usuario" [ref=e88] [cursor=pointer]:
          - img [ref=e89]: account_circle
      - main [ref=e92]:
        - generic [ref=e94]:
          - navigation "Breadcrumb" [ref=e96]:
            - link "Inicio" [ref=e97] [cursor=pointer]:
              - /url: /dashboard
            - img [ref=e98]: chevron_right
            - link "Closures" [ref=e99] [cursor=pointer]:
              - /url: /closures
            - img [ref=e100]: chevron_right
            - generic [ref=e101]: Alpha - Implantación Madrid — Noviembre 2025
          - generic [ref=e102]:
            - heading "Alpha - Implantación Madrid — Noviembre 2025" [level=1] [ref=e103]
            - generic [ref=e104]:
              - button "A3 Innuva" [ref=e105]:
                - img [ref=e106]: download
                - generic [ref=e107]: A3 Innuva
              - button "A3 ERP" [active] [ref=e110] [cursor=pointer]:
                - img [ref=e111]: download
                - generic [ref=e112]: A3 ERP
          - generic [ref=e115]:
            - strong [ref=e116]: "Estado:"
            - generic [ref=e118]: Aprobado
            - generic [ref=e119]: Paso 5 de 5
          - generic [ref=e120]:
            - generic [ref=e122]:
              - generic [ref=e123]: Coste total
              - generic [ref=e124]: 1,860 €
            - generic [ref=e126]:
              - generic [ref=e127]: Facturación
              - generic [ref=e128]: 2,040 €
            - generic [ref=e130]:
              - generic [ref=e131]: Margen
              - generic [ref=e132]: 180 €
              - generic [ref=e133]: 8.8%
          - generic [ref=e134]:
            - generic [ref=e137]: Líneas de cierre
            - table [ref=e139]:
              - rowgroup [ref=e140]:
                - row "Concepto Tipo Usuario Importe" [ref=e141]:
                  - columnheader "Concepto" [ref=e142]
                  - columnheader "Tipo" [ref=e143]
                  - columnheader "Usuario" [ref=e144]
                  - columnheader "Importe" [ref=e145]
                  - columnheader [ref=e146]
                  - columnheader [ref=e147]
              - rowgroup [ref=e148]:
                - row "Suma de gastos directos Pago — 360 € Ver detalle" [ref=e149]:
                  - cell "Suma de gastos directos" [ref=e150]
                  - cell "Pago" [ref=e151]:
                    - generic [ref=e155]: Pago
                  - cell "—" [ref=e156]
                  - cell "360 €" [ref=e157]
                  - cell [ref=e158]
                  - cell "Ver detalle" [ref=e159]:
                    - link "Ver detalle" [ref=e160] [cursor=pointer]:
                      - /url: /calculations/57
                      - img [ref=e161]: visibility
                - row "Bonus por visita estándar Pago — 0 € Ver detalle" [ref=e164]:
                  - cell "Bonus por visita estándar" [ref=e165]
                  - cell "Pago" [ref=e166]:
                    - generic [ref=e170]: Pago
                  - cell "—" [ref=e171]
                  - cell "0 €" [ref=e172]
                  - cell [ref=e173]:
                    - img [ref=e174]: warning
                  - cell "Ver detalle" [ref=e175]:
                    - link "Ver detalle" [ref=e176] [cursor=pointer]:
                      - /url: /calculations/58
                      - img [ref=e177]: visibility
                - row "Bonus por visita premium Pago — 0 € Ver detalle" [ref=e180]:
                  - cell "Bonus por visita premium" [ref=e181]
                  - cell "Pago" [ref=e182]:
                    - generic [ref=e186]: Pago
                  - cell "—" [ref=e187]
                  - cell "0 €" [ref=e188]
                  - cell [ref=e189]:
                    - img [ref=e190]: warning
                  - cell "Ver detalle" [ref=e191]:
                    - link "Ver detalle" [ref=e192] [cursor=pointer]:
                      - /url: /calculations/59
                      - img [ref=e193]: visibility
                - row "Pago por horas trabajadas Pago — 1,500 € Ver detalle" [ref=e196]:
                  - cell "Pago por horas trabajadas" [ref=e197]
                  - cell "Pago" [ref=e198]:
                    - generic [ref=e202]: Pago
                  - cell "—" [ref=e203]
                  - cell "1,500 €" [ref=e204]
                  - cell [ref=e205]
                  - cell "Ver detalle" [ref=e206]:
                    - link "Ver detalle" [ref=e207] [cursor=pointer]:
                      - /url: /calculations/60
                      - img [ref=e208]: visibility
                - row "Pago por implantación completada Pago — 0 € Ver detalle" [ref=e211]:
                  - cell "Pago por implantación completada" [ref=e212]
                  - cell "Pago" [ref=e213]:
                    - generic [ref=e217]: Pago
                  - cell "—" [ref=e218]
                  - cell "0 €" [ref=e219]
                  - cell [ref=e220]:
                    - img [ref=e221]: warning
                  - cell "Ver detalle" [ref=e222]:
                    - link "Ver detalle" [ref=e223] [cursor=pointer]:
                      - /url: /calculations/61
                      - img [ref=e224]: visibility
                - row "Facturación por visita Factura — 126 € Ver detalle" [ref=e227]:
                  - cell "Facturación por visita" [ref=e228]
                  - cell "Factura" [ref=e229]:
                    - generic [ref=e233]: Factura
                  - cell "—" [ref=e234]
                  - cell "126 €" [ref=e235]
                  - cell [ref=e236]
                  - cell "Ver detalle" [ref=e237]:
                    - link "Ver detalle" [ref=e238] [cursor=pointer]:
                      - /url: /calculations/62
                      - img [ref=e239]: visibility
                - row "Mensualidad fija proyecto Factura — 1,500 € Ver detalle" [ref=e242]:
                  - cell "Mensualidad fija proyecto" [ref=e243]
                  - cell "Factura" [ref=e244]:
                    - generic [ref=e248]: Factura
                  - cell "—" [ref=e249]
                  - cell "1,500 €" [ref=e250]
                  - cell [ref=e251]
                  - cell "Ver detalle" [ref=e252]:
                    - link "Ver detalle" [ref=e253] [cursor=pointer]:
                      - /url: /calculations/63
                      - img [ref=e254]: visibility
                - row "Refacturación gastos Factura — 414 € Ver detalle" [ref=e257]:
                  - cell "Refacturación gastos" [ref=e258]
                  - cell "Factura" [ref=e259]:
                    - generic [ref=e263]: Factura
                  - cell "—" [ref=e264]
                  - cell "414 €" [ref=e265]
                  - cell [ref=e266]
                  - cell "Ver detalle" [ref=e267]:
                    - link "Ver detalle" [ref=e268] [cursor=pointer]:
                      - /url: /calculations/64
                      - img [ref=e269]: visibility
          - generic [ref=e272]:
            - generic [ref=e275]: Historial de aprobación
            - table [ref=e277]:
              - rowgroup [ref=e278]:
                - row "Fecha Usuario Acción Motivo" [ref=e279]:
                  - columnheader "Fecha" [ref=e280]
                  - columnheader "Usuario" [ref=e281]
                  - columnheader "Acción" [ref=e282]
                  - columnheader "Motivo" [ref=e283]
              - rowgroup [ref=e284]:
                - row "08/05/2026 09:31 María García Aprobar (ProjectManager → Backoffice) —" [ref=e285]:
                  - cell "08/05/2026 09:31" [ref=e286]
                  - cell "María García" [ref=e287]
                  - cell "Aprobar (ProjectManager → Backoffice)" [ref=e288]
                  - cell "—" [ref=e289]
                - row "08/05/2026 10:31 Laura Sánchez Aprobar (Backoffice → Fico) —" [ref=e290]:
                  - cell "08/05/2026 10:31" [ref=e291]
                  - cell "Laura Sánchez" [ref=e292]
                  - cell "Aprobar (Backoffice → Fico)" [ref=e293]
                  - cell "—" [ref=e294]
                - row "08/05/2026 11:31 Javier López Aprobar (Fico → Direction) —" [ref=e295]:
                  - cell "08/05/2026 11:31" [ref=e296]
                  - cell "Javier López" [ref=e297]
                  - cell "Aprobar (Fico → Direction)" [ref=e298]
                  - cell "—" [ref=e299]
                - row "08/05/2026 12:31 Carmen Ruiz Aprobar (Direction → SystemExports) —" [ref=e300]:
                  - cell "08/05/2026 12:31" [ref=e301]
                  - cell "Carmen Ruiz" [ref=e302]
                  - cell "Aprobar (Direction → SystemExports)" [ref=e303]
                  - cell "—" [ref=e304]
          - generic [ref=e305]:
            - generic [ref=e308]: Comentarios
            - paragraph [ref=e310]: Closure seed - estado Aprobado
  - generic [ref=e318]:
    - generic [ref=e319]: Export A3 ERP descargado
    - button "Cerrar" [ref=e321]:
      - generic [ref=e322]: Cerrar
```

# Test source

```ts
  223 |     // Validar que hay datos (al menos 1 fila)
  224 |     expect(rows.length).toBeGreaterThan(0);
  225 | 
  226 |     // Validar que al menos una fila tiene datos
  227 |     const firstRow = rows[0];
  228 |     expect(Object.keys(firstRow).length).toBeGreaterThan(0);
  229 | 
  230 |     // Columnas esperadas para A3 ERP
  231 |     const expectedColumns = [
  232 |       'Cliente NIF',
  233 |       'Cliente Nombre',
  234 |       'Proyecto',
  235 |       'Concepto',
  236 |       'Importe',
  237 |       'IVA%',
  238 |       'Total'
  239 |     ];
  240 | 
  241 |     const foundColumns = expectedColumns.filter(col =>
  242 |       Object.keys(firstRow).some(key => key.toLowerCase().includes(col.toLowerCase().replace(/[%]/g, '')))
  243 |     );
  244 | 
  245 |     // Al menos 4 columnas esperadas deben estar presentes
  246 |     expect(foundColumns.length).toBeGreaterThanOrEqual(4);
  247 | 
  248 |     // Limpiar archivo temporal
  249 |     fs.unlinkSync(tempPath);
  250 |   });
  251 | 
  252 |   /**
  253 |    * Test 3: Cálculo de VAT en A3 ERP
  254 |    * Valida:
  255 |    *  - VAT = 21% para cliente con país España o null (valor por defecto)
  256 |    *  - VAT = 0% para país intra-EU (ej: Francia, Alemania, etc. si existen en seed)
  257 |    *  - Cálculo de Total = Importe + (Importe * VAT%)
  258 |    */
  259 |   test('VAT calculation en A3 ERP - validar tasas según país', async ({ page, context }) => {
  260 |     if (!closureId) {
  261 |       test.skip();
  262 |       return;
  263 |     }
  264 | 
  265 |     const downloadPromise = context.waitForEvent('download');
  266 | 
  267 |     // Click en botón "A3 ERP"
  268 |     const exportBtn = page.locator('[data-testid=btn-export-erp]');
  269 |     await expect(exportBtn).toBeVisible({ timeout: 5_000 });
  270 |     await exportBtn.click();
  271 | 
  272 |     // Esperar descarga
  273 |     const download = await downloadPromise;
  274 |     expect(download.suggestedFilename()).toMatch(/\.xlsx$/i);
  275 | 
  276 |     // Guardar temporalmente
  277 |     const tempDir = '/tmp';
  278 |     const tempPath = path.join(tempDir, download.suggestedFilename());
  279 |     await download.saveAs(tempPath);
  280 | 
  281 |     // Leer archivo
  282 |     let workbook: XLSX.WorkBook;
  283 |     try {
  284 |       workbook = XLSX.readFile(tempPath);
  285 |     } catch (e) {
  286 |       expect(false, `Error al leer .xlsx: ${e}`).toBeTruthy();
  287 |       return;
  288 |     }
  289 | 
  290 |     const worksheet = workbook.Sheets[workbook.SheetNames[0]];
  291 |     const rows: Record<string, any>[] = XLSX.utils.sheet_to_json(worksheet) as Record<string, any>[];
  292 | 
  293 |     expect(rows.length).toBeGreaterThan(0);
  294 | 
  295 |     // Analizar cada fila
  296 |     rows.forEach((row, index) => {
  297 |       // Buscar columnas de IVA, País, Importe y Total
  298 |       const ivaKey = Object.keys(row).find(k => k.toLowerCase().includes('iva') || k.toLowerCase().includes('vat'));
  299 |       const paisKey = Object.keys(row).find(k => k.toLowerCase().includes('país') || k.toLowerCase().includes('pais'));
  300 |       const importeKey = Object.keys(row).find(k => k.toLowerCase().includes('importe') && !k.toLowerCase().includes('iva'));
  301 |       const totalKey = Object.keys(row).find(k => k.toLowerCase().includes('total'));
  302 | 
  303 |       if (ivaKey && (paisKey || true)) {
  304 |         const ivaValue = parseFloat(String(row[ivaKey] || 0).replace('%', '').trim());
  305 | 
  306 |         // Si hay país, validar regla VAT
  307 |         if (paisKey) {
  308 |           const pais = String(row[paisKey] || 'España').trim();
  309 |           const esEspaña = pais.toLowerCase().includes('españa') || pais === '';
  310 |           const esIntraEU = ['Francia', 'Alemania', 'Italia', 'Portugal', 'Bélgica', 'Países Bajos', 'Irlanda', 'Austria'].some(
  311 |             p => pais.toLowerCase().includes(p.toLowerCase())
  312 |           );
  313 | 
  314 |           if (esEspaña || pais === '') {
  315 |             expect(ivaValue).toBe(21);
  316 |           } else if (esIntraEU) {
  317 |             // Intra-EU podría ser 0% o la tasa local, validar coherencia
  318 |             const validRates = [0, 19, 20, 21, 23];
  319 |             expect(validRates).toContain(ivaValue);
  320 |           }
  321 |         } else {
  322 |           // Sin país especificado, asumir España (21%)
> 323 |           expect(ivaValue).toBe(21);
      |                            ^ Error: expect(received).toBe(expected) // Object.is equality
  324 |         }
  325 | 
  326 |         // Validar cálculo de Total = Importe + (Importe * IVA%)
  327 |         if (importeKey && totalKey) {
  328 |           const importe = parseFloat(String(row[importeKey] || 0));
  329 |           const total = parseFloat(String(row[totalKey] || 0));
  330 |           const expectedTotal = importe + (importe * (ivaValue / 100));
  331 | 
  332 |           expect(Math.abs(total - expectedTotal)).toBeLessThan(0.01);
  333 |         }
  334 |       }
  335 |     });
  336 | 
  337 |     // Limpiar
  338 |     fs.unlinkSync(tempPath);
  339 |   });
  340 | 
  341 |   /**
  342 |    * Test 4: Descargar ambos formatos sin errores en el mismo closure
  343 |    * Valida flujo completo: usuario puede descargar A3 Innuva y A3 ERP sin problemas
  344 |    */
  345 |   test('Flujo completo: descargar A3 Innuva y A3 ERP en secuencia', async ({ page, context }) => {
  346 |     if (!closureId) {
  347 |       test.skip();
  348 |       return;
  349 |     }
  350 | 
  351 |     // Descargar A3 Innuva
  352 |     {
  353 |       const downloadPromise = context.waitForEvent('download');
  354 |       await page.locator('[data-testid=btn-export-innuva]').click();
  355 |       const download = await downloadPromise;
  356 |       expect(download.suggestedFilename()).toMatch(/\.xls$/i);
  357 | 
  358 |       const tempDir = '/tmp';
  359 |       const tempPath = path.join(tempDir, download.suggestedFilename());
  360 |       await download.saveAs(tempPath);
  361 |       expect(fs.existsSync(tempPath)).toBeTruthy();
  362 |       fs.unlinkSync(tempPath);
  363 |     }
  364 | 
  365 |     // Descargar A3 ERP
  366 |     {
  367 |       const downloadPromise = context.waitForEvent('download');
  368 |       await page.locator('[data-testid=btn-export-erp]').click();
  369 |       const download = await downloadPromise;
  370 |       expect(download.suggestedFilename()).toMatch(/\.xlsx$/i);
  371 | 
  372 |       const tempDir = '/tmp';
  373 |       const tempPath = path.join(tempDir, download.suggestedFilename());
  374 |       await download.saveAs(tempPath);
  375 |       expect(fs.existsSync(tempPath)).toBeTruthy();
  376 |       fs.unlinkSync(tempPath);
  377 |     }
  378 |   });
  379 | });
  380 | 
```