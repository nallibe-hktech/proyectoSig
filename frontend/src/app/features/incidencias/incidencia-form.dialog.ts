import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { ClientListItemDto, ClienteIncidenciaCreateRequest } from '../../models/dtos';
import { EstadoIncidencia } from '../../models/enums';

export interface IncidenciaFormData {
  clientes: ClientListItemDto[];
  clientIdPreseleccionado?: number | null;
}
export interface IncidenciaFormResult {
  clientId: number;
  req: ClienteIncidenciaCreateRequest;
}

// Alta manual de incidencia (prototipo "Nueva incidencia"). El cliente se elige aquí porque la pantalla
// es de 1er nivel (no estamos dentro de la ficha de un cliente).
@Component({
  selector: 'app-incidencia-form-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatIconModule, MatButtonModule, MatButtonToggleModule,
    MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule,
  ],
  template: `
    <h2 mat-dialog-title style="display:flex;align-items:center;gap:8px;"><mat-icon>report_problem</mat-icon> Nueva incidencia</h2>
    <mat-dialog-content>
      <p class="inc-note">
        <mat-icon>info</mat-icon>
        Entrada <strong>manual</strong> de comercial / contabilidad. No proviene de ningún sistema origen (Celero, A3…).
      </p>
      <form [formGroup]="form" novalidate class="inc-grid">
        <mat-form-field appearance="outline">
          <mat-label>Cliente *</mat-label>
          <mat-select formControlName="clientId" data-testid="inc-cliente">
            @for (c of data.clientes; track c.id) { <mat-option [value]="c.id">{{ c.nombre }}</mat-option> }
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Tipo de incidencia *</mat-label>
          <input matInput formControlName="tipo" maxlength="100" placeholder="Impago, Disputa, Operativa…" data-testid="inc-tipo" />
        </mat-form-field>
        <mat-form-field appearance="outline" class="inc-full">
          <mat-label>Descripción *</mat-label>
          <textarea matInput formControlName="descripcion" rows="3" maxlength="2000"
            placeholder="Explica la incidencia. Ej.: el cliente no abona la factura 2026-0481 (mayo)…" data-testid="inc-descripcion"></textarea>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Fecha de apertura *</mat-label>
          <input matInput type="date" formControlName="fechaApertura" data-testid="inc-fecha" />
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Origen / Responsable</mat-label>
          <input matInput formControlName="origen" maxlength="120" placeholder="Comercial, Contabilidad…" data-testid="inc-origen" />
        </mat-form-field>
        <div class="inc-full">
          <span class="inc-label">ESTADO INICIAL</span>
          <mat-button-toggle-group formControlName="estado" data-testid="inc-estado">
            <mat-button-toggle value="Abierta">Pendiente</mat-button-toggle>
            <mat-button-toggle value="EnProceso">En proceso</mat-button-toggle>
            <mat-button-toggle value="Resuelta">Resuelta</mat-button-toggle>
          </mat-button-toggle-group>
          <p class="inc-hint">El estado es editable cuando la incidencia evoluciona (p. ej., el cliente paga) y queda registrado en el histórico.</p>
        </div>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-stroked-button (click)="cerrar()" data-testid="btn-inc-cancelar">Cancelar</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="guardar()" data-testid="btn-inc-guardar">
        <mat-icon>check</mat-icon> Guardar incidencia
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .inc-note { display:flex; gap:8px; align-items:flex-start; background:rgba(245,158,11,.10); color:var(--mat-sys-on-surface);
      border:1px solid rgba(245,158,11,.35); border-radius:8px; padding:10px 12px; margin:0 0 12px; font-size:13px; }
    .inc-note mat-icon { color:#f59e0b; font-size:18px; width:18px; height:18px; }
    .inc-grid { display:grid; grid-template-columns:1fr 1fr; gap:0 16px; min-width:520px; }
    .inc-full { grid-column:1 / -1; }
    .inc-label { display:block; font-size:12px; font-weight:600; letter-spacing:.06em; color:var(--mat-sys-on-surface-variant); margin-bottom:6px; }
    .inc-hint { font-size:12px; color:var(--mat-sys-on-surface-variant); margin:8px 0 0; }
  `],
})
export class IncidenciaFormDialog {
  private readonly fb = inject(FormBuilder);
  protected readonly data = inject<IncidenciaFormData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<IncidenciaFormDialog>);

  protected readonly form = this.fb.nonNullable.group({
    clientId: [this.data.clientIdPreseleccionado ?? 0, [Validators.required, Validators.min(1)]],
    tipo: ['', [Validators.required, Validators.maxLength(100)]],
    descripcion: ['', [Validators.required, Validators.maxLength(2000)]],
    fechaApertura: [this.hoy(), [Validators.required]],
    origen: [''],
    estado: ['Abierta' as EstadoIncidencia, [Validators.required]],
  });

  private hoy(): string {
    return new Date().toISOString().slice(0, 10);
  }

  protected cerrar(): void { this.dialogRef.close(); }

  protected guardar(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const result: IncidenciaFormResult = {
      clientId: v.clientId,
      req: {
        tipo: v.tipo.trim(),
        descripcion: v.descripcion.trim(),
        estado: v.estado,
        origen: v.origen.trim() || null,
        fechaApertura: v.fechaApertura,
      },
    };
    this.dialogRef.close(result);
  }
}
