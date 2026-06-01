# SIG-es Tool Requirements Analysis

**Analysis Date:** May 29, 2026  
**Source Documents:** 
- ejemplo ARCHIVO A3NOM.xls
- CierresIntegralesSIG.xlsx
- Demo pantallas.xlsx
- SIG-H&K cuestionario (1).docx

---

## Executive Summary

SIG-es is an integrated financial operations platform designed to consolidate payroll, invoicing, and operational data from 10 different source systems. The platform must handle complex, multi-variable calculation logic with strict audit requirements and role-based access control. The system is being built in phases, starting with an MVP that prioritizes Celero (field operations), A3 Innuva (HR/payroll), and SGPV (field sales), expanding to 10 integrated systems.

**Critical Focus Areas:**
1. Multi-tenant calculation engine with client-specific rules
2. Strict approval workflows with deviation tracking
3. Comprehensive audit logging (5-year retention, fiscal compliance)
4. Excel-based export formats for legacy ERP integration (A3)
5. Role-based visibility and data masking

---

## 1. DATA MODEL REQUIREMENTS

### 1.1 Core Entities

The system manages the following primary entities:

| Entity | Key Fields | Source System | Business Role |
|--------|-----------|----------------|---------------|
| **Usuario (User)** | id, NIF, Name, Surname, Email, Role | Manual + Azure AD | System authentication, approval authority |
| **Rol (Role)** | id, Name, Permissions | Manual | Access control matrix (see Section 7) |
| **Recurso (Employee)** | resourceId, NIF, Name, Surname, Email, Team | Bizneo, A3 Innuva | Field worker, generates visits/hours |
| **Cliente (Client)** | clientId, NIF, Name, Address, Contact | Celero | Customer, receives invoice |
| **Departamento (Department)** | departmentId, Name, Notes | Celero | Internal org unit |
| **Proyecto (Project/Action)** | serviceId, Name, Status, Client, Department, Cost Center | Celero | Billable service or campaign |
| **Visita (Visit)** | visitId, Date, Duration, Resource, Project, Client, Status | Celero | Field activity unit |
| **Concepto (Concept)** | conceptId, Name, Calculation Logic, Hierarchy, Deductions | Manual | Salary/invoice line item (e.g., "Cuota por hora", "Fee") |
| **Pago (Payment)** | paymentId, Period, Date, Amount, Resource, Status | Calculated | Employee compensation |
| **Factura (Invoice)** | invoiceId, Period, Amount, Client, Lines, Status | Calculated | Customer billing |
| **Contrato (Contract)** | contractId, StartDate, EndDate, Salary, CostCenter, Resource | A3 Innuva | Employment agreement |
| **Ceco (Cost Center)** | cecoId, Code, Status | A3 Innuva | Financial classification |
| **Centro (Store/Location)** | centerId, Code, Name, Address | Celero, SGPV | Physical location for visits |
| **Gasto (Expense)** | expenseId, Amount, Category, Date, Resource, Project | Payhawk | Out-of-pocket spending |
| **Viaje (Travel)** | tripId, Type (accommodation/flight), Dates, Cost, Resource | TravelPerk | Business travel booking |
| **Período (Period)** | periodId, StartDate, EndDate, Year, Quarter, Month | Manual | Accounting time unit |
| **Cierre (Closing)** | closureId, Period, Status, ApprovalChain, LastModifier, Timestamp | Calculated | Finalized payroll/invoicing batch |
| **Aprobación (Approval)** | approvalId, ClosureId, Role, Status, Notes, Timestamp | Workflow | Approval audit record |

### 1.2 Entity Relationships

```
Cliente (1) ──→ (M) Proyecto
               ├─→ (M) Departamento
               └─→ (M) Centro

Proyecto ──→ (M) Visita
         ├─→ (M) Concepto
         └─→ (M) Ceco

Recurso ──→ (M) Visita
        ├─→ (M) Contrato
        ├─→ (M) Gasto
        └─→ (M) Pago

Usuario ──→ (1) Rol
        └─→ (M) Aprobación

Pago ──→ (M) Concepto
Factura ──→ (M) Concepto

Cierre ──→ (M) Aprobación
Cierre ──→ (M) Pago | Factura
```

### 1.3 Field Types & Data Characteristics

**Calculations & Hierarchies (from Glosario sheet):**
- **Jerarquía**: Levels at which calculations apply (e.g., per employee, per project, per cost center)
- **Período de aplicación**: Date range when a concept is active
- **Desglose (Breakdown)**: Each concept can be decomposed into sub-components
- **Trazabilidad**: Version history of how each calculation was computed

