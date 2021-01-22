using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DL444.Ucqu.App.WinUniversal.Extensions
{
    internal static class HttpContentExtension
    {
        public static async Task<T> ReadAsJsonObjectAsync<T>(this HttpContent content)
        {
            Stream stream = await content.ReadAsStreamAsync();
            using (var streamReader = new StreamReader(stream))
            {
                using (var jsonReader = new JsonTextReader(streamReader))
                {
                    return new JsonSerializer().Deserialize<T>(jsonReader);
                }
            }
        }
    }
}
