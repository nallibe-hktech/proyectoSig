import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { PresupuestosService } from '../../../core/api/presupuestos.service';
import { NotifyService } from '../../../core/notify.service';
import { PresupuestoServicioDto } from '../../../models/dtos';
import { TipoConcepto } from '../../../models/enums';

interface DialogData {
  serviceId: number;
  presupuesto?: PresupuestoServicioDto;
  mode: 'create' | 'edit';
}

@Component({
  selector: 'app-presupuesto-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatDialogModule, MatSelectModule, MatIconModule],
  template: `
    <h2 mat-dialog-title>{{ data.mode === 'create' ? 'Nuevo Presupuesto' : 'Editar Presupuesto' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form-container">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Tipo *</mat-label>
          <mat-select formControlName="tipo" data-testid="select-tipo">
            <mat-option value="Pago">Pago</mat-option>
            <mat-option value="Factura">Factura</mat-option>
          </mat-select>
          @if (form.get('tipo')?.hasError('required')) {
            <mat-error>Requerido</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Importe *</mat-label>
          <input matInput type="number" step="0.01" formControlName="importe" data-testid="input-importe" />
          @if (form.get('importe')?.hasError('required')) {
            <mat-error>Requerido</mat-error>
          }
          @if (form.get('importe')?.hasError('min')) {
            <mat-error>Debe ser mayor a 0</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>ID Período</mat-label>
          <input matInput type="number" formControlName="periodId" placeholder="Opcional" data-testid="input-periodId" />
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Descripción</mat-label>
          <textarea matInput formControlName="descripcion" rows="3" placeholder="p.ej., Presupuesto Q2 2024" data-testid="textarea-descripcion"></textarea>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="dialogRef.close()" data-testid="btn-cancel">Cancelar</button>
      <button mat-flat-button color="primary" (click)="onSave()" [disabled]="!form.valid" data-testid="btn-save">
        Guardar
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .form-container { display: flex; flex-direction: column; gap: 16px; padding: 16px 0; }
    .full-width { width: 100%; }
  `],
})
export class PresupuestoFormComponent implements OnInit {
  protected readonly dialogRef = inject(MatDialogRef<PresupuestoFormComponent>);
  protected readonly data: DialogData = inject(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);
  private readonly presupuestosSvc = inject(PresupuestosService);
  private readonly notify = inject(NotifyService);

  protected readonly form: FormGroup = this.fb.group({
    tipo: ['', Validators.required],
    importe: ['', [Validators.required, Validators.min(0.01)]],
    periodId: [null],
    descripcion: [''],
  });

  ngOnInit(): void {
    if (this.data.mode === 'edit' && this.data.presupuesto) {
      const p = this.data.presupuesto;
      this.form.patchValue({
        tipo: p.tipo,
        importe: p.importe,
        periodId: p.periodId,
        descripcion: p.descripcion,
      });
    }
  }

  protected onSave(): void {
    if (!this.form.valid) return;

    const payload = {
      tipo: this.form.value.tipo as TipoConcepto,
      importe: this.form.value.importe,
      periodId: this.form.value.periodId || null,
      descripcion: this.form.value.descripcion || null,
    };

    const obs = this.data.mode === 'create'
      ? this.presupuestosSvc.create(this.data.serviceId, payload)
      : this.presupuestosSvc.update(this.data.presupuesto!.id, this.data.serviceId, payload);

    obs.subscribe({
      next: () => {
        this.notify.success(this.data.mode === 'create' ? 'Presupuesto creado' : 'Presupuesto actualizado');
        this.dialogRef.close(true);
      },
      error: (err) => {
        this.notify.error(err?.error?.title ?? 'Error al guardar');
      },
    });
  }
}
