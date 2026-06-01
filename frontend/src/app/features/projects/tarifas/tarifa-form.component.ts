import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatIconModule } from '@angular/material/icon';
import { formatDate } from '@angular/common';
import { TarifasService } from '../../../core/api/tarifas.service';
import { NotifyService } from '../../../core/notify.service';
import { TarifaProyectoDto } from '../../../models/dtos';

interface DialogData {
  projectId: number;
  tarifa?: TarifaProyectoDto;
  mode: 'create' | 'edit';
}

@Component({
  selector: 'app-tarifa-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatDialogModule, MatDatepickerModule, MatNativeDateModule, MatIconModule],
  template: `
    <h2 mat-dialog-title>{{ data.mode === 'create' ? 'Nueva Tarifa' : 'Editar Tarifa' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form-container">
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Nombre *</mat-label>
          <input matInput formControlName="nombre" data-testid="input-nombre" />
          @if (form.get('nombre')?.hasError('required')) {
            <mat-error>Requerido</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Valor *</mat-label>
          <input matInput type="number" step="0.01" formControlName="valor" data-testid="input-valor" />
          @if (form.get('valor')?.hasError('required')) {
            <mat-error>Requerido</mat-error>
          }
          @if (form.get('valor')?.hasError('min')) {
            <mat-error>Debe ser mayor a 0</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Unidad</mat-label>
          <input matInput formControlName="unidad" placeholder="p.ej., hora, día, km" data-testid="input-unidad" />
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Fecha Desde *</mat-label>
          <input matInput [matDatepicker]="pickerDesde" formControlName="fechaDesde" data-testid="input-fechaDesde" />
          <mat-datepicker-toggle matSuffix [for]="pickerDesde"></mat-datepicker-toggle>
          <mat-datepicker #pickerDesde></mat-datepicker>
          @if (form.get('fechaDesde')?.hasError('required')) {
            <mat-error>Requerido</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Fecha Hasta</mat-label>
          <input matInput [matDatepicker]="pickerHasta" formControlName="fechaHasta" data-testid="input-fechaHasta" />
          <mat-datepicker-toggle matSuffix [for]="pickerHasta"></mat-datepicker-toggle>
          <mat-datepicker #pickerHasta></mat-datepicker>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="dialogRef.close()" data-testid="btn-cancel">Cancelar</button>
      <button mat-flat-button color="primary" (click)="onSave()" [disabled]="!form.valid || saving()" data-testid="btn-save">
        @if (saving()) {
          <mat-icon matIconSuffix class="spinner">hourglass_empty</mat-icon>
        }
        Guardar
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .form-container { display: flex; flex-direction: column; gap: 16px; padding: 16px 0; }
    .full-width { width: 100%; }
    .spinner { animation: spin 2s linear infinite; }
    @keyframes spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }
  `],
})
export class TarifaFormComponent implements OnInit {
  protected readonly dialogRef = inject(MatDialogRef<TarifaFormComponent>);
  protected readonly data: DialogData = inject(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);
  private readonly tarifasSvc = inject(TarifasService);
  private readonly notify = inject(NotifyService);

  protected readonly form: FormGroup = this.fb.group({
    nombre: ['', Validators.required],
    valor: ['', [Validators.required, Validators.min(0.01)]],
    unidad: [''],
    fechaDesde: ['', Validators.required],
    fechaHasta: [''],
  });

  protected saving = inject((() => {
    const s = new (class {
      value = false;
    })();
    return () => s.value;
  })(), { optional: true });

  ngOnInit(): void {
    if (this.data.mode === 'edit' && this.data.tarifa) {
      const t = this.data.tarifa;
      this.form.patchValue({
        nombre: t.nombre,
        valor: t.valor,
        unidad: t.unidad,
        fechaDesde: this.parseDate(t.fechaDesde),
        fechaHasta: t.fechaHasta ? this.parseDate(t.fechaHasta) : null,
      });
    }
  }

  protected onSave(): void {
    if (!this.form.valid) return;

    const payload = {
      nombre: this.form.value.nombre,
      valor: this.form.value.valor,
      unidad: this.form.value.unidad || null,
      fechaDesde: formatDate(this.form.value.fechaDesde, 'yyyy-MM-dd', 'en-US'),
      fechaHasta: this.form.value.fechaHasta ? formatDate(this.form.value.fechaHasta, 'yyyy-MM-dd', 'en-US') : null,
    };

    const obs = this.data.mode === 'create'
      ? this.tarifasSvc.create(this.data.projectId, payload)
      : this.tarifasSvc.update(this.data.tarifa!.id, this.data.projectId, payload);

    obs.subscribe({
      next: () => {
        this.notify.success(this.data.mode === 'create' ? 'Tarifa creada' : 'Tarifa actualizada');
        this.dialogRef.close(true);
      },
      error: (err) => {
        this.notify.error(err?.error?.title ?? 'Error al guardar');
      },
    });
  }

  private parseDate(dateStr: string): Date {
    const [y, m, d] = dateStr.split('-').map(Number);
    return new Date(y, m - 1, d);
  }
}
