# Sistema de Diseño - Editor Visual de Fórmulas

> Especificación técnica para implementación del editor drag-drop de fórmulas.
> Basado en Material Design 3 y paleta SIG-ES.

---

## 1. Paleta de Colores Específica del Editor

### 1.1 Colores de Nodos (por tipo)

```css
/* Nodo NÚMERO */
--formula-node-numero-bg:              #E6EEF9;   /* primary light container */
--formula-node-numero-border:          #1F4E78;   /* primary */
--formula-node-numero-icon:            #1F4E78;

/* Nodo VARIABLE */
--formula-node-variable-bg:            #E8F5E9;   /* success light container */
--formula-node-variable-border:        #70AD47;   /* success */
--formula-node-variable-icon:          #70AD47;

/* Nodo OPERACIÓN */
--formula-node-operation-bg:           #FFF3E0;   /* warning light container */
--formula-node-operation-border:       #FFC107;   /* warning */
--formula-node-operation-icon:         #F57C00;   /* warning dark */

/* Estados de nodo */
--formula-node-selected-border:        #1F4E78;   /* primary, 2px */
--formula-node-invalid-border:         #D32F2F;   /* error */
--formula-node-invalid-bg:             #FFEBEE;   /* error container */
--formula-node-hover-shadow:           0px 4px 8px rgba(0, 0, 0, 0.15);
--formula-node-selected-shadow:        0px 6px 12px rgba(31, 78, 120, 0.25);
```

### 1.2 Colores de Conexiones

```css
--formula-connection-default:          #D0D0D0;   /* outline variant */
--formula-connection-hover:            #2E5C8A;   /* secondary */
--formula-connection-selected:         #1F4E78;   /* primary */
--formula-connection-invalid:          #D32F2F;   /* error */
--formula-connection-stroke-width:     1px;       /* normal */
--formula-connection-selected-width:   2px;
```

### 1.3 UI Canvas

```css
--formula-canvas-bg:                   #FAFBFC;   /* surface + 1 */
--formula-canvas-grid:                 #E8EDF5;   /* surface variant */
--formula-canvas-overlay-bg:           rgba(255, 255, 255, 0.95);
```

### 1.4 Panel Lateral (Paleta & Inspector)

```css
--formula-panel-bg:                    #F5F7FA;   /* surface variant light */
--formula-panel-border:                #E8EDF5;   /* outline variant */
--formula-panel-item-bg:               #FFFFFF;
--formula-panel-item-hover:            #F0F4F8;   /* surface */
--formula-panel-item-active:           #D6DFF3;   /* primary container */
```

### 1.5 Semántica

```css
--formula-status-valid:                #70AD47;   /* success */
--formula-status-invalid:              #D32F2F;   /* error */
--formula-status-warning:              #FFC107;   /* warning */
--formula-status-info:                 #1F4E78;   /* primary */
```

---

## 2. Tamaños y Espaciado

### 2.1 Escala de espaciado (base 4px)

```css
--formula-spacing-xs:     4px;   /* gaps mínimos */
--formula-spacing-sm:     8px;   /* espacios pequeños */
--formula-spacing-md:    12px;   /* default */
--formula-spacing-lg:    16px;   /* espacios mayores */
--formula-spacing-xl:    24px;   /* separadores principales */
--formula-spacing-2xl:   32px;   /* márgenes de secciones */
```

### 2.2 Tamaños de Nodos

#### Nodo NÚMERO
```css
--formula-node-numero-width:   120px;
--formula-node-numero-height:   80px;
--formula-node-numero-padding:  12px;
--formula-node-numero-radius:   8px;
--formula-node-numero-font:    14px / 500;  /* label weight */
--formula-node-numero-value:   16px / 600;  /* body weight */
```

#### Nodo VARIABLE
```css
--formula-node-variable-width:   140px;
--formula-node-variable-height:  100px;
--formula-node-variable-padding: 12px;
--formula-node-variable-radius:  8px;
--formula-node-variable-font:   13px / 400;  /* caption */
--formula-node-variable-label:  14px / 500;  /* label */
```

