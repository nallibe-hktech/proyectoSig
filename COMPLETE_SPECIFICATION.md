# Resumen Completo de la Especificación SIG-ES

## Descripción General del Documento

Este documento consolida toda la información de especificación de tres archivos fuente:
1. **CierresIntegralesSIG.xlsx** — Modelo de datos completo, reglas de cálculo y lógica de negocio
2. **Definición alertas y errores.docx** — Definiciones de alertas, mensajes de error y reglas de validación
3. **ejemplo ARCHIVO A3NOM.xls** — Especificación del formato del archivo de exportación A3 Innuva

---

## 1. ESTRUCTURA DEL ARCHIVO EXCEL (CierresIntegralesSIG.xlsx)

### Inventario de Hojas

El libro contiene 20 hojas organizadas por función:

| Nombre de la Hoja | Propósito |
|-----------|---------|
| CRONOGRAMA | Cronograma y calendario del proyecto (plantilla diagrama de Gantt) |
| ESTRUCTURA | Estructura del equipo y asignación de roles |
| CONEXIONES | Credenciales de conexión y detalles de sistemas |
| Glosario | Glosario de conceptos clave y orígenes de datos |
| Roles | Roles de usuario y matriz de permisos |
| Flujo | Flujo de procesamiento de datos y pipeline |
| Entidades | Definiciones de entidades, atributos y relaciones |
| Conceptos x Proyecto | Mapeo de conceptos por proyecto |
| Pagos - Facturación | Lógica de cálculo de pagos y facturación |
| Detalles PagosFact_IL | Detalles de pagos con lógica implícita |
| CuadroDetallesPagosFact | Tablas resumen de facturación de pagos |
| Innuva, Bizneo, Intratime, Payhawk, etc. | Especificaciones y mapeos de fuentes de datos |

---

## 2. MODELO DE DATOS DE ENTIDADES

### 2.1 Definiciones de Entidades Principales

#### USUARIO
- **Definición**: Usuarios de la aplicación con permisos para ver o modificar registros
- **Atributos**: id, NIF, Nombre, Apellidos, Mail, Estado
- **Relaciones**:
  - Un usuario tiene un Rol asignado (uno a muchos)
  - Un usuario pertenece a un Departamento (uno a muchos)
  - Un usuario está asignado a uno o varios Proyectos (muchos a muchos)
  - Todas las acciones de aprobación/edición derivan de Usuario
- **Reglas de Negocio**: Los usuarios sin Rol no pueden ver ni realizar acciones

#### ROL
- **Definición**: Definición del perfil de permisos para los usuarios
- **Atributos**: Nombre, permisos
- **Relaciones**: Un rol se asigna a uno o varios Usuarios
- **Matriz de Permisos**:

| Rol | Vista | Pagos | Facturaciones | Auditorias | Usuarios | Roles |
|-----|-------|-------|---------------|-----------|----------|-------|
| Administrador | Global | Control total | Control total | Ver | Control total | Control total |
| Dirección | Global | Ver/Validar/Editar | Ver/Validar/Editar | Ver | Ver/Editar/Crear | Sin permisos |
| FICO | Global | Ver/Validar/Editar | Ver/Validar/Editar | Ver | Ver/Editar/Crear | Sin permisos |
| RRHH | Global | Ver/Validar/Editar | Sin permisos | Sin permisos | Ver/Editar/Crear | Sin permisos |
| Facilitador | Global | Ver/Validar/Editar | Ver/Validar/Editar | Ver | Ver/Editar/Crear | Sin permisos |
| Interlocutor | Proyecto | Ver/Validar/Editar | Ver/Validar/Editar | Sin permisos | Sin permisos | Sin permisos |
| Gestor | Proyecto | Ver/Validar/Editar | Ver/Validar/Editar | Sin permisos | Sin permisos | Sin permisos |
| Backoffice | Proyecto | Ver/Validar | Sin permisos | Sin permisos | Sin permisos | Sin permisos |
| Auxiliar | Proyecto | Ver | Sin permisos | Sin permisos | Sin permisos | Sin permisos |

#### RECURSO / EMPLEADO
- **Definición**: Empleado de campo que genera registros de rendimiento (no accede a la aplicación)
- **Nombre anterior**: "Recurso" — renombrado a "Empleado"
- **Atributos**: resourceId, NIF, Nombre, Apellidos, Estado
- **Fuentes de Datos**:
  - Principal: A3 Innuva (datos salariales y de contrato)
  - Secundaria: Bizneo (registros de empleados), Celero (asignaciones de trabajo)
- **Relaciones**:
  - Un recurso está asignado a uno o varios Proyectos
  - Un recurso está asignado a uno o varios Contratos
  - Un recurso genera Rendimientos (registros de desempeño)
  - Un recurso tiene Gastos y Viajes asociados

