using SIG.Domain.Common;

namespace SIG.Domain.Entities.Staging;

public class StagingCeleroVisita : IStagingRow
{
    public int Id { get; set; }

    // Datos crudos de Celero
    public string VisitaIdExterno { get; set; } = null!;
    public string ResourceNif { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public string MissionName { get; set; } = "";
    public DateOnly Fecha { get; set; }

    // IDs resueltos (nullable si no hay coincidencia)
    public int? UserId { get; set; }
    public int? ProjectId { get; set; }
    public int? ActionId { get; set; }

    // Auditoría y control
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

public class StagingBizneoEmpleado : IStagingRow
{
    public int Id { get; set; }
    public string EmpleadoIdExterno { get; set; } = null!;
    public int? UserId { get; set; }
    public string NIF { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? Departamento { get; set; }
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

public class StagingBizneoHora : IStagingRow
{
    public int Id { get; set; }
    public string RegistroIdExterno { get; set; } = null!;
    public int UserId { get; set; }
    public int ProjectId { get; set; }
    public DateOnly Fecha { get; set; }
    public decimal Horas { get; set; }
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

public class StagingIntratimeFichaje : IStagingRow
{
    public int Id { get; set; }
    public string FichajeIdExterno { get; set; } = null!;
    public int UserId { get; set; }
    public DateTime Entrada { get; set; }
    public DateTime? Salida { get; set; }
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

public class StagingPayHawkGasto : IStagingRow
{
    public int Id { get; set; }
    public string GastoIdExterno { get; set; } = null!;
    public int UserId { get; set; }
    public int ProjectId { get; set; }
    public DateOnly Fecha { get; set; }
    public decimal Importe { get; set; }
    public string Categoria { get; set; } = null!;
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

// Mapeos Celero → SIG-es
public class CeleroResourceMapping : IAuditable
{
    public int Id { get; set; }
    public string CeleroNif { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string? Descripcion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CeleroServiceMapping : IAuditable
{
    public int Id { get; set; }
    public string CeleroServiceName { get; set; } = null!;
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public string? Descripcion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CeleroMissionMapping : IAuditable
{
    public int Id { get; set; }
    public string CeleroMissionName { get; set; } = null!;
    public int ActionId { get; set; }
    public Action Action { get; set; } = null!;
    public string? Descripcion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
