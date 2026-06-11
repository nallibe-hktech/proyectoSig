# Cambio visual: aprobaciones

Tipo: Actualización de diseño UI (sin nuevos RFs)
Archivo: `frontend/public/penpot-design-aprobaciones.svg`
Acción requerida: Designer y Frontend deben leer el SVG y actualizar el componente Angular correspondiente para que coincida con el nuevo diseño.

## RFs afectados (existentes, sin cambios estructurales)

- **RF-D02** — Panel aprobaciones con filtros
- **RF-D03** — "Pendientes" para el usuario
- **RF-D04** — Detalle aprobación con KPIs
- **RF-D05** — Aprobar cierre
- **RF-D06** — Rechazar con motivo
- **RF-D07** — Detalle cálculo línea

## Componente Angular afectado

- `frontend/src/app/features/approvals/` (ruta lazy-loaded bajo `/aprobaciones`).
- Incluye: `ApprovalsPendingComponent` (pendientes con multi-check), `ApprovalsHistoryComponent` (histórico), `ApprovalDetailComponent` (detalle por proyecto).

## Endpoints relacionados (sin cambio)

- `GET /api/approvals` — panel con filtros (`?periodoId=&clienteId=&proyectoId=&estado=`)
- `GET /api/approvals/pendientes` — pendientes para el usuario según su rol
- `GET /api/approvals/historial/{closureId}` — historial de aprobaciones de un cierre
- `GET /api/closures/{id}` — detalle del cierre (cabecera + líneas + approvals)
- `POST /api/closures/{id}/aprobar` — aprobar (con If-Match rowVersion)
- `POST /api/closures/{id}/rechazar` — rechazar con motivo (body: `{ comentario }`)

## Elementos UI identificados en el diseño

- **Pestañas**: Pendientes | Aprobados
- **Filtros**: selector de Período, Cliente, Proyecto (encadenados: proyecto se filtra por cliente)
- **Tabla Pendientes** con multi-check:
  - Columnas: Período, Cliente, Proyecto, Coste Total, Facturación, Margen, Estado
  - Checkbox en cabecera: "Seleccionar todos"
  - Botón flotante: "Aprobar seleccionados" (habilitado solo si hay checks)
- **Tabla Aprobados** (histórico):
  - Columnas: Período, Cliente, Proyecto, Coste, Facturación, Aprobado por, Fecha
- **Detalle de aprobación por proyecto** (al hacer clic en una fila):
  - Cabecera: Coste total, Facturación total, Margen (cards)
  - Desglose por concepto/empleado/importe (tabla de líneas de cierre)
  - Botones: [Aprobar] [Rechazar] (según rol y paso actual)
  - Campo de texto para comentario (obligatorio en rechazo)
  - Historial de transiciones del flujo (timeline visual)
  - Botón "Ver detalle de cálculo" por cada línea (enlaza a CalculationLog)

## Entidades/endpoints

Sin cambios.

## Notas para Designer

- El multi-check en pendientes debe tener "Seleccionar todo" en cabecera.
- El detalle debe mostrar el flujo de aprobación como timeline vertical (paso actual resaltado).
- Colores: Aprobar `#70AD47` (success), Rechazar `#D32F2F` (danger), Pendiente `#FFC107` (warning).
- Margen negativo debe mostrarse en rojo.

## Notas para Frontend

- Aplicar `data-testid="approvals-pending-table"`, `data-testid="approval-check-{closureId}"`.
- Aplicar `data-testid="approval-detail"`, `data-testid="btn-aprobar"`, `data-testid="btn-rechazar"`.
- Al aprobar, optimistic UI con rollback si el backend responde 412 (concurrency conflict).
- Validar accesibilidad WCAG 2.1 AA.
