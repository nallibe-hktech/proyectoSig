import { Component, inject, OnInit, signal, computed, model } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { SelectionModel } from '@angular/cdk/collections';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatTabsModule } from '@angular/material/tabs';
import { ApprovalService } from '../../core/api/approvals.service';
import { PeriodService } from '../../core/api/periods.service';
import { ClientService } from '../../core/api/clients.service';
import { CostCenterService } from '../../core/api/catalogs.service';
import { DepartmentService } from '../../core/api/catalogs.service';
import { ApprovalPanelItemDto, PeriodDto, ClientListItemDto, CostCenterDto, DepartmentDto } from '../../models/dtos';
import { EstadoClosure, TipoConcepto } from '../../models/enums';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { StateBadgeComponent } from '../../shared/state-badge.component';

@Component({
  selector: 'app-approvals',
  standalone: true,
  imports: [
    CommonModule, DecimalPipe, RouterLink, ReactiveFormsModule,
    MatCardModule, MatTableModule, MatCheckboxModule, MatButtonModule, MatIconModule, MatFormFieldModule,
    MatSelectModule, MatPaginatorModule, MatTabsModule,
    BreadcrumbsComponent, SkeletonComponent, EmptyStateComponent, StateBadgeComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Aprobaciones' }]" />

      <div class="sig-page__header">
        <h1 class="sig-page__title">Panel de aprobaciones</h1>
        <div style="display: flex; gap: 8px;">
          <a mat-stroked-button routerLink="/approvals" [class.sig-tab-active]="!onlyPendientes()" data-testid="tab-todos">Todos</a>
          <a mat-stroked-button routerLink="/approvals/pendientes" [class.sig-tab-active]="onlyPendientes()" data-testid="tab-pendientes">Mis pendientes</a>
        </div>
      </div>

      @if (!onlyPendientes()) {
        <mat-card style="margin-bottom: 16px;">
          <mat-card-header><mat-card-title style="font-size: 14px;">Filtros</mat-card-title></mat-card-header>
          <mat-card-content>
            <form [formGroup]="filterForm" class="sig-filters">
              <mat-form-field appearance="outline"><mat-label>Período</mat-label>
                <mat-select formControlName="periodId" data-testid="filter-periodo">
                  <mat-option [value]="null">Todos</mat-option>
                  @for (p of periodos(); track p.id) { <mat-option [value]="p.id">{{ p.nombre }}</mat-option> }
                </mat-select>
              </mat-form-field>
              <mat-form-field appearance="outline"><mat-label>Cliente</mat-label>
                <mat-select formControlName="clientId" data-testid="filter-cliente">
                  <mat-option [value]="null">Todos</mat-option>
                  @for (c of clients(); track c.id) { <mat-option [value]="c.id">{{ c.nombre }}</mat-option> }
                </mat-select>
              </mat-form-field>
              <mat-form-field appearance="outline"><mat-label>CECO</mat-label>
                <mat-select formControlName="costCenterId" data-testid="filter-ceco">
                  <mat-option [value]="null">Todos</mat-option>
                  @for (cc of ccs(); track cc.id) { <mat-option [value]="cc.id">{{ cc.codigo }} - {{ cc.nombre }}</mat-option> }
                </mat-select>
              </mat-form-field>
              <mat-form-field appearance="outline"><mat-label>Estado</mat-label>
                <mat-select formControlName="estado" data-testid="filter-estado">
                  <mat-option [value]="null">Todos</mat-option>
                  <mat-option value="Borrador">Borrador</mat-option>
                  <mat-option value="EnAprobacion">En aprobación</mat-option>
                  <mat-option value="Aprobado">Aprobado</mat-option>
                  <mat-option value="Rechazado">Rechazado</mat-option>
                  <mat-option value="Exportado">Exportado</mat-option>
                </mat-select>
              </mat-form-field>
              <mat-form-field appearance="outline"><mat-label>Departamento</mat-label>
                <mat-select formControlName="departmentId" data-testid="filter-depto">
                  <mat-option [value]="null">Todos</mat-option>
                  @for (d of depts(); track d.id) { <mat-option [value]="d.id">{{ d.nombre }}</mat-option> }
                </mat-select>
              </mat-form-field>
              <mat-form-field appearance="outline"><mat-label>Tipo concepto</mat-label>
                <mat-select formControlName="tipo" data-testid="filter-tipo">
                  <mat-option [value]="null">Todos</mat-option>
                  <mat-option value="Pago">Pago</mat-option>
                  <mat-option value="Factura">Factura</mat-option>
                </mat-select>
              </mat-form-field>
              <button mat-stroked-button type="button" (click)="clearFilters()" data-testid="btn-limpiar-filtros"><mat-icon>refresh</mat-icon> Limpiar filtros</button>
            </form>
          </mat-card-content>
        </mat-card>
      }

      <mat-card><mat-card-content>
        @if (selection.selected.length > 0) {
          <div class="sig-batch-bar" data-testid="batch-bar">
            <span class="sig-batch-count">{{ selection.selected.length }} seleccionados</span>
            <button mat-flat-button color="primary" (click)="batchApprove()" data-testid="btn-aprobar-lote"><mat-icon>check_circle</mat-icon> Aprobar</button>
            <button mat-stroked-button color="warn" (click)="batchReject()" data-testid="btn-rechazar-lote"><mat-icon>cancel</mat-icon> Rechazar</button>
            <button mat-icon-button (click)="selection.clear()" data-testid="btn-clear-selection" aria-label="Limpiar selección"><mat-icon>close</mat-icon></button>
          </div>
        }
        @if (loading()) { <sig-skeleton [count]="5" /> }
        @else if (items().length === 0) {
          <sig-empty-state icon="approval" [title]="onlyPendientes() ? 'No tienes aprobaciones pendientes' : 'No hay cierres'"
            [description]="onlyPendientes() ? 'Buen trabajo. No hay nada esperando tu acción.' : ''" />
        } @else {
          <div class="sig-table-scroll">
            <table mat-table [dataSource]="items()" class="sig-table" data-testid="tabla-approvals">
              <ng-container matColumnDef="select">
                <th mat-header-cell *matHeaderCellDef>
                  <mat-checkbox (change)="$event ? toggleAllRows() : null" [checked]="selection.hasValue() && isAllSelected()" [indeterminate]="selection.hasValue() && !isAllSelected()" [attr.aria-label]="'Seleccionar todo'" />
                </th>
                <td mat-cell *matCellDef="let row">
                  <mat-checkbox (click)="$event.stopPropagation()" (change)="$event ? selection.toggle(row) : null" [checked]="selection.isSelected(row)" [attr.aria-label]="'Seleccionar fila'" />
                </td>
              </ng-container>
              <ng-container matColumnDef="proyecto"><th mat-header-cell *matHeaderCellDef>PROYECTO</th><td mat-cell *matCellDef="let row">{{ row.projectNombre }}</td></ng-container>
              <ng-container matColumnDef="cliente"><th mat-header-cell *matHeaderCellDef>CLIENTE</th><td mat-cell *matCellDef="let row">{{ row.clientNombre }}</td></ng-container>
              <ng-container matColumnDef="periodo"><th mat-header-cell *matHeaderCellDef>PERÍODO</th><td mat-cell *matCellDef="let row">{{ row.periodNombre }}</td></ng-container>
              <ng-container matColumnDef="estado"><th mat-header-cell *matHeaderCellDef>ESTADO</th>
                <td mat-cell *matCellDef="let row"><sig-state-badge [estado]="row.estado" [paso]="row.pasoActual" /></td>
              </ng-container>
              <ng-container matColumnDef="margen"><th mat-header-cell *matHeaderCellDef>MARGEN</th><td mat-cell *matCellDef="let row" class="mono-num">{{ row.margen | number:'1.0-2' }} €</td></ng-container>
              <ng-container matColumnDef="acciones"><th mat-header-cell *matHeaderCellDef></th>
                <td mat-cell *matCellDef="let row"><a mat-icon-button [routerLink]="['/closures', row.closureId]" [attr.data-testid]="'btn-ver-' + row.closureId" aria-label="Ver detalle"><mat-icon>arrow_forward</mat-icon></a></td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="cols"></tr>
              <tr mat-row *matRowDef="let row; columns: cols"
                [class.sig-row-pending]="row.estado === 'EnAprobacion'"
                data-testid="row-approval"></tr>
            </table>
          </div>
        }
        <mat-paginator [length]="total()" [pageSize]="pageSize()" [pageIndex]="page() - 1" [pageSizeOptions]="[10, 25, 50]" showFirstLastButtons (page)="onPage($event)" data-testid="paginator-approvals" />
      </mat-card-content></mat-card>
    </div>
  `,
  styles: [`
    .sig-filters { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 12px; align-items: center; }
    .sig-tab-active { background: var(--mat-sys-primary-container); color: var(--mat-sys-primary); }
    .sig-batch-bar {
      display: flex; align-items: center; gap: 12px;
      padding: 10px 16px; margin-bottom: 12px;
      border-radius: 8px; background: #E3F2FD;
    }
    .sig-batch-count { font-size: 13px; font-weight: 600; color: #1565C0; margin-right: auto; }
    .sig-row-pending { background: #FFF8E1 !important; }
    .sig-table-scroll { overflow-x: auto; }
    :host ::ng-deep .sig-table th.mat-header-cell {
      background: #1F4E78 !important; color: rgba(255,255,255,0.85) !important;
      font-size: 11px; font-weight: 700; letter-spacing: 0.5px;
    }
  `],
})
export class ApprovalsComponent implements OnInit {
  private readonly approvalSvc = inject(ApprovalService);
  private readonly periodSvc = inject(PeriodService);
  private readonly clientSvc = inject(ClientService);
  private readonly ccSvc = inject(CostCenterService);
  private readonly deptSvc = inject(DepartmentService);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);

  protected readonly items = signal<ApprovalPanelItemDto[]>([]);
  protected readonly periodos = signal<PeriodDto[]>([]);
  protected readonly clients = signal<ClientListItemDto[]>([]);
  protected readonly ccs = signal<CostCenterDto[]>([]);
  protected readonly depts = signal<DepartmentDto[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(25);
  protected readonly loading = signal(true);
  protected readonly cols = ['select', 'proyecto', 'cliente', 'periodo', 'estado', 'margen', 'acciones'];
  protected readonly selection = new SelectionModel<ApprovalPanelItemDto>(true, []);

  protected isAllSelected(): boolean {
    return this.selection.selected.length === this.items().length;
  }
  protected toggleAllRows(): void {
    this.isAllSelected() ? this.selection.clear() : this.items().forEach((r) => this.selection.select(r));
  }
  protected batchApprove(): void {
    const ids = this.selection.selected.map((r) => r.closureId);
    this.approvalSvc.batchApprove(ids).subscribe({ next: () => { this.selection.clear(); this.load(); } });
  }
  protected batchReject(): void {
    const ids = this.selection.selected.map((r) => r.closureId);
    this.approvalSvc.batchReject(ids).subscribe({ next: () => { this.selection.clear(); this.load(); } });
  }

  protected readonly onlyPendientes = signal(false);

  protected readonly filterForm = this.fb.group({
    periodId: [null as number | null],
    clientId: [null as number | null],
    costCenterId: [null as number | null],
    estado: [null as EstadoClosure | null],
    userId: [null as number | null],
    departmentId: [null as number | null],
    tipo: [null as TipoConcepto | null],
    conceptId: [null as number | null],
  });

  ngOnInit(): void {
    this.onlyPendientes.set(this.route.snapshot.data['onlyPendientes'] === true);
    this.route.data.subscribe((d) => {
      this.onlyPendientes.set(d['onlyPendientes'] === true);
      this.load();
    });
    this.periodSvc.list().subscribe({ next: (ps) => this.periodos.set(ps) });
    this.clientSvc.list(1, 200).subscribe({ next: (r) => this.clients.set(r.items), error: () => {} });
    this.ccSvc.list().subscribe({ next: (ccs) => this.ccs.set(ccs), error: () => {} });
    this.deptSvc.list().subscribe({ next: (d) => this.depts.set(d), error: () => {} });
    this.filterForm.valueChanges.subscribe(() => { this.page.set(1); this.load(); });
    this.load();
  }

  protected onPage(e: PageEvent): void { this.pageSize.set(e.pageSize); this.page.set(e.pageIndex + 1); this.load(); }
  protected clearFilters(): void { this.filterForm.reset({}); }

  private load(): void {
    this.loading.set(true);
    const obs = this.onlyPendientes()
      ? this.approvalSvc.pendientes(this.page(), this.pageSize())
      : this.approvalSvc.list({ ...this.filterForm.value, page: this.page(), pageSize: this.pageSize() });
    obs.subscribe({
      next: (r) => { this.items.set(r.items); this.total.set(r.total); this.loading.set(false); },
      error: () => { this.items.set([]); this.total.set(0); this.loading.set(false); },
    });
  }
}
