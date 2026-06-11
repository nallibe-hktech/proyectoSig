import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { AuthService } from '../../core/auth/auth.service';
import { NotifyService } from '../../core/notify.service';
import { environment } from '../../../environments/environment';

interface DemoCred { email: string; password: string; nombre: string; rol: string; }

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatDividerModule,
    MatProgressSpinnerModule,
    MatCheckboxModule,
  ],
  template: `
    <div class="sig-login-wrapper">
      <!-- Background glows -->
      <div class="sig-login-bg">
        <div class="sig-glow sig-glow--blue"></div>
        <div class="sig-glow sig-glow--teal"></div>
        <div class="sig-glow sig-glow--mid"></div>
      </div>

      <div class="sig-login-layout">

        <!-- LEFT: Brand word-blocks -->
        <div class="sig-login-left">
          <div class="sig-word-blocks">
            <div class="sig-word-block sig-word-block--plain">service</div>
            <div class="sig-word-block sig-word-block--accent">innovation</div>
            <div class="sig-word-block sig-word-block--plain">
              group
              <span class="sig-reg">&reg;</span>
            </div>
          </div>
          <p class="sig-tagline">EXCELLENCE &ndash; MADE IN EUROPE</p>
        </div>

        <!-- RIGHT: Login card -->
        <div class="sig-login-right">
          <div class="sig-login-card" data-testid="login-card">
            <h2 class="sig-card-title">&iexcl;Bienvenido de nuevo!</h2>
            <p class="sig-card-sub">Introduce tus credenciales para acceder a la plataforma.</p>

            <form [formGroup]="form" (ngSubmit)="submit()" novalidate class="sig-form">

              <div class="sig-field-wrap">
                <label class="sig-label">Correo electr&oacute;nico</label>
                <div class="sig-input-row">
                  <mat-icon class="sig-input-icon">mail</mat-icon>
                  <input
                    class="sig-input"
                    type="email"
                    formControlName="email"
                    placeholder="nombre@sigeurope.com"
                    autocomplete="email"
                    data-testid="input-email"
                  />
                </div>
                @if (form.controls.email.touched && form.controls.email.hasError('required')) {
                  <span class="sig-field-error">El correo es obligatorio</span>
                }
                @if (form.controls.email.touched && form.controls.email.hasError('email')) {
                  <span class="sig-field-error">Introduce un correo v&aacute;lido</span>
                }
              </div>

              <div class="sig-field-wrap">
                <label class="sig-label">Contrase&ntilde;a</label>
                <div class="sig-input-row">
                  <mat-icon class="sig-input-icon">lock</mat-icon>
                  <input
                    class="sig-input"
                    [type]="showPassword() ? 'text' : 'password'"
                    formControlName="password"
                    placeholder="&bull;&bull;&bull;&bull;&bull;&bull;&bull;&bull;"
                    autocomplete="current-password"
                    data-testid="input-password"
                  />
                  <button
                    type="button"
                    class="sig-eye-btn"
                    (click)="showPassword.set(!showPassword())"
                    [attr.aria-label]="showPassword() ? 'Ocultar' : 'Mostrar'"
                  >
                    <mat-icon>{{ showPassword() ? 'visibility_off' : 'visibility' }}</mat-icon>
                  </button>
                </div>
                @if (form.controls.password.touched && form.controls.password.hasError('required')) {
                  <span class="sig-field-error">La contrase&ntilde;a es obligatoria</span>
                }
              </div>

              <div class="sig-login-opts">
                <label class="sig-remember">
                  <input type="checkbox" class="sig-checkbox" />
                  <span>Recordarme</span>
                </label>
                <a href="#" (click)="$event.preventDefault()" class="sig-forgot">&iquest;Olvidaste tu contrase&ntilde;a?</a>
              </div>

              @if (errorMessage()) {
                <div class="sig-error-banner" data-testid="login-error">
                  <mat-icon>error</mat-icon>
                  {{ errorMessage() }}
                </div>
              }

              <button
                type="submit"
                class="sig-submit-btn"
                [disabled]="loading() || form.invalid"
                data-testid="btn-login"
              >
                @if (loading()) {
                  <mat-spinner diameter="18" />
                } @else {
                  Acceder al Sistema &nbsp;&rarr;
                }
              </button>

              <div class="sig-or-divider"><span>o continua con</span></div>

              <button
                type="button"
                class="sig-sso-btn"
                disabled
                data-testid="btn-azure-sso"
              >
                <!-- Microsoft M logo -->
                <svg width="18" height="18" viewBox="0 0 21 21" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <rect x="1" y="1" width="9" height="9" fill="#f25022"/>
                  <rect x="11" y="1" width="9" height="9" fill="#7fba00"/>
                  <rect x="1" y="11" width="9" height="9" fill="#00a4ef"/>
                  <rect x="11" y="11" width="9" height="9" fill="#ffb900"/>
                </svg>
                Microsoft / Azure AD SSO
              </button>

            </form>
          </div>

          @if (showDemo) {
            <div class="sig-demo-card" data-testid="demo-credentials">
              <div class="sig-demo-header">
                <div>
                  <span class="sig-demo-title">Credenciales demo</span>
                  <span class="sig-demo-sub">Solo desarrollo</span>
                </div>
                <button type="button" (click)="toggleDemo()" class="sig-demo-toggle-btn" title="Desactivar modo demo">✕</button>
              </div>
              @for (c of demoCreds; track c.email) {
                <div class="sig-demo-row">
                  <div>
                    <div class="sig-demo-email">{{ c.email }}</div>
                    <div class="sig-demo-meta">{{ c.nombre }} &middot; {{ c.rol }}</div>
                  </div>
                  <button type="button" class="sig-use-btn" (click)="useDemo(c)">Usar</button>
                </div>
              }
            </div>
          }

          <footer class="sig-login-footer">
            SIG-ES Plataforma Integral v1.0 &middot; &copy; 2026 Service Innovation Group &middot; Excellence made in Europe
          </footer>
        </div>

      </div>
    </div>
  `,
  styles: [`
    :host { display: contents; }

    /* Wrapper */
    .sig-login-wrapper {
      position:       relative;
      display:        flex;
      flex-direction: column;
      min-height:     100vh;
      background:     #0d1b2a;
      overflow:       hidden;
    }

    /* BG glows */
    .sig-login-bg { position:absolute; inset:0; pointer-events:none; }
    .sig-glow     { position:absolute; border-radius:50%; filter:blur(110px); }
    .sig-glow--blue  { width:480px; height:480px; top:-140px; left:-60px; background:#2563eb; opacity:.14; }
    .sig-glow--teal  { width:400px; height:400px; bottom:-100px; right:-60px; background:#00d4c4; opacity:.10; }
    .sig-glow--mid   { width:260px; height:260px; top:42%; left:37%; background:#1e3a5c; opacity:.20; }

    /* Layout */
    .sig-login-layout {
      display:         flex;
      flex:            1;
      align-items:     center;
      justify-content: center;
      gap:             80px;
      padding:         48px 40px;
      position:        relative;
      z-index:         1;
    }

    /* ---- LEFT: word-blocks ---- */
    .sig-login-left {
      display:        flex;
      flex-direction: column;
      gap:            12px;
      align-items:    flex-start;
    }

    .sig-word-blocks {
      display:     flex;
      flex-direction: row;
      align-items: center;
      gap:         8px;
      flex-wrap:   wrap;
    }

    .sig-word-block {
      display:        inline-flex;
      align-items:    center;
      padding:        8px 18px;
      border:         2px solid rgba(255,255,255,.15);
      border-radius:  6px;
      font-size:      20px;
      font-weight:    700;
      letter-spacing: 1.5px;
      text-transform: lowercase;
      color:          #ffffff;
      background:     rgba(255,255,255,.04);
      position:       relative;
      white-space:    nowrap;
    }

    .sig-word-block--accent {
      background:   #2563eb;
      border-color: #2563eb;
      color:        #ffffff;
    }

    .sig-reg {
      font-size:     12px;
      vertical-align: super;
      margin-left:   2px;
      font-weight:   400;
      opacity:       .7;
    }

    .sig-tagline {
      font-size:      10px;
      font-weight:    600;
      letter-spacing: 3px;
      color:          rgba(255,255,255,.4);
      text-align:     left;
      margin:         8px 0 0;
    }

    /* ---- RIGHT: Card ---- */
    .sig-login-right {
      display:        flex;
      flex-direction: column;
      align-items:    stretch;
      gap:            14px;
      width:          400px;
    }

    .sig-login-card {
      background:    rgba(255,255,255,.04);
      border:        1px solid rgba(255,255,255,.1);
      border-radius: 14px;
      padding:       32px;
      backdrop-filter: blur(10px);
    }

    .sig-card-title {
      font-size:   24px;
      font-weight: 700;
      color:       #ffffff;
      margin:      0 0 6px;
    }

    .sig-card-sub {
      font-size:  13px;
      color:      rgba(255,255,255,.45);
      margin:     0 0 24px;
    }

    /* Form */
    .sig-form { display:flex; flex-direction:column; gap:14px; }

    .sig-field-wrap { display:flex; flex-direction:column; gap:5px; }

    .sig-label {
      font-size:   12px;
      font-weight: 500;
      color:       rgba(255,255,255,.55);
      letter-spacing: .3px;
    }

    .sig-input-row {
      display:        flex;
      align-items:    center;
      background:     rgba(255,255,255,.06);
      border:         1px solid rgba(255,255,255,.12);
      border-radius:  8px;
      padding:        0 12px;
      transition:     border-color 150ms;
      &:focus-within { border-color: #2563eb; }
    }

    .sig-input-icon {
      font-size:  18px !important;
      width:      18px !important;
      height:     18px !important;
      color:      rgba(255,255,255,.3) !important;
      margin-right: 8px;
    }

    .sig-input {
      flex:           1;
      background:     transparent;
      border:         none;
      outline:        none;
      font-size:      14px;
      color:          #e8f0f9;
      padding:        11px 0;
      font-family:    inherit;
      &::placeholder { color: rgba(255,255,255,.25); }
    }

    .sig-eye-btn {
      background:  transparent;
      border:      none;
      cursor:      pointer;
      color:       rgba(255,255,255,.3);
      display:     flex;
      align-items: center;
      padding:     0;
      mat-icon { font-size:18px !important; width:18px !important; height:18px !important; }
    }

    .sig-field-error { font-size:11px; color:#ef4444; margin-top:2px; }

    .sig-login-opts {
      display:         flex;
      justify-content: space-between;
      align-items:     center;
      margin-top:      2px;
    }

    .sig-remember {
      display:     flex;
      align-items: center;
      gap:         6px;
      font-size:   12px;
      color:       rgba(255,255,255,.45);
      cursor:      pointer;
    }

    .sig-checkbox {
      accent-color: #2563eb;
      width: 14px; height: 14px;
    }

    .sig-forgot {
      font-size:       12px;
      color:           rgba(255,255,255,.35);
      text-decoration: none;
      &:hover { color: #3b82f6; }
    }

    .sig-error-banner {
      display:       flex;
      align-items:   center;
      gap:           8px;
      color:         #ef4444;
      background:    rgba(239,68,68,.1);
      border:        1px solid rgba(239,68,68,.25);
      padding:       9px 12px;
      border-radius: 8px;
      font-size:     13px;
      mat-icon { font-size:18px !important; width:18px !important; height:18px !important; }
    }

    .sig-submit-btn {
      width:           100%;
      height:          44px;
      background:      #2563eb;
      color:           #ffffff;
      border:          none;
      border-radius:   8px;
      font-size:       15px;
      font-weight:     600;
      font-family:     inherit;
      cursor:          pointer;
      display:         flex;
      align-items:     center;
      justify-content: center;
      gap:             6px;
      transition:      background 150ms;
      margin-top:      4px;
      &:hover:not(:disabled) { background: #3b82f6; }
      &:disabled { opacity:.5; cursor:not-allowed; }
    }

    .sig-or-divider {
      display:     flex;
      align-items: center;
      gap:         12px;
      color:       rgba(255,255,255,.2);
      font-size:   12px;
      &::before, &::after { content:''; flex:1; border-top:1px solid rgba(255,255,255,.1); }
    }

    .sig-sso-btn {
      width:           100%;
      height:          42px;
      background:      transparent;
      color:           rgba(255,255,255,.5);
      border:          1px solid rgba(255,255,255,.1);
      border-radius:   8px;
      font-size:       13px;
      font-family:     inherit;
      cursor:          pointer;
      display:         flex;
      align-items:     center;
      justify-content: center;
      gap:             10px;
      opacity:         0.6;
    }

    /* Demo card */
    .sig-demo-card {
      background:    rgba(255,255,255,.03);
      border:        1px solid rgba(255,255,255,.08);
      border-radius: 10px;
      padding:       16px;
    }

    .sig-demo-header {
      display:         flex;
      justify-content: space-between;
      align-items:     flex-start;
      margin-bottom:   10px;
    }

    .sig-demo-title { font-size:13px; font-weight:600; color:rgba(255,255,255,.7); }
    .sig-demo-sub   { font-size:11px; color:rgba(255,255,255,.3); }

    .sig-demo-toggle-btn {
      background: transparent; border: none; color: rgba(255,255,255,.3); 
      font-size: 18px; cursor: pointer; padding: 0; width: 20px; height: 20px;
      display: flex; align-items: center; justify-content: center;
      transition: color 150ms; &:hover { color: rgba(255,255,255,.6); }
    }

    .sig-demo-row {
      display:         flex;
      justify-content: space-between;
      align-items:     center;
      padding:         7px 0;
      border-bottom:   1px solid rgba(255,255,255,.06);
      &:last-of-type { border-bottom: none; }
    }

    .sig-demo-email { font-size:12px; font-weight:500; color:rgba(255,255,255,.75); }
    .sig-demo-meta  { font-size:11px; color:rgba(255,255,255,.35); margin-top:1px; }

    .sig-use-btn {
      height:        28px;
      padding:       0 12px;
      background:    transparent;
      color:         #3b82f6;
      border:        1px solid rgba(59,130,246,.4);
      border-radius: 6px;
      font-size:     12px;
      font-family:   inherit;
      cursor:        pointer;
      &:hover { background: rgba(59,130,246,.1); }
    }

    /* Footer */
    .sig-login-footer {
      text-align: center;
      font-size:  11px;
      color:      rgba(255,255,255,.2);
      padding:    4px 0 8px;
    }

    @media (max-width: 900px) {
      .sig-login-layout { flex-direction:column; gap:32px; padding:28px 20px; }
      .sig-login-left   { align-items:center; }
      .sig-login-right  { width:100%; max-width:420px; }
      .sig-word-block   { font-size:20px; min-width:180px; }
    }
  `],
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly notify = inject(NotifyService);

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  protected readonly loading = signal(false);
  protected readonly showPassword = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected get showDemo(): boolean {
    if (environment.production) {
      const o = localStorage.getItem('sig_showDemo');
      if (o === 'true') return true;
      if (o === 'false') return false;
    }
    return environment.showDemoCredentials;
  }
  protected readonly demoCreds: DemoCred[] = [
    { email: 'admin@sig.local', password: 'Demo#2026!', nombre: 'Admin SIG', rol: 'Administrator' },
    { email: 'direccion@sig.local', password: 'Demo#2026!', nombre: 'Carmen Ruiz', rol: 'Direction' },
    { email: 'fico@sig.local', password: 'Demo#2026!', nombre: 'Javier Lopez', rol: 'Fico' },
    { email: 'backoffice1@sig.local', password: 'Demo#2026!', nombre: 'Laura Sanchez', rol: 'Backoffice' },
    { email: 'pm.alpha@sig.local', password: 'Demo#2026!', nombre: 'Maria Garcia', rol: 'ProjectManager' },
    { email: 'auditor@sig.local', password: 'Demo#2026!', nombre: 'Ines Romero', rol: 'Auditor' },
    { email: 'reader@sig.local', password: 'Demo#2026!', nombre: 'Luis Vega', rol: 'Reader' },
  ];

  protected useDemo(c: DemoCred): void {
    this.form.patchValue({ email: c.email, password: c.password });
  }

  protected toggleDemo(): void {
    const current = localStorage.getItem('sig_showDemo');
    const next = current === 'false' ? 'true' : 'false';
    localStorage.setItem('sig_showDemo', next);
    location.reload();
  }

  protected submit(): void {
    this.errorMessage.set(null);
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.loading.set(true);
    const { email, password } = this.form.getRawValue();
    this.auth.login({ email, password }).subscribe({
      next: () => {
        this.loading.set(false);
        this.notify.success('Sesion iniciada correctamente');
        void this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.loading.set(false);
        const msg = err?.error?.title ?? 'Correo o contrasena incorrectos';
        this.errorMessage.set(msg);
      },
    });
  }
}
