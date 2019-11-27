using Newtonsoft.Json;

namespace TeslaApi
{
    public class Vehicle
    {
        [JsonProperty(PropertyName = "id")]
        public long Id { get; set; }
        [JsonProperty(PropertyName = "display_name")]
        public string DisplayName { get; set; }
        [JsonProperty(PropertyName = "state")]
        public string State { get; set; }
    }
}
