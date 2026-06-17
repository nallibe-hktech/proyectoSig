import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { Router } from '@angular/router';
import { ApprovalService } from '../../core/api/approvals.service';
import { AuthService } from '../../core/auth/auth.service';
import { CierrePanelItemDto } from '../../models/dtos';
import { TipoCierre } from '../../models/enums';

// Ola 3b (#10): "Mis pendientes" lista cierres de AMBOS tipos (Costes/Facturación) usando
// api/approvals/pendientes. Aprobar/Rechazar se hacen en el detalle del cierre (requiere If-Match).
@Component({
  selector: 'app-my-approvals',
  standalone: true,
  imports: [CommonModule, FormsModule, DecimalPipe, MatIconModule, MatButtonModule],
  template: `
    <div class="sig-myappr-page">
      <!-- Header -->
      <div class="sig-myappr-header">
        <div class="sig-myappr-titles">
          <h1 class="sig-myappr-title">
            <mat-icon>task_alt</mat-icon>
            Mis Aprobaciones
          </h1>
          <p class="sig-myappr-sub">{{ rolActual() }} · {{ totalPendientes() }} pendiente(s)</p>
        </div>
        <div class="sig-myappr-actions">
          <button (click)="recargar()" mat-icon-button class="sig-icon-btn" [disabled]="cargando()" data-testid="mis-aprobaciones-recargar">
            <mat-icon>{{ cargando() ? 'hourglass_empty' : 'refresh' }}</mat-icon>
          </button>
        </div>
      </div>

      <!-- Search + Filter -->
      <div class="sig-myappr-search">
        <input
          type="text"
          class="sig-search-input"
          placeholder="Buscar servicio, cliente, período..."
          [(ngModel)]="textoBusqueda"
          data-testid="mis-aprobaciones-busqueda"
        />
        <select class="sig-filter-select" [(ngModel)]="tipoSeleccionado" data-testid="mis-aprobaciones-filtro-tipo">
          <option value="">Todos los tipos</option>
          <option value="Costes">Costes</option>
          <option value="Facturacion">Facturación</option>
        </select>
      </div>

      <!-- Pendientes Table -->
      @if (cargando()) {
        <div class="sig-skeleton-table">
          @for (_ of [0,1,2]; track _) {
            <div class="sig-skeleton-row"></div>
          }
        </div>
      } @else if (cierresFiltrados().length === 0) {
        <div class="sig-empty-state">
          <mat-icon>check_circle</mat-icon>
          <span>Todas las aprobaciones están al día</span>
        </div>
      } @else {
        <div class="sig-table-card">
          <table class="sig-appr-table" data-testid="mis-aprobaciones-tabla">
            <thead>
              <tr>
                <th>PERÍODO</th>
                <th>CLIENTE</th>
                <th>SERVICIO</th>
                <th>TIPO</th>
                <th>ESTADO</th>
                <th>PASO</th>
                <th>TOTAL</th>
                <th>ACCIONES</th>
              </tr>
            </thead>
            <tbody>
              @for (cierre of cierresFiltrados(); track cierre.cierreId + '-' + cierre.tipoCierre) {
                <tr data-testid="mis-aprobaciones-item">
                  <td class="sig-mono-sm">{{ cierre.periodNombre }}</td>
                  <td>{{ cierre.clientNombre }}</td>
                  <td><a class="sig-link">{{ cierre.serviceNombre }}</a></td>
                  <td>
                    <span class="sig-badge" [class]="cierre.tipoCierre === 'Costes' ? 'sig-badge--enaprobaciom' : 'sig-badge--aprobado'">
                      {{ cierre.tipoCierre === 'Costes' ? 'Costes' : 'Facturación' }}
                    </span>
                  </td>
                  <td>
                    <span class="sig-badge" [class]="'sig-badge--' + estadoClase(cierre.estado)">
                      {{ cierre.estado }}
                    </span>
                  </td>
                  <td>{{ cierre.pasoActualRol }}</td>
                  <td class="sig-mono">€ {{ cierre.total | number:'1.0-0' }}</td>
                  <td>
                    <button class="sig-btn-small" (click)="ver(cierre)" data-testid="mis-aprobaciones-ver">
                      <mat-icon>launch</mat-icon>
                    </button>
                  </td>
                </tr>
              }
              <tr class="sig-total-row">
                <td colspan="6">TOTAL</td>
                <td class="sig-mono">€ {{ totalImporte() | number:'1.0-0' }}</td>
                <td></td>
              </tr>
            </tbody>
          </table>
        </div>
      }
    </div>
  `,
  styles: [`
    :host { display: block; }
    .sig-myappr-page { padding: 28px; background: var(--sig-bg-app); min-height: 100vh; }
    .sig-myappr-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 20px; }
    .sig-myappr-title { font-size: 24px; font-weight: 700; margin: 0 0 4px; display: flex; align-items: center; gap: 8px; mat-icon { color: var(--sig-teal); } }
    .sig-myappr-sub { font-size: 13px; color: var(--sig-text-muted); margin: 0; }
    .sig-myappr-actions { display: flex; gap: 8px; }
    .sig-icon-btn { background: var(--sig-bg-card) !important; border: 1px solid var(--sig-border) !important; }

    .sig-myappr-search { display: flex; gap: 10px; margin-bottom: 20px; }
    .sig-search-input { flex: 1; padding: 10px 14px; border: 1px solid var(--sig-border); border-radius: 8px; background: var(--sig-bg-card); color: var(--sig-text-primary); font-family: inherit; font-size: 13px; outline: none; &:focus { border-color: var(--sig-blue); } }
    .sig-filter-select { padding: 10px 12px; border: 1px solid var(--sig-border); border-radius: 8px; background: var(--sig-bg-card); color: var(--sig-text-primary); font-family: inherit; font-size: 13px; outline: none; &:focus { border-color: var(--sig-blue); } }

    .sig-skeleton-table { display: flex; flex-direction: column; gap: 8px; }
    .sig-skeleton-row { height: 48px; background: rgba(255,255,255,.04); border-radius: 8px; animation: sig-pulse 2s infinite; }
    @keyframes sig-pulse { 0%, 100% { opacity: 0.4; } 50% { opacity: 0.8; } }

    .sig-empty-state { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 12px; padding: 60px 20px; color: var(--sig-text-muted); mat-icon { font-size: 48px; opacity: 0.3; } span { font-size: 16px; } }

    .sig-table-card { background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 12px; overflow: hidden; }
    .sig-appr-table { width: 100%; border-collapse: collapse; }
    .sig-appr-table thead { background: var(--sig-bg-header); }
    .sig-appr-table th { padding: 12px 14px; font-size: 10px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; color: var(--sig-text-muted); text-align: left; }
    .sig-appr-table td { padding: 12px 14px; font-size: 13px; color: var(--sig-text-primary); border-bottom: 1px solid var(--sig-border); }
    .sig-appr-table tbody tr { transition: background 150ms; &:hover { background: var(--sig-bg-hover); } }
    .sig-mono-sm { font-family: 'Roboto Mono', monospace; font-size: 12px; }
    .sig-mono { font-family: 'Roboto Mono', monospace; font-size: 12px; font-weight: 600; }
    .sig-link { color: var(--sig-blue); text-decoration: none; &:hover { text-decoration: underline; } }
    .sig-badge { padding: 4px 8px; border-radius: 6px; font-size: 11px; font-weight: 600; }
    .sig-badge--aprobado { background: rgba(34,197,94,.15); color: #22c55e; }
    .sig-badge--rechazado { background: rgba(239,68,68,.15); color: #ef4444; }
    .sig-badge--enaprobaciom { background: rgba(245,158,11,.15); color: #f59e0b; }

    .sig-total-row td { background: var(--sig-bg-card-alt); font-weight: 700; border-top: 2px solid var(--sig-border); }
    .sig-btn-small { width: 32px; height: 32px; border-radius: 6px; border: 1px solid var(--sig-border); background: transparent; color: var(--sig-text-secondary); cursor: pointer; display: flex; align-items: center; justify-content: center; &:hover { background: var(--sig-bg-hover); } }
  `]
})
export class MyApprovalsComponent implements OnInit {
  private readonly approvalSvc = inject(ApprovalService);
  private readonly authSvc = inject(AuthService);
  private readonly router = inject(Router);

