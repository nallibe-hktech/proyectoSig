import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTabsModule } from '@angular/material/tabs';
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
import { IntratimeService, StagingIntratimeFichaje } from '../services/intratime.service';
import { debounceTime, Subject } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

// @ts-ignore - Template context variables in matRowDef not recognized by type checker
@Component({
  selector: 'app-intratime-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTabsModule,
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
  ],
  template: `
    <div class="sig-page">
      <div class="page-header">
        <h1>Intratime — Control de Fichajes</h1>
      </div>

      <!-- KPI Cards -->
      <div class="kpi-grid">
        <div class="kpi-card">
          <div class="kpi-value">{{ fichajesCount() }}</div>
          <div class="kpi-label">FICHAJES REGISTRADOS</div>
        </div>
        <div class="kpi-card">
          <div class="kpi-value">{{ empleadosUnicos() }}</div>
          <div class="kpi-label">EMPLEADOS CON FICHAJES</div>
        </div>
        <div class="kpi-card">
          <div class="kpi-value">{{ horasTotal() | number: '1.1-1' }}</div>
          <div class="kpi-label">TOTAL HORAS TRABAJADAS</div>
        </div>
      </div>

      <!-- Filters -->
      <div class="filters">
        <mat-form-field appearance="fill">
          <mat-label>Buscar empleado</mat-label>
          <input matInput placeholder="ID de empleado..." (input)="onSearch($event)" />
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

      <!-- Fichajes Table -->
      @if (loading()) {
        <div class="spinner-container">
          <mat-spinner diameter="48"></mat-spinner>
        </div>
      } @else if (fichajes().length === 0) {
        <div class="empty-state">
          <p>Sin datos sincronizados</p>
        </div>
      } @else {
        <table mat-table [dataSource]="fichajes()" class="data-table">
          <ng-container matColumnDef="userIdExterno">
            <th mat-header-cell>ID Empleado</th>
            <td mat-cell *matCellDef="let row">{{ row.userIdExterno }}</td>
          </ng-container>

          <ng-container matColumnDef="entrada">
            <th mat-header-cell>Entrada</th>
            <td mat-cell *matCellDef="let row">{{ row.entrada | date: 'short' }}</td>
          </ng-container>

          <ng-container matColumnDef="salida">
            <th mat-header-cell>Salida</th>
            <td mat-cell *matCellDef="let row">{{ row.salida ? (row.salida | date: 'short') : '-' }}</td>
          </ng-container>

          <ng-container matColumnDef="horasCalculadas">
            <th mat-header-cell>Horas</th>
            <td mat-cell *matCellDef="let row">{{ row.horasCalculadas || '-' }}</td>
          </ng-container>

          <tr mat-header-row></tr>
          <tr mat-row *matRowDef="let row; columns: columns"></tr>
        </table>
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
export class IntratimedashboardComponent implements OnInit {
  private readonly intratimeSvc = inject(IntratimeService);

  // Dummy property for template type checking (actual row comes from *matRowDef)
  protected row: StagingIntratimeFichaje | any;

  fichajes = signal<StagingIntratimeFichaje[]>([]);
  loading = signal(false);

  columns = ['userIdExterno', 'entrada', 'salida', 'horasCalculadas'];

  fichajesCount = computed(() => this.fichajes().length);
  empleadosUnicos = computed(() =>
    new Set(this.fichajes().map(f => f.userIdExterno)).size
  );
  horasTotal = computed(() =>
    this.fichajes().reduce((sum, f) => sum + (f.horasCalculadas || 0), 0)
  );

  private search$ = new Subject<string>();
  private desde: string | undefined;
  private hasta: string | undefined;

  constructor() {
    this.search$
      .pipe(debounceTime(300), takeUntilDestroyed())
      .subscribe((search) => this.loadFichajes(search));
  }

  ngOnInit() {
    this.loadFichajes();
  }

  onSearch(event: Event) {
    const search = (event.target as HTMLInputElement).value;
    this.search$.next(search);
  }

  onDesdeChange(event: Event) {
    const input = event.target as HTMLInputElement;
    this.desde = input.value ? new Date(input.value).toISOString().split('T')[0] : undefined;
    this.loadFichajes();
  }

  onHastaChange(event: Event) {
    const input = event.target as HTMLInputElement;
    this.hasta = input.value ? new Date(input.value).toISOString().split('T')[0] : undefined;
    this.loadFichajes();
  }

  private loadFichajes(search?: string) {
    this.loading.set(true);
    this.intratimeSvc.getFichajes(search, this.desde, this.hasta).subscribe({
      next: (data) => {
        this.fichajes.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Error loading fichajes', err);
        this.fichajes.set([]);
        this.loading.set(false);
      },
    });
  }
}
