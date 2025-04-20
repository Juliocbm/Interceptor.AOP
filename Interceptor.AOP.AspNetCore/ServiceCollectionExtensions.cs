using Interceptor.AOP.Configuration;
using Interceptor.AOP.Interceptors;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;

namespace Interceptor.AOP.AspNetCore
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInterceptedTransient<TInterface, TImplementation>(this IServiceCollection services)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            services.AddTransient<TImplementation>();
            services.AddTransient<TInterface>(provider =>
            {
                var impl = provider.GetRequiredService<TImplementation>();
                var logger = provider.GetRequiredService<ILogger<ExceptionInterceptor<TInterface>>>();
                var cache = provider.GetRequiredService<IMemoryCache>();
                var options = provider.GetService<InterceptorOptions>() ?? new InterceptorOptions();

                var proxy = DispatchProxy.Create<TInterface, ExceptionInterceptor<TInterface>>() as ExceptionInterceptor<TInterface>;
                proxy.Configure(impl, logger, options, cache);
                return proxy as TInterface;
            });

            return services;
        }

        public static IServiceCollection AddInterceptedScoped<TInterface, TImplementation>(this IServiceCollection services)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            services.AddScoped<TImplementation>();
            services.AddScoped<TInterface>(provider =>
            {
                var impl = provider.GetRequiredService<TImplementation>();
                var logger = provider.GetRequiredService<ILogger<ExceptionInterceptor<TInterface>>>();
                var cache = provider.GetRequiredService<IMemoryCache>();
                var options = provider.GetService<InterceptorOptions>() ?? new InterceptorOptions();

                var proxy = DispatchProxy.Create<TInterface, ExceptionInterceptor<TInterface>>() as ExceptionInterceptor<TInterface>;
                proxy.Configure(impl, logger, options, cache);
                return proxy as TInterface;
            });

            return services;
        }
    }
}
