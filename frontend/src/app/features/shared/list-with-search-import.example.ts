/**
 * EXAMPLE: Cómo integrar búsqueda full-text + importación Excel en un listado.
 *
 * Este archivo es solo una referencia. Cópialo y adaptalo a tu componente específico.
 *
 * Componentes necesarios:
 * - FulltextSearchComponent
 * - ExcelImportComponent
 *
 * Ejemplo de uso en projects.component.ts:
 */

/*
import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatTabsModule } from '@angular/material/tabs';
import { FulltextSearchComponent } from '../../shared/fulltext-search.component';
import { ExcelImportComponent, ExcelImportConfig, ExcelImportRow } from '../../shared/excel-import.component';
import { ProjectService } from '../../core/api/projects.service';
import { NotifyService } from '../../core/notify.service';
import { ProjectDto } from '../../models/dtos';

@Component({
  selector: 'app-projects-list',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatCardModule, MatButtonModule, MatIconModule, MatTableModule, MatTabsModule,
    FulltextSearchComponent, ExcelImportComponent,
  ],
  template: `
    <div class="sig-page">
      <div class="sig-page__header">
        <h1 class="sig-page__title">Proyectos</h1>
        <a mat-flat-button color="primary" routerLink="/projects/new">
          <mat-icon>add</mat-icon>
          Nuevo Proyecto
        </a>
      </div>

      <mat-tab-group>
        <!-- Pestaña 1: Listado con búsqueda -->
        <mat-tab>
          <ng-template mat-tab-label>
            <mat-icon>list</mat-icon>
            Listado ({{ filteredProjects().length }})
          </ng-template>

          <mat-card style="margin-top: 16px;">
            <mat-card-content>
              <!-- Componente de búsqueda -->
              <app-fulltext-search
                [items]="projects()"
                [searchFields]="['nombre', 'clientNombre', 'codigo']"
                placeholder="Buscar por nombre, cliente, código..."
                (resultsChange)="onSearchResults($event)"
              />
            </mat-card-content>
          </mat-card>

          <!-- Tabla con resultados -->
          @if (filteredProjects().length > 0) {
            <mat-card style="margin-top: 16px;">
              <mat-card-content>
                <table mat-table [dataSource]="filteredProjects()" class="sig-data-table">
                  <!-- Nombre column -->
                  <ng-container matColumnDef="nombre">
                    <th mat-header-cell>Nombre</th>
                    <td mat-cell [routerLink]="['/projects', row.id]">{{ row.nombre }}</td>
                  </ng-container>

                  <!-- Cliente column -->
                  <ng-container matColumnDef="clientNombre">
                    <th mat-header-cell>Cliente</th>
                    <td mat-cell>{{ row.clientNombre }}</td>
                  </ng-container>

                  <!-- Código column -->
                  <ng-container matColumnDef="codigo">
                    <th mat-header-cell>Código</th>
                    <td mat-cell class="sig-mono">{{ row.codigo }}</td>
                  </ng-container>

                  <!-- Estado column -->
                  <ng-container matColumnDef="estado">
                    <th mat-header-cell>Estado</th>
                    <td mat-cell>
                      <span [class]="'sig-badge sig-badge-' + (row.activo ? 'success' : 'muted')">
                        {{ row.activo ? 'Activo' : 'Inactivo' }}
                      </span>
                    </td>
                  </ng-container>

                  <tr mat-header-row></tr>
                  <tr mat-row *matRowDef="let row"></tr>
                </table>
              </mat-card-content>
            </mat-card>
          } @else {
            <mat-card style="margin-top: 16px;">
              <mat-card-content class="sig-empty-state">
                <mat-icon>search_off</mat-icon>
                <p>No hay proyectos que coincidan con tu búsqueda</p>
              </mat-card-content>
            </mat-card>
          }
        </mat-tab>

        <!-- Pestaña 2: Importación -->
        <mat-tab>
          <ng-template mat-tab-label>
            <mat-icon>upload_file</mat-icon>
            Importar
          </ng-template>

          <mat-card style="margin-top: 16px;">
            <mat-card-content>
              <!-- Componente de importación -->
              <app-excel-import
                [config]="importConfig"
                (importData)="onImportData($event)"
              />
            </mat-card-content>
          </mat-card>
        </mat-tab>
      </mat-tab-group>
    </div>
  `,
  styles: [`
    .sig-data-table {
      width: 100%;
      border-collapse: collapse;
    }

    .sig-data-table th,
    .sig-data-table td {
      padding: 12px;
      text-align: left;
      border-bottom: 1px solid var(--sig-border);
    }

    .sig-empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 12px;
      padding: 60px 20px;
      text-align: center;
      color: var(--sig-text-muted);
    }

    .sig-empty-state mat-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
      opacity: 0.5;
    }

    .sig-badge {
      display: inline-block;
      padding: 4px 8px;
      border-radius: 4px;
      font-size: 11px;
      font-weight: 600;
      text-transform: uppercase;
    }

    .sig-badge-success {
      background: rgba(34, 197, 94, 0.15);
      color: #16a34a;
    }

    .sig-badge-muted {
      background: rgba(0, 0, 0, 0.1);
      color: var(--sig-text-muted);
    }

    .sig-mono {
      font-family: 'Roboto Mono', monospace;
      font-size: 12px;
      color: var(--sig-text-muted);
    }
  `],
})
export class ProjectsListComponent implements OnInit {
  private readonly projectSvc = inject(ProjectService);
  private readonly notify = inject(NotifyService);

  protected readonly projects = signal<ProjectDto[]>([]);
  protected readonly filteredProjects = signal<ProjectDto[]>([]);

  protected readonly importConfig: ExcelImportConfig = {
    requiredColumns: ['nombre', 'clientId', 'codigo'],
    optionalColumns: ['descripcion'],
    sheetName: 'Proyectos',
    validators: {
      nombre: (v) => ({
        valid: String(v).length >= 3,
        error: 'Mínimo 3 caracteres',
      }),
      clientId: (v) => ({
        valid: !isNaN(Number(v)) && Number(v) > 0,
        error: 'ID de cliente debe ser un número > 0',
      }),
      codigo: (v) => ({
        valid: /^[A-Z0-9-]+$/.test(String(v)),
        error: 'Solo mayúsculas, números y guiones',
      }),
    },
  };

  ngOnInit(): void {
    this.loadProjects();
  }

  protected loadProjects(): void {
    this.projectSvc.list().subscribe({
      next: (items) => {
        this.projects.set(items);
        this.filteredProjects.set(items);
      },
      error: (err) => this.notify.error(err?.error?.title ?? 'Error al cargar proyectos'),
    });
  }

  protected onSearchResults(results: Record<string, unknown>[]): void {
    this.filteredProjects.set(results as ProjectDto[]);
  }

  protected onImportData(rows: ExcelImportRow[]): void {
    const batch = rows.map((r) => ({
      nombre: r.data['nombre'] as string,
      clientId: Number(r.data['clientId']),
      codigo: r.data['codigo'] as string,
      descripcion: (r.data['descripcion'] as string) || '',
    }));

    // Llamar al servicio para crear proyectos en batch
    // this.projectSvc.createBatch(batch).subscribe({
    //   next: (created) => {
    //     this.notify.success(`${created.length} proyectos importados`);
    //     this.loadProjects();
    //   },
    //   error: (err) => this.notify.error(err?.error?.title ?? 'Error en importación'),
    // });
  }
}
*/

