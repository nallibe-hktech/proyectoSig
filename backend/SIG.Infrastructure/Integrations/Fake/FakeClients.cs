using Bogus;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Integrations;

namespace SIG.Infrastructure.Integrations.Fake;

#pragma warning disable S2245, S2696 // Random usado intencionalmente para datos de prueba deterministas (semilla fija); static ctor pattern ok

internal static class FakeSeed
{
    public const int Seed = 20260101;
    static FakeSeed() { Randomizer.Seed = new Random(Seed); }
}

public class CeleroFakeClient : ICeleroClient
{
    static CeleroFakeClient() { _ = FakeSeed.Seed; }

    public Task<IReadOnlyList<CeleroVisitaDto>> GetVisitasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        var servicios = new[]
        {
            "Implantación Madrid",
            "Visitas GPV España",
            "Formación Equipos",
            "Implantación Barcelona",
            "Visitas Premium",
            "Mensualidad",
            "Operaciones Campo",
            "Formación Premium"
        };

        var misiones = new[]
        {
            "Implantación Madrid",
            "Visitas GPV España",
            "Formación Equipos",
            "Implantación Barcelona",
            "Visitas Premium",
            "Mensualidad",
            "Operaciones Campo",
            "Formación Premium"
        };

        var nifs = new[]
        {
            "12345678A", "23456789B", "34567890C", "45678901D", "56789012E",
            "67890123F", "78901234G", "89012345H", "90123456J", "01234567K",
            "11234567L", "21234567M", "31234567N", "41234567P", "51234567Q"
        };

        var faker = new Faker<CeleroVisitaDto>()
            .CustomInstantiator(f => new CeleroVisitaDto(
                $"VISIT-{f.Random.AlphaNumeric(8).ToUpper()}",
                f.PickRandom(nifs),
                f.PickRandom(servicios),
                f.PickRandom(misiones),
                DateOnly.FromDateTime(f.Date.Between(desde.ToDateTime(TimeOnly.MinValue), hasta.ToDateTime(TimeOnly.MaxValue)))
            ));
        // Semilla local fija: cada llamada produce el MISMO lote → la sincronización es idempotente
        // (la 2ª sync detecta todos los registros como duplicados por hash SHA256).
        var list = faker.UseSeed(FakeSeed.Seed).Generate(50);
        return Task.FromResult<IReadOnlyList<CeleroVisitaDto>>(list);
    }
}

public class BizneoFakeClient : IBizneoClient
{
    public Task<IReadOnlyList<BizneoEmpleadoDto>> GetEmpleadosAsync(CancellationToken ct)
    {
        var faker = new Faker<BizneoEmpleadoDto>()
            .CustomInstantiator(f => new BizneoEmpleadoDto(
                $"EMP-{f.Random.AlphaNumeric(6).ToUpper()}",
                $"{f.Random.Long(10000000, 99999999)}A",
                f.Name.FullName(),
                f.PickRandom("Operaciones", "Backoffice", "Finanzas", "Dirección")
            ));
        var list = faker.Generate(15);
        return Task.FromResult<IReadOnlyList<BizneoEmpleadoDto>>(list);
    }

    public Task<IReadOnlyList<BizneoAbsenceDto>> GetAbsencesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        var faker = new Faker<BizneoAbsenceDto>()
            .CustomInstantiator(f => new BizneoAbsenceDto(
                $"BA-{f.Random.AlphaNumeric(8).ToUpper()}",
                f.Random.Int(1, 15),
                f.Random.Int(1, 8),
                DateOnly.FromDateTime(f.Date.Between(desde.ToDateTime(TimeOnly.MinValue), hasta.ToDateTime(TimeOnly.MaxValue))),
                Math.Round(f.Random.Decimal(1, 10), 2)
            ));
        var list = faker.Generate(80);
        return Task.FromResult<IReadOnlyList<BizneoAbsenceDto>>(list);
    }
}

public class IntratimeFakeClient : IIntratimeClient
{
    static IntratimeFakeClient() { _ = FakeSeed.Seed; }

    public Task<IReadOnlyList<IntratimeEmpleadoDto>> GetEmpleadosAsync(CancellationToken ct)
    {
        var nifs = new[]
        {
            "12345678A", "23456789B", "34567890C", "45678901D", "56789012E",
            "67890123F", "78901234G", "89012345H", "90123456J", "01234567K",
            "11234567L", "21234567M", "31234567N", "41234567P", "51234567Q"
        };

        var faker = new Faker<IntratimeEmpleadoDto>()
            .CustomInstantiator(f => new IntratimeEmpleadoDto(
                f.Random.Int(20000, 21000).ToString(),  // UserIdExterno (simular ID de Intratime)
                f.Name.FullName(),
                f.Internet.Email(),
                f.PickRandom(nifs),
                f.Random.AlphaNumeric(6),
                f.Random.Int(1, 5)
            ));
        var list = faker.Generate(15);
        return Task.FromResult<IReadOnlyList<IntratimeEmpleadoDto>>(list);
    }

