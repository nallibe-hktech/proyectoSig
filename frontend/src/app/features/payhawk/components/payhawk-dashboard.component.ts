import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { PayHawkService, StagingPayHawkGasto } from '../services/payhawk.service';
import { debounceTime, Subject } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

// @ts-ignore - Template context variables in matRowDef not recognized by type checker
@Component({
  selector: 'app-payhawk-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatInputModule,
    MatFormFieldModule,
    MatProgressSpinnerModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatDividerModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatPaginatorModule,
  ],
  template: `
    <div class="sig-page">
      <div class="page-header">
        <h1>PayHawk — Gestión de Gastos</h1>
      </div>

      <!-- KPI Cards -->
      <div class="kpi-grid">
        <div class="kpi-card">
          <div class="kpi-value">{{ gastosCount() }}</div>
          <div class="kpi-label">GASTOS REGISTRADOS</div>
        </div>
        <div class="kpi-card">
          <div class="kpi-value">€ {{ importeTotal() | number: '1.2-2' }}</div>
          <div class="kpi-label">IMPORTE TOTAL</div>
        </div>
        <div class="kpi-card">
          <div class="kpi-value">{{ categoriasUnicas() }}</div>
          <div class="kpi-label">CATEGORÍAS</div>
        </div>
      </div>

      <!-- Filters -->
      <div class="filters">
        <mat-form-field appearance="fill">
          <mat-label>Buscar categoría</mat-label>
          <input matInput placeholder="Tipo de gasto..." (input)="onSearch($event)" />
          <mat-icon matSuffix>search</mat-icon>
        </mat-form-field>

        <mat-form-field appearance="fill">
          <mat-label>Desde</mat-label>
          <input matInput type="date" (change)="onDesdeChange($event)" />
        </mat-form-field>

        <mat-form-field appearance="fill">
          <mat-label>Hasta</mat-label>
          <input matInput type="date" (change)="onHastaChange($event)" />
        </mat-form-field>
      </div>

      <!-- Gastos Table -->
      @if (loading()) {
        <div class="spinner-container">
          <mat-spinner diameter="48"></mat-spinner>
        </div>
      } @else if (gastos().length === 0) {
        <div class="empty-state">
          <p>Sin datos sincronizados</p>
        </div>
      } @else {
        <table mat-table [dataSource]="gastosPaginated()" class="data-table">
          <ng-container matColumnDef="fecha">
            <th mat-header-cell>Fecha</th>
            <td mat-cell *matCellDef="let row">{{ row.fecha | date: 'short' }}</td>
          </ng-container>

          <ng-container matColumnDef="categoria">
            <th mat-header-cell>Categoría</th>
            <td mat-cell *matCellDef="let row">{{ row.categoria }}</td>
          </ng-container>

          <ng-container matColumnDef="importe">
            <th mat-header-cell>Importe</th>
            <td mat-cell *matCellDef="let row">€ {{ row.importe | number: '1.2-2' }}</td>
          </ng-container>

          <ng-container matColumnDef="userId">
            <th mat-header-cell>ID Usuario</th>
            <td mat-cell *matCellDef="let row">{{ row.userId }}</td>
          </ng-container>

          <ng-container matColumnDef="projectId">
            <th mat-header-cell>Proyecto</th>
            <td mat-cell *matCellDef="let row">{{ row.projectId }}</td>
          </ng-container>

          <tr mat-header-row></tr>
          <tr mat-row *matRowDef="let row; columns: columns"></tr>
        </table>
        <mat-paginator
          [length]="gastosTotal()"
          [pageSize]="gastosPageSize()"
          [pageIndex]="gastosPage() - 1"
          [pageSizeOptions]="[10, 25, 50]"
          showFirstLastButtons
          (page)="onPageChange($event)"
        ></mat-paginator>
      }
    </div>
  `,
  styles: `
    .sig-page {
      padding: 20px;
    }

    .page-header {
      margin-bottom: 30px;

      h1 {
        margin: 0;
        font-size: 28px;
        font-weight: 500;
      }
    }

    .kpi-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 16px;
      margin-bottom: 30px;
    }

    .kpi-card {
      background: var(--sig-bg-card);
      border-radius: 8px;
      padding: 20px;
      text-align: center;
      border: 1px solid var(--sig-border);

      .kpi-value {
        font-size: 32px;
        font-weight: 600;
        color: var(--sig-blue);
        margin-bottom: 8px;
      }

      .kpi-label {
        font-size: 12px;
        color: var(--sig-text-muted);
        text-transform: uppercase;
        letter-spacing: 0.5px;
      }
    }

    .filters {
      display: flex;
      gap: 16px;
      margin-bottom: 20px;
      flex-wrap: wrap;

      mat-form-field {
        min-width: 200px;
      }
    }

    .data-table {
      width: 100%;
      border-collapse: collapse;

      th {
        background-color: var(--sig-bg-header);
        padding: 12px;
        text-align: left;
        font-weight: 600;
        font-size: 12px;
        color: var(--sig-text-muted);
        border-bottom: 1px solid var(--sig-border);
      }

      td {
        padding: 12px;
        border-bottom: 1px solid var(--sig-border);
      }

      tr:hover {
        background-color: var(--sig-bg-hover);
      }
    }

    .spinner-container {
      display: flex;
      justify-content: center;
      padding: 40px 20px;
    }

    .empty-state {
      text-align: center;
      padding: 60px 20px;
      color: #999;

      p {
        margin: 0;
        font-size: 16px;
      }
    }
  `,
})
export class PayHawkDashboardComponent implements OnInit {
  private readonly payHawkSvc = inject(PayHawkService);

