using Interceptor.AOP.Interceptors;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Interceptor.AOP.Utilities
{
    public static class ProxyFactory
    {
        public static T Create<T>(T decorated, ILogger logger, Func<Exception, Task>? callback = null)
        {
            var proxy = DispatchProxy.Create<T, ExceptionInterceptor<T>>();
            ((ExceptionInterceptor<T>)(object)proxy).Configure(decorated, logger, callback);
            return proxy;
        }
    }
}
