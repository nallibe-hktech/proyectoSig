import { Component, inject, signal, OnInit, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BreadcrumbsComponent } from '../../../shared/breadcrumbs.component';
import { NotifyService } from '../../../core/notify.service';
import { SyncService } from '../../../core/api/misc.service';
import { ServiceService } from '../../../core/api/services.service';
import { TravelPerkService, TravelPerkLineaDto, TravelPerkKpisDto } from '../../../core/api/travelperk.service';

@Component({
  selector: 'app-travelperk-dashboard',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatTableModule, MatInputModule, MatFormFieldModule, MatProgressSpinnerModule, MatProgressBarModule,
    MatIconModule, MatButtonModule, MatCheckboxModule, MatTooltipModule, MatPaginatorModule,
    BreadcrumbsComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Viajes Travel Perk' }]" />

      <div class="page-header">
        <h1>Viajes — Travel Perk</h1>
        <button mat-stroked-button (click)="sincronizar()" [disabled]="syncing()">
          @if (syncing()) {
            <mat-spinner diameter="18"></mat-spinner>
          } @else {
            <mat-icon>sync</mat-icon>
          }
          Sincronizar
        </button>
      </div>

      <p class="page-info">
        Costes de viaje importados del Excel de Travel Perk (SharePoint). Cada línea con CECO se imputa al
        servicio del cliente; las líneas sin CECO son gasto propio de SIG (CECO 0423).
      </p>

      <!-- Upload Zone (mismo patrón que Galán/Mediapost) -->
      <div class="upload-section">
        <h3>Importar ficheros</h3>
        <p class="upload-info">Carga el fichero Excel descargado de Travel Perk. Se procesará automáticamente.</p>

        <div class="file-types-grid">
          <div class="file-card" [class.drag-over]="dragOver()"
               (dragover)="onDragOver($event)" (dragleave)="onDragLeave($event)" (drop)="onDrop($event)">
            <mat-icon class="file-icon">flight_takeoff</mat-icon>
            <div class="file-label">Viajes Travel Perk</div>
            <div class="file-pattern">TravelPerk_*.xlsx</div>

            <div class="upload-input-wrapper">
              <input type="file" accept=".xlsx" #fileInput (change)="onFileSelected($event)" style="display: none" />
              <button mat-raised-button color="primary" (click)="fileInput.click()" class="upload-btn" [disabled]="uploading()">
                <mat-icon>upload</mat-icon>
                Subir fichero
              </button>
            </div>

            @if (uploading()) {
              <mat-progress-bar mode="indeterminate" class="upload-progress"></mat-progress-bar>
            } @else if (uploadStatus()) {
              <div class="upload-status">
                <mat-icon class="success-icon">check_circle</mat-icon>
                {{ uploadStatus() }}
              </div>
            }
          </div>
        </div>
      </div>

      <!-- KPI Cards -->
      <div class="kpi-grid">
        <div class="kpi-card">
          <div class="kpi-value">{{ kpis().totalLineas }}</div>
          <div class="kpi-label">LÍNEAS TOTALES</div>
        </div>
        <div class="kpi-card">
          <div class="kpi-value">{{ kpis().totalSinIVA | number: '1.2-2' }} €</div>
          <div class="kpi-label">COSTE TOTAL (SIN IVA)</div>
        </div>
        <div class="kpi-card">
          <div class="kpi-value">{{ kpis().costeImputado | number: '1.2-2' }} €</div>
          <div class="kpi-label">IMPUTADO A CLIENTES ({{ kpis().lineasImputadas }})</div>
        </div>
        <div class="kpi-card">
          <div class="kpi-value">{{ kpis().costeGastoInternoSig | number: '1.2-2' }} €</div>
          <div class="kpi-label">GASTO SIG · 0423 ({{ kpis().lineasGastoInternoSig }})</div>
        </div>
        <div class="kpi-card" [class.kpi-alert]="kpis().lineasCecoNoMaestro > 0">
          <div class="kpi-value">{{ kpis().lineasCecoNoMaestro }}</div>
          <div class="kpi-label">CECO SIN IMPUTAR</div>
        </div>
      </div>

      <!-- Filtros -->
      <div class="filter-controls">
        <mat-form-field appearance="fill">
          <mat-label>Buscar</mat-label>
          <input matInput placeholder="Servicio, Trip ID, CECO, email…" (input)="onSearch($event)" />
          <mat-icon matSuffix>search</mat-icon>
        </mat-form-field>
        <mat-checkbox [(ngModel)]="soloNoMaestro" (change)="onFilterToggle()">
          Solo CECO sin imputar
        </mat-checkbox>
      </div>

      @if (loading()) {
        <div class="spinner-container"><mat-spinner diameter="48"></mat-spinner></div>
      } @else if (lineas().length === 0) {
        <div class="empty-state"><p>Sin líneas sincronizadas</p></div>
      } @else {
        <table mat-table [dataSource]="lineas()" class="data-table">
          <ng-container matColumnDef="fechaGasto">
            <th mat-header-cell *matHeaderCellDef>Fecha</th>
            <td mat-cell *matCellDef="let l">{{ l.fechaGasto ? (l.fechaGasto | date: 'shortDate') : '—' }}</td>
          </ng-container>

          <ng-container matColumnDef="service">
            <th mat-header-cell *matHeaderCellDef>Concepto viaje</th>
            <td mat-cell *matCellDef="let l">{{ l.service }}</td>
          </ng-container>

          <ng-container matColumnDef="ceco">
            <th mat-header-cell *matHeaderCellDef>CECO</th>
            <td mat-cell *matCellDef="let l">{{ l.ceco }}</td>
          </ng-container>

          <ng-container matColumnDef="imputacion">
            <th mat-header-cell *matHeaderCellDef>Imputación</th>
            <td mat-cell *matCellDef="let l">
              @if (l.esGastoInternoSig) {
                <span class="badge badge-sig">Gasto SIG</span>
              } @else if (l.cecoNoMaestro) {
                <span class="badge badge-alert" matTooltip="El CECO no casa con la tabla maestra: línea sin imputar">
                  <mat-icon>warning</mat-icon> Sin imputar
                </span>
              } @else {
                {{ getServiceName(l.serviceId) || ('Servicio #' + l.serviceId) }}
              }
            </td>
          </ng-container>

          <ng-container matColumnDef="costeSinIVA">
            <th mat-header-cell *matHeaderCellDef class="num">Coste s/IVA</th>
            <td mat-cell *matCellDef="let l" class="num" [class.neg]="l.costeSinIVA < 0">
              {{ l.costeSinIVA | number: '1.2-2' }} €
            </td>
          </ng-container>

          <ng-container matColumnDef="travelerEmail">
            <th mat-header-cell *matHeaderCellDef>Viajero</th>
            <td mat-cell *matCellDef="let l">{{ l.travelerEmail || '—' }}</td>
          </ng-container>

          <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
          <tr mat-row *matRowDef="let row; columns: displayedColumns"></tr>
        </table>

        <mat-paginator
          [length]="total()"
          [pageSize]="pageSize()"
          [pageIndex]="page() - 1"
          [pageSizeOptions]="[25, 50, 100]"
          showFirstLastButtons
          (page)="onPageChange($event)">
        </mat-paginator>
      }
    </div>
  `,
  styles: `
    .sig-page { padding: 20px; }
    .page-header {
      margin-bottom: 12px;
      display: flex; justify-content: space-between; align-items: center; gap: 16px;
      h1 { margin: 0; font-size: 28px; font-weight: 500; }
      button { display: flex; align-items: center; gap: 8px; }
      mat-spinner { display: inline-block; }
    }
    .page-info { margin: 0 0 24px; font-size: 14px; color: var(--sig-text-secondary); max-width: 820px; }
    .upload-section {
      margin-bottom: 32px; padding: 20px;
      background: var(--sig-bg-card); border-radius: 8px; border: 1px solid var(--sig-border);
      h3 { margin-top: 0; font-size: 16px; font-weight: 600; color: var(--sig-text-heading); }
      .upload-info { margin: 10px 0 20px 0; font-size: 14px; color: var(--sig-text-secondary); }
    }
    .file-types-grid {
      display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 16px;
    }
    .file-card {
      background: white; border: 2px dashed #ddd; border-radius: 8px;
      padding: 20px; text-align: center; transition: all 0.2s ease; cursor: pointer;
      &:hover { border-color: #1976d2; background: #f0f7ff; }
      &.drag-over { border-color: #1976d2; background: #e3f2fd; }
      .file-icon { font-size: 48px; width: 48px; height: 48px; color: #1976d2; margin: 0 auto 12px; }
      .file-label { font-weight: 600; font-size: 14px; margin-bottom: 4px; }
      .file-pattern { font-size: 12px; color: #999; margin-bottom: 16px; }
      .upload-btn { width: 100%; }
      .upload-progress { margin-top: 12px; }
      .upload-status {
        margin-top: 12px; color: #4caf50; display: flex; align-items: center;
        justify-content: center; gap: 8px; font-size: 12px;
        .success-icon { font-size: 20px; width: 20px; height: 20px; }
      }
    }
    .kpi-grid {
      display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 16px; margin-bottom: 32px;
    }
    .kpi-card {
      background: var(--sig-bg-card); border-radius: 8px; padding: 20px;
      text-align: center; border: 1px solid var(--sig-border);
      .kpi-value { font-size: 28px; font-weight: 600; color: var(--sig-blue); margin-bottom: 8px; }
      .kpi-label { font-size: 12px; color: var(--sig-text-muted); text-transform: uppercase; letter-spacing: 0.5px; }
    }
    .kpi-card.kpi-alert {
      border-color: #f0a000; background: #fff8e6;
      .kpi-value { color: #d98300; }
    }
    .filter-controls {
      display: flex; gap: 24px; align-items: center; margin-bottom: 20px; flex-wrap: wrap;
      mat-form-field { min-width: 320px; }
    }
    .data-table {
      width: 100%; border-collapse: collapse;
      th {
        background-color: var(--sig-bg-header); padding: 12px; text-align: left;
        font-weight: 600; font-size: 12px; color: var(--sig-text-muted);
        border-bottom: 1px solid var(--sig-border);
      }
      td { padding: 12px; border-bottom: 1px solid var(--sig-border); }
      tr:hover { background-color: var(--sig-bg-hover); }
      .num { text-align: right; }
      .neg { color: #c62828; }
    }
    .badge {
      display: inline-flex; align-items: center; gap: 4px;
      padding: 2px 10px; border-radius: 12px; font-size: 12px; font-weight: 600;
      mat-icon { font-size: 16px; width: 16px; height: 16px; }
    }
    .badge-sig { background: #e3f2fd; color: #1565c0; }
    .badge-alert { background: #fff3e0; color: #d98300; }
    .spinner-container { display: flex; justify-content: center; padding: 40px 20px; }
    .empty-state { text-align: center; padding: 60px 20px; color: #999; p { margin: 0; font-size: 16px; } }
  `,
})
export class TravelPerkDashboardComponent implements OnInit {
  private readonly travelPerk = inject(TravelPerkService);
  private readonly serviceSvc = inject(ServiceService);
  private readonly syncSvc = inject(SyncService);
  private readonly notify = inject(NotifyService);
  private readonly zone = inject(NgZone);

  protected readonly lineas = signal<TravelPerkLineaDto[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(25);
  protected readonly loading = signal(false);
  protected readonly syncing = signal(false);
  protected readonly uploading = signal(false);
  protected readonly dragOver = signal(false);
  protected readonly uploadStatus = signal<string | null>(null);
  protected readonly kpis = signal<TravelPerkKpisDto>({
    totalLineas: 0, totalSinIVA: 0, lineasImputadas: 0, costeImputado: 0,
    lineasGastoInternoSig: 0, costeGastoInternoSig: 0, lineasCecoNoMaestro: 0,
  });

  protected soloNoMaestro = false;
  private searchValue = '';
  private search$ = new Subject<string>();

  private readonly servicios = signal<Map<number, string>>(new Map());

  displayedColumns = ['fechaGasto', 'service', 'ceco', 'imputacion', 'costeSinIVA', 'travelerEmail'];

  constructor() {
    this.search$
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe(() => { this.page.set(1); this.cargarLineas(); });
  }

  ngOnInit() {
    this.cargarServicios();
    this.cargarKpis();
    this.cargarLineas();
  }

  private cargarServicios() {
    this.serviceSvc.list(1, 1000).subscribe({
      next: res => this.servicios.set(new Map((res.items || []).map(s => [s.id, s.nombre]))),
      error: () => { /* no bloquear el dashboard si no hay permiso/listado */ },
    });
  }

  private cargarKpis() {
    this.travelPerk.getDashboard().subscribe({
      next: k => this.kpis.set(k),
      error: () => { /* degradar silenciosamente: KPIs a cero */ },
    });
  }

  protected cargarLineas() {
    this.loading.set(true);
    this.travelPerk.getLineas(this.page(), this.pageSize(), this.searchValue, this.soloNoMaestro).subscribe({
      next: res => {
        this.lineas.set(res.items || []);
        this.total.set(res.total || 0);
        this.loading.set(false);
      },
      error: () => {
        this.lineas.set([]);
        this.total.set(0);
        this.loading.set(false);
      },
    });
  }

  protected onSearch(event: Event) {
    this.searchValue = (event.target as HTMLInputElement).value;
    this.search$.next(this.searchValue);
  }

  protected onFilterToggle() {
    this.page.set(1);
    this.cargarLineas();
  }

  protected onPageChange(event: PageEvent) {
    this.page.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    window.scrollTo({ top: 0, behavior: 'smooth' });
    this.cargarLineas();
  }

  protected getServiceName(serviceId?: number | null): string | undefined {
    if (!serviceId) return undefined;
    return this.servicios().get(serviceId);
  }

  protected onDragOver(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.dragOver.set(true);
  }

  protected onDragLeave(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.dragOver.set(false);
  }

  protected onDrop(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.dragOver.set(false);
    const file = event.dataTransfer?.files?.[0];
    if (file) this.subir(file);
  }

  protected onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) this.subir(file);
    input.value = '';
  }

  private subir(file: File) {
    if (!file.name.toLowerCase().endsWith('.xlsx')) {
      this.notify.error('Solo se aceptan archivos .xlsx');
      return;
    }
    this.uploading.set(true);
    this.travelPerk.upload(file).subscribe({
      next: r => {
        this.uploading.set(false);
        this.uploadStatus.set(`✓ Cargado: ${file.name}`);
        setTimeout(() => this.uploadStatus.set(null), 5000);
        this.notify.success(r.mensaje ?? `Sincronizado: ${r.sync?.registrosInsertados ?? 0} líneas nuevas`);
        // El backend ya sincroniza automáticamente; solo recargar datos.
        this.zone.run(() => {
          this.page.set(1);
          this.cargarKpis();
          this.cargarLineas();
        });
      },
      error: err => {
        this.uploading.set(false);
        this.notify.error('Error al cargar archivo: ' + (err?.error?.error || err?.message || 'desconocido'));
      },
    });
  }

  protected sincronizar() {
    this.syncing.set(true);
    this.syncSvc.sync('travelperk').subscribe({
      next: r => {
        this.syncing.set(false);
        this.notify.success(`Sincronizado: ${r.registrosInsertados} líneas nuevas`);
        this.zone.run(() => {
          this.page.set(1);
          this.cargarKpis();
          this.cargarLineas();
        });
      },
      error: err => {
        this.syncing.set(false);
        this.notify.error(err?.error?.title ?? 'No se pudo sincronizar');
      },
    });
  }
}
