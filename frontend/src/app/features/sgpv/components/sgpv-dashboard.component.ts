import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatTabsModule } from '@angular/material/tabs';
import { debounceTime, Subject } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SgpvService, SgpvVisitaDto, SgpvProductoDto } from '../services/sgpv.service';
import { SyncService } from '../../../core/api/misc.service';
import { NotifyService } from '../../../core/notify.service';
import { BreadcrumbsComponent } from '../../../shared/breadcrumbs.component';

@Component({
  selector: 'app-sgpv-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatInputModule,
    MatFormFieldModule,
    MatProgressSpinnerModule,
    MatProgressBarModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatDividerModule,
    MatPaginatorModule,
    MatTabsModule,
    BreadcrumbsComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'SGPV' }]" />

      <div class="sig-page__header">
        <h1 class="sig-page__title"><mat-icon class="title-icon">location_on</mat-icon> SGPV</h1>
        <button mat-flat-button color="primary" (click)="syncManual()" [disabled]="syncing()">
          @if (syncing()) {
            <mat-spinner diameter="18"></mat-spinner>
          } @else {
            <mat-icon>sync</mat-icon>
          }
          Sincronizar
        </button>
      </div>

      <!-- KPI Cards (dynamic based on active tab) -->
      <div class="kpi-row">
        @if (activeTab() === 0) {
          <!-- Visitas KPIs -->
          <div class="sig-kpi-card">
            <div class="sig-kpi-card__label">Visitas</div>
            <div class="sig-kpi-card__value">{{ visitasCount() }}</div>
          </div>
          <div class="sig-kpi-card">
            <div class="sig-kpi-card__label">Horas totales</div>
            <div class="sig-kpi-card__value">{{ horasTotales() | number:'1.0-1' }}</div>
          </div>
          <div class="sig-kpi-card">
            <div class="sig-kpi-card__label">Centros únicos</div>
            <div class="sig-kpi-card__value">{{ centrosUnicos() }}</div>
          </div>
          <div class="sig-kpi-card">
            <div class="sig-kpi-card__label">Recursos asignados</div>
            <div class="sig-kpi-card__value">{{ recursosAsignados() }}</div>
          </div>
        } @else {
          <!-- Productos KPIs -->
          <div class="sig-kpi-card">
            <div class="sig-kpi-card__label">Productos</div>
            <div class="sig-kpi-card__value">{{ productosCount() }}</div>
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
        }
      </div>

      <!-- Tabs -->
      <mat-tab-group (selectedIndexChange)="onTabChange($event)" [selectedIndex]="activeTab()">
        <!-- Visitas Tab -->
        <mat-tab label="Visitas">
          <!-- Search Box -->
          <mat-card>
            <mat-card-content>
              <div class="search-container">
                <mat-form-field appearance="outline" class="search-field">
                  <mat-label>Buscar visita (NIF, centro, servicio...)</mat-label>
                  <input matInput (input)="onSearchVisitas($event)" />
                  <mat-icon matSuffix>search</mat-icon>
                </mat-form-field>
              </div>
            </mat-card-content>
          </mat-card>

          <!-- Visitas Table -->
          <mat-card>
            <mat-card-content>
              @if (loadingVisitas()) {
                <div class="spinner-container">
                  <mat-spinner diameter="48"></mat-spinner>
                </div>
              } @else if (visitas().length === 0) {
                <div class="empty-state">
                  <p>Sin visitas sincronizadas</p>
                </div>
              } @else {
                <table mat-table [dataSource]="visitas()" class="sig-table">
                  <ng-container matColumnDef="visitaIdExterno">
                    <th mat-header-cell>ID Visita</th>
                    <td mat-cell *matCellDef="let row">{{ row.visitaIdExterno }}</td>
                  </ng-container>
                  <ng-container matColumnDef="resourceNif">
                    <th mat-header-cell>NIF Recurso</th>
                    <td mat-cell *matCellDef="let row">{{ row.resourceNif || '—' }}</td>
                  </ng-container>
                  <ng-container matColumnDef="centroNombre">
                    <th mat-header-cell>Centro</th>
                    <td mat-cell *matCellDef="let row">{{ row.centroNombre }}</td>
                  </ng-container>
                  <ng-container matColumnDef="serviceName">
                    <th mat-header-cell>Servicio</th>
                    <td mat-cell *matCellDef="let row">{{ row.serviceName || '—' }}</td>
                  </ng-container>
                  <ng-container matColumnDef="fecha">
                    <th mat-header-cell>Fecha</th>
                    <td mat-cell *matCellDef="let row">{{ row.fecha | date:'dd/MM/yyyy' }}</td>
                  </ng-container>
                  <ng-container matColumnDef="horasDuracion">
                    <th mat-header-cell>Horas</th>
                    <td mat-cell *matCellDef="let row">{{ row.horasDuracion | number:'1.1-1' }}</td>
                  </ng-container>

                  <tr mat-header-row *matHeaderRowDef="displayedColumnsVisitas"></tr>
                  <tr mat-row *matRowDef="let row; columns: displayedColumnsVisitas;"></tr>
                </table>
              }
            </mat-card-content>
          </mat-card>

          <!-- Visitas Paginator -->
          <mat-paginator
            [length]="visitasTotal()"
            [pageSize]="visitasPageSize()"
            [pageIndex]="visitasPage() - 1"
            [pageSizeOptions]="[10, 25, 50, 100]"
            showFirstLastButtons
            (page)="onVisitasPageChange($event)">
          </mat-paginator>
        </mat-tab>

        <!-- Productos Tab -->
        <mat-tab label="Productos">
          <!-- Search Box -->
          <mat-card>
            <mat-card-content>
              <div class="search-container">
                <mat-form-field appearance="outline" class="search-field">
                  <mat-label>Buscar producto (referencia, cliente, categoría...)</mat-label>
                  <input matInput (input)="onSearchProductos($event)" />
                  <mat-icon matSuffix>search</mat-icon>
                </mat-form-field>
              </div>
            </mat-card-content>
          </mat-card>

          <!-- Productos Table -->
          <mat-card>
            <mat-card-content>
              @if (loadingProductos()) {
                <div class="spinner-container">
                  <mat-spinner diameter="48"></mat-spinner>
                </div>
              } @else if (productos().length === 0) {
                <div class="empty-state">
                  <p>Sin productos sincronizados</p>
                </div>
              } @else {
                <table mat-table [dataSource]="productos()" class="sig-table">
                  <ng-container matColumnDef="cliente">
                    <th mat-header-cell>Cliente</th>
                    <td mat-cell *matCellDef="let row">{{ row.cliente }}</td>
                  </ng-container>
                  <ng-container matColumnDef="referencia">
                    <th mat-header-cell>Referencia</th>
                    <td mat-cell *matCellDef="let row">{{ row.referencia || '—' }}</td>
                  </ng-container>
                  <ng-container matColumnDef="categoria">
                    <th mat-header-cell>Categoría</th>
                    <td mat-cell *matCellDef="let row">{{ row.categoria }}</td>
                  </ng-container>
                  <ng-container matColumnDef="subcategoria">
                    <th mat-header-cell>Subcategoría</th>
                    <td mat-cell *matCellDef="let row">{{ row.subcategoria || '—' }}</td>
                  </ng-container>
                  <ng-container matColumnDef="marca">
                    <th mat-header-cell>Marca</th>
                    <td mat-cell *matCellDef="let row">{{ row.marca || '—' }}</td>
                  </ng-container>
                  <ng-container matColumnDef="activo">
                    <th mat-header-cell>Estado</th>
                    <td mat-cell *matCellDef="let row">
                      <span [class.estado-activo]="row.activo" [class.estado-inactivo]="!row.activo">
                        {{ row.activo ? 'Activo' : 'Inactivo' }}
                      </span>
                    </td>
                  </ng-container>

                  <tr mat-header-row *matHeaderRowDef="displayedColumnsProductos"></tr>
                  <tr mat-row *matRowDef="let row; columns: displayedColumnsProductos;"></tr>
                </table>
              }
            </mat-card-content>
          </mat-card>

          <!-- Productos Paginator -->
          <mat-paginator
            [length]="productosTotal()"
            [pageSize]="productosPageSize()"
            [pageIndex]="productosPage() - 1"
            [pageSizeOptions]="[10, 25, 50, 100]"
            showFirstLastButtons
            (page)="onProductosPageChange($event)">
          </mat-paginator>
        </mat-tab>
      </mat-tab-group>
    </div>
  `,
  styles: [`
    .kpi-row { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 16px; margin-bottom: 24px; }
    .sig-kpi-card { background: var(--mat-sys-surface); border: 1px solid var(--mat-sys-outline); border-radius: 8px; padding: 16px; }
    .sig-kpi-card__label { font-size: 12px; color: var(--mat-sys-on-surface-variant); font-weight: 500; }
    .sig-kpi-card__value { font-size: 24px; font-weight: 600; color: var(--mat-sys-primary); margin-top: 8px; }
    .search-container { padding: 16px; }
    .search-field { width: 100%; max-width: 400px; }
    .spinner-container { display: flex; justify-content: center; padding: 48px; }
    .empty-state { text-align: center; padding: 48px; color: var(--mat-sys-on-surface-variant); }
    .sig-table { width: 100%; }
    table { border-collapse: collapse; }
    th { background-color: var(--mat-sys-surface-variant); font-weight: 600; }
    td { padding: 8px; border-bottom: 1px solid var(--mat-sys-outline); }
    .estado-activo { color: #4caf50; font-weight: 500; }
    .estado-inactivo { color: #999; font-weight: 500; }
    mat-card { margin-bottom: 16px; }
  `],
})
export class SgpvDashboardComponent implements OnInit {
  private readonly sgpvSvc = inject(SgpvService);
  private readonly syncSvc = inject(SyncService);
  private readonly notify = inject(NotifyService);

  // Tab state
  protected readonly activeTab = signal(0);

  // Visitas state
  protected readonly visitasPage = signal(1);
  protected readonly visitasPageSize = signal(25);
  protected readonly visitasTotal = signal(0);
  protected readonly visitas = signal<SgpvVisitaDto[]>([]);
  protected readonly loadingVisitas = signal(false);
  private readonly visitasSearch = signal('');
  private readonly visitasSearchSubject = new Subject<string>();

  // Productos state
  protected readonly productosPage = signal(1);
  protected readonly productosPageSize = signal(25);
  protected readonly productosTotal = signal(0);
  protected readonly productos = signal<SgpvProductoDto[]>([]);
  protected readonly loadingProductos = signal(false);
  private readonly productosSearch = signal('');
  private readonly productosSearchSubject = new Subject<string>();

  protected readonly syncing = signal(false);

  protected readonly displayedColumnsVisitas = ['visitaIdExterno', 'resourceNif', 'centroNombre', 'serviceName', 'fecha', 'horasDuracion'];
  protected readonly displayedColumnsProductos = ['cliente', 'referencia', 'categoria', 'subcategoria', 'marca', 'activo'];

  // Computed properties for Visitas
  protected readonly visitasCount = computed(() => this.visitas().length);
  protected readonly horasTotales = computed(() => this.visitas().reduce((sum, v) => sum + (v.horasDuracion || 0), 0));
  protected readonly centrosUnicos = computed(() => new Set(this.visitas().map(v => v.centroId)).size);
  protected readonly recursosAsignados = computed(() => this.visitas().filter(v => v.userId).length);

  // Computed properties for Productos
  protected readonly productosCount = computed(() => this.productos().length);
  protected readonly categoriasUnicas = computed(() => new Set(this.productos().map(p => p.categoria)).size);
  protected readonly clientesUnicos = computed(() => new Set(this.productos().map(p => p.cliente)).size);
  protected readonly productosActivos = computed(() => this.productos().filter(p => p.activo).length);

  constructor() {
    this.visitasSearchSubject.pipe(
      debounceTime(300),
      takeUntilDestroyed()
    ).subscribe(searchTerm => {
      this.visitasSearch.set(searchTerm);
      this.visitasPage.set(1);
      this.loadVisitas();
    });

    this.productosSearchSubject.pipe(
      debounceTime(300),
      takeUntilDestroyed()
    ).subscribe(searchTerm => {
      this.productosSearch.set(searchTerm);
      this.productosPage.set(1);
      this.loadProductos();
    });
  }

  ngOnInit(): void {
    this.loadVisitas();
    this.loadProductos();
  }

  protected onTabChange(index: number): void {
    this.activeTab.set(index);
  }

  // NOTE: Visitas tab is ready for data. Currently returns empty from API,
  // but tab structure is complete. When new SGPV endpoint is available,
  // update HttpClients.cs GetVisitasAsync() and this will automatically work.
  protected onSearchVisitas(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.visitasSearchSubject.next(target.value);
  }

  protected onSearchProductos(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.productosSearchSubject.next(target.value);
  }

  protected onVisitasPageChange(event: PageEvent): void {
    this.visitasPage.set(event.pageIndex + 1);
    this.visitasPageSize.set(event.pageSize);
    window.scrollTo({ top: 0, behavior: 'smooth' });
    this.loadVisitas();
  }

  protected onProductosPageChange(event: PageEvent): void {
    this.productosPage.set(event.pageIndex + 1);
    this.productosPageSize.set(event.pageSize);
    window.scrollTo({ top: 0, behavior: 'smooth' });
    this.loadProductos();
  }

  protected syncManual(): void {
    this.syncing.set(true);
    this.syncSvc.sync('sgpv').subscribe({
      next: (result) => {
        this.syncing.set(false);
        this.notify.success(`Sincronización completada: ${result.registrosInsertados} nuevo(s), ${result.registrosActualizados} actualizado(s)`);
        this.loadVisitas();
        this.loadProductos();
      },
      error: (err) => {
        this.syncing.set(false);
        this.notify.error(err?.error?.title ?? 'Error sincronizando SGPV');
      },
    });
  }

  private loadVisitas(): void {
    this.loadingVisitas.set(true);
    this.sgpvSvc.listVisitasPaginated(this.visitasPage(), this.visitasPageSize(), this.visitasSearch()).subscribe({
      next: (result) => {
        this.visitas.set(result.items);
        this.visitasTotal.set(result.total);
        this.loadingVisitas.set(false);
      },
      error: (err) => {
        this.notify.error(err?.error?.title ?? 'Error cargando visitas');
        this.loadingVisitas.set(false);
      },
    });
  }

  private loadProductos(): void {
    this.loadingProductos.set(true);
    this.sgpvSvc.listProductosPaginated(this.productosPage(), this.productosPageSize(), this.productosSearch()).subscribe({
      next: (result) => {
        this.productos.set(result.items);
        this.productosTotal.set(result.total);
        this.loadingProductos.set(false);
      },
      error: (err) => {
        this.notify.error(err?.error?.title ?? 'Error cargando productos');
        this.loadingProductos.set(false);
      },
    });
  }
}
