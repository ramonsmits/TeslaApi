using Newtonsoft.Json;

namespace TeslaApi
{
    public class DriveState
    {
        /// <summary>
        /// Power output in kilowatt's, negative value when regen or charging
        /// </summary>
        [JsonProperty(PropertyName = "power")]
        public int? Power { get; set; }
        [JsonProperty(PropertyName = "shift_state")]
        public string Shifter { get; set; }
        [JsonProperty(PropertyName = "latitude")]
        public double Latitude { get; set; }
        [JsonProperty(PropertyName = "longitude")]
        public double Longitude { get; set; }
    }
}
