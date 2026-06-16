import { Component, inject, OnInit, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { MatMenuModule } from '@angular/material/menu';
import { interval } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DashboardService } from '../../core/api/dashboard.service';
import { PeriodService } from '../../core/api/periods.service';
import { ClosureService } from '../../core/api/closures.service';
import { AlertReadStateService } from '../../core/services/alert-read-state.service';
import { NotifyService } from '../../core/notify.service';
import { DashboardKpisDto, DashboardAvisoDto, MiServicioDto, PeriodDto, ClosureAlertaDto } from '../../models/dtos';
import { EstadoClosure, ApprovalStep } from '../../models/enums';
import { StateBadgeComponent } from '../../shared/state-badge.component';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, MatIconModule, MatButtonModule, MatTableModule, MatCardModule, MatMenuModule, StateBadgeComponent],
  template: `
    <div class="sig-exec-page">

      <!-- Header -->
      <div class="sig-exec-header">
        <div class="sig-exec-titles">
          <h1 class="sig-exec-title">Resumen Ejecutivo</h1>
          <p class="sig-exec-sub">Per&iacute;odo &middot; {{ kpis()?.periodNombre ?? 'Cargando...' }}</p>
        </div>
        <div class="sig-exec-actions">
          <button (click)="regenerate()" mat-icon-button class="sig-exec-icon-btn" title="Regenerar datos de sincronización" [disabled]="regenerating()" data-testid="btn-recalcular">
            <mat-icon>{{ regenerating() ? 'hourglass_empty' : 'refresh' }}</mat-icon>
          </button>
          <button [matMenuTriggerFor]="periodMenu" class="sig-period-chip" data-testid="period-selector" mat-button>
            <mat-icon style="font-size:16px;width:16px;height:16px;">schedule</mat-icon>
            <span>{{ kpis()?.periodNombre ?? '...' }}</span>
            <mat-icon style="font-size:14px;width:14px;height:14px;">expand_more</mat-icon>
          </button>
          <mat-menu #periodMenu="matMenu">
            @for (period of periodos(); track period.id) {
              <button mat-menu-item (click)="selectPeriod(period.id)" [class.active]="period.id === activePeriodId()">
                {{ period.nombre }}
                @if (period.id === activePeriodId()) {
                  <mat-icon style="margin-left: auto; margin-right: -8px;">check</mat-icon>
                }
              </button>
            }
          </mat-menu>
          <button mat-icon-button class="sig-exec-icon-btn sig-notif-btn" [matMenuTriggerFor]="combinedNotifMenu" aria-label="Notificaciones y Alertas" data-testid="btn-notificaciones">
            <mat-icon>notifications</mat-icon>
            @if (totalNotificaciones() > 0) {
              <span class="sig-notif-badge">{{ totalNotificaciones() }}</span>
            }
          </button>
          <mat-menu #combinedNotifMenu="matMenu" class="sig-combined-notif-menu" style="min-width: 360px;">
            <!-- Sección: Alertas de Cierre -->
            <div class="sig-notif-section">
              <div class="sig-notif-section-header">
                <mat-icon style="font-size:16px;width:16px;height:16px;color:#f59e0b;">warning_amber</mat-icon>
                <span class="sig-notif-section-title">Alertas de Cierre</span>
                @if (alertasPendientesCierre().length > 0) {
                  <span class="sig-notif-count">{{ alertasPendientesCierre().length }}</span>
                }
              </div>
              @if (loadingAlertasCierre()) {
                <div class="sig-notif-item sig-notif-loading">Cargando alertas...</div>
              } @else if (alertasPendientesCierre().length === 0) {
                <div class="sig-notif-item sig-notif-ok">
                  <mat-icon>check_circle</mat-icon>
                  <span>Sin alertas pendientes</span>
                </div>
              } @else {
                @for (a of alertasPendientesCierre().slice(0, 5); track a.id) {
                  <div class="sig-notif-item" (click)="irAServicio(a.serviceId ?? 0)" [class]="'sig-notif--' + (a.tipo === 'Bloqueante' ? 'bloqueante' : 'advertencia')">
                    <mat-icon>{{ a.tipo === 'Bloqueante' ? 'block' : 'warning' }}</mat-icon>
                    <div class="sig-notif-body">
                      <span class="sig-notif-codigo">{{ a.codigo }}</span>
                      <span class="sig-notif-desc">{{ a.descripcion }}</span>
                      @if (a.closureNombre) {
                        <span class="sig-notif-closure">{{ a.closureNombre }}</span>
                      }
                    </div>
                  </div>
                }
              }
              @if (alertasPendientesCierre().length > 0) {
                <button mat-menu-item (click)="marcarTodasLeidas()" class="sig-notif-mark-read">
                  <mat-icon>done_all</mat-icon>
                  <span>Marcar todas como leídas</span>
                </button>
              }
              <div class="sig-notif-divider"></div>
              <button mat-menu-item (click)="irAAlertas()" class="sig-notif-ver-todas">
                <mat-icon>list</mat-icon>
                <span>Ver todas las alertas</span>
              </button>
            </div>
          </mat-menu>
        </div>
      </div>

      <!-- KPI Cards -->
      <div class="sig-kpi-grid" data-testid="dashboard-kpis">

        <div class="sig-kpi" data-testid="kpi-facturacion">
          <div class="sig-kpi-body">
            <div class="sig-kpi-top">
              <span class="sig-kpi-label">FACTURACI&Oacute;N TOTAL</span>
              <div class="sig-kpi-icon sig-kpi-icon--blue"><mat-icon>attach_money</mat-icon></div>
            </div>
            <div class="sig-kpi-value">&euro; {{ facturacionK() }}K</div>
            <div class="sig-kpi-trend sig-trend--up">
              <mat-icon>trending_up</mat-icon>
              +12% vs objetivo
            </div>
          </div>
          <svg class="sig-sparkline" viewBox="0 0 120 32" preserveAspectRatio="none">
            <defs>
              <linearGradient id="spk1" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stop-color="#3b82f6" stop-opacity="0.3"/>
                <stop offset="100%" stop-color="#3b82f6" stop-opacity="0"/>
              </linearGradient>
            </defs>
            <path d="M0 28 L15 22 L30 24 L45 18 L60 20 L75 14 L90 10 L105 8 L120 4" fill="none" stroke="#3b82f6" stroke-width="2" stroke-linecap="round"/>
            <path d="M0 28 L15 22 L30 24 L45 18 L60 20 L75 14 L90 10 L105 8 L120 4 L120 32 L0 32Z" fill="url(#spk1)"/>
          </svg>
        </div>

        <div class="sig-kpi" data-testid="kpi-margen">
          <div class="sig-kpi-body">
            <div class="sig-kpi-top">
              <span class="sig-kpi-label">MARGEN PROMEDIO</span>
              <div class="sig-kpi-icon sig-kpi-icon--teal"><mat-icon>percent</mat-icon></div>
            </div>
            <div class="sig-kpi-value">{{ margenPct() }}%</div>
            <div class="sig-kpi-trend sig-trend--up">
              <mat-icon>trending_up</mat-icon>
              +3 pts vs objetivo
            </div>
          </div>
          <svg class="sig-sparkline" viewBox="0 0 120 32" preserveAspectRatio="none">
            <defs>
              <linearGradient id="spk2" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stop-color="#00d4c4" stop-opacity="0.3"/>
                <stop offset="100%" stop-color="#00d4c4" stop-opacity="0"/>
              </linearGradient>
            </defs>
            <path d="M0 26 L15 20 L30 22 L45 16 L60 18 L75 12 L90 8 L105 10 L120 6" fill="none" stroke="#00d4c4" stroke-width="2" stroke-linecap="round"/>
            <path d="M0 26 L15 20 L30 22 L45 16 L60 18 L75 12 L90 8 L105 10 L120 6 L120 32 L0 32Z" fill="url(#spk2)"/>
          </svg>
        </div>

        <div class="sig-kpi" data-testid="kpi-cierres">
          <div class="sig-kpi-body">
            <div class="sig-kpi-top">
              <span class="sig-kpi-label">CIERRES COMPLETADOS</span>
              <div class="sig-kpi-icon sig-kpi-icon--green"><mat-icon>description</mat-icon></div>
            </div>
            <div class="sig-kpi-value">{{ kpis()?.cierresCompletados ?? '...' }}</div>
            <div class="sig-kpi-trend sig-trend--up">
              <mat-icon>trending_up</mat-icon>
              +2 vs mes ant.
            </div>
          </div>
          <svg class="sig-sparkline" viewBox="0 0 120 32" preserveAspectRatio="none">
            <defs>
              <linearGradient id="spk3" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stop-color="#22c55e" stop-opacity="0.3"/>
                <stop offset="100%" stop-color="#22c55e" stop-opacity="0"/>
              </linearGradient>
            </defs>
            <path d="M0 28 L15 26 L30 22 L45 20 L60 16 L75 14 L90 12 L105 8 L120 6" fill="none" stroke="#22c55e" stroke-width="2" stroke-linecap="round"/>
            <path d="M0 28 L15 26 L30 22 L45 20 L60 16 L75 14 L90 12 L105 8 L120 6 L120 32 L0 32Z" fill="url(#spk3)"/>
          </svg>
        </div>

        <div class="sig-kpi" data-testid="kpi-pendientes">
          <div class="sig-kpi-body">
            <div class="sig-kpi-top">
              <span class="sig-kpi-label">PEND. APROBACI&Oacute;N</span>
              <div class="sig-kpi-icon sig-kpi-icon--orange"><mat-icon>schedule</mat-icon></div>
            </div>
            <div class="sig-kpi-value">{{ kpis()?.cierresPendientes ?? '...' }}</div>
            <div class="sig-kpi-trend sig-trend--warn">
              <mat-icon>warning</mat-icon>
              Requieren atenci&oacute;n
            </div>
          </div>
          <svg class="sig-sparkline" viewBox="0 0 120 32" preserveAspectRatio="none">
            <defs>
              <linearGradient id="spk4" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stop-color="#f59e0b" stop-opacity="0.3"/>
                <stop offset="100%" stop-color="#f59e0b" stop-opacity="0"/>
              </linearGradient>
            </defs>
            <path d="M0 18 L15 22 L30 16 L45 20 L60 12 L75 18 L90 10 L105 14 L120 8" fill="none" stroke="#f59e0b" stroke-width="2" stroke-linecap="round"/>
            <path d="M0 18 L15 22 L30 16 L45 20 L60 12 L75 18 L90 10 L105 14 L120 8 L120 32 L0 32Z" fill="url(#spk4)"/>
          </svg>
        </div>

      </div>

      <!-- Charts row -->
      <div class="sig-charts-row">

        <!-- Area chart: Evolucion facturacion -->
        <div class="sig-chart-card" data-testid="chart-evolucion">
          <div class="sig-chart-hdr">
            <div>
              <div class="sig-chart-icon"><mat-icon>bar_chart</mat-icon></div>
              <span class="sig-chart-title">Evoluci&oacute;n de Facturaci&oacute;n</span>
            </div>
            <div class="sig-chart-legend">
              <span class="sig-legend-dot" style="background:#3b82f6;"></span>
              <span class="sig-legend-txt">Facturaci&oacute;n (miles &euro;)</span>
            </div>
          </div>
          <svg class="sig-area-chart" viewBox="0 0 600 200" preserveAspectRatio="xMidYMid meet">
            <defs>
              <linearGradient id="areaGrad" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stop-color="#3b82f6" stop-opacity="0.25"/>
                <stop offset="100%" stop-color="#3b82f6" stop-opacity="0"/>
              </linearGradient>
            </defs>
            <!-- Grid lines -->
            <line x1="0" y1="160" x2="600" y2="160" stroke="var(--sig-border)" stroke-width="1"/>
            <line x1="0" y1="120" x2="600" y2="120" stroke="var(--sig-border)" stroke-width="1"/>
            <line x1="0" y1="80"  x2="600" y2="80"  stroke="var(--sig-border)" stroke-width="1"/>
            <line x1="0" y1="40"  x2="600" y2="40"  stroke="var(--sig-border)" stroke-width="1"/>
            <!-- Y labels -->
            <text x="8" y="164" fill="var(--sig-text-muted)" font-size="11">&mdash;</text>
            <text x="8" y="124" fill="var(--sig-text-muted)" font-size="11">&mdash;</text>
            <text x="8" y="84"  fill="var(--sig-text-muted)" font-size="11">&mdash;</text>
            <text x="8" y="44"  fill="var(--sig-text-muted)" font-size="11">&mdash;</text>
            @if (evo(); as e) {
              <!-- Area -->
              <path [attr.d]="e.area" fill="url(#areaGrad)"/>
              <!-- Line -->
              <path [attr.d]="e.line" fill="none" stroke="#3b82f6" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"/>
              <!-- End dot -->
              <circle [attr.cx]="e.end.x" [attr.cy]="e.end.y" r="4" fill="#3b82f6"/>
              <!-- X labels -->
              @for (pt of e.points; track pt.label) {
                <text [attr.x]="pt.x" y="185" fill="var(--sig-text-muted)" font-size="11" text-anchor="middle">{{ pt.label.substring(0, 3) }}</text>
              }
            }
          </svg>
        </div>

        <!-- Donut chart: Facturacion por cliente -->
        <div class="sig-chart-card" data-testid="chart-clientes">
          <div class="sig-chart-hdr">
            <div>
              <div class="sig-chart-icon"><mat-icon>people</mat-icon></div>
              <span class="sig-chart-title">Facturaci&oacute;n por Cliente</span>
            </div>
          </div>
          <div class="sig-donut-wrap">
            <svg class="sig-donut" viewBox="0 0 180 180">
              <!-- Background ring -->
              <circle cx="90" cy="90" r="68" fill="none" stroke="var(--sig-border)" stroke-width="24"/>
              <!-- Dynamic segments -->
              @for (seg of donutSegmentos(); track seg.clientId) {
                <circle cx="90" cy="90" r="68" fill="none" [attr.stroke]="seg.color" stroke-width="24"
                        [attr.stroke-dasharray]="seg.dash + ' ' + seg.gap" [attr.stroke-dashoffset]="'-' + seg.offset"
                        transform="rotate(-90 90 90)"/>
              }
              <!-- Center text -->
              <text x="90" y="84" text-anchor="middle" fill="var(--sig-text-heading)" font-size="22" font-weight="700">&euro; {{ facturacionK() }}K</text>
              <text x="90" y="102" text-anchor="middle" fill="var(--sig-text-muted)" font-size="11">Total</text>
            </svg>
            <div class="sig-donut-legend">
              @for (seg of donutSegmentos(); track seg.clientId) {
                <div class="sig-donut-item">
                  <span class="sig-donut-dot" [style.background]="seg.color"></span>
                  <span class="sig-donut-label">{{ seg.nombre }}</span>
                  <span class="sig-donut-pct">{{ seg.pctTotal | number:'1.1-1' }}%</span>
                </div>
              }
            </div>
          </div>
        </div>

      </div>

      <!-- Mis Servicios Table -->
      <div class="sig-table-section" data-testid="dashboard-mis-servicios">
        <div class="sig-section-hdr">
          <div class="sig-chart-icon"><mat-icon>assignment</mat-icon></div>
          <span class="sig-chart-title">Mis Servicios</span>
        </div>
        @if (loadingMis()) {
          <div class="sig-table-skeleton">
            @for (_ of [0,1,2]; track _) {
              <div class="sig-skeleton" style="height:16px;width:100%;border-radius:4px;margin-bottom:8px;"></div>
            }
          </div>
        } @else if (misServicios().length === 0) {
          <div class="sig-empty-table">No hay servicios asignados en el per&iacute;odo activo.</div>
        } @else {
          <table mat-table [dataSource]="misServicios()" class="sig-mat-table">
            <ng-container matColumnDef="nombre">
              <th mat-header-cell *matHeaderCellDef> Servicio </th>
              <td mat-cell *matCellDef="let p">
                <span class="sig-cell-link" [routerLink]="['/services', p.serviceId]">{{ p.nombre }}</span>
              </td>
            </ng-container>
            <ng-container matColumnDef="cliente">
              <th mat-header-cell *matHeaderCellDef> Cliente </th>
              <td mat-cell *matCellDef="let p">{{ p.clientNombre }}</td>
            </ng-container>
            <ng-container matColumnDef="costeBruto">
              <th mat-header-cell *matHeaderCellDef> Coste Bruto </th>
              <td mat-cell *matCellDef="let p" class="sig-cell-mono">{{ p.costeTotal !== null && p.costeTotal !== undefined ? (p.costeTotal | number:'1.0-0') + ' €' : '—' }}</td>
            </ng-container>
            <ng-container matColumnDef="facturacion">
              <th mat-header-cell *matHeaderCellDef> Facturaci&oacute;n </th>
              <td mat-cell *matCellDef="let p" class="sig-cell-mono">{{ p.facturacionTotal !== null && p.facturacionTotal !== undefined ? (p.facturacionTotal | number:'1.0-0') + ' €' : '—' }}</td>
            </ng-container>
            <ng-container matColumnDef="margen">
              <th mat-header-cell *matHeaderCellDef> Margen </th>
              <td mat-cell *matCellDef="let p" class="sig-cell-mono">{{ p.margen !== null && p.margen !== undefined ? (p.margen | number:'1.0-0') + ' €' : '—' }}</td>
            </ng-container>
            <ng-container matColumnDef="estado">
              <th mat-header-cell *matHeaderCellDef> Estado </th>
              <td mat-cell *matCellDef="let p">
                @if (p.estado && p.pasoActual) {
                  <sig-state-badge [estado]="p.estado" [paso]="p.pasoActual" />
                } @else {
                  <span class="sig-text-muted">Sin cierre</span>
                }
              </td>
            </ng-container>
            <ng-container matColumnDef="accion">
              <th mat-header-cell *matHeaderCellDef> Acci&oacute;n </th>
              <td mat-cell *matCellDef="let p">
                @if (p.closureId) {
                  <a mat-icon-button [routerLink]="['/closures', p.closureId]" class="sig-row-action" aria-label="Ir al cierre">
                    <mat-icon>launch</mat-icon>
                  </a>
                } @else {
                  <span class="sig-text-muted">&mdash;</span>
                }
              </td>
            </ng-container>
            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;" class="sig-data-row"></tr>
          </table>
        }
      </div>

      <!-- Bottom row: Gauge + Objectives + Alerts -->
      <div class="sig-bottom-row">

        <!-- Gauge: Margen vs Objetivo -->
        <div class="sig-bottom-card" data-testid="panel-margen">
          <div class="sig-chart-icon"><mat-icon>donut_large</mat-icon></div>
          <span class="sig-chart-title">Margen vs Objetivo</span>
          <svg class="sig-gauge" viewBox="0 0 200 130">
            <!-- Background arc -->
            <path d="M20 110 A80 80 0 0 1 180 110" fill="none" stroke="var(--sig-border)" stroke-width="18" stroke-linecap="round"/>
            <!-- Gauge fill (dynamic) -->
            <path [attr.d]="gaugePath()" fill="none" stroke="#3b82f6" stroke-width="18" stroke-linecap="round"/>
            <!-- Target marker at 25% -->
            <line x1="51" y1="61" x2="35" y2="45" stroke="#ef4444" stroke-width="2.5" stroke-linecap="round"/>
            <!-- Center text -->
            <text x="100" y="98" text-anchor="middle" fill="var(--sig-text-heading)" font-size="28" font-weight="700">{{ margenPct() | number:'1.1-1' }}%</text>
            <text x="100" y="116" text-anchor="middle" fill="var(--sig-text-muted)" font-size="12">Objetivo 25%</text>
          </svg>
        </div>

        <!-- Objectives -->
        <div class="sig-bottom-card" data-testid="panel-objetivos">
          <div class="sig-chart-icon"><mat-icon>track_changes</mat-icon></div>
          <span class="sig-chart-title">Objetivos del Per&iacute;odo</span>
          <div class="sig-obj-list">
            <div class="sig-obj-item">
              <div class="sig-obj-icon sig-obj-icon--blue"><mat-icon>attach_money</mat-icon></div>
              <div class="sig-obj-body">
                @if (kpis()) {
                  <div class="sig-obj-vals"><span class="sig-obj-current">&euro; {{ facturacionK() }}K / 400K</span></div>
                  <div class="sig-obj-track"><div class="sig-obj-fill" [style.width]="Math.min(100, (kpis()!.facturacionTotal / 400000) * 100) + '%'" style="background:#3b82f6;"></div></div>
                }
                <div class="sig-obj-sub">Facturaci&oacute;n objetivo</div>
              </div>
            </div>
            <div class="sig-obj-item">
              <div class="sig-obj-icon sig-obj-icon--teal"><mat-icon>task_alt</mat-icon></div>
              <div class="sig-obj-body">
                @if (kpis()) {
                  <div class="sig-obj-vals"><span class="sig-obj-current">{{ kpis()!.cierresCompletados }} / {{ kpis()!.cierresCompletados + kpis()!.cierresPendientes }}</span></div>
                  <div class="sig-obj-track"><div class="sig-obj-fill" [style.width]="(kpis()!.cierresCompletados / (kpis()!.cierresCompletados + kpis()!.cierresPendientes || 1)) * 100 + '%'" style="background:#00d4c4;"></div></div>
                }
                <div class="sig-obj-sub">Cierres completados</div>
              </div>
            </div>
            <div class="sig-obj-item">
              <div class="sig-obj-icon sig-obj-icon--green"><mat-icon>percent</mat-icon></div>
              <div class="sig-obj-body">
                <div class="sig-obj-vals"><span class="sig-obj-current">{{ margenPct() | number:'1.1-1' }}% / 25%</span></div>
                <div class="sig-obj-track"><div class="sig-obj-fill" [style.width]="Math.min(100, margenPct()) + '%'" style="background:#22c55e;"></div></div>
                <div class="sig-obj-sub">Margen objetivo</div>
              </div>
            </div>
          </div>
        </div>

        <!-- Alertas de Cierre -->
        <div class="sig-bottom-card" data-testid="dashboard-alertas-cierre">
          <div class="sig-chart-icon sig-chart-icon--warn"><mat-icon>warning_amber</mat-icon></div>
          <span class="sig-chart-title">Alertas de Cierre</span>
          @if (loadingAlertasCierre()) {
            <div class="sig-alert-item sig-alert--loading" *ngFor="let i of [0,1,2]">
              <div class="sig-skeleton" style="height:14px;width:80%;border-radius:4px;"></div>
            </div>
          } @else if (alertasCierreNoConfirmadas().length === 0) {
            <div class="sig-alert-item">
              <mat-icon style="color:var(--sig-success);font-size:18px;width:18px;height:18px;">check_circle</mat-icon>
              <span style="color:var(--sig-text-muted);font-size:13px;">Sin alertas pendientes</span>
            </div>
          } @else {
            @for (a of alertasCierreNoConfirmadas().slice(0, 5); track a.id) {
              <div class="sig-alert-item" (click)="irAServicio(a.serviceId ?? 0)" style="cursor:pointer;" [class]="'sig-alert--' + (a.tipo === 'Bloqueante' ? 'warn' : 'info') + (alertReadSvc.isRead(a.id) ? ' sig-alert--read' : '')">
                <mat-icon class="sig-alert-icon" aria-hidden="true">{{ a.tipo === 'Bloqueante' ? 'block' : 'warning' }}</mat-icon>
                <div class="sig-alert-body">
                  <span class="sig-alert-text">{{ a.descripcion }}</span>
                  <span class="sig-alert-closure">{{ a.closureNombre }}</span>
                </div>
              </div>
            }
            @if (alertasCierreNoConfirmadas().length > 5) {
              <a (click)="irAAlertas()" class="sig-alert-ver-todas" style="cursor:pointer;">
                Ver todas ({{ alertasCierreNoConfirmadas().length }})
                <mat-icon style="font-size:14px;width:14px;height:14px;">arrow_forward</mat-icon>
              </a>
            }
          }
        </div>

      </div>

    </div>
`,
  styles: [`
    :host { display: block; }

    .sig-exec-page {
      padding: 28px 28px 40px;
      background: var(--sig-bg-app);
      min-height: 100vh;
    }

    /* Header */
    .sig-exec-header {
      display: flex; align-items: flex-start; justify-content: space-between;
      margin-bottom: 28px;
    }
    .sig-exec-title {
      font-size: 24px; font-weight: 700;
      color: var(--sig-text-heading); margin: 0 0 4px;
    }
    .sig-exec-sub {
      font-size: 13px; color: var(--sig-text-muted); margin: 0;
    }
    .sig-exec-actions {
      display: flex; align-items: center; gap: 10px;
    }
    .sig-demo-badge {
      font-size: 10px; font-weight: 700; letter-spacing: 1px;
      background: rgba(245,158,11,.12); color: #f59e0b;
      border: 1px solid rgba(245,158,11,.25);
      padding: 4px 10px; border-radius: 4px;
    }
    .sig-demo-badge--toggle {
      cursor: pointer; transition: all 150ms;
      &:hover { background: rgba(245,158,11,.18); border-color: rgba(245,158,11,.4); }
    }
    .sig-period-chip {
      display: flex; align-items: center; gap: 6px;
      background: var(--sig-bg-card); border: 1px solid var(--sig-border);
      border-radius: 8px; padding: 6px 12px;
      font-size: 13px; color: var(--sig-text-primary);
      cursor: pointer;
    }
    .sig-period-chip mat-icon { color: var(--sig-text-muted); }
    .sig-exec-icon-btn {
      color: var(--sig-text-secondary) !important;
      background: var(--sig-bg-card) !important;
      border: 1px solid var(--sig-border) !important;
      border-radius: 8px !important;
      &:hover { background: var(--sig-bg-hover) !important; }
    }
    .sig-notif-btn { position: relative; }
    .sig-notif-badge {
      position: absolute; top: 4px; right: 4px;
      width: 16px; height: 16px; border-radius: 50%;
      background: #ef4444; color: white;
      font-size: 9px; font-weight: 700;
      display: flex; align-items: center; justify-content: center;
      pointer-events: none;
    }

    /* KPI Grid */
    .sig-kpi-grid {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 16px;
      margin-bottom: 20px;
    }
    @media (max-width: 1200px) { .sig-kpi-grid { grid-template-columns: repeat(2,1fr); } }
    @media (max-width: 640px)  { .sig-kpi-grid { grid-template-columns: 1fr; } }

    .sig-kpi {
      background: var(--sig-bg-card);
      border: 1px solid var(--sig-border);
      border-radius: 12px;
      overflow: hidden;
      display: flex; flex-direction: column;
    }
    .sig-kpi-body {
      padding: 18px 18px 12px;
      flex: 1;
    }
    .sig-kpi-top {
      display: flex; align-items: flex-start; justify-content: space-between;
      margin-bottom: 10px;
    }
    .sig-kpi-label {
      font-size: 10px; font-weight: 700; letter-spacing: .08em;
      color: var(--sig-text-muted); text-transform: uppercase;
    }
    .sig-kpi-icon {
      width: 32px; height: 32px; border-radius: 8px;
      display: flex; align-items: center; justify-content: center;
      flex-shrink: 0;
      mat-icon { font-size: 18px !important; width: 18px !important; height: 18px !important; color: white !important; }
    }
    .sig-kpi-icon--blue   { background: rgba(59,130,246,.2); mat-icon { color: #3b82f6 !important; } }
    .sig-kpi-icon--teal   { background: rgba(0,212,196,.15); mat-icon { color: #00d4c4 !important; } }
    .sig-kpi-icon--green  { background: rgba(34,197,94,.15); mat-icon { color: #22c55e !important; } }
    .sig-kpi-icon--orange { background: rgba(245,158,11,.15); mat-icon { color: #f59e0b !important; } }
    .sig-kpi-value {
      font-size: 30px; font-weight: 700;
      color: var(--sig-text-heading);
      font-family: 'Roboto Mono', monospace;
      line-height: 1; margin-bottom: 8px;
    }
    .sig-kpi-trend {
      display: flex; align-items: center; gap: 4px;
      font-size: 12px;
      mat-icon { font-size: 14px !important; width: 14px !important; height: 14px !important; }
    }
    .sig-trend--up   { color: #22c55e; }
    .sig-trend--warn { color: #f59e0b; }
    .sig-sparkline {
      width: 100%; height: 48px; display: block;
    }

    /* Charts row */
    .sig-charts-row {
      display: grid;
      grid-template-columns: 1.4fr 1fr;
      gap: 16px;
      margin-bottom: 20px;
    }
    @media (max-width: 900px) { .sig-charts-row { grid-template-columns: 1fr; } }

    .sig-chart-card {
      background: var(--sig-bg-card);
      border: 1px solid var(--sig-border);
      border-radius: 12px;
      padding: 18px;
    }
    .sig-chart-hdr {
      display: flex; align-items: center; justify-content: space-between;
      margin-bottom: 16px;
      > div { display: flex; align-items: center; gap: 8px; }
    }
    .sig-chart-icon {
      width: 28px; height: 28px; border-radius: 6px;
      background: rgba(59,130,246,.15);
      display: flex; align-items: center; justify-content: center;
      mat-icon { font-size: 16px !important; width: 16px !important; height: 16px !important; color: #3b82f6 !important; }
    }
    .sig-chart-icon--warn {
      background: rgba(245,158,11,.15);
      mat-icon { color: #f59e0b !important; }
    }
    .sig-chart-title {
      font-size: 14px; font-weight: 600; color: var(--sig-text-heading);
    }
    .sig-chart-legend { display: flex; align-items: center; gap: 6px; }
    .sig-legend-dot { width: 8px; height: 8px; border-radius: 50%; display: inline-block; }
    .sig-legend-txt { font-size: 11px; color: var(--sig-text-muted); }

    .sig-area-chart { width: 100%; height: 200px; }

    /* Donut */
    .sig-donut-wrap {
      display: flex; align-items: center; gap: 20px;
      justify-content: center; padding: 8px 0;
    }
    .sig-donut { width: 180px; height: 180px; flex-shrink: 0; }
    .sig-donut-legend { display: flex; flex-direction: column; gap: 12px; }
    .sig-donut-item { display: flex; align-items: center; gap: 8px; }
    .sig-donut-dot { width: 10px; height: 10px; border-radius: 50%; flex-shrink: 0; }
    .sig-donut-label { font-size: 13px; color: var(--sig-text-primary); flex: 1; }
    .sig-donut-pct { font-size: 13px; font-weight: 700; color: var(--sig-text-heading); font-family: 'Roboto Mono', monospace; }

    /* Bottom row */
    .sig-bottom-row {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      gap: 16px;
    }
    @media (max-width: 900px) { .sig-bottom-row { grid-template-columns: 1fr; } }

    .sig-bottom-card {
      background: var(--sig-bg-card);
      border: 1px solid var(--sig-border);
      border-radius: 12px;
      padding: 18px;
      display: flex; flex-direction: column; gap: 6px;
      > .sig-chart-icon { margin-bottom: 0; }
    }

    /* Gauge */
    .sig-gauge { width: 100%; height: 130px; margin: 8px 0; }

    /* Objectives */
    .sig-obj-list { display: flex; flex-direction: column; gap: 14px; margin-top: 8px; }
    .sig-obj-item { display: flex; gap: 12px; align-items: flex-start; }
    .sig-obj-icon {
      width: 32px; height: 32px; border-radius: 8px;
      display: flex; align-items: center; justify-content: center; flex-shrink: 0;
      mat-icon { font-size: 16px !important; width: 16px !important; height: 16px !important; }
    }
    .sig-obj-icon--blue  { background: rgba(59,130,246,.15); mat-icon { color: #3b82f6 !important; } }
    .sig-obj-icon--teal  { background: rgba(0,212,196,.15);  mat-icon { color: #00d4c4 !important; } }
    .sig-obj-icon--green { background: rgba(34,197,94,.15);  mat-icon { color: #22c55e !important; } }
    .sig-obj-body { flex: 1; }
    .sig-obj-vals { display: flex; justify-content: space-between; margin-bottom: 5px; }
    .sig-obj-current { font-size: 13px; font-weight: 600; color: var(--sig-text-heading); font-family: 'Roboto Mono', monospace; }
    .sig-obj-track {
      height: 6px; background: var(--sig-border); border-radius: 3px; overflow: hidden;
    }
    .sig-obj-fill { height: 100%; border-radius: 3px; transition: width .3s; }
    .sig-obj-sub { font-size: 11px; color: var(--sig-text-muted); margin-top: 4px; }

    /* Alerts */
    .sig-alert-item {
      display: flex; align-items: flex-start; gap: 10px;
      background: var(--sig-bg-card-alt);
      border: 1px solid var(--sig-border);
      border-radius: 8px;
      padding: 10px 12px;
      margin-top: 6px;
      &:hover { background: var(--sig-bg-hover); }
    }
    .sig-alert--warn {
      background: rgba(239,68,68,.07);
      border-color: rgba(239,68,68,.2);
    }
    .sig-alert--info {
      background: rgba(245,158,11,.07);
      border-color: rgba(245,158,11,.2);
    }
    .sig-alert--success {
      background: rgba(34,197,94,.07);
      border-color: rgba(34,197,94,.2);
    }
    .sig-alert--read {
      opacity: 0.6;
      background: var(--sig-bg-card-alt) !important;
      border-color: var(--sig-border) !important;
    }
    .sig-alert-icon {
      font-size: 16px !important; width: 16px !important; height: 16px !important;
      color: #f59e0b !important; flex-shrink: 0; margin-top: 1px;
    }
    .sig-alert-text { font-size: 12px; color: var(--sig-text-primary); line-height: 1.4; }
    .sig-alert-body { display: flex; flex-direction: column; gap: 2px; }
    .sig-alert-closure { font-size: 11px; color: var(--sig-text-muted); }
    .sig-alert-ver-todas {
      display: flex; align-items: center; gap: 4px;
      font-size: 12px; color: #3b82f6; text-decoration: none;
      padding: 8px 0; margin-top: 4px;
      &:hover { text-decoration: underline; }
    }

    /* Combined notification menu */
    .sig-combined-notif-menu { max-height: 80vh; }
    .sig-notif-section { padding: 8px 0; }
    .sig-notif-section-header {
      display: flex; align-items: center; gap: 6px;
      padding: 4px 12px; font-size: 12px; font-weight: 600;
      color: var(--sig-text-heading);
    }
    .sig-notif-count {
      margin-left: auto;
      font-size: 10px; font-weight: 700;
      background: rgba(59,130,246,.12); color: #3b82f6;
      padding: 2px 6px; border-radius: 10px;
    }
    .sig-notif-item {
      display: flex; align-items: flex-start; gap: 8px;
      padding: 8px 12px; font-size: 12px; cursor: pointer;
      &:hover { background: var(--sig-bg-hover); }
      mat-icon { font-size: 16px !important; width: 16px !important; height: 16px !important; flex-shrink: 0; margin-top: 2px; }
    }
    .sig-notif-body { display: flex; flex-direction: column; gap: 2px; flex: 1; }
    .sig-notif-codigo { font-weight: 600; font-size: 11px; }
    .sig-notif-desc { font-size: 11px; color: var(--sig-text-muted); line-height: 1.3; }
    .sig-notif-closure { font-size: 10px; color: var(--sig-text-muted); font-style: italic; }
    .sig-notif--bloqueante { background: rgba(239,68,68,.05); }
    .sig-notif--bloqueante mat-icon { color: #ef4444 !important; }
    .sig-notif--advertencia { background: rgba(245,158,11,.05); }
    .sig-notif--advertencia mat-icon { color: #f59e0b !important; }
    .sig-notif-loading { color: var(--sig-text-muted); font-style: italic; }
    .sig-notif-ok { color: var(--sig-success); }
    .sig-notif-ok mat-icon { color: #22c55e !important; }
    .sig-notif-divider { height: 1px; background: var(--sig-border); margin: 4px 12px; }
    .sig-notif-ver-todas { color: #3b82f6; font-weight: 500; }

    /* Mis Proyectos Table */
    .sig-table-section {
      background: var(--sig-bg-card);
      border: 1px solid var(--sig-border);
      border-radius: 12px;
      padding: 18px;
      margin-bottom: 20px;
    }
    .sig-section-hdr {
      display: flex; align-items: center; gap: 8px;
      margin-bottom: 16px;
    }
    .sig-mat-table {
      width: 100%;
      background: transparent;
      border-collapse: collapse;
      th.mat-mdc-header-cell {
        color: var(--sig-text-muted);
        font-size: 11px;
        font-weight: 700;
        letter-spacing: .05em;
        text-transform: uppercase;
        border-bottom-color: var(--sig-border);
        padding: 8px 12px;
      }
      td.mat-mdc-cell {
        color: var(--sig-text-primary);
        font-size: 13px;
        border-bottom-color: var(--sig-border);
        padding: 10px 12px;
      }
    }
    .sig-data-row {
      cursor: pointer;
      transition: background 150ms;
      &:hover { background: var(--sig-bg-hover); }
    }
    .sig-cell-link {
      color: #3b82f6;
      text-decoration: none;
      font-weight: 500;
      &:hover { text-decoration: underline; }
    }
    .sig-cell-mono {
      font-family: 'Roboto Mono', monospace;
      font-size: 12px;
      color: var(--sig-text-muted);
    }
    .sig-empty-table {
      padding: 24px 12px;
      text-align: center;
      color: var(--sig-text-muted);
      font-size: 13px;
    }
    .sig-table-skeleton {
      padding: 12px;
    }
    .sig-row-action {
      color: var(--sig-text-secondary) !important;
    }
    .sig-skeleton {
      background: var(--sig-border);
      animation: sig-shimmer 1.4s infinite;
    }
    @keyframes sig-shimmer {
      0% { opacity: .4; }
      50% { opacity: .8; }
      100% { opacity: .4; }
    }
    .sig-text-muted {
      color: var(--sig-text-muted);
      font-size: 12px;
    }
`],
})
export class DashboardComponent implements OnInit {
  private readonly dashboardSvc = inject(DashboardService);
  private readonly closureSvc   = inject(ClosureService);
  private readonly periodSvc    = inject(PeriodService);
  protected readonly alertReadSvc = inject(AlertReadStateService);
  private readonly notify       = inject(NotifyService);
  private readonly router       = inject(Router);