#### Nodo OPERACIÓN
```css
--formula-node-operation-width:   140px;
--formula-node-operation-height:  base 100px + (operandos * 24px);
--formula-node-operation-padding: 12px;
--formula-node-operation-radius:  8px;
--formula-node-operation-icon:   24px;
```

### 2.3 Puntos de Conexión (Connection Points)

```css
--formula-connection-point-radius:  4px;
--formula-connection-point-hover:   6px;
--formula-connection-point-margin:  8px;  /* desde el edge del nodo */
--formula-connection-distance:      20px; /* separación entre múltiples entradas */
```

### 2.4 Paleta (Left Sidebar)

```css
--formula-palette-width:           200px;
--formula-palette-section-height:  48px;  /* header */
--formula-palette-item-height:     44px;
--formula-palette-item-padding:    12px 16px;
--formula-palette-item-font:       14px / 400;
--formula-palette-item-radius:     6px;
```

### 2.5 Inspector (Right Sidebar)

```css
--formula-inspector-width:         280px;
--formula-inspector-padding:       16px;
--formula-inspector-section-gap:   16px;
--formula-inspector-field-height:  48px;
--formula-inspector-label-font:    12px / 500;  /* caption */
--formula-inspector-value-font:    14px / 400;  /* body small */
--formula-inspector-radius:        8px;
```

### 2.6 Botones y Controles

```css
--formula-button-height:           40px;
--formula-button-padding:          8px 16px;
--formula-button-font:             14px / 500;
--formula-button-radius:           6px;
--formula-button-icon-size:        20px;

--formula-input-height:            40px;
--formula-input-padding:           8px 12px;
--formula-input-font:              14px / 400;
--formula-input-radius:            6px;
--formula-input-border:            1px solid #E8EDF5;
--formula-input-focus-border:      2px solid #1F4E78;
```

---

## 3. Tipografía

### 3.1 Escala de fuentes para el editor

| Contexto | Familia | Tamaño | Peso | Alto línea | Uso |
|----------|---------|--------|------|-----------|-----|
| Nodo título | Inter | 14px | 500 | 1.4 | Nombre del tipo nodo |
| Nodo valor | Inter | 16px | 600 | 1.4 | Número/variable seleccionada |
| Label campo | Inter | 12px | 500 | 1.3 | Labels en inspector |
| Campo input | Inter | 14px | 400 | 1.4 | Inputs, valores editable |
| Operador | Inter | 18px | 700 | 1 | Símbolo operación (+, ×, etc) |
| Tooltip | Inter | 12px | 400 | 1.3 | Ayudas al hover |
| Palette item | Inter | 14px | 400 | 1.4 | Items paleta izquierda |
| JSON preview | Roboto Mono | 12px | 400 | 1.5 | Editor JSON readonly |

---

## 4. Iconografía

### 4.1 Sistema de iconos

Usa **Material Symbols Outlined** (mismo que resto de SIG-ES):
- Tamaño por defecto en nodos: 20px
- Tamaño en botones: 20px
- Tamaño en paleta: 18px

### 4.2 Iconos específicos del editor

```
NODOS:
  Número:     '#' (hash) o 'looks_3' (number)
  Variable:   'data_object' (diamond shape)
  Operación:  'calculate' (gear/sigma)

ACCIONES:
  Clear:      'delete_sweep'
  Load JSON:  'upload_file' o 'folder_open'
  Export:     'download' o 'save'
  Undo:       'undo'
  Redo:       'redo'
  Add:        'add_circle_outline'
  Remove:     'close' o 'remove_circle_outline'
  Edit:       'edit' o 'edit_square'
  Duplicate:  'content_copy'
  Valid:      'check_circle' (verde)
  Invalid:    'error' o 'warning' (rojo)
  
CONEXIONES:
  Connection point: círculo relleno dinámico
```

---

## 5. Componentes Base

