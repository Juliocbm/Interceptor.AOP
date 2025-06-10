using Interceptor.AOP.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interceptor.AOP.Tests
{
    public interface ITestService
    {
        [Retry(3)]
        Task WithRetry();

        [Fallback("FallbackMethod")]
        Task<string> WithFallback();

        Task<string> FallbackMethod();

        [Fallback("FallbackMethodWithException")]
        Task<string> WithFallbackException();

        Task<string> FallbackMethodWithException(Exception ex);

        [Cache(60)]
        Task<string> WithCache();

        [Audit]
        Task<string> WithAudit();

        [Validate]
        Task WithValidation(MyModel model);

        [MeasureTime]
        Task<string> WithTiming();

        Task<string> PlainMethod();
    }

    public class MyModel
    {
        [Required]
        public string Name { get; set; }
    }

}
