import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { DepartmentService } from '../../core/api/catalogs.service';
import { DepartmentDto } from '../../models/dtos';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { NotifyService } from '../../core/notify.service';

@Component({
  selector: 'app-departments-list',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, MatTableModule, MatButtonModule, MatIconModule, MatDialogModule, BreadcrumbsComponent, SkeletonComponent, EmptyStateComponent],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Departments' }]" />
      <div class="sig-page__header">
        <h1 class="sig-page__title">Departments</h1>
        <a mat-flat-button color="primary" routerLink="/departments/nuevo" data-testid="btn-nuevo"><mat-icon>add</mat-icon> Nuevo Department</a>
      </div>
      <mat-card><mat-card-content>
        @if (loading()) { <sig-skeleton [count]="3" /> }
        @else if (items().length === 0) {
          <sig-empty-state icon="corporate_fare" title="No hay departamentos" ctaLabel="Crear primero" (ctaClick)="router.navigate(['/departments/nuevo'])" />
        } @else {
          <table mat-table [dataSource]="items()" class="sig-table" data-testid="tabla-departments">
            <ng-container matColumnDef="nombre"><th mat-header-cell *matHeaderCellDef>Nombre</th><td mat-cell *matCellDef="let r">{{ r.nombre }}</td></ng-container>
            <ng-container matColumnDef="acciones"><th mat-header-cell *matHeaderCellDef style="text-align: right;">Acciones</th>
              <td mat-cell *matCellDef="let r">
                <div class="sig-table-actions">
                  <a mat-icon-button [routerLink]="['/departments', r.id, 'editar']" [attr.data-testid]="'btn-editar-' + r.id" aria-label="Editar"><mat-icon>edit</mat-icon></a>
                  <button mat-icon-button (click)="onDelete(r)" [attr.data-testid]="'btn-eliminar-' + r.id" aria-label="Eliminar"><mat-icon>delete</mat-icon></button>
                </div>
              </td>
            </ng-container>
            <tr mat-header-row *matHeaderRowDef="['nombre', 'acciones']"></tr>
            <tr mat-row *matRowDef="let row; columns: ['nombre', 'acciones']" data-testid="row-dept"></tr>
          </table>
        }
      </mat-card-content></mat-card>
    </div>
  `,
})
export class DepartmentsListComponent implements OnInit {
  private readonly deptSvc = inject(DepartmentService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotifyService);
  protected readonly router = inject(Router);

  protected readonly items = signal<DepartmentDto[]>([]);
  protected readonly loading = signal(true);

  ngOnInit(): void { this.load(); }

  protected onDelete(row: DepartmentDto): void {
    this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Eliminar Department', message: 'Acción irreversible.', entityName: row.nombre, destructive: true }, minWidth: 480,
    }).afterClosed().subscribe((ok) => {
      if (!ok) return;
      this.deptSvc.delete(row.id).subscribe({
        next: () => { this.notify.success('Eliminado'); this.load(); },
        error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo eliminar'),
      });
    });
  }
  private load(): void {
    this.loading.set(true);
    this.deptSvc.list().subscribe({
      next: (ds) => { this.items.set(ds); this.loading.set(false); },
      error: () => { this.items.set([]); this.loading.set(false); },
    });
  }
}
