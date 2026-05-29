import { HttpInterceptorFn } from '@angular/common/http';

export const apiInterceptor: HttpInterceptorFn = (req, next) => {
  const apiBaseUrl = 'http://localhost:5180';

  // Solo redirigir peticiones a /api/*
  if (req.url.startsWith('/api/')) {
    const newUrl = apiBaseUrl + req.url;
    req = req.clone({ url: newUrl });
  }

  return next(req);
};
