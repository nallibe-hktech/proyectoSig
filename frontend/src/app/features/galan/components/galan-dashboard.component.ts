import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTableModule } from '@angular/material/table';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { HttpClient } from '@angular/common/http';
import { debounceTime, Subject } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { GalanService, GalanEntradaDto, GalanSalidaDto, GalanStockDto } from '../services/galan.service';
import { SyncService } from '../../../core/api/misc.service';
import { NotifyService } from '../../../core/notify.service';

interface FileType {
  key: string;
  label: string;
  pattern: string;
  icon: string;
}

// Using DTOs from service instead of local interfaces
type GalanEntrada = GalanEntradaDto;
type GalanSalida = GalanSalidaDto;
type GalanStock = GalanStockDto;

// @ts-ignore - Template context variables in matRowDef not recognized by type checker
@Component({
  selector: 'app-galan-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTabsModule,
    MatTableModule,
    MatInputModule,
    MatFormFieldModule,
    MatProgressSpinnerModule,
    MatProgressBarModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatDividerModule,
  ],
  template: `
    <div class="sig-page">
      <div class="page-header">
        <h1>Logística — Galán</h1>
        <button mat-stroked-button (click)="syncManual()" [disabled]="syncing()">
          @if (syncing()) {
            <mat-spinner diameter="18"></mat-spinner>
          } @else {
            <mat-icon>sync</mat-icon>
          }
          Sincronizar
        </button>
      </div>

      <!-- KPI Cards -->
      <div class="kpi-grid">
        <div class="kpi-card">
          <div class="kpi-value">{{ entradasCount() }}</div>
          <div class="kpi-label">ENTRADAS</div>
        </div>
        <div class="kpi-card">
          <div class="kpi-value">{{ salidasCount() }}</div>
          <div class="kpi-label">SALIDAS</div>
        </div>
        <div class="kpi-card">
          <div class="kpi-value">{{ stockCount() | number: '0.0-0' }}</div>
          <div class="kpi-label">STOCK (UDS)</div>
        </div>
      </div>

      <!-- Upload Zone -->
      <div class="upload-section">
        <h3>Importar ficheros</h3>
        <p class="upload-info">Carga ficheros Excel mensuales. Se procesarán automáticamente al subirlos.</p>

        <div class="file-types-grid">
          @for (fileType of fileTypes; track fileType.key) {
            <div class="file-card" (dragover)="onDragOver($event)" (drop)="onDrop($event, fileType.key)">
              <mat-icon class="file-icon">{{ fileType.icon }}</mat-icon>
              <div class="file-label">{{ fileType.label }}</div>
              <div class="file-pattern">{{ fileType.pattern }}</div>

              <div class="upload-input-wrapper">
                <input
                  type="file"
                  accept=".xlsx"
                  #fileInput
                  [id]="'file-' + fileType.key"
                  (change)="onFileSelected($event, fileType.key)"
                  style="display: none"
                />
                <button
                  mat-raised-button
                  color="primary"
                  (click)="fileInput.click()"
                  class="upload-btn"
                >
                  <mat-icon>upload</mat-icon>
                  Subir fichero
                </button>
              </div>

              @if (uploadingTypes[fileType.key]) {
                <mat-progress-bar mode="indeterminate" class="upload-progress"></mat-progress-bar>
              } @else if (uploadStatus[fileType.key]) {
                <div class="upload-status">
                  <mat-icon class="success-icon">check_circle</mat-icon>
                  {{ uploadStatus[fileType.key] }}
                </div>
              }
            </div>
          }
        </div>
      </div>

      <!-- Tabs con datos -->
      <mat-tab-group>
        <!-- Entradas Tab -->
        <mat-tab label="Entradas">
          <div class="tab-content">
            <div class="search-box">
              <mat-form-field appearance="fill">
                <mat-label>Buscar entrada</mat-label>
                <input matInput placeholder="Código, descripción..." (input)="onEntradasSearch($event)" />
                <mat-icon matSuffix>search</mat-icon>
              </mat-form-field>
            </div>

            @if (entradasLoading()) {
              <div class="spinner-container">
                <mat-spinner diameter="48"></mat-spinner>
              </div>
            } @else if (entradas().length === 0) {
              <div class="empty-state">
                <p>Sin datos sincronizados</p>
              </div>
            } @else {
              <table mat-table [dataSource]="entradas()" class="data-table">
                <ng-container matColumnDef="codigoArticulo">
                  <th mat-header-cell>Código</th>
                  <td mat-cell>{{ $any(row).codigoArticulo }}</td>
                </ng-container>

                <ng-container matColumnDef="descripcion">
                  <th mat-header-cell>Descripción</th>
                  <td mat-cell>{{ $any(row).descripcion }}</td>
                </ng-container>

                <ng-container matColumnDef="unidades">
                  <th mat-header-cell>Unidades</th>
                  <td mat-cell>{{ $any(row).unidades | number: '0.0-0' }}</td>
                </ng-container>

                <ng-container matColumnDef="fecha">
                  <th mat-header-cell>Fecha</th>
                  <td mat-cell>{{ $any(row).fecha | date: 'short' }}</td>
                </ng-container>

                <ng-container matColumnDef="almacen">
                  <th mat-header-cell>Almacén</th>
                  <td mat-cell>{{ $any(row).almacen }}</td>
                </ng-container>

                <tr mat-header-row></tr>
                <tr mat-row *matRowDef="let row; columns: entradasColumns"></tr>
              </table>
            }
          </div>
        </mat-tab>

        <!-- Salidas Tab -->
        <mat-tab label="Salidas">
          <div class="tab-content">
            <div class="search-box">
              <mat-form-field appearance="fill">
                <mat-label>Buscar salida</mat-label>
                <input matInput placeholder="Albarán, código..." (input)="onSalidasSearch($event)" />
                <mat-icon matSuffix>search</mat-icon>
              </mat-form-field>
            </div>

            @if (salidasLoading()) {
              <div class="spinner-container">
                <mat-spinner diameter="48"></mat-spinner>
              </div>
            } @else if (salidas().length === 0) {
              <div class="empty-state">
                <p>Sin datos sincronizados</p>
              </div>
            } @else {
              <table mat-table [dataSource]="salidas()" class="data-table">
                <ng-container matColumnDef="albaran">
                  <th mat-header-cell>Albarán</th>
                  <td mat-cell>{{ $any(row).albaran }}</td>
                </ng-container>

                <ng-container matColumnDef="codigoArticulo">
                  <th mat-header-cell>Código</th>
                  <td mat-cell>{{ $any(row).codigoArticulo }}</td>
                </ng-container>

                <ng-container matColumnDef="descripcion">
                  <th mat-header-cell>Descripción</th>
                  <td mat-cell>{{ $any(row).descripcion }}</td>
                </ng-container>

                <ng-container matColumnDef="unidades">
                  <th mat-header-cell>Unidades</th>
                  <td mat-cell>{{ $any(row).unidades | number: '0.0-0' }}</td>
                </ng-container>

                <ng-container matColumnDef="fecha">
                  <th mat-header-cell>Fecha</th>
                  <td mat-cell>{{ $any(row).fecha | date: 'short' }}</td>
                </ng-container>

                <tr mat-header-row></tr>
                <tr mat-row *matRowDef="let row; columns: salidasColumns"></tr>
              </table>
            }
          </div>
        </mat-tab>

        <!-- Stock Tab -->
        <mat-tab label="Stock">
          <div class="tab-content">
            <div class="search-box">
              <mat-form-field appearance="fill">
                <mat-label>Buscar artículo</mat-label>
                <input matInput placeholder="Código, descripción..." (input)="onStockSearch($event)" />
                <mat-icon matSuffix>search</mat-icon>
              </mat-form-field>
            </div>

            @if (stockLoading()) {
              <div class="spinner-container">
                <mat-spinner diameter="48"></mat-spinner>
              </div>
            } @else if (stock().length === 0) {
              <div class="empty-state">
                <p>Sin datos sincronizados</p>
              </div>
            } @else {
              <table mat-table [dataSource]="stock()" class="data-table">
                <ng-container matColumnDef="codigoArticulo">
                  <th mat-header-cell>Código</th>
                  <td mat-cell>{{ $any(row).codigoArticulo }}</td>
                </ng-container>

                <ng-container matColumnDef="descripcion">
                  <th mat-header-cell>Descripción</th>
                  <td mat-cell>{{ $any(row).descripcion }}</td>
                </ng-container>

                <ng-container matColumnDef="stock">
                  <th mat-header-cell>Stock</th>
                  <td mat-cell>{{ $any(row).stock | number: '0.0-0' }}</td>
                </ng-container>

                <ng-container matColumnDef="familia">
                  <th mat-header-cell>Familia</th>
                  <td mat-cell>{{ $any(row).familia }}</td>
                </ng-container>

                <ng-container matColumnDef="almacen">
                  <th mat-header-cell>Almacén</th>
                  <td mat-cell>{{ $any(row).almacen }}</td>
                </ng-container>

                <tr mat-header-row></tr>
                <tr mat-row *matRowDef="let row; columns: stockColumns"></tr>
              </table>
            }
          </div>
        </mat-tab>
      </mat-tab-group>
    </div>
  `,
  styles: `
    .sig-page {
      padding: 20px;
    }

    .page-header {
      margin-bottom: 30px;
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 16px;

      h1 {
        margin: 0;
        font-size: 28px;
        font-weight: 500;
      }

      button {
        display: flex;
        align-items: center;
        gap: 8px;
      }

      mat-spinner {
        display: inline-block;
      }
    }

    .kpi-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 16px;
      margin-bottom: 40px;
    }

    .kpi-card {
      background: #f5f5f5;
      border-radius: 8px;
      padding: 20px;
      text-align: center;

      .kpi-value {
        font-size: 32px;
        font-weight: 600;
        color: #1976d2;
        margin-bottom: 8px;
      }

      .kpi-label {
        font-size: 12px;
        color: #999;
        text-transform: uppercase;
        letter-spacing: 0.5px;
      }
    }

    .upload-section {
      margin-bottom: 40px;
      padding: 20px;
      background: #fafafa;
      border-radius: 8px;

      h3 {
        margin-top: 0;
        font-size: 16px;
        font-weight: 600;
      }

      .upload-info {
        margin: 10px 0 20px 0;
        font-size: 14px;
        color: #666;
      }
    }

    .file-types-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
      gap: 16px;
    }

    .file-card {
      background: white;
      border: 2px dashed #ddd;
      border-radius: 8px;
      padding: 20px;
      text-align: center;
      transition: all 0.2s ease;
      cursor: pointer;

      &:hover {
        border-color: #1976d2;
        background: #f0f7ff;
      }

      &.drag-over {
        border-color: #1976d2;
        background: #e3f2fd;
      }

      .file-icon {
        font-size: 48px;
        width: 48px;
        height: 48px;
        color: #1976d2;
        margin: 0 auto 12px;
      }

      .file-label {
        font-weight: 600;
        font-size: 14px;
        margin-bottom: 4px;
      }

      .file-pattern {
        font-size: 12px;
        color: #999;
        margin-bottom: 16px;
      }

      .upload-btn {
        width: 100%;
      }

      .upload-progress {
        margin-top: 12px;
      }

      .upload-status {
        margin-top: 12px;
        color: #4caf50;
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 8px;
        font-size: 12px;

        .success-icon {
          font-size: 20px;
          width: 20px;
          height: 20px;
        }
      }
    }

    .tab-content {
      padding: 20px 0;
    }

    .search-box {
      margin-bottom: 20px;

      mat-form-field {
        width: 100%;
        max-width: 400px;
      }
    }

    .data-table {
      width: 100%;
      border-collapse: collapse;

      th {
        background-color: #f5f5f5;
        padding: 12px;
        text-align: left;
        font-weight: 600;
        font-size: 12px;
        color: #666;
        border-bottom: 1px solid #e0e0e0;
      }

      td {
        padding: 12px;
        border-bottom: 1px solid #f0f0f0;
      }

      tr:hover {
        background-color: #fafafa;
      }
    }

    .spinner-container {
      display: flex;
      justify-content: center;
      padding: 40px 20px;
    }

    .empty-state {
      text-align: center;
      padding: 60px 20px;
      color: #999;

      p {
        margin: 0;
        font-size: 16px;
      }
    }

    mat-tab-group ::ng-deep {
      .mat-mdc-tab-labels {
        border-bottom: 1px solid #e0e0e0;
      }
    }
  `,
})
export class GalanDashboardComponent implements OnInit {
  private readonly galanSvc = inject(GalanService);
  private readonly http = inject(HttpClient);
  private readonly syncSvc = inject(SyncService);
  private readonly notify = inject(NotifyService);

