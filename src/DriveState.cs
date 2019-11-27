using Newtonsoft.Json;

namespace TeslaApi
{
    public class DriveState
    {
        /// <summary>
        /// Observed values:
        /// Power null:
        ///   Software updating
        /// Power -11:
        ///   Charging
        /// Power 0:
        ///   Software downloaded
        ///   Software scheduled
        ///   Charge complete
        /// Power 1:
        ///   HVAC Preconditioning
        ///   In drive, stopped
        /// Power 2:
        ///   Parked
        /// Power 7: 
        ///   Drive - speed 29 mph
        /// </summary>
        [JsonProperty(PropertyName = "power")]
        public int Power { get; set; }
        [JsonProperty(PropertyName = "shift_state")]
        public string Shifter { get; set; }
        [JsonProperty(PropertyName = "latitude")]
        public double Latitude { get; set; }
        [JsonProperty(PropertyName = "longitude")]
        public double Longitude { get; set; }
    }
}
