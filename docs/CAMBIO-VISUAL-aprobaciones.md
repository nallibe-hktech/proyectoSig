# Cambio visual: aprobaciones

Tipo: Actualización de diseño UI (sin nuevos RFs)
Archivo: `penpot-design-aprobaciones.svg`
Acción requerida: Designer y Frontend deben leer el SVG y actualizar
el componente Angular correspondiente para que coincida con el nuevo diseño.

RFs afectados: RF-D02 (panel aprobaciones), RF-D03 (pendientes), RF-D04 (detalle), RF-D05 (aprobar), RF-D06 (rechazar)
Entidades/endpoints: sin cambios.

## Detalles del diseño

- Sidebar con Aprobaciones activo (indicador verde)
- Top bar: "✅ Aprobaciones"
- Barra de filtros: PERÍODO, CLIENTE, PROYECTO, ESTADO (dropdowns)
- Sección "⏳ Pendientes de Aprobación" (bg #FFF8E1):
  - Checkbox multi-select column + botones "☑ Aprobar Seleccionados" / "✗ Rechazar"
  - Columnas: checkbox | PERÍODO | CLIENTE | PROYECTO | COSTE | FACTURACIÓN | MARGEN | ACCIONES
  - Filas: Amex Shop Small (checked), Granini GPVs, Amex New — con botón individual "Aprobar" por fila
  - Total pendiente €28.700 / €40.000
- Panel detalle izquierdo (680px): "📋 Detalle — Amex Shop Small · Mayo 2026"
  - KPIs: Coste total €15.000, Facturación €20.500, Margen 32%
  - Desglose de conceptos: tabla CONCEPTO/EMPLEADO/IMPORTE PAGO/FACTURA
  - Líneas: Nota de gastos pago (€5.000/€6.500), Pago por visita Juan Pérez (€1.250/€1.625), Pago por visita Marta López (€980/€1.274), Nota de gastos facturación (€420/€546)
  - Campo de comentarios: "✏️ Añadir comentario antes de aprobar..."
- Panel derecho (400px): "✅ Registros Aprobados"
  - Histórico: Abril 2026 Amex (María G. FICO), Granini (Carlos L. FICO), Marzo 2026...
  - Diagrama flujo aprobación: Cálculo ✓ → Revisión ✓ → FICO ⟳ → Dirección — → Cierre —
- Botones inferiores: "✅ Aprobar Seleccionados (1)" (verde), "✗ Rechazar" (rojo borde), "Cancelar" (gris)
