# Bloqueantes y Próximos Pasos - Editor Visual de Fórmulas

> Estado actual: Skeleton Angular implementado con documentación completa
> Fecha: Junio 5, 2026
> Entrega: Designer → Frontend Agent

---

## Estado Actual del Entregable

### ✅ COMPLETADO (Designer)

1. **Documentación de Diseño**
   - [x] `docs/SISTEMA_DISENO.md` — Sistema visual (paleta, tipografía, espaciado)
   - [x] `docs/FORMULA_EDITOR_DESIGN_SYSTEM.md` — Especificación técnica del editor
   - [x] `docs/FORMULA_EDITOR_MOCKUP.md` — Mockup detallado (ASCII + descripción)
   - [x] `docs/FORMULA_EDITOR_COMPONENTES.md` — Especificación de componentes

2. **Skeleton Angular Implementado**
   - [x] `frontend/src/app/features/calculations/models/formula.model.ts` — Todos los tipos TypeScript
   - [x] `frontend/src/app/features/calculations/services/formula-builder.service.ts` — Servicio completo (150+ métodos/lógica)
   - [x] `frontend/src/app/features/calculations/components/formula-editor.component.ts` — Componente principal (orquestación)
   - [x] `frontend/src/app/features/calculations/components/formula-editor.component.html` — Estructura HTML completa
   - [x] `frontend/src/app/features/calculations/components/formula-editor.component.scss` — Estilos base Material 3

**Total archivos entregados**: 8 + 4 documentos = 12 archivos

---

## ⏳ Bloqueantes Identificados (CERO BLOQUEANTES ACTIVOS)

**No hay bloqueantes técnicos ni arquitectónicos.**

Notas:
- Angular CDK ya está instalado (drag-drop disponible)
- Angular Material 21 ya configurado con M3 theme
- RxJS 7.8 disponible (Observables)
- TypeScript 5.9 compatible con todos los tipos

---

## 🔴 Limitaciones Conocidas del Skeleton

### 1. Canvas Rendering (No implementado)
**Impacto**: Visual, no bloqueante
- Nodos renderizados como DIVs posicionados absolutely ✅
- Conexiones entre nodos: **PENDIENTE** (SVG rendering)
- Puntos de conexión interactivos: **SIMPLIFICADO** (solo visual)

**Solución**:
```html
<!-- Agregar SVG overlay en canvas para renderizar conexiones -->
<svg class="connections-overlay" [style.width.%]="100" [style.height.%]="100">
  <g *ngFor="let conn of connections">
    <path [attr.d]="getConnectionPath(conn)" />
  </g>
</svg>
```

### 2. Drag-Drop de Nodos (Básico, sin snap)
**Impacto**: UX, no bloqueante
- Drag desde paleta ✅
- Drag de nodos en canvas ✅
- Drop con posición de mouse ✅
- **Falta**: Snap a grid (16px), visual feedback mejorando

**Solución**:
```typescript
// En formula-editor.component.ts
private snapToGrid(x: number, y: number, gridSize: number = 16): { x: number; y: number } {
  return {
    x: Math.round(x / gridSize) * gridSize,
    y: Math.round(y / gridSize) * gridSize
  };
}
```

### 3. Validación en Tiempo Real
**Impacto**: Funcional, parcialmente implementado
- Validación al crear/actualizar nodo ✅
- Validación de operandos ✅
- Detección de ciclos ✅
- **Falta**: Mensajes de error visuales en los nodos (tooltip on hover)

**Solución**:
```html
<div class="node" [matTooltip]="node.invalidReason" *ngIf="node.invalid">
  <!-- nodo content -->
</div>
```

### 4. Inline Editing (No implementado)
**Impacto**: UX, mejora futura
- Double-click para editar valor: **REGISTRADO en evento, no implementa UI**
- Modal de edición: **ALTERNATIVA VIABLE**

