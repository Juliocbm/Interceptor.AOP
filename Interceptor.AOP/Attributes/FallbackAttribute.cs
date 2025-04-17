using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interceptor.AOP.Attributes
{
    // FallbackAttribute.cs
    [AttributeUsage(AttributeTargets.Method)]
    public class FallbackAttribute : Attribute
    {
        public string FallbackMethodName { get; }

        public FallbackAttribute(string fallbackMethodName)
        {
            FallbackMethodName = fallbackMethodName;
        }
    }
}
