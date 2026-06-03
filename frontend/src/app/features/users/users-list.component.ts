import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { UserService } from '../../core/api/users.service';
import { UserListItemDto } from '../../models/dtos';

@Component({
  selector: 'app-users-list',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule],
  template: `
    <div class="sig-list-page">
      <div class="sig-list-topbar">
        <h1 class="sig-page-title">
          <mat-icon>people</mat-icon>
          Usuarios
          <span class="sig-total-chip">{{ total() }}</span>
        </h1>
        <div class="sig-topbar-actions">
          <button class="sig-btn-outline"><mat-icon>download</mat-icon> Exportar CSV</button>
          <button class="sig-btn-primary" (click)="openNew()"><mat-icon>add</mat-icon> Nuevo Usuario</button>
        </div>
      </div>

      <div class="sig-filter-section">
        <div class="sig-filter-bar">
          <div class="sig-search-wrap">
            <mat-icon>search</mat-icon>
            <input class="sig-search-input" [(ngModel)]="searchQ" placeholder="Nombre, apellidos o NIF..." (input)="onFilter()"/>
          </div>
          <select class="sig-select" [(ngModel)]="filterDepto" (change)="onFilter()">
            <option value="">Departamento</option>
            <option>Sales Training</option>
            <option>Operations</option>
            <option>Finance</option>
          </select>
          <select class="sig-select" [(ngModel)]="filterRol" (change)="onFilter()">
            <option value="">Rol</option>
            <option>Administrator</option>
            <option>Direction</option>
            <option>Fico</option>
            <option>Backoffice</option>
            <option>ProjectManager</option>
            <option>Interlocutor</option>
          </select>
          <select class="sig-select" [(ngModel)]="filterEstado" (change)="onFilter()">
            <option value="">Estado</option>
            <option>Activo</option>
            <option>Inactivo</option>
          </select>
          <button class="sig-btn-filter" (click)="onFilter()">Filtrar</button>
          <button class="sig-btn-limpiar" (click)="clearFilters()">Limpiar</button>
          <span class="sig-filter-right">{{ filtered().length }} usuarios</span>
        </div>
      </div>

      <div class="sig-content-area">
        <div class="sig-table-wrap">
          <table>
            <thead>
              <tr>
                <th>NIF</th>
                <th>NOMBRE</th>
                <th>MAIL</th>
                <th>ROL</th>
                <th>ESTADO</th>
                <th style="text-align:right">ACCIONES</th>
              </tr>
            </thead>
            <tbody>
              @for (u of filtered(); track u.id) {
                <tr (click)="selectRow(u)" [class.selected]="selected()?.id === u.id">
                  <td style="font-family:'Roboto Mono',monospace;font-size:11px;color:var(--sig-text-muted)">{{ u.nif || '—' }}</td>
                  <td style="font-weight:600">{{ u.nombre }} {{ u.apellidos }}</td>
                  <td style="font-size:12px;color:var(--sig-text-secondary)">{{ u.email }}</td>
                  <td><span class="sig-role-chip">{{ u.roles[0] || '—' }}</span></td>
                  <td><span class="sig-badge sig-badge--green">Activo</span></td>
                  <td>
                    <div class="sig-row-actions">
                      <button class="sig-icon-btn" title="Ver" (click)="$event.stopPropagation(); selectRow(u)"><mat-icon>visibility</mat-icon></button>
                      <button class="sig-icon-btn" title="Editar"><mat-icon>edit</mat-icon></button>
                      <button class="sig-icon-btn danger" title="Eliminar"><mat-icon>delete</mat-icon></button>
                    </div>
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="6"><div class="sig-empty"><mat-icon>people</mat-icon><span class="sig-empty-title">Sin usuarios</span></div></td></tr>
              }
            </tbody>
          </table>
          <div class="sig-pagination">
            <span>{{ filtered().length }} usuarios</span>
            <button class="sig-page-btn" disabled>&#8249;</button>
            <div class="sig-page-current">1</div>
            <button class="sig-page-btn" disabled>&#8250;</button>
          </div>
        </div>

        @if (selected()) {
          <div class="sig-detail-panel">
            <div class="sig-detail-hdr">
              <span class="sig-detail-hdr-title">
                <mat-icon>people</mat-icon>
                Detalle &mdash; {{ selected()!.nombre }} {{ selected()!.apellidos }}
              </span>
              <button class="sig-detail-close" (click)="selected.set(null)">&times;</button>
            </div>
            <div class="sig-detail-body">
              <div class="sig-user-avatar-large">
                <div class="sig-avatar-xl" [style.background]="avatarColor(selected()!)">
                  {{ userInitials(selected()!) }}
                </div>
                <div>
                  <div style="font-size:15px;font-weight:700;color:var(--sig-text-heading)">{{ selected()!.nombre }} {{ selected()!.apellidos }}</div>
                  <div style="font-size:12px;color:var(--sig-text-muted)">{{ selected()!.email }}</div>
                </div>
              </div>
              <div class="sig-detail-grid" style="margin-top:16px">
                <div class="sig-detail-field">
                  <label>NIF</label><span>{{ selected()!.nif || '—' }}</span>
                </div>
                <div class="sig-detail-field">
                  <label>Estado</label>
                  <span class="sig-badge sig-badge--green">Activo</span>
                </div>
                <div class="sig-detail-field">
                  <label>Nombre</label><span>{{ selected()!.nombre }}</span>
                </div>
                <div class="sig-detail-field">
                  <label>Apellidos</label><span>{{ selected()!.apellidos }}</span>
                </div>
                <div class="sig-detail-field">
                  <label>Rol</label><span>{{ selected()!.roles[0] || '—' }}</span>
                </div>
                <div class="sig-detail-field">
                  <label>Departamento</label><span>Sales Training</span>
                </div>
              </div>

              <div class="sig-detail-section">
                <div class="sig-detail-section-title">Asignaciones <span>{{ 2 }}</span></div>
                <div style="display:grid;grid-template-columns:auto 1fr 1fr;gap:6px 12px;font-size:11px;color:var(--sig-text-muted);margin-bottom:6px;text-transform:uppercase;letter-spacing:.06em">
                  <span>CLIENTE</span><span>PROYECTO</span><span>ACCION</span>
                </div>
                <div style="display:grid;grid-template-columns:auto 1fr 1fr;gap:8px 12px;font-size:12px">
                  <span style="color:var(--sig-text-muted)">Amex</span>
                  <span>Amex</span>
                  <span style="display:flex;justify-content:space-between">Amex Shop Small <button class="sig-quitar-btn">Quitar</button></span>
                  <span style="color:var(--sig-text-muted)">Amex</span>
                  <span>Amex</span>
                  <span style="display:flex;justify-content:space-between">Amex New <button class="sig-quitar-btn">Quitar</button></span>
                </div>
                <span class="sig-add-link" style="margin-top:8px">+ Añadir asignacion</span>
              </div>
            </div>
            <div class="sig-detail-footer">
              <button class="sig-btn-edit"><mat-icon>edit</mat-icon> Editar</button>
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
    .sig-list-topbar { display: flex; align-items: center; justify-content: space-between; padding: 20px 24px 0; margin-bottom: 16px; }
    .sig-page-title { font-size: 20px; font-weight: 700; color: var(--sig-text-heading); margin: 0; display: flex; align-items: center; gap: 10px; mat-icon { color: var(--sig-text-muted); font-size: 20px; width: 20px; height: 20px; } }
    .sig-total-chip { font-size: 11px; font-weight: 700; padding: 2px 8px; border-radius: 10px; background: var(--sig-bg-active); color: var(--sig-teal); border: 1px solid rgba(0,212,196,.2); }
    .sig-topbar-actions { display: flex; align-items: center; gap: 8px; }
    .sig-btn-primary { display: inline-flex; align-items: center; gap: 6px; padding: 0 16px; height: 36px; border-radius: 8px; background: var(--sig-blue); color: #fff; border: none; font-size: 13px; font-weight: 600; font-family: inherit; cursor: pointer; &:hover { background: var(--sig-blue-light); } mat-icon { font-size: 16px !important; width: 16px !important; height: 16px !important; } }
    .sig-btn-outline { display: inline-flex; align-items: center; gap: 6px; padding: 0 14px; height: 34px; border-radius: 8px; background: transparent; color: var(--sig-blue); border: 1px solid var(--sig-border); font-size: 12px; font-family: inherit; cursor: pointer; &:hover { background: var(--sig-bg-hover); } mat-icon { font-size: 15px !important; width: 15px !important; height: 15px !important; } }
    .sig-filter-section { padding: 0 24px 12px; }
    .sig-filter-bar { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
    .sig-search-wrap { display: flex; align-items: center; gap: 8px; background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 8px; padding: 0 12px; height: 36px; flex: 0 0 260px; &:focus-within { border-color: var(--sig-blue); } mat-icon { font-size: 16px !important; width: 16px !important; height: 16px !important; color: var(--sig-text-muted) !important; } }
    .sig-search-input { background: transparent; border: none; outline: none; font-size: 13px; color: var(--sig-text-primary); width: 100%; &::placeholder { color: var(--sig-text-muted); } }
    .sig-select { height: 36px; background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 8px; padding: 0 10px; font-size: 13px; color: var(--sig-text-primary); font-family: inherit; cursor: pointer; outline: none; }
    .sig-btn-filter { height: 36px; padding: 0 16px; border-radius: 8px; background: var(--sig-blue); color: #fff; border: none; font-size: 13px; font-weight: 600; font-family: inherit; cursor: pointer; }
    .sig-btn-limpiar { height: 36px; padding: 0 14px; border-radius: 8px; background: transparent; color: var(--sig-text-muted); border: 1px solid var(--sig-border); font-size: 13px; font-family: inherit; cursor: pointer; }
    .sig-filter-right { margin-left: auto; font-size: 12px; color: var(--sig-text-muted); }
    .sig-content-area { flex: 1; display: flex; overflow: hidden; padding: 0 24px 24px; gap: 16px; }
    .sig-table-wrap { flex: 1; overflow: auto; background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 12px; min-width: 0; }
    table { width: 100%; border-collapse: collapse; }
    thead tr { background: var(--sig-bg-header); border-bottom: 1px solid var(--sig-border); }
    th { padding: 11px 16px; font-size: 10px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; color: var(--sig-text-muted); text-align: left; }
    td { padding: 12px 16px; font-size: 13px; color: var(--sig-text-primary); border-bottom: 1px solid var(--sig-border); vertical-align: middle; }
    tbody tr { cursor: pointer; transition: background 120ms; &:hover { background: var(--sig-bg-hover); } &:last-child td { border-bottom: none; } &.selected { background: rgba(0,212,196,.06) !important; } }
    .sig-role-chip { display: inline-block; padding: 2px 10px; border-radius: 6px; background: var(--sig-bg-hover); border: 1px solid var(--sig-border); font-size: 11px; color: var(--sig-text-secondary); }
    .sig-badge { display: inline-flex; align-items: center; gap: 5px; padding: 2px 10px; border-radius: 20px; font-size: 11px; font-weight: 600; &::before { content:''; width:6px; height:6px; border-radius:50%; background:currentColor; } }
    .sig-badge--green { color: #22c55e; background: rgba(34,197,94,.12); }
    .sig-row-actions { display: flex; align-items: center; gap: 4px; justify-content: flex-end; }
    .sig-icon-btn { width: 30px; height: 30px; border-radius: 6px; border: none; background: transparent; cursor: pointer; display: flex; align-items: center; justify-content: center; color: var(--sig-text-muted); transition: background 120ms; mat-icon { font-size: 16px !important; width: 16px !important; height: 16px !important; } &:hover { background: var(--sig-bg-hover); color: var(--sig-text-primary); } &.danger:hover { background: rgba(239,68,68,.1); color: #ef4444; } }
    .sig-pagination { display: flex; align-items: center; justify-content: flex-end; gap: 8px; padding: 10px 16px; border-top: 1px solid var(--sig-border); font-size: 12px; color: var(--sig-text-muted); }
    .sig-page-btn { width: 28px; height: 28px; border-radius: 6px; border: 1px solid var(--sig-border); background: transparent; color: var(--sig-text-muted); cursor: pointer; font-size: 14px; display: flex; align-items: center; justify-content: center; &:disabled { opacity: .35; cursor: not-allowed; } }
    .sig-page-current { width: 28px; height: 28px; border-radius: 6px; background: var(--sig-blue); color: #fff; font-size: 13px; font-weight: 600; display: flex; align-items: center; justify-content: center; }
    .sig-detail-panel { width: 340px; flex-shrink: 0; background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 12px; overflow: hidden; display: flex; flex-direction: column; }
    .sig-detail-hdr { display: flex; align-items: center; justify-content: space-between; padding: 12px 16px; background: var(--sig-blue); color: #fff; gap: 8px; }
    .sig-detail-hdr-title { font-size: 13px; font-weight: 600; display: flex; align-items: center; gap: 6px; mat-icon { font-size: 15px !important; width: 15px !important; height: 15px !important; } }
    .sig-detail-close { width: 24px; height: 24px; border-radius: 4px; border: none; background: rgba(255,255,255,.2); color: #fff; cursor: pointer; font-size: 16px; display: flex; align-items: center; justify-content: center; }
    .sig-detail-body { flex: 1; overflow-y: auto; padding: 16px; }
    .sig-user-avatar-large { display: flex; align-items: center; gap: 12px; }
    .sig-avatar-xl { width: 44px; height: 44px; border-radius: 50%; font-size: 16px; font-weight: 700; color: #fff; display: flex; align-items: center; justify-content: center; flex-shrink: 0; }
    .sig-detail-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
    .sig-detail-field label { font-size: 10px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; color: var(--sig-text-muted); display: block; margin-bottom: 3px; }
    .sig-detail-field span  { font-size: 13px; color: var(--sig-text-primary); }
    .sig-detail-section { margin-top: 14px; border-top: 1px solid var(--sig-border); padding-top: 12px; }
    .sig-detail-section-title { font-size: 10px; font-weight: 700; letter-spacing: .1em; text-transform: uppercase; color: var(--sig-text-muted); margin-bottom: 10px; display: flex; align-items: center; justify-content: space-between; }
    .sig-add-link { font-size: 12px; color: var(--sig-blue); cursor: pointer; display: inline-flex; align-items: center; gap: 4px; }
    .sig-quitar-btn { font-size: 11px; color: var(--sig-text-muted); background: none; border: none; cursor: pointer; &:hover { color: #ef4444; } }
    .sig-detail-footer { padding: 12px 16px; border-top: 1px solid var(--sig-border); display: flex; gap: 8px; }
    .sig-btn-edit { flex: 1; height: 34px; border-radius: 8px; border: none; background: var(--sig-blue); color: #fff; font-size: 13px; font-weight: 600; font-family: inherit; cursor: pointer; display: flex; align-items: center; justify-content: center; gap: 6px; mat-icon { font-size: 15px !important; width: 15px !important; height: 15px !important; } }
    .sig-btn-del { width: 34px; height: 34px; border-radius: 8px; border: none; background: rgba(239,68,68,.1); color: #ef4444; cursor: pointer; display: flex; align-items: center; justify-content: center; mat-icon { font-size: 16px !important; width: 16px !important; height: 16px !important; } }
    .sig-empty { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 8px; padding: 60px 24px; color: var(--sig-text-muted); }
    .sig-empty mat-icon { font-size: 40px !important; width: 40px !important; height: 40px !important; opacity: .4; }
    .sig-empty-title { font-size: 15px; font-weight: 600; color: var(--sig-text-secondary); }
`],
})
export class UsersListComponent implements OnInit {
  private readonly svc = inject(UserService);
  protected readonly users    = signal<UserListItemDto[]>([]);
  protected readonly selected = signal<UserListItemDto | null>(null);
  protected searchQ      = '';
  protected filterDepto  = '';
  protected filterRol    = '';
  protected filterEstado = '';
  protected readonly total    = computed(() => this.users().length);
  protected readonly filtered = computed(() => {
    let list = this.users();
    if (this.searchQ) list = list.filter(u =>
      (u.nombre + ' ' + u.apellidos + ' ' + (u.nif ?? '')).toLowerCase().includes(this.searchQ.toLowerCase()));
    if (this.filterRol) list = list.filter(u => u.roles.includes(this.filterRol));
    return list;
  });
  ngOnInit(): void {
    this.svc.list(1, 100).subscribe({
      next: (res: any) => { const d = res?.items ?? res ?? []; this.users.set(d); if (!this.selected() && d.length) this.selected.set(d[0]); },
      error: () => this.users.set([]),
    });
  }
  protected selectRow(u: UserListItemDto): void { this.selected.set(u); }
  protected onFilter(): void { }
  protected clearFilters(): void { this.searchQ=''; this.filterDepto=''; this.filterRol=''; this.filterEstado=''; }
  protected openNew(): void { }
  protected userInitials(u: UserListItemDto): string {
    return ((u.nombre?.[0] ?? '') + (u.apellidos?.[0] ?? '')).toUpperCase();
  }
  protected avatarColor(u: UserListItemDto): string {
    const colors = ['#2563eb','#0d9488','#7c3aed','#db2777','#d97706','#16a34a'];
    return colors[(u.id ?? 0) % colors.length];
  }
}
