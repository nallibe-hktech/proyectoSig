import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ConceptService } from '../../core/api/concepts.service';
import { ServiceService } from '../../core/api/services.service';
import { ConceptDetailDto, ServiceListItemDto } from '../../models/dtos';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { NotifyService } from '../../core/notify.service';

@Component({
  selector: 'app-concept-detail',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink, MatCardModule, MatButtonModule, MatIconModule, MatChipsModule, MatDialogModule, BreadcrumbsComponent, SkeletonComponent],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Concepts', route: '/concepts' }, { label: concept()?.nombre ?? 'Detalle' }]" />
      <div class="sig-page__header">
        <h1 class="sig-page__title">{{ concept()?.nombre ?? 'Cargando...' }}</h1>
        @if (concept()) {
          <div style="display: flex; gap: 8px;">
            <a mat-flat-button color="primary" [routerLink]="['/concepts', concept()!.id, 'formula']" data-testid="btn-formula"><mat-icon>functions</mat-icon> Editor de Fórmula</a>
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
        <mat-card>
          <mat-card-header><mat-card-title>Fórmula (JSON)</mat-card-title></mat-card-header>
          <mat-card-content>
            <pre class="sig-json mono-num" data-testid="formula-json">{{ formattedJson() }}</pre>
          </mat-card-content>
        </mat-card>
      }
    </div>
  `,
  styles: [`
    .sig-dl { display: grid; grid-template-columns: 200px 1fr; gap: 8px 16px; margin: 0; }
    .sig-dl dt { color: var(--mat-sys-on-surface-variant); font-weight: 500; }
    .sig-dl dd { margin: 0; }
    .sig-json { background: var(--mat-sys-surface-variant); padding: 16px; border-radius: 8px; font-size: 12px; overflow: auto; max-height: 400px; white-space: pre-wrap; }
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

  protected formattedJson(): string {
    const c = this.concept(); if (!c) return '';
    try { return JSON.stringify(JSON.parse(c.formulaJson), null, 2); } catch { return c.formulaJson; }
  }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.conceptSvc.getById(id).subscribe({
      next: (c) => {
        this.concept.set(c);
        // Load service details for associated services
        if (c.serviceIds && c.serviceIds.length > 0) {
          this.serviceSvc.list(1, 1000).subscribe({
            next: (result) => {
              const associatedSvcs = result.items.filter(s => c.serviceIds.includes(s.id));
              this.relatedServices.set(associatedSvcs);
              this.loading.set(false);
            },
            error: () => {
              this.loading.set(false);
            },
          });
        } else {
          this.loading.set(false);
        }
      },
      error: () => { this.loading.set(false); this.notify.error('No se pudo cargar el concept'); },
    });
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
