import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideAnimations } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { LoginComponent } from './login.component';
import { AuthService } from '../../core/auth/auth.service';
import { NotifyService } from '../../core/notify.service';

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let authSpy: jasmine.SpyObj<AuthService>;
  let notifySpy: jasmine.SpyObj<NotifyService>;
  let navSpy: jasmine.Spy;

  beforeEach(async () => {
    authSpy = jasmine.createSpyObj('AuthService', ['login']);
    notifySpy = jasmine.createSpyObj('NotifyService', ['success', 'error', 'info']);

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideAnimations(),
        { provide: AuthService, useValue: authSpy },
        { provide: NotifyService, useValue: notifySpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    navSpy = spyOn(TestBed.inject(Router), 'navigate').and.returnValue(Promise.resolve(true));
    fixture.detectChanges();
  });

  it('renderiza la tarjeta de login', () => {
    const card = fixture.nativeElement.querySelector('[data-testid=login-card]');
    expect(card).toBeTruthy();
  });

  it('email + password: campos input visibles con data-testid', () => {
    const email = fixture.nativeElement.querySelector('[data-testid=input-email]');
    expect(email).toBeTruthy();
  });

  it('submit con formulario inválido NO llama a auth.login', () => {
    const comp = fixture.componentInstance as any;
    comp.form.patchValue({ email: '', password: '' });
    comp.submit();
    expect(authSpy.login).not.toHaveBeenCalled();
  });

  it('submit con formulario válido llama a auth.login, muestra snackbar y navega a /dashboard', () => {
    authSpy.login.and.returnValue(of({ accessToken: 'a', refreshToken: 'r', user: { id: 1, nombre: 'X', apellidos: 'Y', email: 'x@y.com', roles: [] } } as any));
    const comp = fixture.componentInstance as any;
    comp.form.patchValue({ email: 'x@y.com', password: 'Demo#2026!' });
    comp.submit();
    expect(authSpy.login).toHaveBeenCalledWith({ email: 'x@y.com', password: 'Demo#2026!' });
    expect(notifySpy.success).toHaveBeenCalled();
    expect(navSpy).toHaveBeenCalledWith(['/dashboard']);
  });

  it('submit con credenciales incorrectas muestra mensaje de error', () => {
    authSpy.login.and.returnValue(throwError(() => ({ error: { title: 'Credenciales inválidas' } })));
    const comp = fixture.componentInstance as any;
    comp.form.patchValue({ email: 'x@y.com', password: 'WrongPassword!' });
    comp.submit();
    expect(comp.errorMessage()).toBe('Credenciales inválidas');
    expect(navSpy).not.toHaveBeenCalled();
  });

  it('useDemo rellena email y password', () => {
    const comp = fixture.componentInstance as any;
    comp.useDemo({ email: 'admin@sig.local', password: 'Demo#2026!', nombre: 'A', rol: 'R' });
    expect(comp.form.value.email).toBe('admin@sig.local');
    expect(comp.form.value.password).toBe('Demo#2026!');
  });
});
