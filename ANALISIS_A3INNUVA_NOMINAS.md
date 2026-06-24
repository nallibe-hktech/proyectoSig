# Análisis de Archivos Excel - A3 INNUVA Nóminas

**Fecha de análisis:** 2026-06-24  
**Archivos analizados:**
1. `CierresIntegralesSIG.xlsx` — Especificación integral de cierres y sincronización de nóminas
2. `ejemplo ARCHIVO A3NOM.xls` — Ejemplo de archivo de nómina descargado de Wolters Kluwer A3 Innuva

---

## Resumen Ejecutivo

### ¿Qué es un cierre integral de nóminas?

Un **cierre integral de nóminas A3 INNUVA** es el proceso de consolidación y sincronización de datos de múltiples sistemas externos (Wolters Kluwer, Galán, Celero, etc.) en una nómina unificada para SIG ES. El proceso:

1. **Extrae** datos de 7 sistemas externos
2. **Transforma** e integra los datos según lógicas de pago y facturación específicas por proyecto
3. **Calcula** conceptos, salarios, deducciones y facturaciones
4. **Valida** usando roles y permisos (Administrador, FICO, RRHH, Dirección, etc.)
5. **Cierra** el período generando asientos de nómina (en formato A3 Innuva)
6. **Exporta** para integración contable en el ERP

---

## 1. Análisis: CierresIntegralesSIG.xlsx

**Dimensiones:** 21 hojas | 148 filas máx | 66 columnas máx

Este archivo es el documento de **especificación funcional y técnica** de toda la integración de nóminas. Actúa como diccionario de datos y guía de implementación.

### 1.1 Hojas Principales (Índice)

| # | Hoja | Propósito | Datos |
|---|------|----------|-------|
| 1 | **CRONOGRAMA** | Plan de implementación del proyecto SIG ES | Fases, tareas, responsables, duración (135 filas) |
| 2 | **ESTRUCTURA** | Roles y responsables funcionales | Equipos de trabajo, asignaciones |
| 3 | **CONEXIONES** | Credenciales y endpoints de integración | Office 365, APIs, usuario genérico |
| 4 | **Glosario** | Términos y conceptos clave | Definición de CONCEPTO, CÁLCULO, etc. |
| 5 | **Roles** | Permisos por rol de usuario | Admin, Dirección, FICO, RRHH, Facilitador |
| 6 | **Flujo** | Diagrama de flujo visual (28 x 24 celdas) | Proceso de nómina paso a paso |

### 1.2 Hojas de Integración (Sistemas Externos)

Cada sistema externo tiene una hoja de especificación con campos clave y ejemplos:

#### **Hoja 7: Innuva** (Wolters Kluwer - NÓMINAS)
**Fuente de datos:** Descarga API de A3 Innuva  
**Archivos:**
- `MARZO_2026_LISTADO PERSONAL CAMPO QUE COBRA A DIA 30 POR CECO.xlsx`
- `MARZO_2026_INFORME PRELIMINAR NOMINAS CECOS.xlsx`
- `NSPP.xlsx`

**Campos clave:**
```
1. Fecha de carga         → Cuándo se descargan los datos
2. Ceco                   → Centro de coste contable
3. Código Empleado        → ID de Innuva
4. Nombre Empleado        → Full name
5. Estado Contrato        → Activo/Inactivo/Suspendido
6. Tipo de Contrato       → Contrato indefinido, temporal, etc.
7. Salario Bruto          → Base de cálculo
8. Seguridad Social       → Aportación patronal
9. IRPF                   → Retención fiscal
10. Importe Líquido       → Neto percibido
```

#### **Hoja 8: Bizneo** (Recursos Humanos)
**Fuente:** API REST de gestión de RRHH  
**Propósito:** Validación de empleados, estructura organizativa, control de horas

**Campos clave:**
```
1. ID Empleado             → 17355675 (ID Bizneo)
2. Empleado                → Diego Casagrande Spallina
3. ID Externo              → Enlace con sistemas terceros
4. Estructura              → Departamento, centro de coste
5. Puesto                  → Rol o posición
6. Estado                  → Activo/Inactivo
7. Horas Trabajadas        → Por día/semana
```

#### **Hoja 9: Intratime** (Fichajes - Control de Horarios)
**Fuente:** API REST de fichajes  
**Propósito:** Entrada/salida de empleados de campo, cálculo de horas trabajadas

**Campos clave (Métodos de API GET USERS):**
```
1. USER_ID                 → 6326
2. USER_COMPANY            → PI9981
3. USER_NAME               → Pruebas Intratime
4. Entry Time              → Hora de entrada
5. Exit Time               → Hora de salida
6. Hours Worked            → Horas calculadas
7. Overtime                → Horas extras
```

#### **Hoja 10: PayHawk** (Gastos de Viaje)
**Fuente:** API REST de gastos  
**Propósito:** Conceptos de gastos reembolsables (dietas, transportes, tickets)

**Campos clave:**
```
1. Expense ID              → 27551 (Clave primaria)
2. Created Date            → Fecha de gasto
3. Settlement Date         → Fecha de liquidación
4. Amount                  → Importe del gasto
5. Category                → Dieta, km, ticket, hotel
6. Employee                → Quién genera el gasto
7. Project                 → Proyecto asociado
```

