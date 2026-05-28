# Sistema de Diseño — SIG · Plataforma de Cierres

> Basado en diseños Penpot (fuente de verdad visual) y Material 3 (Angular Material 21).
> Versión: 1.0 | Fecha: Mayo 2026

---

## 1. Paleta de colores (extraída de Penpot)

### 1.1 Colores corporativos SIG

| Token | HEX | Uso |
|-------|-----|-----|
| `--sig-primary` | `#1F4E78` | Azul corporativo, botones primarios, barras, headers |
| `--sig-primary-light` | `#2E5C8A` | Hover, variante más clara |
| `--sig-primary-dark` | `#163A52` | Sidebar bottom, gradientes, fondos oscuros |
| `--sig-success` | `#70AD47` | Verde aprobación, badges OK, tendencias positivas |
| `--sig-success-dark` | `#5A9438` | Hover sobre success |
| `--sig-warning` | `#FFC107` | Amarillo alerta, badges pendiente suave |
| `--sig-warning-dark` | `#E65100` | Texto warning sobre fondo claro |
| `--sig-danger` | `#D32F2F` | Rojo rechazo/error, badges rechazado |
| `--sig-danger-dark` | `#C62828` | Texto danger sobre fondo claro |
| `--sig-light` | `#E8F4F8` | Fondo tablas, acentos suaves |
| `--sig-lighter` | `#F5F5F5` | Fondos alternativos filas |
| `--sig-surface` | `#F0F4F8` | Fondo página principal |
| `--sig-white` | `#FFFFFF` | Tarjetas, paneles, inputs |
| `--sig-border` | `#D0D0D0` | Bordes de inputs, tablas, separadores |
| `--sig-text-dark` | `#1A1A1A` | Texto principal |
| `--sig-text-muted` | `#666666` | Texto secundario, etiquetas |
| `--sig-text-light` | `#888888` | Texto terciario, placeholders |
| `--sig-bg-sidebar` | Gradiente `#1F4E78` → `#163A52` | Fondo sidebar lateral |

### 1.2 Mapeo a tokens M3 (`--mat-sys-*`)

| Token M3 | Valor | Equivalente SIG |
|----------|-------|-----------------|
| `--mat-sys-primary` | `#1F4E78` | `--sig-primary` |
| `--mat-sys-on-primary` | `#FFFFFF` | — |
| `--mat-sys-primary-container` | `#D6DFF3` | — |
| `--mat-sys-on-primary-container` | `#0D1B30` | — |
| `--mat-sys-secondary` | `#2E5C8A` | `--sig-primary-light` |
| `--mat-sys-on-secondary` | `#FFFFFF` | — |
| `--mat-sys-secondary-container` | `#DBEAFE` | — |
| `--mat-sys-on-secondary-container` | `#1E3A5F` | — |
| `--mat-sys-error` | `#D32F2F` | `--sig-danger` |
| `--mat-sys-on-error` | `#FFFFFF` | — |
| `--mat-sys-error-container` | `#FFDAD6` | — |
| `--mat-sys-on-error-container` | `#410002` | — |
| `--mat-sys-surface` | `#F0F4F8` | `--sig-surface` |
| `--mat-sys-on-surface` | `#1A1A1A` | `--sig-text-dark` |
| `--mat-sys-surface-variant` | `#E8EDF5` | — |
| `--mat-sys-on-surface-variant` | `#44474F` | — |
| `--mat-sys-outline` | `#74777F` | — |
| `--mat-sys-outline-variant` | `#C4C7CF` | — |

### 1.3 Colores semánticos adicionales (propios SIG)

| Token | HEX | Uso |
|-------|-----|-----|
| `--sig-fico-approve` | `#70AD47` | Badge paso FICO aprobado |
| `--sig-fico-pending` | `#FFC107` | Badge paso FICO pendiente |
| `--sig-direction-pending` | `#D0D0D0` | Badge paso Dirección pendiente |
| `--sig-system-pending` | `#D0D0D0` | Badge paso Cierre pendiente |
| `--sig-calc-bg` | `#163A52` | Fondo preview cálculo |

---

## 2. Tipografía

### 2.1 Familias

| Uso | Fuente | Fallback |
|-----|--------|----------|
| UI general | `'Segoe UI'` | `system-ui, sans-serif` |
| Etiquetas UX/App | `'Inter'` (Google Fonts) | `sans-serif` |
| Campos numéricos | `'Roboto Mono'` | `monospace` |

> Los SVGs de Penpot usan `'Segoe UI'` como fuente principal. Inter se usa como la variante web cargada vía Google Fonts (compatible con diseño corporativo).

