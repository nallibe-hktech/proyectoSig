# Sistema de Diseño SIG · Plataforma de Cierres

> Basado en diseños Penpot (fuente de verdad visual) y Material 3 (Angular Material 21).
> Fecha: Junio 2026 | Versión: 1.0

---

## 1. Paleta de colores

### 1.1 Tokens primarios M3 (CSS Custom Properties)

```css
--mat-sys-primary:                  #1F4E78;   /* Azul corporativo navy */
--mat-sys-on-primary:               #FFFFFF;
--mat-sys-primary-container:        #D6DFF3;
--mat-sys-on-primary-container:     #0D1B30;

--mat-sys-secondary:                #2E5C8A;   /* Azul claro */
--mat-sys-on-secondary:             #FFFFFF;
--mat-sys-secondary-container:      #DBEAFE;
--mat-sys-on-secondary-container:   #1E3A5F;

--mat-sys-tertiary:                 #C9A961;   /* Dorado */
--mat-sys-on-tertiary:              #3B2800;
--mat-sys-tertiary-container:       #F5E8C2;
--mat-sys-on-tertiary-container:    #4A3500;

--mat-sys-error:                    #D32F2F;   /* Rojo */
--mat-sys-on-error:                 #FFFFFF;
--mat-sys-error-container:          #FFDAD6;
--mat-sys-on-error-container:       #410002;

--mat-sys-surface:                  #F0F4F8;   /* Fondo página gris claro */
--mat-sys-on-surface:               #1A1A1A;
--mat-sys-surface-variant:          #E8EDF5;
--mat-sys-on-surface-variant:       #44474F;
--mat-sys-outline:                  #74777F;
--mat-sys-outline-variant:          #C4C7CF;
```

### 1.2 Tokens semánticos propietarios SIG

```css
--sig-success:                      #70AD47;   /* Verde aprobación */
--sig-success-dark:                 #5A9438;
--sig-success-container:            #E8F5E9;
--sig-on-success:                   #FFFFFF;
--sig-on-success-container:         #2E7D32;

--sig-warning:                      #FFC107;   /* Ámbar alerta */
--sig-warning-dark:                 #E65100;
--sig-warning-container:            #FFF3E0;
--sig-on-warning:                   #FFFFFF;
--sig-on-warning-container:         #E65100;

--sig-danger:                       #D32F2F;   /* Rojo rechazo/error */
--sig-danger-dark:                  #C62828;
--sig-danger-container:             #FFEBEE;
--sig-on-danger:                    #FFFFFF;
--sig-on-danger-container:          #C62828;

--sig-info:                         #1F4E78;   /* Info = primary */
--sig-info-container:               #E8F4F8;
```

### 1.3 Colores adicionales Penpot

```css
--sig-primary-light:                #2E5C8A;
--sig-primary-dark:                 #163A52;
--sig-light:                        #E8F4F8;   /* Fondos claros */
--sig-lighter:                      #F5F5F5;   /* Fondos alternativos */
--sig-border:                       #D0D0D0;   /* Bordes */
--sig-text-muted:                   #666666;
--sig-text-light:                   #888888;
```

---

## 2. Tipografía

| Nivel | Familia | Tamaño | Peso | Altura línea | Uso |
|-------|---------|--------|------|-------------|-----|
| H1 | Inter | 32px | 700 | 1.2 | Títulos de página (`.sig-page__title`) |
| H2 | Inter | 28px | 700 | 1.2 | Subtítulos de sección |
| H3 | Inter | 24px | 600 | 1.3 | Títulos de card |
| H4 | Inter | 20px | 600 | 1.3 | Subtítulos de card |
| Body | Inter | 16px | 400 | 1.5 | Texto general |
| Body small | Inter | 14px | 400 | 1.5 | Celdas de tabla |
| Caption | Inter | 13px | 400 | 1.4 | Metadatos, breadcrumbs, labels |
| Label | Inter | 12px | 500 | 1.3 | Badges, botones pequeños |
| Mono | Roboto Mono | 14px | 400 | 1.4 | Importes numéricos (`.mono-num`) |
| AppBar logo | Inter | 20px | 700 | 1 | Logo SIG en navbar |
| KPI value | Roboto Mono | 40px | 600 | 1 | Valores de tarjetas KPI |

**Carga:** Google Fonts vía `<link>` en `index.html`.

---

## 3. Escala de espaciado (base 4px)

