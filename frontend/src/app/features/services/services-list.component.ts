import { Component, inject, OnInit, signal, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, switchMap, startWith } from 'rxjs/operators';
import { ServiceService } from '../../core/api/services.service';
import { ServiceListItemDto } from '../../models/dtos';

@Component({
  selector: 'app-services-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatIconModule, MatPaginatorModule],
  template: `
    <div class="sig-list-page">
      <div class="sig-list-topbar">
        <h1 class="sig-page-title">
          <mat-icon>task_alt</mat-icon>
          Servicios
          <span class="sig-total-chip">{{ total() }}</span>
        </h1>
        <div class="sig-topbar-actions">
          <button class="sig-btn-outline" (click)="exportCsv()" data-testid="service-export"><mat-icon>download</mat-icon> Exportar CSV</button>
          <button class="sig-btn-primary" (click)="openNew()" data-testid="service-new"><mat-icon>add</mat-icon> Nuevo Servicio</button>
        </div>
      </div>

      <div class="sig-filter-section">
        <div class="sig-filter-bar">
          <div class="sig-search-wrap">
            <mat-icon>search</mat-icon>
            <input class="sig-search-input" [formControl]="searchCtrl" placeholder="Buscar servicio, cliente, departamento..." data-testid="service-search"/>
          </div>
          <button class="sig-btn-limpiar" (click)="clearSearch()">Limpiar</button>
          <span class="sig-filter-right">{{ items().length }} servicios de {{ total() }}</span>
        </div>
      </div>

      <div class="sig-content-area">
        <div class="sig-table-wrap">
          <table>
            <thead>
              <tr>
                <th>ID</th>
                <th>SERVICIO</th>
                <th>CLIENTE</th>
                <th>ESTADO</th>
                <th>DEPARTAMENTO</th>
                <th style="text-align:right">ACCIONES</th>
              </tr>
            </thead>
            <tbody>
              @for (a of items(); track a.id) {
                <tr (click)="selectRow(a)" [class.selected]="selected()?.id === a.id" data-testid="service-item">
                  <td>
                    <div class="col-id">{{ a.id }}</div>
                    <div class="col-secondary">SRV-{{ (a.id+'').padStart(3,'0') }}</div>
                  </td>
                  <td>
                    <div class="col-main">{{ a.nombre }}</div>
                  </td>
                  <td>{{ a.clientNombre }}</td>
                  <td><span class="sig-badge" [class]="estadoBadge(a.estado)">{{ a.estado }}</span></td>
                  <td style="font-size:11px;color:var(--sig-text-muted)">{{ a.departmentId ?? '—' }}</td>
                  <td>
                    <div class="sig-row-actions">
                      <button class="sig-icon-btn" title="Ver" (click)="$event.stopPropagation(); selectRow(a)" [attr.data-testid]="'service-view-' + a.id"><mat-icon>visibility</mat-icon></button>
                      <button class="sig-icon-btn" title="Editar" (click)="$event.stopPropagation(); openEdit(a)" [attr.data-testid]="'service-edit-' + a.id"><mat-icon>edit</mat-icon></button>
                      <button class="sig-icon-btn danger" title="Eliminar" (click)="$event.stopPropagation(); deleteRow(a)" [attr.data-testid]="'service-delete-' + a.id"><mat-icon>delete</mat-icon></button>
                    </div>
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="6"><div class="sig-empty"><mat-icon>task_alt</mat-icon><span class="sig-empty-title">Sin servicios</span></div></td></tr>
              }
            </tbody>
          </table>
          <mat-paginator
            [length]="total()"
            [pageSize]="pageSize()"
            [pageSizeOptions]="[10, 25, 50]"
            (page)="onPageChange($event)">
          </mat-paginator>
        </div>

        @if (selected()) {
          <div class="sig-detail-panel">
            <div class="sig-detail-hdr">
              <span class="sig-detail-hdr-title">
                <mat-icon>task_alt</mat-icon>
                Detalle &mdash; {{ selected()!.nombre }}
              </span>
              <button class="sig-detail-close" (click)="selected.set(null)">&times;</button>
            </div>
            <div class="sig-detail-body">
              <div class="sig-detail-grid">
                <div class="sig-detail-field">
                  <label>Service ID</label>
                  <span>{{ selected()!.id }}</span>
                </div>
                <div class="sig-detail-field">
                  <label>Estado</label>
                  <span class="sig-badge" [class]="estadoBadge(selected()!.estado)">{{ selected()!.estado }}</span>
                </div>
                <div class="sig-detail-field">
                  <label>Cliente</label>
                  <span>{{ selected()!.clientNombre }}</span>
                </div>
                <div class="sig-detail-field">
                  <label>Departamento</label>
                  <span>{{ selected()!.departmentId ?? '—' }}</span>
                </div>
              </div>
            </div>
            <div class="sig-detail-footer">
              <button class="sig-btn-edit" (click)="openEdit(selected()!)"><mat-icon>edit</mat-icon> Editar</button>
              <button class="sig-btn-del" (click)="deleteRow(selected()!)"><mat-icon>delete</mat-icon></button>
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
    .sig-page-title { font-size: 20px; font-weight: 700; color: var(--sig-text-heading); margin: 0; display: flex; align-items: center; gap: 10px; }
    .sig-page-title mat-icon { color: var(--sig-text-muted); font-size: 20px; width: 20px; height: 20px; }
    .sig-total-chip { font-size: 11px; font-weight: 700; padding: 2px 8px; border-radius: 10px; background: var(--sig-bg-active); color: var(--sig-teal); border: 1px solid rgba(0,212,196,.2); }
    .sig-topbar-actions { display: flex; align-items: center; gap: 8px; }
    .sig-btn-primary { display: inline-flex; align-items: center; gap: 6px; padding: 0 16px; height: 36px; border-radius: 8px; background: var(--sig-blue); color: #fff; border: none; font-size: 13px; font-weight: 600; font-family: inherit; cursor: pointer; &:hover { background: var(--sig-blue-light); } mat-icon { font-size: 16px !important; width: 16px !important; height: 16px !important; } }
    .sig-btn-outline { display: inline-flex; align-items: center; gap: 6px; padding: 0 14px; height: 34px; border-radius: 8px; background: transparent; color: var(--sig-blue); border: 1px solid var(--sig-border); font-size: 12px; font-family: inherit; cursor: pointer; &:hover { background: var(--sig-bg-hover); } mat-icon { font-size: 15px !important; width: 15px !important; height: 15px !important; } }
    .sig-filter-section { padding: 0 24px 12px; }
    .sig-filter-bar { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
    .sig-search-wrap { display: flex; align-items: center; gap: 8px; background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 8px; padding: 0 12px; height: 36px; flex: 0 0 220px; &:focus-within { border-color: var(--sig-blue); } mat-icon { font-size: 16px !important; width: 16px !important; height: 16px !important; color: var(--sig-text-muted) !important; } }
    .sig-search-input { background: transparent; border: none; outline: none; font-size: 13px; color: var(--sig-text-primary); width: 100%; &::placeholder { color: var(--sig-text-muted); } }
    .sig-btn-limpiar { height: 36px; padding: 0 14px; border-radius: 8px; background: transparent; color: var(--sig-text-muted); border: 1px solid var(--sig-border); font-size: 13px; font-family: inherit; cursor: pointer; &:hover { background: var(--sig-bg-hover); } }
    .sig-filter-right { margin-left: auto; font-size: 12px; color: var(--sig-text-muted); }
    .sig-content-area { flex: 1; display: flex; overflow: hidden; padding: 0 24px 24px; gap: 16px; }
    .sig-table-wrap { flex: 1; overflow: auto; background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 12px; min-width: 0; }
    table { width: 100%; border-collapse: collapse; }
    thead tr { background: var(--sig-bg-header); border-bottom: 1px solid var(--sig-border); }
    th { padding: 11px 16px; font-size: 10px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; color: var(--sig-text-muted); text-align: left; white-space: nowrap; }
    td { padding: 12px 16px; font-size: 13px; color: var(--sig-text-primary); border-bottom: 1px solid var(--sig-border); vertical-align: middle; }
    tbody tr { cursor: pointer; transition: background 120ms; &:hover { background: var(--sig-bg-hover); } &:last-child td { border-bottom: none; } &.selected { background: rgba(0,212,196,.06) !important; } }
    .col-id { color: var(--sig-blue); font-weight: 700; font-family: 'Roboto Mono',monospace; font-size: 12px; }
    .col-secondary { font-size: 11px; color: var(--sig-text-muted); margin-top: 2px; }
    .col-main { font-weight: 600; }
    .sig-badge { display: inline-flex; align-items: center; gap: 5px; padding: 2px 10px; border-radius: 20px; font-size: 11px; font-weight: 600; }
    .sig-badge::before { content: ''; width: 6px; height: 6px; border-radius: 50%; background: currentColor; }
    .sig-badge--green  { color: #22c55e; background: rgba(34,197,94,.12); }
    .sig-badge--red    { color: #ef4444; background: rgba(239,68,68,.12); }
    .sig-row-actions { display: flex; align-items: center; gap: 4px; justify-content: flex-end; }
    .sig-icon-btn { width: 30px; height: 30px; border-radius: 6px; border: none; background: transparent; cursor: pointer; display: flex; align-items: center; justify-content: center; color: var(--sig-text-muted); transition: background 120ms, color 120ms; mat-icon { font-size: 16px !important; width: 16px !important; height: 16px !important; } &:hover { background: var(--sig-bg-hover); color: var(--sig-text-primary); } &.danger:hover { background: rgba(239,68,68,.1); color: #ef4444; } }
    .sig-detail-panel { width: 340px; flex-shrink: 0; background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 12px; overflow: hidden; display: flex; flex-direction: column; }
    .sig-detail-hdr { display: flex; align-items: center; justify-content: space-between; padding: 12px 16px; background: var(--sig-blue); color: #fff; gap: 8px; }
    .sig-detail-hdr-title { font-size: 13px; font-weight: 600; display: flex; align-items: center; gap: 6px; mat-icon { font-size: 15px !important; width: 15px !important; height: 15px !important; } }
    .sig-detail-close { width: 24px; height: 24px; border-radius: 4px; border: none; background: rgba(255,255,255,.2); color: #fff; cursor: pointer; font-size: 16px; display: flex; align-items: center; justify-content: center; &:hover { background: rgba(255,255,255,.3); } }
    .sig-detail-body { flex: 1; overflow-y: auto; padding: 16px; }
    .sig-detail-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
    .sig-detail-field label { font-size: 10px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; color: var(--sig-text-muted); display: block; margin-bottom: 3px; }
    .sig-detail-field span  { font-size: 13px; color: var(--sig-text-primary); }
    .sig-detail-footer { padding: 12px 16px; border-top: 1px solid var(--sig-border); display: flex; gap: 8px; }
    .sig-btn-edit { flex: 1; height: 34px; border-radius: 8px; border: none; background: var(--sig-blue); color: #fff; font-size: 13px; font-weight: 600; font-family: inherit; cursor: pointer; display: flex; align-items: center; justify-content: center; gap: 6px; mat-icon { font-size: 15px !important; width: 15px !important; height: 15px !important; } }
    .sig-btn-del { width: 34px; height: 34px; border-radius: 8px; border: none; background: rgba(239,68,68,.1); color: #ef4444; cursor: pointer; display: flex; align-items: center; justify-content: center; mat-icon { font-size: 16px !important; width: 16px !important; height: 16px !important; } }
    .sig-empty { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 8px; padding: 60px 24px; color: var(--sig-text-muted); }
    .sig-empty mat-icon { font-size: 40px !important; width: 40px !important; height: 40px !important; opacity: .4; }
    .sig-empty-title { font-size: 15px; font-weight: 600; color: var(--sig-text-secondary); }
`],
})
export class ServicesListComponent implements OnInit {
  private readonly svc = inject(ServiceService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly searchCtrl = new FormControl('', { nonNullable: true });
  protected readonly items = signal<ServiceListItemDto[]>([]);
  protected readonly selected = signal<ServiceListItemDto | null>(null);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(25);
  protected readonly loading = signal(false);

  ngOnInit(): void {
    this.searchCtrl.valueChanges
      .pipe(
        startWith(''),
        debounceTime(300),
        distinctUntilChanged(),
        switchMap(search => {
          this.loading.set(true);
          this.page.set(1);
          return this.svc.list(this.page(), this.pageSize(), undefined, search || undefined);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (res) => {
          this.items.set(res?.items ?? []);
          this.total.set(res?.total ?? 0);
          if (!this.selected() && res?.items?.length) {
            this.selected.set(res.items[0]);
          }
          this.loading.set(false);
        },
        error: (err) => {
          console.error('Error cargando servicios:', err);
          this.items.set([]);
          this.loading.set(false);
        }
      });

    this.loadInitial();
  }

  private loadInitial(): void {
    this.loading.set(true);
    this.svc.list(this.page(), this.pageSize(), undefined, this.searchCtrl.value || undefined)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.items.set(res?.items ?? []);
          this.total.set(res?.total ?? 0);
          if (!this.selected() && res?.items?.length) {
            this.selected.set(res.items[0]);
          }
          this.loading.set(false);
        },
        error: (err) => {
          console.error('Error cargando servicios:', err);
          this.items.set([]);
          this.loading.set(false);
        }
      });
  }

  protected onPageChange(event: PageEvent): void {
    this.page.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    this.loadPage();
  }

  private loadPage(): void {
    this.loading.set(true);
    this.svc.list(this.page(), this.pageSize(), undefined, this.searchCtrl.value || undefined)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.items.set(res?.items ?? []);
          this.total.set(res?.total ?? 0);
          this.loading.set(false);
        },
        error: (err) => {
          console.error('Error cargando servicios:', err);
          this.items.set([]);
          this.loading.set(false);
        }
      });
  }

  protected clearSearch(): void {
    this.searchCtrl.reset('');
  }

  protected selectRow(a: ServiceListItemDto): void {
    this.selected.set(a);
  }

  protected openNew(): void {
    this.router.navigate(['/services/nuevo']);
  }

  protected openEdit(a: ServiceListItemDto): void {
    this.router.navigate([`/services/${a.id}/editar`]);
  }

  protected deleteRow(a: ServiceListItemDto): void {
    if (!confirm(`¿Estás seguro de que deseas eliminar el servicio "${a.nombre}"?`)) {
      return;
    }
    this.svc.delete(a.id).subscribe({
      next: () => {
        this.selected.set(null);
        this.loadPage();
      },
      error: (err) => {
        console.error('Error eliminando servicio:', err);
        alert('Error al eliminar el servicio');
      }
    });
  }

  protected estadoBadge(estado?: string): string {
    return estado === 'Inactivo' ? 'sig-badge--red' : 'sig-badge--green';
  }

  protected exportCsv(): void {
    // TODO: implementar exportación CSV
  }
}