**Key Numeric Ranges (from client-specific payroll rules):**
- Hourly rates: €11.92 – €60+ depending on client
- Minimum monthly salary: SMI (Spanish national minimum) + variations
- Margin thresholds: Alerts if < 15%, critical if < 10%
- Visit duration: 0.5–8+ hours per day per resource
- Kilometer rates: €0.19–€0.25/km depending on contract

---

## 2. BUSINESS LOGIC

### 2.1 Payment Calculation Models

The system supports **multiple payment models per client**, each with distinct formulas:

#### Model A: Monthly Fixed + Variables (Granini, JDE, Inpost)
```
Payment = Fixed Monthly Salary + 
          (Visits × Per-Visit Bonus) +
          (Days with Activity × Daily Allowance) +
          (KM × Rate) +
          (Hours Extra × Overtime Rate) -
          (Deductions: Embargoes, Loans, Absenteeism)
```

#### Model B: Hourly (OPTIMISING, Morrison, Cosmetica)
```
Payment = (Estimated Hours × Standard Rate) +
          (Extra Hours × Overtime Rate) +
          (Incentives if Margin > Threshold%) -
          (Deductions)
```

**Standard Rate for OPTIMISING:** €11.92/hour gross

#### Model C: Per-Unit/Per-Furniture (COTY, DJI, Kobo, Molins)
```
Payment = Fixed per Unit/Module +
          (Second Visit Surcharge if applicable) +
          (Additional Hours × Rate) -
          (Failure/Return Deductions)
```

#### Model D: Daily Rate + Incentives (DAIKIN, ITC, Apple)
```
Payment = (Contract Salary / Working Days) +
          (Performance Bonus if KPI Met) +
          (Cost Adjustments)
```

#### Model E: Mixed (Cheil, Ploom, COTY)
```
Payment = (Fixed Portion) +
          (Hours-based Portion) +
          (Per-Visit Portion) +
          (Logistics Adjustment)
```

### 2.2 Invoice Calculation Models

Billing to clients follows **different logic than payroll**, allowing for markup/margin:

#### Model A: Monthly Fee (Granini, JDE, Inpost, Daikin, ITC)
```
Invoice = Fixed Monthly Fee +
          Variables (Dietas, Km, Travel) +
          (Fee % on Logistics)
```

#### Model B: Per-Visit (Apple, Dyson, Amex, Morrison, ROLL OUT)
```
Invoice = (Visit Count × Tariff by Type/Zone) +
          (Logistics: COSTE+MARGEN or Fixed Fee) +
          (Add-on fees if 2nd/3rd Visit)
```

#### Model C: Hourly + Action (OPTIMISING, Cosmetica)
```
Invoice = (Hours × Tariff/Hour) +
          (Fee per Action/Implant) +
          (Logistics: Galán or MDP Provider)
```

#### Model D: Quote/Presupuesto (DYSON, Kobo, ROLL OUT, DJI)
```
Invoice = Agreed Quote Amount +
          (Logistics if not included) +
          (Revisit surcharge if applicable)
```

### 2.3 Deductions, Retentions & Exceptions

**Employee Payroll Deductions:**
- Embargo (judicial wage garnishment) – varies by employee
- IRPF (income tax retention) – calculated by salary
- Social Security contribution (employee portion) – standard 6.35%
- Loan repayments – per employee agreement
- Absenteeism penalties – TBD by FICO
- Advance salary repayment – if applicable

**Billing Deductions:**
- 21% VAT on all domestic services, **Exempt for intra-EU** (zero-rated)
- Logistics surcharge or inclusion depending on contract
- Risk of non-payment adjustments for certain clients

### 2.4 Retroactivity Rules

**Decision:** "Conditional - typically full-month application"
- Any tariff change is recorded with a **start date**
- Changes apply **forward-looking by default**
- **Exception:** If error discovered in current month, FICO can mark for retroactive adjustment in next period
- **Requirement:** Original calculation version must be stored for audit (no in-place overwrites)

### 2.5 Validations & Error Handling

**Bloqueante (Must-Fix Before Closure):**
1. Employee without valid contract for the period
2. Gasto (expense) without assigned project
3. Visita without assigned recurso (resource)
4. Tarifa (rate) missing for billable client+project combination
5. Salary below SMI (Spanish minimum) without documented exception
6. Margin discrepancy with presupuesto (if presupuesto exists)
7. Horas sin justificar (unjustified hours) exceeding threshold

**Advertencia (Warning, User Confirms):**
1. Extra hours beyond contract maximum (requires overtime approval)
2. Margin below 15% but above 10% (yellow flag, proceed with caution)
3. Multiple revisits to same center in short period
4. Anticipated payments/advances not yet cleared
5. Logistics cost estimates exceeded

**Auto-Alert Triggers:**
- Every error raises notification to project owner or backoffice
- Escalation to FICO if unresolved after 24 hours
- Admin override with audit trail capability

