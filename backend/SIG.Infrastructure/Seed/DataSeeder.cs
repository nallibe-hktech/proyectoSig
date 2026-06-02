#pragma warning disable S2245 // Random usado intencionalmente para datos semilla deterministas (semilla fija 20260101)

using System.Text.Json;
using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SIG.Application.Calculation;
using SIG.Application.Calculation.Nodes;
using SIG.Application.Interfaces.Services;
using SIG.Application.Services;
using SIG.Domain.Entities;
using SIG.Domain.Entities.Staging;
using SIG.Domain.Enums;
using SIG.Infrastructure.Persistence;
using SIG.Infrastructure.Persistence.Interceptors;
using Action = SIG.Domain.Entities.Action;

namespace SIG.Infrastructure.Seed;

public class DataSeeder : ISeedService
{
    private const int Seed = 20260101;

    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ICalculationEngine _engine;
    private readonly IConfiguration _config;

    public DataSeeder(AppDbContext db, IPasswordHasher hasher, ICalculationEngine engine, IConfiguration config)
    {
        _db = db;
        _hasher = hasher;
        _engine = engine;
        _config = config;
    }

    public async Task RunIfEmptyAsync(CancellationToken ct)
    {
        if (await _db.Users.IgnoreQueryFilters().AnyAsync(ct)) return;
        AuditInterceptor.SuppressAudit.Value = true;
        try
        {
            await RegenerateInternalAsync(ct);
        }
        finally
        {
            AuditInterceptor.SuppressAudit.Value = false;
        }
    }

    public async Task RegenerateAsync(CancellationToken ct)
    {
        // TRUNCATE en orden inverso (transaccionales → maestros)
        await _db.Database.ExecuteSqlRawAsync("""
            TRUNCATE TABLE
                audit_logs,
                calculation_logs,
                approval_history,
                approvals,
                closure_lines,
                closures,
                staging_pay_hawk_gastos,
                staging_intratime_fichajes,
                staging_bizneo_horas,
                staging_bizneo_empleados,
                staging_celero_visitas,
                refresh_tokens,
                action_users,
                action_concepts,
                actions,
                concept_users,
                concepts,
                project_users,
                project_cost_centers,
                projects,
                clients,
                variables,
                periods,
                cost_centers,
                user_roles,
                users,
                departments,
                roles
            RESTART IDENTITY CASCADE;
        """, ct);
        AuditInterceptor.SuppressAudit.Value = true;
        try
        {
            await RegenerateInternalAsync(ct);
        }
        finally
        {
            AuditInterceptor.SuppressAudit.Value = false;
        }
    }

    private async Task RegenerateInternalAsync(CancellationToken ct)
    {
        Randomizer.Seed = new Random(Seed);

        var roles = await SeedRolesAsync(ct);
        var rMap = roles.ToDictionary(r => r.Nombre);

        var dMap = await SeedDepartmentsAsync(ct);

        var costCenters = await SeedCostCentersAsync(ct);

        var passwordHash = _hasher.Hash(_config["Seed:DemoPassword"] ?? throw new InvalidOperationException("Seed:DemoPassword no configurada en appsettings"));
        var users = await SeedUsersAsync(passwordHash, dMap, rMap, ct);
        var uByEmail = users.ToDictionary(u => u.Email);

        var clients = await SeedClientsAsync(ct);

        var projects = await SeedProjectsAsync(clients, costCenters, uByEmail, ct);
        var projectsList = projects.ToList();

        var variables = await SeedVariablesAsync(ct);
        var tarifaHoraId = variables.First(v => v.Nombre == "TarifaHora").Id;

        var concepts = await SeedConceptsAsync(tarifaHoraId, ct);

        var actions = await SeedActionsAsync(projectsList, dMap, concepts, uByEmail, ct);

        var periods = await SeedPeriodsAsync(ct);
        var periodsList = periods.ToList();

        await SeedStagingDataAsync(users, projects, actions, periodsList, uByEmail, ct);

        var closuresFull = await SeedClosuresAsync(projectsList, periodsList, ct);

        await SeedLineasYLogsAsync(closuresFull, concepts, users, ct);

        await SeedApprovalsAsync(closuresFull, rMap, uByEmail, ct);

        await SeedAuditExtraAsync(uByEmail, ct);
    }

    private async Task<List<Role>> SeedRolesAsync(CancellationToken ct)
    {
        var roles = new List<Role>
        {
            new() { Nombre = "Administrator", Descripcion = "Acceso total" },
            new() { Nombre = "Direction", Descripcion = "Dirección" },
            new() { Nombre = "Fico", Descripcion = "Financiero" },
            new() { Nombre = "Backoffice", Descripcion = "Backoffice" },
            new() { Nombre = "ProjectManager", Descripcion = "PM" },
            new() { Nombre = "Auditor", Descripcion = "Auditor" },
            new() { Nombre = "Reader", Descripcion = "Solo lectura" }
        };
        _db.Roles.AddRange(roles);
        await _db.SaveChangesAsync(ct);
        return roles;
    }

