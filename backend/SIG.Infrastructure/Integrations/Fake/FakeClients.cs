using Bogus;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Integrations;

namespace SIG.Infrastructure.Integrations.Fake;

#pragma warning disable S2245 // Random usado intencionalmente para datos de prueba deterministas (semilla fija)

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
        var list = faker.Generate(50);
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
    public Task<IReadOnlyList<IntratimeFichajeDto>> GetFichajesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        var faker = new Faker<IntratimeFichajeDto>()
            .CustomInstantiator(f =>
            {
                var entrada = DateTime.SpecifyKind(f.Date.Between(desde.ToDateTime(TimeOnly.MinValue), hasta.ToDateTime(TimeOnly.MaxValue)), DateTimeKind.Utc);
                var salida = entrada.AddHours(f.Random.Double(7, 9));
                return new IntratimeFichajeDto(
                    $"FIC-{f.Random.AlphaNumeric(8).ToUpper()}",
                    f.Random.Int(1, 15),
                    entrada,
                    DateTime.SpecifyKind(salida, DateTimeKind.Utc)
                );
            });
        var list = faker.Generate(120);
        return Task.FromResult<IReadOnlyList<IntratimeFichajeDto>>(list);
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
                Math.Round(f.Random.Decimal(20000, 80000), 2)
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

#pragma warning restore S2245
