import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { IncidenciasService } from '../../core/api/incidencias.service';
import { ClientService } from '../../core/api/clients.service';
import { AuthService } from '../../core/auth/auth.service';
import { NotifyService } from '../../core/notify.service';
import {
  ClientListItemDto, ClienteIncidenciaDto, IncidenciaListItemDto,
} from '../../models/dtos';
import { EstadoIncidencia } from '../../models/enums';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { IncidenciaFormDialog, IncidenciaFormData, IncidenciaFormResult } from './incidencia-form.dialog';
import { IncidenciaEstadoDialog, IncidenciaEstadoData } from './incidencia-estado.dialog';
import { IncidenciaCambioEstadoRequest } from '../../models/dtos';

// Incidencias — pantalla de 1er nivel (prototipo 4/28 y 5/28): listado global con filtros, panel de
// detalle con histórico, alta manual y cambio de estado. El backend restringe a clientes accesibles.
@Component({
  selector: 'app-incidencias-list',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatTableModule, MatPaginatorModule, MatDialogModule,
    BreadcrumbsComponent, SkeletonComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Incidencias' }]" />

      <div class="sig-page__header">
        <h1 class="sig-page__title"><mat-icon class="title-icon">report_problem</mat-icon> Incidencias de Cliente</h1>
        @if (canManage()) {
          <button mat-flat-button color="primary" (click)="nueva()" data-testid="btn-nueva-incidencia">
            <mat-icon>add</mat-icon> Nueva incidencia
          </button>
        }
      </div>

      <!-- Filtros -->
      <mat-card class="filters-card">
        <div class="filters">
          <mat-form-field appearance="outline" class="f-search">
            <mat-icon matPrefix>search</mat-icon>
            <mat-label>Buscar incidencia…</mat-label>
            <input matInput [formControl]="search" data-testid="filtro-buscar" />
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Cliente</mat-label>
            <mat-select [(value)]="clienteFilter" (selectionChange)="aplicar()" data-testid="filtro-cliente">
              <mat-option [value]="null">Todos</mat-option>
              @for (c of clientes(); track c.id) { <mat-option [value]="c.id">{{ c.nombre }}</mat-option> }
            </mat-select>
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Tipo</mat-label>
            <mat-select [(value)]="tipoFilter" (selectionChange)="aplicar()" data-testid="filtro-tipo">
              <mat-option [value]="null">Todos</mat-option>
              @for (t of tipos(); track t) { <mat-option [value]="t">{{ t }}</mat-option> }
            </mat-select>
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Estado</mat-label>
            <mat-select [(value)]="estadoFilter" (selectionChange)="aplicar()" data-testid="filtro-estado">
              <mat-option [value]="null">Todos</mat-option>
              <mat-option value="Abierta">Pendiente</mat-option>
              <mat-option value="EnProceso">En proceso</mat-option>
              <mat-option value="Resuelta">Resuelta</mat-option>
            </mat-select>
          </mat-form-field>
          <button mat-flat-button color="primary" (click)="aplicar()" data-testid="btn-filtrar"><mat-icon>filter_list</mat-icon> Filtrar</button>
          <button mat-stroked-button (click)="limpiar()" data-testid="btn-limpiar">Limpiar</button>
          <span class="f-total">TOTAL<br /><strong>{{ total() }}</strong></span>
        </div>
      </mat-card>

      <div class="layout">
        <!-- Tabla -->
        <mat-card class="table-card">
          <mat-card-content>
            @if (loading()) {
              <sig-skeleton [count]="5" />
            } @else if (items().length === 0) {
              <p class="sig-empty-text">No hay incidencias que coincidan con los filtros.</p>
            } @else {
              <table mat-table [dataSource]="items()" class="sig-table">
                <ng-container matColumnDef="cliente">
                  <th mat-header-cell *matHeaderCellDef>Cliente</th>
                  <td mat-cell *matCellDef="let i">{{ i.clientNombre }}</td>
                </ng-container>
                <ng-container matColumnDef="incidencia">
                  <th mat-header-cell *matHeaderCellDef>Incidencia</th>
                  <td mat-cell *matCellDef="let i">
                    <div class="cell-title">{{ i.tipo }}</div>
                    <div class="cell-sub">{{ i.descripcion }}</div>
                  </td>
                </ng-container>
                <ng-container matColumnDef="tipo">
                  <th mat-header-cell *matHeaderCellDef>Tipo</th>
                  <td mat-cell *matCellDef="let i"><span class="chip-tipo">{{ i.tipo }}</span></td>
                </ng-container>
                <ng-container matColumnDef="apertura">
                  <th mat-header-cell *matHeaderCellDef>Apertura</th>
                  <td mat-cell *matCellDef="let i">{{ i.fechaApertura | date:'dd/MM/yyyy' }}</td>
                </ng-container>
                <ng-container matColumnDef="estado">
                  <th mat-header-cell *matHeaderCellDef>Estado</th>
                  <td mat-cell *matCellDef="let i"><span class="sig-badge" [class]="estadoBadge(i.estado)">{{ estadoLabel(i.estado) }}</span></td>
                </ng-container>
                <tr mat-header-row *matHeaderRowDef="cols"></tr>
                <tr mat-row *matRowDef="let row; columns: cols"
                    class="clickable" [class.selected]="selected()?.id === row.id"
                    (click)="seleccionar(row)" [attr.data-testid]="'row-incidencia-' + row.id"></tr>
              </table>
              <mat-paginator
                [length]="total()" [pageSize]="pageSize()" [pageIndex]="page() - 1"
                [pageSizeOptions]="[10, 25, 50]" showFirstLastButtons (page)="onPage($event)" />
            }
          </mat-card-content>
        </mat-card>

        <!-- Panel de detalle -->
        @if (selected(); as inc) {
          <mat-card class="detail-card" data-testid="panel-detalle">
            <div class="detail-head">
              <span class="detail-head__title"><mat-icon>report_problem</mat-icon> {{ inc.tipo }}</span>
              <button mat-icon-button (click)="cerrarDetalle()" data-testid="btn-cerrar-detalle"><mat-icon>close</mat-icon></button>
            </div>
            <mat-card-content>
              <p class="inc-note">
                <mat-icon>info</mat-icon>
                Entrada <strong>manual</strong> (comercial / contabilidad). No proviene de ningún sistema origen.
              </p>
              <dl class="detail-dl">
                <div><dt>Cliente</dt><dd>{{ selectedCliente() }}</dd></div>
                <div><dt>Estado</dt><dd><span class="sig-badge" [class]="estadoBadge(inc.estado)">{{ estadoLabel(inc.estado) }}</span></dd></div>
                <div><dt>Tipo</dt><dd>{{ inc.tipo }}</dd></div>
                <div><dt>Apertura</dt><dd>{{ inc.fechaApertura | date:'dd/MM/yyyy' }}</dd></div>
                @if (inc.origen) { <div><dt>Origen / Resp.</dt><dd>{{ inc.origen }}</dd></div> }
              </dl>
              <h4 class="detail-section">Descripción</h4>
              <p class="detail-desc">{{ inc.descripcion }}</p>

              <h4 class="detail-section">Histórico de la incidencia</h4>
              @if (inc.historial.length === 0) {
                <p class="sig-empty-text">Sin movimientos registrados.</p>
              } @else {
                <ul class="timeline">
                  @for (h of inc.historial; track h.id) {
                    <li class="timeline__item">
                      <span class="timeline__dot" [class]="estadoDot(h.estado)"></span>
                      <div>
                        <div class="timeline__note">{{ h.nota }}</div>
                        <div class="timeline__meta">{{ h.fecha | date:'dd/MM/yyyy' }}@if (h.responsable) { · {{ h.responsable }} }</div>
                      </div>
                    </li>
                  }
                </ul>
              }
            </mat-card-content>
            @if (canManage()) {
              <div class="detail-actions">
                <button mat-stroked-button (click)="actualizarEstado(inc)" data-testid="btn-actualizar-estado"><mat-icon>edit</mat-icon> Actualizar estado</button>
                <button mat-flat-button color="primary" [disabled]="inc.estado === 'Resuelta'" (click)="marcarResuelta(inc)" data-testid="btn-marcar-resuelta"><mat-icon>check</mat-icon> Marcar resuelta</button>
              </div>
            }
          </mat-card>
        }
      </div>
    </div>
  `,
  styles: [`
    .title-icon { vertical-align: middle; color: #f59e0b; }
    .filters-card { margin-bottom: 16px; }
    .filters { display: flex; flex-wrap: wrap; align-items: center; gap: 12px; }
    .filters .f-search { flex: 1 1 280px; }
    .filters mat-form-field { margin-bottom: -1.25em; }
    .f-total { margin-left: auto; text-align: center; font-size: 11px; letter-spacing: .06em; color: var(--mat-sys-on-surface-variant); }
    .f-total strong { font-size: 20px; color: var(--mat-sys-on-surface); }
    .layout { display: grid; grid-template-columns: 1fr; gap: 16px; }
    .layout:has(.detail-card) { grid-template-columns: minmax(0, 1fr) 380px; }
    .sig-table { width: 100%; }
    .clickable { cursor: pointer; }
    .clickable:hover { background: var(--mat-sys-surface-container-high); }
    .selected { background: var(--mat-sys-secondary-container); }
    .cell-title { font-weight: 600; }
    .cell-sub { font-size: 12px; color: var(--mat-sys-on-surface-variant); }
    .chip-tipo { display: inline-block; padding: 2px 10px; border-radius: 6px; font-size: 12px;
      background: var(--mat-sys-surface-container-highest); color: var(--mat-sys-on-surface); }
    .sig-badge { display: inline-flex; align-items: center; gap: 5px; padding: 2px 10px; border-radius: 20px; font-size: 11px; font-weight: 600; }
    .sig-badge::before { content: ''; width: 6px; height: 6px; border-radius: 50%; background: currentColor; }
    .sig-badge--green { color: #22c55e; background: rgba(34,197,94,.12); }
    .sig-badge--amber { color: #f59e0b; background: rgba(245,158,11,.12); }
    .sig-badge--blue { color: #3b82f6; background: rgba(59,130,246,.12); }
    .detail-card { align-self: start; position: sticky; top: 16px; }
    .detail-head { display: flex; align-items: center; justify-content: space-between; padding: 12px 16px;
      background: var(--mat-sys-primary); color: var(--mat-sys-on-primary); border-radius: 12px 12px 0 0; }
    .detail-head__title { display: flex; align-items: center; gap: 8px; font-weight: 600; }
    .inc-note { display:flex; gap:8px; align-items:flex-start; background:rgba(245,158,11,.10);
      border:1px solid rgba(245,158,11,.30); border-radius:8px; padding:10px 12px; margin:0 0 14px; font-size:12px; }
    .inc-note mat-icon { color:#f59e0b; font-size:18px; width:18px; height:18px; }
    .detail-dl { display: grid; grid-template-columns: 1fr 1fr; gap: 10px 16px; margin: 0; }
    .detail-dl dt { font-size: 11px; letter-spacing: .05em; text-transform: uppercase; color: var(--mat-sys-on-surface-variant); }
    .detail-dl dd { margin: 2px 0 0; font-weight: 500; }
    .detail-section { margin: 16px 0 6px; font-size: 11px; letter-spacing: .05em; text-transform: uppercase; color: var(--mat-sys-on-surface-variant); }
    .detail-desc { margin: 0; white-space: pre-wrap; }
    .timeline { list-style: none; margin: 0; padding: 0; }
    .timeline__item { display: flex; gap: 10px; padding: 0 0 14px; position: relative; }
    .timeline__item:not(:last-child)::before { content: ''; position: absolute; left: 5px; top: 14px; bottom: 0; width: 2px; background: var(--mat-sys-outline-variant); }
    .timeline__dot { width: 12px; height: 12px; border-radius: 50%; margin-top: 2px; flex-shrink: 0; border: 2px solid currentColor; }
    .timeline__dot.dot--green { color: #22c55e; background: #22c55e; }
    .timeline__dot.dot--amber { color: #f59e0b; background: transparent; }
    .timeline__dot.dot--blue { color: #3b82f6; background: #3b82f6; }
    .timeline__note { font-weight: 600; }
    .timeline__meta { font-size: 12px; color: var(--mat-sys-on-surface-variant); }
    .detail-actions { display: flex; gap: 8px; padding: 12px 16px; border-top: 1px solid var(--mat-sys-outline-variant); }
    .sig-empty-text { color: var(--mat-sys-on-surface-variant); }
  `],
})
export class IncidenciasListComponent implements OnInit {
  private readonly svc = inject(IncidenciasService);
  private readonly clientSvc = inject(ClientService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotifyService);

  protected readonly cols = ['cliente', 'incidencia', 'tipo', 'apertura', 'estado'];

  protected readonly items = signal<IncidenciaListItemDto[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(25);
  protected readonly loading = signal(true);
  protected readonly clientes = signal<ClientListItemDto[]>([]);
  protected readonly selected = signal<ClienteIncidenciaDto | null>(null);
  protected readonly selectedCliente = signal<string>('');

  protected readonly search = new FormControl<string>('', { nonNullable: true });
  protected clienteFilter: number | null = null;
  protected tipoFilter: string | null = null;
  protected estadoFilter: EstadoIncidencia | null = null;

  // Tipos distintos presentes en la página actual (el campo es texto libre).
  protected readonly tipos = computed(() => Array.from(new Set(this.items().map((i) => i.tipo))).sort());

  protected readonly canManage = computed(() => (this.auth.currentUser()?.roles ?? []).includes('Administrator'));

  ngOnInit(): void {
    this.clientSvc.list(1, 500).subscribe({
      next: (r) => this.clientes.set(r.items),
      error: () => this.clientes.set([]),
    });
    this.search.valueChanges.pipe(debounceTime(300), distinctUntilChanged()).subscribe(() => this.aplicar());
    this.load();
  }

  protected aplicar(): void {
    this.page.set(1);
    this.load();
  }

  protected limpiar(): void {
    this.search.setValue('', { emitEvent: false });
    this.clienteFilter = null;
    this.tipoFilter = null;
    this.estadoFilter = null;
    this.page.set(1);
    this.load();
  }

  protected onPage(e: PageEvent): void {
    this.pageSize.set(e.pageSize);
    this.page.set(e.pageIndex + 1);
    window.scrollTo({ top: 0, behavior: 'smooth' });
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.svc.list({
      page: this.page(), pageSize: this.pageSize(), search: this.search.value,
      clientId: this.clienteFilter, tipo: this.tipoFilter, estado: this.estadoFilter,
    }).subscribe({
      next: (r) => { this.items.set(r.items); this.total.set(r.total); this.loading.set(false); },
      error: () => { this.items.set([]); this.total.set(0); this.loading.set(false); },
    });
  }

  protected seleccionar(row: IncidenciaListItemDto): void {
    this.svc.getById(row.clientId, row.id).subscribe({
      next: (d) => { this.selected.set(d); this.selectedCliente.set(row.clientNombre); },
      error: () => this.notify.error('No se pudo cargar el detalle de la incidencia'),
    });
  }

  protected cerrarDetalle(): void { this.selected.set(null); }

  protected estadoLabel(e: EstadoIncidencia): string {
    return e === 'Abierta' ? 'Pendiente' : e === 'EnProceso' ? 'En proceso' : 'Resuelta';
  }
  protected estadoBadge(e: EstadoIncidencia): string {
    return e === 'Resuelta' ? 'sig-badge--green' : e === 'EnProceso' ? 'sig-badge--blue' : 'sig-badge--amber';
  }
  protected estadoDot(e: EstadoIncidencia): string {
    return e === 'Resuelta' ? 'dot--green' : e === 'EnProceso' ? 'dot--blue' : 'dot--amber';
  }

  protected nueva(): void {
    const data: IncidenciaFormData = { clientes: this.clientes() };
    this.dialog.open(IncidenciaFormDialog, { data, autoFocus: false }).afterClosed()
      .subscribe((res: IncidenciaFormResult | undefined) => {
        if (!res) return;
        this.svc.create(res.clientId, res.req).subscribe({
          next: () => { this.notify.success('Incidencia creada'); this.load(); },
          error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo crear la incidencia'),
        });
      });
  }

  protected actualizarEstado(inc: ClienteIncidenciaDto): void {
    this.abrirEstado(inc, undefined);
  }
  protected marcarResuelta(inc: ClienteIncidenciaDto): void {
    this.abrirEstado(inc, 'Resuelta');
  }

  private abrirEstado(inc: ClienteIncidenciaDto, propuesto?: EstadoIncidencia): void {
    const data: IncidenciaEstadoData = { estadoActual: inc.estado, estadoPropuesto: propuesto };
    this.dialog.open(IncidenciaEstadoDialog, { data, autoFocus: false }).afterClosed()
      .subscribe((req: IncidenciaCambioEstadoRequest | undefined) => {
        if (!req) return;
        this.svc.cambiarEstado(inc.clientId, inc.id, req).subscribe({
          next: (d) => { this.notify.success('Estado actualizado'); this.selected.set(d); this.load(); },
          error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo actualizar el estado'),
        });
      });
  }
}
