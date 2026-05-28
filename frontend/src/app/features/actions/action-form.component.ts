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
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ActionService } from '../../core/api/actions.service';
import { ProjectService } from '../../core/api/projects.service';
import { ConceptService } from '../../core/api/concepts.service';
import { UserService } from '../../core/api/users.service';
import { DepartmentService } from '../../core/api/catalogs.service';
import { NotifyService } from '../../core/notify.service';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { ProjectListItemDto, ConceptListItemDto, UserListItemDto, DepartmentDto } from '../../models/dtos';

@Component({
  selector: 'app-action-form',
  standalone: true,
  imports: [
    CommonModule, RouterLink, ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatIconModule, MatProgressSpinnerModule,
    BreadcrumbsComponent, SkeletonComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Actions', route: '/actions' }, { label: isEdit() ? 'Editar' : 'Nueva' }]" />
      <div class="sig-page__header"><h1 class="sig-page__title">{{ isEdit() ? 'Editar Action' : 'Nueva Action' }}</h1></div>

      @if (loading()) { <mat-card><mat-card-content><sig-skeleton [count]="6" /></mat-card-content></mat-card> }
      @else {
        <mat-card><mat-card-content>
          <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
            <div class="sig-form-row">
              <mat-form-field class="sig-form-field"><mat-label>Nombre *</mat-label><input matInput formControlName="nombre" data-testid="input-nombre" />
                @if (form.controls.nombre.touched && form.controls.nombre.hasError('required')) { <mat-error>El nombre es obligatorio</mat-error> }
              </mat-form-field>
              <mat-form-field class="sig-form-field"><mat-label>Proyecto *</mat-label>
                <mat-select formControlName="projectId" data-testid="select-proyecto">
                  @for (p of projects(); track p.id) { <mat-option [value]="p.id">{{ p.nombre }}</mat-option> }
                </mat-select>
              </mat-form-field>
            </div>
            <div class="sig-form-row">
              <mat-form-field class="sig-form-field"><mat-label>Estado *</mat-label>
                <mat-select formControlName="estado" data-testid="select-estado">
                  <mat-option value="Activa">Activa</mat-option><mat-option value="Inactiva">Inactiva</mat-option>
                </mat-select>
              </mat-form-field>
              <mat-form-field class="sig-form-field"><mat-label>Departamento</mat-label>
                <mat-select formControlName="departmentId" data-testid="select-depto">
                  <mat-option [value]="null">— Sin departamento —</mat-option>
                  @for (d of depts(); track d.id) { <mat-option [value]="d.id">{{ d.nombre }}</mat-option> }
                </mat-select>
              </mat-form-field>
            </div>
            <h3 class="sig-form-section">Asignaciones</h3>
            <div class="sig-form-row">
              <mat-form-field class="sig-form-field"><mat-label>Conceptos</mat-label>
                <mat-select formControlName="conceptIds" multiple data-testid="select-conceptos">
                  @for (c of concepts(); track c.id) { <mat-option [value]="c.id">{{ c.nombre }} ({{ c.tipo }})</mat-option> }
                </mat-select>
              </mat-form-field>
              <mat-form-field class="sig-form-field"><mat-label>Usuarios</mat-label>
                <mat-select formControlName="userIds" multiple data-testid="select-usuarios">
                  @for (u of users(); track u.id) { <mat-option [value]="u.id">{{ u.nombre }} {{ u.apellidos }}</mat-option> }
                </mat-select>
              </mat-form-field>
            </div>
            <div class="sig-form-actions">
              <a mat-stroked-button routerLink="/actions" data-testid="btn-cancelar">Cancelar</a>
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
export class ActionFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly actionSvc = inject(ActionService);
  private readonly projectSvc = inject(ProjectService);
  private readonly conceptSvc = inject(ConceptService);
  private readonly userSvc = inject(UserService);
  private readonly deptSvc = inject(DepartmentService);
  private readonly notify = inject(NotifyService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly id = signal<number | null>(null);
  protected readonly isEdit = signal(false);
  protected readonly loading = signal(false);
  protected readonly submitting = signal(false);
  protected readonly projects = signal<ProjectListItemDto[]>([]);
  protected readonly concepts = signal<ConceptListItemDto[]>([]);
  protected readonly users = signal<UserListItemDto[]>([]);
  protected readonly depts = signal<DepartmentDto[]>([]);

  protected readonly form = this.fb.nonNullable.group({
    nombre: ['', [Validators.required, Validators.minLength(2)]],
    projectId: [0 as number, [Validators.required]],
    clientId: [0 as number, [Validators.required]],
    estado: ['Activa' as 'Activa' | 'Inactiva', [Validators.required]],
    departmentId: [null as number | null],
    conceptIds: [[] as number[]],
    userIds: [[] as number[]],
  });

  ngOnInit(): void {
    forkJoin({
      projects: this.projectSvc.list(1, 100),
      concepts: this.conceptSvc.list(1, 200),
      users: this.userSvc.list(1, 200),
      depts: this.deptSvc.list(),
    }).subscribe({
      next: (r) => {
        this.projects.set(r.projects.items);
        this.concepts.set(r.concepts.items);
        this.users.set(r.users.items);
        this.depts.set(r.depts);
      },
      error: () => {},
    });

    // Derive clientId del project seleccionado
    this.form.controls.projectId.valueChanges.subscribe((pid) => {
      const p = this.projects().find((p) => p.id === pid);
      if (p) this.form.controls.clientId.setValue(p.clientId);
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
    this.actionSvc.getById(this.id()!).subscribe({
      next: (a) => {
        this.form.patchValue({
          nombre: a.nombre, projectId: a.projectId, clientId: a.clientId, estado: a.estado,
          departmentId: a.departmentId ?? null, conceptIds: a.conceptIds, userIds: a.userIds,
        });
        this.loading.set(false);
      },
      error: () => { this.loading.set(false); this.notify.error('No se pudo cargar la action'); },
    });
  }

  protected submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const payload = this.form.getRawValue();
    this.submitting.set(true);
    const obs = this.isEdit() ? this.actionSvc.update(this.id()!, payload) : this.actionSvc.create(payload);
    obs.subscribe({
      next: (a) => { this.submitting.set(false); this.notify.success(this.isEdit() ? 'Action actualizada' : 'Action creada'); void this.router.navigate(['/actions', a.id]); },
      error: (err) => { this.submitting.set(false); this.notify.error(err?.error?.title ?? 'No se pudo guardar'); },
    });
  }
}
