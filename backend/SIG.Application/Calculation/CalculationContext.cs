using SIG.Application.Calculation.Nodes;
using SIG.Domain.Entities;
using SIG.Domain.Entities.Staging;
using SIG.Domain.Exceptions;

namespace SIG.Application.Calculation;

public class CalculationContext
{
    public List<StagingCeleroVisita> Visitas { get; set; } = new();
    public List<StagingBizneoHora> HorasBizneo { get; set; } = new();
    public List<StagingIntratimeFichaje> Fichajes { get; set; } = new();
    public List<StagingPayHawkGasto> Gastos { get; set; } = new();
    public List<Variable> Variables { get; set; } = new();
    public HashSet<string> SistemasUsados { get; } = new();
    public Dictionary<string, object> UsedInputs { get; } = new();

    public List<RowAdapter> FilteredRows(SourceNode source, Closure closure, int? recursoId)
    {
        SistemasUsados.Add(EntityToSistema(source.Entity));

        IEnumerable<RowAdapter> baseRows = source.Entity switch
        {
            "GastosPayHawk" => Gastos.Select(g => RowAdapter.FromGasto(g)),
            "VisitasCelero" => Visitas.Select(v => RowAdapter.FromVisita(v)),
            "HorasBizneo" => HorasBizneo.Select(h => RowAdapter.FromHora(h)),
            "HorasIntratime" => Fichajes.Select(f => RowAdapter.FromFichaje(f)),
            _ => throw new FormulaInvalidException($"Entidad desconocida: {source.Entity}")
        };

        // Filtros implícitos: período + projectId (si campo existe) + userId opcional
        var desde = closure.Period.FechaInicio;
        var hasta = closure.Period.FechaFin;

        var rows = baseRows.Where(r =>
        {
            if (r.Fecha.HasValue && (r.Fecha.Value < desde || r.Fecha.Value > hasta)) return false;
            if (r.ProjectId.HasValue && r.ProjectId.Value != closure.ProjectId) return false;
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
    public int? ProjectId { get; set; }
    public int? ActionId { get; set; }
    public decimal? Importe { get; set; }
    public decimal? Horas { get; set; }
    public int? TipoVisita { get; set; }
    public int? PuntoMontado { get; set; }
    public string? Categoria { get; set; }
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
        "ProjectId" => ProjectId,
        "ActionId" => ActionId,
        "Importe" => Importe,
        "Horas" => Horas,
        "TipoVisita" => TipoVisita,
        "PuntoMontado" => PuntoMontado,
        "Categoria" => Categoria,
        "Entrada" => Entrada,
        _ => null
    };

    public static RowAdapter FromGasto(StagingPayHawkGasto g) => new()
    {
        Fecha = g.Fecha, UserId = g.UserId, ProjectId = g.ProjectId, Importe = g.Importe, Categoria = g.Categoria
    };
    public static RowAdapter FromVisita(StagingCeleroVisita v) => new()
    {
        Fecha = v.Fecha, UserId = v.UserId, ProjectId = v.ProjectId, ActionId = v.ActionId, TipoVisita = v.TipoVisita, PuntoMontado = v.PuntoMontado
    };
    public static RowAdapter FromHora(StagingBizneoHora h) => new()
    {
        Fecha = h.Fecha, UserId = h.UserId, ProjectId = h.ProjectId, Horas = h.Horas
    };
    public static RowAdapter FromFichaje(StagingIntratimeFichaje f) => new()
    {
        UserId = f.UserId, Entrada = f.Entrada, Salida = f.Salida
    };
}
