import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatTableModule } from '@angular/material/table';
import { MatSortModule } from '@angular/material/sort';
import { MatTabsModule } from '@angular/material/tabs';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { forkJoin, of } from 'rxjs';
import { map, mergeMap } from 'rxjs/operators';
import { CierresService } from '../../core/api/cierres.service';
import { NotifyService } from '../../core/notify.service';
import { ApprovalFilterRequest, CierreListItemDto } from '../../models/dtos';
import { TipoCierre } from '../../models/enums';

interface AlertaResumida {
  id: number;
  tipo: string;
  codigo: string;
  descripcion: string;
  confirmada: boolean;
  tipoCierre: TipoCierre;
  closureId: number;
  closureNombre: string;
}

@Component({
  selector: 'app-alerts-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, MatIconModule, MatButtonModule, MatChipsModule, MatTableModule, MatSortModule, MatTabsModule, MatExpansionModule, MatFormFieldModule, MatSelectModule, MatInputModule],
  template: `
    <div class="sig-exec-page">
      <div class="sig-exec-header">
        <div class="sig-exec-titles">
          <h1 class="sig-exec-title">Alertas de desviación</h1>
          <p class="sig-exec-sub">Errores de nómina y facturación — bloqueantes y advertencias (PPT slides 40/42)</p>
        </div>
      </div>

      <!-- Summary stats -->
      <div class="sig-summary-grid">
        <div class="sig-summary-card">
          <div class="sig-summary-value">{{ totalAlertas() }}</div>
          <div class="sig-summary-label">Total</div>
        </div>
        <div class="sig-summary-card sig-summary-card--bloqueante">
          <div class="sig-summary-value">{{ bloqueantes() }}</div>
          <div class="sig-summary-label">Bloqueantes</div>
        </div>
        <div class="sig-summary-card sig-summary-card--advertencia">
          <div class="sig-summary-value">{{ advertencias() }}</div>
          <div class="sig-summary-label">Advertencias</div>
        </div>
        <div class="sig-summary-card sig-summary-card--confirmada">
          <div class="sig-summary-value">{{ confirmadas() }}</div>
          <div class="sig-summary-label">Confirmadas</div>
        </div>
      </div>

      <!-- Filtros (slide 40: mantener los filtros) -->
      <div class="filtros">
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Tipo</mat-label>
          <mat-select [ngModel]="fTipo()" (ngModelChange)="fTipo.set($event)" data-testid="f-tipo">
            <mat-option value="">Todas</mat-option>
            <mat-option value="Bloqueante">Bloqueantes</mat-option>
            <mat-option value="Advertencia">Advertencias</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Origen</mat-label>
          <mat-select [ngModel]="fTipoCierre()" (ngModelChange)="fTipoCierre.set($event)" data-testid="f-tipocierre">
            <mat-option value="">Todos</mat-option>
            <mat-option value="Costes">Nómina (costes)</mat-option>
            <mat-option value="Facturacion">Facturación</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic">
          <mat-label>Estado</mat-label>
          <mat-select [ngModel]="fEstado()" (ngModelChange)="fEstado.set($event)" data-testid="f-estado">
            <mat-option value="">Todas</mat-option>
            <mat-option value="pendiente">Pendientes</mat-option>
            <mat-option value="confirmada">Confirmadas</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" subscriptSizing="dynamic" class="search">
          <mat-label>Buscar</mat-label>
          <input matInput [ngModel]="fTexto()" (ngModelChange)="fTexto.set($event)" placeholder="código o descripción" data-testid="f-texto" />
        </mat-form-field>
      </div>

      @if (loading()) {
        <div class="sig-table-skeleton">
          @for (_ of [0,1,2,3,4]; track _) {
            <div class="sig-skeleton" style="height:40px;width:100%;border-radius:4px;margin-bottom:8px;"></div>
          }
        </div>
      } @else if (filtered().length === 0) {
        <div class="sig-empty-state">
          <mat-icon style="font-size:48px;width:48px;height:48px;color:var(--sig-success);">check_circle</mat-icon>
          <p>No hay alertas para los filtros seleccionados</p>
        </div>
      } @else {
        <mat-tab-group>
          <!-- Por bloques (slide 40): agrupadas por código para resolverlas en grupo -->
          <mat-tab label="Por bloques">
            <div class="bloques">
              @for (b of bloques(); track b.codigo) {
                <mat-expansion-panel data-testid="bloque">
                  <mat-expansion-panel-header>
                    <mat-panel-title><code>{{ b.codigo }}</code></mat-panel-title>
                    <mat-panel-description>
                      <span class="bloque-desc">{{ b.descripcion }}</span>
                      <span class="bloque-badges">
                        <span class="badge">{{ b.items.length }}</span>
                        @if (b.bloqueantes > 0) { <span class="badge badge--bloq">{{ b.bloqueantes }} bloq.</span> }
                        @if (b.pendientes > 0) { <span class="badge badge--pend">{{ b.pendientes }} pdte.</span> }
                      </span>
                    </mat-panel-description>
                  </mat-expansion-panel-header>
                  <table class="bloque-table">
                    <thead><tr><th>Tipo</th><th>Origen</th><th>Cierre</th><th>Estado</th></tr></thead>
                    <tbody>
                      @for (a of b.items; track a.id) {
                        <tr>
                          <td><span [class]="'sig-chip--' + a.tipo.toLowerCase()" class="mini-chip">{{ a.tipo }}</span></td>
                          <td>{{ a.tipoCierre === 'Costes' ? 'Nómina' : 'Facturación' }}</td>
                          <td><a [routerLink]="[a.tipoCierre === 'Costes' ? '/cierres-costes' : '/cierres-facturacion', a.closureId]" class="sig-cell-link">{{ a.closureNombre }}</a></td>
                          <td>{{ a.confirmada ? 'Confirmada' : 'Pendiente' }}</td>
                        </tr>
                      }
                    </tbody>
                  </table>
                </mat-expansion-panel>
              }
            </div>
          </mat-tab>

          <!-- Listado plano -->
          <mat-tab label="Listado">
            <table mat-table [dataSource]="filtered()" matSort class="sig-mat-table" style="margin-top:12px;">
              <ng-container matColumnDef="tipo">
                <th mat-header-cell *matHeaderCellDef mat-sort-header> Tipo </th>
                <td mat-cell *matCellDef="let a">
                  <mat-chip [class]="'sig-chip--' + a.tipo.toLowerCase()">
                    <mat-icon>{{ a.tipo === 'Bloqueante' ? 'block' : 'warning' }}</mat-icon>
                    {{ a.tipo }}
                  </mat-chip>
                </td>
              </ng-container>
              <ng-container matColumnDef="codigo">
                <th mat-header-cell *matHeaderCellDef mat-sort-header> Código </th>
                <td mat-cell *matCellDef="let a"><code>{{ a.codigo }}</code></td>
              </ng-container>
              <ng-container matColumnDef="descripcion">
                <th mat-header-cell *matHeaderCellDef mat-sort-header> Descripción </th>
                <td mat-cell *matCellDef="let a">{{ a.descripcion }}</td>
              </ng-container>
              <ng-container matColumnDef="tipoCierre">
                <th mat-header-cell *matHeaderCellDef mat-sort-header> Origen </th>
                <td mat-cell *matCellDef="let a">{{ a.tipoCierre === 'Costes' ? 'Nómina' : 'Facturación' }}</td>
              </ng-container>
              <ng-container matColumnDef="closureNombre">
                <th mat-header-cell *matHeaderCellDef mat-sort-header> Cierre </th>
                <td mat-cell *matCellDef="let a">
                  <a [routerLink]="[a.tipoCierre === 'Costes' ? '/cierres-costes' : '/cierres-facturacion', a.closureId]" class="sig-cell-link">{{ a.closureNombre }}</a>
                </td>
              </ng-container>
              <ng-container matColumnDef="confirmada">
                <th mat-header-cell *matHeaderCellDef mat-sort-header> Estado </th>
                <td mat-cell *matCellDef="let a">
                  @if (a.confirmada) {
                    <mat-chip class="sig-chip--confirmada"><mat-icon>check</mat-icon> Confirmada</mat-chip>
                  } @else {
                    <mat-chip class="sig-chip--pendiente"><mat-icon>pending</mat-icon> Pendiente</mat-chip>
                  }
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: displayedColumns;" class="sig-data-row"></tr>
            </table>
          </mat-tab>
        </mat-tab-group>
      }
    </div>
  `,
  styles: [`
    :host { display: block; }
    .sig-exec-page { padding: 28px 28px 40px; background: var(--sig-bg-app); min-height: 100vh; }
    .sig-exec-header { margin-bottom: 28px; }
    .sig-exec-title { font-size: 24px; font-weight: 700; color: var(--sig-text-heading); margin: 0 0 4px; }
    .sig-exec-sub { font-size: 13px; color: var(--sig-text-muted); margin: 0; }

    .sig-summary-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px; margin-bottom: 24px; }
    @media (max-width: 900px) { .sig-summary-grid { grid-template-columns: repeat(2, 1fr); } }
    .sig-summary-card {
      background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 12px;
      padding: 20px; text-align: center;
    }
    .sig-summary-value { font-size: 32px; font-weight: 700; color: var(--sig-text-heading); font-family: 'Roboto Mono', monospace; }
    .sig-summary-label { font-size: 12px; color: var(--sig-text-muted); text-transform: uppercase; letter-spacing: .05em; margin-top: 4px; }
    .sig-summary-card--bloqueante .sig-summary-value { color: #ef4444; }
    .sig-summary-card--advertencia .sig-summary-value { color: #f59e0b; }
    .sig-summary-card--confirmada .sig-summary-value { color: #22c55e; }

    .sig-mat-table {
      width: 100%; background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 12px; overflow: hidden;
      border-collapse: collapse;
      th.mat-mdc-header-cell { color: var(--sig-text-muted); font-size: 11px; font-weight: 700; letter-spacing: .05em; text-transform: uppercase; border-bottom-color: var(--sig-border); padding: 12px; }
      td.mat-mdc-cell { color: var(--sig-text-primary); font-size: 13px; border-bottom-color: var(--sig-border); padding: 12px; }
      td.mat-mdc-cell code { background: var(--sig-bg-hover); padding: 2px 6px; border-radius: 4px; font-size: 12px; }
    }
    .sig-data-row { cursor: pointer; transition: background 150ms; &:hover { background: var(--sig-bg-hover); } }
    .sig-cell-link { color: #3b82f6; text-decoration: none; font-weight: 500; &:hover { text-decoration: underline; } }

    .sig-chip--bloqueante { background: rgba(239,68,68,.1); color: #ef4444 !important; border: 1px solid rgba(239,68,68,.2); }
    .sig-chip--advertencia { background: rgba(245,158,11,.1); color: #f59e0b !important; border: 1px solid rgba(245,158,11,.2); }
    .sig-chip--confirmada { background: rgba(34,197,94,.1); color: #22c55e !important; border: 1px solid rgba(34,197,94,.2); }
    .sig-chip--pendiente { background: rgba(245,158,11,.1); color: #f59e0b !important; border: 1px solid rgba(245,158,11,.2); }

    .filtros { display: flex; flex-wrap: wrap; gap: 12px; margin-bottom: 16px; }
    .filtros mat-form-field { width: 200px; }
    .filtros .search { width: 240px; }
    .bloques { padding-top: 12px; display: flex; flex-direction: column; gap: 8px; }
    .bloque-desc { color: var(--sig-text-muted); margin-right: 12px; }
    .bloque-badges { display: inline-flex; gap: 6px; align-items: center; }
    .badge { background: var(--sig-bg-hover); border-radius: 10px; padding: 1px 8px; font-size: 11px; font-weight: 600; }
    .badge--bloq { background: rgba(239,68,68,.12); color: #ef4444; }
    .badge--pend { background: rgba(245,158,11,.12); color: #f59e0b; }
    .bloque-table { width: 100%; border-collapse: collapse; font-size: 13px; }
    .bloque-table th, .bloque-table td { text-align: left; padding: 6px 10px; border-bottom: 1px solid var(--sig-border); }
    .mini-chip { padding: 1px 8px; border-radius: 10px; font-size: 11px; font-weight: 600; }
    .sig-empty-state { text-align: center; padding: 60px 20px; color: var(--sig-text-muted); }
    .sig-table-skeleton { padding: 12px; }
    .sig-skeleton { background: var(--sig-border); animation: sig-shimmer 1.4s infinite; }
    @keyframes sig-shimmer { 0% { opacity: .4; } 50% { opacity: .8; } 100% { opacity: .4; } }
  `],
})
export class AlertsListComponent implements OnInit {
  private readonly cierresSvc = inject(CierresService);
  private readonly notify = inject(NotifyService);

