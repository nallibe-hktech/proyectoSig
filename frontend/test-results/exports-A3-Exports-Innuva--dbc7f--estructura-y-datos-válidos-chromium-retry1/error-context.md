# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: exports.spec.ts >> A3 Exports (Innuva .xls y ERP .xlsx) >> Descargar A3 Innuva (.xls) - estructura y datos válidos
- Location: e2e\exports.spec.ts:94:7

# Error details

```
Error: expect(received).toBeGreaterThan(expected)

Expected: > 0
Received:   0
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
              - button "A3 Innuva" [active] [ref=e105] [cursor=pointer]:
                - img [ref=e106]: download
                - generic [ref=e107]: A3 Innuva
              - button "A3 ERP" [ref=e110]:
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
    - generic [ref=e319]: Export A3 Innuva descargado
    - button "Cerrar" [ref=e321]:
      - generic [ref=e322]: Cerrar
```

# Test source

```ts
  41  | 
  42  |     // Probar cierres del 1 al 5 hasta encontrar uno con botones de exportación
  43  |     for (let closureNum = 1; closureNum <= 5; closureNum++) {
  44  |       try {
  45  |         console.log(`Trying closure ${closureNum}...`);
  46  |         await page.goto(`/closures/${closureNum}`, { waitUntil: 'domcontentloaded' });
  47  | 
  48  |         // Esperar a que algo cargue (KPI cards)
  49  |         const kpiLoaded = await page
  50  |           .waitForSelector('[data-testid="kpi-coste-total"]', { timeout: 5_000 })
  51  |           .catch(() => null);
  52  | 
  53  |         if (!kpiLoaded) {
  54  |           console.log(`Closure ${closureNum}: KPI not found, trying next`);
  55  |           continue;
  56  |         }
  57  | 
  58  |         // Buscar los botones de exportación
  59  |         const innuvaBtn = await page
  60  |           .locator('[data-testid=btn-export-innuva]')
  61  |           .isVisible()
  62  |           .catch(() => false);
  63  |         const erpBtn = await page
  64  |           .locator('[data-testid=btn-export-erp]')
  65  |           .isVisible()
  66  |           .catch(() => false);
  67  | 
  68  |         if (innuvaBtn && erpBtn) {
  69  |           closureId = closureNum;
  70  |           console.log(`✓ Found closure ${closureNum} with export buttons visible`);
  71  |           break;
  72  |         } else {
  73  |           console.log(`Closure ${closureNum}: Export buttons not visible (state may not be Aprobado)`);
  74  |         }
  75  |       } catch (e) {
  76  |         console.log(`Error checking closure ${closureNum}: ${e}`);
  77  |       }
  78  |     }
  79  | 
  80  |     if (!closureId) {
  81  |       console.log('No closure with export buttons found in closures 1-5');
  82  |       test.skip();
  83  |     }
  84  |   });
  85  | 
  86  |   /**
  87  |    * Test 1: Descargar A3 Innuva (.xls)
  88  |    * Valida:
  89  |    *  - Archivo se descarga con extensión .xls
  90  |    *  - NPOI format es válido
  91  |    *  - Contiene columnas esperadas: Empresa, Imputación, Tipo de Paga, Importe Bruto, SS Trabajador, IRPF, etc.
  92  |    *  - Datos están presentes (no vacío)
  93  |    */
  94  |   test('Descargar A3 Innuva (.xls) - estructura y datos válidos', async ({ page, context }) => {
  95  |     if (!closureId) {
  96  |       test.skip();
  97  |       return;
  98  |     }
  99  | 
  100 |     const downloadPromise = context.waitForEvent('download');
  101 | 
  102 |     // Click en botón "A3 Innuva"
  103 |     const exportBtn = page.locator('[data-testid=btn-export-innuva]');
  104 |     await expect(exportBtn).toBeVisible({ timeout: 5_000 });
  105 |     await exportBtn.click();
  106 | 
  107 |     // Esperar descarga
  108 |     const download = await downloadPromise;
  109 | 
  110 |     // Validar que el archivo existe y tiene extensión .xls
  111 |     expect(download.suggestedFilename()).toMatch(/\.xls$/i);
  112 |     expect(download.suggestedFilename()).toContain('A3Innuva');
  113 | 
  114 |     // Guardar temporalmente para lectura
  115 |     const tempDir = '/tmp';
  116 |     const tempPath = path.join(tempDir, download.suggestedFilename());
  117 |     await download.saveAs(tempPath);
  118 | 
  119 |     // Verificar que el archivo existe
  120 |     expect(fs.existsSync(tempPath)).toBeTruthy();
  121 | 
  122 |     // Intentar leer el archivo con XLSX
  123 |     let workbook: XLSX.WorkBook;
  124 |     try {
  125 |       workbook = XLSX.readFile(tempPath);
  126 |     } catch (e) {
  127 |       expect(false, `No se pudo leer el archivo .xls como XLSX válido: ${e}`).toBeTruthy();
  128 |       return;
  129 |     }
  130 | 
  131 |     // Validar que hay al menos una sheet
  132 |     expect(workbook.SheetNames.length).toBeGreaterThan(0);
  133 | 
  134 |     const firstSheet = workbook.SheetNames[0];
  135 |     const worksheet = workbook.Sheets[firstSheet];
  136 | 
  137 |     // Convertir a array de objetos
  138 |     const rows: Record<string, any>[] = XLSX.utils.sheet_to_json(worksheet) as Record<string, any>[];
  139 | 
  140 |     // Validar que hay datos (al menos 1 fila)
> 141 |     expect(rows.length).toBeGreaterThan(0);
      |                         ^ Error: expect(received).toBeGreaterThan(expected)
  142 | 
  143 |     // Validar que al menos una fila tiene datos
  144 |     const firstRow = rows[0];
  145 |     expect(Object.keys(firstRow).length).toBeGreaterThan(0);
  146 | 
  147 |     // Columnas esperadas para A3 Innuva (pueden variar según la implementación)
  148 |     // Verificar que contiene al menos algunas columnas clave: Empresa, Imputación, Importe, etc.
  149 |     const expectedColumns = [
  150 |       'Empresa',
  151 |       'Imputación',
  152 |       'Tipo de Paga',
  153 |       'Importe Bruto',
  154 |       'SS Trabajador',
  155 |       'IRPF'
  156 |     ];
  157 | 
  158 |     const foundColumns = expectedColumns.filter(col =>
  159 |       Object.keys(firstRow).some(key => key.toLowerCase().includes(col.toLowerCase()))
  160 |     );
  161 | 
  162 |     // Al menos 3 columnas esperadas deben estar presentes
  163 |     expect(foundColumns.length).toBeGreaterThanOrEqual(3);
  164 | 
  165 |     // Limpiar archivo temporal
  166 |     fs.unlinkSync(tempPath);
  167 |   });
  168 | 
  169 |   /**
  170 |    * Test 2: Descargar A3 ERP (.xlsx)
  171 |    * Valida:
  172 |    *  - Archivo se descarga con extensión .xlsx
  173 |    *  - ClosedXML format es válido
  174 |    *  - Contiene columnas: Cliente NIF, Cliente Nombre, Proyecto, Concepto, Importe, IVA%, Total
  175 |    *  - Datos están presentes
  176 |    */
  177 |   test('Descargar A3 ERP (.xlsx) - estructura y datos válidos', async ({ page, context }) => {
  178 |     if (!closureId) {
  179 |       test.skip();
  180 |       return;
  181 |     }
  182 | 
  183 |     const downloadPromise = context.waitForEvent('download');
  184 | 
  185 |     // Click en botón "A3 ERP"
  186 |     const exportBtn = page.locator('[data-testid=btn-export-erp]');
  187 |     await expect(exportBtn).toBeVisible({ timeout: 5_000 });
  188 |     await exportBtn.click();
  189 | 
  190 |     // Esperar descarga
  191 |     const download = await downloadPromise;
  192 | 
  193 |     // Validar que el archivo existe y tiene extensión .xlsx
  194 |     expect(download.suggestedFilename()).toMatch(/\.xlsx$/i);
  195 |     expect(download.suggestedFilename()).toContain('A3ERP');
  196 | 
  197 |     // Guardar temporalmente para lectura
  198 |     const tempDir = '/tmp';
  199 |     const tempPath = path.join(tempDir, download.suggestedFilename());
  200 |     await download.saveAs(tempPath);
  201 | 
  202 |     // Verificar que el archivo existe
  203 |     expect(fs.existsSync(tempPath)).toBeTruthy();
  204 | 
  205 |     // Intentar leer el archivo con XLSX
  206 |     let workbook: XLSX.WorkBook;
  207 |     try {
  208 |       workbook = XLSX.readFile(tempPath);
  209 |     } catch (e) {
  210 |       expect(false, `No se pudo leer el archivo .xlsx como XLSX válido: ${e}`).toBeTruthy();
  211 |       return;
  212 |     }
  213 | 
  214 |     // Validar que hay al menos una sheet
  215 |     expect(workbook.SheetNames.length).toBeGreaterThan(0);
  216 | 
  217 |     const firstSheet = workbook.SheetNames[0];
  218 |     const worksheet = workbook.Sheets[firstSheet];
  219 | 
  220 |     // Convertir a array de objetos
  221 |     const rows: Record<string, any>[] = XLSX.utils.sheet_to_json(worksheet) as Record<string, any>[];
  222 | 
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
```