import { Component, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { SyncService } from '../../core/api/misc.service';
import { SyncResultDto } from '../../models/dtos';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { NotifyService } from '../../core/notify.service';

type System = 'celero' | 'bizneo' | 'intratime' | 'payhawk';

@Component({
  selector: 'app-sync',
  standalone: true,
  imports: [CommonModule, DatePipe, MatCardModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, BreadcrumbsComponent],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Sincronizaciones' }]" />
      <div class="sig-page__header"><h1 class="sig-page__title">Sincronizaciones</h1></div>

      <p style="color: var(--mat-sys-on-surface-variant); margin-bottom: 24px;">
        Fuerza la sincronización con un sistema externo. En Development se usan datos sintéticos deterministas (semilla 20260101).
      </p>

      <div class="sig-sync-grid">
        @for (s of systems; track s.id) {
          <mat-card>
            <mat-card-header>
              <mat-icon mat-card-avatar>{{ s.icon }}</mat-icon>
              <mat-card-title>{{ s.label }}</mat-card-title>
              <mat-card-subtitle>{{ s.desc }}</mat-card-subtitle>
            </mat-card-header>
            <mat-card-content>
              @if (results()[s.id]) {
                <div class="sig-sync-result">
                  <div><strong>Insertadas:</strong> <span class="mono-num">{{ results()[s.id]!.filasInsertadas }}</span></div>
                  <div><strong>Duplicadas:</strong> <span class="mono-num">{{ results()[s.id]!.filasDuplicadasIgnoradas }}</span></div>
                  <div><strong>Errores:</strong> <span class="mono-num">{{ results()[s.id]!.filasError }}</span></div>
                  <div style="font-size: 12px; color: var(--mat-sys-on-surface-variant); margin-top: 8px;">
                    Última: {{ results()[s.id]!.fechaUltimaSincronizacion | date:'dd/MM/yyyy HH:mm:ss' }}
                  </div>
                </div>
              }
            </mat-card-content>
            <mat-card-actions align="end">
              <button mat-flat-button color="primary" (click)="onSync(s.id)" [disabled]="loading()[s.id]" [attr.data-testid]="'btn-sync-' + s.id">
                @if (loading()[s.id]) { <mat-spinner diameter="20" /> } @else { <ng-container><mat-icon>refresh</mat-icon> Sincronizar</ng-container> }
              </button>
            </mat-card-actions>
          </mat-card>
        }
      </div>
    </div>
  `,
  styles: [`
    .sig-sync-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 16px; }
    .sig-sync-result { background: var(--mat-sys-surface-variant); padding: 12px; border-radius: 8px; font-size: 13px; }
    .sig-sync-result > div { margin-bottom: 4px; }
  `],
})
export class SyncComponent {
  private readonly syncSvc = inject(SyncService);
  private readonly notify = inject(NotifyService);

  protected readonly systems: { id: System; label: string; desc: string; icon: string }[] = [
    { id: 'celero', label: 'Celero', desc: 'CRM — Visitas', icon: 'storefront' },
    { id: 'bizneo', label: 'Bizneo', desc: 'RRHH — Empleados y horas', icon: 'badge' },
    { id: 'intratime', label: 'Intratime', desc: 'Fichajes', icon: 'schedule' },
    { id: 'payhawk', label: 'PayHawk', desc: 'Gastos', icon: 'payments' },
  ];

  protected readonly results = signal<Partial<Record<System, SyncResultDto>>>({});
  protected readonly loading = signal<Partial<Record<System, boolean>>>({});

  protected onSync(system: System): void {
    this.loading.update((l) => ({ ...l, [system]: true }));
    this.syncSvc.sync(system).subscribe({
      next: (r) => {
        this.loading.update((l) => ({ ...l, [system]: false }));
        this.results.update((v) => ({ ...v, [system]: r }));
        this.notify.success(`Sync ${system}: ${r.filasInsertadas} insertadas, ${r.filasDuplicadasIgnoradas} duplicadas, ${r.filasError} errores`);
      },
      error: (err) => {
        this.loading.update((l) => ({ ...l, [system]: false }));
        this.notify.error(err?.error?.title ?? `No se pudo sincronizar ${system}`);
      },
    });
  }
}
