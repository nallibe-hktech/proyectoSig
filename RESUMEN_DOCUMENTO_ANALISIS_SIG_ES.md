# RESUMEN: ANÁLISIS_COMPLETO_SIG_ES_ACTUALIZADO.doc
**Fecha de extracción:** 25 de junio de 2026  
**Documento:** Análisis de Alcance, Flujo de Datos y Dirección del Proyecto SIG-ES

---

## 1. APROBACIONES - Flujo, Pasos, Reglas

### Estado Actual ✅
- **Flujo implementado:** Grupo → FICO con sistema multi-rol
- **Sistema de aprobaciones:** Funcionando con gestión de incidencias y trazabilidad completa
- **Flujo confirmado:** Gestor → Backoffice → FICO → Dirección
- **Característica:** Cualquier usuario que tenga asignada la acción (por ejemplo, Gestor, Backoffice, interlocutor, facilitador) puede aprobar según su rol

### Pendientes de Clarificación
**P28 — Flujo de aprobación:**
- ¿Confirmar: Gestor → Backoffice → FICO → Dirección?
- ¿Todos los proyectos pasan por los 4 pasos o hay proyectos que saltan alguno?

### Decisiones Confirmadas (PARTE 8)
- Flujo de aprobaciones multi-rol: **CORRECTO**
- Trazabilidad y audit log: **CORRECTO**
- Power BI para reporting: **CORRECTO**

---

## 2. TARIFAS Y PARÁMETROS - Qué está Confirmado y Qué Falta

### Datos Confirmados
- Cosmetica: **11,92€/hora**
- Molins: **15€/unidad**
- Algunos proyectos tienen estructura base (€/hora, €/visita, €/km)

### Datos CRÍTICOS FALTANTES
| Elemento | Estado | Impacto |
|----------|--------|--------|
| **Tarifas por proyecto (17 total)** | FALTA 80%+ | Blocker - sin esto no hay cálculo automático |
| **Horas pactadas por acción** | Parcial | Proyectos: Cosmetica, Morrison, Cheil, Kobo |
| **Excepciones por proyecto** | No definidas | Muchos proyectos sin excepciones configuradas |
| **Estructura de comisiones** | Pendiente | Variables por proyecto |
| **Gastos (dietas, KMs)** | Parcial | PayHawk conectado pero parámetros incompletos |

### Preguntas Críticas del Cliente (Blocker Priority)
**P12 — Tarifas completas:**
- Necesitamos las tarifas de TODOS los 17 proyectos
- Formato: €/hora, €/visita, €/km
- **Estado:** Incompleto

**P13 — Horas pactadas:**
- Para proyectos que facturan por horas (Cosmetica, Morrison, Cheil, Kobo)
- ¿Cuántas horas están pactadas para cada tipo de acción?
- **Estado:** No respondido

**P14 — Extracciones y deducciones:**
- Qué se retiene por SGSS, IRPF, Anticipos, seguros
- A nivel de salario bruto vs neto
- **Estado:** Pendiente

---

## 3. FORMATOS DE FICHEROS - Estructura Necesaria

### A3 INNUVA (Nóminas)
**Status:** Integración leída, escritura PENDIENTE

| Aspecto | Descripción | Estado |
|---------|-------------|--------|
| **Lectura** | Empresas, empleados, contratos, nóminas, IRPF | ✅ Funcionando |
| **Escritura (POST)** | Conceptos calculados enviados a A3 Innuva | ⚠️ FALTA IMPLEMENTAR |
| **Formato exacto** | Pendiente confirmación contractual con Wolters Kluwer | 🔴 BLOCKER |
| **Rol de A3 Innuva** | Calcula IRPF, Seguridad Social, deducciones legales | ✅ Claro |

### A3 ERP (Facturas/Contabilidad)
**Status:** No empezada

| Aspecto | Descripción | Estado |
|---------|-------------|--------|
| **Integración** | Generación de asientos contables, gestión de impuestos | 🔴 NO EMPEZADA |
| **Formato esperado** | En definición | 📋 Pendiente |
| **Rol** | Recibe datos de cierre para facturas y contabilidad | 🟡 En diseño |

