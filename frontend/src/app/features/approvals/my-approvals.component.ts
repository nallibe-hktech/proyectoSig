import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule } from '@angular/material/dialog';
import { ActivatedRoute } from '@angular/router';
import { ClosureService } from '../../core/api/closures.service';
import { AuthService } from '../../core/auth/auth.service';
import { ApprovalFilterRequest, ClosureListItemDto, EstadoClosure, ApprovalStep } from '../../models/dtos';

@Component({
  selector: 'app-my-approvals',
  standalone: true,
  imports: [CommonModule, FormsModule, DecimalPipe, MatIconModule, MatButtonModule, MatDialogModule],
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
          <button (click)="recargar()" mat-icon-button class="sig-icon-btn" [disabled]="cargando()">
            <mat-icon>{{ cargando() ? 'hourglass_empty' : 'refresh' }}</mat-icon>
          </button>
        </div>
      </div>

      <!-- Search + Filter -->
      <div class="sig-myappr-search">
        <input
          type="text"
          class="sig-search-input"
          placeholder="🔍 Buscar proyecto, cliente, período..."
          [(ngModel)]="textoBusqueda"
          (input)="aplicarFiltro()"
        />
        <select class="sig-filter-select" (change)="aplicarFiltro()">
          <option value="">Todos los períodos</option>
          <option *ngFor="let p of periodos" [value]="p">{{ p }}</option>
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
          <table class="sig-appr-table">
            <thead>
              <tr>
                <th>PERÍODO</th>
                <th>CLIENTE</th>
                <th>PROYECTO</th>
                <th>ESTADO</th>
                <th>COSTE</th>
                <th>FACTURACIÓN</th>
                <th>MARGEN</th>
                <th>ACCIONES</th>
              </tr>
            </thead>
            <tbody>
              @for (cierre of cierresFiltrados(); track cierre.id) {
                <tr (click)="seleccionar(cierre)" [class.selected]="seleccionado()?.id === cierre.id">
                  <td class="sig-mono-sm">{{ cierre.periodo }}</td>
                  <td>{{ cierre.clientNombre }}</td>
                  <td><a class="sig-link">{{ cierre.projectNombre }}</a></td>
                  <td>
                    <span class="sig-badge" [class]="'sig-badge--' + estadoClase(cierre.estado)">
                      {{ cierre.estado }}
                    </span>
                  </td>
                  <td class="sig-mono">€ {{ cierre.costeTotal | number:'1.0-0' }}</td>
                  <td class="sig-mono">€ {{ cierre.facturacionTotal | number:'1.0-0' }}</td>
                  <td>
                    <span class="sig-margen-badge" [class]="margenClase(cierre.margen)">
                      {{ (cierre.margen / cierre.facturacionTotal * 100 | number:'1.0-0') }}%
                    </span>
                  </td>
                  <td>
                    <button class="sig-btn-small" (click)="ver(cierre); $event.stopPropagation();">
                      <mat-icon>launch</mat-icon>
                    </button>
                  </td>
                </tr>
              }
              <tr class="sig-total-row">
                <td colspan="4">TOTAL</td>
                <td class="sig-mono">€ {{ totalCoste() | number:'1.0-0' }}</td>
                <td class="sig-mono">€ {{ totalFacturacion() | number:'1.0-0' }}</td>
                <td colspan="2"></td>
              </tr>
            </tbody>
          </table>
        </div>
      }

      <!-- Detail Panel (si hay seleccionado) -->
      @if (seleccionado()) {
        <div class="sig-detail-panel">
          <div class="sig-detail-header">
            <h3>{{ seleccionado()!.projectNombre }}</h3>
            <button (click)="seleccionar(null)" class="sig-close-btn">
              <mat-icon>close</mat-icon>
            </button>
          </div>
          <div class="sig-detail-kpis">
            <div class="sig-kpi-card">
              <div class="sig-kpi-label">Coste</div>
              <div class="sig-kpi-value">€ {{ seleccionado()!.costeTotal | number:'1.0-0' }}</div>
            </div>
            <div class="sig-kpi-card">
              <div class="sig-kpi-label">Facturación</div>
              <div class="sig-kpi-value">€ {{ seleccionado()!.facturacionTotal | number:'1.0-0' }}</div>
            </div>
            <div class="sig-kpi-card">
              <div class="sig-kpi-label">Margen</div>
              <div class="sig-kpi-value sig-accent">
                {{ (seleccionado()!.margen / seleccionado()!.facturacionTotal * 100 | number:'1.0-0') }}%
              </div>
            </div>
          </div>
          <div class="sig-detail-actions">
            <button class="sig-btn-approve" (click)="aprobar(seleccionado()!)">
              <mat-icon>check</mat-icon> Aprobar
            </button>
            <button class="sig-btn-reject" (click)="rechazar(seleccionado()!)">
              <mat-icon>close</mat-icon> Rechazar
            </button>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    :host { display: block; }
    .sig-myappr-page { padding: 28px; background: var(--sig-bg-app); min-height: 100vh; }
    .sig-myappr-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 20px; }
    .sig-myappr-titles { }
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
    .sig-appr-table tbody tr { cursor: pointer; transition: background 150ms; &:hover { background: var(--sig-bg-hover); } &.selected { background: rgba(59,130,246,.08); } }
    .sig-mono-sm { font-family: 'Roboto Mono', monospace; font-size: 12px; }
    .sig-mono { font-family: 'Roboto Mono', monospace; font-size: 12px; font-weight: 600; }
    .sig-link { color: var(--sig-blue); text-decoration: none; &:hover { text-decoration: underline; } }
    .sig-badge { padding: 4px 8px; border-radius: 6px; font-size: 11px; font-weight: 600; }
    .sig-badge--aprobado { background: rgba(34,197,94,.15); color: #22c55e; }
    .sig-badge--rechazado { background: rgba(239,68,68,.15); color: #ef4444; }
    .sig-badge--enaprobaciom { background: rgba(245,158,11,.15); color: #f59e0b; }
    .sig-margen-badge { padding: 4px 8px; border-radius: 6px; font-size: 11px; font-weight: 700; font-family: 'Roboto Mono', monospace; }
    .margen--high { background: rgba(34,197,94,.15); color: #22c55e; }
    .margen--med { background: rgba(245,158,11,.15); color: #f59e0b; }
    .margen--low { background: rgba(239,68,68,.15); color: #ef4444; }

    .sig-total-row td { background: var(--sig-bg-card-alt); font-weight: 700; border-top: 2px solid var(--sig-border); }
    .sig-btn-small { width: 32px; height: 32px; border-radius: 6px; border: 1px solid var(--sig-border); background: transparent; color: var(--sig-text-secondary); cursor: pointer; display: flex; align-items: center; justify-content: center; &:hover { background: var(--sig-bg-hover); } }

    .sig-detail-panel { background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 12px; padding: 20px; margin-top: 20px; }
    .sig-detail-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; h3 { margin: 0; font-size: 18px; } }
    .sig-close-btn { background: transparent; border: none; cursor: pointer; color: var(--sig-text-muted); padding: 4px; }
    .sig-detail-kpis { display: grid; grid-template-columns: repeat(3, 1fr); gap: 16px; margin-bottom: 20px; }
    .sig-kpi-card { background: var(--sig-bg-card-alt); padding: 16px; border-radius: 8px; }
    .sig-kpi-label { font-size: 11px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; color: var(--sig-text-muted); margin-bottom: 8px; }
    .sig-kpi-value { font-size: 20px; font-weight: 700; font-family: 'Roboto Mono', monospace; color: var(--sig-text-heading); }
    .sig-kpi-value.sig-accent { color: #22c55e; }
    .sig-detail-actions { display: flex; gap: 10px; }
    .sig-btn-approve { padding: 10px 16px; border-radius: 8px; border: none; background: #22c55e; color: white; font-weight: 600; cursor: pointer; display: flex; align-items: center; gap: 6px; &:hover { opacity: 0.9; } }
    .sig-btn-reject { padding: 10px 16px; border-radius: 8px; border: 1px solid rgba(239,68,68,.35); background: rgba(239,68,68,.1); color: #ef4444; font-weight: 600; cursor: pointer; display: flex; align-items: center; gap: 6px; &:hover { background: rgba(239,68,68,.18); } }
  `]
})
export class MyApprovalsComponent implements OnInit {
  private closureSvc = inject(ClosureService);
  private authSvc = inject(AuthService);

  protected cierres = signal<ClosureListItemDto[]>([]);
  protected cargando = signal(false);
  protected textoBusqueda = '';
  protected periodos: string[] = [];
  protected periodSeleccionado = '';
  protected seleccionado = signal<ClosureListItemDto | null>(null);

  protected rolActual = computed(() => {
    const roles = this.authSvc.currentUser()?.roles ?? [];
    if (roles.includes('Direction')) return 'Dirección';
    if (roles.includes('Fico')) return 'FICO';
    if (roles.includes('ProjectManager')) return 'Gestor';
    return 'Usuario';
  });

  protected cierresFiltrados = computed(() => {
    const texto = this.textoBusqueda.toLowerCase();
    return this.cierres().filter(c => {
      const coincide = c.projectNombre.toLowerCase().includes(texto) ||
                      c.clientNombre?.toLowerCase().includes(texto) ||
                      c.periodo?.toLowerCase().includes(texto);
      const periodoCoinc = !this.periodSeleccionado || c.periodo === this.periodSeleccionado;
      return coincide && periodoCoinc;
    });
  });

  protected totalPendientes = computed(() => this.cierresFiltrados().length);
  protected totalCoste = computed(() => this.cierresFiltrados().reduce((s, c) => s + c.costeTotal, 0));
  protected totalFacturacion = computed(() => this.cierresFiltrados().reduce((s, c) => s + c.facturacionTotal, 0));

  ngOnInit() {
    this.recargar();
  }

  protected recargar() {
    this.cargando.set(true);
    // Llamar al backend para obtener cierres del usuario actual
    // Por ahora usamos datos demo
    this.cierres.set([
      {
        id: 1, periodo: 'Mayo 2026', periodNombre: 'Mayo 2026', clientNombre: 'American Express', projectNombre: 'Amex Shop Small',
        projectId: 1, periodId: 1,
        costeTotal: 15000, facturacionTotal: 20500, margen: 5500, estado: 'EnAprobacion' as EstadoClosure, pasoActual: 'ProjectManager' as ApprovalStep
      },
      {
        id: 2, periodo: 'Mayo 2026', periodNombre: 'Mayo 2026', clientNombre: 'Granini', projectNombre: 'Granini GPVs',
        projectId: 2, periodId: 1,
        costeTotal: 8500, facturacionTotal: 10200, margen: 1700, estado: 'EnAprobacion' as EstadoClosure, pasoActual: 'ProjectManager' as ApprovalStep
      }
    ]);
    this.periodos = ['Mayo 2026', 'Abril 2026', 'Marzo 2026'];
    this.cargando.set(false);
  }

  protected aplicarFiltro() {
    // El computed signal se actualiza automáticamente
  }

  protected seleccionar(cierre: ClosureListItemDto | null) {
    this.seleccionado.set(cierre);
  }

  protected ver(cierre: ClosureListItemDto) {
    window.location.href = `/closures/${cierre.id}`;
  }

  protected aprobar(cierre: ClosureListItemDto) {
    alert(`Aprobando cierre ${cierre.id}`);
    // TODO: Llamar al backend
  }

  protected rechazar(cierre: ClosureListItemDto) {
    alert(`Rechazando cierre ${cierre.id}`);
    // TODO: Llamar al backend
  }

  protected estadoClase(estado: any): string {
    const s = estado?.toString().toLowerCase() || '';
    return s.includes('aprobado') ? 'aprobado' : s.includes('rechazado') ? 'rechazado' : 'enaprobaciom';
  }

  protected margenClase(margen: number): string {
    if (margen > 5000) return 'margen--high';
    if (margen > 2000) return 'margen--med';
    return 'margen--low';
  }
}
