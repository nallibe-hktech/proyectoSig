import { Component, inject } from '@angular/core';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export interface ConfirmDialogData {
  title: string;
  message: string;
  entityName?: string;
  dependencies?: { label: string; count: number }[];
  confirmLabel?: string;
  cancelLabel?: string;
  destructive?: boolean;
}

@Component({
  selector: 'sig-confirm-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MatIconModule],
  template: `
    <h2 mat-dialog-title data-testid="modal-confirmacion">{{ data.title }}</h2>
    <mat-dialog-content>
      <p>{{ data.message }}</p>
      @if (data.entityName) {
        <p style="font-weight: 600; padding: 12px 16px; background: var(--mat-sys-surface-variant); border-radius: 8px; margin: 12px 0;">
          "{{ data.entityName }}"
        </p>
      }
      @if (data.dependencies && data.dependencies.length > 0) {
        <p>Este registro tiene dependencias:</p>
        <ul style="margin: 8px 0 16px 8px;">
          @for (d of data.dependencies; track d.label) {
            <li>{{ d.count }} {{ d.label }}</li>
          }
        </ul>
      }
      @if (data.destructive) {
        <p style="color: var(--mat-sys-error); font-size: 13px;">
          <mat-icon style="vertical-align: middle; font-size: 16px; width: 16px; height: 16px;" aria-hidden="true">warning</mat-icon>
          Esta acción no se puede deshacer.
        </p>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button
        mat-stroked-button
        [mat-dialog-close]="false"
        data-testid="btn-cancelar-eliminar"
      >
        {{ data.cancelLabel ?? 'Cancelar' }}
      </button>
      <button
        mat-flat-button
        [color]="data.destructive ? 'warn' : 'primary'"
        [mat-dialog-close]="true"
        data-testid="btn-confirmar-eliminar"
      >
        {{ data.confirmLabel ?? (data.destructive ? 'Eliminar' : 'Confirmar') }}
      </button>
    </mat-dialog-actions>
  `,
})
export class ConfirmDialogComponent {
  protected readonly data: ConfirmDialogData = inject(MAT_DIALOG_DATA);
  protected readonly dialogRef = inject(MatDialogRef<ConfirmDialogComponent>);
}