#### **Hoja 11: CeleroOne** (Visitas y Rendimiento)
**Fuente:** API REST de campo  
**Propósito:** Visitas realizadas, rendimiento, producción por cliente

**Campos clave (divididos en áreas):**
```
Clientes:
  - clientId               → Código ÚNICO
  - clientCreatedAt        → Fecha creación
  - clientGln              → Código GLN

Departamentos:
  - departmentId           → Código ÚNICO
  - departmentCreatedAt    → Fecha creación

Centros de Coste:
  - centerId               → Código ÚNICO
  - centerName             → Nombre visible
  - centerCreatedAt        → Fecha creación

Visitas:
  - visitId                → Código de visita
  - visitDate              → Fecha
  - visitedBy              → GPV que realiza
  - clientId               → Cliente visitado
  - shelvesUpdated         → Lineales actualizados
```

#### **Hoja 12: TravelPerk** (Viajes Corporativos)
**Fuente:** API REST  
**Propósito:** Vuelos, alojamientos, transportes para empleados en desplazamiento

**Campos clave:**
```
1. Traveler First Name     → Carina
2. Traveler Last Name      → Eiris Villaverde
3. Traveler Email          → carina.eiris@sigespana.es
4. Booking Date            → Fecha de reserva
5. Trip Date               → Fecha de viaje
6. Flight/Hotel            → Detalles del viaje
7. Cost Amount             → Importe
8. Reimbursement Status    → Pagado/Pendiente
```

**Nota:** Falta campo DNI/documento de identidad (identificación pendiente)

#### **Hoja 13: SGPV** (Visitas Geolocalizadas)
**Fuente:** API REST de visitas  
**Propósito:** Validación de visitas, control de productividad

**Campos clave:**
```
Visitas:
  - idVisita               → 188
  - idGpv                  → 9 (recurso)
  - GPV                    → Jose Carlos Plaza Velasco
  - idCliente              → 1

Centros:
  - idCentro               → Código ÚNICO
  - CodigoCentro           → Código legible
  - CodigoNielsen          → Código de cliente externo
  - Frecuencia             → Visitado cada X días
```

### 1.3 Hojas de Definición (Conceptos y Lógicas)

#### **Hoja 14: Entidades** (Diccionario de datos)
Define las entidades principales del sistema y sus relaciones:

```
1. Usuario
   - Atributos: id, NIF, Nombre, Apellidos, Mail, Estado
   - Relaciones: Rol, Departamento, Proyectos
   - Regla: Sin Rol → no puede visualizar ni actuar

2. Rol
   - Atributos: Nombre, Permisos
   - Relaciones: Asignado a varios Usuarios
   - Permisos: Global, Pagos, Facturaciones, Auditorías, Usuarios, Roles

3. Recurso
   - Atributos: resourceId, NIF, Nombre, Apellidos, Estado
   - Relaciones: Proyectos, Contratos
   - Nota: Empleado de campo, NO accede a la aplicación

4. Contrato
   - Atributos: contractId, resourceId, projectId, startDate, endDate, status
   - Relaciones: Concepto, Ciclos de pago
   - Regla: Un recurso puede tener múltiples contratos

5. Concepto
   - Atributos: conceptId, name, type (Pago/Facturación)
   - Relaciones: Cálculos, Proyectos
   - Nota: Cálculos personalizados por franja de tiempo
```

#### **Hoja 15: Hoja1** (Log de cambios o auditoría)
Registro de cambios con timestamps:
```
Lucia Pardo           | 2026-05-26 | 15:30:00
Tomás Martín          | 2026-05-27 | 13:30:00
Esmeralda Rodríguez   | 2026-05-27 | 15:30:00
Mirella Julca         | 2026-05-28 | 13:30:00
Silvia Garzón Bahíllo | 2026-05-28 | 15:30:00
```

#### **Hoja 16: Pagos - Facturación**
Define los **TIPOS DE CONCEPTO** (cálculos) y **FILTROS** aplicables:

**Tipos de Concepto:**
```
1. Cantidad fija mensual
   → Pago fijo independiente de actividad

2. Conteo de Visitas x Cantidad fija
   → Nº de visitas × Tarifa unitaria

3. Conteo de días con actividad (Visitas) x Cantidad fija
   → Nº de días trabajados × Tarifa diaria

4. Tarifa por hora
   → Horas × €/hora

5. Tarifa por visita + Incentivo
   → (Visitas × Base) + Bonus si se alcanza meta

6. Gastos variables (Payhawk, km, dietas)
   → Importes reales de gastos
```

**Filtros aplicables:**
```
Cantidad mínima:  Si resultado < X → devuelve X
Cantidad máxima:  Si resultado > X → devuelve X
Rendimiento mín:  Si < X → devuelve 0
Fechas:          Período de aplicación (desde-hasta)
Proyectos:       Aplicar solo a proyectos específicos
```

#### **Hoja 17-19: Detalles de Pagos y Facturaciones**

**Hoja 17: Detalles PagosFact_IL** (Casos de uso específicos)
13 columnas con casos reales:
```
1. Id
2. Hora inicio / finalización
3. Nombre del recurso
4. Cliente
5. Proyecto/Campaña
6. Cálculo de PAGO (explicado)
   → ¿Por visita, hora o mixto?
   → Variables que determinan el pago

7. Cálculo de FACTURACIÓN (operativa)
   → ¿Igual que pago o diferente?
   → Márgenes y markups

8. Logística y conceptos asociados
   → Almacén, FEE, transporte

9. Situaciones no estándar
   → 2ª/3ª visitas, excepciones

10. Conceptos no estándar en pagos
    → Adelantos, pagos fuera de ciclo, embargos

11. Conceptos no estándar en facturación
    → Descuentos, bonificaciones

12. Documentación adjunta
    → Emails, screenshots, PDF
```

