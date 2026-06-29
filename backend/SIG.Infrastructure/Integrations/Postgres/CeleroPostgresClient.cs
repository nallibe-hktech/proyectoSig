using Npgsql;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Integrations;
using SIG.Domain.Exceptions;

namespace SIG.Infrastructure.Integrations.Postgres;

public class CeleroPostgresClient : ICeleroClient
{
    private readonly string _connectionString;
    private readonly bool _incluirNoRealizadas;

    /// <param name="incluirNoRealizadas">
    /// Si es false (por defecto) solo se traen las visitas con status = 'done', que es el
    /// comportamiento histórico y vale para todos los clientes salvo Inpost. Si es true se
    /// relaja el filtro para traer también fallidas/canceladas, que Inpost necesita facturar
    /// (ver docs/RETOMA_INPOST_FACTURACION.md §4.3). Configurable porque afecta a TODOS los clientes.
    /// </param>
    public CeleroPostgresClient(string connectionString, bool incluirNoRealizadas = false)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _incluirNoRealizadas = incluirNoRealizadas;
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

            // Las columnas de enriquecimiento (realDuration/cancellationReason/addressState/addressCity) se
            // trazaron desde el Excel del cliente, NO contra la BBDD real (ver docs/RETOMA_INPOST_FACTURACION.md §4.3).
            // Si no existen, pedirlas rompe TODA la sincronización (42703). Detectamos cuáles existen de
            // verdad y las ausentes se ingieren como NULL (graceful degradation, solo lectura).
            var columnasDisponibles = await GetColumnasVisitDisponiblesAsync(conn, ct);

            var sql = BuildVisitasSql(_incluirNoRealizadas, columnasDisponibles);

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
                int? duracion = reader.IsDBNull(5) ? null : Convert.ToInt32(reader.GetValue(5));
                var estado = reader.IsDBNull(6) ? null : reader.GetString(6);
                var cancellationReason = reader.IsDBNull(7) ? null : reader.GetString(7);
                var provincia = reader.IsDBNull(8) ? null : reader.GetString(8);
                var ciudad = reader.IsDBNull(9) ? null : reader.GetString(9);
                var muebles = reader.IsDBNull(10) ? null : reader.GetString(10);
                var tipoMueble = reader.IsDBNull(11) ? null : reader.GetString(11);

