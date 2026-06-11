import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { FormulaNode } from '../models/formula.model';

/**
 * Componente que renderiza un nodo individual en el canvas
 * Soporta tipos: número, variable, operación
 */
@Component({
  selector: 'app-formula-node',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, MatTooltipModule],
  template: `
    <div
      class="node"
      [class.selected]="selected"
      [class.invalid]="invalid"
      [style.left.px]="node.posX"
      [style.top.px]="node.posY"
      [style.width.px]="node.width"
      [style.height.px]="node.height"
      [style.background-color]="nodeBackgroundColor"
      [style.border-color]="nodeBorderColor"
      [style.z-index]="zIndex"
      (click)="onNodeClick($event)"
      (dblclick)="onNodeDoubleClick($event)"
      (dragstart)="onNodeDragStart($event)"
      (dragend)="onNodeDragEnd()"
      draggable="true"
      [attr.data-testid]="'formula-node-' + node.id"
      [attr.aria-label]="getNodeLabel()"
      role="button"
      tabindex="0"
    >
      <!-- Node content based on type -->
      <ng-container [ngSwitch]="node.type">
        <!-- NÚMERO -->
        <ng-template ngSwitchCase="numero">
          <div class="node-header">
            <mat-icon class="node-icon">looks_3</mat-icon>
            <span class="node-title">Número</span>
          </div>
          <div
            class="node-value"
            [attr.data-testid]="'formula-node-' + node.id + '__value'"
          >
            {{ node.data.valor || 0 }}
          </div>
          <div class="node-connection-point output" [attr.data-testid]="'connection-point-output-' + node.id"></div>
        </ng-template>

        <!-- VARIABLE -->
        <ng-template ngSwitchCase="variable">
          <div class="node-header">
            <mat-icon class="node-icon">data_object</mat-icon>
            <span class="node-title">Variable</span>
          </div>
          <div
            class="node-value"
            [attr.data-testid]="'formula-node-' + node.id + '__value'"
          >
            {{ getEntityLabel(node.data.entidad) }}
          </div>
          <div class="node-subtitle">{{ node.data.campo }}</div>
          <div class="node-connection-point output" [attr.data-testid]="'connection-point-output-' + node.id"></div>
        </ng-template>

        <!-- OPERACIÓN -->
        <ng-template ngSwitchCase="operacion">
          <div class="node-header">
            <mat-icon class="node-icon">calculate</mat-icon>
            <span class="node-title">{{ getOperationLabel(node.data.operacion) }}</span>
          </div>
          <div class="node-inputs">
            <div
              *ngFor="let i of [0, 1, 2]"
              class="node-connection-point input"
              [attr.data-testid]="'connection-point-input-' + node.id + '-' + i"
            ></div>
          </div>
          <div class="node-connection-point output" [attr.data-testid]="'connection-point-output-' + node.id"></div>
        </ng-template>
      </ng-container>

      <!-- Delete button (shown when selected) -->
      <button
        *ngIf="selected"
        class="node-delete-btn"
        mat-icon-button
        matTooltip="Eliminar nodo (Delete)"
        (click)="onDelete($event)"
        [attr.data-testid]="'formula-node-' + node.id + '__delete-btn'"
      >
        <mat-icon>close</mat-icon>
      </button>
    </div>
  `,
  styles: [
    `
      .node {
        position: absolute;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        border: 1px solid #d0d0d0;
        border-radius: 8px;
        padding: 12px;
        background-color: #fafbfc;
        cursor: grab;
        user-select: none;
        transition: all 150ms cubic-bezier(0.4, 0, 0.2, 1);
        box-shadow: 0px 2px 4px rgba(0, 0, 0, 0.1);

        &:hover {
          box-shadow: 0px 4px 8px rgba(0, 0, 0, 0.15);
          cursor: grab;
        }

        &:active {
          cursor: grabbing;
        }

        &.selected {
          border-width: 2px;
          border-color: #1f4e78;
          box-shadow: 0px 6px 12px rgba(31, 78, 120, 0.25);
        }

        &.invalid {
          border: 2px solid #d32f2f;
          background-color: #ffebee;
        }
      }

      .node-header {
        display: flex;
        align-items: center;
        gap: 6px;
        margin-bottom: 8px;
        font-size: 14px;
        font-weight: 500;
      }

      .node-icon {
        font-size: 20px;
        width: 20px;
        height: 20px;
        color: inherit;
      }

      .node-title {
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
      }

      .node-value {
        font-size: 16px;
        font-weight: 600;
        text-align: center;
        color: #1a1a1a;
        word-break: break-word;
        margin-bottom: 6px;
      }

      .node-subtitle {
        font-size: 12px;
        color: #666;
        margin-bottom: 6px;
      }

      .node-inputs {
        display: flex;
        flex-direction: column;
        gap: 6px;
        margin-bottom: 8px;
      }

      .node-connection-point {
        width: 8px;
        height: 8px;
        border-radius: 50%;
        background-color: #d0d0d0;
        cursor: crosshair;
        transition: all 150ms cubic-bezier(0.4, 0, 0.2, 1);

        &:hover {
          width: 12px;
          height: 12px;
          background-color: #1f4e78;
          box-shadow: 0 0 8px rgba(31, 78, 120, 0.5);
        }

        &.input {
          align-self: flex-start;
        }

        &.output {
          align-self: flex-end;
        }
      }

      .node-delete-btn {
        position: absolute;
        top: 2px;
        right: 2px;
        width: 28px;
        height: 28px;
        display: flex;
        align-items: center;
        justify-content: center;
        background-color: rgba(211, 47, 47, 0.1);
        color: #d32f2f;
        border-radius: 4px;
        transition: all 150ms cubic-bezier(0.4, 0, 0.2, 1);

        &:hover {
          background-color: rgba(211, 47, 47, 0.2);
        }

        mat-icon {
          font-size: 16px;
          width: 16px;
          height: 16px;
        }
      }
    `,
  ],
})
export class FormulaNodeComponent implements OnInit {
  @Input() node!: FormulaNode;
  @Input() selected = false;
  @Input() invalid = false;
  @Input() zIndex = 1;

