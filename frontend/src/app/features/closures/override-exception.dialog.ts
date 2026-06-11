import { Component, Inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { ClosureLine } from '../../models/dtos';

export interface OverrideData {
  line: ClosureLine;
  resultadoOriginal: number;
  razon?: string;
}

@Component({
  selector: 'app-override-exception',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule, MatButtonModule, MatDialogModule],
  template: `
    <div class="sig-override-dialog">
      <div class="sig-dialog-header">
        <h2 class="sig-dialog-title">
          <mat-icon>edit</mat-icon>
          Ajuste Manual de Cálculo
        </h2>
        <button mat-icon-button (click)="cerrar()" class="sig-close-btn">
          <mat-icon>close</mat-icon>
        </button>
      </div>

      <div class="sig-dialog-content">
        <!-- Info original -->
        <div class="sig-info-section">
          <h3>Línea Original</h3>
          <div class="sig-info-grid">
            <div class="sig-info-item">
              <span class="sig-label">Concepto</span>
              <span class="sig-value">{{ data.line.conceptNombre }}</span>
            </div>
            <div class="sig-info-item">
              <span class="sig-label">Empleado</span>
              <span class="sig-value">{{ data.line.userNombre || '—' }}</span>
            </div>
            <div class="sig-info-item">
              <span class="sig-label">Resultado Original</span>
              <span class="sig-value sig-mono">€ {{ data.resultadoOriginal | number:'1.0-0' }}</span>
            </div>
          </div>
        </div>

        <!-- Nuevo valor -->
        <div class="sig-input-section">
          <label class="sig-input-label">Nuevo Importe (€)</label>
          <input
            type="number"
            class="sig-input-field"
            [(ngModel)]="nuevoImporte"
            placeholder="Ingresa el nuevo importe"
          />
          <div class="sig-diff">
            Diferencia:
            <span [class]="difClase()">
              {{ diferencia() | number:'1.0-0' }} €
              ({{ ((diferencia() / data.resultadoOriginal * 100) | number:'1.0-0') }}%)
            </span>
          </div>
        </div>

        <!-- Razon -->
        <div class="sig-textarea-section">
          <label class="sig-input-label">Motivo del Ajuste (requerido)</label>
          <textarea
            class="sig-textarea"
            [(ngModel)]="razon"
            placeholder="Explica por qué se ajusta este valor..."
            rows="4"
          ></textarea>
          <div class="sig-char-count">{{ razon.length }}/500</div>
        </div>

        <!-- Auditoria -->
        <div class="sig-audit-section">
          <mat-icon>info</mat-icon>
          <span>Este cambio se registrará en el auditoría con tu usuario y timestamp</span>
        </div>
      </div>

      <div class="sig-dialog-footer">
        <button class="sig-btn-cancel" (click)="cerrar()">Cancelar</button>
        <button class="sig-btn-save" (click)="guardar()" [disabled]="!valido()">
          <mat-icon>check</mat-icon>
          Guardar Ajuste
        </button>
      </div>
    </div>
  `,
  styles: [`
    :host { display: block; }

    .sig-override-dialog {
      min-width: 500px;
      background: var(--sig-bg-card);
    }

    .sig-dialog-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 16px;
      border-bottom: 1px solid var(--sig-border);
    }

    .sig-dialog-title {
      margin: 0;
      font-size: 16px;
      font-weight: 600;
      display: flex;
      align-items: center;
      gap: 8px;
      mat-icon { color: #f59e0b; }
    }

    .sig-close-btn {
      color: var(--sig-text-muted) !important;
    }

    .sig-dialog-content {
      padding: 20px;
      display: flex;
      flex-direction: column;
      gap: 20px;
    }

    .sig-info-section, .sig-input-section, .sig-textarea-section {
      display: flex;
      flex-direction: column;
      gap: 10px;
    }

    .sig-info-section h3, .sig-input-label {
      font-size: 12px;
      font-weight: 700;
      letter-spacing: .08em;
      text-transform: uppercase;
      color: var(--sig-text-muted);
    }

    .sig-info-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
      gap: 12px;
    }

    .sig-info-item {
      display: flex;
      flex-direction: column;
      gap: 4px;
      padding: 10px;
      background: var(--sig-bg-card-alt);
      border-radius: 6px;
    }

    .sig-label {
      font-size: 10px;
      font-weight: 600;
      color: var(--sig-text-muted);
      text-transform: uppercase;
    }

    .sig-value {
      font-size: 13px;
      font-weight: 600;
      color: var(--sig-text-primary);
    }

    .sig-mono {
      font-family: 'Roboto Mono', monospace;
      color: #22c55e;
    }

    .sig-input-field {
      padding: 10px 12px;
      border: 1px solid var(--sig-border);
      border-radius: 6px;
      background: var(--sig-bg-app);
      color: var(--sig-text-primary);
      font-family: 'Roboto Mono', monospace;
      font-size: 14px;
      outline: none;
      &:focus { border-color: #f59e0b; }
    }

    .sig-diff {
      font-size: 12px;
      color: var(--sig-text-muted);
      padding: 8px 10px;
      background: var(--sig-bg-card-alt);
      border-radius: 4px;
      display: flex;
      align-items: center;
      gap: 8px;
      span {
        font-weight: 600;
        font-family: 'Roboto Mono', monospace;
        &.positive { color: #ef4444; }
        &.negative { color: #22c55e; }
      }
    }

    .sig-textarea {
      padding: 10px 12px;
      border: 1px solid var(--sig-border);
      border-radius: 6px;
      background: var(--sig-bg-app);
      color: var(--sig-text-primary);
      font-family: inherit;
      font-size: 13px;
      resize: vertical;
      outline: none;
      &:focus { border-color: #f59e0b; }
    }

    .sig-char-count {
      text-align: right;
      font-size: 11px;
      color: var(--sig-text-muted);
    }

    .sig-audit-section {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 10px 12px;
      background: rgba(245,158,11,.08);
      border: 1px solid rgba(245,158,11,.2);
      border-radius: 6px;
      font-size: 12px;
      color: var(--sig-text-primary);
      mat-icon { font-size: 16px; color: #f59e0b; }
    }

    .sig-dialog-footer {
      display: flex;
      gap: 10px;
      padding: 16px;
      border-top: 1px solid var(--sig-border);
      justify-content: flex-end;
    }

    .sig-btn-cancel {
      padding: 8px 16px;
      border-radius: 6px;
      border: 1px solid var(--sig-border);
      background: transparent;
      color: var(--sig-text-secondary);
      font-size: 13px;
      font-weight: 600;
      cursor: pointer;
      &:hover { background: var(--sig-bg-card-alt); }
    }

    .sig-btn-save {
      padding: 8px 16px;
      border-radius: 6px;
      border: none;
      background: #f59e0b;
      color: white;
      font-size: 13px;
      font-weight: 600;
      cursor: pointer;
      display: flex;
      align-items: center;
      gap: 6px;
      &:hover { opacity: 0.9; }
      &:disabled { opacity: 0.5; cursor: not-allowed; }
    }
  `]
})
export class OverrideExceptionDialog {
  protected nuevoImporte: number = 0;
  protected razon: string = '';

  protected diferencia = () => this.nuevoImporte - this.data.resultadoOriginal;
  protected difClase = () => this.diferencia() > 0 ? 'positive' : 'negative';
  protected valido = () => this.nuevoImporte > 0 && this.razon.length >= 10;

  constructor(
    public dialogRef: MatDialogRef<OverrideExceptionDialog>,
    @Inject(MAT_DIALOG_DATA) public data: OverrideData
  ) {
    this.nuevoImporte = data.resultadoOriginal;
    this.razon = data.razon || '';
  }

  protected cerrar() {
    this.dialogRef.close();
  }

  protected guardar() {
    if (!this.valido()) return;
    this.dialogRef.close({
      nuevoImporte: this.nuevoImporte,
      razon: this.razon,
      diferencia: this.diferencia()
    });
  }
}
