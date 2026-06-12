import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { ClosureService } from '../../core/api/closures.service';
import { PeriodService } from '../../core/api/periods.service';
import { ClosureListItemDto, PeriodDto } from '../../models/dtos';
import { EstadoClosure, ApprovalStep } from '../../models/enums';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { EmptyStateComponent } from '../../shared/empty-state.component';
import { StateBadgeComponent } from '../../shared/state-badge.component';
import { exportCSV } from '../../core/api/api.helpers';

interface FlowStep { label: string; idx: number; done: boolean; current: boolean; rejected: boolean; }

@Component({
  selector: 'app-closures-list',
  standalone: true,
  imports: [
    CommonModule, DecimalPipe, RouterLink, ReactiveFormsModule,
    MatCardModule, MatTableModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatPaginatorModule,
    BreadcrumbsComponent, SkeletonComponent, EmptyStateComponent, StateBadgeComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Closures' }]" />
      <div class="sig-page__header">
        <h1 class="sig-page__title">Cierres</h1>
        <a mat-flat-button color="primary" routerLink="/closures/nuevo" data-testid="btn-nuevo-cierre"><mat-icon>add</mat-icon> Nuevo cierre</a>
      </div>

      <mat-card style="margin-bottom: 16px;">
        <mat-card-content>
          <div style="display: flex; gap: 16px; align-items: center; font-size: 13px; color: var(--mat-sys-on-surface-variant);">
            <strong>Flujo de aprobaci&oacute;n (5 pasos):</strong>
            <span>1 PM &rarr; 2 Backoffice &rarr; 3 Fico &rarr; 4 Direction &rarr; 5 Exportado</span>
          </div>
        </mat-card-content>
      </mat-card>

      <mat-card><mat-card-content>
        <div class="sig-table-toolbar">
          <mat-form-field appearance="outline" class="sig-search">
            <mat-icon matPrefix aria-hidden="true">search</mat-icon>
            <mat-label>Buscar servicio...</mat-label>
            <input matInput [formControl]="search" data-testid="input-busqueda" />
          </mat-form-field>
          <mat-form-field appearance="outline" style="max-width: 180px;">
            <mat-label>Período</mat-label>
            <mat-select [formControl]="periodFilter" data-testid="filter-periodo">
              <mat-option [value]="null">Todos</mat-option>
              @for (p of periodos(); track p.id) { <mat-option [value]="p.id">{{ p.nombre }}</mat-option> }
            </mat-select>
          </mat-form-field>
          <mat-form-field appearance="outline" style="max-width: 180px;">
            <mat-label>Estado</mat-label>
            <mat-select [formControl]="estadoFilter" data-testid="filter-estado">
              <mat-option [value]="null">Todos</mat-option>
              <mat-option value="Borrador">Borrador</mat-option>
              <mat-option value="EnAprobacion">En aprobación</mat-option>
              <mat-option value="Aprobado">Aprobado</mat-option>
              <mat-option value="Rechazado">Rechazado</mat-option>
              <mat-option value="Exportado">Exportado</mat-option>
            </mat-select>
          </mat-form-field>
          <button mat-stroked-button (click)="onExportCSV()" data-testid="btn-exportar-csv"><mat-icon>download</mat-icon> Exportar CSV</button>
        </div>
        @if (loading()) { <sig-skeleton [count]="5" /> }
        @else if (items().length === 0) {
          <sig-empty-state icon="lock_clock" title="No hay cierres todavía" ctaLabel="Crear primer cierre" (ctaClick)="router.navigate(['/closures/nuevo'])" />
        } @else {
          <table mat-table [dataSource]="items()" class="sig-table" data-testid="tabla-closures">
            <ng-container matColumnDef="servicio"><th mat-header-cell *matHeaderCellDef>Servicio</th><td mat-cell *matCellDef="let row">{{ row.serviceNombre }}</td></ng-container>
            <ng-container matColumnDef="periodo"><th mat-header-cell *matHeaderCellDef>Período</th><td mat-cell *matCellDef="let row">{{ row.periodNombre }}</td></ng-container>
            <ng-container matColumnDef="flujo">
              <th mat-header-cell *matHeaderCellDef>Flujo</th>
              <td mat-cell *matCellDef="let row">
                <div class="sig-flow">
                  @for (s of stepsFor(row); track s.idx) {
                    <span class="sig-flow-dot" [class.sig-flow-dot--done]="s.done" [class.sig-flow-dot--current]="s.current" [class.sig-flow-dot--rejected]="s.rejected" [title]="s.label"></span>
                    @if (s.idx < 5) { <span class="sig-flow-line" [class.sig-flow-line--done]="s.done"></span> }
                  }
                </div>
                <sig-state-badge [estado]="row.estado" [paso]="row.pasoActual" />
              </td>
            </ng-container>
            <ng-container matColumnDef="margen"><th mat-header-cell *matHeaderCellDef>Margen</th><td mat-cell *matCellDef="let row" class="mono-num">{{ row.margen | number:'1.0-2' }} €</td></ng-container>
            <ng-container matColumnDef="acciones"><th mat-header-cell *matHeaderCellDef style="text-align: right;"></th>
              <td mat-cell *matCellDef="let row">
                <a mat-icon-button [routerLink]="['/closures', row.id]" [attr.data-testid]="'btn-ver-' + row.id" aria-label="Ver"><mat-icon>arrow_forward</mat-icon></a>
              </td>
            </ng-container>
            <tr mat-header-row *matHeaderRowDef="cols"></tr>
            <tr mat-row *matRowDef="let row; columns: cols" data-testid="row-closure"></tr>
          </table>
        }
        <mat-paginator [length]="total()" [pageSize]="pageSize()" [pageIndex]="page() - 1" [pageSizeOptions]="[10, 25, 50]" showFirstLastButtons (page)="onPage($event)" data-testid="paginator-closures" />
      </mat-card-content></mat-card>
    </div>
  `,
  styles: [`
    .sig-table-toolbar { display: flex; gap: 12px; align-items: center; margin-bottom: 16px; flex-wrap: wrap; }
    .sig-search { flex: 1; max-width: 360px; }
    .sig-flow { display: flex; align-items: center; gap: 2px; margin-bottom: 4px; }
    .sig-flow-dot { width: 12px; height: 12px; border-radius: 50%; background: var(--mat-sys-outline-variant); }
    .sig-flow-dot--done { background: var(--sig-success); }
    .sig-flow-dot--current { background: var(--sig-warning); box-shadow: 0 0 0 3px var(--sig-warning-container); }
    .sig-flow-dot--rejected { background: var(--mat-sys-error); }
    .sig-flow-line { width: 16px; height: 2px; background: var(--mat-sys-outline-variant); }
    .sig-flow-line--done { background: var(--sig-success); }
  `],
})
export class ClosuresListComponent implements OnInit {
  private readonly closureSvc = inject(ClosureService);
  private readonly periodSvc = inject(PeriodService);
  protected readonly router = inject(Router);

  protected readonly items = signal<ClosureListItemDto[]>([]);
  protected readonly periodos = signal<PeriodDto[]>([]);
  protected readonly total = signal(0);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(25);
  protected readonly loading = signal(true);
  protected readonly search = new FormControl<string>('', { nonNullable: true });
  protected readonly periodFilter = new FormControl<number | null>(null);
  protected readonly estadoFilter = new FormControl<EstadoClosure | null>(null);
  protected readonly cols = ['servicio', 'periodo', 'flujo', 'margen', 'acciones'];

  ngOnInit(): void {
    this.periodSvc.list().subscribe({ next: (ps) => this.periodos.set(ps), error: () => this.periodos.set([]) });
    this.search.valueChanges.pipe(debounceTime(300), distinctUntilChanged()).subscribe(() => { this.page.set(1); this.load(); });
    this.periodFilter.valueChanges.subscribe(() => { this.page.set(1); this.load(); });
    this.estadoFilter.valueChanges.subscribe(() => { this.page.set(1); this.load(); });
    this.load();
  }
  protected onPage(e: PageEvent): void { this.pageSize.set(e.pageSize); this.page.set(e.pageIndex + 1); this.load(); }
  protected onExportCSV(): void { exportCSV('closures.csv', this.items().map((c) => ({ Id: c.id, Servicio: c.serviceNombre, Periodo: c.periodNombre, Coste: c.costeTotal, Facturacion: c.facturacionTotal, Margen: c.margen, Estado: c.estado }))); }
  protected stepsFor(row: ClosureListItemDto): FlowStep[] {
    const stepOrder: ApprovalStep[] = ['ProjectManager', 'Backoffice', 'Fico', 'Direction', 'SystemExports'];
    const labels = ['PM', 'BO', 'Fico', 'Dir', 'Export'];
    const currentIdx = stepOrder.indexOf(row.pasoActual);
    const rejected = row.estado === 'Rechazado';
    return stepOrder.map((s, i) => ({
      label: labels[i], idx: i + 1,
      done: row.estado === 'Aprobado' || row.estado === 'Exportado' ? true : i < currentIdx,
      current: i === currentIdx && !rejected && row.estado !== 'Aprobado' && row.estado !== 'Exportado',
      rejected: rejected && i === currentIdx,
    }));
  }
  private load(): void {
    this.loading.set(true);
    this.closureSvc.list({
      page: this.page(), pageSize: this.pageSize(),
      periodId: this.periodFilter.value, estado: this.estadoFilter.value,
    }).subscribe({
      next: (r) => {
        let items = r.items;
        if (this.search.value) {
          const q = this.search.value.toLowerCase();
          items = items.filter((c) => c.serviceNombre.toLowerCase().includes(q));
        }
        this.items.set(items); this.total.set(r.total); this.loading.set(false);
      },
      error: () => { this.items.set([]); this.total.set(0); this.loading.set(false); },
    });
  }
}
