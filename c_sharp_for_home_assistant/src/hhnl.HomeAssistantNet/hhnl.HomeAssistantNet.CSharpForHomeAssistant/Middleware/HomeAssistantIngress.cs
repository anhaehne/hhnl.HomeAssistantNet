using System.Net;
using Microsoft.AspNetCore.Http;

namespace hhnl.HomeAssistantNet.CSharpForHomeAssistant.Middleware
{
    public static class HomeAssistantIngress
    {
        private static readonly IPAddress _ingressHost = new(new byte[] { 172, 30, 32, 2 });

        public static bool RequestIsViaIngress(HttpContext context)
        {
            // We only have to check ipv4 addresses since the internal docker network is ipv4 only.
            if (context.Connection.RemoteIpAddress is null)
                return false;

            return Equals(context.Connection.RemoteIpAddress.MapToIPv4(), _ingressHost);
        }
    }
}