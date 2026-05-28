namespace SIG.Domain.Exceptions;

public abstract class DomainException : Exception
{
    public abstract string Code { get; }
    public abstract int HttpStatusCode { get; }
    protected DomainException(string message) : base(message) { }
}

public sealed class EntityNotFoundException : DomainException
{
    public override string Code => "entity_not_found";
    public override int HttpStatusCode => 404;
    public EntityNotFoundException(string entity, object id) : base($"{entity} con id {id} no encontrado.") { }
}

public sealed class NotOwnerException : DomainException
{
    public override string Code => "not_owner";
    public override int HttpStatusCode => 403;
    public NotOwnerException() : base("El usuario no tiene acceso a esta entidad.") { }
}

public sealed class ConcurrencyConflictException : DomainException
{
    public override string Code => "concurrency_conflict";
    public override int HttpStatusCode => 412;
    public ConcurrencyConflictException() : base("La entidad fue modificada por otra operación. Recarga y reintenta.") { }
}

public sealed class PeriodClosedException : DomainException
{
    public override string Code => "period_closed";
    public override int HttpStatusCode => 409;
    public PeriodClosedException(string periodName) : base($"El período {periodName} está cerrado o bloqueado.") { }
}

public sealed class InvalidApprovalTransitionException : DomainException
{
    public override string Code => "invalid_approval_transition";
    public override int HttpStatusCode => 409;
    public InvalidApprovalTransitionException(string detalle) : base(detalle) { }
}

public sealed class FormulaInvalidException : DomainException
{
    public override string Code => "formula_invalid";
    public override int HttpStatusCode => 400;
    public FormulaInvalidException(string detalle) : base(detalle) { }
}

public sealed class DuplicateException : DomainException
{
    public override string Code => "duplicate";
    public override int HttpStatusCode => 409;
    public DuplicateException(string detalle) : base(detalle) { }
}

public sealed class DependenciesExistException : DomainException
{
    public override string Code => "dependencies_exist";
    public override int HttpStatusCode => 409;
    public DependenciesExistException(int count) : base($"No se puede eliminar: existen {count} dependencias.") { }
}

public sealed class ClosureNotApprovedException : DomainException
{
    public override string Code => "closure_not_approved";
    public override int HttpStatusCode => 409;
    public ClosureNotApprovedException() : base("Solo se pueden exportar cierres en estado Aprobado.") { }
}

public sealed class IntegrationException : DomainException
{
    public override string Code => "integration_error";
    public override int HttpStatusCode => 502;
    public IntegrationException(string sistema, string detalle) : base($"Error en sistema externo {sistema}: {detalle}") { }
}

public sealed class UnauthorizedException : DomainException
{
    public override string Code => "unauthorized";
    public override int HttpStatusCode => 401;
    public UnauthorizedException(string detalle = "Credenciales inválidas") : base(detalle) { }
}
