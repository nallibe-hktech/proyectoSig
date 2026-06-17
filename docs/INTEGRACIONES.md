# INTEGRACIONES — SIG · Sistemas externos

> Documento complementario de `docs/ARQUITECTURA.md` (§5.2 + §9.1). Describe el patrón de integración para cada sistema externo.

---

## Arquitectura de integración

### Patrón general

```
Sistema Externo (API/SFTP)
        │
        ▼
┌─────────────────────────────┐
│  I{Sistema}Client           │ ← Interface en Application/Interfaces/Integrations
│  ┌───────────────────────┐  │
│  │ Http/{Sistema}Client  │  │ ← Implementación real (HttpClient tipado)
│  │ Fake/{Sistema}Fake    │  │ ← Bogus sin conexión real (MVP/Dev)
│  └───────────────────────┘  │
└─────────────────────────────┘
        │
        ▼
┌─────────────────────────────┐
│  SyncService                │ ← Application/Services/SyncService.cs
│  - Idempotencia por hash    │
│  - Upsert en Staging        │
│  - Registro de errores      │
└─────────────────────────────┘
        │
        ▼
┌─────────────────────────────┐
│  Staging{Sistema}           │ ← Tabla en BD (datos raw + hash)
│  - PayloadJson              │
│  - Hash (SHA-256, unique)   │
│  - FlagProcesado            │
└─────────────────────────────┘
        │
        ▼
┌─────────────────────────────┐
│  CalculationEngine          │ ← Lee staging filtrado por período/servicio
│  (Motor de cálculo)         │
└─────────────────────────────┘
```

### Selección de implementación (fake vs real)

```csharp
// Infrastructure/DependencyInjection.cs
if (config.GetValue<bool>("Integrations:UseFake"))
{
    services.AddSingleton<ICeleroClient, CeleroFakeClient>();
    services.AddSingleton<IBizneoClient, BizneoFakeClient>();
    services.AddSingleton<IIntratimeClient, IntratimeFakeClient>();
    services.AddSingleton<IPayHawkClient, PayHawkFakeClient>();
}
else
{
    services.AddHttpClient<ICeleroClient, CeleroClient>(c => c.BaseAddress = new Uri(config["Integrations:Celero:BaseUri"]));
    services.AddHttpClient<IBizneoClient, BizneoClient>(c => c.BaseAddress = new Uri(config["Integrations:Bizneo:BaseUri"]));
    services.AddHttpClient<IIntratimeClient, IntratimeClient>(c => c.BaseAddress = new Uri(config["Integrations:Intratime:BaseUri"]));
    services.AddHttpClient<IPayHawkClient, PayHawkClient>(c => c.BaseAddress = new Uri(config["Integrations:PayHawk:BaseUri"]));
}
```

### Idempotencia

Cada `SyncService.SyncAsync(sistema)` genera un `SHA-256` del `PayloadJson` completo. La BD tiene unique constraint sobre `hash`. Si el hash ya existe, la fila se ignora (no duplicados).

### Retry y error handling

Las implementaciones HTTP reales usarán `HttpClient` tipado con políticas de retry opcionales (Fase 2: Polly). En MVP, timeout de 30s + log de error + `IntegrationException` con código `502`.

---

## 1. Celero One (AlloyDB PostgreSQL)

| Atributo | Valor |
|---|---|
| Sistema origen | Celero One (Google AlloyDB) |
| Tipo conexión | Live PostgreSQL directa |
| Driver | `Npgsql` (mismo que BD local) |
| Puerto | 5432 (estándar PostgreSQL) |
| Dirección | Lectura |
| Periodicidad | Tiempo real / bajo demanda |
| Autenticación | Google Cloud IAM |
| MVP | Fake con Bogus |

### Datos consumidos

