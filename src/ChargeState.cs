namespace TeslaApi
{
    public class ChargeState
    {
        public int battery_level { get; set; }
        public double battery_range { get; set; }
        public double charge_rate { get; set; }
        public string charging_state { get; set; }
        public int minutes_to_full_charge { get; set; }
    }
}
