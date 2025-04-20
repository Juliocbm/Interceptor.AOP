# Interceptor.AOP.AspNetCore

Extensión para ASP.NET Core que permite registrar servicios automáticamente con interceptores AOP (Polly, Retry, Fallback, Cache, Audit, etc.).

## Uso rápido

```csharp
services.AddInterceptedTransient<IMiServicio, MiServicio>();
