# Diseño de Pantallas — SIG · Plataforma de Cierres

> Basado en `docs/ARQUITECTURA.md` (RFs) y diseños Penpot verificados.
> Versión: 1.0 | Fecha: Mayo 2026

---

## 1. Layout general

```
┌─────────────────────────────────────────────────────────────────┐
│ Sidebar                         │ Top Bar                        │
│ 260px · gradiente dark          │ 60px · bg white                │
│                                 │ Título · Selector período ·   │
│ Logo SIG                        │ Recalcular · Notificaciones    │
│                                 │                                │
│ PRINCIPAL:                      │ Content (padding: 24px)       │
│  Dashboard       ◄ activo      │                                │
│  Clientes                       │ ┌────────────────────────────┐ │
│  Proyectos                      │ │    router-outlet           │ │
│  Acciones                       │ │                            │ │
│  Conceptos                      │ │                            │ │
│  Periodos                       │ │                            │ │
│  Aprobaciones                   │ └────────────────────────────┘ │
│  Contabilidad                   │                                │
│  Informes                       │ Footer: v1.0 · h&k ©2026     │
│                                 │                                │
│ ADMINISTRACIÓN:                 │                                │
│  Usuarios                       │                                │
│  CECOs                          │                                │
│  Auditoría                      │                                │
│                                 │                                │
│ Perfil usuario (bottom)         │                                │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. Login (RF-A01, RF-A02, RF-A03)

**Ruta:** `/login` | **Archivo SVG:** `penpot-design-login.svg`

### Layout
- Fondo completo con gradiente oscuro `#1F4E78` → `#163A52` → `#0D2A3E`
- Círculos decorativos semitransparentes + patrón de puntos
- **Lado izquierdo:** Branding corporativo "h&k consulting · Plataforma Operativa SIG"
  - Feature pills: "✅ Cierres automatizados", "🔗 9 sistemas integrados", "📊 Power BI en tiempo real", "🔒 Auditoría completa"
- **Lado derecho:** Tarjeta blanca 500×720px con sombra profunda
  - Header accent verde `#70AD47`
  - Logo SIG circular en azul + texto "Iniciar Sesión"
  - Campo email corporativo (en foco con `box-shadow` azul)
  - Campo contraseña con toggle visibilidad (👁)
  - Checkbox "Recordar sesión" + link "¿Olvidaste tu contraseña?"
  - Botón "→ Acceder al Sistema" con gradiente azul
  - Divider "o continuar con" + botón Azure AD (SSO)
  - Footer: versión + copyright + roles disponibles

### Componentes usados
- `mat-card`, `mat-form-field` (outline), `mat-checkbox`, `mat-button` (filled primary)

### Estados
| Estado | Comportamiento |
|--------|---------------|
| Default | Formulario limpio, placeholder en inputs |
| Loading | Botón disabled + spinner, inputs disabled |
| Error | `mat-error` en email/password, snackbar error |
| Success | Redirige a `/dashboard` |

### Accesibilidad
- `aria-label="Correo electrónico"` en input email
- `aria-label="Contraseña"` en input password
- `aria-label="Acceder al sistema"` en botón submit
- Focus visible en todos los elementos interactivos

---

## 3. Dashboard (RF-B01, RF-B02, RF-B03)

**Ruta:** `/dashboard` | **Archivo SVG:** `penpot-design-dashboard.svg`

### Layout
- Top bar: título "Dashboard", selector período "📅 Mayo 2026 ▾", botón "↻ Recalcular", campana notificaciones (badge rojo "3")
- **4 KPI cards** (row, gap 16px):
  - Cierres completados: 12 ▲ +2 — acento `#1F4E78`
  - Pend. aprobación: 3 ⚠ — acento `#FFC107`
  - Facturación total: €450K ▲ +12% — acento `#70AD47`
  - Margen promedio: 28% (obj: 25%) — acento `#163A52`
