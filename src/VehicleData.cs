using System;
using Newtonsoft.Json;

namespace TeslaApi
{
    public class VehicleData : Vehicle
    {
        [JsonProperty(PropertyName = "drive_state")]
        public DriveState DriveState { get; set; }
        [JsonProperty(PropertyName = "charge_state")]
        public ChargeState ChargeState { get; set; }
        [JsonProperty(PropertyName = "vehicle_state")]
        public VehicleState VehicleState { get; set; }
        [JsonProperty(PropertyName = "climate_state")]
        public ClimateState ClimateState { get; set; }
        [JsonProperty(PropertyName = "gui_settings")]
        public GuiSettings GuiSettings { get; set; }
        
        public DateTime Fetched { get; }

        public VehicleData() { Fetched = DateTime.UtcNow; }
    }
}
