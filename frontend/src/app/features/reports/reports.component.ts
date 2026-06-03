import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [MatCardModule, MatIconModule, BreadcrumbsComponent],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Reports' }]" />
      <div class="sig-page__header"><h1 class="sig-page__title">Reports</h1></div>

      <mat-card style="margin-bottom: 16px;">
        <mat-card-content>
          <div style="display: flex; gap: 16px; align-items: flex-start;">
            <mat-icon style="font-size: 48px; width: 48px; height: 48px; color: var(--sig-teal);">bar_chart</mat-icon>
            <div>
              <h3 style="margin: 0 0 8px;">Reporting via Power BI</h3>
              <p style="margin: 0; color: var(--mat-sys-on-surface-variant);">
                Los informes operativos y financieros se publican en Power BI conectado a las vistas
                del schema <code>bi</code> de PostgreSQL. Esta sección lista los informes disponibles.
              </p>
            </div>
          </div>
        </mat-card-content>
      </mat-card>

      <div class="sig-reports-grid">
        @for (r of reports; track r.title) {
          <mat-card class="sig-report-card" [attr.data-testid]="'report-' + r.id">
            <mat-card-header>
              <mat-icon mat-card-avatar>{{ r.icon }}</mat-icon>
              <mat-card-title>{{ r.title }}</mat-card-title>
              <mat-card-subtitle>{{ r.view }}</mat-card-subtitle>
            </mat-card-header>
            <mat-card-content>
              <p>{{ r.description }}</p>
            </mat-card-content>
          </mat-card>
        }
      </div>
    </div>
  `,
  styles: [`
    .sig-reports-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(300px, 1fr)); gap: 16px; }
    .sig-report-card { min-height: 180px; }
  `],
})
export class ReportsComponent {
  protected readonly reports = [
    { id: 'cierres-periodo', icon: 'lock_clock', title: 'Cierres por período', view: 'bi.v_cierres_por_periodo', description: 'Estado y totales de los cierres por proyecto y período.' },
    { id: 'lineas-concepto', icon: 'calculate', title: 'Líneas por concepto', view: 'bi.v_lineas_por_concepto', description: 'Detalle de importes de cada línea de cierre agrupados por concepto.' },
    { id: 'aprobaciones', icon: 'approval', title: 'Aprobaciones pendientes', view: 'bi.v_aprobaciones_pendientes', description: 'Cierres pendientes y su paso actual en el flujo.' },
    { id: 'audit-resumen', icon: 'history', title: 'Resumen de auditoría', view: 'bi.v_audit_resumen', description: 'Actividad de los últimos 30 días por usuario.' },
  ];
}