- **Sección Alertas** (izquierda, 500px): cards con acento semántico
  - ⚠ 3 proyectos pendientes aprobación FICO (bg `#FFF3E0`, acento `#FFC107`)
  - ❌ 1 período bloqueado (bg `#FFEBEE`, acento `#D32F2F`)
  - ✅ Cierre abril completado (bg `#E8F5E9`, acento `#70AD47`)
- **Gráfico barras** (derecha, 580px): "Margen por Proyecto — Mayo 2026"
  - Barras: Amex SS 32%, Granini 25%, Amex New 28%, Proj D 21%
  - Línea discontinua objetivo 25% en verde
- **Tabla Proyectos Activos**: columnas PROYECTO/CLIENTE/ESTADO/COSTE/FACTURACIÓN/MARGEN/ACCIONES
  - Filas con datos + fila totales
- **Barra Integraciones**: dots verde/amarillo/rojo para los 7 sistemas

### Componentes usados
- `mat-card` (KPI), `mat-table` o tabla personalizada, badges semánticos, `mat-icon`

### Estados
| Estado | Comportamiento |
|--------|---------------|
| Carga | Skeleton shimmer en KPI + tabla |
| Vacío | Empty state "No hay datos para el período" |
| Error | Snackbar error + retry |
| Datos | KPIs, alertas, tabla con datos reales |

---

## 4. Clientes (RF-C01)

**Ruta:** `/clients` | **RF:** CRUD Cliente

### Layout
- Top bar con título "📁 Clientes" + botón "+ Nuevo Cliente"
- Barra de filtros: BUSCAR texto + botones Filtrar/Limpiar + badge total
- Tabla con cabecera dark: ID/NOMBRE/NIF/CIUDAD/PROYECTOS/ACCIONES
- Panel detalle lateral (overlay) al seleccionar fila
- Formulario nuevo/editar en panel o página independiente

### Campos formulario
Cliente: NIF (required), Nombre (required), Dirección, Ciudad, Provincia, País, C.Postal, Contacto nombre/email/teléfono

### Estados
| Estado | Comportamiento |
|--------|---------------|
| Carga | Skeleton shimmer |
| Vacío | Empty state "No hay clientes. Crea el primero." + CTA |
| Error | Snackbar con mensaje de error |
| Guardando | Botón disabled con spinner |

---

## 5. Proyectos (RF-C02)

**Ruta:** `/projects` | **Archivo SVG:** `penpot-design-proyectos.svg`

### Layout
- Top bar: "🏗️ Proyectos" + breadcrumb "Inicio › Proyectos" + botón "+ Nuevo Proyecto"
- Barra de filtros: BUSCAR, CLIENTE (dropdown), ESTADO (dropdown), CECO (dropdown), Filtrar/Limpiar, badge total
- Tabla con cabecera dark `#1F4E78`: ID/PROYECTO/CLIENTE/ESTADO/CECO(s)/INTERLOCUTOR/ACCIONES
- Filas alternas: `#F0F7FF` + left accent para seleccionada
- Paginación: "Mostrando 1-N de X proyectos" + controles numéricos
- Panel detalle lateral derecho (overlay 450px):
  - Cabecera dark con nombre proyecto + ✕ cerrar
  - Campos: ID, Estado (badge), Cliente, CECO(s), Interlocutor, Departamento, Email, Teléfono
  - Usuarios asignados con avatares circulares (iniciales)
  - Acciones asociadas como pills
  - Botones: Editar, Duplicar, 🗑️ Eliminar

### Estados
| Estado | Comportamiento |
|--------|---------------|
| Carga | Skeleton rows |
| Vacío | Empty state |
| Detalle abierto | Panel lateral overlay |
| Formulario | Página dedicada `/projects/nuevo` o `/projects/:id/editar` |

---

## 6. Acciones (RF-C03)

**Ruta:** `/actions`

