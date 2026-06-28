import { Component, Inject, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { ConceptService } from '../../core/api/concepts.service';
import { ClientService } from '../../core/api/clients.service';
import { ServiceService } from '../../core/api/services.service';
import { PeriodService } from '../../core/api/periods.service';
import { NotifyService } from '../../core/notify.service';
import { PresupuestoConceptoDto } from '../../models/dtos';
import type { TipoPresupuesto } from '../../models/enums';

@Component({
  selector: 'app-presupuestos-form-dialog',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatDialogModule, MatCardModule, MatButtonModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatIconModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ data.presupuesto ? 'Editar Presupuesto' : 'Nuevo Presupuesto' }}</h2>
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
          <mat-label>Período (opcional, dejar vacío para anual)</mat-label>
          <mat-select [(ngModel)]="form.periodId" name="periodId">
            <mat-option [value]="null">— Anual —</mat-option>
            @for (p of periods(); track p.id) {
              <mat-option [value]="p.id">{{ p.nombre }}</mat-option>
            }
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Tipo de concepto</mat-label>
          <mat-select [(ngModel)]="form.tipo" name="tipo" required>
            <mat-option value="INGRESOS">INGRESOS</mat-option>
            <mat-option value="COSTES">COSTES</mat-option>
            <mat-option value="VARIABLE">VARIABLE</mat-option>
            <mat-option value="FIJA">FIJA</mat-option>
          </mat-select>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Importe (€)</mat-label>
          <input matInput type="number" [(ngModel)]="form.importe" name="importe" step="0.01" min="0" required>
        </mat-form-field>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Descripción (opcional)</mat-label>
          <textarea matInput [(ngModel)]="form.descripcion" name="descripcion" rows="3"></textarea>
        </mat-form-field>

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
export class PresupuestosFormDialogComponent {
  private readonly conceptSvc = inject(ConceptService);
  private readonly clientSvc = inject(ClientService);
  private readonly serviceSvc = inject(ServiceService);
  private readonly periodSvc = inject(PeriodService);
  private readonly notify = inject(NotifyService);

  protected readonly saving = signal(false);
  protected readonly error = signal('');
  protected readonly clients = signal<any[]>([]);
  protected readonly services = signal<any[]>([]);
  protected readonly periods = signal<any[]>([]);

  protected form = {
    clientId: null as number | null,
    serviceId: null as number | null,
    periodId: null as number | null,
    tipo: 'COSTES' as TipoPresupuesto,
    importe: 0,
    descripcion: '',
  };

  constructor(
    public dialogRef: MatDialogRef<PresupuestosFormDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { conceptId: number; presupuesto: PresupuestoConceptoDto | null },
  ) {
    this.loadClients();
    this.loadServices();
    this.loadPeriods();
    if (data.presupuesto) {
      this.form = {
        clientId: data.presupuesto.clientId ?? null,
        serviceId: data.presupuesto.serviceId ?? null,
        periodId: data.presupuesto.periodId ?? null,
        tipo: data.presupuesto.tipo,
        importe: data.presupuesto.importe,
        descripcion: data.presupuesto.descripcion || '',
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

  private loadPeriods(): void {
    this.periodSvc.listPaginated(1, 1000).subscribe({
      next: (result: any) => this.periods.set(result.items || []),
      error: () => this.error.set('No se pudieron cargar los períodos'),
    });
  }

  protected save(): void {
    if (this.form.importe < 0) {
      this.error.set('El importe no puede ser negativo');
      return;
    }
    if (!this.form.tipo) {
      this.error.set('Debes seleccionar un tipo');
      return;
    }
    this.saving.set(true);
    const req = {
      clientId: this.form.clientId,
      serviceId: this.form.serviceId,
      periodId: this.form.periodId,
      tipo: this.form.tipo,
      importe: this.form.importe,
      descripcion: this.form.descripcion || null,
    };

    if (this.data.presupuesto) {
      this.conceptSvc.updatePresupuesto(this.data.conceptId, this.data.presupuesto.id, req).subscribe({
        next: () => this.dialogRef.close(true),
        error: (err) => {
          this.error.set(err?.error?.title ?? 'Error al actualizar');
          this.saving.set(false);
        },
      });
    } else {
      this.conceptSvc.createPresupuesto(this.data.conceptId, req).subscribe({
        next: () => this.dialogRef.close(true),
        error: (err) => {
          this.error.set(err?.error?.title ?? 'Error al crear');
          this.saving.set(false);
        },
      });
    }
  }
}
