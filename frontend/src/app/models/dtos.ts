// DTOs espejo de los definidos en docs/ARQUITECTURA.md §5.1.
// Nombres, casing y tipos respetan fidelidad nominal con el backend .NET.

import type {
  EstadoUsuario, EstadoCliente, EstadoServicio, TipoConcepto, EstadoPeriodo,
  EstadoClosure, ApprovalStep, EstadoApproval, AuditAction, TipoCierre, TipoAlerta,
  EstadoIncidencia
} from './enums';

export type { EstadoClosure, ApprovalStep, EstadoApproval, AuditAction, TipoCierre, TipoAlerta };

// ------------------ Paginación ------------------
export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

// ------------------ Auth ------------------
export interface LoginRequest { email: string; password: string; }
export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  user: UsuarioBriefDto;
}
export interface RefreshRequest { refreshToken: string; }
export interface RefreshResponse { accessToken: string; refreshToken: string; }
export interface LogoutRequest { refreshToken?: string | null; }
export interface UsuarioBriefDto {
  id: number;
  nombre: string;
  apellidos: string;
  email: string;
  roles: string[];
}

// ------------------ Client ------------------
export interface ClientListItemDto {
  id: number;
  nombre: string;
  nif: string;
  ciudad?: string | null;
  estado: EstadoCliente;
  serviceCount: number;
}
export interface ClientDetailDto {
  id: number;
  nombre: string;
  nif: string;
  estado: EstadoCliente;
  direccion?: string | null;
  ciudad?: string | null;
  provincia?: string | null;
  pais?: string | null;
  codigoPostal?: string | null;
  contactoNombre?: string | null;
  contactoEmail?: string | null;
  contactoTelefono?: string | null;
}
export type ClientCreateRequest = Omit<ClientDetailDto, 'id'>;
export type ClientUpdateRequest = ClientCreateRequest;

// Incidencias del cliente (PPT slide 6): tipo (texto libre), explicación y estado, editables y con histórico.
export interface ClienteIncidenciaDto {
  id: number;
  clientId: number;
  tipo: string;
  descripcion: string;
  estado: EstadoIncidencia;
  createdAt: string;
  updatedAt: string;
}
export interface ClienteIncidenciaCreateRequest {
  tipo: string;
  descripcion: string;
  estado?: EstadoIncidencia;
}
export interface ClienteIncidenciaUpdateRequest {
  tipo: string;
  descripcion: string;
  estado: EstadoIncidencia;
}

// Forecast (PPT slide 36): previsión mensual de ventas/margen/GPP por servicio.
export interface ForecastDto {
  id: number;
  serviceId: number;
  anio: number;
  mes: number;
  ventasPrevistas: number;
  margenPrevisto?: number | null;
  personasCampo?: number | null;
}
export interface ForecastUpsertRequest {
  anio: number;
  mes: number;
  ventasPrevistas: number;
  margenPrevisto?: number | null;
  personasCampo?: number | null;
}
export interface ForecastResumenCeldaDto {
  mes: number;
  ventas: number;
  margen: number;
  personas: number;
}
export interface ForecastResumenFilaDto {
  departmentId?: number | null;
  departmentNombre?: string | null;
  clientId: number;
  clientNombre: string;
  meses: ForecastResumenCeldaDto[];
  totalVentas: number;
  totalMargen: number;
  totalPersonas: number;
}
export interface ForecastResumenDto {
  anio: number;
  filas: ForecastResumenFilaDto[];
}

// Informes nativos (PPT slide 23)
export interface ReporteResultadoFilaDto {
  departmentId?: number | null;
  departmentNombre?: string | null;
  clientId: number;
  clientNombre: string;
  serviceId: number;
  serviceNombre: string;
  facturacion: number;
  coste: number;
  margen: number;
}
export interface ReporteResultadoDto {
  anio: number;
  filas: ReporteResultadoFilaDto[];
}
export interface PrevisionRealCeldaDto {
  mes: number;
  ventasPrevistas: number;
  ventasReales: number;
  margenPrevisto: number;
  margenReal: number;
}
export interface PrevisionRealFilaDto {
  departmentId?: number | null;
  departmentNombre?: string | null;
  clientId: number;
  clientNombre: string;
  meses: PrevisionRealCeldaDto[];
  totalVentasPrevistas: number;
  totalVentasReales: number;
  totalMargenPrevisto: number;
  totalMargenReal: number;
}
export interface PrevisionRealDto {
  anio: number;
  filas: PrevisionRealFilaDto[];
}

