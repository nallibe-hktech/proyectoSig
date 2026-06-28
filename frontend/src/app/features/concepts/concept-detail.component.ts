import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ConceptService } from '../../core/api/concepts.service';
import { ServiceService } from '../../core/api/services.service';
import { ConceptDetailDto, ServiceListItemDto, AuditLogDto } from '../../models/dtos';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { NotifyService } from '../../core/notify.service';

@Component({
  selector: 'app-concept-detail',
  standalone: true,
  imports: [
    CommonModule, DatePipe, RouterLink, MatCardModule, MatButtonModule, MatIconModule,
    MatChipsModule, MatTabsModule, MatTableModule, MatPaginatorModule, MatDialogModule,
    BreadcrumbsComponent, SkeletonComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Concepts', route: '/concepts' }, { label: concept()?.nombre ?? 'Detalle' }]" />
      <div class="sig-page__header">
        <h1 class="sig-page__title">{{ concept()?.nombre ?? 'Cargando...' }}</h1>
        @if (concept()) {
          <div style="display: flex; gap: 8px; flex-wrap: wrap;">
            <a mat-flat-button color="primary" [routerLink]="['/concepts', concept()!.id, 'formula']" data-testid="btn-formula"><mat-icon>functions</mat-icon> Editor de Fórmula</a>
            <a mat-stroked-button [routerLink]="['/concepts', concept()!.id, 'tarifas']" data-testid="btn-tarifas"><mat-icon>local_offer</mat-icon> Tarifas</a>
            <a mat-stroked-button [routerLink]="['/concepts', concept()!.id, 'presupuestos']" data-testid="btn-presupuestos"><mat-icon>budget</mat-icon> Presupuestos</a>
            <a mat-stroked-button [routerLink]="['/concepts', concept()!.id, 'editar']" data-testid="btn-editar"><mat-icon>edit</mat-icon> Editar</a>
            <button mat-stroked-button color="warn" (click)="onDelete()" data-testid="btn-eliminar"><mat-icon>delete</mat-icon> Eliminar</button>
          </div>
        }
      </div>
      @if (loading()) { <mat-card><mat-card-content><sig-skeleton [count]="4" /></mat-card-content></mat-card> }
      @else if (concept()) {
        <mat-card style="margin-bottom: 16px;">
          <mat-card-content>
            <dl class="sig-dl">
              <dt>Tipo</dt><dd><mat-chip>{{ concept()!.tipo }}</mat-chip></dd>
              <dt>Vigente desde</dt><dd class="mono-num">{{ concept()!.fechaDesde | date:'dd/MM/yyyy' }}</dd>
              <dt>Vigente hasta</dt><dd class="mono-num">{{ concept()!.fechaHasta ? (concept()!.fechaHasta | date:'dd/MM/yyyy') : 'Indefinido' }}</dd>
              <dt>Servicios asociados</dt>
              <dd>
                @if (relatedServices().length > 0) {
                  <div style="display: flex; flex-wrap: wrap; gap: 8px;">
                    @for (svc of relatedServices(); track svc.id) {
                      <mat-chip>{{ svc.nombre }}</mat-chip>
                    }
                  </div>
                } @else {
                  <span style="color: var(--mat-sys-on-surface-variant);">Ninguno</span>
                }
              </dd>
              <dt>Usuarios</dt><dd>{{ concept()!.userIds.length }}</dd>
            </dl>
          </mat-card-content>
        </mat-card>

        <mat-tab-group>
          <mat-tab label="Fórmula (JSON)">
            <mat-card style="margin-top: 16px;">
              <mat-card-content>
                <pre class="sig-json mono-num" data-testid="formula-json">{{ formattedJson() }}</pre>
              </mat-card-content>
            </mat-card>
          </mat-tab>
          <mat-tab label="Historial de cambios">
            <mat-card style="margin-top: 16px;">
              <mat-card-content>
                @if (historialLoading()) {
                  <sig-skeleton [count]="3" />
                } @else if (historial().length === 0) {
                  <p style="color: var(--mat-sys-on-surface-variant); padding: 16px 0;">No hay cambios registrados para este concepto.</p>
                } @else {
                  <table mat-table [dataSource]="historial()" class="sig-table">
                    <ng-container matColumnDef="fecha">
                      <th mat-header-cell *matHeaderCellDef>Fecha</th>
                      <td mat-cell *matCellDef="let log">{{ log.timestamp | date:'dd/MM/yyyy HH:mm' }}</td>
                    </ng-container>
                    <ng-container matColumnDef="usuario">
                      <th mat-header-cell *matHeaderCellDef>Usuario</th>
                      <td mat-cell *matCellDef="let log">{{ log.userNombre ?? 'Sistema' }}</td>
                    </ng-container>
                    <ng-container matColumnDef="accion">
                      <th mat-header-cell *matHeaderCellDef>Acción</th>
                      <td mat-cell *matCellDef="let log">
                        <mat-chip [class]="'chip-' + log.action.toLowerCase()">{{ log.action }}</mat-chip>
                      </td>
                    </ng-container>
                    <ng-container matColumnDef="cambios">
                      <th mat-header-cell *matHeaderCellDef>Cambios</th>
                      <td mat-cell *matCellDef="let log">
                        @if (log.oldValueJson || log.newValueJson) {
                          <button mat-stroked-button (click)="showDiff(log)" data-testid="btn-diff">
                            <mat-icon>compare_arrows</mat-icon> Ver cambios
                          </button>
                        } @else {
                          <span style="color: var(--mat-sys-on-surface-variant);">—</span>
                        }
                      </td>
                    </ng-container>
                    <tr mat-header-row *matHeaderRowDef="historialColumns"></tr>
                    <tr mat-row *matRowDef="let row; columns: historialColumns;"></tr>
                  </table>
                  <mat-paginator
                    [length]="historialTotal()"
                    [pageSize]="historialPageSize()"
                    [pageIndex]="historialPage() - 1"
                    [pageSizeOptions]="[10, 20, 50]"
                    showFirstLastButtons
                    (page)="onHistorialPageChange($event)"
                  />
                }
              </mat-card-content>
            </mat-card>
          </mat-tab>
        </mat-tab-group>
      }
    </div>
  `,
  styles: [`
    .sig-dl { display: grid; grid-template-columns: 200px 1fr; gap: 8px 16px; margin: 0; }
    .sig-dl dt { color: var(--mat-sys-on-surface-variant); font-weight: 500; }
    .sig-dl dd { margin: 0; }
    .sig-json { background: var(--mat-sys-surface-variant); padding: 16px; border-radius: 8px; font-size: 12px; overflow: auto; max-height: 400px; white-space: pre-wrap; }
    .sig-table { width: 100%; }
    .chip-create { background: #e8f5e9 !important; color: #2e7d32 !important; }
    .chip-update { background: #fff3e0 !important; color: #e65100 !important; }
    .chip-delete { background: #ffebee !important; color: #c62828 !important; }
  `],
})
export class ConceptDetailComponent implements OnInit {
  private readonly conceptSvc = inject(ConceptService);
  private readonly serviceSvc = inject(ServiceService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotifyService);

  protected readonly concept = signal<ConceptDetailDto | null>(null);
  protected readonly loading = signal(true);
  protected readonly relatedServices = signal<ServiceListItemDto[]>([]);
  protected readonly historial = signal<AuditLogDto[]>([]);
  protected readonly historialLoading = signal(false);
  protected readonly historialPage = signal(1);
  protected readonly historialPageSize = signal(20);
  protected readonly historialTotal = signal(0);
  protected readonly historialColumns = ['fecha', 'usuario', 'accion', 'cambios'];

  protected formattedJson(): string {
    const c = this.concept(); if (!c) return '';
    try { return JSON.stringify(JSON.parse(c.formulaJson), null, 2); } catch { return c.formulaJson; }
  }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.conceptSvc.getById(id).subscribe({
      next: (c) => {
        this.concept.set(c);
        if (c.serviceId) {
          this.serviceSvc.list(1, 1000).subscribe({
            next: (result) => {
              const associatedSvcs = result.items.filter(s => s.id === c.serviceId);
              this.relatedServices.set(associatedSvcs);
              this.loading.set(false);
            },
            error: () => { this.loading.set(false); },
          });
        } else {
          this.loading.set(false);
        }
        this.loadHistorial();
      },
      error: () => { this.loading.set(false); this.notify.error('No se pudo cargar el concept'); },
    });
  }

  private loadHistorial(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.historialLoading.set(true);
    this.conceptSvc.getHistorial(id, this.historialPage(), this.historialPageSize()).subscribe({
      next: (result) => {
        this.historial.set(result.items);
        this.historialTotal.set(result.total);
        this.historialLoading.set(false);
      },
      error: () => { this.historialLoading.set(false); },
    });
  }

  protected onHistorialPageChange(event: PageEvent): void {
    this.historialPage.set(event.pageIndex + 1);
    this.historialPageSize.set(event.pageSize);
    this.loadHistorial();
  }

  protected showDiff(log: AuditLogDto): void {
    const oldVal = log.oldValueJson ? JSON.parse(log.oldValueJson) : null;
    const newVal = log.newValueJson ? JSON.parse(log.newValueJson) : null;
    const changes: string[] = [];
    if (oldVal && newVal) {
      for (const key of Object.keys(newVal)) {
        if (JSON.stringify(oldVal[key]) !== JSON.stringify(newVal[key])) {
          changes.push(`${key}: ${oldVal[key]} → ${newVal[key]}`);
        }
      }
    } else if (newVal) {
      changes.push(...Object.entries(newVal).map(([k, v]) => `${k}: ${v}`));
    }
    alert(changes.length > 0 ? changes.join('\n') : 'Sin cambios detectados');
  }

  protected onDelete(): void {
    const c = this.concept(); if (!c) return;
    this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Eliminar Concept', message: 'Acción irreversible.', entityName: c.nombre, destructive: true }, minWidth: 480,
    }).afterClosed().subscribe((ok) => {
      if (!ok) return;
      this.conceptSvc.delete(c.id).subscribe({
        next: () => { this.notify.success('Concept eliminado'); void this.router.navigate(['/concepts']); },
        error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo eliminar'),
      });
    });
  }
}
