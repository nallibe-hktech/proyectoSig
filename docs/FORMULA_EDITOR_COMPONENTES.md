# Componentes Esperados - Editor Visual de Fórmulas

Documento de especificación para componentes compartidos que el frontend debe crear/completar en futuras iteraciones.

---

## 1. Componentes Implementados (Skeleton)

### FormulaEditorComponent
- **Selector**: `app-formula-editor`
- **Ubicación**: `frontend/src/app/features/calculations/components/formula-editor.component.ts`
- **Estado**: ✅ Skeleton implementado (HTML + lógica base)
- **Responsabilidad**: Contenedor principal, orquesta paleta, canvas, inspector, JSON preview
- **Props (Inputs)**: Ninguno (actualmente)
- **Outputs**: Ninguno (actualmente, usa observables)
- **Dependencias**:
  - `FormulaBuilderService` (inyectado)
  - Angular Material (`mat-icon`, `mat-button`, `mat-divider`, `mat-tooltip`)

---

## 2. Componentes a Completar (Frontend Agent)

### 2.1 FormulaNodeComponent
- **Selector**: `app-formula-node`
- **Ubicación**: `frontend/src/app/features/calculations/components/formula-node.component.ts`
- **Estado**: Pendiente
- **Responsabilidad**: Renderiza un nodo individual en el canvas (NÚMERO, VARIABLE, u OPERACIÓN)
- **Props (Inputs)**:
  ```typescript
  @Input() node: FormulaNode;
  @Input() selected: boolean = false;
  @Input() invalid: boolean = false;
  @Input() zIndex: number = 1;
  ```
- **Outputs**:
  ```typescript
  @Output() nodeSelected = new EventEmitter<string>();
  @Output() nodeDoubleClicked = new EventEmitter<string>();
  @Output() nodeDeleted = new EventEmitter<string>();
  @Output() nodeDragged = new EventEmitter<{ x: number; y: number }>();
  ```
- **Estilos Esperados**:
  - Altura/ancho dinámico según `node.width` y `node.height`
  - Colores según tipo (número: azul, variable: verde, operación: naranja)
  - Estado selected: border 2px primary, shadow elevada
  - Estado invalid: border rojo, bg error-light, icon warning

---

### 2.2 FormulaConnectionComponent
- **Selector**: `app-formula-connection`
- **Ubicación**: `frontend/src/app/features/calculations/components/formula-connection.component.ts`
- **Estado**: Pendiente
- **Responsabilidad**: Renderiza una línea SVG conectando dos nodos
- **Props (Inputs)**:
  ```typescript
  @Input() connection: Connection;
  @Input() fromNode: FormulaNode;
  @Input() toNode: FormulaNode;
  @Input() selected: boolean = false;
  @Input() invalid: boolean = false;
  ```
- **Outputs**:
  ```typescript
  @Output() connectionSelected = new EventEmitter<string>();
  @Output() connectionDeleted = new EventEmitter<string>();
  ```
- **Implementación**:
  - Usar SVG `<path>` con curva Bézier
  - Calcular puntos inicio/fin desde connection points del nodo
  - Trazo: 1px normal, 2px selected, punteado si invalid
  - Colores: gris normal, primary si selected, error si invalid

---

### 2.3 FormulaPropertyInspectorComponent
- **Selector**: `app-formula-property-inspector`
- **Ubicación**: `frontend/src/app/features/calculations/components/formula-property-inspector.component.ts`
- **Estado**: Parcialmente en HTML (puede extraerse a componente separado)
- **Responsabilidad**: Panel derecho mostrando propiedades del nodo seleccionado
- **Props (Inputs)**:
  ```typescript
  @Input() node: FormulaNode | null;
  @Input() entities: EntityMetadata[] = [];
  @Input() operations: OperationConfig[] = [];
  ```
- **Outputs**:
  ```typescript
  @Output() propertyChanged = new EventEmitter<{
    nodeId: string;
    fieldName: string;
    value: any;
  }>();
  @Output() nodeDeleted = new EventEmitter<string>();
  @Output() nodeDuplicated = new EventEmitter<string>();
  ```
- **Campos dinámicos según tipo de nodo**:
  - **Número**: input decimal
  - **Variable**: selects para entidad, campo, agregación
  - **Operación**: select para tipo operación, info operandos