    private async Task<Dictionary<string, Department>> SeedDepartmentsAsync(CancellationToken ct)
    {
        var deps = new List<Department>
        {
            new() { Nombre = "Operaciones" },
            new() { Nombre = "Backoffice" },
            new() { Nombre = "Finanzas" },
            new() { Nombre = "Dirección" }
        };
        _db.Departments.AddRange(deps);
        await _db.SaveChangesAsync(ct);
        return deps.ToDictionary(d => d.Nombre);
    }

    private async Task<List<CostCenter>> SeedCostCentersAsync(CancellationToken ct)
    {
        var costCenters = new List<CostCenter>
        {
            new() { Codigo = "025888", Nombre = "Operaciones campo" },
            new() { Codigo = "035501", Nombre = "GPV España" },
            new() { Codigo = "035502", Nombre = "GPV Portugal" },
            new() { Codigo = "041200", Nombre = "Formación" }
        };
        _db.CostCenters.AddRange(costCenters);
        await _db.SaveChangesAsync(ct);
        return costCenters;
    }

    private async Task<List<User>> SeedUsersAsync(string passwordHash, Dictionary<string, Department> dMap, Dictionary<string, Role> rMap, CancellationToken ct)
    {
        var users = new List<User>
        {
            New("admin@sig.local",      "12345678A", "Admin",     "SIG",      dMap.GetValueOrDefault("Dirección"), passwordHash, rMap["Administrator"]),
            New("direccion@sig.local",  "23456789B", "Carmen",    "Ruiz",     dMap["Dirección"], passwordHash, rMap["Direction"]),
            New("fico@sig.local",       "34567890C", "Javier",    "López",    dMap["Finanzas"], passwordHash, rMap["Fico"]),
            New("backoffice1@sig.local","45678901D", "Laura",     "Sánchez",  dMap["Backoffice"], passwordHash, rMap["Backoffice"]),
            New("backoffice2@sig.local","56789012E", "Pedro",     "Martín",   dMap["Backoffice"], passwordHash, rMap["Backoffice"]),
            New("pm.alpha@sig.local",   "67890123F", "María",     "García",   dMap["Operaciones"], passwordHash, rMap["ProjectManager"]),
            New("pm.beta@sig.local",    "78901234G", "David",     "Pérez",    dMap["Operaciones"], passwordHash, rMap["ProjectManager"]),
            New("pm.gamma@sig.local",   "89012345H", "Sara",      "Gómez",    dMap["Operaciones"], passwordHash, rMap["ProjectManager"]),
            New("pm.multi@sig.local",   "90123456J", "Alex",      "Torres",   dMap["Operaciones"], passwordHash, rMap["ProjectManager"]),
            New("auditor@sig.local",    "01234567K", "Inés",      "Romero",   dMap["Finanzas"], passwordHash, rMap["Auditor"]),
            New("reader@sig.local",     "11234567L", "Luis",      "Vega",     dMap["Operaciones"], passwordHash, rMap["Reader"]),
            NewRecurso("gpv1@sig.local","21234567M", "Carlos",    "Ramos",    dMap["Operaciones"], passwordHash),
            NewRecurso("gpv2@sig.local","31234567N", "Marta",     "Soler",    dMap["Operaciones"], passwordHash),
            NewRecurso("gpv3@sig.local","41234567P", "Iván",      "Núñez",    dMap["Operaciones"], passwordHash),
            NewRecurso("gpv4@sig.local","51234567Q", "Rosa",      "Castro",   dMap["Operaciones"], passwordHash),
        };
        _db.Users.AddRange(users);
        await _db.SaveChangesAsync(ct);
        return users;
    }

    private async Task<List<Client>> SeedClientsAsync(CancellationToken ct)
    {
        var clients = new List<Client>
        {
            new() { Nombre = "Alpha Foods",     NIF = "A12345678", Direccion = "Calle Mayor 1",  Ciudad = "Madrid",   Provincia = "Madrid",   Pais = "España", CodigoPostal = "28001", ContactoNombre = "Ana Foods",    ContactoEmail = "ana@alpha.es",    ContactoTelefono = "910000001" },
            new() { Nombre = "Beta Cosmetics",  NIF = "B23456789", Direccion = "Av. Diagonal 50",Ciudad = "Barcelona",Provincia = "Barcelona",Pais = "España", CodigoPostal = "08001", ContactoNombre = "Belén Cosmo",  ContactoEmail = "belen@beta.es",   ContactoTelefono = "930000002" },
            new() { Nombre = "Gamma Retail",    NIF = "C34567890", Direccion = "Calle Larga 99", Ciudad = "Valencia", Provincia = "Valencia", Pais = "España", CodigoPostal = "46001", ContactoNombre = "Gabriel Reta", ContactoEmail = "gabriel@gamma.es",ContactoTelefono = "960000003" },
        };
        _db.Clients.AddRange(clients);
        await _db.SaveChangesAsync(ct);
        return clients;
    }

