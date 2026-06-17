import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ClientService } from '../../core/api/clients.service';
import { ServiceService } from '../../core/api/services.service';
import { ClientDetailDto, ServiceListItemDto } from '../../models/dtos';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { NotifyService } from '../../core/notify.service';

@Component({
  selector: 'app-client-detail',
  standalone: true,
  imports: [
    CommonModule, RouterLink,
    MatCardModule, MatButtonModule, MatIconModule, MatDividerModule, MatDialogModule,
    BreadcrumbsComponent, SkeletonComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Clients', route: '/clients' }, { label: client()?.nombre ?? 'Detalle' }]" />

      <div class="sig-page__header">
        <h1 class="sig-page__title">{{ client()?.nombre ?? 'Cargando...' }}</h1>
        @if (client()) {
          <div style="display: flex; gap: 8px;">
            <a mat-flat-button color="primary" [routerLink]="['/clients', client()!.id, 'editar']" data-testid="btn-editar">
              <mat-icon>edit</mat-icon> Editar
            </a>
            <button mat-stroked-button color="warn" (click)="onDelete()" data-testid="btn-eliminar">
              <mat-icon>delete</mat-icon> Eliminar
            </button>
          </div>
        }
      </div>

      @if (loading()) {
        <mat-card><mat-card-content><sig-skeleton [count]="6" /></mat-card-content></mat-card>
      } @else if (client()) {
        <mat-card>
          <mat-card-header><mat-card-title>Datos principales</mat-card-title></mat-card-header>
          <mat-card-content>
            <dl class="sig-dl">
              <dt>Nombre</dt><dd data-testid="field-nombre">{{ client()!.nombre }}</dd>
              <dt>NIF</dt><dd class="mono-num" data-testid="field-nif">{{ client()!.nif }}</dd>
              <dt>Estado</dt><dd data-testid="field-estado"><span class="sig-badge" [class]="estadoBadge(client()!.estado)">{{ client()!.estado }}</span></dd>
              <dt>Dirección</dt><dd>{{ client()!.direccion ?? '—' }}</dd>
              <dt>Ciudad</dt><dd>{{ client()!.ciudad ?? '—' }}</dd>
              <dt>Provincia</dt><dd>{{ client()!.provincia ?? '—' }}</dd>
              <dt>Código Postal</dt><dd>{{ client()!.codigoPostal ?? '—' }}</dd>
              <dt>País</dt><dd>{{ client()!.pais ?? '—' }}</dd>
            </dl>
            <mat-divider />
            <h3 class="sig-form-section">Contacto</h3>
            <dl class="sig-dl">
              <dt>Nombre</dt><dd>{{ client()!.contactoNombre ?? '—' }}</dd>
              <dt>Email</dt><dd>{{ client()!.contactoEmail ?? '—' }}</dd>
              <dt>Teléfono</dt><dd>{{ client()!.contactoTelefono ?? '—' }}</dd>
            </dl>
          </mat-card-content>
        </mat-card>

        <mat-card style="margin-top: 16px;" data-testid="card-servicios">
          <mat-card-header><mat-card-title>Servicios del cliente</mat-card-title></mat-card-header>
          <mat-card-content>
            @if (servicesLoading()) {
              <sig-skeleton [count]="3" />
            } @else if (services().length === 0) {
              <p class="sig-empty-text">Este cliente no tiene servicios todavía.</p>
            } @else {
              <ul class="sig-services">
                @for (s of services(); track s.id) {
                  <li class="sig-services__item" data-testid="row-servicio">
                    <div class="sig-services__info">
                      <span class="sig-services__name">{{ s.nombre }}</span>
                      <span class="sig-badge" [class]="estadoBadge(s.estado)">{{ s.estado }}</span>
                    </div>
                    <a mat-stroked-button [routerLink]="['/services', s.id, 'editar']" [attr.data-testid]="'btn-editar-servicio-' + s.id">
                      <mat-icon>edit</mat-icon> Editar
                    </a>
                  </li>
                }
              </ul>
            }
          </mat-card-content>
        </mat-card>
      }
    </div>
  `,
  styles: [`
    .sig-dl {
      display: grid; grid-template-columns: 200px 1fr; gap: 8px 16px; margin: 0;
    }
    .sig-dl dt { color: var(--mat-sys-on-surface-variant); font-weight: 500; }
    .sig-dl dd { margin: 0; color: var(--mat-sys-on-surface); }
    .sig-form-section {
      font-size: 14px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.08em;
      color: var(--mat-sys-on-surface-variant); margin: 16px 0 8px;
    }
    .sig-badge { display: inline-flex; align-items: center; gap: 5px; padding: 2px 10px; border-radius: 20px; font-size: 11px; font-weight: 600; }
    .sig-badge::before { content: ''; width: 6px; height: 6px; border-radius: 50%; background: currentColor; }
    .sig-badge--green { color: #22c55e; background: rgba(34,197,94,.12); }
    .sig-badge--red { color: #ef4444; background: rgba(239,68,68,.12); }
    .sig-empty-text { color: var(--mat-sys-on-surface-variant); margin: 0; }
    .sig-services { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 8px; }
    .sig-services__item { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 10px 12px; border: 1px solid var(--mat-sys-outline-variant); border-radius: 8px; }
    .sig-services__info { display: flex; align-items: center; gap: 12px; }
    .sig-services__name { font-weight: 500; }
  `],
})
export class ClientDetailComponent implements OnInit {
  private readonly clientSvc = inject(ClientService);
  private readonly serviceSvc = inject(ServiceService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotifyService);

  protected readonly client = signal<ClientDetailDto | null>(null);
  protected readonly loading = signal(true);
  protected readonly services = signal<ServiceListItemDto[]>([]);
  protected readonly servicesLoading = signal(true);

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.clientSvc.getById(id).subscribe({
      next: (c) => { this.client.set(c); this.loading.set(false); },
      error: () => { this.loading.set(false); this.notify.error('No se pudo cargar el cliente'); },
    });
    this.serviceSvc.list(1, 100, id).subscribe({
      next: (r) => { this.services.set(r.items); this.servicesLoading.set(false); },
      error: () => { this.services.set([]); this.servicesLoading.set(false); },
    });
  }

  protected estadoBadge(estado?: string): string {
    return estado === 'Inactivo' ? 'sig-badge--red' : 'sig-badge--green';
  }

  protected onDelete(): void {
    const c = this.client();
    if (!c) return;
    this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Eliminar Client',
        message: 'Esta acción no se puede deshacer.',
        entityName: c.nombre,
        destructive: true,
      },
      minWidth: 480,
    }).afterClosed().subscribe((ok) => {
      if (!ok) return;
      this.clientSvc.delete(c.id).subscribe({
        next: () => { this.notify.success('Cliente eliminado'); void this.router.navigate(['/clients']); },
        error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo eliminar'),
      });
    });
  }
}
