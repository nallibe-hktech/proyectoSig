import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';
import { Rol } from '../../models/enums';

// authGuard — bloquea ruta si no hay sesión.
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAuthenticated()) {
    return true;
  }
  return router.createUrlTree(['/login']);
};

// roleGuard — bloquea ruta si el usuario no tiene ninguno de los roles permitidos.
// Uso: { path: '...', canActivate: [authGuard, roleGuard], data: { roles: ['Administrator'] } }
export const roleGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (!auth.isAuthenticated()) {
    return router.createUrlTree(['/login']);
  }
  const allowed = (route.data?.['roles'] ?? []) as Rol[];
  if (allowed.length === 0) {
    return true;
  }
  if (auth.hasAnyRole(...allowed)) {
    return true;
  }
  // Sin permisos → redirige a dashboard
  return router.createUrlTree(['/dashboard']);
};
