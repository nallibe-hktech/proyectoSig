import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, UrlTree } from '@angular/router';
import { signal } from '@angular/core';
import { authGuard, roleGuard } from './auth.guard';
import { AuthService } from './auth.service';

describe('authGuard', () => {
  let authMock: Partial<AuthService>;
  let router: Router;

  beforeEach(() => {
    authMock = {
      isAuthenticated: signal(false) as any,
      currentUser: signal(null) as any,
    };
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: authMock },
      ],
    });
    router = TestBed.inject(Router);
  });

  function runGuard(fn: any, route: any = {}): boolean | UrlTree {
    return TestBed.runInInjectionContext(() => fn(route, {} as any) as boolean | UrlTree);
  }

  it('authGuard: deja pasar si está autenticado', () => {
    (authMock.isAuthenticated as any) = signal(true);
    const r = runGuard(authGuard);
    expect(r).toBe(true);
  });

  it('authGuard: redirige a /login si NO está autenticado', () => {
    (authMock.isAuthenticated as any) = signal(false);
    const r = runGuard(authGuard);
    expect(r instanceof UrlTree).toBe(true);
    expect(router.serializeUrl(r as UrlTree)).toBe('/login');
  });

  it('roleGuard: redirige a /login si NO está autenticado (aunque haya roles)', () => {
    (authMock.isAuthenticated as any) = signal(false);
    const r = runGuard(roleGuard, { data: { roles: ['Administrator'] } });
    expect(r instanceof UrlTree).toBe(true);
    expect(router.serializeUrl(r as UrlTree)).toBe('/login');
  });

  it('roleGuard: deja pasar si la lista de roles está vacía', () => {
    (authMock.isAuthenticated as any) = signal(true);
    authMock.hasAnyRole = jasmine.createSpy('hasAnyRole').and.returnValue(false);
    const r = runGuard(roleGuard, { data: { roles: [] } });
    expect(r).toBe(true);
  });

  it('roleGuard: deja pasar si el usuario tiene el rol', () => {
    (authMock.isAuthenticated as any) = signal(true);
    authMock.hasAnyRole = jasmine.createSpy('hasAnyRole').and.returnValue(true);
    const r = runGuard(roleGuard, { data: { roles: ['Administrator'] } });
    expect(r).toBe(true);
    expect(authMock.hasAnyRole).toHaveBeenCalledWith('Administrator');
  });

  it('roleGuard: redirige a /dashboard si autenticado pero sin rol', () => {
    (authMock.isAuthenticated as any) = signal(true);
    authMock.hasAnyRole = jasmine.createSpy('hasAnyRole').and.returnValue(false);
    const r = runGuard(roleGuard, { data: { roles: ['Administrator'] } });
    expect(r instanceof UrlTree).toBe(true);
    expect(router.serializeUrl(r as UrlTree)).toBe('/dashboard');
  });
});
