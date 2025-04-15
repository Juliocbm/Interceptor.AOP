using System;

namespace Interceptor.AOP.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class HandleExceptionAttribute : Attribute
    {
        public string Contexto { get; }

        public HandleExceptionAttribute(string contexto = "")
        {
            Contexto = contexto;
        }
    }
}
