using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace TeslaApi
{
    internal static class Splunk
    {
        internal static async Task<T> TimeItToSplunk<T>(string key, Func<Task<T>> action)
        {
            var sw = new Stopwatch();
            sw.Start();
            var result = await action();
            sw.Stop();
            WriteSplunk(key, sw.ElapsedMilliseconds);
            return result;
        }

        internal static async Task TimeItToSplunk(string key, Func<Task> action)
        {
            var sw = new Stopwatch();
            sw.Start();
            await action();
            sw.Stop();
            WriteSplunk(key, sw.ElapsedMilliseconds);
        }

        internal static void WriteSplunk(string key, dynamic value)
        {
            try
            {
                if (Service.Options.HasFlag(ConfigurationOptions.BlockMetrics)) return;
                var splunkUrl = "http://sfs05.skipfire.local:8088/services/collector/event";
                var splunkAuth = "7a8f797f-beb1-4acf-9dab-bd76370e2beb";
                var indexName = Service.Options.HasFlag(ConfigurationOptions.Tests) ? "teslaapi_test" : "teslaapi";
                var eventPayload = new { index = indexName, source = Service.DeviceId, @event = new { key, value } };
                HttpHelper.PostSplunk<object>(splunkUrl, eventPayload, splunkAuth).ContinueWith(Completer);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Debug.WriteLine(e);
            }
        }

        internal static void WriteSplunkString(string value)
        {
            try
            {
                if (Service.Options.HasFlag(ConfigurationOptions.BlockMetrics)) return;
                var splunkUrl = "http://sfs05.skipfire.local:8088/services/collector/event";
                var splunkAuth = "7a8f797f-beb1-4acf-9dab-bd76370e2beb";
                var indexName = Service.Options.HasFlag(ConfigurationOptions.Tests) ? "teslaapi_test" : "teslaapi";
                var eventPayload = new { index = indexName, source = Service.DeviceId, @event = new { value } };
                HttpHelper.PostSplunk<object>(splunkUrl, eventPayload, splunkAuth).ContinueWith(Completer);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Debug.WriteLine(e);
            }
        }

        private static void Completer(Task task) { /*Just something to be called to avoid zombie threads.*/ }
    }
}
