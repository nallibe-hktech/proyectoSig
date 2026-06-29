import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { debounceTime, Subject } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SgpvService, SgpvProductoDto } from '../services/sgpv.service';
import { SyncService } from '../../../core/api/misc.service';
import { NotifyService } from '../../../core/notify.service';
import { BreadcrumbsComponent } from '../../../shared/breadcrumbs.component';

@Component({
  selector: 'app-sgpv-dashboard',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatTableModule, MatInputModule, MatFormFieldModule,
    MatProgressSpinnerModule, MatCardModule, MatIconModule,
    MatButtonModule, MatPaginatorModule,
    BreadcrumbsComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'SGPV' }]" />

      <div class="sig-page__header">
        <h1 class="sig-page__title"><mat-icon class="title-icon">location_on</mat-icon> SGPV — Productos</h1>
        <button mat-flat-button color="primary" (click)="syncManual()" [disabled]="syncing()">
          @if (syncing()) { <mat-spinner diameter="18"></mat-spinner> }
          @else { <mat-icon>sync</mat-icon> }
          Sincronizar
        </button>
      </div>

      <!-- KPIs -->
      <div class="kpi-row">
        <div class="sig-kpi-card">
          <div class="sig-kpi-card__label">Total productos</div>
          <div class="sig-kpi-card__value">{{ total() }}</div>
        </div>
        <div class="sig-kpi-card">
          <div class="sig-kpi-card__label">Categorías</div>
          <div class="sig-kpi-card__value">{{ categoriasUnicas() }}</div>
        </div>
        <div class="sig-kpi-card">
          <div class="sig-kpi-card__label">Clientes</div>
          <div class="sig-kpi-card__value">{{ clientesUnicos() }}</div>
        </div>
        <div class="sig-kpi-card">
          <div class="sig-kpi-card__label">Activos</div>
          <div class="sig-kpi-card__value">{{ productosActivos() }}</div>
        </div>
      </div>

      <!-- Búsqueda -->
      <mat-card>
        <mat-card-content>
          <mat-form-field appearance="outline" class="search-field">
            <mat-label>Buscar producto (referencia, cliente, categoría...)</mat-label>
            <input matInput (input)="onSearch($event)" />
            <mat-icon matSuffix>search</mat-icon>
          </mat-form-field>
        </mat-card-content>
      </mat-card>

      <!-- Tabla -->
      <mat-card>
        <mat-card-content>
          @if (loading()) {
            <div class="spinner-container"><mat-spinner diameter="48"></mat-spinner></div>
          } @else if (productos().length === 0) {
            <div class="empty-state">
              <mat-icon>inventory_2</mat-icon>
              <p>Sin productos sincronizados. Pulsa "Sincronizar" para cargar datos de SGPV.</p>
            </div>
          } @else {
            <table mat-table [dataSource]="productos()" class="sig-table">
              <ng-container matColumnDef="cliente">
                <th mat-header-cell *matHeaderCellDef>Cliente</th>
                <td mat-cell *matCellDef="let row">{{ row.cliente }}</td>
              </ng-container>
              <ng-container matColumnDef="referencia">
                <th mat-header-cell *matHeaderCellDef>Referencia</th>
                <td mat-cell *matCellDef="let row">{{ row.referencia || '—' }}</td>
              </ng-container>
              <ng-container matColumnDef="categoria">
                <th mat-header-cell *matHeaderCellDef>Categoría</th>
                <td mat-cell *matCellDef="let row">{{ row.categoria }}</td>
              </ng-container>
              <ng-container matColumnDef="subcategoria">
                <th mat-header-cell *matHeaderCellDef>Subcategoría</th>
                <td mat-cell *matCellDef="let row">{{ row.subcategoria || '—' }}</td>
              </ng-container>
              <ng-container matColumnDef="marca">
                <th mat-header-cell *matHeaderCellDef>Marca</th>
                <td mat-cell *matCellDef="let row">{{ row.marca || '—' }}</td>
              </ng-container>
              <ng-container matColumnDef="activo">
                <th mat-header-cell *matHeaderCellDef>Estado</th>
                <td mat-cell *matCellDef="let row">
                  <span [class.estado-activo]="row.activo" [class.estado-inactivo]="!row.activo">
                    {{ row.activo ? 'Activo' : 'Inactivo' }}
                  </span>
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="columns"></tr>
              <tr mat-row *matRowDef="let row; columns: columns;"></tr>
            </table>
          }
        </mat-card-content>
      </mat-card>

      <!-- Paginador -->
      <mat-paginator
        [length]="total()"
        [pageSize]="pageSize()"
        [pageIndex]="page() - 1"
        [pageSizeOptions]="[25, 50, 100, 200]"
        showFirstLastButtons
        (page)="onPageChange($event)">
      </mat-paginator>
    </div>
  `,
  styles: [`
    .title-icon { vertical-align: middle; margin-right: 6px; }
    .kpi-row { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 16px; margin-bottom: 20px; }
    .sig-kpi-card { background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 12px; padding: 18px 20px; }
    .sig-kpi-card__label { font-size: 11px; font-weight: 600; letter-spacing: .08em; text-transform: uppercase; color: var(--sig-text-muted); margin-bottom: 10px; }
    .sig-kpi-card__value { font-size: 28px; font-weight: 700; color: var(--sig-text-heading); font-family: 'Roboto Mono', monospace; line-height: 1; }
    mat-card { margin-bottom: 16px; }
    .search-field { width: 100%; max-width: 420px; }
    .sig-table { width: 100%; }
    .spinner-container { display: flex; justify-content: center; padding: 48px; }
    .empty-state { text-align: center; padding: 48px; color: var(--sig-text-muted); }
    .empty-state mat-icon { font-size: 48px; width: 48px; height: 48px; display: block; margin: 0 auto 12px; }
    .estado-activo { color: var(--sig-success); font-weight: 500; }
    .estado-inactivo { color: var(--sig-text-muted); font-weight: 500; }
  `],
})
export class SgpvDashboardComponent implements OnInit {
  private readonly sgpvSvc = inject(SgpvService);
  private readonly syncSvc = inject(SyncService);
  private readonly notify = inject(NotifyService);

  protected readonly page = signal(1);
  protected readonly pageSize = signal(25);
  protected readonly total = signal(0);
  protected readonly productos = signal<SgpvProductoDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly syncing = signal(false);
  private readonly searchSubject = new Subject<string>();

  protected readonly columns = ['cliente', 'referencia', 'categoria', 'subcategoria', 'marca', 'activo'];

  protected readonly categoriasUnicas = computed(() => new Set(this.productos().map(p => p.categoria)).size);
  protected readonly clientesUnicos = computed(() => new Set(this.productos().map(p => p.cliente)).size);
  protected readonly productosActivos = computed(() => this.productos().filter(p => p.activo).length);

  private searchTerm = '';

  constructor() {
    this.searchSubject.pipe(debounceTime(300), takeUntilDestroyed()).subscribe(term => {
      this.searchTerm = term;
      this.page.set(1);
      this.load();
    });
  }

  ngOnInit(): void {
    this.load();
  }

  protected onSearch(event: Event): void {
    this.searchSubject.next((event.target as HTMLInputElement).value);
  }

  protected onPageChange(event: PageEvent): void {
    this.page.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    window.scrollTo({ top: 0, behavior: 'smooth' });
    this.load();
  }

  protected syncManual(): void {
    this.syncing.set(true);
    this.syncSvc.sync('sgpv').subscribe({
      next: (result) => {
        this.syncing.set(false);
        this.notify.success(`Sincronización completada: ${result.registrosInsertados} nuevo(s), ${result.registrosActualizados} actualizado(s)`);
        this.page.set(1);
        this.load();
      },
      error: (err) => {
        this.syncing.set(false);
        this.notify.error(err?.error?.title ?? 'Error sincronizando SGPV');
      },
    });
  }

  private load(): void {
    this.loading.set(true);
    this.sgpvSvc.listProductosPaginated(this.page(), this.pageSize(), this.searchTerm).subscribe({
      next: (result) => {
        this.productos.set(result.items);
        this.total.set(result.total);
        this.loading.set(false);
      },
      error: (err) => {
        this.notify.error(err?.error?.title ?? 'Error cargando productos SGPV');
        this.loading.set(false);
      },
    });
  }
}
