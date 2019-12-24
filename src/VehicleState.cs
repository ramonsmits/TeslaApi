using Newtonsoft.Json;
using TeslaApi.Enums;

namespace TeslaApi
{
    public class VehicleState
    {
        [JsonProperty(PropertyName = "software_update")]
        public SoftwareUpdate SoftwareState { get; set; }

        [JsonProperty(PropertyName = "locked")]
        public bool Locked { get; set; }

        [JsonProperty(PropertyName = "remote_start")]
        public bool StartedRemotely { get; set; }

        /// <summary>
        /// Driver side front door, 0 is closed, 1 is open
        /// </summary>
        [JsonProperty(PropertyName = "df")]
        public int Door_DriverFront { get; set; } //1
        /// <summary>
        /// Driver side rear door, 0 is closed, 4 is open
        /// </summary>
        [JsonProperty(PropertyName = "dr")]
        public int Door_DriverRear { get; set; } //4
        /// <summary>
        /// Passenger side front door, 0 is closed, 2 is open
        /// </summary>
        [JsonProperty(PropertyName = "pf")]
        public int Door_PassengerFront { get; set; } //2
        /// <summary>
        /// Passenger side rear door, 0 is closed, 8 is open
        /// </summary>
        [JsonProperty(PropertyName = "pr")]
        public int Door_PassengerRear { get; set; } //8
        /// <summary>
        /// Frunk/Front Trunk/Bonnet/Hood, 0 is closed, 16 is open
        /// </summary>
        [JsonProperty(PropertyName = "ft")]
        public int Frunk { get; set; } //16
        /// <summary>
        /// Trunk/Boot, 0 is closed, 32 is open
        /// </summary>
        [JsonProperty(PropertyName = "rt")]
        public int Trunk { get; set; } //32
        /// <summary>
        /// Driver side front window
        /// </summary>
        [JsonProperty(PropertyName = "fd_window")]
        public WindowState? Window_DriverFront { get; set; }
        /// <summary>
        /// Driver side rear window
        /// </summary>
        [JsonProperty(PropertyName = "rd_window")]
        public WindowState? Window_DriverRear { get; set; }
        /// <summary>
        /// Passenger side front window
        /// </summary>
        [JsonProperty(PropertyName = "fp_window")]
        public WindowState? Window_PassengerFront { get; set; }
        /// <summary>
        /// Passenger side front window
        /// </summary>
        [JsonProperty(PropertyName = "rp_window")]
        public WindowState? Window_PassengerRear { get; set; }
    }
}