### 2.2 Jerarquía

| Elemento | Tamaño | Peso | Line-height | Uso |
|----------|--------|------|-------------|-----|
| H1 | 36px | 800 | 1.2 | Branding login |
| H2 | 28px | 700 | 1.2 | Título de página |
| H3 | 24px | 700 | 1.3 | Título sección |
| H4 | 20px | 700 | 1.3 | Subtítulo |
| H5 | 16px | 600 | 1.4 | Encabezado tarjeta |
| Body | 14px | 400 | 1.5 | Texto general |
| Body small | 13px | 400 | 1.5 | Texto tabla, detalle |
| Caption | 12px | 400 | 1.4 | Etiquetas, metadatos |
| Label | 11px | 700 | 1.3 | Etiquetas de campo |
| KPI value | 36px | 700 | 1 | Valores KPI en dashboard |
| Overline | 10px | 700 | 1.2 | Secciones de nav, etiquetas técnica |

### 2.3 Pesos disponibles

400 (Regular), 500 (Medium), 600 (SemiBold), 700 (Bold), 800 (ExtraBold)

---

## 3. Espaciado (escala base 4px)

| Token | px | Uso común |
|-------|----|-----------|
| `--space-1` | 4px | Gap entre iconos y texto |
| `--space-2` | 8px | Padding interno badges |
| `--space-3` | 12px | Gap entre elementos relacionados |
| `--space-4` | 16px | Padding tarjetas, gap secciones |
| `--space-5` | 20px | Padding inputs |
| `--space-6` | 24px | Padding página, gap entre cards |
| `--space-8` | 32px | Secciones mayores |
| `--space-10` | 40px | Separación entre bloques |
| `--space-12` | 48px | Espaciado hero/login |
| `--space-16` | 64px | Sidenav colapsado, padding grande |
| `--space-64` | 256px | Sidenav expandido |

---

## 4. Radios, sombras y bordes

### 4.1 Border radius

| Contexto | Valor | Ejemplo |
|----------|-------|---------|
| Cards (KPI, paneles) | 12px | `border-radius: 12px` |
| Botones | 6-12px | Según variante |
| Inputs | 10px | `border-radius: 10px` |
| Badges | 10-12px | `border-radius: 10px` / `12px` |
| Tabla cabecera | 12px top | Solo esquinas superiores |
| Sidebar nav items | 6px | `border-radius: 6px` |
| Paneles de detalle | 12px | `border-radius: 12px` |
| Modales/chips | 6px | `border-radius: 6px` |

### 4.2 Sombras

| Nivel | Filter | Uso |
|-------|--------|-----|
| Card estándar | `drop-shadow(0 2px 4px rgba(31,78,120,0.10))` | Tarjetas menores |
| Card elevada | `drop-shadow(0 4px 8px rgba(31,78,120,0.15))` | KPI cards, paneles principales |
| Card login | `drop-shadow(0 20px 40px rgba(0,0,0,0.35))` | Tarjeta de login |
| Input focus | `drop-shadow(0 0 4px rgba(31,78,120,0.30))` | Input en foco |

### 4.3 Bordes

| Contexto | Grosor | Color |
|----------|--------|-------|
| Input default | 1.5px | `#D0D0D0` |
| Input focus | 2px | `#1F4E78` |
| Tabla separador filas | 1px | `#F0F4F8` |
| Card outline | 1px | `#E0E8F0` |
| Botón outlined | 1px | `#1F4E78` |
| Accento izquierdo tarjeta | 4-5px | Según semántica |

---

## 5. Iconografía

### 5.1 Set

| Propiedad | Valor |
|-----------|-------|
| Fuente | `Material Symbols Outlined` |
| Configuración Angular | `MAT_ICON_DEFAULT_OPTIONS` con `fontSet: 'material-symbols-outlined'` |
| Tamaño mínimo | 20px |
| Tamaño en nav | 24px |
| Tamaño en botones | 18-20px |
| Tamaño en KPIs | 28px |
| Peso óptico (opsz) | 20-48 |

### 5.2 Glifos usados por módulo

| Módulo | Icono | Glifo |
|--------|-------|-------|
| Dashboard | dashboard | `dashboard` |
| Clientes | groups | `groups` |
| Proyectos | folder_open | `folder_open` |
| Acciones | task_alt | `task_alt` |
| Conceptos | calculate | `calculate` |
| Periodos | calendar_month | `calendar_month` |
| Aprobaciones | approval | `approval` |
| Contabilidad | account_balance | `account_balance` |
| Informes | bar_chart | `bar_chart` |
| Usuarios | manage_accounts | `manage_accounts` |
| CECOs | account_balance | `account_balance` |
| Auditoría | history | `history` |
| Login | — | Logo SIG |
| Notificaciones | notifications | `notifications` |
| Buscar | search | `search` |
| Recargar | refresh | `refresh` |
| Editar | edit | `edit` |
| Eliminar | delete | `delete` |
| Cerrar sesión | logout | `logout` |

