import { Component, computed, signal } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { environment } from '../../../environments/environment';

interface Pendiente {
  id: number; periodo: string; cliente: string; proyecto: string;
  coste: number; facturacion: number; margen: number; checked: boolean;
  conceptos: { nombre: string; empleado?: string; pago: number; factura: number; }[];
}
interface Aprobado  { periodo: string; proyecto: string; aprobadoPor: string; }
interface FlujoStep { nombre: string; estado: 'ok' | 'pending' | 'idle'; }

@Component({
  selector: 'app-approvals',
  standalone: true,
  imports: [CommonModule, FormsModule, DecimalPipe, MatIconModule, MatButtonModule],
  template: `
    <div class="sig-appr-page">

      <!-- Header -->
      <div class="sig-appr-header">
        <h1 class="sig-appr-title">
          <mat-icon>task_alt</mat-icon>
          Aprobaciones
        </h1>
        <div class="sig-appr-header-right">
          @if (showDemo) {
            <button (click)="toggleDemo()" title="Click para desactivar modo demo" class="sig-demo-badge sig-demo-badge--toggle">
              ENTORNO DEMO
            </button>
          }
          <div class="sig-period-chip">
            <mat-icon>schedule</mat-icon>
            <span>Mayo 2026</span>
            <mat-icon class="sig-chevron">expand_more</mat-icon>
          </div>
          <button mat-icon-button class="sig-appr-icon-btn" aria-label="Actualizar">
            <mat-icon>refresh</mat-icon>
          </button>
          <button mat-icon-button class="sig-appr-icon-btn sig-notif" aria-label="Notificaciones">
            <mat-icon>notifications</mat-icon>
            <span class="sig-notif-badge">6</span>
          </button>
        </div>
      </div>

      <!-- Filters -->
      <div class="sig-appr-filters">
        <select class="sig-sel"><option>Mayo 2026</option><option>Abril 2026</option></select>
        <select class="sig-sel"><option>Todos</option><option>American Express</option><option>Granini</option></select>
        <select class="sig-sel"><option>Todos</option><option>Amex Shop Small</option><option>Granini GPVs</option></select>
        <select class="sig-sel"><option>Pendiente</option><option>Aprobado</option><option>Rechazado</option></select>
      </div>

      <!-- PANEL 1: Pendientes de Aprobacion -->
      <div class="sig-panel">
        <div class="sig-panel-hdr">
          <span class="sig-panel-title">
            <span class="sig-dot sig-dot--orange"></span>
            Pendientes de Aprobacion
          </span>
          <div class="sig-panel-actions">
            <button class="sig-btn-approve" (click)="aprobarSeleccionados()">
              <mat-icon>check</mat-icon> Aprobar Seleccionados
            </button>
            <button class="sig-btn-reject">
              <mat-icon>close</mat-icon> Rechazar
            </button>
          </div>
        </div>
        <div class="sig-table-wrap">
          <table>
            <thead>
              <tr>
                <th style="width:32px"></th>
                <th>PERIODO</th>
                <th>CLIENTE</th>
                <th>SERVICIO</th>
                <th>COSTE</th>
                <th>FACTURACION</th>
                <th>MARGEN</th>
                <th>ACCIONES</th>
              </tr>
            </thead>
            <tbody>
              @for (item of pendientes; track item.id) {
                <tr (click)="selectPendiente(item)" [class.selected]="selectedPendiente?.id === item.id">
                  <td><input type="checkbox" class="sig-chk" [(ngModel)]="item.checked"/></td>
                  <td style="font-size:12px">{{ item.periodo }}</td>
                  <td>{{ item.cliente }}</td>
                  <td>
                    <a class="sig-link">{{ item.proyecto }}</a>
                  </td>
                  <td class="sig-mono">&euro; {{ item.coste | number:'1.0-0' }}</td>
                  <td class="sig-mono">&euro; {{ item.facturacion | number:'1.0-0' }}</td>
                  <td>
                    <span class="sig-margen-badge" [class]="margenClass(item.margen)">{{ item.margen }}%</span>
                  </td>
                  <td>
                    <div style="display:flex;gap:6px;align-items:center">
                      <button class="sig-action-ghost">Ver</button>
                      <button class="sig-action-approve" (click)="$event.stopPropagation();">Aprobar</button>
                    </div>
                  </td>
                </tr>
              }
              <tr class="sig-total-row">
                <td colspan="4" style="font-size:11px;font-weight:700;letter-spacing:.06em;text-transform:uppercase;color:var(--sig-text-muted)">Total Pendiente</td>
                <td class="sig-mono">&euro; {{ totalCoste() | number:'1.0-0' }}</td>
                <td class="sig-mono">&euro; {{ totalFacturacion() | number:'1.0-0' }}</td>
                <td colspan="2"></td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Bottom row: PANEL 2 + PANEL 3 -->
      <div class="sig-bottom-row">

        <!-- PANEL 2: Detalle del cierre seleccionado -->
        <div class="sig-panel sig-panel--detail">
          @if (selectedPendiente) {
            <div class="sig-panel-hdr">
              <span class="sig-panel-title">
                <span class="sig-dot sig-dot--blue"></span>
                Detalle &mdash; {{ selectedPendiente.proyecto }} &middot; Mayo 2026
              </span>
            </div>
            <div class="sig-detail-kpis">
              <div class="sig-dkpi">
                <div class="sig-dkpi-label">Coste Total</div>
                <div class="sig-dkpi-value">&euro; {{ selectedPendiente.coste | number:'1.0-0' }}</div>
              </div>
              <div class="sig-dkpi">
                <div class="sig-dkpi-label">Facturacion</div>
                <div class="sig-dkpi-value">&euro; {{ selectedPendiente.facturacion | number:'1.0-0' }}</div>
              </div>
              <div class="sig-dkpi">
                <div class="sig-dkpi-label">Margen</div>
                <div class="sig-dkpi-value sig-dkpi-value--accent">{{ selectedPendiente.margen }}%</div>
              </div>
            </div>
            <div class="sig-desglose-title">Desglose de Conceptos</div>
            <table class="sig-desglose-table">
              <thead>
                <tr>
                  <th>Concepto</th><th>Empleado</th><th>Importe Pago</th><th>Factura</th>
                </tr>
              </thead>
              <tbody>
                @for (c of selectedPendiente.conceptos; track c.nombre) {
                  <tr>
                    <td>{{ c.nombre }}</td>
                    <td style="color:var(--sig-text-muted)">{{ c.empleado || '—' }}</td>
                    <td class="sig-mono">&euro; {{ c.pago | number:'1.0-0' }}</td>
                    <td class="sig-mono">&euro; {{ c.factura | number:'1.0-0' }}</td>
                  </tr>
                }
              </tbody>
            </table>
            <div class="sig-comment-box">
              <mat-icon style="font-size:15px;width:15px;height:15px;color:var(--sig-text-muted)">chat_bubble_outline</mat-icon>
              <input class="sig-comment-input" placeholder="Añadir comentario antes de aprobar..." [(ngModel)]="comentario"/>
            </div>
            <div class="sig-detail-footer-btns">
              <button class="sig-btn-approve-lg" (click)="aprobarSeleccionados()">
                <mat-icon>check</mat-icon> Aprobar Seleccionados (1)
              </button>
              <button class="sig-btn-reject-sm">
                <mat-icon>close</mat-icon>
              </button>
              <span style="flex:1"></span>
              <button class="sig-btn-ghost" style="margin-left:auto">Cancelar</button>
            </div>
          } @else {
            <div class="sig-empty-state">
              <mat-icon>task_alt</mat-icon>
              <span>Selecciona un cierre para ver el detalle</span>
            </div>
          }
        </div>

        <!-- PANEL 3: Registros Aprobados + Flujo -->
        <div class="sig-panel sig-panel--approved">
          <div class="sig-panel-hdr">
            <span class="sig-panel-title">
              <span class="sig-dot sig-dot--green"></span>
              Registros Aprobados
            </span>
          </div>
          <table class="sig-aprobados-table">
            <thead>
              <tr><th>Periodo</th><th>Servicio</th><th>Aprobado por</th></tr>
            </thead>
            <tbody>
              @for (r of aprobados; track r.proyecto) {
                <tr>
                  <td style="font-size:11px;color:var(--sig-text-muted)">{{ r.periodo }}</td>
                  <td style="font-size:12px;font-weight:500">{{ r.proyecto }}</td>
                  <td style="font-size:11px;color:var(--sig-teal)">{{ r.aprobadoPor }}</td>
                </tr>
              }
            </tbody>
          </table>

          <div class="sig-flujo-title">Flujo de Aprobacion</div>
          <div class="sig-flujo">
            @for (paso of flujo; track paso.nombre) {
              <div class="sig-flujo-step" [class]="paso.estado">
                <div class="sig-flujo-circle">
                  @if (paso.estado === 'ok') {
                    <span class="sig-flujo-ok">OK</span>
                  } @else {
                    <span class="sig-flujo-dash">&mdash;</span>
                  }
                </div>
                <div class="sig-flujo-label">{{ paso.nombre }}</div>
              </div>
              @if (!$last) {
                <div class="sig-flujo-line" [class.line--done]="paso.estado === 'ok'"></div>
              }
            }
          </div>
        </div>

      </div>
    </div>
`,
  styles: [`
    :host { display: block; }

    .sig-appr-page {
      padding: 20px 24px 32px;
      background: var(--sig-bg-app);
      min-height: 100vh;
      display: flex; flex-direction: column; gap: 16px;
    }

    /* Header */
    .sig-appr-header { display: flex; align-items: center; justify-content: space-between; }
    .sig-appr-title { font-size: 20px; font-weight: 700; color: var(--sig-text-heading); margin: 0; display: flex; align-items: center; gap: 8px; mat-icon { color: var(--sig-teal); font-size: 20px; width: 20px; height: 20px; } }
    .sig-appr-header-right { display: flex; align-items: center; gap: 10px; }
    .sig-demo-badge { font-size: 10px; font-weight: 700; letter-spacing: 1px; background: rgba(245,158,11,.12); color: #f59e0b; border: 1px solid rgba(245,158,11,.25); padding: 4px 10px; border-radius: 4px; }
    .sig-demo-badge--toggle { cursor: pointer; transition: all 150ms; &:hover { background: rgba(245,158,11,.18); border-color: rgba(245,158,11,.4); } }
    .sig-period-chip { display: flex; align-items: center; gap: 6px; background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 8px; padding: 6px 12px; font-size: 13px; color: var(--sig-text-primary); mat-icon { font-size: 15px !important; width: 15px !important; height: 15px !important; color: var(--sig-text-muted) !important; } }
    .sig-appr-icon-btn { color: var(--sig-text-secondary) !important; background: var(--sig-bg-card) !important; border: 1px solid var(--sig-border) !important; border-radius: 8px !important; }
    .sig-notif { position: relative; }
    .sig-notif-badge { position: absolute; top: 4px; right: 4px; width: 16px; height: 16px; border-radius: 50%; background: #ef4444; color: #fff; font-size: 9px; font-weight: 700; display: flex; align-items: center; justify-content: center; pointer-events: none; }

    /* Filters */
    .sig-appr-filters { display: flex; gap: 10px; flex-wrap: wrap; }
    .sig-sel { height: 36px; background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 8px; padding: 0 12px; font-size: 13px; color: var(--sig-text-primary); font-family: inherit; cursor: pointer; outline: none; &:focus { border-color: var(--sig-blue); } }

    /* Panels */
    .sig-panel {
      background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 12px; overflow: hidden;
    }
    .sig-panel-hdr { display: flex; align-items: center; justify-content: space-between; padding: 12px 16px; border-bottom: 1px solid var(--sig-border); }
    .sig-panel-title { display: flex; align-items: center; gap: 8px; font-size: 13px; font-weight: 600; color: var(--sig-text-heading); }
    .sig-panel-actions { display: flex; gap: 8px; }
    .sig-dot { width: 8px; height: 8px; border-radius: 50%; flex-shrink: 0; }
    .sig-dot--orange { background: #f59e0b; }
    .sig-dot--blue   { background: #3b82f6; }
    .sig-dot--green  { background: #22c55e; }

    /* Approve / Reject buttons */
    .sig-btn-approve { display: inline-flex; align-items: center; gap: 5px; height: 30px; padding: 0 12px; border-radius: 6px; border: 1px solid rgba(34,197,94,.35); background: rgba(34,197,94,.1); color: #22c55e; font-size: 12px; font-weight: 600; font-family: inherit; cursor: pointer; mat-icon { font-size: 14px !important; width: 14px !important; height: 14px !important; } &:hover { background: rgba(34,197,94,.18); } }
    .sig-btn-reject  { display: inline-flex; align-items: center; gap: 5px; height: 30px; padding: 0 12px; border-radius: 6px; border: 1px solid rgba(239,68,68,.35); background: rgba(239,68,68,.1); color: #ef4444; font-size: 12px; font-weight: 600; font-family: inherit; cursor: pointer; mat-icon { font-size: 14px !important; width: 14px !important; height: 14px !important; } }

    /* Table */
    .sig-table-wrap { overflow: auto; }
    table { width: 100%; border-collapse: collapse; }
    thead tr { background: var(--sig-bg-header); border-bottom: 1px solid var(--sig-border); }
    th { padding: 10px 14px; font-size: 10px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; color: var(--sig-text-muted); text-align: left; }
    td { padding: 11px 14px; font-size: 13px; color: var(--sig-text-primary); border-bottom: 1px solid var(--sig-border); vertical-align: middle; }
    tbody tr { cursor: pointer; transition: background 120ms; &:hover { background: var(--sig-bg-hover); } &:last-child td { border-bottom: none; } &.selected { background: rgba(59,130,246,.06) !important; } }
    .sig-chk { accent-color: var(--sig-blue); width: 14px; height: 14px; cursor: pointer; }
    .sig-mono { font-family: 'Roboto Mono',monospace; font-size: 12px; font-weight: 600; }
    .sig-link { color: var(--sig-blue); text-decoration: none; font-size: 13px; &:hover { text-decoration: underline; } }
    .sig-margen-badge { padding: 2px 8px; border-radius: 5px; font-size: 11px; font-weight: 700; font-family: 'Roboto Mono',monospace; }
    .margen--green  { background: rgba(34,197,94,.15);  color: #22c55e; }
    .margen--yellow { background: rgba(245,158,11,.15); color: #f59e0b; }
    .margen--red    { background: rgba(239,68,68,.15);  color: #ef4444; }
    .sig-action-ghost   { padding: 3px 10px; border-radius: 5px; border: 1px solid var(--sig-border); background: transparent; color: var(--sig-text-secondary); font-size: 12px; font-family: inherit; cursor: pointer; }
    .sig-action-approve { padding: 3px 10px; border-radius: 5px; border: 1px solid rgba(34,197,94,.35); background: rgba(34,197,94,.1); color: #22c55e; font-size: 12px; font-family: inherit; cursor: pointer; font-weight: 600; }
    .sig-total-row td { background: var(--sig-bg-card-alt); font-size: 12px; font-weight: 700; border-top: 1px solid var(--sig-border); }

    /* Bottom row */
    .sig-bottom-row { display: grid; grid-template-columns: 1.2fr 1fr; gap: 16px; }
    @media (max-width: 900px) { .sig-bottom-row { grid-template-columns: 1fr; } }

    /* Detail KPIs */
    .sig-detail-kpis { display: grid; grid-template-columns: repeat(3,1fr); gap: 1px; background: var(--sig-border); border-bottom: 1px solid var(--sig-border); }
    .sig-dkpi { padding: 14px 16px; background: var(--sig-bg-card); }
    .sig-dkpi-label { font-size: 10px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; color: var(--sig-text-muted); margin-bottom: 4px; }
    .sig-dkpi-value { font-size: 22px; font-weight: 700; color: var(--sig-text-heading); font-family: 'Roboto Mono',monospace; }
    .sig-dkpi-value--accent { color: #22c55e; }

    /* Desglose table */
    .sig-desglose-title { padding: 10px 16px 6px; font-size: 10px; font-weight: 700; letter-spacing: .1em; text-transform: uppercase; color: var(--sig-text-muted); }
    .sig-desglose-table { width: 100%; border-collapse: collapse; th { padding: 7px 14px; font-size: 10px; font-weight: 700; letter-spacing: .06em; text-transform: uppercase; color: var(--sig-text-muted); border-bottom: 1px solid var(--sig-border); text-align: left; } td { padding: 9px 14px; font-size: 12px; color: var(--sig-text-primary); border-bottom: 1px solid var(--sig-border); } tr:last-child td { border-bottom: none; } }

    /* Comment */
    .sig-comment-box { display: flex; align-items: center; gap: 8px; padding: 10px 16px; border-top: 1px solid var(--sig-border); }
    .sig-comment-input { flex: 1; background: transparent; border: none; outline: none; font-size: 13px; color: var(--sig-text-primary); font-family: inherit; &::placeholder { color: var(--sig-text-muted); } }

    /* Footer buttons */
    .sig-detail-footer-btns { display: flex; align-items: center; gap: 8px; padding: 10px 16px; border-top: 1px solid var(--sig-border); }
    .sig-btn-approve-lg { display: inline-flex; align-items: center; gap: 6px; height: 34px; padding: 0 16px; border-radius: 8px; border: none; background: #22c55e; color: #fff; font-size: 13px; font-weight: 600; font-family: inherit; cursor: pointer; mat-icon { font-size: 15px !important; width: 15px !important; height: 15px !important; } }
    .sig-btn-reject-sm { width: 34px; height: 34px; border-radius: 8px; border: 1px solid rgba(239,68,68,.35); background: rgba(239,68,68,.1); color: #ef4444; cursor: pointer; display: flex; align-items: center; justify-content: center; mat-icon { font-size: 16px !important; width: 16px !important; height: 16px !important; } }
    .sig-btn-ghost { height: 34px; padding: 0 14px; border-radius: 8px; border: 1px solid var(--sig-border); background: transparent; color: var(--sig-text-muted); font-size: 13px; font-family: inherit; cursor: pointer; }

    /* Empty state */
    .sig-empty-state { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 8px; padding: 40px; color: var(--sig-text-muted); mat-icon { font-size: 32px !important; width: 32px !important; height: 32px !important; opacity: .4; } span { font-size: 13px; } }

    /* Aprobados */
    .sig-aprobados-table { width: 100%; border-collapse: collapse; th { padding: 8px 14px; font-size: 10px; font-weight: 700; letter-spacing: .06em; text-transform: uppercase; color: var(--sig-text-muted); border-bottom: 1px solid var(--sig-border); text-align: left; } td { padding: 9px 14px; border-bottom: 1px solid var(--sig-border); } tr:last-child td { border-bottom: none; } }

    /* Flujo de aprobacion */
    .sig-flujo-title { padding: 12px 16px 8px; font-size: 10px; font-weight: 700; letter-spacing: .1em; text-transform: uppercase; color: var(--sig-text-muted); border-top: 1px solid var(--sig-border); }
    .sig-flujo { display: flex; align-items: center; padding: 0 16px 16px; gap: 4px; }
    .sig-flujo-step { display: flex; flex-direction: column; align-items: center; gap: 4px; }
    .sig-flujo-circle { width: 36px; height: 36px; border-radius: 50%; border: 2px solid var(--sig-border); display: flex; align-items: center; justify-content: center; background: var(--sig-bg-card-alt); }
    .ok .sig-flujo-circle { background: #22c55e; border-color: #22c55e; }
    .pending .sig-flujo-circle { background: rgba(245,158,11,.15); border-color: #f59e0b; }
    .sig-flujo-ok   { font-size: 11px; font-weight: 700; color: #fff; }
    .sig-flujo-dash { font-size: 14px; color: var(--sig-text-muted); }
    .sig-flujo-label { font-size: 10px; color: var(--sig-text-muted); text-align: center; white-space: nowrap; }
    .sig-flujo-line { flex: 1; height: 2px; background: var(--sig-border); min-width: 8px; &.line--done { background: #22c55e; } }
`],
})
export class ApprovalsComponent {
  protected get showDemo(): boolean {
    const o = localStorage.getItem('sig_showDemo');
    if (o === 'true') return true;
    if (o === 'false') return false;
    return environment.showDemoCredentials;
  }
  protected comentario = '';

