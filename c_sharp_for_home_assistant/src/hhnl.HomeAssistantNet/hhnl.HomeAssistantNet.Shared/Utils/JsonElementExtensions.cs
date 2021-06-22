namespace System.Text.Json
{
    public static class JsonElementExtensions
    {
        public static JsonElement? GetPropertyOrNull(this JsonElement element, string property)
        {
            return element.TryGetProperty(property, out var prop) ? prop : null;
        }

        public static T? ToObject<T>(this JsonElement element)
        {
            var json = element.GetRawText();
            return JsonSerializer.Deserialize<T>(json);
        }
    }
}