# Cambio visual: login

Tipo: Actualización de diseño UI (sin nuevos RFs)
Archivo: `penpot-design-login.svg`
Acción requerida: Designer y Frontend deben leer el SVG y actualizar
el componente Angular correspondiente para que coincida con el nuevo diseño.

RFs afectados: RF-A01 (login), RF-A02 (logout), RF-A03 (refresh)
Entidades/endpoints: sin cambios.

## Detalles del diseño

- Fondo con gradiente oscuro (#1F4E78 → #163A52 → #0D2A3E) + formas decorativas
- Lado izquierdo: branding "h&k consulting · Plataforma Operativa SIG", feature pills (cierres automatizados, 9 sistemas integrados, Power BI, auditoría)
- Lado derecho: tarjeta blanca 500×720px con sombra
  - Logo SIG circular + "Iniciar Sesión"
  - Campo email corporativo con foco azul
  - Campo contraseña con toggle visibilidad (👁)
  - "Recordar sesión" checkbox + "¿Olvidaste tu contraseña?"
  - Botón "Acceder al Sistema" con gradiente
  - Divider "o continuar con" + botón Azure AD (SSO fase 2)
  - Footer: versión + copyright + roles disponibles
- Sidebar derecha: nombres de integraciones (Celero, Bizneo, Intratime, Payhawk)
