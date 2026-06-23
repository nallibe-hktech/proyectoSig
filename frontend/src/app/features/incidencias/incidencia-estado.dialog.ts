import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { IncidenciaCambioEstadoRequest } from '../../models/dtos';
import { EstadoIncidencia } from '../../models/enums';

export interface IncidenciaEstadoData {
  estadoActual: EstadoIncidencia;
  estadoPropuesto?: EstadoIncidencia;   // para el atajo "Marcar resuelta"
  titulo?: string;
}

// Cambio de estado de una incidencia desde el panel de detalle (prototipo: "Actualizar estado" /
// "Marcar resuelta"). Registra una nota y el responsable en el histórico.
@Component({
  selector: 'app-incidencia-estado-dialog',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatIconModule, MatButtonModule, MatButtonToggleModule,
    MatDialogModule, MatFormFieldModule, MatInputModule,
  ],
  template: `
    <h2 mat-dialog-title style="display:flex;align-items:center;gap:8px;"><mat-icon>published_with_changes</mat-icon> Actualizar estado</h2>
    <mat-dialog-content>
      <form [formGroup]="form" novalidate style="display:flex;flex-direction:column;gap:12px;min-width:440px;">
        <div>
          <span class="inc-label">NUEVO ESTADO</span>
          <mat-button-toggle-group formControlName="estado" data-testid="est-estado">
            <mat-button-toggle value="Abierta">Pendiente</mat-button-toggle>
            <mat-button-toggle value="EnProceso">En proceso</mat-button-toggle>
            <mat-button-toggle value="Resuelta">Resuelta</mat-button-toggle>
          </mat-button-toggle-group>
        </div>
        <mat-form-field appearance="outline">
          <mat-label>Nota *</mat-label>
          <textarea matInput formControlName="nota" rows="2" maxlength="500"
            placeholder="Ej.: Reclamación enviada al cliente" data-testid="est-nota"></textarea>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Responsable</mat-label>
          <input matInput formControlName="responsable" maxlength="120" placeholder="Comercial, Contabilidad…" data-testid="est-responsable" />
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-stroked-button (click)="cerrar()" data-testid="btn-est-cancelar">Cancelar</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="guardar()" data-testid="btn-est-guardar">Guardar</button>
    </mat-dialog-actions>
  `,
  styles: [`
    .inc-label { display:block; font-size:12px; font-weight:600; letter-spacing:.06em; color:var(--mat-sys-on-surface-variant); margin-bottom:6px; }
  `],
})
export class IncidenciaEstadoDialog {
  private readonly fb = inject(FormBuilder);
  protected readonly data = inject<IncidenciaEstadoData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject(MatDialogRef<IncidenciaEstadoDialog>);

  protected readonly form = this.fb.nonNullable.group({
    estado: [(this.data.estadoPropuesto ?? this.data.estadoActual) as EstadoIncidencia, [Validators.required]],
    nota: ['', [Validators.required, Validators.maxLength(500)]],
    responsable: [''],
  });

  protected cerrar(): void { this.dialogRef.close(); }

  protected guardar(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const req: IncidenciaCambioEstadoRequest = {
      estado: v.estado,
      nota: v.nota.trim(),
      responsable: v.responsable.trim() || null,
    };
    this.dialogRef.close(req);
  }
}