### Layout
- Similar a Proyectos: listado filtrable + detalle
- Sub-tabla de Conceptos asociados (con acciones Ver/Editar/Quitar/Duplicar)
- Funcionalidad "Añadir Concepto existente" y "Nuevo Concepto directo"

### Componentes
Tabla, chips de concepto, botones inline

---

## 7. Conceptos / Editor de Fórmula (RF-C04)

**Ruta:** `/concepts` | **Archivo SVG:** `penpot-design-conceptos.svg`

### Layout — split panel

**Panel izquierdo (480px):** Lista de conceptos
- Barra de búsqueda + filtro Tipo (Pago/Factura)
- Columnas: ID/CONCEPTO/TIPO/HASTA/ACCIONES
- Fila activa con acento azul izquierdo + bg `#E8F4F8`
- Conceptos: C143 "Nota de gastos pago" (Pago ∞), C78 "Nota de gastos facturación" (Factura ∞), etc.

**Panel derecho (600px):** Editor de Fórmula
- Cabecera dark: "🧮 Editor de Fórmula — C143 · Nota de gastos pago"
- Meta fields: Nombre, Tipo (Pago ▾), Desde/Hasta, Aplica a (multi-select)
- **Visual Formula Builder:**
  - Tokens coloreados: Variable `#1F4E78`, Operador círculo `white+blue border`, Número `#70AD47`, botón + `dashed`
  - **Paleta de Variables:** Σ Gasto Payhawk, N° Visitas Celero, Hrs Bizneo, Hrs Intratime, +Var
  - **Operaciones:** +, −, ×, ÷, %, Σ suma, N count, 123 num
- **Preview oscuro** (`#163A52`): "€ 1.250 — Suma de Gasto Payhawk" con metadata origen/fecha
- **Jerarquía de aplicación:** pills: Global (activo) | Proyecto | Acción | Empleado
- **Botones:** 💾 Guardar, Cancelar, 📋 Duplicar

### Comportamientos interactivos
- Drag/click de variables y operaciones al builder visual
- Preview se actualiza en tiempo real
- Jerarquía determina ámbito del concepto

---

## 8. Periodos (RF-C07)

**Ruta:** `/periods`

### Layout
- Listado de períodos con selector año/mes
- Columnas: AÑO/MES/ESTADO/FECHA CÁLCULO/FECHA CIERRE/ACCIONES
- Estados: Abierto (verde), Cerrado (azul), Bloqueado (gris)
- Acciones: "Recalcular" (lanza motor), "Cerrar", "Reabrir"

### Componentes
Tabla, badges de estado, botones de acción

---

## 9. Aprobaciones (RF-D01 a RF-D07)

**Ruta:** `/approvals` | **Archivo SVG:** `penpot-design-aprobaciones.svg`

### Layout
- Top bar: "✅ Aprobaciones"
- Barra de filtros: PERÍODO, CLIENTE, PROYECTO, ESTADO (dropdowns)
- **Sección "⏳ Pendientes de Aprobación"** (bg `#FFF8E1`):
  - Checkbox multi-select + botones "☑ Aprobar Seleccionados" / "✗ Rechazar"
  - Columnas: checkbox | PERÍODO | CLIENTE | PROYECTO | COSTE | FACTURACIÓN | MARGEN | ACCIONES
  - Botón "Aprobar" individual por fila (verde filled u outlined)
  - Total pendiente
- **Panel detalle izquierdo (680px):** "📋 Detalle — Amex Shop Small · Mayo 2026"
  - KPIs: Coste total, Facturación, Margen
  - Tabla desglose: CONCEPTO/EMPLEADO/IMPORTE PAGO/FACTURA
  - Campo de comentarios "✏️ Añadir comentario..."
- **Panel derecho (400px):** "✅ Registros Aprobados"
  - Histórico: Período, Proyecto, Aprobado por
  - Diagrama flujo: Cálculo ✓ → Revisión ✓ → FICO ⟳ → Dirección — → Cierre —