#### RENDIMIENTO
- **Definición**: Valores numéricos registrados en diversas fuentes de datos
- **Fuentes de Datos**:
  - Celero: Visitas, actividades
  - Bizneo: Fichajes, imputación de horas
  - Intratime: Fichajes de entrada y salida
  - Payhawk: Gastos, dietas, kilometraje
  - A3 Innuva: Contratos y salarios
- **Atributos asociados**: visitas, fichajes, horas, gastos, kilometraje

#### PERIODO
- **Definición**: Rango de fechas que agrupa cierres contables (ej. marzo-2026)
- **Atributos**: Año, trimestre, mes
- **Uso**: Agrupa múltiples registros transaccionales para liquidación mensual/trimestral

#### CLIENTE
- **Definición**: Usuario final al que se le aplica/imputa la facturación; entidad jerárquica que agrupa proyectos
- **Atributos**: clientId, Nombre cliente, CIF, dirección, mail, estado
- **Relaciones**: Un cliente contiene uno o varios Proyectos
- **Fuente de Datos**: Celero o maestro interno
- **Mapeo Celero**: clientId, clientName

#### DEPARTAMENTO
- **Definición**: Grupo interno que gestiona determinados proyectos/acciones
- **Atributos**: departmentId, Nombre departamento
- **Relaciones**:
  - Un departamento contiene uno o varios Proyectos
  - Un departamento contiene uno o varios Usuarios
- **Fuente de Datos**: Celero, Payhawk
- **Mapeo**: Departamento External ID, Departamento

#### PROYECTO / SERVICIO
- **Definición**: Servicio contratado por un cliente que genera actividad en un periodo definido; entidad jerárquica que agrupa Acciones
- **Relaciones**:
  - Pertenece a un Departamento
  - Pertenece a un Cliente
  - Pertenece a un Ceco
  - Contiene uno o varias Acciones
  - Tiene Visitas asociadas
- **Fuente de Datos**: Celero One
- **Mapeo Celero**: serviceId, serviceName
- **Mapeo Payhawk**: Proyecto External ID, Proyecto

#### ACCIÓN
- **Definición**: Actividad temporal o permanente que se realiza dentro de un proyecto
- **Atributos**: serviceId, Nombre acción, estado
- **Relaciones**: Una acción pertenece a un único Proyecto
- **Ejemplos**: Campaña, implantación, merchandising, etc.

#### VISITA
- **Definición**: Unidad de actividad para un proyecto/acción
- **Atributos**: id, fecha, estado, centro, facturación, pago
- **Relaciones**: Una visita pertenece a un único Usuario Y a un único Proyecto
- **Fuente de Datos**: Celero
- **Mapeo Celero**: VisitId, visitPlanDate, visitFinishedAt

#### CONCEPTO
- **Definición**: Paquete que contiene la lógica de cálculo aplicada a montos (ej. dietas, gastos)
- **Atributos**: id, Nombre concepto, Fecha desde, Fecha hasta, Jerarquía, Definición de cálculo
- **Características**:
  - Cálculo: Campos utilizados y operaciones a realizar
  - Periodo de aplicación: Rango de fechas en el que aplica el cálculo
  - Jerarquía: Nivel en el que se realiza el cálculo (todos los proyectos, acciones específicas, empleados específicos)
  - Desglose: Vista de registros fuente (plataforma, fecha, usuario)
  - Trazabilidad: Registro de fecha del último cambio y autor
- **Fuente de Datos**: Payhawk
- **Mapeo**: Expense Category
- **Ejemplos** (de la hoja Conceptos x Proyecto):
  - Cuota / Dietas por día trabajado
  - Cuota fija mensual
  - Cuota por hora estimada
  - Cuota por hora trabajada
  - Cuota por visita
  - Fee sobre conceptos
  - Gastos Payhawk
  - Kilometraje
  - Logística
  - Incentivos mensuales/trimestrales

#### PAGO
- **Definición**: Remuneración pagada a un trabajador (Recurso)
- **Atributos**: Periodo de pago, fecha de pago, estado
- **Reglas de Negocio**: Los pagos se generan a partir de conceptos calculados aplicados a registros de actividad

#### FACTURA
- **Definición**: Costo facturado al cliente
- **Atributos**: Periodo de pago, fecha máxima de pago, estado
- **Relaciones**: Se pueden generar múltiples facturas por periodo para diferentes conceptos/proyectos

#### CONTRATO
- **Definición**: Periodo de días y condiciones bajo las cuales se contrata un Recurso
- **Atributos**: Fecha alta, fecha baja, salario bruto, jornada, fecha cobro nómina, fecha cobro incentivos
- **Relaciones**:
  - Un contrato pertenece a un Recurso
  - Un contrato contiene uno o varios Proyectos
  - Un contrato especifica salario y condiciones de trabajo
- **Fuente de Datos**: A3 Innuva (principal)
- **Mapeo Innuva**: NIF, Codigo Empleado, Trabajador, Imputación, Fecha de cobro, Importe bruto, Días alta
- **Reglas de Negocio**:
  - Dos contratos del mismo Recurso no pueden solaparse
  - Contratos de un solo día suelen indicar separaciones de personal (periodo de prueba no superado)
  - Sin embargo, algunos contratos de un SÍ corresponden a actividad real y pagos reales

