import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';


interface AuditLog {
  id: number; fecha: string; hora: string; usuario: string;
  tipoAccion: string; cliente?: string; proyecto?: string;
  accion: string; recurso?: string; entidad?: string; tipo: string;
}

@Component({
  selector: 'app-audit',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule],
  template: `
    <div class="sig-list-page">
      <div class="sig-list-topbar">
        <h1 class="sig-page-title">
          <mat-icon>manage_search</mat-icon>
          Auditoria &mdash; Log de cambios
        </h1>
        <div class="sig-topbar-actions">
          <button class="sig-btn-outline"><mat-icon>download</mat-icon> Exportar CSV</button>
        </div>
      </div>

      <div class="sig-filter-section">
        <div class="sig-filter-bar">
          <select class="sig-select" [(ngModel)]="filterPeriodo" (change)="onFilter()">
            <option value="">Periodo</option>
            <option value="Abril 2026">Abril 2026</option>
            <option value="Marzo 2026">Marzo 2026</option>
            <option value="Febrero 2026">Febrero 2026</option>
          </select>
          <select class="sig-select" [(ngModel)]="filterAccion" (change)="onFilter()">
            <option value="">Tipo Accion</option>
            <option value="Aprobar">Aprobar</option>
            <option value="Editar">Editar</option>
            <option value="Eliminar">Eliminar</option>
            <option value="Crear">Crear</option>
            <option value="Quitar">Quitar</option>
          </select>
          <select class="sig-select" [(ngModel)]="filterCliente" (change)="onFilter()">
            <option value="">Cliente</option>
            <option *ngFor="let c of clientes" [value]="c">{{c}}</option>
          </select>
          <select class="sig-select" [(ngModel)]="filterUsuario" (change)="onFilter()">
            <option value="">Usuario</option>
            <option *ngFor="let u of usuarios" [value]="u">{{u}}</option>
          </select>
          <button class="sig-btn-limpiar" (click)="clearFilters()">Limpiar</button>
        </div>
      </div>

      <div class="sig-content-area" style="flex-direction: column; overflow: visible;">

        <!-- Registro de actividad -->
        <div class="sig-audit-section">
          <div class="sig-audit-section-hdr">
            <span class="sig-dot-label"><span class="sig-dot--blue"></span> Registro de actividad</span>
            <span class="sig-count-label">{{ logs().length }} registros</span>
          </div>
          <div class="sig-table-wrap" style="border-radius: 10px;">
            <table>
              <thead>
                <tr>
                  <th>FECHA</th>
                  <th>HORA</th>
                  <th>USUARIO</th>
                  <th>TIPO ACCION</th>
                  <th>CLIENTE</th>
                  <th>SERVICIO</th>
                  <th>CONCEPTO</th>
                  <th>RECURSO</th>
                  <th>ENTIDAD</th>
                  <th>TIPO</th>
                </tr>
              </thead>
              <tbody>
                @for (log of logs(); track log.id) {
                  <tr (click)="selectLog(log)" [class.selected]="selectedLog()?.id === log.id">
                    <td style="font-family:'Roboto Mono',monospace;font-size:12px;">{{ log.fecha }}</td>
                    <td style="font-family:'Roboto Mono',monospace;font-size:12px;color:var(--sig-text-muted)">{{ log.hora }}</td>
                    <td style="font-weight:600">{{ log.usuario }}</td>
                    <td><span class="sig-action-badge" [class]="actionBadgeClass(log.tipoAccion)">{{ log.tipoAccion }}</span></td>
                    <td>{{ log.cliente || '—' }}</td>
                    <td>{{ log.proyecto || '—' }}</td>
                    <td>{{ log.accion }}</td>
                    <td>{{ log.recurso || '—' }}</td>
                    <td>{{ log.entidad || '—' }}</td>
                    <td><span class="sig-badge" [class]="tipoBadge(log.tipo)">{{ log.tipo }}</span></td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="10">
                      <div class="sig-empty">
                        <mat-icon>manage_search</mat-icon>
                        <span class="sig-empty-title">Sin registros de auditoria</span>
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
            <div class="sig-pagination">
              <span>Mostrando {{ logs().length }} de {{ logs().length }}</span>
              <button class="sig-page-btn" disabled>&#8249;</button>
              <div class="sig-page-current">1</div>
              <button class="sig-page-btn" disabled>&#8250;</button>
            </div>
          </div>
        </div>

        <!-- Detalle del registro -->
        @if (selectedLog()) {
          <div class="sig-audit-detail">
            <div class="sig-audit-detail-hdr">
              <span class="sig-dot-label">
                <span class="sig-dot--blue"></span>
                Detalle del registro &mdash; {{ selectedLog()!.fecha }} &middot; {{ selectedLog()!.hora }}
              </span>
            </div>
            <div class="sig-detail-origin">
              <mat-icon style="font-size:15px;width:15px;height:15px;color:var(--sig-teal)">sync</mat-icon>
              <span style="font-size:11px;font-weight:700;color:var(--sig-text-muted);letter-spacing:.08em">ORIGEN DATOS</span>
              <span style="font-size:12px;color:var(--sig-teal);font-weight:600">Celero One</span>
            </div>
            <div class="sig-detail-fields">
              <div class="sig-detail-field">
                <label>Fecha</label>
                <span>{{ selectedLog()!.fecha }}</span>
              </div>
              <div class="sig-detail-field">
                <label>Hora</label>
                <span>{{ selectedLog()!.hora }}</span>
              </div>
              <div class="sig-detail-field">
                <label>Usuario</label>
                <span>{{ selectedLog()!.usuario }}</span>
              </div>
              <div class="sig-detail-field">
                <label>Periodo</label>
                <span>{{ filterPeriodo || 'Abril 2026' }}</span>
              </div>
              <div class="sig-detail-field">
                <label>Tipo Accion</label>
                <span class="sig-action-badge" [class]="actionBadgeClass(selectedLog()!.tipoAccion)">{{ selectedLog()!.tipoAccion }}</span>
              </div>
              <div class="sig-detail-field">
                <label>Cliente</label>
                <span>{{ selectedLog()!.cliente || 'Amex' }}</span>
              </div>
              <div class="sig-detail-field">
                <label>Servicio</label>
                <span>{{ selectedLog()!.proyecto || 'Amex' }}</span>
              </div>
              <div class="sig-detail-field">
                <label>Concepto</label>
                <span>{{ selectedLog()!.accion }}</span>
              </div>
              <div class="sig-detail-field">
                <label>Recurso</label>
                <span>{{ selectedLog()!.recurso || '—' }}</span>
              </div>
              <div class="sig-detail-field">
                <label>Entidad</label>
                <span>{{ selectedLog()!.entidad || '—' }}</span>
              </div>
              <div class="sig-detail-field">
                <label>Tipo Registro</label>
                <span class="sig-badge" [class]="tipoBadge(selectedLog()!.tipo)">{{ selectedLog()!.tipo }}</span>
              </div>
            </div>
          </div>
        }
      </div>
    </div>
`,
  styles: [`
    :host { display: block; }
    .sig-list-page { display: flex; flex-direction: column; min-height: 100%; padding: 0; }
    .sig-list-topbar { display: flex; align-items: center; justify-content: space-between; padding: 20px 24px 0; margin-bottom: 16px; }
    .sig-page-title { font-size: 20px; font-weight: 700; color: var(--sig-text-heading); margin: 0; display: flex; align-items: center; gap: 10px; mat-icon { color: var(--sig-teal); font-size: 20px; width: 20px; height: 20px; } }
    .sig-btn-outline { display: inline-flex; align-items: center; gap: 6px; padding: 0 14px; height: 34px; border-radius: 8px; background: transparent; color: var(--sig-blue); border: 1px solid var(--sig-border); font-size: 12px; font-family: inherit; cursor: pointer; &:hover { background: var(--sig-bg-hover); } mat-icon { font-size: 15px !important; width: 15px !important; height: 15px !important; } }
    .sig-filter-section { padding: 0 24px 14px; }
    .sig-filter-bar { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
    .sig-select { height: 36px; background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 8px; padding: 0 10px; font-size: 13px; color: var(--sig-text-primary); font-family: inherit; cursor: pointer; outline: none; &:focus { border-color: var(--sig-blue); } }
    .sig-btn-limpiar { height: 36px; padding: 0 14px; border-radius: 8px; background: transparent; color: var(--sig-text-muted); border: 1px solid var(--sig-border); font-size: 13px; font-family: inherit; cursor: pointer; &:hover { background: var(--sig-bg-hover); } }
    .sig-content-area { flex: 1; display: flex; overflow: auto; padding: 0 24px 24px; gap: 16px; }
    .sig-audit-section { margin-bottom: 16px; }
    .sig-audit-section-hdr { display: flex; align-items: center; justify-content: space-between; margin-bottom: 10px; }
    .sig-dot-label { display: flex; align-items: center; gap: 8px; font-size: 13px; font-weight: 600; color: var(--sig-text-heading); }
    .sig-dot--blue { width: 8px; height: 8px; border-radius: 50%; background: var(--sig-blue); }
    .sig-count-label { font-size: 12px; color: var(--sig-text-muted); }
    .sig-table-wrap { overflow: auto; background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 10px; }
    table { width: 100%; border-collapse: collapse; }
    thead tr { background: var(--sig-bg-header); border-bottom: 1px solid var(--sig-border); }
    th { padding: 10px 14px; font-size: 10px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; color: var(--sig-text-muted); text-align: left; white-space: nowrap; }
    td { padding: 10px 14px; font-size: 12px; color: var(--sig-text-primary); border-bottom: 1px solid var(--sig-border); vertical-align: middle; }
    tbody tr { cursor: pointer; transition: background 120ms; &:hover { background: var(--sig-bg-hover); } &:last-child td { border-bottom: none; } &.selected { background: rgba(0,212,196,.06) !important; } }
    .sig-action-badge { display: inline-flex; align-items: center; gap: 5px; padding: 2px 9px; border-radius: 20px; font-size: 11px; font-weight: 600; }
    .sig-action-badge::before { content: ''; width: 5px; height: 5px; border-radius: 50%; background: currentColor; }
    .badge--aprobar  { color: #22c55e; background: rgba(34,197,94,.12); }
    .badge--editar   { color: #f59e0b; background: rgba(245,158,11,.12); }
    .badge--eliminar { color: #ef4444; background: rgba(239,68,68,.12); }
    .badge--crear    { color: #3b82f6; background: rgba(59,130,246,.12); }
    .badge--quitar   { color: #f59e0b; background: rgba(245,158,11,.12); }
    .sig-badge { display: inline-flex; align-items: center; gap: 5px; padding: 2px 9px; border-radius: 20px; font-size: 11px; font-weight: 600; }
    .sig-badge--yellow { color: #f59e0b; background: rgba(245,158,11,.12); }
    .sig-badge--blue   { color: #3b82f6; background: rgba(59,130,246,.12); }
    .sig-empty { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 8px; padding: 48px 24px; color: var(--sig-text-muted); }
    .sig-empty mat-icon { font-size: 36px !important; width: 36px !important; height: 36px !important; opacity: .4; }
    .sig-empty-title { font-size: 14px; font-weight: 600; color: var(--sig-text-secondary); }
    .sig-pagination { display: flex; align-items: center; justify-content: flex-end; gap: 8px; padding: 10px 16px; border-top: 1px solid var(--sig-border); font-size: 12px; color: var(--sig-text-muted); }
    .sig-page-btn { width: 28px; height: 28px; border-radius: 6px; border: 1px solid var(--sig-border); background: transparent; color: var(--sig-text-muted); cursor: pointer; font-size: 14px; display: flex; align-items: center; justify-content: center; &:disabled { opacity: .35; cursor: not-allowed; } }
    .sig-page-current { width: 28px; height: 28px; border-radius: 6px; background: var(--sig-blue); color: #fff; font-size: 13px; font-weight: 600; display: flex; align-items: center; justify-content: center; }
    .sig-audit-detail { background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 10px; overflow: hidden; }
    .sig-audit-detail-hdr { padding: 10px 16px; border-bottom: 1px solid var(--sig-border); }
    .sig-detail-origin { display: flex; align-items: center; gap: 8px; padding: 10px 16px; background: rgba(0,212,196,.06); border-bottom: 1px solid var(--sig-border); }
    .sig-detail-fields { display: grid; grid-template-columns: repeat(4,1fr); gap: 16px; padding: 16px; }
    .sig-detail-field label { font-size: 10px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; color: var(--sig-text-muted); display: block; margin-bottom: 4px; }
    .sig-detail-field span  { font-size: 13px; color: var(--sig-text-primary); font-weight: 500; }
`],
})
export class AuditComponent implements OnInit {

