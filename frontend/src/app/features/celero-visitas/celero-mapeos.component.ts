import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatBadgeModule } from '@angular/material/badge';
import { HttpClient } from '@angular/common/http';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { NotifyService } from '../../core/notify.service';

interface PendingValue {
  valor: string;
  cantidad: number;
  estaMapado: boolean;
  selectedId?: number; // Para seleccionar el SIG-ES ID
}

interface PendientesResponse {
  recursos: PendingValue[];
  servicios: PendingValue[];
  misiones: PendingValue[];
  totalVisitasSinMapear: number;
}

interface SelectOption {
  id: number;
  nombre: string;
}

@Component({
  selector: 'app-celero-mapeos',
  standalone: true,
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    MatTableModule, MatButtonModule, MatIconModule,
    MatSelectModule, MatFormFieldModule, MatExpansionModule,
    MatProgressSpinnerModule, MatBadgeModule,
    BreadcrumbsComponent
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[
        { label: 'Inicio', route: '/dashboard' },
        { label: 'Visitas Celero', route: '/celero-visitas' },
        { label: 'Gestión de Mapeos' }
      ]" />

      <div class="sig-page__header">
        <h1 class="sig-page__title">Gestión de Mapeos Celero</h1>
        <p class="header-subtitle">Mapea los valores de Celero con los registros de SIG-ES</p>
      </div>

      @if (cargando()) {
        <div class="spinner-container">
          <mat-spinner></mat-spinner>
          <p>Cargando valores pendientes...</p>
        </div>
      } @else {
        <div class="mapeos-container">
          <!-- Estadísticas -->
          <div class="stats-panel">
            <div class="stat-card">
              <h3>Total Visitas sin Mapear</h3>
              <p class="stat-value">{{ totalVisitasSinMapear() }}</p>
            </div>
            <div class="stat-card">
              <h3>Recursos Únicos</h3>
              <p class="stat-value">{{ recursos().length }}</p>
            </div>
            <div class="stat-card">
              <h3>Servicios Únicos</h3>
              <p class="stat-value">{{ servicios().length }}</p>
            </div>
            <div class="stat-card">
              <h3>Misiones Únicas</h3>
              <p class="stat-value">{{ misiones().length }}</p>
            </div>
          </div>

          <!-- Acordeón de mapeos -->
          <mat-accordion data-testid="mappings-accordion">
            <!-- Sección Empleados (NIF → Usuario) -->
            <mat-expansion-panel [expanded]="true" data-testid="recursos-panel">
              <mat-expansion-panel-header>
                <mat-panel-title>
                  Empleados (NIF → Usuario)
                  <mat-icon matBadge="{{ recursosSinMapear() }}" matBadgeColor="warn"
                    matBadgeSize="small" class="badge-margin">people</mat-icon>
                </mat-panel-title>
                <mat-panel-description>
                  {{ recursosSinMapear() }} sin mapear de {{ recursos().length }}
                </mat-panel-description>
              </mat-expansion-panel-header>

              <table mat-table [dataSource]="recursos()" class="mapeos-table" data-testid="recursos-table">
                <!-- Columna: NIF Celero -->
                <ng-container matColumnDef="valor">
                  <th mat-header-cell *matHeaderCellDef>NIF Celero</th>
                  <td mat-cell *matCellDef="let r" class="valor-cell">{{ r.valor }}</td>
                </ng-container>

                <!-- Columna: Visitas -->
                <ng-container matColumnDef="cantidad">
                  <th mat-header-cell *matHeaderCellDef>Visitas</th>
                  <td mat-cell *matCellDef="let r" class="cantidad-cell">
                    <span class="badge" [class.alto]="r.cantidad > 10">{{ r.cantidad }}</span>
                  </td>
                </ng-container>

                <!-- Columna: Usuario SIG-ES -->
                <ng-container matColumnDef="select">
                  <th mat-header-cell *matHeaderCellDef>Usuario SIG-ES</th>
                  <td mat-cell *matCellDef="let r">
                    <mat-form-field>
                      <mat-select
                        [(ngModel)]="r.selectedId"
                        [compareWith]="compareIds"
                        class="select-small"
                        [attr.data-testid]="'recurso-select-' + r.valor">
                        <mat-option [value]="null">— Sin asignar —</mat-option>
                        <mat-option *ngFor="let u of usuarios()" [value]="u.id">
                          {{ u.nombre }}
                        </mat-option>
                      </mat-select>
                    </mat-form-field>
                  </td>
                </ng-container>

                <!-- Columna: Estado -->
                <ng-container matColumnDef="estado">
                  <th mat-header-cell *matHeaderCellDef>Estado</th>
                  <td mat-cell *matCellDef="let r">
                    <span [class]="'badge ' + (r.estaMapado ? 'mapeado' : 'pendiente')">
                      {{ r.estaMapado ? 'Mapeado' : 'Pendiente' }}
                    </span>
                  </td>
                </ng-container>

                <tr mat-header-row *matHeaderRowDef="columnas"></tr>
                <tr mat-row *matRowDef="let row; columns: columnas;"></tr>
              </table>
            </mat-expansion-panel>

            <!-- Sección Servicios (ServiceName → Servicio) -->
            <mat-expansion-panel [expanded]="false" data-testid="servicios-panel">
              <mat-expansion-panel-header>
                <mat-panel-title>
                  Servicios (ServiceName → Servicio)
                  <mat-icon matBadge="{{ serviciosSinMapear() }}" matBadgeColor="warn"
                    matBadgeSize="small" class="badge-margin">business</mat-icon>
                </mat-panel-title>
                <mat-panel-description>
                  {{ serviciosSinMapear() }} sin mapear de {{ servicios().length }}
                </mat-panel-description>
              </mat-expansion-panel-header>

              <table mat-table [dataSource]="servicios()" class="mapeos-table" data-testid="servicios-table">
                <ng-container matColumnDef="valor">
                  <th mat-header-cell *matHeaderCellDef>Servicio Celero</th>
                  <td mat-cell *matCellDef="let s" class="valor-cell">{{ s.valor }}</td>
                </ng-container>

                <ng-container matColumnDef="cantidad">
                  <th mat-header-cell *matHeaderCellDef>Visitas</th>
                  <td mat-cell *matCellDef="let s" class="cantidad-cell">
                    <span class="badge" [class.alto]="s.cantidad > 10">{{ s.cantidad }}</span>
                  </td>
                </ng-container>

                <ng-container matColumnDef="select">
                  <th mat-header-cell *matHeaderCellDef>Servicio SIG-ES</th>
                  <td mat-cell *matCellDef="let s">
                    <mat-form-field>
                      <mat-select
                        [(ngModel)]="s.selectedId"
                        [compareWith]="compareIds"
                        class="select-small"
                        [attr.data-testid]="'servicio-select-' + s.valor">
                        <mat-option [value]="null">— Sin asignar —</mat-option>
                        <mat-option *ngFor="let p of serviciosSig()" [value]="p.id">
                          {{ p.nombre }}
                        </mat-option>
                      </mat-select>
                    </mat-form-field>
                  </td>
                </ng-container>

                <ng-container matColumnDef="estado">
                  <th mat-header-cell *matHeaderCellDef>Estado</th>
                  <td mat-cell *matCellDef="let s">
                    <span [class]="'badge ' + (s.estaMapado ? 'mapeado' : 'pendiente')">
                      {{ s.estaMapado ? 'Mapeado' : 'Pendiente' }}
                    </span>
                  </td>
                </ng-container>

                <tr mat-header-row *matHeaderRowDef="columnas"></tr>
                <tr mat-row *matRowDef="let row; columns: columnas;"></tr>
              </table>
            </mat-expansion-panel>

            <!-- Sección Misiones (MissionType → Servicio) -->
            <mat-expansion-panel [expanded]="false" data-testid="misiones-panel">
              <mat-expansion-panel-header>
                <mat-panel-title>
                  Misiones (MissionType → Servicio)
                  <mat-icon matBadge="{{ misionesSinMapear() }}" matBadgeColor="warn"
                    matBadgeSize="small" class="badge-margin">assignment</mat-icon>
                </mat-panel-title>
                <mat-panel-description>
                  {{ misionesSinMapear() }} sin mapear de {{ misiones().length }}
                </mat-panel-description>
              </mat-expansion-panel-header>

              <table mat-table [dataSource]="misiones()" class="mapeos-table" data-testid="misiones-table">
                <ng-container matColumnDef="valor">
                  <th mat-header-cell *matHeaderCellDef>Misión Celero</th>
                  <td mat-cell *matCellDef="let m" class="valor-cell">{{ m.valor }}</td>
                </ng-container>

                <ng-container matColumnDef="cantidad">
                  <th mat-header-cell *matHeaderCellDef>Visitas</th>
                  <td mat-cell *matCellDef="let m" class="cantidad-cell">
                    <span class="badge" [class.alto]="m.cantidad > 10">{{ m.cantidad }}</span>
                  </td>
                </ng-container>

                <ng-container matColumnDef="select">
                  <th mat-header-cell *matHeaderCellDef>Servicio SIG-ES</th>
                  <td mat-cell *matCellDef="let m">
                    <mat-form-field>
                      <mat-select
                        [(ngModel)]="m.selectedId"
                        [compareWith]="compareIds"
                        class="select-small"
                        [attr.data-testid]="'mision-select-' + m.valor">
                        <mat-option [value]="null">— Sin asignar —</mat-option>
                        <mat-option *ngFor="let a of serviciosSig()" [value]="a.id">
                          {{ a.nombre }}
                        </mat-option>
                      </mat-select>
                    </mat-form-field>
                  </td>
                </ng-container>

                <ng-container matColumnDef="estado">
                  <th mat-header-cell *matHeaderCellDef>Estado</th>
                  <td mat-cell *matCellDef="let m">
                    <span [class]="'badge ' + (m.estaMapado ? 'mapeado' : 'pendiente')">
                      {{ m.estaMapado ? 'Mapeado' : 'Pendiente' }}
                    </span>
                  </td>
                </ng-container>

                <tr mat-header-row *matHeaderRowDef="columnas"></tr>
                <tr mat-row *matRowDef="let row; columns: columnas;"></tr>
              </table>
            </mat-expansion-panel>
          </mat-accordion>

          <!-- Botones de acción -->
          <div class="actions-footer" data-testid="actions-footer">
            <button mat-raised-button color="warn" (click)="sincronizar()" [disabled]="sincronizando() || cargando()" data-testid="btn-sincronizar">
              <mat-icon *ngIf="!sincronizando()">cloud_download</mat-icon>
              <mat-spinner diameter="20" *ngIf="sincronizando()"></mat-spinner>
              {{ sincronizando() ? 'Sincronizando...' : 'Sincronizar desde Celero' }}
            </button>
            <button mat-raised-button color="primary" (click)="guardarMapeos()" [disabled]="guardando()" data-testid="btn-guardar-mapeos">
              <mat-icon *ngIf="!guardando()">save</mat-icon>
              <mat-spinner diameter="20" *ngIf="guardando()"></mat-spinner>
              {{ guardando() ? 'Guardando...' : 'Guardar Mapeos' }}
            </button>
            <button mat-raised-button color="accent" (click)="reprocesarVisitas()" [disabled]="reprocesando()" data-testid="btn-resolver-datos">
              <mat-icon *ngIf="!reprocesando()">check_circle</mat-icon>
              <mat-spinner diameter="20" *ngIf="reprocesando()"></mat-spinner>
              {{ reprocesando() ? 'Resolviendo...' : 'Resolver Datos' }}
            </button>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .sig-page {
      padding: 24px;
    }

    .header-subtitle {
      margin: 8px 0 24px;
      color: #666;
      font-size: 14px;
    }

    .spinner-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      min-height: 300px;
      gap: 16px;
    }

    .stats-panel {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 16px;
      margin-bottom: 32px;
    }

    .stat-card {
      background: var(--sig-bg-header);
      border-radius: 8px;
      padding: 16px;
      text-align: center;
      border-left: 4px solid #1976d2;
    }

    .stat-card h3 {
      margin: 0 0 8px;
      font-size: 13px;
      color: #666;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .stat-value {
      margin: 0;
      font-size: 32px;
      font-weight: 600;
      color: #1976d2;
    }

    .mapeos-container {
      background: white;
      border-radius: 8px;
    }

    .badge-margin {
      margin-left: 8px;
      color: #ff9800;
    }

    .mapeos-table {
      width: 100%;
      margin-top: 16px;
    }

    .valor-cell {
      font-family: monospace;
      font-size: 13px;
      max-width: 200px;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .cantidad-cell {
      text-align: center;
    }

    .badge {
      display: inline-block;
      padding: 4px 8px;
      border-radius: 4px;
      font-size: 12px;
      font-weight: 500;
      background: #e0e0e0;
      color: #333;
    }

    .badge.alto {
      background: #ffb74d;
      color: white;
    }

    .badge.mapeado {
      background: #66bb6a;
      color: white;
    }

    .badge.pendiente {
      background: #ef5350;
      color: white;
    }

    .select-small {
      width: 100% !important;
      font-size: 13px;
    }

    mat-form-field {
      width: 100%;
    }

    mat-expansion-panel {
      margin-bottom: 16px !important;
    }

    .actions-footer {
      display: flex;
      gap: 12px;
      padding: 24px;
      border-top: 1px solid #e0e0e0;
      justify-content: flex-end;
      background: var(--sig-bg-hover);
      border-radius: 0 0 8px 8px;
    }

    button {
      min-width: 150px;
    }

    mat-spinner {
      display: inline-block;
      margin-right: 8px;
    }
  `]
})
export class CeleroMapeosComponent implements OnInit {
  private http = inject(HttpClient);
  private notify = inject(NotifyService);

  // Señales para datos
  recursos = signal<PendingValue[]>([]);
  servicios = signal<PendingValue[]>([]);
  misiones = signal<PendingValue[]>([]);
  totalVisitasSinMapear = signal(0);

  // Selects de referencia
  usuarios = signal<SelectOption[]>([]);
  serviciosSig = signal<SelectOption[]>([]);

  // Estados de carga
  cargando = signal(false);
  guardando = signal(false);
  reprocesando = signal(false);
  sincronizando = signal(false);

  // Computed signals para contar sin mapear
  recursosSinMapear = computed(() =>
    this.recursos().filter(r => !r.estaMapado && r.selectedId).length
  );
  serviciosSinMapear = computed(() =>
    this.servicios().filter(s => !s.estaMapado && s.selectedId).length
  );
  misionesSinMapear = computed(() =>
    this.misiones().filter(m => !m.estaMapado && m.selectedId).length
  );

  columnas = ['valor', 'cantidad', 'select', 'estado'];

  compareIds(id1: any, id2: any): boolean {
    return id1 === id2;
  }

  ngOnInit() {
    this.cargarDatos();
  }

  private cargarDatos() {
    this.cargando.set(true);

    Promise.all([
      this.http.get<PendientesResponse>('/api/celero-mappings/pendientes').toPromise(),
      this.http.get<any>('/api/users').toPromise(),
      this.http.get<any>('/api/services').toPromise()
    ]).then(
      ([mapeos, users, services]) => {
        if (mapeos) {
          this.recursos.set(mapeos.recursos);
          this.servicios.set(mapeos.servicios);
          this.misiones.set(mapeos.misiones);
          this.totalVisitasSinMapear.set(mapeos.totalVisitasSinMapear);
        }
        if (users?.items) {
          this.usuarios.set(users.items);
        }
        if (services?.items) {
          this.serviciosSig.set(services.items);
        }
        this.cargando.set(false);
      },
      (err) => {
        console.error('Error cargando datos:', err);
        this.notify.error('Error cargando datos de mapeos');
        this.cargando.set(false);
      }
    );
  }

  guardarMapeos() {
    this.guardando.set(true);

    const recursosNuevos = this.recursos()
      .filter(r => r.selectedId && !r.estaMapado)
      .map(r => ({ celeroNif: r.valor, userId: r.selectedId }));

    const serviciosNuevos = this.servicios()
      .filter(s => s.selectedId && !s.estaMapado)
      .map(s => ({ celeroServiceName: s.valor, serviceId: s.selectedId }));

    const misionesNuevos = this.misiones()
      .filter(m => m.selectedId && !m.estaMapado)
      .map(m => ({ celeroMissionName: m.valor, serviceId: m.selectedId }));

    const total = recursosNuevos.length + serviciosNuevos.length + misionesNuevos.length;

    if (total === 0) {
      this.notify.warning('No hay nuevos mapeos para guardar');
      this.guardando.set(false);
      return;
    }

    const promesas: Promise<any>[] = [];

    // Guardar recursos
    recursosNuevos.forEach(r => {
      promesas.push(
        this.http.post('/api/celero-mappings/resources', r).toPromise()
          .catch(err => {
            console.error('Error guardando recurso:', err);
            throw err; // Re-lanzar error para que Promise.all lo capture
          })
      );
    });

    // Guardar servicios
    serviciosNuevos.forEach(s => {
      promesas.push(
        this.http.post('/api/celero-mappings/services', s).toPromise()
          .catch(err => {
            console.error('Error guardando servicio:', err);
            throw err;
          })
      );
    });

    // Guardar misiones
    misionesNuevos.forEach(m => {
      promesas.push(
        this.http.post('/api/celero-mappings/missions', m).toPromise()
          .catch(err => {
            console.error('Error guardando misión:', err);
            throw err;
          })
      );
    });

    Promise.allSettled(promesas).then(
      (resultados) => {
        const completados = resultados.filter(r => r.status === 'fulfilled').length;
        const fallidos = resultados.filter(r => r.status === 'rejected').length;

        if (fallidos === 0) {
          this.notify.success(`Mapeos guardados: ${completados} nuevos registros`);
        } else {
          this.notify.warning(`Mapeos guardados: ${completados} éxito, ${fallidos} errores`);
        }

        this.guardando.set(false);
        this.cargarDatos(); // Recargar para actualizar estado
      }
    );
  }

  sincronizar() {
    this.sincronizando.set(true);

    this.http.post('/api/sync/celero', {}).subscribe(
      (res: any) => {
        this.notify.success(`Sincronización completada: ${res.filasInsertadas} nuevos registros`);
        this.cargarDatos(); // Recargar datos de mapeos y estadísticas
        this.sincronizando.set(false);
      },
      (err) => {
        this.notify.error('Error al sincronizar con Celero');
        console.error('Error:', err);
        this.sincronizando.set(false);
      }
    );
  }

  reprocesarVisitas() {
    this.reprocesando.set(true);

    this.http.post('/api/celero-mappings/reprocesar', {}).subscribe(
      (res: any) => {
        this.notify.success(`Datos resueltos: ${res.resueltos} visitas procesadas`);
        this.cargarDatos(); // Recargar datos
        this.reprocesando.set(false);
      },
      (err) => {
        this.notify.error('Error al resolver datos');
        console.error('Error:', err);
        this.reprocesando.set(false);
      }
    );
  }
}