**Hoja 18: CuadroDetallesPagosFact** (Matrix de cálculos)
Resume la lógica para cada cliente/proyecto:

```
Columnas principales:
1. Cliente
2. Proyecto/Campaña
3. MotorPago                    → MENSUAL, UNIDAD, VISITA, MIXTO, etc.
4. Pago_Base                    → Descripción del cálculo base
5. Pago_TarifaHora_€            → Si aplica (ej: 11.92€/h)
6. Pago_Parámetros necesarios   → Detalles del cálculo
7. MotorFacturación             → IGUAL_PAGO, MARGEN, CUSTOM, etc.
8. Fact_Base                    → Descripción del cálculo
9. Fact_Parámetros necesarios
10. Logística_Modelo            → Modelo de transporte/almacén
11. Logística_Parámetros        → Detalles logísticos
12. Excepciones_Modelo          → Flags de casos especiales
13. Pendientes_para_parametrizar → Items TODO
```

**Ejemplos de casos reales:**
```
ECRES GRANINI (GPV GRANINI):
  - Motor: MENSUAL
  - Pago: Fijo mensual + variables (dietas/km/tickets) + objetivos
  - Facturación: Igual que pago

jde (GPV jde):
  - Motor: MENSUAL
  - Pago: Fijo mensual + variables
  - Facturación: Igual que pago

Inpost (Prueba Piloto):
  - Motor: MENSUAL_IMPUTADO
  - Pago: Nómina imputada por horas/porción (sistema externo)

molins-propamsa (Implantaciones/Almacenamiento):
  - Motor: UNIDAD/TRAMOS
  - Pago: 15€ por unidad o tramos (1ª hora/resto)
  - Ejemplo: 1ª hora 15€, resto 11.92€/h

DYSON (Aspiración Q12026):
  - Motor: VISITA/MIXTO
  - Pago: Tarifa por visita + tiempo previsto según tarea
```

#### **Hoja 20: Conceptos x Proyecto**
Mapeo simple de qué concepto aplica en cada proyecto:

```
Concepto                                    | PAGO                        | FACTURACIÓN
─────────────────────────────────────────────────────────────────────────────────
Cuota/Dietas por día trabajado              | Granini, JDE, Dyson, etc.   | Granini, JDE, Morrison
Cuota fija mensual                          | Molins                      | Granini, JDE, Molins, Daikin...
Cuota fija mensual por Recurso              | —                           | Granini, JDE
Tarifa por hora (horas extras, viajes, etc.)| Apple, Cosmética            | Apple, Cosmética
```

#### **Hoja 21: Lógicas Pagos-Facturación**
Tabla completa (20 filas x 14 columnas) que resume toda la lógica de cálculo:

**Proyectos documentados:**
```
Amex       → Cuota por día trabajado
Apple BA   → Incentivos mensuales + Salario fijo
Apple RST  → Cuota por visita + Salario fijo (11.92€/h)
Cosmética  → Cuota por visita (según mueble/tipo) + Cuota por hora (11.92€)
Coty Impl  → Cuota por visita + Cuota por hora
```

---

## 2. Análisis: ejemplo ARCHIVO A3NOM.xls

**Dimensiones:** 8 filas | 29 columnas  
**Formato:** Descarga nativa de Wolters Kluwer A3 Innuva (versión de nómina mensual)

Este es el archivo **de salida** que genera A3 Innuva una vez procesada la nómina. Es el asiento contable que SIG ES debe sincronizar e integrar en su sistema.

### 2.1 Estructura del Archivo

#### **Encabezados (Fila 8 - Donde están los datos)**

```
Col  1-8:    [VACÍO]
Col  9:      Imputación              → Código de imputación contable
Col 10:      Tipo de paga            → Mensual, extra, complemento
Col 11:      Importe bruto           → Base imponible total
Col 12:      Seguridad Social (SS) trabajador     → Cotización del empleado
Col 13:      Tributación IRPF total  → Retención fiscal a cuenta
Col 14:      Importe líquido         → Neto a percibir por empleado
Col 15:      Total SS de empresa     → Cotización patronal (gasto de empresa)
Col 16:      Descuento embargo salarial          → Si procede
Col 17:      Anticipo                → Anticipo de nómina
Col 18:      Descuento préstamo      → Descuentos por préstamos
Col 19:      Prorrata pagas extras   → Acumulado de pagas extras
Col 20:      KM                      → Reembolso de kilómetros
Col 21:      SUPLIDOS                → Conceptos de terceros
Col 22:      Exoneración líquida Empresa Total   → Exención fiscal de empresa
Col 23:      Ajuste Nómina           → Correcciones manuales
Col 24:      Salario en Especie      → Retribución no monetaria
Col 25:      Descuento conceptos especie        → Deducciones en especie
Col 26:      Descuento por absentismo           → Faltas/ausencias
Col 27:      Líquido sumar nómina anterior      → Acumulado anterior
Col 28:      Líquido sumar nómina posterior     → Acumulado posterior
Col 29:      Descuento nóminas negativas        → Si la nómina es negativa
```

