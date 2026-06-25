# RETOMA — Alineación con el penpot definitivo

**Fecha:** 2026-06-25 · **Rama:** `feat/pantalla-a3-erp` · **Estado:** TODO SIN COMMITEAR

Fuente de verdad de UI: `Proyecto sig demo.penpot` (raíz, penpot 2.16.1). 31 frames = **23 pantallas únicas** + 8 estados. Menú canónico en 3 grupos: **Principal / Administración / Configuración**.

---

## ✅ HECHO esta sesión (pendiente de commit)

| Área | Qué se hizo | Archivos |
|------|-------------|----------|
| **Traspaso CECOs** | Pantalla nueva (maqueta front-only, datos ilustrativos). Ruta `/traspaso-cecos`. | `features/traspaso-cecos/traspaso-cecos.component.ts` |
| **Errores Nómina/Pagos** | Pantalla nueva (maqueta). Tabla validación por recurso + "Generar fichero nómina" bloqueado por bloqueantes. Ruta `/errores-nomina`. | `features/errores/errores-nomina.component.ts` |
| **Errores Facturación** | Pantalla nueva (maqueta). Tipo A (presupuesto) + Tipo B (costes con drill-down "Ver exceso"). Ruta `/errores-facturacion`. | `features/errores/errores-facturacion.component.ts` |
| **Contabilidad** | Ampliado el hub `a3-erp`: bloques maqueta "Cierres aprobados a contabilizar", "Costes externos/logística (pagado vs refacturado)", "Historial de envíos". Conserva el export/import REAL. | `features/a3-erp/a3-erp.component.ts` |
| **Sidebar** | Reescrito en español + 4 grupos (Principal/Administración/Configuración/Integraciones). Ensanchado **168→224px** (nombres ya no se cortan). Integraciones (Galán, Mediapost, etc.) degradadas a su grupo. | `layout/shell/shell.component.ts` + `.html`, `styles.scss` |
| **Aprobaciones** | Añadidos paneles MAQUETA (badge DEMO): "Vista dinamizada — Pagos" (pivote empleado×conceptos) y "Borrador de Facturación" con historial de versiones. Lógica real intacta. | `features/approvals/approvals.component.ts` |
| **Forecast** | Integrado como **pestaña** dentro de Config. Presupuesto (toggle "Presupuesto confirmado \| Forecast ventas / GPP"). `ForecastResumenComponent` ganó input `embedded`. Quitado del sidebar (ruta `/forecast` se mantiene). | `features/config-presupuesto/...`, `features/forecast/forecast-resumen.component.ts`, `shell` |
| **Conceptos-Facturación** | REVISADO → sin cambios. La agrupación por categorías vive solo en Config. Factura; el editor de conceptos solo define fórmula. El split actual es mejor que la maqueta penpot. | (ninguno) |
| **Dashboard** | Añadido footer de marca ("SIG-ES Plataforma Integral v1.0 · h&k consulting · © 2026"). El resto ya estaba alineado (KPIs, evolución, donut, Mis Servicios, Alertas). | `features/dashboard/dashboard.component.ts` |
| **Bugs corregidos** | (1) locale `'es'` no registrado → importes vacíos + UI inestable → `registerLocaleData(localeEs,'es')`. (2) `€ 15.000` se partía en 2 líneas en Contabilidad → `white-space:nowrap`. | `app.config.ts`, `a3-erp.component.ts` |

Verificado: `ng build --configuration development` **verde** + revisión visual con Playwright (sesión admin inyectada en sessionStorage).

---

## ⏳ PENDIENTE — para continuar

### 0. Commit
Nada commiteado aún. Sugerencia de commits temáticos:
- `feat(penpot): pantallas Traspaso CECOs + Errores Nómina/Facturación (maqueta)`
- `feat(contabilidad): ampliar hub a3-erp con visión contable`
- `feat(nav): sidebar en español + grupos + ancho`
- `feat(aprobaciones): vista dinamizada + borrador facturación (maqueta)`
- `feat(presupuesto): Forecast como pestaña`
- `fix(i18n): registrar locale es; fix wrap importes Contabilidad`

### 1. Revisión de FIDELIDAD de pantallas existentes (diff fino NO hecho)
Existen y están alineadas a grandes rasgos, pero falta contrastar detalle contra el penpot:
- **Roles** — matriz de permisos completa (9 roles × Pagos/Facturaciones/Usuarios/Roles, ámbito Global/Proyecto) + nota "Editar incluye Eliminar · datos de Celero".
- **Periodos** — detalle de cierres (Facturación día 9; Nóminas grupo A día 30 / grupo B día 15), ciclo de vida, "Cierre por servicio" (Nómina/Factura OK/Alerta).
- **Clientes / Servicios / CECOs / Departamentos / Usuarios** — paneles de detalle (origen Celero vs entrada manual, resumen facturación, usuarios vinculados, asignaciones).
- **Conceptos — editor de fórmula** — variables (Σ Gasto Payhawk, Nº Visitas Celero…), operaciones, Preview del cálculo, jerarquía Global/Servicio/Empleado.
- **Login** — botón Azure AD / Microsoft SSO + branding "h&k consulting · © 2026".
- **Dashboard** — el "objetivo" sigue placeholder (ver SUP-07).

> **Informes**: el penpot aún dibuja Power BI, pero la decisión firme es **nativo** (ver `informes-nativos-no-powerbi`). NO regresar a Power BI.

### 2. Wiring a backend de las pantallas nuevas (hoy son maqueta front-only)
Cuando haya datos/spec:
- **Traspaso CECOs** — endpoint real (sumar pagos donde CECO de contrato ≠ CECO de actividad; regla < 1.000 € por departamento).
- **Errores Nómina/Pagos** y **Errores Facturación** — conectar al motor cuando llegue el catálogo de validaciones de SIG.
- **Contabilidad** — los 3 bloques maqueta (cierres a contabilizar, costes externos, historial) → datos reales.
- **Aprobaciones** — pivote dinamizado + borrador facturación con datos reales (hoy demo).

### 3. Bloqueado por SIG (terceros) — las maquetas ya lo marcan con ⚠
- **Errores Nómina**: lista cerrada de tipos de error/severidades ("otro documento" no entregado).
- **Errores Facturación**: umbral de desviación ±x% (sin fijar); costes de logística/externos dependen del cierre de Contabilidad/Config. Factura.
- **Traspaso CECOs**: columnas exactas de importe, qué selecciona el filtro "Cierre", significado del color de fila, subtotales (consultar a **Yoana / Martha**).
- **Config. Factura / Config. Presupuesto**: categorías y partidas reales por cliente pendientes de validar.

---

## Notas técnicas para retomar
- Levantar front: `cd frontend && npx ng serve --port 4321`.
- Para ver pantallas con guardas de rol sin backend: inyectar en sessionStorage `sig_access_token`, `sig_refresh_token` y `sig_current_user` (JSON `UsuarioBriefDto` con `roles:['Administrator','Fico','RRHH']`).
- Las maquetas reutilizan el sistema de diseño `--sig-*` de `features/closures/alerts-list.component.ts`.
- Rol correcto = `'RRHH'` (mayúsculas), NO `'Rrhh'`.