// ------------------ Service ------------------
export interface ServiceListItemDto {
  id: number;
  nombre: string;
  clientId: number;
  clientNombre: string;
  departmentId?: number | null;
  estado: EstadoServicio;
}
export interface ServiceDetailDto {
  id: number;
  nombre: string;
  clientId: number;
  clientNombre: string;
  departmentId?: number | null;
  estado: EstadoServicio;
  interlocutorNombre?: string | null;
  interlocutorEmail?: string | null;
  interlocutorTelefono?: string | null;
  fechaAlta: string; // DateOnly ISO yyyy-MM-dd
  costCenterIds: number[];
  userIds: number[];
  conceptIds: number[];
}
export interface ServiceCreateRequest {
  nombre: string;
  clientId: number;
  clientNombre?: string;
  departmentId?: number | null;
  estado: EstadoServicio;
  interlocutorNombre?: string | null;
  interlocutorEmail?: string | null;
  interlocutorTelefono?: string | null;
  fechaAlta: string;
  costCenterIds: number[];
  userIds: number[];
  conceptIds: number[];
}
export type ServiceUpdateRequest = ServiceCreateRequest;

// ------------------ Concept ------------------
export interface ConceptListItemDto {
  id: number;
  nombre: string;
  tipo: TipoConcepto;
  fechaDesde: string;
  fechaHasta?: string | null;
}
export interface ConceptDetailDto {
  id: number;
  nombre: string;
  tipo: TipoConcepto;
  fechaDesde: string;
  fechaHasta?: string | null;
  formulaJson: string;
  serviceIds: number[];
  userIds: number[];
}
export interface ConceptCreateRequest {
  nombre: string;
  tipo: TipoConcepto;
  fechaDesde: string;
  fechaHasta?: string | null;
  formulaJson: string;
  serviceIds: number[];
  userIds: number[];
}
export type ConceptUpdateRequest = ConceptCreateRequest;

// ------------------ User / Role / Department / CostCenter ------------------
export interface UserListItemDto {
  id: number;
  nif: string;
  nombre: string;
  apellidos: string;
  email: string;
  estado: EstadoUsuario;
  roles: string[];
}
export interface UserDetailDto {
  id: number;
  nif: string;
  nombre: string;
  apellidos: string;
  email: string;
  estado: EstadoUsuario;
  departmentId?: number | null;
  roleIds: number[];
}
export interface UserCreateRequest {
  nif: string;
  nombre: string;
  apellidos: string;
  email: string;
  password: string;
  estado: EstadoUsuario;
  departmentId?: number | null;
  roleIds: number[];
}
export interface UserUpdateRequest {
  nif: string;
  nombre: string;
  apellidos: string;
  email: string;
  estado: EstadoUsuario;
  departmentId?: number | null;
  roleIds: number[];
}
export interface UserPasswordChangeRequest { newPassword: string; }

export interface RoleDto { id: number; nombre: string; descripcion?: string | null; }
export interface DepartmentDto { id: number; nombre: string; }
export interface DepartmentCreateRequest { nombre: string; }
export type DepartmentUpdateRequest = DepartmentCreateRequest;
export interface CostCenterDto { id: number; codigo: string; nombre: string; }
export interface CostCenterCreateRequest { codigo: string; nombre: string; }
export type CostCenterUpdateRequest = CostCenterCreateRequest;

// ------------------ Period ------------------
export interface PeriodDto {
  id: number;
  nombre: string;
  fechaInicio: string;
  fechaFin: string;
  diaPago: number;
  estado: EstadoPeriodo;
}
export interface PeriodCreateRequest {
  nombre: string;
  fechaInicio: string;
  fechaFin: string;
  diaPago: number;
}
export type PeriodUpdateRequest = PeriodCreateRequest;