  protected readonly alertas = signal<AlertaResumida[]>([]);
  protected readonly loading = signal(true);
  protected readonly displayedColumns = ['tipo', 'codigo', 'descripcion', 'tipoCierre', 'closureNombre', 'confirmada'];

  // Filtros (slide 40). Signals para que `filtered`/`bloques` reaccionen.
  protected readonly fTipo = signal('');
  protected readonly fTipoCierre = signal('');
  protected readonly fEstado = signal('');
  protected readonly fTexto = signal('');

  protected readonly filtered = computed(() => {
    const tipo = this.fTipo(), tc = this.fTipoCierre(), estado = this.fEstado(), txt = this.fTexto().trim().toLowerCase();
    return this.alertas().filter(a =>
      (!tipo || a.tipo === tipo)
      && (!tc || a.tipoCierre === tc)
      && (!estado || (estado === 'confirmada' ? a.confirmada : !a.confirmada))
      && (!txt || a.codigo.toLowerCase().includes(txt) || a.descripcion.toLowerCase().includes(txt)));
  });

  // Agrupadas por código para resolver "de manera grupal" (slide 40).
  protected readonly bloques = computed(() => {
    const map = new Map<string, { codigo: string; descripcion: string; items: AlertaResumida[]; bloqueantes: number; pendientes: number }>();
    for (const a of this.filtered()) {
      let b = map.get(a.codigo);
      if (!b) { b = { codigo: a.codigo, descripcion: a.descripcion, items: [], bloqueantes: 0, pendientes: 0 }; map.set(a.codigo, b); }
      b.items.push(a);
      if (a.tipo === 'Bloqueante') b.bloqueantes++;
      if (!a.confirmada) b.pendientes++;
    }
    return [...map.values()].sort((x, y) => y.items.length - x.items.length);
  });

