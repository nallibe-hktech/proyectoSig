import { Component, input } from '@angular/core';

// Skeleton de página/tabla (5 filas placeholder).
@Component({
  selector: 'sig-skeleton',
  standalone: true,
  template: `
    @for (_ of rows; track $index) {
      <div class="sig-skeleton-row"></div>
    }
  `,
})
export class SkeletonComponent {
  readonly count = input<number>(5);
  get rows() { return Array(this.count()).fill(0); }
}
