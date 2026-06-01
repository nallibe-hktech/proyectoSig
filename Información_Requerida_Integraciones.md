# SIG-es MVP | Información Requerida para Integraciones

**Fecha:** Mayo 2026  
**Proyecto:** SIG-es (Sistema Integral de Gestión)  
**Fase:** Integración de Sistemas Externos  

---

## 1. RESUMEN EJECUTIVO

Hemos completado el **65-70% del MVP** de SIG-es. La plataforma core está lista (Celero, A3 Export, SGPV), y ahora necesitamos integrar **7 sistemas adicionales** para completar la solución.

Para proceder con estas integraciones, requerimos credenciales y configuraciones específicas de cada sistema. Este documento detalla qué información falta.

---

## 2. ESTADO ACTUAL

### ✅ Completado
- **Celero One:** 21,731 visitas sincronizadas
- **A3 Excel Export:** Innuva (.xls) y ERP (.xlsx)
- **SGPV:** Integración completada (falta validar credenciales)
- **Tarifas/Presupuestos:** CRUD backend y frontend
- **E2E Tests:** Infraestructura lista

### 🔄 En Progreso
- Validación de seed data para tests
- Verificación Payhawk + SGPV

### ⏳ Pendiente
- Integraciones: Conectia, Intratime, Bizneo, Travel Perk, Galan + otros

---

## 3. INFORMACIÓN REQUERIDA POR SISTEMA

### 3.1 PAYHAWK
**Estado:** Credenciales parciales recibidas  
**Información Actual:**
- Email: `plataforma.sig@sigespana.es`
- Contraseña: `eHFhsa82*`

**Información Faltante:**
- [ ] API Key / Access Token (si es diferente de usuario/contraseña)
- [ ] Ambiente (desarrollo/producción)
- [ ] ID de cuenta / Workspace ID
- [ ] Scopes/permisos requeridos

---

### 3.2 SGPV
**Estado:** Credenciales parciales recibidas  
**Información Actual:**
- Usuario: `sig`
- URL: `https://sig.sgpv.es/export/ExportData.php`
- Tipo de acceso: Login + descarga JSON

**Información Faltante:**
- [ ] Contraseña (la información anterior está incompleta)
- [ ] Formato esperado del JSON
- [ ] Campos principales que contiene
- [ ] Frecuencia de actualización
- [ ] ¿Necesita autenticación por sesión o hay API key?

---

### 3.3 CONECTIA (Wolters Kluwer)
**Estado:** Solo documentación disponible  
**URL Docs:** https://a3responde.wolterskluwer.com/es/s/article/conectia-que-es-y-como-funciona

**Información Requerida:**
- [ ] API Key
- [ ] Endpoint base (dev/prod)
- [ ] Credenciales de autenticación
- [ ] Documentación técnica de APIs: https://a3developers.wolterskluwer.es/apis
- [ ] ¿Qué datos se sincronizarán? (gastos, facturas, etc.)
- [ ] Frecuencia de sincronización

---

### 3.4 INTRATIME
**Estado:** Solo documentación disponible  
**URL Docs:** https://apidocs.intratime.es/

**Información Requerida:**
- [ ] API Key
- [ ] Tenant ID / Company ID
- [ ] Credenciales de desarrollador
- [ ] Ambiente (dev/producción)
- [ ] ¿Sincronización de qué datos? (horas, proyectos, empleados, etc.)
- [ ] Rate limits
- [ ] Horario de disponibilidad de API

---

### 3.5 BIZNEO
**Estado:** Solo documentación disponible  
**URL Docs:** https://apitracker.io/a/bizneo

**Información Requerida:**
- [ ] API Key / Access Token
- [ ] Client ID / Company ID
- [ ] Credenciales de autenticación
- [ ] Endpoint de API (desarrollo/producción)
- [ ] ¿Qué datos se sincronizarán?
- [ ] Tipo de autenticación (Bearer token, OAuth, básica, etc.)

---

### 3.6 TRAVEL PERK
**Estado:** Solo documentación disponible  
**URL Docs:** https://app.travelperk.com/api/v2

**Información Requerida:**
- [ ] API Key / Bearer Token
- [ ] Workspace ID
- [ ] Account ID
- [ ] Ambiente (sandbox/producción para testing)
- [ ] Scopes requeridos
- [ ] ¿Datos a sincronizar? (gastos de viaje, reservas, etc.)

---