---

## 6. Componentes base

### 6.1 Botones

| Variante | Background | Texto | Borde | Hover |
|----------|-----------|-------|-------|-------|
| Primary (filled) | `#1F4E78` gradient | White | — | `#2E5C8A` |
| Secondary (outlined) | Transparent | `#1F4E78` | `1px #1F4E78` | `bg #F0F4F8` |
| Text | Transparent | `#1F4E78` | — | `bg #F0F4F8` |
| Success (filled) | `#70AD47` | White | — | `#5A9438` |
| Warn (filled) | White | `#D32F2F` | `1.5px #D32F2F` | `bg #FFEBEE` |
| Icon | Transparent | `#1F4E78` | — | `bg #F0F4F8` |
| Danger text | Transparent | `#D32F2F` | — | `bg #FFEBEE` |

Estados: default, hover, active (press), disabled (opacity 0.38), focus (outline 3px primary).

### 6.2 Inputs

| Propiedad | Valor |
|-----------|-------|
| Apariencia | Outline (config vía `MAT_FORM_FIELD_DEFAULT_OPTIONS`) |
| Border radius | 10px |
| Border default | 1.5px `#D0D0D0` |
| Border focus | 2px `#1F4E78` + `box-shadow` azul |
| Label flotante | `#666666` |
| Error | `#D32F2F` + mensaje debajo |
| Height | 48px (estándar) |
| Disabled | Opacidad reducida |

### 6.3 Badges / Chips de estado

| Estado | Background | Texto |
|--------|-----------|-------|
| Activo | `#E8F5E9` | `#2E7D32` |
| Inactivo | `#FFEBEE` | `#C62828` |
| Revisión | `#FFF3E0` | `#E65100` |
| Pago | `#E3F2FD` | `#1565C0` |
| Factura | `#E8F5E9` | `#2E7D32` |
| Pendiente PM | Warning container | Warning |
| Pendiente Backoffice | Warning container | Warning |
| Pendiente FICO | Secondary container | Secondary |
| Pendiente Dirección | Primary container | Primary |
| Aprobado | Success container | Success |
| Rechazado | Error container | Error |
| Cerrado | Surface variant | Surface variant |

### 6.4 Cards

| Propiedad | Valor |
|-----------|-------|
| Background | `#FFFFFF` |
| Border radius | 12px |
| Shadow | `0 4px 8px rgba(31,78,120,0.15)` |
| Padding | 16px (content) |
| Accent bar | 4-5px left border según semántica |

### 6.5 Tablas

| Propiedad | Valor |
|-----------|-------|
| Cabecera | `#1F4E78` dark (en tablas de listado) o `#E8F4F8` light |
| Texto cabecera | White (dark), `#1F4E78` (light) |
| Filas alternas | White + `#FAFAFA` o `#F0F7FF` |
| Fila hover | `#F0F4F8` |
| Fila activa | `#E8F4F8` + left accent 3-4px |
| Separador filas | 1px `#F0F4F8` o `#E8F0F8` |
| Altura fila | 44-52px |
| Altura cabecera | 34-40px |

### 6.6 Modales / Diálogos

- Diálogo de confirmación estándar de Angular Material
- Overlay semi-transparente
- Botón primario (confirmar) y botón secondary/outlined (cancelar)
- `data-testid="confirm-dialog"`

### 6.7 Tooltips

- Estándar Angular Material (`MatTooltip`)
- Retardo: 500ms
- Color: Primary dark `#163A52`

### 6.8 Estado vacío (Empty State)

- Icono grande + texto descriptivo + CTA opcional
- `data-testid="empty-state"`
- Usado en listas sin datos, búsquedas sin resultados

---

## 7. Layout global

### 7.1 Estructura

