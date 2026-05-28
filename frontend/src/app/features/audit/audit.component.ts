import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatChipsModule } from '@angular/material/chips';
import { AuditService } from '../../core/api/misc.service';
import { AuditLogDto } from '../../models/dtos';
import { AuditAction } from '../../models/enums';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { exportCSV } from '../../core/api/api.helpers';

@Component({
  selector: 'app-audit',
  standalone: true,
  imports: [
    CommonModule, DatePipe, ReactiveFormsModule,
    MatCardModule, MatTableModule, MatButtonModule, MatIconModule, MatChipsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatDatepickerModule, MatNativeDateModule, MatPaginatorModule,
    BreadcrumbsComponent, SkeletonComponent, EmptyStateComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Audit Log' }]" />
      <div class="sig-page__header">
        <h1 class="sig-page__title">Audit Log</h1>
        <button mat-stroked-button (click)="onExportCSV()" data-testid="btn-exportar-csv"><mat-icon>download</mat-icon> Exportar CSV</button>
      </div>

      <mat-card style="margin-bottom: 16px;">
        <mat-card-content>
          <form [formGroup]="filterForm" class="sig-filters">
            <mat-form-field appearance="outline"><mat-label>Acción</mat-label>
              <mat-select formControlName="action" data-testid="filter-action">
                <mat-option [value]="null">Todas</mat-option>
                <mat-option value="Create">Create</mat-option>
                <mat-option value="Update">Update</mat-option>
                <mat-option value="Delete">Delete</mat-option>
                <mat-option value="Login">Login</mat-option>
                <mat-option value="Logout">Logout</mat-option>
                <mat-option value="Export">Export</mat-option>
                <mat-option value="Recalc">Recalc</mat-option>
              </mat-select>
            </mat-form-field>
            <mat-form-field appearance="outline"><mat-label>Entity Type</mat-label>
              <input matInput formControlName="entityType" data-testid="filter-entity" />
            </mat-form-field>
            <mat-form-field appearance="outline"><mat-label>Desde</mat-label>
              <input matInput [matDatepicker]="dpD" formControlName="desde" />
              <mat-datepicker-toggle matIconSuffix [for]="dpD" /><mat-datepicker #dpD />
            </mat-form-field>
            <mat-form-field appearance="outline"><mat-label>Hasta</mat-label>
              <input matInput [matDatepicker]="dpH" formControlName="hasta" />
              <mat-datepicker-toggle matIconSuffix [for]="dpH" /><mat-datepicker #dpH />
            </mat-form-field>
            <button mat-stroked-button type="button" (click)="filterForm.reset()" data-testid="btn-limpiar-filtros"><mat-icon>refresh</mat-icon> Limpiar</button>
          </form>
        </mat-card-content>
      </mat-card>

      <mat-card><mat-card-content>
        @if (loading()) { <sig-skeleton [count]="5" /> }
        @else if (items().length === 0) {
          <sig-empty-state icon="history" title="No hay registros de auditoría" />
        } @else {
          <table mat-table [dataSource]="items()" class="sig-table" data-testid="tabla-audit">
            <ng-container matColumnDef="timestamp"><th mat-header-cell *matHeaderCellDef>Fecha</th><td mat-cell *matCellDef="let r" class="mono-num">{{ r.timestamp | date:'dd/MM/yy HH:mm:ss' }}</td></ng-container>
            <ng-container matColumnDef="userId"><th mat-header-cell *matHeaderCellDef>Usuario</th><td mat-cell *matCellDef="let r">{{ r.userNombre ?? '— Sistema —' }}</td></ng-container>
            <ng-container matColumnDef="action"><th mat-header-cell *matHeaderCellDef>Acción</th><td mat-cell *matCellDef="let r"><mat-chip>{{ r.action }}</mat-chip></td></ng-container>
            <ng-container matColumnDef="entity"><th mat-header-cell *matHeaderCellDef>Entidad</th><td mat-cell *matCellDef="let r">{{ r.entityType }} #{{ r.entityId }}</td></ng-container>
            <ng-container matColumnDef="ip"><th mat-header-cell *matHeaderCellDef>IP</th><td mat-cell *matCellDef="let r" class="mono-num">{{ r.ip ?? '—' }}</td></ng-container>
            <tr mat-header-row *matHeaderRowDef="cols"></tr>
            <tr mat-row *matRowDef="let row; columns: cols" data-testid="row-audit"></tr>
          </table>
        }
        <mat-paginator [length]="total()" [pageSize]="pageSize()" [pageIndex]="page() - 1" [pageSizeOptions]="[25, 50, 100]" showFirstLastButtons (page)="onPage($event)" data-testid="paginator-audit" />
      </mat-card-content></mat-card>
    </div>
  `,
  styles: [`.sig-filters { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 12px; align-items: center; }`],
})
export class AuditComponent implements OnInit {
  private readonly auditSvc = inject(AuditService);
  private readonly fb = inject(FormBuilder);

  protected readonly items = signal<AuditLogDto[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(50);
  protected readonly loading = signal(true);
  protected readonly cols = ['timestamp', 'userId', 'action', 'entity', 'ip'];

  protected readonly filterForm = this.fb.group({
    action: [null as AuditAction | null],
    entityType: [''],
    desde: [null as Date | null],
    hasta: [null as Date | null],
  });

  ngOnInit(): void {
    this.filterForm.valueChanges.subscribe(() => { this.page.set(1); this.load(); });
    this.load();
  }

  protected onPage(e: PageEvent): void { this.pageSize.set(e.pageSize); this.page.set(e.pageIndex + 1); this.load(); }
  protected onExportCSV(): void {
    exportCSV('audit-log.csv', this.items().map((a) => ({
      Timestamp: a.timestamp, Usuario: a.userNombre ?? '', Accion: a.action, Entidad: a.entityType, EntidadId: a.entityId, IP: a.ip ?? '',
    })));
  }

  private load(): void {
    this.loading.set(true);
    const v = this.filterForm.value;
    this.auditSvc.list({
      action: v.action ?? null,
      entityType: v.entityType || null,
      desde: v.desde ? v.desde.toISOString().slice(0, 10) : null,
      hasta: v.hasta ? v.hasta.toISOString().slice(0, 10) : null,
      page: this.page(), pageSize: this.pageSize(),
    }).subscribe({
      next: (r) => { this.items.set(r.items); this.total.set(r.total); this.loading.set(false); },
      error: () => { this.items.set([]); this.total.set(0); this.loading.set(false); },
    });
  }
}
