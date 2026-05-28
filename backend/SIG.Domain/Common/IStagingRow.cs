namespace SIG.Domain.Common;

public interface IStagingRow
{
    int Id { get; set; }
    string PayloadJson { get; set; }
    string Hash { get; set; }
    DateTime FechaUltimaSincronizacion { get; set; }
    bool FlagProcesado { get; set; }
    string? ErrorProcesamiento { get; set; }
}