```
┌─────────────────────────────────────────────────────┐
│ Sidebar (260px)   │   Top Bar (60px)                │
│ Gradiente dark     │   Título · Período · Acciones  │
│                    ├─────────────────────────────────┤
│ Logo SIG          │   Content (router-outlet)       │
│                    │   Padding: 24px                 │
│ Nav PRINCIPAL:    │                                 │
│  • Dashboard      │   ┌───┬───┬───┬───┐            │
│  • Clientes       │   │ KPI│KPI │KPI│KPI│           │
│  • Proyectos      │   └───┴───┴───┴───┘            │
│  • Acciones       │                                 │
│  • Conceptos      │   ┌─── Alertas ─────────┐       │
│  • Periodos       │   │ ⚠ 3 pendientes FICO │       │
│  • Aprobaciones   │   └──────────────────────┘       │
│  • Contabilidad   │                                 │
│  • Informes       │   ┌─── Proyectos Activos ──┐     │
│                    │   │ ID │ Nombre │ Estado... │    │
│ ADMINISTRACIÓN:    │   └────────────────────────┘     │
│  • Usuarios       │                                 │
│  • CECOs          │   ┌─── Integraciones ────────┐   │
│  • Auditoría      │   │ ● Celero ● Bizneo ...    │   │
│                    │   └──────────────────────────┘   │
│ Perfil usuario    │   Footer                         │
└─────────────────────────────────────────────────────┘
```

### 7.2 Sidebar

| Propiedad | Valor |
|-----------|-------|
| Ancho expandido | 260px |
| Ancho colapsado | — |
| Fondo | Gradiente `#1F4E78` → `#163A52` |
| Item activo | `rgba(255,255,255,0.18)` + left accent 3px `#70AD47` |
| Item normal | `rgba(255,255,255,0.7)` |
| Logo | Círculo `#70AD47` + texto "SIG" + subtítulo |
| Perfil usuario | Barra inferior `rgba(255,255,255,0.06)` |
| Separador secciones | Línea `rgba(255,255,255,0.1)` |

### 7.3 Top bar

| Propiedad | Valor |
|-----------|-------|
| Background | `#FFFFFF` |
| Altura | 60px |
| Shadow | `0 2px 4px rgba(31,78,120,0.12)` |
| Título | 20px, `#1F4E78`, Bold |
| Selector período | Dropdown `#F0F4F8`, border `#D0D0D0` |
| Botón recalcular | Filled `#1F4E78` gradient |
| Badge notificaciones | `#D32F2F` |

### 7.4 Footer

| Propiedad | Valor |
|-----------|-------|
| Background | `#F8FAFC` |
| Altura | 40px |
| Texto | 11px, `#AAAAAA` |

---

## 8. Responsive breakpoints

| Breakpoint | Ancho | Comportamiento |
|------------|-------|----------------|
| Mobile | < 600px | Sidenav oculto (overlay), padding reducido |
| Tablet | 600-959px | Sidenav colapsable, layout adaptativo |
| Desktop | ≥ 960px | Sidenav fijo, layout completo |

---

## 9. Accesibilidad (WCAG 2.1 AA)

| Requisito | Implementación |
|-----------|----------------|
| Contraste color | Ratio mínimo 4.5:1 para texto normal, 3:1 para texto grande |
| Focus visible | `outline: 3px solid #1F4E78; outline-offset: 2px` |
| ARIA labels | `aria-label` en iconos, `aria-hidden="true"` en decorativos |
| Roles | `role` semántico en regiones (banner, navigation, main) |
| Landmarks | `<nav>` para sidebar, `<main>` para contenido |
| data-testid | Ver §10 |

---

## 10. Convención `data-testid`

| Tipo elemento | Formato | Ejemplo |
|---------------|---------|---------|
| Nav item | `nav-{entidad}` | `nav-dashboard`, `nav-projects` |
| Botón | `btn-{entidad}-{accion}` | `btn-cliente-guardar`, `btn-proyecto-eliminar` |
| Input | `input-{entidad}-{campo}` | `input-cliente-nombre`, `input-proyecto-estado` |
| Tabla | `tbl-{entidad}` | `tbl-proyectos`, `tbl-conceptos` |
| Fila tabla | `row-{entidad}-{id}` | `row-proyecto-32` |
| Card KPI | `kpi-{nombre}` | `kpi-cierres-completados` |
| Empty state | `empty-{entidad}` | `empty-proyectos` |
| Modal/Badge | `{tipo}-{entidad}` | `confirm-dialog`, `badge-estado` |
| Smoke test | `smoke-{elemento}-{nombre}` | `smoke-card`, `smoke-btn-primary` |

---

## 11. Estados semánticos en UI

| Estado | Color | Background | Icono |
|--------|-------|-----------|-------|
| Success | `#70AD47` | `#E8F5E9` | `check_circle` |
| Warning | `#FFC107` | `#FFF3E0` / `#FFF8E1` | `warning` |
| Error | `#D32F2F` | `#FFEBEE` | `error` |
| Info | `#1F4E78` | `#E8F4F8` | `info` |
| Neutral | `#888888` | `#F8FAFC` | — |
