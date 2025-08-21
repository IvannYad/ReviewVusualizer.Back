using System.Text.Json;

namespace ReviewVisualizer.TestUtils
{
    public static class HttpContentExtensions
    {
        public static async Task<T?> GetEntityAsync<T>(this HttpContent content)
        {
            var contentStream = await content.ReadAsStreamAsync();
            var reviewer = await JsonSerializer.DeserializeAsync<T>(
                contentStream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );

            return reviewer;
        }

        public static async Task<string> GetStringEntity(this HttpContent content)
        {
            return await content.ReadAsStringAsync();
        }
    }
}