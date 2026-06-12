import { Component, inject, Input, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule, DatePipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatMenuModule } from '@angular/material/menu';
import { TarifasService } from '../../../core/api/tarifas.service';
import { NotifyService } from '../../../core/notify.service';
import { TarifaServicioDto } from '../../../models/dtos';
import { ConfirmDialogComponent } from '../../../shared/confirm-dialog.component';
import { TarifaFormComponent } from './tarifa-form.component';

@Component({
  selector: 'app-tarifas-list',
  standalone: true,
  imports: [CommonModule, DatePipe, MatTableModule, MatButtonModule, MatIconModule, MatCardModule, MatDialogModule, MatProgressSpinnerModule, MatMenuModule],
  template: `
    <mat-card>
      <mat-card-header>
        <mat-card-title>Tarifas</mat-card-title>
        <button mat-flat-button color="accent" (click)="onAdd()" class="ml-auto" data-testid="btn-add-tarifa">
          <mat-icon>add</mat-icon> Nueva Tarifa
        </button>
      </mat-card-header>
      <mat-card-content>
        @if (loading()) {
          <div style="display: flex; justify-content: center; padding: 32px;">
            <mat-spinner diameter="40"></mat-spinner>
          </div>
        } @else if (tarifas().length === 0) {
          <p style="text-align: center; color: var(--mat-sys-on-surface-variant);">No hay tarifas registradas</p>
        } @else {
          <table mat-table [dataSource]="tarifas()" class="full-width">
            <ng-container matColumnDef="nombre">
              <th mat-header-cell>Nombre</th>
              <td mat-cell *matCellDef="let element">{{ element.nombre }}</td>
            </ng-container>

            <ng-container matColumnDef="valor">
              <th mat-header-cell class="text-right">Valor</th>
              <td mat-cell *matCellDef="let element" class="text-right mono-num">{{ element.valor | number:'1.2-2' }}</td>
            </ng-container>

            <ng-container matColumnDef="unidad">
              <th mat-header-cell>Unidad</th>
              <td mat-cell *matCellDef="let element">{{ element.unidad ?? '—' }}</td>
            </ng-container>

            <ng-container matColumnDef="fechaDesde">
              <th mat-header-cell>Desde</th>
              <td mat-cell *matCellDef="let element">{{ element.fechaDesde | date:'dd/MM/yyyy' }}</td>
            </ng-container>

            <ng-container matColumnDef="fechaHasta">
              <th mat-header-cell>Hasta</th>
              <td mat-cell *matCellDef="let element">{{ element.fechaHasta ? (element.fechaHasta | date:'dd/MM/yyyy') : '—' }}</td>
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
export class TarifasListComponent implements OnInit {
  @Input() serviceId!: number;
  // Permite uso como ruta /services/:id/tarifas (withComponentInputBinding bindea :id -> id)
  @Input() set id(value: number | string) { this.serviceId = Number(value); }

  private readonly tarifasSvc = inject(TarifasService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotifyService);
  private readonly route = inject(ActivatedRoute);

  protected readonly tarifas = signal<TarifaServicioDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly displayedColumns = ['nombre', 'valor', 'unidad', 'fechaDesde', 'fechaHasta', 'acciones'];

  ngOnInit(): void {
    if (this.serviceId == null) {
      const param = this.route.snapshot.paramMap.get('id');
      if (param) this.serviceId = Number(param);
    }
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.tarifasSvc.list(this.serviceId).subscribe({
      next: (data) => {
        this.tarifas.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.notify.error('No se pudo cargar las tarifas');
        this.loading.set(false);
      },
    });
  }

  protected onAdd(): void {
    this.dialog.open(TarifaFormComponent, {
      data: { serviceId: this.serviceId, mode: 'create' },
      minWidth: 500,
    }).afterClosed().subscribe((ok) => {
      if (ok) this.load();
    });
  }

  protected onEdit(tarifa: TarifaServicioDto): void {
    this.dialog.open(TarifaFormComponent, {
      data: { serviceId: this.serviceId, tarifa, mode: 'edit' },
      minWidth: 500,
    }).afterClosed().subscribe((ok) => {
      if (ok) this.load();
    });
  }

  protected onDelete(tarifa: TarifaServicioDto): void {
    this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Eliminar Tarifa', message: 'Acción irreversible.', entityName: tarifa.nombre, destructive: true },
      minWidth: 480,
    }).afterClosed().subscribe((ok) => {
      if (!ok) return;
      this.tarifasSvc.delete(tarifa.id, this.serviceId).subscribe({
        next: () => {
          this.notify.success('Tarifa eliminada');
          this.load();
        },
        error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo eliminar'),
      });
    });
  }
}
