import { Component, OnInit, OnDestroy, ViewChild, ElementRef, inject } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { FormulaBuilderService } from '../services/formula-builder.service';
import { EditorState, FormulaNode, Formula, ExportResult } from '../models/formula.model';
import { FormulaCanvasToolbarComponent } from './formula-canvas-toolbar.component';
import { FormulaPaletteComponent } from './formula-palette.component';
import { FormulaCanvasComponent } from './formula-canvas.component';
import { FormulaPropertyInspectorComponent } from './formula-property-inspector.component';

/**
 * Componente principal del Editor Visual de Fórmulas
 * Layout: Paleta (left) | Canvas (center) | Inspector (right)
 *         Toolbar (top) | JSON Preview (bottom)
 */
@Component({
  selector: 'app-formula-editor',
  templateUrl: './formula-editor.component.html',
  styleUrls: ['./formula-editor.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    MatDividerModule,
    FormulaCanvasToolbarComponent,
    FormulaPaletteComponent,
    FormulaCanvasComponent,
    FormulaPropertyInspectorComponent,
  ],
})
export class FormulaEditorComponent implements OnInit, OnDestroy {
  @ViewChild('canvas', { static: false }) canvasRef?: ElementRef<HTMLDivElement>;

  private formulaService = inject(FormulaBuilderService);

  // Estado observable
  editorState$ = this.formulaService.getState$();
  events$ = this.formulaService.getEvents$();

  // Datos para vistas
  paletteItems = this.formulaService.getPaletteItems();
  entities = this.formulaService.getEntities();
  operations = this.formulaService.getOperations();

  selectedNodeId: string | undefined;
  selectedNode: FormulaNode | undefined;
  selectedConnectionId: string | undefined;
  exportedFormula: Formula | null = null;
  isFormulaValid = false;
  canUndoAction = false;
  canRedoAction = false;

  // Dialogs
  showLoadJsonDialog = false;
  loadJsonInput = '';
  loadJsonError = '';

  // Current state
  currentState: EditorState | null = null;
  nodes: FormulaNode[] = [];
  connections: any[] = [];

  // Drag-drop
  draggedItem: any = null;

  private destroy$ = new Subject<void>();

  ngOnInit(): void {
    // Escuchar cambios de estado
    this.editorState$
      .pipe(takeUntil(this.destroy$))
      .subscribe((state) => {
        this.onStateChanged(state);
      });

    // Escuchar eventos
    this.events$
      .pipe(takeUntil(this.destroy$))
      .subscribe((event) => {
        if (event) {
          this.onEditorEvent(event);
        }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ===== TOOLBAR ACTIONS =====

  onClear(): void {
    if (confirm('¿Estás seguro? Se eliminarán todos los nodos.')) {
      this.formulaService.clear();
    }
  }

  onLoadJson(): void {
    this.showLoadJsonDialog = true;
    this.loadJsonInput = '';
    this.loadJsonError = '';
  }

  onImportJson(): void {
    try {
      const formula = JSON.parse(this.loadJsonInput) as Formula;
      this.formulaService.importFromFormula(formula);
      this.showLoadJsonDialog = false;
      this.loadJsonInput = '';
    } catch (error) {
      this.loadJsonError = `JSON inválido: ${error}`;
    }
  }

  onCancelLoadJson(): void {
    this.showLoadJsonDialog = false;
    this.loadJsonInput = '';
    this.loadJsonError = '';
  }

  onExport(): void {
    const result = this.formulaService.exportToFormula();
    const json = JSON.stringify(result.formula, null, 2);
    const blob = new Blob([json], { type: 'application/json' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'formula.json';
    a.click();
    window.URL.revokeObjectURL(url);
  }

  onCopyJson(): void {
    const result = this.formulaService.exportToFormula();
    const json = JSON.stringify(result.formula, null, 2);
    navigator.clipboard.writeText(json);
  }

  onUndo(): void {
    this.formulaService.undo();
  }

  onRedo(): void {
    this.formulaService.redo();
  }

  // ===== PALETTE DRAG-DROP =====

  onPaletteItemDragStart(item: any, event: DragEvent): void {
    this.draggedItem = item;
  }

  onPaletteItemDragEnd(): void {
    this.draggedItem = null;
  }

  // ===== CANVAS DRAG-DROP =====

  onCanvasDrop(event: { templateData: any; x: number; y: number }): void {
    const { templateData, x, y } = event;
    this.formulaService.createNode(templateData.type, x, y, templateData.data);
  }

  // ===== NODE INTERACTIONS =====

  onNodeClick(nodeId: string): void {
    this.formulaService.selectNode(nodeId);
  }

  onNodeDoubleClick(nodeId: string): void {
    console.log('Double click node:', nodeId);
    // Inline edit activation can be added here
  }

  onNodeDelete(nodeId: string): void {
    this.formulaService.deleteNode(nodeId);
  }

  onNodeDuplicate(nodeId: string): void {
    const state = this.formulaService.getCurrentState();
    const node = state.nodes.get(nodeId);

    if (!node) return;

    this.formulaService.createNode(node.type, node.posX + 50, node.posY + 50, node.data);
  }

  onCanvasClicked(): void {
    this.formulaService.selectNode(undefined);
  }

  // ===== CONNECTION INTERACTIONS =====

  onConnectionSelected(connId: string): void {
    this.selectedConnectionId = connId;
  }

  // ===== INSPECTOR ACTIONS =====

  onPropertyChange(nodeId: string, fieldName: string, value: any): void {
    const state = this.formulaService.getCurrentState();
    const node = state.nodes.get(nodeId);

    if (!node) return;

    // Actualizar data según field
    if (node.type === 'numero' && fieldName === 'valor') {
      node.data.valor = parseFloat(value) || 0;
    } else if (node.type === 'variable') {
      if (fieldName === 'entidad') {
        node.data.entidad = value;
      } else if (fieldName === 'campo') {
        node.data.campo = value;
      } else if (fieldName === 'agregacion') {
        node.data.agregacion = value;
      }
    } else if (node.type === 'operacion') {
      if (fieldName === 'operacion') {
        node.data.operacion = value;
      }
    }

    this.formulaService.updateNode(nodeId, { data: node.data });
  }

  // ===== PRIVATE HELPERS =====

  private onStateChanged(state: EditorState): void {
    this.currentState = state;
    this.nodes = Array.from(state.nodes.values());
    this.connections = Array.from(state.connections.values());

    this.selectedNodeId = state.selectedNodeId;
    this.selectedNode = this.selectedNodeId ? state.nodes.get(this.selectedNodeId) : undefined;
    this.isFormulaValid = state.validationErrors.length === 0;
    this.canUndoAction = this.formulaService.canUndo();
    this.canRedoAction = this.formulaService.canRedo();

    const result = this.formulaService.exportToFormula();
    this.exportedFormula = result.formula;
  }

  private onEditorEvent(event: any): void {
    switch (event.type) {
      case 'node_selected':
        break;
      case 'node_created':
        // Analytics, notifications, etc
        break;
      case 'validation_changed':
        break;
      default:
        break;
    }
  }

  // ===== GETTERS =====

  getFormattedJson(): string {
    if (!this.exportedFormula) return '';
    return JSON.stringify(this.exportedFormula, null, 2);
  }
}