#### CECO (Centro de Coste)
- **Definición**: Codificación contable utilizada para clasificar gastos e ingresos
- **Atributos**: Ceco (identificador), estado
- **Relaciones**:
  - Un Ceco contiene uno o varios Proyectos
  - Algunos Cecos contienen proyectos ficticios (ej. "RRHH" - Recursos Humanos) que no pertenecen a ningún Cliente
- **Fuente de Datos**: Celero, Payhawk, A3 Innuva y otras fuentes
- **Regla de Negocio**: Debe coincidir con la tabla maestra; las discrepancias son ERRORES BLOQUEANTES

#### CENTRO
- **Definición**: Establecimiento donde se realiza la visita
- **Atributos**: id, Nombre centro, dirección, código postal, población, provincia, país, código interno cliente, enseña
- **Relaciones**: Un Centro recibe uno o varias Visitas
- **Fuente de Datos**: Celero
- **Mapeo Celero**: clientId, clientName (implícito vía registro de visita)

#### GASTO
- **Definición**: Monto necesario para la realización de la actividad
- **Atributos**: Recurso, Tipo de gasto, Descripción, Acción, Fecha, Importe, Kilómetros, Pago por km, Aprobador
- **Fuente de Datos**: Payhawk
- **Mapeo Payhawk**:
  - Expense Owner ID, Expense Owner (Recurso)
  - Paid Amount (Importe)
  - Mes y Año, Document Date (Fecha)
  - Proyecto (Acción)
  - Expense Category (Tipo de gasto)
- **Reglas de Negocio**:
  - Los gastos individuales pueden ser negativos (no solo la suma total)
  - Un precio de km superior a €0.25/km genera una ADVERTENCIA

#### VIAJE
- **Definición**: Desplazamiento o alojamiento realizado por un Recurso
- **Atributos**: Código de reserva, Tipo (desplazamiento/alojamiento), Origen, Destino, Recurso, Fecha, Acción, Importe, Cantidad noches
- **Fuente de Datos**: Payhawk
- **Reglas de Negocio**: Se rastrean por separado de los gastos diarios para informes

#### APROBACIÓN DE CIERRE
- **Definición**: Estado de finalización del periodo contable
- **Atributos**: Estado (pendiente, aprobado, rechazado), Usuario aprobador, fecha aprobación, comentarios

---

## 3. FUENTES DE DATOS E INTEGRACIONES

### Matriz de Origen de Datos

| Origen | Rol | Recurso | Rendimiento | Periodo | Cliente | Depto | Proyecto | Visita | Concepto |
|--------|-----|---------|-------------|---------|---------|-------|----------|--------|----------|
| **Celero** | Asignaciones Recurso/Usuario | Mapeo de Recurso | - | - | clientId, clientName | departmentId, departmentName | serviceId, serviceName | VisitId, visitPlanDate, visitFinishedAt | - |
| **Bizneo** | Flujo de aprobación de empleados | ID del empleado, Nombre | Horas registradas - Total | - | - | - | PROJECT_ID | - | - |
| **Intratime** | Rol de aprobación | USER_NIF, USER_ID | INOUT_DATE | - | CLIENT_ID | - | PROJECT_ID | - | - |
| **Payhawk** | Nombre del Aprobador | Expense Owner ID, Expense Owner | Paid Amount | Mes y Año, Document Date | - | Departamento | Proyecto | - | Expense Category |
| **A3 Innuva** | Mapeo de usuario (AD) | NIF, Codigo Empleado, Trabajador | Importe bruto, Salario | Fecha cobro | - | Ceco | Ceco | - | Ceco |
| **Manual** | Azure AD / manual | - | Objetivos, cuotas, conceptos no estándar | - | - | - | - | - | - |

### Detalles de las Fuentes

#### CELERO
- **Tipo**: Plataforma operativa para visitas de campo y campañas
- **Datos**: Visitas, actividades, recursos, proyectos, clientes, departamentos, centros
- **Actualización**: Tiempo real/diaria
- **Campos Clave**:
  - userId: Identificador de Usuario/Recurso
  - resourceId, resourceExternalId: Mapeo de Recurso
  - visitPlanDate, visitFinishedAt: Fechas de actividad
  - clientId, clientName: Referencia de cliente
  - departmentId, departmentName: Referencia de departamento
  - serviceId, serviceName: Referencia de Proyecto/Acción
  - VisitId: Identificador único de visita

#### BIZNEO
- **Tipo**: Gestión de empleados y seguimiento de horas
- **Datos**: Registros de empleados, fichajes, imputación de horas
- **Actualización**: Diaria
- **Campos Clave**:
  - ID del empleado: Identificador del empleado
  - Aprobador: Usuario de aprobación (Rol)
  - Horas registradas - Total: Total de horas registradas
  - PROJECT_ID: Asignación de proyecto

