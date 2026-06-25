using Npgsql;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Integrations;
using SIG.Domain.Exceptions;

namespace SIG.Infrastructure.Integrations.Postgres;

public class CeleroPostgresClient : ICeleroClient
{
    private readonly string _connectionString;

    public CeleroPostgresClient(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task<IReadOnlyList<CeleroVisitaDto>> GetVisitasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        // La resolución de NIF→usuario y servicio/misión→Servicio se realiza en DataProcessorService
        // al migrar staging→productivo. Aquí solo se extraen las visitas crudas (modo read-only).
        var visitas = new List<CeleroVisitaDto>();

        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            // MÁXIMA PROTECCIÓN: Fuerza modo read-only en Celero
            // Cualquier intento de UPDATE/DELETE/INSERT fallará automáticamente
            using var lockCmd = new NpgsqlCommand("SET default_transaction_read_only = on;", conn);
            await lockCmd.ExecuteNonQueryAsync(ct);

            var sql = """
                SELECT v.id::text                          AS visita_id,
                       COALESCE(r."resourceExternalId",'') AS resource_nif,
                       COALESCE(m."serviceName",'')        AS service_name,
                       COALESCE(m."missionType",'')        AS mission_name,
                       v."planDate"                        AS fecha,
                       COALESCE(v."duration", 0)           AS duracion_real_minutos,
                       COALESCE(r."addressState",'')       AS provincia,
                       COALESCE(r."addressCity",'')        AS ciudad,
                       COALESCE(v."status",'')             AS estado
                FROM public.visit v
                JOIN public.analytics_mission_list_view m ON m."missionId" = v."missionId"
                JOIN public.resource_list_view r          ON r."resourceId" = v."resourceId"
                WHERE v."planDate" BETWEEN @desde AND @hasta
                ORDER BY v.id
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@desde", desde.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue("@hasta", hasta.ToDateTime(TimeOnly.MaxValue));

            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var visitaId = reader.GetString(0);
                var resourceNif = reader.GetString(1);
                var serviceName = reader.GetString(2);
                var missionName = reader.GetString(3);
                var fecha = DateOnly.FromDateTime(reader.GetDateTime(4));
                var duracionRealMinutos = reader.GetInt32(5);
                var provincia = reader.GetString(6);
                var ciudad = reader.GetString(7);
                var estado = reader.GetString(8);

                visitas.Add(new CeleroVisitaDto(visitaId, resourceNif, serviceName, missionName, fecha,
                    duracionRealMinutos, provincia, ciudad, estado));
            }
        }
        catch (Exception ex)
        {
            throw new IntegrationException("Celero", $"Error al conectar o consultar Celero One: {ex.Message}");
        }

        return visitas;
    }
}
