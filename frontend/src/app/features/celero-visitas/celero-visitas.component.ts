import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatDialogModule, MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { HttpClient, HttpParams } from '@angular/common/http';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { NotifyService } from '../../core/notify.service';

interface CeleroVisita {
  id: number;
  visitaIdExterno: string;
  resourceNif: string;
  serviceName: string;
  missionName: string;
  fecha: string;
  userId?: number;
  projectId?: number;
  actionId?: number;
  notas?: string;
  estadoMapeo?: string;
}

interface SelectOption {
  id: number;
  nombre: string;
}

@Component({
  selector: 'app-celero-visitas',
  standalone: true,
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    MatTableModule, MatPaginatorModule, MatButtonModule, MatIconModule,
    MatInputModule, MatSelectModule, MatFormFieldModule, MatDialogModule, MatProgressSpinnerModule,
    MatTooltipModule, BreadcrumbsComponent
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Visitas Celero' }]" />
      <div class="sig-page__header">
        <h1 class="sig-page__title">Visitas Celero</h1>
        <button mat-raised-button color="accent" (click)="irAMapeos()" class="mapeos-button">
          <mat-icon>settings</mat-icon>
          Gestión de Mapeos
        </button>
      </div>

      <div class="filters">
        <mat-form-field>
          <mat-label>Buscar NIF</mat-label>
          <input matInput [(ngModel)]="searchNif" (ngModelChange)="onSearch()" />
        </mat-form-field>
        <mat-form-field>
          <mat-label>Buscar Servicio</mat-label>
          <input matInput [(ngModel)]="searchService" (ngModelChange)="onSearch()" />
        </mat-form-field>
      </div>

      @if (loading()) {
        <mat-spinner></mat-spinner>
      } @else {
        <table mat-table [dataSource]="visitas()" class="full-width">
          <ng-container matColumnDef="fecha">
            <th mat-header-cell *matHeaderCellDef>Fecha</th>
            <td mat-cell *matCellDef="let v">{{ v.fecha }}</td>
          </ng-container>

          <ng-container matColumnDef="resourceNif">
            <th mat-header-cell *matHeaderCellDef>NIF Empleado</th>
            <td mat-cell *matCellDef="let v">{{ v.resourceNif }}</td>
          </ng-container>

          <ng-container matColumnDef="serviceName">
            <th mat-header-cell *matHeaderCellDef>Servicio</th>
            <td mat-cell *matCellDef="let v">{{ v.serviceName }}</td>
          </ng-container>

          <ng-container matColumnDef="usuario">
            <th mat-header-cell *matHeaderCellDef>Usuario</th>
            <td mat-cell *matCellDef="let v">{{ getUserName(v.userId) || '—' }}</td>
          </ng-container>

          <ng-container matColumnDef="proyecto">
            <th mat-header-cell *matHeaderCellDef>Proyecto</th>
            <td mat-cell *matCellDef="let v">{{ getProjectName(v.projectId) || '—' }}</td>
          </ng-container>

          <ng-container matColumnDef="notas">
            <th mat-header-cell *matHeaderCellDef>Notas</th>
            <td mat-cell *matCellDef="let v" class="nota-cell">{{ v.notas || '—' }}</td>
          </ng-container>

          <ng-container matColumnDef="acciones">
            <th mat-header-cell *matHeaderCellDef>Acciones</th>
            <td mat-cell *matCellDef="let v">
              <button mat-icon-button (click)="editarVisita(v)" matTooltip="Editar">
                <mat-icon>edit</mat-icon>
              </button>
            </td>
          </ng-container>

          <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
          <tr mat-row *matRowDef="let v; columns: displayedColumns;"></tr>
        </table>

        <mat-paginator
          [length]="total()"
          [pageSize]="pageSize()"
          [pageSizeOptions]="[10, 25, 50]"
          (page)="onPageChange($event)">
        </mat-paginator>
      }
    </div>
  `,
  styles: [`
    .sig-page__header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 24px;
    }
    .mapeos-button {
      margin-left: auto;
    }
    .filters { display: flex; gap: 16px; margin-bottom: 24px; }
    mat-form-field { width: 200px; }
    table { width: 100%; margin-top: 24px; }
    .full-width { width: 100%; }
    .nota-cell { max-width: 150px; overflow: hidden; text-overflow: ellipsis; }
    mat-spinner { margin: 24px auto; }
  `]
})
export class CeleroVisitasComponent {
  private http = inject(HttpClient);
  private notify = inject(NotifyService);
  private dialog = inject(MatDialog);
  private router = inject(Router);

  searchNif = '';
  searchService = '';

  visitas = signal<CeleroVisita[]>([]);
  total = signal(0);
  page = signal(1);
  pageSize = signal(25);
  loading = signal(false);

  displayedColumns = ['fecha', 'resourceNif', 'serviceName', 'usuario', 'proyecto', 'notas', 'acciones'];

  usuarios = signal<SelectOption[]>([]);
  proyectos = signal<SelectOption[]>([]);

  ngOnInit() {
    this.cargarRefData();
    this.cargarVisitas();
  }

  private cargarRefData() {
    this.http.get<any>('/api/users').subscribe(
      res => this.usuarios.set(res.items || []),
      () => this.notify.error('Error cargando usuarios')
    );
    this.http.get<any>('/api/projects').subscribe(
      res => this.proyectos.set(res.items || []),
      () => this.notify.error('Error cargando proyectos')
    );
  }

  private cargarVisitas() {
    this.loading.set(true);
    let params = new HttpParams()
      .set('page', this.page().toString())
      .set('pageSize', this.pageSize().toString());

    if (this.searchNif) {
      params = params.set('searchNif', this.searchNif);
    }
    if (this.searchService) {
      params = params.set('searchService', this.searchService);
    }

    this.http.get<any>('/api/celero-visitas', { params }).subscribe(
      res => {
        this.visitas.set(res.items || []);
        this.total.set(res.total || 0);
        this.loading.set(false);
      },
      err => {
        console.error('Error cargando visitas:', err);
        this.notify.error('Error cargando visitas');
        this.loading.set(false);
      }
    );
  }

  onSearch() {
    this.page.set(1);
    this.cargarVisitas();
  }

  onPageChange(event: PageEvent) {
    this.page.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    this.cargarVisitas();
  }

  editarVisita(visita: CeleroVisita) {
    this.dialog.open(CeleroVisitasEditComponent, {
      data: { visita, usuarios: this.usuarios(), proyectos: this.proyectos() },
      width: '600px'
    }).afterClosed().subscribe(result => {
      if (result) this.cargarVisitas();
    });
  }

  getUserName(userId?: number) {
    if (!userId) return null;
    return this.usuarios().find(u => u.id === userId)?.nombre;
  }

  getProjectName(projectId?: number) {
    if (!projectId) return null;
    return this.proyectos().find(p => p.id === projectId)?.nombre;
  }

  irAMapeos() {
    this.router.navigate(['/celero-mapeos']);
  }
}

// Componente para editar visita
@Component({
  selector: 'app-celero-editar',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule, MatDialogModule],
  template: `
    <h2 mat-dialog-title>Editar Visita</h2>
    <mat-dialog-content>
      <form [formGroup]="form">
        <mat-form-field fullWidth>
          <mat-label>Usuario</mat-label>
          <mat-select formControlName="userId">
            <mat-option [value]="null">— Sin asignar —</mat-option>
            <mat-option *ngFor="let u of usuarios" [value]="u.id">{{ u.nombre }}</mat-option>
          </mat-select>
        </mat-form-field>

        <mat-form-field fullWidth>
          <mat-label>Proyecto</mat-label>
          <mat-select formControlName="projectId">
            <mat-option [value]="null">— Sin asignar —</mat-option>
            <mat-option *ngFor="let p of proyectos" [value]="p.id">{{ p.nombre }}</mat-option>
          </mat-select>
        </mat-form-field>

        <mat-form-field fullWidth>
          <mat-label>Notas</mat-label>
          <textarea matInput formControlName="notas" rows="3"></textarea>
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancelar</button>
      <button mat-raised-button color="primary" (click)="guardar()">Guardar</button>
    </mat-dialog-actions>
  `
})
export class CeleroVisitasEditComponent {
  private http = inject(HttpClient);
  private dialogRef = inject(MatDialogRef);
  private notify = inject(NotifyService);
  private fb = inject(FormBuilder);

  data = inject(MAT_DIALOG_DATA);
  usuarios: SelectOption[] = [];
  proyectos: SelectOption[] = [];
  form: FormGroup = new FormGroup({});

  ngOnInit() {
    this.usuarios = this.data.usuarios;
    this.proyectos = this.data.proyectos;
    const v = this.data.visita;
    this.form = this.fb.group({
      userId: [v.userId],
      projectId: [v.projectId],
      notas: [v.notas]
    });
  }

  guardar() {
    this.http.put(`/api/celero-visitas/${this.data.visita.id}`, this.form.value).subscribe(
      () => {
        this.notify.success('Visita actualizada');
        this.dialogRef.close(true);
      },
      err => this.notify.error('Error actualizando visita')
    );
  }
}
