using System.Diagnostics;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using Polly;
using Interceptor.AOP.Attributes;
using Interceptor.AOP.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Polly.Fallback;
using Polly.Retry;

namespace Interceptor.AOP.Interceptors
{
    public class ExceptionInterceptor<T> : DispatchProxy
    {
        private T _decorated;
        private ILogger _logger;
        private InterceptorOptions _options;
        private IMemoryCache _memoryCache;

        public void Configure(T decorated, ILogger logger, InterceptorOptions options, IMemoryCache memoryCache)
        {
            _decorated = decorated;
            _logger = logger;
            _options = options ?? new InterceptorOptions();
            _memoryCache = memoryCache;
        }

        protected override object Invoke(MethodInfo method, object[] args)
        {
            var returnType = method.ReturnType;
            var isAsync = typeof(Task).IsAssignableFrom(returnType); 

            var contexto =
                method.GetCustomAttribute<AuditAttribute>()?.Contexto ??
                method.GetCustomAttribute<HandleExceptionAttribute>()?.Contexto ??
                method.Name;

            return isAsync
                ? HandleAsync(method, args, contexto)
                : HandleSync(method, args, contexto);
        }

        private async Task InvokeTaskMethodAsync(MethodInfo method, object[] args, string contexto)
        {
            try
            {
                ApplyValidation(method, args);
                LogAuditInput(method, args); // 📥 Entrada

                var sw = StartTimerIfNeeded(method);

                var retryPolicy = CreateAsyncPolicy(method);
                var fallbackAttr = method.GetCustomAttribute<FallbackAttribute>();

                if (fallbackAttr != null)
                {
                    var fallbackMethod = GetFallbackMethod(method, fallbackAttr.FallbackMethodName);

                    var fallbackPolicy = Policy
                        .Handle<Exception>()
                        .FallbackAsync(async (ct) =>
                        {
                            var fallbackReturn = fallbackMethod.Invoke(_decorated, args);

                            object fallbackResult;
                            if (fallbackReturn is Task task)
                            {
                                await task.ConfigureAwait(false);
                                fallbackResult = fallbackReturn.GetType().GetProperty("Result")?.GetValue(fallbackReturn);
                            }
                            else
                            {
                                fallbackResult = fallbackReturn;
                            }

                            LogAuditOutput(method, fallbackResult);

                        });

                    var policyWrap = fallbackPolicy.WrapAsync(retryPolicy);  // 👈 invertido
                    await policyWrap.ExecuteAsync(() => (Task)method.Invoke(_decorated, args));
                }
                else
                {
                    await retryPolicy.ExecuteAsync(() => (Task)method.Invoke(_decorated, args));
                    LogAuditOutput(method, "<void>"); // 📤 Salida normal
                }

                StopTimerAndLogIfNeeded(method, sw);
            }
            catch (TargetInvocationException tie) when (tie.InnerException != null)
            {
                LogAuditError(method, tie.InnerException);
                throw tie.InnerException;
            }
            catch (Exception ex)
            {
                LogAuditError(method, ex);
                throw;
            }

        }

        private object InvokeGenericTaskMethodAsync(MethodInfo method, object[] args, string contexto)
        {
            try
            {
                ApplyValidation(method, args);
                LogAuditInput(method, args);
                var sw = StartTimerIfNeeded(method);

                var cacheAttr = method.GetCustomAttribute<CacheAttribute>();
                var fallbackAttr = method.GetCustomAttribute<FallbackAttribute>();
                var cacheKey = cacheAttr != null ? GenerateCacheKey(method, args) : null;

                if (cacheAttr != null && _memoryCache.TryGetValue(cacheKey, out var cached))
                {
                    _logger.LogInformation("🔁 Cache HIT async<T> en {Method}", method.Name);
                    LogAuditOutput(method, cached);
                    return CreateTypedTaskResult(method, cached);
                }

                var func = new Func<Task<object>>(async () =>
                {
                    var task = (Task)method.Invoke(_decorated, args);
                    await task.ConfigureAwait(false);
                    var result = task.GetType().GetProperty("Result")?.GetValue(task);

                    if (cacheAttr != null)
                        _memoryCache.Set(cacheKey, result, TimeSpan.FromSeconds(cacheAttr.DurationSeconds));

                    LogAuditOutput(method, result);
                    return result;
                });

                var retryPolicy = CreateAsyncPolicy(method);

                Task<object> resultTask;

                if (fallbackAttr != null)
                {
                    var fallbackMethod = GetFallbackMethod(method, fallbackAttr.FallbackMethodName);

                    var fallbackPolicy = Policy<object>
                        .Handle<Exception>()
                        .FallbackAsync(async ct =>
                        {
                            var fallbackReturn = fallbackMethod.Invoke(_decorated, args);

                            object fallbackResult;
                            if (fallbackReturn is Task task)
                            {
                                await task.ConfigureAwait(false);
                                fallbackResult = fallbackReturn.GetType().GetProperty("Result")?.GetValue(fallbackReturn);
                            }
                            else
                            {
                                fallbackResult = fallbackReturn;
                            }

                            LogAuditOutput(method, fallbackResult);
                            return fallbackResult;

                        });

                    resultTask = fallbackPolicy.WrapAsync(retryPolicy).ExecuteAsync(func);
                }
                else
                {
                    resultTask = retryPolicy.ExecuteAsync(func);
                }

                StopTimerAndLogIfNeeded(method, sw);

                return CreateTypedTaskResult(method, resultTask.Result);
            }
            catch (TargetInvocationException tie) when (tie.InnerException != null)
            {
                LogAuditError(method, tie.InnerException);
                throw tie.InnerException;
            }
            catch (Exception ex)
            {
                LogAuditError(method, ex);
                throw;
            }

        }