### 5.1 Nodo Base (Node Component)

```typescript
interface FormulaNode {
  id: string;
  type: 'numero' | 'variable' | 'operacion';
  posX: number;
  posY: number;
  width: number;
  height: number;
  selected: boolean;
  invalid: boolean;
  data: NodeData;
}

interface NodeData {
  // Número
  valor?: number;
  
  // Variable
  entidad?: string;
  campo?: string;
  agregacion?: 'none' | 'sum' | 'avg' | 'count';
  
  // Operación
  operacion?: string;
  operandos?: FormulaNode[];
}
```

**Estilos por estado:**
- Normal: border 1px, bg container light
- Hover: shadow elevado, cursor move
- Selected: border 2px primary, shadow más prominente
- Dragging: opacity 0.7, shadow flotante
- Invalid: border error, bg error light, icon warning

### 5.2 Connection (Línea de Conexión)

```typescript
interface Connection {
  id: string;
  fromNodeId: string;
  fromPoint: 'output';
  toNodeId: string;
  toPoint: 'input_0' | 'input_1' | ... | 'input_N';
  selected: boolean;
  invalid: boolean;
}
```

**Estilos:**
- Trazo: 1px, color default
- Hover: color secondary, 1.5px
- Selected: color primary, 2px
- Invalid: color error, 2px, possiblemente punteado

### 5.3 Connection Point (Punto de conexión)

```typescript
interface ConnectionPoint {
  nodeId: string;
  pointId: string;  // 'output' | 'input_0' | 'input_1'
  type: 'input' | 'output';
  position: { x: number, y: number };
}
```

**Visualización:**
- Círculo relleno 4px (radio)
- Color: gris por defecto
- Hover: radio 6px, color primary, glow sutil
- Connected: color secundario

### 5.4 Palette Item

```typescript
interface PaletteItem {
  id: string;
  name: string;
  type: 'numero' | 'variable' | 'operacion';
  icon: string;          // ej: 'looks_3'
  category: string;      // 'Números', 'Variables', 'Operaciones'
  draggable: true;
  template: Partial<FormulaNode>;  // Datos iniciales al crear
}
```

**Estilos:**
- Fondo blanco normal
- Hover: bg variant, cursor grab
- Dragging: opacity 0.8, cursor grabbing
- Icono a la izquierda (18px), nombre a la derecha

### 5.5 Property Inspector Field

```typescript
interface InspectorField {
  label: string;
  type: 'text' | 'number' | 'select' | 'multi-select';
  value: any;
  options?: Array<{ label: string, value: any }>;
  editable: boolean;
  onChange: (newValue: any) => void;
}
```

**Estilos:**
- Label: caption (12px, 500), color on-surface
- Input: height 40px, border 1px outline-variant, radius 6px
- Focus: border 2px primary, box-shadow sutil
- Disabled: bg surface-variant, cursor not-allowed

---

## 6. Interacciones y Animaciones

### 6.1 Transiciones

```css
--formula-transition-fast:      150ms cubic-bezier(0.4, 0, 0.2, 1);
--formula-transition-default:   250ms cubic-bezier(0.4, 0, 0.2, 1);
--formula-transition-slow:      350ms cubic-bezier(0.4, 0, 0.2, 1);
```

**Aplicables a:**
- Hover (border, shadow): 150ms
- Selection (color, shadow): 250ms
- Modal/sidebar appear: 250ms
- Conexión draw (trazo): 300ms smooth

### 6.2 Drag-Drop Specifics

#### Drag desde paleta
```
1. Mouse down en item paleta:
   - Cursor: grab
   - Opacity: 0.8 (preview)
   
2. Durante drag:
   - Cursor: grabbing
   - Preview semi-transparente sigue mouse
   - Canvas recibe visual feedback (highlight drop zone)
   
3. Drop en canvas:
   - Nodo creado en posición drop
   - Automáticamente selected
   - Inspector abre propiedades
```

