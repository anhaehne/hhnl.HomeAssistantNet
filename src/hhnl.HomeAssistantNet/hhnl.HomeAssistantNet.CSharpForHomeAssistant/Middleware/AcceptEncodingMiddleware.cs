using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Middleware
{
    /// <summary>
    /// Middle ware to remove brotli from the accepted encoding when the request source is the ingress reverse proxy.
    /// The reverse proxy will forward the accepted encoding containing brotli even though, brotli is not supported.
    /// </summary>
    public class AcceptEncodingMiddleware
    {
        private readonly RequestDelegate _next;

        public AcceptEncodingMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task Invoke(HttpContext context)
        {
            // We only have to check ipv4 addresses since the internal docker network is ipv4 only.
            if (!HomeAssistantIngress.RequestIsViaIngress(context))
            {
                await _next(context);
                return;
            }

            var acceptEncoding = context.Request.Headers["Accept-Encoding"].ToString().Split(",").Select(x => x.Trim()).ToList();

            // Check if the request source is the ingress reverse proxy and the accept encoding contains brotli (br). If so remove it from the list.
            if (acceptEncoding.Contains("br"))
                context.Request.Headers["Accept-Encoding"] =
                    new StringValues(string.Join(", ", acceptEncoding.Except(new[] { "br" })));

            await _next(context);
        }
    }
}