import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { NotifyService } from '../../core/notify.service';
import { A3ErpService } from '../../core/api/a3erp.service';
import { ExportService } from '../../core/api/misc.service';
import { CierresService } from '../../core/api/cierres.service';
import { A3ErpStatusDto, CierreListItemDto } from '../../models/dtos';

// A3 ERP (Contabilidad) — hub de SALIDA: traspasa las facturas de un cierre de
// facturación validado a A3 ERP generando un fichero descargable (no escribe en A3 ERP).
// El lado de importación queda como stub honesto hasta tener spec de la API de A3 ERP.
@Component({
  selector: 'app-a3-erp',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatCardModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatFormFieldModule, MatSelectModule, MatTooltipModule,
    BreadcrumbsComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'A3 ERP' }]" />
      <div class="sig-page__header">
        <h1 class="sig-page__title"><mat-icon class="title-icon">account_balance</mat-icon> A3 ERP — Contabilidad</h1>
      </div>

      <p style="color: var(--mat-sys-on-surface-variant); margin-bottom: 24px; max-width: 720px;">
        Traspaso de facturas de cierres validados a A3 ERP (salida). El traspaso genera un fichero
        descargable para importar manualmente; la plataforma <strong>no escribe</strong> en A3 ERP ni en
        ningún sistema del cliente.
      </p>

      <!-- Estado de Autorización -->
      <mat-card class="a3-card">
        <mat-card-header>
          <mat-icon mat-card-avatar>{{ status()?.connected ? 'lock' : 'lock_open' }}</mat-icon>
          <mat-card-title>Estado de Autorización</mat-card-title>
          <mat-card-subtitle>Conexión con A3 ERP</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          @if (statusLoading()) {
            <mat-spinner diameter="22" />
          } @else if (status(); as s) {
            <div class="a3-status">
              <span [class.ok]="s.connected" [class.ko]="!s.connected">
                <mat-icon>{{ s.connected ? 'check_circle' : 'info' }}</mat-icon>
                {{ s.connected ? 'Conectado a A3 ERP' : 'No conectado' }}
              </span>
              <span class="a3-modo" [class.prod]="s.modo === 'Produccion'" [class.test]="s.modo !== 'Produccion'"
                    matTooltip="El modo depende de la configuración del backend (Integrations:A3Erp).">
                <mat-icon>{{ s.modo === 'Produccion' ? 'warning' : 'science' }}</mat-icon>
                Modo: {{ s.modo === 'Produccion' ? 'PRODUCCIÓN' : 'TEST' }}
              </span>
            </div>
            <p class="a3-msg">{{ s.mensaje }}</p>
          } @else {
            <p class="a3-msg">No se pudo obtener el estado de conexión.</p>
          }
        </mat-card-content>
      </mat-card>

      <!-- Traspaso de Facturas (export) -->
      <mat-card class="a3-card">
        <mat-card-header>
          <mat-icon mat-card-avatar>receipt_long</mat-icon>
          <mat-card-title>Traspaso de Facturas a A3 ERP</mat-card-title>
          <mat-card-subtitle>Genera el fichero de facturas de un cierre de facturación validado</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          @if (closures().length === 0 && !closuresLoading()) {
            <p class="a3-msg">No hay cierres de facturación aprobados o exportados disponibles para traspasar.</p>
          } @else {
            <mat-form-field appearance="outline" class="a3-select">
              <mat-label>Cierre de facturación</mat-label>
              <mat-select [ngModel]="selectedClosureId()" (ngModelChange)="selectedClosureId.set($event)" [disabled]="closuresLoading()" data-testid="a3erp-select-closure">
                @for (c of closures(); track c.id) {
                  <mat-option [value]="c.id">
                    #{{ c.id }} · {{ c.serviceNombre }} · {{ c.periodNombre }} · {{ c.estado }}
                  </mat-option>
                }
              </mat-select>
            </mat-form-field>
          }
        </mat-card-content>
        <mat-card-actions align="end">
          <button mat-flat-button color="primary"
                  [disabled]="!selectedClosureId() || exporting()"
                  (click)="onExport()" data-testid="a3erp-btn-export">
            @if (exporting()) { <mat-spinner diameter="20" /> }
            @else { <ng-container><mat-icon>download</mat-icon> Exportar a A3 ERP</ng-container> }
          </button>
        </mat-card-actions>
      </mat-card>

      <!-- Importación (stub honesto) -->
      <mat-card class="a3-card a3-card--muted">
        <mat-card-header>
          <mat-icon mat-card-avatar>sync_problem</mat-icon>
          <mat-card-title>Importación desde A3 ERP</mat-card-title>
          <mat-card-subtitle>Pendiente de especificación de la API de A3 ERP</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <p class="a3-msg">
            La sincronización de datos desde A3 ERP todavía no está disponible: falta la
            especificación de la API. Cuando esté definida se habilitará aquí.
          </p>
        </mat-card-content>
        <mat-card-actions align="end">
          <button mat-stroked-button (click)="onSync()" [disabled]="syncing()" data-testid="a3erp-btn-sync">
            @if (syncing()) { <mat-spinner diameter="20" /> }
            @else { <ng-container><mat-icon>refresh</mat-icon> Sincronizar</ng-container> }
          </button>
        </mat-card-actions>
      </mat-card>
    </div>
  `,
  styles: [`
    .title-icon { vertical-align: middle; margin-right: 6px; }
    .a3-card { margin-bottom: 16px; max-width: 820px; }
    .a3-card--muted { opacity: 0.85; }
    .a3-status { display: flex; flex-wrap: wrap; gap: 16px; align-items: center; font-weight: 500; }
    .a3-status mat-icon { vertical-align: middle; font-size: 18px; height: 18px; width: 18px; }
    .a3-status .ok { color: var(--mat-sys-primary); }
    .a3-status .ko { color: var(--mat-sys-on-surface-variant); }
    .a3-modo.prod { color: var(--mat-sys-error); }
    .a3-modo.test { color: var(--mat-sys-tertiary, var(--mat-sys-on-surface-variant)); }
    .a3-msg { color: var(--mat-sys-on-surface-variant); font-size: 13px; margin: 8px 0 0; }
    .a3-select { width: 100%; max-width: 520px; }
  `],
})
export class A3ErpComponent implements OnInit {
  private readonly a3erp = inject(A3ErpService);
  private readonly exportSvc = inject(ExportService);
  private readonly cierres = inject(CierresService);
  private readonly notify = inject(NotifyService);

  protected readonly status = signal<A3ErpStatusDto | null>(null);
  protected readonly statusLoading = signal(true);
  protected readonly closures = signal<CierreListItemDto[]>([]);
  protected readonly closuresLoading = signal(true);
  protected readonly selectedClosureId = signal<number | null>(null);
  protected readonly exporting = signal(false);
  protected readonly syncing = signal(false);

  ngOnInit(): void {
    this.a3erp.getStatus().subscribe({
      next: (s) => { this.status.set(s); this.statusLoading.set(false); },
      error: () => { this.status.set(null); this.statusLoading.set(false); },
    });

    // Solo cierres de facturación ya validados (Aprobado/Exportado) son traspasables.
    this.cierres.list('Facturacion', { page: 1, pageSize: 200 }).subscribe({
      next: (res) => {
        this.closures.set(res.items.filter((c) => c.estado === 'Aprobado' || c.estado === 'Exportado'));
        this.closuresLoading.set(false);
      },
      error: () => { this.closures.set([]); this.closuresLoading.set(false); },
    });
  }

  protected onExport(): void {
    const id = this.selectedClosureId();
    if (!id) return;
    this.exporting.set(true);
    this.exportSvc.exportA3Erp(id).subscribe({
      next: (resp) => {
        this.exportSvc.saveAttachment(resp, `A3ERP_${id}.xlsx`);
        this.exporting.set(false);
        this.notify.success('Fichero A3 ERP descargado');
      },
      error: (err) => {
        this.exporting.set(false);
        this.notify.error(err?.error?.title ?? 'No se pudo generar el fichero A3 ERP');
      },
    });
  }

  protected onSync(): void {
    this.syncing.set(true);
    this.a3erp.sync().subscribe({
      next: () => { this.syncing.set(false); },
      error: (err) => {
        this.syncing.set(false);
        // El backend responde 501 con el detalle del motivo (pendiente de spec).
        this.notify.info(err?.error?.detail ?? 'Sincronización desde A3 ERP aún no disponible.');
      },
    });
  }
}
