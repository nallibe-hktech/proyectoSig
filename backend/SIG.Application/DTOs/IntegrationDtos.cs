namespace SIG.Application.DTOs;

public record CeleroVisitaDto(string VisitaIdExterno, int UserId, int ProjectId, int ActionId, DateOnly Fecha, int TipoVisita, int PuntoMontado);
public record BizneoEmpleadoDto(string EmpleadoIdExterno, string NIF, string Nombre, string? Departamento);
public record BizneoHoraDto(string RegistroIdExterno, int UserId, int ProjectId, DateOnly Fecha, decimal Horas);
public record IntratimeFichajeDto(string FichajeIdExterno, int UserId, DateTime Entrada, DateTime? Salida);
public record PayHawkGastoDto(string GastoIdExterno, int UserId, int ProjectId, DateOnly Fecha, decimal Importe, string Categoria);
