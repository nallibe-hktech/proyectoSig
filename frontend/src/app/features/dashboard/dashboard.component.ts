import { Component, inject, OnInit, signal, computed, effect } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { DashboardService } from '../../core/api/dashboard.service';
import { PeriodService } from '../../core/api/periods.service';
import { NotifyService } from '../../core/notify.service';
import { DashboardKpisDto, DashboardAvisoDto, MiProyectoDto } from '../../models/dtos';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, DecimalPipe, MatIconModule, MatButtonModule],
  template: `
    <div class="sig-exec-page">

      <!-- Header -->
      <div class="sig-exec-header">
        <div class="sig-exec-titles">
          <h1 class="sig-exec-title">Resumen Ejecutivo</h1>
          <p class="sig-exec-sub">Per&iacute;odo &middot; {{ kpis()?.periodNombre ?? 'Cargando...' }}</p>
        </div>
        <div class="sig-exec-actions">
          @if (showDemo) {
            <span class="sig-demo-badge">ENTORNO DEMO</span>
          }
          <div class="sig-period-chip">
            <mat-icon style="font-size:16px;width:16px;height:16px;">schedule</mat-icon>
            <span>{{ kpis()?.periodNombre ?? '...' }}</span>
            <mat-icon style="font-size:14px;width:14px;height:14px;">expand_more</mat-icon>
          </div>
          <button mat-icon-button class="sig-exec-icon-btn" aria-label="Actualizar datos">
            <mat-icon>refresh</mat-icon>
          </button>
          <button mat-icon-button class="sig-exec-icon-btn sig-notif-btn" aria-label="Notificaciones" data-testid="btn-notificaciones">
            <mat-icon>notifications</mat-icon>
            <span class="sig-notif-badge">3</span>
          </button>
        </div>
      </div>

      <!-- KPI Cards -->
      <div class="sig-kpi-grid">

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
            <text x="8" y="164" fill="var(--sig-text-muted)" font-size="11">270K</text>
            <text x="8" y="124" fill="var(--sig-text-muted)" font-size="11">370K</text>
            <text x="8" y="84"  fill="var(--sig-text-muted)" font-size="11">420K</text>
            <text x="8" y="44"  fill="var(--sig-text-muted)" font-size="11">470K</text>
            <!-- Area -->
            <path d="M50 155 L116 148 L182 142 L248 130 L314 120 L380 105 L446 88 L512 70 L578 50 L578 170 L50 170Z" fill="url(#areaGrad)"/>
            <!-- Line -->
            <path d="M50 155 L116 148 L182 142 L248 130 L314 120 L380 105 L446 88 L512 70 L578 50" fill="none" stroke="#3b82f6" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"/>
            <!-- End dot -->
            <circle cx="578" cy="50" r="4" fill="#3b82f6"/>
            <!-- X labels -->
            <text x="44"  y="185" fill="var(--sig-text-muted)" font-size="11" text-anchor="middle">Jun</text>
            <text x="116" y="185" fill="var(--sig-text-muted)" font-size="11" text-anchor="middle">Jul</text>
            <text x="182" y="185" fill="var(--sig-text-muted)" font-size="11" text-anchor="middle">Ago</text>
            <text x="248" y="185" fill="var(--sig-text-muted)" font-size="11" text-anchor="middle">Sep</text>
            <text x="314" y="185" fill="var(--sig-text-muted)" font-size="11" text-anchor="middle">Oct</text>
            <text x="380" y="185" fill="var(--sig-text-muted)" font-size="11" text-anchor="middle">Nov</text>
            <text x="446" y="185" fill="var(--sig-text-muted)" font-size="11" text-anchor="middle">Dic</text>
            <text x="512" y="185" fill="var(--sig-text-muted)" font-size="11" text-anchor="middle">Ene</text>
            <text x="578" y="185" fill="var(--sig-text-muted)" font-size="11" text-anchor="middle">May</text>
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
              <!-- American Express 58% = 208.8deg -->
              <circle cx="90" cy="90" r="68" fill="none" stroke="#3b82f6" stroke-width="24"
                      stroke-dasharray="247 178" stroke-dashoffset="0"
                      transform="rotate(-90 90 90)"/>
              <!-- Granini 27% = 97.2deg -->
              <circle cx="90" cy="90" r="68" fill="none" stroke="#00d4c4" stroke-width="24"
                      stroke-dasharray="115 310" stroke-dashoffset="-247"
                      transform="rotate(-90 90 90)"/>
              <!-- Otros 15% = 54deg -->
              <circle cx="90" cy="90" r="68" fill="none" stroke="#475569" stroke-width="24"
                      stroke-dasharray="64 361" stroke-dashoffset="-362"
                      transform="rotate(-90 90 90)"/>
              <!-- Center text -->
              <text x="90" y="84" text-anchor="middle" fill="var(--sig-text-heading)" font-size="22" font-weight="700">&euro; 450K</text>
              <text x="90" y="102" text-anchor="middle" fill="var(--sig-text-muted)" font-size="11">Total</text>
            </svg>
            <div class="sig-donut-legend">
              <div class="sig-donut-item">
                <span class="sig-donut-dot" style="background:#3b82f6;"></span>
                <span class="sig-donut-label">American Express</span>
                <span class="sig-donut-pct">58%</span>
              </div>
              <div class="sig-donut-item">
                <span class="sig-donut-dot" style="background:#00d4c4;"></span>
                <span class="sig-donut-label">Granini</span>
                <span class="sig-donut-pct">27%</span>
              </div>
              <div class="sig-donut-item">
                <span class="sig-donut-dot" style="background:#475569;"></span>
                <span class="sig-donut-label">Otros clientes</span>
                <span class="sig-donut-pct">15%</span>
              </div>
            </div>
          </div>
        </div>

      </div>

      <!-- Bottom row: Gauge + Objectives + Alerts -->
      <div class="sig-bottom-row">

        <!-- Gauge: Margen vs Objetivo -->
        <div class="sig-bottom-card" data-testid="panel-margen">
          <div class="sig-chart-icon"><mat-icon>donut_large</mat-icon></div>
          <span class="sig-chart-title">Margen vs Objetivo</span>
          <svg class="sig-gauge" viewBox="0 0 200 130">
            <path d="M20 110 A80 80 0 0 1 180 110" fill="none" stroke="rgba(255,255,255,.08)" stroke-width="18" stroke-linecap="round"/>
            <!-- 28%/100% of 180deg = 50.4deg arc -->
            <path d="M20 110 A80 80 0 0 1 180 110" fill="none" stroke="rgba(255,255,255,.04)" stroke-width="18" stroke-linecap="round"/>
            <!-- Gauge fill 28% -->
            <path d="M20 110 A80 80 0 0 1 127 40" fill="none" stroke="#3b82f6" stroke-width="18" stroke-linecap="round"/>
            <!-- Target marker at 25% -->
            <line x1="115" y1="44" x2="103" y2="32" stroke="#ef4444" stroke-width="2.5" stroke-linecap="round"/>
            <!-- Center text -->
            <text x="100" y="98" text-anchor="middle" fill="var(--sig-text-heading)" font-size="28" font-weight="700">28%</text>
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
                <div class="sig-obj-vals"><span class="sig-obj-current">&euro; 450K / 400K</span></div>
                <div class="sig-obj-track"><div class="sig-obj-fill" style="width:100%;background:#3b82f6;"></div></div>
                <div class="sig-obj-sub">Facturaci&oacute;n objetivo</div>
              </div>
            </div>
            <div class="sig-obj-item">
              <div class="sig-obj-icon sig-obj-icon--teal"><mat-icon>task_alt</mat-icon></div>
              <div class="sig-obj-body">
                <div class="sig-obj-vals"><span class="sig-obj-current">12 / 14</span></div>
                <div class="sig-obj-track"><div class="sig-obj-fill" style="width:86%;background:#00d4c4;"></div></div>
                <div class="sig-obj-sub">Cierres planificados</div>
              </div>
            </div>
            <div class="sig-obj-item">
              <div class="sig-obj-icon sig-obj-icon--green"><mat-icon>percent</mat-icon></div>
              <div class="sig-obj-body">
                <div class="sig-obj-vals"><span class="sig-obj-current">28% / 25%</span></div>
                <div class="sig-obj-track"><div class="sig-obj-fill" style="width:100%;background:#22c55e;"></div></div>
                <div class="sig-obj-sub">Margen objetivo</div>
              </div>
            </div>
          </div>
        </div>

        <!-- Alerts -->
        <div class="sig-bottom-card" data-testid="panel-alertas">
          <div class="sig-chart-icon sig-chart-icon--warn"><mat-icon>warning_amber</mat-icon></div>
          <span class="sig-chart-title">Alertas</span>
          @if (loadingAvisos()) {
            <div class="sig-alert-item sig-alert--loading" *ngFor="let i of [0,1,2]">
              <div class="sig-skeleton" style="height:14px;width:80%;border-radius:4px;"></div>
            </div>
          } @else if (avisos().length === 0) {
            <div class="sig-alert-item">
              <mat-icon style="color:var(--sig-success);font-size:18px;width:18px;height:18px;">check_circle</mat-icon>
              <span style="color:var(--sig-text-muted);font-size:13px;">No hay alertas activas</span>
            </div>
          } @else {
            @for (a of avisos(); track a.descripcion) {
              <div class="sig-alert-item" [class]="'sig-alert--' + a.tipo">
                <mat-icon class="sig-alert-icon" aria-hidden="true">{{ avisoIcon(a.tipo) }}</mat-icon>
                <span class="sig-alert-text">{{ a.descripcion }}</span>
              </div>
            }
            <!-- Demo alerts if empty -->
          }
          <!-- Demo static alerts when no backend -->
          @if (!loadingAvisos() && avisos().length === 0) {
            <div class="sig-alert-item sig-alert--warn">
              <mat-icon class="sig-alert-icon">warning</mat-icon>
              <span class="sig-alert-text">3 proyectos pendientes de aprobaci&oacute;n FICO</span>
            </div>
            <div class="sig-alert-item sig-alert--warn">
              <mat-icon class="sig-alert-icon">cancel</mat-icon>
              <span class="sig-alert-text">1 per&iacute;odo bloqueado &mdash; validaci&oacute;n contabilidad</span>
            </div>
            <div class="sig-alert-item sig-alert--success">
              <mat-icon class="sig-alert-icon" style="color:var(--sig-success)">check_circle</mat-icon>
              <span class="sig-alert-text">Cierre de abril aprobado por FICO</span>
            </div>
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
      height: 6px; background: rgba(255,255,255,.07); border-radius: 3px; overflow: hidden;
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
    }
    .sig-alert--warn {
      background: rgba(245,158,11,.07);
      border-color: rgba(245,158,11,.2);
    }
    .sig-alert--success {
      background: rgba(34,197,94,.07);
      border-color: rgba(34,197,94,.2);
    }
    .sig-alert-icon {
      font-size: 16px !important; width: 16px !important; height: 16px !important;
      color: #f59e0b !important; flex-shrink: 0; margin-top: 1px;
    }
    .sig-alert-text { font-size: 12px; color: var(--sig-text-primary); line-height: 1.4; }
`],
})
export class DashboardComponent implements OnInit {
  private readonly dashboardSvc = inject(DashboardService);
  private readonly periodSvc    = inject(PeriodService);
  private readonly notify       = inject(NotifyService);

