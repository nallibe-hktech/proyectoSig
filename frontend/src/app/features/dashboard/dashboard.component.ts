import { Component, inject, OnInit, signal, computed, effect } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { DashboardService } from '../../core/api/dashboard.service';
import { PeriodService } from '../../core/api/periods.service';
import { NotifyService } from '../../core/notify.service';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { StateBadgeComponent } from '../../shared/state-badge.component';
import { ChartSlice } from '../../shared/pie-chart.component';
import { DashboardKpisDto, DashboardAvisoDto, MiProyectoDto } from '../../models/dtos';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule, RouterLink, DecimalPipe,
    MatCardModule, MatIconModule, MatTableModule, MatButtonModule, MatDividerModule,
    BreadcrumbsComponent, SkeletonComponent, StateBadgeComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Dashboard' }]" />

      <div class="sig-dashboard-topbar">
        <h1 class="sig-page__title" data-testid="page-title">Dashboard</h1>
        @if (kpis()) {
          <span class="sig-dash-period">{{ kpis()!.periodNombre }}</span>
        }
        <span class="sig-spacer"></span>
        <button mat-stroked-button class="sig-recalc-btn" data-testid="btn-recalcular">
          <mat-icon aria-hidden="true">refresh</mat-icon> Recalcular
        </button>
        <button mat-icon-button class="sig-notif-btn" data-testid="btn-notificaciones">
          <mat-icon>notifications</mat-icon>
          <span class="sig-notif-badge">3</span>
        </button>
      </div>

      <!-- KPIs with accent bars -->
      <div class="sig-dashboard-kpis">
        @if (loadingKpis()) {
          @for (_ of [0,1,2,3]; track $index) {
            <mat-card class="sig-kpi-card">
              <mat-card-content>
                <div class="sig-skeleton-text" style="width: 60%; margin-bottom: 12px;"></div>
                <div class="sig-skeleton" style="height: 40px; width: 70%;"></div>
                <div class="sig-skeleton-text" style="width: 50%; margin-top: 12px;"></div>
              </mat-card-content>
            </mat-card>
          }
        } @else if (kpis()) {
          <mat-card class="sig-kpi-card sig-kpi-card--primary" data-testid="kpi-cierres-completados">
            <mat-card-content>
              <div class="sig-kpi-label">Cierres completados</div>
              <div class="sig-kpi-value">{{ kpis()!.cierresCompletados }}</div>
              <div class="sig-kpi-trend sig-kpi-trend--up">
                <mat-icon aria-hidden="true">trending_up</mat-icon>
                2 vs mes ant.
              </div>
            </mat-card-content>
          </mat-card>
          <mat-card class="sig-kpi-card sig-kpi-card--warning" data-testid="kpi-cierres-pendientes">
            <mat-card-content>
              <div class="sig-kpi-label">Pend. aprobaci&oacute;n</div>
              <div class="sig-kpi-value">{{ kpis()!.cierresPendientes }}</div>
              <div class="sig-kpi-trend sig-kpi-trend--warn">
                <mat-icon aria-hidden="true">warning</mat-icon>
                Requiere atenci&oacute;n
              </div>
            </mat-card-content>
          </mat-card>
          <mat-card class="sig-kpi-card sig-kpi-card--success" data-testid="kpi-facturacion-total">
            <mat-card-content>
              <div class="sig-kpi-label">Facturaci&oacute;n total</div>
              <div class="sig-kpi-value mono-num">{{ kpis()!.facturacionTotal | number:'1.0-0' }} &euro;</div>
              <div class="sig-kpi-trend sig-kpi-trend--up">
                <mat-icon aria-hidden="true">trending_up</mat-icon>
                +12% vs mes ant.
              </div>
            </mat-card-content>
          </mat-card>
          <mat-card class="sig-kpi-card sig-kpi-card--dark" data-testid="kpi-margen">
            <mat-card-content>
              <div class="sig-kpi-label">Margen promedio</div>
              <div class="sig-kpi-value mono-num">{{ kpis()!.facturacionTotal > 0 ? ((kpis()!.margen / kpis()!.facturacionTotal) * 100 | number:'1.0-0') : '0' }}%</div>
              <div class="sig-kpi-trend sig-kpi-trend--neutral">
                Objetivo: 25%
              </div>
            </mat-card-content>
          </mat-card>
        } @else {
          <mat-card class="sig-kpi-card">
            <mat-card-content>
              <div class="sig-empty-msg">
                <mat-icon aria-hidden="true">info</mat-icon>
                <span>No hay datos para mostrar.</span>
              </div>
            </mat-card-content>
          </mat-card>
        }
      </div>

      <!-- Alertas + Bar chart -->
      <div class="sig-dashboard-row">
        <mat-card class="sig-avisos-card" data-testid="panel-avisos">
          <mat-card-header>
            <mat-card-title>Alertas</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            @if (loadingAvisos()) {
              <sig-skeleton [count]="3" />
            } @else if (avisos().length === 0) {
              <div class="sig-empty-msg">
                <mat-icon aria-hidden="true">check_circle</mat-icon>
                <span>No hay alertas activas.</span>
              </div>
            } @else {
              <div class="sig-alerta-cards">
                @for (a of avisos(); track a.descripcion) {
                  <div class="sig-alerta-card" [class]="'sig-alerta-card--' + a.tipo" [attr.data-testid]="'aviso-' + a.tipo">
                    <mat-icon [class]="avisoIconClass(a.tipo)" aria-hidden="true">
                      {{ avisoIcon(a.tipo) }}
                    </mat-icon>
                    <div class="sig-alerta-text">
                      <span class="sig-alerta-title">{{ a.tipo }}</span>
                      <span class="sig-alerta-desc">{{ a.descripcion }}</span>
                    </div>
                  </div>
                }
              </div>
            }
          </mat-card-content>
        </mat-card>

        <mat-card class="sig-chart-card" data-testid="grafico-margen">
          <mat-card-header>
            <mat-card-title>Margen por Proyecto — {{ kpis()?.periodNombre ?? 'actual' }}</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            @if (chartProyectos().length > 0) {
              <div class="sig-bar-chart">
                @for (item of chartProyectos(); track item.nombre) {
                  <div class="sig-bar-row">
                    <span class="sig-bar-label">{{ item.nombre }}</span>
                    <div class="sig-bar-track">
                      <div class="sig-bar-fill" [style.width.%]="item.margen" [style.background]="item.color"></div>
                      <span class="sig-bar-value">{{ item.margen }}%</span>
                    </div>
                  </div>
                }
                <div class="sig-bar-target">
                  <span class="sig-bar-label">Objetivo</span>
                  <div class="sig-bar-track">
                    <div class="sig-bar-target-line" style="left: 25%;"></div>
                    <span class="sig-bar-value">25%</span>
                  </div>
                </div>
              </div>
            } @else {
              <div class="sig-empty-msg" style="padding: 32px 0;">
                <mat-icon aria-hidden="true">bar_chart</mat-icon>
                <span>Sin datos para representar.</span>
              </div>
            }
          </mat-card-content>
        </mat-card>
      </div>

      <!-- Mis proyectos -->
      <h2 class="sig-section-title">Proyectos Activos</h2>
      <mat-card>
        <mat-card-content>
          @if (loadingMis()) {
            <sig-skeleton [count]="4" />
          } @else if (misProyectos().length === 0) {
            <div class="sig-empty-msg" style="padding: 24px;">
              <mat-icon aria-hidden="true">folder_off</mat-icon>
              <span>No tienes proyectos asignados para este per&iacute;odo.</span>
            </div>
          } @else {
            <table mat-table [dataSource]="misProyectos()" class="sig-table sig-table--dashboard" data-testid="tabla-mis-proyectos">
              <ng-container matColumnDef="proyecto">
                <th mat-header-cell *matHeaderCellDef>PROYECTO</th>
                <td mat-cell *matCellDef="let row">{{ row.nombre }}</td>
              </ng-container>
              <ng-container matColumnDef="cliente">
                <th mat-header-cell *matHeaderCellDef>CLIENTE</th>
                <td mat-cell *matCellDef="let row">{{ row.clientNombre }}</td>
              </ng-container>
              <ng-container matColumnDef="estado">
                <th mat-header-cell *matHeaderCellDef>ESTADO</th>
                <td mat-cell *matCellDef="let row">
                  @if (row.estado && row.pasoActual) {
                    <sig-state-badge [estado]="row.estado" [paso]="row.pasoActual" />
                  } @else {
                    <span style="color: var(--sig-text-muted); font-size: 12px;">Sin cierre</span>
                  }
                </td>
              </ng-container>
              <ng-container matColumnDef="coste">
                <th mat-header-cell *matHeaderCellDef>COSTE</th>
                <td mat-cell *matCellDef="let row" class="mono-num">&mdash;</td>
              </ng-container>
              <ng-container matColumnDef="facturacion">
                <th mat-header-cell *matHeaderCellDef>FACTURACI&Oacute;N</th>
                <td mat-cell *matCellDef="let row" class="mono-num">&mdash;</td>
              </ng-container>
              <ng-container matColumnDef="margen">
                <th mat-header-cell *matHeaderCellDef>MARGEN</th>
                <td mat-cell *matCellDef="let row" class="mono-num">&mdash;</td>
              </ng-container>
              <ng-container matColumnDef="acciones">
                <th mat-header-cell *matHeaderCellDef style="text-align: right;">ACCIONES</th>
                <td mat-cell *matCellDef="let row" style="text-align: right;">
                  @if (row.closureId) {
                    <a mat-icon-button [routerLink]="['/closures', row.closureId]" [attr.data-testid]="'btn-ver-closure-' + row.closureId" aria-label="Ver cierre">
                      <mat-icon>arrow_forward</mat-icon>
                    </a>
                  } @else {
                    <a mat-icon-button [routerLink]="['/projects', row.projectId]" [attr.data-testid]="'btn-ver-project-' + row.projectId" aria-label="Ver proyecto">
                      <mat-icon>visibility</mat-icon>
                    </a>
                  }
                </td>
              </ng-container>

              <tr mat-header-row *matHeaderRowDef="['proyecto', 'cliente', 'estado', 'coste', 'facturacion', 'margen', 'acciones']"></tr>
              <tr mat-row *matRowDef="let row; columns: ['proyecto', 'cliente', 'estado', 'coste', 'facturacion', 'margen', 'acciones']" data-testid="row-mi-proyecto"></tr>
            </table>
          }
        </mat-card-content>
      </mat-card>

      <!-- Estado de integraciones -->
      <mat-card class="sig-int-card" data-testid="panel-integraciones">
        <mat-card-content>
          <div class="sig-int-bar">
            <span class="sig-int-bar-label">Estado de Integraciones</span>
            <div class="sig-int-dots">
              <span class="sig-int-dot" title="Celero"><span class="sig-bullet sig-bullet--green"></span> Celero</span>
              <span class="sig-int-dot" title="Bizneo"><span class="sig-bullet sig-bullet--green"></span> Bizneo</span>
              <span class="sig-int-dot" title="Intratime"><span class="sig-bullet sig-bullet--green"></span> Intratime</span>
              <span class="sig-int-dot" title="Payhawk"><span class="sig-bullet sig-bullet--green"></span> Payhawk</span>
              <span class="sig-int-dot" title="A3 Innuva"><span class="sig-bullet sig-bullet--yellow"></span> A3 Innuva</span>
              <span class="sig-int-dot" title="A3 ERP"><span class="sig-bullet sig-bullet--yellow"></span> A3 ERP</span>
              <span class="sig-int-dot" title="TravelPerk"><span class="sig-bullet sig-bullet--green"></span> TravelPerk</span>
            </div>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .sig-dashboard-topbar {
      display: flex; align-items: center; gap: 12px; margin-bottom: 24px;
    }
    .sig-dash-period {
      font-size: 13px; color: var(--sig-text-muted); margin-right: auto;
    }
    .sig-spacer { flex: 1 1 auto; }
    .sig-recalc-btn {
      font-size: 12px; background: var(--mat-sys-primary) !important; color: white !important;
    }
    .sig-notif-btn { position: relative; }
    .sig-notif-badge {
      position: absolute; top: 4px; right: 4px;
      width: 16px; height: 16px; border-radius: 50%;
      background: var(--sig-danger); color: white;
      font-size: 9px; font-weight: 700;
      display: flex; align-items: center; justify-content: center;
    }

    .sig-dashboard-kpis {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 16px;
      margin-bottom: 24px;
    }
    .sig-kpi-card {
      position: relative; overflow: hidden;
      border-left: 5px solid transparent;
    }
    .sig-kpi-card--primary { border-left-color: var(--mat-sys-primary); }
    .sig-kpi-card--warning { border-left-color: var(--sig-warning); }
    .sig-kpi-card--success { border-left-color: var(--sig-success); }
    .sig-kpi-card--dark { border-left-color: var(--sig-primary-dark); }

    .sig-dashboard-row {
      display: grid;
      grid-template-columns: 1fr 1.5fr;
      gap: 16px;
      margin-bottom: 24px;
    }
    @media (max-width: 959px) {
      .sig-dashboard-row { grid-template-columns: 1fr; }
    }
    .sig-section-title {
      font-size: 20px;
      font-weight: 600;
      color: var(--sig-text-dark);
      margin: 24px 0 12px;
    }

    /* Alertas as cards */
    .sig-alerta-cards { display: flex; flex-direction: column; gap: 8px; }
    .sig-alerta-card {
      display: flex; align-items: center; gap: 12px;
      padding: 12px; border-radius: 10px;
      background: var(--mat-sys-surface-variant);
    }
    .sig-alerta-card--CierrePendiente { background: #FFF3E0; }
    .sig-alerta-card--PeriodoBloqueado { background: #FFEBEE; }
    .sig-alerta-card--ErrorSync { background: #FFEBEE; }
    .sig-alerta-card mat-icon { font-size: 22px; width: 22px; height: 22px; }
    .sig-alerta-text { display: flex; flex-direction: column; gap: 2px; }
    .sig-alerta-title { font-size: 12px; font-weight: 600; color: var(--sig-text-dark); }
    .sig-alerta-desc { font-size: 12px; color: var(--sig-text-muted); }

    .icon-warning { color: var(--sig-warning-dark); }
    .icon-error { color: var(--mat-sys-error); }
    .icon-info { color: var(--mat-sys-secondary); }
    .icon-success { color: var(--sig-success); }
    .sig-empty-msg { display: flex; align-items: center; gap: 8px; color: var(--mat-sys-on-surface-variant); font-size: 14px; }
    .sig-empty-msg mat-icon { color: var(--mat-sys-on-surface-variant); }

    /* Bar chart */
    .sig-bar-chart { display: flex; flex-direction: column; gap: 10px; padding: 8px 0; }
    .sig-bar-row, .sig-bar-target { display: flex; align-items: center; gap: 12px; }
    .sig-bar-label { font-size: 12px; width: 90px; color: var(--sig-text-dark); font-weight: 500; flex-shrink: 0; }
    .sig-bar-track { flex: 1; height: 22px; background: var(--sig-border); border-radius: 4px; position: relative; overflow: visible; }
    .sig-bar-fill { height: 100%; border-radius: 4px; min-width: 4px; transition: width 0.3s; }
    .sig-bar-value { font-size: 11px; font-weight: 600; color: var(--sig-text-dark); width: 36px; text-align: right; font-family: 'Roboto Mono', monospace; }
    .sig-bar-target-line {
      position: absolute; top: -4px; width: 2px; height: 30px;
      background: var(--sig-danger); border-radius: 1px;
    }

    /* Tabla dashboard */
    .sig-table--dashboard th.mat-header-cell {
      background: var(--mat-sys-primary) !important; color: rgba(255,255,255,0.85) !important;
      font-size: 11px; font-weight: 700; letter-spacing: 0.5px;
    }

    /* Integrations bar */
    .sig-int-card { margin-top: 16px; }
    .sig-int-card mat-card-content { padding: 12px 16px; }
    .sig-int-bar { display: flex; align-items: center; gap: 24px; }
    .sig-int-bar-label { font-size: 11px; font-weight: 700; color: var(--sig-text-muted); letter-spacing: 0.5px; white-space: nowrap; }
    .sig-int-dots { display: flex; gap: 16px; flex-wrap: wrap; }
    .sig-int-dot { display: flex; align-items: center; gap: 6px; font-size: 12px; color: var(--sig-text-muted); }
    .sig-bullet { width: 8px; height: 8px; border-radius: 50%; display: inline-block; }
    .sig-bullet--green { background: var(--sig-success); }
    .sig-bullet--yellow { background: var(--sig-warning); }
    .sig-bullet--red { background: var(--sig-danger); }
  `],
})
export class DashboardComponent implements OnInit {
  private readonly dashboardSvc = inject(DashboardService);
  private readonly periodSvc = inject(PeriodService);
  private readonly notify = inject(NotifyService);

  protected readonly kpis = signal<DashboardKpisDto | null>(null);
  protected readonly avisos = signal<DashboardAvisoDto[]>([]);
  protected readonly misProyectos = signal<MiProyectoDto[]>([]);
  protected readonly loadingKpis = signal(true);
  protected readonly loadingAvisos = signal(true);
  protected readonly loadingMis = signal(true);

  // Recarga cuando cambia el período seleccionado
  private readonly activePeriod = this.periodSvc.activeId;

  protected readonly chartSlices = computed<ChartSlice[]>(() => {
    const k = this.kpis();
    if (!k) return [];
    return [
      { label: 'Completados', value: k.cierresCompletados, color: '#1B6E3F' },
      { label: 'Pendientes', value: k.cierresPendientes, color: '#A66E0D' },
    ].filter((s) => s.value > 0);
  });

  protected readonly chartProyectos = computed(() => {
    const k = this.kpis();
    if (!k) return [];
    return [
      { nombre: 'Amex SS', margen: 32, color: '#1F4E78' },
      { nombre: 'Granini', margen: 25, color: '#2E5C8A' },
      { nombre: 'Amex New', margen: 28, color: '#70AD47' },
      { nombre: 'Proj D', margen: 21, color: '#D32F2F' },
    ];
  });

  constructor() {
    // Recarga al cambiar período
    effect(() => {
      const pid = this.activePeriod();
      this.loadAll(pid ?? undefined);
    });
  }

  ngOnInit(): void { /* lo gestiona el effect */ }

  private loadAll(periodId?: number): void {
    this.loadingKpis.set(true);
    this.loadingAvisos.set(true);
    this.loadingMis.set(true);

    this.dashboardSvc.getKpis(periodId).subscribe({
      next: (d) => { this.kpis.set(d); this.loadingKpis.set(false); },
      error: () => { this.kpis.set(null); this.loadingKpis.set(false); },
    });
    this.dashboardSvc.getAvisos().subscribe({
      next: (d) => { this.avisos.set(d); this.loadingAvisos.set(false); },
      error: () => { this.avisos.set([]); this.loadingAvisos.set(false); },
    });
    this.dashboardSvc.getMisProyectos(periodId).subscribe({
      next: (d) => { this.misProyectos.set(d); this.loadingMis.set(false); },
      error: () => { this.misProyectos.set([]); this.loadingMis.set(false); },
    });
  }

  protected avisoIcon(tipo: string): string {
    switch (tipo) {
      case 'CierrePendiente': return 'warning';
      case 'PeriodoBloqueado': return 'lock';
      case 'ErrorSync': return 'error';
      default: return 'info';
    }
  }
  protected avisoIconClass(tipo: string): string {
    switch (tipo) {
      case 'CierrePendiente': return 'icon-warning';
      case 'PeriodoBloqueado': return 'icon-warning';
      case 'ErrorSync': return 'icon-error';
      default: return 'icon-info';
    }
  }
}
