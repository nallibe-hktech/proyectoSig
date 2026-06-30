import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule, DecimalPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { ActivatedRoute, Router } from '@angular/router';
import { ApprovalService } from '../../core/api/approvals.service';
import { CierrePanelItemDto } from '../../models/dtos';
import { TipoCierre } from '../../models/enums';

// ---- MAQUETA / DEMO: interfaces tipadas para los dos paneles ilustrativos ----
interface ConceptosImporte {
  readonly salarioBruto: number | null;
  readonly pagoVisitas: number | null;
  readonly kilometraje: number | null;
  readonly dietas: number | null;
  readonly incentivos: number | null;
  readonly visitasExtra: number | null;
  readonly total: number | null;
}
interface PagoFilaDemo extends ConceptosImporte {
  readonly empleado: string;
  readonly dni: string;
  readonly contrato: string;
  readonly estado: string;
}
interface PagoGrupoDemo {
  readonly ceco: string;
  readonly titulo: string;
  readonly subtituloCorto: string;
  readonly filas: readonly PagoFilaDemo[];
  readonly subtotal: ConceptosImporte;
}
interface PagosKpisDemo {
  readonly pendientes: number;
  readonly acciones: number;
  readonly empleados: number;
  readonly total: number;
}
interface FacturacionFilaDemo {
  readonly accion: string;
  readonly recurso: string;
  readonly coste: number;
  readonly facturacion: number;
  readonly margen: number;
  readonly estado: string;
  readonly editado: boolean;
}
interface FacturacionTotalDemo {
  readonly coste: number;
  readonly facturacion: number;
  readonly margen: number;
}
interface BorradorVersionDemo {
  readonly version: string;
  readonly cambio: string;
  readonly autor: string;
}

