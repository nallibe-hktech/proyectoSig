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
import { LoginDesignComponent } from '../../shared/login-design.component';
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
    // Login design SVG component
    LoginDesignComponent,
  ],
  template: `
    <div class="sig-login-wrapper">
      <!-- Decorative background elements -->
      <div class="sig-login-bg-decor">
        <div class="sig-bg-circle sig-bg-circle--1"></div>
        <div class="sig-bg-circle sig-bg-circle--2"></div>
        <div class="sig-bg-circle sig-bg-circle--3"></div>
      </div>

      <div class="sig-login-layout">
        <!-- Left: Branding + Feature pills -->
        <div class="sig-login-brand">
          <div class="sig-brand-header">
            <span class="sig-brand-company">h&amp;k consulting</span>
            <h1 class="sig-brand-title">Plataforma<br><span class="sig-brand-accent">Operativa SIG</span></h1>
            <p class="sig-brand-sub">Sistema Integrado de Gesti&oacute;n · ES</p>
          </div>

          <div class="sig-feature-pills">
            <span class="sig-pill sig-pill--success">✅ Cierres automatizados</span>
            <span class="sig-pill">🔗 9 sistemas integrados</span>
            <span class="sig-pill">📊 Power BI en tiempo real</span>
            <span class="sig-pill">🔒 Auditor&iacute;a completa</span>
          </div>

          <div class="sig-integration-sidebar">
            <span class="sig-int-label">Integraciones activas</span>
            <div class="sig-int-list">
              <span class="sig-int-item"><span class="sig-dot sig-dot--green"></span> Celero</span>
              <span class="sig-int-item"><span class="sig-dot sig-dot--green"></span> Bizneo</span>
              <span class="sig-int-item"><span class="sig-dot sig-dot--green"></span> Intratime</span>
              <span class="sig-int-item"><span class="sig-dot sig-dot--green"></span> Payhawk</span>
            </div>
          </div>
        </div>

        <!-- Right: Login Card + Demo -->
        <div class="sig-login-right">
          <mat-card class="sig-login-card" data-testid="login-card">
            <div class="sig-card-accent"></div>

            <div class="sig-card-header">
                          <app-login-design></app-login-design>
              <h2 class="sig-card-title">Iniciar Sesi&oacute;n</h2>
              <p class="sig-card-subtitle">Accede a tu plataforma operativa</p>
            </div>

            <mat-divider />

            <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
              <mat-form-field appearance="outline" class="sig-full-width">
                <mat-label>Correo electr&oacute;nico</mat-label>
                <input
                  matInput
                  type="email"
                  formControlName="email"
                  placeholder="usuario@sigeurope.com"
                  autocomplete="email"
                  data-testid="input-email"
                />
                <mat-icon matSuffix aria-hidden="true">mail</mat-icon>
                @if (form.controls.email.touched && form.controls.email.hasError('required')) {
                  <mat-error>El correo es obligatorio</mat-error>
                }
                @if (form.controls.email.touched && form.controls.email.hasError('email')) {
                  <mat-error>Introduce un correo v&aacute;lido</mat-error>
                }
              </mat-form-field>

              <mat-form-field appearance="outline" class="sig-full-width">
                <mat-label>Contrase&ntilde;a</mat-label>
                <input
                  matInput
                  [type]="showPassword() ? 'text' : 'password'"
                  formControlName="password"
                  placeholder="&bull;&bull;&bull;&bull;&bull;&bull;&bull;&bull;"
                  autocomplete="current-password"
                  data-testid="input-password"
                />
                <button
                  mat-icon-button
                  matSuffix
                  type="button"
                  (click)="showPassword.set(!showPassword())"
                  [attr.aria-label]="showPassword() ? 'Ocultar contraseña' : 'Mostrar contraseña'"
                  data-testid="btn-toggle-password"
                >
                  <mat-icon>{{ showPassword() ? 'visibility_off' : 'visibility' }}</mat-icon>
                </button>
                @if (form.controls.password.touched && form.controls.password.hasError('required')) {
                  <mat-error>La contrase&ntilde;a es obligatoria</mat-error>
                }
                @if (form.controls.password.touched && form.controls.password.hasError('minlength')) {
                  <mat-error>La contrase&ntilde;a debe tener al menos 8 caracteres</mat-error>
                }
              </mat-form-field>

              <div class="sig-login-options">
                <mat-checkbox data-testid="remember-me">Recordar sesi&oacute;n</mat-checkbox>
                <a href="#" (click)="$event.preventDefault()" class="sig-forgot-link">&iquest;Olvidaste tu contrase&ntilde;a?</a>
              </div>

              @if (errorMessage()) {
                <div class="sig-login-error" data-testid="login-error">
                  <mat-icon aria-hidden="true">error</mat-icon>
                  {{ errorMessage() }}
                </div>
              }

              <button
                mat-flat-button
                type="submit"
                class="sig-full-width sig-login-btn"
                [disabled]="loading() || form.invalid"
                data-testid="btn-login"
              >
                @if (loading()) {
                  <mat-spinner diameter="20" />
                } @else {
                  Acceder al Sistema
                }
              </button>

              <div class="sig-divider-text">
                <span>o continuar con</span>
              </div>

              <button
                mat-stroked-button
                type="button"
                class="sig-full-width sig-azure-btn"
                disabled
                data-testid="btn-azure-sso"
              >
                <mat-icon aria-hidden="true">cloud</mat-icon>
                Azure AD (SSO pr&oacute;ximamente)
              </button>
            </form>
          </mat-card>

          @if (showDemo) {
            <mat-card class="sig-demo-card" data-testid="demo-credentials">
              <mat-card-header>
                <mat-card-title style="font-size: 14px;">Credenciales demo</mat-card-title>
                <mat-card-subtitle>Solo desarrollo</mat-card-subtitle>
              </mat-card-header>
              <mat-card-content>
                @for (c of demoCreds; track c.email) {
                  <div class="sig-demo-row">
                    <div>
                      <div style="font-size: 13px; font-weight: 500;">{{ c.email }}</div>
                      <div style="font-size: 11px; color: var(--mat-sys-on-surface-variant);">{{ c.nombre }} · {{ c.rol }}</div>
                    </div>
                    <button
                      mat-stroked-button
                      type="button"
                      size="small"
                      (click)="useDemo(c)"
                      [attr.data-testid]="'btn-demo-' + c.rol.toLowerCase()"
                    >
                      Usar
                    </button>
                  </div>
                }
                <mat-divider />
                <div style="font-size: 11px; color: var(--mat-sys-on-surface-variant); padding: 8px 0 2px;">
                  Password com&uacute;n: <strong>Demo#2026!</strong>
                </div>
              </mat-card-content>
            </mat-card>
          }
        </div>
      </div>

      <footer class="sig-login-footer">
        <span>v1.0.0 · &copy; 2026 SIG ES · Todos los derechos reservados</span>
      </footer>
    </div>
  `,
  styles: [`
    :host { display: contents; }
    .sig-login-wrapper {
      position: relative;
      display: flex;
      flex-direction: column;
      min-height: 100vh;
      background: linear-gradient(135deg, var(--mat-sys-primary) 0%, var(--sig-primary-dark) 50%, var(--sig-primary-dark) 100%);
      overflow: hidden;
    }
    .sig-login-bg-decor {
      position: absolute; inset: 0; pointer-events: none;
    }
    .sig-bg-circle {
      position: absolute; border-radius: 50%;
    }
    .sig-bg-circle--1 {
      width: 400px; height: 400px; top: -100px; left: 10%;
      background: rgba(112,173,71,0.06);
    }
    .sig-bg-circle--2 {
      width: 500px; height: 500px; bottom: -150px; right: -50px;
      background: rgba(46,92,138,0.10);
    }
    .sig-bg-circle--3 {
      width: 300px; height: 300px; top: 50%; left: 40%;
      background: rgba(255,255,255,0.02);
    }

    .sig-login-layout {
      display: flex;
      flex: 1;
      align-items: center;
      justify-content: center;
      gap: 60px;
      padding: 40px;
      position: relative;
      z-index: 1;
    }

    /* Left branding */
    .sig-login-brand {
      display: flex;
      flex-direction: column;
      gap: 32px;
      max-width: 420px;
    }
    .sig-brand-company {
      font-size: 12px;
      font-weight: 700;
      letter-spacing: 3px;
      text-transform: uppercase;
      color: rgba(255,255,255,0.4);
    }
    .sig-brand-title {
      font-size: 36px;
      font-weight: 800;
      color: #FFFFFF;
      margin: 8px 0 0;
      line-height: 1.2;
    }
    .sig-brand-accent {
      color: var(--sig-success);
    }
    .sig-brand-sub {
      font-size: 14px;
      color: rgba(255,255,255,0.5);
      margin: 4px 0 0;
    }
    .sig-feature-pills {
      display: flex;
      flex-direction: column;
      gap: 10px;
    }
    .sig-pill {
      display: inline-flex;
      align-items: center;
      height: 32px;
      padding: 0 16px;
      border-radius: 16px;
      font-size: 12px;
      color: rgba(255,255,255,0.7);
      background: rgba(255,255,255,0.08);
      border: 1px solid rgba(255,255,255,0.15);
      width: fit-content;
    }
    .sig-pill--success {
      background: rgba(112,173,71,0.2);
      border-color: rgba(112,173,71,0.4);
      color: var(--sig-success);
    }
    .sig-integration-sidebar {
      margin-top: 8px;
    }
    .sig-int-label {
      font-size: 10px; font-weight: 700; letter-spacing: 1.5px;
      color: rgba(255,255,255,0.4); text-transform: uppercase;
    }
    .sig-int-list {
      display: flex; flex-direction: column; gap: 6px; margin-top: 8px;
    }
    .sig-int-item {
      display: flex; align-items: center; gap: 8px;
      font-size: 12px; color: rgba(255,255,255,0.6);
    }
    .sig-dot {
      width: 8px; height: 8px; border-radius: 50%;
    }
    .sig-dot--green { background: var(--sig-success); }
    .sig-dot--yellow { background: var(--sig-warning); }
    .sig-dot--red { background: var(--sig-danger); }

    /* Right card area */
    .sig-login-right {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 16px;
    }
    .sig-login-card {
      width: 500px;
      border-radius: 20px;
      padding: 0;
      overflow: hidden;
      box-shadow: 0 20px 40px rgba(0,0,0,0.35);
    }
    .sig-card-accent {
      height: 8px;
      background: var(--sig-success);
    }
    .sig-card-header {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 32px 24px 16px;
    }
    .sig-login-illustration {
      width: 120px;
      height: 120px;
      object-fit: contain;
      margin-bottom: 20px;
      border-radius: 12px;
      box-shadow: 0 8px 24px rgba(0,0,0,0.25);
      background: transparent;
    }
    .sig-card-title {
      font-size: 26px; font-weight: 700; color: var(--mat-sys-primary); margin: 0 0 4px;
    }
    .sig-card-subtitle {
      font-size: 13px; color: #999; margin: 0;
    }

    .sig-login-card form {
      padding: 16px 32px 32px;
    }
    .sig-full-width { width: 100%; margin-bottom: 4px; }
    .sig-login-options {
      display: flex; justify-content: space-between; align-items: center;
      margin: 8px 0 12px;
    }
    .sig-forgot-link {
      font-size: 12px; color: var(--mat-sys-secondary); text-decoration: none;
    }
    .sig-forgot-link:hover { text-decoration: underline; }
    .sig-login-btn {
      margin-top: 8px; height: 44px;
      background: linear-gradient(90deg, var(--mat-sys-primary), var(--sig-primary-light)) !important;
      font-weight: 600; font-size: 15px;
    }
    .sig-login-error {
      display: flex; align-items: center; gap: 8px;
      color: var(--mat-sys-error); background: var(--mat-sys-error-container);
      padding: 8px 12px; border-radius: 8px; margin: 8px 0; font-size: 13px;
    }
    .sig-login-error mat-icon { font-size: 18px; width: 18px; height: 18px; }

    .sig-divider-text {
      display: flex; align-items: center; gap: 12px;
      margin: 16px 0; color: var(--sig-text-light); font-size: 12px;
    }
    .sig-divider-text::before, .sig-divider-text::after {
      content: ''; flex: 1; border-top: 1px solid var(--sig-border);
    }
    .sig-azure-btn {
      color: var(--mat-sys-primary) !important; border-color: var(--mat-sys-primary) !important;
      opacity: 0.6;
    }

    .sig-demo-card {
      width: 500px;
      border-radius: 12px;
    }
    .sig-demo-row {
      display: flex; justify-content: space-between; align-items: center;
      padding: 6px 0; border-bottom: 1px solid var(--mat-sys-outline-variant);
    }
    .sig-demo-row:last-of-type { border-bottom: none; }

    .sig-login-footer {
      text-align: center; padding: 16px;
      font-size: 11px; color: rgba(255,255,255,0.4);
      position: relative; z-index: 1;
    }

    @media (max-width: 1024px) {
      .sig-login-layout { flex-direction: column; gap: 32px; padding: 24px; }
      .sig-login-brand { max-width: 100%; align-items: center; text-align: center; }
      .sig-feature-pills { align-items: center; }
      .sig-integration-sidebar { display: none; }
      .sig-login-card { width: 100%; max-width: 460px; }
      .sig-demo-card { width: 100%; max-width: 460px; }
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

  protected readonly showDemo = environment.showDemoCredentials;
  protected readonly demoCreds: DemoCred[] = [
    { email: 'admin@sig.local', password: 'Demo#2026!', nombre: 'Admin SIG', rol: 'Administrator' },
    { email: 'direccion@sig.local', password: 'Demo#2026!', nombre: 'Carmen Ruiz', rol: 'Direction' },
    { email: 'fico@sig.local', password: 'Demo#2026!', nombre: 'Javier López', rol: 'Fico' },
    { email: 'backoffice1@sig.local', password: 'Demo#2026!', nombre: 'Laura Sánchez', rol: 'Backoffice' },
    { email: 'pm.alpha@sig.local', password: 'Demo#2026!', nombre: 'María García', rol: 'ProjectManager' },
    { email: 'auditor@sig.local', password: 'Demo#2026!', nombre: 'Inés Romero', rol: 'Auditor' },
    { email: 'reader@sig.local', password: 'Demo#2026!', nombre: 'Luis Vega', rol: 'Reader' },
  ];

  protected useDemo(c: DemoCred): void {
    this.form.patchValue({ email: c.email, password: c.password });
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
        this.notify.success('Sesión iniciada correctamente');
        void this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.loading.set(false);
        const msg = err?.error?.title ?? 'Correo o contraseña incorrectos';
        this.errorMessage.set(msg);
      },
    });
  }
}
