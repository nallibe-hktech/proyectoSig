import { Component, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { SyncService } from '../../core/api/misc.service';
import { SyncResultDto, ProcessingResultDto } from '../../models/dtos';
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
        Fuerza la sincronización con sistemas externos. En Development se usan datos sintéticos deterministas.
      </p>

      <div class="sig-sync-grid">
        <!-- Sync systems -->
        @for (s of systems; track s.id) {
          <mat-card>
            <mat-card-header>
              <mat-icon mat-card-avatar>{{ s.icon }}</mat-icon>
              <mat-card-title>{{ s.label }}</mat-card-title>
              <mat-card-subtitle>{{ s.desc }}</mat-card-subtitle>
            </mat-card-header>
            <mat-card-content>
              @if (syncResults()[s.id]) {
                <div class="sig-sync-result">
                  <div>
                    <strong>Estado:</strong>
                    <span [style.color]="syncResults()[s.id]!.exito ? 'var(--mat-sys-primary)' : 'var(--mat-sys-error)'">
                      {{ syncResults()[s.id]!.exito ? '✓ Éxito' : '✗ Error' }}
                    </span>
                  </div>
                  <div><strong>Insertados:</strong> <span class="mono-num">{{ syncResults()[s.id]!.registrosInsertados }}</span></div>
                  <div><strong>Actualizados:</strong> <span class="mono-num">{{ syncResults()[s.id]!.registrosActualizados }}</span></div>
                  <div><strong>Errores:</strong> <span class="mono-num">{{ syncResults()[s.id]!.registrosError }}</span></div>
                  @if (syncResults()[s.id]!.fechaUltimaSincronizacion) {
                    <div style="font-size: 12px; color: var(--mat-sys-on-surface-variant); margin-top: 8px;">
                      Última: {{ syncResults()[s.id]!.fechaUltimaSincronizacion | date:'dd/MM/yyyy HH:mm:ss' }}
                    </div>
                  }
                </div>
              }
            </mat-card-content>
            <mat-card-actions align="end">
              <button mat-flat-button color="primary" (click)="onSync(s.id)" [disabled]="syncLoading()[s.id]" [attr.data-testid]="'btn-sync-' + s.id">
                @if (syncLoading()[s.id]) { <mat-spinner diameter="20" /> } @else { <ng-container><mat-icon>refresh</mat-icon> Sincronizar</ng-container> }
              </button>
            </mat-card-actions>
          </mat-card>
        }

        <!-- Processing card -->
        <mat-card>
          <mat-card-header>
            <mat-icon mat-card-avatar>settings_backup_restore</mat-icon>
            <mat-card-title>Procesar Registros</mat-card-title>
            <mat-card-subtitle>Migrar staging → productivo</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            @if (processingResult()) {
              <div class="sig-sync-result">
                <div>
                  <strong>Total procesado:</strong> <span class="mono-num">{{ processingResult()!.totalProcessed }}</span>
                </div>
                <div>
                  <strong>Errores totales:</strong> <span class="mono-num">{{ processingResult()!.totalErrors }}</span>
                </div>
                @if (processingResult()!.error) {
                  <div style="color: var(--mat-sys-error); font-size: 12px; margin-top: 8px;">
                    Error: {{ processingResult()!.error }}
                  </div>
                }
                <div style="font-size: 12px; color: var(--mat-sys-on-surface-variant); margin-top: 8px;">
                  Timestamp: {{ processingResult()!.timestamp | date:'dd/MM/yyyy HH:mm:ss' }}
                </div>
              </div>
            }
          </mat-card-content>
          <mat-card-actions align="end">
            <button mat-flat-button color="accent" (click)="onProcess()" [disabled]="processingLoading()" [attr.data-testid]="'btn-process'">
              @if (processingLoading()) { <mat-spinner diameter="20" /> } @else { <ng-container><mat-icon>play_arrow</mat-icon> Procesar</ng-container> }
            </button>
          </mat-card-actions>
        </mat-card>
      </div>
    </div>
  `,
  styles: [`
    .sig-sync-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 16px; }
    .sig-sync-result { background: var(--mat-sys-surface-variant); padding: 12px; border-radius: 8px; font-size: 13px; }
    .sig-sync-result > div { margin-bottom: 4px; }
    .mono-num { font-family: monospace; font-weight: 500; }
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

  protected readonly syncResults = signal<Partial<Record<System, SyncResultDto>>>({});
  protected readonly syncLoading = signal<Partial<Record<System, boolean>>>({});
  protected readonly processingResult = signal<ProcessingResultDto | null>(null);
  protected readonly processingLoading = signal(false);

  protected onSync(system: System): void {
    this.syncLoading.update((l) => ({ ...l, [system]: true }));
    this.syncSvc.sync(system).subscribe({
      next: (r) => {
        this.syncLoading.update((l) => ({ ...l, [system]: false }));
        this.syncResults.update((v) => ({ ...v, [system]: r }));
        const msg = `${system}: ${r.registrosInsertados} inserción${r.registrosInsertados === 1 ? '' : 'es'}, ${r.registrosActualizados} actualización${r.registrosActualizados === 1 ? '' : 'es'}, ${r.registrosError} error${r.registrosError === 1 ? '' : 'es'}`;
        this.notify.success(`Sync ${msg}`);
      },
      error: (err) => {
        this.syncLoading.update((l) => ({ ...l, [system]: false }));
        this.notify.error(err?.error?.title ?? `No se pudo sincronizar ${system}`);
      },
    });
  }

  protected onProcess(): void {
    this.processingLoading.set(true);
    this.syncSvc.processAll().subscribe({
      next: (r) => {
        this.processingLoading.set(false);
        this.processingResult.set(r);
        const msg = `${r.totalProcessed} registros procesados, ${r.totalErrors} error${r.totalErrors === 1 ? '' : 'es'}`;
        this.notify.success(`Procesamiento: ${msg}`);
      },
      error: (err) => {
        this.processingLoading.set(false);
        this.notify.error(err?.error?.title ?? 'No se pudo procesar los registros');
      },
    });
  }
}