/**
 * INTEGRACIÓN EN OTROS COMPONENTES:
 *
 * 1. EN PROJECTS COMPONENT:
 *    - Importar FulltextSearchComponent, ExcelImportComponent
 *    - Agregar a imports: [FulltextSearchComponent, ExcelImportComponent]
 *    - Usar en template: <app-fulltext-search .../>
 *    - Usar en template: <app-excel-import .../>
 *    - Implementar handlers: onSearchResults(), onImportData()
 *
 * 2. EN CLIENTS COMPONENT:
 *    - Mismo patrón
 *    - searchFields: ['nombre', 'nif', 'email', 'ciudad']
 *
 * 3. EN CLOSURES COMPONENT:
 *    - searchFields: ['projectNombre', 'clientNombre', 'estado', 'periodo']
 *
 * 4. EN USERS COMPONENT:
 *    - searchFields: ['nombre', 'apellidos', 'email', 'nif']
 *
 * VALIDADORES EJEMPLO PARA EXCEL IMPORT:
 *
 * Para importar clientes:
 * ```
 * validators: {
 *   nif: (v) => ({
 *     valid: /^[0-9XYZ]{8,9}$/.test(String(v)),
 *     error: 'NIF inválido',
 *   }),
 *   email: (v) => ({
 *     valid: /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(String(v)),
 *     error: 'Email inválido',
 *   }),
 * }
 * ```
 *
 * Para importar empleados:
 * ```
 * validators: {
 *   email: (v) => ({
 *     valid: /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(String(v)),
 *     error: 'Email inválido',
 *   }),
 *   telefono: (v) => ({
 *     valid: /^[0-9+\s-]{9,}$/.test(String(v)),
 *     error: 'Teléfono inválido',
 *   }),
 * }
 * ```
 */

export { FulltextSearchComponent } from './fulltext-search.component';
export { ExcelImportComponent } from './excel-import.component';
export type { ExcelImportConfig, ExcelImportRow } from './excel-import.component';