  // Dummy property for template type checking
  protected row: GalanEntrada | GalanSalida | GalanStock | any;

  fileTypes: FileType[] = [
    { key: 'facturas', label: 'Facturación transporte', pattern: 'FACT_MENSUAL_*.xlsx', icon: 'receipt' },
    { key: 'salidas', label: 'Salidas', pattern: 'Salidas_*.xlsx', icon: 'local_shipping' },
    { key: 'stock', label: 'Stock', pattern: 'STOCK_*.xlsx', icon: 'inventory' },
    { key: 'almacenaje', label: 'Almacenaje', pattern: 'ALMACENAJE SIG *.xlsx', icon: 'warehouse' },
    { key: 'entradas', label: 'Entradas', pattern: 'Entradas_*.xlsx', icon: 'input' },
    { key: 'stock-celda', label: 'Stock por celda', pattern: 'STOCK_celda_*.xlsx', icon: 'grid_3x3' },
  ];

  entradas = signal<GalanEntrada[]>([]);
  salidas = signal<GalanSalida[]>([]);
  stock = signal<GalanStock[]>([]);

  entradasLoading = signal(false);
  salidasLoading = signal(false);
  stockLoading = signal(false);
  syncing = signal(false);

  uploadingTypes: { [key: string]: boolean } = {};
  uploadStatus: { [key: string]: string } = {};