  protected readonly totalAlertas = computed(() => this.alertas().length);
  protected readonly bloqueantes = computed(() => this.alertas().filter(a => a.tipo === 'Bloqueante').length);
  protected readonly advertencias = computed(() => this.alertas().filter(a => a.tipo === 'Advertencia').length);
  protected readonly confirmadas = computed(() => this.alertas().filter(a => a.confirmada).length);

  ngOnInit(): void {
    // Ola 3b (#10): no existe un endpoint global de alertas. Se agregan al vuelo
    // recorriendo ambos tipos de cierre y consultando sus alertas (api/cierres-*/{id}/alertas).
    const filter: ApprovalFilterRequest = { page: 1, pageSize: 500 };
    forkJoin({
      costes: this.cierresSvc.list('Costes', filter),
      facturacion: this.cierresSvc.list('Facturacion', filter),
    }).pipe(
      mergeMap((listas) => {
        const todos: CierreListItemDto[] = [...listas.costes.items, ...listas.facturacion.items];
        if (todos.length === 0) return of([] as AlertaResumida[]);
        const calls = todos.map((c) =>
          this.cierresSvc.getAlertas(c.tipoCierre, c.id).pipe(
            map((alertas) => alertas.map((a) => ({
              id: a.id,
              tipo: a.tipo,
              codigo: a.codigo,
              descripcion: a.descripcion,
              confirmada: a.confirmada,
              tipoCierre: c.tipoCierre,
              closureId: c.id,
              closureNombre: `${c.serviceNombre} — ${c.periodNombre}`,
            } as AlertaResumida)))
          )
        );
        return forkJoin(calls).pipe(map((arrs) => arrs.flat()));
      })
    ).subscribe({
      next: (data) => { this.alertas.set(data); this.loading.set(false); },
      error: () => { this.notify.error('No se pudieron cargar las alertas'); this.loading.set(false); },
    });
  }
}