  protected pendientes: Pendiente[] = [
    { id:1, periodo:'Mayo 2026', cliente:'American Express', proyecto:'Amex Shop Small', coste:15000, facturacion:20500, margen:32, checked:true,
      conceptos:[
        { nombre:'Nota de gastos pago',       pago:5000, factura:6500 },
        { nombre:'Pago por visita', empleado:'Juan Perez',  pago:1250, factura:1625 },
        { nombre:'Pago por visita', empleado:'Marta Lopez', pago:980,  factura:1274 },
        { nombre:'Nota de gastos facturacion',              pago:0,    factura:420  },
      ]
    },
    { id:2, periodo:'Mayo 2026', cliente:'Granini', proyecto:'Granini GPVs', coste:8500, facturacion:12000, margen:25, checked:false,
      conceptos:[
        { nombre:'Pago por visita', empleado:'Sergi Soler', pago:3200, factura:4800 },
        { nombre:'Facturacion dietas', pago:0, factura:2200 },
      ]
    },
    { id:3, periodo:'Mayo 2026', cliente:'American Express', proyecto:'Amex New', coste:5200, facturacion:7500, margen:28, checked:false,
      conceptos:[
        { nombre:'Kilometraje', empleado:'Diego Torres', pago:1800, factura:0 },
        { nombre:'Nota de gastos facturacion', pago:0, factura:3400 },
      ]
    },
  ];

