import { Component, OnInit, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { FormulaBuilderService } from '../services/formula-builder.service';
import { EditorState, FormulaNode, Formula, ExportResult } from '../models/formula.model';

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
})
export class FormulaEditorComponent implements OnInit, OnDestroy {
  @ViewChild('canvas', { static: false }) canvasRef?: ElementRef<HTMLDivElement>;

  // Estado observable
  editorState$ = this.formulaService.getState$();
  events$ = this.formulaService.getEvents$();

  // Datos para vistas
  paletteItems = this.formulaService.getPaletteItems();
  selectedNodeId: string | undefined;
  selectedNode: FormulaNode | undefined;
  exportedFormula: Formula | null = null;
  isFormulaValid = false;
  canUndoAction = false;
  canRedoAction = false;

  // Dialogs
  showLoadJsonDialog = false;
  loadJsonInput = '';
  loadJsonError = '';

  // Drag-drop
  draggedItem: any = null;
  draggedNode: FormulaNode | null = null;
  dragOffset = { x: 0, y: 0 };

  private destroy$ = new Subject<void>();

  constructor(private formulaService: FormulaBuilderService) {}

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

  onPaletteItemDragStart(event: DragEvent, item: any): void {
    this.draggedItem = item;
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'copy';
      event.dataTransfer.setData('application/json', JSON.stringify(item.template));
    }
  }

  onPaletteItemDragEnd(): void {
    this.draggedItem = null;
  }

  // ===== CANVAS DRAG-DROP =====

  onCanvasDragOver(event: DragEvent): void {
    event.preventDefault();
    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = 'copy';
    }
  }

  onCanvasDrop(event: DragEvent): void {
    event.preventDefault();

    if (!this.canvasRef) return;

    const rect = this.canvasRef.nativeElement.getBoundingClientRect();
    const x = (event.clientX || 0) - rect.left;
    const y = (event.clientY || 0) - rect.top;

    try {
      const jsonData = event.dataTransfer?.getData('application/json');
      if (jsonData) {
        const template = JSON.parse(jsonData);
        this.formulaService.createNode(template.type, x, y, template.data);
      }
    } catch (error) {
      console.error('Error dropping item:', error);
    }
  }

  // ===== NODE INTERACTIONS =====

  onNodeClick(nodeId: string, event: MouseEvent): void {
    event.stopPropagation();
    this.formulaService.selectNode(nodeId);
  }

  onNodeDoubleClick(nodeId: string, event: MouseEvent): void {
    event.stopPropagation();
    // Activar edición inline (futuro)
    console.log('Double click node:', nodeId);
  }

  onNodeDelete(nodeId: string): void {
    this.formulaService.deleteNode(nodeId);
  }

  onNodeDragStart(node: FormulaNode, event: DragEvent): void {
    this.draggedNode = node;
    this.dragOffset = {
      x: (event.clientX || 0) - node.posX,
      y: (event.clientY || 0) - node.posY,
    };
    event.stopPropagation();
  }

  onNodeDragEnd(): void {
    this.draggedNode = null;
  }

  onCanvasMouseMove(event: MouseEvent): void {
    if (this.draggedNode && this.canvasRef) {
      const rect = this.canvasRef.nativeElement.getBoundingClientRect();
      const x = (event.clientX || 0) - rect.left - this.dragOffset.x;
      const y = (event.clientY || 0) - rect.top - this.dragOffset.y;

      this.formulaService.updateNode(this.draggedNode.id, { posX: x, posY: y });
    }
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

  onNodeDuplicate(nodeId: string): void {
    const state = this.formulaService.getCurrentState();
    const node = state.nodes.get(nodeId);

    if (!node) return;

    this.formulaService.createNode(node.type, node.posX + 50, node.posY + 50, node.data);
  }

  // ===== PRIVATE HELPERS =====

  private onStateChanged(state: EditorState): void {
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

  getNodeColor(nodeType: string): string {
    switch (nodeType) {
      case 'numero':
        return '#1F4E78'; // primary
      case 'variable':
        return '#70AD47'; // success
      case 'operacion':
        return '#FFC107'; // warning
      default:
        return '#D0D0D0'; // outline
    }
  }
}
