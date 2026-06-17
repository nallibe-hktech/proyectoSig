import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { PeriodService } from '../../core/api/periods.service';
import { PeriodDto } from '../../models/dtos';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { NotifyService } from '../../core/notify.service';

@Component({
  selector: 'app-periods-list',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink, MatCardModule, MatTableModule, MatButtonModule, MatIconModule, MatChipsModule, MatDialogModule, MatPaginatorModule, BreadcrumbsComponent, SkeletonComponent, EmptyStateComponent],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Periods' }]" />
      <div class="sig-page__header">
        <h1 class="sig-page__title">Periods</h1>
        <a mat-flat-button color="primary" routerLink="/periods/nuevo" data-testid="btn-nuevo"><mat-icon>add</mat-icon> Nuevo Period</a>
      </div>
      <mat-card><mat-card-content>
        @if (loading()) { <sig-skeleton [count]="5" /> }
        @else if (items().length === 0) {
          <sig-empty-state icon="calendar_month" title="No hay períodos" ctaLabel="Crear primer período" (ctaClick)="router.navigate(['/periods/nuevo'])" />
        } @else {
          <div>
            <table mat-table [dataSource]="items()" class="sig-table" data-testid="tabla-periods">
              <ng-container matColumnDef="nombre"><th mat-header-cell *matHeaderCellDef>Nombre</th><td mat-cell *matCellDef="let row">{{ row.nombre }}</td></ng-container>
              <ng-container matColumnDef="inicio"><th mat-header-cell *matHeaderCellDef>Desde</th><td mat-cell *matCellDef="let row" class="mono-num">{{ row.fechaInicio | date:'dd/MM/yyyy' }}</td></ng-container>
              <ng-container matColumnDef="fin"><th mat-header-cell *matHeaderCellDef>Hasta</th><td mat-cell *matCellDef="let row" class="mono-num">{{ row.fechaFin | date:'dd/MM/yyyy' }}</td></ng-container>
              <ng-container matColumnDef="estado"><th mat-header-cell *matHeaderCellDef>Estado</th><td mat-cell *matCellDef="let row">
                <span [class]="'sig-badge ' + estadoCls(row.estado)" data-testid="badge-estado">
                  <mat-icon style="font-size: 14px; width: 14px; height: 14px;" aria-hidden="true">{{ estadoIcon(row.estado) }}</mat-icon>
                  {{ row.estado }}
                </span>
              </td></ng-container>
              <ng-container matColumnDef="acciones"><th mat-header-cell *matHeaderCellDef style="text-align: right;">Acciones</th>
                <td mat-cell *matCellDef="let row">
                  <div class="sig-table-actions">
                    @if (row.estado === 'Abierto') {
                      <button mat-stroked-button (click)="onCerrar(row)" [attr.data-testid]="'btn-cerrar-' + row.id"><mat-icon>lock</mat-icon> Cerrar</button>
                    } @else if (row.estado === 'Cerrado') {
                      <button mat-stroked-button (click)="onReabrir(row)" [attr.data-testid]="'btn-reabrir-' + row.id"><mat-icon>lock_open</mat-icon> Reabrir</button>
                    }
                    <a mat-icon-button [routerLink]="['/periods', row.id, 'editar']" [attr.data-testid]="'btn-editar-' + row.id" aria-label="Editar"><mat-icon>edit</mat-icon></a>
                  </div>
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="['nombre', 'inicio', 'fin', 'estado', 'acciones']"></tr>
              <tr mat-row *matRowDef="let row; columns: ['nombre', 'inicio', 'fin', 'estado', 'acciones']" data-testid="row-period"></tr>
            </table>
            <mat-paginator [length]="total()" [pageSize]="pageSize()" [pageIndex]="page() - 1" [pageSizeOptions]="[10, 25, 50, 100]" showFirstLastButtons (page)="onPageChange($event)"></mat-paginator>
          </div>
        }
      </mat-card-content></mat-card>
    </div>
  `,
})
export class PeriodsListComponent implements OnInit {
  private readonly periodSvc = inject(PeriodService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotifyService);
  protected readonly router = inject(Router);

  protected readonly page = signal(1);
  protected readonly pageSize = signal(25);
  protected readonly total = signal(0);
  protected readonly items = signal<PeriodDto[]>([]);
  protected readonly loading = signal(true);

  ngOnInit(): void { this.load(); }

  protected onPageChange(event: PageEvent): void {
    this.page.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    window.scrollTo({ top: 0, behavior: 'smooth' });
    this.load();
  }

  protected estadoCls(e: string): string {
    return e === 'Abierto' ? 'sig-badge--approved'
         : e === 'Cerrado' ? 'sig-badge--closed'
         : 'sig-badge--rejected';
  }

  protected estadoIcon(e: string): string {
    return e === 'Abierto' ? 'lock_open' : e === 'Cerrado' ? 'lock' : 'block';
  }

  protected onCerrar(row: PeriodDto): void {
    this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Cerrar período', message: 'Una vez cerrado, no se podrán crear nuevos cierres.', entityName: row.nombre, confirmLabel: 'Cerrar período' },
      minWidth: 480,
    }).afterClosed().subscribe((ok) => {
      if (!ok) return;
      this.periodSvc.cerrar(row.id).subscribe({
        next: () => { this.notify.success('Período cerrado'); this.page.set(1); this.load(); },
        error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo cerrar'),
      });
    });
  }

  protected onReabrir(row: PeriodDto): void {
    this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Reabrir período', message: '¿Confirmas la reapertura?', entityName: row.nombre, confirmLabel: 'Reabrir' },
      minWidth: 480,
    }).afterClosed().subscribe((ok) => {
      if (!ok) return;
      this.periodSvc.reabrir(row.id).subscribe({
        next: () => { this.notify.success('Período reabierto'); this.page.set(1); this.load(); },
        error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo reabrir'),
      });
    });
  }

  private load(): void {
    this.loading.set(true);
    this.periodSvc.listPaginated(this.page(), this.pageSize()).subscribe({
      next: (response) => {
        this.items.set(response.items || []);
        this.total.set(response.total || 0);
        this.loading.set(false);
      },
      error: () => { this.items.set([]); this.total.set(0); this.loading.set(false); },
    });
  }
}