  protected cierres = signal<CierrePanelItemDto[]>([]);
  protected cargando = signal(false);
  protected textoBusqueda = '';
  protected tipoSeleccionado: '' | TipoCierre = '';

  protected rolActual = computed(() => {
    const roles = this.authSvc.currentUser()?.roles ?? [];
    if (roles.includes('Fico')) return 'FICO';
    if (roles.includes('Gestor') || roles.includes('Facilitador') || roles.includes('Interlocutor')) return 'Grupo';
    return 'Usuario';
  });

  protected cierresFiltrados = computed(() => {
    const texto = this.textoBusqueda.toLowerCase();
    return this.cierres().filter(c => {
      const coincide = c.serviceNombre.toLowerCase().includes(texto) ||
                      c.clientNombre.toLowerCase().includes(texto) ||
                      c.periodNombre.toLowerCase().includes(texto);
      const tipoCoinc = !this.tipoSeleccionado || c.tipoCierre === this.tipoSeleccionado;
      return coincide && tipoCoinc;
    });
  });

  protected totalPendientes = computed(() => this.cierresFiltrados().length);
  protected totalImporte = computed(() => this.cierresFiltrados().reduce((s, c) => s + c.total, 0));

  ngOnInit() {
    this.recargar();
  }

  protected recargar() {
    this.cargando.set(true);
    this.approvalSvc.pendientes(1, 200).subscribe({
      next: (r) => { this.cierres.set(r.items); this.cargando.set(false); },
      error: () => { this.cierres.set([]); this.cargando.set(false); },
    });
  }

  protected ver(cierre: CierrePanelItemDto) {
    const base = cierre.tipoCierre === 'Costes' ? '/cierres-costes' : '/cierres-facturacion';
    void this.router.navigate([base, cierre.cierreId]);
  }

  protected estadoClase(estado: string): string {
    const s = estado?.toString().toLowerCase() || '';
    return s.includes('aprobado') ? 'aprobado' : s.includes('rechazado') ? 'rechazado' : 'enaprobaciom';
  }
}
