import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ClientService } from '../../core/api/clients.service';
import { NotifyService } from '../../core/notify.service';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';

@Component({
  selector: 'app-client-form',
  standalone: true,
  imports: [
    CommonModule, RouterLink, ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatIconModule, MatSelectModule, MatProgressSpinnerModule,
    BreadcrumbsComponent, SkeletonComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Clients', route: '/clients' }, { label: isEdit() ? 'Editar Client' : 'Nuevo Client' }]" />

      <div class="sig-page__header">
        <h1 class="sig-page__title">{{ isEdit() ? 'Editar Client' : 'Nuevo Client' }}</h1>
      </div>

      @if (loading()) {
        <mat-card><mat-card-content><sig-skeleton [count]="6" /></mat-card-content></mat-card>
      } @else {
        <mat-card>
          <mat-card-content>
            <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
              <h3 class="sig-form-section">Datos principales</h3>
              <div class="sig-form-row">
                <mat-form-field class="sig-form-field">
                  <mat-label>Nombre *</mat-label>
                  <input matInput formControlName="nombre" data-testid="input-nombre" />
                  @if (form.controls.nombre.touched && form.controls.nombre.hasError('required')) {
                    <mat-error>El nombre es obligatorio</mat-error>
                  }
                  @if (form.controls.nombre.touched && form.controls.nombre.hasError('minlength')) {
                    <mat-error>El nombre debe tener al menos 2 caracteres</mat-error>
                  }
                </mat-form-field>
                <mat-form-field class="sig-form-field">
                  <mat-label>NIF *</mat-label>
                  <input matInput formControlName="nif" data-testid="input-nif" />
                  @if (form.controls.nif.touched && form.controls.nif.hasError('required')) {
                    <mat-error>El NIF es obligatorio</mat-error>
                  }
                  @if (form.controls.nif.touched && form.controls.nif.hasError('pattern')) {
                    <mat-error>El NIF debe tener 9 caracteres (letras y dígitos)</mat-error>
                  }
                </mat-form-field>
              </div>

              <div class="sig-form-row">
                <mat-form-field class="sig-form-field">
                  <mat-label>Correo de contacto</mat-label>
                  <input matInput type="email" formControlName="contactoEmail" data-testid="input-email" />
                  @if (form.controls.contactoEmail.touched && form.controls.contactoEmail.hasError('email')) {
                    <mat-error>Introduce un correo válido</mat-error>
                  }
                </mat-form-field>
                <mat-form-field class="sig-form-field">
                  <mat-label>Teléfono</mat-label>
                  <input matInput formControlName="contactoTelefono" data-testid="input-telefono" />
                </mat-form-field>
              </div>

              <div class="sig-form-row">
                <mat-form-field class="sig-form-field">
                  <mat-label>Estado *</mat-label>
                  <mat-select formControlName="estado" data-testid="select-estado">
                    <mat-option value="Activo">Activo</mat-option>
                    <mat-option value="Inactivo">Inactivo</mat-option>
                  </mat-select>
                </mat-form-field>
              </div>

              <h3 class="sig-form-section">Dirección</h3>
              <div class="sig-form-row">
                <mat-form-field class="sig-form-field sig-form-field--full">
                  <mat-label>Dirección</mat-label>
                  <input matInput formControlName="direccion" data-testid="input-direccion" />
                </mat-form-field>
              </div>
              <div class="sig-form-row">
                <mat-form-field class="sig-form-field">
                  <mat-label>Ciudad</mat-label>
                  <input matInput formControlName="ciudad" data-testid="input-ciudad" />
                </mat-form-field>
                <mat-form-field class="sig-form-field">
                  <mat-label>Provincia</mat-label>
                  <input matInput formControlName="provincia" data-testid="input-provincia" />
                </mat-form-field>
              </div>
              <div class="sig-form-row">
                <mat-form-field class="sig-form-field">
                  <mat-label>Código Postal</mat-label>
                  <input matInput formControlName="codigoPostal" data-testid="input-cp" />
                </mat-form-field>
                <mat-form-field class="sig-form-field">
                  <mat-label>País</mat-label>
                  <input matInput formControlName="pais" data-testid="input-pais" />
                </mat-form-field>
              </div>
              <div class="sig-form-row">
                <mat-form-field class="sig-form-field">
                  <mat-label>Nombre de contacto</mat-label>
                  <input matInput formControlName="contactoNombre" data-testid="input-contacto-nombre" />
                </mat-form-field>
              </div>

              <div class="sig-form-actions">
                <a mat-stroked-button routerLink="/clients" data-testid="btn-cancelar">Cancelar</a>
                <button mat-flat-button color="primary" type="submit" [disabled]="submitting() || form.invalid" data-testid="btn-guardar">
                  @if (submitting()) {
                    <mat-spinner diameter="20" />
                  } @else {
                    Guardar
                  }
                </button>
              </div>
            </form>
          </mat-card-content>
        </mat-card>
      }
    </div>
  `,
  styles: [`
    .sig-form-section {
      font-size: 14px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.08em;
      color: var(--mat-sys-on-surface-variant); margin: 16px 0 8px; padding-bottom: 8px;
      border-bottom: 1px solid var(--mat-sys-outline-variant);
    }
    .sig-form-row {
      display: grid; grid-template-columns: 1fr 1fr; gap: 16px;
    }
    .sig-form-field { width: 100%; }
    .sig-form-field--full { grid-column: 1 / -1; }
    .sig-form-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 16px; }
    @media (max-width: 599px) {
      .sig-form-row { grid-template-columns: 1fr; }
    }
  `],
})
export class ClientFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly clientSvc = inject(ClientService);
  private readonly notify = inject(NotifyService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly id = signal<number | null>(null);
  protected readonly isEdit = signal(false);
  protected readonly loading = signal(false);
  protected readonly submitting = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    nombre: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
    nif: ['', [Validators.required, Validators.pattern(/^[XYZA-Z]?\d{7,8}[A-Z0-9]$/)]],
    estado: ['Activo' as 'Activo' | 'Inactivo', [Validators.required]],
    direccion: [''],
    ciudad: [''],
    provincia: [''],
    pais: [''],
    codigoPostal: [''],
    contactoNombre: [''],
    contactoEmail: ['', [Validators.email]],
    contactoTelefono: [''],
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.id.set(Number(idParam));
      this.isEdit.set(true);
      this.loadEntity();
    }
  }

  private loadEntity(): void {
    this.loading.set(true);
    this.clientSvc.getById(this.id()!).subscribe({
      next: (c) => {
        this.form.patchValue({
          nombre: c.nombre, nif: c.nif, estado: c.estado,
          direccion: c.direccion ?? '', ciudad: c.ciudad ?? '', provincia: c.provincia ?? '',
          pais: c.pais ?? '', codigoPostal: c.codigoPostal ?? '',
          contactoNombre: c.contactoNombre ?? '', contactoEmail: c.contactoEmail ?? '',
          contactoTelefono: c.contactoTelefono ?? '',
        });
        this.loading.set(false);
      },
      error: () => { this.loading.set(false); this.notify.error('No se pudo cargar el cliente'); },
    });
  }

  protected submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const payload = this.form.getRawValue();
    this.submitting.set(true);
    const obs = this.isEdit()
      ? this.clientSvc.update(this.id()!, payload)
      : this.clientSvc.create(payload);
    obs.subscribe({
      next: (c) => {
        this.submitting.set(false);
        this.notify.success(this.isEdit() ? 'Cliente actualizado' : 'Cliente creado');
        void this.router.navigate(['/clients', c.id]);
      },
      error: (err) => {
        this.submitting.set(false);
        this.notify.error(err?.error?.title ?? 'No se pudo guardar el cliente');
      },
    });
  }
}
