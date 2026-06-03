import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { ProjectService } from '../../core/api/projects.service';
import { ProjectListItemDto } from '../../models/dtos';

@Component({
  selector: 'app-projects-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, DatePipe, MatIconModule],
  template: `
    <div class="sig-list-page">

      <div class="sig-list-topbar">
        <h1 class="sig-page-title">
          <mat-icon>folder_open</mat-icon>
          Proyectos
          <span class="sig-total-chip">{{ total() }}</span>
        </h1>
        <div class="sig-topbar-actions">
          <button class="sig-btn-outline" (click)="exportCsv()">
            <mat-icon>download</mat-icon> Exportar CSV
          </button>
          <button class="sig-btn-primary" (click)="openNew()">
            <mat-icon>add</mat-icon> Nuevo Proyecto
          </button>
        </div>
      </div>

      <div class="sig-filter-section">
        <div class="sig-filter-bar">
          <div class="sig-search-wrap">
            <mat-icon>search</mat-icon>
            <input class="sig-search-input" [(ngModel)]="searchQ" placeholder="Buscar proyecto..." (input)="onFilter()"/>
          </div>
          <select class="sig-select" [(ngModel)]="filterCliente" (change)="onFilter()">
            <option value="">Cliente</option>
            <option *ngFor="let c of clientes" [value]="c">{{c}}</option>
          </select>
          <select class="sig-select" [(ngModel)]="filterEstado" (change)="onFilter()">
            <option value="">Estado</option>
            <option value="Activo">Activo</option>
            <option value="Revision">Revision</option>
            <option value="Inactivo">Inactivo</option>
          </select>
          <select class="sig-select" [(ngModel)]="filterCeco" (change)="onFilter()">
            <option value="">CECO</option>
            <option *ngFor="let c of cecos" [value]="c">{{c}}</option>
          </select>
          <button class="sig-btn-filter" (click)="onFilter()">Filtrar</button>
          <button class="sig-btn-limpiar" (click)="clearFilters()">Limpiar</button>
          <span class="sig-filter-right">Mostrando {{ filtered().length }} de {{ total() }} proyectos</span>
        </div>
      </div>

      <div class="sig-content-area">
        <div class="sig-table-wrap">
          <table>
            <thead>
              <tr>
                <th>ID</th>
                <th>PROYECTO</th>
                <th>CLIENTE</th>
                <th>ESTADO</th>
                <th>CECO(S)</th>
                <th style="text-align:right;">ACCIONES</th>
              </tr>
            </thead>
            <tbody>
              @for (p of filtered(); track p.id) {
                <tr (click)="selectRow(p)" [class.selected]="selected()?.id === p.id">
                  <td><span class="col-id">{{ p.id }}<br><span style="font-size:10px;color:var(--sig-text-muted)">PRP-{{ (p.id+'').padStart(3,'0') }}</span></span></td>
                  <td>
                    <div class="col-main">{{ p.nombre }}</div>
                    <div class="col-secondary">{{ '—' }} &middot; {{ '—' }}</div>
                  </td>
                  <td>{{ p.clientNombre }}</td>
                  <td>
                    <span class="sig-badge" [class]="estadoBadge(p.estado)">{{ p.estado || 'Activo' }}</span>
                  </td>
                  <td style="font-size:11px;color:var(--sig-text-muted)">{{ '—' }}</td>
                  <td>
                    <div class="sig-row-actions">
                      <button class="sig-icon-btn" title="Ver" (click)="$event.stopPropagation(); selectRow(p)"><mat-icon>visibility</mat-icon></button>
                      <button class="sig-icon-btn" title="Editar" (click)="$event.stopPropagation(); openEdit(p)"><mat-icon>edit</mat-icon></button>
                      <button class="sig-icon-btn danger" title="Eliminar" (click)="$event.stopPropagation(); deleteRow(p)"><mat-icon>delete</mat-icon></button>
                    </div>
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="6">
                  <div class="sig-empty">
                    <mat-icon>folder_off</mat-icon>
                    <span class="sig-empty-title">Sin proyectos</span>
                  </div>
                </td></tr>
              }
            </tbody>
          </table>
          <div class="sig-pagination">
            <span>{{ filtered().length }} proyectos</span>
            <button class="sig-page-btn" disabled>&#8249;</button>
            <div class="sig-page-current">1</div>
            <button class="sig-page-btn" disabled>&#8250;</button>
          </div>
        </div>

        @if (selected()) {
          <div class="sig-detail-panel">
            <div class="sig-detail-hdr">
              <span class="sig-detail-hdr-title">
                <mat-icon>folder_open</mat-icon>
                Detalle &mdash; {{ selected()!.nombre }}
              </span>
              <button class="sig-detail-close" (click)="selected.set(null)">&times;</button>
            </div>
            <div class="sig-detail-body">
              <div class="sig-detail-grid">
                <div class="sig-detail-field">
                  <label>ID Proyecto</label>
                  <span>PRP-{{ (selected()!.id+'').padStart(3,'0') }} &middot; {{ selected()!.id }}</span>
                </div>
                <div class="sig-detail-field">
                  <label>Estado</label>
                  <span class="sig-badge" [class]="estadoBadge(selected()!.estado)">{{ selected()!.estado || 'Activo' }}</span>
                </div>
                <div class="sig-detail-field">
                  <label>Cliente</label>
                  <span>{{ selected()!.clientNombre }}</span>
                </div>
                <div class="sig-detail-field">
                  <label>CECO(S)</label>
                  <span>{{ '—' }}</span>
                </div>
                <div class="sig-detail-field" style="grid-column:1/-1">
                  <label>Interlocutor</label>
                  <span>{{ '—' }}</span>
                </div>
                <div class="sig-detail-field">
                  <label>Departamento</label>
                  <span>{{ 'Sales Training' }}</span>
                </div>
                <div class="sig-detail-field">
                  <label>Fecha Alta</label>
                  <span>{{ selected()!.fechaAlta | date:'dd/MM/yyyy' }}</span>
                </div>
              </div>

              <div class="sig-detail-section">
                <div class="sig-detail-section-title">
                  Usuarios Asignados
                  <span>{{ 2 }}</span>
                </div>
                <div class="sig-avatar-chip">
                  <div class="sig-avatar" style="background:#2563eb">SG</div>
                  <div class="sig-avatar-info">
                    <span class="sig-avatar-name">{{ 'Silvia Garzon' }}</span>
                    <span class="sig-avatar-role">Interlocutor</span>
                  </div>
                </div>
              </div>

              <div class="sig-detail-section">
                <div class="sig-detail-section-title">
                  Acciones Asociadas
                  <span style="color:var(--sig-blue);font-size:12px;cursor:pointer;">+ Nuevo</span>
                </div>
                <div style="font-size:12px;color:var(--sig-text-muted);padding:4px 0;">
                  {{ selected()!.nombre }}
                </div>
              </div>
            </div>
            <div class="sig-detail-footer">
              <button class="sig-btn-edit" (click)="openEdit(selected()!)">
                <mat-icon>edit</mat-icon> Editar
              </button>
              <button class="sig-btn-dup">
                <mat-icon>content_copy</mat-icon> Duplicar
              </button>
              <button class="sig-btn-del"><mat-icon>delete</mat-icon></button>
            </div>
          </div>
        }
      </div>
    </div>
`,
  styles: [`
    :host { display: block; }
    .sig-list-page { display: flex; flex-direction: column; height: 100%; padding: 0; }

    /* Top bar */
    .sig-list-topbar {
      display: flex; align-items: center; justify-content: space-between;
      padding: 20px 24px 0; margin-bottom: 16px;
    }
    .sig-page-title {
      font-size: 20px; font-weight: 700; color: var(--sig-text-heading); margin: 0;
      display: flex; align-items: center; gap: 10px;
    }
    .sig-page-title mat-icon { color: var(--sig-text-muted); font-size: 20px; width: 20px; height: 20px; }
    .sig-total-chip {
      font-size: 11px; font-weight: 700; padding: 2px 8px; border-radius: 10px;
      background: var(--sig-bg-active); color: var(--sig-teal); border: 1px solid rgba(0,212,196,.2);
    }
    .sig-topbar-actions { display: flex; align-items: center; gap: 8px; }
    .sig-btn-primary {
      display: inline-flex; align-items: center; gap: 6px;
      padding: 0 16px; height: 36px; border-radius: 8px;
      background: var(--sig-blue); color: #fff; border: none;
      font-size: 13px; font-weight: 600; font-family: inherit; cursor: pointer;
      transition: background 150ms;
      &:hover { background: var(--sig-blue-light); }
      mat-icon { font-size: 16px !important; width: 16px !important; height: 16px !important; }
    }
    .sig-btn-outline {
      display: inline-flex; align-items: center; gap: 6px;
      padding: 0 14px; height: 34px; border-radius: 8px;
      background: transparent; color: var(--sig-blue);
      border: 1px solid var(--sig-border); font-size: 12px; font-family: inherit; cursor: pointer;
      transition: background 150ms;
      &:hover { background: var(--sig-bg-hover); }
      mat-icon { font-size: 15px !important; width: 15px !important; height: 15px !important; }
    }

    /* Filter bar */
    .sig-filter-section { padding: 0 24px 12px; }
    .sig-filter-bar {
      display: flex; align-items: center; gap: 10px; flex-wrap: wrap;
    }
    .sig-search-wrap {
      display: flex; align-items: center; gap: 8px;
      background: var(--sig-bg-card); border: 1px solid var(--sig-border);
      border-radius: 8px; padding: 0 12px; height: 36px; flex: 0 0 220px;
      &:focus-within { border-color: var(--sig-blue); }
      mat-icon { font-size: 16px !important; width: 16px !important; height: 16px !important; color: var(--sig-text-muted) !important; }
    }
    .sig-search-input {
      background: transparent; border: none; outline: none;
      font-size: 13px; color: var(--sig-text-primary); width: 100%;
      &::placeholder { color: var(--sig-text-muted); }
    }
    .sig-select {
      height: 36px; background: var(--sig-bg-card); border: 1px solid var(--sig-border);
      border-radius: 8px; padding: 0 10px; font-size: 13px; color: var(--sig-text-primary);
      font-family: inherit; cursor: pointer; outline: none;
      &:focus { border-color: var(--sig-blue); }
    }
    .sig-btn-filter {
      height: 36px; padding: 0 16px; border-radius: 8px;
      background: var(--sig-blue); color: #fff; border: none;
      font-size: 13px; font-weight: 600; font-family: inherit; cursor: pointer;
      &:hover { background: var(--sig-blue-light); }
    }
    .sig-btn-limpiar {
      height: 36px; padding: 0 14px; border-radius: 8px;
      background: transparent; color: var(--sig-text-muted);
      border: 1px solid var(--sig-border); font-size: 13px; font-family: inherit; cursor: pointer;
      &:hover { background: var(--sig-bg-hover); }
    }
    .sig-filter-right { margin-left: auto; font-size: 12px; color: var(--sig-text-muted); }

    /* Content area with optional side panel */
    .sig-content-area {
      flex: 1; display: flex; overflow: hidden; padding: 0 24px 24px;
      gap: 16px;
    }

    /* Table */
    .sig-table-wrap {
      flex: 1; overflow: auto; background: var(--sig-bg-card);
      border: 1px solid var(--sig-border); border-radius: 12px; min-width: 0;
    }
    table { width: 100%; border-collapse: collapse; }
    thead tr { background: var(--sig-bg-header); border-bottom: 1px solid var(--sig-border); }
    th {
      padding: 11px 16px; font-size: 10px; font-weight: 700;
      letter-spacing: .08em; text-transform: uppercase;
      color: var(--sig-text-muted); text-align: left; white-space: nowrap;
    }
    td {
      padding: 12px 16px; font-size: 13px; color: var(--sig-text-primary);
      border-bottom: 1px solid var(--sig-border); vertical-align: middle;
    }
    tbody tr {
      cursor: pointer; transition: background 120ms;
      &:hover { background: var(--sig-bg-hover); }
      &:last-child td { border-bottom: none; }
      &.selected { background: rgba(0,212,196,.06) !important; }
    }
    .col-id { color: var(--sig-blue); font-weight: 700; font-family: 'Roboto Mono',monospace; font-size: 12px; }
    .col-secondary { font-size: 11px; color: var(--sig-text-muted); margin-top: 2px; }
    .col-main { font-weight: 600; }

    /* Status badges */
    .sig-badge {
      display: inline-flex; align-items: center; gap: 5px;
      padding: 2px 10px; border-radius: 20px; font-size: 11px; font-weight: 600;
    }
    .sig-badge::before { content: ''; width: 6px; height: 6px; border-radius: 50%; background: currentColor; }
    .sig-badge--green  { color: #22c55e; background: rgba(34,197,94,.12); }
    .sig-badge--yellow { color: #f59e0b; background: rgba(245,158,11,.12); }
    .sig-badge--red    { color: #ef4444; background: rgba(239,68,68,.12); }
    .sig-badge--blue   { color: #3b82f6; background: rgba(59,130,246,.12); }
    .sig-badge--gray   { color: #94a3b8; background: rgba(148,163,184,.1); }

    /* Action buttons in table */
    .sig-row-actions { display: flex; align-items: center; gap: 4px; justify-content: flex-end; }
    .sig-icon-btn {
      width: 30px; height: 30px; border-radius: 6px; border: none;
      background: transparent; cursor: pointer; display: flex; align-items: center; justify-content: center;
      color: var(--sig-text-muted); transition: background 120ms, color 120ms;
      mat-icon { font-size: 16px !important; width: 16px !important; height: 16px !important; }
      &:hover { background: var(--sig-bg-hover); color: var(--sig-text-primary); }
      &.danger:hover { background: rgba(239,68,68,.1); color: #ef4444; }
    }

    /* Detail panel */
    .sig-detail-panel {
      width: 320px; flex-shrink: 0;
      background: var(--sig-bg-card); border: 1px solid var(--sig-border);
      border-radius: 12px; overflow: hidden; display: flex; flex-direction: column;
    }
    .sig-detail-hdr {
      display: flex; align-items: center; justify-content: space-between;
      padding: 12px 16px; background: var(--sig-blue); color: #fff; gap: 8px;
    }
    .sig-detail-hdr-title {
      font-size: 13px; font-weight: 600; display: flex; align-items: center; gap: 6px;
      mat-icon { font-size: 15px !important; width: 15px !important; height: 15px !important; }
    }
    .sig-detail-close {
      width: 24px; height: 24px; border-radius: 4px; border: none;
      background: rgba(255,255,255,.2); color: #fff; cursor: pointer; font-size: 16px;
      display: flex; align-items: center; justify-content: center;
      &:hover { background: rgba(255,255,255,.3); }
    }
    .sig-detail-body { flex: 1; overflow-y: auto; padding: 16px; }
    .sig-detail-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
    .sig-detail-field label { font-size: 10px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; color: var(--sig-text-muted); display: block; margin-bottom: 3px; }
    .sig-detail-field span  { font-size: 13px; color: var(--sig-text-primary); }
    .sig-detail-section { margin-top: 16px; border-top: 1px solid var(--sig-border); padding-top: 12px; }
    .sig-detail-section-title { font-size: 10px; font-weight: 700; letter-spacing: .1em; text-transform: uppercase; color: var(--sig-text-muted); margin-bottom: 10px; display: flex; align-items: center; justify-content: space-between; }
    .sig-detail-footer {
      padding: 12px 16px; border-top: 1px solid var(--sig-border);
      display: flex; gap: 8px;
    }
    .sig-btn-edit {
      flex: 1; height: 34px; border-radius: 8px; border: none;
      background: var(--sig-blue); color: #fff; font-size: 13px; font-weight: 600; font-family: inherit; cursor: pointer;
      display: flex; align-items: center; justify-content: center; gap: 6px;
      mat-icon { font-size: 15px !important; width: 15px !important; height: 15px !important; }
    }
    .sig-btn-dup {
      flex: 1; height: 34px; border-radius: 8px; border: 1px solid var(--sig-border);
      background: transparent; color: var(--sig-text-primary); font-size: 13px; font-family: inherit; cursor: pointer;
      display: flex; align-items: center; justify-content: center; gap: 6px;
      mat-icon { font-size: 15px !important; width: 15px !important; height: 15px !important; }
    }
    .sig-btn-del {
      width: 34px; height: 34px; border-radius: 8px; border: none;
      background: rgba(239,68,68,.1); color: #ef4444; cursor: pointer;
      display: flex; align-items: center; justify-content: center;
      mat-icon { font-size: 16px !important; width: 16px !important; height: 16px !important; }
    }
    .sig-avatar-chip {
      display: flex; align-items: center; gap: 8px; padding: 6px 0;
    }
    .sig-avatar {
      width: 28px; height: 28px; border-radius: 50%; font-size: 11px; font-weight: 700;
      display: flex; align-items: center; justify-content: center; flex-shrink: 0; color: #fff;
    }
    .sig-avatar-info { display: flex; flex-direction: column; flex: 1; }
    .sig-avatar-name { font-size: 12px; font-weight: 600; color: var(--sig-text-primary); }
    .sig-avatar-role { font-size: 10px; color: var(--sig-text-muted); }
    .sig-quitar-btn { font-size: 11px; color: var(--sig-text-muted); background: none; border: none; cursor: pointer; &:hover { color: #ef4444; } }

    /* Empty state */
    .sig-empty { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 8px; padding: 60px 24px; color: var(--sig-text-muted); }
    .sig-empty mat-icon { font-size: 40px !important; width: 40px !important; height: 40px !important; opacity: .4; }
    .sig-empty-title { font-size: 15px; font-weight: 600; color: var(--sig-text-secondary); }
    .sig-empty-sub { font-size: 13px; }

    /* Pagination */
    .sig-pagination {
      display: flex; align-items: center; justify-content: flex-end; gap: 8px;
      padding: 10px 16px; border-top: 1px solid var(--sig-border);
      font-size: 12px; color: var(--sig-text-muted);
    }
    .sig-page-btn {
      width: 28px; height: 28px; border-radius: 6px; border: 1px solid var(--sig-border);
      background: transparent; color: var(--sig-text-muted); cursor: pointer; font-size: 14px;
      display: flex; align-items: center; justify-content: center;
      &:hover:not(:disabled) { background: var(--sig-bg-hover); color: var(--sig-text-primary); }
      &:disabled { opacity: .35; cursor: not-allowed; }
    }
    .sig-page-current {
      width: 28px; height: 28px; border-radius: 6px;
      background: var(--sig-blue); color: #fff; font-size: 13px; font-weight: 600;
      display: flex; align-items: center; justify-content: center;
    }
`],
})
export class ProjectsListComponent implements OnInit {
  private readonly svc = inject(ProjectService);
  protected readonly projects = signal<ProjectListItemDto[]>([]);
  protected readonly selected  = signal<ProjectListItemDto | null>(null);
  protected searchQ      = '';
  protected filterCliente = '';
  protected filterEstado  = '';
  protected filterCeco    = '';
  protected clientes: string[] = [];
  protected cecos:    string[] = [];
  protected readonly total    = computed(() => this.projects().length);
  protected readonly filtered = computed(() => {
    let list = this.projects();
    if (this.searchQ) list = list.filter(p => p.nombre.toLowerCase().includes(this.searchQ.toLowerCase()));
    if (this.filterCliente) list = list.filter(p => p.clientNombre === this.filterCliente);
    if (this.filterEstado)  list = list.filter(p => (p.estado || 'Activo') === this.filterEstado);
    return list;
  });
  ngOnInit(): void {
    this.svc.list(1, 100).subscribe({
      next: (res: any) => {
        const d: ProjectListItemDto[] = res?.items ?? res ?? [];
        this.projects.set(d);
        if (!this.selected() && d.length) this.selected.set(res?.items?.[0] ?? d[0]);
        this.clientes = [...new Set(d.map((p: ProjectListItemDto) => p.clientNombre).filter(Boolean))] as string[];
      },
      error: () => this.projects.set([]),
    });
  }
  protected selectRow(p: ProjectListItemDto): void { this.selected.set(p); }
  protected onFilter(): void { }
  protected clearFilters(): void { this.searchQ = ''; this.filterCliente = ''; this.filterEstado = ''; this.filterCeco = ''; }
  protected openNew():   void { }
  protected openEdit(p: ProjectListItemDto): void { }
  protected deleteRow(p: ProjectListItemDto): void { }
  protected exportCsv(): void { }
  protected estadoBadge(estado?: string): string {
    switch (estado) {
      case 'Activo':   return 'sig-badge--green';
      case 'Revision': return 'sig-badge--yellow';
      case 'Inactivo': return 'sig-badge--red';
      default:         return 'sig-badge--green';
    }
  }
}
