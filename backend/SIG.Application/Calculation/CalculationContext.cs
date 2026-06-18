using SIG.Application.Calculation.Nodes;
using SIG.Domain.Entities;
using SIG.Domain.Entities.Staging;
using SIG.Domain.Exceptions;

namespace SIG.Application.Calculation;

public class CalculationContext
{
    public List<StagingCeleroVisita> Visitas { get; set; } = new();
    public List<StagingBizneoAbsence> HorasBizneo { get; set; } = new();
    public List<StagingIntratimeFichaje> Fichajes { get; set; } = new();
    public List<StagingPayHawkGasto> Gastos { get; set; } = new();
    public List<TarifaServicio> Tarifas { get; set; } = new();
    public List<Variable> Variables { get; set; } = new();
    public List<StagingSgpvVisita> VisitasSgpv { get; set; } = new();
    public HashSet<string> SistemasUsados { get; } = new();
    public Dictionary<string, object> UsedInputs { get; } = new();

    public List<RowAdapter> FilteredRows(SourceNode source, CalculationTarget target, int? recursoId)
    {
        SistemasUsados.Add(EntityToSistema(source.Entity));

        IEnumerable<RowAdapter> baseRows = source.Entity switch
        {
            "GastosPayHawk" => Gastos.Select(g => RowAdapter.FromGasto(g)),
            "VisitasCelero" => Visitas.Select(v => RowAdapter.FromVisita(v)),
            "HorasBizneo" => HorasBizneo.Select(h => RowAdapter.FromHora(h)),
            "HorasIntratime" => Fichajes.Select(f => RowAdapter.FromFichaje(f)),
            "TarifasServicio" or "TarifasProyecto" => Tarifas.Select(t => RowAdapter.FromTarifa(t)),
            "VisitasSgpv" => VisitasSgpv.Select(s => RowAdapter.FromSgpvVisita(s)),
            _ => throw new FormulaInvalidException($"Entidad desconocida: {source.Entity}")
        };

        // Filtros implícitos: período + projectId (si campo existe) + userId opcional
        var desde = target.Period.FechaInicio;
        var hasta = target.Period.FechaFin;

        var rows = baseRows.Where(r =>
        {
            if (r.Fecha.HasValue && (r.Fecha.Value < desde || r.Fecha.Value > hasta)) return false;
            if (r.ServiceId.HasValue && r.ServiceId.Value != target.ServiceId) return false;
            if (recursoId.HasValue && r.UserId.HasValue && r.UserId.Value != recursoId.Value) return false;
            return true;
        });

        // Filtros explícitos
        foreach (var f in source.Filters ?? new List<FilterSpec>())
        {
            var filter = f;
            rows = rows.Where(r => ApplyFilter(r, filter));
        }

        return rows.ToList();
    }

    private static bool ApplyFilter(RowAdapter row, FilterSpec spec)
    {
        var actual = row.GetField(spec.Field);
        if (actual is null) return false;
        if (spec.Value is null) return spec.Op == "Eq" ? false : true;
        var actualD = TryDecimal(actual);
        var expectedD = TryDecimal(spec.Value);
        return spec.Op switch
        {
            "Eq" => Equal(actual, spec.Value),
            "Neq" => !Equal(actual, spec.Value),
            "Gt" => actualD > expectedD,
            "Gte" => actualD >= expectedD,
            "Lt" => actualD < expectedD,
            "Lte" => actualD <= expectedD,
            "In" => spec.Value is System.Collections.IEnumerable list && list.Cast<object>().Any(v => Equal(actual, v)),
            _ => false
        };
    }

    private static bool Equal(object a, object b)
    {
        if (a is null || b is null) return a == b;
        if (a is System.Text.Json.JsonElement ae) a = JsonElementToObject(ae);
        if (b is System.Text.Json.JsonElement be) b = JsonElementToObject(be);
        if (a.GetType() != b.GetType())
        {
            try { return Convert.ToDecimal(a) == Convert.ToDecimal(b); }
            catch { return a.ToString() == b.ToString(); }
        }
        return a.Equals(b);
    }

