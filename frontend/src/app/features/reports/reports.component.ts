import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTabsModule } from '@angular/material/tabs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { ReportsService } from '../../core/api/reports.service';
import { ServiceService } from '../../core/api/services.service';
import { ClientService } from '../../core/api/clients.service';
import { DepartmentService } from '../../core/api/catalogs.service';
import { NotifyService } from '../../core/notify.service';
import {
  ReporteResultadoFilaDto, PrevisionRealFilaDto, DepartmentDto, ClientListItemDto, ServiceListItemDto,
} from '../../models/dtos';

const MESES = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic'];

interface ServicioNode { serviceId: number; nombre: string; facturacion: number; coste: number; margen: number; }
interface ClienteNode { key: string; clientId: number; nombre: string; facturacion: number; coste: number; margen: number; servicios: ServicioNode[]; }
interface DeptNode { key: string; nombre: string; facturacion: number; coste: number; margen: number; clientes: ClienteNode[]; }

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatCardModule, MatIconModule, MatButtonModule, MatTabsModule,
    MatFormFieldModule, MatSelectModule, MatProgressSpinnerModule, BreadcrumbsComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Informes' }]" />
      <div class="sig-page__header"><h1 class="sig-page__title">Informes</h1></div>

      <mat-card>
        <mat-card-content>
          <div class="filtros">
            <mat-form-field appearance="outline" subscriptSizing="dynamic">
              <mat-label>Año</mat-label>
              <mat-select [(ngModel)]="anio" (ngModelChange)="load()" data-testid="rep-anio">
                @for (y of anios; track y) { <mat-option [value]="y">{{ y }}</mat-option> }
              </mat-select>
            </mat-form-field>
            <mat-form-field appearance="outline" subscriptSizing="dynamic">
              <mat-label>Departamento</mat-label>
              <mat-select [(ngModel)]="departmentId" (ngModelChange)="load()" data-testid="rep-dpto">
                <mat-option [value]="null">Todos</mat-option>
                @for (d of departments(); track d.id) { <mat-option [value]="d.id">{{ d.nombre }}</mat-option> }
              </mat-select>
            </mat-form-field>
            <mat-form-field appearance="outline" subscriptSizing="dynamic">
              <mat-label>Cliente</mat-label>
              <mat-select [(ngModel)]="clientId" (ngModelChange)="load()" data-testid="rep-cliente">
                <mat-option [value]="null">Todos</mat-option>
                @for (c of clients(); track c.id) { <mat-option [value]="c.id">{{ c.nombre }}</mat-option> }
              </mat-select>
            </mat-form-field>
            <mat-form-field appearance="outline" subscriptSizing="dynamic">
              <mat-label>Servicio</mat-label>
              <mat-select [(ngModel)]="serviceId" (ngModelChange)="load()" data-testid="rep-servicio">
                <mat-option [value]="null">Todos</mat-option>
                @for (s of services(); track s.id) { <mat-option [value]="s.id">{{ s.nombre }}</mat-option> }
              </mat-select>
            </mat-form-field>
          </div>

          <mat-tab-group>
            <!-- Resultado: drill-down dpto → cliente → servicio -->
            <mat-tab label="Resultado (facturación / coste / margen)">
              @if (loading()) {
                <div class="center"><mat-spinner diameter="40"></mat-spinner></div>
              } @else if (arbol().length === 0) {
                <p class="empty">Sin datos para los filtros seleccionados.</p>
              } @else {
                <div class="table-scroll">
                  <table class="rep">
                    <thead>
                      <tr>
                        <th>Departamento / Cliente / Servicio</th>
                        <th class="num">Facturación</th>
                        <th class="num">Coste</th>
                        <th class="num">Margen</th>
                      </tr>
                    </thead>
                    <tbody>
                      @for (d of arbol(); track d.key) {
                        <tr class="lvl-dpto" (click)="toggle(d.key)" data-testid="rep-row-dpto">
                          <td>
                            <mat-icon class="caret">{{ isOpen(d.key) ? 'expand_more' : 'chevron_right' }}</mat-icon>
                            {{ d.nombre }}
                          </td>
                          <td class="num">{{ d.facturacion | number:'1.0-0' }} €</td>
                          <td class="num">{{ d.coste | number:'1.0-0' }} €</td>
                          <td class="num" [class.neg]="d.margen < 0">{{ d.margen | number:'1.0-0' }} €</td>
                        </tr>
                        @if (isOpen(d.key)) {
                          @for (c of d.clientes; track c.key) {
                            <tr class="lvl-cliente" (click)="toggle(c.key)">
                              <td class="ind-1">
                                <mat-icon class="caret">{{ isOpen(c.key) ? 'expand_more' : 'chevron_right' }}</mat-icon>
                                {{ c.nombre }}
                              </td>
                              <td class="num">{{ c.facturacion | number:'1.0-0' }} €</td>
                              <td class="num">{{ c.coste | number:'1.0-0' }} €</td>
                              <td class="num" [class.neg]="c.margen < 0">{{ c.margen | number:'1.0-0' }} €</td>
                            </tr>
                            @if (isOpen(c.key)) {
                              @for (s of c.servicios; track s.serviceId) {
                                <tr class="lvl-servicio">
                                  <td class="ind-2">{{ s.nombre }}</td>
                                  <td class="num">{{ s.facturacion | number:'1.0-0' }} €</td>
                                  <td class="num">{{ s.coste | number:'1.0-0' }} €</td>
                                  <td class="num" [class.neg]="s.margen < 0">{{ s.margen | number:'1.0-0' }} €</td>
                                </tr>
                              }
                            }
                          }
                        }
                      }
                      <tr class="totales">
                        <td>Total</td>
                        <td class="num">{{ totalResultado().facturacion | number:'1.0-0' }} €</td>
                        <td class="num">{{ totalResultado().coste | number:'1.0-0' }} €</td>
                        <td class="num" [class.neg]="totalResultado().margen < 0">{{ totalResultado().margen | number:'1.0-0' }} €</td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              }
            </mat-tab>

            <!-- Previsión (Forecast) vs Real -->
            <mat-tab label="Previsión vs Real">
              <div class="metric-switch">
                <button mat-stroked-button [color]="metric() === 'ventas' ? 'primary' : ''" (click)="metric.set('ventas')" data-testid="rep-metric-ventas">Ventas</button>
                <button mat-stroked-button [color]="metric() === 'margen' ? 'primary' : ''" (click)="metric.set('margen')" data-testid="rep-metric-margen">Margen</button>
              </div>
              @if (loading()) {
                <div class="center"><mat-spinner diameter="40"></mat-spinner></div>
              } @else if (prevision().length === 0) {
                <p class="empty">Sin datos para los filtros seleccionados.</p>
              } @else {
                <div class="table-scroll">
                  <table class="rep">
                    <thead>
                      <tr>
                        <th class="sticky">Departamento</th>
                        <th class="sticky">Cliente</th>
                        @for (m of meses; track m.idx) { <th class="num">{{ m.nombre }}</th> }
                        <th class="num total-col">Total</th>
                      </tr>
                      <tr class="subhead">
                        <th colspan="2"></th>
                        @for (m of meses; track m.idx) { <th class="num small">Prev / Real</th> }
                        <th class="num small total-col">Prev / Real</th>
                      </tr>
                    </thead>
                    <tbody>
                      @for (f of prevision(); track f.clientId + '-' + (f.departmentId ?? 0)) {
                        <tr>
                          <td>{{ f.departmentNombre ?? '—' }}</td>
                          <td>{{ f.clientNombre }}</td>
                          @for (m of meses; track m.idx) {
                            <td class="num cell">
                              <span class="prev">{{ prev(f, m.idx) | number:'1.0-0' }}</span>
                              <span class="sep">/</span>
                              <span class="real" [class.neg]="real(f, m.idx) < 0">{{ real(f, m.idx) | number:'1.0-0' }}</span>
                            </td>
                          }
                          <td class="num total-col">
                            <span class="prev">{{ totPrev(f) | number:'1.0-0' }}</span>
                            <span class="sep">/</span>
                            <span class="real" [class.neg]="totReal(f) < 0">{{ totReal(f) | number:'1.0-0' }}</span>
                          </td>
                        </tr>
                      }
                    </tbody>
                  </table>
                  <p class="hint">Cada celda muestra <strong>Previsión (forecast)</strong> / <strong>Real (cierre)</strong>. Valores en €.</p>
                </div>
              }
            </mat-tab>
          </mat-tab-group>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .filtros { display: flex; flex-wrap: wrap; gap: 12px; margin-bottom: 8px; }
    .filtros mat-form-field { width: 180px; }
    .center { display: flex; justify-content: center; padding: 40px; }
    .empty { color: var(--mat-sys-on-surface-variant); text-align: center; padding: 24px; }
    .table-scroll { overflow-x: auto; padding-top: 12px; }
    table.rep { border-collapse: collapse; width: 100%; font-size: 13px; }
    table.rep th, table.rep td { border: 1px solid var(--mat-sys-outline-variant); padding: 6px 10px; white-space: nowrap; }
    table.rep th { background: var(--mat-sys-surface-container); font-weight: 600; text-align: left; }
    .num { text-align: right; font-variant-numeric: tabular-nums; }
    .neg { color: #ef4444; }
    .lvl-dpto { cursor: pointer; font-weight: 600; background: var(--mat-sys-surface-container-low); }
    .lvl-cliente { cursor: pointer; }
    .lvl-servicio { color: var(--mat-sys-on-surface-variant); }
    .caret { font-size: 18px; height: 18px; width: 18px; vertical-align: middle; }
    .ind-1 { padding-left: 28px !important; }
    .ind-2 { padding-left: 52px !important; }
    tr.totales td { font-weight: 700; background: var(--mat-sys-surface-container); }
    .total-col { font-weight: 600; background: var(--mat-sys-surface-container-low); }
    .metric-switch { display: flex; gap: 8px; padding: 12px 0 4px; }
    .subhead th { font-weight: 500; font-size: 11px; color: var(--mat-sys-on-surface-variant); }
    .small { font-size: 11px; }
    .cell .prev { color: var(--mat-sys-on-surface-variant); }
    .cell .sep { margin: 0 3px; color: var(--mat-sys-outline); }
    .hint { color: var(--mat-sys-on-surface-variant); font-size: 12px; margin: 8px 0 0; }
    .sticky { position: sticky; left: 0; }
  `],
})
export class ReportsComponent implements OnInit {
  private readonly reportsSvc = inject(ReportsService);
  private readonly serviceSvc = inject(ServiceService);
  private readonly clientSvc = inject(ClientService);
  private readonly departmentSvc = inject(DepartmentService);
  private readonly notify = inject(NotifyService);

  private readonly now = new Date();
  protected readonly anios: number[] = [this.now.getFullYear() - 2, this.now.getFullYear() - 1, this.now.getFullYear(), this.now.getFullYear() + 1];
  protected anio = this.now.getFullYear();
  protected departmentId: number | null = null;
  protected clientId: number | null = null;
  protected serviceId: number | null = null;

  protected readonly meses = MESES.map((nombre, idx) => ({ idx: idx + 1, nombre }));
  protected readonly metric = signal<'ventas' | 'margen'>('ventas');

  protected readonly resultado = signal<ReporteResultadoFilaDto[]>([]);
  protected readonly prevision = signal<PrevisionRealFilaDto[]>([]);
  protected readonly loading = signal(true);
  private readonly expanded = signal<Set<string>>(new Set());

  protected readonly departments = signal<DepartmentDto[]>([]);
  protected readonly clients = signal<ClientListItemDto[]>([]);
  protected readonly services = signal<ServiceListItemDto[]>([]);

  // Árbol dpto → cliente → servicio a partir de las filas planas.
  protected readonly arbol = computed<DeptNode[]>(() => {
    const depts = new Map<string, DeptNode>();
    for (const f of this.resultado()) {
      const dKey = 'd' + (f.departmentId ?? 0);
      let d = depts.get(dKey);
      if (!d) { d = { key: dKey, nombre: f.departmentNombre ?? 'Sin departamento', facturacion: 0, coste: 0, margen: 0, clientes: [] }; depts.set(dKey, d); }
      const cKey = dKey + '-c' + f.clientId;
      let c = d.clientes.find(x => x.key === cKey);
      if (!c) { c = { key: cKey, clientId: f.clientId, nombre: f.clientNombre, facturacion: 0, coste: 0, margen: 0, servicios: [] }; d.clientes.push(c); }
      c.servicios.push({ serviceId: f.serviceId, nombre: f.serviceNombre, facturacion: f.facturacion, coste: f.coste, margen: f.margen });
      c.facturacion += f.facturacion; c.coste += f.coste; c.margen += f.margen;
      d.facturacion += f.facturacion; d.coste += f.coste; d.margen += f.margen;
    }
    return [...depts.values()].sort((a, b) => a.nombre.localeCompare(b.nombre));
  });

  protected readonly totalResultado = computed(() => {
    return this.resultado().reduce((acc, f) => ({
      facturacion: acc.facturacion + f.facturacion,
      coste: acc.coste + f.coste,
      margen: acc.margen + f.margen,
    }), { facturacion: 0, coste: 0, margen: 0 });
  });

  ngOnInit(): void {
    this.departmentSvc.list().subscribe({ next: (d) => this.departments.set(d), error: () => {} });
    this.clientSvc.list(1, 1000).subscribe({ next: (r) => this.clients.set(r.items), error: () => {} });
    this.serviceSvc.list(1, 1000).subscribe({ next: (r) => this.services.set(r.items), error: () => {} });
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    const filters = {
      departmentId: this.departmentId ?? undefined,
      clientId: this.clientId ?? undefined,
      serviceId: this.serviceId ?? undefined,
    };
    this.reportsSvc.resultado(this.anio, filters).subscribe({
      next: (r) => { this.resultado.set(r.filas); this.loading.set(false); },
      error: () => { this.notify.error('No se pudo cargar el informe'); this.resultado.set([]); this.loading.set(false); },
    });
    this.reportsSvc.previsionVsReal(this.anio, filters).subscribe({
      next: (r) => this.prevision.set(r.filas),
      error: () => this.prevision.set([]),
    });
  }

  protected toggle(key: string): void {
    const s = new Set(this.expanded());
    if (s.has(key)) s.delete(key); else s.add(key);
    this.expanded.set(s);
  }
  protected isOpen(key: string): boolean { return this.expanded().has(key); }

  // Previsión vs Real: selecciona métrica activa.
  protected prev(f: PrevisionRealFilaDto, mes: number): number {
    const c = f.meses.find(x => x.mes === mes);
    if (!c) return 0;
    return this.metric() === 'ventas' ? c.ventasPrevistas : c.margenPrevisto;
  }
  protected real(f: PrevisionRealFilaDto, mes: number): number {
    const c = f.meses.find(x => x.mes === mes);
    if (!c) return 0;
    return this.metric() === 'ventas' ? c.ventasReales : c.margenReal;
  }
  protected totPrev(f: PrevisionRealFilaDto): number {
    return this.metric() === 'ventas' ? f.totalVentasPrevistas : f.totalMargenPrevisto;
  }
  protected totReal(f: PrevisionRealFilaDto): number {
    return this.metric() === 'ventas' ? f.totalVentasReales : f.totalMargenReal;
  }
}