### 2.2 Metadatos (Filas 1-7)

```
Fila 1:      [VACÍO]

Fila 2:      [VACÍO]

Fila 3:      
  Col B:     "Paga Mensual"          → Tipo de período
  Col L:     "Asiento de nómina PRUEBA"  → Tipo de asiento (PRUEBA/REAL)

Fila 4:      
  Col B:     "Del 01/05/2026 al 31/05/2026"    → Período de nómina
  Col AC:    "27/05/2026"                       → Fecha de cierre/asiento

Fila 5:      
  Col B:     "Empresa: 1 - SERVICE INNOVATION GROUP ESPAÑA SERVICIO"  → Empresa destino

Fila 6-7:    [VACÍO]

Fila 8:      [ENCABEZADOS]
```

### 2.3 Interpretación de Datos

El archivo ejemplo contiene **metadatos únicamente**, sin registros de empleados. Pero la estructura indica:

| Concepto | Cálculo en SIG | Fuente |
|----------|----------------|--------|
| **Importe Bruto** | Suma de: Salario base (Innuva) + Conceptos de Pago | Innuva, Celero, Intratime, PayHawk |
| **SS Trabajador** | Aplicar tabla de cotización según base | Innuva (ya calculado) |
| **IRPF** | Aplicar tabla de retención según base | Innuva (ya calculado) |
| **Importe Líquido** | Bruto - SS - IRPF - Descuentos | Innuva (cálculo final) |
| **SS Empresa** | Aplicar tabla de cotización patronal | Innuva (ya calculado) |
| **Embargo** | Si existe orden judicial | Sistema externo de embargos |
| **Anticipo** | Adelanto concedido al empleado | Control interno SIG |
| **Descuento Préstamo** | Por crédito concedido | Control interno SIG |
| **Prorrata Pagas Extras** | 2 pagas extras / 12 meses | Cálculo automático |
| **KM** | Reembolso a tarifa legal | Intratime + PayHawk (gastos) |
| **SUPLIDOS** | Conceptos de terceros (hoteles, etc.) | PayHawk + TravelPerk |
| **Exoneración Empresa** | Gastos exentos de cotización | Normativa fiscal |
| **Ajuste** | Correcciones manuales | FICO/RRHH |
| **Salario en Especie** | Valor de beneficios no monetarios | Políticas de empresa |
| **Absentismo** | Descuento por faltas injustificadas | Bizneo (control horario) |
| **Acumulados** | Totales mes anterior/posterior | Nómina histórica |
| **Nóminas Negativas** | Si empleado debe más de lo que se le paga | Caso excepcional |

---

## 3. Flujo de Sincronización (Data Pipeline)

```
SISTEMAS EXTERNOS                 SIG ES BACKEND                    WOLTERS KLUWER
────────────────────────────────────────────────────────────────────────────────────

A3 INNUVA        ──────────────────► [CalculationEngine]  ──────────────► A3NOM.xls
(Salario base,   │                  (Cálculos de pago)      [A3InnuvaNominasService]
 descuentos)     │                  (Conceptos)              (Exportación)
                 │
Bizneo           ├──────────────────┤ [Validation]
(Empleados,      │                  ├─ Validar empleado existe
 estructura)     │                  ├─ Validar contrato activo
                 │                  ├─ Validar período correcto
Intratime        ├──────────────────┤ [Aggregation]
(Horas)          │                  ├─ Sumar horas por empleado
                 │                  ├─ Detectar horas extras
PayHawk          ├──────────────────┤ [Gastos]
(Gastos,         │                  ├─ Gastos reembolsables
 dietas)         │                  ├─ Cálculo de km
                 │                  ├─ Dietas y suplidos
Celero           ├──────────────────┤ [Performance]
(Visitas,        │                  ├─ Cálculo de visitas
 producción)     │                  ├─ Aplicar incentivos
                 │                  ├─ Validar mínimos
TravelPerk       ├──────────────────┤ [Travel]
(Viajes)         │                  ├─ Viajes corporativos
                 │                  ├─ Tickets y alojamientos
SGPV             ├──────────────────┤ [Geolocation]
(Validación)     │                  ├─ Validar visitas
                 │                  ├─ Control de calidad
                 │
Galán            ├──────────────────┤ [Logistics]
(Entradas/Salidas│                  ├─ Gastos de logística
 Stock)          │                  ├─ Imputación a empleado
                 │
               [PagedResult<A3NominaDto>]
                 │
                 └──────────────────► [A3InnuvaNominasController]
                                     ├─ GET /api/nóminas?page=1
                                     ├─ POST /api/nóminas/sync
                                     ├─ POST /api/nóminas/export
```

---

## 4. Estructura de Datos en SIG ES

### 4.1 Tabla `A3NominaCalculada` (Backend)

Almacena el resultado del cálculo de nómina por empleado:

