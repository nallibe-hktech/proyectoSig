# Mockup Visual - Editor de Fórmulas Drag-Drop

## Vista General (1920x1080)

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│ TOOLBAR                                                                                  │
│ ┌──────────────────────────────────────────────────────────────────────────────────────┐│
│ │ [Clear] [Load JSON ▼] [Export ↓] [Undo] [Redo] │ Valid Formula ✓                    ││
│ └──────────────────────────────────────────────────────────────────────────────────────┘│
├────────────────┬──────────────────────────────────────────────────┬────────────────────┤
│                │                                                   │                    │
│  PALETTE       │           CANVAS (EDITOR)                        │  PROPERTIES        │
│  (LEFT)        │                                                   │  (RIGHT)           │
│                │                                                   │                    │
│ ┌────────────┐ │ ┌────────────────────────────────────────────┐   │ ┌────────────────┐ │
│ │ NÚMEROS    │ │ │                                            │   │ │ NODE SELECTED  │ │
│ ├────────────┤ │ │    ┌─────────┐      ┌─────────┐           │   │ ├────────────────┤ │
│ │            │ │ │    │ Número  │      │ Suma    │           │   │ │ Type: Operación│ │
│ │ ↪ Número   │ │ │    │ (50)    │  ──→ │         │           │   │ │                │ │
│ │   Fijo     │ │ │    └─────────┘      │ (+)     │           │   │ │ Operación:     │ │
│ │            │ │ │                     │         │           │   │ │ [Suma ▼]       │ │
│ └────────────┘ │ │    ┌─────────┐      └────┬────┘           │   │ │                │ │
│                │ │    │ Variable│           │                 │   │ │ [Editar]       │ │
│ ┌────────────┐ │ │    │ "Horas" │──────────┘                 │   │ │ [Eliminar]     │ │
│ │ VARIABLES  │ │ │    └─────────┘                             │   │ │ [Duplicar]     │ │
│ ├────────────┤ │ │                                            │   │ └────────────────┘ │
│ │            │ │ │  (Grid de fondo 16px, snap opcional)       │   │                    │
│ │ ↪ Celero   │ │ │                                            │   │                    │
│ │   Visita   │ │ │                                            │   │                    │
│ │            │ │ └────────────────────────────────────────────┘   │                    │
│ │ ↪ PayHawk  │ │                                                   │                    │
│ │   Gasto    │ │                                                   │                    │
│ │            │ │                                                   │                    │
│ │ ↪ Bizneo   │ │                                                   │                    │
│ │   Hora     │ │                                                   │                    │
│ │            │ │                                                   │                    │
│ └────────────┘ │                                                   │                    │
│                │                                                   │                    │
│ ┌────────────┐ │                                                   │                    │
│ │ OPERACIONES│ │                                                   │                    │
│ ├────────────┤ │                                                   │                    │
│ │            │ │                                                   │                    │
│ │ ↪ Suma     │ │                                                   │                    │
│ │ ↪ Resta    │ │                                                   │                    │
│ │ ↪ Multiplica│ │                                                   │                    │
│ │ ↪ Divide   │ │                                                   │                    │
│ │ ↪ Modulo   │ │                                                   │                    │
│ │ ↪ Promedio │ │                                                   │                    │
│ │ ↪ Cuenta   │ │                                                   │                    │
│ │            │ │                                                   │                    │
│ └────────────┘ │                                                   │                    │
│                │                                                   │                    │
├────────────────┴──────────────────────────────────────────────────┴────────────────────┤
│ JSON PREVIEW                                                                            │
│ ┌──────────────────────────────────────────────────────────────────────────────────────┐│
│ │ {                                                                     [Copy]          ││
│ │   "tipo": "operacion",                                                              ││
│ │   "operacion": "suma",                                                              ││
│ │   "operandos": [                                                                    ││
│ │     { "tipo": "numero", "valor": 50 },                                              ││
│ │     { "tipo": "variable", "entidad": "bizneo_hora", "campo": "horas" }              ││
│ │   ]                                                                                 ││
│ │ }                                                                                   ││
│ └──────────────────────────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## Detalle: Componentes en Canvas

### Nodo NÚMERO (Azul)
```
    ┌──────────────────┐
    │ ⊙ Número        │  ← Selector
    │ 50.5             │  ← Valor editable (double-click)
    │ [Connect ●]      │  ← Punto de conexión (salida)
    └──────────────────┘
    Color: #1F4E78 (Primary)
    Ancho: 120px | Alto: 80px
```

