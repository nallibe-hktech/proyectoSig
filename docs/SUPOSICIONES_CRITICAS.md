# SUPOSICIONES CRÍTICAS — SIG-es

Decisiones autónomas tomadas ante ambigüedad no bloqueante. Cada una se puede revertir si el cliente lo corrige. Ver diseño en `docs/ARQUITECTURA.md §15`.

## Ola 2 (2026-06-17)

### SUP-01 · Cecos: "el ceco en sí, no el número" (#3b)
**Ambigüedad:** el cliente pidió mostrar "el ceco en sí, no el número". `CostCenter` tiene `Codigo` (numérico/alfanumérico) y `Nombre` (descriptivo); hoy la UI muestra `"Codigo - Nombre"`.
**Decisión:** mostrar `CostCenter.Nombre` como etiqueta principal del ceco en las vistas de cálculo/cierre, en lugar del `Codigo` numérico.
**Reversible:** sí, es solo presentación. Confirmar con cliente si prefiere `Codigo`, `Nombre` o ambos.

### SUP-02 · Periodos: fechas de pago 30/15/9 (#9) — CONFIRMADO por cliente 2026-06-17
**Interpretación confirmada:** 30/15/9 son el **día del mes** de pago. Cada periodo tiene asignado un día de pago entre esos valores.
**Decisión:** campo `DiaPago` en `Period` (valores permitidos 30, 15, 9). Validación que restrinja a esos tres valores.
**Reversible:** sí.

### SUP-03 · Contratos de un día: criterio de detección (#2)
**Ambigüedad:** "contrato de un día" no estaba formalmente definido.
**Decisión:** se considera contrato de un día todo `StagingA3InnuvaContrato` con `FechaInicio == FechaFin`. La exclusión es **manual** (el usuario marca "a ignorar" + motivo); la detección automática solo lo **señala**, no lo ignora por sí sola.
**Reversible:** sí.

### SUP-04 · Incentivos manuales: mecanismo (#3a)
**Ambigüedad:** "añadir incentivos manualmente / importe personalizado" sin especificar dónde se persiste.
**Decisión:** reutilizar el scaffolding existente (`OverrideExceptionDialog` en frontend, entidad `PresupuestoServicio`) y persistir el importe manual como una línea de cierre no derivada de fórmula, con auditoría (motivo). Se concreta en la Ola 2.
**Reversible:** sí.

## Aparcados (sin decisión, requieren input del cliente)

### PARK-01 · Panel de facturas pagadas/pendientes por cliente (#5)
No existe entidad Factura/Pago ni estado de pago en el modelo. El cliente no supo definir de dónde sale el estado "pagada/pendiente". **Omitido de momento**; retomar cuando se defina el origen (entidad manual, derivado del cierre de facturación, o integración externa de solo lectura).