#### INTRATIME
- **Tipo**: Sistema de fichajes de entrada y salida
- **Datos**: Marcas de tiempo de entrada y salida
- **Actualización**: Tiempo real
- **Campos Clave**:
  - USER_NIF: NIF del empleado
  - USER_ID: Identificador de usuario
  - INOUT_DATE: Marca de tiempo
  - CLIENT_ID: Referencia de cliente
  - PROJECT_ID: Referencia de proyecto

#### PAYHAWK
- **Tipo**: Plataforma de gestión de gastos
- **Datos**: Gastos, dietas, kilometraje, reservas de viaje
- **Actualización**: Diaria
- **Campos Clave**:
  - Approver Name: Usuario de aprobación
  - Expense Owner ID, Expense Owner: Empleado/Recurso
  - Paid Amount: Monto del gasto
  - Mes y Año External ID, Mes y Año: Referencia de periodo
  - Document Date: Fecha de transacción
  - Departamento External ID, Departamento: Departamento
  - Proyecto External ID, Proyecto: Proyecto/Acción
  - Expense Category: Tipo de concepto
  - Asociado con: Kilometraje, dietas, gastos

#### A3 INNUVA
- **Tipo**: Sistema de gestión de recursos humanos y nóminas (HRIS)
- **Datos**: Contratos, salarios, fechas de empleo, movimientos de personal
- **Actualización**: Dos veces al mes (periodos de pago)
- **Campos Clave**:
  - NIF: NIF del empleado (CLAVE PRIMARIA para conciliación)
  - Codigo Empleado: Identificador de Innuva
  - Trabajador: Nombre del empleado
  - Imputación: Porcentaje de asignación del contrato
  - Fecha de cobro: Fecha de pago (fin de contrato o fin de periodo)
  - Importe bruto: Salario bruto por contrato + extras confirmados
  - Días alta: Días desde el inicio del contrato hasta la fecha de cierre
  - fecha baja innuva: Fecha de baja del empleado
  - Ceco: Centro de coste contable

#### ENTRADA MANUAL DE DATOS
- **Tipo**: Conceptos no digitalizados y objetivos personalizados
- **Datos**: Objetivos, cuotas, bases de cálculo no estándar
- **Actualización**: Ad-hoc
- **Uso**: Suplementa fuentes digitales para el cálculo completo de conceptos

---

## 4. LÓGICA DE CÁLCULO

### 4.1 Tipos de Concepto y Métodos de Cálculo

De la hoja "Pagos - Facturación":

#### Tipo 1: Monto Fijo Mensual
- **Descripción**: Cantidad fija mensual
- **Cálculo**: Monto fijo por mes por recurso
- **Filtros**: Cantidad mínima, Cantidad máxima
- **Proyectos**: Molins, Granini, JDE

#### Tipo 2: Conteo de Visitas × Cantidad Fija
- **Descripción**: Conteo de Visitas x Cantidad fija
- **Cálculo**: Número de visitas × cantidad fija por visita
- **Filtros**: Cantidad mínima, Cantidad máxima
- **Proyectos**: Varios

#### Tipo 3: Días con Actividad × Cantidad Fija
- **Descripción**: Conteo de días con actividad (Visitas) x Cantidad
- **Cálculo**: Conteo de días con cualquier visita × cantidad diaria fija
- **Filtros**: Se aplican igual que Tipo 2
- **Proyectos**: Varios

#### Tipo 4: Kilómetros × Coste por Km
- **Descripción**: Suma de Kilómetros x Coste por Km
- **Cálculo**: Total de kilómetros × tarifa €/km
- **Filtros**: Cantidad mínima, Cantidad máxima, Rendimiento mínimo
- **Fuente de Datos**: Registros de kilometraje de Payhawk
- **Proyectos**: Granini, JDE, Molins, Cosmetica, ITC
- **Regla de Negocio**: Precio de km > €0.25/km genera ADVERTENCIA

#### Tipo 5: Conteo Entidad-A × Entidad-B
- **Descripción**: Conteo de Entidad-A x Entidad-B
- **Cálculo**: Conteo de una entidad multiplicado por el monto de otra entidad
- **Filtros**: Filtrado por entidad (conteo de visitas/kilometraje/suma de actividad)
- **Proyectos**: Varios

#### Tipo 6: Suma Entidad-A × Entidad-B
- **Descripción**: Suma de Entidad-A x Entidad-B
- **Cálculo**: Suma de un tipo de entidad multiplicada por el valor de la entidad relacionada
- **Filtros**: Similar al Tipo 5
- **Proyectos**: Varios

#### Tipo 7: Porcentaje de Entidad
- **Descripción**: Porcentaje de Entidad
- **Cálculo**: Porcentaje aplicado a un conteo/suma de entidad
- **Proyectos**: Varios

