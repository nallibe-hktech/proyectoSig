# Adaptador SGPV para SIG-es

## 📋 Resumen

Este adaptador permite integrar los datos de productos de **SGPV** en la aplicación SIG-es.

**Estadísticas:**
- Total de productos: **997**
- Cliente principal: SPONTEX
- Datos disponibles: Código, Referencia, EAN, Marca, PVP, Categoría, etc.
- Autenticación: HTTP Basic (user: sig, password: hola)
- Timeout recomendado: **120 segundos** (servidor lento)

---

## 🚀 Instalación

### 1. Copiar archivos a tu proyecto

```
C:\Projects\workspaces\SIG-es\
├── Models\
│   └── SGPV\
│       └── SgpvProducto.cs          ← Copiar aquí
├── Services\
│   └── SgpvService.cs               ← Copiar aquí
└── appsettings.json                 ← Actualizar
```

### 2. Actualizar appsettings.json

Agregar la sección SGPV:

```json
{
  "SGPV": {
    "BaseUrl": "https://sig.sgpv.es/export/ExportData.php",
    "Username": "sig",
    "Password": "hola",
    "TimeoutSeconds": 120,
    "Enabled": true
  }
}
```

### 3. Registrar servicio en Program.cs (si usas .NET 6+)

```csharp
using SIG_es.Services;

// En Program.cs o Startup.cs
var sgpvConfig = configuration.GetSection("SGPV");

services.AddScoped(_ => new SgpvService(
    sgpvConfig["BaseUrl"],
    sgpvConfig["Username"],
    sgpvConfig["Password"]
));
```

---

## 💻 Uso

### Opción 1: Inyección de dependencias

```csharp
public class ProductController
{
    private readonly SgpvService _sgpvService;

    public ProductController(SgpvService sgpvService)
    {
        _sgpvService = sgpvService;
    }

    [HttpGet("productos")]
    public async Task<IActionResult> GetProductos()
    {
        var productos = await _sgpvService.GetProductosAsync();
        return Ok(productos);
    }
}
```

### Opción 2: Uso directo

```csharp
var sgpvService = new SgpvService(
    "https://sig.sgpv.es/export/ExportData.php",
    "sig",
    "hola"
);

var productos = await sgpvService.GetProductosAsync();
```

---

## 📚 Métodos disponibles

### GetProductosAsync()
Obtiene **todos los 997 productos** de SGPV.

```csharp
var productos = await sgpvService.GetProductosAsync();
Console.WriteLine($"Total: {productos.Count}");
```

### GetProductosPorClienteAsync(string cliente)
Filtra productos por cliente.

```csharp
var productosSprintex = await sgpvService.GetProductosPorClienteAsync("SPONTEX");
```

### GetProductosPorCategoriaAsync(string categoria)
Filtra productos por categoría.

```csharp
var bayetas = await sgpvService.GetProductosPorCategoriaAsync("BAYETAS");
```

### GetProductosActivosAsync()
Obtiene solo los productos activos.

```csharp
var activos = await sgpvService.GetProductosActivosAsync();
```

### BuscarPorCodigoAsync(string codigo)
Busca un producto específico por código de referencia.

```csharp
var producto = await sgpvService.BuscarPorCodigoAsync("19700053");
if (producto != null)
{
    Console.WriteLine($"{producto.Referencia} - €{producto.PVPRecomendado}");
}
```

### GetEstadisticasAsync()
Obtiene estadísticas generales.

```csharp
var stats = await sgpvService.GetEstadisticasAsync();
Console.WriteLine($"Total: {stats["Total"]}");
Console.WriteLine($"Activos: {stats["Activos"]}");
```

---

## 🏗️ Estructura de datos

### Modelo SgpvProducto

```csharp
public class SgpvProducto
{
    public string IdProducto { get; set; }          // "1"
    public string IdCliente { get; set; }           // "1"
    public string Cliente { get; set; }             // "SPONTEX"
    public string Categoria { get; set; }           // "BAYETAS"
    public string Subcategoria { get; set; }        // "MICROFIBRA"
    public string CodigoReferencia { get; set; }    // "19700053"
    public string Referencia { get; set; }          // "BAYETA MAGIC EFFECT 3UDS"
    public string EAN { get; set; }                 // "3384121970532"
    public string Marca { get; set; }               // "SPONTEX"
    public string PVPRecomendado { get; set; }      // "4.82"
    public string Competencia { get; set; }         // "No"
    public string Activo { get; set; }              // "1" o "0"
}
```

---

## ⚠️ Consideraciones importantes

### 1. **Timeout largo**
SGPV es un servidor lento. Se configura con timeout de 120 segundos.
- En producción, considera cachear los datos
- O ejecutar la descarga en background jobs

### 2. **Autenticación HTTP Basic**
- Usuario: `sig`
- Contraseña: `hola`
- Se envía codificado en Base64 en el header Authorization

### 3. **Volumen de datos**
- 997 productos = ~36.5 MB
- Primera llamada tardará ~100 segundos
- Considera cachear en Redis o base de datos

### 4. **Errores comunes**

**Error: "This page is protected by HTTP Authentication"**
- Asegúrate de que envías las credenciales en el header Authorization

**Error: Timeout después de 30 segundos**
- Aumenta el timeout en appsettings.json a 120 segundos

**Error: SSL Certificate Validation Failed**
- Si usas certificado autofirmado, puedes agregar en Startup:
```csharp
handler.ServerCertificateCustomValidationCallback = 
    (message, cert, chain, errors) => true;
```

---

## 🎯 Próximos pasos

### Opción 1: Cachear datos en BD
Guardar los productos en la base de datos local (PostgreSQL) y actualizarlos cada X horas.

```csharp
public async Task CacheProductosAsync()
{
    var productos = await sgpvService.GetProductosAsync();
    // Guardar en BD...
    _dbContext.Productos.AddRange(productos);
    await _dbContext.SaveChangesAsync();
}
```

### Opción 2: Background Job
Usar Hangfire o similar para actualizar datos cada noche.

```csharp
BackgroundJob.Schedule(
    () => CacheProductosAsync(),
    TimeSpan.FromDays(1)
);
```

### Opción 3: API Endpoint
Exponer un endpoint para obtener productos.

```csharp
[HttpGet("api/sgpv/productos")]
public async Task<IActionResult> GetSgpvProductos()
{
    var productos = await _sgpvService.GetProductosAsync();
    return Ok(productos);
}
```

---

## 📞 Soporte

**Datos de SGPV:**
- URL: https://sig.sgpv.es/export/ExportData.php
- Usuario: sig
- Contraseña: hola
- Formato: JSON
- Total registros: 997 productos

---

**Fecha:** Junio 2026 | **Proyecto:** SIG-es