// Ola 3b (#10): el panel de aprobaciones agrega AMBOS tipos de cierre (Costes + Facturación).
// Cada fila indica su TipoCierre. El flujo mostrado sigue siendo Grupo → FICO (Ola 3a).
// Modo "onlyPendientes" (route data) usa api/approvals/pendientes; el resto usa api/approvals.
@Component({
  selector: 'app-approvals',
  standalone: true,
  imports: [CommonModule, FormsModule, DecimalPipe, DatePipe, MatIconModule, MatButtonModule],
  template: `
    <div class="sig-appr-page">

      <!-- Header -->
      <div class="sig-appr-header">
        <h1 class="sig-appr-title">
          <mat-icon>task_alt</mat-icon>
          {{ onlyPendientes() ? 'Mis pendientes' : 'Aprobaciones' }}
        </h1>
        <div class="sig-appr-header-right">
          <button mat-icon-button class="sig-appr-icon-btn" (click)="recargar()" [disabled]="cargando()" aria-label="Actualizar" data-testid="approvals-recargar">
            <mat-icon>{{ cargando() ? 'hourglass_empty' : 'refresh' }}</mat-icon>
          </button>
        </div>
      </div>

      <!-- Filters -->
      <div class="sig-appr-filters">
        <input class="sig-sel" style="min-width:240px" placeholder="Buscar servicio, cliente, período..." [(ngModel)]="texto" data-testid="approvals-busqueda" />
        <select class="sig-sel" [(ngModel)]="tipoFiltro" data-testid="approvals-filtro-tipo">
          <option value="">Todos los tipos</option>
          <option value="Costes">Costes</option>
          <option value="Facturacion">Facturación</option>
        </select>
      </div>

      <!-- ===================== MAQUETA / DEMO (datos ilustrativos) ===================== -->
      <div class="sig-demo-wrap" data-testid="approvals-demo">
        <div class="sig-demo-banner">
          <span class="sig-demo-badge">DEMO / MAQUETA</span>
          <span class="sig-demo-sub">Entorno Demo &middot; Mayo 2026 &middot; datos ilustrativos</span>
        </div>

        <!-- Sección 1: Vista dinamizada — Pagos -->
        <div class="sig-panel sig-demo-panel">
          <div class="sig-panel-hdr">
            <span class="sig-panel-title">
              <span class="sig-dot sig-dot--orange"></span>
              Vista dinamizada &mdash; Pagos (Mayo 2026)
            </span>
            <div class="sig-demo-actions">
              <button class="sig-demo-btn" data-testid="demo-pagos-exportar">Exportar</button>
              <button class="sig-demo-btn sig-demo-btn--primary" data-testid="demo-pagos-aprobar">Aprobar seleccionados</button>
              <button class="sig-demo-btn sig-demo-btn--danger" data-testid="demo-pagos-rechazar">Rechazar</button>
            </div>
          </div>

          <div class="sig-demo-chips">
            <span class="sig-chip sig-chip--pend"><span class="sig-dot sig-dot--orange"></span> Pendientes &middot; {{ pagosKpis().pendientes }}</span>
            <span class="sig-chip">Acciones &middot; {{ pagosKpis().acciones }}</span>
            <span class="sig-chip">Empleados (recursos) &middot; {{ pagosKpis().empleados }}</span>
            <span class="sig-chip sig-chip--total">Total a aprobar &middot; Mayo &middot; <strong class="sig-mono">{{ pagosKpis().total | number:'1.0-0' }} &euro;</strong></span>
          </div>

          <div class="sig-demo-note">
            Vista dinamizada (según feedback de SIG): las filas se agrupan por acción y empleado, y cada concepto se despliega como columna. Se aprueba por empleado (fila) o en bloque.
          </div>

          <div class="sig-table-wrap">
            <table class="sig-demo-table" data-testid="demo-pagos-tabla">
              <thead>
                <tr>
                  <th>EMPLEADO (RECURSO)</th>
                  <th class="sig-num">SALARIO BRUTO</th>
                  <th class="sig-num">PAGO VISITAS</th>
                  <th class="sig-num">KILOMETRAJE</th>
                  <th class="sig-num">DIETAS</th>
                  <th class="sig-num">INCENTIVOS</th>
                  <th class="sig-num">VISITAS EXTRA</th>
                  <th class="sig-num">TOTAL EMPLEADO</th>
                  <th>ESTADO</th>
                  <th>ACC.</th>
                </tr>
              </thead>
              <tbody>
                @for (grupo of pagosGrupos(); track grupo.ceco) {
                  <tr class="sig-demo-grouprow">
                    <td colspan="10">{{ grupo.titulo }}</td>
                  </tr>
                  @for (fila of grupo.filas; track fila.dni) {
                    <tr data-testid="demo-pagos-item">
                      <td>
                        <div>{{ fila.empleado }}</div>
                        <div class="sig-demo-meta">DNI {{ fila.dni }} &middot; Contrato {{ fila.contrato }}</div>
                      </td>
                      <td class="sig-num sig-mono">{{ cell(fila.salarioBruto) }}</td>
                      <td class="sig-num sig-mono">{{ cell(fila.pagoVisitas) }}</td>
                      <td class="sig-num sig-mono">{{ cell(fila.kilometraje) }}</td>
                      <td class="sig-num sig-mono">{{ cell(fila.dietas) }}</td>
                      <td class="sig-num sig-mono">{{ cell(fila.incentivos) }}</td>
                      <td class="sig-num sig-mono">{{ cell(fila.visitasExtra) }}</td>
                      <td class="sig-num sig-mono"><strong>{{ cell(fila.total) }}</strong></td>
                      <td><span class="sig-chip sig-chip--pend"><span class="sig-dot sig-dot--orange"></span> {{ fila.estado }}</span></td>
                      <td><button class="sig-action-ghost" data-testid="demo-pagos-ver">Ver</button></td>
                    </tr>
                  }
                  <tr class="sig-demo-subtotal">
                    <td>Subtotal &middot; {{ grupo.subtituloCorto }}</td>
                    <td class="sig-num sig-mono">{{ cell(grupo.subtotal.salarioBruto) }}</td>
                    <td class="sig-num sig-mono">{{ cell(grupo.subtotal.pagoVisitas) }}</td>
                    <td class="sig-num sig-mono">{{ cell(grupo.subtotal.kilometraje) }}</td>
                    <td class="sig-num sig-mono">{{ cell(grupo.subtotal.dietas) }}</td>
                    <td class="sig-num sig-mono">{{ cell(grupo.subtotal.incentivos) }}</td>
                    <td class="sig-num sig-mono">{{ cell(grupo.subtotal.visitasExtra) }}</td>
                    <td class="sig-num sig-mono"><strong>{{ cell(grupo.subtotal.total) }}</strong></td>
                    <td colspan="2"></td>
                  </tr>
                }
                <tr class="sig-demo-grandtotal">
                  <td>Total general</td>
                  <td class="sig-num sig-mono">{{ cell(pagosTotal().salarioBruto) }}</td>
                  <td class="sig-num sig-mono">{{ cell(pagosTotal().pagoVisitas) }}</td>
                  <td class="sig-num sig-mono">{{ cell(pagosTotal().kilometraje) }}</td>
                  <td class="sig-num sig-mono">{{ cell(pagosTotal().dietas) }}</td>
                  <td class="sig-num sig-mono">{{ cell(pagosTotal().incentivos) }}</td>
                  <td class="sig-num sig-mono">{{ cell(pagosTotal().visitasExtra) }}</td>
                  <td class="sig-num sig-mono"><strong>{{ cell(pagosTotal().total) }}</strong></td>
                  <td colspan="2"></td>
                </tr>
              </tbody>
            </table>
          </div>

          <div class="sig-demo-footnote">
            &#9888; Agrupación: acción &middot; empleado (recurso); columnas = conceptos dinamizados. Los importes por concepto y los DNI/contrato son ilustrativos (no facilitados a nivel de detalle por SIG) &mdash; pendientes de validar.
          </div>
        </div>

        <!-- Sección 2: Borrador de Facturación -->
        <div class="sig-panel sig-demo-panel">
          <div class="sig-panel-hdr">
            <span class="sig-panel-title">
              <span class="sig-dot sig-dot--green"></span>
              Borrador de Facturación &mdash; DAIKIN &middot; CECO 023301 &middot; Mayo 2026
            </span>
            <div class="sig-demo-actions">
              <button class="sig-demo-btn" data-testid="demo-fact-recalcular">Recalcular borrador</button>
              <button class="sig-demo-btn sig-demo-btn--primary" data-testid="demo-fact-enviar">Enviar a aprobación</button>
            </div>
          </div>

          <div class="sig-table-wrap">
            <table class="sig-demo-table" data-testid="demo-fact-tabla">
              <thead>
                <tr>
                  <th>ACCIÓN / PROYECTO</th>
                  <th>RECURSO</th>
                  <th class="sig-num">COSTE (PAGOS)</th>
                  <th class="sig-num">FACTURACIÓN (BORRADOR)</th>
                  <th class="sig-num">MARGEN</th>
                  <th>ESTADO</th>
                </tr>
              </thead>
              <tbody>
                @for (fila of facturacionFilas(); track fila.recurso) {
                  <tr data-testid="demo-fact-item">
                    <td>{{ fila.accion }}</td>
                    <td>{{ fila.recurso }}</td>
                    <td class="sig-num sig-mono">{{ cell(fila.coste) }}</td>
                    <td class="sig-num sig-mono">
                      {{ cell(fila.facturacion) }}
                      @if (fila.editado) { <span class="sig-demo-tag">Editado</span> }
                    </td>
                    <td class="sig-num sig-mono">{{ fila.margen | number:'1.1-1' }}%</td>
                    <td><span class="sig-chip sig-chip--draft">{{ fila.estado }}</span></td>
                  </tr>
                }
                <tr class="sig-demo-grandtotal">
                  <td>Total borrador</td>
                  <td></td>
                  <td class="sig-num sig-mono">{{ cell(facturacionTotal().coste) }}</td>
                  <td class="sig-num sig-mono"><strong>{{ cell(facturacionTotal().facturacion) }}</strong></td>
                  <td class="sig-num sig-mono">{{ facturacionTotal().margen | number:'1.1-1' }}%</td>
                  <td></td>
                </tr>
              </tbody>
            </table>
          </div>

          <div class="sig-demo-note">
            "Mantener borrador inicial": el borrador se genera desde los pagos y se conserva editable hasta enviarlo a aprobación. &#9888; Importes de facturación y márgenes ilustrativos &mdash; pendientes de validar con SIG.
          </div>

          <div class="sig-demo-subhdr">Historial del borrador</div>
          <div class="sig-table-wrap">
            <table class="sig-demo-table" data-testid="demo-fact-historial">
              <thead>
                <tr>
                  <th>VERSIÓN</th>
                  <th>CAMBIO</th>
                  <th>AUTOR</th>
                </tr>
              </thead>
              <tbody>
                @for (h of facturacionHistorial(); track h.version) {
                  <tr>
                    <td class="sig-mono">{{ h.version }}</td>
                    <td>{{ h.cambio }}</td>
                    <td class="sig-demo-meta">{{ h.autor }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      </div>
      <!-- =================== FIN MAQUETA / DEMO =================== -->

      <!-- Panel -->
      <div class="sig-panel">
        <div class="sig-panel-hdr">
          <span class="sig-panel-title">
            <span class="sig-dot sig-dot--orange"></span>
            {{ onlyPendientes() ? 'Pendientes asignados a mí' : 'Cierres en el flujo de aprobación' }}
            <span class="sig-flow-hint">Flujo: Grupo &rarr; FICO &rarr; Exportado</span>
          </span>
          @if (seleccionados().size > 0) {
            <div class="sig-panel-actions">
              <span class="sig-sel-count">{{ seleccionados().size }} seleccionado(s)</span>
              <button class="sig-btn-approve" (click)="aprobarSeleccionados()" [disabled]="procesando()" data-testid="approvals-batch-aprobar">
                <mat-icon>check</mat-icon> Aprobar
              </button>
              <button class="sig-btn-reject" (click)="rechazarSeleccionados()" [disabled]="procesando()" data-testid="approvals-batch-rechazar">
                <mat-icon>close</mat-icon> Rechazar
              </button>
            </div>
          }
        </div>
        <div class="sig-table-wrap">
          @if (cargando()) {
            <div style="padding:24px;color:var(--sig-text-muted)">Cargando...</div>
          } @else if (filtrados().length === 0) {
            <div style="padding:24px;color:var(--sig-text-muted)" data-testid="approvals-vacio">No hay cierres en este estado.</div>
          } @else {
            <table data-testid="approvals-tabla">
              <thead>
                <tr>
                  <th style="width:36px"><input type="checkbox" class="sig-chk" (change)="toggleTodos($event)" [checked]="todosSeleccionados()" /></th>
                  <th>PERIODO</th>
                  <th>CLIENTE</th>
                  <th>SERVICIO</th>
                  <th>TIPO</th>
                  <th>ESTADO</th>
                  <th>PASO</th>
                  <th>TOTAL</th>
                  <th>ACTUALIZADO</th>
                  <th>ACCIONES</th>
                </tr>
              </thead>
              <tbody>
                @for (item of filtrados(); track item.cierreId + '-' + item.tipoCierre) {
                  <tr data-testid="approvals-item" [class.selected]="estaSeleccionado(item)">
                    <td><input type="checkbox" class="sig-chk" [checked]="estaSeleccionado(item)" (change)="toggleSeleccion(item)" /></td>
                    <td style="font-size:12px">{{ item.periodNombre }}</td>
                    <td>{{ item.clientNombre }}</td>
                    <td><a class="sig-link">{{ item.serviceNombre }}</a></td>
                    <td>
                      <span class="sig-tipo-badge" [class]="item.tipoCierre === 'Costes' ? 'tipo--costes' : 'tipo--fact'">
                        {{ item.tipoCierre === 'Costes' ? 'Costes' : 'Facturación' }}
                      </span>
                    </td>
                    <td>{{ item.estado }}</td>
                    <td>{{ item.pasoActualRol }}</td>
                    <td class="sig-mono">&euro; {{ item.total | number:'1.0-0' }}</td>
                    <td style="font-size:12px;color:var(--sig-text-muted)">{{ item.updatedAt | date:'dd/MM/yyyy HH:mm' }}</td>
                    <td class="sig-actions-cell">
                      <button class="sig-action-ghost" (click)="ver(item)" data-testid="approvals-ver">Ver</button>
                      <button class="sig-action-approve" (click)="aprobarUno(item)" [disabled]="procesando()" data-testid="approvals-aprobar">Aprobar</button>
                      <button class="sig-action-reject" (click)="rechazarUno(item)" [disabled]="procesando()" data-testid="approvals-rechazar">Rechazar</button>
                    </td>
                  </tr>
                }
                <tr class="sig-total-row">
                  <td colspan="7" style="font-size:11px;font-weight:700;letter-spacing:.06em;text-transform:uppercase;color:var(--sig-text-muted)">Total</td>
                  <td class="sig-mono">&euro; {{ totalImporte() | number:'1.0-0' }}</td>
                  <td colspan="2"></td>
                </tr>
              </tbody>
            </table>
          }
        </div>
      </div>
    </div>
  `,
  styles: [`
    :host { display: block; }
    .sig-appr-page { padding: 20px 24px 32px; background: var(--sig-bg-app); min-height: 100vh; display: flex; flex-direction: column; gap: 16px; }
    .sig-appr-header { display: flex; align-items: center; justify-content: space-between; }
    .sig-appr-title { font-size: 20px; font-weight: 700; color: var(--sig-text-heading); margin: 0; display: flex; align-items: center; gap: 8px; mat-icon { color: var(--sig-teal); } }
    .sig-appr-header-right { display: flex; align-items: center; gap: 10px; }
    .sig-appr-icon-btn { color: var(--sig-text-secondary) !important; background: var(--sig-bg-card) !important; border: 1px solid var(--sig-border) !important; border-radius: 8px !important; }
    .sig-appr-filters { display: flex; gap: 10px; flex-wrap: wrap; }
    .sig-sel { height: 36px; background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 8px; padding: 0 12px; font-size: 13px; color: var(--sig-text-primary); font-family: inherit; outline: none; &:focus { border-color: var(--sig-blue); } }
    .sig-panel { background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 12px; overflow: hidden; }
    .sig-panel-hdr { display: flex; align-items: center; justify-content: space-between; padding: 12px 16px; border-bottom: 1px solid var(--sig-border); }
    .sig-panel-title { display: flex; align-items: center; gap: 8px; font-size: 13px; font-weight: 600; color: var(--sig-text-heading); }
    .sig-flow-hint { font-size: 11px; font-weight: 400; color: var(--sig-text-muted); margin-left: 8px; }
    .sig-dot { width: 8px; height: 8px; border-radius: 50%; flex-shrink: 0; }
    .sig-dot--orange { background: #f59e0b; }
    .sig-table-wrap { overflow: auto; }
    table { width: 100%; border-collapse: collapse; }
    thead tr { background: var(--sig-bg-header); border-bottom: 1px solid var(--sig-border); }
    th { padding: 10px 14px; font-size: 10px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; color: var(--sig-text-muted); text-align: left; }
    td { padding: 11px 14px; font-size: 13px; color: var(--sig-text-primary); border-bottom: 1px solid var(--sig-border); vertical-align: middle; }
    .sig-mono { font-family: 'Roboto Mono',monospace; font-size: 12px; font-weight: 600; }
    .sig-link { color: var(--sig-blue); text-decoration: none; font-size: 13px; }
    .sig-tipo-badge { padding: 2px 8px; border-radius: 5px; font-size: 11px; font-weight: 700; }
    .tipo--costes { background: rgba(245,158,11,.15); color: #f59e0b; }
    .tipo--fact { background: rgba(34,197,94,.15); color: #22c55e; }
    .sig-chk { accent-color: var(--sig-blue); width: 14px; height: 14px; cursor: pointer; }
    .selected { background: rgba(59,130,246,.06) !important; }
    .sig-actions-cell { display: flex; gap: 6px; align-items: center; }
    .sig-action-ghost { padding: 3px 10px; border-radius: 5px; border: 1px solid var(--sig-border); background: transparent; color: var(--sig-text-secondary); font-size: 12px; font-family: inherit; cursor: pointer; }
    .sig-action-approve { padding: 3px 10px; border-radius: 5px; border: 1px solid rgba(34,197,94,.35); background: rgba(34,197,94,.1); color: #22c55e; font-size: 12px; font-family: inherit; cursor: pointer; font-weight: 600; &:disabled { opacity: .45; cursor: default; } }
    .sig-action-reject { padding: 3px 10px; border-radius: 5px; border: 1px solid rgba(239,68,68,.35); background: rgba(239,68,68,.1); color: #ef4444; font-size: 12px; font-family: inherit; cursor: pointer; font-weight: 600; &:disabled { opacity: .45; cursor: default; } }
    .sig-panel-actions { display: flex; align-items: center; gap: 8px; }
    .sig-sel-count { font-size: 12px; color: var(--sig-text-muted); font-weight: 600; }
    .sig-btn-approve { display: inline-flex; align-items: center; gap: 4px; padding: 4px 12px; border-radius: 6px; border: 1px solid rgba(34,197,94,.35); background: rgba(34,197,94,.1); color: #22c55e; font-size: 12px; font-weight: 700; font-family: inherit; cursor: pointer; &:disabled { opacity: .45; cursor: default; } mat-icon { font-size: 14px !important; width: 14px !important; height: 14px !important; } }
    .sig-btn-reject { display: inline-flex; align-items: center; gap: 4px; padding: 4px 12px; border-radius: 6px; border: 1px solid rgba(239,68,68,.35); background: rgba(239,68,68,.1); color: #ef4444; font-size: 12px; font-weight: 700; font-family: inherit; cursor: pointer; &:disabled { opacity: .45; cursor: default; } mat-icon { font-size: 14px !important; width: 14px !important; height: 14px !important; } }
    .sig-total-row td { background: var(--sig-bg-card-alt); font-size: 12px; font-weight: 700; border-top: 1px solid var(--sig-border); }

    /* ---- MAQUETA / DEMO ---- */
    .sig-demo-wrap { display: flex; flex-direction: column; gap: 16px; border: 1px dashed var(--sig-blue); border-radius: 12px; padding: 14px; background: var(--sig-bg-card-alt); }
    .sig-demo-banner { display: flex; align-items: center; gap: 10px; }
    .sig-demo-badge { display: inline-block; padding: 3px 10px; border-radius: 6px; background: var(--sig-blue); color: #fff; font-size: 11px; font-weight: 800; letter-spacing: .08em; }
    .sig-demo-sub { font-size: 12px; color: var(--sig-text-muted); font-weight: 600; }
    .sig-demo-panel { background: var(--sig-bg-card); }
    .sig-dot--green { background: #22c55e; }
    .sig-demo-actions { display: flex; gap: 8px; flex-wrap: wrap; }
    .sig-demo-btn { padding: 5px 12px; border-radius: 7px; border: 1px solid var(--sig-border); background: var(--sig-bg-card-alt); color: var(--sig-text-secondary); font-size: 12px; font-weight: 600; font-family: inherit; cursor: pointer; }
    .sig-demo-btn--primary { background: var(--sig-teal); border-color: var(--sig-teal); color: #fff; }
    .sig-demo-btn--danger { background: transparent; border-color: #ef4444; color: #ef4444; }
    .sig-demo-chips { display: flex; gap: 8px; flex-wrap: wrap; padding: 12px 16px 0; }
    .sig-chip { display: inline-flex; align-items: center; gap: 6px; padding: 4px 10px; border-radius: 999px; background: var(--sig-bg-card-alt); border: 1px solid var(--sig-border); font-size: 12px; font-weight: 600; color: var(--sig-text-secondary); }
    .sig-chip--pend { background: rgba(245,158,11,.12); border-color: rgba(245,158,11,.35); color: #b45309; }
    .sig-chip--total { background: rgba(20,184,166,.10); border-color: rgba(20,184,166,.30); color: var(--sig-text-heading); }
    .sig-chip--draft { background: rgba(99,102,241,.12); border-color: rgba(99,102,241,.30); color: #4f46e5; }
    .sig-demo-note { padding: 10px 16px 0; font-size: 12px; color: var(--sig-text-muted); line-height: 1.5; }
    .sig-demo-footnote { padding: 10px 16px 14px; font-size: 11px; color: var(--sig-text-muted); line-height: 1.5; }
    .sig-demo-table .sig-num { text-align: right; }
    .sig-demo-meta { font-size: 11px; color: var(--sig-text-muted); margin-top: 2px; }
    .sig-demo-grouprow td { background: var(--sig-bg-header); font-size: 12px; font-weight: 700; color: var(--sig-text-heading); }
    .sig-demo-subtotal td { background: var(--sig-bg-card-alt); font-size: 12px; font-weight: 700; }
    .sig-demo-grandtotal td { background: var(--sig-bg-card-alt); font-size: 12px; font-weight: 800; border-top: 2px solid var(--sig-border); color: var(--sig-text-heading); }
    .sig-demo-tag { display: inline-block; margin-left: 6px; padding: 1px 6px; border-radius: 4px; background: rgba(99,102,241,.15); color: #4f46e5; font-size: 10px; font-weight: 700; }
    .sig-demo-subhdr { padding: 14px 16px 6px; font-size: 12px; font-weight: 700; color: var(--sig-text-heading); }
  `],
})
export class ApprovalsComponent implements OnInit {
  private readonly approvalSvc = inject(ApprovalService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly onlyPendientes = signal<boolean>(this.route.snapshot.data['onlyPendientes'] === true);
  protected readonly items = signal<CierrePanelItemDto[]>([]);
  protected readonly cargando = signal(false);
  protected readonly procesando = signal(false);
  protected readonly seleccionados = signal<Set<number>>(new Set());
  protected texto = '';
  protected tipoFiltro: '' | TipoCierre = '';

  protected readonly filtrados = computed(() => {
    const q = this.texto.toLowerCase();
    return this.items().filter((c) => {
      const coincide = c.serviceNombre.toLowerCase().includes(q) ||
        c.clientNombre.toLowerCase().includes(q) ||
        c.periodNombre.toLowerCase().includes(q);
      const tipoCoinc = !this.tipoFiltro || c.tipoCierre === this.tipoFiltro;
      return coincide && tipoCoinc;
    });
  });

  protected readonly totalImporte = computed(() => this.filtrados().reduce((s, c) => s + c.total, 0));
  protected readonly todosSeleccionados = computed(() =>
    this.filtrados().length > 0 && this.filtrados().every((i) => this.seleccionados().has(i.cierreId))
  );

  // ===================== MAQUETA / DEMO (datos ilustrativos embebidos) =====================
  // Sección 1 — Vista dinamizada Pagos (Mayo 2026). Datos NO reales, pendientes de validar con SIG.
  protected readonly pagosKpis = signal<PagosKpisDemo>({
    pendientes: 4,
    acciones: 2,
    empleados: 4,
    total: 7235,
  });

  protected readonly pagosGrupos = signal<readonly PagoGrupoDemo[]>([
    {
      ceco: '035501',
      titulo: 'CECO 035501 · Amex Shop Small · American Express · Periodo Mayo 2026 (01/05 – 31/05)',
      subtituloCorto: 'Amex Shop Small',
      filas: [
        { empleado: 'Antonio Pastor', dni: '50318477H', contrato: '—', estado: 'Pendiente', salarioBruto: 1450, pagoVisitas: 320, kilometraje: 85, dietas: 60, incentivos: 120, visitasExtra: 40, total: 2075 },
        { empleado: 'Jorge Díaz', dni: '49102233M', contrato: '—', estado: 'Pendiente', salarioBruto: 1450, pagoVisitas: 280, kilometraje: 40, dietas: null, incentivos: null, visitasExtra: null, total: 1770 },
      ],
      subtotal: { salarioBruto: 2900, pagoVisitas: 600, kilometraje: 125, dietas: 60, incentivos: 120, visitasExtra: 40, total: 3845 },
    },
    {
      ceco: '025888',
      titulo: 'CECO 025888 · Granini GPVs · Granini · Periodo Mayo 2026 (01/05 – 31/05)',
      subtituloCorto: 'Granini GPVs',
      filas: [
        { empleado: 'Sergi Soler', dni: '46778120K', contrato: '—', estado: 'Pendiente', salarioBruto: 1380, pagoVisitas: 210, kilometraje: 55, dietas: 45, incentivos: null, visitasExtra: 30, total: 1720 },
        { empleado: 'Silvia Fernández', dni: '53221908P', contrato: '—', estado: 'Pendiente', salarioBruto: 1380, pagoVisitas: 190, kilometraje: 70, dietas: 30, incentivos: null, visitasExtra: null, total: 1670 },
      ],
      subtotal: { salarioBruto: 2760, pagoVisitas: 400, kilometraje: 125, dietas: 75, incentivos: null, visitasExtra: 30, total: 3390 },
    },
  ]);

  protected readonly pagosTotal = signal<ConceptosImporte>({
    salarioBruto: 5660, pagoVisitas: 1000, kilometraje: 250, dietas: 135, incentivos: 120, visitasExtra: 70, total: 7235,
  });

  // Sección 2 — Borrador de Facturación DAIKIN · CECO 023301 · Mayo 2026. Datos NO reales.
  protected readonly facturacionFilas = signal<readonly FacturacionFilaDemo[]>([
    { accion: 'DAIKIN · CECO 023301', recurso: 'Castellsagüs Nogueras, Nil', coste: 2388.93, facturacion: 3250, margen: 26.5, estado: 'Borrador', editado: false },
    { accion: 'DAIKIN · CECO 023301', recurso: 'Martín Luque, Alejandro', coste: 1675, facturacion: 2300, margen: 27.2, estado: 'Borrador', editado: true },
  ]);

  protected readonly facturacionTotal = signal<FacturacionTotalDemo>({
    coste: 4063.93, facturacion: 5550, margen: 26.8,
  });

  protected readonly facturacionHistorial = signal<readonly BorradorVersionDemo[]>([
    { version: 'v3', cambio: 'Ajuste facturación A. Martín Luque', autor: 'M. Ruiz · 12:41' },
    { version: 'v2', cambio: 'Importado borrador desde pagos DAIKIN', autor: 'Sistema · 08:00' },
    { version: 'v1', cambio: 'Borrador inicial generado', autor: 'Sistema · 01/05' },
  ]);

  /** Formatea un importe de celda MAQUETA: muestra '—' cuando el valor es null, o 'N €' con miles. */
  protected cell(valor: number | null): string {
    if (valor === null) {
      return '—';
    }
    return `${new Intl.NumberFormat('es-ES', { maximumFractionDigits: 2 }).format(valor)} €`;
  }
  // =================== FIN MAQUETA / DEMO ===================

  ngOnInit(): void {
    this.recargar();
  }

  protected recargar(): void {
    this.cargando.set(true);
    this.seleccionados.set(new Set());
    const obs = this.onlyPendientes()
      ? this.approvalSvc.pendientes(1, 200)
      : this.approvalSvc.list({ page: 1, pageSize: 200 });
    obs.subscribe({
      next: (r) => { this.items.set(r.items); this.cargando.set(false); },
      error: () => { this.items.set([]); this.cargando.set(false); },
    });
  }

  protected ver(item: CierrePanelItemDto): void {
    const base = item.tipoCierre === 'Costes' ? '/cierres-costes' : '/cierres-facturacion';
    void this.router.navigate([base, item.cierreId]);
  }

  protected estaSeleccionado(item: CierrePanelItemDto): boolean {
    return this.seleccionados().has(item.cierreId);
  }

  protected toggleSeleccion(item: CierrePanelItemDto): void {
    const s = new Set(this.seleccionados());
    s.has(item.cierreId) ? s.delete(item.cierreId) : s.add(item.cierreId);
    this.seleccionados.set(s);
  }

  protected toggleTodos(event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.seleccionados.set(checked ? new Set(this.filtrados().map((i) => i.cierreId)) : new Set());
  }

  protected aprobarUno(item: CierrePanelItemDto): void {
    this.procesando.set(true);
    this.approvalSvc.batchAprobar([item.cierreId]).subscribe({
      next: () => { this.procesando.set(false); this.recargar(); },
      error: () => this.procesando.set(false),
    });
  }

  protected rechazarUno(item: CierrePanelItemDto): void {
    this.procesando.set(true);
    this.approvalSvc.batchRechazar([item.cierreId]).subscribe({
      next: () => { this.procesando.set(false); this.recargar(); },
      error: () => this.procesando.set(false),
    });
  }

  protected aprobarSeleccionados(): void {
    const ids = [...this.seleccionados()];
    if (!ids.length) return;
    this.procesando.set(true);
    this.approvalSvc.batchAprobar(ids).subscribe({
      next: () => { this.procesando.set(false); this.recargar(); },
      error: () => this.procesando.set(false),
    });
  }

  protected rechazarSeleccionados(): void {
    const ids = [...this.seleccionados()];
    if (!ids.length) return;
    this.procesando.set(true);
    this.approvalSvc.batchRechazar(ids).subscribe({
      next: () => { this.procesando.set(false); this.recargar(); },
      error: () => this.procesando.set(false),
    });
  }
}
