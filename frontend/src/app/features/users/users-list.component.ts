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
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { UserService } from '../../core/api/users.service';
import { UserListItemDto } from '../../models/dtos';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { NotifyService } from '../../core/notify.service';
import { exportCSV } from '../../core/api/api.helpers';

@Component({
  selector: 'app-users-list',
  standalone: true,
  imports: [
    CommonModule, RouterLink, ReactiveFormsModule,
    MatCardModule, MatTableModule, MatButtonModule, MatIconModule, MatChipsModule,
    MatFormFieldModule, MatInputModule, MatPaginatorModule, MatDialogModule,
    BreadcrumbsComponent, SkeletonComponent, EmptyStateComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Users' }]" />
      <div class="sig-page__header">
        <h1 class="sig-page__title">Users</h1>
        <a mat-flat-button color="primary" routerLink="/users/nuevo" data-testid="btn-nuevo"><mat-icon>add</mat-icon> Nuevo Usuario</a>
      </div>
      <mat-card><mat-card-content>
        <div class="sig-table-toolbar">
          <mat-form-field appearance="outline" class="sig-search">
            <mat-icon matPrefix aria-hidden="true">search</mat-icon>
            <mat-label>Buscar...</mat-label>
            <input matInput [formControl]="search" data-testid="input-busqueda" />
          </mat-form-field>
          <button mat-stroked-button (click)="onExportCSV()" data-testid="btn-exportar-csv"><mat-icon>download</mat-icon> Exportar CSV</button>
        </div>
        @if (loading()) { <sig-skeleton [count]="5" /> }
        @else if (items().length === 0) {
          <sig-empty-state icon="manage_accounts" title="No hay usuarios" ctaLabel="Crear primer usuario" [hasFilter]="!!search.value" (ctaClick)="router.navigate(['/users/nuevo'])" />
        } @else {
          <table mat-table [dataSource]="items()" class="sig-table" data-testid="tabla-users">
            <ng-container matColumnDef="nombre"><th mat-header-cell *matHeaderCellDef>Nombre</th><td mat-cell *matCellDef="let row">{{ row.nombre }} {{ row.apellidos }}</td></ng-container>
            <ng-container matColumnDef="email"><th mat-header-cell *matHeaderCellDef>Email</th><td mat-cell *matCellDef="let row">{{ row.email }}</td></ng-container>
            <ng-container matColumnDef="nif"><th mat-header-cell *matHeaderCellDef>NIF</th><td mat-cell *matCellDef="let row" class="mono-num">{{ row.nif }}</td></ng-container>
            <ng-container matColumnDef="roles"><th mat-header-cell *matHeaderCellDef>Roles</th><td mat-cell *matCellDef="let row">
              @for (r of row.roles; track r) { <mat-chip>{{ r }}</mat-chip> }
            </td></ng-container>
            <ng-container matColumnDef="estado"><th mat-header-cell *matHeaderCellDef>Estado</th>
              <td mat-cell *matCellDef="let row"><span [class]="row.estado === 'Activo' ? 'sig-badge sig-badge--approved' : 'sig-badge sig-badge--closed'">{{ row.estado }}</span></td>
            </ng-container>
            <ng-container matColumnDef="acciones"><th mat-header-cell *matHeaderCellDef style="text-align: right;">Acciones</th>
              <td mat-cell *matCellDef="let row">
                <div class="sig-table-actions">
                  <a mat-icon-button [routerLink]="['/users', row.id]" [attr.data-testid]="'btn-ver-' + row.id" aria-label="Ver"><mat-icon>visibility</mat-icon></a>
                  <a mat-icon-button [routerLink]="['/users', row.id, 'editar']" [attr.data-testid]="'btn-editar-' + row.id" aria-label="Editar"><mat-icon>edit</mat-icon></a>
                  <button mat-icon-button (click)="onDelete(row)" [attr.data-testid]="'btn-eliminar-' + row.id" aria-label="Eliminar"><mat-icon>delete</mat-icon></button>
                </div>
              </td>
            </ng-container>
            <tr mat-header-row *matHeaderRowDef="cols"></tr>
            <tr mat-row *matRowDef="let row; columns: cols" data-testid="row-user"></tr>
          </table>
        }
        <mat-paginator [length]="total()" [pageSize]="pageSize()" [pageIndex]="page() - 1" [pageSizeOptions]="[10, 25, 50]" showFirstLastButtons (page)="onPage($event)" data-testid="paginator-users" />
      </mat-card-content></mat-card>
    </div>
  `,
  styles: [`.sig-table-toolbar { display: flex; gap: 12px; align-items: center; margin-bottom: 16px; } .sig-search { flex: 1; max-width: 480px; }`],
})
export class UsersListComponent implements OnInit {
  private readonly userSvc = inject(UserService);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotifyService);
  protected readonly router = inject(Router);

  protected readonly items = signal<UserListItemDto[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(25);
  protected readonly loading = signal(true);
  protected readonly search = new FormControl<string>('', { nonNullable: true });
  protected readonly cols = ['nombre', 'email', 'nif', 'roles', 'estado', 'acciones'];

  ngOnInit(): void {
    this.search.valueChanges.pipe(debounceTime(300), distinctUntilChanged()).subscribe(() => { this.page.set(1); this.load(); });
    this.load();
  }
  protected onPage(e: PageEvent): void { this.pageSize.set(e.pageSize); this.page.set(e.pageIndex + 1); this.load(); }
  protected onDelete(row: UserListItemDto): void {
    this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Eliminar Usuario', message: 'Acción irreversible.', entityName: row.nombre + ' ' + row.apellidos, destructive: true }, minWidth: 480,
    }).afterClosed().subscribe((ok) => {
      if (!ok) return;
      this.userSvc.delete(row.id).subscribe({
        next: () => { this.notify.success('Usuario eliminado'); this.load(); },
        error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo eliminar'),
      });
    });
  }
  protected onExportCSV(): void { exportCSV('users.csv', this.items().map((u) => ({ Id: u.id, Nombre: u.nombre, Apellidos: u.apellidos, Email: u.email, NIF: u.nif, Roles: u.roles.join(', '), Estado: u.estado }))); }
  private load(): void {
    this.loading.set(true);
    this.userSvc.list(this.page(), this.pageSize(), this.search.value).subscribe({
      next: (r) => { this.items.set(r.items); this.total.set(r.total); this.loading.set(false); },
      error: () => { this.items.set([]); this.total.set(0); this.loading.set(false); },
    });
  }
}
