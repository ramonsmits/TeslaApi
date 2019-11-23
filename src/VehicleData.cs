using System;

namespace TeslaApi
{
    public class VehicleData
    {
        public DriveState drive_state { get; set; }
        public ChargeState charge_state { get; set; }
        public VehicleState vehicle_state { get; set; }
        public SoftwareUpdate software_update { get; set; }
        public ClimateState climate_state { get; set; }
        public GuiSettings gui_settings { get; set; }
        public string state { get; set; }
        public DateTime Fetched { get; }

        public VehicleData() { Fetched = DateTime.Now; }
    }
}