| Entidad | Tabla staging | Descripción |
|---|---|---|
| Visitas de campo | `StagingCeleroVisita` | Registro de visitas de GPV a puntos de venta. Cada visita se vincula internamente a un `Service` (`serviceId`) |
| Clientes | Sincronización directa a `Client` | Clientes contratantes |
| Servicios | Sincronización directa a `Service` | Servicios contratados (modelo interno tras refactor Project/Action → Service) |

### Interface (Application)

```csharp
public interface ICeleroClient
{
    Task<IReadOnlyList<CeleroVisitaDto>> GetVisitasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
}

public record CeleroVisitaDto(string VisitaIdExterno, string ResourceNif, string ServiceName, string MissionName, DateOnly Fecha);

// El mapeo interno de cada visita al modelo de SIG se hace contra Service (serviceId), no contra Project/Action.
public record CeleroVisitaUpdateRequest(int? UserId, int? ServiceId, string? Notas, string? EstadoMapeo);
```

### Fake (MVP)

```csharp
// Infrastructure/Integrations/Fake/CeleroFakeClient.cs
// Bogus con Randomizer.Seed = new Random(20260101)
// Genera ~300-600 visitas en el rango de fechas, distribuidas entre los servicios y usuarios de prueba
```

---

## 2. Bizneo (RRHH)

| Atributo | Valor |
|---|---|
| Sistema origen | Bizneo |
| Tipo conexión | REST API |
| Dirección | Lectura |
| Periodicidad | Diario / bajo demanda |
| MVP | Fake con Bogus |

### Datos consumidos

| Entidad | Tabla staging | Descripción |
|---|---|---|
| Empleados | `StagingBizneoEmpleado` | Maestro de empleados (NIF, nombre, departamento) |
| Horas imputadas | `StagingBizneoHora` | Registro de horas por empleado/servicio/fecha |

### Interfaces (Application)

```csharp
public interface IBizneoClient
{
    Task<IReadOnlyList<BizneoEmpleadoDto>> GetEmpleadosAsync(CancellationToken ct);
    Task<IReadOnlyList<BizneoAbsenceDto>> GetAbsencesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
}

public record BizneoEmpleadoDto(string EmpleadoIdExterno, string NIF, string Nombre, string? Departamento);
public record BizneoAbsenceDto(string RegistroIdExterno, int UserId, int ServiceId, DateOnly Fecha, decimal Horas);
```

---

## 3. Intratime (Fichajes)

| Atributo | Valor |
|---|---|
| Sistema origen | Intratime |
| Tipo conexión | REST API |
| Dirección | Lectura |
| Periodicidad | Diario / bajo demanda |
| MVP | Fake con Bogus |

### Datos consumidos

| Entidad | Tabla staging | Descripción |
|---|---|---|
| Fichajes | `StagingIntratimeFichaje` | Entrada/salida por empleado |

### Interface (Application)

```csharp
public interface IIntratimeClient
{
    Task<IReadOnlyList<IntratimeFichajeDto>> GetFichajesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
}

public record IntratimeFichajeDto(string FichajeIdExterno, int UserId, DateTime Entrada, DateTime? Salida);
```

---

## 4. Payhawk (Gastos)

| Atributo | Valor |
|---|---|
| Sistema origen | Payhawk |
| Tipo conexión | REST API |
| Dirección | Lectura |
| Periodicidad | Diario / bajo demanda |
| MVP | Fake con Bogus |

### Datos consumidos

| Entidad | Tabla staging | Descripción |
|---|---|---|
| Gastos | `StagingPayHawkGasto` | Gastos, dietas, kilometraje por empleado/servicio |

### Interface (Application)

```csharp
public interface IPayHawkClient
{
    Task<IReadOnlyList<PayHawkGastoDto>> GetGastosAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
}

public record PayHawkGastoDto(string GastoIdExterno, int UserId, int ServiceId, DateOnly Fecha, decimal Importe, string Categoria);
```

---

## 5. A3 Innuva (Nóminas — salida)

