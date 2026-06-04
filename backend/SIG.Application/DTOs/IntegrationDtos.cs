namespace SIG.Application.DTOs;

public record CeleroVisitaDto(
    string VisitaIdExterno,
    string ResourceNif,
    string ServiceName,
    string MissionName,
    DateOnly Fecha);
public record BizneoEmpleadoDto(string EmpleadoIdExterno, string NIF, string Nombre, string? Departamento);
public record BizneoHoraDto(string RegistroIdExterno, int UserId, int ProjectId, DateOnly Fecha, decimal Horas);
public record IntratimeFichajeDto(string FichajeIdExterno, int UserId, DateTime Entrada, DateTime? Salida);
public record PayHawkGastoDto(string GastoIdExterno, int UserId, int ProjectId, DateOnly Fecha, decimal Importe, string Categoria);
public record SgpvVisitaDto(
    string VisitaIdExterno,
    string ResourceNif,
    string CentroId,
    string? CentroNombre,
    string? ServiceName,
    DateOnly Fecha,
    decimal? HorasDuracion);
public record A3InnuvaEmpleadoDto(string EmpleadoIdExterno, string NIF, string Nombre, string? Departamento, decimal? SueldoMensual);
public record A3InnuvaGenericoDto(string IdExterno, string Nombre, string Tipo, DateTime? FechaRegistro);
public record TravelPerkViajeDto(string ViajeIdExterno, string Solicitante, DateOnly FechaInicio, DateOnly? FechaFin, decimal Presupuesto, string Estado);
