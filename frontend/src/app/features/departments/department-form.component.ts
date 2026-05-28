import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DepartmentService } from '../../core/api/catalogs.service';
import { NotifyService } from '../../core/notify.service';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';

@Component({
  selector: 'app-department-form',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule, MatCardModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule, BreadcrumbsComponent],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Departments', route: '/departments' }, { label: isEdit() ? 'Editar' : 'Nuevo' }]" />
      <div class="sig-page__header"><h1 class="sig-page__title">{{ isEdit() ? 'Editar Department' : 'Nuevo Department' }}</h1></div>
      <mat-card><mat-card-content>
        <form [formGroup]="form" (ngSubmit)="submit()" novalidate style="max-width: 480px;">
          <mat-form-field appearance="outline" style="width: 100%;">
            <mat-label>Nombre *</mat-label>
            <input matInput formControlName="nombre" data-testid="input-nombre" />
            @if (form.controls.nombre.touched && form.controls.nombre.hasError('required')) { <mat-error>El nombre es obligatorio</mat-error> }
          </mat-form-field>
          <div style="display: flex; justify-content: flex-end; gap: 8px; margin-top: 16px;">
            <a mat-stroked-button routerLink="/departments" data-testid="btn-cancelar">Cancelar</a>
            <button mat-flat-button color="primary" type="submit" [disabled]="submitting() || form.invalid" data-testid="btn-guardar">
              @if (submitting()) { <mat-spinner diameter="20" /> } @else { Guardar }
            </button>
          </div>
        </form>
      </mat-card-content></mat-card>
    </div>
  `,
})
export class DepartmentFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly deptSvc = inject(DepartmentService);
  private readonly notify = inject(NotifyService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly id = signal<number | null>(null);
  protected readonly isEdit = signal(false);
  protected readonly submitting = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    nombre: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.id.set(Number(idParam));
      this.isEdit.set(true);
      this.deptSvc.list().subscribe((ds) => {
        const d = ds.find((x) => x.id === this.id());
        if (d) this.form.patchValue({ nombre: d.nombre });
      });
    }
  }

  protected submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const payload = this.form.getRawValue();
    this.submitting.set(true);
    const obs = this.isEdit() ? this.deptSvc.update(this.id()!, payload) : this.deptSvc.create(payload);
    obs.subscribe({
      next: () => { this.submitting.set(false); this.notify.success(this.isEdit() ? 'Actualizado' : 'Creado'); void this.router.navigate(['/departments']); },
      error: (err) => { this.submitting.set(false); this.notify.error(err?.error?.title ?? 'No se pudo guardar'); },
    });
  }
}
