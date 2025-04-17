using Interceptor.AOP.Configuration;
using Interceptor.AOP.Interceptors;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Interceptor.AOP.Utilities
{
    public static class ProxyFactory
    {
        public static TInterface Create<TInterface>(
            TInterface decorated,
            ILogger logger,
            IMemoryCache memoryCache,
            InterceptorOptions options = null)
            where TInterface : class
        {
            var proxy = DispatchProxy.Create<TInterface, ExceptionInterceptor<TInterface>>();
            ((ExceptionInterceptor<TInterface>)(object)proxy).Configure(
                decorated,
                logger,
                options ?? new InterceptorOptions(),
                memoryCache
            );
            return proxy;
        }
    }

}
