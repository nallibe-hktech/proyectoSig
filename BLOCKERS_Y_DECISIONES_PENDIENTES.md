# BLOCKERS Y DECISIONES PENDIENTES - SIG-ES
**Documento origen:** ANÁLISIS_COMPLETO_SIG_ES_ACTUALIZADO.doc (25 junio 2026)  
**Responsable:** h&k consulting  
**Última actualización:** 25 junio 2026

---

## RESUMEN EJECUTIVO

| Métrica | Valor |
|---------|-------|
| **Total Blockers** | 5 críticos |
| **Preguntas cliente pendientes** | 28 (P1-P28) |
| **Integraciones funcionando** | 7 de 9 |
| **Tests pasando** | 212/212 ✅ |
| **Tarifas confirmadas** | 2 de 17 (~12%) |
| **Implementación** | ~70% backend, 40% frontend |

---

## 1. BLOCKERS CRÍTICOS (Sin respuesta = STOP de desarrollo)

### 🔴 BLOCKER #1: Formato de Ficheros A3
**Impacto:** ALTO - Bloquea implementación A3 Innuva y A3 ERP

| Aspecto | Detalle |
|---------|---------|
| **Qué es** | Estructura exacta de datos para ficheros que se envían a A3 Innuva (nóminas) y A3 ERP (contabilidad) |
| **Estado** | Pendiente confirmación contractual con Wolters Kluwer |
| **Por qué bloquea** | No podemos implementar la escritura (POST) sin conocer la estructura de entrada |
| **Responsable** | Cliente (Eladio) → WK |
| **Acción requerida** | Contactar WK Wolters Kluwer y confirmar formato exacto |
| **Timeline** | Esta semana |
| **Dependencias** | A3 Innuva POST, A3 ERP integración |

---

### 🔴 BLOCKER #2: Tarifas Completas (17 Proyectos)
**Impacto:** CRÍTICO - Bloquea motor de cálculo

| Aspecto | Detalle |
|---------|---------|
| **Qué es** | €/hora, €/visita, €/km para TODOS los 17 proyectos |
| **Confirmadas** | Cosmetica (11,92€/h), Molins (15€/unidad) |
| **Faltantes** | ~15 proyectos (~80%) |
| **Por qué bloquea** | Sin tarifas no hay cálculo automático de nóminas/facturas |
| **Responsable** | Cliente (departamento comercial/RRHH) |
| **Acción requerida** | Enviar tabla Excel: Proyecto | €/hora | €/visita | €/km |
| **Timeline** | ESTA SEMANA (máxima urgencia) |
| **Dependencias** | Motor de cálculo, validaciones, pruebas |

---

### 🔴 BLOCKER #3: Horas Pactadas por Acción
**Impacto:** ALTO - Bloqueador de cálculo para 4 proyectos

| Aspecto | Detalle |
|---------|---------|
| **Qué es** | Cuántas horas están pactadas para cada tipo de acción |
| **Proyectos** | Cosmetica, Morrison, Cheil, Kobo (facturación por horas) |
| **Por qué** | Calcula si hay exceso de horas vs. pactado |
| **Responsable** | Cliente (RRHH/comercial) |
| **Acción requerida** | Documentar horas pactadas por tipo de acción |
| **Ejemplo** | Cosmetica: Acción A=8h, Acción B=6h, etc. |
| **Timeline** | Esta semana |
| **Dependencias** | Cálculo de overwork, facturación cliente |

---

### 🔴 BLOCKER #4: Flujo de Aprobación por Proyecto
**Impacto:** MEDIO - Afecta matriz de aprobación

| Aspecto | Detalle |
|---------|---------|
| **Pregunta** | ¿Todos los proyectos pasan por Gestor → Backoffice → FICO → Dirección? |
| **O** | ¿Hay proyectos que saltan pasos? |
| **Actual** | Flujo genérico implementado (4 pasos) |
| **Por qué** | Algunos proyectos pueden tener aprobación simplificada |
| **Responsable** | Cliente (gerencia/RRHH) |
| **Acción requerida** | Confirmar excepciones por proyecto (si las hay) |
| **Timeline** | Próximas 2 semanas |
| **Dependencias** | Configuración matriz aprobación |

---

### 🔴 BLOCKER #5: Validación de Conceptos por Proyecto
**Impacto:** MEDIO - Afecta lógica de cálculo