    private async Task<List<Project>> SeedProjectsAsync(List<Client> clients, List<CostCenter> costCenters, Dictionary<string, User> uByEmail, CancellationToken ct)
    {
        var alpha = clients[0]; var beta = clients[1]; var gamma = clients[2];
        var fechaAlta = new DateOnly(2025, 1, 15);
        var projects = new List<Project>
        {
            NewProject("Alpha - Implantación Madrid", alpha.Id, costCenters[1].Id, uByEmail["pm.alpha@sig.local"].Id, fechaAlta),
            NewProject("Alpha - Visitas GPV España",  alpha.Id, costCenters[1].Id, uByEmail["pm.alpha@sig.local"].Id, fechaAlta),
            NewProject("Alpha - Formación Equipos",   alpha.Id, costCenters[3].Id, uByEmail["pm.alpha@sig.local"].Id, fechaAlta),
            NewProject("Beta - Implantación Barcelona", beta.Id, costCenters[0].Id, uByEmail["pm.beta@sig.local"].Id, fechaAlta),
            NewProject("Beta - Visitas Premium",      beta.Id, costCenters[1].Id, uByEmail["pm.beta@sig.local"].Id, fechaAlta),
            NewProject("Beta - Mensualidad",          beta.Id, costCenters[0].Id, uByEmail["pm.beta@sig.local"].Id, fechaAlta),
            NewProject("Gamma - Operaciones Campo",   gamma.Id, costCenters[0].Id, uByEmail["pm.gamma@sig.local"].Id, fechaAlta),
            NewProject("Gamma - Formación Premium",   gamma.Id, costCenters[3].Id, uByEmail["pm.gamma@sig.local"].Id, fechaAlta),
        };
        foreach (var p in projects)
        {
            p.ProjectUsers.Add(new ProjectUser { UserId = uByEmail["pm.multi@sig.local"].Id });
            foreach (var gpv in new[] { "gpv1@sig.local", "gpv2@sig.local", "gpv3@sig.local", "gpv4@sig.local" })
                p.ProjectUsers.Add(new ProjectUser { UserId = uByEmail[gpv].Id });
        }
        _db.Projects.AddRange(projects);
        await _db.SaveChangesAsync(ct);
        return projects;
    }

    private async Task<List<Variable>> SeedVariablesAsync(CancellationToken ct)
    {
        var variables = new List<Variable>
        {
            new() { Nombre = "PuntoMontado", QuestionIdExterno = "Q12", MapeoValoresJson = JsonSerializer.Serialize(new[] { new { respuesta = "Sí", valor = 1 }, new { respuesta = "No", valor = 0 } }) },
            new() { Nombre = "TipoVisita",   QuestionIdExterno = "Q15", MapeoValoresJson = JsonSerializer.Serialize(new[] { new { respuesta = "Estándar", valor = 1 }, new { respuesta = "Premium", valor = 2 } }) },
            new() { Nombre = "ZonaBonus",    QuestionIdExterno = "Q21", MapeoValoresJson = JsonSerializer.Serialize(new[] { new { respuesta = "A", valor = 1.5 }, new { respuesta = "B", valor = 1.2 }, new { respuesta = "C", valor = 1.0 } }) },
            new() { Nombre = "TarifaHora",   QuestionIdExterno = "T01", MapeoValoresJson = JsonSerializer.Serialize(new[] { new { respuesta = "Default", valor = 25 } }) },
        };
        _db.Variables.AddRange(variables);
        await _db.SaveChangesAsync(ct);
        return variables;
    }

