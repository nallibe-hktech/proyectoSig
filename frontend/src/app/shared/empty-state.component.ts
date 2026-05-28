import { Component, input, output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'sig-empty-state',
  standalone: true,
  imports: [MatIconModule, MatButtonModule],
  template: `
    <div class="sig-empty-state" data-testid="empty-state">
      <mat-icon class="sig-empty-icon" aria-hidden="true">{{ icon() }}</mat-icon>
      <h2 class="sig-empty-title">{{ title() }}</h2>
      @if (description()) {
        <p class="sig-empty-desc">{{ description() }}</p>
      }
      @if (ctaLabel()) {
        <button
          mat-flat-button
          color="primary"
          (click)="ctaClick.emit()"
          data-testid="btn-empty-cta"
        >
          @if (!hasFilter()) {
            <mat-icon aria-hidden="true">add</mat-icon>
          }
          {{ ctaLabel() }}
        </button>
      }
    </div>
  `,
  styles: [`
    .sig-empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 48px 24px;
      text-align: center;
    }
    .sig-empty-icon {
      font-size: 96px;
      width: 96px;
      height: 96px;
      opacity: 0.25;
      color: var(--mat-sys-on-surface);
      margin-bottom: 16px;
    }
    .sig-empty-title {
      font-size: 24px;
      font-weight: 600;
      color: var(--mat-sys-on-surface);
      margin: 0 0 8px;
    }
    .sig-empty-desc {
      font-size: 14px;
      color: var(--mat-sys-on-surface-variant);
      max-width: 400px;
      margin: 0 0 24px;
    }
  `],
})
export class EmptyStateComponent {
  readonly icon = input.required<string>();
  readonly title = input.required<string>();
  readonly description = input<string>('');
  readonly ctaLabel = input<string>('');
  readonly hasFilter = input<boolean>(false);
  readonly ctaClick = output<void>();
}
