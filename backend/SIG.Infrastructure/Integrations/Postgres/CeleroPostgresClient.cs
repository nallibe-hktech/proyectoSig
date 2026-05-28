using Npgsql;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Integrations;
using SIG.Application.Interfaces.Repositories;
using SIG.Domain.Exceptions;

namespace SIG.Infrastructure.Integrations.Postgres;

public class CeleroPostgresClient : ICeleroClient
{
    private readonly string _connectionString;
    private readonly IUserRepository _userRepo;
    private readonly IProjectRepository _projectRepo;
    private readonly IActionRepository _actionRepo;

    public CeleroPostgresClient(string connectionString, IUserRepository userRepo, IProjectRepository projectRepo, IActionRepository actionRepo)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
        _projectRepo = projectRepo ?? throw new ArgumentNullException(nameof(projectRepo));
        _actionRepo = actionRepo ?? throw new ArgumentNullException(nameof(actionRepo));
    }

    public async Task<IReadOnlyList<CeleroVisitaDto>> GetVisitasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        // Load lookups from local database
        var users = await _userRepo.ListAsync(ct);
        var projects = await _projectRepo.ListAsync(ct);
        var actions = await _actionRepo.ListAsync(ct);

        var nifToUserId = users
            .Where(u => !u.IsDeleted)
            .ToDictionary(u => u.NIF, u => u.Id, StringComparer.OrdinalIgnoreCase);

        var serviceNameToProjectId = projects
            .Where(p => !p.IsDeleted)
            .ToDictionary(p => p.Nombre, p => p.Id, StringComparer.OrdinalIgnoreCase);

        var missionNameToActionId = actions
            .ToDictionary(a => a.Nombre, a => a.Id, StringComparer.OrdinalIgnoreCase);

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
                SELECT
                    "visitId"::text AS visita_id,
                    COALESCE("resourceExternalId", '') AS resource_nif,
                    COALESCE("serviceName", '') AS service_name,
                    COALESCE("missionType", '') AS mission_name,
                    "visitPlanDate" AS fecha
                FROM public.visit_list_view
                WHERE "visitPlanDate" BETWEEN @desde AND @hasta
                  AND "visitStatus" = 'done'
                ORDER BY "visitId"
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

                visitas.Add(new CeleroVisitaDto(visitaId, resourceNif, serviceName, missionName, fecha));
            }
        }
        catch (Exception ex)
        {
            throw new IntegrationException("Celero", $"Error al conectar o consultar Celero One: {ex.Message}");
        }

        return visitas;
    }
}
