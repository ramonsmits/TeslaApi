using System;
using System.Threading.Tasks;

namespace TeslaApi
{
    public interface ITelemetryProvider
    {
        Task Time(string key, Func<Task> function);
        Task<T> Time<T>(string key, Func<Task<T>> function);
    }
}
