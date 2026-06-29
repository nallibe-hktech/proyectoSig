import { Component, inject, OnInit, signal, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { debounceTime, distinctUntilChanged, switchMap, startWith } from 'rxjs/operators';
import { ClientService } from '../../core/api/clients.service';
import { ClientListItemDto } from '../../models/dtos';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { NotifyService } from '../../core/notify.service';
import { exportCSV } from '../../core/api/api.helpers';

@Component({
  selector: 'app-clients-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatIconModule, MatPaginatorModule, MatDialogModule],
  template: `
    <div class="sig-list-page">
      <div class="sig-list-topbar">
        <h1 class="sig-page-title">
          <mat-icon>groups</mat-icon>
          Clientes
          <span class="sig-total-chip">{{ total() }}</span>
        </h1>
        <div class="sig-topbar-actions">
          <button class="sig-btn-outline" (click)="exportCsv()" data-testid="client-export"><mat-icon>download</mat-icon> Exportar CSV</button>
          <button class="sig-btn-primary" (click)="openNew()" data-testid="client-new"><mat-icon>add</mat-icon> Nuevo Cliente</button>
        </div>
      </div>

      <div class="sig-filter-section">
        <div class="sig-filter-bar">
          <div class="sig-search-wrap">
            <mat-icon>search</mat-icon>
            <input class="sig-search-input" [formControl]="searchCtrl" placeholder="Buscar cliente, NIF, ciudad..." data-testid="client-search"/>
          </div>
          <button class="sig-btn-limpiar" (click)="clearSearch()">Limpiar</button>
          <span class="sig-filter-right">{{ items().length }} clientes de {{ total() }}</span>
        </div>
      </div>

      <div class="sig-content-area">
        <div class="sig-table-wrap">
          <table>
            <thead>
              <tr>
                <th>NOMBRE</th>
                <th>NIF</th>
                <th>CIUDAD</th>
                <th>ESTADO</th>
                <th>SERVICIOS</th>
                <th class="th-arrow"></th>
              </tr>
            </thead>
            <tbody>
              @for (a of items(); track a.id) {
                <tr (click)="selectRow(a)" [class.selected]="selected()?.id === a.id" data-testid="client-item">
                  <td>
                    <div class="col-main">{{ a.nombre }}</div>
                  </td>
                  <td>
                    <div class="mono-num">{{ a.nif }}</div>
                  </td>
                  <td>{{ a.ciudad ?? '—' }}</td>
                  <td><span class="sig-badge" [class]="estadoBadge(a.estado)">{{ a.estado }}</span></td>
                  <td class="col-secondary">{{ a.serviceCount }}</td>
                  <td class="td-arrow">
                    <mat-icon class="row-chevron">chevron_right</mat-icon>
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="6"><div class="sig-empty"><mat-icon>groups</mat-icon><span class="sig-empty-title">Sin clientes</span></div></td></tr>
              }
            </tbody>
          </table>
          <mat-paginator
            [length]="total()"
            [pageSize]="pageSize()"
            [pageIndex]="page() - 1"
            [pageSizeOptions]="[10, 25, 50]"
            showFirstLastButtons
            (page)="onPageChange($event)">
          </mat-paginator>
        </div>

        @if (selected()) {
          <div class="sig-detail-panel">
            <div class="sig-detail-hdr">
              <span class="sig-detail-hdr-title">
                <mat-icon>groups</mat-icon>
                Detalle &mdash; {{ selected()!.nombre }}
              </span>
              <button class="sig-detail-close" (click)="selected.set(null)">&times;</button>
            </div>
            <div class="sig-detail-body">
              <div class="sig-detail-grid">
                <div class="sig-detail-field">
                  <label>NIF</label>
                  <span class="mono-num">{{ selected()!.nif }}</span>
                </div>
                <div class="sig-detail-field">
                  <label>Estado</label>
                  <span class="sig-badge" [class]="estadoBadge(selected()!.estado)">{{ selected()!.estado }}</span>
                </div>
                <div class="sig-detail-field">
                  <label>Ciudad</label>
                  <span>{{ selected()!.ciudad ?? '—' }}</span>
                </div>
                <div class="sig-detail-field">
                  <label>Servicios</label>
                  <span>{{ selected()!.serviceCount }}</span>
                </div>
              </div>
            </div>
            <div class="sig-detail-footer">
              <button class="sig-btn-secondary" (click)="openDetail(selected()!)" title="Ver detalle completo"><mat-icon>visibility</mat-icon></button>
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
    .col-main { font-weight: 600; }
    .col-secondary { font-size: 11px; color: var(--sig-text-muted); margin-top: 2px; }
    .mono-num { font-family: 'Roboto Mono',monospace; font-size: 12px; }
    .sig-badge { display: inline-flex; align-items: center; gap: 5px; padding: 2px 10px; border-radius: 20px; font-size: 11px; font-weight: 600; }
    .sig-badge::before { content: ''; width: 6px; height: 6px; border-radius: 50%; background: currentColor; }
    .sig-badge--green  { color: #22c55e; background: rgba(34,197,94,.12); }
    .sig-badge--red    { color: #ef4444; background: rgba(239,68,68,.12); }
    .th-arrow { width: 32px; padding: 11px 8px 11px 0; }
    .td-arrow { width: 32px; padding: 0 8px 0 0; text-align: right; }
    .row-chevron { font-size: 18px !important; width: 18px !important; height: 18px !important; color: var(--sig-text-muted); opacity: 0.4; transition: opacity 120ms; }
    tbody tr:hover .row-chevron { opacity: 1; color: var(--sig-blue); }
    .sig-detail-panel { width: 340px; flex-shrink: 0; background: var(--sig-bg-card); border: 1px solid var(--sig-border); border-radius: 12px; overflow: hidden; display: flex; flex-direction: column; }
    .sig-detail-hdr { display: flex; align-items: center; justify-content: space-between; padding: 12px 16px; background: var(--sig-blue); color: #fff; gap: 8px; }
    .sig-detail-hdr-title { font-size: 13px; font-weight: 600; display: flex; align-items: center; gap: 6px; mat-icon { font-size: 15px !important; width: 15px !important; height: 15px !important; } }
    .sig-detail-close { width: 24px; height: 24px; border-radius: 4px; border: none; background: rgba(255,255,255,.2); color: #fff; cursor: pointer; font-size: 16px; display: flex; align-items: center; justify-content: center; &:hover { background: rgba(255,255,255,.3); } }
    .sig-detail-body { flex: 1; overflow-y: auto; padding: 16px; }
    .sig-detail-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
    .sig-detail-field label { font-size: 10px; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; color: var(--sig-text-muted); display: block; margin-bottom: 3px; }
    .sig-detail-field span  { font-size: 13px; color: var(--sig-text-primary); }
    .sig-detail-footer { padding: 12px 16px; border-top: 1px solid var(--sig-border); display: flex; gap: 8px; align-items: center; }
    .sig-btn-secondary { width: 34px; height: 34px; border-radius: 8px; border: 1px solid var(--sig-border); background: transparent; color: var(--sig-text-muted); cursor: pointer; display: flex; align-items: center; justify-content: center; transition: background 120ms, color 120ms; mat-icon { font-size: 16px !important; width: 16px !important; height: 16px !important; } &:hover { background: var(--sig-bg-hover); color: var(--sig-text-primary); } }
    .sig-btn-edit { flex: 1; height: 34px; border-radius: 8px; border: none; background: var(--sig-blue); color: #fff; font-size: 13px; font-weight: 600; font-family: inherit; cursor: pointer; display: flex; align-items: center; justify-content: center; gap: 6px; mat-icon { font-size: 15px !important; width: 15px !important; height: 15px !important; } }
    .sig-btn-del { width: 34px; height: 34px; border-radius: 8px; border: none; background: rgba(239,68,68,.1); color: #ef4444; cursor: pointer; display: flex; align-items: center; justify-content: center; mat-icon { font-size: 16px !important; width: 16px !important; height: 16px !important; } }
    .sig-empty { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 8px; padding: 60px 24px; color: var(--sig-text-muted); }
    .sig-empty mat-icon { font-size: 40px !important; width: 40px !important; height: 40px !important; opacity: .4; }
    .sig-empty-title { font-size: 15px; font-weight: 600; color: var(--sig-text-secondary); }
  `],
})
export class ClientsListComponent implements OnInit {
  private readonly svc = inject(ClientService);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotifyService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly searchCtrl = new FormControl('', { nonNullable: true });
  protected readonly items = signal<ClientListItemDto[]>([]);
  protected readonly selected = signal<ClientListItemDto | null>(null);
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
          return this.svc.list(this.page(), this.pageSize(), search || undefined);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(res => {
        this.items.set(res.items);
        this.total.set(res.total);
        this.loading.set(false);
      });
  }

  protected selectRow(item: ClientListItemDto): void {
    this.selected.set(item);
  }

  protected clearSearch(): void {
    this.searchCtrl.setValue('');
  }

  protected openNew(): void {
    void this.router.navigate(['/clients/nuevo']);
  }

  protected openDetail(item: ClientListItemDto): void {
    void this.router.navigate(['/clients', item.id]);
  }

  protected openEdit(item: ClientListItemDto): void {
    void this.router.navigate(['/clients', item.id, 'editar']);
  }

  protected deleteRow(item: ClientListItemDto): void {
    this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Eliminar Cliente',
        message: 'Estás a punto de eliminar este cliente.',
        entityName: item.nombre,
        dependencies: item.serviceCount > 0 ? [{ label: 'Servicios', count: item.serviceCount }] : undefined,
        destructive: true,
      },
      minWidth: 480,
    }).afterClosed().subscribe((confirm) => {
      if (!confirm) return;
      this.svc.delete(item.id).subscribe({
        next: () => {
          this.notify.success('Cliente eliminado');
          this.selected.set(null);
          this.searchCtrl.setValue(this.searchCtrl.value); // Refresh list
        },
        error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo eliminar'),
      });
    });
  }

  protected exportCsv(): void {
    exportCSV('clientes.csv', this.items().map((c) => ({
      Id: c.id, Nombre: c.nombre, NIF: c.nif, Ciudad: c.ciudad ?? '', Estado: c.estado, Servicios: c.serviceCount,
    })));
  }

  protected estadoBadge(estado?: string): string {
    return estado === 'Inactivo' ? 'sig-badge--red' : 'sig-badge--green';
  }

  protected onPageChange(e: PageEvent): void {
    this.pageSize.set(e.pageSize);
    this.page.set(e.pageIndex + 1);
    window.scrollTo({ top: 0, behavior: 'smooth' });
    this.searchCtrl.setValue(this.searchCtrl.value); // Refresh with current search
  }
}
