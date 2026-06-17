import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule, DecimalPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ClosureService } from '../../core/api/closures.service';
import { ExportService } from '../../core/api/misc.service';
import { AuthService } from '../../core/auth/auth.service';
import { NotifyService } from '../../core/notify.service';
import { ClosureDetailDto, ClosureAlertaDto, ApprovalHistoryDto, ClosureLineDto, ClosureLineIncentivoRequest } from '../../models/dtos';
import { ApprovalStep } from '../../models/enums';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { StateBadgeComponent } from '../../shared/state-badge.component';
import { RejectDialogComponent } from './reject-dialog.component';
import { OverrideExceptionDialog } from './override-exception.dialog';
import { IncentivoDialog } from './incentivo.dialog';

@Component({
  selector: 'app-closure-detail',
  standalone: true,
  imports: [
    CommonModule, DecimalPipe, DatePipe, RouterLink,
    MatCardModule, MatButtonModule, MatIconModule, MatTableModule, MatChipsModule, MatDialogModule, MatExpansionModule, MatTooltipModule,
    BreadcrumbsComponent, SkeletonComponent, StateBadgeComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Closures', route: '/closures' }, { label: closure() ? closure()!.serviceNombre + ' — ' + closure()!.periodNombre : 'Detalle' }]" />

      <div class="sig-page__header">
        <h1 class="sig-page__title">{{ closure() ? closure()!.serviceNombre + ' — ' + closure()!.periodNombre : 'Cargando...' }}</h1>
        @if (closure()) {
          <div style="display: flex; gap: 8px; flex-wrap: wrap;">
            @if (canRecalculate()) {
              <button mat-stroked-button (click)="onRecalcular()" data-testid="btn-recalcular"><mat-icon>refresh</mat-icon> Recalcular</button>
            }
            @if (canApprove()) {
              <button mat-stroked-button color="warn" (click)="onRechazar()" data-testid="btn-rechazar"><mat-icon>cancel</mat-icon> Rechazar</button>
              <button
                mat-flat-button
                color="primary"
                (click)="onAprobar()"
                data-testid="btn-aprobar"
                [disabled]="alertasPendientes().length > 0"
                [matTooltip]="alertasPendientes().length > 0 ? 'Hay ' + alertasPendientes().length + ' alerta(s) sin confirmar' : ''">
                <mat-icon>check_circle</mat-icon> Aprobar
              </button>
            }
            @if (closure()!.estado === 'Aprobado' || closure()!.estado === 'Exportado') {
              <button mat-stroked-button (click)="onExportInnuva()" data-testid="btn-export-innuva"><mat-icon>download</mat-icon> A3 Innuva</button>
              <button mat-stroked-button (click)="onExportErp()" data-testid="btn-export-erp"><mat-icon>download</mat-icon> A3 ERP</button>
            }
          </div>
        }
      </div>

      @if (closure()) {
        <div style="display: flex; align-items: center; gap: 16px; margin-bottom: 16px;">
          <strong>Estado:</strong>
          <sig-state-badge [estado]="closure()!.estado" [paso]="closure()!.pasoActual" />
          <span style="color: var(--mat-sys-on-surface-variant);">Paso {{ pasoNumero(closure()!.pasoActual) }} de 3</span>
        </div>
      }

      @if (loading()) { <mat-card><mat-card-content><sig-skeleton [count]="6" /></mat-card-content></mat-card> }
      @else if (closure()) {
        <!-- KPIs -->
        <div class="sig-kpi-row">
          <mat-card class="sig-kpi-card" data-testid="kpi-coste-total">
            <mat-card-content>
              <div class="sig-kpi-label">Coste total</div>
              <div class="sig-kpi-value mono-num">{{ closure()!.costeTotal | number:'1.0-2' }} €</div>
            </mat-card-content>
          </mat-card>
          <mat-card class="sig-kpi-card" data-testid="kpi-facturacion">
            <mat-card-content>
              <div class="sig-kpi-label">Facturación</div>
              <div class="sig-kpi-value mono-num">{{ closure()!.facturacionTotal | number:'1.0-2' }} €</div>
            </mat-card-content>
          </mat-card>
          <mat-card class="sig-kpi-card" data-testid="kpi-margen">
            <mat-card-content>
              <div class="sig-kpi-label">Margen</div>
              <div class="sig-kpi-value mono-num">{{ closure()!.margen | number:'1.0-2' }} €</div>
              <div class="sig-kpi-trend" [class.sig-kpi-trend--up]="closure()!.margen >= 0" [class.sig-kpi-trend--down]="closure()!.margen < 0">
                {{ closure()!.facturacionTotal > 0 ? ((closure()!.margen / closure()!.facturacionTotal) * 100 | number:'1.0-1') : '0' }}%
              </div>
            </mat-card-content>
          </mat-card>
        </div>

        <!-- Alertas -->
        @if (closure()!.alertas && closure()!.alertas.length > 0) {
          <mat-card style="margin-bottom: 16px; border-left: 4px solid var(--sig-warning);">
            <mat-card-header>
              <mat-card-title style="display: flex; align-items: center; gap: 8px;">
                <mat-icon style="color: var(--sig-warning);">warning</mat-icon>
                Alertas ({{ closure()!.alertas.length }})
              </mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <div style="display: flex; flex-direction: column; gap: 12px;">
                @for (alerta of closure()!.alertas; track alerta.id) {
                  <div style="padding: 12px; border: 1px solid var(--mat-sys-outline-variant); border-radius: 4px; background: var(--mat-sys-surface-dim);">
                    <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 8px;">
                      @if (alerta.tipo === 'Bloqueante') {
                        <mat-chip [style.backgroundColor]="'#f44336'" [style.color]="'white'">BLOQUEANTE</mat-chip>
                      } @else {
                        <mat-chip [style.backgroundColor]="'#ff9800'" [style.color]="'white'">ADVERTENCIA</mat-chip>
                      }
                      <span style="flex: 1; font-weight: 500;">{{ alerta.descripcion }}</span>
                      @if (alerta.confirmada) {
                        <mat-icon style="color: var(--sig-success); font-size: 20px; height: 20px; width: 20px;" title="Confirmada">check_circle</mat-icon>
                      }
                    </div>
                    <div style="font-size: 12px; color: var(--mat-sys-on-surface-variant); margin-bottom: 8px;">
                      <strong>Código:</strong> {{ alerta.codigo }}
                    </div>
                    @if (alerta.detalle) {
                      <details style="font-size: 12px; color: var(--mat-sys-on-surface-variant); margin-bottom: 8px;">
                        <summary style="cursor: pointer; user-select: none;">Detalles técnicos</summary>
                        <pre style="margin-top: 4px; white-space: pre-wrap; word-break: break-word;">{{ alerta.detalle }}</pre>
                      </details>
                    }
                    @if (alerta.tipo === 'Advertencia' && !alerta.confirmada) {
                      <button mat-stroked-button (click)="onConfirmarAlerta(alerta.id)" style="font-size: 12px;">Confirmar advertencia</button>
                    }
                  </div>
                }
              </div>
            </mat-card-content>
          </mat-card>
        }

        <!-- Líneas -->
        <mat-card style="margin-bottom: 16px;">
          <mat-card-header style="display:flex;align-items:center;justify-content:space-between;">
            <mat-card-title>Líneas de cierre</mat-card-title>
            @if (canEditLines()) {
              <button mat-stroked-button (click)="onAnadirIncentivo()" data-testid="btn-anadir-incentivo"><mat-icon>add_card</mat-icon> Añadir incentivo</button>
            }
          </mat-card-header>
          <mat-card-content>
            @if (closure()!.lines.length === 0) {
              <p style="color: var(--mat-sys-on-surface-variant);">Sin líneas calculadas.</p>
            } @else {
              <table mat-table [dataSource]="closure()!.lines" class="sig-table" data-testid="tabla-closure-lines">
                <ng-container matColumnDef="concepto"><th mat-header-cell *matHeaderCellDef>Concepto</th><td mat-cell *matCellDef="let l">{{ l.conceptNombre }}</td></ng-container>
                <ng-container matColumnDef="tipo"><th mat-header-cell *matHeaderCellDef>Tipo</th><td mat-cell *matCellDef="let l"><mat-chip>{{ l.tipo }}</mat-chip></td></ng-container>
                <ng-container matColumnDef="usuario"><th mat-header-cell *matHeaderCellDef>Usuario</th><td mat-cell *matCellDef="let l">{{ l.userNombre ?? '—' }}</td></ng-container>
                <ng-container matColumnDef="importe"><th mat-header-cell *matHeaderCellDef>Importe</th>
                  <td mat-cell *matCellDef="let l" class="mono-num">
                    {{ l.importe | number:'1.0-2' }} €
                    @if (l.esManual) {
                      <mat-icon style="font-size:16px;height:16px;width:16px;vertical-align:middle;color:var(--sig-warning);" [title]="l.motivoManual ?? 'Línea manual'">edit_note</mat-icon>
                    }
                  </td>
                </ng-container>
                <ng-container matColumnDef="incidencia"><th mat-header-cell *matHeaderCellDef></th>
                  <td mat-cell *matCellDef="let l">
                    @if (l.tieneIncidencia) {
                      <mat-icon style="color: var(--sig-warning);" title="Tiene incidencias">warning</mat-icon>
                    }
                  </td>
                </ng-container>
                <ng-container matColumnDef="acciones"><th mat-header-cell *matHeaderCellDef></th>
                  <td mat-cell *matCellDef="let l">
                    @if (canEditLines()) {
                      <button mat-icon-button (click)="onOverrideLinea(l)" [attr.data-testid]="'btn-override-' + l.id" aria-label="Ajustar importe"><mat-icon>edit</mat-icon></button>
                    }
                    <a mat-icon-button [routerLink]="['/calculations', l.id]" [attr.data-testid]="'btn-detalle-' + l.id" aria-label="Ver detalle"><mat-icon>visibility</mat-icon></a>
                  </td>
                </ng-container>
                <tr mat-header-row *matHeaderRowDef="lineCols"></tr>
                <tr mat-row *matRowDef="let row; columns: lineCols" data-testid="row-line"></tr>
              </table>
            }
          </mat-card-content>
        </mat-card>

        <!-- Historial -->
        <mat-card style="margin-bottom: 16px;">
          <mat-card-header><mat-card-title>Historial de aprobación</mat-card-title></mat-card-header>
          <mat-card-content>
            @if (historial().length === 0) {
              <p style="color: var(--mat-sys-on-surface-variant);">Sin movimientos.</p>
            } @else {
              <table mat-table [dataSource]="historial()" class="sig-table" data-testid="tabla-historial">
                <ng-container matColumnDef="fecha"><th mat-header-cell *matHeaderCellDef>Fecha</th><td mat-cell *matCellDef="let h" class="mono-num">{{ h.timestamp | date:'dd/MM/yyyy HH:mm' }}</td></ng-container>
                <ng-container matColumnDef="usuario"><th mat-header-cell *matHeaderCellDef>Usuario</th><td mat-cell *matCellDef="let h">{{ h.userNombre }}</td></ng-container>
                <ng-container matColumnDef="accion"><th mat-header-cell *matHeaderCellDef>Acción</th><td mat-cell *matCellDef="let h">{{ h.accion }} ({{ h.pasoOrigen }} → {{ h.pasoDestino }})</td></ng-container>
                <ng-container matColumnDef="motivo"><th mat-header-cell *matHeaderCellDef>Motivo</th><td mat-cell *matCellDef="let h">{{ h.motivo ?? '—' }}</td></ng-container>
                <tr mat-header-row *matHeaderRowDef="['fecha', 'usuario', 'accion', 'motivo']"></tr>
                <tr mat-row *matRowDef="let row; columns: ['fecha', 'usuario', 'accion', 'motivo']"></tr>
              </table>
            }
          </mat-card-content>
        </mat-card>

        @if (closure()!.comentarios) {
          <mat-card>
            <mat-card-header><mat-card-title>Comentarios</mat-card-title></mat-card-header>
            <mat-card-content><p>{{ closure()!.comentarios }}</p></mat-card-content>
          </mat-card>
        }
      }
    </div>
  `,
  styles: [`
    .sig-kpi-row { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 16px; margin-bottom: 24px; }
  `],
})
export class ClosureDetailComponent implements OnInit {
  private readonly closureSvc = inject(ClosureService);
  private readonly exportSvc = inject(ExportService);
  private readonly auth = inject(AuthService);
  private readonly notify = inject(NotifyService);
  private readonly route = inject(ActivatedRoute);
  private readonly dialog = inject(MatDialog);

  protected readonly closure = signal<ClosureDetailDto | null>(null);
  protected readonly historial = signal<ApprovalHistoryDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly lineCols = ['concepto', 'tipo', 'usuario', 'importe', 'incidencia', 'acciones'];

  protected readonly alertasPendientes = computed(() => {
    const c = this.closure();
    return c?.alertas.filter((a: ClosureAlertaDto) => !a.confirmada) ?? [];
  });

  protected readonly canRecalculate = computed(() => {
    const c = this.closure();
    return !!c && (c.estado === 'Borrador' || c.estado === 'EnAprobacion' || c.estado === 'Rechazado') && this.auth.hasAnyRole('Administrator', 'Backoffice', 'ProjectManager', 'Facilitador', 'Interlocutor', 'Gestor');
  });

  // Ola 2 (#3a): override de importe e incentivos solo en estados editables (Borrador/Rechazado).
  protected readonly canEditLines = computed(() => {
    const c = this.closure();
    return !!c && (c.estado === 'Borrador' || c.estado === 'Rechazado') && this.auth.hasAnyRole('Administrator', 'Backoffice', 'ProjectManager', 'Facilitador', 'Interlocutor', 'Gestor');
  });

  protected readonly canApprove = computed(() => {
    const c = this.closure();
    if (!c) return false;
    if (c.estado === 'Aprobado' || c.estado === 'Exportado') return false;
    if (this.auth.hasRole('Administrator')) return true;
    // Ola 3a (#1): flujo Grupo → FICO. El paso Grupo lo aprueba un miembro del grupo
    // (rol global Facilitador/Interlocutor/Gestor); el refuerzo de la asignación al
    // servicio lo aplica el backend. El paso Fico lo aprueba el rol Fico.
    if (c.pasoActual === 'Grupo') {
      return this.auth.hasAnyRole('Facilitador', 'Interlocutor', 'Gestor');
    }
    if (c.pasoActual === 'Fico') {
      return this.auth.hasRole('Fico');
    }
    return false;
  });

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.load(id);
    this.closureSvc.historial(id).subscribe({
      next: (h) => this.historial.set(h),
      error: () => this.historial.set([]),
    });
  }

  protected pasoNumero(p: ApprovalStep): number {
    return { Grupo: 1, Fico: 2, SystemExports: 3 }[p];
  }

  private load(id: number): void {
    this.loading.set(true);
    this.closureSvc.getById(id).subscribe({
      next: (c) => { this.closure.set(c); this.loading.set(false); },
      error: () => { this.loading.set(false); this.notify.error('No se pudo cargar el cierre'); },
    });
  }

  protected onRecalcular(): void {
    const c = this.closure(); if (!c) return;
    this.closureSvc.recalcular(c.id, c.rowVersion, { comentarios: null }).subscribe({
      next: (updated) => { this.closure.set(updated); this.notify.success('Cierre recalculado'); },
      error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo recalcular'),
    });
  }

  protected onOverrideLinea(line: ClosureLineDto): void {
    const c = this.closure(); if (!c) return;
    this.dialog.open(OverrideExceptionDialog, {
      data: { line, resultadoOriginal: line.importeOriginal ?? line.importe, razon: line.motivoManual ?? '' },
    }).afterClosed().subscribe((res) => {
      if (!res) return;
      this.closureSvc.overrideLinea(c.id, line.id, c.rowVersion, { importe: res.nuevoImporte, motivo: res.razon }).subscribe({
        next: (updated) => { this.closure.set(updated); this.notify.success('Importe ajustado'); },
        error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo ajustar la línea'),
      });
    });
  }

  protected onAnadirIncentivo(): void {
    const c = this.closure(); if (!c) return;
    this.dialog.open(IncentivoDialog, {}).afterClosed().subscribe((req: ClosureLineIncentivoRequest | undefined) => {
      if (!req) return;
      this.closureSvc.agregarIncentivo(c.id, c.rowVersion, req).subscribe({
        next: (updated) => { this.closure.set(updated); this.notify.success('Incentivo añadido'); },
        error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo añadir el incentivo'),
      });
    });
  }

  protected onAprobar(): void {
    const c = this.closure(); if (!c) return;
    this.closureSvc.aprobar(c.id, c.rowVersion, { comentarios: null }).subscribe({
      next: (updated) => {
        this.closure.set(updated);
        this.notify.success('Cierre aprobado');
        this.closureSvc.historial(c.id).subscribe({ next: (h) => this.historial.set(h) });
      },
      error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo aprobar'),
    });
  }

  protected onRechazar(): void {
    const c = this.closure(); if (!c) return;
    this.dialog.open(RejectDialogComponent, { minWidth: 480 }).afterClosed().subscribe((motivo?: string | null) => {
      if (!motivo) return;
      this.closureSvc.rechazar(c.id, c.rowVersion, { motivo }).subscribe({
        next: (updated) => {
          this.closure.set(updated);
          this.notify.warning('Cierre rechazado');
          this.closureSvc.historial(c.id).subscribe({ next: (h) => this.historial.set(h) });
        },
        error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo rechazar'),
      });
    });
  }

  protected onExportInnuva(): void {
    const c = this.closure(); if (!c) return;
    this.exportSvc.exportA3Innuva(c.id).subscribe({
      next: (resp) => { this.exportSvc.saveAttachment(resp, `A3Innuva_${c.id}.xls`); this.notify.success('Export A3 Innuva descargado'); },
      error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo exportar'),
    });
  }

  protected onExportErp(): void {
    const c = this.closure(); if (!c) return;
    this.exportSvc.exportA3Erp(c.id).subscribe({
      next: (resp) => { this.exportSvc.saveAttachment(resp, `A3ERP_${c.id}.xlsx`); this.notify.success('Export A3 ERP descargado'); },
      error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo exportar'),
    });
  }

  protected onConfirmarAlerta(alertaId: number): void {
    const c = this.closure(); if (!c) return;
    this.closureSvc.confirmarAlerta(c.id, alertaId).subscribe({
      next: (updated: ClosureDetailDto) => {
        this.closure.set(updated);
        this.notify.success('Advertencia confirmada');
      },
      error: (err: unknown) => this.notify.error((err as any)?.error?.title ?? 'No se pudo confirmar la advertencia'),
    });
  }
}