    private static decimal TryDecimal(object o)
    {
        if (o is System.Text.Json.JsonElement je) o = JsonElementToObject(je);
        try { return Convert.ToDecimal(o); } catch { return 0m; }
    }

    private static object JsonElementToObject(System.Text.Json.JsonElement el) => el.ValueKind switch
    {
        System.Text.Json.JsonValueKind.Number => el.TryGetInt64(out var l) ? (object)l : el.GetDouble(),
        System.Text.Json.JsonValueKind.String => el.GetString() ?? string.Empty,
        System.Text.Json.JsonValueKind.True => true,
        System.Text.Json.JsonValueKind.False => false,
        _ => el.ToString()
    };

    private static string EntityToSistema(string entity) => entity switch
    {
        "GastosPayHawk" => "PayHawk",
        "VisitasCelero" => "Celero",
        "HorasBizneo" => "Bizneo",
        "HorasIntratime" => "Intratime",
        "TarifasServicio" => "Tarifas",
        "TarifasProyecto" => "Tarifas",
        "VisitasSgpv" => "SGPV",
        _ => "Desconocido"
    };

    public string DetectSistemaOrigen()
    {
        if (SistemasUsados.Count == 0) return "Ninguno";
        if (SistemasUsados.Count == 1) return SistemasUsados.First();
        return "Mixto";
    }
}

public class RowAdapter
{
    public DateOnly? Fecha { get; set; }
    public int? UserId { get; set; }
    public int? ServiceId { get; set; }
    public decimal? Importe { get; set; }
    public decimal? Horas { get; set; }
    public int? TipoVisita { get; set; }
    public int? PuntoMontado { get; set; }
    public string? Categoria { get; set; }
    public string? Nombre { get; set; }
    public DateTime? Entrada { get; set; }
    public DateTime? Salida { get; set; }

    public decimal GetDecimal(string field) => field switch
    {
        "Importe" => Importe ?? 0,
        "Horas" => Horas ?? 0,
        "TipoVisita" => TipoVisita ?? 0,
        "PuntoMontado" => PuntoMontado ?? 0,
        _ => 0
    };

    public object? GetField(string field) => field switch
    {
        "Fecha" => Fecha,
        "UserId" => UserId,
        "ServiceId" => ServiceId,
        // Alias retro-compat para fórmulas almacenadas que filtraban por ProjectId/ActionId
        "ProjectId" => ServiceId,
        "ActionId" => ServiceId,
        "Importe" => Importe,
        "Horas" => Horas,
        "TipoVisita" => TipoVisita,
        "PuntoMontado" => PuntoMontado,
        "Categoria" => Categoria,
        "Nombre" => Nombre,
        "Entrada" => Entrada,
        _ => null
    };

    public static RowAdapter FromGasto(StagingPayHawkGasto g) => new()
    {
        Fecha = g.Fecha, UserId = g.UserId, ServiceId = g.ServiceId, Importe = g.Importe, Categoria = g.Categoria
    };
    public static RowAdapter FromVisita(StagingCeleroVisita v) => new()
    {
        Fecha = v.Fecha, UserId = v.UserId, ServiceId = v.ServiceId, TipoVisita = null, PuntoMontado = null
    };
    public static RowAdapter FromHora(StagingBizneoAbsence h) => new()
    {
        Fecha = h.Fecha, UserId = h.UserId, ServiceId = h.ServiceId, Horas = h.Horas
    };
    public static RowAdapter FromFichaje(StagingIntratimeFichaje f) => new()
    {
        UserId = f.UserId, Entrada = f.Entrada, Salida = f.Salida, Horas = f.HorasCalculadas
    };
    public static RowAdapter FromTarifa(TarifaServicio t) => new()
    {
        Nombre = t.Nombre, Importe = t.Valor, Fecha = t.FechaDesde, ServiceId = t.ServiceId
    };
    public static RowAdapter FromSgpvVisita(StagingSgpvVisita s) => new()
    {
        Fecha = s.Fecha, UserId = s.UserId, ServiceId = s.ServiceId
    };
}
