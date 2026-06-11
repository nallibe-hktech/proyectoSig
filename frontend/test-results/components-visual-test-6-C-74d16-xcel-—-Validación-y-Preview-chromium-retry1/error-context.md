# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: components-visual-test.spec.ts >> 6 Componentes Frontend - Prueba Visual Completa >> 6️⃣ Importación Excel — Validación y Preview
- Location: e2e\components-visual-test.spec.ts:271:7

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
  218 |     // Screenshot
  219 |     await page.screenshot({ path: 'test-results/04-override-dialog.png', fullPage: true });
  220 |     console.log('  📸 Screenshot guardado: test-results/04-override-dialog.png\n');
  221 |   });
  222 | 
  223 |   // ===== TEST 5: BÚSQUEDA FULL-TEXT =====
  224 |   test('5️⃣ Búsqueda Full-text — Multi-campo', async () => {
  225 |     console.log('\n🔍 Probando Búsqueda Full-text...');
  226 | 
  227 |     await page.goto(`${baseUrl}/projects`);
  228 |     await page.waitForTimeout(2000);
  229 | 
  230 |     // Buscar componente de búsqueda
  231 |     const searchComponent = await page.$('app-fulltext-search');
  232 |     console.log('  ✓ Search component visible:', !!searchComponent);
  233 | 
  234 |     const searchInput = await page.$('app-fulltext-search input');
  235 |     if (searchInput) {
  236 |       // Prueba búsqueda
  237 |       await searchInput.fill('test');
  238 |       await page.waitForTimeout(500);
  239 | 
  240 |       // Verifica estadísticas
  241 |       const stats = await page.locator('.sig-search-stats').innerText();
  242 |       console.log('  ✓ Search stats:', stats.substring(0, 50) + '...');
  243 | 
  244 |       // Verifica filtros de columna (si existen)
  245 |       const columnFilters = await page.locator('.sig-field-chip');
  246 |       const filterCount = await columnFilters.count();
  247 |       console.log('  ✓ Column filters encontrados:', filterCount);
  248 | 
  249 |       // Verifica resultados
  250 |       const results = await page.locator('tbody tr').count();
  251 |       console.log('  ✓ Filas visibles tras búsqueda:', results);
  252 | 
  253 |       // Prueba typo (Levenshtein)
  254 |       await searchInput.clear();
  255 |       await searchInput.fill('projct'); // typo: falta 'e'
  256 |       await page.waitForTimeout(500);
  257 | 
  258 |       const suggestions = await page.locator('.sig-suggestion-btn');
  259 |       const suggestionCount = await suggestions.count();
  260 |       console.log('  ✓ Sugerencias fuzzy encontradas:', suggestionCount, '(Levenshtein distance <= 2)');
  261 |     }
  262 | 
  263 |     // Screenshot
  264 |     await page.screenshot({ path: 'test-results/05-fulltext-search.png', fullPage: true });
  265 |     console.log('  📸 Screenshot guardado: test-results/05-fulltext-search.png\n');
  266 | 
  267 |     expect(searchComponent).toBeTruthy();
  268 |   });
  269 | 
  270 |   // ===== TEST 6: IMPORTACIÓN EXCEL =====
  271 |   test('6️⃣ Importación Excel — Validación y Preview', async () => {
  272 |     console.log('\n📤 Probando Importación Excel...');
  273 | 
  274 |     await page.goto(`${baseUrl}/projects`);
  275 |     await page.waitForTimeout(2000);
  276 | 
  277 |     // Busca el componente de importación (generalmente en un tab)
  278 |     const importTab = await page.$('button:has-text("Importar")');
  279 |     if (importTab) {
  280 |       await importTab.click();
  281 |       await page.waitForTimeout(1000);
  282 |     }
  283 | 
  284 |     // Buscar componente de importación
  285 |     const importComponent = await page.$('app-excel-import');
  286 |     console.log('  ✓ Import component visible:', !!importComponent);
  287 | 
  288 |     // Buscar zona de upload
  289 |     const uploadZone = await page.$('.sig-upload-zone');
  290 |     console.log('  ✓ Upload zone visible:', !!uploadZone);
  291 | 
  292 |     // Crear archivo Excel de prueba
  293 |     const fs = require('fs');
  294 |     const path = require('path');
  295 | 
  296 |     // Crear archivo Excel mínimo (XLSX)
  297 |     try {
  298 |       const testFile = path.join(__dirname, '../../test-import.xlsx');
  299 |       // Nota: Para prueba real, necesitarías crear un archivo XLSX válido
  300 |       // Por ahora, verificamos que el componente está presente
  301 | 
  302 |       console.log('  ℹ️ Componente preparado para upload (archivo real requiere XLSX válido)');
  303 |     } catch (err) {
  304 |       console.log('  ⚠️ No se pudo crear archivo de prueba:', err);
  305 |     }
  306 | 
  307 |     // Verificar elementos del componente
  308 |     const fileInput = await page.$('input[type="file"]');
  309 |     console.log('  ✓ File input visible:', !!fileInput);
  310 | 
  311 |     const uploadButton = await page.$('button:has-text("Seleccionar archivo")');
  312 |     console.log('  ✓ Upload button visible:', !!uploadButton);
  313 | 
  314 |     // Screenshot
  315 |     await page.screenshot({ path: 'test-results/06-excel-import.png', fullPage: true });
  316 |     console.log('  📸 Screenshot guardado: test-results/06-excel-import.png\n');
  317 | 
> 318 |     expect(importComponent).toBeTruthy();
      |                             ^ Error: expect(received).toBeTruthy()
  319 |   });
  320 | 
  321 |   // ===== TEST 7: RESPONSIVIDAD =====
  322 |   test('7️⃣ Responsividad — Mobile / Tablet / Desktop', async () => {
  323 |     console.log('\n📱 Probando Responsividad...');
  324 | 
  325 |     // Mobile (375px)
  326 |     await page.setViewportSize({ width: 375, height: 667 });
  327 |     await page.goto(`${baseUrl}/dashboard`);
  328 |     await page.waitForTimeout(1000);
  329 |     await page.screenshot({ path: 'test-results/07a-mobile.png', fullPage: true });
  330 |     console.log('  📸 Mobile screenshot: test-results/07a-mobile.png');
  331 | 
  332 |     // Tablet (768px)
  333 |     await page.setViewportSize({ width: 768, height: 1024 });
  334 |     await page.goto(`${baseUrl}/dashboard`);
  335 |     await page.waitForTimeout(1000);
  336 |     await page.screenshot({ path: 'test-results/07b-tablet.png', fullPage: true });
  337 |     console.log('  📸 Tablet screenshot: test-results/07b-tablet.png');
  338 | 
  339 |     // Desktop (1920px)
  340 |     await page.setViewportSize({ width: 1920, height: 1080 });
  341 |     await page.goto(`${baseUrl}/dashboard`);
  342 |     await page.waitForTimeout(1000);
  343 |     await page.screenshot({ path: 'test-results/07c-desktop.png', fullPage: true });
  344 |     console.log('  📸 Desktop screenshot: test-results/07c-desktop.png\n');
  345 | 
  346 |     expect(true).toBeTruthy();
  347 |   });
  348 | 
  349 |   // ===== TEST 8: ERRORES DE CONSOLA =====
  350 |   test('8️⃣ Verificar Consola — Sin Errores TypeScript/API', async () => {
  351 |     console.log('\n⚠️ Verificando Console Errors...');
  352 | 
  353 |     const errors: string[] = [];
  354 |     const warnings: string[] = [];
  355 | 
  356 |     page.on('console', (msg) => {
  357 |       if (msg.type() === 'error') {
  358 |         errors.push(msg.text());
  359 |       } else if (msg.type() === 'warning') {
  360 |         warnings.push(msg.text());
  361 |       }
  362 |     });
  363 | 
  364 |     // Navega por todas las páginas
  365 |     await page.goto(`${baseUrl}/dashboard`);
  366 |     await page.waitForTimeout(2000);
  367 | 
  368 |     await page.goto(`${baseUrl}/approvals`);
  369 |     await page.waitForTimeout(2000);
  370 | 
  371 |     await page.goto(`${baseUrl}/projects`);
  372 |     await page.waitForTimeout(2000);
  373 | 
  374 |     console.log('  ✓ Console errors encontrados:', errors.length);
  375 |     if (errors.length > 0) {
  376 |       console.log('    Errores:');
  377 |       errors.slice(0, 5).forEach((err) => console.log(`      - ${err.substring(0, 100)}`));
  378 |     }
  379 | 
  380 |     console.log('  ✓ Console warnings encontrados:', warnings.length);
  381 | 
  382 |     expect(errors.length).toBeLessThan(10); // Tolerancia para warnings menores
  383 |   });
  384 | });
  385 | 
```