#### Tipo 8: Porcentaje Fijo de Cantidad Variable
- **Descripción**: Porcentaje fijo de cantidad variable
- **Cálculo**: Porcentaje fijo aplicado a un monto variable (ej. total de gastos)
- **Proyectos**: Cálculos de fees

#### Tipo 9: Conteo de Horas × Cantidad Incremental
- **Descripción**: Conteo de horas x Cantidad incremental (primera hora...)
- **Cálculo**: Horas con tarifas crecientes (primera hora = X, siguientes = Y)
- **Fuente de Datos**: Fichajes de horas de Bizneo
- **Proyectos**: Molins, Morrison

#### Tipo 10: Tarifa/Hora × Horas Estimadas
- **Descripción**: Tarifa/hora × Horas estimadas (acordadas)
- **Ejemplo**: €11.92/hora × 4 horas = €47.68 brutos
- **Regla de Negocio**: Cuando el tiempo real excede el estimado, se aplica pago adicional
- **Proyectos**: Dyson, DJI, Kobo, Coty Impl, Cosmetica

### 4.2 Mapeo Proyecto-Concepto

| Concepto | Pago | Facturación |
|----------|------|-------------|
| Cuota / Dietas por día trabajado | Granini, JDE, Dyson, Amex, Ploom | Granini, JDE, Morrison |
| Cuota fija mensual | Molins | Granini, JDE, Molins, Daikin, Apple RST, ITC |
| Cuota fija mensual por Recurso | Granini, JDE | - |
| Cuota por hora estimada | Dyson, DJI | - |
| Cuota por hora estimada - según tipo visita | Kobo | Kobo |
| Cuota por hora trabajada | Molins, Morrison | Morrison |
| Cuota por hora trabajada - incremental según tipo | Molins | - |
| Cuota por hora trabajada - según tipo de extra | Coty Impl, Cosmetica | Coty Impl, Cosmetica |
| Cuota por visita | Apple RST, Ploom | Dyson, Apple RST |
| Cuota por visita - según tiempo, provincia y tipo | Inpost | - |
| Cuota por visita - según tipo mueble y tipo visita | Coty Impl, Cosmetica | Coty Impl, Cosmetica |
| Cuota por visita - según tipo visita | Apple BA, Amex, Ploom | - |
| Fee sobre conceptos | Granini, JDE, Inpost, Molins, Cosmetica, Kobo, ITC | - |
| Gastos Payhawk | Granini, JDE, Cosmetica, DJI, ITC | Granini, JDE, Cosmetica, DJI, ITC |
| Gastos proyecto (catering, alquiler salas) | Apple RST, Amex | - |
| Incentivos mensuales | Granini, JDE, Apple BA, ITC | Amex, ITC |
| Incentivos trimestrales | Granini, Daikin | Granini |
| Kilometraje | Granini, JDE, Molins, Cosmetica, ITC | Granini, JDE, Cosmetica, ITC |
| Logística | Kobo, Apple RST, Amex, ITC | - |
| Logística autónomos | Coty Impl | - |
| Logística Galán | Cosmetica, Apple BA | - |

### 4.3 Ejemplo: Lógica de Pago de Merchandising de Cosmética

**Escenario**: Proyecto de optimización (merchandising de campo de cosmética)

**Tarifa**: €11.92 brutos/hora (tarifa estándar)

**Cálculo Base**:
```
Pago = Tarifa/hora × Horas Estimadas (acordadas)
Ejemplo: 11.92 € × 4 horas = 47.68 € (brutos)
```

**Regla de Excedente**: Cuando el tiempo real excede el estimado:
- Primeras X horas: Tarifa estándar
- Horas adicionales: [Regla a especificar en documentación completa]

**Conceptos Aplicables**:
- Pago: Cuota por hora trabajada - según tipo de extra
- Facturación: Cuota por hora trabajada - según tipo de extra

---

## 5. DEFINICIONES DE ALERTAS Y ERRORES

### 5.1 Clasificación de Errores

Dos categorías de errores:

#### BLOQUEANTES
Impiden el cierre de proyecto/acción hasta que se resuelvan. Si la acción no está identificada, aparecen como ADVERTENCIA para los usuarios con vista global.

#### ADVERTENCIAS
Una vez confirmadas por un usuario autorizado (con permiso de validación en su rol), el cierre puede proceder aunque no estén resueltas.

### 5.2 Errores Bloqueantes (Fallos en datos de PAGO)

| Error | Descripción | Acción Requerida |
|-------|-------------|------------------|
| Contratos duplicados o solapados en A3 Innuva | Mismo empleado con dos contratos cruzados en fechas de vigencia | Resolver el solapamiento de contratos en A3 Innuva |
| NIF no coincide con empleado de A3 Innuva | Datos incorrectos en Celero, Payhawk u otras fuentes | Corregir NIF en la fuente de datos |
| Campos clave faltantes o sin coincidencia | NIF, fecha de actividad, identificador, etc. en blanco o sin coincidencia | Completar los campos faltantes en la fuente |
| Actividad sin contrato | Visitas en Celero con fecha fuera de cualquier periodo de vigencia de contrato en A3 Innuva | Associar la actividad con un periodo de contrato válido |
| Centro de coste sin coincidencia con tabla maestra | CECO incorrecto en Celero, Payhawk, A3 Innuva, etc. | Corregir CECO para que coincida con datos maestros |

