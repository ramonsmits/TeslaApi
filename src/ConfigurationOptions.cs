using System;

namespace TeslaApi
{
    [Flags]
    public enum ConfigurationOptions
    {
        None = 0,
        RemoteStartWithoutPassword = 1
    }
}
