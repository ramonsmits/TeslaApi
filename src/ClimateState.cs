using Newtonsoft.Json;

namespace TeslaApi
{
    public class ClimateState
    {
        [JsonProperty(PropertyName = "driver_temp_setting")]
        public double DriverTempSetting { get; set; }
        [JsonProperty(PropertyName = "inside_temp")]
        public double InsideTemp { get; set; }
        [JsonProperty(PropertyName = "is_auto_conditioning_on")]
        public bool IsAutoConditioning { get; set; }
        [JsonProperty(PropertyName = "is_climate_on")]
        public bool IsOn { get; set; }
        [JsonProperty(PropertyName = "is_preconditioning")]
        public bool IsPreconditioning { get; set; }
    }
}