```csharp
public class A3NominaCalculada
{
    public int Id                          // PK
    public int EmployeeId                  // FK → Recurso
    public int CompanyId                   // FK → Empresa
    public DateTime PeriodStart             // Desde
    public DateTime PeriodEnd               // Hasta
    public string PayType                   // "Paga Mensual" / "Paga Extra"
    
    // Cálculos
    public decimal BrutAmount              // Importe bruto
    public decimal SSEmployee               // SS trabajador
    public decimal IrpfAmount              // IRPF total
    public decimal NetAmount               // Líquido
    public decimal SSCompany               // SS empresa
    
    // Deducciones
    public decimal Embargo                 // Embargo salarial
    public decimal Advance                 // Anticipo
    public decimal LoanDiscount            // Préstamo
    public decimal ProrrataBonus           // Pagas extras
    
    // Conceptos
    public decimal Kilometers              // Reembolso km
    public decimal Supplies                // Suplidos
    public decimal TravelCosts             // Viajes
    public decimal SalaryInKind            // Salario en especie
    
    // Acumulados
    public decimal AccumulatedPrevious      // Acumulado anterior
    public decimal AccumulatedNext          // Acumulado siguiente
    public decimal NegativeNomina           // Nóminas negativas
    
    // Control
    public string Status                   // Draft, Calculated, Validated, Exported
    public string ExportCode               // "PRUEBA" / "REAL"
    public DateTime ExportDate             // Fecha de asiento
    public bool IsExported                 // ¿Ya enviada a A3?
    
    public DateTime CreatedAt
    public DateTime? UpdatedAt
}
```

### 4.2 DTOs para API

```csharp
public class A3NominaDto
{
    public int Id
    public int EmployeeId
    public string EmployeeName
    public string EmployeeNIF
    public DateTime PeriodStart
    public DateTime PeriodEnd
    public string PayType
    public decimal BrutAmount
    public decimal NetAmount
    public string Status
    public bool IsExported
}

public class A3NominaDetailDto : A3NominaDto
{
    // Todos los campos de A3NominaCalculada
    public decimal SSEmployee
    public decimal IrpfAmount
    public decimal SSCompany
    public decimal Embargo
    // ... etc
}

public class PagedResult<T>
{
    public List<T> Items
    public int Total
    public int Page
    public int PageSize
}
```

---

## 5. Dónde Obtener Cada Dato

### 5.1 Salario Base y Descuentos
**Fuente:** Wolters Kluwer A3 INNUVA (descarga mensual)  
**Campos:** Importe bruto, SS trabajador, IRPF  
**Patrón de sincronización:** POST `/api/a3-innuva/sync`  
**Frecuencia:** Mensual (antes del cierre de nómina)

### 5.2 Horas Trabajadas y Fichajes
**Fuente:** Intratime (API)  
**Campos:** USER_ID, USER_COMPANY, Entrada, Salida, Horas totales, Extras  
**Patrón de sincronización:** GET `/api/intratime/timesheets?startDate=X&endDate=Y`  
**Frecuencia:** Diaria o cada 8 horas
**Cálculo:** Horas × Tarifa base (si aplica)

### 5.3 Gastos Reembolsables (km, dietas, viajes)
**Fuente:** PayHawk (API)  
**Campos:** Expense ID, Amount, Category, Employee, Settlement Date  
**Patrón de sincronización:** GET `/api/payhawk/expenses?startDate=X&endDate=Y`  
**Frecuencia:** Diaria
**Cálculo por categoría:**
- **KM:** millas × 0.19€ (tarifa legal 2026) → Campo KM en A3NOM
- **Dietas:** Por proyecto/tarifa → Campo SUPLIDOS en A3NOM
- **Tickets/Hotel:** Suplidos → Campo SUPLIDOS en A3NOM

### 5.4 Visitas y Rendimiento
**Fuente:** Celero (API)  
**Campos:** visitId, clientId, visitDate, visitedBy, shelvesUpdated  
**Patrón de sincronización:** GET `/api/celero/visits?startDate=X&endDate=Y`  
**Frecuencia:** Diaria
**Cálculo:** Visitas × Tarifa (variable por proyecto)  
**Ejemplo:** DYSON = 25€/visita + incentivos si > 100 visitas/mes

### 5.5 Validación de Empleados y Estructura
**Fuente:** Bizneo (API)  
**Campos:** ID Empleado, Nombre, Estado, Puesto, Estructura, Horas contratadas  
**Patrón de sincronización:** GET `/api/bizneo/employees?active=true`  
**Frecuencia:** Semanal (cambios de estructura)
**Usos:**
- Validar que el empleado existe y está activo
- Obtener departamento/centro de coste
- Validar horas según contrato

### 5.6 Viajes Corporativos
**Fuente:** TravelPerk (API)  
**Campos:** Traveler, Booking Date, Trip Date, Cost Amount  
**Patrón de sincronización:** GET `/api/travelperk/bookings?startDate=X&endDate=Y`  
**Frecuencia:** A demanda (cuando hay viajes)
**Cálculo:** Se suma a SUPLIDOS en A3NOM

### 5.7 Validación de Visitas (Geolocalizadas)
**Fuente:** SGPV (API)  
**Campos:** idVisita, GPV, idCliente, visitDate  
**Patrón de sincronización:** GET `/api/sgpv/visits?startDate=X&endDate=Y`  
**Frecuencia:** Diaria
**Propósito:** Validación de calidad (asegura que las visitas de Celero son reales)

### 5.8 Gastos Logísticos
**Fuente:** Galán (Excel upload)  
**Campos:** Entrada (inbound), Salida (outbound), Stock, Almacenaje  
**Patrón de sincronización:** Auto-sync on upload (POST `/api/galan/upload`)  
**Frecuencia:** A demanda (cuando hay movimientos)
**Cálculo:** Imputación por empleado/proyecto → Campo Imputación en A3NOM

