using System.Text.Json;

namespace SIG.Application.Calculation;

/// <summary>
/// Plantillas JSON de fórmulas para cada tipo de concepto del catálogo SIG-ES.
/// Se usan para crear conceptos nuevos sin escribir JSON a mano.
/// </summary>
public static class FormulaTemplates
{
    // ── PAGOS ──

    /// <summary>Dieta/cuota por día trabajado: count distinct fechas × tarifa fija.</summary>
    public static string DietaPorDia(decimal cuota, string origen = "VisitasCelero") => Serializar(new
    {
        type = "BinaryOp",
        op = "Mul",
        left = new
        {
            type = "Aggregate",
            op = "Count",
            source = new { entity = origen, filters = origen == "VisitasCelero" ? new[] { new { field = "Estado", op = "Eq", value = "done" } } : Array.Empty<object>() },
            distinct = "Fecha"
        },
        right = new { type = "Number", value = cuota }
    });

    /// <summary>Cuota fija mensual: importe manual sin cálculo.</summary>
    public static string CuotaFija(decimal importe) => Serializar(new { type = "Number", value = importe });

    /// <summary>Cuota fija mensual por recurso: importe × count distinct empleados.</summary>
    public static string CuotaPorRecurso(decimal cuota, string origen = "VisitasCelero") => Serializar(new
    {
        type = "BinaryOp",
        op = "Mul",
        left = new { type = "Number", value = cuota },
        right = new
        {
            type = "Aggregate",
            op = "Count",
            source = new { entity = origen },
            distinct = "UserId"
        }
    });

    /// <summary>Cuota por visita: count visitas "done" × tarifa.</summary>
    public static string CuotaPorVisita(decimal cuota, string origen = "VisitasCelero") => Serializar(new
    {
        type = "BinaryOp",
        op = "Mul",
        left = new
        {
            type = "Aggregate",
            op = "Count",
            source = new { entity = origen, filters = origen == "VisitasCelero" ? new[] { new { field = "Estado", op = "Eq", value = "done" } } : Array.Empty<object>() }
        },
        right = new { type = "Number", value = cuota }
    });

    /// <summary>Cuota por hora trabajada: sum horas × tarifa.</summary>
    public static string CuotaPorHoraTrabajada(decimal tarifa, string origen = "HorasIntratime") => Serializar(new
    {
        type = "BinaryOp",
        op = "Mul",
        left = new { type = "Aggregate", op = "Sum", source = new { entity = origen }, field = "Horas" },
        right = new { type = "Number", value = tarifa }
    });

    /// <summary>Cuota por hora estimada: sum horas estimadas (mapeo Celero) × coste/hora.</summary>
    public static string CuotaPorHoraEstimada(decimal costeHora) => Serializar(new
    {
        type = "BinaryOp",
        op = "Mul",
        left = new { type = "Aggregate", op = "Sum", source = new { entity = "VisitasCelero" }, field = "Horas" },
        right = new { type = "Number", value = costeHora }
    });

    /// <summary>Kilometraje PAGO: sum importe PayHawk categoría "kilometraje" approved.</summary>
    public static string KilometrajePago() => Serializar(new
    {
        type = "Aggregate",
        op = "Sum",
        source = new
        {
            entity = "GastosPayHawk",
            filters = new[]
            {
                new { field = "Categoria", op = "Eq", value = "kilometraje" },
                new { field = "ApprovalStatus", op = "Eq", value = "Approved" }
            }
        },
        field = "Importe"
    });

    /// <summary>Kilometraje FACTURACIÓN: sum km × tarifa por km.</summary>
    public static string KilometrajeFacturacion(decimal tarifaKm) => Serializar(new
    {
        type = "BinaryOp",
        op = "Mul",
        left = new
        {
            type = "Aggregate",
            op = "Sum",
            source = new
            {
                entity = "GastosPayHawk",
                filters = new[]
                {
                    new { field = "Categoria", op = "Eq", value = "kilometraje" },
                    new { field = "ApprovalStatus", op = "Eq", value = "Approved" }
                }
            },
            field = "Km"
        },
        right = new { type = "Number", value = tarifaKm }
    });

