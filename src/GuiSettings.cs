using Newtonsoft.Json;

namespace TeslaApi
{
    public class GuiSettings
    {
        [JsonProperty(PropertyName = "gui_charge_rate_units")]
        public string ChargeRateUnit { get; set; }

        /// <summary>
        /// Is not provided correctly by the Tesla API.  Provides in units per hour (mi/h, km/h) instead of mi or km
        /// </summary>
        [JsonProperty(PropertyName = "gui_distance_units")]
        public string DistanceUnit { get; set; }

        [JsonProperty(PropertyName = "gui_temperature_units")]
        public string TemperatureUnit { get; set; }
    }
}
