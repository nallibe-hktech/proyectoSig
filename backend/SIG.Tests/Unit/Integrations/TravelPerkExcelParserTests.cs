using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SIG.Application.Integrations;
using SIG.Infrastructure.Integrations.Fake;

namespace SIG.Tests.Unit.Integrations;

/// <summary>
/// Verifica el parser del Excel de TravelPerk con un libro construido en memoria que reproduce el formato real
/// (hoja "report", cabeceras por nombre, "Cost per traveler without tax" como coste sin IVA, Subscription sin Cost object).
/// </summary>
public class TravelPerkExcelParserTests
{
    private static XLWorkbook BuildWorkbook()
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("report");

        // Subconjunto representativo de las columnas reales, en orden arbitrario (el parser mapea por nombre).
        var headers = new[]
        {
            "Traveler first name", "Traveler email", "Trip ID", "Service", "Cost object",
            "Cost per traveler (EUR)", "Cost per traveler without tax", "Tax per traveler",
            "Currency code", "Expense date"
        };
        for (int c = 0; c < headers.Length; c++) ws.Cell(1, c + 1).Value = headers[c];

        //       fila first   email      trip  service                     costObject        EUR    sinIVA  tax    cur    fecha
        AddRow(ws, 2, "Ana", "ana@x.es", "T1", "Hotels",                  "0139_CLIENTE_A",  60.00,  60.00,  0.00, "EUR", "2026-05-31");
        AddRow(ws, 3, "Ana", "ana@x.es", "T1", "Premium Service",         "0139_CLIENTE_A",   2.42,   2.00,  0.42, "EUR", "2026-05-31");
        AddRow(ws, 4, "Leo", "leo@x.es", "T2", "Refund for train",        "0216_CLIENTE_D", -38.06, -38.04, -0.02, "EUR", "2026-05-15");
        AddRow(ws, 5, "SIG", "",         "",   "Subscription fee",        "",               119.79,  99.00, 20.79, "EUR", "2026-05-31");
        return wb;
    }

    private static void AddRow(IXLWorksheet ws, int r, string first, string email, string trip, string service,
        string costObject, double eur, double sinIva, double tax, string cur, string date)
    {
        ws.Cell(r, 1).Value = first;
        ws.Cell(r, 2).Value = email;
        ws.Cell(r, 3).Value = trip;
        ws.Cell(r, 4).Value = service;
        ws.Cell(r, 5).Value = costObject;
        ws.Cell(r, 6).Value = eur;
        ws.Cell(r, 7).Value = sinIva;
        ws.Cell(r, 8).Value = tax;
        ws.Cell(r, 9).Value = cur;
        ws.Cell(r, 10).Value = date;
    }

    [Fact]
    public void Parse_LeeLineasYMapeaColumnasClavePorNombre()
    {
        using var wb = BuildWorkbook();

        var lineas = TravelPerkExcelParser.Parse(wb, NullLogger.Instance);

        lineas.Should().HaveCount(4);

        var hotel = lineas.First(l => l.Service == "Hotels");
        hotel.CostObject.Should().Be("0139_CLIENTE_A");
        hotel.CosteSinIVA.Should().Be(60.00m); // toma "without tax", NO el EUR con IVA
        hotel.TripId.Should().Be("T1");
        hotel.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Parse_SubscriptionFeeSinCostObject_DejaCostObjectNull()
    {
        using var wb = BuildWorkbook();

        var lineas = TravelPerkExcelParser.Parse(wb, NullLogger.Instance);

        var sub = lineas.First(l => l.Service == "Subscription fee");
        sub.CostObject.Should().BeNull();
        sub.CosteSinIVA.Should().Be(99.00m);
    }

    [Fact]
    public void Parse_RefundMantieneImporteNegativo()
    {
        using var wb = BuildWorkbook();

        var lineas = TravelPerkExcelParser.Parse(wb, NullLogger.Instance);

        lineas.First(l => l.Service == "Refund for train").CosteSinIVA.Should().Be(-38.04m);
    }

    [Fact]
    public void Parse_EndToEnd_ParserMasImputador_CuadraLaImputacion()
    {
        using var wb = BuildWorkbook();

        var lineas = TravelPerkExcelParser.Parse(wb, NullLogger.Instance);
        var r = TravelPerkImputador.Imputar(lineas);

        // Subscription (sin Cost object) → 0423
        r.PorCeco.Single(c => c.EsGastoInternoSig).CosteSinIVA.Should().Be(99.00m);
        // 0139_CLIENTE_A = Hotels 60 + Premium 2 = 62
        r.PorCeco.Single(c => c.Ceco == "0139_CLIENTE_A").CosteSinIVA.Should().Be(62.00m);
        // Total = 60 + 2 - 38.04 + 99 = 122.96
        r.TotalSinIVA.Should().Be(122.96m);
    }

    [Fact]
    public void Parse_LibroSinHojaReport_UsaPrimeraHojaConCabeceraValida()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Hoja1");
        ws.Cell(1, 1).Value = "Service";
        ws.Cell(1, 2).Value = "Cost object";
        ws.Cell(1, 3).Value = "Cost per traveler without tax";
        ws.Cell(2, 1).Value = "Hotels";
        ws.Cell(2, 2).Value = "0139_CLIENTE_A";
        ws.Cell(2, 3).Value = 50.00;

        var lineas = TravelPerkExcelParser.Parse(wb, NullLogger.Instance);

        lineas.Should().ContainSingle();
        lineas[0].CosteSinIVA.Should().Be(50.00m);
    }
}