    private async Task<List<Concept>> SeedConceptsAsync(int tarifaHoraId, CancellationToken ct)
    {
        var fechaDesde = new DateOnly(2025, 1, 1);
        var concepts = new List<Concept>
        {
            new() { Nombre = "Suma de gastos directos", Tipo = TipoConcepto.Pago, ColumnaA3 = "ImporteBruto", FechaDesde = fechaDesde, FormulaJson = JsonSerializer.Serialize(new {
                type = "Aggregate", op = "Sum", field = "Importe",
                source = new { type = "Source", entity = "GastosPayHawk", filters = new object[0] }
            }) },
            new() { Nombre = "Bonus por visita estándar", Tipo = TipoConcepto.Pago, ColumnaA3 = "ImporteBruto", FechaDesde = fechaDesde, FormulaJson = JsonSerializer.Serialize(new {
                type = "BinaryOp", op = "Mul",
                left = new { type = "Aggregate", op = "Count", source = new { type = "Source", entity = "VisitasCelero",
                    filters = new[] { new { field = "TipoVisita", op = "Eq", value = 1 } } } },
                right = new { type = "Number", value = 5 }
            }) },
            new() { Nombre = "Bonus por visita premium", Tipo = TipoConcepto.Pago, ColumnaA3 = "ImporteBruto", FechaDesde = fechaDesde, FormulaJson = JsonSerializer.Serialize(new {
                type = "BinaryOp", op = "Mul",
                left = new { type = "Aggregate", op = "Count", source = new { type = "Source", entity = "VisitasCelero",
                    filters = new[] { new { field = "TipoVisita", op = "Eq", value = 2 } } } },
                right = new { type = "Number", value = 8 }
            }) },
            new() { Nombre = "Pago por horas trabajadas", Tipo = TipoConcepto.Pago, ColumnaA3 = "ImporteBruto", FechaDesde = fechaDesde, FormulaJson = JsonSerializer.Serialize(new {
                type = "BinaryOp", op = "Mul",
                left = new { type = "Aggregate", op = "Sum", field = "Horas", source = new { type = "Source", entity = "HorasBizneo", filters = new object[0] } },
                right = new { type = "Variable", variableId = tarifaHoraId }
            }) },
            new() { Nombre = "Pago por implantación completada", Tipo = TipoConcepto.Pago, ColumnaA3 = "ImporteBruto", FechaDesde = fechaDesde, FormulaJson = JsonSerializer.Serialize(new {
                type = "BinaryOp", op = "Mul",
                left = new { type = "Aggregate", op = "Count", source = new { type = "Source", entity = "VisitasCelero",
                    filters = new[] { new { field = "PuntoMontado", op = "Eq", value = 1 } } } },
                right = new { type = "Number", value = 250 }
            }) },
            new() { Nombre = "Facturación por visita", Tipo = TipoConcepto.Factura, FechaDesde = fechaDesde, FormulaJson = JsonSerializer.Serialize(new {
                type = "BinaryOp", op = "Mul",
                left = new { type = "Aggregate", op = "Count", source = new { type = "Source", entity = "VisitasCelero", filters = new object[0] } },
                right = new { type = "Number", value = 18 }
            }) },
            new() { Nombre = "Mensualidad fija proyecto", Tipo = TipoConcepto.Factura, FechaDesde = fechaDesde, FormulaJson = JsonSerializer.Serialize(new { type = "Number", value = 1500 }) },
            new() { Nombre = "Refacturación gastos", Tipo = TipoConcepto.Factura, FechaDesde = fechaDesde, FormulaJson = JsonSerializer.Serialize(new {
                type = "BinaryOp", op = "Pct",
                left = new { type = "Aggregate", op = "Sum", field = "Importe", source = new { type = "Source", entity = "GastosPayHawk", filters = new object[0] } },
                right = new { type = "Number", value = 15 }
            }) },
        };
        _db.Concepts.AddRange(concepts);
        await _db.SaveChangesAsync(ct);
        return concepts;
    }

    private async Task<List<Action>> SeedActionsAsync(List<Project> projects, Dictionary<string, Department> dMap, List<Concept> concepts, Dictionary<string, User> uByEmail, CancellationToken ct)
    {
        var actions = new List<Action>();
        var actionNames = new[] { "Implantación", "Visita Estándar", "Visita Premium", "Formación", "Reposición" };
        foreach (var p in projects)
        {
            int count = new Random(Seed + p.Id).Next(2, 4);
            for (int i = 0; i < count; i++)
            {
                var name = actionNames[i % actionNames.Length] + " - " + p.Nombre;
                var act = new Action
                {
                    Nombre = name, ProjectId = p.Id, ClientId = p.ClientId,
                    DepartmentId = dMap["Operaciones"].Id, Estado = EstadoAccion.Activa
                };
                foreach (var c in concepts.Take(3))
                    act.ActionConcepts.Add(new ActionConcept { ConceptId = c.Id });
                foreach (var gpv in new[] { "gpv1@sig.local", "gpv2@sig.local" })
                    act.ActionUsers.Add(new ActionUser { UserId = uByEmail[gpv].Id });
                actions.Add(act);
            }
        }
        _db.Actions.AddRange(actions);
        await _db.SaveChangesAsync(ct);
        return actions;
    }

    private async Task<List<Period>> SeedPeriodsAsync(CancellationToken ct)
    {
        var periods = new List<Period>
        {
            new() { Nombre = "Noviembre 2025", FechaInicio = new(2025,11,1), FechaFin = new(2025,11,30), Estado = EstadoPeriodo.Cerrado },
            new() { Nombre = "Diciembre 2025", FechaInicio = new(2025,12,1), FechaFin = new(2025,12,31), Estado = EstadoPeriodo.Cerrado },
            new() { Nombre = "Enero 2026",     FechaInicio = new(2026,1,1),  FechaFin = new(2026,1,31),  Estado = EstadoPeriodo.Cerrado },
            new() { Nombre = "Febrero 2026",   FechaInicio = new(2026,2,1),  FechaFin = new(2026,2,28),  Estado = EstadoPeriodo.Abierto },
            new() { Nombre = "Marzo 2026",     FechaInicio = new(2026,3,1),  FechaFin = new(2026,3,31),  Estado = EstadoPeriodo.Abierto },
        };
        _db.Periods.AddRange(periods);
        await _db.SaveChangesAsync(ct);
        return periods;
    }

