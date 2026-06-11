using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SIG_es.Models.SGPV
{
    /// <summary>
    /// Modelo para un producto de SGPV
    /// </summary>
    public class SgpvProducto
    {
        [JsonPropertyName("idProducto")]
        public string IdProducto { get; set; }

        [JsonPropertyName("idCliente")]
        public string IdCliente { get; set; }

        [JsonPropertyName("Cliente")]
        public string Cliente { get; set; }

        [JsonPropertyName("Categoria")]
        public string Categoria { get; set; }

        [JsonPropertyName("Subcategoria")]
        public string Subcategoria { get; set; }

        [JsonPropertyName("CodigoReferencia")]
        public string CodigoReferencia { get; set; }

        [JsonPropertyName("Referencia")]
        public string Referencia { get; set; }

        [JsonPropertyName("EAN")]
        public string EAN { get; set; }

        [JsonPropertyName("Marca")]
        public string Marca { get; set; }

        [JsonPropertyName("PVPRecomendado")]
        public string PVPRecomendado { get; set; }

        [JsonPropertyName("Competencia")]
        public string Competencia { get; set; }

        [JsonPropertyName("activo")]
        public string Activo { get; set; }
    }

    /// <summary>
    /// Modelo para la respuesta de SGPV
    /// </summary>
    public class SgpvExport
    {
        [JsonPropertyName("ET_Referencias")]
        public List<SgpvProducto> Referencias { get; set; } = new List<SgpvProducto>();
    }

    /// <summary>
    /// Wrapper de la respuesta API
    /// </summary>
    public class SgpvApiResponse
    {
        [JsonPropertyName("export")]
        public SgpvExport Export { get; set; }
    }
}