  @Output() nodeSelected = new EventEmitter<string>();
  @Output() nodeDoubleClicked = new EventEmitter<string>();
  @Output() nodeDeleted = new EventEmitter<string>();
  @Output() nodeDragged = new EventEmitter<{ x: number; y: number }>();

  nodeBackgroundColor = '#fafbfc';
  nodeBorderColor = '#d0d0d0';

  private entities: Record<string, string> = {
    celero_visita: 'Celero Visita',
    payhawk_gasto: 'PayHawk Gasto',
    bizneo_hora: 'Bizneo Hora',
    intratime_fichaje: 'Intratime Fichaje',
    sgpv_producto: 'SGPV Producto',
  };

  private operations: Record<string, string> = {
    suma: 'Suma',
    resta: 'Resta',
    multiplica: 'Multiplica',
    divide: 'Divide',
    modulo: 'Módulo',
    promedio: 'Promedio',
    cuenta: 'Cuenta',
  };

  ngOnInit(): void {
    this.updateColors();
  }

  ngOnChanges(): void {
    this.updateColors();
  }

  private updateColors(): void {
    const colors = this.getNodeColors(this.node.type);
    this.nodeBackgroundColor = colors.bg;
    this.nodeBorderColor = this.selected ? colors.borderSelected : this.invalid ? colors.borderInvalid : colors.border;
  }

  private getNodeColors(
    type: string
  ): { bg: string; border: string; borderSelected: string; borderInvalid: string } {
    const baseColors: Record<
      string,
      { bg: string; border: string; borderSelected: string; borderInvalid: string }
    > = {
      numero: {
        bg: '#E6EEF915',
        border: '#1F4E78',
        borderSelected: '#1F4E78',
        borderInvalid: '#D32F2F',
      },
      variable: {
        bg: '#E8F5E915',
        border: '#70AD47',
        borderSelected: '#1F4E78',
        borderInvalid: '#D32F2F',
      },
      operacion: {
        bg: '#FFF3E015',
        border: '#FFC107',
        borderSelected: '#1F4E78',
        borderInvalid: '#D32F2F',
      },
    };

    return baseColors[type] || baseColors['numero'];
  }

  getNodeLabel(): string {
    switch (this.node.type) {
      case 'numero':
        return `Número ${this.node.data.valor || 0}`;
      case 'variable':
        return `Variable ${this.node.data.entidad}`;
      case 'operacion':
        return `Operación ${this.node.data.operacion}`;
      default:
        return `Nodo ${this.node.id}`;
    }
  }

  getEntityLabel(entity?: string): string {
    return entity ? this.entities[entity] || entity : 'Selecciona';
  }

  getOperationLabel(operation?: string): string {
    return operation ? this.operations[operation] || operation : 'Operación';
  }

  onNodeClick(event: MouseEvent): void {
    event.stopPropagation();
    this.nodeSelected.emit(this.node.id);
  }

  onNodeDoubleClick(event: MouseEvent): void {
    event.stopPropagation();
    this.nodeDoubleClicked.emit(this.node.id);
  }

  onNodeDragStart(event: DragEvent): void {
    event.stopPropagation();
    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move';
      event.dataTransfer.setData('application/json', JSON.stringify(this.node));
    }
  }

  onNodeDragEnd(): void {
    // Handled by parent canvas component
  }

  onDelete(event: MouseEvent): void {
    event.stopPropagation();
    this.nodeDeleted.emit(this.node.id);
  }
}
