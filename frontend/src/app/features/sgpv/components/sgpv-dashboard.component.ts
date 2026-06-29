import { Component, inject, signal, computed, OnInit, effect } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTableModule } from '@angular/material/table';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { debounceTime, Subject } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SgpvService, SgpvProductoDto, SgpvVisitaDashboardDto, SgpvCentroDashboardDto } from '../../../core/api/sgpv.service';
import { BreadcrumbsComponent } from '../../../shared/breadcrumbs.component';

@Component({
  selector: 'app-sgpv-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTabsModule,
    MatTableModule,
    MatInputModule,
    MatFormFieldModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    BreadcrumbsComponent,
    DecimalPipe,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'SGPV' }]" />
      <div class="sig-page__header"><h1 class="sig-page__title">SGPV</h1></div>

      <mat-tab-group [selectedIndex]="selectedTab()" (selectedIndexChange)="selectedTab.set($event)">
        <!-- PRODUCTOS TAB -->
        <mat-tab label="Productos">
          <ng-template mat-tab-label>
            <mat-icon class="mr-8">inventory</mat-icon>
            Productos
          </ng-template>

          <div class="tab-content">
            <!-- KPI Cards -->
            <div class="kpi-grid">
              <div class="kpi-card">
                <div class="kpi-value">{{ productosTotal() }}</div>
                <div class="kpi-label">TOTAL PRODUCTOS</div>
              </div>
              <div class="kpi-card">
                <div class="kpi-value">{{ productosCategories() }}</div>
                <div class="kpi-label">CATEGORÍAS</div>
              </div>
              <div class="kpi-card">
                <div class="kpi-value">{{ productosClientes() }}</div>
                <div class="kpi-label">CLIENTES</div>
              </div>
              <div class="kpi-card">
                <div class="kpi-value">{{ productosActivos() }}</div>
                <div class="kpi-label">ACTIVOS</div>
              </div>
            </div>

            <!-- Search & Table -->
            <mat-form-field appearance="outline" class="search-field">
              <mat-label>Buscar por ID, Cliente, Referencia o Categoría...</mat-label>
              <input matInput [(ngModel)]="productosSearch" (ngModelChange)="onProductosSearchChange($event)" />
            </mat-form-field>

            <div class="table-container">
              <table mat-table [dataSource]="productos()" class="full-width-table">
                <!-- Cliente Column -->
                <ng-container matColumnDef="cliente">
                  <th mat-header-cell *matHeaderCellDef>Cliente</th>
                  <td mat-cell *matCellDef="let element">{{ element.cliente }}</td>
                </ng-container>

                <!-- Referencia Column -->
                <ng-container matColumnDef="referencia">
                  <th mat-header-cell *matHeaderCellDef>Referencia</th>
                  <td mat-cell *matCellDef="let element">{{ element.referencia }}</td>
                </ng-container>

                <!-- Categoría Column -->
                <ng-container matColumnDef="categoria">
                  <th mat-header-cell *matHeaderCellDef>Categoría</th>
                  <td mat-cell *matCellDef="let element">{{ element.categoria }}</td>
                </ng-container>

                <!-- Subcategoría Column -->
                <ng-container matColumnDef="subcategoria">
                  <th mat-header-cell *matHeaderCellDef>Subcategoría</th>
                  <td mat-cell *matCellDef="let element">{{ element.subcategoria || '—' }}</td>
                </ng-container>

                <!-- Marca Column -->
                <ng-container matColumnDef="marca">
                  <th mat-header-cell *matHeaderCellDef>Marca</th>
                  <td mat-cell *matCellDef="let element">{{ element.marca || '—' }}</td>
                </ng-container>

                <!-- Estado Column -->
                <ng-container matColumnDef="activo">
                  <th mat-header-cell *matHeaderCellDef>Estado</th>
                  <td mat-cell *matCellDef="let element">
                    @if (element.activo) {
                      <span class="badge-active">Activo</span>
                    } @else {
                      <span class="badge-inactive">Inactivo</span>
                    }
                  </td>
                </ng-container>

                <tr mat-header-row *matHeaderRowDef="productosColumns"></tr>
                <tr mat-row *matRowDef="let element; columns: productosColumns;"></tr>
              </table>
            </div>

            @if (productosLoading()) {
              <div class="loading-container">
                <mat-spinner diameter="40"></mat-spinner>
              </div>
            }

            <mat-paginator
              [length]="productosTotal()"
              [pageSize]="productosPageSize()"
              [pageIndex]="productosPage() - 1"
              [pageSizeOptions]="[10, 25, 50, 100]"
              showFirstLastButtons
              (page)="onProductosPageChange($event)">
            </mat-paginator>
          </div>
        </mat-tab>

        <!-- VISITAS TAB -->
        <mat-tab label="Visitas">
          <ng-template mat-tab-label>
            <mat-icon class="mr-8">location_on</mat-icon>
            Visitas
          </ng-template>

          <div class="tab-content">
            <!-- KPI Cards -->
            <div class="kpi-grid">
              <div class="kpi-card">
                <div class="kpi-value">{{ visitasTotal() }}</div>
                <div class="kpi-label">TOTAL VISITAS</div>
              </div>
              <div class="kpi-card">
                <div class="kpi-value">{{ visitasEmpleados() }}</div>
                <div class="kpi-label">EMPLEADOS</div>
              </div>
              <div class="kpi-card">
                <div class="kpi-value">{{ visitasHoras() | number: '1.2-2' }}</div>
                <div class="kpi-label">HORAS TOTALES</div>
              </div>
            </div>

            <!-- Search & Table -->
            <mat-form-field appearance="outline" class="search-field">
              <mat-label>Buscar por NIF, Centro o Servicio...</mat-label>
              <input matInput [(ngModel)]="visitasSearch" (ngModelChange)="onVisitasSearchChange($event)" />
            </mat-form-field>

            <div class="table-container">
              <table mat-table [dataSource]="visitas()" class="full-width-table">
                <!-- NIF Column -->
                <ng-container matColumnDef="nif">
                  <th mat-header-cell *matHeaderCellDef>NIF</th>
                  <td mat-cell *matCellDef="let element" class="monospace">{{ element.resourceNif }}</td>
                </ng-container>

                <!-- Centro Column -->
                <ng-container matColumnDef="centro">
                  <th mat-header-cell *matHeaderCellDef>Centro</th>
                  <td mat-cell *matCellDef="let element">{{ element.centroNombre || element.centroId }}</td>
                </ng-container>

                <!-- Servicio Column -->
                <ng-container matColumnDef="servicio">
                  <th mat-header-cell *matHeaderCellDef>Servicio</th>
                  <td mat-cell *matCellDef="let element">{{ element.serviceName || '—' }}</td>
                </ng-container>

                <!-- Fecha Column -->
                <ng-container matColumnDef="fecha">
                  <th mat-header-cell *matHeaderCellDef>Fecha</th>
                  <td mat-cell *matCellDef="let element">{{ element.fecha | date: 'dd/MM/yyyy' }}</td>
                </ng-container>

                <!-- Horas Column -->
                <ng-container matColumnDef="horas">
                  <th mat-header-cell *matHeaderCellDef>Horas</th>
                  <td mat-cell *matCellDef="let element" class="monospace">{{ element.horasDuracion | number: '1.2-2' }}</td>
                </ng-container>

                <tr mat-header-row *matHeaderRowDef="visitasColumns"></tr>
                <tr mat-row *matRowDef="let element; columns: visitasColumns;"></tr>
              </table>
            </div>

            @if (visitasLoading()) {
              <div class="loading-container">
                <mat-spinner diameter="40"></mat-spinner>
              </div>
            }

            <mat-paginator
              [length]="visitasTotal()"
              [pageSize]="visitasPageSize()"
              [pageIndex]="visitasPage() - 1"
              [pageSizeOptions]="[10, 25, 50, 100]"
              showFirstLastButtons
              (page)="onVisitasPageChange($event)">
            </mat-paginator>
          </div>
        </mat-tab>

        <!-- CENTROS TAB -->
        <mat-tab label="Centros">
          <ng-template mat-tab-label>
            <mat-icon class="mr-8">store</mat-icon>
            Centros
          </ng-template>

          <div class="tab-content">
            <!-- KPI Cards -->
            <div class="kpi-grid">
              <div class="kpi-card">
                <div class="kpi-value">{{ centrosTotal() }}</div>
                <div class="kpi-label">TOTAL CENTROS</div>
              </div>
            </div>

            <!-- Search & Table -->
            <mat-form-field appearance="outline" class="search-field">
              <mat-label>Buscar por Centro, Provincia o Ciudad...</mat-label>
              <input matInput [(ngModel)]="centrosSearch" (ngModelChange)="onCentrosSearchChange($event)" />
            </mat-form-field>

            <div class="table-container">
              <table mat-table [dataSource]="centros()" class="full-width-table">
                <!-- Centro Column -->
                <ng-container matColumnDef="centro">
                  <th mat-header-cell *matHeaderCellDef>Centro</th>
                  <td mat-cell *matCellDef="let element">
                    <div>{{ element.centroNombre }}</div>
                    <div class="text-secondary" style="font-size: 12px;">{{ element.centroId }}</div>
                  </td>
                </ng-container>

                <!-- Provincia Column -->
                <ng-container matColumnDef="provincia">
                  <th mat-header-cell *matHeaderCellDef>Provincia</th>
                  <td mat-cell *matCellDef="let element">{{ element.provincia || '—' }}</td>
                </ng-container>

                <!-- Ciudad Column -->
                <ng-container matColumnDef="ciudad">
                  <th mat-header-cell *matHeaderCellDef>Ciudad</th>
                  <td mat-cell *matCellDef="let element">{{ element.ciudad || '—' }}</td>
                </ng-container>

                <tr mat-header-row *matHeaderRowDef="centrosColumns"></tr>
                <tr mat-row *matRowDef="let element; columns: centrosColumns;"></tr>
              </table>
            </div>

            @if (centrosLoading()) {
              <div class="loading-container">
                <mat-spinner diameter="40"></mat-spinner>
              </div>
            }

            <mat-paginator
              [length]="centrosTotal()"
              [pageSize]="centrosPageSize()"
              [pageIndex]="centrosPage() - 1"
              [pageSizeOptions]="[10, 25, 50, 100]"
              showFirstLastButtons
              (page)="onCentrosPageChange($event)">
            </mat-paginator>
          </div>
        </mat-tab>
      </mat-tab-group>
    </div>
  `,
  styles: [`
    .sig-page {
      padding: 24px;
    }

    .sig-page__header {
      margin-bottom: 24px;
    }

    .sig-page__title {
      margin: 0;
      font-size: 28px;
      font-weight: 500;
    }

    .tab-content {
      padding: 24px;
    }

    .kpi-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 16px;
      margin-bottom: 24px;
    }

    .kpi-card {
      background: var(--mat-sys-surface);
      border: 1px solid var(--mat-sys-outline-variant);
      border-radius: 8px;
      padding: 16px;
      text-align: center;
    }

    .kpi-value {
      font-size: 32px;
      font-weight: 600;
      color: var(--mat-sys-primary);
      margin-bottom: 8px;
    }

    .kpi-label {
      font-size: 12px;
      font-weight: 500;
      color: var(--mat-sys-on-surface-variant);
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .search-field {
      width: 100%;
      max-width: 500px;
      margin-bottom: 24px;
    }

    .table-container {
      overflow-x: auto;
      margin-bottom: 16px;
    }

    .full-width-table {
      width: 100%;
    }

    .monospace {
      font-family: monospace;
      font-weight: 500;
    }

    .badge-active {
      background-color: rgba(76, 175, 80, 0.2);
      color: #2e7d32;
      padding: 4px 8px;
      border-radius: 4px;
      font-size: 12px;
      font-weight: 500;
    }

    .badge-inactive {
      background-color: rgba(244, 67, 54, 0.2);
      color: #c62828;
      padding: 4px 8px;
      border-radius: 4px;
      font-size: 12px;
      font-weight: 500;
    }

    .text-secondary {
      color: var(--mat-sys-on-surface-variant);
    }

    .loading-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 200px;
    }

    .mr-8 {
      margin-right: 8px;
    }
  `],
})
export class SgpvDashboardComponent implements OnInit {
  private readonly sgpvService = inject(SgpvService);

  // Tab state
  selectedTab = signal(0);

  // Productos state
  productos = signal<SgpvProductoDto[]>([]);
  productosPage = signal(1);
  productosPageSize = signal(25);
  productosTotal = signal(0);
  productosLoading = signal(false);
  productosSearch = '';
  private productosSearchSubject = new Subject<string>();
  productosColumns = ['cliente', 'referencia', 'categoria', 'subcategoria', 'marca', 'activo'];

  // Computed KPIs for Productos
  productosCategories = computed(() => {
    const unique = new Set(this.productos().map((p) => p.categoria));
    return unique.size;
  });
  productosClientes = computed(() => {
    const unique = new Set(this.productos().map((p) => p.cliente));
    return unique.size;
  });
  productosActivos = computed(() => {
    return this.productos().filter((p) => p.activo).length;
  });

  // Visitas state
  visitas = signal<SgpvVisitaDashboardDto[]>([]);
  visitasPage = signal(1);
  visitasPageSize = signal(25);
  visitasTotal = signal(0);
  visitasLoading = signal(false);
  visitasSearch = '';
  private visitasSearchSubject = new Subject<string>();
  visitasColumns = ['nif', 'centro', 'servicio', 'fecha', 'horas'];

  // Computed KPIs for Visitas
  visitasEmpleados = computed(() => {
    const unique = new Set(this.visitas().map((v) => v.resourceNif));
    return unique.size;
  });
  visitasHoras = computed(() => {
    return this.visitas().reduce((sum, v) => sum + (v.horasDuracion || 0), 0);
  });

  // Centros state
  centros = signal<SgpvCentroDashboardDto[]>([]);
  centrosPage = signal(1);
  centrosPageSize = signal(25);
  centrosTotal = signal(0);
  centrosLoading = signal(false);
  centrosSearch = '';
  private centrosSearchSubject = new Subject<string>();
  centrosColumns = ['centro', 'provincia', 'ciudad'];

  constructor() {
    // Debounce searches
    this.productosSearchSubject
      .pipe(debounceTime(300), takeUntilDestroyed())
      .subscribe(() => {
        this.productosPage.set(1);
        this.loadProductos();
      });

    this.visitasSearchSubject
      .pipe(debounceTime(300), takeUntilDestroyed())
      .subscribe(() => {
        this.visitasPage.set(1);
        this.loadVisitas();
      });

    this.centrosSearchSubject
      .pipe(debounceTime(300), takeUntilDestroyed())
      .subscribe(() => {
        this.centrosPage.set(1);
        this.loadCentros();
      });

    // Load data when tab changes
    effect(() => {
      const tab = this.selectedTab();
      switch (tab) {
        case 0:
          if (this.productosTotal() === 0) this.loadProductos();
          break;
        case 1:
          if (this.visitasTotal() === 0) this.loadVisitas();
          break;
        case 2:
          if (this.centrosTotal() === 0) this.loadCentros();
          break;
      }
    });
  }

  ngOnInit(): void {
    this.loadProductos();
  }

  // Productos methods
  private loadProductos(): void {
    this.productosLoading.set(true);
    this.sgpvService
      .getProductos(this.productosPage(), this.productosPageSize(), this.productosSearch || undefined)
      .subscribe({
        next: (res) => {
          this.productos.set(res.items);
          this.productosTotal.set(res.total);
          this.productosLoading.set(false);
          window.scrollTo({ top: 0, behavior: 'smooth' });
        },
        error: (err) => {
          console.error('Error loading productos:', err);
          this.productosLoading.set(false);
        },
      });
  }

  onProductosSearchChange(search: string): void {
    this.productosSearch = search;
    this.productosSearchSubject.next(search);
  }

  onProductosPageChange(event: PageEvent): void {
    this.productosPage.set(event.pageIndex + 1);
    this.productosPageSize.set(event.pageSize);
    this.loadProductos();
  }

  // Visitas methods
  private loadVisitas(): void {
    this.visitasLoading.set(true);
    this.sgpvService
      .getVisitas(this.visitasPage(), this.visitasPageSize(), this.visitasSearch || undefined)
      .subscribe({
        next: (res) => {
          this.visitas.set(res.items);
          this.visitasTotal.set(res.total);
          this.visitasLoading.set(false);
          window.scrollTo({ top: 0, behavior: 'smooth' });
        },
        error: (err) => {
          console.error('Error loading visitas:', err);
          this.visitasLoading.set(false);
        },
      });
  }

  onVisitasSearchChange(search: string): void {
    this.visitasSearch = search;
    this.visitasSearchSubject.next(search);
  }

  onVisitasPageChange(event: PageEvent): void {
    this.visitasPage.set(event.pageIndex + 1);
    this.visitasPageSize.set(event.pageSize);
    this.loadVisitas();
  }

  // Centros methods
  private loadCentros(): void {
    this.centrosLoading.set(true);
    this.sgpvService
      .getCentros(this.centrosPage(), this.centrosPageSize(), this.centrosSearch || undefined)
      .subscribe({
        next: (res) => {
          this.centros.set(res.items);
          this.centrosTotal.set(res.total);
          this.centrosLoading.set(false);
          window.scrollTo({ top: 0, behavior: 'smooth' });
        },
        error: (err) => {
          console.error('Error loading centros:', err);
          this.centrosLoading.set(false);
        },
      });
  }

  onCentrosSearchChange(search: string): void {
    this.centrosSearch = search;
    this.centrosSearchSubject.next(search);
  }

  onCentrosPageChange(event: PageEvent): void {
    this.centrosPage.set(event.pageIndex + 1);
    this.centrosPageSize.set(event.pageSize);
    this.loadCentros();
  }
}
