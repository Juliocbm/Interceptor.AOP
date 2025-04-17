using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interceptor.AOP.Configuration
{
    public class InterceptorOptions
    {
        public bool EnableRetries { get; set; } = true;
        public bool EnableValidation { get; set; } = true;
        public bool EnableTiming { get; set; } = true;
    }
}