                visitas.Add(new CeleroVisitaDto(
                    visitaId, resourceNif, serviceName, missionName, fecha,
                    DuracionMinutos: duracion,
                    Estado: estado,
                    Provincia: provincia,
                    Ciudad: ciudad,
                    CancellationReason: cancellationReason,
                    Muebles: muebles,
                    TipoMueble: tipoMueble));
            }
        }
        catch (Exception ex)
        {
            throw new IntegrationException("Celero", $"Error al conectar o consultar Celero One: {ex.Message}");
        }

        return visitas;
    }

    /// <summary>
    /// Construye la consulta de visitas. Extraído como método puro para poder verificar de forma
    /// determinista (sin BD) la decisión delicada del filtro de estado: por defecto solo 'done',
    /// y al relajarlo se traen también fallidas/canceladas (ver docs/RETOMA_INPOST_FACTURACION.md §4.3).
    /// Columnas nuevas realDuration / status / cancellationReason / addressState → viajan al PayloadJson
    /// y alimentan la segmentación del motor (franjas de minutos, provincia, excepciones). La provincia
    /// se lee directamente de la visita (`addressState`), según la hoja CeleroOne del Excel del cliente,
    /// que sitúa addressState en el VisitReport (no requiere JOIN a centro/POA).
    /// </summary>
    /// <summary>Columnas de enriquecimiento (origen Celero/Inpost) que pueden no existir en el esquema real.</summary>
    public static readonly IReadOnlyList<string> ColumnasOpcionalesVisit = new[]
    {
        "realDuration", "cancellationReason", "addressState", "addressCity",
        "realTimeTo"    // fecha real de finalización de visita (confirma cliente 2026-06-29); fallback a planDate si no existe
        // Nota: furniture data (Muebles/TipoMueble/CantidadMuebles) se extrae via JOIN a feedback→article,
        // no como columnas opcionales en visit. Ver BuildVisitasSql.
    };

    /// <summary>
    /// Sobrecarga optimista: asume que todas las columnas opcionales existen. La usan los tests de SQL.
    /// En ejecución se usa la sobrecarga con el conjunto real de columnas detectado en la BBDD.
    /// </summary>
    public static string BuildVisitasSql(bool incluirNoRealizadas)
        => BuildVisitasSql(incluirNoRealizadas, new HashSet<string>(ColumnasOpcionalesVisit, StringComparer.Ordinal));

    public static string BuildVisitasSql(bool incluirNoRealizadas, ISet<string> columnasVisit)
    {
        var statusFilter = incluirNoRealizadas ? string.Empty : "AND v.status = 'done'";

        // Cada columna opcional se emite solo si existe en el esquema real; si falta se sustituye por
        // NULL para no romper la consulta (§4.3). Las posiciones del SELECT quedan fijas (5..8) para
        // que el reader siga leyendo por índice.
        var duracion     = columnasVisit.Contains("realDuration")       ? "v.\"realDuration\""       : "NULL::int";
        var cancellation = columnasVisit.Contains("cancellationReason") ? "v.\"cancellationReason\"" : "NULL::text";
        var provincia    = columnasVisit.Contains("addressState")       ? "v.\"addressState\""       : "NULL::text";
        var ciudad       = columnasVisit.Contains("addressCity")        ? "v.\"addressCity\""        : "NULL::text";
        // Cliente confirmó 2026-06-29: la fecha de realización es "realTimeTo", no "planDate".
        // Si la columna no existe en el esquema (entorno sin datos históricos) se cae a planDate.
        var fechaCol     = columnasVisit.Contains("realTimeTo")         ? "v.\"realTimeTo\""         : "v.\"planDate\"";

        return $"""
            SELECT v.id::text                          AS visita_id,
                   COALESCE(r."resourceExternalId",'') AS resource_nif,
                   COALESCE(m."serviceName",'')        AS service_name,
                   COALESCE(m."missionType",'')        AS mission_name,
                   {fechaCol}                          AS fecha,
                   {duracion}                          AS duracion,
                   COALESCE(v.status,'')               AS estado,
                   {cancellation}                      AS cancellation_reason,
                   {provincia}                         AS provincia,
                   {ciudad}                            AS ciudad,
                   -- Muebles extraídos via feedback→article (string concatenado, NULL si no existen)
                   STRING_AGG(DISTINCT COALESCE(a.name, ''), ' | ') FILTER (WHERE a.id IS NOT NULL) AS muebles,
                   STRING_AGG(DISTINCT COALESCE(a."categoryId"::text, ''), ' | ') FILTER (WHERE a.id IS NOT NULL) AS tipo_mueble
            FROM public.visit v
            LEFT JOIN public.feedback f ON f."visitId" = v.id
            LEFT JOIN public.article a ON a.id = f."articleId"
            JOIN public.analytics_mission_list_view m ON m."missionId" = v."missionId"
            JOIN public.resource_list_view r          ON r."resourceId" = v."resourceId"
            WHERE {fechaCol} BETWEEN @desde AND @hasta
              {statusFilter}
            GROUP BY v.id, {fechaCol}, v.status, r."resourceExternalId", m."serviceName", m."missionType",
                     {duracion}, {cancellation}, {provincia}, {ciudad}
            ORDER BY v.id
            """;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Importación de clientes y servicios desde las tablas maestras de Celero
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lee todos los clientes activos de <c>public.client</c> de Celero.
    /// Columnas reales confirmadas (2026-06-29): id, name, "vatNr", "deletedAt".
    /// Direcciones y contactos están en client_address / client_contact (no se leen aquí).
    /// Si un cliente no tiene vatNr se le asigna un NIF sintético estable: 'CELERO-{id}'.
    /// Esto garantiza que todos los clientes activos de Celero se importen, no solo los que tienen CIF real.
    /// </summary>
    public async Task<IReadOnlyList<CeleroClienteDto>> GetClientesAsync(CancellationToken ct)
    {
        var result = new List<CeleroClienteDto>();
        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            using var lockCmd = new NpgsqlCommand("SET default_transaction_read_only = on;", conn);
            await lockCmd.ExecuteNonQueryAsync(ct);

            const string sql = """
                SELECT c.id::text                                              AS id_externo,
                       c.name                                                  AS nombre,
                       COALESCE(c."vatNr", 'CELERO-' || c.id::text)           AS nif
                FROM public.client c
                WHERE c."deletedAt" IS NULL
                  AND c.name IS NOT NULL
                ORDER BY c.name
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                result.Add(new CeleroClienteDto(
                    IdExterno: reader.GetString(0),
                    Nombre:    reader.GetString(1),
                    Nif:       reader.GetString(2),
                    Direccion: null, Ciudad: null, Provincia: null,
                    CodigoPostal: null, ContactoEmail: null, ContactoTelefono: null
                ));
            }
        }
        catch (Exception ex)
        {
            throw new IntegrationException("Celero", $"Error al leer clientes de Celero: {ex.Message}");
        }
        return result;
    }

    /// <summary>
    /// Lee todos los servicios activos de la tabla <c>public.service</c> de Celero con su FK al cliente.
    /// </summary>
    public async Task<IReadOnlyList<CeleroServicioDto>> GetServiciosAsync(CancellationToken ct)
    {
        var result = new List<CeleroServicioDto>();
        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            using var lockCmd = new NpgsqlCommand("SET default_transaction_read_only = on;", conn);
            await lockCmd.ExecuteNonQueryAsync(ct);

            const string sql = """
                SELECT s.id::text          AS id_externo,
                       s.name              AS nombre,
                       s."clientId"::text  AS cliente_id_externo
                FROM public.service s
                WHERE s."deletedAt" IS NULL
                  AND s."isActive"  = true
                ORDER BY s.name
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                result.Add(new CeleroServicioDto(
                    IdExterno:       reader.GetString(0),
                    Nombre:          reader.GetString(1),
                    ClienteIdExterno: reader.GetString(2)
                ));
            }
        }
        catch (Exception ex)
        {
            throw new IntegrationException("Celero", $"Error al leer servicios de Celero: {ex.Message}");
        }
        return result;
    }

    /// <summary>
    /// Lee todos los departamentos activos de la tabla <c>public.department</c> de Celero.
    /// Columnas reales: id, name, notes, deletedAt.
    /// Los departamentos soft-deleted (deletedAt IS NOT NULL) se excluyen.
    /// </summary>
    public async Task<IReadOnlyList<CeleroDepartmentDto>> GetDepartmentsAsync(CancellationToken ct)
    {
        var result = new List<CeleroDepartmentDto>();
        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            using var lockCmd = new NpgsqlCommand("SET default_transaction_read_only = on;", conn);
            await lockCmd.ExecuteNonQueryAsync(ct);

            const string sql = """
                SELECT d.id::text   AS id_externo,
                       d.name       AS nombre,
                       d.notes      AS notas
                FROM public.department d
                WHERE d."deletedAt" IS NULL
                  AND d.name IS NOT NULL
                ORDER BY d.name
                """;

            using var cmd = new NpgsqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                result.Add(new CeleroDepartmentDto(
                    IdExterno: reader.GetString(0),
                    Nombre:    reader.GetString(1),
                    Notas:     reader.IsDBNull(2) ? null : reader.GetString(2)
                ));
            }
        }
        catch (Exception ex)
        {
            throw new IntegrationException("Celero", $"Error al leer departamentos de Celero: {ex.Message}");
        }
        return result;
    }

    /// <summary>
    /// Consulta a information_schema (solo lectura) qué columnas opcionales existen realmente en
    /// public.visit. Evita el 42703 cuando los nombres trazados desde el Excel no coinciden con la BBDD.
    /// </summary>
    private static Task<ISet<string>> GetColumnasVisitDisponiblesAsync(NpgsqlConnection conn, CancellationToken ct)
        => GetColumnasTablaAsync(conn, "visit", ColumnasOpcionalesVisit, ct);

    /// <summary>
    /// Versión genérica: detecta qué columnas de <paramref name="columnasOpcionales"/> existen
    /// en la tabla indicada (schema <c>public</c>). Usada por visit, client y service.
    /// </summary>
    private static async Task<ISet<string>> GetColumnasTablaAsync(
        NpgsqlConnection conn, string tabla, IReadOnlyList<string> columnasOpcionales, CancellationToken ct)
    {
        var disponibles = new HashSet<string>(StringComparer.Ordinal);
        const string sql = """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name   = @tabla
              AND column_name  = ANY(@cols)
            """;
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@tabla", tabla);
        cmd.Parameters.AddWithValue("@cols", columnasOpcionales.ToArray());

        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            disponibles.Add(reader.GetString(0));
        return disponibles;
    }
}
