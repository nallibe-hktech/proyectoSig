# Integración de APIs Reales - Guía de Implementación

**Estado**: ✅ Infraestructura preparada | 🔵 Awaiting credentials

---

## 📋 Resumen

El backend ya está preparado para conectarse a las APIs reales de:
- **Celero One** (clientes, proyectos, acciones, visitas)
- **Bizneo** (horas trabajadas, ausencias)
- **PayHawk** (gastos, dietas, kilometraje)

Actualmente usa **MOCK SERVICES** que retornan datos de prueba. El cambio a APIs reales requiere solo 2 pasos por API.

---

## 🏗️ Arquitectura Implementada

```
┌─────────────────────────────────────────────────────┐
│ IntegracionService (Application.Services)           │
│ - Orquesta sincronizaciones                          │
└────────────┬────────────────────────────────────────┘
             │
    ┌────────┴────────┬──────────────┐
    │                 │              │
    ▼                 ▼              ▼
ICeleroService   IBizneoService  IPayHawkService
    │                 │              │
┌───┴────────────┐    │              │
│   Mock (DEV)   │    │              │
│   Real (PROD)  │    │              │
└────────────────┘    │              │
  
Mocks:     Application/ExternalServices/Mock/
Reales:    Infrastructure/ExternalServices/
```

---

## 🚀 Pasos para Activar Cada API

### PASO 1: Celero One

#### 1. Obtener Credenciales
```
Contacta a: Celero One (comercial)
Recibirás: 
  - API Base URL (ej: https://api.celero.com/v1)
  - API Key / Token
```

#### 2. Configurar Credenciales
En `appsettings.json` (desarrollo) o variables de entorno (producción):
```json
{
  "ExternalServices": {
    "Celero": {
      "Enabled": true,
      "BaseUrl": "https://api.celero.com/v1",
      "ApiKey": "tu-api-key-aqui",
      "Timeout": 30
    }
  }
}
```

#### 3. Activar Implementación Real
En `backend/SigEs.Application/DependencyInjection.cs` (línea ~33):

**ANTES (Mock):**
```csharp
services.AddScoped<ICeleroService, CeleroMockService>();
```

**DESPUÉS (Real):**
```csharp
services.AddScoped<ICeleroService, SigEs.Infrastructure.ExternalServices.Celero.CeleroService>();
```

#### 4. Implementar Métodos API
En `backend/SigEs.Infrastructure/ExternalServices/Celero/CeleroService.cs`:

Reemplazar los `TODO` con llamadas HTTP reales:

```csharp
public async Task<IEnumerable<ClienteIntegracionDto>> SincronizarClientesAsync(CancellationToken ct = default)
{
    // Reemplazar con:
    var response = await _httpClient.GetAsync($"{_apiBaseUrl}/customers", ct);
    var json = await response.Content.ReadAsStringAsync(ct);
    var data = JsonSerializer.Deserialize<List<ClienteApiDto>>(json);
    return data.Select(c => new ClienteIntegracionDto
    {
        IdExterno = c.Id,
        Nombre = c.Name,
        Nif = c.TaxId,
        Email = c.Email,
        Telefono = c.Phone,
        Direccion = c.Address,
        Municipio = c.City,
        Provincia = c.State
    }).ToList();
}
```

---

### PASO 2: Bizneo

#### 1. Obtener Credenciales
```
Contacta a: Bizneo (soporte)
Recibirás:
  - API Base URL
  - API Key
  - Username (para autenticación)
```

#### 2. Configurar en appsettings.json
```json
{
  "ExternalServices": {
    "Bizneo": {
      "Enabled": true,
      "BaseUrl": "https://api.bizneo.com/v1",
      "ApiKey": "tu-api-key",
      "Username": "tu-usuario",
      "Timeout": 30
    }
  }
}
```

#### 3. Actualizar DependencyInjection.cs
```csharp
services.AddScoped<IBizneoService, SigEs.Infrastructure.ExternalServices.Bizneo.BizneoService>();
```

