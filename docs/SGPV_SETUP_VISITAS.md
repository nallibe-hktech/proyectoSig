# SGPV Visitas Setup — Guía de Implementación

## Estado Actual

- ✅ **Productos**: Sincronizando correctamente desde `ExportData.php` (ET_Referencias)
- ⏳ **Visitas**: Tab lista en dashboard, pero API retorna vacío
  - Endpoint actual: `ExportData.php?start=YYYY-MM-DD&end=YYYY-MM-DD`
  - Respuesta: Solo ET_Referencias (productos), sin ET_Visitas

## Cambios Necesarios Cuando Hay Nuevo Endpoint

Cuando SGPV proporcione un endpoint que exporte visitas:

### 1. Backend — `HttpClients.cs` (línea 668)

Actualizar el método `GetVisitasAsync()`:

```csharp
// CAMBIO: Actualizar la URL del endpoint
var request = new HttpRequestMessage(HttpMethod.Get, $"ExportVisitas.php?start={desde:yyyy-MM-dd}&end={hasta:yyyy-MM-dd}");
// O parámetro diferente: ?type=visitas, ?format=visitas, etc.

// CAMBIO: Actualizar estructura JSON si es diferente
// Si SGPV devuelve {"export": {"ET_Visitas": [...]}}
// El código ya lo busca — solo asegúrate de que la estructura coincida
```

**Campos esperados en JSON de visita:**
```json
{
  "id": "123",
  "resource_nif": "12345678A",
  "centro_id": "C1",
  "centro_nombre": "Centro Madrid",
  "service_name": "Limpieza",
  "fecha": "2026-06-28",
  "horas_duracion": 8.5
}
```

### 2. Frontend — NO requiere cambios

Todos los cambios se propagan automáticamente:
- `sgpv-dashboard.component.ts` — Ya espera datos en `loadVisitas()`
- `sgpv.service.ts` — Ya llamará a `/api/sgpv/visitas/paginated`
- Backend sync — Ya procesa visitas en caso `"sgpv"`

### 3. Backend — Sync automático

El caso `"sgpv"` en `DashboardCalcSyncAudit.cs` (línea 506) ya:
- Llama a `GetVisitasAsync()`
- Mapea NIF → UserId y ServiceName → ServiceId
- Deduplicación por hash
- Guarda en `StagingSgpvVisita`

**No requiere cambios.**

## Prueba Rápida

Después de actualizar `GetVisitasAsync()`:

```bash
# 1. Backend
cd backend
dotnet build
dotnet run

# 2. Ir al dashboard SGPV
# Hacer clic en "Sincronizar"
# La tab de Visitas debería llenar con datos

# 3. Verificar logs
# Buscar "[SGPV]" en consola para ver cuántas visitas se descargaron
```

## Referencias

- **Backend Sync**: `backend/SIG.Application/Services/DashboardCalcSyncAudit.cs:506`
- **API Client**: `backend/SIG.Infrastructure/Integrations/Http/HttpClients.cs:668`
- **API Endpoint**: `backend/SIG.API/Controllers/SgpvController.cs:23`
- **Frontend Dashboard**: `frontend/src/app/features/sgpv/components/sgpv-dashboard.component.ts`
- **Frontend Service**: `frontend/src/app/features/sgpv/services/sgpv.service.ts`

## Preguntas para SGPV

1. ¿Existe un endpoint que exporte visitas (registros de puntos de venta)?
2. ¿Cuál es la URL/parámetro exacto?
3. ¿Qué campos incluye cada visita?
4. ¿Formato de fecha? (YYYY-MM-DD, timestamp, etc.)
5. ¿Soporta filtros de fecha (start/end)?

---

**Última actualización**: 2026-06-28 | **Status**: Listo para visitas
