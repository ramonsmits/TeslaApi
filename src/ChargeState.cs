using Newtonsoft.Json;

namespace TeslaApi
{
    public class ChargeState
    {
        [JsonProperty(PropertyName = "battery_level")]
        public int BatteryPercentage { get; set; }
        [JsonProperty(PropertyName = "battery_range")]
        public double BatteryRange { get; set; }
        [JsonProperty(PropertyName = "charge_rate")]
        public double ChargingRate { get; set; }
        [JsonProperty(PropertyName = "charging_state")]
        public string ChargingState { get; set; }
        /// <summary>
        /// Time to full charge in minutes
        /// </summary>
        [JsonProperty(PropertyName = "minutes_to_full_charge")]
        public int TimeToFullCharge { get; set; }
        [JsonProperty(PropertyName= "charge_port_door_open")]
        public bool? ChargeDoorOpen { get; set; }
    }
}
