namespace CanisLupus.Worker.Models
{
    public class RateOfChangeData : CandleRawData
    {
        public double Change { get; set; }
        public double ChangePrevious5 { get; set; }
    }
}