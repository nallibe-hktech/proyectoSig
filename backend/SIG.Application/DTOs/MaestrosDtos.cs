using SIG.Domain.Enums;

namespace SIG.Application.DTOs;

// Client
public record ClientListItemDto(int Id, string Nombre, string NIF, string? Ciudad, int ProjectCount);
public record ClientDetailDto(int Id, string Nombre, string NIF, string? Direccion, string? Ciudad, string? Provincia, string? Pais, string? CodigoPostal, string? ContactoNombre, string? ContactoEmail, string? ContactoTelefono);
public record ClientCreateRequest(string Nombre, string NIF, string? Direccion, string? Ciudad, string? Provincia, string? Pais, string? CodigoPostal, string? ContactoNombre, string? ContactoEmail, string? ContactoTelefono);
public record ClientUpdateRequest(string Nombre, string NIF, string? Direccion, string? Ciudad, string? Provincia, string? Pais, string? CodigoPostal, string? ContactoNombre, string? ContactoEmail, string? ContactoTelefono);

// Project
public record ProjectListItemDto(int Id, string Nombre, int ClientId, string ClientNombre, EstadoProyecto Estado, DateOnly FechaAlta);
public record ProjectDetailDto(int Id, string Nombre, int ClientId, string ClientNombre, EstadoProyecto Estado, string? InterlocutorNombre, string? InterlocutorEmail, string? InterlocutorTelefono, DateOnly FechaAlta, int[] CostCenterIds, int[] UserIds);
public record ProjectCreateRequest(string Nombre, int ClientId, EstadoProyecto Estado, string? InterlocutorNombre, string? InterlocutorEmail, string? InterlocutorTelefono, DateOnly FechaAlta, int[] CostCenterIds, int[] UserIds);
public record ProjectUpdateRequest(string Nombre, int ClientId, EstadoProyecto Estado, string? InterlocutorNombre, string? InterlocutorEmail, string? InterlocutorTelefono, DateOnly FechaAlta, int[] CostCenterIds, int[] UserIds);

// Action
public record ActionListItemDto(int Id, string Nombre, int ProjectId, string ProjectNombre, int ClientId, int? DepartmentId, EstadoAccion Estado);
public record ActionDetailDto(int Id, string Nombre, int ProjectId, int ClientId, int? DepartmentId, EstadoAccion Estado, int[] ConceptIds, int[] UserIds);
public record ActionCreateRequest(string Nombre, int ProjectId, int ClientId, int? DepartmentId, EstadoAccion Estado, int[] ConceptIds, int[] UserIds);
public record ActionUpdateRequest(string Nombre, int ProjectId, int ClientId, int? DepartmentId, EstadoAccion Estado, int[] ConceptIds, int[] UserIds);

// Concept
public record ConceptListItemDto(int Id, string Nombre, TipoConcepto Tipo, DateOnly FechaDesde, DateOnly? FechaHasta);
public record ConceptDetailDto(int Id, string Nombre, TipoConcepto Tipo, DateOnly FechaDesde, DateOnly? FechaHasta, string FormulaJson, int[] ActionIds, int[] UserIds);
public record ConceptCreateRequest(string Nombre, TipoConcepto Tipo, DateOnly FechaDesde, DateOnly? FechaHasta, string FormulaJson, int[] ActionIds, int[] UserIds);
public record ConceptUpdateRequest(string Nombre, TipoConcepto Tipo, DateOnly FechaDesde, DateOnly? FechaHasta, string FormulaJson, int[] ActionIds, int[] UserIds);
public record ValidarFormulaRequest(string FormulaJson);
public record ValidarFormulaResponse(bool Ok, string[] Errores);

// Variable
public record VariableDto(int Id, string Nombre, string QuestionIdExterno, string MapeoValoresJson);
public record VariableCreateRequest(string Nombre, string QuestionIdExterno, string MapeoValoresJson);
public record VariableUpdateRequest(string Nombre, string QuestionIdExterno, string MapeoValoresJson);

// User
public record UserListItemDto(int Id, string NIF, string Nombre, string Apellidos, string Email, EstadoUsuario Estado, string[] Roles);
public record UserDetailDto(int Id, string NIF, string Nombre, string Apellidos, string Email, EstadoUsuario Estado, int? DepartmentId, int[] RoleIds);
public record UserCreateRequest(string NIF, string Nombre, string Apellidos, string Email, string Password, EstadoUsuario Estado, int? DepartmentId, int[] RoleIds);
public record UserUpdateRequest(string NIF, string Nombre, string Apellidos, string Email, EstadoUsuario Estado, int? DepartmentId, int[] RoleIds);
public record UserPasswordChangeRequest(string NewPassword);

// Role / Department / CostCenter
public record RoleDto(int Id, string Nombre, string? Descripcion);
public record DepartmentDto(int Id, string Nombre);
public record DepartmentCreateRequest(string Nombre);
public record DepartmentUpdateRequest(string Nombre);
public record CostCenterDto(int Id, string Codigo, string Nombre);
public record CostCenterCreateRequest(string Codigo, string Nombre);
public record CostCenterUpdateRequest(string Codigo, string Nombre);

// Period
public record PeriodDto(int Id, string Nombre, DateOnly FechaInicio, DateOnly FechaFin, EstadoPeriodo Estado);
public record PeriodCreateRequest(string Nombre, DateOnly FechaInicio, DateOnly FechaFin);
public record PeriodUpdateRequest(string Nombre, DateOnly FechaInicio, DateOnly FechaFin);
