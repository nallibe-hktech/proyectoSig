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
    public List<StagingTravelPerkLinea> ViajesTravelPerk { get; set; } = new();
    public List<StagingA3InnuvaSalary> SalariosA3 { get; set; } = new();
    // Logística Galán
    public List<StagingGalanEntrada> GalanEntradas { get; set; } = new();
    public List<StagingGalanSalida> GalanSalidas { get; set; } = new();
    public List<StagingGalanStock> GalanStock { get; set; } = new();
    // Logística Mediapost
    public List<StagingMediapostPedido> MediapostPedidos { get; set; } = new();
    public List<StagingMediapostRecepcion> MediapostRecepciones { get; set; } = new();
    // Datos cross-service: horas de TODOS los servicios del empleado (para "salario ÷ horas dedicadas")
    public Dictionary<string, List<RowAdapter>> CrossServiceRows { get; } = new();
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
            "ViajesTravelPerk" => ViajesTravelPerk.Select(t => RowAdapter.FromTravelPerkLinea(t)),
            "SalariosA3" => SalariosA3.Select(s => RowAdapter.FromSalarioA3(s)),
            "EntradasGalan" => GalanEntradas.Select(e => RowAdapter.FromGalanEntrada(e)),
            "SalidasGalan" => GalanSalidas.Select(s => RowAdapter.FromGalanSalida(s)),
            "StockGalan" => GalanStock.Select(s => RowAdapter.FromGalanStock(s)),
            "PedidosMediapost" => MediapostPedidos.Select(p => RowAdapter.FromMediapostPedido(p)),
            "RecepcionesMediapost" => MediapostRecepciones.Select(r => RowAdapter.FromMediapostRecepcion(r)),
            _ => throw new FormulaInvalidException($"Entidad desconocida: {source.Entity}")
        };

        // Filtros implícitos: período + projectId (si campo existe) + userId opcional
        var desde = target.Period.FechaInicio;
        var hasta = target.Period.FechaFin;

        var rows = baseRows.Where(r =>
        {
            // SalariosA3 requiere fecha válida; si no tiene, se excluye
            if (source.Entity == "SalariosA3" && !r.Fecha.HasValue) return false;
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
        // Flags de excepción: si un lado es booleano, comparamos de forma tolerante (true/1/"sí"/"true").
        if (a is bool || b is bool) return CoerceBool(a) == CoerceBool(b);
        if (a.GetType() != b.GetType())
        {
            try { return Convert.ToDecimal(a) == Convert.ToDecimal(b); }
            catch { return string.Equals(a.ToString(), b.ToString(), StringComparison.OrdinalIgnoreCase); }
        }
        if (a is string sa && b is string sb) return string.Equals(sa, sb, StringComparison.OrdinalIgnoreCase);
        return a.Equals(b);
    }

    private static bool CoerceBool(object o) => o switch
    {
        bool b => b,
        string s => s.Equals("true", StringComparison.OrdinalIgnoreCase)
                 || s.Equals("sí", StringComparison.OrdinalIgnoreCase)
                 || s.Equals("si", StringComparison.OrdinalIgnoreCase)
                 // Las respuestas booleanas de Celero llegan en inglés ("Yes"/"No"); sin esto un "Yes" se evaluaría como false.
                 || s.Equals("yes", StringComparison.OrdinalIgnoreCase)
                 || s == "1",
        _ => TryDecimal(o) != 0m
    };

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
        "ViajesTravelPerk" => "TravelPerk",
        "SalariosA3" => "A3Innuva",
        "EntradasGalan" => "Galan",
        "SalidasGalan" => "Galan",
        "StockGalan" => "Galan",
        "PedidosMediapost" => "Mediapost",
        "RecepcionesMediapost" => "Mediapost",
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
    public decimal? Km { get; set; }
    public int? TipoVisita { get; set; }
    public int? PuntoMontado { get; set; }
    public string? Zona { get; set; }
    public string? Categoria { get; set; }
    public string? Nombre { get; set; }
    // Flags de excepción del Excel (columna "Excepciones_Modelo"): estado de la visita,
    // nº de visita (1ª/2ª/3ª), nocturnidad y pernocta. Se extraen del PayloadJson de Celero.
    public string? Estado { get; set; }      // "ok" | "fallida" | "cancelada" | "anulada" ...
    public int? NumeroVisita { get; set; }   // 1 = primera, 2 = segunda, 3 = tercera ...
    public bool Nocturnidad { get; set; }
    public bool Pernocta { get; set; }
    // Campos de muebles para facturación (no todas las visitas los traen)
    public string? Muebles { get; set; }      // tipo de mueble: "Expositor", "Gondola", "Cabecera", etc.
    public string? TipoMueble { get; set; }   // subtipo o categoría de mueble
    public int? CantidadMuebles { get; set; } // cantidad de unidades instaladas
    public DateTime? Entrada { get; set; }
    public DateTime? Salida { get; set; }
    // Atributos arbitrarios extraídos del PayloadJson (respuestas Celero/idQuestion, etc.) para filtrar/segmentar.
    public Dictionary<string, object?> Extra { get; } = new(StringComparer.OrdinalIgnoreCase);

    public decimal GetDecimal(string field) => field switch
    {
        "Importe" => Importe ?? 0,
        "Horas" => Horas ?? 0,
        "Km" => Km ?? 0,
        "TipoVisita" => TipoVisita ?? 0,
        "PuntoMontado" => PuntoMontado ?? 0,
        "NumeroVisita" => NumeroVisita ?? 0,
        "Nocturnidad" => Nocturnidad ? 1 : 0,
        "Pernocta" => Pernocta ? 1 : 0,
        "CantidadMuebles" => CantidadMuebles ?? 0,
        _ => Extra.TryGetValue(field, out var v) ? ToDecimal(v) : 0
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
        "Km" => Km,
        "TipoVisita" => TipoVisita,
        "PuntoMontado" => PuntoMontado,
        "Zona" => Zona,
        "Categoria" => Categoria,
        "Nombre" => Nombre,
        "Entrada" => Entrada,
        "Estado" => Estado,
        "NumeroVisita" => NumeroVisita,
        "Nocturnidad" => Nocturnidad,
        "Pernocta" => Pernocta,
        "Muebles" => Muebles,
        "TipoMueble" => TipoMueble,
        "CantidadMuebles" => CantidadMuebles,
        _ => Extra.TryGetValue(field, out var v) ? v : null
    };

    private static decimal ToDecimal(object? o)
    {
        if (o is null) return 0m;
        try { return Convert.ToDecimal(o); } catch { return 0m; }
    }

    private static bool ToBool(object? o) => o switch
    {
        null => false,
        bool b => b,
        string s => s.Equals("true", StringComparison.OrdinalIgnoreCase)
                 || s.Equals("sí", StringComparison.OrdinalIgnoreCase)
                 || s.Equals("si", StringComparison.OrdinalIgnoreCase)
                 // Respuestas booleanas de Celero en inglés ("Yes"/"No").
                 || s.Equals("yes", StringComparison.OrdinalIgnoreCase)
                 || s == "1",
        _ => ToDecimal(o) != 0m
    };

    // Vuelca las propiedades escalares de un PayloadJson en Extra y mapea las claves conocidas (case-insensitive).
    private void PopulateFromPayload(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson)) return;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(payloadJson);
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object) return;
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                object? val = prop.Value.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.Number => prop.Value.TryGetInt64(out var l) ? l : prop.Value.GetDouble(),
                    System.Text.Json.JsonValueKind.String => prop.Value.GetString(),
                    System.Text.Json.JsonValueKind.True => true,
                    System.Text.Json.JsonValueKind.False => false,
                    _ => null
                };
                Extra[prop.Name] = val;
                switch (prop.Name.ToLowerInvariant())
                {
                    case "tipovisita": TipoVisita = (int)ToDecimal(val); break;
                    case "puntomontado": PuntoMontado = (int)ToDecimal(val); break;
                    case "zona": Zona = val?.ToString(); break;
                    case "horas": Horas = ToDecimal(val); break;
                    case "km":
                    case "kilometros": Km = ToDecimal(val); break;
                    case "importe": Importe = ToDecimal(val); break;
                    case "categoria": Categoria = val?.ToString(); break;
                    case "estado": Estado = val?.ToString(); break;
                    case "numerovisita":
                    case "numvisita":
                    case "nvisita": NumeroVisita = (int)ToDecimal(val); break;
                    case "nocturnidad":
                    case "nocturna": Nocturnidad = ToBool(val); break;
                    case "pernocta": Pernocta = ToBool(val); break;
                    // Campos de muebles para facturación
                    case "muebles":
                    case "mueble": Muebles = val?.ToString(); break;
                    case "tipomueble":
                    case "tipo_mueble":
                    case "tipomuebles": TipoMueble = val?.ToString(); break;
                    case "cantidadmuebles":
                    case "cantidad_muebles":
                    case "numuebles":
                    case "n_muebles": CantidadMuebles = (int)ToDecimal(val); break;
                }
            }
        }
        catch { /* payload no parseable: se ignora, los campos quedan en null */ }
    }

    public static RowAdapter FromGasto(StagingPayHawkGasto g) => new()
    {
        Fecha = g.Fecha, UserId = g.UserId, ServiceId = g.ServiceId, Importe = g.Importe, Categoria = g.Categoria
    };
    public static RowAdapter FromVisita(StagingCeleroVisita v)
    {
        var r = new RowAdapter { Fecha = v.Fecha, UserId = v.UserId, ServiceId = v.ServiceId };
        r.PopulateFromPayload(v.PayloadJson);
        return r;
    }
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
        Fecha = s.Fecha, UserId = s.UserId, ServiceId = s.ServiceId, Horas = s.HorasDuracion
    };
    public static RowAdapter FromTravelPerkLinea(StagingTravelPerkLinea t)
    {
        var r = new RowAdapter { Fecha = t.FechaGasto, ServiceId = t.ServiceId, Importe = t.CosteSinIVA, Categoria = t.Service };
        r.Extra["Service"] = t.Service;
        r.Extra["Ceco"] = t.Ceco;
        r.Extra["CostObject"] = t.CostObject;
        return r;
    }
    public static RowAdapter FromSalarioA3(StagingA3InnuvaSalary s) => new()
    {
        UserId = s.UserId,
        Importe = s.ImporteBruto,
        Fecha = s.FechaInicio != default ? DateOnly.FromDateTime(s.FechaInicio) : null,
        Entrada = s.FechaInicio,
        Salida = s.FechaFin,
    };
    public static RowAdapter FromGalanEntrada(StagingGalanEntrada e)
    {
        var r = new RowAdapter
        {
            Fecha = DateOnly.FromDateTime(e.Fecha),
            Nombre = e.Descripcion,
            Categoria = e.CodigoFamilia,
        };
        r.Extra["CodigoArticulo"] = e.CodigoArticulo;
        r.Extra["CodigoDepartamento"] = e.CodigoDepartamento;
        r.Extra["CodigoFamilia"] = e.CodigoFamilia;
        r.Extra["Unidades"] = e.Unidades;
        r.Extra["Empresa"] = e.Empresa;
        r.Extra["Almacen"] = e.Almacen;
        r.Extra["Celda"] = e.Celda;
        return r;
    }
    public static RowAdapter FromGalanSalida(StagingGalanSalida s)
    {
        var r = new RowAdapter
        {
            Fecha = DateOnly.FromDateTime(s.Fecha),
            Nombre = s.Descripcion,
            Categoria = s.CodigoFamilia,
        };
        r.Extra["Albaran"] = s.Albaran;
        r.Extra["NumeroPedidoTercero"] = s.NumeroPedidoTercero;
        r.Extra["CodigoArticulo"] = s.CodigoArticulo;
        r.Extra["CodigoDepartamento"] = s.CodigoDepartamento;
        r.Extra["CodigoFamilia"] = s.CodigoFamilia;
        r.Extra["Unidades"] = s.Unidades;
        r.Extra["CodigoTransporte"] = s.CodigoTransporte;
        r.Extra["Matricula"] = s.Matricula;
        r.Extra["Destinatario"] = s.Destinatario;
        r.Extra["Almacen"] = s.Almacen;
        r.Extra["Celda"] = s.Celda;
        return r;
    }
    public static RowAdapter FromGalanStock(StagingGalanStock s)
    {
        var r = new RowAdapter
        {
            Nombre = s.Descripcion,
            Categoria = s.Familia,
            Importe = s.Stock,
        };
        r.Extra["CodigoArticulo"] = s.CodigoArticulo;
        r.Extra["CodigoDepartamento"] = s.CodigoDepartamento;
        r.Extra["CodigoFamilia"] = s.CodigoFamilia;
        r.Extra["CodigoCelda"] = s.CodigoCelda;
        r.Extra["StockB"] = s.StockB;
        r.Extra["StockA"] = s.StockA;
        r.Extra["Stock"] = s.Stock;
        r.Extra["Almacen"] = s.Almacen;
        r.Extra["Familia"] = s.Familia;
        r.Extra["SubFamilia"] = s.SubFamilia;
        return r;
    }
    public static RowAdapter FromMediapostPedido(StagingMediapostPedido p)
    {
        var r = new RowAdapter
        {
            Fecha = DateOnly.FromDateTime(p.FechaPedido),
            Nombre = p.ReferenciaPedido,
            Estado = p.Estado,
            Importe = p.Cantidad,
        };
        r.Extra["PedidoId"] = p.PedidoId;
        r.Extra["ReferenciaPedido"] = p.ReferenciaPedido;
        r.Extra["CodigoArticulo"] = p.CodigoArticulo;
        r.Extra["Cantidad"] = p.Cantidad;
        r.Extra["DestinatarioNombre"] = p.DestinatarioNombre;
        r.Extra["DireccionEntrega"] = p.DireccionEntrega;
        r.Extra["CodigoPostal"] = p.CodigoPostal;
        r.Extra["Ciudad"] = p.Ciudad;
        r.Extra["Provincia"] = p.Provincia;
        return r;
    }
    public static RowAdapter FromMediapostRecepcion(StagingMediapostRecepcion recep)
    {
        var r = new RowAdapter
        {
            Fecha = DateOnly.FromDateTime(recep.FechaRecepcion),
            Nombre = recep.ReferenciaRecepcion,
            Estado = recep.Estado,
            Importe = recep.Cantidad,
        };
        r.Extra["RecepcionId"] = recep.RecepcionId;
        r.Extra["ReferenciaRecepcion"] = recep.ReferenciaRecepcion;
        r.Extra["CodigoArticulo"] = recep.CodigoArticulo;
        r.Extra["Cantidad"] = recep.Cantidad;
        r.Extra["CantidadDanada"] = recep.CantidadDañada;
        r.Extra["Almacen"] = recep.Almacen;
        r.Extra["Observaciones"] = recep.Observaciones;
        return r;
    }
}