| Aspecto | Detalle |
|---------|---------|
| **Pregunta** | ¿Todos los conceptos aplican a todos los proyectos? |
| **O** | ¿Varía según proyecto/cliente? |
| **Ejemplo** | ¿Concepto "Incentivo Visitas" aplica solo a Cosmetica o a todos? |
| **Responsable** | Cliente (finanzas/operaciones) |
| **Acción requerida** | Mapeo: Proyecto → Conceptos permitidos |
| **Timeline** | Próximas 2 semanas |
| **Dependencias** | Lógica cálculo, filtros UI, validaciones backend |

---

## 2. TABLA: TARIFAS FALTANTES

### Tarifas Confirmadas (2/17)
```
Proyecto      | €/hora | €/visita | €/km
------------- | ------ | -------- | ----
Cosmetica     | 11,92  |    -     |  -
Molins        |   -    |   15     |  -
```

### Tarifas Faltantes (15/17)
```
Proyecto      | €/hora | €/visita | €/km | Estado
------------- | ------ | -------- | ---- | ------
Morrison      |   ?    |    ?     |  ?   | ❌
Cheil         |   ?    |    ?     |  ?   | ❌
Kobo          |   ?    |    ?     |  ?   | ❌
TravelPerk    |   ?    |    -     |  ?   | ❌
Celero        |   -    |    ?     |  ?   | ❌
PayHawk       |   -    |    -     |  ?   | ❌
Galan         |   -    |    -     |  ?   | ❌
Mediapost     |   -    |    -     |  ?   | ❌
... (7 más)   |   ?    |    ?     |  ?   | ❌
```

---

## 3. INTEGRACIONES - ESTADO ACTUAL

### ✅ Funcionando (7/9)
| Sistema | Tipo | Dato Real | Auto-Sync | API |
|---------|------|-----------|-----------|-----|
| Galán | Excel | Stock, Entradas, Salidas | Sí | No |
| Mediapost | Excel | Pedidos, Recepciones | Sí | No |
| PayHawk | API OAuth | Gastos | Manual | Sí |
| Bizneo | API REST | Empleados, Ausencias | Manual | Sí |
| Intratime | API REST | Fichajes | Manual | Sí |
| Celero | API REST | Visitas | Manual | Sí |
| SGPV | API REST | Visitas | Manual | Sí |

### ⚠️ Parcial (1/9)
| Sistema | Estado | Qué falta |
|---------|--------|-----------|
| A3 Innuva | Lectura ✅, Escritura ❌ | POST de conceptos calculados |

### ❌ Pendiente (1/9)
| Sistema | Estado | Por qué |
|---------|--------|--------|
| TravelPerk | API conectada | Implementación Fase 2 |

---

## 4. APROBACIONES - ESTADO ACTUAL

### Flujo Implementado
```
Petición de cierre
        ↓
   Gestor revisa
        ↓
  Backoffice revisa
        ↓
    FICO aprueba
        ↓
  Dirección aprueba
        ↓
  Cierre FINAL
```

### Características
- Sistema multi-rol: ✅ Funcionando
- Trazabilidad/Audit log: ✅ Implementada
- Overrides con justificación: ✅ Implementado
- Power BI reporting: ✅ Planeado

