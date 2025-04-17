# 📦 Interceptor.AOP

[![NuGet](https://img.shields.io/nuget/v/Interceptor.AOP.svg?style=flat-square)](https://www.nuget.org/packages/Interceptor.AOP/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Interceptor.AOP.svg?style=flat-square)](https://www.nuget.org/packages/Interceptor.AOP/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square)](https://opensource.org/licenses/MIT)

Una librería ligera y extensible para aplicar **Aspect-Oriented Programming (AOP)** en aplicaciones .NET, usando interceptores automáticos basados en `DispatchProxy`.

Ideal para centralizar lógica transversal como **reintentos, validaciones, cacheo, auditoría, y manejo de errores** sin contaminar la lógica de negocio.

---

## 🎯 Propósito

Centralizar y estandarizar:

- 🛡️ Manejo de excepciones con contexto
- 🔁 Reintentos y circuit breaker con Polly
- 🧪 Validaciones automáticas con DataAnnotations
- ⏱️ Medición de tiempo de ejecución
- 🔐 Cacheo en memoria por método
- 📝 Logging estructurado y auditoría completa

---

## 🚀 Características

✔️ Soporte para métodos síncronos y asincrónicos (`Task`, `Task<T>`)  
✔️ Decoración por atributos: fácil de aplicar, sin modificar código existente  
✔️ Totalmente extensible: desacoplado y adaptable  
✔️ Compatible con `ILogger<T>`, `IMemoryCache`, y Polly  
✔️ Ideal para microservicios, APIs, backend robusto

---

## 🧱 Instalación

Desde NuGet:

```bash
dotnet add package Interceptor.AOP