        private object CreateTypedTaskResult(MethodInfo method, object result)
        {
            var resultType = method.ReturnType.GenericTypeArguments[0];
            var fromResultMethod = typeof(Task).GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(resultType);
            return fromResultMethod.Invoke(null, new[] { result });
        }

        private MethodInfo GetFallbackMethod(MethodInfo originalMethod, string fallbackMethodName)
        {
            var fallbackMethod = _decorated.GetType()
                .GetMethod(fallbackMethodName, originalMethod.GetParameters().Select(p => p.ParameterType).ToArray());

            if (fallbackMethod == null)
                throw new InvalidOperationException($"⚠️ Método fallback '{fallbackMethodName}' no encontrado con la misma firma que '{originalMethod.Name}'.");

            return fallbackMethod;
        }

        private string GenerateCacheKey(MethodInfo method, object[] args)
        {
            var methodName = $"{typeof(T).FullName}.{method.Name}";
            var parameters = string.Join("_", args.Select(a => a?.ToString() ?? "<null>"));
            return $"{methodName}_{parameters}";
        }

        private object HandleAsync(MethodInfo method, object[] args, string contexto)
        {
            var returnType = method.ReturnType;

            if (returnType == typeof(Task))
                return InvokeTaskMethodAsync(method, args, contexto);

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
                return InvokeGenericTaskMethodAsync(method, args, contexto);

            throw new InvalidOperationException("Tipo async no soportado.");
        }

        private object HandleSync(MethodInfo method, object[] args, string contexto)
        {

            try
            {
                ApplyValidation(method, args);

                LogAuditInput(method, args);

                var sw = StartTimerIfNeeded(method);

                var cacheAttr = method.GetCustomAttribute<CacheAttribute>();
                if (cacheAttr != null)
                {
                    var cacheKey = GenerateCacheKey(method, args);
                    if (_memoryCache.TryGetValue(cacheKey, out var cached))
                    {
                        _logger.LogInformation("🔁 Cache HIT en {Method}", method.Name);
                        return cached;
                    }

                    var result = method.Invoke(_decorated, args);
                    _memoryCache.Set(cacheKey, result, TimeSpan.FromSeconds(cacheAttr.DurationSeconds));
                    StopTimerAndLogIfNeeded(method, sw);
                    return result;
                }

                var retryPolicy = CreateSyncPolicy(method);
                var fallbackAttr = method.GetCustomAttribute<FallbackAttribute>();

                object resultado;

                if (fallbackAttr != null)
                {
                    var fallbackMethod = GetFallbackMethod(method, fallbackAttr.FallbackMethodName);
                    var fallbackPolicy = Policy<object>
                        .Handle<Exception>()
                        .Fallback(() => fallbackMethod.Invoke(_decorated, args));

                    var policyWrap = fallbackPolicy.Wrap(retryPolicy); // ✔️ retry antes que fallback

                    resultado = policyWrap.Execute(() => method.Invoke(_decorated, args));
                }
                else
                {
                    resultado = retryPolicy.Execute(() => method.Invoke(_decorated, args));
                }

                StopTimerAndLogIfNeeded(method, sw);

                LogAuditOutput(method, resultado);

                return resultado;
            }
            catch (Exception ex)
            {
                LogAuditError(method, ex);
                throw;
            }
        }

