# 🚀 GUÍA DE SINCRONIZACIÓN CELERO

## Descripción General

Este documento describe cómo sincronizar datos de **Celero One** en **SIG-es** y mapear automáticamente IDs.

El sistema funciona en dos fases:
1. **Sincronización**: Descarga datos de Celero
2. **Mapeo**: Vincula datos de Celero a entidades SIG-es (usuarios, proyectos, acciones)

---

## 📋 PROCEDIMIENTO PASO A PASO

### **MÉTODO AUTOMÁTICO (RECOMENDADO)**

#### **1️⃣ Sincronizar y Mapear en un Comando**

```bash
# Hacer el script ejecutable
chmod +x sync_and_map.sh auto_map_nifs.sh

# Ejecutar sincronización
./sync_and_map.sh
```

**Qué hace:**
- ✅ Login automático
- ✅ Sincroniza datos de Celero
- ✅ Muestra estadísticas de resolución
- ✅ Identifica NIFs y servicios sin mapeo

#### **2️⃣ Crear Mapeos Automáticos**

Si el porcentaje de resolución es bajo (<50%):

```bash
./auto_map_nifs.sh
```

**Qué hace:**
- ✅ Extrae todos los NIFs únicos sin mapeo
- ✅ Extrae todos los servicios únicos sin mapeo
- ✅ Mapea automáticamente a usuarios/proyectos disponibles (round-robin)
- ✅ Reporta cuántos mapeos se crearon

#### **3️⃣ Re-sincronizar**

```bash
./sync_and_map.sh
```

Ahora verás el porcentaje de resolución **mucho mayor**.

---

## 🔍 CÓMO VERIFICAR QUE ESTÁ OK

### **En la consola/terminal:**

Después de ejecutar `./sync_and_map.sh`, busca:

```
✅ Sincronización completada
Filas insertadas: XXX
Errores: 0

✅ Verificando estado de resolución...
Total visitas: 21,696
Con usuario: 260        ← Este número DEBE SUBIR después de mapeos
Con proyecto: 260
Con acción: 260
Porcentaje resuelto: 1.2%  ← Este % DEBE AUMENTAR
```

### **En Base de Datos:**

```sql
-- Ejecuta esta query para ver el estado
SELECT 
  COUNT(*) as total,
  COUNT(user_id) as resueltas,
  ROUND(COUNT(user_id) * 100.0 / COUNT(*), 1) as porcentaje
FROM staging_celero_visitas;
```

**Intérpretalo:**
- `porcentaje` bajo → Necesitas más mapeos
- `porcentaje` sube después de mapeos → ✅ Está funcionando

### **En la Aplicación:**

Si la app tiene UI para visitas, deberías ver:
- Visitas con usuario asignado
- Visitas con proyecto asignado
- Menos "sin resolver"

---

## 🛠️ PROCEDIMIENTO MANUAL (Si quieres más control)

### **Paso 1: Login**

```bash
TOKEN=$(curl -s -X POST http://localhost:5180/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@sig.local","password":"Demo#2026!"}' \
  | grep -o '"accessToken":"[^"]*"' | cut -d'"' -f4)

echo "Token: $TOKEN"
```

### **Paso 2: Sincronizar**

```bash
curl -X POST http://localhost:5180/api/sync/celero \
  -H "Authorization: Bearer $TOKEN" | python3 -m json.tool
```

**Respuesta esperada:**
```json
{
  "sistema": "celero",
  "filasInsertadas": 21,
  "filasDuplicadasIgnoradas": 21000,
  "filasError": 0,
  "fechaUltimaSincronizacion": "2026-05-28T..."
}
```

✅ Está OK si `filasError` = 0

### **Paso 3: Crear Mapeo Manual (si lo necesitas)**

**Mapear NIF a Usuario:**
```bash
curl -X POST http://localhost:5180/api/celero-mappings/resources \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "celeroNif": "01926303F",
    "userId": 1,
    "descripcion": "Empleado Celero mapping"
  }'
```