    /// <summary>Gastos PayHawk: sum importe approved excluyendo dietas y kilometraje.</summary>
    public static string GastosPayHawk() => Serializar(new
    {
        type = "Aggregate",
        op = "Sum",
        source = new
        {
            entity = "GastosPayHawk",
            filters = new[]
            {
                new { field = "ApprovalStatus", op = "Eq", value = "Approved" },
                new { field = "Categoria", op = "Neq", value = "Dietas" },
                new { field = "Categoria", op = "Neq", value = "kilometraje" }
            }
        },
        field = "Importe"
    });

    /// <summary>Dietas PayHawk: sum importe categoría "Dietas" approved.</summary>
    public static string DietasPayHawk() => Serializar(new
    {
        type = "Aggregate",
        op = "Sum",
        source = new
        {
            entity = "GastosPayHawk",
            filters = new[]
            {
                new { field = "Categoria", op = "Eq", value = "Dietas" },
                new { field = "ApprovalStatus", op = "Eq", value = "Approved" }
            }
        },
        field = "Importe"
    });

    /// <summary>Salario fijo: sum salario base de A3 Innuva.</summary>
    public static string SalarioFijo() => Serializar(new
    {
        type = "Aggregate",
        op = "Sum",
        source = new { entity = "SalariosA3" },
        field = "Importe"
    });

    /// <summary>Salario ÷ horas dedicadas: redistribuye salario base según % tiempo en este servicio.</summary>
    public static string SalarioDivididoHoras(string entity = "VisitasCelero", string field = "Horas") => Serializar(new
    {
        type = "CrossService",
        entity = entity,
        field = field,
        baseSalary = new
        {
            type = "Aggregate",
            op = "Sum",
            source = new { entity = "SalariosA3" },
            field = "Importe"
        }
    });

    /// <summary>Viajes TravelPerk: sum coste sin IVA.</summary>
    public static string ViajesTravelPerk() => Serializar(new
    {
        type = "Aggregate",
        op = "Sum",
        source = new { entity = "ViajesTravelPerk" },
        field = "Importe"
    });

    /// <summary>Incentivo: importe manual (se gestiona vía AddIncentivoAsync).</summary>
    public static string Incentivo() => Serializar(new { type = "Number", value = 0 });

    // ── FACTURACIÓN ──

    /// <summary>Fee sobre otros conceptos: porcentaje sobre importes de conceptos base.</summary>
    public static string FeeSobreConceptos(params int[] conceptIds) => Serializar(new
    {
        type = "ConceptRef",
        conceptIds = conceptIds ?? Array.Empty<int>()
    });

    /// <summary>Fee sobre conceptos específicos con porcentaje.</summary>
    public static string FeeSobreConceptosConPct(decimal porcentaje, params int[] conceptIds) => Serializar(new
    {
        type = "BinaryOp",
        op = "Mul",
        left = new { type = "ConceptRef", conceptIds = conceptIds ?? Array.Empty<int>() },
        right = new { type = "Number", value = porcentaje / 100m }
    });

    /// <summary>Logística con margen: coste base × (1 + margen%).</summary>
    public static string LogisticaConMargen(decimal margenPct) => Serializar(new
    {
        type = "BinaryOp",
        op = "Pct",
        left = new { type = "Number", value = 0 }, // el coste base se introduce manual
        right = new { type = "Number", value = margenPct }
    });

    /// <summary>Cuota por hora con tramos: tarifa base + incrementos por tramo de minutos.</summary>
    public static string CuotaHoraTramos(int minutosBase, decimal tarifaBase, decimal incrementoPorTramo, int minutosTramo) => Serializar(new
    {
        type = "BinaryOp",
        op = "Mul",
        left = new { type = "Aggregate", op = "Count", source = new { entity = "VisitasCelero" } },
        right = new
        {
            type = "Tramos",
            cantidad = new { type = "Number", value = minutosBase },
            tramos = new[]
            {
                new { hasta = (decimal?)minutosBase, precio = tarifaBase },
                new { hasta = (decimal?)(minutosBase + minutosTramo), precio = incrementoPorTramo },
                new { hasta = (decimal?)(minutosBase + minutosTramo * 2), precio = incrementoPorTramo },
                new { hasta = (decimal?)null, precio = incrementoPorTramo }
            }
        }
    });

    // ── HELPERS ──

    private static string Serializar(object obj) =>
        JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = false });
}