  protected readonly gastos = signal<StagingPayHawkGasto[]>([]);
  protected readonly gastosTotal = signal(0);
  protected readonly gastosPage = signal(1);
  protected readonly gastosPageSize = signal(25);
  protected readonly importeTotalValue = signal(0);
  protected readonly categoriasUnicasTotal = signal(0);
  protected readonly loading = signal(false);

  protected readonly gastosPaginated = computed(() => {
    const start = (this.gastosPage() - 1) * this.gastosPageSize();
    const end = start + this.gastosPageSize();
    return this.gastos().slice(start, end);
  });

  columns = ['fecha', 'categoria', 'importe', 'userId', 'projectId'];

  gastosCount = computed(() => this.gastosTotal());
  importeTotal = computed(() => this.importeTotalValue());
  categoriasUnicas = computed(() => this.categoriasUnicasTotal());

  private search$ = new Subject<string>();
  private desde: string | undefined;
  private hasta: string | undefined;

  constructor() {
    this.search$
      .pipe(debounceTime(300), takeUntilDestroyed())
      .subscribe((search) => { this.gastosPage.set(1); this.loadGastos(search); });
  }

  ngOnInit() {
    this.loadGastos();
  }

  protected onSearch(event: Event) {
    const search = (event.target as HTMLInputElement).value;
    this.search$.next(search);
  }

  protected onDesdeChange(event: Event) {
    const input = event.target as HTMLInputElement;
    this.desde = input.value ? new Date(input.value).toISOString().split('T')[0] : undefined;
    this.gastosPage.set(1);
    this.loadGastos();
  }

  protected onHastaChange(event: Event) {
    const input = event.target as HTMLInputElement;
    this.hasta = input.value ? new Date(input.value).toISOString().split('T')[0] : undefined;
    this.gastosPage.set(1);
    this.loadGastos();
  }

  protected onPageChange(event: PageEvent): void {
    this.gastosPageSize.set(event.pageSize);
    this.gastosPage.set(event.pageIndex + 1);
    window.scrollTo({ top: 0, behavior: 'smooth' });
    this.loadGastos();
  }

  protected loadGastos(search?: string) {
    this.loading.set(true);
    this.payHawkSvc.getGastos(search, this.desde, this.hasta).subscribe({
      next: (data) => {
        const gastos = data ?? [];
        this.gastos.set(gastos);
        this.gastosTotal.set(gastos.length);
        this.importeTotalValue.set(gastos.reduce((sum, g) => sum + (g.importe || 0), 0));
        this.categoriasUnicasTotal.set(new Set(gastos.map(g => g.categoria)).size);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading gastos', err);
        this.gastos.set([]);
        this.gastosTotal.set(0);
        this.importeTotalValue.set(0);
        this.categoriasUnicasTotal.set(0);
        this.loading.set(false);
      },
    });
  }
}
