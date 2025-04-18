using Interceptor.AOP.Configuration;
using Interceptor.AOP.Interceptors;
using Interceptor.AOP.Attributes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Xunit;
using System.Reflection;

namespace Interceptor.AOP.Tests
{
    public class AllAttributesTests
    {
        private ITestService _interceptor;
        private Mock<ITestService> _mockService;
        private IMemoryCache _cache;
        private ILogger _logger;

        public AllAttributesTests()
        {
            _mockService = new Mock<ITestService>();
            _logger = new LoggerFactory().CreateLogger("TestLogger");
            _cache = new MemoryCache(new MemoryCacheOptions());

            var proxy = DispatchProxy.Create<ITestService, ExceptionInterceptor<ITestService>>() as ExceptionInterceptor<ITestService>;
            proxy.Configure(_mockService.Object, _logger, new InterceptorOptions
            {
                EnableRetries = true,
                EnableValidation = true,
                EnableTiming = true
            }, _cache);
            _interceptor = proxy as ITestService;
        }

        [Fact]
        public async Task RetryAttribute_ShouldRetryOnFailure()
        {
            int callCount = 0;
            _mockService.Setup(s => s.WithRetry()).Returns(() =>
            {
                callCount++;
                throw new Exception("retry");
            });

            var ex = await Assert.ThrowsAsync<Exception>(() => _interceptor.WithRetry());

            Assert.Equal("retry", ex.Message);
            Assert.Equal(4, callCount); // 1 original + 3 reintentos
        }


        [Fact]
        public async Task FallbackAttribute_ShouldExecuteFallback()
        {
            _mockService.Setup(s => s.WithFallback()).Throws(new Exception("Fail"));
            _mockService.Setup(s => s.FallbackMethod()).ReturnsAsync("fallback");

            var result = await _interceptor.WithFallback();
            Assert.Equal("fallback", result);
        }

        [Fact]
        public async Task CacheAttribute_ShouldUseCachedValue()
        {
            string cacheValue = "cached";
            _cache.Set("Interceptor.AOP.Tests.ITestService.WithCache_", cacheValue);
            var result = await _interceptor.WithCache();
            Assert.Equal(cacheValue, result);
        }

        [Fact]
        public async Task AuditAttribute_ShouldLogInputOutput()
        {
            _mockService.Setup(s => s.WithAudit()).ReturnsAsync("ok");
            var result = await _interceptor.WithAudit();
            Assert.Equal("ok", result);
        }

        [Fact]
        public async Task ValidateAttribute_ShouldThrowOnInvalid()
        {
            var ex = await Assert.ThrowsAsync<ValidationException>(() =>
                _interceptor.WithValidation(new BadModel()));
            Assert.Contains("The Name field is required", ex.Message);
        }

        [Fact]
        public async Task MeasureTimeAttribute_ShouldLogTime()
        {
            _mockService.Setup(s => s.WithTiming()).ReturnsAsync("timed");
            var result = await _interceptor.WithTiming();
            Assert.Equal("timed", result);
        }

        [Fact]
        public async Task NoAttribute_ShouldJustInvoke()
        {
            _mockService.Setup(s => s.PlainMethod()).ReturnsAsync("ok");
            var result = await _interceptor.PlainMethod();
            Assert.Equal("ok", result);
        }
    }

    public class BadModel : MyModel { }
}