#### 4. Implementar Métodos en BizneoService.cs
```csharp
public async Task<IEnumerable<HoraIntegracionDto>> SincronizarHorasAsync(CancellationToken ct = default)
{
    var dateFrom = DateTime.Now.AddDays(-30);
    var dateTo = DateTime.Now;
    
    var response = await _httpClient.GetAsync(
        $"{_apiBaseUrl}/timesheets/hours?dateFrom={dateFrom:yyyy-MM-dd}&dateTo={dateTo:yyyy-MM-dd}", 
        ct);
    
    // Mapear respuesta a HoraIntegracionDto
}
```

---

### PASO 3: PayHawk

#### 1. Obtener Credenciales
```
Contacta a: PayHawk (integrations@payhawk.com)
Recibirás:
  - API Base URL
  - API Key
```

#### 2. Configurar en appsettings.json
```json
{
  "ExternalServices": {
    "PayHawk": {
      "Enabled": true,
      "BaseUrl": "https://api.payhawk.io/v1",
      "ApiKey": "tu-api-key",
      "Timeout": 30
    }
  }
}
```

#### 3. Actualizar DependencyInjection.cs
```csharp
services.AddScoped<IPayHawkService, SigEs.Infrastructure.ExternalServices.PayHawk.PayHawkService>();
```

#### 4. Implementar Métodos en PayHawkService.cs
Similar a Bizneo, pero para 3 endpoints: `/expenses`, `/diets`, `/mileage`

---

## 📍 Ubicaciones Clave de Archivos

| Componente | Ubicación |
|-----------|-----------|
| Interfaces | `backend/SigEs.Application/Interfaces/Integrations/` |
| Mock Services | `backend/SigEs.Application/ExternalServices/Mock/` |
| Real Services (stubs) | `backend/SigEs.Infrastructure/ExternalServices/{Provider}/` |
| Service Orchestrator | `backend/SigEs.Application/Services/IntegracionService.cs` |
| Dependency Injection | `backend/SigEs.Application/DependencyInjection.cs` |
| Configuration | `backend/SigEs.API/appsettings.json` |

---

## 🧪 Testing Durante Activación

Después de implementar cada API:

```bash
# 1. Verificar que compila
cd backend/SigEs.API
dotnet build

# 2. Probar endpoint de sincronización
POST http://localhost:5000/api/integraciones/celero/sync
Authorization: Bearer {token}

# 3. Verificar respuesta
{
  "status": "success",
  "registrosSincronizados": 3,
  "fechaSincronizacion": "2026-05-26T...",
  "sistema": "Celero"
}
```

---

## ⚠️ Checklist Antes de Producción

- [ ] Credenciales obtenidas de cada proveedor
- [ ] URLs de API validadas (dev/prod)
- [ ] Implementación real completada en cada servicio
- [ ] Tests unitarios para mapeo de DTOs
- [ ] Validación de campos requeridos en respuestas API
- [ ] Manejo de errores de conectividad
- [ ] Rate limiting configurado (si aplica)
- [ ] Logging habilitado para auditoría
- [ ] Build sin warnings ni errores
- [ ] E2E tests con datos reales

---

## 🔄 Rollback a Mocks

Si algo falla, revertir es simple:

```csharp
// Volver a Mocks en DependencyInjection.cs
services.AddScoped<ICeleroService, CeleroMockService>();
services.AddScoped<IBizneoService, BizneoMockService>();
services.AddScoped<IPayHawkService, PayHawkMockService>();
```

---

## 📞 Contactos de Proveedores

| Proveedor | Contacto | Portal |
|-----------|----------|--------|
| **Celero One** | soporte@celero.es | https://app.celero.one |
| **Bizneo** | api@bizneo.com | https://app.bizneo.com |
| **PayHawk** | integrations@payhawk.io | https://app.payhawk.io |

---

## ✅ Próximos Pasos (Post-API)

1. **Persistencia de datos** - Actualizar `SincronizarAsync()` en IntegracionService para guardar en BD
2. **Validación** - Añadir validación de datos sincronizados
3. **Alertas** - Notificar si sincronización falla
4. **Scheduling** - Automatizar sincronización periódica (Hangfire/Quartz)
5. **Power BI** - Conectar dataset de sincronizaciones

---

**Última actualización**: 26 de Mayo, 2026  
**Estado de build**: ✅ 0 Errores | 0 Warnings