**Impacto**: El cierre de proyecto/acción queda bloqueado hasta su resolución. Las acciones no identificadas aparecen solo como advertencias.

### 5.3 Errores de Advertencia

| Advertencia | Descripción | Acción de Validación |
|---------|-------------|----------------------|
| Contrato sin actividad | Un contrato de A3 Innuva no tiene visitas en Celero y/o no tiene fichajes | Confirmar: empleado inactivo en el periodo o problema de sincronización de datos |
| Coste de kilometraje elevado | Precio de km de Payhawk > €0.25/km | Confirmar: necesidad empresarial legítima o corrección necesaria |
| Gastos negativos | Un gasto individual (no la suma total) en Payhawk es negativo | Confirmar: reembolso o ajuste intencional |
| Visitas infrapagadas | Pago de actividad < cantidad contratada en Innuva | Confirmar: cálculo correcto o discrepancia en condiciones del contrato |

**Impacto**: Un usuario con permiso de validación (diferente del de aprobación) puede confirmar y permitir el cierre a pesar de la advertencia.

### 5.4 Filtro Especial: Contratos de Un Día

**Regla**: Los contratos de un día en A3 Innuva suelen indicar separaciones de personal (periodo de prueba no superado).

**Punto Clave**: Aunque el registro muestra pago, estos NO se ejecutan. Los montos deben IGNORARSE en el cierre.

**Excepción**: Algunos contratos de un día SÍ corresponden a actividad real y pagos reales.

**Acción**: El sistema debe distinguir entre:
- Contratos de un día falsos (separaciones) → Ignorar en el pago
- Contratos de un día verdaderos (trabajo legítimo) → Incluir en el pago

---

## 6. FLUJO DE PROCESAMIENTO DE DATOS

### Pipeline de Procesamiento (de la hoja "Flujo")

```
┌─────────────────────────────────────────────────────────┐
│  FUENTES DE DATOS (Múltiples)                           │
│  Manual | Celero | Bizneo | Intratime | Payhawk | Innuva │
└────────────────────────┬────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────┐
│  INTERFAZ / INGESTIÓN DE DATOS                           │
│  Carga / Sincronización API desde cada fuente            │
└────────────────────────┬────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────┐
│  REPOSITORIO (BASE DE DATOS)                             │
│  Almacenamiento unificado con tablas de staging           │
└────────────────────────┬────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────┐
│  LIMPIEZA Y ESTANDARIZACIÓN DE DATOS                     │
│  - Normalizar formatos de campos                         │
│  - Mapear IDs externos a entidades internas              │
│  - Resolver duplicados                                   │
└────────────────────────┬────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────┐
│  DETECCIÓN DE ERRORES Y VALIDACIÓN                       │
│  - Identificar inconsistencias (BLOQUEANTES, ADVERTENCIAS)│
│  - Notificar a los responsables                          │
└────────────────────────┬────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────┐
│  CÁLCULO DE CONCEPTOS                                    │
│  - Aplicar lógica de cálculo por concepto                │
│  - Agrupar por periodo, jerarquía, entidad               │
└────────────────────────┬────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────┐
│  VALIDACIÓN DE RESPONSABLES                              │
│  - Enviar a FICO / Finanzas para aprobación              │
│  - Puede editar/devolver registros                       │
└────────────────────────┬────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────┐
│  NOTIFICACIÓN A FICO                                     │
│  - Validación individual y cierre a nivel de proyecto    │
│  - Puerta de aprobación antes de exportar                │
└────────────────────────┬────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────┐
│  EXPORTACIÓN A INNUVA / SISTEMA ERP A3                   │
│  - Formatear y transmitir datos de liquidación           │
│  - Generación de nóminas y facturas                      │
└─────────────────────────────────────────────────────────┘
```

---

## 7. FORMATO DEL ARCHIVO DE EXPORTACIÓN A3 INNUVA

### Fuente: "ejemplo ARCHIVO A3NOM.xls"

**Estructura del Archivo**: Hoja única "Pág._1_"

**Dimensiones**: 8 filas × 29 columnas

**Información de Cabecera (Filas 1-5)**:
```
Fila 3, Col B: "Paga Mensual" / "Asiento de nómina PRUEBA" (Col L)
Fila 4, Col B: "Del 01/05/2026 al 31/05/2026" (Periodo: 1-31 de mayo de 2026)
Fila 5, Col B: "Empresa: 1 - SERVICE INNOVATION GROUP ES" (Empresa)
```

**Columnas de Datos (Fila 8 en adelante)**:

