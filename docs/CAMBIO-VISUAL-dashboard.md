# Cambio visual: dashboard

Tipo: Actualización de diseño UI (sin nuevos RFs)
Archivo: `frontend/public/penpot-design-dashboard.svg`
Acción requerida: Designer y Frontend deben leer el SVG y actualizar el componente Angular correspondiente para que coincida con el nuevo diseño.

## RFs afectados (existentes, sin cambios estructurales)

- **RF-B01** — Dashboard KPIs período activo
- **RF-B02** — Dashboard avisos automáticos
- **RF-B03** — Dashboard "Mis proyectos" filtrado por ownership

## Componente Angular afectado

- `frontend/src/app/features/dashboard/` (ruta lazy-loaded bajo `/dashboard`).
- Incluye: `DashboardComponent` con KPIs cards, tabla "Mis Proyectos", panel de alertas.

## Endpoints relacionados (sin cambio)

- `GET /api/dashboard?periodId=` — KPIs globales
- `GET /api/dashboard/avisos` — alertas (cierres pendientes, periodos bloqueados, errores sync)
- `GET /api/dashboard/mis-proyectos?periodId=` — proyectos del usuario con estado de cierre

## Elementos UI identificados en el diseño

- **Header**: Selector de periodo (mat-select con los periodos disponibles) + botón "Recalcular"
- **Fila de KPIs** (mat-card grid):
  - Tarjeta: Cierres Completados (número grande + icono check)
  - Tarjeta: Pendientes Aprobación (número grande + icono warning)
  - Tarjeta: Facturación Total (importe €)
  - Tarjeta: Margen Promedio (porcentaje)
- **Panel "Mis Proyectos"** (tabla mat-table):
  - Columnas: Proyecto, Cliente, Coste Bruto, Facturación, Margen, Estado, Acción
  - Cada fila enlaza al detalle del cierre o al panel de aprobaciones
- **Panel de Alertas** (mat-card con lista):
  - Tipo: proyecto pendiente FICO, periodo bloqueado, error de sincronización
  - Color coding: warning (amarillo), danger (rojo), success (verde)

## Entidades/endpoints

Sin cambios.

## Notas para Designer

- KPIs en cards con icono representativo y color de acento.
- El selector de periodo debe ser prominente en la parte superior.
- Las alertas deben destacar visualmente (sin ser intrusivas).
- Paleta: KPIs numbers en primary `#1F4E78`, alertas warning `#FFC107`, danger `#D32F2F`.

## Notas para Frontend

- Aplicar `data-testid="dashboard-kpis"`, `data-testid="dashboard-alertas"`, `data-testid="dashboard-mis-proyectos"`.
- Aplicar `data-testid="period-selector"`, `data-testid="btn-recalcular"`.
- El dashboard se carga al entrar; el selector de periodo dispara recarga de KPIs y Mis Proyectos.
- Validar accesibilidad WCAG 2.1 AA en todos los elementos interactivos.
