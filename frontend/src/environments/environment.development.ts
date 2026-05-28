// Environment de desarrollo
// NOTA: La URL de API usa HTTP (no HTTPS) en Development.
// Justificación: HTTPS en .NET dev requiere certificado aceptado por el navegador;
// HTTP en dev funciona siempre. El puerto 5180 está fijado en launchSettings.json
// del backend (ver SUPOSICIONES_CRITICAS.md SUP-A01).
// El Desarrollador Backend debe confirmar el puerto real en PROGRESO_BACKEND.md
// (campo BACKEND_PORT_HTTP) si difiere del placeholder.
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5180/api',
  showDemoCredentials: true,
};
