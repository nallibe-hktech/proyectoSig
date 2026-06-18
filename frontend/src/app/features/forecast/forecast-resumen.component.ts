import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ForecastService } from '../../core/api/forecast.service';
import { ServiceService } from '../../core/api/services.service';
import { ClientService } from '../../core/api/clients.service';
import { DepartmentService } from '../../core/api/catalogs.service';
import { NotifyService } from '../../core/notify.service';
import {
  ForecastResumenFilaDto, ServiceListItemDto, ClientListItemDto, DepartmentDto,
} from '../../models/dtos';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';

const MESES = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];

@Component({
  selector: 'app-forecast-resumen',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatCardModule, MatIconModule, MatTabsModule,
    MatFormFieldModule, MatSelectModule, MatProgressSpinnerModule, BreadcrumbsComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Forecast' }]" />

      <div class="sig-page__header">
        <h1 class="sig-page__title">Forecast — Resumen</h1>
      </div>

      <mat-card>
        <mat-card-content>
          <div class="filtros">
            <mat-form-field appearance="outline" subscriptSizing="dynamic">
              <mat-label>Año</mat-label>
              <mat-select [(ngModel)]="anio" (ngModelChange)="load()" data-testid="filtro-anio">
                @for (y of anios; track y) { <mat-option [value]="y">{{ y }}</mat-option> }
              </mat-select>
            </mat-form-field>

            <mat-form-field appearance="outline" subscriptSizing="dynamic">
              <mat-label>Departamento</mat-label>
              <mat-select [(ngModel)]="departmentId" (ngModelChange)="load()" data-testid="filtro-dpto">
                <mat-option [value]="null">Todos</mat-option>
                @for (d of departments(); track d.id) { <mat-option [value]="d.id">{{ d.nombre }}</mat-option> }
              </mat-select>
            </mat-form-field>

            <mat-form-field appearance="outline" subscriptSizing="dynamic">
              <mat-label>Cliente</mat-label>
              <mat-select [(ngModel)]="clientId" (ngModelChange)="load()" data-testid="filtro-cliente">
                <mat-option [value]="null">Todos</mat-option>
                @for (c of clients(); track c.id) { <mat-option [value]="c.id">{{ c.nombre }}</mat-option> }
              </mat-select>
            </mat-form-field>

            <mat-form-field appearance="outline" subscriptSizing="dynamic">
              <mat-label>Servicio</mat-label>
              <mat-select [(ngModel)]="serviceId" (ngModelChange)="load()" data-testid="filtro-servicio">
                <mat-option [value]="null">Todos</mat-option>
                @for (s of services(); track s.id) { <mat-option [value]="s.id">{{ s.nombre }}</mat-option> }
              </mat-select>
            </mat-form-field>
          </div>

          @if (loading()) {
            <div class="center"><mat-spinner diameter="40"></mat-spinner></div>
          } @else if (filas().length === 0) {
            <p class="empty">No hay datos de forecast para los filtros seleccionados.</p>
          } @else {
            <mat-tab-group>
              <mat-tab label="Ventas (€)">
                <ng-container [ngTemplateOutlet]="pivot" [ngTemplateOutletContext]="{ metric: 'ventas', decimals: true }"></ng-container>
              </mat-tab>
              <mat-tab label="GPP (nº personas)">
                <ng-container [ngTemplateOutlet]="pivot" [ngTemplateOutletContext]="{ metric: 'personas', decimals: false }"></ng-container>
              </mat-tab>
            </mat-tab-group>
          }

          <ng-template #pivot let-metric="metric" let-decimals="decimals">
            <div class="table-scroll">
              <table class="pivot">
                <thead>
                  <tr>
                    <th class="sticky">Departamento</th>
                    <th class="sticky">Cliente</th>
                    @for (m of meses; track m.idx) { <th class="num">{{ m.nombre }}</th> }
                    <th class="num total-col">Total</th>
                  </tr>
                </thead>
                <tbody>
                  @for (f of filas(); track f.clientId + '-' + (f.departmentId ?? 0)) {
                    <tr>
                      <td>{{ f.departmentNombre ?? '—' }}</td>
                      <td>{{ f.clientNombre }}</td>
                      @for (m of meses; track m.idx) {
                        <td class="num">{{ cell(f, m.idx, metric) | number: (decimals ? '1.0-0' : '1.0-0') }}</td>
                      }
                      <td class="num total-col">{{ rowTotal(f, metric) | number:'1.0-0' }}</td>
                    </tr>
                  }
                  <tr class="totales">
                    <td colspan="2">Total</td>
                    @for (m of meses; track m.idx) { <td class="num">{{ colTotal(m.idx, metric) | number:'1.0-0' }}</td> }
                    <td class="num total-col">{{ grandTotal(metric) | number:'1.0-0' }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </ng-template>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .filtros { display: flex; flex-wrap: wrap; gap: 12px; margin-bottom: 16px; }
    .filtros mat-form-field { width: 180px; }
    .center { display: flex; justify-content: center; padding: 40px; }
    .empty { color: var(--mat-sys-on-surface-variant); text-align: center; padding: 24px; }
    .table-scroll { overflow-x: auto; padding-top: 12px; }
    table.pivot { border-collapse: collapse; width: 100%; font-size: 13px; }
    table.pivot th, table.pivot td { border: 1px solid var(--mat-sys-outline-variant); padding: 6px 10px; white-space: nowrap; }
    table.pivot th { background: var(--mat-sys-surface-container); font-weight: 600; text-align: left; }
    .num { text-align: right; font-variant-numeric: tabular-nums; }
    .total-col { font-weight: 600; background: var(--mat-sys-surface-container-low); }
    tr.totales td { font-weight: 700; background: var(--mat-sys-surface-container); }
    .sticky { position: sticky; left: 0; }
  `],
})
export class ForecastResumenComponent implements OnInit {
  private readonly forecastSvc = inject(ForecastService);
  private readonly serviceSvc = inject(ServiceService);
  private readonly clientSvc = inject(ClientService);
  private readonly departmentSvc = inject(DepartmentService);
  private readonly notify = inject(NotifyService);

  private readonly now = new Date();
  protected readonly anios: number[] = [this.now.getFullYear() - 1, this.now.getFullYear(), this.now.getFullYear() + 1, this.now.getFullYear() + 2];
  protected anio = this.now.getFullYear();
  protected departmentId: number | null = null;
  protected clientId: number | null = null;
  protected serviceId: number | null = null;

  protected readonly meses = MESES.map((nombre, idx) => ({ idx: idx + 1, nombre }));
  protected readonly filas = signal<ForecastResumenFilaDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly departments = signal<DepartmentDto[]>([]);
  protected readonly clients = signal<ClientListItemDto[]>([]);
  protected readonly services = signal<ServiceListItemDto[]>([]);

  ngOnInit(): void {
    this.departmentSvc.list().subscribe({ next: (d) => this.departments.set(d), error: () => {} });
    this.clientSvc.list(1, 1000).subscribe({ next: (r) => this.clients.set(r.items), error: () => {} });
    this.serviceSvc.list(1, 1000).subscribe({ next: (r) => this.services.set(r.items), error: () => {} });
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.forecastSvc.resumen(this.anio, {
      departmentId: this.departmentId ?? undefined,
      clientId: this.clientId ?? undefined,
      serviceId: this.serviceId ?? undefined,
    }).subscribe({
      next: (r) => { this.filas.set(r.filas); this.loading.set(false); },
      error: () => { this.notify.error('No se pudo cargar el resumen de forecast'); this.filas.set([]); this.loading.set(false); },
    });
  }

  protected cell(f: ForecastResumenFilaDto, mes: number, metric: 'ventas' | 'personas'): number {
    const c = f.meses.find((x) => x.mes === mes);
    return c ? (metric === 'ventas' ? c.ventas : c.personas) : 0;
  }
  protected rowTotal(f: ForecastResumenFilaDto, metric: 'ventas' | 'personas'): number {
    return metric === 'ventas' ? f.totalVentas : f.totalPersonas;
  }
  protected colTotal(mes: number, metric: 'ventas' | 'personas'): number {
    return this.filas().reduce((acc, f) => acc + this.cell(f, mes, metric), 0);
  }
  protected grandTotal(metric: 'ventas' | 'personas'): number {
    return this.filas().reduce((acc, f) => acc + this.rowTotal(f, metric), 0);
  }
}
