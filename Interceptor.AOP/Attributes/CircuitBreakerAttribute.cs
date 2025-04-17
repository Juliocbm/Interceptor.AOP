using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interceptor.AOP.Attributes
{
    // CircuitBreakerAttribute.cs
    [AttributeUsage(AttributeTargets.Method)]
    public class CircuitBreakerAttribute : Attribute
    {
        public int ExceptionsAllowedBeforeBreaking { get; }
        public int DurationOfBreakSeconds { get; }

        public CircuitBreakerAttribute(int exceptionsAllowedBeforeBreaking = 3, int durationOfBreakSeconds = 30)
        {
            ExceptionsAllowedBeforeBreaking = exceptionsAllowedBeforeBreaking;
            DurationOfBreakSeconds = durationOfBreakSeconds;
        }
    }
}