import { Component, Inject, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatIconModule } from '@angular/material/icon';
import { ConceptService } from '../../core/api/concepts.service';
import { ClientService } from '../../core/api/clients.service';
import { ServiceService } from '../../core/api/services.service';
import { NotifyService } from '../../core/notify.service';
import { TarifaConceptoDto } from '../../models/dtos';

@Component({
  selector: 'app-tarifas-form-dialog',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatDialogModule, MatCardModule, MatButtonModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatDatepickerModule,
    MatNativeDateModule, MatIconModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ data.tarifa ? 'Editar Tarifa' : 'Nueva Tarifa' }}</h2>
    <mat-dialog-content>
      <form>
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Cliente (opcional, dejar vacío para global)</mat-label>
          <mat-select [(ngModel)]="form.clientId" name="clientId">
            <mat-option [value]="null">— Global —</mat-option>
            @for (c of clients(); track c.id) {
              <mat-option [value]="c.id">{{ c.nombre }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Servicio (opcional, dejar vacío para todos)</mat-label>
          <mat-select [(ngModel)]="form.serviceId" name="serviceId">
            <mat-option [value]="null">— Todos —</mat-option>
            @for (s of services(); track s.id) {
              <mat-option [value]="s.id">{{ s.nombre }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Valor (€)</mat-label>
          <input matInput type="number" [(ngModel)]="form.valor" name="valor" step="0.01" min="0" required>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Unidad (p.ej. visita, hora, km, día)</mat-label>
          <input matInput [(ngModel)]="form.unidad" name="unidad" placeholder="visita">
        </mat-form-field>

        <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 12px;">
          <mat-form-field appearance="outline">
            <mat-label>Vigente desde</mat-label>
            <input matInput [matDatepicker]="dpDesde" [(ngModel)]="form.fechaDesde" name="fechaDesde" required>
            <mat-datepicker-toggle matSuffix [for]="dpDesde"></mat-datepicker-toggle>
            <mat-datepicker #dpDesde></mat-datepicker>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Vigente hasta (opcional)</mat-label>
            <input matInput [matDatepicker]="dpHasta" [(ngModel)]="form.fechaHasta" name="fechaHasta">
            <mat-datepicker-toggle matSuffix [for]="dpHasta"></mat-datepicker-toggle>
            <mat-datepicker #dpHasta></mat-datepicker>
          </mat-form-field>
        </div>

        @if (error()) {
          <div style="color: var(--sig-warning); font-size: 14px; margin-top: 12px; padding: 8px; background: color-mix(in srgb, var(--sig-warning) 14%, transparent); border-radius: 4px;">
            <mat-icon style="font-size: 16px; width: 16px; height: 16px; vertical-align: middle;">error</mat-icon>
            {{ error() }}
          </div>
        }
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="dialogRef.close()">Cancelar</button>
      <button mat-flat-button color="primary" (click)="save()" [disabled]="saving()">
        <mat-icon>save</mat-icon> Guardar
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .full-width { width: 100%; margin-bottom: 12px; }
    mat-dialog-content { display: flex; flex-direction: column; }
  `],
})
export class TarifasFormDialogComponent {
  private readonly conceptSvc = inject(ConceptService);
  private readonly clientSvc = inject(ClientService);
  private readonly serviceSvc = inject(ServiceService);
  private readonly notify = inject(NotifyService);

  protected readonly saving = signal(false);
  protected readonly error = signal('');
  protected readonly clients = signal<any[]>([]);
  protected readonly services = signal<any[]>([]);

  protected form: any = {
    clientId: null as number | null,
    serviceId: null as number | null,
    valor: 0,
    unidad: '' as string | null,
    fechaDesde: new Date().toISOString().split('T')[0],
    fechaHasta: null as string | null,
  };

  constructor(
    public dialogRef: MatDialogRef<TarifasFormDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { conceptId: number; tarifa: TarifaConceptoDto | null },
  ) {
    this.loadClients();
    this.loadServices();
    if (data.tarifa) {
      this.form = {
        clientId: data.tarifa.clientId ?? null,
        serviceId: data.tarifa.serviceId ?? null,
        valor: data.tarifa.valor,
        unidad: data.tarifa.unidad ?? '',
        fechaDesde: data.tarifa.fechaDesde,
        fechaHasta: data.tarifa.fechaHasta ?? null,
      };
    }
  }

  private loadClients(): void {
    this.clientSvc.list(1, 1000).subscribe({
      next: (result: any) => this.clients.set(result.items || []),
      error: () => this.error.set('No se pudieron cargar los clientes'),
    });
  }

  private loadServices(): void {
    this.serviceSvc.list(1, 1000).subscribe({
      next: (result: any) => this.services.set(result.items || []),
      error: () => this.error.set('No se pudieron cargar los servicios'),
    });
  }

  protected save(): void {
    if (this.form.valor <= 0) {
      this.error.set('El valor debe ser mayor a 0');
      return;
    }
    this.saving.set(true);
    const req = {
      clientId: this.form.clientId,
      serviceId: this.form.serviceId,
      valor: this.form.valor,
      unidad: this.form.unidad,
      fechaDesde: this.form.fechaDesde,
      fechaHasta: this.form.fechaHasta,
    };

    if (this.data.tarifa) {
      this.conceptSvc.updateTarifa(this.data.conceptId, this.data.tarifa.id, req).subscribe({
        next: () => this.dialogRef.close(true),
        error: (err: any) => {
          this.error.set(err?.error?.title ?? 'Error al actualizar');
          this.saving.set(false);
        },
      });
    } else {
      this.conceptSvc.createTarifa(this.data.conceptId, req).subscribe({
        next: () => this.dialogRef.close(true),
        error: (err: any) => {
          this.error.set(err?.error?.title ?? 'Error al crear');
          this.saving.set(false);
        },
      });
    }
  }
}
