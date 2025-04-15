# 📦 Interceptor.AOP

Una librería ligera y extensible para aplicar **Aspect-Oriented Programming (AOP)** en aplicaciones .NET mediante **interceptores de método automáticos** usando `DispatchProxy`.

## 🎯 Propósito

Centralizar y estandarizar el manejo de lógica transversal como:

- 🛡️ Manejo automático de excepciones
- 📝 Logging estructurado con `ILogger<T>`
- 🧠 Contextualización de errores usando atributos
- 💡 Preparado para futuras extensiones (reintentos, métricas, validaciones, etc.)

---

## 🚀 Características

✔️ Intercepta automáticamente todos los métodos públicos de servicios registrados por interfaz  
✔️ Captura excepciones sin necesidad de `try/catch` en cada método  
✔️ Registra errores con la categoría de clase usando `ILogger<T>`  
✔️ Compatible con `Serilog`, `Seq`, `Application Insights`, etc.  
✔️ Opcional: usa el atributo `[HandleException("Contexto")]` para enriquecer los logs

---

## 🧱 Cómo instalar

Desde NuGet:

```bash
dotnet add package Interceptor.AOP
