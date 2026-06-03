import { Component } from '@angular/core';

@Component({
  selector: 'app-login-design',
  standalone: true,
  template: `
    <div class="sig-login-logo">
      <svg width="72" height="72" viewBox="0 0 72 72" fill="none" xmlns="http://www.w3.org/2000/svg">
        <circle cx="36" cy="36" r="34" stroke="#1e3a5c" stroke-width="2"/>
        <circle cx="36" cy="36" r="28" fill="#0d1b2a"/>
        <circle cx="36" cy="36" r="28" fill="url(#logoGrad)" opacity="0.35"/>
        <text x="36" y="44" text-anchor="middle"
              font-family="Inter, Roboto, sans-serif"
              font-size="22" font-weight="800"
              letter-spacing="-1"
              fill="url(#textGrad)">SIG</text>
        <line x1="20" y1="52" x2="52" y2="52" stroke="url(#lineGrad)" stroke-width="1.5" stroke-linecap="round"/>
        <circle cx="55" cy="17" r="4" fill="#00d4c4" opacity="0.8"/>
        <circle cx="55" cy="17" r="2" fill="#00d4c4"/>
        <defs>
          <radialGradient id="logoGrad" cx="50%" cy="30%" r="70%">
            <stop offset="0%" stop-color="#2563eb"/>
            <stop offset="100%" stop-color="#00d4c4"/>
          </radialGradient>
          <linearGradient id="textGrad" x1="20" y1="36" x2="52" y2="36">
            <stop offset="0%" stop-color="#ffffff"/>
            <stop offset="100%" stop-color="#00d4c4"/>
          </linearGradient>
          <linearGradient id="lineGrad" x1="20" y1="52" x2="52" y2="52">
            <stop offset="0%" stop-color="#2563eb" stop-opacity="0"/>
            <stop offset="50%" stop-color="#00d4c4"/>
            <stop offset="100%" stop-color="#2563eb" stop-opacity="0"/>
          </linearGradient>
        </defs>
      </svg>
    </div>
  `,
  styles: [`
    .sig-login-logo {
      display: flex;
      align-items: center;
      justify-content: center;
      margin-bottom: 8px;
      filter: drop-shadow(0 4px 16px rgba(0, 212, 196, 0.2));
    }
  `],
})
export class LoginDesignComponent {}
