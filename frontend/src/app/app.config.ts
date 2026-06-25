import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { MAT_FORM_FIELD_DEFAULT_OPTIONS } from '@angular/material/form-field';
import { MAT_ICON_DEFAULT_OPTIONS } from '@angular/material/icon';
import { registerLocaleData } from '@angular/common';
import localeEs from '@angular/common/locales/es';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { apiInterceptor } from './core/api/api.interceptor';

// Registrar datos de locale 'es' para que los pipes `number:...:'es'` (formato
// español: miles con punto, decimales con coma) funcionen. Sin esto lanzan
// "Missing locale data for the locale 'es'" y el importe se renderiza vacío.
registerLocaleData(localeEs, 'es');

// NOTA: provideAnimationsAsync() está deprecado desde Angular v20.2.
// Se usa provideAnimations() (eager) que sigue siendo la opción correcta
// para Angular Material 21. Ver SUPOSICIONES_CRITICAS.md SUP-L03.
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideAnimations(),
    provideHttpClient(withInterceptors([apiInterceptor, authInterceptor])),
    {
      provide: MAT_FORM_FIELD_DEFAULT_OPTIONS,
      useValue: { appearance: 'outline' },
    },
    {
      provide: MAT_ICON_DEFAULT_OPTIONS,
      useValue: { fontSet: 'material-symbols-outlined' },
    },
  ],
};
