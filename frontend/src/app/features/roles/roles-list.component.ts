import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { RoleService } from '../../core/api/catalogs.service';
import { RoleDto } from '../../models/dtos';

interface RolDef {
  nombre: string; ambito: string;
  pagos: string; facturaciones: string; usuarios: string; roles: string;
}

@Component({
  selector: 'app-roles-list',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule, MatPaginatorModule],
  template: `
    <div class="sig-list-page">
      <div class="sig-list-topbar">
        <h1 class="sig-page-title">
          <mat-icon>verified_user</mat-icon>
          Roles
          <span class="sig-total-chip">{{ roles().length }}</span>
        </h1>
        <div class="sig-topbar-actions">
          <button class="sig-btn-outline"><mat-icon>add</mat-icon> Nuevo Rol</button>
        </div>
      </div>

      <div class="sig-filter-section">
        <div class="sig-filter-bar">
          <div class="sig-search-wrap">
            <mat-icon>search</mat-icon>
            <input class="sig-search-input" [(ngModel)]="searchQ" placeholder="Buscar tipo de rol..." (input)="onFilter()"/>
          </div>
          <select class="sig-select">
            <option value="">Vista</option>
            <option>Todas</option>
            <option>Global</option>
            <option>Servicio</option>
          </select>
          <select class="sig-select">
            <option value="">Ambito</option>
            <option>Todos</option>
            <option>Global</option>
            <option>Servicio</option>
          </select>
          <button class="sig-btn-filter">Filtrar</button>
          <button class="sig-btn-limpiar" (click)="clearFilters()">Limpiar</button>
          <span class="sig-filter-right">TOTAL {{ roles().length }}</span>
        </div>
      </div>

      <div class="sig-content-area">
        <div class="sig-table-wrap">
          <table>
            <thead>
              <tr>
                <th>TIPO DE ROL</th>
                <th>VISTA</th>
                <th>PAGOS</th>
                <th>FACTURACIONES</th>
                <th>USUARIOS</th>
                <th>ROLES</th>
              </tr>
            </thead>
            <tbody>
              @for (r of roles(); track r.nombre) {
                <tr (click)="selectRow(r)" [class.selected]="selected()?.nombre === r.nombre">
                  <td style="font-weight:600">{{ r.nombre }}</td>
                  <td><span class="sig-scope-badge" [class]="r.ambito === 'Global' ? 'scope--global' : 'scope--proyecto'">{{ r.ambito }}</span></td>
                  <td [innerHTML]="permLabel(r.pagos)"></td>
                  <td [innerHTML]="permLabel(r.facturaciones)"></td>
                  <td [innerHTML]="permLabel(r.usuarios)"></td>
                  <td [innerHTML]="permLabel(r.roles)"></td>
                </tr>
              }
            </tbody>
          </table>
          <div class="sig-footer-note">
            El permiso "Editar" incluye Eliminar &middot; Los datos de Ceco / Cliente / Servicio / Concepto se alimentan de Celero
          </div>
          <mat-paginator
            [length]="total()"
            [pageSize]="pageSize()"
            [pageIndex]="page() - 1"
            [pageSizeOptions]="[10, 25, 50]"
            showFirstLastButtons
            (page)="onPageChange($event)"
          ></mat-paginator>
        </div>

        @if (selected()) {
          <div class="sig-detail-panel">
            <div class="sig-detail-hdr">
              <span class="sig-detail-hdr-title">
                <mat-icon>verified_user</mat-icon>
                Detalle del Rol
              </span>
              <button class="sig-detail-close" (click)="selected.set(null)">&times;</button>
            </div>
            <div class="sig-detail-body">
              <div class="sig-role-header">
                <div class="sig-role-icon"><mat-icon>verified_user</mat-icon></div>
                <div>
                  <div style="font-size:15px;font-weight:700;color:var(--sig-text-heading)">{{ selected()!.nombre }}</div>
                  <div class="sig-scope-badge" [class]="selected()!.ambito === 'Global' ? 'scope--global' : 'scope--proyecto'" style="margin-top:4px">Vista {{ selected()!.ambito }}</div>
                </div>
              </div>

              <div class="sig-detail-section" style="margin-top:14px">
                <div class="sig-detail-section-title">Matriz de Permisos</div>
                <div class="sig-perm-matrix">
                  <div class="sig-perm-row">
                    <span class="sig-perm-label">Pagos</span>
                    <div class="sig-perm-pills" [innerHTML]="permPills(selected()!.pagos)"></div>
                  </div>
                  <div class="sig-perm-row">
                    <span class="sig-perm-label">Facturaciones</span>
                    <div class="sig-perm-pills" [innerHTML]="permPills(selected()!.facturaciones)"></div>
                  </div>
                  <div class="sig-perm-row">
                    <span class="sig-perm-label">Ceco</span>
                    <div class="sig-perm-pills"><span class="sig-perm-pill perm--ver">Ver</span></div>
                  </div>
                  <div class="sig-perm-row">
                    <span class="sig-perm-label">Departamento</span>
                    <div class="sig-perm-pills"><span class="sig-perm-pill perm--ver">Ver</span></div>
                  </div>
                  <div class="sig-perm-row">
                    <span class="sig-perm-label">Cliente</span>
                    <div class="sig-perm-pills"><span class="sig-perm-pill perm--ver">Ver</span></div>
                  </div>
                  <div class="sig-perm-row">
                    <span class="sig-perm-label">Servicio</span>
                    <div class="sig-perm-pills"><span class="sig-perm-pill perm--ver">Ver</span></div>
                  </div>
                  <div class="sig-perm-row">
                    <span class="sig-perm-label">Concepto</span>
                    <div class="sig-perm-pills"><span class="sig-perm-pill perm--ver">Ver</span></div>
                  </div>
                  <div class="sig-perm-row">
                    <span class="sig-perm-label">Usuarios</span>
                    <div class="sig-perm-pills" [innerHTML]="permPills(selected()!.usuarios)"></div>
                  </div>
                  <div class="sig-perm-row">
                    <span class="sig-perm-label">Roles</span>
                    <div class="sig-perm-pills" [innerHTML]="permPills(selected()!.roles)"></div>
                  </div>
                </div>
              </div>

              <div class="sig-detail-section">
                <div class="sig-detail-section-title">Usuarios con este Rol <span>3</span></div>
                <div class="sig-avatar-chip">
                  <div class="sig-avatar" style="background:#7c3aed">AT</div>
                  <div class="sig-avatar-info"><span class="sig-avatar-name">Adrian Tomas</span><span class="sig-avatar-role">adrian.tomas&#64;sigeurope.com</span></div>
                  <button class="sig-quitar-btn">Quitar</button>
                </div>
                <div class="sig-avatar-chip">
                  <div class="sig-avatar" style="background:#0d9488">TM</div>
                  <div class="sig-avatar-info"><span class="sig-avatar-name">Tomas Martin</span><span class="sig-avatar-role">tomas.martin&#64;sigeurope.com</span></div>
                  <button class="sig-quitar-btn">Quitar</button>
                </div>
                <span class="sig-add-link">+ Añadir Usuario</span>
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
    .sig-page-title { font-size: 20px; font-weight: 700; color: var(--sig-text-heading); margin: 0; display: flex; align-items: center; gap: 10px; mat-icon { color: var(--sig-teal); font-size: 20px; width: 20px; height: 20px; } }
    .sig-total-chip { font-size: 11px; font-weight: 700; padding: 2px 8px; border-radius: 10px; background: var(--sig-bg-active); color: var(--sig-teal); border: 1px solid rgba(0,212,196,.2); }
    .sig-topbar-actions { display: flex; gap: 8px; }
    .sig-btn-outline { display: inline-flex; align-items: center; gap: 6px; padding: 0 14px; height: 34px; border-radius: 8px; background: transparent; color: var(--sig-blue); border: 1px solid var(--sig-border); font-size: 12px; font-family: inherit; cursor: pointer; mat-icon { font-size: 15px !important; width: 15px !important; height: 15px !important; } }
    .sig-filter-section { padding: 0 24px 12px; }
    .sig-filter-bar { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
    .sig-search-wrap { display: flex; align-items: center; gap: 8px; background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 8px; padding: 0 12px; height: 36px; flex: 0 0 260px; mat-icon { font-size: 16px !important; width: 16px !important; height: 16px !important; color: var(--sig-text-muted) !important; } }
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
    .sig-scope-badge { display: inline-block; padding: 2px 9px; border-radius: 5px; font-size: 11px; font-weight: 600; }
    .scope--global   { background: rgba(0,212,196,.12); color: #00d4c4; }
    .scope--proyecto { background: rgba(59,130,246,.12); color: #3b82f6; }
    .sig-perm-text { font-size: 12px; }
    .perm-ctrl   { color: #00d4c4; font-weight: 600; }
    .perm-full   { color: var(--sig-text-primary); }
    .perm-none   { color: var(--sig-text-muted); }
    .sig-footer-note { padding: 10px 16px; font-size: 11px; color: var(--sig-text-muted); border-top: 1px solid var(--sig-border); }
    .sig-detail-panel { width: 360px; flex-shrink: 0; background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 12px; overflow: hidden; display: flex; flex-direction: column; }
    .sig-detail-hdr { display: flex; align-items: center; justify-content: space-between; padding: 12px 16px; background: var(--sig-blue); color: #fff; }
    .sig-detail-hdr-title { font-size: 13px; font-weight: 600; display: flex; align-items: center; gap: 6px; mat-icon { font-size: 15px !important; width: 15px !important; height: 15px !important; } }
    .sig-detail-close { width: 24px; height: 24px; border-radius: 4px; border: none; background: rgba(255,255,255,.2); color: #fff; cursor: pointer; font-size: 16px; display: flex; align-items: center; justify-content: center; }
    .sig-detail-body { flex: 1; overflow-y: auto; padding: 16px; }
    .sig-role-header { display: flex; align-items: center; gap: 12px; }
    .sig-role-icon { width: 40px; height: 40px; border-radius: 10px; background: rgba(0,212,196,.12); display: flex; align-items: center; justify-content: center; mat-icon { font-size: 20px !important; width: 20px !important; height: 20px !important; color: var(--sig-teal) !important; } }
    .sig-detail-section { border-top: 1px solid var(--sig-border); padding-top: 12px; }
    .sig-detail-section-title { font-size: 10px; font-weight: 700; letter-spacing: .1em; text-transform: uppercase; color: var(--sig-text-muted); margin-bottom: 10px; display: flex; align-items: center; justify-content: space-between; }
    .sig-perm-matrix { display: flex; flex-direction: column; gap: 6px; }
    .sig-perm-row { display: flex; align-items: center; padding: 5px 0; border-bottom: 1px solid var(--sig-border); gap: 8px; &:last-child { border-bottom: none; } }
    .sig-perm-label { font-size: 12px; color: var(--sig-text-secondary); width: 100px; flex-shrink: 0; }
    .sig-perm-pills { display: flex; gap: 4px; flex-wrap: wrap; }
    .sig-perm-pill { padding: 1px 7px; border-radius: 4px; font-size: 11px; font-weight: 600; }
    .perm--ver    { background: rgba(59,130,246,.12); color: #3b82f6; }
    .perm--val    { background: rgba(245,158,11,.12); color: #f59e0b; }
    .perm--edit   { background: rgba(0,212,196,.12); color: #00d4c4; }
    .perm--crear  { background: rgba(34,197,94,.12); color: #22c55e; }
    .perm--none   { background: rgba(239,68,68,.1); color: #ef4444; }
    .sig-avatar-chip { display: flex; align-items: center; gap: 8px; padding: 6px 0; border-bottom: 1px solid var(--sig-border); &:last-of-type { border-bottom: none; } }
    .sig-avatar { width: 28px; height: 28px; border-radius: 50%; font-size: 11px; font-weight: 700; display: flex; align-items: center; justify-content: center; flex-shrink: 0; color: #fff; }
    .sig-avatar-info { display: flex; flex-direction: column; flex: 1; }
    .sig-avatar-name { font-size: 12px; font-weight: 600; color: var(--sig-text-primary); }
    .sig-avatar-role { font-size: 10px; color: var(--sig-text-muted); }
    .sig-quitar-btn { font-size: 11px; color: var(--sig-text-muted); background: none; border: none; cursor: pointer; &:hover { color: #ef4444; } }
    .sig-add-link { font-size: 12px; color: var(--sig-blue); cursor: pointer; display: inline-flex; align-items: center; gap: 4px; margin-top: 8px; }
    .sig-detail-footer { padding: 12px 16px; border-top: 1px solid var(--sig-border); display: flex; gap: 8px; }
    .sig-btn-edit { flex: 1; height: 34px; border-radius: 8px; border: none; background: var(--sig-blue); color: #fff; font-size: 13px; font-weight: 600; font-family: inherit; cursor: pointer; display: flex; align-items: center; justify-content: center; gap: 6px; mat-icon { font-size: 15px !important; width: 15px !important; height: 15px !important; } }
    .sig-btn-del { width: 34px; height: 34px; border-radius: 8px; border: none; background: rgba(239,68,68,.1); color: #ef4444; cursor: pointer; display: flex; align-items: center; justify-content: center; mat-icon { font-size: 16px !important; width: 16px !important; height: 16px !important; } }
`],
})
export class RolesListComponent implements OnInit {
  protected searchQ = '';
  protected readonly selected = signal<RolDef | null>(null);
  protected selectedIndex = 0;