    public Task<IReadOnlyList<IntratimeFichajeDto>> GetFichajesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        var faker = new Faker<IntratimeFichajeDto>()
            .CustomInstantiator(f =>
            {
                var entrada = DateTime.SpecifyKind(f.Date.Between(desde.ToDateTime(TimeOnly.MinValue), hasta.ToDateTime(TimeOnly.MaxValue)), DateTimeKind.Utc);
                var salida = entrada.AddHours(f.Random.Double(7, 9));
                return new IntratimeFichajeDto(
                    $"FIC-{f.Random.AlphaNumeric(8).ToUpper()}",
                    f.Random.Int(20000, 20015).ToString(),  // UserIdExterno como string
                    entrada,
                    DateTime.SpecifyKind(salida, DateTimeKind.Utc)
                );
            });
        var list = faker.Generate(120);
        return Task.FromResult<IReadOnlyList<IntratimeFichajeDto>>(list);
    }

    public Task<IReadOnlyList<IntratimeClockingRequestDto>> GetClockingRequestsAsync(int year, CancellationToken ct)
    {
        var estados = new[] { "Pendiente", "Aprobado", "Rechazado" };
        var tipos = new[] { "Ajuste", "Corrección", "Validación" };

        var faker = new Faker<IntratimeClockingRequestDto>()
            .CustomInstantiator(f => new IntratimeClockingRequestDto(
                $"REQ-{f.Random.AlphaNumeric(8).ToUpper()}",
                f.Random.Int(20000, 20015).ToString(),  // UserIdExterno
                DateTime.SpecifyKind(f.Date.Past(365, DateTime.UtcNow), DateTimeKind.Utc),
                f.PickRandom(tipos),
                f.PickRandom(estados),
                f.Lorem.Sentence(3),
                "09:00",
                "17:00"
            ));
        var list = faker.Generate(25);
        return Task.FromResult<IReadOnlyList<IntratimeClockingRequestDto>>(list);
    }

    public Task<IReadOnlyList<IntratimeExpenseDto>> GetExpensesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        var expenseNames = new[] { "Comidas", "Transporte", "Hotel", "Combustible", "Teléfono", "Material" };
        var faker = new Faker<IntratimeExpenseDto>()
            .CustomInstantiator(f =>
            {
                var fecha = f.Date.Between(desde.ToDateTime(TimeOnly.MinValue), hasta.ToDateTime(TimeOnly.MaxValue));
                return new IntratimeExpenseDto(
                    $"EXP-{f.Random.AlphaNumeric(8).ToUpper()}",
                    f.Random.Int(20000, 20015).ToString(),  // UserIdExterno
                    fecha,
                    Math.Round(f.Random.Decimal(10, 200), 2),  // Cantidad en euros
                    f.PickRandom(expenseNames),
                    f.Lorem.Sentence(2),
                    f.Random.Bool(0.6f) ? f.PickRandom("Proyecto A", "Proyecto B", "Proyecto C") : null  // ProyectoNombre (60% probabilidad)
                );
            });
        var list = faker.Generate(35);
        return Task.FromResult<IReadOnlyList<IntratimeExpenseDto>>(list);
    }

    public Task<IReadOnlyList<IntratimeProyectoDto>> GetProyectosAsync(CancellationToken ct)
    {
        var clientNames = new[] { "TERESA CARLES", "APPLE", "SAMSUNG", "MICROSOFT", "GOOGLE" };
        var ciudades = new[] { "Barcelona", "Madrid", "Valencia", "Sevilla", "Bilbao" };
        var regiones = new[] { "Cataluña", "Madrid", "Valencia", "Andalucía", "País Vasco" };

        var faker = new Faker<IntratimeProyectoDto>()
            .CustomInstantiator(f =>
            {
                var clientName = f.PickRandom(clientNames);
                var ciudad = f.PickRandom(ciudades);
                var region = f.PickRandom(regiones);
                var usuariosIds = new List<string>();
                for (int i = 0; i < f.Random.Int(2, 5); i++)
                    usuariosIds.Add(f.Random.Int(20000, 21000).ToString());

                return new IntratimeProyectoDto(
                    f.Random.Int(70000, 80000).ToString(),  // PROJECT_ID
                    $"{clientName} - {f.Lorem.Word()}",     // PROJECT_NAME
                    new IntratimeClienteDto(
                        f.Random.Int(40000, 50000).ToString(),  // CLIENT_ID
                        clientName + " MANUFACTURING",
                        "España",
                        region,
                        ciudad,
                        f.Address.StreetAddress()
                    ),
                    usuariosIds
                );
            });
        var list = faker.Generate(8);
        return Task.FromResult<IReadOnlyList<IntratimeProyectoDto>>(list);
    }
}