  protected readonly kpis          = signal<DashboardKpisDto | null>(null);
  protected readonly avisos        = signal<DashboardAvisoDto[]>([]);
  protected readonly misServicios  = signal<MiServicioDto[]>([]);
  protected readonly alertasCierre = signal<ClosureAlertaDto[]>([]);
  protected readonly loadingKpis   = signal(true);
  protected readonly loadingAvisos = signal(true);
  protected readonly loadingMis    = signal(true);
  protected readonly loadingAlertasCierre = signal(true);
  protected readonly regenerating  = signal(false);
  protected readonly periodos      = signal<PeriodDto[]>([]);
  protected readonly activePeriodId = computed(() => this.periodSvc.activeId());

  protected readonly displayedColumns = ['nombre', 'cliente', 'costeBruto', 'facturacion', 'margen', 'estado', 'accion'];

  protected readonly Math = Math; // expose Math to template

  protected readonly alertasPendientesCierre = computed(() =>
    this.alertasCierre().filter(a => !a.confirmada && !this.alertReadSvc.isRead(a.id))
  );

  protected readonly alertasCierreNoConfirmadas = computed(() =>
    this.alertasCierre().filter(a => !a.confirmada)
  );

  protected readonly totalNotificaciones = computed(() =>
    this.alertasPendientesCierre().length + this.avisos().length
  );

