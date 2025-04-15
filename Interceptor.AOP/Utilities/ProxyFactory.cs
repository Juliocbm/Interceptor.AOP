using Interceptor.AOP.Configuration;
using Interceptor.AOP.Interceptors;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Interceptor.AOP.Utilities
{
    //public static class ProxyFactory
    //{
    //    public static T Create<T>(T decorated, ILogger logger, Func<Exception, Task>? callback = null)
    //    {
    //        var proxy = DispatchProxy.Create<T, ExceptionInterceptor<T>>();
    //        ((ExceptionInterceptor<T>)(object)proxy).Configure(decorated, logger, callback);
    //        return proxy;
    //    }
    //}

    public static class ProxyFactory
    {
        public static TInterface Create<TInterface>(TInterface decorated, ILogger logger, InterceptorOptions options = null)
            where TInterface : class
        {
            var proxy = DispatchProxy.Create<TInterface, ExceptionInterceptor<TInterface>>();
            ((ExceptionInterceptor<TInterface>)(object)proxy).Configure(decorated, logger, options ?? new InterceptorOptions());
            return proxy;
        }
    }

}
