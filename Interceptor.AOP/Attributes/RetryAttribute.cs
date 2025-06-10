using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interceptor.AOP.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RetryAttribute : Attribute
    {
        public int Attempts { get; }

        public RetryAttribute(int attempts = 3)
        {
            if (attempts < 1)
                throw new ArgumentOutOfRangeException(nameof(attempts));

            Attempts = attempts;
        }
    }
}
