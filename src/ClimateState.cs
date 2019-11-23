namespace TeslaApi
{
    public class ClimateState
    {
        public double driver_temp_setting { get; set; }
        public double inside_temp { get; set; }
        public bool is_auto_conditioning_on { get; set; }
        public bool is_climate_on { get; set; }
        public bool is_preconditioning { get; set; }
    }
}
