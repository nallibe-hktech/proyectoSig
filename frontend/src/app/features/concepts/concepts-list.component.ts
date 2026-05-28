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
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ConceptService } from '../../core/api/concepts.service';
import { ConceptListItemDto } from '../../models/dtos';
import { TipoConcepto } from '../../models/enums';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { NotifyService } from '../../core/notify.service';
import { exportCSV } from '../../core/api/api.helpers';

@Component({
  selector: 'app-concepts-list',
  standalone: true,
  imports: [
    CommonModule, DatePipe, RouterLink, ReactiveFormsModule,
    MatCardModule, MatTableModule, MatButtonModule, MatIconModule, MatChipsModule,
    MatFormFieldModule, MatInputModule, MatPaginatorModule, MatSelectModule, MatDialogModule,
    BreadcrumbsComponent, SkeletonComponent, EmptyStateComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Conceptos' }]" />
      <div class="sig-page__header">
        <h1 class="sig-page__title">Conceptos</h1>
        <a mat-flat-button color="primary" routerLink="/concepts/nuevo" data-testid="btn-nuevo"><mat-icon>add</mat-icon> Nuevo Concepto</a>
      </div>
      <mat-card>
        <mat-card-content>
          <div class="sig-table-toolbar">
            <mat-form-field appearance="outline" class="sig-search">
              <mat-icon matPrefix aria-hidden="true">search</mat-icon>
              <mat-label>Buscar concepto...</mat-label>
              <input matInput [formControl]="search" data-testid="input-busqueda" />
            </mat-form-field>
            <mat-form-field appearance="outline" style="max-width: 160px;">
              <mat-label>Tipo</mat-label>
              <mat-select [formControl]="tipoFilter" data-testid="filter-tipo">
                <mat-option [value]="null">Todos</mat-option>
                <mat-option value="Pago">Pago</mat-option>
                <mat-option value="Factura">Factura</mat-option>
              </mat-select>
            </mat-form-field>
            <button mat-stroked-button (click)="onExportCSV()" style="margin-left: auto;" data-testid="btn-exportar-csv"><mat-icon>download</mat-icon> Exportar CSV</button>
          </div>
          @if (loading()) { <sig-skeleton [count]="5" /> }
          @else if (items().length === 0) {
            <sig-empty-state icon="calculate" title="No hay conceptos" ctaLabel="Crear primer concept" [hasFilter]="!!search.value || !!tipoFilter.value" (ctaClick)="onEmptyCta()" />
          } @else {
            <table mat-table [dataSource]="items()" class="sig-table sig-table--dark-header" data-testid="tabla-concepts">
              <ng-container matColumnDef="nombre"><th mat-header-cell *matHeaderCellDef>NOMBRE</th><td mat-cell *matCellDef="let row"><span class="sig-concept-name">{{ row.nombre }}</span></td></ng-container>
              <ng-container matColumnDef="tipo"><th mat-header-cell *matHeaderCellDef>TIPO</th><td mat-cell *matCellDef="let row"><span class="sig-type-badge" [class.sig-type--pago]="row.tipo === 'Pago'" [class.sig-type--factura]="row.tipo === 'Factura'">{{ row.tipo === 'Pago' ? '💰 Pago' : '📄 Factura' }}</span></td></ng-container>
              <ng-container matColumnDef="desde"><th mat-header-cell *matHeaderCellDef>DESDE</th><td mat-cell *matCellDef="let row" class="mono-num">{{ row.fechaDesde | date:'dd/MM/yyyy' }}</td></ng-container>
              <ng-container matColumnDef="hasta"><th mat-header-cell *matHeaderCellDef>HASTA</th><td mat-cell *matCellDef="let row" class="mono-num">{{ row.fechaHasta ? (row.fechaHasta | date:'dd/MM/yyyy') : '—' }}</td></ng-container>
              <ng-container matColumnDef="acciones">
                <th mat-header-cell *matHeaderCellDef style="text-align: right;">ACCIONES</th>
                <td mat-cell *matCellDef="let row">
                  <div class="sig-table-actions">
                    <a mat-icon-button [routerLink]="['/concepts', row.id, 'formula']" matTooltip="Editar fórmula" [attr.data-testid]="'btn-formula-' + row.id" aria-label="Fórmula"><mat-icon>functions</mat-icon></a>
                    <a mat-icon-button [routerLink]="['/concepts', row.id]" [attr.data-testid]="'btn-ver-' + row.id" aria-label="Ver"><mat-icon>visibility</mat-icon></a>
                    <a mat-icon-button [routerLink]="['/concepts', row.id, 'editar']" [attr.data-testid]="'btn-editar-' + row.id" aria-label="Editar"><mat-icon>edit</mat-icon></a>
                    <button mat-icon-button (click)="onDelete(row)" [attr.data-testid]="'btn-eliminar-' + row.id" aria-label="Eliminar"><mat-icon>delete</mat-icon></button>
                  </div>
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="cols"></tr>
              <tr mat-row *matRowDef="let row; columns: cols" data-testid="row-concept"></tr>
            </table>
          }
          <mat-paginator [length]="total()" [pageSize]="pageSize()" [pageIndex]="page() - 1" [pageSizeOptions]="[10, 25, 50]" showFirstLastButtons (page)="onPage($event)" data-testid="paginator-concepts" />
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .sig-table-toolbar { display: flex; gap: 8px; align-items: center; margin-bottom: 16px; flex-wrap: wrap; }
    .sig-search { flex: 1; max-width: 240px; }
    .sig-concept-name { font-weight: 600; color: #1F4E78; }
    .sig-type-badge { display: inline-block; padding: 2px 10px; border-radius: 10px; font-size: 11px; font-weight: 600; }
    .sig-type--pago { background: #FFF3E0; color: #E65100; }
    .sig-type--factura { background: #E3F2FD; color: #1565C0; }
    :host ::ng-deep .sig-table--dark-header th.mat-header-cell {
      background: #1F4E78 !important; color: rgba(255,255,255,0.85) !important;
      font-size: 11px; font-weight: 700; letter-spacing: 0.5px;
    }
  `],
})
export class ConceptsListComponent implements OnInit {
  private readonly conceptSvc = inject(ConceptService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotifyService);
  private readonly router = inject(Router);

  protected readonly items = signal<ConceptListItemDto[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(25);
  protected readonly loading = signal(true);
  protected readonly search = new FormControl<string>('', { nonNullable: true });
  protected readonly tipoFilter = new FormControl<TipoConcepto | null>(null);
  protected readonly cols = ['nombre', 'tipo', 'desde', 'hasta', 'acciones'];

  ngOnInit(): void {
    this.search.valueChanges.pipe(debounceTime(300), distinctUntilChanged()).subscribe(() => { this.page.set(1); this.load(); });
    this.tipoFilter.valueChanges.subscribe(() => { this.page.set(1); this.load(); });
    this.load();
  }
  protected onPage(e: PageEvent): void { this.pageSize.set(e.pageSize); this.page.set(e.pageIndex + 1); this.load(); }
  protected onEmptyCta(): void {
    if (this.search.value || this.tipoFilter.value) { this.search.setValue(''); this.tipoFilter.setValue(null); }
    else { void this.router.navigate(['/concepts/nuevo']); }
  }
  protected onDelete(row: ConceptListItemDto): void {
    this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Eliminar Concept', message: 'Acción irreversible.', entityName: row.nombre, destructive: true }, minWidth: 480,
    }).afterClosed().subscribe((ok) => {
      if (!ok) return;
      this.conceptSvc.delete(row.id).subscribe({ next: () => { this.notify.success('Concept eliminado'); this.load(); }, error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo eliminar') });
    });
  }
  protected onExportCSV(): void { exportCSV('concepts.csv', this.items().map((c) => ({ Id: c.id, Nombre: c.nombre, Tipo: c.tipo, Desde: c.fechaDesde, Hasta: c.fechaHasta ?? '' }))); }
  private load(): void {
    this.loading.set(true);
    this.conceptSvc.list(this.page(), this.pageSize(), this.tipoFilter.value, this.search.value).subscribe({
      next: (r) => { this.items.set(r.items); this.total.set(r.total); this.loading.set(false); },
      error: () => { this.items.set([]); this.total.set(0); this.loading.set(false); },
    });
  }
}
