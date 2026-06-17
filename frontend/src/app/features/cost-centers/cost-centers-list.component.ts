import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { CostCenterService } from '../../core/api/catalogs.service';
import { CostCenterDto } from '../../models/dtos';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { NotifyService } from '../../core/notify.service';

@Component({
  selector: 'app-cost-centers-list',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, MatTableModule, MatButtonModule, MatIconModule, MatDialogModule, MatPaginatorModule, BreadcrumbsComponent, SkeletonComponent, EmptyStateComponent],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Cost Centers' }]" />
      <div class="sig-page__header">
        <h1 class="sig-page__title">Centros de Coste</h1>
        <a mat-flat-button color="primary" routerLink="/cost-centers/nuevo" data-testid="btn-nuevo"><mat-icon>add</mat-icon> Nuevo CECO</a>
      </div>
      <mat-card><mat-card-content>
        @if (loading()) { <sig-skeleton [count]="4" /> }
        @else if (items().length === 0) {
          <sig-empty-state icon="account_balance" title="No hay centros de coste" ctaLabel="Crear primero" (ctaClick)="router.navigate(['/cost-centers/nuevo'])" />
        } @else {
          <div>
            <table mat-table [dataSource]="items()" class="sig-table" data-testid="tabla-costcenters">
              <ng-container matColumnDef="codigo"><th mat-header-cell *matHeaderCellDef>Código</th><td mat-cell *matCellDef="let r" class="mono-num">{{ r.codigo }}</td></ng-container>
              <ng-container matColumnDef="nombre"><th mat-header-cell *matHeaderCellDef>Nombre</th><td mat-cell *matCellDef="let r">{{ r.nombre }}</td></ng-container>
              <ng-container matColumnDef="acciones"><th mat-header-cell *matHeaderCellDef style="text-align: right;">Acciones</th>
                <td mat-cell *matCellDef="let r">
                  <div class="sig-table-actions">
                    <a mat-icon-button [routerLink]="['/cost-centers', r.id, 'editar']" [attr.data-testid]="'btn-editar-' + r.id" aria-label="Editar"><mat-icon>edit</mat-icon></a>
                    <button mat-icon-button (click)="onDelete(r)" [attr.data-testid]="'btn-eliminar-' + r.id" aria-label="Eliminar"><mat-icon>delete</mat-icon></button>
                  </div>
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="['codigo', 'nombre', 'acciones']"></tr>
              <tr mat-row *matRowDef="let row; columns: ['codigo', 'nombre', 'acciones']" data-testid="row-cc"></tr>
            </table>
            <mat-paginator [length]="total()" [pageSize]="pageSize()" [pageIndex]="page() - 1" [pageSizeOptions]="[10, 25, 50, 100]" showFirstLastButtons (page)="onPageChange($event)"></mat-paginator>
          </div>
        }
      </mat-card-content></mat-card>
    </div>
  `,
})
export class CostCentersListComponent implements OnInit {
  private readonly ccSvc = inject(CostCenterService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotifyService);
  protected readonly router = inject(Router);

  protected readonly page = signal(1);
  protected readonly pageSize = signal(25);
  protected readonly total = signal(0);
  protected readonly items = signal<CostCenterDto[]>([]);
  protected readonly loading = signal(true);

  ngOnInit(): void { this.load(); }

  protected onPageChange(event: PageEvent): void {
    this.page.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    window.scrollTo({ top: 0, behavior: 'smooth' });
    this.load();
  }

  protected onDelete(row: CostCenterDto): void {
    this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Eliminar CECO', message: 'Acción irreversible.', entityName: `${row.codigo} - ${row.nombre}`, destructive: true }, minWidth: 480,
    }).afterClosed().subscribe((ok) => {
      if (!ok) return;
      this.ccSvc.delete(row.id).subscribe({
        next: () => { this.notify.success('Eliminado'); this.page.set(1); this.load(); },
        error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo eliminar'),
      });
    });
  }

  private load(): void {
    this.loading.set(true);
    this.ccSvc.listPaginated(this.page(), this.pageSize()).subscribe({
      next: (response) => {
        this.items.set(response.items || []);
        this.total.set(response.total || 0);
        this.loading.set(false);
      },
      error: () => { this.items.set([]); this.total.set(0); this.loading.set(false); },
    });
  }
}
