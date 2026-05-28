# Cambio visual: conceptos

Tipo: Actualización de diseño UI (sin nuevos RFs)
Archivo: `penpot-design-conceptos.svg`
Acción requerida: Designer y Frontend deben leer el SVG y actualizar
el componente Angular correspondiente para que coincida con el nuevo diseño.

RFs afectados: RF-C04 (CRUD Concept)
Entidades/endpoints: sin cambios.

## Detalles del diseño

- Sidebar con Conceptos activo (indicador verde)
- Top bar: "🧮 Conceptos de Cálculo" + botón "+ Nuevo Concepto"
- Panel izquierdo (480px): lista de conceptos con búsqueda y filtro Tipo
  - Columnas: ID/CONCEPTO/TIPO/HASTA/ACCIONES
  - Filas: C143 "Nota de gastos pago" (Pago ∞), C78 "Nota de gastos facturación" (Factura ∞), C59 "Pago por visita" (Pago 31/12/26), C91 "Sueldo Base" (Pago ∞), C104 "Bonus por Visita" (Pago 31/12/26)
  - Fila activa con acento azul izquierdo + bg #E8F4F8
- Panel derecho (600px): Editor de Fórmula — C143
  - Meta fields: Nombre, Tipo (Pago ▾), Hasta (Sin límite), Desde, Aplica a (multi-select)
  - Visual Formula Builder: tokens coloreados (Variable Σ Gasto + Operador × + Número 1 + botón +)
  - Paleta de elementos: Variables (Σ Gasto Payhawk, N° Visitas Celero, Hrs Bizneo, Hrs Intratime, +Var)
  - Operaciones: +, −, ×, ÷, %, Σ suma, N count, 123 num
  - Preview oscuro: "€ 1.250 — Suma de Gasto Payhawk" con metadata origen/fecha
  - Jerarquía de aplicación: Global (activo) | Proyecto | Acción | Empleado (pills)
  - Botones: 💾 Guardar, Cancelar, 📋 Duplicar