  protected readonly alertasCierreResumen = computed(() => {
    const alertas = this.alertasCierre();
    return {
      total: alertas.length,
      bloqueantes: alertas.filter(a => a.tipo === 'Bloqueante' && !a.confirmada).length,
      advertencias: alertas.filter(a => a.tipo === 'Advertencia' && !a.confirmada).length,
    };
  });

  private readonly activePeriod = this.periodSvc.activeId;

  protected readonly facturacionK = computed(() => {
    const k = this.kpis();
    if (!k) return '...';
    return k.facturacionTotal > 0 ? Math.round(k.facturacionTotal / 1000).toString() : '0';
  });

  protected readonly margenPct = computed(() => {
    const k = this.kpis();
    return k?.margenPct ?? 0;
  });

  protected readonly donutSegmentos = computed(() => {
    const clientes = this.kpis()?.desglosePorCliente ?? [];
    const C = 2 * Math.PI * 68; // circunferencia para radio 68 (debe coincidir con el <circle r="68"> del SVG)
    const COLORS = ['#3b82f6','#00d4c4','#f59e0b','#ef4444','#8b5cf6','#06b6d4'];
    let offset = 0;
    return clientes.map((c, i) => {
      const dash = (c.pctTotal / 100) * C;
      const seg = { ...c, dash, gap: C - dash, offset, color: COLORS[i % COLORS.length] };
      offset += dash;
      return seg;
    });
  });

