import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { ConceptService } from '../../core/api/concepts.service';
import { ConceptListItemDto, ClosureLineIncentivoRequest } from '../../models/dtos';
import { TipoConcepto } from '../../models/enums';

// Ola 2 (#3a): alta de una línea de incentivo manual en un cierre.
@Component({
  selector: 'app-incentivo-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatIconModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule],
  template: `
    <h2 mat-dialog-title style="display:flex;align-items:center;gap:8px;"><mat-icon>add_card</mat-icon> Añadir incentivo</h2>
    <mat-dialog-content>
      <form [formGroup]="form" novalidate style="display:flex;flex-direction:column;gap:8px;min-width:420px;">
        <mat-form-field>
          <mat-label>Concepto *</mat-label>
          <mat-select formControlName="conceptId" data-testid="select-incentivo-concepto">
            @for (c of conceptos(); track c.id) { <mat-option [value]="c.id">{{ c.nombre }} ({{ c.tipo }})</mat-option> }
          </mat-select>
        </mat-form-field>
        <mat-form-field>
          <mat-label>Tipo *</mat-label>
          <mat-select formControlName="tipo" data-testid="select-incentivo-tipo">
            <mat-option value="Pago">Pago</mat-option>
            <mat-option value="Factura">Factura</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field>
          <mat-label>Importe (€) *</mat-label>
          <input matInput type="number" formControlName="importe" data-testid="input-incentivo-importe" />
        </mat-form-field>
        <mat-form-field>
          <mat-label>Motivo *</mat-label>
          <textarea matInput rows="3" formControlName="motivo" data-testid="input-incentivo-motivo"></textarea>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-stroked-button (click)="cerrar()" data-testid="btn-incentivo-cancelar">Cancelar</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid" (click)="guardar()" data-testid="btn-incentivo-guardar">Añadir</button>
    </mat-dialog-actions>
  `,
})
export class IncentivoDialog implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly conceptSvc = inject(ConceptService);
  protected readonly conceptos = signal<ConceptListItemDto[]>([]);

  protected readonly form = this.fb.nonNullable.group({
    conceptId: [0, [Validators.required, Validators.min(1)]],
    tipo: ['Pago' as TipoConcepto, [Validators.required]],
    importe: [0, [Validators.required]],
    motivo: ['', [Validators.required, Validators.minLength(10)]],
  });

  constructor(public dialogRef: MatDialogRef<IncentivoDialog>) {}

  ngOnInit(): void {
    this.conceptSvc.list(1, 200).subscribe({
      next: (r) => this.conceptos.set(r.items ?? []),
      error: () => this.conceptos.set([]),
    });
  }

  protected cerrar(): void { this.dialogRef.close(); }

  protected guardar(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const req: ClosureLineIncentivoRequest = { conceptId: v.conceptId, tipo: v.tipo, importe: v.importe, motivo: v.motivo, userId: null };
    this.dialogRef.close(req);
  }
}
