using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interceptor.AOP.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AuditAttribute : Attribute
    {
        public string Contexto { get; set; }
        public bool LogInput { get; set; } = true;
        public bool LogOutput { get; set; } = true;
        public bool LogError { get; set; } = true;

        public AuditAttribute() { }

        public AuditAttribute(string contexto)
        {
            Contexto = contexto;
        }
    }
}