    private async Task SeedStagingDataAsync(List<User> users, List<Project> projects, List<Action> actions, List<Period> periods, Dictionary<string, User> uByEmail, CancellationToken ct)
    {
        Randomizer.Seed = new Random(Seed);
        var stagingVisitas = new List<StagingCeleroVisita>();
        var stagingHoras = new List<StagingBizneoHora>();
        var stagingEmps = new List<StagingBizneoEmpleado>();
        var stagingFichajes = new List<StagingIntratimeFichaje>();
        var stagingGastos = new List<StagingPayHawkGasto>();
        int hashSeed = 0;

        foreach (var u in users)
        {
            var emp = new StagingBizneoEmpleado
            {
                EmpleadoIdExterno = $"EMP-{u.Id:000}",
                UserId = u.Id, NIF = u.NIF, Nombre = $"{u.Nombre} {u.Apellidos}", Departamento = u.Department?.Nombre,
                FechaUltimaSincronizacion = DateTime.UtcNow, FlagProcesado = true
            };
            emp.PayloadJson = JsonSerializer.Serialize(new { emp.EmpleadoIdExterno, emp.NIF, emp.Nombre, emp.Departamento });
            emp.Hash = Sha256($"emp-{u.Id}-{++hashSeed}");
            stagingEmps.Add(emp);
        }

        int hashCounter = 0;
        foreach (var p in projects)
        {
            foreach (var period in periods)
            {
                var pId = p.Id; var periodId = period.Id;
                stagingVisitas.AddRange(GenerateVisitas(p, period, actions, users, hashCounter));
                hashCounter += 5 + (pId + periodId) % 4;
                stagingHoras.AddRange(GenerateHoras(p, period, users, hashCounter));
                hashCounter += 8 + (pId + periodId) % 5;
                stagingFichajes.AddRange(GenerateFichajes(p, period, users, hashCounter));
                hashCounter += 12 + (pId + periodId) % 4;
                stagingGastos.AddRange(GenerateGastos(p, period, users, hashCounter));
                hashCounter += 3 + (pId + periodId) % 4;
            }
        }
        _db.StagingBizneoEmpleados.AddRange(stagingEmps);
        _db.StagingCeleroVisitas.AddRange(stagingVisitas);
        _db.StagingBizneoHoras.AddRange(stagingHoras);
        _db.StagingIntratimeFichajes.AddRange(stagingFichajes);
        _db.StagingPayHawkGastos.AddRange(stagingGastos);
        await _db.SaveChangesAsync(ct);
    }

    private static List<StagingCeleroVisita> GenerateVisitas(Project p, Period period, List<Action> actions, List<User> users, int hashOffset)
    {
        var list = new List<StagingCeleroVisita>();
        int num = 5 + (p.Id + period.Id) % 4;
        for (int i = 0; i < num; i++)
        {
            var rec = users.Skip(11).Take(4).ToList()[i % 4];
            var date = period.FechaInicio.AddDays(new Random(Seed + p.Id + period.Id + i).Next(0, 27));
            if (date > period.FechaFin) date = period.FechaFin;
            var action = actions.First(a => a.ProjectId == p.Id);
            var v = new StagingCeleroVisita
            {
                VisitaIdExterno = $"VC-{p.Id:00}{period.Id:00}{i:00}",
                ResourceNif = rec.NIF, ServiceName = p.Nombre, MissionName = action.Nombre,
                Fecha = date, UserId = rec.Id, ProjectId = p.Id, ActionId = action.Id,
                FechaUltimaSincronizacion = DateTime.UtcNow, FlagProcesado = true
            };
            v.PayloadJson = JsonSerializer.Serialize(new { v.VisitaIdExterno, v.ResourceNif, v.ServiceName, v.MissionName, v.Fecha });
            v.Hash = Sha256($"visita-{hashOffset + i}");
            list.Add(v);
        }
        return list;
    }

    private static List<StagingBizneoHora> GenerateHoras(Project p, Period period, List<User> users, int hashOffset)
    {
        var list = new List<StagingBizneoHora>();
        int num = 8 + (p.Id + period.Id) % 5;
        for (int i = 0; i < num; i++)
        {
            var rec = users.Skip(11).Take(4).ToList()[i % 4];
            var date = period.FechaInicio.AddDays(new Random(Seed + p.Id + period.Id + i + 1000).Next(0, 27));
            if (date > period.FechaFin) date = period.FechaFin;
            var h = new StagingBizneoHora
            {
                RegistroIdExterno = $"BH-{p.Id:00}{period.Id:00}{i:00}",
                UserId = rec.Id, ProjectId = p.Id, Fecha = date, Horas = 4 + (i % 5),
                FechaUltimaSincronizacion = DateTime.UtcNow, FlagProcesado = true
            };
            h.PayloadJson = JsonSerializer.Serialize(new { h.RegistroIdExterno, h.UserId, h.ProjectId, h.Fecha, h.Horas });
            h.Hash = Sha256($"hora-{hashOffset + i}");
            list.Add(h);
        }
        return list;
    }

