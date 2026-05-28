import { Injectable, signal, inject, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, of, BehaviorSubject } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  LoginRequest, LoginResponse, RefreshResponse, UsuarioBriefDto,
} from '../../models/dtos';
import { Rol } from '../../models/enums';

const ACCESS_TOKEN_KEY = 'sig_access_token';
const REFRESH_TOKEN_KEY = 'sig_refresh_token';
const USER_KEY = 'sig_current_user';

// AuthService — gestión de autenticación JWT + refresh + sesión.
// Tokens en sessionStorage (regla CLAUDE.md — NUNCA localStorage).
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  // Estado reactivo del usuario actual
  readonly currentUser = signal<UsuarioBriefDto | null>(this.readStoredUser());
  readonly isAuthenticated = computed(() => this.currentUser() !== null);

  // BehaviorSubject usado por el interceptor para serializar refreshes concurrentes
  private refreshing$ = new BehaviorSubject<string | null>(null);

  login(req: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/login`, req).pipe(
      tap((res) => this.persistSession(res.accessToken, res.refreshToken, res.user)),
    );
  }

  refresh(): Observable<RefreshResponse | null> {
    const refreshToken = sessionStorage.getItem(REFRESH_TOKEN_KEY);
    if (!refreshToken) {
      return of(null);
    }
    return this.http
      .post<RefreshResponse>(`${environment.apiUrl}/auth/refresh`, { refreshToken })
      .pipe(
        tap((res) => {
          sessionStorage.setItem(ACCESS_TOKEN_KEY, res.accessToken);
          sessionStorage.setItem(REFRESH_TOKEN_KEY, res.refreshToken);
        }),
        catchError(() => {
          this.clearSession();
          return of(null);
        }),
      );
  }

  logout(): Observable<void> {
    const refreshToken = sessionStorage.getItem(REFRESH_TOKEN_KEY);
    return this.http
      .post<void>(`${environment.apiUrl}/auth/logout`, { refreshToken })
      .pipe(
        tap(() => this.finalizeLogout()),
        catchError(() => {
          this.finalizeLogout();
          return of(undefined as unknown as void);
        }),
      );
  }

  // Cuando el refresh falla en interceptor, forzar logout
  forceLogout(): void {
    this.finalizeLogout();
  }

  getAccessToken(): string | null {
    return sessionStorage.getItem(ACCESS_TOKEN_KEY);
  }

  hasRole(role: Rol): boolean {
    const u = this.currentUser();
    return !!u && u.roles.includes(role);
  }

  hasAnyRole(...roles: Rol[]): boolean {
    const u = this.currentUser();
    return !!u && u.roles.some((r) => roles.includes(r as Rol));
  }

  private persistSession(access: string, refresh: string, user: UsuarioBriefDto): void {
    sessionStorage.setItem(ACCESS_TOKEN_KEY, access);
    sessionStorage.setItem(REFRESH_TOKEN_KEY, refresh);
    sessionStorage.setItem(USER_KEY, JSON.stringify(user));
    this.currentUser.set(user);
  }

  private clearSession(): void {
    sessionStorage.removeItem(ACCESS_TOKEN_KEY);
    sessionStorage.removeItem(REFRESH_TOKEN_KEY);
    sessionStorage.removeItem(USER_KEY);
    sessionStorage.removeItem('sig_periodo_activo');
    this.currentUser.set(null);
  }

  private finalizeLogout(): void {
    this.clearSession();
    void this.router.navigate(['/login']);
  }

  private readStoredUser(): UsuarioBriefDto | null {
    try {
      const raw = sessionStorage.getItem(USER_KEY);
      return raw ? (JSON.parse(raw) as UsuarioBriefDto) : null;
    } catch {
      return null;
    }
  }
}
