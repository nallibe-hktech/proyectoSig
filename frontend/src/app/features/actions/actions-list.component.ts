import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatChipsModule } from '@angular/material/chips';
import { ActionService } from '../../core/api/actions.service';
import { ActionListItemDto } from '../../models/dtos';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { NotifyService } from '../../core/notify.service';
import { exportCSV } from '../../core/api/api.helpers';

@Component({
  selector: 'app-actions-list',
  standalone: true,
  imports: [
    CommonModule, RouterLink, ReactiveFormsModule,
    MatCardModule, MatTableModule, MatButtonModule, MatIconModule, MatChipsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatPaginatorModule, MatDialogModule,
    BreadcrumbsComponent, SkeletonComponent, EmptyStateComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Acciones' }]" />
      <div class="sig-page__header">
        <h1 class="sig-page__title">Acciones</h1>
        <a mat-flat-button color="primary" routerLink="/actions/nuevo" data-testid="btn-nuevo"><mat-icon>add</mat-icon> Nueva Acción</a>
      </div>
      <mat-card>
        <mat-card-content>
          <div class="sig-table-toolbar">
            <mat-form-field appearance="outline" class="sig-search">
              <mat-icon matPrefix aria-hidden="true">search</mat-icon>
              <mat-label>Buscar...</mat-label>
              <input matInput [formControl]="search" data-testid="input-busqueda" />
            </mat-form-field>
            <mat-form-field appearance="outline" class="sig-filter-select">
              <mat-label>Proyecto</mat-label>
              <mat-select [formControl]="projectFilter" data-testid="filter-proyecto">
                <mat-option [value]="null">Todos</mat-option>
              </mat-select>
            </mat-form-field>
            <mat-form-field appearance="outline" class="sig-filter-select">
              <mat-label>Estado</mat-label>
              <mat-select [formControl]="estadoFilter" data-testid="filter-estado">
                <mat-option [value]="null">Todos</mat-option>
                <mat-option value="Activa">Activa</mat-option>
                <mat-option value="Inactiva">Inactiva</mat-option>
              </mat-select>
            </mat-form-field>
            <button mat-stroked-button (click)="onExportCSV()" data-testid="btn-exportar-csv"><mat-icon>download</mat-icon> Exportar CSV</button>
            <span class="sig-total-badge" data-testid="total-badge">{{ total() }} acciones</span>
          </div>
          @if (loading()) { <sig-skeleton [count]="5" /> }
          @else if (items().length === 0) {
            <sig-empty-state icon="task_alt" title="No hay acciones todavía" ctaLabel="Crear primera acción" [hasFilter]="!!search.value" (ctaClick)="onEmptyCta()" />
          } @else {
            <table mat-table [dataSource]="items()" class="sig-table sig-table-dark-header" data-testid="tabla-actions">
              <ng-container matColumnDef="id"><th mat-header-cell *matHeaderCellDef>ID</th><td mat-cell *matCellDef="let row" class="mono-num">{{ row.id }}</td></ng-container>
              <ng-container matColumnDef="nombre"><th mat-header-cell *matHeaderCellDef>Acción</th><td mat-cell *matCellDef="let row">{{ row.nombre }}</td></ng-container>
              <ng-container matColumnDef="proyecto"><th mat-header-cell *matHeaderCellDef>Proyecto</th><td mat-cell *matCellDef="let row">{{ row.projectNombre }}</td></ng-container>
              <ng-container matColumnDef="estado"><th mat-header-cell *matHeaderCellDef>Estado</th>
                <td mat-cell *matCellDef="let row"><span [class]="'sig-badge sig-badge--' + (row.estado === 'Activa' ? 'approved' : 'closed')" data-testid="badge-estado">{{ row.estado }}</span></td>
              </ng-container>
              <ng-container matColumnDef="acciones">
                <th mat-header-cell *matHeaderCellDef style="text-align: right;">Acciones</th>
                <td mat-cell *matCellDef="let row">
                  <div class="sig-table-actions">
                    <a mat-icon-button [routerLink]="['/actions', row.id]" [attr.data-testid]="'btn-ver-' + row.id" aria-label="Ver"><mat-icon>visibility</mat-icon></a>
                    <a mat-icon-button [routerLink]="['/actions', row.id, 'editar']" [attr.data-testid]="'btn-editar-' + row.id" aria-label="Editar"><mat-icon>edit</mat-icon></a>
                    <button mat-icon-button (click)="onDelete(row)" [attr.data-testid]="'btn-eliminar-' + row.id" aria-label="Eliminar"><mat-icon>delete</mat-icon></button>
                  </div>
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="cols"></tr>
              <tr mat-row *matRowDef="let row; columns: cols" data-testid="row-action"></tr>
            </table>
          }
          <mat-paginator [length]="total()" [pageSize]="pageSize()" [pageIndex]="page() - 1" [pageSizeOptions]="[10, 25, 50]" showFirstLastButtons (page)="onPage($event)" data-testid="paginator-actions" />
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`.sig-table-toolbar { display: flex; gap: 12px; align-items: center; margin-bottom: 16px; } .sig-search { flex: 1; max-width: 320px; } .sig-filter-select { width: 180px; } .sig-total-badge { font-size: 13px; color: var(--sig-text-muted); margin-left: auto; }`],
})
export class ActionsListComponent implements OnInit {
  private readonly actionSvc = inject(ActionService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotifyService);
  private readonly router = inject(Router);

  protected readonly items = signal<ActionListItemDto[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(25);
  protected readonly loading = signal(true);
  protected readonly search = new FormControl<string>('', { nonNullable: true });
  protected readonly projectFilter = new FormControl<number | null>(null);
  protected readonly estadoFilter = new FormControl<string | null>(null);
  protected readonly cols = ['id', 'nombre', 'proyecto', 'estado', 'acciones'];

  ngOnInit(): void {
    this.search.valueChanges.pipe(debounceTime(300), distinctUntilChanged()).subscribe(() => { this.page.set(1); this.load(); });
    this.projectFilter.valueChanges.subscribe(() => { this.page.set(1); this.load(); });
    this.estadoFilter.valueChanges.subscribe(() => { this.page.set(1); this.load(); });
    this.load();
  }
  protected onPage(e: PageEvent): void { this.pageSize.set(e.pageSize); this.page.set(e.pageIndex + 1); this.load(); }
  protected onEmptyCta(): void { this.search.value ? this.search.setValue('') : this.router.navigate(['/actions/nuevo']); }
  protected onDelete(row: ActionListItemDto): void {
    this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Eliminar Acción', message: 'Acción irreversible.', entityName: row.nombre, destructive: true }, minWidth: 480,
    }).afterClosed().subscribe((ok) => {
      if (!ok) return;
      this.actionSvc.delete(row.id).subscribe({ next: () => { this.notify.success('Acción eliminada'); this.load(); }, error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo eliminar') });
    });
  }
  protected onExportCSV(): void { exportCSV('actions.csv', this.items().map((a) => ({ Id: a.id, Nombre: a.nombre, Proyecto: a.projectNombre, Estado: a.estado }))); }
  private load(): void {
    this.loading.set(true);
    this.actionSvc.list(this.page(), this.pageSize(), this.projectFilter.value, this.search.value).subscribe({
      next: (r) => { this.items.set(r.items); this.total.set(r.total); this.loading.set(false); },
      error: () => { this.items.set([]); this.total.set(0); this.loading.set(false); },
    });
  }
}
