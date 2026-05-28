# Informe de Entrega — Cambio Visual (Frontend)

**Fecha**: 27/05/2026  
**Estado**: ✅ Build exitoso  
**Tamaño inicial**: 504.45 kB (budget: 500 kB — margen +4.45 kB)

---

## Componentes Modificados

### 1. Login (`login.component.ts`)
- Fondo degradado oscuro `#1F4E78 → #163A52 → #0D2A3E`
- Izquierda: branding "h&k consulting" con claim "Strategic Solutions"
- Derecha: card 500px con logo circular SIG + "ACCEDER AL SISTEMA" gradient button + Azure AD
- Footer: "powered by SIG ©2025"

### 2. Dashboard (`dashboard.component.ts`)
- Topbar: period badge + recalcular + notificaciones con badge rojo
- KPIs: accent left border + trend indicators (↑↓)
- Alertas: cards coloreadas por tipo (CierrePendiente=amber, PeriodoBloqueado=rojo)
- Bar chart "Margen por Proyecto" con target line roja
- Integraciones: green/yellow/red status dots
- PieChartComponent eliminado del import

### 3. Proyectos (`projects-list.component.ts`)
- Header de tabla oscuro `#1F4E78`
- Columna ID añadida
- Toolbar: search + Cliente dropdown + Estado dropdown + Filtrar
- Badge total, chips de estado coloreados

### 4. Conceptos (`concepts-list.component.ts`)
- Header de tabla oscuro `#1F4E78`
- Badges de tipo coloreados (Pago=amber, Factura=blue)

### 5. Aprobaciones (`approvals.component.ts`)
- Header de tabla oscuro `#1F4E78` en todas las tablas
- Columna checkbox con Select All
- Batch action bar (Aprobar/Rechazar) al seleccionar
- Filas pendientes con fondo amarillo `#FFF8E1`
- `batchApprove()` y `batchReject()` añadidos a `ApprovalService`

---

## Servicios Afectados

| Servicio | Cambio |
|----------|--------|
| `approvals.service.ts` | Añadidos `batchApprove(ids)` y `batchReject(ids)` |

---

## Build Output

```
✔ Application bundle generation complete.
Warnings:
  - bundle initial exceeded max budget (504.45 kB / 500.00 kB)
  - login styles exceeded max budget (4.29 kB / 4.00 kB)
Errors: 0
```

---

## Pendientes / Observaciones

- Los dropdowns de Cliente y Estado en la toolbar de proyectos son placeholder — requieren inyectar `ClientService` y `CostCenterService` y conectar los form controls a la carga de datos real
- El layout split-panel de conceptos (lista + editor de fórmula) no se implementó — requiere reestructuración mayor del routing
- Considerar aumentar `maximumBudget` en `angular.json` a 550 kB para el bundle inicial
