import { Component, Input, Output, EventEmitter, ViewChild, ElementRef, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormulaNode, Connection, EditorState } from '../models/formula.model';
import { FormulaNodeComponent } from './formula-node.component';
import { FormulaConnectionComponent } from './formula-connection.component';

/**
 * Componente Canvas: área central para renderizar nodos y conexiones
 * Maneja drag-drop, selección y zoom
 */
@Component({
  selector: 'app-formula-canvas',
  standalone: true,
  imports: [CommonModule, FormulaNodeComponent, FormulaConnectionComponent],
  template: `
    <div
      class="canvas-container"
      #canvas
      (dragover)="onCanvasDragOver($event)"
      (drop)="onCanvasDrop($event)"
      (mousemove)="onCanvasMouseMove($event)"
      (mouseup)="onCanvasMouseUp()"
      (click)="onCanvasClick()"
      data-testid="formula-canvas"
      [style.cursor]="'default'"
    >
      <!-- Grid background -->
      <div class="canvas-grid" data-testid="formula-canvas__grid"></div>

      <!-- SVG layer for connections (z-index 0) -->
      <svg class="connections-layer" [attr.width]="canvasWidth" [attr.height]="canvasHeight">
        <app-formula-connection
          *ngFor="let conn of connections"
          [connection]="conn"
          [fromNode]="getNode(conn.fromNodeId)"
          [toNode]="getNode(conn.toNodeId)"
          [selected]="selectedConnectionId === conn.id"
          [invalid]="conn.invalid"
          (connectionSelected)="onConnectionSelected($event)"
        />
      </svg>

      <!-- Nodes layer (z-index 1+) -->
      <div class="nodes-layer">
        <app-formula-node
          *ngFor="let node of nodes; trackBy: trackByNodeId"
          [node]="node"
          [selected]="selectedNodeId === node.id"
          [invalid]="node.invalid"
          [zIndex]="selectedNodeId === node.id ? 100 : 1"
          (nodeSelected)="onNodeSelected($event)"
          (nodeDoubleClicked)="onNodeDoubleClicked($event)"
          (nodeDeleted)="onNodeDeleted($event)"
          (nodeDragged)="onNodeDragged($event)"
        />
      </div>
    </div>
  `,
  styles: [
    `
      .canvas-container {
        flex: 1;
        background-color: #fafbfc;
        position: relative;
        overflow: hidden;
        display: flex;
        align-items: center;
        justify-content: center;
      }

      .canvas-grid {
        position: absolute;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background-image: linear-gradient(
            0deg,
            transparent 24%,
            #e8edf5 25%,
            #e8edf5 26%,
            transparent 27%,
            transparent 74%,
            #e8edf5 75%,
            #e8edf5 76%,
            transparent 77%,
            transparent
          ),
          linear-gradient(
            90deg,
            transparent 24%,
            #e8edf5 25%,
            #e8edf5 26%,
            transparent 27%,
            transparent 74%,
            #e8edf5 75%,
            #e8edf5 76%,
            transparent 77%,
            transparent
          );
        background-size: 20px 20px;
        z-index: 0;
        pointer-events: none;
      }

      .connections-layer {
        position: absolute;
        top: 0;
        left: 0;
        z-index: 0;
        pointer-events: all;
      }

      .nodes-layer {
        position: absolute;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        z-index: 1;
      }
    `,
  ],
})
export class FormulaCanvasComponent implements OnInit {
  @Input() nodes: FormulaNode[] = [];
  @Input() connections: Connection[] = [];
  @Input() selectedNodeId: string | undefined;
  @Input() selectedConnectionId: string | undefined;
  @Input() editorState: EditorState | null = null;

  @Output() nodeSelected = new EventEmitter<string>();
  @Output() nodeDoubleClicked = new EventEmitter<string>();
  @Output() nodeDeleted = new EventEmitter<string>();
  @Output() nodeDragStarted = new EventEmitter<FormulaNode>();
  @Output() nodeDragged = new EventEmitter<{ nodeId: string; x: number; y: number }>();
  @Output() nodeDropped = new EventEmitter<{ nodeId: string; x: number; y: number }>();
  @Output() nodeDragEnded = new EventEmitter<void>();
  @Output() canvasDropped = new EventEmitter<{ templateData: any; x: number; y: number }>();
  @Output() connectionSelected = new EventEmitter<string>();
  @Output() canvasClicked = new EventEmitter<void>();

  @ViewChild('canvas', { static: false }) canvasRef?: ElementRef<HTMLDivElement>;

  canvasWidth = 1600;
  canvasHeight = 1200;

  private draggedNode: FormulaNode | null = null;
  private dragOffset = { x: 0, y: 0 };

  ngOnInit(): void {
    // Canvas initialization
  }

  getNode(nodeId: string): FormulaNode {
    return this.nodes.find((n) => n.id === nodeId) || ({} as FormulaNode);
  }

  trackByNodeId(index: number, node: FormulaNode): string {
    return node.id;
  }

  // ===== DRAG-DROP FROM PALETTE =====

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
        const templateData = JSON.parse(jsonData);
        this.canvasDropped.emit({ templateData, x, y });
      }
    } catch (error) {
      console.error('Error dropping item on canvas:', error);
    }
  }

  // ===== NODE INTERACTIONS =====

  onNodeSelected(nodeId: string): void {
    this.nodeSelected.emit(nodeId);
  }

  onNodeDoubleClicked(nodeId: string): void {
    this.nodeDoubleClicked.emit(nodeId);
  }

  onNodeDeleted(nodeId: string): void {
    this.nodeDeleted.emit(nodeId);
  }

  onNodeDragged(pos: { x: number; y: number }): void {
    // Handled by mouse move
  }

  // ===== CANVAS MOUSE EVENTS =====

  onCanvasMouseMove(event: MouseEvent): void {
    // Canvas mouse move for potential future interactions
  }

  onCanvasMouseUp(): void {
    // Handle mouse up for drag end
  }

  onCanvasClick(): void {
    this.canvasClicked.emit();
  }

  // ===== CONNECTION INTERACTIONS =====

  onConnectionSelected(connId: string): void {
    this.connectionSelected.emit(connId);
  }
}