  protected readonly rawLogs    = signal<any[]>([]);
  protected readonly selectedLog = signal<AuditLog | null>(null);
  protected filterPeriodo = '';
  protected filterAccion  = '';
  protected filterCliente = '';
  protected filterUsuario = '';
  protected clientes:  string[] = [];
  protected usuarios:  string[] = [];

  protected readonly logs = signal<AuditLog[]>([
    { id:1,  fecha:'15/04/2026', hora:'17:32', usuario:'Silvia Garzon',  tipoAccion:'Aprobar',  cliente:'Amex', proyecto:'Amex',   accion:'Amex Shop Small',     recurso:'Antonio Pastor',  entidad:'Pago por visitas', tipo:'Pago' },
    { id:2,  fecha:'15/04/2026', hora:'18:12', usuario:'Silvia Garzon',  tipoAccion:'Aprobar',  cliente:'Amex', proyecto:'Amex',   accion:'Amex Shop Small',     recurso:'Jorge Diaz',      entidad:'Pago por visitas', tipo:'Pago' },
    { id:3,  fecha:'12/04/2026', hora:'14:11', usuario:'Tino Fanjul',    tipoAccion:'Editar',   cliente:'ITC',  proyecto:'ITC',    accion:'ITC GPVs',            recurso:'—',               entidad:'Mantenimiento BI', tipo:'Facturacion' },
    { id:4,  fecha:'23/03/2026', hora:'08:09', usuario:'Tomas Martin',   tipoAccion:'Eliminar', cliente:'Granini', proyecto:'Granini', accion:'Granini GPVs',   recurso:'Sergi Soler',     entidad:'Facturacion dietas', tipo:'Facturacion' },
    { id:5,  fecha:'21/03/2026', hora:'12:21', usuario:'Tomas Martin',   tipoAccion:'Quitar',   cliente:'Granini', proyecto:'Granini', accion:'Granini GPVs',   recurso:'Silvia Fernandez', entidad:'Kilometraje', tipo:'Pago' },
    { id:6,  fecha:'16/03/2026', hora:'10:02', usuario:'Adrian Tomas',   tipoAccion:'Aprobar',  cliente:'Apple', proyecto:'Apple',  accion:'Apple BA',            recurso:'Juan Moreno',     entidad:'NPI Watch 03-26', tipo:'Pago' },
    { id:7,  fecha:'08/02/2026', hora:'14:01', usuario:'Adrian Tomas',   tipoAccion:'Aprobar',  cliente:'Apple', proyecto:'Apple',  accion:'Apple RST',           recurso:'Diego Pontevedra', entidad:'Facturacion formaciones', tipo:'Facturacion' },
    { id:8,  fecha:'05/02/2026', hora:'15:23', usuario:'Arantxa Val',    tipoAccion:'Crear',    cliente:'Coty',  proyecto:'Coty',   accion:'Coty implantaciones', recurso:'—',               entidad:'Logistica', tipo:'Facturacion' },
  ]);

  ngOnInit(): void {
    const logs = this.logs();
    if (logs.length && !this.selectedLog()) this.selectedLog.set(logs[0]);
  }

  protected selectLog(log: AuditLog): void { this.selectedLog.set(log); }
  protected onFilter(): void { }
  protected clearFilters(): void { this.filterPeriodo=''; this.filterAccion=''; this.filterCliente=''; this.filterUsuario=''; }

  protected actionBadgeClass(accion: string): string {
    switch (accion) {
      case 'Aprobar':  return 'sig-action-badge badge--aprobar';
      case 'Editar':   return 'sig-action-badge badge--editar';
      case 'Eliminar': return 'sig-action-badge badge--eliminar';
      case 'Crear':    return 'sig-action-badge badge--crear';
      case 'Quitar':   return 'sig-action-badge badge--quitar';
      default:         return 'sig-action-badge badge--aprobar';
    }
  }

  protected tipoBadge(tipo: string): string {
    return tipo === 'Pago' ? 'sig-badge sig-badge--yellow' : 'sig-badge sig-badge--blue';
  }
}
