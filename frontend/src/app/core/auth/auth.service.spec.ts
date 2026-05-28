import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';
import { LoginRequest, LoginResponse, RefreshResponse, UsuarioBriefDto } from '../../models/dtos';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.clear();
  });

  it('login: hace POST /auth/login y guarda tokens en sessionStorage', () => {
    const req: LoginRequest = { email: 'admin@sig.local', password: 'Demo#2026!' };
    const user: UsuarioBriefDto = { id: 1, nombre: 'Admin', apellidos: 'SIG', email: 'admin@sig.local', roles: ['Administrator'] };
    const resp: LoginResponse = { accessToken: 'token-a', refreshToken: 'token-r', user };

    let captured: LoginResponse | undefined;
    service.login(req).subscribe((r) => (captured = r));

    const httpReq = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
    expect(httpReq.request.method).toBe('POST');
    expect(httpReq.request.body).toEqual(req);
    httpReq.flush(resp);

    expect(captured).toEqual(resp);
    expect(sessionStorage.getItem('sig_access_token')).toBe('token-a');
    expect(sessionStorage.getItem('sig_refresh_token')).toBe('token-r');
    expect(JSON.parse(sessionStorage.getItem('sig_current_user')!)).toEqual(user);
    expect(service.isAuthenticated()).toBe(true);
    expect(service.currentUser()?.email).toBe('admin@sig.local');
  });

  it('login: NUNCA usa localStorage', () => {
    const spyLs = spyOn(localStorage, 'setItem');
    const req: LoginRequest = { email: 'x@y.com', password: 'Demo#2026!' };
    service.login(req).subscribe();
    httpMock.expectOne(`${environment.apiUrl}/auth/login`).flush({ accessToken: 'a', refreshToken: 'r', user: { id: 1, nombre: 'X', apellidos: 'Y', email: 'x@y.com', roles: [] } });
    expect(spyLs).not.toHaveBeenCalled();
  });

  it('refresh: con refreshToken válido renueva tokens', () => {
    sessionStorage.setItem('sig_refresh_token', 'rtoken-old');
    let result: RefreshResponse | null = null;
    service.refresh().subscribe((r) => (result = r));
    const httpReq = httpMock.expectOne(`${environment.apiUrl}/auth/refresh`);
    expect(httpReq.request.method).toBe('POST');
    expect(httpReq.request.body).toEqual({ refreshToken: 'rtoken-old' });
    httpReq.flush({ accessToken: 'new-a', refreshToken: 'new-r' } satisfies RefreshResponse);
    expect((result as RefreshResponse | null)?.accessToken).toBe('new-a');
    expect(sessionStorage.getItem('sig_access_token')).toBe('new-a');
  });

  it('refresh: sin refreshToken devuelve null sin hacer POST', () => {
    let result: RefreshResponse | null = null;
    service.refresh().subscribe((r) => (result = r));
    httpMock.expectNone(`${environment.apiUrl}/auth/refresh`);
    expect(result).toBeNull();
  });

  it('refresh: si el endpoint devuelve 401 limpia la sesión', () => {
    sessionStorage.setItem('sig_access_token', 'a');
    sessionStorage.setItem('sig_refresh_token', 'r');
    sessionStorage.setItem('sig_current_user', JSON.stringify({ id: 1 }));
    service.refresh().subscribe((r) => {
      expect(r).toBeNull();
    });
    httpMock.expectOne(`${environment.apiUrl}/auth/refresh`).flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(sessionStorage.getItem('sig_access_token')).toBeNull();
    expect(sessionStorage.getItem('sig_refresh_token')).toBeNull();
  });

  it('logout: hace POST /auth/logout, limpia sessionStorage y navega a /login', () => {
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate').and.returnValue(Promise.resolve(true));
    sessionStorage.setItem('sig_access_token', 'a');
    sessionStorage.setItem('sig_refresh_token', 'r');
    sessionStorage.setItem('sig_current_user', JSON.stringify({ id: 1 }));

    service.logout().subscribe();
    const httpReq = httpMock.expectOne(`${environment.apiUrl}/auth/logout`);
    expect(httpReq.request.method).toBe('POST');
    httpReq.flush(null, { status: 204, statusText: 'No Content' });

    expect(sessionStorage.getItem('sig_access_token')).toBeNull();
    expect(sessionStorage.getItem('sig_refresh_token')).toBeNull();
    expect(sessionStorage.getItem('sig_current_user')).toBeNull();
    expect(navSpy).toHaveBeenCalledWith(['/login']);
  });

  it('logout: aunque el server falle, igualmente limpia sesión y navega a /login', () => {
    const router = TestBed.inject(Router);
    const navSpy = spyOn(router, 'navigate').and.returnValue(Promise.resolve(true));
    sessionStorage.setItem('sig_access_token', 'a');
    service.logout().subscribe();
    httpMock.expectOne(`${environment.apiUrl}/auth/logout`).flush(null, { status: 500, statusText: 'Server Error' });
    expect(sessionStorage.getItem('sig_access_token')).toBeNull();
    expect(navSpy).toHaveBeenCalledWith(['/login']);
  });

  it('hasRole / hasAnyRole', () => {
    const user: UsuarioBriefDto = { id: 1, nombre: 'X', apellidos: 'Y', email: 'x@y.com', roles: ['Administrator', 'Backoffice'] };
    sessionStorage.setItem('sig_current_user', JSON.stringify(user));
    const auth = TestBed.runInInjectionContext(() => new AuthService());
    expect(auth.hasRole('Administrator' as any)).toBe(true);
    expect(auth.hasRole('Reader' as any)).toBe(false);
    expect(auth.hasAnyRole('Reader' as any, 'Backoffice' as any)).toBe(true);
  });

  it('getAccessToken devuelve null si no hay token guardado', () => {
    expect(service.getAccessToken()).toBeNull();
    sessionStorage.setItem('sig_access_token', 'foo');
    expect(service.getAccessToken()).toBe('foo');
  });
});