#### Drag nodo en canvas
```
1. Mouse down en nodo:
   - Cursor: grab
   - Selecciona nodo
   
2. Durante drag:
   - Cursor: grabbing
   - Nodo sigue mouse (shadow elevado)
   - Conexiones se redibujan en tiempo real
   - Snap a grid (16px) si está habilitado
   
3. Mouse up:
   - Nodo queda en nueva posición
   - Conexiones se validan
```

#### Conexión entre nodos
```
1. Click en punto salida:
   - Punto se resalta (color primary, radio 6px)
   - Cursor: crosshair
   - Línea temporal sigue mouse
   
2. Hover sobre punto entrada válido:
   - Punto entrada se resalta
   - Línea cambio color (secondary)
   
3. Release sobre punto entrada:
   - Conexión se crea (animada 300ms)
   - Inspector actualiza
   
3b. Release en área vacía:
   - Línea desaparece
   - Sin cambios
```

### 6.3 Inline Editing

```
1. Double-click en valor nodo:
   - Valor actual se selecciona en input
   - Input recibe foco automático
   - Campo editable aparece (transition 150ms)
   
2. Typing:
   - Validación en tiempo real (color border)
   - Preview de tipo numeral/string
   
3. Enter o blur:
   - Guarda valor
   - Input desaparece (transition 150ms)
   - JSON preview actualiza
   
4. Escape:
   - Cancela edición
   - Vuelve al valor anterior
   - Input desaparece
```

### 6.4 Modal Dialogs

```
Load JSON modal:
  - Aparece con transform + opacity (250ms)
  - Textarea recibe focus automático
  - Botones: Cancelar, Importar
  - Validación JSON en tiempo real (color rojo si inválido)
  - Escape cierra modal

Clear Confirmation:
  - Diálogo simple: "¿Eliminar todos los nodos?"
  - Botones: Cancelar, Eliminar
  - Focus en Cancelar por defecto
```

---

## 7. Estados de Nodos

### 7.1 Normal (Deseleccionado, válido)
- **Border**: 1px, color tipo nodo
- **Background**: color tipo container (light)
- **Shadow**: 0px 2px 4px rgba(0,0,0,0.1)
- **Opacity**: 1.0
- **Cursor**: default (o pointer en hover)

### 7.2 Hover (Mouse sobre nodo)
- **Border**: 1px, color tipo más oscuro
- **Shadow**: 0px 4px 8px rgba(0,0,0,0.15)
- **Cursor**: grab
- **Transition**: 150ms

### 7.3 Selected
- **Border**: 2px, color primary #1F4E78
- **Background**: más opaco (+10% del container)
- **Shadow**: 0px 6px 12px rgba(31, 78, 120, 0.25)
- **Inspector**: abierto/visible a la derecha
- **Outline**: visible en canvas

### 7.4 Dragging
- **Opacity**: 0.7
- **Shadow**: 0px 8px 16px rgba(0,0,0,0.3) (flotante)
- **Cursor**: grabbing
- **Z-index**: elevado (999)
- **Pointer-events**: none para otros nodos

### 7.5 Invalid
- **Border**: 2px, color error #D32F2F
- **Background**: error container #FFEBEE
- **Icon**: ⚠ error en esquina superior derecha
- **Tooltip**: aparece al hover (motivo: variable no existe, etc)
- **Shadow**: normal pero con tint rojo

### 7.6 Disabled
- **Opacity**: 0.5
- **Cursor**: not-allowed
- **Pointer-events**: none
- **Background**: más gris

---

## 8. Validación y Restricciones

### 8.1 Reglas de validación