  protected readonly gaugePath = computed(() => {
    const pct = Math.min(this.margenPct(), 100) / 100;
    if (pct <= 0) return '';
    // Geometría fiel al arco de fondo del SVG: centro (100,110), radio 80, semicírculo superior
    const a = pct * Math.PI;
    const x = 100 - 80 * Math.cos(a);
    const y = 110 - 80 * Math.sin(a);
    return `M 20 110 A 80 80 0 ${pct > 0.5 ? 1 : 0} 1 ${x.toFixed(1)} ${y.toFixed(1)}`;
  });

  protected readonly evo = computed(() => {
    const pts = this.kpis()?.evolucion ?? [];
    if (pts.length < 2) return null;
    const maxF = Math.max(...pts.map(p => p.facturacion), 1);
    // Coordenadas absolutas en el viewBox "0 0 600 200": zona útil x[50..510], y[40..160]
    const X0 = 50, W = 460, YTOP = 40, YBOT = 160;
    const xy = pts.map((p, i) => ({
      x: X0 + (i / (pts.length - 1)) * W,
      y: YBOT - (p.facturacion / maxF) * (YBOT - YTOP),
      label: p.periodNombre,
    }));
    const line = xy.map((pt, i) => `${i === 0 ? 'M' : 'L'} ${pt.x.toFixed(1)} ${pt.y.toFixed(1)}`).join(' ');
    const first = xy[0], last = xy[xy.length - 1];
    const area = `${line} L ${last.x.toFixed(1)} ${YBOT} L ${first.x.toFixed(1)} ${YBOT} Z`;
    return { line, area, points: xy, end: last };
  });