  protected readonly kpis          = signal<DashboardKpisDto | null>(null);
  protected readonly avisos        = signal<DashboardAvisoDto[]>([]);
  protected readonly misProyectos  = signal<MiProyectoDto[]>([]);
  protected readonly loadingKpis   = signal(true);
  protected readonly loadingAvisos = signal(true);
  protected readonly loadingMis    = signal(true);
  protected readonly showDemo      = environment.showDemoCredentials;

  private readonly activePeriod = this.periodSvc.activeId;

  protected readonly facturacionK = computed(() => {
    const k = this.kpis();
    if (!k) return '...';
    return k.facturacionTotal > 0 ? Math.round(k.facturacionTotal / 1000).toString() : '0';
  });

  protected readonly margenPct = computed(() => {
    const k = this.kpis();
    if (!k || k.facturacionTotal === 0) return '0';
    return Math.round((k.margen / k.facturacionTotal) * 100).toString();
  });

  constructor() {
    effect(() => {
      const pid = this.activePeriod();
      this.loadAll(pid ?? undefined);
    });
  }

  ngOnInit(): void { }

  private loadAll(periodId?: number): void {
    this.loadingKpis.set(true);
    this.loadingAvisos.set(true);
    this.loadingMis.set(true);
    this.dashboardSvc.getKpis(periodId).subscribe({
      next:  (d) => { this.kpis.set(d);  this.loadingKpis.set(false); },
      error: ()  => { this.kpis.set(null); this.loadingKpis.set(false); },
    });
    this.dashboardSvc.getAvisos().subscribe({
      next:  (d) => { this.avisos.set(d);   this.loadingAvisos.set(false); },
      error: ()  => { this.avisos.set([]);   this.loadingAvisos.set(false); },
    });
    this.dashboardSvc.getMisProyectos(periodId).subscribe({
      next:  (d) => { this.misProyectos.set(d);   this.loadingMis.set(false); },
      error: ()  => { this.misProyectos.set([]);   this.loadingMis.set(false); },
    });
  }

  protected avisoIcon(tipo: string): string {
    switch (tipo) {
      case 'CierrePendiente':   return 'warning';
      case 'PeriodoBloqueado':  return 'cancel';
      case 'ErrorSync':         return 'error';
      default:                  return 'info';
    }
  }
}
