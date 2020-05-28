using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TeslaApi
{
    public interface ITelemetryProvider
    {
        void SetWebProxy(IWebProxy proxy);
        void ClearWebProxy();
        Task Time(string key, Func<Task> function);
        Task<T> Time<T>(string key, Func<Task<T>> function);

    }
}
