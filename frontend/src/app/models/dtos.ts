// DTOs espejo de los definidos en docs/ARQUITECTURA.md §5.1.
// Nombres, casing y tipos respetan fidelidad nominal con el backend .NET.

import type {
  EstadoUsuario, EstadoProyecto, EstadoAccion, TipoConcepto, EstadoPeriodo,
  EstadoClosure, ApprovalStep, EstadoApproval, AuditAction
} from './enums';

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
  projectCount: number;
}
export interface ClientDetailDto {
  id: number;
  nombre: string;
  nif: string;
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

// ------------------ Project ------------------
export interface ProjectListItemDto {
  id: number;
  nombre: string;
  clientId: number;
  clientNombre: string;
  estado: EstadoProyecto;
  fechaAlta: string; // DateOnly ISO yyyy-MM-dd
}
export interface ProjectDetailDto {
  id: number;
  nombre: string;
  clientId: number;
  clientNombre: string;
  estado: EstadoProyecto;
  interlocutorNombre?: string | null;
  interlocutorEmail?: string | null;
  interlocutorTelefono?: string | null;
  fechaAlta: string;
  costCenterIds: number[];
  userIds: number[];
}
export interface ProjectCreateRequest {
  nombre: string;
  clientId: number;
  estado: EstadoProyecto;
  interlocutorNombre?: string | null;
  interlocutorEmail?: string | null;
  interlocutorTelefono?: string | null;
  fechaAlta: string;
  costCenterIds: number[];
  userIds: number[];
}
export type ProjectUpdateRequest = ProjectCreateRequest;

// ------------------ Action ------------------
export interface ActionListItemDto {
  id: number;
  nombre: string;
  projectId: number;
  projectNombre: string;
  clientId: number;
  departmentId?: number | null;
  estado: EstadoAccion;
}
export interface ActionDetailDto {
  id: number;
  nombre: string;
  projectId: number;
  clientId: number;
  departmentId?: number | null;
  estado: EstadoAccion;
  conceptIds: number[];
  userIds: number[];
}
export interface ActionCreateRequest {
  nombre: string;
  projectId: number;
  clientId: number;
  departmentId?: number | null;
  estado: EstadoAccion;
  conceptIds: number[];
  userIds: number[];
}
export type ActionUpdateRequest = ActionCreateRequest;

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
  actionIds: number[];
  userIds: number[];
}
export interface ConceptCreateRequest {
  nombre: string;
  tipo: TipoConcepto;
  fechaDesde: string;
  fechaHasta?: string | null;
  formulaJson: string;
  actionIds: number[];
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
  estado: EstadoPeriodo;
}
export interface PeriodCreateRequest {
  nombre: string;
  fechaInicio: string;
  fechaFin: string;
}
export type PeriodUpdateRequest = PeriodCreateRequest;

// ------------------ Closure / Approval ------------------
export interface ClosureListItemDto {
  id: number;
  projectId: number;
  projectNombre: string;
  periodId: number;
  periodNombre: string;
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
}
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
export interface ClosureDetailDto {
  id: number;
  projectId: number;
  projectNombre: string;
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
}
export interface ClosureCreateRequest { projectId: number; periodId: number; comentarios?: string | null; }
export interface ClosureRecalcRequest { comentarios?: string | null; }
export interface ClosureApproveRequest { comentarios?: string | null; }
export interface ClosureRejectRequest { motivo: string; }

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
  projectId: number;
  projectNombre: string;
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
export interface DashboardKpisDto {
  periodId: number;
  periodNombre: string;
  cierresCompletados: number;
  cierresPendientes: number;
  facturacionTotal: number;
  costeTotal: number;
  margen: number;
}
export interface DashboardAvisoDto {
  tipo: string;
  descripcion: string;
  entityId?: number | null;
}
export interface MiProyectoDto {
  projectId: number;
  nombre: string;
  clientId: number;
  clientNombre: string;
  closureId?: number | null;
  estado?: EstadoClosure | null;
  pasoActual?: ApprovalStep | null;
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
  filasInsertadas: number;
  filasDuplicadasIgnoradas: number;
  filasError: number;
  fechaUltimaSincronizacion: string;
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
export interface TarifaProyectoDto {
  id: number;
  projectId: number;
  nombre: string;
  valor: number;
  unidad?: string | null;
  fechaDesde: string; // DateOnly ISO yyyy-MM-dd
  fechaHasta?: string | null; // DateOnly ISO yyyy-MM-dd
}
export interface TarifaProyectoCreateRequest {
  nombre: string;
  valor: number;
  unidad?: string | null;
  fechaDesde: string; // DateOnly ISO yyyy-MM-dd
  fechaHasta?: string | null; // DateOnly ISO yyyy-MM-dd
}
export type TarifaProyectoUpdateRequest = TarifaProyectoCreateRequest;

// ------------------ Presupuesto ------------------
export interface PresupuestoProyectoDto {
  id: number;
  projectId: number;
  periodId?: number | null;
  tipo: TipoConcepto;
  importe: number;
  descripcion?: string | null;
}
export interface PresupuestoProyectoCreateRequest {
  periodId?: number | null;
  tipo: TipoConcepto;
  importe: number;
  descripcion?: string | null;
}
export type PresupuestoProyectoUpdateRequest = PresupuestoProyectoCreateRequest;

// ------------------ Formula AST ------------------
export type FormulaNode =
  | { type: 'Number'; value: number }
  | { type: 'Variable'; variableId: number }
  | { type: 'Source'; entity: SourceEntity; field?: string | null; filters: FormulaFilter[] }
  | { type: 'Aggregate'; op: AggregateOp; source: FormulaNode; field?: string | null }
  | { type: 'BinaryOp'; op: BinaryOpKind; left: FormulaNode; right: FormulaNode };

export type SourceEntity = 'GastosPayHawk' | 'VisitasCelero' | 'HorasBizneo' | 'HorasIntratime';
export type AggregateOp = 'Sum' | 'Count' | 'Min' | 'Max';
export type BinaryOpKind = 'Add' | 'Sub' | 'Mul' | 'Div' | 'Pct';
export type FilterOp = 'Eq' | 'Neq' | 'Gt' | 'Gte' | 'Lt' | 'Lte' | 'In';
export interface FormulaFilter {
  field: string;
  op: FilterOp;
  value: number | string;
}
