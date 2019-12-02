using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TeslaApi
{
    internal class HttpHelper
    {
        internal static string UserAgent;
        internal static IWebProxy Proxy;

        internal static async Task<TReturn> HttpGetOAuth<TReturn>(string authorization, string url)
        {
            using (var hc = CreateHttpClient())
            {
                AddAgent(hc);
                hc.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authorization);
                var result = await hc.GetAsync(url);
                var resultContent = await result.Content.ReadAsStringAsync();
                hc.DefaultRequestHeaders.Remove("Authorization");
                if (string.IsNullOrWhiteSpace(resultContent)) return default(TReturn);
                return JsonConvert.DeserializeObject<TReturn>(resultContent);
            }
        }

        internal static async Task<TReturn> HttpPost<TReturn, TBody>(string url, TBody body)
        {
            var bodyContent = JsonConvert.SerializeObject(body);
            using (var content = new StringContent(bodyContent, Encoding.UTF8, "application/json"))
            {
                using (var hc = CreateHttpClient())
                {
                    AddAgent(hc);
                    hc.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var result = await hc.PostAsync(url, content);
                    var resultContent = await result.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(resultContent)) return default(TReturn);
                    return JsonConvert.DeserializeObject<TReturn>(resultContent);
                }
            }
        }

        internal static async Task<TReturn> HttpPostOAuth<TReturn, TBody>(string authorization, string url, TBody body)
        {
            var bodyContent = JsonConvert.SerializeObject(body);
            using (var content = new StringContent(bodyContent, Encoding.UTF8, "application/json"))
            {
                using (var hc = CreateHttpClient())
                {
                    AddAgent(hc);
                    hc.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authorization);
                    var result = await hc.PostAsync(url, content);
                    var resultContent = await result.Content.ReadAsStringAsync();
                    hc.DefaultRequestHeaders.Remove("Authorization");
                    if (string.IsNullOrWhiteSpace(resultContent)) return default(TReturn);
                    return JsonConvert.DeserializeObject<TReturn>(resultContent);
                }
            }
        }

        private static void AddAgent(HttpClient hc)
        {
            if (string.IsNullOrWhiteSpace(UserAgent)) return;
            var userAgent = new ProductInfoHeaderValue(new ProductHeaderValue(UserAgent));
            hc.DefaultRequestHeaders.UserAgent.Add(userAgent);
        }

        private static HttpClient CreateHttpClient()
        {
            if (Proxy != null)
                return new HttpClient(new HttpClientHandler {Proxy = Proxy});
            return new HttpClient();
        }
    }
}
