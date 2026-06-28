import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ClientService } from '../../core/api/clients.service';
import { ServiceService } from '../../core/api/services.service';
import { ClientDetailDto, ServiceListItemDto, ClienteIncidenciaDto } from '../../models/dtos';
import { EstadoIncidencia } from '../../models/enums';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { NotifyService } from '../../core/notify.service';

@Component({
  selector: 'app-client-detail',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterLink,
    MatCardModule, MatButtonModule, MatIconModule, MatDividerModule, MatDialogModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    BreadcrumbsComponent, SkeletonComponent,
  ],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Clients', route: '/clients' }, { label: client()?.nombre ?? 'Detalle' }]" />

      <div class="sig-page__header">
        <h1 class="sig-page__title">{{ client()?.nombre ?? 'Cargando...' }}</h1>
        @if (client()) {
          <div style="display: flex; gap: 8px; flex-wrap: wrap;">
            <a mat-flat-button color="primary" [routerLink]="['/clients', client()!.id, 'plantillas']" data-testid="btn-customizacion">
              <mat-icon>tune</mat-icon> Customización
            </a>
            <a mat-stroked-button [routerLink]="['/clients', client()!.id, 'editar']" data-testid="btn-editar">
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

        <mat-card style="margin-top: 16px;" data-testid="card-incidencias">
          <mat-card-header>
            <mat-card-title>Incidencias</mat-card-title>
            <span style="flex: 1;"></span>
            @if (!showForm()) {
              <button mat-flat-button color="primary" (click)="openNew()" data-testid="btn-nueva-incidencia">
                <mat-icon>add</mat-icon> Nueva incidencia
              </button>
            }
          </mat-card-header>
          <mat-card-content>
            @if (showForm()) {
              <div class="inc-form" data-testid="form-incidencia">
                <mat-form-field appearance="outline">
                  <mat-label>Tipo</mat-label>
                  <input matInput [(ngModel)]="form.tipo" maxlength="100" data-testid="inc-tipo" />
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label>Estado</mat-label>
                  <mat-select [(ngModel)]="form.estado" data-testid="inc-estado">
                    @for (e of estados; track e) { <mat-option [value]="e">{{ e }}</mat-option> }
                  </mat-select>
                </mat-form-field>
                <mat-form-field appearance="outline" class="inc-form__full">
                  <mat-label>Explicación</mat-label>
                  <textarea matInput [(ngModel)]="form.descripcion" rows="2" maxlength="2000" data-testid="inc-descripcion"></textarea>
                </mat-form-field>
                <div class="inc-form__actions">
                  <button mat-stroked-button (click)="cancelForm()" data-testid="btn-cancelar-incidencia">Cancelar</button>
                  <button mat-flat-button color="primary" (click)="saveForm()"
                          [disabled]="!form.tipo.trim() || !form.descripcion.trim() || saving()" data-testid="btn-guardar-incidencia">
                    Guardar
                  </button>
                </div>
              </div>
              <mat-divider style="margin: 8px 0 16px;" />
            }

            @if (incidenciasLoading()) {
              <sig-skeleton [count]="2" />
            } @else if (incidencias().length === 0) {
              <p class="sig-empty-text">Este cliente no tiene incidencias registradas.</p>
            } @else {
              <ul class="sig-services">
                @for (i of incidencias(); track i.id) {
                  <li class="sig-services__item" data-testid="row-incidencia">
                    <div class="inc-row">
                      <div class="inc-row__head">
                        <span class="sig-services__name">{{ i.tipo }}</span>
                        <span class="sig-badge" [class]="estadoIncidenciaBadge(i.estado)">{{ i.estado }}</span>
                      </div>
                      <p class="inc-row__desc">{{ i.descripcion }}</p>
                      <span class="inc-row__date">Actualizada {{ i.updatedAt | date:'dd/MM/yyyy HH:mm' }}</span>
                    </div>
                    <div class="inc-row__actions">
                      <button mat-stroked-button (click)="openEdit(i)" [attr.data-testid]="'btn-editar-incidencia-' + i.id">
                        <mat-icon>edit</mat-icon>
                      </button>
                      <button mat-stroked-button color="warn" (click)="deleteIncidencia(i)" [attr.data-testid]="'btn-eliminar-incidencia-' + i.id">
                        <mat-icon>delete</mat-icon>
                      </button>
                    </div>
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
    .sig-badge--amber { color: #f59e0b; background: rgba(245,158,11,.12); }
    .sig-badge--blue { color: #3b82f6; background: rgba(59,130,246,.12); }
    .inc-form { display: grid; grid-template-columns: 1fr 1fr; gap: 0 16px; }
    .inc-form__full { grid-column: 1 / -1; }
    .inc-form__actions { grid-column: 1 / -1; display: flex; justify-content: flex-end; gap: 8px; }
    .inc-row { display: flex; flex-direction: column; gap: 4px; }
    .inc-row__head { display: flex; align-items: center; gap: 12px; }
    .inc-row__desc { margin: 0; color: var(--mat-sys-on-surface); white-space: pre-wrap; }
    .inc-row__date { font-size: 11px; color: var(--mat-sys-on-surface-variant); }
    .inc-row__actions { display: flex; gap: 8px; flex-shrink: 0; }
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

  // Incidencias del cliente (PPT slide 6)
  protected readonly incidencias = signal<ClienteIncidenciaDto[]>([]);
  protected readonly incidenciasLoading = signal(true);
  protected readonly showForm = signal(false);
  protected readonly saving = signal(false);
  protected editingId: number | null = null;
  protected readonly estados: EstadoIncidencia[] = ['Abierta', 'EnProceso', 'Resuelta'];
  protected form: { tipo: string; descripcion: string; estado: EstadoIncidencia } =
    { tipo: '', descripcion: '', estado: 'Abierta' };

  private clientId = 0;

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.clientId = id;
    this.clientSvc.getById(id).subscribe({
      next: (c) => { this.client.set(c); this.loading.set(false); },
      error: () => { this.loading.set(false); this.notify.error('No se pudo cargar el cliente'); },
    });
    this.serviceSvc.list(1, 100, id).subscribe({
      next: (r) => { this.services.set(r.items); this.servicesLoading.set(false); },
      error: () => { this.services.set([]); this.servicesLoading.set(false); },
    });
    this.loadIncidencias();
  }

  protected estadoBadge(estado?: string): string {
    return estado === 'Inactivo' ? 'sig-badge--red' : 'sig-badge--green';
  }

  protected estadoIncidenciaBadge(estado: EstadoIncidencia): string {
    switch (estado) {
      case 'Resuelta': return 'sig-badge--green';
      case 'EnProceso': return 'sig-badge--blue';
      default: return 'sig-badge--amber';
    }
  }

  private loadIncidencias(): void {
    this.incidenciasLoading.set(true);
    this.clientSvc.listIncidencias(this.clientId).subscribe({
      next: (rows) => { this.incidencias.set(rows); this.incidenciasLoading.set(false); },
      error: () => { this.incidencias.set([]); this.incidenciasLoading.set(false); },
    });
  }

  protected openNew(): void {
    this.editingId = null;
    this.form = { tipo: '', descripcion: '', estado: 'Abierta' };
    this.showForm.set(true);
  }

  protected openEdit(i: ClienteIncidenciaDto): void {
    this.editingId = i.id;
    this.form = { tipo: i.tipo, descripcion: i.descripcion, estado: i.estado };
    this.showForm.set(true);
  }

  protected cancelForm(): void {
    this.showForm.set(false);
    this.editingId = null;
  }

  protected saveForm(): void {
    const tipo = this.form.tipo.trim();
    const descripcion = this.form.descripcion.trim();
    if (!tipo || !descripcion) return;
    this.saving.set(true);
    const done = () => {
      this.saving.set(false);
      this.showForm.set(false);
      this.editingId = null;
      this.loadIncidencias();
    };
    const onError = (err: { error?: { title?: string } }) => {
      this.saving.set(false);
      this.notify.error(err?.error?.title ?? 'No se pudo guardar la incidencia');
    };
    if (this.editingId == null) {
      this.clientSvc.createIncidencia(this.clientId, { tipo, descripcion, estado: this.form.estado }).subscribe({
        next: () => { this.notify.success('Incidencia creada'); done(); }, error: onError,
      });
    } else {
      this.clientSvc.updateIncidencia(this.clientId, this.editingId, { tipo, descripcion, estado: this.form.estado }).subscribe({
        next: () => { this.notify.success('Incidencia actualizada'); done(); }, error: onError,
      });
    }
  }

  protected deleteIncidencia(i: ClienteIncidenciaDto): void {
    this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Eliminar incidencia',
        message: 'Esta acción no se puede deshacer.',
        entityName: i.tipo,
        destructive: true,
      },
      minWidth: 480,
    }).afterClosed().subscribe((ok) => {
      if (!ok) return;
      this.clientSvc.deleteIncidencia(this.clientId, i.id).subscribe({
        next: () => { this.notify.success('Incidencia eliminada'); this.loadIncidencias(); },
        error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo eliminar'),
      });
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