// ------------------ Closure / Approval ------------------
export interface ClosureListItemDto {
  id: number;
  serviceId: number;
  serviceNombre: string;
  periodId: number;
  periodNombre: string;
  periodo?: string; // Alias para periodNombre
  clientId?: number;
  clientNombre?: string;
  costeTotal: number;
  facturacionTotal: number;
  margen: number;
  estado: EstadoClosure;
  pasoActual: ApprovalStep;
}
export interface ClosureLineDto {
  id: number;
  conceptId: number;
  conceptNombre: string;
  userId?: number | null;
  userNombre?: string | null;
  importe: number;
  tipo: TipoConcepto;
  tieneIncidencia: boolean;
  rowVersion: number;
  esManual?: boolean;
  importeOriginal?: number | null;
  motivoManual?: string | null;
  sourceDataSummary?: string | null;
  inputMetadata?: string | null;
}
export interface ClosureLineOverrideRequest { importe: number; motivo: string; }
export interface ClosureLineIncentivoRequest { conceptId: number; importe: number; motivo: string; userId?: number | null; }

// Alias para compatibilidad (algunos componentes usan ClosureLine en lugar de ClosureLineDto)
export type ClosureLine = ClosureLineDto;
export interface ApprovalDto {
  id: number;
  paso: ApprovalStep;
  roleId: number;
  roleNombre: string;
  estado: EstadoApproval;
  userId?: number | null;
  userNombre?: string | null;
  motivo?: string | null;
  fechaDecision?: string | null;
}
export interface ClosureAlertaDto {
  id: number;
  tipo: TipoAlerta;
  codigo: string;
  descripcion: string;
  detalle?: string | null;
  confirmada: boolean;
  confirmadaPorNombre?: string | null;
  fechaConfirmacion?: string | null;
  closureId?: number;
  serviceId?: number;
  closureNombre?: string;
}
export interface ClosureDetailDto {
  id: number;
  serviceId: number;
  serviceNombre: string;
  periodId: number;
  periodNombre: string;
  costeTotal: number;
  facturacionTotal: number;
  margen: number;
  estado: EstadoClosure;
  pasoActual: ApprovalStep;
  comentarios?: string | null;
  rowVersion: number;
  lines: ClosureLineDto[];
  approvals: ApprovalDto[];
  alertas: ClosureAlertaDto[];
}
export interface ClosureCreateRequest { serviceId: number; periodId: number; comentarios?: string | null; }
export interface ClosureRecalcRequest { comentarios?: string | null; }
export interface ClosureApproveRequest { comentarios?: string | null; }
export interface ClosureRejectRequest { motivo: string; }

// ───────────── Ola 3b (#10): cierres separados (Costes mensual / Facturación plurianual) ─────────────
// Cada cierre evalúa SOLO sus conceptos (Costes -> Pago, Facturación -> Factura) y expone un único Total.
// El margen es "al vuelo" (facturación − costes) y se calcula en el Dashboard, no en el cierre.
export interface CierreListItemDto {
  id: number;
  tipoCierre: TipoCierre;
  serviceId: number;
  serviceNombre: string;
  periodId: number;
  periodNombre: string;
  total: number;
  estado: EstadoClosure;
  pasoActual: ApprovalStep;
}
export interface CierreDetailDto {
  id: number;
  tipoCierre: TipoCierre;
  serviceId: number;
  serviceNombre: string;
  periodId: number;
  periodNombre: string;
  total: number;
  estado: EstadoClosure;
  pasoActual: ApprovalStep;
  comentarios?: string | null;
  rowVersion: number;
  lines: ClosureLineDto[];
  approvals: ApprovalDto[];
  alertas: ClosureAlertaDto[];
}
export interface CierreCreateRequest { serviceId: number; periodId: number; comentarios?: string | null; }
export interface CierreRecalcRequest { comentarios?: string | null; }
export interface CierreLineOverrideRequest { importe: number; motivo: string; }
export interface CierreLineIncentivoRequest { conceptId: number; importe: number; motivo: string; userId?: number | null; }
export interface CierreApproveRequest { comentarios?: string | null; }
export interface CierreRejectRequest { motivo: string; }

