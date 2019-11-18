using TeslaApi.Enums;

namespace TeslaApi
{
    public class VehicleState
    {
        public bool locked { get; set; }
        public bool remote_start { get; set; }

        /// <summary>
        /// Driver side front door, 0 is closed, 1 is open
        /// </summary>
        public int df { get; set; } //1
        /// <summary>
        /// Driver side rear door, 0 is closed, 4 is open
        /// </summary>
        public int dr { get; set; } //4
        /// <summary>
        /// Passenger side front door, 0 is closed, 2 is open
        /// </summary>
        public int pf { get; set; } //2
        /// <summary>
        /// Passenger side rear door, 0 is closed, 8 is open
        /// </summary>
        public int pr { get; set; } //8
        /// <summary>
        /// Frunk/Front Trunk/Bonnet/Hood, 0 is closed, 16 is open
        /// </summary>
        public int ft { get; set; } //16
        /// <summary>
        /// Trunk/Boot, 0 is closed, 32 is open
        /// </summary>
        public int rt { get; set; } //32
        /// <summary>
        /// Driver side front window
        /// </summary>
        public WindowState df_window { get; set; }
        /// <summary>
        /// Driver side rear window
        /// </summary>
        public WindowState dr_window { get; set; }
        /// <summary>
        /// Passenger side front window
        /// </summary>
        public WindowState pf_window { get; set; }
        /// <summary>
        /// Passenger side front window
        /// </summary>
        public WindowState pr_window { get; set; }
    }
}
