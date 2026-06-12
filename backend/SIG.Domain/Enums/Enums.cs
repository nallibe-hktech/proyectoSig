namespace SIG.Domain.Enums;

public enum EstadoUsuario { Activo, Inactivo }
public enum EstadoProyecto { Activo, Pausado, Cerrado }
public enum EstadoAccion { Activa, Inactiva }
public enum TipoConcepto { Pago, Factura }
public enum EstadoPeriodo { Abierto, Cerrado, Bloqueado }
public enum EstadoClosure { Borrador, EnAprobacion, Aprobado, Rechazado, Exportado }
public enum ApprovalStep
{
    ProjectManager = 1,
    Backoffice = 2,
    Fico = 3,
    Direction = 4,
    SystemExports = 5
}
public enum EstadoApproval { Pendiente, Aprobado, Rechazado }
public enum AuditAction { Create, Update, Delete, Login, Logout, Export, Recalc }
public enum TipoAlerta { Bloqueante, Advertencia }
