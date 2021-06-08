using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json
{
    public static class JsonElementExtensions
    {
        public static JsonElement? GetPropertyOrNull(this JsonElement element, string property)
        {
            return element.TryGetProperty(property, out var prop) ? prop : null;
        }
    }
}