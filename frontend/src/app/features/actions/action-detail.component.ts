import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ActionService } from '../../core/api/actions.service';
import { ActionDetailDto } from '../../models/dtos';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { NotifyService } from '../../core/notify.service';

@Component({
  selector: 'app-action-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, MatButtonModule, MatIconModule, MatChipsModule, MatDialogModule, BreadcrumbsComponent, SkeletonComponent],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Actions', route: '/actions' }, { label: action()?.nombre ?? 'Detalle' }]" />
      <div class="sig-page__header">
        <h1 class="sig-page__title">{{ action()?.nombre ?? 'Cargando...' }}</h1>
        @if (action()) {
          <div style="display: flex; gap: 8px;">
            <a mat-flat-button color="primary" [routerLink]="['/actions', action()!.id, 'editar']" data-testid="btn-editar"><mat-icon>edit</mat-icon> Editar</a>
            <button mat-stroked-button color="warn" (click)="onDelete()" data-testid="btn-eliminar"><mat-icon>delete</mat-icon> Eliminar</button>
          </div>
        }
      </div>
      @if (loading()) { <mat-card><mat-card-content><sig-skeleton [count]="6" /></mat-card-content></mat-card> }
      @else if (action()) {
        <mat-card><mat-card-content>
          <dl class="sig-dl">
            <dt>Estado</dt><dd><mat-chip>{{ action()!.estado }}</mat-chip></dd>
            <dt>Proyecto ID</dt><dd>{{ action()!.projectId }}</dd>
            <dt>Cliente ID</dt><dd>{{ action()!.clientId }}</dd>
            <dt>Departamento</dt><dd>{{ action()!.departmentId ?? '—' }}</dd>
            <dt>Conceptos asociados</dt><dd>{{ action()!.conceptIds.length }}</dd>
            <dt>Usuarios asignados</dt><dd>{{ action()!.userIds.length }}</dd>
          </dl>
        </mat-card-content></mat-card>
      }
    </div>
  `,
  styles: [`.sig-dl { display: grid; grid-template-columns: 200px 1fr; gap: 8px 16px; margin: 0; } .sig-dl dt { color: var(--mat-sys-on-surface-variant); font-weight: 500; } .sig-dl dd { margin: 0; }`],
})
export class ActionDetailComponent implements OnInit {
  private readonly actionSvc = inject(ActionService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly notify = inject(NotifyService);

  protected readonly action = signal<ActionDetailDto | null>(null);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.actionSvc.getById(id).subscribe({
      next: (a) => { this.action.set(a); this.loading.set(false); },
      error: () => { this.loading.set(false); this.notify.error('No se pudo cargar la action'); },
    });
  }

  protected onDelete(): void {
    const a = this.action(); if (!a) return;
    this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Eliminar Action', message: 'Acción irreversible.', entityName: a.nombre, destructive: true }, minWidth: 480,
    }).afterClosed().subscribe((ok) => {
      if (!ok) return;
      this.actionSvc.delete(a.id).subscribe({
        next: () => { this.notify.success('Action eliminada'); void this.router.navigate(['/actions']); },
        error: (err) => this.notify.error(err?.error?.title ?? 'No se pudo eliminar'),
      });
    });
  }
}
