using Newtonsoft.Json;

namespace TeslaApi
{
    public class ClimateState
    {
        [JsonProperty(PropertyName = "driver_temp_setting")]
        public double DriverTempSetting { get; set; }
        [JsonProperty(PropertyName = "inside_temp")]
        public double? InsideTemp { get; set; }
        [JsonProperty(PropertyName = "is_auto_conditioning_on")]
        public bool? IsAutoConditioning { get; set; }
        [JsonProperty(PropertyName = "is_climate_on")]
        public bool IsOn { get; set; }
        [JsonProperty(PropertyName = "is_preconditioning")]
        public bool IsPreconditioning { get; set; }

        [JsonProperty(PropertyName = "seat_heater_left")]
        public int SeatHeaterFrontLeft { get; set; }
        [JsonProperty(PropertyName = "seat_heater_right")]
        public int SeatHeaterFrontRight { get; set; }
        [JsonProperty(PropertyName = "seat_heater_rear_left")]
        public int SeatHeaterRearLeft { get; set; }
        [JsonProperty(PropertyName = "seat_heater_rear_center")]
        public int SeatHeaterRearCenter { get; set; }
        [JsonProperty(PropertyName = "seat_heater_rear_right")]
        public int SeatHeaterRearRight { get; set; }
    }
}
