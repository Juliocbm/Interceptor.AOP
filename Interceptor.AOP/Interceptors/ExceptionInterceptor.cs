using System.Diagnostics;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using Polly;
using Interceptor.AOP.Attributes;
using Interceptor.AOP.Configuration;

namespace Interceptor.AOP.Interceptors
{
    public class ExceptionInterceptor<T> : DispatchProxy
    {
        private T _decorated;
        private ILogger _logger;
        private InterceptorOptions _options;

        public void Configure(T decorated, ILogger logger, InterceptorOptions options)
        {
            _decorated = decorated;
            _logger = logger;
            _options = options ?? new InterceptorOptions();
        }

        protected override object Invoke(MethodInfo method, object[] args)
        {
            var returnType = method.ReturnType;
            var isAsync = typeof(Task).IsAssignableFrom(returnType);
            var contexto = method.GetCustomAttribute<HandleExceptionAttribute>()?.Contexto ?? method.Name;

            return isAsync
                ? HandleAsync(method, args, contexto)
                : HandleSync(method, args, contexto);
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

        private async Task InvokeTaskMethodAsync(MethodInfo method, object[] args, string contexto)
        {
            try
            {
                ApplyValidation(method, args);
                var sw = StartTimerIfNeeded(method);

                var retryAttr = method.GetCustomAttribute<RetryAttribute>();

                if (_options.EnableRetries && retryAttr != null)
                {
                    await Policy
                        .Handle<Exception>()
                        .RetryAsync(retryAttr.Attempts)
                        .ExecuteAsync(() => (Task)method.Invoke(_decorated, args));
                }
                else
                {
                    await (Task)method.Invoke(_decorated, args);
                }

                StopTimerAndLogIfNeeded(method, sw);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en método async: {Method} - Contexto: {Contexto}", method.Name, contexto);
                throw;
            }
        }

        private object InvokeGenericTaskMethodAsync(MethodInfo method, object[] args, string contexto)
        {
            try
            {
                ApplyValidation(method, args);
                var sw = StartTimerIfNeeded(method);

                var retryAttr = method.GetCustomAttribute<RetryAttribute>();
                var returnType = method.ReturnType.GetGenericArguments()[0];

                var func = new Func<Task<object>>(async () =>
                {
                    var task = (Task)method.Invoke(_decorated, args);
                    await task.ConfigureAwait(false);
                    var resultProperty = task.GetType().GetProperty("Result");
                    return resultProperty?.GetValue(task);
                });

                if (_options.EnableRetries && retryAttr != null)
                {
                    var policy = Policy
                        .Handle<Exception>()
                        .RetryAsync(retryAttr.Attempts);

                    var wrappedTask = policy.ExecuteAsync(func);
                    StopTimerAndLogIfNeeded(method, sw);
                    return wrappedTask;
                }

                var original = func();
                StopTimerAndLogIfNeeded(method, sw);
                return original;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en método async<T>: {Method} - Contexto: {Contexto}", method.Name, contexto);
                throw;
            }
        }

        private object HandleSync(MethodInfo method, object[] args, string contexto)
        {
            try
            {
                ApplyValidation(method, args);
                var sw = StartTimerIfNeeded(method);

                var retryAttr = method.GetCustomAttribute<RetryAttribute>();
                object result;

                if (_options.EnableRetries && retryAttr != null)
                {
                    result = Policy
                        .Handle<Exception>()
                        .Retry(retryAttr.Attempts)
                        .Execute(() => method.Invoke(_decorated, args));
                }
                else
                {
                    result = method.Invoke(_decorated, args);
                }

                StopTimerAndLogIfNeeded(method, sw);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en método: {Method} - Contexto: {Contexto}", method.Name, contexto);
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
    }
}
