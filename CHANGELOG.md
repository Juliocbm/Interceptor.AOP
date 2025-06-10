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


## [1.3.6] - 2025-04-22

### Changed
- 🔄 Rediseño del atributo `[Audit]` para incluir todo lo que antes hacía `[HandleException]`.
- `[Audit]` ahora permite controlar individualmente el log de entrada, salida y errores (`LogInput`, `LogOutput`, `LogError`).
- Removido el atributo `[HandleException]` como requisito para logging de errores (ya incluido en `[Audit]`).

### Added
- 🧠 Parámetro `Contexto` opcional en `[Audit]` para personalizar el contexto funcional.
- 📦 Nueva sección de ejemplo global en el `README.md` para mostrar uso combinado de todos los atributos.

### Improved
- 📘 Documentación completa del atributo `[Audit]` y todos los demás con ejemplos y explicación clara.
- 🧪 Todas las pruebas unitarias actualizadas para reflejar los nuevos comportamientos de `[Audit]`.

## [1.3.7] - 2025-04-22

### Changed
- `[Audit]` Removido log de errores no controlados por [Audit] para no logear excepciones duplicada

### Added
- 🧠 Parámetro `method.Name` en `[Audit]` para personalizar mas el log ademas del contexto funcional.

## [1.3.8] - 2025-04-22

### Changed
- Se documenta ejemplo de configuracion de Program.cs o Startup.cs en README.md

## [1.3.9] - 2025-04-30

### Changed
- Se agrega log informativo cuando se ejecuta metodo fallback

## [1.3.10] - 2025-04-30

### Changed
- Se agrega log informativo cuando se ejecuta metodo fallback en metodo handleAsync

## [1.3.11] - 2025-04-30
### Changed
- Ajuste de README.md

## [1.3.12] - 2025-06-10

### Removed
- Eliminado completamente el archivo `HandleExceptionAttribute` y su referencia
  en el interceptor principal.
