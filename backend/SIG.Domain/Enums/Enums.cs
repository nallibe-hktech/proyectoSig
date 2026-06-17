namespace SIG.Domain.Enums;

public enum EstadoUsuario { Activo, Inactivo }
public enum EstadoCliente { Activo, Inactivo }
public enum EstadoServicio { Activo, Inactivo }
public enum TipoConcepto { Pago, Factura }
public enum EstadoPeriodo { Abierto, Cerrado, Bloqueado }
public enum EstadoClosure { Borrador, EnAprobacion, Aprobado, Rechazado, Exportado }
public enum ApprovalStep
{
    Grupo = 1,
    Fico = 2,
    SystemExports = 3
}
public enum EstadoApproval { Pendiente, Aprobado, Rechazado }
// Ola 3b (#10): discrimina a qué tipo de cierre (raíz) pertenece una línea/aprobación/alerta.
public enum TipoCierre { Costes, Facturacion }
public enum AuditAction { Create, Update, Delete, Login, Logout, Export, Recalc }
public enum TipoAlerta { Bloqueante, Advertencia }