### Nodo VARIABLE (Verde)
```
    ┌──────────────────┐
    │ ◆ Variable       │  ← Selector
    │ Bizneo Hora      │  ← Tipo variable
    │ [horas ▼]        │  ← Campo editable
    │ [Connect ●]      │  ← Punto de conexión
    └──────────────────┘
    Color: #70AD47 (Success)
    Ancho: 140px | Alto: 100px
```

### Nodo OPERACIÓN (Naranja)
```
    ┌──────────────────┐
    │ ⊕ Suma           │  ← Tipo operación (editable)
    │ [Connect ●]      │  ← Puntos entrada (izq)
    │ [Connect ●]      │
    │ [Connect ●]      │
    │         [●]      │  ← Punto salida (dcha)
    └──────────────────┘
    Color: #FFC107 (Warning)
    Ancho: 140px | Alto: 120px (dinámico según # operandos)
```

### Conexión entre nodos
```
  Nodo A [●] ────────╔════╗────── [●] Nodo B
         salida      ║    ║       entrada
                Hover: color azul (#2E5C8A)
                Selected: más grueso (2px)
                Normal: gris (#D0D0D0, 1px)
```

---

## Interacciones UX

### 1. Drag-Drop desde Paleta
- Click en "Número Fijo" (o cualquier item de paleta)
- Mientras se arrastra: cursor `grab`, preview semi-transparente sigue mouse
- Drop en canvas: nodo se crea, se selecciona automáticamente
- **Data transferencia**: `{ "tipo": "numero", "operacion": "suma", ... }`

### 2. Edición de Nodo
- **Click en nodo**: selecciona (border resaltado, inspector abre a la derecha)
- **Double-click en valor**: entrada editable aparece (mode inline)
- **Esc**: cancela edición
- **Enter**: guarda edición
- Tipos válidos según nodo:
  - Número: decimal (ej: 50.5, -10)
  - Variable: texto (dropdown de entidades)
  - Operación: dropdown de operadores

### 3. Conexiones
- Hover sobre punto conexión: cursor `crosshair`, punto se resalta
- Click en punto salida + arrastrar → línea sigue mouse
- Release sobre punto entrada: conexión se crea
- Click en conexión: selecciona (resalta), muestra botón eliminar
- **Validación**: 
  - Operación suma: max 2 operandos
  - Operación multiplica: max 2 operandos
  - Operación promedio: min 1, max N operandos
  - No permitir ciclos (A → B → A)

### 4. Selección múltiple (futuro)
- Ctrl+Click: añade a selección
- Shift+Click: rango de selección
- Drag en canvas vacío: marquesina de selección

### 5. Zoom & Pan
- **Scroll**: zoom in/out (1.0 a 2.5x)
- **Espacio + Drag**: pan (mano)
- **Botón Reset**: zoom 1.0, centra fórmula

---

## Estados Visuales

### Nodo Normal (deseleccionado)
- Border: gris #D0D0D0, 1px
- Background: color del tipo (semitransparente 0.1)
- Sombra: sutil (0px 2px 4px rgba(0,0,0,0.1))

### Nodo Selected
- Border: primary #1F4E78, 2px
- Background: más opaco (0.15)
- Sombra: más prominente (0px 4px 8px rgba(0,0,0,0.2))
- Inspector abre a la derecha

### Nodo Dragging (siendo arrastrado)
- Opacity: 0.7
- Cursor: grabbing
- Sombra: (0px 8px 16px rgba(0,0,0,0.3))

### Nodo Invalid
- Border: error rojo #D32F2F, 2px
- Background: error container #FFEBEE
- Icon: ⚠ en esquina
- Tooltip: motivo inválido (ej: "Variable no existe")

### Punto Conexión Normal
- Radio: 4px
- Color: gris #D0D0D0
- Opacity: 0.6

### Punto Conexión Hover
- Radio: 6px
- Color: primary #1F4E78
- Opacity: 1
- Glow: sutil

---

## Property Inspector (Derecha)

### Cuando nodo NÚMERO seleccionado
```
┌──────────────────────────┐
│ Número                    │  (Título)
├──────────────────────────┤
│ Valor                     │
│ [    50.5         ]       │  (Input decimal)
├──────────────────────────┤
│ [Editar] [Eliminar]      │
└──────────────────────────┘
```