### 2.6 Complex Business Scenarios (from Detalles Pagos/Fact_IL sheet)

**Logistics Models:**
1. **COSTE+MARGEN**: Provider cost (Galán, MDP) + markup % or fixed fee
2. **FEE-based**: Fixed logistics fee regardless of km/cost
3. **PRESUPUESTO**: Logistics quoted upfront, not recalculated
4. **INCLUIDA_EN_TARIFA**: Logistics baked into hourly/per-visit rate
5. **VARIABLE/UNDEFINED**: TBD for several clients (Apple BA, Daikin, Apple Training)

**Revisit Rules (2nd/3rd Visit):**
- Some clients charge fixed surcharge per revisit
- Others include revisits in base tariff
- Exception: revisits due to **SIG error are free**; revisits due to customer request charged

**Travel & Advanced Payments:**
- TravelPerk bookings auto-captured, linked to project
- Per-diem/dietas tracked separately from salary
- Advances deducted in next payroll (with embargo risk)
- Requires FICO pre-approval for amounts > threshold

---

## 3. UI/UX REQUIREMENTS

### 3.1 Main Navigation Structure

**From "Navegación principal" sheet:**

**Primary Menu:**
- Dashboard
- Clientes (Clients)
- Proyectos (Projects)
- Conceptos (Concepts)
- Periodos (Periods)
- Aprobaciones (Approvals)
- Contabilidad (Accounting/Reporting)
- Informes (Reports)
- Administración (Admin)

**Admin Submenu:**
- Cecos (Cost Centers)
- Departamentos (Departments)
- Roles (Role Configuration)
- Usuarios (User Management)
- Auditoria (Audit Log)

### 3.2 Key Screens (Planned)

**Dashboard Screen:**
- Summary metrics: Total payroll MTD, invoiced MTD, margin %, pending approvals count
- By-client view: revenue, margin %, outstanding items per client
- By-period view: payment status, invoicing completion %
- Alert panel: Blockers, warnings, escalations
- Date range picker (default: current month + YTD)

**Data Ingestion (Celero, Bizneo, Intratime, etc.):**
- Manual upload or API auto-sync status
- Last sync timestamp per system
- Row count ingested vs. errors
- Data quality warnings (missing fields, inconsistencies)

**Concept Configuration Screen:**
- List: Concept name, hierarchy level, period applicability, calculation formula
- CRUD: Create new concept, edit rules, version history
- Test: Input sample data, preview calculation output
- Approval: Requires FICO sign-off to activate

**Pago/Cierre (Payroll Closure) Screen:**
- Period selector
- Employee list filtered by project/department
- For each employee: base salary, visitas, horas, gastos, conceptos applied, deductions, net total
- Edit capability (with audit trail)
- Validation status: green (ok), yellow (warning), red (blocker)
- Approval workflow buttons: Submit to FICO, Approve (if FICO), Return for revision
- Export to Excel (template for A3 Innuva)

**Factura/Cierre (Invoice Closure) Screen:**
- Similar layout to payroll but by client+project
- Revenue line items (services, logistics, extras)
- Margin calculation vs. presupuesto
- Invoicing model applied (monthly fee, per-visit, hourly, quote)
- Approval workflow (same as payroll)
- Export to Excel (template for A3 ERP)

**Approval Queue:**
- List of pending closures (payroll + invoices) awaiting action
- Grouped by role (for FICO: all pending; for Gestor: only assigned projects)
- Bulk actions: approve multiple, return multiple with comment
- Time-in-queue indicator (escalation after 24h)

**Audit Log / Auditoría:**
- Full change history: user, timestamp, field changed, old value, new value
- Filter by entity (closure, user, concept), date range, change type
- Export to CSV for compliance
- Only visible to Admin + Facilitators + FICO (per D.2)

**Usuarios y Roles (User Management):**
- User list: name, email, assigned role, projects (if restricted), active status
- CRUD user: create, edit role, disable, reset password
- Role matrix editor: define permissions per role (CRUD, approve, export, etc.)
- SSO/Azure AD integration status

**Reportes/Informes (Reporting):**
- Pre-built reports: Margin by client, Productivity by employee, Costs by project, Monthly trend
- Drill-down capability: Select client → see all projects → see all visits → see all expenses
- Dimensions: Client, Project, Employee, Period, Role, Geography (if applicable)
- Export: Excel, PDF

### 3.3 Mobile Considerations

**Not in scope for MVP** – Field workers (recursos) will use Celero's mobile app for time tracking, not SIG directly. SIG is backoffice/finance operations only.

### 3.4 Accessibility & Localization

