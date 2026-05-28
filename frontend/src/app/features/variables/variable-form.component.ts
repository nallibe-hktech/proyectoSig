import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, FormArray, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { VariableService } from '../../core/api/misc.service';
import { NotifyService } from '../../core/notify.service';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { VariableMapeo } from '../../models/dtos';

@Component({
  selector: 'app-variable-form',
  standalone: true,
  imports: [
    CommonModule, RouterLink, ReactiveFormsModule,
    MatCardModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatIconModule, MatProgressSpinnerModule,
    BreadcrumbsComponent, SkeletonComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Variables', route: '/variables' }, { label: isEdit() ? 'Editar' : 'Nueva' }]" />
      <div class="sig-page__header"><h1 class="sig-page__title">{{ isEdit() ? 'Editar Variable' : 'Nueva Variable' }}</h1></div>
      @if (loading()) { <mat-card><mat-card-content><sig-skeleton [count]="4" /></mat-card-content></mat-card> }
      @else {
        <mat-card><mat-card-content>
          <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
            <div class="sig-form-row">
              <mat-form-field class="sig-form-field"><mat-label>Nombre de la variable *</mat-label>
                <input matInput formControlName="nombre" data-testid="input-nombre-variable" />
                @if (form.controls.nombre.touched && form.controls.nombre.hasError('required')) { <mat-error>El nombre es obligatorio</mat-error> }
              </mat-form-field>
              <mat-form-field class="sig-form-field"><mat-label>ID de pregunta Celero (questionId) *</mat-label>
                <input matInput formControlName="questionIdExterno" placeholder="Q08" data-testid="input-question-id" />
                <mat-hint>Referencia al campo de la encuesta en Celero CRM</mat-hint>
                @if (form.controls.questionIdExterno.touched && form.controls.questionIdExterno.hasError('required')) { <mat-error>El questionId es obligatorio</mat-error> }
              </mat-form-field>
            </div>

            <h3 class="sig-form-section">Mapeo de respuestas</h3>
            <p style="font-size: 13px; color: var(--mat-sys-on-surface-variant); margin: 0 0 12px;">Si la respuesta a <strong>{{ form.controls.questionIdExterno.value || '(questionId)' }}</strong> es...</p>

            <div formArrayName="mapeos" class="sig-mapeos">
              @for (group of mapeos.controls; track $index; let i = $index) {
                <div [formGroupName]="i" class="sig-mapeo-row">
                  <mat-form-field appearance="outline" style="flex: 1;">
                    <mat-label>Respuesta</mat-label>
                    <input matInput formControlName="respuesta" [attr.data-testid]="'input-respuesta-' + i" />
                  </mat-form-field>
                  <mat-icon style="margin: 0 8px;">arrow_forward</mat-icon>
                  <mat-form-field appearance="outline" style="width: 140px;">
                    <mat-label>Valor</mat-label>
                    <input matInput type="number" formControlName="valor" [attr.data-testid]="'input-valor-' + i" />
                  </mat-form-field>
                  <button mat-icon-button type="button" (click)="removeMapeo(i)" aria-label="Eliminar fila" [attr.data-testid]="'btn-remove-mapeo-' + i"><mat-icon>delete</mat-icon></button>
                </div>
              }
            </div>
            <button mat-stroked-button type="button" (click)="addMapeo()" data-testid="btn-add-mapeo"><mat-icon>add</mat-icon> Añadir fila de mapeo</button>

            <h3 class="sig-form-section" style="margin-top: 16px;">Vista previa</h3>
            <pre class="sig-preview mono-num">{{ preview() }}</pre>

            <div class="sig-form-actions">
              <a mat-stroked-button routerLink="/variables" data-testid="btn-cancelar">Cancelar</a>
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
    .sig-mapeo-row { display: flex; align-items: center; margin-bottom: 8px; }
    .sig-form-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 16px; }
    .sig-preview { background: var(--mat-sys-surface-variant); padding: 12px; border-radius: 8px; font-size: 13px; }
    @media (max-width: 599px) { .sig-form-row { grid-template-columns: 1fr; } }
  `],
})
export class VariableFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly variableSvc = inject(VariableService);
  private readonly notify = inject(NotifyService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly id = signal<number | null>(null);
  protected readonly isEdit = signal(false);
  protected readonly loading = signal(false);
  protected readonly submitting = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    nombre: ['', [Validators.required]],
    questionIdExterno: ['', [Validators.required]],
    mapeos: this.fb.array([this.createMapeoGroup({ respuesta: '', valor: 0 })]),
  });

  protected get mapeos(): FormArray { return this.form.controls.mapeos as FormArray; }

  protected readonly preview = computed(() => {
    const v = this.form.getRawValue();
    const lines = (v.mapeos as VariableMapeo[]).map((m) => `Si respuesta a [${v.questionIdExterno || '?'}] es [${m.respuesta || '?'}] = ${m.valor}`);
    return lines.length > 0 ? lines.join('\n') : '(sin mapeos)';
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.id.set(Number(idParam));
      this.isEdit.set(true);
      this.loading.set(true);
      this.variableSvc.getById(this.id()!).subscribe({
        next: (v) => {
          this.form.patchValue({ nombre: v.nombre, questionIdExterno: v.questionIdExterno });
          this.mapeos.clear();
          try {
            const ms = JSON.parse(v.mapeoValoresJson) as VariableMapeo[];
            ms.forEach((m) => this.mapeos.push(this.createMapeoGroup(m)));
          } catch { /* ignore */ }
          if (this.mapeos.length === 0) this.mapeos.push(this.createMapeoGroup({ respuesta: '', valor: 0 }));
          this.loading.set(false);
        },
        error: () => { this.loading.set(false); this.notify.error('No se pudo cargar la variable'); },
      });
    }
  }

  private createMapeoGroup(m: VariableMapeo) {
    return this.fb.nonNullable.group({
      respuesta: [m.respuesta, [Validators.required]],
      valor: [m.valor, [Validators.required]],
    });
  }

  protected addMapeo(): void { this.mapeos.push(this.createMapeoGroup({ respuesta: '', valor: 0 })); }
  protected removeMapeo(idx: number): void { if (this.mapeos.length > 1) this.mapeos.removeAt(idx); }

  protected submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const payload = {
      nombre: v.nombre, questionIdExterno: v.questionIdExterno,
      mapeoValoresJson: JSON.stringify(v.mapeos),
    };
    this.submitting.set(true);
    const obs = this.isEdit() ? this.variableSvc.update(this.id()!, payload) : this.variableSvc.create(payload);
    obs.subscribe({
      next: () => { this.submitting.set(false); this.notify.success(this.isEdit() ? 'Variable actualizada' : 'Variable creada'); void this.router.navigate(['/variables']); },
      error: (err) => { this.submitting.set(false); this.notify.error(err?.error?.title ?? 'No se pudo guardar'); },
    });
  }
}