```
NÚMERO:
  ✓ Valor debe ser decimal válido (ej: 50, -10.5, 0.001)
  ✗ Strings, valores inválidos → Invalid
  
VARIABLE:
  ✓ Entidad existe en sistema
  ✓ Campo existe para esa entidad
  ✗ Entidad/campo no existe → Invalid
  
OPERACIÓN:
  ✓ Suma: 2 operandos exactamente
  ✓ Resta: 2 operandos exactamente
  ✓ Multiplica: 2 operandos exactamente
  ✓ Divide: 2 operandos exactamente
  ✓ Modulo: 2 operandos exactamente
  ✓ Promedio: 1-N operandos
  ✓ Cuenta: 1-N operandos
  ✗ Operandos faltantes → Invalid
  ✗ Operandos excesivos → Invalid
  
CICLOS:
  ✓ No permitir A → B → A
  ✗ Si se detecta ciclo → conexión se rechaza
```

### 8.2 Indicador de validez

**Toolbar mostrará:**
- "Valid Formula ✓" en verde (#70AD47) si la fórmula es válida
- "Invalid Formula ✗" en rojo (#D32F2F) si hay errores

**Nodos invalid:** border rojo, icon warning, tooltip con causa

---

## 9. Accesibilidad (WCAG 2.1 AA)

### 9.1 Contraste

- Texto sobre fondos: ratio mín 4.5:1
- Border sobre fondo: ratio mín 3:1
- En nodos, asegurarse colores bastante saturados

### 9.2 Focus Visible

- Todos los elementos interactivos (nodos, botones, inputs):
  ```css
  outline: 2px solid #1F4E78;
  outline-offset: 2px;
  ```

### 9.3 Aria Labels

```html
<!-- Nodo -->
<div aria-label="Número 50" role="button" tabindex="0">

<!-- Paleta item -->
<div aria-label="Número Fijo - Arrastra para agregar" draggable="true">

<!-- Connection point -->
<circle aria-label="Punto de salida - Arrastra para conectar" />

<!-- Botones toolbar -->
<button aria-label="Limpiar todos los nodos">Clear</button>
```

### 9.4 Keyboard Navigation

- **Tab**: mueve focus por nodos, inputs, botones
- **Enter/Space**: selecciona nodo, activa drag
- **Delete**: elimina nodo seleccionado
- **Escape**: cancela inline edit, cierra modales
- **Ctrl+Z**: undo
- **Ctrl+Y**: redo

---

## 10. Data-testid Conventions

```html
<!-- Nodos -->
data-testid="formula-node-{id}"
data-testid="formula-node-{id}__title"
data-testid="formula-node-{id}__value"
data-testid="formula-node-{id}__delete-btn"

<!-- Conexiones -->
data-testid="formula-connection-{id}"
data-testid="formula-connection-{id}__delete-btn"

<!-- Paleta -->
data-testid="formula-palette-section-{category}"
data-testid="formula-palette-item-{itemId}"

<!-- Inspector -->
data-testid="formula-inspector-field-{fieldName}"
data-testid="formula-inspector-delete-btn"

<!-- Canvas -->
data-testid="formula-canvas"
data-testid="formula-canvas__grid"

<!-- Toolbar -->
data-testid="formula-toolbar-clear"
data-testid="formula-toolbar-load"
data-testid="formula-toolbar-export"
data-testid="formula-toolbar-status"

<!-- JSON Preview -->
data-testid="formula-json-preview"
data-testid="formula-json-copy-btn"
```

---

## 11. Responsive Design

| Breakpoint | Cambio Layout |
|---|---|
| < 768px (mobile) | No soportado aún. Mostrar mensaje "Usar en desktop" |
| 768-1024px (tablet) | Paleta horizontal arriba, Canvas central, Inspector abajo modal |
| > 1024px (desktop) | Paleta izq, Canvas central, Inspector derecha (spec actual) |

**Nota**: El diseño actual es **desktop-first**. Mobile/tablet son futuros.

---

## 12. Temas (Light/Dark)

El editor debe soportar tema claro y oscuro usando las variables CSS de Material Design 3.

**En tema oscuro:**
- Fondo canvas más gris (#121212)
- Nodos más claros (menos contraste visual)
- Texto más claro (blanco)
- Bordes más oscuros (más visibilidad)
- Shadows más sutiles

Las variables `--formula-*` deben cambiar automáticamente con `@media (prefers-color-scheme: dark)`.

