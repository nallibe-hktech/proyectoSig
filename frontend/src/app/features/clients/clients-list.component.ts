import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSortModule } from '@angular/material/sort';
import { MatSelectModule } from '@angular/material/select';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { ClientService } from '../../core/api/clients.service';
import { ClientListItemDto } from '../../models/dtos';
import { EstadoCliente } from '../../models/enums';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { NotifyService } from '../../core/notify.service';
import { exportCSV } from '../../core/api/api.helpers';

@Component({
  selector: 'app-clients-list',
  standalone: true,
  imports: [
    CommonModule, RouterLink, ReactiveFormsModule,
    MatCardModule, MatTableModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatPaginatorModule, MatSortModule, MatSelectModule, MatDialogModule, MatSlideToggleModule,
    BreadcrumbsComponent, SkeletonComponent, EmptyStateComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Clients' }]" />

      <div class="sig-page__header">
        <h1 class="sig-page__title">Clients</h1>
        <a mat-flat-button color="primary" routerLink="/clients/nuevo" data-testid="btn-nuevo">
          <mat-icon>add</mat-icon> Nuevo Client
        </a>
      </div>

      <mat-card>
        <mat-card-content>
          <div class="sig-table-toolbar">
            <mat-form-field appearance="outline" class="sig-search">
              <mat-icon matPrefix aria-hidden="true">search</mat-icon>
              <mat-label>Buscar por nombre, NIF...</mat-label>
              <input matInput [formControl]="search" data-testid="input-busqueda" />
            </mat-form-field>
            <mat-form-field appearance="outline" class="sig-filter-estado">
              <mat-label>Estado</mat-label>
              <mat-select [formControl]="estadoFilter" data-testid="select-estado">
                <mat-option [value]="''">Todos</mat-option>
                <mat-option value="Activo">Activo</mat-option>
                <mat-option value="Inactivo">Inactivo</mat-option>
              </mat-select>
            </mat-form-field>
            <button mat-stroked-button (click)="onExportCSV()" data-testid="btn-exportar-csv">
              <mat-icon>download</mat-icon> Exportar CSV
            </button>
          </div>

          @if (loading()) {
            <sig-skeleton [count]="5" />
          } @else if (displayItems().length === 0) {
            <sig-empty-state
              icon="groups"
              title="No hay clientes todavía"
              description="Crea el primer cliente para empezar a gestionar servicios y cierres."
              ctaLabel="Crear primer client"
              [hasFilter]="!!search.value"
              (ctaClick)="onEmptyCta()"
            />
          } @else {
            <div class="sig-canvas">
              <table mat-table [dataSource]="displayItems()" class="sig-table" data-testid="tabla-clients">
                <ng-container matColumnDef="nombre">
                  <th mat-header-cell *matHeaderCellDef>Nombre</th>
                  <td mat-cell *matCellDef="let row">{{ row.nombre }}</td>
                </ng-container>
                <ng-container matColumnDef="nif">
                  <th mat-header-cell *matHeaderCellDef>NIF</th>
                  <td mat-cell *matCellDef="let row" class="mono-num">{{ row.nif }}</td>
                </ng-container>
                <ng-container matColumnDef="ciudad">
                  <th mat-header-cell *matHeaderCellDef>Ciudad</th>
                  <td mat-cell *matCellDef="let row">{{ row.ciudad ?? '—' }}</td>
                </ng-container>
                <ng-container matColumnDef="estado">
                  <th mat-header-cell *matHeaderCellDef>Estado</th>
                  <td mat-cell *matCellDef="let row">
                    <span class="sig-badge" [class]="estadoBadge(row.estado)">{{ row.estado }}</span>
                  </td>
                </ng-container>
                <ng-container matColumnDef="serviceCount">
                  <th mat-header-cell *matHeaderCellDef>Servicios</th>
                  <td mat-cell *matCellDef="let row" class="mono-num">{{ row.serviceCount }}</td>
                </ng-container>
                <ng-container matColumnDef="chevron">
                  <th mat-header-cell *matHeaderCellDef class="th-arrow"></th>
                  <td mat-cell *matCellDef="let row" class="td-arrow">
                    <mat-icon class="row-chevron" (click)="selectClient(row)">chevron_right</mat-icon>
                  </td>
                </ng-container>

                <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
                <tr mat-row *matRowDef="let row; columns: displayedColumns" (click)="selectClient(row)" data-testid="row-client" [class.selected]="selected()?.id === row.id"></tr>
              </table>

              <!-- Detail panel lateral -->
              @if (selected(); as client) {
                <div class="sig-detail-panel">
                  <div class="sig-detail-header">
                    <h2 class="sig-detail-title">{{ client.nombre }}</h2>
                    <button mat-icon-button class="sig-close-btn" (click)="closeDetail()" aria-label="Cerrar">
                      <mat-icon>close</mat-icon>
                    </button>
                  </div>
                  <div class="sig-detail-content">
                    <div class="sig-detail-field">
                      <span class="sig-field-label">NIF</span>
                      <span class="sig-field-value mono-num">{{ client.nif }}</span>
                    </div>
                    <div class="sig-detail-field">
                      <span class="sig-field-label">Ciudad</span>
                      <span class="sig-field-value">{{ client.ciudad ?? '—' }}</span>
                    </div>
                    <div class="sig-detail-field">
                      <span class="sig-field-label">Estado</span>
                      <span class="sig-badge" [class]="estadoBadge(client.estado)">{{ client.estado }}</span>
                    </div>
                    <div class="sig-detail-field">
                      <span class="sig-field-label">Servicios</span>
                      <span class="sig-field-value">{{ client.serviceCount }}</span>
                    </div>
                  </div>
                  <div class="sig-detail-footer">
                    <button class="sig-btn-secondary" (click)="openDetail(client)" title="Ver detalle completo"><mat-icon>visibility</mat-icon></button>
                    <button class="sig-btn-edit" (click)="editClient(client)"><mat-icon>edit</mat-icon> Editar</button>
                    <button class="sig-btn-del" (click)="deleteRow(client)"><mat-icon>delete</mat-icon></button>
                  </div>
                </div>
              }
            </div>
          }

          <mat-paginator
            [length]="total()"
            [pageSize]="pageSize()"
            [pageIndex]="page() - 1"
            [pageSizeOptions]="[10, 25, 50]"
            showFirstLastButtons
            (page)="onPage($event)"
            data-testid="paginator-clients"
          />
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .sig-table-toolbar {
      display: flex; gap: 12px; align-items: center; margin-bottom: 16px;
    }
    .sig-search { flex: 1; max-width: 480px; }
    .sig-filter-estado { width: 160px; }
    .sig-badge { display: inline-flex; align-items: center; gap: 5px; padding: 2px 10px; border-radius: 20px; font-size: 11px; font-weight: 600; }
    .sig-badge::before { content: ''; width: 6px; height: 6px; border-radius: 50%; background: currentColor; }
    .sig-badge--green  { color: #22c55e; background: rgba(34,197,94,.12); }
    .sig-badge--red    { color: #ef4444; background: rgba(239,68,68,.12); }
    .mono-num { font-family: 'Roboto Mono', monospace; font-size: 12px; }

    .sig-canvas {
      display: grid; grid-template-columns: 1fr auto; gap: 16px; align-items: start;
    }
    .sig-table { width: 100%; }
    tbody tr { cursor: pointer; }
    tbody tr:hover { background: rgba(255, 255, 255, 0.04); }
    tbody tr.selected { background: rgba(33, 150, 243, 0.08); }

    .th-arrow { width: 32px; padding: 11px 8px 11px 0; }
    .td-arrow { width: 32px; padding: 0 8px 0 0; text-align: right; }
    .row-chevron { font-size: 18px !important; width: 18px !important; height: 18px !important; color: var(--sig-text-muted); opacity: 0.4; transition: opacity 120ms; cursor: pointer; }
    tbody tr:hover .row-chevron { opacity: 1; color: var(--sig-blue); }

    .sig-detail-panel {
      background: var(--mat-sys-surface); border: 1px solid var(--mat-sys-outline); border-radius: 8px; width: 320px; display: flex; flex-direction: column; box-shadow: 0 2px 8px rgba(0,0,0,0.1);
    }
    .sig-detail-header { padding: 16px; border-bottom: 1px solid var(--mat-sys-outline); display: flex; justify-content: space-between; align-items: center; }
    .sig-detail-title { font-size: 16px; font-weight: 600; margin: 0; }
    .sig-close-btn { width: 32px; height: 32px; }
    .sig-detail-content { padding: 16px; flex: 1; overflow-y: auto; }
    .sig-detail-field { display: flex; flex-direction: column; gap: 4px; margin-bottom: 12px; }
    .sig-field-label { font-size: 12px; color: var(--mat-sys-on-surface-variant); font-weight: 600; text-transform: uppercase; letter-spacing: 0.08em; }
    .sig-field-value { font-size: 14px; color: var(--mat-sys-on-surface); }

    .sig-detail-footer {
      padding: 12px 16px; border-top: 1px solid var(--mat-sys-outline); display: flex; gap: 8px;
    }
    .sig-btn-secondary {
      flex: 0 0 auto; padding: 8px; border-radius: 4px; background: transparent; border: 1px solid var(--mat-sys-outline); color: var(--mat-sys-on-surface); cursor: pointer; display: flex; align-items: center; justify-content: center;
    }
    .sig-btn-secondary:hover { background: rgba(255,255,255,0.08); }
    .sig-btn-edit {
      flex: 1; padding: 8px 12px; border-radius: 4px; background: var(--sig-blue); color: white; border: none; cursor: pointer; display: flex; align-items: center; justify-content: center; gap: 6px; font-size: 13px;
    }
    .sig-btn-edit:hover { opacity: 0.9; }
    .sig-btn-del {
      flex: 0 0 auto; padding: 8px; border-radius: 4px; background: transparent; border: 1px solid #ef4444; color: #ef4444; cursor: pointer; display: flex; align-items: center; justify-content: center;
    }
    .sig-btn-del:hover { background: rgba(239,68,68,0.1); }
  `],
})
export class ClientsListComponent implements OnInit {
  private readonly clientSvc = inject(ClientService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotifyService);
  private readonly router = inject(Router);

  protected readonly items = signal<ClientListItemDto[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(25);
  protected readonly loading = signal(true);
  protected readonly search = new FormControl<string>('', { nonNullable: true });
  protected readonly estadoFilter = new FormControl<'' | EstadoCliente>('', { nonNullable: true });
  protected readonly estadoFilterValue = signal<'' | EstadoCliente>('');
  protected readonly selected = signal<ClientListItemDto | null>(null);

  protected readonly displayItems = computed(() => {
    const estado = this.estadoFilterValue();
    return estado ? this.items().filter((c) => c.estado === estado) : this.items();
  });

  protected readonly displayedColumns = ['nombre', 'nif', 'ciudad', 'estado', 'serviceCount', 'chevron'];

  ngOnInit(): void {
    this.search.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged())
      .subscribe(() => {
        this.page.set(1);
        this.load();
      });
    this.estadoFilter.valueChanges.subscribe((v) => this.estadoFilterValue.set(v));
    this.load();
  }

  protected estadoBadge(estado?: string): string {
    return estado === 'Inactivo' ? 'sig-badge--red' : 'sig-badge--green';
  }

  protected onPage(e: PageEvent): void {
    this.pageSize.set(e.pageSize);
    this.page.set(e.pageIndex + 1);
    window.scrollTo({ top: 0, behavior: 'smooth' });
    this.load();
  }

  protected onEmptyCta(): void {
    if (this.search.value) {
      this.search.setValue('');
    } else {
      void this.router.navigate(['/clients/nuevo']);
    }
  }

  protected selectClient(row: ClientListItemDto): void {
    this.selected.set(row);
  }

  protected closeDetail(): void {
    this.selected.set(null);
  }

  protected openDetail(row: ClientListItemDto): void {
    void this.router.navigate(['/clients', row.id]);
  }

  protected editClient(row: ClientListItemDto): void {
    void this.router.navigate(['/clients', row.id, 'editar']);
  }

  protected deleteRow(row: ClientListItemDto): void {
    this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Eliminar Client',
        message: 'Estás a punto de eliminar este cliente.',
        entityName: row.nombre,
        dependencies: row.serviceCount > 0 ? [{ label: 'Servicios', count: row.serviceCount }] : undefined,
        destructive: true,
      },
      minWidth: 480,
    }).afterClosed().subscribe((confirm) => {
      if (!confirm) return;
      this.clientSvc.delete(row.id).subscribe({
        next: () => { this.notify.success('Client eliminado'); this.load(); this.closeDetail(); },
        error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo eliminar'),
      });
    });
  }

  protected onDelete(row: ClientListItemDto): void {
    this.deleteRow(row);
  }

  protected onExportCSV(): void {
    exportCSV('clients.csv', this.displayItems().map((c) => ({
      Id: c.id, Nombre: c.nombre, NIF: c.nif, Ciudad: c.ciudad ?? '', Estado: c.estado, Servicios: c.serviceCount,
    })));
  }

  private load(): void {
    this.loading.set(true);
    this.clientSvc.list(this.page(), this.pageSize(), this.search.value).subscribe({
      next: (r) => { this.items.set(r.items); this.total.set(r.total); this.loading.set(false); },
      error: () => { this.items.set([]); this.total.set(0); this.loading.set(false); },
    });
  }
}