- **Language:** Spanish (ES) as primary; English optional for tech team
- **Accessibility:** WCAG 2.1 Level AA (required for public sector compliance)
- **Date Format:** DD/MM/YYYY (Spain standard)
- **Currency:** EUR (€)
- **Time Format:** 24-hour

---

## 4. INTEGRATION POINTS

### 4.1 Data Source Systems (Priority Order)

| Priority | System | Data Type | Frequency | Access Method | Validation |
|----------|--------|-----------|-----------|----------------|------------|
| 1 | **Celero** | Clients, Projects, Actions, Visits, Resources | Every 3 hours | Direct PostgreSQL + VPN | Master data (no duplicates, valid IDs) |
| 2 | **A3 Innuva** | Contracts, Salaries, Employees, Deductions | Daily (TBD hour) | API (ConectIA) | Valid employee, contract dates, salary > SMI |
| 3 | **SGPV** | Additional field resources, visit data | Daily (TBD hour) | HTTPS Download (login) | Resource ID must match master |
| 4 | **Bizneo** | Hours logged, Organizations, Absences | Daily (TBD hour) | API (REST) | Hours within contract bounds |
| 5 | **Intratime** | Timeclock in/out, Shifts, Projects | Daily (TBD hour) | API (REST) | Timestamp consistency, no future dates |
| 6 | **Payhawk** | Expenses, Dietas, Mileage, Categories | Daily (TBD hour) | API (REST) + Manual Export | Receipt linked, amount > 0, category valid |
| 7 | **TravelPerk** | Flights, Hotels, Car Rentals | Daily (TBD hour) | API (REST v2) | Dates within project, no duplicates |
| 8 | **Galán** | Logistics costs, Deliveries | Daily (SFTP file) | SFTP (stock delivery) | Linked to project, km reasonable |
| 9 | **Mediapost (MDP)** | Logistics costs, Distribution | Daily (SFTP file) | SFTP or API | Linked to project |
| 10 | **A3 ERP** | (Destination, not source) | Invoice data | API or Manual | N/A |

**Manual Data Entry (Low Volume):**
- Objetivos (sales targets) per employee per project – monthly input
- Cuotas (fixed monthly fees) for line items, storage, GPV services – annual config
- Conceptos especiales (one-off items) – per closure by authorized user
- Comisiones variables (variable bonuses) – quarterly or annual input

### 4.2 Output/Export Systems

#### A3 Innuva (Payroll Platform)

**Format:** Excel 97-2003 (.xls, not .xlsx)
**Frequency:** Monthly after FICO approval
**Required Fields (from ejemplo ARCHIVO A3NOM.xls):**
- Empresa (Company Code)
- Imputación (Cost allocation %)
- Tipo de Paga (Payroll type: base, overtime, bonus, etc.)
- Importe Bruto (Gross amount)
- Seguridad Social Trabajador (Employee SS contribution)
- Tributación IRPF (Income tax deduction)
- Importe Líquido (Net payment)
- Seguridad Social Empresa (Employer SS – informational)
- Descuento Embargo (Garnishment)
- Anticipo (Advances)
- Descuento Préstamo (Loan repayment)
- Prorrata Pagas Extras (Prorated bonuses)
- KM (Mileage reimbursement)

**Validation in A3:**
- Must include all contracted employees for the period
- Salary format must match A3's column structure
- No negative amounts (except deductions)
- Gross must reconcile with concepts used

#### A3 ERP (Accounting/Invoicing)

**Format:** Excel 2007+ (.xlsx)
**Frequency:** Monthly after FICO approval
**Data:**
- Client name, ID, VAT number
- Invoice lines: description, hours/units, rate, amount
- 21% VAT (domestic) or 0% (intra-EU exempt)
- Total invoice amount
- Project/Cost Center reference
- Billing period

**Validation in A3:**
- VAT calculation correct per region
- Line items must sum to total
- At least one line per invoice
- No duplicate invoice numbers

### 4.3 Data Integration Constraints

**No writes to source systems** – SIG is read-only from all source systems. No reverse sync.

