import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { FormulaNode, EntityMetadata, OperationConfig } from '../models/formula.model';

/**
 * Componente Property Inspector: panel derecho con propiedades del nodo seleccionado
 */
@Component({
  selector: 'app-formula-property-inspector',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule, MatButtonModule, MatTooltipModule],
  template: `
    <div class="inspector-panel" data-testid="formula-inspector">
      <div class="inspector-content">
        <!-- Si hay nodo seleccionado -->
        <ng-container *ngIf="node">
          <h3 class="inspector-title">{{ node.type | uppercase }}</h3>

          <!-- NÚMERO -->
          <ng-container *ngIf="node.type === 'numero'">
            <div class="inspector-field">
              <label for="valor-input">Valor</label>
              <input
                id="valor-input"
                type="number"
                step="0.01"
                [value]="node.data.valor || 0"
                (change)="onPropertyChange('valor', $event)"
                [attr.data-testid]="'formula-inspector-field-valor'"
              />
            </div>
          </ng-container>

          <!-- VARIABLE -->
          <ng-container *ngIf="node.type === 'variable'">
            <div class="inspector-field">
              <label for="entidad-select">Origen de datos</label>
              <select
                id="entidad-select"
                [value]="node.data.entidad || ''"
                (change)="onPropertyChange('entidad', $event)"
                [attr.data-testid]="'formula-inspector-field-entidad'"
              >
                <option value="">-- Selecciona --</option>
                <option
                  *ngFor="let entity of entities"
                  [value]="entity.id"
                >
                  {{ entity.label }}
                </option>
              </select>
            </div>

            <div class="inspector-field">
              <label for="campo-select">Campo</label>
              <select
                id="campo-select"
                [value]="node.data.campo || ''"
                (change)="onPropertyChange('campo', $event)"
                [attr.data-testid]="'formula-inspector-field-campo'"
              >
                <option value="">-- Selecciona --</option>
                <option
                  *ngFor="let field of getFieldsForEntity(node.data.entidad)"
                  [value]="field.name"
                >
                  {{ field.label }}
                </option>
              </select>
            </div>

            <div class="inspector-field">
              <label for="agregacion-select">Agregación (opcional)</label>
              <select
                id="agregacion-select"
                [value]="node.data.agregacion || 'none'"
                (change)="onPropertyChange('agregacion', $event)"
                [attr.data-testid]="'formula-inspector-field-agregacion'"
              >
                <option value="none">Ninguna</option>
                <option value="sum">Suma</option>
                <option value="avg">Promedio</option>
                <option value="count">Cantidad</option>
              </select>
            </div>
          </ng-container>

          <!-- OPERACIÓN -->
          <ng-container *ngIf="node.type === 'operacion'">
            <div class="inspector-field">
              <label for="operacion-select">Tipo de operación</label>
              <select
                id="operacion-select"
                [value]="node.data.operacion || ''"
                (change)="onPropertyChange('operacion', $event)"
                [attr.data-testid]="'formula-inspector-field-operacion'"
              >
                <option value="">-- Selecciona --</option>
                <option
                  *ngFor="let op of operations"
                  [value]="op.id"
                >
                  {{ op.label }}
                </option>
              </select>
            </div>

            <div class="inspector-info">
              <p>Operandos conectados: <strong>{{ node.data.operandos?.length || 0 }}</strong></p>
              <p class="info-small">Esperados: {{ getOperationConfig()?.minOperands }} - {{ getOperationConfig()?.maxOperands || '∞' }}</p>
            </div>
          </ng-container>

          <!-- Botones de acción -->
          <div class="inspector-actions">
            <button
              mat-stroked-button
              (click)="onDuplicate()"
              [attr.data-testid]="'formula-inspector-duplicate-btn'"
              matTooltip="Duplicar nodo (Ctrl+D)"
            >
              <mat-icon>content_copy</mat-icon>
              Duplicar
            </button>

            <button
              mat-stroked-button
              color="warn"
              (click)="onDelete()"
              [attr.data-testid]="'formula-inspector-delete-btn'"
              matTooltip="Eliminar nodo (Delete)"
            >
              <mat-icon>delete</mat-icon>
              Eliminar
            </button>
          </div>
        </ng-container>

        <!-- Si no hay nodo seleccionado -->
        <ng-container *ngIf="!node">
          <div class="inspector-empty">
            <mat-icon>info</mat-icon>
            <p>Selecciona un nodo para ver sus propiedades</p>
          </div>
        </ng-container>
      </div>
    </div>
  `,
  styles: [
    `
      .inspector-panel {
        width: 280px;
        background-color: #f5f7fa;
        border-left: 1px solid #e8edf5;
        flex-shrink: 0;
        overflow: hidden;
        display: flex;
        flex-direction: column;
      }

      .inspector-content {
        flex: 1;
        overflow-y: auto;
        padding: 16px;
        display: flex;
        flex-direction: column;
        gap: 16px;

        &::-webkit-scrollbar {
          width: 6px;
        }

        &::-webkit-scrollbar-track {
          background: transparent;
        }

        &::-webkit-scrollbar-thumb {
          background: #d0d0d0;
          border-radius: 3px;

          &:hover {
            background: #2e5c8a;
          }
        }
      }

      .inspector-title {
        font-size: 16px;
        font-weight: 600;
        color: #1a1a1a;
        margin: 0;
        padding-bottom: 8px;
        border-bottom: 2px solid #e8edf5;
      }

      .inspector-field {
        display: flex;
        flex-direction: column;
        gap: 6px;

        label {
          font-size: 12px;
          font-weight: 500;
          color: #1a1a1a;
          text-transform: uppercase;
          letter-spacing: 0.3px;
        }

        input,
        select {
          height: 40px;
          padding: 8px 12px;
          border: 1px solid #e8edf5;
          border-radius: 6px;
          font-size: 14px;
          font-family: 'Inter', 'Roboto', sans-serif;
          background-color: #ffffff;
          color: #1a1a1a;
          transition: all 150ms cubic-bezier(0.4, 0, 0.2, 1);

          &:hover {
            border-color: #d0d0d0;
          }

          &:focus {
            outline: none;
            border: 2px solid #1f4e78;
            box-shadow: 0 0 0 3px rgba(31, 78, 120, 0.1);
          }

          &:disabled {
            background-color: #f5f7fa;
            color: #999;
            cursor: not-allowed;
          }
        }
      }

      .inspector-info {
        padding: 12px;
        background-color: #ffffff;
        border: 1px solid #e8edf5;
        border-radius: 6px;
        font-size: 13px;
        color: #1a1a1a;

        p {
          margin: 0 0 4px 0;

          &:last-child {
            margin-bottom: 0;
          }
        }

        .info-small {
          font-size: 11px;
          color: #999;
        }
      }

      .inspector-actions {
        display: flex;
        flex-direction: column;
        gap: 8px;
        margin-top: auto;
        padding-top: 12px;
        border-top: 1px solid #e8edf5;

        button {
          transition: all 150ms cubic-bezier(0.4, 0, 0.2, 1);

          mat-icon {
            font-size: 18px;
            width: 18px;
            height: 18px;
          }
        }
      }

      .inspector-empty {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 12px;
        flex: 1;
        color: #999;
        text-align: center;

        mat-icon {
          font-size: 48px;
          width: 48px;
          height: 48px;
          opacity: 0.5;
        }

        p {
          margin: 0;
          font-size: 14px;
        }
      }
    `,
  ],
})
export class FormulaPropertyInspectorComponent {
  @Input() node: FormulaNode | null = null;
  @Input() entities: EntityMetadata[] = [];
  @Input() operations: OperationConfig[] = [];

  @Output() propertyChanged = new EventEmitter<{
    nodeId: string;
    fieldName: string;
    value: any;
  }>();
  @Output() nodeDeleted = new EventEmitter<string>();
  @Output() nodeDuplicated = new EventEmitter<string>();

  getFieldsForEntity(entityId?: string): any[] {
    if (!entityId) return [];
    const entity = this.entities.find((e) => e.id === entityId);
    return entity?.fields || [];
  }

  getOperationConfig(): OperationConfig | undefined {
    if (!this.node || this.node.type !== 'operacion') return undefined;
    return this.operations.find((op) => op.id === this.node!.data.operacion);
  }

  onPropertyChange(fieldName: string, event: any): void {
    if (!this.node) return;

    const target = event.target as HTMLInputElement | HTMLSelectElement;
    const value = target.type === 'number' ? parseFloat(target.value) : target.value;

    this.propertyChanged.emit({
      nodeId: this.node.id,
      fieldName,
      value,
    });
  }

  onDuplicate(): void {
    if (this.node) {
      this.nodeDuplicated.emit(this.node.id);
    }
  }

  onDelete(): void {
    if (this.node) {
      this.nodeDeleted.emit(this.node.id);
    }
  }
}
