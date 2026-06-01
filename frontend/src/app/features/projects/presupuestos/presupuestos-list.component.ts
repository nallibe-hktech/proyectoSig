import { Component, inject, Input, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatMenuModule } from '@angular/material/menu';
import { MatChipsModule } from '@angular/material/chips';
import { PresupuestosService } from '../../../core/api/presupuestos.service';
import { NotifyService } from '../../../core/notify.service';
import { PresupuestoProyectoDto } from '../../../models/dtos';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog.component';
import { PresupuestoFormComponent } from './presupuesto-form.component';

@Component({
  selector: 'app-presupuestos-list',
  standalone: true,
  imports: [CommonModule, MatTableModule, MatButtonModule, MatIconModule, MatCardModule, MatDialogModule, MatProgressSpinnerModule, MatMenuModule, MatChipsModule],
  template: `
    <mat-card>
      <mat-card-header>
        <mat-card-title>Presupuestos</mat-card-title>
        <button mat-flat-button color="accent" (click)="onAdd()" class="ml-auto" data-testid="btn-add-presupuesto">
          <mat-icon>add</mat-icon> Nuevo Presupuesto
        </button>
      </mat-card-header>
      <mat-card-content>
        @if (loading()) {
          <div style="display: flex; justify-content: center; padding: 32px;">
            <mat-spinner diameter="40"></mat-spinner>
          </div>
        } @else if (presupuestos().length === 0) {
          <p style="text-align: center; color: var(--mat-sys-on-surface-variant);">No hay presupuestos registrados</p>
        } @else {
          <table mat-table [dataSource]="presupuestos()" class="full-width">
            <ng-container matColumnDef="tipo">
              <th mat-header-cell>Tipo</th>
              <td mat-cell *matCellDef="let element">
                <mat-chip [highlighted]="element.tipo === 'Factura'">
                  {{ element.tipo }}
                </mat-chip>
              </td>
            </ng-container>

            <ng-container matColumnDef="importe">
              <th mat-header-cell class="text-right">Importe</th>
              <td mat-cell *matCellDef="let element" class="text-right mono-num">{{ element.importe | number:'1.2-2' }}</td>
            </ng-container>

            <ng-container matColumnDef="periodId">
              <th mat-header-cell>Período</th>
              <td mat-cell *matCellDef="let element">{{ element.periodId ?? '—' }}</td>
            </ng-container>

            <ng-container matColumnDef="descripcion">
              <th mat-header-cell>Descripción</th>
              <td mat-cell *matCellDef="let element">{{ element.descripcion ?? '—' }}</td>
            </ng-container>

            <ng-container matColumnDef="acciones">
              <th mat-header-cell class="text-center" style="width: 100px;">Acciones</th>
              <td mat-cell *matCellDef="let element" class="text-center">
                <button mat-icon-button [matMenuTriggerFor]="menu" data-testid="btn-menu">
                  <mat-icon>more_vert</mat-icon>
                </button>
                <mat-menu #menu="matMenu">
                  <button mat-menu-item (click)="onEdit(element)" data-testid="btn-edit">
                    <mat-icon>edit</mat-icon> Editar
                  </button>
                  <button mat-menu-item (click)="onDelete(element)" color="warn" data-testid="btn-delete">
                    <mat-icon>delete</mat-icon> Eliminar
                  </button>
                </mat-menu>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let element; columns: displayedColumns;"></tr>
          </table>
        }
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    :host { display: block; }
    mat-card-header { display: flex; align-items: center; gap: 16px; }
    mat-card-title { flex: 1; margin: 0; }
    .full-width { width: 100%; }
    .text-right { text-align: right; }
    .text-center { text-align: center; }
    .mono-num { font-variant-numeric: tabular-nums; }
    .ml-auto { margin-left: auto; }
  `],
})
export class PresupuestosListComponent implements OnInit {
  @Input() projectId!: number;

  private readonly presupuestosSvc = inject(PresupuestosService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotifyService);

  protected readonly presupuestos = signal<PresupuestoProyectoDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly displayedColumns = ['tipo', 'importe', 'periodId', 'descripcion', 'acciones'];

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.presupuestosSvc.list(this.projectId).subscribe({
      next: (data) => {
        this.presupuestos.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.notify.error('No se pudo cargar los presupuestos');
        this.loading.set(false);
      },
    });
  }

  protected onAdd(): void {
    this.dialog.open(PresupuestoFormComponent, {
      data: { projectId: this.projectId, mode: 'create' },
      minWidth: 500,
    }).afterClosed().subscribe((ok) => {
      if (ok) this.load();
    });
  }

  protected onEdit(presupuesto: PresupuestoProyectoDto): void {
    this.dialog.open(PresupuestoFormComponent, {
      data: { projectId: this.projectId, presupuesto, mode: 'edit' },
      minWidth: 500,
    }).afterClosed().subscribe((ok) => {
      if (ok) this.load();
    });
  }

  protected onDelete(presupuesto: PresupuestoProyectoDto): void {
    const label = `${presupuesto.tipo} - ${presupuesto.importe}`;
    this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Eliminar Presupuesto', message: 'Acción irreversible.', entityName: label, destructive: true },
      minWidth: 480,
    }).afterClosed().subscribe((ok) => {
      if (!ok) return;
      this.presupuestosSvc.delete(presupuesto.id, this.projectId).subscribe({
        next: () => {
          this.notify.success('Presupuesto eliminado');
          this.load();
        },
        error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo eliminar'),
      });
    });
  }
}
