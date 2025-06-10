# 📦 Interceptor.AOP

[![NuGet](https://img.shields.io/nuget/v/Interceptor.AOP.svg?style=flat-square)](https://www.nuget.org/packages/Interceptor.AOP/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Interceptor.AOP.svg?style=flat-square)](https://www.nuget.org/packages/Interceptor.AOP/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](https://opensource.org/licenses/MIT)
[![GitHub stars](https://img.shields.io/github/stars/Juliocbm/Interceptor.AOP?style=flat-square)](https://github.com/Juliocbm/Interceptor.AOP/stargazers)


## Descripción general
Una librería ligera y extensible para aplicar **Aspect-Oriented Programming (AOP)** en aplicaciones .NET, usando interceptores automáticos basados en `DispatchProxy`.

Ideal para centralizar lógica transversal como **reintentos, validaciones, cacheo, auditoría, y manejo de errores** sin contaminar la lógica de negocio.


- Contiene toda la lógica de interceptación (ExceptionInterceptor<T>)
- Define los atributos como [Audit], [Retry], [Fallback], etc.
- Se puede usar en cualquier tipo de aplicación .NET: consola, worker, API, etc.
- Requiere que vos crees el proxy a mano con ProxyFactory.Create<>().
🔧 Ideal cuando querés máximo control o no estás usando ASP.NET Core con DI tradicional.
## 🎯 Propósito

### Centralizar y estandarizar:

- 🛡️ Manejo de excepciones con contexto
- 🔁 Reintentos y circuit breaker con Polly
- 🧪 Validaciones automáticas con DataAnnotations
- ⏱️ Medición de tiempo de ejecución
- 🔐 Cacheo en memoria por método
- 📝 Auditoría flexible: entrada, salida y errores


## 🚀 Características

✔️ Soporte para métodos síncronos y asincrónicos (`Task`, `Task<T>`)  
✔️ Decoración por atributos: fácil de aplicar, simple y expresiva, sin modificar código existente  
✔️ Interceptores desacoplados y extensibles
✔️ Soporte para ILogger<T>, IMemoryCache y IOptions<T>
✔️ Ideal para microservicios, APIs, backend robusto
✔️ Integración lista para DI con [`Interceptor.AOP.AspNetCore`](https://www.nuget.org/packages/Interceptor.AOP.AspNetCore/)


## 📥 Instalación

### Desde NuGet:
```bash
  dotnet add package Interceptor.AOP
```
## 🧩 Configuración manual (ProxyFactory)

### Ejemplo de configuración en Program.cs o Startup.cs
```csharp
services.AddSingleton(provider =>
{
    var logger = provider.GetRequiredService<ILogger<ProcesarDatosService>>();
    var memoryCache = provider.GetRequiredService<IMemoryCache>();

    // Instancia real del servicio
    var real = new ProcesarDatosService(logger);

    // Proxy con interceptor
    return ProxyFactory.Create<IProcesarDatosService>(
        real,
        logger,
        memoryCache,
        new InterceptorOptions
        {
            EnableRetries = true,
            EnableValidation = true,
            EnableTiming = true
        });
});
```
### Ejemplo de uso de decoradores
```csharp
public interface IProcesarDatosService
{
    [Audit("ProcesarArchivo", LogInput = true)]
    [Retry(3, 200)]
    [Fallback("FallbackProcesarArchivo")]
    [MeasureTime]
    [Validate]
    [Cache(60)]
    int ProcesarArchivo(int number);

    // El método fallback puede opcionalmente recibir la excepción lanzada
    int FallbackProcesarArchivo(int number, Exception? ex = null);
}
```

### Ejemplo de implementacion
```csharp
public class ProcesarDatosService : IProcesarDatosService
{
    private readonly ILogger<ProcesarDatosService> _logger;

    public ProcesarDatosService(ILogger<ProcesarDatosService> logger)
    {
        _logger = logger;
    }

    public int ProcesarArchivo(int number)
    {
        _logger.LogInformation("📊 Calculando algo con {number}", number);
        if (number < 0) throw new Exception("Valor negativo no permitido");
        return number * 10;
    }

    public int FallbackProcesarArchivo(int number, Exception? ex = null)
    {
        _logger.LogWarning("⚠️ Fallback ejecutado para {number}. Excepcion: {ex}", number, ex?.Message);
        return -1;
    }
}
```

## 📚 Atributos disponibles

|| Decorador | Description                |
|:--------| :-------- | :------------------------- |
|📝| `[Audit]` | Auditoría flexible: entrada, salida y manejo de excepciones con contexto |
|🔁| `[Retry(n, delayMs)]` | Reintenta el método n veces (opcional espera entre intentos en milisegundos y filtrado por tipos de excepción) |
|🛡️| `[Fallback("...")]` | Llama a otro método si el actual falla |
|⏱️| `[MeasureTime]` | Logea cuánto tarda la ejecución del método |
|🧪| `[Validate]` | Validaciones automáticas con DataAnnotations |
|🔐| `[Cache(n)]` | Cachea el resultado por n segundos (solo para Task<T> y métodos síncronos) |




## 📝[Audit] - Auditoría flexible

### Auditoría avanzada
El atributo [Audit] permite registrar entrada, salida y errores con granularidad y contexto funcional:

### Por defecto
```csharp
[Audit]
public Task<string> ObtenerClientesAsync() { ... }
```
➡️ Registra entrada, salida y errores automáticamente.

### Con contexto funcional
```csharp
[Audit("ProcesarArchivo")]
public void ProcesarArchivo(string path) { ... }
```

➡️ Aparece en logs como:
📥 Entrada en ProcesarArchivo
📤 Salida de ProcesarArchivo
❌ Error en ProcesarArchivo (Auditable)

### Auditoría selectiva
```csharp
[Audit(LogInput = false, LogOutput = false, LogError = true)] // Solo errores
```
```csharp
[Audit(Contexto = "Importación", LogOutput = false)] // Entrada + error
```
```csharp
[Audit(LogInput = false, LogOutput = true, LogError = false)] // Solo salida
```

## 🔁 [Retry(n, delayMs)] - Reintentos automáticos

Permite reintentar un método si lanza una excepción.

```csharp
[Retry(3, 200)]
public void ProcesarArchivo() { ... }
```

➡️ Reintenta hasta 3 veces antes de fallar. Con `delayMs` > 0 espera ese tiempo entre intentos.

También podés indicar qué tipos de excepción disparan el reintento:

```csharp
[Retry(3, ExceptionTypes = new[] { typeof(HttpRequestException) })]
public void LlamarApi() { ... }
```

En este caso solo se reintenta si se lanza `HttpRequestException`.

Ideal cuando interactuás con servicios externos, bases de datos o archivos inestables.
## 🛑 [Fallback("MetodoAlternativo")] - Método alternativo en caso de error

Define un método de respaldo si el original lanza excepción después de los reintentos.

```csharp
[Fallback("ProcesarArchivoFallback")]
public void ProcesarArchivo() { ... }

public void ProcesarArchivoFallback(Exception ex) { ... }
```
➡️ Si ProcesarArchivo() falla, se ejecuta ProcesarArchivoFallback() automáticamente y recibe la excepción lanzada.
## ⏱️ [MeasureTime] - Medición de rendimiento

Registra cuánto tarda en ejecutarse un método.

```csharp
[MeasureTime]
public void CalcularEstadisticas() { ... }
```
➡️ Logea automáticamente algo como:

```bash
⏱️ CalcularEstadisticas ejecutado en 215ms
```

## 🧪 [Validate] - Validación automática

Aplica validaciones usando [Required], [Range], etc. sobre modelos.

```csharp
[Validate]
public void GuardarCliente(ClienteModel cliente) { ... }

public class ClienteModel
{
    [Required]
    public string Nombre { get; set; }
}
```

➡️ Si cliente.Nombre es null, lanza ValidationException.
## 🧠 [Cache(segundos)] - Cache automático en memoria

Guarda el resultado de un método por X segundos.

```csharp
[Cache(60)]
public async Task<string> ObtenerDatos() => "Desde API";
```

➡️ Durante 60 segundos, ObtenerDatos() devuelve la misma respuesta (usando IMemoryCache).

⚠️ Solo válido para métodos síncronos o Task<T>. No se aplica a Task (sin retorno).
## Author

- [@Juliocbm](https://github.com/Juliocbm)

