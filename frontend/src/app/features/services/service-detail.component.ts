import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatTabsModule } from '@angular/material/tabs';
import { ServiceService } from '../../core/api/services.service';
import { ServiceDetailDto } from '../../models/dtos';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { NotifyService } from '../../core/notify.service';
import { TarifasListComponent } from './tarifas/tarifas-list.component';
import { PresupuestosListComponent } from './presupuestos/presupuestos-list.component';

@Component({
  selector: 'app-service-detail',
  standalone: true,
  imports: [
    CommonModule, DatePipe, RouterLink,
    MatCardModule, MatButtonModule, MatIconModule, MatChipsModule, MatDialogModule, MatTabsModule,
    BreadcrumbsComponent, SkeletonComponent,
    TarifasListComponent, PresupuestosListComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Servicios', route: '/services' }, { label: service()?.nombre ?? 'Detalle' }]" />
      <div class="sig-page__header">
        <h1 class="sig-page__title">{{ service()?.nombre ?? 'Cargando...' }}</h1>
        @if (service()) {
          <div style="display: flex; gap: 8px;">
            <a mat-flat-button color="primary" [routerLink]="['/services', service()!.id, 'editar']" data-testid="service-editar"><mat-icon>edit</mat-icon> Editar</a>
            <button mat-stroked-button color="warn" (click)="onDelete()" data-testid="service-eliminar"><mat-icon>delete</mat-icon> Eliminar</button>
          </div>
        }
      </div>
      @if (loading()) { <mat-card><mat-card-content><sig-skeleton [count]="6" /></mat-card-content></mat-card> }
      @else if (service()) {
        <mat-card>
          <mat-card-header><mat-card-title>Datos del servicio</mat-card-title></mat-card-header>
          <mat-card-content>
            <dl class="sig-dl">
              <dt>Cliente</dt><dd>{{ service()!.clientNombre }}</dd>
              <dt>Estado</dt><dd><mat-chip>{{ service()!.estado }}</mat-chip></dd>
              <dt>Departamento</dt><dd>{{ service()!.departmentId ?? '—' }}</dd>
              <dt>Fecha alta</dt><dd class="mono-num">{{ service()!.fechaAlta | date:'dd/MM/yyyy' }}</dd>
              <dt>Interlocutor</dt><dd>{{ service()!.interlocutorNombre ?? '—' }}</dd>
              <dt>Email</dt><dd>{{ service()!.interlocutorEmail ?? '—' }}</dd>
              <dt>Teléfono</dt><dd>{{ service()!.interlocutorTelefono ?? '—' }}</dd>
              <dt>Centros de coste</dt><dd>{{ service()!.costCenterIds.length }} asignados</dd>
              <dt>Conceptos</dt><dd>{{ service()!.conceptIds.length }} asociados</dd>
              <dt>Usuarios</dt><dd>{{ service()!.userIds.length }} asignados</dd>
            </dl>
          </mat-card-content>
        </mat-card>

        <mat-tab-group>
          <mat-tab>
            <ng-template mat-tab-label>
              <mat-icon>local_offer</mat-icon>
              <span style="margin-left: 8px;">Tarifas</span>
            </ng-template>
            <div style="padding: 16px;">
              <app-tarifas-list [serviceId]="service()!.id"></app-tarifas-list>
            </div>
          </mat-tab>

          <mat-tab>
            <ng-template mat-tab-label>
              <mat-icon>attach_money</mat-icon>
              <span style="margin-left: 8px;">Presupuestos</span>
            </ng-template>
            <div style="padding: 16px;">
              <app-presupuestos-list [serviceId]="service()!.id"></app-presupuestos-list>
            </div>
          </mat-tab>
        </mat-tab-group>
      }
    </div>
  `,
  styles: [`
    .sig-dl { display: grid; grid-template-columns: 200px 1fr; gap: 8px 16px; margin: 0; }
    .sig-dl dt { color: var(--mat-sys-on-surface-variant); font-weight: 500; }
    .sig-dl dd { margin: 0; }
  `],
})
export class ServiceDetailComponent implements OnInit {
  private readonly serviceSvc = inject(ServiceService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotifyService);

  protected readonly service = signal<ServiceDetailDto | null>(null);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.serviceSvc.getById(id).subscribe({
      next: (s) => { this.service.set(s); this.loading.set(false); },
      error: () => { this.loading.set(false); this.notify.error('No se pudo cargar el servicio'); },
    });
  }

  protected onDelete(): void {
    const s = this.service();
    if (!s) return;
    this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Eliminar Servicio', message: 'Acción irreversible.', entityName: s.nombre, destructive: true }, minWidth: 480,
    }).afterClosed().subscribe((ok) => {
      if (!ok) return;
      this.serviceSvc.delete(s.id).subscribe({
        next: () => { this.notify.success('Servicio eliminado'); void this.router.navigate(['/services']); },
        error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo eliminar'),
      });
    });
  }
}