public class PayHawkFakeClient : IPayHawkClient
{
    public Task<IReadOnlyList<PayHawkGastoDto>> GetGastosAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        var faker = new Faker<PayHawkGastoDto>()
            .CustomInstantiator(f => new PayHawkGastoDto(
                $"GH-{f.Random.AlphaNumeric(8).ToUpper()}",
                f.Random.Int(1, 15),
                f.Random.Int(1, 8),
                DateOnly.FromDateTime(f.Date.Between(desde.ToDateTime(TimeOnly.MinValue), hasta.ToDateTime(TimeOnly.MaxValue))),
                Math.Round(f.Random.Decimal(10, 500), 2),
                f.PickRandom("Viajes", "Material", "Restauración", "Combustible", "Alojamiento")
            ));
        var list = faker.Generate(40);
        return Task.FromResult<IReadOnlyList<PayHawkGastoDto>>(list);
    }
}

public class SgpvFakeClient : ISgpvClient
{
    public Task<IReadOnlyList<SgpvVisitaDto>> GetVisitasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        var centros = new[]
        {
            ("CENTRO001", "Centro Madrid Centro"),
            ("CENTRO002", "Centro Madrid Norte"),
            ("CENTRO003", "Centro Barcelona"),
            ("CENTRO004", "Centro Valencia"),
            ("CENTRO005", "Centro Sevilla")
        };

        var servicios = new[]
        {
            "Visitas GPV España",
            "Visitas Premium",
            "Operaciones Campo",
            "Formación Equipos",
            "Implantación Madrid"
        };

        var nifs = new[]
        {
            "12345678A", "23456789B", "34567890C", "45678901D", "56789012E",
            "67890123F", "78901234G", "89012345H", "90123456J", "01234567K"
        };

        var faker = new Faker<SgpvVisitaDto>()
            .CustomInstantiator(f =>
            {
                var centro = f.PickRandom(centros);
                return new SgpvVisitaDto(
                    $"SGPV-{f.Random.AlphaNumeric(8).ToUpper()}",
                    f.PickRandom(nifs),
                    centro.Item1,
                    centro.Item2,
                    f.PickRandom(servicios),
                    DateOnly.FromDateTime(f.Date.Between(desde.ToDateTime(TimeOnly.MinValue), hasta.ToDateTime(TimeOnly.MaxValue))),
                    Math.Round(f.Random.Decimal(0.5m, 8m), 2)
                );
            });
        var list = faker.Generate(30);
        return Task.FromResult<IReadOnlyList<SgpvVisitaDto>>(list);
    }

    public Task<IReadOnlyList<SgpvProductoDto>> GetProductosAsync(CancellationToken ct)
    {
        var clientes = new[] { "SPONTEX", "UNICS", "ROMMER" };
        var categorias = new[] { "BAYETAS", "ESPONJAS", "PAÑOS", "CEPILLOS", "ACCESORIOS" };
        var subcategorias = new[] { "MICROFIBRA", "DESECHABLES", "POSAVAJILLAS", "LIMPIADORAS" };
        var marcas = new[] { "SPONTEX", "UNICS", "VILEDA", "SWIRL", "O-CEDAR" };

        var faker = new Faker<SgpvProductoDto>()
            .CustomInstantiator(f =>
            {
                var cliente = f.PickRandom(clientes);
                var categoria = f.PickRandom(categorias);
                return new SgpvProductoDto(
                    f.Random.Number(1, 997).ToString(),
                    f.Random.Number(1, 10).ToString(),
                    cliente,
                    categoria,
                    f.PickRandom(subcategorias),
                    f.Random.Number(10000000, 99999999).ToString(),
                    f.Commerce.ProductName(),
                    $"{f.Random.Long(1000000000, 9999999999)}",
                    f.PickRandom(marcas),
                    Math.Round(f.Random.Decimal(1, 20), 2).ToString(),
                    f.PickRandom(new[] { "Si", "No" }),
                    f.Random.Bool(0.9f) // 90% activos
                );
            });

        var list = faker.Generate(25);
        return Task.FromResult<IReadOnlyList<SgpvProductoDto>>(list);
    }
}

