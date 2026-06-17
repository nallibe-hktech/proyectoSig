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
import { MatPaginatorModule } from '@angular/material/paginator';
import { debounceTime, Subject } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MediapostService, MediapostPedidoDto, MediapostRecepcionDto } from '../services/mediapost.service';
import { SyncService } from '../../../core/api/misc.service';
import { NotifyService } from '../../../core/notify.service';

interface FileType {
  key: string;
  label: string;
  pattern: string;
  icon: string;
}

// Using DTOs from service instead of local interfaces
type MediapostPedido = MediapostPedidoDto;
type MediapostRecepcion = MediapostRecepcionDto;

// @ts-ignore - Template context variables in matRowDef not recognized by type checker
@Component({
  selector: 'app-mediapost-dashboard',
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
    MatPaginatorModule,
  ],
  template: `
    <div class="sig-page">
      <div class="page-header">
        <h1>Distribución — Mediapost</h1>
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
          <div class="kpi-value">{{ pedidosCount() }}</div>
          <div class="kpi-label">PEDIDOS TOTALES</div>
        </div>
        <div class="kpi-card">
          <div class="kpi-value">{{ pedidosEntregados() }}</div>
          <div class="kpi-label">ENTREGADOS</div>
        </div>
        <div class="kpi-card">
          <div class="kpi-value">{{ recepcionesCount() }}</div>
          <div class="kpi-label">RECEPCIONES</div>
        </div>
      </div>

      <!-- Upload Zone -->
      <div class="upload-section">
        <h3>Importar ficheros</h3>
        <p class="upload-info">Carga ficheros Excel de pedidos y recepciones. Se procesarán automáticamente.</p>

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
        <!-- Pedidos Tab -->
        <mat-tab label="Pedidos">
          <div class="tab-content">
            <div class="filter-controls">
              <mat-form-field appearance="fill">
                <mat-label>Buscar pedido</mat-label>
                <input matInput placeholder="Documento, referencia..." (input)="onPedidosSearch($event)" />
                <mat-icon matSuffix>search</mat-icon>
              </mat-form-field>

              <mat-form-field appearance="fill">
                <mat-label>Estado</mat-label>
                <select matNativeControl (change)="onEstadoFilter($event)">
                  <option value="">Todos</option>
                  <option value="Entregado">Entregado</option>
                  <option value="Pendiente">Pendiente</option>
                  <option value="En tránsito">En tránsito</option>
                </select>
              </mat-form-field>
            </div>

            @if (pedidosLoading()) {
              <div class="spinner-container">
                <mat-spinner diameter="48"></mat-spinner>
              </div>
            } @else if (pedidos().length === 0) {
              <div class="empty-state">
                <p>Sin datos sincronizados</p>
              </div>
            } @else {
              <table mat-table [dataSource]="pedidos()" class="data-table">
                <ng-container matColumnDef="pedidoId">
                  <th mat-header-cell>Nº Pedido</th>
                  <td mat-cell *matCellDef="let row">{{ row.pedidoId }}</td>
                </ng-container>

                <ng-container matColumnDef="referenciaPedido">
                  <th mat-header-cell>Referencia</th>
                  <td mat-cell *matCellDef="let row">{{ row.referenciaPedido }}</td>
                </ng-container>

                <ng-container matColumnDef="fechaPedido">
                  <th mat-header-cell>Fecha</th>
                  <td mat-cell *matCellDef="let row">{{ row.fechaPedido | date: 'short' }}</td>
                </ng-container>

                <ng-container matColumnDef="estado">
                  <th mat-header-cell>Estado</th>
                  <td mat-cell *matCellDef="let row">{{ row.estado }}</td>
                </ng-container>

                <ng-container matColumnDef="destinatarioNombre">
                  <th mat-header-cell>Destinatario</th>
                  <td mat-cell *matCellDef="let row">{{ row.destinatarioNombre }}</td>
                </ng-container>

                <tr mat-header-row></tr>
                <tr mat-row *matRowDef="let row; columns: pedidosColumns"></tr>
              </table>
            }
          </div>
        </mat-tab>

        <!-- Recepciones Tab -->
        <mat-tab label="Recepciones">
          <div class="tab-content">
            <div class="search-box">
              <mat-form-field appearance="fill">
                <mat-label>Buscar recepción</mat-label>
                <input matInput placeholder="Nº Recepción, cliente..." (input)="onRecepcionesSearch($event)" />
                <mat-icon matSuffix>search</mat-icon>
              </mat-form-field>
            </div>

            @if (recepcionesLoading()) {
              <div class="spinner-container">
                <mat-spinner diameter="48"></mat-spinner>
              </div>
            } @else if (recepciones().length === 0) {
              <div class="empty-state">
                <p>Sin datos sincronizados</p>
              </div>
            } @else {
              <table mat-table [dataSource]="recepciones()" class="data-table">
                <ng-container matColumnDef="recepcionId">
                  <th mat-header-cell>Nº Recepción</th>
                  <td mat-cell *matCellDef="let row">{{ row.recepcionId }}</td>
                </ng-container>

                <ng-container matColumnDef="codigoArticulo">
                  <th mat-header-cell>Artículo</th>
                  <td mat-cell *matCellDef="let row">{{ row.codigoArticulo }}</td>
                </ng-container>

                <ng-container matColumnDef="fechaRecepcion">
                  <th mat-header-cell>Fecha</th>
                  <td mat-cell *matCellDef="let row">{{ row.fechaRecepcion | date: 'short' }}</td>
                </ng-container>

                <ng-container matColumnDef="cantidad">
                  <th mat-header-cell>Cantidad</th>
                  <td mat-cell *matCellDef="let row">{{ row.cantidad }}</td>
                </ng-container>

                <ng-container matColumnDef="cantidadDanada">
                  <th mat-header-cell>Dañada</th>
                  <td mat-cell *matCellDef="let row">{{ row['cantidadDañada'] || 0 }}</td>
                </ng-container>

                <ng-container matColumnDef="estado">
                  <th mat-header-cell>Estado</th>
                  <td mat-cell *matCellDef="let row">{{ row.estado }}</td>
                </ng-container>

                <tr mat-header-row></tr>
                <tr mat-row *matRowDef="let row; columns: recepcionesColumns"></tr>
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
      background: var(--sig-bg-card);
      border-radius: 8px;
      padding: 20px;
      text-align: center;
      border: 1px solid var(--sig-border);

      .kpi-value {
        font-size: 32px;
        font-weight: 600;
        color: var(--sig-blue);
        margin-bottom: 8px;
      }

      .kpi-label {
        font-size: 12px;
        color: var(--sig-text-muted);
        text-transform: uppercase;
        letter-spacing: 0.5px;
      }
    }

    .upload-section {
      margin-bottom: 40px;
      padding: 20px;
      background: var(--sig-bg-card);
      border-radius: 8px;
      border: 1px solid var(--sig-border);

      h3 {
        margin-top: 0;
        font-size: 16px;
        font-weight: 600;
        color: var(--sig-text-heading);
      }

      .upload-info {
        margin: 10px 0 20px 0;
        font-size: 14px;
        color: var(--sig-text-secondary);
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
      padding: 20px;
      background: var(--sig-bg-card);
      border-radius: 8px;
    }

    .filter-controls {
      display: flex;
      gap: 16px;
      margin-bottom: 20px;
      flex-wrap: wrap;

      mat-form-field {
        min-width: 250px;
      }
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
        background-color: var(--sig-bg-header);
        padding: 12px;
        text-align: left;
        font-weight: 600;
        font-size: 12px;
        color: var(--sig-text-muted);
        border-bottom: 1px solid var(--sig-border);
      }

      td {
        padding: 12px;
        border-bottom: 1px solid var(--sig-border);
      }

      tr:hover {
        background-color: var(--sig-bg-hover);
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
export class MediapostDashboardComponent implements OnInit {
  private readonly mediapostSvc = inject(MediapostService);
  private readonly syncSvc = inject(SyncService);
  private readonly notify = inject(NotifyService);

  fileTypes: FileType[] = [
    { key: 'pedidos', label: 'Pedidos', pattern: 'infpedsit11_*.xlsx', icon: 'receipt' },
    { key: 'recepciones', label: 'Recepciones', pattern: 'infrecep07_*.xlsx', icon: 'check_circle' },
  ];

  pedidos = signal<MediapostPedido[]>([]);
  recepciones = signal<MediapostRecepcion[]>([]);

  pedidosLoading = signal(false);
  recepcionesLoading = signal(false);
  syncing = signal(false);

  uploadingTypes: { [key: string]: boolean } = {};
  uploadStatus: { [key: string]: string } = {};

  pedidosColumns = ['pedidoId', 'referenciaPedido', 'fechaPedido', 'estado', 'destinatarioNombre'];
  recepcionesColumns = ['recepcionId', 'codigoArticulo', 'fechaRecepcion', 'cantidad', 'cantidadDanada', 'estado'];

  pedidosCount = computed(() => this.pedidos().length);
  recepcionesCount = computed(() => this.recepciones().length);
  pedidosEntregados = computed(() =>
    this.pedidos().filter(p => p.estado?.toLowerCase().includes('entregado')).length
  );

  private pedidosSearch$ = new Subject<string>();
  private recepcionesSearch$ = new Subject<string>();
  private estadoFilter$ = '';

  constructor() {
    this.pedidosSearch$
      .pipe(debounceTime(300), takeUntilDestroyed())
      .subscribe((search) => this.loadPedidos(search));

    this.recepcionesSearch$
      .pipe(debounceTime(300), takeUntilDestroyed())
      .subscribe((search) => this.loadRecepciones(search));
  }

  ngOnInit() {
    this.loadPedidos();
    this.loadRecepciones();
  }

  onPedidosSearch(event: Event) {
    const search = (event.target as HTMLInputElement).value;
    this.pedidosSearch$.next(search);
  }

  onRecepcionesSearch(event: Event) {
    const search = (event.target as HTMLInputElement).value;
    this.recepcionesSearch$.next(search);
  }

  onEstadoFilter(event: Event) {
    const select = event.target as HTMLSelectElement;
    this.estadoFilter$ = select.value;
    this.loadPedidos();
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

    this.mediapostSvc.uploadFile(tipoKey, file).subscribe({
      next: () => {
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

  private loadPedidos(search?: string) {
    this.pedidosLoading.set(true);
    this.mediapostSvc.getPedidos(1, 500, search || '', this.estadoFilter$ || '').subscribe({
      next: (response) => {
        this.pedidos.set(response.items || []);
        this.pedidosLoading.set(false);
      },
      error: () => {
        this.pedidos.set([]);
        this.pedidosLoading.set(false);
      },
    });
  }

  private loadRecepciones(search?: string) {
    this.recepcionesLoading.set(true);
    this.mediapostSvc.getRecepciones(1, 500, search || '').subscribe({
      next: (response) => {
        this.recepciones.set(response.items || []);
        this.recepcionesLoading.set(false);
      },
      error: () => {
        this.recepciones.set([]);
        this.recepcionesLoading.set(false);
      },
    });
  }

  protected syncManual(): void {
    this.syncing.set(true);
    this.syncSvc.sync('mediapost').subscribe({
      next: (r) => {
        this.syncing.set(false);
        this.notify.success(`Sincronizado: ${r.registrosInsertados} registros nuevos`);
        this.loadPedidos();
        this.loadRecepciones();
      },
      error: (err) => {
        this.syncing.set(false);
        this.notify.error(err?.error?.title ?? 'No se pudo sincronizar');
      },
    });
  }
}
