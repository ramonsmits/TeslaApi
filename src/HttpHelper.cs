using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TeslaApi
{
    internal static class HttpHelper
    {
        internal static async Task<TReturn> HttpGetOAuth<TReturn>(this HttpClient hc, string url)
        {
            var result = await hc.GetAsync(url);
            result.EnsureSuccessStatusCode();
            var resultContent = await result.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(resultContent)) return default;
            return JsonConvert.DeserializeObject<TReturn>(resultContent);
        }

        internal static async Task<TReturn> HttpPost<TReturn, TBody>(this HttpClient hc, string url, TBody body)
        {
            var bodyContent = JsonConvert.SerializeObject(body);
            using (var content = new StringContent(bodyContent, Encoding.UTF8, "application/json"))
            {
                var result = await hc.PostAsync(url, content);
                result.EnsureSuccessStatusCode();
                var resultContent = await result.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(resultContent)) return default;
                return JsonConvert.DeserializeObject<TReturn>(resultContent);
            }
        }

        internal static async Task<TReturn> HttpPostOAuth<TReturn, TBody>(this HttpClient hc, string url, TBody body)
        {
            var bodyContent = JsonConvert.SerializeObject(body);
            using (var content = new StringContent(bodyContent, Encoding.UTF8, "application/json"))
            {
                var result = await hc.PostAsync(url, content);
                result.EnsureSuccessStatusCode();
                var resultContent = await result.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(resultContent)) return default;
                return JsonConvert.DeserializeObject<TReturn>(resultContent);
            }
        }
    }
}