---

### 2.4 FormulaPaletteComponent
- **Selector**: `app-formula-palette`
- **Ubicación**: `frontend/src/app/features/calculations/components/formula-palette.component.ts`
- **Estado**: Parcialmente en HTML (puede extraerse a componente separado)
- **Responsabilidad**: Panel izquierdo con items draggables
- **Props (Inputs)**:
  ```typescript
  @Input() items: PaletteItem[] = [];
  ```
- **Outputs**:
  ```typescript
  @Output() itemDragStart = new EventEmitter<{ item: PaletteItem; event: DragEvent }>();
  @Output() itemDragEnd = new EventEmitter<void>();
  ```
- **Estructura**:
  - Secciones colapsibles: Números, Variables, Operaciones
  - Items con icono + nombre
  - Draggable con visual feedback

---

### 2.5 FormulaJsonPreviewComponent
- **Selector**: `app-formula-json-preview`
- **Ubicación**: `frontend/src/app/features/calculations/components/formula-json-preview.component.ts`
- **Estado**: Parcialmente en HTML
- **Responsabilidad**: Panel inferior mostrando JSON readonly
- **Props (Inputs)**:
  ```typescript
  @Input() formula: Formula | null;
  @Input() isValid: boolean = false;
  ```
- **Outputs**:
  ```typescript
  @Output() copiedToClipboard = new EventEmitter<void>();
  ```
- **Features**:
  - Syntax highlighting (opcional pero recomendado)
  - Copy to clipboard button
  - Actualización en tiempo real

---

### 2.6 FormulaLoadJsonDialogComponent
- **Selector**: `app-formula-load-json-dialog`
- **Ubicación**: `frontend/src/app/features/calculations/components/formula-load-json-dialog.component.ts`
- **Estado**: Parcialmente en HTML (como overlay)
- **Responsabilidad**: Modal para importar JSON
- **Props (Inputs)**:
  ```typescript
  @Input() visible: boolean = false;
  ```
- **Outputs**:
  ```typescript
  @Output() jsonSubmitted = new EventEmitter<string>();
  @Output() closed = new EventEmitter<void>();
  ```
- **Features**:
  - Textarea para pegar JSON
  - Validación JSON en tiempo real
  - Botones: Cancelar, Importar

---

## 3. Servicios Implementados

### 3.1 FormulaBuilderService
- **Ubicación**: `frontend/src/app/features/calculations/services/formula-builder.service.ts`
- **Estado**: ✅ Completamente implementado
- **Responsabilidades**:
  - Gestionar estado del editor (nodos, conexiones)
  - Crear, actualizar, eliminar nodos
  - Validar estructura de fórmula
  - Exportar/importar JSON
  - Undo/Redo
  - Emitir eventos

**Métodos públicos principales**:
```typescript
getState$(): Observable<EditorState>                 // Observable de estado actual
getEvents$(): Observable<EditorEvent | null>        // Observable de eventos
getCurrentState(): EditorState                       // Snapshot del estado
getPaletteItems(): PaletteItem[]                     // Items para paleta
getEntities(): EntityMetadata[]                      // Metadatos de entidades
getOperations(): OperationConfig[]                   // Config de operaciones

createNode(type, posX, posY, data?): FormulaNode    // Crear nodo
updateNode(nodeId, updates): void                    // Actualizar nodo
deleteNode(nodeId): void                             // Eliminar nodo
selectNode(nodeId): void                             // Seleccionar nodo

createConnection(fromId, toId, toPoint): Connection | null  // Conectar
deleteConnection(connId): void                       // Desconectar

validate(): ValidationError[]                        // Validar
exportToFormula(): ExportResult                      // Exportar a JSON
importFromFormula(formula): void                     // Importar desde JSON
exportState(): string                                // Serializar estado
importState(json): boolean                           // Deserializar estado

undo(): void                                         // Deshacer
redo(): void                                         // Rehacer
canUndo(): boolean
canRedo(): boolean

clear(): void                                        // Limpiar canvas
```

---

## 4. Modelos de Datos

**Ubicación**: `frontend/src/app/features/calculations/models/formula.model.ts`

