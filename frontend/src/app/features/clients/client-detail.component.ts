import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ClientService } from '../../core/api/clients.service';
import { ClientDetailDto } from '../../models/dtos';
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
  `],
})
export class ClientDetailComponent implements OnInit {
  private readonly clientSvc = inject(ClientService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotifyService);

  protected readonly client = signal<ClientDetailDto | null>(null);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.clientSvc.getById(id).subscribe({
      next: (c) => { this.client.set(c); this.loading.set(false); },
      error: () => { this.loading.set(false); this.notify.error('No se pudo cargar el cliente'); },
    });
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
