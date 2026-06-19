// Enums espejo de los del backend (.NET). Se serializan como strings.
// Backend está configurado con JsonStringEnumConverter — los nombres deben coincidir exactamente.

export type EstadoUsuario = 'Activo' | 'Inactivo';

export type EstadoCliente = 'Activo' | 'Inactivo';

export type EstadoServicio = 'Activo' | 'Inactivo';

// Incidencias del cliente (PPT slide 6).
export type EstadoIncidencia = 'Abierta' | 'EnProceso' | 'Resuelta';

export type TipoConcepto = 'Pago' | 'Factura';

export type EstadoPeriodo = 'Abierto' | 'Cerrado' | 'Bloqueado';

// Config. Presupuesto (prototipo 24/28): cada partida es Anual o Total acción.
export type TipoPartidaPresupuesto = 'Anual' | 'TotalAccion';

export type EstadoClosure =
  | 'Borrador'
  | 'EnAprobacion'
  | 'Aprobado'
  | 'Rechazado'
  | 'Exportado';

export type ApprovalStep =
  | 'Grupo'
  | 'Fico'
  | 'SystemExports';

export type EstadoApproval = 'Pendiente' | 'Aprobado' | 'Rechazado';

// Ola 3b (#10): discrimina la raíz de cierre (mensual de costes / plurianual de facturación).
export type TipoCierre = 'Costes' | 'Facturacion';

export type TipoAlerta = 'Bloqueante' | 'Advertencia';

export type AuditAction =
  | 'Create'
  | 'Update'
  | 'Delete'
  | 'Login'
  | 'Logout'
  | 'Export'
  | 'Recalc';

export type Rol =
  | 'Administrator'
  | 'Direction'
  | 'Fico'
  | 'Backoffice'
  | 'ProjectManager'
  | 'Auditor'
  | 'Reader'
  // Ola 3a (#1): roles globales que conforman el "grupo" del servicio.
  | 'Facilitador'
  | 'Interlocutor'
  | 'Gestor';

// Helper para badge UI a partir de un Closure
export function badgeClassFromClosure(estado: EstadoClosure, paso: ApprovalStep): string {
  if (estado === 'Aprobado') return 'approved';
  if (estado === 'Rechazado') return 'rejected';
  if (estado === 'Exportado') return 'closed';
  switch (paso) {
    case 'Grupo': return 'pending-grupo';
    case 'Fico': return 'pending-fico';
    case 'SystemExports': return 'closed';
    default: return 'closed';
  }
}

export function badgeLabelFromClosure(estado: EstadoClosure, paso: ApprovalStep): string {
  if (estado === 'Aprobado') return 'Aprobado';
  if (estado === 'Rechazado') return 'Rechazado';
  if (estado === 'Exportado') return 'Exportado';
  switch (paso) {
    case 'Grupo': return 'Pdte. Grupo';
    case 'Fico': return 'Pdte. FICO';
    case 'SystemExports': return 'Cerrado';
    default: return 'Pendiente';
  }
}
