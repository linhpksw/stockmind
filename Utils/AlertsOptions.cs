namespace stockmind.Utils
{
    public class AlertsOptions
    {
        public int ExpiryThresholdDays { get; set; } = 3;     // default 3 days
        public int SlowMoverWindowDays { get; set; } = 30;    // default 30 days
        public int SlowMoverUnitThreshold { get; set; } = 1;  // default threshold: <1 = zero sales
    }
}