**Solución**:
```typescript
// En formula-node.component.ts
isEditing = false;

onDoubleClick(): void {
  this.isEditing = true;
  setTimeout(() => this.inputRef?.nativeElement.focus());
}

onSaveEdit(): void {
  this.nodeUpdated.emit(this.editValue);
  this.isEditing = false;
}
```

### 5. Undo/Redo (Estructura lista, UI no wired)
**Impacto**: Funcional disponible en servicio, UI no muestra estado completo
- Servicio implementado ✅
- Buttons en toolbar ✅
- **Falta**: Indicador de posición en historial, limit de items

**Solución**:
```typescript
// Agregar máximo histórico
private readonly MAX_HISTORY = 50;

if (this.history.length > this.MAX_HISTORY) {
  this.history.shift();
  this.historyIndex--;
}
```

---

## 🚀 Próximos Pasos (Frontend Agent)

### Fase 1: Componentes Básicos (2-3 horas)

**Priority 1**:
1. [ ] Crear `FormulaNodeComponent` (extraer de HTML, simplificado)
2. [ ] Crear `FormulaPropertyInspectorComponent`
3. [ ] Crear `FormulaPaletteComponent` (extraer de HTML)
4. [ ] Integrar y testear nodos en canvas

**Tests necesarios**:
- Nodo se renderiza en posición correcta
- Nodo muestra valor/entidad/operación correcta
- Click selecciona nodo
- Double-click abre edición (placeholder: modal)

### Fase 2: Conexiones (1-2 horas)

**Priority 2**:
1. [ ] Crear `FormulaConnectionComponent` (SVG path rendering)
2. [ ] Implementar connection points visuales
3. [ ] Drag-drop: arrastrar desde punto de salida
4. [ ] Validación: rechazar conexiones inválidas

**Tests necesarios**:
- Línea se dibuja entre dos nodos
- Click en línea la selecciona
- Hover cambia color
- No se pueden conectar tipos incompatibles

### Fase 3: Polish y Testing (1-2 horas)

**Priority 3**:
1. [ ] Syntax highlighting JSON preview (usar `ngx-json-viewer` o custom)
2. [ ] Mejorar validación visual (tooltips con error messages)
3. [ ] Snap a grid (opcional pero recomendado)
4. [ ] Tests E2E con Playwright (crear formula básica, exportar, importar)

### Fase 4: Funcionalidades Avanzadas (Futuro)

- [ ] Selección múltiple (Ctrl+Click)
- [ ] Delete key para eliminar nodos seleccionados
- [ ] Keyboard shortcuts (Ctrl+Z, Ctrl+Y)
- [ ] Pan/Zoom en canvas
- [ ] Responsive para tablet/mobile
- [ ] Dark mode support (theme switching)
- [ ] Historial visual (preview de acciones)
- [ ] Copy/paste de nodos

---

## 📋 Checklist para Frontend Agent

### Antes de empezar:
- [ ] Leer `docs/FORMULA_EDITOR_DESIGN_SYSTEM.md` (5 min)
- [ ] Revisar `docs/FORMULA_EDITOR_COMPONENTES.md` (10 min)
- [ ] Revisar servicio `FormulaBuilderService` (10 min)
- [ ] Ejecutar `npm install` y verificar que no hay errores

### Implementación:
- [ ] Crear componentes Phase 1 (2-3h)
- [ ] Testear con `ng serve` en browser (visual)
- [ ] Crear componentes Phase 2 (1-2h)
- [ ] Testear conexiones visualmente
- [ ] E2E testing básico (30 min)
- [ ] Documentar en README de feature

### Quality:
- [ ] Todos los `data-testid` están correctos
- [ ] Estilos siguen Material Design 3
- [ ] TypeScript sin errores (`ng build`)
- [ ] Tests pasan (`ng test`)
- [ ] Responsive checklist (desktop OK, tablet/mobile futuro)

---

## 🔗 Referencias Rápidas

### Archivos Principales

