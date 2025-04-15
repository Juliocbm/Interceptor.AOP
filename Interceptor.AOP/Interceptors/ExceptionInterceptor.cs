using Interceptor.AOP.Attributes;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Interceptor.AOP.Interceptors
{
    public class ExceptionInterceptor<T> : DispatchProxy
    {
        private T _decorated;
        private ILogger _logger;
        private Func<Exception, Task>? _errorCallback;

        public void Configure(T decorated, ILogger logger, Func<Exception, Task>? errorCallback = null)
        {
            _decorated = decorated;
            _logger = logger;
            _errorCallback = errorCallback;
        }

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            var attr = targetMethod.GetCustomAttribute<HandleExceptionAttribute>();

            try
            {
                var result = targetMethod.Invoke(_decorated, args);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error en método: {targetMethod.Name} - Contexto: {attr?.Contexto ?? "N/A"}");

                _errorCallback?.Invoke(ex);

                throw;
            }
        }
    }
}