### Flujo de aprobación (5 pasos)
1. ProjectManager → 2. Backoffice → 3. FICO → 4. Dirección → 5. SystemExports
- Cada paso muestra OK (verde), pendiente (amarillo), inactive (gris)
- Aprobar avanza al siguiente paso, Rechazar retrocede

---

## 10. Contabilidad (RF-E02, RF-E03)

**Ruta:** N/A (pendiente de implementación completa)

### Layout
- Panel de exportación A3 Innuva y A3 ERP
- Histórico de exportaciones
- Validación previa al envío

---

## 11. Informes (Power BI)

**Ruta:** `/reports`

### Layout
- Integración vía iframe o embed Power BI
- Dimensiones: margen por proyecto, productividad, costes
- Selector de período y proyecto

---

## 12. Administración

### 12.1 Usuarios (RF-C05)

**Ruta:** `/users`

- Listado con filtros: rol, departamento, estado, búsqueda
- Columnas: NIF/NOMBRE/EMAIL/ROL(ES)/DEPARTAMENTO/ESTADO/ACCIONES
- Formulario: NIF, Nombre, Apellidos, Email, Contraseña, Rol(es) multi-select, Departamento(s) multi-select, Asignaciones
- Asignaciones: selector multi-nivel Cliente → Proyecto → Acción

### 12.2 CECOs

**Ruta:** `/cost-centers`

- CRUD simple: código, nombre, activo/inactivo

### 12.3 Departamentos

**Ruta:** `/departments`

- CRUD simple: nombre, activo/inactivo

### 12.4 Roles

**Ruta:** `/roles`

- Listado de roles fijos: Administrator, Direction, Fico, Backoffice, ProjectManager, Auditor, Reader

### 12.5 Auditoría (RF-F01, RF-F02)

**Ruta:** `/audit`

- Log con filtros: usuario, entidad, acción, fechas
- Columnas: FECHA/USUARIO/ENTIDAD/ID/ACCIÓN/CAMBIOS/IP
- inmutable (solo lectura)

---

## 13. Responsive

| Breakpoint | Ancho | Layout |
|------------|-------|--------|
| Mobile | < 600px | Sidenav oculto (hamburger), contenido a ancho completo |
| Tablet | 600-959px | Sidenav colapsable, KPI cards 2×2, tablas con scroll horizontal |
| Desktop | ≥ 960px | Sidenav fijo 260px, layout completo |

---

## 14. Navegación (flujo entre pantallas)

```
/login ──auth──> /dashboard
                    ├── /clients
                    ├── /projects
                    │    └── /projects/:id
                    ├── /actions
                    ├── /concepts
                    │    └── /concepts/:id/formula
                    ├── /periods
                    ├── /approvals
                    │    └── /approvals/pendientes
                    ├── /closures
                    │    └── /closures/:id
                    ├── /reports
                    ├── /users
                    ├── /cost-centers
                    ├── /departments
                    ├── /roles
                    └── /audit
```

- Redirect raíz: `/` → `/dashboard`
- Fallback: `**` → redirect to `/dashboard`
- Login: público (sin shell)
- Smoke test: `/_smoke` (público, solo dev)

---

## 15. Componentes compartidos (shared)

Ver `docs/COMPONENTES_SHARED.md` para detalle de cada componente reutilizable.

---

## 16. Accesibilidad aplicada por pantalla

| Pantalla | Consideraciones |
|----------|-----------------|
| Login | Labels visibles, focus order lógico, mensajes de error claros |
| Dashboard | `aria-label` en KPIs, tabla con `aria-label`, iconos decorativos con `aria-hidden` |
| Listados | Tablas con `aria-label`, paginación accesible, `aria-sort` en columnas |
| Formularios | `mat-error` para validaciones, `aria-describedby` para ayudas |
| Detalles | Regiones con `role="region"` y `aria-label` descriptivo |
| Editor fórmula | Drag & drop con alternativa de teclado, `aria-live` para preview |
