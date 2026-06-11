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
import { BizneoService, StagingBizneoEmpleado, StagingBizneoAbsence } from '../services/bizneo.service';
import { debounceTime, Subject } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

// @ts-ignore - Template context variables in matRowDef not recognized by type checker
@Component({
  selector: 'app-bizneo-dashboard',
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
  ],
  template: `
    <div class="sig-page">
      <div class="page-header">
        <h1>Bizneo — Gestión RRHH</h1>
      </div>

      <!-- KPI Cards -->
      <div class="kpi-grid">
        <div class="kpi-card">
          <div class="kpi-value">{{ empleadosCount() }}</div>
          <div class="kpi-label">EMPLEADOS ACTIVOS</div>
        </div>
        <div class="kpi-card">
          <div class="kpi-value">{{ ausenciasCount() }}</div>
          <div class="kpi-label">AUSENCIAS REGISTRADAS</div>
        </div>
      </div>

      <!-- Tabs -->
      <mat-tab-group>
        <!-- Empleados Tab -->
        <mat-tab label="Empleados">
          <div class="tab-content">
            <div class="search-box">
              <mat-form-field appearance="fill">
                <mat-label>Buscar empleado</mat-label>
                <input matInput placeholder="Nombre, NIF..." (input)="onEmpleadosSearch($event)" />
                <mat-icon matSuffix>search</mat-icon>
              </mat-form-field>
            </div>

            @if (empleadosLoading()) {
              <div class="spinner-container">
                <mat-spinner diameter="48"></mat-spinner>
              </div>
            } @else if (empleados().length === 0) {
              <div class="empty-state">
                <p>Sin datos sincronizados</p>
              </div>
            } @else {
              <table mat-table [dataSource]="empleados()" class="data-table">
                <ng-container matColumnDef="nombre">
                  <th mat-header-cell>Nombre</th>
                  <td mat-cell>{{ $any(row).nombre }}</td>
                </ng-container>

                <ng-container matColumnDef="nif">
                  <th mat-header-cell>NIF</th>
                  <td mat-cell>{{ $any(row).nif }}</td>
                </ng-container>

                <ng-container matColumnDef="departamento">
                  <th mat-header-cell>Departamento</th>
                  <td mat-cell>{{ $any(row).departamento || '-' }}</td>
                </ng-container>

                <ng-container matColumnDef="sincronizado">
                  <th mat-header-cell>Sincronizado</th>
                  <td mat-cell>{{ $any(row).fechaUltimaSincronizacion | date: 'short' }}</td>
                </ng-container>

                <tr mat-header-row></tr>
                <tr mat-row *matRowDef="let row; columns: empleadosColumns"></tr>
              </table>
            }
          </div>
        </mat-tab>

        <!-- Ausencias Tab -->
        <mat-tab label="Ausencias">
          <div class="tab-content">
            <div class="search-box">
              <mat-form-field appearance="fill">
                <mat-label>Buscar ausencia</mat-label>
                <input matInput placeholder="ID de usuario..." (input)="onAusenciasSearch($event)" />
                <mat-icon matSuffix>search</mat-icon>
              </mat-form-field>
            </div>

            @if (ausenciasLoading()) {
              <div class="spinner-container">
                <mat-spinner diameter="48"></mat-spinner>
              </div>
            } @else if (ausencias().length === 0) {
              <div class="empty-state">
                <p>Sin datos sincronizados</p>
              </div>
            } @else {
              <table mat-table [dataSource]="ausencias()" class="data-table">
                <ng-container matColumnDef="userId">
                  <th mat-header-cell>ID Usuario</th>
                  <td mat-cell>{{ $any(row).userId }}</td>
                </ng-container>

                <ng-container matColumnDef="fecha">
                  <th mat-header-cell>Fecha</th>
                  <td mat-cell>{{ $any(row).fecha | date: 'short' }}</td>
                </ng-container>

                <ng-container matColumnDef="horas">
                  <th mat-header-cell>Horas</th>
                  <td mat-cell>{{ $any(row).horas }}</td>
                </ng-container>

                <ng-container matColumnDef="proyecto">
                  <th mat-header-cell>Proyecto</th>
                  <td mat-cell>{{ $any(row).projectId }}</td>
                </ng-container>

                <tr mat-header-row></tr>
                <tr mat-row *matRowDef="let row; columns: ausenciasColumns"></tr>
              </table>
            }
          </div>
        </mat-tab>
      </mat-tab-group>
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
      background: #f5f5f5;
      border-radius: 8px;
      padding: 20px;
      text-align: center;

      .kpi-value {
        font-size: 32px;
        font-weight: 600;
        color: #1976d2;
        margin-bottom: 8px;
      }

      .kpi-label {
        font-size: 12px;
        color: #999;
        text-transform: uppercase;
        letter-spacing: 0.5px;
      }
    }

    .tab-content {
      padding: 20px 0;
    }

    .search-box {
      margin-bottom: 20px;

      mat-form-field {
        width: 100%;
        max-width: 400px;
      }
    }

    .data-table {
      width: 100%;
      border-collapse: collapse;

      th {
        background-color: #f5f5f5;
        padding: 12px;
        text-align: left;
        font-weight: 600;
        font-size: 12px;
        color: #666;
        border-bottom: 1px solid #e0e0e0;
      }

      td {
        padding: 12px;
        border-bottom: 1px solid #f0f0f0;
      }

      tr:hover {
        background-color: #fafafa;
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

    mat-tab-group ::ng-deep {
      .mat-mdc-tab-labels {
        border-bottom: 1px solid #e0e0e0;
      }
    }
  `,
})
export class BizneoDashboardComponent implements OnInit {
  private readonly bizneoSvc = inject(BizneoService);

