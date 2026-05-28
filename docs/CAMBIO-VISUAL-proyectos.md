# Cambio visual: proyectos

Tipo: Actualización de diseño UI (sin nuevos RFs)
Archivo: `penpot-design-proyectos.svg`
Acción requerida: Designer y Frontend deben leer el SVG y actualizar
el componente Angular correspondiente para que coincida con el nuevo diseño.

RFs afectados: RF-C02 (CRUD Project)
Entidades/endpoints: sin cambios.

## Detalles del diseño

- Sidebar con Proyectos activo (indicador verde)
- Top bar: "🏗️ Proyectos" + breadcrumb (Inicio › Proyectos) + botón "+ Nuevo Proyecto"
- Barra de filtros: BUSCAR (texto), CLIENTE (dropdown), ESTADO (dropdown), CECO (dropdown), botones Filtrar/Limpiar, badge total (24)
- Tabla con cabecera dark #1F4E78: ID/PROYECTO/CLIENTE/ESTADO/CECO(s)/INTERLOCUTOR/ACCIONES
- Filas con datos reales: Amex Shop Small (Activo, verde), Granini GPVs (Activo), Amex New (Revisión, naranja), Proyecto Alfa (Inactivo, rojo)
- Paginación: "Mostrando 1-4 de 24 proyectos" + controles numéricos
- Panel detalle lateral derecho (overlay): cabecera dark "📋 Detalle — Amex Shop Small"
  - ID, Estado (badge), Cliente, CECO(s), Interlocutor, Departamento, Email, Teléfono
  - Usuarios asignados con avatares (SG, TM)
  - Acciones asociadas como pills (Amex Shop Small, Amex New, +1)
  - Botones: Editar, Duplicar, 🗑️