    private static List<StagingIntratimeFichaje> GenerateFichajes(Project p, Period period, List<User> users, int hashOffset)
    {
        var list = new List<StagingIntratimeFichaje>();
        int num = 12 + (p.Id + period.Id) % 4;
        for (int i = 0; i < num; i++)
        {
            var rec = users.Skip(11).Take(4).ToList()[i % 4];
            var date = period.FechaInicio.AddDays(new Random(Seed + p.Id + period.Id + i + 2000).Next(0, 27));
            if (date > period.FechaFin) date = period.FechaFin;
            var dt = DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(8, 0)), DateTimeKind.Utc);
            var f = new StagingIntratimeFichaje
            {
                FichajeIdExterno = $"FIC-{p.Id:00}{period.Id:00}{i:00}",
                UserId = rec.Id, Entrada = dt,
                Salida = DateTime.SpecifyKind(dt.AddHours(8), DateTimeKind.Utc),
                FechaUltimaSincronizacion = DateTime.UtcNow, FlagProcesado = true
            };
            f.PayloadJson = JsonSerializer.Serialize(new { f.FichajeIdExterno, f.UserId, f.Entrada, f.Salida });
            f.Hash = Sha256($"fichaje-{hashOffset + i}");
            list.Add(f);
        }
        return list;
    }

    private static List<StagingPayHawkGasto> GenerateGastos(Project p, Period period, List<User> users, int hashOffset)
    {
        var list = new List<StagingPayHawkGasto>();
        int num = 3 + (p.Id + period.Id) % 4;
        for (int i = 0; i < num; i++)
        {
            var rec = users.Skip(11).Take(4).ToList()[i % 4];
            var date = period.FechaInicio.AddDays(new Random(Seed + p.Id + period.Id + i + 3000).Next(0, 27));
            if (date > period.FechaFin) date = period.FechaFin;
            var g = new StagingPayHawkGasto
            {
                GastoIdExterno = $"GH-{p.Id:00}{period.Id:00}{i:00}",
                UserId = rec.Id, ProjectId = p.Id, Fecha = date,
                Importe = 50 + (i * 11) % 200,
                Categoria = new[] { "Viajes", "Material", "Restauración", "Combustible" }[i % 4],
                FechaUltimaSincronizacion = DateTime.UtcNow, FlagProcesado = true
            };
            g.PayloadJson = JsonSerializer.Serialize(new { g.GastoIdExterno, g.UserId, g.ProjectId, g.Fecha, g.Importe, g.Categoria });
            g.Hash = Sha256($"gasto-{hashOffset + i}");
            list.Add(g);
        }
        return list;
    }

    private async Task<List<Closure>> SeedClosuresAsync(List<Project> projects, List<Period> periods, CancellationToken ct)
    {
        var closures = new List<Closure>();
        foreach (var period in periods.Take(3))
            foreach (var p in projects)
                closures.Add(NewClosureBare(p.Id, period.Id, EstadoClosure.Aprobado, ApprovalStep.SystemExports));

        var febrero = periods[3];
        for (int idx = 0; idx < projects.Count; idx++)
        {
            var paso = idx < 6 ? ApprovalStep.Fico : ApprovalStep.Backoffice;
            closures.Add(NewClosureBare(projects[idx].Id, febrero.Id, EstadoClosure.EnAprobacion, paso));
        }

        var marzo = periods[4];
        for (int i = 0; i < projects.Count; i++)
        {
            var p = projects[i];
            closures.Add(i < 5
                ? NewClosureBare(p.Id, marzo.Id, EstadoClosure.Borrador, ApprovalStep.ProjectManager)
                : NewClosureBare(p.Id, marzo.Id, EstadoClosure.Rechazado, ApprovalStep.ProjectManager));
        }

        _db.Closures.AddRange(closures);
        await _db.SaveChangesAsync(ct);

        return await _db.Closures.Include(c => c.Project).Include(c => c.Period).ToListAsync(ct);
    }

    private async Task SeedLineasYLogsAsync(List<Closure> closuresFull, List<Concept> concepts, List<User> users, CancellationToken ct)
    {
        var allLines = new List<ClosureLine>();
        var allLogs = new List<(ClosureLine line, CalculationResult result, int conceptId)>();
        var fieldUsers = users.Skip(11).Take(4).ToList();

        foreach (var c in closuresFull)
        {
            decimal coste = 0, factura = 0;
            var aplic = concepts.Where(cn => cn.FechaDesde <= c.Period.FechaFin &&
                                              (cn.FechaHasta == null || cn.FechaHasta >= c.Period.FechaInicio)).ToList();
            int userIdx = 0;
            foreach (var concept in aplic)
            {
                var result = await _engine.EvaluateAsync(concept, c, null, ct);
                var assignedUserId = concept.Tipo == TipoConcepto.Pago
                    ? fieldUsers[userIdx % fieldUsers.Count].Id
                    : (int?)null;
                var line = new ClosureLine
                {
                    ClosureId = c.Id, ConceptId = concept.Id, UserId = assignedUserId,
                    Importe = result.Resultado, DatosEntradaJson = result.InputsJson,
                    Tipo = concept.Tipo, TieneIncidencia = result.Incidencias.Any()
                };
                allLines.Add(line);
                allLogs.Add((line, result, concept.Id));
                if (concept.Tipo == TipoConcepto.Pago) coste += result.Resultado;
                else factura += result.Resultado;
                userIdx++;
            }
            c.CosteTotal = Math.Round(coste, 2);
            c.FacturacionTotal = Math.Round(factura, 2);
            c.Margen = Math.Round(factura - coste, 2);
        }

        _db.ClosureLines.AddRange(allLines);
        await _db.SaveChangesAsync(ct);

        var calcLogs = allLogs.Select(t => new CalculationLog
        {
            ClosureLineId = t.line.Id, ConceptId = t.conceptId,
            FormulaSnapshotJson = t.result.FormulaSnapshotJson, InputsJson = t.result.InputsJson,
            Resultado = t.result.Resultado,
            Incidencias = t.result.Incidencias.Any() ? JsonSerializer.Serialize(t.result.Incidencias) : null,
            SistemaOrigen = t.result.SistemaOrigen, Timestamp = DateTime.UtcNow
        }).ToList();
        _db.CalculationLogs.AddRange(calcLogs);
        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedApprovalsAsync(List<Closure> closuresFull, Dictionary<string, Role> rMap, Dictionary<string, User> uByEmail, CancellationToken ct)
    {
        var pmId = uByEmail["pm.alpha@sig.local"].Id;
        var bofId = uByEmail["backoffice1@sig.local"].Id;
        var ficoId = uByEmail["fico@sig.local"].Id;
        var dirId = uByEmail["direccion@sig.local"].Id;

        var approvals = new List<Approval>();
        var history = new List<ApprovalHistory>();
        var ts = DateTime.UtcNow.AddDays(-20);
        foreach (var c in closuresFull)
        {
            if (c.Estado == EstadoClosure.Aprobado)
            {
                approvals.Add(new Approval { ClosureId = c.Id, RoleId = rMap["ProjectManager"].Id, Paso = ApprovalStep.ProjectManager, UserId = pmId, Estado = EstadoApproval.Aprobado, FechaDecision = ts });
                approvals.Add(new Approval { ClosureId = c.Id, RoleId = rMap["Backoffice"].Id,     Paso = ApprovalStep.Backoffice,     UserId = bofId, Estado = EstadoApproval.Aprobado, FechaDecision = ts.AddHours(1) });
                approvals.Add(new Approval { ClosureId = c.Id, RoleId = rMap["Fico"].Id,           Paso = ApprovalStep.Fico,           UserId = ficoId, Estado = EstadoApproval.Aprobado, FechaDecision = ts.AddHours(2) });
                approvals.Add(new Approval { ClosureId = c.Id, RoleId = rMap["Direction"].Id,      Paso = ApprovalStep.Direction,      UserId = dirId, Estado = EstadoApproval.Aprobado, FechaDecision = ts.AddHours(3) });
                history.Add(new ApprovalHistory { ClosureId = c.Id, UserId = pmId, PasoOrigen = ApprovalStep.ProjectManager, PasoDestino = ApprovalStep.Backoffice, Accion = "Aprobar", Timestamp = ts });
                history.Add(new ApprovalHistory { ClosureId = c.Id, UserId = bofId, PasoOrigen = ApprovalStep.Backoffice, PasoDestino = ApprovalStep.Fico, Accion = "Aprobar", Timestamp = ts.AddHours(1) });
                history.Add(new ApprovalHistory { ClosureId = c.Id, UserId = ficoId, PasoOrigen = ApprovalStep.Fico, PasoDestino = ApprovalStep.Direction, Accion = "Aprobar", Timestamp = ts.AddHours(2) });
                history.Add(new ApprovalHistory { ClosureId = c.Id, UserId = dirId, PasoOrigen = ApprovalStep.Direction, PasoDestino = ApprovalStep.SystemExports, Accion = "Aprobar", Timestamp = ts.AddHours(3) });
            }
            else if (c.Estado == EstadoClosure.EnAprobacion)
            {
                approvals.Add(new Approval { ClosureId = c.Id, RoleId = rMap["ProjectManager"].Id, Paso = ApprovalStep.ProjectManager, UserId = pmId, Estado = EstadoApproval.Aprobado, FechaDecision = ts });
                history.Add(new ApprovalHistory { ClosureId = c.Id, UserId = pmId, PasoOrigen = ApprovalStep.ProjectManager, PasoDestino = ApprovalStep.Backoffice, Accion = "Aprobar", Timestamp = ts });
                if (c.PasoActual == ApprovalStep.Fico)
                {
                    approvals.Add(new Approval { ClosureId = c.Id, RoleId = rMap["Backoffice"].Id, Paso = ApprovalStep.Backoffice, UserId = bofId, Estado = EstadoApproval.Aprobado, FechaDecision = ts.AddHours(1) });
                    history.Add(new ApprovalHistory { ClosureId = c.Id, UserId = bofId, PasoOrigen = ApprovalStep.Backoffice, PasoDestino = ApprovalStep.Fico, Accion = "Aprobar", Timestamp = ts.AddHours(1) });
                    approvals.Add(new Approval { ClosureId = c.Id, RoleId = rMap["Fico"].Id, Paso = ApprovalStep.Fico, Estado = EstadoApproval.Pendiente });
                }
                else
                {
                    approvals.Add(new Approval { ClosureId = c.Id, RoleId = rMap["Backoffice"].Id, Paso = ApprovalStep.Backoffice, Estado = EstadoApproval.Pendiente });
                }
            }
            else if (c.Estado == EstadoClosure.Borrador)
            {
                approvals.Add(new Approval { ClosureId = c.Id, RoleId = rMap["ProjectManager"].Id, Paso = ApprovalStep.ProjectManager, Estado = EstadoApproval.Pendiente });
            }
            else if (c.Estado == EstadoClosure.Rechazado)
            {
                approvals.Add(new Approval { ClosureId = c.Id, RoleId = rMap["ProjectManager"].Id, Paso = ApprovalStep.ProjectManager, UserId = pmId, Estado = EstadoApproval.Aprobado, FechaDecision = ts });
                approvals.Add(new Approval { ClosureId = c.Id, RoleId = rMap["Backoffice"].Id, Paso = ApprovalStep.Backoffice, UserId = bofId, Estado = EstadoApproval.Rechazado, FechaDecision = ts.AddHours(1), Motivo = "Datos inconsistentes" });
                approvals.Add(new Approval { ClosureId = c.Id, RoleId = rMap["ProjectManager"].Id, Paso = ApprovalStep.ProjectManager, Estado = EstadoApproval.Pendiente });
                history.Add(new ApprovalHistory { ClosureId = c.Id, UserId = pmId, PasoOrigen = ApprovalStep.ProjectManager, PasoDestino = ApprovalStep.Backoffice, Accion = "Aprobar", Timestamp = ts });
                history.Add(new ApprovalHistory { ClosureId = c.Id, UserId = bofId, PasoOrigen = ApprovalStep.Backoffice, PasoDestino = ApprovalStep.ProjectManager, Accion = "Rechazar", Motivo = "Datos inconsistentes", Timestamp = ts.AddHours(1) });
            }
        }
        _db.Approvals.AddRange(approvals);
        _db.ApprovalHistory.AddRange(history);
        await _db.SaveChangesAsync(ct);
    }

    private async Task SeedAuditExtraAsync(Dictionary<string, User> uByEmail, CancellationToken ct)
    {
        var auditExtra = new List<AuditLog>();
        for (int i = 0; i < 30; i++)
        {
            auditExtra.Add(new AuditLog
            {
                UserId = uByEmail["admin@sig.local"].Id,
                EntityType = "User",
                EntityId = uByEmail["admin@sig.local"].Id.ToString(),
                Action = AuditAction.Login,
                Timestamp = DateTime.UtcNow.AddDays(-i),
                Ip = "127.0.0.1"
            });
        }
        _db.AuditLogs.AddRange(auditExtra);
        await _db.SaveChangesAsync(ct);
    }

    private static User New(string email, string nif, string nombre, string apellidos, Department? dep, string hash, Role role)
    {
        var u = new User
        {
            Email = email, NIF = nif, Nombre = nombre, Apellidos = apellidos,
            DepartmentId = dep?.Id, Department = dep,
            PasswordHash = hash, Estado = EstadoUsuario.Activo
        };
        u.UserRoles.Add(new UserRole { Role = role });
        return u;
    }

    private static User NewRecurso(string email, string nif, string nombre, string apellidos, Department? dep, string hash)
    {
        return new User
        {
            Email = email, NIF = nif, Nombre = nombre, Apellidos = apellidos,
            DepartmentId = dep?.Id, Department = dep,
            PasswordHash = hash, Estado = EstadoUsuario.Activo
        };
    }

    private static Project NewProject(string nombre, int clientId, int costCenterId, int pmUserId, DateOnly fechaAlta)
    {
        var p = new Project
        {
            Nombre = nombre, ClientId = clientId, FechaAlta = fechaAlta, Estado = EstadoProyecto.Activo,
            InterlocutorNombre = "Contacto " + nombre, InterlocutorEmail = "contacto@cliente.es", InterlocutorTelefono = "910000000"
        };
        p.ProjectCostCenters.Add(new ProjectCostCenter { CostCenterId = costCenterId });
        p.ProjectUsers.Add(new ProjectUser { UserId = pmUserId });
        return p;
    }

    private static Closure NewClosureBare(int projectId, int periodId, EstadoClosure estado, ApprovalStep paso)
    {
        return new Closure
        {
            ProjectId = projectId,
            PeriodId = periodId,
            Estado = estado,
            PasoActual = paso,
            Comentarios = $"Closure seed - estado {estado}",
            FechaCreacion = DateTime.UtcNow
        };
    }

    private static string Sha256(string s)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s));
        return Convert.ToHexString(bytes);
    }
}

#pragma warning restore S2245
