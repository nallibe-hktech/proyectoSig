namespace SIG.Application.Alerts;

/// <summary>
/// Códigos de alertas y validaciones para cierres.
/// Utiliza constantes en lugar de enum para permitir extensibilidad futura.
/// </summary>
public static class AlertaCodigos
{
    // BLOQUEANTES — impiden el cierre hasta resolverse
    public const string NifSinMapeo = "NIF_SIN_MAPEO";
    public const string ContratosDuplicados = "CONTRATOS_DUPLICADOS";
    public const string CamposClave = "CAMPOS_CLAVE_INVALIDOS";
    public const string ActividadSinContrato = "ACTIVIDAD_SIN_CONTRATO";
    public const string CecoNoMaestro = "CECO_NO_MAESTRO";
    public const string NifBizneoSinContrato = "NIF_BIZNEO_SIN_CONTRATO";
    public const string NifIntratimeSinContrato = "NIF_INTRATIME_SIN_CONTRATO";

    // ADVERTENCIAS — requieren confirmación para permitir el cierre
    public const string ContratoSinActividad = "CONTRATO_SIN_ACTIVIDAD";
    public const string PagoPorKmExcesivo = "PAGO_KM_EXCESIVO";
    public const string GastoNegativo = "GASTO_NEGATIVO";
    public const string PagoInferiorContrato = "PAGO_INFERIOR_CONTRATO";
    public const string VisitaSinRecurso = "VISITA_SIN_RECURSO";
    public const string GastoSinProyecto = "GASTO_SIN_PROYECTO";
}