  constructor() {
    effect(() => {
      const pid = this.activePeriod();
      this.loadAll(pid ?? undefined);
    });

    interval(30_000).pipe(takeUntilDestroyed()).subscribe(() => {
      this.closureSvc.getAllAlertas().subscribe({
        next: d => this.alertasCierre.set(d as ClosureAlertaDto[])
      });
    });
  }

  ngOnInit(): void {
    // Cargar lista de períodos disponibles
    this.periodSvc.list().subscribe({
      next: (periods) => this.periodos.set(periods),
      error: () => this.periodos.set([]),
    });
  }

  private loadAll(periodId?: number): void {
    this.loadingKpis.set(true);
    this.loadingAvisos.set(true);
    this.loadingMis.set(true);
    this.loadingAlertasCierre.set(true);
    this.dashboardSvc.getKpis(periodId).subscribe({
      next:  (d) => { this.kpis.set(d);  this.loadingKpis.set(false); },
      error: ()  => { this.kpis.set(null); this.loadingKpis.set(false); },
    });
    this.dashboardSvc.getAvisos().subscribe({
      next:  (d) => { this.avisos.set(d);   this.loadingAvisos.set(false); },
      error: ()  => { this.avisos.set([]);   this.loadingAvisos.set(false); },
    });
    this.dashboardSvc.getMisServicios(periodId).subscribe({
      next:  (d) => { this.misServicios.set(d);   this.loadingMis.set(false); },
      error: ()  => { this.misServicios.set([]);   this.loadingMis.set(false); },
    });
    this.closureSvc.getAllAlertas().subscribe({
      next: (d) => { this.alertasCierre.set(d as ClosureAlertaDto[]); this.loadingAlertasCierre.set(false); },
      error: () => { this.alertasCierre.set([]); this.loadingAlertasCierre.set(false); },
    });
  }

