import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule, DecimalPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { ActivatedRoute, Router } from '@angular/router';
import { ApprovalService } from '../../core/api/approvals.service';
import { CierrePanelItemDto } from '../../models/dtos';
import { TipoCierre } from '../../models/enums';

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

      <!-- Panel -->
      <div class="sig-panel">
        <div class="sig-panel-hdr">
          <span class="sig-panel-title">
            <span class="sig-dot sig-dot--orange"></span>
            {{ onlyPendientes() ? 'Pendientes asignados a mí' : 'Cierres en el flujo de aprobación' }}
            <span class="sig-flow-hint">Flujo: Grupo &rarr; FICO &rarr; Exportado</span>
          </span>
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
                  <tr data-testid="approvals-item">
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
                    <td>
                      <button class="sig-action-ghost" (click)="ver(item)" data-testid="approvals-ver">Ver</button>
                    </td>
                  </tr>
                }
                <tr class="sig-total-row">
                  <td colspan="6" style="font-size:11px;font-weight:700;letter-spacing:.06em;text-transform:uppercase;color:var(--sig-text-muted)">Total</td>
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
    .sig-action-ghost { padding: 3px 10px; border-radius: 5px; border: 1px solid var(--sig-border); background: transparent; color: var(--sig-text-secondary); font-size: 12px; font-family: inherit; cursor: pointer; }
    .sig-total-row td { background: var(--sig-bg-card-alt); font-size: 12px; font-weight: 700; border-top: 1px solid var(--sig-border); }
  `],
})
export class ApprovalsComponent implements OnInit {
  private readonly approvalSvc = inject(ApprovalService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly onlyPendientes = signal<boolean>(this.route.snapshot.data['onlyPendientes'] === true);
  protected readonly items = signal<CierrePanelItemDto[]>([]);
  protected readonly cargando = signal(false);
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

  ngOnInit(): void {
    this.recargar();
  }

  protected recargar(): void {
    this.cargando.set(true);
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
}
