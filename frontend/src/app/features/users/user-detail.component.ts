import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { UserService } from '../../core/api/users.service';
import { UserDetailDto } from '../../models/dtos';
import { BreadcrumbsComponent } from '../../shared/breadcrumbs.component';
import { SkeletonComponent } from '../../shared/page-skeleton.component';
import { NotifyService } from '../../core/notify.service';

@Component({
  selector: 'app-user-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, MatButtonModule, MatIconModule, MatChipsModule, BreadcrumbsComponent, SkeletonComponent],
  template: `
    <div class="sig-page">
      <sig-breadcrumbs [crumbs]="[{ label: 'Inicio', route: '/dashboard' }, { label: 'Users', route: '/users' }, { label: user() ? user()!.nombre + ' ' + user()!.apellidos : 'Detalle' }]" />
      <div class="sig-page__header">
        <h1 class="sig-page__title">{{ user() ? user()!.nombre + ' ' + user()!.apellidos : 'Cargando...' }}</h1>
        @if (user()) {
          <a mat-flat-button color="primary" [routerLink]="['/users', user()!.id, 'editar']" data-testid="btn-editar"><mat-icon>edit</mat-icon> Editar</a>
        }
      </div>
      @if (loading()) { <mat-card><mat-card-content><sig-skeleton [count]="6" /></mat-card-content></mat-card> }
      @else if (user()) {
        <mat-card><mat-card-content>
          <dl class="sig-dl">
            <dt>NIF</dt><dd class="mono-num">{{ user()!.nif }}</dd>
            <dt>Email</dt><dd>{{ user()!.email }}</dd>
            <dt>Estado</dt><dd><mat-chip>{{ user()!.estado }}</mat-chip></dd>
            <dt>Departamento</dt><dd>{{ user()!.departmentId ?? '—' }}</dd>
            <dt>Roles</dt><dd>{{ user()!.roleIds.length }} asignados</dd>
          </dl>
        </mat-card-content></mat-card>
      }
    </div>
  `,
  styles: [`.sig-dl { display: grid; grid-template-columns: 200px 1fr; gap: 8px 16px; margin: 0; } .sig-dl dt { color: var(--mat-sys-on-surface-variant); font-weight: 500; } .sig-dl dd { margin: 0; }`],
})
export class UserDetailComponent implements OnInit {
  private readonly userSvc = inject(UserService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly notify = inject(NotifyService);

  protected readonly user = signal<UserDetailDto | null>(null);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.userSvc.getById(id).subscribe({
      next: (u) => { this.user.set(u); this.loading.set(false); },
      error: () => { this.loading.set(false); this.notify.error('No se pudo cargar el usuario'); },
    });
  }
}
