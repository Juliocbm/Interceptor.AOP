using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interceptor.AOP.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CacheAttribute : Attribute
    {
        public int DurationSeconds { get; }

        public CacheAttribute(int durationSeconds = 60)
        {
            DurationSeconds = durationSeconds;
        }
    }
}
