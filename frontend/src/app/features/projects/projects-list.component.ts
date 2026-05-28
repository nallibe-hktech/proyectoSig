import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatChipsModule } from '@angular/material/chips';
import { MatSelectModule } from '@angular/material/select';
import { ProjectService } from '../../core/api/projects.service';
import { ProjectListItemDto } from '../../models/dtos';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { NotifyService } from '../../core/notify.service';
import { exportCSV } from '../../core/api/api.helpers';

@Component({
  selector: 'app-projects-list',
  standalone: true,
  imports: [
    CommonModule, DatePipe, RouterLink, ReactiveFormsModule,
    MatCardModule, MatTableModule, MatButtonModule, MatIconModule, MatChipsModule,
    MatFormFieldModule, MatInputModule, MatPaginatorModule, MatDialogModule, MatSelectModule,
    BreadcrumbsComponent, SkeletonComponent, EmptyStateComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Proyectos' }]" />
      <div class="sig-page__header">
        <h1 class="sig-page__title" data-testid="page-title">&#x1F3D7;&#xFE0F;  Proyectos</h1>
        <div style="display: flex; align-items: center; gap: 12px;">
          <span class="sig-total-badge" data-testid="total-badge">{{ total() }}</span>
          <a mat-flat-button color="primary" routerLink="/projects/nuevo" data-testid="btn-nuevo"><mat-icon>add</mat-icon> Nuevo Proyecto</a>
        </div>
      </div>
      <mat-card>
        <mat-card-content>
          <div class="sig-table-toolbar">
            <mat-form-field appearance="outline" class="sig-search">
              <mat-icon matPrefix aria-hidden="true">search</mat-icon>
              <mat-label>Buscar proyecto...</mat-label>
              <input matInput [formControl]="search" data-testid="input-busqueda" />
            </mat-form-field>
            <mat-form-field appearance="outline" class="sig-filter-select">
              <mat-label>Cliente</mat-label>
              <mat-select data-testid="select-cliente">
                <mat-option value="">Todos</mat-option>
              </mat-select>
            </mat-form-field>
            <mat-form-field appearance="outline" class="sig-filter-select">
              <mat-label>Estado</mat-label>
              <mat-select data-testid="select-estado">
                <mat-option value="">Todos</mat-option>
                <mat-option value="Activo">Activo</mat-option>
                <mat-option value="Inactivo">Inactivo</mat-option>
              </mat-select>
            </mat-form-field>
            <button mat-flat-button class="sig-filtrar-btn" data-testid="btn-filtrar">Filtrar</button>
            <button mat-stroked-button (click)="onExportCSV()" data-testid="btn-exportar-csv" style="margin-left: auto;">
              <mat-icon>download</mat-icon> Exportar CSV
            </button>
          </div>
          @if (loading()) {
            <sig-skeleton [count]="5" />
          } @else if (items().length === 0) {
            <sig-empty-state icon="folder_open" title="No hay proyectos todav&iacute;a" description="Crea el primer proyecto."
              ctaLabel="Crear primer project" [hasFilter]="!!search.value" (ctaClick)="onEmptyCta()" />
          } @else {
            <table mat-table [dataSource]="items()" class="sig-table sig-table--dark-header" data-testid="tabla-projects">
              <ng-container matColumnDef="id"><th mat-header-cell *matHeaderCellDef>ID</th><td mat-cell *matCellDef="let row"><span class="sig-id-cell">{{ row.id }}</span></td></ng-container>
              <ng-container matColumnDef="nombre"><th mat-header-cell *matHeaderCellDef>PROYECTO</th><td mat-cell *matCellDef="let row"><span class="sig-project-name">{{ row.nombre }}</span></td></ng-container>
              <ng-container matColumnDef="cliente"><th mat-header-cell *matHeaderCellDef>CLIENTE</th><td mat-cell *matCellDef="let row">{{ row.clientNombre }}</td></ng-container>
              <ng-container matColumnDef="estado"><th mat-header-cell *matHeaderCellDef>ESTADO</th><td mat-cell *matCellDef="let row"><span class="sig-chip" [class.sig-chip--active]="row.estado === 'Activo'" [class.sig-chip--inactive]="row.estado === 'Inactivo'">{{ row.estado }}</span></td></ng-container>
              <ng-container matColumnDef="fechaAlta"><th mat-header-cell *matHeaderCellDef>FECHA ALTA</th><td mat-cell *matCellDef="let row" class="mono-num">{{ row.fechaAlta | date:'dd/MM/yyyy' }}</td></ng-container>
              <ng-container matColumnDef="acciones">
                <th mat-header-cell *matHeaderCellDef style="text-align: right;">ACCIONES</th>
                <td mat-cell *matCellDef="let row">
                  <div class="sig-table-actions">
                    <a mat-icon-button [routerLink]="['/projects', row.id]" [attr.data-testid]="'btn-ver-' + row.id" aria-label="Ver"><mat-icon>visibility</mat-icon></a>
                    <a mat-icon-button [routerLink]="['/projects', row.id, 'editar']" [attr.data-testid]="'btn-editar-' + row.id" aria-label="Editar"><mat-icon>edit</mat-icon></a>
                    <button mat-icon-button (click)="onDelete(row)" [attr.data-testid]="'btn-eliminar-' + row.id" aria-label="Eliminar"><mat-icon>delete</mat-icon></button>
                  </div>
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="cols"></tr>
              <tr mat-row *matRowDef="let row; columns: cols" data-testid="row-project"></tr>
            </table>
          }
          <mat-paginator [length]="total()" [pageSize]="pageSize()" [pageIndex]="page() - 1" [pageSizeOptions]="[10, 25, 50]" showFirstLastButtons (page)="onPage($event)" data-testid="paginator-projects" />
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .sig-table-toolbar { display: flex; gap: 8px; align-items: center; margin-bottom: 16px; flex-wrap: wrap; }
    .sig-search { flex: 1; min-width: 200px; max-width: 240px; }
    .sig-filter-select { width: 150px; }
    .sig-filtrar-btn { height: 40px; }
    .sig-total-badge {
      display: inline-flex; align-items: center; justify-content: center;
      min-width: 32px; height: 24px; padding: 0 10px;
      border-radius: 12px; background: #E3F2FD;
      font-size: 12px; font-weight: 700; color: #1565C0;
    }
    .sig-project-name { font-weight: 600; color: #1F4E78; }
    .sig-id-cell { font-weight: 700; color: #1F4E78; font-size: 12px; }
    .sig-chip {
      display: inline-block; padding: 2px 10px; border-radius: 10px;
      font-size: 11px; font-weight: 600;
    }
    .sig-chip--active { background: #E8F5E9; color: #2E7D32; }
    .sig-chip--inactive { background: #FFEBEE; color: #C62828; }

    :host ::ng-deep .sig-table--dark-header th.mat-header-cell {
      background: #1F4E78 !important; color: rgba(255,255,255,0.85) !important;
      font-size: 11px; font-weight: 700; letter-spacing: 0.5px;
    }
  `],
})
export class ProjectsListComponent implements OnInit {
  private readonly projectSvc = inject(ProjectService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotifyService);
  private readonly router = inject(Router);

  protected readonly items = signal<ProjectListItemDto[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(25);
  protected readonly loading = signal(true);
  protected readonly search = new FormControl<string>('', { nonNullable: true });
  protected readonly cols = ['id', 'nombre', 'cliente', 'estado', 'fechaAlta', 'acciones'];

  ngOnInit(): void {
    this.search.valueChanges.pipe(debounceTime(300), distinctUntilChanged()).subscribe(() => { this.page.set(1); this.load(); });
    this.load();
  }
  protected onPage(e: PageEvent): void { this.pageSize.set(e.pageSize); this.page.set(e.pageIndex + 1); this.load(); }
  protected onEmptyCta(): void { this.search.value ? this.search.setValue('') : this.router.navigate(['/projects/nuevo']); }
  protected onDelete(row: ProjectListItemDto): void {
    this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Eliminar Project', message: 'Esta acción es irreversible.', entityName: row.nombre, destructive: true }, minWidth: 480,
    }).afterClosed().subscribe((ok) => {
      if (!ok) return;
      this.projectSvc.delete(row.id).subscribe({ next: () => { this.notify.success('Project eliminado'); this.load(); }, error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo eliminar') });
    });
  }
  protected onExportCSV(): void { exportCSV('projects.csv', this.items().map((p) => ({ Id: p.id, Nombre: p.nombre, Cliente: p.clientNombre, Estado: p.estado, FechaAlta: p.fechaAlta }))); }
  private load(): void {
    this.loading.set(true);
    this.projectSvc.list(this.page(), this.pageSize(), null, this.search.value).subscribe({
      next: (r) => { this.items.set(r.items); this.total.set(r.total); this.loading.set(false); },
      error: () => { this.items.set([]); this.total.set(0); this.loading.set(false); },
    });
  }
}