  protected selectedPendiente: Pendiente | null = this.pendientes[0];

  protected aprobados: Aprobado[] = [
    { periodo:'Abril 2026', proyecto:'Amex Shop Small',  aprobadoPor:'Maria G. (FICO)' },
    { periodo:'Abril 2026', proyecto:'Granini GPVs',     aprobadoPor:'Carlos L. (FICO)' },
    { periodo:'Marzo 2026', proyecto:'Amex Shop Small',  aprobadoPor:'Maria G. (FICO)' },
    { periodo:'Marzo 2026', proyecto:'Granini GPVs',     aprobadoPor:'Carlos L. (FICO)' },
  ];

  protected flujo: FlujoStep[] = [
    { nombre:'Grupo',  estado:'ok'      },
    { nombre:'FICO',   estado:'pending' },
    { nombre:'Cierre', estado:'idle'    },
  ];

  protected readonly totalCoste       = computed(() => this.pendientes.reduce((s, p) => s + p.coste, 0));
  protected readonly totalFacturacion = computed(() => this.pendientes.reduce((s, p) => s + p.facturacion, 0));

  protected selectPendiente(p: Pendiente): void { this.selectedPendiente = p; }
  protected aprobarSeleccionados(): void { }

  protected margenClass(m: number): string {
    if (m >= 28) return 'sig-margen-badge margen--green';
    if (m >= 20) return 'sig-margen-badge margen--yellow';
    return 'sig-margen-badge margen--red';
  }

  protected toggleDemo(): void {
    const current = localStorage.getItem('sig_showDemo');
    const next = current === 'false' ? 'true' : 'false';
    localStorage.setItem('sig_showDemo', next);
    location.reload();
  }
}