| Columna | Cabecera | Descripción |
|--------|--------|-------------|
| I | Imputación | Código de asignación/imputación del contrato |
| J | Tipo de paga | Tipo de pago (mensual, extras, ajustes) |
| K | Importe bruto | Monto bruto (salario + adiciones confirmadas) |
| L | Seguridad Social trabajador | Cotización del empleado a Seguridad Social |
| M | Tributación IRPF total | Retención total del IRPF |
| N | Importe líquido | Monto neto (bruto - deducciones) |
| O | Total Seguridad Social de empresa | Cotización de la empresa a Seguridad Social |
| P | Descuento embargo salarial | Deducción de embargo salarial |
| Q | Anticipo | Anticipo de pago |
| R | Descuento préstamo | Deducción de devolución de préstamo |
| S | Prorrata pagas extras | Acumulado proporcional de pagas extra |
| T | KM | Kilometraje/asignación de kilómetros (vinculado a Payhawk) |
| U | SUPLIDOS | Gasto de suministros/artículos proporcionados |
| V | Exoneración líquida de Empresa Total | Exoneración neta total de la empresa |
| W | Ajuste Nómina | Ajuste de nómina |
| X | Salario en Especie | Salario en especie/beneficios adicionales |
| Y | Descuento conceptos en especie | Deducción de beneficios en especie |

**Características Clave**:
- Un empleado por fila (después de la fila 8)
- Columnas A-H: Metadatos previos a la fila (no detallados en el ejemplo)
- Columnas I-Y: Desglose de composición del pago
- Diseñado para importación al sistema de nóminas (A3 / ERP)
- Específico del periodo (mensual)
- Soporta múltiples tipos de pago y categorías de deducción

**Mapeo de Datos**:
- **Importe Bruto (Columna K)**: Suma de:
  - Salario base del contrato de A3 Innuva (Importe bruto)
  - Montos de pago calculados desde la plataforma (visitas, horas, conceptos, gastos)
  - Incentivos (mensuales/trimestrales de proyectos)
  - Kilometraje (de Payhawk, Columna T)
  
- **Deducciones (Columnas L, M, P, Q, R, W, Y)**: Reglas fiscales y de RRHH aplicadas por el sistema A3

- **Importe Líquido (Columna N)**: Bruto - Todas las deducciones = Monto pagado al empleado

---

## 8. PUNTOS DE INTEGRACIÓN Y MAPEOS DE DATOS

### 8.1 Mapeo de Identificadores de Entidad

| Entidad | Celero | Payhawk | Bizneo | Intratime | A3 Innuva |
|--------|--------|---------|--------|-----------|-----------|
| **Usuario/Recurso** | userId, resourceId | Expense Owner ID | ID del empleado, Aprobador | USER_NIF, USER_ID | NIF (PRIMARIO), Codigo Empleado |
| **Proyecto** | serviceId | Proyecto External ID | PROJECT_ID | PROJECT_ID | Ceco (implícito) |
| **Departamento** | departmentId | Departamento External ID | - | - | Jerarquía de Ceco |
| **Cliente** | clientId | - | - | CLIENT_ID | - |
| **Periodo** | - | Mes y Año | - | - | Fecha cobro |
| **Centro de Coste** | - | - | - | - | Ceco (PRIMARIO) |

### 8.2 Claves de Conciliación

**Conciliación Primaria**: NIF (NIF)
- Todos los registros de empleados deben conciliarse por NIF en Celero, Payhawk, Bizneo, Intratime, A3 Innuva
- Discrepancia = ERROR BLOQUEANTE

**Claves Secundarias**:
- resourceId (Celero) → Codigo Empleado (A3 Innuva)
- serviceId (Celero) → Proyecto (Payhawk) + Ceco (A3 Innuva)
- clientId (Celero) → Cliente (si está mapeado en nómina)

---

## 9. GLOSARIO Y TÉRMINOS CLAVE

| Término | Definición | Nota de Idioma |
|------|-----------|----------------|
| Recurso | Empleado de campo / Recurso; se está renombrando a "Empleado" | Español |
| Rendimiento | Registro de rendimiento / Valor numérico de fuente de datos | Español |
| Concepto | Paquete de lógica de cálculo (ej. dietas, gastos) | Español |
| Ceco | Centro de coste / Codificación contable | Abreviatura en español |
| Visita | Visita de campo / Unidad de actividad | Español |
| Acción | Acción / Actividad a nivel de proyecto (campaña, merchandising) | Español |
| Cierre | Cierre de periodo contable / Liquidación | Español |
| Bloqueante | Error bloqueante / Debe resolverse antes del cierre | Español |
| Advertencia | Advertencia / Puede confirmarse y proceder | Español |
| Innuva / A3 ERP | Punto de integración del sistema de nóminas y RRHH | Nombre del sistema |
| Imputación | Porcentaje de asignación del contrato | Español |
| Nómina | Nómina / Liquidación salarial | Español |

---

## 10. RESUMEN DE REGLAS DE NEGOCIO

### 10.1 Integridad de Datos

