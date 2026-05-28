using FluentValidation;
using SIG.Application.DTOs;

namespace SIG.Application.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

public class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public class ClientCreateRequestValidator : AbstractValidator<ClientCreateRequest>
{
    public ClientCreateRequestValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().Length(2, 200);
        RuleFor(x => x.NIF).NotEmpty().Length(8, 12);
        RuleFor(x => x.ContactoEmail).EmailAddress().When(x => !string.IsNullOrEmpty(x.ContactoEmail));
    }
}

public class ClientUpdateRequestValidator : AbstractValidator<ClientUpdateRequest>
{
    public ClientUpdateRequestValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().Length(2, 200);
        RuleFor(x => x.NIF).NotEmpty().Length(8, 12);
        RuleFor(x => x.ContactoEmail).EmailAddress().When(x => !string.IsNullOrEmpty(x.ContactoEmail));
    }
}

public class ProjectCreateRequestValidator : AbstractValidator<ProjectCreateRequest>
{
    public ProjectCreateRequestValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().Length(2, 200);
        RuleFor(x => x.ClientId).GreaterThan(0);
        RuleFor(x => x.CostCenterIds).NotNull();
        RuleFor(x => x.UserIds).NotNull();
    }
}

public class ProjectUpdateRequestValidator : AbstractValidator<ProjectUpdateRequest>
{
    public ProjectUpdateRequestValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().Length(2, 200);
        RuleFor(x => x.ClientId).GreaterThan(0);
    }
}

public class ActionCreateRequestValidator : AbstractValidator<ActionCreateRequest>
{
    public ActionCreateRequestValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().Length(2, 200);
        RuleFor(x => x.ProjectId).GreaterThan(0);
        RuleFor(x => x.ClientId).GreaterThan(0);
    }
}

public class ActionUpdateRequestValidator : AbstractValidator<ActionUpdateRequest>
{
    public ActionUpdateRequestValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().Length(2, 200);
        RuleFor(x => x.ProjectId).GreaterThan(0);
        RuleFor(x => x.ClientId).GreaterThan(0);
    }
}

public class ConceptCreateRequestValidator : AbstractValidator<ConceptCreateRequest>
{
    public ConceptCreateRequestValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().Length(2, 200);
        RuleFor(x => x.FormulaJson).NotEmpty();
        RuleFor(x => x).Must(c => !c.FechaHasta.HasValue || c.FechaDesde <= c.FechaHasta.Value)
            .WithMessage("FechaDesde debe ser <= FechaHasta");
    }
}

public class ConceptUpdateRequestValidator : AbstractValidator<ConceptUpdateRequest>
{
    public ConceptUpdateRequestValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().Length(2, 200);
        RuleFor(x => x.FormulaJson).NotEmpty();
    }
}

public class UserCreateRequestValidator : AbstractValidator<UserCreateRequest>
{
    public UserCreateRequestValidator()
    {
        RuleFor(x => x.NIF).NotEmpty().Length(8, 12);
        RuleFor(x => x.Nombre).NotEmpty().Length(2, 100);
        RuleFor(x => x.Apellidos).NotEmpty().Length(2, 200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.RoleIds).NotEmpty();
    }
}

public class UserUpdateRequestValidator : AbstractValidator<UserUpdateRequest>
{
    public UserUpdateRequestValidator()
    {
        RuleFor(x => x.NIF).NotEmpty().Length(8, 12);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public class UserPasswordChangeRequestValidator : AbstractValidator<UserPasswordChangeRequest>
{
    public UserPasswordChangeRequestValidator()
    {
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}

public class CostCenterCreateRequestValidator : AbstractValidator<CostCenterCreateRequest>
{
    public CostCenterCreateRequestValidator()
    {
        RuleFor(x => x.Codigo).NotEmpty().Matches(@"^\d{6}$");
        RuleFor(x => x.Nombre).NotEmpty().Length(2, 200);
    }
}

public class CostCenterUpdateRequestValidator : AbstractValidator<CostCenterUpdateRequest>
{
    public CostCenterUpdateRequestValidator()
    {
        RuleFor(x => x.Codigo).NotEmpty().Matches(@"^\d{6}$");
        RuleFor(x => x.Nombre).NotEmpty().Length(2, 200);
    }
}

public class DepartmentCreateRequestValidator : AbstractValidator<DepartmentCreateRequest>
{
    public DepartmentCreateRequestValidator() { RuleFor(x => x.Nombre).NotEmpty().Length(2, 100); }
}

public class DepartmentUpdateRequestValidator : AbstractValidator<DepartmentUpdateRequest>
{
    public DepartmentUpdateRequestValidator() { RuleFor(x => x.Nombre).NotEmpty().Length(2, 100); }
}

public class PeriodCreateRequestValidator : AbstractValidator<PeriodCreateRequest>
{
    public PeriodCreateRequestValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().Length(2, 100);
        RuleFor(x => x).Must(p => p.FechaInicio <= p.FechaFin).WithMessage("FechaInicio debe ser <= FechaFin");
    }
}

public class PeriodUpdateRequestValidator : AbstractValidator<PeriodUpdateRequest>
{
    public PeriodUpdateRequestValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().Length(2, 100);
        RuleFor(x => x).Must(p => p.FechaInicio <= p.FechaFin).WithMessage("FechaInicio debe ser <= FechaFin");
    }
}

public class ClosureCreateRequestValidator : AbstractValidator<ClosureCreateRequest>
{
    public ClosureCreateRequestValidator()
    {
        RuleFor(x => x.ProjectId).GreaterThan(0);
        RuleFor(x => x.PeriodId).GreaterThan(0);
    }
}

public class ClosureRejectRequestValidator : AbstractValidator<ClosureRejectRequest>
{
    public ClosureRejectRequestValidator() { RuleFor(x => x.Motivo).NotEmpty().MaximumLength(2000); }
}
