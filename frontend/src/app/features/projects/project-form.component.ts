import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ProjectService } from '../../core/api/projects.service';
import { ClientService } from '../../core/api/clients.service';
import { CostCenterService } from '../../core/api/catalogs.service';
import { UserService } from '../../core/api/users.service';
import { NotifyService } from '../../core/notify.service';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { ClientListItemDto, CostCenterDto, UserListItemDto } from '../../models/dtos';

@Component({
  selector: 'app-project-form',
  standalone: true,
  imports: [
    CommonModule, RouterLink, ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatIconModule, MatDatepickerModule, MatNativeDateModule, MatProgressSpinnerModule,
    BreadcrumbsComponent, SkeletonComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Projects', route: '/projects' }, { label: isEdit() ? 'Editar' : 'Nuevo' }]" />
      <div class="sig-page__header"><h1 class="sig-page__title">{{ isEdit() ? 'Editar Project' : 'Nuevo Project' }}</h1></div>

      @if (loading()) { <mat-card><mat-card-content><sig-skeleton [count]="6" /></mat-card-content></mat-card> }
      @else {
        <mat-card><mat-card-content>
          <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
            <div class="sig-form-row">
              <mat-form-field class="sig-form-field">
                <mat-label>Nombre *</mat-label>
                <input matInput formControlName="nombre" data-testid="input-nombre" />
                @if (form.controls.nombre.touched && form.controls.nombre.hasError('required')) { <mat-error>El nombre es obligatorio</mat-error> }
              </mat-form-field>
              <mat-form-field class="sig-form-field">
                <mat-label>Cliente *</mat-label>
                <mat-select formControlName="clientId" data-testid="select-cliente">
                  @for (c of clients(); track c.id) {
                    <mat-option [value]="c.id">{{ c.nombre }}</mat-option>
                  }
                </mat-select>
                @if (form.controls.clientId.touched && form.controls.clientId.hasError('required')) { <mat-error>Selecciona un cliente</mat-error> }
              </mat-form-field>
            </div>
            <div class="sig-form-row">
              <mat-form-field class="sig-form-field">
                <mat-label>Estado *</mat-label>
                <mat-select formControlName="estado" data-testid="select-estado">
                  <mat-option value="Activo">Activo</mat-option>
                  <mat-option value="Pausado">Pausado</mat-option>
                  <mat-option value="Cerrado">Cerrado</mat-option>
                </mat-select>
              </mat-form-field>
              <mat-form-field class="sig-form-field">
                <mat-label>Fecha de alta *</mat-label>
                <input matInput [matDatepicker]="dp" formControlName="fechaAlta" data-testid="input-fechaalta" />
                <mat-datepicker-toggle matIconSuffix [for]="dp" />
                <mat-datepicker #dp />
              </mat-form-field>
            </div>
            <h3 class="sig-form-section">Interlocutor</h3>
            <div class="sig-form-row">
              <mat-form-field class="sig-form-field">
                <mat-label>Nombre</mat-label>
                <input matInput formControlName="interlocutorNombre" data-testid="input-interlocutor-nombre" />
              </mat-form-field>
              <mat-form-field class="sig-form-field">
                <mat-label>Email</mat-label>
                <input matInput type="email" formControlName="interlocutorEmail" data-testid="input-interlocutor-email" />
              </mat-form-field>
            </div>
            <div class="sig-form-row">
              <mat-form-field class="sig-form-field">
                <mat-label>Teléfono</mat-label>
                <input matInput formControlName="interlocutorTelefono" data-testid="input-interlocutor-tel" />
              </mat-form-field>
            </div>
            <h3 class="sig-form-section">Asignaciones</h3>
            <div class="sig-form-row">
              <mat-form-field class="sig-form-field">
                <mat-label>Centros de coste</mat-label>
                <mat-select formControlName="costCenterIds" multiple data-testid="select-costcenters">
                  @for (cc of ccs(); track cc.id) {
                    <mat-option [value]="cc.id">{{ cc.codigo }} - {{ cc.nombre }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>
              <mat-form-field class="sig-form-field">
                <mat-label>Usuarios asignados</mat-label>
                <mat-select formControlName="userIds" multiple data-testid="select-usuarios">
                  @for (u of users(); track u.id) {
                    <mat-option [value]="u.id">{{ u.nombre }} {{ u.apellidos }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>
            </div>

            <div class="sig-form-actions">
              <a mat-stroked-button routerLink="/projects" data-testid="btn-cancelar">Cancelar</a>
              <button mat-flat-button color="primary" type="submit" [disabled]="submitting() || form.invalid" data-testid="btn-guardar">
                @if (submitting()) { <mat-spinner diameter="20" /> } @else { Guardar }
              </button>
            </div>
          </form>
        </mat-card-content></mat-card>
      }
    </div>
  `,
  styles: [`
    .sig-form-section { font-size: 14px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.08em; color: var(--mat-sys-on-surface-variant); margin: 16px 0 8px; padding-bottom: 8px; border-bottom: 1px solid var(--mat-sys-outline-variant); }
    .sig-form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
    .sig-form-field { width: 100%; }
    .sig-form-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 16px; }
    @media (max-width: 599px) { .sig-form-row { grid-template-columns: 1fr; } }
  `],
})
export class ProjectFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly projectSvc = inject(ProjectService);
  private readonly clientSvc = inject(ClientService);
  private readonly ccSvc = inject(CostCenterService);
  private readonly userSvc = inject(UserService);
  private readonly notify = inject(NotifyService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly id = signal<number | null>(null);
  protected readonly isEdit = signal(false);
  protected readonly loading = signal(false);
  protected readonly submitting = signal(false);
  protected readonly clients = signal<ClientListItemDto[]>([]);
  protected readonly ccs = signal<CostCenterDto[]>([]);
  protected readonly users = signal<UserListItemDto[]>([]);

  protected readonly form = this.fb.nonNullable.group({
    nombre: ['', [Validators.required, Validators.minLength(2)]],
    clientId: [0 as number, [Validators.required]],
    estado: ['Activo' as 'Activo' | 'Pausado' | 'Cerrado', [Validators.required]],
    interlocutorNombre: [''],
    interlocutorEmail: ['', [Validators.email]],
    interlocutorTelefono: [''],
    fechaAlta: [new Date(), [Validators.required]],
    costCenterIds: [[] as number[]],
    userIds: [[] as number[]],
  });

  ngOnInit(): void {
    forkJoin({
      clients: this.clientSvc.list(1, 100),
      ccs: this.ccSvc.list(),
      users: this.userSvc.list(1, 200),
    }).subscribe({
      next: (r) => {
        this.clients.set(r.clients.items);
        this.ccs.set(r.ccs);
        this.users.set(r.users.items);
      },
      error: () => { /* listas vacías si falla */ },
    });

    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.id.set(Number(idParam));
      this.isEdit.set(true);
      this.loadEntity();
    }
  }

  private loadEntity(): void {
    this.loading.set(true);
    this.projectSvc.getById(this.id()!).subscribe({
      next: (p) => {
        this.form.patchValue({
          nombre: p.nombre, clientId: p.clientId, estado: p.estado,
          interlocutorNombre: p.interlocutorNombre ?? '',
          interlocutorEmail: p.interlocutorEmail ?? '',
          interlocutorTelefono: p.interlocutorTelefono ?? '',
          fechaAlta: new Date(p.fechaAlta),
          costCenterIds: p.costCenterIds, userIds: p.userIds,
        });
        this.loading.set(false);
      },
      error: () => { this.loading.set(false); this.notify.error('No se pudo cargar el proyecto'); },
    });
  }

  protected submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const payload = {
      nombre: v.nombre, clientId: v.clientId, estado: v.estado,
      interlocutorNombre: v.interlocutorNombre || null,
      interlocutorEmail: v.interlocutorEmail || null,
      interlocutorTelefono: v.interlocutorTelefono || null,
      fechaAlta: v.fechaAlta.toISOString().slice(0, 10),
      costCenterIds: v.costCenterIds, userIds: v.userIds,
    };
    this.submitting.set(true);
    const obs = this.isEdit() ? this.projectSvc.update(this.id()!, payload) : this.projectSvc.create(payload);
    obs.subscribe({
      next: (p) => { this.submitting.set(false); this.notify.success(this.isEdit() ? 'Proyecto actualizado' : 'Proyecto creado'); void this.router.navigate(['/projects', p.id]); },
      error: (err) => { this.submitting.set(false); this.notify.error(err?.error?.title ?? 'No se pudo guardar'); },
    });
  }
}
