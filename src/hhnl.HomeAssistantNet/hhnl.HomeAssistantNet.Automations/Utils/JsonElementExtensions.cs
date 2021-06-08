using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json
{
    public static class JsonElementExtensions
    {
        public static async Task<T?> ToObjectAsync<T>(
            this JsonElement element,
            JsonSerializerOptions? options = null,
            CancellationToken token = default)
        {
            var bufferWriter = new ArrayBufferWriter<byte>();
            await using var writer = new Utf8JsonWriter(bufferWriter);
            element.WriteTo(writer);

            // NOTE: call Flush on the writer before Deserializing since Dispose is called at the end of the scope of the method
            await writer.FlushAsync(token);

            return JsonSerializer.Deserialize<T>(bufferWriter.WrittenSpan, options);
        }
    }
}