  // Dummy property for template type checking (actual row comes from *matRowDef)
  protected row: StagingBizneoEmpleado | StagingBizneoAbsence | any;

  empleados = signal<StagingBizneoEmpleado[]>([]);
  ausencias = signal<StagingBizneoAbsence[]>([]);
  empleadosLoading = signal(false);
  ausenciasLoading = signal(false);

  empleadosCount = computed(() => this.empleados().length);
  ausenciasCount = computed(() => this.ausencias().length);

  empleadosColumns = ['nombre', 'nif', 'departamento', 'sincronizado'];
  ausenciasColumns = ['userId', 'fecha', 'horas', 'proyecto'];

  private empleadosSearch$ = new Subject<string>();
  private ausenciasSearch$ = new Subject<string>();

  constructor() {
    this.empleadosSearch$
      .pipe(debounceTime(300), takeUntilDestroyed())
      .subscribe((search) => this.loadEmpleados(search));

    this.ausenciasSearch$
      .pipe(debounceTime(300), takeUntilDestroyed())
      .subscribe((search) => this.loadAusencias(search));
  }

  ngOnInit() {
    this.loadEmpleados();
    this.loadAusencias();
  }

  onEmpleadosSearch(event: Event) {
    const search = (event.target as HTMLInputElement).value;
    this.empleadosSearch$.next(search);
  }

  onAusenciasSearch(event: Event) {
    const search = (event.target as HTMLInputElement).value;
    this.ausenciasSearch$.next(search);
  }

  private loadEmpleados(search?: string) {
    this.empleadosLoading.set(true);
    this.bizneoSvc.getEmpleados(search).subscribe({
      next: (data) => {
        this.empleados.set(data);
        this.empleadosLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading empleados', err);
        this.empleados.set([]);
        this.empleadosLoading.set(false);
      },
    });
  }

  private loadAusencias(search?: string) {
    this.ausenciasLoading.set(true);
    this.bizneoSvc.getAusencias(search).subscribe({
      next: (data) => {
        this.ausencias.set(data);
        this.ausenciasLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading ausencias', err);
        this.ausencias.set([]);
        this.ausenciasLoading.set(false);
      },
    });
  }
}