        private void ApplyValidation(MethodInfo method, object[] args)
        {
            if (_options.EnableValidation && method.GetCustomAttribute<ValidateAttribute>() != null)
            {
                foreach (var arg in args)
                {
                    if (arg == null) continue;
                    var context = new ValidationContext(arg);
                    Validator.ValidateObject(arg, context, validateAllProperties: true);
                }
            }
        }

        private Stopwatch StartTimerIfNeeded(MethodInfo method)
        {
            return _options.EnableTiming && method.GetCustomAttribute<MeasureTimeAttribute>() != null
                ? Stopwatch.StartNew()
                : null;
        }

        private void StopTimerAndLogIfNeeded(MethodInfo method, Stopwatch sw)
        {
            if (sw != null)
            {
                sw.Stop();
                _logger.LogInformation("⏱️ {Method} ejecutado en {Ms}ms", method.Name, sw.ElapsedMilliseconds);
            }
        }

        private IAsyncPolicy CreateAsyncPolicy(MethodInfo method)
        {
            var retryAttr = method.GetCustomAttribute<RetryAttribute>();
            var circuitAttr = method.GetCustomAttribute<CircuitBreakerAttribute>();

            IAsyncPolicy policy = Policy.NoOpAsync();

            if (_options.EnableRetries && retryAttr != null)
            {
                policy = policy.WrapAsync(
                    //Policy.Handle<Exception>().RetryAsync(retryAttr.Attempts)

                    Policy
                    .Handle<Exception>()
                    .RetryAsync(retryAttr.Attempts, onRetry: (exception, retryCount, context) =>
                    {
                        _logger.LogWarning("🔁 Reintento async #{RetryCount} en {Method} - Error: {Message}",
                            retryCount, method.Name, exception.Message);
                    })

                );
            }

            if (circuitAttr != null)
            {
                policy = policy.WrapAsync(
                    Policy
                        .Handle<Exception>()
                        .CircuitBreakerAsync(
                            circuitAttr.ExceptionsAllowedBeforeBreaking,
                            TimeSpan.FromSeconds(circuitAttr.DurationOfBreakSeconds)
                        )
                );
            }

            return policy;
        }

        private ISyncPolicy CreateSyncPolicy(MethodInfo method)
        {
            var retryAttr = method.GetCustomAttribute<RetryAttribute>();
            var circuitAttr = method.GetCustomAttribute<CircuitBreakerAttribute>();

            ISyncPolicy policy = Policy.NoOp();

            if (_options.EnableRetries && retryAttr != null)
            {
                policy = policy.Wrap(
                    //Policy.Handle<Exception>().Retry(retryAttr.Attempts)

                    Policy
                    .Handle<Exception>()
                    .Retry(retryAttr.Attempts, onRetry: (exception, retryCount) =>
                    {
                        _logger.LogWarning("🔁 Reintento sync #{RetryCount} en {Method} - Error: {Message}",
                            retryCount, method.Name, exception.Message);
                    })

                );
            }

            if (circuitAttr != null)
            {
                policy = policy.Wrap(
                    Policy
                        .Handle<Exception>()
                        .CircuitBreaker(
                            circuitAttr.ExceptionsAllowedBeforeBreaking,
                            TimeSpan.FromSeconds(circuitAttr.DurationOfBreakSeconds)
                        )
                );
            }

            return policy;
        }

        private void LogAuditInput(MethodInfo method, object[] args)
        {
            var auditAttr = method.GetCustomAttribute<AuditAttribute>();

            if (auditAttr?.LogInput == true)
            {
                var contexto = auditAttr.Contexto ?? method.Name;
                var argList = string.Join(", ", args.Select(a => a?.ToString() ?? "<null>"));
                _logger.LogInformation("📥 Entrada de método: {Method} - {Contexto}: {Args}", method.Name, contexto, argList);
            }
        }

        private void LogAuditOutput(MethodInfo method, object result)
        {
            var auditAttr = method.GetCustomAttribute<AuditAttribute>();

            if (auditAttr?.LogOutput == true)
            {
                var contexto = auditAttr.Contexto ?? method.Name;
                _logger.LogInformation("📤 Salida de método: {Method} - {Contexto}: {Result}", method.Name, contexto, result);
            }
        }

        private void LogAuditError(MethodInfo method, Exception ex)
        {
            var auditAttr = method.GetCustomAttribute<AuditAttribute>();

            if (auditAttr?.LogError == true)
            {
                var contexto = auditAttr.Contexto ?? method.Name;
                _logger.LogError(ex, "❌ Error en método: {Method} - Contexto: {Contexto}", method.Name, contexto);
            }
        }
    }
}
