import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { PeriodService } from '../../core/api/periods.service';
import { PeriodDto } from '../../models/dtos';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { NotifyService } from '../../core/notify.service';

@Component({
  selector: 'app-periods-list',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink, MatIconModule, MatDialogModule, MatPaginatorModule, SkeletonComponent, EmptyStateComponent],
  template: `
    <div class="sig-list-page">
      <div class="sig-list-topbar">
        <h1 class="sig-page-title">
          <mat-icon>calendar_month</mat-icon>
          Periodos
          <span class="sig-total-chip">{{ total() }}</span>
        </h1>
        <div class="sig-topbar-actions">
          <a class="sig-btn-primary" routerLink="/periods/nuevo" data-testid="btn-nuevo"><mat-icon>add</mat-icon> Abrir Periodo</a>
        </div>
      </div>
      <nav class="sig-breadcrumb"><a routerLink="/dashboard">Inicio</a> <span>›</span> <span>Periodos</span></nav>

      <div class="sig-content-area">
        <!-- Tabla de periodos (datos reales) -->
        <div class="sig-table-wrap">
          @if (loading()) {
            <div style="padding:16px"><sig-skeleton [count]="5" /></div>
          } @else if (items().length === 0) {
            <sig-empty-state icon="calendar_month" title="No hay períodos" ctaLabel="Abrir primer período" (ctaClick)="router.navigate(['/periods/nuevo'])" />
          } @else {
            <table data-testid="tabla-periods">
              <thead>
                <tr>
                  <th>PERIODO</th>
                  <th>CIERRE NÓMINAS</th>
                  <th>CIERRE FACTURACIÓN</th>
                  <th>ESTADO</th>
                  <th style="text-align:right">ACCIONES</th>
                </tr>
              </thead>
              <tbody>
                @for (row of items(); track row.id) {
                  <tr data-testid="row-period">
                    <td>
                      <div class="sig-periodo-name">{{ row.nombre }}</div>
                      <div class="sig-periodo-range mono-num">{{ row.fechaInicio | date:'dd/MM' }} – {{ row.fechaFin | date:'dd/MM' }}</div>
                    </td>
                    <td><span [class]="'sig-estado-badge ' + estadoCls(row.estado)" data-testid="badge-estado">{{ estadoLabel(row.estado) }}</span></td>
                    <td><span [class]="'sig-estado-badge ' + estadoCls(row.estado)">{{ estadoLabel(row.estado) }}</span></td>
                    <td><span [class]="'sig-estado-badge ' + estadoCls(row.estado)">{{ estadoLabel(row.estado) }}</span></td>
                    <td>
                      <div class="sig-row-actions">
                        @if (row.estado === 'Abierto') {
                          <button class="sig-btn-ghost" (click)="onCerrar(row)" [attr.data-testid]="'btn-cerrar-' + row.id"><mat-icon>lock</mat-icon> Cerrar</button>
                        } @else if (row.estado === 'Cerrado') {
                          <button class="sig-btn-ghost" (click)="onReabrir(row)" [attr.data-testid]="'btn-reabrir-' + row.id"><mat-icon>lock_open</mat-icon> Reabrir</button>
                        }
                        <a class="sig-icon-btn" [routerLink]="['/periods', row.id, 'editar']" [attr.data-testid]="'btn-editar-' + row.id" aria-label="Editar"><mat-icon>edit</mat-icon></a>
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
            <div class="sig-footer-note">
              Día de pago del periodo seleccionado: <strong class="mono-num" data-testid="cell-dia-pago">{{ items()[0]?.diaPago ?? '—' }}</strong>.
              El desglose <em>Nóminas / Facturación</em> y el cierre por servicio son ilustrativos hasta que el backend exponga el detalle por servicio.
            </div>
            <mat-paginator
              [length]="total()"
              [pageSize]="pageSize()"
              [pageIndex]="page() - 1"
              [pageSizeOptions]="[10, 25, 50, 100]"
              showFirstLastButtons
              (page)="onPageChange($event)"
            ></mat-paginator>
          }
        </div>

        <!-- Panel lateral: fechas de pago + ciclo de vida + cierre por servicio -->
        <aside class="sig-side">
          <section class="sig-side-card">
            <div class="sig-side-title"><mat-icon>event</mat-icon> Cierres y fechas de pago</div>
            <div class="sig-pay-block">
              <span class="sig-pay-tag tag--fact">Facturación</span>
              <div class="sig-pay-line">Emisión: <strong>día 9</strong> de cada mes</div>
            </div>
            <div class="sig-pay-block">
              <span class="sig-pay-tag tag--nom">Nóminas (costes)</span>
              <div class="sig-pay-line">Pago grupo A: <strong>día 30</strong> (fin de mes)</div>
              <div class="sig-pay-line">Pago grupo B: <strong>día 15</strong> (mes vencido)</div>
            </div>
            <div class="sig-pay-note">Cálculo sobre actividad 01–30 del mes anterior.</div>
          </section>

          <section class="sig-side-card">
            <div class="sig-side-title"><mat-icon>timeline</mat-icon> Ciclo de vida del periodo</div>
            <ul class="sig-lifecycle">
              <li><span class="sig-lc-ico lc--ok">✓</span> Abierto</li>
              <li><span class="sig-lc-ico lc--ok">✓</span> Revisado <span class="sig-lc-sub">(operaciones cargó datos)</span></li>
              <li><span class="sig-lc-ico lc--alert">!</span> Revisión con alertas <span class="sig-lc-sub">(incidencias por resolver)</span></li>
              <li><span class="sig-lc-ico lc--muted">—</span> Bloqueado</li>
              <li><span class="sig-lc-ico lc--muted">—</span> Cerrado <span class="sig-lc-sub">(finanzas)</span></li>
            </ul>
          </section>

          <section class="sig-side-card">
            <div class="sig-side-title">
              <mat-icon>fact_check</mat-icon> Cierre por servicio
              <span class="sig-demo-tag">demo</span>
            </div>
            <table class="sig-svc-table">
              <thead><tr><th>Servicio</th><th>Nómina</th><th>Factura</th></tr></thead>
              <tbody>
                <tr><td>Amex Shop Small</td><td><span class="sig-svc-badge ok">OK</span></td><td><span class="sig-svc-badge open">Abierto</span></td></tr>
                <tr><td>Granini GPVs</td><td><span class="sig-svc-badge alert">Alerta</span></td><td><span class="sig-svc-badge open">Abierto</span></td></tr>
                <tr><td>Amex New</td><td><span class="sig-svc-badge ok">OK</span></td><td><span class="sig-svc-badge ok">OK</span></td></tr>
              </tbody>
            </table>
            <p class="sig-svc-note">El interlocutor valida los gastos antes del cierre y puede aprobar o bloquear cada servicio.</p>
            <div class="sig-svc-actions">
              <button class="sig-btn-ghost" disabled>Validación previa de gastos</button>
              <button class="sig-btn-approve" disabled>Aprobar cierre</button>
              <button class="sig-btn-block" disabled>Bloquear</button>
            </div>
          </section>
        </aside>
      </div>
    </div>
  `,
  styles: [`
    :host { display: block; }
    .sig-list-page { display: flex; flex-direction: column; height: 100%; }
    .sig-list-topbar { display: flex; align-items: center; justify-content: space-between; padding: 20px 24px 0; }
    .sig-page-title { font-size: 20px; font-weight: 700; color: var(--sig-text-heading); margin: 0; display: flex; align-items: center; gap: 10px; mat-icon { color: var(--sig-teal); font-size: 20px; width: 20px; height: 20px; } }
    .sig-total-chip { font-size: 11px; font-weight: 700; padding: 2px 8px; border-radius: 10px; background: var(--sig-bg-active); color: var(--sig-teal); border: 1px solid rgba(0,212,196,.2); }
    .sig-topbar-actions { display: flex; gap: 8px; }
    .sig-btn-primary { display: inline-flex; align-items: center; gap: 6px; padding: 0 16px; height: 36px; border-radius: 8px; background: var(--sig-blue); color: #fff; border: none; font-size: 13px; font-weight: 600; font-family: inherit; cursor: pointer; text-decoration: none; mat-icon { font-size: 16px !important; width: 16px !important; height: 16px !important; } }
    .sig-breadcrumb { padding: 6px 24px 12px; font-size: 12px; color: var(--sig-text-muted); display: flex; gap: 6px; a { color: var(--sig-blue); text-decoration: none; } span { color: var(--sig-text-muted); } }
    .sig-content-area { flex: 1; display: flex; overflow: hidden; padding: 0 24px 24px; gap: 16px; }
    .sig-table-wrap { flex: 1; overflow: auto; background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 12px; min-width: 0; }
    table[data-testid="tabla-periods"] { width: 100%; border-collapse: collapse; }
    thead tr { background: var(--sig-bg-header); border-bottom: 1px solid var(--sig-border); }
    th { padding: 11px 16px; font-size: 10px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; color: var(--sig-text-muted); text-align: left; }
    td { padding: 12px 16px; font-size: 13px; color: var(--sig-text-primary); border-bottom: 1px solid var(--sig-border); vertical-align: middle; }
    tbody tr:last-child td { border-bottom: none; }
    .sig-periodo-name { font-weight: 600; color: var(--sig-text-heading); }
    .sig-periodo-range { font-size: 11px; color: var(--sig-text-muted); margin-top: 2px; }
    .mono-num { font-variant-numeric: tabular-nums; }
    .sig-estado-badge { display: inline-block; padding: 3px 10px; border-radius: 6px; font-size: 11px; font-weight: 700; letter-spacing: .03em; }
    .estado--abierto  { background: rgba(59,130,246,.14); color: #3b82f6; }
    .estado--revisado { background: rgba(0,212,196,.14); color: #00d4c4; }
    .estado--alerta   { background: rgba(245,158,11,.14); color: #f59e0b; }
    .estado--bloqueado{ background: rgba(148,163,184,.16); color: #94a3b8; }
    .estado--cerrado  { background: rgba(148,163,184,.12); color: #94a3b8; }
    .sig-row-actions { display: flex; align-items: center; gap: 6px; justify-content: flex-end; }
    .sig-btn-ghost { display: inline-flex; align-items: center; gap: 5px; height: 30px; padding: 0 12px; border-radius: 7px; background: transparent; color: var(--sig-text-secondary); border: 1px solid var(--sig-border); font-size: 12px; font-family: inherit; cursor: pointer; &:disabled { opacity: .5; cursor: default; } mat-icon { font-size: 15px !important; width: 15px !important; height: 15px !important; } }
    .sig-icon-btn { width: 30px; height: 30px; border-radius: 7px; border: 1px solid var(--sig-border); display: inline-flex; align-items: center; justify-content: center; color: var(--sig-text-secondary); cursor: pointer; text-decoration: none; mat-icon { font-size: 15px !important; width: 15px !important; height: 15px !important; } }
    .sig-footer-note { padding: 10px 16px; font-size: 11px; color: var(--sig-text-muted); border-top: 1px solid var(--sig-border); em { font-style: italic; } }

    .sig-side { width: 320px; flex-shrink: 0; display: flex; flex-direction: column; gap: 16px; overflow-y: auto; }
    .sig-side-card { background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 12px; padding: 14px 16px; }
    .sig-side-title { display: flex; align-items: center; gap: 8px; font-size: 12px; font-weight: 700; letter-spacing: .04em; text-transform: uppercase; color: var(--sig-text-secondary); margin-bottom: 12px; mat-icon { color: var(--sig-teal); font-size: 17px !important; width: 17px !important; height: 17px !important; } }
    .sig-demo-tag { margin-left: auto; font-size: 9px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; color: var(--sig-warning); background: rgba(245,158,11,.12); border: 1px solid rgba(245,158,11,.3); padding: 1px 7px; border-radius: 9px; }
    .sig-pay-block { padding: 8px 0; border-bottom: 1px solid var(--sig-border); &:last-of-type { border-bottom: none; } }
    .sig-pay-tag { display: inline-block; font-size: 10px; font-weight: 700; text-transform: uppercase; letter-spacing: .05em; padding: 2px 8px; border-radius: 5px; margin-bottom: 6px; }
    .tag--fact { background: rgba(0,212,196,.12); color: #00d4c4; }
    .tag--nom  { background: rgba(59,130,246,.12); color: #3b82f6; }
    .sig-pay-line { font-size: 12px; color: var(--sig-text-primary); padding: 2px 0; strong { color: var(--sig-text-heading); } }
    .sig-pay-note { font-size: 11px; color: var(--sig-text-muted); margin-top: 8px; font-style: italic; }
    .sig-lifecycle { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 8px; li { font-size: 12px; color: var(--sig-text-primary); display: flex; align-items: center; gap: 8px; } }
    .sig-lc-ico { width: 18px; height: 18px; border-radius: 50%; display: inline-flex; align-items: center; justify-content: center; font-size: 11px; font-weight: 800; flex-shrink: 0; }
    .lc--ok    { background: rgba(0,212,196,.16); color: #00d4c4; }
    .lc--alert { background: rgba(245,158,11,.16); color: #f59e0b; }
    .lc--muted { background: rgba(148,163,184,.16); color: #94a3b8; }
    .sig-lc-sub { color: var(--sig-text-muted); font-size: 11px; }
    .sig-svc-table { width: 100%; border-collapse: collapse; th { padding: 6px 4px; font-size: 9px; text-transform: uppercase; letter-spacing: .05em; color: var(--sig-text-muted); text-align: left; } td { padding: 7px 4px; font-size: 12px; color: var(--sig-text-primary); border-bottom: 1px solid var(--sig-border); } tbody tr:last-child td { border-bottom: none; } }
    .sig-svc-badge { display: inline-block; padding: 2px 8px; border-radius: 5px; font-size: 10px; font-weight: 700; }
    .sig-svc-badge.ok    { background: rgba(0,212,196,.14); color: #00d4c4; }
    .sig-svc-badge.alert { background: rgba(245,158,11,.14); color: #f59e0b; }
    .sig-svc-badge.open  { background: rgba(59,130,246,.14); color: #3b82f6; }
    .sig-svc-note { font-size: 11px; color: var(--sig-text-muted); margin: 10px 0; line-height: 1.4; }
    .sig-svc-actions { display: flex; flex-wrap: wrap; gap: 6px; }
    .sig-btn-approve { height: 30px; padding: 0 12px; border-radius: 7px; border: none; background: var(--sig-teal); color: #04201d; font-size: 12px; font-weight: 600; font-family: inherit; cursor: pointer; opacity: .5; }
    .sig-btn-block { height: 30px; padding: 0 12px; border-radius: 7px; border: 1px solid rgba(239,68,68,.4); background: rgba(239,68,68,.08); color: #ef4444; font-size: 12px; font-family: inherit; cursor: pointer; opacity: .6; }

    @media (max-width: 1100px) {
      .sig-content-area { flex-direction: column; overflow: visible; }
      .sig-side { width: 100%; flex-direction: row; flex-wrap: wrap; }
      .sig-side-card { flex: 1 1 280px; }
    }
  `],
})
export class PeriodsListComponent implements OnInit {
  private readonly periodSvc = inject(PeriodService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotifyService);
  protected readonly router = inject(Router);