// Panel de aprobaciones agregando AMBOS tipos de cierre (cada item indica su TipoCierre).
export interface CierrePanelItemDto {
  cierreId: number;
  tipoCierre: TipoCierre;
  serviceId: number;
  serviceNombre: string;
  clientId: number;
  clientNombre: string;
  periodId: number;
  periodNombre: string;
  estado: EstadoClosure;
  pasoActual: ApprovalStep;
  pasoActualRol: string;
  total: number;
  updatedAt: string;
}
export interface CierreHistoryDto {
  id: number;
  cierreId: number;
  tipoCierre: TipoCierre;
  userId: number;
  userNombre: string;
  pasoOrigen: ApprovalStep;
  pasoDestino: ApprovalStep;
  accion: string;
  motivo?: string | null;
  timestamp: string;
}

// ------------------ Contratos A3 Innuva (Ola 2 #2 — contratos de un día) ------------------
export interface ContratoUnDiaDto {
  id: number;
  contratoIdExterno: string;
  nif: string;
  fechaInicio: string;
  fechaFin: string;
  importeBruto: number;
  userId?: number | null;
  userNombre?: string | null;
  ignoradoEnCierre: boolean;
  motivoIgnorar?: string | null;
}
export interface ContratoIgnorarRequest { ignorar: boolean; motivo?: string | null; }

export interface ApprovalFilterRequest {
  periodId?: number | null;
  clientId?: number | null;
  costCenterId?: number | null;
  estado?: EstadoClosure | null;
  userId?: number | null;
  departmentId?: number | null;
  tipo?: TipoConcepto | null;
  conceptId?: number | null;
  page?: number;
  pageSize?: number;
}
export interface ApprovalPanelItemDto {
  closureId: number;
  serviceId: number;
  serviceNombre: string;
  clientId: number;
  clientNombre: string;
  periodId: number;
  periodNombre: string;
  estado: EstadoClosure;
  pasoActual: ApprovalStep;
  pasoActualRol: string;
  margen: number;
  updatedAt: string;
}
export interface ApprovalHistoryDto {
  id: number;
  closureId: number;
  userId: number;
  userNombre: string;
  pasoOrigen: ApprovalStep;
  pasoDestino: ApprovalStep;
  accion: string;
  motivo?: string | null;
  timestamp: string;
}

// ------------------ Dashboard ------------------
export interface KpiClienteDto {
  clientId: number;
  nombre: string;
  facturacion: number;
  coste: number;
  margen: number;
  pctTotal: number;
}
export interface EvolucionPeriodoDto {
  periodNombre: string;
  facturacion: number;
  coste: number;
  margen: number;
}
export interface DashboardKpisDto {
  periodId: number;
  periodNombre: string;
  cierresCompletados: number;
  cierresPendientes: number;
  cierresCostesCompletados: number;
  cierresCostesPendientes: number;
  cierresFacturacionCompletados: number;
  cierresFacturacionPendientes: number;
  facturacionTotal: number;
  costeTotal: number;
  margen: number;
  margenPct: number;
  desglosePorCliente: KpiClienteDto[];
  evolucion: EvolucionPeriodoDto[];
}
export interface DashboardAvisoDto {
  tipo: string;
  descripcion: string;
  entityId?: number | null;
}
export interface MiServicioDto {
  serviceId: number;
  nombre: string;
  clientId: number;
  clientNombre: string;
  closureId?: number | null;
  estado?: EstadoClosure | null;
  pasoActual?: ApprovalStep | null;
  costeTotal?: number | null;
  facturacionTotal?: number | null;
  margen?: number | null;
}