### Pendientes de Clarificación
- ¿Excepciones por proyecto? (BLOCKER #4)
- ¿Quiénes son Gestor, Backoffice, FICO en cada proyecto?

---

## 5. FORMATOS A3 - ESTADO ACTUAL

### A3 INNUVA (Nóminas)

| Fase | Estado | Detalles |
|------|--------|----------|
| **Lectura** | ✅ Funcionando | Empresas, empleados, contratos, nóminas, IRPF, salarios |
| **Escritura** | ❌ No implementada | POST de conceptos calculados (bloqueado por BLOCKER #1) |
| **Formato** | 🔴 Pendiente | Confirmación con Wolters Kluwer requerida |
| **Rol** | ✅ Claro | A3 calcula IRPF, SS, deducciones legales |

**Conceptos que enviaremos:**
- Visitas realizadas
- Horas trabajadas
- Gastos (dietas, km)
- Incentivos (con overrides manuales)
- Bonificaciones
- Deducciones

### A3 ERP (Contabilidad)

| Aspecto | Estado | Detalles |
|---------|--------|----------|
| **Integración** | 🔴 NO EMPEZADA | Fase 2 o posterior |
| **Formato** | 📋 Pendiente | En definición con cliente |
| **Rol** | 🟡 Tentativo | Recibe datos para asientos contables + impuestos |
| **Datos** | 📋 Pendiente | Facturación cliente, costes de nómina, etc. |

---

## 6. PREGUNTAS CRÍTICAS DEL CLIENTE (28 total)

### P1-P11: Sobre A3 Innuva
- P1: ¿Confirmáis que A3 Innuva es quien calcula IRPF, SS, deducciones legales?
- P2: ¿Necesitáis que enviemos los conceptos de forma específica?
- ... (9 más)

### P12-P14: TARIFAS Y PARÁMETROS ⚠️ CRÍTICAS
- **P12 — Tarifas completas:** Necesitamos €/hora, €/visita, €/km para 17 proyectos
- **P13 — Horas pactadas:** Cuántas horas pactadas para cada tipo de acción
- **P14 — Extracciones:** Qué retiene (SGSS, IRPF, Anticipos, seguros)

### P15-P27: Sobre estructuras, conceptos, etc.
- P15: Celero datos
- P16: Validación conceptos
- ... (13 más)

### P28: APROBACIONES ⚠️ CRÍTICA
- **P28 — Flujo aprobación:** ¿Gestor → Backoffice → FICO → Dirección? ¿Excepciones?

---

## 7. PRÓXIMOS PASOS POR PRIORIDAD

### 🔴 ESTA SEMANA (Criticidad máxima)
- [ ] Tabla de tarifas (17 proyectos): €/hora, €/visita, €/km
- [ ] Confirmar horas pactadas (Cosmetica, Morrison, Cheil, Kobo)
- [ ] Contactar WK Wolters Kluwer para confirmar formato A3 ficheros

### 🟡 PRÓXIMAS 2 SEMANAS
- [ ] Validación de conceptos por proyecto
- [ ] Confirmar flujo aprobación + excepciones
- [ ] Testing A3 Innuva POST (escritura)
- [ ] Definir estructura A3 ERP

### 🟢 FASE 2 (Próximo sprint)
- [ ] A3 ERP integración
- [ ] TravelPerk implementación completa
- [ ] Pantallas UI finales
- [ ] Azure AD / SSO

---

## 8. RESUMEN POR ÁREA

### Aprobaciones
✅ Flujo multi-rol implementado  
✅ Trazabilidad funcionando  
⚠️ Validación de excepciones por proyecto (BLOCKER #4)

### Tarifas y Parámetros
✅ Estructura definida  
❌ 80% de datos faltantes (BLOCKER #2)  
❌ Horas pactadas sin documentar (BLOCKER #3)

### Formatos A3
⚠️ A3 Innuva: Lectura OK, Escritura falta (BLOCKER #1)  
❌ A3 ERP: No empezada  
🔴 Formato exacto pendiente confirmación (BLOCKER #1)

### Integraciones
✅ 7 de 9 funcionando con datos reales  
⚠️ A3 Innuva: Lectura solo  
❌ TravelPerk: Fase 2

### Testing
✅ 212 tests de integración pasando  
⚠️ Falta testing A3 Innuva POST  
⚠️ Falta testing A3 ERP (no empezada)

---

## 9. MATRIZ RIESGO/IMPACTO

| Blocker | Riesgo | Impacto | Timeline | Propietario |
|---------|--------|--------|----------|------------|
| Formato A3 | Alto | Muy Alto | 3-5 días | Cliente + WK |
| Tarifas (17) | Crítico | Muy Alto | 2-3 días | Cliente (Comercial) |
| Horas pactadas | Alto | Alto | 2-3 días | Cliente (RRHH) |
| Flujo aprobación | Medio | Medio | 5-10 días | Cliente (Gerencia) |
| Conceptos validación | Medio | Medio | 5-10 días | Cliente (Finanzas) |

---

## 10. DOCUMENTACIÓN DE REFERENCIA

| Documento | Ubicación | Descripción |
|-----------|-----------|-------------|
| Análisis completo | `C:\Users\NallibeRiveraGrisale\proyecto SIG ES\ANALISIS_COMPLETO_SIG_ES_ACTUALIZADO.doc` | Documento oficial h&k (25 jun 2026) |
| Resumen | `C:\Projects\workspaces\SIG-es\RESUMEN_DOCUMENTO_ANALISIS_SIG_ES.md` | Versión Markdown del análisis |
| Este documento | `C:\Projects\workspaces\SIG-es\BLOCKERS_Y_DECISIONES_PENDIENTES.md` | Tabla ejecutiva de blockers |
| CLAUDE.md (proyecto) | `C:\Projects\workspaces\SIG-es\CLAUDE.md` | Status del proyecto + roadmap |

---

## Firma

**Preparado por:** h&k consulting  
**Fecha análisis:** 25 junio 2026  
**Última revisión:** 25 junio 2026  
**Responsable de seguimiento:** [Cliente - Eladio]