  entradasColumns = ['codigoArticulo', 'descripcion', 'unidades', 'fecha', 'almacen'];
  salidasColumns = ['albaran', 'codigoArticulo', 'descripcion', 'unidades', 'fecha'];
  stockColumns = ['codigoArticulo', 'descripcion', 'stock', 'familia', 'almacen'];

  entradasCount = computed(() => this.entradas().length);
  salidasCount = computed(() => this.salidas().length);
  stockCount = computed(() => this.stock().reduce((sum, s) => sum + s.stock, 0));

  private entradasSearch$ = new Subject<string>();
  private salidasSearch$ = new Subject<string>();
  private stockSearch$ = new Subject<string>();

  constructor() {
    this.entradasSearch$
      .pipe(debounceTime(300), takeUntilDestroyed())
      .subscribe((search) => this.loadEntradas(search));

    this.salidasSearch$
      .pipe(debounceTime(300), takeUntilDestroyed())
      .subscribe((search) => this.loadSalidas(search));

    this.stockSearch$
      .pipe(debounceTime(300), takeUntilDestroyed())
      .subscribe((search) => this.loadStock(search));
  }

  ngOnInit() {
    this.loadEntradas();
    this.loadSalidas();
    this.loadStock();
  }

  onEntradasSearch(event: Event) {
    const search = (event.target as HTMLInputElement).value;
    this.entradasSearch$.next(search);
  }