  protected readonly page = signal(1);
  protected readonly pageSize = signal(25);
  protected readonly total = signal(0);
  protected readonly items = signal<PeriodDto[]>([]);
  protected readonly loading = signal(true);

  ngOnInit(): void { this.load(); }

  protected onPageChange(event: PageEvent): void {
    this.page.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    window.scrollTo({ top: 0, behavior: 'smooth' });
    this.load();
  }

  protected estadoCls(e: string): string {
    switch (e) {
      case 'Abierto': return 'estado--abierto';
      case 'Revisado': return 'estado--revisado';
      case 'RevisionConAlertas':
      case 'Revisión con alertas': return 'estado--alerta';
      case 'Bloqueado': return 'estado--bloqueado';
      case 'Cerrado': return 'estado--cerrado';
      default: return 'estado--abierto';
    }
  }

  protected estadoLabel(e: string): string {
    return (e ?? '').toString().toUpperCase();
  }

  protected onCerrar(row: PeriodDto): void {
    this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Cerrar período', message: 'Una vez cerrado, no se podrán crear nuevos cierres.', entityName: row.nombre, confirmLabel: 'Cerrar período' },
      minWidth: 480,
    }).afterClosed().subscribe((ok) => {
      if (!ok) return;
      this.periodSvc.cerrar(row.id).subscribe({
        next: () => { this.notify.success('Período cerrado'); this.page.set(1); this.load(); },
        error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo cerrar'),
      });
    });
  }

  protected onReabrir(row: PeriodDto): void {
    this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Reabrir período', message: '¿Confirmas la reapertura?', entityName: row.nombre, confirmLabel: 'Reabrir' },
      minWidth: 480,
    }).afterClosed().subscribe((ok) => {
      if (!ok) return;
      this.periodSvc.reabrir(row.id).subscribe({
        next: () => { this.notify.success('Período reabierto'); this.page.set(1); this.load(); },
        error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo reabrir'),
      });
    });
  }

  private load(): void {
    this.loading.set(true);
    this.periodSvc.listPaginated(this.page(), this.pageSize()).subscribe({
      next: (response) => {
        this.items.set(response.items || []);
        this.total.set(response.total || 0);
        this.loading.set(false);
      },
      error: () => { this.items.set([]); this.total.set(0); this.loading.set(false); },
    });
  }
}