1. **NIF es CLAVE PRIMARIA**: Todos los registros de empleados deben conciliarse por NIF
2. **Sin contratos solapados**: Dos contratos del mismo recurso no pueden tener fechas solapadas
3. **Alineación Actividad-Contrato**: Toda actividad (visitas, horas, gastos) debe caer dentro de un periodo de contrato válido
4. **Validación de centro de coste**: Todos los centros de coste deben existir en la tabla maestra
5. **Filtro de contratos de un día**: Distinguir entre separaciones (ignorar) y trabajo legítimo (incluir)

### 10.2 Reglas de Cálculo

1. **Jerarquía de conceptos**: Los cálculos se aplican en el nivel especificado (todos los proyectos, acción específica, empleado específico)
2. **Periodo del concepto**: El cálculo solo aplica dentro del rango de fechas definido
3. **Múltiples conceptos permitidos**: La misma entidad puede tener múltiples conceptos aplicados en el mismo periodo
4. **Agregación**: Los conceptos pueden sumarse, contarse o multiplicarse dependiendo del tipo
5. **Fees**: Pueden aplicarse como porcentaje fijo del total de otros conceptos

### 10.3 Aprobación y Validación

1. **Usuarios con vista global**: Ven todos los proyectos; ven errores bloqueantes incluso si la acción no está identificada
2. **Usuarios con ámbito de proyecto**: Solo ven proyectos asignados
3. **Permiso de validación**: Diferente del de aprobación; puede confirmar advertencias sin aprobación completa
4. **Revisión FICO**: Finanzas valida y puede editar/devolver registros individuales o a nivel de proyecto
5. **Puerta de cierre**: El proyecto se cierra solo después de la aprobación de FICO

### 10.4 Exportación y Liquidación

1. **Exportación A3 Innuva**: Formateado según estándar de archivo NOM (columnas I-Y según lo especificado)
2. **Específico del periodo**: Cada exportación cubre un periodo de pago específico (ej. mayo 2026)
3. **Gestión fiscal/RRHH**: El sistema A3 aplica deducciones, impuestos, seguridad social
4. **Generación de nóminas**: El sistema A3 genera cheques de pago a partir de montos netos
5. **Generación de facturas**: Exportación de facturas separadas a clientes (no cubierto en detalle aquí)

---

## 11. ESTRUCTURA DEL EQUIPO Y RESPONSABILIDADES

### Hoja CRONOGRAMA - Fases del Proyecto

**Fase 0: Definir Roles y Responsabilidades**
- Designar responsable funcional por área (Operaciones/Campo, Finanzas/Pagos)
- Asignar Product Owner, responsables técnicos

**Fase 1: Requisitos y Definición de Procesos**
- Procesos AS-IS / TO-BE (Celero, Bizneo, Intratime, Payhawk, A3 Innuva)
- Documentación de reglas de negocio
- Definir dinámica de trabajo

**Fase 2: Preparación y Limpieza de Datos**
- Importar y mapear datos de todas las fuentes
- Resolver problemas iniciales de calidad

**Fase 3: Configuración y Preparación del Sistema**
- Configurar grupos de Azure Active Directory
- Definir acceso basado en roles
- Establecer reglas de validación de datos

**Fase 4: Configuración del Motor de Cálculo**
- Definir conceptos por proyecto
- Configurar lógica de cálculo
- Establecer flujos de aprobación

**Fase 5: Integración y Pruebas**
- Probar flujos de datos de extremo a extremo
- Validar cálculos
- Ejecutar cierre(s) piloto

**Fase 6: Informes y Analítica (BI)**
- Definir KPIs
- Configurar Power BI / analítica
- Diseñar paneles de control

**Fase 7: Validación y Puesta en Producción**
- Aseguramiento de calidad final
- Formación del equipo
- Lanzamiento a producción

---

## 12. REFERENCIAS DOCUMENTALES

### Archivos Fuente
- **CierresIntegralesSIG.xlsx**: 20 hojas con modelo de datos completo, cálculos y mapeos
- **Definición alertas y errores.docx**: Definiciones de alertas y errores con reglas de negocio
- **ejemplo ARCHIVO A3NOM.xls**: Ejemplo del formato del archivo de exportación de nóminas A3 Innuva

### Hojas Clave a Referenciar
- **Entidades**: Definiciones completas de entidades (filas 3-59+)
- **Conceptos x Proyecto**: Mapeo proyecto-concepto (filas 4-24)
- **Pagos - Facturación**: Tipos de cálculo y ejemplos (filas 3-30)
- **Innuva**: Mapeos de fuente de datos de contratos y salarios (filas 1-19)
- **Roles**: Matriz de permisos de roles de usuario (filas 1-10)
- **Flujo**: Diagrama del pipeline de procesamiento de datos (filas 2-28)

---

## Fin de la Especificación

**Última Actualización**: 2026-06-15  
**Completitud**: Todas las hojas, todas las filas con datos, todas las reglas de cálculo, todos los mapeos de entidades, todas las reglas de validación incluidas