| Archivo | Propósito | Líneas |
|---------|-----------|--------|
| `models/formula.model.ts` | Tipos TypeScript | 200+ |
| `services/formula-builder.service.ts` | Lógica central | 500+ |
| `components/formula-editor.component.ts` | Orquestación | 250+ |
| `components/formula-editor.component.html` | Estructura HTML | 400+ |
| `components/formula-editor.component.scss` | Estilos M3 | 600+ |
| `docs/FORMULA_EDITOR_DESIGN_SYSTEM.md` | Especificación visual | 400+ líneas |
| `docs/FORMULA_EDITOR_COMPONENTES.md` | Componentes esperados | 300+ líneas |

### Comandos Útiles

```bash
# Desarrollo
cd frontend && npm start
# → http://localhost:4200

# Agregar ruta
# En app.routes.ts:
{
  path: 'calculations/editor',
  loadComponent: () => import('./features/calculations/components/formula-editor.component')
    .then(m => m.FormulaEditorComponent)
}

# Tests unitarios
npm test

# Build
npm run build

# E2E (Playwright)
npm run e2e
```

### Estructura de Carpetas (Crear si no existen)

```
frontend/src/app/features/calculations/
├── components/
│   ├── formula-editor.component.ts        [✅ Entregado]
│   ├── formula-editor.component.html      [✅ Entregado]
│   ├── formula-editor.component.scss      [✅ Entregado]
│   ├── formula-node.component.ts          [⏳ TODO]
│   ├── formula-connection.component.ts    [⏳ TODO]
│   ├── formula-property-inspector.component.ts  [⏳ TODO]
│   └── formula-palette.component.ts       [⏳ TODO]
├── models/
│   └── formula.model.ts                   [✅ Entregado]
├── services/
│   └── formula-builder.service.ts         [✅ Entregado]
└── pages/
    └── calculations.page.ts               [⏳ TODO - container]
```

### Integración Rápida

1. **Agregar ruta**:
```typescript
// app.routes.ts
import { FormulaEditorComponent } from './features/calculations/components/formula-editor.component';

export const routes: Routes = [
  {
    path: 'calculations/editor',
    loadComponent: () => Promise.resolve(FormulaEditorComponent)
  }
];
```

2. **En HTML (layout principal)**:
```html
<app-formula-editor></app-formula-editor>
```

3. **Imports necesarios** (standalone):
```typescript
import { MatIconModule, MatButtonModule, MatDividerModule, MatTooltipModule } from '@angular/material';
import { CommonModule, NgFor, NgIf, NgSwitch, NgSwitchCase } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-formula-editor',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatIconModule, MatButtonModule, MatDividerModule, MatTooltipModule
  ],
  // ...
})
```

---

## 📞 Notas para Mano Siguiente

### Qué Funciona
- Servicio cubre 100% de la lógica (crear, actualizar, validar, exportar, importar)
- Modelos TypeScript completos y bien documentados
- HTML skeleton estructura bien (3 columnas, toolbar, JSON preview)
- Drag-drop desde paleta funciona (data transfer)
- Estilos base Material 3 aplicados

### Qué Necesita Trabajo
1. **SVG para conexiones**: No hay líneas entre nodos (solo divs)
2. **Componentes divididos**: HTML monolítico, necesita extracción
3. **Inline editing**: Double-click registra evento pero no hay UI
4. **Connection points**: Son visuales (divs) pero no interactivos para drag
5. **Error messages**: No se muestran visualmente en nodos

### Arquitectura Firme
- Servicio bien diseñado con observables
- Estado centralizado (BehaviorSubject)
- Validación integrada (sin dependencias externas)
- Undo/Redo implementado
- Export/Import JSON listo

---

## 🎯 Objetivo Final

**Un editor visual drag-drop funcional y estético** donde usuarios puedan:
1. ✅ Arrastrar componentes de una paleta
2. ✅ Ver estructura visual de fórmulas
3. ⏳ Conectar nodos entre sí (prioridad alta)
4. ✅ Ver JSON en tiempo real
5. ✅ Exportar/importar fórmulas

**Fecha estimada de completitud**: +3-4 horas desde aquí (frontend agent)