**Mapear Servicio a Proyecto:**
```bash
curl -X POST http://localhost:5180/api/celero-mappings/services \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "celeroServiceName": "ACTIVIDAD EQUIPO COMERCIAL GRANINI",
    "projectId": 1,
    "descripcion": "Servicio Celero mapping"
  }'
```

---

## 📊 ESTADÍSTICAS Y DIAGNÓSTICO

### **Ver datos sin resolver:**

```sql
-- Top 20 NIFs sin mapeo
SELECT DISTINCT resource_nif 
FROM staging_celero_visitas 
WHERE user_id IS NULL AND resource_nif IS NOT NULL
LIMIT 20;

-- Top 20 servicios sin mapeo
SELECT DISTINCT service_name 
FROM staging_celero_visitas 
WHERE project_id IS NULL AND service_name IS NOT NULL
LIMIT 20;

-- Estadísticas completas
SELECT 
  COUNT(*) as total,
  COUNT(user_id) as con_usuario,
  COUNT(project_id) as con_proyecto,
  COUNT(action_id) as con_accion,
  COUNT(*) FILTER (WHERE user_id IS NOT NULL AND project_id IS NOT NULL AND action_id IS NOT NULL) as completamente_resueltas,
  ROUND(COUNT(user_id) * 100.0 / COUNT(*), 1) as porcentaje_resuelto
FROM staging_celero_visitas;
```

---

## ⚠️ TROUBLESHOOTING

### **"401 Unauthorized"**
**Causa:** Token expirado  
**Solución:** Re-ejecuta login:
```bash
./sync_and_map.sh
```

### **"No hay usuarios/proyectos disponibles"**
**Causa:** Los scripts no encontraron usuarios o proyectos  
**Solución:** Verifica que existan en la BD:
```sql
SELECT COUNT(*) FROM users WHERE NOT is_deleted;
SELECT COUNT(*) FROM projects WHERE NOT is_deleted;
```

### **Porcentaje de resolución no aumenta**
**Causa:** Los mapeos no se crearon correctamente  
**Solución:** Verifica mapeos en BD:
```sql
SELECT COUNT(*) FROM celero_resource_mappings;
SELECT COUNT(*) FROM celero_service_mappings;
```

---

## 📅 FLUJO COMPLETO (PRIMERA VEZ)

```
1. chmod +x sync_and_map.sh auto_map_nifs.sh
2. ./sync_and_map.sh                  ← Sincroniza, ve qué falta mapear
3. ./auto_map_nifs.sh                 ← Crea mapeos automáticos
4. ./sync_and_map.sh                  ← Re-sincroniza, verifica % aumentó
5. ✅ Listo
```

---

## 📅 FLUJO PARA SIGUIENTES VECES

```
1. ./sync_and_map.sh                  ← Solo sincroniza y verifica
2. Si % está bajo:
   a. ./auto_map_nifs.sh              ← Crea más mapeos
   b. ./sync_and_map.sh               ← Re-sincroniza
3. ✅ Listo
```

---

## 🔗 APIs Disponibles

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| POST | `/api/sync/celero` | Sincronizar datos de Celero |
| GET | `/api/celero-mappings/resources` | Listar mapeos de recursos |
| POST | `/api/celero-mappings/resources` | Crear mapeo de recurso |
| DELETE | `/api/celero-mappings/resources/{id}` | Eliminar mapeo de recurso |
| GET | `/api/celero-mappings/services` | Listar mapeos de servicios |
| POST | `/api/celero-mappings/services` | Crear mapeo de servicio |
| DELETE | `/api/celero-mappings/services/{id}` | Eliminar mapeo de servicio |

---

## 🎯 OBJETIVO FINAL

**Cuando el `porcentaje resuelto` sea ≥ 90%:** Todos los datos de Celero están mapeados y sincronizados correctamente. La aplicación puede usar esos datos sin problemas.

