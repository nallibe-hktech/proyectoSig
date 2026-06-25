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
import { ConceptService } from '../../core/api/concepts.service';
import { ServiceService } from '../../core/api/services.service';
import { NotifyService } from '../../core/notify.service';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { ServiceListItemDto } from '../../models/dtos';

@Component({
  selector: 'app-concept-form',
  standalone: true,
  imports: [
    CommonModule, RouterLink, ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatIconModule, MatDatepickerModule, MatNativeDateModule, MatProgressSpinnerModule,
    BreadcrumbsComponent, SkeletonComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Concepts', route: '/concepts' }, { label: isEdit() ? 'Editar' : 'Nuevo' }]" />
      <div class="sig-page__header"><h1 class="sig-page__title">{{ isEdit() ? 'Editar Concept' : 'Nuevo Concept' }}</h1></div>

      @if (loading()) { <mat-card><mat-card-content><sig-skeleton [count]="4" /></mat-card-content></mat-card> }
      @else {
        <mat-card><mat-card-content>
          <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
            <div class="sig-form-row">
              <mat-form-field class="sig-form-field"><mat-label>Nombre *</mat-label>
                <input matInput formControlName="nombre" data-testid="input-nombre" />
                @if (form.controls.nombre.touched && form.controls.nombre.hasError('required')) { <mat-error>El nombre es obligatorio</mat-error> }
              </mat-form-field>
              <mat-form-field class="sig-form-field"><mat-label>Tipo *</mat-label>
                <mat-select formControlName="tipo" data-testid="select-tipo">
                  <mat-option value="Pago">Pago</mat-option>
                  <mat-option value="Factura">Factura</mat-option>
                </mat-select>
              </mat-form-field>
            </div>
            <div class="sig-form-row">
              <mat-form-field class="sig-form-field"><mat-label>Fecha desde *</mat-label>
                <input matInput [matDatepicker]="dpDesde" formControlName="fechaDesde" data-testid="input-desde" />
                <mat-datepicker-toggle matIconSuffix [for]="dpDesde" /><mat-datepicker #dpDesde />
              </mat-form-field>
              <mat-form-field class="sig-form-field"><mat-label>Fecha hasta</mat-label>
                <input matInput [matDatepicker]="dpHasta" formControlName="fechaHasta" data-testid="input-hasta" />
                <mat-datepicker-toggle matIconSuffix [for]="dpHasta" /><mat-datepicker #dpHasta />
              </mat-form-field>
            </div>
            <div class="sig-form-row">
              <mat-form-field class="sig-form-field">
                <mat-label>Servicios asociados</mat-label>
                <mat-select [value]="selectedServiceIds()" (selectionChange)="onServicesChange($event.value)" multiple data-testid="select-servicios">
                  @for (svc of availableServices(); track svc.id) {
                    <mat-option [value]="svc.id">{{ svc.nombre }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>
            </div>
            <p style="margin: 12px 0; font-size: 13px; color: var(--mat-sys-on-surface-variant);">
              <mat-icon style="vertical-align: middle; font-size: 16px; width: 16px; height: 16px;" aria-hidden="true">info</mat-icon>
              La fórmula se edita visualmente desde el botón <strong>Editor de Fórmula</strong> en el detalle del concept.
            </p>
            <div class="sig-form-actions">
              <a mat-stroked-button routerLink="/concepts" data-testid="btn-cancelar">Cancelar</a>
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
    .sig-form-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 16px; }
    @media (max-width: 599px) { .sig-form-row { grid-template-columns: 1fr; } }
  `],
})
export class ConceptFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly conceptSvc = inject(ConceptService);
  private readonly serviceSvc = inject(ServiceService);
  private readonly notify = inject(NotifyService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly id = signal<number | null>(null);
  protected readonly isEdit = signal(false);
  protected readonly loading = signal(false);
  protected readonly submitting = signal(false);
  protected readonly availableServices = signal<ServiceListItemDto[]>([]);
  protected readonly selectedServiceIds = signal<number[]>([]);

  protected readonly form = this.fb.nonNullable.group({
    nombre: ['', [Validators.required]],
    tipo: ['Pago' as 'Pago' | 'Factura', [Validators.required]],
    fechaDesde: [new Date(), [Validators.required]],
    fechaHasta: [null as Date | null],
  });
  private formulaJson = '{}';
  private serviceIds: number[] = [];
  private userIds: number[] = [];

  ngOnInit(): void {
    // Load available services
    this.serviceSvc.list(1, 1000).subscribe({
      next: (result) => {
        this.availableServices.set(result.items);
      },
      error: () => {
        this.notify.error('No se pudieron cargar los servicios');
      },
    });

    // Load concept if editing
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.id.set(Number(idParam));
      this.isEdit.set(true);
      this.loading.set(true);
      this.conceptSvc.getById(this.id()!).subscribe({
        next: (c) => {
          this.form.patchValue({
            nombre: c.nombre, tipo: c.tipo,
            fechaDesde: new Date(c.fechaDesde),
            fechaHasta: c.fechaHasta ? new Date(c.fechaHasta) : null,
          });
          this.formulaJson = c.formulaJson;
          this.serviceIds = c.serviceIds;
          this.selectedServiceIds.set(c.serviceIds);
          this.userIds = c.userIds;
          this.loading.set(false);
        },
        error: () => { this.loading.set(false); this.notify.error('No se pudo cargar el concept'); },
      });
    }
  }

  protected onServicesChange(value: number[]): void {
    this.selectedServiceIds.set(value);
    this.serviceIds = value;
  }

  protected submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const payload = {
      nombre: v.nombre, tipo: v.tipo,
      fechaDesde: v.fechaDesde.toISOString().slice(0, 10),
      fechaHasta: v.fechaHasta ? v.fechaHasta.toISOString().slice(0, 10) : null,
      formulaJson: this.formulaJson, serviceIds: this.serviceIds, userIds: this.userIds,
    };
    this.submitting.set(true);
    const obs = this.isEdit() ? this.conceptSvc.update(this.id()!, payload) : this.conceptSvc.create(payload);
    obs.subscribe({
      next: (c) => { this.submitting.set(false); this.notify.success(this.isEdit() ? 'Concept actualizado' : 'Concept creado'); void this.router.navigate(['/concepts', c.id]); },
      error: (err) => { this.submitting.set(false); this.notify.error(err?.error?.title ?? 'No se pudo guardar'); },
    });
  }
}
