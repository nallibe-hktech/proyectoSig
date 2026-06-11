using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SIG_es.Models.SGPV;

namespace SIG_es.Services
{
    /// <summary>
    /// Servicio para acceder a los datos de productos de SGPV
    /// </summary>
    public class SgpvService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _username;
        private readonly string _password;

        public SgpvService(string baseUrl, string username, string password)
        {
            _baseUrl = baseUrl;
            _username = username;
            _password = password;
            _httpClient = new HttpClient();
            
            // Configurar timeout largo porque SGPV es lento
            _httpClient.Timeout = TimeSpan.FromSeconds(120);
        }

        /// <summary>
        /// Obtiene todos los productos de SGPV
        /// </summary>
        public async Task<List<SgpvProducto>> GetProductosAsync()
        {
            try
            {
                var url = $"{_baseUrl}?user={_username}&password={_password}&format=json";
                
                // Crear credencial para autenticación HTTP Basic
                var auth = Convert.ToBase64String(
                    Encoding.ASCII.GetBytes($"{_username}:{_password}")
                );
                
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

                Console.WriteLine($"[SGPV] Obteniendo productos desde {_baseUrl}...");
                var start = DateTime.Now;
                
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var data = JsonSerializer.Deserialize<SgpvApiResponse>(content, options);

                    var elapsed = (DateTime.Now - start).TotalSeconds;
                    var count = data?.Export?.Referencias?.Count ?? 0;
                    
                    Console.WriteLine($"[SGPV] OK - {count} productos obtenidos en {elapsed:F2}s");
                    
                    return data?.Export?.Referencias ?? new List<SgpvProducto>();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[SGPV] Error {response.StatusCode}: {error}");
                    throw new Exception($"Error SGPV: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[SGPV] Error de conectividad: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SGPV] Error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtiene productos filtrando por cliente
        /// </summary>
        public async Task<List<SgpvProducto>> GetProductosPorClienteAsync(string cliente)
        {
            try
            {
                var todos = await GetProductosAsync();
                var filtrados = todos.FindAll(p => p.Cliente.Equals(cliente, StringComparison.OrdinalIgnoreCase));
                
                Console.WriteLine($"[SGPV] {filtrados.Count} productos encontrados para cliente '{cliente}'");
                
                return filtrados;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SGPV] Error al filtrar por cliente: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtiene productos filtrando por categoria
        /// </summary>
        public async Task<List<SgpvProducto>> GetProductosPorCategoriaAsync(string categoria)
        {
            try
            {
                var todos = await GetProductosAsync();
                var filtrados = todos.FindAll(p => p.Categoria.Equals(categoria, StringComparison.OrdinalIgnoreCase));
                
                Console.WriteLine($"[SGPV] {filtrados.Count} productos encontrados en categoria '{categoria}'");
                
                return filtrados;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SGPV] Error al filtrar por categoria: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtiene solo productos activos
        /// </summary>
        public async Task<List<SgpvProducto>> GetProductosActivosAsync()
        {
            try
            {
                var todos = await GetProductosAsync();
                var activos = todos.FindAll(p => p.Activo == "1");
                
                Console.WriteLine($"[SGPV] {activos.Count} productos activos");
                
                return activos;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SGPV] Error al obtener activos: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Busca productos por codigo de referencia
        /// </summary>
        public async Task<SgpvProducto> BuscarPorCodigoAsync(string codigo)
        {
            try
            {
                var todos = await GetProductosAsync();
                var producto = todos.Find(p => p.CodigoReferencia == codigo);
                
                if (producto != null)
                {
                    Console.WriteLine($"[SGPV] Producto encontrado: {producto.Referencia}");
                }
                else
                {
                    Console.WriteLine($"[SGPV] No se encontró producto con codigo {codigo}");
                }
                
                return producto;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SGPV] Error al buscar: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Obtiene estadísticas de productos
        /// </summary>
        public async Task<Dictionary<string, int>> GetEstadisticasAsync()
        {
            try
            {
                var todos = await GetProductosAsync();
                var stats = new Dictionary<string, int>
                {
                    { "Total", todos.Count },
                    { "Activos", todos.FindAll(p => p.Activo == "1").Count },
                    { "Inactivos", todos.FindAll(p => p.Activo == "0").Count }
                };
                
                Console.WriteLine($"[SGPV] Estadísticas: {stats["Total"]} total, {stats["Activos"]} activos");
                
                return stats;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SGPV] Error al calcular estadísticas: {ex.Message}");
                throw;
            }
        }
    }
}
