import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule, DecimalPipe, DatePipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { CalculationService } from '../../core/api/misc.service';
import { CalculationDetailDto } from '../../models/dtos';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { NotifyService } from '../../core/notify.service';

@Component({
  selector: 'app-calculation-detail',
  standalone: true,
  imports: [CommonModule, DecimalPipe, DatePipe, MatCardModule, MatIconModule, MatChipsModule, BreadcrumbsComponent, SkeletonComponent],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Cálculos' }, { label: detail()?.conceptNombre ?? 'Detalle' }]" />
      <div class="sig-page__header"><h1 class="sig-page__title">Detalle de cálculo</h1></div>

      @if (loading()) { <mat-card><mat-card-content><sig-skeleton [count]="6" /></mat-card-content></mat-card> }
      @else if (detail()) {
        <mat-card style="margin-bottom: 16px;">
          <mat-card-header><mat-card-title>Identificación</mat-card-title></mat-card-header>
          <mat-card-content>
            <dl class="sig-dl">
              <dt>Concepto</dt><dd>{{ detail()!.conceptNombre }}</dd>
              <dt>Closure Line ID</dt><dd class="mono-num">{{ detail()!.closureLineId }}</dd>
              <dt>Sistema origen</dt><dd><mat-chip>{{ detail()!.sistemaOrigen }}</mat-chip></dd>
              <dt>Calculado</dt><dd class="mono-num">{{ detail()!.timestamp | date:'dd/MM/yyyy HH:mm:ss' }}</dd>
            </dl>
          </mat-card-content>
        </mat-card>

        <mat-card style="margin-bottom: 16px;">
          <mat-card-header><mat-card-title>Fórmula (snapshot inmutable)</mat-card-title></mat-card-header>
          <mat-card-content>
            <pre class="sig-json mono-num" data-testid="formula-snapshot">{{ formattedFormula() }}</pre>
          </mat-card-content>
        </mat-card>

        <mat-card style="margin-bottom: 16px;">
          <mat-card-header><mat-card-title>Datos de entrada (inputs)</mat-card-title></mat-card-header>
          <mat-card-content>
            <pre class="sig-json mono-num" data-testid="inputs-snapshot">{{ formattedInputs() }}</pre>
          </mat-card-content>
        </mat-card>

        <mat-card>
          <mat-card-header><mat-card-title>Resultado</mat-card-title></mat-card-header>
          <mat-card-content>
            <div class="sig-kpi-value mono-num" data-testid="resultado">{{ detail()!.resultado | number:'1.0-2' }} €</div>
            @if (detail()!.incidencias) {
              <h3 class="sig-form-section" style="margin-top: 12px;">Incidencias</h3>
              <pre class="sig-json mono-num">{{ formattedIncidencias() }}</pre>
            } @else {
              <p style="margin-top: 8px; color: var(--sig-success);">
                <mat-icon style="vertical-align: middle; font-size: 16px; width: 16px; height: 16px;" aria-hidden="true">check_circle</mat-icon>
                Sin incidencias.
              </p>
            }
          </mat-card-content>
        </mat-card>
      }
    </div>
  `,
  styles: [`
    .sig-dl { display: grid; grid-template-columns: 200px 1fr; gap: 8px 16px; margin: 0; }
    .sig-dl dt { color: var(--mat-sys-on-surface-variant); font-weight: 500; }
    .sig-dl dd { margin: 0; }
    .sig-json { background: var(--mat-sys-surface-variant); padding: 16px; border-radius: 8px; font-size: 12px; overflow: auto; max-height: 400px; white-space: pre-wrap; word-break: break-word; }
    .sig-form-section { font-size: 14px; font-weight: 600; text-transform: uppercase; color: var(--mat-sys-on-surface-variant); margin: 16px 0 8px; }
  `],
})
export class CalculationDetailComponent implements OnInit {
  private readonly calcSvc = inject(CalculationService);
  private readonly route = inject(ActivatedRoute);
  private readonly notify = inject(NotifyService);

  protected readonly detail = signal<CalculationDetailDto | null>(null);
  protected readonly loading = signal(true);

  protected readonly formattedFormula = computed(() => {
    const d = this.detail(); if (!d) return '';
    try { return JSON.stringify(JSON.parse(d.formulaSnapshotJson), null, 2); } catch { return d.formulaSnapshotJson; }
  });
  protected readonly formattedInputs = computed(() => {
    const d = this.detail(); if (!d) return '';
    try { return JSON.stringify(JSON.parse(d.inputsJson), null, 2); } catch { return d.inputsJson; }
  });
  protected readonly formattedIncidencias = computed(() => {
    const d = this.detail(); if (!d || !d.incidencias) return '';
    try { return JSON.stringify(JSON.parse(d.incidencias), null, 2); } catch { return d.incidencias; }
  });

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('closureLineId'));
    this.calcSvc.getByLine(id).subscribe({
      next: (d) => { this.detail.set(d); this.loading.set(false); },
      error: () => { this.loading.set(false); this.notify.error('No se pudo cargar el detalle'); },
    });
  }
}
