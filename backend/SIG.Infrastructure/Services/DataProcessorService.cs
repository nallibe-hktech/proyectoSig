using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Entities.Staging;
using SIG.Domain.Enums;
using SIG.Infrastructure.Persistence;

namespace SIG.Infrastructure.Services;

/// <summary>
/// Procesa datos desde staging tables hacia tablas productivas.
/// - Lee registros sin procesar de staging
/// - Valida y mapea a entidades productivas
/// - Crea audit logs
/// - Marca registros como procesados
/// </summary>
public class DataProcessorService : IDataProcessorService
{
    private readonly AppDbContext _db;
    private readonly IUserRepository _userRepo;
    private readonly IDepartmentRepository _deptRepo;
    private readonly IAuditLogRepository _auditRepo;
    private readonly ILogger<DataProcessorService> _logger;

    public DataProcessorService(
        AppDbContext db,
        IUserRepository userRepo,
        IDepartmentRepository deptRepo,
        IAuditLogRepository auditRepo,
        ILogger<DataProcessorService> logger)
    {
        _db = db;
        _userRepo = userRepo;
        _deptRepo = deptRepo;
        _auditRepo = auditRepo;
        _logger = logger;
    }

    /// <summary>
    /// Procesa empleados desde Bizneo staging → productivo
    /// Crea/actualiza usuarios basado en NIF
    /// </summary>
    public async Task<(int Processed, int Errors)> ProcessBizneoEmpleadosAsync(CancellationToken ct)
    {
        var pendientes = await _db.StagingBizneoEmpleados
            .Where(x => !x.FlagProcesado)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);

        _logger.LogInformation($"[Bizneo Empleados] Encontrados {pendientes.Count} registros pendientes");

        int processed = 0, errors = 0;
        int nuevos = 0, actualizados = 0;

