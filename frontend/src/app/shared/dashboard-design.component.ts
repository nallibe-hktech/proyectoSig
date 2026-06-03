import { Component } from '@angular/core';

@Component({
  selector: 'app-dashboard-design',
  standalone: true,
  template: `
    <svg width="32" height="32" viewBox="0 0 32 32" fill="none" xmlns="http://www.w3.org/2000/svg"
         class="sig-logo-svg" aria-label="SIG Logo">
      <rect width="32" height="32" rx="8" fill="#2563eb"/>
      <rect width="32" height="32" rx="8" fill="url(#dashGrad)" opacity="0.3"/>
      <text x="16" y="21" text-anchor="middle"
            font-family="Inter, Roboto, sans-serif"
            font-size="11" font-weight="800"
            letter-spacing="-0.3"
            fill="#ffffff">SIG</text>
      <circle cx="26" cy="6" r="3" fill="#00d4c4"/>
      <defs>
        <radialGradient id="dashGrad" cx="30%" cy="20%" r="80%">
          <stop offset="0%" stop-color="#00d4c4"/>
          <stop offset="100%" stop-color="#1d4ed8"/>
        </radialGradient>
      </defs>
    </svg>
  `,
  styles: [`
    .sig-logo-svg {
      display: block;
      filter: drop-shadow(0 2px 8px rgba(37, 99, 235, 0.4));
    }
  `],
})
export class DashboardDesignComponent {}
