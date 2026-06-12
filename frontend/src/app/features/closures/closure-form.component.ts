import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ClosureService } from '../../core/api/closures.service';
import { ServiceService } from '../../core/api/services.service';
import { PeriodService } from '../../core/api/periods.service';
import { NotifyService } from '../../core/notify.service';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { ServiceListItemDto, PeriodDto } from '../../models/dtos';

@Component({
  selector: 'app-closure-form',
  standalone: true,
  imports: [
    CommonModule, RouterLink, ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatIconModule, MatProgressSpinnerModule,
    BreadcrumbsComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Closures', route: '/closures' }, { label: 'Nuevo cierre' }]" />
      <div class="sig-page__header"><h1 class="sig-page__title">Nuevo cierre</h1></div>
      <mat-card><mat-card-content>
        <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
          <p style="font-size: 14px; color: var(--mat-sys-on-surface-variant);">
            Selecciona el servicio y el período. Tras crear el cierre, el motor calculará automáticamente todas las líneas.
          </p>
          <div class="sig-form-row">
            <mat-form-field class="sig-form-field"><mat-label>Servicio *</mat-label>
              <mat-select formControlName="serviceId" data-testid="select-servicio">
                @for (p of services(); track p.id) { <mat-option [value]="p.id">{{ p.nombre }} ({{ p.clientNombre }})</mat-option> }
              </mat-select>
            </mat-form-field>
            <mat-form-field class="sig-form-field"><mat-label>Período *</mat-label>
              <mat-select formControlName="periodId" data-testid="select-periodo">
                @for (p of periodos(); track p.id) { <mat-option [value]="p.id" [disabled]="p.estado !== 'Abierto'">{{ p.nombre }} ({{ p.estado }})</mat-option> }
              </mat-select>
              <mat-hint>Solo períodos en estado Abierto pueden recibir cierres</mat-hint>
            </mat-form-field>
          </div>
          <mat-form-field class="sig-form-field sig-form-field--full">
            <mat-label>Comentarios</mat-label>
            <textarea matInput formControlName="comentarios" rows="3" data-testid="input-comentarios"></textarea>
          </mat-form-field>
          <div class="sig-form-actions">
            <a mat-stroked-button routerLink="/closures" data-testid="btn-cancelar">Cancelar</a>
            <button mat-flat-button color="primary" type="submit" [disabled]="submitting() || form.invalid" data-testid="btn-guardar">
              @if (submitting()) { <mat-spinner diameter="20" /> } @else { Crear cierre }
            </button>
          </div>
        </form>
      </mat-card-content></mat-card>
    </div>
  `,
  styles: [`
    .sig-form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; max-width: 800px; }
    .sig-form-field { width: 100%; }
    .sig-form-field--full { display: block; max-width: 800px; }
    .sig-form-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 16px; }
    @media (max-width: 599px) { .sig-form-row { grid-template-columns: 1fr; } }
  `],
})
export class ClosureFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly closureSvc = inject(ClosureService);
  private readonly serviceSvc = inject(ServiceService);
  private readonly periodSvc = inject(PeriodService);
  private readonly notify = inject(NotifyService);
  private readonly router = inject(Router);

  protected readonly services = signal<ServiceListItemDto[]>([]);
  protected readonly periodos = signal<PeriodDto[]>([]);
  protected readonly submitting = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    serviceId: [0 as number, [Validators.required, Validators.min(1)]],
    periodId: [0 as number, [Validators.required, Validators.min(1)]],
    comentarios: [''],
  });

  ngOnInit(): void {
    forkJoin({
      services: this.serviceSvc.list(1, 100),
      periodos: this.periodSvc.list(),
    }).subscribe({
      next: (r) => { this.services.set(r.services.items); this.periodos.set(r.periodos); },
      error: () => {},
    });
  }

  protected submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    this.submitting.set(true);
    this.closureSvc.create({ serviceId: v.serviceId, periodId: v.periodId, comentarios: v.comentarios || null }).subscribe({
      next: (c) => { this.submitting.set(false); this.notify.success('Cierre creado'); void this.router.navigate(['/closures', c.id]); },
      error: (err) => { this.submitting.set(false); this.notify.error(err?.error?.title ?? 'No se pudo crear el cierre'); },
    });
  }
}
