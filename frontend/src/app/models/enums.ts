// Enums espejo de los del backend (.NET). Se serializan como strings.
// Backend está configurado con JsonStringEnumConverter — los nombres deben coincidir exactamente.

export type EstadoUsuario = 'Activo' | 'Inactivo';

export type EstadoProyecto = 'Activo' | 'Pausado' | 'Cerrado';

export type EstadoAccion = 'Activa' | 'Inactiva';

export type TipoConcepto = 'Pago' | 'Factura';

export type EstadoPeriodo = 'Abierto' | 'Cerrado' | 'Bloqueado';

export type EstadoClosure =
  | 'Borrador'
  | 'EnAprobacion'
  | 'Aprobado'
  | 'Rechazado'
  | 'Exportado';

export type ApprovalStep =
  | 'ProjectManager'
  | 'Backoffice'
  | 'Fico'
  | 'Direction'
  | 'SystemExports';

export type EstadoApproval = 'Pendiente' | 'Aprobado' | 'Rechazado';

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
  | 'Reader';

// Helper para badge UI a partir de un Closure
export function badgeClassFromClosure(estado: EstadoClosure, paso: ApprovalStep): string {
  if (estado === 'Aprobado') return 'approved';
  if (estado === 'Rechazado') return 'rejected';
  if (estado === 'Exportado') return 'closed';
  switch (paso) {
    case 'ProjectManager': return 'pending-pm';
    case 'Backoffice': return 'pending-backoffice';
    case 'Fico': return 'pending-fico';
    case 'Direction': return 'pending-direction';
    case 'SystemExports': return 'closed';
    default: return 'closed';
  }
}

export function badgeLabelFromClosure(estado: EstadoClosure, paso: ApprovalStep): string {
  if (estado === 'Aprobado') return 'Aprobado';
  if (estado === 'Rechazado') return 'Rechazado';
  if (estado === 'Exportado') return 'Exportado';
  switch (paso) {
    case 'ProjectManager': return 'Pdte. PM';
    case 'Backoffice': return 'Pdte. Backoffice';
    case 'Fico': return 'Pdte. Fico';
    case 'Direction': return 'Pdte. Dirección';
    case 'SystemExports': return 'Cerrado';
    default: return 'Pendiente';
  }
}