### 5.9 Información de Empresa y Contrato
**Fuente:** SIG ES (Base de datos interna)  
**Campos:** Company.Id, Contract.StartDate, Contract.EndDate, Contract.Status  
**Propósito:** Filtros para la nómina
- Solo empleados con contrato activo en el período
- Nómina a empresa = Company.Id (en ejemplo: "1 - SERVICE INNOVATION GROUP ESPAÑA SERVICIO")

---

## 6. Cálculos Realizados por CalculationEngine

### 6.1 Cálculo de Importe Bruto

```
BRUTO = Salario Base A3 INNUVA
      + (Horas Extras × Tarifa Extra)           [Intratime]
      + (Visitas × Tarifa Visita)                [Celero]
      + (Dietas por día trabajado)              [PayHawk, Galán]
      + (Conceptos Fijos del Proyecto)          [CierresIntegralesSIG]
      + (Incentivos si se cumplen metas)        [Celero + lógica]
      - (Descuentos por Absentismo)             [Bizneo]
      ─────────────────────────────────────────
      = BRUTO MENSUAL
```

### 6.2 Cálculo de Deducciones Obligatorias

```
SS TRABAJADOR    = BRUTO × Tasa SS (según tramo)
                 Ej: 4.7% hasta Base máxima de cotización
                 
IRPF             = BRUTO × Tasa IRPF (según estado civil, hijos, etc.)
                 Ej: 19%, 21%, 24%, 26%, 28%, 30%, 33%, 35%, 37%, 40%
```

**Nota:** SS Trabajador e IRPF ya vienen calculados en A3 INNUVA. SIG ES no necesita recalcularlos, solo replicarlos.

### 6.3 Importe Líquido

```
LÍQUIDO          = BRUTO
                 - SS TRABAJADOR
                 - IRPF
                 - EMBARGO (si existe)
                 - ANTICIPO (si existe)
                 - DESCUENTO PRÉSTAMO (si existe)
                 - DESCUENTO ABSENTISMO (si aplica)
                 + PRORRATA PAGAS EXTRAS (acumulado)
```

### 6.4 Imputación Contable

```
IMPUTACIÓN       = Cód. Contable del Centro de Coste (CECO)
                 
Estructura:
  - Centro de Coste (de Bizneo)
  - Código Proyecto (de Celero)
  - Cuenta analítica (según tipo de concepto)
  
Ejemplo:
  640000.001.001  = 640000 (Cuenta de Sueldos)
                    .001    (Centro de Coste)
                    .001    (Código de Proyecto)
```

### 6.5 Validaciones

```
Pre-cierre:
✓ Empleado existe en Bizneo (ID existe y está activo)
✓ Contrato existe y es válido en el período
✓ Horas fichadas <= Horas contratadas (warning si > 180%)
✓ Visitas >= Visitas mínimas del proyecto (warning si no)
✓ Período coincide con período de nómina

Post-cálculo:
✓ BRUTO >= 0 (no negativo)
✓ LÍQUIDO >= 0 (puede ser 0 si hay muchos descuentos)
✓ SS TRABAJADOR <= BRUTO × 7% (validación de rango)
✓ IRPF <= BRUTO × 45% (validación de rango)
✓ Imputación contable existe en chart of accounts
```

---

## 7. Flujo de Aprobación y Cierre

### 7.1 Estados de la Nómina

```
┌─────────────────┐
│  DRAFT          │  Estado inicial tras descarga de A3 INNUVA
└────────┬────────┘
         │ [Cálculos CalculationEngine]
         ▼
┌─────────────────┐
│ CALCULATED      │  Cálculos completados, pendiente validación
└────────┬────────┘
         │ [Validación de RRHH/Facilitador]
         ▼
┌─────────────────┐
│ VALIDATED       │  Validaciones pasadas, pendiente aprobación FICO
└────────┬────────┘
         │ [Aprobación Grupo → FICO]
         ▼
┌─────────────────┐
│ APPROVED        │  Aprobado por FICO, listo para exportar
└────────┬────────┘
         │ [A3InnuvaNominasService.Export()]
         ▼
┌─────────────────┐
│ EXPORTED        │  Exportado a A3 INNUVA (asiento REAL generado)
└─────────────────┘
```

### 7.2 Matriz de Aprobación

| Rol | DRAFT | CALCULATED | VALIDATED | APPROVED | EXPORTED |
|-----|-------|-----------|-----------|----------|----------|
| **Admin** | Ver/Editar/Validar | Ver/Editar/Validar | Ver/Editar/Validar | Ver/Editar/Validar | Ver/Editar |
| **Dirección** | Ver | Ver/Validar | Ver/Validar/Aprobar | Ver/Aprobar | Ver |
| **FICO** | Ver | Ver | Ver/Validar | Ver/Aprobar | Ver |
| **RRHH** | Ver | Ver/Validar | Ver/Validar | Ver | Ver |
| **Facilitador** | Ver | Ver/Validar | Ver/Validar | Ver | Ver |

---

## 8. Casos de Uso Reales (Por Proyecto)

### 8.1 ECRES GRANINI (GPV GRANINI)

