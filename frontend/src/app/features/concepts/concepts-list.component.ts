import { Component, computed, inject, OnInit, signal } from '@angular/core';
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
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ConceptService } from '../../core/api/concepts.service';
import { AuthService } from '../../core/auth/auth.service';
import { ConceptListItemDto } from '../../models/dtos';
import { TipoConcepto } from '../../models/enums';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { ConfirmDialogComponent, ConfirmDialogData } from '../../shared/confirm-dialog.component';
import { NotifyService } from '../../core/notify.service';
import { exportCSV } from '../../core/api/api.helpers';

@Component({
  selector: 'app-concepts-list',
  standalone: true,
  imports: [
    CommonModule, DatePipe, RouterLink, ReactiveFormsModule,
    MatCardModule, MatTableModule, MatButtonModule, MatIconModule, MatChipsModule,
    MatFormFieldModule, MatInputModule, MatPaginatorModule, MatSelectModule,
    MatDialogModule, MatTooltipModule,
    BreadcrumbsComponent, SkeletonComponent, EmptyStateComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Conceptos' }]" />
      <div class="sig-page__header">
        <h1 class="sig-page__title">Conceptos</h1>
        @if (isAdmin()) {
          <a mat-flat-button color="primary" routerLink="/concepts/nuevo" data-testid="btn-nuevo-concepto">
            <mat-icon>add</mat-icon> Nuevo Concepto
          </a>
        }
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
            <sig-empty-state icon="calculate" title="No hay conceptos" ctaLabel="Crear primer concepto" [hasFilter]="!!search.value || !!tipoFilter.value" (ctaClick)="onEmptyCta()" />
          } @else {
            @if (showPagos()) {
              <section class="sig-concept-group" data-testid="grupo-pagos">
                <h2 class="sig-group-title sig-group-title--pago">💰 Pagos</h2>
                @if (pagos().length === 0) {
                  <p class="sig-group-empty">No hay conceptos de tipo Pago.</p>
                } @else {
                  <table mat-table [dataSource]="pagos()" class="sig-table sig-table--dark-header" data-testid="tabla-pagos">
                    <ng-container matColumnDef="nombre"><th mat-header-cell *matHeaderCellDef>NOMBRE</th><td mat-cell *matCellDef="let row"><span class="sig-concept-name">{{ row.nombre }}</span></td></ng-container>
                    <ng-container matColumnDef="tipo"><th mat-header-cell *matHeaderCellDef>TIPO</th><td mat-cell *matCellDef="let row"><span class="sig-type-badge" [class.sig-type--pago]="row.tipo === 'Pago'" [class.sig-type--factura]="row.tipo === 'Factura'">{{ row.tipo === 'Pago' ? '💰 Pago' : '📄 Factura' }}</span></td></ng-container>
                    <ng-container matColumnDef="desde"><th mat-header-cell *matHeaderCellDef>DESDE</th><td mat-cell *matCellDef="let row" class="mono-num">{{ row.fechaDesde | date:'dd/MM/yyyy' }}</td></ng-container>
                    <ng-container matColumnDef="hasta"><th mat-header-cell *matHeaderCellDef>HASTA</th><td mat-cell *matCellDef="let row" class="mono-num">{{ row.fechaHasta ? (row.fechaHasta | date:'dd/MM/yyyy') : '—' }}</td></ng-container>
                    <ng-container matColumnDef="acciones">
                      <th mat-header-cell *matHeaderCellDef style="text-align: right;">ACCIONES</th>
                      <td mat-cell *matCellDef="let row">
                        <div class="sig-table-actions">
                          <a mat-icon-button [routerLink]="['/concepts', row.id, 'formula']" matTooltip="Editor de fórmula" [attr.data-testid]="'btn-formula-' + row.id" aria-label="Fórmula"><mat-icon>functions</mat-icon></a>
                          <a mat-icon-button [routerLink]="['/concepts', row.id, 'tarifas']" matTooltip="Tarifas" [attr.data-testid]="'btn-tarifas-' + row.id" aria-label="Tarifas"><mat-icon>local_offer</mat-icon></a>
                          <a mat-icon-button [routerLink]="['/concepts', row.id, 'presupuestos']" matTooltip="Presupuestos" [attr.data-testid]="'btn-presupuestos-' + row.id" aria-label="Presupuestos"><mat-icon>savings</mat-icon></a>
                          <a mat-icon-button [routerLink]="['/concepts', row.id]" matTooltip="Ver detalles" [attr.data-testid]="'btn-ver-' + row.id" aria-label="Ver"><mat-icon>visibility</mat-icon></a>
                          @if (isAdmin()) {
                            <a mat-icon-button [routerLink]="['/concepts', row.id, 'editar']" matTooltip="Editar" [attr.data-testid]="'btn-editar-' + row.id" aria-label="Editar"><mat-icon>edit</mat-icon></a>
                            <button mat-icon-button color="warn" (click)="onDelete(row)" matTooltip="Eliminar" [attr.data-testid]="'btn-eliminar-' + row.id" aria-label="Eliminar"><mat-icon>delete</mat-icon></button>
                          }
                        </div>
                      </td>
                    </ng-container>
                    <tr mat-header-row *matHeaderRowDef="cols"></tr>
                    <tr mat-row *matRowDef="let row; columns: cols" data-testid="row-concept"></tr>
                  </table>
                }
              </section>
            }
            @if (showFacturas()) {
              <section class="sig-concept-group" data-testid="grupo-facturacion">
                <h2 class="sig-group-title sig-group-title--factura">📄 Facturación</h2>
                @if (facturas().length === 0) {
                  <p class="sig-group-empty">No hay conceptos de tipo Factura.</p>
                } @else {
                  <table mat-table [dataSource]="facturas()" class="sig-table sig-table--dark-header" data-testid="tabla-facturacion">
                    <ng-container matColumnDef="nombre"><th mat-header-cell *matHeaderCellDef>NOMBRE</th><td mat-cell *matCellDef="let row"><span class="sig-concept-name">{{ row.nombre }}</span></td></ng-container>
                    <ng-container matColumnDef="tipo"><th mat-header-cell *matHeaderCellDef>TIPO</th><td mat-cell *matCellDef="let row"><span class="sig-type-badge" [class.sig-type--pago]="row.tipo === 'Pago'" [class.sig-type--factura]="row.tipo === 'Factura'">{{ row.tipo === 'Pago' ? '💰 Pago' : '📄 Factura' }}</span></td></ng-container>
                    <ng-container matColumnDef="desde"><th mat-header-cell *matHeaderCellDef>DESDE</th><td mat-cell *matCellDef="let row" class="mono-num">{{ row.fechaDesde | date:'dd/MM/yyyy' }}</td></ng-container>
                    <ng-container matColumnDef="hasta"><th mat-header-cell *matHeaderCellDef>HASTA</th><td mat-cell *matCellDef="let row" class="mono-num">{{ row.fechaHasta ? (row.fechaHasta | date:'dd/MM/yyyy') : '—' }}</td></ng-container>
                    <ng-container matColumnDef="acciones">
                      <th mat-header-cell *matHeaderCellDef style="text-align: right;">ACCIONES</th>
                      <td mat-cell *matCellDef="let row">
                        <div class="sig-table-actions">
                          <a mat-icon-button [routerLink]="['/concepts', row.id, 'formula']" matTooltip="Editor de fórmula" [attr.data-testid]="'btn-formula-' + row.id" aria-label="Fórmula"><mat-icon>functions</mat-icon></a>
                          <a mat-icon-button [routerLink]="['/concepts', row.id, 'tarifas']" matTooltip="Tarifas" [attr.data-testid]="'btn-tarifas-' + row.id" aria-label="Tarifas"><mat-icon>local_offer</mat-icon></a>
                          <a mat-icon-button [routerLink]="['/concepts', row.id, 'presupuestos']" matTooltip="Presupuestos" [attr.data-testid]="'btn-presupuestos-' + row.id" aria-label="Presupuestos"><mat-icon>savings</mat-icon></a>
                          <a mat-icon-button [routerLink]="['/concepts', row.id]" matTooltip="Ver detalles" [attr.data-testid]="'btn-ver-' + row.id" aria-label="Ver"><mat-icon>visibility</mat-icon></a>
                          @if (isAdmin()) {
                            <a mat-icon-button [routerLink]="['/concepts', row.id, 'editar']" matTooltip="Editar" [attr.data-testid]="'btn-editar-' + row.id" aria-label="Editar"><mat-icon>edit</mat-icon></a>
                            <button mat-icon-button color="warn" (click)="onDelete(row)" matTooltip="Eliminar" [attr.data-testid]="'btn-eliminar-' + row.id" aria-label="Eliminar"><mat-icon>delete</mat-icon></button>
                          }
                        </div>
                      </td>
                    </ng-container>
                    <tr mat-header-row *matHeaderRowDef="cols"></tr>
                    <tr mat-row *matRowDef="let row; columns: cols" data-testid="row-concept"></tr>
                  </table>
                }
              </section>
            }
          }
          <mat-paginator [length]="total()" [pageSize]="pageSize()" [pageIndex]="page() - 1" [pageSizeOptions]="[10, 25, 50]" showFirstLastButtons (page)="onPage($event)" data-testid="paginator-concepts" />
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .sig-table-toolbar { display: flex; gap: 8px; align-items: center; margin-bottom: 16px; flex-wrap: wrap; }
    .sig-search { flex: 1; max-width: 240px; }
    .sig-concept-name { font-weight: 600; color: var(--sig-text-heading); }
    .sig-type-badge { display: inline-block; padding: 2px 10px; border-radius: 10px; font-size: 11px; font-weight: 600; }
    .sig-type--pago { background: rgba(37,99,235,.15); color: #3b82f6; }
    .sig-type--factura { background: rgba(139,92,246,.15); color: #8b5cf6; }
    .sig-concept-group { margin-bottom: 24px; }
    .sig-group-title { font-size: 15px; font-weight: 700; margin: 8px 0 12px; padding-bottom: 6px; border-bottom: 2px solid var(--mat-sys-outline-variant); }
    .sig-group-title--pago { color: #3b82f6; }
    .sig-group-title--factura { color: #8b5cf6; }
    .sig-group-empty { color: var(--sig-text-muted, var(--mat-sys-on-surface-variant)); font-size: 13px; margin: 4px 0 0; }
    :host ::ng-deep .sig-table--dark-header th.mat-header-cell {
      background: var(--sig-bg-header) !important; color: var(--sig-text-muted) !important;
      font-size: 11px; font-weight: 700; letter-spacing: 0.5px;
    }
  `],
})
export class ConceptsListComponent implements OnInit {
  private readonly conceptSvc = inject(ConceptService);
  private readonly authSvc = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly notify = inject(NotifyService);

  protected readonly isAdmin = computed(() => this.authSvc.hasRole('Administrator'));

  protected readonly items = signal<ConceptListItemDto[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(25);
  protected readonly loading = signal(true);
  protected readonly search = new FormControl<string>('', { nonNullable: true });
  protected readonly tipoFilter = new FormControl<TipoConcepto | null>(null);
  protected readonly tipoFilterValue = signal<TipoConcepto | null>(null);
  protected readonly cols = ['nombre', 'tipo', 'desde', 'hasta', 'acciones'];

  protected readonly pagos = computed(() => this.items().filter((c) => c.tipo === 'Pago'));
  protected readonly facturas = computed(() => this.items().filter((c) => c.tipo === 'Factura'));
  protected readonly showPagos = computed(() => this.tipoFilterValue() !== 'Factura');
  protected readonly showFacturas = computed(() => this.tipoFilterValue() !== 'Pago');

  ngOnInit(): void {
    this.search.valueChanges.pipe(debounceTime(300), distinctUntilChanged()).subscribe(() => { this.page.set(1); this.load(); });
    this.tipoFilter.valueChanges.subscribe((v) => { this.tipoFilterValue.set(v); this.page.set(1); this.load(); });
    this.load();
  }

  protected onPage(e: PageEvent): void { this.pageSize.set(e.pageSize); this.page.set(e.pageIndex + 1); window.scrollTo({ top: 0, behavior: 'smooth' }); this.load(); }

  protected onEmptyCta(): void {
    if (this.search.value || this.tipoFilter.value) {
      this.search.setValue('');
      this.tipoFilter.setValue(null);
    } else if (this.isAdmin()) {
      this.router.navigate(['/concepts/nuevo']);
    }
  }

  protected onExportCSV(): void {
    exportCSV('concepts.csv', this.items().map((c) => ({ Id: c.id, Nombre: c.nombre, Tipo: c.tipo, Desde: c.fechaDesde, Hasta: c.fechaHasta ?? '' })));
  }

  protected onDelete(row: ConceptListItemDto): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Eliminar concepto',
        message: '¿Estás seguro de que quieres eliminar este concepto? Esta acción no se puede deshacer.',
        entityName: row.nombre,
        confirmLabel: 'Eliminar',
        destructive: true,
      } satisfies ConfirmDialogData,
      width: '420px',
    });
    ref.afterClosed().subscribe((confirmed) => {
      if (!confirmed) return;
      this.conceptSvc.delete(row.id).subscribe({
        next: () => { this.notify.success(`Concepto "${row.nombre}" eliminado`); this.load(); },
        error: () => this.notify.error('No se pudo eliminar el concepto'),
      });
    });
  }

  private load(): void {
    this.loading.set(true);
    this.conceptSvc.list(this.page(), this.pageSize(), this.tipoFilter.value, this.search.value).subscribe({
      next: (r) => { this.items.set(r.items); this.total.set(r.total); this.loading.set(false); },
      error: () => { this.items.set([]); this.total.set(0); this.loading.set(false); },
    });
  }
}
