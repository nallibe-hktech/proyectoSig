import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';

/**
 * Componente Toolbar: barra superior con acciones del editor
 * Controles: Clear, Load JSON, Export, Undo, Redo, Validation Status
 */
@Component({
  selector: 'app-formula-canvas-toolbar',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, MatDividerModule, MatTooltipModule],
  template: `
    <div class="toolbar" data-testid="formula-toolbar">
      <div class="toolbar-left">
        <button
          mat-icon-button
          (click)="onClear()"
          matTooltip="Limpiar canvas (Ctrl+Shift+D)"
          data-testid="formula-toolbar-clear"
        >
          <mat-icon>delete_sweep</mat-icon>
        </button>

        <button
          mat-icon-button
          (click)="onLoadJson()"
          matTooltip="Importar desde JSON"
          data-testid="formula-toolbar-load"
        >
          <mat-icon>upload_file</mat-icon>
        </button>

        <button
          mat-icon-button
          (click)="onExport()"
          matTooltip="Descargar JSON"
          data-testid="formula-toolbar-export"
        >
          <mat-icon>download</mat-icon>
        </button>

        <mat-divider [vertical]="true"></mat-divider>

        <button
          mat-icon-button
          (click)="onUndo()"
          [disabled]="!canUndo"
          matTooltip="Deshacer (Ctrl+Z)"
          data-testid="formula-toolbar-undo"
        >
          <mat-icon>undo</mat-icon>
        </button>

        <button
          mat-icon-button
          (click)="onRedo()"
          [disabled]="!canRedo"
          matTooltip="Rehacer (Ctrl+Y)"
          data-testid="formula-toolbar-redo"
        >
          <mat-icon>redo</mat-icon>
        </button>
      </div>

      <div class="toolbar-right">
        <span
          [class.valid]="isFormulaValid"
          [class.invalid]="!isFormulaValid"
          data-testid="formula-toolbar-status"
          [attr.aria-label]="isFormulaValid ? 'Fórmula válida' : 'Fórmula inválida'"
        >
          <mat-icon>{{ isFormulaValid ? 'check_circle' : 'error' }}</mat-icon>
          {{ isFormulaValid ? 'Fórmula válida' : 'Fórmula inválida' }}
        </span>
      </div>
    </div>
  `,
  styles: [
    `
      .toolbar {
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 16px;
        background-color: #ffffff;
        border-bottom: 1px solid #e8edf5;
        height: 60px;
        flex-shrink: 0;
        gap: 16px;
      }

      .toolbar-left {
        display: flex;
        align-items: center;
        gap: 8px;

        mat-divider {
          height: 24px;
        }

        button {
          transition: all 150ms cubic-bezier(0.4, 0, 0.2, 1);

          &:hover:not(:disabled) {
            background-color: #e8edf5;
          }

          &:disabled {
            opacity: 0.5;
            cursor: not-allowed;
          }

          mat-icon {
            font-size: 20px;
            width: 20px;
            height: 20px;
          }
        }
      }

      .toolbar-right {
        display: flex;
        align-items: center;
        gap: 12px;
        margin-left: auto;

        span {
          display: flex;
          align-items: center;
          gap: 8px;
          padding: 8px 12px;
          border-radius: 6px;
          font-size: 13px;
          font-weight: 500;
          transition: all 150ms cubic-bezier(0.4, 0, 0.2, 1);

          mat-icon {
            font-size: 18px;
            width: 18px;
            height: 18px;
          }

          &.valid {
            color: #70ad47;
            background-color: #e8f5e9;
          }

          &.invalid {
            color: #d32f2f;
            background-color: #ffebee;
          }
        }
      }
    `,
  ],
})
export class FormulaCanvasToolbarComponent {
  @Input() canUndo = false;
  @Input() canRedo = false;
  @Input() isFormulaValid = false;

  @Output() clearClicked = new EventEmitter<void>();
  @Output() loadJsonClicked = new EventEmitter<void>();
  @Output() exportClicked = new EventEmitter<void>();
  @Output() undoClicked = new EventEmitter<void>();
  @Output() redoClicked = new EventEmitter<void>();

  onClear(): void {
    this.clearClicked.emit();
  }

  onLoadJson(): void {
    this.loadJsonClicked.emit();
  }

  onExport(): void {
    this.exportClicked.emit();
  }

  onUndo(): void {
    this.undoClicked.emit();
  }

  onRedo(): void {
    this.redoClicked.emit();
  }
}
