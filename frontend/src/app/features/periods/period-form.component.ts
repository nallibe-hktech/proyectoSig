import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PeriodService } from '../../core/api/periods.service';
import { NotifyService } from '../../core/notify.service';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';

@Component({
  selector: 'app-period-form',
  standalone: true,
  imports: [
    CommonModule, RouterLink, ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatIconModule, MatDatepickerModule, MatNativeDateModule, MatProgressSpinnerModule,
    BreadcrumbsComponent, SkeletonComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Periods', route: '/periods' }, { label: isEdit() ? 'Editar' : 'Nuevo' }]" />
      <div class="sig-page__header"><h1 class="sig-page__title">{{ isEdit() ? 'Editar Period' : 'Nuevo Period' }}</h1></div>
      @if (loading()) { <mat-card><mat-card-content><sig-skeleton [count]="3" /></mat-card-content></mat-card> }
      @else {
        <mat-card><mat-card-content>
          <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
            <mat-form-field class="sig-form-field" style="width: 100%; max-width: 400px;">
              <mat-label>Nombre *</mat-label>
              <input matInput formControlName="nombre" placeholder="Marzo 2026" data-testid="input-nombre" />
              @if (form.controls.nombre.touched && form.controls.nombre.hasError('required')) { <mat-error>El nombre es obligatorio</mat-error> }
            </mat-form-field>
            <div class="sig-form-row">
              <mat-form-field class="sig-form-field"><mat-label>Fecha inicio *</mat-label>
                <input matInput [matDatepicker]="dpI" formControlName="fechaInicio" data-testid="input-inicio" />
                <mat-datepicker-toggle matIconSuffix [for]="dpI" /><mat-datepicker #dpI />
              </mat-form-field>
              <mat-form-field class="sig-form-field"><mat-label>Fecha fin *</mat-label>
                <input matInput [matDatepicker]="dpF" formControlName="fechaFin" data-testid="input-fin" />
                <mat-datepicker-toggle matIconSuffix [for]="dpF" /><mat-datepicker #dpF />
              </mat-form-field>
            </div>
            <mat-form-field class="sig-form-field" style="width: 100%; max-width: 400px;">
              <mat-label>Día de pago *</mat-label>
              <mat-select formControlName="diaPago" data-testid="select-dia-pago">
                <mat-option [value]="30">30</mat-option>
                <mat-option [value]="15">15</mat-option>
                <mat-option [value]="9">9</mat-option>
              </mat-select>
            </mat-form-field>
            <div class="sig-form-actions">
              <a mat-stroked-button routerLink="/periods" data-testid="btn-cancelar">Cancelar</a>
              <button mat-flat-button color="primary" type="submit" [disabled]="submitting() || form.invalid" data-testid="btn-guardar">
                @if (submitting()) { <mat-spinner diameter="20" /> } @else { Guardar }
              </button>
            </div>
          </form>
        </mat-card-content></mat-card>
      }
    </div>
  `,
  styles: [`.sig-form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; max-width: 600px; } .sig-form-field { width: 100%; } .sig-form-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 16px; } @media (max-width: 599px) { .sig-form-row { grid-template-columns: 1fr; } }`],
})
export class PeriodFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly periodSvc = inject(PeriodService);
  private readonly notify = inject(NotifyService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly id = signal<number | null>(null);
  protected readonly isEdit = signal(false);
  protected readonly loading = signal(false);
  protected readonly submitting = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    nombre: ['', [Validators.required]],
    fechaInicio: [new Date(), [Validators.required]],
    fechaFin: [new Date(), [Validators.required]],
    diaPago: [30, [Validators.required]],
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.id.set(Number(idParam));
      this.isEdit.set(true);
      this.loading.set(true);
      this.periodSvc.getById(this.id()!).subscribe({
        next: (p) => {
          this.form.patchValue({ nombre: p.nombre, fechaInicio: new Date(p.fechaInicio), fechaFin: new Date(p.fechaFin), diaPago: p.diaPago });
          this.loading.set(false);
        },
        error: () => { this.loading.set(false); this.notify.error('No se pudo cargar el período'); },
      });
    }
  }

  protected submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const payload = { nombre: v.nombre, fechaInicio: v.fechaInicio.toISOString().slice(0, 10), fechaFin: v.fechaFin.toISOString().slice(0, 10), diaPago: v.diaPago };
    this.submitting.set(true);
    const obs = this.isEdit() ? this.periodSvc.update(this.id()!, payload) : this.periodSvc.create(payload);
    obs.subscribe({
      next: () => { this.submitting.set(false); this.notify.success(this.isEdit() ? 'Período actualizado' : 'Período creado'); void this.router.navigate(['/periods']); },
      error: (err) => { this.submitting.set(false); this.notify.error(err?.error?.title ?? 'No se pudo guardar'); },
    });
  }
}