  protected irAServicio(serviceId: number): void {
    if (serviceId > 0) {
      this.router.navigate(['/services', serviceId]);
    }
  }

  protected irAAlertas(): void {
    this.router.navigate(['/alertas']);
  }

  protected avisoIcon(tipo: string): string {
    switch (tipo) {
      case 'CierrePendiente':     return 'warning';
      case 'CierreRechazado':     return 'cancel';
      case 'PeriodoBloqueado':    return 'cancel';
      case 'PeriodoProximoVencer': return 'hourglass_empty';
      case 'IncidenciaCalculo':   return 'warning_amber';
      case 'ErrorSync':           return 'error';
      default:                    return 'info';
    }
  }

  protected regenerate(): void {
    if (this.regenerating()) return;
    this.regenerating.set(true);
    this.dashboardSvc.regenerateSeed().subscribe({
      next: () => {
        this.notify.success('Datos regenerados exitosamente');
        this.loadAll(this.activePeriod() ?? undefined);
        this.regenerating.set(false);
      },
      error: (err) => {
        this.notify.error('Error al regenerar datos: ' + (err?.error?.title || err?.message || 'Error desconocido'));
        this.regenerating.set(false);
      }
    });
  }

  protected selectPeriod(periodId: number): void {
    this.periodSvc.setActive(periodId);
    // El effect en el constructor reaccionará automáticamente al cambio de activePeriod
  }

  protected marcarTodasLeidas() {
    this.alertReadSvc.markAllAsRead(this.alertasPendientesCierre().map(a => a.id));
  }
}
