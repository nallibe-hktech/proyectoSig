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
import { UserService } from '../../core/api/users.service';
import { RoleService, DepartmentService } from '../../core/api/catalogs.service';
import { NotifyService } from '../../core/notify.service';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { RoleDto, DepartmentDto } from '../../models/dtos';

@Component({
  selector: 'app-user-form',
  standalone: true,
  imports: [
    CommonModule, RouterLink, ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatIconModule, MatProgressSpinnerModule,
    BreadcrumbsComponent, SkeletonComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Users', route: '/users' }, { label: isEdit() ? 'Editar' : 'Nuevo' }]" />
      <div class="sig-page__header"><h1 class="sig-page__title">{{ isEdit() ? 'Editar Usuario' : 'Nuevo Usuario' }}</h1></div>
      @if (loading()) { <mat-card><mat-card-content><sig-skeleton [count]="6" /></mat-card-content></mat-card> }
      @else {
        <mat-card><mat-card-content>
          <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
            <div class="sig-form-row">
              <mat-form-field class="sig-form-field"><mat-label>NIF *</mat-label>
                <input matInput formControlName="nif" data-testid="input-nif" />
                @if (form.controls.nif.touched && form.controls.nif.hasError('required')) { <mat-error>El NIF es obligatorio</mat-error> }
                @if (form.controls.nif.touched && form.controls.nif.hasError('pattern')) { <mat-error>El NIF debe tener 9 caracteres (letras y dígitos)</mat-error> }
              </mat-form-field>
              <mat-form-field class="sig-form-field"><mat-label>Email *</mat-label>
                <input matInput type="email" formControlName="email" data-testid="input-email" />
                @if (form.controls.email.touched && form.controls.email.hasError('required')) { <mat-error>El email es obligatorio</mat-error> }
                @if (form.controls.email.touched && form.controls.email.hasError('email')) { <mat-error>Email inválido</mat-error> }
              </mat-form-field>
            </div>
            <div class="sig-form-row">
              <mat-form-field class="sig-form-field"><mat-label>Nombre *</mat-label>
                <input matInput formControlName="nombre" data-testid="input-nombre" />
              </mat-form-field>
              <mat-form-field class="sig-form-field"><mat-label>Apellidos *</mat-label>
                <input matInput formControlName="apellidos" data-testid="input-apellidos" />
              </mat-form-field>
            </div>
            @if (!isEdit()) {
              <div class="sig-form-row">
                <mat-form-field class="sig-form-field"><mat-label>Contraseña *</mat-label>
                  <input matInput type="password" formControlName="password" data-testid="input-password" />
                  @if (form.controls.password.touched && form.controls.password.hasError('minlength')) { <mat-error>Mínimo 8 caracteres</mat-error> }
                </mat-form-field>
              </div>
            }
            <div class="sig-form-row">
              <mat-form-field class="sig-form-field"><mat-label>Estado *</mat-label>
                <mat-select formControlName="estado" data-testid="select-estado">
                  <mat-option value="Activo">Activo</mat-option>
                  <mat-option value="Inactivo">Inactivo</mat-option>
                </mat-select>
              </mat-form-field>
              <mat-form-field class="sig-form-field"><mat-label>Departamento</mat-label>
                <mat-select formControlName="departmentId" data-testid="select-depto">
                  <mat-option [value]="null">— Sin departamento —</mat-option>
                  @for (d of depts(); track d.id) { <mat-option [value]="d.id">{{ d.nombre }}</mat-option> }
                </mat-select>
              </mat-form-field>
            </div>
            <mat-form-field class="sig-form-field sig-form-field--full"><mat-label>Roles *</mat-label>
              <mat-select formControlName="roleIds" multiple data-testid="select-roles">
                @for (r of roles(); track r.id) { <mat-option [value]="r.id">{{ r.nombre }}</mat-option> }
              </mat-select>
            </mat-form-field>

            <div class="sig-form-actions">
              <a mat-stroked-button routerLink="/users" data-testid="btn-cancelar">Cancelar</a>
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
    .sig-form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
    .sig-form-field { width: 100%; }
    .sig-form-field--full { display: block; }
    .sig-form-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 16px; }
    @media (max-width: 599px) { .sig-form-row { grid-template-columns: 1fr; } }
  `],
})
export class UserFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly userSvc = inject(UserService);
  private readonly roleSvc = inject(RoleService);
  private readonly deptSvc = inject(DepartmentService);
  private readonly notify = inject(NotifyService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly id = signal<number | null>(null);
  protected readonly isEdit = signal(false);
  protected readonly loading = signal(false);
  protected readonly submitting = signal(false);
  protected readonly roles = signal<RoleDto[]>([]);
  protected readonly depts = signal<DepartmentDto[]>([]);

  protected readonly form = this.fb.nonNullable.group({
    nif: ['', [Validators.required, Validators.pattern(/^[XYZA-Z]?\d{7,8}[A-Z]$/)]],
    nombre: ['', [Validators.required]],
    apellidos: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.minLength(8)]],
    estado: ['Activo' as 'Activo' | 'Inactivo', [Validators.required]],
    departmentId: [null as number | null],
    roleIds: [[] as number[]],
  });

  ngOnInit(): void {
    forkJoin({ roles: this.roleSvc.list(), depts: this.deptSvc.list() }).subscribe({
      next: (r) => { this.roles.set(r.roles); this.depts.set(r.depts); },
      error: () => {},
    });
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.id.set(Number(idParam));
      this.isEdit.set(true);
      this.form.controls.password.clearValidators();
      this.form.controls.password.updateValueAndValidity();
      this.loading.set(true);
      this.userSvc.getById(this.id()!).subscribe({
        next: (u) => {
          this.form.patchValue({
            nif: u.nif, nombre: u.nombre, apellidos: u.apellidos, email: u.email,
            estado: u.estado, departmentId: u.departmentId ?? null, roleIds: u.roleIds,
          });
          this.loading.set(false);
        },
        error: () => { this.loading.set(false); this.notify.error('No se pudo cargar el usuario'); },
      });
    } else {
      this.form.controls.password.addValidators([Validators.required, Validators.minLength(8)]);
    }
  }

  protected submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    this.submitting.set(true);
    const obs = this.isEdit()
      ? this.userSvc.update(this.id()!, {
          nif: v.nif, nombre: v.nombre, apellidos: v.apellidos, email: v.email,
          estado: v.estado, departmentId: v.departmentId ?? null, roleIds: v.roleIds,
        })
      : this.userSvc.create({
          nif: v.nif, nombre: v.nombre, apellidos: v.apellidos, email: v.email,
          password: v.password, estado: v.estado, departmentId: v.departmentId ?? null, roleIds: v.roleIds,
        });
    obs.subscribe({
      next: (u) => { this.submitting.set(false); this.notify.success(this.isEdit() ? 'Usuario actualizado' : 'Usuario creado'); void this.router.navigate(['/users', u.id]); },
      error: (err) => { this.submitting.set(false); this.notify.error(err?.error?.title ?? 'No se pudo guardar'); },
    });
  }
}