// ------------------ Calculation / Sync / Audit / Variable ------------------
export interface CalculationDetailDto {
  closureLineId: number;
  conceptId: number;
  conceptNombre: string;
  formulaSnapshotJson: string;
  inputsJson: string;
  resultado: number;
  incidencias?: string | null;
  sistemaOrigen: string;
  timestamp: string;
}
export interface SyncResultDto {
  sistema: string;
  exito: boolean;
  registrosInsertados: number;
  registrosActualizados: number;
  registrosError: number;
  mensajeError?: string | null;
  fechaUltimaSincronizacion?: string | null;
}
export interface ProcessingResultDto {
  timestamp: string;
  systems: Record<string, { processed: number; errors: number }>;
  totalProcessed: number;
  totalErrors: number;
  error?: string | null;
}
export interface AuditLogFilterRequest {
  userId?: number | null;
  entityType?: string | null;
  action?: AuditAction | null;
  desde?: string | null;
  hasta?: string | null;
  page?: number;
  pageSize?: number;
}
export interface AuditLogDto {
  id: number;
  userId?: number | null;
  userNombre?: string | null;
  entityType: string;
  entityId: string;
  action: AuditAction;
  oldValueJson?: string | null;
  newValueJson?: string | null;
  timestamp: string;
  ip?: string | null;
}

export interface VariableMapeo { respuesta: string; valor: number; }
export interface VariableDto {
  id: number;
  nombre: string;
  questionIdExterno: string;
  mapeoValoresJson: string;
}
export interface VariableCreateRequest {
  nombre: string;
  questionIdExterno: string;
  mapeoValoresJson: string;
}
export type VariableUpdateRequest = VariableCreateRequest;

// ------------------ Tarifa ------------------
export interface TarifaServicioDto {
  id: number;
  serviceId: number;
  nombre: string;
  valor: number;
  unidad?: string | null;
  fechaDesde: string; // DateOnly ISO yyyy-MM-dd
  fechaHasta?: string | null; // DateOnly ISO yyyy-MM-dd
}
export interface TarifaServicioCreateRequest {
  nombre: string;
  valor: number;
  unidad?: string | null;
  fechaDesde: string; // DateOnly ISO yyyy-MM-dd
  fechaHasta?: string | null; // DateOnly ISO yyyy-MM-dd
}
export type TarifaServicioUpdateRequest = TarifaServicioCreateRequest;

// ------------------ Presupuesto ------------------
export interface PresupuestoServicioDto {
  id: number;
  serviceId: number;
  periodId?: number | null;
  tipo: TipoConcepto;
  importe: number;
  descripcion?: string | null;
}
export interface PresupuestoServicioCreateRequest {
  periodId?: number | null;
  tipo: TipoConcepto;
  importe: number;
  descripcion?: string | null;
}
export type PresupuestoServicioUpdateRequest = PresupuestoServicioCreateRequest;

// ------------------ Formula AST ------------------
export type FormulaNode =
  | { type: 'Number'; value: number }
  | { type: 'Variable'; variableId: number }
  | { type: 'Source'; entity: SourceEntity; field?: string | null; filters: FormulaFilter[] }
  | { type: 'Aggregate'; op: AggregateOp; source: FormulaNode; field?: string | null; distinct?: string | null }
  | { type: 'BinaryOp'; op: BinaryOpKind; left: FormulaNode; right: FormulaNode }
  | { type: 'Modifier'; kind: ModifierKind; threshold: number; inner: FormulaNode }
  | { type: 'Tramos'; cantidad: FormulaNode; tramos: Tramo[] }
  | { type: 'ConceptRef'; conceptIds: number[] };

export type SourceEntity = 'GastosPayHawk' | 'VisitasCelero' | 'HorasBizneo' | 'HorasIntratime' | 'VisitasSgpv' | 'TarifasServicio';
export type AggregateOp = 'Sum' | 'Count' | 'Min' | 'Max';
export type BinaryOpKind = 'Add' | 'Sub' | 'Mul' | 'Div' | 'Pct';
export type ModifierKind = 'Min' | 'Max' | 'FloorZero' | 'Franquicia';
export interface Tramo {
  hasta: number | null;
  precio: number;
}
export type FilterOp = 'Eq' | 'Neq' | 'Gt' | 'Gte' | 'Lt' | 'Lte' | 'In';
export interface FormulaFilter {
  field: string;
  op: FilterOp;
  value: number | string;
}