| Atributo | Valor |
|---|---|
| Sistema destino | A3 Innuva |
| Tipo conexión | Fichero (.txt/.csv XML, formato A3) |
| Dirección | Escritura |
| Momento | Al cierre aprobado |
| MVP | Generación de fichero + descarga manual |

### Endpoint

`GET /api/exports/a3-innuva/{closureId}` → genera XML con estructura A3 (formato exacto pendiente de confirmación contractual con el cliente).

### Servicio

```csharp
public interface IExportService
{
    Task<(byte[] Content, string FileName)> ExportA3InnuvaAsync(int closureId, int usuarioId, CancellationToken ct);
}
```

**Flujo:**
1. Verificar `Closure.Estado == Aprobado` (si no → `ClosureNotApprovedException`, 409).
2. Cargar líneas de cierre del período filtradas por servicio.
3. Agrupar por empleado (UserId).
4. Generar XML con nodos: `Nomina → Empleado (NIF, Nombre) → Conceptos (Tipo, Importe) → Totales`.
5. Grabar AuditLog `Action=Export`.
6. Devolver `FileContentResult` con `Content-Disposition: attachment`.

> **⚠ Pendiente:** El formato XML/EDI exacto de A3 Innuva debe definirse contractualmente con el cliente. El endpoint expone la estructura actual como placeholder razonable. Ver `docs/EXPORTS.md`.

---

## 6. A3 ERP (Facturas — salida)

| Atributo | Valor |
|---|---|
| Sistema destino | A3 ERP |
| Tipo conexión | Fichero (.txt/.csv XML, formato A3) |
| Dirección | Escritura |
| Momento | Al cierre aprobado |
| MVP | Generación de fichero + descarga manual |

### Endpoint

`GET /api/exports/a3-erp/{closureId}` → genera XML con estructura A3.

**Flujo:**
1. Verificar `Closure.Estado == Aprobado`.
2. Cargar líneas de cierre de tipo `Factura`.
3. Agrupar por cliente.
4. Generar XML con nodos: `Factura → Cliente (NIF, Nombre) → Lineas (Concepto, Importe) → Totales`.
5. AuditLog + descarga.

> **⚠ Pendiente:** Misma nota que A3 Innuva. Ver `docs/EXPORTS.md`.

---

## 7. Galán, Mediapost, TravelPerk (Fase 2)

Estos sistemas están identificados como fuentes de datos pero quedan fuera del MVP:

| Sistema | Tipo datos | Conexión | Fase estimada |
|---|---|---|---|
| Galán | Field service management | API/SFTP | Fase 2 |
| Mediapost | Logística/distribución | API/SFTP | Fase 2 |
| TravelPerk | Gestión de viajes | REST API | Fase 2 |

En Fase 2 se agregarán sus respectivas interfaces (`IGalanClient`, `IMediapostClient`, `ITravelPerkClient`), tablas staging y registros en `SyncService`.

---

## Configuración appsettings

```json
{
  "Integrations": {
    "UseFake": true,
    "Celero": {
      "BaseUri": "postgresql://host:5432/alloydb",
      "ConnectionString": "Host=...;Database=celero;Username=...;Password=..."
    },
    "Bizneo": {
      "BaseUri": "https://api.bizneo.com/v2",
      "ApiKey": ""
    },
    "Intratime": {
      "BaseUri": "https://api.intratime.es/v1",
      "ApiKey": ""
    },
    "PayHawk": {
      "BaseUri": "https://api.payhawk.com/v1",
      "ApiKey": ""
    }
  }
}
```

En `appsettings.Development.json` → `UseFake: true`. En producción, `UseFake: false` y las API keys vendrán vía Azure Key Vault (Fase 2) o variables de entorno.

---

> **Nota:** Este documento complementa a `docs/ARQUITECTURA.md`. La verdad única vinculante es ARQUITECTURA.md.
