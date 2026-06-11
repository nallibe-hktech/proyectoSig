import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Connection, FormulaNode } from '../models/formula.model';

/**
 * Componente que renderiza una línea SVG conectando dos nodos
 * Usa curva Bézier para conexiones suaves
 */
@Component({
  selector: 'app-formula-connection',
  standalone: true,
  imports: [CommonModule],
  template: `
    <svg
      class="formula-connection-svg"
      [style.position]="'absolute'"
      [style.top]="'0'"
      [style.left]="'0'"
      [style.pointer-events]="'none'"
      [attr.width]="svgWidth"
      [attr.height]="svgHeight"
      [attr.data-testid]="'formula-connection-' + connection.id"
    >
      <!-- Curva Bézier -->
      <path
        [attr.d]="pathData"
        [attr.stroke]="connectionColor"
        [attr.stroke-width]="selected ? 2 : 1"
        fill="none"
        stroke-linecap="round"
        stroke-linejoin="round"
        class="connection-path"
        (click)="onPathClick($event)"
        [class.selected]="selected"
        [class.invalid]="invalid"
      />

      <!-- Puntos finales (para debug y interacción) -->
      <circle
        *ngIf="showDebug"
        [attr.cx]="startX"
        [attr.cy]="startY"
        r="3"
        fill="#1f4e78"
      />
      <circle
        *ngIf="showDebug"
        [attr.cx]="endX"
        [attr.cy]="endY"
        r="3"
        fill="#70ad47"
      />
    </svg>
  `,
  styles: [
    `
      .formula-connection-svg {
        z-index: 0;
      }

      .connection-path {
        cursor: pointer;
        transition: stroke 150ms cubic-bezier(0.4, 0, 0.2, 1);

        &:hover {
          stroke: #2e5c8a;
          stroke-width: 1.5px;
          filter: drop-shadow(0 0 4px rgba(46, 92, 138, 0.4));
        }

        &.selected {
          stroke: #1f4e78;
          stroke-width: 2px;
          filter: drop-shadow(0 0 6px rgba(31, 78, 120, 0.5));
        }

        &.invalid {
          stroke: #d32f2f;
          stroke-dasharray: 5, 5;
        }
      }
    `,
  ],
})
export class FormulaConnectionComponent implements OnInit, OnChanges {
  @Input() connection!: Connection;
  @Input() fromNode!: FormulaNode;
  @Input() toNode!: FormulaNode;
  @Input() selected = false;
  @Input() invalid = false;
  @Input() showDebug = false;

  @Output() connectionSelected = new EventEmitter<string>();

  pathData = '';
  connectionColor = '#d0d0d0';
  svgWidth = 800;
  svgHeight = 600;

  startX = 0;
  startY = 0;
  endX = 0;
  endY = 0;

  ngOnInit(): void {
    this.updatePath();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['fromNode'] || changes['toNode'] || changes['selected'] || changes['invalid']) {
      this.updatePath();
      this.updateColor();
    }
  }

  private updatePath(): void {
    if (!this.fromNode || !this.toNode) {
      return;
    }

    // Calcular puntos de conexión (centro inferior del nodo origen, centro superior del nodo destino)
    this.startX = this.fromNode.posX + this.fromNode.width / 2;
    this.startY = this.fromNode.posY + this.fromNode.height;

    // Determinar punto de entrada según toPoint
    const inputIndex = this.getInputIndex(this.connection.toPoint);
    const inputSpacing = (this.toNode.height - 40) / 3;
    this.endX = this.toNode.posX + 10;
    this.endY = this.toNode.posY + 20 + inputIndex * inputSpacing;

    // Crear curva Bézier suave
    const dx = this.endX - this.startX;
    const dy = this.endY - this.startY;
    const controlPointOffset = Math.abs(dy) / 2;

    const cp1x = this.startX;
    const cp1y = this.startY + controlPointOffset;
    const cp2x = this.endX;
    const cp2y = this.endY - controlPointOffset;

    this.pathData = `M ${this.startX} ${this.startY} C ${cp1x} ${cp1y}, ${cp2x} ${cp2y}, ${this.endX} ${this.endY}`;

    // Actualizar tamaño SVG para englobar todo
    this.updateSvgSize();
  }

  private updateSvgSize(): void {
    const padding = 20;
    const minX = Math.min(this.startX, this.endX) - padding;
    const minY = Math.min(this.startY, this.endY) - padding;
    const maxX = Math.max(this.startX, this.endX) + padding;
    const maxY = Math.max(this.startY, this.endY) + padding;

    this.svgWidth = maxX - minX;
    this.svgHeight = maxY - minY;
  }

  private updateColor(): void {
    if (this.invalid) {
      this.connectionColor = '#d32f2f';
    } else if (this.selected) {
      this.connectionColor = '#1f4e78';
    } else {
      this.connectionColor = '#d0d0d0';
    }
  }

  private getInputIndex(toPoint: string): number {
    const match = toPoint.match(/input_(\d+)/);
    return match ? parseInt(match[1], 10) : 0;
  }

  onPathClick(event: MouseEvent): void {
    event.stopPropagation();
    this.connectionSelected.emit(this.connection.id);
  }
}
