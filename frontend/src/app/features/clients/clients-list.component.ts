import { Component, inject, OnInit, signal } from '@angular/core';
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
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ClientService } from '../../core/api/clients.service';
import { ClientListItemDto } from '../../models/dtos';
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
    MatFormFieldModule, MatInputModule, MatPaginatorModule, MatSortModule, MatDialogModule,
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
            <button mat-stroked-button (click)="onExportCSV()" data-testid="btn-exportar-csv">
              <mat-icon>download</mat-icon> Exportar CSV
            </button>
          </div>

          @if (loading()) {
            <sig-skeleton [count]="5" />
          } @else if (items().length === 0) {
            <sig-empty-state
              icon="groups"
              title="No hay clientes todavía"
              description="Crea el primer cliente para empezar a gestionar servicios y cierres."
              ctaLabel="Crear primer client"
              [hasFilter]="!!search.value"
              (ctaClick)="onEmptyCta()"
            />
          } @else {
            <table mat-table [dataSource]="items()" class="sig-table" data-testid="tabla-clients">
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
              <ng-container matColumnDef="serviceCount">
                <th mat-header-cell *matHeaderCellDef>Servicios</th>
                <td mat-cell *matCellDef="let row" class="mono-num">{{ row.serviceCount }}</td>
              </ng-container>
              <ng-container matColumnDef="acciones">
                <th mat-header-cell *matHeaderCellDef style="text-align: right;">Acciones</th>
                <td mat-cell *matCellDef="let row">
                  <div class="sig-table-actions">
                    <a mat-icon-button [routerLink]="['/clients', row.id]" [attr.data-testid]="'btn-ver-' + row.id" aria-label="Ver detalle">
                      <mat-icon>visibility</mat-icon>
                    </a>
                    <a mat-icon-button [routerLink]="['/clients', row.id, 'editar']" [attr.data-testid]="'btn-editar-' + row.id" aria-label="Editar">
                      <mat-icon>edit</mat-icon>
                    </a>
                    <button mat-icon-button (click)="onDelete(row)" [attr.data-testid]="'btn-eliminar-' + row.id" aria-label="Eliminar">
                      <mat-icon>delete</mat-icon>
                    </button>
                  </div>
                </td>
              </ng-container>

              <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: displayedColumns" data-testid="row-client"></tr>
            </table>
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

  protected readonly displayedColumns = ['nombre', 'nif', 'ciudad', 'serviceCount', 'acciones'];

  ngOnInit(): void {
    this.search.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged())
      .subscribe(() => {
        this.page.set(1);
        this.load();
      });
    this.load();
  }

  protected onPage(e: PageEvent): void {
    this.pageSize.set(e.pageSize);
    this.page.set(e.pageIndex + 1);
    this.load();
  }

  protected onEmptyCta(): void {
    if (this.search.value) {
      this.search.setValue('');
    } else {
      void this.router.navigate(['/clients/nuevo']);
    }
  }

  protected onDelete(row: ClientListItemDto): void {
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
        next: () => { this.notify.success('Client eliminado'); this.load(); },
        error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo eliminar'),
      });
    });
  }

  protected onExportCSV(): void {
    exportCSV('clients.csv', this.items().map((c) => ({
      Id: c.id, Nombre: c.nombre, NIF: c.nif, Ciudad: c.ciudad ?? '', Servicios: c.serviceCount,
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