        foreach (var staging in pendientes)
        {
            try
            {
                // Buscar usuario por NIF
                var usuario = await _userRepo.ListAsync(ct)
                    .ContinueWith(t => t.Result.FirstOrDefault(u => u.NIF == staging.NIF), ct);

                if (usuario == null)
                {
                    // Crear nuevo usuario
                    usuario = new User
                    {
                        Nombre = staging.Nombre,
                        Apellidos = "", // No disponible en staging
                        NIF = staging.NIF,
                        Email = $"{staging.Nombre.ToLower().Replace(" ", ".")}@empresa.es",
                        PasswordHash = "temp", // Usuario necesita resetear
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.Users.Add(usuario);
                    nuevos++;
                    _logger.LogDebug($"[Bizneo] Nuevo usuario: {staging.Nombre} ({staging.NIF})");
                }
                else
                {
                    // Actualizar datos existentes
                    usuario.Nombre = staging.Nombre;
                    usuario.UpdatedAt = DateTime.UtcNow;
                    actualizados++;
                    _logger.LogDebug($"[Bizneo] Usuario actualizado: {staging.Nombre} ({staging.NIF})");
                }

                // Marcar staging como procesado
                staging.FlagProcesado = true;
                staging.ErrorProcesamiento = null;
                processed++;
            }
            catch (Exception ex)
            {
                staging.ErrorProcesamiento = ex.Message;
                errors++;
                _logger.LogError($"[Bizneo] Error procesando {staging.Nombre}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation($"[Bizneo Empleados] Resultado: {nuevos} nuevos, {actualizados} actualizados, {errors} errores");
        return (processed, errors);
    }

    /// <summary>
    /// Procesa productos desde SGPV staging
    /// Solo marca como procesado (sin tabla productiva)
    /// </summary>
    public async Task<(int Processed, int Errors)> ProcessSgpvProductosAsync(CancellationToken ct)
    {
        var pendientes = await _db.StagingSgpvProductos
            .Where(x => !x.FlagProcesado)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);

        _logger.LogInformation($"[SGPV Productos] Encontrados {pendientes.Count} registros pendientes");

        int processed = 0, errors = 0;

        foreach (var staging in pendientes)
        {
            try
            {
                // Solo marcar como procesado (sin tabla productiva destino)
                staging.FlagProcesado = true;
                staging.ErrorProcesamiento = null;
                processed++;
                _logger.LogDebug($"[SGPV] Producto procesado: {staging.IdProducto}");
            }
            catch (Exception ex)
            {
                staging.ErrorProcesamiento = ex.Message;
                errors++;
                _logger.LogError($"[SGPV] Error procesando {staging.IdProducto}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation($"[SGPV Productos] Resultado: {processed} procesados, {errors} errores");
        return (processed, errors);
    }

    /// <summary>
    /// Procesa empleados desde A3 Innuva staging → productivo
    /// Similar a Bizneo
    /// </summary>
    public async Task<(int Processed, int Errors)> ProcessA3InnuvaEmpleadosAsync(CancellationToken ct)
    {
        var pendientes = await _db.StagingA3InnuvaEmpleados
            .Where(x => !x.FlagProcesado)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);

        int processed = 0, errors = 0;

        foreach (var staging in pendientes)
        {
            try
            {
                var usuario = await _userRepo.ListAsync(ct)
                    .ContinueWith(t => t.Result.FirstOrDefault(u => u.NIF == staging.NIF), ct);

                if (usuario == null)
                {
                    usuario = new User
                    {
                        Nombre = staging.Nombre,
                        Apellidos = "",
                        NIF = staging.NIF,
                        Email = $"{staging.Nombre.ToLower().Replace(" ", ".")}@empresa.es",
                        PasswordHash = "temp",
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.Users.Add(usuario);
                }
                else
                {
                    usuario.Nombre = staging.Nombre;
                    usuario.UpdatedAt = DateTime.UtcNow;
                }

                staging.FlagProcesado = true;
                staging.ErrorProcesamiento = null;
                processed++;
            }
            catch (Exception ex)
            {
                staging.ErrorProcesamiento = ex.Message;
                errors++;
            }
        }

        await _db.SaveChangesAsync(ct);
        return (processed, errors);
    }

    /// <summary>
    /// Procesa empleados desde Intratime staging → productivo
    /// Crea/actualiza usuarios basado en NIF
    /// Resuelve retroactivamente fichajes sin UserId
    /// </summary>
    public async Task<(int Processed, int Errors)> ProcessIntratimeEmpleadosAsync(CancellationToken ct)
    {
        var pendientes = await _db.StagingIntratimeEmpleados
            .Where(x => !x.FlagProcesado)
            .OrderBy(x => x.Id)
            .ToListAsync(ct);

        _logger.LogInformation($"[Intratime Empleados] Encontrados {pendientes.Count} registros pendientes");

        int processed = 0, errors = 0;

        foreach (var staging in pendientes)
        {
            try
            {
                // Buscar usuario por NIF
                var usuario = (await _userRepo.ListAsync(ct)).FirstOrDefault(u => u.NIF == staging.NIF);

                if (usuario == null)
                {
                    // Crear nuevo usuario
                    var partes = staging.Nombre.Split(',');
                    usuario = new User
                    {
                        Nombre = partes.Length > 1 ? partes[1].Trim() : staging.Nombre,
                        Apellidos = partes.Length > 0 ? partes[0].Trim() : "",
                        NIF = staging.NIF,
                        Email = staging.Email,
                        PasswordHash = "temp",
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.Users.Add(usuario);
                    await _db.SaveChangesAsync(ct); // para obtener el Id
                    _logger.LogDebug($"[Intratime] Nuevo usuario: {staging.Nombre} ({staging.NIF})");
                }
                else
                {
                    usuario.Email = staging.Email;
                    usuario.UpdatedAt = DateTime.UtcNow;
                    _logger.LogDebug($"[Intratime] Usuario actualizado: {staging.Nombre} ({staging.NIF})");
                }

                staging.UserId = usuario.Id;
                staging.FlagProcesado = true;
                staging.ErrorProcesamiento = null;

                // Retroactivo: resolver fichajes sin UserId de este empleado
                var fichajesSinResolver = await _db.StagingIntratimeFichajes
                    .Where(f => f.UserIdExterno == staging.UserIdExterno && !f.UserId.HasValue)
                    .ToListAsync(ct);
                foreach (var f in fichajesSinResolver)
                    f.UserId = usuario.Id;

                processed++;
            }
            catch (Exception ex)
            {
                staging.ErrorProcesamiento = ex.Message;
                errors++;
                _logger.LogError($"[Intratime] Error procesando {staging.Nombre}: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation($"[Intratime Empleados] Resultado: {processed} procesados, {errors} errores");
        return (processed, errors);
    }

    /// <summary>
    /// Valida y marca como procesados los registros de otros sistemas
    /// (ausencias, gastos, fiches, viajes) que no tienen mapeo directo a tablas productivas
    /// </summary>
    public async Task<(int Processed, int Errors)> ProcessHorasYGastosAsync(CancellationToken ct)
    {
        // Procesar ausencias Bizneo
        var absencesPendientes = await _db.StagingBizneoAbsences
            .Where(x => !x.FlagProcesado && x.UserId > 0 && x.ProjectId > 0)
            .ToListAsync(ct);

        foreach (var a in absencesPendientes)
        {
            a.FlagProcesado = true;
        }

        // Procesar gastos PayHawk
        var gastosPendientes = await _db.StagingPayHawkGastos
            .Where(x => !x.FlagProcesado && x.UserId > 0 && x.ProjectId > 0)
            .ToListAsync(ct);

        foreach (var g in gastosPendientes)
        {
            g.FlagProcesado = true;
        }

        // Procesar fichajes Intratime
        var fichajesPendientes = await _db.StagingIntratimeFichajes
            .Where(x => !x.FlagProcesado && x.UserId.HasValue)
            .ToListAsync(ct);

        foreach (var f in fichajesPendientes)
        {
            // Calcular HorasCalculadas si es null (para fichajes importados antes del fix)
            if (!f.HorasCalculadas.HasValue && f.Salida.HasValue)
                f.HorasCalculadas = (decimal)(f.Salida.Value - f.Entrada).TotalHours;
            f.FlagProcesado = true;
        }

        // Procesar viajes TravelPerk
        var viajesPendientes = await _db.StagingTravelPerkViajes
            .Where(x => !x.FlagProcesado)
            .ToListAsync(ct);

        foreach (var v in viajesPendientes)
        {
            v.FlagProcesado = true;
        }

        var total = absencesPendientes.Count + gastosPendientes.Count + fichajesPendientes.Count + viajesPendientes.Count;
        await _db.SaveChangesAsync(ct);
        return (total, 0);
    }

    /// <summary>
    /// Valida discrepancias entre horas Intratime y visitas Celero/SGPV
    /// Compara horas registradas vs. horas facturables
    /// </summary>
    public async Task<IReadOnlyList<DiscrepanciaIntratimeDto>> ValidarDiscrepanciasIntratimeAsync(
        DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        // Horas por usuario en el período (Intratime)
        var horasPorUsuario = await _db.StagingIntratimeFichajes
            .Where(f => f.UserId.HasValue
                && f.Entrada >= desde.ToDateTime(TimeOnly.MinValue)
                && f.Entrada <= hasta.ToDateTime(TimeOnly.MaxValue)
                && f.HorasCalculadas.HasValue)
            .GroupBy(f => f.UserId!.Value)
            .Select(g => new { UserId = g.Key, TotalHoras = g.Sum(f => f.HorasCalculadas!.Value) })
            .ToListAsync(ct);

        if (horasPorUsuario.Count == 0)
            return Array.Empty<DiscrepanciaIntratimeDto>();

        // Visitas por usuario en el período (Celero)
        var visitasCelero = await _db.StagingCeleroVisitas
            .Where(v => v.UserId.HasValue && v.Fecha >= desde && v.Fecha <= hasta)
            .GroupBy(v => v.UserId!.Value)
            .Select(g => new { UserId = g.Key, TotalVisitas = g.Count() })
            .ToListAsync(ct);

        // Horas por usuario en el período (SGPV)
        var visitasSgpv = await _db.StagingSgpvVisitas
            .Where(v => v.UserId.HasValue && v.Fecha >= desde && v.Fecha <= hasta)
            .GroupBy(v => v.UserId!.Value)
            .Select(g => new { UserId = g.Key, TotalHorasSgpv = g.Sum(v => v.HorasDuracion ?? 0) })
            .ToListAsync(ct);

        // Calcular discrepancias
        var resultado = horasPorUsuario.Select(h =>
        {
            var totalVisitas = visitasCelero.FirstOrDefault(v => v.UserId == h.UserId)?.TotalVisitas ?? 0;
            var horasSgpv = visitasSgpv.FirstOrDefault(v => v.UserId == h.UserId)?.TotalHorasSgpv ?? 0;
            var horasEsperadas = totalVisitas * 8m + horasSgpv;
            var diferencia = h.TotalHoras - horasEsperadas;
            return new DiscrepanciaIntratimeDto(
                UserId: h.UserId,
                HorasIntratime: h.TotalHoras,
                VisitasCelero: totalVisitas,
                HorasSgpv: horasSgpv,
                HorasEsperadas: horasEsperadas,
                Diferencia: diferencia,
                TieneDiscrepancia: Math.Abs(diferencia) > 4m  // umbral: 4 horas
            );
        }).ToList();

        return resultado;
    }

    /// <summary>
    /// Procesa todos los registros pendientes (llamar después de cada sync)
    /// </summary>
    public async Task<ProcessingResultDto> ProcessAllPendingAsync(CancellationToken ct)
    {
        var systems = new Dictionary<string, (int Processed, int Errors)>();
        var totalProcessed = 0;
        var totalErrors = 0;
        string? error = null;

        try
        {
            // Empleados de Bizneo
            var bizneo = await ProcessBizneoEmpleadosAsync(ct);
            systems["bizneo_empleados"] = bizneo;

            // Empleados de A3 Innuva
            var a3 = await ProcessA3InnuvaEmpleadosAsync(ct);
            systems["a3innuva_empleados"] = a3;

            // Empleados de Intratime
            var intratimeEmp = await ProcessIntratimeEmpleadosAsync(ct);
            systems["intratime_empleados"] = intratimeEmp;

            // Productos de SGPV
            var sgpvProductos = await ProcessSgpvProductosAsync(ct);
            systems["sgpv_productos"] = sgpvProductos;

            // Horas, gastos, fiches, viajes (validación y marcado)
            var otros = await ProcessHorasYGastosAsync(ct);
            systems["otros"] = otros;

            totalProcessed = bizneo.Processed + a3.Processed + intratimeEmp.Processed + sgpvProductos.Processed + otros.Processed;
            totalErrors = bizneo.Errors + a3.Errors + intratimeEmp.Errors + sgpvProductos.Errors + otros.Errors;
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }

        return new ProcessingResultDto(
            Timestamp: DateTime.UtcNow,
            Systems: systems,
            TotalProcessed: totalProcessed,
            TotalErrors: totalErrors,
            Error: error);
    }
}