Todos los tipos TypeScript necesarios están definidos:
- `FormulaNode`
- `Connection`
- `ConnectionPoint`
- `EditorState`
- `ValidationError`
- `PaletteItem`
- `EntityMetadata`
- `EntityField`
- `OperationConfig`
- `Formula` (formato JSON salida)
- `EditorEvent`

---

## 5. Integración con el Resto de la App

### Routing
```typescript
// app.routes.ts
const routes: Routes = [
  {
    path: 'calculations',
    loadComponent: () => import('./features/calculations/pages/calculations.page').then(m => m.CalculationsPage),
    children: [
      {
        path: 'editor',
        loadComponent: () => import('./features/calculations/components/formula-editor.component').then(m => m.FormulaEditorComponent)
      }
    ]
  }
];
```

### Module/Imports
```typescript
// FormulaEditorComponent debe ser standalone o importado en feature module
import { FormulaEditorComponent } from './components/formula-editor.component';
import { FormulaBuilderService } from './services/formula-builder.service';

// En module o config:
imports: [FormulaEditorComponent, ...MaterialModules]
providers: [FormulaBuilderService]
```

---

## 6. Testing

Cada componente debe tener tests con:
```typescript
// formula-editor.component.spec.ts
describe('FormulaEditorComponent', () => {
  it('should create node on canvas drop', () => {
    // TODO
  });

  it('should export valid formula JSON', () => {
    // TODO
  });

  it('should validate connections', () => {
    // TODO
  });
});
```

**Usar `data-testid`** en todos los elementos interactivos para E2E tests (Playwright):
```typescript
data-testid="formula-canvas"
data-testid="formula-node-{id}"
data-testid="formula-connection-{id}"
data-testid="formula-palette-item-{itemId}"
data-testid="formula-inspector-field-{fieldName}"
data-testid="formula-toolbar-clear"
```

---

## 7. Prioridad de Implementación

**Fase 1 (MVP)**:
1. ✅ FormulaEditorComponent (skeleton)
2. ✅ FormulaBuilderService
3. ✅ Modelos
4. ⏳ FormulaNodeComponent (simple, sin conexiones)
5. ⏳ FormulaPropertyInspectorComponent
6. ⏳ Validación básica

**Fase 2 (Conexiones)**:
7. ⏳ FormulaConnectionComponent (SVG rendering)
8. ⏳ Drag-drop entre nodos (connection creation)
9. ⏳ Validación de ciclos

**Fase 3 (Polish)**:
10. ⏳ FormulaJsonPreviewComponent (syntax highlighting)
11. ⏳ Undo/Redo UI
12. ⏳ Responsive para tablet/mobile
13. ⏳ Animaciones refinadas

---

## 8. Dependencias Externas Necesarias

```json
{
  "dependencies": {
    "@angular/animations": "^21.2.14",
    "@angular/cdk": "^21.2.12",  // Ya instalado (drag-drop)
    "@angular/common": "^21.2.0",
    "@angular/core": "^21.2.0",
    "@angular/forms": "^21.2.0",
    "@angular/material": "^21.2.12",  // Ya instalado
    "@angular/platform-browser": "^21.2.0",
    "@angular/router": "^21.2.0",
    "rxjs": "~7.8.0"
  }
}
```

**Ninguna dependencia adicional necesaria** — el proyecto ya tiene todo instalado.

---

## 9. Checklistfor Frontend Agent

- [ ] Extraer FormulaNodeComponent del HTML
- [ ] Crear FormulaConnectionComponent (SVG)
- [ ] Extraer FormulaPropertyInspectorComponent
- [ ] Extraer FormulaPaletteComponent
- [ ] Extraer FormulaJsonPreviewComponent
- [ ] Implementar drag-drop de nodos en canvas
- [ ] Implementar conexión drag-drop
- [ ] Mejorar validación en tiempo real
- [ ] Agregar syntax highlighting a JSON preview
- [ ] Tests unitarios para cada componente
- [ ] Tests E2E con Playwright
- [ ] Documentar en README de features/calculations
- [ ] Soporte responsive (tablet/mobile)
- [ ] Animaciones suaves (transitions)
- [ ] Integración en rutas principales
- [ ] Manejo de errores (importación JSON inválida)
- [ ] Performance: canvas rendering optimizado
- [ ] Accesibilidad: ARIA labels, focus management