  protected readonly page = signal(1);
  protected readonly pageSize = signal(25);
  protected readonly total = signal(0);
  protected readonly items = signal<RolDef[]>([]);

  private readonly allRoles: RolDef[] = [
    { nombre:'Administrador', ambito:'Global',   pagos:'Control total',       facturaciones:'Control total',       usuarios:'Control total',   roles:'Control total' },
    { nombre:'Direccion',     ambito:'Global',   pagos:'Ver / Validar / Editar', facturaciones:'Ver / Validar / Editar', usuarios:'Ver / Editar / Crear', roles:'Sin permisos' },
    { nombre:'FICO',          ambito:'Global',   pagos:'Ver / Validar / Editar', facturaciones:'Ver / Validar / Editar', usuarios:'Ver / Editar / Crear', roles:'Sin permisos' },
    { nombre:'RRHH',          ambito:'Global',   pagos:'Ver / Validar / Editar', facturaciones:'Sin permisos',          usuarios:'Ver / Editar / Crear', roles:'Sin permisos' },
    { nombre:'Facilitador',   ambito:'Global',   pagos:'Ver / Validar / Editar', facturaciones:'Ver / Validar / Editar', usuarios:'Ver / Editar / Crear', roles:'Sin permisos' },
    { nombre:'Interlocutor',  ambito:'Servicio', pagos:'Ver / Validar / Editar', facturaciones:'Ver / Validar / Editar', usuarios:'Sin permisos',         roles:'Sin permisos' },
    { nombre:'Gestor',        ambito:'Servicio', pagos:'Ver / Validar / Editar', facturaciones:'Ver / Validar / Editar', usuarios:'Sin permisos',         roles:'Sin permisos' },
    { nombre:'Backoffice',    ambito:'Servicio', pagos:'Ver / Validar',          facturaciones:'Sin permisos',          usuarios:'Sin permisos',         roles:'Sin permisos' },
    { nombre:'Auxiliar',      ambito:'Servicio', pagos:'Ver',                    facturaciones:'Sin permisos',          usuarios:'Sin permisos',         roles:'Sin permisos' },
  ]);

  get roles() { return this.items; }

  ngOnInit(): void {
    this.load();
  }

  protected onPageChange(event: PageEvent): void {
    this.pageSize.set(event.pageSize);
    this.page.set(event.pageIndex + 1);
    window.scrollTo({ top: 0, behavior: 'smooth' });
    this.load();
  }

  private load(): void {
    this.total.set(this.allRoles.length);
    const start = (this.page() - 1) * this.pageSize();
    const end = start + this.pageSize();
    const paginated = this.allRoles.slice(start, end);
    this.items.set(paginated);
    if (paginated.length && !this.selected()) {
      this.selected.set(paginated[0]);
    }
  }

  protected selectRow(r: RolDef): void { this.selected.set(r); }
  protected onFilter(): void { }
  protected clearFilters(): void { this.searchQ = ''; }

  protected permLabel(perm: string): string {
    if (perm === 'Control total') return '<span class="sig-perm-text perm-ctrl">Control total</span>';
    if (perm === 'Sin permisos')  return '<span class="sig-perm-text perm-none">Sin permisos</span>';
    return '<span class="sig-perm-text perm-full">' + perm + '</span>';
  }

  protected permPills(perm: string): string {
    if (perm === 'Control total') return '<span class="sig-perm-pill perm--edit">Control total</span>';
    if (perm === 'Sin permisos')  return '<span class="sig-perm-pill perm--none">Sin permisos</span>';
    const map: Record<string,string> = { 'Ver':'perm--ver', 'Validar':'perm--val', 'Editar':'perm--edit', 'Crear':'perm--crear' };
    return perm.split(' / ').map(p => '<span class="sig-perm-pill ' + (map[p] || '') + '">' + p + '</span>').join('');
  }
}