**Data freshness requirements:**
- Celero: Projects/Actions updated every 3 hours to reflect new campaigns ASAP
- Employees: Updated daily (contracts change monthly typically)
- Timekeeping (Bizneo, Intratime): Updated daily (reflects yesterday's attendance)
- Expenses (Payhawk): Updated daily
- Travel (TravelPerk): Updated daily

**Conflict resolution (if same data from multiple sources):**
- Employee data: A3 Innuva is source-of-truth (payroll master)
- Visit data: Celero is master; SGPV is supplementary
- Hours: Bizneo (HR system) > Intratime (timeclock) in hierarchy if conflict

---

## 5. MISSING FEATURES & GAPS IN CURRENT CODEBASE

### 5.1 Not Yet Implemented

1. **Calculation Engine**
   - ❌ Multi-client concept framework (rules per client)
   - ❌ Hierarchical concept application (per employee, project, cost center)
   - ❌ Deduction priority system (what deducts first: IRPF → SS → Embargoes → Loans)
   - ❌ Retroactivity handling + version history storage
   - ❌ Test harness for formula validation before deployment

2. **Data Ingestion Pipelines**
   - ❌ Celero PostgreSQL connector (requires VPN setup, credential management)
   - ❌ Bizneo REST API integration (authentication, pagination, incremental sync)
   - ❌ Intratime API integration
   - ❌ Payhawk API integration + expense categorization logic
   - ❌ TravelPerk API integration
   - ❌ SFTP connectors for Galán/Mediapost
   - ❌ SGPV download automation (login + session management)
   - ❌ Data validation layer (schema, business rules, duplicate detection)
   - ❌ Error recovery + retry logic (exponential backoff, alerts)
   - ❌ Change Data Capture (CDC) for Celero to detect updates in 3-hour window

3. **Approval Workflow**
   - ❌ Multi-level approval state machine (Gestor → FICO → Dirección with returns)
   - ❌ Escalation logic (auto-notify if > 24h pending)
   - ❌ Bulk approval API
   - ❌ Email notifications on state changes
   - ❌ In-app notification queue

4. **Audit & Compliance**
   - ❌ Comprehensive audit log (who changed what, when, old→new value)
   - ❌ 5-year data retention policy + archival strategy
   - ❌ Read access controls per role (data masking in queries)
   - ❌ Version history storage for closures (snapshot before FICO approval)
   - ❌ GDPR deletion on-demand (employee exit process)
   - ❌ Compliance reporting (ISO 27001, fiscal requirements)

5. **Reporting & Analytics**
   - ❌ Pre-built dashboards (margin by client, productivity by employee, etc.)
   - ❌ Drill-down navigation (client → project → visit → expense)
   - ❌ Multi-dimensional analytics (dimensions: client, project, period, geography)
   - ❌ Export to PDF/Excel
   - ❌ Scheduled report delivery (email on fixed day/time)

6. **A3 Export Orchestration**
   - ❌ Excel template generation for A3 Innuva (payroll format)
   - ❌ Excel template generation for A3 ERP (invoice format)
   - ❌ Validation before export (field presence, format, value ranges)
   - ❌ Retry logic + human review if A3 rejects
   - ❌ Notification routing (FICO receives rejection message)

7. **Azure AD / SSO**
   - ❌ Azure AD integration (tenant: sigespana.es)
   - ❌ Role assignment: users in Azure NOT auto-granted SIG access (manual whitelist in SIG)
   - ❌ Password reset via Azure
   - ❌ MFA enforcement (if organizational policy requires)

8. **Operational Data Structures**
   - ❌ Cost Center (Ceco) master data management
   - ❌ Period (Período) management – open/close periods, prevent changes to closed
   - ❌ Concept templates + version control
   - ❌ Rate card management (tariff by client/project, validity dates)

### 5.2 Partially Implemented

- **User Management:** Basic CRUD exists; roles not wired to permissions yet
- **Project/Client Views:** Likely exists in Celero sync; detail screens missing
- **API Structure:** May have skeleton; business logic endpoints missing

### 5.3 Assumptions Made (Need Confirmation)

1. **Payment Timing:** All payments monthly, in-arrears (last day of month)? Or can vary per client?
2. **Margin Calculation:** Is margin = (Invoice - Total Cost) / Invoice? Or different formula?
3. **Logistics Cost Allocation:** For mixed models (e.g., OPTIMISING), how are Galán/MDP costs split among employees? Per visit? Per km? Equal?
4. **Dietas Threshold:** Is there a "dieta minimorum" (guaranteed daily allowance) independent of hours?
5. **Presupuesto Enforcement:** If invoice exceeds quote, is it auto-capped or escalated?
6. **Overtime Limits:** Is overtime capped per day (e.g., max 2h) or per month? Can be refused?
7. **Period Lock:** Once FICO approves a month, can it still be edited? Or read-only?
8. **Audit Retention:** After 5-year legal hold, can data be deleted? Or archived to cold storage?

---

## 6. INFORMATION GAPS & CLARIFICATIONS NEEDED

### 6.1 From Stakeholders (FICO, Operations, RRHH)

**Calculation Logic:**
- [ ] Exact formula for "Cuota por hora estimada" in DYSON project (estimated vs. actual reconciliation)
- [ ] How are "Incentivos trimestrales" calculated (threshold %, bonus amount)?
- [ ] For DAIKIN, what triggers the incentive? Sale target? Margin?
- [ ] Embargoes: Who informs SIG of new embargo (FICO)? Can employees dispute in-app?
- [ ] SMI threshold: Is there a per-project exception list, or strictly enforced?

**Data & Governance:**
- [ ] Payhawk: Are all employee expenses auto-captured, or does user filter/upload subset?
- [ ] Galán/Mediapost: Are delivery costs always linked to a project, or can they be unallocated?
- [ ] TravelPerk: If trip duration spans 2 months, which month's billing?
- [ ] Bizneo: Are absences (sick, vacation) coded so SIG can exclude hours from payment?
- [ ] A3 Innuva contract change: If salary increases mid-month, retroactive or forward?

**Approvals & Workflows:**
- [ ] After FICO rejects, does rejection reason auto-notify the Gestor? In-app only or email?
- [ ] Can multiple users approve simultaneously (parallel) or strict sequence (serial)?
- [ ] Devolución (Return): Max iterations before escalation to Director? Or unlimited?
- [ ] If FICO is unavailable for a week, can Admin force-approve with audit trail?

**Reporting & Compliance:**
- [ ] "Margin by Client" – is this month-end snapshot or rolling 12-month average?
- [ ] "Productivity by Employee" – is this visits/day? Hours/day? Revenue/day?
- [ ] Audit log: Do role changes (e.g., Gestor → Facilitador) need special audit events?
- [ ] GDPR deletion: When employee leaves, delete all historical data or anonymize?
- [ ] ISO 27001: Encryption at rest, in transit, key rotation strategy?

**A3 Integration:**
- [ ] When A3 Innuva rejects a payroll file (e.g., invalid NIF), who corrects: SIG user or A3 user?
- [ ] Does SIG retry auto-upload to A3, or human clicks "Send Again"?
- [ ] Invoice decimal places: 2? 4? Any rounding rules?
- [ ] A3 ERP: Can single SIG closure produce multiple ERP invoices (one per client)?

### 6.2 From System Owners (Celero, Bizneo, etc.)

**Celero:**
- [ ] PostgreSQL connection details, VPN IP range, credentials rotation schedule
- [ ] Full schema: visitReport table structure, field lengths, nullable columns
- [ ] Does Celero guarantee timestamp consistency (no out-of-order inserts)?
- [ ] Service/Action: Is status field reliable (ACTIVE/INACTIVE)?

**Bizneo:**
- [ ] Absences field: Is there a standardized code (SICK, VACATION, etc.) or free text?
- [ ] Hourly balance: How is "Horas sin justificar" calculated (flagged by manager)? Auto-alert in SIG?
- [ ] Revision/Amendment: If hours corrected, is it a full row replace or delta?

**Intratime:**
- [ ] Timezone handling: If user in Barcelona but logs from Berlin, which timezone in INOUT_DATE?
- [ ] Aggregation: SIG must sum INOUT entries daily per user; any edge cases (double-punch)?

**Payhawk:**
- [ ] Tax codes: "Tax Rate Name" – is this always one of a standard list?
- [ ] Currency: Can expenses be in USD/GBP or always EUR?
- [ ] Duplicate detection: If same expense uploaded twice, how to identify?

**TravelPerk:**
- [ ] Cost per traveler: Multi-traveler bookings – is cost per person or total?
- [ ] Non-taxable amount: What triggers this field (corporate rate vs. personal)?

**A3 Innuva:**
- [ ] Concept mapping: Does A3 have fixed concept IDs (e.g., 001=base, 002=overtime) that SIG must match?
- [ ] Salary edits: If FICO corrects salary in SIG, can it be pushed to A3 or manual override only?
- [ ] Versioning: Does A3 API support posting multiple versions of same payroll, or only latest?

---

## 7. ROLE-BASED ACCESS CONTROL (RBAC) MATRIX

**Source:** CierresIntegralesSIG.xlsx "Roles" sheet + requirements doc

### 7.1 Role Definitions

| Role | Scope | Primary Responsibility | Example Users |
|------|-------|------------------------|----------------|
| **Administrador** | Global | System config, user management, security | Silvia (IT) |
| **Dirección** | Global | Strategic approval, final sign-off, KPIs | Eladio, Sergio (executives) |
| **FICO** | Global | Financial validation, compliance, final approval | Lourdes, Lara, Yoana (finance) |
| **RRHH** | Global | Payroll validation, employee data, contracts | Martha (HR) |
| **Facilitador** | Global | Implementation support, training, issue resolution | (TBD) |
| **Interlocutor** | Project-level | Customer liaison, project constraints | (assigned per client) |
| **Gestor** | Project-level | Day-to-day closure management, QA before FICO | (assigned per project) |
| **Backoffice** | Project-level | Data entry, expense coding, manual reconciliation | (assigned per project) |
| **Auxiliar** | Project-level | Read-only, supporting backoffice | (assigned per project) |

### 7.2 Permissions Matrix

| Action | Admin | Dirección | FICO | RRHH | Facilitador | Interlocutor | Gestor | Backoffice | Auxiliar |
|--------|-------|-----------|------|------|-------------|--------------|--------|------------|----------|
| **Closures (Pagos/Facturas)** |
| Ver (View) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ Own | ✅ Own | ✅ Own | ✅ Own |
| Validar | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ Own | ✅ Own | ❌ | ❌ |
| Editar | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ Own | ✅ Own | ✅ Own | ❌ |
| Aprobar (Submit) | ❌ | ✅ | ✅ Final | ✅ | ✅ | ✅ Own | ✅ Own | ❌ | ❌ |
| **Conceptos (Concepts)** |
| Ver | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ | ✅ |
| Crear | ✅ | ❌ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Editar | ✅ | ❌ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Eliminar | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| **Auditoría (Audit Log)** |
| Ver Completo | ✅ | ❌ | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Ver Por Proyecto | ❌ | ❌ | ✅ | ❌ | ✅ | ✅ Own | ✅ Own | ✅ Own | ❌ |
| **Usuarios (Users)** |
| Ver Lista | ✅ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Crear | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Editar | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| **Roles (Permissions)** |
| Ver | ✅ | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ |
| Editar | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| **Cecos, Departamentos (Masters)** |
| Ver | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Editar | ✅ | ❌ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |

**Notes:**
- "Own" = restricted to own project/entity
- "Final" = can approve and send to A3
- Empty (❌) = no access
- RRHH can edit payroll but not invoicing; FICO does both

### 7.3 Data Visibility (Masking Rules)

| Data Field | Visible To | Masked From |
|------------|-----------|------------|
| Salary / Gross Amount | Admin, FICO, RRHH, Dirección, own Gestor/Backoffice | Otros roles |
| Employee NIF | Admin, FICO, RRHH | Interlocutors (non-HR) |
| Invoice Total | Admin, FICO, Dirección, own Gestor | Auxiliar |
| Cost Center (Ceco) | Admin, FICO, Gestor, Dirección | Auxiliar, Interlocutor |
| Expense Receipts | Admin, FICO, RRHH (own employee) | No one else |
| Audit Log Full | Admin, FICO, Facilitadores | Others see only own closures |
| Concept Formulas | Admin, FICO, Facilitadores | Operatives |

### 7.4 Approval Workflow Rules

**Current State Machine (from requirements):**

```
[Draft] 
  ↓ 
[Gestor/Backoffice Submit]
  ↓
[FICO Review & Validate]
  ├→ APPROVE → [Ready for Export] → A3
  └→ REJECT → [Devolver] → [Draft]
    ↓
    [Gestor/Backoffice Edit & Resubmit]
```

**Rules:**
- Multiple Gestores can view and edit simultaneously (no locking)
- Only ONE FICO can approve (first FICO to click approve wins; others see "approved")
- Return can happen unlimited times (no max-rejection threshold)
- After approval by FICO, record is **locked** (read-only)
- Escalation: If in FICO queue > 24h, notify FICO + Dirección

---

## 8. NON-FUNCTIONAL REQUIREMENTS

### 8.1 Performance

- **API Response Time:** < 2s for single-record operations; < 5s for bulk operations
- **Dashboard Load Time:** < 3s (initial load)
- **Bulk Closure Processing:** 1,000 records/minute (depends on formula complexity)
- **Data Ingestion:** All 10 systems' daily/hourly sync complete within 2-hour window

### 8.2 Availability & Uptime

**SLA:** 99.99% uptime (max 4 min downtime/month)

**Critical Hours:** 7:00 AM – 8:00 PM (business hours)

**Maintenance Window:** Off-hours (8 PM – 7 AM) with advance notice (48h)

### 8.3 Security & Compliance

**Encryption:**
- HTTPS only (TLS 1.3+)
- Data at rest: AES-256 (if applicable to cloud storage)
- Database: Azure SQL encryption

**Access Control:**
- Azure AD SSO (sigespana.es domain)
- MFA enforcement for FICO + Admin roles
- API key rotation every 90 days (for system integrations)
- VPN requirement for Celero connection

**Compliance:**
- **GDPR:** Data retention 5 years post-employment; right to deletion honored
- **Fiscal Spain:** All financial records retained per Spanish law (minimum 5 years)
- **ISO 27001:** Information security management (if certification planned)
- **SOC 2 Type II** (optional, but useful for customer confidence)

### 8.4 Data Retention & Archival

- **Active Period:** Current month + 11 previous months hot (fast access)
- **Archive:** Months 13–60 in cold storage (Azure Archive)
- **Deletion:** After 5 years, records eligible for legal hold review; not auto-deleted
- **GDPR Exceptions:** Personal data of terminated employees can be purged upon request (with audit trail)

### 8.5 Disaster Recovery

- **RTO (Recovery Time Objective):** 4 hours (resume operations)
- **RPO (Recovery Point Objective):** 1 hour (max data loss)
- **Backup Frequency:** Daily full backup + hourly transactional logs
- **Backup Location:** Geo-redundant (Azure paired region)

### 8.6 Scalability

- **Concurrent Users:** 25 peak (per G.4 requirement)
- **Data Growth:** ~10,000 closures/year (based on 500 employees × 20 projects), scaling to 20,000+ in Year 2
- **Database:** Azure SQL with auto-scaling (up to 8 vCore)

### 8.7 Monitoring & Logging

- **System Logs:** All API calls logged (request, response, user, timestamp, status)
- **Application Errors:** Alert on failures (email to Admin)
- **Data Quality Alerts:** Alert on ingestion errors (email to BI owner)
- **Performance Monitoring:** Track API response times, database query performance

---

## 9. IMPLEMENTATION ROADMAP IMPLICATIONS

### Phase 0 (Current - Requirements)
- ✅ Document all 10 systems' APIs and data structures
- ✅ Finalize calculation logic per client (FICO ownership)
- ✅ Define exact approval workflow (1-day workshop)
- ✅ Identify Azure AD tenant and service accounts
- ✅ Procure API keys / VPN access for all systems

### Phase 1 (MVP - Backend & Integrations)
- Build core data model (entities, schema)
- Implement Celero PostgreSQL connector + sync
- Implement A3 Innuva, SGPV, Bizneo integrations
- Build calculation engine (support 3–5 payment models)
- Implement basic audit logging
- Deploy to Azure (SQL, App Service)

### Phase 1.5 (Frontend & Workflows)
- Build UI screens (Dashboard, Closures, Approvals)
- Implement approval state machine
- Add role-based access control
- Excel export templates (A3 Innuva, A3 ERP)
- User management + Azure AD SSO

### Phase 2 (Extended Integrations)
- Add Payhawk, TravelPerk, Galán, Mediapost
- Build advanced reporting + analytics
- Implement audit log UI
- Add escalation logic

### Phase 3 (Hardening & Optimization)
- Performance tuning, caching strategy
- Advanced security (MFA, encryption key management)
- GDPR/Compliance testing
- Load testing (1,000+ concurrent)

---

## 10. OPEN QUESTIONS FOR FINAL SIGN-OFF

1. **MVP Scope:** Are all 10 systems required for MVP launch, or can Phase 1 ship with Celero + A3 Innuva + SGPV only?

2. **Approval Authority:** Is FICO the **only** final approver, or can Dirección override FICO's rejection?

3. **Margin Calculation:** Exact formula needed (reference: CierresIntegralesSIG.xlsx "Pagos - Facturación (2)" or detailed walk-through with FICO)?

4. **Retroactive Edits:** Once a month is exported to A3 (outside SIG), can it still be edited in SIG? Or read-only?

5. **Concept Versioning:** If a concept changes mid-month, do previous calculations use old formula or new formula (retroactive)?

6. **Azure AD Role Sync:** Should Azure AD group membership auto-grant SIG role, or is SIG role assigned manually via SIG admin UI (current assumption)?

7. **Logistics Allocation:** For clients with multiple employees in same visit, how is Galán/MDP cost split? Equal? Proportional to hours?

8. **A3 Error Handling:** If A3 rejects a payroll file, who investigates: SIG support or FICO? Can FICO correct in SIG and retry, or is it manual in A3?

---

## Appendix: File References

| File | Key Sheets | Usage |
|------|-----------|-------|
| **CierresIntegralesSIG.xlsx** | CRONOGRAMA, ESTRUCTURA, CONEXIONES, ROLES, ENTIDADES, Pagos-Facturación, Conceptos x Proyecto, CuadroDetallesPagosFact | Master reference for integrations, roles, calculation models, entities |
| **Demo pantallas.xlsx** | Navegación principal, Dashboard, Datos Celero | UI structure + navigation map |
| **SIG-H&K cuestionario (1).docx** | Full questionnaire + responses | Requirements capture, approval workflows, RBAC, compliance |
| **ejemplo ARCHIVO A3NOM.xls** | Pág._1_ (payroll export sample) | Output format for A3 Innuva |

---

**Report Compiled:** May 29, 2026  
**Analyst:** Claude Code Agent  
**Next Step:** Validation workshop with FICO + Operations + IT to confirm assumptions and prioritize MVP scope