```
Motor Pago:      MENSUAL
Estructura:      Salario fijo + variables

Cálculo:
  BRUTO = Salario Fijo A3 INNUVA (3000€ aprox.)
        + Dietas por día trabajado (15€/día) [PayHawk]
        + KM reembolsados (0.19€/km)        [PayHawk]
        + Tickets pagados (real)            [PayHawk]
        + Incentivos visitas (si > 200)     [Celero]
  
  LÍQUIDO = BRUTO - SS - IRPF
  
Facturación:  Igual que pago (cliente paga lo que se paga al recurso)
Imputación:   CECO de proyecto GRANINI
```

### 8.2 Inpost (Prueba Piloto MENSUAL_IMPUTADO)

```
Motor Pago:      MENSUAL_IMPUTADO
Estructura:      Nómina pagada por horas trabajadas

Cálculo:
  BRUTO = Horas Fichadas (Intratime) × Tarifa Horaria
        = 180 horas × 12€/hora = 2160€
  
  LÍQUIDO = BRUTO - SS - IRPF
  
Facturación:  Según modelo de la piloto (se define en CierresIntegralesSIG.xlsx)
Imputación:   Por proyecto Inpost
```

### 8.3 molins-propamsa (UNIDAD/TRAMOS)

```
Motor Pago:      UNIDAD/TRAMOS
Estructura:      Tarifa por unidad + tramos de tiempo

Cálculo:
  BRUTO = Nº Unidades × 15€/unidad
        + Horas Adicionales × 11.92€/hora
        = (200 unidades × 15€) + (10 horas × 11.92€)
        = 3000€ + 119.2€ = 3119.2€
  
  LÍQUIDO = BRUTO - SS - IRPF
  
Facturación:  Margen sobre pago (ej: +15%)
Imputación:   CECO Molins, proyecto Implantaciones/Almacenamiento
```

### 8.4 DYSON (VISITA/MIXTO)

```
Motor Pago:      VISITA/MIXTO
Estructura:      Tarifa por visita + tarifa por tiempo

Cálculo:
  BRUTO = Nº Visitas × Tarifa
        + Tiempo Adicional × Tarifa Hora
        + Incentivos si > Meta
  
  Ejemplo:
    150 visitas × 15€ = 2250€
    + 5 horas extras × 11.92€ = 59.6€
    + Incentivo (100% meta alcanzada) = 200€
    ─────────────────────────────────
    = 2509.6€ BRUTO
  
  LÍQUIDO = BRUTO - SS - IRPF
  
Facturación:  Igual a pago o con margen (según contrato)
Imputación:   CECO Dyson
```

---

## 9. Checklist de Implementación

### 9.1 Backend

- [ ] **Tabla `A3NominaCalculada`** creada en BD
  - [ ] Campos de salario, deducciones, conceptos
  - [ ] Índices en EmployeeId, PeriodStart/End, Status
  
- [ ] **CalculationEngine.cs** actualizado
  - [ ] Método `CalculateNomina(employeeId, periodStart, periodEnd)`
  - [ ] Validaciones pre-cálculo
  - [ ] Cálculo de bruto, deducciones, líquido
  - [ ] Imputación contable
  - [ ] Logging detallado
  
- [ ] **A3InnuvaNominasService.cs**
  - [ ] `SyncFromInnuva()` — Descarga de A3
  - [ ] `CalculateForPeriod()` — Dispara CalculationEngine
  - [ ] `ExportToInnuva()` — Genera archivo A3NOM.xls
  - [ ] `ValidateNomina()` — Valida pre-aprobación
  
- [ ] **A3InnuvaNominasController.cs**
  - [ ] GET `/api/a3-nominas?page=1&pageSize=25` — Listado paginado
  - [ ] GET `/api/a3-nominas/{id}` — Detalle
  - [ ] POST `/api/a3-nominas/sync` — Sincronización manual
  - [ ] POST `/api/a3-nominas/{id}/calculate` — Recálculo
  - [ ] POST `/api/a3-nominas/{id}/validate` — Validación
  - [ ] POST `/api/a3-nominas/{id}/approve` — Aprobación
  - [ ] POST `/api/a3-nominas/{id}/export` — Exportación
  - [ ] GET `/api/a3-nominas/{id}/download` — Descargar A3NOM.xls
  
- [ ] **DTOs**
  - [ ] `A3NominaDto` — Lista
  - [ ] `A3NominaDetailDto` — Detalle completo
  - [ ] `A3NominaExportDto` — Formato de exportación
  
- [ ] **Integración con sistemas externos**
  - [ ] HttpClient para Bizneo (validación empleados)
  - [ ] HttpClient para Intratime (horas fichadas)
  - [ ] HttpClient para PayHawk (gastos)
  - [ ] HttpClient para Celero (visitas)
  - [ ] Excel reader para Galán (logística)
  
- [ ] **Migraciones EF Core**
  - [ ] `CreateA3NominaCalculadaTable`
  - [ ] Seed de periodos iniciales
  - [ ] Índices de performance

### 9.2 Frontend

- [ ] **Componente `a3-nominas-list.component.ts`**
  - [ ] Tabla paginada de nóminas
  - [ ] Filtros por período, empleado, estado
  - [ ] Búsqueda por nombre de empleado
  - [ ] Acciones: Ver, Editar (DRAFT), Validar, Aprobar, Exportar
  
- [ ] **Componente `a3-nominas-detail.component.ts`**
  - [ ] Desglose completo de la nómina
  - [ ] Secciones: Metadatos, Bruto, Deducciones, Líquido, Imputación
  - [ ] Botones de acción según rol/estado
  - [ ] Descarga de A3NOM.xls
  
