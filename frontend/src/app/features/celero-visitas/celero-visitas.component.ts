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
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { NotifyService } from '../../core/notify.service';
import { CeleroService, CeleroVisitaDto } from '../../core/api/celero.service';
import { UserService } from '../../core/api/users.service';
import { ServiceService } from '../../core/api/services.service';

type CeleroVisita = CeleroVisitaDto;

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

          <ng-container matColumnDef="servicio">
            <th mat-header-cell *matHeaderCellDef>Servicio</th>
            <td mat-cell *matCellDef="let v">{{ getServiceName(v.serviceId) || '—' }}</td>
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
  private celero = inject(CeleroService);
  private userSvc = inject(UserService);
  private serviceSvc = inject(ServiceService);
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

  displayedColumns = ['fecha', 'resourceNif', 'serviceName', 'usuario', 'servicio', 'notas', 'acciones'];

  usuarios = signal<SelectOption[]>([]);
  servicios = signal<SelectOption[]>([]);

  ngOnInit() {
    this.cargarRefData();
    this.cargarVisitas();
  }

  private cargarRefData() {
    this.userSvc.list(1, 1000).subscribe({
      next: res => this.usuarios.set((res.items || []).map(u => ({ id: u.id, nombre: u.nombre }))),
      error: () => this.notify.error('Error cargando usuarios'),
    });
    this.serviceSvc.list(1, 1000).subscribe({
      next: res => this.servicios.set((res.items || []).map(s => ({ id: s.id, nombre: s.nombre }))),
      error: () => this.notify.error('Error cargando servicios'),
    });
  }

  private cargarVisitas() {
    this.loading.set(true);
    this.celero.listVisitas({
      page: this.page(),
      pageSize: this.pageSize(),
      searchNif: this.searchNif || undefined,
      searchService: this.searchService || undefined,
    }).subscribe({
      next: res => {
        this.visitas.set(res.items || []);
        this.total.set(res.total || 0);
        this.loading.set(false);
      },
      error: err => {
        console.error('Error cargando visitas:', err);
        this.notify.error('Error cargando visitas');
        this.loading.set(false);
      },
    });
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
      data: { visita, usuarios: this.usuarios(), servicios: this.servicios() },
      width: '600px'
    }).afterClosed().subscribe(result => {
      if (result) this.cargarVisitas();
    });
  }

  getUserName(userId?: number) {
    if (!userId) return null;
    return this.usuarios().find(u => u.id === userId)?.nombre;
  }

  getServiceName(serviceId?: number) {
    if (!serviceId) return null;
    return this.servicios().find(p => p.id === serviceId)?.nombre;
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
          <mat-label>NIF Empleado</mat-label>
          <input matInput formControlName="resourceNif" placeholder="12345678A">
        </mat-form-field>

        <mat-form-field fullWidth>
          <mat-label>Usuario</mat-label>
          <mat-select formControlName="userId">
            <mat-option [value]="null">— Sin asignar —</mat-option>
            <mat-option *ngFor="let u of usuarios" [value]="u.id">{{ u.nombre }}</mat-option>
          </mat-select>
        </mat-form-field>

        <mat-form-field fullWidth>
          <mat-label>Servicio</mat-label>
          <mat-select formControlName="serviceId">
            <mat-option [value]="null">— Sin asignar —</mat-option>
            <mat-option *ngFor="let p of servicios" [value]="p.id">{{ p.nombre }}</mat-option>
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
  private celero = inject(CeleroService);
  private dialogRef = inject(MatDialogRef);
  private notify = inject(NotifyService);
  private fb = inject(FormBuilder);

  data = inject(MAT_DIALOG_DATA);
  usuarios: SelectOption[] = [];
  servicios: SelectOption[] = [];
  form: FormGroup = new FormGroup({});

  ngOnInit() {
    this.usuarios = this.data.usuarios;
    this.servicios = this.data.servicios;
    const v = this.data.visita;
    this.form = this.fb.group({
      resourceNif: [v.resourceNif ?? ''],
      userId: [v.userId],
      serviceId: [v.serviceId],
      notas: [v.notas]
    });
  }

  guardar() {
    this.celero.updateVisita(this.data.visita.id, this.form.value).subscribe({
      next: () => {
        this.notify.success('Visita actualizada');
        this.dialogRef.close(true);
      },
      error: () => this.notify.error('Error actualizando visita'),
    });
  }
}
