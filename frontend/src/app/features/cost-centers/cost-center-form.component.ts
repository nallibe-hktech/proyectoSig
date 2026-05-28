import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CostCenterService } from '../../core/api/catalogs.service';
import { NotifyService } from '../../core/notify.service';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';

@Component({
  selector: 'app-cost-center-form',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule, MatCardModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatProgressSpinnerModule, BreadcrumbsComponent],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Cost Centers', route: '/cost-centers' }, { label: isEdit() ? 'Editar' : 'Nuevo' }]" />
      <div class="sig-page__header"><h1 class="sig-page__title">{{ isEdit() ? 'Editar CECO' : 'Nuevo CECO' }}</h1></div>
      <mat-card><mat-card-content>
        <form [formGroup]="form" (ngSubmit)="submit()" novalidate style="max-width: 480px;">
          <mat-form-field appearance="outline" style="width: 100%;">
            <mat-label>Código *</mat-label>
            <input matInput formControlName="codigo" placeholder="035501" data-testid="input-codigo" />
            @if (form.controls.codigo.touched && form.controls.codigo.hasError('pattern')) {
              <mat-error>El código debe tener 6 dígitos</mat-error>
            }
          </mat-form-field>
          <mat-form-field appearance="outline" style="width: 100%;">
            <mat-label>Nombre *</mat-label>
            <input matInput formControlName="nombre" data-testid="input-nombre" />
            @if (form.controls.nombre.touched && form.controls.nombre.hasError('required')) { <mat-error>El nombre es obligatorio</mat-error> }
          </mat-form-field>
          <div style="display: flex; justify-content: flex-end; gap: 8px; margin-top: 16px;">
            <a mat-stroked-button routerLink="/cost-centers" data-testid="btn-cancelar">Cancelar</a>
            <button mat-flat-button color="primary" type="submit" [disabled]="submitting() || form.invalid" data-testid="btn-guardar">
              @if (submitting()) { <mat-spinner diameter="20" /> } @else { Guardar }
            </button>
          </div>
        </form>
      </mat-card-content></mat-card>
    </div>
  `,
})
export class CostCenterFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly ccSvc = inject(CostCenterService);
  private readonly notify = inject(NotifyService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly id = signal<number | null>(null);
  protected readonly isEdit = signal(false);
  protected readonly submitting = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    codigo: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]],
    nombre: ['', [Validators.required]],
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.id.set(Number(idParam));
      this.isEdit.set(true);
      this.ccSvc.list().subscribe((cs) => {
        const c = cs.find((x) => x.id === this.id());
        if (c) this.form.patchValue({ codigo: c.codigo, nombre: c.nombre });
      });
    }
  }

  protected submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const payload = this.form.getRawValue();
    this.submitting.set(true);
    const obs = this.isEdit() ? this.ccSvc.update(this.id()!, payload) : this.ccSvc.create(payload);
    obs.subscribe({
      next: () => { this.submitting.set(false); this.notify.success(this.isEdit() ? 'Actualizado' : 'Creado'); void this.router.navigate(['/cost-centers']); },
      error: (err) => { this.submitting.set(false); this.notify.error(err?.error?.title ?? 'No se pudo guardar'); },
    });
  }
}