### FICHEROS DE SALIDA
- **Génesis:** Una vez aprobado el cierre
- **Destino:** 
  - A3 Innuva (nóminas)
  - A3 ERP (facturas/contabilidad)
- **Formato:** Pendiente confirmación contractual
- **Datos:** Conceptos calculados, overrides, incentivos, facturación al cliente

### Estructura de Conceptos
- A nivel de empleado
- Por proyecto
- Con validaciones de salario mínimo, IRPF, SS, etc.
- Soporta override manual con justificación

---

## 4. CAMBIOS DESDE EL ANÁLISIS ANTERIOR

### NUEVO en esta iteración (Ola 2 / Junio 2026)
1. **Sistema de aprobaciones mejorado:** Flujo Grupo → FICO implementado
2. **Gestión de incidencias:** Nueva tabla ClienteIncidencia
3. **Forecast vs Actuals:** Cálculos y visualización
4. **Overrides de incentivos:** Con justificación manual
5. **Sincronización A3 Innuva:** Empresas, empleados, nóminas reales

### CONFIRMADO pero no completado
- Integración Celero: API real, webhooks listos
- TravelPerk: API conectada (Fase 2 completa)
- Paginación: 16+ dashboards

### CAMBIOS EN SCOPE
| Elemento | Antes | Ahora | Razón |
|----------|-------|-------|-------|
| Cierre | Monolítico (ClosureService) | Split (CierreCostes + CierreFacturacion) | Separar responsabilidades |
| Conceptos | Fijos por rol | Configurables por proyecto + servicios | Flexibilidad |
| A3 | Solo lectura | Lectura + escritura (pendiente) | Integración bidireccional |
| Aprobación | Simple | Flujo multi-rol con matriz | Mayor control |

---

## 5. BLOCKERS Y DECISIONES PENDIENTES CON EL CLIENTE

### 🔴 CRITICAL BLOCKERS (Sin respuesta = no avanza)

#### BLOCKER #1: Formato de Ficheros A3
- **Qué es:** Estructura exacta de datos que enviaremos a A3 Innuva y A3 ERP
- **Por qué bloquea:** No podemos implementar la escritura sin conocer el formato
- **Status:** Pendiente confirmación contractual con **Wolters Kluwer**
- **Acción:** Confirmar con Eladio (cliente) o contacto de WK

#### BLOCKER #2: Tarifas Completas (17 proyectos)
- **Qué es:** €/hora, €/visita, €/km para TODOS los proyectos
- **Por qué bloquea:** Sin esto el cálculo automático no funciona
- **Datos tenidos:** Cosmetica (11,92€/h), Molins (15€/unidad) + otros incompletos
- **Proyectos afectados:** 17 en total
- **Acción:** Tabla Excel con tarifas por proyecto

#### BLOCKER #3: Horas Pactadas (por acción/proyecto)
- **Qué es:** Cuántas horas están pactadas para cada tipo de acción
- **Proyectos:** Cosmetica, Morrison, Cheil, Kobo (facturación por horas)
- **Por qué:** Define si hay exceso de horas vs. pactado
- **Acción:** Documentar horas pactadas por tipo de acción

#### BLOCKER #4: Roles y Permisos de Aprobación
- **Pregunta:** ¿Todos los proyectos pasan por Gestor → Backoffice → FICO → Dirección?
- **O:** ¿Hay proyectos que saltan pasos?
- **Impacto:** Configuración de matriz de aprobación
- **Acción:** Confirmar flujo exacto por proyecto (si varía)

#### BLOCKER #5: Conceptos y Validaciones
- **Qué falta:** Validación de conceptos por servicio/proyecto
- **Ejemplo:** ¿Todos los conceptos aplican a todos los proyectos?
- **Impacto:** Lógica de cálculo y filtros de UI
- **Acción:** Mapeo Proyecto → Conceptos permitidos

### 🟡 IMPORTANTES (Fase 2 pero críticos para producción)

