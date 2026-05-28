import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { RoleService } from '../../core/api/catalogs.service';
import { RoleDto } from '../../models/dtos';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';

@Component({
  selector: 'app-roles-list',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatTableModule, MatIconModule, BreadcrumbsComponent, SkeletonComponent],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Roles' }]" />
      <div class="sig-page__header"><h1 class="sig-page__title">Roles del sistema</h1></div>
      <p style="color: var(--mat-sys-on-surface-variant); margin-bottom: 16px;">
        Los 7 roles están definidos por el sistema (Administrator, Direction, Fico, Backoffice, ProjectManager, Auditor, Reader). No son editables.
      </p>
      <mat-card><mat-card-content>
        @if (loading()) { <sig-skeleton [count]="5" /> }
        @else {
          <table mat-table [dataSource]="items()" class="sig-table" data-testid="tabla-roles">
            <ng-container matColumnDef="nombre"><th mat-header-cell *matHeaderCellDef>Rol</th><td mat-cell *matCellDef="let r">{{ r.nombre }}</td></ng-container>
            <ng-container matColumnDef="desc"><th mat-header-cell *matHeaderCellDef>Descripción</th><td mat-cell *matCellDef="let r">{{ r.descripcion ?? '—' }}</td></ng-container>
            <tr mat-header-row *matHeaderRowDef="['nombre', 'desc']"></tr>
            <tr mat-row *matRowDef="let row; columns: ['nombre', 'desc']" data-testid="row-role"></tr>
          </table>
        }
      </mat-card-content></mat-card>
    </div>
  `,
})
export class RolesListComponent implements OnInit {
  private readonly roleSvc = inject(RoleService);
  protected readonly items = signal<RoleDto[]>([]);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    this.roleSvc.list().subscribe({
      next: (rs) => { this.items.set(rs); this.loading.set(false); },
      error: () => { this.items.set([]); this.loading.set(false); },
    });
  }
}
