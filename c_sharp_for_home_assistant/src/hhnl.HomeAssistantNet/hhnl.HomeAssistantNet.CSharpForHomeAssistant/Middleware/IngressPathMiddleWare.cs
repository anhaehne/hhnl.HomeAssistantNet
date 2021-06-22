using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Middleware
{
    public class IngressPathMiddleWare
    {
        private const string XIngressPathHeaderName = "X-Ingress-Path";
        private readonly RequestDelegate _next;

        public IngressPathMiddleWare(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Headers[XIngressPathHeaderName] != StringValues.Empty)
                context.Request.PathBase = context.Request.Headers[XIngressPathHeaderName].First();

            await _next.Invoke(context);
        }
    }
}