  onSalidasSearch(event: Event) {
    const search = (event.target as HTMLInputElement).value;
    this.salidasSearch$.next(search);
  }

  onStockSearch(event: Event) {
    const search = (event.target as HTMLInputElement).value;
    this.stockSearch$.next(search);
  }

  onDragOver(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
  }

  onDrop(event: DragEvent, tipoKey: string) {
    event.preventDefault();
    event.stopPropagation();
    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.uploadFile(files[0], tipoKey);
    }
  }

  onFileSelected(event: Event, tipoKey: string) {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.uploadFile(input.files[0], tipoKey);
    }
  }

  private uploadFile(file: File, tipoKey: string) {
    if (!file.name.endsWith('.xlsx')) {
      this.notify.error('Solo se aceptan archivos .xlsx');
      return;
    }

    this.uploadingTypes[tipoKey] = true;
    const formData = new FormData();
    formData.append('file', file);

    this.http.post(`/api/galan/upload?tipo=${tipoKey}`, formData).subscribe({
      next: (response: any) => {
        this.uploadingTypes[tipoKey] = false;
        this.uploadStatus[tipoKey] = `✓ Cargado: ${file.name}`;
        setTimeout(() => {
          delete this.uploadStatus[tipoKey];
        }, 5000);
        // Sincronizar automáticamente después del upload
        this.syncManual();
      },
      error: (err) => {
        this.uploadingTypes[tipoKey] = false;
        this.notify.error('Error al cargar archivo: ' + (err.error?.error || err.message));
      },
    });
  }

  private loadEntradas(search?: string) {
    this.entradasLoading.set(true);
    this.galanSvc.getEntradas(1, 500, search || '').subscribe({
      next: (response) => {
        this.entradas.set(response.items || []);
        this.entradasLoading.set(false);
      },
      error: () => {
        this.entradas.set([]);
        this.entradasLoading.set(false);
      },
    });
  }

  private loadSalidas(search?: string) {
    this.salidasLoading.set(true);
    this.galanSvc.getSalidas(1, 500, search || '').subscribe({
      next: (response) => {
        this.salidas.set(response.items || []);
        this.salidasLoading.set(false);
      },
      error: () => {
        this.salidas.set([]);
        this.salidasLoading.set(false);
      },
    });
  }

  private loadStock(search?: string) {
    this.stockLoading.set(true);
    this.galanSvc.getStock().subscribe({
      next: (response) => {
        this.stock.set(response || []);
        this.stockLoading.set(false);
      },
      error: () => {
        this.stock.set([]);
        this.stockLoading.set(false);
      },
    });
  }

  protected syncManual(): void {
    this.syncing.set(true);
    this.syncSvc.sync('galan').subscribe({
      next: (r) => {
        this.syncing.set(false);
        this.notify.success(`Sincronizado: ${r.registrosInsertados} registros nuevos`);
        this.loadEntradas();
        this.loadSalidas();
        this.loadStock();
      },
      error: (err) => {
        this.syncing.set(false);
        this.notify.error(err?.error?.title ?? 'No se pudo sincronizar');
      },
    });
  }
}
