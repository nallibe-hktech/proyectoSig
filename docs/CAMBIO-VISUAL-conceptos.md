# Cambio visual: conceptos

Tipo: Actualización de diseño UI (sin nuevos RFs)
Archivo: `frontend/public/penpot-design-conceptos.svg`
Acción requerida: Designer y Frontend deben leer el SVG y actualizar el componente Angular correspondiente para que coincida con el nuevo diseño.

## RFs afectados (existentes, sin cambios estructurales)

- **RF-C04** — CRUD Concept con fórmula
- **RF-D07** — Detalle cálculo línea (CalculationLog)

## Componente Angular afectado

- `frontend/src/app/features/concepts/` (ruta lazy-loaded bajo `/conceptos`).
- Incluye: `ConceptsListComponent`, `ConceptFormComponent` (con editor visual de fórmula), `ConceptDetailComponent`.

## Endpoints relacionados (sin cambio)

- `GET /api/concepts` — listado paginado con filtro por tipo y texto
- `GET /api/concepts/{id}` — detalle
- `POST /api/concepts` — crear (Administrator, Backoffice)
- `PUT /api/concepts/{id}` — actualizar
- `DELETE /api/concepts/{id}` — eliminar
- `POST /api/concepts/{id}/validar-formula` — validar sintaxis de fórmula
- `POST /api/concepts/{id}/duplicate` — duplicar concepto
- `GET /api/concepts/{id}/variables` — variables disponibles para formulación

## Elementos UI identificados en el diseño

- Tabla `mat-table` con columnas: Nombre, Tipo (Pago/Factura badge), Desde, Hasta, Acciones
- Filtro por Tipo: chips/toggle Pago | Factura | Todos
- Botón "Nuevo Concepto" + "Duplicar" en cada fila
- **Formulario de concepto**:
  - Campos básicos: Nombre, Tipo (radio), Fecha Desde/Hasta (datepicker)
  - Ámbito de aplicación: selectores multi para Acciones y Usuarios
- **Editor visual de fórmula** (componente crítico):
  - Árbol/línea de expresión con nodos encadenados
  - Añadir nodo: Número, Variable, Fuente de datos (Visitas Celero, Horas Bizneo, Gastos PayHawk, etc.)
  - Operaciones: +, -, ×, ÷, Suma, Cuenta, %
  - Filtros por campo en nodos Source
  - Vista previa del JSON generado
- **Detalle de cálculo** (modal/página):
  - Inputs usados, operación, resultado, origen de datos, fecha de importación
  - Incidencias si las hubo (dataset vacío, división por cero)

## Entidades/endpoints

Sin cambios.

## Notas para Designer

- El editor visual de fórmula es el componente más complejo. Ver `docs/FORMULA_EDITOR_COMPONENTES.md` y `docs/FORMULA_EDITOR_DESIGN_SYSTEM.md` para el diseño detallado.
- Representar la fórmula como una cadena de bloques visuales conectados (drag & drop de nodos).
- Los nodos Source deben mostrar icono del sistema origen (Celero, Bizneo, PayHawk, Intratime).

## Notas para Frontend

- Aplicar `data-testid="concepts-table"`, `data-testid="concepts-row-{conceptId}"`.
- Aplicar `data-testid="formula-editor"`, `data-testid="formula-add-node"`, `data-testid="formula-json-preview"`.
- El editor de fórmula es standalone; debe tener su propio servicio de validación que llame al endpoint de validar.
- Validar accesibilidad WCAG 2.1 AA.
