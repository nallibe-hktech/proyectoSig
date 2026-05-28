import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

describe('authInterceptor', () => {
  let http: HttpClient;
  let mock: HttpTestingController;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideRouter([]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    mock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    mock.verify();
    sessionStorage.clear();
  });

  it('añade Authorization Bearer si hay token y el host es la API', () => {
    sessionStorage.setItem('sig_access_token', 'tok-123');
    http.get(`${environment.apiUrl}/clients`).subscribe();
    const req = mock.expectOne(`${environment.apiUrl}/clients`);
    expect(req.request.headers.get('Authorization')).toBe('Bearer tok-123');
    req.flush({});
  });

  it('NO añade Bearer a /auth/login aunque haya token', () => {
    sessionStorage.setItem('sig_access_token', 'tok-123');
    http.post(`${environment.apiUrl}/auth/login`, {}).subscribe();
    const req = mock.expectOne(`${environment.apiUrl}/auth/login`);
    expect(req.request.headers.get('Authorization')).toBeNull();
    req.flush({});
  });

  it('NO añade Bearer a /auth/refresh aunque haya token', () => {
    sessionStorage.setItem('sig_access_token', 'tok-123');
    http.post(`${environment.apiUrl}/auth/refresh`, {}).subscribe();
    const req = mock.expectOne(`${environment.apiUrl}/auth/refresh`);
    expect(req.request.headers.get('Authorization')).toBeNull();
    req.flush({});
  });

  it('NO toca peticiones a URLs externas (Google Fonts, etc.)', () => {
    http.get('https://fonts.googleapis.com/css?family=Inter').subscribe();
    const req = mock.expectOne('https://fonts.googleapis.com/css?family=Inter');
    expect(req.request.headers.get('Authorization')).toBeNull();
    req.flush({});
  });

  it('no añade header si no hay token', () => {
    http.get(`${environment.apiUrl}/clients`).subscribe();
    const req = mock.expectOne(`${environment.apiUrl}/clients`);
    expect(req.request.headers.get('Authorization')).toBeNull();
    req.flush({});
  });
});