- [ ] **Componente `a3-nominas-sync.component.ts`**
  - [ ] Botón "Sincronizar de A3 INNUVA"
  - [ ] Progress bar (cálculos en backend)
  - [ ] Notificación de resultado
  
- [ ] **Routing**
  - [ ] `/a3-nominas` — Listado
  - [ ] `/a3-nominas/:id` — Detalle
  - [ ] `/a3-nominas/new` — Crear (si aplica)
  
- [ ] **Services**
  - [ ] `a3-nominas.service.ts` — API calls con paginación
  - [ ] `calculation.service.ts` — Lógica de cálculo (si se refleja en frontend)

### 9.3 Testing

- [ ] **Backend Tests**
  - [ ] CalculationEngine: casos normales, mínimos, máximos, excepciones
  - [ ] A3InnuvaNominasService: sync, calculate, export, validate
  - [ ] A3InnuvaNominasController: endpoints REST
  
- [ ] **Frontend Tests**
  - [ ] Listado paginado carga datos
  - [ ] Detalle muestra cálculos correctamente
  - [ ] Filtros y búsqueda funcionan
  - [ ] Botones de acción habilitados/deshabilitados según rol/estado
  
- [ ] **Integration Tests**
  - [ ] End-to-end: Descarga A3 → Cálculos → Validación → Exportación
  - [ ] Casos de proyectos ECRES, Inpost, Molins, Dyson

---

## 10. Glosario y Términos

| Término | Definición |
|---------|-----------|
| **A3 INNUVA** | Plataforma de gestión de nóminas de Wolters Kluwer |
| **A3NOM.xls** | Archivo de nómina (asiento contable) generado por A3 INNUVA |
| **Bruto** | Suma de todos los conceptos salariales (antes de deducciones) |
| **Centro de Coste (CECO)** | Código de imputación contable (ej: departamento, proyecto) |
| **Concepto** | Componente de la nómina (salario base, dietas, horas extras, etc.) |
| **Deducción** | Descuento sobre el bruto (SS, IRPF, embargo, etc.) |
| **Empresa** | Entidad legal que paga la nómina (ej: "SERVICE INNOVATION GROUP ESPAÑA") |
| **Imputación** | Código contable donde se registra el gasto |
| **IRPF** | Impuesto sobre la Renta de Personas Físicas (retención fiscal) |
| **Líquido** | Importe neto que percibe el empleado (Bruto - Deducciones) |
| **Nómina Imputada** | Nómina cuyos gastos se trasladan directamente al cliente (proyecto) |
| **Prorrata** | Cálculo proporcional (ej: pagas extras repartidas en 12 meses) |
| **SS (Seguridad Social)** | Cotización obligatoria (trabajador + empresa) |
| **Suplidos** | Conceptos pagados por terceros (hoteles, transporte, tickets) |
| **Tarifa Hora** | Precio €/hora (ej: 11.92€/hora en Cosmética) |

---

## 11. Archivos Relacionados en el Repositorio

```
backend/
├── SIG.API/
│   ├── Controllers/A3InnuvaNominasController.cs
│   └── Program.cs                         (configuración de DI)
│
├── SIG.Application/
│   ├── Interfaces/Integrations/IA3InnuvaClient.cs
│   ├── Calculation/CalculationEngine.cs
│   ├── DTOs/A3NominaDtos.cs
│   └── Services/A3InnuvaNominasService.cs
│
├── SIG.Infrastructure/
│   ├── Integrations/Http/A3InnuvaClient.cs
│   ├── Services/A3InnuvaSyncService.cs
│   └── Persistence/AppDbContext.cs       (DbSet<A3NominaCalculada>)
│
└── SIG.Tests/
    └── A3InnuvaNominasTests.cs

frontend/
├── src/app/
│   ├── features/a3-nominas/
│   │   ├── a3-nominas-list.component.ts
│   │   ├── a3-nominas-detail.component.ts
│   │   ├── a3-nominas-sync.component.ts
│   │   └── a3-nominas.service.ts
│   │
│   └── core/api/
│       └── a3-nominas.service.ts

docs/
├── CierresIntegralesSIG.xlsx              (especificación maestro)
├── ejemplo ARCHIVO A3NOM.xls              (ejemplo de salida)
└── ANALISIS_A3INNUVA_NOMINAS.md           (este archivo)
```

---

## 12. Próximos Pasos

1. **Leer detalladamente** las hojas de CierresIntegralesSIG.xlsx:
   - Especialmente "Pagos - Facturación" y "Lógicas Pagos-Facturación" para entender cálculos por proyecto
   
2. **Descargar ejemplos reales** de cada fuente de datos:
   - Archivo Innuva con empleados reales
   - Descarga Bizneo (empleados)
   - Descarga Intratime (fichajes)
   - Etc.
   
3. **Mapear campos** de cada fuente a campos en A3NOM.xls
   
4. **Implementar CalculationEngine** con lógica de cálculo por proyecto
   
5. **Crear tests** con casos de ejemplo (ECRES GRANINI, Dyson, etc.)
   
6. **Validar con el equipo FICO/RRHH** que la lógica de cálculo es correcta

---

**Documento generado:** 2026-06-24  
**Archivos analizados:** 2  
**Hojas procesadas:** 22  
**Campos documentados:** 150+