### 3.7 GALAN
**Estado:** Recibe ficheros diarios (sin integración API)

**Información Requerida:**
- [ ] Tipo de conexión: FTP / SFTP / HTTP / Otro
- [ ] Servidor / Hostname / IP
- [ ] Puerto
- [ ] Usuario
- [ ] Contraseña / Clave privada (si SFTP)
- [ ] Ruta de carpeta de origen
- [ ] Formato de archivos (CSV, JSON, XML, Excel, etc.)
- [ ] Campos que contiene cada archivo
- [ ] Nombre/patrón de archivos (ej: `galan_YYYY-MM-DD.csv`)
- [ ] Horario de disponibilidad de archivos
- [ ] ¿Hay que confirmar descarga?

---

### 3.8 OTROS SISTEMAS
Para cualquier otro sistema no listado:

**Información Requerida (genérica):**
- [ ] Nombre del sistema
- [ ] Tipo de integración (API REST, SFTP, XML, Webhook, etc.)
- [ ] Documentación técnica
- [ ] API Key / Credenciales
- [ ] Endpoint/URL de conexión
- [ ] Ambiente (dev/prod)
- [ ] ¿Qué datos se sincronizarán?
- [ ] Frecuencia de sincronización

---

## 4. MATRIZ DE PRIORIDADES

| Sistema | Documentación | Credenciales | Prioridad | Estimado |
|---------|:-------------:|:------------:|:---------:|:--------:|
| Payhawk | ✅ | 🟡 Parcial | ALTA | 3-5 días |
| SGPV | ✅ | 🟡 Parcial | ALTA | 3-5 días |
| Conectia | ✅ | ❌ Falta | MEDIA | 1 semana |
| Intratime | ✅ | ❌ Falta | MEDIA | 1 semana |
| Bizneo | ✅ | ❌ Falta | MEDIA | 1 semana |
| Travel Perk | ✅ | ❌ Falta | BAJA | 1 semana |
| Galan | ✅ | ❌ Falta | MEDIA | 3-5 días |

---

## 5. CHECKLIST DE ENTREGA

### Para proceder, necesitamos que el cliente proporcione:

**URGENTE (próximos 3 días):**
- [ ] Contraseña completa de SGPV
- [ ] API Key o credenciales de Payhawk adicionales (si aplica)

**IMPORTANTE (próxima semana):**
- [ ] Credenciales de Conectia
- [ ] Credenciales de Intratime
- [ ] Credenciales de Galan (FTP/SFTP)
- [ ] Configuración de Travel Perk

**COMPLEMENTARIO:**
- [ ] Credenciales de Bizneo
- [ ] Documentación interna de formatos de datos esperados
- [ ] Información de ambientes separados (dev/prod)
- [ ] Rate limits y restricciones de uso

---

## 6. PRÓXIMOS PASOS

### Fase 1 (Semana 1):
1. Cliente proporciona información faltante
2. Validamos conexión con Payhawk + SGPV
3. Completamos E2E tests

### Fase 2 (Semana 2-3):
1. Iniciar integraciones por prioridad
2. Comenzar con sistemas que ya tenemos credenciales (Payhawk, SGPV)
3. Parallelizar otras integraciones conforme llegue información

### Fase 3 (Semana 4+):
1. Integración de sistemas faltantes
2. Testing y validación
3. Deployment a producción

---

## 7. CONTACTO

**Para enviar la información:**

Por favor, envíe toda la información solicitada a través de un canal seguro (email encriptado o similar), especificando claramente:

```
SISTEMA: [nombre]
TIPO DE CREDENCIAL: [API Key / Usuario+Contraseña / Certificado / Otro]
AMBIENTE: [Desarrollo / Producción / Ambos]
INFORMACIÓN:
- [campo 1]: [valor]
- [campo 2]: [valor]
```

---

## 8. NOTAS IMPORTANTES

- ⚠️ **Seguridad:** No compartir credenciales en emails sin encriptar
- ⚠️ **Validación:** Verificaremos que cada credencial funciona en environment de desarrollo primero
- ⚠️ **Rate Limits:** Asegúrense de tener suficientes cuotas/límites para testing
- ⚠️ **Horarios:** Algunos sistemas pueden tener horarios de disponibilidad específicos

---

**Documento preparado:** Equipo de Desarrollo SIG-es  
**Válido para:** Período de integración MVP  
**Próxima revisión:** Después de recibir credenciales
