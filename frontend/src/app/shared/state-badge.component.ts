import { Component, input, computed } from '@angular/core';
import { EstadoClosure, ApprovalStep, badgeClassFromClosure, badgeLabelFromClosure } from '../models/enums';

@Component({
  selector: 'sig-state-badge',
  standalone: true,
  template: `<span [class]="cssClass()" data-testid="badge-estado">{{ label() }}</span>`,
})
export class StateBadgeComponent {
  readonly estado = input.required<EstadoClosure>();
  readonly paso = input.required<ApprovalStep>();

  readonly cssClass = computed(() => `sig-badge sig-badge--${badgeClassFromClosure(this.estado(), this.paso())}`);
  readonly label = computed(() => badgeLabelFromClosure(this.estado(), this.paso()));
}
