# Interceptor.AOP - Changelog

## [1.1.1] - 2025-04-17

### Added
- Soporte para interrupción con `[CircuitBreaker]`.
- Soporte de fallback alternativo con `[Fallback]` para métodos sync y async.
- Auditoría con `[Audit]`: parámetros de entrada, salida, errores.
- Contexto de error personalizado con `[HandleException]`.

### Fixed
- Compatibilidad con métodos `Task<T>` para obtener el resultado correctamente.

---

## [1.1.0] - 2025-04-15

### Added
- Soporte para métodos `Task` y `Task<T>` en el interceptor.
- Integración con Polly para reintentos automáticos (`[Retry]`).
- Validaciones automáticas utilizando `DataAnnotations` (`[Validate]`).
- Medición de tiempo de ejecución con `Stopwatch` (`[MeasureTime]`).
- Logging contextualizado con `ILogger<T>`.

### Changed
- Refactorización del interceptor para mejorar la modularidad y extensibilidad.

### Added
- Log de cada intento de reintento `[Retry]` (tanto sync como async) con detalles de número de intento, método y mensaje de error.

## [1.2.2] - 2025-04-17

### Fixed
- ✅ Corrección en el orden de ejecución de políticas `Retry` y `Fallback`. Ahora el fallback solo se ejecuta si todos los reintentos fallan.

### Improved
- 🛠️ Refactor en la combinación de políticas usando `WrapAsync()` y `Wrap()` para asegurar comportamiento predecible y consistente.
