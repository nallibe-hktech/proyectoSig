import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';

export interface Crumb { label: string; route?: string; }

@Component({
  selector: 'sig-breadcrumbs',
  standalone: true,
  imports: [RouterLink, MatIconModule],
  template: `
    <nav class="sig-breadcrumbs" aria-label="Breadcrumb" data-testid="breadcrumbs">
      @for (c of crumbs(); track c.label; let last = $last) {
        @if (!last && c.route) {
          <a [routerLink]="c.route">{{ c.label }}</a>
          <mat-icon class="sig-breadcrumb-separator" aria-hidden="true">chevron_right</mat-icon>
        } @else {
          <span class="sig-breadcrumb-current">{{ c.label }}</span>
        }
      }
    </nav>
  `,
})
export class BreadcrumbsComponent {
  readonly crumbs = input.required<Crumb[]>();
}