public class A3InnuvaFakeClient : IA3InnuvaClient
{
    public Task<IReadOnlyList<A3InnuvaEmpleadoDto>> GetEmpleadosAsync(CancellationToken ct)
    {
        var departamentos = new[] { "RRHH", "Finanzas", "Operaciones", "Dirección", "Backoffice" };
        var faker = new Faker<A3InnuvaEmpleadoDto>()
            .CustomInstantiator(f => new A3InnuvaEmpleadoDto(
                $"A3EMP-{f.Random.AlphaNumeric(6).ToUpper()}",
                $"{f.Random.Long(10000000, 99999999)}A",
                f.Name.FullName(),
                f.PickRandom(departamentos),
                Math.Round(f.Random.Decimal(20000, 80000), 2),
                DateTime.UtcNow
            ));
        var list = faker.Generate(20);
        return Task.FromResult<IReadOnlyList<A3InnuvaEmpleadoDto>>(list);
    }
}

public class TravelPerkFakeClient : ITravelPerkClient
{
    public Task<IReadOnlyList<TravelPerkViajeDto>> GetViajesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        var estados = new[] { "pending", "approved", "completed", "rejected" };
        var faker = new Faker<TravelPerkViajeDto>()
            .CustomInstantiator(f =>
            {
                var inicio = f.Date.Between(desde.ToDateTime(TimeOnly.MinValue), hasta.ToDateTime(TimeOnly.MaxValue));
                var fin = inicio.AddDays(f.Random.Int(1, 7));
                return new TravelPerkViajeDto(
                    $"TP-{f.Random.AlphaNumeric(8).ToUpper()}",
                    f.Name.FullName(),
                    DateOnly.FromDateTime(inicio),
                    DateOnly.FromDateTime(fin),
                    Math.Round(f.Random.Decimal(500, 5000), 2),
                    f.PickRandom(estados)
                );
            });
        var list = faker.Generate(25);
        return Task.FromResult<IReadOnlyList<TravelPerkViajeDto>>(list);
    }
}

public class A3InnuvaNominasFakeClient : IA3InnuvaNominasClient
{
    static A3InnuvaNominasFakeClient() { _ = FakeSeed.Seed; }

    public Task<IReadOnlyList<A3InnuvaNominasCompanyDto>> GetCompaniesAsync(
        int pageNumber = 1, int pageSize = 25, DateTime? lastUpdate = null, CancellationToken ct = default)
    {
        var companies = new List<A3InnuvaNominasCompanyDto>
        {
            new("1", "1", "SERVICE INNOVATIVO GROUP ESPAÑA", "2Q4YX", "Madrid", "Madrid", "España", "plataforma.sig@sigespana.es", "")
        };
        return Task.FromResult<IReadOnlyList<A3InnuvaNominasCompanyDto>>(companies);
    }

    public Task<IReadOnlyList<A3InnuvaNominasPayrollDto>> GetPayrollsAsync(
        string companyCode, int pageNumber = 1, int pageSize = 25, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken ct = default)
    {
        var nifs = new[] { "12345678A", "23456789B", "34567890C", "45678901D", "56789012E" };
        var faker = new Faker<A3InnuvaNominasPayrollDto>()
            .CustomInstantiator(f => new A3InnuvaNominasPayrollDto(
                $"PAY-{f.Random.AlphaNumeric(6).ToUpper()}",
                f.Random.AlphaNumeric(6),
                f.Name.FullName(),
                "2026-01",
                Math.Round(f.Random.Decimal(2000, 3500), 2),
                Math.Round(f.Random.Decimal(300, 600), 2),
                Math.Round(f.Random.Decimal(1400, 3000), 2),
                DateTime.UtcNow
            ));
        var list = faker.Generate(pageSize);
        return Task.FromResult<IReadOnlyList<A3InnuvaNominasPayrollDto>>(list);
    }

    public Task<IReadOnlyList<EmployeeDto>> GetEmployeesAsync(
        int pageNumber = 1, int pageSize = 25, CancellationToken ct = default)
    {
        var nifs = new[] { "12345678A", "23456789B", "34567890C", "45678901D", "56789012E" };
        var faker = new Faker<EmployeeDto>()
            .CustomInstantiator(f => new EmployeeDto(
                f.Random.AlphaNumeric(6),
                f.Random.AlphaNumeric(4),
                f.Name.FullName(),
                f.PickRandom(nifs),
                f.Random.Int(1000, 9999).ToString(),
                DateTime.Now.AddYears(-f.Random.Int(1, 10))
            ));
        var list = faker.Generate(pageSize);
        return Task.FromResult<IReadOnlyList<EmployeeDto>>(list);
    }