#### PENDIENTE #1: A3 Innuva — Integración de escritura
- Lectura: ✅ Implementada
- Escritura: ⚠️ No implementada
- **Acción:** Implementar POST de conceptos calculados

#### PENDIENTE #2: A3 ERP — Integración completa
- Status: No empezada
- **Acción:** Definir formato + implementar

#### PENDIENTE #3: Pantallas de UI
- Dashboard, Proyectos, Acciones, Conceptos, Aprobaciones, Contabilidad
- Status: Parcialmente implementadas
- **Acción:** Completar e iterar con cliente

#### PENDIENTE #4: TravelPerk
- API conectada pero sin implementación completa
- **Acción:** Fase 2 (menos prioritario)

---

## 6. RESUMEN DE DATOS CONFIRMADOS

### ✅ Funcionando en PRODUCCIÓN
| Sistema | Tipo | Status | Datos Reales |
|---------|------|--------|--------------|
| Galán | Excel | ✅ Auto-sync | Stock, Entradas, Salidas, Almacenaje |
| Mediapost | Excel | ✅ Auto-sync | Pedidos, Recepciones |
| PayHawk | API OAuth | ✅ Manual | Gastos reales |
| Bizneo | API REST | ✅ Manual | Empleados, Ausencias |
| Intratime | API REST | ✅ Manual | Fichajes |
| Celero | API REST | ✅ Manual | Visitas (API real) |
| SGPV | API REST | ✅ Manual | Visitas |
| A3 Innuva | API REST | ✅ Lectura | Empresas, empleados, nóminas, IRPF |

### ⚠️ Pendiente de Definición
| Aspecto | Falta | Responsable |
|---------|-------|-------------|
| 17 tarifas por proyecto | Completar tabla | Cliente |
| Horas pactadas | 4 proyectos principales | Cliente |
| Formato A3 ficheros | Confirmación WK | Cliente + h&k |
| A3 ERP estructura | Definir campos | Cliente + h&k |
| Excepciones por proyecto | Documentar | Cliente |

---

## 7. MATRIZ DE DECISIÓN

### Preguntas Críticas Pendientes (PARTE 7)
Total documentadas: **28 preguntas (P1-P28)**

**Prioridad CRITICAL (respuesta requerida antes de continuar):**
- P12 (Tarifas)
- P13 (Horas pactadas)
- P14 (Extracciones)
- P28 (Flujo aprobación)
- FORMATO A3 (sin número específico, pero es BLOCKER)

**Prioridad HIGH (próximas 2 semanas):**
- Celero: Webhooks
- A3 Innuva: Formato exacto de escritura
- Conceptos: Validación por proyecto

**Prioridad MEDIUM (Fase 2):**
- TravelPerk: Implementación completa
- A3 ERP: Integración
- Azure AD/SSO

---

## 8. PRÓXIMOS PASOS INMEDIATOS

1. **Esta semana:**
   - Enviar tabla de tarifas (17 proyectos) ← CRITICIDAD MÁXIMA
   - Confirmar horas pactadas (Cosmetica, Morrison, Cheil, Kobo)
   - Confirmar formato A3 ficheros con WK

2. **Próximas 2 semanas:**
   - Validación de conceptos por proyecto
   - Confirmar flujo de aprobación por proyecto
   - Testing A3 Innuva POST (escritura)

3. **Fase 2 (próximo sprint):**
   - A3 ERP integración
   - TravelPerk completa
   - Pantallas UI finales

---

## 9. DOCUMENTO ORIGINAL

**Estructura del documento:**
- PARTE 1: Qué es la herramienta y qué NO es
- PARTE 2: Flujo completo de datos (7 etapas)
- PARTE 3: Datos de cada sistema (10 integraciones)
- PARTE 4: A3 Innuva vs nuestra herramienta (diferencias)
- PARTE 5: Estado actual (✅ implementado, ⚠️ pendiente)
- PARTE 6: Qué falta (datos del cliente necesarios)
- PARTE 7: Preguntas críticas (28 preguntas blocker)
- PARTE 8: Recomendación de dirección

**Fecha:** 25 de junio de 2026  
**Preparado por:** h&k consulting
