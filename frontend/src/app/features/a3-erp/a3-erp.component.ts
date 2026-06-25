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
// Los bloques de visión contable (cierres a contabilizar, costes externos, historial)
// son maqueta ilustrativa fiel al penpot, pendientes de cerrar la spec con SIG.
interface CierreContableMaqueta {
  cliente: string; servicio: string; tipo: 'Pago' | 'Factura';
  importe: string; destino: string; estado: 'Enviado' | 'Pendiente' | 'Error';
}
interface CosteExternoMaqueta {
  servicio: string; proveedor: string; tipo: string;
  pagado: string; refacturado: string; margen: string;
}
interface HistorialEnvioMaqueta {
  estado: 'Enviado' | 'Pendiente' | 'Error'; descripcion: string; fecha: string;
}

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
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Contabilidad' }]" />
      <div class="sig-page__header">
        <h1 class="sig-page__title"><mat-icon class="title-icon">account_balance</mat-icon> A3 ERP — Contabilidad</h1>
      </div>

      <p style="color: var(--mat-sys-on-surface-variant); margin-bottom: 24px; max-width: 720px;">
        Traspaso de facturas de cierres validados a A3 ERP (salida). El traspaso genera un fichero
        descargable para importar manualmente; la plataforma <strong>no escribe</strong> en A3 ERP ni en
        ningún sistema del cliente.
      </p>

      <!-- Cierres aprobados — listos para contabilizar (maqueta penpot) -->
      <mat-card class="a3-card a3-card--wide">
        <mat-card-header>
          <mat-icon mat-card-avatar>fact_check</mat-icon>
          <mat-card-title>Cierres aprobados — listos para contabilizar</mat-card-title>
          <mat-card-subtitle>Servicio: Todos · Mayo 2026 · los pagos van a A3 Innuva y las facturas a A3 ERP</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <table class="a3-table">
            <thead>
              <tr><th>Cliente</th><th>Servicio</th><th>Tipo</th><th class="num">Importe</th><th>Destino</th><th>Estado</th></tr>
            </thead>
            <tbody>
              @for (r of cierresAContabilizar(); track $index) {
                <tr>
                  <td>{{ r.cliente }}</td>
                  <td>{{ r.servicio }}</td>
                  <td><span class="a3-pill" [class.pago]="r.tipo === 'Pago'" [class.factura]="r.tipo === 'Factura'">{{ r.tipo }}</span></td>
                  <td class="num">{{ r.importe }}</td>
                  <td>{{ r.destino }}</td>
                  <td><span class="a3-state" [class]="'a3-state--' + r.estado.toLowerCase()">{{ r.estado }}</span></td>
                </tr>
              }
            </tbody>
            <tfoot>
              <tr>
                <td colspan="6" class="a3-totals">
                  <span>Total pagos → A3 Innuva <strong>€ 23.500</strong></span>
                  <span>Total facturas → A3 ERP <strong>€ 40.000</strong></span>
                  <span>Registros <strong>5</strong></span>
                </td>
              </tr>
            </tfoot>
          </table>
          <button mat-stroked-button color="primary" class="a3-inline-btn" disabled
                  matTooltip="Maqueta ilustrativa: el envío real se hace en «Envío a sistemas A3» (abajo).">
            <mat-icon>send</mat-icon> Generar y enviar a A3
          </button>
        </mat-card-content>
      </mat-card>

      <!-- Costes de servicios externos y logística (maqueta penpot) -->
      <mat-card class="a3-card a3-card--wide">
        <mat-card-header>
          <mat-icon mat-card-avatar>local_shipping</mat-icon>
          <mat-card-title>Costes de servicios externos y logística — coste de proyecto</mat-card-title>
          <mat-card-subtitle>Mayo 2026 · diferenciando lo pagado a proveedor vs lo refacturado al cliente</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <p class="a3-note">
            Los servicios externos y la logística (Galán / Mediapost) computan como coste de proyecto.
            La diferencia entre lo refacturado y lo pagado al proveedor es el margen de logística.
            <strong>⚠ Importes ilustrativos</strong>, dependen del cierre de Config. Factura con SIG.
          </p>
          <table class="a3-table">
            <thead>
              <tr><th>Servicio / Cliente</th><th>Proveedor</th><th>Tipo</th><th class="num">Pagado a proveedor</th><th class="num">Refacturado al cliente</th><th class="num">Margen</th></tr>
            </thead>
            <tbody>
              @for (r of costesExternos(); track $index) {
                <tr>
                  <td>{{ r.servicio }}</td>
                  <td>{{ r.proveedor }}</td>
                  <td>{{ r.tipo }}</td>
                  <td class="num">{{ r.pagado }}</td>
                  <td class="num">{{ r.refacturado }}</td>
                  <td class="num">{{ r.margen }}</td>
                </tr>
              }
            </tbody>
            <tfoot>
              <tr>
                <td colspan="3">Totales</td>
                <td class="num"><strong>€ 7.450</strong></td>
                <td class="num"><strong>€ 8.230</strong></td>
                <td class="num"><strong>€ 780</strong></td>
              </tr>
            </tfoot>
          </table>
        </mat-card-content>
      </mat-card>

      <h2 class="a3-section">Envío a sistemas A3</h2>

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

      <!-- Historial de envíos (maqueta penpot) -->
      <mat-card class="a3-card a3-card--wide">
        <mat-card-header>
          <mat-icon mat-card-avatar>history</mat-icon>
          <mat-card-title>Historial de envíos</mat-card-title>
          <mat-card-subtitle>⚠ Datos ilustrativos</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <table class="a3-table">
            <thead><tr><th>Estado</th><th>Envío</th><th>Fecha / hora</th></tr></thead>
            <tbody>
              @for (h of historialEnvios(); track $index) {
                <tr>
                  <td><span class="a3-state" [class]="'a3-state--' + h.estado.toLowerCase()">{{ h.estado }}</span></td>
                  <td>{{ h.descripcion }}</td>
                  <td>{{ h.fecha }}</td>
                </tr>
              }
            </tbody>
          </table>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .title-icon { vertical-align: middle; margin-right: 6px; }
    .a3-card { margin-bottom: 16px; max-width: 820px; }
    .a3-card--wide { max-width: 1000px; }
    .a3-card--muted { opacity: 0.85; }
    .a3-section { font-size: 16px; font-weight: 700; margin: 28px 0 12px; color: var(--mat-sys-on-surface); }
    .a3-table { width: 100%; border-collapse: collapse; font-size: 13px; }
    .a3-table th, .a3-table td { text-align: left; padding: 8px 10px; border-bottom: 1px solid var(--mat-sys-outline-variant); white-space: nowrap; }
    .a3-table th { color: var(--mat-sys-on-surface-variant); font-size: 11px; font-weight: 700; text-transform: uppercase; letter-spacing: .04em; }
    .a3-table td.num, .a3-table th.num { text-align: right; font-variant-numeric: tabular-nums; }
    .a3-table tfoot td { border-top: 2px solid var(--mat-sys-outline-variant); border-bottom: none; font-weight: 600; }
    .a3-totals { display: flex; gap: 24px; flex-wrap: wrap; color: var(--mat-sys-on-surface-variant); font-weight: 500; }
    .a3-totals strong { color: var(--mat-sys-on-surface); }
    .a3-pill { padding: 2px 8px; border-radius: 10px; font-size: 11px; font-weight: 600; }
    .a3-pill.pago { background: rgba(59,130,246,.12); color: #3b82f6; }
    .a3-pill.factura { background: rgba(34,197,94,.12); color: #22c55e; }
    .a3-state { padding: 2px 8px; border-radius: 10px; font-size: 11px; font-weight: 600; }
    .a3-state--enviado { background: rgba(34,197,94,.12); color: #22c55e; }
    .a3-state--pendiente { background: rgba(245,158,11,.12); color: #f59e0b; }
    .a3-state--error { background: rgba(239,68,68,.12); color: #ef4444; }
    .a3-note { color: var(--mat-sys-on-surface-variant); font-size: 12.5px; margin: 0 0 12px; }
    .a3-inline-btn { margin-top: 12px; }
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

  // Maqueta penpot (ilustrativo): visión de contabilidad. No proviene de API hasta cerrar spec con SIG.
  protected readonly cierresAContabilizar = signal<CierreContableMaqueta[]>([
    { cliente: 'American Express', servicio: 'Amex Shop Small', tipo: 'Pago', importe: '€ 15.000', destino: 'A3 Innuva', estado: 'Enviado' },
    { cliente: 'American Express', servicio: 'Amex Shop Small', tipo: 'Factura', importe: '€ 20.500', destino: 'A3 ERP', estado: 'Enviado' },
    { cliente: 'Granini', servicio: 'Granini GPVs', tipo: 'Pago', importe: '€ 8.500', destino: 'A3 Innuva', estado: 'Pendiente' },
    { cliente: 'Granini', servicio: 'Granini GPVs', tipo: 'Factura', importe: '€ 12.000', destino: 'A3 ERP', estado: 'Pendiente' },
    { cliente: 'Apple', servicio: 'Apple Formaciones', tipo: 'Factura', importe: '€ 7.500', destino: 'A3 ERP', estado: 'Error' },
  ]);

  protected readonly costesExternos = signal<CosteExternoMaqueta[]>([
    { servicio: 'Granini GPVs · Granini', proveedor: 'Galán', tipo: 'Logística', pagado: '€ 3.200', refacturado: '€ 3.680', margen: '€ 480' },
    { servicio: 'Coty Verano 26 · Coty', proveedor: 'Mediapost', tipo: 'Logística', pagado: '€ 2.450', refacturado: '€ 2.450', margen: '€ 0' },
    { servicio: 'Apple Formaciones · Apple', proveedor: 'Formador externo', tipo: 'Servicio externo', pagado: '€ 1.800', refacturado: '€ 2.100', margen: '€ 300' },
  ]);

  protected readonly historialEnvios = signal<HistorialEnvioMaqueta[]>([
    { estado: 'Enviado', descripcion: 'Amex — Pagos (A3 Innuva)', fecha: '31/05 08:02' },
    { estado: 'Enviado', descripcion: 'Amex — Facturas (A3 ERP)', fecha: '31/05 08:03' },
    { estado: 'Error', descripcion: 'Apple Formaciones — factura sin línea de detalle', fecha: '31/05 08:05' },
    { estado: 'Pendiente', descripcion: 'Granini — Pagos y Facturas', fecha: '—' },
  ]);

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
