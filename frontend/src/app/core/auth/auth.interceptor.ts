import { HttpInterceptorFn, HttpRequest, HttpHandlerFn, HttpEvent, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, BehaviorSubject, throwError, of } from 'rxjs';
import { catchError, filter, switchMap, take } from 'rxjs/operators';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

const ACCESS_TOKEN_KEY = 'sig_access_token';

// Coordinador de refresh concurrente (un único refresh en vuelo)
let isRefreshing = false;
const refreshSubject = new BehaviorSubject<string | null>(null);

function addAuthHeader(req: HttpRequest<unknown>, token: string): HttpRequest<unknown> {
  return req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
}

function isAuthEndpoint(url: string): boolean {
  return url.includes('/auth/login') || url.includes('/auth/refresh');
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // Solo tocar peticiones a la API. Recursos estáticos / Google Fonts: pasar tal cual.
  if (!req.url.startsWith(environment.apiUrl)) {
    return next(req);
  }

  const auth = inject(AuthService);
  const token = sessionStorage.getItem(ACCESS_TOKEN_KEY);

  // No añadir Bearer a /auth/login ni /auth/refresh
  const initialReq = token && !isAuthEndpoint(req.url) ? addAuthHeader(req, token) : req;

  return next(initialReq).pipe(
    catchError((err: HttpErrorResponse) => {
      // Si 401 y no es endpoint de auth, intentar refresh
      if (err.status === 401 && !isAuthEndpoint(req.url)) {
        return handle401(req, next, auth);
      }
      return throwError(() => err);
    }),
  );
};

function handle401(
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
  auth: AuthService,
): Observable<HttpEvent<unknown>> {
  if (!isRefreshing) {
    isRefreshing = true;
    refreshSubject.next(null);

    return auth.refresh().pipe(
      switchMap((res) => {
        isRefreshing = false;
        if (!res) {
          auth.forceLogout();
          return throwError(() => new HttpErrorResponse({ status: 401 }));
        }
        refreshSubject.next(res.accessToken);
        return next(addAuthHeader(req, res.accessToken));
      }),
      catchError((e) => {
        isRefreshing = false;
        auth.forceLogout();
        return throwError(() => e);
      }),
    );
  }

  // Ya hay un refresh en vuelo: esperar a su resultado
  return refreshSubject.pipe(
    filter((t): t is string => t !== null),
    take(1),
    switchMap((t) => next(addAuthHeader(req, t))),
  );
}