### Cuando nodo VARIABLE seleccionado
```
┌──────────────────────────┐
│ Variable                  │
├──────────────────────────┤
│ Origen de datos           │
│ [Bizneo Hora      ▼]     │  (Dropdown)
├──────────────────────────┤
│ Campo/Agregación          │
│ [horas            ▼]     │  (Dropdown)
│                           │
│ Agregación (opcional)     │
│ [Ninguna          ▼]     │  (sum, avg, count...)
├──────────────────────────┤
│ [Editar] [Eliminar] [Dup]│
└──────────────────────────┘
```

### Cuando nodo OPERACIÓN seleccionado
```
┌──────────────────────────┐
│ Operación                 │
├──────────────────────────┤
│ Tipo                      │
│ [Suma             ▼]     │  (Dropdown)
│ Suma | Resta | Multiplica│
│ Divide | Modulo | Promedio│
│ Cuenta                    │
├──────────────────────────┤
│ Operandos: 2              │
│ [+ Añadir operando]       │
├──────────────────────────┤
│ [Editar] [Eliminar]      │
└──────────────────────────┘
```

### Cuando nada seleccionado
```
┌──────────────────────────┐
│                           │
│ Selecciona un nodo       │
│ para ver propiedades      │
│                           │
└──────────────────────────┘
```

---

## Toolbar Superior

```
┌──────────────────────────────────────────────────────────────┐
│ [🗑 Clear] [📂 Load JSON] [💾 Export] │ [↶ Undo] [↷ Redo] │ │
│ Tooltip: Elimina todos los nodos                            │ │
│ Tooltip: Importar desde JSON                                │ │
│ Tooltip: Descargar JSON                                     │ │
│                                        Estado: Valid ✓      │ │
│                        (Rojo si inválida: Invalid ✗)        │ │
└──────────────────────────────────────────────────────────────┘
```

### Load JSON Modal
```
┌─────────────────────────────────┐
│ Importar Fórmula (JSON)          │
├─────────────────────────────────┤
│ Pega tu JSON aquí:              │
│ ┌─────────────────────────────┐ │
│ │ { "tipo": "numero", ...     │ │
│ │ }                           │ │
│ └─────────────────────────────┘ │
│                                 │
│ [Cancelar] [Importar]          │
└─────────────────────────────────┘
```

---

## JSON Preview (Abajo)

```
┌────────────────────────────────────────────────────────────────┐
│ {                                          [Copiar al portapap]│
│   "tipo": "operacion",                                         │
│   "operacion": "suma",                                         │
│   "operandos": [                                               │
│     {                                                          │
│       "tipo": "numero",                                        │
│       "valor": 50                                              │
│     },                                                         │
│     {                                                          │
│       "tipo": "variable",                                      │
│       "entidad": "bizneo_hora",                                │
│       "campo": "horas"                                         │
│     }                                                          │
│   ]                                                            │
│ }                                                              │
└────────────────────────────────────────────────────────────────┘
```

**Nota**: Actualización en tiempo real mientras editas canvas. 
Readonly (no editable a mano). Sintaxis highlighting si es posible.

---

## Paletasssstss Visibles

### NÚMEROS
- Número Fijo (icono: "#")

### VARIABLES
- Celero Visita (icono: "◆", campos: duracion_minutos)
- PayHawk Gasto (icono: "◆", campos: monto)
- Bizneo Hora (icono: "◆", campos: horas, aprobadas)
- Intratime Fichaje (icono: "◆", campos: tiempo_total)
- SGPV Producto (icono: "◆", campos: cantidad, precio)

### OPERACIONES
- Suma (+) [color naranja]
- Resta (-) [color naranja]
- Multiplica (×) [color naranja]
- Divide (÷) [color naranja]
- Modulo (%) [color naranja]
- Promedio (∅) [color naranja]
- Cuenta (∑) [color naranja]

---

## Responsivo

| Breakpoint | Layout |
|---|---|
| < 768px (mobile) | Paleta arriba (tabs), Canvas full, Inspector debajo (modal) |
| 768-1024px (tablet) | Paleta izq (pequeña), Canvas central, Inspector abajo |
| > 1024px (desktop) | Paleta izq (normal), Canvas central (grande), Inspector derecha |

**Desktop primario** para desarrollo (diseño actual arriba). Mobile y tablet son futuro.

