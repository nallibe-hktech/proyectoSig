# Cambio visual: dashboard

Tipo: Actualización de diseño UI (sin nuevos RFs)
Archivo: `penpot-design-dashboard.svg`
Acción requerida: Designer y Frontend deben leer el SVG y actualizar
el componente Angular correspondiente para que coincida con el nuevo diseño.

RFs afectados: RF-B01 (KPIs período), RF-B02 (avisos), RF-B03 (mis proyectos)
Entidades/endpoints: sin cambios.

## Detalles del diseño

- Sidebar oscura (#1F4E78 gradient) con logo SIG, navegación completa (Dashboard activo con indicador verde), ADMINISTRACIÓN (Usuarios, CECOs, Auditoría), perfil usuario abajo
- Top bar blanca: título "Dashboard" + período "Mayo 2026" + selector 📅 ▾ + botón "↻ Recalcular" + campana notificaciones (badge rojo "3")
- 4 KPI cards:
  - Cierres completados (12 ▲ +2 vs mes ant.) — acento #1F4E78
  - Pend. aprobación (3 ⚠) — acento #FFC107
  - Facturación total (€450K ▲ +12%) — acento #70AD47
  - Margen promedio (28%, objetivo: 25%) — acento #163A52
- Sección Alertas con cards de estado (⚠ pendientes FICO, ❌ período bloqueado, ✅ cierre abril)
- Bar chart "Margen por Proyecto — Mayo 2026" (Amex SS 32%, Granini 25%, Amex New 28%, Proj D 21%), línea objetivo 25%
- Tabla "Proyectos Activos": columnas PROYECTO/CLIENTE/ESTADO/COSTE/FACTURACIÓN/MARGEN/ACCIONES, con filas de datos y totales
- Barra "Estado de Integraciones": dots verde/amarillo/rojo para Celero, Bizneo, Intratime, Payhawk, A3 Innuva, A3 ERP, TravelPerk
- Footer: versión + copyright + build date
