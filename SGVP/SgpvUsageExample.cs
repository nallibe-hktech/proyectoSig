using System;
using System.Threading.Tasks;
using SIG_es.Services;

namespace SIG_es.Examples
{
    /// <summary>
    /// Ejemplos de uso del servicio SGPV
    /// </summary>
    public class SgpvUsageExample
    {
        public static async Task Main()
        {
            // Configuración (obtener de appsettings.json en producción)
            var baseUrl = "https://sig.sgpv.es/export/ExportData.php";
            var username = "sig";
            var password = "hola";

            var sgpvService = new SgpvService(baseUrl, username, password);

            try
            {
                // ============================================
                // EJEMPLO 1: Obtener todos los productos
                // ============================================
                Console.WriteLine("========== EJEMPLO 1: Obtener todos los productos ==========\n");
                var todos = await sgpvService.GetProductosAsync();
                Console.WriteLine($"Total de productos: {todos.Count}\n");

                // Mostrar primeros 5
                Console.WriteLine("Primeros 5 productos:");
                for (int i = 0; i < Math.Min(5, todos.Count); i++)
                {
                    var p = todos[i];
                    Console.WriteLine($"{i + 1}. {p.Referencia} - {p.Marca} - €{p.PVPRecomendado}");
                }

                // ============================================
                // EJEMPLO 2: Obtener productos por cliente
                // ============================================
                Console.WriteLine("\n\n========== EJEMPLO 2: Productos por cliente ==========\n");
                var productosSprintex = await sgpvService.GetProductosPorClienteAsync("SPONTEX");
                Console.WriteLine($"Productos de SPONTEX: {productosSprintex.Count}");

                // ============================================
                // EJEMPLO 3: Obtener solo productos activos
                // ============================================
                Console.WriteLine("\n========== EJEMPLO 3: Productos activos ==========\n");
                var activos = await sgpvService.GetProductosActivosAsync();
                Console.WriteLine($"Productos activos: {activos.Count}");

                // ============================================
                // EJEMPLO 4: Buscar por código de referencia
                // ============================================
                Console.WriteLine("\n========== EJEMPLO 4: Buscar por código ==========\n");
                var producto = await sgpvService.BuscarPorCodigoAsync("19700053");
                if (producto != null)
                {
                    Console.WriteLine($"Encontrado: {producto.Referencia}");
                    Console.WriteLine($"Cliente: {producto.Cliente}");
                    Console.WriteLine($"Categoría: {producto.Categoria} > {producto.Subcategoria}");
                    Console.WriteLine($"EAN: {producto.EAN}");
                    Console.WriteLine($"Marca: {producto.Marca}");
                    Console.WriteLine($"PVP Recomendado: €{producto.PVPRecomendado}");
                    Console.WriteLine($"Activo: {(producto.Activo == "1" ? "Sí" : "No")}");
                }

                // ============================================
                // EJEMPLO 5: Estadísticas
                // ============================================
                Console.WriteLine("\n========== EJEMPLO 5: Estadísticas ==========\n");
                var stats = await sgpvService.GetEstadisticasAsync();
                foreach (var stat in stats)
                {
                    Console.WriteLine($"{stat.Key}: {stat.Value}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