```css
--space-1:   4px;    /* xx-small */
--space-2:   8px;    /* x-small */
--space-3:  12px;    /* small */
--space-4:  16px;    /* base */
--space-5:  20px;    /* medium+ */
--space-6:  24px;    /* medium */
--space-8:  32px;    /* large */
--space-10: 40px;    /* x-large */
--space-12: 48px;    /* xx-large */
--space-16: 64px;    /* section */
--space-64: 256px;   /* sidenav width */
```

---

## 4. Radios, bordes y sombras

| Elemento | Radio | Borde | Sombra |
|----------|-------|-------|--------|
| Cards (mat-card) | 12px | none | M3 elevación 1 |
| Botones (flat) | 20px | none | M3 elevación |
| Botones (stroked) | 20px | 1px outline | none |
| Inputs (outline) | 4px | M3 field | none |
| Badges | 12px | none | none |
| Sidenav items | 8px | none | none |
| Diálogos | 16px | none | M3 elevation 3 |
| AppBar | 0 | none | 0 2px 8px rgba(0,0,0,0.2) |

---

## 5. Iconografía

- **Set:** Material Symbols Outlined (variable font)
- **Configuración:** `MAT_ICON_DEFAULT_OPTIONS` con `fontSet: 'material-symbols-outlined'`
- **Tamaño mínimo:** 18px en contexto denso, 24px estándar
- **Glifos usados en navegación:** `dashboard`, `groups`, `folder_open`, `task_alt`, `calculate`, `data_object`, `calendar_month`, `approval`, `lock_clock`, `bar_chart`, `account_balance`, `corporate_fare`, `verified_user`, `manage_accounts`, `history`, `refresh`, `location_on`, `menu`, `menu_open`, `logout`, `person`, `account_circle`
- **Glifos de acción:** `add`, `edit`, `delete`, `search`, `filter_list`, `more_vert`, `chevron_right`, `trending_up`, `trending_down`, `check_circle`, `warning`, `info`, `close`, `download`, `upload`, `sync`, `undo`

---

## 6. Componentes base M3

### 6.1 Botones

| Variante | Selector | Uso | Color |
|----------|----------|-----|-------|
| Flat | `mat-flat-button` | Acción principal | `color="primary"` o `color="warn"` |
| Stroked | `mat-stroked-button` | Acción secundaria | `color="primary"` |
| Text | `mat-button` | Acción terciaria | - |
| Icon | `mat-icon-button` | Acción compacta | - |
| FAB | `mat-fab` | Acción flotante | Solo en listados/detalle |

### 6.2 Inputs (Form Fields)

| Configuración | Valor |
|---------------|-------|
| Apariencia | `outline` (global vía `MAT_FORM_FIELD_DEFAULT_OPTIONS`) |
| Espaciado | Compacto (sin `subscriptSizing`) |
| Iconos | `matSuffix` o `matPrefix` con Material Symbols |

### 6.3 Tablas (mat-table)

- Clase `.sig-table` para estilos base
- Clase `.sig-table-dark-header` para cabecera primaria oscura
- Header sticky con `z-index: 10`
- Hover row con `--mat-sys-surface-variant`
- Acciones alineadas a la derecha con `.sig-table-actions`

### 6.4 Cards (mat-card)

- Header: `mat-card-title` + `mat-card-subtitle`
- Content: padding estándar M3
- Actions: alineación `end`
- Variante KPI: `.sig-kpi-card` con label uppercase + value grande

### 6.5 Badges de estado (`.sig-badge`)

| Modificador | Uso | Color |
|-------------|-----|-------|
| `--pending-pm` | Pendiente gestor proyecto | bg warning container |
| `--pending-backoffice` | Pendiente Backoffice | bg warning container |
| `--pending-fico` | Pendiente FICO | bg secondary container |
| `--pending-direction` | Pendiente Dirección | bg primary container |
| `--approved` | Aprobado | bg success container |
| `--rejected` | Rechazado | bg error container |
| `--closed` | Cerrado | bg surface variant |

---

## 7. Accesibilidad (WCAG 2.1 AA)

- Contraste mínimo 4.5:1 para texto normal, 3:1 para texto grande
- `:focus-visible` con outline 3px sólido `--mat-sys-primary`
- `aria-label` en todos los elementos interactivos sin texto visible
- `data-testid` convención: `<entidad>-<acción>` para E2E
- Roles semánticos ARIA en shell: `role="banner"` en AppBar, `aria-label` en navegación
- Color no es el único indicador de estado (texto + icono + badge)
---

## 8. Layouts

- **Shell**: `mat-sidenav-container` 100vh, sidebar 256px, AppBar sticky 64px
- **Page**: `.sig-page` con padding 24px, header flexbox con título + acciones
- **Responsive**: breakpoint 600px para versión mobile (sidebar overlay, padding reducido)