    public Task<IReadOnlyList<ConceptoDto>> GetConceptosAsync(
        string employeeCode, int pageNumber = 1, int pageSize = 25, CancellationToken ct = default)
    {
        var conceptos = new List<ConceptoDto>
        {
            new(001, "Salario Base", 2500m, "E", false, false, "Percepciones"),
            new(002, "Complemento Antigüedad", 300m, "E", false, false, "Percepciones"),
            new(003, "IRPF", -400m, "D", false, false, "Descuentos"),
            new(004, "Seguridad Social", -250m, "D", false, false, "Descuentos"),
            new(005, "Bono Desempeño", 500m, "E", false, false, "Percepciones"),
        };
        return Task.FromResult<IReadOnlyList<ConceptoDto>>(conceptos);
    }

    public Task<string> WritePayrollAsync(
        string companyCode, string employeeCode, string periodCode, decimal percepciones, decimal descuentos, decimal neto, CancellationToken ct = default)
    {
        return Task.FromResult($"{{\"status\":\"success\",\"employeeCode\":\"{employeeCode}\",\"periodCode\":\"{periodCode}\",\"salaryProcessed\":true}}");
    }

    // PHASE 1 REDESIGNED: Fake implementations for real endpoints
    public Task<IReadOnlyList<SalaryDto>> GetSalaryAsync(string companyCode, string employeeCode, CancellationToken ct = default)
    {
        var salaries = new List<SalaryDto>
        {
            new($"{employeeCode}_salary", employeeCode, "12345678A", 2500m, 2000m, "EUR", DateTime.Now.AddYears(-2), null)
        };
        return Task.FromResult<IReadOnlyList<SalaryDto>>(salaries);
    }

    public Task<IReadOnlyList<IRPFDto>> GetIRPFAsync(string companyCode, string employeeCode, CancellationToken ct = default)
    {
        var irpf = new List<IRPFDto>
        {
            new($"{employeeCode}_irpf", employeeCode, "12345678A", "IRPF", 21m, 500m, DateTime.Now.AddYears(-1), null)
        };
        return Task.FromResult<IReadOnlyList<IRPFDto>>(irpf);
    }

    public Task<IReadOnlyList<RemunerationDto>> GetRemunerationAsync(string companyCode, string employeeCode, CancellationToken ct = default)
    {
        var remunerations = new List<RemunerationDto>
        {
            new($"{employeeCode}_rem_bonus", employeeCode, "12345678A", "Bono", 500m, "Desempeño", DateTime.Now.AddMonths(-3), null)
        };
        return Task.FromResult<IReadOnlyList<RemunerationDto>>(remunerations);
    }

    public Task<IReadOnlyList<BankAccountDto>> GetBankAccountsAsync(string companyCode, string employeeCode, CancellationToken ct = default)
    {
        var bankAccounts = new List<BankAccountDto>
        {
            new($"{employeeCode}_bank", employeeCode, "12345678A", "ES9121000418450200051332", "BBVAESMM", "Juan García López", "Principal", true, DateTime.Now.AddYears(-3), null)
        };
        return Task.FromResult<IReadOnlyList<BankAccountDto>>(bankAccounts);
    }

    public Task<IReadOnlyList<AgreementDto>> GetAgreementsAsync(string companyCode, string employeeCode, CancellationToken ct = default)
    {
        var agreements = new List<AgreementDto>
        {
            new($"{employeeCode}_agree", employeeCode, "12345678A", "COL_2024", "Convenio Sector Servicios 2024", "Colectivo", DateTime.Parse("2024-01-01"), DateTime.Parse("2024-12-31"), "Acuerdo negociación colectiva sectorial")
        };
        return Task.FromResult<IReadOnlyList<AgreementDto>>(agreements);
    }

    public Task<IReadOnlyList<ContractAgreementDto>> GetContractAgreementAsync(
        string employeeCode,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<ContractAgreementDto>>(new List<ContractAgreementDto>());
    }

    public Task<IReadOnlyList<ContractTimetableDto>> GetContractTimetableAsync(
        string employeeCode,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<ContractTimetableDto>>(new List<ContractTimetableDto>());
    }
}

#pragma warning restore S2245, S2696
