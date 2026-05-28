using Bogus;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Integrations;

namespace SIG.Infrastructure.Integrations.Fake;

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
        Randomizer.Seed = new Random(FakeSeed.Seed);
        var faker = new Faker<CeleroVisitaDto>()
            .CustomInstantiator(f => new CeleroVisitaDto(
                $"VC-{f.Random.AlphaNumeric(8).ToUpper()}",
                f.Random.Int(1, 15),
                f.Random.Int(1, 8),
                f.Random.Int(1, 25),
                DateOnly.FromDateTime(f.Date.Between(desde.ToDateTime(TimeOnly.MinValue), hasta.ToDateTime(TimeOnly.MaxValue))),
                f.Random.Int(1, 2),
                f.Random.Int(0, 1)
            ));
        var list = faker.Generate(50);
        return Task.FromResult<IReadOnlyList<CeleroVisitaDto>>(list);
    }
}

public class BizneoFakeClient : IBizneoClient
{
    public Task<IReadOnlyList<BizneoEmpleadoDto>> GetEmpleadosAsync(CancellationToken ct)
    {
        Randomizer.Seed = new Random(FakeSeed.Seed);
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

    public Task<IReadOnlyList<BizneoHoraDto>> GetHorasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        Randomizer.Seed = new Random(FakeSeed.Seed);
        var faker = new Faker<BizneoHoraDto>()
            .CustomInstantiator(f => new BizneoHoraDto(
                $"BH-{f.Random.AlphaNumeric(8).ToUpper()}",
                f.Random.Int(1, 15),
                f.Random.Int(1, 8),
                DateOnly.FromDateTime(f.Date.Between(desde.ToDateTime(TimeOnly.MinValue), hasta.ToDateTime(TimeOnly.MaxValue))),
                Math.Round(f.Random.Decimal(1, 10), 2)
            ));
        var list = faker.Generate(80);
        return Task.FromResult<IReadOnlyList<BizneoHoraDto>>(list);
    }
}

public class IntratimeFakeClient : IIntratimeClient
{
    public Task<IReadOnlyList<IntratimeFichajeDto>> GetFichajesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        Randomizer.Seed = new Random(FakeSeed.Seed);
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
        Randomizer.Seed = new Random(FakeSeed.Seed);
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
