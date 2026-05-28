import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

@Component({
  selector: 'sig-reject-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  template: `
    <h2 mat-dialog-title data-testid="modal-rechazo">Rechazar cierre</h2>
    <mat-dialog-content>
      <p style="margin: 0 0 12px;">Indica el motivo del rechazo. El cierre volverá al paso anterior.</p>
      <form [formGroup]="form">
        <mat-form-field appearance="outline" style="width: 100%;">
          <mat-label>Motivo del rechazo *</mat-label>
          <textarea matInput formControlName="motivo" rows="4" data-testid="input-motivo"></textarea>
          @if (form.controls.motivo.touched && form.controls.motivo.hasError('required')) {
            <mat-error>El motivo es obligatorio</mat-error>
          }
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-stroked-button (click)="dialogRef.close(null)" data-testid="btn-cancelar-rechazo">Cancelar</button>
      <button mat-flat-button color="warn" (click)="confirm()" [disabled]="form.invalid" data-testid="btn-confirmar-rechazo">Rechazar</button>
    </mat-dialog-actions>
  `,
})
export class RejectDialogComponent {
  protected readonly dialogRef = inject(MatDialogRef<RejectDialogComponent>);
  private readonly fb = inject(FormBuilder);
  protected readonly form = this.fb.nonNullable.group({
    motivo: ['', [Validators.required, Validators.minLength(5)]],
  });
  protected confirm(): void {
    if (this.form.valid) this.dialogRef.close(this.form.getRawValue().motivo);
  }
}
